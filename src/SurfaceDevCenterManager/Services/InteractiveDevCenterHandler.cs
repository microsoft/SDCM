/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Devices.HardwareDevCenterManager;
using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;

namespace SurfaceDevCenterManager.Services;

/// <summary>
///     An <see cref="IDevCenterHandler" /> implementation for the <see cref="AuthMode.Interactive" />
///     auth mode. The library's own <c>DevCenterHandler</c> only supports managed identity, client
///     secret, and certificate credentials internally, with no seam for a delegated user token, so
///     this mirrors its request shape while sourcing the bearer token from
///     <see cref="IAadTokenProvider" /> instead.
/// </summary>
public sealed class InteractiveDevCenterHandler : IDevCenterHandler, IDisposable
{
    private const string DefaultErrorCode = "InvalidInput";

    private const string ProductsUrl = "/hardware/products";
    private const string ProductSubmissionUrl = "/hardware/products/{0}/submissions";

    private const string PartnerSubmissionUrl =
        "/hardware/products/relationships/sourcepubliherid/{0}/sourceproductid/{1}/sourcesubmissionid/{2}";

    private const string ProductSubmissionCommitUrl = "/hardware/products/{0}/submissions/{1}/commit";
    private const string ShippingLabelUrl = "/hardware/products/{0}/submissions/{1}/shippingLabels";
    private const string AudienceUrl = "/hardware/audiences";
    private const string CreateMetaDataUrl = "/hardware/products/{0}/submissions/{1}/createpublishermetadata";
    private const string CancelShippingLabelUrl = "/hardware/products/{0}/submissions/{1}/shippingLabels/{2}/cancel";

    private readonly HttpClient _client;
    private readonly Guid _correlationId;
    private readonly string _baseUrl;
    private readonly LastCommandDelegate? _lastCommand;
    private DevCenterTrace? _trace;

    public InteractiveDevCenterHandler(
        IAadTokenProvider tokenProvider,
        string clientId,
        string authority,
        string redirectUri,
        string url,
        string urlPrefix,
        AadPromptMode promptMode,
        DevCenterOptions options)
    {
        _baseUrl = new Uri(new Uri(url, UriKind.Absolute), urlPrefix).AbsoluteUri;
        _correlationId = options.CorrelationId;
        _lastCommand = options.LastCommand;

        BearerTokenHandler bearerHandler = new(tokenProvider, clientId, authority, redirectUri, url, promptMode);
        _client = new HttpClient(bearerHandler, true)
        {
            Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds)
        };
    }

    public async Task<DevCenterErrorDetails?> InvokeHdcService(
        HttpMethod method, string uri, object? input, Action<string>? processContent)
    {
        string requestId = Guid.NewGuid().ToString();
        string json = JsonSerializer.Serialize(input ?? new object());

        using HttpRequestMessage request = new(method, uri);
        _client.DefaultRequestHeaders.Remove("MS-CorrelationId");
        _client.DefaultRequestHeaders.Remove("MS-RequestId");
        _client.DefaultRequestHeaders.Add("MS-CorrelationId", _correlationId.ToString());
        _client.DefaultRequestHeaders.Add("MS-RequestId", requestId);

        _trace = new DevCenterTrace
        {
            CorrelationId = _correlationId.ToString(),
            RequestId = requestId,
            Method = method.ToString(),
            Url = uri,
            Content = json
        };

        _lastCommand?.Invoke(new DevCenterErrorDetails { Trace = _trace });

        if (method != HttpMethod.Get && method != HttpMethod.Post && method != HttpMethod.Put)
        {
            return new DevCenterErrorDetails
            {
                HttpErrorCode = -1,
                Code = DefaultErrorCode,
                Message = "Unsupported HTTP method",
                Trace = _trace
            };
        }

        HttpResponseMessage response;
        if (method == HttpMethod.Get)
        {
            response = await _client.GetAsync(new Uri(uri)).ConfigureAwait(false);
        }
        else if (method == HttpMethod.Post)
        {
            using StringContent content = new(json, System.Text.Encoding.UTF8, "application/json");
            response = await _client.PostAsync(new Uri(uri), content).ConfigureAwait(false);
        }
        else
        {
            response = await _client.PutAsync(new Uri(uri), null).ConfigureAwait(false);
        }

        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            processContent?.Invoke(body);
            return null;
        }

        DevCenterErrorReturn? returnError;
        try
        {
            returnError = JsonSerializer.Deserialize<DevCenterErrorReturn>(body);
        }
        catch (JsonException)
        {
            returnError = new DevCenterErrorReturn
            {
                HttpErrorCode = (int)response.StatusCode,
                StatusCode = ((int)response.StatusCode) + " " + response.StatusCode,
                Message = body
            };
        }

        if (returnError is null || (returnError.HttpErrorCode.HasValue && returnError.HttpErrorCode.Value == 0))
        {
            returnError = new DevCenterErrorReturn
            {
                HttpErrorCode = (int)response.StatusCode,
                StatusCode = ((int)response.StatusCode) + " " + response.StatusCode,
                Message = response.ReasonPhrase
            };
        }

        if (returnError.Error != null)
        {
            returnError.Error.HttpErrorCode = (int)response.StatusCode;
            return returnError.Error;
        }

        return new DevCenterErrorDetails
        {
            Headers = response.Headers,
            HttpErrorCode = (int)response.StatusCode,
            Code = returnError.StatusCode,
            Message = returnError.Message,
            ValidationErrors = returnError.ValidationErrors,
            Trace = _trace
        };
    }

    public async Task<DevCenterResponse<TOutput>> InvokeHdcService<TOutput>(
        HttpMethod method, string uri, object? input, bool isMany) where TOutput : IArtifact
    {
        DevCenterResponse<TOutput> response = new();
        response.Error = await InvokeHdcService(method, uri, input, content =>
        {
            if (isMany)
            {
                Response<TOutput>? parsed = JsonSerializer.Deserialize<Response<TOutput>>(content);
                response.ReturnValue = parsed?.Value;
            }
            else
            {
                TOutput? parsed = JsonSerializer.Deserialize<TOutput>(content);
                if (parsed?.Id != null)
                {
                    response.ReturnValue = [parsed];
                }
            }
        }).ConfigureAwait(false);

        response.Trace = _trace;
        return response;
    }

    public Task<DevCenterResponse<TOutput>> HdcGet<TOutput>(string uri, bool isMany) where TOutput : IArtifact
    {
        return InvokeHdcService<TOutput>(HttpMethod.Get, uri, null, isMany);
    }

    public Task<DevCenterResponse<TOutput>> HdcPost<TOutput>(string uri, object input) where TOutput : IArtifact
    {
        return InvokeHdcService<TOutput>(HttpMethod.Post, uri, input, false);
    }

    public Task<DevCenterResponse<Product>> NewProduct(NewProduct input)
    {
        return HdcPost<Product>(_baseUrl + ProductsUrl, input);
    }

    public Task<DevCenterResponse<Product>> GetProducts(string? productId = null)
    {
        string url = _baseUrl + ProductsUrl;
        bool isMany = string.IsNullOrEmpty(productId);
        if (!isMany)
        {
            url += "/" + Uri.EscapeDataString(productId!);
        }

        return HdcGet<Product>(url, isMany);
    }

    public Task<DevCenterResponse<Submission>> NewSubmission(string productId, NewSubmission submissionInfo)
    {
        string url = _baseUrl + string.Format(ProductSubmissionUrl, Uri.EscapeDataString(productId));
        return HdcPost<Submission>(url, submissionInfo);
    }

    public Task<DevCenterResponse<Submission>> GetSubmission(string productId, string? submissionId = null)
    {
        string url = _baseUrl + string.Format(ProductSubmissionUrl, Uri.EscapeDataString(productId));
        bool isMany = string.IsNullOrEmpty(submissionId);
        if (!isMany)
        {
            url += "/" + Uri.EscapeDataString(submissionId!);
        }

        return HdcGet<Submission>(url, isMany);
    }

    public Task<DevCenterResponse<Submission>> GetPartnerSubmission(
        string publisherId, string productId, string submissionId)
    {
        string url = _baseUrl + string.Format(
            PartnerSubmissionUrl,
            Uri.EscapeDataString(publisherId),
            Uri.EscapeDataString(productId),
            Uri.EscapeDataString(submissionId));
        return HdcGet<Submission>(url, string.IsNullOrEmpty(submissionId));
    }

    public async Task<DevCenterResponse<bool>> CommitSubmission(string productId, string submissionId)
    {
        string url = _baseUrl + string.Format(
            ProductSubmissionCommitUrl, Uri.EscapeDataString(productId), Uri.EscapeDataString(submissionId));
        DevCenterErrorDetails? error = await InvokeHdcService(HttpMethod.Post, url, null, null).ConfigureAwait(false);

        DevCenterResponse<bool> result = new()
        {
            Error = error,
            ReturnValue = [error == null],
            Trace = _trace
        };

        if (error is { HttpErrorCode: (int)HttpStatusCode.BadGateway } &&
            string.Equals(error.Code, "requestInvalidForCurrentState", StringComparison.OrdinalIgnoreCase))
        {
            DevCenterResponse<Submission> status = await GetSubmission(productId, submissionId).ConfigureAwait(false);
            if (status.Error == null && status.ReturnValue is { Count: > 0 } &&
                string.Equals(status.ReturnValue[0].CommitStatus, "commitComplete", StringComparison.OrdinalIgnoreCase))
            {
                result.Error = null;
                result.ReturnValue = [true];
            }
        }

        return result;
    }

    public Task<DevCenterResponse<ShippingLabel>> NewShippingLabel(
        string productId, string submissionId, NewShippingLabel shippingLabelInfo)
    {
        string url = _baseUrl + string.Format(
            ShippingLabelUrl, Uri.EscapeDataString(productId), Uri.EscapeDataString(submissionId));
        return HdcPost<ShippingLabel>(url, shippingLabelInfo);
    }

    public Task<DevCenterResponse<ShippingLabel>> GetShippingLabels(
        string productId, string submissionId, string? shippingLabelId = null)
    {
        string url = _baseUrl + string.Format(
            ShippingLabelUrl, Uri.EscapeDataString(productId), Uri.EscapeDataString(submissionId));
        bool isMany = string.IsNullOrEmpty(shippingLabelId);
        if (!isMany)
        {
            url += "/" + Uri.EscapeDataString(shippingLabelId!);
        }

        url += "?includeTargetingInfo=true";
        return HdcGet<ShippingLabel>(url, isMany);
    }

    public Task<DevCenterResponse<Audience>> GetAudiences()
    {
        return HdcGet<Audience>(_baseUrl + AudienceUrl, true);
    }

    public async Task<DevCenterResponse<bool>> CreateMetaData(string productId, string submissionId)
    {
        string url = _baseUrl + string.Format(
            CreateMetaDataUrl, Uri.EscapeDataString(productId), Uri.EscapeDataString(submissionId));
        DevCenterErrorDetails? error = await InvokeHdcService(HttpMethod.Post, url, null, null).ConfigureAwait(false);
        return new DevCenterResponse<bool>
        {
            Error = error,
            ReturnValue = [error == null],
            Trace = _trace
        };
    }

    public async Task<DevCenterResponse<bool>> CancelShippingLabel(
        string productId, string submissionId, string shippingLabelId)
    {
        string url = _baseUrl + string.Format(
            CancelShippingLabelUrl, productId, submissionId, shippingLabelId);
        DevCenterErrorDetails? error = await InvokeHdcService(HttpMethod.Put, url, null, null).ConfigureAwait(false);
        return new DevCenterResponse<bool>
        {
            Error = error,
            ReturnValue = [error == null],
            Trace = _trace
        };
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    /// <summary>Attaches a bearer token acquired via MSAL to every outgoing request.</summary>
    private sealed class BearerTokenHandler(
        IAadTokenProvider tokenProvider,
        string clientId,
        string authority,
        string redirectUri,
        string resource,
        AadPromptMode promptMode) : DelegatingHandler(new HttpClientHandler())
    {
        private string? _accessToken;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // MSAL's own cache makes repeated acquisitions within a process cheap (silent cache hit),
            // so there is no need to hand-roll a token cache or a request-retry-on-401 here.
            _accessToken = await tokenProvider.AcquireTokenAsync(
                clientId, authority, redirectUri, resource, promptMode, cancellationToken).ConfigureAwait(false);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
