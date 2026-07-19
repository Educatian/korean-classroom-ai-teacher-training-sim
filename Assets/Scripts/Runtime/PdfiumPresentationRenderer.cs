using System;
using System.Collections.Generic;
using System.IO;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using Docnet.Core;
using Docnet.Core.Models;
#endif

namespace AdieLab.TeacherTraining
{
    public sealed class PdfiumPresentationRenderer : IPdfPresentationRenderer
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        public bool IsSupported => true;
        public string UnsupportedReason => string.Empty;

        public BoardPresentationDocument Open(string pdfPath)
        {
            BoardPresentationPolicy.ValidatePdfPath(pdfPath);
            using var reader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(1.0));
            int pageCount = reader.GetPageCount();
            if (pageCount <= 0 || pageCount > BoardPresentationPolicy.MaxPages)
            {
                throw new InvalidDataException($"PDF must contain 1-{BoardPresentationPolicy.MaxPages} pages.");
            }
            List<string> pageTexts = new List<string>(pageCount);
            for (int index = 0; index < pageCount; index++)
            {
                using var pageReader = reader.GetPageReader(index);
                pageTexts.Add(BoardPresentationPolicy.NormalizePageText(pageReader.GetText()));
            }
            return new BoardPresentationDocument(pdfPath, Path.GetFileNameWithoutExtension(pdfPath), pageTexts);
        }

        public BoardPresentationPage RenderPage(string pdfPath, int pageIndex)
        {
            BoardPresentationPolicy.ValidatePdfPath(pdfPath);
            using var reader = DocLib.Instance.GetDocReader(
                pdfPath,
                new PageDimensions(BoardPresentationPolicy.MaxRenderDimension, BoardPresentationPolicy.MaxRenderDimension));
            int pageCount = reader.GetPageCount();
            if (pageIndex < 0 || pageIndex >= pageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex));
            }
            using var pageReader = reader.GetPageReader(pageIndex);
            byte[] pixels = pageReader.GetImage(RenderFlags.RenderAnnotations | RenderFlags.OptimizeTextForLcd);
            return new BoardPresentationPage(pageReader.GetPageWidth(), pageReader.GetPageHeight(), pixels);
        }
#else
        public bool IsSupported => false;
        public string UnsupportedReason => "이 빌드에서는 PDF 렌더링 프록시가 필요합니다.";

        public BoardPresentationDocument Open(string pdfPath)
        {
            throw new PlatformNotSupportedException(UnsupportedReason);
        }

        public BoardPresentationPage RenderPage(string pdfPath, int pageIndex)
        {
            throw new PlatformNotSupportedException(UnsupportedReason);
        }
#endif
    }
}