using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Optional full-screen learning panel for the proposal-aligned crisis
    /// orchestration scenario. It shares the main HUD canvas, so desktop and XR
    /// use the same authored content and interaction state.
    /// </summary>
    public sealed class CrisisOrchestrationPanel : MonoBehaviour
    {
        private const string ScenarioResource =
            "Training/Orchestration/TeacherDirectedAggression";

        private static readonly Color Ink = new(0.08f, 0.12f, 0.16f, 1f);
        private static readonly Color Muted = new(0.29f, 0.36f, 0.42f, 1f);
        private static readonly Color Accent = new(0.04f, 0.55f, 0.50f, 1f);
        private static readonly Color Pale = new(0.91f, 0.97f, 0.95f, 1f);
        private static readonly Color Paper = new(0.98f, 0.985f, 0.98f, 0.985f);

        private TMP_FontAsset font;
        private Sprite rounded;
        private RectTransform modal;
        private TMP_Text title;
        private TMP_Text progress;
        private TMP_Text body;
        private TMP_Text prompt;
        private TMP_Text feedback;
        private RectTransform optionsRoot;
        private CrisisOrchestrationSession session;
        private Action<string, CrisisOrchestrationAction, CrisisOrchestrationResolution>
            actionObserved;

        public static CrisisOrchestrationPanel Install(
            Canvas canvas,
            TMP_FontAsset font,
            Action<string, CrisisOrchestrationAction, CrisisOrchestrationResolution>
                onActionObserved = null)
        {
            if (canvas == null || font == null) return null;
            CrisisOrchestrationPanel existing =
                canvas.GetComponent<CrisisOrchestrationPanel>();
            if (existing != null) return existing;

            CrisisOrchestrationScenarioAsset scenario =
                Resources.Load<CrisisOrchestrationScenarioAsset>(ScenarioResource);
            if (scenario == null)
            {
                Debug.LogWarning("Crisis orchestration scenario asset is missing.");
                return null;
            }

            CrisisOrchestrationPanel panel =
                canvas.gameObject.AddComponent<CrisisOrchestrationPanel>();
            panel.font = font;
            panel.actionObserved = onActionObserved;
            panel.rounded = canvas.transform.Find("SituationPanel")
                ?.GetComponent<Image>()?.sprite;
            panel.session = new CrisisOrchestrationSession(scenario);
            panel.Build(canvas.transform);
            panel.SetOpen(false);
            return panel;
        }

        private void Build(Transform canvas)
        {
            GameObject launcher = Surface("CrisisTrainingLauncher", canvas, Accent);
            RectTransform launcherRect = (RectTransform)launcher.transform;
            launcherRect.anchorMin = launcherRect.anchorMax = new Vector2(0f, 0.5f);
            launcherRect.pivot = new Vector2(0f, 0.5f);
            launcherRect.anchoredPosition = new Vector2(20f, -122f);
            launcherRect.sizeDelta = new Vector2(58f, 58f);
            launcher.AddComponent<Button>().onClick.AddListener(() => SetOpen(true));
            launcher.AddComponent<ButtonMotion>();
            TMP_Text launcherLabel = Label(launcherRect, "LauncherLabel", "안전\n훈련", 12f,
                FontStyles.Bold, Color.white);
            Stretch(launcherLabel.rectTransform, 3f);
            launcherLabel.alignment = TextAlignmentOptions.Center;

            GameObject dim = Surface("CrisisOrchestrationOverlay", canvas,
                new Color(0.01f, 0.025f, 0.035f, 0.82f));
            modal = (RectTransform)dim.transform;
            modal.anchorMin = Vector2.zero;
            modal.anchorMax = Vector2.one;
            modal.offsetMin = modal.offsetMax = Vector2.zero;

            GameObject card = Surface("CrisisOrchestrationCard", modal, Paper);
            RectTransform cardRect = (RectTransform)card.transform;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1180f, 790f);

            title = Label(cardRect, "Title", string.Empty, 29f, FontStyles.Bold, Ink);
            Place(title.rectTransform, new Vector2(42f, -78f), new Vector2(-180f, -28f));
            progress = Label(cardRect, "Progress", string.Empty, 15f, FontStyles.Bold, Accent);
            Place(progress.rectTransform, new Vector2(820f, -72f), new Vector2(-88f, -32f));
            progress.alignment = TextAlignmentOptions.Right;

            Button close = ActionButton(cardRect, "Close", "닫기", new Color(0.90f, 0.93f, 0.94f, 1f));
            RectTransform closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-28f, -24f);
            closeRect.sizeDelta = new Vector2(76f, 42f);
            close.onClick.AddListener(() => SetOpen(false));

            body = Label(cardRect, "Situation", string.Empty, 18f, FontStyles.Normal, Ink);
            Place(body.rectTransform, new Vector2(42f, -290f), new Vector2(-42f, -105f));
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Ellipsis;

            GameObject promptCard = Surface("PrivatePrompt", cardRect, Pale);
            RectTransform promptRect = (RectTransform)promptCard.transform;
            promptRect.anchorMin = new Vector2(0f, 1f);
            promptRect.anchorMax = new Vector2(1f, 1f);
            promptRect.pivot = new Vector2(0.5f, 1f);
            promptRect.anchoredPosition = new Vector2(0f, -302f);
            promptRect.sizeDelta = new Vector2(-84f, 112f);
            prompt = Label(promptRect, "PromptText", string.Empty, 17f, FontStyles.Italic, Ink);
            Stretch(prompt.rectTransform, 20f);
            prompt.textWrappingMode = TextWrappingModes.Normal;

            feedback = Label(cardRect, "Feedback", string.Empty, 15f, FontStyles.Normal, Muted);
            Place(feedback.rectTransform, new Vector2(42f, -468f), new Vector2(-42f, -426f));
            feedback.textWrappingMode = TextWrappingModes.Normal;

            GameObject options = new("Options", typeof(RectTransform));
            optionsRoot = (RectTransform)options.transform;
            optionsRoot.SetParent(cardRect, false);
            optionsRoot.anchorMin = new Vector2(0f, 0f);
            optionsRoot.anchorMax = new Vector2(1f, 0f);
            optionsRoot.pivot = new Vector2(0.5f, 0f);
            optionsRoot.anchoredPosition = new Vector2(0f, 34f);
            optionsRoot.sizeDelta = new Vector2(-84f, 280f);

            Render();
            modal.SetAsLastSibling();
        }

        private void Render()
        {
            for (int index = optionsRoot.childCount - 1; index >= 0; index--)
                Destroy(optionsRoot.GetChild(index).gameObject);

            if (session.IsComplete)
            {
                RenderDebrief();
                return;
            }

            CrisisOrchestrationScenarioBeat beat = session.CurrentBeat;
            title.text = beat.title;
            progress.text = $"TEAM RESPONSE  ·  {session.BeatIndex + 1}/{session.Scenario.Beats.Count}";
            body.text = $"<b>상황</b>\n{beat.situation}\n\n<b>관찰 신호</b>\n{beat.observableSignals}";
            prompt.text = $"나에게 묻기  ·  {beat.privateTeacherPrompt}";
            if (string.IsNullOrWhiteSpace(feedback.text))
                feedback.text = "감정에는 점수가 없습니다. 선택한 행동의 안전 효과를 피드백합니다.";

            CrisisOrchestrationActionOption[] options = beat.options ??
                Array.Empty<CrisisOrchestrationActionOption>();
            float height = options.Length <= 1 ? 116f : 100f;
            for (int index = 0; index < options.Length; index++)
            {
                CrisisOrchestrationActionOption option = options[index];
                Button button = ActionButton(optionsRoot, "Option_" + index,
                    $"<b>{option.label}</b>\n<size=13>{option.operationalScript}</size>",
                    index == 0 ? Accent : new Color(0.11f, 0.16f, 0.20f, 1f));
                RectTransform rect = (RectTransform)button.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -index * (height + 12f));
                rect.sizeDelta = new Vector2(0f, height);
                CrisisOrchestrationAction captured = option.action;
                button.onClick.AddListener(() => Select(captured));
            }
        }

        private void Select(CrisisOrchestrationAction action)
        {
            string beatId = session.CurrentBeat?.beatId ?? string.Empty;
            CrisisOrchestrationResolution result = session.SelectAction(action);
            actionObserved?.Invoke(beatId, action, result);
            feedback.text = result.accepted
                ? "피드백  ·  " + result.feedback
                : "지금은 실행할 수 없음  ·  " + result.feedback;
            Render();
        }

        private void RenderDebrief()
        {
            CrisisOrchestrationDebrief debrief =
                CrisisOrchestrationDebriefBuilder.Build(session.Scenario, session.History);
            title.text = "팀 기반 위기대응 디브리핑";
            progress.text = "SESSION COMPLETE";
            body.text = debrief.headline + "\n\n<b>확인된 강점</b>\n" +
                        Bullets(debrief.strengths, 3) + "\n\n<b>다시 볼 지점</b>\n" +
                        Bullets(debrief.revisit, 2);
            prompt.text = "조직에 요청할 지원  ·  " + debrief.organizationalSupports[0];
            feedback.text = $"관찰근거 기반 평균 {debrief.assessment.averageScore:0.0}/3.0  ·  미관찰 역량은 추정하지 않습니다.";

            Button close = ActionButton(optionsRoot, "DebriefClose", "훈련 화면으로 돌아가기", Accent);
            RectTransform rect = (RectTransform)close.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 78f);
            close.onClick.AddListener(() => SetOpen(false));
        }

        private void SetOpen(bool open)
        {
            if (modal == null) return;
            modal.gameObject.SetActive(open);
            if (open)
            {
                modal.SetAsLastSibling();
                UiEntranceMotion.Play(modal.gameObject, 0.16f);
            }
        }

        private GameObject Surface(string objectName, Transform parent, Color color)
        {
            GameObject instance = new(objectName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            instance.transform.SetParent(parent, false);
            Image image = instance.GetComponent<Image>();
            image.color = color;
            image.sprite = rounded;
            image.type = rounded != null ? Image.Type.Sliced : Image.Type.Simple;
            return instance;
        }

        private Button ActionButton(
            Transform parent, string objectName, string label, Color color)
        {
            GameObject instance = Surface(objectName, parent, color);
            Button button = instance.AddComponent<Button>();
            instance.AddComponent<ButtonMotion>();
            TMP_Text text = Label(instance.transform, "Label", label, 16f,
                FontStyles.Normal, color.r + color.g + color.b > 2.1f ? Ink : Color.white);
            Stretch(text.rectTransform, 20f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            return button;
        }

        private TMP_Text Label(
            Transform parent, string objectName, string value, float size,
            FontStyles style, Color color)
        {
            GameObject instance = new(objectName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            instance.transform.SetParent(parent, false);
            TMP_Text text = instance.GetComponent<TMP_Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            return text;
        }

        private static string Bullets(string[] items, int limit)
        {
            var builder = new StringBuilder();
            for (int index = 0; index < items.Length && index < limit; index++)
                builder.Append("• ").Append(items[index]).Append('\n');
            return builder.ToString().TrimEnd();
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(min.x, max.y);
            rect.offsetMax = new Vector2(max.x, min.y);
        }
    }
}
