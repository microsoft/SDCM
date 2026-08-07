/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager.Services;

/// <summary>
///     All command-visible stdout output goes through here, rather than straight to
///     <see cref="Console" />, so that <c>--output json</c> can suppress human-readable progress lines
///     and emit machine-readable results instead.
/// </summary>
public interface IOutputWriter
{
    OutputFormat Format { get; }

    /// <summary>A "&gt; doing something" progress line. Suppressed entirely in JSON mode.</summary>
    void Progress(string message);

    /// <summary>Writes a single result: <paramref name="textDump" /> in text mode, or a JSON serialization of <paramref name="model" /> in JSON mode.</summary>
    void Result<T>(T model, Action<T> textDump);

    /// <summary>Writes a list of results the same way <see cref="Result{T}" /> writes one.</summary>
    void Results<T>(IReadOnlyList<T> models, Action<T> textDump);

    /// <summary>Writes a diagnostic/error message to stderr, regardless of format.</summary>
    void Error(string message);
}
