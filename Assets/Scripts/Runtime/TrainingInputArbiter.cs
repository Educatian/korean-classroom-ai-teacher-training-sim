using System;

namespace AdieLab.TeacherTraining
{
    public enum TeacherActionKind
    {
        Choice,
        FreeText
    }

    public enum TeacherActionSource
    {
        ResponseOption,
        TypedDialogue,
        VoiceDictation
    }

    public sealed class TeacherAction
    {
        public TeacherActionKind Kind { get; }
        public TeacherActionSource Source { get; }
        public int OptionIndex { get; }
        public string Utterance { get; }

        private TeacherAction(
            TeacherActionKind kind,
            TeacherActionSource source,
            int optionIndex,
            string utterance)
        {
            Kind = kind;
            Source = source;
            OptionIndex = optionIndex;
            Utterance = utterance ?? string.Empty;
        }

        public static TeacherAction FromChoice(int optionIndex)
        {
            return new TeacherAction(
                TeacherActionKind.Choice,
                TeacherActionSource.ResponseOption,
                optionIndex,
                string.Empty);
        }

        public static TeacherAction FromUtterance(string utterance)
        {
            return new TeacherAction(
                TeacherActionKind.FreeText,
                TeacherActionSource.TypedDialogue,
                -1,
                utterance);
        }

        public static TeacherAction FromDictation(string utterance)
        {
            return new TeacherAction(
                TeacherActionKind.FreeText,
                TeacherActionSource.VoiceDictation,
                -1,
                utterance);
        }
    }

    public readonly struct TrainingRequestToken : IEquatable<TrainingRequestToken>
    {
        public int Value { get; }
        public bool IsValid => Value > 0;

        public TrainingRequestToken(int value)
        {
            Value = value;
        }

        public bool Equals(TrainingRequestToken other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is TrainingRequestToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }

    public sealed class TrainingInputArbiter
    {
        private readonly TrainingSessionState state;
        private TrainingRequestToken activeToken;
        private int nextTokenValue;

        public TrainingInputArbiter(TrainingSessionState state)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public bool TrySubmit(TeacherAction action, out TrainingRequestToken token)
        {
            token = default;
            if (action == null ||
                state.CurrentPhase != TrainingPhase.AwaitingTeacherAction ||
                !state.TryTransitionTo(TrainingPhase.AwaitingStudentResponse))
            {
                return false;
            }

            activeToken = new TrainingRequestToken(++nextTokenValue);
            token = activeToken;
            return true;
        }

        public bool TryComplete(TrainingRequestToken token)
        {
            if (!token.IsValid ||
                !token.Equals(activeToken) ||
                state.CurrentPhase != TrainingPhase.AwaitingStudentResponse ||
                !state.TryTransitionTo(TrainingPhase.ReviewingFeedback))
            {
                return false;
            }

            activeToken = default;
            return true;
        }

        public void InvalidateActiveRequest()
        {
            activeToken = default;
        }
    }
}
