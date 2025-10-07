using System;
using System.IO;
using TSLPatcher.Core.Common;

namespace TSLPatcher.Core.Installation;

/// <summary>
/// Provides utilities for working with KOTOR game installations.
/// </summary>
public static class Installation
{
    /// <summary>
    /// Determines which KOTOR game is installed at the specified path.
    /// </summary>
    /// <param name="gamePath">Path to the game installation directory</param>
    /// <returns>The detected game, or null if unable to determine</returns>
    public static Game? DetermineGame(string gamePath)
    {
        if (!Directory.Exists(gamePath))
        {
            return null;
        }

        // Check for KOTOR 2 specific files
        var k2Indicators = new[]
        {
            Path.Combine(gamePath, "swkotor2.exe"),
            Path.Combine(gamePath, "KOTOR2.exe"),
            Path.Combine(gamePath, "modules", "262TEL.rim"),
            Path.Combine(gamePath, "modules", "003EBO.rim")
        };

        foreach (var indicator in k2Indicators)
        {
            if (File.Exists(indicator))
            {
                return Game.K2;
            }
        }

        // Check for KOTOR 1 specific files
        var k1Indicators = new[]
        {
            Path.Combine(gamePath, "swkotor.exe"),
            Path.Combine(gamePath, "KOTOR.exe"),
            Path.Combine(gamePath, "modules", "tar_m02aa.rim"),
            Path.Combine(gamePath, "modules", "danm13.rim")
        };

        foreach (var indicator in k1Indicators)
        {
            if (File.Exists(indicator))
            {
                return Game.K1;
            }
        }

        // Check for dialog.tlk as a fallback (both games have this)
        if (File.Exists(Path.Combine(gamePath, "dialog.tlk")))
        {
            // Try to determine by checking modules directory
            var modulesPath = Path.Combine(gamePath, "modules");
            if (Directory.Exists(modulesPath))
            {
                var moduleFiles = Directory.GetFiles(modulesPath, "*.rim", SearchOption.TopDirectoryOnly);

                // KOTOR 2 has more modules than KOTOR 1
                if (moduleFiles.Length > 60)
                {
                    return Game.K2;
                }
                if (moduleFiles.Length > 30)
                {
                    return Game.K1;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the module root name from a full path or filename.
    /// </summary>
    /// <param name="path">Module filename or path (e.g., "001ebo.rim", "modules/001ebo_s.rim")</param>
    /// <returns>The module root (e.g., "001ebo")</returns>
    public static string GetModuleRoot(string path)
    {
        var filename = Path.GetFileNameWithoutExtension(path);

        // Remove suffixes like _s, _dlg
        if (filename.Contains("_"))
        {
            var parts = filename.Split('_');
            return parts[0];
        }

        return filename;
    }

    /// <summary>
    /// Checks if the specified path is a valid KOTOR game installation.
    /// </summary>
    public static bool IsValidInstallation(string gamePath)
    {
        return DetermineGame(gamePath) != null;
    }

    /// <summary>
    /// Gets the Override directory path for a game installation.
    /// </summary>
    public static string GetOverridePath(string gamePath)
    {
        return Path.Combine(gamePath, "Override");
    }

    /// <summary>
    /// Gets the Modules directory path for a game installation.
    /// </summary>
    public static string GetModulesPath(string gamePath)
    {
        return Path.Combine(gamePath, "modules");
    }
}

