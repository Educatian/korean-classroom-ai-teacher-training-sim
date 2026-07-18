using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    public sealed class RubricDimension
    {
        public string label;
        public float score;
    }

    [Serializable]
    public sealed class RubricSummary
    {
        public RubricDimension[] dimensions;
        public float averageScore;
        public string overallLevel;
    }

    public static class TeacherRubricEvaluator
    {
        private static readonly string[] Labels =
        {
            "학생 존엄", "낮은 자극", "감정 인정", "선택권", "안전", "수업 복귀"
        };

        public static RubricSummary Evaluate(IReadOnlyList<TeacherResponseOption> choices)
        {
            float average = 0f;
            if (choices != null && choices.Count > 0)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    average += Mathf.Clamp(choices[i].quality, 0, 3);
                }

                average /= choices.Count;
            }

            var dimensions = new RubricDimension[Labels.Length];
            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = new RubricDimension
                {
                    label = Labels[i],
                    score = average
                };
            }

            return new RubricSummary
            {
                dimensions = dimensions,
                averageScore = average,
                overallLevel = average >= 2.5f ? "숙련" : average >= 1.7f ? "성장 중" : "기초 연습"
            };
        }

        public static RubricSummary Evaluate(IReadOnlyList<TrainingTelemetryEvent> events)
        {
            EvidenceCenteredScoreSummary evidence = EvidenceCenteredScoring.Evaluate(events);
            var dimensions = new RubricDimension[Labels.Length];
            for (int index = 0; index < dimensions.Length; index++)
            {
                CompetencyDimensionScore score = evidence.dimensions[index];
                dimensions[index] = new RubricDimension
                {
                    label = Labels[index],
                    score = score.hasEvidence ? score.score : 0f
                };
            }

            return new RubricSummary
            {
                dimensions = dimensions,
                averageScore = evidence.averageScore,
                overallLevel = OverallLevel(evidence.averageScore)
            };
        }

        private static string OverallLevel(float average)
        {
            return average >= 2.5f
                ? Korean('\uC219', '\uB828')
                : average >= 1.7f
                    ? Korean('\uC131', '\uC7A5', ' ', '\uC911')
                    : Korean('\uAE30', '\uCD08', ' ', '\uC5F0', '\uC2B5');
        }

        private static string Korean(params char[] characters)
        {
            return new string(characters);
        }
    }
}
