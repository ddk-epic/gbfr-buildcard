# Skills

The build card's skills section, `bc_skills` in `status01`. It shows the character's four skills as the Skills
screen's cards, reduced to icon, name and element tag, in a 2×2 grid. The layout is built by
`tools/build/steps/status01/skills.py`; the game fills the cards and the mod wraps their names.

## Display

Under the Skills title (`TXT_PAU_ABILITY`), each card shows the skill's icon, its name and its element tag under the
name. The cards' frames are transparent and their button prompts hidden. A name is wrapped to its card and cut to two
lines, the second ending in the game's ellipsis.

`CardIds.SkillNames` holds the four names' Ids, and `CardIds.SkillNameWidth` their wrap width.

## Implementation

The build points the `CharaInfo` reference `Ability` at the four cards, so `FillCharacterStatus` fills them through each
card's `AbilityInfo`.

After writing the other sections, `CardWriter` sets each name again: it reads the name's string and text id hash from
its `Text` component, sets the wrap width, sets the same string with `TextComponentSetText`, and cuts it to two lines;
see [text wrapping](ui.md#text-wrapping).

## Runtime data

The names are read from and written to the [`Text` component](ui.md#text-component); the mod reads no other skill
data.
