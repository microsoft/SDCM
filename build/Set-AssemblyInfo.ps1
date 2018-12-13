#-------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation.  All rights reserved.
# Licensed under the MIT license.  See LICENSE file in the project root for full license information.
#-------------------------------------------------------------------------------
<#
.SYNOPSIS
    Script to set the AssemblyInfo.cs version information

.PARAMETER BuildVersion
    New version to use
#>
#Requires -Version 5.0

param(
 [Parameter(Mandatory=$true,Position=0)]
 [string] $BuildVersion,

 [Parameter(Mandatory=$true,Position=1)]
 [string] $AssemblyInfoFile
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

###################################################################################################
# Functions
###################################################################################################

###################################################################################################
# Main
###################################################################################################
#$AssemblyInfoFile = "$PSScriptRoot\..\SurfaceDevCenterManager\Properties\AssemblyInfo.cs"

"Open: $AssemblyInfoFile" | Write-Output
$AssemblyInfo = gc $AssemblyInfoFile


"Set Version: $BuildVersion" | Write-Output
$BuildVersion = "`"$BuildVersion`""
$NewAssemblyInfo = $AssemblyInfo -replace '\"1.0.0.0\"',$BuildVersion

"Replace: $AssemblyInfoFile" | Write-Output
$NewAssemblyInfo | Out-File -Encoding ASCII -FilePath $AssemblyInfoFile

