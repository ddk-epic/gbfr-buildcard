# Usage: python widen_weapon_section.py <status01.prfb.yaml> <out.prfb.yaml>
# Widens the weapon section to the trait rows' width: the dividers and the stat row stretch, the level and the gauge move out to the sides.
import sys
from prefab import Prefab

p = Prefab(sys.argv[1])

ROW = 1398  # list_skill_p01_01
DIVIDERS = [1231, 1232]  # line01, line02
LEVEL, GAUGE, TAGS = 1239, 1286, 1390  # loc_lv01, loc_lt01, loc_tag01
STATS = 1376  # loc_status01
CELLS = [1377, 1380, 1383, 1387]  # loc_hp01, loc_atk01, loc_crt01, loc_brk01

width = p.vec(ROW, "SizeDelta")[0]
shift = (width - p.vec(DIVIDERS[0], "SizeDelta")[0]) / 2

for line in DIVIDERS:
    p.place(line, size=(width, p.vec(line, "SizeDelta")[1]))

for id_, dx in ((LEVEL, -shift), (GAUGE, shift), (TAGS, shift)):
    x, y = p.vec(id_, "Position")[:2]
    p.place(id_, pos=(x + dx, y))

# the stat row's four cells, a quarter of its width each, edge to edge
stats_w, stats_h = p.vec(STATS, "SizeDelta")
stats_w += 2 * shift
p.place(STATS, size=(stats_w, stats_h))
for n, cell in enumerate(CELLS):
    h = p.vec(cell, "SizeDelta")[1]
    x = -stats_w / 2 + stats_w / 4 * (n + p.vec(cell, "Pivot")[0])
    p.place(cell, pos=(x, p.vec(cell, "Position")[1]), size=(stats_w / 4, h))
    p.repin(cell)

p.save(sys.argv[2])
