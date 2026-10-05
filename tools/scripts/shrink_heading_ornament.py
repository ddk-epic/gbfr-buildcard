# Usage: python shrink_heading_ornament.py <status01.prfb.yaml> <out.prfb.yaml>
# Shrinks the master traits heading's ornament and continues its line at full size.
import sys
from prefab import Prefab, renumber
from card import card, rect

p = Prefab(sys.argv[1])

BOARD = 427  # bc_mtraits
LINE = 458  # line01
LINE_Y = 191  # line01's line centre below its top
TIP = 276  # ps_frame_line02's first column of plain line
ORNAMENT_W = 336  # ps_frame_line02's minimum width
ON_LINE = [459, 496]  # bc_mt_perks, bc_mt_heading
FACTOR = 0.85
TOP = 16  # sharecard pixels
MASK = "layouts/pause/status/noatlastextures/bc_white"

assert p.get(BOARD, "Name") == "bc_mtraits" and p.get(LINE, "Name") == "line01"
assert [p.get(i, "Name") for i in ON_LINE] == ["bc_mt_perks", "bc_mt_heading"]
k = p.vec(LINE, "Scale")[0]
left, top, _ = p.vec(LINE, "Position")
w, h = p.vec(LINE, "SizeDelta")
right = left + w * k
new_k = round(k * FACTOR, 3)
new_top = card(0, TOP)[1]
line_y = new_top - LINE_Y * new_k
cut = left + TIP * new_k

blocks = {i: p.lines[slice(*p.range(i))] for i in p.starts}
children = {i: p.children(i) for i in p.starts}
mask = ["  - ComponentName: Mask", "    Component:", "      Sprite:", f"        TexturePath: {MASK}",
        f"        SpriteName: {MASK.rsplit('/', 1)[1]}", "      Offset: 0, 0", "      ChannelWeights: 0, 0, 0, 1",
        "      InvertMask: false", "      InvertOutsides: false", "      Enable: true"]
ornament_clip, line_clip, line = max(p.starts) + 1, max(p.starts) + 2, max(p.starts) + 3
for id_, name in ((ornament_clip, "bc_mt_ornament_clip"), (line_clip, "bc_mt_line_clip")):
    block = rect(name, mask, (0.5, 0.5))
    blocks[id_] = [f"- Id: {id_}", block[0], "  Children:", *block[1:]]
blocks[line] = [f"- Id: {line}", "  Name: bc_mt_line", *blocks[LINE][2:]]
children[ornament_clip], children[line_clip], children[line] = [LINE], [line], []
i = children[BOARD].index(LINE)
children[BOARD][i:i + 1] = [ornament_clip, line_clip]

dy = line_y - (top - LINE_Y * k)
new = renumber(p, blocks, children)
ornament, ornament_clip, line_clip, line = new[LINE], new[ornament_clip], new[line_clip], new[line]

# the ornament up to its tip
clip_x, clip_y = (left + cut) / 2, new_top - h * new_k / 2
p.place(ornament_clip, pos=(clip_x, clip_y), size=(cut - left, h * new_k))
p.set(ornament, "Scale", (new_k, new_k, 1))
p.place(ornament, pos=(left - clip_x, new_top - clip_y), size=(ORNAMENT_W, h))

# the line from the ornament's tip
clip_x, clip_y = (cut + right) / 2, line_y
p.place(line_clip, pos=(clip_x, clip_y), size=(right - cut, h * k))
line_left = cut - TIP * k
p.place(line, pos=(line_left - clip_x, LINE_Y * k), size=((right - line_left) / k, h))

for i in ON_LINE:
    x, y, _ = p.vec(new[i], "Position")
    p.place(new[i], pos=(x, y + dy))

p.save(sys.argv[2])
print(f"scale {new_k}, ornament clip {ornament_clip}, ornament {ornament}, line clip {line_clip}, line {line}, "
      f"line up {dy:.3f}")
