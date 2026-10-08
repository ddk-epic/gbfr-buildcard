# Lays out the status section: the stat rows in a 2x2 grid spaced as sharecard's StatusPanel, and the badges above it.
import re

from model.prefab import f
from steps.layout import PANEL_SCALE, SCREEN_CARD_RATIO, SKILLS, STATUS, card, move

# sharecard's StatusPanel, in sharecard pixels
BORDER = 1
PAD_X, PAD_Y = 20, 17.5  # pl-4 pr-4, py-3.5
COLUMN_GAP = 25  # gap-x-5
LEFT_FR, RIGHT_FR = 9, 11  # grid-cols-[9fr_11fr]
RIGHT_PAD = 20  # pr-4 of the right column
ICON_BOX = 35  # w-7
ROW_GAP = 22.5  # gap-y-4.5
LABEL_PX, VALUE_PX = 25, 28  # text-xl, text-[28px]
UNIT_SCALE, UNIT_GAP, UNIT_RAISE = 0.65, 2.5, 1  # text-[65%], pl-0.5, bottom-px

GAME_CAP = 0.85  # cap height per font size
SINK = 0.45  # baseline below a middle-aligned text's centre, per font size

LABEL_SIZE = 40
NUMBER_SIZE = LABEL_SIZE * VALUE_PX / LABEL_PX
PERCENT_SIZE = NUMBER_SIZE * UNIT_SCALE

# block units
W = 1000
ROW_H = 64
HP_ICON, HP_NUM = 52, 72  # icon centre and number right edge, from the row's edges
CRT_ICON, CRT_NUM = 92, 34
PERCENT_RECT = (29.38, 32)  # at font size 32
PERCENT_CJK_SIZE, CJK_NUMBER_SIZE = 36, 56

BADGE_SCALE = 0.7

# sharecard pixels
MASTER_LEVEL_DX = 118.3  # loc_ml_level01's centre right of level01's
POWER_OVERHANG = 50  # power01's right edge past the column's
COLUMN_X = 296

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


def status_block(block):
    left, top, width, height = STATUS
    scale = PANEL_SCALE
    u = scale / SCREEN_CARD_RATIO  # sharecard pixels per block unit

    value_cap = NUMBER_SIZE * GAME_CAP
    h = (2 * (BORDER + PAD_Y) + ROW_GAP) / u + 2 * value_cap

    # x from the block's left edge
    content = W - 2 * (BORDER + PAD_X) / u
    column = (content - COLUMN_GAP / u) / (LEFT_FR + RIGHT_FR)
    left_icon = (BORDER + PAD_X + ICON_BOX / 2) / u
    left_num = (BORDER + PAD_X) / u + LEFT_FR * column
    right_icon = left_num + (COLUMN_GAP + ICON_BOX / 2) / u
    right_num = W - (BORDER + PAD_X + RIGHT_PAD) / u

    # y from the block's bottom edge
    bottom_baseline = (BORDER + PAD_Y) / u
    top_baseline = bottom_baseline + value_cap + ROW_GAP / u
    top_centre, bottom_centre = top_baseline + SINK * LABEL_SIZE, bottom_baseline + SINK * LABEL_SIZE

    x, y = card(left + width / 2, top + height)
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
    base.replace("SpriteName: ps_cmn_base52", "SpriteName: ps_cmn_base54")
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
    percent.place(pos=(UNIT_GAP / u + percent_w, UNIT_RAISE / u - SINK * (NUMBER_SIZE - PERCENT_SIZE)),
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

    gap = (SKILLS[1] - (STATUS[1] + STATUS[3])) * SCREEN_CARD_RATIO
    overhang = half_height(element) - half_height(name)
    name_centre = status_top + gap + overhang + half_height(name)

    for node in (level, master_level, power, name):
        node.set("Scale", (BADGE_SCALE, BADGE_SCALE, 1))
    left, top = card(STATUS[0], STATUS[1])
    right = card(STATUS[0] + STATUS[2] + POWER_OVERHANG, 0)[0]
    level_x = left + LEVEL_R * BADGE_SCALE
    move(level, level_x, top - LEVEL_R * BADGE_SCALE)
    move(master_level, level_x + MASTER_LEVEL_DX * SCREEN_CARD_RATIO, top - MASTER_LEVEL_TOP * BADGE_SCALE)
    move(power, right - POWER_RIGHT * BADGE_SCALE, top - POWER_TOP * BADGE_SCALE)
    name.place(pivot=(0.5, 0.5))
    move(name, card(COLUMN_X, 0)[0], name_centre)


def apply(ctx):
    base = ctx.prefab("status01").at("root/loc_base01")
    status_top = status_block(base.child("loc_status02").child("loc_chr_status01"))
    badges(base, status_top)
