using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    public enum ResearchDashboardView { Locked, Summary, Full }

    public sealed class ResearchDashboardState
    {
        public ResearchDashboardView View { get; private set; } = ResearchDashboardView.Locked;
        public bool IsUnlocked => View != ResearchDashboardView.Locked;

        public void UnlockSummary() => View = ResearchDashboardView.Summary;

        public bool OpenFull()
        {
            if (!IsUnlocked) return false;
            View = ResearchDashboardView.Full;
            return true;
        }

        public void ReturnToSummary()
        {
            if (IsUnlocked) View = ResearchDashboardView.Summary;
        }
    }

    internal sealed class FullResearchDashboard
    {
        private static readonly Color Background = new Color(0.025f, 0.055f, 0.095f, 1f);
        private static readonly Color Surface = new Color(0.055f, 0.10f, 0.16f, 1f);
        private static readonly Color Raised = new Color(0.075f, 0.135f, 0.20f, 1f);
        private static readonly Color Accent = new Color(0.055f, 0.53f, 0.50f, 1f);
        private static readonly Color Primary = new Color(0.94f, 0.98f, 0.99f, 1f);
        private static readonly Color Secondary = new Color(0.68f, 0.78f, 0.82f, 1f);
        private static readonly Color Warning = new Color(1f, 0.72f, 0.36f, 1f);

        private readonly RectTransform root;
        private readonly TMP_FontAsset font;
        private readonly Sprite roundedSprite;
        private readonly TMP_Text title;
        private readonly TMP_Text[] kpiValues = new TMP_Text[4];
        private TMP_Text insight;
        private readonly TMP_Text[] competencyCards = new TMP_Text[6];
        private TMP_Text interventionText;
        private TMP_Text missedSignalText;
        private readonly TMP_Text status;
        private AffectTrendGraphic graph;
        private readonly RectTransform[] pages = new RectTransform[3];
        private readonly Button[] tabs = new Button[3];
        private Action retryAction;
        private Action exportAction;

        public FullResearchDashboard(RectTransform dashboardRoot, TMP_FontAsset dashboardFont, Sprite surfaceSprite, Action returnMain)
        {
            root = dashboardRoot;
            font = dashboardFont;
            roundedSprite = surfaceSprite;
            Image background = root.GetComponent<Image>();
            background.color = Background;
            background.raycastTarget = true;

            title = Text(root, "DashboardTitle", "훈련 디브리핑", 27f, TextAlignmentOptions.MidlineLeft, Primary);
            title.fontStyle = FontStyles.Bold;
            SetRect(title.rectTransform, new Vector2(0.035f, 0.89f), new Vector2(0.68f, 0.975f));
            TMP_Text subtitle = Text(root, "DashboardSubtitle", "교사 대응 역량 · 관찰 근거 · 학생 정서 변화", 12f, TextAlignmentOptions.MidlineLeft, Secondary);
            SetRect(subtitle.rectTransform, new Vector2(0.035f, 0.845f), new Vector2(0.68f, 0.895f));

            Button back = CreateButton(root, "DashboardBackButton", "훈련 화면으로", new Vector2(0.80f, 0.89f), new Vector2(0.965f, 0.965f), Raised);
            back.onClick.AddListener(() => returnMain?.Invoke());

            string[] labels = { "개요", "역량·근거", "개입 타임라인" };
            for (int index = 0; index < labels.Length; index++)
            {
                float min = 0.035f + index * 0.132f;
                tabs[index] = CreateButton(root, $"DashboardTab_{index}", labels[index], new Vector2(min, 0.775f), new Vector2(min + 0.116f, 0.83f), Raised);
                int captured = index;
                tabs[index].onClick.AddListener(() => SelectTab(captured));
                pages[index] = Panel(root, $"DashboardPage_{index}", Color.clear);
                SetRect(pages[index], new Vector2(0.035f, 0.135f), new Vector2(0.965f, 0.75f));
            }

            BuildOverviewPage();
            BuildCompetencyPage();
            BuildTimelinePage();

            Button retry = CreateButton(root, "DashboardRetryButton", "같은 상황 재시도", new Vector2(0.035f, 0.035f), new Vector2(0.20f, 0.105f), Raised);
            retry.onClick.AddListener(() => retryAction?.Invoke());
            Button export = CreateButton(root, "DashboardExportButton", "연구 데이터 내보내기", new Vector2(0.21f, 0.035f), new Vector2(0.40f, 0.105f), Accent);
            export.onClick.AddListener(() => exportAction?.Invoke());
            status = Text(root, "DashboardExportStatus", string.Empty, 11f, TextAlignmentOptions.MidlineRight, Secondary);
            SetRect(status.rectTransform, new Vector2(0.42f, 0.035f), new Vector2(0.965f, 0.105f));

            SelectTab(0);
            SetVisible(false);
        }

        public void Show(ResearchDebriefReport report, Action retry, Action export)
        {
            retryAction = retry;
            exportAction = export;
            if (report == null) return;

            title.text = $"훈련 디브리핑  <color=#52D7BE>· {report.overallLevel}</color>";
            int evidenceCount = 0;
            foreach (EcdCompetencyResult item in report.competencies) evidenceCount += item.evidenceCount;
            kpiValues[0].text = $"<color=#52D7BE>{report.averageScore:0.00}</color><size=15> / 3.00</size>";
            kpiValues[1].text = $"<color=#52D7BE>{evidenceCount}</color><size=15>개</size>";
            kpiValues[2].text = $"<color=#52D7BE>{report.interventionTimeline.Length}</color><size=15>회</size>";
            kpiValues[3].text = $"<color=#FFB85C>{report.missedSignals.Length}</color><size=15>개</size>";

            EcdCompetencyResult strongest = Extreme(report, true);
            EcdCompetencyResult priority = Extreme(report, false);
            insight.text =
                $"<color=#9FB7BD>세션 판정</color>\n<b>{report.overallLevel}</b> · 평균 {report.averageScore:0.00}/3.00\n" +
                $"<color=#9FB7BD>가장 강한 역량</color>\n<b>{strongest?.label ?? "근거 없음"}</b>  <color=#52D7BE>{FormatScore(strongest)}</color>\n" +
                $"<color=#9FB7BD>다음 훈련 초점</color>\n<b>{priority?.label ?? "추가 관찰"}</b>  <color=#FFB85C>{FormatScore(priority)}</color>\n" +
                $"<color=#9FB7BD>근거 커버리지</color>\n관찰 근거 {evidenceCount}개 · 놓친 신호 {report.missedSignals.Length}개\n" +
                "<color=#9FB7BD>다음 시도</color>\n낮은 점수의 관찰행동을 한 번 더 적용하고 정서 곡선의 변화를 비교하세요.";

            for (int index = 0; index < competencyCards.Length; index++)
            {
                competencyCards[index].text = index < report.competencies.Length
                    ? BuildCompetencyCard(report.competencies[index])
                    : string.Empty;
            }

            interventionText.text = BuildInterventions(report);
            missedSignalText.text = BuildSpeechCoaching(report);
            graph.SetPoints(report.affectTrend);
            status.text = "익명화된 연구 데이터 · 원문 교사 발화 제외";
        }

        public void SetStatus(string value) => status.text = value;

        public void SetVisible(bool visible)
        {
            root.gameObject.SetActive(visible);
            if (visible)
            {
                root.SetAsLastSibling();
                SelectTab(0);
            }
        }

        private void BuildOverviewPage()
        {
            string[] labels = { "평균 점수", "수집 근거", "교사 개입", "놓친 신호" };
            for (int index = 0; index < labels.Length; index++)
            {
                float min = index * 0.25f;
                RectTransform card = Panel(pages[0], $"KpiCard_{index}", Raised);
                SetRect(card, new Vector2(min, 0.77f), new Vector2(min + 0.235f, 1f), new Vector2(index == 0 ? 0f : 6f, 0f), new Vector2(index == 3 ? 0f : -6f, 0f));
                TMP_Text label = Text(card, "Label", labels[index], 12f, TextAlignmentOptions.MidlineLeft, Secondary);
                SetRect(label.rectTransform, new Vector2(0.08f, 0.60f), new Vector2(0.92f, 0.92f));
                kpiValues[index] = Text(card, "Value", "0", 26f, TextAlignmentOptions.MidlineLeft, Primary);
                kpiValues[index].fontStyle = FontStyles.Bold;
                SetRect(kpiValues[index].rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.64f));
            }

            RectTransform chart = Panel(pages[0], "AffectChartCard", Surface);
            SetRect(chart, Vector2.zero, new Vector2(0.67f, 0.72f), Vector2.zero, new Vector2(-7f, 0f));
            TMP_Text chartTitle = Text(chart, "ChartTitle", "학생 정서 변화", 17f, TextAlignmentOptions.MidlineLeft, Primary);
            chartTitle.fontStyle = FontStyles.Bold;
            SetRect(chartTitle.rectTransform, new Vector2(0.045f, 0.82f), new Vector2(0.95f, 0.97f));
            TMP_Text legend = Text(chart, "ChartLegend", "<color=#52D7BE>● 정서가</color>     <color=#FFB85C>● 각성</color>     개입 순서에 따른 변화", 12f, TextAlignmentOptions.MidlineLeft, Secondary);
            SetRect(legend.rectTransform, new Vector2(0.045f, 0.70f), new Vector2(0.95f, 0.84f));
            RectTransform plot = Panel(chart, "ChartPlot", new Color(0.025f, 0.065f, 0.105f, 1f));
            SetRect(plot, new Vector2(0.045f, 0.10f), new Vector2(0.955f, 0.68f));
            graph = Graph(plot, "FullAffectTrend");
            SetRect(graph.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 14f), new Vector2(-16f, -14f));

            RectTransform insightCard = Panel(pages[0], "InsightCard", Surface);
            SetRect(insightCard, new Vector2(0.69f, 0f), new Vector2(1f, 0.72f), new Vector2(7f, 0f), Vector2.zero);
            TMP_Text insightTitle = Text(insightCard, "InsightTitle", "이번 세션 핵심", 17f, TextAlignmentOptions.MidlineLeft, Primary);
            insightTitle.fontStyle = FontStyles.Bold;
            SetRect(insightTitle.rectTransform, new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.97f));
            insight = Text(insightCard, "InsightBody", string.Empty, 14f, TextAlignmentOptions.TopLeft, Secondary);
            insight.lineSpacing = 5f;
            SetRect(insight.rectTransform, new Vector2(0.08f, 0.09f), new Vector2(0.92f, 0.80f));
        }

        private void BuildCompetencyPage()
        {
            for (int index = 0; index < competencyCards.Length; index++)
            {
                int column = index % 2;
                int row = index / 2;
                float xMin = column == 0 ? 0f : 0.51f;
                float xMax = column == 0 ? 0.49f : 1f;
                float yMax = 1f - row * 0.34f;
                float yMin = yMax - 0.31f;
                RectTransform card = Panel(pages[1], $"CompetencyCard_{index}", Surface);
                SetRect(card, new Vector2(xMin, yMin), new Vector2(xMax, yMax));
                competencyCards[index] = Text(card, "Content", string.Empty, 17f, TextAlignmentOptions.MidlineLeft, Primary);
                competencyCards[index].lineSpacing = 9f;
                SetRect(competencyCards[index].rectTransform, new Vector2(0.055f, 0.07f), new Vector2(0.945f, 0.93f));
            }
        }

        private void BuildTimelinePage()
        {
            RectTransform interventions = Panel(pages[2], "InterventionCard", Surface);
            SetRect(interventions, Vector2.zero, new Vector2(0.65f, 1f), Vector2.zero, new Vector2(-8f, 0f));
            TMP_Text interventionTitle = Text(interventions, "Title", "개입 타임라인", 18f, TextAlignmentOptions.MidlineLeft, Primary);
            interventionTitle.fontStyle = FontStyles.Bold;
            SetRect(interventionTitle.rectTransform, new Vector2(0.055f, 0.84f), new Vector2(0.95f, 0.97f));
            interventionText = Text(interventions, "Content", string.Empty, 17f, TextAlignmentOptions.TopLeft, Primary);
            interventionText.lineSpacing = 12f;
            SetRect(interventionText.rectTransform, new Vector2(0.055f, 0.08f), new Vector2(0.95f, 0.82f));

            RectTransform missed = Panel(pages[2], "SpeechCoachingCard", Surface);
            SetRect(missed, new Vector2(0.67f, 0f), Vector2.one, new Vector2(8f, 0f), Vector2.zero);
            TMP_Text missedTitle = Text(missed, "Title", "발화 코칭", 18f, TextAlignmentOptions.MidlineLeft, Primary);
            missedTitle.fontStyle = FontStyles.Bold;
            SetRect(missedTitle.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.97f));
            missedSignalText = Text(missed, "Content", string.Empty, 15f, TextAlignmentOptions.TopLeft, Secondary);
            missedSignalText.lineSpacing = 8f;
            SetRect(missedSignalText.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.82f));
        }

        private void SelectTab(int selected)
        {
            for (int index = 0; index < pages.Length; index++)
            {
                bool active = index == selected;
                pages[index].gameObject.SetActive(active);
                tabs[index].targetGraphic.color = active ? Accent : Raised;
                tabs[index].transform.localScale = active ? Vector3.one * 1.02f : Vector3.one;
            }
        }

        private static string BuildCompetencyCard(EcdCompetencyResult item)
        {
            int filled = Mathf.Clamp(Mathf.RoundToInt(item.score / 3f * 8f), 0, 8);
            var text = new StringBuilder();
            text.Append("<size=19><b>").Append(item.label).Append("</b></size>    <color=#52D7BE><size=28>")
                .Append(item.score.ToString("0.0")).Append("</size></color><size=13> / 3.0</size>\n")
                .Append("<size=19><color=#52D7BE>").Append(new string('■', filled)).Append("</color><color=#28404E>")
                .Append(new string('■', 8 - filled)).Append("</color></size>\n")
                .Append("<size=14><color=#9FB7BD>수집 근거</color>  ").Append(item.evidenceCount)
                .Append("개    <color=#9FB7BD>가중치</color>  ").Append(item.weight.ToString("0.00")).Append("</size>\n");
            if (item.evidenceCount > 0 && item.evidence.Length > 0)
            {
                EcdEvidenceTrace trace = item.evidence[item.evidence.Length - 1];
                text.Append("<size=14><color=#9FB7BD>최근 관찰</color>  상황 #").Append(trace.beatIndex + 1)
                    .Append(" · ").Append(trace.source).Append("\n")
                    .Append("<color=#9FB7BD>관찰행동</color>  ").Append(trace.observableId).Append("</size>");
            }
            else
            {
                text.Append("<size=14><color=#FFB85C>이번 세션에서 직접 근거가 수집되지 않았습니다.</color>\n")
                    .Append("<color=#9FB7BD>다음 시도에서 관련 관찰행동을 명시적으로 사용하세요.</color></size>");
            }
            return text.ToString();
        }
        private static string BuildInterventions(ResearchDebriefReport report)
        {
            var text = new StringBuilder();
            if (report.interventionTimeline.Length == 0) return "기록된 교사 개입이 없습니다.";
            foreach (InterventionTimelineItem item in report.interventionTimeline)
            {
                float change = item.valenceAfter - item.valenceBefore;
                string direction = change > 0.05f ? "정서 개선" : change < -0.05f ? "정서 악화" : "정서 유지";
                text.Append("<color=#52D7BE><size=18><b>#").Append(item.beatIndex + 1).Append("</b></size></color>  <b>")
                    .Append(item.actionSource).Append("</b>     <color=#9FB7BD>정서가 ")
                    .Append(item.valenceBefore.ToString("+0.0;-0.0;0.0")).Append(" → ")
                    .Append(item.valenceAfter.ToString("+0.0;-0.0;0.0")).Append("</color>\n")
                    .Append("<size=13><color=#9FB7BD>").Append(direction);
                if (!string.IsNullOrWhiteSpace(item.evidenceSummary))
                {
                    text.Append(" · ").Append(item.evidenceSummary);
                }
                text.Append("</color></size>\n")
                    .Append("<size=12><color=#6F8993>").Append(EscapeRichText(item.teacherUtteranceSummary)).Append("</color></size>\n");
            }
            return text.ToString();
        }
        private static string BuildSpeechCoaching(ResearchDebriefReport report)
        {
            var text = new StringBuilder();
            InterventionTimelineItem latest = report.interventionTimeline.Length > 0
                ? report.interventionTimeline[report.interventionTimeline.Length - 1]
                : null;
            if (latest == null)
            {
                return "기록된 교사 발화가 없습니다.";
            }

            text.Append("<color=#52D7BE><size=13>발화 요약 · 원문 비저장</size></color>\n")
                .Append("<size=17><b>").Append(EscapeRichText(latest.teacherUtteranceSummary)).Append("</b></size>\n\n")
                .Append("<color=#FFB85C><size=13>권장 대체 발화</size></color>\n")
                .Append("<size=16>“").Append(EscapeRichText(latest.recommendedUtterance)).Append("”</size>\n\n")
                .Append("<color=#9FB7BD><size=13>평가 근거</size></color>\n")
                .Append(EscapeRichText(latest.evaluationRationale)).Append("\n\n")
                .Append("<color=#9FB7BD><size=13>놓친 신호</size></color>\n");

            if (report.missedSignals.Length == 0)
            {
                text.Append("<color=#52D7BE>핵심 관찰행동 근거가 모두 확인되었습니다.</color>");
                return text.ToString();
            }

            int count = Mathf.Min(2, report.missedSignals.Length);
            for (int index = 0; index < count; index++)
            {
                MissedSignal item = report.missedSignals[index];
                text.Append("<b>").Append(EscapeRichText(item.label)).Append("</b> · ")
                    .Append(EscapeRichText(item.message));
                if (index < count - 1)
                {
                    text.Append("\n");
                }
            }

            if (report.missedSignals.Length > count)
            {
                text.Append("\n<size=12><color=#9FB7BD>외 ")
                    .Append(report.missedSignals.Length - count)
                    .Append("개 신호는 역량·근거 탭에서 확인</color></size>");
            }

            return text.ToString();
        }

        private static string EscapeRichText(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "근거 없음"
                : value.Replace("<", "＜").Replace(">", "＞");
        }

        private static EcdCompetencyResult Extreme(ResearchDebriefReport report, bool highest)
        {
            EcdCompetencyResult result = null;
            foreach (EcdCompetencyResult candidate in report.competencies)
                if (result == null || (highest ? candidate.score > result.score : candidate.score < result.score)) result = candidate;
            return result;
        }

        private static string FormatScore(EcdCompetencyResult item) => item == null ? "-" : item.score.ToString("0.0");

        private AffectTrendGraphic Graph(Transform parent, string name)
        {
            var instance = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(AffectTrendGraphic));
            instance.transform.SetParent(parent, false);
            AffectTrendGraphic result = instance.GetComponent<AffectTrendGraphic>();
            result.raycastTarget = false;
            return result;
        }

        private RectTransform Panel(Transform parent, string name, Color color)
        {
            var instance = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            instance.transform.SetParent(parent, false);
            Image image = instance.GetComponent<Image>();
            image.color = color;
            if (roundedSprite != null)
            {
                image.sprite = roundedSprite;
                image.type = Image.Type.Sliced;
            }
            return (RectTransform)instance.transform;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 min, Vector2 max, Color color)
        {
            RectTransform panel = Panel(parent, name, color);
            SetRect(panel, min, max);
            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            panel.gameObject.AddComponent<ButtonMotion>();
            TMP_Text caption = Text(panel, "Label", label, 13f, TextAlignmentOptions.Center, Primary);
            caption.fontStyle = FontStyles.Bold;
            SetRect(caption.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 2f), new Vector2(-8f, -2f));
            return button;
        }

        private TMP_Text Text(Transform parent, string name, string value, float size, TextAlignmentOptions alignment, Color color)
        {
            var instance = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            instance.transform.SetParent(parent, false);
            TMP_Text result = instance.GetComponent<TMP_Text>();
            result.font = font;
            result.text = value;
            result.fontSize = size;
            result.color = color;
            result.alignment = alignment;
            result.textWrappingMode = TextWrappingModes.Normal;
            result.overflowMode = TextOverflowModes.Ellipsis;
            return result;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
