using System;
using System.Collections.Generic;
using System.Linq;
using AdieLab.TeacherTraining;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdieLab.TeacherTraining.Editor
{
    // Scene 3: 안정 공간(마음쉼터) — a child-friendly 1:1 recovery-conversation
    // nook carved out of the west neighbor classroom's corridor-adjacent corner
    // (x -13.6..-8.4, z 0..5). Safeguarding by design: the sliding door stands
    // half-open (>=0.5 m gap) and a corridor-facing window band (sill 1.1,
    // top 2.3) keeps the corridor visible from inside — never a sealed box.
    public static partial class KoreanClassroomBuilder
    {
        private const string RecoveryScenePath = "Assets/Scenes/KoreanClassroomRecoveryTraining.unity";

        // Room bounds. West/east partitions are new interior walls; the room's
        // north wall is the neighbor room's existing corridor wall (z = 5),
        // re-segmented with a door opening and a window band.
        private const float RecoveryWestX = -13.6f;
        private const float RecoveryEastX = -8.4f;
        private const float RecoveryCenterX = -11.0f;
        private const float RecoveryCenterZ = 2.5f;
        private const float RecoveryTableTopY = 0.45f;
        private static readonly Vector3 RecoveryTableCenter = new Vector3(RecoveryCenterX, 0f, 2.45f);
        private static readonly Vector3 RecoveryStudentSeat = new Vector3(RecoveryCenterX, 0f, 3.30f);
        private static readonly Vector3 RecoveryTeacherSeat = new Vector3(RecoveryCenterX, 0f, 1.55f);

        [MenuItem("Tools/Teacher Training/Build Recovery Room Scene")]
        public static void BuildRecoverySceneFromMenu()
        {
            BuildRecoveryScene();
            EditorTools.MainMenuBuilder.Build();
        }

        public static void BuildRecoverySceneFromCommandLine()
        {
            try
            {
                BuildAll();
                BuildRecoveryScene();
                EditorTools.MainMenuBuilder.Build();
                Debug.Log("KOREAN_CLASSROOM_RECOVERY_SCENE_BUILD_OK");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildRecoveryScene()
        {
            EnsureRecoveryMaterials();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform furniture = GameObject.Find("00_ENVIRONMENT/Furniture").transform;
            Transform props = GameObject.Find("00_ENVIRONMENT/Props").transform;
            Transform students = GameObject.Find("10_STUDENTS").transform;
            Transform camera = GameObject.Find("20_SYSTEMS/TeacherCamera").transform;
            Transform corridor = GameObject.Find("00_ENVIRONMENT/Architecture/CorridorFloor").transform;
            SimulationController simulation = UnityEngine.Object.FindAnyObjectByType<SimulationController>();

            // 1) Empty the general classroom: hide furniture and desk-mounted
            // props (props float mid-air once desks are gone), keep the
            // architecture, corridor, and systems intact.
            furniture.gameObject.SetActive(false);
            props.gameObject.SetActive(false);

            Transform focal = null;
            foreach (Transform student in students.Cast<Transform>())
            {
                if (student.name == "FocalStudent_Minjun")
                {
                    focal = student;
                }
                else
                {
                    student.gameObject.SetActive(false);
                }
            }

            if (focal == null)
            {
                throw new InvalidOperationException("FocalStudent_Minjun is missing from 10_STUDENTS.");
            }

            // 2) Convert the west neighbor room's corridor-adjacent corner.
            CarveRecoveryOpenings(corridor);
            GameObject roomRoot = new GameObject("30_RECOVERY_ROOM");
            roomRoot.transform.position = Vector3.zero;
            BuildRecoveryShell(roomRoot.transform);
            BuildRecoveryDoorAndSigns(roomRoot.transform);
            BuildRecoveryFurnishing(roomRoot.transform);
            BuildRecoveryLighting(roomRoot.transform);

            // 3) Seat Minjun on the student bean bag facing the teacher side.
            // Seated animation poses sit ~0.42 m above the container origin, so
            // floor level + a chair-height bean bag reads as sitting (mirrors
            // how the circle scene drops students onto chair anchors).
            focal.SetPositionAndRotation(RecoveryStudentSeat, Quaternion.Euler(0f, 180f, 0f));

            // 4) Teacher camera on the teacher cushion side; seated eye height so
            // the teacher meets the child at eye level (pedagogically deliberate).
            Vector3 teacherEye = new Vector3(RecoveryCenterX, 1.22f, 1.38f);
            Vector3 studentFace = new Vector3(RecoveryCenterX, 0.80f, RecoveryStudentSeat.z);
            camera.SetPositionAndRotation(teacherEye, Quaternion.LookRotation(studentFace - teacherEye));
            Camera sceneCamera = camera.GetComponent<Camera>();
            if (sceneCamera != null)
            {
                sceneCamera.fieldOfView = 50f;
            }

            TeacherCameraController cameraController = camera.GetComponent<TeacherCameraController>();
            if (cameraController != null)
            {
                SerializedObject cameraData = new SerializedObject(cameraController);
                cameraData.FindProperty("focusTarget").objectReferenceValue = focal;
                cameraData.ApplyModifiedPropertiesWithoutUndo();
            }

            // 5) Scenario flags.
            SerializedObject simulationData = new SerializedObject(simulation);
            simulationData.FindProperty("recoveryRoomScenario").boolValue = true;
            simulationData.FindProperty("circleDiscussionScenario").boolValue = false;
            simulationData.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text title = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(label => label.name == "AppTitle");
            if (title != null)
            {
                title.text = "정서·행동 지원 교사 대응 훈련 · 안정 공간(마음쉼터) 1:1 회복 대화";
            }

            // 6) Save as the recovery training scene.
            KoreanClassroomVisualPolish.ApplyVisualPolish();
            EditorSceneManager.SaveScene(scene, RecoveryScenePath);
        }

        private static void PlaceBeanBag(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation,
            Material tint)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Models/Generated/SM_RecoveryBeanBag_Realistic.obj");
            if (model == null)
            {
                // Fallback keeps the scene buildable if the authored mesh is absent.
                Sphere(name, parent, position + new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 0.45f, 0.9f), tint);
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            // 0.85 drops the dented seat to ~0.41 m, matching the seated-pose hip height.
            instance.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial = tint;
            }

            BoxCollider collider = instance.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.05f, 0.5f, 1.05f);
            collider.center = new Vector3(0f, 0.25f, 0f);
            RegisterTrainingScenes();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureRecoveryMaterials()
        {
            EnsureFolder(MaterialRoot);
            CreateMaterial("M_RecoveryWall", new Color(0.972f, 0.905f, 0.826f), 0f, 0.32f);
            CreateMaterial("M_RecoveryRug", new Color(0.695f, 0.768f, 0.636f), 0f, 0.12f);
            CreateMaterial(
                "M_RecoveryFloorWood",
                new Color(0.83f, 0.70f, 0.53f),
                0f,
                0.30f,
                "Assets/Art/Textures/BirchDesk_Laminate_HQ_v2.png",
                new Vector2(3.4f, 3.2f));
            CreateMaterial("M_RecoveryCushionStudent", new Color(0.55f, 0.70f, 0.86f), 0f, 0.16f);
            CreateMaterial("M_RecoveryCushionTeacher", new Color(0.93f, 0.74f, 0.50f), 0f, 0.16f);
            CreateMaterial("M_RecoveryLampWarm", new Color(1f, 0.86f, 0.62f), 0f, 0.4f, null, null, false, true);
            CreateMaterial("M_RecoveryCard_Red", new Color(0.90f, 0.45f, 0.42f), 0f, 0.24f);
            CreateMaterial("M_RecoveryCard_Orange", new Color(0.95f, 0.66f, 0.36f), 0f, 0.24f);
            CreateMaterial("M_RecoveryCard_Yellow", new Color(0.96f, 0.85f, 0.44f), 0f, 0.24f);
            CreateMaterial("M_RecoveryCard_Green", new Color(0.55f, 0.80f, 0.55f), 0f, 0.24f);
            CreateMaterial("M_RecoveryCard_Blue", new Color(0.48f, 0.66f, 0.88f), 0f, 0.24f);
            CreateMaterial("M_RecoveryCard_Purple", new Color(0.72f, 0.58f, 0.85f), 0f, 0.24f);
            AssetDatabase.SaveAssets();
        }

        // Remove the west neighbor room's full-width corridor wall pieces (and
        // the block furniture that sits inside the new room footprint) so the
        // re-segmented wall with door + window band can take their place.
        private static void CarveRecoveryOpenings(Transform corridor)
        {
            Transform west = corridor.Find("NeighborClassroom_West");
            if (west == null)
            {
                throw new InvalidOperationException("NeighborClassroom_West is missing from CorridorFloor.");
            }

            var doomed = new List<GameObject>();
            foreach (Transform child in west.Cast<Transform>())
            {
                if (child.name == "CorridorWallLower" ||
                    child.name == "BlockDesk_02" ||
                    child.name == "DarkFixture_1" ||
                    child.name.StartsWith("Transom", StringComparison.Ordinal))
                {
                    doomed.Add(child.gameObject);
                }
            }

            foreach (GameObject item in doomed)
            {
                UnityEngine.Object.DestroyImmediate(item);
            }

            // The corridor-side baseboard originally runs across the new door
            // opening; rebuild it in two segments that skip the doorway.
            Transform baseboard = corridor.Find("CorridorBaseboardInnerWest");
            if (baseboard != null)
            {
                UnityEngine.Object.DestroyImmediate(baseboard.gameObject);
            }

            Material baseboardMat = Mat("M_Baseboard");
            Cube("CorridorBaseboardInnerWestA", corridor, new Vector3(-17.45f, 0.09f, 5.115f), new Vector3(7.54f, 0.18f, 0.06f), baseboardMat);
            Cube("CorridorBaseboardInnerWestB", corridor, new Vector3(-9.4225f, 0.09f, 5.115f), new Vector3(5.695f, 0.18f, 0.06f), baseboardMat);

            // The west corridor bulletin sits exactly where the recovery room's
            // window band opens; slide it west so the corridor stays visible.
            Transform westBulletin = corridor.Find("CorridorProps/CorridorBulletinWest");
            if (westBulletin != null)
            {
                Vector3 bulletinPosition = westBulletin.localPosition;
                bulletinPosition.x = -15.3f;
                westBulletin.localPosition = bulletinPosition;
            }
        }

        private static void BuildRecoveryShell(Transform root)
        {
            Material wall = Mat("M_Wall");
            Material recoveryWall = Mat("M_RecoveryWall");
            Material metal = Mat("M_Metal");
            Material glass = Mat("M_Glass");
            Material sill = Mat("M_Baseboard");
            Transform shell = RootObject("Shell", root, Vector3.zero).transform;

            // --- North wall (z = 5) rebuilt in segments -------------------------
            // West remainder of the neighbor room keeps the original look.
            Cube("NorthWestLowerWall", shell, new Vector3(-17.38f, 0.95f, 5f), new Vector3(7.56f, 1.9f, 0.16f), wall);
            BuildTransomBand(shell, "RecoveryWestTransom", -17.38f, 7.56f, 5f);

            // Door opening x -13.6..-12.35 (built in BuildRecoveryDoorAndSigns);
            // header closes 2.32..2.6 above it.
            Cube("NorthDoorHeader", shell, new Vector3(-12.975f, 2.46f, 5f), new Vector3(1.25f, 0.28f, 0.16f), recoveryWall);

            // Pier between door and window band.
            Cube("NorthDoorPier", shell, new Vector3(-12.15f, 1.3f, 5f), new Vector3(0.4f, 2.6f, 0.16f), recoveryWall);

            // Corridor-facing window band x -11.95..-8.75 (sill 1.1, top 2.3):
            // the corridor stays visible from inside the recovery room.
            Cube("NorthWindowSillWall", shell, new Vector3(-10.35f, 0.55f, 5f), new Vector3(3.2f, 1.1f, 0.16f), recoveryWall);
            Cube("NorthWindowSillLedge", shell, new Vector3(-10.35f, 1.125f, 4.90f), new Vector3(3.3f, 0.07f, 0.24f), sill);
            Cube("NorthWindowRailBottom", shell, new Vector3(-10.35f, 1.13f, 5f), new Vector3(3.2f, 0.06f, 0.14f), metal);
            Cube("NorthWindowRailTop", shell, new Vector3(-10.35f, 2.27f, 5f), new Vector3(3.2f, 0.06f, 0.14f), metal);
            Cube("NorthWindowGlass", shell, new Vector3(-10.35f, 1.70f, 5f), new Vector3(3.2f, 1.2f, 0.04f), glass);
            Cube("NorthWindowMullionA", shell, new Vector3(-11.15f, 1.70f, 5f), new Vector3(0.07f, 1.2f, 0.12f), metal);
            Cube("NorthWindowMullionB", shell, new Vector3(-9.55f, 1.70f, 5f), new Vector3(0.07f, 1.2f, 0.12f), metal);
            Cube("NorthAboveWindowWall", shell, new Vector3(-10.35f, 2.45f, 5f), new Vector3(3.2f, 0.3f, 0.16f), recoveryWall);

            // East jamb up to the corridor pilaster (which starts at x -7.33).
            Cube("NorthEastJamb", shell, new Vector3(-8.04f, 1.3f, 5f), new Vector3(1.42f, 2.6f, 0.16f), wall);

            // Interior cladding over the kept CorridorWallUpper (y 2.6..3.4) so
            // the north wall reads as one warm pastel surface from inside.
            Cube("NorthUpperCladding", shell, new Vector3(RecoveryCenterX, 3.0f, 4.905f), new Vector3(5.04f, 0.8f, 0.03f), recoveryWall);

            // --- Interior partitions -------------------------------------------
            Cube("SouthPartition", shell, new Vector3(RecoveryCenterX, 1.7f, 0f), new Vector3(5.36f, 3.4f, 0.16f), recoveryWall);
            Cube("WestPartition", shell, new Vector3(RecoveryWestX, 1.7f, 2.5f), new Vector3(0.16f, 3.4f, 5.16f), recoveryWall);
            Cube("EastPartition", shell, new Vector3(RecoveryEastX, 1.7f, 2.5f), new Vector3(0.16f, 3.4f, 5.16f), recoveryWall);

            // --- Warm wood floor overlay + soft rug ----------------------------
            Cube("RecoveryFloor", shell, new Vector3(RecoveryCenterX, 0.006f, RecoveryCenterZ), new Vector3(5.04f, 0.012f, 4.84f), Mat("M_RecoveryFloorWood"));
            Cylinder("RecoveryRug", shell, new Vector3(RecoveryCenterX, 0.022f, 2.45f), new Vector3(3.1f, 0.01f, 3.1f), Quaternion.identity, Mat("M_RecoveryRug"));
        }

        private static void BuildRecoveryDoorAndSigns(Transform root)
        {
            Material metal = Mat("M_Metal");
            Material wood = Mat("M_TrimWood");
            Material glass = Mat("M_Glass");
            Transform doorRoot = RootObject("RecoveryDoor", root, Vector3.zero).transform;

            // Sliding door parked HALF-OPEN: opening spans x -13.6..-12.35, the
            // slab covers x -12.9..-11.6, leaving a 0.70 m clear gap (>= 0.5 m).
            Cube("RecoveryDoorRail", doorRoot, new Vector3(-12.35f, 2.42f, 5.17f), new Vector3(2.9f, 0.07f, 0.16f), metal);
            GameObject slab = RootObject("RecoverySlidingDoor", doorRoot, new Vector3(-12.25f, 0f, 5.17f));
            Cube("Door", slab.transform, new Vector3(0f, 1.16f, 0f), new Vector3(1.3f, 2.32f, 0.10f), wood);
            Cube("DoorWindow", slab.transform, new Vector3(0f, 1.70f, 0.055f), new Vector3(0.55f, 0.62f, 0.04f), glass);
            Sphere("DoorHandleCorridor", slab.transform, new Vector3(-0.56f, 1.05f, 0.09f), new Vector3(0.10f, 0.10f, 0.10f), metal);
            Sphere("DoorHandleInside", slab.transform, new Vector3(-0.56f, 1.05f, -0.09f), new Vector3(0.10f, 0.10f, 0.10f), metal);

            // "마음쉼터" nameplate above the door on the corridor side (matches
            // the neighbor-classroom nameplate convention) and inside the room.
            // TextMeshPro instead of TextMesh: the legacy font shader ignores
            // depth and bleeds through the recovery-room walls from inside.
            // Raised above the door rail (y 2.42) so the corridor-side plate
            // and label stay fully visible from the hallway.
            Cube("RecoverySignPlate", doorRoot, new Vector3(-12.975f, 2.66f, 5.105f), new Vector3(0.92f, 0.24f, 0.035f), Mat("M_WorkMint"));
            CreateRecoverySignText("RecoverySignText", doorRoot, "마음쉼터", new Vector3(-12.975f, 2.66f, 5.14f), Quaternion.Euler(0f, 180f, 0f), 1.6f, new Color(0.09f, 0.30f, 0.22f));
            CreateRecoverySignText("RecoverySignInnerText", doorRoot, "마음쉼터", new Vector3(-12.975f, 2.46f, 4.895f), Quaternion.identity, 1.2f, new Color(0.44f, 0.30f, 0.16f));
        }

        // World-space TMP sign: depth-tested (unlike TextMesh) so it never
        // shows through walls, and it reuses the bundled Korean SDF font.
        private static void CreateRecoverySignText(
            string name,
            Transform parent,
            string value,
            Vector3 position,
            Quaternion rotation,
            float fontSize,
            Color color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            textObject.transform.localRotation = rotation;
            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
        }

        private static void BuildRecoveryFurnishing(Transform root)
        {
            Transform furnishing = RootObject("Furnishing", root, Vector3.zero).transform;
            Material wood = Mat("M_TrimWood");
            Material metal = Mat("M_Metal");
            Material paper = Mat("M_Paper");

            // --- Low round table with emotion cards ----------------------------
            GameObject table = RootObject("RecoveryTable", furnishing, RecoveryTableCenter);
            Cylinder("Top", table.transform, new Vector3(0f, RecoveryTableTopY - 0.02f, 0f), new Vector3(1.0f, 0.02f, 1.0f), Quaternion.identity, wood);
            Cylinder("Pedestal", table.transform, new Vector3(0f, 0.24f, 0f), new Vector3(0.14f, 0.19f, 0.14f), Quaternion.identity, wood);
            Cylinder("Base", table.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.56f, 0.02f, 0.56f), Quaternion.identity, metal);

            Material[] cardMaterials =
            {
                Mat("M_RecoveryCard_Red"), Mat("M_RecoveryCard_Orange"), Mat("M_RecoveryCard_Yellow"),
                Mat("M_RecoveryCard_Green"), Mat("M_RecoveryCard_Blue"), Mat("M_RecoveryCard_Purple"),
                Mat("M_RecoveryCard_Red")
            };
            for (int i = 0; i < 7; i++)
            {
                float angle = Mathf.Lerp(155f, 25f, i / 6f) * Mathf.Deg2Rad;
                Vector3 cardPosition = new Vector3(
                    Mathf.Cos(angle) * 0.31f,
                    RecoveryTableTopY + 0.004f,
                    Mathf.Sin(angle) * 0.28f);
                GameObject card = Cube($"EmotionCard_{i:00}", table.transform, cardPosition, new Vector3(0.095f, 0.006f, 0.135f), cardMaterials[i]);
                card.transform.localRotation = Quaternion.Euler(0f, -Mathf.Rad2Deg * angle + 90f + (i % 3 - 1) * 8f, 0f);
            }

            GameObject tissue = Cube("TissueBox", table.transform, new Vector3(0.26f, RecoveryTableTopY + 0.045f, -0.24f), new Vector3(0.23f, 0.09f, 0.115f), paper);
            Cube("TissuePuff", tissue.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.35f, 0.28f, 0.5f), paper);
            GameObject cup = Cylinder("PencilCup", table.transform, new Vector3(-0.30f, RecoveryTableTopY + 0.055f, -0.20f), new Vector3(0.09f, 0.055f, 0.09f), Quaternion.identity, Mat("M_WorkBlue"));
            Cylinder("Pencil_A", cup.transform, new Vector3(-0.15f, 1.4f, 0f), new Vector3(0.14f, 1.5f, 0.14f), Quaternion.Euler(0f, 0f, 8f), Mat("M_RecoveryCard_Red"));
            Cylinder("Pencil_B", cup.transform, new Vector3(0.12f, 1.5f, 0.1f), new Vector3(0.14f, 1.5f, 0.14f), Quaternion.Euler(6f, 0f, -7f), Mat("M_RecoveryCard_Green"));
            Cylinder("Pencil_C", cup.transform, new Vector3(0.02f, 1.45f, -0.14f), new Vector3(0.14f, 1.5f, 0.14f), Quaternion.Euler(-7f, 0f, 3f), Mat("M_RecoveryCard_Blue"));

            // --- Bean-bag floor cushions (Blender-authored, dented seat, raised back) ---
            PlaceBeanBag(furnishing, "StudentBeanBag", RecoveryStudentSeat + new Vector3(0f, 0f, 0.16f),
                Quaternion.Euler(0f, 180f, 0f), Mat("M_RecoveryCushionStudent"));
            PlaceBeanBag(furnishing, "TeacherBeanBag", RecoveryTeacherSeat + new Vector3(0f, 0f, -0.16f),
                Quaternion.identity, Mat("M_RecoveryCushionTeacher"));

            // --- Low open bookshelf with picture books (east wall) -------------
            GameObject shelf = RootObject("PictureBookShelf", furnishing, new Vector3(-8.66f, 0f, 1.15f));
            for (int px = 0; px < 2; px++)
            {
                for (int pz = 0; pz < 2; pz++)
                {
                    Cube($"Post_{px}{pz}", shelf.transform, new Vector3(-0.12f + px * 0.24f, 0.36f, -0.70f + pz * 1.40f), new Vector3(0.04f, 0.72f, 0.04f), wood);
                }
            }
            Cube("BoardLow", shelf.transform, new Vector3(0f, 0.10f, 0f), new Vector3(0.30f, 0.035f, 1.48f), wood);
            Cube("BoardMid", shelf.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.30f, 0.035f, 1.48f), wood);
            Cube("BoardTop", shelf.transform, new Vector3(0f, 0.72f, 0f), new Vector3(0.30f, 0.035f, 1.48f), wood);
            Material[] bookMaterials =
            {
                Mat("M_RecoveryCard_Blue"), Mat("M_RecoveryCard_Yellow"), Mat("M_BookTeal"),
                Mat("M_RecoveryCard_Red"), Mat("M_RecoveryCard_Green"), Mat("M_RecoveryCard_Purple"),
                Mat("M_RecoveryCard_Orange"), Mat("M_BookTeal")
            };
            for (int i = 0; i < 8; i++)
            {
                float z = -0.60f + i * 0.17f;
                float height = 0.21f + (i % 3) * 0.02f;
                GameObject book = Cube($"PictureBook_{i:00}", shelf.transform, new Vector3(0f, 0.7375f + height * 0.5f, z), new Vector3(0.16f, height, 0.038f), bookMaterials[i]);
                book.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 4 == 3) ? 9f : 0f);
            }
            for (int i = 0; i < 4; i++)
            {
                Cube($"MidBook_{i:00}", shelf.transform, new Vector3(0f, 0.4375f + 0.10f, -0.45f + i * 0.16f), new Vector3(0.15f, 0.20f, 0.036f), bookMaterials[(i + 3) % bookMaterials.Length]);
            }

            // --- Potted plants (Blender-authored, two corners) ------------------
            PlaceAuthoredPlant(furnishing, "RecoveryPlant_SW", "SM_PottedSansevieria_Realistic.obj",
                new Vector3(-13.1f, 0f, 0.55f), 35f, 1.05f);
            PlaceAuthoredPlant(furnishing, "RecoveryPlant_NE", "SM_PottedRubberPlant_Realistic.obj",
                new Vector3(-9.0f, 0f, 4.35f), 140f, 1.1f);

            // --- Framed kid-art posters ----------------------------------------
            Cube("KidArtFrame_A", furnishing, new Vector3(-11.85f, 1.78f, 0.10f), new Vector3(0.66f, 0.50f, 0.03f), wood);
            Cube("KidArt_A", furnishing, new Vector3(-11.85f, 1.78f, 0.118f), new Vector3(0.58f, 0.42f, 0.012f), Mat("M_Bulletin_Art_TreeSpring"));
            Cube("KidArtFrame_B", furnishing, new Vector3(-10.55f, 1.86f, 0.10f), new Vector3(0.66f, 0.50f, 0.03f), wood);
            Cube("KidArt_B", furnishing, new Vector3(-10.55f, 1.86f, 0.118f), new Vector3(0.58f, 0.42f, 0.012f), Mat("M_Bulletin_Art_Friends"));
            Cube("KidArtFrame_C", furnishing, new Vector3(-13.51f, 1.80f, 3.10f), new Vector3(0.03f, 0.50f, 0.66f), wood);
            Cube("KidArt_C", furnishing, new Vector3(-13.492f, 1.80f, 3.10f), new Vector3(0.012f, 0.42f, 0.58f), Mat("M_Bulletin_Art_SelfPortrait"));

            // --- Wall clock (south wall, mirrored from the classroom clock) ----
            GameObject clock = Cylinder("RecoveryClock", furnishing, new Vector3(-9.7f, 2.55f, 0.14f), new Vector3(0.34f, 0.06f, 0.34f), Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(90f, 0f, 0f), wood);
            Cylinder("ClockFace", clock.transform, new Vector3(0f, 0.055f, 0f), new Vector3(0.88f, 0.03f, 0.88f), Quaternion.identity, Mat("M_ClockFace"));
            Cube("HourHand", clock.transform, new Vector3(0f, 0.09f, 0.09f), new Vector3(0.025f, 0.025f, 0.20f), metal);
            Cube("MinuteHand", clock.transform, new Vector3(0.09f, 0.10f, 0f), new Vector3(0.28f, 0.025f, 0.025f), metal);
        }

        private static void PlaceAuthoredPlant(
            Transform parent,
            string name,
            string fileName,
            Vector3 position,
            float yaw,
            float scale)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Generated/" + fileName);
            if (model == null)
            {
                Debug.LogWarning("Authored plant missing, keeping empty spot: " + fileName);
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = new Vector3(scale, scale, scale);
        }

        private static void BuildRecoveryLighting(Transform root)
        {
            Transform lighting = RootObject("RecoveryLighting", root, Vector3.zero).transform;
            Material fixture = Mat("M_LightFixture");
            Material emissive = Mat("M_Fluorescent");
            Material lampWarm = Mat("M_RecoveryLampWarm");
            Material metal = Mat("M_Metal");

            // Soft ceiling light over the table.
            Cube("RecoveryCeilingFixture", lighting, new Vector3(RecoveryCenterX, 3.30f, RecoveryCenterZ), new Vector3(1.7f, 0.07f, 0.55f), fixture);
            Cube("RecoveryCeilingDiffuser", lighting, new Vector3(RecoveryCenterX, 3.255f, RecoveryCenterZ), new Vector3(1.5f, 0.03f, 0.42f), emissive);
            GameObject ceilingLight = new GameObject("RecoveryCeilingLight");
            ceilingLight.transform.SetParent(lighting, false);
            ceilingLight.transform.localPosition = new Vector3(RecoveryCenterX, 2.95f, RecoveryCenterZ);
            Light overhead = ceilingLight.AddComponent<Light>();
            overhead.type = LightType.Point;
            overhead.range = 6.2f;
            overhead.intensity = 0.78f;
            overhead.color = new Color(1f, 0.94f, 0.83f);
            overhead.shadows = LightShadows.None;

            // Warm floor lamp in the south-east corner.
            GameObject lamp = RootObject("RecoveryFloorLamp", lighting, new Vector3(-8.85f, 0f, 0.55f));
            Cylinder("LampBase", lamp.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.30f, 0.02f, 0.30f), Quaternion.identity, metal);
            Cylinder("LampPole", lamp.transform, new Vector3(0f, 0.70f, 0f), new Vector3(0.035f, 0.68f, 0.035f), Quaternion.identity, metal);
            Cylinder("LampShade", lamp.transform, new Vector3(0f, 1.46f, 0f), new Vector3(0.34f, 0.14f, 0.34f), Quaternion.identity, lampWarm);
            Sphere("LampBulb", lamp.transform, new Vector3(0f, 1.40f, 0f), new Vector3(0.14f, 0.14f, 0.14f), lampWarm);
            GameObject lampLightObject = new GameObject("LampLight");
            lampLightObject.transform.SetParent(lamp.transform, false);
            lampLightObject.transform.localPosition = new Vector3(0f, 1.42f, 0f);
            Light lampLight = lampLightObject.AddComponent<Light>();
            lampLight.type = LightType.Point;
            lampLight.range = 4.6f;
            lampLight.intensity = 0.68f;
            lampLight.color = new Color(1f, 0.82f, 0.58f);
            lampLight.shadows = LightShadows.None;
        }
    }
}
