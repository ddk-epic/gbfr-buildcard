# Lays out the status section: the stat rows in a 2x2 grid, the masteries panel under it, and the badges above it.
import re

from model.components import LEFT, components, rect, set_line
from model.prefab import f
from steps.layout import FRAME, GAP, INSET, PANEL_SCALE, SKILLS, STATUS, card, move, rounded_panel

# the status panel, in card units
BORDER = 1
PAD_X, PAD_Y = 24, 21
COLUMN_GAP = 31
LEFT_FR, RIGHT_FR = 9, 11  # the columns' shares
RIGHT_PAD = 24  # the right column's right padding
ICON_BOX = 43
ROW_GAP = 28
UNIT_SCALE, UNIT_GAP, UNIT_RAISE = 0.65, 3, 1  # the percent sign's

# the masteries panel, in card units
MASTERIES_PAD_TOP, MASTERIES_PAD_BOTTOM = 8, 9
MASTERIES_PAD_LEFT, MASTERIES_PAD_RIGHT = 31, 24
STACK_GAP = GAP / 4  # between the status and masteries panels

GAME_CAP = 0.85  # cap height per font size
SINK = 0.45  # baseline below a middle-aligned text's centre, per font size

LABEL_SIZE = 40
NUMBER_SIZE = 44.8
PERCENT_SIZE = NUMBER_SIZE * UNIT_SCALE
MASTERIES_SIZE = 32

# block units
W = 1000
ROW_H = 64
HP_ICON, HP_NUM = 52, 72  # icon centre and number right edge, from the row's edges
CRT_ICON, CRT_NUM = 92, 34
PERCENT_RECT = (29.38, 32)  # at font size 32
PERCENT_CJK_SIZE, CJK_NUMBER_SIZE = 36, 56

BADGE_SCALE = 0.7

# card units
MASTER_LEVEL_DX = 144  # loc_ml_level01's centre right of level01's
MASTER_LEVEL_RAISE = 8  # loc_ml_level01's top above level01's
BADGE_INSET = (FRAME + INSET) / 2
POWER_OVERHANG = 61  # power01's right edge past the column's
COLUMN_X = 362

# badge rect units, from each badge's pivot
LEVEL_R = 148  # ps_cmn_icon_base03's radius
MASTER_LEVEL_TOP = 174 * 0.7  # masterlevel02's lv_base01 top, at masterlevel02's scale
POWER_TOP, POWER_RIGHT = 184, 92  # pwr_icon01's top, ps_cmn_icon_base02's right edge


def font_sizes(node, size, overrides):
    # sets the Text's FontSize and every enabled override's
    for i, line in enumerate(node.lines):
        m = isinstance(line, str) and re.match(r"(\s+FontSize: )(\d+(\.\d+)?)$", line)
        if not m:
            continue
        if line.startswith("      FontSize: "):
            node.lines[i] = f"{m[1]}{f(size)}"
        elif float(m[2]):
            node.lines[i] = f"{m[1]}{overrides}"


def masteries_block(status, label):
    # the masteries and collection texts in a panel at the section's bottom, its top corners square
    left, top, width, height = STATUS
    scale = PANEL_SCALE
    h = (2 * BORDER + MASTERIES_PAD_TOP + MASTERIES_PAD_BOTTOM) / scale + MASTERIES_SIZE * GAME_CAP

    parent = status.parent
    block = parent.add(rect("bc_masteries"), parent.children.index(status) + 1)
    x, y = card(left + width / 2, top + height)
    block.place(size=(W, h))
    move(block, x, y + h * scale / 2)
    block.set("Scale", (scale, scale, 1))
    rounded_panel(block, "bc_masteries_panel", scale, corners="bottom")

    # two columns from the left padding, on the bottom padding's baseline
    content = W - (2 * BORDER + MASTERIES_PAD_LEFT + MASTERIES_PAD_RIGHT) / scale
    text_left = -W / 2 + (BORDER + MASTERIES_PAD_LEFT) / scale
    baseline = -h / 2 + (BORDER + MASTERIES_PAD_BOTTOM) / scale
    lines = components(label)
    start = lines.index("  - ComponentName: TextSetter")
    end = lines.index("  - ComponentName: LanguageSetter")
    texts = []
    for i, name in enumerate(("bc_masteries_text", "bc_collection_text")):
        text = block.add(rect(name, lines[:start] + lines[end:], (0, 0.5)))
        set_line(text, "Alignment", LEFT)
        font_sizes(text, MASTERIES_SIZE, round(MASTERIES_SIZE))
        text.place(pos=(text_left + i * content / 2, baseline + SINK * MASTERIES_SIZE), size=(content / 2, MASTERIES_SIZE))
        texts.append(text)
    return y + h * scale + STACK_GAP, texts


def status_block(block, bottom):
    left, top, width, height = STATUS
    scale = PANEL_SCALE

    value_cap = NUMBER_SIZE * GAME_CAP
    h = (2 * (BORDER + PAD_Y) + ROW_GAP) / scale + 2 * value_cap

    # x from the block's left edge
    content = W - 2 * (BORDER + PAD_X) / scale
    column = (content - COLUMN_GAP / scale) / (LEFT_FR + RIGHT_FR)
    left_icon = (BORDER + PAD_X + ICON_BOX / 2) / scale
    left_num = (BORDER + PAD_X) / scale + LEFT_FR * column
    right_icon = left_num + (COLUMN_GAP + ICON_BOX / 2) / scale
    right_num = W - (BORDER + PAD_X + RIGHT_PAD) / scale

    # y from the block's bottom edge
    bottom_baseline = (BORDER + PAD_Y) / scale
    top_baseline = bottom_baseline + value_cap + ROW_GAP / scale
    top_centre, bottom_centre = top_baseline + SINK * LABEL_SIZE, bottom_baseline + SINK * LABEL_SIZE

    x, y = card(left + width / 2, 0)[0], bottom
    block.place(size=(W, h))
    move(block, x, y + h * scale / 2)
    block.set("Scale", (scale, scale, 1))

    chr_status = block.child("chr_status01")
    root = chr_status.child("root")
    base = root.child("status_base01")
    grid = base.child("loc_status01")
    chr_status.place(pos=(0, 0), size=(W, h))
    root.place(pos=(0, h / 2))
    base.place(pos=(0, -h / 2), size=(W, h))
    base.replace("Color: 1, 1, 1, 1", "Color: 1, 1, 1, 0")
    rounded_panel(base, "bc_status_panel", scale, corners="top")
    base.child("status_ttl_text01").set("Active", False)
    grid.place(pos=(0, -h / 2), size=(W, h))

    for hidden in ("loc_chr_icon01", "line01"):
        grid.child(hidden).set("Active", False)
    rows = {name: grid.child(f"loc_{name}") for name in ("hp", "atk", "crt", "brk")}
    for name, row in rows.items():
        row.child(f"{name}_line01").set("Active", False)

    left_x, left_w = left_icon - HP_ICON, left_num + HP_NUM - (left_icon - HP_ICON)
    right_x, right_w = right_icon - CRT_ICON, right_num + CRT_NUM - (right_icon - CRT_ICON)
    rows["hp"].place(pos=(left_x + left_w - W / 2, top_centre - ROW_H / 2), size=(left_w, ROW_H))
    rows["atk"].place(pos=(left_x + left_w - W / 2, bottom_centre + ROW_H / 2), size=(left_w, ROW_H))
    rows["crt"].place(pos=(right_x - W / 2, top_centre - ROW_H / 2), size=(right_w, ROW_H))
    rows["brk"].place(pos=(right_x - W / 2, bottom_centre + ROW_H / 2), size=(right_w, ROW_H))
    for row in rows.values():
        row.repin()

    numbers = {name: row.find(f"{name}_num01") for name, row in rows.items()}
    for number in numbers.values():
        font_sizes(number, NUMBER_SIZE, round(NUMBER_SIZE))
        for i, line in enumerate(number.lines):
            if isinstance(line, str) and line.startswith("      Margin: "):
                number.lines[i] = "      Margin: 0, 0, 0, 0"

    # numbers raised onto the labels' baseline
    raised = SINK * (NUMBER_SIZE - LABEL_SIZE)
    numbers["hp"].place(pos=(-HP_NUM, ROW_H / 2 + raised))
    numbers["atk"].place(pos=(-HP_NUM, -ROW_H / 2 + raised))
    rows["crt"].child("loc_crt_num").place(pos=(right_w - CRT_NUM, ROW_H / 2 + raised))
    numbers["brk"].place(pos=(right_w - CRT_NUM, -ROW_H / 2 + raised))

    # the percent sign after the digits
    percent = rows["crt"].find("crt_num00")
    k = PERCENT_SIZE / 32
    percent_w = PERCENT_RECT[0] * k
    font_sizes(percent, PERCENT_SIZE, round(PERCENT_CJK_SIZE * NUMBER_SIZE / CJK_NUMBER_SIZE))
    percent.place(pos=(UNIT_GAP / scale + percent_w, UNIT_RAISE / scale - SINK * (NUMBER_SIZE - PERCENT_SIZE)),
                  size=(percent_w, PERCENT_RECT[1] * k))
    return y + h * scale


def badges(base, status_top):
    # level badges at the portrait column's top left, the PWR diamond at its top right, the name band above the status
    badge_root = base.child("loc_status01")
    level, master_level = badge_root.child("level01"), badge_root.child("loc_ml_level01")
    power, name = badge_root.child("power01"), base.child("loc_name01")
    element = name.child("loc_icon_elem01")

    def half_height(node):
        return node.vec("SizeDelta")[1] * BADGE_SCALE / 2

    gap = SKILLS[1] - (STATUS[1] + STATUS[3])
    overhang = half_height(element) - half_height(name)
    name_centre = status_top + gap + overhang + half_height(name)

    for node in (level, master_level, power, name):
        node.set("Scale", (BADGE_SCALE, BADGE_SCALE, 1))
    left, top = card(BADGE_INSET, BADGE_INSET)
    right = card(STATUS[0] + STATUS[2] + POWER_OVERHANG, 0)[0]
    level_x = left + LEVEL_R * BADGE_SCALE
    move(level, level_x, top - LEVEL_R * BADGE_SCALE)
    move(master_level, level_x + MASTER_LEVEL_DX, top + MASTER_LEVEL_RAISE - MASTER_LEVEL_TOP * BADGE_SCALE)
    move(power, right - POWER_RIGHT * BADGE_SCALE, top - POWER_TOP * BADGE_SCALE)
    name.place(pivot=(0.5, 0.5))
    move(name, card(COLUMN_X, 0)[0], name_centre)


def apply(ctx):
    base = ctx.prefab("status01").at("root/loc_base01")
    status = base.child("loc_status02").child("loc_chr_status01")
    bottom, texts = masteries_block(status, status.find("loc_crt/crt_text01"))
    status_top = status_block(status, bottom)
    badges(base, status_top)
    for text in texts:
        ctx.powers(text, "Text")
    ctx.export("MasteryTexts", texts)
