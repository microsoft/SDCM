/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;

namespace SurfaceDevCenterManager.DevCenterApi
{
    public class DevCenterErrorReturn
    {
        [JsonProperty("error")]
        public DevCenterErrorDetails Error { get; set; }

        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
