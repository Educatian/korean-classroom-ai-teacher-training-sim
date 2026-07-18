using System;
using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class OpenRouterSafetyTests
    {
        [Test]
        public void RuntimeConfiguration_UsesEnvironmentOverridesAtTheBoundary()
        {
            OpenRouterRuntimeConfiguration config =
                OpenRouterRuntimeConfiguration.FromEnvironment(key =>
                {
                    if (key == OpenRouterRuntimeConfiguration.MaxTokensEnvironmentVariable)
                    {
                        return 384.ToString();
                    }
                    if (key == OpenRouterRuntimeConfiguration.TemperatureEnvironmentVariable)
                    {
                        return 0.35f.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    if (key == OpenRouterRuntimeConfiguration.PromptVersionEnvironmentVariable)
                    {
                        return 7.ToString();
                    }

                    return null;
                });

            Assert.That(config.MaxTokens, Is.EqualTo(384));
            Assert.That(config.Temperature, Is.EqualTo(0.35f).Within(0.001f));
            Assert.That(config.PromptVersion, Is.EqualTo(7));
        }

        [Test]
        public void RequestPayload_ContainsReproducibleGenerationSettings()
        {
            var config = new OpenRouterRuntimeConfiguration(320, 0.25f, 4);

            string json = GenerativeAiCoach.CreateRequestJson(
                string.Empty,
                string.Empty,
                true,
                config);
            PayloadProbe payload = JsonUtility.FromJson<PayloadProbe>(json);

            Assert.That(payload.max_tokens, Is.EqualTo(320));
            Assert.That(payload.temperature, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(payload.metadata.prompt_version, Is.EqualTo(4));
        }

        [TestCase(StudentSafetyCategory.Privacy)]
        [TestCase(StudentSafetyCategory.SelfHarm)]
        [TestCase(StudentSafetyCategory.HarmToOthers)]
        [TestCase(StudentSafetyCategory.StigmatizingLanguage)]
        public void SafetyPolicy_RoutesUnsafeLanguageToLocalFallback(
            StudentSafetyCategory category)
        {
            string text = category switch
            {
                StudentSafetyCategory.Privacy =>
                    new string(new[] { '주', '민', '등', '록', '번', '호' }),
                StudentSafetyCategory.SelfHarm =>
                    new string(new[] { '자', '해', '하', '고', ' ', '싶', '어' }),
                StudentSafetyCategory.HarmToOthers =>
                    new string(new[] { '죽', '여', '버', '릴', ' ', '거', '야' }),
                StudentSafetyCategory.StigmatizingLanguage =>
                    new string(new[] { '정', '신', '병', '자' }),
                _ => string.Empty
            };

            StudentSafetyDecision decision = StudentSafetyPolicy.Evaluate(text);

            Assert.That(decision.Category, Is.EqualTo(category));
            Assert.That(decision.Route, Is.EqualTo(StudentTurnRoute.LocalFallback));
        }

        [Test]
        public void SafetyPolicy_DetectsEnglishHarmLanguage()
        {
            string text = new string(new[] { 'I', ' ', 'w', 'i', 'l', 'l', ' ', 'k', 'i', 'l', 'l', ' ', 'y', 'o', 'u' });

            StudentSafetyDecision decision = StudentSafetyPolicy.Evaluate(text);

            Assert.That(decision.Category, Is.EqualTo(StudentSafetyCategory.HarmToOthers));
            Assert.That(decision.Route, Is.EqualTo(StudentTurnRoute.LocalFallback));
        }

        [Test]
        public void StudentTurnBoundary_ClampsValidStructuredOutput()
        {
            string raw = JsonUtility.ToJson(new StudentAgentTurn
            {
                studentReply = nameof(StudentTurnBoundary_ClampsValidStructuredOutput),
                valence = 2f,
                arousal = -1f,
                dominance = 3f,
                gesture = BehaviorGesture.Recover.ToString()
            });

            StudentTurnResolution resolution = StudentTurnBoundary.Normalize(raw);

            Assert.That(resolution.Outcome, Is.EqualTo(StudentTurnOutcome.Accepted));
            Assert.That(resolution.Turn.valence, Is.EqualTo(1f));
            Assert.That(resolution.Turn.arousal, Is.EqualTo(0f));
            Assert.That(resolution.Turn.dominance, Is.EqualTo(1f));
        }

        [Test]
        public void StudentTurnBoundary_NormalizesMalformedAndUnsafeOutput()
        {
            StudentTurnResolution malformed = StudentTurnBoundary.Normalize(string.Empty);
            string unsafeRaw = JsonUtility.ToJson(new StudentAgentTurn
            {
                studentReply = new string(new[] { '자', '해', '하', '고', ' ', '싶', '어' }),
                valence = -0.5f,
                arousal = 0.8f,
                dominance = 0f,
                gesture = BehaviorGesture.Withdraw.ToString()
            });
            StudentTurnResolution unsafeResult = StudentTurnBoundary.Normalize(unsafeRaw);

            Assert.That(malformed.Outcome, Is.EqualTo(StudentTurnOutcome.Malformed));
            Assert.That(malformed.Route, Is.EqualTo(StudentTurnRoute.LocalFallback));
            Assert.That(unsafeResult.Outcome, Is.EqualTo(StudentTurnOutcome.Unsafe));
            Assert.That(unsafeResult.Route, Is.EqualTo(StudentTurnRoute.LocalFallback));
        }

        [Serializable]
        private sealed class PayloadProbe
        {
            public int max_tokens;
            public float temperature;
            public MetadataProbe metadata;
        }

        [Serializable]
        private sealed class MetadataProbe
        {
            public int prompt_version;
        }
    }
}
