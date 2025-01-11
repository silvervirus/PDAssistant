using System.IO;
using UnityEngine;

public class AudioLoader : MonoBehaviour
{
    // Method to load the audio clip from the file system
    public static AudioClip LoadAudioClip(string fileName)
    {
        string basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string audioAssetsPath = Path.Combine(basePath, "Assets", "AudioAssets");
        string fullPath = Path.Combine(audioAssetsPath, fileName); // E.g., ImCallingAboutyour.wav

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Audio file not found at path: {fullPath}");
            return null;
        }

        // Load the audio file directly using AudioClip.LoadAudioClip() for non-Resources files
        byte[] audioData = File.ReadAllBytes(fullPath);
        AudioClip audioClip = WavUtility.ToAudioClip(audioData, 0, fileName); // Convert the WAV byte data to AudioClip

        return audioClip;
    }
}