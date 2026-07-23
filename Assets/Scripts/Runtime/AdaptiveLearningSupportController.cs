using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    public sealed class LearningSupportContext
    {
        public CrisisStage stage;
        public PeerAttentionPattern peerAttention;
        public bool presentationAvoidance;
        public TeacherCompetency[] teacherGoals = Array.Empty<TeacherCompetency>();
        public ScenarioBeat beat;
        public TeacherResponseOption latestResponse;
    }

    [DisallowMultipleComponent]
    public sealed class AdaptiveLearningSupportController : MonoBehaviour
    {
        private LearningSupportPolicy policy;
        private ApprovedKnowledgeCatalog knowledgeCatalog;
        private LearningSupportStateMachine stateMachine;
        private AdaptiveLearningSupportPanel panel;
        private Func<bool> canOfferSupport;
        private Func<bool> isAfterAction;
        private Func<LearningSupportContext> contextProvider;
        private Action<TrainingEventKind, LearningSupportTelemetry> recordTelemetry;
        private LearningSupportDecision visibleDecision;
        private double visibleSinceSeconds;
        private bool initialized;

        public void Initialize(
            Canvas canvas,
            TMP_FontAsset font,
            LearningSupportPolicy authoredPolicy,
            Func<bool> canOffer,
            Func<bool> afterAction,
            Func<LearningSupportContext> provideContext,
            Action<TrainingEventKind, LearningSupportTelemetry> telemetryRecorder)
        {
            if (initialized || canvas == null || font == null)
            {
                return;
            }

            policy = authoredPolicy != null ? authoredPolicy : LearningSupportPolicy.LoadDefault();
            knowledgeCatalog = ApprovedKnowledgeCatalog.LoadDefault();
            stateMachine = new LearningSupportStateMachine(policy);
            canOfferSupport = canOffer;
            isAfterAction = afterAction;
            contextProvider = provideContext;
            recordTelemetry = telemetryRecorder;
            panel = new AdaptiveLearningSupportPanel(canvas, font, HandleManualRequest, HandleDismissed);
            initialized = true;
        }

        public void BeginBeat(int attemptNumber)
        {
            if (!initialized)
            {
                return;
            }

            HideVisibleSupport(true);
            stateMachine.BeginBeat(attemptNumber, Time.unscaledTimeAsDouble);
            panel.SetRequestLabel("도움 요청");
        }

        public void RecordOutcome(TeacherResponseOption response)
        {
            if (!initialized || response == null)
            {
                return;
            }

            LearningSupportContext context = contextProvider?.Invoke();
            if (context != null)
            {
                context.latestResponse = response;
            }

            if (stateMachine.RecordOutcome(
                    response.quality,
                    Time.unscaledTimeAsDouble,
                    out LearningSupportDecision decision))
            {
                Show(decision, context);
            }

            panel.SetRequestLabel("대응 비교");
        }

        public LearningSupportTelemetry CaptureActionSnapshot()
        {
            return ToTelemetry(stateMachine?.CurrentActionSnapshot() ?? default, 0L);
        }

        public void EndSession()
        {
            if (!initialized)
            {
                return;
            }

            HideVisibleSupport(true);
            panel.SetAvailable(false);
        }

        private void Update()
        {
            if (!initialized || canOfferSupport?.Invoke() != true)
            {
                return;
            }

            if (stateMachine.TryTick(Time.unscaledTimeAsDouble, out LearningSupportDecision decision))
            {
                Show(decision, contextProvider?.Invoke());
            }
        }

        private void HandleManualRequest()
        {
            if (!initialized || (canOfferSupport?.Invoke() != true && isAfterAction?.Invoke() != true))
            {
                return;
            }

            bool afterAction = isAfterAction?.Invoke() == true;
            LearningSupportDecision decision = stateMachine.RequestManual(
                Time.unscaledTimeAsDouble,
                afterAction);
            Show(decision, contextProvider?.Invoke());
            panel.SetRequestLabel(afterAction ? "대응 비교" : "도움 더 보기");
        }

        private void Show(LearningSupportDecision decision, LearningSupportContext context)
        {
            HideVisibleSupport(true);
            visibleDecision = decision;
            visibleSinceSeconds = Time.unscaledTimeAsDouble;
            panel.Show(
                HeadingFor(decision.Level),
                BuildBody(decision.Level, context),
                decision.Automatic ? "상황 흐름에 따라 제공된 단서" : "교사가 요청한 단서");
            panel.SetSource(BuildSourceLabel(decision, context));
            recordTelemetry?.Invoke(
                TrainingEventKind.LearningSupportShown,
                ToTelemetry(decision, 0L));
        }

        private void HandleDismissed()
        {
            if (visibleDecision.Level == LearningSupportLevel.Hidden)
            {
                return;
            }

            long duration = (long)Math.Max(
                0d,
                Math.Round((Time.unscaledTimeAsDouble - visibleSinceSeconds) * 1000d));
            recordTelemetry?.Invoke(
                TrainingEventKind.LearningSupportDismissed,
                ToTelemetry(visibleDecision, duration));
            visibleDecision = default;
        }

        private void HideVisibleSupport(bool recordDismissal)
        {
            if (panel == null || !panel.IsVisible)
            {
                return;
            }

            if (recordDismissal)
            {
                HandleDismissed();
            }
            panel.Hide();
        }

        private string BuildBody(LearningSupportLevel level, LearningSupportContext context)
        {
            context ??= new LearningSupportContext();
            LearningSupportStagePrompt prompt = policy.PromptFor(context.stage);
            if (level == LearningSupportLevel.ObservationCue)
            {
                string cue = prompt?.observationCue ??
                    "학생의 시선, 자세, 손의 긴장과 주변 또래의 반응을 함께 살펴보세요.";
                return cue + PeerAttentionSuffix(context.peerAttention, context.presentationAvoidance);
            }

            if (level == LearningSupportLevel.Principle)
            {
                string cue = prompt?.principleCue ??
                    "행동을 바로 교정하기보다 정서를 확인하고 낮은 자극으로 선택권을 제공합니다.";
                string goals = GoalLabels(context.teacherGoals);
                return string.IsNullOrWhiteSpace(goals) ? cue : cue + "\n\n이번 단계의 관찰 초점 · " + goals;
            }

            if (level == LearningSupportLevel.PostActionContrast)
            {
                return BuildPostActionContrast(context);
            }

            return string.Empty;
        }

        private string BuildSourceLabel(
            LearningSupportDecision decision,
            LearningSupportContext context)
        {
            string delivery = decision.Automatic
                ? "상황 흐름에 따라 제공된 단서"
                : "교사가 요청한 단서";
            if (knowledgeCatalog == null || context == null)
            {
                return delivery + " · 작성형 정책";
            }

            GroundedKnowledgeCitation[] citations = knowledgeCatalog.Retrieve(
                context.stage.ToString(), context.stage, context.teacherGoals, 2);
            if (citations.Length == 0)
            {
                return delivery + " · 승인 근거 없음(작성형 정책 사용)";
            }

            var label = new StringBuilder(delivery + " · 승인 문서 ");
            for (int index = 0; index < citations.Length; index++)
            {
                if (index > 0) label.Append(" / ");
                label.Append(citations[index].sourceTitle);
                if (!string.IsNullOrWhiteSpace(citations[index].locator))
                {
                    label.Append(" ").Append(citations[index].locator);
                }
            }
            return label.ToString();
        }

        private static string BuildPostActionContrast(LearningSupportContext context)
        {
            TeacherResponseOption selected = context.latestResponse;
            TeacherResponseOption best = BestOption(context.beat);
            if (best == null)
            {
                return "방금 대응에서 학생의 정서가 어떻게 달라졌는지 확인하고, 다음에는 관찰 근거를 한 문장으로 반영해 보세요.";
            }

            if (selected != null && selected.quality >= best.quality)
            {
                return $"방금 대응의 강점 · {best.label}\n\n학생에게 실제로 전달된 선택권과 안전감을 정서 변화에서 확인해 보세요.";
            }

            string selectedLabel = selected?.label ?? "방금 직접 발화";
            return $"방금 대응 · {selectedLabel}\n비교할 원리 · {best.label}\n\n대안 발화 예시 · “{best.spokenResponse}”";
        }

        private static TeacherResponseOption BestOption(ScenarioBeat beat)
        {
            if (beat?.options == null || beat.options.Length == 0)
            {
                return null;
            }

            TeacherResponseOption best = beat.options[0];
            for (int index = 1; index < beat.options.Length; index++)
            {
                if (beat.options[index] != null && (best == null || beat.options[index].quality > best.quality))
                {
                    best = beat.options[index];
                }
            }
            return best;
        }

        private static string PeerAttentionSuffix(PeerAttentionPattern attention, bool presentationAvoidance)
        {
            if (presentationAvoidance)
            {
                return "\n\n발표 자체보다 ‘여러 사람이 보고 있다’는 부담이 커지고 있는지 확인하세요.";
            }

            return attention switch
            {
                PeerAttentionPattern.FocalStudentConcern => "\n\n주변 학생의 걱정 어린 시선도 자극이 될 수 있습니다.",
                PeerAttentionPattern.PeerDistraction => "\n\n또래의 웃음과 움직임이 긴장을 키우는지 함께 확인하세요.",
                PeerAttentionPattern.PresentationAudience => "\n\n집단의 시선이 학생에게 압박으로 느껴지는지 살펴보세요.",
                _ => string.Empty
            };
        }

        private static string GoalLabels(IReadOnlyList<TeacherCompetency> goals)
        {
            if (goals == null || goals.Count == 0)
            {
                return string.Empty;
            }

            var text = new StringBuilder();
            for (int index = 0; index < goals.Count; index++)
            {
                if (index > 0)
                {
                    text.Append(" · ");
                }
                text.Append(CompetencyLabel(goals[index]));
            }
            return text.ToString();
        }

        private static string CompetencyLabel(TeacherCompetency competency)
        {
            return competency switch
            {
                TeacherCompetency.StudentDignity => "학생 존엄",
                TeacherCompetency.LowStimulusResponse => "낮은 자극",
                TeacherCompetency.EmotionAcknowledgement => "감정 인정",
                TeacherCompetency.StudentAgency => "선택권",
                TeacherCompetency.Safety => "안전",
                TeacherCompetency.InstructionalReentry => "수업 복귀",
                _ => competency.ToString()
            };
        }

        private static string HeadingFor(LearningSupportLevel level)
        {
            return level switch
            {
                LearningSupportLevel.ObservationCue => "먼저 볼 것",
                LearningSupportLevel.Principle => "대응 원리",
                LearningSupportLevel.PostActionContrast => "행동 후 비교",
                _ => "학습 지원"
            };
        }

        private static LearningSupportTelemetry ToTelemetry(
            LearningSupportDecision decision,
            long displayedMilliseconds)
        {
            return new LearningSupportTelemetry
            {
                level = decision.Level,
                trigger = decision.Trigger,
                automatic = decision.Automatic,
                idleMilliseconds = decision.IdleMilliseconds,
                requestCount = decision.RequestCount,
                consecutiveMisses = decision.ConsecutiveMisses,
                displayedMilliseconds = Math.Max(0L, displayedMilliseconds)
            };
        }

        private sealed class AdaptiveLearningSupportPanel
        {
            private static readonly Color Surface = new Color(0.035f, 0.085f, 0.10f, 0.96f);
            private static readonly Color ButtonSurface = new Color(0.04f, 0.14f, 0.16f, 0.94f);
            private static readonly Color Accent = new Color(0.32f, 0.84f, 0.75f, 1f);
            private static readonly Color MainText = new Color(0.92f, 0.97f, 0.96f, 1f);
            private static readonly Color SoftText = new Color(0.62f, 0.72f, 0.74f, 1f);

            private readonly RectTransform root;
            private readonly TMP_Text heading;
            private readonly TMP_Text body;
            private readonly TMP_Text source;
            private readonly TMP_Text requestLabel;
            private readonly GameObject requestButton;

            public AdaptiveLearningSupportPanel(
                Canvas canvas,
                TMP_FontAsset font,
                Action request,
                Action dismissed)
            {
                Sprite rounded = canvas.transform.Find("SituationPanel")?.GetComponent<Image>()?.sprite;

                var buttonObject = new GameObject(
                    "LearningSupportRequestButton",
                    typeof(RectTransform), typeof(Image), typeof(Button));
                RectTransform buttonRect = (RectTransform)buttonObject.transform;
                requestButton = buttonObject;
                buttonRect.SetParent(canvas.transform, false);
                buttonRect.anchorMin = Vector2.one;
                buttonRect.anchorMax = Vector2.one;
                buttonRect.pivot = Vector2.one;
                buttonRect.anchoredPosition = new Vector2(-34f, -68f);
                buttonRect.sizeDelta = new Vector2(142f, 38f);
                StyleSurface(buttonObject.GetComponent<Image>(), rounded, ButtonSurface);
                requestLabel = Label(buttonRect, font, "Label", "도움 요청", 14, FontStyles.Bold, Accent);
                requestLabel.alignment = TextAlignmentOptions.Center;
                buttonObject.GetComponent<Button>().onClick.AddListener(() => request?.Invoke());

                var rootObject = new GameObject(
                    "AdaptiveLearningSupportPanel",
                    typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                root = (RectTransform)rootObject.transform;
                root.SetParent(canvas.transform, false);
                root.anchorMin = Vector2.one;
                root.anchorMax = Vector2.one;
                root.pivot = Vector2.one;
                root.anchoredPosition = new Vector2(-34f, -114f);
                root.sizeDelta = new Vector2(440f, 230f);
                StyleSurface(rootObject.GetComponent<Image>(), rounded, Surface);

                heading = Label(root, font, "Heading", string.Empty, 19, FontStyles.Bold, Accent);
                SetRect(heading.rectTransform, new Vector2(24f, -54f), new Vector2(-62f, -18f));

                body = Label(root, font, "Body", string.Empty, 15, FontStyles.Normal, MainText);
                body.lineSpacing = 7f;
                body.overflowMode = TextOverflowModes.Ellipsis;
                SetRect(body.rectTransform, new Vector2(24f, -174f), new Vector2(-24f, -60f));

                source = Label(root, font, "Source", string.Empty, 12, FontStyles.Normal, SoftText);
                SetRect(source.rectTransform, new Vector2(24f, -210f), new Vector2(-24f, -180f));

                var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Button));
                RectTransform closeRect = (RectTransform)closeObject.transform;
                closeRect.SetParent(root, false);
                closeRect.anchorMin = Vector2.one;
                closeRect.anchorMax = Vector2.one;
                closeRect.pivot = Vector2.one;
                closeRect.anchoredPosition = new Vector2(-14f, -12f);
                closeRect.sizeDelta = new Vector2(44f, 36f);
                TMP_Text closeLabel = Label(closeRect, font, "Label", "닫기", 12, FontStyles.Normal, SoftText);
                closeLabel.alignment = TextAlignmentOptions.Center;
                closeObject.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Hide();
                    dismissed?.Invoke();
                });

                root.SetAsLastSibling();
                buttonRect.SetAsLastSibling();
                rootObject.SetActive(false);
            }

            public bool IsVisible => root != null && root.gameObject.activeSelf;

            public void SetRequestLabel(string value)
            {
                requestLabel.text = value;
            }

            public void SetAvailable(bool available)
            {
                requestButton.SetActive(available);
            }

            public void Show(string headingText, string bodyText, string sourceText)
            {
                heading.text = headingText;
                body.text = bodyText;
                source.text = sourceText + " · 점수에 직접 반영되지 않음";
                root.gameObject.SetActive(true);
                root.SetAsLastSibling();
                UiEntranceMotion.Play(root.gameObject, 0.2f);
            }

            public void SetSource(string value)
            {
                source.text = (value ?? string.Empty) + " · 점수에 직접 반영되지 않음";
            }

            public void Hide()
            {
                if (root != null)
                {
                    root.gameObject.SetActive(false);
                }
            }

            private static TMP_Text Label(
                RectTransform parent,
                TMP_FontAsset font,
                string name,
                string value,
                float size,
                FontStyles styles,
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
                label.fontStyle = styles;
                label.color = color;
                label.alignment = TextAlignmentOptions.TopLeft;
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
}
