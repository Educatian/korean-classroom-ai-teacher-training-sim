using System;

namespace AdieLab.TeacherTraining
{
    public enum TrainingSceneId
    {
        GeneralClassroom = 1,
        CircleDiscussion = 2
    }

    public enum StudentGradeBand
    {
        UpperElementary = 0
    }

    public enum StudentStrength
    {
        VerbalReasoning = 0,
        Creativity = 1,
        PeerConnection = 2,
        Persistence = 3,
        Humor = 4,
        VisualLearning = 5
    }

    public enum PersonaSupportNeed
    {
        PredictableChoice = 0,
        LowStimulusLanguage = 1,
        ProcessingTime = 2,
        PrivateCorrection = 3,
        MovementBreak = 4,
        CoRegulation = 5
    }

    public enum CrisisScenarioType
    {
        TaskOverload = 0,
        EscalatingDeskTap = 1,
        PanicBreathing = 2,
        PeerConflict = 3,
        PresentationAvoidance = 4,
        InstructionalRefusal = 5
    }

    public enum TrainingSafetyFlag
    {
        EmotionalDistress = 0,
        Escalation = 1,
        PhysicalSafety = 2,
        PeerAudience = 3,
        PrivacySensitive = 4
    }

    public enum PeerAttentionPattern
    {
        TeacherCentered = 0,
        FocalStudentConcern = 1,
        PeerDistraction = 2,
        PresentationAudience = 3
    }

    [Serializable]
    public sealed class StudentPersonaProfile
    {
        public string id;
        public string displayName;
        public StudentGradeBand gradeBand;
        public StudentStrength[] strengths;
        public PersonaSupportNeed[] supportNeeds;
    }

    [Serializable]
    public sealed class CrisisScenarioProfile
    {
        public string id;
        public string title;
        public TrainingSceneId sceneId;
        public CrisisScenarioType crisisType;
        public string personaId;
        public TeacherCompetency[] evidenceTargets;
        public TrainingSafetyFlag[] safetyFlags;
        public PeerAttentionPattern peerAttention;
        public bool presentationAvoidance;
    }

    public static class TrainingResearchCatalog
    {
        private enum PersonaKey
        {
            Minjun,
            Seoyun,
            Jiho,
            Haeun,
            Jiwon
        }

        private enum ScenarioKey
        {
            WorkloadFreeze,
            DeskTapEscalation,
            PanicRecovery,
            PeerDisagreement,
            PresentationTurn,
            ReentryRefusal
        }

        public static StudentPersonaProfile[] BuildPersonas()
        {
            return new[]
            {
                Persona(
                    PersonaKey.Minjun,
                    Korean('\uBBFC', '\uC900'),
                    new[] { StudentStrength.VisualLearning, StudentStrength.Creativity },
                    new[] { PersonaSupportNeed.PredictableChoice, PersonaSupportNeed.ProcessingTime }),
                Persona(
                    PersonaKey.Seoyun,
                    Korean('\uC11C', '\uC724'),
                    new[] { StudentStrength.VerbalReasoning, StudentStrength.PeerConnection },
                    new[] { PersonaSupportNeed.CoRegulation, PersonaSupportNeed.LowStimulusLanguage }),
                Persona(
                    PersonaKey.Jiho,
                    Korean('\uC9C0', '\uD638'),
                    new[] { StudentStrength.Persistence, StudentStrength.Humor },
                    new[] { PersonaSupportNeed.MovementBreak, PersonaSupportNeed.PrivateCorrection }),
                Persona(
                    PersonaKey.Haeun,
                    Korean('\uD558', '\uC740'),
                    new[] { StudentStrength.Creativity, StudentStrength.PeerConnection },
                    new[] { PersonaSupportNeed.ProcessingTime, PersonaSupportNeed.CoRegulation }),
                Persona(
                    PersonaKey.Jiwon,
                    Korean('\uC9C0', '\uC6D0'),
                    new[] { StudentStrength.VisualLearning, StudentStrength.Persistence },
                    new[] { PersonaSupportNeed.PredictableChoice, PersonaSupportNeed.PrivateCorrection })
            };
        }

        public static CrisisScenarioProfile[] BuildCrisisScenarios()
        {
            return new[]
            {
                Scenario(
                    ScenarioKey.WorkloadFreeze,
                    TrainingSceneId.GeneralClassroom,
                    CrisisScenarioType.TaskOverload,
                    PersonaKey.Minjun,
                    PeerAttentionPattern.TeacherCentered,
                    false,
                    new[] { TeacherCompetency.EmotionAcknowledgement, TeacherCompetency.StudentAgency },
                    new[] { TrainingSafetyFlag.EmotionalDistress }),
                Scenario(
                    ScenarioKey.DeskTapEscalation,
                    TrainingSceneId.GeneralClassroom,
                    CrisisScenarioType.EscalatingDeskTap,
                    PersonaKey.Jiho,
                    PeerAttentionPattern.TeacherCentered,
                    false,
                    new[] { TeacherCompetency.LowStimulusResponse, TeacherCompetency.Safety },
                    new[] { TrainingSafetyFlag.Escalation, TrainingSafetyFlag.PhysicalSafety }),
                Scenario(
                    ScenarioKey.PanicRecovery,
                    TrainingSceneId.GeneralClassroom,
                    CrisisScenarioType.PanicBreathing,
                    PersonaKey.Seoyun,
                    PeerAttentionPattern.TeacherCentered,
                    false,
                    new[] { TeacherCompetency.EmotionAcknowledgement, TeacherCompetency.Safety },
                    new[] { TrainingSafetyFlag.EmotionalDistress }),
                Scenario(
                    ScenarioKey.PeerDisagreement,
                    TrainingSceneId.CircleDiscussion,
                    CrisisScenarioType.PeerConflict,
                    PersonaKey.Haeun,
                    PeerAttentionPattern.PeerDistraction,
                    false,
                    new[] { TeacherCompetency.StudentDignity, TeacherCompetency.LowStimulusResponse },
                    new[] { TrainingSafetyFlag.PeerAudience, TrainingSafetyFlag.Escalation }),
                Scenario(
                    ScenarioKey.PresentationTurn,
                    TrainingSceneId.CircleDiscussion,
                    CrisisScenarioType.PresentationAvoidance,
                    PersonaKey.Minjun,
                    PeerAttentionPattern.PresentationAudience,
                    true,
                    new[] { TeacherCompetency.StudentAgency, TeacherCompetency.InstructionalReentry },
                    new[] { TrainingSafetyFlag.PeerAudience, TrainingSafetyFlag.EmotionalDistress }),
                Scenario(
                    ScenarioKey.ReentryRefusal,
                    TrainingSceneId.GeneralClassroom,
                    CrisisScenarioType.InstructionalRefusal,
                    PersonaKey.Jiwon,
                    PeerAttentionPattern.FocalStudentConcern,
                    false,
                    new[] { TeacherCompetency.StudentDignity, TeacherCompetency.InstructionalReentry },
                    new[] { TrainingSafetyFlag.PrivacySensitive })
            };
        }

        public static CrisisScenarioProfile ForBeat(TrainingSceneId sceneId, int beatIndex)
        {
            CrisisScenarioProfile[] all = BuildCrisisScenarios();
            int matchingCount = 0;
            for (int index = 0; index < all.Length; index++)
            {
                if (all[index].sceneId == sceneId)
                {
                    matchingCount++;
                }
            }

            if (matchingCount == 0)
            {
                throw new InvalidOperationException(sceneId.ToString());
            }

            int target = Math.Abs(beatIndex) % matchingCount;
            for (int index = 0; index < all.Length; index++)
            {
                if (all[index].sceneId != sceneId)
                {
                    continue;
                }

                if (target == 0)
                {
                    return all[index];
                }

                target--;
            }

            throw new InvalidOperationException(sceneId.ToString());
        }

        private static StudentPersonaProfile Persona(
            PersonaKey key,
            string displayName,
            StudentStrength[] strengths,
            PersonaSupportNeed[] supportNeeds)
        {
            return new StudentPersonaProfile
            {
                id = key.ToString(),
                displayName = displayName,
                gradeBand = StudentGradeBand.UpperElementary,
                strengths = strengths,
                supportNeeds = supportNeeds
            };
        }

        private static CrisisScenarioProfile Scenario(
            ScenarioKey key,
            TrainingSceneId sceneId,
            CrisisScenarioType crisisType,
            PersonaKey persona,
            PeerAttentionPattern peerAttention,
            bool presentationAvoidance,
            TeacherCompetency[] evidenceTargets,
            TrainingSafetyFlag[] safetyFlags)
        {
            return new CrisisScenarioProfile
            {
                id = key.ToString(),
                title = crisisType.ToString(),
                sceneId = sceneId,
                crisisType = crisisType,
                personaId = persona.ToString(),
                evidenceTargets = evidenceTargets,
                safetyFlags = safetyFlags,
                peerAttention = peerAttention,
                presentationAvoidance = presentationAvoidance
            };
        }

        private static string Korean(params char[] characters)
        {
            return new string(characters);
        }
    }
}
