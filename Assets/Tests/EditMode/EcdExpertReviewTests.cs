using System.Collections.Generic;
using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class EcdExpertReviewTests
    {
        [Test]
        public void BlindCase_DoesNotExposeModelScoresOrUnconsentedTeacherText()
        {
            var telemetry = new[]
            {
                TeacherAction("action-1", 18, -0.6f, -0.2f)
            };

            EcdBlindReviewCase reviewCase = EcdExpertReviewBuilder.BuildBlindCase(
                "session-sensitive-id",
                "classroom-escalation",
                1,
                telemetry,
                EcdAssessmentModel.CreateRuntimeDefault());

            Assert.That(reviewCase.caseId, Does.StartWith("ECD-"));
            Assert.That(reviewCase.caseId, Does.Not.Contain("session-sensitive-id"));
            Assert.That(reviewCase.evidence[0].semanticTextAvailable, Is.False);
            StringAssert.Contains("text not retained", reviewCase.evidence[0].teacherUtterance);
            Assert.That(reviewCase.competencyPrompts, Has.Length.EqualTo(6));
        }

        [Test]
        public void BlindCase_IncludesOnlyExplicitlyConsentedRedactedUtterance()
        {
            var utterances = new Dictionary<string, string>
            {
                ["action-1"] = "지금 많이 답답해 보이는구나. 잠깐 기다릴게."
            };

            EcdBlindReviewCase reviewCase = EcdExpertReviewBuilder.BuildBlindCase(
                "session-1", "scenario-1", 1,
                new[] { TeacherAction("action-1", 24, -0.5f, 0.1f) },
                EcdAssessmentModel.CreateRuntimeDefault(),
                utterances);

            Assert.That(reviewCase.evidence[0].semanticTextAvailable, Is.True);
            Assert.That(reviewCase.evidence[0].teacherUtterance,
                Is.EqualTo(utterances["action-1"]));
        }

        [Test]
        public void Comparison_ComputesMatchedCompetencyAgreement()
        {
            var reference = new EcdModelReference
            {
                caseId = "ECD-case",
                scores = new[]
                {
                    new EcdModelReferenceScore { competency = TeacherCompetency.StudentDignity, score = 2.5f },
                    new EcdModelReferenceScore { competency = TeacherCompetency.Safety, score = 1f }
                }
            };
            var expert = new EcdExpertReviewSubmission
            {
                caseId = "ECD-case",
                scores = new[]
                {
                    new EcdExpertScore { competency = TeacherCompetency.StudentDignity, score = 2f },
                    new EcdExpertScore { competency = TeacherCompetency.Safety, score = 2f }
                }
            };

            EcdReviewComparison comparison = EcdExpertReviewBuilder.Compare(reference, expert);

            Assert.That(comparison.matchedCompetencies, Is.EqualTo(2));
            Assert.That(comparison.meanAbsoluteError, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(comparison.withinHalfPointRate, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void Comparison_RejectsMismatchedBlindCase()
        {
            Assert.Throws<System.ArgumentException>(() => EcdExpertReviewBuilder.Compare(
                new EcdModelReference { caseId = "A" },
                new EcdExpertReviewSubmission { caseId = "B" }));
        }

        private static TrainingTelemetryEvent TeacherAction(
            string actionId,
            int textLength,
            float before,
            float after)
        {
            return new TrainingTelemetryEvent
            {
                kind = TrainingEventKind.TeacherAction,
                actionId = actionId,
                actionSource = TrainingActionSource.TeacherUtterance,
                teacherTextLength = textLength,
                studentStateBefore = new StudentStateSnapshot { affect = new AffectVector(before, 0.8f, -0.2f) },
                studentStateAfter = new StudentStateSnapshot { affect = new AffectVector(after, 0.5f, 0f) },
                gaze = new TeacherGazeSummary
                {
                    trackingSource = EyeTrackingSource.EyeGaze,
                    focalStudentDwellMilliseconds = 1200,
                    mutualGazeMilliseconds = 400
                }
            };
        }
    }
}
