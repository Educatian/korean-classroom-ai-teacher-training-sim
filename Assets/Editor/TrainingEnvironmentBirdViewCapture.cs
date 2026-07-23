using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdieLab.TeacherTraining.Editor
{
    /// <summary>
    /// Produces orthographic, ceiling-free plan views from the authored scene geometry.
    /// The capture is intentionally generated from scene coordinates rather than a
    /// hand-drawn approximation so it remains useful as a spatial QA reference.
    /// </summary>
    public static class TrainingEnvironmentBirdViewCapture
    {
        private const int CaptureWidth = 1800;
        private const int CaptureHeight = 1350;

        private readonly struct SceneCapture
        {
            public SceneCapture(string scenePath, string environmentRoot, string outputName)
            {
                ScenePath = scenePath;
                EnvironmentRoot = environmentRoot;
                OutputName = outputName;
            }

            public string ScenePath { get; }
            public string EnvironmentRoot { get; }
            public string OutputName { get; }
        }

        private static readonly SceneCapture[] Captures =
        {
            new SceneCapture(
                "Assets/Scenes/KoreanClassroomTraining.unity",
                "00_ENVIRONMENT",
                "01-general-classroom.png"),
            new SceneCapture(
                "Assets/Scenes/KoreanClassroomCircleTraining.unity",
                "00_ENVIRONMENT",
                "02-circle-discussion.png"),
            new SceneCapture(
                "Assets/Scenes/KoreanClassroomRecoveryTraining.unity",
                "30_RECOVERY_ROOM",
                "03-recovery-room.png"),
            new SceneCapture(
                "Assets/Scenes/KoreanSchoolyardTraining.unity",
                "40_SCHOOLYARD",
                "04-schoolyard.png"),
            new SceneCapture(
                "Assets/Scenes/KoreanGymnasiumTraining.unity",
                "50_GYMNASIUM",
                "05-gymnasium.png")
        };

        [MenuItem("Tools/Teacher Training/Capture Environment Bird Views")]
        public static void CaptureAll()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unable to resolve the Unity project root.");
            string outputDirectory = Path.Combine(projectRoot, "docs", "DevCaptures", "BirdView");
            Directory.CreateDirectory(outputDirectory);

            foreach (SceneCapture capture in Captures)
            {
                CaptureScene(capture, outputDirectory);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Bird-view capture complete: {outputDirectory}");
        }

        private static void CaptureScene(SceneCapture capture, string outputDirectory)
        {
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(
                capture.ScenePath,
                OpenSceneMode.Single);

            GameObject environment = GameObject.Find(capture.EnvironmentRoot);
            if (environment == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{capture.ScenePath}' does not contain '{capture.EnvironmentRoot}'.");
            }

            GameObject students = GameObject.Find("10_STUDENTS");
            Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>()
                .Where(renderer => renderer.gameObject.scene == scene)
                .ToArray();
            var rendererStates = new Dictionary<Renderer, bool>(renderers.Length);

            try
            {
                foreach (Renderer renderer in renderers)
                {
                    rendererStates[renderer] = renderer.enabled;
                    bool belongsToMap = IsDescendantOf(renderer.transform, environment.transform) ||
                                        (students != null && IsDescendantOf(renderer.transform, students.transform));
                    renderer.enabled = renderer.enabled && belongsToMap && !IsOverheadOccluder(renderer.name);
                }

                Bounds bounds = CalculateBounds(renderers);
                if (bounds.size.sqrMagnitude < 0.01f)
                {
                    throw new InvalidOperationException($"No visible map geometry found in '{capture.ScenePath}'.");
                }

                RenderBirdView(bounds, Path.Combine(outputDirectory, capture.OutputName));
            }
            finally
            {
                foreach (KeyValuePair<Renderer, bool> state in rendererStates)
                {
                    if (state.Key != null)
                    {
                        state.Key.enabled = state.Value;
                    }
                }
            }
        }

        private static void RenderBirdView(Bounds bounds, string outputPath)
        {
            var cameraObject = new GameObject("__BirdViewCaptureCamera", typeof(Camera));
            var lightObject = new GameObject("__BirdViewCaptureLight", typeof(Light));
            RenderTexture target = null;
            Texture2D image = null;

            AmbientMode previousAmbientMode = RenderSettings.ambientMode;
            Color previousAmbientLight = RenderSettings.ambientLight;

            try
            {
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.aspect = CaptureWidth / (float)CaptureHeight;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.075f, 0.085f, 1f);
                camera.allowHDR = true;
                camera.allowMSAA = true;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = Mathf.Max(80f, bounds.size.y + 60f);
                camera.transform.SetPositionAndRotation(
                    new Vector3(bounds.center.x, bounds.max.y + 32f, bounds.center.z),
                    Quaternion.Euler(90f, 0f, 0f));

                float horizontalSize = bounds.extents.x / camera.aspect;
                camera.orthographicSize = Mathf.Max(bounds.extents.z, horizontalSize) * 1.10f;

                Light key = lightObject.GetComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.05f;
                key.color = new Color(1f, 0.965f, 0.90f, 1f);
                key.shadows = LightShadows.None;
                lightObject.transform.rotation = Quaternion.Euler(52f, -32f, 0f);

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.68f, 1f);

                target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 4,
                    name = "BirdViewCaptureTarget"
                };
                target.Create();
                camera.targetTexture = target;
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                image = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false, false);
                image.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0, false);
                image.Apply(false, false);
                RenderTexture.active = previous;

                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                Debug.Log($"Captured {outputPath} from bounds center={bounds.center}, size={bounds.size}");
            }
            finally
            {
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbientLight;
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (image != null)
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Bounds CalculateBounds(IEnumerable<Renderer> renderers)
        {
            bool initialized = false;
            Bounds result = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!initialized)
                {
                    result = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(renderer.bounds);
                }
            }
            return result;
        }

        private static bool IsDescendantOf(Transform candidate, Transform ancestor)
        {
            return candidate == ancestor || candidate.IsChildOf(ancestor);
        }

        private static bool IsOverheadOccluder(string objectName)
        {
            string normalized = objectName.ToLowerInvariant();
            return normalized.Contains("ceiling") ||
                   normalized.Contains("roof") ||
                   normalized.Contains("skydome") ||
                   normalized.Contains("cloud");
        }
    }
}
