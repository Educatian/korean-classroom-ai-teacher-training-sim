using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    public sealed class CrisisOrchestrationActionOption
    {
        public CrisisOrchestrationAction action;
        public string label = string.Empty;
        [TextArea(2, 5)] public string operationalScript = string.Empty;
        [TextArea(2, 5)] public string rationale = string.Empty;
    }

    [Serializable]
    public sealed class CrisisOrchestrationScenarioBeat
    {
        public string beatId = string.Empty;
        public CrisisOrchestrationPhase phase;
        public string title = string.Empty;
        [TextArea(2, 6)] public string situation = string.Empty;
        [TextArea(2, 5)] public string privateTeacherPrompt = string.Empty;
        [TextArea(2, 5)] public string observableSignals = string.Empty;
        [TextArea(2, 6)] public string facilitatorNote = string.Empty;
        [TextArea(2, 5)] public string debriefPrompt = string.Empty;
        [Tooltip("이 행동이 수용되면 다음 비트로 전환합니다.")]
        public CrisisOrchestrationAction completionAction;
        public CrisisOrchestrationActionOption[] options =
            Array.Empty<CrisisOrchestrationActionOption>();
    }

    [CreateAssetMenu(
        fileName = "CrisisOrchestrationScenario",
        menuName = "Teacher Training/Crisis Orchestration Scenario",
        order = 48)]
    public sealed class CrisisOrchestrationScenarioAsset : ScriptableObject
    {
        [SerializeField] private string scenarioId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea(2, 6)] private string learningPurpose = string.Empty;
        [SerializeField, TextArea(2, 6)] private string context = string.Empty;
        [SerializeField, TextArea(2, 6)] private string safetyBoundary = string.Empty;
        [SerializeField] private CrisisOrchestrationState initialState =
            new CrisisOrchestrationState();
        [SerializeField] private CrisisOrchestrationScenarioBeat[] beats =
            Array.Empty<CrisisOrchestrationScenarioBeat>();

        public string ScenarioId => scenarioId;
        public string DisplayName => displayName;
        public string LearningPurpose => learningPurpose;
        public string Context => context;
        public string SafetyBoundary => safetyBoundary;
        public CrisisOrchestrationState InitialState => initialState?.Copy();
        public IReadOnlyList<CrisisOrchestrationScenarioBeat> Beats => beats;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string authoredScenarioId,
            string authoredDisplayName,
            string authoredLearningPurpose,
            string authoredContext,
            string authoredSafetyBoundary,
            CrisisOrchestrationState authoredInitialState,
            CrisisOrchestrationScenarioBeat[] authoredBeats)
        {
            scenarioId = authoredScenarioId ?? string.Empty;
            displayName = authoredDisplayName ?? string.Empty;
            learningPurpose = authoredLearningPurpose ?? string.Empty;
            context = authoredContext ?? string.Empty;
            safetyBoundary = authoredSafetyBoundary ?? string.Empty;
            initialState = authoredInitialState?.Copy() ?? new CrisisOrchestrationState();
            beats = authoredBeats ?? Array.Empty<CrisisOrchestrationScenarioBeat>();
        }
#endif
    }
}
