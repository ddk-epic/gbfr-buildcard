# First build step: starts each prefab from stock and adds the card's container to status01.
from context import TARGETS
from model.components import rect
from steps.layout import CARD_H, CARD_W, SCREEN_CARD_RATIO


def apply(ctx):
    for name in TARGETS:
        ctx.prefabs[name] = ctx.stock(name)
    card = ctx.prefab("status01").at("root/loc_base01").add(rect("loc_buildcard"))
    card.place(pos=(0, 0), size=(CARD_W * SCREEN_CARD_RATIO, CARD_H * SCREEN_CARD_RATIO))
