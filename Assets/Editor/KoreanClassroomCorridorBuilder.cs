using UnityEditor;
using UnityEngine;

namespace AdieLab.TeacherTraining.Editor
{
    // Corridor floor extension: hallway (복도) along z +5..+8.2, two neighbor
    // classrooms at x offsets -14.16/+14.16, corridor props, and stair-well end
    // caps so free roam never reaches floating void. Called from
    // BuildArchitecture, so both the general scene and the circle-discussion
    // scene (which reopens the general scene) include it.
    public static partial class KoreanClassroomBuilder
    {
        private const float CorridorOuterZ = 8.2f;   // centreline of the outer (courtyard) wall
        private const float CorridorCenterZ = 6.64f; // corridor mid-line between wall faces
        private const float CorridorEndX = 21.3f;    // centreline of the stair-well end caps
        private const float NeighborOffsetX = 14.16f;

        private static void BuildCorridorFloor(Transform parent)
        {
            Transform root = RootObject("CorridorFloor", parent, Vector3.zero).transform;

            BuildCorridorShell(root);
            BuildCorridorOuterWall(root);
            BuildCorridorEndCaps(root);
            BuildCorridorLighting(root);
            BuildNeighborClassroom(root, -NeighborOffsetX, "1학년 1반");
            BuildNeighborClassroom(root, NeighborOffsetX, "1학년 3반");
            BuildCorridorProps(root);
        }

        private static void BuildCorridorShell(Transform root)
        {
            Material floor = Mat("M_CorridorFloor");
            Material ceiling = Mat("M_Ceiling");
            Material baseboard = Mat("M_Baseboard");
            Material wall = Mat("M_Wall");

            Cube("CorridorSlab", root, new Vector3(0f, -0.06f, CorridorCenterZ), new Vector3(42.76f, 0.12f, 3.28f), floor);
            Cube("CorridorCeiling", root, new Vector3(0f, 3.42f, CorridorCenterZ), new Vector3(42.76f, 0.16f, 3.28f), ceiling);

            // Pilasters fill the 0.16 m seams where the classroom party walls
            // meet the corridor-side wall plane at x = +-7.
            Cube("CorridorPilasterWest", root, new Vector3(-7.08f, 1.7f, 5f), new Vector3(0.5f, 3.4f, 0.16f), wall);
            Cube("CorridorPilasterEast", root, new Vector3(7.08f, 1.7f, 5f), new Vector3(0.5f, 3.4f, 0.16f), wall);

            // Classroom-side baseboards skip the open main doorway (x -6.575..-4.925).
            Cube("CorridorBaseboardInnerWest", root, new Vector3(-13.8975f, 0.09f, 5.115f), new Vector3(14.645f, 0.18f, 0.06f), baseboard);
            Cube("CorridorBaseboardInnerEast", root, new Vector3(8.1475f, 0.09f, 5.115f), new Vector3(26.145f, 0.18f, 0.06f), baseboard);
            Cube("CorridorBaseboardOuter", root, new Vector3(0f, 0.09f, 8.085f), new Vector3(42.44f, 0.18f, 0.06f), baseboard);
        }

        private static void BuildCorridorOuterWall(Transform root)
        {
            Material wall = Mat("M_CorridorWall");
            Material metal = Mat("M_Metal");
            Material glass = Mat("M_Glass");
            Material exterior = Mat("M_ExteriorView");
            Material sill = Mat("M_Baseboard");

            Cube("OuterWallLower", root, new Vector3(0f, 0.48f, CorridorOuterZ), new Vector3(42.6f, 0.96f, 0.16f), wall);
            Cube("OuterWallUpper", root, new Vector3(0f, 3.06f, CorridorOuterZ), new Vector3(42.6f, 0.68f, 0.16f), wall);
            Cube("OuterWindowSill", root, new Vector3(0f, 0.93f, 8.06f), new Vector3(42.44f, 0.09f, 0.30f), sill);

            // Continuous glazing band y 0.96..2.72 in 2.13 m bays.
            const int bays = 20;
            const float bandWidth = 42.6f;
            for (int i = 0; i <= bays; i++)
            {
                float x = -bandWidth * 0.5f + i * (bandWidth / bays);
                Cube($"OuterWindowMullion_{i:00}", root, new Vector3(x, 1.84f, CorridorOuterZ), new Vector3(0.09f, 1.76f, 0.14f), metal);
            }

            Cube("OuterWindowRailTop", root, new Vector3(0f, 2.70f, CorridorOuterZ), new Vector3(42.6f, 0.07f, 0.14f), metal);
            Cube("OuterWindowRailBottom", root, new Vector3(0f, 0.985f, CorridorOuterZ), new Vector3(42.6f, 0.07f, 0.14f), metal);
            Cube("OuterWindowRailMid", root, new Vector3(0f, 2.02f, CorridorOuterZ), new Vector3(42.6f, 0.05f, 0.12f), metal);
            Cube("OuterWindowGlass", root, new Vector3(0f, 1.84f, CorridorOuterZ + 0.045f), new Vector3(42.6f, 1.7f, 0.03f), glass);

            // Courtyard backdrop behind the glazing so the windows never show
            // void, even at glancing angles from the corridor ends. One wide
            // campus panorama split into four tiles; alternate tiles mirror
            // (negative x scale) so the seam lines up without visible repeats.
            Material panorama = Mat("M_CorridorPanorama") ?? exterior;
            for (int i = 0; i < 4; i++)
            {
                float x = -16.2f + i * 10.8f;
                GameObject tile = Quad($"CorridorBackdrop_{i}", root, new Vector3(x, 1.75f, 8.85f), new Vector3(10.85f, 3.6f, 1f), Quaternion.identity, panorama);
                if (i % 2 == 1)
                {
                    Vector3 scale = tile.transform.localScale;
                    tile.transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
                }
            }
        }

        private static void BuildCorridorEndCaps(Transform root)
        {
            Material wall = Mat("M_Wall");
            Material metal = Mat("M_Metal");
            Material glass = Mat("M_Glass");
            Material rubber = Mat("M_Rubber");

            for (int side = -1; side <= 1; side += 2)
            {
                string tag = side < 0 ? "West" : "East";
                float x = side * CorridorEndX;
                Cube($"StairwellWall{tag}", root, new Vector3(x, 1.7f, CorridorCenterZ), new Vector3(0.16f, 3.4f, 3.28f), wall);

                float faceX = x - side * 0.13f;
                Transform doors = RootObject($"StairwellDoors{tag}", root, new Vector3(faceX, 0f, CorridorCenterZ)).transform;
                Cube("FramePostNorth", doors, new Vector3(0f, 1.1f, 0.95f), new Vector3(0.12f, 2.2f, 0.10f), rubber);
                Cube("FramePostSouth", doors, new Vector3(0f, 1.1f, -0.95f), new Vector3(0.12f, 2.2f, 0.10f), rubber);
                Cube("FrameHeader", doors, new Vector3(0f, 2.16f, 0f), new Vector3(0.12f, 0.12f, 2.0f), rubber);
                for (int leaf = -1; leaf <= 1; leaf += 2)
                {
                    string leafTag = leaf < 0 ? "A" : "B";
                    float z = leaf * 0.44f;
                    Cube($"DoorLeaf{leafTag}", doors, new Vector3(0f, 1.05f, z), new Vector3(0.08f, 2.10f, 0.86f), metal);
                    Cube($"LeafWindow{leafTag}", doors, new Vector3(-side * 0.035f, 1.58f, z), new Vector3(0.04f, 0.52f, 0.22f), glass);
                    Cube($"PushBar{leafTag}", doors, new Vector3(-side * 0.075f, 1.02f, z), new Vector3(0.035f, 0.05f, 0.56f), metal);
                }

                Cube($"ExitSign{tag}", root, new Vector3(x - side * 0.26f, 2.56f, CorridorCenterZ), new Vector3(0.10f, 0.24f, 0.50f), Mat("M_PlantLeaf"));
                CreateWorldText($"ExitText{tag}", root, "비상구", new Vector3(x - side * 0.325f, 2.56f, CorridorCenterZ), Quaternion.Euler(0f, -side * 90f, 0f), 0.045f, new Color(0.93f, 0.98f, 0.94f));
            }
        }

        private static void BuildCorridorLighting(Transform root)
        {
            Material fixture = Mat("M_LightFixture");
            Material emissive = Mat("M_Fluorescent");
            for (int i = 0; i < 12; i++)
            {
                float x = -19.55f + i * 3.55f;
                Cube($"CorridorFixture_{i:00}", root, new Vector3(x, 3.30f, CorridorCenterZ), new Vector3(1.9f, 0.07f, 0.42f), fixture);
                Cube($"CorridorDiffuser_{i:00}", root, new Vector3(x, 3.255f, CorridorCenterZ), new Vector3(1.7f, 0.03f, 0.30f), emissive);
                if (i % 2 != 0)
                {
                    continue;
                }

                GameObject lightObject = new GameObject($"CorridorLight_{i:00}");
                lightObject.transform.SetParent(root, false);
                lightObject.transform.localPosition = new Vector3(x, 3.0f, CorridorCenterZ);
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 6.0f;
                light.intensity = 0.38f;
                light.color = new Color(1f, 0.97f, 0.90f);
                light.shadows = LightShadows.None;
            }
        }

        // Upper window band (transom) between a classroom and the corridor,
        // built from real rails/mullions/glass inside a wall opening.
        private static void BuildTransomBand(Transform parent, string prefix, float centerX, float width, float wallZ)
        {
            Material metal = Mat("M_Metal");
            Material glass = Mat("M_Glass");
            Cube($"{prefix}RailBottom", parent, new Vector3(centerX, 1.925f, wallZ), new Vector3(width, 0.06f, 0.18f), metal);
            Cube($"{prefix}RailTop", parent, new Vector3(centerX, 2.575f, wallZ), new Vector3(width, 0.06f, 0.18f), metal);
            Cube($"{prefix}Glass", parent, new Vector3(centerX, 2.25f, wallZ), new Vector3(width, 0.65f, 0.04f), glass);
            int mullions = Mathf.Max(1, Mathf.RoundToInt(width / 1.35f) - 1);
            for (int i = 1; i <= mullions; i++)
            {
                float x = centerX - width * 0.5f + width * i / (mullions + 1f);
                Cube($"{prefix}Mullion_{i:00}", parent, new Vector3(x, 2.25f, wallZ), new Vector3(0.06f, 0.7f, 0.14f), metal);
            }
        }

        private static void BuildNeighborClassroom(Transform root, float dx, string label)
        {
            Transform room = RootObject($"NeighborClassroom_{(dx < 0 ? "West" : "East")}", root, Vector3.zero).transform;
            Material wall = Mat("M_Wall");
            Material floor = Mat("M_Floor");
            Material ceiling = Mat("M_Ceiling");
            Material trim = Mat("M_TrimWood");
            Material metal = Mat("M_Metal");
            Material glass = Mat("M_Glass");

            Cube("Floor", room, new Vector3(dx, -0.06f, 0f), new Vector3(14f, 0.12f, 10f), floor);
            Cube("Ceiling", room, new Vector3(dx, 3.42f, 0f), new Vector3(14f, 0.16f, 10f), ceiling);
            Cube("BackWall", room, new Vector3(dx, 1.7f, -5f), new Vector3(14f, 3.4f, 0.16f), wall);
            float outerX = dx < 0 ? dx - 7f : dx + 7f;
            float innerX = dx < 0 ? dx + 7f : dx - 7f;
            Cube("OuterSideWall", room, new Vector3(outerX, 1.7f, 0f), new Vector3(0.16f, 3.4f, 10f), wall);
            Cube("PartySideWall", room, new Vector3(innerX, 1.7f, 0f), new Vector3(0.16f, 3.4f, 10f), wall);

            // Corridor wall with a real transom opening (y 1.9..2.6).
            Cube("CorridorWallLower", room, new Vector3(dx, 0.95f, 5f), new Vector3(14f, 1.9f, 0.16f), wall);
            Cube("CorridorWallUpper", room, new Vector3(dx, 3.0f, 5f), new Vector3(14f, 0.8f, 0.16f), wall);
            Cube("TransomJambWest", room, new Vector3(dx - 6.825f, 2.25f, 5f), new Vector3(0.35f, 0.7f, 0.16f), wall);
            Cube("TransomJambEast", room, new Vector3(dx + 6.825f, 2.25f, 5f), new Vector3(0.35f, 0.7f, 0.16f), wall);
            BuildTransomBand(room, "Transom", dx, 13.3f, 5f);

            // Closed sliding door parked on the corridor-side rail.
            float doorX = dx - 5.75f;
            Cube("DoorRail", room, new Vector3(doorX + 0.85f, 2.42f, 5.17f), new Vector3(3.6f, 0.07f, 0.16f), metal);
            Transform door = RootObject("SlidingDoorClosed", room, new Vector3(doorX, 0f, 5.17f)).transform;
            Cube("Door", door, new Vector3(0f, 1.16f, 0f), new Vector3(1.65f, 2.32f, 0.10f), trim);
            Cube("DoorWindow", door, new Vector3(0f, 1.75f, 0.055f), new Vector3(0.68f, 0.78f, 0.04f), glass);
            Sphere("DoorHandle", door, new Vector3(0.59f, 1.05f, 0.09f), new Vector3(0.10f, 0.10f, 0.10f), metal);

            // Corridor-side nameplate beside the door.
            Cube("Nameplate", room, new Vector3(doorX + 1.35f, 2.10f, 5.10f), new Vector3(0.56f, 0.20f, 0.035f), Mat("M_WorkBlue"));
            CreateWorldText("NameplateText", room, label, new Vector3(doorX + 1.35f, 2.10f, 5.135f), Quaternion.identity, 0.042f, Color.white);

            BuildNeighborInterior(room, dx);
        }

        // Sparse blocked-out interior visible through the transom band; lights off.
        private static void BuildNeighborInterior(Transform room, float dx)
        {
            Material deskWood = Mat("M_DeskWood");
            Material deskMetal = Mat("M_DeskMetal");
            Material chair = Mat("M_ChairPlastic");
            Material board = Mat("M_Whiteboard");
            Material trim = Mat("M_TrimWood");
            Material fixture = Mat("M_LightFixture");
            Material lockerPaint = Mat("M_LockerPaint");

            Cube("RearChalkboard", room, new Vector3(dx, 2.05f, -4.86f), new Vector3(7.2f, 1.75f, 0.09f), board);
            Cube("RearBoardTrim", room, new Vector3(dx, 1.15f, -4.80f), new Vector3(7.5f, 0.09f, 0.20f), trim);
            Cube("TeacherDeskBlock", room, new Vector3(dx - 4.4f, 0.40f, 3.4f), new Vector3(1.6f, 0.80f, 0.75f), deskWood);
            float lockerX = dx < 0 ? dx - 6.62f : dx + 6.62f;
            Cube("SideLockerRun", room, new Vector3(lockerX, 0.6f, 0f), new Vector3(0.5f, 1.2f, 6.4f), lockerPaint);

            float[] xs = { dx - 3.2f, dx, dx + 3.2f };
            float[] zs = { 1.6f, -0.4f, -2.4f };
            int index = 0;
            foreach (float z in zs)
            {
                foreach (float x in xs)
                {
                    Transform desk = RootObject($"BlockDesk_{index++:00}", room, new Vector3(x, 0f, z)).transform;
                    Cube("Top", desk, new Vector3(0f, 0.70f, 0f), new Vector3(0.96f, 0.07f, 0.62f), deskWood);
                    Cube("LegPanelL", desk, new Vector3(-0.43f, 0.335f, 0f), new Vector3(0.06f, 0.67f, 0.56f), deskMetal);
                    Cube("LegPanelR", desk, new Vector3(0.43f, 0.335f, 0f), new Vector3(0.06f, 0.67f, 0.56f), deskMetal);
                    Cube("ChairSeat", desk, new Vector3(0f, 0.40f, -0.55f), new Vector3(0.50f, 0.07f, 0.46f), chair);
                    Cube("ChairBack", desk, new Vector3(0f, 0.72f, -0.76f), new Vector3(0.52f, 0.42f, 0.05f), chair);
                    Cube("ChairLegs", desk, new Vector3(0f, 0.18f, -0.55f), new Vector3(0.42f, 0.36f, 0.38f), deskMetal);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                float fx = dx + (i % 2 == 0 ? -2.6f : 2.6f);
                float fz = i < 2 ? 1.6f : -1.9f;
                Cube($"DarkFixture_{i}", room, new Vector3(fx, 3.30f, fz), new Vector3(2.0f, 0.07f, 0.45f), fixture);
            }
        }

        private static void BuildCorridorProps(Transform root)
        {
            Transform props = RootObject("CorridorProps", root, Vector3.zero).transform;

            // Blender-authored hero props (shoe lockers, fire cabinet, bench).
            InstantiateGeneratedProp("SM_CorridorShoeLocker_Realistic.obj", "ShoeLockerMain", props, new Vector3(-3.3f, 0f, 5.27f), Quaternion.identity, new Vector3(2.2f, 1.12f, 0.38f), new Vector3(0f, 0.56f, 0f));
            InstantiateGeneratedProp("SM_CorridorShoeLocker_Realistic.obj", "ShoeLockerWest", props, new Vector3(-17.6f, 0f, 5.27f), Quaternion.identity, new Vector3(2.2f, 1.12f, 0.38f), new Vector3(0f, 0.56f, 0f));
            InstantiateGeneratedProp("SM_CorridorShoeLocker_Realistic.obj", "ShoeLockerEast", props, new Vector3(10.7f, 0f, 5.27f), Quaternion.identity, new Vector3(2.2f, 1.12f, 0.38f), new Vector3(0f, 0.56f, 0f));
            InstantiateGeneratedProp("SM_FireExtinguisherCabinet_Realistic.obj", "FireExtinguisherCabinet", props, new Vector3(0.9f, 0.95f, 5.22f), Quaternion.identity, new Vector3(0.44f, 0.66f, 0.28f), Vector3.zero);
            InstantiateGeneratedProp("SM_CorridorBench_Realistic.obj", "CorridorBench", props, new Vector3(-1.3f, 0f, 7.68f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.5f, 0.46f, 0.44f), new Vector3(0f, 0.23f, 0f));

            BuildCorridorBulletins(props);
            BuildCleaningCloset(props);
            BuildRecyclingStation(props);
        }

        private static void InstantiateGeneratedProp(
            string fileName,
            string objectName,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 colliderSize,
            Vector3 colliderCenter)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Models/Generated/{fileName}");
            if (model == null)
            {
                Debug.LogWarning($"Generated corridor prop missing: {fileName}");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = objectName;
            instance.transform.localPosition = position;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = Vector3.one;
            BoxCollider collider = instance.AddComponent<BoxCollider>();
            collider.size = colliderSize;
            collider.center = colliderCenter;
        }

        private static void BuildCorridorBulletins(Transform props)
        {
            Material[] eastPosters =
            {
                Mat("M_Bulletin_Poster_Friendship"), Mat("M_Bulletin_Art_OurTown"),
                Mat("M_Bulletin_Worksheet_Green"), Mat("M_Bulletin_Notice_HomeLetter")
            };
            BuildCorridorBulletin(props, "CorridorBulletinEast", 5.85f, eastPosters);

            Material[] westPosters =
            {
                Mat("M_Bulletin_Art_School"), Mat("M_Bulletin_Worksheet_Blue"),
                Mat("M_Bulletin_Poster_Diversity"), Mat("M_Bulletin_Notice_ClassLetter")
            };
            BuildCorridorBulletin(props, "CorridorBulletinWest", -11.3f, westPosters);
        }

        private static void BuildCorridorBulletin(Transform props, string name, float x, Material[] posters)
        {
            Transform board = RootObject(name, props, new Vector3(x, 1.28f, 5.105f)).transform;
            Cube("Backing", board, Vector3.zero, new Vector3(1.9f, 1.15f, 0.05f), Mat("M_BulletinGreen"));
            Cube("FrameTop", board, new Vector3(0f, 0.60f, 0f), new Vector3(1.98f, 0.05f, 0.06f), Mat("M_TrimWood"));
            Cube("FrameBottom", board, new Vector3(0f, -0.60f, 0f), new Vector3(1.98f, 0.05f, 0.06f), Mat("M_TrimWood"));
            for (int i = 0; i < posters.Length; i++)
            {
                float px = i % 2 == 0 ? -0.44f : 0.44f;
                float py = i < 2 ? 0.27f : -0.28f;
                Cube($"Item_{i}", board, new Vector3(px, py, 0.032f), new Vector3(0.62f, 0.44f, 0.012f), posters[i]);
            }
        }

        private static void BuildCleaningCloset(Transform props)
        {
            Transform closet = RootObject("CleaningCloset", props, new Vector3(19.55f, 0f, 5.36f)).transform;
            Cube("Body", closet, new Vector3(0f, 0.9f, 0f), new Vector3(0.95f, 1.80f, 0.52f), Mat("M_LockerPaint"));
            Cube("DoorL", closet, new Vector3(-0.235f, 0.9f, 0.275f), new Vector3(0.43f, 1.72f, 0.03f), Mat("M_LockerYellow"));
            Cube("DoorR", closet, new Vector3(0.235f, 0.9f, 0.275f), new Vector3(0.43f, 1.72f, 0.03f), Mat("M_LockerPaint"));
            Cylinder("HandleL", closet, new Vector3(-0.06f, 0.95f, 0.30f), new Vector3(0.02f, 0.05f, 0.02f), Quaternion.Euler(90f, 0f, 0f), Mat("M_Metal"));
            Cylinder("HandleR", closet, new Vector3(0.06f, 0.95f, 0.30f), new Vector3(0.02f, 0.05f, 0.02f), Quaternion.Euler(90f, 0f, 0f), Mat("M_Metal"));
            CreateWorldText("ClosetLabel", closet, "청소도구함", new Vector3(0f, 1.60f, 0.305f), Quaternion.identity, 0.045f, new Color(0.28f, 0.22f, 0.13f));
        }

        private static void BuildRecyclingStation(Transform props)
        {
            Material[] binMaterials = { Mat("M_PlantLeaf"), Mat("M_WorkBlue"), Mat("M_Metal") };
            string[] names = { "BinGeneral", "BinRecycle", "BinCans" };
            for (int i = 0; i < names.Length; i++)
            {
                float x = -20.3f + i * 0.5f;
                Transform bin = RootObject(names[i], props, new Vector3(x, 0f, 7.6f)).transform;
                Cylinder("Body", bin, new Vector3(0f, 0.30f, 0f), new Vector3(0.36f, 0.30f, 0.36f), Quaternion.identity, binMaterials[i]);
                Cylinder("Rim", bin, new Vector3(0f, 0.60f, 0f), new Vector3(0.38f, 0.02f, 0.38f), Quaternion.identity, Mat("M_Rubber"));
            }
        }
    }
}
