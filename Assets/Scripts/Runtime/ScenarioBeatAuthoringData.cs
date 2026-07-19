using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum CrisisStage
    {
        Trigger = 0,
        Escalation = 1,
        Peak = 2,
        Deescalation = 3,
        Reconnection = 4,
        InstructionalReentry = 5
    }

    [Serializable]
    public sealed class ScenarioBeatAuthoringSeed
    {
        public string scenarioId;
        public string trigger;
        public CrisisStage stage;
        public StudentPersonaAsset studentPersona;
        public CrisisScenarioType crisisType;
        public TeacherCompetency[] teacherGoals;
        public TrainingSafetyFlag[] safetyFlags;
        public PeerAttentionPattern peerAttention;
        public bool presentationAvoidance;
        public ScenarioBeat beat;
    }

    [Serializable]
    public sealed class ScenarioBeatAuthoringData
    {
        [Header("Research Metadata")]
        [SerializeField] private string scenarioId;
        [SerializeField, TextArea(2, 4)] private string trigger;
        [SerializeField] private CrisisStage stage;
        [SerializeField] private StudentPersonaAsset studentPersona;
        [SerializeField] private CrisisScenarioType crisisType;
        [SerializeField] private TeacherCompetency[] teacherGoals = Array.Empty<TeacherCompetency>();
        [SerializeField] private TrainingSafetyFlag[] safetyFlags = Array.Empty<TrainingSafetyFlag>();
        [SerializeField] private PeerAttentionPattern peerAttention;
        [SerializeField] private bool presentationAvoidance;

        [Header("Simulation Beat")]
        [SerializeField] private ScenarioBeat beat = new ScenarioBeat();

        public string ScenarioId => scenarioId;
        public string Trigger => trigger;
        public CrisisStage Stage => stage;
        public StudentPersonaAsset StudentPersona => studentPersona;
        public CrisisScenarioType CrisisType => crisisType;
        public IReadOnlyList<TeacherCompetency> TeacherGoals => teacherGoals;
        public PeerAttentionPattern PeerAttention => peerAttention;
        public bool PresentationAvoidance => presentationAvoidance;
        public ScenarioBeat Beat => beat;

        public ScenarioBeat ToRuntimeBeat()
        {
            TeacherResponseOption[] sourceOptions = beat.options ?? Array.Empty<TeacherResponseOption>();
            var runtimeOptions = new TeacherResponseOption[sourceOptions.Length];
            for (int index = 0; index < sourceOptions.Length; index++)
            {
                runtimeOptions[index] = CopyOption(sourceOptions[index]);
            }

            return new ScenarioBeat
            {
                title = beat.title,
                studentLine = beat.studentLine,
                observation = beat.observation,
                entryGesture = beat.entryGesture,
                gestureIntensity = beat.gestureIntensity,
                options = runtimeOptions
            };
        }

        public CrisisScenarioProfile ToResearchProfile(TrainingSceneId sceneId)
        {
            return new CrisisScenarioProfile
            {
                id = scenarioId,
                title = beat.title,
                sceneId = sceneId,
                crisisType = crisisType,
                personaId = studentPersona != null ? studentPersona.PersonaId : string.Empty,
                evidenceTargets = (TeacherCompetency[])teacherGoals.Clone(),
                safetyFlags = (TrainingSafetyFlag[])safetyFlags.Clone(),
                peerAttention = peerAttention,
                presentationAvoidance = presentationAvoidance
            };
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(ScenarioBeatAuthoringSeed seed)
        {
            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            scenarioId = seed.scenarioId;
            trigger = seed.trigger;
            stage = seed.stage;
            studentPersona = seed.studentPersona;
            crisisType = seed.crisisType;
            teacherGoals = seed.teacherGoals ?? Array.Empty<TeacherCompetency>();
            safetyFlags = seed.safetyFlags ?? Array.Empty<TrainingSafetyFlag>();
            peerAttention = seed.peerAttention;
            presentationAvoidance = seed.presentationAvoidance;
            beat = seed.beat ?? new ScenarioBeat();
        }
#endif

        private static TeacherResponseOption CopyOption(TeacherResponseOption source)
        {
            if (source == null)
            {
                return new TeacherResponseOption();
            }

            CompetencyEvidence[] sourceEvidence = source.competencyEvidence ?? Array.Empty<CompetencyEvidence>();
            var evidence = new CompetencyEvidence[sourceEvidence.Length];
            for (int index = 0; index < sourceEvidence.Length; index++)
            {
                CompetencyEvidence item = sourceEvidence[index];
                evidence[index] = item == null
                    ? null
                    : new CompetencyEvidence
                    {
                        evidenceId = item.evidenceId,
                        dimension = item.dimension,
                        score = item.score
                    };
            }

            return new TeacherResponseOption
            {
                label = source.label,
                spokenResponse = source.spokenResponse,
                quality = source.quality,
                rationale = source.rationale,
                resultingAffect = source.resultingAffect,
                competencyEvidence = evidence
            };
        }
    }
}
