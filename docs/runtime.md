# Runtime

How the mod's parts pass events to each other and which thread runs them.

## Source layout

| Folder | Holds |
|---|---|
| `Host/` | `Mod`, the features, the game and render threads, `SwapChainHooks` and `Dispatch`. |
| `Interop/Native/` | The exe's sections, RTTI vtables and game containers (`PeImage`, `Vtables`, `GameMemory`). |
| `Interop/Game/` | `GameApi` and UI objects and components as structs (`UiHandles`). |
| `Interop/D3D11/` | COM calls by vtable index and the backbuffer read (`Com`, `BackbufferReadback`). |
| `Game/` | Services on the game's UI: `UiTree`, `TextService`, `ControllerLifetimes`, `MenuState`, `Input`, `CharaStatusHooks`. |
| `Sources/` | What the card reads from the game: `CharaSnapshot`, `TraitTables`, `MasteryReader`, `OverMasteryParams`, `TextTable`. |
| `Model/` | `BuildCard`, `BuildCardAssembler`, `BuildCardJson`, `CardSession`. |
| `Card/` | `CardView`, its binders in `Card/Binders/`, `StatusBackground`, `WeaponArt`. |
| `Export/` | `ExportController`, `UiFrameCapture`, `UiRedraw`, `SaveCardFooter`; `StatusGuide` and `SavedNotice` in `Overlays/`, `Png` and `Resample` in `Encoders/`, `CardFile`, `SteamScreenshots` and `NoticeSink` in `Sinks/`. |
| `Generated/` | `CardLayout.g.cs` ([build](build.md)). |

## Features

`GameApi` holds every game function and vtable the mod uses as a named `Capability`: the signatures' scans are added
when the mod loads and found when the loader runs them, and the vtables and virtual functions are found at once. Each
part of the mod that calls or hooks game code is an `IFeature` that lists the capabilities it requires and the ones it
uses when found. `Features` starts once, at the first of: `GameApi.ScansFound`, raised when every signature is found,
`OnModLoaderInitialized`, and the mod's load when every signature is found by then. It starts each feature in order
whose required capabilities were all found. A feature left out does nothing; the others still start.

| Feature | Requires |
|---|---|
| `ControllerLifetimes` | `ControllerStatusBg function 7`, `ControllerInformationToast function 7`; `ControllerStatus function 7` when found |
| `UiTree` | `SetObjectActive` |
| `TextTable` | `TextComponentSetText`, `TextLookup`; `SetOverMasteryLine` when found, for the loaded language |
| `TextService` | `TextComponentSetText`, `Text vtable`; the wrapping functions when found |
| `TraitTables` | `SetSkillBoardDescription` |
| `MasteryReader` | `MasteryPercent`, `SetMasteryPercents` |
| `OverMasteryParams` | `SetOverMasteryLine` |
| `MenuState` | `StartFlow`, `EndFlow`, `FindView`, `OpenView`, `CloseView` |
| `CharaStatusHooks` | `FillCharacterStatus` |
| `WeaponArt` | `FillCharacterStatus`, `EnableSkinView function 9`, `LoadWeaponParty function 4`, `LoadWeaponParty function 5` |
| `OverMasteryBinder` | `SetOverMasteryLine`, `LimitBonusInfo vtable` |
| `SummonsBinder` | `SetSummonInfo`, `SummonInfo vtable` |
| `StatusBackground` | `ControllerStatusBg vtable`, `ControllerStatusBg function 8` and `ControllerLifetimes`' capabilities |
| `Input` | `UpdateShortcutInput`, `GetButtonBits` |
| `SaveCardFooter` | `SetCharacterDetailsFooter` |
| `StatusGuide` | `ControllerStatusGuide function 21` and `ControllerLifetimes`' capabilities |
| `NoticeSink` | `PushInfo` |
| `SavedNotice` | `ControllerLifetimes`' capabilities; each controller's `ShowInfo` (function 35) when found |
| `CardView` | `FillCharacterStatus`, `SetObjectActive`, `TextComponentSetText`, `Text vtable` |
| `SwapChainHooks` | Nothing; it finds `Present` itself ([export](export.md#frame-timing)) |
| `ExportController` | `Input`'s and `MenuState`'s capabilities and `Present`, which `SwapChainHooks` found and hooked |

| Log line | Logged |
|---|---|
| `<feature> disabled: <capabilities> not found` | At start, for each feature left out. |
| `<feature> failed to start: <exception>` | At start, when a feature throws while starting. |
| `<feature> started without <capabilities>` | At start, for each started feature with capabilities it uses when found that were not found. |

## Events

`Dispatch.Raise` raises every event of the mod's parts: it calls the handlers one by one, and a handler that throws is
logged and skipped while the others still run. An event raised from a hooked game function returns to the game either
way.

| Event | Raised by | Thread |
|---|---|---|
| `CharacterShown` | `CharaStatusHooks`, after the game fills the card's `CharaInfo` | game |
| `Shown` | `CardSession`, after it assembles the card | game |
| `Changed` | `MenuState`, after the menu's state changes | game |
| `Destroyed` | `ControllerLifetimes`, before a controller's destructor runs | game |
| `ShortcutUpdated` | `Input`, at each `UpdateShortcutInput` | game |
| `Tick` | `GameThread`, once per presented frame; `ExportController` handles it once started | game |
| `Presenting` | `SwapChainHooks` and then `RenderThread`, before each present | render |
| `Saved` | `CardFile`, after the PNG is written | worker |

| Log line | Logged |
|---|---|
| `<event> handler <class>.<method> failed: <exception>` | When a handler throws. |

## Game thread

`GameThread` runs work on the game thread. `UpdateShortcutInput` runs once per shortcut each frame
([export](export.md#input)); `GameThread.Pump`, called at each `ShortcutUpdated`, ticks at the first call after each
present that `RenderThread` raises, and at every call while nothing was presented yet. A tick runs the actions
posted with `GameThread.Post` from any thread, in order, then raises `Tick`. Ticks come only while some shortcut
updates.

## Render thread

`RenderThread` runs work on the render thread. At each `SwapChainHooks.Presenting` it runs the actions posted with
`RenderThread.Post` from any thread, in order, then raises its own `Presenting`. While `SwapChainHooks` has not hooked
`Present`, `RenderThread.Post` drops the action. `GameThread` counts the presents and
`UiFrameCapture` captures at them ([export](export.md#frame-timing)).

## Card model

`BuildCard` is the card's content: the character key, the weapon, skin and effect off flag, the Master Traits
board, the mastery percentages, the Over Mastery lines with their shown values, the summons, the character's name and the language of its texts. `CardSession` holds the
card last shown as `Current`. `BuildCardJson` serializes it with source-generated metadata, as the export's PNG holds
it ([export](export.md#output)). `BuildCardAssembler` builds it from the
character's build, its master trait cells, its mastery percentages, a text lookup and a `limit_bonus_param` lookup, and uses no UI objects. It takes
the board's shape from `CardLayout.MasterTraits`: `StyleCount` styles, `Slots` slots per rank on the normal board
and `CaptainSlots` on the captain's, as `master_traits.py` builds them. It holds the budgets of 10, 10, 10 and 20
points per rank.

`tests/gbfr.qol.buildcard.Tests` tests `BuildCardAssembler`, the prefab hash, the card's JSON and the PNG's text
chunk with xunit:

```
dotnet test tests/gbfr.qol.buildcard.Tests
```

## Card view

`CardView` writes a shown card to `status01`. At the first fill of a `status01` instance it walks the tree once and
finds its objects by Id; when their hash differs from `CardLayout.Status01.PrefabHash` it writes nothing to that
instance ([build](build.md#build)). It keeps the objects until `ControllerLifetimes` raises `Destroyed` for the
instance's `ControllerStatus`; without that destructor hooked it walks the tree at every fill. For each card it runs
every binder's two phases.

1. Write: each binder writes its section's objects from the card.
2. Layout: each binder wraps and joins the texts the game's fill and the Write phase set.

| Binder | Write | Layout |
|---|---|---|
| `GearBinder` | The weapon heading ([weapon](weapon.md#implementation)). | The skill names and sigil trait names ([skills](skills.md), [sigils](sigils.md)). |
| `MasterTraitsBinder` | The Master Traits board ([master traits](master-traits.md#implementation)). | |
| `MasteriesBinder` | The masteries texts ([masteries](masteries.md#implementation)). | |
| `OverMasteryBinder` | The Over Mastery rows ([Over Mastery](over-mastery.md#implementation)). | Each row's value after its name. |
| `SummonsBinder` | The summon cells ([summons](summons.md#implementation)). | |
