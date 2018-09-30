/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SurfaceDevCenterManager.DevCenterAPI
{
    public class WorkflowStatus
    {
        [JsonProperty("currentStep")]
        public string CurrentStep { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("messages")]
        public List<string> Messages { get; set; }

        //Not documented on MSDN but shows up when submissions fail
        [JsonProperty("errorReport")]
        public string ErrorReport { get; set; }
    }
}
