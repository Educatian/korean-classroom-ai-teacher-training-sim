using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum OrchestrationCompetency
    {
        TeacherSelfRegulation = 0,
        RiskAssessment = 1,
        HelpSeeking = 2,
        ClassroomSafetyOrchestration = 3,
        TeamCoordinationAndHandoff = 4,
        GuardianCommunication = 5,
        ObjectiveDocumentation = 6,
        PostIncidentRecovery = 7
    }

    [Serializable]
    public sealed class OrchestrationCompetencyDefinition
    {
        public OrchestrationCompetency competency;
        public string label = string.Empty;
        [TextArea] public string description = string.Empty;
        public CrisisOrchestrationEvidenceId[] positiveEvidence =
            Array.Empty<CrisisOrchestrationEvidenceId>();
        public CrisisOrchestrationEvidenceId[] adverseEvidence =
            Array.Empty<CrisisOrchestrationEvidenceId>();
        [Range(0f, 3f)] public float observedScore = 3f;
        [Range(0f, 3f)] public float adversePenalty = 1.5f;
    }

    [CreateAssetMenu(
        fileName = "CrisisOrchestrationAssessmentModel",
        menuName = "Teacher Training/Crisis Orchestration Assessment Model",
        order = 47)]
    public sealed class CrisisOrchestrationAssessmentModel : ScriptableObject
    {
        public const string DefaultResourcePath =
            "Training/Orchestration/CrisisOrchestrationAssessmentModel";

        [SerializeField] private string modelId = "crisis-orchestration-ecd-v1";
        [SerializeField] private int schemaVersion = 1;
        [SerializeField] private OrchestrationCompetencyDefinition[] competencies =
            Array.Empty<OrchestrationCompetencyDefinition>();

        public string ModelId => modelId;
        public int SchemaVersion => schemaVersion;
        public IReadOnlyList<OrchestrationCompetencyDefinition> Competencies => competencies;

        public static CrisisOrchestrationAssessmentModel LoadDefault()
        {
            CrisisOrchestrationAssessmentModel model =
                Resources.Load<CrisisOrchestrationAssessmentModel>(DefaultResourcePath);
            return model != null ? model : CreateRuntimeDefault();
        }

        public static CrisisOrchestrationAssessmentModel CreateRuntimeDefault()
        {
            CrisisOrchestrationAssessmentModel model =
                CreateInstance<CrisisOrchestrationAssessmentModel>();
            model.hideFlags = HideFlags.DontSave;
            model.competencies = new[]
            {
                Definition(OrchestrationCompetency.TeacherSelfRegulation,
                    "교사 자기조절", "감정을 부정하지 않고 안전한 판단 여유를 확보한다.",
                    new[] { CrisisOrchestrationEvidenceId.RegulatedBeforeIntervention },
                    new[] { CrisisOrchestrationEvidenceId.PrematureDirectIntervention }),
                Definition(OrchestrationCompetency.RiskAssessment,
                    "위험 판단", "학생, 교사, 주변 학생의 위험을 함께 판단한다.",
                    new[] { CrisisOrchestrationEvidenceId.TeacherStateAcknowledged,
                            CrisisOrchestrationEvidenceId.PeerSafetyPrioritized },
                    new[] { CrisisOrchestrationEvidenceId.UnsafeDocumentationTiming }),
                Definition(OrchestrationCompetency.HelpSeeking,
                    "지원 요청", "혼자 감당하기 어려운 상황에서 적시에 구체적인 도움을 요청한다.",
                    new[] { CrisisOrchestrationEvidenceId.TimelyHelpSeeking,
                            CrisisOrchestrationEvidenceId.EscalatedSupportRequest },
                    Array.Empty<CrisisOrchestrationEvidenceId>()),
                Definition(OrchestrationCompetency.ClassroomSafetyOrchestration,
                    "학급 안전 조율", "초점학생뿐 아니라 주변 학생과 학급 전체의 안전을 확보한다.",
                    new[] { CrisisOrchestrationEvidenceId.PeerSafetyPrioritized },
                    Array.Empty<CrisisOrchestrationEvidenceId>()),
                Definition(OrchestrationCompetency.TeamCoordinationAndHandoff,
                    "팀 조정과 인계", "관찰 사실, 위험, 실시 조치와 다음 역할을 명확히 인계한다.",
                    new[] { CrisisOrchestrationEvidenceId.ClearHandoff },
                    new[] { CrisisOrchestrationEvidenceId.UnsupportedHandoffAttempt }),
                Definition(OrchestrationCompetency.GuardianCommunication,
                    "보호자 협력 소통", "관찰 사실과 해석을 구분하며 후속 지원을 협의한다.",
                    Array.Empty<CrisisOrchestrationEvidenceId>(),
                    Array.Empty<CrisisOrchestrationEvidenceId>()),
                Definition(OrchestrationCompetency.ObjectiveDocumentation,
                    "객관적 기록", "진단과 비난 없이 시간, 관찰, 조치와 인계를 기록한다.",
                    new[] { CrisisOrchestrationEvidenceId.ObjectiveDocumentation },
                    new[] { CrisisOrchestrationEvidenceId.UnsafeDocumentationTiming }),
                Definition(OrchestrationCompetency.PostIncidentRecovery,
                    "사건 이후 회복", "동료 디브리핑과 필요한 심리·업무 지원을 계획한다.",
                    new[] { CrisisOrchestrationEvidenceId.TeacherRecoveryPlanned },
                    Array.Empty<CrisisOrchestrationEvidenceId>())
            };
            return model;
        }

        private static OrchestrationCompetencyDefinition Definition(
            OrchestrationCompetency competency,
            string label,
            string description,
            CrisisOrchestrationEvidenceId[] positive,
            CrisisOrchestrationEvidenceId[] adverse)
        {
            return new OrchestrationCompetencyDefinition
            {
                competency = competency,
                label = label,
                description = description,
                positiveEvidence = positive,
                adverseEvidence = adverse
            };
        }
    }

    [Serializable]
    public sealed class OrchestrationCompetencyResult
    {
        public OrchestrationCompetency competency;
        public string label = string.Empty;
        [Range(0f, 3f)] public float score;
        public CrisisOrchestrationEvidenceId[] observedEvidence =
            Array.Empty<CrisisOrchestrationEvidenceId>();
        public CrisisOrchestrationEvidenceId[] adverseEvidence =
            Array.Empty<CrisisOrchestrationEvidenceId>();
    }

    [Serializable]
    public sealed class CrisisOrchestrationAssessmentReport
    {
        public string modelId = string.Empty;
        public int modelSchemaVersion;
        [Range(0f, 3f)] public float averageScore;
        public OrchestrationCompetencyResult[] competencies =
            Array.Empty<OrchestrationCompetencyResult>();
    }

    public static class CrisisOrchestrationAssessmentEngine
    {
        public static CrisisOrchestrationAssessmentReport Evaluate(
            IReadOnlyList<CrisisOrchestrationResolution> actions,
            CrisisOrchestrationAssessmentModel model = null)
        {
            model = model != null
                ? model
                : CrisisOrchestrationAssessmentModel.CreateRuntimeDefault();
            var allEvidence = new HashSet<CrisisOrchestrationEvidenceId>();
            if (actions != null)
            {
                for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
                {
                    CrisisOrchestrationEvidenceId[] evidence = actions[actionIndex]?.evidence;
                    if (evidence == null) continue;
                    for (int evidenceIndex = 0; evidenceIndex < evidence.Length; evidenceIndex++)
                        allEvidence.Add(evidence[evidenceIndex]);
                }
            }

            var results = new List<OrchestrationCompetencyResult>();
            float total = 0f;
            for (int index = 0; index < model.Competencies.Count; index++)
            {
                OrchestrationCompetencyDefinition definition = model.Competencies[index];
                if (definition == null) continue;
                CrisisOrchestrationEvidenceId[] positive = Matching(
                    definition.positiveEvidence, allEvidence);
                CrisisOrchestrationEvidenceId[] adverse = Matching(
                    definition.adverseEvidence, allEvidence);
                float score = positive.Length > 0 ? definition.observedScore : 0f;
                score = Mathf.Clamp(score - adverse.Length * definition.adversePenalty, 0f, 3f);
                results.Add(new OrchestrationCompetencyResult
                {
                    competency = definition.competency,
                    label = definition.label,
                    score = score,
                    observedEvidence = positive,
                    adverseEvidence = adverse
                });
                total += score;
            }

            return new CrisisOrchestrationAssessmentReport
            {
                modelId = model.ModelId,
                modelSchemaVersion = model.SchemaVersion,
                averageScore = results.Count > 0 ? total / results.Count : 0f,
                competencies = results.ToArray()
            };
        }

        private static CrisisOrchestrationEvidenceId[] Matching(
            CrisisOrchestrationEvidenceId[] candidates,
            ISet<CrisisOrchestrationEvidenceId> observed)
        {
            if (candidates == null || candidates.Length == 0)
                return Array.Empty<CrisisOrchestrationEvidenceId>();
            var result = new List<CrisisOrchestrationEvidenceId>();
            for (int index = 0; index < candidates.Length; index++)
                if (observed.Contains(candidates[index])) result.Add(candidates[index]);
            return result.ToArray();
        }
    }
}
