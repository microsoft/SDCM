/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SurfaceDevCenterManager.Handlers;

namespace SurfaceDevCenterManager.Cli.Commands;

internal static class ShippingLabelCommand
{
    public static Command Build(ServiceProviderAccessor accessor)
    {
        Option<string> productId = Opt.Str("--product-id", "Product id the submission belongs to", true);
        Option<string> submissionId = Opt.Str("--submission-id", "Submission id the shipping label belongs to", true);

        Option<string> createInput = Opt.Str("--input", "Path to a JSON file with the NewShippingLabel payload", true);
        Option<string?> partnerId = Opt.OptionalStr("--partner-id", "Publisher id to ship the driver to (sets recipientSpecifications.receiverPublisherId)");
        Command create = new("create", "Create a shipping label for a submission");
        create.Options.Add(productId);
        create.Options.Add(submissionId);
        create.Options.Add(createInput);
        create.Options.Add(partnerId);
        create.SetHandlerAction(
            accessor,
            (pr, global) => new ShippingLabelCreateInput(
                pr.Required(productId), pr.Required(submissionId), pr.Required(createInput), pr.GetValue(partnerId), global),
            (sp, i, ct) => sp.GetRequiredService<ShippingLabelCreateHandler>().RunAsync(i, ct));

        Option<string?> listShippingLabelId = Opt.OptionalStr("--shipping-label-id", "Shipping label id to fetch; omit to list every shipping label for the submission");
        Command list = new("list", "List shipping labels for a submission, or get one by id");
        list.Options.Add(productId);
        list.Options.Add(submissionId);
        list.Options.Add(listShippingLabelId);
        list.SetHandlerAction(
            accessor,
            (pr, global) => new ShippingLabelListInput(
                pr.Required(productId), pr.Required(submissionId), pr.GetValue(listShippingLabelId), global),
            (sp, i, ct) => sp.GetRequiredService<ShippingLabelListHandler>().RunAsync(i, ct));

        Option<string> waitShippingLabelId = Opt.Str("--shipping-label-id", "Shipping label id to wait on", true);
        Option<uint> pollInterval = Opt.UInt("--poll-interval", "Seconds between status checks", 5);
        Option<uint?> waitTimeout = new("--wait-timeout") { Description = "Give up after this many seconds (default: wait indefinitely)" };
        Command wait = new("wait", "Wait for a shipping label to reach a terminal workflow state");
        wait.Options.Add(productId);
        wait.Options.Add(submissionId);
        wait.Options.Add(waitShippingLabelId);
        wait.Options.Add(pollInterval);
        wait.Options.Add(waitTimeout);
        wait.SetHandlerAction(
            accessor,
            (pr, global) => new ShippingLabelWaitInput(
                pr.Required(productId), pr.Required(submissionId), pr.Required(waitShippingLabelId),
                pr.GetValue(pollInterval), pr.GetValue(waitTimeout), global),
            (sp, i, ct) => sp.GetRequiredService<ShippingLabelWaitHandler>().RunAsync(i, ct));

        Command shippingLabel = new("shipping-label", "Manage shipping labels");
        shippingLabel.Subcommands.Add(create);
        shippingLabel.Subcommands.Add(list);
        shippingLabel.Subcommands.Add(wait);
        return shippingLabel;
    }
}
