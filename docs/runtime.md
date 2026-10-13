# Runtime

How the mod's parts pass events to each other and which thread runs them.

## Events

`Dispatch.Raise` raises every event of the mod's parts: it calls the handlers one by one, and a handler that throws is
logged and skipped while the others still run. An event raised from a hooked game function returns to the game either
way.

| Event | Raised by | Thread |
|---|---|---|
| `CharacterShown` | `CharaStatusHooks`, after the game fills the card's `CharaInfo` | game |
| `Shown` | `CardSession`, after it assembles the card | game |
| `Changed` | `CharacterDetailsMenu`, after the menu's state changes | game |
| `Destroyed` | `UiControllers`, before a controller's destructor runs | game |
| `ShortcutUpdated` | `SaveCardButton`, at each `UpdateShortcutInput` | game |
| `Tick` | `GameThread`, once per presented frame | game |
| `Presenting` | `SwapChainHooks`, before each present | render |
| `Saved` | `CardFile`, after the PNG is written | worker |

| Log line | Logged |
|---|---|
| `<event> handler <class>.<method> failed: <exception>` | When a handler throws. |

## Game thread

`GameThread` runs work on the game thread. `UpdateShortcutInput` runs once per shortcut each frame
([export](export.md#input)); `GameThread.Pump`, called at each `ShortcutUpdated`, ticks at the first call after each
present that `SwapChainHooks` counts, and at every call while nothing was presented yet. A tick runs the actions
posted with `GameThread.Post` from any thread, in order, then raises `Tick`. Ticks come only while some shortcut
updates.

## Card model

`BuildCard` is the card's content: the character key, the weapon, mirage and alternate art flag, the Master Traits
board, the mastery percentages, the Over Mastery lines, the summons and the character's name. `CardSession` holds the
card last shown as `Current`. `BuildCardAssembler` builds it from the
character's build, its master trait cells, its mastery percentages and a text lookup, and uses no UI objects. It holds
the game's board rules: three styles, 4, 8, 8 and 10 slots per rank on the normal board, 4, 8, 8 and 14 on the
captain's, and budgets of 10, 10, 10 and 20 points per rank.

`tests/gbfr.qol.buildcard.Tests` tests `BuildCardAssembler` with xunit:

```
dotnet test tests/gbfr.qol.buildcard.Tests
```

## Card view

`CardView` writes a shown card to `status01`. It walks `status01`'s tree once per fill and finds its objects by Id;
when their hash differs from `CardLayout.Status01.PrefabHash` it writes nothing ([build](build.md#build)). Otherwise it
runs every binder's two phases, then reads the name `FillCharacterStatus` wrote to `name01_01`
(`CardLayout.Status.CharaName`) into `CardSession.Current`.

1. Write: each binder writes its section's objects from the card.
2. Layout: each binder wraps and joins the texts the game's fill and the Write phase set.

| Binder | Write | Layout |
|---|---|---|
| `GearBinder` | The weapon heading ([weapon](weapon.md#implementation)). | The skill names and sigil trait names ([skills](skills.md), [sigils](sigils.md)). |
| `MasterTraitsBinder` | The Master Traits board ([master traits](master-traits.md#implementation)). | |
| `MasteriesBinder` | The masteries texts ([masteries](masteries.md#implementation)). | |
| `OverMasteryBinder` | The Over Mastery rows ([Over Mastery](over-mastery.md#implementation)). | Each row's value after its name. |
| `SummonsBinder` | The summon cells ([summons](summons.md#implementation)). | |
