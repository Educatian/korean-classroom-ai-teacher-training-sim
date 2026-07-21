using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    internal sealed class ResearchQuestTokenRequest
    {
        public int schemaVersion = 1;
        public string clientId = "teacher-training-quest";
        public string installationId = string.Empty;
        public string buildVersion = string.Empty;
        public string deviceModel = string.Empty;
    }

    [Serializable]
    internal sealed class ResearchQuestTokenResponse
    {
        public int schemaVersion;
        public string token = string.Empty;
        public string participantCode = string.Empty;
        public string expiresAtUtc = string.Empty;
    }

    public static class ResearchInstallIdentity
    {
        private const string InstallationPreferenceKey =
            "TeacherTraining.Research.InstallationId";
        private const string ClientId = "teacher-training-quest";

        public static string GetOrCreateInstallationId()
        {
            string existing = PlayerPrefs.GetString(InstallationPreferenceKey, string.Empty);
            if (IsInstallationId(existing))
            {
                return existing;
            }

            string created = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(InstallationPreferenceKey, created);
            PlayerPrefs.Save();
            return created;
        }

        public static string ParticipantCodeForInstallationId(string installationId)
        {
            if (!IsInstallationId(installationId))
            {
                throw new ArgumentException(
                    "Installation ID must contain 32 lowercase hexadecimal characters.",
                    nameof(installationId));
            }

            byte[] source = Encoding.UTF8.GetBytes(ClientId + ":" + installationId);
            using SHA256 sha = SHA256.Create();
            byte[] digest = sha.ComputeHash(source);
            var code = new StringBuilder("Q-", 26);
            for (int index = 0; index < 12; index++)
            {
                code.Append(digest[index].ToString("x2"));
            }
            return code.ToString();
        }

        private static bool IsInstallationId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
            {
                return false;
            }
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'a' && character <= 'f')))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public static class ResearchConsentPreferences
    {
        private const string RawGazePreferenceKey =
            "TeacherTraining.Research.RawGazeConsent";

        public static bool RawGazeConsent
        {
            get => PlayerPrefs.GetInt(RawGazePreferenceKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(RawGazePreferenceKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
