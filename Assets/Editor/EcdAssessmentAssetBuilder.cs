using System.IO;
using UnityEditor;
using UnityEngine;

namespace AdieLab.TeacherTraining.Editor
{
    public static class EcdAssessmentAssetBuilder
    {
        private const string DirectoryPath = "Assets/Resources/Training/ECD";
        private const string AssetPath = DirectoryPath + "/TeacherResponseEcdModel.asset";

        [MenuItem("Teacher Training/ECD/Create or Refresh Default Model")]
        public static void CreateOrRefresh()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "Training");
            EnsureFolder("Assets/Resources/Training", "ECD");

            EcdAssessmentModel model = AssetDatabase.LoadAssetAtPath<EcdAssessmentModel>(AssetPath);
            if (model == null)
            {
                model = ScriptableObject.CreateInstance<EcdAssessmentModel>();
                AssetDatabase.CreateAsset(model, AssetPath);
            }

            model.PopulateDefaults();
            EditorUtility.SetDirty(model);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"ECD assessment model saved: {AssetPath}");
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
