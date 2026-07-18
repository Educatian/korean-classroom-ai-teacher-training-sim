using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    public sealed class TrainingHud : MonoBehaviour
    {
        [Header("Scenario")]
        [SerializeField] private TMP_Text beatLabel;
        [SerializeField] private TMP_Text studentLine;
        [SerializeField] private TMP_Text observationText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text responseChipLabel;

        [Header("Controls")]
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TMP_Text[] optionLabels;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_InputField dialogueInput;
        [SerializeField] private Button dialogueSendButton;
        [SerializeField] private TMP_Text dialogueStatus;
        [SerializeField] private RectTransform dialoguePanel;
        [Header("Student speech bubble")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform speechTarget;
        [SerializeField] private RectTransform speechBubble;
        [SerializeField] private TMP_Text speechBubbleText;
        [SerializeField] private CanvasGroup speechBubbleGroup;
        private bool speechBubbleAvoidsFace;

        public event Action<int> OptionSelected;
        public event Action ContinueSelected;
        public event Action<string> TeacherUtteranceSubmitted;

        private void Awake()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int captured = i;
                optionButtons[i].onClick.AddListener(() => OptionSelected?.Invoke(captured));
            }

            continueButton.onClick.AddListener(() => ContinueSelected?.Invoke());
            dialogueSendButton.onClick.AddListener(SubmitDialogue);
            dialogueInput.onEndEdit.AddListener(value =>
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    SubmitDialogue();
                }
            });
            speechBubble.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!speechBubble.gameObject.activeSelf || speechTarget == null || worldCamera == null)
            {
                return;
            }

            RectTransform canvasRect = (RectTransform)rootCanvas.transform;
            Vector3 viewport = worldCamera.WorldToViewportPoint(speechTarget.position);
            if (viewport.z <= 0f)
            {
                speechBubbleGroup.alpha = 0f;
                return;
            }

            if (speechBubbleGroup.alpha <= 0f)
            {
                speechBubbleGroup.alpha = 1f;
            }

            Vector2 local = new Vector2(
                Mathf.Lerp(canvasRect.rect.xMin, canvasRect.rect.xMax, viewport.x),
                Mathf.Lerp(canvasRect.rect.yMin, canvasRect.rect.yMax, viewport.y));
            float halfWidth = speechBubble.rect.width * 0.5f;
            float halfHeight = speechBubble.rect.height * 0.5f;
            float verticalOffset = halfHeight + (speechBubbleAvoidsFace ? 178f : 118f);
            Vector2 desired = local + new Vector2(81f, verticalOffset);
            desired.x = Mathf.Clamp(desired.x, canvasRect.rect.xMin + halfWidth + 20f, canvasRect.rect.xMax - halfWidth - 20f);
            desired.y = Mathf.Clamp(desired.y, canvasRect.rect.yMin + halfHeight + 130f, canvasRect.rect.yMax - halfHeight - 80f);
            Vector2 next = speechBubbleAvoidsFace
                ? desired
                : Vector2.Lerp(speechBubble.anchoredPosition, desired, 1f - Mathf.Exp(-12f * Time.deltaTime));
            speechBubble.anchoredPosition = new Vector2(Mathf.Round(next.x), Mathf.Round(next.y));
        }

        private void SubmitDialogue()
        {
            string utterance = dialogueInput.text.Trim();
            if (string.IsNullOrWhiteSpace(utterance) || !dialogueInput.interactable)
            {
                return;
            }

            TeacherUtteranceSubmitted?.Invoke(utterance);
        }

        public void SetDialogueState(bool waiting, string status)
        {
            dialogueInput.interactable = !waiting;
            dialogueSendButton.interactable = !waiting;
            dialogueStatus.text = status;
            if (!waiting)
            {
                dialogueInput.text = string.Empty;
                dialogueInput.ActivateInputField();
            }
        }

        public void SetSpeechBubbleAvoidsFace(bool enabled)
        {
            speechBubbleAvoidsFace = enabled;
        }

        public void ShowStudentTurn(string teacherUtterance, string studentReply, AffectVector affect)
        {
            studentLine.text = $"학생: “{KeepKoreanPhrasesTogether(studentReply)}”";
            speechBubbleText.text = KeepKoreanPhrasesTogether(studentReply);
            speechBubble.gameObject.SetActive(true);
            StopCoroutine(nameof(AnimateSpeechBubble));
            StartCoroutine(nameof(AnimateSpeechBubble));
            observationText.text =
                $"<b><color=#FFFFFF>교사 · “{KeepKoreanPhrasesTogether(teacherUtterance)}”</color></b>\n" +
                $"<color=#BFE7DF>정서가 {affect.valence:+0.00;-0.00;0.00} · 각성 {affect.arousal:0.00} · 주도성 {affect.dominance:+0.00;-0.00;0.00}</color>";
        }

        private System.Collections.IEnumerator AnimateSpeechBubble()
        {
            const float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                speechBubbleGroup.alpha = progress;
                speechBubble.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, progress);
                yield return null;
            }

            speechBubbleGroup.alpha = 1f;
            speechBubble.localScale = Vector3.one;
        }

        public void ShowBeat(int index, int total, ScenarioBeat beat, int score, int completed)
        {
            speechBubble.gameObject.SetActive(false);
            dialoguePanel.gameObject.SetActive(true);
            responseChipLabel.text = "대응 선택";
            feedbackText.alignment = TextAlignmentOptions.TopLeft;
            feedbackText.fontSize = 18;
            feedbackText.rectTransform.anchorMin = new Vector2(0f, 0.65f);
            feedbackText.rectTransform.anchorMax = Vector2.one;
            feedbackText.rectTransform.offsetMin = new Vector2(24f, 12f);
            feedbackText.rectTransform.offsetMax = new Vector2(-24f, -50f);
            beatLabel.text = $"상황 {index + 1}/{total}  ·  {beat.title}";
            studentLine.text = $"학생: “{KeepKoreanPhrasesTogether(beat.studentLine)}”";
            observationText.text = beat.observation;
            feedbackText.text = "학생의 정서 신호와 교실 맥락을 살핀 뒤 대응을 선택하세요.";
            scoreText.text = $"공동조절 점수 {score}  ·  {ResponseScorer.GetLevel(score, completed)}";

            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool available = i < beat.options.Length;
                optionButtons[i].gameObject.SetActive(available);
                if (!available)
                {
                    continue;
                }

                optionButtons[i].interactable = true;
                optionLabels[i].text = $"{i + 1}. {beat.options[i].label}";
            }

            continueButton.gameObject.SetActive(false);
        }

        public void ShowFeedback(TeacherResponseOption option, int score, int completed)
        {
            string marker = option.quality == 3 ? "효과적인 대응" : option.quality == 2 ? "부분적으로 효과적" : "재구성이 필요한 대응";
            feedbackText.text = $"<b>{marker}</b>\n{option.rationale}";
            scoreText.text = $"공동조절 점수 {score}  ·  {ResponseScorer.GetLevel(score, completed)}";
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].interactable = false;
            }

            continueButton.gameObject.SetActive(true);
        }

        public void ShowCompletion(int score, int maxScore)
        {
            speechBubble.gameObject.SetActive(false);
            dialoguePanel.gameObject.SetActive(false);
            dialogueInput.interactable = false;
            dialogueSendButton.interactable = false;
            dialogueStatus.text = "훈련 완료 · 세션을 다시 시작하면 직접 대화를 사용할 수 있습니다.";
            beatLabel.text = "훈련 완료";
            studentLine.text = "학생의 정서 강도가 낮아지고\n수업에\u00a0다시\u00a0참여할 준비가 되었습니다.";
            observationText.text = "핵심 원리\n낮은 자극 · 감정 인정 · 선택권\n공개적 대치 회피 · 후속 지원 연결";
            responseChipLabel.text = "훈련 결과";
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.fontSize = 24;
            feedbackText.rectTransform.anchorMin = Vector2.zero;
            feedbackText.rectTransform.anchorMax = Vector2.one;
            feedbackText.rectTransform.offsetMin = new Vector2(24f, 24f);
            feedbackText.rectTransform.offsetMax = new Vector2(-24f, -50f);
            feedbackText.text = $"<size=18>최종 공동조절 점수</size>\n<b><size=38>{score}/{maxScore}</size></b>\n<size=18>{ResponseScorer.GetLevel(score, 3)}</size>";
            scoreText.text = "세션 기록이 로컬에 저장되었습니다.";
            foreach (Button button in optionButtons)
            {
                button.gameObject.SetActive(false);
            }

            continueButton.gameObject.SetActive(false);
        }

        public void AppendAiCoachFeedback(string feedback)
        {
            if (!string.IsNullOrWhiteSpace(feedback))
            {
                feedbackText.text += $"\n\n<b>생성형 AI 코치</b>\n{feedback}";
            }
        }

        private static string KeepKoreanPhrasesTogether(string value)
        {
            return value
                .Replace("다 쳐다보잖아요", "<nobr>다\u00a0쳐다보잖아요</nobr>")
                .Replace("다시 이야기해 볼게요", "<nobr>다시\u00a0이야기해\u00a0볼게요</nobr>")
                .Replace("더는 못 하겠어요", "<nobr>더는\u00a0못\u00a0하겠어요</nobr>")
                .Replace("못 하겠어요", "<nobr>못\u00a0하겠어요</nobr>")
                .Replace("다시 이야기해", "<nobr>다시\u00a0이야기해</nobr>")
                .Replace("잠깐 쉬어도 괜찮아", "<nobr>잠깐\u00a0쉬어도\u00a0괜찮아</nobr>")
                .Replace("하겠어요", "<nobr>하겠어요</nobr>")
                .Replace("볼게요", "<nobr>볼게요</nobr>")
                .Replace("수업에 다시 참여할", "<nobr>수업에\u00a0다시\u00a0참여할</nobr>");
        }
    }
}
