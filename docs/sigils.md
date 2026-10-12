# Sigils

The build card's sigils, the gear screen's Sigils section with its 12 sigil rows, under the
[weapon](weapon.md) panel in `bc_weapon`. The section is built by `tools/build/steps/status01/gear.py`, filled by the
game, and its trait names cut by the mod.

## Display

Each row shows the sigil's two traits, each with its icon and name, side by side, and the sigil's level at the right
edge in the summon rows' gold. The sigil's own icon and name are hidden. A trait name is cut to one line, ending in the
game's ellipsis when it does not fit its column.

`CardIds.SigilTraitNames` holds each row's two names' Ids, and `CardIds.SigilTraitNameWidth` their wrap width.

## Implementation

The build points the `CharaInfo` reference `Gem` at the 12 rows, so `FillCharacterStatus` fills them through each row's
`GemInfo`. Each trait's name is a `bc_trait01` or `bc_trait02` text, which the trait's `SkillInfo` fills through its
`Names` reference. The sigil icon is dropped from `GemInfo`'s `Sets`.

`CardContents` ends the writes with each trait name's rewrap, and `CardWriter` sets each trait name again as it does
the skill names, and cuts it to one line; see [text wrapping](ui.md#text-wrapping).

## Runtime data

The game fills the rows during the [character fill](character.md#character-fill). The trait names are read from and
written to the [`Text` component](ui.md#text-component); the mod reads no other sigil data.
