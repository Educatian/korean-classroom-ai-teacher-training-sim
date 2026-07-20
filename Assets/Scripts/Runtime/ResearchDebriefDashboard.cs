using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class AffectTrendGraphic : MaskableGraphic
    {
        private AffectTrendPoint[] points = Array.Empty<AffectTrendPoint>();

        public void SetPoints(AffectTrendPoint[] values)
        {
            points = values ?? Array.Empty<AffectTrendPoint>();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if (points.Length < 2)
            {
                return;
            }

            Rect graph = rectTransform.rect;
            DrawSeries(helper, graph, point => point.valence * 0.5f + 0.5f, new Color32(67, 213, 181, 255), 3.5f);
            DrawSeries(helper, graph, point => point.arousal, new Color32(255, 184, 92, 255), 2.5f);
        }

        private void DrawSeries(
            VertexHelper helper,
            Rect graph,
            Func<AffectTrendPoint, float> selector,
            Color32 seriesColor,
            float thickness)
        {
            for (int index = 1; index < points.Length; index++)
            {
                Vector2 from = Point(graph, index - 1, selector(points[index - 1]));
                Vector2 to = Point(graph, index, selector(points[index]));
                AddLine(helper, from, to, thickness, seriesColor);
            }
        }

        private Vector2 Point(Rect graph, int index, float value)
        {
            float x = Mathf.Lerp(graph.xMin, graph.xMax, index / Mathf.Max(1f, points.Length - 1f));
            float y = Mathf.Lerp(graph.yMin + 3f, graph.yMax - 3f, Mathf.Clamp01(value));
            return new Vector2(x, y);
        }

        private static void AddLine(
            VertexHelper helper,
            Vector2 from,
            Vector2 to,
            float thickness,
            Color32 lineColor)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * thickness * 0.5f;
            int start = helper.currentVertCount;
            helper.AddVert(from - normal, lineColor, Vector2.zero);
            helper.AddVert(from + normal, lineColor, Vector2.zero);
            helper.AddVert(to + normal, lineColor, Vector2.zero);
            helper.AddVert(to - normal, lineColor, Vector2.zero);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }
    }

    [DisallowMultipleComponent]
    public sealed class ResearchDebriefDashboard : MonoBehaviour
    {
        private RectTransform root;
        private TMP_Text summaryText;
        private TMP_Text detailsText;
        private TMP_Text exportStatus;
        private AffectTrendGraphic affectGraph;
        private Button retryButton;
        private Button exportButton;
        private ResearchDebriefReport currentReport;
        private Action retryAction;
        private FullResearchDashboard fullDashboard;

        public ResearchDashboardState State { get; } = new ResearchDashboardState();

        public void Initialize(
            RectTransform panel,
            TMP_Text existingText,
            RectTransform fullDashboardPanel,
            Action requestFull,
            Action returnMain)
        {
            root = panel;
            summaryText = existingText;
            summaryText.alignment = TextAlignmentOptions.TopLeft;
            summaryText.fontSize = 17f;
            summaryText.enableAutoSizing = true;
            summaryText.fontSizeMin = 13f;
            summaryText.fontSizeMax = 17f;
            SetRect(summaryText.rectTransform, new Vector2(0f, 0.57f), Vector2.one, new Vector2(20f, 12f), new Vector2(-20f, -18f));

            RectTransform graphSurface = CreatePanel("AffectTrendSurface", new Color(0.035f, 0.07f, 0.12f, 0.92f));
            SetRect(graphSurface, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.56f), Vector2.zero, Vector2.zero);
            var graphObject = new GameObject("AffectTrend", typeof(RectTransform), typeof(CanvasRenderer), typeof(AffectTrendGraphic));
            graphObject.transform.SetParent(graphSurface, false);
            affectGraph = graphObject.GetComponent<AffectTrendGraphic>();
            affectGraph.raycastTarget = false;
            SetRect(affectGraph.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 6f), new Vector2(-8f, -6f));
            TMP_Text legend = CreateText(graphSurface, "Legend", "정서가  ●   각성  ●", 11f, TextAlignmentOptions.TopLeft);
            legend.color = new Color(0.78f, 0.88f, 0.88f, 1f);
            SetRect(legend.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));

            detailsText = CreateText(root, "ResearchDetails", string.Empty, 13f, TextAlignmentOptions.TopLeft);
            detailsText.enableAutoSizing = true;
            detailsText.fontSizeMin = 10f;
            detailsText.fontSizeMax = 13f;
            SetRect(detailsText.rectTransform, new Vector2(0f, 0.14f), new Vector2(1f, 0.34f), new Vector2(20f, 4f), new Vector2(-20f, -4f));

            retryButton = CreateButton("RetryButton", "같은 상황 재시도", new Vector2(0.05f, 0.035f), new Vector2(0.40f, 0.125f));
            exportButton = CreateButton("OpenResearchDashboardButton", "큰 대시보드 열기  ›", new Vector2(0.43f, 0.035f), new Vector2(0.95f, 0.125f));
            retryButton.onClick.AddListener(() => retryAction?.Invoke());
            exportButton.onClick.AddListener(() => requestFull?.Invoke());
            exportStatus = CreateText(root, "ExportStatus", string.Empty, 9f, TextAlignmentOptions.Bottom);
            SetRect(exportStatus.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.03f), Vector2.zero, Vector2.zero);

            Sprite rounded = root.GetComponent<Image>()?.sprite;
            fullDashboard = new FullResearchDashboard(fullDashboardPanel, summaryText.font, rounded, returnMain);
            root.gameObject.SetActive(false);
            fullDashboard.SetVisible(false);
        }
        public void Show(ResearchDebriefReport report, Action retry)
        {
            currentReport = report;
            retryAction = retry;
            if (report == null)
            {
                return;
            }

            State.UnlockSummary();
            summaryText.text = BuildSummary(report);
            detailsText.text = BuildDetails(report);
            affectGraph.SetPoints(report.affectTrend);
            exportStatus.text = "내보내기에는 원문 발화가 포함되지 않습니다.";
            fullDashboard.Show(report, retry, ExportFromFullDashboard);
            ShowSummary();
        }

        public void ShowSummary()
        {
            State.ReturnToSummary();
            root.gameObject.SetActive(State.IsUnlocked);
            fullDashboard.SetVisible(false);
        }

        public bool OpenFull()
        {
            if (!State.OpenFull())
            {
                return false;
            }

            root.gameObject.SetActive(false);
            fullDashboard.SetVisible(true);
            return true;
        }

        public void HideAll()
        {
            root.gameObject.SetActive(false);
            fullDashboard.SetVisible(false);
        }

        private void ExportFromFullDashboard()
        {
            ExportCurrentReport();
            fullDashboard.SetStatus(exportStatus.text);
        }

        private string BuildSummary(ResearchDebriefReport report)
        {
            EcdCompetencyResult strongest = null;
            EcdCompetencyResult priority = null;
            for (int index = 0; index < report.competencies.Length; index++)
            {
                EcdCompetencyResult item = report.competencies[index];
                if (strongest == null || item.score > strongest.score)
                {
                    strongest = item;
                }
                if (priority == null || item.score < priority.score)
                {
                    priority = item;
                }
            }

            return $"<size=12><color=#9FB7BD>훈련 요약</color></size>\n<b>{report.overallLevel}</b>  <color=#52D7BE>{report.averageScore:0.00}/3.00</color>\n<size=12>강점  {strongest?.label ?? "-"}  ·  보완  {priority?.label ?? "-"}</size>";
        }
        private string BuildDetails(ResearchDebriefReport report)
        {
            var text = new StringBuilder("<b>개입 타임라인</b>  ");
            int timelineStart = Mathf.Max(0, report.interventionTimeline.Length - 3);
            for (int index = timelineStart; index < report.interventionTimeline.Length; index++)
            {
                InterventionTimelineItem item = report.interventionTimeline[index];
                text.Append("#").Append(item.beatIndex + 1).Append(" ")
                    .Append(item.actionSource).Append(" ")
                    .Append(item.valenceBefore.ToString("+0.0;-0.0;0.0")).Append("→")
                    .Append(item.valenceAfter.ToString("+0.0;-0.0;0.0"));
                if (index < report.interventionTimeline.Length - 1)
                {
                    text.Append("  ·  ");
                }
            }

            text.Append("\n<b>놓친 신호</b>  ");
            if (report.missedSignals.Length == 0)
            {
                text.Append("핵심 관찰행동 근거가 모두 확인되었습니다.");
            }
            else
            {
                int count = Mathf.Min(2, report.missedSignals.Length);
                for (int index = 0; index < count; index++)
                {
                    MissedSignal item = report.missedSignals[index];
                    text.Append(item.label).Append(": ").Append(item.message);
                    if (index < count - 1)
                    {
                        text.Append("\n");
                    }
                }
            }

            return text.ToString();
        }

        private void ExportCurrentReport()
        {
            if (currentReport == null)
            {
                return;
            }

#if UNITY_WEBGL
            exportStatus.text = "WebGL에서는 서버 내보내기 연결이 필요합니다.";
#else
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, "research-debrief");
                Directory.CreateDirectory(directory);
                string stem = string.IsNullOrWhiteSpace(currentReport.sessionId)
                    ? DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                    : currentReport.sessionId;
                string jsonPath = Path.Combine(directory, stem + ".json");
                string csvPath = Path.Combine(directory, stem + "-competencies.csv");
                File.WriteAllText(jsonPath, JsonUtility.ToJson(currentReport, true), Encoding.UTF8);
                var csv = new StringBuilder("model_id,session_id,competency,score,evidence_count\n");
                for (int index = 0; index < currentReport.competencies.Length; index++)
                {
                    EcdCompetencyResult item = currentReport.competencies[index];
                    csv.Append(Escape(currentReport.modelId)).Append(',')
                        .Append(Escape(currentReport.sessionId)).Append(',')
                        .Append(Escape(item.competency.ToString())).Append(',')
                        .Append(item.score.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                        .Append(item.evidenceCount).Append('\n');
                }

                File.WriteAllText(csvPath, csv.ToString(), Encoding.UTF8);
                exportStatus.text = "익명화된 JSON·CSV 저장 완료";
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                exportStatus.text = "내보내기 실패 · 저장 위치 권한을 확인하세요.";
                Debug.LogWarning($"Research debrief export failed: {exception.GetType().Name}");
            }
#endif
        }

        private RectTransform CreatePanel(string name, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(root, false);
            gameObject.GetComponent<Image>().color = color;
            return (RectTransform)gameObject.transform;
        }

        private Button CreateButton(string name, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform panel = CreatePanel(name, new Color(0.055f, 0.53f, 0.50f, 1f));
            SetRect(panel, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Button button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel.GetComponent<Image>();
            panel.gameObject.AddComponent<ButtonMotion>();
            TMP_Text text = CreateText(panel, "Label", label, 12f, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(5f, 2f), new Vector2(-5f, -2f));
            return button;
        }

        private TMP_Text CreateText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            TMP_Text text = gameObject.GetComponent<TMP_Text>();
            text.font = summaryText.font;
            text.text = value;
            text.fontSize = size;
            text.color = new Color(0.92f, 0.97f, 0.98f, 1f);
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static string Escape(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
