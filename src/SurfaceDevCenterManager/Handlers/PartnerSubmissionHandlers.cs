/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;
using SurfaceDevCenterManager.Cli;
using SurfaceDevCenterManager.Services;

namespace SurfaceDevCenterManager.Handlers;

public sealed record PartnerSubmissionListInput(
    string PublisherId, string ProductId, string SubmissionId, GlobalInvocationOptions Global);

public sealed class PartnerSubmissionListHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(PartnerSubmissionListInput input, CancellationToken cancellationToken)
    {
        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<Submission> response = await api
                    .GetPartnerSubmission(input.PublisherId, input.ProductId, input.SubmissionId)
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
                return errors.ReportException(ex, "partner-submission list");
            }
        }, cancellationToken);
    }
}

public sealed record PartnerSubmissionTranslateInput(
    string PublisherId, string ProductId, string SubmissionId, GlobalInvocationOptions Global);

/// <summary>
///     Resolves a partner's product/submission id pair into the corresponding submission on this
///     account, for scripts that only know the identifiers the partner shared with them.
/// </summary>
public sealed class PartnerSubmissionTranslateHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(PartnerSubmissionTranslateInput input, CancellationToken cancellationToken)
    {
        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<Submission> response = await api
                    .GetPartnerSubmission(input.PublisherId, input.ProductId, input.SubmissionId)
                    .ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                Submission submission = response.ReturnValue![0];
                output.Result(submission, s =>
                {
                    Console.WriteLine($"ProductId: {s.ProductId}");
                    Console.WriteLine($"SubmissionId: {s.Id}");
                });
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "partner-submission translate");
            }
        }, cancellationToken);
    }
}
