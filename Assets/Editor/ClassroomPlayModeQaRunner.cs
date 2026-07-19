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
    public static class ClassroomPlayModeQaRunner
    {
        private const string ArmedKey = "AdieLab.TeacherTraining.QaArmed";
        private const string QuitKey = "AdieLab.TeacherTraining.QaQuit";
        private const string ScenePath = "Assets/Scenes/KoreanClassroomTraining.unity";
        private const string OutputPath = "Logs/VisualQa/Unity_PlayMode_Preview.png";
        private static double phaseStartedAt;
        private static QaPhase phase;
        private static string recordPath;
        private static int initialRecordCount;

        private enum QaPhase
        {
            Initial,
            AwaitDialogue,
            AwaitEyeContact,
            AwaitUprightEyeContact,
            AwaitAngry,
            AwaitRecovering,
            AwaitCompletion
        }

        static ClassroomPlayModeQaRunner()
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
                phase = QaPhase.Initial;
                phaseStartedAt = EditorApplication.timeSinceStartup;
                recordPath = Path.Combine(Application.persistentDataPath, "teacher_training_sessions.jsonl");
                initialRecordCount = CountRecords(recordPath);
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
            double elapsed = EditorApplication.timeSinceStartup - phaseStartedAt;
            if ((phase == QaPhase.Initial && elapsed < 2.5d) || (phase != QaPhase.Initial && elapsed < 2.1d))
            {
                return;
            }

            try
            {
                NpcPerformance focal = FindFocalStudent();
                switch (phase)
                {
                    case QaPhase.Initial:
                        ValidateInitialState(focal);
                        ValidateOptionLabels();
                        ClickButton("ModeButton_2");
                        CanvasGroup dialogueMode = GameObject.Find("DialoguePanel")?.GetComponent<CanvasGroup>();
                        Require(dialogueMode != null && dialogueMode.alpha > 0.99f && dialogueMode.interactable, "Dialogue mode button did not activate the dialogue panel.");
                        CaptureCurrentFrame(OutputPath);
                        CaptureAvatarFrame(focal, "Logs/VisualQa/Unity_Distressed_Closeup.png");
                        TMP_InputField dialogue = GameObject.Find("DialogueInput")?.GetComponent<TMP_InputField>();
                        Require(dialogue != null && dialogue.interactable, "Direct dialogue input is unavailable.");
                        dialogue.text = "지금 많이 답답해 보이는구나. 잠깐 쉬어도 괜찮아.";
                        ClickButton("DialogueSendButton");
                        Advance(QaPhase.AwaitDialogue);
                        return;
                    case QaPhase.AwaitDialogue:
                        if (focal.CurrentGesture != BehaviorGesture.Recover && elapsed < 15d)
                        {
                            return;
                        }
                        Require(focal.CurrentGesture == BehaviorGesture.Recover, "Supportive direct dialogue did not select Recover gesture.");
                        Require(focal.CurrentVector.valence > -0.7f, "Dynamic valence did not move toward recovery.");
                        Require(focal.GetActionUnit(FacialActionUnit.AU1InnerBrowRaiser) < 0.4f, "AU1 did not relax as valence improved.");
                        GameObject bubble = GameObject.Find("StudentSpeechBubble");
                        Require(bubble != null && bubble.activeInHierarchy, "Student reply speech bubble is not visible.");
                        TMP_Text bubbleText = GameObject.Find("StudentReply")?.GetComponent<TMP_Text>();
                        Require(bubbleText != null && !string.IsNullOrWhiteSpace(bubbleText.text), "Student speech bubble has no reply text.");
                        Camera dialogueCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
                        Animator dialogueAnimator = focal.GetComponentInChildren<Animator>();
                        Transform dialogueHead = dialogueAnimator != null ? dialogueAnimator.GetBoneTransform(HumanBodyBones.Head) : null;
                        Require(dialogueCamera != null && dialogueHead != null && Mathf.Abs(dialogueCamera.transform.position.x - dialogueHead.position.x) < 0.12f, "Conversation camera is not centered face-to-face.");
                        Vector3 faceDirection = (dialogueHead.position + Vector3.up * 0.02f - dialogueCamera.transform.position).normalized;
                        Require(Vector3.Dot(dialogueCamera.transform.forward, faceDirection) > 0.995f, "Conversation camera is not aimed at the student's face.");
                        Require(Vector3.Distance(dialogueCamera.transform.position, focal.transform.position) < 2.55f, "Conversation camera is too far from the student.");
                        Light faceLight = dialogueCamera.GetComponent<Light>();
                        Require(faceLight != null && faceLight.enabled && faceLight.intensity >= 0.8f, "Conversation face key light is missing or too dim.");
                        CaptureCurrentFrame("Logs/VisualQa/Unity_FaceToFace_Dialogue.png");
                        focal.SetGesture(BehaviorGesture.Listen, 0.18f);
                        Advance(QaPhase.AwaitEyeContact);
                        return;
                    case QaPhase.AwaitEyeContact:
                        Require(focal.CurrentGesture == BehaviorGesture.Listen, "Eye-contact capture did not enter the listening gesture.");
                        Animator eyeAnimator = focal.GetComponentInChildren<Animator>();
                        Transform head = eyeAnimator != null ? eyeAnimator.GetBoneTransform(HumanBodyBones.Head) : null;
                        Camera eyeCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
                        Require(head != null && eyeCamera != null, "Eye-contact head or camera is unavailable.");
                        Vector3 lensDirection = (eyeCamera.transform.position - head.position).normalized;
                        float eyeAlignment = Vector3.Dot(head.up, lensDirection);
                        Require(eyeAlignment > 0.96f, $"Student face is not aligned to the camera lens. alignment={eyeAlignment:F3}");
                        Debug.Log($"EYE_CONTACT_ALIGNMENT faceAxis={eyeAlignment:F3} forward={Vector3.Dot(head.forward, lensDirection):F3} up={Vector3.Dot(head.up, lensDirection):F3}");
                        CaptureCurrentFrame("Logs/VisualQa/Unity_EyeContact_Moment.png");
                        focal.SetUprightEyeContact(true);
                        eyeCamera.GetComponent<TeacherCameraController>()?.SetUprightFocus(true);
                        UnityEngine.Object.FindAnyObjectByType<TrainingHud>()?.SetSpeechBubbleAvoidsFace(true);
                        Advance(QaPhase.AwaitUprightEyeContact);
                        return;
                    case QaPhase.AwaitUprightEyeContact:
                        Require(focal.CurrentGesture == BehaviorGesture.Listen && focal.UprightEyeContact, "Upright eye-contact pose is not active.");
                        CaptureCurrentFrame("Logs/VisualQa/Unity_Upright_EyeContact.png");
                        CaptureAvatarFrame(focal, "Logs/VisualQa/Unity_Upright_EyeContact_Closeup.png");
                        focal.SetUprightEyeContact(false);
                        UnityEngine.Object.FindAnyObjectByType<TeacherCameraController>()?.SetUprightFocus(false);
                        UnityEngine.Object.FindAnyObjectByType<TrainingHud>()?.SetSpeechBubbleAvoidsFace(false);
                        ClickButton(new string(new[]
                        {
                            'C', 'o', 'n', 't', 'i', 'n', 'u', 'e', 'B', 'u', 't', 't', 'o', 'n'
                        }));
                        ClickButton("ModeButton_1");
                        ClickButton("OptionButton_2");
                        Advance(QaPhase.AwaitAngry);
                        return;
                    case QaPhase.AwaitAngry:
                        Require(focal.CurrentAffect == StudentAffect.Angry, "Poor first response did not drive Angry affect.");
                        float angryWeight = focal.GetMaxBlendShapeWeight("browdown", "brow_down", "browlower");
                        Require(angryWeight > 20f, $"Angry brow blendshape did not animate. weight={angryWeight:F1}");
                        CaptureAvatarFrame(focal, "Logs/VisualQa/Unity_Angry_Closeup.png");
                        ClickButton("ContinueButton");
                        ClickButton("OptionButton_1");
                        Advance(QaPhase.AwaitRecovering);
                        return;
                    case QaPhase.AwaitRecovering:
                        Require(focal.CurrentAffect == StudentAffect.Recovering, "Effective second response did not drive Recovering affect.");
                        float smileWeight = focal.GetMaxBlendShapeWeight("mouthsmile", "mouth_smile", "lipcornerpull");
                        Require(smileWeight > 3f, $"Recovering smile blendshape did not animate. weight={smileWeight:F1}");
                        CaptureAvatarFrame(focal, "Logs/VisualQa/Unity_Recovering_Closeup.png");
                        for (int beat = 3; beat < 6; beat++)
                        {
                            ClickButton("ContinueButton");
                            ClickButton("OptionButton_1");
                        }
                        Advance(QaPhase.AwaitCompletion);
                        return;
                    case QaPhase.AwaitCompletion:
                        ClickButton("ContinueButton");
                        TMP_Text beatLabel = GameObject.Find("BeatLabel")?.GetComponent<TMP_Text>();
                        Require(beatLabel != null && beatLabel.text == "훈련 완료", "Training did not reach completion UI.");
                        int writtenRecords = CountRecords(recordPath) - initialRecordCount;
                        Require(writtenRecords == 6, $"Expected 6 session records, wrote {writtenRecords}.");
                        RectTransform debrief = GameObject.Find("DebriefPanel")?.GetComponent<RectTransform>();
                        Require(debrief != null && debrief.gameObject.activeInHierarchy, "Completion did not activate the debrief mode panel.");
                        CaptureCurrentFrame("Logs/VisualQa/Unity_Completion_Preview.png");
                        Debug.Log("PLAYMODE_FLOW_OK beats=6 modes=4 buttons=verified persistenceRecords=6");
                        FinishSuccessfully();
                        return;
                }
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

        private static void ValidateInitialState(NpcPerformance focal)
        {
            Animator animator = focal.GetComponentInChildren<Animator>();
            AnimatorStateInfo state = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
            float distressedWeight = focal.GetMaxBlendShapeWeight("browinnerup", "brow_inner_up", "browraise");
            Require(focal.CurrentAffect == StudentAffect.Distressed, "Initial focal affect is not Distressed.");
            Require(animator != null && animator.isHuman, "Focal animator is not a Humanoid rig.");
            Require(focal.BlendShapeChannelCount >= 100, "Facial blendshape channels are missing.");
            Require(distressedWeight > 20f, $"Distressed brow blendshape did not animate. weight={distressedWeight:F1}");
            Debug.Log(
                $"PLAYMODE_QA_OK affect={focal.CurrentAffect} blendShapes={focal.BlendShapeChannelCount} " +
                $"distressedWeight={distressedWeight:F1} animatorHuman={animator.isHuman} state={state.IsName("Withdraw")} normalizedTime={state.normalizedTime:F2}");
        }

        private static void ValidateOptionLabels()
        {
            for (int i = 1; i <= 3; i++)
            {
                TMP_Text label = GameObject.Find($"OptionButton_{i}")?.GetComponentInChildren<TMP_Text>();
                Require(label != null && !string.IsNullOrWhiteSpace(label.text), $"Option {i} label text is missing.");
                label.ForceMeshUpdate();
                Require(label.textInfo.characterCount > 0 && label.textInfo.meshInfo[0].vertexCount > 0,
                    $"Option {i} label has text but generated no visible glyph mesh.");
            }
        }

        private static NpcPerformance FindFocalStudent()
        {
            NpcPerformance focal = UnityEngine.Object.FindObjectsByType<NpcPerformance>()
                .FirstOrDefault(item => item.name.Contains("FocalStudent", StringComparison.Ordinal));
            return focal ?? throw new InvalidOperationException("Focal student was not initialized in play mode.");
        }

        private static void ClickButton(string objectName)
        {
            Button button = GameObject.Find(objectName)?.GetComponent<Button>();
            Require(button != null && button.gameObject.activeInHierarchy && button.interactable, $"Button {objectName} is unavailable.");
            button.onClick.Invoke();
        }

        private static void Advance(QaPhase next)
        {
            phase = next;
            phaseStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void FinishSuccessfully()
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(ArmedKey, false);
            SessionState.SetBool(QuitKey, true);
            EditorApplication.ExitPlaymode();
        }

        private static int CountRecords(string path)
        {
            return File.Exists(path) ? File.ReadLines(path).Count() : 0;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void CaptureCurrentFrame(string outputPath)
        {
            Camera camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("Teacher camera is missing in play mode.");
            }

            Render(camera, outputPath);
        }

        private static void CaptureAvatarFrame(NpcPerformance focal, string outputPath)
        {
            Camera camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("Teacher camera is missing in play mode.");
            }

            Vector3 savedPosition = camera.transform.position;
            Quaternion savedRotation = camera.transform.rotation;
            float savedFieldOfView = camera.fieldOfView;
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>();
            bool[] canvasStates = canvases.Select(canvas => canvas.enabled).ToArray();
            foreach (Canvas canvas in canvases)
            {
                canvas.enabled = false;
            }

            NpcPerformance[] students = UnityEngine.Object.FindObjectsByType<NpcPerformance>();
            NpcPerformance[] hiddenStudents = students.Where(student => student != focal && student.gameObject.activeSelf).ToArray();
            foreach (NpcPerformance student in hiddenStudents)
            {
                student.gameObject.SetActive(false);
            }

            float focusDistance = focal.UprightEyeContact ? 1.92f : 1.72f;
            Animator animator = focal.GetComponentInChildren<Animator>();
            Transform head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            float fallbackHeight = focal.UprightEyeContact ? 1.66f : 1.34f;
            Vector3 focus = head != null
                ? head.position + Vector3.up * 0.02f
                : focal.transform.position + Vector3.up * fallbackHeight;
            camera.transform.position = focus + focal.transform.forward * focusDistance;
            camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position);
            camera.fieldOfView = 32f;
            Render(camera, outputPath);

            camera.transform.position = savedPosition;
            camera.transform.rotation = savedRotation;
            camera.fieldOfView = savedFieldOfView;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].enabled = canvasStates[i];
            }
            foreach (NpcPerformance student in hiddenStudents)
            {
                student.gameObject.SetActive(true);
            }
        }

        private static void Render(Camera camera, string outputPath)
        {

            const int width = 3200;
            const int height = 1800;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            LightShadows[] shadowModes = lights.Select(light => light.shadows).ToArray();
            Texture2D image = null;
            try
            {
                foreach (Light light in lights)
                {
                    light.shadows = LightShadows.None;
                }

                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "Logs/VisualQa");
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                for (int i = 0; i < lights.Length; i++)
                {
                    lights[i].shadows = shadowModes[i];
                }
                if (image != null)
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            Debug.Log($"PLAYMODE_CAPTURE_OK {outputPath} {width}x{height}");
        }
    }
}
