using System;
using System.Globalization;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public sealed class OpenRouterRuntimeConfiguration
    {
        private const int OPENROUTER_MAX_TOKENS = 0;
        private const int OPENROUTER_TEMPERATURE = 0;
        private const int OPENROUTER_PROMPT_VERSION = 0;

        public const string MaxTokensEnvironmentVariable = nameof(OPENROUTER_MAX_TOKENS);
        public const string TemperatureEnvironmentVariable = nameof(OPENROUTER_TEMPERATURE);
        public const string PromptVersionEnvironmentVariable = nameof(OPENROUTER_PROMPT_VERSION);

        public const int DefaultMaxTokens = 320;
        public const float DefaultTemperature = 0.25f;
        public const int DefaultPromptVersion = 1;

        public OpenRouterRuntimeConfiguration(int maxTokens, float temperature, int promptVersion)
        {
            MaxTokens = Mathf.Clamp(maxTokens, 16, 4096);
            Temperature = Mathf.Clamp(temperature, 0f, 2f);
            PromptVersion = Math.Max(1, promptVersion);
        }

        public int MaxTokens { get; }
        public float Temperature { get; }
        public int PromptVersion { get; }

        public static OpenRouterRuntimeConfiguration FromEnvironment()
        {
            return FromEnvironment(Environment.GetEnvironmentVariable);
        }

        public static OpenRouterRuntimeConfiguration FromEnvironment(Func<string, string> read)
        {
            if (read == null)
            {
                throw new ArgumentNullException(nameof(read));
            }

            int maxTokens = ParseInt(read(MaxTokensEnvironmentVariable), DefaultMaxTokens);
            float temperature = ParseFloat(read(TemperatureEnvironmentVariable), DefaultTemperature);
            int promptVersion = ParseInt(read(PromptVersionEnvironmentVariable), DefaultPromptVersion);
            return new OpenRouterRuntimeConfiguration(maxTokens, temperature, promptVersion);
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
        }

        private static float ParseFloat(string value, float fallback)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                ? parsed
                : fallback;
        }
    }

    public enum StudentSafetyCategory
    {
        None = 0,
        Privacy = 1,
        SelfHarm = 2,
        HarmToOthers = 3,
        StigmatizingLanguage = 4
    }

    public enum StudentTurnRoute
    {
        OpenRouter = 0,
        LocalFallback = 1,
        ScriptedScenario = 2
    }

    public sealed class StudentSafetyDecision
    {
        public StudentSafetyDecision(StudentSafetyCategory category, StudentTurnRoute route)
        {
            Category = category;
            Route = route;
        }

        public StudentSafetyCategory Category { get; }
        public StudentTurnRoute Route { get; }
    }

    public static class StudentSafetyPolicy
    {
        private static readonly string PrivacyKorean = new string(new[]
        {
            '\uC8FC', '\uBBFC', '\uB4F1', '\uB85D', '\uBC88', '\uD638'
        });

        private static readonly string SelfHarmKorean = new string(new[]
        {
            '\uC790', '\uD574'
        });

        private static readonly string HarmKorean = new string(new[]
        {
            '\uC8FD', '\uC5EC'
        });

        private static readonly string StigmaKorean = new string(new[]
        {
            '\uC815', '\uC2E0', '\uBCD1', '\uC790'
        });

        private static readonly string HarmEnglish = new string(new[]
        {
            'k', 'i', 'l', 'l'
        });

        private static readonly string SelfHarmEnglish = new string(new[]
        {
            's', 'u', 'i', 'c', 'i', 'd', 'e'
        });

        public static StudentSafetyDecision Evaluate(string text)
        {
            string value = text ?? string.Empty;
            if (Contains(value, PrivacyKorean))
            {
                return Unsafe(StudentSafetyCategory.Privacy);
            }

            if (Contains(value, SelfHarmKorean) || Contains(value, SelfHarmEnglish))
            {
                return Unsafe(StudentSafetyCategory.SelfHarm);
            }

            if (Contains(value, HarmKorean) || Contains(value, HarmEnglish))
            {
                return Unsafe(StudentSafetyCategory.HarmToOthers);
            }

            if (Contains(value, StigmaKorean))
            {
                return Unsafe(StudentSafetyCategory.StigmatizingLanguage);
            }

            return new StudentSafetyDecision(StudentSafetyCategory.None, StudentTurnRoute.OpenRouter);
        }

        private static bool Contains(string value, string keyword)
        {
            return value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static StudentSafetyDecision Unsafe(StudentSafetyCategory category)
        {
            return new StudentSafetyDecision(category, StudentTurnRoute.LocalFallback);
        }
    }

    public enum StudentTurnOutcome
    {
        Accepted = 0,
        Malformed = 1,
        Unsafe = 2
    }

    public sealed class StudentTurnResolution
    {
        public StudentTurnResolution(
            StudentTurnOutcome outcome,
            StudentTurnRoute route,
            StudentAgentTurn turn,
            StudentSafetyCategory safetyCategory)
        {
            Outcome = outcome;
            Route = route;
            Turn = turn;
            SafetyCategory = safetyCategory;
        }

        public StudentTurnOutcome Outcome { get; }
        public StudentTurnRoute Route { get; }
        public StudentAgentTurn Turn { get; }
        public StudentSafetyCategory SafetyCategory { get; }
    }

    public static class StudentTurnBoundary
    {
        public static StudentTurnResolution Normalize(string raw)
        {
            StudentAgentTurn turn = GenerativeAiCoach.ParseStudentTurn(raw);
            if (turn == null)
            {
                return Fallback(StudentTurnOutcome.Malformed, StudentSafetyCategory.None);
            }

            StudentSafetyDecision safety = StudentSafetyPolicy.Evaluate(turn.studentReply);
            if (safety.Route == StudentTurnRoute.LocalFallback)
            {
                return Fallback(StudentTurnOutcome.Unsafe, safety.Category);
            }

            StudentTurnPerformanceNormalizer.Normalize(turn);
            return new StudentTurnResolution(
                StudentTurnOutcome.Accepted,
                StudentTurnRoute.OpenRouter,
                turn,
                StudentSafetyCategory.None);
        }

        private static StudentTurnResolution Fallback(
            StudentTurnOutcome outcome,
            StudentSafetyCategory category)
        {
            var turn = new StudentAgentTurn
            {
                studentReply = nameof(StudentTurnRoute.LocalFallback),
                valence = -0.25f,
                arousal = 0.45f,
                dominance = -0.2f,
                gesture = BehaviorGesture.Listen.ToString(),
                actionUnits = new ActionUnitDirective()
            };
            return new StudentTurnResolution(outcome, StudentTurnRoute.LocalFallback, turn, category);
        }

    }
}
