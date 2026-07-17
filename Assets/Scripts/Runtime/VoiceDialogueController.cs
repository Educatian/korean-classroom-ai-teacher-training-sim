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
        [SerializeField] private TMP_Text status;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private DictationRecognizer recognizer;
#endif

        private void Awake()
        {
            microphoneButton.onClick.AddListener(ToggleListening);
        }

        private void OnDestroy()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (recognizer != null)
            {
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
                recognizer.DictationResult += (text, confidence) => input.text = text;
                recognizer.DictationComplete += cause => status.text = "음성 입력 완료 · 확인 후 말하기";
                recognizer.DictationError += (error, hresult) => status.text = $"음성 입력 오류 · {error}";
            }

            if (recognizer.Status == SpeechSystemStatus.Running)
            {
                recognizer.Stop();
                status.text = "음성 입력 완료 · 확인 후 말하기";
            }
            else
            {
                recognizer.Start();
                status.text = "듣고 있습니다… 다시 눌러 완료";
            }
#else
            status.text = "Windows 빌드에서 음성 입력을 사용할 수 있습니다.";
#endif
        }
    }
}
