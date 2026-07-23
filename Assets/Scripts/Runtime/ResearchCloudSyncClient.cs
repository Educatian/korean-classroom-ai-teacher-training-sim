using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    internal sealed class ResearchQueuedSessionBundle
    {
        public ResearchSessionStartPayload start = new ResearchSessionStartPayload();
        public ResearchEventBatchPayload events = new ResearchEventBatchPayload();
        public ResearchSessionCompletionPayload completion =
            new ResearchSessionCompletionPayload();
        public string rawGazePath = string.Empty;
    }

    [DisallowMultipleComponent]
    public sealed class ResearchCloudSyncClient : MonoBehaviour
    {
        [SerializeField] private ResearchCloudSyncSettings settings;

        private string sessionAuthorization;
        private string participantCode;
        private bool rawGazeConsent;
        private bool uploadInProgress;

        public bool UploadInProgress => uploadInProgress;
        public int PendingUploadCount => CountPendingManifests();

        public bool IsConfigured =>
            settings != null &&
            settings.IsConfigured &&
            !string.IsNullOrWhiteSpace(sessionAuthorization);

        private void Awake()
        {
            if (settings == null)
            {
                settings = ResearchCloudSyncSettings.LoadDefault();
            }
        }

        public void SetSessionAuthorization(
            string bearerToken,
            string pseudonymousParticipantCode,
            bool consentToRawGaze)
        {
            sessionAuthorization = string.IsNullOrWhiteSpace(bearerToken)
                ? null
                : bearerToken.Trim();
            participantCode = string.IsNullOrWhiteSpace(pseudonymousParticipantCode)
                ? string.Empty
                : pseudonymousParticipantCode.Trim();
            rawGazeConsent = consentToRawGaze;
            if (IsConfigured && !uploadInProgress)
            {
                StartCoroutine(ResumePendingUploads());
            }
        }

        public void RegisterActiveSession(
            string sessionId,
            string scenarioId,
            string startedAtUtc)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            var start = new ResearchSessionStartPayload
            {
                sessionId = sessionId,
                participantCode = participantCode,
                scenarioId = scenarioId ?? string.Empty,
                startedAtUtc = startedAtUtc,
                deviceModel = SystemInfo.deviceModel,
                buildVersion = Application.version,
                rawGazeConsent = rawGazeConsent
            };
            StartCoroutine(RegisterActiveSession(start));
        }

        private IEnumerator RegisterActiveSession(ResearchSessionStartPayload start)
        {
            yield return SendJson(
                UnityWebRequest.kHttpVerbPOST,
                "/v1/sessions",
                JsonUtility.ToJson(start),
                start.sessionId + "-start-v1",
                _ => { });
        }

        public void QueueCompletedSession(
            string sessionId,
            string scenarioId,
            string startedAtUtc,
            IReadOnlyList<TrainingTelemetryEvent> telemetry,
            ResearchDebriefReport report,
            string rawGazePath)
        {
            QueueSession(sessionId, scenarioId, startedAtUtc, telemetry, report, rawGazePath, "completed");
        }

        public void QueueAbortedSession(
            string sessionId,
            string scenarioId,
            string startedAtUtc,
            IReadOnlyList<TrainingTelemetryEvent> telemetry,
            ResearchDebriefReport report,
            string rawGazePath)
        {
            QueueSession(sessionId, scenarioId, startedAtUtc, telemetry, report, rawGazePath, "aborted");
        }

        private void QueueSession(
            string sessionId,
            string scenarioId,
            string startedAtUtc,
            IReadOnlyList<TrainingTelemetryEvent> telemetry,
            ResearchDebriefReport report,
            string rawGazePath,
            string status)
        {
#if !UNITY_WEBGL
            if (string.IsNullOrWhiteSpace(sessionId) || report == null) return;
            string participant = string.IsNullOrWhiteSpace(participantCode)
                ? "anon-" + sessionId.Substring(0, Mathf.Min(8, sessionId.Length))
                : participantCode;
            var bundle = new ResearchQueuedSessionBundle
            {
                start = new ResearchSessionStartPayload
                {
                    sessionId = sessionId,
                    participantCode = participant,
                    scenarioId = scenarioId ?? string.Empty,
                    startedAtUtc = startedAtUtc,
                    deviceModel = SystemInfo.deviceModel,
                    buildVersion = Application.version,
                    rawGazeConsent = rawGazeConsent
                },
                events = new ResearchEventBatchPayload
                {
                    requestId = sessionId + "-events-v1",
                    events = CopyTelemetry(telemetry)
                },
                completion = new ResearchSessionCompletionPayload
                {
                    completedAtUtc = DateTime.UtcNow.ToString("O"),
                    status = status,
                    report = report
                },
                rawGazePath = rawGazeConsent ? rawGazePath ?? string.Empty : string.Empty
            };

            try
            {
                string directory = QueueDirectory();
                Directory.CreateDirectory(directory);
                File.WriteAllText(
                    Path.Combine(directory, sessionId + ".json"),
                    JsonUtility.ToJson(bundle),
                    new UTF8Encoding(false));
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Research cloud queue write failed: " + exception.GetType().Name);
                return;
            }

            if (IsConfigured && !uploadInProgress)
            {
                StartCoroutine(ResumePendingUploads());
            }
#endif
        }

        private IEnumerator ResumePendingUploads()
        {
#if !UNITY_WEBGL
            uploadInProgress = true;
            string[] manifests;
            try
            {
                string directory = QueueDirectory();
                manifests = Directory.Exists(directory)
                    ? Directory.GetFiles(directory, "*.json")
                    : Array.Empty<string>();
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Research cloud queue scan failed: " + exception.GetType().Name);
                uploadInProgress = false;
                yield break;
            }

            foreach (string manifestPath in manifests)
            {
                ResearchQueuedSessionBundle bundle;
                try
                {
                    bundle = JsonUtility.FromJson<ResearchQueuedSessionBundle>(
                        File.ReadAllText(manifestPath, Encoding.UTF8));
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is UnauthorizedAccessException ||
                    exception is ArgumentException)
                {
                    Debug.LogWarning("Research cloud manifest read failed: " + exception.GetType().Name);
                    continue;
                }

                if (bundle == null || !IsConfigured) continue;
                bool succeeded = false;
                yield return UploadBundle(bundle, value => succeeded = value);
                if (!succeeded) continue;
                try
                {
                    File.Delete(manifestPath);
                }
                catch (Exception exception) when (
                    exception is IOException || exception is UnauthorizedAccessException)
                {
                    Debug.LogWarning("Research cloud manifest cleanup failed: " + exception.GetType().Name);
                }
            }

            uploadInProgress = false;
#else
            yield break;
#endif
        }

        private IEnumerator UploadBundle(
            ResearchQueuedSessionBundle bundle,
            Action<bool> completed)
        {
            bool requestSucceeded = false;
            yield return SendJson(
                UnityWebRequest.kHttpVerbPOST,
                "/v1/sessions",
                JsonUtility.ToJson(bundle.start),
                bundle.start.sessionId + "-start-v1",
                value => requestSucceeded = value);
            if (!requestSucceeded)
            {
                completed(false);
                yield break;
            }

            if (bundle.events.events.Length > 0)
            {
                yield return SendJson(
                    UnityWebRequest.kHttpVerbPOST,
                    "/v1/sessions/" + bundle.start.sessionId + "/events",
                    JsonUtility.ToJson(bundle.events),
                    bundle.events.requestId,
                    value => requestSucceeded = value);
                if (!requestSucceeded)
                {
                    completed(false);
                    yield break;
                }
            }

            if (bundle.start.rawGazeConsent &&
                settings.UploadRawGaze &&
                File.Exists(bundle.rawGazePath))
            {
                ResearchRawGazeDescriptor descriptor;
                try
                {
                    descriptor = ResearchRawGazeDescriptor.FromFile(bundle.rawGazePath);
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is UnauthorizedAccessException ||
                    exception is ArgumentException)
                {
                    Debug.LogWarning("Raw gaze preparation failed: " + exception.GetType().Name);
                    completed(false);
                    yield break;
                }

                if (descriptor.sampleCount > 0)
                {
                    yield return SendRawGaze(
                        bundle.start.sessionId,
                        descriptor,
                        value => requestSucceeded = value);
                    if (!requestSucceeded)
                    {
                        completed(false);
                        yield break;
                    }
                }
            }

            yield return SendJson(
                UnityWebRequest.kHttpVerbPOST,
                "/v1/sessions/" + bundle.start.sessionId + "/complete",
                JsonUtility.ToJson(bundle.completion),
                bundle.start.sessionId + "-complete-v1",
                value => requestSucceeded = value);
            completed(requestSucceeded);
        }

        private IEnumerator SendJson(
            string method,
            string route,
            string json,
            string idempotencyKey,
            Action<bool> completed)
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            using var request = new UnityWebRequest(
                settings.Endpoint.TrimEnd('/') + route,
                method);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            ConfigureRequest(request, "application/json", idempotencyKey);
            yield return request.SendWebRequest();
            completed(Accepted(request));
        }

        private IEnumerator SendRawGaze(
            string sessionId,
            ResearchRawGazeDescriptor descriptor,
            Action<bool> completed)
        {
            using var request = new UnityWebRequest(
                settings.Endpoint.TrimEnd('/') + "/v1/sessions/" + sessionId + "/raw-gaze",
                UnityWebRequest.kHttpVerbPUT);
            request.uploadHandler = new UploadHandlerFile(descriptor.path);
            request.downloadHandler = new DownloadHandlerBuffer();
            ConfigureRequest(request, "application/x-ndjson", sessionId + "-raw-v1");
            request.SetRequestHeader("X-Content-Sha256", descriptor.sha256);
            request.SetRequestHeader("X-Gaze-Sample-Count", descriptor.sampleCount.ToString());
            request.SetRequestHeader("X-Gaze-Started-At", descriptor.startedAtUtc);
            request.SetRequestHeader("X-Gaze-Ended-At", descriptor.endedAtUtc);
            yield return request.SendWebRequest();
            completed(Accepted(request));
        }

        private void ConfigureRequest(
            UnityWebRequest request,
            string contentType,
            string idempotencyKey)
        {
            request.SetRequestHeader("Content-Type", contentType);
            request.SetRequestHeader("Authorization", "Bearer " + sessionAuthorization);
            request.SetRequestHeader("X-Client-Id", settings.ClientId);
            request.SetRequestHeader("Idempotency-Key", idempotencyKey);
            request.timeout = settings.TimeoutSeconds;
        }

        private static bool Accepted(UnityWebRequest request)
        {
            bool success = request.result == UnityWebRequest.Result.Success &&
                           request.responseCode >= 200 &&
                           request.responseCode < 300;
            if (!success)
            {
                Debug.LogWarning(
                    "Research cloud upload retained for retry: " +
                    request.responseCode + " " + request.error);
            }
            return success;
        }

        private static TrainingTelemetryEvent[] CopyTelemetry(
            IReadOnlyList<TrainingTelemetryEvent> telemetry)
        {
            if (telemetry == null || telemetry.Count == 0)
            {
                return Array.Empty<TrainingTelemetryEvent>();
            }

            var result = new TrainingTelemetryEvent[telemetry.Count];
            for (int index = 0; index < telemetry.Count; index++)
            {
                result[index] = telemetry[index];
            }
            return result;
        }

        private static string QueueDirectory()
        {
            return Path.Combine(Application.persistentDataPath, "research-upload-queue");
        }

        private static int CountPendingManifests()
        {
#if UNITY_WEBGL
            return 0;
#else
            try
            {
                string directory = QueueDirectory();
                return Directory.Exists(directory)
                    ? Directory.GetFiles(directory, "*.json").Length
                    : 0;
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                return -1;
            }
#endif
        }
    }
}
