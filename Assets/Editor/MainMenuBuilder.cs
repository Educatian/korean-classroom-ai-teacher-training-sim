using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining.EditorTools
{
    /// <summary>
    /// Deterministically builds the published build's front page (MainMenu.unity)
    /// and registers it as the first scene in the build settings.
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string HeroPath = "Assets/Art/MainMenu/MainMenuHero.jpg";
        private const string FontPath = "Assets/Art/Fonts/NotoSansKR-SDF.asset";

        private static readonly Color Bg = new Color(0.024f, 0.075f, 0.10f, 1f);
        private static readonly Color Teal = new Color(0.32f, 0.84f, 0.75f, 1f);
        private static readonly Color TealDeep = new Color(0.016f, 0.14f, 0.12f, 1f);
        private static readonly Color TextMain = new Color(0.92f, 0.96f, 0.95f, 1f);
        private static readonly Color TextSoft = new Color(0.62f, 0.72f, 0.74f, 1f);
        private static readonly Color ButtonDark = new Color(0.055f, 0.145f, 0.18f, 0.94f);

        [MenuItem("Teacher Training/Build Main Menu Scene")]
        public static void Build()
        {
            EnsureHeroSprite();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite hero = AssetDatabase.LoadAssetAtPath<Sprite>(HeroPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Bg;
            camera.orthographic = true;

            var canvasObject = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            RectTransform root = (RectTransform)canvasObject.transform;

            // Hero backdrop + shade
            Image heroImage = FullRectImage(root, "HeroBackdrop", Color.white);
            if (hero != null)
            {
                heroImage.sprite = hero;
                heroImage.preserveAspect = false;
            }
            else
            {
                heroImage.color = Bg;
            }

            Image shade = FullRectImage(root, "Shade", new Color(0.02f, 0.06f, 0.08f, 0.62f));
            Image bottomShade = FullRectImage(root, "BottomShade", new Color(0.012f, 0.045f, 0.06f, 0.9f));
            RectTransform bottomRect = bottomShade.rectTransform;
            bottomRect.anchorMin = new Vector2(0f, 0f);
            bottomRect.anchorMax = new Vector2(1f, 0.42f);
            bottomRect.offsetMin = Vector2.zero;
            bottomRect.offsetMax = Vector2.zero;

            // Title block
            Text(root, "Chip", "UNITY 6 연구 프로토타입 · 교사 교육 시뮬레이션", font, 17, FontStyles.Bold, Teal,
                new Vector2(0.5f, 1f), new Vector2(-600f, -270f), new Vector2(600f, -230f), TextAlignmentOptions.Center);
            Text(root, "Title", "정서·행동 지원 교사 대응 훈련", font, 62, FontStyles.Bold, TextMain,
                new Vector2(0.5f, 1f), new Vector2(-760f, -390f), new Vector2(760f, -290f), TextAlignmentOptions.Center);
            Text(root, "Subtitle", "한국 초등교실을 재현한 몰입형 시뮬레이터에서 위기 신호를 관찰하고, 대응을 선택하고,\nAI 학생과 직접 대화하며 공동조절 역량을 훈련합니다.",
                font, 21, FontStyles.Normal, TextSoft,
                new Vector2(0.5f, 1f), new Vector2(-620f, -486f), new Vector2(620f, -400f), TextAlignmentOptions.Center);

            var controllerObject = new GameObject("MainMenuController", typeof(MainMenuController));
            MainMenuController controller = controllerObject.GetComponent<MainMenuController>();

            // Menu buttons
            Button startGeneral = MenuButton(root, font, "StartGeneralButton", "일반 교실 훈련 시작", Teal, TealDeep, new Vector2(0.5f, 0f), new Vector2(-250f, 316f), new Vector2(250f, 388f), 26);
            Button startCircle = MenuButton(root, font, "StartCircleButton", "서클 토론 훈련", ButtonDark, TextMain, new Vector2(0.5f, 0f), new Vector2(-250f, 240f), new Vector2(-4f, 302f), 20);
            Button startRecovery = MenuButton(root, font, "StartRecoveryButton", "안정 공간 1:1 대화", ButtonDark, TextMain, new Vector2(0.5f, 0f), new Vector2(4f, 240f), new Vector2(250f, 302f), 20);
            Button infoButton = MenuButton(root, font, "ResearchInfoButton", "연구 정보", ButtonDark, TextSoft, new Vector2(0.5f, 0f), new Vector2(-250f, 166f), new Vector2(-4f, 226f), 19);
            Button quitButton = MenuButton(root, font, "QuitButton", "종료", ButtonDark, TextSoft, new Vector2(0.5f, 0f), new Vector2(4f, 166f), new Vector2(250f, 226f), 19);

            Text(root, "Footer", "연구·교사교육용 프로토타입 · 임상 판단과 학교 위기대응 절차를 대체하지 않습니다", font, 14, FontStyles.Normal,
                new Color(0.45f, 0.56f, 0.58f, 1f),
                new Vector2(0.5f, 0f), new Vector2(-640f, 40f), new Vector2(640f, 70f), TextAlignmentOptions.Center);

            // Research info panel (hidden by default)
            var infoPanelObject = new GameObject("ResearchInfoPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            RectTransform infoRect = (RectTransform)infoPanelObject.transform;
            infoRect.SetParent(root, false);
            infoRect.anchorMin = new Vector2(0.5f, 0.5f);
            infoRect.anchorMax = new Vector2(0.5f, 0.5f);
            infoRect.sizeDelta = new Vector2(880f, 560f);
            infoPanelObject.GetComponent<Image>().color = new Color(0.035f, 0.10f, 0.125f, 0.985f);
            CanvasGroup infoGroup = infoPanelObject.GetComponent<CanvasGroup>();
            infoGroup.alpha = 0f;
            infoGroup.interactable = false;
            infoGroup.blocksRaycasts = false;
            Text(infoRect, "InfoTitle", "연구 정보", font, 30, FontStyles.Bold, Teal,
                new Vector2(0f, 1f), new Vector2(48f, -84f), new Vector2(832f, -36f), TextAlignmentOptions.TopLeft);
            Text(infoRect, "InfoBody",
                "이 시뮬레이터는 교사 교육 연구를 위한 프로토타입입니다.\n\n" +
                "· 평가는 증거중심설계(ECD) 기반의 결정론적 규칙으로만 이루어지며, 생성형 AI는 학생 연기와 조언에만 사용됩니다.\n" +
                "· 발화 원문은 저장되지 않습니다. 해시와 길이만 기록됩니다.\n" +
                "· 참가자는 가명 코드로만 식별되며, 시선 원자료는 명시적 동의 시에만 수집됩니다.\n" +
                "· 훈련 종료 후 정서 궤적·역량 증거·개입 타임라인을 담은 디브리핑이 제공됩니다.\n\n" +
                "이 도구는 임상적 판단, 학교 위기대응 절차, 전문 상담 인력 연계를 대체하지 않습니다.",
                font, 19, FontStyles.Normal, TextMain,
                new Vector2(0f, 1f), new Vector2(48f, -470f), new Vector2(832f, -100f), TextAlignmentOptions.TopLeft);
            Button closeInfo = MenuButton(infoRect, font, "CloseInfoButton", "닫기", Teal, TealDeep, new Vector2(0.5f, 0f), new Vector2(-90f, 26f), new Vector2(90f, 74f), 20);

            controller.SetInfoPanel(infoGroup);
            UnityEventTools(startGeneral, controller.StartGeneralClassroom);
            UnityEventTools(startCircle, controller.StartCircleDiscussion);
            UnityEventTools(startRecovery, controller.StartRecoveryRoom);
            UnityEventTools(infoButton, controller.ToggleResearchInfo);
            UnityEventTools(closeInfo, controller.ToggleResearchInfo);
            UnityEventTools(quitButton, controller.QuitApplication);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterBuildScenes();
            Debug.Log("Main menu scene built at " + ScenePath);
        }

        private static void UnityEventTools(Button button, UnityEngine.Events.UnityAction action)
        {
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(button.onClick, action);
        }

        private static void RegisterBuildScenes()
        {
            var desired = new List<string>
            {
                ScenePath,
                "Assets/Scenes/KoreanClassroomTraining.unity",
                "Assets/Scenes/KoreanClassroomCircleTraining.unity",
                "Assets/Scenes/KoreanClassroomRecoveryTraining.unity"
            };
            EditorBuildSettings.scenes = desired
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        private static void EnsureHeroSprite()
        {
            var importer = AssetImporter.GetAtPath(HeroPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        private static Image FullRectImage(RectTransform parent, string name, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)imageObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text Text(
            RectTransform parent,
            string name,
            string value,
            TMP_FontAsset font,
            float size,
            FontStyles style,
            Color color,
            Vector2 anchor,
            Vector2 offsetMin,
            Vector2 offsetMax,
            TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var text = textObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static Button MenuButton(
            RectTransform parent,
            TMP_FontAsset font,
            string name,
            string label,
            Color surface,
            Color labelColor,
            Vector2 anchor,
            Vector2 offsetMin,
            Vector2 offsetMax,
            float fontSize)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            buttonObject.GetComponent<Image>().color = surface;
            TMP_Text text = Text(rect, "Label", label, font, fontSize, FontStyles.Bold, labelColor,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            RectTransform labelRect = text.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return buttonObject.GetComponent<Button>();
        }
    }
}
