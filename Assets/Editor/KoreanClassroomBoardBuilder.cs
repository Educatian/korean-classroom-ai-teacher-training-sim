using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdieLab.TeacherTraining.Editor
{
    public static partial class KoreanClassroomBuilder
    {
        private static void BuildBoardDetails(Transform parent)
        {
            Material paper = Mat("M_Paper");
            BuildElectronicBoard(parent);
            BuildFrontCabinets(parent);
            BuildKoreanFlag(parent, paper);

            Cube("ClassSchedule", parent, new Vector3(-4.32f, 2.12f, 4.68f), new Vector3(0.62f, 1.12f, 0.025f), paper);
            CreateWorldText("ScheduleText", parent, string.Empty, new Vector3(-4.32f, 2.13f, 4.63f), Quaternion.Euler(0f, 180f, 0f), 0.040f, new Color(0.12f, 0.16f, 0.18f));
            RoundedBox("WallSpeaker", parent, new Vector3(2.72f, 2.78f, 4.70f), new Vector3(0.42f, 0.48f, 0.20f), 0.035f, "FrontWallSpeaker", Mat("M_Metal"));
        }

        private static void BuildElectronicBoard(Transform parent)
        {
            if (TryBuildGeneratedElectronicBoard(parent))
            {
                return;
            }

            GameObject board = RootObject("ElectronicBoardAssembly", parent, Vector3.zero);
            RoundedBox("Housing", board.transform, new Vector3(-0.75f, 2.08f, 4.60f), new Vector3(4.58f, 1.80f, 0.18f), 0.055f, "ElectronicBoardHousing", Mat("M_ElectronicFrame"));
            RoundedBox("Display", board.transform, new Vector3(-0.75f, 2.08f, 4.49f), new Vector3(4.28f, 1.50f, 0.035f), 0.025f, "ElectronicBoardDisplay", Mat("M_ElectronicScreen"));
            Cube("GlassReflection", board.transform, new Vector3(-1.58f, 2.49f, 4.465f), new Vector3(1.65f, 0.025f, 0.008f), Mat("M_Glass"));
            Cube("Header", board.transform, new Vector3(-0.75f, 2.55f, 4.45f), new Vector3(2.55f, 0.10f, 0.018f), Mat("M_WorkMint"));
            Cube("CardA", board.transform, new Vector3(-1.90f, 2.02f, 4.45f), new Vector3(0.92f, 0.66f, 0.018f), Mat("M_WorkBlue"));
            Cube("CardB", board.transform, new Vector3(-0.75f, 2.02f, 4.45f), new Vector3(0.92f, 0.66f, 0.018f), Mat("M_WorkMint"));
            Cube("CardC", board.transform, new Vector3(0.40f, 2.02f, 4.45f), new Vector3(0.92f, 0.66f, 0.018f), Mat("M_Paper"));
            Cube("Footer", board.transform, new Vector3(-0.75f, 1.52f, 4.45f), new Vector3(3.15f, 0.08f, 0.018f), Mat("M_LockerYellow"));

            Cube("BottomTray", board.transform, new Vector3(-0.75f, 1.13f, 4.49f), new Vector3(3.30f, 0.07f, 0.26f), Mat("M_ElectronicFrame"));
            Cylinder("TopCamera", board.transform, new Vector3(-0.75f, 2.94f, 4.46f), new Vector3(0.055f, 0.025f, 0.055f), Quaternion.Euler(90f, 0f, 0f), Mat("M_Rubber"));
            Sphere("PowerLed", board.transform, new Vector3(1.38f, 1.32f, 4.43f), new Vector3(0.025f, 0.025f, 0.025f), Mat("M_WorkMint"));

            for (int index = 0; index < 7; index++)
            {
                float y = 1.66f + index * 0.16f;
                Cube($"SpeakerSlotL_{index:00}", board.transform, new Vector3(-2.94f, y, 4.43f), new Vector3(0.045f, 0.075f, 0.012f), Mat("M_Metal"));
                Cube($"SpeakerSlotR_{index:00}", board.transform, new Vector3(1.44f, y, 4.43f), new Vector3(0.045f, 0.075f, 0.012f), Mat("M_Metal"));
            }
        }

        private static bool TryBuildGeneratedElectronicBoard(Transform parent)
        {
            const string modelPath = "Assets/Models/Generated/SM_ElectronicBoard_Realistic.obj";
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                return false;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = "ElectronicBoardAssembly_Blender";
            instance.transform.localPosition = new Vector3(-0.75f, 0.45f, 4.55f);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            BuildGeneratedElectronicBoardHardware(instance.transform);

            Cube("Header", instance.transform, new Vector3(0f, 2.06f, -0.18f), new Vector3(2.55f, 0.10f, 0.018f), Mat("M_WorkMint"));
            Cube("CardA", instance.transform, new Vector3(-1.15f, 1.58f, -0.18f), new Vector3(0.92f, 0.62f, 0.018f), Mat("M_WorkBlue"));
            Cube("CardB", instance.transform, new Vector3(0f, 1.58f, -0.18f), new Vector3(0.92f, 0.62f, 0.018f), Mat("M_WorkMint"));
            Cube("CardC", instance.transform, new Vector3(1.15f, 1.58f, -0.18f), new Vector3(0.92f, 0.62f, 0.018f), Mat("M_Paper"));
            Cube("Footer", instance.transform, new Vector3(0f, 1.10f, -0.18f), new Vector3(3.15f, 0.08f, 0.018f), Mat("M_LockerYellow"));
            return true;
        }

        private static void BuildGeneratedElectronicBoardHardware(Transform board)
        {
            Material frame = Mat("M_ElectronicFrame");
            Material screen = Mat("M_ElectronicScreen");
            Material metal = Mat("M_Metal");
            Material rubber = Mat("M_Rubber");
            Material glass = Mat("M_Glass");
            Material indicator = Mat("M_WorkMint");

            Cube("ActiveDisplaySurface", board, new Vector3(0f, 1.65f, -0.145f), new Vector3(4.28f, 1.24f, 0.025f), screen);
            Cube("BezelTop", board, new Vector3(0f, 2.30f, -0.19f), new Vector3(4.56f, 0.085f, 0.075f), frame);
            Cube("BezelBottom", board, new Vector3(0f, 0.99f, -0.19f), new Vector3(4.56f, 0.085f, 0.075f), frame);
            Cube("BezelLeft", board, new Vector3(-2.26f, 1.65f, -0.19f), new Vector3(0.085f, 1.38f, 0.075f), frame);
            Cube("BezelRight", board, new Vector3(2.26f, 1.65f, -0.19f), new Vector3(0.085f, 1.38f, 0.075f), frame);
            Cube("BrushedMetalTopTrim", board, new Vector3(0f, 2.335f, -0.235f), new Vector3(4.42f, 0.018f, 0.018f), metal);
            Cube("BrushedMetalBottomTrim", board, new Vector3(0f, 1.025f, -0.235f), new Vector3(4.42f, 0.018f, 0.018f), metal);

            RoundedBox("CameraHousing", board, new Vector3(0f, 2.46f, -0.235f), new Vector3(0.34f, 0.16f, 0.09f), 0.035f, "ElectronicBoardCameraHousingV2", frame);
            Cylinder("CameraLensRing", board, new Vector3(0f, 2.46f, -0.295f), new Vector3(0.048f, 0.018f, 0.048f), Quaternion.Euler(90f, 0f, 0f), metal);
            Cylinder("CameraLens", board, new Vector3(0f, 2.46f, -0.318f), new Vector3(0.029f, 0.012f, 0.029f), Quaternion.Euler(90f, 0f, 0f), glass);
            Sphere("CameraStatusLed", board, new Vector3(0.105f, 2.455f, -0.291f), new Vector3(0.018f, 0.018f, 0.012f), indicator);

            Cube("AccessoryTray", board, new Vector3(0f, 0.82f, -0.18f), new Vector3(3.78f, 0.075f, 0.34f), frame);
            Cube("AccessoryTrayLip", board, new Vector3(0f, 0.84f, -0.355f), new Vector3(3.78f, 0.055f, 0.035f), metal);
            Cylinder("TrayStylus", board, new Vector3(-1.15f, 0.89f, -0.36f), new Vector3(0.018f, 0.26f, 0.018f), Quaternion.Euler(0f, 0f, 90f), Mat("M_Paper"));

            RoundedBox("InputPanel", board, new Vector3(1.60f, 0.90f, -0.275f), new Vector3(0.72f, 0.20f, 0.055f), 0.018f, "ElectronicBoardInputPanelV2", frame);
            Material[] portMaterials = { metal, Mat("M_WorkBlue"), metal, indicator };
            string[] portNames = { "UsbCPort", "UsbAPort", "HdmiPort", "AudioPort" };
            for (int index = 0; index < portNames.Length; index++)
            {
                float x = 1.37f + index * 0.15f;
                Cube(portNames[index], board, new Vector3(x, 0.90f, -0.31f), new Vector3(0.085f, index == 3 ? 0.045f : 0.055f, 0.018f), portMaterials[index]);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 2.17f;
                Cube(side < 0 ? "LeftSpeakerGrille" : "RightSpeakerGrille", board, new Vector3(x, 1.64f, -0.235f), new Vector3(0.13f, 1.02f, 0.045f), metal);
                for (int index = 0; index < 8; index++)
                {
                    float y = 1.25f + index * 0.11f;
                    Sphere($"{(side < 0 ? "Left" : "Right")}SpeakerPerforation_{index:00}", board, new Vector3(x, y, -0.27f), new Vector3(0.027f, 0.027f, 0.012f), rubber);
                }
            }
        }
        private static void BuildGeneratedClassroomProps(Transform parent)
        {
            InstantiateGeneratedModel("SM_AirPurifier_Realistic.obj", "AirPurifier_Blender", parent, new Vector3(5.75f, 0f, 4.35f), Quaternion.identity);
            InstantiateGeneratedModel("SM_TeacherPodium_Realistic.obj", "TeacherPodium_Blender", parent, new Vector3(-3.25f, 0f, 3.85f), Quaternion.identity);
            InstantiateGeneratedModel("SM_DeskProps_Realistic.obj", "TeacherDeskProps_Blender", parent, new Vector3(-4.90f, 0.91f, 3.18f), Quaternion.identity);
            InstantiateGeneratedModel("SM_SchoolBackpack_Realistic.obj", "SchoolBackpack_Blender", parent, new Vector3(4.55f, 0f, -4.28f), Quaternion.Euler(0f, -18f, 0f));
        }

        private static void InstantiateGeneratedModel(string fileName, string objectName, Transform parent, Vector3 position, Quaternion rotation)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Models/Generated/{fileName}");
            if (model == null)
            {
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = objectName;
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = Vector3.one;
        }
        [MenuItem("Tools/Teacher Training/Replace Electronic Board In Training Scenes")]
        public static void ReplaceElectronicBoardInTrainingScenes()
        {
            ValidateGeneratedElectronicBoardSource();
            foreach (string scenePath in GetTrainingScenePaths())
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                GameObject legacy = FindSceneObject(scene, "ElectronicBoardAssembly");
                GameObject generated = FindSceneObject(scene, "ElectronicBoardAssembly_Blender");
                Transform parent = legacy != null
                    ? legacy.transform.parent
                    : generated != null
                        ? generated.transform.parent
                        : null;
                if (parent == null)
                {
                    throw new InvalidOperationException($"Electronic board parent is missing in {scenePath}.");
                }

                if (legacy != null)
                {
                    UnityEngine.Object.DestroyImmediate(legacy);
                }
                if (generated != null)
                {
                    UnityEngine.Object.DestroyImmediate(generated);
                }
                if (!TryBuildGeneratedElectronicBoard(parent))
                {
                    throw new InvalidOperationException($"Generated electronic board asset is missing in {scenePath}.");
                }

                if (scene.name == "KoreanClassroomCircleTraining")
                {
                    GameObject teacherCamera = FindSceneObject(scene, "TeacherCamera");
                    if (teacherCamera != null)
                    {
                        Vector3 eyePosition = new Vector3(-5.40f, 1.62f, 4.15f);
                        teacherCamera.transform.SetPositionAndRotation(
                            eyePosition,
                            Quaternion.LookRotation(new Vector3(0f, 1.08f, 0f) - eyePosition));
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"ELECTRONIC_BOARD_REPLACED scene={scene.name}");
            }

            AssetDatabase.SaveAssets();
        }

        private static void ValidateGeneratedElectronicBoardSource()
        {
            const string modelPath = "Assets/Models/Generated/SM_ElectronicBoard_Realistic.obj";
            string source = File.ReadAllText(modelPath);
            string[] requiredParts =
            {
                "Frame", "InnerBezel", "Screen", "Tray", "CameraRing", "CameraLens",
                "InputPanel", "Port0", "Port1", "Port2", "Port3", "Speaker_-1_0_0", "Speaker_1_0_0"
            };
            foreach (string part in requiredParts)
            {
                if (!source.Contains($"o {part}\n") && !source.Contains($"o {part}\r\n"))
                {
                    throw new InvalidDataException($"Generated electronic board is missing authored part '{part}'.");
                }
            }
        }

        public static void ReplaceElectronicBoardInTrainingScenesFromCommandLine()
        {
            try
            {
                ReplaceElectronicBoardInTrainingScenes();
                Debug.Log("ELECTRONIC_BOARD_REPLACEMENT_OK");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name == objectName)
                    {
                        return candidate.gameObject;
                    }
                }
            }
            return null;
        }
        private static void BuildFrontCabinets(Transform parent)
        {
            for (int index = 0; index < 8; index++)
            {
                float x = -4.55f + index * 0.95f;
                Material finish = Mat(index % 3 == 1 ? "M_LockerYellow" : "M_LockerPaint");
                RoundedBox($"FrontCabinet_{index:00}", parent, new Vector3(x, 0.66f, 4.66f), new Vector3(0.90f, 0.96f, 0.58f), 0.025f, "FrontCabinetBody", finish);
                Cube($"FrontCabinetReveal_{index:00}", parent, new Vector3(x, 0.66f, 4.355f), new Vector3(0.78f, 0.82f, 0.018f), finish);
                RoundedBox($"FrontCabinetHandle_{index:00}", parent, new Vector3(x + 0.28f, 0.77f, 4.32f), new Vector3(0.08f, 0.18f, 0.035f), 0.012f, "FrontCabinetHandle", Mat("M_Metal"));
            }
        }

        private static void BuildKoreanFlag(Transform parent, Material paper)
        {
            Cube("KoreanFlagBacking", parent, new Vector3(-0.75f, 3.18f, 4.65f), new Vector3(0.72f, 0.40f, 0.025f), paper);
            Cylinder("KoreanFlagRed", parent, new Vector3(-0.86f, 3.18f, 4.61f), new Vector3(0.11f, 0.018f, 0.11f), Quaternion.Euler(90f, 0f, 0f), Mat("M_FlagRed"));
            Cylinder("KoreanFlagBlue", parent, new Vector3(-0.64f, 3.18f, 4.61f), new Vector3(0.11f, 0.018f, 0.11f), Quaternion.Euler(90f, 0f, 0f), Mat("M_FlagBlue"));
        }
    }
}
