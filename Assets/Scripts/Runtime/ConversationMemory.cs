using System.Collections.Generic;
using System.Text;

namespace AdieLab.TeacherTraining
{
    public sealed class ConversationMemory
    {
        private readonly int capacity;
        private readonly Queue<Turn> turns = new Queue<Turn>();

        private readonly struct Turn
        {
            public readonly string teacher;
            public readonly string student;

            public Turn(string teacher, string student)
            {
                this.teacher = teacher;
                this.student = student;
            }
        }

        public ConversationMemory(int capacity)
        {
            this.capacity = capacity < 1 ? 1 : capacity;
        }

        public void Add(string teacher, string student)
        {
            turns.Enqueue(new Turn(teacher, student));
            while (turns.Count > capacity)
            {
                turns.Dequeue();
            }
        }

        public string BuildContext()
        {
            var builder = new StringBuilder();
            foreach (Turn turn in turns)
            {
                builder.Append("교사: ").AppendLine(turn.teacher);
                builder.Append("학생: ").AppendLine(turn.student);
            }

            return builder.ToString().TrimEnd();
        }
    }
}
