/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SurfaceDevCenterManager.DevCenterAPI
{
    public class Download
    {
        [JsonProperty("items")]
        public List<Item> Items { get; set; }

        [JsonProperty("messages")]
        public List<string> Messages { get; set; }

        public class Item
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("url")]
            public System.Uri Url { get; set; }
        }

        public enum Type
        {
            initialPackage,
            signedPackage,
            certificationReport,
            driverMetadata,
            derivedPackage
        }

        public void Dump()
        {
            foreach (Item item in Items)
            {
                Console.WriteLine("               - url: " + item.Url);
                Console.WriteLine("               - type:" + item.Type);
            }
            Console.WriteLine("               - messages: ");

            if (Messages != null)
            {
                foreach (string msg in Messages)
                {
                    Console.WriteLine("                 " + msg);
                }
            }
        }
    }
}
