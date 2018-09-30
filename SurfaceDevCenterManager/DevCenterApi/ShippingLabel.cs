/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SurfaceDevCenterManager.DevCenterAPI
{
    public class AdditionalInfoForMsApproval
    {
        [JsonProperty("microsoftContact")]
        public string MicrosoftContact { get; set; }

        [JsonProperty("validationsPerformed")]
        public string ValidationsPerformed { get; set; }

        [JsonProperty("affectedOems")]
        public List<string> AffectedOems { get; set; }

        [JsonProperty("isRebootRequired")]
        public bool IsRebootRequired { get; set; }

        [JsonProperty("isCoEngineered")]
        public bool IsCoEngineered { get; set; }

        [JsonProperty("isForUnreleasedHardware")]
        public bool IsForUnreleasedHardware { get; set; }

        [JsonProperty("hasUiSoftware")]
        public bool HasUiSoftware { get; set; }

        [JsonProperty("businessJustification")]
        public string BusinessJustification { get; set; }
    }

    public class PublishingSpecifications
    {
        [JsonProperty("goLiveDate")]
        public DateTime GoLiveDate { get; set; }

        [JsonProperty("visibleToAccounts")]
        public List<string> VisibleToAccounts { get; set; }

        [JsonProperty("isAutoInstallDuringOSUpgrade")]
        public bool IsAutoInstallDuringOSUpgrade { get; set; }

        [JsonProperty("isAutoInstallOnApplicableSystems")]
        public bool IsAutoInstallOnApplicableSystems { get; set; }

        [JsonProperty("isDisclosureRestricted")]
        public bool IsDisclosureRestricted { get; set; }

        [JsonProperty("publishToWindows10s")]
        public bool PublishToWindows10S { get; set; }

        [JsonProperty("additionalInfoForMsApproval")]
        public AdditionalInfoForMsApproval AdditionalInfoForMsApproval { get; set; }
    }

    public class HardwareId
    {

        [JsonProperty("bundleId")]
        public string BundleId { get; set; }

        [JsonProperty("infId")]
        public string InfId { get; set; }

        [JsonProperty("operatingSystemCode")]
        public string OperatingSystemCode { get; set; }

        [JsonProperty("pnpString")]
        public string PnpString { get; set; }
    }

    public class CHID
    {

        [JsonProperty("distributionState")]
        public string DistributionState { get; set; }

        [JsonProperty("chid")]
        public string Chid { get; set; }
    }

    public class Targeting
    {
        [JsonProperty("hardwareIds")]
        public List<HardwareId> HardwareIds { get; set; }

        [JsonProperty("chids")]
        public List<CHID> Chids { get; set; }

        [JsonProperty("restrictedToAudiences")]
        public List<string> RestrictedToAudiences { get; set; }
    }

    public class NewShippingLabel
    {
        [JsonProperty("publishingSpecifications")]
        public PublishingSpecifications PublishingSpecifications { get; set; }

        [JsonProperty("targeting")]
        public Targeting Targeting { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("destination")]
        public string Destination { get; set; }
    }

    public class ShippingLabel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("productId")]
        public string ProductId { get; set; }

        [JsonProperty("submissionId")]
        public string SubmissionId { get; set; }

        [JsonProperty("publishingSpecifications")]
        public PublishingSpecifications PublishingSpecifications { get; set; }

        [JsonProperty("workflowStatus")]
        public WorkflowStatus WorkflowStatus { get; set; }

        [JsonProperty("links")]
        public List<Link> Links;

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("destination")]
        public string Destination { get; set; }
    }

}
