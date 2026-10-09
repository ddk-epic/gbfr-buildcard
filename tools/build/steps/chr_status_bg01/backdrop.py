# Fits the white panel over the card and stretches the blue portrait backdrop over the parchment, clipped to it.
from model.components import mask
from steps.layout import CARD_H, CARD_W

CARD_Y = 74  # status01's loc_base01
MASK = "layouts/pause/status/noatlastextures/bc_backdrop_mask"

# ps_cmn_base53's transparent margins
MARGIN_LEFT, MARGIN_TOP, MARGIN_RIGHT, MARGIN_BOTTOM = 24, 24, 24, 27

# the parchment, in card units from the card's top-left corner
ART_LEFT, ART_RIGHT, ART_TOP, ART_BOTTOM = -12, 1124, 0, CARD_H

# ps_cmn_base_st01
TEX_W, TEX_H = 1400, 1824
PAD_LEFT, PAD_RIGHT, PAD_TOP, PAD_BOTTOM = 6, 2, 6, 6


def apply(ctx):
    panel = ctx.prefab("chr_status_bg01").at("root/loc_bg/bg01")
    backdrop = panel.child("bg02")

    pw, ph = CARD_W + MARGIN_LEFT + MARGIN_RIGHT, CARD_H + MARGIN_TOP + MARGIN_BOTTOM
    panel.place(pos=((MARGIN_RIGHT - MARGIN_LEFT) / 2, CARD_Y + (MARGIN_TOP - MARGIN_BOTTOM) / 2), size=(pw, ph))

    kx = (ART_RIGHT - ART_LEFT) / (TEX_W - PAD_LEFT - PAD_RIGHT)
    ky = (ART_BOTTOM - ART_TOP) / (TEX_H - PAD_TOP - PAD_BOTTOM)
    x = MARGIN_LEFT + ART_LEFT - PAD_LEFT * kx
    y = MARGIN_TOP + ART_TOP - PAD_TOP * ky
    backdrop.place(pos=(x - pw / 2, ph / 2 - y), size=(TEX_W * kx, TEX_H * ky))

    i = panel.lines.index("  Active: true")
    panel.lines[i:i] = mask((MASK, MASK.rsplit("/", 1)[1]))
