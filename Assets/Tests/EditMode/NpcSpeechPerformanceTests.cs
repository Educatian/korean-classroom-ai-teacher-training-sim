using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class NpcSpeechPerformanceTests
    {
        [Test]
        public void SpeechDirective_StopSpeakingRevealsAmbientOverride()
        {
            GameObject avatar = new GameObject(nameof(SpeechDirective_StopSpeakingRevealsAmbientOverride));
            FacialActionUnitController face = avatar.AddComponent<FacialActionUnitController>();
            NpcPerformance performance = avatar.AddComponent<NpcPerformance>();
            NpcSpeechPerformance speech = avatar.AddComponent<NpcSpeechPerformance>();
            Wire(performance, face, speech);

            performance.SetActionUnit(FacialActionUnit.AU4BrowLowerer, 0.12f, true);
            ApplyDirective(speech, new ActionUnitDirective { au4 = 0.92f });
            Assert.That(performance.GetActionUnit(FacialActionUnit.AU4BrowLowerer), Is.EqualTo(0.92f).Within(0.001f));

            speech.StopSpeaking();

            Assert.That(performance.GetActionUnit(FacialActionUnit.AU4BrowLowerer), Is.EqualTo(0.12f).Within(0.001f));
            Object.DestroyImmediate(avatar);
        }

        [Test]
        public void SpeechDirective_SecondTurnReleasesPreviousTurnUnits()
        {
            GameObject avatar = new GameObject(nameof(SpeechDirective_SecondTurnReleasesPreviousTurnUnits));
            FacialActionUnitController face = avatar.AddComponent<FacialActionUnitController>();
            NpcPerformance performance = avatar.AddComponent<NpcPerformance>();
            NpcSpeechPerformance speech = avatar.AddComponent<NpcSpeechPerformance>();
            Wire(performance, face, speech);

            ApplyDirective(speech, new ActionUnitDirective { au4 = 0.88f });
            ApplyDirective(speech, new ActionUnitDirective { au12 = 0.76f });

            Assert.That(performance.GetActionUnit(FacialActionUnit.AU4BrowLowerer), Is.EqualTo(0f).Within(0.001f));
            Assert.That(performance.GetActionUnit(FacialActionUnit.AU12LipCornerPuller), Is.EqualTo(0.76f).Within(0.001f));
            Object.DestroyImmediate(avatar);
        }

        [Test]
        public void ProceduralRotation_DoesNotAccumulateWhenAnimatorPoseIsStable()
        {
            var state = new ProceduralRotationState();
            Quaternion animatedPose = Quaternion.Euler(4f, -2f, 1f);
            Quaternion offset = Quaternion.Euler(7f, 0f, 0f);

            Quaternion firstFrame = state.Apply(animatedPose, offset);
            Quaternion secondFrame = state.Apply(firstFrame, offset);

            Assert.That(Quaternion.Angle(firstFrame, secondFrame), Is.LessThan(0.001f));
        }

        private static void Wire(
            NpcPerformance performance,
            FacialActionUnitController face,
            NpcSpeechPerformance speech)
        {
            FieldInfo faceField = typeof(NpcPerformance)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(field => field.FieldType == typeof(FacialActionUnitController));
            faceField.SetValue(performance, face);

            FieldInfo performanceField = typeof(NpcSpeechPerformance)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(field => field.FieldType == typeof(NpcPerformance));
            performanceField.SetValue(speech, performance);
        }

        private static void ApplyDirective(NpcSpeechPerformance speech, ActionUnitDirective directive)
        {
            MethodInfo method = typeof(NpcSpeechPerformance)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(item =>
                {
                    ParameterInfo[] parameters = item.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == typeof(ActionUnitDirective);
                });
            method.Invoke(speech, new object[] { directive });
        }
    }
}
