using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class CrisisOrchestrationScenarioAssetTests
    {
        private const string ResourcePath =
            "Training/Orchestration/TeacherDirectedAggression";

        [Test]
        public void AuthoredScenario_CoversCompleteTeamResponseSequence()
        {
            CrisisOrchestrationScenarioAsset scenario =
                Resources.Load<CrisisOrchestrationScenarioAsset>(ResourcePath);

            Assert.That(scenario, Is.Not.Null,
                "Generate the scenario from Teacher Training/Content before running this test.");
            Assert.That(scenario.Beats.Count, Is.GreaterThanOrEqualTo(7));
            Assert.That(AllActions(scenario), Does.Contain(
                CrisisOrchestrationAction.PauseAndRegulate));
            Assert.That(AllActions(scenario), Does.Contain(
                CrisisOrchestrationAction.MovePeersToSafety));
            Assert.That(AllActions(scenario), Does.Contain(
                CrisisOrchestrationAction.RequestColleagueSupport));
            Assert.That(AllActions(scenario), Does.Contain(
                CrisisOrchestrationAction.HandoffWithBriefing));
            Assert.That(AllActions(scenario), Does.Contain(
                CrisisOrchestrationAction.RecordObjectiveFacts));
            Assert.That(AllActions(scenario), Does.Contain(
                CrisisOrchestrationAction.RequestTeacherRecoverySupport));
        }

        [Test]
        public void AuthoredScenario_MakesSafetyBoundaryAndNonScoredFeelingExplicit()
        {
            CrisisOrchestrationScenarioAsset scenario =
                Resources.Load<CrisisOrchestrationScenarioAsset>(ResourcePath);

            Assert.That(scenario.SafetyBoundary, Does.Contain("안전 절차"));
            Assert.That(scenario.Beats[0].privateTeacherPrompt,
                Does.Contain("감정에는 점수가 없습니다"));
        }

        private static CrisisOrchestrationAction[] AllActions(
            CrisisOrchestrationScenarioAsset scenario)
        {
            return scenario.Beats
                .Where(beat => beat != null && beat.options != null)
                .SelectMany(beat => beat.options)
                .Where(option => option != null)
                .Select(option => option.action)
                .ToArray();
        }
    }
}
