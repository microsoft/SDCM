/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;
using SurfaceDevCenterManager.Cli;
using SurfaceDevCenterManager.Json;
using SurfaceDevCenterManager.Services;

namespace SurfaceDevCenterManager.Handlers;

public sealed record ProductCreateInput(string InputPath, GlobalInvocationOptions Global);

public sealed class ProductCreateHandler(IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(ProductCreateInput input, CancellationToken cancellationToken)
    {
        NewProduct newProduct;
        try
        {
            newProduct = InputFileReader.Read<NewProduct>(input.InputPath);
        }
        catch (InputFileException ex)
        {
            output.Error(ex.Message);
            return ExitCode.InvalidArguments;
        }

        return await factory.UseAsync(input.Global, output, async api =>
        {
            output.Progress($"Creating product from '{input.InputPath}'...");
            try
            {
                DevCenterResponse<Product> response = await api.NewProduct(newProduct).ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                output.Result(response.ReturnValue![0], p => p.Dump());
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "product create");
            }
        }, cancellationToken);
    }
}

public sealed record ProductListInput(string? ProductId, GlobalInvocationOptions Global);

public sealed class ProductListHandler(IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(ProductListInput input, CancellationToken cancellationToken)
    {
        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<Product> response = await api.GetProducts(input.ProductId).ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                output.Results(response.ReturnValue ?? [], p => p.Dump());
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "product list");
            }
        }, cancellationToken);
    }
}
