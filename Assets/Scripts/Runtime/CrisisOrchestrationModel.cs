using System;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum CrisisOrchestrationPhase
    {
        Assess = 0,
        Stabilize = 1,
        AwaitSupport = 2,
        Handoff = 3,
        Documentation = 4,
        Recovery = 5,
        Complete = 6
    }

    public enum CrisisOrchestrationAction
    {
        PauseAndRegulate = 0,
        AddressStudent = 1,
        MovePeersToSafety = 2,
        RequestColleagueSupport = 3,
        RequestAdministratorSupport = 4,
        ConfirmSupportArrival = 5,
        HandoffWithBriefing = 6,
        RecordObjectiveFacts = 7,
        RequestTeacherRecoverySupport = 8
    }

    public enum SupportResponseState
    {
        NotRequested = 0,
        Requested = 1,
        Unavailable = 2,
        EnRoute = 3,
        Present = 4,
        HandoffComplete = 5
    }

    public enum CrisisOrchestrationEvidenceId
    {
        TeacherStateAcknowledged = 0,
        RegulatedBeforeIntervention = 1,
        PeerSafetyPrioritized = 2,
        TimelyHelpSeeking = 3,
        EscalatedSupportRequest = 4,
        ClearHandoff = 5,
        ObjectiveDocumentation = 6,
        TeacherRecoveryPlanned = 7,
        PrematureDirectIntervention = 8,
        UnsupportedHandoffAttempt = 9,
        UnsafeDocumentationTiming = 10
    }

    [Serializable]
    public sealed class TeacherOperationalState
    {
        [Range(0f, 1f)] public float arousal;
        [Range(0f, 1f)] public float regulationCapacity;
        [Range(0f, 1f)] public float physicalSafety = 1f;
        [Range(0f, 1f)] public float perceivedSupport;
        [Range(0f, 1f)] public float responseConfidence;

        public TeacherOperationalState Copy()
        {
            return (TeacherOperationalState)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class ClassroomOperationalState
    {
        [Range(0f, 1f)] public float focalStudentRisk;
        [Range(0f, 1f)] public float peerDistress;
        [Range(0f, 1f)] public float noise;
        [Min(0)] public int peersInUnsafeArea;
        [Range(0f, 1f)] public float learningContinuity;

        public ClassroomOperationalState Copy()
        {
            return (ClassroomOperationalState)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class SupportResourceState
    {
        public bool colleagueAvailable;
        public bool administratorAvailable;
        public bool counselorAvailable;
        public SupportResponseState colleagueResponse;
        public SupportResponseState administratorResponse;

        public SupportResourceState Copy()
        {
            return (SupportResourceState)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class CrisisOrchestrationState
    {
        public CrisisOrchestrationPhase phase;
        public TeacherOperationalState teacher = new TeacherOperationalState();
        public ClassroomOperationalState classroom = new ClassroomOperationalState();
        public SupportResourceState support = new SupportResourceState();
        public bool objectiveRecordCompleted;
        public bool recoveryPlanCompleted;

        public CrisisOrchestrationState Copy()
        {
            return new CrisisOrchestrationState
            {
                phase = phase,
                teacher = teacher?.Copy() ?? new TeacherOperationalState(),
                classroom = classroom?.Copy() ?? new ClassroomOperationalState(),
                support = support?.Copy() ?? new SupportResourceState(),
                objectiveRecordCompleted = objectiveRecordCompleted,
                recoveryPlanCompleted = recoveryPlanCompleted
            };
        }
    }

    [Serializable]
    public sealed class CrisisOrchestrationResolution
    {
        public bool accepted;
        public string feedback = string.Empty;
        public CrisisOrchestrationState before = new CrisisOrchestrationState();
        public CrisisOrchestrationState after = new CrisisOrchestrationState();
        public CrisisOrchestrationEvidenceId[] evidence = Array.Empty<CrisisOrchestrationEvidenceId>();
    }
}
