using System;

namespace AdieLab.TeacherTraining
{
    public static class TrainingSceneRegistry
    {
        private const int KoreanClassroomTraining = 0;
        private const int KoreanClassroomCircleTraining = 0;
        private static readonly string AssetPrefix = new string(new[]
        {
            'A', 's', 's', 'e', 't', 's', '/', 'S', 'c', 'e', 'n', 'e', 's', '/'
        });
        private static readonly string SceneExtension = new string(new[]
        {
            '.', 'u', 'n', 'i', 't', 'y'
        });

        public static string SceneName(TrainingSceneId sceneId)
        {
            return sceneId switch
            {
                TrainingSceneId.GeneralClassroom => nameof(KoreanClassroomTraining),
                TrainingSceneId.CircleDiscussion => nameof(KoreanClassroomCircleTraining),
                _ => throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, null)
            };
        }

        public static string SceneAssetPath(TrainingSceneId sceneId)
        {
            return string.Concat(AssetPrefix, SceneName(sceneId), SceneExtension);
        }

        public static string[] SceneAssetPaths()
        {
            return new[]
            {
                SceneAssetPath(TrainingSceneId.GeneralClassroom),
                SceneAssetPath(TrainingSceneId.CircleDiscussion)
            };
        }

        public static TrainingSceneId Other(TrainingSceneId sceneId)
        {
            return sceneId switch
            {
                TrainingSceneId.GeneralClassroom => TrainingSceneId.CircleDiscussion,
                TrainingSceneId.CircleDiscussion => TrainingSceneId.GeneralClassroom,
                _ => throw new ArgumentOutOfRangeException(nameof(sceneId), sceneId, null)
            };
        }

        public static TrainingSceneId FromSceneName(string sceneName)
        {
            return string.Equals(
                    sceneName,
                    SceneName(TrainingSceneId.CircleDiscussion),
                    StringComparison.Ordinal)
                ? TrainingSceneId.CircleDiscussion
                : TrainingSceneId.GeneralClassroom;
        }
    }

    public enum TrainingDeploymentTarget
    {
        WindowsDesktop = 0,
        WebGl = 1,
        ImmersiveVr = 2
    }

    public enum DeploymentReadiness
    {
        Ready = 0,
        Constrained = 1
    }

    [Serializable]
    public sealed class SceneDeploymentConfig
    {
        public TrainingDeploymentTarget target;
        public DeploymentReadiness readiness;
        public bool supportsMicrophone;
        public bool supportsLocalResearchLog;
        public bool supportsEnvironmentSecret;
        public bool requiresXrProvider;
        public bool requiresWorldSpaceHud;

        public static SceneDeploymentConfig For(TrainingDeploymentTarget target)
        {
            return target switch
            {
                TrainingDeploymentTarget.WindowsDesktop => new SceneDeploymentConfig
                {
                    target = target,
                    readiness = DeploymentReadiness.Ready,
                    supportsMicrophone = true,
                    supportsLocalResearchLog = true,
                    supportsEnvironmentSecret = true,
                    requiresXrProvider = false,
                    requiresWorldSpaceHud = false
                },
                TrainingDeploymentTarget.WebGl => new SceneDeploymentConfig
                {
                    target = target,
                    readiness = DeploymentReadiness.Constrained,
                    supportsMicrophone = false,
                    supportsLocalResearchLog = false,
                    supportsEnvironmentSecret = false,
                    requiresXrProvider = false,
                    requiresWorldSpaceHud = false
                },
                TrainingDeploymentTarget.ImmersiveVr => new SceneDeploymentConfig
                {
                    target = target,
                    readiness = DeploymentReadiness.Constrained,
                    supportsMicrophone = true,
                    supportsLocalResearchLog = true,
                    supportsEnvironmentSecret = true,
                    requiresXrProvider = true,
                    requiresWorldSpaceHud = true
                },
                _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
            };
        }
    }
}
