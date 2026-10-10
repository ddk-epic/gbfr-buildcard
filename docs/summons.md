# Summons

The build card's summons section, `bc_smn` in `status01`. It shows the character's four summon slots as cells in a
2×2 grid. The layout is built by `tools/build/steps/status01/summons.py`; the cells are filled at runtime by
`CardContents` and `CardWriter`.

## Display

Each cell, `bc_smn_0` to `bc_smn_3`, is a copy of the summon list's slot: the summon's name on its band, the summon's
art, and the summon's trait and equip bonus rows, the trait's level after its name.

`CardIds.SummonSlots` holds the four cells' Ids.

## Implementation

1. `CharaBuild.Decode` reads each slot's summon id, leaving out 0.
2. `CardContents.ComposeSummons` writes every slot, with summon id 0 for an empty one.
3. `CardWriter` calls `SetSummonInfo` with the cell's `SummonInfo` component, found by its RTTI vtable
   (`.?AVSummonInfo@component@ui@@`), and the summon id.

## Runtime data

### Summon

Four summons at `chara+0x5DD8`, 0x1C bytes each.

| Offset | Data |
|---|---|
| `+0x00` | summon key |
| `+0x04` | summon id, passed to `SetSummonInfo` |
| `+0x08` | trait key |
| `+0x0C` | equip bonus key |
| `+0x10` | trait level |
| `+0x14` | equip bonus level |
| `+0x18` | unknown |

### Game functions

| Function | Effect |
|---|---|
| `SetSummonInfo(SummonInfo, summon id)` | Fills a summon cell, its trait and equip bonus rows included. |
