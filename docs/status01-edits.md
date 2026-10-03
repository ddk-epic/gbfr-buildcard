# `status01` edit log

Append-only: new entries go at the bottom, existing entries are never changed. A later edit that undoes or changes an earlier one gets its own entry. Object Ids refer to the file as it was at that entry. Structure: [status01.md](status01.md).

## 2026-10-03 · d6ca695 · Stock import

`status01.prfb` from the game (v2.0.6), converted unchanged with `gbfr.uitools.exe b-convert`. 426 objects, Ids 0-425.

## 2026-10-03 · 6c6097b · Build card section outlines

Script: `tools/scripts/scaffold.py`

| Objects | Change |
|---|---|
| 2 `loc_base01` | 426 appended to `Children` |
| 426-461 (new) | `loc_buildcard`, 3424x1712 (2:1), centred in `loc_base01`; the sharecard grid scaled x1.189 as 7 outlined sections (`bc_card`, `bc_portrait`, `bc_skills`, `bc_gear`, `bc_mtraits`, `bc_om`, `bc_summons`), each 4 sprite-less `Image` edges, 6 px |

## 2026-10-03 · acf24f1 · Blocks scaled into their sections

Script: `tools/scripts/scale_blocks.py`

| Objects | Change |
|---|---|
| 73 `loc_chr_status01` (status) | bottom of the yellow section; `Position` -2000.089, -431.239; `Scale` 0.328 |
| 109 `loc_chr_status02` (gear) | top of the cyan section; `Position` -1189.267, 707.82; `Scale` 0.442 |
| 275 `loc_chr_status03` (skills) | centre of the green section; `Position` -2000.089, -657.456; `Scale` 0.328 |
| 369 `loc_chr_status04` (support skills) | `Active: false` |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match each new `Position`.

## 2026-10-03 · cf69e02 · Badges moved into the yellow section

Script: `tools/scripts/place_badges.py`

Unscaled, stacked bottom-up in sharecard order with a 23 px (sharecard) gap: status block, name, PWR; the level pair at the top.

| Objects | Change |
|---|---|
| 7 `loc_name01` (name band) | left edge of the yellow section, one gap above the status block; `Pivot` 0, 0.5 so the fitted width grows right; `Position` -1692.978, -325.956 |
| 17 `power01` (PWR) | left-aligned, one gap above the name band; `Position` 151.022, -217.611 |
| 21 `level01` (Lvl) | 52 down so the "Lvl" label clears the section top; `Position` 192, 695.978 |
| 45 `loc_ml_level01` (Master Lvl) | 52 down with `level01`; `Position` 396, 731.978 |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match each new `Position`.
