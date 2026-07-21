using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [CreateAssetMenu(
        fileName = "ResearchCloudSyncSettings",
        menuName = "Teacher Training/Research Cloud Sync Settings",
        order = 41)]
    public sealed class ResearchCloudSyncSettings : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/Research/ResearchCloudSyncSettings";

        [SerializeField] private string endpoint;
        [SerializeField] private string clientId = "teacher-training-quest";
        [SerializeField, Range(10, 120)] private int timeoutSeconds = 45;
        [SerializeField] private bool uploadRawGaze = true;
        [SerializeField] private bool automaticLogging = true;
        [SerializeField] private string registrationKey = string.Empty;

        public string Endpoint => endpoint;
        public string ClientId => clientId;
        public string RegistrationKey => registrationKey;
        public int TimeoutSeconds => timeoutSeconds;
        public bool UploadRawGaze => uploadRawGaze;
        public bool AutomaticLogging => automaticLogging;
        public bool IsConfigured => IsAllowedEndpoint(endpoint);

        public static ResearchCloudSyncSettings LoadDefault()
        {
            return Resources.Load<ResearchCloudSyncSettings>(DefaultResourcePath);
        }

        public static bool IsAllowedEndpoint(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) ||
                uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            string host = uri.Host.ToLowerInvariant();
            return host != "api.cloudflare.com" &&
                   host != "api.openrouter.ai" &&
                   !host.EndsWith(".r2.cloudflarestorage.com", StringComparison.Ordinal);
        }
    }

    [Serializable]
    public sealed class ResearchSessionStartPayload
    {
        public int schemaVersion = 1;
        public string sessionId = string.Empty;
        public string participantCode = string.Empty;
        public string scenarioId = string.Empty;
        public string startedAtUtc = string.Empty;
        public string deviceModel = string.Empty;
        public string buildVersion = string.Empty;
        public bool rawGazeConsent;
    }

    [Serializable]
    public sealed class ResearchEventBatchPayload
    {
        public int schemaVersion = 1;
        public string requestId = string.Empty;
        public TrainingTelemetryEvent[] events = Array.Empty<TrainingTelemetryEvent>();
    }

    [Serializable]
    public sealed class ResearchSessionCompletionPayload
    {
        public int schemaVersion = 1;
        public string completedAtUtc = string.Empty;
        public string status = "completed";
        public ResearchDebriefReport report = new ResearchDebriefReport();
    }

    [Serializable]
    internal sealed class ResearchCollectorResponse
    {
        public bool stored;
        public string sessionId = string.Empty;
        public string objectKey = string.Empty;
        public string error = string.Empty;
    }

    [Serializable]
    internal sealed class GazeTimestampEnvelope
    {
        public string timestampUtc = string.Empty;
    }

    public sealed class ResearchRawGazeDescriptor
    {
        public string path = string.Empty;
        public string sha256 = string.Empty;
        public int sampleCount;
        public long byteLength;
        public string startedAtUtc = string.Empty;
        public string endedAtUtc = string.Empty;

        public static ResearchRawGazeDescriptor FromFile(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException("Raw gaze path is required.", nameof(sourcePath));
            }
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Raw gaze file was not found.", sourcePath);
            }

            string hash;
            using (FileStream stream = File.OpenRead(sourcePath))
            using (SHA256 sha = SHA256.Create())
            {
                hash = ToLowerHex(sha.ComputeHash(stream));
            }

            int count = 0;
            string firstTimestamp = string.Empty;
            string lastTimestamp = string.Empty;
            foreach (string line in File.ReadLines(sourcePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                GazeTimestampEnvelope item =
                    JsonUtility.FromJson<GazeTimestampEnvelope>(line);
                string timestamp = item != null ? item.timestampUtc : string.Empty;
                if (count == 0) firstTimestamp = timestamp;
                lastTimestamp = timestamp;
                count++;
            }

            return new ResearchRawGazeDescriptor
            {
                path = sourcePath,
                sha256 = hash,
                sampleCount = count,
                byteLength = new FileInfo(sourcePath).Length,
                startedAtUtc = firstTimestamp,
                endedAtUtc = lastTimestamp
            };
        }

        private static string ToLowerHex(IReadOnlyList<byte> bytes)
        {
            var characters = new char[bytes.Count * 2];
            const string alphabet = "0123456789abcdef";
            for (int index = 0; index < bytes.Count; index++)
            {
                byte value = bytes[index];
                characters[index * 2] = alphabet[value >> 4];
                characters[index * 2 + 1] = alphabet[value & 0x0f];
            }
            return new string(characters);
        }
    }
}
