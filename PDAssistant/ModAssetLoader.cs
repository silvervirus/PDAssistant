using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using BepInEx;
using Nautilus.Handlers;
using Nautilus.Utility;

namespace PDAssistant
{
    public class ModAssetLoader
    {
        private string modFolderPath;
        private string pdaDirectory;
        private string voiceLogsDirectory;
        private string assetBundleDirectory;
        private string assetBundlePath;
        public static AssetBundle AudioassetBundle { get; private set; }
        public static AssetBundle PDAassetBundle { get; private set; }
        private Dictionary<string, AssetBundle> loadedAssetBundles = new Dictionary<string, AssetBundle>();

        // Constructor to initialize mod folder paths
        public ModAssetLoader(string modFolderName)
        { 
            string pluginBasePath = BepInEx.Paths.PluginPath;
            modFolderPath = Path.Combine(pluginBasePath, modFolderName);

            if (Directory.Exists(modFolderPath))
            {
                pdaDirectory = Path.Combine(modFolderPath, "Assets", "PDA");
                voiceLogsDirectory = Path.Combine(modFolderPath, "Assets", "VoiceLogs");
                assetBundleDirectory = Path.Combine(modFolderPath, "Assets");
            }
            else
            {
                Debug.Log($"Mod folder '{modFolderName}' not found in: {pluginBasePath}");
                return;
            }

            Debug.Log($"PDA Assets Path: {pdaDirectory}");
            Debug.Log($"Voice Assets Path: {voiceLogsDirectory}");
            Debug.Log($"AssetBundle Directory: {assetBundleDirectory}");
        }

        // Load all assets from mod
        public void LoadAllAssets()
        {
            try
            {
                // Ensure audioLog is initialized before using
                AudioLog audioLog = new AudioLog(); // You need to properly load or assign audioLog based on your use case
                assetBundlePath = Path.Combine(assetBundleDirectory, audioLog.bundlename);
             

           

                LoadPDAEntries();
                LoadAudioLogs();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading mod assets: {ex.Message}");
            }
        }
        
        // Load PDA entries
        private void LoadPDAEntries()
        {
            var pdaFiles = Directory.GetFiles(pdaDirectory, "*.json");

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

        // Register the PDA entry
       private void RegisterEntry(PDA pda)
{
    Debug.Log($"Registering PDA entry: {pda.title ?? "Untitled"}");

    LanguageManager.SetLanguageLine($"EncyTitle_{pda.key}", pda.title);
    LanguageManager.SetLanguageLine($"EncyDesc_{pda.key}", pda.description);

    Sprite icon;
    Sprite imageSprite;

    if (pda.usefallback)
    {
        // Load sprite using fallback
        icon = LoadSprite(pda.popupImage);
        if (icon == null)
        {
            Debug.LogWarning($"Fallback failed to load popup sprite '{pda.popupImage}' for {pda.key}");
        }

        imageSprite = LoadSprite(pda.image);
        if (imageSprite == null)
        {
            Debug.LogWarning($"Fallback failed to load image sprite '{pda.image}' for {pda.key}");
        }
    }
    else
    {
        // Load sprite from AssetBundle
        PDAassetBundle = AssetBundle.LoadFromFile(Path.Combine(assetBundleDirectory, pda.bundlename));
        if (PDAassetBundle == null)
        {
            Debug.LogError($"Failed to load AssetBundle '{pda.bundlename}' for {pda.key}");
            return;
        }

        icon = PDAassetBundle.LoadAsset<Sprite>(pda.popupImage);
        if (icon == null)
        {
            Debug.LogWarning($"Failed to load popup sprite '{pda.popupImage}' from AssetBundle for {pda.key}");
        }

        imageSprite = PDAassetBundle.LoadAsset<Sprite>(pda.image);
        if (imageSprite == null)
        {
            Debug.LogWarning($"Failed to load image sprite '{pda.image}' from AssetBundle for {pda.key}");
        }
    }

    if (Enum.TryParse(pda.techType, true, out TechType techType) && Enum.IsDefined(typeof(TechType), techType))
    {
        StoryGoalHandler.RegisterItemGoal(pda.key, Story.GoalType.Encyclopedia, techType, 2.0f);
        Debug.Log($"Story goal for {pda.key} registered with TechType: {techType}");
    }
    else
    {
        Debug.LogError($"Invalid TechType for PDA entry {pda.key}");
    }

    try
    {
        PDAHandler.AddEncyclopediaEntry(
            pda.key,
            pda.path,
            LanguageManager.GetLanguageLine($"EncyTitle_{pda.key}"),
            LanguageManager.GetLanguageLine($"EncyDesc_{pda.key}"),
            imageSprite?.texture,
            icon
        );
        Debug.Log($"PDA entry for {pda.key} registered.");
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error registering PDA entry {pda.key}: {ex.Message}");
    }
}


        // Load audio log entries
        private void LoadAudioLogs()
        {
            var audioLogFiles = Directory.GetFiles(voiceLogsDirectory, "*.json");

            if (audioLogFiles.Length == 0)
            {
                Debug.LogWarning("No audio log files found in the directory.");
            }

            foreach (var audioLogFile in audioLogFiles)
            {
                try
                {
                    Debug.Log($"Loading audio log file: {audioLogFile}");
                    var audioLog = JsonUtility.FromJson<AudioLog>(File.ReadAllText(audioLogFile));
                    RegisterAudioLogEntry(audioLog);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading audio log file {audioLogFile}: {ex.Message}");
                }
            }
        }

        // Register the AudioLog entry
      private void RegisterAudioLogEntry(AudioLog audioLog)
{
    string soundId = !string.IsNullOrEmpty(audioLog.soundid) ? audioLog.soundid : $"{audioLog.key}AudioLog";
    AudioClip audioClip = null;
    Sprite icon = null;

    // Load AssetBundle only once
    AssetBundle bundle = null;
    if (!audioLog.usefallback)
    {
        bundle = AssetBundle.LoadFromFile(Path.Combine(assetBundleDirectory, audioLog.bundlename));
        if (bundle == null)
        {
            Debug.LogError($"Failed to load AssetBundle '{audioLog.bundlename}' for {audioLog.key}");
            return;
        }
    }

    // Load audio clip
    if (audioLog.usefallback)
    {
        audioClip = AudioLoader.LoadAudioClip(audioLog.soundname);
        if (audioClip == null)
        {
            Debug.LogWarning($"Fallback failed to load audio clip '{audioLog.soundname}' for {audioLog.key}");
        }
    }
    else
    {
        audioClip = bundle?.LoadAsset<AudioClip>(audioLog.soundname);
        if (audioClip == null)
        {
            Debug.LogWarning($"Failed to load audio clip '{audioLog.soundname}' from AssetBundle for {audioLog.key}");
        }
    }

    // Load icon sprite
    if (audioLog.usefallback)
    {
        icon = LoadSprite(audioLog.popupImage);
        if (icon == null)
        {
            Debug.LogWarning($"Fallback failed to load popup sprite '{audioLog.popupImage}' for {audioLog.key}");
        }
    }
    else
    {
        icon = bundle?.LoadAsset<Sprite>(audioLog.popupImage);
        if (icon == null)
        {
            Debug.LogWarning($"Failed to load popup sprite '{audioLog.popupImage}' from AssetBundle for {audioLog.key}");
        }
    }

    // Register the audio and icon
    FMODAsset fmodid = AudioUtils.GetFmodAsset(soundId);
    CustomSoundHandler.RegisterCustomSound(soundId, audioClip, AudioUtils.BusPaths.PDAVoice);
    Debug.Log($"Sound registered with ID: {soundId}");

    // Set subtitle if not already set
    if (!LanguageManager.HasLanguageLine(soundId))
    {
        LanguageManager.SetLanguageLine(soundId, audioLog.description);
        Debug.Log($"Subtitle for {soundId} set: {audioLog.description}");
    }

    // Handle TechType and Story goals
    if (Enum.TryParse(audioLog.techType, true, out TechType techType) && Enum.IsDefined(typeof(TechType), techType))
    {
        StoryGoalHandler.RegisterItemGoal(audioLog.key, Story.GoalType.PDA, techType, 0.0f);
        Debug.Log($"Story goal for {audioLog.key} registered with TechType: {techType}");
    }
    else
    {
        Debug.LogError($"Invalid TechType for AudioLog entry {audioLog.key}");
    }

    // Add the log entry to PDA
    PDAHandler.AddLogEntry(audioLog.key, audioLog.description, fmodid, icon);
    Debug.Log($"Log entry for {audioLog.key} added.");
}


        // Load a sprite from the asset path
        private Sprite LoadSprite(string assetKey)
        {
            if (string.IsNullOrEmpty(assetKey))
            {
                Debug.LogWarning("Asset key is null or empty.");
                return null;
            }

            try
            {
                Atlas.Sprite atlasSprite = RamuneLib.Utils.ImageUtilss.GetSprite(
                    filename: assetKey,
                    extension: ".png",
                    pdaDirectory: pdaDirectory,
                    voiceLogsDirectory: voiceLogsDirectory
                );

                return atlasSprite == null || atlasSprite.texture == null
                    ? null
                    : Sprite.Create(atlasSprite.texture, new Rect(0, 0, atlasSprite.texture.width, atlasSprite.texture.height), new Vector2(0.5f, 0.5f));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading sprite from asset key {assetKey}: {ex.Message}");
                return null;
            }
        }
    }
}
