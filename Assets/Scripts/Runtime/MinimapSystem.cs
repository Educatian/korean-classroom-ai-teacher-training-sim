using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Desktop minimap: a top-down orthographic camera renders the floor below
    /// ceiling height into a RenderTexture shown in the HUD's top-left corner,
    /// with a rotating teacher arrow. North-up, follows the main camera. Toggled
    /// with M or its fold pill. Skipped when an XR display is active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapSystem : MonoBehaviour
    {
        private const float ViewHalfSize = 8.5f;
        private const float CameraHeight = 3.05f;
        private const float NearClip = 0.55f;

        private Camera mapCamera;
        private RenderTexture mapTexture;
        private RectTransform frame;
        private RectTransform arrow;
        private Transform followTarget;

        public static MinimapSystem Install(Canvas canvas, TMP_FontAsset font, Camera sceneCamera)
        {
            if (canvas == null || sceneCamera == null)
            {
                return null;
            }

            if (UnityEngine.XR.XRSettings.isDeviceActive)
            {
                return null;
            }

            var existing = canvas.GetComponentInChildren<MinimapSystem>();
            if (existing != null)
            {
                return existing;
            }

            var systemObject = new GameObject("MinimapSystem", typeof(MinimapSystem));
            var system = systemObject.GetComponent<MinimapSystem>();
            system.Build(canvas, font, sceneCamera);
            return system;
        }

        private void Build(Canvas canvas, TMP_FontAsset font, Camera sceneCamera)
        {
            followTarget = sceneCamera.transform;

            mapTexture = new RenderTexture(256, 256, 16)
            {
                name = "MinimapRT",
                antiAliasing = 2
            };

            var cameraObject = new GameObject("MinimapCamera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            mapCamera = cameraObject.GetComponent<Camera>();
            mapCamera.orthographic = true;
            mapCamera.orthographicSize = ViewHalfSize;
            mapCamera.nearClipPlane = NearClip;
            mapCamera.farClipPlane = CameraHeight + 6f;
            mapCamera.targetTexture = mapTexture;
            mapCamera.clearFlags = CameraClearFlags.SolidColor;
            mapCamera.backgroundColor = new Color(0.03f, 0.07f, 0.09f, 1f);
            mapCamera.allowMSAA = true;
            // Renders below the ceiling plane so rooms read as a floor plan.
            mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var frameObject = new GameObject("MinimapFrame", typeof(RectTransform), typeof(Image));
            frame = (RectTransform)frameObject.transform;
            frame.SetParent(canvas.transform, false);
            frame.anchorMin = new Vector2(0f, 1f);
            frame.anchorMax = new Vector2(0f, 1f);
            frame.pivot = new Vector2(0f, 1f);
            frame.anchoredPosition = new Vector2(14f, -64f);
            frame.sizeDelta = new Vector2(176f, 176f);
            frameObject.GetComponent<Image>().color = new Color(0.05f, 0.13f, 0.15f, 0.9f);

            var viewObject = new GameObject("MinimapView", typeof(RectTransform), typeof(RawImage));
            var viewRect = (RectTransform)viewObject.transform;
            viewRect.SetParent(frame, false);
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = new Vector2(4f, 4f);
            viewRect.offsetMax = new Vector2(-4f, -4f);
            RawImage view = viewObject.GetComponent<RawImage>();
            view.texture = mapTexture;

            var arrowObject = new GameObject("TeacherArrow", typeof(RectTransform), typeof(Image));
            arrow = (RectTransform)arrowObject.transform;
            arrow.SetParent(frame, false);
            arrow.anchorMin = new Vector2(0.5f, 0.5f);
            arrow.anchorMax = new Vector2(0.5f, 0.5f);
            arrow.sizeDelta = new Vector2(14f, 14f);
            Image arrowImage = arrowObject.GetComponent<Image>();
            Sprite arrowSprite = Resources.Load<Sprite>("Training/Icons/icon_guide");
            if (arrowSprite != null)
            {
                arrowImage.sprite = arrowSprite;
            }
            arrowImage.color = new Color(0.32f, 0.84f, 0.75f, 1f);

            if (font != null)
            {
                var labelObject = new GameObject("MinimapLabel", typeof(RectTransform));
                var labelRect = (RectTransform)labelObject.transform;
                labelRect.SetParent(frame, false);
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 0f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.offsetMin = new Vector2(4f, -22f);
                labelRect.offsetMax = new Vector2(-4f, -4f);
                var label = labelObject.AddComponent<TextMeshProUGUI>();
                label.font = font;
                label.fontSize = 11;
                label.color = new Color(0.62f, 0.72f, 0.74f, 1f);
                label.alignment = TextAlignmentOptions.Center;
                label.text = "M · 미니맵";
            }
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.M) && frame != null)
            {
                bool active = !frame.gameObject.activeSelf;
                frame.gameObject.SetActive(active);
                if (mapCamera != null)
                {
                    mapCamera.enabled = active;
                }
            }

            if (followTarget == null || mapCamera == null || !mapCamera.enabled)
            {
                return;
            }

            Vector3 anchor = followTarget.position;
            mapCamera.transform.position = new Vector3(anchor.x, CameraHeight, anchor.z);
            if (arrow != null)
            {
                float yaw = followTarget.eulerAngles.y;
                arrow.localRotation = Quaternion.Euler(0f, 0f, -yaw);
            }
        }

        private void OnDestroy()
        {
            if (mapTexture != null)
            {
                mapTexture.Release();
                Destroy(mapTexture);
            }
        }
    }
}
