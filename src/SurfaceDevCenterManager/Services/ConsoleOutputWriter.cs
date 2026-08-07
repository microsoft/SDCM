/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

using System.Text.Json;

namespace SurfaceDevCenterManager.Services;

public sealed class ConsoleOutputWriter(OutputFormat format) : IOutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public OutputFormat Format { get; } = format;

    public void Progress(string message)
    {
        if (Format == OutputFormat.Text)
        {
            Console.WriteLine(message);
        }
    }

    public void Result<T>(T model, Action<T> textDump)
    {
        if (Format == OutputFormat.Text)
        {
            textDump(model);
        }
        else
        {
            Console.WriteLine(JsonSerializer.Serialize(model, JsonOptions));
        }
    }

    public void Results<T>(IReadOnlyList<T> models, Action<T> textDump)
    {
        if (Format == OutputFormat.Text)
        {
            foreach (T model in models)
            {
                textDump(model);
            }
        }
        else
        {
            Console.WriteLine(JsonSerializer.Serialize(models, JsonOptions));
        }
    }

    public void Error(string message)
    {
        Console.Error.WriteLine(message);
    }
}
