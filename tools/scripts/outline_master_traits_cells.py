# Usage: python outline_master_traits_cells.py <status01.prfb.yaml> <out.prfb.yaml>
# Replaces the master traits cells' frames with a hidden picked style, which CardWriter shows on picked cells.
import re, sys
from prefab import Prefab, f, renumber
from card import add_powers

p = Prefab(sys.argv[1])

GRIDS = [503, 912]  # bc_mt_cells, bc_mt_cells_captain
OUTLINE = "layouts/pause/status/noatlastextures/bc_outline"
PICKED = (107 / 255, 132 / 255, 155 / 255)  # a quarter of the way from ps_sboard_list01's colour to white
UNPICKED = (36 / 255, 30 / 255, 43 / 255)  # the cell colour at half saturation
ALPHA = 0.5
PICKED_ALPHA = 0.7  # with the unpicked base under it

assert [p.get(i, "Name") for i in GRIDS] == ["bc_mt_cells", "bc_mt_cells_captain"]
blocks = {i: p.lines[slice(*p.range(i))] for i in p.starts}
children = {i: p.children(i) for i in p.starts}

def set_line(block, key, value):
    block[next(k for k, line in enumerate(block) if line.strip().startswith(f"{key}: "))] = value

def color(rgb, alpha):
    return f"      Color: {', '.join(f(v) for v in rgb)}, {f(alpha)}"

added, firsts = [], []
next_id = max(p.starts) + 1
for grid in GRIDS:
    cells, frames = [], []
    for i in children[grid]:
        m = re.fullmatch(r"bc_mt_(\d+)_(\d+)_(\d+)_(base|frame)", p.get(i, "Name"))
        if m and m[4] == "base":
            cells.append((tuple(int(v) for v in m.groups()[:3]), i))
        elif m:
            frames.append(i)
    children[grid] = [i for i in children[grid] if i not in frames]
    for i in frames:
        del blocks[i], children[i]
    groups = []
    firsts.append(next_id)
    for (s, r, c), base in sorted(cells):
        group, fill, outline = next_id, next_id + 1, next_id + 2
        next_id += 3
        # a container with the grid's rect
        rect = blocks[grid][next(k for k, line in enumerate(blocks[grid]) if line.startswith("  Active: ")):]
        blocks[group] = [f"- Id: {group}", f"  Name: bc_mt_{s}_{r}_{c}_picked", "  Children:", "  Active: false", *rect[1:]]
        original = next(line for line in blocks[base] if line.strip().startswith("Color: "))
        blocks[fill] = [f"- Id: {fill}", f"  Name: bc_mt_{s}_{r}_{c}_picked_base", *blocks[base][2:]]
        set_line(blocks[fill], "Color", f"{original[: original.rindex(',')]}, {f(1 - (1 - PICKED_ALPHA) / (1 - ALPHA))}")
        blocks[outline] = [f"- Id: {outline}", f"  Name: bc_mt_{s}_{r}_{c}_picked_outline", *blocks[base][2:]]
        set_line(blocks[outline], "Color", color(PICKED, 1))
        set_line(blocks[outline], "TexturePath", f"        TexturePath: {OUTLINE}")
        set_line(blocks[outline], "SpriteName", f"        SpriteName: {OUTLINE.rsplit('/', 1)[1]}")
        set_line(blocks[outline], "FillCenter", "      FillCenter: false")
        set_line(blocks[base], "Color", color(UNPICKED, ALPHA))
        children[group], children[fill], children[outline] = [fill, outline], [], []
        groups.append(group)
        added.append((group, outline, base))
    # after the last cell base, under the texts
    last = max(children[grid].index(base) for _, base in cells)
    children[grid][last + 1:last + 1] = groups

new = renumber(p, blocks, children)
for _, outline, base in added:
    k = p.vec(new[base], "Scale")[0]
    w, h = p.vec(new[base], "SizeDelta")
    p.set(new[outline], "Scale", (1, 1, 1))
    p.place(new[outline], size=(w * k, h * k))
groups = [new[group] for group, _, _ in added]
add_powers(p, groups, component="")
p.reindex()

p.save(sys.argv[2])
names = ["bc_mt_cells_captain", "bc_mt_0_0_label", "bc_om_0", "bc_smn_0"]
print(f"{len(groups)} picked groups, first per grid {[new[i] for i in firsts]}")
print({n: [i for i in p.starts if p.get(i, "Name") == n] for n in names})
