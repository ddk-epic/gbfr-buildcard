# Adds an ornate frame around the card.
from model.components import image, rect
from steps.layout import CARD_H, CARD_W, place, section

SPRITE = ("atlas/pause_pause_common", "ps_cmn_frame02")
LINE_COLOR = (173 / 255, 199 / 255, 223 / 255)  # ps_cmn_base53's line
PANEL_COLOR = (245 / 255, 251 / 255, 255 / 255)  # ps_cmn_base53's panel
INSET = 4  # card units
BAND = 12  # card units
CORNER_LEN, CORNER_W = 30, 16  # card units


def apply(ctx):
    frame = section(ctx.prefab("status01").find("loc_buildcard"), "bc_frame")
    def add(name, components, x, y, w, h):
        place(frame.add(rect(name, components, pivot=(0, 1))), x, y, w, h)

    for name, x, y, w, h in (("top", 0, 0, CARD_W, BAND), ("bottom", 0, CARD_H - BAND, CARD_W, BAND),
                             ("left", 0, 0, BAND, CARD_H), ("right", CARD_W - BAND, 0, BAND, CARD_H)):
        add(f"bc_frame_{name}", image(PANEL_COLOR, 1), x, y, w, h)
    for corner, right, bottom in (("top_left", 0, 0), ("top_right", 1, 0), ("bottom_left", 0, 1),
                                  ("bottom_right", 1, 1)):
        for edge, w, h in (("h", CORNER_LEN, CORNER_W), ("v", CORNER_W, CORNER_LEN)):
            add(f"bc_frame_{corner}_{edge}", image(PANEL_COLOR, 1), right * (CARD_W - w), bottom * (CARD_H - h), w, h)
    add("bc_frame_line", image(LINE_COLOR, 1, SPRITE, sliced=True), INSET, INSET, CARD_W - 2 * INSET,
        CARD_H - 2 * INSET)
