#-------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation.  All rights reserved.
# Licensed under the MIT license.  See LICENSE file in the project root for full license information.
#-------------------------------------------------------------------------------
<#
.SYNOPSIS
    Script to use Surface Dev Center Manager to create a Shipping Label from a Submission

.PARAMETER ProductId
    Product ID of the Submission to make a Shipping Label for

.PARAMETER SubmissionId
    Submission ID of the Submission to make a Shipping Label for

.PARAMETER CHIDs
    Array of Computer Hardware IDs (CHIDs) to target the driver at a specific set of devices

.PARAMETER IsManualAcquistion
    In PublishingSpecifications if isAutoInstallDuringOSUpgrade or isAutoInstallOnApplicableSystems is true, then ManualAcquistion must be false
    In PublishingSpecifications if isAutoInstallDuringOSUpgrade and isAutoInstallOnApplicableSystems are false, then ManualAcquistion must be true

.PARAMETER Audiences
    Array of Audience IDs the publication should be restricted to

.PARAMETER Floor
    Lowest OS the driver is available for

.PARAMETER Ceiling
    Highest OS the driver is available for
#>
#Requires -Version 5.0

param(
  [Parameter(Mandatory = $true, Position = 0)]
  [string] $ProductId,

  [Parameter(Mandatory = $true, Position = 1)]
  [string] $SubmissionId,

  [Parameter(Mandatory = $true, Position = 2)]
  [string[]] $CHIDs,

  [Parameter(Mandatory = $false, Position = 3)]
  [bool] $ManualAcquistion = $false,

  [Parameter(Mandatory = $false, Position = 4)]
  [string[]] $Audiences,

  [Parameter(Mandatory = $false, Position = 5)]
  [string] $Floor = "19H1",

  [Parameter(Mandatory = $false, Position = 6)]
  [string] $Ceiling = "19H1"
)

###################################################################################################
# Global Error Handler
###################################################################################################
trap {
  Write-Output "----- TRAP ----"
  Write-Output "Unhandled Exception: $($_.Exception.GetType().Name)"
  Write-Output $_.Exception
  $_ | Format-List -Force 
}

###################################################################################################
# Globals
###################################################################################################
$global:ErrorActionPreference = "stop"
Set-StrictMode -Version Latest
$SDCM = ".\sdcm.exe"

###################################################################################################
# Functions
###################################################################################################

###################################################################################################
# Main
###################################################################################################

Write-Output "Shipping Label"
Write-Output ""

Write-Output "> Wait for Driver Metadata to be ready"
& $SDCM -wait -waitmetadata -productid $ProductId -submissionid $SubmissionId

Write-Output "> Generate Shipping Label json"
$CreateShippingLabelJson = @"
{
  "createType": "shippingLabel",
  "createShippingLabel": {
      "publishingSpecifications": {
        "goLiveDate": "2018-10-02T00:00:00.000Z",
        "visibleToAccounts": [],
        "isAutoInstallDuringOSUpgrade": true,
        "isAutoInstallOnApplicableSystems": true,
        "manualAcquistion": $ManualAcquistion,
        "isDisclosureRestricted": true,
        "publishToWindows10s": false,
        "additionalInfoForMsApproval": {
          "microsoftContact": "contact@microsoft.com",
          "validationsPerformed": "TBD",
          "affectedOems": [
            "Your Company"
          ],
          "isRebootRequired": true,
          "isCoEngineered": true,
          "isForUnreleasedHardware": true,
          "hasUiSoftware": false,
          "businessJustification": "Driver Update"
        }
      },
    "targeting": {
      "hardwareIds": [
        {
          "bundleId": "0",
          "infId": "empty.inf",
          "operatingSystemCode": "WINDOWS_v100_RS4_FULL",
          "pnpString": "empty pnp"
        }
      ],
      "chids": [
        {
          "chid": "guid",
          "distributionState": "pendingAdd"
        }
      ],
      "restrictedToAudiences": $Audiences,
      "inServicePublishInfo": {
        "flooring": $Floor,
        "ceiling": $Ceiling
      }
    },
      "name": "$ProductName`_ShippingLabel",
      "destination": "windowsUpdate"
  }
}
"@
$CreateShippingLabelJson | Write-Host
$CreateShippingLabelJson | Out-File -encoding ASCII CreateShippingLabel.json

Write-Output "> Create Shipping Label"
$output = & $SDCM -create CreateShippingLabel.json -productid $ProductId -submissionid $SubmissionId
if (-not ([string]$output -match "---- Shipping Label: (\d+)")) {
  Write-Output "Did not find shipping label ID"
  Write-Output $output
  return -1
}
$ShippingLabelId = $Matches[1]
Write-Output "    * ShippingLabelId: $ShippingLabelId"

Write-Output "> Wait for Shipping Label"
& $SDCM -wait -productid $ProductId -submissionid $SubmissionId -shippinglabelid $ShippingLabelId

Write-Output "> Done"

