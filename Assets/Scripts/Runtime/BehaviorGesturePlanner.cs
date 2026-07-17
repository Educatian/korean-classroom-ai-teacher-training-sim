namespace AdieLab.TeacherTraining
{
    public static class BehaviorGesturePlanner
    {
        private static readonly BehaviorGesture[] EscalatedDominant =
        {
            BehaviorGesture.Defiant,
            BehaviorGesture.DeskTap,
            BehaviorGesture.Protest,
            BehaviorGesture.PushAway,
            BehaviorGesture.Point,
            BehaviorGesture.DeskTap
        };

        private static readonly BehaviorGesture[] EscalatedWithdrawn =
        {
            BehaviorGesture.Withdraw,
            BehaviorGesture.Shield,
            BehaviorGesture.AvoidGaze,
            BehaviorGesture.Fidget,
            BehaviorGesture.Withdraw,
            BehaviorGesture.Shield
        };

        private static readonly BehaviorGesture[] Uneasy =
        {
            BehaviorGesture.Fidget,
            BehaviorGesture.AvoidGaze,
            BehaviorGesture.Shield,
            BehaviorGesture.Withdraw
        };

        private static readonly BehaviorGesture[] Regulated =
        {
            BehaviorGesture.Recover,
            BehaviorGesture.Listen,
            BehaviorGesture.Neutral,
            BehaviorGesture.Listen
        };

        public static BehaviorGesture Select(AffectVector state, int phase)
        {
            BehaviorGesture[] sequence;
            if (state.arousal >= 0.72f && state.valence <= -0.45f)
            {
                sequence = state.dominance >= 0.15f ? EscalatedDominant : EscalatedWithdrawn;
            }
            else if (state.valence <= -0.12f || state.arousal >= 0.45f)
            {
                sequence = Uneasy;
            }
            else
            {
                sequence = Regulated;
            }

            int index = phase % sequence.Length;
            return sequence[index < 0 ? index + sequence.Length : index];
        }
    }
}
