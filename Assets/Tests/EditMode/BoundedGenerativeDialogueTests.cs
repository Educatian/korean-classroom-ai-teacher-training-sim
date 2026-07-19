using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class BoundedGenerativeDialogueTests
    {
        [Test]
        public void ConversationState_RetainsOnlyTheConfiguredRecentTurns()
        {
            var state = new ConversationSessionState(2);
            state.AddTurn("teacher-1", "student-1", DialogueSignals.Neutral);
            state.AddTurn("teacher-2", "student-2", DialogueSignals.Neutral);

            state.AddTurn("teacher-3", "student-3", DialogueSignals.Neutral);

            Assert.That(state.RecentTurnCount, Is.EqualTo(2));
            Assert.That(state.TotalTurnCount, Is.EqualTo(3));
        }

        [Test]
        public void ConversationState_AccumulatesValidatedRelationalSignals()
        {
            var state = new ConversationSessionState(4);
            var signals = new DialogueSignals
            {
                feltHeard = 0.9f,
                choiceOffered = 0.8f,
                perceivedPressure = 0.1f,
                readyForReentry = 0.7f
            };

            state.AddTurn("teacher", "student", signals);

            Assert.That(state.Trust, Is.GreaterThan(0.5f));
            Assert.That(state.DemandPressure, Is.LessThan(0.5f));
            Assert.That(state.Readiness, Is.GreaterThan(0.5f));
        }

        [Test]
        public void ConversationState_RetainsTeacherCommitmentsBeyondTheRecentTurnWindow()
        {
            var state = new ConversationSessionState(1);
            state.AddTurn("잠깐 기다릴게. 함께 다시 시작하자.", "네.", DialogueSignals.Neutral);
            state.AddTurn("지금 기분은 어때?", "조금 나아요.", DialogueSignals.Neutral);

            string context = state.BuildPromptContext();

            Assert.That(state.RecentTurnCount, Is.EqualTo(1));
            Assert.That(state.DurableCommitmentCount, Is.EqualTo(1));
            Assert.That(context, Does.Contain("잠깐 기다릴게"));
            Assert.That(context, Does.Contain("durable_teacher_commitments"));
        }

        [Test]
        public void PerformanceNormalizer_AlignsNegativeHighArousalWithGestureAndFacialUnits()
        {
            var turn = new StudentAgentTurn
            {
                studentReply = "지금은 싫어요.",
                valence = -0.8f,
                arousal = 0.9f,
                dominance = 0.45f,
                gesture = BehaviorGesture.Recover.ToString(),
                actionUnits = new ActionUnitDirective { au12 = 0.9f },
                dialogueSignals = new DialogueSignals { perceivedPressure = 0.85f }
            };

            StudentTurnPerformanceNormalizer.Normalize(turn);

            Assert.That(turn.gesture, Is.EqualTo(BehaviorGesture.Protest.ToString()));
            Assert.That(turn.actionUnits.au4, Is.GreaterThanOrEqualTo(0.58f));
            Assert.That(turn.actionUnits.au7, Is.GreaterThanOrEqualTo(0.48f));
            Assert.That(turn.actionUnits.au12, Is.LessThanOrEqualTo(0.18f));
        }

        [Test]
        public void PerformanceNormalizer_UsesReentrySignalsForRecoveryEyeContactPose()
        {
            var turn = new StudentAgentTurn
            {
                studentReply = "다시 해 볼게요.",
                valence = 0.2f,
                arousal = 0.3f,
                dominance = 0.05f,
                gesture = BehaviorGesture.Withdraw.ToString(),
                dialogueSignals = new DialogueSignals
                {
                    feltHeard = 0.8f,
                    perceivedPressure = 0.1f,
                    readyForReentry = 0.8f
                }
            };

            StudentTurnPerformanceNormalizer.Normalize(turn);

            Assert.That(turn.gesture, Is.EqualTo(BehaviorGesture.Recover.ToString()));
            Assert.That(turn.actionUnits, Is.Not.Null);
            Assert.That(turn.actionUnits.au12, Is.GreaterThanOrEqualTo(0.28f));
        }

        [Test]
        public void TransitionEngine_RoutesSafetyConcernToTheAuthoredPeakStage()
        {
            var context = new ScenarioTransitionContext(
                0,
                1,
                new[] { CrisisStage.Trigger, CrisisStage.Escalation, CrisisStage.Peak, CrisisStage.Deescalation },
                new DialogueSignals { safetyConcern = 0.9f });

            ScenarioTransitionDecision result = ScenarioTransitionEngine.Select(context);

            Assert.That(result.NextBeatIndex, Is.EqualTo(2));
            Assert.That(result.Reason, Is.EqualTo(ScenarioTransitionReason.SafetyOverride));
        }

        [Test]
        public void TransitionEngine_RoutesSupportiveDialogueTowardDeescalation()
        {
            var context = new ScenarioTransitionContext(
                1,
                2,
                new[] { CrisisStage.Trigger, CrisisStage.Escalation, CrisisStage.Peak, CrisisStage.Deescalation },
                new DialogueSignals
                {
                    feltHeard = 0.8f,
                    choiceOffered = 0.7f,
                    perceivedPressure = 0.1f
                });

            ScenarioTransitionDecision result = ScenarioTransitionEngine.Select(context);

            Assert.That(result.NextBeatIndex, Is.EqualTo(3));
            Assert.That(result.Reason, Is.EqualTo(ScenarioTransitionReason.SupportiveDeescalation));
        }

        [Test]
        public void TransitionEngine_HoldsAfterOnlyOneSupportiveTurnAtTheTrigger()
        {
            var context = new ScenarioTransitionContext(
                0,
                1,
                new[] { CrisisStage.Trigger, CrisisStage.Escalation, CrisisStage.Deescalation, CrisisStage.InstructionalReentry },
                new DialogueSignals
                {
                    feltHeard = 0.85f,
                    choiceOffered = 0.7f,
                    perceivedPressure = 0.1f,
                    readyForReentry = 0.8f
                });

            ScenarioTransitionDecision result = ScenarioTransitionEngine.Select(context);

            Assert.That(result.NextBeatIndex, Is.EqualTo(0));
            Assert.That(result.Reason, Is.EqualTo(ScenarioTransitionReason.Hold));
        }

        [Test]
        public void RubricContract_RejectsAResultWithoutAllCompetencyDimensions()
        {
            var incomplete = new TeacherRubricResult
            {
                schemaVersion = 1,
                confidence = 0.8f,
                dimensions = new[]
                {
                    new RubricDimensionScore
                    {
                        dimension = TeacherCompetency.StudentDignity,
                        score = 2.5f
                    }
                }
            };

            bool accepted = LlmContractValidator.TryAcceptRubric(incomplete, out _);

            Assert.That(accepted, Is.False);
        }

        [Test]
        public void RubricContract_ParsesACompleteStructuredResult()
        {
            string json = "{\"schemaVersion\":1,\"confidence\":0.8,\"dimensions\":[" +
                          DimensionJson(TeacherCompetency.StudentDignity) + "," +
                          DimensionJson(TeacherCompetency.LowStimulusResponse) + "," +
                          DimensionJson(TeacherCompetency.EmotionAcknowledgement) + "," +
                          DimensionJson(TeacherCompetency.StudentAgency) + "," +
                          DimensionJson(TeacherCompetency.Safety) + "," +
                          DimensionJson(TeacherCompetency.InstructionalReentry) +
                          "],\"improvementSuggestion\":\"next step\"}";

            TeacherRubricResult result = GenerativeAiCoach.ParseTeacherRubric(json);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.dimensions, Has.Length.EqualTo(6));
            Assert.That(result.confidence, Is.EqualTo(0.8f).Within(0.001f));
        }

        [Test]
        public void RubricContract_RejectsNonFiniteDimensionScores()
        {
            var result = new TeacherRubricResult
            {
                confidence = 0.8f,
                dimensions = new[]
                {
                    Dimension(TeacherCompetency.StudentDignity),
                    Dimension(TeacherCompetency.LowStimulusResponse),
                    Dimension(TeacherCompetency.EmotionAcknowledgement),
                    Dimension(TeacherCompetency.StudentAgency),
                    Dimension(TeacherCompetency.Safety),
                    Dimension(TeacherCompetency.InstructionalReentry)
                }
            };
            result.dimensions[2].score = float.NaN;

            Assert.That(LlmContractValidator.TryAcceptRubric(result, out _), Is.False);
        }

        [Test]
        public void TurnCoordinator_AdvancesToADeterministicallySelectedBeat()
        {
            var coordinator = new TrainingTurnCoordinator(6);
            coordinator.Start();
            coordinator.TrySubmit(TeacherAction.FromUtterance("teacher"), out TrainingRequestToken token);
            coordinator.TryResolve(token);

            bool advanced = coordinator.TryAdvanceTo(3, out bool complete);

            Assert.That(advanced, Is.True);
            Assert.That(complete, Is.False);
            Assert.That(coordinator.BeatIndex, Is.EqualTo(3));
        }

        private static string DimensionJson(TeacherCompetency dimension)
        {
            return $"{{\"dimension\":{(int)dimension},\"score\":2.0,\"evidence\":\"e\"}}";
        }

        private static RubricDimensionScore Dimension(TeacherCompetency dimension)
        {
            return new RubricDimensionScore { dimension = dimension, score = 2f, evidence = "e" };
        }
    }
}
