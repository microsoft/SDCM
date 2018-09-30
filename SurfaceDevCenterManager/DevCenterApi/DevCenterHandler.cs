/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using SurfaceDevCenterManager.Utility;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SurfaceDevCenterManager.DevCenterAPI
{
    //Reference Link:
    //https://docs.microsoft.com/en-us/windows/uwp/monetize/access-analytics-data-using-windows-store-services#prerequisites
    internal sealed class DevCenterHandler : IDisposable
    {
        private readonly DelegatingHandler AuthHandler;
        private readonly AuthorizationHandlerCredentials AuthCredentials;

        public DevCenterHandler(AuthorizationHandlerCredentials credentials)
        {
            AuthCredentials = credentials;
            AuthHandler = new AuthorizationHandler(AuthCredentials);
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

        private const string DevCenterProductsUrl = "/hardware/products";
        public async Task<DevCenterResponse<Product>> NewProduct(NewProduct input)
        {
            DevCenterResponse<Product> retval = null;
            string NewProductsUrl = GetDevCenterBaseUrl() + DevCenterProductsUrl;

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(NewProductsUrl);

                string json = JsonConvert.SerializeObject(input);
                StringContent postcontent = new StringContent(json,
                                                              System.Text.Encoding.UTF8,
                                                              "application/json");

                HttpResponseMessage infoResult = await client.PostAsync(restApi, postcontent);

                string content = await infoResult.Content.ReadAsStringAsync();

                retval = new DevCenterResponse<Product>();
                if (infoResult.IsSuccessStatusCode)
                {
                    Product ret = JsonConvert.DeserializeObject<Product>(content);
                    if (ret.Id != null)
                    {
                        retval.ReturnValue = new List<Product>
                        {
                            ret
                        };
                    }
                }
                else
                {
                    retval.Error = DeserializeDevCenterError(content);
                }
            }

            return retval;
        }

        public async Task<DevCenterResponse<Product>> GetProducts(string ProductId = null)
        {
            DevCenterResponse<Product> retval = null;
            string GetProductsUrl = GetDevCenterBaseUrl() + DevCenterProductsUrl;

            if (ProductId != null)
            {
                GetProductsUrl += "/" + Uri.EscapeDataString(ProductId);
            }

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(GetProductsUrl);

                HttpResponseMessage infoResult = await client.GetAsync(restApi);

                string content = await infoResult.Content.ReadAsStringAsync();

                retval = new DevCenterResponse<Product>();
                if (infoResult.IsSuccessStatusCode)
                {
                    //dynamic jObj = JsonConvert.DeserializeObject(content);
                    if (ProductId != null)
                    {
                        Product ret = JsonConvert.DeserializeObject<Product>(content);
                        if (ret.Id != null)
                        {
                            retval.ReturnValue = new List<Product>
                            {
                                ret
                            };
                        }
                    }
                    else
                    {
                        Response<Product> ret = JsonConvert.DeserializeObject<Response<Product>>(content);
                        retval.ReturnValue = ret.Value;
                    }
                }
                else
                {
                    retval.Error = DeserializeDevCenterError(content);
                }
            }

            return retval;
        }

        private const string DevCenterProductSubmissionUrl = "/hardware/products/{0}/submissions";
        public async Task<DevCenterResponse<Submission>> NewProductSubmission(string ProductId, NewSubmission submissionInfo)
        {
            DevCenterResponse<Submission> retval = null;
            string NewProductSubmissionUrl = GetDevCenterBaseUrl() + string.Format(DevCenterProductSubmissionUrl, Uri.EscapeDataString(ProductId));

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(NewProductSubmissionUrl);

                string json = JsonConvert.SerializeObject(submissionInfo);
                StringContent postcontent = new StringContent(json,
                                                              System.Text.Encoding.UTF8,
                                                              "application/json");

                HttpResponseMessage infoResult = await client.PostAsync(restApi, postcontent);

                string content = await infoResult.Content.ReadAsStringAsync();

                retval = new DevCenterResponse<Submission>();
                if (infoResult.IsSuccessStatusCode)
                {
                    Submission ret = JsonConvert.DeserializeObject<Submission>(content);
                    if (ret.Id != null)
                    {
                        retval.ReturnValue = new List<Submission>
                        {
                            ret
                        };
                    }
                }
                else
                {
                    retval.Error = DeserializeDevCenterError(content);
                }

            }

            return retval;
        }

        public async Task<DevCenterResponse<Submission>> GetProductSubmission(string ProductId, string SubmissionId = null)
        {
            DevCenterResponse<Submission> retval = null;
            string GetProductSubmissionUrl = GetDevCenterBaseUrl() + string.Format(DevCenterProductSubmissionUrl, Uri.EscapeDataString(ProductId));

            if (SubmissionId != null)
            {
                GetProductSubmissionUrl += "/" + Uri.EscapeDataString(SubmissionId);
            }

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(GetProductSubmissionUrl);

                HttpResponseMessage infoResult = await client.GetAsync(restApi);

                string content = await infoResult.Content.ReadAsStringAsync();

                retval = new DevCenterResponse<Submission>();
                if (infoResult.IsSuccessStatusCode)
                {
                    //dynamic jObj = JsonConvert.DeserializeObject(content);
                    if (SubmissionId != null)
                    {
                        Submission ret = JsonConvert.DeserializeObject<Submission>(content);
                        if (ret.Id != null)
                        {
                            retval.ReturnValue = new List<Submission>
                            {
                                ret
                            };
                        }
                    }
                    else
                    {
                        Response<Submission> ret = JsonConvert.DeserializeObject<Response<Submission>>(content);
                        retval.ReturnValue = ret.Value;
                    }
                }
                else
                {
                    retval.Error = DeserializeDevCenterError(content);
                }
            }

            return retval;
        }

        public async Task<bool> CommitProductSubmission(string ProductId, string SubmissionId)
        {
            bool retval = false;
            string CommitProductSubmissionUrl = GetDevCenterBaseUrl() + string.Format(DevCenterProductSubmissionUrl, Uri.EscapeDataString(ProductId)) +
                                                "/" + Uri.EscapeDataString(SubmissionId) + "/commit";

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(CommitProductSubmissionUrl);

                StringContent postcontent = new StringContent("{}",
                                                              System.Text.Encoding.UTF8,
                                                              "application/json");

                HttpResponseMessage infoResult = await client.PostAsync(restApi, postcontent);

                string content = await infoResult.Content.ReadAsStringAsync();

                retval = infoResult.IsSuccessStatusCode;
            }

            return retval;
        }

        private const string DevCenterShippingLabelUrl = "/hardware/products/{0}/submissions/{1}/shippingLabels";

        public async Task<DevCenterResponse<ShippingLabel>> NewShippingLabel(string ProductId, string SubmissionId, NewShippingLabel shippingLabelInfo)
        {
            DevCenterResponse<ShippingLabel> retval = null;
            string ShippingLabelUrl = GetDevCenterBaseUrl() + string.Format(DevCenterShippingLabelUrl, Uri.EscapeDataString(ProductId), Uri.EscapeDataString(SubmissionId));

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(ShippingLabelUrl);

                string json = JsonConvert.SerializeObject(shippingLabelInfo);
                StringContent postcontent = new StringContent(json,
                                                              System.Text.Encoding.UTF8,
                                                              "application/json");

                HttpResponseMessage infoResult = await client.PostAsync(restApi, postcontent);

                string content = await infoResult.Content.ReadAsStringAsync();

                retval = new DevCenterResponse<ShippingLabel>();
                if (infoResult.IsSuccessStatusCode)
                {
                    ShippingLabel ret = JsonConvert.DeserializeObject<ShippingLabel>(content);
                    if (ret.Id != null)
                    {
                        retval.ReturnValue = new List<ShippingLabel>
                        {
                            ret
                        };
                    }
                }
                else
                {
                    retval.Error = DeserializeDevCenterError(content);
                }
            }

            return retval;
        }

        public async Task<DevCenterResponse<ShippingLabel>> GetShippingLabels(string ProductId, string SubmissionId, string ShippingLabelId = null)
        {
            DevCenterResponse<ShippingLabel> retval = null;
            string GetShippingLabelUrl = GetDevCenterBaseUrl() + string.Format(DevCenterShippingLabelUrl, Uri.EscapeDataString(ProductId), Uri.EscapeDataString(SubmissionId));

            if (ShippingLabelId != null)
            {
                GetShippingLabelUrl += "/" + Uri.EscapeDataString(ShippingLabelId);
            }

            GetShippingLabelUrl += @"?includeTargetingInfo=true";

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(GetShippingLabelUrl);

                HttpResponseMessage infoResult = await client.GetAsync(restApi);

                string content = await infoResult.Content.ReadAsStringAsync();

                retval = new DevCenterResponse<ShippingLabel>();
                if (infoResult.IsSuccessStatusCode)
                {
                    //dynamic jObj = JsonConvert.DeserializeObject(content);
                    if (ShippingLabelId != null)
                    {
                        ShippingLabel ret = JsonConvert.DeserializeObject<ShippingLabel>(content);
                        retval.ReturnValue = new List<ShippingLabel>
                        {
                            ret
                        };
                    }
                    else
                    {
                        Response<ShippingLabel> ret = JsonConvert.DeserializeObject<Response<ShippingLabel>>(content);
                        retval.ReturnValue = ret.Value;
                    }
                }
                else
                {
                    retval.Error = DeserializeDevCenterError(content);
                }
            }

            return retval;
        }

        private DevCenterErrorDetails DeserializeDevCenterError(string content)
        {
            DevCenterErrorReturn reterr = JsonConvert.DeserializeObject<DevCenterErrorReturn>(content);
            if (reterr.Error == null && reterr.StatusCode != null)
            {
                reterr.Error = new DevCenterErrorDetails()
                {
                    Code = reterr.StatusCode,
                    Message = reterr.Message
                };
            }
            return reterr.Error;
        }

        private const string DevCenterAudienceUrl = "/hardware/audiences";
        public async Task<DevCenterResponse<Audience>> GetAudiences()
        {
            DevCenterResponse<Audience> retval = null;
            string GetAudienceUrl = GetDevCenterBaseUrl() + DevCenterAudienceUrl;

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(GetAudienceUrl);

                HttpResponseMessage infoResult = await client.GetAsync(restApi);

                string content = await infoResult.Content.ReadAsStringAsync();

                retval = new DevCenterResponse<Audience>();
                if (infoResult.IsSuccessStatusCode)
                {
                    Response<Audience> ret = JsonConvert.DeserializeObject<Response<Audience>>(content);
                    retval.ReturnValue = ret.Value;
                }
                else
                {
                    retval.Error = DeserializeDevCenterError(content);
                }

            }

            return retval;
        }

        public void Dispose()
        {
            AuthHandler.Dispose();
        }
    }
}
