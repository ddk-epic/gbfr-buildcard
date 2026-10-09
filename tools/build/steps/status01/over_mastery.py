# Lays out the Over Mastery section: lb_ovtli02's rows on a panel under a title bar.
from model.components import rect, set_line
from model.prefab import copy, f
from steps.layout import CARD_ORDER, HEADING_MARGIN, ICON_PAD, OVER_MASTERY, insert
from steps.panel import Heading, panel

INK = (0.19607843, 0.37254903, 0.4901961)
TITLE_TEXT_ID = "TXT_PAU_LB_TAB_LIMIT_OVER"
ROWS = 4
ROW_ICON = 88  # icon01's size, in row units
ROW_SCALE = 1.1
ICON = 51  # card units, before ROW_SCALE
LINE_H, LINE_GAP = 44, 5  # card units, before ROW_SCALE
ICON_X_ROW = -482  # icon01's left edge in the row
TEXT_X_ROW = -384  # loc_text01's left edge in the row


def row(source, name):
    # a copy of the row with its separator and stars hidden and its texts in ink
    node = copy(source)
    node.name = name
    for hidden in ("line01", "loc_star01"):
        node.find(hidden).set("Active", False)
    for colored in ("text01", "icon_plus01", "num01", "percent01"):
        set_line(node.find(colored), "Color", f"{', '.join(f(c) for c in INK)}, 1")
    return node


def apply(ctx):
    card_node = ctx.prefab("status01").find("loc_buildcard")
    source_row = ctx.stock("lb_ovtli02").find("var00_lb_ovtli01_p01_01")
    title = ctx.stock("equip01_info02").find("loc_info02/ttl01")

    om = insert(card_node, rect("bc_om"), CARD_ORDER)
    box = panel(om, OVER_MASTERY, heading=Heading(title, TITLE_TEXT_ID, HEADING_MARGIN))

    # rows centred in the content box, at the skills icons' left inset
    row_scale = ICON / ROW_ICON * ROW_SCALE
    line_h, line_gap = LINE_H * ROW_SCALE, LINE_GAP * ROW_SCALE
    rows_h = ROWS * line_h + (ROWS - 1) * line_gap
    rows_top = (box.top + box.bottom) / 2 + rows_h / 2
    x = box.left + ICON_PAD - ICON_X_ROW * row_scale
    rows = []
    for i in range(ROWS):
        node = om.add(row(source_row, f"bc_om_{i}"))
        node.find("icon01").place(pos=(ICON_X_ROW, 0))
        node.find("loc_text01").place(pos=(TEXT_X_ROW, 0))
        node.set("Scale", (row_scale, row_scale, 1))
        node.place(pos=(x, rows_top - i * (line_h + line_gap) - line_h / 2))
        rows.append(node)
    ctx.export("OverMasteryRows", rows)
