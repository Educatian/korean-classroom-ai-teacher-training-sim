using System;
using System.Linq;
using AdieLab.TeacherTraining;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace AdieLab.TeacherTraining.Editor
{
    [InitializeOnLoad]
    public static class XrRigStructurePlayModeQaRunner
    {
        private const string ArmedKey = "AdieLab.TeacherTraining.XrRigQaArmed";
        private const string ScenePath = "Assets/Scenes/KoreanClassroomTraining.unity";
        private static XrTeacherRigAdapter adapter;
        private static int phase;
        private static double phaseStartedAt;

        static XrRigStructurePlayModeQaRunner()
        {
            if (SessionState.GetBool(ArmedKey, false))
            {
                ArmCallbacks();
            }
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(ArmedKey, true);
            phase = 0;
            ArmCallbacks();
            TrainingExperienceModePolicy.Save(TrainingExperienceMode.Desktop);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        private static void ArmCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ArmedKey, false) || state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            phaseStartedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update -= RunPhase;
            EditorApplication.update += RunPhase;
        }

        private static void RunPhase()
        {
            if (EditorApplication.timeSinceStartup - phaseStartedAt < 1d)
            {
                return;
            }

            try
            {
                if (phase == 0)
                {
                    EnableAndValidate();
                    adapter.Disable();
                    phase = 1;
                    phaseStartedAt = EditorApplication.timeSinceStartup;
                    return;
                }

                ValidateDesktopRestored();
                SessionState.SetBool(ArmedKey, false);
                EditorApplication.update -= RunPhase;
                Debug.Log("XR_RIG_STRUCTURE_QA_OK controllers=2 hud=world-space cleanup=desktop-restored");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(ArmedKey, false);
                EditorApplication.update -= RunPhase;
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void EnableAndValidate()
        {
            Camera camera = UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.GetComponent<TeacherCameraController>() != null);
            Canvas canvas = UnityEngine.Object.FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.name == "TrainingCanvas");
            Require(camera != null && canvas != null, "Training camera or HUD canvas is missing.");

            adapter = new XrTeacherRigAdapter();
            adapter.Enable(camera, canvas);

            Require(adapter.IsEnabled, "XR adapter did not enter the enabled state.");
            Require(adapter.EyeGazeProvider != null,
                "Quest Pro eye-gaze provider was not created with the XR Origin.");
            Require(UnityEngine.Object.FindAnyObjectByType<XROrigin>() != null, "XR Origin was not created.");
            Require(UnityEngine.Object.FindObjectsByType<XRRayInteractor>(FindObjectsSortMode.None).Length == 2,
                "Two tracked XR ray controllers were not created.");
            Require(canvas.renderMode == RenderMode.WorldSpace, "HUD did not switch to world-space mode.");
            Require(canvas.GetComponent<TrackedDeviceGraphicRaycaster>()?.enabled == true,
                "Tracked-device UI raycaster is not active.");
            Require(UnityEngine.Object.FindAnyObjectByType<XRUIInputModule>()?.enabled == true,
                "XR UI input module is not active.");
        }

        private static void ValidateDesktopRestored()
        {
            Canvas canvas = UnityEngine.Object.FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.name == "TrainingCanvas");
            Require(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera,
                "HUD did not return to desktop screen-space mode.");
            Require(UnityEngine.Object.FindObjectsByType<XROrigin>(FindObjectsSortMode.None).Length == 0,
                "XR Origin remained after disabling IVR mode.");
            Require(!UnityEngine.Object.FindObjectsByType<XRUIInputModule>(FindObjectsSortMode.None).Any(module => module.enabled),
                "XR UI input remained enabled after restoring desktop mode.");
            Require(canvas.GetComponent<TrackedDeviceGraphicRaycaster>()?.enabled != true,
                "Training HUD raycaster remained enabled after restoring desktop mode.");
            Require(UnityEngine.Object.FindAnyObjectByType<StandaloneInputModule>()?.enabled == true,
                "Desktop input module was not restored.");
            Camera camera = UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate.GetComponent<TeacherCameraController>() != null);
            Require(camera?.GetComponent<TeacherCameraController>()?.enabled == true,
                "Desktop camera controller was not restored.");
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
