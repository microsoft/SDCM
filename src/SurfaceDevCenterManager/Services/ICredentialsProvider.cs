/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.Utility;
using SurfaceDevCenterManager.Configuration;

namespace SurfaceDevCenterManager.Services;

/// <summary>
///     Result of resolving a named profile and <see cref="AuthMode" /> into something that can build
///     an <see cref="Microsoft.Devices.HardwareDevCenterManager.DevCenterApi.IDevCenterHandler" />.
///     <see cref="LibraryCredentials" /> is set for every mode the underlying library supports
///     natively (managed identity, client secret); it is null for <see cref="AuthMode.Interactive" />,
///     which sdcm implements itself via <see cref="InteractiveDevCenterHandler" />.
/// </summary>
public sealed record ResolvedCredentials(
    AuthorizationHandlerCredentials? LibraryCredentials,
    AuthMode EffectiveMode,
    AuthProfile Profile,
    string Url,
    string UrlPrefix,
    string Authority);

public interface ICredentialsProvider
{
    ResolvedCredentials Resolve(string profileName, AuthMode mode);
}
