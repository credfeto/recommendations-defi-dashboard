using System;
using System.IO;

namespace Credfeto.Defi.Database;

internal static class CacheDirectoryResolver
{
    public static string EnsureWritableDirectory(string configuredDirectory)
    {
        if (TryCreateDirectory(configuredDirectory))
        {
            return configuredDirectory;
        }

        string fallbackDirectory = Path.Combine(
            path1: Path.GetTempPath(),
            path2: "credfeto-defi-dashboard",
            path3: "data"
        );

        if (TryCreateDirectory(fallbackDirectory))
        {
            return fallbackDirectory;
        }

        throw new IOException($"Unable to create cache directory at '{configuredDirectory}' or fallback '{fallbackDirectory}'.");
    }

    private static bool TryCreateDirectory(string directory)
    {
        try
        {
            _ = Directory.CreateDirectory(path: directory);

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}

