/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;

namespace SurfaceDevCenterManager.Cli;

/// <summary>
///     Options available on every subcommand (<c>Recursive = true</c>), added once to the root
///     command. Kept as raw strings for --auth/--aad/--output rather than typed enum options, so
///     invalid values produce one consistent, testable error path via <see cref="EnumParsing" />
///     instead of relying on System.CommandLine's built-in enum conversion.
/// </summary>
internal static class GlobalOptions
{
    public static readonly Option<string> Profile = new("--profile")
    {
        Description = "Named credential profile from authconfig.json to use",
        DefaultValueFactory = _ => "default",
        Recursive = true
    };

    public static readonly Option<string> Auth = new("--auth")
    {
        Description = "Credential type: auto (default), managed-identity, client-secret, interactive",
        DefaultValueFactory = _ => "auto",
        Recursive = true
    };

    public static readonly Option<string> Aad = new("--aad")
    {
        Description = "Interactive sign-in behavior: never (default), prompt, always, refresh-session, select-account",
        DefaultValueFactory = _ => "never",
        Recursive = true
    };

    public static readonly Option<string?> Config = new("--config")
    {
        Description = "Explicit path to authconfig.json (overrides the usual discovery order)",
        Recursive = true
    };

    public static readonly Option<uint> Timeout = new("--timeout")
    {
        Description = "HTTP request timeout, in seconds",
        DefaultValueFactory = _ => 300u,
        Recursive = true
    };

    public static readonly Option<string> Output = new("--output")
    {
        Description = "Output format: text (default) or json",
        DefaultValueFactory = _ => "text",
        Recursive = true
    };

    public static readonly Option<bool> Verbose = new("--verbose", "-v")
    {
        Description = "Enable verbose diagnostic logging on stderr",
        Recursive = true
    };

    public static void AddTo(RootCommand rootCommand)
    {
        rootCommand.Options.Add(Profile);
        rootCommand.Options.Add(Auth);
        rootCommand.Options.Add(Aad);
        rootCommand.Options.Add(Config);
        rootCommand.Options.Add(Timeout);
        rootCommand.Options.Add(Output);
        rootCommand.Options.Add(Verbose);
    }
}
