using UnityEditor;
using UnityEngine;

namespace AdieLab.TeacherTraining.Editor
{
    public static partial class KoreanClassroomBuilder
    {
        private const string BulletinTextureRoot = "Assets/Art/Textures/BulletinItems";

        private readonly struct BulletinItemSpec
        {
            public BulletinItemSpec(string asset, Vector2 position, float height, float rotation)
            {
                Asset = asset;
                Position = position;
                Height = height;
                Rotation = rotation;
            }

            public string Asset { get; }
            public Vector2 Position { get; }
            public float Height { get; }
            public float Rotation { get; }
        }

        private static readonly BulletinItemSpec[] BulletinItems =
        {
            new("Title_OurClass", new Vector2(-1.85f, 3.02f), 0.42f, -1.2f),
            new("Title_HomeLetter", new Vector2(4.35f, 3.01f), 0.40f, 1.0f),
            new("Art_SelfPortrait", new Vector2(-3.10f, 2.35f), 0.84f, -2.4f),
            new("Art_Friends", new Vector2(-1.52f, 2.36f), 0.76f, 0.8f),
            new("Art_OurTown", new Vector2(0.25f, 2.37f), 0.82f, -1.0f),
            new("Notice_ClassLetter", new Vector2(2.08f, 2.32f), 0.87f, 2.1f),
            new("Notice_HomeLetter", new Vector2(3.57f, 2.34f), 0.88f, -1.5f),
            new("Worksheet_Yellow", new Vector2(5.08f, 2.35f), 0.72f, 1.8f),
            new("Art_TreeSpring", new Vector2(-3.23f, 1.55f), 0.56f, -1.3f),
            new("Art_TreeSummer", new Vector2(-2.48f, 1.56f), 0.55f, 1.1f),
            new("Art_TreeAutumn", new Vector2(-1.73f, 1.55f), 0.55f, -0.7f),
            new("Art_TreeWinter", new Vector2(-0.98f, 1.57f), 0.55f, 1.7f),
            new("Art_School", new Vector2(-0.10f, 1.57f), 0.56f, -1.1f),
            new("Poster_Friendship", new Vector2(0.90f, 1.58f), 0.55f, 1.2f),
            new("Poster_Diversity", new Vector2(1.83f, 1.56f), 0.56f, -2.0f),
            new("Science_PlantA", new Vector2(2.65f, 1.57f), 0.55f, 0.8f),
            new("Science_PlantB", new Vector2(3.37f, 1.56f), 0.55f, -1.5f),
            new("Worksheet_Blue", new Vector2(4.10f, 1.56f), 0.54f, 1.2f),
            new("Worksheet_Green", new Vector2(4.85f, 1.58f), 0.53f, -1.7f),
            new("Worksheet_Pink", new Vector2(5.57f, 1.56f), 0.53f, 1.9f)
        };

        private static void BuildRearBulletin(Transform parent)
        {
            GameObject board = RootObject("RearBulletinAssembly", parent, Vector3.zero);
            RoundedBox("FeltBoard", board.transform, new Vector3(1.2f, 2.25f, -4.82f), new Vector3(9.8f, 2.02f, 0.10f), 0.025f, "RearFeltBoard", Mat("M_BulletinGreen"));
            Cube("TopTrim", board.transform, new Vector3(1.2f, 3.29f, -4.73f), new Vector3(10.1f, 0.08f, 0.16f), Mat("M_TrimWood"));
            Cube("BottomTrim", board.transform, new Vector3(1.2f, 1.20f, -4.73f), new Vector3(10.1f, 0.08f, 0.16f), Mat("M_TrimWood"));

            for (int index = 0; index < BulletinItems.Length; index++)
            {
                AddBulletinItem(board.transform, BulletinItems[index], index);
            }
        }

        private static void AddBulletinItem(Transform parent, BulletinItemSpec spec, int index)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{BulletinTextureRoot}/{spec.Asset}.png");
            float aspect = texture == null ? 1f : (float)texture.width / texture.height;
            GameObject item = Cube(
                $"Pinned_{index:00}_{spec.Asset}",
                parent,
                new Vector3(spec.Position.x, spec.Position.y, -4.745f - index * 0.0002f),
                new Vector3(spec.Height * aspect, spec.Height, 0.012f),
                Mat($"M_Bulletin_{spec.Asset}"));
            item.transform.localRotation = Quaternion.Euler(0f, 0f, spec.Rotation);
            item.GetComponent<Collider>().enabled = false;
        }

        private static void CreateBulletinMaterials()
        {
            foreach (BulletinItemSpec item in BulletinItems)
            {
                CreateMaterial(
                    $"M_Bulletin_{item.Asset}",
                    Color.white,
                    0f,
                    0.16f,
                    $"{BulletinTextureRoot}/{item.Asset}.png",
                    Vector2.one,
                    true);
            }
        }

        private static void EnsureBulletinItemTextureSettings()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { BulletinTextureRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Trilinear;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 8;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }
}
