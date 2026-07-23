using System;
using System.IO;
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
    public static class AdaptiveLearningSupportPlayModeQaRunner
    {
        private const string ArmedKey = "AdieLab.TeacherTraining.LearningSupportQaArmed";
        private const string QuitKey = "AdieLab.TeacherTraining.LearningSupportQaQuit";
        private const string ScenePath = "Assets/Scenes/KoreanClassroomTraining.unity";
        private const string OutputPath = "Assets/Reference/Unity_Adaptive_Learning_Support.png";
        private static double phaseStartedAt;
        private static QaPhase phase;

        private enum QaPhase
        {
            AwaitScene,
            AwaitTraining,
            AwaitDockNotification,
            AwaitSupportPanel
        }

        static AdaptiveLearningSupportPlayModeQaRunner()
        {
            if (SessionState.GetBool(ArmedKey, false) || SessionState.GetBool(QuitKey, false))
            {
                ArmCallbacks();
            }
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(ArmedKey, true);
            SessionState.SetBool(QuitKey, false);
            ArmCallbacks();
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
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ArmedKey, false))
            {
                phase = QaPhase.AwaitScene;
                phaseStartedAt = EditorApplication.timeSinceStartup;
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(QuitKey, false))
            {
                SessionState.SetBool(QuitKey, false);
                EditorApplication.Exit(0);
            }
        }

        private static void Tick()
        {
            double requiredDelay = phase == QaPhase.AwaitScene ? 2.5d : 0.8d;
            if (EditorApplication.timeSinceStartup - phaseStartedAt < requiredDelay)
            {
                return;
            }

            try
            {
                if (phase == QaPhase.AwaitScene)
                {
                    ClickButton("PrebriefStartButton");
                    phase = QaPhase.AwaitTraining;
                    phaseStartedAt = EditorApplication.timeSinceStartup;
                    return;
                }

                if (phase == QaPhase.AwaitTraining)
                {
                    ClickButton("SituationPanelDockButton");
                    phase = QaPhase.AwaitDockNotification;
                    phaseStartedAt = EditorApplication.timeSinceStartup;
                    return;
                }

                if (phase == QaPhase.AwaitDockNotification)
                {
                    ValidateDockNotification();
                    ClickButton("SituationPanelDockButton");
                    ClickButton("LearningSupportRequestButton");
                    phase = QaPhase.AwaitSupportPanel;
                    phaseStartedAt = EditorApplication.timeSinceStartup;
                    return;
                }

                ValidateSupportPanel();
                CaptureCurrentFrame(OutputPath);
                Debug.Log("ADAPTIVE_LEARNING_SUPPORT_QA_OK level=ObservationCue text=visible bounds=inside-canvas overlap=clear");
                FinishSuccessfully();
            }
            catch (Exception exception)
            {
                EditorApplication.update -= Tick;
                Debug.LogException(exception);
                SessionState.SetBool(ArmedKey, false);
                SessionState.SetBool(QuitKey, false);
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateSupportPanel()
        {
            GameObject panelObject = GameObject.Find("AdaptiveLearningSupportPanel");
            Require(panelObject != null && panelObject.activeInHierarchy, "Adaptive learning-support panel is not visible.");

            TMP_Text heading = panelObject.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text.name == "Heading");
            TMP_Text body = panelObject.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text.name == "Body");
            TMP_Text source = panelObject.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text.name == "Source");
            Require(heading != null && !string.IsNullOrWhiteSpace(heading.text), "Learning-support heading is missing.");
            Require(body != null && !string.IsNullOrWhiteSpace(body.text), "Learning-support body is missing.");
            Require(source != null && !string.IsNullOrWhiteSpace(source.text), "Learning-support source label is missing.");

            foreach (TMP_Text label in new[] { heading, body, source })
            {
                label.ForceMeshUpdate();
                Require(label.textInfo.characterCount > 0, $"{label.name} generated no visible characters.");
                Require(label.textInfo.meshInfo.Any(mesh => mesh.vertexCount > 0), $"{label.name} generated no glyph mesh.");
                Require(!label.isTextOverflowing, $"{label.name} text is overflowing its assigned area.");
            }

            RectTransform panel = panelObject.GetComponent<RectTransform>();
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            Require(canvas != null, "Learning-support panel has no parent canvas.");
            Require(IsInside(panel, canvas.GetComponent<RectTransform>()), "Learning-support panel extends outside the HUD canvas.");

            RectTransform responsePanel = GameObject.Find("ResponsePanel")?.GetComponent<RectTransform>();
            if (responsePanel != null && responsePanel.gameObject.activeInHierarchy)
            {
                Require(!Overlaps(panel, responsePanel), "Learning-support panel overlaps the response-choice panel.");
            }
        }

        private static void ValidateDockNotification()
        {
            GameObject dock = GameObject.Find("HudQuickDock");
            Require(dock != null && dock.activeInHierarchy, "Left-edge HUD quick dock is missing.");
            Button[] buttons = dock.GetComponentsInChildren<Button>(true);
            Require(buttons.Length == 3, $"Expected three HUD dock buttons, found {buttons.Length}.");
            int generatedIcons = dock.GetComponentsInChildren<Image>(true)
                .Count(item => item.name == "Icon" && item.sprite != null);
            Require(generatedIcons == 3, $"Expected three generated dock icon sprites, found {generatedIcons}.");

            RectTransform dockRect = dock.GetComponent<RectTransform>();
            Canvas canvas = dock.GetComponentInParent<Canvas>();
            Require(canvas != null && IsInside(dockRect, canvas.GetComponent<RectTransform>()),
                "HUD quick dock extends outside the canvas.");
            Require(WorldRect(dockRect).xMin - WorldRect(canvas.GetComponent<RectTransform>()).xMin <= 20f,
                "HUD quick dock is not aligned to the left edge.");

            GameObject notification = GameObject.Find("HudDockNotification");
            Require(notification != null && notification.activeInHierarchy,
                "Dock click did not open its notification window.");
            TMP_Text title = notification.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text.name == "Title");
            TMP_Text body = notification.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(text => text.name == "Body");
            Require(title != null && !string.IsNullOrWhiteSpace(title.text), "Dock notification title is missing.");
            Require(body != null && !string.IsNullOrWhiteSpace(body.text), "Dock notification body is missing.");
        }

        private static bool IsInside(RectTransform child, RectTransform parent)
        {
            Rect childRect = WorldRect(child);
            Rect parentRect = WorldRect(parent);
            const float tolerance = 2f;
            return childRect.xMin >= parentRect.xMin - tolerance &&
                   childRect.yMin >= parentRect.yMin - tolerance &&
                   childRect.xMax <= parentRect.xMax + tolerance &&
                   childRect.yMax <= parentRect.yMax + tolerance;
        }

        private static bool Overlaps(RectTransform first, RectTransform second)
        {
            return WorldRect(first).Overlaps(WorldRect(second));
        }

        private static Rect WorldRect(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static void ClickButton(string objectName)
        {
            Button button = GameObject.Find(objectName)?.GetComponent<Button>();
            Require(button != null && button.gameObject.activeInHierarchy && button.interactable,
                $"Button {objectName} is unavailable.");
            button.onClick.Invoke();
        }

        private static void CaptureCurrentFrame(string outputPath)
        {
            Camera camera = GameObject.Find("TeacherCamera")?.GetComponent<Camera>();
            Require(camera != null, "Teacher camera is missing in play mode.");

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            const int width = 3200;
            const int height = 1800;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                if (image != null)
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void FinishSuccessfully()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(ArmedKey, false);
            SessionState.SetBool(QuitKey, true);
            EditorApplication.ExitPlaymode();
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
