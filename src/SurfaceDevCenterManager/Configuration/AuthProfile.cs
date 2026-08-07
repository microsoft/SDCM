/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager.Configuration;

/// <summary>
///     A single named credential profile, as stored under "profiles" in authconfig.json or bound
///     from SDCM_PROFILES__&lt;name&gt;__* environment variables.
/// </summary>
public sealed class AuthProfile
{
    public string? TenantId { get; set; }

    public string? ClientId { get; set; }

    /// <summary>Client secret, used for the "client-secret" auth mode.</summary>
    public string? Key { get; set; }

    /// <summary>User-assigned managed identity client id, used for the "managed-identity" auth mode.</summary>
    public string? ManagedIdentityClientId { get; set; }

    public string? Url { get; set; }

    public string? UrlPrefix { get; set; }

    public string? Authority { get; set; }

    public bool IsPlaceholder()
    {
        return string.IsNullOrWhiteSpace(TenantId)
               || string.IsNullOrWhiteSpace(ClientId)
               || TenantId == "00000000-0000-0000-0000-000000000000"
               || ClientId == "00000000-0000-0000-0000-000000000000";
    }
}
