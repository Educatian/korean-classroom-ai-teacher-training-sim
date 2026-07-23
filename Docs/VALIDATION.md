# Validation record

## Student TTS, ECD, and research dashboard (2026-07-19)

- The full EditMode suite passed 100/100 with zero failures or skips.
- Final Windows player build completed successfully at 331238930 bytes.
- Continuous Windows autoplay completed direct dialogue, upright eye contact, six authored intervention beats, and the research debrief capture without dashboard exceptions.
- The student speech path plans rate, pitch, volume, comma pauses, and sentence pauses from affect. Windows and Quest prioritize the authenticated OpenRouter proxy and retain Windows system-speech and pause-aware lip-sync fallbacks.
- AU25 and AU26 are driven from the played audio amplitude. Telemetry schema 2 records the provider route and prosody plan without raw utterance text.
- Live OpenRouter audio QA generated a 196,372-byte Korean student RIFF/WAVE through `x-ai/grok-voice-tts-1.0` and transcribed it back exactly through `openai/gpt-4o-mini-transcribe` using the same Worker key.
- The researcher-editable ECD ScriptableObject maps six competencies to observable behaviors, evidence identifiers, weights, expected scores, score bands, and missed-signal prompts.
- The dashboard renders valence/arousal trajectories, intervention history, competency evidence counts, missed signals, retry, and anonymized JSON/CSV export.
- The first real-player pass exposed a Unity Graphic component conflict on the affect chart. The graph was moved to a dedicated child renderer, rebuilt, and the repeated autoplay passed. The resulting capture is Assets/Reference/ResearchDebriefDashboard.png.

## Full dialogue, performance, and scenario regression (2026-07-19)

- The free-dialogue request now carries authored scenario context, crisis stage, and persona identity. Conversation state keeps a bounded recent-turn window plus four durable teacher commitments so promises survive short-term history eviction.
- Student turns are normalized as one performance unit: emotional valence/arousal/dominance, dialogue signals, gesture, and facial action units remain coherent. Strong negative arousal suppresses smiling and adds protest tension; felt-heard and re-entry signals select recovery gestures and relaxed facial movement.
- Dynamic scenario transitions now preserve instructional pacing. Immediate safety escalation remains available, while de-escalation and re-entry jumps require relational momentum; a hold decision no longer pins the current beat and block normal sequential continuation.
- LLM teacher-rubric results replace provisional scoring evidence, retain model provenance in telemetry, and refresh the debrief when an evaluation arrives after scenario completion.
- The full EditMode suite passed `91/91` with zero failures, skips, or inconclusive tests. The classroom PlayMode flow passed all six beats and all four modes with six persisted records.
- NPC PlayMode QA passed for 14 classmates: all tracked teacher movement, gesture and idle pools each exposed seven variants, yawn drove AU26, chin-rest measured `handGap=0.008 m` and `elbowGap=0.010 m`, and button/footstep audio played. Windows batch-mode capture is skipped to avoid a Unity 6 native D3D11 `Camera.Render` crash; interactive-editor visual capture remains enabled.
- Both ScriptableObject scenes passed the scenario surface gate with six beats and three response choices. The circle-discussion asset includes audience pressure, presentation avoidance, relational reconnection, and instructional re-entry.
- The XR rig gate passed with two controller rays, world-space HUD conversion, and clean desktop restoration. The Windows player built successfully at `327082700` bytes. The Quest Android ARM64/Vulkan build produced an APK of `123680411` bytes and logged `META_QUEST_BUILD_OK bytes=937435283`.
- OpenXR `1.16.1` still emits a non-fatal Meta Quest validation-hook `NullReferenceException`, and Gradle warm-up emits a non-fatal initialization diagnostic before the real `assembleRelease` succeeds. Physical-headset controller, microphone, comfort, readability, and sustained frame-rate acceptance remain outstanding.

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
- The Quest Android Vulkan release build logged `META_QUEST_BUILD_OK` and produced `Builds/TeacherResponseTrainingQuest/TeacherResponseTrainingQuest.apk` (`123656631` bytes on disk). No XR Simulation asset-move collision or Gamma/OpenGLES validation error was observed in that run.
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
## 2026-07-19 visual, voice, and live-dialogue validation

- EditMode: `91/91` passed (`Logs/visual-upgrade-editmode.xml`).
- Windows build: passed (`WINDOWS_BUILD_OK`, `325950338` bytes).
- Windows autoplay: captured `ClassroomReference.png`, `StudentEyeContact.png`, and `TrainingDebrief.png` at 1920x1080.
- Live OpenRouter evidence: three consecutive turns passed with `openai/gpt-4o-mini`; log markers `LIVE_LLM_DIALOGUE_TURN_OK` x3 and `LIVE_LLM_DIALOGUE_EVIDENCE_OK`.
- Visual inspection: speech bubble remained above the focal student's head, HUD text was readable, and the new floor/desk/wall surfaces and Blender-authored air purifier appeared at teacher eye height.
- Known tooling issue: the headless editor-only preview camera crashed in native shadow rendering after writing no new preview; Windows-player captures succeeded and are the authoritative evidence for this pass.

## Electronic-board object replacement (2026-07-19)

- Replaced the legacy primitive `ElectronicBoardAssembly` in both training scenes with the Blender-authored `ElectronicBoardAssembly_Blender` model.
- The replacement validates 13 authored OBJ parts before saving: frame, inner bezel, active screen, tray, camera ring/lens, input panel, four ports, and both speaker arrays.
- A visible hardware pass adds a brushed-metal double bezel, active teal display surface, camera housing/lens/status light, accessory tray and stylus, four differentiated input ports, and perforated left/right speaker grilles.
- The final Windows player evidence passed with `ELECTRONIC_BOARD_EVIDENCE_OK renderers=43 meshes=43 vertices=26491` and produced `Assets/Reference/Unity_ElectronicBoard_Applied.png` plus `Assets/Reference/Unity_ElectronicBoard_Detail.png`.
- The final EditMode regression suite passed `91/91`. The Windows build succeeded with `WINDOWS_BUILD_OK bytes=326098822`.
- Desktop validation emitted expected non-fatal OpenXR runtime discovery warnings because no headset runtime was active; rendering and evidence capture completed successfully.
## Electronic-board PDF presentation (2026-07-19)

- Added user-selected local PDF import, previous/next navigation, five-second autoplay, keyboard shortcuts, world-space desktop/XR controls, page transitions, and aspect-preserving display on the Blender-authored electronic board.
- Current-page text is normalized and bounded before it is appended to the student dialogue and teacher-rubric LLM prompts. The PDF raster remains local.
- The Windows-only Docnet.Core/PDFium plugins are disabled for Android and other unsupported targets, preserving the Meta Quest build boundary.
- The full EditMode suite passed `95/95`, including a real three-page PDF open, text extraction, and raster-render test.
- The final Windows build emitted `WINDOWS_BUILD_OK bytes=331207394`.
- The final Meta Quest build emitted `META_QUEST_BUILD_OK bytes=941446479`; the APK is 124,082,840 bytes on disk and excludes the Windows-only PDF plugins.
- The built player loaded the sample, rendered pages 1 and 2, and emitted `BOARD_PRESENTATION_EVIDENCE_OK title=BoardPresentationDemo pages=3 current=2`.
- Visual review confirmed upright, non-mirrored Korean text, correct 16:9 fitting, visible page controls, and successful page transition in `Unity_PdfPresentation_Page01.png` and `Unity_PdfPresentation_Page02.png`.
# Adaptive learning support and left-edge HUD dock (2026-07-22)

- Full EditMode regression suite passed: `134/134`.
- The learning-support PlayMode gate opened the authored classroom scene, dismissed the prebrief, toggled the observation window from the left dock, verified the notification window, restored the panel, and opened an observation cue.
- All three generated HUD icons loaded as runtime sprites from separate transparent PNG assets.
- Runtime checks confirmed that the dock remains inside and within 20 pixels of the left canvas edge; the learning-support panel remains inside the canvas and does not overlap the response-choice panel.
- Heading, body, source, option, and notification labels generated visible TextMeshPro glyph meshes without overflow.
- Evidence capture: `Assets/Reference/Unity_Adaptive_Learning_Support.png` at 3200×1800.
