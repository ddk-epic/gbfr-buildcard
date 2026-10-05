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

## 2026-10-04 · Master traits board

Script: `tools/scripts/add_master_traits.py`

| Objects | Change |
|---|---|
| 463 `bc_mtraits` (new) | container, same coordinates as 426 `loc_buildcard`; last child of 426 |
| 464-556 (new) | Images: per style a top border in the style's colour (`bc_mt_<s>_border`) and 30 cell fills (`bc_mt_<s>_<r>_<c>_fill`) |
| 557-767 (new) | Texts: the heading `bc_mt_heading`, then per style the title, the style name, per rank its label and count, then per rank and cell slot a picked (`_on`) and an unpicked (`_off`) text |
| 557-767 | `LanguageSetter` with `ld_skipstd_b_sdf_material`; cell texts `LineSpacing` -35, English override 8 |
| 462 `bc_text01` | perk summary: `FontSize` 22, right-aligned, 1070x38 at the heading's right; `LanguageSetter` added |
| 0 `status01`, `CharaInfo.Powers` | refs to 557-767's Texts appended |

The layout follows sharecard's board scaled by 3424/2880: three columns, rank sections of 4, 8, 8 and 10 cells in a two-column grid. The mod writes every text (`Hooks/CardWriter.cs`).

## 2026-10-04 · Over Mastery section

Script: `tools/scripts/add_over_mastery.py`, from `ui/layouts/pause/limitbonus/prefabs/lb_ovtli02.prfb`

| Objects | Change |
|---|---|
| 768 `bc_om` (new) | container, same coordinates as 426 `loc_buildcard`; last child of 426 |
| 769 `bc_om_heading` (new) | Text with `LanguageSetter` |
| 770, 802, 834, 866 `bc_om_<i>` (new) | copies of `lb_ovtli02` object 14 (`var00_lb_ovtli01_p01_01`, an Over Mastery row with `LimitBonusInfo`) and its subtree, 32 objects each, `Scale` 0.5674, one line per row in the magenta section |
| +3 of each row (`line01`), +11 (`loc_star01`) | `Active: false` |
| +4 of each row (`icon01`), +5 (`loc_text01`) | moved onto one line: icon at the row's left, name and value 10 right of it |
| +6, +9, +10 (`text01`, `num01`, `percent01`), +8 (`icon_plus01`) | `Color` 0.196, 0.373, 0.49, 1 (from near-white) |
| 0 `status01`, `CharaInfo.Powers` | ref to 769's Text appended, then plain object refs (`ComponentName: ''`, `Index: -1`) to the four rows |
| `status01.list` | `ImageData` `data/image/meditationicons`, `meditationlatters` and `LanguageData` `data/language/ld_tsukuoldminpro_r_sdf_material` appended (`tools/scripts/sync_list.py`) |

The mod calls the game's setter for an Over Mastery row on each row's `LimitBonusInfo` with the character's Over Mastery line, and hides rows whose line has no value (`Hooks/CardWriter.cs`). Plain object refs in `Powers` have no component, so the fill skips them.

The list holds the assets the game loads with the prefab. Without the image data the rows' `ImageMultiSetter` has no sprite sets, and the setter faults on them.

## 2026-10-04 · Summons section

Script: `tools/scripts/add_summons.py`, from `ui/layouts/pause/summon/prefabs/summon_list01.prfb` and `summon_info01.prfb`

| Objects | Change |
|---|---|
| 898 `bc_smn` (new) | container, same coordinates as 426 `loc_buildcard`; last child of 426 |
| 899, 955, 1011, 1067 `bc_smn_<i>` (new) | copies of `summon_list01` object 90 (summon equip slot) with only its `SummonInfo`, `Scale` 0.5116, in a 2x2 grid in the orange section; each the root of 56 objects |
| +1-15 of each slot (new) | the slot's visible parts from `summon_list01`: frame `base01`, icon stack, both name texts, element badge |
| +16-35 of each slot (new) | copy of `summon_info01` object 20 (`list_skill_p05_01`, trait row with `SkillInfo`) and its subtree, centred below the frame |
| +36-55 of each slot (new) | copy of `summon_info01` object 43 (`list_skill_p05_02`, equip bonus row with `LimitBonusInfo`) and its subtree, below the trait row |
| slot `SummonInfo` | `_5D33A08E` set to the trait row's `SkillInfo`, `F58112CE` to the equip bonus row's `LimitBonusInfo`, as in `summon_info01` |
| 0 `status01`, `CharaInfo.Powers` | plain object refs to the four slots appended |
| `status01.list` | `Materials` `fonts/fot_skipstd_b_sdf_ds01`, `ImageData` `data/image/summoniconframe01`, `summoniconbase`, `summonbaseparamicons`, `arrow/arrow03` and `LanguageData` `data/language/ld_skipstd_b_sdf_ds01` appended (`tools/scripts/sync_list.py`) |

The mod calls the game's `SetSummonInfo` on each slot with the equipped summon's id (`Hooks/CardWriter.cs`); it fills the slot and, through the two refs, both rows.

## 2026-10-04 · Weapon panel

Script: `tools/scripts/add_weapon.py`, from `ui/layouts/pause/equip/prefabs/equip01_info_weapon01.prfb`

| Objects | Change |
|---|---|
| 1123 `bc_weapon` (new) | copy of `equip01_info_weapon01` object 0 with only its `WeaponInfo`, `Scale` 0.749, at the top of the cyan section; last child of 426 `loc_buildcard` |
| 1124-1543 (new) | copies of the panel's objects 1-420: name and series, art, gauge, level, stats, weapon trait rows and wrightstone trait rows |
| 1124 `root` | `Active: true` (from false) |
| 0 `status01`, `CharaInfo.Weapon` | ref changed from 114's `WeaponInfo` to 1123's |
| 109 `loc_chr_status02` | `Active: false`: the gear block's weapon row and the sigils |
| `status01.list` | the panel's referenced textures, atlases, materials, animations, image data and language data appended, then the rest of `equip01_info_weapon01.list` except animations (`tools/scripts/sync_list.py`) |

The page fill calls the game's `WeaponInfo` setter on the ref in `CharaInfo.Weapon` with the character's weapon, which fills the whole panel. The art's `WeaponIconSetter` shows only textures that are already loaded; the mod adds the card's weapon to the art that the game's `LoadWeaponParty` loader loads while the Character Details page is open (`Hooks/WeaponArtHooks.cs`).

## 2026-10-04 · Portrait and badges scaled

Scripts: `tools/scripts/scale_portrait.py`, then `tools/scripts/scale_badges.py`

| Objects | Change |
|---|---|
| 3 `loc_chr` (portrait) | centred on sharecard's art; `Position` -1310.156, 0 |
| 4 `chr_img01`, 5 `chr_img01_mask` | `Scale` 0.788: the art 135% of the card's height, as on sharecard |
| 21 `level01`, 45 `loc_ml_level01` | `Scale` 0.7; diamonds at sharecard's places; `Position` 119.483, 737.111 and 260.129, 760.889 |
| 17 `power01` | `Scale` 0.7; `Position` 97.727, -76.575 |
| 7 `loc_name01` | `Scale` 0.7; `Pivot` 0.5, 0.5, centred in the first column; `Position` -1360.089, -181.672 |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match each new `Position` and `Pivot`.

## 2026-10-04 · Portrait masked

Script: `tools/scripts/mask_portrait.py`

| Objects | Change |
|---|---|
| 3 `loc_chr` (portrait) | `Mask` added, sprite `layouts/pause/pause_common/noatlastextures/ps_cmn_mask_chara02` (the gear screen's portrait mask, from `pause_chara01`); `Rotation` 0, 0, 1, 0 (180°), so the fade runs out to the right; `SizeDelta` 1407.644, 1758.93, `Pivot` 0.715, 0.487: the rect starts at the card's left and top edges, and the opaque part ends at sharecard x 518 |
| 4 `chr_img01`, 5 `chr_img01_mask` | `Rotation` 0, 0, 1, 0, cancelling `loc_chr`'s; pivots kept on `loc_chr`'s pivot |
| `status01.list` | `ps_cmn_mask_chara02` appended (`tools/scripts/sync_list.py`) |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match. The art is cut at the card's left and top edges and fades out over its bottom edge.

## 2026-10-04 · Gear column from the gear screen

Script: `tools/scripts/stack_gear.py`, from `ui/layouts/pause/equip/prefabs/equip01_info01.prfb` (Weapon section), `equip01_info_weapon01.prfb` (trait rows) and `equip01_info02.prfb` (Sigils section)

| Objects | Change |
|---|---|
| 1123-1543 (the equip screen's weapon panel) | removed |
| 1123 `bc_weapon` (new) | copy of `equip01_info01` object 0 with only its `WeaponInfo`, `Scale` 0.705, at the top of the cyan section; last child of 426 `loc_buildcard` |
| 1124-1396 (new) | copies of `equip01_info01`'s objects 1-273: title, name, art, level, gauge, stats |
| 1124 `root` | `Active: true` (from false); 1397, 1483, 1540 and 1542 appended to `Children` |
| 1248 `loc_max01` | `Active: true` (from false): the max level after the level |
| 1278 `loc_exp01` | `Active: false` (from true): the exp bar |
| 1397-1539 (new) | copies of `equip01_info_weapon01`'s objects 278-420: 1397 `loc_skill01` (weapon trait rows) and 1483 `loc_skill02` (imbued trait title and rows), below the stat row |
| 1540-1699 (new) | copies of `equip01_info02`'s objects 3-162: 1540 `ttl01` (Sigils title) and 1542 `loc_gene01` with the 12 sigil rows (`GemInfo`) 1544, 1557, ..., 1687, below the imbued traits |
| 1123's `WeaponInfo` | `Skills`, `PendulumSkillObj`, `PendulumSkills` and `PendulumNames` added, pointing at the copied trait rows as in `equip01_info_weapon01` |
| 0 `status01`, `CharaInfo.Gem` | refs changed from 143, 154, ..., 264 (the gear block's sigil rows) to 1544, 1557, ..., 1687 |
| `status01.list` | `equip01_info01.list` and `equip01_info02.list` merged, except animations (`tools/scripts/sync_list.py`) |

The page fill calls the `WeaponInfo` setter on 1123, which fills the Weapon section and the trait rows, and the `GemInfo` setter on each sigil row. 109 `loc_chr_status02` stays hidden. `ItemLevel` reads `HideExp` but doesn't act on it; the gear screen's controller hides the exp bar, so 1278 is hidden in the prefab.

## 2026-10-04 · Both sigil traits, matched trait rows

Scripts: `tools/scripts/rework_sigils.py`, then `tools/scripts/match_trait_rows.py`, then `tools/scripts/widen_weapon_section.py`

| Objects | Change |
|---|---|
| 1544, 1559, ..., 1709 (sigil rows, `GemInfo`) | 1266x80, the cyan section's width; 15 objects each, so Ids from 1557 on shift by 2 per row before them |
| +13 `bc_trait01`, +14 `bc_trait02` of each sigil row (new) | copies of the row's `text01_01`, without `ContentSizeFitter`, `FontSize` 40, `CharacterSpacing` 0, 442x80; last children of `loc_icon_skill` |
| +11 `icon_skill01`, +12 `icon_skill02` | 72x72; in front of the trait names; `SkillInfo.Names` set to +13 and +14 |
| +4 `icon01`, +5 `text01_01` (sigil icon and name) | `Active: false`; +4 removed from `GemInfo.Sets` |
| +6 `loc_text01_02` (level) | at the row's right edge, 8 in; `Padding` bottom 4 |
| 1398, 1415, ..., 1523 (weapon and wrightstone rows) | 1266 wide, left insets kept; +6 `loc_skill_lv01` at the right edge, 8 in, `Padding` right 56 -> 40, bottom 4; +7 `loc_lv01` `Spacing` 24 -> 8 |
| 1484 `loc_title01` (imbued traits title) | 1266 wide |
| 1231 `line01`, 1232 `line02` | 1266 wide |
| 1239 `loc_lv01` (weapon level), 1286 `loc_lt01` (gauge), 1390 `loc_tag01` | 65 left, 65 right, 65 right |
| 1376 `loc_status01` (stat row) | 1154 wide; 1377, 1380, 1383, 1387 (stat cells) a quarter each, edge to edge |
| 0 `status01`, `CharaInfo.Gem` | refs follow the renumbered rows |

`GemInfo` passes both traits to the row's two `SkillInfo`s; the `SkillInfo` setter writes the trait name into every text in `Names`. `Padding` is left, top, right, bottom.

## 2026-10-05 · Status block in a 2x2 grid, name and PWR restacked

Scripts: `tools/scripts/stats_grid.py`, then `tools/scripts/scale_badges.py`

Spacing follows sharecard's `StatusPanel` (border 1, padding 20/17.5, columns 9fr/11fr with a 25 gap, row gap 22.5), converted at the block's scale (0.56 sharecard pixels per unit).

| Objects | Change |
|---|---|
| 73 `loc_chr_status01` (status) | 1000x182.41, bottom edge kept; `Position` -2000.089, -412.456 |
| 74 `chr_status01`, 76 `status_base01`, 78 `loc_status01` | resized to 1000x182.41 with the block; 75 `root` moved to the new top |
| 79 `loc_chr_icon01` (character icon and element) | `Active: false` |
| 90, 95, 100, 107 (`hp_line01`, `atk_line01`, `crt_line01`, `brk_line01`) | `Active: false` |
| 87 `loc_hp`, 92 `loc_atk` | left column, 488.911x64; `Position` 5.661, 97.294 and 5.661, 83.036 |
| 97 `loc_crt`, 104 `loc_brk` | right column, 543.232x64; `Position` -82.446, 97.294 and -82.446, 83.036 |
| 91, 96, 102, 108 (`hp_num01`, `atk_num01`, `crt_num01`, `brk_num01`) | `FontSize` 56 -> 44.8, every enabled override 45 (1.12x the labels' 40); `Margin` bottom 2 -> 0; raised 2.16 onto the labels' baseline |
| 101 `loc_crt_num` | at the right column's digit edge; `Position` 509.232, 34.16 |
| 103 `crt_num00` (percent sign) | `FontSize` 32 -> 29.12 (0.65x the number), Chinese overrides 36 -> 29; 26.736x29.12; `Position` 31.2, -5.27 |
| 7 `loc_name01` | its bottom edge above the status block by the status-to-skills gap (28.4) plus the element icon's overhang (7); `Position` -1360.089, -291.11 |
| 17 `power01` | the diamond's lowest point 28.4 above the name band; `Position` 97.727, -173.814 |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match each new `Position` and size.

## 2026-10-05 · Skills in a 2x2 grid under a title bar on a status-style panel

Script: `tools/scripts/grid_skills.py` (with `pause/ability/prefabs/ability_info01_02.prfb.yaml`)

The Skills screen's cards, reduced to skill icon, name and element tag, laid out like sharecard's compact `SkillsSection` grid: icon on the left, name over element tag beside it. A `VerticalLayoutGroup` centres the name and tag vertically, so a name on more lines stays centred with its tag.

| Objects | Change |
|---|---|
| 275 `loc_chr_status03` (old skills block) | `Active: false` |
| 1724 `bc_skills` (new, last child of 426) | 1000x467.857 at scale 0.666 (the status block's), filling the green section; `Position` -1360.089, -657.456 |
| 1725 `bc_skills_base` (new) | the status block's frame Image (`ps_cmn_base54`, sliced), 1000x467.857 |
| 1726 `bc_skills_ttl`, 1727 `ttl01_text01` (new, copies of the Sigils title 1540-1541) | `TextSetter` `TXT_PAU_ABILITY` (the stock skills title); `Scale` 1.059 (the gear column's 0.705 in the container); inside the panel: text top 18.5 sharecard px (the panel's top padding) below its top, bar top 46 title units above the cells, as in the gear column; `Position` 0, 169.126 |
| 1728, 1786, 1844, 1902 (`ability_set01_btn04/03/01/02`, copies of `ability_info01_02` objects 4-231) | cells 482.143x160.69 in slot order, row by row, below the title, inside a 10/18.5 sharecard px padding; `Position` ±241.071, 40.088 and -120.566; only `AbilityInfo` kept (`DeviceObjSetter`, `Animator` removed) |
| `loc_base01`, `base01_set01` of each card | resized to the cell; Image `Color` alpha 0 |
| `loc_icon_pos`, `loc_guide_button` of each card | `Active: false` (slot diamond, key badges) |
| `loc_icon_ability` of each card | anchored left, `Scale` 0.825 (sharecard's 85 px icon), centre 81 from the cell's left edge |
| `bc_skill_text` (new, one per card, after the icon under `base01_set01`) | `VerticalLayoutGroup`: `ChildAlignment` 3 (middle-left), `Spacing` 32, `Padding` bottom 6 (centres the visible stack, cap top to element icon bottom); 326.571x160.69, from the element bar's left edge (155.6) to the cell's right edge |
| `text01` (name) of each card | moved into `bc_skill_text`; 326.571x50, anchors 0, 1, pivot 0, 0.5; `Margin` left 23 (the name starts above the element icon); `ContentSizeFitter` `VerticalFit` 2 (height from its lines); centre y 33 in the cell |
| `loc_elem01` (element tag) of each card | moved into `bc_skill_text` after the name; anchors 0, 1, pivot 0, 0.5; bar centre y -38 in the cell |
| 0 `status01`, `CharaInfo.Ability` | refs 281/303/325/347 -> 1728/1786/1844/1902 |
| 0 `status01`, `CharaInfo.Powers` | plain object refs to the names 1735/1793/1851/1909 appended; `CardWriter` wraps long names through them |

## 2026-10-05 · Singular Weapon title

Script: `tools/scripts/patch_texts.py`

| Objects | Change |
|---|---|
| 1127 `ttl01_text01` (Weapon title) | `TXT_PAU_ITEM_WEAPON` reads "Weapons" except for the sub-id `equip01_info01` ("Weapon"), the gear screen's prefab; the mod's English `text_ui.msg` adds a `status01` row with that text |

The sub-id is assumed to be the name of the prefab the text is in; not yet confirmed in game.

## 2026-10-05 · Over Mastery on a panel under a title bar

Script: `tools/scripts/panel_over_mastery.py`

| Objects | Change |
|---|---|
| 768 `bc_om` | the magenta section, 877.082x467.701 at `Scale` 0.666 (the skills container's); `Image` copied from 1725 `bc_skills_base` |
| 769 `bc_om_heading` -> `bc_om_ttl` | Text replaced by 1726 `bc_skills_ttl`'s `Image` and rect; `Position` 0, 169.048 |
| 1960 `bc_om_ttl_text` (new) | copy of 1727 `ttl01_text01`, `TextSetter` `TXT_PAU_LB_TAB_LIMIT_OVER` ("Over Mastery"); last child of 426 `loc_buildcard`, over the bar; `Scale` 0.705 |
| 770, 802, 834, 866 `bc_om_<i>` | `Scale` x1.1 of the old size (0.9365 in the panel); centred between the bar's gap and the bottom padding, icons 15 sharecard px from the panel's left edge |
| 0 `status01`, `CharaInfo.Powers` | the Text ref to 769 removed; `CardWriter` no longer writes the heading |

## 2026-10-05 · Master traits in the game's Master Traits list style

Script: `tools/scripts/restyle_master_traits.py`, then `tools/scripts/sync_list.py`. Styles from `skillboard_window01`'s rank lists (`loc_base`, `loc_ttl`, `skillboard_list01_*`); `gbfr.uitools` reads that prefab through new classes for `ControllerSkillBoardWindow`, `MenuSkillBoardWindowList`, `ItemSkillBoardSquare` and the other skillboard components.

| Objects | Change |
|---|---|
| `bc_mt_<s>_border` -> `bc_mt_<s>_0_base` | rank 1's panel |
| `bc_mt_<s>_<r>_base` (new for ranks 2 to EX) | `ps_sboard_list02`, sliced, `loc_base`'s colour (navy, alpha 0.502); 10 sharecard px inside the column, 5 above the rank label to 6 below the last cell |
| `bc_mt_<s>_<r>_ttl` (new) | `ps_sboard_ttl_base01` to `04` (rank 1 to EX), sliced, 31 sharecard px high, centred on the rank label, 3 inside the panel |
| `bc_mt_<s>_<r>_<c>_fill` -> `_base` | `ps_sboard_list02`, sliced, `base01`'s colour (purple, alpha 0.6) |
| `bc_mt_<s>_<r>_<c>_frame` (new) | `ps_sboard_list01`, the cell less 6 game units each side |
| `_label`, `_count` | `ttl_text01`'s colour (white, alpha 0.698) |
| `_on`, `_off` | `text01`'s and `text02`'s colours; `fot_skipstd_b_sdf_ds01` material, `ld_skipstd_b_sdf_ds01` language data; left edge 7 sharecard px further in, past the frame's corner; English `LineSpaching` 8 → 4, the line gap about the cell's top and bottom padding |
| everything after `bc_mtraits` | Ids +111 (renumbered depth-first); `CardWriter`'s Id constants follow |
| `status01.list` | `atlas/pause_skillboard` and the four title bar textures |

All new images are `Type` 1 (sliced) with `FillCenter: true`, as in the game; without it the sliced centres are left empty. Images are scaled to the cell's height over the game's (0.217), the title bars to 31 sharecard px over 74 units, so the sliced borders keep the game's proportions.

## 2026-10-05 · Master Traits menu background behind the board

Script: `tools/scripts/add_master_traits_bg.py`, then `tools/scripts/sync_list.py`. `gen_white.py` draws `bc_white` (8x8 opaque white), shipped in `ui/layouts/...` and `ui/fhd/...` like `bc_backdrop_mask`.

| Objects | Change |
|---|---|
| 464 `bc_mt_bg` (new) | first child of 463 `bc_mtraits`; `Mask` with `bc_white`, the blue section (1484x1102 sharecard px) at `Scale` 0.6066 |
| 465-493 (new) | copies of `background04`'s 2 `loc_bg` and its subtree without 26 `loc_chara01` (the character): base gradient, glows, rings, smoke, `ps_bg04_*` textures with `uiadd` materials; the 3840x2160 scene covers the section, centred |
| everything after `bc_mt_bg` | Ids +30; `CardWriter`'s Id constants follow |
| `status01.list` | `bc_white`, the `background04` textures, `ps_bg02_dissolve01`, `atlas/pause_background04` |

`background04`'s `Animator` (on its root, not copied) rotates the rings in the menu; the copies show its unanimated values.

## 2026-10-05 · Master Traits menu heading, style perks, style page titles

Script: `tools/scripts/add_master_traits_heading.py`, then `tools/scripts/sync_list.py`. `gbfr.uitools` reads `var00_frame_header02` with `AnimationType` added to `ControllerFrameHeader`.

| Objects | Change |
|---|---|
| 462 `bc_text01` (the perk summary) | removed, with its ref; everything after it Ids -1 |
| 493 `line01` (new) | copy of `var00_frame_header02`'s 4: `ps_frame_line02`, the ornament and its line (sliced), without the header's fading mask so the line crosses the board; after `bc_mt_bg`; top-left at the blue section's, the section's width, `Scale` 0.425 |
| 494 `bc_mt_perks` (new) | `HorizontalLayoutGroup` (middle-right) and `ContentSizeFitter`; `Scale` 0.2009, the names at 22.5, halfway between the style titles' (26) and the cells' (19) text size, and the stars in the style page's star to title ratio; right edge on Crux's rank panels' right edge, bottom halfway between where centring it at the header's title height puts it and the line |
| `bc_mt_perk_<s>` (new) | per style, a layout group with `bc_mt_perk_<s>_name` (`title_text01`'s components without `TextSetter`, `FontSize` 112, Text ref added) and `bc_mt_perk_<s>_stars` (copy of `skillboard_window01`'s 2578 `loc_level02` without its inactive glows: three `base`, `icon_add`, `icon`; `Scale` 1); spacings 6 and 24 card units over the row's scale. The engine ignores `ChildScaleWidth`/`ChildScaleHeight`, so only the row is scaled |
| 735 `bc_mt_heading` | `title_text01`'s components: `TextSetter` `TXT_PAU_TTL_SKL_BD` ("Master Traits"), `skill_down01` material, `FontSize` 40.8; 108.8 right of and 54.4 below the line's top-left corner, as in the header at 0.425 |
| `bc_mt_<s>_title` | `info01_text01`'s style: `ds01` material, its white to pale blue gradient; centred between the line and rank 1's panel |
| `bc_mt_<s>_style` | removed, with their refs |
| 0 `status01`, `CharaInfo.Powers` | the heading's, the style labels' and the summary's Text refs removed; plain refs to the 9 `icon_add` objects appended |
| everything after the board | Ids renumbered; `CardWriter`'s Id constants follow |

The header's scale (0.425) is the rank labels' `FontSize` (17) over the game's rank title's (40), so the title keeps the game's 96:40 ratio to them. `CardWriter` writes each style title as "<style>: <title>", as on the style pages, and the style names, no longer writes the heading, and shows a style's first n stars lit (`icon_add`, with its `icon`, active) for its n picked perks.

## 2026-10-05 · Captain master traits board

Script: `tools/scripts/add_captain_master_traits.py`. Gran's and Djeeta's EX rank has 14 cells per style instead of 10 (two more rows).

| Objects | Change |
|---|---|
| 535 `bc_mt_cells` (new) | the board's geometry, no components; holds the rank panels, title bars, cells and their texts (moved from 462 `bc_mtraits`): images of all styles, then texts of all styles |
| 944 `bc_mt_cells_captain` (new) | `Active: false`; copies of `bc_mt_cells`' objects plus EX cells 10-13 per style (base, frame, on, off, copied from the cells two before). From rank 1's panel top down, the cells, panel paddings, gaps and row gaps are shrunk by 0.8937 to fit 7 EX rows where 5 fit; the title bars and rank labels keep their height and follow their rank. Rank 1's panel top and the EX panel's bottom stay where they are |
| 531 `bc_mt_heading`, 532-534 `bc_mt_<s>_title` | now before the grids |
| 0 `status01`, `CharaInfo.Powers` | Text refs to `bc_mt_cells_captain`'s 228 texts and plain refs to both grids appended |
| everything after the board | Ids renumbered; `CardWriter`'s Id constants follow |

`CardWriter` shows `bc_mt_cells_captain` and hides `bc_mt_cells` when the character has an EX cell past position 10, and writes the shown grid's texts; the style titles are written separately.

## 2026-10-05 · Summon cells in sharecard's style

Script: `tools/scripts/restyle_summons.py`, from `summon_list01`. Per slot `bc_smn_<i>` (Ids 1532, 1583, 1634, 1685; 51 objects each, 56 before):

| Objects | Change |
|---|---|
| `base01`, `summon_icon01`, `loc_text02`, `loc_elem01` | removed; `SummonInfo` loses `Sets` and `Elements`, and `Names` keeps only `text01_01` |
| `bc_smn_band` (new) | `Mask` with `ps_cmn_list02_mask` (fades out to the right) over the band; holds `band`, a copy of `summon_list01`'s `place01_set01` (`ps_cmn_ability_base06_01`), `Scale` -1, 1, 1 so the patterned end is on the left |
| `text01_01` (summon name) | on the band; `online_text01`'s style: `fot_skipstd_b_sdf_ol09`, off-white; `FontSize` 52.031 (the status panel labels' size at the slot's scale) |
| `bc_smn_art` (new) | `Mask` with `ps_cmn_mask_list_w01` (fades at both sides), its opaque part over the cell's last third; holds a copy of `summon_list01`'s `icon01` (`SummonIconSetter` `Type` 0), 120% of the third wide, centred on it and 18 sharecard px below the cell's centre; `SummonInfo._57A2478C` points at it |
| `list_skill_p05_01` (trait row) | `Scale` 1.301: the status panel labels' size; `line01` inactive; `loc_skill_lv01` moved into `loc_text` (`Spacing` 8) inside `bc_smn_level` (new, `HorizontalLayoutGroup` lower-left, 56 high) so the level follows the name with its top on the row's centre; level `Padding` 28, 0, 40, 4 and `loc_lv01` `Spacing` 8 as in the gear trait rows |
| `list_skill_p05_02` (equip bonus row) | `line01` inactive; the level's right edge at the art's left edge |
| everything after the summons | Ids renumbered; `CardWriter`'s Id constants follow |
| `status01.list` | `fonts/fot_skipstd_b_sdf_ol09` appended (`tools/scripts/sync_list.py`) |

## 2026-10-05 · Scaffold removed

`tools/scripts/scaffold.py` removed.

| Objects | Change |
|---|---|
| 427-461 (`bc_card`, `bc_portrait`, `bc_skills`, `bc_gear`, `bc_mtraits`, `bc_om`, `bc_summons` and their edge Images) | removed: the section outlines |
| everything after them | Ids renumbered, 35 lower; `CardWriter`'s Id constants follow |

## 2026-10-05 · Ornate frame

Script: `tools/scripts/add_frame.py`. The white panel's frame line and corner ornaments (baked into `chr_status_bg01`'s `ps_cmn_base53`) are covered by the sections; this draws them again over the card.

| Objects | Change |
|---|---|
| 2539 `bc_frame` (new) | last child of 426 `loc_buildcard`, the card's rect |
| 2540-2543 `bc_frame_top`, `_bottom`, `_left`, `_right` (new) | sprite-less Images in `ps_cmn_base53`'s panel colour (245, 251, 255): the band from the card's edge to the line, 12 wide |
| 2544-2551 `bc_frame_<corner>_h`, `_v` (new) | the same colour, 30x16 and 16x30 from each corner: the band under the corner ornaments, short of their holes |
| 2552 `bc_frame_line` (new) | `ps_cmn_frame02` (sliced), `ps_cmn_base53`'s line colour (173, 199, 223); the card inset by 4, so the line lies 10 inside the card's edge as in `ps_cmn_base53` |

## 2026-10-06 · Summons panel

Script: `tools/scripts/panel_summons.py`.

| Objects | Change |
|---|---|
| 1497 `bc_smn_base` (new) | first child of 1496 `bc_smn`; the skills panel's `ps_cmn_base54` (sliced) at the skills panel's `Scale` 0.666, over the summon cells' area |
| everything after it | Ids renumbered, 1 higher; `CardWriter`'s Id constants follow |

## 2026-10-06 · Smaller master traits heading ornament

Script: `tools/scripts/shrink_heading_ornament.py`.

| Objects | Change |
|---|---|
| 458 `bc_mt_ornament_clip` (new) | `Mask` with `bc_white`, from the ornament's left edge to its tip (`ps_frame_line02` column 276) |
| 459 `line01` | moved into 458; `Scale` 0.361 (0.85 of 0.425), `SizeDelta` 336, 400 (the sprite's minimum width); its top on the master traits background's top edge |
| 460 `bc_mt_line_clip` (new) | `Mask` with `bc_white`, from the ornament's tip to the line's original right end, centred on the ornament's line |
| 461 `bc_mt_line` (new) | a copy of `line01` at the original `Scale` 0.425, its column 276 on the ornament's tip: the line at full thickness |
| `bc_mt_perks`, `bc_mt_heading` | 12.224 higher, with the line |
| everything after `line01` | Ids renumbered, 3 higher; `CardWriter`'s Id constants follow |

## 2026-10-06 · Gold gear levels

Script: `tools/scripts/gold_gear_levels.py`. The game draws a `SkillInfo` level in gold only when it is a bonus level (the summon rows); this sets the same style in the prefab, which the gear fill keeps.

| Objects | Change |
|---|---|
| `lv01_text01`, `lv01_num01` of the weapon and wrightstone trait rows; `text01_02`, `text01_03` of the sigil rows (40 texts) | gradient (255, 240, 225) to (255, 175, 115); outline material `ol07`, also first in `ContainerData` |

## 2026-10-06 · Weapon series line

Script: `tools/scripts/add_weapon_type.py`, with `equip01_info_weapon01`.

| Objects | Change |
|---|---|
| 1814 `line02` | `Image` disabled; the name stays on it |
| 1815 `loc_name01_text` | 8 higher |
| 1821 `type_text01` (new) | `equip01_info_weapon01` object 123, last child of 1814; `Scale` 0.749 (font size 19, the master traits cell texts'), 37 below the name's original centre |
| 1822, 1823 `main_image01_01`, `_02` (new) | its ornaments, `cmn_win_pop_frame03` |
| 1705 `bc_weapon` | `WeaponInfo.TypeText` points at 1821 |
| everything after 1820 | Ids renumbered, 3 higher; `CardWriter`'s Id constants follow |

## 2026-10-06 · Master traits cell styles

Script: `tools/scripts/outline_master_traits_cells.py`, with the texture `bc_outline` (`tools/scripts/gen_outline.py 20 2 8`: a 2 wide white outline with corners of radius 8, sliced with border 8; registered in `status01.list`).

| Objects | Change |
|---|---|
| `bc_mt_<s>_<r>_<c>_frame` (192) | removed |
| `bc_mt_<s>_<r>_<c>_base` (192) | the unpicked style: (36, 30, 43), the cell colour at half saturation, alpha 0.5 |
| `bc_mt_<s>_<r>_<c>_picked` (new, 192) | inactive containers with the grid's rect, after the cell bases and before the texts, in style, rank, cell order (618 in `bc_mt_cells`, 1219 in `bc_mt_cells_captain`); in `CharaInfo.Powers`, shown by `CardWriter` on picked cells |
| `_picked_base`, `_picked_outline` (new, under each) | the cell colour (39, 27, 53) at alpha 0.4 (0.7 over the unpicked base); `bc_outline` (no centre fill) in (107, 132, 155) at the cell's size, `Scale` 1 |
| everything after the first grid's cells | Ids renumbered; `CardWriter`'s Id constants follow |
