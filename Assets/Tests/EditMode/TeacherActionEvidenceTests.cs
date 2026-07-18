using System.Linq;
using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class TeacherActionEvidenceTests
    {
        [Test]
        public void ChoiceEvidence_UsesScenarioTargetsOnly()
        {
            CrisisScenarioProfile scenario = TrainingResearchCatalog.BuildCrisisScenarios()[0];
            var option = new TeacherResponseOption { quality = 3 };

            CompetencyEvidence[] evidence = TeacherActionEvidenceEvaluator.ForChoice(scenario, option);

            Assert.That(evidence, Has.Length.EqualTo(scenario.evidenceTargets.Length));
            Assert.That(evidence.All(item => scenario.evidenceTargets.Contains(item.dimension)), Is.True);
            Assert.That(evidence.All(item => item.score == 3f), Is.True);
        }

        [Test]
        public void SupportiveFreeText_ProducesAgencyAndEmotionEvidence()
        {
            string utterance = new string(new[]
            {
                '\uAD1C', '\uCC2E', '\uC544', ' ', '\uC120', '\uD0DD', '\uD574'
            });

            TeacherResponseOption assessment = TeacherActionEvidenceEvaluator.ForUtterance(utterance);

            Assert.That(assessment.quality, Is.GreaterThanOrEqualTo(2));
            Assert.That(Find(assessment, TeacherCompetency.EmotionAcknowledgement).score, Is.GreaterThanOrEqualTo(2.5f));
            Assert.That(Find(assessment, TeacherCompetency.StudentAgency).score, Is.GreaterThanOrEqualTo(2.5f));
        }

        [Test]
        public void CoerciveFreeText_DoesNotReceiveSupportiveScore()
        {
            string utterance = new string(new[]
            {
                '\uB2F9', '\uC7A5', ' ', '\uD574'
            });

            TeacherResponseOption assessment = TeacherActionEvidenceEvaluator.ForUtterance(utterance);

            Assert.That(assessment.quality, Is.LessThanOrEqualTo(1));
            Assert.That(Find(assessment, TeacherCompetency.LowStimulusResponse).score, Is.LessThan(1f));
        }

        private static CompetencyEvidence Find(
            TeacherResponseOption option,
            TeacherCompetency competency)
        {
            return option.competencyEvidence.Single(item => item.dimension == competency);
        }
    }
}
