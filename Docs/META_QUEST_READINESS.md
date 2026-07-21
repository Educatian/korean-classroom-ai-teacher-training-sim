# Meta Quest / IVR implementation

## Current status

The same two classroom scenarios now support a desktop/IVR experience-mode boundary. Windows and Android share the scenario catalog, simulation controller, NPC performance, facial action-unit system, HUD content, and OpenRouter policy. Platform-specific camera, input, and UI presentation are selected at runtime.

The top navigation contains a `DESKTOP` / `IVR` toggle. Desktop mode preserves the existing teacher camera, mouse input, screen-space HUD, and footstep behavior. IVR mode initializes OpenXR, creates an XR Origin with tracked head and left/right controller rays, disables desktop locomotion, and moves the existing HUD into world space. If no headset/runtime is available, the application remains in desktop mode and displays `DESKTOP · IVR 미연결`.

Android builds default to IVR. Windows and the Unity Editor default to desktop, so researchers can continue authoring and testing without a connected headset.

## Installed XR stack

- XR Interaction Toolkit `3.0.9`
- XR Plug-in Management `4.5.3`
- Unity OpenXR `1.16.1`
- Unity OpenXR: Meta `2.4.0`
- Input System `1.19.0` (Both input backends enabled for desktop compatibility)

OpenXR loaders are assigned to Standalone and Android, but automatic startup is disabled so the mode controller owns initialization. Android uses ARM64 with Vulkan-only rendering and enables KHR Simple Controller, Oculus Touch, Meta Quest Touch Plus/Pro, Meta Quest Support, and OpenXR Composition Layers.

## Runtime structure

```text
Shared training scene
├── Desktop mode
│   ├── TeacherCameraController
│   ├── TeacherFootstepAudio
│   ├── StandaloneInputModule
│   └── Screen-space training HUD
└── IVR mode
    ├── XR Origin + floor-offset camera
    ├── tracked HMD pose
    ├── left/right XR ray interactors
    ├── XRUIInputModule
    └── world-space training HUD
```

`TrainingExperienceModeController` owns subsystem startup/shutdown and display state. `XrTeacherRigAdapter` owns transient rig, tracked-pose actions, controller rays, and XR UI components. Returning to desktop disposes input actions, destroys the transient head driver and XR Origin, disables/removes XR-only UI components, and restores the original camera, canvas, raycaster, and input module.

## Build commands

Configure XR settings from the Unity command line:

```powershell
& '<Unity.exe>' -batchmode -nographics -quit `
  -projectPath '<repository-root>' `
  -executeMethod AdieLab.TeacherTraining.Editor.MetaQuestProjectConfigurator.ConfigureFromCommandLine `
  -logFile '<repository-root>\Logs\meta-quest-configure.log'
```

Build the Quest APK:

```powershell
& '<Unity.exe>' -batchmode -nographics -quit `
  -projectPath '<repository-root>' `
  -executeMethod AdieLab.TeacherTraining.Editor.KoreanClassroomBuilder.BuildMetaQuestFromCommandLine `
  -logFile '<repository-root>\Logs\meta-quest-build.log'
```

Output: `Builds/TeacherResponseTrainingQuest/TeacherResponseTrainingQuest.apk`

## Secure LLM proxy boundary

Do not place an OpenRouter API key in the APK, PlayerPrefs, scene, ScriptableObject, StreamingAssets, or source control. `TrainingDeploymentTarget.ImmersiveVr` explicitly reports `supportsEnvironmentSecret=false` and `requiresSecureLlmProxy=true`. Android and WebGL resolve to `LlmTransportMode.SecureProxy`; the policy rejects insecure URLs and the direct `openrouter.ai` host.

The Unity client side is implemented in `SecureProxyLlmGateway`. It sends versioned, provider-key-free envelopes to `/v1/student-turn` and `/v1/teacher-rubric`, requires a short-lived bearer token held only in memory, checks request/response IDs, and reuses the same student-turn and rubric validators as desktop. A deployment-specific `SecureLlmProxySettings` asset supplies only the HTTPS relay URL and public client ID.

The Cloudflare relay is deployed and the Quest build packages a provider-key-free `SecureLlmProxySettings` asset. The automatic 24-hour Quest session token is held in memory and authorizes research upload plus the versioned LLM, transcription, and speech routes. Provider keys remain Worker secrets. If the OpenRouter provider secret is unavailable, the server returns a typed `503` response and Unity preserves the deterministic local dialogue / scheduled-lipsync fallback.

### Proxy API contract

- `POST {proxy}/v1/student-turn`: accepts `LlmProxyTurnEnvelope.studentTurn`; returns `LlmProxyTurnResponse.studentTurn`.
- `POST {proxy}/v1/teacher-rubric`: accepts `LlmProxyTurnEnvelope.teacherRubric`; returns `LlmProxyTurnResponse.teacherRubric`.
- `POST {proxy}/v1/transcribe`: accepts bounded mono WAV microphone data; returns a Korean transcript without persisting the audio.
- `POST {proxy}/v1/speech`: accepts bounded text and affect-derived prosody; returns WAV student speech for spatial playback and waveform lip sync.
- `Authorization: Bearer <short-lived study session token>` is required. This token is not an OpenRouter key.
- Both response routes must echo `requestId` and schema version `1`.
- Provider prompts, provider selection, secrets, quotas, and audit policy remain server-side.

## Validation completed on 2026-07-19

- XR configuration command completed with Standalone and Android loaders and ARM64.
- EditMode regression suite passed `74/74`.
- Both authored classroom scenes passed the ScriptableObject PlayMode gate with six beats and three visible choices.
- XR rig structure PlayMode QA created two tracked controller rays, changed the HUD to world space, then restored all desktop camera/input/UI state.
- Windows player build succeeded.
- Meta Quest release APK build succeeded.

Physical headset acceptance is still required. The APK has not yet been evaluated on a Quest for controller ergonomics, readable angular text size, motion comfort, sustained frame timing, thermal behavior, passthrough/guardian interaction, microphone permission, or long-session memory use.

## Device acceptance checklist

1. Install the APK on Quest 2/3/3S and verify OpenXR startup from a cold launch.
2. Confirm head height, teacher viewpoint, floor origin, recentering, and seated/standing comfort.
3. Select every top mode, response choice, dialogue control, and debrief action with both controller rays.
4. Verify the world-space HUD never intersects the student, desks, or speech bubble and remains legible at normal render scale.
5. Exercise eye contact, extreme gestures, chin-rest, yawn, and chair collision in both scenes.
6. Profile GPU/CPU frame time, draw calls, memory, thermals, and battery for a representative training session.
7. Connect only to the secure LLM relay; confirm the APK contains no provider credential.

## Device-adaptive Quest attention tracking

The same Quest APK now selects its attention source from `SystemInfo.deviceModel` at runtime. Quest Pro requires live OpenXR eye gaze before accepting a teacher utterance or response choice. Quest 2, Quest 3, and Quest 3S automatically use the headset camera's forward ray as `EyeTrackingSource.HeadGazeFallback`, remain fully interactive, and skip the unsupported runtime eye-permission request. Eye-gaze and head-gaze records are never merged in the research aggregates.

| Headset | Runtime source | Interaction gate | Research interpretation |
|---|---|---|---|
| Meta Quest Pro | `EyeGaze` | Requires permission, calibration, and active tracking | Eye-attention evidence |
| Meta/Oculus Quest 2 | `HeadGazeFallback` | No eye-tracking gate | Head-orientation proxy only |
| Meta Quest 3 / 3S | `HeadGazeFallback` | No eye-tracking gate | Head-orientation proxy only |
| Unknown model in the research build | No fallback | Conservative live-eye requirement | Must be qualified before use |

Runtime flow:

1. `QuestProEyeGazeProvider` reads OpenXR `<EyeGaze>/pose` on Quest Pro. On known head-gaze-only Quest models it skips the eye permission request and uses the center-camera ray.
2. Semantic AOIs follow the focal student's eyes, face, mouth, torso, hands, and desk. Peer students and the world-space HUD have separate stable AOI identifiers.
3. `TeacherEyeTrackingRecorder` samples at 30 Hz by default and links gaze evidence to scenario, beat, student affect, gesture, and student-to-teacher gaze state.
4. Raw gaze JSONL is disabled by default. When explicitly enabled in `EyeTrackingResearchSettings`, it is saved below `Application.persistentDataPath/eye-tracking`.
5. Every teacher action stores a summarized `TeacherGazeSummary`: valid-sample ratio, cue-to-fixation latency, AOI dwell, mutual-gaze duration, fixation/revisit/transition counts, and missed-cue status.
6. The debrief dashboard and timeline label live eye tracking and head-gaze fallback separately; exports preserve `trackingSource` for every action.

The fixation thresholds are engineering defaults for pilot testing, not validated psychological cutoffs. Quest Pro OpenXR supplies a combined gaze pose; the implementation does not claim pupil diameter, per-eye gaze, blink metrics, or access to eye-camera images. Quest 2/3/3S values describe head orientation and must not be reported as eye tracking or fixation.

### Quest Pro physical-device gate

- Complete Meta eye-tracking calibration before launch.
- Verify the permission-granted and permission-denied paths.
- Confirm that research controls remain locked until `EyeTrackingSource.EyeGaze` is active.
- Validate focal eyes/face/hands/desk, peer, HUD, and board AOIs in both classroom scenes.
- Confirm invalid tracking during headset removal produces invalid samples rather than false AOI hits.
- Run microphone, student TTS, lip sync, secure LLM relay, and eye tracking concurrently.
- Complete a 20 to 30 minute thermal/frame-time session and inspect valid-sample coverage.
- Export the debrief JSON and both CSV files, then verify action IDs, beats, student state, and gaze summaries align.

### Quest 2 physical-device gate

- Confirm the app starts without an eye-tracking calibration or permission blocker.
- Confirm free dialogue, response choices, controller input, microphone, and locomotion remain usable.
- Verify raw samples and action summaries report `HeadGazeFallback`.
- Confirm the dashboard increments **머리방향 대체 행동** while **유효 아이트래킹 행동** remains zero.
- Re-run the same scenario on Quest Pro and verify the two sources remain analytically separate.
### Automated verification (2026-07-20)

- Unity EditMode regression: **115/115 passed**, zero failures or skips.
- Worker contract/type/lint suite: **12/12 passed** with strict TypeScript and Zod boundary validation.
- XR rig PlayMode QA: `XR_RIG_STRUCTURE_QA_OK controllers=2 hud=world-space cleanup=desktop-restored`.
- Scenario PlayMode QA: both scenes, six beats per scene, three choices, ScriptableObject source.
- Final ARM64 APK: `Builds/TeacherResponseTrainingQuest/TeacherResponseTrainingQuest.apk`, 130,546,150 bytes.
- APK SHA-256: `E29C633B3E254577A48B9FE7B362592393F8A7DC72AB9059C5D43C14F1629BE4`.
- Final merged manifest: `android.permission.RECORD_AUDIO` is present; microphone and eye tracking are optional; minimum SDK 29, target SDK 36, ARM64 only.
- Worker deployment: `30c6e002-0990-4796-af1d-c60d3205ad6e` at `https://teacher-training-collector.jewoong-moon.workers.dev`; health, automatic Quest token issuance, Korean TTS, and Korean STT passed live checks.

### Remaining deployment and device gates

- `OPENROUTER_API_KEY` is active for every managed AI route. Live Quest-token requests completed schema-valid student dialogue, Korean student TTS (`x-ai/grok-voice-tts-1.0`, voice `Ara`), and WAV transcription (`openai/gpt-4o-mini-transcribe`) through OpenRouter. A generated 196,372-byte RIFF/WAVE round-tripped to the exact Korean transcript.
- The APK is valid for local sideloading but is signed with the Android debug certificate. A release keystore and incremented version code are still required for managed distribution.
- No Quest was connected during this implementation pass, so on-headset controller mapping, microphone permission UX, Korean transcription quality, spatial TTS/lip sync, frame timing, and suspend/resume remain physical-device acceptance gates.