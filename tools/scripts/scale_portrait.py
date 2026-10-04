# Usage: python scale_portrait.py <status01.prfb.yaml> <out.prfb.yaml>
# Scales and moves the portrait to its place on sharecard's card.
import sys
from prefab import Prefab
from card import S, card

p = Prefab(sys.argv[1])

PORTRAIT = 3  # loc_chr
IMAGES = [4, 5]  # chr_img01, chr_img01_mask (with the colour overlay 6)
ART_H = 2932  # chr_img01

# sharecard pixels
SEAM = 576  # the first column's right edge
BLEED = 320
OFFSET_X = 50
ART_SCALE = 1.35

x, y = card(-BLEED + (SEAM + 2 * BLEED) / 2 + OFFSET_X, 720)
p.place(PORTRAIT, pos=(x, y))
scale = ART_SCALE * 1440 * S / ART_H
for image in IMAGES:
    p.set(image, "Scale", (scale, scale, 1))

p.save(sys.argv[2])
