# Usage: python place_badges.py <status01.prfb.yaml> <out.prfb.yaml>
# Moves the level, master level, PWR and name badges into the yellow section, unscaled, in sharecard order.
import sys

SRC, DST = sys.argv[1], sys.argv[2]
S = 3424 / 2880
GAP = 23 * S
INSET = 12

LEFT = (16 - 1440) * S
TOP = (720 - 16) * S
STATUS_TOP = (720 - 1118) * S + 256 * (560 * S / 2032)  # top of the scaled status block (73)

STATUS01 = (-1712, 0, 1336, 1884)  # loc_status01: pivot x, pivot y, width, height in loc_base01 coordinates
BASE01 = (3424, 1884)  # loc_base01: width, height

def f(v):
    v = round(v, 3)
    return str(int(v)) if v == int(v) else str(v)

text = open(SRC, encoding="utf-8").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)

def obj_range(id_):
    start = lines.index(f"- Id: {id_}")
    end = start + 1
    while end < len(lines) and not lines[end].startswith("- Id: "):
        end += 1
    return start, end

def get_field(id_, key):
    start, end = obj_range(id_)
    for i in range(start, end):
        if lines[i].startswith(f"  {key}: "):
            return [float(v) for v in lines[i].split(": ", 1)[1].split(", ")]
    raise KeyError(f"{id_}.{key}")

def set_field(id_, key, value):
    start, end = obj_range(id_)
    for i in range(start, end):
        if lines[i].startswith(f"  {key}: "):
            lines[i] = f"  {key}: {value}"
            return
    raise KeyError(f"{id_}.{key}")

def place(id_, x, y, parent_pivot, parent_size, pivot=None):
    # x, y: the new pivot position in loc_base01 coordinates
    if pivot:
        set_field(id_, "Pivot", f"{f(pivot[0])}, {f(pivot[1])}")
    px, py = get_field(id_, "Pivot")
    amin = get_field(id_, "AnchorMin")
    w, h = get_field(id_, "SizeDelta")
    ppx, ppy, ppivx, ppivy = parent_pivot
    pw, ph = parent_size
    lx, ly = x - ppx, y - ppy
    ax, ay = lx - (amin[0] - ppivx) * pw, ly - (amin[1] - ppivy) * ph
    set_field(id_, "Position", f"{f(lx)}, {f(ly)}, 0")
    set_field(id_, "AnchorPoint", f"{f(ax)}, {f(ay)}")
    set_field(id_, "OffsetMin", f"{f(ax - px * w)}, {f(ay - py * h)}")
    set_field(id_, "OffsetMax", f"{f(ax + (1 - px) * w)}, {f(ay + (1 - py) * h)}")

STATUS01_PIVOT = (STATUS01[0], STATUS01[1], 0, 0.5)
STATUS01_SIZE = STATUS01[2:]
BASE01_PIVOT = (0, 0, 0.5, 0.5)

# loc_name01: left-aligned above the status block, growing right with the name
NAME_H = 72
name_y = STATUS_TOP + GAP + NAME_H / 2
place(7, LEFT, name_y, BASE01_PIVOT, BASE01, pivot=(0, 0.5))

# power01: icon top 184 and text bottom 45 from its centre, text 240 wide
pwr_y = name_y + NAME_H / 2 + GAP + 45
place(17, LEFT + INSET + 120, pwr_y, STATUS01_PIVOT, STATUS01_SIZE)

# level01 and loc_ml_level01 keep their stock pairing, moved down so the "Lvl" label (877 at stock) clears the top
dy = TOP - INSET - 877
place(21, STATUS01[0] + 192, 748 + dy, STATUS01_PIVOT, STATUS01_SIZE)
place(45, STATUS01[0] + 396, 784 + dy, STATUS01_PIVOT, STATUS01_SIZE)

open(DST, "w", encoding="utf-8", newline="").write(nl.join(lines))
