using System;

namespace AdieLab.TeacherTraining
{
    public enum ScenarioTransitionReason
    {
        Hold = 0,
        SafetyOverride = 1,
        SupportiveDeescalation = 2,
        PressureEscalation = 3,
        ReadyForReentry = 4,
        MaximumTurnsReached = 5
    }

    public sealed class ScenarioTransitionContext
    {
        public ScenarioTransitionContext(
            int currentBeatIndex,
            int turnsInCurrentBeat,
            CrisisStage[] stages,
            DialogueSignals signals)
        {
            CurrentBeatIndex = currentBeatIndex;
            TurnsInCurrentBeat = turnsInCurrentBeat;
            Stages = stages ?? throw new ArgumentNullException(nameof(stages));
            Signals = signals ?? throw new ArgumentNullException(nameof(signals));
        }

        public int CurrentBeatIndex { get; }
        public int TurnsInCurrentBeat { get; }
        public CrisisStage[] Stages { get; }
        public DialogueSignals Signals { get; }
    }

    public readonly struct ScenarioTransitionDecision
    {
        public ScenarioTransitionDecision(int nextBeatIndex, ScenarioTransitionReason reason)
        {
            NextBeatIndex = nextBeatIndex;
            Reason = reason;
        }

        public int NextBeatIndex { get; }
        public ScenarioTransitionReason Reason { get; }
    }

    public static class ScenarioTransitionEngine
    {
        private const int MaximumTurnsPerBeat = 3;

        public static ScenarioTransitionDecision Select(ScenarioTransitionContext context)
        {
            if (context.CurrentBeatIndex < 0 || context.CurrentBeatIndex >= context.Stages.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(context));
            }

            DialogueSignals signals = context.Signals;
            if (signals.safetyConcern >= 0.65f)
            {
                return DecisionForStage(context, CrisisStage.Peak, ScenarioTransitionReason.SafetyOverride);
            }

            if (signals.readyForReentry >= 0.65f && signals.perceivedPressure <= 0.45f)
            {
                int reentry = FindForwardStage(context, CrisisStage.InstructionalReentry);
                if (reentry >= 0)
                {
                    return new ScenarioTransitionDecision(reentry, ScenarioTransitionReason.ReadyForReentry);
                }
            }

            if (signals.feltHeard >= 0.65f &&
                signals.choiceOffered >= 0.55f &&
                signals.perceivedPressure <= 0.4f)
            {
                return DecisionForStage(
                    context,
                    CrisisStage.Deescalation,
                    ScenarioTransitionReason.SupportiveDeescalation);
            }

            if (signals.perceivedPressure >= 0.7f)
            {
                return DecisionForStage(
                    context,
                    CrisisStage.Escalation,
                    ScenarioTransitionReason.PressureEscalation);
            }

            if (context.TurnsInCurrentBeat >= MaximumTurnsPerBeat)
            {
                return new ScenarioTransitionDecision(
                    Math.Min(context.CurrentBeatIndex + 1, context.Stages.Length - 1),
                    ScenarioTransitionReason.MaximumTurnsReached);
            }

            return new ScenarioTransitionDecision(context.CurrentBeatIndex, ScenarioTransitionReason.Hold);
        }

        private static ScenarioTransitionDecision DecisionForStage(
            ScenarioTransitionContext context,
            CrisisStage target,
            ScenarioTransitionReason reason)
        {
            int index = FindForwardStage(context, target);
            return index >= 0
                ? new ScenarioTransitionDecision(index, reason)
                : new ScenarioTransitionDecision(context.CurrentBeatIndex, ScenarioTransitionReason.Hold);
        }

        private static int FindForwardStage(ScenarioTransitionContext context, CrisisStage target)
        {
            for (int index = context.CurrentBeatIndex; index < context.Stages.Length; index++)
            {
                if (context.Stages[index] == target)
                {
                    return index;
                }
            }
            return -1;
        }
    }
}
