using System;

namespace AdieLab.TeacherTraining
{
    public readonly struct LearningSupportDecision
    {
        public LearningSupportDecision(
            LearningSupportLevel level,
            LearningSupportTrigger trigger,
            bool automatic,
            long idleMilliseconds,
            int requestCount,
            int consecutiveMisses)
        {
            Level = level;
            Trigger = trigger;
            Automatic = automatic;
            IdleMilliseconds = Math.Max(0L, idleMilliseconds);
            RequestCount = Math.Max(0, requestCount);
            ConsecutiveMisses = Math.Max(0, consecutiveMisses);
        }

        public LearningSupportLevel Level { get; }
        public LearningSupportTrigger Trigger { get; }
        public bool Automatic { get; }
        public long IdleMilliseconds { get; }
        public int RequestCount { get; }
        public int ConsecutiveMisses { get; }
    }

    public sealed class LearningSupportStateMachine
    {
        private readonly LearningSupportPolicy policy;
        private double beatStartedAtSeconds;
        private int attemptNumber = 1;
        private int requestCount;
        private int consecutiveMisses;
        private bool inactivitySupportIssued;
        private LearningSupportLevel highestLevelBeforeAction;
        private LearningSupportDecision latestDecision;

        public LearningSupportStateMachine(LearningSupportPolicy policy)
        {
            this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public LearningSupportLevel HighestLevelBeforeAction => highestLevelBeforeAction;
        public int ConsecutiveMisses => consecutiveMisses;
        public int RequestCount => requestCount;

        public void BeginBeat(int authoredAttemptNumber, double nowSeconds)
        {
            attemptNumber = Math.Max(1, authoredAttemptNumber);
            beatStartedAtSeconds = Math.Max(0d, nowSeconds);
            inactivitySupportIssued = false;
            highestLevelBeforeAction = LearningSupportLevel.Hidden;
            latestDecision = default;
        }

        public bool TryTick(double nowSeconds, out LearningSupportDecision decision)
        {
            decision = default;
            if (inactivitySupportIssued)
            {
                return false;
            }

            double idleSeconds = Math.Max(0d, nowSeconds - beatStartedAtSeconds);
            if (idleSeconds < policy.InactivityThresholdForAttempt(attemptNumber))
            {
                return false;
            }

            inactivitySupportIssued = true;
            LearningSupportLevel level = ClampAutomatic(LearningSupportLevel.ObservationCue);
            if (level == LearningSupportLevel.Hidden || level <= highestLevelBeforeAction)
            {
                return false;
            }

            decision = CreateDecision(level, LearningSupportTrigger.Inactivity, true, idleSeconds);
            Remember(decision, beforeAction: true);
            return true;
        }

        public LearningSupportDecision RequestManual(double nowSeconds, bool afterAction)
        {
            requestCount++;
            double idleSeconds = Math.Max(0d, nowSeconds - beatStartedAtSeconds);
            LearningSupportLevel level;
            LearningSupportTrigger trigger;
            if (afterAction)
            {
                level = LearningSupportLevel.PostActionContrast;
                trigger = LearningSupportTrigger.PostActionRequest;
            }
            else
            {
                level = highestLevelBeforeAction < LearningSupportLevel.ObservationCue
                    ? LearningSupportLevel.ObservationCue
                    : LearningSupportLevel.Principle;
                trigger = LearningSupportTrigger.ManualRequest;
            }

            LearningSupportDecision decision = CreateDecision(level, trigger, false, idleSeconds);
            Remember(decision, beforeAction: !afterAction);
            return decision;
        }

        public bool RecordOutcome(int quality, double nowSeconds, out LearningSupportDecision decision)
        {
            decision = default;
            if (quality <= policy.LowQualityThreshold)
            {
                consecutiveMisses++;
            }
            else
            {
                consecutiveMisses = 0;
                return false;
            }

            if (consecutiveMisses < policy.RepeatedMissesForPrinciple)
            {
                return false;
            }

            LearningSupportLevel level = ClampAutomatic(LearningSupportLevel.Principle);
            if (level == LearningSupportLevel.Hidden || level <= highestLevelBeforeAction)
            {
                return false;
            }

            double idleSeconds = Math.Max(0d, nowSeconds - beatStartedAtSeconds);
            decision = CreateDecision(level, LearningSupportTrigger.RepeatedMissedSignal, true, idleSeconds);
            Remember(decision, beforeAction: false);
            return true;
        }

        public LearningSupportDecision CurrentActionSnapshot()
        {
            if (highestLevelBeforeAction == LearningSupportLevel.Hidden)
            {
                return default;
            }

            return new LearningSupportDecision(
                highestLevelBeforeAction,
                latestDecision.Trigger,
                latestDecision.Automatic,
                latestDecision.IdleMilliseconds,
                requestCount,
                consecutiveMisses);
        }

        private LearningSupportLevel ClampAutomatic(LearningSupportLevel requested)
        {
            LearningSupportLevel maximum = policy.AutomaticMaximumForAttempt(attemptNumber);
            return requested > maximum ? maximum : requested;
        }

        private LearningSupportDecision CreateDecision(
            LearningSupportLevel level,
            LearningSupportTrigger trigger,
            bool automatic,
            double idleSeconds)
        {
            return new LearningSupportDecision(
                level,
                trigger,
                automatic,
                (long)Math.Round(idleSeconds * 1000d),
                requestCount,
                consecutiveMisses);
        }

        private void Remember(LearningSupportDecision decision, bool beforeAction)
        {
            latestDecision = decision;
            if (beforeAction && decision.Level > highestLevelBeforeAction)
            {
                highestLevelBeforeAction = decision.Level;
            }
        }
    }
}
