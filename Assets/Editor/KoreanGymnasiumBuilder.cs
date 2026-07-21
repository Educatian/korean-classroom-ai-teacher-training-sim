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
    // Scene 5: 강당 — talent-show rehearsal (학예회 리허설) crisis training in the
    // school gymnasium/auditorium. Benchmarked on a modern Korean elementary
    // gym interior: light maple sports flooring with blue + green court line
    // markings, a very high gabled ceiling with exposed green steel trusses,
    // suspended white light fixtures and a silver vent unit, wood-tone
    // acoustic slat walls over a green/orange padded wainscot, small windows
    // on one long side, a raised stage with wooden front steps, a navy
    // pleated valance banner with gold fringe and gold school-name lettering,
    // and black speaker arrays flanking the stage.
    public static partial class KoreanClassroomBuilder
    {
        private const string GymnasiumScenePath = "Assets/Scenes/KoreanGymnasiumTraining.unity";

        // Hall footprint: 30 m wide (x), 42 m long (z). Stage fills the +z end.
        private const float GymHalfWidth = 15f;
        private const float GymHalfLength = 21f;
        private const float GymEaveHeight = 9f;
        private const float GymRidgeHeight = 12f;
        private const float GymStageFrontZ = 15f;
        private const float GymStageHeight = 1.0f;

        // Focal freezes by the east side steps, crumpling the script (beat 1:
        // 무대 옆 계단에서 대본을 구김) — standing, turned slightly away from stage.
        private static readonly Vector3 GymnasiumFocalPosition = new Vector3(9.9f, 0f, 13.4f);

        [MenuItem("Tools/Teacher Training/Build Gymnasium Scene")]
        public static void BuildGymnasiumSceneFromMenu()
        {
            BuildGymnasiumScene();
            EditorTools.MainMenuBuilder.Build();
        }

        public static void BuildGymnasiumSceneFromCommandLine()
        {
            try
            {
                BuildAll();
                BuildGymnasiumScene();
                EditorTools.MainMenuBuilder.Build();
                Debug.Log("KOREAN_GYMNASIUM_SCENE_BUILD_OK");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildGymnasiumScene()
        {
            EnsureGymnasiumMaterials();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform architecture = GameObject.Find("00_ENVIRONMENT/Architecture").transform;
            Transform furniture = GameObject.Find("00_ENVIRONMENT/Furniture").transform;
            Transform props = GameObject.Find("00_ENVIRONMENT/Props").transform;
            Transform lighting = GameObject.Find("00_ENVIRONMENT/Lighting").transform;
            Transform students = GameObject.Find("10_STUDENTS").transform;
            Transform camera = GameObject.Find("20_SYSTEMS/TeacherCamera").transform;
            SimulationController simulation = UnityEngine.Object.FindAnyObjectByType<SimulationController>();

            // 1) Hide the classroom interior; keep systems + the daylight rig.
            architecture.gameObject.SetActive(false);
            furniture.gameObject.SetActive(false);
            props.gameObject.SetActive(false);
            foreach (string legacyGroup in new[] { "CorridorFloor", "30_RECOVERY_ROOM", "40_SCHOOLYARD" })
            {
                GameObject legacy = GameObject.Find(legacyGroup);
                if (legacy != null)
                {
                    legacy.SetActive(false);
                }
            }

            // Interior fluorescents would float mid-air in the hall volume —
            // keep only the directional light, dimmed for an indoor feel (the
            // hall stays bright via emissive fixtures + warm point lights).
            foreach (Transform child in lighting.Cast<Transform>())
            {
                if (child.name != "Daylight")
                {
                    child.gameObject.SetActive(false);
                }
            }
            Light sun = lighting.Find("Daylight").GetComponent<Light>();
            sun.transform.rotation = Quaternion.Euler(55f, -32f, 0f);
            sun.intensity = 0.52f;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.65f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.54f, 0.51f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.39f, 0.34f);

            // 2) Focal stays active (container name is historical — the
            // scenario persona is Seoyun; SimulationController's focalStudent
            // reference is what matters). Six classmates wait on the audience
            // chairs; the rest deactivate. Chair placements mirror
            // BuildGymnasiumRehearsalDressing seat anchors.
            var waitingSeats = new Dictionary<string, (Vector3 position, float yaw)>
            {
                { "Classmate_Seoyeon", (GymWaitingSeat(0, 1), 0f) },
                { "Classmate_Doyun", (GymWaitingSeat(0, 4), 0f) },
                { "Classmate_Yuna", (GymWaitingSeat(1, 2), 0f) },
                { "Classmate_Junseo", (GymWaitingSeat(1, 6), 0f) },
                { "Classmate_Sua", (GymWaitingSeat(2, 3), 0f) },
                { "Classmate_Hyeonwoo", (GymWaitingSeat(2, 5), 0f) }
            };

            Transform focal = null;
            foreach (Transform student in students.Cast<Transform>())
            {
                if (student.name == "FocalStudent_Minjun")
                {
                    focal = student;
                }
                else if (waitingSeats.TryGetValue(student.name, out var seat))
                {
                    student.gameObject.SetActive(true);
                    student.SetPositionAndRotation(
                        seat.position,
                        Quaternion.Euler(0f, seat.yaw, 0f));
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

            // 3) Build the hall.
            GameObject gymRoot = new GameObject("50_GYMNASIUM");
            gymRoot.transform.position = Vector3.zero;
            BuildGymnasiumShell(gymRoot.transform);
            BuildGymnasiumCourtMarkings(gymRoot.transform);
            BuildGymnasiumRoofStructure(gymRoot.transform);
            BuildGymnasiumWallDressing(gymRoot.transform);
            BuildGymnasiumStage(gymRoot.transform);
            BuildGymnasiumRehearsalDressing(gymRoot.transform);
            BuildGymnasiumLighting(gymRoot.transform);

            // 4) Focal by the east side steps, yaw turned slightly away from
            // the stage (avoiding it), script-crumpling spot. Teacher stands
            // ~5 m away on the court at standing eye height.
            focal.SetPositionAndRotation(GymnasiumFocalPosition, Quaternion.Euler(0f, 205f, 0f));

            Vector3 teacherEye = new Vector3(6.3f, 1.62f, 10.0f);
            Vector3 focalFace = GymnasiumFocalPosition + new Vector3(0f, 1.15f, 0f);
            camera.SetPositionAndRotation(teacherEye, Quaternion.LookRotation(focalFace - teacherEye));
            Camera sceneCamera = camera.GetComponent<Camera>();
            if (sceneCamera != null)
            {
                sceneCamera.fieldOfView = 52f;
                sceneCamera.farClipPlane = 160f;
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
            simulationData.FindProperty("gymnasiumScenario").boolValue = true;
            simulationData.FindProperty("schoolyardScenario").boolValue = false;
            simulationData.FindProperty("recoveryRoomScenario").boolValue = false;
            simulationData.FindProperty("circleDiscussionScenario").boolValue = false;
            simulationData.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text title = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(label => label.name == "AppTitle");
            if (title != null)
            {
                title.text = "정서·행동 지원 교사 대응 훈련 · 강당 학예회 리허설 위기 대응";
            }

            // 6) Polish + save + register.
            KoreanClassroomVisualPolish.ApplyVisualPolish();
            EditorSceneManager.SaveScene(scene, GymnasiumScenePath);
            RegisterTrainingScenes();
            AssetDatabase.SaveAssets();
        }

        // Audience/waiting chairs: 3 rows x 8, on the near half facing the
        // stage. Row 0 is nearest the stage.
        private static Vector3 GymWaitingSeat(int row, int column)
        {
            return new Vector3(-5.25f + column * 1.5f, 0f, -5.2f - row * 1.4f);
        }

        private static void EnsureGymnasiumMaterials()
        {
            EnsureFolder(MaterialRoot);
            CreateMaterial(
                "M_GymFloorMaple",
                new Color(0.88f, 0.77f, 0.58f),
                0f,
                0.42f,
                "Assets/Art/Textures/BirchDesk_Laminate_HQ_v2.png",
                new Vector2(14f, 20f));
            CreateMaterial("M_GymLineBlue", new Color(0.16f, 0.32f, 0.62f), 0f, 0.40f);
            CreateMaterial("M_GymLineGreen", new Color(0.18f, 0.46f, 0.28f), 0f, 0.40f);
            CreateMaterial("M_GymTrussGreen", new Color(0.44f, 0.52f, 0.38f), 0.35f, 0.45f);
            CreateMaterial("M_GymRoofDeck", new Color(0.82f, 0.84f, 0.80f), 0.1f, 0.25f);
            CreateMaterial(
                "M_GymWallSlat",
                new Color(0.80f, 0.68f, 0.52f),
                0f,
                0.22f,
                "Assets/Art/Textures/BirchDesk_Laminate_HQ_v2.png",
                new Vector2(6f, 3f));
            CreateMaterial("M_GymSlatSeam", new Color(0.58f, 0.47f, 0.34f), 0f, 0.18f);
            CreateMaterial("M_GymWainscotGreen", new Color(0.42f, 0.66f, 0.30f), 0f, 0.28f);
            CreateMaterial("M_GymWainscotOrange", new Color(0.92f, 0.55f, 0.16f), 0f, 0.28f);
            CreateMaterial("M_GymTrimWhite", new Color(0.92f, 0.92f, 0.90f), 0.1f, 0.35f);
            CreateMaterial("M_GymStageWoodDark", new Color(0.42f, 0.29f, 0.18f), 0f, 0.38f);
            CreateMaterial("M_GymStageFascia", new Color(0.62f, 0.45f, 0.29f), 0f, 0.30f);
            CreateMaterial("M_GymBackPanel", new Color(0.84f, 0.74f, 0.58f), 0f, 0.24f);
            CreateMaterial("M_GymValanceNavy", new Color(0.09f, 0.13f, 0.33f), 0f, 0.34f);
            CreateMaterial("M_GymValanceNavyDeep", new Color(0.06f, 0.09f, 0.25f), 0f, 0.30f);
            CreateMaterial("M_GymGoldFringe", new Color(0.86f, 0.68f, 0.22f), 0.45f, 0.62f);
            CreateMaterial("M_GymSpeakerBlack", new Color(0.09f, 0.09f, 0.10f), 0.1f, 0.30f);
            CreateMaterial("M_GymSteelSilver", new Color(0.72f, 0.74f, 0.76f), 0.7f, 0.55f);
            CreateMaterial("M_GymWindowGlass", new Color(0.74f, 0.83f, 0.90f), 0.1f, 0.85f, null, null, false, true);
            CreateMaterial("M_GymDoorWood", new Color(0.52f, 0.38f, 0.26f), 0f, 0.30f);
            CreateMaterial("M_GymMatBlue", new Color(0.22f, 0.36f, 0.62f), 0f, 0.18f);
            CreateMaterial("M_GymMatGreen", new Color(0.30f, 0.52f, 0.34f), 0f, 0.18f);
            CreateMaterial("M_GymPianoBlack", new Color(0.06f, 0.06f, 0.07f), 0.2f, 0.72f);
            CreateMaterial("M_GymLightEmissive", new Color(1f, 0.97f, 0.90f), 0f, 0.4f, null, null, false, true);
            AssetDatabase.SaveAssets();
        }

        // Floor slab, perimeter walls to eave height, stepped gable tops on
        // both end walls, and the two sloped roof deck slabs.
        private static void BuildGymnasiumShell(Transform root)
        {
            Transform shell = RootObject("Shell", root, Vector3.zero).transform;
            Material floor = Mat("M_GymFloorMaple");
            Material slat = Mat("M_GymWallSlat");
            Material deck = Mat("M_GymRoofDeck");

            Cube("MapleFloor", shell, new Vector3(0f, -0.06f, 0f), new Vector3(GymHalfWidth * 2f, 0.12f, GymHalfLength * 2f), floor);

            // Side walls (x = ±15) to eave height. Colliders bound free roam.
            Cube("WallWest", shell, new Vector3(-GymHalfWidth, GymEaveHeight * 0.5f, 0f), new Vector3(0.2f, GymEaveHeight, GymHalfLength * 2f), slat);
            Cube("WallEast", shell, new Vector3(GymHalfWidth, GymEaveHeight * 0.5f, 0f), new Vector3(0.2f, GymEaveHeight, GymHalfLength * 2f), slat);

            // End walls (z = ±21) + stepped gable triangles up to the ridge.
            foreach (int sign in new[] { -1, 1 })
            {
                string tag = sign < 0 ? "South" : "North";
                float z = sign * GymHalfLength;
                Cube($"Wall{tag}", shell, new Vector3(0f, GymEaveHeight * 0.5f, z), new Vector3(GymHalfWidth * 2f, GymEaveHeight, 0.2f), slat);
                for (int step = 0; step < 4; step++)
                {
                    float midHeight = GymEaveHeight + 0.375f + step * 0.75f;
                    float width = GymHalfWidth * 2f * (1f - (0.375f + step * 0.75f) / (GymRidgeHeight - GymEaveHeight));
                    Cube($"Gable{tag}_{step}", shell, new Vector3(0f, midHeight, z), new Vector3(width, 0.75f, 0.2f), slat);
                }
            }

            // Sloped roof decks: eave (±15, 9) up to ridge (0, 12).
            float slopeAngle = Mathf.Atan2(GymRidgeHeight - GymEaveHeight, GymHalfWidth) * Mathf.Rad2Deg;
            GameObject deckWest = Cube("RoofDeckWest", shell, new Vector3(-GymHalfWidth * 0.5f, (GymEaveHeight + GymRidgeHeight) * 0.5f + 0.10f, 0f), new Vector3(15.65f, 0.16f, GymHalfLength * 2f + 0.4f), deck);
            deckWest.transform.localRotation = Quaternion.Euler(0f, 0f, slopeAngle);
            GameObject deckEast = Cube("RoofDeckEast", shell, new Vector3(GymHalfWidth * 0.5f, (GymEaveHeight + GymRidgeHeight) * 0.5f + 0.10f, 0f), new Vector3(15.65f, 0.16f, GymHalfLength * 2f + 0.4f), deck);
            deckEast.transform.localRotation = Quaternion.Euler(0f, 0f, -slopeAngle);

            // Eave closure beams: seal the daylight slit between the wall
            // tops and the deck undersides (visible as sky wedges in the
            // first play-mode captures).
            Cube("EaveBeamWest", shell, new Vector3(-GymHalfWidth + 0.2f, 9.15f, 0f), new Vector3(0.7f, 0.7f, GymHalfLength * 2f + 0.4f), deck);
            Cube("EaveBeamEast", shell, new Vector3(GymHalfWidth - 0.2f, 9.15f, 0f), new Vector3(0.7f, 0.7f, GymHalfLength * 2f + 0.4f), deck);

            // Verge closure boards along the gable rooflines: cover the
            // sawtooth gaps between the stepped gable tops and the decks.
            foreach (int sign in new[] { -1, 1 })
            {
                string tag = sign < 0 ? "South" : "North";
                float z = sign * (GymHalfLength - 0.25f);
                GameObject vergeWest = Cube($"Verge{tag}West", shell, new Vector3(-GymHalfWidth * 0.5f, (GymEaveHeight + GymRidgeHeight) * 0.5f - 0.15f, z), new Vector3(15.6f, 1.1f, 0.45f), deck);
                vergeWest.transform.localRotation = Quaternion.Euler(0f, 0f, slopeAngle);
                GameObject vergeEast = Cube($"Verge{tag}East", shell, new Vector3(GymHalfWidth * 0.5f, (GymEaveHeight + GymRidgeHeight) * 0.5f - 0.15f, z), new Vector3(15.6f, 1.1f, 0.45f), deck);
                vergeEast.transform.localRotation = Quaternion.Euler(0f, 0f, -slopeAngle);
            }
        }

        // Court markings on the maple: blue sidelines + center circle, green
        // boundary + keys — thin raised cubes like the schoolyard lines.
        private static void BuildGymnasiumCourtMarkings(Transform root)
        {
            Transform markings = RootObject("CourtMarkings", root, Vector3.zero).transform;
            Material blue = Mat("M_GymLineBlue");
            Material green = Mat("M_GymLineGreen");
            const float lineY = 0.005f;
            const float lineH = 0.01f;
            const float lineW = 0.08f;
            const float courtCenterZ = -2f;

            // Blue outer boundary (sidelines + baselines), 14.4 x 26.
            Cube("BlueSideWest", markings, new Vector3(-7.2f, lineY, courtCenterZ), new Vector3(lineW, lineH, 26f), blue);
            Cube("BlueSideEast", markings, new Vector3(7.2f, lineY, courtCenterZ), new Vector3(lineW, lineH, 26f), blue);
            Cube("BlueBaseNorth", markings, new Vector3(0f, lineY, courtCenterZ + 13f), new Vector3(14.48f, lineH, lineW), blue);
            Cube("BlueBaseSouth", markings, new Vector3(0f, lineY, courtCenterZ - 13f), new Vector3(14.48f, lineH, lineW), blue);
            Cube("BlueHalfLine", markings, new Vector3(0f, lineY, courtCenterZ), new Vector3(14.48f, lineH, lineW), blue);

            // Blue center circle (radius 1.8) from short rotated segments.
            const int circleSegments = 26;
            const float circleRadius = 1.8f;
            float segmentLength = 2f * Mathf.PI * circleRadius / circleSegments + 0.02f;
            for (int i = 0; i < circleSegments; i++)
            {
                float angle = i * (360f / circleSegments);
                float rad = angle * Mathf.Deg2Rad;
                GameObject segment = Cube($"CenterCircle_{i:00}", markings,
                    new Vector3(Mathf.Cos(rad) * circleRadius, lineY, courtCenterZ + Mathf.Sin(rad) * circleRadius),
                    new Vector3(segmentLength, lineH, lineW), blue);
                segment.transform.localRotation = Quaternion.Euler(0f, -angle + 90f, 0f);
            }

            // Green inner boundary, 12 x 22, with green keys at both ends.
            Cube("GreenSideWest", markings, new Vector3(-6f, lineY, courtCenterZ), new Vector3(lineW, lineH, 22f), green);
            Cube("GreenSideEast", markings, new Vector3(6f, lineY, courtCenterZ), new Vector3(lineW, lineH, 22f), green);
            Cube("GreenBaseNorth", markings, new Vector3(0f, lineY, courtCenterZ + 11f), new Vector3(12.08f, lineH, lineW), green);
            Cube("GreenBaseSouth", markings, new Vector3(0f, lineY, courtCenterZ - 11f), new Vector3(12.08f, lineH, lineW), green);
            foreach (int sign in new[] { -1, 1 })
            {
                string tag = sign < 0 ? "South" : "North";
                float baseZ = courtCenterZ + sign * 11f;
                float frontZ = courtCenterZ + sign * 7f;
                Cube($"KeySideW{tag}", markings, new Vector3(-1.8f, lineY, (baseZ + frontZ) * 0.5f), new Vector3(lineW, lineH, 4f), green);
                Cube($"KeySideE{tag}", markings, new Vector3(1.8f, lineY, (baseZ + frontZ) * 0.5f), new Vector3(lineW, lineH, 4f), green);
                Cube($"KeyFront{tag}", markings, new Vector3(0f, lineY, frontZ), new Vector3(3.68f, lineH, lineW), green);
            }
        }

        // Exposed green steel trusses (7 bays), suspended white light discs,
        // and the big silver ceiling vent unit.
        private static void BuildGymnasiumRoofStructure(Transform root)
        {
            Transform structure = RootObject("RoofStructure", root, Vector3.zero).transform;
            for (int bay = 0; bay < 7; bay++)
            {
                BuildGymTrussBay(structure, -18f + bay * 6f);
            }

            // Suspended white light fixtures: emissive discs on drop rods.
            Transform fixtures = RootObject("HangingLights", structure, Vector3.zero).transform;
            Material steel = Mat("M_GymSteelSilver");
            Material glow = Mat("M_GymLightEmissive");
            foreach (float z in new[] { -13.5f, -4.5f, 4.5f, 13.5f })
            {
                foreach (float x in new[] { -6.5f, 6.5f })
                {
                    Transform lamp = RootObject($"HangingLight_{x:0}_{z:0}", fixtures, new Vector3(x, 0f, z)).transform;
                    Cylinder("DropRod", lamp, new Vector3(0f, 8.15f, 0f), new Vector3(0.04f, 0.55f, 0.04f), Quaternion.identity, steel);
                    Cylinder("Shade", lamp, new Vector3(0f, 7.56f, 0f), new Vector3(0.78f, 0.10f, 0.78f), Quaternion.identity, Mat("M_GymTrimWhite"));
                    Cylinder("Diffuser", lamp, new Vector3(0f, 7.47f, 0f), new Vector3(0.62f, 0.035f, 0.62f), Quaternion.identity, glow);
                }
            }

            // Silver vent unit hanging near the south end (benchmark photo).
            Transform vent = RootObject("CeilingVent", structure, new Vector3(-6f, 0f, -16.5f)).transform;
            Cylinder("VentRodA", vent, new Vector3(-0.6f, 8.6f, 0f), new Vector3(0.05f, 0.6f, 0.05f), Quaternion.identity, steel);
            Cylinder("VentRodB", vent, new Vector3(0.6f, 8.6f, 0f), new Vector3(0.05f, 0.6f, 0.05f), Quaternion.identity, steel);
            Cube("VentBody", vent, new Vector3(0f, 7.55f, 0f), new Vector3(1.7f, 0.95f, 1.7f), steel);
            Cube("VentSkirt", vent, new Vector3(0f, 6.98f, 0f), new Vector3(2.1f, 0.22f, 2.1f), steel);
        }

        // One triangulated truss frame: horizontal bottom chord, sloped top
        // chords following the roof pitch, verticals + diagonals between them.
        private static void BuildGymTrussBay(Transform parent, float z)
        {
            Transform truss = RootObject($"Truss_{z:0}", parent, new Vector3(0f, 0f, z)).transform;
            Material green = Mat("M_GymTrussGreen");
            const float bottomY = 8.6f;
            const float apexY = 11.85f;
            const float halfSpan = 14.4f;
            float slope = (apexY - GymEaveHeight * 0.997f) / halfSpan; // ~0.2 m per m

            float RoofY(float x) => apexY - Mathf.Abs(x) * slope;

            Cube("BottomChord", truss, new Vector3(0f, bottomY, 0f), new Vector3(halfSpan * 2f, 0.16f, 0.16f), green);
            float chordLength = Mathf.Sqrt(halfSpan * halfSpan + (apexY - RoofY(halfSpan)) * (apexY - RoofY(halfSpan))) + 0.3f;
            float chordAngle = Mathf.Atan2(apexY - RoofY(halfSpan), halfSpan) * Mathf.Rad2Deg;
            GameObject chordWest = Cube("TopChordWest", truss, new Vector3(-halfSpan * 0.5f, (apexY + RoofY(halfSpan)) * 0.5f, 0f), new Vector3(chordLength, 0.16f, 0.16f), green);
            chordWest.transform.localRotation = Quaternion.Euler(0f, 0f, chordAngle);
            GameObject chordEast = Cube("TopChordEast", truss, new Vector3(halfSpan * 0.5f, (apexY + RoofY(halfSpan)) * 0.5f, 0f), new Vector3(chordLength, 0.16f, 0.16f), green);
            chordEast.transform.localRotation = Quaternion.Euler(0f, 0f, -chordAngle);

            // Verticals every 3 m + diagonals leaning toward the apex.
            for (float x = -12f; x <= 12.01f; x += 3f)
            {
                float height = RoofY(x) - bottomY;
                Cube($"Web_V_{x:0}", truss, new Vector3(x, bottomY + height * 0.5f, 0f), new Vector3(0.10f, height, 0.10f), green);
                if (Mathf.Abs(x) > 0.1f)
                {
                    float xTop = x - Mathf.Sign(x) * 3f;
                    Vector3 a = new Vector3(x, bottomY, 0f);
                    Vector3 b = new Vector3(xTop, RoofY(xTop), 0f);
                    Vector3 mid = (a + b) * 0.5f;
                    float length = Vector3.Distance(a, b);
                    float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                    GameObject diagonal = Cube($"Web_D_{x:0}", truss, mid, new Vector3(length, 0.08f, 0.08f), green);
                    diagonal.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }

        // Slat seams, padded wainscot, high windows (west side), doors with
        // kick plates on both ends, and wall-mounted speakers.
        private static void BuildGymnasiumWallDressing(Transform root)
        {
            Transform dressing = RootObject("WallDressing", root, Vector3.zero).transform;
            Material seam = Mat("M_GymSlatSeam");
            Material trim = Mat("M_GymTrimWhite");
            Material glass = Mat("M_GymWindowGlass");
            Material doorWood = Mat("M_GymDoorWood");
            Material kick = Mat("M_GymSteelSilver");
            Material speaker = Mat("M_GymSpeakerBlack");

            // Vertical slat seams on the long walls (acoustic panel joints).
            // Spacing matches the west window rhythm so seams land between
            // window frames; south seams skip the door bays.
            Transform seams = RootObject("SlatSeams", dressing, Vector3.zero).transform;
            for (float z = -17.8f; z <= 17.5f; z += 3.2f)
            {
                Cube($"SeamWest_{z:0}", seams, new Vector3(-GymHalfWidth + 0.11f, 5.1f, z), new Vector3(0.03f, 7.7f, 0.09f), seam);
                Cube($"SeamEast_{z:0}", seams, new Vector3(GymHalfWidth - 0.11f, 5.1f, z), new Vector3(0.03f, 7.7f, 0.09f), seam);
            }
            for (float x = -12.6f; x <= 12.7f; x += 2.4f)
            {
                if (Mathf.Abs(Mathf.Abs(x) - 8f) < 1.4f)
                {
                    continue;
                }
                Cube($"SeamSouth_{x:0}", seams, new Vector3(x, 5.1f, -GymHalfLength + 0.11f), new Vector3(0.09f, 7.7f, 0.03f), seam);
            }

            // Padded wainscot: 1.2 m alternating green/orange panels + white
            // top rail, on both long walls and the south end wall. Panels and
            // rails skip the door bays (side doors z 12.6, south doors x ±8)
            // and stop short of the stage platform.
            Transform wainscot = RootObject("Wainscot", dressing, Vector3.zero).transform;
            int panel = 0;
            for (float z = -20.25f; z <= 14.5f; z += 1.5f, panel++)
            {
                if (Mathf.Abs(z - 12.6f) < 1.35f)
                {
                    continue;
                }
                Material pad = panel % 4 == 0 ? Mat("M_GymWainscotOrange") : Mat("M_GymWainscotGreen");
                Cube($"PadWest_{panel:00}", wainscot, new Vector3(-GymHalfWidth + 0.14f, 0.6f, z), new Vector3(0.08f, 1.2f, 1.42f), pad);
                Cube($"PadEast_{panel:00}", wainscot, new Vector3(GymHalfWidth - 0.14f, 0.6f, z), new Vector3(0.08f, 1.2f, 1.42f), pad);
            }
            panel = 1;
            for (float x = -14.25f; x <= 14.3f; x += 1.5f, panel++)
            {
                if (Mathf.Abs(Mathf.Abs(x) - 8f) < 1.85f)
                {
                    continue;
                }
                Material pad = panel % 4 == 0 ? Mat("M_GymWainscotOrange") : Mat("M_GymWainscotGreen");
                Cube($"PadSouth_{panel:00}", wainscot, new Vector3(x, 0.6f, -GymHalfLength + 0.14f), new Vector3(1.42f, 1.2f, 0.08f), pad);
            }
            foreach (float x in new[] { -GymHalfWidth + 0.15f, GymHalfWidth - 0.15f })
            {
                string tag = x < 0f ? "West" : "East";
                Cube($"Rail{tag}A", wainscot, new Vector3(x, 1.26f, -4.6f), new Vector3(0.09f, 0.08f, 32.8f), trim);
                Cube($"Rail{tag}B", wainscot, new Vector3(x, 1.26f, 17.2f), new Vector3(0.09f, 0.08f, 7.6f), trim);
            }
            Cube("RailSouthWest", wainscot, new Vector3(-11.6f, 1.26f, -GymHalfLength + 0.15f), new Vector3(4.9f, 0.08f, 0.09f), trim);
            Cube("RailSouthCenter", wainscot, new Vector3(0f, 1.26f, -GymHalfLength + 0.15f), new Vector3(13.7f, 0.08f, 0.09f), trim);
            Cube("RailSouthEast", wainscot, new Vector3(11.6f, 1.26f, -GymHalfLength + 0.15f), new Vector3(4.9f, 0.08f, 0.09f), trim);

            // Row of small windows above the wainscot on the west long side.
            Transform windows = RootObject("WestWindows", dressing, Vector3.zero).transform;
            for (int i = 0; i < 6; i++)
            {
                float z = -16f + i * 3.2f;
                Transform window = RootObject($"Window_{i:00}", windows, new Vector3(-GymHalfWidth + 0.09f, 2.15f, z)).transform;
                Cube("Frame", window, Vector3.zero, new Vector3(0.06f, 1.15f, 1.65f), trim);
                Cube("Glass", window, new Vector3(0.02f, 0f, 0f), new Vector3(0.05f, 1.0f, 1.5f), glass);
                Cube("MullionV", window, new Vector3(0.03f, 0f, 0f), new Vector3(0.05f, 1.05f, 0.05f), trim);
                Cube("MullionH", window, new Vector3(0.03f, 0f, 0f), new Vector3(0.05f, 0.05f, 1.55f), trim);
            }

            // Clerestory band just under the eaves on both long walls: the
            // continuous high daylight strip typical of Korean school gyms.
            Transform clerestory = RootObject("Clerestory", dressing, Vector3.zero).transform;
            for (int side = 0; side < 2; side++)
            {
                float x = (side == 0 ? -1f : 1f) * (GymHalfWidth - 0.09f);
                float inward = side == 0 ? 0.02f : -0.02f;
                for (int i = 0; i < 10; i++)
                {
                    float z = -18f + i * 4f;
                    Transform pane = RootObject($"Clerestory_{(side == 0 ? "W" : "E")}_{i:00}", clerestory,
                        new Vector3(x, GymEaveHeight - 1.35f, z)).transform;
                    Cube("Frame", pane, Vector3.zero, new Vector3(0.06f, 1.5f, 3.7f), trim);
                    Cube("Glass", pane, new Vector3(inward, 0f, 0f), new Vector3(0.05f, 1.34f, 3.54f), glass);
                    Cube("MullionV1", pane, new Vector3(inward * 1.5f, 0f, -1.18f), new Vector3(0.05f, 1.4f, 0.05f), trim);
                    Cube("MullionV2", pane, new Vector3(inward * 1.5f, 0f, 1.18f), new Vector3(0.05f, 1.4f, 0.05f), trim);
                }
            }

            // Doors with steel kick plates: double doors on the south end,
            // single doors on both long walls near the stage.
            Transform doors = RootObject("Doors", dressing, Vector3.zero).transform;
            foreach (float x in new[] { -8f, 8f })
            {
                Transform pair = RootObject($"SouthDoors_{x:0}", doors, new Vector3(x, 0f, -GymHalfLength + 0.12f)).transform;
                foreach (float leaf in new[] { -0.5f, 0.5f })
                {
                    Cube($"Leaf_{leaf:0.0}", pair, new Vector3(leaf, 1.05f, 0f), new Vector3(0.96f, 2.1f, 0.09f), doorWood);
                    Cube($"Kick_{leaf:0.0}", pair, new Vector3(leaf, 0.17f, -0.052f), new Vector3(0.88f, 0.3f, 0.02f), kick);
                    Sphere($"Handle_{leaf:0.0}", pair, new Vector3(leaf - Mathf.Sign(leaf) * 0.36f, 1.02f, -0.08f), new Vector3(0.09f, 0.09f, 0.09f), kick);
                }
                Cube("Header", pair, new Vector3(0f, 2.22f, 0f), new Vector3(2.1f, 0.14f, 0.12f), trim);
            }
            foreach (float x in new[] { -GymHalfWidth + 0.12f, GymHalfWidth - 0.12f })
            {
                float inward = x < 0f ? 1f : -1f;
                Transform side = RootObject($"StageSideDoor_{x:0}", doors, new Vector3(x, 0f, 12.6f)).transform;
                Cube("Leaf", side, new Vector3(0f, 1.05f, 0f), new Vector3(0.09f, 2.1f, 0.96f), doorWood);
                Cube("Kick", side, new Vector3(inward * 0.052f, 0.17f, 0f), new Vector3(0.02f, 0.3f, 0.88f), kick);
                Sphere("Handle", side, new Vector3(inward * 0.08f, 1.02f, 0.36f), new Vector3(0.09f, 0.09f, 0.09f), kick);
            }

            // Speaker arrays flanking the stage (stacked boxes, tilted down)
            // + two side wall speakers mid-hall.
            Transform speakers = RootObject("Speakers", dressing, Vector3.zero).transform;
            foreach (float x in new[] { -13.1f, 13.1f })
            {
                Transform array = RootObject($"StageArray_{x:0}", speakers, new Vector3(x, 0f, 14.35f)).transform;
                Cube("Bracket", array, new Vector3(0f, 7.35f, 0.22f), new Vector3(0.34f, 0.10f, 0.6f), Mat("M_GymSteelSilver"));
                for (int box = 0; box < 3; box++)
                {
                    GameObject unit = Cube($"Box_{box}", array,
                        new Vector3(0f, 7.0f - box * 0.42f, 0.05f + box * 0.06f),
                        new Vector3(0.58f, 0.40f, 0.5f), speaker);
                    unit.transform.localRotation = Quaternion.Euler(-6f - box * 6f, 0f, 0f);
                }
            }
            foreach (float x in new[] { -GymHalfWidth + 0.32f, GymHalfWidth - 0.32f })
            {
                GameObject wallBox = Cube($"WallSpeaker_{x:0}", speakers, new Vector3(x, 5.2f, -6f), new Vector3(0.42f, 0.62f, 0.38f), speaker);
                wallBox.transform.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -8f : 8f);
            }
        }

        // Raised stage across the +z end: platform, fascia, steps, back
        // panel, navy pleated valance with gold fringe + school-name TMP
        // lettering, side curtains, and a standing microphone.
        private static void BuildGymnasiumStage(Transform root)
        {
            Transform stage = RootObject("Stage", root, Vector3.zero).transform;
            Material stageWood = Mat("M_GymStageWoodDark");
            Material fascia = Mat("M_GymStageFascia");
            Material navy = Mat("M_GymValanceNavy");
            Material navyDeep = Mat("M_GymValanceNavyDeep");
            Material gold = Mat("M_GymGoldFringe");

            float stageCenterZ = (GymStageFrontZ + GymHalfLength) * 0.5f;
            Cube("Platform", stage, new Vector3(0f, GymStageHeight * 0.5f, stageCenterZ), new Vector3(GymHalfWidth * 2f - 0.4f, GymStageHeight, GymHalfLength - GymStageFrontZ), stageWood);
            Cube("FrontFascia", stage, new Vector3(0f, GymStageHeight * 0.5f, GymStageFrontZ - 0.05f), new Vector3(GymHalfWidth * 2f - 0.4f, GymStageHeight, 0.1f), fascia);
            Cube("StageNosing", stage, new Vector3(0f, GymStageHeight + 0.02f, GymStageFrontZ + 0.04f), new Vector3(GymHalfWidth * 2f - 0.4f, 0.04f, 0.12f), fascia);

            // Back wall panel behind the stage (light wood, photo look) with
            // vertical panel seams so it does not read as one blank slab.
            Cube("BackPanel", stage, new Vector3(0f, 4.6f, GymHalfLength - 0.18f), new Vector3(GymHalfWidth * 2f - 1.2f, 7.2f, 0.12f), Mat("M_GymBackPanel"));
            for (float seamX = -12.1f; seamX <= 12.2f; seamX += 2.2f)
            {
                Cube($"BackPanelSeam_{seamX:0.0}", stage, new Vector3(seamX, 4.6f, GymHalfLength - 0.25f), new Vector3(0.06f, 7.0f, 0.03f), Mat("M_GymSlatSeam"));
            }
            Cube("BackPanelBase", stage, new Vector3(0f, 1.35f, GymHalfLength - 0.25f), new Vector3(GymHalfWidth * 2f - 1.2f, 0.7f, 0.04f), Mat("M_GymStageFascia"));

            // Center step unit (3 treads) + narrower side step units.
            BuildGymStepUnit(stage, "CenterSteps", 0f, 1.8f);
            BuildGymStepUnit(stage, "SideStepsWest", -11f, 1.2f);
            BuildGymStepUnit(stage, "SideStepsEast", 11f, 1.2f);

            // Navy valance banner across the stage top: straight header band,
            // pleated skirt from alternating yaw-angled panels, gold fringe.
            Transform valance = RootObject("ValanceBanner", stage, new Vector3(0f, 0f, GymStageFrontZ - 0.12f)).transform;
            Cube("HeaderBand", valance, new Vector3(0f, 8.1f, 0.02f), new Vector3(28.6f, 0.42f, 0.06f), navyDeep);
            const int pleats = 38;
            const float pleatWidth = 28.4f / pleats;
            for (int i = 0; i < pleats; i++)
            {
                float x = -14.2f + pleatWidth * (i + 0.5f);
                GameObject pleat = Cube($"Pleat_{i:00}", valance,
                    new Vector3(x, 7.15f, i % 2 == 0 ? 0f : 0.045f),
                    new Vector3(pleatWidth + 0.10f, 1.55f, 0.03f),
                    i % 2 == 0 ? navy : navyDeep);
                pleat.transform.localRotation = Quaternion.Euler(0f, i % 2 == 0 ? 9f : -9f, 0f);
            }
            Cube("FringeStrip", valance, new Vector3(0f, 6.32f, 0.02f), new Vector3(28.5f, 0.14f, 0.05f), gold);

            // Gold school-name lettering, centered, hall-facing.
            CreateGymnasiumSignText("SchoolNameBanner", valance, "한울초등학교 학예회",
                new Vector3(0f, 7.30f, -0.16f), Quaternion.identity, 14f, new Color(0.93f, 0.78f, 0.34f));

            // Navy side curtains framing the stage opening.
            foreach (float x in new[] { -13.6f, 13.6f })
            {
                Transform curtain = RootObject($"SideCurtain_{x:0}", stage, new Vector3(x, 0f, GymStageFrontZ + 0.35f)).transform;
                Cube("Main", curtain, new Vector3(0f, 3.7f, 0f), new Vector3(2.2f, 5.4f, 0.14f), navy);
                GameObject foldA = Cube("FoldA", curtain, new Vector3(-0.7f, 3.7f, 0.09f), new Vector3(0.7f, 5.4f, 0.07f), navyDeep);
                foldA.transform.localRotation = Quaternion.Euler(0f, 14f, 0f);
                GameObject foldB = Cube("FoldB", curtain, new Vector3(0.7f, 3.7f, 0.09f), new Vector3(0.7f, 5.4f, 0.07f), navyDeep);
                foldB.transform.localRotation = Quaternion.Euler(0f, -14f, 0f);
            }

            // Standing microphone center stage.
            Transform microphone = RootObject("StandingMic", stage, new Vector3(0f, GymStageHeight, 17.2f)).transform;
            Material steel = Mat("M_GymSteelSilver");
            Cylinder("MicBase", microphone, new Vector3(0f, 0.02f, 0f), new Vector3(0.34f, 0.02f, 0.34f), Quaternion.identity, steel);
            Cylinder("MicPole", microphone, new Vector3(0f, 0.72f, 0f), new Vector3(0.03f, 0.70f, 0.03f), Quaternion.identity, steel);
            Sphere("MicHead", microphone, new Vector3(0f, 1.46f, 0f), new Vector3(0.12f, 0.14f, 0.12f), Mat("M_GymSpeakerBlack"));
        }

        // Wooden step unit against the stage front (treads descending toward
        // the hall). Width varies (wide center unit, narrow side units).
        private static void BuildGymStepUnit(Transform stage, string name, float x, float width)
        {
            Transform steps = RootObject(name, stage, new Vector3(x, 0f, GymStageFrontZ)).transform;
            Material fascia = Mat("M_GymStageFascia");
            for (int tread = 0; tread < 3; tread++)
            {
                float height = GymStageHeight - tread * 0.33f;
                Cube($"Tread_{tread}", steps,
                    new Vector3(0f, height * 0.5f, -0.20f - tread * 0.36f),
                    new Vector3(width, height, 0.36f), fascia);
            }
        }

        // Waiting chairs, folding prop table, gym mats, and an upright-piano
        // block at stage side — the rehearsal dressing.
        private static void BuildGymnasiumRehearsalDressing(Transform root)
        {
            Transform dressing = RootObject("RehearsalDressing", root, Vector3.zero).transform;
            Material chairPlastic = Mat("M_ChairPlastic");
            Material metal = Mat("M_Metal");
            Material trimWood = Mat("M_TrimWood");

            // 3 rows x 8 small waiting chairs facing the stage.
            Transform chairs = RootObject("WaitingChairs", dressing, Vector3.zero).transform;
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 8; column++)
                {
                    Vector3 seat = GymWaitingSeat(row, column);
                    Transform chair = RootObject($"Chair_{row}{column}", chairs, seat).transform;
                    Cube("Seat", chair, new Vector3(0f, 0.30f, 0f), new Vector3(0.38f, 0.05f, 0.38f), chairPlastic);
                    Cube("Back", chair, new Vector3(0f, 0.55f, -0.19f), new Vector3(0.38f, 0.45f, 0.05f), chairPlastic);
                    Cube("Legs", chair, new Vector3(0f, 0.14f, 0f), new Vector3(0.32f, 0.28f, 0.32f), metal);
                }
            }

            // Folding table with rehearsal props (west of the chairs).
            Transform table = RootObject("PropTable", dressing, new Vector3(-9f, 0f, -3.5f)).transform;
            Cube("Top", table, new Vector3(0f, 0.72f, 0f), new Vector3(1.8f, 0.05f, 0.75f), Mat("M_GymTrimWhite"));
            Cube("LegWest", table, new Vector3(-0.8f, 0.36f, 0f), new Vector3(0.05f, 0.72f, 0.6f), metal);
            Cube("LegEast", table, new Vector3(0.8f, 0.36f, 0f), new Vector3(0.05f, 0.72f, 0.6f), metal);
            Cube("PropBoxRed", table, new Vector3(-0.5f, 0.83f, 0.1f), new Vector3(0.34f, 0.16f, 0.26f), Mat("M_RecoveryCard_Red"));
            Cube("PropBoxBlue", table, new Vector3(0.15f, 0.80f, -0.14f), new Vector3(0.28f, 0.11f, 0.22f), Mat("M_GymMatBlue"));
            Cylinder("Tambourine", table, new Vector3(0.62f, 0.77f, 0.12f), new Vector3(0.24f, 0.02f, 0.24f), Quaternion.identity, trimWood);

            // Rolled gym mat + two flat blue mats along the east wall.
            Transform mats = RootObject("GymMats", dressing, new Vector3(13.4f, 0f, -12f)).transform;
            Cylinder("RolledMat", mats, new Vector3(0f, 0.38f, 0f), new Vector3(0.76f, 0.9f, 0.76f), Quaternion.Euler(90f, 0f, 0f), Mat("M_GymMatGreen"));
            Cube("FlatMatA", mats, new Vector3(-0.1f, 0.03f, -2.4f), new Vector3(1.0f, 0.06f, 1.9f), Mat("M_GymMatBlue"));
            Cube("FlatMatB", mats, new Vector3(-0.1f, 0.09f, -2.5f), new Vector3(1.0f, 0.06f, 1.9f), Mat("M_GymMatBlue"));

            // Upright-piano block at the west stage side, keyboard toward the hall.
            Transform piano = RootObject("Piano", dressing, new Vector3(-11.6f, 0f, 13.0f)).transform;
            piano.localRotation = Quaternion.Euler(0f, 24f, 0f);
            Material black = Mat("M_GymPianoBlack");
            Cube("Body", piano, new Vector3(0f, 0.62f, 0.1f), new Vector3(1.45f, 1.24f, 0.42f), black);
            Cube("KeyBed", piano, new Vector3(0f, 0.74f, -0.22f), new Vector3(1.38f, 0.07f, 0.28f), black);
            Cube("Keys", piano, new Vector3(0f, 0.783f, -0.24f), new Vector3(1.24f, 0.015f, 0.20f), Mat("M_GymTrimWhite"));
            Cube("Bench", piano, new Vector3(0f, 0.26f, -0.72f), new Vector3(0.8f, 0.06f, 0.34f), black);
            Cube("BenchLegs", piano, new Vector3(0f, 0.12f, -0.72f), new Vector3(0.7f, 0.22f, 0.26f), black);
        }

        // Warm-white point lights under the trusses keep the hall bright
        // without the interior fluorescent rig.
        private static void BuildGymnasiumLighting(Transform root)
        {
            Transform lights = RootObject("GymLights", root, Vector3.zero).transform;
            foreach (float z in new[] { -13.5f, -4.5f, 4.5f, 13.5f })
            {
                foreach (float x in new[] { -6.5f, 6.5f })
                {
                    GameObject lightObject = new GameObject($"GymPoint_{x:0}_{z:0}");
                    lightObject.transform.SetParent(lights, false);
                    lightObject.transform.localPosition = new Vector3(x, 7.2f, z);
                    Light point = lightObject.AddComponent<Light>();
                    point.type = LightType.Point;
                    point.range = 16f;
                    point.intensity = 0.85f;
                    point.color = new Color(1f, 0.96f, 0.88f);
                    point.shadows = LightShadows.None;
                }
            }

            // Soft stage wash so the valance + back panel read clearly.
            GameObject stageWash = new GameObject("StageWash");
            stageWash.transform.SetParent(lights, false);
            stageWash.transform.localPosition = new Vector3(0f, 6.4f, 12.5f);
            Light wash = stageWash.AddComponent<Light>();
            wash.type = LightType.Point;
            wash.range = 15f;
            wash.intensity = 0.75f;
            wash.color = new Color(1f, 0.95f, 0.85f);
            wash.shadows = LightShadows.None;
        }

        // World-space TMP sign (depth-tested Korean SDF font — mirrors the
        // recovery/schoolyard sign helpers, sized for the stage banner).
        private static void CreateGymnasiumSignText(
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
            text.rectTransform.sizeDelta = new Vector2(24f, 2.4f);
        }
    }
}
