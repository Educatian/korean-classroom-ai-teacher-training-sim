using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class BackpackSceneTests
    {
        [Test]
        public void ClassroomScene_ContainsDetailedBackpacksHangingFromDesks()
        {
            // Given
            EditorSceneManager.OpenScene("Assets/Scenes/KoreanClassroomTraining.unity");

            // When / Then
            for (int index = 0; index < 6; index++)
            {
                GameObject backpack = GameObject.Find($"Backpack_{index:00}");
                Assert.That(backpack, Is.Not.Null);
                Assert.That(backpack.transform.Find("Body"), Is.Not.Null);
                Assert.That(backpack.transform.Find("FrontPocket"), Is.Not.Null);
                Assert.That(backpack.transform.Find("HangStrap"), Is.Not.Null);
                Assert.That(backpack.transform.Find("DeskHook"), Is.Not.Null);
            }
        }
    }
}
