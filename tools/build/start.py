# First build step: starts each prefab from stock and adds the card's container to status01.
from codegen import Hash, name_hash
from context import TARGETS
from model.components import rect
from steps.layout import CANVAS_H, CANVAS_W, CARD_H, CARD_W


def apply(ctx):
    for name in TARGETS:
        ctx.prefabs[name] = ctx.stock(name)
    base = ctx.prefab("status01").at("root/loc_base01")
    card = base.add(rect("loc_buildcard"))
    card.place(pos=(0, 0), size=(CARD_W, CARD_H))
    ctx.export("Canvas.Width", CANVAS_W)
    ctx.export("Canvas.Height", CANVAS_H)
    ctx.export("Card.Width", CARD_W)
    ctx.export("Card.Height", CARD_H)
    ctx.export("Card.Y", int(base.vec("Position")[1]))
    ctx.export("Status01.NameHash", Hash(name_hash(ctx.prefab("status01").root)))
