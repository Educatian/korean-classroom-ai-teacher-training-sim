using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    public sealed class EcdObservableBehavior
    {
        public string id;
        public string label;
        [TextArea] public string description;
        public string evidenceIdContains;
        [Range(0f, 3f)] public float expectedScore = 2f;
        [Min(0f)] public float weight = 1f;
        [TextArea] public string missedSignalMessage;
    }

    [Serializable]
    public sealed class EcdCompetencyDefinition
    {
        public TeacherCompetency competency;
        public string label;
        [TextArea] public string description;
        [Min(0f)] public float weight = 1f;
        public EcdObservableBehavior[] observableBehaviors = Array.Empty<EcdObservableBehavior>();
    }

    [Serializable]
    public sealed class EcdScoreBand
    {
        public string label;
        [Range(0f, 3f)] public float minimumScore;
        [TextArea] public string interpretation;
    }

    [CreateAssetMenu(
        fileName = "TeacherResponseEcdModel",
        menuName = "Teacher Training/ECD Assessment Model",
        order = 30)]
    public sealed class EcdAssessmentModel : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/ECD/TeacherResponseEcdModel";

        [SerializeField] private string modelId = "teacher-response-ecd-v1";
        [SerializeField] private int schemaVersion = 1;
        [SerializeField] private EcdCompetencyDefinition[] competencies = Array.Empty<EcdCompetencyDefinition>();
        [SerializeField] private EcdScoreBand[] scoreBands = Array.Empty<EcdScoreBand>();

        public string ModelId => modelId;
        public int SchemaVersion => schemaVersion;
        public IReadOnlyList<EcdCompetencyDefinition> Competencies => competencies;
        public IReadOnlyList<EcdScoreBand> ScoreBands => scoreBands;

        public static EcdAssessmentModel LoadDefault()
        {
            EcdAssessmentModel model = Resources.Load<EcdAssessmentModel>(DefaultResourcePath);
            return model != null ? model : CreateRuntimeDefault();
        }

        public static EcdAssessmentModel CreateRuntimeDefault()
        {
            EcdAssessmentModel model = CreateInstance<EcdAssessmentModel>();
            model.hideFlags = HideFlags.DontSave;
            model.PopulateDefaults();
            return model;
        }

        public void PopulateDefaults()
        {
            competencies = new[]
            {
                Definition(TeacherCompetency.StudentDignity, "학생 존엄", "공개적 대치와 낙인을 피하고 학생의 체면과 권리를 보호한다.", "존중 언어", "StudentDignity", "압박 없이 학생의 존엄을 확인하는 언어 근거가 부족했습니다."),
                Definition(TeacherCompetency.LowStimulusResponse, "낮은 자극", "목소리와 요구 강도를 낮추고 처리 시간을 제공한다.", "처리 시간 제공", "LowStimulusResponse", "각성이 높을 때 요구 강도를 낮추거나 기다리는 개입이 필요했습니다."),
                Definition(TeacherCompetency.EmotionAcknowledgement, "감정 인정", "행동을 단정하지 않고 관찰된 정서와 어려움을 반영한다.", "정서 반영", "EmotionAcknowledgement", "학생의 정서 신호를 말로 확인한 근거가 부족했습니다."),
                Definition(TeacherCompetency.StudentAgency, "선택권", "안전한 범위 안에서 학생이 다음 행동을 선택하도록 돕는다.", "선택권 제공", "StudentAgency", "학생이 선택할 수 있는 구체적인 대안을 제시하지 않았습니다."),
                Definition(TeacherCompetency.Safety, "안전", "위험 신호와 주변 학생의 안전을 확인하며 위협을 높이지 않는다.", "안전 확인", "Safety", "위기 단계에서 학생과 주변의 안전을 확인한 근거가 부족했습니다."),
                Definition(TeacherCompetency.InstructionalReentry, "수업 복귀", "진정 이후 작고 실행 가능한 재참여 단계를 함께 정한다.", "단계적 복귀", "InstructionalReentry", "진정 이후 수업으로 복귀하는 작은 다음 단계를 연결하지 못했습니다.")
            };
            scoreBands = new[]
            {
                new EcdScoreBand { label = "기초 연습", minimumScore = 0f, interpretation = "핵심 신호를 발견하고 대응 문장을 다시 구성합니다." },
                new EcdScoreBand { label = "성장 중", minimumScore = 1.7f, interpretation = "일부 근거가 확인되며 상황에 맞는 일관성을 높입니다." },
                new EcdScoreBand { label = "숙련", minimumScore = 2.5f, interpretation = "관찰 근거에 맞춰 정서 지원과 수업 복귀를 연결합니다." }
            };
        }

        public EcdCompetencyDefinition Find(TeacherCompetency competency)
        {
            for (int index = 0; index < competencies.Length; index++)
            {
                if (competencies[index] != null && competencies[index].competency == competency)
                {
                    return competencies[index];
                }
            }

            return null;
        }

        public string BandFor(float score)
        {
            string label = scoreBands.Length > 0 ? scoreBands[0].label : string.Empty;
            float threshold = float.MinValue;
            for (int index = 0; index < scoreBands.Length; index++)
            {
                EcdScoreBand band = scoreBands[index];
                if (band != null && score >= band.minimumScore && band.minimumScore >= threshold)
                {
                    threshold = band.minimumScore;
                    label = band.label;
                }
            }

            return label;
        }

        private static EcdCompetencyDefinition Definition(
            TeacherCompetency competency,
            string label,
            string description,
            string observableLabel,
            string evidenceId,
            string missed)
        {
            return new EcdCompetencyDefinition
            {
                competency = competency,
                label = label,
                description = description,
                weight = 1f,
                observableBehaviors = new[]
                {
                    new EcdObservableBehavior
                    {
                        id = evidenceId,
                        label = observableLabel,
                        description = description,
                        evidenceIdContains = evidenceId,
                        expectedScore = 2f,
                        weight = 1f,
                        missedSignalMessage = missed
                    }
                }
            };
        }
    }

    [Serializable]
    public sealed class EcdEvidenceTrace
    {
        public string evidenceId;
        public string observableId;
        public TeacherCompetency competency;
        public float score;
        public int eventSequence;
        public int beatIndex;
        public string source;
    }

    [Serializable]
    public sealed class EcdCompetencyResult
    {
        public TeacherCompetency competency;
        public string label;
        public float score;
        public float weight;
        public int evidenceCount;
        public EcdEvidenceTrace[] evidence = Array.Empty<EcdEvidenceTrace>();
    }

    [Serializable]
    public sealed class AffectTrendPoint
    {
        public int sequence;
        public int beatIndex;
        public float valence;
        public float arousal;
        public float dominance;
    }

    [Serializable]
    public sealed class InterventionTimelineItem
    {
        public int sequence;
        public int beatIndex;
        public string actionSource;
        public float valenceBefore;
        public float valenceAfter;
        public string evidenceSummary;
        public string teacherUtteranceSummary;
        public string recommendedUtterance;
        public string evaluationRationale;
    }

    [Serializable]
    public sealed class MissedSignal
    {
        public TeacherCompetency competency;
        public string label;
        public string message;
        public int beatIndex;
    }

    [Serializable]
    public sealed class ResearchDebriefReport
    {
        public string modelId;
        public int modelSchemaVersion;
        public string sessionId;
        public string overallLevel;
        public float averageScore;
        public EcdCompetencyResult[] competencies = Array.Empty<EcdCompetencyResult>();
        public AffectTrendPoint[] affectTrend = Array.Empty<AffectTrendPoint>();
        public InterventionTimelineItem[] interventionTimeline = Array.Empty<InterventionTimelineItem>();
        public MissedSignal[] missedSignals = Array.Empty<MissedSignal>();

        public RubricSummary ToRubricSummary()
        {
            var dimensions = new RubricDimension[competencies.Length];
            for (int index = 0; index < competencies.Length; index++)
            {
                dimensions[index] = new RubricDimension
                {
                    label = competencies[index].label,
                    score = competencies[index].score
                };
            }

            return new RubricSummary
            {
                dimensions = dimensions,
                averageScore = averageScore,
                overallLevel = overallLevel
            };
        }
    }

    public static class EcdAssessmentEngine
    {
        public static ResearchDebriefReport Evaluate(
            IReadOnlyList<TrainingTelemetryEvent> events,
            EcdAssessmentModel model)
        {
            model = model != null ? model : EcdAssessmentModel.LoadDefault();
            var affect = new List<AffectTrendPoint>();
            var timeline = new List<InterventionTimelineItem>();
            Dictionary<string, string> coachSuggestions = CollectCoachSuggestions(events);
            string sessionId = string.Empty;
            if (events != null)
            {
                for (int index = 0; index < events.Count; index++)
                {
                    TrainingTelemetryEvent item = events[index];
                    if (item == null)
                    {
                        continue;
                    }

                    sessionId = string.IsNullOrEmpty(sessionId) ? item.sessionId : sessionId;
                    StudentStateSnapshot state = item.studentStateAfter;
                    if (state != null)
                    {
                        affect.Add(new AffectTrendPoint
                        {
                            sequence = item.sequence,
                            beatIndex = item.beatIndex,
                            valence = state.affect.valence,
                            arousal = state.affect.arousal,
                            dominance = state.affect.dominance
                        });
                    }

                    if (item.kind == TrainingEventKind.TeacherAction)
                    {
                        timeline.Add(new InterventionTimelineItem
                        {
                            sequence = item.sequence,
                            beatIndex = item.beatIndex,
                            actionSource = item.actionSource.ToString(),
                            valenceBefore = item.studentStateBefore?.affect.valence ?? 0f,
                            valenceAfter = item.studentStateAfter?.affect.valence ?? 0f,
                            evidenceSummary = EvidenceSummary(item.competencyEvidence),
                            teacherUtteranceSummary = TeacherUtteranceSummary(item),
                            recommendedUtterance = RecommendedUtterance(item, coachSuggestions),
                            evaluationRationale = EvaluationRationale(item.competencyEvidence)
                        });
                    }
                }
            }

            var competencyResults = new List<EcdCompetencyResult>();
            var missed = new List<MissedSignal>();
            float weightedSum = 0f;
            float totalWeight = 0f;
            for (int definitionIndex = 0; definitionIndex < model.Competencies.Count; definitionIndex++)
            {
                EcdCompetencyDefinition definition = model.Competencies[definitionIndex];
                if (definition == null)
                {
                    continue;
                }

                var traces = CollectEvidence(events, definition);
                float score = 0f;
                for (int index = 0; index < traces.Count; index++)
                {
                    score += traces[index].score;
                }

                score = traces.Count > 0 ? score / traces.Count : 0f;
                float weight = Mathf.Max(0f, definition.weight);
                competencyResults.Add(new EcdCompetencyResult
                {
                    competency = definition.competency,
                    label = definition.label,
                    score = score,
                    weight = weight,
                    evidenceCount = traces.Count,
                    evidence = traces.ToArray()
                });
                weightedSum += score * weight;
                totalWeight += weight;
                EcdObservableBehavior observable = definition.observableBehaviors != null && definition.observableBehaviors.Length > 0
                    ? definition.observableBehaviors[0]
                    : null;
                float expected = observable?.expectedScore ?? 2f;
                if (traces.Count == 0 || score < expected)
                {
                    missed.Add(new MissedSignal
                    {
                        competency = definition.competency,
                        label = definition.label,
                        message = observable?.missedSignalMessage ?? definition.description,
                        beatIndex = FindHighestArousalBeat(events)
                    });
                }
            }

            float average = totalWeight > 0f ? weightedSum / totalWeight : 0f;
            return new ResearchDebriefReport
            {
                modelId = model.ModelId,
                modelSchemaVersion = model.SchemaVersion,
                sessionId = sessionId,
                overallLevel = model.BandFor(average),
                averageScore = average,
                competencies = competencyResults.ToArray(),
                affectTrend = affect.ToArray(),
                interventionTimeline = timeline.ToArray(),
                missedSignals = missed.ToArray()
            };
        }

        private static List<EcdEvidenceTrace> CollectEvidence(
            IReadOnlyList<TrainingTelemetryEvent> events,
            EcdCompetencyDefinition definition)
        {
            var result = new List<EcdEvidenceTrace>();
            if (events == null)
            {
                return result;
            }

            for (int eventIndex = 0; eventIndex < events.Count; eventIndex++)
            {
                TrainingTelemetryEvent item = events[eventIndex];
                CompetencyEvidence[] evidence = item?.competencyEvidence;
                if (evidence == null)
                {
                    continue;
                }

                for (int evidenceIndex = 0; evidenceIndex < evidence.Length; evidenceIndex++)
                {
                    CompetencyEvidence entry = evidence[evidenceIndex];
                    if (entry == null || entry.dimension != definition.competency)
                    {
                        continue;
                    }

                    result.Add(new EcdEvidenceTrace
                    {
                        evidenceId = entry.evidenceId,
                        observableId = ResolveObservableId(definition, entry),
                        competency = entry.dimension,
                        score = Mathf.Clamp(entry.score, 0f, 3f),
                        eventSequence = item.sequence,
                        beatIndex = item.beatIndex,
                        source = item.actionSource.ToString()
                    });
                }
            }

            return result;
        }

        private static string ResolveObservableId(
            EcdCompetencyDefinition definition,
            CompetencyEvidence evidence)
        {
            if (!string.IsNullOrWhiteSpace(evidence.observableId))
            {
                return evidence.observableId;
            }

            if (definition.observableBehaviors == null)
            {
                return string.Empty;
            }

            for (int index = 0; index < definition.observableBehaviors.Length; index++)
            {
                EcdObservableBehavior observable = definition.observableBehaviors[index];
                if (observable != null &&
                    !string.IsNullOrWhiteSpace(observable.evidenceIdContains) &&
                    !string.IsNullOrWhiteSpace(evidence.evidenceId) &&
                    evidence.evidenceId.IndexOf(observable.evidenceIdContains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return observable.id;
                }
            }

            return definition.observableBehaviors.Length > 0
                ? definition.observableBehaviors[0].id
                : string.Empty;
        }

        private static Dictionary<string, string> CollectCoachSuggestions(
            IReadOnlyList<TrainingTelemetryEvent> events)
        {
            var result = new Dictionary<string, string>();
            if (events == null)
            {
                return result;
            }

            for (int index = 0; index < events.Count; index++)
            {
                TrainingTelemetryEvent item = events[index];
                if (item == null ||
                    item.kind != TrainingEventKind.RubricEvaluation ||
                    string.IsNullOrWhiteSpace(item.actionId) ||
                    string.IsNullOrWhiteSpace(item.coachSuggestion))
                {
                    continue;
                }

                result[item.actionId] = item.coachSuggestion.Trim();
            }

            return result;
        }

        private static string TeacherUtteranceSummary(TrainingTelemetryEvent item)
        {
            CompetencyEvidence strongest = ExtremeEvidence(item.competencyEvidence, true);
            string source = item.actionSource == TrainingActionSource.TeacherUtterance
                ? "자유 발화"
                : item.actionSource == TrainingActionSource.TeacherChoice
                    ? "선택형 응답"
                    : "교사 개입";
            if (strongest == null)
            {
                return $"{source} · 대응 원칙을 확인할 근거가 부족함";
            }

            string length = item.teacherTextLength > 0 ? $" · {item.teacherTextLength}자" : string.Empty;
            return $"{source} · {CompetencyLabel(strongest.dimension)} 신호 중심{length}";
        }

        private static string RecommendedUtterance(
            TrainingTelemetryEvent item,
            IReadOnlyDictionary<string, string> coachSuggestions)
        {
            if (!string.IsNullOrWhiteSpace(item.actionId) &&
                coachSuggestions.TryGetValue(item.actionId, out string suggestion) &&
                !string.IsNullOrWhiteSpace(suggestion))
            {
                return suggestion;
            }

            CompetencyEvidence weakest = ExtremeEvidence(item.competencyEvidence, false);
            return RecommendedFor(weakest?.dimension ?? TeacherCompetency.EmotionAcknowledgement);
        }

        private static string EvaluationRationale(CompetencyEvidence[] evidence)
        {
            CompetencyEvidence strongest = ExtremeEvidence(evidence, true);
            CompetencyEvidence weakest = ExtremeEvidence(evidence, false);
            if (strongest == null)
            {
                return "평가 근거가 수집되지 않았습니다.";
            }

            if (weakest == null || ReferenceEquals(strongest, weakest))
            {
                return $"관찰 근거 · {CompetencyLabel(strongest.dimension)} {strongest.score:0.0}";
            }

            return $"강점 {CompetencyLabel(strongest.dimension)} {strongest.score:0.0} · 보완 {CompetencyLabel(weakest.dimension)} {weakest.score:0.0}";
        }

        private static CompetencyEvidence ExtremeEvidence(CompetencyEvidence[] evidence, bool highest)
        {
            CompetencyEvidence result = null;
            if (evidence == null)
            {
                return result;
            }

            for (int index = 0; index < evidence.Length; index++)
            {
                CompetencyEvidence candidate = evidence[index];
                if (candidate == null)
                {
                    continue;
                }

                if (result == null || (highest ? candidate.score > result.score : candidate.score < result.score))
                {
                    result = candidate;
                }
            }

            return result;
        }

        private static string RecommendedFor(TeacherCompetency competency)
        {
            return competency switch
            {
                TeacherCompetency.StudentDignity => "지금 힘든 마음을 존중할게. 준비되면 이야기해도 괜찮아.",
                TeacherCompetency.LowStimulusResponse => "지금은 잠깐 쉬자. 천천히 숨을 고른 뒤 이야기해도 괜찮아.",
                TeacherCompetency.EmotionAcknowledgement => "많이 답답하고 힘들어 보이는구나. 내가 여기서 기다릴게.",
                TeacherCompetency.StudentAgency => "잠깐 쉴지, 조용한 곳에서 이야기할지 네가 선택해도 좋아.",
                TeacherCompetency.Safety => "먼저 다치지 않도록 안전한 거리를 두고 필요한 도움을 함께 찾자.",
                TeacherCompetency.InstructionalReentry => "준비되면 쉬운 것 하나부터 나와 같이 다시 시작해 보자.",
                _ => "지금 어떤 도움이 필요한지 천천히 말해 줘도 괜찮아."
            };
        }

        private static string CompetencyLabel(TeacherCompetency competency)
        {
            return competency switch
            {
                TeacherCompetency.StudentDignity => "학생 존엄",
                TeacherCompetency.LowStimulusResponse => "낮은 자극",
                TeacherCompetency.EmotionAcknowledgement => "감정 인정",
                TeacherCompetency.StudentAgency => "선택권",
                TeacherCompetency.Safety => "안전",
                TeacherCompetency.InstructionalReentry => "수업 복귀",
                _ => competency.ToString()
            };
        }
        private static string EvidenceSummary(CompetencyEvidence[] evidence)
        {
            if (evidence == null || evidence.Length == 0)
            {
                return "근거 없음";
            }

            var labels = new List<string>();
            for (int index = 0; index < evidence.Length; index++)
            {
                if (evidence[index] != null)
                {
                    labels.Add($"{evidence[index].dimension} {evidence[index].score:0.0}");
                }
            }

            return string.Join(" · ", labels);
        }

        private static int FindHighestArousalBeat(IReadOnlyList<TrainingTelemetryEvent> events)
        {
            int beat = 0;
            float highest = float.MinValue;
            if (events == null)
            {
                return beat;
            }

            for (int index = 0; index < events.Count; index++)
            {
                TrainingTelemetryEvent item = events[index];
                float arousal = item?.studentStateBefore?.affect.arousal ?? float.MinValue;
                if (arousal > highest)
                {
                    highest = arousal;
                    beat = item.beatIndex;
                }
            }

            return beat;
        }
    }
}
