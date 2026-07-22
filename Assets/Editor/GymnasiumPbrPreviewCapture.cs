using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdieLab.TeacherTraining.Editor
{
    public static class GymnasiumPbrPreviewCapture
    {
        private const string ScenePath = "Assets/Scenes/KoreanGymnasiumTraining.unity";
        private const string OutputDirectory = ".qa-previews/GymnasiumPbr";

        public static void CaptureFromCommandLine()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Camera camera = GameObject.Find("20_SYSTEMS/TeacherCamera")?.GetComponent<Camera>();
                if (camera == null)
                {
                    throw new InvalidOperationException("TeacherCamera is missing from the gymnasium scene.");
                }

                foreach (TMP_Text label in UnityEngine.Object.FindObjectsByType<TMP_Text>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    if (label.font != null && !string.IsNullOrEmpty(label.text))
                    {
                        label.font.TryAddCharacters(label.text, out _);
                    }
                    label.ForceMeshUpdate(true, true);
                }
                Canvas.ForceUpdateCanvases();

                Directory.CreateDirectory(OutputDirectory);
                Capture(camera, Path.Combine(OutputDirectory, "Gymnasium_PBR_TeacherPOV.png"));

                camera.transform.SetPositionAndRotation(
                    new Vector3(-11.8f, 4.9f, -15.8f),
                    Quaternion.LookRotation(new Vector3(2.5f, 2.7f, 15.8f) - new Vector3(-11.8f, 4.9f, -15.8f)));
                camera.fieldOfView = 60f;
                Capture(camera, Path.Combine(OutputDirectory, "Gymnasium_PBR_Wide.png"));

                Debug.Log("GYMNASIUM_PBR_PREVIEW_CAPTURE_OK");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BakeReflectionProbesFromCommandLine()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                const string lightingDirectory = "Assets/Art/Lighting/Gymnasium";
                Directory.CreateDirectory(lightingDirectory);
                AssetDatabase.Refresh();

                ReflectionProbe[] probes = UnityEngine.Object.FindObjectsByType<ReflectionProbe>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                int bakedCount = 0;
                foreach (ReflectionProbe probe in probes)
                {
                    if (!probe.name.StartsWith("Probe_Gym"))
                    {
                        continue;
                    }

                    string outputPath = $"{lightingDirectory}/{probe.name}.exr";
                    if (AssetDatabase.LoadAssetAtPath<Cubemap>(outputPath) != null)
                    {
                        AssetDatabase.DeleteAsset(outputPath);
                    }
                    probe.mode = ReflectionProbeMode.Baked;
                    if (!Lightmapping.BakeReflectionProbe(probe, outputPath))
                    {
                        throw new InvalidOperationException($"Failed to bake {probe.name}.");
                    }
                    AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
                    Cubemap cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(outputPath);
                    if (cubemap == null)
                    {
                        throw new InvalidOperationException($"Baked cubemap was not imported for {probe.name}.");
                    }
                    // Custom mode serializes the pre-baked cubemap directly into
                    // the scene and is reliable on Quest where realtime probes are disabled.
                    probe.mode = ReflectionProbeMode.Custom;
                    probe.customBakedTexture = cubemap;
                    EditorUtility.SetDirty(probe);
                    bakedCount++;
                }

                if (bakedCount != 2)
                {
                    throw new InvalidOperationException($"Expected two gym probes, baked {bakedCount}.");
                }

                AssetDatabase.Refresh();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                Debug.Log("GYMNASIUM_REFLECTION_PROBE_BAKE_OK");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Capture(Camera camera, string outputPath)
        {
            const int width = 1920;
            const int height = 1080;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false, false);
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
