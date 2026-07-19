using System;
using System.IO;
using System.Linq;
using AdieLab.TeacherTraining;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining.Editor
{
    [InitializeOnLoad]
    public static class ClassmateGazePlayModeQaRunner
    {
        private const string ArmedKey = "AdieLab.TeacherTraining.ClassmateGazeQaArmed";
        private const string QuitKey = "AdieLab.TeacherTraining.ClassmateGazeQaQuit";
        private const string ScenePath = "Assets/Scenes/KoreanClassroomTraining.unity";
        private const string OutputPath = "Logs/VisualQa/Unity_Classmates_Tracking_Teacher.png";
        private const string IdleBehaviorOutputPath = "Logs/VisualQa/Unity_Npc_IdleBehaviors.png";
        private const string ClassroomIdleOutputPath = "Logs/VisualQa/Unity_Npc_IdleBehaviors_ClassroomView.png";
        private const string FemaleYawnOutputPath = "Logs/VisualQa/Unity_VisualPolish_FemaleYawn_FullBody.png";
        private const string ChinRestOutputPath = "Logs/VisualQa/Unity_VisualPolish_ChinRest_FullBody.png";
        private const string ExtremeGestureOutputPath = "Logs/VisualQa/Unity_VisualPolish_ExtremePushAway_FullBody.png";
        private static double startedAt;
        private static bool cameraMoved;
        private static Vector3[] directionsBeforeMove;

        static ClassmateGazePlayModeQaRunner()
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
            cameraMoved = false;
            directionsBeforeMove = null;
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
                startedAt = EditorApplication.timeSinceStartup;
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(QuitKey, false))
            {
                SessionState.SetBool(QuitKey, false);
                EditorApplication.Exit(0);
            }
        }

        private static void Tick()
        {
            try
            {
                double elapsed = EditorApplication.timeSinceStartup - startedAt;
                StudentGazeController[] gazes = UnityEngine.Object.FindObjectsByType<StudentGazeController>(FindObjectsSortMode.None);
                Camera teacherCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
                double requiredWait = cameraMoved ? 2.2d : 8.0d;
                if (gazes.Length != 14 || teacherCamera == null || elapsed < requiredWait)
                {
                    return;
                }

                StudentGazeController[] observed = gazes.Where(gaze => gaze.StartsAttentive).ToArray();
                StudentGazeController[] distracted = gazes.Where(gaze => !gaze.StartsAttentive).ToArray();
                NpcIdleBehaviorController[] idleBehaviors = UnityEngine.Object.FindObjectsByType<NpcIdleBehaviorController>(FindObjectsSortMode.None);
                Require(observed.Length == 14 && distracted.Length == 0,
                    $"Expected every classmate to attend to the teacher. attentive={observed.Length} distracted={distracted.Length}");
                Require(idleBehaviors.Length == 14, $"Expected an idle behavior controller on every classmate. actual={idleBehaviors.Length}");
                Require(gazes.All(gaze => gaze.GetComponent<NpcPerformance>()?.UprightEyeContact == false),
                    "Teacher-facing gaze incorrectly replaced a seated classmate gesture with the full-body UprightListen state.");
                int overheadArmPoses = gazes.Count(HasOverheadArmPose);
                Require(overheadArmPoses == 0,
                    $"Classmate gaze produced overhead or sharply folded arm poses. expected=0 actual={overheadArmPoses}");
                float initialFaceAlignment = observed.Average(gaze =>
                {
                    Vector3 toTeacher = (teacherCamera.transform.position - Head(gaze).position).normalized;
                    return Vector3.Dot(Head(gaze).up, toTeacher);
                });
                Require(initialFaceAlignment > 0.82f,
                    $"Classmates are not visibly facing the teacher. expected>0.82 actual={initialFaceAlignment:F3}");
                string[] headDownStudents = gazes
                    .Where(gaze => IsHeadDownGesture(gaze.GetComponent<NpcPerformance>()?.CurrentGesture))
                    .Select(gaze => $"{gaze.name}:{gaze.GetComponent<NpcPerformance>()?.CurrentGesture}:" +
                                    $"{gaze.GetComponent<NpcIdleBehaviorController>()?.CurrentBehavior}")
                    .ToArray();
                if (!cameraMoved)
                {
                    Require(headDownStudents.Length == 0,
                        $"A classmate still starts with a head-down gesture. expected=0 actual={headDownStudents.Length} " +
                        $"students=[{string.Join(",", headDownStudents)}]");
                }
                int fullBodyAmbientGestures = observed.Count(gaze =>
                {
                    Animator classmateAnimator = gaze.GetComponentInChildren<Animator>();
                    return classmateAnimator != null && !classmateAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle");
                });
                Require(fullBodyAmbientGestures <= 1,
                    $"Too many attentive classmates play full-body idle clips concurrently. expected<=1 actual={fullBodyAmbientGestures}");
                int idleVariety = idleBehaviors.Select(controller => controller.CurrentBehavior).Distinct().Count();
                Require(idleVariety >= 5, $"Classmate idle behavior pool is too repetitive. distinct={idleVariety}");
                int fastPassiveIdles = idleBehaviors.Count(controller =>
                {
                    Animator classmateAnimator = controller.GetComponentInChildren<Animator>();
                    return controller.CurrentBehavior != NpcIdleBehavior.Yawn &&
                           controller.CurrentBehavior != NpcIdleBehavior.ChinRest &&
                           classmateAnimator != null && classmateAnimator.speed > 0.05f;
                });
                Require(fastPassiveIdles == 0,
                    $"Passive idle clips still advance fast enough to synchronize large motions. expected=0 actual={fastPassiveIdles}");
                int gestureVariety = gazes.Select(gaze => gaze.GetComponent<NpcPerformance>()?.CurrentGesture).Distinct().Count();
                Require(gestureVariety >= 5, $"Classmate ambient gestures are still synchronized. distinct={gestureVariety}");
                foreach (StudentGazeController gaze in observed)
                {
                    gaze.BeginTeacherAttention(8f);
                }
                foreach (StudentGazeController gaze in distracted)
                {
                    gaze.BeginTeacherDistraction(8f);
                }

                if (!cameraMoved)
                {
                    Render(teacherCamera, ClassroomIdleOutputPath);
                    GameObject.Find("Classmate_Seoyeon")?.GetComponent<NpcIdleBehaviorController>()
                        ?.PlayImmediately(NpcIdleBehavior.Yawn, 6f);
                    GameObject.Find("Classmate_Sua")?.GetComponent<NpcIdleBehaviorController>()
                        ?.PlayImmediately(NpcIdleBehavior.ChinRest, 8f);
                    directionsBeforeMove = observed.Select(HeadFaceDirection).ToArray();
                    teacherCamera.transform.position += new Vector3(1.35f, 0.12f, -0.45f);
                    cameraMoved = true;
                    startedAt = EditorApplication.timeSinceStartup;
                    return;
                }

                if (elapsed < 2.2d)
                {
                    return;
                }

                float meanAlignment = observed.Average(gaze =>
                {
                    Vector3 toTeacher = (teacherCamera.transform.position - Head(gaze).position).normalized;
                    return Vector3.Dot(HeadFaceDirection(gaze), toTeacher);
                });
                float meanDirectionChange = observed.Select((gaze, index) =>
                    Vector3.Angle(directionsBeforeMove[index], HeadFaceDirection(gaze))).Average();

                Require(meanAlignment > 0.82f, $"Classmate heads did not align with the moved teacher. alignment={meanAlignment:F3}");
                Require(meanDirectionChange > 2.5f, $"Classmate gaze did not respond to teacher movement. change={meanDirectionChange:F2}");
                NpcPerformance yawningStudent = GameObject.Find("Classmate_Seoyeon")?.GetComponent<NpcPerformance>();
                NpcIdleBehaviorController chinRestStudent = GameObject.Find("Classmate_Sua")?.GetComponent<NpcIdleBehaviorController>();
                Require(yawningStudent != null && yawningStudent.GetActionUnit(FacialActionUnit.AU26JawDrop) > 0.8f,
                    "Yawn idle behavior did not drive the jaw-drop action unit.");
                Require(chinRestStudent != null && chinRestStudent.CurrentBehavior == NpcIdleBehavior.ChinRest,
                    "Chin-rest idle behavior did not remain active for the capture.");
                ChinRestDeskContactController chinRestContact = chinRestStudent.ChinRestContact;
                Require(chinRestContact != null && chinRestContact.HasDeskSurface,
                    "Chin-rest pose did not resolve a supporting student desk.");
                Require(chinRestContact.ContactWeight > 0.95f,
                    $"Chin-rest desk contact did not blend in. weight={chinRestContact.ContactWeight:F3}");
                Require(chinRestContact.HandChinGap < 0.12f,
                    $"Chin-rest hand remains too far from the chin. gap={chinRestContact.HandChinGap:F3}");
                Require(chinRestContact.ElbowDeskGap < 0.06f,
                    $"Chin-rest elbow remains too far from the desk. gap={chinRestContact.ElbowDeskGap:F3} desktopLocal={chinRestContact.ElbowDesktopLocal}");
                Require(chinRestContact.ElbowOverDesktop,
                    $"Chin-rest elbow remains outside the usable desktop inset. desktopLocal={chinRestContact.ElbowDesktopLocal}");

                Button modeButton = GameObject.Find("ModeButton_2")?.GetComponent<Button>();
                TeacherFootstepAudio footsteps = teacherCamera.GetComponent<TeacherFootstepAudio>();
                Require(modeButton != null && footsteps != null, "Classroom audio feedback components are missing.");
                modeButton.onClick.Invoke();
                footsteps.PlayStep();
                Require(modeButton.GetComponent<AudioSource>()?.isPlaying == true, "Button click audio did not play.");
                Require(footsteps.IsPlaying && footsteps.PlayedStepCount == 1, "Teacher footstep audio did not play.");

                Render(teacherCamera, OutputPath);
                Render(teacherCamera, IdleBehaviorOutputPath);
                RenderStudentFrame(teacherCamera, yawningStudent, FemaleYawnOutputPath);
                NpcPerformance chinRestPerformance = chinRestStudent.GetComponent<NpcPerformance>();
                Require(chinRestPerformance != null, "Chin-rest student performance component is missing.");
                RenderStudentFrame(teacherCamera, chinRestPerformance, ChinRestOutputPath);

                yawningStudent.SetGesture(BehaviorGesture.PushAway, 0.78f);
                Animator extremeAnimator = yawningStudent.GetComponentInChildren<Animator>();
                Require(extremeAnimator != null, "Extreme-gesture student animator is missing.");
                extremeAnimator.Play("PushAway", 0, 0.42f);
                extremeAnimator.Update(0f);
                RenderStudentFrame(teacherCamera, yawningStudent, ExtremeGestureOutputPath);
                Debug.Log($"CLASSMATE_GAZE_QA_OK students={gazes.Length} attentive={observed.Length} gestures={gestureVariety} idleBehaviors={idleVariety} alignment={meanAlignment:F3} directionChange={meanDirectionChange:F2}");
                Debug.Log($"NPC_IDLE_BEHAVIOR_QA_OK yawn=AU26 chinRest=desk-contact handGap={chinRestContact.HandChinGap:F3} elbowGap={chinRestContact.ElbowDeskGap:F3} elbowLocal={chinRestContact.ElbowDesktopLocal} concurrentFullBody<=1");
                Debug.Log("CLASSROOM_AUDIO_QA_OK buttonClick=playing footstep=playing movementBinding=keyboard");
                Finish(true);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(false);
            }
        }

        private static Transform Head(StudentGazeController gaze)
        {
            Animator animator = gaze.GetComponentInChildren<Animator>();
            Transform head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            return head != null ? head : throw new InvalidOperationException($"Head bone missing on {gaze.name}.");
        }

        private static Vector3 HeadFaceDirection(StudentGazeController gaze)
        {
            return Head(gaze).up;
        }

        private static bool HasOverheadArmPose(StudentGazeController gaze)
        {
            Animator animator = gaze.GetComponentInChildren<Animator>();
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            return leftHand.position.y > head.position.y + 0.03f ||
                   rightHand.position.y > head.position.y + 0.03f;
        }

        private static bool IsHeadDownGesture(BehaviorGesture? gesture)
        {
            return gesture == BehaviorGesture.AvoidGaze ||
                   gesture == BehaviorGesture.Withdraw ||
                   gesture == BehaviorGesture.Shield;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Render(Camera camera, string outputPath)
        {
            const int width = 2560;
            const int height = 1440;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "Logs/VisualQa");
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }

        private static void RenderStudentFrame(Camera camera, NpcPerformance student, string outputPath)
        {
            Vector3 savedPosition = camera.transform.position;
            Quaternion savedRotation = camera.transform.rotation;
            float savedFieldOfView = camera.fieldOfView;
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            bool[] canvasStates = canvases.Select(canvas => canvas.enabled).ToArray();
            foreach (Canvas canvas in canvases)
            {
                canvas.enabled = false;
            }

            NpcPerformance[] students = UnityEngine.Object.FindObjectsByType<NpcPerformance>(FindObjectsSortMode.None);
            NpcPerformance[] hiddenStudents = students
                .Where(candidate => candidate != student && candidate.gameObject.activeSelf)
                .ToArray();
            foreach (NpcPerformance hiddenStudent in hiddenStudents)
            {
                hiddenStudent.gameObject.SetActive(false);
            }

            Vector3 focus = student.transform.position + Vector3.up * 0.92f;
            Vector3 cameraPosition = Vector3.Lerp(focus, savedPosition, 0.52f);
            Vector3 framingOffset = cameraPosition - focus;
            if (framingOffset.magnitude > 3.2f)
            {
                cameraPosition = focus + framingOffset.normalized * 3.2f;
            }
            cameraPosition.y = Mathf.Max(cameraPosition.y, focus.y + 0.85f);
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position, Vector3.up);
            camera.fieldOfView = 36f;
            Render(camera, outputPath);

            camera.transform.position = savedPosition;
            camera.transform.rotation = savedRotation;
            camera.fieldOfView = savedFieldOfView;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].enabled = canvasStates[i];
            }
            foreach (NpcPerformance hiddenStudent in hiddenStudents)
            {
                hiddenStudent.gameObject.SetActive(true);
            }
        }
        private static void Finish(bool success)
        {
            EditorApplication.update -= Tick;
            SessionState.SetBool(ArmedKey, false);
            SessionState.SetBool(QuitKey, true);
            EditorApplication.ExitPlaymode();
            if (!success)
            {
                EditorApplication.delayCall += () => EditorApplication.Exit(1);
            }
        }
    }
}
