# Usage: python relayout_status.py <status01.prfb.yaml> <out.prfb.yaml>
# Stacks the status block's four stat rows in one column at the bottom of the yellow section.
import sys
from prefab import Prefab

p = Prefab(sys.argv[1])
S = 3424 / 2880
PARENT_X = 640  # loc_status02's pivot in loc_base01 coordinates

LEFT, RIGHT = (16 - 1440) * S, (576 - 1440) * S
BOTTOM = (720 - 1118) * S

ROW_W, ROW_H = 736, 64
ROWS_X = 280  # the rows' left edge from the block's left
PAD = 40

W = 1000
H = 4 * ROW_H + 2 * PAD

scale = (RIGHT - LEFT) / W
p.place(73, pos=((LEFT + RIGHT) / 2 - PARENT_X, BOTTOM + H * scale / 2), size=(W, H))
p.set(73, "Scale", (scale, scale, 1))

p.place(74, pos=(0, 0), size=(W, H))  # chr_status01
p.place(75, pos=(0, H / 2))  # root
p.place(76, pos=(0, -H / 2), size=(W, H))  # status_base01
p.replace(76, "SpriteName: ps_cmn_base52", "SpriteName: ps_cmn_base54")  # the same frame without the title tab
p.set(77, "Active", "false")  # status_ttl_text01
p.place(78, pos=(0, -H / 2), size=(W, H))  # loc_status01

p.place(79, pos=(112 - W / 2, H / 2))  # loc_chr_icon01
p.set(86, "Active", "false")  # line01: the divider between the two columns

# rows pivoted on the stock grid's centre: hp and atk on their right edge, crt and brk on their left
p.place(87, pos=(ROWS_X + ROW_W - W / 2, H / 2 + ROW_H))  # loc_hp: bottom edge
p.place(92, pos=(ROWS_X + ROW_W - W / 2, H / 2 + ROW_H))  # loc_atk: top edge
p.place(97, pos=(ROWS_X - 40 - W / 2, H / 2 - ROW_H))  # loc_crt: bottom edge
p.place(104, pos=(ROWS_X - 40 - W / 2, H / 2 - ROW_H))  # loc_brk: top edge

p.save(sys.argv[2])
