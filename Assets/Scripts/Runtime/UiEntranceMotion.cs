using UnityEngine;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// One-shot entrance motion for runtime-built UI: fades a CanvasGroup in while
    /// easing scale from 96% to 100%. Removes itself when finished.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiEntranceMotion : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float duration = 0.24f;

        private CanvasGroup group;
        private float elapsed;

        public static void Play(GameObject target, float duration = 0.24f)
        {
            if (target == null)
            {
                return;
            }

            var motion = target.GetComponent<UiEntranceMotion>();
            if (motion == null)
            {
                motion = target.AddComponent<UiEntranceMotion>();
            }

            motion.duration = Mathf.Max(0.05f, duration);
            motion.Restart();
        }

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Restart()
        {
            if (group == null)
            {
                Awake();
            }

            elapsed = 0f;
            group.alpha = 0f;
            transform.localScale = Vector3.one * 0.96f;
            enabled = true;
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            group.alpha = progress;
            transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, progress);
            if (progress >= 1f)
            {
                enabled = false;
            }
        }
    }
}
