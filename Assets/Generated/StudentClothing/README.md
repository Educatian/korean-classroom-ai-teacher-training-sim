# Generated student clothing assets

These textures were created with Codex built-in image generation on 2026-07-17 for the Korean elementary-school teacher-training simulation. They contain no third-party marks, words, or branded apparel artwork.

## Fabric albedo set

The generation mode was square, neutral, photorealistic, tileable fabric albedo without folds, lighting gradients, logos, text, people, or garment silhouettes.

- `Cloth_01_HeatherCotton.png`: neutral heather cotton jersey with fine mixed fibers.
- `Cloth_02_RibKnit.png`: soft vertical rib-knit cotton.
- `Cloth_03_WaffleKnit.png`: small-scale waffle-knit cotton.
- `Cloth_04_CottonTwill.png`: subtle diagonal cotton twill weave.
- `Cloth_05_SlubJersey.png`: fine slub jersey with irregular natural yarn detail.
- `Cloth_06_BrushedFleece.png`: restrained brushed sweatshirt fleece.
- `Cloth_07_MicroCheck.png`: very small woven micro-check surface.
- `Cloth_08_MarledStripe.png`: muted marled horizontal knit stripe.

Unity imports these eight albedos at 1024 px, sRGB, repeat wrapping, high-quality compression, mipmaps enabled, and anisotropic level 4. The outfit shader recolors them, so the source images intentionally use neutral values.

## Original chest graphic atlas

`GraphicAtlas_15.png` was generated as an exact 4 by 4 high-contrast atlas: fifteen original, text-free youth apparel motifs and one blank bottom-right cell. Motifs include abstract geometry, mountain and sun, orbit, lightning, waves, pixel flower, checker crest, smiling sun, paper plane, sprout, arches, star cluster, ribbon, shape-only shield, and modern doodle lines. The source prompt required pure black cell backgrounds, centered white and light-gray graphics, generous padding, crisp edges, no words, no letters, no numbers, no brands, and no trademarks.

Unity imports the atlas at native resolution up to 2048 px, sRGB, clamp wrapping, uncompressed, and without mipmaps to prevent cross-cell bleeding. `StudentClothingTint.shader` thresholds the black background, recolors each motif, and selects a different cell for every student.

## Runtime assignment

`KoreanClassroomBuilder.ApplyStudentClothingTint` uses a fixed seed so regeneration is reproducible while still producing randomized-looking values. Across 15 students it assigns:

- 15 distinct primary colors;
- randomized contrasting accents and graphic colors;
- varied pattern type, density, and strength;
- all eight fabric surfaces;
- all fifteen nonblank graphic cells exactly once;
- randomized graphic scale, rotation, and chest offset.

