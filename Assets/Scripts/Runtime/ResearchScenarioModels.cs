using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum ResearchEvidenceStatus
    {
        Provisional = 0,
        InterviewSupported = 1,
        ExpertReviewed = 2,
        Approved = 3
    }

    public enum StudentGenderProfile
    {
        Unspecified = 0,
        Boy = 1,
        Girl = 2,
        Nonbinary = 3
    }

    public enum StudentTemperamentProfile
    {
        Cautious = 0,
        Expressive = 1,
        Persistent = 2,
        SensitiveToEvaluation = 3,
        SociallyAlert = 4
    }

    public enum RegulationStrategy
    {
        QuietWaitTime = 0,
        PrivateConversation = 1,
        MovementBreak = 2,
        VisualChoice = 3,
        BreathingPrompt = 4,
        TrustedAdultContact = 5,
        SmallReentryStep = 6
    }

    [Serializable]
    public sealed class CrisisTypeDefinition
    {
        public string id;
        public string label;
        [TextArea(2, 4)] public string operationalDefinition;
        public CrisisScenarioType[] mappedScenarioTypes = Array.Empty<CrisisScenarioType>();
        public string[] observableSignals = Array.Empty<string>();
        public string[] likelyTriggers = Array.Empty<string>();
        public string[] responsePriorities = Array.Empty<string>();
        public string[] contraindicatedResponses = Array.Empty<string>();
        public ResearchEvidenceStatus evidenceStatus = ResearchEvidenceStatus.Provisional;
    }

    [CreateAssetMenu(
        fileName = "CrisisTypeCatalog",
        menuName = "Teacher Training/Research/Crisis Type Catalog",
        order = 40)]
    public sealed class CrisisTypeCatalogAsset : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/Research/CrisisTypeCatalog";

        [SerializeField] private string catalogId = "elementary-crisis-types-v1";
        [SerializeField] private int schemaVersion = 1;
        [SerializeField] private CrisisTypeDefinition[] definitions = Array.Empty<CrisisTypeDefinition>();

        public string CatalogId => catalogId;
        public int SchemaVersion => schemaVersion;
        public IReadOnlyList<CrisisTypeDefinition> Definitions => definitions;

#if UNITY_EDITOR
        public void ConfigureForEditor(CrisisTypeDefinition[] authoredDefinitions)
        {
            definitions = authoredDefinitions ?? Array.Empty<CrisisTypeDefinition>();
        }
#endif
    }

    [Serializable]
    public sealed class ResearchInterviewPrompt
    {
        public string id;
        public string section;
        [TextArea(2, 5)] public string prompt;
        [TextArea(1, 3)] public string evidenceTarget;
    }

    [CreateAssetMenu(
        fileName = "TeacherInterviewProtocol",
        menuName = "Teacher Training/Research/Interview Protocol",
        order = 41)]
    public sealed class ResearchInterviewProtocolAsset : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/Research/TeacherInterviewProtocol";

        [SerializeField] private string protocolId = "teacher-crisis-interview-v1";
        [SerializeField] private string participantGroup = "현직 초등교사 및 정서행동 지원 전문가";
        [SerializeField] private ResearchEvidenceStatus evidenceStatus = ResearchEvidenceStatus.Provisional;
        [SerializeField] private ResearchInterviewPrompt[] prompts = Array.Empty<ResearchInterviewPrompt>();

        public string ProtocolId => protocolId;
        public string ParticipantGroup => participantGroup;
        public ResearchEvidenceStatus EvidenceStatus => evidenceStatus;
        public IReadOnlyList<ResearchInterviewPrompt> Prompts => prompts;

#if UNITY_EDITOR
        public void ConfigureForEditor(ResearchInterviewPrompt[] authoredPrompts)
        {
            prompts = authoredPrompts ?? Array.Empty<ResearchInterviewPrompt>();
            evidenceStatus = ResearchEvidenceStatus.Provisional;
        }
#endif
    }
}
