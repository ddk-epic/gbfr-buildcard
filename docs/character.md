# Character

The build card's character parts in `status01`: the portrait, the level, Master Lvl and PWR badges, the name band and
the status panel. They are the stock page's objects, moved and restyled by `tools/build/steps/status01/portrait.py` and
`status.py`, and filled by the game. The character fill is also what writes the rest of the card.

## Display

| Part | Shows |
|---|---|
| Portrait | The character's art at the card's left edge, faded out by `bc_portrait_mask` from the seam under the second column. |
| Badges | The level and Master Lvl badges at the portrait column's top left, the PWR diamond at its top right. |
| Name band | The character's name and element above the status panel. |
| Status panel | HP, ATK, critical rate and stun power in a 2×2 grid, with the [masteries](masteries.md) panel under it. |

## Implementation

1. `CharaStatusHooks` hooks `FillCharacterStatus`. After the game fills a `CharaInfo`, it raises `Filled` with the
   component and the character.
2. `CardWriter.OnFilled` finds `status01`'s objects by Id. When their count matches `CardIds.ObjectCount`, it decodes the
   character with `CharaBuild.Decode` and writes every section; otherwise it writes nothing.
3. `CharaBuild.Decode` reads the character key, the skill board entries with a non-zero key, the Over Mastery lines
   with a non-zero value, and the summons with a non-zero id. A line whose value is not finite or whose level bit is not
   a single bit is left out.

The portrait, badges, name band and status panel are filled by `FillCharacterStatus` itself, through the `CharaInfo`
component's references in the prefab file.

## Runtime data

### Character fill

`FillCharacterStatus(charaInfo, chara, index)` fills a `CharaInfo` component from a character. The mod runs after it,
with both pointers:

- `charaInfo`: the `CharaInfo` component on `status01`, the page's root object.
- `chara`: the character's build.

### Character (`chara`)

| Offset | Size | Data | Doc |
|---|---|---|---|
| `+0x054` | 4 | Weapon key: the equipped weapon. | [weapon](weapon.md) |
| `+0x058` | 4 | Mirage key: the weapon whose look is shown, `0x887AE0B0` when none. | [weapon](weapon.md) |
| `+0x05C` | 1 | Weapon flags; bit `0x20` set when the weapon's alternate art is chosen. | [weapon](weapon.md) |
| `+0x138` to `+0x58B8` | 400 × 0x38 | Skill board entries, below. | |
| `+0x58B8` | 4 × 0x10 | Over Mastery lines. | [Over Mastery](over-mastery.md) |
| `+0x5DD8` | 4 × 0x1C | Summons. | [summons](summons.md) |
| `+0x5EA8` | 4 | Character key. | |

### Skill board entries

Each 0x38-byte entry starts with a key and a 32-bit value; the value's meaning depends on the key. An entry with key 0
is unused.

| Key | Value | Doc |
|---|---|---|
| A master trait cell (`skillboard_effect` key) | 1 when the cell is picked | [master traits](master-traits.md) |
| A mastery node (`limit_bonus` key) | One bit per `LimitBonusParamIndex`, set when that step is taken | [masteries](masteries.md) |

### `CharaInfo` component

| Offset | Data |
|---|---|
| `+0x010` | The object the component is on (`status01`). |
| `+0x3D0`, `+0x3D8` | `Powers`: begin and end of its object references. |

`FillCharacterStatus` writes to every `Text` component referenced from `Powers` each time it fills a `CharaInfo`: the
PWR value or, on its other branch, the text id `0x4EDE20AA`.

The card's weapon panel, sigil rows and skill cards are filled through the `CharaInfo` references `Weapon`, `Gem` and
`Ability`, which the build points at the card's copies.
