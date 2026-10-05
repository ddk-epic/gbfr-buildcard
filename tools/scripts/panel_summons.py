# Usage: python panel_summons.py <status01.prfb.yaml> <out.prfb.yaml>
# Adds the generic white panel behind the summon cells.
import sys
from prefab import Prefab, renumber
from card import S, card, rect

p = Prefab(sys.argv[1])

SUMMONS = 1496  # bc_smn
SKILLS, SKILLS_PANEL = 2302, 2303  # bc_skills, bc_skills_base

# sharecard pixels
LEFT, TOP, WIDTH, HEIGHT = 1876.33, 1142, 987.67, 262

assert p.get(SUMMONS, "Name") == "bc_smn" and p.get(SKILLS_PANEL, "Name") == "bc_skills_base"
blocks = {i: p.lines[slice(*p.range(i))] for i in p.starts}
children = {i: p.children(i) for i in p.starts}

start, end = p.range(SKILLS_PANEL)
block = p.lines[start:end]
panel = max(p.starts) + 1
blocks[panel] = [f"- Id: {panel}", *rect("bc_smn_base", block[block.index("  Components:") + 1:block.index("  Active: true")],
                                         (0.5, 0.5))]
children[panel] = []
children[SUMMONS] = [panel] + children[SUMMONS]

new = renumber(p, blocks, children)
panel = new[panel]

scale = p.vec(new[SKILLS], "Scale")[0]
p.set(panel, "Scale", (scale, scale, 1))
p.place(panel, pos=card(LEFT + WIDTH / 2, TOP + HEIGHT / 2), size=(WIDTH * S / scale, HEIGHT * S / scale))

p.save(sys.argv[2])
print(f"panel {panel}, slots {[new[c] for c in children[SUMMONS][1:]]}, skills {new[SKILLS]}")
