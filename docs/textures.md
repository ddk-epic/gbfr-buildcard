# Textures

The mod's own textures, in `gbfr.qol.buildcard/GBFR/data/ui/layouts/pause/status/noatlastextures/`. Each is a `.wtb`
with a hand-written `.tex.yaml` next to it; the mod build converts the `.tex.yaml` to the game's `.texb`.

| Texture | Drawn by | Size | Use |
|---|---|---|---|
| `bc_white` | `tools/textures/gen_white.py 8 <out.png>` | 8x8 | opaque white, the sprite of rectangular masks |
| `bc_outline` | `tools/textures/gen_outline.py 20 2 8 <out.png>` | 20x20 | white outline 2 wide with corners of radius 8, sliced with border 8 |
| `bc_backdrop_mask` | `tools/textures/gen_backdrop_mask.py 2048 1024 <out.png>` | 2048x1024 | sharecard's parchment cut over `chr_status_bg01`'s white panel |

## Building a texture

1. Draw the PNG with its script.
2. Convert it with `gbfr.uitools.exe img-to-tex -i <name>.png -o <name>.wtb --no-mipmaps`. The tool runs
   `Binaries/texconv.exe` relative to the working directory; a copy is in
   `external/GBFRDataTools/ImageSharp.Textures/tests/Tools/`.
3. Write `<name>.tex.yaml`: the size, and one sprite with its rect, `Border` for slicing, padding and UV.
4. Copy the `.wtb` to the same path under `ui/fhd/`: the game reads the `fhd` copy at 1080p, without falling back.
5. Add the texture to the prefab that uses it; the build adds it to that prefab's asset list.

The tool's `.tex.yaml` input strips the name with `TrimEnd(".tex.yaml")`, a character set, so a name ending in any of
those letters loses them.
