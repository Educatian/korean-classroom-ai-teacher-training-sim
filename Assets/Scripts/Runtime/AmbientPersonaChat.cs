using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Lightweight persona-flavored replies for classmates outside the assessed
    /// crisis flow. Each classmate keeps a small per-session conversation state so
    /// repeated exchanges stay varied and follow simple keyword branches.
    /// </summary>
    public static class AmbientPersonaChat
    {
        private enum Archetype
        {
            Cheerful = 0,
            Shy = 1,
            Diligent = 2,
            Playful = 3
        }

        private sealed class ChatState
        {
            public int turn;
        }

        private static readonly Dictionary<string, ChatState> States = new Dictionary<string, ChatState>();

        private static readonly string[][] Greetings =
        {
            new[] { "안녕하세요 선생님! 오늘 급식 뭔지 아세요?", "선생님, 안녕하세요! 저 오늘 일찍 왔어요." },
            new[] { "…안녕하세요.", "네, 안녕하세요 선생님." },
            new[] { "안녕하세요 선생님. 오늘 배울 부분 미리 읽어왔어요.", "안녕하세요! 숙제 다 해왔어요." },
            new[] { "선생님 안녕하세요! 저 어제 진짜 웃긴 거 봤어요.", "안녕하세요! 쉬는 시간 언제예요?" }
        };

        private static readonly string[][] Concerns =
        {
            new[] { "네! 저는 완전 괜찮아요. 근데 짝꿍이 좀 힘들어 보여요.", "저는 좋아요! 선생님은요?" },
            new[] { "괜찮아요… 조금 졸리긴 해요.", "네… 괜찮은 것 같아요." },
            new[] { "네, 괜찮습니다. 다음 활동 준비할까요?", "괜찮아요. 오늘 진도 나가는 거 맞죠?" },
            new[] { "네! 완전 쌩쌩해요!", "괜찮아요! 근데 배고파요." }
        };

        private static readonly string[][] ClassTopics =
        {
            new[] { "이번 활동 재밌을 것 같아요! 모둠으로 해요?", "저 발표해도 돼요?" },
            new[] { "…어려우면 조금 천천히 해 주세요.", "네, 열심히 해 볼게요." },
            new[] { "지난 시간에 배운 거랑 이어지는 내용이죠? 공책 정리해 뒀어요.", "문제 다 풀면 다음 것 미리 해도 돼요?" },
            new[] { "우리 조가 이길 거예요!", "재밌는 걸로 해요, 선생님!" }
        };

        private static readonly string[][] Defaults =
        {
            new[] { "네, 선생님!", "알겠어요! 친구들한테도 말해 줄게요.", "히히, 네!" },
            new[] { "…네.", "네, 알겠어요.", "네…" },
            new[] { "네, 알겠습니다.", "네, 그렇게 할게요.", "확인했어요, 선생님." },
            new[] { "넵!", "오케이입니다!", "네네!" }
        };

        public static string ReplyFor(string studentName, string teacherUtterance)
        {
            Archetype archetype = ArchetypeFor(studentName);
            if (!States.TryGetValue(studentName, out ChatState state))
            {
                state = new ChatState();
                States[studentName] = state;
            }

            string utterance = teacherUtterance ?? string.Empty;
            string[] pool;
            if (utterance.Contains("안녕") || state.turn == 0)
            {
                pool = Greetings[(int)archetype];
            }
            else if (utterance.Contains("괜찮") || utterance.Contains("힘들") || utterance.Contains("기분"))
            {
                pool = Concerns[(int)archetype];
            }
            else if (utterance.Contains("수업") || utterance.Contains("활동") || utterance.Contains("공부") || utterance.Contains("발표"))
            {
                pool = ClassTopics[(int)archetype];
            }
            else
            {
                pool = Defaults[(int)archetype];
            }

            string reply = pool[state.turn % pool.Length];
            state.turn++;
            return reply;
        }

        public static string DisplayNameFor(string gameObjectName)
        {
            const string prefix = "Classmate_";
            if (!string.IsNullOrEmpty(gameObjectName) && gameObjectName.StartsWith(prefix))
            {
                return gameObjectName.Substring(prefix.Length);
            }

            return gameObjectName ?? "학생";
        }

        private static Archetype ArchetypeFor(string name)
        {
            int hash = 17;
            foreach (char character in name ?? string.Empty)
            {
                hash = hash * 31 + character;
            }

            return (Archetype)(Mathf.Abs(hash) % 4);
        }
    }
}
