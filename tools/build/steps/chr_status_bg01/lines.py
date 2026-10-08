# Moves the decoration lines out with the card's edges, keeping their gaps to the card.
from steps.layout import CARD_H, CARD_W, SCREEN_CARD_RATIO

STOCK_W, STOCK_H = 3424, 1712  # stock card size, in loc_buildcard units

# each line, the axis it moves along and its direction
LINES = [("line01_01", 0, -1), ("line01_02", 1, -1), ("line02_01", 0, 1), ("line02_02", 1, 1)]


def apply(ctx):
    lines = ctx.prefab("chr_status_bg01").at("root/loc_bg/loc_line")
    grow = ((CARD_W * SCREEN_CARD_RATIO - STOCK_W) / 2, (CARD_H * SCREEN_CARD_RATIO - STOCK_H) / 2)
    for name, axis, sign in LINES:
        line = lines.child(name)
        pos = list(line.vec("Position")[:2])
        pos[axis] += sign * grow[axis]
        line.place(pos=pos)
