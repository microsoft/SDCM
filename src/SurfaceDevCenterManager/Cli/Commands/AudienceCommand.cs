/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SurfaceDevCenterManager.Handlers;

namespace SurfaceDevCenterManager.Cli.Commands;

internal static class AudienceCommand
{
    public static Command Build(ServiceProviderAccessor accessor)
    {
        Command list = new("list", "List the audiences visible to this account");
        list.SetHandlerAction(
            accessor,
            (_, global) => new AudienceListInput(global),
            (sp, i, ct) => sp.GetRequiredService<AudienceListHandler>().RunAsync(i, ct));

        Command audience = new("audience", "Query audiences");
        audience.Subcommands.Add(list);
        return audience;
    }
}
