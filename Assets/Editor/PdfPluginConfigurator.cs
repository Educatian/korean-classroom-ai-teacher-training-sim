using System;
using UnityEditor;
using UnityEngine;

namespace AdieLab.TeacherTraining.Editor
{
    public static class PdfPluginConfigurator
    {
        private const string ManagedPath = "Assets/Plugins/Docnet/Docnet.Core.dll";
        private const string NativePath = "Assets/Plugins/x86_64/pdfium.dll";

        [MenuItem("Tools/Teacher Training/Configure PDF Presentation Plugins")]
        public static void Configure()
        {
            ConfigurePlugin(ManagedPath, "AnyCPU");
            ConfigurePlugin(NativePath, "x86_64");
            AssetDatabase.SaveAssets();
            Debug.Log("PDF_PRESENTATION_PLUGINS_CONFIGURED");
        }

        public static void ConfigureFromCommandLine()
        {
            try
            {
                Configure();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ConfigurePlugin(string path, string cpu)
        {
            PluginImporter importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"PDF plugin importer missing: {path}");
            }
            importer.SetCompatibleWithAnyPlatform(false);
            importer.SetCompatibleWithEditor(true);
            importer.SetEditorData("OS", "Windows");
            importer.SetEditorData("CPU", cpu);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
            importer.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);
            importer.SetPlatformData(BuildTarget.StandaloneWindows64, "CPU", cpu);
            importer.SetCompatibleWithPlatform(BuildTarget.Android, false);
            importer.SetCompatibleWithPlatform(BuildTarget.WebGL, false);
            importer.SaveAndReimport();
        }
    }
}