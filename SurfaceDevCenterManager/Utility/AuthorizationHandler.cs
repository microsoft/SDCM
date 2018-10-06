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
using System.Threading;
using System.Threading.Tasks;

namespace SurfaceDevCenterManager.Utility
{
    internal class AuthorizationHandler : DelegatingHandler
    {
        private string _AccessToken;
        private readonly AuthorizationHandlerCredentials AuthCredentials;

        /// <summary>
        /// Handles OAuth Tokens for HTTP request to Microsoft Hardware Dev Center
        /// </summary>
        /// <param name="credentials">The set of credentials to use for the token acquitisiton</param>
        public AuthorizationHandler(AuthorizationHandlerCredentials credentials)
        {
            _AccessToken = null;
            AuthCredentials = credentials;
            InnerHandler = new HttpClientHandler();
        }

        /// <summary>
        /// Inserts Bearer token into HTTP requests and also does a retry on failed requests since 
        /// HWDC sometimes fails
        /// </summary>
        /// <param name="request">HTTP Request to send</param>
        /// <param name="cancellationToken">CancellationToken in case the request is cancelled</param>
        /// <returns>Returns the HttpResonseMessage from the request</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //Clone the original request so we have a copy in case of a failure
            HttpRequestMessage clonedRequest = await CloneHttpRequestMessageAsync(request);

            //If there is no valid access token for HWDC, get one and then add it to the request
            if (_AccessToken == null)
            {
                await ObtainAccessToken();
            }
            request.Headers.Add("Authorization", "Bearer " + _AccessToken);

            //Send request
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            //If unauthorized, the token likely expired so get a new one
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                try
                {
                    //Get a new access token
                    if (await ObtainAccessToken())
                    {
                        //Add token to cloned request and resend
                        clonedRequest.Headers.Add("Authorization", "Bearer " + _AccessToken);
                        response = await base.SendAsync(clonedRequest, cancellationToken);
                    }
                }
                catch (InvalidOperationException)
                {
                    return response;
                }
            }
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                //Somtimes HWDC returns 500 errors so wait a bit then retry once instead of failing the whole flow
                Thread.Sleep(2000);

                // Resend the request with the token
                clonedRequest.Headers.Add("Authorization", "Bearer " + _AccessToken);
                response = await base.SendAsync(clonedRequest, cancellationToken);
            }

            return response;
        }

        private async Task<bool> ObtainAccessToken()
        {
            bool IsSuccess = false;
            string DevCenterTokenUrl = string.Format("https://login.microsoftonline.com/{0}/oauth2/token", AuthCredentials.TenantId);

            using (HttpClient client = new HttpClient())
            {
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
