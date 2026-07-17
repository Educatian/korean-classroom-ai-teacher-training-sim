using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class AudioFeedbackTests
    {
        [TestCaseSource(nameof(FeedbackClips))]
        public void ProceduralFeedbackClips_AreShortSubtleAndAudible(AudioClip clip, float maximumDuration)
        {
            float[] samples = new float[clip.samples];
            clip.GetData(samples, 0);
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
            }

            Assert.That(clip.channels, Is.EqualTo(1));
            Assert.That(clip.length, Is.LessThan(maximumDuration));
            Assert.That(peak, Is.InRange(0.025f, 0.32f));
        }

        private static object[] FeedbackClips => new object[]
        {
            new object[] { ProceduralAudioClips.UiClick, 0.07f },
            new object[] { ProceduralAudioClips.SlipperLeft, 0.20f },
            new object[] { ProceduralAudioClips.SlipperRight, 0.20f }
        };
    }
}
