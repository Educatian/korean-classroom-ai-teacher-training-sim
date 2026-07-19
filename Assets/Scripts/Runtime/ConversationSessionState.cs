using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public sealed class ConversationSessionState
    {
        private readonly int recentCapacity;
        private readonly Queue<ConversationTurn> recentTurns = new Queue<ConversationTurn>();
        private readonly Queue<string> durableTeacherCommitments = new Queue<string>();
        private const int DurableCommitmentCapacity = 4;

        public ConversationSessionState(int recentCapacity)
        {
            if (recentCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(recentCapacity));
            }

            this.recentCapacity = recentCapacity;
        }

        public int RecentTurnCount => recentTurns.Count;
        public int DurableCommitmentCount => durableTeacherCommitments.Count;
        public int TotalTurnCount { get; private set; }
        public float Trust { get; private set; } = 0.35f;
        public float DemandPressure { get; private set; } = 0.5f;
        public float Readiness { get; private set; } = 0.2f;
        public DialogueSignals LatestSignals { get; private set; } = DialogueSignals.Neutral;

        public void AddTurn(string teacher, string student, DialogueSignals signals)
        {
            if (!LlmContractValidator.TryAcceptSignals(signals, out DialogueSignals accepted))
            {
                accepted = DialogueSignals.Neutral;
            }

            recentTurns.Enqueue(new ConversationTurn(teacher, student));
            while (recentTurns.Count > recentCapacity)
            {
                recentTurns.Dequeue();
            }

            TotalTurnCount++;
            RememberCommitment(teacher);
            LatestSignals = accepted;
            Trust = Mathf.Clamp01(
                Trust + accepted.feltHeard * 0.18f + accepted.choiceOffered * 0.1f -
                accepted.perceivedPressure * 0.12f);
            DemandPressure = Mathf.Lerp(DemandPressure, accepted.perceivedPressure, 0.55f);
            Readiness = Mathf.Lerp(
                Readiness,
                Mathf.Max(accepted.readyForReentry, accepted.feltHeard * 0.75f),
                0.65f);
        }

        public string BuildPromptContext()
        {
            var builder = new StringBuilder();
            builder.Append("session_state: trust=").Append(Trust.ToString("F2"));
            builder.Append(", pressure=").Append(DemandPressure.ToString("F2"));
            builder.Append(", readiness=").AppendLine(Readiness.ToString("F2"));
            if (durableTeacherCommitments.Count > 0)
            {
                builder.AppendLine("durable_teacher_commitments:");
                foreach (string commitment in durableTeacherCommitments)
                {
                    builder.Append("- ").AppendLine(commitment);
                }
            }
            foreach (ConversationTurn turn in recentTurns)
            {
                builder.Append("teacher: ").AppendLine(turn.Teacher);
                builder.Append("student: ").AppendLine(turn.Student);
            }
            return builder.ToString().TrimEnd();
        }

        private void RememberCommitment(string teacher)
        {
            string value = teacher?.Trim();
            if (string.IsNullOrWhiteSpace(value) || !ContainsCommitmentMarker(value))
            {
                return;
            }

            if (value.Length > 120)
            {
                value = value.Substring(0, 120);
            }

            foreach (string existing in durableTeacherCommitments)
            {
                if (string.Equals(existing, value, StringComparison.Ordinal))
                {
                    return;
                }
            }

            durableTeacherCommitments.Enqueue(value);
            while (durableTeacherCommitments.Count > DurableCommitmentCapacity)
            {
                durableTeacherCommitments.Dequeue();
            }
        }

        private static bool ContainsCommitmentMarker(string value)
        {
            string normalized = value.ToLowerInvariant();
            return normalized.Contains("약속") || normalized.Contains("기다") ||
                   normalized.Contains("함께") || normalized.Contains("도와") ||
                   normalized.Contains("선택") || normalized.Contains("promise") ||
                   normalized.Contains("wait") || normalized.Contains("together") ||
                   normalized.Contains("help") || normalized.Contains("choice");
        }

        private readonly struct ConversationTurn
        {
            public ConversationTurn(string teacher, string student)
            {
                Teacher = teacher ?? string.Empty;
                Student = student ?? string.Empty;
            }

            public string Teacher { get; }
            public string Student { get; }
        }
    }
}
