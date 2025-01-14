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
   
    public class Main : BaseUnityPlugin
    {
        public const string
            MODNAME = "PDAssistant",
            AUTHOR = "Cookay",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "1.0.0.0";

        private string dataPath;
        private const string SoftDependenciesFolder = "Assets/SoftDependencies";
        public static AssetBundle AudioassetBundle { get; private set; }
        public static AssetBundle PDAAssetBundle  { get; private set; }
        string assetBundleDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets");
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

            // Load AudioLogs (this is where your audioLog data comes from)
            LoadAudioLogs();

            // Load assets from the appropriate AssetBundles
            LoadEntries();

            // Load assets from other mods using ModAssetLoader
            LoadAssetsFromOtherMods();

            // Register localization
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
                    if (audioLog != null)
                    {
                        RegisterAudioLogEntry(audioLog); // Handle audio log registration
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"Failed to deserialize audio log file: {audioLogFile}");
                    }
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
    // Use the provided soundid if available; otherwise, fallback to auto-generated ID
    string soundId = !string.IsNullOrEmpty(audioLog.soundid) ? audioLog.soundid : $"{audioLog.key}AudioLog";



    // Attempt to load the AssetBundle if fallback is not enabled
    if (!audioLog.usefallback)
    {
        AudioassetBundle = AssetBundle.LoadFromFile(Path.Combine(assetBundleDirectory, audioLog.bundlename));
        if (AudioassetBundle == null)
        {
            Debug.LogError($"Failed to load AssetBundle: {audioLog.bundlename}");
            if (!audioLog.usefallback)
            {
                return; // Exit if fallbacks are not allowed and AssetBundle loading failed
            }
        }
    }

    // Load the AudioClip
    AudioClip audioClip = null;
    if (audioLog.usefallback)
    {
        audioClip = AudioLoader.LoadAudioClip(audioLog.soundname);
        if (audioClip == null)
        {
            Debug.LogError($"Fallback failed to load AudioClip '{audioLog.soundname}' for {audioLog.key}");
        }
    }
    else
    {
        audioClip = AudioassetBundle?.LoadAsset<AudioClip>(audioLog.soundname);
        if (audioClip == null)
        {
            Debug.LogError($"Failed to load AudioClip '{audioLog.soundname}' from AssetBundle '{audioLog.bundlename}' for {audioLog.key}");
        }
    }

    if (audioClip != null)
    {
        CustomSoundHandler.RegisterCustomSound(soundId, audioClip, AudioUtils.BusPaths.PDAVoice);
        Debug.Log($"Sound registered with ID: {soundId}");
    }
    else
    {
        Debug.LogError($"Unable to load AudioClip for {audioLog.key}");
        return;
    }

    // Load the icon sprite
    Sprite icon = null;
    if (audioLog.usefallback)
    {
        icon = LoadSpriteonly(audioLog.popupImage);
        if (icon == null)
        {
            Debug.LogWarning($"Fallback failed to load sprite '{audioLog.popupImage}' for {audioLog.key}");
        }
    }
    else
    {
        icon = AudioassetBundle?.LoadAsset<Sprite>(audioLog.popupImage);
        if (icon == null)
        {
            Debug.LogWarning($"Failed to load sprite '{audioLog.popupImage}' from AssetBundle '{audioLog.bundlename}' for {audioLog.key}");
        }
    }

    // Register the audio log entry in the PDA
    FMODAsset fmodAsset = AudioUtils.GetFmodAsset(soundId);
    PDAHandler.AddLogEntry(audioLog.key, audioLog.description, fmodAsset, icon);
    Debug.Log($"Log entry for {audioLog.key} added to PDA.");

    // Handle subtitles
    if (!LanguageManager.HasLanguageLine(soundId))
    {
        LanguageHandler.SetLanguageLine(soundId, audioLog.description);
        Debug.Log($"Subtitle for {soundId} set: {audioLog.description}");
    }

    // Handle story goals
    TechType techType;
    if (Enum.TryParse(audioLog.techType, true, out techType) && Enum.IsDefined(typeof(TechType), techType))
    {
        StoryGoalHandler.RegisterItemGoal(
            key: audioLog.key,
            Story.GoalType.PDA,
            techType,
            delay: 0.0f
        );
        Debug.Log($"Story goal for {audioLog.key} registered with TechType: {techType}");
    }
    else
    {
        Debug.LogError($"Invalid TechType for AudioLog entry {audioLog.key}");
    }

    // Add Encyclopedia entry
    try
    {
        PDAHandler.AddEncyclopediaEntry(
            audioLog.key,
            audioLog.path,
            LanguageManager.GetLanguageLine($"EncyTitle_{audioLog.key}"),
            LanguageManager.GetLanguageLine($"EncyDesc_{audioLog.key}"),
            null, // No texture for Encyclopedia entry
            icon, // Icon for the entry
            null, // No additional media
            fmodAsset
        );
        Debug.Log($"Encyclopedia entry for {audioLog.key} successfully registered.");
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error registering encyclopedia entry for {audioLog.key}: {ex.Message}");
    }
}

// Register the encyclopedia entry in the system
public void RegisterEntry(PDA pda)
{
    Debug.Log($"Registering entry: {pda.key} under path {pda.path}");

   

    // Attempt to load the AssetBundle if fallback is not enabled
    if (!pda.usefallback)
    {
        PDAAssetBundle = AssetBundle.LoadFromFile(Path.Combine(assetBundleDirectory, pda.bundlename));
        if (PDAAssetBundle == null)
        {
            Debug.LogError($"Failed to load AssetBundle: {pda.bundlename}");
            if (!pda.usefallback)
            {
                Debug.LogError($"Failed to load AssetBundle: {pda.bundlename}");
                return; // Exit if fallbacks are not allowed and AssetBundle loading failed
            }
        }
    }

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
    Sprite icon = null;
    if (pda.usefallback)
    {
        icon = LoadSprite(pda.popupImage);
        if (icon == null)
        {
            Debug.LogWarning($"Fallback failed to load popup sprite '{pda.popupImage}' for {pda.key}");
        }
    }
    else
    {
        icon = PDAAssetBundle?.LoadAsset<Sprite>(pda.popupImage);
        if (icon == null)
        {
            Debug.LogWarning($"Failed to load popup sprite '{pda.popupImage}' from AssetBundle '{pda.bundlename}' for {pda.key}");
        }
    }

    // Load the main image sprite for the entry
    Sprite imageSprite = null;
    if (pda.usefallback)
    {
        imageSprite = LoadSprite(pda.image);
        if (imageSprite == null)
        {
            Debug.LogWarning($"Fallback failed to load image sprite '{pda.image}' for {pda.key}");
        }
    }
    else
    {
        imageSprite = PDAAssetBundle?.LoadAsset<Sprite>(pda.image);
        if (imageSprite == null)
        {
            Debug.LogWarning($"Failed to load image sprite '{pda.image}' from AssetBundle '{pda.bundlename}' for {pda.key}");
        }
    }

    // Parse and validate TechType from the pda.TechType string
    TechType techType;
    if (Enum.TryParse(pda.techType, true, out techType) && Enum.IsDefined(typeof(TechType), techType))
    {
        Debug.Log($"TechType {pda.techType} for {pda.key} successfully parsed and validated.");

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
                null // No additional media
            );
            Debug.Log($"Encyclopedia entry for {pda.key} successfully registered.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error registering entry {pda.key}: {ex.Message}");
        }

        // Register Story Goal for this TechType (based on the encyclopedia entry)
        StoryGoalHandler.RegisterItemGoal(
            key: pda.key,
            Story.GoalType.Encyclopedia,
            techType,
            delay: 2.0f // Optional delay before goal playback
        );
        Debug.Log($"Story goal for {pda.key} registered with TechType: {techType}");
    }
    else
    {
        Debug.LogError($"Invalid TechType '{pda.techType}' for {pda.key}. Please ensure the TechType is correct in the JSON.");
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
private Sprite LoadSpriteonly(string assetKey)
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
        if (atlasSprite == null)
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
private void LoadAssetsFromOtherMods()
{
    var registeredPaths = ModAssetRegistry.GetRegisteredModPaths();

    foreach (var modPath in registeredPaths)
    {
        try
        {
            if (!Directory.Exists(modPath))
            {
                UnityEngine.Debug.LogWarning($"Registered mod path not found: {modPath}");
                continue;
            }

            // Extract the folder name for logging/debugging
            string modFolderName = new DirectoryInfo(modPath).Name;

            // Create a ModAssetLoader for the mod
            var modAssetLoader = new ModAssetLoader(modFolderName);

            // Load and register assets
            modAssetLoader.LoadAllAssets(); // Call the loader's method to load assets
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Error loading assets from path {modPath}: {ex.Message}");
        }
    }
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
        public string bundlename;
        public bool usefallback;
    }

    [System.Serializable]
    public class AudioLog
    {
        public string key;
        public string path;
        public string title;
        public string description;
        public string popupImage;
        public string techType; // Added TechType field (as string)
        public string soundid;
        public string bundlename;
        public string soundname;
        public bool usefallback;
    }
}


    

