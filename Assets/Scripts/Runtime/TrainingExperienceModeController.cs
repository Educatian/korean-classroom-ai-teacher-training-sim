using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Management;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class TrainingExperienceModeController : MonoBehaviour
    {
        [SerializeField] private Button modeToggleButton;
        [SerializeField] private TMP_Text modeToggleLabel;
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private Camera teacherCamera;

        private readonly XrTeacherRigAdapter xrRig = new XrTeacherRigAdapter();
        private bool transitionInProgress;

        public TrainingExperienceMode CurrentMode { get; private set; } = TrainingExperienceMode.Desktop;
        public string Status { get; private set; } = "DESKTOP";
        public bool IsXrActive => xrRig.IsEnabled;

        private void Awake()
        {
            modeToggleButton?.onClick.AddListener(ToggleMode);
            TrainingExperienceMode requested = TrainingExperienceModePolicy.Load(Application.platform);
            StartCoroutine(ApplyMode(requested));
        }

        private void OnDestroy()
        {
            modeToggleButton?.onClick.RemoveListener(ToggleMode);
            xrRig.Disable();
        }

        public void ToggleMode()
        {
            if (transitionInProgress)
            {
                return;
            }

            TrainingExperienceMode next = CurrentMode == TrainingExperienceMode.Desktop
                ? TrainingExperienceMode.ImmersiveVr
                : TrainingExperienceMode.Desktop;
            StartCoroutine(ApplyMode(next));
        }

        public IEnumerator ApplyMode(TrainingExperienceMode requested)
        {
            if (transitionInProgress)
            {
                yield break;
            }

            transitionInProgress = true;
            SetToggleInteractable(false);
            if (requested == TrainingExperienceMode.ImmersiveVr)
            {
                SetStatus("IVR 연결 중");
                yield return null;
                if (!TryStartXr())
                {
                    requested = TrainingExperienceMode.Desktop;
                    SetStatus("DESKTOP · IVR 미연결");
                }
            }

            if (requested == TrainingExperienceMode.Desktop)
            {
                xrRig.Disable();
                StopXr();
                SetStatus(Status.Contains("미연결", StringComparison.Ordinal) ? Status : "DESKTOP");
            }

            CurrentMode = requested;
            TrainingExperienceModePolicy.Save(CurrentMode);
            SetToggleInteractable(true);
            transitionInProgress = false;
        }

        private void SetToggleInteractable(bool interactable)
        {
            if (modeToggleButton != null)
            {
                modeToggleButton.interactable = interactable;
            }
        }

        private bool TryStartXr()
        {
            XRGeneralSettings settings = XRGeneralSettings.Instance;
            if (settings == null || settings.Manager == null)
            {
                return false;
            }

            try
            {
                if (settings.Manager.activeLoader == null)
                {
                    settings.Manager.InitializeLoaderSync();
                }
                if (settings.Manager.activeLoader == null)
                {
                    return false;
                }

                settings.Manager.StartSubsystems();
                xrRig.Enable(teacherCamera, hudCanvas);
                SetStatus("IVR · QUEST");
                return xrRig.IsEnabled;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"IVR initialization failed. Desktop mode remains active. {exception.Message}");
                return false;
            }
        }

        private static void StopXr()
        {
            XRGeneralSettings settings = XRGeneralSettings.Instance;
            if (settings?.Manager == null || settings.Manager.activeLoader == null)
            {
                return;
            }

            settings.Manager.StopSubsystems();
            settings.Manager.DeinitializeLoader();
        }

        private void SetStatus(string value)
        {
            Status = value;
            if (modeToggleLabel != null)
            {
                modeToggleLabel.text = value;
            }
        }
    }
}
