# Electronic-board PDF presentations

The classroom electronic board can display a teacher-selected PDF as an in-world slideshow in the Windows player. The current page text is also supplied as bounded context to the student-response and teacher-rubric LLM prompts.

## Use

1. Start either classroom scene in the Windows player.
2. Select **PDF 불러오기** on the electronic board, or press `Ctrl+O`.
3. Choose a local `.pdf` file.
4. Use **이전** / **다음**, `Page Up` / `Page Down`, or the arrow keys to change pages.
5. Select **자동 5초** or press `Space` to toggle timed playback.

The board keeps the document aspect ratio, fades between pages, shows the current page count, and uses the existing subtle button-motion and click-audio treatment. The world-space controls support mouse input and XR ray input.

## Runtime limits and privacy

- PDF only, 50 MB maximum, 80 pages maximum.
- Pages are rendered on demand at no more than 2048 pixels per axis.
- At most five rendered pages are cached; the entire rasterized document is never retained in memory.
- Extracted text is whitespace-normalized and limited to 2,400 characters for the active page before it enters an LLM prompt.
- The source PDF remains local. The LLM receives only the bounded active-page text, document title, and page number.
- The bundled sample is `Assets/StreamingAssets/BoardPresentationDemo.pdf`.

## Platform boundary

Windows uses the bundled Docnet.Core 2.6.0 managed assembly and PDFium native runtime. Their license notices are under `Assets/ThirdPartyNotices`. Both plugins are explicitly disabled for Android, Linux, macOS, WebGL, and 32-bit Windows.

The Meta Quest build remains valid, but direct on-headset PDF rendering and file selection are not yet implemented. A production Quest implementation should use Android Storage Access Framework for user selection and either an ARM64-compatible audited renderer or the existing secure application proxy to rasterize pages. Provider or proxy credentials must never be embedded in the APK.

## Evidence and verification

- EditMode regression: `95/95` passed, including real opening, text extraction, and rendering of the bundled three-page PDF.
- Windows build: `WINDOWS_BUILD_OK bytes=331207394`.
- Meta Quest build: `META_QUEST_BUILD_OK bytes=941446479`; the Windows PDF plugins were excluded from the APK.
- Player evidence markers: `BOARD_PRESENTATION_LOADED`, `BOARD_PRESENTATION_PAGE`, and `BOARD_PRESENTATION_EVIDENCE_OK`.
- Captures: `Assets/Reference/Unity_PdfPresentation_Page01.png` and `Assets/Reference/Unity_PdfPresentation_Page02.png`.

The implementation is based on [Docnet.Core](https://github.com/GowenGit/docnet), distributed under the MIT license.
