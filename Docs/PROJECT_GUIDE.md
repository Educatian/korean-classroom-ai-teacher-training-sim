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
3. The local scenario model or OpenRouter returns a student turn and affect targets.
4. `AffectDynamics` interpolates valence, arousal, dominance, and distress.
5. `FacialActionUnitController`, `NpcPerformance`, and the gesture planner update face and body behavior.
6. Response quality and the final debrief are written to a JSON Lines session log under `Application.persistentDataPath`.

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
- `Assets/Reference`: selected visual QA and storyboard artifacts.
- `Docs`: architecture, asset pipeline, validation, and visual-improvement notes.

## Development rules

- Edit the scene builder when a generated object or material must survive scene regeneration.
- Keep API keys in the operating-system environment only.
- Preserve Unity `.meta` files whenever an asset is added or moved.
- Run scene regeneration and the EditMode suite after changing shader properties or serialized builder data.
- Treat student behavior as a training scenario, not a clinical label or diagnosis.
