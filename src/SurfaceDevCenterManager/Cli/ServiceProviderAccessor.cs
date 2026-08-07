/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager.Cli;

/// <summary>
///     The full command tree (including every leaf's <c>SetAction</c>) is built and used to parse
///     args before the generic host exists, because building the host itself depends on parsed
///     global option values (--config, --output, -v). Leaf actions close over this mutable holder
///     instead of a concrete <see cref="IServiceProvider" />; it is populated once the host is built,
///     which happens before the actions are actually invoked.
/// </summary>
internal sealed class ServiceProviderAccessor
{
    public IServiceProvider Provider { get; set; } = null!;
}
