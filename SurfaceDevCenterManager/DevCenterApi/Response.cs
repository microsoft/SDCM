/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SurfaceDevCenterManager.DevCenterAPI
{
    public class Response<T>
    {
        [JsonProperty("value")]
        public List<T> Value { get; set; }

        [JsonProperty("links")]
        public List<Link> Links { get; set; }
    }
}
