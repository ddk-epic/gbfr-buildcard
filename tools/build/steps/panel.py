# Draws a section's rounded panel over its area and stacks its heading at the top of the padding.
from collections import namedtuple

from model.components import image, rect, set_line
from model.prefab import copy
from steps.layout import OUTLINE, PAD, TITLE_BAR_H, TITLE_SCALE, card, move, sprite

ROUNDED = "layouts/pause/status/noatlastextures/bc_rounded"
OUTLINE_TOP = "layouts/pause/status/noatlastextures/bc_outline_top"
ROUNDED_TOP = "layouts/pause/status/noatlastextures/bc_rounded_top"
FILL = (1, 1, 1), 0.9
STROKE = (133 / 255, 163 / 255, 181 / 255), 223 / 255  # ps_cmn_base54's
HEADING_H = 4 * TITLE_BAR_H * TITLE_SCALE  # card units, the bar centred in it

# left, top, right, bottom from the panel's centre, in card units
Box = namedtuple("Box", "left top right bottom")
# the bar to copy, its TextID and the margin above it
Heading = namedtuple("Heading", "source text_id margin_top", defaults=[0])


def paddings(padding):
    # left, top, right and bottom from one value or four
    return (padding,) * 4 if isinstance(padding, (int, float)) else padding


def heading_y(h, heading, padding=PAD):
    # the heading's centre from the centre of a panel h high
    return h / 2 - paddings(padding)[1] - heading.margin_top - HEADING_H / 2


def content_box(w, h, heading=None, padding=PAD):
    # the inside of a panel w by h within its padding, below the heading
    left, top, right, bottom = paddings(padding)
    box_top = heading_y(h, heading, padding) - HEADING_H / 2 if heading else h / 2 - top
    return Box(-w / 2 + left, box_top, w / 2 - right, -h / 2 + bottom)


def panel(node, area, corners="all", heading=None, padding=PAD):
    # places node over the area with all, the top or the bottom corners rounded, and returns its content box
    left, top, w, h = area
    node.place(size=(w, h))
    move(node, *card(left + w / 2, top + h / 2))
    fill, stroke = (ROUNDED, OUTLINE) if corners == "all" else (ROUNDED_TOP, OUTLINE_TOP)
    base = node.add(rect(f"{node.name}_panel", image(*FILL, sprite(fill), sliced=True, fill_center=True)))
    if corners == "bottom":
        base.set("Rotation", (0, 0, 1, 0))
    base.place(pos=(0, 0), size=(w, h))
    outline = base.add(rect(f"{node.name}_panel_outline", image(*STROKE, sprite(stroke), sliced=True)))
    outline.place(pos=(0, 0), size=(w, h))
    if heading:
        bar = node.add(copy(heading.source))
        bar.name = f"{node.name}_ttl"
        set_line(bar.children[0], "TextID", heading.text_id)
        bar.set("Scale", (TITLE_SCALE, TITLE_SCALE, 1))
        bar.place(pos=(0, heading_y(h, heading, padding)))
    return content_box(w, h, heading, padding)
