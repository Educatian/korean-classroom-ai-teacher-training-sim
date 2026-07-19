using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [CreateAssetMenu(
        fileName = "TrainingScenario",
        menuName = "Teacher Training/Training Scenario",
        order = 20)]
    public sealed class TrainingScenarioAsset : ScriptableObject
    {
        [Header("Scenario Identity")]
        [SerializeField] private string scenarioId;
        [SerializeField] private string displayName;
        [SerializeField] private TrainingSceneId sceneId;
        [SerializeField, TextArea(2, 5)] private string researchNotes;

        [Header("Ordered Crisis Sequence")]
        [SerializeField] private ScenarioBeatAuthoringData[] beats = Array.Empty<ScenarioBeatAuthoringData>();

        public string ScenarioId => scenarioId;
        public string DisplayName => displayName;
        public TrainingSceneId SceneId => sceneId;
        public string ResearchNotes => researchNotes;
        public IReadOnlyList<ScenarioBeatAuthoringData> AuthoredBeats => beats;

        public ScenarioBeat[] BuildRuntimeBeats()
        {
            var runtime = new ScenarioBeat[beats.Length];
            for (int index = 0; index < beats.Length; index++)
            {
                runtime[index] = beats[index].ToRuntimeBeat();
            }

            return runtime;
        }

        public CrisisScenarioProfile[] BuildResearchProfiles()
        {
            var profiles = new CrisisScenarioProfile[beats.Length];
            for (int index = 0; index < beats.Length; index++)
            {
                profiles[index] = beats[index].ToResearchProfile(sceneId);
            }

            return profiles;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string authoredScenarioId,
            string authoredDisplayName,
            TrainingSceneId authoredSceneId,
            string authoredResearchNotes,
            ScenarioBeatAuthoringData[] authoredBeats)
        {
            scenarioId = authoredScenarioId;
            displayName = authoredDisplayName;
            sceneId = authoredSceneId;
            researchNotes = authoredResearchNotes;
            beats = authoredBeats ?? Array.Empty<ScenarioBeatAuthoringData>();
        }
#endif
    }
}