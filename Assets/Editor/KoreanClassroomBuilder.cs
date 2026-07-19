using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdieLab.TeacherTraining;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining.Editor
{
    // allow: SIZE_OK — Declarative scene composition root; focused builders are extracted into partial files.
    public static partial class KoreanClassroomBuilder
    {
        private const string ScenePath = "Assets/Scenes/KoreanClassroomTraining.unity";
        private const string PreviewPath = "Assets/Reference/Unity_KoreanClassroom_Preview.png";
        private const string SceneOnlyPreviewPath = "Assets/Reference/Unity_SceneOnly_Preview.png";
        private const string ElectronicBoardPreviewPath = "Assets/Reference/Unity_ElectronicBoard_Preview.png";
        private const string MaterialRoot = "Assets/Materials";
        private const string ControllerRoot = "Assets/Animations/Controllers";
        private const string MeshRoot = "Assets/Meshes";
        private const string RocketboxRoot = "Assets/ThirdParty/MicrosoftRocketbox";
        private const string RoundedUiSpritePath = "Assets/Art/UI/RoundedSurface.png";
        private const string SpeechTailSpritePath = "Assets/Art/UI/SpeechTail.png";
        private const string KoreanFontSourcePath = "Assets/Art/Fonts/NotoSansKR-VF.ttf";
        private const string KoreanFontAssetPath = "Assets/Art/Fonts/NotoSansKR-SDF.asset";
        private const string TmpSettingsPath = "Assets/Resources/TMP Settings.asset";

        private static readonly Color Navy = new Color(0.055f, 0.10f, 0.18f, 1f);
        private static readonly Color Teal = new Color(0.09f, 0.49f, 0.48f, 1f);
        private static readonly Color WarmWhite = new Color(0.94f, 0.92f, 0.86f, 1f);

        private static class UiTokens
        {
            public static readonly Color Header = new Color(0.035f, 0.07f, 0.12f, 0.94f);
            public static readonly Color DarkSurface = new Color(0.035f, 0.07f, 0.12f, 0.91f);
            public static readonly Color LightSurface = new Color(0.965f, 0.955f, 0.925f, 1f);
            public static readonly Color Primary = new Color(0.055f, 0.53f, 0.50f, 1f);
            public static readonly Color PrimaryHover = new Color(0.17f, 0.68f, 0.62f, 1f);
            public static readonly Color Option = new Color(0.12f, 0.17f, 0.23f, 0.98f);
            public static readonly Color ObservationChip = new Color(0.11f, 0.33f, 0.36f, 0.96f);
            public static readonly Color PaleChip = new Color(0.82f, 0.91f, 0.88f, 1f);
            public static readonly Color ChipText = new Color(0.04f, 0.36f, 0.34f, 1f);
            public static readonly Color InputSurface = new Color(0.96f, 0.96f, 0.93f, 1f);
            public static readonly Color BubbleBorder = new Color(0.055f, 0.53f, 0.50f, 0.90f);
            public static readonly Color BubbleSurface = new Color(0.99f, 0.985f, 0.96f, 0.92f);
            public static readonly Color Placeholder = new Color(0.18f, 0.22f, 0.25f, 1f);
            public static readonly Color TextOnDark = Color.white;
            public static readonly Color MutedOnDark = new Color(0.82f, 0.88f, 0.92f);
            public static readonly Color AccentText = new Color(0.48f, 0.90f, 0.82f);
            public static readonly Color TextOnLight = new Color(0.12f, 0.16f, 0.20f);
            public const int TitleSize = 24;
            public const int HeadingSize = 22;
            public const int BodySize = 18;
            public const int OptionSize = 16;
            public const float HeaderHeight = 64f;
            public const float HudHeight = 270f;
        }

        [MenuItem("Tools/Teacher Training/Build Korean Classroom")]
        public static void BuildFromMenu()
        {
            BuildAll();
            Debug.Log($"Korean classroom scene built at {ScenePath}");
        }

        public static string[] GetTrainingScenePaths()
        {
            return TrainingSceneRegistry.SceneAssetPaths();
        }

        private static void RegisterTrainingScenes()
        {
            string[] paths = GetTrainingScenePaths();
            EditorBuildSettings.scenes = Array.ConvertAll(
                paths,
                path => new EditorBuildSettingsScene(path, true));
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildAll();
                CapturePreview();
                Debug.Log("KOREAN_CLASSROOM_BUILD_OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildSceneFromCommandLine()
        {
            try
            {
                BuildAll();
                Debug.Log("KOREAN_CLASSROOM_SCENE_BUILD_OK");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildWindowsFromCommandLine()
        {
            try
            {
                BuildAll();
                BuildCircleScene();
                const string output = "Builds/TeacherResponseTrainingFinal/TeacherResponseTraining.exe";
                Directory.CreateDirectory(Path.GetDirectoryName(output) ?? "Builds");
                string[] buildScenes = GetTrainingScenePaths();
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = buildScenes,
                    locationPathName = output,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException($"Windows build failed: {report.summary.result}");
                }

                Debug.Log($"WINDOWS_BUILD_OK bytes={report.summary.totalSize} output={output}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Tools/Teacher Training/Capture Classroom Preview")]
        public static void CapturePreview()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("Preview camera is missing.");
            }

            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            bool canvasWasEnabled = canvas != null && canvas.enabled;
            if (canvas != null)
            {
                canvas.enabled = false;
            }
            RenderCamera(camera, SceneOnlyPreviewPath);
            Vector3 savedPosition = camera.transform.position;
            Quaternion savedRotation = camera.transform.rotation;
            camera.transform.position = new Vector3(0.2f, 1.58f, -2.8f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(-0.75f, 1.95f, 4.55f) - camera.transform.position);
            RenderCamera(camera, ElectronicBoardPreviewPath);
            camera.transform.SetPositionAndRotation(savedPosition, savedRotation);
            if (canvas != null)
            {
                canvas.enabled = canvasWasEnabled;
            }
            RenderCamera(camera, PreviewPath);
            AssetDatabase.ImportAsset(PreviewPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(SceneOnlyPreviewPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ElectronicBoardPreviewPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"KOREAN_CLASSROOM_PREVIEW_OK {scene.name} 1600x900");
        }

        private static void BuildAll()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(MaterialRoot);
            EnsureFolder(ControllerRoot);
            EnsureFolder(MeshRoot);
            EnsureFolder("Assets/Art/UI");
            EnsureRoundedUiSprite();
            EnsureSpeechTailSprite();
            EnsureGeneratedTextureSettings();
            EnsureBulletinItemTextureSettings();

            CreateMaterials();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CreateAnimatorController(false);
            CreateAnimatorController(true);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureEnvironment();

            GameObject environment = Root("00_ENVIRONMENT");
            GameObject architecture = ChildRoot(environment, "Architecture");
            GameObject furniture = ChildRoot(environment, "Furniture");
            GameObject props = ChildRoot(environment, "Props");
            GameObject lighting = ChildRoot(environment, "Lighting");
            GameObject students = Root("10_STUDENTS");
            GameObject systems = Root("20_SYSTEMS");
            GameObject interfaceRoot = Root("30_INTERFACE");

            BuildArchitecture(architecture.transform);
            BuildFurniture(furniture.transform, props.transform);
            BuildLighting(lighting.transform);
            Camera camera = BuildCamera(systems.transform);
            StudentSet studentSet = BuildStudents(students.transform, camera.transform);
            TeacherCameraController cameraController = camera.GetComponent<TeacherCameraController>();
            cameraController.SetFocusTarget(studentSet.focal.transform);
            studentSet.focal.SetConversationTarget(camera.transform);
            Animator focalAnimator = studentSet.focal.GetComponentInChildren<Animator>();
            Transform speechTarget = focalAnimator.GetBoneTransform(HumanBodyBones.Head);
            TrainingHud hud = BuildInterface(interfaceRoot.transform, camera, speechTarget);
            BuildSimulationSystem(systems.transform, hud, studentSet, cameraController);

            Selection.activeGameObject = camera.gameObject;
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterTrainingScenes();
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.82f, 0.82f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.60f, 0.57f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.23f, 0.20f);
            RenderSettings.ambientIntensity = 0.68f;
            RenderSettings.fog = false;
            QualitySettings.shadowDistance = 45f;
            QualitySettings.antiAliasing = 4;
        }

        private static void BuildArchitecture(Transform parent)
        {
            Material wall = Mat("M_Wall");
            Material ceiling = Mat("M_Ceiling");
            Material floor = Mat("M_Floor");
            Material board = Mat("M_Whiteboard");
            Material trim = Mat("M_TrimWood");
            Material metal = Mat("M_Metal");
            Material glass = Mat("M_Glass");
            Material exterior = Mat("M_ExteriorView");

            Cube("Floor", parent, new Vector3(0f, -0.06f, 0f), new Vector3(14f, 0.12f, 10f), floor);
            Cube("Ceiling", parent, new Vector3(0f, 3.42f, 0f), new Vector3(14f, 0.16f, 10f), ceiling);
            Cube("FrontWall", parent, new Vector3(0f, 1.7f, 5f), new Vector3(14f, 3.4f, 0.16f), wall);
            Cube("BackWall", parent, new Vector3(0f, 1.7f, -5f), new Vector3(14f, 3.4f, 0.16f), wall);
            Cube("LeftWall", parent, new Vector3(-7f, 1.7f, 0f), new Vector3(0.16f, 3.4f, 10f), wall);
            Cube("RightLowerWall", parent, new Vector3(7f, 0.48f, 0f), new Vector3(0.16f, 0.96f, 10f), wall);
            Cube("RightUpperWall", parent, new Vector3(7f, 3.05f, 0f), new Vector3(0.16f, 0.74f, 10f), wall);

            Cube("MainChalkboard", parent, new Vector3(-1.2f, 2.05f, 4.86f), new Vector3(7.2f, 1.75f, 0.09f), board);
            Cube("BoardTopTrim", parent, new Vector3(-1.2f, 2.95f, 4.78f), new Vector3(7.5f, 0.09f, 0.16f), trim);
            Cube("BoardBottomTrim", parent, new Vector3(-1.2f, 1.15f, 4.72f), new Vector3(7.5f, 0.09f, 0.28f), trim);
            CreateWorldText("ClassLabel", parent, string.Empty, new Vector3(-1.2f, 3.14f, 4.72f), Quaternion.Euler(0f, 180f, 0f), 0.16f, new Color(0.08f, 0.25f, 0.20f));

            BuildWindows(parent, wall, metal, glass, exterior);
            BuildDoor(parent, wall, trim, metal);
            BuildClock(parent, trim, metal);
            BuildBoardDetails(parent);
            BuildRearBulletin(parent);
            BuildArchitecturalDetails(parent);
        }

        private static void BuildArchitecturalDetails(Transform parent)
        {
            Material baseboard = Mat("M_Baseboard");
            Material ceilingGrid = Mat("M_CeilingGrid");
            Cube("FrontBaseboard", parent, new Vector3(0f, 0.09f, 4.82f), new Vector3(13.7f, 0.18f, 0.08f), baseboard);
            Cube("BackBaseboard", parent, new Vector3(0f, 0.09f, -4.82f), new Vector3(13.7f, 0.18f, 0.08f), baseboard);
            Cube("LeftBaseboard", parent, new Vector3(-6.82f, 0.09f, 0f), new Vector3(0.08f, 0.18f, 9.55f), baseboard);
            Cube("WindowSill", parent, new Vector3(6.80f, 0.91f, 0f), new Vector3(0.34f, 0.09f, 9.55f), baseboard);

            for (int i = -3; i <= 3; i++)
            {
                Cube($"CeilingGridX_{i}", parent, new Vector3(i * 1.95f, 3.325f, 0f), new Vector3(0.018f, 0.018f, 9.75f), ceilingGrid);
            }

            for (int i = -2; i <= 2; i++)
            {
                Cube($"CeilingGridZ_{i}", parent, new Vector3(0f, 3.325f, i * 1.92f), new Vector3(13.75f, 0.018f, 0.018f), ceilingGrid);
            }
        }

        private static void BuildWindows(Transform parent, Material wall, Material metal, Material glass, Material exterior)
        {
            Quad("ExteriorBackdrop", parent, new Vector3(7.28f, 1.82f, 0f), new Vector3(9.5f, 2.2f, 1f), Quaternion.Euler(0f, 90f, 0f), exterior);
            for (int i = 0; i < 4; i++)
            {
                float z = -3.55f + i * 2.38f;
                Cube($"WindowFrameV_{i}_A", parent, new Vector3(6.91f, 1.75f, z - 1.12f), new Vector3(0.18f, 2.55f, 0.08f), metal);
                Cube($"WindowFrameV_{i}_B", parent, new Vector3(6.91f, 1.75f, z + 1.12f), new Vector3(0.18f, 2.55f, 0.08f), metal);
                Cube($"WindowFrameH_{i}_A", parent, new Vector3(6.91f, 0.95f, z), new Vector3(0.18f, 0.08f, 2.3f), metal);
                Cube($"WindowFrameH_{i}_B", parent, new Vector3(6.91f, 2.12f, z), new Vector3(0.18f, 0.08f, 2.3f), metal);
                Cube($"WindowGlass_{i}", parent, new Vector3(6.96f, 1.76f, z), new Vector3(0.035f, 2.45f, 2.18f), glass);
            }
        }

        private static void BuildDoor(Transform parent, Material wall, Material wood, Material metal)
        {
            Cube("Door", parent, new Vector3(-5.75f, 1.16f, 4.82f), new Vector3(1.65f, 2.32f, 0.12f), wood);
            Cube("DoorWindow", parent, new Vector3(-5.75f, 1.75f, 4.73f), new Vector3(0.68f, 0.78f, 0.04f), Mat("M_Glass"));
            Sphere("DoorHandle", parent, new Vector3(-5.16f, 1.05f, 4.63f), new Vector3(0.10f, 0.10f, 0.10f), metal);
        }

        private static void BuildClock(Transform parent, Material rim, Material hand)
        {
            GameObject clock = Cylinder("WallClock", parent, new Vector3(-4.72f, 2.88f, 4.72f), new Vector3(0.34f, 0.06f, 0.34f), Quaternion.Euler(90f, 0f, 0f), rim);
            Cylinder("ClockFace", clock.transform, new Vector3(0f, 0.055f, 0f), new Vector3(0.88f, 0.03f, 0.88f), Quaternion.identity, Mat("M_ClockFace"));
            Cube("HourHand", clock.transform, new Vector3(0f, 0.09f, 0.09f), new Vector3(0.025f, 0.025f, 0.20f), hand);
            Cube("MinuteHand", clock.transform, new Vector3(0.09f, 0.10f, 0f), new Vector3(0.28f, 0.025f, 0.025f), hand);
        }

        private static void BuildFurniture(Transform furniture, Transform props)
        {
            Material wood = Mat("M_DeskWood");
            Material metal = Mat("M_DeskMetal");
            Material chair = Mat("M_ChairPlastic");
            Material paper = Mat("M_Paper");

            float[] xs = { -4.6f, -2.3f, 0f, 2.3f, 4.6f };
            float[] zs = { 2.1f, 0.4f, -1.3f, -3.0f };
            int deskIndex = 0;
            foreach (float z in zs)
            {
                foreach (float x in xs)
                {
                    CreateStudentDesk(furniture, x, z, deskIndex++, wood, metal, chair);
                }
            }

            GameObject teacherDesk = RootObject("TeacherDesk", furniture, new Vector3(-4.9f, 0f, 3.25f));
            RoundedBox("Top", teacherDesk.transform, new Vector3(0f, 0.82f, 0f), new Vector3(2.5f, 0.11f, 0.9f), 0.045f, "TeacherDeskTop", wood);
            Cube("Front", teacherDesk.transform, new Vector3(0f, 0.43f, 0.39f), new Vector3(2.5f, 0.75f, 0.08f), wood);
            Cube("LeftSide", teacherDesk.transform, new Vector3(-1.18f, 0.42f, 0f), new Vector3(0.10f, 0.82f, 0.82f), wood);
            Cube("RightDrawers", teacherDesk.transform, new Vector3(0.92f, 0.43f, 0f), new Vector3(0.48f, 0.76f, 0.78f), wood);
            Cube("Clipboard", props, new Vector3(-5.25f, 0.90f, 3.16f), new Vector3(0.55f, 0.025f, 0.36f), paper);
            Cube("TeacherBook", props, new Vector3(-4.45f, 0.91f, 3.22f), new Vector3(0.48f, 0.06f, 0.32f), Mat("M_BookTeal"));
            Cylinder("TeacherTumbler", props, new Vector3(-5.78f, 1.08f, 3.24f), new Vector3(0.12f, 0.48f, 0.12f), Quaternion.identity, Mat("M_Tumbler"));

            BuildLockers(furniture, wood, metal);
            BuildWallPoster(props);
            BuildClassroomProps(props);
        }

        private static void CreateStudentDesk(Transform parent, float x, float z, int index, Material wood, Material metal, Material chair)
        {
            GameObject desk = RootObject($"StudentDesk_{index:00}", parent, new Vector3(x, 0f, z));
            RoundedBox("Desktop", desk.transform, new Vector3(0f, 0.70f, 0f), new Vector3(0.96f, 0.065f, 0.62f), 0.025f, "StudentDesktop", wood);
            Cube("DesktopEdge", desk.transform, new Vector3(0f, 0.662f, 0f), new Vector3(0.98f, 0.022f, 0.64f), Mat("M_DeskEdge"));
            Cube("Apron", desk.transform, new Vector3(0f, 0.53f, 0.23f), new Vector3(0.82f, 0.24f, 0.05f), metal);
            Cube("Shelf", desk.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.80f, 0.05f, 0.46f), metal);
            CreateFourLegs(desk.transform, 0.39f, 0.24f, 0.34f, metal);
            Cylinder("LegBrace", desk.transform, new Vector3(0f, 0.28f, 0.27f), new Vector3(0.026f, 0.51f, 0.026f), Quaternion.Euler(0f, 0f, 90f), metal);

            GameObject seat = RootObject("Chair", desk.transform, new Vector3(0f, 0f, -0.58f));
            RoundedBox("Seat", seat.transform, new Vector3(0f, 0.39f, 0f), new Vector3(0.52f, 0.07f, 0.46f), 0.025f, "ChairSeat", chair);
            Cube("SeatEdge", seat.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.54f, 0.025f, 0.48f), Mat("M_DeskEdge"));
            GameObject chairBack = RoundedBox("Back", seat.transform, new Vector3(0f, 0.80f, -0.30f), new Vector3(0.56f, 0.36f, 0.055f), 0.022f, "ChairBack", chair);
            chairBack.transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);
            Cylinder("BackSupportL", seat.transform, new Vector3(-0.21f, 0.64f, -0.27f), new Vector3(0.024f, 0.25f, 0.024f), Quaternion.Euler(-9f, 0f, 0f), metal);
            Cylinder("BackSupportR", seat.transform, new Vector3(0.21f, 0.64f, -0.27f), new Vector3(0.024f, 0.25f, 0.024f), Quaternion.Euler(-9f, 0f, 0f), metal);
            CreateFourLegs(seat.transform, 0.21f, 0.17f, 0.22f, metal);
        }

        private static void CreateFourLegs(Transform parent, float x, float z, float height, Material material)
        {
            Vector3[] positions =
            {
                new Vector3(-x, height, -z), new Vector3(x, height, -z),
                new Vector3(-x, height, z), new Vector3(x, height, z)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                Cylinder($"Leg_{i}", parent, positions[i], new Vector3(0.035f, height, 0.035f), Quaternion.identity, material);
            }
        }

        private static void BuildLockers(Transform parent, Material wood, Material metal)
        {
            Material lockerPaint = Mat("M_LockerPaint");
            for (int i = 0; i < 7; i++)
            {
                float x = -0.5f + i * 0.95f;
                Material doorPaint = i % 2 == 0 ? Mat("M_LockerYellow") : lockerPaint;
                GameObject locker = RootObject($"Locker_{i:00}", parent, new Vector3(x, 0f, -4.72f));
                Cube("Body", locker.transform, new Vector3(0f, 0.61f, 0f), new Vector3(0.91f, 1.20f, 0.50f), lockerPaint);
                Cube("DoorTop", locker.transform, new Vector3(0f, 0.88f, 0.27f), new Vector3(0.82f, 0.54f, 0.035f), doorPaint);
                Cube("DoorBottom", locker.transform, new Vector3(0f, 0.34f, 0.27f), new Vector3(0.82f, 0.50f, 0.035f), doorPaint);
                Cube("KickPlate", locker.transform, new Vector3(0f, 0.10f, 0.30f), new Vector3(0.80f, 0.12f, 0.025f), Mat("M_DeskEdge"));
                Cube("LabelHolder", locker.transform, new Vector3(-0.19f, 1.03f, 0.30f), new Vector3(0.28f, 0.10f, 0.018f), metal);
                for (int vent = 0; vent < 3; vent++)
                {
                    Cube($"Vent_{vent}", locker.transform, new Vector3(-0.16f + vent * 0.16f, 0.73f, 0.31f), new Vector3(0.09f, 0.018f, 0.016f), Mat("M_DeskEdge"));
                }
                Cylinder("HandleTop", locker.transform, new Vector3(0.28f, 0.86f, 0.31f), new Vector3(0.025f, 0.07f, 0.025f), Quaternion.Euler(90f, 0f, 0f), metal);
                Cylinder("HandleBottom", locker.transform, new Vector3(0.28f, 0.35f, 0.31f), new Vector3(0.025f, 0.07f, 0.025f), Quaternion.Euler(90f, 0f, 0f), metal);
            }
        }

        private static void BuildClassroomProps(Transform parent)
        {
            Material paper = Mat("M_BookPaper");
            Material teal = Mat("M_BookTeal");
            Material rubber = Mat("M_Rubber");
            Cube("MinjunNotebook", parent, new Vector3(0.43f, 0.80f, 0.86f), new Vector3(0.43f, 0.018f, 0.30f), paper);
            Cylinder("MinjunPencil", parent, new Vector3(0.68f, 0.83f, 0.78f), new Vector3(0.012f, 0.19f, 0.012f), Quaternion.Euler(90f, 0f, 22f), teal);
            Cube("BackRowBooks", parent, new Vector3(2.62f, 0.81f, -2.85f), new Vector3(0.34f, 0.08f, 0.25f), teal);
            Cube("PencilCase", parent, new Vector3(-2.18f, 0.82f, -0.84f), new Vector3(0.34f, 0.09f, 0.13f), rubber);
            Vector3[] devicePositions =
            {
                new Vector3(-4.6f, 0.78f, 2.08f), new Vector3(2.3f, 0.78f, 2.08f),
                new Vector3(-2.3f, 0.78f, 0.38f), new Vector3(4.6f, 0.78f, 0.38f),
                new Vector3(0f, 0.78f, -1.32f), new Vector3(2.3f, 0.78f, -3.02f)
            };
            for (int i = 0; i < devicePositions.Length; i++)
            {
                Cube($"StudentTablet_{i:00}", parent, devicePositions[i], new Vector3(0.40f, 0.025f, 0.28f), Mat("M_ElectronicFrame"));
                CreateHangingBackpack(parent, i, devicePositions[i]);
            }

            GameObject plant = RootObject("ClassroomPlant", parent, new Vector3(5.95f, 0f, 4.20f));
            Cylinder("Pot", plant.transform, new Vector3(0f, 0.24f, 0f), new Vector3(0.25f, 0.24f, 0.25f), Quaternion.identity, Mat("M_PlantPot"));
            Cylinder("Stem", plant.transform, new Vector3(0f, 0.63f, 0f), new Vector3(0.035f, 0.38f, 0.035f), Quaternion.identity, Mat("M_PlantLeaf"));
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f;
                Vector3 leafPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.22f, 0.74f + (i % 2) * 0.12f, Mathf.Sin(angle * Mathf.Deg2Rad) * 0.22f);
                GameObject leaf = Sphere($"Leaf_{i}", plant.transform, leafPosition, new Vector3(0.28f, 0.10f, 0.16f), Mat("M_PlantLeaf"));
                leaf.transform.localRotation = Quaternion.Euler(0f, -angle, 18f);
            }
        }

        private static void BuildWallPoster(Transform parent)
        {
            Cube("RespectPosterBacking", parent, new Vector3(5.5f, 2.15f, 4.72f), new Vector3(1.25f, 1.4f, 0.04f), Mat("M_PosterFrame"));
            Cube("RespectPosterPaper", parent, new Vector3(5.5f, 2.15f, 4.68f), new Vector3(1.08f, 1.22f, 0.025f), Mat("M_Paper"));
            CreateWorldText("RespectPosterText", parent, string.Empty, new Vector3(5.5f, 2.18f, 4.63f), Quaternion.Euler(0f, 180f, 0f), 0.095f, new Color(0.09f, 0.30f, 0.22f));
        }

        private static void BuildLighting(Transform parent)
        {
            GameObject sunObject = new GameObject("Daylight");
            sunObject.transform.SetParent(parent, false);
            sunObject.transform.rotation = Quaternion.Euler(28f, -58f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.92f, 0.80f);
            sun.intensity = 0.84f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;

            Material fixture = Mat("M_LightFixture");
            Material emissive = Mat("M_Fluorescent");
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    float x = -3.8f + col * 3.8f;
                    float z = -2.4f + row * 4.4f;
                    Cube($"Fixture_{row}_{col}", parent, new Vector3(x, 3.28f, z), new Vector3(2.1f, 0.08f, 0.55f), fixture);
                    Cube($"Diffuser_{row}_{col}", parent, new Vector3(x, 3.23f, z), new Vector3(1.86f, 0.03f, 0.38f), emissive);
                    GameObject lightObject = new GameObject($"FluorescentLight_{row}_{col}");
                    lightObject.transform.SetParent(parent, false);
                    lightObject.transform.position = new Vector3(x, 3.0f, z);
                    Light light = lightObject.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 5.4f;
                    light.intensity = 0.42f;
                    light.color = new Color(1f, 0.96f, 0.88f);
                    light.shadows = LightShadows.None;
                }
            }

            GameObject reflectionObject = new GameObject("ClassroomReflectionProbe");
            reflectionObject.transform.SetParent(parent, false);
            reflectionObject.transform.position = new Vector3(0f, 1.65f, 0f);
            ReflectionProbe probe = reflectionObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(13.4f, 3.1f, 9.4f);
            probe.resolution = 128;
        }

        private static Camera BuildCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("TeacherCamera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(-5.35f, 1.58f, 3.60f);
            cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0.85f, 1.02f, 0.05f) - cameraObject.transform.position);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<TeacherCameraController>();
            cameraObject.AddComponent<TeacherFootstepAudio>();
            Light faceLight = cameraObject.AddComponent<Light>();
            faceLight.type = LightType.Spot;
            faceLight.color = new Color(1f, 0.95f, 0.90f);
            faceLight.intensity = 0.85f;
            faceLight.range = 7f;
            faceLight.spotAngle = 54f;
            faceLight.innerSpotAngle = 34f;
            faceLight.shadows = LightShadows.None;
            return camera;
        }

        private sealed class StudentSet
        {
            public NpcPerformance focal;
            public NpcPerformance[] classmates;
        }

        private static StudentSet BuildStudents(Transform parent, Transform teacherTarget)
        {
            RuntimeAnimatorController maleController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{ControllerRoot}/RocketboxMale.controller");
            RuntimeAnimatorController femaleController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{ControllerRoot}/RocketboxFemale.controller");

            NpcPerformance focal = CreateStudent(parent, "FocalStudent_Minjun", "Male_Child_01", new Vector3(0f, 0f, -0.18f), maleController, StudentAffect.Distressed, 15, 1);
            (string name, string asset, Vector3 position, StudentAffect affect)[] roster =
            {
                ("Classmate_Seoyeon", "Female_Child_01", new Vector3(-4.6f, 0f, 1.52f), StudentAffect.Uneasy),
                ("Classmate_Doyun", "Male_Child_01", new Vector3(-2.3f, 0f, 1.52f), StudentAffect.Calm),
                ("Classmate_Yuna", "Female_Child_01", new Vector3(0f, 0f, 1.52f), StudentAffect.Calm),
                ("Classmate_Junseo", "Male_Child_01", new Vector3(2.3f, 0f, 1.52f), StudentAffect.Uneasy),
                ("Classmate_Sua", "Female_Child_01", new Vector3(4.6f, 0f, 1.52f), StudentAffect.Calm),
                ("Classmate_Hyeonwoo", "Male_Child_01", new Vector3(-4.6f, 0f, -0.18f), StudentAffect.Calm),
                ("Classmate_Jiwon", "Female_Child_01", new Vector3(-2.3f, 0f, -0.18f), StudentAffect.Uneasy),
                ("Classmate_Eunho", "Male_Child_01", new Vector3(2.3f, 0f, -0.18f), StudentAffect.Calm),
                ("Classmate_Nayeon", "Female_Child_01", new Vector3(4.6f, 0f, -0.18f), StudentAffect.Uneasy),
                ("Classmate_Jiho", "Male_Child_01", new Vector3(-2.3f, 0f, -1.88f), StudentAffect.Calm),
                ("Classmate_Harin", "Female_Child_01", new Vector3(2.3f, 0f, -1.88f), StudentAffect.Uneasy),
                ("Classmate_Geonu", "Male_Child_01", new Vector3(-4.6f, 0f, -1.88f), StudentAffect.Calm),
                ("Classmate_Chaewon", "Female_Child_01", new Vector3(0f, 0f, -1.88f), StudentAffect.Calm),
                ("Classmate_Seojun", "Male_Child_01", new Vector3(4.6f, 0f, -1.88f), StudentAffect.Uneasy)
            };

            NpcPerformance[] classmates = new NpcPerformance[roster.Length];
            int femaleFaceIndex = 1;
            int maleFaceIndex = 2;
            for (int i = 0; i < roster.Length; i++)
            {
                (string name, string asset, Vector3 position, StudentAffect affect) = roster[i];
                bool female = asset.Contains("Female");
                RuntimeAnimatorController controller = female ? femaleController : maleController;
                int faceTextureIndex = female ? femaleFaceIndex++ : maleFaceIndex++;
                if (maleFaceIndex > 5)
                {
                    maleFaceIndex = 1;
                }
                NpcPerformance classmate = CreateStudent(parent, name, asset, position, controller, affect, i + 1, faceTextureIndex);
                SerializedObject classmateData = new SerializedObject(classmate);
                classmateData.FindProperty("ambientPerformance").boolValue = true;
                classmateData.ApplyModifiedPropertiesWithoutUndo();
                classmate.transform.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.04f, (i % 5) / 4f);
                StudentGazeController gaze = classmate.gameObject.AddComponent<StudentGazeController>();
                bool startsAttentive = true;
                float attentionBias = 1f;
                gaze.Configure(teacherTarget, attentionBias, startsAttentive);
                NpcIdleBehaviorController idleBehavior = classmate.gameObject.AddComponent<NpcIdleBehaviorController>();
                idleBehavior.Configure(startsAttentive, i);
                classmates[i] = classmate;
            }

            return new StudentSet { focal = focal, classmates = classmates };
        }

        private static NpcPerformance CreateStudent(
            Transform parent,
            string objectName,
            string assetName,
            Vector3 position,
            RuntimeAnimatorController controller,
            StudentAffect affect,
            int appearanceVariant = 0,
            int faceTextureIndex = 1)
        {
            string path = $"{RocketboxRoot}/Avatars/Children/{assetName}/Export/{assetName}_facial.fbx";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject container = RootObject(objectName, parent, position);
            container.transform.rotation = Quaternion.identity;
            if (source == null)
            {
                CreateAvatarPlaceholder(container.transform, assetName.Contains("Female"));
            }
            else
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, container.transform);
                instance.name = "RocketboxAvatar";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                Animator animator = instance.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    if (appearanceVariant > 0)
                    {
                        ApplyStudentAppearance(container, animator, appearanceVariant, assetName.Contains("Female"), faceTextureIndex);
                    }
                }

                foreach (SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.updateWhenOffscreen = false;
                    renderer.forceMatrixRecalculationPerRender = true;
                }

                int blendShapeCount = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(renderer => renderer.sharedMesh != null)
                    .Sum(renderer => renderer.sharedMesh.blendShapeCount);
                Debug.Log($"ROCKETBOX_AVATAR_OK {assetName} humanoid={(animator != null && animator.isHuman)} blendShapes={blendShapeCount}");
            }

            container.AddComponent<FacialActionUnitController>();
            NpcPerformance performance = container.AddComponent<NpcPerformance>();
            SerializedObject serialized = new SerializedObject(performance);
            serialized.FindProperty("initialAffect").enumValueIndex = (int)affect;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return performance;
        }

        private static void ApplyStudentAppearance(GameObject student, Animator animator, int variant, bool female, int faceTextureIndex)
        {
            Vector3[] faceMorphs =
            {
                new Vector3(0.94f, 1.035f, 0.98f),
                new Vector3(1.045f, 0.975f, 1.01f),
                new Vector3(0.975f, 1.015f, 1.045f),
                new Vector3(1.025f, 1.025f, 0.965f),
                new Vector3(0.955f, 0.985f, 1.025f),
                new Vector3(1.055f, 1.005f, 0.985f),
                new Vector3(0.985f, 1.045f, 1.015f)
            };
            Vector3 faceMorph = faceMorphs[(variant - 1) % faceMorphs.Length];
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
            {
                head.localScale = Vector3.Scale(head.localScale, faceMorph);
            }

            Transform jaw = animator.GetBoneTransform(HumanBodyBones.Jaw);
            if (jaw != null)
            {
                float jawWidth = Mathf.Lerp(0.95f, 1.055f, (variant % 5) / 4f);
                jaw.localScale = Vector3.Scale(jaw.localScale, new Vector3(jawWidth, 1f, 1f));
            }

            ApplyStudentClothingTint(student, variant, female);
            ApplyStudentHairStyle(student, variant, female, faceTextureIndex);
        }

        private static void ApplyStudentHairStyle(GameObject student, int variant, bool female, int faceTextureIndex)
        {
            string[] styleNames = { "ShortCrop", "SidePart", "SoftFringe", "LowPonytail", "NaturalPonytail" };
            RootObject($"HairStyle_{styleNames[variant % styleNames.Length]}", student.transform, Vector3.zero);
            Color naturalTone = variant % 3 == 0
                ? new Color(0.075f, 0.045f, 0.030f, 1f)
                : new Color(0.018f, 0.014f, 0.012f, 1f);
            Shader shader = Shader.Find("AdieLab/StudentHeadHairTint");
            string gender = female ? "Female" : "Male";
            string generatedFacePath = $"Assets/Generated/StudentFaces/Face_{gender}_{faceTextureIndex:00}.png";
            Texture2D generatedFace = AssetDatabase.LoadAssetAtPath<Texture2D>(generatedFacePath);
            Vector4 paintedEyeCenters = GetGeneratedFaceEyeCenters(female, faceTextureIndex);
            Vector4 socketEyeCenters = female
                ? new Vector4(0.4296f, 0.7175f, 0.5696f, 0.7178f)
                : new Vector4(0.4309f, 0.7186f, 0.5700f, 0.7183f);

            foreach (SkinnedMeshRenderer renderer in student.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || !source.name.Contains("head", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string materialPath = $"{MaterialRoot}/M_StudentHair_{variant:00}_{i:00}.mat";
                    Material hair = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (hair == null)
                    {
                        hair = new Material(shader) { name = $"M_StudentHair_{variant:00}_{i:00}" };
                        AssetDatabase.CreateAsset(hair, materialPath);
                    }

                    hair.shader = shader;
                    hair.SetTexture("_MainTex", generatedFace != null ? generatedFace : source.mainTexture);
                    hair.SetTexture("_ReferenceTex", source.mainTexture);
                    hair.SetTextureScale("_MainTex", source.mainTextureScale);
                    hair.SetTextureOffset("_MainTex", source.mainTextureOffset);
                    hair.SetTextureScale("_ReferenceTex", source.mainTextureScale);
                    hair.SetTextureOffset("_ReferenceTex", source.mainTextureOffset);
                    if (source.HasProperty("_BumpMap"))
                    {
                        hair.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                    }
                    if (source.HasProperty("_BumpScale"))
                    {
                        hair.SetFloat("_BumpScale", source.GetFloat("_BumpScale"));
                    }
                    hair.SetColor("_HairColor", naturalTone);
                    hair.SetFloat("_HairTintStrength", generatedFace != null ? 0.10f : 0.92f);
                    hair.SetFloat("_EyeSocketCorrection", generatedFace != null ? 1f : 0f);
                    hair.SetFloat("_EyeSocketBrightness", 1f);
                    hair.SetVector("_PaintedEyeCenters", paintedEyeCenters);
                    hair.SetVector("_PaintedEyeRadius", new Vector4(0.060f, 0.030f, 0f, 0f));
                    hair.SetVector("_SocketEyeCenters", socketEyeCenters);
                    hair.SetVector("_SocketEyeRadius", new Vector4(0.062f, 0.035f, 0f, 0f));
                    hair.SetFloat("_SocketBlendStrength", 0.72f);
                    hair.SetVector("_PaintedMouthCenterRadius", new Vector4(0.500f, 0.558f, 0.105f, 0.055f));
                    bool needsFemaleCentralRestore = female && (faceTextureIndex == 3 || faceTextureIndex == 6);
                    hair.SetFloat("_MouthRestoreStrength", generatedFace != null
                        ? (!female ? 0.92f : needsFemaleCentralRestore ? 0.88f : 0f)
                        : 0f);
                    hair.SetVector("_PaintedNoseCenterRadius", new Vector4(0.500f, 0.610f, 0.130f, 0.125f));
                    hair.SetFloat("_NoseNeutralizeStrength", generatedFace != null
                        ? (!female ? 0.96f : needsFemaleCentralRestore ? 0.90f : 0f)
                        : 0f);
                    hair.SetFloat("_FacialNormalSuppression", generatedFace != null ? 1f : 0f);
                    hair.SetVector("_IrisAtlasCenter", new Vector4(0.2636f, 0.0688f, 0f, 0f));
                    hair.SetColor("_IrisColor", new Color(0.30f, 0.11f, 0.04f, 1f));
                    hair.SetFloat("_IrisTintStrength", 1f);
                    hair.SetFloat("_IrisRadius", 0.0235f);
                    hair.SetFloat("_PupilRadius", 0.0075f);
                    hair.name = $"M_StudentHair_{variant:00}_{i:00}";
                    materials[i] = hair;
                    EditorUtility.SetDirty(hair);
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private static Vector4 GetGeneratedFaceEyeCenters(bool female, int faceTextureIndex)
        {
            if (female)
            {
                switch (faceTextureIndex)
                {
                    case 1: return new Vector4(0.4321f, 0.7145f, 0.5662f, 0.7147f);
                    case 2: return new Vector4(0.4354f, 0.7111f, 0.5646f, 0.7120f);
                    case 3: return new Vector4(0.4411f, 0.7098f, 0.5827f, 0.7103f);
                    case 4: return new Vector4(0.4333f, 0.7118f, 0.5633f, 0.7125f);
                    case 5: return new Vector4(0.4323f, 0.7085f, 0.5646f, 0.7091f);
                    case 6: return new Vector4(0.4294f, 0.6996f, 0.5680f, 0.7010f);
                    case 7: return new Vector4(0.4277f, 0.7115f, 0.5731f, 0.7119f);
                }
            }
            else
            {
                switch (faceTextureIndex)
                {
                    case 1: return new Vector4(0.4275f, 0.7004f, 0.5714f, 0.7004f);
                    case 2: return new Vector4(0.4339f, 0.7094f, 0.5687f, 0.7096f);
                    case 3: return new Vector4(0.4356f, 0.7102f, 0.5661f, 0.7112f);
                    case 4: return new Vector4(0.4330f, 0.7068f, 0.5676f, 0.7070f);
                    case 5: return new Vector4(0.4307f, 0.6794f, 0.5654f, 0.6807f);
                }
            }

            return new Vector4(0.4330f, 0.7050f, 0.5670f, 0.7050f);
        }

        private static void ApplyStudentClothingTint(GameObject student, int variant, bool female)
        {
            string[] fabricTextures =
            {
                "Cloth_01_HeatherCotton.png", "Cloth_02_RibKnit.png", "Cloth_03_WaffleKnit.png",
                "Cloth_04_CottonTwill.png", "Cloth_05_SlubJersey.png", "Cloth_06_BrushedFleece.png",
                "Cloth_07_MicroCheck.png", "Cloth_08_MarledStripe.png"
            };
            int styleIndex = Mathf.Clamp(variant - 1, 0, 14);
            System.Random styleRandom = new System.Random(20260717 + styleIndex * 7919);
            float hue = Mathf.Repeat(styleIndex * 0.61803398875f + 0.03f +
                                     ((float)styleRandom.NextDouble() - 0.5f) * 0.025f, 1f);
            Color clothingColor = Color.HSVToRGB(
                hue,
                Mathf.Lerp(0.50f, 0.72f, (float)styleRandom.NextDouble()),
                Mathf.Lerp(0.42f, 0.68f, (float)styleRandom.NextDouble()));
            Color accentColor = Color.HSVToRGB(
                Mathf.Repeat(hue + Mathf.Lerp(0.28f, 0.50f, (float)styleRandom.NextDouble()), 1f),
                Mathf.Lerp(0.34f, 0.58f, (float)styleRandom.NextDouble()),
                Mathf.Lerp(0.76f, 0.92f, (float)styleRandom.NextDouble()));
            Color graphicColor = Color.HSVToRGB(
                Mathf.Repeat(hue + Mathf.Lerp(0.38f, 0.62f, (float)styleRandom.NextDouble()), 1f),
                Mathf.Lerp(0.20f, 0.46f, (float)styleRandom.NextDouble()),
                Mathf.Lerp(0.88f, 0.98f, (float)styleRandom.NextDouble()));
            float patternType = 1f + (styleIndex * 5 + 2) % 6;
            float patternScale = Mathf.Lerp(8f, 22f, (float)styleRandom.NextDouble());
            float patternStrength = Mathf.Lerp(0.18f, 0.42f, (float)styleRandom.NextDouble());
            int fabricIndex = (styleIndex * 5 + 2) % fabricTextures.Length;
            float fabricScale = Mathf.Lerp(1.5f, 3.1f, (float)styleRandom.NextDouble());
            float fabricStrength = Mathf.Lerp(0.24f, 0.38f, (float)styleRandom.NextDouble());
            float graphicIndex = (styleIndex * 7 + 3) % 15;
            float graphicScale = Mathf.Lerp(0.76f, 1.12f, (float)styleRandom.NextDouble());
            float graphicRotation = Mathf.Lerp(-12f, 12f, (float)styleRandom.NextDouble());
            Vector4 graphicOffset = new Vector4(
                Mathf.Lerp(-0.014f, 0.014f, (float)styleRandom.NextDouble()),
                Mathf.Lerp(-0.010f, 0.010f, (float)styleRandom.NextDouble()),
                0f,
                0f);
            float graphicStrength = Mathf.Lerp(0.88f, 0.98f, (float)styleRandom.NextDouble());
            Shader shader = Shader.Find("AdieLab/StudentClothingTint");
            Texture2D graphicAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Generated/StudentClothing/GraphicAtlas_15.png");
            foreach (SkinnedMeshRenderer renderer in student.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || !source.name.Contains("body", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string materialPath = $"{MaterialRoot}/M_StudentOutfit_{variant:00}.mat";
                    Material outfit = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (outfit == null)
                    {
                        outfit = new Material(shader) { name = $"M_StudentOutfit_{variant:00}" };
                        AssetDatabase.CreateAsset(outfit, materialPath);
                    }

                    outfit.shader = shader;
                    outfit.SetTexture("_MainTex", source.mainTexture);
                    outfit.SetTextureScale("_MainTex", source.mainTextureScale);
                    outfit.SetTextureOffset("_MainTex", source.mainTextureOffset);
                    if (source.HasProperty("_BumpMap"))
                    {
                        outfit.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                    }

                    outfit.SetColor("_ClothingColor", clothingColor);
                    outfit.SetColor("_AccentColor", accentColor);
                    outfit.SetColor("_GraphicColor", graphicColor);
                    outfit.SetFloat("_TintStrength", 0.98f);
                    outfit.SetFloat("_PatternType", patternType);
                    outfit.SetFloat("_PatternScale", patternScale);
                    outfit.SetFloat("_PatternStrength", patternStrength);
                    outfit.SetTexture("_FabricTex", AssetDatabase.LoadAssetAtPath<Texture2D>(
                        $"Assets/Generated/StudentClothing/{fabricTextures[fabricIndex]}"));
                    outfit.SetFloat("_FabricScale", fabricScale);
                    outfit.SetFloat("_FabricStrength", fabricStrength);
                    outfit.SetTexture("_GraphicAtlas", graphicAtlas);
                    outfit.SetFloat("_GraphicIndex", graphicIndex);
                    outfit.SetFloat("_GraphicCenterY", female ? 0.455f : 0.430f);
                    outfit.SetFloat("_GraphicScale", graphicScale);
                    outfit.SetFloat("_GraphicRotation", graphicRotation);
                    outfit.SetVector("_GraphicOffset", graphicOffset);
                    outfit.SetFloat("_GraphicStrength", graphicStrength);
                    outfit.SetFloat("_SkinProtection", 1f);
                    outfit.SetFloat("_Glossiness", 0.28f);
                    materials[i] = outfit;
                    EditorUtility.SetDirty(outfit);
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private static void CreateAvatarPlaceholder(Transform parent, bool female)
        {
            Material clothing = female ? Mat("M_PlaceholderNavy") : Mat("M_PlaceholderBeige");
            Material skin = Mat("M_PlaceholderSkin");
            Capsule("Body", parent, new Vector3(0f, 0.98f, 0f), new Vector3(0.42f, 0.66f, 0.30f), clothing);
            Sphere("Head", parent, new Vector3(0f, 1.64f, 0f), new Vector3(0.32f, 0.36f, 0.30f), skin);
        }

        private static TrainingHud BuildInterface(Transform parent, Camera worldCamera, Transform speechTarget)
        {
            GameObject canvasObject = new GameObject("TrainingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = worldCamera;
            canvas.planeDistance = 0.10f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;

            TMP_FontAsset font = GetOrCreateKoreanFontAsset();
            TrainingHud hud = canvasObject.AddComponent<TrainingHud>();

            RectTransform topBar = Panel(canvasObject.transform, "TopBar", UiTokens.Header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -UiTokens.HeaderHeight), Vector2.zero);
            TMP_Text title = Label(topBar, "AppTitle", "정서·행동 지원 교사 대응 훈련", font, UiTokens.TitleSize, FontStyles.Bold, UiTokens.TextOnDark, TextAlignmentOptions.MidlineLeft);
            SetRect(title.rectTransform, new Vector2(0f, 0f), new Vector2(0.285f, 1f), new Vector2(32f, 0f), new Vector2(-10f, 0f));
            TMP_Text beat = Label(topBar, "BeatLabel", "상황 1/3", font, 19, FontStyles.Bold, UiTokens.AccentText, TextAlignmentOptions.MidlineRight);
            SetRect(beat.rectTransform, new Vector2(0.76f, 0f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-32f, 0f));

            string[] modeNames = { "관찰", "대응", "대화", "디브리핑" };
            RectTransform sceneSwitch = Panel(
                topBar,
                nameof(TrainingSceneSelector),
                UiTokens.Option,
                new Vector2(0.292f, 0.20f),
                new Vector2(0.37f, 0.80f),
                Vector2.zero,
                Vector2.zero);
            Button sceneToggleButton = sceneSwitch.gameObject.AddComponent<Button>();
            sceneToggleButton.targetGraphic = sceneSwitch.GetComponent<Image>();
            sceneSwitch.gameObject.AddComponent<ButtonMotion>();
            TMP_Text sceneToggleLabel = Label(
                sceneSwitch,
                nameof(TrainingSceneId),
                new string(new[] { 'S', 'C', 'E', 'N', 'E', ' ', '1' }),
                font,
                12,
                FontStyles.Bold,
                UiTokens.TextOnDark,
                TextAlignmentOptions.Center);
            SetRect(sceneToggleLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Button[] modeButtons = new Button[modeNames.Length];
            for (int i = 0; i < modeNames.Length; i++)
            {
                float min = 0.38f + i * 0.092f;
                RectTransform modeRect = Panel(topBar, $"ModeButton_{i}", i == 1 ? UiTokens.Primary : UiTokens.Option, new Vector2(min, 0.20f), new Vector2(min + 0.082f, 0.80f), Vector2.zero, Vector2.zero);
                Button modeButton = modeRect.gameObject.AddComponent<Button>();
                modeButton.targetGraphic = modeRect.GetComponent<Image>();
                modeRect.gameObject.AddComponent<ButtonMotion>();
                TMP_Text modeLabel = Label(modeRect, "Label", modeNames[i], font, 14, FontStyles.Bold, UiTokens.TextOnDark, TextAlignmentOptions.Center);
                SetRect(modeLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                modeButtons[i] = modeButton;
            }

            RectTransform leftPanel = Panel(canvasObject.transform, "SituationPanel", UiTokens.DarkSurface, new Vector2(0f, 0f), new Vector2(0.30f, 0f), new Vector2(24f, 20f), new Vector2(-10f, UiTokens.HudHeight));
            CanvasGroup observationGroup = leftPanel.gameObject.AddComponent<CanvasGroup>();
            AddSoftShadow(leftPanel, 0.24f, 7f);
            CreateChip(leftPanel, "ObservationChip", "실시간 관찰", font, UiTokens.ObservationChip, UiTokens.AccentText, new Vector2(24f, -42f), new Vector2(158f, -14f));
            TMP_Text studentLine = Label(leftPanel, "StudentLine", "학생 대사", font, UiTokens.HeadingSize, FontStyles.Bold, UiTokens.TextOnDark, TextAlignmentOptions.TopLeft);
            SetRect(studentLine.rectTransform, new Vector2(0f, 0.54f), new Vector2(1f, 1f), new Vector2(26f, 12f), new Vector2(-26f, -54f));
            RectTransform observationSurface = Panel(leftPanel, "ObservationDetailsSurface", new Color(0.07f, 0.13f, 0.18f, 0.98f), new Vector2(0f, 0.18f), new Vector2(1f, 0.54f), new Vector2(18f, 2f), new Vector2(-18f, -2f));
            TMP_Text observation = Label(observationSurface, "Observation", "관찰 정보", font, UiTokens.BodySize, FontStyles.Normal, UiTokens.TextOnDark, TextAlignmentOptions.TopLeft);
            observation.lineSpacing = 2f;
            SetRect(observation.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 8f), new Vector2(-14f, -8f));
            TMP_Text score = Label(leftPanel, "Score", "공동조절 점수 0", font, UiTokens.BodySize, FontStyles.Bold, UiTokens.AccentText, TextAlignmentOptions.MidlineLeft);
            SetRect(score.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.18f), new Vector2(26f, 0f), new Vector2(-26f, 0f));

            RectTransform rightPanel = Panel(canvasObject.transform, "ResponsePanel", UiTokens.LightSurface, new Vector2(0.62f, 0f), new Vector2(1f, 0f), new Vector2(10f, 20f), new Vector2(-24f, UiTokens.HudHeight));
            CanvasGroup responseGroup = rightPanel.gameObject.AddComponent<CanvasGroup>();
            AddSoftShadow(rightPanel, 0.18f, 7f);
            TMP_Text responseChipLabel = CreateChip(rightPanel, "ResponseChip", "대응 선택", font, UiTokens.PaleChip, UiTokens.ChipText, new Vector2(22f, -42f), new Vector2(144f, -14f));
            TMP_Text feedback = Label(rightPanel, "Feedback", "대응을 선택하세요.", font, UiTokens.BodySize, FontStyles.Normal, UiTokens.TextOnLight, TextAlignmentOptions.TopLeft);
            SetRect(feedback.rectTransform, new Vector2(0f, 0.65f), new Vector2(1f, 1f), new Vector2(24f, 12f), new Vector2(-24f, -50f));

            Button[] buttons = new Button[3];
            TMP_Text[] labels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                float top = 0.63f - i * 0.19f;
                RectTransform buttonRect = Panel(rightPanel, $"OptionButton_{i + 1}", UiTokens.Option, new Vector2(0f, top - 0.17f), new Vector2(1f, top), new Vector2(18f, 2f), new Vector2(-18f, -2f));
                AddSoftShadow(buttonRect, 0.13f, 3f);
                Button button = buttonRect.gameObject.AddComponent<Button>();
                button.targetGraphic = buttonRect.GetComponent<Image>();
                buttonRect.gameObject.AddComponent<ButtonMotion>();
                ColorBlock colors = button.colors;
                colors.highlightedColor = UiTokens.PrimaryHover;
                colors.pressedColor = new Color(0.05f, 0.35f, 0.35f);
                colors.disabledColor = new Color(0.12f, 0.18f, 0.24f, 0.96f);
                button.colors = colors;
                TMP_Text label = Label(buttonRect, "Label", $"{i + 1}. 선택지", font, 15, FontStyles.Bold, UiTokens.TextOnDark, TextAlignmentOptions.MidlineLeft);
                SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 1f), new Vector2(-20f, -1f));
                label.enableAutoSizing = true;
                label.fontSizeMin = 13f;
                label.fontSizeMax = 15f;
                label.lineSpacing = -5f;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
                buttons[i] = button;
                labels[i] = label;
            }

            RectTransform continueRect = Panel(rightPanel, "ContinueButton", UiTokens.Primary, new Vector2(0.63f, 0.02f), new Vector2(1f, 0.14f), new Vector2(-18f, 0f), new Vector2(-18f, 0f));
            Button continueButton = continueRect.gameObject.AddComponent<Button>();
            continueButton.targetGraphic = continueRect.GetComponent<Image>();
            continueRect.gameObject.AddComponent<ButtonMotion>();
            TMP_Text continueLabel = Label(continueRect, "Label", "다음 상황", font, UiTokens.BodySize, FontStyles.Bold, UiTokens.TextOnDark, TextAlignmentOptions.Center);
            SetRect(continueLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform debriefPanel = Panel(rightPanel, "DebriefPanel", new Color(0.055f, 0.10f, 0.18f, 1f), Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f));
            TMP_Text debriefText = Label(debriefPanel, "DebriefText", "<b>디브리핑</b>\n훈련을 완료하면 6가지 교사 대응 역량이 표시됩니다.", font, 18, FontStyles.Normal, new Color(0.92f, 0.97f, 0.98f, 1f), TextAlignmentOptions.Center);
            debriefText.lineSpacing = 4f;
            SetRect(debriefText.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 24f), new Vector2(-24f, -24f));
            debriefPanel.gameObject.SetActive(false);

            RectTransform dialoguePanel = Panel(canvasObject.transform, "DialoguePanel", UiTokens.DarkSurface, new Vector2(0.30f, 0f), new Vector2(0.62f, 0f), new Vector2(8f, 32f), new Vector2(-8f, 124f));
            CanvasGroup dialogueGroup = dialoguePanel.gameObject.AddComponent<CanvasGroup>();
            AddSoftShadow(dialoguePanel, 0.24f, 7f);
            TMP_Text dialogueStatus = Label(dialoguePanel, "DialogueStatus", "학생에게 직접 말하기 · F: 정면 대화 시점", font, 15, FontStyles.Normal, UiTokens.AccentText, TextAlignmentOptions.MidlineLeft);
            SetRect(dialogueStatus.rectTransform, new Vector2(0f, 0.64f), new Vector2(1f, 1f), new Vector2(18f, 0f), new Vector2(-18f, 0f));

            RectTransform inputRect = Panel(dialoguePanel, "DialogueInput", UiTokens.InputSurface, new Vector2(0f, 0.10f), new Vector2(0.68f, 0.64f), new Vector2(16f, 0f), new Vector2(-5f, 0f));
            TMP_InputField dialogueInput = inputRect.gameObject.AddComponent<TMP_InputField>();
            TMP_Text inputText = Label(inputRect, "Text", string.Empty, font, 16, FontStyles.Normal, UiTokens.TextOnLight, TextAlignmentOptions.MidlineLeft);
            SetRect(inputText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 5f), new Vector2(-12f, -5f));
            TMP_Text placeholder = Label(inputRect, "Placeholder", "예: 지금 많이 답답해 보이는구나. 잠깐 쉴까?", font, 16, FontStyles.Normal, UiTokens.Placeholder, TextAlignmentOptions.MidlineLeft);
            SetRect(placeholder.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 5f), new Vector2(-12f, -5f));
            dialogueInput.textComponent = inputText;
            dialogueInput.placeholder = placeholder;
            dialogueInput.lineType = TMP_InputField.LineType.SingleLine;

            RectTransform microphoneRect = Panel(dialoguePanel, "MicrophoneButton", UiTokens.Option, new Vector2(0.68f, 0.10f), new Vector2(0.80f, 0.64f), new Vector2(5f, 0f), new Vector2(-3f, 0f));
            Button microphoneButton = microphoneRect.gameObject.AddComponent<Button>();
            microphoneButton.targetGraphic = microphoneRect.GetComponent<Image>();
            microphoneRect.gameObject.AddComponent<ButtonMotion>();
            TMP_Text microphoneLabel = Label(microphoneRect, "Label", "마이크", font, 13, FontStyles.Bold, UiTokens.TextOnDark, TextAlignmentOptions.Center);
            SetRect(microphoneLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            microphoneLabel.textWrappingMode = TextWrappingModes.NoWrap;
            microphoneLabel.enableAutoSizing = true;
            microphoneLabel.fontSizeMin = 11f;
            microphoneLabel.fontSizeMax = 13f;

            RectTransform sendRect = Panel(dialoguePanel, "DialogueSendButton", UiTokens.Primary, new Vector2(0.80f, 0.10f), new Vector2(1f, 0.64f), new Vector2(3f, 0f), new Vector2(-16f, 0f));
            Button dialogueSend = sendRect.gameObject.AddComponent<Button>();
            dialogueSend.targetGraphic = sendRect.GetComponent<Image>();
            sendRect.gameObject.AddComponent<ButtonMotion>();
            TMP_Text sendLabel = Label(sendRect, "Label", "말하기", font, 16, FontStyles.Bold, UiTokens.TextOnDark, TextAlignmentOptions.Center);
            SetRect(sendLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform bubble = Panel(canvasObject.transform, "StudentSpeechBubble", UiTokens.BubbleBorder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-270f, -72f), new Vector2(270f, 72f));
            bubble.pivot = new Vector2(0.5f, 0.5f);
            Shadow bubbleShadow = bubble.gameObject.AddComponent<Shadow>();
            bubbleShadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
            bubbleShadow.effectDistance = new Vector2(0f, -9f);
            CanvasGroup bubbleGroup = bubble.gameObject.AddComponent<CanvasGroup>();

            RectTransform bubbleInner = Panel(bubble, "BubbleInner", UiTokens.BubbleSurface, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            TMP_Text speakerChip = CreateChip(bubbleInner, "SpeakerChip", "학생 응답", font, UiTokens.PaleChip, UiTokens.ChipText, new Vector2(24f, -42f), new Vector2(126f, -14f));
            speakerChip.fontSize = 14;
            TMP_Text bubbleText = Label(bubbleInner, "StudentReply", "잠깐만 시간을 주세요.", font, 22, FontStyles.Bold, UiTokens.TextOnLight, TextAlignmentOptions.MidlineLeft);
            SetRect(bubbleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(28f, 14f), new Vector2(-28f, -42f));

            RectTransform tailBorder = Panel(bubble, "BubbleTailBorder", UiTokens.BubbleBorder, new Vector2(0.35f, 0f), new Vector2(0.35f, 0f), new Vector2(-22f, -30f), new Vector2(22f, 12f));
            Image tailBorderImage = tailBorder.GetComponent<Image>();
            tailBorderImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpeechTailSpritePath);
            tailBorderImage.type = Image.Type.Simple;
            RectTransform tailInner = Panel(tailBorder, "BubbleTailInner", UiTokens.BubbleSurface, Vector2.zero, Vector2.one, new Vector2(4f, 5f), new Vector2(-4f, -3f));
            Image tailInnerImage = tailInner.GetComponent<Image>();
            tailInnerImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpeechTailSpritePath);
            tailInnerImage.type = Image.Type.Simple;
            tailBorder.SetAsFirstSibling();
            bubbleInner.SetAsLastSibling();
            bubble.SetAsLastSibling();

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(parent, false);

            SerializedObject hudSerialized = new SerializedObject(hud);
            hudSerialized.FindProperty("beatLabel").objectReferenceValue = beat;
            hudSerialized.FindProperty("studentLine").objectReferenceValue = studentLine;
            hudSerialized.FindProperty("observationText").objectReferenceValue = observation;
            hudSerialized.FindProperty("feedbackText").objectReferenceValue = feedback;
            hudSerialized.FindProperty("scoreText").objectReferenceValue = score;
            hudSerialized.FindProperty("responseChipLabel").objectReferenceValue = responseChipLabel;
            SetArray(hudSerialized.FindProperty("optionButtons"), buttons);
            SetArray(hudSerialized.FindProperty("optionLabels"), labels);
            hudSerialized.FindProperty("continueButton").objectReferenceValue = continueButton;
            hudSerialized.FindProperty("dialogueInput").objectReferenceValue = dialogueInput;
            hudSerialized.FindProperty("dialogueSendButton").objectReferenceValue = dialogueSend;
            hudSerialized.FindProperty("dialogueStatus").objectReferenceValue = dialogueStatus;
            hudSerialized.FindProperty("dialoguePanel").objectReferenceValue = dialoguePanel;
            hudSerialized.FindProperty("rootCanvas").objectReferenceValue = canvas;
            hudSerialized.FindProperty("worldCamera").objectReferenceValue = worldCamera;
            hudSerialized.FindProperty("speechTarget").objectReferenceValue = speechTarget;
            hudSerialized.FindProperty("speechBubble").objectReferenceValue = bubble;
            hudSerialized.FindProperty("speechBubbleText").objectReferenceValue = bubbleText;
            hudSerialized.FindProperty("speechBubbleGroup").objectReferenceValue = bubbleGroup;
            hudSerialized.ApplyModifiedPropertiesWithoutUndo();

            TrainingModeNavigator navigator = canvasObject.AddComponent<TrainingModeNavigator>();
            SerializedObject navigatorSerialized = new SerializedObject(navigator);
            SetArray(navigatorSerialized.FindProperty("modeButtons"), modeButtons);
            navigatorSerialized.FindProperty("observationPanel").objectReferenceValue = observationGroup;
            navigatorSerialized.FindProperty("responsePanel").objectReferenceValue = responseGroup;
            navigatorSerialized.FindProperty("dialoguePanel").objectReferenceValue = dialogueGroup;
            navigatorSerialized.FindProperty("debriefPanel").objectReferenceValue = debriefPanel;
            navigatorSerialized.FindProperty("debriefText").objectReferenceValue = debriefText;
            navigatorSerialized.ApplyModifiedPropertiesWithoutUndo();

            TrainingSceneSelector sceneSelector = canvasObject.AddComponent<TrainingSceneSelector>();
            SerializedObject sceneSelectorSerialized = new SerializedObject(sceneSelector);
            sceneSelectorSerialized.FindProperty(nameof(sceneToggleButton)).objectReferenceValue = sceneToggleButton;
            sceneSelectorSerialized.FindProperty(nameof(sceneToggleLabel)).objectReferenceValue = sceneToggleLabel;
            sceneSelectorSerialized.ApplyModifiedPropertiesWithoutUndo();

            VoiceDialogueController voice = canvasObject.AddComponent<VoiceDialogueController>();
            SerializedObject voiceSerialized = new SerializedObject(voice);
            voiceSerialized.FindProperty("input").objectReferenceValue = dialogueInput;
            voiceSerialized.FindProperty("microphoneButton").objectReferenceValue = microphoneButton;
            voiceSerialized.FindProperty("status").objectReferenceValue = dialogueStatus;
            voiceSerialized.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        private static void BuildSimulationSystem(Transform parent, TrainingHud hud, StudentSet students, TeacherCameraController cameraController)
        {
            GameObject simulationObject = new GameObject("SimulationController");
            simulationObject.transform.SetParent(parent, false);
            GenerativeAiCoach aiCoach = simulationObject.AddComponent<GenerativeAiCoach>();
            simulationObject.AddComponent<DemoAutoplayController>();
            SimulationController controller = simulationObject.AddComponent<SimulationController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("hud").objectReferenceValue = hud;
            serialized.FindProperty("focalStudent").objectReferenceValue = students.focal;
            SetArray(serialized.FindProperty("classmates"), students.classmates);
            serialized.FindProperty("aiCoach").objectReferenceValue = aiCoach;
            serialized.FindProperty("teacherCamera").objectReferenceValue = cameraController;
            serialized.FindProperty("modeNavigator").objectReferenceValue = hud.GetComponent<TrainingModeNavigator>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateMaterials()
        {
            CreateMaterial("M_Wall", Color.white, 0f, 0.20f, "Assets/Art/Textures/ClassroomWall_Eggshell_HQ_v2.png", new Vector2(3.5f, 2.2f), false, false, "Assets/Art/Textures/ClassroomWall_Eggshell_HQ_v2_Normal.png");
            CreateMaterial("M_Ceiling", new Color(0.88f, 0.88f, 0.84f), 0f, 0.18f);
            CreateMaterial("M_Floor", Color.white, 0f, 0.27f, "Assets/Art/Textures/ClassroomFloor_Terrazzo_HQ_v2.png", new Vector2(5f, 3.5f), false, false, "Assets/Art/Textures/ClassroomFloor_Terrazzo_HQ_v2_Normal.png");
            CreateMaterial("M_Chalkboard", new Color(0.035f, 0.20f, 0.16f), 0f, 0.12f);
            CreateMaterial("M_Whiteboard", new Color(0.90f, 0.92f, 0.90f), 0.02f, 0.38f);
            CreateMaterial("M_ElectronicFrame", new Color(0.025f, 0.035f, 0.045f), 0.55f, 0.42f);
            CreateMaterial("M_ElectronicScreen", new Color(0.035f, 0.17f, 0.20f), 0f, 0.42f, null, Vector2.one, false, true);
            CreateMaterial("M_BulletinGreen", new Color(0.12f, 0.30f, 0.15f), 0f, 0.28f);
            CreateBulletinMaterials();
            CreateMaterial("M_TrimWood", new Color(0.32f, 0.17f, 0.08f), 0f, 0.28f);
            CreateMaterial("M_Metal", new Color(0.48f, 0.52f, 0.54f), 0.72f, 0.56f);
            CreateMaterial("M_DeskMetal", new Color(0.30f, 0.32f, 0.32f), 0.76f, 0.40f);
            CreateMaterial("M_DeskWood", Color.white, 0f, 0.40f, "Assets/Art/Textures/BirchDesk_Laminate_HQ_v2.png", new Vector2(1.8f, 1f), false, false, "Assets/Art/Textures/BirchDesk_Laminate_HQ_v2_Normal.png");
            CreateMaterial("M_ChairWood", new Color(0.58f, 0.40f, 0.24f), 0f, 0.36f, "Assets/Art/Textures/BirchDesk_Laminate_HQ_v2.png", Vector2.one, false, false, "Assets/Art/Textures/BirchDesk_Laminate_HQ_v2_Normal.png");
            CreateMaterial("M_ChairPlastic", new Color(0.44f, 0.62f, 0.18f), 0f, 0.46f);
            CreateMaterial("M_DeskEdge", new Color(0.17f, 0.12f, 0.08f), 0.05f, 0.24f);
            CreateMaterial("M_Rubber", new Color(0.05f, 0.12f, 0.15f), 0f, 0.30f);
            CreateMaterial("M_LockerPaint", new Color(0.57f, 0.47f, 0.32f), 0.08f, 0.22f);
            CreateMaterial("M_LockerYellow", new Color(0.91f, 0.70f, 0.16f), 0.03f, 0.31f);
            CreateMaterial("M_WorkBlue", new Color(0.62f, 0.82f, 0.90f), 0f, 0.25f);
            CreateMaterial("M_WorkMint", new Color(0.65f, 0.86f, 0.74f), 0f, 0.25f);
            CreateMaterial("M_FlagRed", new Color(0.78f, 0.11f, 0.15f), 0f, 0.32f);
            CreateMaterial("M_FlagBlue", new Color(0.05f, 0.22f, 0.62f), 0f, 0.32f);
            CreateMaterial("M_Baseboard", new Color(0.36f, 0.31f, 0.25f), 0.02f, 0.26f);
            CreateMaterial("M_StudentNavy", new Color(0.055f, 0.12f, 0.22f), 0f, 0.30f);
            CreateMaterial("M_StudentForest", new Color(0.10f, 0.28f, 0.19f), 0f, 0.28f);
            CreateMaterial("M_StudentCream", new Color(0.82f, 0.76f, 0.63f), 0f, 0.34f);
            CreateMaterial("M_StudentRust", new Color(0.60f, 0.20f, 0.10f), 0f, 0.27f);
            CreateMaterial("M_StudentCharcoal", new Color(0.09f, 0.11f, 0.13f), 0f, 0.25f);
            CreateMaterial("M_StudentSky", new Color(0.16f, 0.43f, 0.63f), 0f, 0.31f);
            CreateMaterial("M_StudentMustard", new Color(0.72f, 0.49f, 0.08f), 0f, 0.28f);
            CreateMaterial("M_StudentLavender", new Color(0.42f, 0.30f, 0.57f), 0f, 0.32f);
            CreateMaterial("M_HairNaturalBlack", new Color(0.012f, 0.009f, 0.008f), 0f, 0.24f);
            CreateMaterial("M_HairDarkBrown", new Color(0.055f, 0.025f, 0.014f), 0f, 0.27f);
            CreateMaterial("M_CeilingGrid", new Color(0.56f, 0.58f, 0.57f), 0.42f, 0.34f);
            CreateMaterial("M_PlantPot", new Color(0.34f, 0.18f, 0.10f), 0f, 0.28f);
            CreateMaterial("M_PlantLeaf", new Color(0.12f, 0.36f, 0.18f), 0f, 0.30f);
            CreateMaterial("M_Glass", new Color(0.70f, 0.86f, 0.92f, 0.06f), 0.05f, 0.92f, null, Vector2.one, true);
            CreateMaterial("M_ExteriorView", Color.white, 0f, 0.02f, "Assets/Art/Textures/WindowBackdrop_KoreanSchool.png", Vector2.one);
            CreateMaterial("M_ClockFace", new Color(0.95f, 0.94f, 0.89f), 0f, 0.25f);
            CreateMaterial("M_Paper", new Color(0.96f, 0.95f, 0.90f), 0f, 0.18f);
            CreateMaterial("M_PosterFrame", new Color(0.12f, 0.34f, 0.20f), 0f, 0.25f);
            CreateMaterial("M_BookTeal", new Color(0.07f, 0.31f, 0.35f), 0f, 0.38f);
            CreateMaterial("M_Tumbler", new Color(0.04f, 0.05f, 0.06f), 0.75f, 0.62f);
            CreateMaterial("M_LightFixture", new Color(0.72f, 0.74f, 0.73f), 0.55f, 0.44f);
            CreateMaterial("M_Fluorescent", new Color(0.95f, 0.98f, 1f), 0f, 0.35f, null, Vector2.one, false, true);
            CreateMaterial("M_BookPaper", WarmWhite, 0f, 0.2f);
            CreateMaterial("M_PlaceholderNavy", new Color(0.05f, 0.10f, 0.18f), 0f, 0.35f);
            CreateMaterial("M_PlaceholderBeige", new Color(0.66f, 0.58f, 0.48f), 0f, 0.32f);
            CreateMaterial("M_PlaceholderSkin", new Color(0.72f, 0.49f, 0.37f), 0f, 0.42f);
        }

        private static void CreateAnimatorController(bool female)
        {
            string gender = female ? "Female" : "Male";
            string prefix = female ? "f" : "m";
            string controllerPath = $"{ControllerRoot}/Rocketbox{gender}.controller";
            AssetDatabase.DeleteAsset(controllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorControllerLayer layer = controller.layers[0];
            layer.iKPass = true;
            controller.layers = new[] { layer };

            AnimationClip idle = Clip($"{prefix}_sit_table_idle_neutral_01.max.fbx");
            AnimationClip nervous = Clip(female ? "f_sit_table_idle_waiting_01.max.fbx" : "m_sit_table_idle_nervous_01.max.fbx");
            AnimationClip thoughtful = Clip($"{prefix}_sit_table_gestic_thoughtful.max.fbx");
            AnimationClip shrugStrong = Clip($"{prefix}_sit_table_gestic_shrug_02.max.fbx");
            AnimationClip shrugSoft = Clip($"{prefix}_sit_table_gestic_shrug_01.max.fbx");
            AnimationClip listening = Clip($"{prefix}_sit_table_idle_neutral_01.max.fbx");
            AnimationClip uprightListening = Clip($"{prefix}_gestic_listen_neutral_01.max.fbx");

            AnimatorState idleState = State(machine, "Idle", idle);
            State(machine, "Fidget", nervous);
            AnimatorState avoidState = State(machine, "AvoidGaze", thoughtful);
            AnimatorState withdrawState = State(machine, "Withdraw", thoughtful);
            AnimatorState protestState = State(machine, "Protest", shrugSoft);
            AnimatorState defiantState = State(machine, "Defiant", shrugStrong);
            AnimatorState deskTapState = State(machine, "DeskTap", nervous);
            AnimatorState shieldState = State(machine, "Shield", thoughtful);
            AnimatorState pointState = State(machine, "Point", shrugSoft);
            AnimatorState pushAwayState = State(machine, "PushAway", shrugStrong);
            State(machine, "Listen", listening);
            State(machine, "UprightListen", uprightListening);
            State(machine, "Recover", idle);
            machine.defaultState = idleState;
            AddReturnToIdle(avoidState, idleState);
            AddReturnToIdle(withdrawState, idleState);
            AddReturnToIdle(protestState, idleState);
            AddReturnToIdle(defiantState, idleState);
            AddReturnToIdle(deskTapState, idleState);
            AddReturnToIdle(shieldState, idleState);
            AddReturnToIdle(pointState, idleState);
            AddReturnToIdle(pushAwayState, idleState);
            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState State(AnimatorStateMachine machine, string name, AnimationClip clip)
        {
            AnimatorState state = machine.AddState(name);
            state.motion = clip;
            state.speed = 1f;
            if (clip == null)
            {
                Debug.LogWarning($"Rocketbox animation missing for state {name}.");
            }
            return state;
        }

        private static void AddReturnToIdle(AnimatorState source, AnimatorState idle)
        {
            AnimatorStateTransition transition = source.AddTransition(idle);
            transition.hasExitTime = true;
            transition.exitTime = 0.92f;
            transition.duration = 0.22f;
            transition.hasFixedDuration = true;
        }

        private static void RenderCamera(Camera camera, string outputPath)
        {
            const int width = 1600;
            const int height = 900;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }

        private static AnimationClip Clip(string fileName)
        {
            string path = $"{RocketboxRoot}/Animations/{fileName}";
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase));
        }

        private static void EnsureRoundedUiSprite()
        {
            const int size = 128;
            const float radius = 30f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 half = new Vector2(size * 0.5f - radius, size * 0.5f - radius);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f) - center;
                    Vector2 corner = new Vector2(Mathf.Max(Mathf.Abs(point.x) - half.x, 0f), Mathf.Max(Mathf.Abs(point.y) - half.y, 0f));
                    float edge = Mathf.InverseLerp(radius - 1.5f, radius + 1.5f, corner.magnitude);
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, edge);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(RoundedUiSpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(RoundedUiSpritePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(RoundedUiSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(32f, 32f, 32f, 32f);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void EnsureSpeechTailSprite()
        {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float vertical = (y + 0.5f) / size;
                float halfWidth = Mathf.Lerp(0.055f, 0.48f, Mathf.SmoothStep(0f, 1f, vertical));
                for (int x = 0; x < size; x++)
                {
                    float horizontal = Mathf.Abs((x + 0.5f) / size - 0.5f);
                    float edge = Mathf.InverseLerp(halfWidth - 0.018f, halfWidth + 0.018f, horizontal);
                    float alpha = 1f - Mathf.SmoothStep(0f, 1f, edge);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(SpeechTailSpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpeechTailSpritePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpeechTailSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void EnsureGeneratedTextureSettings()
        {
            string[] faceTextureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Generated/StudentFaces" });
            foreach (string guid in faceTextureGuids)
            {
                string facePath = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter faceImporter = AssetImporter.GetAtPath(facePath) as TextureImporter;
                if (faceImporter == null)
                {
                    continue;
                }

                faceImporter.textureType = TextureImporterType.Default;
                faceImporter.sRGBTexture = true;
                faceImporter.wrapMode = TextureWrapMode.Clamp;
                faceImporter.npotScale = TextureImporterNPOTScale.None;
                faceImporter.maxTextureSize = 2048;
                faceImporter.mipmapEnabled = true;
                faceImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                faceImporter.anisoLevel = 4;
                faceImporter.SaveAndReimport();
            }

            string[] clothingTextureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Generated/StudentClothing" });
            foreach (string guid in clothingTextureGuids)
            {
                string clothingPath = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter clothingImporter = AssetImporter.GetAtPath(clothingPath) as TextureImporter;
                if (clothingImporter == null)
                {
                    continue;
                }

                clothingImporter.textureType = TextureImporterType.Default;
                clothingImporter.sRGBTexture = true;
                clothingImporter.npotScale = TextureImporterNPOTScale.None;
                bool isGraphicAtlas = clothingPath.EndsWith("GraphicAtlas_15.png", StringComparison.OrdinalIgnoreCase);
                clothingImporter.wrapMode = isGraphicAtlas ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                clothingImporter.maxTextureSize = isGraphicAtlas ? 2048 : 1024;
                clothingImporter.mipmapEnabled = !isGraphicAtlas;
                clothingImporter.textureCompression = isGraphicAtlas
                    ? TextureImporterCompression.Uncompressed
                    : TextureImporterCompression.CompressedHQ;
                clothingImporter.anisoLevel = 4;
                clothingImporter.SaveAndReimport();
            }

            foreach (string path in new[]
                     {
                         "Assets/Art/Textures/BirchDesk_Albedo.png",
                         "Assets/Art/Textures/ClassroomFloor_Albedo.png",
                         "Assets/Art/Textures/WindowBackdrop_KoreanSchool.png",
                         "Assets/Art/Textures/BirchDesk_Laminate_HQ_v2.png",
                         "Assets/Art/Textures/ClassroomFloor_Terrazzo_HQ_v2.png",
                         "Assets/Art/Textures/ClassroomWall_Eggshell_HQ_v2.png",
                         "Assets/Art/Textures/BirchDesk_Laminate_HQ_v2_Normal.png",
                         "Assets/Art/Textures/ClassroomFloor_Terrazzo_HQ_v2_Normal.png",
                         "Assets/Art/Textures/ClassroomWall_Eggshell_HQ_v2_Normal.png"
                     })
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool isExteriorBackdrop = path.EndsWith("WindowBackdrop_KoreanSchool.png", StringComparison.OrdinalIgnoreCase);
                importer.wrapMode = isExteriorBackdrop ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.maxTextureSize = 2048;
                importer.textureCompression = isExteriorBackdrop
                    ? TextureImporterCompression.Uncompressed
                    : TextureImporterCompression.CompressedHQ;
                importer.mipmapEnabled = !isExteriorBackdrop;
                importer.npotScale = isExteriorBackdrop ? TextureImporterNPOTScale.None : TextureImporterNPOTScale.ToNearest;
                importer.anisoLevel = isExteriorBackdrop ? 4 : 1;
                importer.ignoreMipmapLimit = isExteriorBackdrop;
                if (path.EndsWith("_Normal.png", StringComparison.OrdinalIgnoreCase))
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.convertToNormalmap = true;
                    importer.heightmapScale = 0.055f;
                }
                importer.SaveAndReimport();
            }
        }

        private static Material CreateMaterial(
            string name,
            Color color,
            float metallic,
            float smoothness,
            string texturePath = null,
            Vector2? tiling = null,
            bool transparent = false,
            bool emissive = false,
            string normalPath = null)
        {
            string path = $"{MaterialRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find(name == "M_ExteriorView" ? "Unlit/Texture" : "Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (name == "M_ExteriorView")
            {
                material.shader = Shader.Find("Unlit/Texture");
            }

            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            if (!string.IsNullOrEmpty(texturePath))
            {
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                material.mainTextureScale = tiling ?? Vector2.one;
            }

            if (!string.IsNullOrEmpty(normalPath))
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                material.SetTextureScale("_BumpMap", tiling ?? Vector2.one);
                material.SetTextureOffset("_BumpMap", material.mainTextureOffset);
                material.SetFloat("_BumpScale", 0.38f);
                material.EnableKeyword("_NORMALMAP");
            }

            if (transparent)
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.SetOverrideTag("RenderType", "Transparent");
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.45f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialRoot}/{name}.mat");

        private static GameObject Root(string name) => new GameObject(name);

        private static GameObject ChildRoot(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static GameObject RootObject(string name, Transform parent, Vector3 localPosition)
        {
            GameObject result = new GameObject(name);
            result.transform.SetParent(parent, false);
            result.transform.localPosition = localPosition;
            return result;
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            return result;
        }

        private static GameObject Quad(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Quad);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localRotation = rotation;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(result.GetComponent<Collider>());
            return result;
        }

        private static GameObject RoundedBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 size,
            float bevel,
            string meshKey,
            Material material)
        {
            string meshPath = $"{MeshRoot}/{meshKey}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            Mesh generated = CreateRoundedBoxMesh(size, bevel);
            if (mesh == null)
            {
                mesh = generated;
                mesh.name = meshKey;
                AssetDatabase.CreateAsset(mesh, meshPath);
            }
            else
            {
                generated.name = meshKey;
                EditorUtility.CopySerialized(generated, mesh);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(mesh);
            }

            GameObject result = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.GetComponent<MeshFilter>().sharedMesh = mesh;
            result.GetComponent<MeshRenderer>().sharedMaterial = material;
            result.GetComponent<BoxCollider>().size = size;
            return result;
        }

        private static Mesh CreateRoundedBoxMesh(Vector3 size, float requestedBevel)
        {
            Vector3 half = size * 0.5f;
            float bevel = Mathf.Min(requestedBevel, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.92f);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            const int roundSegments = 5;

            AddRoundedFace(vertices, triangles, normals, uvs, half, bevel, 0, 1, 2, 1, roundSegments);
            AddRoundedFace(vertices, triangles, normals, uvs, half, bevel, 0, 2, 1, -1, roundSegments);
            AddRoundedFace(vertices, triangles, normals, uvs, half, bevel, 1, 2, 0, 1, roundSegments);
            AddRoundedFace(vertices, triangles, normals, uvs, half, bevel, 1, 0, 2, -1, roundSegments);
            AddRoundedFace(vertices, triangles, normals, uvs, half, bevel, 2, 0, 1, 1, roundSegments);
            AddRoundedFace(vertices, triangles, normals, uvs, half, bevel, 2, 1, 0, -1, roundSegments);

            Mesh mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                normals = normals.ToArray(),
                uv = uvs.ToArray()
            };
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddRoundedFace(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector3> normals,
            List<Vector2> uvs,
            Vector3 half,
            float bevel,
            int normalAxis,
            int uAxis,
            int vAxis,
            int sign,
            int segments)
        {
            float[] uCoordinates = RoundedAxisCoordinates(half[uAxis], bevel, segments);
            float[] vCoordinates = RoundedAxisCoordinates(half[vAxis], bevel, segments);
            Vector3 inner = half - Vector3.one * bevel;
            int[,] indices = new int[uCoordinates.Length, vCoordinates.Length];

            for (int v = 0; v < vCoordinates.Length; v++)
            for (int u = 0; u < uCoordinates.Length; u++)
            {
                Vector3 source = Vector3.zero;
                source[normalAxis] = sign * half[normalAxis];
                source[uAxis] = uCoordinates[u];
                source[vAxis] = vCoordinates[v];
                Vector3 core = new Vector3(
                    Mathf.Clamp(source.x, -inner.x, inner.x),
                    Mathf.Clamp(source.y, -inner.y, inner.y),
                    Mathf.Clamp(source.z, -inner.z, inner.z));
                Vector3 radial = source - core;
                Vector3 normal = radial.sqrMagnitude > 0.000001f
                    ? radial.normalized
                    : (normalAxis == 0 ? Vector3.right : normalAxis == 1 ? Vector3.up : Vector3.forward) * sign;

                indices[u, v] = vertices.Count;
                vertices.Add(core + normal * bevel);
                normals.Add(normal);
                uvs.Add(new Vector2(
                    uCoordinates[u] / (half[uAxis] * 2f) + 0.5f,
                    vCoordinates[v] / (half[vAxis] * 2f) + 0.5f));
            }

            for (int v = 0; v < vCoordinates.Length - 1; v++)
            for (int u = 0; u < uCoordinates.Length - 1; u++)
            {
                int a = indices[u, v];
                int b = indices[u + 1, v];
                int c = indices[u + 1, v + 1];
                int d = indices[u, v + 1];
                Vector3 center = (vertices[a] + vertices[b] + vertices[c] + vertices[d]) * 0.25f;
                if (Vector3.Dot(Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]), center) < 0f)
                    triangles.AddRange(new[] { a, d, c, a, c, b });
                else
                    triangles.AddRange(new[] { a, b, c, a, c, d });
            }
        }

        private static float[] RoundedAxisCoordinates(float half, float bevel, int segments)
        {
            float inner = half - bevel;
            var values = new List<float> { -half };
            for (int i = 1; i < segments; i++)
                values.Add(Mathf.Lerp(-half, -inner, i / (float)segments));
            values.Add(-inner);
            values.Add(inner);
            for (int i = 1; i < segments; i++)
                values.Add(Mathf.Lerp(inner, half, i / (float)segments));
            values.Add(half);
            return values.ToArray();
        }

        private static Mesh CreateBeveledBoxMesh(Vector3 size, float requestedBevel)
        {
            Vector3 half = size * 0.5f;
            float bevel = Mathf.Min(requestedBevel, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.92f);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            AddMeshQuad(vertices, triangles, new Vector3(half.x, -half.y + bevel, -half.z + bevel), new Vector3(half.x, half.y - bevel, -half.z + bevel), new Vector3(half.x, half.y - bevel, half.z - bevel), new Vector3(half.x, -half.y + bevel, half.z - bevel));
            AddMeshQuad(vertices, triangles, new Vector3(-half.x, -half.y + bevel, half.z - bevel), new Vector3(-half.x, half.y - bevel, half.z - bevel), new Vector3(-half.x, half.y - bevel, -half.z + bevel), new Vector3(-half.x, -half.y + bevel, -half.z + bevel));
            AddMeshQuad(vertices, triangles, new Vector3(-half.x + bevel, half.y, -half.z + bevel), new Vector3(-half.x + bevel, half.y, half.z - bevel), new Vector3(half.x - bevel, half.y, half.z - bevel), new Vector3(half.x - bevel, half.y, -half.z + bevel));
            AddMeshQuad(vertices, triangles, new Vector3(-half.x + bevel, -half.y, half.z - bevel), new Vector3(-half.x + bevel, -half.y, -half.z + bevel), new Vector3(half.x - bevel, -half.y, -half.z + bevel), new Vector3(half.x - bevel, -half.y, half.z - bevel));
            AddMeshQuad(vertices, triangles, new Vector3(-half.x + bevel, -half.y + bevel, half.z), new Vector3(half.x - bevel, -half.y + bevel, half.z), new Vector3(half.x - bevel, half.y - bevel, half.z), new Vector3(-half.x + bevel, half.y - bevel, half.z));
            AddMeshQuad(vertices, triangles, new Vector3(half.x - bevel, -half.y + bevel, -half.z), new Vector3(-half.x + bevel, -half.y + bevel, -half.z), new Vector3(-half.x + bevel, half.y - bevel, -half.z), new Vector3(half.x - bevel, half.y - bevel, -half.z));

            foreach (int sy in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
            {
                AddMeshQuad(vertices, triangles,
                    new Vector3(-half.x + bevel, sy * half.y, sz * (half.z - bevel)),
                    new Vector3(half.x - bevel, sy * half.y, sz * (half.z - bevel)),
                    new Vector3(half.x - bevel, sy * (half.y - bevel), sz * half.z),
                    new Vector3(-half.x + bevel, sy * (half.y - bevel), sz * half.z));
            }

            foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
            {
                AddMeshQuad(vertices, triangles,
                    new Vector3(sx * half.x, -half.y + bevel, sz * (half.z - bevel)),
                    new Vector3(sx * half.x, half.y - bevel, sz * (half.z - bevel)),
                    new Vector3(sx * (half.x - bevel), half.y - bevel, sz * half.z),
                    new Vector3(sx * (half.x - bevel), -half.y + bevel, sz * half.z));
            }

            foreach (int sx in new[] { -1, 1 })
            foreach (int sy in new[] { -1, 1 })
            {
                AddMeshQuad(vertices, triangles,
                    new Vector3(sx * half.x, sy * (half.y - bevel), -half.z + bevel),
                    new Vector3(sx * half.x, sy * (half.y - bevel), half.z - bevel),
                    new Vector3(sx * (half.x - bevel), sy * half.y, half.z - bevel),
                    new Vector3(sx * (half.x - bevel), sy * half.y, -half.z + bevel));
            }

            foreach (int sx in new[] { -1, 1 })
            foreach (int sy in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
            {
                AddMeshTriangle(vertices, triangles,
                    new Vector3(sx * half.x, sy * (half.y - bevel), sz * (half.z - bevel)),
                    new Vector3(sx * (half.x - bevel), sy * half.y, sz * (half.z - bevel)),
                    new Vector3(sx * (half.x - bevel), sy * (half.y - bevel), sz * half.z));
            }

            Mesh mesh = new Mesh { vertices = vertices.ToArray(), triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            var uvs = new Vector2[vertices.Count];
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 normal = normals[i];
                Vector3 vertex = vertices[i];
                if (Mathf.Abs(normal.y) >= Mathf.Abs(normal.x) && Mathf.Abs(normal.y) >= Mathf.Abs(normal.z))
                    uvs[i] = new Vector2(vertex.x / size.x + 0.5f, vertex.z / size.z + 0.5f);
                else if (Mathf.Abs(normal.x) >= Mathf.Abs(normal.z))
                    uvs[i] = new Vector2(vertex.z / size.z + 0.5f, vertex.y / size.y + 0.5f);
                else
                    uvs[i] = new Vector2(vertex.x / size.x + 0.5f, vertex.y / size.y + 0.5f);
            }
            mesh.uv = uvs;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddMeshQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 center = (a + b + c + d) * 0.25f;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), center) < 0f)
                (b, d) = (d, b);
            int start = vertices.Count;
            vertices.AddRange(new[] { a, b, c, d });
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        private static void AddMeshTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 center = (a + b + c) / 3f;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), center) < 0f)
                (b, c) = (c, b);
            int start = vertices.Count;
            vertices.AddRange(new[] { a, b, c });
            triangles.AddRange(new[] { start, start + 1, start + 2 });
        }

        private static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            return result;
        }

        private static GameObject Capsule(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            return result;
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            GameObject result = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = position;
            result.transform.localRotation = rotation;
            result.transform.localScale = scale;
            result.GetComponent<Renderer>().sharedMaterial = material;
            return result;
        }

        private static void CreateWorldText(string name, Transform parent, string value, Vector3 position, Quaternion rotation, float characterSize, Color color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            textObject.transform.localRotation = rotation;
            TextMesh text = textObject.AddComponent<TextMesh>();
            text.text = value;
            text.font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 64);
            text.fontSize = 64;
            text.characterSize = characterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
        }

        private static RectTransform Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            result.transform.SetParent(parent, false);
            RectTransform rect = result.GetComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = result.GetComponent<Image>();
            image.color = color;
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedUiSpritePath);
            image.type = Image.Type.Sliced;
            return rect;
        }

        private static TMP_Text CreateChip(
            Transform parent,
            string name,
            string value,
            TMP_FontAsset font,
            Color surface,
            Color textColor,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            RectTransform chip = Panel(parent, name, surface, new Vector2(0f, 1f), new Vector2(0f, 1f), offsetMin, offsetMax);
            TMP_Text label = Label(chip, "Label", value, font, 14, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));
            return label;
        }

        private static void AddSoftShadow(RectTransform rect, float alpha = 0.20f, float distance = 5f)
        {
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, alpha);
            shadow.effectDistance = new Vector2(0f, -distance);
            shadow.useGraphicAlpha = true;
        }

        private static TMP_Text Label(Transform parent, string name, string value, TMP_FontAsset font, int size, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            GameObject result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            result.transform.SetParent(parent, false);
            TextMeshProUGUI text = result.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.richText = true;
            text.extraPadding = true;
            return text;
        }

        private static TMP_FontAsset GetOrCreateKoreanFontAsset()
        {
            EnsureTmpEssentialResources();
            EnsureTmpSettings();
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            if (existing != null)
            {
                return existing;
            }

            Font source = AssetDatabase.LoadAssetAtPath<Font>(KoreanFontSourcePath);
            if (source == null)
            {
                throw new InvalidOperationException($"Bundled Korean font is missing at {KoreanFontSourcePath}.");
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
            fontAsset.name = "NotoSansKR-SDF";
            fontAsset.isMultiAtlasTexturesEnabled = true;
            AssetDatabase.CreateAsset(fontAsset, KoreanFontAssetPath);

            foreach (Texture2D atlas in fontAsset.atlasTextures)
            {
                if (atlas != null)
                {
                    atlas.name = $"{fontAsset.name} Atlas";
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{fontAsset.name} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static void EnsureTmpEssentialResources()
        {
            if (Shader.Find("TextMeshPro/Distance Field") != null)
            {
                return;
            }

            string package = Directory
                .GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(package))
            {
                throw new FileNotFoundException("TMP Essential Resources package was not found.");
            }

            AssetDatabase.ImportPackage(Path.GetFullPath(package), false);
            AssetDatabase.Refresh();
            if (Shader.Find("TextMeshPro/Distance Field") == null)
            {
                throw new InvalidOperationException("TMP distance-field shader could not be imported.");
            }
        }

        private static void EnsureTmpSettings()
        {
            if (TMP_Settings.LoadDefaultSettings() != null)
            {
                return;
            }

            Directory.CreateDirectory("Assets/Resources");
            TMP_Settings settings = ScriptableObject.CreateInstance<TMP_Settings>();
            settings.name = "TMP Settings";
            AssetDatabase.CreateAsset(settings, TmpSettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (TMP_Settings.LoadDefaultSettings() == null)
            {
                throw new InvalidOperationException("Unable to initialize TMP Settings for SDF text rendering.");
            }
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetArray<T>(SerializedProperty property, IReadOnlyList<T> values) where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
