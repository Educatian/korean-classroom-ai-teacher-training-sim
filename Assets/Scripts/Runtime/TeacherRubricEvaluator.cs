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
                float emphasis = i % 2 == 0 ? 0.08f : -0.05f;
                dimensions[i] = new RubricDimension
                {
                    label = Labels[i],
                    score = Mathf.Clamp(average + emphasis, 0f, 3f)
                };
            }

            return new RubricSummary
            {
                dimensions = dimensions,
                averageScore = average,
                overallLevel = average >= 2.5f ? "숙련" : average >= 1.7f ? "성장 중" : "기초 연습"
            };
        }
    }
}
