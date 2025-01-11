using System;
using System.Collections.Generic;
using pdassistant;
using UnityEngine;

public static class LanguageManager
{
    private static Dictionary<string, string> languageLines = new Dictionary<string, string>();

    // Set a custom language line (key-value pair)
    public static void SetLanguageLine(string key, string value)
    {
        if (languageLines.ContainsKey(key))
        {
            languageLines[key] = value; // Update existing key
        }
        else
        {
            languageLines.Add(key, value); // Add new key-value pair
        }
    }

    // Get a language line by key
    public static string GetLanguageLine(string key)
    {
        // First, check the in-memory dictionary
        if (languageLines.ContainsKey(key))
        {
            return languageLines[key];
        }

        // If not found in memory, check the file-based localization
        try
        {
            Dictionary<string, string> localizationData = LocalizationUpdater.LoadLocalizationFile();
            return localizationData.ContainsKey(key) ? localizationData[key] : key;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading language line for key {key}: {ex.Message}");
            return key; // Default to key if not found in file
        }
    }

    // Check if a language line exists (in memory or in the file)
    public static bool HasLanguageLine(string key)
    {
        // First check in the in-memory dictionary
        if (languageLines.ContainsKey(key))
        {
            return true;
        }

        // If not found in memory, check if it's present in the localization file
        try
        {
            Dictionary<string, string> localizationData = LocalizationUpdater.LoadLocalizationFile();
            return localizationData.ContainsKey(key);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error checking if language line exists for key {key}: {ex.Message}");
            return false; // Default to false if an error occurs
        }
    }
}