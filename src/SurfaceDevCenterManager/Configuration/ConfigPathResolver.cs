/*++
    Copyright (c) Microsoft Corporation and Contributors. All rights reserved.

    Licensed under the MIT license. See LICENSE file in the project root for full license information.
--*/

namespace SurfaceDevCenterManager.Configuration;

/// <summary>
///     Resolves the location of authconfig.json for a tool that may be installed as a global dotnet
///     tool (where the install directory is not a sensible place for a user to keep credentials).
/// </summary>
public static class ConfigPathResolver
{
    private const string ConfigFileName = "authconfig.json";

    /// <summary>
    ///     Probes, in order: an explicit path, the current working directory, the per-user config
    ///     directory, and finally the application base directory (for copy-deployed builds).
    ///     Returns the first candidate that exists, or null if none do.
    /// </summary>
    public static string? Resolve(string? explicitPath)
    {
        foreach (string candidate in EnumerateCandidates(explicitPath))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    ///     Returns the path authconfig.json would be written to by <c>sdcm config init</c>: the
    ///     per-user config directory, regardless of whether a file exists there yet.
    /// </summary>
    public static string GetUserConfigPath()
    {
        return Path.Combine(GetUserConfigDirectory(), ConfigFileName);
    }

    /// <summary>
    ///     Returns every location that is probed, in priority order, whether or not each exists.
    ///     Used by <c>sdcm config path</c> to show the user exactly where sdcm is looking.
    /// </summary>
    public static IReadOnlyList<string> EnumerateCandidates(string? explicitPath)
    {
        List<string> candidates = new();

        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            candidates.Add(Path.GetFullPath(explicitPath));
        }

        candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName));
        candidates.Add(GetUserConfigPath());
        candidates.Add(Path.Combine(AppContext.BaseDirectory, ConfigFileName));

        return candidates;
    }

    private static string GetUserConfigDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "sdcm");
        }

        string? xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdgConfigHome))
        {
            return Path.Combine(xdgConfigHome, "sdcm");
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config", "sdcm");
    }
}
