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

1. `CharaStatusHooks` hooks `FillCharacterStatus`. After the game fills a `CharaInfo` on `status01`, it decodes the
   character with `CharaSnapshot.Decode` and raises `CharacterShown` with the component and the build. `CardSession`
   handles it.
2. It reads the character's name from the `CharaInfo`'s first `Names` text, which the fill has just set, into the
   build's `CharaName`.
3. `CharaSnapshot.Decode` reads the character key, the origin, the weapon key, the skin key, the effect off flag, the skill board
   entries with a non-zero key, the Over Mastery lines with a non-zero value, and the summons with a non-zero id. A line
   whose value is not finite or whose level bit is not a single bit is left out.
4. `CardSession` assembles a `BuildCard` from the build with `BuildCardAssembler`, which also takes the character's
   master trait cells, mastery percentages and text lookup. It keeps the card as `Current` and raises `Shown` with the
   component and the card. `WeaponArt` ([weapon](weapon.md)) and `CardView` handle it.
5. `CardView` writes the card; see [card view](../runtime.md#card-view).

The portrait, badges, name band and status panel are filled by `FillCharacterStatus` itself, through the `CharaInfo`
component's references in the prefab file.

### Player name

`loc_name02`, the platform icon and player name `ControllerStatus` shows for another player's character, sits under
`bc_player_name`, a holder the size of `loc_base01`. `PlayerNameSection` sets the holder active when the Show Player
Name setting is on and inactive when it is off. `loc_name02` is centred under the name band, 1 card pixel below halfway
between the band's bottom and the status panel's top. It is scaled so `name01_02`'s text is 19 card pixels high.

### Portrait placement

`loc_chr` is turned 180° so `bc_portrait_mask` lies with its opaque side at the card's left edge. Its child
`bc_chr_art` turns back and scales the art; `chr_img01` and `chr_img01_mask` sit under it with their stock transforms,
in a frame the size of stock `loc_chr` (2886x2932). The scale is not on `loc_chr` itself: `status01_in01` animates
`loc_chr`'s scale from 1.2 to 1.

The game sizes and offsets the art per character:

- `CharaIconSetter` (Type 100) sets only the texture, `cmn_imgchr_{name}` in
  `layouts/common/image_chara/noatlastextures`, picked from the character's image names by the fill's variant index.
- `ControllerStatus` keeps each object in its `_5955CB0C` list (`chr_img01`, `chr_img01_mask`) with its size and
  anchor point as loaded. When the character changes, it sets a per-character size and adds a per-character y offset
  to the loaded anchor point, both in the parent's frame.

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
| `+0x058` | 4 | Skin key: the weapon whose look is shown, `0x887AE0B0` when none. | [weapon](weapon.md) |
| `+0x05C` | 1 | Weapon flags; bit `0x20` set when the weapon's effect is turned off. | [weapon](weapon.md) |
| `+0x138` to `+0x58B8` | 400 × 0x38 | Skill board entries, below. | |
| `+0x58B8` | 4 × 0x10 | Over Mastery lines. | [Over Mastery](over-mastery.md) |
| `+0x5DD8` | 4 × 0x1C | Summons. | [summons](summons.md) |
| `+0x5EA8` | 4 | Character key. | |
| `+0x5EAC` | 1 | Origin: 0 or 1 for the local player's characters, 4 or 5 for another player's (`CharaSnapshot.Remote`). | |

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
| `+0x140`, `+0x148` | `Names`: begin and end of its object references, 0x20 bytes each with the `Text` component at `+0x10`. |
| `+0x3D0`, `+0x3D8` | `Powers`: begin and end of its object references. |

`FillCharacterStatus` writes to every `Text` component referenced from `Powers` each time it fills a `CharaInfo`: the
PWR value or, on its other branch, the text id `0x4EDE20AA`.

`FillCharacterStatus` sets every `Names` text to the character's name: the text of a text id it looks up by the
character key, or for some characters a string the character holds.

The card's weapon panel, sigil rows and skill cards are filled through the `CharaInfo` references `Weapon`, `Gem` and
`Ability`, which the build points at the card's copies.
