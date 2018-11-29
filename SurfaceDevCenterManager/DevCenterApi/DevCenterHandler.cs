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
    internal sealed class DevCenterHandler : IDisposable
    {
        private readonly DelegatingHandler AuthHandler;
        private readonly AuthorizationHandlerCredentials AuthCredentials;

        /// <summary>
        /// Creates a new DevCenterHandler using the provided credentials
        /// </summary>
        /// <param name="credentials">Authorization credentials for HWDC</param>
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

        /// <summary>
        /// Creates a new New Product in HWDC with the specified options
        /// </summary>
        /// <param name="input">Options for the new Product to be generated</param>
        /// <returns>Dev Center response with either an error or a Product if created successfully</returns>
        public async Task<DevCenterResponse<Product>> NewProduct(NewProduct input)
        {
            DevCenterResponse<Product> retval = null;
            string NewProductsUrl = GetDevCenterBaseUrl() + DevCenterProductsUrl;

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(NewProductsUrl);

                string json = JsonConvert.SerializeObject(input);
                StringContent postcontent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

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

        /// <summary>
        /// Gets a list of products or a specific product from HWDC
        /// </summary>
        /// <param name="ProductId">Gets all products if null otherwise retrieves the specified product</param>
        /// <returns>Dev Center response with either an error or a Product if queried successfully</returns>
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
        /// <summary>
        /// Creates a new Submission in HWDC with the specified options
        /// </summary>
        /// <param name="ProductId">Specifiy the Product ID for this Submission</param>
        /// <param name="submissionInfo">Options for the new Submission to be generated</param>
        /// <returns>Dev Center response with either an error or a Submission if created successfully</returns>
        public async Task<DevCenterResponse<Submission>> NewSubmission(string ProductId, NewSubmission submissionInfo)
        {
            DevCenterResponse<Submission> retval = null;
            string NewProductSubmissionUrl = GetDevCenterBaseUrl() + string.Format(DevCenterProductSubmissionUrl, Uri.EscapeDataString(ProductId));

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(NewProductSubmissionUrl);

                string json = JsonConvert.SerializeObject(submissionInfo);
                StringContent postcontent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

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

        /// <summary>
        /// Gets a list of submissions or a specific submission from HWDC
        /// </summary>
        /// <param name="ProductId">Specifiy the Product ID for this Submission</param>
        /// <param name="SubmissionId">Gets all submissions if null otherwise retrieves the specified submission</param>
        /// <returns>Dev Center response with either an error or a Submission if queried successfully</returns>
        public async Task<DevCenterResponse<Submission>> GetSubmission(string ProductId, string SubmissionId = null)
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

        private const string DevCenterPartnerSubmissionUrl = "/hardware/products/relationships/sourcepubliherid/{0}/sourceproductid/{1}/sourcesubmissionid/{2}";
        /// <summary>
        /// Gets shared submission info from a partner-shared Submission with partner ids
        /// </summary>
        /// <param name="PublisherId">Specifiy the Partner's Publisher ID for this Submission</param>
        /// <param name="ProductId">Specifiy the Partner's Product ID for this Submission</param>
        /// <param name="SubmissionId">Specifiy the Partner's Submission ID for this Submission</param>
        /// <returns>Dev Center response with either an error or a Submission if queried successfully with IDs for the querying account</returns>
        public async Task<DevCenterResponse<Submission>> GetPartnerSubmission(string PublisherId, string ProductId, string SubmissionId)
        {
            DevCenterResponse<Submission> retval = null;
            string GetProductSubmissionUrl = GetDevCenterBaseUrl() + string.Format(DevCenterPartnerSubmissionUrl, Uri.EscapeDataString(PublisherId), Uri.EscapeDataString(ProductId), Uri.EscapeDataString(SubmissionId));

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(GetProductSubmissionUrl);

                HttpResponseMessage infoResult = await client.GetAsync(restApi);

                string content = await infoResult.Content.ReadAsStringAsync();

                retval = new DevCenterResponse<Submission>();
                if (infoResult.IsSuccessStatusCode)
                {
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

        /// <summary>
        /// Commits a Submission in HWDC
        /// </summary>
        /// <param name="ProductId">Specifiy the Product ID for the Submission to commit</param>
        /// <param name="SubmissionId">Specifiy the Submission ID for the Submission to commit</param>
        /// <returns>Dev Center response with either an error or a true if comitted successfully</returns>
        public async Task<bool> CommitSubmission(string ProductId, string SubmissionId)
        {
            bool retval = false;
            string CommitProductSubmissionUrl = GetDevCenterBaseUrl() + string.Format(DevCenterProductSubmissionUrl, Uri.EscapeDataString(ProductId)) +
                                                "/" + Uri.EscapeDataString(SubmissionId) + "/commit";

            using (HttpClient client = new HttpClient(AuthHandler, false))
            {
                Uri restApi = new Uri(CommitProductSubmissionUrl);

                StringContent postcontent = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage infoResult = await client.PostAsync(restApi, postcontent);

                string content = await infoResult.Content.ReadAsStringAsync();

                retval = infoResult.IsSuccessStatusCode;
            }

            return retval;
        }

        private const string DevCenterShippingLabelUrl = "/hardware/products/{0}/submissions/{1}/shippingLabels";
        /// <summary>
        /// Creates a new Shipping Label in HWDC with the specified options
        /// </summary>
        /// <param name="ProductId">Specifiy the Product ID for this Shipping Label</param>
        /// <param name="SubmissionId">Specifiy the Submission ID for this Shipping Label</param>
        /// <param name="shippingLabelInfo">Options for the new Shipping Label to be generated</param>
        /// <returns>Dev Center response with either an error or a ShippingLabel if created successfully</returns>
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

        /// <summary>
        /// Gets a list of shipping labels or a specific shipping label from HWDC
        /// </summary>
        /// <param name="ProductId">Specifiy the Product ID for this Shipping Label</param>
        /// <param name="SubmissionId">Specifiy the Submission ID for this Shipping Label</param>
        /// <param name="ShippingLabelId">Gets all Shipping Labels if null otherwise retrieves the specified Shipping Label</param>
        /// <returns>Dev Center response with either an error or a ShippingLabel if queried successfully</returns>
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
        /// <summary>
        /// Gets a list of valid audiences from HWDC
        /// </summary>
        /// <returns>Dev Center response with either an error or a Audience if queried successfully</returns>
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
