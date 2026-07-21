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
    // Scene 4: 운동장 — recess-time peer-crisis training on the outdoor field.
    // Benchmarked on a modern Korean elementary schoolyard: bright green
    // artificial-turf soccer field with mowing stripes and white markings, a
    // red urethane track strip around it, orange/white shade awnings along the
    // building side, a pergola shelter, a corner basketball hoop with a blue
    // pole pad, volleyball net posts, a red-brick + bold-yellow + blue-glass
    // school facade behind, and apartment towers + trees past the fence.
    public static partial class KoreanClassroomBuilder
    {
        private const string SchoolyardScenePath = "Assets/Scenes/KoreanSchoolyardTraining.unity";

        // Turf field footprint (x = length, z = width) and the track strip.
        private const float FieldHalfLength = 17f;
        private const float FieldHalfWidth = 11f;
        private const float TrackWidth = 2.2f;
        private const int TurfStripeCount = 13;

        // Focal student withdraws behind the west goal (beat 1: 골대 뒤로 물러나).
        private static readonly Vector3 SchoolyardFocalPosition = new Vector3(-18.6f, 0f, 3.2f);

        [MenuItem("Tools/Teacher Training/Build Schoolyard Scene")]
        public static void BuildSchoolyardSceneFromMenu()
        {
            BuildSchoolyardScene();
            EditorTools.MainMenuBuilder.Build();
        }

        public static void BuildSchoolyardSceneFromCommandLine()
        {
            try
            {
                BuildAll();
                BuildSchoolyardScene();
                EditorTools.MainMenuBuilder.Build();
                Debug.Log("KOREAN_SCHOOLYARD_SCENE_BUILD_OK");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildSchoolyardScene()
        {
            EnsureSchoolyardMaterials();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform architecture = GameObject.Find("00_ENVIRONMENT/Architecture").transform;
            Transform furniture = GameObject.Find("00_ENVIRONMENT/Furniture").transform;
            Transform props = GameObject.Find("00_ENVIRONMENT/Props").transform;
            Transform lighting = GameObject.Find("00_ENVIRONMENT/Lighting").transform;
            Transform students = GameObject.Find("10_STUDENTS").transform;
            Transform camera = GameObject.Find("20_SYSTEMS/TeacherCamera").transform;
            SimulationController simulation = UnityEngine.Object.FindAnyObjectByType<SimulationController>();

            // 1) Hide the interior: architecture (includes CorridorFloor),
            // furniture, props, and any recovery room. Keep systems + daylight.
            architecture.gameObject.SetActive(false);
            furniture.gameObject.SetActive(false);
            props.gameObject.SetActive(false);
            GameObject corridor = GameObject.Find("CorridorFloor");
            if (corridor != null)
            {
                corridor.SetActive(false);
            }
            GameObject recoveryRoom = GameObject.Find("30_RECOVERY_ROOM");
            if (recoveryRoom != null)
            {
                recoveryRoom.SetActive(false);
            }

            // Interior fluorescents would float mid-air over the yard — keep
            // only the directional daylight and retune it for outdoors.
            foreach (Transform child in lighting.Cast<Transform>())
            {
                if (child.name != "Daylight")
                {
                    child.gameObject.SetActive(false);
                }
            }
            Light sun = lighting.Find("Daylight").GetComponent<Light>();
            // High near-noon sun so the tall apartment ring cannot drape the
            // field in shadow.
            sun.transform.rotation = Quaternion.Euler(62f, -25f, 0f);
            sun.intensity = 1.12f;
            RenderSettings.ambientSkyColor = new Color(0.64f, 0.74f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.59f, 0.56f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.39f, 0.32f);

            // 2) Focal stays active (container name is historical — the scenario
            // persona is Jiho; SimulationController's focalStudent reference is
            // what matters). Keep 5 classmates as playing peers on the turf;
            // deactivate the rest. Session-start code hands active classmates
            // ambient gestures automatically.
            var peerPlacements = new Dictionary<string, (Vector3 position, float yaw)>
            {
                { "Classmate_Seoyeon", (new Vector3(2.0f, 0f, 3.1f), 140f) },
                { "Classmate_Doyun", (new Vector3(5.2f, 0f, -2.3f), -60f) },
                { "Classmate_Yuna", (new Vector3(9.1f, 0f, 4.3f), -120f) },
                { "Classmate_Junseo", (new Vector3(12.2f, 0f, -4.8f), 25f) },
                { "Classmate_Sua", (new Vector3(6.8f, 0f, 7.6f), 175f) }
            };

            Transform focal = null;
            foreach (Transform student in students.Cast<Transform>())
            {
                if (student.name == "FocalStudent_Minjun")
                {
                    focal = student;
                }
                else if (peerPlacements.TryGetValue(student.name, out var placement))
                {
                    student.gameObject.SetActive(true);
                    student.SetPositionAndRotation(
                        placement.position,
                        Quaternion.Euler(0f, placement.yaw, 0f));
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

            // 3) Build the outdoor environment.
            GameObject yardRoot = new GameObject("40_SCHOOLYARD");
            yardRoot.transform.position = Vector3.zero;
            BuildSchoolyardGround(yardRoot.transform);
            BuildSchoolyardFieldMarkings(yardRoot.transform);
            BuildSchoolyardGoals(yardRoot.transform);
            BuildSchoolyardCourts(yardRoot.transform);
            BuildSchoolyardCanopies(yardRoot.transform);
            BuildSchoolyardPergola(yardRoot.transform);
            BuildSchoolyardFacade(yardRoot.transform);
            BuildSchoolyardPerimeter(yardRoot.transform);

            // 4) Focal behind the west goal on the track edge, turned away from
            // the field (beat 1: withdrawn, avoiding gaze). Teacher stands on
            // the turf ~6 m away at standing eye height.
            focal.SetPositionAndRotation(SchoolyardFocalPosition, Quaternion.Euler(0f, -125f, 0f));

            // Offset north of the goal mouth so the sight line to the focal
            // clears the goal frame and net (verified via play-mode capture).
            Vector3 teacherEye = new Vector3(-13.4f, 1.62f, 3.0f);
            Vector3 focalFace = SchoolyardFocalPosition + new Vector3(0f, 1.15f, 0f);
            camera.SetPositionAndRotation(teacherEye, Quaternion.LookRotation(focalFace - teacherEye));
            Camera sceneCamera = camera.GetComponent<Camera>();
            if (sceneCamera != null)
            {
                sceneCamera.fieldOfView = 52f;
                sceneCamera.farClipPlane = 300f;
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
            simulationData.FindProperty("schoolyardScenario").boolValue = true;
            simulationData.FindProperty("recoveryRoomScenario").boolValue = false;
            simulationData.FindProperty("circleDiscussionScenario").boolValue = false;
            simulationData.FindProperty("gymnasiumScenario").boolValue = false;
            simulationData.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text title = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(label => label.name == "AppTitle");
            if (title != null)
            {
                title.text = "정서·행동 지원 교사 대응 훈련 · 운동장 쉬는 시간 또래 위기 대응";
            }

            // 6) Polish + save + register.
            KoreanClassroomVisualPolish.ApplyVisualPolish();
            EditorSceneManager.SaveScene(scene, SchoolyardScenePath);
            RegisterTrainingScenes();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureSchoolyardMaterials()
        {
            EnsureFolder(MaterialRoot);
            CreateMaterial("M_YardUrethane", new Color(0.70f, 0.70f, 0.72f), 0f, 0.22f);
            CreateMaterial("M_YardSkirt", new Color(0.46f, 0.54f, 0.38f), 0f, 0.10f);
            CreateMaterial("M_TurfLight", new Color(0.40f, 0.62f, 0.26f), 0f, 0.10f);
            CreateMaterial("M_TurfDark", new Color(0.30f, 0.51f, 0.20f), 0f, 0.10f);
            CreateMaterial("M_TrackRed", new Color(0.66f, 0.29f, 0.25f), 0f, 0.18f);
            CreateMaterial("M_FieldLine", new Color(0.93f, 0.94f, 0.94f), 0f, 0.20f);
            CreateMaterial("M_GoalFrameWhite", new Color(0.92f, 0.93f, 0.94f), 0.35f, 0.45f);
            CreateMaterial("M_GoalNetRope", new Color(0.88f, 0.89f, 0.91f), 0f, 0.30f);
            CreateMaterial("M_AwningOrange", new Color(0.93f, 0.51f, 0.16f), 0f, 0.30f);
            CreateMaterial("M_AwningYellow", new Color(0.95f, 0.77f, 0.20f), 0f, 0.30f);
            CreateMaterial("M_AwningWhite", new Color(0.94f, 0.93f, 0.89f), 0f, 0.30f);
            CreateMaterial("M_AwningSteel", new Color(0.62f, 0.64f, 0.66f), 0.6f, 0.5f);
            CreateMaterial("M_PergolaDark", new Color(0.24f, 0.20f, 0.17f), 0f, 0.30f);
            CreateMaterial("M_BrickRed", new Color(0.60f, 0.32f, 0.25f), 0f, 0.18f);
            CreateMaterial("M_FacadeYellow", new Color(0.95f, 0.79f, 0.10f), 0f, 0.30f);
            CreateMaterial("M_FacadeConcrete", new Color(0.84f, 0.83f, 0.79f), 0f, 0.24f);
            CreateMaterial("M_GlassBlue", new Color(0.42f, 0.60f, 0.78f), 0.15f, 0.90f);
            CreateMaterial("M_WindowDark", new Color(0.13f, 0.16f, 0.20f), 0.1f, 0.72f);
            CreateMaterial("M_FenceGreen", new Color(0.22f, 0.35f, 0.24f), 0.3f, 0.40f);
            CreateMaterial("M_FenceMesh", new Color(0.25f, 0.38f, 0.27f, 0.45f), 0f, 0.30f, null, null, true);
            CreateMaterial("M_TreeTrunk", new Color(0.37f, 0.27f, 0.19f), 0f, 0.20f);
            CreateMaterial("M_TreeLeaf", new Color(0.27f, 0.44f, 0.21f), 0f, 0.12f);
            CreateMaterial("M_ApartmentLight", new Color(0.87f, 0.85f, 0.81f), 0f, 0.24f);
            CreateMaterial("M_ApartmentBeige", new Color(0.80f, 0.76f, 0.70f), 0f, 0.24f);
            CreateMaterial("M_ApartmentBand", new Color(0.30f, 0.34f, 0.40f), 0.1f, 0.55f);
            CreateMaterial("M_ApartmentFar", new Color(0.79f, 0.81f, 0.85f), 0f, 0.20f);
            CreateMaterial("M_ApartmentFarBand", new Color(0.62f, 0.65f, 0.71f), 0f, 0.30f);
            CreateMaterial("M_AptAccentGreen", new Color(0.44f, 0.58f, 0.47f), 0f, 0.28f);
            CreateMaterial("M_AptAccentBlue", new Color(0.41f, 0.52f, 0.66f), 0f, 0.28f);
            CreateMaterial("M_AptAccentOrange", new Color(0.79f, 0.55f, 0.33f), 0f, 0.28f);
            CreateMaterial("M_AptSignPlate", new Color(0.94f, 0.94f, 0.95f), 0f, 0.30f);
            CreateMaterial("M_PoleBlue", new Color(0.18f, 0.33f, 0.64f), 0.4f, 0.45f);
            CreateMaterial("M_VolleyNet", new Color(0.94f, 0.94f, 0.96f, 0.42f), 0f, 0.25f, null, null, true);
            AssetDatabase.SaveAssets();
        }

        private static void BuildSchoolyardGround(Transform root)
        {
            Transform ground = RootObject("Ground", root, Vector3.zero).transform;
            Material urethane = Mat("M_YardUrethane");
            Material track = Mat("M_TrackRed");

            // Base yard slab (60 x 46) + a far grass skirt so no void shows at
            // eye level in any yaw direction.
            Cube("YardBase", ground, new Vector3(0f, -0.05f, 0f), new Vector3(60f, 0.1f, 46f), urethane);
            Cube("FarSkirt", ground, new Vector3(0f, -0.11f, 0f), new Vector3(240f, 0.1f, 240f), Mat("M_YardSkirt"));

            // Turf field: alternating light/dark mowing stripes across x.
            float stripeWidth = FieldHalfLength * 2f / TurfStripeCount;
            for (int i = 0; i < TurfStripeCount; i++)
            {
                float x = -FieldHalfLength + stripeWidth * (i + 0.5f);
                Cube($"TurfStripe_{i:00}", ground,
                    new Vector3(x, 0.0225f, 0f),
                    new Vector3(stripeWidth + 0.01f, 0.045f, FieldHalfWidth * 2f),
                    i % 2 == 0 ? Mat("M_TurfLight") : Mat("M_TurfDark"));
            }

            // Red urethane track strip bordering the field on all four sides.
            float outerHalfX = FieldHalfLength + TrackWidth;
            Cube("TrackNorth", ground, new Vector3(0f, 0.015f, FieldHalfWidth + TrackWidth * 0.5f), new Vector3(outerHalfX * 2f, 0.03f, TrackWidth), track);
            Cube("TrackSouth", ground, new Vector3(0f, 0.015f, -FieldHalfWidth - TrackWidth * 0.5f), new Vector3(outerHalfX * 2f, 0.03f, TrackWidth), track);
            Cube("TrackEast", ground, new Vector3(FieldHalfLength + TrackWidth * 0.5f, 0.015f, 0f), new Vector3(TrackWidth, 0.03f, FieldHalfWidth * 2f), track);
            Cube("TrackWest", ground, new Vector3(-FieldHalfLength - TrackWidth * 0.5f, 0.015f, 0f), new Vector3(TrackWidth, 0.03f, FieldHalfWidth * 2f), track);
        }

        private static void BuildSchoolyardFieldMarkings(Transform root)
        {
            Transform markings = RootObject("FieldMarkings", root, Vector3.zero).transform;
            Material line = Mat("M_FieldLine");
            const float lineY = 0.05f;
            const float lineH = 0.012f;
            const float lineW = 0.1f;

            // Touchlines + goal lines + center line.
            Cube("TouchlineNorth", markings, new Vector3(0f, lineY, FieldHalfWidth - 0.05f), new Vector3(FieldHalfLength * 2f, lineH, lineW), line);
            Cube("TouchlineSouth", markings, new Vector3(0f, lineY, -FieldHalfWidth + 0.05f), new Vector3(FieldHalfLength * 2f, lineH, lineW), line);
            Cube("GoalLineWest", markings, new Vector3(-FieldHalfLength + 0.05f, lineY, 0f), new Vector3(lineW, lineH, FieldHalfWidth * 2f), line);
            Cube("GoalLineEast", markings, new Vector3(FieldHalfLength - 0.05f, lineY, 0f), new Vector3(lineW, lineH, FieldHalfWidth * 2f), line);
            Cube("CenterLine", markings, new Vector3(0f, lineY, 0f), new Vector3(lineW, lineH, FieldHalfWidth * 2f), line);

            // Center circle from short rotated segments + center spot.
            const float circleRadius = 3f;
            const int circleSegments = 28;
            float segmentLength = 2f * Mathf.PI * circleRadius / circleSegments + 0.02f;
            for (int i = 0; i < circleSegments; i++)
            {
                float angle = i * (360f / circleSegments);
                float rad = angle * Mathf.Deg2Rad;
                GameObject segment = Cube($"CenterCircle_{i:00}", markings,
                    new Vector3(Mathf.Cos(rad) * circleRadius, lineY, Mathf.Sin(rad) * circleRadius),
                    new Vector3(segmentLength, lineH, lineW), line);
                segment.transform.localRotation = Quaternion.Euler(0f, -angle + 90f, 0f);
            }
            Cylinder("CenterSpot", markings, new Vector3(0f, lineY, 0f), new Vector3(0.24f, lineH * 0.5f, 0.24f), Quaternion.identity, line);

            // Penalty boxes (4.2 m deep, 12 m wide) at both ends.
            foreach (int sign in new[] { -1, 1 })
            {
                string tag = sign < 0 ? "West" : "East";
                float frontX = sign * (FieldHalfLength - 4.2f);
                Cube($"PenaltyFront{tag}", markings, new Vector3(frontX, lineY, 0f), new Vector3(lineW, lineH, 12f), line);
                Cube($"PenaltySideN{tag}", markings, new Vector3(sign * (FieldHalfLength - 2.1f), lineY, 6f), new Vector3(4.2f, lineH, lineW), line);
                Cube($"PenaltySideS{tag}", markings, new Vector3(sign * (FieldHalfLength - 2.1f), lineY, -6f), new Vector3(4.2f, lineH, lineW), line);
                Cylinder($"PenaltySpot{tag}", markings, new Vector3(sign * (FieldHalfLength - 2.8f), lineY, 0f), new Vector3(0.2f, lineH * 0.5f, 0.2f), Quaternion.identity, line);
            }
        }

        // Blender-authored soccer goals (white frame + rope-net lattice) on
        // both goal lines, mouths facing the field center.
        private static void BuildSchoolyardGoals(Transform root)
        {
            Transform goals = RootObject("Goals", root, Vector3.zero).transform;
            PlaceSchoolyardGoal(goals, "SoccerGoalWest", new Vector3(-FieldHalfLength, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            PlaceSchoolyardGoal(goals, "SoccerGoalEast", new Vector3(FieldHalfLength, 0f, 0f), Quaternion.Euler(0f, -90f, 0f));
        }

        private static void PlaceSchoolyardGoal(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Models/Generated/SM_SoccerGoal_Realistic.obj");
            if (model == null)
            {
                // Fallback keeps the scene buildable without the authored mesh.
                GameObject fallback = RootObject(name, parent, position);
                fallback.transform.localRotation = rotation;
                Material white = Mat("M_GoalFrameWhite");
                Cylinder("PostL", fallback.transform, new Vector3(-2f, 1f, 0f), new Vector3(0.11f, 1f, 0.11f), Quaternion.identity, white);
                Cylinder("PostR", fallback.transform, new Vector3(2f, 1f, 0f), new Vector3(0.11f, 1f, 0.11f), Quaternion.identity, white);
                Cylinder("Crossbar", fallback.transform, new Vector3(0f, 2f, 0f), new Vector3(0.11f, 2.05f, 0.11f), Quaternion.Euler(0f, 0f, 90f), white);
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = name;
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterial = renderer.name.Contains("Net")
                    ? Mat("M_GoalNetRope")
                    : Mat("M_GoalFrameWhite");
            }

            BoxCollider collider = instance.AddComponent<BoxCollider>();
            collider.size = new Vector3(4.3f, 2.1f, 1.5f);
            collider.center = new Vector3(0f, 1.05f, -0.6f);
        }

        // Basketball hoop (blue pole pad, benchmark photo 3) + volleyball/
        // badminton net posts on the south urethane apron outside the track.
        private static void BuildSchoolyardCourts(Transform root)
        {
            Transform courts = RootObject("Courts", root, Vector3.zero).transform;

            GameObject hoopModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Models/Generated/SM_BasketballHoop_Realistic.obj");
            if (hoopModel != null)
            {
                GameObject hoop = (GameObject)PrefabUtility.InstantiatePrefab(hoopModel, courts);
                hoop.name = "BasketballHoop";
                hoop.transform.localPosition = new Vector3(-14f, 0f, -19.2f);
                hoop.transform.localRotation = Quaternion.identity;
                BoxCollider hoopCollider = hoop.AddComponent<BoxCollider>();
                hoopCollider.size = new Vector3(1.0f, 3.6f, 1.1f);
                hoopCollider.center = new Vector3(0f, 1.8f, -0.3f);
            }
            // Painted half-court key under the hoop.
            Material line = Mat("M_FieldLine");
            Cube("HoopKeyLeft", courts, new Vector3(-15.3f, 0.011f, -17.4f), new Vector3(0.08f, 0.012f, 3.4f), line);
            Cube("HoopKeyRight", courts, new Vector3(-12.7f, 0.011f, -17.4f), new Vector3(0.08f, 0.012f, 3.4f), line);
            Cube("HoopKeyFront", courts, new Vector3(-14f, 0.011f, -15.7f), new Vector3(2.68f, 0.012f, 0.08f), line);

            // Volleyball/badminton posts pair: blue poles + translucent net.
            Transform volley = RootObject("VolleyballNet", courts, new Vector3(12f, 0f, -17.5f)).transform;
            Material blue = Mat("M_PoleBlue");
            Cylinder("PoleWest", volley, new Vector3(-3f, 1.2f, 0f), new Vector3(0.09f, 1.2f, 0.09f), Quaternion.identity, blue);
            Cylinder("PoleEast", volley, new Vector3(3f, 1.2f, 0f), new Vector3(0.09f, 1.2f, 0.09f), Quaternion.identity, blue);
            Cylinder("BaseWest", volley, new Vector3(-3f, 0.03f, 0f), new Vector3(0.4f, 0.03f, 0.4f), Quaternion.identity, blue);
            Cylinder("BaseEast", volley, new Vector3(3f, 0.03f, 0f), new Vector3(0.4f, 0.03f, 0.4f), Quaternion.identity, blue);
            Cube("Net", volley, new Vector3(0f, 1.72f, 0f), new Vector3(5.9f, 0.78f, 0.02f), Mat("M_VolleyNet"));
            Cube("NetTape", volley, new Vector3(0f, 2.14f, 0f), new Vector3(5.9f, 0.07f, 0.03f), Mat("M_FieldLine"));
        }

        // Angled striped shade awnings along the building side of the track
        // (benchmark photo 2: orange/white and yellow/white canopy bands on
        // thin steel posts, sloping down toward the field).
        private static void BuildSchoolyardCanopies(Transform root)
        {
            Transform canopies = RootObject("ShadeCanopies", root, Vector3.zero).transform;
            Material steel = Mat("M_AwningSteel");
            float[] unitCenters = { -12.8f, -6.4f, 0f, 6.4f, 12.8f };
            for (int unit = 0; unit < unitCenters.Length; unit++)
            {
                Transform awning = RootObject($"Awning_{unit:00}", canopies, new Vector3(unitCenters[unit], 0f, 15f)).transform;
                Material bandA = unit % 2 == 0 ? Mat("M_AwningOrange") : Mat("M_AwningYellow");
                Material bandB = Mat("M_AwningWhite");

                // Six alternating canopy stripes, pitched down toward the field.
                for (int band = 0; band < 6; band++)
                {
                    float x = -2.5f + band * 1f;
                    GameObject stripe = Cube($"CanopyBand_{band}", awning,
                        new Vector3(x, 2.2f, 0f), new Vector3(0.99f, 0.035f, 2.35f),
                        band % 2 == 0 ? bandA : bandB);
                    stripe.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
                }

                // Steel posts: taller rear pair, shorter front pair.
                Cylinder("PostRearL", awning, new Vector3(-2.6f, 1.24f, 1.05f), new Vector3(0.07f, 1.24f, 0.07f), Quaternion.identity, steel);
                Cylinder("PostRearR", awning, new Vector3(2.6f, 1.24f, 1.05f), new Vector3(0.07f, 1.24f, 0.07f), Quaternion.identity, steel);
                Cylinder("PostFrontL", awning, new Vector3(-2.6f, 0.96f, -1.05f), new Vector3(0.06f, 0.96f, 0.06f), Quaternion.identity, steel);
                Cylinder("PostFrontR", awning, new Vector3(2.6f, 0.96f, -1.05f), new Vector3(0.06f, 0.96f, 0.06f), Quaternion.identity, steel);
                Cube("RearBeam", awning, new Vector3(0f, 2.44f, 1.05f), new Vector3(5.3f, 0.07f, 0.07f), steel);
                Cube("FrontBeam", awning, new Vector3(0f, 1.9f, -1.05f), new Vector3(5.3f, 0.07f, 0.07f), steel);
            }
        }

        // Pergola shade shelter (dark posts, flat slat roof, two benches) in
        // the north-west corner outside the track — benchmark photo 2 corner.
        private static void BuildSchoolyardPergola(Transform root)
        {
            Transform pergola = RootObject("Pergola", root, new Vector3(-22.3f, 0f, 10.5f)).transform;
            Material dark = Mat("M_PergolaDark");

            foreach ((float px, float pz) in new[] { (-1.7f, -1.5f), (1.7f, -1.5f), (-1.7f, 1.5f), (1.7f, 1.5f) })
            {
                Cube($"Post_{px:0.0}_{pz:0.0}", pergola, new Vector3(px, 1.25f, pz), new Vector3(0.14f, 2.5f, 0.14f), dark);
            }
            Cube("BeamNorth", pergola, new Vector3(0f, 2.56f, 1.5f), new Vector3(3.9f, 0.12f, 0.16f), dark);
            Cube("BeamSouth", pergola, new Vector3(0f, 2.56f, -1.5f), new Vector3(3.9f, 0.12f, 0.16f), dark);
            for (int slat = 0; slat < 9; slat++)
            {
                float x = -1.8f + slat * 0.45f;
                Cube($"RoofSlat_{slat}", pergola, new Vector3(x, 2.68f, 0f), new Vector3(0.12f, 0.05f, 3.6f), dark);
            }

            // Two corridor benches under the roof, facing each other.
            InstantiateGeneratedProp("SM_CorridorBench_Realistic.obj", "PergolaBenchWest", pergola,
                new Vector3(-0.95f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.44f, 0.46f, 1.5f), new Vector3(0f, 0.23f, 0f));
            InstantiateGeneratedProp("SM_CorridorBench_Realistic.obj", "PergolaBenchEast", pergola,
                new Vector3(0.95f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), new Vector3(0.44f, 0.46f, 1.5f), new Vector3(0f, 0.23f, 0f));
        }

        // School building backdrop along the +z edge: red brick + one bold
        // yellow painted section + blue-glass stair core, window grid, and an
        // entrance canopy (benchmark photo 2 facade).
        private static void BuildSchoolyardFacade(Transform root)
        {
            Transform facade = RootObject("SchoolFacade", root, Vector3.zero).transform;
            Material brick = Mat("M_BrickRed");
            Material yellow = Mat("M_FacadeYellow");
            Material concrete = Mat("M_FacadeConcrete");
            Material glassBlue = Mat("M_GlassBlue");
            Material window = Mat("M_WindowDark");

            const float frontZ = 19.6f;
            const float depth = 4.3f;
            const float centerZ = frontZ + depth * 0.5f;
            const float height = 14f;

            // Facade sections (west brick / bold yellow / east brick / glass core).
            Cube("SectionBrickWest", facade, new Vector3(-14.5f, height * 0.5f, centerZ), new Vector3(21f, height, depth), brick);
            Cube("SectionYellow", facade, new Vector3(4f, height * 0.5f, centerZ), new Vector3(16f, height, depth), yellow);
            Cube("SectionBrickEast", facade, new Vector3(20.5f, height * 0.5f, centerZ), new Vector3(9f, height, depth), brick);
            Cube("GlassStairCore", facade, new Vector3(14f, 7.5f, centerZ - 0.2f), new Vector3(4f, 15f, depth + 0.4f), glassBlue);
            for (int mullion = 0; mullion < 3; mullion++)
            {
                Cube($"CoreMullion_{mullion}", facade, new Vector3(13f + mullion, 7.5f, frontZ - 0.24f), new Vector3(0.09f, 15f, 0.05f), concrete);
            }
            for (int level = 1; level <= 4; level++)
            {
                Cube($"CoreBand_{level}", facade, new Vector3(14f, level * 3.4f, frontZ - 0.24f), new Vector3(4f, 0.12f, 0.05f), concrete);
            }
            Cube("Parapet", facade, new Vector3(0f, height + 0.15f, centerZ), new Vector3(50.6f, 0.35f, depth + 0.25f), concrete);

            // Window grid: 4 floors of dark inset quads, skipping the glass core.
            for (int floor = 0; floor < 4; floor++)
            {
                float y = 1.9f + floor * 3.4f;
                for (float x = -23.4f; x <= 23.5f; x += 2.4f)
                {
                    if (x > 11.2f && x < 17f)
                    {
                        continue;
                    }
                    Cube($"Window_{floor}_{x:0.0}", facade, new Vector3(x, y, frontZ - 0.03f), new Vector3(1.7f, 1.5f, 0.06f), window);
                }
            }

            // Entrance with canopy + steps on the west brick section.
            Transform entrance = RootObject("Entrance", facade, new Vector3(-14.5f, 0f, 0f)).transform;
            Cube("Door", entrance, new Vector3(0f, 1.35f, frontZ - 0.04f), new Vector3(2.6f, 2.7f, 0.08f), window);
            Cube("CanopySlab", entrance, new Vector3(0f, 3.15f, frontZ - 1.2f), new Vector3(4.8f, 0.2f, 2.6f), concrete);
            Cylinder("CanopyPostL", entrance, new Vector3(-2f, 1.55f, frontZ - 2.3f), new Vector3(0.09f, 1.55f, 0.09f), Quaternion.identity, concrete);
            Cylinder("CanopyPostR", entrance, new Vector3(2f, 1.55f, frontZ - 2.3f), new Vector3(0.09f, 1.55f, 0.09f), Quaternion.identity, concrete);
            Cube("StepUpper", entrance, new Vector3(0f, 0.12f, frontZ - 0.6f), new Vector3(3.4f, 0.24f, 1.0f), concrete);
            Cube("StepLower", entrance, new Vector3(0f, 0.06f, frontZ - 1.35f), new Vector3(3.8f, 0.12f, 0.7f), concrete);
        }

        // Perimeter fence + trees inside it + distant apartment towers past it.
        private static void BuildSchoolyardPerimeter(Transform root)
        {
            Transform perimeter = RootObject("Perimeter", root, Vector3.zero).transform;
            Material fence = Mat("M_FenceGreen");
            Material mesh = Mat("M_FenceMesh");

            // Fence runs: south edge + west/east edges (building seals north).
            BuildFenceRun(perimeter, "FenceSouth", new Vector3(-29f, 0f, -22.5f), new Vector3(29f, 0f, -22.5f), fence, mesh);
            BuildFenceRun(perimeter, "FenceWest", new Vector3(-29.5f, 0f, -22f), new Vector3(-29.5f, 0f, 19f), fence, mesh);
            BuildFenceRun(perimeter, "FenceEast", new Vector3(29.5f, 0f, -22f), new Vector3(29.5f, 0f, 19f), fence, mesh);

            // Trees ring the yard inside the fence.
            Vector3[] treeSpots =
            {
                new Vector3(-27f, 0f, 10f), new Vector3(-27f, 0f, 2f), new Vector3(-27f, 0f, -6f), new Vector3(-27f, 0f, -14f),
                new Vector3(27f, 0f, 10f), new Vector3(27f, 0f, 2f), new Vector3(27f, 0f, -6f), new Vector3(27f, 0f, -14f),
                new Vector3(-20f, 0f, -21f), new Vector3(-10f, 0f, -21.2f), new Vector3(0f, 0f, -21f), new Vector3(10f, 0f, -21.2f), new Vector3(20f, 0f, -21f),
                new Vector3(-26.5f, 0f, 17.5f), new Vector3(26.5f, 0f, 17.5f)
            };
            for (int i = 0; i < treeSpots.Length; i++)
            {
                BuildSchoolyardTree(perimeter, $"Tree_{i:00}", treeSpots[i], 0.9f + (i % 4) * 0.12f);
            }

            // Dense Korean apartment complex (아파트촌) ringing the yard on the
            // three non-school sides — 판상형 slab buildings in two depth
            // layers (benchmark photos 1 and 3). Near ring: fully detailed
            // beyond the fence + tree line. Far ring: taller, hazier material,
            // offset so gaps in the near ring show far towers.
            Transform complex = RootObject("ApartmentComplex", perimeter, Vector3.zero).transform;

            // Near ring. yaw 0 = long axis along x, yard face toward +z
            // (north). Set back ~25 m past the fence so rooftops leave sky
            // visible from the field (verified via play-mode captures — the
            // first pass at z -34 read as a windowless canyon).
            BuildApartmentSlab(complex, "AptNear_S1", new Vector3(-18f, 0f, -46f), 4f, 33f, 11, Mat("M_ApartmentLight"), Mat("M_AptAccentGreen"), false, null);
            BuildApartmentSlab(complex, "AptNear_S2", new Vector3(14f, 0f, -52f), -3f, 24f, 13, Mat("M_ApartmentBeige"), Mat("M_AptAccentGreen"), false, "105");
            BuildApartmentSlab(complex, "AptNear_W1", new Vector3(-50f, 0f, -8f), 92f, 33f, 11, Mat("M_ApartmentBeige"), Mat("M_AptAccentBlue"), false, "102");
            BuildApartmentSlab(complex, "AptNear_W2", new Vector3(-54f, 0f, 14f), 87f, 22f, 13, Mat("M_ApartmentLight"), Mat("M_AptAccentBlue"), false, null);
            BuildApartmentSlab(complex, "AptNear_E1", new Vector3(50f, 0f, -4f), -88f, 33f, 10, Mat("M_ApartmentLight"), Mat("M_AptAccentOrange"), false, "107");
            BuildApartmentSlab(complex, "AptNear_E2", new Vector3(53f, 0f, 16f), -94f, 24f, 12, Mat("M_ApartmentBeige"), Mat("M_AptAccentOrange"), false, null);

            // Far ring: taller and hazier, offset to show through near-ring
            // gaps and sealing the diagonal corners so a 360-degree yaw sweep
            // never hits empty horizon.
            BuildApartmentSlab(complex, "AptFar_S1", new Vector3(-16f, 0f, -80f), 2f, 40f, 19, Mat("M_ApartmentFar"), null, true, null);
            BuildApartmentSlab(complex, "AptFar_S2", new Vector3(26f, 0f, -74f), -6f, 33f, 17, Mat("M_ApartmentFar"), null, true, null);
            BuildApartmentSlab(complex, "AptFar_SW", new Vector3(-58f, 0f, -58f), 42f, 30f, 16, Mat("M_ApartmentFar"), null, true, null);
            BuildApartmentSlab(complex, "AptFar_SE", new Vector3(60f, 0f, -56f), -40f, 30f, 18, Mat("M_ApartmentFar"), null, true, null);
            BuildApartmentSlab(complex, "AptFar_W", new Vector3(-78f, 0f, 6f), 90f, 36f, 19, Mat("M_ApartmentFar"), null, true, null);
            BuildApartmentSlab(complex, "AptFar_E", new Vector3(76f, 0f, -12f), -89f, 33f, 20, Mat("M_ApartmentFar"), null, true, null);
            BuildApartmentSlab(complex, "AptFar_NW", new Vector3(-64f, 0f, 36f), 58f, 30f, 17, Mat("M_ApartmentFar"), null, true, null);
            BuildApartmentSlab(complex, "AptFar_NE", new Vector3(64f, 0f, 36f), -58f, 30f, 16, Mat("M_ApartmentFar"), null, true, null);
        }

        // One 판상형 slab building: light facade, dense horizontal window-band
        // grid (one wrap-around dark band per floor — cheap quads, no
        // per-window objects), colored gable accent stripes, rooftop
        // elevator/stair penthouses, and an optional painted 동-number plate.
        private static void BuildApartmentSlab(
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            float length,
            int floors,
            Material body,
            Material accent,
            bool far,
            string dongNumber)
        {
            const float storyHeight = 2.8f;
            const float depth = 10f;
            float height = floors * storyHeight;
            Transform slab = RootObject(name, parent, position).transform;
            slab.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Cube("Body", slab, new Vector3(0f, height * 0.5f, 0f), new Vector3(length, height, depth), body);

            // Window/balcony bands: skip every other floor on the far ring to
            // halve the cost while keeping the striped high-rise read.
            Material band = far ? Mat("M_ApartmentFarBand") : Mat("M_ApartmentBand");
            int bandStep = far ? 2 : 1;
            for (int floor = 1; floor < floors; floor += bandStep)
            {
                Cube($"Band_{floor:00}", slab,
                    new Vector3(0f, floor * storyHeight, 0f),
                    new Vector3(length + 0.18f, far ? 1.1f : 0.62f, depth + 0.18f), band);
            }

            // Vertical unit seams (판상형 slabs are 2-4 joined units).
            for (float seamX = -length * 0.5f + 11f; seamX < length * 0.5f - 1f; seamX += 11f)
            {
                Cube($"UnitSeam_{seamX:0}", slab,
                    new Vector3(seamX, height * 0.5f, 0f),
                    new Vector3(0.35f, height, depth + 0.24f), band);
            }

            // Gable accent stripes on both end walls (one hue per cluster).
            if (accent != null)
            {
                Cube("AccentEast", slab, new Vector3(length * 0.5f + 0.06f, height * 0.5f, 0f), new Vector3(0.35f, height, 4.4f), accent);
                Cube("AccentWest", slab, new Vector3(-length * 0.5f - 0.06f, height * 0.5f, 0f), new Vector3(0.35f, height, 4.4f), accent);
            }

            // Rooftop elevator/stair penthouse boxes.
            Cube("PenthouseA", slab, new Vector3(-length * 0.28f, height + 1.3f, 0f), new Vector3(5f, 2.6f, 4.2f), body);
            Cube("PenthouseB", slab, new Vector3(length * 0.30f, height + 1f, 0.6f), new Vector3(3.4f, 2f, 3.2f), body);

            // Painted 동-number plate high on the gable wall facing the yard.
            if (!string.IsNullOrEmpty(dongNumber))
            {
                Cube("DongPlate", slab, new Vector3(length * 0.5f + 0.30f, height - 5f, 0f), new Vector3(0.12f, 2.6f, 3.6f), Mat("M_AptSignPlate"));
                CreateSchoolyardSignText($"{name}_DongNumber", slab, dongNumber,
                    new Vector3(length * 0.5f + 0.45f, height - 5f, 0f), Quaternion.Euler(0f, 90f, 0f), 22f, new Color(0.18f, 0.22f, 0.30f));
            }
        }

        // World-space TMP sign (depth-tested Korean SDF font — mirrors the
        // recovery-room sign helper but sized for building-scale signage).
        private static void CreateSchoolyardSignText(
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
            text.rectTransform.sizeDelta = new Vector2(6f, 3f);
        }

        private static void BuildFenceRun(Transform parent, string name, Vector3 from, Vector3 to, Material fence, Material mesh)
        {
            Transform run = RootObject(name, parent, Vector3.zero).transform;
            Vector3 delta = to - from;
            float length = delta.magnitude;
            Vector3 direction = delta.normalized;
            Quaternion yaw = Quaternion.LookRotation(Vector3.Cross(direction, Vector3.up), Vector3.up);
            int segments = Mathf.Max(1, Mathf.RoundToInt(length / 4.8f));
            float segmentLength = length / segments;

            for (int i = 0; i <= segments; i++)
            {
                Vector3 postPosition = from + direction * (segmentLength * i);
                GameObject post = Cube($"Post_{i:00}", run, postPosition + new Vector3(0f, 0.7f, 0f), new Vector3(0.08f, 1.4f, 0.08f), fence);
                post.transform.localRotation = yaw;
            }

            for (int i = 0; i < segments; i++)
            {
                Vector3 center = from + direction * (segmentLength * (i + 0.5f));
                GameObject top = Cube($"RailTop_{i:00}", run, center + new Vector3(0f, 1.32f, 0f), new Vector3(segmentLength, 0.06f, 0.05f), fence);
                GameObject bottom = Cube($"RailBottom_{i:00}", run, center + new Vector3(0f, 0.15f, 0f), new Vector3(segmentLength, 0.05f, 0.05f), fence);
                GameObject panel = Cube($"MeshPanel_{i:00}", run, center + new Vector3(0f, 0.73f, 0f), new Vector3(segmentLength - 0.1f, 1.1f, 0.02f), mesh);
                Quaternion segmentYaw = Quaternion.LookRotation(Vector3.Cross(direction, Vector3.up), Vector3.up);
                top.transform.localRotation = segmentYaw;
                bottom.transform.localRotation = segmentYaw;
                panel.transform.localRotation = segmentYaw;
            }
        }

        private static void BuildSchoolyardTree(Transform parent, string name, Vector3 position, float scale)
        {
            Transform tree = RootObject(name, parent, position).transform;
            tree.localScale = Vector3.one * scale;
            Material trunk = Mat("M_TreeTrunk");
            Material leaf = Mat("M_TreeLeaf");
            Cylinder("Trunk", tree, new Vector3(0f, 1.3f, 0f), new Vector3(0.14f, 1.3f, 0.14f), Quaternion.identity, trunk);
            Sphere("CanopyMain", tree, new Vector3(0f, 3.2f, 0f), new Vector3(2.9f, 2.5f, 2.9f), leaf);
            Sphere("CanopySide", tree, new Vector3(0.9f, 2.7f, 0.4f), new Vector3(1.9f, 1.7f, 1.9f), leaf);
            Sphere("CanopyTop", tree, new Vector3(-0.5f, 3.9f, -0.4f), new Vector3(1.7f, 1.5f, 1.7f), leaf);
        }

    }
}
