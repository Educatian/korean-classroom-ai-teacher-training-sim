using System;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public static class StudentTurnPerformanceNormalizer
    {
        public static StudentAgentTurn Normalize(StudentAgentTurn turn)
        {
            if (turn == null)
            {
                throw new ArgumentNullException(nameof(turn));
            }

            turn.valence = Mathf.Clamp(turn.valence, -1f, 1f);
            turn.arousal = Mathf.Clamp01(turn.arousal);
            turn.dominance = Mathf.Clamp(turn.dominance, -1f, 1f);
            turn.dialogueSignals ??= DialogueSignals.Neutral;
            turn.actionUnits ??= new ActionUnitDirective();

            BehaviorGesture gesture = Enum.TryParse(turn.gesture, true, out BehaviorGesture parsed)
                ? parsed
                : BehaviorGesture.Fidget;
            DialogueSignals signals = turn.dialogueSignals;

            if (signals.readyForReentry >= 0.65f && signals.perceivedPressure <= 0.45f)
            {
                gesture = BehaviorGesture.Recover;
            }
            else if (signals.feltHeard >= 0.65f && turn.arousal <= 0.55f)
            {
                gesture = BehaviorGesture.Listen;
            }
            else if (turn.valence <= -0.5f && turn.arousal >= 0.65f)
            {
                gesture = turn.dominance >= 0.15f
                    ? BehaviorGesture.Protest
                    : BehaviorGesture.Withdraw;
            }

            turn.gesture = gesture.ToString();
            ApplyCoherentActionUnits(turn, gesture);
            return turn;
        }

        private static void ApplyCoherentActionUnits(StudentAgentTurn turn, BehaviorGesture gesture)
        {
            ActionUnitDirective units = turn.actionUnits;
            Clamp(units);

            if (turn.valence <= -0.4f)
            {
                units.au12 = Mathf.Min(units.au12, 0.18f);
                units.au4 = Mathf.Max(units.au4, Mathf.Lerp(0.32f, 0.72f, turn.arousal));
                units.au7 = Mathf.Max(units.au7, 0.28f + turn.arousal * 0.32f);
            }
            else if (turn.valence >= 0.1f)
            {
                units.au4 = Mathf.Min(units.au4, 0.22f);
                units.au15 = Mathf.Min(units.au15, 0.16f);
                units.au6 = Mathf.Max(units.au6, 0.22f + turn.valence * 0.24f);
                units.au12 = Mathf.Max(units.au12, 0.28f + turn.valence * 0.34f);
            }

            if (gesture == BehaviorGesture.Withdraw || gesture == BehaviorGesture.Shield)
            {
                units.au1 = Mathf.Max(units.au1, 0.34f);
                units.au15 = Mathf.Max(units.au15, 0.28f);
                units.au20 = Mathf.Max(units.au20, 0.18f);
            }
            else if (gesture == BehaviorGesture.Protest || gesture == BehaviorGesture.Defiant)
            {
                units.au4 = Mathf.Max(units.au4, 0.58f);
                units.au7 = Mathf.Max(units.au7, 0.48f);
                units.au23 = Mathf.Max(units.au23, 0.38f);
                units.au24 = Mathf.Max(units.au24, 0.3f);
            }
            else if (gesture == BehaviorGesture.Listen || gesture == BehaviorGesture.Recover)
            {
                units.au4 = Mathf.Min(units.au4, 0.25f);
                units.au7 = Mathf.Min(units.au7, 0.3f);
                units.au23 = Mathf.Min(units.au23, 0.2f);
            }

            Clamp(units);
        }

        private static void Clamp(ActionUnitDirective units)
        {
            units.au1 = Mathf.Clamp01(units.au1);
            units.au2 = Mathf.Clamp01(units.au2);
            units.au4 = Mathf.Clamp01(units.au4);
            units.au5 = Mathf.Clamp01(units.au5);
            units.au6 = Mathf.Clamp01(units.au6);
            units.au7 = Mathf.Clamp01(units.au7);
            units.au9 = Mathf.Clamp01(units.au9);
            units.au12 = Mathf.Clamp01(units.au12);
            units.au15 = Mathf.Clamp01(units.au15);
            units.au17 = Mathf.Clamp01(units.au17);
            units.au20 = Mathf.Clamp01(units.au20);
            units.au23 = Mathf.Clamp01(units.au23);
            units.au24 = Mathf.Clamp01(units.au24);
            units.au25 = Mathf.Clamp01(units.au25);
            units.au26 = Mathf.Clamp01(units.au26);
        }
    }
}
