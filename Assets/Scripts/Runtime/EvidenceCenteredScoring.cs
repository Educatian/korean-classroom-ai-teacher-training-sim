using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    public sealed class CompetencyDimensionScore
    {
        public TeacherCompetency dimension;
        public float score;
        public int evidenceCount;
        public bool hasEvidence;
    }

    [Serializable]
    public sealed class EvidenceCenteredScoreSummary
    {
        public CompetencyDimensionScore[] dimensions;
        public float averageScore;
        public int evidenceCount;
    }

    public static class EvidenceCenteredScoring
    {
        public static EvidenceCenteredScoreSummary Evaluate(
            IReadOnlyList<TrainingTelemetryEvent> events)
        {
            TeacherCompetency[] values =
                (TeacherCompetency[])Enum.GetValues(typeof(TeacherCompetency));
            var sums = new float[values.Length];
            var counts = new int[values.Length];
            int totalEvidence = 0;

            if (events != null)
            {
                for (int eventIndex = 0; eventIndex < events.Count; eventIndex++)
                {
                    CompetencyEvidence[] evidence = events[eventIndex]?.competencyEvidence;
                    if (evidence == null)
                    {
                        continue;
                    }

                    for (int evidenceIndex = 0; evidenceIndex < evidence.Length; evidenceIndex++)
                    {
                        CompetencyEvidence item = evidence[evidenceIndex];
                        if (item == null)
                        {
                            continue;
                        }

                        int dimensionIndex = (int)item.dimension;
                        if (dimensionIndex < 0 || dimensionIndex >= values.Length)
                        {
                            continue;
                        }

                        sums[dimensionIndex] += Mathf.Clamp(item.score, 0f, 3f);
                        counts[dimensionIndex]++;
                        totalEvidence++;
                    }
                }
            }

            var dimensions = new CompetencyDimensionScore[values.Length];
            float evidencedDimensionSum = 0f;
            int evidencedDimensionCount = 0;
            for (int index = 0; index < values.Length; index++)
            {
                bool hasEvidence = counts[index] > 0;
                float score = hasEvidence ? sums[index] / counts[index] : 0f;
                dimensions[index] = new CompetencyDimensionScore
                {
                    dimension = values[index],
                    score = score,
                    evidenceCount = counts[index],
                    hasEvidence = hasEvidence
                };
                if (hasEvidence)
                {
                    evidencedDimensionSum += score;
                    evidencedDimensionCount++;
                }
            }

            return new EvidenceCenteredScoreSummary
            {
                dimensions = dimensions,
                averageScore = evidencedDimensionCount > 0
                    ? evidencedDimensionSum / evidencedDimensionCount
                    : 0f,
                evidenceCount = totalEvidence
            };
        }
    }
}
