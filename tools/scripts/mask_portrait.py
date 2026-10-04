# Usage: python mask_portrait.py <status01.prfb.yaml> <out.prfb.yaml>
# Masks the portrait with the gear screen's portrait mask, turned 180° to fade out on the right, over the card's left column.
import sys
from prefab import Prefab
from card import S

p = Prefab(sys.argv[1])

PORTRAIT = 3  # loc_chr
IMAGES = [4, 5]  # chr_img01, chr_img01_mask
MASK = "layouts/pause/pause_common/noatlastextures/ps_cmn_mask_chara02"

CARD_W, CARD_H = 3424, 1712

# sharecard pixels
FADE_START = 518

# ps_cmn_mask_chara02 texels
TEX_SIZE = 640
OPAQUE_W = 280  # opaque columns on the right
PAD_TOP = 17.076  # transparent rows on top

TURNED = (0, 0, 1, 0)

# turned, the opaque columns start at the card's left edge and the transparent rows lie below the card
left, top = -CARD_W / 2, CARD_H / 2
w = ((FADE_START - 1440) * S - left) * TEX_SIZE / OPAQUE_W
h = CARD_H * TEX_SIZE / (TEX_SIZE - PAD_TOP)
x, y = p.vec(PORTRAIT, "Position")[:2]
p.place(PORTRAIT, size=(w, h), pivot=(1 - (x - left) / w, (top - y) / h))
p.set(PORTRAIT, "Rotation", TURNED)

for image in IMAGES:
    p.set(image, "Rotation", TURNED)
    p.place(image)

start, end = p.range(PORTRAIT)
i = p.lines.index("  Active: true", start, end)
p.lines[i:i] = ["  - ComponentName: Mask", "    Component:", "      Sprite:", f"        TexturePath: {MASK}",
                f"        SpriteName: {MASK.rsplit('/', 1)[1]}", "      Offset: 0, 0", "      ChannelWeights: 0, 0, 0, 1",
                "      InvertMask: false", "      InvertOutsides: false", "      Enable: true"]

p.save(sys.argv[2])
