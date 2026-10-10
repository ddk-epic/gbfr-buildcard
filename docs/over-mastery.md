# Over Mastery

The build card's Over Mastery section, `bc_om` in `status01`. It shows the character's four Over Mastery lines as the
game's Over Mastery screen lists them. The layout is built by `tools/build/steps/status01/over_mastery.py`; the rows
are filled at runtime by `CardContents` and `CardWriter`.

## Display

Under the Over Mastery title (`TXT_PAU_LB_TAB_LIMIT_OVER`), four rows, `bc_om_0` to `bc_om_3`, copies of
`lb_ovtli02`'s row. Each shows a line's icon, name and value in ink, without the row's separator and stars. A row is
hidden when its line is empty.

`CardIds.OverMasteryRows` holds the four rows' Ids.

## Implementation

1. `CharaBuild.Decode` reads the four lines, leaving out an empty line (value 0) and an invalid one (a value that is not
   finite, or a level field that is not a single bit).
2. `CardContents.ComposeOverMastery` shows the row of each line read and hides the others.
3. `CardWriter` copies each line into memory the mod owns and calls `SetOverMasteryLine` with the row's
   `LimitBonusInfo` component, found by its RTTI vtable (`.?AVLimitBonusInfo@component@ui@@`).

## Runtime data

### Over Mastery line

Four lines at `chara+0x58B8`, 0x10 bytes each.

| Offset | Data |
|---|---|
| `+0x0` | `limit_bonus_param` key: the line's effect |
| `+0x4` | `1 << (level - 1)` |
| `+0x8` | unknown |
| `+0xC` | float value; 0 when the slot is empty |

### Game functions

| Function | Effect |
|---|---|
| `SetOverMasteryLine(LimitBonusInfo, line)` | Fills an Over Mastery row from a line. |
| `SetObjectActive(object, active)` | Shows or hides a row. |
