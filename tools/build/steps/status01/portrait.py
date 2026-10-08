# Places the portrait on sharecard's spot, masked by the gear screen's portrait mask fading out over the left column.
from model.components import mask
from steps.layout import CARD_H, CARD_W, SCREEN_CARD_RATIO, card

IMAGES = ["chr_img01", "chr_img01_mask"]
ART_H = 2932  # chr_img01
MASK = "layouts/pause/pause_common/noatlastextures/ps_cmn_mask_chara02"
TURNED = (0, 0, 1, 0)

# sharecard pixels
SEAM = 576  # the first column's right edge
BLEED = 320
OFFSET_X = 50
ART_SCALE = 1.35
FADE_START = 518

# ps_cmn_mask_chara02 texels
TEX_SIZE = 640
OPAQUE_W = 280  # opaque columns on the right
PAD_TOP = 17.076  # transparent rows on top


def apply(ctx):
    portrait = ctx.prefab("status01").at("root/loc_base01/loc_chr")
    images = [portrait.child(name) for name in IMAGES]

    x, y = card(-BLEED + (SEAM + 2 * BLEED) / 2 + OFFSET_X, CARD_H / 2)
    portrait.place(pos=(x, y))
    scale = ART_SCALE * CARD_H * SCREEN_CARD_RATIO / ART_H
    for image in images:
        image.set("Scale", (scale, scale, 1))

    # turned, the opaque columns start at the card's left edge and the transparent rows lie below the card
    left, top = -CARD_W * SCREEN_CARD_RATIO / 2, CARD_H * SCREEN_CARD_RATIO / 2
    w = (card(FADE_START, 0)[0] - left) * TEX_SIZE / OPAQUE_W
    h = CARD_H * SCREEN_CARD_RATIO * TEX_SIZE / (TEX_SIZE - PAD_TOP)
    portrait.place(size=(w, h), pivot=(1 - (x - left) / w, (top - y) / h))
    portrait.set("Rotation", TURNED)
    for image in images:
        image.set("Rotation", TURNED)
        image.place()

    i = portrait.lines.index("  Active: true")
    portrait.lines[i:i] = mask((MASK, MASK.rsplit("/", 1)[1]))
