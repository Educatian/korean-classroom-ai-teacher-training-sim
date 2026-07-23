using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum LearningSupportLevel
    {
        Hidden = 0,
        ObservationCue = 1,
        Principle = 2,
        PostActionContrast = 3
    }

    public enum LearningSupportTrigger
    {
        None = 0,
        ManualRequest = 1,
        Inactivity = 2,
        RepeatedMissedSignal = 3,
        PostActionRequest = 4
    }

    [Serializable]
    public sealed class LearningSupportStagePrompt
    {
        public CrisisStage stage;
        [TextArea(2, 4)] public string observationCue;
        [TextArea(2, 4)] public string principleCue;
    }

    [CreateAssetMenu(
        fileName = "LearningSupportPolicy",
        menuName = "Teacher Training/Learning Support Policy",
        order = 42)]
    public sealed class LearningSupportPolicy : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/Research/LearningSupportPolicy";

        [Header("Adaptive Triggers")]
        [SerializeField, Min(5f)] private float inactivitySeconds = 18f;
        [SerializeField, Range(1, 4)] private int repeatedMissesForPrinciple = 2;
        [SerializeField, Range(0, 2)] private int lowQualityThreshold = 1;

        [Header("Fading Across Retries")]
        [SerializeField, Min(1f)] private float retryDelayMultiplier = 1.35f;
        [SerializeField] private LearningSupportLevel firstAttemptAutomaticMaximum = LearningSupportLevel.Principle;
        [SerializeField] private LearningSupportLevel retryAutomaticMaximum = LearningSupportLevel.ObservationCue;

        [Header("Authored Stage Guidance")]
        [SerializeField] private LearningSupportStagePrompt[] stagePrompts = Array.Empty<LearningSupportStagePrompt>();

        public float InactivitySeconds => inactivitySeconds;
        public int RepeatedMissesForPrinciple => repeatedMissesForPrinciple;
        public int LowQualityThreshold => lowQualityThreshold;
        public float RetryDelayMultiplier => retryDelayMultiplier;
        public LearningSupportLevel FirstAttemptAutomaticMaximum => firstAttemptAutomaticMaximum;
        public LearningSupportLevel RetryAutomaticMaximum => retryAutomaticMaximum;
        public IReadOnlyList<LearningSupportStagePrompt> StagePrompts => stagePrompts;

        public float InactivityThresholdForAttempt(int attemptNumber)
        {
            return inactivitySeconds * (attemptNumber > 1 ? retryDelayMultiplier : 1f);
        }

        public LearningSupportLevel AutomaticMaximumForAttempt(int attemptNumber)
        {
            return attemptNumber > 1 ? retryAutomaticMaximum : firstAttemptAutomaticMaximum;
        }

        public LearningSupportStagePrompt PromptFor(CrisisStage stage)
        {
            for (int index = 0; index < stagePrompts.Length; index++)
            {
                LearningSupportStagePrompt prompt = stagePrompts[index];
                if (prompt != null && prompt.stage == stage)
                {
                    return prompt;
                }
            }

            return null;
        }

        public static LearningSupportPolicy LoadDefault()
        {
            LearningSupportPolicy policy = Resources.Load<LearningSupportPolicy>(DefaultResourcePath);
            return policy != null ? policy : CreateRuntimeDefault();
        }

        public static LearningSupportPolicy CreateRuntimeDefault()
        {
            var policy = CreateInstance<LearningSupportPolicy>();
            policy.name = "Runtime Learning Support Policy";
            policy.stagePrompts = DefaultPrompts();
            return policy;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            float authoredInactivitySeconds,
            int authoredRepeatedMisses,
            int authoredLowQualityThreshold,
            float authoredRetryDelayMultiplier,
            LearningSupportStagePrompt[] authoredPrompts)
        {
            inactivitySeconds = Mathf.Max(5f, authoredInactivitySeconds);
            repeatedMissesForPrinciple = Mathf.Clamp(authoredRepeatedMisses, 1, 4);
            lowQualityThreshold = Mathf.Clamp(authoredLowQualityThreshold, 0, 2);
            retryDelayMultiplier = Mathf.Max(1f, authoredRetryDelayMultiplier);
            firstAttemptAutomaticMaximum = LearningSupportLevel.Principle;
            retryAutomaticMaximum = LearningSupportLevel.ObservationCue;
            stagePrompts = authoredPrompts ?? DefaultPrompts();
        }

        public static LearningSupportStagePrompt[] DefaultPromptsForEditor()
        {
            return DefaultPrompts();
        }
#endif

        private static LearningSupportStagePrompt[] DefaultPrompts()
        {
            return new[]
            {
                Prompt(CrisisStage.Trigger,
                    "학생의 말만 듣지 말고 시선, 자세, 손의 긴장과 과제 회피 신호를 먼저 살펴보세요.",
                    "행동을 바로 교정하기보다 관찰된 어려움을 낮은 목소리로 확인합니다."),
                Prompt(CrisisStage.Escalation,
                    "목소리 크기, 움직임, 또래의 시선이 학생의 자극을 더 높이고 있는지 살펴보세요.",
                    "요구를 반복하기보다 자극을 낮추고 처리할 시간을 제공합니다."),
                Prompt(CrisisStage.Peak,
                    "지금은 설명보다 안전거리, 주변 학생, 다칠 가능성을 우선 확인할 단계입니다.",
                    "짧고 예측 가능한 언어로 안전을 확보하고 공개적인 대치를 피합니다."),
                Prompt(CrisisStage.Deescalation,
                    "호흡, 시선, 손의 힘이 느슨해지는 작은 진정 신호를 기다려 보세요.",
                    "서두르지 말고 학생이 통제감을 되찾을 수 있는 작은 선택권을 제공합니다."),
                Prompt(CrisisStage.Reconnection,
                    "학생이 자신의 경험을 설명하려는지, 여전히 방어하고 있는지 구분해 보세요.",
                    "원인을 추궁하기보다 학생이 이해받았다고 느끼는지 먼저 확인합니다."),
                Prompt(CrisisStage.InstructionalReentry,
                    "완전한 복귀를 요구하기 전에 학생이 감당할 수 있는 가장 작은 참여 단계를 찾으세요.",
                    "복귀 시간과 과제량을 구체적인 선택으로 제시하고 후속 지원을 연결합니다.")
            };
        }

        private static LearningSupportStagePrompt Prompt(
            CrisisStage stage,
            string observation,
            string principle)
        {
            return new LearningSupportStagePrompt
            {
                stage = stage,
                observationCue = observation,
                principleCue = principle
            };
        }
    }
}
