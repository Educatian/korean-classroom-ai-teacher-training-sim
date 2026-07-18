using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class TrainingTurnCoordinatorTests
    {
        [Test]
        public void FreeTextTurn_UsesSameAdvancePathAsChoice()
        {
            var coordinator = new TrainingTurnCoordinator(2);
            Assert.That(coordinator.Start(), Is.True);
            Assert.That(
                coordinator.TrySubmit(
                    TeacherAction.FromUtterance(nameof(FreeTextTurn_UsesSameAdvancePathAsChoice)),
                    out TrainingRequestToken token),
                Is.True);
            Assert.That(coordinator.TrySubmit(TeacherAction.FromChoice(0), out _), Is.False);

            Assert.That(coordinator.TryResolve(token), Is.True);
            Assert.That(coordinator.TryAdvance(out bool complete), Is.True);

            Assert.That(complete, Is.False);
            Assert.That(coordinator.BeatIndex, Is.EqualTo(1));
            Assert.That(coordinator.Phase, Is.EqualTo(TrainingPhase.AwaitingTeacherAction));
        }

        [Test]
        public void StaleToken_CannotResolveANewTurn()
        {
            var coordinator = new TrainingTurnCoordinator(2);
            coordinator.Start();
            coordinator.TrySubmit(TeacherAction.FromChoice(0), out TrainingRequestToken first);
            coordinator.TryResolve(first);
            coordinator.TryAdvance(out _);
            coordinator.TrySubmit(TeacherAction.FromChoice(1), out TrainingRequestToken second);

            Assert.That(coordinator.TryResolve(first), Is.False);
            Assert.That(coordinator.TryResolve(second), Is.True);
        }

        [Test]
        public void FinalTurn_TransitionsToCompleteExactlyOnce()
        {
            var coordinator = new TrainingTurnCoordinator(1);
            coordinator.Start();
            coordinator.TrySubmit(TeacherAction.FromChoice(0), out TrainingRequestToken token);
            coordinator.TryResolve(token);

            Assert.That(coordinator.TryAdvance(out bool complete), Is.True);
            Assert.That(complete, Is.True);
            Assert.That(coordinator.Phase, Is.EqualTo(TrainingPhase.Complete));
            Assert.That(coordinator.TryAdvance(out _), Is.False);
        }
    }
}
