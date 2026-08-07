/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.CommandLine;

namespace SurfaceDevCenterManager.Cli;

/// <summary>Small factory helpers to keep the ~20 leaf command builders free of boilerplate.</summary>
internal static class Opt
{
    public static Option<string> Str(string name, string description, bool required = false)
    {
        return new Option<string>(name) { Description = description, Required = required };
    }

    public static Option<string?> OptionalStr(string name, string description)
    {
        return new Option<string?>(name) { Description = description };
    }

    public static Option<uint> UInt(string name, string description, uint defaultValue)
    {
        return new Option<uint>(name) { Description = description, DefaultValueFactory = _ => defaultValue };
    }

    public static Option<bool> Flag(string name, string description, params string[] aliases)
    {
        return new Option<bool>(name, aliases) { Description = description };
    }
}
