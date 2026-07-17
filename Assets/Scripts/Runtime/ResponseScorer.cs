namespace AdieLab.TeacherTraining
{
    public static class ResponseScorer
    {
        public static int AddResponse(int currentScore, TeacherResponseOption option)
        {
            if (option == null)
            {
                return currentScore;
            }

            return currentScore + option.quality;
        }

        public static string GetLevel(int score, int completedBeats)
        {
            if (completedBeats <= 0)
            {
                return "관찰 중";
            }

            float ratio = score / (completedBeats * 3f);
            if (ratio >= 0.8f)
            {
                return "안정적 공동조절";
            }

            if (ratio >= 0.5f)
            {
                return "부분적 안정화";
            }

            return "대응 재구성 필요";
        }
    }
}
