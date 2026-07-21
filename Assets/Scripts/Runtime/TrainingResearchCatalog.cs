using System;

namespace AdieLab.TeacherTraining
{
    public enum TrainingSceneId
    {
        GeneralClassroom = 1,
        CircleDiscussion = 2,
        RecoveryRoom = 3,
        Schoolyard = 4,
        Gymnasium = 5
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
        public static StudentPersonaProfile[] BuildPersonas()
        {
            return TrainingScenarioCatalog.LoadDefault().BuildPersonas();
        }

        public static CrisisScenarioProfile[] BuildCrisisScenarios()
        {
            return TrainingScenarioCatalog.LoadDefault().BuildCrisisScenarios();
        }

        public static CrisisScenarioProfile ForBeat(TrainingSceneId sceneId, int beatIndex)
        {
            CrisisScenarioProfile[] profiles = TrainingScenarioCatalog
                .LoadDefault()
                .ScenarioFor(sceneId)
                .BuildResearchProfiles();
            if (profiles.Length == 0)
            {
                throw new InvalidOperationException($"No authored crisis beats exist for {sceneId}.");
            }

            return profiles[Math.Abs(beatIndex) % profiles.Length];
        }
    }
}