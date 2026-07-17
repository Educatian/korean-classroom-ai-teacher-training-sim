using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class ClassmateGazeSceneTests
    {
        [Test]
        public void ClassroomScene_ContainsExpandedClassAndTeacherGazeControllers()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/KoreanClassroomTraining.unity");

            NpcPerformance[] students = Object.FindObjectsByType<NpcPerformance>(FindObjectsSortMode.None);
            StudentGazeController[] gazes = Object.FindObjectsByType<StudentGazeController>(FindObjectsSortMode.None);
            NpcIdleBehaviorController[] idleBehaviors = Object.FindObjectsByType<NpcIdleBehaviorController>(FindObjectsSortMode.None);

            Assert.That(students.Length, Is.EqualTo(15));
            Assert.That(gazes.Length, Is.EqualTo(14));
            Assert.That(idleBehaviors.Length, Is.EqualTo(14));
            Assert.That(idleBehaviors.Count(controller => controller.IsAttentiveProfile), Is.EqualTo(14));
            Assert.That(idleBehaviors.Count(controller => !controller.IsAttentiveProfile), Is.EqualTo(0));
            Assert.That(System.Enum.IsDefined(typeof(NpcIdleBehavior), NpcIdleBehavior.Yawn), Is.True);
            Assert.That(System.Enum.IsDefined(typeof(NpcIdleBehavior), NpcIdleBehavior.ChinRest), Is.True);
            Assert.That(gazes.All(gaze => gaze.TeacherTarget != null), Is.True);
            Assert.That(gazes.All(gaze => gaze.name.StartsWith("Classmate_")), Is.True);
            Assert.That(gazes.Count(gaze => gaze.StartsAttentive), Is.EqualTo(14));
            Assert.That(gazes.Count(gaze => !gaze.StartsAttentive), Is.EqualTo(0));
            Assert.That(gazes.All(gaze => gaze.MaxHeadTurnDegrees <= 35f), Is.True,
                "Teacher tracking must not twist a seated student's neck beyond a natural range.");

            NpcPerformance[] classmates = students.Where(student => student.name.StartsWith("Classmate_")).ToArray();
            int distinctHeadShapes = classmates
                .Select(student => student.GetComponentInChildren<Animator>()?.GetBoneTransform(HumanBodyBones.Head)?.localScale ?? Vector3.one)
                .Select(scale => $"{scale.x:F3}:{scale.y:F3}:{scale.z:F3}")
                .Distinct()
                .Count();
            Assert.That(distinctHeadShapes, Is.GreaterThanOrEqualTo(7));
            Material[] outfitMaterials = classmates
                .SelectMany(student => student.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null && material.name.StartsWith("M_StudentOutfit_"))
                .ToArray();
            Assert.That(outfitMaterials.Length, Is.EqualTo(14));
            Assert.That(outfitMaterials.All(material => material.shader.name == "AdieLab/StudentClothingTint"), Is.True);
            int distinctGarmentColors = outfitMaterials
                .Select(material => material.GetColor("_ClothingColor"))
                .Select(color => $"{color.r:F3}:{color.g:F3}:{color.b:F3}")
                .Distinct()
                .Count();
            Assert.That(distinctGarmentColors, Is.EqualTo(14),
                "Every classmate must have a distinct primary garment color.");
            Material[] allOutfitMaterials = students
                .SelectMany(student => student.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null && material.name.StartsWith("M_StudentOutfit_"))
                .ToArray();
            Assert.That(allOutfitMaterials.Length, Is.EqualTo(15));
            Assert.That(allOutfitMaterials.All(material => material.HasProperty("_AccentColor") &&
                                                            material.HasProperty("_PatternType") &&
                                                            material.HasProperty("_PatternScale") &&
                                                            material.HasProperty("_PatternStrength") &&
                                                            material.HasProperty("_FabricTex") &&
                                                            material.HasProperty("_FabricScale") &&
                                                            material.HasProperty("_FabricStrength") &&
                                                            material.HasProperty("_GraphicAtlas") &&
                                                            material.HasProperty("_GraphicColor") &&
                                                            material.HasProperty("_GraphicIndex") &&
                                                            material.HasProperty("_GraphicScale") &&
                                                            material.HasProperty("_GraphicRotation") &&
                                                            material.HasProperty("_GraphicOffset") &&
                                                            material.HasProperty("_GraphicStrength")), Is.True);
            Assert.That(allOutfitMaterials.All(material =>
            {
                Texture fabric = material.GetTexture("_FabricTex");
                return fabric != null && AssetDatabase.GetAssetPath(fabric).Contains("Generated/StudentClothing");
            }), Is.True, "Every student outfit must consume a Codex imagegen fabric texture.");
            Assert.That(allOutfitMaterials.Select(material => material.GetTexture("_FabricTex")).Distinct().Count(),
                Is.GreaterThanOrEqualTo(8), "At least eight generated fabric surfaces must be visible across the class.");
            Assert.That(allOutfitMaterials.All(material =>
            {
                Texture atlas = material.GetTexture("_GraphicAtlas");
                return atlas != null && AssetDatabase.GetAssetPath(atlas).EndsWith("GraphicAtlas_15.png");
            }), Is.True, "Every outfit must consume the generated original chest-graphic atlas.");
            Assert.That(allOutfitMaterials.Select(material => Mathf.RoundToInt(material.GetFloat("_GraphicIndex")))
                .Distinct().Count(), Is.EqualTo(15), "Every student must receive a different generated chest graphic.");
            Assert.That(allOutfitMaterials.All(material =>
            {
                int graphicIndex = Mathf.RoundToInt(material.GetFloat("_GraphicIndex"));
                return graphicIndex >= 0 && graphicIndex <= 14 &&
                       material.GetFloat("_GraphicStrength") >= 0.88f;
            }), Is.True, "Graphic assignment must exclude the blank atlas cell and remain clearly visible.");
            TextureImporter graphicAtlasImporter = AssetImporter.GetAtPath(
                "Assets/Generated/StudentClothing/GraphicAtlas_15.png") as TextureImporter;
            Assert.That(graphicAtlasImporter, Is.Not.Null);
            Assert.That(graphicAtlasImporter.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(graphicAtlasImporter.mipmapEnabled, Is.False,
                "The graphic atlas must avoid mip bleeding between neighboring decal cells.");
            int distinctOutfitSignatures = allOutfitMaterials
                .Select(material =>
                {
                    Color primary = material.GetColor("_ClothingColor");
                    Color accent = material.GetColor("_AccentColor");
                    return $"{primary.r:F3}:{primary.g:F3}:{primary.b:F3}|" +
                           $"{accent.r:F3}:{accent.g:F3}:{accent.b:F3}|" +
                           $"{material.GetFloat("_PatternType"):F1}:" +
                           $"{material.GetFloat("_PatternScale"):F1}:" +
                           $"{material.GetFloat("_PatternStrength"):F2}|" +
                           $"{material.GetFloat("_GraphicIndex"):F0}:" +
                           $"{material.GetFloat("_GraphicScale"):F2}:" +
                           $"{material.GetFloat("_GraphicRotation"):F1}";
                })
                .Distinct()
                .Count();
            Assert.That(distinctOutfitSignatures, Is.EqualTo(15),
                "No two students may share the same randomized color, pattern, and graphic outfit combination.");
            Transform[] hairstyles = classmates
                .SelectMany(student => student.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name.StartsWith("HairStyle_"))
                .ToArray();
            Assert.That(hairstyles.Length, Is.EqualTo(14));
            Assert.That(hairstyles.Select(style => style.name).Distinct().Count(), Is.EqualTo(5));
            Material[] hairMaterials = students
                .SelectMany(student => student.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null && material.name.StartsWith("M_StudentHair_"))
                .ToArray();
            Assert.That(hairMaterials.Length, Is.GreaterThanOrEqualTo(15));
            Assert.That(hairMaterials.All(material => material.shader.name == "AdieLab/StudentHeadHairTint"), Is.True);
            Material[] correctedFemaleFaces = hairMaterials
                .Where(material => material.name.StartsWith("M_StudentHair_05_") ||
                                   material.name.StartsWith("M_StudentHair_11_"))
                .ToArray();
            Assert.That(correctedFemaleFaces.Length, Is.EqualTo(2));
            Assert.That(correctedFemaleFaces.All(material => material.GetFloat("_MouthRestoreStrength") >= 0.85f &&
                                                               material.GetFloat("_NoseNeutralizeStrength") >= 0.88f), Is.True,
                "Harin and Sua must use the UV-aligned central-face restore to remove detached ghost lips.");
            string[] generatedFaceGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Generated/StudentFaces" });
            Assert.That(generatedFaceGuids.Length, Is.GreaterThanOrEqualTo(12));
            Assert.That(generatedFaceGuids.All(guid =>
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                return importer != null && importer.npotScale == TextureImporterNPOTScale.None;
            }), Is.True, "Generated face atlases must keep their native resolution instead of being reduced to 1024 px.");
            Assert.That(hairMaterials.All(material => material.HasProperty("_ReferenceTex")), Is.True,
                "Rocketbox eye sockets must be available as a mask without replacing the generated Korean face.");
            Assert.That(hairMaterials.All(material =>
            {
                Texture texture = material.GetTexture("_MainTex");
                return texture != null && AssetDatabase.GetAssetPath(texture).Contains("Generated/StudentFaces");
            }), Is.True, "The generated Korean face must remain the visible head albedo.");
            Assert.That(hairMaterials.All(material =>
            {
                Texture texture = material.GetTexture("_ReferenceTex");
                return texture != null && AssetDatabase.GetAssetPath(texture).Contains("MicrosoftRocketbox");
            }), Is.True, "The Rocketbox head atlas must only provide the aligned eye-socket reference.");
            int distinctAppliedFaces = hairMaterials
                .Select(material => material.GetTexture("_MainTex"))
                .Where(texture => texture != null && AssetDatabase.GetAssetPath(texture).Contains("Generated/StudentFaces"))
                .Select(texture => texture.name)
                .Distinct()
                .Count();
            Assert.That(distinctAppliedFaces, Is.GreaterThanOrEqualTo(10));
            Assert.That(hairMaterials.All(material => material.GetFloat("_EyeSocketCorrection") >= 0.95f), Is.True,
                "Painted irises must be removed where the separate Rocketbox eyeball mesh renders.");
            Assert.That(hairMaterials.All(material => material.HasProperty("_PaintedEyeCenters") &&
                                                       material.HasProperty("_PaintedEyeRadius")), Is.True,
                "Each generated face atlas must provide calibrated eye centers instead of sharing one oversized mask.");
            Assert.That(hairMaterials.All(material =>
            {
                Vector4 centers = material.GetVector("_PaintedEyeCenters");
                Vector4 radius = material.GetVector("_PaintedEyeRadius");
                return centers.x > 0.40f && centers.x < 0.46f &&
                       centers.z > 0.54f && centers.z < 0.60f &&
                       centers.y > 0.67f && centers.y < 0.73f &&
                       centers.w > 0.67f && centers.w < 0.73f &&
                       radius.x >= 0.045f && radius.x <= 0.065f &&
                       radius.y >= 0.018f && radius.y <= 0.032f &&
                       centers.x + radius.x < centers.z - radius.x;
            }), Is.True, "Painted-eye masks must cover each atlas eye independently without overlapping across the nose.");
            Assert.That(hairMaterials.All(material => Mathf.Approximately(material.GetFloat("_FacialNormalSuppression"), 1f)), Is.True,
                "The Rocketbox face normal must be fully suppressed beneath generated facial features.");
            Assert.That(hairMaterials.All(material => material.HasProperty("_EyeSocketBrightness") &&
                                                       material.GetFloat("_EyeSocketBrightness") >= 0.82f), Is.True,
                "Eye sockets must stay skin-toned instead of reading as heavy black makeup.");
            Assert.That(hairMaterials.All(material => material.HasProperty("_IrisColor") &&
                                                       material.HasProperty("_IrisTintStrength") &&
                                                       material.HasProperty("_IrisAtlasCenter") &&
                                                       material.HasProperty("_IrisRadius") &&
                                                       material.HasProperty("_PupilRadius")), Is.True,
                "Rocketbox eyeballs must expose a dedicated iris tint instead of sampling the generated atlas eye island.");
            Assert.That(hairMaterials.All(material =>
            {
                Color iris = material.GetColor("_IrisColor");
                Vector4 irisCenter = material.GetVector("_IrisAtlasCenter");
                return iris.r > iris.g * 2f && iris.g > iris.b * 1.5f &&
                       iris.r >= 0.20f && iris.r <= 0.40f &&
                       iris.g >= 0.07f && iris.g <= 0.16f &&
                       iris.b >= 0.025f && iris.b <= 0.08f &&
                       material.GetFloat("_IrisTintStrength") >= 0.95f &&
                       material.GetFloat("_IrisRadius") >= 0.021f &&
                       material.GetFloat("_IrisRadius") <= 0.026f &&
                       material.GetFloat("_PupilRadius") >= 0.006f &&
                       material.GetFloat("_PupilRadius") <= 0.009f &&
                       irisCenter.x >= 0.262f && irisCenter.x <= 0.266f &&
                       irisCenter.y >= 0.067f && irisCenter.y <= 0.071f &&
                       material.GetFloat("_IrisRadius") > material.GetFloat("_PupilRadius") * 2.5f;
            }), Is.True, "Student irises must be a visible reddish brown, with black reserved for the pupil.");
            Assert.That(hairMaterials.All(material => material.HasProperty("_SocketEyeCenters") &&
                                                       material.HasProperty("_SocketEyeRadius") &&
                                                       material.HasProperty("_SocketBlendStrength") &&
                                                       material.GetFloat("_SocketBlendStrength") >= 0.68f &&
                                                       material.GetFloat("_SocketBlendStrength") <= 0.78f), Is.True,
                "Generated painted eyes must be replaced by tone-matched Rocketbox eyelids around the tracked eyeball mesh.");
            Assert.That(hairMaterials.All(material => material.HasProperty("_PaintedMouthCenterRadius") &&
                                                       material.HasProperty("_MouthRestoreStrength")), Is.True,
                "Generated male faces must expose a calibrated mask for the rigged mouth instead of rendering two lip seams.");
            Material focalMaleFace = hairMaterials.Single(material => material.name.StartsWith("M_StudentHair_15_"));
            Vector4 focalMouthMask = focalMaleFace.GetVector("_PaintedMouthCenterRadius");
            Assert.That(focalMaleFace.GetFloat("_MouthRestoreStrength"), Is.GreaterThanOrEqualTo(0.90f));
            Assert.That(focalMouthMask.x, Is.InRange(0.49f, 0.51f));
            Assert.That(focalMouthMask.y, Is.InRange(0.54f, 0.58f));
            Assert.That(focalMaleFace.HasProperty("_PaintedNoseCenterRadius"), Is.True);
            Assert.That(focalMaleFace.HasProperty("_NoseNeutralizeStrength"), Is.True);
            Vector4 focalNoseMask = focalMaleFace.GetVector("_PaintedNoseCenterRadius");
            Assert.That(focalMaleFace.GetFloat("_NoseNeutralizeStrength"), Is.GreaterThanOrEqualTo(0.95f));
            Assert.That(focalNoseMask.x, Is.InRange(0.49f, 0.51f));
            Assert.That(focalNoseMask.y, Is.InRange(0.60f, 0.65f));
            Assert.That(focalNoseMask.z, Is.GreaterThanOrEqualTo(0.12f),
                "The UV-correct central feature restore must cover the full painted nose outline.");
            Assert.That(focalNoseMask.w, Is.GreaterThanOrEqualTo(0.11f),
                "The central feature restore must feather through the rigged nose and mouth region.");
            Assert.That(hairMaterials.All(material =>
            {
                Color color = material.GetColor("_HairColor");
                return color.r < 0.09f && color.g < 0.06f && color.b < 0.05f;
            }), Is.True);
        }
    }
}
