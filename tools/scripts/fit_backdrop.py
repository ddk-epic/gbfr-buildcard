# Usage: python fit_backdrop.py <chr_status_bg01.prfb.yaml> <out.prfb.yaml>
# Stretches the blue portrait backdrop over sharecard's parchment area.
import sys
from prefab import Prefab
from card import S

p = Prefab(sys.argv[1])

PANEL, BACKDROP = 3, 4  # bg01, bg02

# in bg01
CARD_LEFT, CARD_TOP = 24, 24

# sharecard pixels on the card
LEFT, RIGHT, TOP, BOTTOM = -10, 918, 0, 1440

# ps_cmn_base_st01
TEX_W, TEX_H = 1400, 1824
PAD_LEFT, PAD_RIGHT, PAD_TOP, PAD_BOTTOM = 6, 2, 6, 6

art_w, art_h = (RIGHT - LEFT) * S, (BOTTOM - TOP) * S
kx, ky = art_w / (TEX_W - PAD_LEFT - PAD_RIGHT), art_h / (TEX_H - PAD_TOP - PAD_BOTTOM)
x = CARD_LEFT + LEFT * S - PAD_LEFT * kx
y = CARD_TOP + TOP * S - PAD_TOP * ky

pw, ph = p.vec(PANEL, "SizeDelta")
p.place(BACKDROP, pos=(x - pw / 2, ph / 2 - y), size=(TEX_W * kx, TEX_H * ky))

p.save(sys.argv[2])
