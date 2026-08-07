/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager.Cli;

/// <summary>
///     Parses CLI-friendly, optionally kebab-cased enum values (e.g. "managed-identity") into their
///     PascalCase enum members (e.g. <c>AuthMode.ManagedIdentity</c>).
/// </summary>
internal static class EnumParsing
{
    public static bool TryParseKebab<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        string normalized = value.Replace("-", string.Empty);
        return Enum.TryParse(normalized, true, out result);
    }

    public static TEnum ParseKebabOrThrow<TEnum>(string? value, string optionName) where TEnum : struct, Enum
    {
        if (TryParseKebab(value, out TEnum result))
        {
            return result;
        }

        string allowed = string.Join(", ", Enum.GetNames<TEnum>().Select(ToKebab));
        throw new ArgumentException($"Invalid value for {optionName}: '{value}'. Allowed values: {allowed}.");
    }

    private static string ToKebab(string pascalCase)
    {
        return string.Concat(
            pascalCase.Select((c, i) => i > 0 && char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
    }
}
