using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class TrainingScenarioAssetTests
    {
        [Test]
        public void ScenarioCatalog_LoadsAuthoredPersonasAndBothClassroomScenes()
        {
            // Given / When
            TrainingScenarioCatalog catalog = TrainingScenarioCatalog.LoadDefault();

            // Then
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.StudentPersonas.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(catalog.Scenarios.Select(item => item.SceneId),
                Is.EquivalentTo(new[]
                {
                    TrainingSceneId.GeneralClassroom,
                    TrainingSceneId.CircleDiscussion,
                    TrainingSceneId.RecoveryRoom,
                    TrainingSceneId.Schoolyard,
                    TrainingSceneId.Gymnasium
                }));
        }

        [Test]
        public void AuthoredScenario_ExposesTriggerStageAndTeacherGoalsForEveryBeat()
        {
            // Given
            TrainingScenarioCatalog catalog = TrainingScenarioCatalog.LoadDefault();

            // When
            ScenarioBeatAuthoringData[] beats = catalog.Scenarios
                .SelectMany(item => item.AuthoredBeats)
                .ToArray();

            // Then
            Assert.That(beats, Has.Length.EqualTo(30));
            Assert.That(beats.All(item => !string.IsNullOrWhiteSpace(item.Trigger)), Is.True);
            Assert.That(beats.Select(item => item.Stage).Distinct().Count(), Is.GreaterThanOrEqualTo(4));
            Assert.That(beats.All(item => item.TeacherGoals.Count > 0), Is.True);
            Assert.That(beats.All(item => item.StudentPersona != null), Is.True);
        }

        [Test]
        public void ScenarioLibrary_ReturnsFreshRuntimeCopiesFromAuthoredAssets()
        {
            // Given
            ScenarioBeat[] first = TrainingScenarioLibrary.BuildDefaultScenario();

            // When
            first[0].title = "mutated";
            first[0].options[0].quality = 0;
            ScenarioBeat[] second = TrainingScenarioLibrary.BuildDefaultScenario();

            // Then
            Assert.That(second[0].title, Is.Not.EqualTo("mutated"));
            Assert.That(second[0].options[0].quality, Is.EqualTo(3));
        }

        [Test]
        public void CircleScenario_AuthorsPeerAudienceAndPresentationAvoidanceAcrossTheSequence()
        {
            TrainingScenarioAsset circle = TrainingScenarioCatalog.LoadDefault()
                .ScenarioFor(TrainingSceneId.CircleDiscussion);

            Assert.That(circle.AuthoredBeats.Count, Is.EqualTo(6));
            Assert.That(circle.AuthoredBeats.Any(item =>
                item.PeerAttention == PeerAttentionPattern.PresentationAudience), Is.True);
            Assert.That(circle.AuthoredBeats.Count(item => item.PresentationAvoidance),
                Is.GreaterThanOrEqualTo(3));
            Assert.That(circle.AuthoredBeats.Any(item => item.Stage == CrisisStage.Reconnection), Is.True);
            Assert.That(circle.AuthoredBeats.Last().Stage, Is.EqualTo(CrisisStage.InstructionalReentry));
        }
    }
}
