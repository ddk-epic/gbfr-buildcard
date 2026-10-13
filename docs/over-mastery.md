# Over Mastery

The build card's Over Mastery section, `bc_om` in `status01`. It shows the character's four Over Mastery lines as the
game's Over Mastery screen lists them. The layout is built by `tools/build/steps/status01/over_mastery.py`; the rows
are filled at runtime by `OverMasteryBinder`.

## Display

Under the Over Mastery title (`TXT_PAU_LB_TAB_LIMIT_OVER`), four rows, `bc_om_0` to `bc_om_3`, copies of
`lb_ovtli02`'s row. Each shows a line's icon, name and value in ink, without the row's separator and stars. A row is
hidden when its line is empty or was not filled. The value follows the name in the name's text, as `+`, the number and the percent sign
when the game shows one; the row's own plus icon, number and percent sign are hidden. The value stays on one line. The text is wrapped to the panel's
content width and cut to two lines, the second ending in the game's ellipsis; the two lines are centred on the row. Its lines are spaced at
`LineSpacing` -6, and -40 through the `LanguageSetter`'s Japanese overwrite.

`CardLayout.OverMastery.Rows` holds the four rows' Ids, `Names` their names' Ids, `Values` each row's plus icon,
number and percent sign Ids, and `NameWidth` the names' wrap width.

## Implementation

1. `CharaSnapshot.Decode` reads the four lines, leaving out an empty line (value 0) and an invalid one (a value that is not
   finite, or a level field that is not a single bit).
2. `BuildCardAssembler` turns each line into an `OverMasteryRow` with the value `SetOverMasteryLine` shows: the line's
   value times its `limit_bonus_param` row's scale, truncated to an integer, and whether the row shows a percent sign.
   `OverMasteryParams` reads the rows. A line whose key has no row is left out.
3. In the Write phase, `OverMasteryBinder` copies each line into memory the mod owns and calls `SetOverMasteryLine`
   with the row's `LimitBonusInfo` component, found by its RTTI vtable (`.?AVLimitBonusInfo@component@ui@@`). It shows
   the row of each line it filled and hides the others.
4. In the Layout phase, for each row it filled, `OverMasteryBinder` sets the name again with ` +`, the value and, for a
   percentage, the text of `TXT_PAU_PAR` (`CardLayout.OverMastery.PercentText`, `percent01`'s text) appended, as it does
   the skill names, wrapped to the width and cut to two lines, and hides the value's objects, the plus icon included;
   see [text wrapping](ui.md#text-wrapping).

## Runtime data

### Over Mastery line

Four lines at `chara+0x58B8`, 0x10 bytes each.

| Offset | Data |
|---|---|
| `+0x0` | `limit_bonus_param` key: the line's effect |
| `+0x4` | `1 << (level - 1)` |
| `+0x8` | unknown |
| `+0xC` | float value; 0 when the slot is empty |

### `limit_bonus_param` rows

`SetOverMasteryLine` loads the param tables with `mov rdx, [rip + disp32]` at `+0x2C`. Their MSVC unordered map at
`+0x88` maps a `limit_bonus_param` key to its row.

| Offset | Data |
|---|---|
| `+0x40` | The name's text id. |
| `+0x4C` | Scale, a 32-bit integer: the shown value is the line's value times it, truncated. |
| `+0x50` | 1 when the value is a percentage: `percent01` is shown. |

### Game functions

| Function | Effect |
|---|---|
| `SetOverMasteryLine(LimitBonusInfo, line)` | Fills an Over Mastery row from a line: finds the line's `limit_bonus_param` row, sets the name, writes the value as a decimal integer to `num01`, and shows `percent01` for a percentage. Without a row it fills nothing. |
| `SetObjectActive(object, active)` | Shows or hides a row. |
