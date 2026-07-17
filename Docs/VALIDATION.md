# Validation record

## Current automated baseline

On 2026-07-17, the main project was regenerated with Unity `6000.4.9f1` and the EditMode suite passed `33/33` tests with zero failures, skips, or inconclusive results.

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

## Open visual QA items

The repository is an active prototype. Current review notes to address in the next visual pass are:

- tune the female Rocketbox torso UV origin so chest graphics sit slightly higher;
- replace the ribbon-like first graphic with a less letter-like abstract motif;
- harden the clothing mask around hand UV islands and verify uncropped full-body poses;
- replace the legacy gameplay preview with a verified continuous interaction recording.

These items do not invalidate the automated baseline, but they remain release blockers for a polished demonstration build.

## Release checklist

1. Regenerate both scenes from the editor builder.
2. Run all EditMode tests.
3. Build the Windows player and confirm the Unity build result is `Succeeded`.
4. Exercise response, dialogue, observation, and debrief modes in the built player.
5. Inspect focal and classmate faces, hands, clothing, desk collision, and chair masking.
6. Decode any delivered MP4 from start to finish and inspect a timeline contact sheet.
7. Scan tracked files for secrets and files above GitHub's size limit.
