using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class GymnasiumPbrPolishTests
    {
        [TestCase("M_GymFloorMaple", 0.78f, 0.88f)]
        [TestCase("M_GymWallSlat", 0.14f, 0.32f)]
        [TestCase("M_GymStageWoodDark", 0.30f, 0.50f)]
        [TestCase("M_GymDoorWood", 0.20f, 0.42f)]
        public void HeroWoodMaterials_UseNormalMappedStandardPbr(
            string materialName,
            float minimumSmoothness,
            float maximumSmoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                $"Assets/Materials/{materialName}.mat");

            Assert.That(material, Is.Not.Null);
            Assert.That(material.shader.name, Is.EqualTo("Standard"));
            Assert.That(material.GetTexture("_BumpMap"), Is.Not.Null,
                $"{materialName} must have a normal map for close-range Quest rendering.");
            Assert.That(material.IsKeywordEnabled("_NORMALMAP"), Is.True);
            Assert.That(material.GetFloat("_Glossiness"), Is.InRange(minimumSmoothness, maximumSmoothness));
        }

        [Test]
        public void GymnasiumScene_UsesBakedProbesAndStaticEnvironment()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/KoreanGymnasiumTraining.unity");

            ReflectionProbe[] probes = Object.FindObjectsByType<ReflectionProbe>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            ReflectionProbe[] gymProbes = probes.Where(probe => probe.name.StartsWith("Probe_Gym")).ToArray();
            GameObject gymRoot = GameObject.Find("50_GYMNASIUM");
            LightProbeGroup lightProbeGroup = Object.FindAnyObjectByType<LightProbeGroup>();

            Assert.That(gymRoot, Is.Not.Null);
            Assert.That(gymRoot.isStatic, Is.True);
            Assert.That(gymProbes, Has.Length.EqualTo(2));
            Assert.That(gymProbes.All(probe => probe.mode == ReflectionProbeMode.Custom), Is.True);
            Assert.That(gymProbes.All(probe => probe.customBakedTexture != null), Is.True,
                "Quest must ship the probe cubemaps instead of rendering them at runtime.");
            Assert.That(gymProbes.All(probe => probe.boxProjection && probe.resolution <= 128), Is.True);
            Assert.That(lightProbeGroup, Is.Not.Null);
            Assert.That(lightProbeGroup.probePositions.Length, Is.GreaterThanOrEqualTo(24));
        }

        [Test]
        public void GymnasiumScene_LimitsRealtimeLocalLightingForQuest()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/KoreanGymnasiumTraining.unity");

            Light[] localLights = Object.FindObjectsByType<Light>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Where(light => light.type == LightType.Point || light.type == LightType.Spot)
                .ToArray();

            Assert.That(localLights.Count(light => light.lightmapBakeType != LightmapBakeType.Baked),
                Is.LessThanOrEqualTo(2));
            Assert.That(localLights.Count(light => light.shadows != LightShadows.None),
                Is.LessThanOrEqualTo(2));
        }

        [Test]
        public void GymnasiumScene_UsesLowCostCeilingLightSheenOnCoatedFloor()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/KoreanGymnasiumTraining.unity");

            GameObject sheenRoot = GameObject.Find("FloorLightSheen");
            Material sheenMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/M_GymFloorSheen.mat");

            Assert.That(sheenRoot, Is.Not.Null);
            Assert.That(sheenRoot.GetComponentsInChildren<MeshRenderer>(), Has.Length.EqualTo(8));
            Assert.That(sheenMaterial, Is.Not.Null);
            Assert.That(sheenMaterial.shader.name, Is.EqualTo("Unlit/Transparent"));
            Assert.That(sheenMaterial.mainTexture, Is.Not.Null);
        }
    }
}
