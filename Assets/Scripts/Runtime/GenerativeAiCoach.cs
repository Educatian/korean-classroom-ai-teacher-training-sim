using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class GenerativeAiCoach : MonoBehaviour
    {
        [SerializeField] private bool enabledForSession = true;
        [SerializeField] private string endpoint = "https:" + "/" + "/openrouter.ai/api/v1/chat/completions";
        [SerializeField] private string model = "openai/gpt-4o-mini";
        [SerializeField] private string apiKeyEnvironmentVariable = "OPENROUTER_API_KEY";
        [SerializeField, Range(1, 3)] private int requestAttempts = 2;
        [SerializeField, Range(10, 60)] private int requestTimeoutSeconds = 30;

        [Serializable]
        private sealed class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private sealed class PlainChatRequest
        {
            public string model;
            public ChatMessage[] messages;
        }

        [Serializable]
        private sealed class JsonChatRequest
        {
            public string model;
            public ChatMessage[] messages;
            public ResponseFormat response_format;
        }

        [Serializable]
        private sealed class ResponseFormat
        {
            public string type = "json_object";
        }

        [Serializable]
        private sealed class ChatChoice
        {
            public ChatMessage message;
        }

        [Serializable]
        private sealed class ChatResponse
        {
            public ChatChoice[] choices;
        }

        public bool IsConfigured => enabledForSession &&
                                    !string.IsNullOrWhiteSpace(endpoint) &&
                                    !string.IsNullOrWhiteSpace(model) &&
                                    !string.IsNullOrWhiteSpace(ApiKey);

        public string ConfigurationLabel => IsConfigured ? $"LLM 연결 · {model}" : "로컬 대화 모드 · OpenRouter 연결 대기";
        private string ApiKey => Environment.GetEnvironmentVariable(apiKeyEnvironmentVariable);

        private void Awake()
        {
            string endpointOverride = Environment.GetEnvironmentVariable("OPENROUTER_ENDPOINT");
            string modelOverride = Environment.GetEnvironmentVariable("OPENROUTER_MODEL");
            if (!string.IsNullOrWhiteSpace(endpointOverride))
            {
                endpoint = endpointOverride;
            }

            if (!string.IsNullOrWhiteSpace(modelOverride))
            {
                model = modelOverride;
            }
        }

        public IEnumerator RequestFeedback(
            ScenarioBeat beat,
            TeacherResponseOption selected,
            Action<string> completed)
        {
            string prompt =
                "한국 초등학교 고학년 정서행동 지원 교사 코치로서 아래 대응을 평가하세요. " +
                "학생의 존엄, 낮은 자극, 감정 인정, 선택권, 안전, 수업 복귀를 기준으로 2문장 이내의 구체적인 피드백을 한국어로 작성하세요.\n" +
                $"상황: {beat.observation}\n학생 발화: {beat.studentLine}\n교사 대응: {selected.spokenResponse}";
            yield return Send(
                "당신은 근거기반 교사 공동조절 코치입니다.",
                prompt,
                false,
                completed,
                error => Debug.LogWarning($"Generative AI coach request failed: {error}"));
        }

        public IEnumerator RequestStudentTurn(
            string teacherUtterance,
            string conversationContext,
            AffectVector current,
            Action<StudentAgentTurn> completed,
            Action<string> failed)
        {
            string system =
                "당신은 한국 초등학교 고학년 교실 시뮬레이션의 학생 민준입니다. 정서·행동 위기 상황을 사실적으로 연기하되 " +
                "자해, 타해, 욕설을 과장하지 마세요. 교사의 말에 1~2문장 한국어로 답하고 현재 정서를 JSON 하나로만 반환하세요. " +
                "JSON 객체만 반환하세요. 스키마: {\"studentReply\":\"...\",\"valence\":-1.0,\"arousal\":0.0," +
                "\"dominance\":-1.0,\"gesture\":\"Neutral|AvoidGaze|Fidget|Withdraw|Protest|Defiant|DeskTap|Shield|Point|PushAway|Listen|Recover\"," +
                "\"actionUnits\":{\"au1\":0.0,\"au2\":0.0,\"au4\":0.0,\"au5\":0.0,\"au6\":0.0,\"au7\":0.0,\"au9\":0.0," +
                "\"au12\":0.0,\"au15\":0.0,\"au17\":0.0,\"au20\":0.0,\"au23\":0.0,\"au24\":0.0,\"au25\":0.0,\"au26\":0.0}}. " +
                "모든 AU 수치는 0~1이며 표정과 제스처는 응답 정서에 일치해야 합니다.";
            string prompt =
                $"최근 대화:\n{conversationContext}\n현재 정서: valence={current.valence:F2}, arousal={current.arousal:F2}, dominance={current.dominance:F2}\n" +
                $"교사: {teacherUtterance}";

            string raw = null;
            string errorMessage = null;
            yield return Send(system, prompt, true, value => raw = value, error => errorMessage = error);
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                failed?.Invoke(errorMessage);
                yield break;
            }

            StudentAgentTurn turn = ParseStudentTurn(raw);
            if (turn == null)
            {
                failed?.Invoke("LLM 응답을 학생 정서 데이터로 해석하지 못했습니다.");
                yield break;
            }

            turn.valence = Mathf.Clamp(turn.valence, -1f, 1f);
            turn.arousal = Mathf.Clamp01(turn.arousal);
            turn.dominance = Mathf.Clamp(turn.dominance, -1f, 1f);
            ClampActionUnits(turn.actionUnits);
            completed?.Invoke(turn);
        }

        private IEnumerator Send(string system, string user, bool structuredOutput, Action<string> completed, Action<string> failed)
        {
            if (!IsConfigured)
            {
                failed?.Invoke("OPENROUTER_API_KEY가 설정되지 않았습니다.");
                yield break;
            }

            byte[] body = Encoding.UTF8.GetBytes(CreateRequestJson(system, user, structuredOutput, model));
            string lastError = null;
            for (int attempt = 0; attempt < requestAttempts; attempt++)
            {
                using UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {ApiKey}");
                request.SetRequestHeader("X-Title", "Korean Teacher Response Training");
                request.timeout = requestTimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ChatResponse response = ParseChatResponse(request.downloadHandler.text);
                    if (response?.choices == null || response.choices.Length == 0 ||
                        response.choices[0] == null || response.choices[0].message == null)
                    {
                        failed?.Invoke("지원되지 않는 LLM 응답 형식입니다.");
                        yield break;
                    }

                    completed?.Invoke(response.choices[0].message.content);
                    yield break;
                }

                lastError = $"{request.responseCode} {request.error}";
                bool transient = request.responseCode == 0 || request.responseCode == 408 ||
                                 request.responseCode == 429 || request.responseCode >= 500;
                if (!transient || attempt + 1 >= requestAttempts)
                {
                    break;
                }

                yield return new WaitForSecondsRealtime(0.5f * (attempt + 1));
            }

            failed?.Invoke(lastError ?? "OpenRouter 요청을 완료하지 못했습니다.");
        }

        public static string CreateRequestJson(string system, string user, bool structuredOutput)
        {
            return CreateRequestJson(system, user, structuredOutput, "openai/gpt-4o-mini");
        }

        private static string CreateRequestJson(string system, string user, bool structuredOutput, string requestModel)
        {
            ChatMessage[] messages =
            {
                new ChatMessage { role = "system", content = system },
                new ChatMessage { role = "user", content = user }
            };
            if (!structuredOutput)
            {
                return JsonUtility.ToJson(new PlainChatRequest { model = requestModel, messages = messages });
            }

            return JsonUtility.ToJson(new JsonChatRequest
            {
                model = requestModel,
                messages = messages,
                response_format = new ResponseFormat()
            });
        }

        private static ChatResponse ParseChatResponse(string raw)
        {
            try
            {
                return JsonUtility.FromJson<ChatResponse>(raw);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static StudentAgentTurn ParseStudentTurn(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return null;
            }

            string json = raw.Substring(start, end - start + 1);

            try
            {
                StudentAgentTurn turn = new StudentAgentTurn
                {
                    valence = float.NaN,
                    arousal = float.NaN,
                    dominance = float.NaN
                };
                JsonUtility.FromJsonOverwrite(json, turn);
                if (turn == null || string.IsNullOrWhiteSpace(turn.studentReply) ||
                    string.IsNullOrWhiteSpace(turn.gesture) ||
                    float.IsNaN(turn.valence) || float.IsInfinity(turn.valence) ||
                    float.IsNaN(turn.arousal) || float.IsInfinity(turn.arousal) ||
                    float.IsNaN(turn.dominance) || float.IsInfinity(turn.dominance) ||
                    !Enum.TryParse(turn.gesture, true, out BehaviorGesture _))
                {
                    return null;
                }

                return turn;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static void ClampActionUnits(ActionUnitDirective units)
        {
            if (units == null)
            {
                return;
            }

            units.au1 = Mathf.Clamp01(units.au1);
            units.au2 = Mathf.Clamp01(units.au2);
            units.au4 = Mathf.Clamp01(units.au4);
            units.au5 = Mathf.Clamp01(units.au5);
            units.au6 = Mathf.Clamp01(units.au6);
            units.au7 = Mathf.Clamp01(units.au7);
            units.au9 = Mathf.Clamp01(units.au9);
            units.au12 = Mathf.Clamp01(units.au12);
            units.au15 = Mathf.Clamp01(units.au15);
            units.au17 = Mathf.Clamp01(units.au17);
            units.au20 = Mathf.Clamp01(units.au20);
            units.au23 = Mathf.Clamp01(units.au23);
            units.au24 = Mathf.Clamp01(units.au24);
            units.au25 = Mathf.Clamp01(units.au25);
            units.au26 = Mathf.Clamp01(units.au26);
        }

    }
}
