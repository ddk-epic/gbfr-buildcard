# UI

The game's UI memory the mod reads and writes on the Character Details page, shared by every section of the card.
Offsets are from the start of each structure; pointers are 8 bytes; keys are the game's 32-bit hashes of table keys,
`0x887AE0B0` being the hash of an empty string (no key, no text id).

The sections' own data is in their docs: [character](character.md), [master traits](master-traits.md),
[masteries](masteries.md), [Over Mastery](over-mastery.md), [summons](summons.md), [weapon](weapon.md),
[sigils](sigils.md) and [skills](skills.md).

## UI objects

A loaded prefab is a tree of `ui::Object`s, one per object in the file, `status01` at its root.

| Offset | Data |
|---|---|
| `+0x000` | vtable, the same for every object |
| `+0x010`, `+0x018` | Children: begin and end of an array of object pointers, in the file's `Children` order; both 0 on a leaf. |
| `+0x028`, `+0x030` | Components: begin and end of 0x20-byte entries, the component at `+0x18` of each. A component's first 8 bytes are its class's vtable, found through the exe's RTTI by class name (`.?AVText@component@ui@@`). |
| `+0x100` | The prefab's root object. |
| `+0x108` | The parent object. |
| `+0x1C4` | The object's `Name`, hashed with the game's string hash (`status01`: `0xD54E236E`). |
| `+0x1CC` | The object's `Id` from the file, an int32. `CardIds.g.cs` holds the Ids of the objects the mod writes to. |
| `+0x1D0`, `+0x1D1` | Active flags, both written by `SetObjectActive`. |

`ObjectTree.Find` walks the tree from the object the `CharaInfo` component is on and maps every object by its `Id`.

An object reference is 0x20 bytes: vtable, the object at `+0x08`, the component at `+0x10` (0 for a reference to the
object itself), the component name hash at `+0x18`, and the file's `ObjectRefId` at `+0x1E`.

## `Text` component

| Offset | Data |
|---|---|
| `+0x040` | The shown string, an MSVC `std::string`: the characters inline when the capacity at `+0x58` is at most 15, else a pointer to them; the length at `+0x50`. |
| `+0x0A0`, `+0x0A8` | Lines: begin and end of 0x18-byte glyph vectors (begin, end, capacity), one per shown line; a glyph is 0x50 bytes. |
| `+0x188` | The text id hash the string came from. |
| `+0x18C` | The text's sub-id hash. |
| `+0x198` | Line breaking: the reflow wrap breaks lines only when it is 1 to 6. |
| `+0x1D0` | Wrap width, an int32. |
| `+0x1D4` | Wrap mode; 1 joins the lines and wraps them again to the width. |
| `+0x1D8` | Wrap switch, 1 byte. |
| `+0x1DF` | Icon size in percent of the font size, 1 byte; sizes the icons with ids 300–368, 1300–1699, 1720–1798 and 2000–3379. |

The wrap and icon size fields apply to the strings set after them.

## Text tables

The loaded language's texts, a global pointer loaded by the `mov rdx, [rip + disp32]` at `TextComponentSetText+0x26`.
`TextLookup` reads a text from it as a pointer and a 64-bit length; the text is null-terminated. A text is found by its
text id hash and a sub-id hash, the name hash of the prefab it is shown in; a text id can have a different text per
prefab.

## Text wrapping

`TextWrap` wraps a text to a width with the `Text` component's own wrap fields. The reflow wrap breaks a line after its
last space that fits, or where it stops fitting when no space does, and only when `+0x198` is 1 to 6. `TextWrap` then splits each line still wider than the width where it stops fitting. A text of more than
two lines is then cut to two: the lines from the second onward are joined into the second, its glyphs past what fits the width with the
ellipsis are dropped, and the ellipsis's glyphs are inserted at its end. The ellipsis is `TXT_HUD_COMMUNICATION_OVER`
(`0x6895D7BB`) in the text's sub-id. `TextWrap` also scales a text's icons by a factor of the icon size the game first
set.

## Game functions

Functions in the game's exe, found by the byte signatures in `Signatures/granblue_fantasy_relink_er.ini`. The exe has
no symbols; the names are the signatures' keys.

| Function | Use | Effect | Doc |
|---|---|---|---|
| `FillCharacterStatus(charaInfo, chara, index)` | hooked | Fills a `CharaInfo` from a character; the mod writes the card after it. | [character](character.md) |
| `TextComponentSetText(text, string, text id hash, -1)` | called | Sets a `Text` component's string and its text id hash. | |
| `TextLookup(text tables, text out, text id hash, sub-id hash)` | called | Reads a text id's text in the loaded language. | |
| `TextJoinLines(text)` | called | Joins a `Text` component's lines from its lines' begin onward into the first of them. | |
| `TextFitLine(text, width, line, with ellipsis)` | called | Returns how many of a line's glyphs fit in the width, leaving room for the ellipsis. | |
| `TextSplitLine(text, line index, at, last glyph)` | called | Moves a line's glyphs from `at` to the last glyph to the start of the next line, adding the line when there is none. | |
| `TextBuildGlyphs(text, string, glyphs)` | called | Builds a null-terminated string's glyphs in the `Text` component's font into a glyph vector. | |
| `TextInsertGlyphs(line, at, glyphs, count)` | called | Inserts glyphs into a line's glyph vector. | |
| `SetObjectActive(object, active)` | called | Shows or hides an object and its subtree. | |
| `SetSkillBoardDescription(cell component, character key, slot)` | called | Sets a master trait cell's description in the loaded language. | [master traits](master-traits.md) |
| `MasteryPercent(mastery manager, category, character key)` | called | Returns a mastery category's percentage. | [masteries](masteries.md) |
| `SetMasteryPercents(screen)` | read | Fills the Masteries screen's percentages; read for the mastery manager. | [masteries](masteries.md) |
| `SetOverMasteryLine(LimitBonusInfo, line)` | called | Fills an Over Mastery row from a `chara` Over Mastery line. | [Over Mastery](over-mastery.md) |
| `SetSummonInfo(SummonInfo, summon id)` | called | Fills a summon slot, its trait and equip bonus rows included. | [summons](summons.md) |

The weapon art hooks are found through RTTI vtables instead of signatures; see [weapon](weapon.md).
