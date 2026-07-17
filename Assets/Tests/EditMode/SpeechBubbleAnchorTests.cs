using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class SpeechBubbleAnchorTests
    {
        [Test]
        public void StudentSpeechBubble_TargetsAnimatedHeadInsteadOfAvatarRoot()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/KoreanClassroomTraining.unity", OpenSceneMode.Single);
            GameObject focalStudent = GameObject.Find("FocalStudent_Minjun");
            TrainingHud hud = Object.FindAnyObjectByType<TrainingHud>();

            Assert.That(focalStudent, Is.Not.Null, "The focal student is missing from the classroom scene.");
            Assert.That(hud, Is.Not.Null, "The training HUD is missing from the classroom scene.");

            Animator animator = focalStudent.GetComponentInChildren<Animator>();
            Transform head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            Transform speechTarget = (Transform)new SerializedObject(hud)
                .FindProperty("speechTarget").objectReferenceValue;

            Assert.That(head, Is.Not.Null, "The Rocketbox avatar must expose a humanoid Head bone.");
            Assert.That(speechTarget, Is.EqualTo(head),
                "The reply bubble is detached because it follows the avatar root instead of the animated head.");
        }

        [Test]
        public void StudentSpeechBubble_ClearsTheAnimatedFaceWhenEyeContactIsActive()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/KoreanClassroomTraining.unity", OpenSceneMode.Single);
            TrainingHud hud = Object.FindAnyObjectByType<TrainingHud>();
            SerializedObject serializedHud = new SerializedObject(hud);
            RectTransform bubble = (RectTransform)serializedHud.FindProperty("speechBubble").objectReferenceValue;
            RectTransform canvas = (RectTransform)((Canvas)serializedHud.FindProperty("rootCanvas").objectReferenceValue).transform;
            Camera camera = (Camera)serializedHud.FindProperty("worldCamera").objectReferenceValue;
            Transform target = (Transform)serializedHud.FindProperty("speechTarget").objectReferenceValue;

            bubble.gameObject.SetActive(true);
            hud.SetSpeechBubbleAvoidsFace(true);
            typeof(TrainingHud).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(hud, null);

            Vector3 viewport = camera.WorldToViewportPoint(target.position);
            float targetY = Mathf.Lerp(canvas.rect.yMin, canvas.rect.yMax, viewport.y);
            float bubbleBottom = bubble.anchoredPosition.y - bubble.rect.height * 0.5f;
            Assert.That(bubbleBottom - targetY, Is.GreaterThanOrEqualTo(108f),
                "The eye-contact reply bubble still overlaps the student's animated face.");
        }

        [Test]
        public void ConversationCamera_CachesTheAnimatedHeadInsteadOfGuessingRootHeight()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/KoreanClassroomTraining.unity", OpenSceneMode.Single);
            GameObject focalStudent = GameObject.Find("FocalStudent_Minjun");
            TeacherCameraController controller = Object.FindAnyObjectByType<TeacherCameraController>();
            Animator animator = focalStudent != null ? focalStudent.GetComponentInChildren<Animator>() : null;
            Transform head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;

            Assert.That(controller, Is.Not.Null, "The conversation camera controller is missing.");
            Assert.That(head, Is.Not.Null, "The focal student must expose a humanoid Head bone.");

            controller.SetFocusTarget(focalStudent.transform);
            Transform cachedHead = (Transform)typeof(TeacherCameraController)
                .GetField("focusHead", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(controller);

            Assert.That(cachedHead, Is.EqualTo(head),
                "The camera still relies on a fixed root-relative face height instead of the animated Head bone.");
        }

        [TestCase("Assets/Scenes/KoreanClassroomTraining.unity")]
        [TestCase("Assets/Scenes/KoreanClassroomCircleTraining.unity")]
        public void ConversationCamera_UsesAReadableFaceToFaceFraming(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            TeacherCameraController controller = Object.FindAnyObjectByType<TeacherCameraController>();

            Assert.That(controller, Is.Not.Null, "The conversation camera controller is missing.");

            Vector3 focusOffset = (Vector3)typeof(TeacherCameraController)
                .GetField("focusOffset", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(controller);
            float focusFieldOfView = (float)typeof(TeacherCameraController)
                .GetField("focusFieldOfView", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(controller);

            Assert.That(focusOffset.z, Is.InRange(1.2f, 1.45f),
                "The face-to-face camera is too far from the focal student.");
            Assert.That(focusFieldOfView, Is.InRange(38f, 46f),
                "The focus lens is too wide to read the student's facial expression.");
        }
    }
}
