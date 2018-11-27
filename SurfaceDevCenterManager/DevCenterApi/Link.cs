/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using System;

namespace SurfaceDevCenterManager.DevCenterApi
{
    public class Link
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        public void Dump()
        {
            Console.WriteLine("               - href:   " + Href);
            Console.WriteLine("               - method: " + Method);
            Console.WriteLine("               - rel:    " + Rel);
        }
    }

}
