using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    public sealed class TrainingModeNavigator : MonoBehaviour
    {
        [SerializeField] private Button[] modeButtons;
        [SerializeField] private CanvasGroup observationPanel;
        [SerializeField] private CanvasGroup responsePanel;
        [SerializeField] private CanvasGroup dialoguePanel;
        [SerializeField] private RectTransform debriefPanel;
        [SerializeField] private TMP_Text debriefText;

        private int selectedMode = 1;
        private bool debriefUnlocked;

        public bool DebriefUnlocked => debriefUnlocked;

        private void Awake()
        {
            for (int i = 0; i < modeButtons.Length; i++)
            {
                int captured = i;
                modeButtons[i].onClick.AddListener(() => SelectMode(captured));
            }

            SelectMode(selectedMode);
        }

        public void SelectMode(int mode)
        {
            if (mode == 3 && !debriefUnlocked)
            {
                mode = 1;
            }

            selectedMode = Mathf.Clamp(mode, 0, 3);
            SetPanel(observationPanel, 1f, selectedMode == 0 || selectedMode == 1);
            SetPanel(responsePanel, selectedMode == 1 || selectedMode == 3 ? 1f : 0f, selectedMode == 1);
            SetPanel(dialoguePanel, selectedMode == 2 ? 1f : 0f, selectedMode == 2);
            debriefPanel.gameObject.SetActive(selectedMode == 3);
            for (int i = 0; i < modeButtons.Length; i++)
            {
                modeButtons[i].transform.localScale = i == selectedMode ? Vector3.one * 1.045f : Vector3.one;
                modeButtons[i].targetGraphic.color = i == selectedMode
                    ? new Color(0.055f, 0.53f, 0.50f, 1f)
                    : new Color(0.12f, 0.17f, 0.23f, 1f);
            }
        }

        public void ShowDebrief(RubricSummary summary)
        {
            if (summary == null || summary.dimensions == null)
            {
                return;
            }

            string text = $"<b>교사 대응 역량 · {summary.overallLevel}</b>\n평균 {summary.averageScore:0.0}/3.0\n";
            for (int i = 0; i < summary.dimensions.Length; i++)
            {
                text += $"{summary.dimensions[i].label} {summary.dimensions[i].score:0.0}";
                if (i == 2)
                {
                    text += "\n";
                }
                else if (i < summary.dimensions.Length - 1)
                {
                    text += "  ·  ";
                }
            }

            debriefText.text = text;
            debriefUnlocked = true;
            SelectMode(3);
        }

        private static void SetPanel(CanvasGroup panel, float alpha, bool interactive)
        {
            panel.alpha = alpha;
            panel.interactable = interactive;
            panel.blocksRaycasts = interactive;
        }
    }
}
