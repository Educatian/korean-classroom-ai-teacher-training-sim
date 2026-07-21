using System.IO;
using System.Text;
using NUnit.Framework;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class ResearchCloudSyncTests
    {
        [Test]
        public void EndpointPolicy_AcceptsHttpsWorkerAndRejectsDirectStorageHosts()
        {
            Assert.That(ResearchCloudSyncSettings.IsAllowedEndpoint(
                "https://teacher-training-collector.example.workers.dev"), Is.True);
            Assert.That(ResearchCloudSyncSettings.IsAllowedEndpoint(
                "http://teacher-training-collector.example.workers.dev"), Is.False);
            Assert.That(ResearchCloudSyncSettings.IsAllowedEndpoint(
                "https://abc.r2.cloudflarestorage.com"), Is.False);
        }

        [Test]
        public void RawGazeDescriptor_CountsSamplesAndComputesStableSha256()
        {
            string path = Path.GetTempFileName();
            try
            {
                string payload =
                    "{\"timestampUtc\":\"2026-07-20T20:00:01.000Z\"}\n" +
                    "{\"timestampUtc\":\"2026-07-20T20:00:01.033Z\"}\n";
                File.WriteAllText(path, payload, new UTF8Encoding(false));

                ResearchRawGazeDescriptor descriptor =
                    ResearchRawGazeDescriptor.FromFile(path);

                Assert.That(descriptor.sampleCount, Is.EqualTo(2));
                Assert.That(descriptor.byteLength, Is.EqualTo(Encoding.UTF8.GetByteCount(payload)));
                Assert.That(descriptor.sha256, Has.Length.EqualTo(64));
                Assert.That(descriptor.startedAtUtc, Is.EqualTo("2026-07-20T20:00:01.000Z"));
                Assert.That(descriptor.endedAtUtc, Is.EqualTo("2026-07-20T20:00:01.033Z"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void InstallIdentity_DerivesStablePseudonymousParticipantCode()
        {
            string installationId = "11111111111111111111111111111111";

            string participantCode =
                ResearchInstallIdentity.ParticipantCodeForInstallationId(installationId);

            Assert.That(participantCode, Is.EqualTo("Q-0065099da16cf418e05616f1"));
            Assert.Throws<System.ArgumentException>(() =>
                ResearchInstallIdentity.ParticipantCodeForInstallationId("not-an-install-id"));
        }

        [Test]
        public void DefaultSettings_EnableAutomaticLogging()
        {
            ResearchCloudSyncSettings settings = ResearchCloudSyncSettings.LoadDefault();

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.AutomaticLogging, Is.True);
            Assert.That(settings.IsConfigured, Is.True);
        }
    }
}
