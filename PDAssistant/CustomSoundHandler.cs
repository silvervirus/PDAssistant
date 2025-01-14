using System.Collections.Generic;
using UnityEngine;

public static class CustomSoundHandler
{
    private static readonly HashSet<string> registeredSounds = new HashSet<string>();

    /// <summary>
    /// Registers a custom sound.
    /// </summary>
    public static void RegisterCustomSound(string soundId, AudioClip audioClip, string busPath)
    {
        if (string.IsNullOrEmpty(soundId))
        {
            Debug.LogError("Sound ID is null or empty.");
            return;
        }

        if (registeredSounds.Contains(soundId))
        {
            Debug.LogWarning($"Sound ID '{soundId}' is already registered.");
            return;
        }

        // Register the sound with your audio system here
        // Example: AudioUtils.RegisterSound(soundId, audioClip, busPath);

        registeredSounds.Add(soundId);
        Debug.Log($"Sound '{soundId}' registered successfully.");
    }

    /// <summary>
    /// Checks if a sound with the given ID has already been registered.
    /// </summary>
    public static bool IsSoundRegistered(string soundId)
    {
        return registeredSounds.Contains(soundId);
    }
}