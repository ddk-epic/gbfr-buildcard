# `chr_status_bg01` edit log

**Frozen.** This log ends at commit 5f5f456; the prefab is now written by `tools/build/build.py`, whose steps replace these entries (see [build.md](build.md)).

Append-only: new entries go at the bottom, existing entries are never changed. A later edit that undoes or changes an earlier one gets its own entry. Object Ids refer to the file as it was at that entry.

## 2026-10-04 · Stock import

`chr_status_bg01.prfb` from the game (v2.0.6), converted unchanged with `gbfr.uitools.exe b-convert`. 10 objects, Ids 0-9: the white panel `bg01`, the blue portrait area `bg02` and the frame lines under `loc_line`.

## 2026-10-04 · Panel fitted to the card

Script: `tools/scripts/fit_background.py`

| Objects | Change |
|---|---|
| 3 `bg01` (white panel) | `SizeDelta` 3472, 1763, `Position` 0, 72.5: the sprite's opaque part covers the card (3424x1712 at y 74) |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match.

## 2026-10-04 · Backdrop cut like sharecard's parchment

Scripts: `tools/scripts/fit_backdrop.py`, then `tools/scripts/mask_backdrop.py`; mask texture: `tools/scripts/gen_backdrop_mask.py`

| Objects | Change |
|---|---|
| 4 `bg02` (blue backdrop) | `SizeDelta` 1109.63, 1723.338, `AnchorPoint` 7.356, -18.331: the art spans sharecard's card x -10 to 918 at the card's height |
| 3 `bg01` | `Mask` added, sprite `layouts/pause/status/noatlastextures/bc_backdrop_mask`: sharecard's parchment cut, the diagonal body edge and the spike |
| `chr_status_bg01.list` | stock list added, `bc_backdrop_mask` appended (`tools/scripts/sync_list.py`) |

`bc_backdrop_mask` is a 2048x1024 BC7 texture covering `bg01`'s rect, shipped in `ui/layouts/...` and `ui/fhd/...`: the game reads the `fhd` copy at 1080p, without falling back to the other.
