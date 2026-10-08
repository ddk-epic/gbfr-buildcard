# Stacks the gear screen's Weapon and Sigils sections and the equip screen's trait rows in the gear column.
from model.components import get_line, set_line, set_refs, single
from model.prefab import Ref, copy, f
from steps.layout import (CARD_ORDER, GEAR, GENE_TOP, PENDULUM_TOP, SCREEN_CARD_RATIO, SIGILS_TITLE_TOP, SKILLS_TOP, TITLE_SCALE,
                          WEAPON_TOP, card, insert)

TRAIT_FIELDS = ["Skills", "PendulumSkillObj", "PendulumSkills", "PendulumNames"]
SIGIL_ROWS = 12

# sigil rows, in loc_gene units
ROW_H = 80
EDGE = 8  # the columns' and the level's inset from the row's edges
LEVEL_W = 175
ICON_W = 72
ICON_X = 40  # the icon's centre from the column's left edge
NAME_X = 96  # the name's left edge from the column's left edge
SKILLS_INSET = 40  # loc_icon_skill's pivot, its right edge, from the row's right edge
FONT_SIZE = 40

# trait rows
LEVEL_GAP = 8  # loc_text01_03's spacing
RAISE = 4  # the level's bottom padding

# trait level gold, as SkillInfo's bonus level
LEVELS = ["lv01_text01", "lv01_num01", "text01_02", "text01_03"]
GOLD_TOP = "1, 0.9411765, 0.88235295, 1"
GOLD_BOTTOM = "1, 0.6862745, 0.4509804, 1"
OUTLINE_FROM, OUTLINE_TO = "_ol06", "_ol07"

# the weapon type under the name
CELL_FONT = 19  # bc_mt_0_0_0_off
TYPE_FONT = 36
TYPE_GAP = 37  # the name's original centre to the series line's centre
NAME_RISE = 8


def field(node, name, mapping):
    # a component field's lines, with references mapped
    lines = node.lines
    i = lines.index(f"      {name}:")
    end = i + 1
    while isinstance(lines[end], Ref) or lines[end].startswith(("       ", "      - ")):
        end += 1
    return [Ref(line.prefix, mapping[line.node]) if isinstance(line, Ref) else line for line in lines[i:end]]


def insert_after(node, line, new):
    i = node.lines.index(line) + 1
    node.lines[i:i] = new


def drop_component(node, name):
    start = node.lines.index(f"  - ComponentName: {name}")
    end = start + 1
    while isinstance(node.lines[end], Ref) or node.lines[end].startswith("    "):
        end += 1
    del node.lines[start:end]


def keep_component(node, name):
    # removes every component but the named one
    start = node.lines.index("  Components:") + 1
    end = node.lines.index("  Active: true") if "  Active: true" in node.lines else node.lines.index("  Active: false")
    kept, keep = [], False
    for line in node.lines[start:end]:
        if isinstance(line, str) and line.startswith("  - ComponentName: "):
            keep = line == f"  - ComponentName: {name}"
        if keep:
            kept.append(line)
    node.lines[start:end] = kept


def copied(source, mapping, refs=None):
    # a copy of the subtree, recording each source node's copy
    node = copy(source, refs)
    mapping.update(zip(source.walk(), node.walk()))
    return node


def weapon_panel(info01, info_weapon01, info02):
    # the Weapon section with the trait rows and the Sigils section under its root
    mapping = {}
    panel = copied(info01.root, mapping)
    keep_component(panel, "WeaponInfo")
    panel.name = "bc_weapon"
    root = panel.child("root")
    root.set("Active", True)
    level = root.find("loc_lv01/level02").child("root")
    level.child("loc_max01").set("Active", True)
    level.child("loc_exp01").set("Active", False)

    trait_source = info_weapon01.root
    image = mapping[info01.find("loc_weapon01/image_equip_00")]
    refs = {info_weapon01.find("loc_weapon02/image_equip_00"): image}
    traits = [copied(trait_source.find(name), mapping, refs) for name in ("loc_skill01", "loc_skill02")]
    sigils = [copied(info02.find(f"loc_info02/{name}"), mapping) for name in ("ttl01", "loc_gene01")]
    for i, node in enumerate(traits + sigils):
        root.add(node, 1 + i)

    # WeaponInfo's trait fields, in the class's order
    fields = {name: field(trait_source, name, mapping) for name in TRAIT_FIELDS}
    star = panel.lines.index("      Star:") + 4
    panel.lines[star:star] = fields["Skills"] + fields["PendulumSkillObj"]
    insert_after(panel, "      SkillListPendulumEntry: false", fields["PendulumSkills"] + fields["PendulumNames"])
    return panel, traits, sigils


def rework_sigil_row(row, row_w):
    # both traits' icons and names, then the level, like sharecard's sigil rows
    gene = row.child("root").child("loc_gene")
    icon, name, level, skills = (gene.child(n) for n in ("icon01", "text01_01", "loc_text01_02", "loc_icon_skill"))
    column_w = (row_w - 2 * EDGE - LEVEL_W) / 2
    columns = [-row_w / 2 + EDGE, -row_w / 2 + EDGE + column_w]
    skills_right = row_w / 2 - SKILLS_INSET

    for n in range(2):
        trait = single(name)
        trait.name = f"bc_trait0{n + 1}"
        drop_component(trait, "ContentSizeFitter")
        trait.replace("FontSize: 40", f"FontSize: {FONT_SIZE}")
        trait.replace("CharacterSpacing: -1", "CharacterSpacing: 0")
        skills.add(trait)
        # the trait's SkillInfo fills the name
        skill_icon = skills.child(f"icon_skill0{n + 1}")
        i = skill_icon.lines.index("      Icons:") + 4
        skill_icon.lines[i:i] = ["      Names:", "      - ComponentName: Text", "        Index: 0",
                                 Ref("        ObjectRefId: ", trait)]

    # the sigil icon, out of GemInfo's Sets, and the sigil name
    i = next(i for i, line in enumerate(row.lines) if isinstance(line, Ref) and line.node is icon)
    del row.lines[i - 2:i + 1]
    icon.set("Active", False)
    name.set("Active", False)

    row.place(size=(row_w, ROW_H))
    level.place(pos=(row_w / 2 - EDGE, level.vec("Position")[1]))
    skills.place(pos=(skills_right, 0))
    for n in range(2):
        skills.child(f"icon_skill0{n + 1}").place(pos=(columns[n] + ICON_X - skills_right, 0), size=(ICON_W, ICON_W))
        skills.child(f"bc_trait0{n + 1}").place(pos=(columns[n] + NAME_X - skills_right, 0),
                                                 size=(column_w - NAME_X, ROW_H))


def raise_level(level):
    left, top, right, _ = get_line(level, "Padding").split(", ")
    set_line(level, "Padding", f"{left}, {top}, {right}, {RAISE}")


def match_trait_rows(traits, sigil_rows, width):
    # the trait rows at the sigil rows' width, their levels at the right edge, levels raised as in the sigil rows
    weapon_rows = [row for loc in traits for row in loc.children if row.name.startswith("list_skill_p01_")]
    for row in weapon_rows:
        raise_level(row.find("loc_skill_lv01"))
    for row in sigil_rows:
        raise_level(row.find("loc_text01_02"))
    for row in weapon_rows:
        row.place(size=(width, row.vec("SizeDelta")[1]))
        row.repin()
        level = row.find("loc_skill_lv01")
        level.place(pos=(width / 2 - EDGE, level.vec("Position")[1]))
        # the bar's right padding shortened by as much as the gap
        pair = level.child("loc_lv01")
        left, top, right, bottom = get_line(level, "Padding").split(", ")
        gap = float(get_line(pair, "Spacing"))
        set_line(level, "Padding", f"{left}, {top}, {f(float(right) - (gap - LEVEL_GAP))}, {bottom}")
        set_line(pair, "Spacing", LEVEL_GAP)
    title = traits[1].child("loc_title01")
    title.place(size=(width, title.vec("SizeDelta")[1]))
    title.repin()
    return weapon_rows


def widen_weapon_section(info, width):
    # the dividers and the stat row stretched to the trait rows' width, the level and the gauge moved out to the sides
    dividers = [info.child("line01"), info.child("line02")]
    shift = (width - dividers[0].vec("SizeDelta")[0]) / 2
    for line in dividers:
        line.place(size=(width, line.vec("SizeDelta")[1]))
    for name, dx in (("loc_lv01", -shift), ("loc_lt01", shift), ("loc_tag01", shift)):
        node = info.child(name)
        x, y = node.vec("Position")[:2]
        node.place(pos=(x + dx, y))
    # the stat row's four cells, a quarter of its width each, edge to edge
    stats = info.child("loc_status01")
    stats_w, stats_h = stats.vec("SizeDelta")
    stats_w += 2 * shift
    stats.place(size=(stats_w, stats_h))
    for n, cell in enumerate(stats.child(name) for name in ("loc_hp01", "loc_atk01", "loc_crt01", "loc_brk01")):
        h = cell.vec("SizeDelta")[1]
        x = -stats_w / 2 + stats_w / 4 * (n + cell.vec("Pivot")[0])
        cell.place(pos=(x, cell.vec("Position")[1]), size=(stats_w / 4, h))
        cell.repin()


def gold_levels(panel):
    # the trait levels in the summon rows' gold
    for text in (node for node in panel.walk() if node.name in LEVELS):
        lines = text.lines
        for i, line in enumerate(lines):
            if not isinstance(line, str):
                continue
            key = line.strip().split(":")[0]
            indent = line[:len(line) - len(line.lstrip())]
            if key in ("ColorTL", "ColorTR"):
                lines[i] = f"{indent}{key}: {GOLD_TOP}"
            elif key in ("ColorBL", "ColorBR"):
                lines[i] = f"{indent}{key}: {GOLD_BOTTOM}"
            elif key == "MaterialPath":
                lines[i] = line.replace(OUTLINE_FROM, OUTLINE_TO)
        # the first language container is the default outline
        first = next(i for i, line in enumerate(lines) if isinstance(line, str) and line.strip() == "ContainerData:") + 1
        lines[first], lines[first + 1] = lines[first + 1], lines[first]


def weapon_type(panel, info, info_weapon01, scale):
    # the equip screen's series line and its ornaments in place of the line under the weapon name
    line = info.child("line02")
    name = line.child("loc_name01_text")
    type_text = line.add(copy(info_weapon01.find("ttl01/type_text01")))
    image = line.lines.index("  - ComponentName: Image")
    line.lines[line.lines.index("      Enable: true", image)] = "      Enable: false"
    i = panel.lines.index("      WeaponImageObj:")
    panel.lines[i:i] = ["      TypeText:", "        ComponentName: Text", "        Index: 0",
                        Ref("        ObjectRefId: ", type_text)]
    name_x, name_y = name.vec("Position")[:2]
    k = CELL_FONT / (TYPE_FONT * scale)
    type_text.set("Scale", (k, k, 1))
    type_text.place(pos=(0, name_y - TYPE_GAP))
    name.place(pos=(name_x, name_y + NAME_RISE))


def apply(ctx):
    prefab = ctx.prefab("status01")
    info01, info_weapon01, info02 = (ctx.stock(name) for name in
                                     ("equip01_info01", "equip01_info_weapon01", "equip01_info02"))
    for name in ("equip01_info_weapon01", "equip01_info01", "equip01_info02"):
        ctx.merge_list("status01", name)
    panel, traits, sigils = weapon_panel(info01, info_weapon01, info02)
    insert(prefab.find("loc_buildcard"), panel, CARD_ORDER)

    left, top, width, height = GEAR
    scale = TITLE_SCALE
    x, y = card(left + width / 2, top)
    panel.set("Scale", (scale, scale, 1))
    panel.place(pos=(x, y - WEAPON_TOP * scale))
    for node, node_top in zip(traits + sigils, (SKILLS_TOP, PENDULUM_TOP, SIGILS_TITLE_TOP, GENE_TOP)):
        node.place(pos=(0, node_top))

    gene = sigils[1]
    sigil_rows = [gene.find(f"equip01_p01_{i + 1:02}") for i in range(SIGIL_ROWS)]
    row_w = width * SCREEN_CARD_RATIO / scale - 8
    for row in sigil_rows:
        rework_sigil_row(row, row_w)
    match_trait_rows(traits, sigil_rows, row_w)
    info = panel.child("root").child("loc_info01")
    widen_weapon_section(info, row_w)
    gold_levels(panel)
    weapon_type(panel, info, info_weapon01, scale)

    prefab.find("loc_status02/loc_chr_status02").set("Active", False)
    set_refs(prefab.root, "Weapon", [panel])
    set_refs(prefab.root, "Gem", sigil_rows)
