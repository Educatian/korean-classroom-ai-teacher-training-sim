using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    public sealed class DialogueSignals
    {
        [Range(0f, 1f)] public float feltHeard;
        [Range(0f, 1f)] public float perceivedPressure = 0.5f;
        [Range(0f, 1f)] public float choiceOffered;
        [Range(0f, 1f)] public float safetyConcern;
        [Range(0f, 1f)] public float readyForReentry;

        public static DialogueSignals Neutral => new DialogueSignals();

        public DialogueSignals Copy()
        {
            return new DialogueSignals
            {
                feltHeard = feltHeard,
                perceivedPressure = perceivedPressure,
                choiceOffered = choiceOffered,
                safetyConcern = safetyConcern,
                readyForReentry = readyForReentry
            };
        }
    }

    [Serializable]
    public sealed class StudentTurnRequest
    {
        public string teacherUtterance;
        public string conversationContext;
        public AffectVector currentAffect;
    }

    [Serializable]
    public sealed class RubricDimensionScore
    {
        public TeacherCompetency dimension;
        [Range(0f, 3f)] public float score;
        public string evidence;
    }

    [Serializable]
    public sealed class TeacherRubricResult
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        [Range(0f, 1f)] public float confidence;
        public RubricDimensionScore[] dimensions = Array.Empty<RubricDimensionScore>();
        public string improvementSuggestion;

        public CompetencyEvidence[] ToEvidence()
        {
            var evidence = new CompetencyEvidence[dimensions.Length];
            for (int index = 0; index < dimensions.Length; index++)
            {
                RubricDimensionScore source = dimensions[index];
                evidence[index] = new CompetencyEvidence
                {
                    evidenceId = source.dimension.ToString(),
                    dimension = source.dimension,
                    score = source.score
                };
            }
            return evidence;
        }
    }

    [Serializable]
    public sealed class TeacherRubricRequest
    {
        public string teacherUtterance;
        public string studentReply;
        public string scenarioContext;
    }

    public interface ILlmGateway
    {
        bool IsConfigured { get; }
        string ConfigurationLabel { get; }
        string ModelId { get; }
        int PromptVersion { get; }

        IEnumerator RequestStudentTurn(
            StudentTurnRequest request,
            Action<StudentAgentTurn> completed,
            Action<string> failed);

        IEnumerator RequestTeacherRubric(
            TeacherRubricRequest request,
            Action<TeacherRubricResult> completed,
            Action<string> failed);
    }

    public static class LlmContractValidator
    {
        public static bool TryAcceptSignals(DialogueSignals source, out DialogueSignals accepted)
        {
            accepted = null;
            if (source == null ||
                !InUnitRange(source.feltHeard) ||
                !InUnitRange(source.perceivedPressure) ||
                !InUnitRange(source.choiceOffered) ||
                !InUnitRange(source.safetyConcern) ||
                !InUnitRange(source.readyForReentry))
            {
                return false;
            }

            accepted = source.Copy();
            return true;
        }

        public static bool TryAcceptRubric(TeacherRubricResult source, out TeacherRubricResult accepted)
        {
            accepted = null;
            if (source == null ||
                source.schemaVersion != TeacherRubricResult.CurrentSchemaVersion ||
                !InUnitRange(source.confidence) ||
                source.dimensions == null ||
                source.dimensions.Length != Enum.GetValues(typeof(TeacherCompetency)).Length)
            {
                return false;
            }

            var seen = new HashSet<TeacherCompetency>();
            foreach (RubricDimensionScore dimension in source.dimensions)
            {
                if (dimension == null ||
                    float.IsNaN(dimension.score) ||
                    float.IsInfinity(dimension.score) ||
                    dimension.score < 0f ||
                    dimension.score > 3f ||
                    string.IsNullOrWhiteSpace(dimension.evidence) ||
                    !seen.Add(dimension.dimension))
                {
                    return false;
                }
            }

            accepted = source;
            return true;
        }

        private static bool InUnitRange(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
        }
    }
}
