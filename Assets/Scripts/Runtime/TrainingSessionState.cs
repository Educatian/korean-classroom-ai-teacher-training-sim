namespace AdieLab.TeacherTraining
{
    public enum TrainingPhase
    {
        PresentingScenario,
        AwaitingTeacherAction,
        AwaitingStudentResponse,
        ReviewingFeedback,
        Paused,
        Complete,
        Aborted
    }

    public sealed class TrainingSessionState
    {
        private TrainingPhase phaseBeforePause;

        public TrainingPhase CurrentPhase { get; private set; }

        public TrainingSessionState()
        {
            CurrentPhase = TrainingPhase.PresentingScenario;
            phaseBeforePause = CurrentPhase;
        }

        public bool TryTransitionTo(TrainingPhase next)
        {
            if (CurrentPhase == next ||
                CurrentPhase == TrainingPhase.Paused ||
                CurrentPhase == TrainingPhase.Complete ||
                CurrentPhase == TrainingPhase.Aborted)
            {
                return false;
            }

            bool allowed = CurrentPhase switch
            {
                TrainingPhase.PresentingScenario =>
                    next == TrainingPhase.AwaitingTeacherAction ||
                    next == TrainingPhase.Complete,
                TrainingPhase.AwaitingTeacherAction =>
                    next == TrainingPhase.AwaitingStudentResponse,
                TrainingPhase.AwaitingStudentResponse =>
                    next == TrainingPhase.ReviewingFeedback,
                TrainingPhase.ReviewingFeedback =>
                    next == TrainingPhase.PresentingScenario ||
                    next == TrainingPhase.Complete,
                _ => false
            };
            if (!allowed)
            {
                return false;
            }

            CurrentPhase = next;
            return true;
        }

        public bool TryPause()
        {
            if (CurrentPhase == TrainingPhase.Paused ||
                CurrentPhase == TrainingPhase.Complete ||
                CurrentPhase == TrainingPhase.Aborted)
            {
                return false;
            }

            phaseBeforePause = CurrentPhase;
            CurrentPhase = TrainingPhase.Paused;
            return true;
        }

        public bool TryResume()
        {
            if (CurrentPhase != TrainingPhase.Paused)
            {
                return false;
            }

            CurrentPhase = phaseBeforePause;
            return true;
        }

        /// <summary>
        /// Cancels a pending student response and returns control to the teacher.
        /// Used when a pause outlived an in-flight response request: the reply was
        /// dropped while paused, so waiting again would soft-lock the session.
        /// </summary>
        public bool TryCancelStudentResponse()
        {
            if (CurrentPhase != TrainingPhase.AwaitingStudentResponse)
            {
                return false;
            }

            CurrentPhase = TrainingPhase.AwaitingTeacherAction;
            return true;
        }

        public bool TryAbort()
        {
            if (CurrentPhase == TrainingPhase.Complete ||
                CurrentPhase == TrainingPhase.Aborted)
            {
                return false;
            }

            CurrentPhase = TrainingPhase.Aborted;
            return true;
        }
    }
}
