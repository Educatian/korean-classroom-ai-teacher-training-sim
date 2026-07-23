using System.Linq;
using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class QuestAcceptanceDiagnosticsTests
    {
        [Test]
        public void EditorSnapshot_NeverClaimsHeadsetAcceptance()
        {
            QuestAcceptanceReport report = QuestAcceptanceEvaluator.Evaluate(new QuestAcceptanceSnapshot
            {
                isEditor = true,
                isAndroid = false,
                xrRigActive = true,
                microphoneDeviceAvailable = true,
                microphonePermissionGranted = true,
                secureProxyAuthorized = true,
                researchCloudAuthorized = true,
                gazeStatus = EyeTrackingRuntimeStatus.Tracking,
                rawGazeConsent = true,
                rawGazeFileObserved = true,
                runtimeSeconds = 1200d
            });

            Assert.That(report.overallState, Is.EqualTo(QuestAcceptanceState.NotObserved));
            Assert.That(Check(report, QuestAcceptanceCheckId.AndroidRuntime).state,
                Is.EqualTo(QuestAcceptanceState.RequiresDevice));
            Assert.That(Check(report, QuestAcceptanceCheckId.GazeTracking).state,
                Is.EqualTo(QuestAcceptanceState.RequiresDevice));
        }

        [Test]
        public void QuestTwoHeadFallback_IsAcceptedButExplicitlyDegraded()
        {
            QuestAcceptanceReport report = QuestAcceptanceEvaluator.Evaluate(ReadyDevice(
                EyeTrackingRuntimeStatus.HeadGazeFallback,
                false));

            Assert.That(Check(report, QuestAcceptanceCheckId.GazeTracking).state,
                Is.EqualTo(QuestAcceptanceState.Degraded));
            Assert.That(report.overallState, Is.EqualTo(QuestAcceptanceState.Degraded));
        }

        [Test]
        public void QuestProResearchPolicy_FailsWithoutLiveEyeTracking()
        {
            QuestAcceptanceReport report = QuestAcceptanceEvaluator.Evaluate(ReadyDevice(
                EyeTrackingRuntimeStatus.HeadGazeFallback,
                true));

            Assert.That(Check(report, QuestAcceptanceCheckId.GazeTracking).state,
                Is.EqualTo(QuestAcceptanceState.Failed));
            Assert.That(report.overallState, Is.EqualTo(QuestAcceptanceState.Failed));
        }

        [Test]
        public void PendingCloudBundles_AreVisibleAsDegraded()
        {
            QuestAcceptanceSnapshot snapshot = ReadyDevice(EyeTrackingRuntimeStatus.Tracking, true);
            snapshot.pendingCloudUploads = 3;

            QuestAcceptanceReport report = QuestAcceptanceEvaluator.Evaluate(snapshot);

            QuestAcceptanceCheck cloud = Check(report, QuestAcceptanceCheckId.ResearchCloud);
            Assert.That(cloud.state, Is.EqualTo(QuestAcceptanceState.Degraded));
            StringAssert.Contains("3", cloud.detail);
        }

        [Test]
        public void RawGazeWithoutConsent_IsNotTreatedAsFailure()
        {
            QuestAcceptanceSnapshot snapshot = ReadyDevice(EyeTrackingRuntimeStatus.Tracking, true);
            snapshot.rawGazeConsent = false;
            snapshot.rawGazeFileObserved = false;

            QuestAcceptanceCheck raw = Check(
                QuestAcceptanceEvaluator.Evaluate(snapshot),
                QuestAcceptanceCheckId.RawGazeCapture);

            Assert.That(raw.state, Is.EqualTo(QuestAcceptanceState.NotObserved));
        }

        [Test]
        public void DeviceWithoutVoiceRoundTrip_RequiresUserAction()
        {
            QuestAcceptanceSnapshot snapshot = ReadyDevice(EyeTrackingRuntimeStatus.Tracking, true);
            snapshot.successfulTranscriptionObserved = false;

            QuestAcceptanceCheck transcription = Check(
                QuestAcceptanceEvaluator.Evaluate(snapshot),
                QuestAcceptanceCheckId.SpeechTranscriptionRoundTrip);

            Assert.That(transcription.state, Is.EqualTo(QuestAcceptanceState.RequiresUserAction));
        }

        private static QuestAcceptanceSnapshot ReadyDevice(
            EyeTrackingRuntimeStatus gaze,
            bool requiresLiveEyeTracking)
        {
            return new QuestAcceptanceSnapshot
            {
                isAndroid = true,
                deviceModel = requiresLiveEyeTracking ? "Meta Quest Pro" : "Meta Quest 2",
                xrRigActive = true,
                microphoneDeviceAvailable = true,
                microphonePermissionGranted = true,
                secureProxyAuthorized = true,
                researchCloudAuthorized = true,
                pendingCloudUploads = 0,
                successfulLlmDialogueObserved = true,
                successfulTranscriptionObserved = true,
                successfulSpeechSynthesisObserved = true,
                gazeStatus = gaze,
                requiresLiveEyeTracking = requiresLiveEyeTracking,
                rawGazeConsent = true,
                rawGazeFileObserved = true,
                runtimeSeconds = 1200d,
                sustainedRuntimeTargetSeconds = 1200d
            };
        }

        private static QuestAcceptanceCheck Check(
            QuestAcceptanceReport report,
            QuestAcceptanceCheckId id)
        {
            return report.checks.Single(item => item.id == id);
        }
    }
}
