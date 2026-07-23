using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class AdaptiveLearningSupportTests
    {
        private LearningSupportPolicy policy;

        [SetUp]
        public void SetUp()
        {
            policy = LearningSupportPolicy.CreateRuntimeDefault();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(policy);
        }

        [Test]
        public void Inactivity_OffersOneObservationCueAfterThreshold()
        {
            var machine = new LearningSupportStateMachine(policy);
            machine.BeginBeat(1, 10d);

            Assert.That(machine.TryTick(27.9d, out _), Is.False);
            Assert.That(machine.TryTick(28.1d, out LearningSupportDecision decision), Is.True);
            Assert.That(decision.Level, Is.EqualTo(LearningSupportLevel.ObservationCue));
            Assert.That(decision.Trigger, Is.EqualTo(LearningSupportTrigger.Inactivity));
            Assert.That(decision.Automatic, Is.True);
            Assert.That(machine.TryTick(50d, out _), Is.False);
        }

        [Test]
        public void ManualSupport_EscalatesButDoesNotRevealContrastBeforeAction()
        {
            var machine = new LearningSupportStateMachine(policy);
            machine.BeginBeat(1, 0d);

            LearningSupportDecision first = machine.RequestManual(2d, false);
            LearningSupportDecision second = machine.RequestManual(4d, false);
            LearningSupportDecision after = machine.RequestManual(6d, true);

            Assert.That(first.Level, Is.EqualTo(LearningSupportLevel.ObservationCue));
            Assert.That(second.Level, Is.EqualTo(LearningSupportLevel.Principle));
            Assert.That(after.Level, Is.EqualTo(LearningSupportLevel.PostActionContrast));
            Assert.That(after.Trigger, Is.EqualTo(LearningSupportTrigger.PostActionRequest));
            Assert.That(after.RequestCount, Is.EqualTo(3));
        }

        [Test]
        public void RepeatedMisses_TriggerPrincipleOnFirstAttempt()
        {
            var machine = new LearningSupportStateMachine(policy);
            machine.BeginBeat(1, 0d);

            Assert.That(machine.RecordOutcome(1, 2d, out _), Is.False);
            machine.BeginBeat(1, 3d);
            Assert.That(machine.RecordOutcome(0, 5d, out LearningSupportDecision decision), Is.True);

            Assert.That(decision.Level, Is.EqualTo(LearningSupportLevel.Principle));
            Assert.That(decision.Trigger, Is.EqualTo(LearningSupportTrigger.RepeatedMissedSignal));
            Assert.That(decision.ConsecutiveMisses, Is.EqualTo(2));
        }

        [Test]
        public void Retry_FadesAutomaticSupportToObservationOnly()
        {
            var machine = new LearningSupportStateMachine(policy);
            machine.BeginBeat(2, 0d);

            Assert.That(machine.RecordOutcome(0, 1d, out _), Is.False);
            machine.BeginBeat(2, 2d);
            Assert.That(machine.RecordOutcome(0, 3d, out LearningSupportDecision decision), Is.True);
            Assert.That(decision.Level, Is.EqualTo(LearningSupportLevel.ObservationCue));
        }

        [Test]
        public void SuccessfulResponse_ResetsMissedSignalStreak()
        {
            var machine = new LearningSupportStateMachine(policy);
            machine.BeginBeat(1, 0d);
            machine.RecordOutcome(0, 1d, out _);
            machine.BeginBeat(1, 2d);
            machine.RecordOutcome(3, 3d, out _);

            Assert.That(machine.ConsecutiveMisses, Is.Zero);
        }

        [Test]
        public void DefaultPolicy_DefinesGuidanceForEveryCrisisStage()
        {
            foreach (CrisisStage stage in System.Enum.GetValues(typeof(CrisisStage)))
            {
                LearningSupportStagePrompt prompt = policy.PromptFor(stage);
                Assert.That(prompt, Is.Not.Null, stage.ToString());
                Assert.That(prompt.observationCue, Is.Not.Empty, stage.ToString());
                Assert.That(prompt.principleCue, Is.Not.Empty, stage.ToString());
            }
        }
    }
}
