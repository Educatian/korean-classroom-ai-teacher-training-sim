using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class ApprovedKnowledgeCatalogTests
    {
        [Test]
        public void Retrieve_ExcludesUnapprovedSources()
        {
            ApprovedKnowledgeCatalog catalog = ApprovedKnowledgeCatalog.CreateRuntime(
                Source("approved", true, "낮은 자극으로 기다림을 제공한다."),
                Source("draft", false, "즉시 지시를 반복한다."));

            GroundedKnowledgeCitation[] results = catalog.Retrieve(
                "낮은 자극 기다림",
                CrisisStage.Escalation,
                new[] { TeacherCompetency.LowStimulusResponse });

            Assert.That(results, Has.Length.EqualTo(1));
            Assert.That(results[0].sourceId, Is.EqualTo("approved"));
        }

        [Test]
        public void Retrieve_RanksStageAndCompetencyMatchFirst()
        {
            ApprovedKnowledgeCatalog catalog = ApprovedKnowledgeCatalog.CreateRuntime(
                Source("generic", true, "학생을 관찰한다.", System.Array.Empty<CrisisStage>(), System.Array.Empty<TeacherCompetency>()),
                Source("matched", true, "요구 강도를 낮추고 처리 시간을 제공한다.",
                    new[] { CrisisStage.Escalation },
                    new[] { TeacherCompetency.LowStimulusResponse }));

            GroundedKnowledgeCitation[] results = catalog.Retrieve(
                "처리 시간",
                CrisisStage.Escalation,
                new[] { TeacherCompetency.LowStimulusResponse });

            Assert.That(results[0].sourceId, Is.EqualTo("matched"));
            Assert.That(results[0].locator, Is.EqualTo("p. 12"));
        }

        [Test]
        public void Retrieve_EnforcesRequestedResultLimit()
        {
            ApprovedKnowledgeCatalog catalog = ApprovedKnowledgeCatalog.CreateRuntime(
                Source("one", true, "안전 거리 확보"),
                Source("two", true, "안전 거리 확인"),
                Source("three", true, "안전 거리 유지"));

            GroundedKnowledgeCitation[] results = catalog.Retrieve(
                "안전 거리", CrisisStage.Escalation,
                new[] { TeacherCompetency.LowStimulusResponse }, 2);

            Assert.That(results, Has.Length.EqualTo(2));
        }

        private static ApprovedKnowledgeSource Source(
            string id,
            bool approved,
            string passage,
            CrisisStage[] stages = null,
            TeacherCompetency[] competencies = null)
        {
            return new ApprovedKnowledgeSource
            {
                sourceId = id,
                title = "연구진 승인 지침 " + id,
                version = "2026-07",
                approvedForCoaching = approved,
                approvedAtUtc = "2026-07-22T00:00:00Z",
                chunks = new[]
                {
                    new ApprovedKnowledgeChunk
                    {
                        chunkId = id + "-1",
                        locator = "p. 12",
                        passage = passage,
                        stages = stages ?? new[] { CrisisStage.Escalation },
                        competencies = competencies ?? new[] { TeacherCompetency.LowStimulusResponse }
                    }
                }
            };
        }
    }
}
