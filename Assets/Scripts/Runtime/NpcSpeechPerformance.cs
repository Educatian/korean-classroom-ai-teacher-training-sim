using System.Collections;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class NpcSpeechPerformance : MonoBehaviour
    {
        private const int SpeechOverrideSource = 4101;
        private NpcPerformance performance;
        private Coroutine speech;

        private void Awake()
        {
            performance = GetComponent<NpcPerformance>();
        }

        public void Speak(string text, ActionUnitDirective directive)
        {
            StopSpeaking();
            ApplyDirective(directive);
            int characterCount = string.IsNullOrEmpty(text) ? 1 : text.Length;
            speech = StartCoroutine(AnimateSpeech(Mathf.Clamp(characterCount * 0.055f, 1.1f, 5.5f)));
        }

        public void StopSpeaking()
        {
            if (speech != null)
            {
                StopCoroutine(speech);
                speech = null;
            }

            ClearSpeechOverrides();
        }

        private void OnDisable()
        {
            StopSpeaking();
        }

        private IEnumerator AnimateSpeech(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float syllable = Mathf.Abs(Mathf.Sin(elapsed * 11.5f));
                performance.SetActionUnit(
                    FacialActionUnit.AU25LipsPart,
                    Mathf.Lerp(0.12f, 0.58f, syllable),
                    SpeechOverrideSource);
                performance.SetActionUnit(
                    FacialActionUnit.AU26JawDrop,
                    Mathf.Lerp(0.04f, 0.28f, syllable),
                    SpeechOverrideSource);
                yield return null;
            }

            speech = null;
            ClearSpeechOverrides();
        }

        private void ApplyDirective(ActionUnitDirective directive)
        {
            ClearSpeechOverrides();
            if (directive == null)
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
