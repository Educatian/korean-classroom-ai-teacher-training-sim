using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Mouse hover/click selection for students. Hovering brightens the student's
    /// renderers slightly; clicking selects the student as the current dialogue
    /// target. The focal student stays the default target.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StudentDialogueSelector : MonoBehaviour
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly Color HighlightTint = new Color(1.22f, 1.22f, 1.18f, 1f);

        private Camera rayCamera;
        private NpcPerformance hovered;
        private readonly System.Collections.Generic.Dictionary<NpcPerformance, Renderer[]> rendererCache =
            new System.Collections.Generic.Dictionary<NpcPerformance, Renderer[]>();

        public event Action<NpcPerformance> StudentClicked;

        public static StudentDialogueSelector Install(
            Camera camera,
            NpcPerformance focal,
            NpcPerformance[] classmates)
        {
            if (camera == null || focal == null)
            {
                return null;
            }

            var selector = camera.gameObject.GetComponent<StudentDialogueSelector>();
            if (selector == null)
            {
                selector = camera.gameObject.AddComponent<StudentDialogueSelector>();
            }

            selector.rayCamera = camera;
            selector.EnsureCollider(focal);
            if (classmates != null)
            {
                foreach (NpcPerformance classmate in classmates)
                {
                    selector.EnsureCollider(classmate);
                }
            }

            return selector;
        }

        private void EnsureCollider(NpcPerformance student)
        {
            if (student == null || student.GetComponent<Collider>() != null)
            {
                return;
            }

            var capsule = student.gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.65f, 0f);
            capsule.height = 1.35f;
            capsule.radius = 0.28f;
        }

        private void Update()
        {
            if (rayCamera == null)
            {
                return;
            }

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            NpcPerformance target = null;
            if (!overUi)
            {
                Ray ray = rayCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 25f))
                {
                    target = hit.collider.GetComponentInParent<NpcPerformance>();
                }
            }

            if (target != hovered)
            {
                SetHighlight(hovered, false);
                hovered = target;
                SetHighlight(hovered, true);
            }

            if (hovered != null && Input.GetMouseButtonDown(0))
            {
                StudentClicked?.Invoke(hovered);
            }
        }

        private void OnDisable()
        {
            SetHighlight(hovered, false);
            hovered = null;
        }

        private void SetHighlight(NpcPerformance student, bool active)
        {
            if (student == null)
            {
                return;
            }

            if (!rendererCache.TryGetValue(student, out Renderer[] renderers))
            {
                renderers = student.GetComponentsInChildren<SkinnedMeshRenderer>();
                rendererCache[student] = renderers;
            }

            var block = new MaterialPropertyBlock();
            foreach (Renderer renderer in renderers)
            {
                if (active)
                {
                    renderer.GetPropertyBlock(block);
                    block.SetColor(ColorProperty, HighlightTint);
                    renderer.SetPropertyBlock(block);
                }
                else
                {
                    renderer.SetPropertyBlock(null);
                }
            }
        }
    }
}
