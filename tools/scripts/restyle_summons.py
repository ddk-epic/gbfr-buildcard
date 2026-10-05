# Usage: python restyle_summons.py <status01.prfb.yaml> <summon_list01.prfb.yaml> <out.prfb.yaml>
# Restyles the summon slots as sharecard's summon cells: the name on summon_list01's slot band, mirrored and faded to the
# right, the trait and equip bonus rows below it and the summon's icon art on the cell's last third, faded at both sides.
# Renumbers the Ids.
import sys
from prefab import Prefab, f, renumber
from card import S, copy_objects, rect

p = Prefab(sys.argv[1])
source = Prefab(sys.argv[2])

SLOTS = 4
BAND = 14  # place01_set01
ICON = 106  # icon01
ICON_W, ICON_H = 320, 440  # cmn_icsmn sprites
FADE = ("atlas/pause_pause_common", "ps_cmn_mask_list_w01")
FADE_PADDING = 243.07613 / 1152  # the fade sprite's transparent left padding, of its width
BAND_FADE = ("atlas/pause_pause_common", "ps_cmn_list02_mask")
BAND_FADE_PADDING = 390.0761 / 1112  # the band fade sprite's transparent right padding, of its width
NAME_COLOR = "0.98039216, 0.98039216, 0.9411765, 1"  # online_text01
NAME_MATERIAL, NAME_LANGUAGE = "fonts/fot_skipstd_b_sdf_ol09", "data/language/ld_skipstd_b_sdf_ol09"
LEVEL_H = 56  # twice the level badge's height
LEVEL_GAP = 8  # the trait name to its level
LOWER_LEFT = 6  # ChildAlignment
STATUS_SIZE, STATUS_SCALE = 40, 0.666  # the status panel labels and skill names
REMOVED = ["base01", "summon_icon01", "loc_text02", "loc_elem01"]

# slot units: the slot frame's width is the cell's
SLOT_SCALE = 0.5116
CELL_W = 1136
CELL_TOP, CELL_H = 70 + 4 * S / SLOT_SCALE, 131 * S / SLOT_SCALE
BAND_X, BAND_Y, BAND_W, BAND_H = 33, 17, 735, 60  # from the cell's top-left corner
NAME_X = 24  # from the band's left edge
ROW_ICON_LEFT = -478  # the row icon's left edge in its row
TRAIT_Y, BONUS_Y = 144, 230  # row centres below the cell's top
ART_W = CELL_W / 3
PORTRAIT_W = 1.2  # of the art's width
PORTRAIT_Y = 18 * S / SLOT_SCALE  # below the cell's centre

blocks = {i: p.lines[slice(*p.range(i))] for i in p.starts}
children = {i: p.children(i) for i in p.starts}
slots = [next(i for i in p.starts if p.get(i, "Name") == f"bc_smn_{s}") for s in range(SLOTS)]

def descendants(id_):
    out = [id_]
    for c in children[id_]:
        out += descendants(c)
    return out

def find(root, name):
    return next(i for i in descendants(root) if blocks[i][1] == f"  Name: {name}")

def drop_field(block, field):
    # removes a component field and its nested lines
    i = block.index(f"      {field}:")
    j = i + 1
    while block[j].startswith("      - ") or block[j].startswith("        "):
        j += 1
    del block[i:j]

def mask(sprite):
    return ["  - ComponentName: Mask", "    Component:", "      Sprite:", f"        TexturePath: {sprite[0]}",
            f"        SpriteName: {sprite[1]}", "      Offset: 0, 0", "      ChannelWeights: 0, 0, 0, 1",
            "      InvertMask: false", "      InvertOutsides: false", "      Enable: true"]

def world_scale(id_):
    scale = 1
    while id_ in p.parents:
        scale *= p.vec(id_, "Scale")[1]
        id_ = p.parents[id_]
    return scale

def set_line(block, key, value):
    i = next(i for i, l in enumerate(block) if l.strip().startswith(f"{key}: "))
    block[i] = block[i][:block[i].index(key)] + f"{key}: {value}"

name_size = STATUS_SIZE * STATUS_SCALE / world_scale(slots[0])

new_id = max(p.starts) + 1
added = []
for slot in slots:
    item = find(slot, "loc_item01")
    for name in REMOVED:
        old = next(c for c in children[item] if p.get(c, "Name") == name)
        children[item].remove(old)
        for i in descendants(old):
            del blocks[i]

    band_fade, band, fade, icon = new_id, new_id + 1, new_id + 2, new_id + 3
    new_id += 4
    blocks[band] = copy_objects(source, [BAND], {BAND: band})
    set_line(blocks[band], "Name", "band")
    set_line(blocks[band], "Active", "true")
    set_line(blocks[band], "Scale", "-1, 1, 1")
    blocks[icon] = copy_objects(source, [ICON], {ICON: icon})
    for id_, name, sprite in ((band_fade, "bc_smn_band", BAND_FADE), (fade, "bc_smn_art", FADE)):
        block = rect(name, mask(sprite), (0.5, 0.5))
        blocks[id_] = [f"- Id: {id_}", block[0], "  Children:", *block[1:]]
    children[band_fade], children[band], children[fade], children[icon] = [band], [], [icon], []
    children[item] = [fade, band_fade] + children[item]

    # SummonInfo: the slot's name text and the icon
    block = blocks[slot]
    drop_field(block, "Sets")
    drop_field(block, "Elements")
    i = block.index("      _57A2478C:") + 1
    block[i:i + 3] = ["      - ComponentName: SummonIconSetter", "        Index: 0", f"        ObjectRefId: {icon}"]
    i = block.index("      Names:") + 1
    del block[i + 3:i + 6]
    # the trait level after the trait name, its top on the row's centre
    trait = find(slot, "list_skill_p05_01")
    level, text = find(trait, "loc_skill_lv01"), find(trait, "loc_text")
    wrap = new_id
    new_id += 1
    layout = ["  - ComponentName: HorizontalLayoutGroup", "    Component:", "      Padding: 0, 0, 0, 0", "      Spacing: 0",
              f"      ChildAlignment: {LOWER_LEFT}", "      ChildControlWidth: false", "      ChildControlHeight: false",
              "      ChildScaleWidth: false", "      ChildScaleHeight: false", "      ChildForceExpandWidth: false",
              "      ChildForceExpandHeight: false", "      Enable: true", "  - ComponentName: ContentSizeFitter",
              "    Component:", "      HorizontalFit: 2", "      VerticalFit: 0", "      Enable: true"]
    block = rect("bc_smn_level", layout, (0.5, 0.5))
    blocks[wrap] = [f"- Id: {wrap}", block[0], "  Children:", *block[1:]]
    children[p.parents[level]].remove(level)
    children[wrap] = [level]
    children[text].append(wrap)
    set_line(blocks[text], "Spacing", LEVEL_GAP)
    added.append((band_fade, band, fade, icon, wrap))

new = renumber(p, blocks, children)
slots = [new[s] for s in slots]

left, top = -CELL_W / 2, CELL_TOP
for slot, (band_fade, band, fade, icon, wrap) in zip(slots, added):
    band_fade, band, fade, icon, wrap = new[band_fade], new[band], new[fade], new[icon], new[wrap]
    p.place(wrap, size=(0, LEVEL_H))
    # the band fade's ramp across the band
    band_fade_w = BAND_W / (1 - BAND_FADE_PADDING)
    p.place(band_fade, pos=(left + BAND_X + band_fade_w / 2, top - BAND_Y - BAND_H / 2), size=(band_fade_w, BAND_H))
    p.place(band, pos=(BAND_W / 2 - band_fade_w / 2, 0), size=(BAND_W, BAND_H))
    name = next(i for i in range(slot, slot + 60) if p.get(i, "Name") == "text01_01")
    start, end = p.range(name)
    for i in range(start, end):
        line = p.lines[i]
        if line.startswith("      MaterialPath: "):
            p.lines[i] = f"      MaterialPath: {NAME_MATERIAL}"
        elif line.startswith("      FontSize: "):
            p.lines[i] = f"      FontSize: {f(name_size)}"
        elif line.startswith("      Color: "):
            p.lines[i] = f"      Color: {NAME_COLOR}"
        elif line == "      MultiData: true":
            p.lines[i:i + 1] = ["      MultiData: false", f"      LanguageData: {NAME_LANGUAGE}"]
            p.reindex()
            break
    p.place(name, pos=(left + BAND_X + NAME_X, top - BAND_Y - BAND_H / 2))

    # the fade's opaque part over the last third, the icon centred on it
    fade_w = ART_W / (1 - FADE_PADDING)
    p.place(fade, pos=(-left - fade_w / 2, top - CELL_H / 2), size=(fade_w, CELL_H))
    w = ART_W * PORTRAIT_W
    p.place(icon, pos=(fade_w / 2 - ART_W / 2, -PORTRAIT_Y), size=(w, w * ICON_H / ICON_W))

    for row_name, y in (("list_skill_p05_01", TRAIT_Y), ("list_skill_p05_02", BONUS_Y)):
        # the trait row at the status panel labels' size; the rows' icons at the band's left edge
        row = next(c for c in p.children(slot) if p.get(c, "Name") == row_name)
        k = STATUS_SCALE / world_scale(slot) if row_name == "list_skill_p05_01" else 1
        x = left + BAND_X - ROW_ICON_LEFT * k
        p.set(row, "Scale", (k, k, 1))
        p.place(row, pos=(x, top - y))
        rows = [i for i in range(row, row + 20)]
        line = next(i for i in rows if p.get(i, "Name") == "line01")
        p.set(line, "Active", "false")
        # the level's padding and spacing as in the gear trait rows
        level = next(i for i in rows if p.get(i, "Name") == "loc_skill_lv01")
        p.replace(level, "Padding: 28, 0, 34, 0", "Padding: 28, 0, 40, 4")
        p.replace(next(i for i in rows if p.get(i, "Name") == "loc_lv01"), "Spacing: 24", "Spacing: 8")
        if row_name == "list_skill_p05_02":
            p.place(level, pos=((-left - ART_W - x) / k, p.vec(level, "Position")[1]))

p.save(sys.argv[3])
print(f"slots {slots}, objects per slot {slots[1] - slots[0]}, next {max(p.starts) + 1}")
