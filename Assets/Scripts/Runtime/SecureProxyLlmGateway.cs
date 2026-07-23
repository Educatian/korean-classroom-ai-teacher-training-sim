using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AdieLab.TeacherTraining
{
    [CreateAssetMenu(fileName = "SecureLlmProxySettings", menuName = "Teacher Training/Secure LLM Proxy Settings", order = 40)]
    public sealed class SecureLlmProxySettings : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/SecureLlmProxySettings";
        [SerializeField] private string endpoint;
        [SerializeField] private string clientId = "teacher-training-quest";
        [SerializeField, Range(10, 60)] private int timeoutSeconds = 30;
        public string Endpoint => endpoint;
        public string ClientId => clientId;
        public int TimeoutSeconds => timeoutSeconds;
        public bool IsValid => LlmDeploymentPolicy.IsEndpointAllowed(LlmTransportMode.SecureProxy, endpoint);
    }

    [DisallowMultipleComponent]
    public sealed class SecureProxyLlmGateway : MonoBehaviour, ILlmGateway
    {
        [Serializable] private sealed class TranscriptResponse { public int schemaVersion; public string transcript; }
        [Serializable] private sealed class SpeechRequest { public int schemaVersion = 1; public string text; public float rate; public float pitchSemitones; public float volume; }

        [SerializeField] private SecureLlmProxySettings settings;
        private string sessionAuthorization;
        private string pseudonymousSessionScope;
        public bool HasSuccessfulStudentTurn { get; private set; }
        public bool HasSuccessfulTranscription { get; private set; }
        public bool HasSuccessfulSpeechSynthesis { get; private set; }
        public string LastSuccessfulOperationAtUtc { get; private set; } = string.Empty;
        public bool IsConfigured => settings != null && settings.IsValid && !string.IsNullOrWhiteSpace(sessionAuthorization);
        public string ConfigurationLabel => IsConfigured ? "보안 LLM 프록시 연결" : "로컬 대화 모드 · 보안 프록시 세션 대기";
        public string ModelId => "secure-proxy/managed";
        public int PromptVersion => OpenRouterRuntimeConfiguration.DefaultPromptVersion;

        private void Awake()
        {
            if (settings == null) settings = Resources.Load<SecureLlmProxySettings>(SecureLlmProxySettings.DefaultResourcePath);
            pseudonymousSessionScope = ResearchInstallIdentity.ParticipantCodeForInstallationId(
                ResearchInstallIdentity.GetOrCreateInstallationId());
        }

        public void SetSessionAuthorization(string bearerToken) => sessionAuthorization = string.IsNullOrWhiteSpace(bearerToken) ? null : bearerToken.Trim();

        public IEnumerator RequestStudentTurn(StudentTurnRequest request, Action<StudentAgentTurn> completed, Action<string> failed)
        {
            var envelope = CreateEnvelope(); envelope.studentTurn = request;
            LlmProxyTurnResponse response = null; string error = null;
            yield return SendJson("student-turn", envelope, value => response = value, value => error = value);
            if (!string.IsNullOrWhiteSpace(error) || response?.studentTurn == null) { failed?.Invoke(error ?? "보안 프록시 학생 응답이 비어 있습니다."); yield break; }
            StudentTurnResolution resolution = StudentTurnBoundary.Normalize(JsonUtility.ToJson(response.studentTurn));
            if (resolution.Outcome != StudentTurnOutcome.Accepted) { failed?.Invoke("보안 프록시 학생 응답이 검증 계약을 충족하지 못했습니다."); yield break; }
            HasSuccessfulStudentTurn = true;
            LastSuccessfulOperationAtUtc = DateTime.UtcNow.ToString("O");
            completed?.Invoke(resolution.Turn);
        }

        public IEnumerator RequestTeacherRubric(TeacherRubricRequest request, Action<TeacherRubricResult> completed, Action<string> failed)
        {
            var envelope = CreateEnvelope(); envelope.teacherRubric = request;
            LlmProxyTurnResponse response = null; string error = null;
            yield return SendJson("teacher-rubric", envelope, value => response = value, value => error = value);
            if (!string.IsNullOrWhiteSpace(error) || !LlmContractValidator.TryAcceptRubric(response?.teacherRubric, out TeacherRubricResult accepted)) { failed?.Invoke(error ?? "보안 프록시 루브릭이 검증 계약을 충족하지 못했습니다."); yield break; }
            completed?.Invoke(accepted);
        }

        public IEnumerator RequestTranscription(byte[] wav, Action<string> completed, Action<string> failed)
        {
            if (!IsConfigured) { failed?.Invoke("보안 음성 프록시 세션이 준비되지 않았습니다."); yield break; }
            if (wav == null || wav.Length <= 44) { failed?.Invoke("녹음된 음성이 비어 있습니다."); yield break; }
            using var request = CreateRequest("transcribe", wav, "audio/wav");
            yield return request.SendWebRequest();
            if (!Succeeded(request, failed)) yield break;
            TranscriptResponse response = JsonUtility.FromJson<TranscriptResponse>(request.downloadHandler.text);
            if (response == null || response.schemaVersion != 1 || string.IsNullOrWhiteSpace(response.transcript)) { failed?.Invoke("음성 전사 응답이 올바르지 않습니다."); yield break; }
            HasSuccessfulTranscription = true;
            LastSuccessfulOperationAtUtc = DateTime.UtcNow.ToString("O");
            completed?.Invoke(response.transcript.Trim());
        }

        public IEnumerator RequestSpeech(string text, StudentSpeechProsody prosody, Action<AudioClip> completed, Action<string> failed)
        {
            if (!IsConfigured) { failed?.Invoke("보안 TTS 프록시 세션이 준비되지 않았습니다."); yield break; }
            var payload = new SpeechRequest { text = text, rate = prosody.rate, pitchSemitones = prosody.pitchSemitones, volume = prosody.volume };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using var request = CreateRequest("speech", body, "application/json");
            yield return request.SendWebRequest();
            if (!Succeeded(request, failed)) yield break;
            string path = Path.Combine(Application.temporaryCachePath, $"student-speech-proxy-{Guid.NewGuid():N}.wav");
            try { File.WriteAllBytes(path, request.downloadHandler.data); }
            catch (IOException) { failed?.Invoke("합성 음성 임시 파일을 만들 수 없습니다."); yield break; }
            using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.WAV);
            yield return audioRequest.SendWebRequest();
            if (audioRequest.result != UnityWebRequest.Result.Success) failed?.Invoke("합성된 학생 음성을 불러오지 못했습니다.");
            else { AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest); clip.name = "StudentSpeechSecureProxy"; HasSuccessfulSpeechSynthesis = true; LastSuccessfulOperationAtUtc = DateTime.UtcNow.ToString("O"); completed?.Invoke(clip); }
            try { File.Delete(path); } catch (IOException) { }
        }

        private LlmProxyTurnEnvelope CreateEnvelope() => new LlmProxyTurnEnvelope { sessionId = pseudonymousSessionScope, requestId = Guid.NewGuid().ToString("N"), promptVersion = PromptVersion };

        private IEnumerator SendJson(string route, LlmProxyTurnEnvelope envelope, Action<LlmProxyTurnResponse> completed, Action<string> failed)
        {
            if (!IsConfigured) { failed?.Invoke("보안 LLM 프록시 설정 또는 단기 세션 토큰이 없습니다."); yield break; }
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope));
            using var request = CreateRequest(route, body, "application/json");
            yield return request.SendWebRequest();
            if (!Succeeded(request, failed)) yield break;
            LlmProxyTurnResponse response;
            try { response = JsonUtility.FromJson<LlmProxyTurnResponse>(request.downloadHandler.text); }
            catch (ArgumentException) { failed?.Invoke("보안 프록시 응답 JSON이 올바르지 않습니다."); yield break; }
            if (response == null || response.schemaVersion != LlmProxyTurnEnvelope.CurrentSchemaVersion || response.requestId != envelope.requestId || !string.IsNullOrWhiteSpace(response.errorCode)) { failed?.Invoke(response?.errorCode ?? "보안 프록시 응답 검증에 실패했습니다."); yield break; }
            completed?.Invoke(response);
        }

        private UnityWebRequest CreateRequest(string route, byte[] body, string contentType)
        {
            var request = new UnityWebRequest(settings.Endpoint.TrimEnd('/') + "/v1/" + route, UnityWebRequest.kHttpVerbPOST) { uploadHandler = new UploadHandlerRaw(body), downloadHandler = new DownloadHandlerBuffer(), timeout = settings.TimeoutSeconds };
            request.SetRequestHeader("Content-Type", contentType);
            request.SetRequestHeader("Authorization", "Bearer " + sessionAuthorization);
            request.SetRequestHeader("X-Client-Id", settings.ClientId);
            return request;
        }

        private static bool Succeeded(UnityWebRequest request, Action<string> failed)
        {
            if (request.result == UnityWebRequest.Result.Success && request.responseCode >= 200 && request.responseCode < 300) return true;
            failed?.Invoke($"{request.responseCode} {request.error}");
            return false;
        }
    }
}
