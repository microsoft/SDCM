/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;

namespace SurfaceDevCenterManager.Services;

/// <summary>
///     Builds an <see cref="IDevCenterHandler" /> for a given profile/auth mode. Credential
///     acquisition (particularly interactive sign-in) is inherently asynchronous, so this is an
///     async factory rather than a plain DI registration; callers are responsible for disposing the
///     returned handler.
/// </summary>
public interface IDevCenterHandlerFactory
{
    Task<IDevCenterHandler> CreateAsync(
        string profileName, AuthMode authMode, AadPromptMode promptMode, uint httpTimeoutSeconds,
        CancellationToken cancellationToken);
}
