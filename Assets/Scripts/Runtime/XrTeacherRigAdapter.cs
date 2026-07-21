using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace AdieLab.TeacherTraining
{
    public sealed class XrTeacherRigAdapter
    {
        private readonly List<InputAction> actions = new List<InputAction>();
        private Camera teacherCamera;
        private Canvas hudCanvas;
        private Transform originalCameraParent;
        private int originalCameraSiblingIndex;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private RenderMode originalCanvasMode;
        private Camera originalCanvasCamera;
        private Vector3 originalCanvasPosition;
        private Quaternion originalCanvasRotation;
        private Vector3 originalCanvasScale;
        private GameObject xrRoot;
        private StandaloneInputModule desktopInputModule;
        private GraphicRaycaster desktopRaycaster;
        private XRUIInputModule xrInputModule;
        private TrackedDeviceGraphicRaycaster xrRaycaster;
        private TrackedPoseDriver headPoseDriver;
        private bool ownsXrInputModule;
        private bool ownsXrRaycaster;

        public bool IsEnabled => xrRoot != null;
        public QuestProEyeGazeProvider EyeGazeProvider { get; private set; }

        public void Enable(Camera camera, Canvas canvas)
        {
            if (IsEnabled || camera == null || canvas == null)
            {
                return;
            }

            teacherCamera = camera;
            hudCanvas = canvas;
            SaveDesktopState();
            BuildOrigin();
            ConfigureWorldSpaceHud();
        }

        public void Disable()
        {
            if (!IsEnabled)
            {
                return;
            }

            foreach (InputAction action in actions)
            {
                action.Disable();
                action.Dispose();
            }
            actions.Clear();

            if (headPoseDriver != null)
            {
                headPoseDriver.enabled = false;
                Object.Destroy(headPoseDriver);
                headPoseDriver = null;
            }

            teacherCamera.transform.SetParent(originalCameraParent, true);
            teacherCamera.transform.SetSiblingIndex(originalCameraSiblingIndex);
            teacherCamera.transform.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
            TeacherCameraController controller = teacherCamera.GetComponent<TeacherCameraController>();
            if (controller != null)
            {
                controller.enabled = true;
            }
            TeacherFootstepAudio footsteps = teacherCamera.GetComponent<TeacherFootstepAudio>();
            if (footsteps != null)
            {
                footsteps.enabled = true;
            }

            hudCanvas.renderMode = originalCanvasMode;
            hudCanvas.worldCamera = originalCanvasCamera;
            hudCanvas.transform.SetPositionAndRotation(originalCanvasPosition, originalCanvasRotation);
            hudCanvas.transform.localScale = originalCanvasScale;
            if (desktopRaycaster != null)
            {
                desktopRaycaster.enabled = true;
            }
            if (desktopInputModule != null)
            {
                desktopInputModule.enabled = true;
            }
            ReleaseXrUiComponents();

            EyeGazeProvider = null;
            Object.Destroy(xrRoot);
            xrRoot = null;
        }

        private void SaveDesktopState()
        {
            Transform cameraTransform = teacherCamera.transform;
            originalCameraParent = cameraTransform.parent;
            originalCameraSiblingIndex = cameraTransform.GetSiblingIndex();
            originalCameraPosition = cameraTransform.position;
            originalCameraRotation = cameraTransform.rotation;
            originalCanvasMode = hudCanvas.renderMode;
            originalCanvasCamera = hudCanvas.worldCamera;
            originalCanvasPosition = hudCanvas.transform.position;
            originalCanvasRotation = hudCanvas.transform.rotation;
            originalCanvasScale = hudCanvas.transform.localScale;
        }

        private void BuildOrigin()
        {
            Vector3 start = teacherCamera.transform.position;
            float heading = teacherCamera.transform.eulerAngles.y;
            xrRoot = new GameObject("XR Teacher Origin", typeof(XROrigin), typeof(XRInteractionManager));
            xrRoot.transform.SetPositionAndRotation(new Vector3(start.x, 0f, start.z), Quaternion.Euler(0f, heading, 0f));
            GameObject offset = new GameObject("Camera Floor Offset");
            offset.transform.SetParent(xrRoot.transform, false);

            teacherCamera.transform.SetParent(offset.transform, false);
            teacherCamera.transform.localPosition = Vector3.zero;
            teacherCamera.transform.localRotation = Quaternion.identity;
            headPoseDriver = ConfigureTrackedPose(
                teacherCamera.gameObject,
                "<XRHMD>/centerEyePosition",
                "<XRHMD>/centerEyeRotation",
                "Head");
            TeacherCameraController controller = teacherCamera.GetComponent<TeacherCameraController>();
            if (controller != null)
            {
                controller.enabled = false;
            }
            TeacherFootstepAudio footsteps = teacherCamera.GetComponent<TeacherFootstepAudio>();
            if (footsteps != null)
            {
                footsteps.enabled = false;
            }

            XROrigin origin = xrRoot.GetComponent<XROrigin>();
            origin.Origin = xrRoot;
            origin.CameraFloorOffsetObject = offset;
            origin.Camera = teacherCamera;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            BuildController(offset.transform, "Left Controller", "LeftHand");
            BuildController(offset.transform, "Right Controller", "RightHand");
            ConfigureEventSystem();
            EyeGazeProvider = xrRoot.AddComponent<QuestProEyeGazeProvider>();
            EyeTrackingResearchSettings researchSettings = EyeTrackingResearchSettings.LoadDefault();
            EyeGazeProvider.Configure(
                xrRoot.transform, teacherCamera,
                researchSettings.allowHeadGazeFallback);
        }

        private void BuildController(Transform parent, string name, string hand)
        {
            GameObject controller = new GameObject(name);
            controller.transform.SetParent(parent, false);
            ConfigureTrackedPose(
                controller,
                $"<XRController>{{{hand}}}/devicePosition",
                $"<XRController>{{{hand}}}/deviceRotation",
                hand);

            XRRayInteractor ray = controller.AddComponent<XRRayInteractor>();
            ray.maxRaycastDistance = 8f;
            ray.enableUIInteraction = true;
            ray.selectInput = ButtonReader($"{hand} Select", $"<XRController>{{{hand}}}/gripPressed");
            ray.uiPressInput = ButtonReader($"{hand} UI Press", $"<XRController>{{{hand}}}/triggerPressed");
            LineRenderer line = controller.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = 0.006f;
            line.endWidth = 0.002f;
            XRInteractorLineVisual visual = controller.AddComponent<XRInteractorLineVisual>();
            visual.lineWidth = 0.006f;
            visual.lineLength = 8f;
        }

        private XRInputButtonReader ButtonReader(string name, string binding)
        {
            InputAction action = new InputAction(name, InputActionType.Button, binding);
            action.Enable();
            actions.Add(action);
            return new XRInputButtonReader(name, inputSourceMode: XRInputButtonReader.InputSourceMode.InputAction)
            {
                inputActionPerformed = action,
                inputActionValue = action
            };
        }

        private TrackedPoseDriver ConfigureTrackedPose(
            GameObject target,
            string positionBinding,
            string rotationBinding,
            string prefix)
        {
            InputAction position = new InputAction($"{prefix} Position", InputActionType.Value, positionBinding, expectedControlType: "Vector3");
            InputAction rotation = new InputAction($"{prefix} Rotation", InputActionType.Value, rotationBinding, expectedControlType: "Quaternion");
            position.Enable();
            rotation.Enable();
            actions.Add(position);
            actions.Add(rotation);
            TrackedPoseDriver driver = target.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(position);
            driver.rotationInput = new InputActionProperty(rotation);
            return driver;
        }

        private void ConfigureEventSystem()
        {
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
                eventSystem.transform.SetParent(xrRoot.transform, false);
            }
            desktopInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (desktopInputModule != null)
            {
                desktopInputModule.enabled = false;
            }
            xrInputModule = eventSystem.GetComponent<XRUIInputModule>();
            ownsXrInputModule = xrInputModule == null;
            if (ownsXrInputModule)
            {
                xrInputModule = eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }
            xrInputModule.enabled = true;
        }

        private void ConfigureWorldSpaceHud()
        {
            desktopRaycaster = hudCanvas.GetComponent<GraphicRaycaster>();
            if (desktopRaycaster != null)
            {
                desktopRaycaster.enabled = false;
            }
            xrRaycaster = hudCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            ownsXrRaycaster = xrRaycaster == null;
            if (ownsXrRaycaster)
            {
                xrRaycaster = hudCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            xrRaycaster.enabled = true;

            Transform cameraTransform = teacherCamera.transform;
            Vector3 flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            hudCanvas.renderMode = RenderMode.WorldSpace;
            hudCanvas.worldCamera = teacherCamera;
            RectTransform rect = (RectTransform)hudCanvas.transform;
            rect.sizeDelta = new Vector2(1600f, 900f);
            rect.localScale = Vector3.one * 0.001f;
            rect.position = cameraTransform.position + flatForward * 1.85f;
            rect.rotation = Quaternion.LookRotation(flatForward);
        }

        private void ReleaseXrUiComponents()
        {
            if (xrInputModule != null)
            {
                xrInputModule.enabled = false;
                if (ownsXrInputModule)
                {
                    Object.Destroy(xrInputModule);
                }
            }
            if (xrRaycaster != null)
            {
                xrRaycaster.enabled = false;
                if (ownsXrRaycaster)
                {
                    Object.Destroy(xrRaycaster);
                }
            }

            xrInputModule = null;
            xrRaycaster = null;
            ownsXrInputModule = false;
            ownsXrRaycaster = false;
        }
    }
}
