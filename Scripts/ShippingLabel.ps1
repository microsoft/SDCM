#-------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation and Contributors. All rights reserved.
# Licensed under the MIT license.  See LICENSE file in the project root for full license information.
#-------------------------------------------------------------------------------
<#
.SYNOPSIS
    Script to use Surface Dev Center Manager to create a Shipping Label from a Submission

.PARAMETER ProductName
    Product Name used to name the shipping label, visible in Hardware Dev Center

.PARAMETER ProductId
    Product ID of the Submission to make a Shipping Label for

.PARAMETER SubmissionId
    Submission ID of the Submission to make a Shipping Label for

.PARAMETER CHIDs
    Array of Computer Hardware IDs (CHIDs) to target the driver at a specific set of devices

.PARAMETER ManualAcquisition
    In PublishingSpecifications if isAutoInstallDuringOSUpgrade or isAutoInstallOnApplicableSystems is true, then ManualAcquisition must be false.
    If isAutoInstallDuringOSUpgrade and isAutoInstallOnApplicableSystems are both false, then ManualAcquisition must be true.

.PARAMETER Audiences
    Array of Audience IDs the publication should be restricted to

.PARAMETER Floor
    Lowest OS the driver is available for

.PARAMETER Ceiling
    Highest OS the driver is available for

.NOTES
    Requires the sdcm dotnet tool to be installed and on PATH:
      dotnet tool install -g Nefarius.Tools.SDCM

    Fixes two bugs present in the sdcm 1.x version of this script: $ProductName was used but never
    declared as a parameter, and the "manualAcquistion" JSON key was misspelled (the API expects
    "manualAcquisition") so it was silently ignored by the service.
#>
#Requires -Version 7.0

param(
  [Parameter(Mandatory = $true, Position = 0)]
  [string] $ProductName,

  [Parameter(Mandatory = $true, Position = 1)]
  [string] $ProductId,

  [Parameter(Mandatory = $true, Position = 2)]
  [string] $SubmissionId,

  [Parameter(Mandatory = $true, Position = 3)]
  [string[]] $CHIDs,

  [Parameter(Mandatory = $false, Position = 4)]
  [bool] $ManualAcquisition = $false,

  [Parameter(Mandatory = $false, Position = 5)]
  [string[]] $Audiences = @(),

  [Parameter(Mandatory = $false, Position = 6)]
  [string] $Floor = "19H1",

  [Parameter(Mandatory = $false, Position = 7)]
  [string] $Ceiling = "19H1"
)

###################################################################################################
# Globals
###################################################################################################
$global:ErrorActionPreference = "stop"
Set-StrictMode -Version Latest

function Invoke-Sdcm {
  & sdcm --output json @args
  if ($LASTEXITCODE -ne 0) {
    Write-Error "sdcm $($args -join ' ') failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
  }
}

###################################################################################################
# Main
###################################################################################################

Write-Output "Shipping Label"
Write-Output ""

Write-Output "> Wait for Driver Metadata to be ready"
Invoke-Sdcm submission wait --wait-metadata --product-id $ProductId --submission-id $SubmissionId

Write-Output "> Generate Shipping Label json"
$shippingLabel = [ordered]@{
  publishingSpecifications = [ordered]@{
    goLiveDate                       = "2018-10-02T00:00:00.000Z"
    visibleToAccounts                = @()
    isAutoInstallDuringOSUpgrade     = $true
    isAutoInstallOnApplicableSystems = $true
    manualAcquisition                = $ManualAcquisition
    isDisclosureRestricted           = $true
    publishToWindows10s              = $false
    additionalInfoForMsApproval      = [ordered]@{
      microsoftContact       = "contact@microsoft.com"
      validationsPerformed   = "TBD"
      affectedOems           = @("Your Company")
      isRebootRequired       = $true
      isCoEngineered         = $true
      isForUnreleasedHardware = $true
      hasUiSoftware           = $false
      businessJustification   = "Driver Update"
    }
  }
  targeting                = [ordered]@{
    hardwareIds = @(
      [ordered]@{
        bundleId            = "0"
        infId               = "empty.inf"
        operatingSystemCode = "WINDOWS_v100_RS4_FULL"
        pnpString           = "empty pnp"
      }
    )
    chids       = @($CHIDs | ForEach-Object { [ordered]@{ chid = $_; distributionState = "pendingAdd" } })
    restrictedToAudiences = $Audiences
    inServicePublishInfo  = [ordered]@{
      flooring = $Floor
      ceiling  = $Ceiling
    }
  }
  name                      = "$($ProductName)_ShippingLabel"
  destination               = "windowsUpdate"
}
$shippingLabel | ConvertTo-Json -Depth 10 | Tee-Object -FilePath "CreateShippingLabel.json" | Write-Output

Write-Output "> Create Shipping Label"
$shippingLabelResult = Invoke-Sdcm shipping-label create --product-id $ProductId --submission-id $SubmissionId --input "CreateShippingLabel.json" | ConvertFrom-Json
$ShippingLabelId = $shippingLabelResult.id
Write-Output "    * ShippingLabelId: $ShippingLabelId"

Write-Output "> Wait for Shipping Label"
Invoke-Sdcm shipping-label wait --product-id $ProductId --submission-id $SubmissionId --shipping-label-id $ShippingLabelId

Write-Output "> Done"
