# Places the portrait at the card's left edge, masked by bc_portrait_mask fading out from the seam under the second column.
from model.components import mask, rect
from steps.layout import CARD_H, CARD_W, card

IMAGES = ["chr_img01", "chr_img01_mask"]
ART = "bc_chr_art"
ART_W, ART_H = 2886, 2932  # stock loc_chr
MASK = "layouts/pause/status/noatlastextures/bc_portrait_mask"
TURNED = (0, 0, 1, 0)

# card units
SEAM = 705  # the first column's right edge
BLEED = 392
OFFSET_X = 61
ART_SCALE = 1.35
MASK_W = 1000  # bc_portrait_mask's width

# bc_portrait_mask texels
TEX_SIZE = 640
PAD_TOP = 17.076  # transparent rows on top


def apply(ctx):
    portrait = ctx.prefab("status01").at("root/loc_base01/loc_chr")
    images = [portrait.child(name) for name in IMAGES]

    x, y = card(-BLEED + (SEAM + 2 * BLEED) / 2 + OFFSET_X, CARD_H / 2)
    portrait.place(pos=(x, y))

    # turned, the opaque side starts at the card's left edge and the transparent rows lie below the card
    left, top = -CARD_W / 2, CARD_H / 2
    h = CARD_H * TEX_SIZE / (TEX_SIZE - PAD_TOP)
    portrait.place(size=(MASK_W, h), pivot=(1 - (x - left) / MASK_W, (top - y) / h))
    portrait.set("Rotation", TURNED)
    portrait.add_component(mask((MASK, MASK.rsplit("/", 1)[1])))

    # turned back and scaled, the images in stock loc_chr's frame
    art = portrait.add(rect(ART))
    art.place(pos=(0, 0), size=(ART_W, ART_H))
    art.set("Rotation", TURNED)
    scale = ART_SCALE * CARD_H / ART_H
    art.set("Scale", (scale, scale, 1))
    for image in images:
        art.add(image)
