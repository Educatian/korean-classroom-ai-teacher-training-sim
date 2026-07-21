using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class QuestVoiceRuntimeTests
    {
        [Test]
        public void WavPcm16Encoder_ProducesMono16KhzRiffWave()
        {
            AudioClip clip = AudioClip.Create("test", 160, 1, 16000, false);
            clip.SetData(new float[160], 0);

            byte[] wav = WavPcm16Encoder.Encode(clip, 160);

            Assert.That(System.Text.Encoding.ASCII.GetString(wav, 0, 4), Is.EqualTo("RIFF"));
            Assert.That(System.Text.Encoding.ASCII.GetString(wav, 8, 4), Is.EqualTo("WAVE"));
            Assert.That(System.Text.Encoding.ASCII.GetString(wav, 36, 4), Is.EqualTo("data"));
            Assert.That(System.BitConverter.ToInt32(wav, 24), Is.EqualTo(16000));
            Assert.That(System.BitConverter.ToInt16(wav, 22), Is.EqualTo(1));
            Assert.That(wav.Length, Is.EqualTo(44 + 160 * 2));
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void AndroidManifestPostprocessor_AddsMicrophonePermissionAndOptionalFeature()
        {
            const string source = "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"><uses-application /></manifest>";

            string updated = QuestAndroidManifestPolicy.Apply(source);

            StringAssert.Contains("android.permission.RECORD_AUDIO", updated);
            StringAssert.Contains("android.hardware.microphone", updated);
            StringAssert.Contains("required=\"false\"", updated);
        }
    }
}