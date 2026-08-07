/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;
using SurfaceDevCenterManager.Cli.Commands;

namespace SurfaceDevCenterManager.Cli;

/// <summary>
///     Builds the full noun-verb command tree. Every leaf's action is attached here, even though the
///     dependency injection container it needs doesn't exist yet - see
///     <see cref="ServiceProviderAccessor" /> for why that ordering is safe.
/// </summary>
internal static class CommandTreeBuilder
{
    public static RootCommand Build(ServiceProviderAccessor accessor)
    {
        RootCommand root = new(
            "sdcm - automate Microsoft Hardware Dev Center driver and firmware submissions");

        GlobalOptions.AddTo(root);

        root.Subcommands.Add(ProductCommand.Build(accessor));
        root.Subcommands.Add(SubmissionCommand.Build(accessor));
        root.Subcommands.Add(ShippingLabelCommand.Build(accessor));
        root.Subcommands.Add(PartnerSubmissionCommand.Build(accessor));
        root.Subcommands.Add(AudienceCommand.Build(accessor));
        root.Subcommands.Add(ConfigCommand.Build(accessor));

        return root;
    }
}
