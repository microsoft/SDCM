/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager.Configuration;

/// <summary>
///     Root of authconfig.json: a set of named credential profiles. Deliberately kept separate from
///     the library's AuthorizationHandlerCredentials type, which carries an X509Certificate2 property
///     that should never be routed through System.Text.Json deserialization.
/// </summary>
public sealed class AuthConfigEntry
{
    public Dictionary<string, AuthProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
