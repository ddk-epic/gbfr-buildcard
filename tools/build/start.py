# First build step: starts each prefab from stock and adds the card's container to status01.
from context import TARGETS
from model.components import rect
from steps.layout import CARD_H, CARD_W


def apply(ctx):
    for name in TARGETS:
        ctx.prefabs[name] = ctx.stock(name)
    base = ctx.prefab("status01").at("root/loc_base01")
    card = base.add(rect("loc_buildcard"))
    card.place(pos=(0, 0), size=(CARD_W, CARD_H))
    ctx.export("CardWidth", CARD_W)
    ctx.export("CardHeight", CARD_H)
    ctx.export("CardY", int(base.vec("Position")[1]))
