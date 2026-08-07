/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurfaceDevCenterManager.Json;

/// <summary>
///     Reads and deserializes a --input file into one of the library's NewProduct/NewSubmission/
///     NewShippingLabel payload types. sdcm 1.x wrapped these in a {"createType": ..., "createXxx": {...}}
///     envelope so a single flag could tell them apart; the subcommand now does that job, so a bare
///     payload is expected, and a leftover envelope produces an explicit migration error instead of a
///     confusing null-reference deeper in the call stack.
/// </summary>
public static class InputFileReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static T Read<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new InputFileException($"Input file not found: {path}");
        }

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            throw new InputFileException($"Could not read input file '{path}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InputFileException($"Could not read input file '{path}': {ex.Message}");
        }

        using (JsonDocument document = ParseOrThrow(json, path))
        {
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("createType", out _))
            {
                throw new InputFileException(
                    $"'{path}' uses the old sdcm 1.x envelope (\"createType\"/\"createProduct\"/\"createSubmission\"/" +
                    "\"createShippingLabel\"). Pass the inner object directly to --input instead - see the " +
                    "migration section in README.md.");
            }
        }

        try
        {
            T? result = JsonSerializer.Deserialize<T>(json, Options);
            if (result is null)
            {
                throw new InputFileException($"Input file '{path}' deserialized to nothing (is it just \"null\"?).");
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new InputFileException($"Input file '{path}' could not be parsed as the expected object: {ex.Message}");
        }
    }

    private static JsonDocument ParseOrThrow(string json, string path)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InputFileException($"Input file '{path}' is not valid JSON: {ex.Message}");
        }
    }
}
