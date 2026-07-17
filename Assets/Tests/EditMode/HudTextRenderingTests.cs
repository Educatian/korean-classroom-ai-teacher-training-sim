using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class HudTextRenderingTests
    {
        private const string ScenePath = "Assets/Scenes/KoreanClassroomTraining.unity";

        [Test]
        public void TrainingCanvas_UsesSdfTextAndTmpInputExclusively()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvas = GameObject.Find("TrainingCanvas");

            Assert.That(canvas, Is.Not.Null, "TrainingCanvas is missing from the generated scene.");
            Assert.That(canvas.GetComponentsInChildren<Text>(true), Is.Empty,
                "Legacy UGUI Text rasterization makes Korean text soft at fractional display scales.");

            TMP_Text[] sdfLabels = canvas.GetComponentsInChildren<TMP_Text>(true);
            Assert.That(sdfLabels.Length, Is.GreaterThanOrEqualTo(15));
            Assert.That(sdfLabels.All(label => label.font != null), Is.True,
                "Every HUD label must use a persistent TMP SDF font asset.");
            Assert.That(GameObject.Find("DialogueInput")?.GetComponent<TMP_InputField>(), Is.Not.Null);
        }

        [Test]
        public void TrainingModes_ShowOnlyTheActiveWorkSurfaceAtFullContrast()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvas = GameObject.Find("TrainingCanvas");
            TrainingModeNavigator navigator = canvas.GetComponent<TrainingModeNavigator>();
            CanvasGroup observation = canvas.transform.Find("SituationPanel").GetComponent<CanvasGroup>();
            CanvasGroup response = canvas.transform.Find("ResponsePanel").GetComponent<CanvasGroup>();
            CanvasGroup dialogue = canvas.transform.Find("DialoguePanel").GetComponent<CanvasGroup>();

            navigator.SelectMode(2);
            Assert.That(observation.alpha, Is.EqualTo(1f));
            Assert.That(response.alpha, Is.EqualTo(0f));
            Assert.That(dialogue.alpha, Is.EqualTo(1f));

            navigator.SelectMode(1);
            Assert.That(observation.alpha, Is.EqualTo(1f));
            Assert.That(response.alpha, Is.EqualTo(1f));
            Assert.That(dialogue.alpha, Is.EqualTo(0f));

            TMP_Text placeholder = canvas.GetComponentsInChildren<TMP_Text>(true)
                .Single(label => label.name == "Placeholder");
            Assert.That(placeholder.color.a, Is.EqualTo(1f));
            Assert.That(placeholder.fontSize, Is.GreaterThanOrEqualTo(16f));

            RectTransform studentLine = canvas.transform.Find("SituationPanel/StudentLine").GetComponent<RectTransform>();
            RectTransform observationSurface = canvas.transform.Find("SituationPanel/ObservationDetailsSurface").GetComponent<RectTransform>();
            Assert.That(studentLine.anchorMin.y, Is.GreaterThanOrEqualTo(0.53f));
            Assert.That(observationSurface.anchorMax.y, Is.GreaterThanOrEqualTo(0.53f));
        }
    }
}
