using StardewModdingAPI;

namespace Alca259.Common;

/// <summary>Provides common utility methods for interacting with the game code shared by my various mods.</summary>
internal static class CommonHelper
{
    /// <summary>Remove one or more obsolete files from the mod folder, if they exist.</summary>
    /// <param name="mod">The mod for which to delete files.</param>
    /// <param name="relativePaths">The relative file path within the mod's folder.</param>
    public static void RemoveObsoleteFiles(IMod mod, params string[] relativePaths)
    {
        string basePath = mod.Helper.DirectoryPath;

        foreach (string relativePath in relativePaths)
        {
            string fullPath = Path.Combine(basePath, relativePath);
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    mod.Monitor.Log($"Removed obsolete file '{relativePath}'.");
                }
                catch (Exception ex)
                {
                    mod.Monitor.Log($"Failed deleting obsolete file '{relativePath}':\n{ex}");
                }
            }
        }
    }

    /// <summary>Compare two strings for equality, ignoring case and optionally trimming whitespace.</summary>
    public static bool EqualsIgnoreCase(this string? str1, string? str2, bool trim = true)
    {
        if (trim)
        {
            str1 = str1?.Trim();
            str2 = str2?.Trim();
        }

        if (str1 == null)
            return str2 == null;

        if (str2 == null)
            return false;

        return str1.Equals(str2, StringComparison.InvariantCultureIgnoreCase);
    }
}