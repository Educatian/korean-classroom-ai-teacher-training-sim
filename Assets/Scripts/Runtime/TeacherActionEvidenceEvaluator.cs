using System;

namespace AdieLab.TeacherTraining
{
    public static class TeacherActionEvidenceEvaluator
    {
        private static readonly string EmotionCare = Korean('\uAD1C', '\uCC2E');
        private static readonly string EmotionHard = Korean('\uD798', '\uB4E4');
        private static readonly string EmotionUnderstood = Korean('\uC54C', '\uACA0');
        private static readonly string AgencyChoice = Korean('\uC120', '\uD0DD');
        private static readonly string AgencyChoose = Korean('\uACE0', '\uB974');
        private static readonly string LowPause = Korean('\uC7A0', '\uAE50');
        private static readonly string LowSlowly = Korean('\uCC9C', '\uCC9C', '\uD788');
        private static readonly string LowRest = Korean('\uC26C');
        private static readonly string SafetySafe = Korean('\uC548', '\uC804');
        private static readonly string SafetyHand = Korean('\uC190');
        private static readonly string ReentryAgain = Korean('\uB2E4', '\uC2DC');
        private static readonly string ReentryTogether = Korean('\uAC19', '\uC774');
        private static readonly string ReentryStart = Korean('\uC2DC', '\uC791');
        private static readonly string CoerciveNow = Korean('\uB2F9', '\uC7A5');
        private static readonly string CoerciveFast = Korean('\uBE68', '\uB9AC');
        private static readonly string CoerciveWhy = Korean('\uC65C');

        public static CompetencyEvidence[] ForChoice(
            CrisisScenarioProfile scenario,
            TeacherResponseOption option)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            if (option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            TeacherCompetency[] targets = scenario.evidenceTargets ?? Array.Empty<TeacherCompetency>();
            var evidence = new CompetencyEvidence[targets.Length];
            for (int index = 0; index < targets.Length; index++)
            {
                evidence[index] = Evidence(
                    string.Concat(scenario.id, targets[index].ToString()),
                    targets[index],
                    option.quality);
            }

            return evidence;
        }

        public static TeacherResponseOption ForUtterance(string utterance)
        {
            string value = utterance ?? string.Empty;
            bool emotion = ContainsAny(value, EmotionCare, EmotionHard, EmotionUnderstood);
            bool agency = ContainsAny(value, AgencyChoice, AgencyChoose);
            bool lowStimulus = ContainsAny(value, LowPause, LowSlowly, LowRest);
            bool safety = ContainsAny(value, SafetySafe, SafetyHand);
            bool reentry = ContainsAny(value, ReentryAgain, ReentryTogether, ReentryStart);
            bool coercive = ContainsAny(value, CoerciveNow, CoerciveFast, CoerciveWhy);
            int supportiveSignals = Count(emotion, agency, lowStimulus, safety, reentry);
            // A coercive marker only zeroes the turn when no supportive move accompanies it;
            // a supportive utterance that happens to contain "\uC65C" is a risky move, not abuse.
            bool coerciveDominant = coercive && supportiveSignals == 0;
            int quality = coerciveDominant
                ? 0
                : coercive
                    ? 1
                    : supportiveSignals >= 2 ? 3 : supportiveSignals == 1 ? 2 : 1;

            CompetencyEvidence[] evidence;
            if (coerciveDominant)
            {
                // Coercion itself is earned negative evidence across every dimension.
                evidence = new[]
                {
                    Evidence(nameof(TeacherCompetency.StudentDignity), TeacherCompetency.StudentDignity, 0.25f),
                    Evidence(nameof(TeacherCompetency.LowStimulusResponse), TeacherCompetency.LowStimulusResponse, 0.35f),
                    Evidence(nameof(TeacherCompetency.EmotionAcknowledgement), TeacherCompetency.EmotionAcknowledgement, 0.4f),
                    Evidence(nameof(TeacherCompetency.StudentAgency), TeacherCompetency.StudentAgency, 0.4f),
                    Evidence(nameof(TeacherCompetency.Safety), TeacherCompetency.Safety, 0.75f),
                    Evidence(nameof(TeacherCompetency.InstructionalReentry), TeacherCompetency.InstructionalReentry, 0.75f)
                };
            }
            else
            {
                // Emit evidence only for dimensions the utterance actually demonstrated \u2014
                // undetected dimensions stay unobserved instead of receiving filler mid-scores.
                float detectedScore = coercive ? 1.5f : 3f;
                var collected = new System.Collections.Generic.List<CompetencyEvidence>(6);
                if (supportiveSignals > 0)
                {
                    collected.Add(Evidence(nameof(TeacherCompetency.StudentDignity), TeacherCompetency.StudentDignity,
                        coercive ? 1f : 2.5f));
                }
                if (lowStimulus)
                {
                    collected.Add(Evidence(nameof(TeacherCompetency.LowStimulusResponse), TeacherCompetency.LowStimulusResponse, detectedScore));
                }
                if (emotion)
                {
                    collected.Add(Evidence(nameof(TeacherCompetency.EmotionAcknowledgement), TeacherCompetency.EmotionAcknowledgement, detectedScore));
                }
                if (agency)
                {
                    collected.Add(Evidence(nameof(TeacherCompetency.StudentAgency), TeacherCompetency.StudentAgency, detectedScore));
                }
                if (safety)
                {
                    collected.Add(Evidence(nameof(TeacherCompetency.Safety), TeacherCompetency.Safety, detectedScore));
                }
                if (reentry)
                {
                    collected.Add(Evidence(nameof(TeacherCompetency.InstructionalReentry), TeacherCompetency.InstructionalReentry, detectedScore));
                }
                evidence = collected.ToArray();
            }

            return new TeacherResponseOption
            {
                label = value,
                spokenResponse = value,
                quality = quality,
                rationale = BuildUtteranceRationale(
                    quality, coerciveDominant, coercive, emotion, agency, lowStimulus, safety, reentry),
                resultingAffect = quality >= 2 ? StudentAffect.Recovering : StudentAffect.Distressed,
                competencyEvidence = evidence
            };
        }

        private static string BuildUtteranceRationale(
            int quality,
            bool coerciveDominant,
            bool coercive,
            bool emotion,
            bool agency,
            bool lowStimulus,
            bool safety,
            bool reentry)
        {
            if (coerciveDominant)
            {
                return "\uC555\uBC15 \uD45C\uD604\uC774 \uAC10\uC9C0\uB418\uC5C8\uC2B5\uB2C8\uB2E4. \uAC10\uC815 \uC778\uC815\uACFC \uC120\uD0DD\uAD8C \uC81C\uC548\uC73C\uB85C \uC7AC\uAD6C\uC131\uD574 \uBCF4\uC138\uC694.";
            }

            var detected = new System.Collections.Generic.List<string>(5);
            var missing = new System.Collections.Generic.List<string>(5);
            (detected, missing) = SortSignals(emotion, agency, lowStimulus, safety, reentry);
            string detectedText = detected.Count > 0 ? string.Join(", ", detected) : "\uC5C6\uC74C";
            if (quality >= 2)
            {
                string caveat = coercive ? " \uB2E4\uB9CC \uC555\uBC15\uC131 \uD45C\uD604\uC774 \uC11E\uC5EC \uD6A8\uACFC\uAC00 \uC904 \uC218 \uC788\uC2B5\uB2C8\uB2E4." : string.Empty;
                return $"\uACF5\uB3D9\uC870\uC808 \uC2E0\uD638\uAC00 \uD655\uC778\uB418\uC5C8\uC2B5\uB2C8\uB2E4: {detectedText}.{caveat}";
            }

            string missingText = missing.Count > 0 ? string.Join(", ", missing) : string.Empty;
            return coercive
                ? $"\uC9C0\uC9C0 \uC2E0\uD638({detectedText})\uAC00 \uC788\uC73C\uB098 \uC555\uBC15\uC131 \uD45C\uD604\uC774 \uD568\uAED8 \uAC10\uC9C0\uB418\uC5C8\uC2B5\uB2C8\uB2E4."
                : $"\uACF5\uB3D9\uC870\uC808 \uC2E0\uD638\uB97C \uBCF4\uC644\uD558\uC138\uC694. \uC608: {missingText}.";
        }

        private static (System.Collections.Generic.List<string> detected, System.Collections.Generic.List<string> missing) SortSignals(
            bool emotion,
            bool agency,
            bool lowStimulus,
            bool safety,
            bool reentry)
        {
            var detected = new System.Collections.Generic.List<string>(5);
            var missing = new System.Collections.Generic.List<string>(5);
            (emotion ? detected : missing).Add("\uAC10\uC815 \uC778\uC815");
            (agency ? detected : missing).Add("\uC120\uD0DD\uAD8C \uC81C\uC548");
            (lowStimulus ? detected : missing).Add("\uC644\uAE09 \uC870\uC808");
            (safety ? detected : missing).Add("\uC548\uC804 \uD655\uC778");
            (reentry ? detected : missing).Add("\uBCF5\uADC0 \uC5F0\uACB0");
            return (detected, missing);
        }

        private static CompetencyEvidence Evidence(
            string evidenceId,
            TeacherCompetency dimension,
            float score)
        {
            return new CompetencyEvidence
            {
                evidenceId = evidenceId,
                observableId = dimension.ToString(),
                rationale = string.Empty,
                dimension = dimension,
                score = score
            };
        }

        private static bool ContainsAny(string value, params string[] keywords)
        {
            for (int index = 0; index < keywords.Length; index++)
            {
                if (value.IndexOf(keywords[index], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int Count(params bool[] signals)
        {
            int count = 0;
            for (int index = 0; index < signals.Length; index++)
            {
                if (signals[index])
                {
                    count++;
                }
            }

            return count;
        }

        private static string Korean(params char[] characters)
        {
            return new string(characters);
        }
    }
}
