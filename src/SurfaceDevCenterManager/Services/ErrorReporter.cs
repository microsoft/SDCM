/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;
using Microsoft.Extensions.Logging;

namespace SurfaceDevCenterManager.Services;

public sealed class ErrorReporter(IOutputWriter output, RunContext runContext, ILogger<ErrorReporter> logger)
    : IErrorReporter
{
    private const string EntityNotFound = "entityNotFound";
    private const string RequestInvalidForCurrentState = "requestInvalidForCurrentState";

    public ExitCode Report(DevCenterErrorDetails? error)
    {
        if (error == null)
        {
            return ExitCode.Success;
        }

        runContext.SetLastCommand(error);

        output.Error($"Hardware Dev Center request failed (correlation id {runContext.CorrelationId}):");
        output.Error($"  Code:     {error.Code}");
        output.Error($"  HTTP:     {error.HttpErrorCode}");
        output.Error($"  Message:  {error.Message}");

        if (error.ValidationErrors != null)
        {
            foreach (DevCenterErrorValidationErrorEntry entry in error.ValidationErrors)
            {
                output.Error($"  Validation: [{entry.Target}] {entry.Message}");
            }
        }

        if (error.Trace != null)
        {
            logger.LogDebug(
                "Request trace - Method: {Method}, Url: {Url}, RequestId: {RequestId}, Content: {Content}",
                error.Trace.Method, error.Trace.Url, error.Trace.RequestId, error.Trace.Content);
        }

        if (error.HttpErrorCode == 429)
        {
            return ExitCode.RateLimited;
        }

        if (string.Equals(error.Code, EntityNotFound, StringComparison.OrdinalIgnoreCase))
        {
            return ExitCode.NotFound;
        }

        if (string.Equals(error.Code, RequestInvalidForCurrentState, StringComparison.OrdinalIgnoreCase))
        {
            return ExitCode.InvalidState;
        }

        return ExitCode.ApiRequestFailed;
    }

    public ExitCode ReportException(Exception exception, string operation)
    {
        output.Error($"Unhandled exception during '{operation}' (correlation id {runContext.CorrelationId}):");
        output.Error($"  {exception.GetType().Name}: {exception.Message}");
        if (exception.InnerException != null)
        {
            output.Error($"  Inner: {exception.InnerException.Message}");
        }

        logger.LogDebug(exception, "Unhandled exception during {Operation}", operation);

        if (runContext.LastCommand?.Trace != null)
        {
            DevCenterTrace trace = runContext.LastCommand.Trace;
            logger.LogDebug(
                "Last request - Method: {Method}, Url: {Url}, RequestId: {RequestId}",
                trace.Method, trace.Url, trace.RequestId);
        }

        return exception is OperationCanceledException ? ExitCode.Canceled : ExitCode.UnhandledException;
    }
}
