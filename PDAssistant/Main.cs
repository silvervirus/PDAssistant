using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BepInEx;
using HarmonyLib;
using RamuneLib;
using Nautilus.Handlers;
using Nautilus.Utility;
using pdassistant;


namespace PDAssistant
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    [BepInDependency("com.snmodding.nautilus", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("SN.MoreIngots", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("SN.MoreIngotsClassic", BepInDependency.DependencyFlags.SoftDependency)]
    public class Main : BaseUnityPlugin
    {
        public const string
            MODNAME = "PDAssistant",
            AUTHOR = "Cookay",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "1.0.0.0";

        private string dataPath;
        private const string SoftDependenciesFolder = "Assets/SoftDependencies";

        public void Awake()
        {
            HandleSoftDependencies();
            // Initialize Harmony patches for mod functionality
            var harmony = new Harmony("Cookay.PDAssistant.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Log mod loading
            UnityEngine.Debug.Log("Loaded PDAssistant " + AUTHOR + VERSION);

            // Set up the data path for loading encyclopedia entries (assets)
            string pluginBasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            dataPath = Path.Combine(pluginBasePath, "Assets\\EncyclopediaData\\");
            UnityEngine.Debug.Log($"Plugin base path: {pluginBasePath}");
            UnityEngine.Debug.Log($"Data path: {dataPath}");

            // Load entries from JSON files immediately
            LoadEntries();
            LanguageHandler.RegisterLocalizationFolder();
        }

        // Load entries from JSON files
        public void LoadEntries()
        {
            // Load PDA entries from the PDA folder
            LoadPDAEntries();

            // Load Audio logs from the AudioLogs folder
            LoadAudioLogs();
        }

        private void LoadPDAEntries()
        {
            var pdaFiles = Directory.GetFiles(Path.Combine(dataPath, "PDA"), "*.json");

            if (pdaFiles.Length == 0)
            {
                UnityEngine.Debug.LogWarning("No PDA files found in the directory.");
            }

            foreach (var pdaFile in pdaFiles)
            {
                try
                {
                    UnityEngine.Debug.Log($"Loading PDA file: {pdaFile}");
                    var pda = JsonUtility.FromJson<PDA>(File.ReadAllText(pdaFile));
                    RegisterEntry(pda); // Register general PDA entry
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error loading PDA file {pdaFile}: {ex.Message}");
                }
            }
        }

        private void LoadAudioLogs()
        {
            var audioLogFiles = Directory.GetFiles(Path.Combine(dataPath, "AudioLogs"), "*.json");

            if (audioLogFiles.Length == 0)
            {
                UnityEngine.Debug.LogWarning("No audio log files found in the directory.");
            }

            foreach (var audioLogFile in audioLogFiles)
            {
                try
                {
                    UnityEngine.Debug.Log($"Loading audio log file: {audioLogFile}");
                    var audioLog = JsonUtility.FromJson<AudioLog>(File.ReadAllText(audioLogFile));
                    RegisterAudioLogEntry(audioLog); // Handle audio log registration
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error loading audio log file {audioLogFile}: {ex.Message}");
                }
            }
        }

// Registering Audio Log entry with audio clip handling
        private void RegisterAudioLogEntry(AudioLog audioLog)
        {
            // Load the audio clip
            AudioClip audioClip = AudioLoader.LoadAudioClip(audioLog.audioClip);

            if (audioClip != null)
            {
                string soundId = $"{audioLog.key}AudioLog";

                // Register the sound if not already registered
                if (!CustomSoundHandler.IsSoundRegistered(soundId))
                {
                    CustomSoundHandler.RegisterCustomSound(soundId, audioClip, AudioUtils.BusPaths.VoiceOvers);
                    UnityEngine.Debug.Log($"Sound registered with ID: {soundId}");
                }

                // Ensure the subtitles exist
                if (!LanguageManager.HasLanguageLine(soundId))
                {
                    LanguageHandler.SetLanguageLine(soundId, audioLog.description);
                    UnityEngine.Debug.Log($"Subtitle for {soundId} set to: {audioLog.description}");
                }

                // Load sprite for the icon
                Sprite icon = LoadSprite(audioLog.popupImage);
                if (icon == null)
                {
                    UnityEngine.Debug.LogWarning($"Failed to load icon sprite for {audioLog.key}");
                }

                // Register the audio log entry with the Story Goal system
                // Try to parse the string into a TechType enum
                TechType techType;
                if (Enum.TryParse(audioLog.techType, true, out techType) && Enum.IsDefined(typeof(TechType), techType))
                {
                    // Successfully parsed and validated the TechType
                    StoryGoalHandler.RegisterItemGoal(
                        key: audioLog.key,
                        Story.GoalType.PDA,
                        techType, // Use the dynamic TechType from the JSON
                        delay: 2.0f // Optional delay before log playback
                    );
                    UnityEngine.Debug.Log($"Story goal for {audioLog.key} registered with TechType: {techType}");
                }
                else
                {
                    // Log an error if the TechType string is invalid
                    UnityEngine.Debug.LogError(
                        $"Invalid TechType '{audioLog.techType}' for {audioLog.key}. Please ensure the TechType is correct in the JSON.");
                }

                // Optional: Add custom events to the story goal
                StoryGoalHandler.RegisterCustomEvent(audioLog.key,
                    () => { UnityEngine.Debug.Log($"Custom event triggered for audio log: {audioLog.key}"); });

                // Add log entry to the PDA
                PDAHandler.AddLogEntry(audioLog.key, soundId, audioClip);
                UnityEngine.Debug.Log($"Log entry for {audioLog.key} successfully added.");

                // Register the encyclopedia entry (if needed)
                Sprite imageSprite = LoadSprite(audioLog.image);
                if (imageSprite == null)
                {
                    UnityEngine.Debug.LogWarning($"Failed to load image sprite for {audioLog.key}");
                }

                try
                {
                    PDAHandler.AddEncyclopediaEntry(
                        audioLog.key,
                        audioLog.path,
                        LanguageManager.GetLanguageLine($"EncyTitle_{audioLog.key}"),
                        LanguageManager.GetLanguageLine($"EncyDesc_{audioLog.key}"),
                        imageSprite?.texture,
                        icon, // Popup image sprite
                        AudioUtils.GetFmodAsset(soundId) // FMOD asset for audio playback
                    );
                    UnityEngine.Debug.Log($"Audio log entry for {audioLog.key} successfully registered.");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error registering audio log entry {audioLog.key}: {ex.Message}");
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"Failed to load audio clip for audio log entry: {audioLog.key}");
            }
        }

        // Register the encyclopedia entry in the system
        public void RegisterEntry(PDA pda)
        {
            UnityEngine.Debug.Log($"Registering entry: {pda.key} under path {pda.path}");

            // Register dynamic path segments for localization
            var pathSegments = pda.path.Split('/');
            for (int i = 0; i < pathSegments.Length; i++)
            {
                string pathKey = $"EncyPath_{string.Join("/", pathSegments.Take(i + 1))}";
                LocalizationUpdater.AddCustomPathToLocalization(pathKey, pathSegments[i]);
            }

            // Register title and description dynamically
            LanguageManager.SetLanguageLine($"EncyTitle_{pda.key}", pda.title);
            LanguageManager.SetLanguageLine($"EncyDesc_{pda.key}", pda.description);

            // Load the sprite for the log entry (icon for popup)
            Sprite icon = LoadSprite(pda.popupImage);

            // Create sprites for images
            Sprite imageSprite = LoadSprite(pda.image);

            // Parse and validate TechType from the pda.TechType string
            TechType techType;
            if (Enum.TryParse(pda.techType, true, out techType) && Enum.IsDefined(typeof(TechType), techType))
            {
                // Successfully parsed and validated the TechType
                UnityEngine.Debug.Log($"TechType {pda.techType} for {pda.key} successfully parsed and validated.");

                // Add the entry to the encyclopedia handler
                try
                {
                    PDAHandler.AddEncyclopediaEntry(
                        pda.key,
                        pda.path,
                        LanguageManager.GetLanguageLine($"EncyTitle_{pda.key}"),
                        LanguageManager.GetLanguageLine($"EncyDesc_{pda.key}"),
                        imageSprite?.texture,
                        icon, // Popup image sprite
                        null // No audio asset for regular PDA entries
                    );
                    UnityEngine.Debug.Log($"Encyclopedia entry for {pda.key} successfully registered.");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error registering entry {pda.key}: {ex.Message}");
                }

                // Register Story Goal for this TechType (based on the encyclopedia entry)
                StoryGoalHandler.RegisterItemGoal(
                    key: pda.key,
                    Story.GoalType.Encyclopedia,
                    techType, // Use the dynamic TechType from the JSON
                    delay: 2.0f // Optional delay before goal playback
                );
                UnityEngine.Debug.Log($"Story goal for {pda.key} registered with TechType: {techType}");
            }
            else
            {
                // Log an error if the TechType string is invalid
                UnityEngine.Debug.LogError(
                    $"Invalid TechType '{pda.techType}' for {pda.key}. Please ensure the TechType is correct in the JSON.");
            }
        }






        // New method to add custom log entries with audio and icon
        public static void AddLogEntry(string key, string languageKey, AudioClip audioClip, Sprite icon = null)
        {
            // Register the language line for the log entry message
            LanguageManager.SetLanguageLine(languageKey, key);

            // Log the addition of this log entry
            Debug.Log($"Adding log entry: {key} with language key: {languageKey}");

            // Handle audio clip registration (if present)
            FMODAsset audioAsset = null;
            if (audioClip != null)
            {
                string soundId = $"{key}AudioLog";
                CustomSoundHandler.RegisterCustomSound(soundId, audioClip, AudioUtils.BusPaths.VoiceOvers);
                audioAsset = AudioUtils.GetFmodAsset(soundId);

                // Add subtitle entry for the audio log
                LanguageManager.SetLanguageLine(soundId, key); // Use the languageKey as subtitle for consistency
            }
            else
            {
                Debug.LogWarning($"No audio clip provided for log entry {key}");
            }

            // Add the log entry to the PDA
            try
            {
                // Call PDAHandler to register the log entry in the system
                PDAHandler.AddLogEntry(
                    key,
                    LanguageManager.GetLanguageLine(languageKey),
                    audioAsset,
                    icon
                );
                Debug.Log($"Log entry '{key}' successfully added.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding log entry {key}: {ex.Message}");
            }
        }


        private void RegisterAudio(string audioClipPath, string description, string pdaKey)
        {
            if (string.IsNullOrEmpty(audioClipPath))
            {
                Debug.LogError("Audio clip path is empty or null!");
                return;
            }

            try
            {
                // Load the audio clip from the path provided
                AudioClip audioClip = AudioLoader.LoadAudioClip(audioClipPath);

                if (audioClip != null)
                {
                    // Register the audio with a unique sound ID
                    string soundId = $"{pdaKey}AudioLog";
                    CustomSoundHandler.RegisterCustomSound(soundId, audioClip, AudioUtils.BusPaths.VoiceOvers);

                    // Get FMOD asset for the sound
                    FMODAsset audioAsset = AudioUtils.GetFmodAsset(soundId);

                    // Set subtitle text for the audio (using the description)
                    LanguageManager.SetLanguageLine(soundId, description);

                    Debug.Log($"Audio for {pdaKey} registered successfully.");
                }
                else
                {
                    Debug.LogError($"Failed to load audio clip for {pdaKey} from path {audioClipPath}.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading audio clip for {pdaKey}: {ex.Message}");
            }
        }




// Load sprite from the asset key
        private Sprite LoadSprite(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                UnityEngine.Debug.LogWarning("Asset key is null or empty.");
                return null;
            }

            try
            {
                // Load the Atlas.Sprite using the RamuneLib utility
                Atlas.Sprite atlasSprite = RamuneLib.Utils.ImageUtils.GetSprite(assetKey);

                // Check if the atlasSprite or its texture is null
                if (atlasSprite == null || atlasSprite.texture == null)
                {
                    UnityEngine.Debug.LogError($"Failed to load Atlas.Sprite from asset key: {assetKey}");
                    return null;
                }

                // Convert Atlas.Sprite to Unity-compatible Sprite using an extension method
                return SpriteExtensions.AsUnitySprite(atlasSprite);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception loading sprite from asset key {assetKey}: {ex.Message}");
                return null;
            }
        }

        // Extension method to convert Atlas.Sprite to Unity Sprite
        public static class SpriteExtensions
        {
            public static Sprite AsUnitySprite(Atlas.Sprite atlasSprite)
            {
                if (atlasSprite == null || atlasSprite.texture == null)
                {
                    UnityEngine.Debug.LogError(
                        "Cannot convert to Unity Sprite. The Atlas.Sprite or its texture is null.");
                    return null;
                }

                return Sprite.Create(
                    atlasSprite.texture,
                    new Rect(0f, 0f, atlasSprite.texture.width, atlasSprite.texture.height),
                    new Vector2(0.5f, 0.5f) // Default pivot at the center
                );
            }
        }

        private void HandleSoftDependencies()
        {
            if (!Directory.Exists(SoftDependenciesFolder))
            {
                UnityEngine.Debug.LogWarning(
                    $"SoftDependencies folder not found at {SoftDependenciesFolder}. No additional dependencies loaded.");
                return;
            }

            var jsonFiles = Directory.GetFiles(SoftDependenciesFolder, "*.json");
            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFile);
                    var dependencies = JsonUtility.FromJson<SoftDependencyList>(jsonContent);

                    foreach (var mod in dependencies.Mods)
                    {
                        if (IsModLoaded(mod))
                        {
                            UnityEngine.Debug.Log($"Soft dependency '{mod}' is loaded.");
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning(
                                $"Soft dependency '{mod}' is not loaded. Ensure the mod is installed if needed.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error loading soft dependency file {jsonFile}: {ex.Message}");
                }
            }
        }

        private bool IsModLoaded(string modId)
        {
            return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modId);
        }
    }

    [Serializable]
    public class SoftDependencyList
    {
        public List<string> Mods;
    }



    // Representation of the PDA entry
    [System.Serializable]
    public class PDA
    {
        public string key;
        public string path;
        public string title;
        public string description;
        public string image;
        public string popupImage;
        public string techType; // Added TechType field (as string)

    }

    [System.Serializable]
    public class AudioLog
    {
        public string key;
        public string path;
        public string title;
        public string description;
        public string image;
        public string popupImage;
        public string audioClip; // Path to the audio file for audio logs
        public string techType; // Added TechType field (as string)
    }
}


    

