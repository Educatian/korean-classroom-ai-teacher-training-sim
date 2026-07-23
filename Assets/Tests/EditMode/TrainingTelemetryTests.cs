using System;
using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class TrainingTelemetryTests
    {
        [Test]
        public void TelemetryEvent_JsonRoundTripPreservesResearchFields()
        {
            var record = new TrainingTelemetryEvent
            {
                eventId = nameof(TelemetryEvent_JsonRoundTripPreservesResearchFields),
                sessionId = nameof(TrainingTelemetryTests),
                sequence = 4,
                timestampUtc = DateTime.UnixEpoch.ToString(),
                kind = TrainingEventKind.StudentResponse,
                actionSource = TrainingActionSource.GenerativeModel,
                phaseBefore = TrainingPhase.AwaitingStudentResponse,
                phaseAfter = TrainingPhase.ReviewingFeedback,
                teacherTextLength = 17,
                teacherTextHash = nameof(TrainingTelemetryEvent.teacherTextHash),
                studentReplyHash = nameof(TrainingTelemetryEvent.studentReplyHash),
                turnRoute = StudentTurnRoute.LocalFallback,
                turnOutcome = StudentTurnOutcome.Malformed,
                studentStateBefore = Snapshot(-0.8f, 0.9f, 0.2f),
                studentStateAfter = Snapshot(-0.3f, 0.5f, 0.6f),
                inference = new ModelPromptProvenance
                {
                    modelId = nameof(ModelPromptProvenance),
                    promptTemplateId = nameof(TrainingTelemetryEvent),
                    promptVersion = 3,
                    promptHash = nameof(ModelPromptProvenance.promptHash),
                    fallbackUsed = true,
                    fallbackReason = nameof(ModelPromptProvenance.fallbackReason),
                    latencyMilliseconds = 1234
                },
                learningSupport = new LearningSupportTelemetry
                {
                    level = LearningSupportLevel.Principle,
                    trigger = LearningSupportTrigger.ManualRequest,
                    automatic = false,
                    idleMilliseconds = 8200,
                    requestCount = 2,
                    consecutiveMisses = 1,
                    displayedMilliseconds = 3400
                }
            };

            string json = JsonUtility.ToJson(record);
            TrainingTelemetryEvent restored = JsonUtility.FromJson<TrainingTelemetryEvent>(json);

            Assert.That(restored.schemaVersion, Is.EqualTo(TrainingTelemetryEvent.CurrentSchemaVersion));
            Assert.That(restored.studentStateBefore.affect.valence, Is.EqualTo(-0.8f).Within(0.001f));
            Assert.That(restored.studentStateAfter.gazeContact, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(restored.studentStateAfter.engagement, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(restored.studentStateAfter.trust, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(restored.inference.fallbackUsed, Is.True);
            Assert.That(restored.inference.latencyMilliseconds, Is.EqualTo(1234));
            Assert.That(restored.phaseAfter, Is.EqualTo(TrainingPhase.ReviewingFeedback));
            Assert.That(restored.teacherTextLength, Is.EqualTo(17));
            Assert.That(restored.teacherTextHash, Is.Not.Empty);
            Assert.That(restored.turnRoute, Is.EqualTo(StudentTurnRoute.LocalFallback));
            Assert.That(restored.learningSupport.level, Is.EqualTo(LearningSupportLevel.Principle));
            Assert.That(restored.learningSupport.requestCount, Is.EqualTo(2));
            Assert.That(restored.learningSupport.displayedMilliseconds, Is.EqualTo(3400));
        }

        [Test]
        public void SessionReplay_ReproducesFinalStateWithoutMutatingInitialState()
        {
            StudentStateSnapshot before = Snapshot(-0.8f, 0.9f, 0.2f);
            StudentStateSnapshot after = Snapshot(0.1f, 0.3f, 0.8f);
            TrainingSessionReplayState initial = TrainingSessionReplay.Start(
                nameof(SessionReplay_ReproducesFinalStateWithoutMutatingInitialState),
                before);
            var item = new TrainingTelemetryEvent
            {
                sessionId = initial.sessionId,
                sequence = 0,
                kind = TrainingEventKind.StudentResponse,
                studentStateBefore = before,
                studentStateAfter = after
            };

            TrainingSessionReplayState replayed = TrainingSessionReplay.Replay(
                initial,
                new[] { item });

            Assert.That(replayed.nextSequence, Is.EqualTo(1));
            Assert.That(replayed.studentState.affect.valence, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(initial.studentState.affect.valence, Is.EqualTo(-0.8f).Within(0.001f));
        }

        [Test]
        public void SessionReplay_RejectsSequenceGaps()
        {
            TrainingSessionReplayState initial = TrainingSessionReplay.Start(
                nameof(SessionReplay_RejectsSequenceGaps),
                Snapshot(-0.8f, 0.9f, 0.2f));
            var item = new TrainingTelemetryEvent
            {
                sessionId = initial.sessionId,
                sequence = 1,
                studentStateAfter = Snapshot(0f, 0.5f, 0.5f)
            };

            Assert.Throws<InvalidOperationException>(
                () => TrainingSessionReplay.Reduce(initial, item));
        }

        [Test]
        public void EvidenceScoring_UsesOnlyEvidenceAssignedToEachCompetency()
        {
            var item = new TrainingTelemetryEvent
            {
                competencyEvidence = new[]
                {
                    Evidence(TeacherCompetency.StudentDignity, 2f),
                    Evidence(TeacherCompetency.StudentAgency, 2.5f),
                    Evidence(TeacherCompetency.StudentAgency, 3f),
                    Evidence(TeacherCompetency.Safety, 0.5f)
                }
            };

            EvidenceCenteredScoreSummary summary = EvidenceCenteredScoring.Evaluate(new[] { item });

            Assert.That(summary.dimensions, Has.Length.EqualTo(6));
            Assert.That(Find(summary, TeacherCompetency.StudentDignity).score, Is.EqualTo(2f));
            Assert.That(Find(summary, TeacherCompetency.StudentAgency).score, Is.EqualTo(2.75f));
            Assert.That(Find(summary, TeacherCompetency.Safety).score, Is.EqualTo(0.5f));
            Assert.That(Find(summary, TeacherCompetency.EmotionAcknowledgement).hasEvidence, Is.False);
            Assert.That(summary.averageScore, Is.EqualTo(1.75f).Within(0.001f));
        }

        [Test]
        public void RubricEvaluationEvent_ContributesValidatedLlmEvidenceToDebrief()
        {
            var item = new TrainingTelemetryEvent
            {
                kind = TrainingEventKind.RubricEvaluation,
                actionSource = TrainingActionSource.GenerativeModel,
                competencyEvidence = new[]
                {
                    Evidence(TeacherCompetency.StudentDignity, 2.8f),
                    Evidence(TeacherCompetency.LowStimulusResponse, 2.4f),
                    Evidence(TeacherCompetency.EmotionAcknowledgement, 2.6f),
                    Evidence(TeacherCompetency.StudentAgency, 2.2f),
                    Evidence(TeacherCompetency.Safety, 2.5f),
                    Evidence(TeacherCompetency.InstructionalReentry, 2.1f)
                }
            };

            RubricSummary summary = TeacherRubricEvaluator.Evaluate(new[] { item });

            Assert.That(summary.dimensions, Has.Length.EqualTo(6));
            Assert.That(summary.averageScore, Is.EqualTo(2.4333f).Within(0.001f));
            Assert.That(summary.dimensions[0].score, Is.EqualTo(2.8f).Within(0.001f));
        }

        private static StudentStateSnapshot Snapshot(float valence, float arousal, float progress)
        {
            return new StudentStateSnapshot
            {
                affect = new AffectVector(valence, arousal, 0f),
                gesture = BehaviorGesture.Listen,
                gestureIntensity = progress,
                gazeContact = progress,
                engagement = progress,
                trust = progress
            };
        }

        private static CompetencyEvidence Evidence(TeacherCompetency dimension, float score)
        {
            return new CompetencyEvidence
            {
                evidenceId = dimension.ToString(),
                dimension = dimension,
                score = score
            };
        }

        private static CompetencyDimensionScore Find(
            EvidenceCenteredScoreSummary summary,
            TeacherCompetency dimension)
        {
            return Array.Find(summary.dimensions, item => item.dimension == dimension);
        }
    }
}
