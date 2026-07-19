using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class AffectAndOpenRouterTests
    {
        [Test]
        public void OpenRouterPayload_PlainFeedbackDoesNotForceJsonMode()
        {
            string json = GenerativeAiCoach.CreateRequestJson("system", "teacher", false);

            Assert.That(json, Does.Not.Contain("response_format"));
        }

        [Test]
        public void OpenRouterPayload_StudentTurnRequestsJsonMode()
        {
            string json = GenerativeAiCoach.CreateRequestJson("system", "teacher", true);

            Assert.That(json, Does.Contain("response_format"));
            Assert.That(json, Does.Contain("json_schema"));
            Assert.That(json, Does.Contain("strict"));
        }

        [Test]
        public void AffectDynamics_ConstrainsSingleTurnEmotionJump()
        {
            AffectVector current = new AffectVector(-0.8f, 0.9f, -0.5f);
            AffectVector proposed = new AffectVector(1f, 0f, 1f);

            AffectVector result = AffectDynamics.ConstrainTurn(current, proposed, 0.35f);

            Assert.That(result.valence, Is.EqualTo(-0.45f).Within(0.001f));
            Assert.That(result.arousal, Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(result.dominance, Is.EqualTo(-0.15f).Within(0.001f));
        }

        [Test]
        public void AffectDynamics_StepUsesContinuousExponentialTransition()
        {
            AffectVector current = new AffectVector(-0.8f, 0.9f, -0.5f);
            AffectVector target = new AffectVector(0.2f, 0.2f, 0.1f);

            AffectVector result = AffectDynamics.Step(current, target, 2f, 0.5f);

            Assert.That(result.valence, Is.GreaterThan(current.valence).And.LessThan(target.valence));
            Assert.That(result.arousal, Is.LessThan(current.arousal).And.GreaterThan(target.arousal));
            Assert.That(result.dominance, Is.GreaterThan(current.dominance).And.LessThan(target.dominance));
        }

        [Test]
        public void GesturePlanner_ProducesVariedHighArousalProblemBehavior()
        {
            AffectVector state = new AffectVector(-0.85f, 0.94f, 0.7f);

            BehaviorGesture[] gestures = new BehaviorGesture[6];
            for (int i = 0; i < gestures.Length; i++)
            {
                gestures[i] = BehaviorGesturePlanner.Select(state, i);
            }

            Assert.That(gestures, Does.Contain(BehaviorGesture.Defiant));
            Assert.That(gestures, Does.Contain(BehaviorGesture.DeskTap));
            Assert.That(gestures, Does.Contain(BehaviorGesture.PushAway));
        }

        [Test]
        public void FacialController_ManualActionUnitSurvivesAffectUpdatesUntilReleased()
        {
            GameObject avatar = new GameObject("FaceTest");
            FacialActionUnitController controller = avatar.AddComponent<FacialActionUnitController>();
            controller.ApplyAffect(new AffectVector(-0.8f, 0.9f, 0.7f), true);
            controller.SetActionUnit(FacialActionUnit.AU4BrowLowerer, 0.92f, true);

            controller.ApplyAffect(new AffectVector(0.5f, 0.2f, 0f), true);

            Assert.That(controller.GetActionUnit(FacialActionUnit.AU4BrowLowerer), Is.EqualTo(0.92f).Within(0.001f));
            controller.ReleaseActionUnit(FacialActionUnit.AU4BrowLowerer, true);
            Assert.That(controller.GetActionUnit(FacialActionUnit.AU4BrowLowerer), Is.LessThan(0.1f));
            Object.DestroyImmediate(avatar);
        }

        [Test]
        public void RocketboxMaleFace_PrefersCanonicalAuChannelsWithoutDoubleDeformation()
        {
            const string path = "Assets/ThirdParty/MicrosoftRocketbox/Avatars/Children/Male_Child_01/Export/Male_Child_01_facial.fbx";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(source, Is.Not.Null, "The male Rocketbox facial avatar is missing.");

            GameObject avatar = Object.Instantiate(source);
            FacialActionUnitController controller = avatar.AddComponent<FacialActionUnitController>();
            typeof(FacialActionUnitController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, null);
            Assert.That(controller.ChannelCount, Is.EqualTo(175));

            controller.SetActionUnit(FacialActionUnit.AU4BrowLowerer, 1f, true);
            Assert.That(controller.GetMaxWeight("au_04_browlowerer"), Is.GreaterThan(99f));
            Assert.That(controller.GetMaxWeight("ak_01_browdown", "ak_02_browdown"), Is.LessThan(0.01f),
                "ARKit synonyms must stay neutral when a canonical AU channel exists.");

            controller.ClearActionUnitOverrides(true);
            controller.SetActionUnit(FacialActionUnit.AU5UpperLidRaiser, 1f, true);
            Assert.That(controller.GetMaxWeight("au_05_upperlidraiser"), Is.GreaterThan(99f));
            Assert.That(controller.GetMaxWeight("ak_21_eyewide", "ak_22_eyewide"), Is.LessThan(0.01f));

            controller.ClearActionUnitOverrides(true);
            controller.SetActionUnit(FacialActionUnit.AU45Blink, 1f, true);
            Assert.That(controller.GetMaxWeight("au_45_blink"), Is.GreaterThan(99f));
            Assert.That(controller.GetMaxWeight("ak_09_eyeblink", "ak_10_eyeblink", "sr_05_eye_left_blink", "sr_12_eye_right_blink"), Is.LessThan(0.01f));
            Object.DestroyImmediate(avatar);
        }

        [Test]
        public void FacialAffectProfile_RecoveryLooksRelaxedInsteadOfStartled()
        {
            GameObject avatar = new GameObject("RecoveryFaceTest");
            FacialActionUnitController controller = avatar.AddComponent<FacialActionUnitController>();

            controller.ApplyAffect(new AffectVector(0.22f, 0.26f, 0.12f), true);

            Assert.That(controller.GetActionUnit(FacialActionUnit.AU5UpperLidRaiser), Is.LessThan(0.04f));
            Assert.That(controller.GetActionUnit(FacialActionUnit.AU12LipCornerPuller), Is.GreaterThan(0.15f));
            Object.DestroyImmediate(avatar);
        }
    }
}
