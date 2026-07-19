using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace AdieLab.TeacherTraining.Tests
{
    public sealed class BoardPresentationTests
    {
        [TearDown]
        public void TearDown() => BoardPresentationContext.Clear();

        [Test]
        public void NormalizePageText_CollapsesWhitespaceAndBoundsPromptContext()
        {
            string input = "  감정\n\t온도계  " + new string('가', BoardPresentationPolicy.MaxLlmContextCharacters + 100);
            string normalized = BoardPresentationPolicy.NormalizePageText(input);
            Assert.That(normalized, Does.StartWith("감정 온도계 "));
            Assert.That(normalized.Length, Is.EqualTo(BoardPresentationPolicy.MaxLlmContextCharacters));
        }

        [Test]
        public void FitAspect_PreservesLandscapeAndPortraitRatios()
        {
            (float landscapeWidth, float landscapeHeight) = BoardPresentationPolicy.FitAspect(1920, 1080, 4f, 2f);
            Assert.That(landscapeWidth / landscapeHeight, Is.EqualTo(16f / 9f).Within(0.001f));
            Assert.That(landscapeHeight, Is.EqualTo(2f).Within(0.001f));

            (float portraitWidth, float portraitHeight) = BoardPresentationPolicy.FitAspect(1080, 1920, 4f, 2f);
            Assert.That(portraitWidth / portraitHeight, Is.EqualTo(9f / 16f).Within(0.001f));
            Assert.That(portraitHeight, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void BoardContext_ExposesOnlyCurrentBoundedSlide()
        {
            BoardPresentationContext.Set("공동조절", 1, 3, "선택권을 제공합니다.");
            string context = BoardPresentationContext.BuildLlmContext();
            Assert.That(context, Does.Contain("공동조절"));
            Assert.That(context, Does.Contain("2/3쪽"));
            Assert.That(context, Does.Contain("선택권을 제공합니다."));
        }

#if UNITY_EDITOR_WIN
        [Test]
        public void PdfiumRenderer_OpensAndRendersBundledThreePageDemo()
        {
            string path = Path.Combine(Application.dataPath, "StreamingAssets", "BoardPresentationDemo.pdf");
            PdfiumPresentationRenderer renderer = new PdfiumPresentationRenderer();
            Assert.That(renderer.IsSupported, Is.True);
            BoardPresentationDocument document = renderer.Open(path);
            Assert.That(document.PageCount, Is.EqualTo(3));
            Assert.That(document.PageTexts[0], Does.Contain("감정"));

            BoardPresentationPage page = renderer.RenderPage(path, 0);
            Assert.That(page.Width, Is.GreaterThan(1000));
            Assert.That(page.Height, Is.GreaterThan(500));
            Assert.That(page.Bgra32.Length, Is.EqualTo(page.Width * page.Height * 4));
        }
#endif
    }
}