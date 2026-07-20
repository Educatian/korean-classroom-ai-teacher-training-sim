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

            Assert.That(restored.schemaVersion, Is.EqualTo(2));
            Assert.That(restored.competencyEvidence[0].observableId, Is.EqualTo("observe-emotion"));
            Assert.That(restored.competencyEvidence[0].rationale, Is.EqualTo("정서 반영 발화"));
            Assert.That(restored.studentSpeech.providerRoute, Is.EqualTo("openai-audio-api"));
            Assert.That(restored.studentSpeech.sentencePauseMilliseconds, Is.EqualTo(380));
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
