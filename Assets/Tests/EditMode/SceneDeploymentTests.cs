using System.Linq;
using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class SceneDeploymentTests
    {
        [Test]
        public void SceneRegistry_MapsBothTrainingScenesDeterministically()
        {
            string general = TrainingSceneRegistry.SceneName(TrainingSceneId.GeneralClassroom);
            string circle = TrainingSceneRegistry.SceneName(TrainingSceneId.CircleDiscussion);

            Assert.That(general, Is.Not.Empty);
            Assert.That(circle, Is.Not.Empty.And.Not.EqualTo(general));
            Assert.That(TrainingSceneRegistry.Other(TrainingSceneId.GeneralClassroom), Is.EqualTo(TrainingSceneId.CircleDiscussion));
            Assert.That(TrainingSceneRegistry.Other(TrainingSceneId.CircleDiscussion), Is.EqualTo(TrainingSceneId.GeneralClassroom));
        }

        [Test]
        public void BuildRegistry_AlwaysIncludesBothCanonicalScenePaths()
        {
            string[] paths = TrainingSceneRegistry.SceneAssetPaths();

            Assert.That(paths, Has.Length.EqualTo(2));
            Assert.That(paths.Distinct().Count(), Is.EqualTo(2));
            Assert.That(paths, Does.Contain(TrainingSceneRegistry.SceneAssetPath(TrainingSceneId.GeneralClassroom)));
            Assert.That(paths, Does.Contain(TrainingSceneRegistry.SceneAssetPath(TrainingSceneId.CircleDiscussion)));
        }

        [Test]
        public void DeploymentConfig_DeclaresWindowsReadyAndConstrainedAlternates()
        {
            SceneDeploymentConfig windows = SceneDeploymentConfig.For(TrainingDeploymentTarget.WindowsDesktop);
            SceneDeploymentConfig web = SceneDeploymentConfig.For(TrainingDeploymentTarget.WebGl);
            SceneDeploymentConfig immersive = SceneDeploymentConfig.For(TrainingDeploymentTarget.ImmersiveVr);

            Assert.That(windows.readiness, Is.EqualTo(DeploymentReadiness.Ready));
            Assert.That(windows.supportsMicrophone, Is.True);
            Assert.That(web.readiness, Is.EqualTo(DeploymentReadiness.Constrained));
            Assert.That(web.supportsLocalResearchLog, Is.False);
            Assert.That(immersive.readiness, Is.EqualTo(DeploymentReadiness.Constrained));
            Assert.That(immersive.requiresXrProvider, Is.True);
        }
    }
}
