using System;
using System.Collections.Generic;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Counts training attempts per scene within one app run so retries are
    /// flagged in research exports and later attempts can reshuffle option
    /// order (a retry should measure judgement, not position memory).
    /// </summary>
    public static class TrainingAttemptTracker
    {
        private static readonly Dictionary<TrainingSceneId, int> counts =
            new Dictionary<TrainingSceneId, int>();

        public static int BeginAttempt(TrainingSceneId sceneId)
        {
            counts.TryGetValue(sceneId, out int previous);
            int attempt = previous + 1;
            counts[sceneId] = attempt;
            return attempt;
        }

        public static void Reset()
        {
            counts.Clear();
        }
    }

    /// <summary>
    /// Deterministic per-attempt option shuffle. Attempt 1 keeps the authored
    /// order (the pedagogical baseline); attempt 2+ reorders every beat's
    /// options with a seed derived from scene and attempt so the same retry
    /// always sees the same order (reproducible for research).
    /// </summary>
    public static class TeacherResponseOptionShuffler
    {
        public static void Shuffle(ScenarioBeat[] beats, int attemptNumber, TrainingSceneId sceneId)
        {
            if (beats == null || attemptNumber <= 1)
            {
                return;
            }

            var random = new Random(attemptNumber * 7919 + (int)sceneId * 104729);
            for (int beatIndex = 0; beatIndex < beats.Length; beatIndex++)
            {
                TeacherResponseOption[] options = beats[beatIndex]?.options;
                if (options == null || options.Length < 2)
                {
                    continue;
                }

                for (int index = options.Length - 1; index > 0; index--)
                {
                    int swap = random.Next(index + 1);
                    (options[index], options[swap]) = (options[swap], options[index]);
                }
            }
        }
    }
}
