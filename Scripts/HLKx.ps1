#-------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation and Contributors. All rights reserved.
# Licensed under the MIT license.  See LICENSE file in the project root for full license information.
#-------------------------------------------------------------------------------
<#
.SYNOPSIS
    Script to use Surface Dev Center Manager to get a WHQL signed driver from a HLKx package

.PARAMETER ProductName
    Product Name to use for the driver, visible in Hardware Dev Center

.PARAMETER Signatures
    OS Version and Architecture to submit the driver for

.PARAMETER InputFile
    Path to the EV-signed HLKx file needed for an WHQL-signed driver
    See steps here:
    https://docs.microsoft.com/en-us/windows-hardware/test/hlk/user/digitally-sign-an-hlkx-package

.NOTES
    Requires the sdcm dotnet tool to be installed and on PATH:
      dotnet tool install -g Nefarius.Tools.SDCM
#>
#Requires -Version 7.0

param(
  [Parameter(Mandatory = $true, Position = 0)]
  [string] $ProductName,

  [Parameter(Mandatory = $true, Position = 1)]
  [ValidateSet("WINDOWS_v100_X64_RS3_FULL", "WINDOWS_v100_X64_RS4_FULL")]
  [string[]] $Signatures,

  [Parameter(Mandatory = $true, Position = 2)]
  [ValidateScript( { Test-Path -Path $_ -PathType Leaf })]
  [string] $InputFile
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

Write-Output "HLK Submission"
Write-Output ""

Write-Output "> Create Product"
$product = [ordered]@{
  productName          = "$($ProductName)_HLK"
  testHarness          = "HLK"
  announcementDate     = (Get-Date).AddDays(7).ToString("s")
  firmwareVersion      = "0"
  deviceType           = "external"
  isTestSign           = $false
  isFlightSign         = $false
  selectedProductTypes = @{ windows_v100_RS4 = "Unclassified" }
  requestedSignatures  = $Signatures
}
$product | ConvertTo-Json | Out-File -Encoding utf8 -FilePath "CreateHLK.json"
$productResult = Invoke-Sdcm product create --input "CreateHLK.json" | ConvertFrom-Json
$SdcmProductId = $productResult.id
Write-Output "    * ProductId: $SdcmProductId"

Write-Output "> Create Submission"
$submission = [ordered]@{
  name = "$($ProductName)_HLK_Submission"
  type = "initial"
}
$submission | ConvertTo-Json | Out-File -Encoding utf8 -FilePath "CreateSubmissionHLK.json"
$submissionResult = Invoke-Sdcm submission create --product-id $SdcmProductId --input "CreateSubmissionHLK.json" | ConvertFrom-Json
$SdcmSubmissionId = $submissionResult.id
Write-Output "    * SubmissionId: $SdcmSubmissionId"

Write-Output "> Upload File"
Invoke-Sdcm submission upload --product-id $SdcmProductId --submission-id $SdcmSubmissionId --package $InputFile

Write-Output "> Commit Submission"
Invoke-Sdcm submission commit --product-id $SdcmProductId --submission-id $SdcmSubmissionId

Write-Output "> Wait for Submission to complete"
Write-Output "    * Dev Center URL: https://developer.microsoft.com/en-us/dashboard/hardware/driver/$SdcmProductId"
Write-Output "    * ProductId: $SdcmProductId"
Write-Output "    * SubmissionId: $SdcmSubmissionId"
Invoke-Sdcm submission wait --product-id $SdcmProductId --submission-id $SdcmSubmissionId

Write-Output "> Download File"
$signedPackagePath = "$InputFile.signed.zip"
Invoke-Sdcm submission download --product-id $SdcmProductId --submission-id $SdcmSubmissionId --output-file $signedPackagePath

Write-Output "> Done"
Write-Output "    * Output: $signedPackagePath"
