# Usage: python scaffold.py <status01.prfb.yaml> <out.prfb.yaml>
# Appends a 2:1 build card scaffold (section outlines) to status01's loc_base01.
import re, sys

SRC, DST = sys.argv[1], sys.argv[2]
PARENT_ID = 2           # loc_base01, 3424x1884, centre anchored
CARD_W, CARD_H = 3424, 1712
S = CARD_W / 2880       # sharecard px -> panel px
T = 6                   # outline thickness

# Sharecard grid (2880x1440): inset 16, gap 24, columns 560/756/1484, rows 1102/262.
SECTIONS = [  # name, x, y, w, h (sharecard px, top-left origin), colour
    ("card",      0,    0,    2880,   1440, "1, 0, 0, 1"),
    ("portrait",  16,   16,   560,    1102, "1, 0.85, 0, 1"),
    ("skills",    16,   1142, 560,    262,  "0, 1, 0, 1"),
    ("gear",      600,  16,   756,    1388, "0, 1, 1, 1"),
    ("mtraits",   1380, 16,   1484,   1102, "0.2, 0.4, 1, 1"),
    ("om",        1380, 1142, 491.33, 262,  "1, 0, 1, 1"),
    ("summons",   1876.33, 1142, 987.67, 262, "1, 0.5, 0, 1"),
]

def f(v):
    v = round(v, 2)
    return str(int(v)) if v == int(v) else str(v)

def rect(id_, name, children, comps, pos, size, amin, amax, pivot, anchor_point):
    # Position: local position of the pivot relative to the parent's centre.
    # Offsets: rect edges relative to the anchors.
    w, h = size
    om = (anchor_point[0] - pivot[0] * w, anchor_point[1] - pivot[1] * h)
    ox = (anchor_point[0] + (1 - pivot[0]) * w, anchor_point[1] + (1 - pivot[1]) * h)
    out = [f"- Id: {id_}", f"  Name: {name}"]
    if children:
        out.append("  Children:")
        out += [f"  - {c}" for c in children]
    if comps:
        out.append("  Components:")
        out += comps
    out += [
        "  Active: true",
        f"  Position: {f(pos[0])}, {f(pos[1])}, 0",
        "  Rotation: 0, 0, 0, 1",
        "  Scale: 1, 1, 1",
        f"  Pivot: {f(pivot[0])}, {f(pivot[1])}",
        f"  AnchorPoint: {f(anchor_point[0])}, {f(anchor_point[1])}",
        f"  AnchorMin: {f(amin[0])}, {f(amin[1])}",
        f"  AnchorMax: {f(amax[0])}, {f(amax[1])}",
        f"  OffsetMin: {f(om[0])}, {f(om[1])}",
        f"  OffsetMax: {f(ox[0])}, {f(ox[1])}",
        f"  SizeDelta: {f(w)}, {f(h)}",
    ]
    return out

def image(color):
    return [
        "  - ComponentName: Image",
        "    Component:",
        f"      Color: {color}",
        "      Type: 0",
        "      FillCenter: false",
        "      FillMethod: 0",
        "      FillOrigin: 0",
        "      FillAmount: 0",
        "      UvRect: 0, 0, 0, 0",
        "      RawImage: false",
        "      Clockwise: false",
        "      PreserveAspect: false",
        "      E3ED5266: 0",
        "      Enable: true",
    ]

text = open(SRC, encoding="utf-8").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
next_id = max(int(m) for m in re.findall(r"(?m)^- Id: (\d+)\r?$", text)) + 1

new = []
card_id = next_id
section_ids = []
nid = card_id + 1
body = []
for name, x, y, w, h, color in SECTIONS:
    sid = nid
    line_ids = [sid + 1 + i for i in range(4)]
    nid = sid + 5
    section_ids.append(sid)
    cx = (x + w / 2 - 1440) * S
    cy = (720 - (y + h / 2)) * S
    W, H = w * S, h * S
    body += rect(sid, f"bc_{name}", line_ids, None, (cx, cy), (W, H), (0.5, 0.5), (0.5, 0.5), (0.5, 0.5), (cx, cy))
    # top, bottom, left, right: stretched along one edge, T thick, inside the section.
    edges = [
        ("top",    (0, 1), (1, 1), (0.5, 1), (0, H / 2),  (0, T)),
        ("bottom", (0, 0), (1, 0), (0.5, 0), (0, -H / 2), (0, T)),
        ("left",   (0, 0), (0, 1), (0, 0.5), (-W / 2, 0), (T, 0)),
        ("right",  (1, 0), (1, 1), (1, 0.5), (W / 2, 0),  (T, 0)),
    ]
    for lid, (en, amin, amax, piv, pos, sd) in zip(line_ids, edges):
        # Stretched axis: SizeDelta 0, offsets 0; fixed axis: T thick at the anchor.
        out = rect(lid, f"bc_{name}_{en}", None, image(color), pos, sd, amin, amax, piv, (0, 0))
        body += out

card = rect(card_id, "loc_buildcard", section_ids, None, (0, 0), (CARD_W, CARD_H), (0.5, 0.5), (0.5, 0.5), (0.5, 0.5), (0, 0))

# Register loc_buildcard as the last child of the parent.
i = lines.index(f"- Id: {PARENT_ID}")
j = lines.index("  Children:", i)
k = j + 1
while lines[k].startswith("  - ") and lines[k][4:].isdigit():
    k += 1
lines.insert(k, f"  - {card_id}")

while lines and lines[-1] == "":
    lines.pop()
lines += card + body
open(DST, "w", encoding="utf-8", newline="").write(nl.join(lines) + nl)
print(f"added ids {card_id}..{nid - 1}")
