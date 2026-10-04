# Usage: python add_over_mastery.py <status01.prfb.yaml> <lb_ovtli02.prfb.yaml> <out.prfb.yaml>
# Lays out the Over Mastery section in the magenta section from copies of lb_ovtli02's first Over Mastery row.
import sys
from prefab import Prefab, f
from card import INK, LEFT, S, Group, add_powers, append, card, copy_objects, text

p = Prefab(sys.argv[1])
source = Prefab(sys.argv[2])

ROW = 14  # var00_lb_ovtli01_p01_01
ROW_OBJECTS = 32
LINE, ICON_OBJ, TEXTS, PLUS, STARS = 17, 18, 19, 22, 25  # line01, icon01, loc_text01, icon_plus01, loc_star01
COLORED_TEXTS = [20, 23, 24]  # text01, num01, percent01
ICON_X_ROW = -482  # icon01's left edge in the row
TEXT_X_ROW = -384  # loc_text01's left edge in the row
ROW_ICON = 88

# sharecard pixels, relative to the magenta section's top-left corner
SECTION_X, SECTION_Y, SECTION_W, SECTION_H = 1380, 1142, 491.33, 262
HEADING_H = 50
PAD_BOTTOM = 8
LINES = 4
LINE_H, LINE_GAP = 36, 4
ICON = 42
ICON_X = 12
SCALE = ICON * S / ROW_ICON

def edit(old, block):
    # names the row, hides the separator and the stars, colours the texts
    if old == ROW:
        block[1] = f"  Name: {name}"
    if old in (LINE, STARS):
        block[block.index("  Active: true")] = "  Active: false"
    if old in COLORED_TEXTS + [PLUS]:
        i = next(i for i, line in enumerate(block) if line.startswith("      Color: "))
        block[i] = f"      Color: {', '.join(f(c) for c in INK)}, 1"
    return block

group = Group()
group.text("bc_om_heading", text("OVER MASTERY", 30, INK, 1, LEFT), SECTION_X + 8, SECTION_Y + 4, 480, 40)
container, _, (heading,) = append(p, "bc_om", group)

first = max(p.starts) + 1
rows, new = [], []
for i in range(LINES):
    name = f"bc_om_{i}"
    ids = {ROW + k: first + i * ROW_OBJECTS + k for k in range(ROW_OBJECTS)}
    rows.append(ids[ROW])
    new += copy_objects(source, list(ids), ids, edit)
start, end = p.range(container)
last_child = max(i for i in range(start, end) if p.lines[i].startswith("  - "))
p.lines[last_child + 1:last_child + 1] = [f"  - {r}" for r in rows]
end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline
p.lines[end:end] = new
p.reindex()

lines_h = LINES * LINE_H + (LINES - 1) * LINE_GAP
top = HEADING_H + (SECTION_H - PAD_BOTTOM - HEADING_H - lines_h) / 2
for i, row in enumerate(rows):
    y = top + i * (LINE_H + LINE_GAP) + LINE_H / 2
    x = ICON_X - ICON_X_ROW * SCALE / S  # the row's centre
    p.set(row, "Scale", (SCALE, SCALE, 1))
    p.place(row, pos=card(SECTION_X + x, SECTION_Y + y))
    p.place(row - ROW + ICON_OBJ, pos=(ICON_X_ROW, 0))
    p.place(row - ROW + TEXTS, pos=(TEXT_X_ROW, 0))

add_powers(p, [heading])
add_powers(p, rows, component="")
p.save(sys.argv[3])
print(f"added ids {container}..{rows[-1] + ROW_OBJECTS - 1}, heading {heading}, rows {rows}, scale {SCALE:.4f}")
