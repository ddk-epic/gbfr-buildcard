# Sigils

The build card's sigils, the gear screen's Sigils section with its 12 sigil rows, under the
[weapon](weapon.md) panel in `bc_weapon`. The section is built by `tools/build/steps/status01/gear.py` and filled by the
game.

## Display

Each row shows the sigil's two traits, each with its icon and name, side by side, and the sigil's level at the right
edge in the summon rows' gold. The sigil's own icon and name are hidden.

## Implementation

The build points the `CharaInfo` reference `Gem` at the 12 rows, so `FillCharacterStatus` fills them through each row's
`GemInfo`. Each trait's name is a `bc_trait01` or `bc_trait02` text, which the trait's `SkillInfo` fills through its
`Names` reference. The sigil icon is dropped from `GemInfo`'s `Sets`.

## Runtime data

The mod reads and writes no sigil data; the game fills the rows during the [character fill](character.md#character-fill).
