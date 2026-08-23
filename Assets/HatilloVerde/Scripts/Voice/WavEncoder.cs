using System.IO;
using UnityEngine;

/// <summary>PCM16 WAV from a Unity AudioClip. Whisper needs a real .wav, not raw samples.</summary>
public static class WavEncoder
{
    public static byte[] FromClip(AudioClip clip)
    {
        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        return FromSamples(samples, clip.channels, clip.frequency);
    }

    public static byte[] FromSamples(float[] samples, int channels, int frequency)
    {
        var pcm = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            var v = (short)Mathf.Clamp(samples[i] * 32767f, short.MinValue, short.MaxValue);
            pcm[i * 2] = (byte)(v & 0xff);
            pcm[i * 2 + 1] = (byte)((v >> 8) & 0xff);
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(44 + pcm.Length - 8);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(frequency);
        writer.Write(frequency * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(pcm.Length);
        writer.Write(pcm);
        return stream.ToArray();
    }
}
