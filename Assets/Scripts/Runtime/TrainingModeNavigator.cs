using System;
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
        [SerializeField] private RectTransform fullDashboardPanel;

        private int selectedMode = 1;
        private bool debriefUnlocked;
        private ResearchDebriefDashboard researchDashboard;

        public bool DebriefUnlocked => debriefUnlocked;

        private void Awake()
        {
            for (int i = 0; i < modeButtons.Length; i++)
            {
                int captured = i;
                modeButtons[i].onClick.AddListener(() => SelectMode(captured));
            }

            researchDashboard = gameObject.AddComponent<ResearchDebriefDashboard>();
            researchDashboard.Initialize(
                debriefPanel,
                debriefText,
                fullDashboardPanel,
                () => SelectMode(3),
                () => SelectMode(1));
            SelectMode(selectedMode);
        }

        public void SelectMode(int mode)
        {
            if (mode == 3 && !debriefUnlocked)
            {
                mode = 1;
            }

            selectedMode = Mathf.Clamp(mode, 0, 3);
            bool fullDashboard = selectedMode == 3;
            SetPanel(observationPanel, fullDashboard ? 0f : 1f, !fullDashboard && (selectedMode == 0 || selectedMode == 1));
            SetPanel(responsePanel, selectedMode == 1 ? 1f : 0f, selectedMode == 1 && !debriefUnlocked);
            SetPanel(dialoguePanel, selectedMode == 2 ? 1f : 0f, selectedMode == 2);

            if (researchDashboard != null)
            {
                if (fullDashboard)
                {
                    researchDashboard.OpenFull();
                }
                else if (selectedMode == 1 && debriefUnlocked)
                {
                    researchDashboard.ShowSummary();
                }
                else
                {
                    researchDashboard.HideAll();
                }
            }

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
                if (i == 2) text += "\n";
                else if (i < summary.dimensions.Length - 1) text += "  ·  ";
            }

            debriefText.text = text;
            debriefUnlocked = true;
            SelectMode(1);
        }

        public void ShowResearchDebrief(ResearchDebriefReport report, Action retry)
        {
            if (report == null)
            {
                return;
            }

            researchDashboard.Show(report, retry);
            debriefUnlocked = true;
            SelectMode(1);
        }

        private static void SetPanel(CanvasGroup panel, float alpha, bool interactive)
        {
            panel.alpha = alpha;
            panel.interactable = interactive;
            panel.blocksRaycasts = interactive;
        }
    }
}
