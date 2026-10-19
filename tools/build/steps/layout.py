# Measurements and orders shared by the steps.
from model.components import mask, rect

# the UI's reference resolution
CANVAS_W, CANVAS_H = 3840, 2160

# loc_buildcard's size in card pixels and its scale onto the canvas
CARD_W, CARD_H = 2880, 1440
CARD_SCALE = 3528 / 2880
CANVAS_CARD_W, CANVAS_CARD_H = CARD_W * CARD_SCALE, CARD_H * CARD_SCALE
CARD_PATH, BASE_PATH = "root/loc_base01/loc_buildcard", "root/loc_base01"

# the grid inside the card's frame, in card pixels
FRAME = 10  # bc_frame's border
INSET = 3 * FRAME  # bc_frame's border and twice its width
BOX_W, BOX_H = CARD_W - 2 * INSET, CARD_H - 2 * INSET
GAP = 24
SHARES = (20, 27, 53)  # status, gear, master traits
ROW_SHARES = (81, 19)  # upper row, band

# the last column and the band take the remainder
COL_W = [round((BOX_W - 2 * GAP) * share / sum(SHARES)) for share in SHARES[:2]]
COL_W.append(BOX_W - 2 * GAP - sum(COL_W))
COL_X = [INSET, INSET + COL_W[0] + GAP, INSET + COL_W[0] + COL_W[1] + 2 * GAP]
ROW_UPPER = round((BOX_H - GAP) * ROW_SHARES[0] / sum(ROW_SHARES))
BAND_H = BOX_H - GAP - ROW_UPPER
BAND_Y = INSET + ROW_UPPER + GAP
THIRD = round((COL_W[2] - 2 * GAP) / 3)  # over mastery's width; summons spans the rest

# sections: left, top, width, height in card pixels from the card's top-left corner
STATUS = (COL_X[0], INSET, COL_W[0], ROW_UPPER)
GEAR = (COL_X[1], FRAME, COL_W[1], BOX_H + INSET - FRAME)  # from the frame's border
MASTER_TRAITS = (COL_X[2], INSET, COL_W[2], ROW_UPPER)
SKILLS = (COL_X[0], BAND_Y, COL_W[0], BAND_H)
OVER_MASTERY = (COL_X[2], BAND_Y, THIRD, BAND_H)
SUMMONS = (COL_X[2] + THIRD + GAP, BAND_Y, COL_W[2] - THIRD - GAP, BAND_H)

# panels, in card pixels
PAD = 10  # on every side
ICON_PAD = 5  # a skill icon's inset in its cell

# the status-style panels' scale: the skills block's 1000 units across its section
PANEL_SCALE = SKILLS[2] / 1000
TITLE_BAR_H = 20  # title units

# rounded corners of radius 7, at scale 1
OUTLINE = "layouts/pause/status/noatlastextures/bc_outline"
MT_CLIP = "layouts/pause/status/noatlastextures/bc_mt_clip"

# the gear column's stack, in bc_weapon units from its root's pivot
WEAPON_TOP = 328  # the outer padding and a bar height above the weapon bar's top
SKILLS_TOP = -305  # under the stat row
PENDULUM_GAP = 386  # loc_skill02's top below loc_skill01's
PENDULUM_H = 274  # loc_skill02's title and three rows
STACK_GAP = 65
GENE_H = 936
PENDULUM_TOP = SKILLS_TOP - PENDULUM_GAP
SIGILS_TITLE_TOP = PENDULUM_TOP - PENDULUM_H - STACK_GAP - 2 * TITLE_BAR_H
GENE_TOP = SIGILS_TITLE_TOP - 2 * TITLE_BAR_H

# bc_weapon's scale, the stack across the gear section's height, and every title's
TITLE_SCALE = GEAR[3] / (WEAPON_TOP - GENE_TOP + GENE_H)

# puts the section headings' bar top the frame's gap below the panel's top
HEADING_MARGIN = INSET - FRAME - TITLE_BAR_H * TITLE_SCALE - PAD

# loc_buildcard's children in order, each added by its section's step
CARD_ORDER = ["bc_mtraits", "bc_om", "bc_smn", "bc_weapon", "bc_skills", "bc_frame"]


def insert(parent, node, order):
    # adds node to parent at its slot in order, after the present children that precede it
    rank = {name: i for i, name in enumerate(order)}
    for child in parent.children:
        if child.name not in rank:
            raise KeyError(f"{parent.path}: {child.name} has no slot")
    index = sum(1 for child in parent.children if rank[child.name] < rank[node.name])
    return parent.add(node, index)


def section(card, name):
    # a container over the whole card at the section's slot under loc_buildcard
    node = insert(card, rect(name), CARD_ORDER)
    node.place(pos=(0, 0), size=(CARD_W, CARD_H))
    return node


def sprite(path):
    # a texture path and its sprite name
    return path, path.rsplit("/", 1)[1]


def mt_clip(name):
    # a mask over the master traits section with rounded corners
    return rect(name, mask(sprite(MT_CLIP)))


def card(x, y):
    # card pixels from the card's top-left corner to loc_buildcard's centre
    return x - CARD_W / 2, CARD_H / 2 - y


def in_card(node):
    # whether the node lies under loc_buildcard
    while node is not None and node.path != CARD_PATH:
        node = node.parent
    return node is not None


def move(node, x, y):
    # moves the node's pivot to card pixels from loc_buildcard's centre
    ax, ay, parent = 0, 0, node
    while parent.path not in (CARD_PATH, BASE_PATH):
        px, py = parent.vec("Position")[:2]
        ax, ay, parent = ax + px, ay + py, parent.parent
    if parent.path == BASE_PATH:
        x, y = x * CARD_SCALE, y * CARD_SCALE
    px, py = node.vec("Position")[:2]
    node.place(pos=(px + x - ax, py + y - ay))


def set_scale(node, k):
    # sets the node's scale in card pixels
    k = k if in_card(node) else k * CARD_SCALE
    node.set("Scale", (k, k, 1))


def place(node, x, y, w, h):
    # places node by its pivot at card pixels from the card's top-left corner
    node.place(pos=card(x, y), size=(w, h))
