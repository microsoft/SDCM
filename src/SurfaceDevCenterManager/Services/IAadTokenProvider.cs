/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager.Services;

/// <summary>
///     Acquires an Azure AD access token for the interactively-signed-in user, for the
///     <see cref="AuthMode.Interactive" /> auth mode.
/// </summary>
public interface IAadTokenProvider
{
    Task<string> AcquireTokenAsync(
        string clientId,
        string authority,
        string redirectUri,
        string resource,
        AadPromptMode promptMode,
        CancellationToken cancellationToken);
}
