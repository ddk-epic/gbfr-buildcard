# Measurements and orders shared by the steps.
from model.components import rect

# loc_buildcard units per sharecard pixel
S = 3424 / 2880
CARD_W, CARD_H = 2880, 1440  # sharecard pixels

# sections: left, top, width, height in sharecard pixels
STATUS = (16, 16, 560, 1102)
GEAR = (600, 16, 756, 1388)
MASTER_TRAITS = (1380, 16, 1484, 1102)
SKILLS = (16, 1142, 560, 262)
OVER_MASTERY = (1380, 1142, 491.33, 262)
SUMMONS = (1876.33, 1142, 987.67, 262)

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
