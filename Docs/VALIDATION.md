# Validation record

## Bounded-generative dialogue and secure proxy validation (2026-07-19)

- The full EditMode suite passed `85/85` with no failures, skips, or inconclusive tests. New coverage verifies bounded conversation state, strict student and teacher-rubric JSON contracts, non-finite score rejection, deterministic crisis-stage transitions, exact coordinator jumps, Android secure-proxy selection, direct-provider endpoint rejection, and provider-key-free proxy envelopes.
- Real classroom PlayMode QA completed the free-utterance interaction, local student response, face-to-face camera, visible speech bubble, upright eye contact, response buttons, all six beats, six persisted records, and debrief. The log emitted `PLAYMODE_QA_OK` and `PLAYMODE_FLOW_OK`.
- The final Windows build emitted `WINDOWS_BUILD_OK bytes=327078876`.
- The final Android/Meta Quest build emitted `META_QUEST_BUILD_OK bytes=937314984`; the APK is `123675967` bytes on disk.
- No provider key is serialized by the Quest proxy envelope or settings contract. Live Quest LLM acceptance still requires a deployed HTTPS relay, short-lived session issuance, and physical-headset QA.

## Current automated baseline

On 2026-07-18, both classroom scenes were regenerated with Unity `6000.4.9f1` and the expanded EditMode suite passed `71/71` tests with zero failures, skips, or inconclusive results. The Windows player build completed successfully at 318,275,046 bytes.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath '<repository-root>' `
  -runTests -testPlatform editmode `
  -testResults '<repository-root>\Logs\editmode-results.xml' `
  -logFile '<repository-root>\Logs\editmode.log'
```

The local logs are excluded from Git. A passing result must be regenerated on each development machine or continuous-integration runner.

## ScriptableObject scenario migration (2026-07-18)

- Migrated five reusable student personas and both six-beat training scenarios to assets under `Assets/Resources/Training`.
- Each authored beat now carries an explicit trigger, crisis stage, student persona, scenario type, teacher competency goals, and safety/context flags.
- Added EditMode coverage for catalog completeness, all 12 authored beats, deep-copy runtime conversion, and the Quest secure-proxy deployment boundary.
- A dedicated PlayMode gate loaded `KoreanClassroomTraining` and `KoreanClassroomCircleTraining` in sequence. Both scenes logged six runtime beats, displayed situation `1/6`, and presented all three response choices from the ScriptableObject source.
- The final player build log confirmed the persona and scenario assets were included in Resources and emitted `WINDOWS_BUILD_OK bytes=318275046`.

Meta Quest is now an implemented build target. `Docs/META_QUEST_READINESS.md` records the installed XR stack, runtime desktop/IVR mode boundary, rig/input/UI behavior, build command, secure OpenRouter proxy requirement, and remaining physical-headset acceptance work.

## Meta Quest / IVR implementation (2026-07-19)

- Installed XR Interaction Toolkit `3.0.9`, XR Plug-in Management `4.5.3`, OpenXR `1.16.1`, and Meta OpenXR `2.4.0`; configured Standalone and Android OpenXR loaders with runtime-controlled initialization.
- Added the top-bar `DESKTOP` / `IVR` mode control. IVR creates a floor-origin tracked-head rig, left/right controller rays, XR UI input, and a world-space HUD. Returning to desktop disposes XR actions/components and restores camera, input, audio, and canvas state.
- The XR rig PlayMode surface logged `XR_RIG_STRUCTURE_QA_OK controllers=2 hud=world-space cleanup=desktop-restored`.
- The full EditMode regression suite passed `74/74` with zero failures or skips.
- The Windows player build succeeded at `327117816` bytes.
- The Quest Android Vulkan release build logged `META_QUEST_BUILD_OK` and produced `Builds/TeacherResponseTrainingQuest/TeacherResponseTrainingQuest.apk` (`123656631` bytes on disk). The final log contains no OpenXR build error, XR Simulation asset-move collision, or Gamma/OpenGLES validation error.
- Physical Quest controller, comfort, visual-angle readability, microphone, and sustained performance testing remain required; no claim of headset acceptance is made.

## Manual QA completed

- Both classroom layouts render with 15 students and direct teacher-facing conversation framing.
- A 30-render face roster covering both scenes was independently reviewed with no duplicate eyes, noses, lips, or broken face meshes.
- The randomized outfit roster shows 15 distinct color, pattern, fabric, and chest-graphic assignments, and the original Rocketbox shirt marks are suppressed.
- Speech bubbles remain anchored above the focal student's head and the dialogue HUD remains readable at 1920×1080.

## Completed visual-polish pass

- Raised the female Rocketbox chest-graphic origin from 0.430 to 0.455 while preserving the male origin.
- Replaced the top-left letter-like apparel mark with a rounded three-pebble orbit generated through imagegen.
- Added a skin-protection mask so torso fallback tinting does not recolor skin-like hand UV islands.

## Final manual visual sign-off (2026-07-18)

- `Unity_VisualPolish_FemaleYawn_FullBody.png` confirms the raised female chest graphic remains on the shirt and both hands retain their skin material during the yawn pose.
- `Unity_VisualPolish_ChinRest_FullBody.png` confirms the full seated silhouette remains readable during the chin-rest/thoughtful pose, without clothing, desk, or chair penetration.
- `Unity_VisualPolish_ExtremePushAway_FullBody.png` confirms both open hands remain skin-toned during the strong two-arm gesture and neither sleeve nor torso is masked by the chair.
- The rounded three-pebble orbit motif remains visibly abstract and non-letter-like in both the close pose captures and normal-distance classroom captures.

The final PlayMode run logged `CLASSMATE_GAZE_QA_OK`, `NPC_IDLE_BEHAVIOR_QA_OK`, and `CLASSROOM_AUDIO_QA_OK`. Automated scene contracts, the `67/67` EditMode suite, player build, MP4 decode, timeline contact-sheet generation, and the required human visual checks are complete.

## Chin-rest desk-contact correction (2026-07-18)

- Added a runtime humanoid IK contact controller that resolves the nearest student desktop, blends the supporting hand to the chin, keeps the supporting elbow inside the usable desktop inset, and places the free hand separately on the desk.
- The fresh PlayMode gate measured `handGap=0.008 m`, `elbowGap=0.010 m`, and desktop-local elbow position `(0.18, 0.04, -0.26)`. It also revalidated 14/14 teacher-facing classmates, seven gesture/idle behavior variants, button audio, and movement-bound footsteps.
- `Unity_VisualPolish_ChinRest_FullBody.png` is the fresh full-body visual artifact. The pose shows a supported chin, an elbow over the tabletop rather than outside its edge, a separated free arm, and no visible desk/chair/clothing penetration.
- The final EditMode regression suite passed `71/71` with no compile errors.

## Release checklist

1. Regenerate both scenes from the editor builder.
2. Run all EditMode tests.
3. Build the Windows player and confirm the Unity build result is `Succeeded`.
4. Exercise response, dialogue, observation, and debrief modes in the built player.
5. Inspect focal and classmate faces, hands, clothing, desk collision, and chair masking.
6. Decode any delivered MP4 from start to finish and inspect a timeline contact sheet.
7. Scan tracked files for secrets and files above GitHub's size limit.
