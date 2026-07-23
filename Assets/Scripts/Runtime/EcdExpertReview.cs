using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    public sealed class EcdBlindEvidenceItem
    {
        public int sequence;
        public int beatIndex;
        public string actionSource = string.Empty;
        public string teacherUtterance = string.Empty;
        public bool semanticTextAvailable;
        public float studentValenceBefore;
        public float studentValenceAfter;
        public int focalStudentDwellMilliseconds;
        public int mutualGazeMilliseconds;
        public EyeTrackingSource gazeSource;
    }

    [Serializable]
    public sealed class EcdBlindCompetencyPrompt
    {
        public TeacherCompetency competency;
        public string label = string.Empty;
        public string description = string.Empty;
    }

    [Serializable]
    public sealed class EcdBlindReviewCase
    {
        public int schemaVersion = 1;
        public string caseId = string.Empty;
        public string scenarioId = string.Empty;
        public int attemptNumber = 1;
        public EcdBlindCompetencyPrompt[] competencyPrompts = Array.Empty<EcdBlindCompetencyPrompt>();
        public EcdBlindEvidenceItem[] evidence = Array.Empty<EcdBlindEvidenceItem>();
    }

    [Serializable]
    public sealed class EcdModelReferenceScore
    {
        public TeacherCompetency competency;
        public float score;
    }

    [Serializable]
    public sealed class EcdModelReference
    {
        public int schemaVersion = 1;
        public string caseId = string.Empty;
        public string modelId = string.Empty;
        public int modelSchemaVersion;
        public EcdModelReferenceScore[] scores = Array.Empty<EcdModelReferenceScore>();
    }

    [Serializable]
    public sealed class EcdExpertScore
    {
        public TeacherCompetency competency;
        public float score;
        public string evidenceNote = string.Empty;
    }

    [Serializable]
    public sealed class EcdExpertReviewSubmission
    {
        public int schemaVersion = 1;
        public string caseId = string.Empty;
        public string reviewerCode = string.Empty;
        public string submittedAtUtc = string.Empty;
        public EcdExpertScore[] scores = Array.Empty<EcdExpertScore>();
    }

    [Serializable]
    public sealed class EcdScoreAgreement
    {
        public TeacherCompetency competency;
        public float modelScore;
        public float expertScore;
        public float absoluteError;
        public bool withinHalfPoint;
    }

    [Serializable]
    public sealed class EcdReviewComparison
    {
        public string caseId = string.Empty;
        public int matchedCompetencies;
        public float meanAbsoluteError;
        public float withinHalfPointRate;
        public EcdScoreAgreement[] agreements = Array.Empty<EcdScoreAgreement>();
    }

    public static class EcdExpertReviewBuilder
    {
        public static EcdBlindReviewCase BuildBlindCase(
            string sessionId,
            string scenarioId,
            int attemptNumber,
            IReadOnlyList<TrainingTelemetryEvent> telemetry,
            EcdAssessmentModel model)
        {
            return BuildBlindCase(
                sessionId, scenarioId, attemptNumber, telemetry, model, null);
        }

        public static EcdBlindReviewCase BuildBlindCase(
            string sessionId,
            string scenarioId,
            int attemptNumber,
            IReadOnlyList<TrainingTelemetryEvent> telemetry,
            EcdAssessmentModel model,
            IReadOnlyDictionary<string, string> consentedRedactedUtterancesByActionId)
        {
            model = model != null ? model : EcdAssessmentModel.LoadDefault();
            var prompts = new List<EcdBlindCompetencyPrompt>();
            for (int index = 0; index < model.Competencies.Count; index++)
            {
                EcdCompetencyDefinition definition = model.Competencies[index];
                if (definition == null) continue;
                prompts.Add(new EcdBlindCompetencyPrompt
                {
                    competency = definition.competency,
                    label = definition.label,
                    description = definition.description
                });
            }

            var evidence = new List<EcdBlindEvidenceItem>();
            if (telemetry != null)
            {
                for (int index = 0; index < telemetry.Count; index++)
                {
                    TrainingTelemetryEvent item = telemetry[index];
                    if (item == null || item.kind != TrainingEventKind.TeacherAction) continue;
                    string utterance = string.Empty;
                    bool hasSemanticText =
                        consentedRedactedUtterancesByActionId != null &&
                        !string.IsNullOrWhiteSpace(item.actionId) &&
                        consentedRedactedUtterancesByActionId.TryGetValue(item.actionId, out utterance) &&
                        !string.IsNullOrWhiteSpace(utterance);
                    evidence.Add(new EcdBlindEvidenceItem
                    {
                        sequence = item.sequence,
                        beatIndex = item.beatIndex,
                        actionSource = item.actionSource.ToString(),
                        teacherUtterance = hasSemanticText
                            ? utterance.Trim()
                            : item.actionSource + " · " + Math.Max(0, item.teacherTextLength) + " chars · text not retained",
                        semanticTextAvailable = hasSemanticText,
                        studentValenceBefore = item.studentStateBefore?.affect.valence ?? 0f,
                        studentValenceAfter = item.studentStateAfter?.affect.valence ?? 0f,
                        focalStudentDwellMilliseconds = item.gaze?.focalStudentDwellMilliseconds ?? 0,
                        mutualGazeMilliseconds = item.gaze?.mutualGazeMilliseconds ?? 0,
                        gazeSource = item.gaze?.trackingSource ?? EyeTrackingSource.Unavailable
                    });
                }
            }

            return new EcdBlindReviewCase
            {
                caseId = CaseId(sessionId),
                scenarioId = scenarioId ?? string.Empty,
                attemptNumber = Math.Max(1, attemptNumber),
                competencyPrompts = prompts.ToArray(),
                evidence = evidence.ToArray()
            };
        }

        public static EcdModelReference BuildModelReference(
            EcdBlindReviewCase blindCase,
            ResearchDebriefReport report)
        {
            if (blindCase == null) throw new ArgumentNullException(nameof(blindCase));
            if (report == null) throw new ArgumentNullException(nameof(report));
            var scores = new EcdModelReferenceScore[report.competencies?.Length ?? 0];
            for (int index = 0; index < scores.Length; index++)
            {
                scores[index] = new EcdModelReferenceScore
                {
                    competency = report.competencies[index].competency,
                    score = report.competencies[index].score
                };
            }
            return new EcdModelReference
            {
                caseId = blindCase.caseId,
                modelId = report.modelId,
                modelSchemaVersion = report.modelSchemaVersion,
                scores = scores
            };
        }

        public static EcdReviewComparison Compare(
            EcdModelReference reference,
            EcdExpertReviewSubmission expert)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            if (expert == null) throw new ArgumentNullException(nameof(expert));
            if (string.IsNullOrWhiteSpace(reference.caseId) || reference.caseId != expert.caseId)
                throw new ArgumentException("Expert review and model reference must share a case ID.");

            var expertByCompetency = new Dictionary<TeacherCompetency, float>();
            if (expert.scores != null)
            {
                for (int index = 0; index < expert.scores.Length; index++)
                    expertByCompetency[expert.scores[index].competency] = expert.scores[index].score;
            }
            var agreements = new List<EcdScoreAgreement>();
            float errorSum = 0f;
            int within = 0;
            if (reference.scores != null)
            {
                for (int index = 0; index < reference.scores.Length; index++)
                {
                    EcdModelReferenceScore modelScore = reference.scores[index];
                    if (!expertByCompetency.TryGetValue(modelScore.competency, out float expertScore)) continue;
                    float error = Math.Abs(modelScore.score - expertScore);
                    bool withinHalf = error <= 0.5f;
                    errorSum += error;
                    if (withinHalf) within++;
                    agreements.Add(new EcdScoreAgreement
                    {
                        competency = modelScore.competency,
                        modelScore = modelScore.score,
                        expertScore = expertScore,
                        absoluteError = error,
                        withinHalfPoint = withinHalf
                    });
                }
            }
            int count = agreements.Count;
            return new EcdReviewComparison
            {
                caseId = reference.caseId,
                matchedCompetencies = count,
                meanAbsoluteError = count > 0 ? errorSum / count : 0f,
                withinHalfPointRate = count > 0 ? (float)within / count : 0f,
                agreements = agreements.ToArray()
            };
        }

        private static string CaseId(string sessionId)
        {
            byte[] source = Encoding.UTF8.GetBytes("ecd-review:" + (sessionId ?? string.Empty));
            using SHA256 sha = SHA256.Create();
            byte[] digest = sha.ComputeHash(source);
            var result = new StringBuilder("ECD-");
            for (int index = 0; index < 10; index++) result.Append(digest[index].ToString("x2"));
            return result.ToString();
        }
    }
}
