using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    // Converts a byte array of WAV data into an AudioClip
    public static AudioClip ToAudioClip(byte[] data, int offset, string name)
    {
        // Read WAV header (this is a simplified version, more complex headers may need additional parsing)
        int sampleRate = BitConverter.ToInt32(data, 24); // Read sample rate (this could vary)
        int channels = BitConverter.ToInt16(data, 22); // Read the number of channels (mono, stereo, etc.)
        int byteRate = BitConverter.ToInt32(data, 28); // Byte rate used in audio file
        
        // Assuming the data after the header is just PCM data
        // You would need to check for the specifics of your WAV file to correctly extract PCM data

        // Skip over the header
        int headerSize = 44; // WAV header is usually 44 bytes
        byte[] pcmData = new byte[data.Length - headerSize];
        Array.Copy(data, headerSize, pcmData, 0, pcmData.Length);

        // Create an AudioClip from PCM data
        AudioClip audioClip = AudioClip.Create(name, pcmData.Length / (channels * 2), channels, sampleRate, false);
        float[] floatData = new float[pcmData.Length / 2];

        // Convert PCM byte data into float data that Unity's AudioClip can use
        for (int i = 0; i < pcmData.Length / 2; i++)
        {
            short sample = BitConverter.ToInt16(pcmData, i * 2);
            floatData[i] = sample / 32768f; // Normalize to [-1, 1] range for Unity
        }

        audioClip.SetData(floatData, 0);
        return audioClip;
    }
}