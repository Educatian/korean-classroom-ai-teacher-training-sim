using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [CreateAssetMenu(
        fileName = "ScenarioCatalog",
        menuName = "Teacher Training/Scenario Catalog",
        order = 30)]
    public sealed class TrainingScenarioCatalog : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/ScenarioCatalog";

        [Header("Authoring Assets")]
        [SerializeField] private StudentPersonaAsset[] studentPersonas = Array.Empty<StudentPersonaAsset>();
        [SerializeField] private TrainingScenarioAsset[] scenarios = Array.Empty<TrainingScenarioAsset>();

        public IReadOnlyList<StudentPersonaAsset> StudentPersonas => studentPersonas;
        public IReadOnlyList<TrainingScenarioAsset> Scenarios => scenarios;

        public static TrainingScenarioCatalog LoadDefault()
        {
            TrainingScenarioCatalog catalog = Resources.Load<TrainingScenarioCatalog>(DefaultResourcePath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"Training scenario catalog is missing at Resources/{DefaultResourcePath}.asset.");
            }

            return catalog;
        }

        public TrainingScenarioAsset ScenarioFor(TrainingSceneId sceneId)
        {
            for (int index = 0; index < scenarios.Length; index++)
            {
                TrainingScenarioAsset scenario = scenarios[index];
                if (scenario != null && scenario.SceneId == sceneId)
                {
                    return scenario;
                }
            }

            throw new InvalidOperationException($"No authored training scenario exists for {sceneId}.");
        }

        public StudentPersonaProfile[] BuildPersonas()
        {
            var profiles = new StudentPersonaProfile[studentPersonas.Length];
            for (int index = 0; index < studentPersonas.Length; index++)
            {
                profiles[index] = studentPersonas[index].ToRuntimeProfile();
            }

            return profiles;
        }

        public CrisisScenarioProfile[] BuildCrisisScenarios()
        {
            var profiles = new List<CrisisScenarioProfile>();
            for (int index = 0; index < scenarios.Length; index++)
            {
                profiles.AddRange(scenarios[index].BuildResearchProfiles());
            }

            return profiles.ToArray();
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            StudentPersonaAsset[] authoredPersonas,
            TrainingScenarioAsset[] authoredScenarios)
        {
            studentPersonas = authoredPersonas ?? Array.Empty<StudentPersonaAsset>();
            scenarios = authoredScenarios ?? Array.Empty<TrainingScenarioAsset>();
        }
#endif
    }
}