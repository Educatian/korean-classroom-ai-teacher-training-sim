using System;
using System.Collections.Generic;
using System.IO;

namespace AdieLab.TeacherTraining
{
    public sealed class BoardPresentationPage
    {
        public BoardPresentationPage(int width, int height, byte[] bgra32)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }
            Width = width;
            Height = height;
            Bgra32 = bgra32 ?? throw new ArgumentNullException(nameof(bgra32));
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] Bgra32 { get; }
    }

    public sealed class BoardPresentationDocument
    {
        public BoardPresentationDocument(string sourcePath, string title, IReadOnlyList<string> pageTexts)
        {
            SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
            Title = string.IsNullOrWhiteSpace(title) ? "Untitled PDF" : title.Trim();
            PageTexts = pageTexts ?? throw new ArgumentNullException(nameof(pageTexts));
            if (PageTexts.Count == 0)
            {
                throw new ArgumentException("A presentation requires at least one page.", nameof(pageTexts));
            }
        }

        public string SourcePath { get; }
        public string Title { get; }
        public IReadOnlyList<string> PageTexts { get; }
        public int PageCount => PageTexts.Count;
    }

    public interface IPdfPresentationRenderer
    {
        bool IsSupported { get; }
        string UnsupportedReason { get; }
        BoardPresentationDocument Open(string pdfPath);
        BoardPresentationPage RenderPage(string pdfPath, int pageIndex);
    }

    public static class BoardPresentationPolicy
    {
        public const long MaxPdfBytes = 50L * 1024L * 1024L;
        public const int MaxPages = 80;
        public const int MaxRenderDimension = 2048;
        public const int MaxCachedPages = 5;
        public const int MaxLlmContextCharacters = 2400;

        public static void ValidatePdfPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("PDF path is empty.", nameof(path));
            }
            if (!string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Only PDF files are supported.");
            }
            FileInfo file = new FileInfo(path);
            if (!file.Exists)
            {
                throw new FileNotFoundException("The selected PDF does not exist.", path);
            }
            if (file.Length <= 0 || file.Length > MaxPdfBytes)
            {
                throw new InvalidDataException($"PDF size must be between 1 byte and {MaxPdfBytes / (1024 * 1024)} MB.");
            }
        }

        public static string NormalizePageText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            string normalized = string.Join(" ", value
                .Replace('\0', ' ')
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            return normalized.Length <= MaxLlmContextCharacters
                ? normalized
                : normalized.Substring(0, MaxLlmContextCharacters);
        }

        public static (float width, float height) FitAspect(int sourceWidth, int sourceHeight, float maxWidth, float maxHeight)
        {
            if (sourceWidth <= 0 || sourceHeight <= 0 || maxWidth <= 0f || maxHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceWidth), "Source and target dimensions must be positive.");
            }
            float aspect = sourceWidth / (float)sourceHeight;
            float width = Math.Min(maxWidth, maxHeight * aspect);
            float height = width / aspect;
            return (width, height);
        }
    }

    public static class BoardPresentationContext
    {
        public static string DocumentTitle { get; private set; } = string.Empty;
        public static int PageNumber { get; private set; }
        public static int PageCount { get; private set; }
        public static string PageText { get; private set; } = string.Empty;

        public static void Set(string documentTitle, int zeroBasedPage, int pageCount, string pageText)
        {
            DocumentTitle = documentTitle ?? string.Empty;
            PageNumber = Math.Max(0, zeroBasedPage) + 1;
            PageCount = Math.Max(0, pageCount);
            PageText = BoardPresentationPolicy.NormalizePageText(pageText);
        }

        public static string BuildLlmContext()
        {
            if (PageCount == 0)
            {
                return string.Empty;
            }
            string text = string.IsNullOrWhiteSpace(PageText) ? "텍스트 추출 없음" : PageText;
            return $"현재 전자칠판 자료: {DocumentTitle}, {PageNumber}/{PageCount}쪽\n현재 페이지 내용: {text}";
        }

        public static void Clear()
        {
            DocumentTitle = string.Empty;
            PageNumber = 0;
            PageCount = 0;
            PageText = string.Empty;
        }
    }
}