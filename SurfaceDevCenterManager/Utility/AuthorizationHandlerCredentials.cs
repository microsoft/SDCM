/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;

namespace SurfaceDevCenterManager.Utility
{
    internal class AuthorizationHandlerCredentials
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("url")]
        public System.Uri Url { get; set; }

        [JsonProperty("urlPrefix")]
        public System.Uri UrlPrefix { get; set; }
    }
}
