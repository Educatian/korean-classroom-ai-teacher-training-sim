using System;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum NpcIdleBehavior
    {
        Neutral,
        Listening,
        LookAround,
        DeskFidget,
        ShoulderShift,
        ChinRest,
        Yawn
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NpcPerformance))]
    public sealed class NpcIdleBehaviorController : MonoBehaviour
    {
        private static readonly NpcIdleBehavior[] AttentiveSequence =
        {
            NpcIdleBehavior.Neutral,
            NpcIdleBehavior.Listening,
            NpcIdleBehavior.LookAround,
            NpcIdleBehavior.DeskFidget,
            NpcIdleBehavior.ShoulderShift,
            NpcIdleBehavior.Listening,
            NpcIdleBehavior.Neutral,
            NpcIdleBehavior.Yawn,
            NpcIdleBehavior.Listening,
            NpcIdleBehavior.LookAround
        };

        private static readonly NpcIdleBehavior[] DistractedSequence =
        {
            NpcIdleBehavior.ChinRest,
            NpcIdleBehavior.LookAround,
            NpcIdleBehavior.Neutral,
            NpcIdleBehavior.DeskFidget,
            NpcIdleBehavior.ChinRest,
            NpcIdleBehavior.ShoulderShift
        };

        [SerializeField] private bool attentive = true;
        [SerializeField] private int behaviorVariant;

        private NpcPerformance performance;
        private Animator animator;
        private System.Random random;
        private int sequenceIndex;
        private float nextBehaviorTime;
        private float yawnExpressionEndTime;

        public NpcIdleBehavior CurrentBehavior { get; private set; }
        public bool IsAttentiveProfile => attentive;
        public ChinRestDeskContactController ChinRestContact { get; private set; }

        private void Awake()
        {
            performance = GetComponent<NpcPerformance>();
            random = new System.Random(StableHash(gameObject.name));
            sequenceIndex = Mathf.Abs(behaviorVariant);
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                ChinRestContact = animator.GetComponent<ChinRestDeskContactController>();
                if (ChinRestContact == null)
                {
                    ChinRestContact = animator.gameObject.AddComponent<ChinRestDeskContactController>();
                }

                ChinRestContact.Configure(this);
            }
        }

        private void Start()
        {
            NpcIdleBehavior initial = attentive
                ? AttentiveSequence[sequenceIndex % AttentiveSequence.Length]
                : behaviorVariant % 2 == 0 ? NpcIdleBehavior.ChinRest : NpcIdleBehavior.LookAround;
            PlayImmediately(initial, 5.4f + Mathf.Abs(behaviorVariant % 6) * 0.43f);
        }

        private void Update()
        {
            if (yawnExpressionEndTime > 0f && Time.time >= yawnExpressionEndTime)
            {
                ReleaseYawnExpression();
            }

            if (Time.time < nextBehaviorTime)
            {
                return;
            }

            NpcIdleBehavior[] sequence = attentive ? AttentiveSequence : DistractedSequence;
            sequenceIndex = (sequenceIndex + 1 + random.Next(0, 2)) % sequence.Length;
            float holdSeconds = Mathf.Lerp(6.2f, 11.8f, (float)random.NextDouble());
            PlayImmediately(sequence[sequenceIndex], holdSeconds);
        }

        public void Configure(bool attentiveProfile, int variant)
        {
            attentive = attentiveProfile;
            behaviorVariant = variant;
        }

        public void PlayImmediately(NpcIdleBehavior behavior, float holdSeconds)
        {
            ReleaseIdleExpression();
            CurrentBehavior = behavior;
            switch (behavior)
            {
                case NpcIdleBehavior.Neutral:
                    performance.SetAmbientGesture(BehaviorGesture.Neutral, 0.18f, holdSeconds);
                    break;
                case NpcIdleBehavior.Listening:
                    performance.SetAmbientGesture(BehaviorGesture.Listen, 0.24f, holdSeconds);
                    performance.SetActionUnit(FacialActionUnit.AU1InnerBrowRaiser, 0.06f);
                    performance.SetActionUnit(FacialActionUnit.AU5UpperLidRaiser, 0.08f);
                    break;
                case NpcIdleBehavior.LookAround:
                    performance.SetAmbientGesture(BehaviorGesture.Point, 0.20f, holdSeconds);
                    performance.SetActionUnit(FacialActionUnit.AU2OuterBrowRaiser, 0.10f);
                    performance.SetActionUnit(FacialActionUnit.AU5UpperLidRaiser, 0.12f);
                    break;
                case NpcIdleBehavior.DeskFidget:
                    performance.SetAmbientGesture(BehaviorGesture.DeskTap, 0.30f, holdSeconds);
                    performance.SetActionUnit(FacialActionUnit.AU7LidTightener, 0.10f);
                    performance.SetActionUnit(FacialActionUnit.AU24LipPressor, 0.08f);
                    break;
                case NpcIdleBehavior.ShoulderShift:
                    performance.SetAmbientGesture(BehaviorGesture.PushAway, 0.28f, holdSeconds);
                    performance.SetActionUnit(FacialActionUnit.AU1InnerBrowRaiser, 0.08f);
                    break;
                case NpcIdleBehavior.ChinRest:
                    performance.SetAmbientGesture(BehaviorGesture.AvoidGaze, 0.36f, holdSeconds);
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        animator.Play("AvoidGaze", 0, 0.42f);
                        animator.Update(0f);
                        animator.speed = 0f;
                    }
                    performance.SetActionUnit(FacialActionUnit.AU4BrowLowerer, 0.12f);
                    performance.SetActionUnit(FacialActionUnit.AU17ChinRaiser, 0.10f);
                    break;
                case NpcIdleBehavior.Yawn:
                    performance.SetAmbientGesture(BehaviorGesture.Protest, 0.38f, holdSeconds);
                    performance.SetActionUnit(FacialActionUnit.AU25LipsPart, 0.78f);
                    performance.SetActionUnit(FacialActionUnit.AU26JawDrop, 0.92f);
                    performance.SetActionUnit(FacialActionUnit.AU45Blink, 0.48f);
                    yawnExpressionEndTime = Time.time + Mathf.Min(3.2f, Mathf.Max(1.8f, holdSeconds));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(behavior), behavior, null);
            }

            nextBehaviorTime = Time.time + Mathf.Max(1.8f, holdSeconds);
        }

        private void ReleaseIdleExpression()
        {
            performance.ReleaseActionUnit(FacialActionUnit.AU1InnerBrowRaiser);
            performance.ReleaseActionUnit(FacialActionUnit.AU2OuterBrowRaiser);
            performance.ReleaseActionUnit(FacialActionUnit.AU4BrowLowerer);
            performance.ReleaseActionUnit(FacialActionUnit.AU5UpperLidRaiser);
            performance.ReleaseActionUnit(FacialActionUnit.AU7LidTightener);
            performance.ReleaseActionUnit(FacialActionUnit.AU17ChinRaiser);
            performance.ReleaseActionUnit(FacialActionUnit.AU24LipPressor);
            ReleaseYawnExpression();
        }

        private void ReleaseYawnExpression()
        {
            if (yawnExpressionEndTime <= 0f)
            {
                return;
            }

            performance.ReleaseActionUnit(FacialActionUnit.AU25LipsPart);
            performance.ReleaseActionUnit(FacialActionUnit.AU26JawDrop);
            performance.ReleaseActionUnit(FacialActionUnit.AU45Blink);
            yawnExpressionEndTime = 0f;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }
    }
}
