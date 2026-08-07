/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager;
using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;
using Microsoft.Extensions.Options;
using SurfaceDevCenterManager.Configuration;

namespace SurfaceDevCenterManager.Services;

public sealed class DevCenterHandlerFactory(
    ICredentialsProvider credentialsProvider,
    IAadTokenProvider tokenProvider,
    IOptions<DevCenterAppOptions> appOptions,
    RunContext runContext) : IDevCenterHandlerFactory
{
    public Task<IDevCenterHandler> CreateAsync(
        string profileName, AuthMode authMode, AadPromptMode promptMode, uint httpTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        ResolvedCredentials resolved = credentialsProvider.Resolve(profileName, authMode);

        DevCenterOptions options = new()
        {
            CorrelationId = runContext.CorrelationId,
            HttpTimeoutSeconds = httpTimeoutSeconds,
            RequestDelayMs = 250,
            LastCommand = runContext.SetLastCommand
        };

        if (resolved.LibraryCredentials != null)
        {
            return Task.FromResult<IDevCenterHandler>(new DevCenterHandler(resolved.LibraryCredentials, options));
        }

        IDevCenterHandler handler = new InteractiveDevCenterHandler(
            tokenProvider,
            resolved.Profile.ClientId!,
            resolved.Authority,
            appOptions.Value.RedirectUri,
            resolved.Url,
            resolved.UrlPrefix,
            promptMode,
            options);

        return Task.FromResult(handler);
    }
}
