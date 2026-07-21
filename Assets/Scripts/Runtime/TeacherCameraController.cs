using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public sealed class TeacherCameraController : MonoBehaviour
    {
        [SerializeField] private float lookSensitivity = 1.5f;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField, Min(0f)] private float collisionRadius = 0.26f;
        [SerializeField] private Vector2 pitchLimits = new Vector2(-25f, 35f);
        [Header("Face-to-face conversation")]
        [SerializeField] private Transform focusTarget;
        [SerializeField] private Vector3 focusOffset = new Vector3(0f, 1.40f, 1.35f);
        [SerializeField, Range(30f, 60f)] private float focusFieldOfView = 44f;
        [SerializeField, Min(0.1f)] private float focusResponsiveness = 4.5f;

        private float yaw;
        private float pitch;
        private float orbitYaw;
        private float orbitPitch;
        private bool conversationFocused;
        private bool uprightFocus;
        private Transform focusHead;
        private Camera controlledCamera;
        private float explorationFieldOfView;
        private Vector3 explorationPosition;
        private Quaternion explorationRotation;

        private void Start()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);
            explorationPosition = transform.position;
            explorationRotation = transform.rotation;
            controlledCamera = GetComponent<Camera>();
            explorationFieldOfView = controlledCamera != null ? controlledCamera.fieldOfView : 0f;
            CacheFocusHead();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F) && focusTarget != null)
            {
                if (conversationFocused)
                {
                    ExitConversationFocus();
                }
                else
                {
                    EnterConversationFocus();
                }
            }

            if (conversationFocused && focusTarget != null)
            {
                Vector3 face;
                Vector3 desiredPosition;
                if (focusHead != null)
                {
                    face = focusHead.position + Vector3.up * 0.02f;
                    desiredPosition = face
                        + focusTarget.right * focusOffset.x
                        + focusTarget.forward * focusOffset.z;
                }
                else
                {
                    float focusHeight = uprightFocus ? 1.66f : 1.40f;
                    face = focusTarget.position + Vector3.up * focusHeight;
                    float cameraHeight = uprightFocus ? 1.68f : focusOffset.y;
                    desiredPosition = focusTarget.position
                        + focusTarget.right * focusOffset.x
                        + Vector3.up * cameraHeight
                        + focusTarget.forward * focusOffset.z;
                }
                // Right-mouse drag orbits around the student's face during conversation
                // focus, so the viewpoint stays adjustable while the scene is fixed.
                if (Input.GetMouseButton(1))
                {
                    orbitYaw += Input.GetAxis("Mouse X") * lookSensitivity * 1.6f;
                    orbitPitch -= Input.GetAxis("Mouse Y") * lookSensitivity * 1.2f;
                    orbitYaw = Mathf.Clamp(orbitYaw, -80f, 80f);
                    orbitPitch = Mathf.Clamp(orbitPitch, -18f, 32f);
                }

                Quaternion orbit =
                    Quaternion.AngleAxis(orbitYaw, Vector3.up)
                    * Quaternion.AngleAxis(orbitPitch, focusTarget.right);
                desiredPosition = face + orbit * (desiredPosition - face);

                float blend = 1f - Mathf.Exp(-focusResponsiveness * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(face - desiredPosition),
                    blend);
                if (controlledCamera != null)
                {
                    controlledCamera.fieldOfView = Mathf.Lerp(
                        controlledCamera.fieldOfView,
                        focusFieldOfView,
                        blend);
                }
                return;
            }

            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * lookSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
                pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 movement = (transform.right * horizontal + planarForward * vertical) * (moveSpeed * Time.deltaTime);
            transform.position += ResolveCollisions(movement);
        }

        // Walls, closed doors, and props physically block free roam; axis-split
        // retries let the teacher slide along surfaces instead of stopping dead.
        private Vector3 ResolveCollisions(Vector3 movement)
        {
            if (collisionRadius <= 0f || movement == Vector3.zero)
            {
                return movement;
            }

            if (CanOccupy(transform.position + movement))
            {
                return movement;
            }

            Vector3 xOnly = new Vector3(movement.x, 0f, 0f);
            if (xOnly.x != 0f && CanOccupy(transform.position + xOnly))
            {
                return xOnly;
            }

            Vector3 zOnly = new Vector3(0f, 0f, movement.z);
            if (zOnly.z != 0f && CanOccupy(transform.position + zOnly))
            {
                return zOnly;
            }

            return Vector3.zero;
        }

        private bool CanOccupy(Vector3 target)
        {
            if (!Physics.CheckSphere(target, collisionRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            // Already overlapping (spawn/focus edge cases): allow movement so
            // the teacher can back out instead of freezing in place.
            return Physics.CheckSphere(transform.position, collisionRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        public void SetFocusTarget(Transform target)
        {
            focusTarget = target;
            CacheFocusHead();
        }

        public void SetUprightFocus(bool enabled)
        {
            uprightFocus = enabled;
        }

        public void EnterConversationFocus()
        {
            if (focusTarget == null || conversationFocused)
            {
                return;
            }

            explorationPosition = transform.position;
            explorationRotation = transform.rotation;
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }
            explorationFieldOfView = controlledCamera != null ? controlledCamera.fieldOfView : 0f;
            orbitYaw = 0f;
            orbitPitch = 0f;
            conversationFocused = true;
        }

        public void ExitConversationFocus()
        {
            conversationFocused = false;
            transform.SetPositionAndRotation(explorationPosition, explorationRotation);
            if (controlledCamera != null && explorationFieldOfView > 0f)
            {
                controlledCamera.fieldOfView = explorationFieldOfView;
            }
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private void CacheFocusHead()
        {
            focusHead = null;
            if (focusTarget == null)
            {
                return;
            }

            Animator animator = focusTarget.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                focusHead = animator.GetBoneTransform(HumanBodyBones.Head);
            }
        }
    }
}
