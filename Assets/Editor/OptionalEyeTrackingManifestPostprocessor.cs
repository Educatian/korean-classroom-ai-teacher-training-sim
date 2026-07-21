#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace AdieLab.TeacherTraining.Editor
{
    public sealed class OptionalEyeTrackingManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => int.MaxValue;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            int changed = 0;
            foreach (string manifestPath in Directory.GetFiles(path, "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                string xml = File.ReadAllText(manifestPath);
                string updated = ApplyQuestRuntimeRequirements(xml);
                if (string.Equals(xml, updated, StringComparison.Ordinal)) continue;
                File.WriteAllText(manifestPath, updated);
                changed++;
            }
            Debug.Log($"QUEST_RUNTIME_MANIFEST_OK manifests={changed} microphone=enabled eyeTracking=optional");
        }

        public static string MakeEyeTrackingOptional(string manifestXml) => QuestAndroidManifestPolicy.MakeEyeTrackingOptional(manifestXml);
        public static string ApplyQuestRuntimeRequirements(string manifestXml) => QuestAndroidManifestPolicy.Apply(manifestXml);
    }
}
#endif