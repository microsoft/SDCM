/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SurfaceDevCenterManager.DevCenterApi
{
    public class WorkflowStatus
    {
        [JsonProperty("currentStep")]
        public string CurrentStep { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("messages")]
        public List<string> Messages { get; set; }

        [JsonProperty("errorReport")]
        public string ErrorReport { get; set; }

        public async Task<bool> Dump()
        {
            bool retval = true;

            Console.WriteLine("> Step:  {0}", CurrentStep);
            Console.WriteLine("> State: {0}", State);
            if (Messages != null)
            {
                foreach (string msg in Messages)
                {
                    Console.WriteLine("> Message: {0}", msg);
                }
            }
            if (ErrorReport != null)
            {
                Console.WriteLine("> Error Report:");
                string tmpfile = System.IO.Path.GetTempFileName();
                Utility.BlobStorageHandler bsh = new Utility.BlobStorageHandler(ErrorReport);
                retval = await bsh.Download(tmpfile);

                string errorContent = System.IO.File.ReadAllText(tmpfile);
                Console.WriteLine(errorContent);
                System.IO.File.Delete(tmpfile);
            }
            return retval;
        }
    }
}
