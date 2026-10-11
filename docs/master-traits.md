# Master traits

The build card's master traits section, `bc_mtraits` in `status01`. It shows the character's Master Traits board as the
game's Master Traits menu lays it out. The layout is built by `tools/build/steps/status01/master_traits.py`; the
contents are written at runtime by `CardContents`, `MasterTraits` and `CardWriter`. Its colours are one of two palettes,
picked by the config option `MasterTraitsPalette` ("Master Traits Palette"): Blue by default, or Original.

## Display

| Part | Shows |
|---|---|
| Background | The Master Traits menu's scene (`background04`, without the character), clipped to the section, in the palette's colours. |
| Heading | The "Master Traits" title (`TXT_PAU_TTL_SKL_BD`) over the page header's line. |
| Styles row | Each style's name and three stars, right of the title; the name is the style title's text before its colon, empty when the style has no title; a star is lit per picked perk of the style. |
| Style titles | Above each style's column, the title of the style's rank 1 perk, in the loaded language; empty when the perk has none. |
| Rank panels | Per style, one panel per rank (STYLE RANK 1, 2, 3, EX) under a title bar, with a `picked/budget` count: the picked cells of that rank over all three styles, out of 10, 10, 10 and 20. The `/budget` is at 0.8 of the picked count's font size, the two vertically centred in a right-aligned row, the budget 1 unit lower. |
| Cells | Per rank, the cells in two columns: 4/8/8/10 slots, or 4/8/8/14 on the captain's board. |

A picked cell shows its description in the bright text on its palette's picked base under an outline. An unpicked
cell shows it in the dim text at 40% opacity on the palette's unpicked base. A slot without a cell is empty.

A description is the text the Master Traits menu shows for the cell, in the loaded language, with its `{n}` values
filled in. It is wrapped to the cell's width and cut to two lines, the second ending in the game's ellipsis; its icons
are drawn at 0.7 times the game's size. Its lines are spaced at `LineSpacing` 4, and -30 through the
`LanguageSetter`'s Japanese overwrite.

## Palettes

| Part | Blue | Original |
|---|---|---|
| Scene | Each layer tinted with a stop of the [portrait gradient](#portrait-gradient), 15% toward its grey; the textured layers without a material greyed by `uimonochrome`; `vignette01` at alpha 0.2, `add01` at 0.2. | The stock scene. |
| Title bars | The bar's shape in `uialphaa`, under the EX bar's texture added in `uiadd` (mirrored on ranks 3 and EX), so each bar runs from stop 0 to a highlight halfway between stops 2 and 3; rank 3's highlight at half strength. | The stock bars. |
| Picked base | `#0b3653` (stop 0 at 75%) at 0.7. | `#1e1a3d` (deep indigo) at 0.7. |
| Unpicked base | Stop 1 at 0.6. | `#2a2a5b` (the stock bars' indigo) at 0.6. |

The picked base replaces the unpicked one: the unpicked base is hidden on picked cells.

### Portrait gradient

The blue of the portrait backdrop in `status01`, as six stops from darkest to lightest. Each stop is the mean colour of
an equal lightness band of the backdrop's blue pixels, between their 1st and 99th lightness percentiles.

| Stop | Colour |
|---|---|
| 0 | `#0f486f` |
| 1 | `#0f5a81` |
| 2 | `#14678f` |
| 3 | `#1282a5` |
| 4 | `#1491b2` |
| 5 | `#27a1b7` |

The Blue colours are in `master_traits.py`: `GRADIENT`, `SCENE_TINTS`, `SCENE_DESATURATE`, `BAR_HIGHLIGHT`,
`BAR_RAMPS` and `CELL_STYLES`. `EX_DARK` and `EX_LIGHT` are the EX texture's two ends as drawn, which the bars' colours
are solved from.

## Objects

| Object | Contents |
|---|---|
| `bc_mt_perk_{s}_name`, `bc_mt_perk_{s}_stars` | Style `s`'s name and stars. |
| `bc_mt_{s}_title` | Style `s`'s title. |
| `bc_mt_bg_{palette}` | Each palette's scene; only `bc_mt_bg_blue` active in the file. |
| `bc_mt_cells` | The 4/8/8/10-slot board; active unless the captain's board is shown. |
| `bc_mt_cells_captain` | The 4/8/8/14-slot board, inactive in the file. |
| `{board}_{palette}` | In each board, each palette's title bars and cell bases; only the blue one active in the file. |
| `bc_mt_{s}_{r}_count` | Rank `r`'s count row in style `s`'s column, in each board, holding `bc_mt_{s}_{r}_spent` and `bc_mt_{s}_{r}_budget`. |
| `bc_mt_{s}_{r}_{c}_on`, `_off` | Cell `c` of rank `r`'s picked and unpicked description; the unused one is empty. |
| `bc_mt_{s}_{r}_{c}_base` | Cell `c` of rank `r`'s unpicked base, in each palette. |
| `bc_mt_{s}_{r}_{c}_picked` | Cell `c` of rank `r`'s picked base and outline, in each palette, inactive in the file. |

`CardIds.g.cs` holds these objects' Ids: `PerkNames`, `PerkStars`, `StyleTitles`, `Scenes`, `Cells`, `CaptainCells`,
`Palettes`, `RankCounts`, `RankBudgets`, `CellOn`, `CellOff`, `CellBases` and `CellPicked`. A palette's index is its
`MasterTraitsPalette` value.

## Implementation

1. `MasterTraits.ReadCells` reads the character's cells from the skill board tables: each cell's slot, its
   `skillboard_effect` key and its title's text id. The cells are read once per character key.
2. `CardContents.ComposeMasterTraits` matches the character's skill board entries to the cells by key. An entry whose
   value is 1 is a picked cell. When two cells share an effect key, the first one read is kept.
3. A perk counts toward its style's stars; the rank 1 perk's title is the style's title. Every other cell is placed at
   its rank and position. The captain's board is shown when a picked or unpicked cell's position is past the normal
   board's slots of its rank.
4. The configured palette's scene and its group in the shown board are activated, the other palette's deactivated; an
   undefined `MasterTraitsPalette` value shows Blue. In that palette, each picked cell's picked base is shown and its
   unpicked base hidden.
5. `CardWriter` writes each cell's description with `SetSkillBoardDescription`, through a mock cell component whose
   only `Text` is the card's cell text. Before the call it sets the text's wrap width and icon size; after it, it splits
   the lines still wider than the cell and cuts the text to two lines.

## Runtime data

### Skill board entries

In the character struct (`chara`), an entry whose key is a `skillboard_effect` key is a master trait cell: its value is
1 when the cell is picked. See [character](character.md#skill-board-entries).

### Skill board tables

The game's skill board data, a global pointer loaded by the `mov r15, [rip + disp32]` at
`SetSkillBoardDescription+0x43`. Its maps are MSVC `unordered_map`s: a pointer to the sentinel node of a list holding
every node, the list size, a bucket vector and a mask. A node is the next node, the previous node, the key at `+0x10`
and the value at `+0x18`.

| Map (sentinel pointer at) | Key | Value |
|---|---|---|
| `+0x6D0` | 64-bit: character key, slot `<< 32`, layout type 4 `<< 48` | Layout id |
| `+0x320` | Layout id | Layout record; its `+0x48` is the cell's `skillboard_effect` key |
| `+0x008` | `skillboard_effect` key | Effect row; its `+0x44` is the title's text id hash (`Unk18`), `+0x48` the description's (`Unk19`) |

### Slot

A cell's slot is the `skillboard_layout` row's `Unk30`: the style × 100, plus the rank 1–3 perk's 0–2, a rank 1–3
cell's 10–39 (rank × 10 plus its 0-based position), or an EX cell's 50 plus its 0-based position (50–63).

### Master trait cell component

`SetSkillBoardDescription` sets the description on every `Text` of the cell component passed to it.

| Offset | Data |
|---|---|
| `+0x018`, `+0x030`, `+0x048` | Icon object lists: begin, end and capacity of 0x20-byte entries. |
| `+0x060` | `Text` list: begin, end and capacity of 0x20-byte entries, the `Text` component at `+0x10`. |
| `+0x078` | 1 byte; set for a perk, whose text is the effect row's `+0x4C` (`Unk20`). |
| `+0x07C`, `+0x080` | The character key and slot last set. |

### Game functions

| Function | Effect |
|---|---|
| `SetSkillBoardDescription(cell component, character key, slot)` | Sets a master trait cell's description in the loaded language, its `{n}` filled from the effect's action parts. |
| `SetObjectActive(object, active)` | Shows the picked outlines and the board in use. |
| `TextComponentSetText`, `TextLookup` | Set the style names, counts and titles. |
| `TextFitLine`, `TextSplitLine` | Split a description's lines wider than the cell; see [text wrapping](ui.md#text-wrapping). |
| `TextJoinLines`, `TextFitLine`, `TextBuildGlyphs`, `TextInsertGlyphs` | Cut a description to two lines ending in the ellipsis; see [text wrapping](ui.md#text-wrapping). |
