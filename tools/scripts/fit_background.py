# Usage: python fit_background.py <chr_status_bg01.prfb.yaml> <out.prfb.yaml>
# Resizes the white panel so its opaque part covers the card.
import sys
from prefab import Prefab

p = Prefab(sys.argv[1])

PANEL = 3  # bg01

CARD_W, CARD_H, CARD_Y = 3424, 1712, 74

# transparent margins of ps_cmn_base53
LEFT, TOP, RIGHT, BOTTOM = 24, 24, 24, 27

p.place(PANEL, pos=((RIGHT - LEFT) / 2, CARD_Y + (TOP - BOTTOM) / 2),
        size=(CARD_W + LEFT + RIGHT, CARD_H + TOP + BOTTOM))

p.save(sys.argv[2])
