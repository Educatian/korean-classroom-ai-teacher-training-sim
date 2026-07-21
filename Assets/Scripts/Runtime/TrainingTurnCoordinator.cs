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
            return TryAdvanceTo(BeatIndex + 1, out complete);
        }

        public bool TryAdvanceTo(int targetBeatIndex, out bool complete)
        {
            complete = false;
            if (state.CurrentPhase != TrainingPhase.ReviewingFeedback)
            {
                return false;
            }

            input.InvalidateActiveRequest();
            if (targetBeatIndex >= beatCount)
            {
                complete = state.TryTransitionTo(TrainingPhase.Complete);
                return complete;
            }

            if (targetBeatIndex < 0)
            {
                return false;
            }

            if (!state.TryTransitionTo(TrainingPhase.PresentingScenario))
            {
                return false;
            }

            BeatIndex = targetBeatIndex;
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

        public bool TryCancelPendingStudentResponse()
        {
            if (!state.TryCancelStudentResponse())
            {
                return false;
            }

            input.InvalidateActiveRequest();
            return true;
        }

        public bool TryAbort()
        {
            input.InvalidateActiveRequest();
            return state.TryAbort();
        }
    }
}
