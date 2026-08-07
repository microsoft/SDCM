/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;
using SurfaceDevCenterManager.Cli;

namespace SurfaceDevCenterManager.Services;

public static class DevCenterHandlerFactoryExtensions
{
    /// <summary>
    ///     Creates a handler for the duration of <paramref name="action" /> and disposes it afterward.
    ///     <see cref="Microsoft.Devices.HardwareDevCenterManager.DevCenterApi.DevCenterHandler" /> is
    ///     <see cref="IDisposable" /> but <see cref="IDevCenterHandler" /> itself is not, so this is the
    ///     one place that needs to know that and cast.
    /// </summary>
    public static async Task<ExitCode> UseAsync(
        this IDevCenterHandlerFactory factory,
        GlobalInvocationOptions global,
        Func<IDevCenterHandler, Task<ExitCode>> action,
        CancellationToken cancellationToken)
    {
        return await factory.UseAsync(global, null, action, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Same as the two-argument overload, but also catches failures while acquiring credentials or
    ///     building the handler itself (an invalid/placeholder profile, an unreachable managed identity
    ///     endpoint, etc.) and reports them through <paramref name="output" /> as
    ///     <see cref="ExitCode.AuthenticationFailed" /> instead of letting them escape as an unhandled
    ///     exception.
    /// </summary>
    public static async Task<ExitCode> UseAsync(
        this IDevCenterHandlerFactory factory,
        GlobalInvocationOptions global,
        IOutputWriter? output,
        Func<IDevCenterHandler, Task<ExitCode>> action,
        CancellationToken cancellationToken)
    {
        IDevCenterHandler handler;
        try
        {
            handler = await factory
                .CreateAsync(global.Profile, global.Auth, global.Aad, global.TimeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (output != null)
        {
            output.Error(ex.Message);
            return ExitCode.AuthenticationFailed;
        }

        try
        {
            return await action(handler).ConfigureAwait(false);
        }
        finally
        {
            (handler as IDisposable)?.Dispose();
        }
    }
}
