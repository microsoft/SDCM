/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager.Services;

/// <summary>
///     Selects which kind of credential sdcm uses to talk to Hardware Dev Center. Where the
///     configuration for that credential comes from is a separate, orthogonal concern handled by the
///     layered configuration (appsettings.json, authconfig.json, SDCM_ environment variables).
/// </summary>
public enum AuthMode
{
    /// <summary>Chooses managed identity, then client secret, then interactive, based on what the selected profile configures.</summary>
    Auto,
    ManagedIdentity,
    ClientSecret,
    Interactive
}

/// <summary>Controls how aggressively interactive sign-in prompts the user, mirroring MSAL's <c>Prompt</c> options.</summary>
public enum AadPromptMode
{
    /// <summary>Only ever use a silently-cached token; never show UI.</summary>
    Never,

    /// <summary>Try silently first, then prompt with account selection if needed.</summary>
    Prompt,

    /// <summary>Always show the interactive prompt, forcing login.</summary>
    Always,

    /// <summary>Force a silent token refresh, then prompt with forced login if that fails.</summary>
    RefreshSession,

    /// <summary>Always show the account selection prompt.</summary>
    SelectAccount
}
