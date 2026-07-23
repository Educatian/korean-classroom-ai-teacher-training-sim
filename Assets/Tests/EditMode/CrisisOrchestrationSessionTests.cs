using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class CrisisOrchestrationSessionTests
    {
        private const string ResourcePath =
            "Training/Orchestration/TeacherDirectedAggression";

        [Test]
        public void FullAuthoredPath_CompletesOnlyAfterRecordAndRecovery()
        {
            CrisisOrchestrationSession session = Session();

            Select(session, CrisisOrchestrationAction.PauseAndRegulate);
            Select(session, CrisisOrchestrationAction.MovePeersToSafety);
            Select(session, CrisisOrchestrationAction.RequestColleagueSupport);
            Select(session, CrisisOrchestrationAction.ConfirmSupportArrival);
            Assert.That(session.BeatIndex, Is.EqualTo(3),
                "Arrival confirmation should not skip the handoff practice.");
            Select(session, CrisisOrchestrationAction.HandoffWithBriefing);
            Select(session, CrisisOrchestrationAction.AddressStudent);
            Select(session, CrisisOrchestrationAction.RecordObjectiveFacts);
            Select(session, CrisisOrchestrationAction.RequestTeacherRecoverySupport);

            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.State.objectiveRecordCompleted, Is.True);
            Assert.That(session.State.recoveryPlanCompleted, Is.True);
        }

        [Test]
        public void OutOfBeatAction_IsRejectedWithoutChangingHistory()
        {
            CrisisOrchestrationSession session = Session();

            CrisisOrchestrationResolution result = session.SelectAction(
                CrisisOrchestrationAction.RecordObjectiveFacts);

            Assert.That(result.accepted, Is.False);
            Assert.That(session.History.Count, Is.Zero);
            Assert.That(session.BeatIndex, Is.Zero);
        }

        [Test]
        public void Debrief_SeparatesTeacherActionsFromOrganizationalSupport()
        {
            CrisisOrchestrationSession session = Session();
            Select(session, CrisisOrchestrationAction.PauseAndRegulate);

            CrisisOrchestrationDebrief debrief =
                CrisisOrchestrationDebriefBuilder.Build(session.Scenario, session.History);

            Assert.That(debrief.headline, Does.Contain("감정을 평가하지 않고"));
            Assert.That(debrief.strengths[0], Does.Contain("판단 여유"));
            Assert.That(debrief.organizationalSupports.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(debrief.reflectionPrompts.Length, Is.GreaterThanOrEqualTo(7));
        }

        private static CrisisOrchestrationSession Session()
        {
            CrisisOrchestrationScenarioAsset scenario =
                Resources.Load<CrisisOrchestrationScenarioAsset>(ResourcePath);
            Assert.That(scenario, Is.Not.Null);
            return new CrisisOrchestrationSession(scenario);
        }

        private static void Select(
            CrisisOrchestrationSession session,
            CrisisOrchestrationAction action)
        {
            Assert.That(session.SelectAction(action).accepted, Is.True, action.ToString());
        }
    }
}
