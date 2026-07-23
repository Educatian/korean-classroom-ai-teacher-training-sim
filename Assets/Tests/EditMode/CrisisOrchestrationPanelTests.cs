using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class CrisisOrchestrationPanelTests
    {
        [Test]
        public void Install_BuildsHiddenLauncherAndReadableFirstBeat()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Art/Fonts/NotoSansKR-SDF.asset");
            GameObject root = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                Canvas canvas = root.GetComponent<Canvas>();
                CrisisOrchestrationPanel panel = CrisisOrchestrationPanel.Install(canvas, font);

                Assert.That(panel, Is.Not.Null);
                Transform launcher = root.transform.Find("CrisisTrainingLauncher");
                Transform overlay = root.transform.Find("CrisisOrchestrationOverlay");
                Assert.That(launcher, Is.Not.Null);
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.gameObject.activeSelf, Is.False);

                launcher.GetComponent<Button>().onClick.Invoke();
                Assert.That(overlay.gameObject.activeSelf, Is.True);
                TMP_Text title = overlay.Find("CrisisOrchestrationCard/Title")
                    .GetComponent<TMP_Text>();
                Assert.That(title.text, Does.Contain("내 상태"));
                Assert.That(overlay.Find("CrisisOrchestrationCard/Options/Option_0"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
