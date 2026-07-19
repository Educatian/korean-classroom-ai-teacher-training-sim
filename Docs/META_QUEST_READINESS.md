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

The relay must still be deployed. It owns the provider key, issues/validates participant sessions, enforces rate and token limits, strips unapproved telemetry, applies the same JSON schemas, and returns the versioned response envelope. Until a valid settings asset and session token are supplied, Quest remains in the deterministic local dialogue mode.

### Proxy API contract

- `POST {proxy}/v1/student-turn`: accepts `LlmProxyTurnEnvelope.studentTurn`; returns `LlmProxyTurnResponse.studentTurn`.
- `POST {proxy}/v1/teacher-rubric`: accepts `LlmProxyTurnEnvelope.teacherRubric`; returns `LlmProxyTurnResponse.teacherRubric`.
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
