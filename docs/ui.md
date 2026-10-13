# UI

The game's UI memory the mod reads and writes on the Character Details menu's status page, shared by every section of the card.
Offsets are from the start of each structure; pointers are 8 bytes; keys are the game's 32-bit hashes of table keys,
`0x887AE0B0` being the hash of an empty string (no key, no text id).

The sections' own data is in their docs: [character](character.md), [master traits](master-traits.md),
[masteries](masteries.md), [Over Mastery](over-mastery.md), [summons](summons.md), [weapon](weapon.md),
[sigils](sigils.md) and [skills](skills.md). How the mod's parts raise events and which thread runs them is in
[runtime](runtime.md).

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
| `+0x1CC` | The object's `Id` from the file, an int32. `CardLayout.g.cs` holds the Ids of the objects the mod writes to. |
| `+0x1D0`, `+0x1D1` | Active flags, both written by `SetObjectActive`. |

`UiTree.Find` walks the tree from the object the `CharaInfo` component is on and maps every object by its `Id`.

An object reference is 0x20 bytes: vtable, the object at `+0x08`, the component at `+0x10` (0 for a reference to the
object itself), the component name hash at `+0x18`, and the file's `ObjectRefId` at `+0x1E`.

## Views

A screen is made of views, each placing a prefab, opened and closed by the view manager by name hash. A view's byte at
`+0x38` is 1 while it is open. `OpenView` and `CloseView` can leave a view as it was: a listener of the view manager can
stop either, `OpenView` skips a view already in the manager's open list and `CloseView` a view it cannot find. Other
game code also closes views without `CloseView`.

| Structure | Offset | Data |
|---|---|---|
| View | `+0x18`, `+0x20` | Prefabs: begin and end of 0x10-byte entries, the prefab at `+0x00`. |
| View | `+0x38` | Open, 1 byte. |
| View prefab | `+0x10` | The prefab's root object. |
| View prefab | `+0x188` | State: 0 closed, 1 opening, 2 open, 3 closing. |

`OpenView` opens a view's prefabs at once, showing their root objects. `CloseView` clears the open byte at once and sets
each opening or open prefab to closing, playing its close animation. The prefab's own update ends the close on a later
frame, once the animation is done, hiding the root object and setting the state to 0. Hiding the root object before
then leaves the prefab closing for good. A closed view's prefab is drawn until its close ends. The FSM condition `IsDisplayView` holds while
any of the view's prefabs returns true from its vfunc `0x90`.

The Character Details menu's FSM (`system/fsm/ui/ui_pausestatus_v1_0_1_fsm_ingame.msg`) switches pages by opening and
closing views. The status page is the view `PauseStatus` (`0xF355AE9C`); Trait Details and Skill Details are
`PauseStatusSkill`, Master Traits Details is `PauseStatusSkillBoard`, and `PauseStatusBg` (`0x4BE58B5D`) holds
`chr_status_bg01`. Leaving a page closes its view, waits until `IsDisplayView` on that view fails, then opens
`PauseStatusBg`, the next page's view and `CommandFooter`; `PauseStatusBg` is already open and stays as it is. Leaving
the status page closes `PauseStatus`, returning opens it again, and a character change updates it without closing it. Closing the menu closes `PauseStatusBg` before `PauseStatus`. The menu can open on
another page, such as Master Traits Details, without opening `PauseStatus`. `chr_status_bg01` stays loaded while the
menu is closed.

The Command List (`CommandListDialog`) and the captain's menu (`GenderChangeMenu`, with `PauseCharaColor` and the
name input) open over the status page and leave `PauseStatus` open.

## Flows

A menu runs as an FSM flow, named by a hash. The FSM action `FlowStart` starts one with
`StartFlow(flow manager, flow hash, callback, param, …, 1)`, which queues a start request; `FlowEnd` jumps to
`EndFlow(flow manager, flow hash, result)`, which moves the running flow from state 1 to 2, stores the result at its
`+0x140` and drops its start requests. `FlowStart`'s abort calls `EndFlow` while its flow is running or requested. The
flow manager is a global pointer, loaded by the `mov rcx, [rip + disp32]` at the start of
`LoadSkillBoardCategoryStatus`'s vfunc 4.

| Structure | Offset | Data |
|---|---|---|
| Flow manager | `+0x40` | Flows, begin and end: entries of 0x20 bytes, the flow at `+0x18`. |
| Flow manager | `+0xD8` | Start requests, begin and end: entries of 0x48 bytes, kind (1 to start) then the flow's hash. |
| Flow | `+0x110` | State: 1 while running, 2 once ended. |
| Flow | `+0x148` | Name hash. |

The Character Details menu is the flow `PauseStatus` (`0xF355AE9C`), started by the pause menu's FSM.
`LoadSkillBoardCategoryStatus`'s vfunc 4 (`.?AVLoadSkillBoardCategoryStatus@icon@ui@@`) returns whether the flow
`PauseSkillBoard` (`0xE0458EF5`) or `PauseStatus` is running in state 1 or requested to start.

## Character Details menu

`MenuState` hooks `StartFlow`, `EndFlow`, `OpenView` and `CloseView` and keeps the menu in one state:

| State | Entered when |
|---|---|
| Closed | `EndFlow` ends `PauseStatus`. |
| Opening | `StartFlow` starts `PauseStatus` while the menu is closed. |
| On the status page | `PauseStatusBg` and `PauseStatus` are both open. |
| On another page | `PauseStatusBg` is open and `PauseStatus` is not. |
| Closing | `PauseStatusBg` closes after it opened. |

It updates the state from both views' open bytes, read through `FindView`, after each `OpenView` of `PauseStatus` or
`PauseStatusBg` and after each `CloseView` that closes `PauseStatusBg`. Closing `PauseStatus` leaves the state as it is
until the next page opens `PauseStatusBg`, which comes once `status01` is no longer drawn. It raises `Changed`
on the game thread after each change. `IsOpen` is true from Opening to Closing.

## Status background

`StatusBackground` shows `chr_status_bg01`'s card panel and lines on the status page and stock's on another page,
reading `MenuState` at each `Changed` and when `chr_status_bg01` is set up; while the menu opens, closes or
is closed it keeps the variant shown ([build.md](build.md#background-variants)). It finds `chr_status_bg01` through its `ControllerStatusBg`: the vtable's
entry 8 is a setup every controller shares, run once the prefab is built. `StatusBackground` hooks it and keeps the
controller whose vtable is `ControllerStatusBg`'s until `ControllerLifetimes` raises `Destroyed` for it.

## Controller lifetimes

`ControllerLifetimes` hooks the scalar deleting destructor, vtable entry 7, of `ControllerStatusBg`, which
`ControllerStatusGuide` and `ControllerInformationSystem` share, and of `ControllerInformationToast`. It raises
`Destroyed` with the controller on the game thread before each runs. It hooks both or neither; `StatusBackground`,
`StatusGuide` and `SavedNotice` hook nothing and keep no controller without them. It also hooks `ControllerStatus`'s
when found, which `CardView` needs to keep `status01`'s objects.

## `Text` component

| Offset | Data |
|---|---|
| `+0x040` | The shown string, an MSVC `std::string`: the characters inline when the capacity at `+0x58` is at most 15, else a pointer to them; the length at `+0x50`. |
| `+0x0A0`, `+0x0A8` | Lines: begin and end of 0x18-byte glyph vectors (begin, end, capacity), one per shown line; a glyph is 0x50 bytes. |
| `+0x188` | The text id hash the string came from. |
| `+0x18C` | The text's sub-id hash. |
| `+0x198` | Line breaking: the reflow wrap breaks lines only when it is 1 to 6. |
| `+0x1B0` | `FontSize`, a float. |
| `+0x1B4` | `CharacterSpacing`, a float. |
| `+0x1B8` | `LineSpacing`, a float. |
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

## Loaded language

A global pointer loaded by the `mov rcx, [rip + disp32]` at `SetOverMasteryLine+0x1CF` holds the loaded language's
index as a signed byte at `+0x50`; the text setters clamp it to 0–9 and set it on a `Text` at `+0x198` before its
string. `TextTable.Language` reads it.

| Index | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
|---|---|---|---|---|---|---|---|---|---|---|
| `LanguageSetter` name | `Jpn` | `Eng` | `Fra` | `Ita` | `Deu` | `Esp` | `Por` | `Chs` | `Cht` | `Kor` |

## Text wrapping

`TextService` wraps a text to a width with the `Text` component's own wrap fields. A rewrap sets an empty string before the text's own string, so the string is laid out again with the wrap fields. The reflow wrap breaks a line after its
last space that fits, or where it stops fitting when no space does, and only when `+0x198` is 1 to 6. `TextService` then splits each line still wider than the width where it stops fitting. A rewrap's suffix, from its
first word on, is kept on one line: when it starts on the line above the last, its glyphs there are moved to the last
line's start. Each UTF-8 code point is one glyph, and the last line ends in an end glyph, `2` at the glyph's `+0x4F`. A text of more
lines than its cap, two or one, is then cut to the cap: the lines from the last kept onward are joined into it, its glyphs past what fits the width with the
ellipsis are dropped, and the ellipsis's glyphs are inserted at its end. The ellipsis is `TXT_HUD_COMMUNICATION_OVER`
(`0x6895D7BB`) in the text's sub-id. `TextService` also scales a text's icons by a factor of the icon size the game first
set.

## Game functions

Functions in the game's exe, found by the byte signatures in `Signatures/granblue_fantasy_relink_er.ini`. The exe has
no symbols; the names are the signatures' keys. `GameApi` adds each signature's scan; see
[features](runtime.md#features).

| Function | Use | Effect | Doc |
|---|---|---|---|
| `FillCharacterStatus(charaInfo, chara, index)` | hooked | Fills a `CharaInfo` from a character; the mod writes the card after it. | [character](character.md) |
| `TextComponentSetText(text, string, text id hash, -1)` | called | Sets a `Text` component's string and its text id hash; it skips the layout when the string and hash are the ones shown and the text has lines. | |
| `TextLookup(text tables, text out, text id hash, sub-id hash)` | called | Reads a text id's text in the loaded language. | |
| `TextJoinLines(text)` | called | Joins a `Text` component's lines from its lines' begin onward into the first of them. | |
| `TextFitLine(text, width, line, with ellipsis)` | called | Returns how many of a line's glyphs fit in the width, leaving room for the ellipsis. | |
| `TextSplitLine(text, line index, at, last glyph)` | called | Moves a line's glyphs from `at` to the last glyph to the start of the next line, adding the line when there is none. | |
| `TextBuildGlyphs(text, string, glyphs)` | called | Builds a null-terminated string's glyphs in the `Text` component's font into a glyph vector. | |
| `TextInsertGlyphs(line, at, glyphs, count)` | called | Inserts glyphs into a line's glyph vector. | |
| `SetObjectActive(object, active)` | called | Shows or hides an object and its subtree. | |
| `FindView(view manager, out view, view hash)` | called | Looks up a view by name hash, writing 0 when there is none. | |
| `OpenView(view manager, view hash, layer, flag, reset)` | hooked | Opens a view; the FSM action `ViewOpen` calls it. | |
| `CloseView(view manager, view hash, remove, flag)` | hooked | Closes a view; the FSM action `ViewClose` calls it. | |
| `StartFlow(flow manager, flow hash, callback, param, …, 1)` | hooked | Queues a flow's start; the FSM action `FlowStart` calls it. | |
| `EndFlow(flow manager, flow hash, result)` | hooked | Ends a running flow and drops its start requests; the FSM action `FlowEnd` jumps to it. | |
| `SetSkillBoardDescription(cell component, character key, slot)` | called | Sets a master trait cell's description in the loaded language. | [master traits](master-traits.md) |
| `MasteryPercent(mastery manager, category, character key)` | called | Returns a mastery category's percentage. | [masteries](masteries.md) |
| `SetMasteryPercents(screen)` | read | Fills the Masteries screen's percentages; read for the mastery manager. | [masteries](masteries.md) |
| `SetOverMasteryLine(LimitBonusInfo, line)` | called | Fills an Over Mastery row from a `chara` Over Mastery line. | [Over Mastery](over-mastery.md) |
| `SetSummonInfo(SummonInfo, summon id)` | called | Fills a summon slot, its trait and equip bonus rows included. | [summons](summons.md) |
| `SetCharacterDetailsFooter()` | hooked | Builds the Character Details footer from its label list. | [export](export.md) |
| `UpdateShortcutInput(shortcut)` | hooked | Reads a footer shortcut's button, once per shortcut each frame. | [export](export.md) |
| `GetButtonBits(context, button, 1, mode)` | called | Returns a button's bits in the input manager; the signature matches a call of it, read for the input manager and the function. | [export](export.md) |

## Virtual functions

Functions of game classes found through the class's vtable instead of a signature. `Vtables` finds a class's vtable
through the exe's RTTI by its type name, reads the function at its vtable index and checks that its code starts with
the expected bytes; `GameApi` does this for each function below when the mod loads. The feature that needs it hooks the
function's code, so every class sharing the function runs the hook. A missing vtable is logged as
`<class> vtable not found` and code that does not match as `<class> function <index> not found`; the feature is then
disabled ([features](runtime.md#features)).

| Class | Index | Function | Doc |
|---|---|---|---|
| `ControllerStatusBg` | 8 | The setup every controller shares. | [status background](#status-background) |
| `ControllerStatusBg`, `ControllerInformationToast` | 7 | The scalar deleting destructor. | [controller lifetimes](#controller-lifetimes) |
| `ControllerStatusGuide` | 21 | `SetUpPageArrows` | [export](export.md#page-button-prompts) |
| `ControllerInformationSystem`, `ControllerInformationToast` | 35 | `ShowInfo` | [export](export.md#saved-notice) |
| `LoadWeaponParty` | 4, 5 | Whether the loader is open; collecting its list, not checked. | [weapon](weapon.md) |
