# Runtime data

The game memory the mod reads and writes on the Character Details page. Offsets are from
the start of each structure; pointers are 8 bytes; keys are the game's 32-bit hashes of table keys, `0x887AE0B0`
being the hash of an empty string (no key, no text id).

## Character fill

`FillCharacterStatus(charaInfo, chara, index)` fills a `CharaInfo` component from a character. The mod runs after it,
with both pointers:

- `charaInfo`: the `CharaInfo` component on `status01`, the page's root object.
- `chara`: the character's build, read for the master traits, masteries, Over Mastery, summons and weapon.

## Character (`chara`)

| Offset | Size | Data |
|---|---|---|
| `+0x054` | 4 | Weapon key: the equipped weapon. |
| `+0x058` | 4 | Mirage key: the weapon whose look is shown, `0x887AE0B0` when none. |
| `+0x05C` | 1 | Weapon flags; bit `0x20` set when the weapon's alternate art is chosen. |
| `+0x138` to `+0x58B8` | 400 × 0x38 | Skill board entries, below. |
| `+0x58B8` | 4 × 0x10 | Over Mastery lines, below. |
| `+0x5DD8` | 4 × 0x1C | Summons, below. |
| `+0x5EA8` | 4 | Character key. |

### Skill board entries

Each 0x38-byte entry starts with a key and a 32-bit value; the value's meaning depends on the key.

| Key | Value | Source of the key's meaning |
|---|---|---|
| A master trait cell (`skillboard_effect` key) | 1 when the cell is picked | The skill board tables' layout for the character key: the cell's slot, below |
| A mastery node (`limit_bonus` key) | One bit per `LimitBonusParamIndex`, set when that step is taken | `Data/masteries.tsv`, per character key: the section of each bit (offense, offense extension, defense, defense extension, collection, transcendence) |

The master traits board is the 4/8/8/10-slot board, or the 4/8/8/14-slot captain's board when a picked or unpicked
cell's position is past the normal board's slots. A perk's picks are counted as its style's stars, and the rank 1
perk's title is the style's title. A cell's text is what `SetSkillBoardDescription` sets for its slot.

### Over Mastery line

| Offset | Data |
|---|---|
| `+0x0` | `limit_bonus_param` key: the line's effect |
| `+0x4` | `1 << (level - 1)` |
| `+0x8` | unknown |
| `+0xC` | float value; 0 when the slot is empty |

### Summon

| Offset | Data |
|---|---|
| `+0x00` | summon key |
| `+0x04` | summon id, passed to `SetSummonInfo` |
| `+0x08` | trait key |
| `+0x0C` | equip bonus key |
| `+0x10` | trait level |
| `+0x14` | equip bonus level |
| `+0x18` | unknown |

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

### `CharaInfo` component

| Offset | Data |
|---|---|
| `+0x010` | The object the component is on (`status01`). |
| `+0x3D0`, `+0x3D8` | `Powers`: begin and end of its object references. |

An object reference is 0x20 bytes: vtable, the object at `+0x08`, the component at `+0x10` (0 for a reference to the
object itself), the component name hash at `+0x18`, and the file's `ObjectRefId` at `+0x1E`.

`FillCharacterStatus` writes to every `Text` component referenced from `Powers` each time it fills a `CharaInfo`: the
PWR value or, on its other branch, the text id `0x4EDE20AA`.

### `Text` component

| Offset | Data |
|---|---|
| `+0x040` | The shown string, an MSVC `std::string`: the characters inline when the capacity at `+0x58` is at most 15, else a pointer to them; the length at `+0x50`. |
| `+0x0A0`, `+0x0A8` | Lines: begin and end of 0x18-byte glyph vectors (begin, end, capacity), one per shown line; a glyph is 0x50 bytes. |
| `+0x188` | The text id hash the string came from. |
| `+0x18C` | The text's sub-id hash. |
| `+0x1D0` | Wrap width, an int32. |
| `+0x1D4` | Wrap mode; 1 joins the lines and wraps them again to the width. |
| `+0x1D8` | Wrap switch, 1 byte. |
| `+0x1DF` | Icon size in percent of the font size, 1 byte; sizes the icons with ids 300–368, 1300–1699, 1720–1798 and 2000–3379. |

The wrap and icon size fields apply to the strings set after them.

### Text tables

The loaded language's texts, a global pointer loaded by the `mov rdx, [rip + disp32]` at `TextComponentSetText+0x26`.
`TextLookup` reads a text from it as a pointer and a 64-bit length; the text is null-terminated. The ellipsis that ends a
cut line is `TXT_HUD_COMMUNICATION_OVER` (`0x6895D7BB`).

## Skill board tables

The game's skill board data, a global pointer loaded by the `mov r15, [rip + disp32]` at
`SetSkillBoardDescription+0x43`. Its maps are MSVC `unordered_map`s: a pointer to the sentinel node of a list holding
every node, the list size, a bucket vector and a mask. A node is the next node, the previous node, the key at `+0x10`
and the value at `+0x18`.

| Map (sentinel pointer at) | Key | Value |
|---|---|---|
| `+0x6D0` | 64-bit: character key, slot `<< 32`, layout type 4 `<< 48` | Layout id |
| `+0x320` | Layout id | Layout record; its `+0x48` is the cell's `skillboard_effect` key |
| `+0x008` | `skillboard_effect` key | Effect row; its `+0x44` is the title's text id hash (`Unk18`), `+0x48` the description's (`Unk19`) |

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

## Game functions

Functions in the game's exe, found by the byte signatures in `Signatures/granblue_fantasy_relink_er.ini`. The exe has
no symbols; the names are the signatures' keys.

| Function | Use | Effect |
|---|---|---|
| `FillCharacterStatus(charaInfo, chara, index)` | hooked | Fills a `CharaInfo` from a character; the mod writes the card after it. |
| `TextComponentSetText(text, string, text id hash, -1)` | called | Sets a `Text` component's string and its text id hash. |
| `TextLookup(text tables, text out, text id hash, sub-id hash)` | called | Reads a text id's text in the loaded language. |
| `TextJoinLines(text)` | called | Joins a `Text` component's lines from its lines' begin onward into the first of them. |
| `TextFitLine(text, width, line, with ellipsis)` | called | Returns how many of a line's glyphs fit in the width, leaving room for the ellipsis. |
| `TextBuildGlyphs(text, string, glyphs)` | called | Builds a null-terminated string's glyphs in the `Text` component's font into a glyph vector. |
| `TextInsertGlyphs(line, at, glyphs, count)` | called | Inserts glyphs into a line's glyph vector. |
| `SetSkillBoardDescription(cell component, character key, slot)` | called | Sets a master trait cell's description in the loaded language, its `{n}` filled from the effect's action parts. |
| `SetObjectActive(object, active)` | called | Shows or hides an object and its subtree. |
| `SetOverMasteryLine(LimitBonusInfo, line)` | called | Fills an Over Mastery row from a `chara` Over Mastery line. |
| `SetSummonInfo(SummonInfo, summon id)` | called | Fills a summon slot, its trait and equip bonus rows included. |

## Weapon art

The weapon's portrait art comes from `ui::icon::LoadWeaponParty`, loading the art of each weapon in its list.

| Structure | Offset | Data |
|---|---|---|
| Loader | `+0x40` | Number of list entries, at most 6. |
| Loader | `+0x48` | List entries, 8 bytes each: weapon key, then 1 byte set for the alternate art. |
| Settings object | `+0x1113` | Non-zero when mirages are shown. |
| Settings object | `+0x1114` | Non-zero when alternate weapon art is shown. |

The settings object's address is read from the `mov rcx, [rip + disp32]` at `LoadWeaponParty`'s vfunc 5 + 0x26.
`LoadSkillBoardCategoryStatus`'s vfunc 4 returns whether the Character Details page is open.
