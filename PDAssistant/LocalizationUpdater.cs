using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace pdassistant
{
    public class LocalizationUpdater
    {
        static string pluginBasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static string localizationFilePath = Path.Combine(pluginBasePath, "Localization", "English.json");
        private static readonly object fileLock = new object(); // Thread safety lock

        // Load the localization file into a dictionary
        public static Dictionary<string, string> LoadLocalizationFile()
        {
            lock (fileLock)
            {
                if (!File.Exists(localizationFilePath))
                {
                    Debug.LogError($"Localization file not found at: {localizationFilePath}");
                    return new Dictionary<string, string>();
                }

                try
                {
                    string jsonContent = File.ReadAllText(localizationFilePath);
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error loading localization file at {localizationFilePath}: {ex.Message}");
                    return new Dictionary<string, string>();
                }
            }
        }

        // Save the updated localization file, appending new entries
        public static void SaveLocalizationFile(Dictionary<string, string> localizationData)
        {
            lock (fileLock)
            {
                try
                {
                    // Ensure directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(localizationFilePath));

                    string jsonContent = JsonConvert.SerializeObject(localizationData, Formatting.Indented);
                    File.WriteAllText(localizationFilePath, jsonContent);
                    Debug.Log($"Updated localization file saved to {localizationFilePath}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error saving localization file at {localizationFilePath}: {ex.Message}");
                }
            }
        }

        // Add new custom path labels to the localization data without overwriting existing entries
        public static void AddCustomPathToLocalization(string path, string label)
        {
            lock (fileLock)
            {
                // Load the current localization data
                Dictionary<string, string> localizationData = LoadLocalizationFile();

                // Add the new label for the path if it doesn't already exist
                if (!localizationData.ContainsKey(path))
                {
                    localizationData.Add(path, label);
                    Debug.Log($"Added new path to localization: {path} = {label}");
                }
                else
                {
                    Debug.Log($"Path {path} already exists in localization data.");
                }

                // Save the updated localization file with new paths
                SaveLocalizationFile(localizationData);
            }
        }
       

    }
}
