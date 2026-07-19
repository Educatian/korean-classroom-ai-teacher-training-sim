using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class ChinRestDeskContactController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float blendSpeed = 4.5f;
        [SerializeField] private Vector3 chinOffset = new Vector3(0.015f, -0.055f, 0.025f);
        [SerializeField] private Vector3 deskContactOffset = new Vector3(0.12f, 0.034f, -0.16f);
        [SerializeField, Range(0f, 0.16f)] private float forwardSlideDistance = 0.105f;

        private Animator animator;
        private NpcIdleBehaviorController behavior;
        private Transform studentRoot;
        private Transform desktop;
        private Transform head;
        private Transform rightHand;
        private Transform rightElbow;
        private float contactWeight;
        private Vector3 chinTarget;
        private Vector3 elbowTarget;
        private Vector3 leftHandTarget;
        private Vector3 leftElbowTarget;
        private Vector3 baseStudentLocalPosition;

        public bool HasDeskSurface => desktop != null;
        public float ContactWeight => contactWeight;
        public float HandChinGap { get; private set; } = float.PositiveInfinity;
        public float ElbowDeskGap { get; private set; } = float.PositiveInfinity;
        public Vector3 ElbowDesktopLocal { get; private set; }
        public bool ElbowOverDesktop => ElbowDesktopLocal.x >= -0.44f && ElbowDesktopLocal.x <= 0.44f
            && ElbowDesktopLocal.z >= -0.28f && ElbowDesktopLocal.z <= 0.28f;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            CacheBones();
        }

        public void Configure(NpcIdleBehaviorController source)
        {
            behavior = source;
            studentRoot = source != null ? source.transform : null;
            baseStudentLocalPosition = studentRoot != null ? studentRoot.localPosition : Vector3.zero;
            FindNearestDesktop();
            CacheBones();
        }

        private void Update()
        {
            float targetWeight = behavior != null && behavior.CurrentBehavior == NpcIdleBehavior.ChinRest ? 1f : 0f;
            contactWeight = Mathf.MoveTowards(contactWeight, targetWeight, blendSpeed * Time.deltaTime);
            ApplyForwardSlide();
            UpdateTargets();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || desktop == null || head == null || contactWeight <= 0.001f)
            {
                return;
            }

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, contactWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, chinTarget);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, contactWeight);
            animator.SetIKHintPosition(AvatarIKHint.RightElbow, elbowTarget);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, contactWeight * 0.82f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, contactWeight * 0.70f);
            animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftElbowTarget);
        }

        private void LateUpdate()
        {
            if (contactWeight <= 0.001f || rightHand == null || rightElbow == null)
            {
                HandChinGap = float.PositiveInfinity;
                ElbowDeskGap = float.PositiveInfinity;
                return;
            }

            HandChinGap = Vector3.Distance(rightHand.position, chinTarget);
            ElbowDesktopLocal = desktop != null ? desktop.InverseTransformPoint(rightElbow.position) : Vector3.zero;
            Vector3 nearestSurfaceLocal = new Vector3(
                Mathf.Clamp(ElbowDesktopLocal.x, -0.48f, 0.48f),
                0.034f,
                Mathf.Clamp(ElbowDesktopLocal.z, -0.31f, 0.31f));
            ElbowDeskGap = desktop != null
                ? Vector3.Distance(rightElbow.position, desktop.TransformPoint(nearestSurfaceLocal))
                : float.PositiveInfinity;
        }

        private void ApplyForwardSlide()
        {
            if (studentRoot == null || desktop == null || studentRoot.parent == null)
            {
                return;
            }

            Vector3 towardDesk = desktop.position - studentRoot.position;
            towardDesk.y = 0f;
            if (towardDesk.sqrMagnitude > 0.0001f)
            {
                towardDesk.Normalize();
            }

            Vector3 localOffset = studentRoot.parent.InverseTransformVector(towardDesk * forwardSlideDistance * contactWeight);
            studentRoot.localPosition = baseStudentLocalPosition + localOffset;
        }

        private void UpdateTargets()
        {
            if (desktop == null || head == null || studentRoot == null)
            {
                return;
            }

            Vector3 towardDesk = desktop.position - studentRoot.position;
            towardDesk.y = 0f;
            if (towardDesk.sqrMagnitude < 0.0001f)
            {
                towardDesk = studentRoot.forward;
            }
            else
            {
                towardDesk.Normalize();
            }

            Vector3 side = Vector3.Cross(Vector3.up, towardDesk).normalized;
            chinTarget = head.position + Vector3.up * chinOffset.y + towardDesk * chinOffset.z + side * chinOffset.x;
            elbowTarget = desktop.TransformPoint(deskContactOffset);
            leftHandTarget = desktop.TransformPoint(new Vector3(-0.16f, 0.065f, -0.08f));
            leftElbowTarget = desktop.TransformPoint(new Vector3(-0.26f, 0.055f, -0.24f));
        }

        private void CacheBones()
        {
            if (animator == null || !animator.isHuman)
            {
                return;
            }

            head = animator.GetBoneTransform(HumanBodyBones.Head);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            rightElbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        }

        private void FindNearestDesktop()
        {
            desktop = null;
            if (studentRoot == null)
            {
                return;
            }

            float nearestDistance = float.PositiveInfinity;
            Transform[] candidates = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (Transform candidate in candidates)
            {
                if (candidate.name != "Desktop")
                {
                    continue;
                }

                Vector3 delta = candidate.position - studentRoot.position;
                delta.y = 0f;
                float distance = delta.sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    desktop = candidate;
                }
            }
        }
    }
}