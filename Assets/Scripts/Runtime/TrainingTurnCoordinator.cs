using System;

namespace AdieLab.TeacherTraining
{
    public sealed class TrainingTurnCoordinator
    {
        private readonly int beatCount;
        private readonly TrainingSessionState state;
        private readonly TrainingInputArbiter input;

        public TrainingTurnCoordinator(int beatCount)
        {
            if (beatCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(beatCount));
            }

            this.beatCount = beatCount;
            state = new TrainingSessionState();
            input = new TrainingInputArbiter(state);
        }

        public int BeatIndex { get; private set; }
        public TrainingPhase Phase => state.CurrentPhase;

        public bool Start()
        {
            return state.TryTransitionTo(TrainingPhase.AwaitingTeacherAction);
        }

        public bool TrySubmit(TeacherAction action, out TrainingRequestToken token)
        {
            return input.TrySubmit(action, out token);
        }

        public bool TryResolve(TrainingRequestToken token)
        {
            return input.TryComplete(token);
        }

        public bool TryAdvance(out bool complete)
        {
            complete = false;
            if (state.CurrentPhase != TrainingPhase.ReviewingFeedback)
            {
                return false;
            }

            input.InvalidateActiveRequest();
            if (BeatIndex + 1 >= beatCount)
            {
                complete = state.TryTransitionTo(TrainingPhase.Complete);
                return complete;
            }

            if (!state.TryTransitionTo(TrainingPhase.PresentingScenario))
            {
                return false;
            }

            BeatIndex++;
            return state.TryTransitionTo(TrainingPhase.AwaitingTeacherAction);
        }

        public bool TryPause()
        {
            return state.TryPause();
        }

        public bool TryResume()
        {
            return state.TryResume();
        }

        public bool TryAbort()
        {
            input.InvalidateActiveRequest();
            return state.TryAbort();
        }
    }
}
