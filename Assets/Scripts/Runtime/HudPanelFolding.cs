using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Keeps the main HUD windows accessible through a compact left-edge dock.
    /// Each circular button toggles its window and briefly confirms the result.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudPanelFolding : MonoBehaviour
    {
        private static readonly Color DockSurface = new Color(0.025f, 0.075f, 0.09f, 0.82f);
        private static readonly Color ButtonSurface = new Color(0.045f, 0.14f, 0.16f, 0.96f);
        private static readonly Color ButtonActive = new Color(0.12f, 0.48f, 0.44f, 0.98f);
        private static readonly Color ToastSurface = new Color(0.025f, 0.075f, 0.09f, 0.95f);
        private static readonly Color ToastAccent = new Color(0.32f, 0.84f, 0.75f, 1f);
        private static readonly Color ToastText = new Color(0.91f, 0.97f, 0.96f, 1f);

        private sealed class FoldEntry
        {
            public CanvasGroup group;
            public Image buttonImage;
            public bool folded;
            public string title;
        }

        private readonly List<FoldEntry> entries = new List<FoldEntry>();
        private RectTransform dock;
        private CanvasGroup toastGroup;
        private TMP_Text toastTitle;
        private TMP_Text toastBody;
        private float toastHideAt;

        public static void Install(Canvas canvas, TMP_FontAsset font)
        {
            if (canvas == null || font == null || canvas.gameObject.GetComponent<HudPanelFolding>() != null)
            {
                return;
            }

            var folding = canvas.gameObject.AddComponent<HudPanelFolding>();
            folding.BuildDock(canvas, font);
            folding.Attach(canvas, "SituationPanel", "\uad00\ucc30", "UI/HudDock/hud_observation");
            folding.Attach(canvas, "ResponsePanel", "\ub300\uc751", "UI/HudDock/hud_response");
            folding.Attach(canvas, "DialoguePanel", "\ub300\ud654", "UI/HudDock/hud_dialogue");
            folding.dock.SetAsLastSibling();
            folding.BuildToast(canvas, font);
        }

        private void BuildDock(Canvas canvas, TMP_FontAsset font)
        {
            Sprite rounded = canvas.transform.Find("SituationPanel")?.GetComponent<Image>()?.sprite;
            var dockObject = new GameObject(
                "HudQuickDock",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dock = (RectTransform)dockObject.transform;
            dock.SetParent(canvas.transform, false);
            dock.anchorMin = new Vector2(0f, 0.5f);
            dock.anchorMax = new Vector2(0f, 0.5f);
            dock.pivot = new Vector2(0f, 0.5f);
            dock.anchoredPosition = new Vector2(14f, 18f);
            dock.sizeDelta = new Vector2(64f, 212f);
            StyleSurface(dockObject.GetComponent<Image>(), rounded, DockSurface);

            TMP_Text caption = CreateLabel(dock, font, "Caption", "HUD", 10f, FontStyles.Bold, ToastAccent);
            SetRect(caption.rectTransform, new Vector2(0f, -24f), new Vector2(0f, 0f));
            caption.alignment = TextAlignmentOptions.Center;
        }

        private void Attach(Canvas canvas, string panelName, string title, string iconResourcePath)
        {
            Transform panel = canvas.transform.Find(panelName);
            if (panel == null)
            {
                return;
            }

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = panel.gameObject.AddComponent<CanvasGroup>();
            }

            Sprite rounded = panel.GetComponent<Image>()?.sprite;
            var entry = new FoldEntry { group = group, title = title };
            int index = entries.Count;
            var buttonObject = new GameObject(
                panelName + "DockButton",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ButtonMotion));
            RectTransform buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.SetParent(dock, false);
            buttonRect.anchorMin = new Vector2(0.5f, 1f);
            buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.anchoredPosition = new Vector2(0f, -34f - index * 58f);
            buttonRect.sizeDelta = new Vector2(46f, 46f);

            entry.buttonImage = buttonObject.GetComponent<Image>();
            StyleSurface(entry.buttonImage, rounded, ButtonActive);
            CreateIcon(buttonRect, iconResourcePath);
            buttonObject.GetComponent<Button>().onClick.AddListener(() => Toggle(entry));
            entries.Add(entry);
        }

        private static void CreateIcon(RectTransform parent, string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = texture.name + "_RuntimeSprite";
            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.SetParent(parent, false);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(5f, 5f);
            iconRect.offsetMax = new Vector2(-5f, -5f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        private void BuildToast(Canvas canvas, TMP_FontAsset font)
        {
            Sprite rounded = canvas.transform.Find("SituationPanel")?.GetComponent<Image>()?.sprite;
            var toastObject = new GameObject(
                "HudDockNotification",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            RectTransform toast = (RectTransform)toastObject.transform;
            toast.SetParent(canvas.transform, false);
            toast.anchorMin = new Vector2(0f, 0.5f);
            toast.anchorMax = new Vector2(0f, 0.5f);
            toast.pivot = new Vector2(0f, 0.5f);
            toast.anchoredPosition = new Vector2(88f, 18f);
            toast.sizeDelta = new Vector2(292f, 82f);
            StyleSurface(toastObject.GetComponent<Image>(), rounded, ToastSurface);
            toastGroup = toastObject.GetComponent<CanvasGroup>();

            toastTitle = CreateLabel(toast, font, "Title", string.Empty, 15f, FontStyles.Bold, ToastAccent);
            SetRect(toastTitle.rectTransform, new Vector2(18f, -34f), new Vector2(-18f, -12f));
            toastBody = CreateLabel(toast, font, "Body", string.Empty, 13f, FontStyles.Normal, ToastText);
            SetRect(toastBody.rectTransform, new Vector2(18f, -66f), new Vector2(-18f, -38f));
            toastObject.SetActive(false);
            toast.SetAsLastSibling();
        }

        private void Toggle(FoldEntry entry)
        {
            entry.folded = !entry.folded;
            entry.group.alpha = entry.folded ? 0f : 1f;
            entry.group.interactable = !entry.folded;
            entry.group.blocksRaycasts = !entry.folded;
            entry.buttonImage.color = entry.folded ? ButtonSurface : ButtonActive;
            ShowNotification(
                entry.title + " \ucc3d",
                entry.folded ? "\ud654\uba74\uc5d0\uc11c \uc228\uae30\uc600\uc2b5\ub2c8\ub2e4." : "\ub2e4\uc2dc \uc5f4\uc5c8\uc2b5\ub2c8\ub2e4.");
        }

        private void ShowNotification(string title, string body)
        {
            toastTitle.text = title;
            toastBody.text = body;
            toastGroup.gameObject.SetActive(true);
            toastGroup.alpha = 1f;
            toastHideAt = Time.unscaledTime + 2.4f;
            toastGroup.transform.SetAsLastSibling();
            UiEntranceMotion.Play(toastGroup.gameObject, 0.16f);
        }

        private void Update()
        {
            if (toastGroup != null && toastGroup.gameObject.activeSelf && Time.unscaledTime >= toastHideAt)
            {
                toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 0f, Time.unscaledDeltaTime * 4f);
                if (toastGroup.alpha <= 0.01f)
                {
                    toastGroup.gameObject.SetActive(false);
                }
            }

            // Mode changes may explicitly reveal a previously folded panel.
            for (int index = 0; index < entries.Count; index++)
            {
                FoldEntry entry = entries[index];
                if (entry.folded && entry.group.alpha > 0.99f)
                {
                    entry.folded = false;
                    entry.buttonImage.color = ButtonActive;
                }
            }
        }

        private static TMP_Text CreateLabel(
            RectTransform parent,
            TMP_FontAsset font,
            string name,
            string value,
            float size,
            FontStyles style,
            Color color)
        {
            var labelObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)labelObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            label.text = value;
            return label;
        }

        private static void SetRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void StyleSurface(Image image, Sprite rounded, Color color)
        {
            image.color = color;
            if (rounded != null)
            {
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
            }
        }
    }
}
