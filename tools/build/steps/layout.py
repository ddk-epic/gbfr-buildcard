# Measurements and orders shared by the steps.
from model.components import rect

# loc_buildcard units per sharecard pixel
S = 3526.72 / 2880
CARD_W, CARD_H = 2880, 1440  # sharecard pixels

# sharecard's grid inside the card's frame, in sharecard pixels
INSET = 3 * 12 / S  # bc_frame's border and twice its width
BOX_W, BOX_H = CARD_W - 2 * INSET, CARD_H - 2 * INSET
GAP = 24
SHARES = (20, 27, 53)  # status, gear, master traits
ROW_UPPER = 1102

COL_W = [(BOX_W - 2 * GAP) * share / sum(SHARES) for share in SHARES]
COL_X = [INSET, INSET + COL_W[0] + GAP, INSET + COL_W[0] + COL_W[1] + 2 * GAP]
BAND_Y = INSET + ROW_UPPER + GAP
BAND_H = INSET + BOX_H - BAND_Y
THIRD = (COL_W[2] - 2 * GAP) / 3  # over mastery's width; summons spans two and a gap

# sections: left, top, width, height in sharecard pixels
STATUS = (COL_X[0], INSET, COL_W[0], ROW_UPPER)
GEAR = (COL_X[1], INSET, COL_W[1], BOX_H)
MASTER_TRAITS = (COL_X[2], INSET, COL_W[2], ROW_UPPER)
SKILLS = (COL_X[0], BAND_Y, COL_W[0], BAND_H)
OVER_MASTERY = (COL_X[2], BAND_Y, THIRD, BAND_H)
SUMMONS = (COL_X[2] + THIRD + GAP, BAND_Y, 2 * THIRD + GAP, BAND_H)

# panels, in sharecard pixels
PAD_X, PAD_Y = 10, 18.5  # border and padding
ICON_PAD = 5  # pl-1 of sharecard's compact skill cell

# the status-style panels' scale: the skills block's 1000 units across its section
PANEL_SCALE = SKILLS[2] * S / 1000
TITLE_TEXT = 30  # a title text's top above its bar's top, in title units

# the gear column's stack, in bc_weapon units from its root's pivot
WEAPON_TOP = 314  # the weapon title text's top
SKILLS_TOP = -305  # under the stat row
PENDULUM_GAP = 396  # loc_skill02's top below loc_skill01's
PENDULUM_H = 280  # loc_skill02's title and three rows
STACK_GAP = 10
GENE_GAP = 46  # loc_gene01's top below the title bar's top
GENE_H = 960
PENDULUM_TOP = SKILLS_TOP - PENDULUM_GAP
SIGILS_TITLE_TOP = PENDULUM_TOP - PENDULUM_H - STACK_GAP - TITLE_TEXT
GENE_TOP = SIGILS_TITLE_TOP - GENE_GAP

# bc_weapon's scale, the stack across the gear section's height, and every title's
TITLE_SCALE = GEAR[3] * S / (WEAPON_TOP - GENE_TOP + GENE_H)

# loc_buildcard's children in order, each added by its section's step
CARD_ORDER = ["bc_mtraits", "bc_om", "bc_smn", "bc_weapon", "bc_skills", "bc_om_ttl_text", "bc_frame"]


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
    node.place(pos=(0, 0), size=(CARD_W * S, CARD_H * S))
    return node


def card(x, y):
    # sharecard pixels on the card to loc_buildcard units
    return (x - CARD_W / 2) * S, (CARD_H / 2 - y) * S


def move(node, x, y):
    # moves the node's pivot to loc_base01 units, the card's
    ax, ay, parent = 0, 0, node
    while parent.path != "root/loc_base01":
        px, py = parent.vec("Position")[:2]
        ax, ay, parent = ax + px, ay + py, parent.parent
    px, py = node.vec("Position")[:2]
    node.place(pos=(px + x - ax, py + y - ay))


def place(node, x, y, w, h):
    # places node by its pivot at sharecard pixels on the card, sized in sharecard pixels
    node.place(pos=card(x, y), size=(w * S, h * S))
