using UnityEditor;
using UnityEngine;

namespace AdieLab.TeacherTraining.Editor
{
    public sealed class RocketboxImportProcessor : AssetPostprocessor
    {
        private const string Root = "Assets/ThirdParty/MicrosoftRocketbox/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root))
            {
                return;
            }

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importBlendShapes = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                string lower = assetPath.ToLowerInvariant();
                clips[i].loopTime = lower.Contains("idle") || lower.Contains("breathe") || lower.Contains("waiting");
                clips[i].loopPose = clips[i].loopTime;
            }

            if (clips.Length > 0)
            {
                importer.clipAnimations = clips;
            }
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (assetPath.ToLowerInvariant().Contains("normal"))
            {
                importer.textureType = TextureImporterType.NormalMap;
            }
        }

        private void OnPostprocessMaterial(Material material)
        {
            if (!assetPath.StartsWith(Root))
            {
                return;
            }

            material.color = Color.white;
            string lower = material.name.ToLowerInvariant();
            if (!lower.Contains("hair") && !lower.Contains("lash"))
            {
                return;
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 2f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 3000;
            }
        }

        private void OnPostprocessMeshHierarchy(GameObject gameObject)
        {
            if (!assetPath.StartsWith(Root))
            {
                return;
            }

            string lower = gameObject.name.ToLowerInvariant();
            if (lower.Contains("poly") && !lower.Contains("hipoly"))
            {
                gameObject.SetActive(false);
            }
        }
    }
}
