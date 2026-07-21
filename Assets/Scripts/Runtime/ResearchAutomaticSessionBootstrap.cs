using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class ResearchAutomaticSessionBootstrap : MonoBehaviour
    {
        private ResearchCloudSyncSettings settings;
        private SimulationController controller;
        private Coroutine authorizationRoutine;
        private string installationId;
        private string participantCode;
        private bool rawGazeConsent;
        private bool authorized;

        public void Initialize(SimulationController simulationController)
        {
            if (controller != null)
            {
                return;
            }

            controller = simulationController;
            settings = ResearchCloudSyncSettings.LoadDefault();
            if (controller == null ||
                settings == null ||
                !settings.IsConfigured ||
                !settings.AutomaticLogging)
            {
                return;
            }

            installationId = ResearchInstallIdentity.GetOrCreateInstallationId();
            participantCode =
                ResearchInstallIdentity.ParticipantCodeForInstallationId(installationId);
            rawGazeConsent = ResearchConsentPreferences.RawGazeConsent;
            controller.ConfigureResearchLoggingSession(
                null,
                participantCode,
                rawGazeConsent);
            TryAuthorize();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                TryAuthorize();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
            {
                TryAuthorize();
            }
        }

        private void TryAuthorize()
        {
            if (controller == null ||
                settings == null ||
                authorized ||
                authorizationRoutine != null)
            {
                return;
            }
            authorizationRoutine = StartCoroutine(RequestAuthorization());
        }

        private IEnumerator RequestAuthorization()
        {
            var payload = new ResearchQuestTokenRequest
            {
                clientId = settings.ClientId,
                installationId = installationId,
                buildVersion = Application.version,
                deviceModel = string.IsNullOrWhiteSpace(SystemInfo.deviceModel)
                    ? "Unknown Quest device"
                    : SystemInfo.deviceModel
            };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using var request = new UnityWebRequest(
                settings.Endpoint.TrimEnd('/') + "/v1/auth/quest-session",
                UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Client-Id", settings.ClientId);
            if (!string.IsNullOrWhiteSpace(settings.RegistrationKey))
            {
                request.SetRequestHeader("X-Registration-Key", settings.RegistrationKey.Trim());
            }
            request.timeout = settings.TimeoutSeconds;
            yield return request.SendWebRequest();

            authorizationRoutine = null;
            if (request.result != UnityWebRequest.Result.Success ||
                request.responseCode < 200 ||
                request.responseCode >= 300)
            {
                Debug.LogWarning(
                    "Automatic research authorization will retry on app resume: " +
                    request.responseCode + " " + request.error);
                yield break;
            }

            ResearchQuestTokenResponse response =
                JsonUtility.FromJson<ResearchQuestTokenResponse>(
                    request.downloadHandler.text);
            if (response == null ||
                string.IsNullOrWhiteSpace(response.token) ||
                response.participantCode != participantCode)
            {
                Debug.LogWarning("Automatic research authorization returned an invalid response.");
                yield break;
            }

            authorized = true;
            controller.ConfigureResearchSession(
                response.token,
                participantCode,
                rawGazeConsent);
            controller.RegisterResearchLoggingSession();
            Debug.Log("Automatic pseudonymous research logging is active.");
        }
    }
}
