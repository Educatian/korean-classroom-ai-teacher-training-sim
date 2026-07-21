using System.Collections.Generic;
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

            // --- Advanced pass: adaptation, photographic texture, reflections ---
            if (!profile.TryGetSettings(out AutoExposure exposure))
            {
                exposure = profile.AddSettings<AutoExposure>();
            }

            exposure.minLuminance.Override(-2f);
            exposure.maxLuminance.Override(2f);
            exposure.keyValue.Override(1f);
            exposure.speedUp.Override(2.2f);
            exposure.speedDown.Override(1.4f);

            if (!profile.TryGetSettings(out Grain grain))
            {
                grain = profile.AddSettings<Grain>();
            }

            grain.intensity.Override(0.12f);
            grain.size.Override(1.1f);
            grain.lumContrib.Override(0.75f);

            if (!profile.TryGetSettings(out ChromaticAberration aberration))
            {
                aberration = profile.AddSettings<ChromaticAberration>();
            }

            aberration.intensity.Override(0.06f);

            if (!profile.TryGetSettings(out ScreenSpaceReflections reflections))
            {
                reflections = profile.AddSettings<ScreenSpaceReflections>();
            }

            // Requires the deferred rendering path (set on cameras in AttachPostFx).
            reflections.preset.Override(ScreenSpaceReflectionPreset.Medium);
            reflections.distanceFade.Override(0.12f);
            reflections.vignette.Override(0.45f);

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

                // Deferred enables screen-space reflections on the glossy floors;
                // the platform guard reverts to forward on Android at runtime.
                camera.renderingPath = RenderingPath.DeferredShading;
                camera.allowHDR = true;

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
            // Gymnasium: polished maple court reads glossy; stage wood satin.
            SetMaterialFloat("M_GymFloorMaple", "_Glossiness", 0.62f);
            SetMaterialFloat("M_GymLineBlue", "_Glossiness", 0.58f);
            SetMaterialFloat("M_GymLineGreen", "_Glossiness", 0.58f);
            SetMaterialFloat("M_GymStageWoodDark", "_Glossiness", 0.42f);
            SetMaterialFloat("M_GymWindowGlass", "_Glossiness", 0.94f);
            SetMaterialFloat("M_GymPianoBlack", "_Glossiness", 0.7f);
            // Recovery room: satin birch floor, matte rug stays untouched.
            SetMaterialFloat("M_RecoveryFloorWood", "_Glossiness", 0.4f);
            ApplyDetailMaps();
        }

        private const string DetailWallPath = "Assets/Art/GeneratedMaterials/TX_Detail_WallMottle.png";
        private const string DetailFloorPath = "Assets/Art/GeneratedMaterials/TX_Detail_FloorScuff.png";
        private const string DetailNormalPath = "Assets/Art/GeneratedMaterials/TX_Detail_MicroNormal.png";

        private static void ApplyDetailMaps()
        {
            EnsureNormalImport(DetailNormalPath);
            var wallDetail = AssetDatabase.LoadAssetAtPath<Texture2D>(DetailWallPath);
            var floorDetail = AssetDatabase.LoadAssetAtPath<Texture2D>(DetailFloorPath);
            var microNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(DetailNormalPath);
            SetDetail("M_Wall", wallDetail, microNormal, 6f, 0.35f);
            SetDetail("M_CorridorWall", wallDetail, microNormal, 6f, 0.3f);
            SetDetail("M_RecoveryWall", wallDetail, microNormal, 5f, 0.3f);
            SetDetail("M_Floor", floorDetail, microNormal, 8f, 0.45f);
            SetDetail("M_CorridorFloor", floorDetail, microNormal, 8f, 0.4f);
        }

        private static void EnsureNormalImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        private static void SetDetail(
            string materialName,
            Texture2D detailAlbedo,
            Texture2D detailNormal,
            float tiling,
            float normalScale)
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
                if (material == null)
                {
                    continue;
                }

                material.EnableKeyword("_DETAIL_MULX2");
                if (detailAlbedo != null && material.HasProperty("_DetailAlbedoMap"))
                {
                    material.SetTexture("_DetailAlbedoMap", detailAlbedo);
                    material.SetTextureScale("_DetailAlbedoMap", new Vector2(tiling, tiling));
                }
                if (detailNormal != null && material.HasProperty("_DetailNormalMap"))
                {
                    material.SetTexture("_DetailNormalMap", detailNormal);
                    material.SetFloat("_DetailNormalMapScale", normalScale);
                }
                EditorUtility.SetDirty(material);
            }
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

            // Indoor grounding shadows: the two strongest point/spot lights per scene
            // cast soft shadows so characters and furniture stop floating visually.
            var localLights = new List<Light>();
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (light.type == LightType.Point || light.type == LightType.Spot)
                {
                    light.shadows = LightShadows.None;
                    localLights.Add(light);
                }
            }

            localLights.Sort((a, b) => b.intensity.CompareTo(a.intensity));
            for (int index = 0; index < Mathf.Min(2, localLights.Count); index++)
            {
                localLights[index].shadows = LightShadows.Soft;
                localLights[index].shadowStrength = 0.6f;
            }

            // Scene-typed trilight ambient: cool sky bounce outdoors, warm interior fill.
            string sceneName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
            bool outdoor = sceneName.Contains("Schoolyard");
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            if (outdoor)
            {
                RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.85f);
                RenderSettings.ambientEquatorColor = new Color(0.60f, 0.62f, 0.58f);
                RenderSettings.ambientGroundColor = new Color(0.30f, 0.36f, 0.26f);
                // Afternoon sun angle: long readable shadows from trees, goals, and
                // the apartment ring — near-noon light left the ground shadowless.
                // Ambient is pulled down outdoors so sun shadows keep their contrast
                // against the bright field instead of washing out.
                RenderSettings.ambientIntensity = 0.85f;
                foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional)
                    {
                        light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                        light.shadowStrength = 0.9f;
                    }
                }
            }
            else
            {
                RenderSettings.ambientSkyColor = new Color(0.72f, 0.72f, 0.70f);
                RenderSettings.ambientEquatorColor = new Color(0.58f, 0.56f, 0.53f);
                RenderSettings.ambientGroundColor = new Color(0.36f, 0.33f, 0.30f);
            }

            // Shadow fidelity for close-range character shots.
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, outdoor ? 110f : 55f);
            QualitySettings.shadowCascades = 4;
        }
    }
}
