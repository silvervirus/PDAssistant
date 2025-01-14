
using System.Collections.Generic;
using System.IO;

namespace PDAssistant;
public static class ModAssetRegistry
{
    // List to store registered mod folder names or paths
    private static readonly List<string> RegisteredModPaths = new List<string>();

    /// <summary>
    /// Allows mods to register their asset folder paths.
    /// </summary>
    /// <param name="modFolderPath">The path to the mod's asset folder.</param>
    public static void RegisterModPath(string modFolderPath)
    {
        if (!string.IsNullOrWhiteSpace(modFolderPath) && Directory.Exists(modFolderPath))
        {
            if (!RegisteredModPaths.Contains(modFolderPath))
            {
                RegisteredModPaths.Add(modFolderPath);
                UnityEngine.Debug.Log($"Registered mod path: {modFolderPath}");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Invalid or non-existent mod path: {modFolderPath}");
        }
    }

    /// <summary>
    /// Gets all registered mod paths.
    /// </summary>
    /// <returns>A list of registered mod folder paths.</returns>
    public static List<string> GetRegisteredModPaths()
    {
        return new List<string>(RegisteredModPaths);
    }
}