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
    /// Beat-4 coping-plan assembly: strategy cards snap into the three slots of
    /// the "우리의 약속" easel board. Quest uses XR grab + magnetic sockets with
    /// haptics; desktop clicks glide a card into the first free slot (clicking a
    /// pinned card returns it to the tray, so the choice stays revisable). The
    /// first pin records Agency evidence; completing all three records Reentry.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrategyBoardSystem : MonoBehaviour
    {
        public const string PinActionId = "handson.strategy.pinned";
        public const string CompleteActionId = "handson.strategyboard.completed";

        private SimulationController controller;
        private Camera rayCamera;
        private readonly List<Transform> cards = new List<Transform>();
        private readonly List<Transform> slots = new List<Transform>();
        private readonly Dictionary<Transform, Pose> homePoses = new Dictionary<Transform, Pose>();
        private readonly Dictionary<Transform, Transform> slotContents = new Dictionary<Transform, Transform>();
        private readonly Dictionary<Transform, Transform> labels = new Dictionary<Transform, Transform>();
        private readonly Dictionary<Transform, Pose> labelOffsets = new Dictionary<Transform, Pose>();
        private bool xrMode;

        public static StrategyBoardSystem Install(SimulationController controller, Camera sceneCamera)
        {
            GameObject board = GameObject.Find("StrategyBoard");
            if (board == null || controller == null)
            {
                return null;
            }

            var system = board.GetComponent<StrategyBoardSystem>();
            if (system == null)
            {
                system = board.AddComponent<StrategyBoardSystem>();
            }

            system.controller = controller;
            system.rayCamera = sceneCamera;
            system.Build(board.transform);
            return system;
        }

        private void Build(Transform board)
        {
            xrMode = UnityEngine.XR.XRSettings.isDeviceActive;
            cards.Clear();
            slots.Clear();

            Transform panel = board.Find("Face");
            if (panel == null)
            {
                panel = board.Find("Panel");
            }
            if (panel != null)
            {
                foreach (Transform child in panel)
                {
                    if (child.name.StartsWith("StrategySlot_"))
                    {
                        slots.Add(child);
                        if (xrMode)
                        {
                            MakeSocket(child);
                        }
                    }
                }
            }

            foreach (Transform child in board)
            {
                if (!child.name.StartsWith("StrategyCard_"))
                {
                    continue;
                }

                cards.Add(child);
                homePoses[child] = new Pose(child.position, child.rotation);
                Transform label = board.Find("StrategyCardLabel_" + child.name.Substring("StrategyCard_".Length));
                if (label != null)
                {
                    labels[child] = label;
                    labelOffsets[child] = new Pose(
                        Quaternion.Inverse(child.rotation) * (label.position - child.position),
                        Quaternion.Inverse(child.rotation) * label.rotation);
                }
                var collider = child.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = child.gameObject.AddComponent<BoxCollider>();
                }

                collider.size = new Vector3(1.25f, 9f, 1.6f);

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

            slots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        private void MakeSocket(Transform slot)
        {
            var trigger = slot.GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = slot.gameObject.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.radius = 0.1f;
            var socket = slot.GetComponent<XRSocketInteractor>();
            if (socket == null)
            {
                socket = slot.gameObject.AddComponent<XRSocketInteractor>();
            }

            socket.attachTransform = slot;
            socket.selectEntered.AddListener(args => OnPinned(slot, args.interactableObject.transform));
        }

        private void OnXrCardReleased(SelectExitEventArgs args)
        {
            Transform card = args.interactableObject.transform;
            StartCoroutine(ReturnHomeIfUnpinned(card));
        }

        private IEnumerator ReturnHomeIfUnpinned(Transform card)
        {
            yield return null;
            yield return null;
            if (!slotContents.ContainsValue(card))
            {
                yield return Glide(card, homePoses[card].position, homePoses[card].rotation);
            }
        }

        private void Update()
        {
            if (xrMode || rayCamera == null || !Input.GetMouseButtonDown(0))
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

            Transform currentSlot = null;
            foreach (var pair in slotContents)
            {
                if (pair.Value == card)
                {
                    currentSlot = pair.Key;
                    break;
                }
            }

            if (currentSlot != null)
            {
                // Revising the plan: clicking a pinned card returns it to the tray.
                slotContents.Remove(currentSlot);
                StartCoroutine(Glide(card, homePoses[card].position, homePoses[card].rotation));
                return;
            }

            Transform freeSlot = null;
            foreach (Transform slot in slots)
            {
                if (!slotContents.ContainsKey(slot))
                {
                    freeSlot = slot;
                    break;
                }
            }

            if (freeSlot == null)
            {
                return;
            }

            slotContents[freeSlot] = card;
            StartCoroutine(GlideAndRecord(card, freeSlot));
        }

        private IEnumerator GlideAndRecord(Transform card, Transform slot)
        {
            yield return Glide(card, slot.position + slot.forward * -0.02f, slot.rotation * Quaternion.Euler(90f, 0f, 0f));
            OnPinned(slot, card);
        }

        private void OnPinned(Transform slot, Transform card)
        {
            slotContents[slot] = card;
            SendHaptics();
            controller.RecordHandsOnAction(
                PinActionId,
                TeacherCompetency.StudentAgency,
                "대처 전략을 계획판에 붙였습니다 · 학생과 함께 고른 전략입니다");
            if (slotContents.Count >= slots.Count && slots.Count > 0)
            {
                controller.RecordHandsOnAction(
                    CompleteActionId,
                    TeacherCompetency.InstructionalReentry,
                    "우리의 약속 계획판이 완성되었습니다 · 복귀 준비가 눈에 보입니다");
            }
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
                Vector3 arc = Vector3.up * Mathf.Sin(progress * Mathf.PI) * 0.05f;
                card.position = Vector3.Lerp(fromPosition, position, progress) + arc;
                card.rotation = Quaternion.Slerp(fromRotation, rotation, progress);
                yield return null;
            }

            card.SetPositionAndRotation(position, rotation);
        }

        private void LateUpdate()
        {
            // Labels ride along with their cards (kept unparented so the cards'
            // non-uniform scale never distorts the TMP text).
            foreach (var pair in labels)
            {
                Pose offset = labelOffsets[pair.Key];
                pair.Value.SetPositionAndRotation(
                    pair.Key.position + pair.Key.rotation * offset.position,
                    pair.Key.rotation * offset.rotation);
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
