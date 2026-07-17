using UnityEngine;
using UnityEditor.SceneManagement;

namespace AdieLab.TeacherTraining.Editor
{
    public static partial class KoreanClassroomBuilder
    {
        private const string BackpackPreviewPath = "Assets/Reference/Unity_HangingBackpacks_Preview.png";

        private readonly struct StrapSpec
        {
            public StrapSpec(string name, StrapPath path, StrapSurface surface)
            {
                Name = name;
                Path = path;
                Surface = surface;
            }

            public string Name { get; }
            public StrapPath Path { get; }
            public StrapSurface Surface { get; }
        }

        private readonly struct StrapPath
        {
            public StrapPath(Vector3 start, Vector3 end)
            {
                Start = start;
                End = end;
            }

            public Vector3 Start { get; }
            public Vector3 End { get; }
        }

        private readonly struct StrapSurface
        {
            public StrapSurface(float radius, Material material)
            {
                Radius = radius;
                Material = material;
            }

            public float Radius { get; }
            public Material Material { get; }
        }

        private static void CreateHangingBackpack(Transform parent, int index, Vector3 deskSurface)
        {
            string[] bodyMaterials =
            {
                "M_BookTeal", "M_PlaceholderNavy", "M_FlagRed",
                "M_LockerYellow", "M_WorkBlue", "M_Rubber"
            };
            string[] accentMaterials =
            {
                "M_WorkMint", "M_LockerYellow", "M_WorkBlue",
                "M_PlaceholderNavy", "M_FlagRed", "M_BookTeal"
            };
            float[] yaw = { -7f, 5f, -3f, 8f, -6f, 4f };
            float[] roll = { -4f, 3f, -2f, 5f, -3f, 2f };
            Vector3[] styleScale =
            {
                new Vector3(0.82f, 0.78f, 0.78f),
                new Vector3(0.90f, 0.86f, 0.75f),
                new Vector3(0.78f, 0.92f, 0.68f)
            };

            Vector3 anchor = deskSurface + new Vector3(0.49f, -0.46f, -0.37f);
            GameObject root = RootObject($"Backpack_{index:00}", parent, anchor);
            root.transform.localRotation = Quaternion.Euler(2f, yaw[index], roll[index]);
            root.transform.localScale = styleScale[index % styleScale.Length];

            Material body = Mat(bodyMaterials[index]);
            Material accent = Mat(accentMaterials[index]);
            Material strap = Mat("M_Rubber");
            Material hardware = Mat("M_Metal");
            Material reflector = Mat("M_ClockFace");
            StrapSurface wideStrap = new StrapSurface(0.018f, strap);
            StrapSurface handleStrap = new StrapSurface(0.014f, strap);
            StrapSurface zipper = new StrapSurface(0.007f, hardware);
            StrapSurface hook = new StrapSurface(0.016f, hardware);

            RoundedBox("Body", root.transform, Vector3.zero, new Vector3(0.40f, 0.49f, 0.21f), 0.055f, "BackpackBody", body);
            RoundedBox("FrontPocket", root.transform, new Vector3(0f, -0.08f, 0.135f), new Vector3(0.32f, 0.23f, 0.085f), 0.035f, "BackpackFrontPocket", accent);
            RoundedBox("TopPanel", root.transform, new Vector3(0f, 0.19f, 0.115f), new Vector3(0.33f, 0.10f, 0.055f), 0.025f, "BackpackTopPanel", body);
            float pocketSide = index % 2 == 0 ? 1f : -1f;
            RoundedBox("SidePocket", root.transform, new Vector3(0.22f * pocketSide, -0.08f, 0.015f), new Vector3(0.075f, 0.20f, 0.15f), 0.022f, "BackpackSidePocket", accent);
            Cube("ReflectiveStrip", root.transform, new Vector3(0f, -0.025f, 0.183f), new Vector3(0.22f, 0.022f, 0.009f), reflector);
            Cube("BrandPatch", root.transform, new Vector3(0f, 0.075f, 0.184f), new Vector3(0.095f, 0.055f, 0.010f), accent);

            CreateStrap(root.transform, new StrapSpec("HangStrap", new StrapPath(new Vector3(-0.10f, 0.23f, -0.015f), new Vector3(-0.03f, 0.39f, -0.005f)), wideStrap));
            CreateStrap(root.transform, new StrapSpec("HandleLeft", new StrapPath(new Vector3(-0.10f, 0.23f, -0.015f), new Vector3(-0.10f, 0.31f, -0.015f)), handleStrap));
            CreateStrap(root.transform, new StrapSpec("HandleTop", new StrapPath(new Vector3(-0.10f, 0.31f, -0.015f), new Vector3(0.10f, 0.31f, -0.015f)), handleStrap));
            CreateStrap(root.transform, new StrapSpec("HandleRight", new StrapPath(new Vector3(0.10f, 0.31f, -0.015f), new Vector3(0.10f, 0.23f, -0.015f)), handleStrap));
            CreateStrap(root.transform, new StrapSpec("FrontZipper", new StrapPath(new Vector3(-0.12f, 0.035f, 0.185f), new Vector3(0.12f, 0.035f, 0.185f)), zipper));
            CreateStrap(root.transform, new StrapSpec("DeskHook", new StrapPath(new Vector3(-0.03f, 0.39f, -0.005f), new Vector3(-0.03f, 0.43f, 0.07f)), hook));

            Cylinder("ZipperPull", root.transform, new Vector3(0.13f, 0.015f, 0.19f), new Vector3(0.008f, 0.035f, 0.008f), Quaternion.Euler(0f, 0f, -25f), hardware);
            Cylinder("BottleCap", root.transform, new Vector3(0.23f * pocketSide, 0.055f, 0.02f), new Vector3(0.035f, 0.028f, 0.035f), Quaternion.identity, Mat("M_WorkMint"));
            Sphere("BagCharm", root.transform, new Vector3(-0.15f, 0.03f, 0.20f), new Vector3(0.035f, 0.035f, 0.025f), accent);
        }

        private static void CreateStrap(Transform parent, StrapSpec spec)
        {
            Vector3 direction = spec.Path.End - spec.Path.Start;
            GameObject segment = Cylinder(
                spec.Name,
                parent,
                (spec.Path.Start + spec.Path.End) * 0.5f,
                new Vector3(spec.Surface.Radius, direction.magnitude * 0.5f, spec.Surface.Radius),
                Quaternion.FromToRotation(Vector3.up, direction.normalized),
                spec.Surface.Material);
            segment.GetComponent<Collider>().enabled = false;
        }

        public static void CaptureBackpackPreviewFromCommandLine()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = Object.FindAnyObjectByType<Camera>();
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
            }

            camera.transform.position = new Vector3(4.15f, 1.18f, 3.35f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(2.82f, 0.34f, 1.65f) - camera.transform.position);
            camera.fieldOfView = 43f;
            RenderCamera(camera, BackpackPreviewPath);
            Debug.Log("BACKPACK_PREVIEW_OK");
        }
    }
}
