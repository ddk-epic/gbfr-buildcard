# Export

The build card saved as a 2880x1440 PNG or JPEG from the Character Details menu's status page. The footer's Save Card starts it; the mod hides
the page arrows' button prompts, redraws one frame into off-screen frame buffers with the card filling 2880x1440, and
saves the UI's frame buffer. The code is in `gbfr.qol.buildcard/Export/`, the swapchain hook in `Core/` and the
D3D11 calls in `Interop/D3D11/`.

## Output

| Item | Value |
|---|---|
| File | `Pictures/GBFR Character Build Cards/<name>_<yyyyMMdd_HHmmss>.png`, 2880x1440, 8-bit RGB, or `.jpg` with the config option `CardFormat` set to `Jpeg`. See [Formats](#formats). `<name>` is the name of the card shown at the press, as `CharaStatusHooks` read it from the `CharaInfo`'s first `Names` text, without the characters a file name can't have; without a name the file is `<yyyyMMdd_HHmmss>` and the extension. |
| Metadata | `BuildCard` (`CardFile.JsonKeyword`), a 0 byte and the JSON, in a PNG's `tEXt` chunk after the header or a JPEG's COM segment after the JFIF header: the shown card's `BuildCard` ([runtime](runtime.md#card-model)) as JSON, its non-ASCII characters escaped as `\uXXXX`. Absent when no card was shown. |
| Content | The card's rect, `CardLayout.Card.Width` x `CardLayout.Card.Height` (3528x1764) at `CardLayout.Card.Y` (74) above the centre of the 3840x2160 UI canvas (`CardLayout.Canvas`), without the page arrows' button prompts and the info notices. |
| Steam | With the config option `SteamScreenshots` ("Add Cards to Steam Screenshots", off by default), the file is also added to the game's Steam screenshots. |
| Notice | After the file is written, the photo mode's notice "A photo has been taken and saved." shows for `NoticeTiming.NoticeDuration` milliseconds. See [Saved notice](#saved-notice). |
| Log | See [Log](#log). |

## Save Card

| Part | Source |
|---|---|
| Footer entry | The row `SaveCard` in `ui/table/guide_button.msg`, written by `tools/build/tables.py` ([prefab build](assets/build.md)): button `L3` (index 13), text id `TXT_PAU_PHT_SHOOT`, color `Guide`. |
| Footer list | `SetCharacterDetailsFooter` builds the Character Details footer from a static list loaded by the `mov rbx, [rip + disp32]` at `SetCharacterDetailsFooter+0x36`: a 64-bit count, then 32-bit label hashes, room for 10. The stock list is Close, CommandList and SkillConfirm. `SaveCardFooter` appends `CardLayout.Footer.SaveCardLabel` once before the original runs. |
| Text | "Take Photo", the stock text of `TXT_PAU_PHT_SHOOT`, the photo mode shutter's prompt, in the loaded language. |
| Button | 1 on the keyboard, L3 on a controller. |

A footer button's shortcut enum is its `guide_button.msg` index minus 1; Save Card's is `CardLayout.Footer.SaveCardButton` (12).

## Guide table

The game loads `guide_button.msg` into an MSVC `unordered_map<uint, GuideData>` on the UI manager (the global at
`0x147C24720`), keyed by the label's hash. The function that sets a footer entry from its label returns without setting it when the
label is missing. There is no lookup function to hook: the map is read inline in each of its users, among them that
function, the one that refreshes an entry's color and the one that sorts the entries by button. No stock row has button `L3` with
text `TXT_PAU_PHT_SHOOT`, so the mod ships the table with the `SaveCard` row added.

| Offset | Field |
|---|---|
| `+0x1018` | Max load factor, a float. |
| `+0x1020` | List head node. |
| `+0x1028` | Node count. |
| `+0x1030` | Buckets: each the bucket's first and last node. |
| `+0x1048` | Bucket mask; a label's bucket is its hash and the mask. |
| `+0x1050` | Bucket count. |

A node is 0x140 bytes: next and previous node, the label hash at `+0x10` and a 0x128-byte `GuideData` at `+0x18`.
`GuideData` holds `label_` (`+0x08`) and `textID_` (`+0x98`) as null-terminated char arrays, and each enum field
(`button_` `+0x30`, `button2_` `+0x60`, `color_` `+0xC8`, `config_` `+0xF8`) as its index at `+0x08` and its name as a
string at `+0x10`. Loading the table frees the map's nodes with the game's allocator and rebuilds it.

## Input

`UpdateShortcutInput(shortcut)` runs on the game thread once per shortcut each frame; `Input` raises
`ShortcutUpdated` from it, which drives `GameThread`'s `Tick` ([runtime](runtime.md#game-thread)). It reads the
shortcut's button from the input manager, the global at `0x147C24180`:

| Offset | Data |
|---|---|
| `+0x0C`..`+0x1C` | Keyboard keys held. In the menu map at `+0x0C`: 1 is `0x4000`, 2 `0x2000`, 3 `0x1000`, 4 `0x800`. |
| `+0x24`.. | Keyboard keys released. |
| `+0x3C`.. | Keyboard keys pressed. |
| `+0x54`.. | Keyboard keys repeating. |
| `+0x68` | Controller buttons pressed. |
| `+0x84` | Controller buttons held; L3 is `0x1000`. |
| `+0x88` | Controller buttons triggered. |
| `+0x8C` | Controller buttons released. |
| `+0xA8` | Input context, a uint32 passed to the lookup of a button's bits. |
| `+0xB0` | Input lock, an int32; input is read when it is below 1. |

On a press, `UpdateShortcutInput` calls the shortcut's pressed callable (see [Shortcut input](#shortcut-input)).
`ControllerStatus` gives `status01`'s button 12 shortcut, Save Card's, a callable whose invoke the signature
`StatusL3Action` matches; it runs on L3 on a controller and 1 on the keyboard. `Input` hooks it: each call marks a
press and returns without running the original. The original checks the session mode at `+0xB18` of the global at
`0x147C49640` and the party list, toggles the flag at `+0xBB8` of that global, and refreshes the object at
`ControllerStatus+0x1D0`. `Input.Pressed` returns the mark and clears it. A press
counts only while the card is open and `CardFile` is not writing a card. The card is open while `MenuState` is on the
status page ([ui](ui.md#character-details-menu)); the character fill that writes the card runs before the status page's
view opens. Other menus that read the same key, such as Master Traits, do not start an export.

## Shortcut input

`ui::component::Shortcut` assigns a button. `status01` holds eight, one with `ButtonType` 12, Save Card's button.
`UpdateShortcutInput` is vfunc 1 of the Shortcut's `IUpdateable` base at `+0x68` and gets that base.

| Offset | Field |
|---|---|
| `+0x10` | The component's object. |
| `+0x20` | Input flags: `0x1` enabled, `0x2` buttons 10 and 13, `0x4` the other buttons. |
| `+0x24` | Input block, a byte. |
| `+0x70` | `ButtonType`. |
| `+0x75` | `Always`: the button is read without the input check. |
| `+0xD8` | The pressed callable: its vtable, then 0x20 bytes of storage at `+0xE0`. |
| `+0x100` | The released callable, laid out the same. |

A callable's vtable holds invoke (storage), copy, move (destination, source) and destroy (storage). The empty
callable's vtable is `0x1460E24D8`; its invoke throws, and `UpdateShortcutInput` calls no empty callable.
`ControllerStatus`'s vfunc 21 moves a callable into seven of `status01`'s shortcuts with `FUN_143348b50(shortcut,
callable)`, each holding the controller; the shortcut with `ButtonType` 16 keeps the empty one.

Without `Always`, `UpdateShortcutInput` reads the button only when `+0x24` is 0 and `+0x20` has `0x1` and the bit of
the button. The sub-menus of Character Details, such as Trait Details and Command List, turn the page's shortcuts off
with the FSM action `SetControlEnable` (`ui::fsm::action`): for each component of the listed types on the listed
layouts it sets or clears bits `0x2` and `0x4` of `+0x20`, then does the same on each child in the component's input
tree (children at `+0x28`..`+0x30`).

## Page button prompts

The page arrows' button prompts show the input bound to switching pages on the device in use. `StatusGuide` hooks
`SetUpPageArrows`, vfunc 21 of `ui::component::ControllerStatusGuide`, found through RTTI, which sets up the page
arrows' shortcuts. After it, `StatusGuide` finds the prompts, the objects named `loc_button_system01_l` (`0x6030E86D`)
and `loc_button_system01_r` (`0xFB687EBB`) under the component's object (`+0x10`). The export hides them with
`SetObjectActive` and shows them again after the capture; the arrows stay. The prompts are dropped when the controller
is destroyed ([ui](ui.md#controller-lifetimes)).

## Frame timing

`SwapChainHooks` hooks the game's present, function 0 of `gpu::SwapChainImpl` ([ui](ui.md#virtual-functions)), and
patches no DXGI code. The game makes one `SwapChainImpl` with `CreateSwapChainForHwnd` and holds its `IDXGISwapChain3`
at `+0x8`; the function calls that swapchain's `Present` with the sync interval at `+0x3c` and no flags, then resizes
the buffers when the window's size changed. Before it runs, the hook raises `Presenting` with the swapchain on the
render thread, which drives `RenderThread` ([runtime](runtime.md#render-thread)). Overlays that hook DXGI's `Present`,
such as Steam's, draw after the read, so the card holds none of them.

On a press, `ExportController` hides the overlays, the page button prompts and the info notices (`IOverlay`), and posts
a `CaptureRequest` for the card's rect at 2880x1440 to `UiFrameCapture`. `UiFrameCapture` counts the presents from the
request:

| Present after the request | Step |
|---|---|
| 1 | Nothing; the frame may predate the hidden overlays. |
| 2 | `UiRedraw.Begin`: the next frame is redrawn. |
| The one after | `UiRedraw.End`: the UI's frame buffer, or the backbuffer's rect, is handed back to the request. |

The request is then posted to the game thread, where `ExportController` shows the overlays and hands the frame to
`CardFile`. A request is started by `UiFrameCapture` or cancelled by `ExportController`, whichever comes first. When it
is not started within 1000 ms of the press, `ExportController` cancels it, shows the overlays and logs
`Card export failed: no frame was captured`.

## Game rendering

The game renders with D3D11 on one immediate context. On the status page the UI is drawn last, onto a
backbuffer-sized RGBA8 texture that holds the 3D scene, bound alone with no depth buffer; one draw then copies that
texture to the backbuffer. The UI's texture changes from frame to frame.

## Redraw

`UiRedraw` hooks the immediate context's `DrawIndexed` (12), `Draw` (13), `DrawIndexedInstanced` (20),
`DrawInstanced` (21), `OMSetRenderTargets` (33) and `OMSetRenderTargetsAndUnorderedAccessViews` (34), read from the
device of the first presented swapchain whose backbuffer is a D3D11 texture. They are turned on at present 1 after a request and off at the present that runs `UiRedraw.End`, or that drops the request when it was cancelled. Outside a redraw frame they pass through; calls on other contexts always pass through.

In the redraw frame:

1. A backbuffer-sized 2D texture bound alone through `OMSetRenderTargets`, with no depth buffer, gets a frame buffer on
   its first bind: a 2880x1440 texture of the same format with a render target view in the game's view format, cleared
   to black. At most 6 frame buffers exist; the least recently drawn to is released first.
2. After each draw to such a texture, the same draw call runs again into its frame buffer, with every viewport and
   scissor rect mapped by `x' = x * scale + offset`, then the game's render targets, viewports and scissor rects are
   bound again.
3. The texture drawn to last before the first draw to the backbuffer is the UI's; its frame buffer is read at `End` and
   all frame buffers are released.

The card's rect on the screen fills the frame buffer: `scale` is 2880 over the rect's width (equal to 1440 over its
height), and `offset` is minus the rect's corner times `scale`. The rect comes from the UI canvas fitted into the
backbuffer by the smaller of the width and height ratios, centred. At 1920x1080 the scale is 1.6327 and the offset
(-127.35, -101.22).

The frame buffer is read by copying it into a CPU-readable staging texture and mapping it. Formats R8G8B8A8 (typeless,
unorm, sRGB), B8G8R8A8 (unorm, sRGB) and R10G10B10A2 are read; the alpha is dropped.

## Backbuffer capture

When no UI frame buffer is found, `End` returns nothing and the card's rect is copied from the backbuffer at the same
present instead, then resized to 2880x1440 by `Resample` in `CardFile`: averaged when shrinking, interpolated when enlarging.

## Card file

`ExportController` keeps `CardSession.Current` at the press ([runtime](runtime.md#card-view)) and hands it to
`CardFile.Save` with the frame. `CardFile.Save` reads `CardFormat` at the press, names the file after the card's name, writes it with the card's JSON
on a worker thread, and keeps `CardFile.Saving` set on the game thread until the worker posts the write's end. After the file is
written, `CardFile` raises `Saved` on the worker thread with the file's path and size. `Notice` and
`SteamScreenshots` handle it.

## Formats

`CardFormat` ("Card File Format") is `Png` by default. Both encoders are managed code in `Encoders/`.

| Format | Encoder | Size of a card |
|---|---|---|
| `Png` | `Png`: 8-bit RGB, filter type 4 (Paeth) on every row, one zlib `IDAT` at `CompressionLevel.Optimal`. | About 3.2 MB. |
| `Jpeg` | `Jpeg`: baseline JFIF, YCbCr with 4:2:0 chroma (16x16 MCUs), the ITU T.81 Annex K quantization tables scaled to quality `CardFile.JpegQuality` (90) on libjpeg's scale, and the Annex K Huffman tables. | About 1.1 MB. |

Steam's screenshots of the game are the same kind of JPEG: libjpeg's quality 90 tables, 4:2:0, the Annex K Huffman
tables and a JFIF header. A JPEG COM segment holds at most 65533 bytes (`Jpeg.MaxCommentLength`); a longer JSON fails the
export.

## Log

Each export logs its lines to the Reloaded-II log, prefixed with `[gbfr.qol.buildcard]`.

| Line | Logged |
|---|---|
| `Redraw format <format> scale <x>x<y> offset <x>,<y> frame buffers <count> UI draws <count>` | At the present after the redraw, when the UI's frame buffer was found. |
| `Redraw no UI frame buffer, <count> frame buffers` | At the present after the redraw, when it was not; the card comes from the backbuffer. |
| `Card saved to <path>` | After the file is written. |
| `Card added to Steam` | After `Card saved to`, when `SteamScreenshots` is on and Steam took the file. |
| `Card not added to Steam` | After `Card saved to`, when `SteamScreenshots` is on and the game runs without Steam or Steam refused the file. |
| `Card export failed: <reason>` | When resizing, serializing or writing the file fails. |
| `Presenting handler UiFrameCapture.OnPresenting failed: <exception>` | When the redraw or the read throws on the render thread; the overlays are shown and nothing is saved. |
| `SwapChainHooks disabled: SwapChainImpl function 0 not found` | At start, when the game's present is not found; `ExportController` is left out and there is no export. |
| `SwapChainHooks failed to start: <exception>` | At start, when hooking the game's present throws; `ExportController` is left out and there is no export. |
| `Card export failed: no frame was captured` | When no redraw starts within 1000 ms of the press. |

The redraw line's fields:

| Field | Value |
|---|---|
| `format` | The DXGI format of the game's render target view on the UI's texture; 28 is `R8G8B8A8_UNORM`. |
| `scale` | The viewport scale, horizontal and vertical: 2880 over the card's width on the screen. |
| `offset` | The viewport offset in output pixels, moving the card's top-left corner to (0,0). |
| `frame buffers` | The frame buffers made in the redraw frame, one per backbuffer-sized texture drawn to alone. |
| `UI draws` | The draws redrawn into the UI's frame buffer. |

At 1920x1080 a redraw logs `Redraw format 28 scale 1.6326531x1.6326531 offset -127.34695,-101.224495 frame buffers 5 UI
draws 990`.

## Steam screenshots

With the config option on, `SteamScreenshots` adds each saved card on `CardFile.Saved`. It calls `SteamAPI_SteamScreenshots_v003` and `SteamAPI_ISteamScreenshots_AddScreenshotToLibrary`
from the game's `steam_api64.dll` with the file's path and size. Steam copies the file into its library,
`userdata/<account>/760/remote/881020/screenshots`.

## Saved notice

The photo mode reports a saved photo through the UI manager's info queue (`+0x310` on the UI manager). `Notice`
posts the same info to `GameThread` at `CardFile.Saved` and queues it at the next tick
([runtime](runtime.md#game-thread)).

| Part | Source |
|---|---|
| Queue push | `PushInfo(queue, type, callback, log)` builds an info of kind 2 with the type, queues it and, when `log` is set, adds it to the message log. The signature `PushInfo` matches the photo mode's call with type `0x27`: the UI manager is the global loaded by the `mov rax, [rip + disp32]` at its start, the empty callback's vtable is loaded by the `lea rax, [rip + disp32]` at `+0x6E`, and the call is at `+0x85`. |
| Callback | A 0x40-byte callable with the empty callback's vtable first and zeros after it, as the photo mode passes. |
| Text | Info type `0x27` shows `TXT_INFO_PHT_SAVE_ABLE` ("A photo has been taken and saved.", `0x6086243B`) with the camera icon. |
| Display | `ui::component::ControllerInformationSystem` and `ui::component::ControllerInformationToast` show the queued infos; both show type `0x27`. |

An info is a 0xB8-byte record: kind at `+0x00`, type at `+0x04`, text hash at `+0x08`, the callback's vtable at `+0x58`.
Fields of both controllers:

| Offset | Field |
|---|---|
| `+0x1B4` | Seconds the current info has been shown, a float; the update adds the frame time. `ControllerInformationSystem` holds it at 0 while some menus are open. |
| `+0x1D0` | Seconds an info stays, a float, 4.5 from the `ControllerInformationSystem` constructor. The info is hidden when `+0x1B4` reaches it. |
| `+0x1F8` | The current info's kind. |
| `+0x1FC` | The current info's type. |

`ShowInfo`, vfunc 35 of both controllers, sets the controller from an info. `NoticeTiming` hooks both: for an info of
kind 2 and type `0x27` within 5000 ms of the push, it sets the controller's `+0x1D0` to `NoticeDuration` in seconds, and at that controller's next
info it puts the previous value back.

Each controller's object (`+0x10`) is the shown info: `ShowInfo` activates it and sets `+0x1A8`; when the info has
finished, the controller clears `+0x1A8` and deactivates the object. On a Save Card press, `NoticeTiming` deactivates the
object of each controller that has shown an info and still has `+0x1A8` set, and of each controller that shows an info
during the capture. This covers any info, such as another player's lobby notices. After the capture it activates the
objects again whose `+0x1A8` is still set. A destroyed controller is forgotten ([ui](ui.md#controller-lifetimes)).

A shown notice never blocks Save Card, by design: a press while it shows starts the next capture at once, the capture
hides it, and the controllers queue the next notice behind it.

## Game functions

| Function | Use | Effect |
|---|---|---|
| `SetCharacterDetailsFooter()` | hooked | Builds the Character Details footer from its label list. |
| `UpdateShortcutInput(shortcut)` | hooked | Reads a footer shortcut's button, once per shortcut each frame on the game thread. |
| `StatusL3Action(callable)` | hooked | The status page's action on L3 (1 on the keyboard), the pressed callable `ControllerStatus` gives `status01`'s button 12 shortcut; replaced by Save Card. |
| `SetUpPageArrows(controller)` | hooked | Sets up the page arrows' shortcuts; vfunc 21 of `ControllerStatusGuide`. |
| `SetObjectActive(object, active)` | called | Shows or hides an object and its subtree. |
| `PushInfo(queue, type, callback, log)` | called | Queues an info of kind 2; found through the photo mode's call of it. |
| `ShowInfo(controller, info)` | hooked | Sets `ControllerInformationSystem` or `ControllerInformationToast` from an info; vfunc 35. |
