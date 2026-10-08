# Measurements and orders shared by the steps.

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
