using System;
using UnityEngine;
using Nautilus.Handlers;
using RamuneLib;
using System.IO;
using System.Reflection;

namespace PDAssistant
{
    public class EncyclopediaEntryLoader : MonoBehaviour
    {
         private string dataPath;

        [System.Serializable]
        public class PDA
        {
            public string key;
            public string path;
            public string title;
            public string description;
            public string image;
            public string popupImage;
        }

        void Start()
        {
            // Get the path where the DLL is located
            string pluginBasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Define the data path relative to the plugin's location
            dataPath = Path.Combine(pluginBasePath, "Assets/EncyclopediaData/");

            Debug.Log($"Plugin base path: {pluginBasePath}");
            Debug.Log($"Data path: {dataPath}");

            LoadEntries();
        }

        public void LoadEntries()
        {
            var pdaFiles = Directory.GetFiles($"{dataPath}/PDA", "*.json");

            if (pdaFiles.Length == 0)
            {
                Debug.LogWarning("No PDA files found in the directory.");
            }

            foreach (var pdaFile in pdaFiles)
            {
                try
                {
                    Debug.Log($"Loading PDA file: {pdaFile}");
                    var pda = JsonUtility.FromJson<PDA>(File.ReadAllText(pdaFile));
                    RegisterEntry(pda);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading PDA file {pdaFile}: {ex.Message}");
                }
            }
        }

       private void RegisterEntry(PDA pda)
        {
            Debug.Log($"Registering entry: {pda.key} under path {pda.path}");

            // Set language lines for path and title (for localization purposes)
            LanguageManager.SetLanguageLine($"EncyPath_{pda.path}", pda.path); // Custom path key
            LanguageManager.SetLanguageLine($"EncyTitle_{pda.key}", pda.title); // Custom title key
            LanguageManager.SetLanguageLine($"EncyDesc_{pda.key}", pda.description); // Custom description key

            // Log language line registration
            Debug.Log($"Registered language lines for path: {pda.path}, title: {pda.title}");

            // Load and convert the main image and popup image using the asset keys
            Sprite imageSprite = LoadSprite(pda.image); // Loads the main image
            Sprite popupSprite = LoadSprite(pda.popupImage); // Loads the popup image

            if (imageSprite == null)
            {
                Debug.LogWarning($"Main image for {pda.key} is missing or failed to load.");
            }

            if (popupSprite == null)
            {
                Debug.LogWarning($"Popup image for {pda.key} is missing or failed to load.");
            }

            // Register the encyclopedia entry
            try
            {
                PDAHandler.AddEncyclopediaEntry(
                    pda.key,
                    pda.path,               // This is where your entry will appear in the Databank
                    LanguageManager.GetLanguageLine($"EncyTitle_{pda.key}"), // Get the localized title
                    LanguageManager.GetLanguageLine($"EncyDesc_{pda.key}"), // Get the localized description
                    imageSprite?.texture,   // Use the texture of the main image Sprite
                    popupSprite,            // Popup image Sprite
                    null                    // No unlock sound, or specify if needed
                );
                Debug.Log($"Encyclopedia entry for {pda.key} successfully registered.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error registering entry {pda.key}: {ex.Message}");
            }
        }

        private Sprite LoadSprite(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                Debug.LogWarning("Asset key is null or empty.");
                return null;
            }

            try
            {
                // Load the Atlas.Sprite using the RamuneLib utility
                Atlas.Sprite atlasSprite = RamuneLib.Utils.ImageUtils.GetSprite(assetKey);

                // Check if the atlasSprite or its texture is null
                if (atlasSprite == null || atlasSprite.texture == null)
                {
                    Debug.LogError($"Failed to load Atlas.Sprite from asset key: {assetKey}");
                    return null;
                }

                // Convert Atlas.Sprite to Unity-compatible Sprite using an extension method
                return SpriteExtensions.AsUnitySprite(atlasSprite);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception loading sprite from asset key {assetKey}: {ex.Message}");
                return null;
            }
        }

        public static class SpriteExtensions
        {
            /// <summary>
            /// Converts an existing Atlas.Sprite to a Unity-compatible Sprite.
            /// </summary>
            /// <param name="atlasSprite">The existing Atlas.Sprite.</param>
            /// <returns>A new Sprite compatible with Unity.</returns>
            public static Sprite AsUnitySprite(Atlas.Sprite atlasSprite)
            {
                if (atlasSprite == null || atlasSprite.texture == null)
                {
                    Debug.LogError("Cannot convert to Unity Sprite. The Atlas.Sprite or its texture is null.");
                    return null;
                }

                return Sprite.Create(
                    atlasSprite.texture,
                    new Rect(0f, 0f, atlasSprite.texture.width, atlasSprite.texture.height),
                    new Vector2(0.5f, 0.5f) // Default pivot at the center
                );
            }
        }
    }
}
