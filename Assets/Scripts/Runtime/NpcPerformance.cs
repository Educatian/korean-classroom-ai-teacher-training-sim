using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class NpcPerformance : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private FacialActionUnitController face;
        [SerializeField] private StudentAffect initialAffect = StudentAffect.Calm;
        [SerializeField, Min(0.05f)] private float affectTransitionSpeed = 1.2f;
        [SerializeField] private Transform conversationTarget;

        private StudentAffect currentAffect;
        private BehaviorGesture currentGesture;
        private AffectVector currentVector;
        private AffectVector targetVector;
        private float gestureIntensity;
        private bool uprightEyeContact;
        private int gesturePhase;
        private float nextGestureTime;
        private float gestureMotionPhase;
        private float animationSpeedBias = 1f;
        private System.Random gestureRandom;
        private readonly ProceduralRotationState proceduralRotation = new ProceduralRotationState();
        private Transform proceduralChest;
        [SerializeField] private bool ambientPerformance;

        public StudentAffect CurrentAffect => currentAffect;
        public BehaviorGesture CurrentGesture => currentGesture;
        public AffectVector CurrentVector => currentVector;
        public AffectVector TargetVector => targetVector;
        public int BlendShapeChannelCount => face == null ? 0 : face.ChannelCount;
        public bool UprightEyeContact => uprightEyeContact;
        private void Awake()
        {
            int seed = StableHash(gameObject.name);
            gestureRandom = new System.Random(seed);
            gesturePhase = Mathf.Abs(seed % 19);
            gestureMotionPhase = (float)gestureRandom.NextDouble() * Mathf.PI * 2f;
            animationSpeedBias = Mathf.Lerp(0.92f, 1.08f, (float)gestureRandom.NextDouble());

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (face == null)
            {
                face = GetComponent<FacialActionUnitController>();
                if (face == null)
                {
                    face = gameObject.AddComponent<FacialActionUnitController>();
                }
            }

            SetAffect(initialAffect, true);
            if (ambientPerformance)
            {
                SetGesture(BehaviorGesture.Neutral, 0.18f);
            }
        }

        private void Update()
        {
            currentVector = AffectDynamics.Step(currentVector, targetVector, affectTransitionSpeed, Time.deltaTime);
            face.ApplyAffect(currentVector);

            if (!uprightEyeContact && !ambientPerformance && Time.time >= nextGestureTime)
            {
                SetGesture(BehaviorGesturePlanner.Select(currentVector, gesturePhase++), currentVector.arousal);
            }

            if (animator != null)
            {
                if (uprightEyeContact)
                {
                    animator.speed = 0.72f;
                }
                else if (ambientPerformance)
                {
                    if (animator.enabled)
                    {
                        animator.speed = (gestureIntensity < 0.45f ? 0f : 0.72f) * animationSpeedBias;
                    }
                }
                else
                {
                    animator.speed = Mathf.Lerp(0.88f, 1.18f, currentVector.arousal) * animationSpeedBias;
                }
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null)
            {
                return;
            }

            if (conversationTarget == null)
            {
                return;
            }

            float avoidance = currentGesture == BehaviorGesture.AvoidGaze || currentGesture == BehaviorGesture.Withdraw
                ? Mathf.Lerp(0.18f, 0.48f, 1f - currentVector.arousal)
                : 0.86f;
            animator.SetLookAtWeight(avoidance, 0.18f, 0.95f, 1f, 0.52f);
            animator.SetLookAtPosition(conversationTarget.position);
        }

        private void LateUpdate()
        {
            if (animator == null)
            {
                return;
            }

            ApplyProceduralGesture();
            if (conversationTarget == null || (currentGesture != BehaviorGesture.Listen && !uprightEyeContact))
            {
                return;
            }

            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head == null)
            {
                return;
            }

            Vector3 lensDirection = conversationTarget.position - head.position;
            if (lensDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion correction = Quaternion.FromToRotation(head.up, lensDirection.normalized);
            head.rotation = correction * head.rotation;
        }

        public void SetConversationTarget(Transform target)
        {
            conversationTarget = target;
        }

        public void SetUprightEyeContact(bool enabled)
        {
            uprightEyeContact = enabled;
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            if (enabled)
            {
                animator.enabled = true;
                animator.Play("UprightListen", 0, 0.08f);
                animator.Update(0f);
                return;
            }

            SetGesture(BehaviorGesture.Listen, 0.18f);
        }

        public void SetAffect(StudentAffect affect, bool immediate = false)
        {
            currentAffect = affect;
            targetVector = VectorFor(affect);
            BehaviorGesture gesture = affect switch
            {
                StudentAffect.Calm => BehaviorGesture.Neutral,
                StudentAffect.Uneasy => BehaviorGesture.Fidget,
                StudentAffect.Distressed => BehaviorGesture.Withdraw,
                StudentAffect.Angry => BehaviorGesture.Defiant,
                StudentAffect.Recovering => BehaviorGesture.Recover,
                _ => BehaviorGesture.Neutral
            };
            if (!ambientPerformance)
            {
                SetGesture(gesture, targetVector.arousal);
            }

            if (!immediate)
            {
                return;
            }

            currentVector = targetVector;
            face.ApplyAffect(currentVector, true);
        }

        public void SetAffectVector(AffectVector vector, BehaviorGesture gesture, bool immediate = false)
        {
            targetVector = AffectDynamics.ConstrainTurn(currentVector, vector, 0.42f);
            currentAffect = Classify(targetVector);
            SetGesture(gesture, targetVector.arousal);
            if (immediate)
            {
                currentVector = targetVector;
                face.ApplyAffect(currentVector, true);
            }
        }

        public void SetActionUnit(FacialActionUnit unit, float intensity, bool immediate = false)
        {
            face.SetActionUnit(unit, intensity, immediate);
        }

        public void SetActionUnit(
            FacialActionUnit unit,
            float intensity,
            int sourceId,
            bool immediate = false)
        {
            face.SetActionUnit(unit, intensity, sourceId, immediate);
        }

        public float GetActionUnit(FacialActionUnit unit)
        {
            return face.GetActionUnit(unit);
        }

        public void ReleaseActionUnit(FacialActionUnit unit, bool immediate = false)
        {
            face.ReleaseActionUnit(unit, immediate);
        }

        public void ReleaseActionUnit(
            FacialActionUnit unit,
            int sourceId,
            bool immediate = false)
        {
            face.ReleaseActionUnit(unit, sourceId, immediate);
        }

        public void ClearActionUnitOverrides(bool immediate = false)
        {
            face.ClearActionUnitOverrides(immediate);
        }

        public void ClearActionUnitOverrides(int sourceId, bool immediate = false)
        {
            face.ClearActionUnitOverrides(sourceId, immediate);
        }

        public float GetMaxBlendShapeWeight(params string[] tokens)
        {
            return face == null ? 0f : face.GetMaxWeight(tokens);
        }

        public void SetGesture(BehaviorGesture gesture, float intensity = 0.7f)
        {
            currentGesture = gesture;
            gestureIntensity = Mathf.Clamp01(intensity);
            gestureRandom ??= new System.Random(StableHash(gameObject.name));
            float intervalVariation = Mathf.Lerp(0.72f, 1.28f, (float)gestureRandom.NextDouble());
            nextGestureTime = Time.time + Mathf.Lerp(4.8f, 2.6f, currentVector.arousal) * intervalVariation;
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            if (gestureIntensity < 0.45f)
            {
                if (ambientPerformance && !animator.enabled)
                {
                    return;
                }

                animator.enabled = true;
                animator.speed = 1f;
                animator.Play("Idle", 0, 0f);
                animator.Update(0f);
                animator.speed = 0f;
                return;
            }

            animator.enabled = true;

            string stateName = gesture switch
                {
                    BehaviorGesture.Neutral => "Idle",
                    BehaviorGesture.AvoidGaze => "AvoidGaze",
                    BehaviorGesture.Fidget => "Fidget",
                    BehaviorGesture.Withdraw => "Withdraw",
                    BehaviorGesture.Protest => "Protest",
                    BehaviorGesture.Defiant => "Defiant",
                    BehaviorGesture.DeskTap => "DeskTap",
                    BehaviorGesture.Shield => "Shield",
                    BehaviorGesture.Point => "Point",
                    BehaviorGesture.PushAway => "PushAway",
                    BehaviorGesture.Listen => "Listen",
                    BehaviorGesture.Recover => "Recover",
                    _ => "Idle"
                };
            float transition = Mathf.Lerp(0.42f, 0.16f, gestureIntensity);
            float normalizedTimeOffset = (float)gestureRandom.NextDouble() * 0.85f;
            animator.CrossFadeInFixedTime(stateName, transition, 0, normalizedTimeOffset);
        }

        public void SetAmbientGesture(BehaviorGesture gesture, float intensity, float minimumHoldSeconds)
        {
            ambientPerformance = true;
            SetGesture(gesture, Mathf.Min(intensity, 0.44f));
            nextGestureTime = Mathf.Max(nextGestureTime, Time.time + Mathf.Max(0f, minimumHoldSeconds));
        }

        private void ApplyProceduralGesture()
        {
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chest == null)
            {
                chest = animator.GetBoneTransform(HumanBodyBones.Spine);
            }

            if (chest == null)
            {
                return;
            }

            if (proceduralChest != chest)
            {
                proceduralChest = chest;
                proceduralRotation.Reset();
            }

            float pulse = Mathf.Sin(Time.time * Mathf.Lerp(2.1f, 7.5f, currentVector.arousal) + gestureMotionPhase);
            Quaternion offset = Quaternion.identity;
            if (currentGesture == BehaviorGesture.Fidget || currentGesture == BehaviorGesture.DeskTap)
            {
                offset = Quaternion.AngleAxis(pulse * 2.2f * gestureIntensity, Vector3.forward);
            }
            else if (currentGesture == BehaviorGesture.Shield || currentGesture == BehaviorGesture.Withdraw)
            {
                offset = Quaternion.AngleAxis(7f * gestureIntensity, Vector3.right);
            }
            else if (currentGesture == BehaviorGesture.Point || currentGesture == BehaviorGesture.PushAway)
            {
                offset = Quaternion.AngleAxis(pulse * 3.5f * gestureIntensity, Vector3.up);
            }
            else if (currentGesture == BehaviorGesture.Listen)
            {
                offset = Quaternion.AngleAxis(-2.4f * gestureIntensity, Vector3.right);
            }
            else if (currentGesture == BehaviorGesture.AvoidGaze)
            {
                offset = Quaternion.AngleAxis(4.5f * gestureIntensity, Vector3.right);
            }
            else if (currentGesture == BehaviorGesture.Protest)
            {
                offset = Quaternion.AngleAxis(pulse * 1.4f * gestureIntensity, Vector3.forward)
                    * Quaternion.AngleAxis(-2.2f * gestureIntensity, Vector3.right);
            }
            else if (currentGesture == BehaviorGesture.Neutral)
            {
                offset = Quaternion.AngleAxis(pulse * 0.65f * gestureIntensity, Vector3.right);
            }

            chest.rotation = proceduralRotation.Apply(chest.rotation, offset);
        }

        private static AffectVector VectorFor(StudentAffect affect)
        {
            return affect switch
            {
                StudentAffect.Calm => new AffectVector(0.15f, 0.18f, 0.08f),
                StudentAffect.Uneasy => new AffectVector(-0.35f, 0.52f, -0.28f),
                StudentAffect.Distressed => new AffectVector(-0.78f, 0.82f, -0.58f),
                StudentAffect.Angry => new AffectVector(-0.82f, 0.92f, 0.68f),
                StudentAffect.Recovering => new AffectVector(0.22f, 0.26f, 0.12f),
                _ => new AffectVector(0f, 0.2f, 0f)
            };
        }

        private static StudentAffect Classify(AffectVector vector)
        {
            if (vector.valence > 0.08f && vector.arousal < 0.42f)
            {
                return StudentAffect.Recovering;
            }

            if (vector.valence < -0.55f && vector.arousal > 0.72f)
            {
                return vector.dominance > 0.2f ? StudentAffect.Angry : StudentAffect.Distressed;
            }

            return vector.valence < -0.12f ? StudentAffect.Uneasy : StudentAffect.Calm;
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
