using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AdieLab.TeacherTraining.Editor
{
    public static class ResearchQuestBuildVerifier
    {
        public static void BuildCurrentScenesFromCommandLine()
        {
            try
            {
                MetaQuestProjectConfigurator.Configure();
                const string output =
                    "Builds/TeacherResponseTrainingQuest/TeacherResponseTrainingQuest.apk";
                Directory.CreateDirectory(Path.GetDirectoryName(output) ?? "Builds");
                string[] scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();
                if (scenes.Length == 0)
                {
                    throw new InvalidOperationException("No enabled training scenes were found.");
                }

                PlayerSettings.SetApplicationIdentifier(
                    NamedBuildTarget.Android,
                    "edu.adielab.teacherresponsetraining");
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                    extraScriptingDefines = new[] { "QUEST_PRO_RESEARCH" }
                });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Meta Quest build failed: {report.summary.result}");
                }

                Debug.Log(
                    $"RESEARCH_AUTO_LOGGING_QUEST_BUILD_OK " +
                    $"bytes={report.summary.totalSize} output={output}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
