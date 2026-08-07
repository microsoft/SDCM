/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using Microsoft.Devices.HardwareDevCenterManager.DevCenterApi;

namespace SurfaceDevCenterManager.Services;

/// <summary>
///     Per-invocation state that used to live in static fields on the old Program class:
///     the correlation id sent with every request, and the last request/response trace, kept
///     around so it can be dumped to help diagnose an unhandled exception.
/// </summary>
public sealed class RunContext
{
    public Guid CorrelationId { get; } = Guid.NewGuid();

    public DevCenterErrorDetails? LastCommand { get; private set; }

    public void SetLastCommand(DevCenterErrorDetails error)
    {
        LastCommand = error;
    }
}
