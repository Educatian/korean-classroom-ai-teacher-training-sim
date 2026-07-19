using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AdieLab.TeacherTraining
{
    [CreateAssetMenu(
        fileName = "SecureLlmProxySettings",
        menuName = "Teacher Training/Secure LLM Proxy Settings",
        order = 40)]
    public sealed class SecureLlmProxySettings : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/SecureLlmProxySettings";

        [SerializeField] private string endpoint;
        [SerializeField] private string clientId = "teacher-training-unity";
        [SerializeField, Range(10, 60)] private int timeoutSeconds = 30;

        public string Endpoint => endpoint;
        public string ClientId => clientId;
        public int TimeoutSeconds => timeoutSeconds;
        public bool IsValid => LlmDeploymentPolicy.IsEndpointAllowed(LlmTransportMode.SecureProxy, endpoint);
    }

    [DisallowMultipleComponent]
    public sealed class SecureProxyLlmGateway : MonoBehaviour, ILlmGateway
    {
        [SerializeField] private SecureLlmProxySettings settings;

        private string sessionAuthorization;
        public bool IsConfigured => settings != null && settings.IsValid &&
                                    !string.IsNullOrWhiteSpace(sessionAuthorization);
        public string ConfigurationLabel => IsConfigured
            ? "보안 LLM 프록시 연결"
            : "로컬 대화 모드 · 보안 프록시 세션 대기";
        public string ModelId => "secure-proxy/managed";
        public int PromptVersion => OpenRouterRuntimeConfiguration.DefaultPromptVersion;

        private void Awake()
        {
            if (settings == null)
            {
                settings = Resources.Load<SecureLlmProxySettings>(SecureLlmProxySettings.DefaultResourcePath);
            }
        }

        public void SetSessionAuthorization(string bearerToken)
        {
            sessionAuthorization = string.IsNullOrWhiteSpace(bearerToken) ? null : bearerToken.Trim();
        }

        public IEnumerator RequestStudentTurn(
            StudentTurnRequest request,
            Action<StudentAgentTurn> completed,
            Action<string> failed)
        {
            var envelope = CreateEnvelope();
            envelope.studentTurn = request;
            LlmProxyTurnResponse response = null;
            string error = null;
            yield return Send("student-turn", envelope, value => response = value, value => error = value);
            if (!string.IsNullOrWhiteSpace(error) || response?.studentTurn == null)
            {
                failed?.Invoke(error ?? "보안 프록시 학생 응답이 비어 있습니다.");
                yield break;
            }

            StudentTurnResolution resolution = StudentTurnBoundary.Normalize(JsonUtility.ToJson(response.studentTurn));
            if (resolution.Outcome != StudentTurnOutcome.Accepted)
            {
                failed?.Invoke("보안 프록시 학생 응답이 검증 계약을 충족하지 못했습니다.");
                yield break;
            }
            completed?.Invoke(resolution.Turn);
        }

        public IEnumerator RequestTeacherRubric(
            TeacherRubricRequest request,
            Action<TeacherRubricResult> completed,
            Action<string> failed)
        {
            var envelope = CreateEnvelope();
            envelope.teacherRubric = request;
            LlmProxyTurnResponse response = null;
            string error = null;
            yield return Send("teacher-rubric", envelope, value => response = value, value => error = value);
            if (!string.IsNullOrWhiteSpace(error) ||
                !LlmContractValidator.TryAcceptRubric(response?.teacherRubric, out TeacherRubricResult accepted))
            {
                failed?.Invoke(error ?? "보안 프록시 루브릭이 검증 계약을 충족하지 못했습니다.");
                yield break;
            }
            completed?.Invoke(accepted);
        }

        private LlmProxyTurnEnvelope CreateEnvelope()
        {
            return new LlmProxyTurnEnvelope
            {
                sessionId = SystemInfo.deviceUniqueIdentifier,
                requestId = Guid.NewGuid().ToString("N"),
                promptVersion = PromptVersion
            };
        }

        private IEnumerator Send(
            string route,
            LlmProxyTurnEnvelope envelope,
            Action<LlmProxyTurnResponse> completed,
            Action<string> failed)
        {
            if (!IsConfigured)
            {
                failed?.Invoke("보안 LLM 프록시 설정 또는 단기 세션 토큰이 없습니다.");
                yield break;
            }

            string url = settings.Endpoint.TrimEnd('/') + "/v1/" + route;
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope));
            using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + sessionAuthorization);
            request.SetRequestHeader("X-Client-Id", settings.ClientId);
            request.timeout = settings.TimeoutSeconds;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                failed?.Invoke($"{request.responseCode} {request.error}");
                yield break;
            }

            try
            {
                LlmProxyTurnResponse response = JsonUtility.FromJson<LlmProxyTurnResponse>(request.downloadHandler.text);
                if (response == null || response.schemaVersion != LlmProxyTurnEnvelope.CurrentSchemaVersion ||
                    response.requestId != envelope.requestId || !string.IsNullOrWhiteSpace(response.errorCode))
                {
                    failed?.Invoke(response?.errorCode ?? "보안 프록시 응답 검증에 실패했습니다.");
                    yield break;
                }
                completed?.Invoke(response);
            }
            catch (ArgumentException)
            {
                failed?.Invoke("보안 프록시 응답 JSON이 올바르지 않습니다.");
            }
        }
    }
}
