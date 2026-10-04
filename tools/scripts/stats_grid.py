# Usage: python stats_grid.py <status01.prfb.yaml> <out.prfb.yaml>
# Lays out the status block's stat rows in a 2x2 grid spaced as sharecard's StatusPanel, without the character icon and element.
import re, sys
from prefab import Prefab, f

p = Prefab(sys.argv[1])
S = 3424 / 2880
PARENT_X = 640  # loc_status02's pivot in loc_base01 coordinates

LEFT, RIGHT = (16 - 1440) * S, (576 - 1440) * S
BOTTOM = (720 - 1118) * S

NUMBERS = (91, 96, 102, 108)  # hp_num01, atk_num01, crt_num01, brk_num01
PERCENT = 103  # crt_num00
PERCENT_GROUP = 101  # loc_crt_num

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

# cap height per font size
GAME_CAP = 0.85
# baseline below a middle-aligned text's centre, per font size
SINK = 0.45

LABEL_SIZE = 40
NUMBER_SIZE = LABEL_SIZE * VALUE_PX / LABEL_PX
PERCENT_SIZE = NUMBER_SIZE * UNIT_SCALE

W = 1000
scale = (RIGHT - LEFT) / W
U = scale / S  # sharecard pixels per block unit

# icon centre and number right edge, from the row's edges
ROW_H = 64
HP_ICON, HP_NUM = 52, 72
CRT_ICON, CRT_NUM = 92, 34
PERCENT_RECT = (29.38, 32)  # at font size 32
PERCENT_CJK_SIZE, CJK_NUMBER_SIZE = 36, 56

def u(px):
    return px / U

value_cap = NUMBER_SIZE * GAME_CAP
H = u(2 * (BORDER + PAD_Y) + ROW_GAP) + 2 * value_cap

# x from the block's left edge
content = W - u(2 * (BORDER + PAD_X))
column = (content - u(COLUMN_GAP)) / (LEFT_FR + RIGHT_FR)
left_icon = u(BORDER + PAD_X + ICON_BOX / 2)
left_num = u(BORDER + PAD_X) + LEFT_FR * column
right_icon = left_num + u(COLUMN_GAP + ICON_BOX / 2)
right_num = W - u(BORDER + PAD_X + RIGHT_PAD)

# y from the block's bottom edge
bottom_baseline = u(BORDER + PAD_Y)
top_baseline = bottom_baseline + value_cap + u(ROW_GAP)
top_centre, bottom_centre = top_baseline + SINK * LABEL_SIZE, bottom_baseline + SINK * LABEL_SIZE

p.place(73, pos=((LEFT + RIGHT) / 2 - PARENT_X, BOTTOM + H * scale / 2), size=(W, H))
p.set(73, "Scale", (scale, scale, 1))

p.place(74, pos=(0, 0), size=(W, H))  # chr_status01
p.place(75, pos=(0, H / 2))  # root
p.place(76, pos=(0, -H / 2), size=(W, H))  # status_base01
p.place(78, pos=(0, -H / 2), size=(W, H))  # loc_status01

p.set(79, "Active", "false")  # loc_chr_icon01: character icon and element
for id_ in (90, 95, 100, 107):  # hp_line01, atk_line01, crt_line01, brk_line01
    p.set(id_, "Active", "false")

left_x, left_w = left_icon - HP_ICON, left_num + HP_NUM - (left_icon - HP_ICON)
right_x, right_w = right_icon - CRT_ICON, right_num + CRT_NUM - (right_icon - CRT_ICON)

p.place(87, pos=(left_x + left_w - W / 2, top_centre - ROW_H / 2), size=(left_w, ROW_H))  # loc_hp
p.place(92, pos=(left_x + left_w - W / 2, bottom_centre + ROW_H / 2), size=(left_w, ROW_H))  # loc_atk
p.place(97, pos=(right_x - W / 2, top_centre - ROW_H / 2), size=(right_w, ROW_H))  # loc_crt
p.place(104, pos=(right_x - W / 2, bottom_centre + ROW_H / 2), size=(right_w, ROW_H))  # loc_brk
for id_ in (87, 92, 97, 104):
    p.repin(id_)

def font_sizes(id_, size, overrides):
    # sets the Text's FontSize and every enabled override's
    start, end = p.range(id_)
    for i in range(start, end):
        m = re.match(r"(\s+FontSize: )(\d+(\.\d+)?)$", p.lines[i])
        if not m:
            continue
        if p.lines[i].startswith("      FontSize: "):
            p.lines[i] = f"{m[1]}{f(size)}"
        elif float(m[2]):
            p.lines[i] = f"{m[1]}{overrides}"

for id_ in NUMBERS:
    font_sizes(id_, NUMBER_SIZE, round(NUMBER_SIZE))
    start, end = p.range(id_)
    for i in range(start, end):
        if p.lines[i].startswith("      Margin: "):
            p.lines[i] = "      Margin: 0, 0, 0, 0"

# numbers raised onto the labels' baseline
raise_ = SINK * (NUMBER_SIZE - LABEL_SIZE)
p.place(91, pos=(-HP_NUM, ROW_H / 2 + raise_))  # hp_num01
p.place(96, pos=(-HP_NUM, -ROW_H / 2 + raise_))  # atk_num01
p.place(PERCENT_GROUP, pos=(right_w - CRT_NUM, ROW_H / 2 + raise_))
p.place(108, pos=(right_w - CRT_NUM, -ROW_H / 2 + raise_))  # brk_num01

# the percent sign after the digits
k = PERCENT_SIZE / 32
percent_w = PERCENT_RECT[0] * k
font_sizes(PERCENT, PERCENT_SIZE, round(PERCENT_CJK_SIZE * NUMBER_SIZE / CJK_NUMBER_SIZE))
p.place(PERCENT, pos=(u(UNIT_GAP) + percent_w, u(UNIT_RAISE) - SINK * (NUMBER_SIZE - PERCENT_SIZE)),
        size=(percent_w, PERCENT_RECT[1] * k))

p.save(sys.argv[2])
