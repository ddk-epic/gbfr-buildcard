# Places the portrait on sharecard's spot, masked by bc_portrait_mask fading out from the seam under the second column.
from model.components import mask
from steps.layout import CARD_H, CARD_W, SCREEN_CARD_RATIO, card

IMAGES = ["chr_img01", "chr_img01_mask"]
ART_H = 2932  # chr_img01
MASK = "layouts/pause/status/noatlastextures/bc_portrait_mask"
TURNED = (0, 0, 1, 0)

# sharecard pixels
SEAM = 576  # the first column's right edge
BLEED = 320
OFFSET_X = 50
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
    scale = ART_SCALE * CARD_H * SCREEN_CARD_RATIO / ART_H
    for image in images:
        image.set("Scale", (scale, scale, 1))

    # turned, the opaque side starts at the card's left edge and the transparent rows lie below the card
    left, top = -CARD_W * SCREEN_CARD_RATIO / 2, CARD_H * SCREEN_CARD_RATIO / 2
    w = MASK_W * SCREEN_CARD_RATIO
    h = CARD_H * SCREEN_CARD_RATIO * TEX_SIZE / (TEX_SIZE - PAD_TOP)
    portrait.place(size=(w, h), pivot=(1 - (x - left) / w, (top - y) / h))
    portrait.set("Rotation", TURNED)
    for image in images:
        image.set("Rotation", TURNED)
        image.place()

    i = portrait.lines.index("  Active: true")
    portrait.lines[i:i] = mask((MASK, MASK.rsplit("/", 1)[1]))
