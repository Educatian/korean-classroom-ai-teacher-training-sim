using System;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum TrainingEventKind
    {
        SessionStarted = 0,
        BeatPresented = 1,
        TeacherAction = 2,
        StudentResponse = 3,
        SessionCompleted = 4,
        SessionPaused = 5,
        SessionResumed = 6,
        SessionAborted = 7,
        RubricEvaluation = 8,
        ReflectionSubmitted = 9
    }

    public enum TrainingActionSource
    {
        System = 0,
        ScriptedScenario = 1,
        TeacherChoice = 2,
        TeacherUtterance = 3,
        GenerativeModel = 4,
        LocalFallback = 5
    }

    public enum TeacherCompetency
    {
        StudentDignity = 0,
        LowStimulusResponse = 1,
        EmotionAcknowledgement = 2,
        StudentAgency = 3,
        Safety = 4,
        InstructionalReentry = 5
    }

    public static class TrainingActionSourceLabels
    {
        public static string Korean(TrainingActionSource source)
        {
            return source switch
            {
                TrainingActionSource.System => "시스템",
                TrainingActionSource.ScriptedScenario => "시나리오",
                TrainingActionSource.TeacherChoice => "선택 대응",
                TrainingActionSource.TeacherUtterance => "직접 대화",
                TrainingActionSource.GenerativeModel => "AI 학생 반응",
                TrainingActionSource.LocalFallback => "로컬 대화",
                _ => source.ToString()
            };
        }

        public static string Korean(string serializedSource)
        {
            return System.Enum.TryParse(serializedSource, out TrainingActionSource parsed)
                ? Korean(parsed)
                : serializedSource;
        }
    }

    [Serializable]
    public sealed class StudentStateSnapshot
    {
        public AffectVector affect;
        public BehaviorGesture gesture;
        [Range(0f, 1f)] public float gestureIntensity;
        [Range(0f, 1f)] public float gazeContact;
        [Range(0f, 1f)] public float engagement;
        [Range(0f, 1f)] public float trust;
    }

    [Serializable]
    public sealed class ModelPromptProvenance
    {
        public string modelId;
        public string promptTemplateId;
        public int promptVersion;
        public string promptHash;
        public bool fallbackUsed;
        public string fallbackReason;
        public long latencyMilliseconds;
    }

    [Serializable]
    public sealed class StudentSpeechTelemetry
    {
        public bool requested;
        public string providerRoute;
        public float rate;
        public float pitchSemitones;
        public float volume;
        public int commaPauseMilliseconds;
        public int sentencePauseMilliseconds;
        public string disclosure;
    }

    [Serializable]
    public sealed class CompetencyEvidence
    {
        public string evidenceId;
        public string observableId;
        public string rationale;
        public TeacherCompetency dimension;
        [Range(0f, 3f)] public float score;
    }

    [Serializable]
    public sealed class TrainingTelemetryEvent
    {
        public const int CurrentSchemaVersion = 5;

        public int schemaVersion = CurrentSchemaVersion;
        public string eventId;
        public string sessionId;
        public int attemptNumber = 1;
        public int sequence;
        public string timestampUtc;
        public string scenarioId;
        public int beatIndex;
        public TrainingEventKind kind;
        public TrainingPhase phaseBefore;
        public TrainingPhase phaseAfter;
        public string actionId;
        public TrainingActionSource actionSource;
        public int teacherTextLength;
        public string teacherTextHash;
        public string coachSuggestion;
        public string studentReplyHash;
        public StudentTurnRoute turnRoute;
        public StudentTurnOutcome turnOutcome;
        public StudentStateSnapshot studentStateBefore = new StudentStateSnapshot();
        public StudentStateSnapshot studentStateAfter = new StudentStateSnapshot();
        public ModelPromptProvenance inference = new ModelPromptProvenance();
        public CompetencyEvidence[] competencyEvidence = Array.Empty<CompetencyEvidence>();
        public StudentSpeechTelemetry studentSpeech = new StudentSpeechTelemetry();
        public TeacherGazeSummary gaze = new TeacherGazeSummary();
    }
}
