/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SurfaceDevCenterManager.DevCenterApi
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

    public enum InServicePublishInfoOSEnum
    {
        TH,
        RS1,
        RS2,
        RS3,
        RS4,
        RS5,
        RS6
    }

    public class InServicePublishInfo
    {
        [JsonProperty("flooring")]
        public string Flooring { get; set; }
        [JsonProperty("ceiling")]
        public string Ceiling { get; set; }
    }

    public class Targeting
    {
        [JsonProperty("hardwareIds")]
        public List<HardwareId> HardwareIds { get; set; }

        [JsonProperty("chids")]
        public List<CHID> Chids { get; set; }

        [JsonProperty("restrictedToAudiences")]
        public List<string> RestrictedToAudiences { get; set; }

        [JsonProperty("inServicePublishInfo")]
        public InServicePublishInfo InServicePublishInfo { get; set; }
    }

    public class RecipientSpecifications
    {
        [JsonProperty("receiverPublisherId")]
        public string ReceiverPublisherId { get; set; }
        [JsonProperty("enforceChidTargeting")]
        public bool EnforceChidTargeting { get; set; }
    }

    public class NewShippingLabel
    {
        [JsonProperty("publishingSpecifications")]
        public PublishingSpecifications PublishingSpecifications { get; set; }

        [JsonProperty("targeting")]
        public Targeting Targeting { get; set; }

        [JsonProperty("recipientSpecifications")]
        public RecipientSpecifications RecipientSpecifications { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("destination")]
        public string Destination { get; set; }
    }

    public class ShippingLabel : IArtifact
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

        public async void Dump()
        {
            Console.WriteLine("---- Shipping Label: " + Id);
            Console.WriteLine("         Name:        " + Name);
            Console.WriteLine("         ProductId:   " + ProductId);
            Console.WriteLine("         SubmissionId:" + SubmissionId);

            Console.WriteLine("         Publishing Specifications:");
            if (PublishingSpecifications != null)
            {
                Console.WriteLine("           publishToWindows10s:" + PublishingSpecifications.PublishToWindows10S);
                Console.WriteLine("           isDisclosureRestricted:" + PublishingSpecifications.IsDisclosureRestricted);
                Console.WriteLine("           isAutoInstallOnApplicableSystems:" + PublishingSpecifications.IsAutoInstallOnApplicableSystems);
                Console.WriteLine("           isAutoInstallDuringOSUpgrade:" + PublishingSpecifications.IsAutoInstallDuringOSUpgrade);
                Console.WriteLine("           goLiveDate:" + PublishingSpecifications.GoLiveDate);
                Console.WriteLine("           additionalInfoForMsApproval:");
                Console.WriteLine("               businessJustification:" + PublishingSpecifications.AdditionalInfoForMsApproval.BusinessJustification);
                Console.WriteLine("               hasUiSoftware:" + PublishingSpecifications.AdditionalInfoForMsApproval.HasUiSoftware);
                Console.WriteLine("               isCoEngineered:" + PublishingSpecifications.AdditionalInfoForMsApproval.IsCoEngineered);
                Console.WriteLine("               isForUnreleasedHardware:" + PublishingSpecifications.AdditionalInfoForMsApproval.IsForUnreleasedHardware);
                Console.WriteLine("               isRebootRequired:" + PublishingSpecifications.AdditionalInfoForMsApproval.IsRebootRequired);
                Console.WriteLine("               microsoftContact:" + PublishingSpecifications.AdditionalInfoForMsApproval.MicrosoftContact);
                Console.WriteLine("               validationsPerformed:" + PublishingSpecifications.AdditionalInfoForMsApproval.ValidationsPerformed);
                Console.WriteLine("               affectedOems:");
                foreach (string oem in PublishingSpecifications.AdditionalInfoForMsApproval.AffectedOems)
                {
                    Console.WriteLine("                            " + oem);
                }
            }

            Console.WriteLine("         Links:");
            if (Links != null)
            {
                foreach (Link link in Links)
                {
                    link.Dump();
                }
            }
            Console.WriteLine("         Status:");
            if (WorkflowStatus != null)
            {
                await WorkflowStatus.Dump();
            }
            Console.WriteLine();
        }
    }
}
