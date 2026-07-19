using System;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum LlmTransportMode
    {
        DirectProviderDesktop = 0,
        SecureProxy = 1
    }

    public static class LlmDeploymentPolicy
    {
        public static LlmTransportMode TransportFor(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.Android || platform == RuntimePlatform.WebGLPlayer
                ? LlmTransportMode.SecureProxy
                : LlmTransportMode.DirectProviderDesktop;
        }

        public static bool IsEndpointAllowed(LlmTransportMode mode, string endpoint)
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (mode != LlmTransportMode.SecureProxy)
            {
                return true;
            }

            return uri.Host.IndexOf("openrouter.ai", StringComparison.OrdinalIgnoreCase) < 0;
        }
    }

    [Serializable]
    public sealed class LlmProxyTurnEnvelope
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string sessionId;
        public string requestId;
        public int promptVersion;
        public StudentTurnRequest studentTurn;
        public TeacherRubricRequest teacherRubric;
    }

    [Serializable]
    public sealed class LlmProxyTurnResponse
    {
        public int schemaVersion = LlmProxyTurnEnvelope.CurrentSchemaVersion;
        public string requestId;
        public StudentAgentTurn studentTurn;
        public TeacherRubricResult teacherRubric;
        public string errorCode;
    }
}
