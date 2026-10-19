# Moves bg01's white panel image to a child and adds the card's panel and masked portrait backdrop beside it, hidden.
import os
import sys

from codegen import Hash
from model.components import mask, single
from model.prefab import copy
from steps.layout import CANVAS_CARD_H as CARD_H, CANVAS_CARD_W as CARD_W

sys.path.append(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "data"))
from hashing import xxhash32_custom  # noqa: E402

CARD_Y = 74  # status01's loc_base01
MASK = "layouts/pause/status/noatlastextures/bc_backdrop_mask"
STOCK_PANEL = "bc_panel_stock"
CARD_PANEL = "bc_bg01"
CARD_BACKDROP = "bc_bg02"

# ps_cmn_base53's transparent margins
MARGIN_LEFT, MARGIN_TOP, MARGIN_RIGHT, MARGIN_BOTTOM = 24, 24, 24, 27

# the parchment, in canvas units from the card's top-left corner
ART_LEFT, ART_RIGHT, ART_TOP, ART_BOTTOM = -12, 1124, 0, CARD_H

# ps_cmn_base_st01
TEX_W, TEX_H = 1400, 1824
PAD_LEFT, PAD_RIGHT, PAD_TOP, PAD_BOTTOM = 6, 2, 6, 6


def apply(ctx):
    bg01 = ctx.prefab("chr_status_bg01").at("root/loc_bg/bg01")
    panel = bg01.add(copy(bg01))
    panel.name = CARD_PANEL
    panel.set("Active", False)
    panel.keep_components("Image")
    backdrop = panel.child("bg02")
    backdrop.name = CARD_BACKDROP

    image = bg01.add(single(bg01), 0)
    image.name = STOCK_PANEL
    image.keep_components("Image")
    image.set("AnchorMin", (0, 0))
    image.set("AnchorMax", (1, 1))
    image.place(pos=(0, 0), size=(0, 0))
    bg01.keep_components("CanvasGroup")

    pw, ph = CARD_W + MARGIN_LEFT + MARGIN_RIGHT, CARD_H + MARGIN_TOP + MARGIN_BOTTOM
    cy = CARD_Y + (MARGIN_TOP - MARGIN_BOTTOM) / 2
    panel.place(pos=((MARGIN_RIGHT - MARGIN_LEFT) / 2, cy - bg01.vec("Position")[1]), size=(pw, ph))

    kx = (ART_RIGHT - ART_LEFT) / (TEX_W - PAD_LEFT - PAD_RIGHT)
    ky = (ART_BOTTOM - ART_TOP) / (TEX_H - PAD_TOP - PAD_BOTTOM)
    x = MARGIN_LEFT + ART_LEFT - PAD_LEFT * kx
    y = MARGIN_TOP + ART_TOP - PAD_TOP * ky
    backdrop.place(pos=(x - pw / 2, ph / 2 - y), size=(TEX_W * kx, TEX_H * ky))

    panel.add_component(mask((MASK, MASK.rsplit("/", 1)[1])))
    ctx.export("Background.StockPanel", Hash(xxhash32_custom(STOCK_PANEL)))
    ctx.export("Background.StockBackdrop", Hash(xxhash32_custom(bg01.child("bg02").name)))
    ctx.export("Background.CardPanel", Hash(xxhash32_custom(CARD_PANEL)))
