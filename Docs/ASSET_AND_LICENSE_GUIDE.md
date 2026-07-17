# Asset and license guide

## Microsoft Rocketbox

Student avatar source files under `Assets/ThirdParty/MicrosoftRocketbox` are distributed under the Microsoft MIT license included at `Assets/ThirdParty/MicrosoftRocketbox/LICENSE.md`. The copyright notice and permission text must remain with redistributed copies or substantial portions.

The project modifies appearance at material and shader level. Rocketbox geometry and its original texture atlases remain identifiable third-party source assets.

## Generated student assets

Textures under `Assets/Generated/StudentFaces` and `Assets/Generated/StudentClothing` were generated for this prototype. The clothing set includes eight neutral fabric surfaces and a text-free 15-motif chest-graphic atlas. Generation intent, prompts, Unity import settings, and assignment rules are documented in `Assets/Generated/StudentClothing/README.md` and `Docs/IMAGEGEN_ASSET_PIPELINE.md`.

Do not replace the generated atlas with real school emblems, commercial apparel marks, student photographs, or recognizable copyrighted characters.

## Classroom reference media

`Assets/Reference` contains selected development screenshots and generated storyboard material. Local attachment directories, raw user photographs, diagnostic crops, duplicated QA projects, and recording intermediates are excluded through `.gitignore`.

## Fonts and Unity packages

`Assets/Art/Fonts/NotoSansKR-VF.ttf` is distributed under the SIL Open Font License 1.1. Its copyright notice and complete license text are preserved at `Assets/Art/Fonts/OFL.txt`, sourced from the official Google Fonts Noto Sans KR package. TextMesh Pro resources are included as Unity project assets. Unity package dependencies are declared in `Packages/manifest.json` and remain subject to their upstream Unity terms.

## Adding an asset

Before committing a new binary asset:

1. Record its source, author, and license.
2. Confirm redistribution is allowed for a private repository and any planned release.
3. Keep the Unity `.meta` file with the asset.
4. Avoid files larger than GitHub's per-file limit.
5. Do not commit credentials, student personally identifiable information, or raw classroom photographs without documented authorization.
