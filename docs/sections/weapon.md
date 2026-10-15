# Weapon

The build card's weapon panel, `bc_weapon` in `status01`, and the weapon's art behind the portrait column. The panel
stacks the gear screen's Weapon section, the equip screen's weapon trait and wrightstone trait rows, and the
[sigils](sigils.md) under them; it is built by `tools/build/steps/status01/gear.py`. The game fills it; the mod loads
the weapon's art with `WeaponArt`.

## Display

| Part | Shows |
|---|---|
| Heading | The gear screen's Weapon heading, `TXT_PAU_ITEM_WEAPON` in `equip01_info01`'s sub-id, in the loaded language. |
| Weapon | The weapon's icon, name, `+` level, level and HP, ATK, critical rate and stun power, as the gear screen's Weapon section shows them, with the weapon's type under the name (the equip screen's `type_text01`, set through `WeaponInfo`'s `TypeText`). |
| Weapon traits | The weapon's trait rows from the equip screen, their levels at the right edge in the summon rows' gold, as are the sigils' levels. |
| Wrightstone | The wrightstone's trait rows from the equip screen, styled as the weapon traits. |
| Art | The equipped weapon's art, or its skin's when one is set; its effect off art when the effect is turned off. |
| Skin tag | The equip screen's `tag_skin01` plaque (`WeaponInfo`'s `WeaponSkinText`), shown when a skin is set. |
| Effect tag | The equip screen's `tag_eff01` (`WeaponInfo`'s `B47B9FCC`), shown when the effect is turned off. |
| Gauge | The weapon's limit break stars, or its awakening or transcendence bar (`WeaponInfo`'s `_24E49AB7`, an `ItemWeaponGauge`). |

## Implementation

The build points the `CharaInfo` reference `Weapon` at `bc_weapon` and gives its `WeaponInfo` the trait fields
(`Skills`, `PendulumSkillObj`, `PendulumSkills`, `PendulumNames`) of the equip screen's `WeaponInfo`, so
`FillCharacterStatus` fills the panel, the weapon traits and the wrightstone traits.

The heading's text, `CardLayout.Gear.WeaponTitle`, has no `TextSetter`; the text tables hold no `TXT_PAU_ITEM_WEAPON` for
the `status01` sub-id. After each character fill `GearSection` looks the text up with `TextTable` (`TextLookup`) in the sub-id
`equip01_info01` (`0xDE6482AF`) and sets it with `TextComponentSetText`.

`WeaponInfo`'s setter picks the art, the skin tag and the effect tag from the character's weapon and the skin view,
three bytes the menu flows set with `ui::fsm::action::EnableSkinView`. The pause menu clears them; the equip menu sets
the skin and effect bytes.

| Skin view byte | `EnableSkinView` field | Setter |
|---|---|---|
| Weapon skin | `enableWeaponSkin_` | Uses the skin key for the art and the skin tag when it is set. |
| Force skin tag | `forceEnableSkinTag_` | Shows the skin tag when no skin is shown. |
| Weapon effect off | `enableWeaponEffectOff_` | Uses the effect off flag for the art and the effect tag. |

While `FillCharacterStatus` fills the card's `CharaInfo`, `WeaponArt` sets the skin view as the equip menu does (weapon
skin and weapon effect off set, force skin tag clear): at `CharaStatusHooks`' `Filling`, and it puts the previous bytes
back at `Filled`.

The art is loaded by the game's `ui::icon::LoadWeaponParty`, which loads the art of each weapon in its list while the
party menu is open. `WeaponArt` hooks two of its vfuncs:

1. At each `CardSession.Shown` ([character](character.md#implementation)) `WeaponArt` sets the weapon key and the
   effect off flag to load: the card's skin key when one is set, its weapon key otherwise, and its effect off flag.
2. The hooked vfunc 4, whether the loader is open, also returns true while the Character Details menu is open
   ([ui](../ui.md#character-details-menu)) and a card weapon is set.
3. The hooked vfunc 5, which collects the list, collects the party's weapons only while the party menu is open, then
   adds the card's weapon when the list does not hold it and has room.

### Weapon gauge

`FillCharacterStatus` passes the weapon at `chara+0x50` to `WeaponInfo`'s setter, which fills the gauge only when the
weapon's inventory id is not 0. The gauge follows the weapon's upgrade status from
`WeaponUpgradeStatus(weapon manager, weapon key)`, which reads the local weapon inventory's entry with that key.

| Upgrade status | Gauge |
|---|---|
| 0, 6, 7 | Hides the stars. |
| 6, 7 | Shows the transcendence bar, filled to the transcendence level. |
| Others | Shows the awakening bar, filled to the awakening level, when the awakening level is above 0; the stars otherwise. |
| 5, 7 | Also shows the gauge's completed state. |
| 2 | The key is not in the local inventory. |

Another player's weapon has inventory id 0. While the game fills another player's character on the card,
`CharaStatusHooks` sets the id to `0xFFFFFFFF`, which no inventory weapon has, and sets it back to 0 after. During
that fill it hooks `WeaponUpgradeStatus` for that weapon's key, returning 6 when its transcendence level is above 0 and 2
otherwise.

## Runtime data

### Character

| Offset in `chara` | Size | Data |
|---|---|---|
| `+0x050` | 4 | Weapon inventory id; 0 for another player's character. |
| `+0x054` | 4 | Weapon key: the equipped weapon. |
| `+0x058` | 4 | Skin key: the weapon whose look is shown, `0x887AE0B0` when none. |
| `+0x05C` | 1 | Weapon flags; bit `0x20` set when the weapon's effect is turned off. |
| `+0x064` | 4 | Limit break stars. |
| `+0x06C` | 4 | Awakening level. |
| `+0x090` | 4 | Transcendence level. |

### Weapon art

| Structure | Offset | Data |
|---|---|---|
| Loader | `+0x40` | Number of list entries, at most 6. |
| Loader | `+0x48` | List entries, 8 bytes each: weapon key, then 1 byte set for effect off. |
| Skin view object | `+0x1112` | Force skin tag. |
| Skin view object | `+0x1113` | Weapon skin. |
| Skin view object | `+0x1114` | Weapon effect off. |

The loader's vtable is found through the exe's RTTI (`.?AVLoadWeaponParty@icon@ui@@`). The skin view object's address
is read from the `mov rax, [rip + disp32]` that starts `EnableSkinView`'s vfunc 9 (`.?AVEnableSkinView@action@fsm@ui@@`),
which copies the action's three fields to `+0x1113`, `+0x1112` and `+0x1114`.
