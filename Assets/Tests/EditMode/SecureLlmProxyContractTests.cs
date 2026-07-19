using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class SecureLlmProxyContractTests
    {
        [Test]
        public void AndroidRuntime_RequiresSecureProxyTransport()
        {
            Assert.That(
                LlmDeploymentPolicy.TransportFor(RuntimePlatform.Android),
                Is.EqualTo(LlmTransportMode.SecureProxy));
        }

        [Test]
        public void SecureProxy_RejectsDirectOpenRouterEndpoint()
        {
            bool allowed = LlmDeploymentPolicy.IsEndpointAllowed(
                LlmTransportMode.SecureProxy,
                "https://openrouter.ai/api/v1/chat/completions");

            Assert.That(allowed, Is.False);
        }

        [Test]
        public void ProxyEnvelope_NeverSerializesAProviderApiKey()
        {
            var envelope = new LlmProxyTurnEnvelope
            {
                sessionId = "session",
                requestId = "request",
                promptVersion = 2,
                studentTurn = new StudentTurnRequest { teacherUtterance = "괜찮니?" }
            };

            string json = JsonUtility.ToJson(envelope);

            Assert.That(json.ToLowerInvariant(), Does.Not.Contain("apikey"));
            Assert.That(json.ToLowerInvariant(), Does.Not.Contain("openrouter"));
        }
    }
}
