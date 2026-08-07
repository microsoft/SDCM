/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager.Json;

/// <summary>Thrown for any problem with a user-supplied --input file; always maps to <see cref="ExitCode.InvalidArguments" />.</summary>
public sealed class InputFileException(string message) : Exception(message);
