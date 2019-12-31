/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Microsoft.Devices.HardwareDevCenterManager.Utility;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SurfaceDevCenterManager.Utility
{
    internal class DevCenterCredentialsHandler
    {
        private static readonly byte[] s_aditionalEntropy = { 254, 122, 123, 135, 23, 79, 6 };

        private static string GetCredential()
        {
            byte[] data = null;
            byte[] encryptData = null;
            string retval = null;

            if (System.IO.File.Exists(GetSdcmBinPath()))
            {

                encryptData = System.IO.File.ReadAllBytes(GetSdcmBinPath());

                if (encryptData != null)
                {
                    try
                    {
                        data = ProtectedData.Unprotect(encryptData, s_aditionalEntropy, DataProtectionScope.CurrentUser);
                        retval = Encoding.Unicode.GetString(data);
                    }
                    catch (CryptographicException)
                    {
                        data = null;
                    }
                }
            }

            return retval;
        }

        private static bool SetCredential(string token)
        {
            byte[] data = Encoding.Unicode.GetBytes(token);
            byte[] encryptData = null;
            bool retval = false;
            try
            {
                encryptData = ProtectedData.Protect(data, s_aditionalEntropy, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException)
            {
                encryptData = null;
            }

            if (encryptData != null)
            {

                System.IO.File.WriteAllBytes(GetSdcmBinPath(), encryptData);
                retval = true;
            }

            return retval;
        }

        private static string GetSdcmBinPath()
        {
            string tmpPath = System.IO.Path.GetTempPath();
            return tmpPath + "sdcm.bin";
        }

        private static bool DeleteCredential()
        {
            System.IO.File.Delete(GetSdcmBinPath());
            return true;
        }

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
            AADAuthenticationOption = AADAuthenticationOption.ToLowerInvariant();
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

            string AccessTokenType = null, AccessToken = null;
            Uri restApi = new Uri(WebAPIUri, "/api/credentials");

            AccessToken = GetCredential();
            if (AccessToken != null)
            {
                ReturnList = await FetchList(restApi, AccessToken);
            }

            if (ReturnList == null)
            {
                DeleteCredential();
                try
                {
                    authResult = await authContext.AcquireTokenAsync(resource, clientID, redirectUri, platformParams);
                    AccessTokenType = authResult.AccessTokenType;
                    AccessToken = authResult.AccessToken;
                }
                catch (AdalException)
                {
                    retryAuth = true;
                    authResult = null;
                    AccessTokenType = null;
                    AccessToken = null;
                }

                if (retryAuth)
                {
                    try
                    {
                        authResult = await authContext.AcquireTokenAsync(resource, clientID, redirectUri, new PlatformParameters(PromptBehavior.Auto));
                        AccessTokenType = authResult.AccessTokenType;
                        AccessToken = authResult.AccessToken;
                    }
                    catch (AdalException)
                    {
                        authResult = null;
                        AccessTokenType = null;
                        AccessToken = null;
                    }
                }

                if (AccessToken != null)
                {
                    SetCredential(AccessToken);
                    ReturnList = await FetchList(restApi, AccessToken);
                }
            }

            return ReturnList;
        }

        private static async Task<List<AuthorizationHandlerCredentials>> FetchList(Uri restApi, string AccessToken)
        {
            List<AuthorizationHandlerCredentials> ReturnList = null;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                HttpResponseMessage infoResult = await client.GetAsync(restApi);

                string content = await infoResult.Content.ReadAsStringAsync();

                if (infoResult.IsSuccessStatusCode)
                {
                    ReturnList = JsonConvert.DeserializeObject<List<AuthorizationHandlerCredentials>>(content);
                }
            }

            return ReturnList;
        }
    }
}
