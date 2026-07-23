using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    public sealed class ApprovedKnowledgeChunk
    {
        public string chunkId = string.Empty;
        public string locator = string.Empty;
        [TextArea(2, 8)] public string passage = string.Empty;
        public CrisisStage[] stages = Array.Empty<CrisisStage>();
        public TeacherCompetency[] competencies = Array.Empty<TeacherCompetency>();
    }

    [Serializable]
    public sealed class ApprovedKnowledgeSource
    {
        public string sourceId = string.Empty;
        public string title = string.Empty;
        public string version = string.Empty;
        public bool approvedForCoaching;
        public string approvedAtUtc = string.Empty;
        public ApprovedKnowledgeChunk[] chunks = Array.Empty<ApprovedKnowledgeChunk>();
    }

    [Serializable]
    public sealed class GroundedKnowledgeCitation
    {
        public string sourceId = string.Empty;
        public string sourceTitle = string.Empty;
        public string sourceVersion = string.Empty;
        public string chunkId = string.Empty;
        public string locator = string.Empty;
        public string passage = string.Empty;
        public float relevance;
    }

    [CreateAssetMenu(
        fileName = "ApprovedKnowledgeCatalog",
        menuName = "Teacher Training/Approved Knowledge Catalog",
        order = 45)]
    public sealed class ApprovedKnowledgeCatalog : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/Knowledge/ApprovedKnowledgeCatalog";
        [SerializeField] private int schemaVersion = 1;
        [SerializeField] private ApprovedKnowledgeSource[] sources = Array.Empty<ApprovedKnowledgeSource>();

        public int SchemaVersion => schemaVersion;
        public IReadOnlyList<ApprovedKnowledgeSource> Sources => sources;

        public static ApprovedKnowledgeCatalog LoadDefault()
        {
            return Resources.Load<ApprovedKnowledgeCatalog>(DefaultResourcePath);
        }

        public static ApprovedKnowledgeCatalog CreateRuntime(params ApprovedKnowledgeSource[] authoredSources)
        {
            ApprovedKnowledgeCatalog catalog = CreateInstance<ApprovedKnowledgeCatalog>();
            catalog.hideFlags = HideFlags.DontSave;
            catalog.sources = authoredSources ?? Array.Empty<ApprovedKnowledgeSource>();
            return catalog;
        }

        public GroundedKnowledgeCitation[] Retrieve(
            string query,
            CrisisStage stage,
            IReadOnlyList<TeacherCompetency> competencies,
            int maximumResults = 3)
        {
            var candidates = new List<GroundedKnowledgeCitation>();
            HashSet<string> queryTerms = Terms(query);
            int limit = Mathf.Clamp(maximumResults, 1, 8);
            for (int sourceIndex = 0; sourceIndex < sources.Length; sourceIndex++)
            {
                ApprovedKnowledgeSource source = sources[sourceIndex];
                if (source == null || !source.approvedForCoaching ||
                    string.IsNullOrWhiteSpace(source.sourceId) || source.chunks == null) continue;
                for (int chunkIndex = 0; chunkIndex < source.chunks.Length; chunkIndex++)
                {
                    ApprovedKnowledgeChunk chunk = source.chunks[chunkIndex];
                    if (chunk == null || string.IsNullOrWhiteSpace(chunk.passage)) continue;
                    float relevance = Relevance(chunk, queryTerms, stage, competencies);
                    if (relevance <= 0f) continue;
                    candidates.Add(new GroundedKnowledgeCitation
                    {
                        sourceId = source.sourceId,
                        sourceTitle = source.title,
                        sourceVersion = source.version,
                        chunkId = chunk.chunkId,
                        locator = chunk.locator,
                        passage = chunk.passage,
                        relevance = relevance
                    });
                }
            }
            candidates.Sort((left, right) => right.relevance.CompareTo(left.relevance));
            if (candidates.Count > limit) candidates.RemoveRange(limit, candidates.Count - limit);
            return candidates.ToArray();
        }

        private static float Relevance(
            ApprovedKnowledgeChunk chunk,
            HashSet<string> queryTerms,
            CrisisStage stage,
            IReadOnlyList<TeacherCompetency> competencies)
        {
            float score = Contains(chunk.stages, stage) ? 3f :
                chunk.stages == null || chunk.stages.Length == 0 ? 0.5f : 0f;
            if (competencies != null)
            {
                for (int index = 0; index < competencies.Count; index++)
                    if (Contains(chunk.competencies, competencies[index])) score += 2f;
            }
            HashSet<string> passageTerms = Terms(chunk.passage);
            foreach (string term in queryTerms) if (passageTerms.Contains(term)) score += 0.25f;
            return score;
        }

        private static bool Contains<T>(T[] values, T target) where T : struct
        {
            if (values == null) return false;
            for (int index = 0; index < values.Length; index++)
                if (EqualityComparer<T>.Default.Equals(values[index], target)) return true;
            return false;
        }

        private static HashSet<string> Terms(string text)
        {
            var normalized = new StringBuilder();
            string source = (text ?? string.Empty).ToLowerInvariant();
            for (int index = 0; index < source.Length; index++)
                normalized.Append(char.IsLetterOrDigit(source[index]) ? source[index] : ' ');
            var result = new HashSet<string>(StringComparer.Ordinal);
            string[] parts = normalized.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < parts.Length; index++)
                if (parts[index].Length >= 2) result.Add(parts[index]);
            return result;
        }
    }
}
