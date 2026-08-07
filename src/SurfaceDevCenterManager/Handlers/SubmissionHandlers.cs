/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;
using Microsoft.Devices.HardwareDevCenterManager.Utility;
using SurfaceDevCenterManager.Cli;
using SurfaceDevCenterManager.Json;
using SurfaceDevCenterManager.Services;

namespace SurfaceDevCenterManager.Handlers;

public sealed record SubmissionCreateInput(string ProductId, string InputPath, GlobalInvocationOptions Global);

public sealed class SubmissionCreateHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(SubmissionCreateInput input, CancellationToken cancellationToken)
    {
        NewSubmission newSubmission;
        try
        {
            newSubmission = InputFileReader.Read<NewSubmission>(input.InputPath);
        }
        catch (InputFileException ex)
        {
            output.Error(ex.Message);
            return ExitCode.InvalidArguments;
        }

        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<Submission> response = await api
                    .NewSubmission(input.ProductId, newSubmission).ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                output.Result(response.ReturnValue![0], s => s.Dump());
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "submission create");
            }
        }, cancellationToken);
    }
}

public sealed record SubmissionListInput(string ProductId, string? SubmissionId, GlobalInvocationOptions Global);

public sealed class SubmissionListHandler(IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(SubmissionListInput input, CancellationToken cancellationToken)
    {
        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<Submission> response = await api
                    .GetSubmission(input.ProductId, input.SubmissionId).ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                output.Results(response.ReturnValue ?? [], s => s.Dump());
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "submission list");
            }
        }, cancellationToken);
    }
}

public sealed record SubmissionCommitInput(string ProductId, string SubmissionId, GlobalInvocationOptions Global);

public sealed class SubmissionCommitHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(SubmissionCommitInput input, CancellationToken cancellationToken)
    {
        return await factory.UseAsync(input.Global, output, async api =>
        {
            output.Progress($"Committing submission {input.SubmissionId}...");
            try
            {
                DevCenterResponse<bool> response = await api
                    .CommitSubmission(input.ProductId, input.SubmissionId).ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                output.Progress("Commit accepted.");
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "submission commit");
            }
        }, cancellationToken);
    }
}

public sealed record SubmissionUploadInput(
    string ProductId, string SubmissionId, string PackagePath, GlobalInvocationOptions Global);

public sealed class SubmissionUploadHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(SubmissionUploadInput input, CancellationToken cancellationToken)
    {
        if (!File.Exists(input.PackagePath))
        {
            output.Error($"Package file not found: {input.PackagePath}");
            return ExitCode.IoError;
        }

        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<Submission> response = await api
                    .GetSubmission(input.ProductId, input.SubmissionId).ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                Submission submission = response.ReturnValue![0];
                Download.Item? uploadTarget = submission.Downloads?.Items?
                    .FirstOrDefault(i => string.Equals(i.Type, "initialPackage", StringComparison.OrdinalIgnoreCase));

                if (uploadTarget?.Url is null)
                {
                    output.Error("This submission has no 'initialPackage' upload URL.");
                    return ExitCode.InvalidState;
                }

                output.Progress($"Uploading '{input.PackagePath}'...");
                BlobStorageHandler blob = new(uploadTarget.Url.ToString());
                await blob.Upload(input.PackagePath).ConfigureAwait(false);
                output.Progress("Upload complete.");
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "submission upload");
            }
        }, cancellationToken);
    }
}

public sealed record SubmissionDownloadInput(
    string ProductId, string SubmissionId, string OutputFile, GlobalInvocationOptions Global);

public sealed class SubmissionDownloadHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(SubmissionDownloadInput input, CancellationToken cancellationToken)
    {
        if (File.Exists(input.OutputFile))
        {
            output.Error($"Destination already exists: {input.OutputFile}");
            return ExitCode.IoError;
        }

        string? directory = Path.GetDirectoryName(Path.GetFullPath(input.OutputFile));
        if (directory != null && !Directory.Exists(directory))
        {
            output.Error($"Destination directory does not exist: {directory}");
            return ExitCode.IoError;
        }

        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<Submission> response = await api
                    .GetSubmission(input.ProductId, input.SubmissionId).ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                Submission submission = response.ReturnValue![0];
                Download.Item? downloadTarget = submission.Downloads?.Items?
                    .FirstOrDefault(i => string.Equals(i.Type, "signedPackage", StringComparison.OrdinalIgnoreCase));

                if (downloadTarget?.Url is null)
                {
                    output.Error("This submission has no 'signedPackage' download available yet.");
                    return ExitCode.InvalidState;
                }

                output.Progress($"Downloading to '{input.OutputFile}'...");
                BlobStorageHandler blob = new(downloadTarget.Url.ToString());
                await blob.Download(input.OutputFile).ConfigureAwait(false);
                output.Progress("Download complete.");
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "submission download");
            }
        }, cancellationToken);
    }
}

public sealed record SubmissionMetadataDownloadInput(
    string ProductId, string SubmissionId, string OutputFile, GlobalInvocationOptions Global);

public sealed class SubmissionMetadataDownloadHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(SubmissionMetadataDownloadInput input, CancellationToken cancellationToken)
    {
        if (File.Exists(input.OutputFile))
        {
            output.Error($"Destination already exists: {input.OutputFile}");
            return ExitCode.IoError;
        }

        return await factory.UseAsync(input.Global, output, async api =>
        {
            try
            {
                DevCenterResponse<Submission> response = await api
                    .GetSubmission(input.ProductId, input.SubmissionId).ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                Submission submission = response.ReturnValue![0];
                Download.Item? metadataTarget = submission.Downloads?.Items?
                    .FirstOrDefault(i => string.Equals(i.Type, "driverMetadata", StringComparison.OrdinalIgnoreCase));

                if (metadataTarget?.Url is null)
                {
                    output.Error("This submission has no publisher metadata yet - run 'submission metadata create' first.");
                    return ExitCode.InvalidState;
                }

                output.Progress($"Downloading publisher metadata to '{input.OutputFile}'...");
                BlobStorageHandler blob = new(metadataTarget.Url.ToString());
                await blob.Download(input.OutputFile).ConfigureAwait(false);
                output.Progress("Download complete.");
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "submission metadata download");
            }
        }, cancellationToken);
    }
}

public sealed record SubmissionMetadataCreateInput(string ProductId, string SubmissionId, GlobalInvocationOptions Global);

public sealed class SubmissionMetadataCreateHandler(
    IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(SubmissionMetadataCreateInput input, CancellationToken cancellationToken)
    {
        return await factory.UseAsync(input.Global, output, async api =>
        {
            output.Progress("Requesting publisher metadata generation...");
            try
            {
                DevCenterResponse<bool> response = await api
                    .CreateMetaData(input.ProductId, input.SubmissionId).ConfigureAwait(false);
                if (response.Error != null)
                {
                    return errors.Report(response.Error);
                }

                output.Progress("Metadata generation requested; poll 'submission metadata download' once ready.");
                return ExitCode.Success;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "submission metadata create");
            }
        }, cancellationToken);
    }
}

public sealed record SubmissionWaitInput(
    string ProductId,
    string SubmissionId,
    bool WaitMetadata,
    uint PollIntervalSeconds,
    uint? WaitTimeoutSeconds,
    GlobalInvocationOptions Global);

public sealed class SubmissionWaitHandler(IDevCenterHandlerFactory factory, IOutputWriter output, IErrorReporter errors)
{
    public async Task<ExitCode> RunAsync(SubmissionWaitInput input, CancellationToken cancellationToken)
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

                    DevCenterResponse<Submission> response = await api
                        .GetSubmission(input.ProductId, input.SubmissionId).ConfigureAwait(false);
                    if (response.Error != null)
                    {
                        return errors.Report(response.Error);
                    }

                    Submission submission = response.ReturnValue![0];
                    WorkflowStatus? status = submission.WorkflowStatus;

                    if (output.Format == OutputFormat.Text && status != null)
                    {
                        await status.Dump().ConfigureAwait(false);
                    }

                    bool metadataReady = !input.WaitMetadata || submission.Downloads?.Items?.Any(
                        i => string.Equals(i.Type, "driverMetadata", StringComparison.OrdinalIgnoreCase)) == true;

                    bool failed = status?.State?.Contains("fail", StringComparison.OrdinalIgnoreCase) == true;
                    bool terminal = failed
                        || status?.State?.Contains("complet", StringComparison.OrdinalIgnoreCase) == true
                        || status?.State?.Contains("publish", StringComparison.OrdinalIgnoreCase) == true
                        || string.Equals(submission.CommitStatus, "commitFailed", StringComparison.OrdinalIgnoreCase);

                    if (terminal && metadataReady)
                    {
                        output.Result(submission, s => s.Dump());
                        return failed ? ExitCode.WorkflowFailed : ExitCode.Success;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(input.PollIntervalSeconds), linkedCts.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
            {
                output.Error($"Timed out after {input.WaitTimeoutSeconds}s waiting for the submission to reach a terminal state.");
                return ExitCode.Canceled;
            }
            catch (OperationCanceledException)
            {
                return ExitCode.Canceled;
            }
            catch (Exception ex)
            {
                return errors.ReportException(ex, "submission wait");
            }
        }, cancellationToken);
    }
}
