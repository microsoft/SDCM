/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.
--*/
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SurfaceDevCenterManager.Utility
{
    internal class HttpRetriesExhaustedException : Exception
    {
        public HttpRetriesExhaustedException(string msg) : base(msg) { }
    }

    internal class AuthorizationHandler : DelegatingHandler
    {
        private string _AccessToken;
        private readonly AuthorizationHandlerCredentials AuthCredentials;
        private readonly TimeSpan HttpTimeout;

        private const int MAX_RETRIES = 10;

        /// <summary>
        /// Handles OAuth Tokens for HTTP request to Microsoft Hardware Dev Center
        /// </summary>
        /// <param name="credentials">The set of credentials to use for the token acquitisiton</param>
        public AuthorizationHandler(AuthorizationHandlerCredentials credentials, int HttpTimeoutSeconds)
        {
            _AccessToken = null;
            AuthCredentials = credentials;
            InnerHandler = new HttpClientHandler();
            HttpTimeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);
        }

        /// <summary>
        /// Inserts Bearer token into HTTP requests and also does a retry on failed requests since
        /// HDC sometimes fails
        /// </summary>
        /// <param name="request">HTTP Request to send</param>
        /// <param name="cancellationToken">CancellationToken in case the request is cancelled</param>
        /// <returns>Returns the HttpResonseMessage from the request</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int tries = 0;
            HttpResponseMessage response = null;

            //If there is no valid access token for HDC, get one and then add it to the request
            if (_AccessToken == null)
            {
                await ObtainAccessToken();
            }

            while (tries < MAX_RETRIES)
            {
                tries++;

                //Clone the original request so we have a copy in case of a failure
                HttpRequestMessage clonedRequest = await CloneHttpRequestMessageAsync(request);

                clonedRequest.Headers.Add("Authorization", "Bearer " + _AccessToken);

                //Send request
                try
                {
                    response = await base.SendAsync(clonedRequest, cancellationToken);
                }
                catch (SocketException)
                {
                    //HDC timed out, wait a bit and try again
                    Thread.Sleep(2000);
                    continue;
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

                //If unauthorized, the token likely expired so get a new one and retry
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await ObtainAccessToken();
                    continue;
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    //Somtimes HDC returns 500 errors so wait a bit then retry once instead of failing the whole flow
                    Thread.Sleep(2000);
                    continue;
                }

                break;
            }

            if (response == null)
            {
                throw new HttpRetriesExhaustedException("AuthorizationHandler: NULL response, unable to talk to HDC");
            }

            return response;
        }

        private async Task<bool> ObtainAccessToken()
        {
            bool IsSuccess = false;
            string DevCenterTokenUrl = string.Format("https://login.microsoftonline.com/{0}/oauth2/token", AuthCredentials.TenantId);

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = HttpTimeout;
                Uri restApi = new Uri(DevCenterTokenUrl);

                StringContent postcontent = new StringContent("grant_type=client_credentials" +
                                                              "&client_id=" + Uri.EscapeDataString(AuthCredentials.ClientId) +
                                                              "&client_secret=" + Uri.EscapeDataString(AuthCredentials.Key) +
                                                              "&resource=" + Uri.EscapeDataString("https://manage.devcenter.microsoft.com"),
                                                              System.Text.Encoding.UTF8,
                                                              "application/x-www-form-urlencoded");

                HttpResponseMessage infoResult = await client.PostAsync(restApi, postcontent);

                string content = await infoResult.Content.ReadAsStringAsync();

                if (infoResult.IsSuccessStatusCode)
                {
                    dynamic jObj = JsonConvert.DeserializeObject(content);

                    if (jObj != null)
                    {
                        _AccessToken = jObj.access_token;
                        IsSuccess = true;
                    }
                }
            }

            return IsSuccess;
        }

        //
        // https://stackoverflow.com/questions/21467018/how-to-forward-an-httprequestmessage-to-another-server
        //
        private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
        {
            HttpRequestMessage clone = new HttpRequestMessage(request.Method, request.RequestUri);

            // Copy the request's content (via a MemoryStream) into the cloned object
            MemoryStream ms = new MemoryStream();
            if (request.Content != null)
            {
                await request.Content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                // Copy the content headers
                if (request.Content.Headers != null)
                {
                    foreach (KeyValuePair<string, IEnumerable<string>> h in request.Content.Headers)
                    {
                        clone.Content.Headers.Add(h.Key, h.Value);
                    }
                }
            }
            clone.Version = request.Version;

            foreach (KeyValuePair<string, object> prop in request.Properties)
            {
                clone.Properties.Add(prop);
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
