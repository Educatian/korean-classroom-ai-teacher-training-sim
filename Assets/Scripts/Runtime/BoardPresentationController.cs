using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class BoardPresentationController : MonoBehaviour
    {
        private const float DisplayMaxWidth = 4.12f;
        private const float DisplayMaxHeight = 1.40f;
        private const float AutoAdvanceSeconds = 5f;

        private readonly Dictionary<int, Texture2D> pageCache = new Dictionary<int, Texture2D>();
        private readonly LinkedList<int> cacheOrder = new LinkedList<int>();
        private IPdfPresentationRenderer pdfRenderer;
        private BoardPresentationDocument document;
        private Renderer presentationSurface;
        private Material presentationMaterial;
        private TMP_Text pageLabel;
        private TMP_Text statusLabel;
        private Button previousButton;
        private Button nextButton;
        private Button autoButton;
        private bool initialized;
        private bool loadingDocument;
        private bool loadingPage;
        private bool autoAdvance;
        private float autoAdvanceElapsed;
        private int currentPage;
        private int pageRequestVersion;
        private Coroutine transition;

        public bool IsDocumentLoaded => document != null && pageCache.ContainsKey(currentPage);
        public bool IsBusy => loadingDocument || loadingPage;
        public int CurrentPageIndex => currentPage;
        public int PageCount => document?.PageCount ?? 0;
        public string DocumentTitle => document?.Title ?? string.Empty;
        public string LastError { get; private set; } = string.Empty;

        private void Awake() => Initialize();

        private void Update()
        {
            if (!initialized)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.O) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                OpenPdfPicker();
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.PageUp))
            {
                PreviousPage();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.PageDown))
            {
                NextPage();
            }
            if (Input.GetKeyDown(KeyCode.Space) && document != null)
            {
                ToggleAutoAdvance();
            }
            if (!autoAdvance || document == null || IsBusy)
            {
                return;
            }
            autoAdvanceElapsed += Time.unscaledDeltaTime;
            if (autoAdvanceElapsed >= AutoAdvanceSeconds)
            {
                autoAdvanceElapsed = 0f;
                ShowPage((currentPage + 1) % document.PageCount);
            }
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in pageCache.Values)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
            pageCache.Clear();
            if (presentationMaterial != null)
            {
                Destroy(presentationMaterial);
            }
            BoardPresentationContext.Clear();
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            pdfRenderer = new PdfiumPresentationRenderer();
            BuildPresentationSurface();
            BuildControlCanvas();
            SetStatus(pdfRenderer.IsSupported ? "PDF를 불러오세요" : pdfRenderer.UnsupportedReason);
            RefreshControls();
        }

        public void OpenPdfPicker()
        {
            if (IsBusy || !pdfRenderer.IsSupported)
            {
                return;
            }
            string path = PdfPresentationFilePicker.PickPdf();
            if (!string.IsNullOrWhiteSpace(path))
            {
                LoadPdfFromPath(path);
            }
        }

        public async void LoadPdfFromPath(string path)
        {
            Initialize();
            if (IsBusy)
            {
                return;
            }
            if (!pdfRenderer.IsSupported)
            {
                Fail(pdfRenderer.UnsupportedReason);
                return;
            }
            loadingDocument = true;
            LastError = string.Empty;
            autoAdvance = false;
            SetStatus("PDF 분석 중…");
            RefreshControls();
            try
            {
                BoardPresentationDocument loaded = await Task.Run(() => pdfRenderer.Open(path));
                if (this == null)
                {
                    return;
                }
                ClearPresentation();
                document = loaded;
                currentPage = 0;
                SetStatus($"{document.Title} · {document.PageCount}쪽");
                await ShowPageAsync(0);
                Debug.Log($"BOARD_PRESENTATION_LOADED title={document.Title} pages={document.PageCount}");
            }
            catch (Exception exception)
            {
                Fail(ToUserMessage(exception));
            }
            finally
            {
                if (this != null)
                {
                    loadingDocument = false;
                    RefreshControls();
                }
            }
        }

        public void PreviousPage()
        {
            if (document != null && !IsBusy)
            {
                ShowPage(Mathf.Max(0, currentPage - 1));
            }
        }

        public void NextPage()
        {
            if (document != null && !IsBusy)
            {
                ShowPage(Mathf.Min(document.PageCount - 1, currentPage + 1));
            }
        }

        public void ToggleAutoAdvance()
        {
            if (document == null)
            {
                return;
            }
            autoAdvance = !autoAdvance;
            autoAdvanceElapsed = 0f;
            SetStatus(autoAdvance ? $"자동 재생 · {AutoAdvanceSeconds:0}초" : $"{document.Title} · 수동 넘김");
            RefreshControls();
        }

        public void ShowPage(int pageIndex)
        {
            if (document == null || IsBusy || pageIndex < 0 || pageIndex >= document.PageCount || pageIndex == currentPage && IsDocumentLoaded)
            {
                return;
            }
            _ = ShowPageAsync(pageIndex);
        }

        private async Task ShowPageAsync(int pageIndex)
        {
            if (document == null)
            {
                return;
            }
            int requestVersion = ++pageRequestVersion;
            loadingPage = true;
            RefreshControls();
            try
            {
                if (!pageCache.TryGetValue(pageIndex, out Texture2D texture))
                {
                    SetStatus($"{pageIndex + 1}쪽 렌더링 중…");
                    BoardPresentationPage rendered = await Task.Run(() => pdfRenderer.RenderPage(document.SourcePath, pageIndex));
                    if (this == null || requestVersion != pageRequestVersion)
                    {
                        return;
                    }
                    texture = CreateTexture(rendered);
                    AddToCache(pageIndex, texture);
                }
                currentPage = pageIndex;
                Display(texture);
                BoardPresentationContext.Set(document.Title, currentPage, document.PageCount, document.PageTexts[currentPage]);
                SetStatus(autoAdvance ? $"자동 재생 · {AutoAdvanceSeconds:0}초" : document.Title);
                Debug.Log($"BOARD_PRESENTATION_PAGE page={currentPage + 1}/{document.PageCount} textChars={document.PageTexts[currentPage].Length}");
            }
            catch (Exception exception)
            {
                Fail(ToUserMessage(exception));
            }
            finally
            {
                if (this != null && requestVersion == pageRequestVersion)
                {
                    loadingPage = false;
                    RefreshControls();
                }
            }
        }

        private void BuildPresentationSurface()
        {
            GameObject surfaceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surfaceObject.name = "PdfPresentationSurface";
            surfaceObject.transform.SetParent(transform, false);
            surfaceObject.transform.localPosition = new Vector3(0f, 1.65f, -0.255f);
            surfaceObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            surfaceObject.transform.localScale = new Vector3(DisplayMaxWidth, DisplayMaxHeight, 0.012f);
            Collider collider = surfaceObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            presentationSurface = surfaceObject.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            presentationMaterial = new Material(shader) { name = "M_PdfPresentation_Runtime" };
            presentationSurface.sharedMaterial = presentationMaterial;
            surfaceObject.SetActive(false);
        }

        private void BuildControlCanvas()
        {
            TMP_FontAsset font = FindUiFont();
            GameObject canvasObject = new GameObject(
                "BoardPresentationControls",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            RectTransform canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.localPosition = new Vector3(0f, 0.74f, -0.49f);
            canvasRect.localRotation = Quaternion.identity;
            float controlScale = 0.00215f;
            float parentVerticalScale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
            canvasRect.localScale = new Vector3(controlScale, controlScale / parentVerticalScale, controlScale);
            canvasRect.sizeDelta = new Vector2(1500f, 118f);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            canvas.sortingOrder = 30;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2f;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
            RectTransform backgroundRect = (RectTransform)background.transform;
            backgroundRect.SetParent(canvasRect, false);
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.025f, 0.04f, 0.055f, 0.94f);
            HorizontalLayoutGroup layout = background.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 16, 16);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            CreateButton(background.transform, "PdfImportButton", "PDF 불러오기", 210f, OpenPdfPicker, font);
            previousButton = CreateButton(background.transform, "PdfPreviousButton", "이전", 105f, PreviousPage, font);
            pageLabel = CreateLabel(background.transform, "PdfPageLabel", "- / -", 125f, 29f, TextAlignmentOptions.Center, font);
            nextButton = CreateButton(background.transform, "PdfNextButton", "다음", 105f, NextPage, font);
            autoButton = CreateButton(background.transform, "PdfAutoButton", "자동 5초", 155f, ToggleAutoAdvance, font);
            statusLabel = CreateLabel(background.transform, "PdfStatusLabel", string.Empty, 650f, 25f, TextAlignmentOptions.MidlineLeft, font);
        }

        private Button CreateButton(Transform parent, string name, string label, float width, UnityEngine.Events.UnityAction clicked, TMP_FontAsset font)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(ButtonMotion));
            buttonObject.transform.SetParent(parent, false);
            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.48f, 0.48f, 0.96f);
            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.74f, 0.9f, 0.9f, 1f);
            colors.disabledColor = new Color(0.35f, 0.38f, 0.4f, 0.65f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(clicked);
            CreateStretchText(buttonObject.transform, label, 27f, TextAlignmentOptions.Center, font);
            return button;
        }

        private TMP_Text CreateLabel(Transform parent, string name, string value, float width, float fontSize, TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            LayoutElement layout = labelObject.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.text = value;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.93f, 0.97f, 0.98f, 1f);
            label.enableWordWrapping = false;
            if (font != null)
            {
                label.font = font;
            }
            return label;
        }

        private void CreateStretchText(Transform parent, string value, float fontSize, TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 4f);
            rect.offsetMax = new Vector2(-8f, -4f);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.enableWordWrapping = false;
            if (font != null)
            {
                text.font = font;
            }
        }

        private static TMP_FontAsset FindUiFont()
        {
            TMP_Text[] labels = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (TMP_Text label in labels)
            {
                if (label.font != null && label.font.name.Contains("Noto"))
                {
                    return label.font;
                }
            }
            return labels.Length > 0 ? labels[0].font : null;
        }

        private Texture2D CreateTexture(BoardPresentationPage page)
        {
            Texture2D texture = new Texture2D(page.Width, page.Height, TextureFormat.BGRA32, false, false)
            {
                name = $"PdfPage_{document.Title}_{currentPage + 1}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
            texture.LoadRawTextureData(page.Bgra32);
            texture.Apply(false, false);
            return texture;
        }

        private void Display(Texture2D texture)
        {
            float scaleX = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
            float scaleY = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y));
            float localAspect = texture.width / (float)texture.height * scaleY / scaleX;
            float width = Mathf.Min(DisplayMaxWidth, DisplayMaxHeight * localAspect);
            float height = width / localAspect;
            presentationSurface.transform.localScale = new Vector3(width, height, 0.012f);
            presentationSurface.gameObject.SetActive(true);
            HideTemplateContent();
            if (transition != null)
            {
                StopCoroutine(transition);
            }
            transition = StartCoroutine(TransitionTo(texture));
        }

        private IEnumerator TransitionTo(Texture2D texture)
        {
            float elapsed = 0f;
            while (elapsed < 0.10f)
            {
                elapsed += Time.unscaledDeltaTime;
                presentationMaterial.color = Color.Lerp(Color.white, new Color(0.10f, 0.12f, 0.14f, 1f), elapsed / 0.10f);
                yield return null;
            }
            presentationMaterial.mainTexture = texture;
            presentationMaterial.mainTextureScale = new Vector2(1f, -1f);
            presentationMaterial.mainTextureOffset = new Vector2(0f, 1f);
            elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.unscaledDeltaTime;
                presentationMaterial.color = Color.Lerp(new Color(0.10f, 0.12f, 0.14f, 1f), Color.white, Mathf.SmoothStep(0f, 1f, elapsed / 0.18f));
                yield return null;
            }
            presentationMaterial.color = Color.white;
        }

        private void HideTemplateContent()
        {
            string[] names = { "Header", "CardA", "CardB", "CardC", "Footer" };
            foreach (string targetName in names)
            {
                Transform target = FindDescendant(transform, targetName);
                if (target != null)
                {
                    target.gameObject.SetActive(false);
                }
            }
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == targetName)
                {
                    return candidate;
                }
            }
            return null;
        }

        private void AddToCache(int pageIndex, Texture2D texture)
        {
            pageCache[pageIndex] = texture;
            cacheOrder.Remove(pageIndex);
            cacheOrder.AddLast(pageIndex);
            while (cacheOrder.Count > BoardPresentationPolicy.MaxCachedPages)
            {
                int removeIndex = cacheOrder.First.Value;
                cacheOrder.RemoveFirst();
                if (pageCache.Remove(removeIndex, out Texture2D removed) && removed != null)
                {
                    Destroy(removed);
                }
            }
        }

        private void ClearPresentation()
        {
            pageRequestVersion++;
            foreach (Texture2D texture in pageCache.Values)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
            pageCache.Clear();
            cacheOrder.Clear();
            document = null;
            currentPage = 0;
            BoardPresentationContext.Clear();
        }

        private void RefreshControls()
        {
            bool hasDocument = document != null;
            bool canInteract = hasDocument && !IsBusy;
            if (previousButton != null)
            {
                previousButton.interactable = canInteract && currentPage > 0;
            }
            if (nextButton != null)
            {
                nextButton.interactable = canInteract && currentPage + 1 < document.PageCount;
            }
            if (autoButton != null)
            {
                autoButton.interactable = canInteract;
                TMP_Text label = autoButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = autoAdvance ? "자동 정지" : "자동 5초";
                }
            }
            if (pageLabel != null)
            {
                pageLabel.text = hasDocument ? $"{currentPage + 1} / {document.PageCount}" : "- / -";
            }
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message ?? string.Empty;
            }
        }

        private void Fail(string message)
        {
            LastError = string.IsNullOrWhiteSpace(message) ? "PDF를 불러오지 못했습니다." : message;
            SetStatus(LastError);
            Debug.LogError($"BOARD_PRESENTATION_FAILED {LastError}");
        }

        private static string ToUserMessage(Exception exception)
        {
            if (exception is DllNotFoundException)
            {
                return "PDF 렌더링 모듈을 불러오지 못했습니다.";
            }
            if (exception is InvalidDataException || exception is FileNotFoundException || exception is PlatformNotSupportedException)
            {
                return exception.Message;
            }
            return "PDF를 분석하는 중 오류가 발생했습니다.";
        }
    }
}