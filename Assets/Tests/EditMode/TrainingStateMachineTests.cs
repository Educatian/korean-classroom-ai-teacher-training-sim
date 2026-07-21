using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class TrainingStateMachineTests
    {
        [Test]
        public void SessionState_AllowsTheCanonicalTurnFlow()
        {
            // Given
            var state = new TrainingSessionState();

            // When / Then
            Assert.That(state.CurrentPhase, Is.EqualTo(TrainingPhase.PresentingScenario));
            Assert.That(state.TryTransitionTo(TrainingPhase.AwaitingTeacherAction), Is.True);
            Assert.That(state.TryTransitionTo(TrainingPhase.AwaitingStudentResponse), Is.True);
            Assert.That(state.TryTransitionTo(TrainingPhase.ReviewingFeedback), Is.True);
            Assert.That(state.TryTransitionTo(TrainingPhase.PresentingScenario), Is.True);
            Assert.That(state.TryTransitionTo(TrainingPhase.Complete), Is.True);
        }

        [Test]
        public void SessionState_RejectsIllegalSkippedTransitions()
        {
            // Given
            var state = new TrainingSessionState();

            // When
            bool accepted = state.TryTransitionTo(TrainingPhase.ReviewingFeedback);

            // Then
            Assert.That(accepted, Is.False);
            Assert.That(state.CurrentPhase, Is.EqualTo(TrainingPhase.PresentingScenario));
        }

        [Test]
        public void InputArbiter_AcceptsExactlyOneTeacherActionPerTurn()
        {
            var state = new TrainingSessionState();
            Assert.That(state.TryTransitionTo(TrainingPhase.AwaitingTeacherAction), Is.True);
            var arbiter = new TrainingInputArbiter(state);

            bool firstAccepted = arbiter.TrySubmit(
                TeacherAction.FromChoice(0),
                out TrainingRequestToken firstToken);
            bool duplicateAccepted = arbiter.TrySubmit(
                TeacherAction.FromUtterance(string.Empty),
                out TrainingRequestToken duplicateToken);

            Assert.That(firstAccepted, Is.True);
            Assert.That(firstToken.IsValid, Is.True);
            Assert.That(duplicateAccepted, Is.False);
            Assert.That(duplicateToken.IsValid, Is.False);
            Assert.That(state.CurrentPhase, Is.EqualTo(TrainingPhase.AwaitingStudentResponse));
        }

        [Test]
        public void InputArbiter_RejectsStaleAndDuplicateCompletions()
        {
            var state = new TrainingSessionState();
            Assert.That(state.TryTransitionTo(TrainingPhase.AwaitingTeacherAction), Is.True);
            var arbiter = new TrainingInputArbiter(state);
            Assert.That(arbiter.TrySubmit(TeacherAction.FromChoice(0), out TrainingRequestToken firstToken), Is.True);
            Assert.That(arbiter.TryComplete(firstToken), Is.True);
            Assert.That(state.TryTransitionTo(TrainingPhase.PresentingScenario), Is.True);
            Assert.That(state.TryTransitionTo(TrainingPhase.AwaitingTeacherAction), Is.True);
            Assert.That(arbiter.TrySubmit(TeacherAction.FromChoice(1), out TrainingRequestToken secondToken), Is.True);

            bool staleAccepted = arbiter.TryComplete(firstToken);
            bool currentAccepted = arbiter.TryComplete(secondToken);
            bool duplicateAccepted = arbiter.TryComplete(secondToken);

            Assert.That(staleAccepted, Is.False);
            Assert.That(currentAccepted, Is.True);
            Assert.That(duplicateAccepted, Is.False);
        }

        [Test]
        public void SessionState_CancelStudentResponseReturnsTurnToTeacher()
        {
            var state = new TrainingSessionState();
            Assert.That(state.TryTransitionTo(TrainingPhase.AwaitingTeacherAction), Is.True);
            Assert.That(state.TryTransitionTo(TrainingPhase.AwaitingStudentResponse), Is.True);

            Assert.That(state.TryCancelStudentResponse(), Is.True);
            Assert.That(state.CurrentPhase, Is.EqualTo(TrainingPhase.AwaitingTeacherAction));
            Assert.That(state.TryCancelStudentResponse(), Is.False);
        }

        [Test]
        public void Coordinator_PauseDuringResponseThenCancelRecoversTheTurn()
        {
            var coordinator = new TrainingTurnCoordinator(3);
            Assert.That(coordinator.Start(), Is.True);
            Assert.That(coordinator.TrySubmit(TeacherAction.FromUtterance("잠깐"), out TrainingRequestToken token), Is.True);

            // Pause lands while the student reply is in flight; the reply is
            // dropped, so the resume path must hand the turn back.
            Assert.That(coordinator.TryPause(), Is.True);
            Assert.That(coordinator.TryResolve(token), Is.False);
            Assert.That(coordinator.TryResume(), Is.True);
            Assert.That(coordinator.Phase, Is.EqualTo(TrainingPhase.AwaitingStudentResponse));
            Assert.That(coordinator.TryCancelPendingStudentResponse(), Is.True);
            Assert.That(coordinator.Phase, Is.EqualTo(TrainingPhase.AwaitingTeacherAction));

            // The stale token can no longer complete anything after the cancel.
            Assert.That(coordinator.TryResolve(token), Is.False);
            Assert.That(coordinator.TrySubmit(TeacherAction.FromChoice(1), out TrainingRequestToken second), Is.True);
            Assert.That(second.IsValid, Is.True);
        }

        [Test]
        public void SessionState_PauseResumeAndAbortPreserveSafetySemantics()
        {
            var state = new TrainingSessionState();
            Assert.That(state.TryTransitionTo(TrainingPhase.AwaitingTeacherAction), Is.True);

            Assert.That(state.TryPause(), Is.True);
            Assert.That(state.CurrentPhase, Is.EqualTo(TrainingPhase.Paused));
            Assert.That(state.TryResume(), Is.True);
            Assert.That(state.CurrentPhase, Is.EqualTo(TrainingPhase.AwaitingTeacherAction));
            Assert.That(state.TryAbort(), Is.True);
            Assert.That(state.CurrentPhase, Is.EqualTo(TrainingPhase.Aborted));
            Assert.That(state.TryResume(), Is.False);
            Assert.That(state.TryTransitionTo(TrainingPhase.PresentingScenario), Is.False);
        }
    }
}
