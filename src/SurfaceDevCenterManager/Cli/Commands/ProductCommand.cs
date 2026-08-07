/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SurfaceDevCenterManager.Handlers;

namespace SurfaceDevCenterManager.Cli.Commands;

internal static class ProductCommand
{
    public static Command Build(ServiceProviderAccessor accessor)
    {
        Option<string> input = Opt.Str("--input", "Path to a JSON file with the NewProduct payload", true);
        Command create = new("create", "Create a new product");
        create.Options.Add(input);
        create.SetHandlerAction(
            accessor,
            (pr, global) => new ProductCreateInput(pr.Required(input), global),
            (sp, i, ct) => sp.GetRequiredService<ProductCreateHandler>().RunAsync(i, ct));

        Option<string?> productId = Opt.OptionalStr("--product-id", "Product id to fetch; omit to list every product");
        Command list = new("list", "List products, or get one by id");
        list.Options.Add(productId);
        list.SetHandlerAction(
            accessor,
            (pr, global) => new ProductListInput(pr.GetValue(productId), global),
            (sp, i, ct) => sp.GetRequiredService<ProductListHandler>().RunAsync(i, ct));

        Command product = new("product", "Manage Hardware Dev Center products");
        product.Subcommands.Add(create);
        product.Subcommands.Add(list);
        return product;
    }
}
