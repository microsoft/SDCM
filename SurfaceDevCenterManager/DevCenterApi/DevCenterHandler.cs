/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using SurfaceDevCenterManager.Utility;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SurfaceDevCenterManager.DevCenterApi
{
    internal sealed class DevCenterHandler : IDisposable
    {
        private readonly DelegatingHandler AuthHandler;
        private readonly AuthorizationHandlerCredentials AuthCredentials;
        private readonly TimeSpan HttpTimeout;
        private const int MAX_RETRIES = 10;

        /// <summary>
        /// Creates a new DevCenterHandler using the provided credentials
        /// </summary>
        /// <param name="credentials">Authorization credentials for HWDC</param>
        public DevCenterHandler(AuthorizationHandlerCredentials credentials, uint httpTimeoutSeconds)
        {
            AuthCredentials = credentials;
            AuthHandler = new AuthorizationHandler(AuthCredentials, httpTimeoutSeconds);
            HttpTimeout = TimeSpan.FromSeconds(httpTimeoutSeconds);
        }

        private string GetDevCenterBaseUrl()
        {
            Uri returnUri = AuthCredentials.Url;
            if (AuthCredentials.UrlPrefix != null)
            {
                returnUri = new Uri(returnUri, AuthCredentials.UrlPrefix);
            }
            return returnUri.AbsoluteUri;
        }

        private const string DefaultErrorcode = "InvalidInput";

        /// <summary>
        /// Invokes HDC service with the specified options
        /// </summary>
        /// <param name="method">HTTP method to use</param>
        /// <param name="uri">URI of the service to invoke</param>
        /// <param name="input">Content used as the request options</param>
        /// <param name="processContent">Process the service return content</param>
        /// <returns>Dev Center response with either an error or null if the operation was successful</returns>
        public async Task<DevCenterErrorDetails> InvokeHdcService(
            HttpMethod method, string uri, object input, Action<string> processContent)
        {
            int retries = 0;
            DevCenterErrorReturn reterr = null;

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                client.Timeout = HttpTimeout;
                Uri restApi = new Uri(uri);

                if (!(HttpMethod.Get == method || HttpMethod.Post == method))
                {
                    return new DevCenterErrorDetails
                    {
                        HttpErrorCode = -1,
                        Code = DefaultErrorcode,
                        Message = "Unsupported HTTP method"
                    };
                }

                HttpResponseMessage infoResult = null;

                while (retries < MAX_RETRIES)
                {
                    retries++;
                    reterr = null;

                    try
                    {
                        if (HttpMethod.Get == method)
                        {
                            infoResult = await client.GetAsync(restApi);
                        }
                        else if (HttpMethod.Post == method)
                        {
                            string json = JsonConvert.SerializeObject(input ?? new object());
                            StringContent postContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                            infoResult = await client.PostAsync(restApi, postContent);
                        }
                    }
                    catch (TaskCanceledException tcex)
                    {
                        if (!tcex.CancellationToken.IsCancellationRequested)
                        {
                            //HDC time out, wait a bit and try again
                            Thread.Sleep(2000);
                            continue;
                        }
                        else
                        {
                            throw tcex;
                        }
                    }

                    string content = await infoResult.Content.ReadAsStringAsync();
                    if (infoResult.IsSuccessStatusCode)
                    {
                        processContent?.Invoke(content);
                        return null;
                    }

                    try
                    {
                        reterr = JsonConvert.DeserializeObject<DevCenterErrorReturn>(content);
                    }
                    catch (JsonReaderException)
                    {
                        // Error is in bad format, return raw
                        reterr = new DevCenterErrorReturn()
                        {
                            HttpErrorCode = (int)infoResult.StatusCode,
                            StatusCode = infoResult.StatusCode.ToString("D") + " " + infoResult.StatusCode.ToString(),
                            Message = content
                        };
                    }

                    // reterr can be null when there is HTTP error
                    if (reterr == null)
                    {
                        reterr = new DevCenterErrorReturn()
                        {
                            HttpErrorCode = (int)infoResult.StatusCode,
                            StatusCode = infoResult.StatusCode.ToString("D") + " " + infoResult.StatusCode.ToString(),
                            Message = infoResult.ReasonPhrase
                        };
                    }

                    // Retry on the following 'infrastructure' issues
                    // 502 BadGateway
                    if (infoResult.StatusCode != System.Net.HttpStatusCode.BadGateway)
                    {
                        break;
                    }
                    await Task.Delay(new Random().Next(1000, 10000));
                }

                if (reterr.Error != null)
                {
                    return reterr.Error;
                }

                return new DevCenterErrorDetails
                {
                    HttpErrorCode = reterr.HttpErrorCode,
                    Code = reterr.StatusCode,
                    Message = reterr.Message
                };
            }
        }

        /// <summary>
        /// Invokes HDC service with the specified options
        /// </summary>
        /// <param name="method">HTTP method to use</param>
        /// <param name="uri">URI of the service to invoke</param>
        /// <param name="input">Options for the new artifact to be generated</param>
        /// <param name="isMany">Whether the result has a single entity or multiple</param>
        /// <returns>Dev Center response with either an error or an artifact if created/queried successfully</returns>
        public async Task<DevCenterResponse<Output>> InvokeHdcService<Output>(
            HttpMethod method, string uri, object input, bool isMany) where Output : IArtifact
        {
            DevCenterResponse<Output> retval = new DevCenterResponse<Output>();
            retval.Error = await InvokeHdcService(method, uri, input, (content) =>
            {
                if (isMany)
                {
                    Response<Output> ret = JsonConvert.DeserializeObject<Response<Output>>(content);
                    retval.ReturnValue = ret.Value;
                }
                else
                {
                    Output ret = JsonConvert.DeserializeObject<Output>(content);
                    if (ret.Id != null)
                    {
                        retval.ReturnValue = new List<Output> { ret };
                    }
                }
            });

            return retval;
        }

        /// <summary>
        /// Invokes HDC GET request with the specified options
        /// </summary>
        /// <param name="uri">URI of the service to invoke</param>
        /// <param name="isMany">Whether the result has a single entity or multiple</param>
        /// <returns>Dev Center response with either an error or a list of artifacts if queried successfully</returns>
        public async Task<DevCenterResponse<Output>> HdcGet<Output>(string uri, bool isMany) where Output : IArtifact
        {
            return await InvokeHdcService<Output>(HttpMethod.Get, uri, null, isMany);
        }

        /// <summary>
        /// Invokes HDC POST request with the specified options
        /// </summary>
        /// <param name="uri">URI of the service to invoke</param>
        /// <param name="input">Options for the new artifact to be generated</param>
        /// <returns>Dev Center response with either an error or an artifact if created successfully</returns>
        public async Task<DevCenterResponse<Output>> HdcPost<Output>(string uri, object input) where Output : IArtifact
        {
            return await InvokeHdcService<Output>(HttpMethod.Post, uri, input, false);
        }

        private const string DevCenterProductsUrl = "/hardware/products";

        /// <summary>
        /// Creates a new New Product in HWDC with the specified options
        /// </summary>
        /// <param name="input">Options for the new Product to be generated</param>
        /// <returns>Dev Center response with either an error or a Product if created successfully</returns>
        public async Task<DevCenterResponse<Product>> NewProduct(NewProduct input)
        {
            string newProductsUrl = GetDevCenterBaseUrl() + DevCenterProductsUrl;
            return await HdcPost<Product>(newProductsUrl, input);
        }

        /// <summary>
        /// Gets a list of products or a specific product from HWDC
        /// </summary>
        /// <param name="productId">Gets all products if null otherwise retrieves the specified product</param>
        /// <returns>Dev Center response with either an error or a Product if queried successfully</returns>
        public async Task<DevCenterResponse<Product>> GetProducts(string productId = null)
        {
            string getProductsUrl = GetDevCenterBaseUrl() + DevCenterProductsUrl;

            bool isMany = string.IsNullOrEmpty(productId);
            if (!isMany)
            {
                getProductsUrl += "/" + Uri.EscapeDataString(productId);
            }

            return await HdcGet<Product>(getProductsUrl, isMany);
        }

        private const string DevCenterProductSubmissionUrl = "/hardware/products/{0}/submissions";

        /// <summary>
        /// Creates a new Submission in HWDC with the specified options
        /// </summary>
        /// <param name="productId">Specifiy the Product ID for this Submission</param>
        /// <param name="submissionInfo">Options for the new Submission to be generated</param>
        /// <returns>Dev Center response with either an error or a Submission if created successfully</returns>
        public async Task<DevCenterResponse<Submission>> NewSubmission(string productId, NewSubmission submissionInfo)
        {
            string newProductSubmissionUrl = GetDevCenterBaseUrl() +
                string.Format(DevCenterProductSubmissionUrl, Uri.EscapeDataString(productId));
            return await HdcPost<Submission>(newProductSubmissionUrl, submissionInfo);
        }

        /// <summary>
        /// Gets a list of submissions or a specific submission from HWDC
        /// </summary>
        /// <param name="productId">Specifiy the Product ID for this Submission</param>
        /// <param name="submissionId">Gets all submissions if null otherwise retrieves the specified submission</param>
        /// <returns>Dev Center response with either an error or a Submission if queried successfully</returns>
        public async Task<DevCenterResponse<Submission>> GetSubmission(string productId, string submissionId = null)
        {
            string getProductSubmissionUrl = GetDevCenterBaseUrl() +
                string.Format(DevCenterProductSubmissionUrl, Uri.EscapeDataString(productId));

            bool isMany = string.IsNullOrEmpty(submissionId);
            if (!isMany)
            {
                getProductSubmissionUrl += "/" + Uri.EscapeDataString(submissionId);
            }

            return await HdcGet<Submission>(getProductSubmissionUrl, isMany);
        }

        private const string DevCenterPartnerSubmissionUrl =
            "/hardware/products/relationships/sourcepubliherid/{0}/sourceproductid/{1}/sourcesubmissionid/{2}";

        /// <summary>
        /// Gets shared submission info from a partner-shared Submission with partner ids
        /// </summary>
        /// <param name="publisherId">Specifiy the Partner's Publisher ID for this Submission</param>
        /// <param name="productId">Specifiy the Partner's Product ID for this Submission</param>
        /// <param name="submissionId">Specifiy the Partner's Submission ID for this Submission</param>
        /// <returns>Dev Center response with either an error or a Submission if queried successfully with IDs for the querying account</returns>
        public async Task<DevCenterResponse<Submission>> GetPartnerSubmission(
            string publisherId, string productId, string submissionId)
        {
            string getProductSubmissionUrl = GetDevCenterBaseUrl() +
                string.Format(DevCenterPartnerSubmissionUrl, Uri.EscapeDataString(publisherId),
                Uri.EscapeDataString(productId), Uri.EscapeDataString(submissionId));
            return await HdcGet<Submission>(getProductSubmissionUrl, string.IsNullOrEmpty(submissionId));
        }


        private const string DevCenterProductSubmissionCommitUrl = "/hardware/products/{0}/submissions/{1}/commit";
        /// <summary>
        /// Commits a Submission in HWDC
        /// </summary>
        /// <param name="productId">Specifiy the Product ID for the Submission to commit</param>
        /// <param name="submissionId">Specifiy the Submission ID for the Submission to commit</param>
        /// <returns>Dev Center response with either an error or a true if comitted successfully</returns>
        public async Task<DevCenterResponse<bool>> CommitSubmission(string productId, string submissionId)
        {
            string commitProductSubmissionUrl = GetDevCenterBaseUrl() +
                string.Format(DevCenterProductSubmissionCommitUrl, Uri.EscapeDataString(productId), Uri.EscapeDataString(submissionId));
            DevCenterErrorDetails error = await InvokeHdcService(HttpMethod.Post, commitProductSubmissionUrl, null, null);
            DevCenterResponse<bool> ret = new DevCenterResponse<bool>()
            {
                Error = error,
                ReturnValue = new List<bool>()
                {
                    error == null
                }
            };

            if (error != null)
            {
                if ((error.HttpErrorCode == (int)System.Net.HttpStatusCode.BadGateway) &&
                    (string.Compare(error.Code, "requestInvalidForCurrentState", true) == 0)
                    )
                {
                    //  Communication issue likely caused the submission to already be done.  Check.
                    DevCenterResponse<Submission> SubmissionStatus = await GetSubmission(productId, submissionId);
                    if (SubmissionStatus.Error == null)
                    {
                        Submission s = SubmissionStatus.ReturnValue[0];
                        if (string.Compare(s.CommitStatus, "commitComplete", true) == 0)
                        {
                            //Actually did commit
                            ret.Error = null;
                            ret.ReturnValue = new List<bool>() { true };
                        }
                    }
                }
            }

            return ret;
        }

        private const string DevCenterShippingLabelUrl = "/hardware/products/{0}/submissions/{1}/shippingLabels";

        /// <summary>
        /// Creates a new Shipping Label in HWDC with the specified options
        /// </summary>
        /// <param name="productId">Specifiy the Product ID for this Shipping Label</param>
        /// <param name="submissionId">Specifiy the Submission ID for this Shipping Label</param>
        /// <param name="shippingLabelInfo">Options for the new Shipping Label to be generated</param>
        /// <returns>Dev Center response with either an error or a ShippingLabel if created successfully</returns>
        public async Task<DevCenterResponse<ShippingLabel>> NewShippingLabel(
            string productId, string submissionId, NewShippingLabel shippingLabelInfo)
        {
            string shippingLabelUrl = GetDevCenterBaseUrl() +
                string.Format(DevCenterShippingLabelUrl, Uri.EscapeDataString(productId), Uri.EscapeDataString(submissionId));
            return await HdcPost<ShippingLabel>(shippingLabelUrl, shippingLabelInfo);
        }

        /// <summary>
        /// Gets a list of shipping labels or a specific shipping label from HWDC
        /// </summary>
        /// <param name="productId">Specifiy the Product ID for this Shipping Label</param>
        /// <param name="submissionId">Specifiy the Submission ID for this Shipping Label</param>
        /// <param name="shippingLabelId">Gets all Shipping Labels if null otherwise retrieves the specified Shipping Label</param>
        /// <returns>Dev Center response with either an error or a ShippingLabel if queried successfully</returns>
        public async Task<DevCenterResponse<ShippingLabel>> GetShippingLabels(
            string productId, string submissionId, string shippingLabelId = null)
        {
            string getShippingLabelUrl = GetDevCenterBaseUrl() +
                string.Format(DevCenterShippingLabelUrl, Uri.EscapeDataString(productId), Uri.EscapeDataString(submissionId));

            bool isMany = string.IsNullOrEmpty(shippingLabelId);
            if (!isMany)
            {
                getShippingLabelUrl += "/" + Uri.EscapeDataString(shippingLabelId);
            }

            getShippingLabelUrl += @"?includeTargetingInfo=true";
            return await HdcGet<ShippingLabel>(getShippingLabelUrl, isMany);
        }

        private const string DevCenterAudienceUrl = "/hardware/audiences";

        /// <summary>
        /// Gets a list of valid audiences from HWDC
        /// </summary>
        /// <returns>Dev Center response with either an error or a Audience if queried successfully</returns>
        public async Task<DevCenterResponse<Audience>> GetAudiences()
        {
            string getAudienceUrl = GetDevCenterBaseUrl() + DevCenterAudienceUrl;
            return await HdcGet<Audience>(getAudienceUrl, true);
        }


        private const string DevCenterCreateMetaDataUrl = "/hardware/products/{0}/submissions/{1}/createpublishermetadata";

        /// <summary>
        /// Requests creation of driver metadata on older submissions to HWDC
        /// </summary>
        /// <returns>Dev Center response with either an error or ok if metadata was created successfully</returns>
        public async Task<DevCenterResponse<bool>> CreateMetaData(string productId, string submissionId)
        {
            string createMetaDataUrl = GetDevCenterBaseUrl() +
                string.Format(DevCenterCreateMetaDataUrl, Uri.EscapeDataString(productId), Uri.EscapeDataString(submissionId));
            DevCenterErrorDetails error = await InvokeHdcService(HttpMethod.Post, createMetaDataUrl, null, null);
            DevCenterResponse<bool> ret = new DevCenterResponse<bool>()
            {
                Error = error,
                ReturnValue = new List<bool>()
                {
                    error == null
                }
            };
            return ret;
        }

        public void Dispose()
        {
            AuthHandler.Dispose();
        }
    }
}
