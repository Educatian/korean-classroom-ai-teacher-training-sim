using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Psychological-safety pause control: a small HUD button (and Escape) opens
    /// an overlay where the teacher can rest, resume, or abort to the main menu
    /// without penalty. Abort asks for confirmation inside the same card. The
    /// overlay blocks scene raycasts while visible so no assessed input leaks
    /// through during the pause.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseMenuSystem : MonoBehaviour
    {
        private static readonly Color Overlay = new Color(0.02f, 0.05f, 0.06f, 0.86f);
        private static readonly Color CardSurface = new Color(0.05f, 0.12f, 0.14f, 0.98f);
        private static readonly Color Accent = new Color(0.32f, 0.84f, 0.75f, 1f);
        private static readonly Color Danger = new Color(0.86f, 0.42f, 0.38f, 1f);
        private static readonly Color TextMain = new Color(0.92f, 0.97f, 0.96f, 1f);
        private static readonly Color TextSoft = new Color(0.62f, 0.72f, 0.74f, 1f);

        private SimulationController controller;
        private TMP_FontAsset font;
        private GameObject overlayRoot;
        private TMP_Text bodyLabel;
        private TMP_Text resumeLabel;
        private TMP_Text abortLabel;
        private bool confirmingAbort;

        public bool IsOpen => overlayRoot != null && overlayRoot.activeSelf;

        public static PauseMenuSystem Install(SimulationController controller, Canvas canvas, TMP_FontAsset font)
        {
            if (controller == null || canvas == null || font == null)
            {
                return null;
            }

            var host = new GameObject("PauseMenuSystem", typeof(RectTransform));
            var hostRect = (RectTransform)host.transform;
            hostRect.SetParent(canvas.transform, false);
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.offsetMin = Vector2.zero;
            hostRect.offsetMax = Vector2.zero;
            var system = host.AddComponent<PauseMenuSystem>();
            system.controller = controller;
            system.font = font;
            system.BuildPauseButton(hostRect);
            return system;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsOpen)
                {
                    Resume();
                }
                else
                {
                    Open();
                }
            }
        }

        public void Open()
        {
            if (IsOpen || !controller.PauseSession())
            {
                return;
            }

            confirmingAbort = false;
            if (overlayRoot == null)
            {
                BuildOverlay();
            }

            ApplyRestingCopy();
            // The host installs before some HUD panels; hoist it above them so
            // the dim layer really covers (and raycast-blocks) the whole HUD.
            transform.SetAsLastSibling();
            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
        }

        private void Resume()
        {
            if (!IsOpen)
            {
                return;
            }

            overlayRoot.SetActive(false);
            controller.ResumeSession();
        }

        private void OnAbortPressed()
        {
            if (!confirmingAbort)
            {
                confirmingAbort = true;
                bodyLabel.text =
                    "정말 중단할까요? 중단하면 메인 화면으로 돌아갑니다.\n" +
                    "지금까지의 진행은 저장되며, 중단해도 불이익은 없습니다.";
                resumeLabel.text = "돌아가기";
                abortLabel.text = "중단하기";
                return;
            }

            overlayRoot.SetActive(false);
            controller.AbortSessionToMenu();
        }

        private void OnResumePressed()
        {
            if (confirmingAbort)
            {
                confirmingAbort = false;
                ApplyRestingCopy();
                return;
            }

            Resume();
        }

        private void ApplyRestingCopy()
        {
            bodyLabel.text =
                "필요한 만큼 쉬어 가세요. 준비되면 이어서 진행할 수 있습니다.\n" +
                "위기 대응 훈련은 감정적으로 부담이 될 수 있습니다.\n" +
                "언제든 중단할 수 있으며, 중단해도 불이익은 없습니다.";
            resumeLabel.text = "이어서 진행";
            abortLabel.text = "훈련 중단";
        }

        private void BuildPauseButton(RectTransform parent)
        {
            var buttonObject = new GameObject("PauseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -14f);
            rect.sizeDelta = new Vector2(150f, 38f);
            buttonObject.GetComponent<Image>().color = new Color(0.05f, 0.12f, 0.14f, 0.88f);
            TMP_Text label = Label(rect, "Label", "II  잠시 멈춤", 15, FontStyles.Bold, Accent);
            label.alignment = TextAlignmentOptions.Center;
            buttonObject.GetComponent<Button>().onClick.AddListener(Open);
        }

        private void BuildOverlay()
        {
            overlayRoot = new GameObject("PauseOverlay", typeof(RectTransform), typeof(Image));
            var rootRect = (RectTransform)overlayRoot.transform;
            rootRect.SetParent(transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            overlayRoot.GetComponent<Image>().color = Overlay;
            // Sibling order alone proved unreliable against the HUD panels, so
            // the overlay claims an explicit sorting layer just below the
            // research dashboard canvas (500).
            var overlayCanvas = overlayRoot.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 400;
            overlayRoot.AddComponent<GraphicRaycaster>();

            var card = new GameObject("PauseCard", typeof(RectTransform), typeof(Image));
            var cardRect = (RectTransform)card.transform;
            cardRect.SetParent(rootRect, false);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(560f, 320f);
            card.GetComponent<Image>().color = CardSurface;

            TMP_Text title = Label(cardRect, "Title", "잠시 멈춤", 26, FontStyles.Bold, Accent);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = new Vector2(40f, -76f);
            title.rectTransform.offsetMax = new Vector2(-40f, -32f);
            title.alignment = TextAlignmentOptions.TopLeft;

            bodyLabel = Label(cardRect, "Body", string.Empty, 16, FontStyles.Normal, TextMain);
            bodyLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            bodyLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            bodyLabel.rectTransform.offsetMin = new Vector2(40f, -204f);
            bodyLabel.rectTransform.offsetMax = new Vector2(-40f, -88f);
            bodyLabel.alignment = TextAlignmentOptions.TopLeft;

            TMP_Text hint = Label(cardRect, "Hint", "Esc 키로도 열고 닫을 수 있습니다", 12, FontStyles.Normal, TextSoft);
            hint.rectTransform.anchorMin = new Vector2(0f, 0f);
            hint.rectTransform.anchorMax = new Vector2(1f, 0f);
            hint.rectTransform.offsetMin = new Vector2(40f, 14f);
            hint.rectTransform.offsetMax = new Vector2(-40f, 34f);
            hint.alignment = TextAlignmentOptions.Center;

            resumeLabel = BuildCardButton(cardRect, "ResumeButton", Accent,
                new Vector2(-120f, 64f), OnResumePressed);
            abortLabel = BuildCardButton(cardRect, "AbortButton", Danger,
                new Vector2(120f, 64f), OnAbortPressed);
        }

        private TMP_Text BuildCardButton(RectTransform card, string name, Color color, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(card, false);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(210f, 50f);
            buttonObject.GetComponent<Image>().color = color;
            TMP_Text label = Label(rect, "Label", string.Empty, 17, FontStyles.Bold, new Color(0.03f, 0.1f, 0.1f, 1f));
            label.alignment = TextAlignmentOptions.Center;
            buttonObject.GetComponent<Button>().onClick.AddListener(onClick);
            return label;
        }

        private TMP_Text Label(RectTransform parent, string name, string text, float size, FontStyles style, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = textObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            return label;
        }
    }
}
