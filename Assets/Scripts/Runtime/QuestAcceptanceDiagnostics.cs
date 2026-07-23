using System;
using System.Collections.Generic;

namespace AdieLab.TeacherTraining
{
    public enum QuestAcceptanceState
    {
        NotObserved = 0,
        Passed = 1,
        Degraded = 2,
        Failed = 3,
        RequiresDevice = 4,
        RequiresUserAction = 5
    }

    public enum QuestAcceptanceCheckId
    {
        AndroidRuntime = 0,
        XrRig = 1,
        Microphone = 2,
        SecureProxyAuthorization = 3,
        ResearchCloud = 4,
        GazeTracking = 5,
        RawGazeCapture = 6,
        SustainedRuntime = 7,
        LlmDialogueRoundTrip = 8,
        SpeechTranscriptionRoundTrip = 9,
        SpeechSynthesisRoundTrip = 10
    }

    [Serializable]
    public sealed class QuestAcceptanceCheck
    {
        public QuestAcceptanceCheckId id;
        public QuestAcceptanceState state;
        public string label = string.Empty;
        public string detail = string.Empty;
        public string observedAtUtc = string.Empty;
    }

    [Serializable]
    public sealed class QuestAcceptanceSnapshot
    {
        public bool isEditor;
        public bool isAndroid;
        public string deviceModel = string.Empty;
        public bool xrRigActive;
        public bool microphoneDeviceAvailable;
        public bool microphonePermissionGranted;
        public bool secureProxyAuthorized;
        public bool researchCloudAuthorized;
        public int pendingCloudUploads;
        public bool successfulLlmDialogueObserved;
        public bool successfulTranscriptionObserved;
        public bool successfulSpeechSynthesisObserved;
        public EyeTrackingRuntimeStatus gazeStatus;
        public bool requiresLiveEyeTracking;
        public bool rawGazeConsent;
        public bool rawGazeFileObserved;
        public double runtimeSeconds;
        public double sustainedRuntimeTargetSeconds = 1200d;
    }

    [Serializable]
    public sealed class QuestAcceptanceReport
    {
        public int schemaVersion = 1;
        public string generatedAtUtc = string.Empty;
        public string buildVersion = string.Empty;
        public string deviceModel = string.Empty;
        public QuestAcceptanceState overallState;
        public QuestAcceptanceCheck[] checks = Array.Empty<QuestAcceptanceCheck>();
    }

    public static class QuestAcceptanceEvaluator
    {
        public static QuestAcceptanceReport Evaluate(QuestAcceptanceSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            string now = DateTime.UtcNow.ToString("O");
            var checks = new List<QuestAcceptanceCheck>
            {
                Check(QuestAcceptanceCheckId.AndroidRuntime, "Quest Android runtime",
                    snapshot.isEditor ? QuestAcceptanceState.RequiresDevice :
                    snapshot.isAndroid ? QuestAcceptanceState.Passed : QuestAcceptanceState.Failed,
                    snapshot.isEditor ? "Editor cannot verify headset runtime." :
                    snapshot.isAndroid ? "Android player is running on the target device." : "This is not an Android player.", now),
                Check(QuestAcceptanceCheckId.XrRig, "XR teacher rig",
                    DeviceState(snapshot, snapshot.xrRigActive),
                    snapshot.xrRigActive ? "XR origin and teacher view are active." : "No active XR teacher rig was observed.", now),
                Check(QuestAcceptanceCheckId.Microphone, "Microphone permission and device",
                    snapshot.isEditor ? QuestAcceptanceState.RequiresDevice :
                    !snapshot.microphoneDeviceAvailable ? QuestAcceptanceState.Failed :
                    snapshot.microphonePermissionGranted ? QuestAcceptanceState.Passed : QuestAcceptanceState.RequiresUserAction,
                    !snapshot.microphoneDeviceAvailable ? "No microphone input device was reported." :
                    snapshot.microphonePermissionGranted ? "Microphone input and permission are available." : "The user must grant microphone permission.", now),
                Check(QuestAcceptanceCheckId.SecureProxyAuthorization, "Secure LLM proxy",
                    snapshot.secureProxyAuthorized ? QuestAcceptanceState.Passed : QuestAcceptanceState.Failed,
                    snapshot.secureProxyAuthorized ? "A short-lived research session token is active." : "No authorized secure proxy session is active.", now),
                Check(QuestAcceptanceCheckId.ResearchCloud, "Research cloud upload",
                    snapshot.researchCloudAuthorized ?
                        (snapshot.pendingCloudUploads > 0 ? QuestAcceptanceState.Degraded : QuestAcceptanceState.Passed) :
                        QuestAcceptanceState.Failed,
                    snapshot.researchCloudAuthorized ?
                        $"Cloud authorization is active; pending bundles: {Math.Max(0, snapshot.pendingCloudUploads)}." :
                        "Research cloud authorization is not active.", now),
                UserRoundTrip(QuestAcceptanceCheckId.LlmDialogueRoundTrip, "LLM dialogue round trip",
                    snapshot, snapshot.successfulLlmDialogueObserved,
                    "A validated student dialogue response was received.", now),
                UserRoundTrip(QuestAcceptanceCheckId.SpeechTranscriptionRoundTrip, "STT round trip",
                    snapshot, snapshot.successfulTranscriptionObserved,
                    "A recorded teacher utterance was transcribed successfully.", now),
                UserRoundTrip(QuestAcceptanceCheckId.SpeechSynthesisRoundTrip, "TTS round trip",
                    snapshot, snapshot.successfulSpeechSynthesisObserved,
                    "A student speech clip was synthesized and decoded successfully.", now),
                EvaluateGaze(snapshot, now),
                EvaluateRawGaze(snapshot, now),
                Check(QuestAcceptanceCheckId.SustainedRuntime, "Sustained runtime",
                    snapshot.isEditor ? QuestAcceptanceState.RequiresDevice :
                    snapshot.runtimeSeconds >= snapshot.sustainedRuntimeTargetSeconds ? QuestAcceptanceState.Passed : QuestAcceptanceState.NotObserved,
                    $"Observed {Math.Max(0d, snapshot.runtimeSeconds):0}s / target {Math.Max(1d, snapshot.sustainedRuntimeTargetSeconds):0}s.", now)
            };

            return new QuestAcceptanceReport
            {
                generatedAtUtc = now,
                deviceModel = snapshot.deviceModel ?? string.Empty,
                overallState = Overall(checks),
                checks = checks.ToArray()
            };
        }

        private static QuestAcceptanceCheck EvaluateGaze(QuestAcceptanceSnapshot snapshot, string now)
        {
            if (snapshot.isEditor)
                return Check(QuestAcceptanceCheckId.GazeTracking, "Student gaze tracking", QuestAcceptanceState.RequiresDevice, "Editor input cannot validate headset gaze.", now);
            if (snapshot.gazeStatus == EyeTrackingRuntimeStatus.Tracking)
                return Check(QuestAcceptanceCheckId.GazeTracking, "Student gaze tracking", QuestAcceptanceState.Passed, "Live eye gaze is active.", now);
            if (!snapshot.requiresLiveEyeTracking && snapshot.gazeStatus == EyeTrackingRuntimeStatus.HeadGazeFallback)
                return Check(QuestAcceptanceCheckId.GazeTracking, "Student gaze tracking", QuestAcceptanceState.Degraded, "Head-gaze fallback is active and will be labeled separately in research data.", now);
            QuestAcceptanceState state = snapshot.gazeStatus == EyeTrackingRuntimeStatus.PermissionRequired
                ? QuestAcceptanceState.RequiresUserAction
                : QuestAcceptanceState.Failed;
            return Check(QuestAcceptanceCheckId.GazeTracking, "Student gaze tracking", state, $"Tracking status: {snapshot.gazeStatus}.", now);
        }

        private static QuestAcceptanceCheck UserRoundTrip(
            QuestAcceptanceCheckId id,
            string label,
            QuestAcceptanceSnapshot snapshot,
            bool observed,
            string passedDetail,
            string now)
        {
            return Check(id, label,
                snapshot.isEditor ? QuestAcceptanceState.RequiresDevice :
                observed ? QuestAcceptanceState.Passed : QuestAcceptanceState.RequiresUserAction,
                snapshot.isEditor ? "A headset interaction is required for end-to-end evidence." :
                observed ? passedDetail : "No successful user-triggered round trip has been observed in this app run.",
                now);
        }

        private static QuestAcceptanceCheck EvaluateRawGaze(QuestAcceptanceSnapshot snapshot, string now)
        {
            if (!snapshot.rawGazeConsent)
                return Check(QuestAcceptanceCheckId.RawGazeCapture, "Raw gaze capture", QuestAcceptanceState.NotObserved, "Raw gaze consent is disabled; no raw samples should be written.", now);
            if (snapshot.isEditor)
                return Check(QuestAcceptanceCheckId.RawGazeCapture, "Raw gaze capture", QuestAcceptanceState.RequiresDevice, "A headset session is required to validate the raw gaze file.", now);
            return Check(QuestAcceptanceCheckId.RawGazeCapture, "Raw gaze capture",
                snapshot.rawGazeFileObserved ? QuestAcceptanceState.Passed : QuestAcceptanceState.NotObserved,
                snapshot.rawGazeFileObserved ? "A consented raw gaze stream was written." : "No consented raw gaze sample has been observed yet.", now);
        }

        private static QuestAcceptanceState DeviceState(QuestAcceptanceSnapshot snapshot, bool passed)
        {
            return snapshot.isEditor ? QuestAcceptanceState.RequiresDevice : passed ? QuestAcceptanceState.Passed : QuestAcceptanceState.Failed;
        }

        private static QuestAcceptanceCheck Check(QuestAcceptanceCheckId id, string label, QuestAcceptanceState state, string detail, string now)
        {
            return new QuestAcceptanceCheck { id = id, label = label, state = state, detail = detail, observedAtUtc = now };
        }

        private static QuestAcceptanceState Overall(IReadOnlyList<QuestAcceptanceCheck> checks)
        {
            bool degraded = false;
            bool incomplete = false;
            for (int index = 0; index < checks.Count; index++)
            {
                if (checks[index].state == QuestAcceptanceState.Failed) return QuestAcceptanceState.Failed;
                if (checks[index].state == QuestAcceptanceState.Degraded) degraded = true;
                if (checks[index].state == QuestAcceptanceState.RequiresDevice ||
                    checks[index].state == QuestAcceptanceState.RequiresUserAction ||
                    checks[index].state == QuestAcceptanceState.NotObserved) incomplete = true;
            }
            return incomplete ? QuestAcceptanceState.NotObserved : degraded ? QuestAcceptanceState.Degraded : QuestAcceptanceState.Passed;
        }
    }
}
