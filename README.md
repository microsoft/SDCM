# Surface Dev Center Manager (SDCM)

Surface Dev Center Manager is a tool that utilizes the REST APIs made available by Microsoft Hardware Dev Center to automate many common tasks for hardware development and maintenance around driver and firmware management.

SDCM enables you to create Attestation and WHQL products, submissions, download the resulting signed packages and manage shipping labels to release software on Windows Update.

This tool is based on the
[Hardware Dev Center API](https://docs.microsoft.com/en-us/windows-hardware/drivers/dashboard/dashboard-api) and detailed options are available with the -?, -h or -help option at the command line.

# Getting Started
- Clone the repo
- Follow the steps here to [setup your app](https://docs.microsoft.com/en-us/windows-hardware/drivers/dashboard/dashboard-api#associate-an-azure-ad-application-with-your-windows-dev-center-account) to get credentials
- Edit authconfig.json to the appropriate values after your app was set up
    - Change clientId, tenantId and key to match the values from your app registration
    - You should not have to change the url or urlPrefix
- Build the project

# Input Json Formats
Please refer to the [Hardware Dev Center API](https://docs.microsoft.com/en-us/windows-hardware/drivers/dashboard/dashboard-api) for detailed information on each of the settings below.  This sample is targeted for a RS4 HLK submission and shipping label.

## Creating a Product
> {
>   "createType": "product",
>   "createProduct": {
>     "productName": "ProductName_HLK",
>     "testHarness": "HLK",
>     "selectedProductTypes": { "windows_v100_RS4": "Unclassified" },
>     "requestedSignatures": [ "WINDOWS_v100_X64_RS4_FULL" ],
>     "announcementDate": "2018-10-02T00:00:00",
>     "deviceType": "external",
>     "deviceMetaDataIds": null,
>     "firmwareVersion": "0",
>     "isTestSign": false,
>     "markettingNames": null,
>     "additionalAttributes": null
>   }
> }

For an Attestation submission, change testHarness to Attestation.

## Creating a Submission
> {
>   "createType": "submission",
>   "createSubmission": {
>     "name": "ProductName_HLK_Submission",
>     "type":  "initial"
>   }
> }

## Creating a Shipping Label
> {
>   "createType": "shippingLabel",
>   "createShippingLabel": {
>       "publishingSpecifications": {
>         "goLiveDate": "2018-10-02T00:00:00.000Z",
>         "visibleToAccounts": [],
>         "isAutoInstallDuringOSUpgrade": true,
>         "isAutoInstallOnApplicableSystems": true,
>         "isDisclosureRestricted": true,
>         "publishToWindows10s": false,
>         "additionalInfoForMsApproval": {
>           "microsoftContact": "contact@microsoft.com",
>           "validationsPerformed": "TBD",
>           "affectedOems": [
>             "Your Company"
>           ],
>           "isRebootRequired": true,
>           "isCoEngineered": true,
>           "isForUnreleasedHardware": true,
>           "hasUiSoftware": false,
>           "businessJustification": "Driver Update"
>         }
>       },
>     "targeting": {
>       "hardwareIds": [
>         {
>           "bundleId": "0",
>           "infId": "empty.inf",
>           "operatingSystemCode": "WINDOWS_v100_RS4_FULL",
>           "pnpString": "empty pnp"
>         }
>       ],
>       "chids": [
>         {
>           "chid": "guid"
>         }
>       ]
>     },
>       "name": "ProductName_HLK_ShippingLabel",
>       "destination": "windowsUpdate"
>   }
> }

Note that SDCM will auto-populate and publish all hardware IDs found in a Submission when creating a Shipping Label.

# Basic Operations
## Create a Product
- Create a json file 'Create_ProductName_HLK.json' using the Product json example above
> sdcm.exe -create Create_ProductName_HLK.json
- This will output a Product ID (PID) if successful
## List the Product
- Verify the product was created by listing the details.
> sdcm.exe -list product -productid PID
## List the Product
- Verify the product was created by listing the details.
> sdcm.exe -list product -productid PID
##Create a Submission
- Create a json file 'Create_ProductName_Submission_HLK.json' using the Submission json example above
> sdcm.exe -create Create_ProductName_Submission_HLK.json -productid PID
- This will output a Submission ID (SID) if successful
## List the Submission
- List all the submissions for the product
> sdcm.exe -list submission -productid PID
- List a specific submission for the product
> sdcm.exe -list submission -productid PID -submissionid SID
## Upload a package to a Submission
- Make sure the package (.cab or .hlkx) is signed by the EV Cert registered with your Hardware Dev Center Account
> sdcm.exe -upload test.hlkx -productid PID -submissionid SID
## Commit a Submission
- When everything is ready to start processing the submission, commit it
> sdcm.exe -commit -productid PID -submissionid SID
## Wait for a Submission to be Ready
> sdcm.exe -wait -productid PID -submissionid SID
## Download files from a Submission
> sdcm.exe -download hlksigned.zip -productid PID -submissionid SID

# WHQL signing a Driver
See HLKx.ps1 in the Scripts folder
# Attestation signing a Driver
See Attestation.ps1 in the Scripts folder
# Creating a Shipping Label
See ShippingLabel.ps1 in the Scripts folder


# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
















