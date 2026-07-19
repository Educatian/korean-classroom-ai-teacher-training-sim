# Visual Upgrade Audit

## Outcome

The classroom visual pass now uses three generated, tileable surface assets and a Blender-authored reusable prop kit. The source `.blend`, Unity-importable OBJ/MTL assets, and preview render are versioned so the scene can be rebuilt without a flattened classroom image.

## Gaps found in the previous build

| Priority | Area | Previous limitation | Upgrade / next acceptance check |
|---|---|---|---|
| P0 | Electronic board | Read as stacked primitives; camera, bezel, speakers and I/O were only symbolic | Blender mesh separates frame, inset glass, tray, stylus, camera/lens, status LED, ports and speaker apertures. Check scale and screen alignment in both scenes. |
| P0 | Floor | Texture was visually uniform and repeated too cleanly | Generated Korean classroom vinyl base color with restrained wear and retained micro-normal. Check tiling from teacher eye height. |
| P0 | Desktops | Laminate lacked edge/material variation at close range | Generated birch laminate is used on desk tops with existing micro-normal. Check grain scale and hand-contact close-ups. |
| P0 | Walls | Eggshell wall read as a flat color | Generated warm off-white painted wall surface with subtle roller variation. Check that contrast does not reduce HUD readability. |
| P1 | HUD microphone state | Active/inactive state was easy to miss | Microphone button now changes label, color and status text; second click stops capture and submits the transcript. Add a waveform only after real input amplitude is exposed. |
| P1 | HUD response feedback | Most feedback relied on text/color alone | Preserve press animation and audio; add a compact affect delta strip and request/provenance badge in the next HUD pass. |
| P1 | Teacher-area props | Air purifier, podium and desk objects were simple or absent | Blender-authored purifier, podium, books, pencil case, bottle and backpack are independent assets and placed by the procedural builder. |
| P1 | Material cohesion | HUD looked polished while the environment remained primitive | Use physically plausible roughness and consistent teal/charcoal accents across electronic equipment and HUD. Validate under classroom lighting. |
| P2 | HUD surface depth | Panels remain mostly solid-color rounded rectangles | Add subtle frosted noise, one-pixel highlight and restrained shadow without rasterized text. |
| P2 | Emotion telemetry | Valence/arousal/dominance exists but is not immediately scannable | Add a compact three-channel change indicator with accessible numeric labels and no face occlusion. |

## Generated assets

- `Assets/Art/GeneratedMaterials/TX_ClassroomVinyl_BaseColor.png`
- `Assets/Art/GeneratedMaterials/TX_DeskBirch_BaseColor.png`
- `Assets/Art/GeneratedMaterials/TX_WallPaint_BaseColor.png`
- `Assets/Models/Generated/ClassroomUpgrade_Source.blend`
- `Assets/Models/Generated/SM_ElectronicBoard_Realistic.obj`
- `Assets/Models/Generated/SM_AirPurifier_Realistic.obj`
- `Assets/Models/Generated/SM_TeacherPodium_Realistic.obj`
- `Assets/Models/Generated/SM_DeskProps_Realistic.obj`
- `Assets/Models/Generated/SM_SchoolBackpack_Realistic.obj`
- `Assets/Reference/Blender_ClassroomUpgrade_Preview.png`

## Design principles

1. Do not bake Korean classroom walls or bulletin boards into a single backdrop.
2. Keep every high-value prop independently placeable and reusable between the general and circle scenes.
3. Keep text in TextMeshPro, not in generated raster images.
4. Prefer believable material response and proportions over excessive surface noise.
5. Validate from the actual teacher camera, including Quest eye height, before accepting a mesh.