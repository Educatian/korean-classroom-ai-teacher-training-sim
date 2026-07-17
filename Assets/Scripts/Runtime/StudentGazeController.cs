using System;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class StudentGazeController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform teacherTarget;
        [SerializeField, Range(0f, 1f)] private float attentionProbability = 0.48f;
        [SerializeField] private Vector2 idleDurationRange = new Vector2(1.8f, 5.2f);
        [SerializeField] private Vector2 attentionDurationRange = new Vector2(1.2f, 3.8f);
        [SerializeField, Min(0.1f)] private float lookResponsiveness = 2.6f;
        [SerializeField, Range(0f, 1f)] private float headTurnStrength = 1f;
        [SerializeField, Range(15f, 60f)] private float maxHeadTurnDegrees = 34f;
        [SerializeField] private bool startTrackingTeacher = true;

        private System.Random random;
        private float nextDecisionTime;
        private float lookWeight;
        private bool trackingTeacher;

        public Transform TeacherTarget => teacherTarget;
        public bool IsTrackingTeacher => trackingTeacher;
        public float LookWeight => lookWeight;
        public bool StartsAttentive => startTrackingTeacher;
        public float MaxHeadTurnDegrees => maxHeadTurnDegrees;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            random = new System.Random(StableHash(gameObject.name));
            trackingTeacher = startTrackingTeacher;
            lookWeight = startTrackingTeacher ? 1f : 0f;
            nextDecisionTime = Time.time + SampleRange(trackingTeacher ? attentionDurationRange : idleDurationRange);
        }

        private void Update()
        {
            if (animator == null || teacherTarget == null)
            {
                return;
            }

            if (Time.time >= nextDecisionTime)
            {
                trackingTeacher = random.NextDouble() < attentionProbability;
                nextDecisionTime = Time.time + SampleRange(trackingTeacher ? attentionDurationRange : idleDurationRange);
            }

            float targetWeight = trackingTeacher ? 1f : 0f;
            float blend = 1f - Mathf.Exp(-lookResponsiveness * Time.deltaTime);
            lookWeight = Mathf.Lerp(lookWeight, targetWeight, blend);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || teacherTarget == null || lookWeight <= 0.001f)
            {
                return;
            }

            animator.SetLookAtWeight(lookWeight, 0.08f, 0.24f, 0.92f, 0.58f);
            animator.SetLookAtPosition(teacherTarget.position);
        }

        private void LateUpdate()
        {
            ApplyCurrentGazePose();
        }

        public void ApplyCurrentGazePose()
        {
            if (animator == null || teacherTarget == null || lookWeight <= 0.001f)
            {
                return;
            }

            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head == null)
            {
                return;
            }

            Vector3 direction = teacherTarget.position - head.position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion correction = Quaternion.FromToRotation(head.up, direction.normalized);
            Quaternion targetRotation = correction * head.rotation;
            float permittedTurn = maxHeadTurnDegrees * Mathf.Clamp01(lookWeight * headTurnStrength);
            head.rotation = Quaternion.RotateTowards(head.rotation, targetRotation, permittedTurn);
        }

        public void Configure(Transform target, float attentionBias, bool startsAttentive = true)
        {
            teacherTarget = target;
            attentionProbability = Mathf.Clamp01(attentionBias);
            startTrackingTeacher = startsAttentive;
            if (startsAttentive)
            {
                idleDurationRange = new Vector2(0.8f, 1.8f);
                attentionDurationRange = new Vector2(5.0f, 9.0f);
            }
            else
            {
                idleDurationRange = new Vector2(4.5f, 8.0f);
                attentionDurationRange = new Vector2(1.2f, 2.4f);
            }
        }

        public void BeginTeacherAttention(float duration)
        {
            trackingTeacher = true;
            nextDecisionTime = Time.time + Mathf.Max(0.1f, duration);
        }

        public void BeginTeacherDistraction(float duration)
        {
            trackingTeacher = false;
            nextDecisionTime = Time.time + Mathf.Max(0.1f, duration);
        }

        private float SampleRange(Vector2 range)
        {
            float sample = (float)random.NextDouble();
            return Mathf.Lerp(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y), sample);
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
