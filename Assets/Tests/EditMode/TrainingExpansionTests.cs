using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
namespace AdieLab.TeacherTraining.Tests
{
    public sealed class TrainingExpansionTests
    {
        [Test]
        public void ScenarioLibrary_BuildsSixDistinctCrisisContexts()
        {
            // Given / When
            ScenarioBeat[] beats = TrainingScenarioLibrary.BuildDefaultScenario();

            // Then
            Assert.That(beats, Has.Length.EqualTo(6));
            Assert.That(beats.Select(beat => beat.title).Distinct().Count(), Is.EqualTo(6));
            Assert.That(beats.All(beat => beat.options != null && beat.options.Length == 3), Is.True);
        }

        [Test]
        public void CircleDiscussionScenario_CoversEscalationAndSupportedReentry()
        {
            // Given / When
            ScenarioBeat[] beats = TrainingScenarioLibrary.BuildCircleDiscussionScenario();

            // Then
            Assert.That(beats, Has.Length.EqualTo(6));
            Assert.That(beats.Select(beat => beat.title).Distinct().Count(), Is.EqualTo(6));
            Assert.That(beats.All(beat => beat.options.Length == 3), Is.True);
            Assert.That(beats.All(beat => beat.gestureIntensity >= 0.5f), Is.True);
            Assert.That(beats.Last().entryGesture, Is.EqualTo(BehaviorGesture.Recover));
            Assert.That(beats.Last().options.Max(option => option.quality), Is.EqualTo(3));
        }

        [Test]
        public void ConversationMemory_KeepsOnlyTheLatestBoundedTurns()
        {
            // Given
            var memory = new ConversationMemory(2);

            // When
            memory.Add("첫 번째 교사 발화", "첫 번째 학생 응답");
            memory.Add("두 번째 교사 발화", "두 번째 학생 응답");
            memory.Add("세 번째 교사 발화", "세 번째 학생 응답");
            string context = memory.BuildContext();

            // Then
            Assert.That(context, Does.Not.Contain("첫 번째 교사 발화"));
            Assert.That(context, Does.Contain("두 번째 학생 응답"));
            Assert.That(context, Does.Contain("세 번째 교사 발화"));
        }

        [Test]
        public void RubricEvaluator_ProducesSixDimensionsAndOverallLevel()
        {
            // Given
            var choices = new List<TeacherResponseOption>
            {
                new TeacherResponseOption { quality = 3 },
                new TeacherResponseOption { quality = 2 },
                new TeacherResponseOption { quality = 3 }
            };

            // When
            RubricSummary summary = TeacherRubricEvaluator.Evaluate(choices);

            // Then
            Assert.That(summary.dimensions, Has.Length.EqualTo(6));
            Assert.That(summary.averageScore, Is.GreaterThan(0f));
            Assert.That(
                summary.dimensions.All(item =>
                    System.Math.Abs(item.score - summary.averageScore) < 0.001f),
                Is.True);
            Assert.That(summary.overallLevel, Is.Not.Empty);
        }
    }
}
