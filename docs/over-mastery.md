# Over Mastery

The build card's Over Mastery section, `bc_om` in `status01`. It shows the character's four Over Mastery lines as the
game's Over Mastery screen lists them. The layout is built by `tools/build/steps/status01/over_mastery.py`; the rows
are filled at runtime by `OverMasteryBinder`.

## Display

Under the Over Mastery title (`TXT_PAU_LB_TAB_LIMIT_OVER`), four rows, `bc_om_0` to `bc_om_3`, copies of
`lb_ovtli02`'s row. Each shows a line's icon, name and value in ink, without the row's separator and stars. A row is
hidden when its line is empty. The value follows the name in the name's text, as `+`, the number and the percent sign
when the game shows one; the row's own plus icon, number and percent sign are hidden. The text is wrapped to the panel's
content width and cut to two lines, the second ending in the game's ellipsis; the two lines are centred on the row. Its lines are spaced at
`LineSpacing` -6, and -40 through the `LanguageSetter`'s Japanese overwrite.

`CardLayout.OverMastery.Rows` holds the four rows' Ids, `Names` their names' Ids, `Values` each row's plus icon,
number and percent sign Ids, and `NameWidth` the names' wrap width.

## Implementation

1. `CharaSnapshot.Decode` reads the four lines, leaving out an empty line (value 0) and an invalid one (a value that is not
   finite, or a level field that is not a single bit).
2. In the Write phase, `OverMasteryBinder` copies each line into memory the mod owns and calls `SetOverMasteryLine`
   with the row's `LimitBonusInfo` component, found by its RTTI vtable (`.?AVLimitBonusInfo@component@ui@@`). It shows
   the row of each line read and hides the others.
3. In the Layout phase, for each shown row, `OverMasteryBinder` reads the row's number and, when its object is active,
   its percent sign, sets the name again with ` +` and them appended as it does the skill names, wrapped to the width and cut to two lines, and hides the value's objects, the plus icon included; see
   [text wrapping](ui.md#text-wrapping).

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
