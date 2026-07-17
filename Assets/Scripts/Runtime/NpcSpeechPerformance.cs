using System.Collections;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class NpcSpeechPerformance : MonoBehaviour
    {
        private NpcPerformance performance;
        private Coroutine speech;

        private void Awake()
        {
            performance = GetComponent<NpcPerformance>();
        }

        public void Speak(string text, ActionUnitDirective directive)
        {
            if (speech != null)
            {
                StopCoroutine(speech);
            }

            ApplyDirective(directive);
            speech = StartCoroutine(AnimateSpeech(Mathf.Clamp(text.Length * 0.055f, 1.1f, 5.5f)));
        }

        private IEnumerator AnimateSpeech(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float syllable = Mathf.Abs(Mathf.Sin(elapsed * 11.5f));
                performance.SetActionUnit(FacialActionUnit.AU25LipsPart, Mathf.Lerp(0.12f, 0.58f, syllable));
                performance.SetActionUnit(FacialActionUnit.AU26JawDrop, Mathf.Lerp(0.04f, 0.28f, syllable));
                yield return null;
            }

            performance.ReleaseActionUnit(FacialActionUnit.AU25LipsPart);
            performance.ReleaseActionUnit(FacialActionUnit.AU26JawDrop);
        }

        private void ApplyDirective(ActionUnitDirective directive)
        {
            performance.ClearActionUnitOverrides();
            if (directive == null)
            {
                return;
            }

            performance.SetActionUnit(FacialActionUnit.AU1InnerBrowRaiser, directive.au1);
            performance.SetActionUnit(FacialActionUnit.AU2OuterBrowRaiser, directive.au2);
            performance.SetActionUnit(FacialActionUnit.AU4BrowLowerer, directive.au4);
            performance.SetActionUnit(FacialActionUnit.AU5UpperLidRaiser, directive.au5);
            performance.SetActionUnit(FacialActionUnit.AU6CheekRaiser, directive.au6);
            performance.SetActionUnit(FacialActionUnit.AU7LidTightener, directive.au7);
            performance.SetActionUnit(FacialActionUnit.AU9NoseWrinkler, directive.au9);
            performance.SetActionUnit(FacialActionUnit.AU12LipCornerPuller, directive.au12);
            performance.SetActionUnit(FacialActionUnit.AU15LipCornerDepressor, directive.au15);
            performance.SetActionUnit(FacialActionUnit.AU17ChinRaiser, directive.au17);
            performance.SetActionUnit(FacialActionUnit.AU20LipStretcher, directive.au20);
            performance.SetActionUnit(FacialActionUnit.AU23LipTightener, directive.au23);
            performance.SetActionUnit(FacialActionUnit.AU24LipPressor, directive.au24);
        }
    }
}
