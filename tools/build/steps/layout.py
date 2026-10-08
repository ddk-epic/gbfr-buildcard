# Measurements and orders shared by the steps.
from model.components import rect

# loc_buildcard units per sharecard pixel
S = 3424 / 2880
CARD_W, CARD_H = 2880, 1440  # sharecard pixels

# sections: left, top, width, height in sharecard pixels
SKILLS = (16, 1142, 560, 262)
OVER_MASTERY = (1380, 1142, 491.33, 262)
SUMMONS = (1876.33, 1142, 987.67, 262)

# panels, in sharecard pixels
PAD_X, PAD_Y = 10, 18.5  # border and padding
ICON_PAD = 5  # pl-1 of sharecard's compact skill cell

# the status-style panels' scale: the skills block's 1000 units across its section
PANEL_SCALE = SKILLS[2] * S / 1000
TITLE_SCALE = 0.705  # bc_weapon's
TITLE_TEXT = 30  # a title text's top above its bar's top, in title units

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


def place(node, x, y, w, h):
    # places node by its pivot at sharecard pixels on the card, sized in sharecard pixels
    node.place(pos=card(x, y), size=(w * S, h * S))
