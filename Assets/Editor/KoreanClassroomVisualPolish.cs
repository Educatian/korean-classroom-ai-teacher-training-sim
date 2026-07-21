using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace AdieLab.TeacherTraining.Editor
{
    /// <summary>
    /// Cinematic look pass for all training scenes: ACES tonemapping + color
    /// grading + soft bloom + desktop ambient occlusion, baked reflection probes
    /// for glass/floor response, and a material gloss pass. Idempotent — safe to
    /// run after any scene rebuild.
    /// </summary>
    public static class KoreanClassroomVisualPolish
    {
        private const string ProfileDirectory = "Assets/Art/PostFx";
        private const string ProfilePath = ProfileDirectory + "/TT_PostProfile.asset";
        private const string ResourcesPath =
            "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset";

        [MenuItem("Tools/Teacher Training/Apply Visual Polish To Open Scene")]
        public static void ApplyToOpenScene()
        {
            ApplyVisualPolish();
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        public static void ApplyVisualPolish()
        {
            PostProcessProfile profile = EnsureProfile();
            AttachPostFx(profile);
            EnsureReflectionProbes();
            PolishMaterials();
            TuneLighting();
        }

        private static PostProcessProfile EnsureProfile()
        {
            if (!Directory.Exists(ProfileDirectory))
            {
                Directory.CreateDirectory(ProfileDirectory);
                AssetDatabase.Refresh();
            }

            var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PostProcessProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            if (!profile.TryGetSettings(out ColorGrading grading))
            {
                grading = profile.AddSettings<ColorGrading>();
            }

            grading.gradingMode.Override(GradingMode.HighDefinitionRange);
            grading.tonemapper.Override(Tonemapper.ACES);
            grading.temperature.Override(6f);
            grading.postExposure.Override(0.25f);
            grading.saturation.Override(6f);
            grading.contrast.Override(8f);

            if (!profile.TryGetSettings(out Bloom bloom))
            {
                bloom = profile.AddSettings<Bloom>();
            }

            bloom.intensity.Override(1.1f);
            bloom.threshold.Override(1.05f);
            bloom.softKnee.Override(0.6f);

            if (!profile.TryGetSettings(out AmbientOcclusion occlusion))
            {
                occlusion = profile.AddSettings<AmbientOcclusion>();
            }

            occlusion.mode.Override(AmbientOcclusionMode.MultiScaleVolumetricObscurance);
            occlusion.intensity.Override(0.32f);
            occlusion.thicknessModifier.Override(1f);

            if (!profile.TryGetSettings(out Vignette vignette))
            {
                vignette = profile.AddSettings<Vignette>();
            }

            vignette.intensity.Override(0.16f);
            vignette.smoothness.Override(0.42f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void AttachPostFx(PostProcessProfile profile)
        {
            var resources = AssetDatabase.LoadAssetAtPath<PostProcessResources>(ResourcesPath);
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (camera.targetTexture != null || camera.gameObject.name == "MinimapCamera")
                {
                    continue;
                }

                var layer = camera.GetComponent<PostProcessLayer>();
                if (layer == null)
                {
                    layer = camera.gameObject.AddComponent<PostProcessLayer>();
                }

                if (resources != null)
                {
                    layer.Init(resources);
                }

                layer.volumeTrigger = camera.transform;
                layer.volumeLayer = 1 << LayerMask.NameToLayer("TransparentFX");
                layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                if (camera.GetComponent<PostFxPlatformGuard>() == null)
                {
                    camera.gameObject.AddComponent<PostFxPlatformGuard>();
                }
            }

            GameObject volumeObject = GameObject.Find("GlobalPostFxVolume");
            if (volumeObject == null)
            {
                volumeObject = new GameObject("GlobalPostFxVolume");
            }

            volumeObject.layer = LayerMask.NameToLayer("TransparentFX");
            var volume = volumeObject.GetComponent<PostProcessVolume>();
            if (volume == null)
            {
                volume = volumeObject.AddComponent<PostProcessVolume>();
            }

            volume.isGlobal = true;
            volume.priority = 10f;
            volume.profile = profile;
        }

        private static void EnsureReflectionProbes()
        {
            CreateProbe("Probe_Classroom", new Vector3(0f, 1.7f, 0f), new Vector3(14.4f, 3.6f, 10.4f));
            CreateProbe("Probe_CorridorEast", new Vector3(12f, 1.7f, 6.6f), new Vector3(20f, 3.6f, 3.6f));
            CreateProbe("Probe_CorridorWest", new Vector3(-12f, 1.7f, 6.6f), new Vector3(20f, 3.6f, 3.6f));
            CreateProbe("Probe_RecoveryRoom", new Vector3(-11f, 1.7f, 2.6f), new Vector3(6f, 3.6f, 5.4f));
        }

        private static void CreateProbe(string name, Vector3 center, Vector3 size)
        {
            GameObject probeObject = GameObject.Find(name);
            if (probeObject == null)
            {
                probeObject = new GameObject(name);
            }

            probeObject.transform.position = center;
            var probe = probeObject.GetComponent<ReflectionProbe>();
            if (probe == null)
            {
                probe = probeObject.AddComponent<ReflectionProbe>();
            }

            // Realtime once-at-awake avoids editor bake APIs; six-face render happens
            // one time per scene load.
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.boxProjection = true;
            probe.size = size;
            probe.resolution = 128;
            probe.intensity = 0.85f;
        }

        private static void PolishMaterials()
        {
            SetMaterialFloat("M_Floor", "_Glossiness", 0.52f);
            SetMaterialFloat("M_CorridorFloor", "_Glossiness", 0.48f);
            SetMaterialFloat("M_Glass", "_Glossiness", 0.94f);
            SetMaterialFloat("M_Wall", "_Glossiness", 0.18f);
            SetMaterialFloat("M_CorridorWall", "_Glossiness", 0.16f);
        }

        private static void SetMaterialFloat(string materialName, string property, float value)
        {
            string[] guids = AssetDatabase.FindAssets(materialName + " t:Material");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != materialName)
                {
                    continue;
                }

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null && material.HasProperty(property))
                {
                    material.SetFloat(property, value);
                    EditorUtility.SetDirty(material);
                }
            }
        }

        private static void TuneLighting()
        {
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(1f, 0.956f, 0.89f, 1f);
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = 0.72f;
                }
            }

            RenderSettings.ambientIntensity = 1.02f;
        }
    }
}
