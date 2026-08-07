/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;
using SurfaceDevCenterManager.Cli;
using SurfaceDevCenterManager.Json;
using SurfaceDevCenterManager.Services;

namespace SurfaceDevCenterManager.Handlers;

public sealed record ShippingLabelCreateInput(
    string ProductId, string SubmissionId, string InputPath, string? PartnerId, GlobalInvocationOptions Global);

public sealed class ShippingLabelCreateHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(ShippingLabelCreateInput input, CancellationToken cancellationToken)
    {
        NewShippingLabel newShippingLabel;
        try
        {
            newShippingLabel = InputFileReader.Read<NewShippingLabel>(input.InputPath);
        }
        catch (InputFileException ex)
        {
            output.Error(ex.Message);
            return ExitCode.InvalidArguments;
        }

        if (!string.IsNullOrWhiteSpace(input.PartnerId))
        {
            newShippingLabel.RecipientSpecifications ??= new RecipientSpecifications();
            newShippingLabel.RecipientSpecifications.ReceiverPublisherId = input.PartnerId;
        }

        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<ShippingLabel> response = await api
                    .NewShippingLabel(input.ProductId, input.SubmissionId, newShippingLabel).ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                output.Result(response.ReturnValue![0], s => s.Dump());
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "shipping-label create");
            }
        }, cancellationToken);
    }
}

public sealed record ShippingLabelListInput(
    string ProductId, string SubmissionId, string? ShippingLabelId, GlobalInvocationOptions Global);

public sealed class ShippingLabelListHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(ShippingLabelListInput input, CancellationToken cancellationToken)
    {
        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<ShippingLabel> response = await api
                    .GetShippingLabels(input.ProductId, input.SubmissionId, input.ShippingLabelId)
                    .ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                output.Results(response.ReturnValue ?? [], s => s.Dump());
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "shipping-label list");
            }
        }, cancellationToken);
    }
}

public sealed record ShippingLabelWaitInput(
    string ProductId,
    string SubmissionId,
    string ShippingLabelId,
    uint PollIntervalSeconds,
    uint? WaitTimeoutSeconds,
    GlobalInvocationOptions Global);

public sealed class ShippingLabelWaitHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(ShippingLabelWaitInput input, CancellationToken cancellationToken)
    {
        using CancellationTokenSource? timeoutCts = input.WaitTimeoutSeconds is { } seconds
            ? new CancellationTokenSource(TimeSpan.FromSeconds(seconds))
            : null;
        using CancellationTokenSource linkedCts = timeoutCts != null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                while (true)
                {
                    linkedCts.Token.ThrowIfCancellationRequested();

                    DevCenterResponse<ShippingLabel> response = await api
                        .GetShippingLabels(input.ProductId, input.SubmissionId, input.ShippingLabelId)
                        .ConfigureAwait(false);
                    if (response.Error != null)
                    {
                        return errors.Report(response.Error);
                    }

                    ShippingLabel shippingLabel = response.ReturnValue![0];
                    WorkflowStatus? status = shippingLabel.WorkflowStatus;

                    if (output.Format == OutputFormat.Text && status != null)
                    {
                        await status.Dump().ConfigureAwait(false);
                    }

                    bool failed = status?.State?.Contains("fail", StringComparison.OrdinalIgnoreCase) == true;
                    bool terminal = failed
                        || status?.State?.Contains("complet", StringComparison.OrdinalIgnoreCase) == true
                        || status?.State?.Contains("publish", StringComparison.OrdinalIgnoreCase) == true;

                    if (terminal)
                    {
                        output.Result(shippingLabel, s => s.Dump());
                        return failed ? ExitCode.WorkflowFailed : ExitCode.Success;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(input.PollIntervalSeconds), linkedCts.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
            {
                output.Error($"Timed out after {input.WaitTimeoutSeconds}s waiting for the shipping label to reach a terminal state.");
                return ExitCode.Canceled;
            }
            catch (OperationCanceledException)
            {
                return ExitCode.Canceled;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "shipping-label wait");
            }
        }, cancellationToken);
    }
}
