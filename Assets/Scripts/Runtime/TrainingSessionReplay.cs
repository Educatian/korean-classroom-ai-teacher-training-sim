using System;
using System.Collections.Generic;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    public sealed class TrainingSessionReplayState
    {
        public string sessionId;
        public int nextSequence;
        public StudentStateSnapshot studentState;
    }

    public static class TrainingSessionReplay
    {
        public static TrainingSessionReplayState Start(
            string sessionId,
            StudentStateSnapshot initialStudentState)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException("A replay session requires an identifier.", nameof(sessionId));
            }

            return new TrainingSessionReplayState
            {
                sessionId = sessionId,
                nextSequence = 0,
                studentState = CopyStudentState(initialStudentState)
            };
        }

        public static TrainingSessionReplayState Reduce(
            TrainingSessionReplayState state,
            TrainingTelemetryEvent trainingEvent)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (trainingEvent == null)
            {
                throw new ArgumentNullException(nameof(trainingEvent));
            }

            if (trainingEvent.schemaVersion != TrainingTelemetryEvent.CurrentSchemaVersion)
            {
                throw new InvalidOperationException(
                    $"Unsupported telemetry schema version {trainingEvent.schemaVersion}.");
            }

            if (!string.Equals(state.sessionId, trainingEvent.sessionId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The event belongs to a different session.");
            }

            if (trainingEvent.sequence != state.nextSequence)
            {
                throw new InvalidOperationException(
                    $"Expected sequence {state.nextSequence}, received {trainingEvent.sequence}.");
            }

            if (trainingEvent.studentStateAfter == null)
            {
                throw new InvalidOperationException("A replayable event requires a post-event student state.");
            }

            return new TrainingSessionReplayState
            {
                sessionId = state.sessionId,
                nextSequence = state.nextSequence + 1,
                studentState = CopyStudentState(trainingEvent.studentStateAfter)
            };
        }

        public static TrainingSessionReplayState Replay(
            TrainingSessionReplayState initial,
            IReadOnlyList<TrainingTelemetryEvent> events)
        {
            if (initial == null)
            {
                throw new ArgumentNullException(nameof(initial));
            }

            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            var current = new TrainingSessionReplayState
            {
                sessionId = initial.sessionId,
                nextSequence = initial.nextSequence,
                studentState = CopyStudentState(initial.studentState)
            };

            for (int index = 0; index < events.Count; index++)
            {
                current = Reduce(current, events[index]);
            }

            return current;
        }

        private static StudentStateSnapshot CopyStudentState(StudentStateSnapshot source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new StudentStateSnapshot
            {
                affect = source.affect,
                gesture = source.gesture,
                gestureIntensity = source.gestureIntensity,
                gazeContact = source.gazeContact,
                engagement = source.engagement,
                trust = source.trust
            };
        }
    }
}
