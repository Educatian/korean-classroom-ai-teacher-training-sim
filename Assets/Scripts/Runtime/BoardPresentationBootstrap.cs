using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdieLab.TeacherTraining
{
    public static class BoardPresentationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.name.StartsWith("KoreanClassroom", StringComparison.Ordinal))
            {
                return;
            }
            GameObject board = GameObject.Find("ElectronicBoardAssembly_Blender");
            if (board == null)
            {
                Debug.LogWarning("BOARD_PRESENTATION_UNAVAILABLE electronic board not found");
                return;
            }
            BoardPresentationController controller = board.GetComponent<BoardPresentationController>();
            if (controller == null)
            {
                controller = board.AddComponent<BoardPresentationController>();
            }
            controller.Initialize();

            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index + 1 < arguments.Length; index++)
            {
                if (arguments[index] == "--presentation-pdf")
                {
                    controller.LoadPdfFromPath(arguments[index + 1]);
                    break;
                }
            }
        }
    }
}