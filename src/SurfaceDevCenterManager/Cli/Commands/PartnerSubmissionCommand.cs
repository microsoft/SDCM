/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SurfaceDevCenterManager.Handlers;

namespace SurfaceDevCenterManager.Cli.Commands;

internal static class PartnerSubmissionCommand
{
    public static Command Build(ServiceProviderAccessor accessor)
    {
        Option<string> publisherId = Opt.Str("--publisher-id", "The partner's publisher id", true);
        Option<string> productId = Opt.Str("--product-id", "The partner's product id", true);
        Option<string> submissionId = Opt.Str("--submission-id", "The partner's submission id", true);

        Command list = new("list", "Look up a partner's submission as it appears on this account");
        list.Options.Add(publisherId);
        list.Options.Add(productId);
        list.Options.Add(submissionId);
        list.SetHandlerAction(
            accessor,
            (pr, global) => new PartnerSubmissionListInput(
                pr.Required(publisherId), pr.Required(productId), pr.Required(submissionId), global),
            (sp, i, ct) => sp.GetRequiredService<PartnerSubmissionListHandler>().RunAsync(i, ct));

        Command translate = new("translate", "Translate a partner's product/submission id pair into this account's ids");
        translate.Options.Add(publisherId);
        translate.Options.Add(productId);
        translate.Options.Add(submissionId);
        translate.SetHandlerAction(
            accessor,
            (pr, global) => new PartnerSubmissionTranslateInput(
                pr.Required(publisherId), pr.Required(productId), pr.Required(submissionId), global),
            (sp, i, ct) => sp.GetRequiredService<PartnerSubmissionTranslateHandler>().RunAsync(i, ct));

        Command partnerSubmission = new("partner-submission", "Work with submissions shared by a partner publisher");
        partnerSubmission.Subcommands.Add(list);
        partnerSubmission.Subcommands.Add(translate);
        return partnerSubmission;
    }
}
