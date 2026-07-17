using UnityEngine;

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
