using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace AdieLab.TeacherTraining
{
    public sealed class VoiceDialogueController : MonoBehaviour
    {
        private const int QuestSampleRate = 16000;
        private const int QuestMaxSeconds = 30;
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button microphoneButton;
        [SerializeField] private Button dialogueSendButton;
        [SerializeField] private TMP_Text microphoneLabel;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Color inactiveColor = new Color(0.12f, 0.18f, 0.28f, 0.96f);
        [SerializeField] private Color activeColor = new Color(0.02f, 0.64f, 0.58f, 1f);
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_ANDROID)
        private AudioClip microphoneClip;
#endif

        public bool IsListening
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_ANDROID
                return Microphone.IsRecording(null);
#else
                return false;
#endif
            }
        }

        public static bool HasInputDevice
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_ANDROID
                return Microphone.devices != null && Microphone.devices.Length > 0;
#else
                return false;
#endif
            }
        }

        public static bool HasMicrophonePermission
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
                return HasInputDevice;
#endif
            }
        }

        private void Awake()
        {
            microphoneButton.onClick.AddListener(ToggleListening);
            SetVisualState(false);
        }

        private void OnDestroy()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_ANDROID
            if (Microphone.IsRecording(null)) Microphone.End(null);
            if (microphoneClip != null) Destroy(microphoneClip);
#endif
        }

        private void ToggleListening()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_ANDROID
            ToggleRecording();
#else
            status.text = "현재 빌드에서는 음성 인식을 사용할 수 없습니다.";
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_ANDROID
        private void ToggleRecording()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                status.text = "마이크 권한을 허용한 뒤 다시 눌러 주세요.";
                return;
            }
#endif
            if (Microphone.IsRecording(null))
            {
                int frames = Microphone.GetPosition(null);
                Microphone.End(null);
                SetVisualState(false);
                if (microphoneClip == null || frames <= QuestSampleRate / 4) { status.text = "녹음이 너무 짧습니다 · 다시 말씀해 주세요."; return; }
                byte[] wav;
                try { wav = WavPcm16Encoder.Encode(microphoneClip, frames); }
                catch (InvalidOperationException) { status.text = "녹음 데이터를 읽지 못했습니다."; return; }
                Destroy(microphoneClip); microphoneClip = null;
                StartCoroutine(TranscribeAndSend(wav));
                return;
            }
            microphoneClip = Microphone.Start(null, false, QuestMaxSeconds, QuestSampleRate);
            if (microphoneClip == null) { status.text = "마이크를 시작하지 못했습니다."; return; }
            SetVisualState(true);
            status.text = "마이크 활성 · 말씀한 뒤 다시 눌러 전송";
        }

        private IEnumerator TranscribeAndSend(byte[] wav)
        {
            status.text = "음성을 텍스트로 변환 중…";
            microphoneButton.interactable = false;
            SecureProxyLlmGateway gateway = FindAnyObjectByType<SecureProxyLlmGateway>();
            string transcript = null; string error = null;
            if (gateway == null) error = "보안 음성 프록시를 찾지 못했습니다.";
            else yield return gateway.RequestTranscription(wav, value => transcript = value, value => error = value);
            microphoneButton.interactable = true;
            if (string.IsNullOrWhiteSpace(transcript)) { status.text = error ?? "음성을 인식하지 못했습니다."; yield break; }
            input.text = transcript;
            status.text = "음성 인식 완료 · 학생에게 전송 중…";
            dialogueSendButton.onClick.Invoke();
        }
#endif

        private void SetVisualState(bool active)
        {
            if (microphoneLabel != null) microphoneLabel.text = active ? "녹음 중\n끄기" : "마이크\n켜기";
            if (microphoneButton != null && microphoneButton.targetGraphic != null) microphoneButton.targetGraphic.color = active ? activeColor : inactiveColor;
        }
    }
}
