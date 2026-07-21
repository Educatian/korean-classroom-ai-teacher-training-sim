using NUnit.Framework;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class MetaQuestConfigurationTests
    {
        [Test]
        public void OpenXrLoader_IsAssignedForDesktopAndAndroidWithoutAutomaticStartup()
        {
            AssertTarget(BuildTargetGroup.Standalone);
            AssertTarget(BuildTargetGroup.Android);
        }

        [Test]
        public void AndroidOpenXr_EnablesMetaQuestAndTouchControllers()
        {
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.GetFeature<MetaQuestFeature>()?.enabled, Is.True);
            Assert.That(settings.GetFeature<OculusTouchControllerProfile>()?.enabled, Is.True);
            Assert.That(settings.GetFeature<MetaQuestTouchPlusControllerProfile>()?.enabled, Is.True);
            Assert.That(settings.GetFeature<KHRSimpleControllerProfile>()?.enabled, Is.True);
            Assert.That(settings.GetFeature<EyeGazeInteraction>()?.enabled, Is.True);
            Assert.That(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android), Is.False);
            Assert.That(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android), Is.EqualTo(new[] { GraphicsDeviceType.Vulkan }));
        }


        private static void AssertTarget(BuildTargetGroup group)
        {
            XRGeneralSettings settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            Assert.That(settings, Is.Not.Null, $"XR settings missing for {group}.");
            Assert.That(settings.InitManagerOnStart, Is.False);
            Assert.That(
                settings.Manager.activeLoaders,
                Has.Some.Matches<XRLoader>(loader => loader.GetType().FullName == "UnityEngine.XR.OpenXR.OpenXRLoader"));
        }
    }
}
