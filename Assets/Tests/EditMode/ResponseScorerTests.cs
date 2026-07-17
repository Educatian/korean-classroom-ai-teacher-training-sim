using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class ResponseScorerTests
    {
        [Test]
        public void AddResponse_AddsAuthoredQuality_WhenOptionExists()
        {
            TeacherResponseOption option = new TeacherResponseOption { quality = 3 };

            int result = ResponseScorer.AddResponse(2, option);

            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void GetLevel_ReturnsStableCoRegulation_WhenRatioIsHigh()
        {
            string result = ResponseScorer.GetLevel(8, 3);

            Assert.That(result, Is.EqualTo("안정적 공동조절"));
        }

        [Test]
        public void AffectVector_MoveTowards_AdvancesAllDimensionsWithoutOvershoot()
        {
            AffectVector current = new AffectVector(-0.8f, 0.9f, -0.6f);
            AffectVector target = new AffectVector(0.2f, 0.3f, 0.1f);

            AffectVector result = AffectVector.MoveTowards(current, target, 0.25f);

            Assert.That(result.valence, Is.EqualTo(-0.55f).Within(0.001f));
            Assert.That(result.arousal, Is.EqualTo(0.65f).Within(0.001f));
            Assert.That(result.dominance, Is.EqualTo(-0.35f).Within(0.001f));
        }

        [Test]
        public void ParseStudentTurn_ExtractsStructuredAffectFromWrappedJson()
        {
            const string raw = "```json\n{\"studentReply\":\"잠깐 쉬고 싶어요.\",\"valence\":-0.2,\"arousal\":0.4,\"dominance\":-0.1,\"gesture\":\"Recover\"}\n```";

            StudentAgentTurn result = GenerativeAiCoach.ParseStudentTurn(raw);

            Assert.That(result.studentReply, Is.EqualTo("잠깐 쉬고 싶어요."));
            Assert.That(result.arousal, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(result.gesture, Is.EqualTo("Recover"));
        }

        [Test]
        public void ParseStudentTurn_RejectsReplyWhenRequiredAffectFieldsAreMissing()
        {
            const string raw = "{\"studentReply\":\"알겠어요.\",\"gesture\":\"Recover\"}";

            StudentAgentTurn result = GenerativeAiCoach.ParseStudentTurn(raw);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ParseStudentTurn_RejectsUnknownGesture()
        {
            const string raw = "{\"studentReply\":\"알겠어요.\",\"valence\":0.2,\"arousal\":0.3,\"dominance\":0.0,\"gesture\":\"Teleport\"}";

            StudentAgentTurn result = GenerativeAiCoach.ParseStudentTurn(raw);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ParseStudentTurn_RejectsSingleMissingNumericFieldEvenWhenNameAppearsInReply()
        {
            const string raw = "{\"studentReply\":\"valence라는 말을 했어요.\",\"arousal\":0.3,\"dominance\":0.0,\"gesture\":\"Listen\"}";

            StudentAgentTurn result = GenerativeAiCoach.ParseStudentTurn(raw);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ParseStudentTurn_RejectsMalformedJson()
        {
            const string raw = "{\"studentReply\":\"알겠어요.\",\"valence\":0.2";

            StudentAgentTurn result = GenerativeAiCoach.ParseStudentTurn(raw);

            Assert.That(result, Is.Null);
        }
    }
}
