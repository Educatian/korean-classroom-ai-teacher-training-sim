namespace AdieLab.TeacherTraining
{
    public static class TrainingScenarioLibrary
    {
        public static ScenarioBeat[] BuildDefaultScenario()
        {
            return Build(TrainingSceneId.GeneralClassroom);
        }

        public static ScenarioBeat[] BuildCircleDiscussionScenario()
        {
            return Build(TrainingSceneId.CircleDiscussion);
        }

        private static ScenarioBeat[] Build(TrainingSceneId sceneId)
        {
            return TrainingScenarioCatalog
                .LoadDefault()
                .ScenarioFor(sceneId)
                .BuildRuntimeBeats();
        }
    }
}