using System.Collections;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class NpcSpeechPerformance : MonoBehaviour
    {
        private const int SpeechOverrideSource = 4101;
        private readonly float[] outputSamples = new float[128];
        private NpcPerformance performance;
        private StudentSpeechSynthesizer synthesizer;
        private AudioSource voiceSource;
        private Coroutine speech;
        private AudioClip generatedClip;
        private int speechGeneration;
        private StudentSpeechProsody currentProsody;
        private string currentProviderRoute = "scheduled-lipsync";

        public string VoiceStatus { get; private set; } = StudentSpeechSynthesizer.VoiceDisclosure;

        private void Awake()
        {
            performance = GetComponent<NpcPerformance>();
            synthesizer = GetComponent<StudentSpeechSynthesizer>();
            if (synthesizer == null)
            {
                synthesizer = gameObject.AddComponent<StudentSpeechSynthesizer>();
            }

            voiceSource = GetComponent<AudioSource>();
            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
            }

            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0.82f;
            voiceSource.minDistance = 0.7f;
            voiceSource.maxDistance = 10f;
            voiceSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        public void Speak(string text, ActionUnitDirective directive)
        {
            Speak(text, directive, new AffectVector(0f, 0.45f, 0f));
        }

        public void Speak(string text, ActionUnitDirective directive, AffectVector affect)
        {
            StopSpeaking();
            int generation = speechGeneration;
            ApplyDirective(directive);
            StudentSpeechProsody prosody = StudentSpeechProsodyPlanner.Plan(text, affect);
            currentProsody = prosody;
            currentProviderRoute = PreferredProviderRoute();
            speech = StartCoroutine(AnimateScheduledSpeech(text, prosody));
            VoiceStatus = StudentSpeechSynthesizer.VoiceDisclosure + " · 음성 준비 중";
            synthesizer.Synthesize(
                text,
                affect,
                (clip, resolvedProsody) =>
                {
                    if (generation != speechGeneration || clip == null)
                    {
                        if (clip != null)
                        {
                            Destroy(clip);
                        }
                        return;
                    }

                    if (speech != null)
                    {
                        StopCoroutine(speech);
                    }

                    generatedClip = clip;
                    voiceSource.clip = clip;
                    voiceSource.pitch = Mathf.Clamp(resolvedProsody.rate, 0.8f, 1.18f);
                    voiceSource.volume = resolvedProsody.volume;
                    voiceSource.Play();
                    VoiceStatus = StudentSpeechSynthesizer.VoiceDisclosure + " · 파형 립싱크";
                    Debug.Log($"STUDENT_TTS_OK provider={currentProviderRoute} duration={clip.length:0.00}s");
                    speech = StartCoroutine(AnimateAudioSpeech());
                },
                error =>
                {
                    if (generation == speechGeneration)
                    {
                        VoiceStatus = StudentSpeechSynthesizer.VoiceDisclosure + " · 무음 립싱크";
                        Debug.LogWarning(error);
                    }
                });
        }

        public StudentSpeechTelemetry CaptureTelemetry()
        {
            return new StudentSpeechTelemetry
            {
                requested = true,
                providerRoute = currentProviderRoute,
                rate = currentProsody.rate,
                pitchSemitones = currentProsody.pitchSemitones,
                volume = currentProsody.volume,
                commaPauseMilliseconds = currentProsody.commaPauseMilliseconds,
                sentencePauseMilliseconds = currentProsody.sentencePauseMilliseconds,
                disclosure = StudentSpeechSynthesizer.VoiceDisclosure
            };
        }

        private static string PreferredProviderRoute()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                ? "windows-sapi"
                : "openai-audio-api";
#else
            return "secure-proxy-required";
#endif
        }
        public void StopSpeaking()
        {
            speechGeneration++;
            synthesizer?.Cancel();
            if (speech != null)
            {
                StopCoroutine(speech);
                speech = null;
            }

            if (voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = null;
            }

            if (generatedClip != null)
            {
                Destroy(generatedClip);
                generatedClip = null;
            }

            ClearSpeechOverrides();
        }

        private void OnDisable()
        {
            StopSpeaking();
        }

        private IEnumerator AnimateScheduledSpeech(string text, StudentSpeechProsody prosody)
        {
            float elapsed = 0f;
            while (elapsed < prosody.estimatedDurationSeconds)
            {
                elapsed += Time.deltaTime;
                ApplyMouth(StudentSpeechProsodyPlanner.ScheduledMouthEnvelope(elapsed, text, prosody));
                yield return null;
            }

            speech = null;
            ClearSpeechOverrides();
        }

        private IEnumerator AnimateAudioSpeech()
        {
            while (voiceSource != null && voiceSource.isPlaying)
            {
                voiceSource.GetOutputData(outputSamples, 0);
                float sumSquares = 0f;
                for (int index = 0; index < outputSamples.Length; index++)
                {
                    sumSquares += outputSamples[index] * outputSamples[index];
                }

                float rms = Mathf.Sqrt(sumSquares / outputSamples.Length);
                ApplyMouth(Mathf.Clamp01(rms * 9.5f));
                yield return null;
            }

            speech = null;
            ClearSpeechOverrides();
            if (generatedClip != null)
            {
                Destroy(generatedClip);
                generatedClip = null;
            }
        }

        private void ApplyMouth(float envelope)
        {
            if (performance == null)
            {
                return;
            }

            performance.SetActionUnit(
                FacialActionUnit.AU25LipsPart,
                Mathf.Lerp(0.04f, 0.62f, envelope),
                SpeechOverrideSource);
            performance.SetActionUnit(
                FacialActionUnit.AU26JawDrop,
                Mathf.Lerp(0.02f, 0.31f, envelope),
                SpeechOverrideSource);
        }

        private void ApplyDirective(ActionUnitDirective directive)
        {
            ClearSpeechOverrides();
            if (directive == null || performance == null)
            {
                return;
            }

            performance.SetActionUnit(FacialActionUnit.AU1InnerBrowRaiser, directive.au1, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU2OuterBrowRaiser, directive.au2, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU4BrowLowerer, directive.au4, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU5UpperLidRaiser, directive.au5, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU6CheekRaiser, directive.au6, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU7LidTightener, directive.au7, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU9NoseWrinkler, directive.au9, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU12LipCornerPuller, directive.au12, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU15LipCornerDepressor, directive.au15, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU17ChinRaiser, directive.au17, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU20LipStretcher, directive.au20, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU23LipTightener, directive.au23, SpeechOverrideSource);
            performance.SetActionUnit(FacialActionUnit.AU24LipPressor, directive.au24, SpeechOverrideSource);
        }

        private void ClearSpeechOverrides()
        {
            performance?.ClearActionUnitOverrides(SpeechOverrideSource);
        }
    }
}
