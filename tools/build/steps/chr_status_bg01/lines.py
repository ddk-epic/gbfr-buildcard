# Adds the card's copy of the decoration lines, each vertical line as far from the card as its horizontal one, hidden.
from model.prefab import copy
from steps.chr_status_bg01.backdrop import CARD_Y, xxhash32_custom
from steps.layout import CARD_H, CARD_W

STOCK_H = 1712  # stock card height, in loc_buildcard units
CARD_LINES = "bc_loc_line"

# each corner's vertical and horizontal line and its side: -1 bottom left, 1 top right
CORNERS = [("line01_01", "line01_02", -1), ("line02_01", "line02_02", 1)]


def apply(ctx):
    stock = ctx.prefab("chr_status_bg01").at("root/loc_bg/loc_line")
    lines = stock.parent.add(copy(stock), stock.parent.children.index(stock) + 1)
    lines.name = CARD_LINES
    lines.set("Active", False)
    for vertical, horizontal, side in CORNERS:
        x, y = lines.child(horizontal).vec("Position")[:2]
        y += side * (CARD_H - STOCK_H) / 2
        lines.child(horizontal).place(pos=(x, y))
        gap = side * (y - CARD_Y) - CARD_H / 2
        line = lines.child(vertical)
        line.place(pos=(side * (CARD_W / 2 + gap), line.vec("Position")[1]))
    ctx.export("StockLines", xxhash32_custom(stock.name))
    ctx.export("CardLines", xxhash32_custom(CARD_LINES))
