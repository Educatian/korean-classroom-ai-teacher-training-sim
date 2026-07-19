using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

namespace AdieLab.TeacherTraining
{
    public sealed class VoiceDialogueController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button microphoneButton;
        [SerializeField] private Button dialogueSendButton;
        [SerializeField] private TMP_Text microphoneLabel;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Color inactiveColor = new Color(0.12f, 0.18f, 0.28f, 0.96f);
        [SerializeField] private Color activeColor = new Color(0.02f, 0.64f, 0.58f, 1f);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private DictationRecognizer recognizer;
#endif

        public bool IsListening
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return recognizer != null && recognizer.Status == SpeechSystemStatus.Running;
#else
                return false;
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
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (recognizer != null)
            {
                recognizer.DictationResult -= HandleDictationResult;
                recognizer.DictationComplete -= HandleDictationComplete;
                recognizer.DictationError -= HandleDictationError;
                recognizer.Stop();
                recognizer.Dispose();
            }
#endif
        }

        private void ToggleListening()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (recognizer == null)
            {
                recognizer = new DictationRecognizer();
                recognizer.DictationResult += HandleDictationResult;
                recognizer.DictationComplete += HandleDictationComplete;
                recognizer.DictationError += HandleDictationError;
            }

            if (recognizer.Status == SpeechSystemStatus.Running)
            {
                recognizer.Stop();
                SetVisualState(false);
                if (string.IsNullOrWhiteSpace(input.text))
                {
                    status.text = "음성이 인식되지 않았습니다 · 다시 시도해 주세요";
                    return;
                }

                status.text = "음성 입력 완료 · 학생에게 전송 중…";
                dialogueSendButton.onClick.Invoke();
            }
            else
            {
                try
                {
                    recognizer.Start();
                    SetVisualState(true);
                    status.text = "마이크 활성 · 말씀한 뒤 다시 눌러 전송";
                }
                catch (System.Exception exception)
                {
                    SetVisualState(false);
                    status.text = $"마이크를 시작할 수 없습니다 · {exception.Message}";
                }
            }
#else
            status.text = "현재 빌드에서는 음성 인식을 사용할 수 없습니다.";
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private void HandleDictationResult(string text, ConfidenceLevel confidence)
        {
            input.text = text.Trim();
            status.text = "음성 인식됨 · 다시 눌러 학생에게 전송";
        }

        private void HandleDictationComplete(DictationCompletionCause cause)
        {
            SetVisualState(false);
            if (cause != DictationCompletionCause.Complete && cause != DictationCompletionCause.TimeoutExceeded)
            {
                status.text = $"음성 입력 종료 · {cause}";
            }
        }

        private void HandleDictationError(string error, int hresult)
        {
            SetVisualState(false);
            status.text = $"음성 입력 오류 · {error}";
        }
#endif

        private void SetVisualState(bool active)
        {
            if (microphoneLabel != null)
            {
                microphoneLabel.text = active ? "녹음 중\n끄기" : "마이크\n켜기";
            }

            if (microphoneButton != null && microphoneButton.targetGraphic != null)
            {
                microphoneButton.targetGraphic.color = active ? activeColor : inactiveColor;
            }
        }
    }
}
