/*++
    Copyright (c) Microsoft Corporation. All rights reserved.

    Licensed under the MIT license.  See LICENSE file in the project root for full license information.  
--*/
using System;
using System.Collections.Generic;

namespace SurfaceDevCenterManager.DevCenterAPI
{
    public class DriverMetadataHWID
    {
        public string Manufacturer { get; set; }
        public string DeviceDescription { get; set; }
        public string FeatureScore { get; set; }
    }

    public class DriverMetadataInfDetails
    {
        public string DriverPackageFamilyId { get; set; }
        public string InfClass { get; set; }
        public string DriverVersion { get; set; }
        public DateTime DriverDate { get; set; }
        public string ExtensionId { get; set; }
        public string Provider { get; set; }
        public string ClassGuid { get; set; }
        public List<object> InstallationComputerHardwareIds { get; set; }
        public Dictionary<string, Dictionary<string, DriverMetadataHWID>> OSPnPInfoMap { get; set; }
    }

    public class DriverMetadataDetails
    {
        public string Locales { get; set; }
        public Dictionary<string, DriverMetadataInfDetails> InfInfoMap { get; set; }
    }

    public class DriverMetadata
    {
        public Dictionary<string, DriverMetadataDetails> BundleInfoMap { get; set; }
    }
}
