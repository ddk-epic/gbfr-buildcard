# Lays out the skills section: the Skills screen's cards, reduced to icon, name and element tag, in a 2x2 grid.
from model.components import rect, set_line, set_refs
from model.prefab import copy
from steps.layout import (CARD_ORDER, FRAME, ICON_PAD, INSET, PAD_X, PAD_Y, PANEL_SCALE, SKILLS, TITLE_BAR_H, TITLE_SCALE, card,
                          insert, rounded_panel)

CARDS = ["ability_set01_btn04", "ability_set01_btn03", "ability_set01_btn01", "ability_set01_btn02"]  # slots 1 to 4
TITLE_TEXT_ID = "TXT_PAU_ABILITY"
ICON_W = 108  # card units
ICON_GAP = 12  # card units
HIDDEN = ["loc_icon_pos", "loc_guide_button", "loc_guide_button_key"]

# block units
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

baseline = NAME_H / 2 - SINK * NAME_SIZE  # above the name rect's bottom
icon_top = ELEMENT_RAISE + ELEMENT_H / 2  # above the bar's centre
SPACING = round(icon_top - BAR_H / 2 + NAME_GAP - baseline)
# bottom padding centres the visible stack, cap top to element icon bottom
PADDING_BOTTOM = round(NAME_H - baseline - NAME_SIZE * CAP - (ELEMENT_RAISE + BAR_H / 2 - ELEMENT_H / 2))


def text_group():
    # the name and the element tag stacked
    return rect("bc_skill_text", [
        "  - ComponentName: VerticalLayoutGroup", "    Component:", f"      Padding: 0, 0, 0, {PADDING_BOTTOM}",
        f"      Spacing: {SPACING}", f"      ChildAlignment: {MIDDLE_LEFT}", "      ChildControlWidth: false",
        "      ChildControlHeight: false", "      ChildScaleWidth: false", "      ChildScaleHeight: false",
        "      ChildForceExpandWidth: false", "      ChildForceExpandHeight: false", "      Enable: true"])


def skill_card(source):
    # the card with its frames transparent, its buttons hidden and its name and element tag in a text group
    node = copy(source)
    start = node.lines.index("  Components:") + 1
    end = node.lines.index("  Active: true")
    kept, keep = [], False
    for line in node.lines[start:end]:
        if isinstance(line, str) and line.startswith("  - ComponentName: "):
            keep = line == "  - ComponentName: AbilityInfo"
        if keep:
            kept.append(line)
    node.lines[start:end] = kept

    base = node.find("loc_base01")
    frames = base.child("base01_set01")
    for frame in (base, frames):
        frame.replace("Color: 1, 1, 1, 1", "Color: 1, 1, 1, 0")
    for name in HIDDEN:
        base.child(name).set("Active", False)

    name = base.child("text01")
    element = frames.child("loc_elem01")
    set_line(name, "Margin", f"{ELEMENT_PAD}, 0, 0, 0")
    name.lines[name.lines.index("  Active: true"):name.lines.index("  Active: true")] = [
        "  - ComponentName: ContentSizeFitter", "    Component:", "      HorizontalFit: 0", "      VerticalFit: 2",
        "      Enable: true"]
    group = frames.add(text_group(), frames.children.index(element))
    group.add(name)
    group.add(element)
    return node


def apply(ctx):
    prefab = ctx.prefab("status01")
    ability_info = ctx.stock("ability_info01_02")
    title = ctx.stock("equip01_info02").find("loc_info02/ttl01")

    left, top, width, height = SKILLS
    scale = PANEL_SCALE
    title_k = TITLE_SCALE / scale
    h = height / scale
    # the outer padding and a bar height above the bar, two bar heights below its top
    bar_top = h / 2 - (INSET - FRAME) / scale - TITLE_BAR_H * title_k
    heading_h = h / 2 - bar_top + 2 * TITLE_BAR_H * title_k
    cell_w, cell_h = (W - 2 * PAD_X / scale) / 2, (h - PAD_Y / scale - heading_h) / 2
    cells_y = (PAD_Y / scale - heading_h) / 2
    icon_scale = ICON_W / scale / ICON_SIZE
    icon_x = (ICON_PAD + ICON_W / 2) / scale  # from the cell's left edge
    name_x = (ICON_PAD + ICON_W + ICON_GAP) / scale
    # the text group, from the element bar's left edge to the cell's right edge
    text_w = cell_w - name_x + ELEMENT_PAD
    content_h = NAME_H + SPACING + BAR_H

    skills = insert(prefab.find("loc_buildcard"), rect("bc_skills"), CARD_ORDER)
    panel = skills.add(rect("bc_skills_base"))
    bar = skills.add(copy(title))
    bar.name = "bc_skills_ttl"
    set_line(bar.children[0], "TextID", TITLE_TEXT_ID)
    cards = [skills.add(skill_card(ability_info.find(f"loc_list_control01/{name}"))) for name in CARDS]

    skills.place(pos=card(left + width / 2, top + height / 2), size=(W, h))
    skills.set("Scale", (scale, scale, 1))
    panel.place(pos=(0, 0), size=(W, h))
    rounded_panel(panel, "bc_skills_panel", scale)
    bar.set("Scale", (title_k, title_k, 1))
    bar.place(pos=(0, bar_top))

    for slot, node in enumerate(cards):
        x = cell_w / 2 * (1 if slot % 2 else -1)
        y = cells_y + cell_h / 2 * (-1 if slot // 2 else 1)
        node.place(pos=(x, y))

        base = node.find("loc_base01")
        frames = base.child("base01_set01")
        base.place(size=(cell_w, cell_h))
        frames.place(size=(cell_w, cell_h))
        base.repin()

        icon = frames.child("loc_icon_ability")
        icon.set("Scale", (icon_scale, icon_scale, 1))
        icon.set("AnchorMin", (0, 0.5))
        icon.set("AnchorMax", (0, 0.5))
        icon.place(pos=(icon_x - cell_w / 2, 0))
        icon.repin()

        # children at the group's one-line positions, anchored top-left like the game's layout group children
        group = frames.child("bc_skill_text")
        group.place(pos=(name_x - ELEMENT_PAD + text_w / 2 - cell_w / 2, 0), size=(text_w, cell_h))
        group_top = content_h / 2 + PADDING_BOTTOM / 2
        name, element = group.children
        for child, y, size in ((name, group_top - NAME_H / 2, (text_w, NAME_H)),
                               (element, group_top - NAME_H - SPACING - BAR_H / 2, None)):
            child.set("AnchorMin", (0, 1))
            child.set("AnchorMax", (0, 1))
            child.place(pos=(-text_w / 2, y), size=size, pivot=(0, 0.5))
            child.repin()
        ctx.powers(name)

    for block in ("loc_chr_status03", "loc_chr_status04"):
        prefab.find(f"loc_status02/{block}").set("Active", False)
    set_refs(prefab.root, "Ability", cards)
    ctx.export("SkillNames", [node.find("bc_skill_text/text01") for node in cards])
