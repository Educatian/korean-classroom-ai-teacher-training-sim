using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Collects device-observable acceptance evidence without treating Editor
    /// simulation as headset evidence. Reports contain no bearer tokens or PII.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestAcceptanceMonitor : MonoBehaviour
    {
        private const float PollIntervalSeconds = 2f;
        private const float PersistIntervalSeconds = 10f;

        [SerializeField, Min(1f)] private float sustainedRuntimeTargetMinutes = 20f;

        private SimulationController controller;
        private ResearchAutomaticSessionBootstrap authorization;
        private ResearchCloudSyncClient cloudSync;
        private SecureProxyLlmGateway secureProxy;
        private TeacherEyeTrackingRecorder eyeTracking;
        private TrainingExperienceModeController experienceMode;
        private float nextPollAt;
        private float nextPersistAt;

        public QuestAcceptanceReport LatestReport { get; private set; }
        public string ReportPath => Path.Combine(
            Application.persistentDataPath,
            "quest-acceptance",
            "latest.json");

        public void Initialize(SimulationController simulationController)
        {
            controller = simulationController;
            authorization = controller != null
                ? controller.GetComponent<ResearchAutomaticSessionBootstrap>()
                : null;
            cloudSync = controller != null
                ? controller.GetComponent<ResearchCloudSyncClient>()
                : null;
            secureProxy = controller != null
                ? controller.GetComponent<SecureProxyLlmGateway>()
                : null;
            eyeTracking = controller != null
                ? controller.GetComponent<TeacherEyeTrackingRecorder>()
                : null;
            RefreshDependencies();
            Poll(true);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextPollAt) return;
            Poll(Time.unscaledTime >= nextPersistAt);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Poll(true);
        }

        private void OnApplicationQuit()
        {
            Poll(true);
        }

        public QuestAcceptanceReport CaptureNow()
        {
            Poll(true);
            return LatestReport;
        }

        private void Poll(bool persist)
        {
            nextPollAt = Time.unscaledTime + PollIntervalSeconds;
            if (experienceMode == null || eyeTracking == null) RefreshDependencies();

            string rawPath = eyeTracking != null ? eyeTracking.RawDataPath : string.Empty;
            var snapshot = new QuestAcceptanceSnapshot
            {
                isEditor = Application.isEditor,
                isAndroid = Application.platform == RuntimePlatform.Android,
                deviceModel = SystemInfo.deviceModel,
                xrRigActive = experienceMode != null && experienceMode.IsXrActive,
                microphoneDeviceAvailable = VoiceDialogueController.HasInputDevice,
                microphonePermissionGranted = VoiceDialogueController.HasMicrophonePermission,
                secureProxyAuthorized = secureProxy != null && secureProxy.IsConfigured,
                researchCloudAuthorized = authorization != null && authorization.IsAuthorized &&
                                          cloudSync != null && cloudSync.IsConfigured,
                pendingCloudUploads = cloudSync != null ? cloudSync.PendingUploadCount : -1,
                successfulLlmDialogueObserved = secureProxy != null && secureProxy.HasSuccessfulStudentTurn,
                successfulTranscriptionObserved = secureProxy != null && secureProxy.HasSuccessfulTranscription,
                successfulSpeechSynthesisObserved = secureProxy != null && secureProxy.HasSuccessfulSpeechSynthesis,
                gazeStatus = eyeTracking != null
                    ? eyeTracking.RuntimeStatus
                    : EyeTrackingRuntimeStatus.Unsupported,
                requiresLiveEyeTracking = eyeTracking != null && eyeTracking.RequiresLiveEyeTracking,
                rawGazeConsent = ResearchConsentPreferences.RawGazeConsent,
                rawGazeFileObserved = !string.IsNullOrWhiteSpace(rawPath) &&
                                      File.Exists(rawPath) && new FileInfo(rawPath).Length > 0,
                runtimeSeconds = Time.realtimeSinceStartupAsDouble,
                sustainedRuntimeTargetSeconds = Math.Max(60d, sustainedRuntimeTargetMinutes * 60d)
            };
            LatestReport = QuestAcceptanceEvaluator.Evaluate(snapshot);
            LatestReport.buildVersion = Application.version;
            if (persist)
            {
                Persist(LatestReport);
                nextPersistAt = Time.unscaledTime + PersistIntervalSeconds;
            }
        }

        private void RefreshDependencies()
        {
            if (controller == null) controller = GetComponent<SimulationController>();
            if (authorization == null) authorization = GetComponent<ResearchAutomaticSessionBootstrap>();
            if (cloudSync == null) cloudSync = GetComponent<ResearchCloudSyncClient>();
            if (secureProxy == null) secureProxy = GetComponent<SecureProxyLlmGateway>();
            if (eyeTracking == null) eyeTracking = GetComponent<TeacherEyeTrackingRecorder>();
            if (experienceMode == null) experienceMode = FindAnyObjectByType<TrainingExperienceModeController>();
        }

        private void Persist(QuestAcceptanceReport report)
        {
            if (report == null) return;
            try
            {
                string directory = Path.GetDirectoryName(ReportPath);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Quest acceptance report write failed: " + exception.GetType().Name);
            }
        }
    }
}
