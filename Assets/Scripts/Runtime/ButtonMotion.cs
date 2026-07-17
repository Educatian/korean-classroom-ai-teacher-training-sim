using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class ButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.025f;
        [SerializeField] private float pressedScale = 0.965f;
        [SerializeField] private float duration = 0.11f;

        private RectTransform rect;
        private Button button;
        private AudioSource audioSource;
        private Coroutine motion;

        private void Awake()
        {
            rect = (RectTransform)transform;
            button = GetComponent<Button>();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            if (button != null)
            {
                button.onClick.AddListener(PlayClick);
            }
        }

        private void OnDestroy()
        {
            button?.onClick.RemoveListener(PlayClick);
        }

        public void OnPointerEnter(PointerEventData eventData) => AnimateTo(hoverScale);
        public void OnPointerExit(PointerEventData eventData) => AnimateTo(1f);
        public void OnPointerDown(PointerEventData eventData) => AnimateTo(pressedScale);
        public void OnPointerUp(PointerEventData eventData) => AnimateTo(eventData.pointerEnter == gameObject ? hoverScale : 1f);

        private void AnimateTo(float scale)
        {
            if (button != null && !button.interactable)
            {
                scale = 1f;
            }

            if (motion != null)
            {
                StopCoroutine(motion);
            }

            motion = StartCoroutine(Animate(scale));
        }

        private void PlayClick()
        {
            if (audioSource != null && audioSource.isActiveAndEnabled && button != null && button.interactable)
            {
                audioSource.pitch = 0.99f + Mathf.Repeat(Time.unscaledTime * 0.37f, 0.02f);
                audioSource.PlayOneShot(ProceduralAudioClips.UiClick, 0.58f);
            }
        }

        private IEnumerator Animate(float target)
        {
            Vector3 from = rect.localScale;
            Vector3 to = Vector3.one * target;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                rect.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            rect.localScale = to;
        }
    }
}
