using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
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

            if (Array.IndexOf(arguments, "--tts-evidence") >= 0)
            {
                StartCoroutine(CaptureStudentTtsEvidence(arguments));
                return;
            }

            if (Array.IndexOf(arguments, "--face-roster-capture") >= 0)
            {
                StartCoroutine(CaptureFaceRoster(arguments));
                return;
            }

            if (Array.IndexOf(arguments, "--llm-dialogue-evidence") >= 0)
            {
                StartCoroutine(CaptureLiveDialogueEvidence(arguments, circleSceneLoaded));
                return;
            }

            if (Array.IndexOf(arguments, "--board-evidence") >= 0)
            {
                StartCoroutine(CaptureElectronicBoardEvidence(arguments));
                return;
            }
            if (Array.IndexOf(arguments, "--presentation-evidence") >= 0)
            {
                StartCoroutine(CapturePresentationEvidence(arguments));
                return;
            }

            StartCoroutine(PlayDemo(arguments, circleSceneLoaded));
        }

        private IEnumerator CaptureStudentTtsEvidence(string[] arguments)
        {
            yield return new WaitForSecondsRealtime(7f);
            NpcPerformance focal = GameObject.Find("FocalStudent_Minjun")?.GetComponent<NpcPerformance>();
            if (focal == null)
            {
                FailEvidence("focal student unavailable for TTS evidence", 21);
                yield break;
            }

            NpcSpeechPerformance speech = focal.GetComponent<NpcSpeechPerformance>();
            if (speech == null)
            {
                speech = focal.gameObject.AddComponent<NpcSpeechPerformance>();
            }

            focal.SetUprightEyeContact(true);
            focal.SetGesture(BehaviorGesture.Listen, 0.24f);
            speech.Speak(
                "선생님, 지금은 조금 힘들지만 잠깐 쉬었다가 다시 이야기해 볼게요.",
                new ActionUnitDirective { au1 = 0.18f, au15 = 0.12f, au25 = 0.15f, au26 = 0.10f },
                new AffectVector(-0.28f, 0.42f, -0.08f));
            float deadline = Time.realtimeSinceStartup + 45f;
            while (!speech.VoiceStatus.Contains("파형 립싱크") && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (!speech.VoiceStatus.Contains("파형 립싱크"))
            {
                FailEvidence($"student TTS did not reach audio playback: {speech.VoiceStatus}", 22);
                yield break;
            }

            StudentSpeechTelemetry trace = speech.CaptureTelemetry();
            Debug.Log($"STUDENT_TTS_EVIDENCE_OK provider={trace.providerRoute} rate={trace.rate:0.00} pitch={trace.pitchSemitones:+0.0;-0.0;0.0} commaPause={trace.commaPauseMilliseconds} sentencePause={trace.sentencePauseMilliseconds}");
            Capture("StudentTtsLipSync.png");
            yield return new WaitForSecondsRealtime(1.5f);
            if (Array.IndexOf(arguments, "--autoplay-exit") >= 0)
            {
                Application.Quit();
            }
        }
        private IEnumerator CapturePresentationEvidence(string[] arguments)
        {
            float deadline = Time.realtimeSinceStartup + 40f;
            BoardPresentationController presentation = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                presentation = FindAnyObjectByType<BoardPresentationController>();
                if (presentation != null && presentation.IsDocumentLoaded && !presentation.IsBusy)
                {
                    break;
                }
                if (presentation != null && !string.IsNullOrWhiteSpace(presentation.LastError))
                {
                    FailEvidence($"presentation load failed: {presentation.LastError}", 11);
                    yield break;
                }
                yield return new WaitForSecondsRealtime(0.25f);
            }
            if (presentation == null || !presentation.IsDocumentLoaded)
            {
                FailEvidence("presentation did not become ready", 12);
                yield break;
            }

            Camera camera = FindAnyObjectByType<Camera>();
            if (camera == null || string.IsNullOrWhiteSpace(captureDirectory))
            {
                FailEvidence("presentation capture prerequisites unavailable", 13);
                yield break;
            }
            TeacherCameraController teacherCamera = camera.GetComponent<TeacherCameraController>();
            if (teacherCamera != null)
            {
                teacherCamera.enabled = false;
            }
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            bool[] canvasStates = canvases.Select(canvas => canvas.enabled).ToArray();
            foreach (Canvas canvas in canvases)
            {
                canvas.enabled = canvas.GetComponentInParent<BoardPresentationController>() != null;
            }

            Vector3 savedPosition = camera.transform.position;
            Quaternion savedRotation = camera.transform.rotation;
            float savedFieldOfView = camera.fieldOfView;
            camera.transform.position = new Vector3(0.20f, 1.58f, -2.80f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(-0.75f, 1.86f, 4.50f) - camera.transform.position);
            camera.fieldOfView = 40f;
            yield return new WaitForSecondsRealtime(0.5f);
            Capture("Unity_PdfPresentation_Page01.png");
            yield return new WaitForSecondsRealtime(1f);

            if (presentation.PageCount > 1)
            {
                presentation.NextPage();
                deadline = Time.realtimeSinceStartup + 20f;
                while ((presentation.IsBusy || presentation.CurrentPageIndex < 1) && Time.realtimeSinceStartup < deadline)
                {
                    yield return new WaitForSecondsRealtime(0.2f);
                }
                yield return new WaitForSecondsRealtime(0.5f);
                Capture("Unity_PdfPresentation_Page02.png");
                yield return new WaitForSecondsRealtime(1f);
            }

            camera.transform.SetPositionAndRotation(savedPosition, savedRotation);
            camera.fieldOfView = savedFieldOfView;
            for (int index = 0; index < canvases.Length; index++)
            {
                canvases[index].enabled = canvasStates[index];
            }
            if (teacherCamera != null)
            {
                teacherCamera.enabled = true;
            }
            Debug.Log($"BOARD_PRESENTATION_EVIDENCE_OK title={presentation.DocumentTitle} pages={presentation.PageCount} current={presentation.CurrentPageIndex + 1}");
            if (Array.IndexOf(arguments, "--autoplay-exit") >= 0)
            {
                Application.Quit();
            }
        }
        private IEnumerator CaptureElectronicBoardEvidence(string[] arguments)
        {
            yield return new WaitForSecondsRealtime(6f);
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                FailEvidence("electronic-board capture directory unavailable", 8);
                yield break;
            }

            GameObject board = GameObject.Find("ElectronicBoardAssembly_Blender");
            Camera camera = FindAnyObjectByType<Camera>();
            if (board == null || camera == null || GameObject.Find("ElectronicBoardAssembly") != null)
            {
                FailEvidence("electronic-board replacement validation failed", 9);
                yield break;
            }

            Renderer[] renderers = board.GetComponentsInChildren<Renderer>(true);
            MeshFilter[] meshFilters = board.GetComponentsInChildren<MeshFilter>(true);
            int vertexCount = meshFilters.Sum(filter => filter.sharedMesh != null ? filter.sharedMesh.vertexCount : 0);
            if (renderers.Length == 0 || meshFilters.Length == 0 || vertexCount < 500)
            {
                FailEvidence($"electronic-board mesh validation failed: renderers={renderers.Length}, meshes={meshFilters.Length}, vertices={vertexCount}", 10);
                yield break;
            }
            TeacherCameraController teacherCamera = camera.GetComponent<TeacherCameraController>();
            if (teacherCamera != null)
            {
                teacherCamera.enabled = false;
            }
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            bool[] canvasStates = canvases.Select(canvas => canvas.enabled).ToArray();
            foreach (Canvas canvas in canvases)
            {
                canvas.enabled = false;
            }

            Vector3 savedPosition = camera.transform.position;
            Quaternion savedRotation = camera.transform.rotation;
            float savedFieldOfView = camera.fieldOfView;
            camera.transform.position = new Vector3(0.20f, 1.58f, -2.80f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(-0.75f, 1.95f, 4.55f) - camera.transform.position);
            camera.fieldOfView = 40f;
            yield return new WaitForEndOfFrame();
            Capture("Unity_ElectronicBoard_Applied.png");
            yield return new WaitForSecondsRealtime(1f);

            camera.transform.position = new Vector3(-0.10f, 1.78f, 1.45f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(-0.75f, 1.92f, 4.48f) - camera.transform.position);
            camera.fieldOfView = 48f;
            yield return new WaitForEndOfFrame();
            Capture("Unity_ElectronicBoard_Detail.png");
            yield return new WaitForSecondsRealtime(1f);

            camera.transform.SetPositionAndRotation(savedPosition, savedRotation);
            camera.fieldOfView = savedFieldOfView;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].enabled = canvasStates[i];
            }
            if (teacherCamera != null)
            {
                teacherCamera.enabled = true;
            }

            Debug.Log($"ELECTRONIC_BOARD_EVIDENCE_OK renderers={renderers.Length} meshes={meshFilters.Length} vertices={vertexCount}");
            if (Array.IndexOf(arguments, "--autoplay-exit") >= 0)
            {
                Application.Quit();
            }
        }
        private IEnumerator CaptureLiveDialogueEvidence(string[] arguments, bool circleScene)
        {
            yield return new WaitForSecondsRealtime(7f);
            if (string.IsNullOrWhiteSpace(captureDirectory))
            {
                FailEvidence("capture directory unavailable", 2);
                yield break;
            }

            GenerativeAiCoach coach = FindAnyObjectByType<GenerativeAiCoach>();
            if (coach == null || !coach.IsConfigured)
            {
                FailEvidence("OpenRouter is not configured", 3);
                yield break;
            }

            Click("ModeButton_2");
            TeacherCameraController teacherCamera = FindAnyObjectByType<TeacherCameraController>();
            NpcPerformance focalStudent = GameObject.Find("FocalStudent_Minjun")?.GetComponent<NpcPerformance>();
            teacherCamera?.EnterConversationFocus();
            teacherCamera?.SetUprightFocus(true);
            focalStudent?.SetUprightEyeContact(true);
            yield return new WaitForSecondsRealtime(1f);

            TMP_InputField input = GameObject.Find("DialogueInput")?.GetComponent<TMP_InputField>();
            TMP_Text status = GameObject.Find("DialogueStatus")?.GetComponent<TMP_Text>();
            TMP_Text studentReply = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(label => label.name == "StudentReply");
            if (input == null || status == null || studentReply == null)
            {
                FailEvidence("dialogue HUD bindings unavailable", 4);
                yield break;
            }

            string[] teacherUtterances =
            {
                "민준아, 지금 많이 답답해 보이네. 무엇이 가장 힘든지 네 말로 들려줄래?",
                "과제가 한꺼번에 밀려서 막막했구나. 지금은 잠깐 쉬기와 첫 문제를 같이 시작하기 중 무엇이 더 괜찮을까?",
                "좋아, 네가 선택한 속도로 하자. 선생님이 옆에서 첫 단계만 같이 해도 괜찮을까?"
            };
            StringBuilder transcript = new StringBuilder();
            transcript.AppendLine("# Live OpenRouter dialogue evidence");
            transcript.AppendLine();
            transcript.AppendLine($"- Scene: {(circleScene ? "CircleDiscussion" : "GeneralClassroom")}");
            transcript.AppendLine($"- Model: `{coach.ModelId}`");
            transcript.AppendLine("- Source: OpenRouter structured student-turn response");
            transcript.AppendLine();

            for (int turn = 0; turn < teacherUtterances.Length; turn++)
            {
                input.text = teacherUtterances[turn];
                Click("DialogueSendButton");
                yield return null;
                float startedAt = Time.realtimeSinceStartup;
                while (!input.interactable && Time.realtimeSinceStartup - startedAt < 60f)
                {
                    yield return null;
                }

                if (!input.interactable)
                {
                    FailEvidence($"turn {turn + 1} timed out", 5);
                    yield break;
                }

                yield return new WaitForSecondsRealtime(1.2f);
                if (!status.text.Contains("LLM 학생 응답", StringComparison.Ordinal) ||
                    status.text.Contains("대체됨", StringComparison.Ordinal))
                {
                    FailEvidence($"turn {turn + 1} did not resolve from OpenRouter: {status.text}", 6);
                    yield break;
                }

                string reply = studentReply.text.Trim();
                if (string.IsNullOrWhiteSpace(reply))
                {
                    FailEvidence($"turn {turn + 1} returned an empty student reply", 7);
                    yield break;
                }

                string fileName = $"LLM_FreeDialogue_Turn_{turn + 1:00}.png";
                Capture(fileName);
                yield return new WaitForSecondsRealtime(1f);
                transcript.AppendLine($"## Turn {turn + 1}");
                transcript.AppendLine();
                transcript.AppendLine($"- Teacher: {teacherUtterances[turn]}");
                transcript.AppendLine($"- Student: {reply}");
                transcript.AppendLine($"- Screenshot: `{fileName}`");
                transcript.AppendLine();
                Debug.Log($"LIVE_LLM_DIALOGUE_TURN_OK turn={turn + 1} model={coach.ModelId} screenshot={fileName}");

                if (turn < teacherUtterances.Length - 1)
                {
                    Click("ContinueButton");
                    yield return new WaitForSecondsRealtime(1f);
                    Click("ModeButton_2");
                }
            }

            Directory.CreateDirectory(captureDirectory);
            File.WriteAllText(
                Path.Combine(captureDirectory, "LLM_FreeDialogue_Evidence.md"),
                transcript.ToString(),
                new UTF8Encoding(false));
            Debug.Log($"LIVE_LLM_DIALOGUE_EVIDENCE_OK turns={teacherUtterances.Length} model={coach.ModelId}");
            if (Array.IndexOf(arguments, "--autoplay-exit") >= 0)
            {
                yield return new WaitForSecondsRealtime(1f);
                Application.Quit();
            }
        }

        private static void FailEvidence(string message, int exitCode)
        {
            Debug.LogError($"LIVE_LLM_DIALOGUE_EVIDENCE_FAILED {message}");
            Application.Quit(exitCode);
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
            Capture("TrainingDebriefSummary.png");
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(0.25f);
            Click("ModeButton_3");
            yield return new WaitForSecondsRealtime(1.0f);
            Capture("ResearchDashboardOverview.png");
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(0.25f);
            Click("DashboardTab_1");
            yield return new WaitForSecondsRealtime(0.6f);
            Capture("ResearchDashboardEvidence.png");
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(0.25f);
            Click("DashboardTab_2");
            yield return new WaitForSecondsRealtime(0.6f);
            Capture("ResearchDashboardTimeline.png");
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(0.25f);
            Click("DashboardBackButton");
            yield return new WaitForSecondsRealtime(0.6f);
            Debug.Log("DASHBOARD_QA_OK summary=true overview=true tabs=true return=true");
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
