using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Adds a small fold/unfold pill to each major HUD panel so the teacher can
    /// reduce on-screen windows while observing the classroom. Folding drives the
    /// panel's CanvasGroup; the pill carries its own CanvasGroup with
    /// ignoreParentGroups so it stays visible and clickable while folded.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudPanelFolding : MonoBehaviour
    {
        private static readonly Color PillSurface = new Color(0.05f, 0.13f, 0.15f, 0.92f);
        private static readonly Color PillText = new Color(0.62f, 0.88f, 0.82f, 1f);

        private sealed class FoldEntry
        {
            public CanvasGroup group;
            public TMP_Text pillLabel;
            public bool folded;
            public string title;
        }

        private readonly List<FoldEntry> entries = new List<FoldEntry>();

        public static void Install(Canvas canvas, TMP_FontAsset font)
        {
            if (canvas == null || font == null || canvas.gameObject.GetComponent<HudPanelFolding>() != null)
            {
                return;
            }

            var folding = canvas.gameObject.AddComponent<HudPanelFolding>();
            folding.Attach(canvas, font, "SituationPanel", "관찰");
            folding.Attach(canvas, font, "ResponsePanel", "대응");
            folding.Attach(canvas, font, "DialoguePanel", "대화");
        }

        private void Attach(Canvas canvas, TMP_FontAsset font, string panelName, string title)
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

            var entry = new FoldEntry { group = group, title = title };

            var pillObject = new GameObject(panelName + "FoldPill",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
            var pillRect = (RectTransform)pillObject.transform;
            pillRect.SetParent(panel, false);
            pillRect.anchorMin = new Vector2(1f, 1f);
            pillRect.anchorMax = new Vector2(1f, 1f);
            pillRect.pivot = new Vector2(1f, 1f);
            pillRect.anchoredPosition = new Vector2(-8f, -6f);
            pillRect.sizeDelta = new Vector2(84f, 24f);
            pillObject.GetComponent<Image>().color = PillSurface;
            CanvasGroup pillGroup = pillObject.GetComponent<CanvasGroup>();
            pillGroup.ignoreParentGroups = true;
            pillGroup.blocksRaycasts = true;

            var labelObject = new GameObject("Label", typeof(RectTransform));
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(pillRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 12;
            label.color = PillText;
            label.alignment = TextAlignmentOptions.Center;
            label.text = "접기";
            entry.pillLabel = label;

            pillObject.GetComponent<Button>().onClick.AddListener(() => Toggle(entry));
            entries.Add(entry);
        }

        private void Toggle(FoldEntry entry)
        {
            entry.folded = !entry.folded;
            entry.group.alpha = entry.folded ? 0f : 1f;
            entry.group.interactable = !entry.folded;
            entry.group.blocksRaycasts = !entry.folded;
            entry.pillLabel.text = entry.folded ? entry.title + " 펼치기" : "접기";
        }

        private void Update()
        {
            // A mode switch can re-show a folded panel through the same CanvasGroup;
            // keep the pill label honest when that happens.
            for (int index = 0; index < entries.Count; index++)
            {
                FoldEntry entry = entries[index];
                if (entry.folded && entry.group.alpha > 0.99f)
                {
                    entry.folded = false;
                    entry.pillLabel.text = "접기";
                }
            }
        }
    }
}
