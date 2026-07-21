using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Keeps the desktop post-processing stack off the Quest GPU: on Android the
    /// full layer is disabled; ambient occlusion is desktop-only either way.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PostFxPlatformGuard : MonoBehaviour
    {
        private void Awake()
        {
            var layer = GetComponent<PostProcessLayer>();
            if (layer == null)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            layer.enabled = false;
#endif
        }
    }
}
