using System;
using System.Linq;
using AdieLab.TeacherTraining;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining.Editor
{
    [InitializeOnLoad]
    public static class ScenarioAssetPlayModeQaRunner
    {
        private const string ArmedKey = "AdieLab.TeacherTraining.ScenarioAssetQaArmed";
        private const string IndexKey = "AdieLab.TeacherTraining.ScenarioAssetQaIndex";
        private const string GeneralScene = "Assets/Scenes/KoreanClassroomTraining.unity";
        private const string CircleScene = "Assets/Scenes/KoreanClassroomCircleTraining.unity";
        private static readonly string[] ScenePaths = { GeneralScene, CircleScene };
        private static double enteredPlayModeAt;

        static ScenarioAssetPlayModeQaRunner()
        {
            if (SessionState.GetBool(ArmedKey, false))
            {
                ArmCallbacks();
            }
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(ArmedKey, true);
            SessionState.SetInt(IndexKey, 0);
            ArmCallbacks();
            OpenCurrentSceneAndPlay();
        }

        private static void ArmCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ArmedKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                enteredPlayModeAt = EditorApplication.timeSinceStartup;
                EditorApplication.update -= ValidateCurrentScene;
                EditorApplication.update += ValidateCurrentScene;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                int nextIndex = SessionState.GetInt(IndexKey, 0) + 1;
                SessionState.SetInt(IndexKey, nextIndex);
                if (nextIndex < ScenePaths.Length)
                {
                    OpenCurrentSceneAndPlay();
                }
                else
                {
                    SessionState.SetBool(ArmedKey, false);
                    Debug.Log("SCENARIO_ASSET_PLAYMODE_QA_OK scenes=2 beatsPerScene=6 source=ScriptableObject");
                    EditorApplication.Exit(0);
                }
            }
        }

        private static void ValidateCurrentScene()
        {
            if (EditorApplication.timeSinceStartup - enteredPlayModeAt < 2.5d)
            {
                return;
            }

            try
            {
                EditorApplication.update -= ValidateCurrentScene;
                int index = SessionState.GetInt(IndexKey, 0);
                TrainingSceneId sceneId = index == 0
                    ? TrainingSceneId.GeneralClassroom
                    : TrainingSceneId.CircleDiscussion;
                TrainingScenarioAsset scenario = TrainingScenarioCatalog.LoadDefault().ScenarioFor(sceneId);
                Require(scenario != null, $"No scenario asset is registered for {sceneId}.");
                Require(scenario.BuildRuntimeBeats().Length == 6, $"{sceneId} did not load six runtime beats.");
                Require(UnityEngine.Object.FindAnyObjectByType<SimulationController>() != null,
                    $"{sceneId} has no active SimulationController.");
                TrainingExperienceModeController experienceMode =
                    UnityEngine.Object.FindAnyObjectByType<TrainingExperienceModeController>();
                Require(experienceMode != null, $"{sceneId} has no desktop/IVR mode controller.");
                Require(experienceMode.CurrentMode == TrainingExperienceMode.Desktop,
                    $"{sceneId} did not preserve desktop mode in the Windows editor.");
                Require(GameObject.Find("ExperienceModeButton")?.GetComponent<Button>() != null,
                    $"{sceneId} has no visible experience-mode toggle.");

                TMP_Text beatLabel = GameObject.Find("BeatLabel")?.GetComponent<TMP_Text>();
                Require(beatLabel != null && beatLabel.text.Contains("1/6"),
                    $"{sceneId} did not present the first authored beat. label={beatLabel?.text}");
                int visibleChoices = Enumerable.Range(1, 3)
                    .Count(choice => !string.IsNullOrWhiteSpace(
                        GameObject.Find($"OptionButton_{choice}")?.GetComponentInChildren<TMP_Text>()?.text));
                Require(visibleChoices == 3, $"{sceneId} did not present all three response choices.");
                Debug.Log($"SCENARIO_ASSET_SCENE_OK scene={sceneId} scenario={scenario.ScenarioId} choices={visibleChoices}");
                EditorApplication.ExitPlaymode();
            }
            catch (Exception exception)
            {
                SessionState.SetBool(ArmedKey, false);
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void OpenCurrentSceneAndPlay()
        {
            EditorSceneManager.OpenScene(ScenePaths[SessionState.GetInt(IndexKey, 0)], OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
