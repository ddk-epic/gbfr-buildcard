# Weapon

The build card's weapon panel, `bc_weapon` in `status01`, and the weapon's art behind the portrait column. The panel
stacks the gear screen's Weapon section, the equip screen's weapon trait and wrightstone trait rows, and the
[sigils](sigils.md) under them; it is built by `tools/build/steps/status01/gear.py`. The game fills it; the mod loads
the weapon's art with `WeaponArtHooks`.

## Display

| Part | Shows |
|---|---|
| Heading | The gear screen's Weapon heading, `TXT_PAU_ITEM_WEAPON` in `equip01_info01`'s sub-id, in the loaded language. |
| Weapon | The weapon's icon, name, `+` level, level and HP, ATK, critical rate and stun power, as the gear screen's Weapon section shows them, with the weapon's type under the name (the equip screen's `type_text01`, set through `WeaponInfo`'s `TypeText`). |
| Weapon traits | The weapon's trait rows from the equip screen, their levels at the right edge in the summon rows' gold, as are the sigils' levels. |
| Wrightstone | The wrightstone's trait rows from the equip screen, styled as the weapon traits. |
| Art | The equipped weapon's art, or the mirage's when one is set and mirages are shown in the settings; the alternate art when it is chosen and alternate art is shown in the settings. |

## Implementation

The build points the `CharaInfo` reference `Weapon` at `bc_weapon` and gives its `WeaponInfo` the trait fields
(`Skills`, `PendulumSkillObj`, `PendulumSkills`, `PendulumNames`) of the equip screen's `WeaponInfo`, so
`FillCharacterStatus` fills the panel, the weapon traits and the wrightstone traits.

The heading's text, `CardIds.WeaponTitle`, has no `TextSetter`; the text tables hold no `TXT_PAU_ITEM_WEAPON` for
the `status01` sub-id. After each character fill `CardContents` looks the text up with `TextLookup` in the sub-id
`equip01_info01` (`0xDE6482AF`) and `CardWriter` sets it with `TextComponentSetText`.

The art is loaded by the game's `ui::icon::LoadWeaponParty`, which loads the art of each weapon in its list while the
party menu is open. `WeaponArtHooks` hooks two of its vfuncs:

1. `CardWriter` calls `WeaponArtHooks.Show` after each character fill with the weapon key and the alternate art flag
   to load, chosen from the character's weapon, mirage and flags and the settings.
2. The hooked vfunc 4, whether the loader is open, also returns true while the Character Details page is open and a
   card weapon is set.
3. The hooked vfunc 5, which collects the list, collects the party's weapons only while the party menu is open, then
   adds the card's weapon when the list does not hold it and has room.

## Runtime data

### Character

| Offset in `chara` | Size | Data |
|---|---|---|
| `+0x054` | 4 | Weapon key: the equipped weapon. |
| `+0x058` | 4 | Mirage key: the weapon whose look is shown, `0x887AE0B0` when none. |
| `+0x05C` | 1 | Weapon flags; bit `0x20` set when the weapon's alternate art is chosen. |

### Weapon art

| Structure | Offset | Data |
|---|---|---|
| Loader | `+0x40` | Number of list entries, at most 6. |
| Loader | `+0x48` | List entries, 8 bytes each: weapon key, then 1 byte set for the alternate art. |
| Settings object | `+0x1113` | Non-zero when mirages are shown. |
| Settings object | `+0x1114` | Non-zero when alternate weapon art is shown. |

The loader's vtable is found through the exe's RTTI (`.?AVLoadWeaponParty@icon@ui@@`). The settings object's address
is read from the `mov rcx, [rip + disp32]` at `LoadWeaponParty`'s vfunc 5 + 0x26.

### Menus

`LoadSkillBoardCategoryStatus`'s vfunc 4 (`.?AVLoadSkillBoardCategoryStatus@icon@ui@@`) returns whether the Master
Traits menu (`PauseSkillBoard`, `0xE0458EF5`) or the Character Details menu (`PauseStatus`, `0xF355AE9C`) is open.
`WeaponArtHooks` reads the menu manager from the `mov rcx, [rip + disp32]` at its start and checks `PauseStatus` alone
the same way.

| Structure | Offset | Data |
|---|---|---|
| Menu manager | `+0x40` | Open menus, begin and end: entries of 0x20 bytes, the menu at `+0x18`. |
| Menu manager | `+0xD8` | Menu requests, begin and end: entries of 0x48 bytes, kind (1 to open) then the menu's name hash. |
| Menu | `+0x110` | 1 while open. |
| Menu | `+0x148` | Name hash. |

A menu is open when it is in the open menus with state 1, or, when it is not in them, when an open request names it.
