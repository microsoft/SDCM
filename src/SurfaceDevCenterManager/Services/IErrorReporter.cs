/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;

namespace SurfaceDevCenterManager.Services;

/// <summary>
///     Turns a <see cref="DevCenterErrorDetails" /> or an unhandled exception into the right
///     <see cref="ExitCode" /> and prints diagnostics, replacing the ten copy-pasted
///     "if 429 ... else DevCenterErrorDetailsDump ..." blocks the old Program.cs had, one per command.
/// </summary>
public interface IErrorReporter
{
    ExitCode Report(DevCenterErrorDetails? error);

    ExitCode ReportException(Exception exception, string operation);
}
