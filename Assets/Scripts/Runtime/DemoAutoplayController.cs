using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    public sealed class DemoAutoplayController : MonoBehaviour
    {
        private string captureDirectory;

        private void Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (Array.IndexOf(arguments, "--autoplay") < 0)
            {
                return;
            }

            Application.runInBackground = true;
            bool circleSceneRequested = Array.IndexOf(arguments, "--scene2") >= 0;
            bool circleSceneLoaded = SceneManager.GetActiveScene().name == "KoreanClassroomCircleTraining";
            if (circleSceneRequested && !circleSceneLoaded)
            {
                SceneManager.LoadScene("KoreanClassroomCircleTraining");
                return;
            }

            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);

            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == "--capture-dir")
                {
                    captureDirectory = Path.GetFullPath(arguments[i + 1]);
                }
            }

            if (Array.IndexOf(arguments, "--face-roster-capture") >= 0)
            {
                StartCoroutine(CaptureFaceRoster(arguments));
                return;
            }

            StartCoroutine(PlayDemo(arguments, circleSceneLoaded));
        }

        private IEnumerator CaptureFaceRoster(string[] arguments)
        {
            yield return new WaitForSecondsRealtime(5f);

            Camera camera = FindAnyObjectByType<Camera>();
            TeacherCameraController teacherCamera = camera != null
                ? camera.GetComponent<TeacherCameraController>()
                : null;
            if (camera == null || string.IsNullOrWhiteSpace(captureDirectory))
            {
                Debug.LogError("FACE_ROSTER_CAPTURE_FAILED camera or capture directory unavailable");
                Application.Quit(2);
                yield break;
            }

            string faceDirectory = Path.Combine(captureDirectory, "FaceRoster");
            Directory.CreateDirectory(faceDirectory);
            NpcPerformance[] students = FindObjectsByType<NpcPerformance>(FindObjectsSortMode.None);
            Array.Sort(students, (left, right) => string.CompareOrdinal(left.name, right.name));

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            bool[] canvasStates = new bool[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                canvasStates[i] = canvases[i].enabled;
                canvases[i].enabled = false;
            }

            if (teacherCamera != null)
            {
                teacherCamera.enabled = false;
            }

            float savedFieldOfView = camera.fieldOfView;
            for (int i = 0; i < students.Length; i++)
            {
                NpcPerformance student = students[i];
                student.SetGesture(BehaviorGesture.Listen, 0.12f);
                student.SetUprightEyeContact(true);
                yield return new WaitForSecondsRealtime(0.35f);

                Animator animator = student.GetComponentInChildren<Animator>();
                Transform head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
                if (head == null)
                {
                    Debug.LogError($"FACE_ROSTER_CAPTURE_SKIPPED {student.name} missing head bone");
                    continue;
                }

                Vector3 focus = head.position + Vector3.up * 0.015f;
                camera.transform.position = focus + student.transform.forward * 1.72f;
                camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position, Vector3.up);
                camera.fieldOfView = 31f;
                yield return new WaitForEndOfFrame();

                string fileName = $"Face_{i + 1:00}_{student.name}.png";
                string outputPath = Path.Combine(faceDirectory, fileName);
                ScreenCapture.CaptureScreenshot(outputPath);
                Debug.Log($"FACE_ROSTER_CAPTURE_OK {fileName}");
                yield return new WaitForSecondsRealtime(0.3f);
                student.SetUprightEyeContact(false);
            }

            camera.fieldOfView = savedFieldOfView;
            if (teacherCamera != null)
            {
                teacherCamera.enabled = true;
            }
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].enabled = canvasStates[i];
            }

            yield return new WaitForSecondsRealtime(1f);
            if (Array.IndexOf(arguments, "--autoplay-exit") >= 0)
            {
                Application.Quit();
            }
        }

        private IEnumerator PlayDemo(string[] arguments, bool circleScene)
        {
            yield return new WaitForSecondsRealtime(8f);
            Capture(circleScene ? "CircleDiscussionReference.png" : "ClassroomReference.png");
            yield return new WaitForEndOfFrame();
            Click("ModeButton_2");
            TeacherCameraController teacherCamera = FindAnyObjectByType<TeacherCameraController>();
            NpcPerformance focalStudent = GameObject.Find("FocalStudent_Minjun")?.GetComponent<NpcPerformance>();
            teacherCamera?.EnterConversationFocus();
            teacherCamera?.SetUprightFocus(true);
            focalStudent?.SetUprightEyeContact(true);
            yield return new WaitForSecondsRealtime(0.8f);

            TMP_InputField input = GameObject.Find("DialogueInput").GetComponent<TMP_InputField>();
            input.text = "지금 많이 답답해 보이는구나. 잠깐 쉬고 천천히 이야기해도 괜찮아.";
            Click("DialogueSendButton");
            yield return new WaitForSecondsRealtime(2.6f);
            focalStudent?.SetGesture(BehaviorGesture.Listen, 0.18f);
            yield return null;
            teacherCamera?.SetUprightFocus(true);
            focalStudent?.SetUprightEyeContact(true);
            yield return new WaitForSecondsRealtime(1.4f);
            Capture(circleScene ? "CircleStudentEyeContact.png" : "StudentEyeContact.png");
            yield return new WaitForEndOfFrame();

            Click("ModeButton_1");
            focalStudent?.SetUprightEyeContact(false);
            teacherCamera?.SetUprightFocus(false);
            teacherCamera?.ExitConversationFocus();
            yield return new WaitForSecondsRealtime(0.7f);
            for (int beat = 0; beat < 6; beat++)
            {
                Click("OptionButton_1");
                yield return new WaitForSecondsRealtime(1.4f);
                Click("ContinueButton");
                yield return new WaitForSecondsRealtime(1.1f);
                if (beat == 2)
                {
                    Click("ModeButton_0");
                    yield return new WaitForSecondsRealtime(1.1f);
                    Click("ModeButton_1");
                }
            }

            yield return new WaitForSecondsRealtime(1.2f);
            Capture("TrainingDebrief.png");
            if (Array.IndexOf(arguments, "--autoplay-exit") >= 0)
            {
                yield return new WaitForSecondsRealtime(2f);
                Application.Quit();
            }
        }

        private static void Click(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null && target.TryGetComponent(out Button button))
            {
                button.onClick.Invoke();
            }
        }

        private void Capture(string fileName)
        {
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                return;
            }

            Directory.CreateDirectory(captureDirectory);
            ScreenCapture.CaptureScreenshot(Path.Combine(captureDirectory, fileName));
        }
    }
}
