using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Hands-on emotion-card interaction for the recovery room. On Quest the
    /// cards are XR grabbables that snap magnetically into socket zones; on
    /// desktop a click sends the card to its matching zone. Handing a card to
    /// the student and placing the blue signal card are recorded as deterministic
    /// hands-on evidence.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HandsOnCardSystem : MonoBehaviour
    {
        public const string StudentActionId = "handson.emotioncard.offered";
        public const string SignalActionId = "handson.signalcard.placed";
        private const int BlueCardIndex = 4;

        private SimulationController controller;
        private Camera rayCamera;
        private Transform studentAnchor;
        private Transform signalAnchor;
        private readonly List<Transform> cards = new List<Transform>();
        private readonly Dictionary<Transform, Pose> homePoses = new Dictionary<Transform, Pose>();
        private readonly Dictionary<Transform, Transform> occupied = new Dictionary<Transform, Transform>();
        private bool xrMode;

        public static HandsOnCardSystem Install(SimulationController controller, Camera sceneCamera)
        {
            GameObject table = GameObject.Find("RecoveryTable");
            if (table == null || controller == null)
            {
                return null;
            }

            var system = table.GetComponent<HandsOnCardSystem>();
            if (system == null)
            {
                system = table.AddComponent<HandsOnCardSystem>();
            }

            system.controller = controller;
            system.rayCamera = sceneCamera;
            system.Build(table.transform);
            return system;
        }

        private void Build(Transform table)
        {
            studentAnchor = table.Find("StudentCardAnchor");
            signalAnchor = table.Find("SignalCardAnchor");
            xrMode = UnityEngine.XR.XRSettings.isDeviceActive;

            cards.Clear();
            foreach (Transform child in table)
            {
                if (!child.name.StartsWith("EmotionCard_"))
                {
                    continue;
                }

                cards.Add(child);
                homePoses[child] = new Pose(child.position, child.rotation);
                var collider = child.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = child.gameObject.AddComponent<BoxCollider>();
                }

                // Generous pick volume: thin cards are hard to point at.
                collider.size = new Vector3(1.3f, 8f, 1.3f);

                if (xrMode)
                {
                    var body = child.GetComponent<Rigidbody>();
                    if (body == null)
                    {
                        body = child.gameObject.AddComponent<Rigidbody>();
                    }

                    body.isKinematic = true;
                    body.useGravity = false;
                    var grab = child.GetComponent<XRGrabInteractable>();
                    if (grab == null)
                    {
                        grab = child.gameObject.AddComponent<XRGrabInteractable>();
                    }

                    grab.movementType = XRBaseInteractable.MovementType.Kinematic;
                    grab.throwOnDetach = false;
                    grab.selectExited.AddListener(OnXrCardReleased);
                }
            }

            if (xrMode)
            {
                MakeSocket(studentAnchor);
                MakeSocket(signalAnchor);
            }
        }

        private void MakeSocket(Transform anchor)
        {
            if (anchor == null)
            {
                return;
            }

            var trigger = anchor.GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = anchor.gameObject.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.radius = 0.11f;
            var socket = anchor.GetComponent<XRSocketInteractor>();
            if (socket == null)
            {
                socket = anchor.gameObject.AddComponent<XRSocketInteractor>();
            }

            socket.attachTransform = anchor;
            socket.selectEntered.AddListener(args => OnCardSocketed(anchor, args.interactableObject.transform));
        }

        private void OnXrCardReleased(SelectExitEventArgs args)
        {
            // Released in mid-air (no socket claimed it this frame): float home.
            Transform card = args.interactableObject.transform;
            StartCoroutine(ReturnHomeIfUnsocketed(card));
        }

        private IEnumerator ReturnHomeIfUnsocketed(Transform card)
        {
            yield return null;
            yield return null;
            if (!occupied.ContainsValue(card))
            {
                yield return Glide(card, homePoses[card].position, homePoses[card].rotation);
            }
        }

        private void OnCardSocketed(Transform anchor, Transform card)
        {
            occupied[anchor] = card;
            SendHaptics();
            RecordPlacement(anchor, card);
        }

        private void Update()
        {
            if (xrMode || rayCamera == null)
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = rayCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 8f))
            {
                return;
            }

            Transform card = hit.collider.transform;
            if (!cards.Contains(card))
            {
                return;
            }

            int index = cards.IndexOf(card);
            Transform target = index == BlueCardIndex && signalAnchor != null && !occupied.ContainsKey(signalAnchor)
                ? signalAnchor
                : studentAnchor;
            if (target == null)
            {
                return;
            }

            if (occupied.TryGetValue(target, out Transform previous) && previous != card)
            {
                StartCoroutine(Glide(previous, homePoses[previous].position, homePoses[previous].rotation));
            }

            occupied[target] = card;
            StartCoroutine(GlideAndRecord(card, target));
        }

        private IEnumerator GlideAndRecord(Transform card, Transform anchor)
        {
            yield return Glide(card, anchor.position + Vector3.up * 0.004f, anchor.rotation);
            RecordPlacement(anchor, card);
        }

        private IEnumerator Glide(Transform card, Vector3 position, Quaternion rotation)
        {
            Vector3 fromPosition = card.position;
            Quaternion fromRotation = card.rotation;
            const float duration = 0.35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                Vector3 arc = Vector3.up * Mathf.Sin(progress * Mathf.PI) * 0.06f;
                card.position = Vector3.Lerp(fromPosition, position, progress) + arc;
                card.rotation = Quaternion.Slerp(fromRotation, rotation, progress);
                yield return null;
            }

            card.SetPositionAndRotation(position, rotation);
        }

        private void RecordPlacement(Transform anchor, Transform card)
        {
            int index = cards.IndexOf(card);
            if (anchor == signalAnchor && index == BlueCardIndex)
            {
                controller.RecordHandsOnAction(
                    SignalActionId,
                    TeacherCompetency.Safety,
                    "파란 신호 카드를 함께 정한 자리에 놓았습니다 · 도움 요청 약속이 눈에 보이게 되었습니다");
            }
            else if (anchor == studentAnchor)
            {
                controller.RecordHandsOnAction(
                    StudentActionId,
                    TeacherCompetency.StudentAgency,
                    "감정 카드를 학생 앞에 건넸습니다 · 학생이 고를 수 있게 되었습니다");
            }
        }

        private void SendHaptics()
        {
            var devices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Controller, devices);
            foreach (var device in devices)
            {
                if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                {
                    device.SendHapticImpulse(0, 0.45f, 0.08f);
                }
            }
        }
    }
}
