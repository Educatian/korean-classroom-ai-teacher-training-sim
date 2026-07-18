using System;
using System.Linq;
using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class TrainingResearchCatalogTests
    {
        [Test]
        public void PersonaCatalog_ContainsDistinctUpperElementaryProfiles()
        {
            StudentPersonaProfile[] personas = TrainingResearchCatalog.BuildPersonas();

            Assert.That(personas, Has.Length.GreaterThanOrEqualTo(5));
            Assert.That(personas.Select(item => item.id).Distinct().Count(), Is.EqualTo(personas.Length));
            Assert.That(personas.All(item => item.gradeBand == StudentGradeBand.UpperElementary), Is.True);
            Assert.That(personas.All(item => item.supportNeeds != null && item.supportNeeds.Length > 0), Is.True);
        }

        [Test]
        public void CrisisCatalog_CoversFiveTypesWithExplicitEvidenceAndSafety()
        {
            CrisisScenarioProfile[] scenarios = TrainingResearchCatalog.BuildCrisisScenarios();

            Assert.That(scenarios, Has.Length.GreaterThanOrEqualTo(6));
            Assert.That(scenarios.Select(item => item.id).Distinct().Count(), Is.EqualTo(scenarios.Length));
            Assert.That(scenarios.Select(item => item.crisisType).Distinct().Count(), Is.GreaterThanOrEqualTo(5));
            Assert.That(scenarios.All(item => item.evidenceTargets != null && item.evidenceTargets.Length > 0), Is.True);
            Assert.That(scenarios.All(item => item.safetyFlags != null && item.safetyFlags.Length > 0), Is.True);
        }

        [Test]
        public void SceneTwoScenarios_UseAudienceAwarePeerAttention()
        {
            CrisisScenarioProfile[] sceneTwo = TrainingResearchCatalog.BuildCrisisScenarios()
                .Where(item => item.sceneId == TrainingSceneId.CircleDiscussion)
                .ToArray();

            Assert.That(sceneTwo, Is.Not.Empty);
            Assert.That(sceneTwo.All(item => item.peerAttention != PeerAttentionPattern.TeacherCentered), Is.True);
            Assert.That(sceneTwo.Any(item => item.presentationAvoidance), Is.True);
        }

        [Test]
        public void EveryScenario_ReferencesKnownPersona()
        {
            string[] personaIds = TrainingResearchCatalog.BuildPersonas()
                .Select(item => item.id)
                .ToArray();

            foreach (CrisisScenarioProfile scenario in TrainingResearchCatalog.BuildCrisisScenarios())
            {
                Assert.That(personaIds, Does.Contain(scenario.personaId));
            }
        }
    }
}
