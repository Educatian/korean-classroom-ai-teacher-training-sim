using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public static class WavPcm16Encoder
    {
        public static byte[] Encode(AudioClip clip, int sampleFrames = -1)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            int frames = sampleFrames < 0 ? clip.samples : Mathf.Clamp(sampleFrames, 0, clip.samples);
            int sampleCount = frames * clip.channels;
            var samples = new float[sampleCount];
            if (sampleCount > 0 && !clip.GetData(samples, 0))
            {
                throw new InvalidOperationException("Unable to read microphone samples.");
            }

            using var stream = new MemoryStream(44 + sampleCount * 2);
            using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2);
            writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(sampleCount * 2);
            for (int index = 0; index < samples.Length; index++)
            {
                float clamped = Mathf.Clamp(samples[index], -1f, 1f);
                writer.Write((short)Mathf.RoundToInt(clamped * short.MaxValue));
            }
            writer.Flush();
            return stream.ToArray();
        }
    }
}