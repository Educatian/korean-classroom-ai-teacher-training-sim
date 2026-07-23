using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class CrisisOrchestrationAssessmentTests
    {
        [Test]
        public void RuntimeModel_DefinesEightProposalAlignedCompetencies()
        {
            CrisisOrchestrationAssessmentModel model =
                CrisisOrchestrationAssessmentModel.CreateRuntimeDefault();

            Assert.That(model.Competencies.Count, Is.EqualTo(8));
            Assert.That(model.Competencies[0].competency,
                Is.EqualTo(OrchestrationCompetency.TeacherSelfRegulation));
            Assert.That(model.Competencies[5].competency,
                Is.EqualTo(OrchestrationCompetency.GuardianCommunication));
        }

        [Test]
        public void DistressedInitialState_ReceivesNoNegativeScoreWithoutObservedAction()
        {
            CrisisOrchestrationAssessmentReport report =
                CrisisOrchestrationAssessmentEngine.Evaluate(
                    System.Array.Empty<CrisisOrchestrationResolution>());

            OrchestrationCompetencyResult self = Find(
                report, OrchestrationCompetency.TeacherSelfRegulation);
            Assert.That(self.score, Is.Zero);
            Assert.That(self.adverseEvidence, Is.Empty);
        }

        [Test]
        public void PositiveAndAdverseEvidence_AreBothRetainedForDebrief()
        {
            var actions = new[]
            {
                Resolution(CrisisOrchestrationEvidenceId.RegulatedBeforeIntervention),
                Resolution(CrisisOrchestrationEvidenceId.PrematureDirectIntervention)
            };

            OrchestrationCompetencyResult self = Find(
                CrisisOrchestrationAssessmentEngine.Evaluate(actions),
                OrchestrationCompetency.TeacherSelfRegulation);

            Assert.That(self.score, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(self.observedEvidence,
                Does.Contain(CrisisOrchestrationEvidenceId.RegulatedBeforeIntervention));
            Assert.That(self.adverseEvidence,
                Does.Contain(CrisisOrchestrationEvidenceId.PrematureDirectIntervention));
        }

        [Test]
        public void FirstVerticalSlice_DoesNotFabricateGuardianCommunicationEvidence()
        {
            var actions = new[]
            {
                Resolution(CrisisOrchestrationEvidenceId.ClearHandoff),
                Resolution(CrisisOrchestrationEvidenceId.ObjectiveDocumentation)
            };

            OrchestrationCompetencyResult guardian = Find(
                CrisisOrchestrationAssessmentEngine.Evaluate(actions),
                OrchestrationCompetency.GuardianCommunication);

            Assert.That(guardian.score, Is.Zero);
            Assert.That(guardian.observedEvidence, Is.Empty);
        }

        private static CrisisOrchestrationResolution Resolution(
            CrisisOrchestrationEvidenceId evidence)
        {
            return new CrisisOrchestrationResolution
            {
                accepted = true,
                evidence = new[] { evidence }
            };
        }

        private static OrchestrationCompetencyResult Find(
            CrisisOrchestrationAssessmentReport report,
            OrchestrationCompetency competency)
        {
            foreach (OrchestrationCompetencyResult result in report.competencies)
                if (result.competency == competency) return result;
            Assert.Fail("Missing orchestration competency: " + competency);
            return null;
        }
    }
}
