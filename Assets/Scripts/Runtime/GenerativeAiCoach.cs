using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class GenerativeAiCoach : MonoBehaviour, ILlmGateway
    {
        [SerializeField] private bool enabledForSession = true;
        [SerializeField] private string endpoint = "https:" + "/" + "/openrouter.ai/api/v1/chat/completions";
        [SerializeField] private string model = "openai/gpt-4o-mini";
        [SerializeField] private string apiKeyEnvironmentVariable = "OPENROUTER_API_KEY";
        [SerializeField, Range(1, 3)] private int requestAttempts = 2;
        [SerializeField, Range(10, 60)] private int requestTimeoutSeconds = 30;

        private OpenRouterRuntimeConfiguration runtimeConfiguration;
        private string ApiKey => Environment.GetEnvironmentVariable(apiKeyEnvironmentVariable);
        public OpenRouterRuntimeConfiguration RuntimeConfiguration =>
            runtimeConfiguration ??= OpenRouterRuntimeConfiguration.FromEnvironment();
        public bool IsConfigured => enabledForSession && !string.IsNullOrWhiteSpace(endpoint) &&
                                    !string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(ApiKey);
        public string ConfigurationLabel => IsConfigured ? $"LLM 연결 · {model}" : "로컬 대화 모드 · LLM 연결 대기";
        public string ModelId => model;
        public int PromptVersion => RuntimeConfiguration.PromptVersion;

        private void Awake()
        {
            runtimeConfiguration = OpenRouterRuntimeConfiguration.FromEnvironment();
            ApplyEnvironmentOverride("OPENROUTER_ENDPOINT", value => endpoint = value);
            ApplyEnvironmentOverride("OPENROUTER_MODEL", value => model = value);
        }

        public IEnumerator RequestFeedback(ScenarioBeat beat, TeacherResponseOption selected, Action<string> completed)
        {
            string prompt =
                "한국 초등학교 고학년 정서행동 지원 교사 코치로서 아래 대응을 평가하세요. " +
                "학생 존엄, 낮은 자극, 감정 인정, 선택권, 안전, 수업 복귀를 기준으로 2문장 이내 한국어 피드백을 작성하세요.\n" +
                $"상황: {beat.observation}\n학생 발화: {beat.studentLine}\n교사 대응: {selected.spokenResponse}";
            yield return Send(
                "당신은 근거기반 교사 공동조절 코치입니다.", prompt, LlmResponseContract.PlainText,
                completed, error => Debug.LogWarning($"Generative AI coach request failed: {error}"));
        }

        public IEnumerator RequestStudentTurn(
            StudentTurnRequest request,
            Action<StudentAgentTurn> completed,
            Action<string> failed)
        {
            if (request == null)
            {
                failed?.Invoke("학생 대화 요청이 비어 있습니다.");
                yield break;
            }

            StudentSafetyDecision inputSafety = StudentSafetyPolicy.Evaluate(request.teacherUtterance);
            if (inputSafety.Route == StudentTurnRoute.LocalFallback)
            {
                failed?.Invoke(inputSafety.Category.ToString());
                yield break;
            }

            const string system =
                "당신은 한국 초등학교 고학년 교실 시뮬레이션의 학생 민준입니다. 정서·행동 위기를 현실적으로 연기하되 " +
                "자해, 타해, 욕설을 과장하지 마세요. 교사의 말에 1~2문장 한국어로 답하세요. " +
                "표정 AU, 제스처, 정서가 서로 일치해야 합니다. dialogueSignals는 학생 관점에서 이번 교사 발화가 " +
                "경청, 압박, 선택권, 안전 우려, 수업 복귀 준비도에 미친 영향을 0~1로 나타냅니다.";
            AffectVector current = request.currentAffect;
            string prompt =
                $"시나리오 맥락: {request.scenarioContext}\n" +
                $"현재 위기 단계: {request.crisisStage}\n" +
                $"학생 페르소나 ID: {request.personaId}\n" +
                $"최근 대화 및 누적 상태:\n{request.conversationContext}\n" +
                $"현재 정서: valence={current.valence:F2}, arousal={current.arousal:F2}, dominance={current.dominance:F2}\n" +
                $"교사: {request.teacherUtterance}";
            string boardContext = BoardPresentationContext.BuildLlmContext();
            if (!string.IsNullOrWhiteSpace(boardContext))
            {
                prompt += $"\n{boardContext}";
            }

            string raw = null;
            string errorMessage = null;
            yield return Send(system, prompt, LlmResponseContract.StudentTurn,
                value => raw = value, error => errorMessage = error);
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                failed?.Invoke(errorMessage);
                yield break;
            }

            StudentTurnResolution resolution = StudentTurnBoundary.Normalize(raw);
            if (resolution.Outcome != StudentTurnOutcome.Accepted || resolution.Turn == null)
            {
                failed?.Invoke(resolution.Outcome == StudentTurnOutcome.Unsafe
                    ? resolution.SafetyCategory.ToString()
                    : "LLM 응답을 학생 정서 데이터로 해석하지 못했습니다.");
                yield break;
            }

            completed?.Invoke(resolution.Turn);
        }

        public IEnumerator RequestStudentTurn(
            string teacherUtterance,
            string conversationContext,
            AffectVector current,
            Action<StudentAgentTurn> completed,
            Action<string> failed)
        {
            yield return RequestStudentTurn(new StudentTurnRequest
            {
                teacherUtterance = teacherUtterance,
                conversationContext = conversationContext,
                currentAffect = current
            }, completed, failed);
        }

        public IEnumerator RequestTeacherRubric(
            TeacherRubricRequest request,
            Action<TeacherRubricResult> completed,
            Action<string> failed)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.teacherUtterance))
            {
                failed?.Invoke("교사 발화 평가 요청이 비어 있습니다.");
                yield break;
            }

            const string system =
                "당신은 정서행동 위기 학생 대응 교사교육 평가자입니다. 단일 발화를 학생 존엄(0), 낮은 자극(1), " +
                "감정 인정(2), 학생 선택권(3), 안전(4), 수업 복귀(5)의 여섯 차원에서 각각 0~3점으로 평가하세요. " +
                "각 차원을 정확히 한 번 포함하고, 관찰 가능한 짧은 근거와 다음 발화 개선안을 한국어로 제시하세요.";
            string prompt =
                $"시나리오: {request.scenarioContext}\n교사 발화: {request.teacherUtterance}\n학생 응답: {request.studentReply}";
            string boardContext = BoardPresentationContext.BuildLlmContext();
            if (!string.IsNullOrWhiteSpace(boardContext))
            {
                prompt += $"\n{boardContext}";
            }
            string raw = null;
            string errorMessage = null;
            yield return Send(system, prompt, LlmResponseContract.TeacherRubric,
                value => raw = value, error => errorMessage = error);
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                failed?.Invoke(errorMessage);
                yield break;
            }

            TeacherRubricResult rubric = ParseTeacherRubric(raw);
            if (rubric == null)
            {
                failed?.Invoke("LLM 루브릭 응답이 검증 계약을 충족하지 못했습니다.");
                yield break;
            }

            completed?.Invoke(rubric);
        }

        private IEnumerator Send(
            string system,
            string user,
            LlmResponseContract contract,
            Action<string> completed,
            Action<string> failed)
        {
            if (!IsConfigured)
            {
                failed?.Invoke("LLM 연결 정보가 설정되지 않았습니다.");
                yield break;
            }

            byte[] body = Encoding.UTF8.GetBytes(
                OpenRouterRequestFactory.Create(system, user, contract, model, RuntimeConfiguration));
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
                    if (LlmResponseParser.TryExtractContent(request.downloadHandler.text, out string content))
                    {
                        completed?.Invoke(content);
                    }
                    else
                    {
                        failed?.Invoke("지원되지 않는 LLM 응답 형식입니다.");
                    }
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
            failed?.Invoke(lastError ?? "LLM 요청을 완료하지 못했습니다.");
        }

        public static string CreateRequestJson(string system, string user, bool structuredOutput)
        {
            return CreateRequestJson(system, user, structuredOutput, null);
        }

        public static string CreateRequestJson(
            string system,
            string user,
            bool structuredOutput,
            OpenRouterRuntimeConfiguration configuration)
        {
            configuration ??= new OpenRouterRuntimeConfiguration(
                OpenRouterRuntimeConfiguration.DefaultMaxTokens,
                OpenRouterRuntimeConfiguration.DefaultTemperature,
                OpenRouterRuntimeConfiguration.DefaultPromptVersion);
            return OpenRouterRequestFactory.Create(
                system, user,
                structuredOutput ? LlmResponseContract.StudentTurn : LlmResponseContract.PlainText,
                "openai/gpt-4o-mini", configuration);
        }

        public static StudentAgentTurn ParseStudentTurn(string raw) => LlmResponseParser.ParseStudentTurn(raw);
        public static TeacherRubricResult ParseTeacherRubric(string raw) => LlmResponseParser.ParseTeacherRubric(raw);

        private static void ApplyEnvironmentOverride(string key, Action<string> assign)
        {
            string value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                assign(value);
            }
        }
    }
}
