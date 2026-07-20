using System;
using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class StudentSpeechAndEcdTests
    {
        [Test]
        public void ProsodyPlanner_HighArousalSpeaksFasterWithShorterPauses()
        {
            StudentSpeechProsody calm = StudentSpeechProsodyPlanner.Plan(
                "잠깐 쉬었다가 다시 이야기할게요.",
                new AffectVector(-0.2f, 0.15f, -0.1f));
            StudentSpeechProsody activated = StudentSpeechProsodyPlanner.Plan(
                "잠깐 쉬었다가 다시 이야기할게요.",
                new AffectVector(-0.2f, 0.9f, 0.2f));

            Assert.That(activated.rate, Is.GreaterThan(calm.rate));
            Assert.That(activated.commaPauseMilliseconds, Is.LessThan(calm.commaPauseMilliseconds));
            Assert.That(activated.sentencePauseMilliseconds, Is.LessThan(calm.sentencePauseMilliseconds));
        }

        [Test]
        public void ProsodyPlanner_SsmlEscapesTextAndAddsPunctuationBreaks()
        {
            StudentSpeechProsody prosody = StudentSpeechProsodyPlanner.Plan(
                "저는 <지금> 힘들어요.",
                new AffectVector(-0.7f, 0.6f, -0.3f));

            string ssml = StudentSpeechProsodyPlanner.BuildSsml("저는 <지금> 힘들어요.", prosody);

            Assert.That(ssml, Does.Contain("&lt;지금&gt;"));
            Assert.That(ssml, Does.Contain("<break time="));
            Assert.That(ssml, Does.Contain("xml:lang=\"ko-KR\""));
        }

        [Test]
        public void EcdEngine_ConnectsCompetencyObservationEvidenceAndScore()
        {
            EcdAssessmentModel model = EcdAssessmentModel.CreateRuntimeDefault();
            var events = new[]
            {
                new TrainingTelemetryEvent
                {
                    sessionId = "session",
                    sequence = 0,
                    beatIndex = 1,
                    kind = TrainingEventKind.TeacherAction,
                    actionSource = TrainingActionSource.TeacherUtterance,
                    studentStateBefore = Snapshot(-0.8f, 0.9f),
                    studentStateAfter = Snapshot(-0.3f, 0.5f),
                    competencyEvidence = new[]
                    {
                        new CompetencyEvidence
                        {
                            evidenceId = nameof(TeacherCompetency.EmotionAcknowledgement),
                            observableId = nameof(TeacherCompetency.EmotionAcknowledgement),
                            dimension = TeacherCompetency.EmotionAcknowledgement,
                            score = 2.8f,
                            rationale = "교사가 학생의 어려움을 반영함"
                        }
                    }
                }
            };

            ResearchDebriefReport report = EcdAssessmentEngine.Evaluate(events, model);
            EcdCompetencyResult emotion = Array.Find(
                report.competencies,
                item => item.competency == TeacherCompetency.EmotionAcknowledgement);

            Assert.That(emotion, Is.Not.Null);
            Assert.That(emotion.score, Is.EqualTo(2.8f).Within(0.001f));
            Assert.That(emotion.evidenceCount, Is.EqualTo(1));
            Assert.That(emotion.evidence[0].observableId, Is.EqualTo(nameof(TeacherCompetency.EmotionAcknowledgement)));
            Assert.That(report.affectTrend, Has.Length.EqualTo(1));
            Assert.That(report.interventionTimeline, Has.Length.EqualTo(1));
            Assert.That(report.missedSignals, Is.Not.Empty);
            UnityEngine.Object.DestroyImmediate(model);
        }

        [Test]
        public void EcdModel_DefaultDefinesEveryTeacherCompetency()
        {
            EcdAssessmentModel model = EcdAssessmentModel.CreateRuntimeDefault();
            TeacherCompetency[] values = (TeacherCompetency[])Enum.GetValues(typeof(TeacherCompetency));

            Assert.That(model.Competencies.Count, Is.EqualTo(values.Length));
            foreach (TeacherCompetency competency in values)
            {
                EcdCompetencyDefinition definition = model.Find(competency);
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition.observableBehaviors, Is.Not.Empty);
                Assert.That(definition.observableBehaviors[0].missedSignalMessage, Is.Not.Empty);
            }

            UnityEngine.Object.DestroyImmediate(model);
        }

        [Test]
        public void TelemetrySchema_PreservesObservableEvidenceTrace()
        {
            var original = new TrainingTelemetryEvent
            {
                sessionId = "session",
                coachSuggestion = "선택권을 포함한 짧은 대안을 제시하세요.",
                competencyEvidence = new[]
                {
                    new CompetencyEvidence
                    {
                        evidenceId = "ev-1",
                        observableId = "observe-emotion",
                        rationale = "정서 반영 발화",
                        dimension = TeacherCompetency.EmotionAcknowledgement,
                        score = 2.4f
                    }
                },
                studentSpeech = new StudentSpeechTelemetry
                {
                    requested = true,
                    providerRoute = "openai-audio-api",
                    rate = 0.94f,
                    sentencePauseMilliseconds = 380,
                    disclosure = StudentSpeechSynthesizer.VoiceDisclosure
                }
            };

            TrainingTelemetryEvent restored = JsonUtility.FromJson<TrainingTelemetryEvent>(
                JsonUtility.ToJson(original));

            Assert.That(restored.schemaVersion, Is.EqualTo(3));
            Assert.That(restored.competencyEvidence[0].observableId, Is.EqualTo("observe-emotion"));
            Assert.That(restored.coachSuggestion, Does.Contain("선택권"));
            Assert.That(restored.competencyEvidence[0].rationale, Is.EqualTo("정서 반영 발화"));
            Assert.That(restored.studentSpeech.providerRoute, Is.EqualTo("openai-audio-api"));
            Assert.That(restored.studentSpeech.sentencePauseMilliseconds, Is.EqualTo(380));
        }

        [Test]
        public void EcdEngine_BuildsPrivacySafeTeacherSpeechCoaching()
        {
            EcdAssessmentModel model = EcdAssessmentModel.CreateRuntimeDefault();
            var events = new[]
            {
                new TrainingTelemetryEvent
                {
                    sessionId = "session",
                    actionId = "turn-1",
                    sequence = 1,
                    beatIndex = 2,
                    kind = TrainingEventKind.TeacherAction,
                    actionSource = TrainingActionSource.TeacherUtterance,
                    teacherTextLength = 18,
                    teacherTextHash = "hash-only",
                    studentStateBefore = Snapshot(-0.7f, 0.8f),
                    studentStateAfter = Snapshot(-0.3f, 0.5f),
                    competencyEvidence = new[]
                    {
                        new CompetencyEvidence
                        {
                            dimension = TeacherCompetency.EmotionAcknowledgement,
                            score = 3f
                        },
                        new CompetencyEvidence
                        {
                            dimension = TeacherCompetency.StudentAgency,
                            score = 1.4f
                        }
                    }
                },
                new TrainingTelemetryEvent
                {
                    sessionId = "session",
                    actionId = "turn-1",
                    sequence = 2,
                    beatIndex = 2,
                    kind = TrainingEventKind.RubricEvaluation,
                    coachSuggestion = "잠깐 쉴지 이야기할지 네가 선택해도 좋아."
                }
            };

            ResearchDebriefReport report = EcdAssessmentEngine.Evaluate(events, model);
            InterventionTimelineItem coaching = report.interventionTimeline[0];

            Assert.That(coaching.teacherUtteranceSummary, Does.Contain("자유 발화"));
            Assert.That(coaching.teacherUtteranceSummary, Does.Contain("18자"));
            Assert.That(coaching.recommendedUtterance, Does.Contain("선택해도 좋아"));
            Assert.That(coaching.evaluationRationale, Does.Contain("강점"));
            Assert.That(coaching.evaluationRationale, Does.Contain("보완"));
            UnityEngine.Object.DestroyImmediate(model);
        }
        [Test]
        public void ResearchDashboard_RequiresUnlockAndReturnsToCompactSummary()
        {
            var state = new ResearchDashboardState();

            Assert.That(state.OpenFull(), Is.False);
            Assert.That(state.View, Is.EqualTo(ResearchDashboardView.Locked));

            state.UnlockSummary();
            Assert.That(state.View, Is.EqualTo(ResearchDashboardView.Summary));
            Assert.That(state.OpenFull(), Is.True);
            Assert.That(state.View, Is.EqualTo(ResearchDashboardView.Full));

            state.ReturnToSummary();
            Assert.That(state.View, Is.EqualTo(ResearchDashboardView.Summary));
        }
        private static StudentStateSnapshot Snapshot(float valence, float arousal)
        {
            return new StudentStateSnapshot
            {
                affect = new AffectVector(valence, arousal, 0f),
                gesture = BehaviorGesture.Listen,
                gazeContact = 1f,
                engagement = 0.5f,
                trust = 0.5f
            };
        }
    }
}
