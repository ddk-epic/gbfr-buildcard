# Usage: python grid_skills.py <status01.prfb.yaml> <ability_info01_02.prfb.yaml> <out.prfb.yaml>
# Replaces the skills block with the Skills screen's cards, reduced to icon, name and element tag, in a 2x2 grid under a
# title bar on a status-style panel in the green section.
import sys
from prefab import Prefab
from card import PARENT, S, add_powers, card, copy_objects, keep_components

p = Prefab(sys.argv[1])
src = Prefab(sys.argv[2])

OLD_BLOCK = 275  # loc_chr_status03
OLD_CARDS = [281, 303, 325, 347]
PANEL_SOURCE = 76  # status_base01, the status block's frame
GEAR = 1123  # bc_weapon, whose scale the title takes
TITLE_SOURCE = 1540  # ttl01, the Sigils title bar, then its text
TITLE_TEXT_ID = "TXT_PAU_ABILITY"  # the stock skills block's title
TITLE_TEXT = 30  # the title text's top above the bar's top
TITLE_GAP = 46  # the bar's top above the cells, as loc_gene01 below the Sigils title
CARDS = [4, 61, 118, 175]  # ability_set01_btn04, 03, 01, 02: slots 1 to 4
CARD_OBJECTS = range(4, 232)

# offsets from a card's first object
BASE, SET, ICON, ELEMENT, POSITIONS, BUTTON, BUTTON_KEY, NAME = 2, 3, 4, 6, 9, 14, 36, 56
NAMES = {BASE: "loc_base01", SET: "base01_set01", ICON: "loc_icon_ability", ELEMENT: "loc_elem01",
         POSITIONS: "loc_icon_pos", BUTTON: "loc_guide_button", BUTTON_KEY: "loc_guide_button_key", NAME: "text01"}

# sharecard pixels, the green section
LEFT, RIGHT, TOP, BOTTOM = 16, 576, 1142, 1404
PAD_X, PAD_Y = 10, 18.5  # border and padding
ICON_PAD = 5  # pl-1 of sharecard's compact skill cell
ICON_PX = 85  # size-17
ICON_GAP = 10

# block units, at the status block's scale
W = 1000
ICON_SIZE = 184  # loc_icon_ability
NAME_SIZE = 40
CAP, SINK = 0.85, 0.45  # cap height, and a middle-aligned text's baseline below its centre, per font size
NAME_H = 50  # one line
ELEMENT_PAD = 23  # loc_elem01's left padding before the element icon
ELEMENT_RAISE = 14  # the element icon's and text's centre above the bar's
ELEMENT_H = 50  # the element icon's visible height
NAME_GAP = 14  # the name's baseline to the element icon's top
BAR_H = 28  # loc_elem01
MIDDLE_LEFT = 3  # VerticalLayoutGroup.ChildAlignment

scale = (RIGHT - LEFT) * S / W
U = scale / S  # sharecard pixels per unit
title_scale = p.vec(GEAR, "Scale")[0]
title_k = title_scale / scale
H = (BOTTOM - TOP) / U
heading_h = (TITLE_TEXT + TITLE_GAP) * title_k
CELL_W, CELL_H = (W - 2 * PAD_X / U) / 2, (H - 2 * PAD_Y / U - heading_h) / 2
cells_y = -heading_h / 2
icon_scale = ICON_PX / U / ICON_SIZE
icon_x = (ICON_PAD + ICON_PX / 2) / U  # from the cell's left edge
name_x = (ICON_PAD + ICON_PX + ICON_GAP) / U

# the text group, from the element bar's left edge to the cell's right edge
text_w = CELL_W - name_x + ELEMENT_PAD
baseline = NAME_H / 2 - SINK * NAME_SIZE  # above the name rect's bottom
icon_top = ELEMENT_RAISE + ELEMENT_H / 2  # above the bar's centre
spacing = icon_top - BAR_H / 2 + NAME_GAP - baseline
# bottom padding centres the visible stack, cap top to element icon bottom
inset_top = NAME_H - baseline - NAME_SIZE * CAP
inset_bottom = ELEMENT_RAISE + BAR_H / 2 - ELEMENT_H / 2
padding_bottom = inset_top - inset_bottom
content_h = NAME_H + spacing + BAR_H

for old in CARDS:
    for offset, name in NAMES.items():
        assert src.get(old + offset, "Name") == name, (old + offset, name)

def group_key(old):
    return 10000 + old

container = max(p.starts) + 1
panel = container + 1
title, title_text = panel + 1, panel + 2
# depth-first: the text group follows the icon under base01_set01 and holds the name, then the tag
order = []
for old in CARDS:
    order += [*range(old, old + ELEMENT), group_key(old), old + NAME, *range(old + ELEMENT, old + NAME)]
ids = {old: title_text + 1 + i for i, old in enumerate(order)}
frames = {old + offset for old in CARDS for offset in (BASE, SET)}
hidden = {old + offset for old in CARDS for offset in (POSITIONS, BUTTON, BUTTON_KEY)}
names = {old + NAME: old for old in CARDS}
bases = {old + BASE: old for old in CARDS}
sets = {old + SET: old for old in CARDS}

def edit(old, block):
    if old in bases:
        block.remove(f"  - {old + NAME - BASE}")
    if old in sets:
        block[block.index(f"  - {old + ELEMENT - SET}")] = f"  - {group_key(sets[old])}"
    if old in names:
        block[block.index("      Margin: 0, 0, 0, 0")] = f"      Margin: {ELEMENT_PAD}, 0, 0, 0"
        i = block.index("  Active: true")
        block[i:i] = ["  - ComponentName: ContentSizeFitter", "    Component:", "      HorizontalFit: 0",
                      "      VerticalFit: 2", "      Enable: true"]
    if old in CARDS:
        block = keep_components(block, ["AbilityInfo"])
    if old in frames:
        i = block.index("      Color: 1, 1, 1, 1")
        block[i] = "      Color: 1, 1, 1, 0"
    if old in hidden and "  Active: true" in block:
        block[block.index("  Active: true")] = "  Active: false"
    return block

def text_group(old):
    return [f"- Id: {ids[group_key(old)]}", "  Name: bc_skill_text", "  Children:", f"  - {ids[old + NAME]}",
            f"  - {ids[old + ELEMENT]}", "  Components:", "  - ComponentName: VerticalLayoutGroup", "    Component:",
            f"      Padding: 0, 0, 0, {round(padding_bottom)}", f"      Spacing: {round(spacing)}",
            f"      ChildAlignment: {MIDDLE_LEFT}", "      ChildControlWidth: false", "      ChildControlHeight: false",
            "      ChildScaleWidth: false", "      ChildScaleHeight: false", "      ChildForceExpandWidth: false",
            "      ChildForceExpandHeight: false", "      Enable: true", *rect]

rect = ["  Active: true", "  Position: 0, 0, 0", "  Rotation: 0, 0, 0, 1", "  Scale: 1, 1, 1", "  Pivot: 0.5, 0.5",
        "  AnchorPoint: 0, 0", "  AnchorMin: 0.5, 0.5", "  AnchorMax: 0.5, 0.5", "  OffsetMin: 0, 0", "  OffsetMax: 0, 0",
        "  SizeDelta: 0, 0"]
cards = []
for old in CARDS:
    cards += copy_objects(src, list(range(old, old + ELEMENT)), ids, edit)
    cards += text_group(old)
    cards += copy_objects(src, [old + NAME, *range(old + ELEMENT, old + NAME)], ids, edit)
cards = [line for line in cards if line != ""]  # the source file's trailing newline

def title_edit(old, block):
    if old == TITLE_SOURCE:
        block[1] = "  Name: bc_skills_ttl"
    else:
        i = block.index("  - ComponentName: TextSetter") + 2
        block[i] = f"      TextID: {TITLE_TEXT_ID}"
    return block

title_lines = copy_objects(p, [TITLE_SOURCE, TITLE_SOURCE + 1], {TITLE_SOURCE: title, TITLE_SOURCE + 1: title_text},
                           title_edit)

start, end = p.range(PANEL_SOURCE)
panel_lines = p.lines[start:end]
panel_lines = panel_lines[panel_lines.index("  Components:"):panel_lines.index("  Active: true")]

start, end = p.range(PARENT)
last_child = max(i for i in range(start, end) if p.lines[i].startswith("  - "))
p.lines.insert(last_child + 1, f"  - {container}")
end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline
p.lines[end:end] = [f"- Id: {container}", "  Name: bc_skills", "  Children:", f"  - {panel}", f"  - {title}",
                    *[f"  - {ids[old]}" for old in CARDS], *rect,
                    f"- Id: {panel}", "  Name: bc_skills_base", *panel_lines, *rect, *title_lines, *cards]
p.reindex()

p.place(container, pos=card((LEFT + RIGHT) / 2, (TOP + BOTTOM) / 2), size=(W, H))
p.set(container, "Scale", (scale, scale, 1))
p.place(panel, pos=(0, 0), size=(W, H))
p.set(title, "Scale", (title_k, title_k, 1))
p.place(title, pos=(0, H / 2 - PAD_Y / U - TITLE_TEXT * title_k))

for slot, old in enumerate(CARDS):
    x = CELL_W / 2 * (1 if slot % 2 else -1)
    y = cells_y + CELL_H / 2 * (-1 if slot // 2 else 1)
    p.place(ids[old], pos=(x, y))

    p.place(ids[old + BASE], size=(CELL_W, CELL_H))
    p.place(ids[old + SET], size=(CELL_W, CELL_H))
    p.repin(ids[old + BASE])

    icon = ids[old + ICON]
    p.set(icon, "Scale", (icon_scale, icon_scale, 1))
    p.set(icon, "AnchorMin", (0, 0.5))
    p.set(icon, "AnchorMax", (0, 0.5))
    p.place(icon, pos=(icon_x - CELL_W / 2, 0))
    p.repin(icon)

    # children at the group's one-line positions, anchored top-left like the game's layout group children
    group = ids[group_key(old)]
    p.place(group, pos=(name_x - ELEMENT_PAD + text_w / 2 - CELL_W / 2, 0), size=(text_w, CELL_H))
    top = content_h / 2 + padding_bottom / 2
    for id_, y, size in ((ids[old + NAME], top - NAME_H / 2, (text_w, NAME_H)),
                         (ids[old + ELEMENT], top - NAME_H - spacing - BAR_H / 2, None)):
        p.set(id_, "AnchorMin", (0, 1))
        p.set(id_, "AnchorMax", (0, 1))
        p.place(id_, pos=(-text_w / 2, y), size=size, pivot=(0, 0.5))
        p.repin(id_)

p.set(OLD_BLOCK, "Active", "false")

# CharaInfo.Ability on object 0
start, end = p.range(0)
i = p.lines.index("      Ability:", start, end)
for old, new in zip(OLD_CARDS, CARDS):
    j = p.lines.index(f"        ObjectRefId: {old}", i, end)
    p.lines[j] = f"        ObjectRefId: {ids[new]}"

# plain object refs, for CardWriter to wrap long names
add_powers(p, [ids[old + NAME] for old in CARDS], component="")

p.save(sys.argv[3])
print(f"ids {container}..{max(p.starts)}, scale {scale:.4f}, cell {CELL_W:.1f}x{CELL_H:.1f}, "
      f"text group {text_w:.1f}x{CELL_H:.1f}, spacing {spacing:.1f}, bottom padding {padding_bottom:.1f}")
