using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class EyeTrackingResearchTests
    {
        [Test]
        public void GazeAccumulator_SummarizesRelevantStudentAttention()
        {
            var accumulator = new GazeMetricAccumulator(100, 120f);
            accumulator.BeginCue(0d);
            accumulator.AddSample(Sample(0.00, GazeAoiCategory.FocalStudentEyes, true, true));
            accumulator.AddSample(Sample(0.05, GazeAoiCategory.FocalStudentEyes, true, true));
            accumulator.AddSample(Sample(0.10, GazeAoiCategory.FocalStudentEyes, true, true));
            accumulator.AddSample(Sample(0.15, GazeAoiCategory.FocalStudentHands, true, false));
            accumulator.AddSample(Sample(0.20, GazeAoiCategory.Hud, true, false));

            TeacherGazeSummary summary = accumulator.Complete("action-1", 0.25d);

            Assert.That(summary.validSampleRatio, Is.EqualTo(1f).Within(0.001f));
            Assert.That(summary.firstRelevantFixationMilliseconds, Is.EqualTo(0));
            Assert.That(summary.focalStudentDwellMilliseconds, Is.GreaterThanOrEqualTo(150));
            Assert.That(summary.mutualGazeMilliseconds, Is.GreaterThan(0));
            Assert.That(summary.fixationCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(summary.missedRelevantCue, Is.False);
        }

        [Test]
        public void GazeAccumulator_MarksCueMissedWhenOnlyInterfaceWasViewed()
        {
            var accumulator = new GazeMetricAccumulator(100, 120f);
            accumulator.BeginCue(1d);
            accumulator.AddSample(Sample(1.00, GazeAoiCategory.Hud, true, false));
            accumulator.AddSample(Sample(1.10, GazeAoiCategory.Hud, true, false));

            TeacherGazeSummary summary = accumulator.Complete("action-2", 1.30d);

            Assert.That(summary.missedRelevantCue, Is.True);
            Assert.That(summary.firstRelevantFixationMilliseconds, Is.EqualTo(-1));
            Assert.That(summary.hudDwellMilliseconds, Is.GreaterThan(0));
        }

        [Test]
        public void DevicePolicy_RecognizesHeadGazeOnlyAndEyeTrackingQuestModels()
        {
            Assert.That(EyeTrackingDevicePolicy.IsKnownHeadGazeOnlyQuest("Oculus Quest 2"), Is.True);
            Assert.That(EyeTrackingDevicePolicy.IsKnownHeadGazeOnlyQuest("Meta Quest 2"), Is.True);
            Assert.That(EyeTrackingDevicePolicy.IsKnownHeadGazeOnlyQuest("Meta Quest 3"), Is.True);
            Assert.That(EyeTrackingDevicePolicy.IsKnownHeadGazeOnlyQuest("Meta Quest 3S"), Is.True);
            Assert.That(EyeTrackingDevicePolicy.IsKnownHeadGazeOnlyQuest("Meta Quest Pro"), Is.False);
        }

        [Test]
        public void GazeAccumulator_PreservesHeadGazeFallbackSource()
        {
            var accumulator = new GazeMetricAccumulator(100, 120f);
            accumulator.BeginCue(0d);
            GazeResearchSample sample = Sample(0d, GazeAoiCategory.FocalStudentFace, true, false);
            sample.trackingSource = EyeTrackingSource.HeadGazeFallback;
            accumulator.AddSample(sample);

            TeacherGazeSummary summary = accumulator.Complete("fallback-action", 0.1d);

            Assert.That(summary.trackingSource, Is.EqualTo(EyeTrackingSource.HeadGazeFallback));
        }

        [Test]
        public void TelemetryEvent_JsonRoundTripPreservesGazeSummary()
        {
            var source = new TrainingTelemetryEvent
            {
                gaze = new TeacherGazeSummary
                {
                    actionId = "turn-7",
                    trackingSource = EyeTrackingSource.EyeGaze,
                    validSampleRatio = 0.92f,
                    focalStudentDwellMilliseconds = 820,
                    firstRelevantFixationMilliseconds = 140
                }
            };

            TrainingTelemetryEvent restored =
                JsonUtility.FromJson<TrainingTelemetryEvent>(JsonUtility.ToJson(source));

            Assert.That(restored.schemaVersion, Is.EqualTo(TrainingTelemetryEvent.CurrentSchemaVersion));
            Assert.That(restored.gaze.actionId, Is.EqualTo("turn-7"));
            Assert.That(restored.gaze.trackingSource, Is.EqualTo(EyeTrackingSource.EyeGaze));
            Assert.That(restored.gaze.validSampleRatio, Is.EqualTo(0.92f).Within(0.001f));
        }

        [Test]
        public void Debrief_AggregatesOnlyLiveEyeGazeActions()
        {
            var live = new TrainingTelemetryEvent
            {
                sessionId = "session",
                kind = TrainingEventKind.TeacherAction,
                gaze = new TeacherGazeSummary
                {
                    trackingSource = EyeTrackingSource.EyeGaze,
                    validSampleRatio = 0.9f,
                    firstRelevantFixationMilliseconds = 120,
                    focalStudentDwellMilliseconds = 700
                }
            };
            var fallback = new TrainingTelemetryEvent
            {
                sessionId = "session",
                kind = TrainingEventKind.TeacherAction,
                gaze = new TeacherGazeSummary
                {
                    trackingSource = EyeTrackingSource.HeadGazeFallback,
                    validSampleRatio = 1f,
                    focalStudentDwellMilliseconds = 900
                }
            };

            ResearchDebriefReport report =
                EcdAssessmentEngine.Evaluate(new[] { live, fallback }, EcdAssessmentModel.CreateRuntimeDefault());

            Assert.That(report.gaze.actionsWithEyeTracking, Is.EqualTo(1));
            Assert.That(report.gaze.actionsWithHeadGazeFallback, Is.EqualTo(1));
            Assert.That(report.gaze.averageValidSampleRatio, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(report.gaze.totalFocalStudentDwellMilliseconds, Is.EqualTo(700));
        }

        private static GazeResearchSample Sample(double seconds, GazeAoiCategory category,
            bool valid, bool mutual)
        {
            return new GazeResearchSample
            {
                monotonicSeconds = seconds,
                trackingValid = valid,
                trackingSource = EyeTrackingSource.EyeGaze,
                category = category,
                studentLookingAtTeacher = mutual,
                direction = Vector3.forward
            };
        }
    }
}
