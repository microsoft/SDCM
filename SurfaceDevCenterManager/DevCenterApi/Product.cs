/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SurfaceDevCenterManager.DevCenterAPI
{
    public class Product
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("sharedProductId")]
        public string SharedProductId { get; set; }

        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("productType")]
        public string ProductType { get; set; }

        [JsonProperty("firmwareVersionid")]
        public string FirmwareVersion { get; set; }

        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }

        [JsonProperty("isTestSign")]
        public bool IsTestSign { get; set; }

        [JsonProperty("isFlightSign")]
        public bool IsFlightSign { get; set; }

        [JsonProperty("requestedSignatures")]
        public List<string> RequestedSignatures { get; set; }

        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }

        [JsonProperty("updatedBy")]
        public string UpdatedBy { get; set; }

        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        [JsonProperty("updatedDateTime")]
        public DateTime UpdatedDateTime { get; set; }

        [JsonProperty("announcementDate")]
        public DateTime AnnouncementDate { get; set; }

        [JsonProperty("deviceMetadataIds")]
        public List<string> DeviceMetadataIds { get; set; }

        [JsonProperty("marketingNames")]
        public List<string> MarketingNames { get; set; }

        [JsonProperty("testHarness")]
        public string TestHarness { get; set; }

        [JsonProperty("selectedProductTypes")]
        public Dictionary<string, string> SelectedProductTypes { get; set; }
    }

    public class NewProduct
    {
        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }

        [JsonProperty("requestedSignatures")]
        public List<string> RequestedSignatures { get; set; }

        [JsonProperty("announcementDate")]
        public DateTime AnnouncementDate { get; set; }

        [JsonProperty("deviceMetadataIds")]
        public List<string> DeviceMetadataIds { get; set; }

        [JsonProperty("firmwareVersion")]
        public string FirmwareVersion { get; set; }

        [JsonProperty("isTestSign")]
        public bool IsTestSign { get; set; }

        [JsonProperty("isFlightSign")]
        public bool IsFlightSign { get; set; }

        [JsonProperty("marketingNames")]
        public List<string> MarketingNames { get; set; }

        [JsonProperty("selectedProductTypes")]
        public Dictionary<string, string> SelectedProductTypes { get; set; }

        [JsonProperty("testHarness")]
        public string TestHarness { get; set; }
    }

}
