/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SurfaceDevCenterManager.Utility
{
    internal class DevCenterCredentialsHandler
    {
        public static async Task<List<AuthorizationHandlerCredentials>> GetApiCreds(string CredentialsOption, string AADAuthenticationOption)
        {
            List<AuthorizationHandlerCredentials> myCreds = null;
            if (CredentialsOption == null)
            {
                CredentialsOption = "AADThenFile";
            }
            CredentialsOption = CredentialsOption.ToLowerInvariant();

            if ((CredentialsOption.CompareTo("aadonly") == 0) || (CredentialsOption.CompareTo("aadthenfile") == 0))
            {
                myCreds = await GetWebApiCreds(AADAuthenticationOption);
            }

            if (myCreds == null)
            {
                if ((CredentialsOption.CompareTo("fileonly") == 0) || (CredentialsOption.CompareTo("aadthenfile") == 0))
                {
                    try
                    {
                        string authconfig = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\authconfig.json");
                        myCreds = JsonConvert.DeserializeObject<List<AuthorizationHandlerCredentials>>(authconfig);
                        if (myCreds.Count == 0)
                        {
                            myCreds = null;
                        }
                        else
                        {
                            if (myCreds[0].ClientId.CompareTo("guid") == 0)
                            {
                                myCreds = null;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        myCreds = null;
                    }
                }
            }

            return myCreds;
        }

        private static async Task<List<AuthorizationHandlerCredentials>> GetWebApiCreds(string AADAuthenticationOption)
        {
            List<AuthorizationHandlerCredentials> ReturnList = null;

            string url = ConfigurationManager.AppSettings["url"];

            if (url == null)
            {
                return null;
            }

            Uri WebAPIUri = new Uri(url);

            string clientID = ConfigurationManager.AppSettings["clientID"];
            Uri redirectUri = new Uri(ConfigurationManager.AppSettings["redirectUri"]);
            string resource = ConfigurationManager.AppSettings["resource"];
            string authority = ConfigurationManager.AppSettings["authority"];
            AuthenticationContext authContext = new AuthenticationContext(authority);

            if (AADAuthenticationOption == null)
            {
                AADAuthenticationOption = "never";
            }
            AADAuthenticationOption.ToLowerInvariant();
            PlatformParameters platformParams = new PlatformParameters(PromptBehavior.Never);

            if (AADAuthenticationOption.CompareTo("prompt") == 0)
            {
                platformParams = new PlatformParameters(PromptBehavior.Auto);
            }
            else if (AADAuthenticationOption.CompareTo("always") == 0)
            {
                platformParams = new PlatformParameters(PromptBehavior.Always);
            }
            else if (AADAuthenticationOption.CompareTo("refreshsession") == 0)
            {
                platformParams = new PlatformParameters(PromptBehavior.RefreshSession);
            }
            else if (AADAuthenticationOption.CompareTo("selectaccount") == 0)
            {
                platformParams = new PlatformParameters(PromptBehavior.SelectAccount);
            }

            AuthenticationResult authResult = null;
            bool retryAuth = false;

            try
            {
                authResult = await authContext.AcquireTokenAsync(resource, clientID, redirectUri, platformParams);
            }
            catch (AdalException)
            {
                retryAuth = true;
                authResult = null;
            }

            if (retryAuth)
            {

                try
                {
                    authResult = await authContext.AcquireTokenAsync(resource, clientID, redirectUri, new PlatformParameters(PromptBehavior.Auto));
                }
                catch (AdalException)
                {
                    authResult = null;
                }
            }

            Uri restApi = new Uri(WebAPIUri, "/api/credentials");

            if (authResult != null)
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authResult.AccessTokenType, authResult.AccessToken);

                    HttpResponseMessage infoResult = await client.GetAsync(restApi);

                    string content = await infoResult.Content.ReadAsStringAsync();

                    if (infoResult.IsSuccessStatusCode)
                    {
                        ReturnList = JsonConvert.DeserializeObject<List<AuthorizationHandlerCredentials>>(content);
                    }
                }
            }
            return ReturnList;
        }
    }
}
