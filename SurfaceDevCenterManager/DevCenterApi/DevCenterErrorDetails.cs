/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SurfaceDevCenterManager.DevCenterApi
{
    public class DevCenterErrorDetails
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("validationErrors")]
        public List<DevCenterErrorValidationErrorEntry> ValidationErrors;

        [JsonProperty("httpErrorCode")]
        public int HttpErrorCode { get; set; }
    }

    public class DevCenterErrorValidationErrorEntry
    {
        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
