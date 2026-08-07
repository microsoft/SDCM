/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager;

/// <summary>
///     Process exit codes returned by sdcm. <c>0</c> means success, any other value indicates the reason for failure.
/// </summary>
public enum ExitCode
{
    Success = 0,
    InvalidArguments = 1,
    AuthenticationFailed = 2,
    ApiRequestFailed = 3,
    NotFound = 4,
    InvalidState = 5,
    RateLimited = 6,
    WorkflowFailed = 7,
    IoError = 8,
    Canceled = 9,
    UnhandledException = 10
}
