# Masteries

The build card's masteries panel, `bc_masteries` in `status01`, under the status panel. It shows the character's mastery
percentages as the game's Masteries screen shows them. The panel is built by `tools/build/steps/status01/status.py`;
its texts are written at runtime by `Masteries`, `CardContents` and `CardWriter`.

## Display

| Text | Shows |
|---|---|
| `bc_masteries_text` | `{Masteries} {offense}% / {defense}%` |
| `bc_collection_text` | `{Collection} {collection}% / {transcendence}%` |

The labels are the Masteries screen's texts in the loaded language: `TXT_PAU_TTL_LB` (`0xB090BB12`), the screen's
title, and `TXT_PAU_TREE_TAB_WEAPON` (`0x8278DE48`), its Collection tab. Each number is the percentage the Masteries screen shows for that category. Offense and defense run from 0 to 150%:
the base nodes fill 0–100% and the extension nodes 100–150%. Both texts are empty when the percentages are unavailable.

`CardIds.MasteryTexts` holds the two texts' Ids.

## Implementation

1. `Masteries.ReadPercents` calls `MasteryPercent` with the mastery manager for each of the four categories and the
   character key.
2. `CardContents.ComposeMasteries` formats the four numbers into the two texts' values.
3. `CardWriter` looks up each label with `TextLookup`, puts it before the value and sets the texts with
   `TextComponentSetText`.

## Runtime data

### Mastery manager

A global pointer, loaded by the `mov rcx, [rip + disp32]` at `SetMasteryPercents+0x30`. `MasteryPercent` takes it as its
first argument.

### Categories

| Category | Mastery | Table |
|---|---|---|
| 0 | Offense | `ap_tree_atk` |
| 1 | Defense | `ap_tree_def` |
| 2 | Collection | `ap_tree_wep` |
| 3 | Transcendence | `ap_tree_rebuild` |

### Game functions

| Function | Effect |
|---|---|
| `MasteryPercent(mastery manager, category, character key)` | Returns the category's percentage as the Masteries screen shows it. |
| `SetMasteryPercents(screen)` | Fills the Masteries screen's four percentages with `MasteryPercent`; read for the mastery manager only. |
