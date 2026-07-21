using System.Linq;
using NUnit.Framework;
using UnityEngine;

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
        public void MetaQuestDeployment_RequiresSecureProxyInsteadOfEnvironmentSecret()
        {
            // Given / When
            SceneDeploymentConfig immersive = SceneDeploymentConfig.For(TrainingDeploymentTarget.ImmersiveVr);

            // Then
            Assert.That(immersive.supportsEnvironmentSecret, Is.False);
            Assert.That(immersive.requiresSecureLlmProxy, Is.True);
            Assert.That(immersive.requiresWorldSpaceHud, Is.True);
        }

        [Test]
        public void ExperienceMode_DefaultsToQuestOnAndroidAndDesktopElsewhere()
        {
            Assert.That(
                TrainingExperienceModePolicy.DefaultFor(RuntimePlatform.Android),
                Is.EqualTo(TrainingExperienceMode.ImmersiveVr));
            Assert.That(
                TrainingExperienceModePolicy.DefaultFor(RuntimePlatform.WindowsPlayer),
                Is.EqualTo(TrainingExperienceMode.Desktop));
            Assert.That(
                TrainingExperienceModePolicy.DefaultFor(RuntimePlatform.WindowsEditor),
                Is.EqualTo(TrainingExperienceMode.Desktop));
        }

        [Test]
        public void ExperienceMode_AndroidIgnoresPersistedDesktopPreference()
        {
            TrainingExperienceMode original = TrainingExperienceModePolicy.Load(RuntimePlatform.WindowsEditor);
            try
            {
                TrainingExperienceModePolicy.Save(TrainingExperienceMode.Desktop);
                Assert.That(
                    TrainingExperienceModePolicy.Load(RuntimePlatform.Android),
                    Is.EqualTo(TrainingExperienceMode.ImmersiveVr));
            }
            finally
            {
                TrainingExperienceModePolicy.Save(original);
            }
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
