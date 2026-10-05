# Usage: python restyle_master_traits.py <status01.prfb.yaml> <out.prfb.yaml>
# Restyles the master traits board as the game's Master Traits list (skillboard_window01): a panel and a title bar per
# style rank, and a base and a frame per cell. Renumbers the Ids after the board.
import sys
from prefab import Prefab, f, renumber
from card import S, image, rect

p = Prefab(sys.argv[1])

BOARD = 463  # bc_mtraits
STYLES, RANKS = 3, 4
SLOTS = [4, 8, 8, 10]

ATLAS = "atlas/pause_skillboard"
FRAME = (ATLAS, "ps_sboard_list01")  # frame01
BASE = (ATLAS, "ps_sboard_list02")  # loc_base and base01
TITLE_BARS = [(f"layouts/pause/skillboard/noatlastextures/{n}", n)
              for n in ("ps_sboard_ttl_base01", "ps_sboard_ttl_base02", "ps_sboard_ttl_base03", "ps_sboard_ttl_base04")]
PANEL_COLOR = (0.05490196, 0.11764706, 0.23137255), 0.5019608  # loc_base
CELL_COLOR = (0.15294118, 0.105882354, 0.20784314), 0.6  # base01
TITLE_TEXT_COLOR = (1, 1, 1), 0.69803923  # ttl_text01
ON_COLOR = (0.98039216, 0.98039216, 0.9411765), 1  # text01
OFF_COLOR = (0.6392157, 0.6784314, 0.6784314), 1  # text02
CELL_MATERIAL, CELL_LANGUAGE = "fonts/fot_skipstd_b_sdf_ds01", "data/language/ld_skipstd_b_sdf_ds01"
CELL_LINE_SPACING = 4  # the cell texts' English line spacing
# game units: a cell, its frame, a title bar
CELL_H, FRAME_INSET, TITLE_H = 252, 6, 74
SLICED = 1  # Image.Type

# sharecard pixels
PANEL_X, PANEL_TOP, PANEL_BOTTOM = 10, 5, 6  # the panel around the rank's title and cells
TITLE_X, TITLE_BAR_H = 3, 31  # the title bar's inset in the panel; its height, centred on the rank label
TEXT_SHIFT = 7  # the cell texts' left edge moved right, past the frame's corner

names = {p.get(i, "Name"): i for i in p.children(BOARD)}
blocks = {i: p.lines[slice(*p.range(i))] for i in p.starts}
children = {i: p.children(i) for i in p.starts}

def box(id_):
    # left, bottom, right, top
    (x0, y0), (x1, y1) = p.vec(id_, "OffsetMin"), p.vec(id_, "OffsetMax")
    return x0, y0, x1, y1

def image_block(id_, name, color, sprite, center, size, scale):
    comps = image(*color, sprite)
    comps[comps.index("      Type: 0")] = f"      Type: {SLICED}"
    comps[comps.index("      FillCenter: false")] = "      FillCenter: true"
    block = [f"- Id: {id_}", *rect(name, comps, (0.5, 0.5))]
    w, h = size[0] / scale, size[1] / scale
    x, y = center
    for key, value in (("Position", (x, y, 0)), ("Scale", (scale, scale, 1)), ("AnchorPoint", (x, y)),
                       ("OffsetMin", (x - w / 2, y - h / 2)), ("OffsetMax", (x + w / 2, y + h / 2)),
                       ("SizeDelta", (w, h))):
        block[block.index(next(l for l in block if l.startswith(f"  {key}: ")))] = f"  {key}: {', '.join(f(v) for v in value)}"
    return block

def shift_left_edge(id_, dx):
    block = blocks[id_]
    for key in ("Position", "AnchorPoint", "OffsetMin"):
        i = next(i for i, l in enumerate(block) if l.startswith(f"  {key}: "))
        v = p.vec(id_, key)
        v[0] += dx
        block[i] = f"  {key}: {', '.join(f(c) for c in v)}"
    i = next(i for i, l in enumerate(block) if l.startswith("  SizeDelta: "))
    w, h = p.vec(id_, "SizeDelta")
    block[i] = f"  SizeDelta: {f(w - dx)}, {f(h)}"

def set_text(id_, color, material=None):
    block = blocks[id_]
    i = next(i for i, l in enumerate(block) if l.startswith("      Color: "))
    block[i] = f"      Color: {', '.join(f(c) for c in color[0])}, {f(color[1])}"
    if material:
        block[next(i for i, l in enumerate(block) if l.startswith("      MaterialPath: "))] = f"      MaterialPath: {CELL_MATERIAL}"
        block[block.index("      LanguageData: data/language/ld_skipstd_b_sdf_material")] = f"      LanguageData: {CELL_LANGUAGE}"
        i = block.index("      - Language: Eng")
        i = next(j for j in range(i, len(block)) if block[j].startswith("        LineSpaching: "))
        assert block[i - 1] == "        EnableLS: true"
        block[i] = f"        LineSpaching: {CELL_LINE_SPACING}"

new_id = max(p.starts) + 1
images = []  # bc_mtraits' new image children, in drawing order
for s in range(STYLES):
    border = names[f"bc_mt_{s}_border"]
    col_left, _, col_right, _ = box(border)
    panels, bars, cells = [], [], []
    for r in range(RANKS):
        label = names[f"bc_mt_{s}_{r}_label"]
        _, label_bottom, _, label_top = box(label)
        fills = [names[f"bc_mt_{s}_{r}_{c}_fill"] for c in range(SLOTS[r])]
        cell_bottom = min(box(i)[1] for i in fills)
        cell_h = box(fills[0])[3] - box(fills[0])[1]
        k = cell_h / CELL_H

        left, right = col_left + PANEL_X * S, col_right - PANEL_X * S
        top, bottom = label_top + PANEL_TOP * S, cell_bottom - PANEL_BOTTOM * S
        panel = border if r == 0 else new_id
        new_id += panel != border
        blocks[panel] = image_block(panel, f"bc_mt_{s}_{r}_base", PANEL_COLOR, BASE, ((left + right) / 2, (top + bottom) / 2),
                                    (right - left, top - bottom), k)
        panels.append(panel)

        bar_h = TITLE_BAR_H * S
        bars.append(new_id)
        blocks[new_id] = image_block(new_id, f"bc_mt_{s}_{r}_ttl", ((1, 1, 1), 1), TITLE_BARS[r],
                                     ((left + right) / 2, (label_top + label_bottom) / 2),
                                     (right - left - 2 * TITLE_X * S, bar_h), bar_h / TITLE_H)
        new_id += 1
        set_text(label, TITLE_TEXT_COLOR)
        set_text(names[f"bc_mt_{s}_{r}_count"], TITLE_TEXT_COLOR)

        for c, fill in enumerate(fills):
            x0, y0, x1, y1 = box(fill)
            center = ((x0 + x1) / 2, (y0 + y1) / 2)
            blocks[fill] = image_block(fill, f"bc_mt_{s}_{r}_{c}_base", CELL_COLOR, BASE, center, (x1 - x0, y1 - y0), k)
            blocks[new_id] = image_block(new_id, f"bc_mt_{s}_{r}_{c}_frame", ((1, 1, 1), 1), FRAME, center,
                                         (x1 - x0 - 2 * FRAME_INSET * k, y1 - y0 - 2 * FRAME_INSET * k), k)
            cells += [fill, new_id]
            new_id += 1
            for state, color in (("on", ON_COLOR), ("off", OFF_COLOR)):
                set_text(names[f"bc_mt_{s}_{r}_{c}_{state}"], color, CELL_MATERIAL)
                shift_left_edge(names[f"bc_mt_{s}_{r}_{c}_{state}"], TEXT_SHIFT * S)
    images += panels + bars + cells

for i in range(max(p.starts) + 1, new_id):
    children[i] = []
texts = [i for i in children[BOARD] if i not in images and not p.get(i, "Name").endswith(("_border", "_fill"))]
children[BOARD] = images + texts

ids = renumber(p, blocks, children)

p.save(sys.argv[2])
first_text = min(ids[i] for i in texts)
print(f"ids {ids[BOARD]}..{ids[texts[-1]]}, texts from {first_text}, shift after the board {ids[texts[-1]] - texts[-1]}")
