# Usage: python relayout_gear.py <status01.prfb.yaml> <out.prfb.yaml>
# Splits the gear block's weapon row into a name line and a stats line, stacks the 12 sigil rows in one column
# below it, drops the title and its tab, then fits the narrower block to the top of the cyan section.
import sys
from prefab import Prefab

p = Prefab(sys.argv[1])
S = 3424 / 2880
PARENT_X = 640  # loc_status02's pivot in loc_base01 coordinates

LEFT, RIGHT = (600 - 1440) * S, (1356 - 1440) * S
TOP = (720 - 16) * S

LINE_H = 96  # a weapon line
EQUIP_W = 1060  # loc_equip01: icon, name and level
SIGILS = [143, 154, 165, 176, 187, 198, 209, 220, 231, 242, 253, 264]  # slots 1 to 12
SIGIL_W, SIGIL_H = 988, 68
SIGIL_X = 24  # the rows' left edge from the block's left, which lines their icons up with the weapon's
INSET = 12  # loc_status01's background inset from the frame, as at stock
PAD = 16

W = EQUIP_W + 2 * INSET
BG_H = len(SIGILS) * SIGIL_H + 24
H = PAD + 2 * LINE_H + PAD + BG_H + INSET

scale = (RIGHT - LEFT) / W
p.place(109, pos=((LEFT + RIGHT) / 2 - PARENT_X, TOP - H * scale / 2), size=(W, H))
p.set(109, "Scale", (scale, scale, 1))

p.place(110, pos=(0, 0), size=(W, H))  # chr_status02
p.place(111, pos=(0, 0))  # root
p.place(112, pos=(0, 0), size=(W, H))  # status_base01
p.replace(112, "SpriteName: ps_cmn_base52", "SpriteName: ps_cmn_base54")  # the same frame without the title tab
p.set(113, "Active", "false")  # ttl01_text01

# chr_status02_p01, the weapon: name line on top, the stats right-aligned on a second line below it
p.place(114, pos=(0, H / 2 - PAD - LINE_H / 2), size=(W, LINE_H))
p.place(115, pos=(0, 0))  # root
p.place(116, pos=(-W / 2, 0))  # loc_equip01
for stat in [128, 131, 134, 139]:  # loc_hp, loc_atk, loc_crt, loc_brk: anchored to the row's right edge
    p.place(stat, pos=(W / 2 + p.vec(stat, "AnchorPoint")[0], -LINE_H))

# loc_status01, the sigils' background: its width follows the frame
p.place(142, pos=(0, -H / 2 + INSET), size=(-2 * INSET, BG_H))
for i, sigil in enumerate(SIGILS):
    p.place(sigil, pos=(SIGIL_X + SIGIL_W / 2 - W / 2, BG_H - 13 - SIGIL_H / 2 - i * SIGIL_H))
p.set(255, "Active", "true")  # slot 11's separator, which ended the left column at stock

p.save(sys.argv[2])
