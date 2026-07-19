using System;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    internal static class LlmResponseParser
    {
        [Serializable]
        private sealed class ChatMessage
        {
            public string content;
        }

        [Serializable]
        private sealed class ChatChoice
        {
            public ChatMessage message;
        }

        [Serializable]
        private sealed class ChatResponse
        {
            public ChatChoice[] choices;
        }

        public static bool TryExtractContent(string raw, out string content)
        {
            content = null;
            try
            {
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(raw);
                if (response?.choices == null || response.choices.Length == 0 ||
                    response.choices[0]?.message == null)
                {
                    return false;
                }

                content = response.choices[0].message.content;
                return !string.IsNullOrWhiteSpace(content);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static StudentAgentTurn ParseStudentTurn(string raw)
        {
            if (!TryExtractObject(raw, out string json))
            {
                return null;
            }

            try
            {
                var turn = new StudentAgentTurn
                {
                    valence = float.NaN,
                    arousal = float.NaN,
                    dominance = float.NaN
                };
                JsonUtility.FromJsonOverwrite(json, turn);
                if (string.IsNullOrWhiteSpace(turn.studentReply) || string.IsNullOrWhiteSpace(turn.gesture) ||
                    !Finite(turn.valence) || !Finite(turn.arousal) || !Finite(turn.dominance) ||
                    !Enum.TryParse(turn.gesture, true, out BehaviorGesture _))
                {
                    return null;
                }

                if (turn.dialogueSignals == null)
                {
                    turn.dialogueSignals = DialogueSignals.Neutral;
                }
                else if (!LlmContractValidator.TryAcceptSignals(turn.dialogueSignals, out DialogueSignals accepted))
                {
                    return null;
                }
                else
                {
                    turn.dialogueSignals = accepted;
                }

                return turn;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static TeacherRubricResult ParseTeacherRubric(string raw)
        {
            if (!TryExtractObject(raw, out string json))
            {
                return null;
            }

            try
            {
                TeacherRubricResult result = JsonUtility.FromJson<TeacherRubricResult>(json);
                return LlmContractValidator.TryAcceptRubric(result, out TeacherRubricResult accepted)
                    ? accepted
                    : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static bool TryExtractObject(string raw, out string json)
        {
            json = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return false;
            }

            json = raw.Substring(start, end - start + 1);
            return true;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
