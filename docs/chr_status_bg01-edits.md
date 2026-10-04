# `chr_status_bg01` edit log

Append-only: new entries go at the bottom, existing entries are never changed. A later edit that undoes or changes an earlier one gets its own entry. Object Ids refer to the file as it was at that entry.

## 2026-10-04 · Stock import

`chr_status_bg01.prfb` from the game (v2.0.6), converted unchanged with `gbfr.uitools.exe b-convert`. 10 objects, Ids 0-9: the white panel `bg01`, the blue portrait area `bg02` and the frame lines under `loc_line`.

## 2026-10-04 · Panel fitted to the card

Script: `tools/scripts/fit_background.py`

| Objects | Change |
|---|---|
| 3 `bg01` (white panel) | `SizeDelta` 3472, 1763, `Position` 0, 72.5: the sprite's opaque part covers the card (3424x1712 at y 74) |

`AnchorPoint` and `OffsetMin`/`OffsetMax` changed to match.
