# Validation record

## Current automated baseline

On 2026-07-18, both classroom scenes were regenerated with Unity `6000.4.9f1` and the expanded EditMode suite passed `67/67` tests with zero failures, skips, or inconclusive results. The Windows player build completed successfully at 318,261,430 bytes.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath '<repository-root>' `
  -runTests -testPlatform editmode `
  -testResults '<repository-root>\Logs\editmode-results.xml' `
  -logFile '<repository-root>\Logs\editmode.log'
```

The local logs are excluded from Git. A passing result must be regenerated on each development machine or continuous-integration runner.

## Manual QA completed

- Both classroom layouts render with 15 students and direct teacher-facing conversation framing.
- A 30-render face roster covering both scenes was independently reviewed with no duplicate eyes, noses, lips, or broken face meshes.
- The randomized outfit roster shows 15 distinct color, pattern, fabric, and chest-graphic assignments, and the original Rocketbox shirt marks are suppressed.
- Speech bubbles remain anchored above the focal student's head and the dialogue HUD remains readable at 1920×1080.

## Completed visual-polish pass

- Raised the female Rocketbox chest-graphic origin from 0.430 to 0.455 while preserving the male origin.
- Replaced the top-left letter-like apparel mark with a rounded three-pebble orbit generated through imagegen.
- Added a skin-protection mask so torso fallback tinting does not recolor skin-like hand UV islands.
- Recorded a continuous built-player interaction covering free dialogue, upright eye contact, mode changes, response choices, and debrief. The delivered H.264 MP4 is 2560x1080 at 30 fps for 36.2 seconds and decodes without errors.

## Final manual visual sign-off (2026-07-18)

- `Unity_VisualPolish_FemaleYawn_FullBody.png` confirms the raised female chest graphic remains on the shirt and both hands retain their skin material during the yawn pose.
- `Unity_VisualPolish_ChinRest_FullBody.png` confirms the full seated silhouette remains readable during the chin-rest/thoughtful pose, without clothing, desk, or chair penetration.
- `Unity_VisualPolish_ExtremePushAway_FullBody.png` confirms both open hands remain skin-toned during the strong two-arm gesture and neither sleeve nor torso is masked by the chair.
- The rounded three-pebble orbit motif remains visibly abstract and non-letter-like in both the close pose captures and normal-distance classroom captures.

The final PlayMode run logged `CLASSMATE_GAZE_QA_OK`, `NPC_IDLE_BEHAVIOR_QA_OK`, and `CLASSROOM_AUDIO_QA_OK`. Automated scene contracts, the `67/67` EditMode suite, player build, MP4 decode, timeline contact-sheet generation, and the required human visual checks are complete.

## Release checklist

1. Regenerate both scenes from the editor builder.
2. Run all EditMode tests.
3. Build the Windows player and confirm the Unity build result is `Succeeded`.
4. Exercise response, dialogue, observation, and debrief modes in the built player.
5. Inspect focal and classmate faces, hands, clothing, desk collision, and chair masking.
6. Decode any delivered MP4 from start to finish and inspect a timeline contact sheet.
7. Scan tracked files for secrets and files above GitHub's size limit.
