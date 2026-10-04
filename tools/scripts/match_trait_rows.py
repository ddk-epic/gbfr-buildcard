# Usage: python match_trait_rows.py <status01.prfb.yaml> <out.prfb.yaml>
# Sizes the weapon, wrightstone and sigil trait rows' levels so the label matches the trait names, and stretches the weapon and wrightstone rows to the sigil rows' width.
import re, sys
from prefab import Prefab, f

p = Prefab(sys.argv[1])

WEAPON_ROWS = [1398, 1415, 1432, 1449, 1466, 1489, 1506, 1523]  # list_skill_p01_01 to 08
SIGIL_ROWS = [1544 + 15 * i for i in range(12)]  # equip01_p01_01 to 12
PENDULUM_TITLE = 1484  # loc_title01
# offsets from a row's Id
LABEL, NUMBER = 8, 9  # lv01_text01, lv01_num01 or text01_02, text01_03
LEVEL, LEVEL_PAIR = 6, 7  # loc_skill_lv01, loc_lv01 in a weapon row, loc_text01_02, loc_text01_03 in a sigil row

NAME_SIZE, LABEL_SIZE = 40, 32
NUMBER_SIZE, STOCK_NUMBER_SIZE = 44, 40  # English
SCALES = {LABEL: NAME_SIZE / LABEL_SIZE, NUMBER: NUMBER_SIZE / STOCK_NUMBER_SIZE}
GAP = 8  # loc_text01_03's spacing
RAISE = 4  # the level's bottom padding

EDGE = 8  # a sigil row's level inset from its right edge

def get_line(id_, key):
    start, end = p.range(id_)
    for i in range(start, end):
        if p.lines[i].strip().startswith(f"{key}: "):
            return p.lines[i].split(": ", 1)[1]
    raise KeyError(f"{id_}.{key}")

def set_line(id_, key, value):
    start, end = p.range(id_)
    for i in range(start, end):
        if p.lines[i].strip().startswith(f"{key}: "):
            p.lines[i] = p.lines[i][: p.lines[i].index(key)] + f"{key}: {value}"
            return
    raise KeyError(f"{id_}.{key}")

for row in WEAPON_ROWS + SIGIL_ROWS:
    for part in (LABEL, NUMBER):
        start, end = p.range(row + part)
        for i in range(start, end):
            m = re.match(r"(\s+FontSize: )(\d+)$", p.lines[i])
            if m and int(m[2]):
                size = int(m[2]) * SCALES[part]
                # overrides are ints, the Text's FontSize a float
                p.lines[i] = f"{m[1]}{f(size)}" if m[1] == "      FontSize: " else f"{m[1]}{round(size)}"
    w, h = p.vec(row + NUMBER, "SizeDelta")
    p.place(row + NUMBER, size=(w * SCALES[NUMBER], h))
    left, top, right, _ = get_line(row + LEVEL, "Padding").split(", ")
    set_line(row + LEVEL, "Padding", f"{left}, {top}, {right}, {RAISE}")

width = p.vec(SIGIL_ROWS[0], "SizeDelta")[0]
for row in WEAPON_ROWS:
    p.place(row, size=(width, p.vec(row, "SizeDelta")[1]))
    p.repin(row)
    p.place(row + LEVEL, pos=(width / 2 - EDGE, p.vec(row + LEVEL, "Position")[1]))
    # the bar's right padding shortened by as much as the gap
    left, top, right, bottom = get_line(row + LEVEL, "Padding").split(", ")
    gap = float(get_line(row + LEVEL_PAIR, "Spacing"))
    set_line(row + LEVEL, "Padding", f"{left}, {top}, {f(float(right) - (gap - GAP))}, {bottom}")
    set_line(row + LEVEL_PAIR, "Spacing", GAP)

p.place(PENDULUM_TITLE, size=(width, p.vec(PENDULUM_TITLE, "SizeDelta")[1]))
p.repin(PENDULUM_TITLE)

p.save(sys.argv[2])
