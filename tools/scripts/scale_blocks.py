# Usage: python scale_blocks.py <status01.prfb.yaml> <out.prfb.yaml>
# Moves the main page's blocks into the build card sections, scaled uniformly to the section width.
import re, sys

SRC, DST = sys.argv[1], sys.argv[2]
S = 3424 / 2880
PARENT_X = 640  # loc_status02's centre in loc_base01 coordinates

def section(x, y, w, h):  # sharecard px -> (left, top, right, bottom) in loc_base01 coordinates
    return ((x - 1440) * S, (720 - y) * S, (x + w - 1440) * S, (720 - y - h) * S)

YELLOW = section(16, 16, 560, 1102)
GREEN = section(16, 1142, 560, 262)
CYAN = section(600, 16, 756, 1388)

BLOCKS = {  # object id: (section, block size, vertical alignment)
    73: (YELLOW, (2032, 256), "bottom"),   # loc_chr_status01: status
    109: (CYAN, (2032, 584), "top"),       # loc_chr_status02: gear
    275: (GREEN, (2032, 352), "center"),   # loc_chr_status03: skills
}
HIDE = [369]  # loc_chr_status04: support skills

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

def set_field(id_, key, value):
    start, end = obj_range(id_)
    for i in range(start, end):
        if lines[i].startswith(f"  {key}: "):
            lines[i] = f"  {key}: {value}"
            return
    raise KeyError(f"{id_}.{key}")

for id_, ((l, t, r, b), (w, h), align) in BLOCKS.items():
    s = (r - l) / w
    cx = (l + r) / 2
    cy = {"top": t - h * s / 2, "bottom": b + h * s / 2, "center": (t + b) / 2}[align]
    px, py = cx - PARENT_X, cy
    set_field(id_, "Position", f"{f(px)}, {f(py)}, 0")
    set_field(id_, "AnchorPoint", f"{f(px)}, {f(py)}")
    set_field(id_, "OffsetMin", f"{f(px - w / 2)}, {f(py - h / 2)}")
    set_field(id_, "OffsetMax", f"{f(px + w / 2)}, {f(py + h / 2)}")
    set_field(id_, "Scale", f"{f(s)}, {f(s)}, 1")

for id_ in HIDE:
    set_field(id_, "Active", "false")

open(DST, "w", encoding="utf-8", newline="").write(nl.join(lines))
