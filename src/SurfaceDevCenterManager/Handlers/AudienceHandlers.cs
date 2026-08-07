/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;
using SurfaceDevCenterManager.Cli;
using SurfaceDevCenterManager.Services;

namespace SurfaceDevCenterManager.Handlers;

public sealed record AudienceListInput(GlobalInvocationOptions Global);

public sealed class AudienceListHandler(IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(AudienceListInput input, CancellationToken cancellationToken)
    {
        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<Audience> response = await api.GetAudiences().ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                output.Results(response.ReturnValue ?? [], a => a.Dump());
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "audience list");
            }
        }, cancellationToken);
    }
}
