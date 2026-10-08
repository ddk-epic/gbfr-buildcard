# Lays out the Over Mastery section: lb_ovtli02's rows on a status-style panel under a title bar.
from model.components import rect, set_line
from model.prefab import copy, f
from steps.layout import (CARD_ORDER, ICON_PAD, OVER_MASTERY, PAD_X, PAD_Y, PANEL_SCALE, SCREEN_CARD_RATIO, TITLE_SCALE, TITLE_TEXT,
                          card, insert, rounded_panel)

INK = (0.19607843, 0.37254903, 0.4901961)
TITLE_TEXT_ID = "TXT_PAU_LB_TAB_LIMIT_OVER"
TITLE_GAP = 46  # the bar's top above the rows, in title units
BAR_TEXT = 20  # the title text's pivot below the bar's, in title units
ROWS = 4
ROW_ICON = 88  # icon01's size, in row units
ROW_SCALE = 1.1
ICON = 42  # sharecard pixels, before ROW_SCALE
LINE_H, LINE_GAP = 36, 4  # sharecard pixels, before ROW_SCALE
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

    left, top, width, height = OVER_MASTERY
    scale = PANEL_SCALE
    u = scale / SCREEN_CARD_RATIO  # sharecard pixels per panel unit
    title_k = TITLE_SCALE / scale
    w, h = width / u, height / u
    bar_y = h / 2 - PAD_Y / u - TITLE_TEXT * title_k
    cx, cy = card(left + width / 2, top + height / 2)

    panel = insert(card_node, rect("bc_om"), CARD_ORDER)
    panel.set("Scale", (scale, scale, 1))
    panel.place(pos=(cx, cy), size=(w, h))
    rounded_panel(panel, "bc_om_panel", scale)

    bar = copy(title)
    bar.children[0].remove()
    bar.name = "bc_om_ttl"
    panel.add(bar)
    bar.set("Scale", (title_k, title_k, 1))
    bar.place(pos=(0, bar_y))

    text = copy(title.children[0])
    text.name = "bc_om_ttl_text"
    set_line(text, "TextID", TITLE_TEXT_ID)
    text.set("AnchorMin", "0.5, 0.5")
    text.set("AnchorMax", "0.5, 0.5")
    insert(card_node, text, CARD_ORDER)
    text.set("Scale", (scale * title_k, scale * title_k, 1))
    text.place(pos=(cx, cy + scale * (bar_y - BAR_TEXT * title_k)))

    # rows centred between the bar's gap and the bottom padding, at the skills icons' left inset
    row_scale = ICON * SCREEN_CARD_RATIO / ROW_ICON * ROW_SCALE / scale
    line_h, line_gap = LINE_H * ROW_SCALE / u, LINE_GAP * ROW_SCALE / u
    content_top = bar_y - TITLE_GAP * title_k
    content_bottom = -h / 2 + PAD_Y / u
    rows_h = ROWS * line_h + (ROWS - 1) * line_gap
    rows_top = (content_top + content_bottom) / 2 + rows_h / 2
    x = -w / 2 + (PAD_X + ICON_PAD) / u - ICON_X_ROW * row_scale
    rows = []
    for i in range(ROWS):
        node = panel.add(row(source_row, f"bc_om_{i}"))
        node.find("icon01").place(pos=(ICON_X_ROW, 0))
        node.find("loc_text01").place(pos=(TEXT_X_ROW, 0))
        node.set("Scale", (row_scale, row_scale, 1))
        node.place(pos=(x, rows_top - i * (line_h + line_gap) - line_h / 2))
        ctx.powers(node)
        rows.append(node)
    ctx.export("OverMasteryRows", rows)
