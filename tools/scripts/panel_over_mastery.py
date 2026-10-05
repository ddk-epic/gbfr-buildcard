# Usage: python panel_over_mastery.py <status01.prfb.yaml> <out.prfb.yaml>
# Puts the Over Mastery rows on the skills section's panel under its title bar in the magenta section, and enlarges
# the rows.
import sys
from prefab import Prefab
from card import PARENT, S, card, copy_objects

p = Prefab(sys.argv[1])

CONTAINER, BAR = 768, 769  # bc_om, bc_om_heading
ROWS = [770, 802, 834, 866]  # bc_om_<i>
ICON_X_ROW = -482  # icon01's left edge in a row
SKILLS, SKILLS_PANEL, SKILLS_BAR = 1724, 1725, 1726  # bc_skills, bc_skills_base, bc_skills_ttl
TITLE_TEXT_ID = "TXT_PAU_LB_TAB_LIMIT_OVER"
BAR_TEXT = 20  # the title text's pivot below the bar's

# sharecard pixels, the magenta section
LEFT, TOP, WIDTH, HEIGHT = 1380, 1142, 491.33, 262
PAD_X, PAD_Y = 10, 18.5  # border and padding
ICON_PAD = 5
TITLE_GAP = 46  # the bar's top above the rows, in title units
ROW_SCALE = 1.1
LINE_H, LINE_GAP = 36, 4

def components(id_):
    start, end = p.range(id_)
    block = p.lines[start:end]
    return block[block.index("  Components:"):block.index("  Active: true")]

scale = p.vec(SKILLS, "Scale")[0]
U = scale / S  # sharecard pixels per unit
title_k = p.vec(SKILLS_BAR, "Scale")[0]
W, H = WIDTH / U, HEIGHT / U
bar_y = H / 2 - (p.size(SKILLS)[1] / 2 - p.vec(SKILLS_BAR, "Position")[1])

start, end = p.range(CONTAINER)
i = p.lines.index("  Active: true", start, end)
p.lines[i:i] = components(SKILLS_PANEL)
p.reindex()
start, end = p.range(BAR)
block = p.lines[start:end]
bar_components = components(SKILLS_BAR)
bar_rect = p.lines[p._line(SKILLS_BAR, "Active"):p.range(SKILLS_BAR)[1]]
p.lines[start:end] = [f"- Id: {BAR}", "  Name: bc_om_ttl", *bar_components, *bar_rect]
p.reindex()

text = max(p.starts) + 1
def text_edit(old, block):
    block[1] = "  Name: bc_om_ttl_text"
    block[block.index("  - ComponentName: TextSetter") + 2] = f"      TextID: {TITLE_TEXT_ID}"
    block[block.index("  AnchorMin: 0.5, 0")] = "  AnchorMin: 0.5, 0.5"
    block[block.index("  AnchorMax: 0.5, 0")] = "  AnchorMax: 0.5, 0.5"
    return block
text_lines = copy_objects(p, [SKILLS_BAR + 1], {SKILLS_BAR + 1: text}, text_edit)
start, end = p.range(PARENT)
last_child = max(i for i in range(start, end) if p.lines[i].startswith("  - "))
p.lines.insert(last_child + 1, f"  - {text}")
end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline
p.lines[end:end] = [line for line in text_lines if line != ""]
p.reindex()

cx, cy = card(LEFT + WIDTH / 2, TOP + HEIGHT / 2)
p.set(CONTAINER, "Scale", (scale, scale, 1))
p.place(CONTAINER, pos=(cx, cy), size=(W, H))
p.place(BAR, pos=(0, bar_y))
p.set(text, "Scale", (scale * title_k, scale * title_k, 1))
p.place(text, pos=(cx, cy + scale * (bar_y - BAR_TEXT * title_k)))

# rows centred between the bar's gap and the bottom padding, at the skills icons' left inset
row_scale = p.vec(ROWS[0], "Scale")[0] * ROW_SCALE / scale
line_h, line_gap = LINE_H * ROW_SCALE / U, LINE_GAP * ROW_SCALE / U
content_top = bar_y - TITLE_GAP * title_k
content_bottom = -H / 2 + PAD_Y / U
lines_h = len(ROWS) * line_h + (len(ROWS) - 1) * line_gap
top = (content_top + content_bottom) / 2 + lines_h / 2
x = -W / 2 + (PAD_X + ICON_PAD) / U - ICON_X_ROW * row_scale
for i, row in enumerate(ROWS):
    p.set(row, "Scale", (row_scale, row_scale, 1))
    p.place(row, pos=(x, top - i * (line_h + line_gap) - line_h / 2))

# the heading's Text ref, out of CharaInfo.Powers
start, end = p.range(0)
i = p.lines.index(f"        ObjectRefId: {BAR}", start, end)
assert p.lines[i - 2] == "      - ComponentName: Text"
del p.lines[i - 2:i + 1]

p.save(sys.argv[2])
print(f"title text {text}, panel {W:.1f}x{H:.1f} at scale {scale}, row scale {row_scale:.4f}, "
      f"rows {lines_h * U:.1f} px of {(content_top - content_bottom) * U:.1f} px")
