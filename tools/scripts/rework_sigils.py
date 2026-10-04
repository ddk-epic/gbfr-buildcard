# Usage: python rework_sigils.py <status01.prfb.yaml> <out.prfb.yaml>
# Lays out each sigil row as both traits' icons and names, then the level, like sharecard's sigil rows.
from prefab import Prefab
from card import S
import re, sys

p = Prefab(sys.argv[1])

ROWS = [1544 + 13 * i for i in range(12)]  # equip01_p01_01 to 12
# offsets from a row's Id
ICON, NAME, LEVEL, SKILLS, SKILL_ICONS = 4, 5, 6, 10, (11, 12)  # icon01, text01_01, loc_text01_02, loc_icon_skill, icon_skill01/02

PANEL = 1123  # bc_weapon
SECTION_W = 756  # sharecard pixels, the cyan section

# loc_gene coordinates, from its centre; the row spans the section
ROW_W = int(SECTION_W * S / p.vec(PANEL, "Scale")[0]) - 8
ROW_H = 80
EDGE = 8  # the columns' and the level's inset from the row's edges
LEVEL_W = 175
ICON_W = 72
ICON_X = 40  # the icon's centre from the column's left edge
NAME_X = 96  # the name's left edge from the column's left edge
COLUMN_W = (ROW_W - 2 * EDGE - LEVEL_W) / 2
COLUMNS = [-ROW_W / 2 + EDGE, -ROW_W / 2 + EDGE + COLUMN_W]  # each trait's left edge
SKILLS_INSET = 40  # loc_icon_skill's pivot, its right edge, from the row's right edge
FONT_SIZE = 40

blocks = {i: p.lines[slice(*p.range(i))] for i in p.starts}
children = {i: p.children(i) for i in p.starts}

def name_text(row, n):
    # the row's name text, renamed, at a fixed width without its ContentSizeFitter
    block = blocks[row + NAME][1:]
    block[0] = f"  Name: bc_trait0{n + 1}"
    i = block.index("  - ComponentName: ContentSizeFitter")
    j = i + 1
    while block[j].startswith("    "):
        j += 1
    del block[i:j]
    block[block.index("      FontSize: 40")] = f"      FontSize: {FONT_SIZE}"
    block[block.index("      CharacterSpacing: -1")] = "      CharacterSpacing: 0"
    return block

new_id = max(p.starts) + 1
added = {}  # temporary Id: (row, trait)
for row in ROWS:
    for n in range(2):
        added[new_id] = (row, n)
        blocks[new_id] = [f"- Id: {new_id}", *name_text(row, n)]
        children[new_id] = []
        children[row + SKILLS].append(new_id)
        # the trait's SkillInfo fills the name
        icon = blocks[row + SKILL_ICONS[n]]
        i = icon.index("  - ComponentName: SkillInfo")
        i = icon.index("      Icons:", i) + 4
        icon[i:i] = ["      Names:", "      - ComponentName: Text", "        Index: 0", f"        ObjectRefId: {new_id}"]
        new_id += 1
    # the sigil icon, out of GemInfo's Sets, and the sigil name
    gem = blocks[row]
    i = gem.index(f"        ObjectRefId: {row + ICON}")
    del gem[i - 2:i + 1]
    for part in (ICON, NAME):
        b = blocks[row + part]
        b[b.index("  Active: true")] = "  Active: false"

# renumbers depth-first
order = []
stack = [0]
while stack:
    i = stack.pop()
    order.append(i)
    stack.extend(reversed(children[i]))
assert [i for i in order if i not in added] == sorted(p.starts)
ids = {old: new for new, old in enumerate(order)}

lines = []
for old in order:
    block = blocks[old]
    lines.append(f"- Id: {ids[old]}")
    in_children = False
    for line in block[1:]:
        if line == "  Children:":
            lines.append(line)
            lines.extend(f"  - {ids[c]}" for c in children[old])
            in_children = True
            continue
        if in_children and line.startswith("  - "):
            continue
        in_children = False
        m = re.match(r"(\s+ObjectRefId: )(-?\d+)$", line)
        if m and int(m[2]) != -1:
            line = f"{m[1]}{ids[int(m[2])]}"
        lines.append(line)
start = p.starts[0]
end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline
p.lines[start:end] = lines
p.reindex()

skills_right = ROW_W / 2 - SKILLS_INSET
for row in ROWS:
    row = ids[row]
    p.place(row, size=(ROW_W, ROW_H))
    p.place(row + LEVEL, pos=(ROW_W / 2 - EDGE, p.vec(row + LEVEL, "Position")[1]))
    p.place(row + SKILLS, pos=(skills_right, 0))
for temp, (row, n) in added.items():
    skills = ids[row + SKILLS]
    p.place(skills + 1 + n, pos=(COLUMNS[n] + ICON_X - skills_right, 0), size=(ICON_W, ICON_W))
    p.place(ids[temp], pos=(COLUMNS[n] + NAME_X - skills_right, 0), size=(COLUMN_W - NAME_X, ROW_H))

p.save(sys.argv[2])
print(f"rows {[ids[r] for r in ROWS]}, ids to {max(p.starts)}, row width {ROW_W}, name width {COLUMN_W - NAME_X:.0f}")
