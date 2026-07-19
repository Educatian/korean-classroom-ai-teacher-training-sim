# Project guide

## Purpose

This Unity project is a research prototype for training teachers to respond to elementary students showing emotional and behavioral distress. It combines a realistic Korean classroom, embodied student non-player characters, direct dialogue, structured response choices, affect dynamics, and optional OpenRouter interaction.

It is a simulation and development artifact. It is not a diagnostic tool, a substitute for school policy, or evidence that a trainee is qualified to handle a real crisis.

## Supported environment

- Windows 11
- Unity `6000.4.9f1`
- Built-in Render Pipeline
- TextMesh Pro and Unity UI
- Optional network access to OpenRouter

Open the repository root through Unity Hub. Unity will recreate `Library`, `Temp`, `Logs`, and IDE project files locally.

## Primary scenes

- `Assets/Scenes/KoreanClassroomTraining.unity`: forward-facing classroom response scenario.
- `Assets/Scenes/KoreanClassroomCircleTraining.unity`: circular discussion and presentation scenario.

The scene builder is the source of truth for generated classroom objects and material assignments:

`Tools > Teacher Training > Build Korean Classroom`

The command-line entry points are implemented in `Assets/Editor/KoreanClassroomBuilder.cs`.

## Runtime flow

1. The teacher observes the focal student's affect, gaze, breathing, and gesture cues.
2. The teacher selects a response or enters a direct Korean utterance.
3. The active `ILlmGateway` returns a strictly validated student turn, affect targets, action units, gesture, and relational dialogue signals.
4. `ConversationSessionState` retains a bounded recent transcript plus trust, pressure, and re-entry readiness.
5. `ScenarioTransitionEngine` deterministically selects hold, escalation, de-escalation, safety override, or instructional re-entry from validated signals.
6. `AffectDynamics`, `FacialActionUnitController`, `NpcPerformance`, and the gesture planner update face and body behavior.
7. A second structured LLM call evaluates the teacher utterance against all six research competencies. It currently runs as a visible shadow rubric while the established local score remains the study-safe primary score.
8. Response quality and the final debrief are written to a JSON Lines session log under `Application.persistentDataPath`.

## Bounded-generative dialogue architecture

`GenerativeAiCoach` is the desktop development implementation of `ILlmGateway`. It requests OpenRouter `json_schema` output with `strict=true`; malformed student turns, invalid signal ranges, incomplete rubrics, unsafe text, and unsupported gestures are rejected at the runtime boundary. Prompt context contains only the bounded recent conversation and derived session state.

Student output includes five normalized transition signals: felt heard, perceived pressure, choice offered, safety concern, and readiness for instructional re-entry. These signals do not directly mutate the authored scenario. `ScenarioTransitionEngine` converts them into an auditable deterministic next-beat decision against the ScriptableObject crisis stages.

`SecureProxyLlmGateway` is the Quest/WebGL client boundary. It accepts only HTTPS endpoints that are not the direct OpenRouter host, sends provider-key-free versioned envelopes, and requires a short-lived bearer session token held only in memory. The server URL belongs in a `SecureLlmProxySettings` Resources asset for a deployment environment; provider credentials remain server-side.

## Researcher-authored scenarios

Personas and scenario beats are ScriptableObject assets under `Assets/Resources/Training`. Each beat explicitly records its trigger, crisis stage, focal student, scenario type, teacher competency goals, and safety/context flags. Researchers can edit or add these assets in the Unity Inspector without changing runtime code. Follow `Docs/SCENARIO_AUTHORING.md` for the complete authoring and catalog-registration workflow.

`TrainingScenarioLibrary` remains as a compatibility-facing runtime loader; it no longer owns hardcoded scenario content.

## Meta Quest / immersive VR status

The training scenes now expose a top-bar `DESKTOP` / `IVR` toggle. XR Plug-in Management, OpenXR, Meta OpenXR, and XR Interaction Toolkit are installed. IVR activation creates a transient XR Origin, tracked head and two controller-ray inputs, and a world-space version of the existing HUD; returning to desktop restores the original camera, input module, footstep behavior, and screen-space HUD. Android defaults to IVR while Windows and the Editor default to desktop. A release Quest APK is produced by `KoreanClassroomBuilder.BuildMetaQuestFromCommandLine`.

The local XR rig and APK build are validated, but physical-headset acceptance remains outstanding. The Quest client contract and transport policy are implemented; a deployed relay URL, participant authentication/session issuance, and server-side OpenRouter credential are still required for live headset dialogue. Package versions, build commands, security boundaries, and the headset QA checklist are documented in `Docs/META_QUEST_READINESS.md`.

## OpenRouter setup

No API key is serialized into scenes, prefabs, source code, or this repository. Set the following operating-system environment variables before starting Unity or the player:

```text
OPENROUTER_API_KEY=<your key>
OPENROUTER_ENDPOINT=https://openrouter.ai/api/v1/chat/completions
OPENROUTER_MODEL=openai/gpt-4o-mini
```

Only `OPENROUTER_API_KEY` is required. The endpoint and model values are optional overrides. When configuration or connectivity is unavailable, the simulation keeps the session running with its local deterministic dialogue path.

## Building

From the Unity menu, use the project builder command. For automated Windows builds:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' `
  -batchmode -nographics -quit `
  -projectPath '<repository-root>' `
  -executeMethod AdieLab.TeacherTraining.Editor.KoreanClassroomBuilder.BuildWindowsFromCommandLine `
  -logFile '<repository-root>\Logs\windows-build.log'
```

Generated player output belongs under `Builds/` and is intentionally excluded from Git.

## Testing

Run EditMode tests through Unity Test Runner or the command in `Docs/VALIDATION.md`. The tests cover scenario scoring, affect transitions, facial controls, OpenRouter payload handling, UI text, speech-bubble anchoring, classroom layout, gaze constraints, backpacks, and randomized student outfit assignments.

## Key directories

- `Assets/Scripts/Runtime`: simulation, interaction, affect, animation, UI, logging, and OpenRouter code.
- `Assets/Editor`: reproducible scene, material, mesh, UI, and build generation.
- `Assets/Tests/EditMode`: automated behavioral and scene-contract tests.
- `Assets/Generated`: project-specific generated face and clothing textures.
- `Assets/Art`: classroom textures, UI surfaces, audio, and reference-driven art.
- `Assets/ThirdParty/MicrosoftRocketbox`: Microsoft Rocketbox source avatars and license.
- `Logs/VisualQa`: local-only QA captures excluded from source control.
- `Docs`: architecture, asset pipeline, validation, and visual-improvement notes.

## Development rules

- Edit the scene builder when a generated object or material must survive scene regeneration.
- Keep API keys in the operating-system environment only.
- Preserve Unity `.meta` files whenever an asset is added or moved.
- Run scene regeneration and the EditMode suite after changing shader properties or serialized builder data.
- Treat student behavior as a training scenario, not a clinical label or diagnosis.
