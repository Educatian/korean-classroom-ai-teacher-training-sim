using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public static class ProceduralAudioClips
    {
        private const int SampleRate = 44100;
        private static AudioClip uiClick;
        private static AudioClip slipperLeft;
        private static AudioClip slipperRight;

        public static AudioClip UiClick => uiClick ??= CreateUiClick();
        public static AudioClip SlipperLeft => slipperLeft ??= CreateFootstep("SlipperStep_Left", 0.94f, 0x51A7u);
        public static AudioClip SlipperRight => slipperRight ??= CreateFootstep("SlipperStep_Right", 1.06f, 0x8D31u);

        private static AudioClip CreateUiClick()
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * 0.052f);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)SampleRate;
                float envelope = Mathf.Exp(-time * 78f) * Mathf.Clamp01(time * 900f);
                float tone = Mathf.Sin(2f * Mathf.PI * 1680f * time)
                    + 0.32f * Mathf.Sin(2f * Mathf.PI * 2480f * time);
                samples[i] = tone * envelope * 0.105f;
            }

            return CreateClip("UiClick_Delicate", samples);
        }

        private static AudioClip CreateFootstep(string name, float toneShift, uint seed)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * 0.17f);
            float[] samples = new float[sampleCount];
            uint state = seed;
            float filteredNoise = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)SampleRate;
                state = state * 1664525u + 1013904223u;
                float noise = ((state >> 8) / 8388607.5f) - 1f;
                filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.18f);

                float impactEnvelope = Mathf.Exp(-time * 25f) * Mathf.Clamp01(time * 520f);
                float scuffTime = Mathf.Max(0f, time - 0.035f);
                float scuffEnvelope = Mathf.Exp(-scuffTime * 28f) * Mathf.Clamp01(scuffTime * 80f);
                float soleImpact = Mathf.Sin(2f * Mathf.PI * 112f * toneShift * time) * impactEnvelope * 0.19f;
                float heelTick = Mathf.Sin(2f * Mathf.PI * 760f * toneShift * time) * impactEnvelope * 0.045f;
                float rubberScuff = filteredNoise * scuffEnvelope * 0.055f;
                samples[i] = soleImpact + heelTick + rubberScuff;
            }

            return CreateClip(name, samples);
        }

        private static AudioClip CreateClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
