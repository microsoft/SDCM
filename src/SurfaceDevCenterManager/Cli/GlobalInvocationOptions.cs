/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;
using SurfaceDevCenterManager.Services;

namespace SurfaceDevCenterManager.Cli;

/// <summary>The parsed, validated values of the recursive global options, threaded into every leaf command's input record.</summary>
public sealed record GlobalInvocationOptions(string Profile, AuthMode Auth, AadPromptMode Aad, uint TimeoutSeconds)
{
    public static GlobalInvocationOptions FromParseResult(ParseResult parseResult)
    {
        string profile = parseResult.GetValue(GlobalOptions.Profile) ?? "default";
        AuthMode auth = EnumParsing.ParseKebabOrThrow<AuthMode>(parseResult.GetValue(GlobalOptions.Auth), "--auth");
        AadPromptMode aad = EnumParsing.ParseKebabOrThrow<AadPromptMode>(parseResult.GetValue(GlobalOptions.Aad), "--aad");
        uint timeout = parseResult.GetValue(GlobalOptions.Timeout);

        return new GlobalInvocationOptions(profile, auth, aad, timeout);
    }
}
