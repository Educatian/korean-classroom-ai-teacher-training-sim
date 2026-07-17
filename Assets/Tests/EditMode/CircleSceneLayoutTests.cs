using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class CircleSceneLayoutTests
    {
        [Test]
        public void CircleScene_UsesChairAnchorsAndAnObliqueRingOverview()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/KoreanClassroomCircleTraining.unity", OpenSceneMode.Single);
            Transform furniture = GameObject.Find("00_ENVIRONMENT/Furniture").transform;
            Transform students = GameObject.Find("10_STUDENTS").transform;
            Transform[] desks = furniture.Cast<Transform>()
                .Where(item => item.name.StartsWith("StudentDesk_", StringComparison.Ordinal) && item.gameObject.activeSelf)
                .OrderBy(item => item.name)
                .ToArray();
            Transform[] participants = students.Cast<Transform>()
                .OrderBy(item => item.name == "FocalStudent_Minjun" ? 0 : 1)
                .ThenBy(item => item.name)
                .ToArray();

            Assert.That(desks, Has.Length.EqualTo(participants.Length));
            for (int i = 0; i < participants.Length; i++)
            {
                Transform chair = desks[i].Find("Chair");
                Assert.That(chair, Is.Not.Null);
                Assert.That(Vector3.Distance(participants[i].position, chair.position), Is.LessThan(0.02f),
                    $"{participants[i].name} must sit at the chair instead of intersecting the desktop.");
            }

            Assert.That(desks.All(desk => Mathf.Abs(desk.position.magnitude - 3.25f) < 0.03f), Is.True,
                "Discussion desks must remain a real ring instead of flattening into a row.");
            Assert.That(desks.Max(desk => desk.position.z), Is.GreaterThan(3.0f));
            Assert.That(desks.Min(desk => desk.position.z), Is.LessThan(-3.0f));
            Assert.That(desks.Max(desk => Mathf.Abs(desk.position.x)), Is.GreaterThan(3.0f));

            Camera teacherCamera = GameObject.Find("20_SYSTEMS/TeacherCamera").GetComponent<Camera>();
            Assert.That(teacherCamera.transform.position.x, Is.LessThan(-5.2f));
            Assert.That(teacherCamera.transform.position.y, Is.GreaterThan(2.5f));
            Assert.That(teacherCamera.transform.position.z, Is.GreaterThan(4.0f));
            Assert.That(teacherCamera.fieldOfView, Is.GreaterThanOrEqualTo(59f));
        }
    }
}
