using System.Linq;
using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class CrisisOrchestrationEngineTests
    {
        [Test]
        public void PauseAndRegulate_EvaluatesActionRatherThanFeeling()
        {
            CrisisOrchestrationEngine engine = Engine();

            CrisisOrchestrationResolution result = engine.Apply(
                CrisisOrchestrationAction.PauseAndRegulate);

            Assert.That(result.accepted, Is.True);
            Assert.That(result.after.teacher.arousal, Is.LessThan(result.before.teacher.arousal));
            Assert.That(result.after.teacher.regulationCapacity,
                Is.GreaterThan(result.before.teacher.regulationCapacity));
            Assert.That(result.evidence,
                Does.Contain(CrisisOrchestrationEvidenceId.RegulatedBeforeIntervention));
        }

        [Test]
        public void DirectInterventionDuringMutualHighArousal_IsFlaggedNotSilentlyRewarded()
        {
            CrisisOrchestrationEngine engine = Engine();

            CrisisOrchestrationResolution result = engine.Apply(
                CrisisOrchestrationAction.AddressStudent);

            Assert.That(result.accepted, Is.True);
            Assert.That(result.after.classroom.focalStudentRisk,
                Is.GreaterThan(result.before.classroom.focalStudentRisk));
            Assert.That(result.evidence,
                Does.Contain(CrisisOrchestrationEvidenceId.PrematureDirectIntervention));
        }

        [Test]
        public void PeerMovement_RemovesPeersFromUnsafeAreaBeforeLearningContinuity()
        {
            CrisisOrchestrationEngine engine = Engine();

            CrisisOrchestrationResolution result = engine.Apply(
                CrisisOrchestrationAction.MovePeersToSafety);

            Assert.That(result.after.classroom.peersInUnsafeArea, Is.Zero);
            Assert.That(result.after.classroom.peerDistress,
                Is.LessThan(result.before.classroom.peerDistress));
            Assert.That(result.evidence,
                Does.Contain(CrisisOrchestrationEvidenceId.PeerSafetyPrioritized));
        }

        [Test]
        public void UnavailableColleague_CanEscalateToAdministrator()
        {
            CrisisOrchestrationState initial = Initial();
            initial.support.colleagueAvailable = false;
            CrisisOrchestrationEngine engine = new CrisisOrchestrationEngine(initial);

            CrisisOrchestrationResolution colleague = engine.Apply(
                CrisisOrchestrationAction.RequestColleagueSupport);
            CrisisOrchestrationResolution administrator = engine.Apply(
                CrisisOrchestrationAction.RequestAdministratorSupport);

            Assert.That(colleague.after.support.colleagueResponse,
                Is.EqualTo(SupportResponseState.Unavailable));
            Assert.That(administrator.after.support.administratorResponse,
                Is.EqualTo(SupportResponseState.EnRoute));
            Assert.That(administrator.evidence,
                Does.Contain(CrisisOrchestrationEvidenceId.EscalatedSupportRequest));
        }

        [Test]
        public void HandoffBeforeSupportArrival_IsRejected()
        {
            CrisisOrchestrationEngine engine = Engine();

            CrisisOrchestrationResolution result = engine.Apply(
                CrisisOrchestrationAction.HandoffWithBriefing);

            Assert.That(result.accepted, Is.False);
            Assert.That(result.after.phase, Is.EqualTo(CrisisOrchestrationPhase.Assess));
            Assert.That(result.evidence,
                Does.Contain(CrisisOrchestrationEvidenceId.UnsupportedHandoffAttempt));
        }

        [Test]
        public void DocumentationBeforeSafetyAndHandoff_IsRejected()
        {
            CrisisOrchestrationEngine engine = Engine();

            CrisisOrchestrationResolution result = engine.Apply(
                CrisisOrchestrationAction.RecordObjectiveFacts);

            Assert.That(result.accepted, Is.False);
            Assert.That(result.after.objectiveRecordCompleted, Is.False);
            Assert.That(result.evidence,
                Does.Contain(CrisisOrchestrationEvidenceId.UnsafeDocumentationTiming));
        }

        [Test]
        public void CompleteVerticalSlice_RequiresSafetyHandoffRecordAndRecovery()
        {
            CrisisOrchestrationEngine engine = Engine();

            engine.Apply(CrisisOrchestrationAction.PauseAndRegulate);
            engine.Apply(CrisisOrchestrationAction.MovePeersToSafety);
            engine.Apply(CrisisOrchestrationAction.RequestColleagueSupport);
            engine.Apply(CrisisOrchestrationAction.ConfirmSupportArrival);
            engine.Apply(CrisisOrchestrationAction.HandoffWithBriefing);
            engine.Apply(CrisisOrchestrationAction.AddressStudent);
            engine.Apply(CrisisOrchestrationAction.AddressStudent);
            CrisisOrchestrationResolution record = engine.Apply(
                CrisisOrchestrationAction.RecordObjectiveFacts);
            CrisisOrchestrationResolution recovery = engine.Apply(
                CrisisOrchestrationAction.RequestTeacherRecoverySupport);

            Assert.That(record.accepted, Is.True);
            Assert.That(record.evidence,
                Does.Contain(CrisisOrchestrationEvidenceId.ObjectiveDocumentation));
            Assert.That(recovery.accepted, Is.True);
            Assert.That(recovery.after.phase, Is.EqualTo(CrisisOrchestrationPhase.Complete));
            Assert.That(recovery.after.recoveryPlanCompleted, Is.True);
        }

        [Test]
        public void CurrentState_IsDefensiveCopy()
        {
            CrisisOrchestrationEngine engine = Engine();
            CrisisOrchestrationState exposed = engine.Current;
            exposed.teacher.arousal = 0f;

            Assert.That(engine.Current.teacher.arousal, Is.EqualTo(0.82f).Within(0.001f));
        }

        private static CrisisOrchestrationEngine Engine()
        {
            return new CrisisOrchestrationEngine(Initial());
        }

        private static CrisisOrchestrationState Initial()
        {
            return new CrisisOrchestrationState
            {
                phase = CrisisOrchestrationPhase.Assess,
                teacher = new TeacherOperationalState
                {
                    arousal = 0.82f,
                    regulationCapacity = 0.28f,
                    physicalSafety = 0.42f,
                    perceivedSupport = 0.12f,
                    responseConfidence = 0.38f
                },
                classroom = new ClassroomOperationalState
                {
                    focalStudentRisk = 0.82f,
                    peerDistress = 0.68f,
                    noise = 0.64f,
                    peersInUnsafeArea = 4,
                    learningContinuity = 0.4f
                },
                support = new SupportResourceState
                {
                    colleagueAvailable = true,
                    administratorAvailable = true,
                    counselorAvailable = true
                }
            };
        }
    }
}
