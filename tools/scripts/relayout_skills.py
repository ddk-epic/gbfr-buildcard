# Usage: python relayout_skills.py <status01.prfb.yaml> <out.prfb.yaml>
# Stacks the skills block's four cards in one column, drops the title and its tab, then fits the block to the
# green section's height with the frame filling its width. The cards keep their size: their sprite isn't sliced.
import sys
from prefab import Prefab

p = Prefab(sys.argv[1])
S = 3424 / 2880
PARENT_X = 640  # loc_status02's pivot in loc_base01 coordinates

LEFT, RIGHT = (16 - 1440) * S, (576 - 1440) * S
TOP, BOTTOM = (720 - 1142) * S, (720 - 1404) * S

CARDS = [281, 303, 325, 347]  # in CharaInfo's Ability order, slots 1 to 4
CARD_W, CARD_H = 1004, 144
GAP = 4
PAD = 4

C = len(CARDS) * CARD_H + (len(CARDS) - 1) * GAP  # the cards' column height
H = C + 2 * PAD
scale = (TOP - BOTTOM) / H
W = (RIGHT - LEFT) / scale

p.place(275, pos=((LEFT + RIGHT) / 2 - PARENT_X, (TOP + BOTTOM) / 2), size=(W, H))
p.set(275, "Scale", (scale, scale, 1))

p.place(276, pos=(0, 0), size=(W, H))  # chr_status03
p.place(277, pos=(0, H / 2))  # root
p.place(278, pos=(0, -H / 2), size=(W, H))  # status_base01
p.replace(278, "SpriteName: ps_cmn_base52", "SpriteName: ps_cmn_base54")  # the same frame without the title tab
p.set(279, "Active", "false")  # ttl01_text01
p.place(280, pos=(0, 0), size=(CARD_W, C))  # loc_status01

for i, card in enumerate(CARDS):
    p.place(card, pos=(0, C / 2 - CARD_H / 2 - i * (CARD_H + GAP)))

p.save(sys.argv[2])
