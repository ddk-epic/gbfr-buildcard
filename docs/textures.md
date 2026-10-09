# Textures

The mod's own textures, in `gbfr.qol.buildcard/GBFR/data/ui/layouts/pause/status/noatlastextures/`. Each is a `.wtb`
with a hand-written `.tex.yaml` next to it; the mod build converts the `.tex.yaml` to the game's `.texb`.

| Texture | Drawn by | Size | Use |
|---|---|---|---|
| `bc_white` | `tools/textures/gen_white.py 8 <out.png>` | 8x8 | opaque white, the sprite of rectangular masks |
| `bc_outline` | `tools/textures/gen_outline.py 20 2 8 <out.png>` | 20x20 | white outline 2 wide with corners of radius 8, sliced with border 8 |
| `bc_backdrop_mask` | `tools/textures/gen_backdrop_mask.py 2048 1024 <out.png>` | 2048x1024 | the parchment cut over `chr_status_bg01`'s white panel |
| `bc_rounded` | `tools/textures/gen_outline.py 20 10 8 <out.png>` | 20x20 | opaque white square with corners of radius 8, sliced with border 8; the panels' fill under `bc_outline` |
| `bc_outline_top` | `tools/textures/gen_outline.py 20 2 8 <out.png> top` | 20x20 | `bc_outline` with only the top corners rounded; turned 180° for the bottom ones |
| `bc_rounded_top` | `tools/textures/gen_outline.py 20 10 8 <out.png> top` | 20x20 | `bc_rounded` with only the top corners rounded; turned 180° for the bottom ones |
| `bc_mt_clip` | `tools/textures/gen_rounded.py 1024 1024 1800.94 1347.03 8 <out.png>` | 1024x1024 | opaque white over the master traits section's rect in card units, corners of radius 8; that section's mask |
| `bc_portrait_mask` | `tools/textures/gen_portrait_mask.py <ps_cmn_mask_chara02.dds> 1225 661 722 820 0.75 0.3 8 710 <out.png>` | 640x640 | opaque on the right; over card units from that edge, an ease to 0.75 over 661-722, an S-curve to 0.3 over 722-820, then a tail to 0 at 1225, joined without kinks; the fade leans 8° about card row 710, further right above it; its rows fade like `ps_cmn_mask_chara02`'s, its source decoded with `texconv -m 1 -f R8G8B8A8_UNORM -ft dds`. The portrait's mask, 1225 card units wide |

## Building a texture

1. Draw the PNG with its script.
2. Convert it with `gbfr.uitools.exe img-to-tex -i <name>.png -o <name>.wtb --no-mipmaps`. The tool runs
   `Binaries/texconv.exe` relative to the working directory; a copy is in
   `external/GBFRDataTools/ImageSharp.Textures/tests/Tools/`. `texconv` writes to `temp/temp/` while the tool reads
   `temp/`, so `temp/temp` must be a junction to `temp`.
3. Write `<name>.tex.yaml`: the size, and one sprite with its rect, `Border` for slicing, padding and UV.
4. Copy the `.wtb` to the same path under `ui/fhd/`: the game reads the `fhd` copy at 1080p, without falling back.
5. Add the texture to the prefab that uses it; the build adds it to that prefab's asset list.

The tool's `.tex.yaml` input strips the name with `TrimEnd(".tex.yaml")`, a character set, so a name ending in any of
those letters loses them.
