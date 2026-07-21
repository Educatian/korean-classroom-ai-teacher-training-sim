using System.IO;
using UnityEditor;
using UnityEngine;

namespace AdieLab.TeacherTraining.Editor
{
    public static class ResearchCloudSyncAssetConfigurator
    {
        private const string DirectoryPath = "Assets/Resources/Training/Research";
        private const string AssetPath = DirectoryPath + "/ResearchCloudSyncSettings.asset";
        private const string CollectorEndpoint =
            "https://teacher-training-collector.jewoong-moon.workers.dev";

        [MenuItem("Teacher Training/Research/Create or Refresh Cloud Sync Settings")]
        public static void CreateOrRefresh()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "Training");
            EnsureFolder("Assets/Resources/Training", "Research");

            ResearchCloudSyncSettings settings =
                AssetDatabase.LoadAssetAtPath<ResearchCloudSyncSettings>(AssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<ResearchCloudSyncSettings>();
                AssetDatabase.CreateAsset(settings, AssetPath);
            }

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("endpoint").stringValue = CollectorEndpoint;
            serialized.FindProperty("clientId").stringValue = "teacher-training-quest";
            serialized.FindProperty("timeoutSeconds").intValue = 45;
            serialized.FindProperty("uploadRawGaze").boolValue = true;
            serialized.FindProperty("automaticLogging").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Research cloud sync settings saved: {AssetPath}");
        }

        public static void CreateFromCommandLine()
        {
            CreateOrRefresh();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = Path.Combine(parent, child).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
