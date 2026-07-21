using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class QuestProEyeGazeProvider : MonoBehaviour
    {
        public const string AndroidEyeTrackingPermission = "com.oculus.permission.EYE_TRACKING";
        private InputAction position;
        private InputAction rotation;
        private InputAction tracked;
        private Transform trackingSpace;
        private Camera fallbackCamera;
        private bool allowHeadFallback;
        private bool requireLiveEyeTracking;
        private bool useEyeTracking;

        public EyeTrackingRuntimeStatus Status { get; private set; } = EyeTrackingRuntimeStatus.TrackingUnavailable;
        public EyeTrackingSource ActiveSource { get; private set; } = EyeTrackingSource.Unavailable;

        public void Configure(Transform origin, Camera camera, bool fallbackAllowed)
        {
            trackingSpace = origin;
            fallbackCamera = camera;
            useEyeTracking = !EyeTrackingDevicePolicy.IsKnownHeadGazeOnlyQuest(SystemInfo.deviceModel);
            requireLiveEyeTracking = EyeTrackingResearchSettings.RequireLiveEyeTracking;
            allowHeadFallback = fallbackAllowed && !requireLiveEyeTracking;
            position = new InputAction("Eye Gaze Position", InputActionType.Value,
                "<EyeGaze>/pose/position", expectedControlType: "Vector3");
            rotation = new InputAction("Eye Gaze Rotation", InputActionType.Value,
                "<EyeGaze>/pose/rotation", expectedControlType: "Quaternion");
            tracked = new InputAction("Eye Gaze Tracked", InputActionType.Button,
                "<EyeGaze>/pose/isTracked");
            position.Enable();
            rotation.Enable();
            tracked.Enable();
            if (useEyeTracking)
            {
                RequestPermissionIfNeeded();
            }
        }

        public bool TryGetRay(out Ray ray, out EyeTrackingSource source)
        {
            bool eyeTracked = useEyeTracking && HasPermission() && tracked != null && tracked.IsPressed();
            if (eyeTracked)
            {
                Vector3 localPosition = position.ReadValue<Vector3>();
                Quaternion localRotation = rotation.ReadValue<Quaternion>();
                Vector3 worldPosition = trackingSpace != null
                    ? trackingSpace.TransformPoint(localPosition)
                    : localPosition;
                Vector3 worldDirection = trackingSpace != null
                    ? trackingSpace.TransformDirection(localRotation * Vector3.forward)
                    : localRotation * Vector3.forward;
                ray = new Ray(worldPosition, worldDirection.normalized);
                source = EyeTrackingSource.EyeGaze;
                ActiveSource = source;
                Status = EyeTrackingRuntimeStatus.Tracking;
                return true;
            }

            if (!HasPermission())
            {
                Status = EyeTrackingRuntimeStatus.PermissionDenied;
            }
            else
            {
                Status = EyeTrackingRuntimeStatus.TrackingUnavailable;
            }

            if (allowHeadFallback && fallbackCamera != null)
            {
                ray = new Ray(fallbackCamera.transform.position, fallbackCamera.transform.forward);
                source = EyeTrackingSource.HeadGazeFallback;
                ActiveSource = source;
                Status = EyeTrackingRuntimeStatus.HeadGazeFallback;
                return true;
            }

            ray = default;
            source = EyeTrackingSource.Unavailable;
            ActiveSource = source;
            return false;
        }

        private static bool HasPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission(AndroidEyeTrackingPermission);
#else
            return true;
#endif
        }

        private void RequestPermissionIfNeeded()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(AndroidEyeTrackingPermission))
            {
                Status = EyeTrackingRuntimeStatus.PermissionRequired;
                UnityEngine.Android.Permission.RequestUserPermission(AndroidEyeTrackingPermission);
            }
#endif
        }

        private void OnDestroy()
        {
            position?.Dispose();
            rotation?.Dispose();
            tracked?.Dispose();
        }
    }

    [DisallowMultipleComponent]
    public sealed class GazeAoiTarget : MonoBehaviour
    {
        [SerializeField] private string stableId;
        [SerializeField] private string actorId;
        [SerializeField] private GazeAoiCategory category;

        public string StableId => stableId;
        public string ActorId => actorId;
        public GazeAoiCategory Category => category;

        public void Configure(string id, string actor, GazeAoiCategory value)
        {
            stableId = id ?? string.Empty;
            actorId = actor ?? string.Empty;
            category = value;
        }
    }

    public static class StudentGazeAoiInstaller
    {
        public static void Install(NpcPerformance student, string actorId, bool focal)
        {
            if (student == null) return;
            Animator animator = student.GetComponentInChildren<Animator>();
            Transform head = Bone(animator, HumanBodyBones.Head, student.transform);
            Transform chest = Bone(animator, HumanBodyBones.Chest, student.transform);
            Transform leftHand = Bone(animator, HumanBodyBones.LeftHand, student.transform);
            Transform rightHand = Bone(animator, HumanBodyBones.RightHand, student.transform);

            if (!focal)
            {
                AddSphere(head, actorId + ".peer", actorId, GazeAoiCategory.PeerStudent,
                    Vector3.zero, 0.16f);
                return;
            }

            AddSphere(head, actorId + ".eyes", actorId, GazeAoiCategory.FocalStudentEyes,
                new Vector3(0f, 0.035f, 0.10f), 0.075f);
            AddSphere(head, actorId + ".face", actorId, GazeAoiCategory.FocalStudentFace,
                Vector3.zero, 0.145f);
            AddSphere(head, actorId + ".mouth", actorId, GazeAoiCategory.FocalStudentMouth,
                new Vector3(0f, -0.07f, 0.12f), 0.055f);
            AddSphere(chest, actorId + ".torso", actorId, GazeAoiCategory.FocalStudentTorso,
                Vector3.zero, 0.24f);
            AddSphere(leftHand, actorId + ".left-hand", actorId, GazeAoiCategory.FocalStudentHands,
                Vector3.zero, 0.09f);
            AddSphere(rightHand, actorId + ".right-hand", actorId, GazeAoiCategory.FocalStudentHands,
                Vector3.zero, 0.09f);
            AddSphere(student.transform, actorId + ".desk", actorId, GazeAoiCategory.FocalStudentDesk,
                new Vector3(0f, 0.72f, 0.38f), 0.22f);
        }

        public static void InstallHud(Canvas canvas)
        {
            if (canvas == null || canvas.GetComponent<GazeAoiTarget>() != null) return;
            GazeAoiTarget target = canvas.gameObject.AddComponent<GazeAoiTarget>();
            target.Configure("teacher-hud", "interface", GazeAoiCategory.Hud);
            BoxCollider collider = canvas.gameObject.AddComponent<BoxCollider>();
            RectTransform rect = canvas.transform as RectTransform;
            Vector2 size = rect != null ? rect.rect.size : new Vector2(1600f, 900f);
            collider.size = new Vector3(size.x, size.y, 2f);
            collider.isTrigger = true;
        }

        public static void InstallNamedSceneTargets()
        {
            AddNamed("StudentSpeechBubble", "student-speech-bubble",
                GazeAoiCategory.SpeechBubble);
            AddNamed("OptionButton_1", "response-option-1",
                GazeAoiCategory.ResponseOptions);
            AddNamed("OptionButton_2", "response-option-2",
                GazeAoiCategory.ResponseOptions);
            AddNamed("OptionButton_3", "response-option-3",
                GazeAoiCategory.ResponseOptions);
            AddNamed("ActiveDisplaySurface", "electronic-board",
                GazeAoiCategory.ElectronicBoard);
        }

        private static void AddNamed(string objectName, string stableId,
            GazeAoiCategory category)
        {
            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject == null) return;
            GazeAoiTarget target = targetObject.GetComponent<GazeAoiTarget>();
            if (target == null) target = targetObject.AddComponent<GazeAoiTarget>();
            target.Configure(stableId, "environment", category);
            if (targetObject.GetComponent<Collider>() != null) return;

            BoxCollider collider = targetObject.AddComponent<BoxCollider>();
            RectTransform rect = targetObject.transform as RectTransform;
            if (rect != null)
            {
                collider.size = new Vector3(
                    Mathf.Max(1f, rect.rect.width),
                    Mathf.Max(1f, rect.rect.height),
                    2f);
            }
            else
            {
                Renderer renderer = targetObject.GetComponent<Renderer>();
                collider.size = renderer != null
                    ? renderer.localBounds.size
                    : Vector3.one * 0.25f;
                collider.center = renderer != null
                    ? renderer.localBounds.center
                    : Vector3.zero;
            }
            collider.isTrigger = true;
        }

        private static Transform Bone(Animator animator, HumanBodyBones bone, Transform fallback)
        {
            if (animator != null && animator.isHuman)
            {
                Transform result = animator.GetBoneTransform(bone);
                if (result != null) return result;
            }
            return fallback;
        }

        private static void AddSphere(Transform parent, string id, string actor,
            GazeAoiCategory category, Vector3 localPosition, float radius)
        {
            GameObject existing = GameObject.Find(id);
            if (existing != null) return;
            var region = new GameObject(id, typeof(SphereCollider), typeof(GazeAoiTarget));
            region.transform.SetParent(parent, false);
            region.transform.localPosition = localPosition;
            SphereCollider collider = region.GetComponent<SphereCollider>();
            collider.radius = radius;
            collider.isTrigger = true;
            region.GetComponent<GazeAoiTarget>().Configure(id, actor, category);
        }
    }

    [DisallowMultipleComponent]
    public sealed class TeacherEyeTrackingRecorder : MonoBehaviour
    {
        private readonly Dictionary<string, TeacherGazeSummary> completed =
            new Dictionary<string, TeacherGazeSummary>();
        private EyeTrackingResearchSettings settings;
        private QuestProEyeGazeProvider provider;
        private Func<StudentStateSnapshot> studentState;
        private GazeMetricAccumulator accumulator;
        private string sessionId;
        private string scenarioId;
        private int beatIndex;
        private float nextSampleTime;
        private bool cueActive;
        private bool rawGazeConsent;

        public string RawDataPath { get; private set; } = string.Empty;
        public bool RequiresLiveEyeTracking => EyeTrackingResearchSettings.RequireLiveEyeTracking;
        public bool ResearchReady => !RequiresLiveEyeTracking ||
            (provider != null && provider.ActiveSource == EyeTrackingSource.EyeGaze);
        public EyeTrackingRuntimeStatus RuntimeStatus => provider != null ? provider.Status : EyeTrackingRuntimeStatus.Unsupported;

        public void Initialize(string id, EyeTrackingResearchSettings researchSettings,
            Func<StudentStateSnapshot> captureStudentState)
        {
            sessionId = id ?? string.Empty;
            settings = researchSettings != null ? researchSettings : EyeTrackingResearchSettings.LoadDefault();
            studentState = captureStudentState;
            accumulator = new GazeMetricAccumulator(
                settings.minimumFixationMilliseconds,
                settings.maximumFixationAngularVelocity);
            RawDataPath = Path.Combine(Application.persistentDataPath,
                "eye-tracking", sessionId + "-gaze.jsonl");
        }

        public void SetRawGazeConsent(bool consent)
        {
            rawGazeConsent = consent;
        }

        public void BeginCue(string currentScenarioId, int currentBeatIndex)
        {
            scenarioId = currentScenarioId ?? string.Empty;
            beatIndex = currentBeatIndex;
            accumulator.BeginCue(Time.realtimeSinceStartupAsDouble);
            cueActive = true;
        }

        public void MarkTeacherAction(string actionId)
        {
            if (!cueActive || string.IsNullOrWhiteSpace(actionId)) return;
            completed[actionId] = accumulator.Complete(actionId, Time.realtimeSinceStartupAsDouble);
            cueActive = false;
        }

        public TeacherGazeSummary TakeSummary(string actionId)
        {
            if (actionId != null && completed.TryGetValue(actionId, out TeacherGazeSummary summary))
            {
                completed.Remove(actionId);
                return summary;
            }
            return new TeacherGazeSummary { actionId = actionId ?? string.Empty };
        }

        private void Update()
        {
            if (!cueActive || settings == null || Time.unscaledTime < nextSampleTime) return;
            nextSampleTime = Time.unscaledTime + 1f / Mathf.Max(10, settings.sampleRateHz);
            if (provider == null) provider = FindAnyObjectByType<QuestProEyeGazeProvider>();
            Ray ray = default;
            EyeTrackingSource source = EyeTrackingSource.Unavailable;
            bool tracked = provider != null && provider.TryGetRay(out ray, out source);
            StudentStateSnapshot state = studentState?.Invoke() ?? new StudentStateSnapshot();
            var sample = new GazeResearchSample
            {
                sessionId = sessionId,
                timestampUtc = DateTime.UtcNow.ToString("O"),
                monotonicSeconds = Time.realtimeSinceStartupAsDouble,
                scenarioId = scenarioId,
                beatIndex = beatIndex,
                trackingSource = tracked ? source : EyeTrackingSource.Unavailable,
                trackingValid = tracked,
                origin = tracked ? ray.origin : Vector3.zero,
                direction = tracked ? ray.direction : Vector3.zero,
                studentAffect = state.affect,
                studentGesture = state.gesture,
                studentLookingAtTeacher = state.gazeContact >= 0.75f
            };

            if (tracked && Physics.Raycast(ray, out RaycastHit hit, settings.maximumRayDistance,
                    settings.gazeLayerMask, QueryTriggerInteraction.Collide))
            {
                GazeAoiTarget target = hit.collider.GetComponentInParent<GazeAoiTarget>();
                if (target != null)
                {
                    sample.aoiId = target.StableId;
                    sample.actorId = target.ActorId;
                    sample.category = target.Category;
                    sample.hitDistance = hit.distance;
                }
            }

            accumulator.AddSample(sample);
            if (settings.persistRawGaze && rawGazeConsent) AppendRaw(sample);
        }

        private void AppendRaw(GazeResearchSample sample)
        {
#if !UNITY_WEBGL
            try
            {
                string directory = Path.GetDirectoryName(RawDataPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.AppendAllText(RawDataPath, JsonUtility.ToJson(sample) + Environment.NewLine);
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Eye tracking log write failed: " + exception.GetType().Name);
            }
#endif
        }
    }
}


