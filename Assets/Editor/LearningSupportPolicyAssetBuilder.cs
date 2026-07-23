using System.IO;
using UnityEditor;
using UnityEngine;

namespace AdieLab.TeacherTraining.Editor
{
    public static class LearningSupportPolicyAssetBuilder
    {
        private const string DirectoryPath = "Assets/Resources/Training/Research";
        private const string AssetPath = DirectoryPath + "/LearningSupportPolicy.asset";

        [MenuItem("Tools/Teacher Training/Rebuild Learning Support Policy")]
        public static void Rebuild()
        {
            Directory.CreateDirectory(DirectoryPath);
            LearningSupportPolicy policy = AssetDatabase.LoadAssetAtPath<LearningSupportPolicy>(AssetPath);
            if (policy == null)
            {
                policy = ScriptableObject.CreateInstance<LearningSupportPolicy>();
                AssetDatabase.CreateAsset(policy, AssetPath);
            }

            policy.ConfigureForEditor(
                18f,
                2,
                1,
                1.35f,
                LearningSupportPolicy.DefaultPromptsForEditor());
            EditorUtility.SetDirty(policy);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LEARNING_SUPPORT_POLICY_READY " + AssetPath);
        }
    }
}
