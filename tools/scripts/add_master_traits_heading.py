# Usage: python add_master_traits_heading.py <status01.prfb.yaml> <var00_frame_header02.prfb.yaml>
#        <skillboard_window01.prfb.yaml> <out.prfb.yaml>
# Heads the master traits board like the Master Traits menu (var00_frame_header02: the ornament with its line across
# the board and the "Master Traits" title) with each style's name and stars on the line's right end, styles the style
# titles like the style pages' (skillboard_info03's info01_text01), and removes the style labels and the perk summary.
# Renumbers the Ids.
import sys
from prefab import Prefab, f, renumber
from card import S, card, copy_objects, rect

p = Prefab(sys.argv[1])
header = Prefab(sys.argv[2])
window = Prefab(sys.argv[3])

BOARD = 463  # bc_mtraits
SUMMARY = 462  # bc_text01
STYLES = ["Insight", "Essence", "Crux"]
LINE, TITLE = 4, 6  # line01, title_text01
TITLE_TEXT_ID = "TXT_PAU_TTL_SKL_BD"  # "Master Traits"
HEADER_H = 400
TITLE_X, TITLE_Y = 256, 128  # title_text01's left edge and centre from the header's top-left corner
LINE_Y = 190  # line01's line below its top
TITLE_SIZE, RANK_TITLE_SIZE = 96, 40  # title_text01, skillboard_window01's ttl_text01
STARS = 2578  # skillboard_window01's loc_level02: three stars, each a base with its glow and icon
STAR_GLOWS = {2582, 2586, 2590}  # glow01_add to glow03_add, inactive
STAR_SIZE, STAR_PAGE_SCALE, PAGE_TITLE_SIZE = 144, 0.5, 56  # a star, loc_level02's scale on the style page, its title
STYLE_TITLE_COLOR = "0.98039216, 0.98039216, 0.9411765, 1"  # info01_text01
STYLE_TITLE_GRADIENT = [("ColorTL", "0.98039216, 0.98039216, 0.9411765, 1"), ("ColorTR", "0.98039216, 0.98039216, 0.9411765, 1"),
                        ("ColorBL", "0.8627451, 0.9607843, 1, 1"), ("ColorBR", "0.8627451, 0.9607843, 1, 1")]
PERK_GAP, PERKS_GAP = 6, 24  # a name to its stars, a style's stars to the next name
MIDDLE_LEFT, MIDDLE_RIGHT = 3, 5  # ChildAlignment
PERK_NAME_SIZE = PAGE_TITLE_SIZE / STAR_PAGE_SCALE  # the names' size beside unscaled stars

# sharecard pixels, the blue section
LEFT, TOP, WIDTH = 1380, 16, 1484

names = {p.get(i, "Name"): i for i in p.children(BOARD)}
heading = names["bc_mt_heading"]
labels = [names[f"bc_mt_{s}_style"] for s in range(len(STYLES))]
titles = [names[f"bc_mt_{s}_title"] for s in range(len(STYLES))]

def font_size(id_):
    start, end = p.range(id_)
    return float(next(l for l in p.lines[start:end] if l.startswith("      FontSize: ")).split(": ")[1])

# the header at the size where its title keeps its ratio to the rank labels; the style names halfway between the
# style titles' and the cells' text size
SCALE = font_size(names["bc_mt_0_0_label"]) / RANK_TITLE_SIZE
ROW_SCALE = (font_size(names["bc_mt_0_title"]) + font_size(names["bc_mt_0_0_0_on"])) / 2 / PERK_NAME_SIZE

blocks = {i: p.lines[slice(*p.range(i))] for i in p.starts}
children = {i: p.children(i) for i in p.starts}

def set_line(block, key, value):
    i = next(i for i, l in enumerate(block) if l.strip().startswith(f"{key}: "))
    block[i] = block[i][:block[i].index(key)] + f"{key}: {value}"

def components(prefab, id_):
    start, end = prefab.range(id_)
    block = prefab.lines[start:end]
    return block[block.index("  Components:") + 1:block.index("  Active: true")]

def add(id_, lines, kids):
    blocks[id_], children[id_] = lines, kids

def layout_group(spacing, alignment):
    # loc_level02's HorizontalLayoutGroup and ContentSizeFitter
    comps = components(window, STARS)
    set_line(comps, "Spacing", f(spacing / ROW_SCALE))
    set_line(comps, "ChildAlignment", alignment)
    return comps

def top_left(block):
    # anchored like a layout group's children, a star high
    set_line(block, "AnchorMin", "0, 1")
    set_line(block, "AnchorMax", "0, 1")
    set_line(block, "SizeDelta", f"0, {STAR_SIZE}")
    return block

new_id = max(p.starts) + 1

# the ornament line, after the background
line = new_id
new_id += 1
add(line, copy_objects(header, [LINE], {LINE: line}), [])
after_bg = children[BOARD].index(names["bc_mt_bg"]) + 1
children[BOARD].insert(after_bg, line)

# the style names and stars, after the line
perks = new_id
new_id += 1
perk_groups = []
perk_names = []
star_glows = []
for s, style in enumerate(STYLES):
    group, name, stars = new_id, new_id + 1, new_id + 2
    new_id += 3
    perk_groups.append(group)
    add(group, [f"- Id: {group}", *top_left(rect(f"bc_mt_perk_{s}", layout_group(PERK_GAP, MIDDLE_LEFT), (0, 0.5)))],
        [name, stars])
    blocks[group].insert(2, "  Children:")

    comps = components(header, TITLE)
    comps = comps[:comps.index("  - ComponentName: TextSetter")] + comps[comps.index("  - ComponentName: ContentSizeFitter"):]
    set_line(comps, "Text", "''")
    set_line(comps, "FontSize", f(PERK_NAME_SIZE))
    set_line(comps, "Alignment", 513)
    add(name, [f"- Id: {name}", *top_left(rect(f"bc_mt_perk_{s}_name", comps, (0, 0.5)))], [])
    perk_names.append(name)

    objects = [i for i in window.starts if STARS <= i <= max(STAR_GLOWS) and i not in STAR_GLOWS]
    ids = {old: stars + i for i, old in enumerate(objects)}
    ids_lines = copy_objects(window, objects, ids)
    starts = [i for i, l in enumerate(ids_lines) if l.startswith("- Id: ")] + [len(ids_lines)]
    for a, b in zip(starts, starts[1:]):
        block = ids_lines[a:b]
        id_ = int(block[0][6:])
        kids = []
        if "  Children:" in block:
            i = block.index("  Children:") + 1
            while i < len(block) and block[i].startswith("  - "):
                kids.append(int(block[i][4:]))
                i += 1
        add(id_, block, kids)
    set_line(blocks[stars], "Name", f"bc_mt_perk_{s}_stars")
    set_line(blocks[stars], "Scale", "1, 1, 1")
    set_line(blocks[stars], "AnchorMin", "0, 1")
    set_line(blocks[stars], "AnchorMax", "0, 1")
    star_glows += [ids[old] for old in objects if window.get(old, "Name").startswith("icon0") and old not in STAR_GLOWS
                   and window.get(old, "Name").endswith("_add")]
    new_id += len(objects)

add(perks, [f"- Id: {perks}", *rect("bc_mt_perks", layout_group(PERKS_GAP, MIDDLE_RIGHT), (1, 0.5))], perk_groups)
blocks[perks].insert(2, "  Children:")
children[BOARD].insert(after_bg + 1, perks)

# the title: title_text01's components on bc_mt_heading
comps = components(header, TITLE)
set_line(comps, "FontSize", f(TITLE_SIZE * SCALE))
set_line(comps, "TextID", TITLE_TEXT_ID)
block = blocks[heading]
blocks[heading] = block[:block.index("  Components:") + 1] + comps + block[block.index("  Active: true"):]

# the style titles in info01_text01's style
for t in titles:
    block = blocks[t]
    set_line(block, "MaterialPath", "fonts/fot_skipstd_b_sdf_ds01")
    set_line(block, "LanguageData", "data/language/ld_skipstd_b_sdf_ds01")
    set_line(block, "Color", STYLE_TITLE_COLOR)
    set_line(block, "IsGradient", "true")
    set_line(block, "ColorMode", 2)
    for key, value in STYLE_TITLE_GRADIENT:
        set_line(block, key, value)

# the style labels, the perk summary and their and the heading's Text refs go; the style names get Text refs and the
# star glows plain refs
for label in labels:
    children[BOARD].remove(label)
    del blocks[label]
children[p.parents[SUMMARY]].remove(SUMMARY)
del blocks[SUMMARY]
powers = blocks[0]
for id_ in labels + [heading, SUMMARY]:
    i = powers.index(f"        ObjectRefId: {id_}")
    assert powers[i - 2] == "      - ComponentName: Text"
    del powers[i - 2:i + 1]
i = powers.index("      Powers:") + 1
while powers[i].startswith("      - ") or powers[i].startswith("        "):
    i += 1
powers[i:i] = ([l for n in perk_names for l in ("      - ComponentName: Text", "        Index: 0", f"        ObjectRefId: {n}")]
               + [l for g in star_glows for l in ("      - ComponentName: ''", "        Index: -1", f"        ObjectRefId: {g}")])

new = renumber(p, blocks, children)
line, perks, heading = new[line], new[perks], new[heading]
titles = [new[t] for t in titles]

left, top = card(LEFT, TOP)
w = WIDTH * S / SCALE
p.set(line, "Scale", (SCALE, SCALE, 1))
p.place(line, pos=(left, top), size=(w, HEADER_H))
p.place(heading, pos=(left + TITLE_X * SCALE, top - TITLE_Y * SCALE), size=(w * SCALE / 2, TITLE_SIZE * SCALE), pivot=(0, 0.5))
line_y = top - LINE_Y * SCALE
base = new[names["bc_mt_0_0_base"]]
k = p.vec(base, "Scale")[0]
panel_top = p.vec(base, "Position")[1] + p.vec(base, "SizeDelta")[1] * k / 2
last_base = new[names[f"bc_mt_{len(STYLES) - 1}_0_base"]]
panel_right = p.vec(last_base, "Position")[0] + p.vec(last_base, "SizeDelta")[0] * k / 2

# the perks right-aligned with the last style's rank panels, halfway between their bottom centred at the title's
# height and the line
p.set(perks, "Scale", (ROW_SCALE, ROW_SCALE, 1))
bottom = top - TITLE_Y * SCALE - STAR_SIZE * ROW_SCALE / 2
p.place(perks, pos=(panel_right, (bottom + line_y) / 2), size=(0, STAR_SIZE), pivot=(1, 0))

# style titles centred between the line and the rank panels
for t in titles:
    x = p.vec(t, "Position")[0]
    w_t, h_t = p.vec(t, "SizeDelta")
    p.place(t, pos=(x, (line_y + panel_top) / 2 + h_t / 2))

p.save(sys.argv[4])
print(f"scale {SCALE}, title size {f(TITLE_SIZE * SCALE)}, row scale {ROW_SCALE:.4f}, line {line}, perks {perks}, "
      f"heading {heading}, style titles {titles}, style names {[new[n] for n in perk_names]}, "
      f"star glows {[new[g] for g in star_glows]}")
