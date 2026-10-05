# Usage: python add_captain_master_traits.py <status01.prfb.yaml> <out.prfb.yaml>
# Moves the master traits board's rank panels, title bars, cells and their texts into bc_mt_cells and adds
# bc_mt_cells_captain (inactive): a copy with 14 EX cells per style, where everything from rank 1's panel down except
# the title bars and rank labels is shrunk vertically by one factor to fit two more cell rows. Renumbers the Ids.
import re, sys
from prefab import Prefab, renumber
from card import copy_objects

p = Prefab(sys.argv[1])

BOARD = 462  # bc_mtraits
STYLES = 3
SLOTS, CAPTAIN_SLOTS = [4, 8, 8, 10], [4, 8, 8, 14]
COLUMNS = 2

names = {p.get(i, "Name"): i for i in p.children(BOARD)}
blocks = {i: p.lines[slice(*p.range(i))] for i in p.starts}
children = {i: p.children(i) for i in p.starts}

def grid_objects(s, slots):
    images = [f"bc_mt_{s}_{r}_{kind}" for kind in ("base", "ttl") for r in range(len(slots))]
    images += [f"bc_mt_{s}_{r}_{c}_{kind}" for r in range(len(slots)) for c in range(slots[r]) for kind in ("base", "frame")]
    texts = [f"bc_mt_{s}_{r}_{kind}" for r in range(len(slots)) for kind in ("label", "count")]
    texts += [f"bc_mt_{s}_{r}_{c}_{state}" for r in range(len(slots)) for c in range(slots[r]) for state in ("on", "off")]
    return images, texts

def container(id_, name, active):
    block = [l for l in blocks[BOARD] if not l.startswith("  - ")]
    block[0] = f"- Id: {id_}"
    block[1] = f"  Name: {name}"
    block[block.index("  Active: true")] = f"  Active: {str(active).lower()}"
    return block

new_id = max(p.starts) + 1
cells, captain = new_id, new_id + 1
new_id += 2

# the stock grid: images of all styles, then texts of all styles
grid = [names[n] for s in range(STYLES) for n in grid_objects(s, SLOTS)[0]]
grid += [names[n] for s in range(STYLES) for n in grid_objects(s, SLOTS)[1]]
blocks[cells], children[cells] = container(cells, "bc_mt_cells", True), grid
children[BOARD] = [i for i in children[BOARD] if i not in grid] + [cells, captain]

# the captain grid: copies, the new EX cells copied from the cells two before them
sources = {}
for s in range(STYLES):
    for part in grid_objects(s, CAPTAIN_SLOTS):
        for n in part:
            m = re.match(r"bc_mt_(\d)_3_(\d+)_(\w+)$", n)
            source = n
            while source not in names:
                c = int(re.match(r"bc_mt_\d_3_(\d+)_", source)[1])
                source = f"bc_mt_{s}_3_{c - COLUMNS}_{m[3]}"
            sources[n] = names[source]
captain_grid = []
for n in [n for s in range(STYLES) for n in grid_objects(s, CAPTAIN_SLOTS)[0]] + \
         [n for s in range(STYLES) for n in grid_objects(s, CAPTAIN_SLOTS)[1]]:
    block = copy_objects(p, [sources[n]], {sources[n]: new_id})
    block[1] = f"  Name: {n}"
    blocks[new_id], children[new_id] = block, []
    captain_grid.append(new_id)
    new_id += 1
blocks[captain], children[captain] = container(captain, "bc_mt_cells_captain", False), captain_grid

new = renumber(p, blocks, children)
cells, captain = new[cells], new[captain]
grid_names = {p.get(i, "Name"): i for i in p.children(cells)}
captain_names = {p.get(i, "Name"): i for i in p.children(captain)}

def box(id_):
    # bottom and top in the parent, with the object's scale
    s = p.vec(id_, "Scale")[1]
    y, h, py = p.vec(id_, "Position")[1], p.vec(id_, "SizeDelta")[1] * s, p.vec(id_, "Pivot")[1]
    return y - py * h, y + (1 - py) * h

def move(id_, bottom, top):
    # keeps x and the scale; sets the object's height to top - bottom
    s = p.vec(id_, "Scale")[1]
    w = p.vec(id_, "SizeDelta")[0]
    py = p.vec(id_, "Pivot")[1]
    p.place(id_, pos=(p.vec(id_, "Position")[0], bottom + py * (top - bottom)), size=(w, (top - bottom) / s))

def shift(id_, dy):
    x, y = p.vec(id_, "Position")[:2]
    p.place(id_, pos=(x, y + dy))

# the stock layout's vertical parts, from style 0
g = lambda n: grid_names[n]
ranks = []
for r in range(len(SLOTS)):
    panel_bottom, panel_top = box(g(f"bc_mt_0_{r}_base"))
    label_bottom, label_top = box(g(f"bc_mt_0_{r}_label"))
    rows = [box(g(f"bc_mt_0_{r}_{c}_base")) for c in range(0, SLOTS[r], COLUMNS)]
    ranks.append(dict(pad_top=panel_top - label_top, label=label_top - label_bottom, gap=label_bottom - rows[0][1],
                      cell=rows[0][1] - rows[0][0], row_gap=rows[0][0] - rows[1][1],
                      pad_bottom=rows[-1][0] - panel_bottom, top=panel_top, bottom=panel_bottom))
top, bottom = ranks[0]["top"], ranks[-1]["bottom"]
fixed = sum(r["label"] for r in ranks)
extra = sum((CAPTAIN_SLOTS[r] - SLOTS[r]) // COLUMNS * (ranks[r]["cell"] + ranks[r]["row_gap"]) for r in range(len(SLOTS)))
k = (top - bottom - fixed) / (top - bottom - fixed + extra)

# the captain layout, from rank 1's panel top down
for s in range(STYLES):
    c_ = lambda n: captain_names[n.format(s=s)]
    y = top
    for r, rank in enumerate(ranks):
        panel_top = y
        y -= rank["pad_top"] * k
        label_center = y - rank["label"] / 2
        dy = label_center - sum(box(grid_names[f"bc_mt_{s}_{r}_label"])) / 2
        for kind in ("ttl", "label", "count"):
            shift(c_(f"bc_mt_{{s}}_{r}_{kind}"), dy)
        y -= rank["label"] + rank["gap"] * k
        for row in range(CAPTAIN_SLOTS[r] // COLUMNS):
            if row:
                y -= rank["row_gap"] * k
            cell_top, cell_bottom = y, y - rank["cell"] * k
            for c in range(row * COLUMNS, (row + 1) * COLUMNS):
                base, frame = c_(f"bc_mt_{{s}}_{r}_{c}_base"), c_(f"bc_mt_{{s}}_{r}_{c}_frame")
                inset = box(frame)[0] - box(base)[0]
                move(base, cell_bottom, cell_top)
                move(frame, cell_bottom + inset, cell_top - inset)
                for state in ("on", "off"):
                    move(c_(f"bc_mt_{{s}}_{r}_{c}_{state}"), cell_bottom, cell_top)
            y = cell_bottom
        y -= rank["pad_bottom"] * k
        move(c_(f"bc_mt_{{s}}_{r}_base"), y, panel_top)
        y -= (rank["bottom"] - ranks[r + 1]["top"]) * k if r + 1 < len(ranks) else 0

# Text refs for the captain grid's texts and plain refs for both grids, at the end of CharaInfo.Powers
texts = [captain_names[n] for s in range(STYLES) for n in grid_objects(s, CAPTAIN_SLOTS)[1]]
refs = [l for t in texts for l in ("      - ComponentName: Text", "        Index: 0", f"        ObjectRefId: {t}")]
refs += [l for g_ in (cells, captain) for l in ("      - ComponentName: ''", "        Index: -1", f"        ObjectRefId: {g_}")]
i = p.lines.index("      Powers:", *p.range(0)) + 1
while p.lines[i].startswith("      - ") or p.lines[i].startswith("        "):
    i += 1
p.lines[i:i] = refs
p.reindex()

p.save(sys.argv[2])
print(f"scale {k:.4f}, cell height {ranks[0]['cell']:.2f} -> {ranks[0]['cell'] * k:.2f}, bc_mt_cells {cells}, "
      f"bc_mt_cells_captain {captain}, first texts {grid_names['bc_mt_0_0_label']}, {captain_names['bc_mt_0_0_label']}")
