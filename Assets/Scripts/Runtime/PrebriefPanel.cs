using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Session pre-brief overlay: introduces the teacher role, the focal student,
    /// the four response principles, and the assessed competencies before beat 1.
    /// Built at runtime on the training canvas; dismissed by the start button or
    /// automatically by the first teacher action (QA drivers invoke buttons directly).
    /// </summary>
    public sealed class PrebriefPanel
    {
        private static readonly Color Overlay = new Color(0.02f, 0.05f, 0.06f, 0.86f);
        private static readonly Color CardSurface = new Color(0.05f, 0.12f, 0.14f, 0.98f);
        private static readonly Color Accent = new Color(0.32f, 0.84f, 0.75f, 1f);
        private static readonly Color TextMain = new Color(0.92f, 0.97f, 0.96f, 1f);
        private static readonly Color TextSoft = new Color(0.62f, 0.72f, 0.74f, 1f);

        private readonly GameObject root;

        private PrebriefPanel(GameObject root)
        {
            this.root = root;
        }

        public bool IsVisible => root != null && root.activeSelf;

        public void Dismiss()
        {
            if (root != null && root.activeSelf)
            {
                root.SetActive(false);
                UnityEngine.Object.Destroy(root, 0.1f);
            }
        }

        public static PrebriefPanel Show(
            Canvas canvas,
            TMP_FontAsset font,
            CrisisScenarioProfile scenario,
            Action onStart)
        {
            if (canvas == null || font == null)
            {
                return null;
            }

            var rootObject = new GameObject("PrebriefOverlay", typeof(RectTransform), typeof(Image));
            var rootRect = (RectTransform)rootObject.transform;
            rootRect.SetParent(canvas.transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootObject.GetComponent<Image>().color = Overlay;
            rootRect.SetAsLastSibling();

            RectTransform card = Panel(rootRect, "PrebriefCard", CardSurface);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(760f, 560f);

            Icon(card, "icon_guide", new Vector2(40f, -62f), 34f);
            Label(card, font, "Title", "훈련 시작 전 안내", 26, FontStyles.Bold, Accent,
                new Vector2(84f, -66f), new Vector2(-40f, -24f));
            Label(card, font, "Role",
                "당신은 담임 교사입니다. 정서·행동 위기 신호를 보이는 학생에게 여섯 개 상황에 걸쳐 대응합니다.\n" +
                "각 상황에서 학생을 관찰한 뒤 대응을 선택하거나, 학생에게 직접 말을 건넬 수 있습니다.",
                16, FontStyles.Normal, TextMain, new Vector2(40f, -132f), new Vector2(-40f, -72f));

            Icon(card, "icon_student", new Vector2(40f, -168f), 24f);
            Label(card, font, "StudentHeading", "오늘의 학생", 17, FontStyles.Bold, Accent,
                new Vector2(72f, -172f), new Vector2(-40f, -140f));
            Label(card, font, "StudentBody", DescribeStudent(scenario), 15, FontStyles.Normal, TextMain,
                new Vector2(40f, -244f), new Vector2(-40f, -176f));

            Icon(card, "icon_respond", new Vector2(40f, -280f), 24f);
            Label(card, font, "PrincipleHeading", "좋은 대응의 네 가지 원칙", 17, FontStyles.Bold, Accent,
                new Vector2(72f, -284f), new Vector2(-40f, -252f));
            Label(card, font, "PrincipleBody",
                "정서 안정과 존엄 보호  ·  선택권 제공  ·  안전 확인  ·  수업 복귀 경로 연결",
                15, FontStyles.Normal, TextMain, new Vector2(40f, -320f), new Vector2(-40f, -288f));

            Icon(card, "icon_debrief", new Vector2(40f, -356f), 24f);
            Label(card, font, "CompetencyHeading", "이번 훈련에서 관찰되는 역량", 17, FontStyles.Bold, Accent,
                new Vector2(72f, -360f), new Vector2(-40f, -328f));
            Label(card, font, "CompetencyBody",
                "학생 존엄 · 낮은 자극 · 감정 인정 · 선택권 · 안전 · 수업 복귀",
                15, FontStyles.Normal, TextMain, new Vector2(40f, -396f), new Vector2(-40f, -364f));

            Label(card, font, "EthicsNote",
                "이 시뮬레이션은 훈련 도구입니다. 실제 반복적인 위기 상황은 학교 위기대응 절차와\n상담 전문 인력 연계를 따릅니다.",
                13, FontStyles.Normal, TextSoft, new Vector2(40f, -458f), new Vector2(-40f, -404f));

            var panel = new PrebriefPanel(rootObject);

            var buttonObject = new GameObject("PrebriefStartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.SetParent(card, false);
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 58f);
            buttonRect.sizeDelta = new Vector2(240f, 52f);
            buttonObject.GetComponent<Image>().color = Accent;
            TMP_Text buttonLabel = Label(buttonRect, font, "Label", "훈련 시작", 18, FontStyles.Bold,
                new Color(0.03f, 0.1f, 0.1f, 1f), Vector2.zero, Vector2.zero);
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                panel.Dismiss();
                onStart?.Invoke();
            });

            UiEntranceMotion.Play(card.gameObject, 0.28f);
            return panel;
        }

        private static string DescribeStudent(CrisisScenarioProfile scenario)
        {
            StudentPersonaAsset persona = FindPersona(scenario?.personaId);
            if (persona == null)
            {
                return "초등학교 고학년 학생이 정서·행동 위기 신호를 보이고 있습니다. 행동 이면의 정서를 먼저 살펴보세요.";
            }

            string strengths = JoinLabels(persona.Strengths, StrengthLabel);
            string needs = JoinLabels(persona.SupportNeeds, SupportNeedLabel);
            return $"<b>{persona.DisplayName}</b> · 초등 고학년\n" +
                $"강점: {strengths}\n" +
                $"지원이 필요한 부분: {needs}";
        }

        private static StudentPersonaAsset FindPersona(string personaId)
        {
            if (string.IsNullOrWhiteSpace(personaId))
            {
                return null;
            }

            StudentPersonaAsset[] personas = Resources.LoadAll<StudentPersonaAsset>("Training/Personas");
            foreach (StudentPersonaAsset persona in personas)
            {
                if (string.Equals(persona.PersonaId, personaId, StringComparison.OrdinalIgnoreCase))
                {
                    return persona;
                }
            }

            return null;
        }

        private static string JoinLabels<T>(System.Collections.Generic.IReadOnlyList<T> values, Func<T, string> label)
        {
            if (values == null || values.Count == 0)
            {
                return "정보 없음";
            }

            var builder = new System.Text.StringBuilder();
            for (int index = 0; index < values.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(label(values[index]));
            }

            return builder.ToString();
        }

        private static string StrengthLabel(StudentStrength strength)
        {
            return strength switch
            {
                StudentStrength.VerbalReasoning => "언어 표현",
                StudentStrength.Creativity => "창의성",
                StudentStrength.PeerConnection => "또래 관계",
                StudentStrength.Persistence => "끈기",
                StudentStrength.Humor => "유머",
                StudentStrength.VisualLearning => "시각적 학습",
                _ => strength.ToString()
            };
        }

        private static string SupportNeedLabel(PersonaSupportNeed need)
        {
            return need switch
            {
                PersonaSupportNeed.PredictableChoice => "예측 가능한 선택지",
                PersonaSupportNeed.LowStimulusLanguage => "낮은 자극의 언어",
                PersonaSupportNeed.ProcessingTime => "처리 시간",
                PersonaSupportNeed.PrivateCorrection => "비공개 지도",
                PersonaSupportNeed.MovementBreak => "움직임 휴식",
                PersonaSupportNeed.CoRegulation => "공동 조절",
                _ => need.ToString()
            };
        }

        private static void Icon(RectTransform parent, string spriteName, Vector2 topLeft, float size)
        {
            Sprite sprite = Resources.Load<Sprite>("Training/Icons/" + spriteName);
            if (sprite == null)
            {
                return;
            }

            var iconObject = new GameObject(spriteName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)iconObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = new Vector2(size, size);
            Image image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static RectTransform Panel(RectTransform parent, string name, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panelObject.transform;
            rect.SetParent(parent, false);
            panelObject.GetComponent<Image>().color = color;
            return rect;
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
            Vector2 offsetMax)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var label = textObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            return label;
        }
    }
}
