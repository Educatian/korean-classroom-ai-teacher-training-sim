using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Post-session reflection: two open prompts shown between the completion
    /// banner and the research debrief. Answering is voluntary (skip is always
    /// available) so the reflection stays a learning aid, not a gate. Answers
    /// are handed back to the controller for telemetry; this panel never scores.
    /// </summary>
    public sealed class ReflectionPromptPanel
    {
        public static readonly string[] Questions =
        {
            "오늘 대응 중 가장 도움이 되었다고 생각하는 선택은 무엇이었나요? 그 이유는요?",
            "실제 교실에서 비슷한 상황을 만난다면, 무엇을 다르게 해 보고 싶나요?"
        };

        private static readonly Color Overlay = new Color(0.02f, 0.05f, 0.06f, 0.86f);
        private static readonly Color CardSurface = new Color(0.05f, 0.12f, 0.14f, 0.98f);
        private static readonly Color FieldSurface = new Color(0.03f, 0.08f, 0.1f, 1f);
        private static readonly Color Accent = new Color(0.32f, 0.84f, 0.75f, 1f);
        private static readonly Color TextMain = new Color(0.92f, 0.97f, 0.96f, 1f);
        private static readonly Color TextSoft = new Color(0.62f, 0.72f, 0.74f, 1f);

        private readonly GameObject root;
        private readonly TMP_InputField[] inputs;
        private readonly Action<string[]> onFinished;
        private bool finished;

        private ReflectionPromptPanel(GameObject root, TMP_InputField[] inputs, Action<string[]> onFinished)
        {
            this.root = root;
            this.inputs = inputs;
            this.onFinished = onFinished;
        }

        public static ReflectionPromptPanel Show(Canvas canvas, TMP_FontAsset font, Action<string[]> onFinished)
        {
            if (canvas == null || font == null)
            {
                return null;
            }

            var rootObject = new GameObject("ReflectionOverlay", typeof(RectTransform), typeof(Image));
            var rootRect = (RectTransform)rootObject.transform;
            rootRect.SetParent(canvas.transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootObject.GetComponent<Image>().color = Overlay;
            rootRect.SetAsLastSibling();
            // Same sorting guarantee as the pause overlay: HUD panels must not
            // draw or click through the reflection card.
            var overlayCanvas = rootObject.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 400;
            rootObject.AddComponent<GraphicRaycaster>();

            var card = new GameObject("ReflectionCard", typeof(RectTransform), typeof(Image));
            var cardRect = (RectTransform)card.transform;
            cardRect.SetParent(rootRect, false);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(720f, 560f);
            card.GetComponent<Image>().color = CardSurface;

            Label(cardRect, font, "Title", "잠시 돌아보기", 25, FontStyles.Bold, Accent,
                new Vector2(40f, -70f), new Vector2(-40f, -28f), TextAlignmentOptions.TopLeft);
            Label(cardRect, font, "Subtitle",
                "결과를 보기 전에 오늘의 대응을 짧게 돌아봅니다. 적지 않고 건너뛰어도 괜찮습니다.",
                14, FontStyles.Normal, TextSoft,
                new Vector2(40f, -100f), new Vector2(-40f, -72f), TextAlignmentOptions.TopLeft);

            var inputs = new TMP_InputField[Questions.Length];
            float top = -116f;
            for (int index = 0; index < Questions.Length; index++)
            {
                Label(cardRect, font, "Question" + index, (index + 1) + ". " + Questions[index],
                    15, FontStyles.Bold, TextMain,
                    new Vector2(40f, top - 42f), new Vector2(-40f, top), TextAlignmentOptions.TopLeft);
                inputs[index] = BuildInput(cardRect, font, "Answer" + index,
                    new Vector2(40f, top - 158f), new Vector2(-40f, top - 48f));
                top -= 170f;
            }

            var panel = new ReflectionPromptPanel(rootObject, inputs, onFinished);
            BuildButton(cardRect, font, "SkipButton", "건너뛰기", FieldSurface, TextSoft,
                new Vector2(-140f, 46f), () => panel.Finish(false));
            BuildButton(cardRect, font, "SubmitButton", "제출하고 결과 보기", Accent,
                new Color(0.03f, 0.1f, 0.1f, 1f), new Vector2(120f, 46f), () => panel.Finish(true));

            UiEntranceMotion.Play(card, 0.28f);
            return panel;
        }

        private void Finish(bool submitted)
        {
            if (finished)
            {
                return;
            }

            finished = true;
            var answers = new string[inputs.Length];
            for (int index = 0; index < inputs.Length; index++)
            {
                answers[index] = submitted && inputs[index] != null
                    ? inputs[index].text ?? string.Empty
                    : string.Empty;
            }

            if (root != null)
            {
                root.SetActive(false);
                UnityEngine.Object.Destroy(root, 0.1f);
            }

            onFinished?.Invoke(answers);
        }

        private static TMP_InputField BuildInput(
            RectTransform parent,
            TMP_FontAsset font,
            string name,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var fieldObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            var fieldRect = (RectTransform)fieldObject.transform;
            fieldRect.SetParent(parent, false);
            fieldRect.anchorMin = new Vector2(0f, 1f);
            fieldRect.anchorMax = new Vector2(1f, 1f);
            fieldRect.offsetMin = offsetMin;
            fieldRect.offsetMax = offsetMax;
            fieldObject.GetComponent<Image>().color = FieldSurface;

            var areaObject = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            var areaRect = (RectTransform)areaObject.transform;
            areaRect.SetParent(fieldRect, false);
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(12f, 8f);
            areaRect.offsetMax = new Vector2(-12f, -8f);

            TMP_Text placeholder = Label(areaRect, font, "Placeholder", "생각을 자유롭게 적어 주세요…",
                14, FontStyles.Italic, TextSoft, Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            TMP_Text text = Label(areaRect, font, "Text", string.Empty,
                14, FontStyles.Normal, TextMain, Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);

            var input = fieldObject.GetComponent<TMP_InputField>();
            input.textViewport = areaRect;
            input.textComponent = (TextMeshProUGUI)text;
            input.placeholder = placeholder;
            input.fontAsset = font;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.characterLimit = 600;
            return input;
        }

        private static void BuildButton(
            RectTransform card,
            TMP_FontAsset font,
            string name,
            string caption,
            Color background,
            Color textColor,
            Vector2 position,
            Action onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(card, false);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(name == "SkipButton" ? 150f : 280f, 50f);
            buttonObject.GetComponent<Image>().color = background;
            TMP_Text label = Label(rect, font, "Label", caption, 16, FontStyles.Bold, textColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            buttonObject.GetComponent<Button>().onClick.AddListener(() => onClick());
        }

        private static TMP_Text Label(
            RectTransform parent,
            TMP_FontAsset font,
            string name,
            string text,
            float size,
            FontStyles style,
            Color color,
            Vector2 offsetMin,
            Vector2 offsetMax,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            if (offsetMin == Vector2.zero && offsetMax == Vector2.zero)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;
            }

            var label = textObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            return label;
        }
    }
}
