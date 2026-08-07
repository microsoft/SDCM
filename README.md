# Surface Dev Center Manager (SDCM)

Surface Dev Center Manager (SDCM) is a .NET tool that automates common Microsoft Hardware Dev Center
(Partner Center) tasks around driver and firmware submissions, using the
[Hardware Dashboard API](https://docs.microsoft.com/en-us/windows-hardware/drivers/dashboard/dashboard-api).

**sdcm** lets you create `Attestation` and `WHQL` products and submissions, upload and download
packages, and manage shipping labels to release drivers on Windows Update.

> This is `nefarius/SDCM`, a modernized fork of [`microsoft/SDCM`](https://github.com/microsoft/SDCM):
> .NET 10, dependency injection, `System.CommandLine`, MSAL instead of the now end-of-life ADAL, and
> a `dotnet tool` distribution. Backward compatibility with the original CLI was **not** a design
> goal here - see [Migrating from sdcm 1.x](#migrating-from-sdcm-1x) below if you're coming from the
> upstream tool.

<br/>

## Installation

Requires the [.NET 10 runtime](https://dotnet.microsoft.com/download) or later.

**As a global tool** (recommended - puts `sdcm` on your PATH):

```bash
dotnet tool install -g Nefarius.Tools.SDCM
sdcm --help
```

**As a local tool** (pinned per-repository via a tool manifest):

```bash
dotnet new tool-manifest # if you don't already have one
dotnet tool install Nefarius.Tools.SDCM
dotnet sdcm --help
```

To update: `dotnet tool update -g Nefarius.Tools.SDCM`.

<br/>

## Setting up credentials

1. Follow the steps in [Associate an Azure AD application with your Windows Dev Center account](https://docs.microsoft.com/en-us/windows-hardware/drivers/dashboard/dashboard-api#associate-an-azure-ad-application-with-your-windows-dev-center-account)
   to register an Azure AD application and grant it access to your Hardware Dev Center account.
   - If you intend to use `--auth interactive`, add `http://localhost` as a redirect URI on the app
     registration (System.CommandLine/MSAL's interactive flow uses a loopback listener).
2. Run `sdcm config init` to write a starter `authconfig.json` into your per-user config directory.
3. Edit the generated file's `default` profile:
   - `tenantId` / `clientId` - from your app registration
   - `key` - a client secret, if you'll use `--auth client-secret` (the default `auto` chain picks
     this automatically when set)
   - `managedIdentityClientId` - a user-assigned managed identity's client id, if running on Azure
     with `--auth managed-identity`
   - leave both blank to use `--auth interactive`, which opens a browser to sign in as a user
4. Run `sdcm config path` any time to see exactly which file sdcm resolved.

<br/>

## Configuration model

Config is layered, each layer overriding the previous:

1. `appsettings.json` (shipped with the tool) - non-secret HTTP/AAD defaults
2. `authconfig.json` - your named credential profiles (gitignored, never packed into the tool)
3. Environment variables prefixed `SDCM_` (double-underscore for nesting, e.g.
   `SDCM_PROFILES__DEFAULT__CLIENTID`)
4. Command-line options

`authconfig.json` is probed for, in order (first match wins):

1. An explicit `--config <path>`
2. The current working directory
3. The per-user config directory: `%APPDATA%\sdcm` on Windows, `$XDG_CONFIG_HOME/sdcm` (or
   `~/.config/sdcm`) elsewhere
4. The tool's own installation directory (for copy-deployed/self-contained builds)

Run `sdcm config path` to see this chain resolved for your machine, and `sdcm config init` to create
a starter file at the per-user location.

`authconfig.json` uses named profiles instead of the ordinal array sdcm 1.x used:

```json
{
  "profiles": {
    "default": {
      "tenantId": "00000000-0000-0000-0000-000000000000",
      "clientId": "00000000-0000-0000-0000-000000000000",
      "key": null,
      "managedIdentityClientId": null,
      "url": "https://manage.devcenter.microsoft.com",
      "urlPrefix": "v2.0/my"
    }
  }
}
```

Select a profile with `--profile <name>` (defaults to `default`).

<br/>

## Authentication

`--auth` selects the credential type:

| Value              | Behavior                                                                |
|--------------------|--------------------------------------------------------------------------|
| `auto` (default)   | Picks `managed-identity` if `managedIdentityClientId` is set, else `client-secret` if `key` is set, else `interactive` |
| `managed-identity` | Azure managed identity (requires `managedIdentityClientId` in the profile) |
| `client-secret`    | Azure AD app + client secret (requires `key` in the profile)             |
| `interactive`      | Interactive browser sign-in via MSAL, cached for reuse between runs      |

`--aad` controls how aggressively interactive sign-in prompts (only relevant with `--auth interactive`
or when `auto` falls back to it):

| Value                    | Behavior                                                       |
|--------------------------|------------------------------------------------------------------|
| `never` (default)        | Silent/cached only; fails if nothing is cached                   |
| `prompt`                 | Silent first, then an interactive account-selection prompt       |
| `always`                 | Always interactive, forcing login                                |
| `refresh-session`        | Force a silent token refresh, then interactive forced login if that fails |
| `select-account`         | Always show the account-selection prompt                         |

<br/>

## Command reference

```
sdcm
├─ product
│  ├─ create              --input <file>
│  └─ list                [--product-id <id>]
├─ submission
│  ├─ create              --product-id --input <file>
│  ├─ list                --product-id [--submission-id]
│  ├─ commit              --product-id --submission-id
│  ├─ upload              --product-id --submission-id --package <path>
│  ├─ download            --product-id --submission-id --output-file <path>
│  ├─ wait                --product-id --submission-id [--wait-metadata]
│  │                      [--poll-interval <sec>] [--wait-timeout <sec>]
│  └─ metadata
│     ├─ download         --product-id --submission-id --output-file <path>
│     └─ create           --product-id --submission-id
├─ shipping-label
│  ├─ create              --product-id --submission-id --input <file> [--partner-id]
│  ├─ list                --product-id --submission-id [--shipping-label-id]
│  └─ wait                --product-id --submission-id --shipping-label-id
│                         [--poll-interval <sec>] [--wait-timeout <sec>]
├─ partner-submission
│  ├─ list                --publisher-id --product-id --submission-id
│  └─ translate           --publisher-id --product-id --submission-id
├─ audience list
└─ config
   ├─ path
   └─ init                [--force]
```

Global options, valid anywhere in the tree: `--profile`, `--auth`, `--aad`, `--config`, `--timeout`
(HTTP timeout in seconds, default 300), `--output text|json` (default `text`), and `-v`/`--verbose`
(diagnostic logging on stderr).

Run `sdcm <command> --help` (or `sdcm <noun> <verb> --help`) for the full option list of any command.

<br/>

## Input file schema

`--input` takes the bare payload for the type being created - no wrapper object. This deserializes
directly into the underlying library's `NewProduct`, `NewSubmission` or `NewShippingLabel` types.

### Creating a product

```json
{
  "productName": "ProductName_HLK",
  "testHarness": "HLK",
  "announcementDate": "2023-01-01T00:00:00",
  "firmwareVersion": "0",
  "deviceType": "external",
  "isTestSign": false,
  "isFlightSign": false,
  "selectedProductTypes": {
    "windows_v100_RS4": "Unclassified"
  },
  "requestedSignatures": [
    "WINDOWS_v100_X64_RS4_FULL"
  ]
}
```

> For an Attestation submission, set `testHarness` to `Attestation`.

### Creating a submission

```json
{
  "name": "ProductName_HLK_Submission",
  "type": "initial"
}
```

### Creating a shipping label

```json
{
  "publishingSpecifications": {
    "goLiveDate": "2023-01-01T00:00:00.000Z",
    "visibleToAccounts": [],
    "isAutoInstallDuringOSUpgrade": true,
    "isAutoInstallOnApplicableSystems": true,
    "manualAcquisition": false,
    "isDisclosureRestricted": true,
    "publishToWindows10s": false,
    "additionalInfoForMsApproval": {
      "microsoftContact": "contact@microsoft.com",
      "validationsPerformed": "TBD",
      "affectedOems": ["Your Company"],
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
      { "chid": "guid", "distributionState": "pendingAdd" }
    ],
    "restrictedToAudiences": [],
    "inServicePublishInfo": { "flooring": "19H1", "ceiling": "19H1" }
  },
  "name": "ProductName_HLK_ShippingLabel",
  "destination": "windowsUpdate"
}
```

A file still using the old `{"createType": ..., "createProduct"/"createSubmission"/"createShippingLabel": {...}}`
envelope from sdcm 1.x fails fast with an explicit message pointing back to this section, instead of
a confusing null-reference deeper in the call stack.

<br/>

## Basic operations

Create a product:

```bash
sdcm product create --input product.json
```

Get it back by id, or list every product:

```bash
sdcm product list --product-id 12345
sdcm product list
```

Create and inspect a submission:

```bash
sdcm submission create --product-id 12345 --input submission.json
sdcm submission list --product-id 12345 --submission-id 67890
```

Upload the package (must be signed by the [Extended Validation Certificate (EV Cert)](https://docs.microsoft.com/en-us/windows-hardware/drivers/dashboard/get-a-code-signing-certificate)
registered on your Hardware account), commit, and wait for processing:

```bash
sdcm submission upload --product-id 12345 --submission-id 67890 --package test.hlkx
sdcm submission commit --product-id 12345 --submission-id 67890
sdcm submission wait --product-id 12345 --submission-id 67890
```

Download the signed result:

```bash
sdcm submission download --product-id 12345 --submission-id 67890 --output-file signed.zip
```

Add `--output json` to any command to get machine-readable results for scripting, instead of
regexing human-readable text:

```bash
$id = (sdcm product create --input product.json --output json | ConvertFrom-Json).id
```

<br/>

## Exit codes

| Code | Meaning                                                             |
|------|----------------------------------------------------------------------|
| 0    | Success                                                               |
| 1    | InvalidArguments - parse errors, malformed or missing `--input` file |
| 2    | AuthenticationFailed - no usable credentials, token acquisition failed |
| 3    | ApiRequestFailed - generic Hardware Dev Center API error             |
| 4    | NotFound - the requested entity doesn't exist                        |
| 5    | InvalidState - the request is invalid for the entity's current state |
| 6    | RateLimited - HTTP 429 from the service                              |
| 7    | WorkflowFailed - the submission or shipping label failed server-side |
| 8    | IoError - output path missing, or destination already exists         |
| 9    | Canceled - Ctrl+C, or a `--wait-timeout` was exceeded                |
| 10   | UnhandledException                                                    |

<br/>

## Automation scripts

The `Scripts/` folder has three ready-made end-to-end scripts, updated for the new CLI and requiring
`sdcm` to be installed and on `PATH`:

- [`Scripts/HLKx.ps1`](Scripts/HLKx.ps1) - WHQL-sign a driver from a signed HLKx package
- [`Scripts/Attestation.ps1`](Scripts/Attestation.ps1) - Attestation-sign a driver package
- [`Scripts/ShippingLabel.ps1`](Scripts/ShippingLabel.ps1) - create and wait on a shipping label

They use `--output json | ConvertFrom-Json` to pick up created ids and check `$LASTEXITCODE` after
every invocation, so a failed step stops the script instead of silently continuing (a bug in the
sdcm 1.x versions of these scripts).

<br/>

## Migrating from sdcm 1.x

| sdcm 1.x                                                | sdcm 2.x (this fork)                                             |
|----------------------------------------------------------|-------------------------------------------------------------------|
| `-create <product json>`                                 | `product create --input`                                          |
| `-create <submission json> -productid`                   | `submission create --product-id --input`                          |
| `-create <shippingLabel json> -productid -submissionid`  | `shipping-label create --product-id --submission-id --input`      |
| `-commit`                                                 | `submission commit`                                                |
| `-list product\|submission\|shippinglabel\|partnersubmission` | `product list` / `submission list` / `shipping-label list` / `partner-submission list` |
| `-upload`                                                 | `submission upload --package`                                     |
| `-download`                                               | `submission download --output-file`                               |
| `-metadata`                                               | `submission metadata download --output-file`                      |
| `-createmetadata`                                         | `submission metadata create`                                      |
| `-wait` (with/without `-shippinglabelid`)                | `submission wait` / `shipping-label wait`                          |
| `-a` / `-audience`                                        | `audience list`                                                    |
| `-translate`                                              | `partner-submission translate`                                    |
| `-partnerid`                                               | `shipping-label create --partner-id`                               |
| `-server <int>`                                            | `--profile <name>`                                                 |
| `-creds <mode>`                                            | `--auth <mode>`                                                    |

Also new:

- `--input` files no longer use the `{"createType": ..., "createXxx": {...}}` envelope - see
  [Input file schema](#input-file-schema).
- `authconfig.json` moved from an ordinal array to named `profiles`, and now lives in a per-user
  config directory by default rather than next to the executable - see
  [Configuration model](#configuration-model).
- `ErrorCodes` (48 negative values) was replaced by ten positive [exit codes](#exit-codes).
- `-v` used to be dead code; it now actually raises the log level.

<br/>

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
