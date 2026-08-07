/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SurfaceDevCenterManager.Services;

namespace SurfaceDevCenterManager.Cli;

internal static class CommandActionHelper
{
    /// <summary>
    ///     Wires a leaf command's action: parses the recursive global options, builds the leaf's
    ///     strongly-typed input record, resolves its DI-registered handler, and runs it, mapping the
    ///     result to a process exit code. Centralizes the one bit of error handling every leaf needs
    ///     (an invalid --auth/--aad value).
    /// </summary>
    public static void SetHandlerAction<TInput>(
        this Command command,
        ServiceProviderAccessor accessor,
        Func<ParseResult, GlobalInvocationOptions, TInput> buildInput,
        Func<IServiceProvider, TInput, CancellationToken, Task<ExitCode>> run)
    {
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            GlobalInvocationOptions global;
            try
            {
                global = GlobalInvocationOptions.FromParseResult(parseResult);
            }
            catch (ArgumentException ex)
            {
                await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                return (int)ExitCode.InvalidArguments;
            }

            TInput input = buildInput(parseResult, global);
            ExitCode exitCode = await run(accessor.Provider, input, cancellationToken).ConfigureAwait(false);
            return (int)exitCode;
        });
    }

    public static T Required<T>(this ParseResult parseResult, Option<T> option)
    {
        T value = parseResult.GetValue(option)!;
        return value;
    }
}
