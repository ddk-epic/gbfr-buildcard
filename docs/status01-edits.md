# `status01` edit log

Append-only: new entries go at the bottom, existing entries are never changed. A later edit that undoes or changes an earlier one gets its own entry. Object Ids refer to the file as it was at that entry. Structure: [status01.md](status01.md).

## 2026-10-03 · Stock import

`status01.prfb` from the game (v2.0.6), converted unchanged with `gbfr.uitools.exe b-convert`. 426 objects, Ids 0-425.

## 2026-10-03 · Build card section outlines

Script: `tools/scripts/scaffold.py`

| Objects | Change |
|---|---|
| 2 `loc_base01` | 426 appended to `Children` |
| 426-461 (new) | `loc_buildcard`, 3424x1712 (2:1), centred in `loc_base01`; the sharecard grid scaled x1.189 as 7 outlined sections (`bc_card`, `bc_portrait`, `bc_skills`, `bc_gear`, `bc_mtraits`, `bc_om`, `bc_summons`), each 4 sprite-less `Image` edges, 6 px |

## 2026-10-03 · Blocks scaled into their sections

Script: `tools/scripts/scale_blocks.py`

| Objects | Change |
|---|---|
| 73 `loc_chr_status01` (status) | bottom of the yellow section; `Position` -2000.089, -431.239; `Scale` 0.328 |
| 109 `loc_chr_status02` (gear) | top of the cyan section; `Position` -1189.267, 707.82; `Scale` 0.442 |
| 275 `loc_chr_status03` (skills) | centre of the green section; `Position` -2000.089, -657.456; `Scale` 0.328 |
| 369 `loc_chr_status04` (support skills) | `Active: false` |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match each new `Position`.

## 2026-10-03 · Badges moved into the yellow section

Script: `tools/scripts/place_badges.py`

Unscaled, stacked bottom-up in sharecard order with a 23 px (sharecard) gap: status block, name, PWR; the level pair at the top.

| Objects | Change |
|---|---|
| 7 `loc_name01` (name band) | left edge of the yellow section, one gap above the status block; `Pivot` 0, 0.5 so the fitted width grows right; `Position` -1692.978, -325.956 |
| 17 `power01` (PWR) | left-aligned, one gap above the name band; `Position` 151.022, -217.611 |
| 21 `level01` (Lvl) | 52 down so the "Lvl" label clears the section top; `Position` 192, 695.978 |
| 45 `loc_ml_level01` (Master Lvl) | 52 down with `level01`; `Position` 396, 731.978 |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match each new `Position`.

## 2026-10-03 · Status block stacked into one column

Scripts: `tools/scripts/relayout_status.py`, then `tools/scripts/place_badges.py` to restack the badges above it

| Objects | Change |
|---|---|
| 73 `loc_chr_status01` (status) | 1000x336, bottom of the yellow section; `Position` -2000.089, -361.327; `Scale` 0.666 |
| 74 `chr_status01`, 76 `status_base01`, 78 `loc_status01` | resized to 1000x336 with the block; 75 `root` moved to the new top |
| 76 `status_base01` | `SpriteName` `ps_cmn_base52` -> `ps_cmn_base54`, the same frame without the title tab |
| 77 `status_ttl_text01` | `Active: false` |
| 79 `loc_chr_icon01` | left column, vertically centred |
| 86 `line01` | `Active: false` |
| 87 `loc_hp`, 92 `loc_atk`, 97 `loc_crt`, 104 `loc_brk` | one column of 64-high rows, HP to Stun Power; crt and brk 40 left so their icons line up |
| 7 `loc_name01` | `Position` -1692.978, -186.095 |
| 17 `power01` | `Position` 151.022, -77.75 |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match each new `Position` and size.

## 2026-10-03 · Skills block stacked into one column

Script: `tools/scripts/relayout_skills.py`

The cards keep their 1004x144 size: their sprite (`ps_cmn_ability_base04_02sub`) isn't sliced, so narrowing them would squash it.

| Objects | Change |
|---|---|
| 275 `loc_chr_status03` (skills) | 1273.893x596, centre of the green section, fitted to its height; `Position` -2000.089, -657.456; `Scale` 0.523 |
| 276 `chr_status03`, 278 `status_base01` | resized to 1273.893x596 with the block; 277 `root` moved to the new top |
| 278 `status_base01` | `SpriteName` `ps_cmn_base52` -> `ps_cmn_base54`, the same frame without the title tab |
| 279 `ttl01_text01` | `Active: false` |
| 280 `loc_status01` | 1004x588, centred |
| 281, 303, 325, 347 (ability cards) | one column in that order (slots 1 to 4), 4 apart; `Position` y 222, 74, -74, -222 |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match each new `Position` and size.

## 2026-10-03 · Gear block stacked into one column

Script: `tools/scripts/relayout_gear.py`

| Objects | Change |
|---|---|
| 109 `loc_chr_status02` (gear) | 1084x1076, top of the cyan section, fitted to its width; `Position` -1189.267, 390.894; `Scale` 0.829 |
| 110 `chr_status02`, 112 `status_base01` | resized to 1084x1076 with the block |
| 112 `status_base01` | `SpriteName` `ps_cmn_base52` -> `ps_cmn_base54`, the same frame without the title tab |
| 113 `ttl01_text01` | `Active: false` |
| 114 `chr_status02_p01` (weapon) | 1084x96, at the top; 116 `loc_equip01` (icon, name, level) at its left edge |
| 128 `loc_hp`, 131 `loc_atk`, 134 `loc_crt`, 139 `loc_brk` | a second line 96 below the weapon's, still right-aligned |
| 142 `loc_status01` (sigil background) | `SizeDelta` -24, 840, 12 above the frame's bottom |
| 143, 154, ..., 264 (sigil rows) | one column in slot order, 68 apart, left edge 24 from the frame's so the icons line up with the weapon's |
| 255 slot 11's `line01` | `Active: true` |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match each new `Position` and size.

## 2026-10-04 · Card text written by the mod

Script: `tools/scripts/add_card_text.py`

| Objects | Change |
|---|---|
| 462 `bc_text01` (new) | Text, 1700x64, top-left of the blue section; last child of 426 `loc_buildcard`, so Ids stay depth-first |
| 426 `loc_buildcard` | 462 appended to `Children` |
| 0 `status01`, `CharaInfo.Powers` | ref to 462's Text appended |

The game writes PWR into every `Powers` text while filling the page; the mod finds 462 through that ref and overwrites it right after (`Hooks/CardWriter.cs`).
