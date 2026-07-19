using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum TrainingExperienceMode
    {
        Desktop = 0,
        ImmersiveVr = 1
    }

    public static class TrainingExperienceModePolicy
    {
        private const string PreferenceKey = "TeacherTraining.ExperienceMode";

        public static TrainingExperienceMode DefaultFor(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.Android
                ? TrainingExperienceMode.ImmersiveVr
                : TrainingExperienceMode.Desktop;
        }

        public static TrainingExperienceMode Load(RuntimePlatform platform)
        {
            int fallback = (int)DefaultFor(platform);
            return (TrainingExperienceMode)PlayerPrefs.GetInt(PreferenceKey, fallback);
        }

        public static void Save(TrainingExperienceMode mode)
        {
            PlayerPrefs.SetInt(PreferenceKey, (int)mode);
            PlayerPrefs.Save();
        }
    }
}
