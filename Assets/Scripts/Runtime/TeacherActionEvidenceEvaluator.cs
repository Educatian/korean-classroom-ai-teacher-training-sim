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
            int quality = coercive ? 0 : supportiveSignals >= 2 ? 3 : supportiveSignals == 1 ? 2 : 1;

            var evidence = new[]
            {
                Evidence(nameof(TeacherCompetency.StudentDignity), TeacherCompetency.StudentDignity,
                    coercive ? 0.25f : supportiveSignals > 0 ? 2.5f : 1.25f),
                Evidence(nameof(TeacherCompetency.LowStimulusResponse), TeacherCompetency.LowStimulusResponse,
                    coercive ? 0.35f : lowStimulus ? 3f : 1.5f),
                Evidence(nameof(TeacherCompetency.EmotionAcknowledgement), TeacherCompetency.EmotionAcknowledgement,
                    emotion ? 3f : coercive ? 0.4f : 1.4f),
                Evidence(nameof(TeacherCompetency.StudentAgency), TeacherCompetency.StudentAgency,
                    agency ? 3f : coercive ? 0.4f : 1.4f),
                Evidence(nameof(TeacherCompetency.Safety), TeacherCompetency.Safety,
                    safety ? 3f : coercive ? 0.75f : 1.5f),
                Evidence(nameof(TeacherCompetency.InstructionalReentry), TeacherCompetency.InstructionalReentry,
                    reentry ? 3f : coercive ? 0.75f : 1.5f)
            };

            return new TeacherResponseOption
            {
                label = value,
                spokenResponse = value,
                quality = quality,
                rationale = quality >= 2
                    ? Korean('\uACF5', '\uB3D9', '\uC870', '\uC808', ' ', '\uC2E0', '\uD638', '\uAC00', ' ', '\uD655', '\uC778', '\uB418', '\uC5C8', '\uC2B5', '\uB2C8', '\uB2E4', '.')
                    : Korean('\uACF5', '\uB3D9', '\uC870', '\uC808', ' ', '\uC2E0', '\uD638', '\uB97C', ' ', '\uBCF4', '\uC644', '\uD558', '\uC138', '\uC694', '.'),
                resultingAffect = quality >= 2 ? StudentAffect.Recovering : StudentAffect.Distressed,
                competencyEvidence = evidence
            };
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
