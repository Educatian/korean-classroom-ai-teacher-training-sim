using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.CompositionLayers;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace AdieLab.TeacherTraining.Editor
{
    public static class MetaQuestProjectConfigurator
    {
        private const string OpenXrLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";

        [MenuItem("Tools/Teacher Training/Configure Desktop + Meta Quest XR")]
        public static void Configure()
        {
            XRGeneralSettingsPerBuildTarget settingsPerTarget = GetOrCreateSettings();
            ConfigureTarget(settingsPerTarget, BuildTargetGroup.Standalone);
            ConfigureTarget(settingsPerTarget, BuildTargetGroup.Android);
            EnableOpenXrFeatures(BuildTargetGroup.Standalone, false);
            EnableOpenXrFeatures(BuildTargetGroup.Android, true);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            AssetDatabase.SaveAssets();
            Debug.Log("META_QUEST_XR_CONFIGURATION_OK loaders=Standalone,Android initializeOnStart=false architecture=ARM64 graphics=Vulkan");
        }

        public static void ConfigureFromCommandLine()
        {
            Configure();
        }

        public static bool HasOpenXrLoader(BuildTargetGroup group)
        {
            XRGeneralSettings settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            if (settings?.Manager == null)
            {
                return false;
            }

            foreach (XRLoader loader in settings.Manager.activeLoaders)
            {
                if (string.Equals(loader.GetType().FullName, OpenXrLoaderType, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreateSettings()
        {
            MethodInfo getOrCreate = typeof(XRGeneralSettingsPerBuildTarget).GetMethod(
                "GetOrCreate",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (getOrCreate?.Invoke(null, null) is XRGeneralSettingsPerBuildTarget settings)
            {
                return settings;
            }
            throw new InvalidOperationException("Unable to create XR settings per build target.");
        }

        private static void ConfigureTarget(XRGeneralSettingsPerBuildTarget settingsPerTarget, BuildTargetGroup group)
        {
            if (!settingsPerTarget.HasManagerSettingsForBuildTarget(group))
            {
                settingsPerTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            }

            XRGeneralSettings settings = settingsPerTarget.SettingsForBuildTarget(group);
            settings.InitManagerOnStart = false;
            if (!XRPackageMetadataStore.AssignLoader(settings.Manager, OpenXrLoaderType, group))
            {
                throw new InvalidOperationException($"Unable to assign OpenXR loader for {group}.");
            }
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(settings.Manager);
        }

        private static void EnableOpenXrFeatures(BuildTargetGroup group, bool metaQuest)
        {
            FeatureHelpers.RefreshFeatures(group);
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (settings == null)
            {
                throw new InvalidOperationException($"OpenXR settings are missing for {group}.");
            }

            SetEnabled<KHRSimpleControllerProfile>(settings, true);
            SetEnabled<OpenXRCompositionLayersFeature>(settings, true);
            SetEnabled<OculusTouchControllerProfile>(settings, true);
            SetEnabled<MetaQuestTouchPlusControllerProfile>(settings, metaQuest);
            SetEnabled<MetaQuestTouchProControllerProfile>(settings, metaQuest);
            SetEnabled<MetaQuestFeature>(settings, metaQuest);
            EditorUtility.SetDirty(settings);
        }

        private static void SetEnabled<T>(OpenXRSettings settings, bool enabled) where T : OpenXRFeature
        {
            T feature = settings.GetFeature<T>();
            if (feature == null)
            {
                if (enabled)
                {
                    throw new InvalidOperationException($"OpenXR feature is unavailable: {typeof(T).Name}");
                }
                return;
            }
            feature.enabled = enabled;
            EditorUtility.SetDirty(feature);
        }
    }
}
