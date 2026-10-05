# Usage: python add_frame.py <status01.prfb.yaml> <out.prfb.yaml>
# Adds an ornate frame around the card.
import sys
from prefab import Prefab
from card import S, Group, append, image

p = Prefab(sys.argv[1])

SPRITE = ("atlas/pause_pause_common", "ps_cmn_frame02")
LINE_COLOR = (173 / 255, 199 / 255, 223 / 255)  # ps_cmn_base53's line
PANEL_COLOR = (245 / 255, 251 / 255, 255 / 255)  # ps_cmn_base53's panel
INSET = 4
BAND = 12
CORNER_LEN, CORNER_W = 30, 16

band, inset = BAND / S, INSET / S
length, width = CORNER_LEN / S, CORNER_W / S

group = Group()
panel = image(PANEL_COLOR, 1)
for name, x, y, w, h in (("top", 0, 0, 2880, band), ("bottom", 0, 1440 - band, 2880, band),
                         ("left", 0, 0, band, 1440), ("right", 2880 - band, 0, band, 1440)):
    group.image(f"bc_frame_{name}", panel, x, y, w, h)
for corner, right, bottom in (("top_left", 0, 0), ("top_right", 1, 0), ("bottom_left", 0, 1), ("bottom_right", 1, 1)):
    for edge, w, h in (("h", length, width), ("v", width, length)):
        group.image(f"bc_frame_{corner}_{edge}", panel, right * (2880 - w), bottom * (1440 - h), w, h)
line = image(LINE_COLOR, 1, SPRITE)
line[line.index("      Type: 0")] = "      Type: 1"
group.image("bc_frame_line", line, inset, inset, 2880 - 2 * inset, 1440 - 2 * inset)
container, images, _ = append(p, "bc_frame", group)

p.save(sys.argv[2])
print(f"ids {container}..{images[-1]}")
