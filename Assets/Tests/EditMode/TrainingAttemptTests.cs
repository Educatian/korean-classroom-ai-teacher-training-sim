using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class TrainingAttemptTests
    {
        [SetUp]
        public void ResetTracker()
        {
            TrainingAttemptTracker.Reset();
        }

        [Test]
        public void AttemptTracker_CountsPerSceneIndependently()
        {
            Assert.That(TrainingAttemptTracker.BeginAttempt(TrainingSceneId.GeneralClassroom), Is.EqualTo(1));
            Assert.That(TrainingAttemptTracker.BeginAttempt(TrainingSceneId.GeneralClassroom), Is.EqualTo(2));
            Assert.That(TrainingAttemptTracker.BeginAttempt(TrainingSceneId.RecoveryRoom), Is.EqualTo(1));
            Assert.That(TrainingAttemptTracker.BeginAttempt(TrainingSceneId.GeneralClassroom), Is.EqualTo(3));
        }

        [Test]
        public void Shuffler_FirstAttemptKeepsAuthoredOrder()
        {
            ScenarioBeat[] beats = BuildBeats();
            string[] before = OptionTexts(beats);

            TeacherResponseOptionShuffler.Shuffle(beats, 1, TrainingSceneId.GeneralClassroom);

            Assert.That(OptionTexts(beats), Is.EqualTo(before));
        }

        [Test]
        public void Shuffler_RetryReordersButPreservesTheOptionSet()
        {
            ScenarioBeat[] beats = BuildBeats();
            string[] before = OptionTexts(beats);

            TeacherResponseOptionShuffler.Shuffle(beats, 2, TrainingSceneId.GeneralClassroom);
            string[] after = OptionTexts(beats);

            Assert.That(after, Is.EquivalentTo(before));
            Assert.That(after, Is.Not.EqualTo(before));
        }

        [Test]
        public void Shuffler_SameAttemptAndSceneIsReproducible()
        {
            ScenarioBeat[] first = BuildBeats();
            ScenarioBeat[] second = BuildBeats();

            TeacherResponseOptionShuffler.Shuffle(first, 2, TrainingSceneId.Schoolyard);
            TeacherResponseOptionShuffler.Shuffle(second, 2, TrainingSceneId.Schoolyard);

            Assert.That(OptionTexts(first), Is.EqualTo(OptionTexts(second)));
        }

        private static ScenarioBeat[] BuildBeats()
        {
            var beats = new ScenarioBeat[2];
            for (int beatIndex = 0; beatIndex < beats.Length; beatIndex++)
            {
                var options = new TeacherResponseOption[4];
                for (int index = 0; index < options.Length; index++)
                {
                    options[index] = new TeacherResponseOption
                    {
                        spokenResponse = "beat" + beatIndex + "-option" + index
                    };
                }

                beats[beatIndex] = new ScenarioBeat { title = "b" + beatIndex, options = options };
            }

            return beats;
        }

        private static string[] OptionTexts(ScenarioBeat[] beats)
        {
            var texts = new System.Collections.Generic.List<string>();
            foreach (ScenarioBeat beat in beats)
            {
                foreach (TeacherResponseOption option in beat.options)
                {
                    texts.Add(option.spokenResponse);
                }
            }

            return texts.ToArray();
        }
    }
}
