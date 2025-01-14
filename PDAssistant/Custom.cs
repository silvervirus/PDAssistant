

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RamuneLib.Utils
{
    public static class ImageUtilss
    {
        private static Dictionary<string, Texture2D> CachedTextures = new();
        private static Dictionary<string, Atlas.Sprite> CachedSprites = new();


        public static Atlas.Sprite GetSprite(string filename, string extension = ".png", string pdaDirectory = null,
            string voiceLogsDirectory = null)
        {
            // Check cache first
            if (CachedSprites.ContainsKey(filename + extension))
                return CachedSprites[filename + extension];

            // List of directories to check, provided by the mod loader
            List<string> directoriesToCheck = new();
            if (!string.IsNullOrEmpty(pdaDirectory)) directoriesToCheck.Add(pdaDirectory);
            if (!string.IsNullOrEmpty(voiceLogsDirectory)) directoriesToCheck.Add(voiceLogsDirectory);

            string spritePath = null;

            // Search through the specified directories
            foreach (var directory in directoriesToCheck)
            {
                string potentialPath = Path.Combine(directory, filename + extension);
                if (File.Exists(potentialPath))
                {
                    spritePath = potentialPath;
                    break; // Stop searching once we find the file
                }
            }

            // If no valid path is found, log an error and return null
            if (string.IsNullOrEmpty(spritePath))
            {
                Debug.LogError($"GetSprite: File '{filename + extension}' not found in specified directories.");
                return null;
            }

            // Load the sprite
            var sprite = Nautilus.Utility.ImageUtils.LoadSpriteFromFile(spritePath);
            if (sprite == null)
            {
                Debug.LogError($"GetSprite: Failed to load sprite from {spritePath}");
                return null;
            }

            // Cache the loaded sprite
            CachedSprites.Add(filename + extension, sprite);
            return sprite;
        }
    }
}
