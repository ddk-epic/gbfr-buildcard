# First build step: starts each prefab from its frozen copy and resets the ported paths to stock.
from context import TARGETS, git_show
from model.prefab import Prefab, Ref, copy

# the commit the frozen prefabs are read from
FROZEN = "5f5f45671a159215941f4632267213071ae516c9"

# per prefab, the paths reset to stock, or removed where stock has none
PORTED = {
    "status01": [],
    "chr_status_bg01": [],
}


def frozen(name):
    return Prefab.parse(git_show(FROZEN, TARGETS[name]))


def apply(ctx):
    for name in TARGETS:
        prefab = frozen(name)
        if PORTED[name]:
            reset(prefab, ctx.stock(name), PORTED[name])
        ctx.prefabs[name] = prefab
    card = ctx.prefab("status01").find("loc_buildcard")
    for export in EXPORTS:
        export(ctx, card)


# exports of the sections not ported yet

STYLES = 3
SLOTS = [4, 8, 8, 10]
CAPTAIN_SLOTS = [4, 8, 8, 14]


def export_master_traits(ctx, card):
    mt = card.child("bc_mtraits")
    perks = mt.child("bc_mt_perks")
    ctx.export("PerkNames", [perks.find(f"bc_mt_perk_{s}_name") for s in range(STYLES)])
    ctx.export("PerkStars", [[perks.find(f"bc_mt_perk_{s}_stars/base0{k + 1}/icon0{k + 1}_add") for k in range(3)]
                             for s in range(STYLES)])
    ctx.export("StyleTitles", [mt.child(f"bc_mt_{s}_title") for s in range(STYLES)])
    boards = [mt.child("bc_mt_cells"), mt.child("bc_mt_cells_captain")]
    ctx.export("Cells", boards[0])
    ctx.export("CaptainCells", boards[1])

    # [board][style][rank][cell]; board 1 is the captain's
    def cells(suffix):
        return [[[[board.find(f"bc_mt_{s}_{r}_{c}_{suffix}") for c in range(slots[r])] for r in range(len(slots))]
                 for s in range(STYLES)] for board, slots in zip(boards, [SLOTS, CAPTAIN_SLOTS])]
    ctx.export("RankCounts", [[[board.find(f"bc_mt_{s}_{r}_count") for r in range(len(SLOTS))] for s in range(STYLES)]
                              for board in boards])
    ctx.export("CellPicked", cells("picked"))
    ctx.export("CellOn", cells("on"))
    ctx.export("CellOff", cells("off"))


def export_over_mastery(ctx, card):
    ctx.export("OverMasteryRows", [card.find(f"bc_om/bc_om_{i}") for i in range(4)])


def export_summons(ctx, card):
    ctx.export("SummonSlots", [card.find(f"bc_smn/bc_smn_{i}") for i in range(4)])


def export_skills(ctx, card):
    # bc_skills' ability cards, in slot order
    cards = [c for c in card.child("bc_skills").children if c.name.startswith("ability_set01_btn")]
    ctx.export("SkillNames", [c.find("bc_skill_text/text01") for c in cards])


EXPORTS = [export_master_traits, export_over_mastery, export_summons, export_skills]


def reset(prefab, stock, paths):
    for path in paths:
        old = prefab.at(path)
        old_paths = {n: n.path for n in old.walk()}
        removed = set(old_paths)
        if path in stock.paths().values():
            # stock's subtree, outside references mapped by path
            stock_node = stock.at(path)
            inside = set(stock_node.walk())
            outside = {line.node for n in inside for line in n.refs() if line.node not in inside}
            new = copy(stock_node, refs={n: prefab.at(n.path) for n in outside})
            parent = old.parent
            index = parent.children.index(old)
            old.remove()
            parent.add(new, index)
            # old nodes to the new nodes of the same path
            by_path = {n.path: n for n in new.walk()}
            moved = {n: by_path.get(p) for n, p in old_paths.items()}
        else:
            old.remove()
            moved = {}
        retarget(prefab, removed, moved, stock)


def retarget(prefab, removed, moved, stock):
    # points references to removed objects at their replacement or stock's value, or drops their list entry
    stock_paths = set(stock.paths().values())
    for node in prefab.walk():
        if not any(line.node in removed for line in node.refs()):
            continue
        stock_refs = {}
        if node.path in stock_paths:
            stock_node = stock.at(node.path)
            stock_refs = {ref_key(stock_node.lines, j): line.node.path
                          for j, line in enumerate(stock_node.lines) if isinstance(line, Ref)}
        keys = {i: ref_key(node.lines, i) for i, line in enumerate(node.lines) if isinstance(line, Ref)}
        dropped = set()
        for i, key in keys.items():
            line = node.lines[i]
            if line.node not in removed:
                continue
            if moved.get(line.node) is not None:
                node.lines[i] = Ref(line.prefix, moved[line.node])
            elif key in stock_refs:
                node.lines[i] = Ref(line.prefix, prefab.at(stock_refs[key]))
            elif key[1][-1].endswith("]") and node.lines[i - 2].lstrip().startswith("- ComponentName: "):
                dropped.update((i - 2, i - 1, i))
            else:
                raise KeyError(f"{node.path or '(root)'}: {'.'.join(key[1])} references {line.node.name}, which the "
                               f"reset removes, and stock has no value for it")
        node.lines = [line for i, line in enumerate(node.lines) if i not in dropped]


def ref_key(lines, i):
    # the reference's component number and field path, with list indexes
    keys, indent = [], indentation(lines[i])
    j = i - 1
    while j >= 0:
        line = lines[j]
        if isinstance(line, Ref) or indentation(line) >= indent:
            j -= 1
            continue
        indent = indentation(line)
        if indent <= 2:
            break
        if entry(line):
            # a list entry: counts the entries before it
            n = 0
            while entry(lines[j]) or indentation(lines[j]) != indent:
                n += entry(lines[j]) and indentation(lines[j]) == indent
                j -= 1
            keys.append(f"{lines[j].strip().split(':')[0]}[{n - 1}]")
        else:
            keys.append(line.strip().split(":")[0])
        j -= 1
    component = sum(1 for line in lines[:i] if isinstance(line, str) and line.startswith("  - ComponentName: "))
    return component, tuple(reversed(keys))


def entry(line):
    return isinstance(line, str) and line.lstrip().startswith("- ")


def indentation(line):
    text = line.prefix if isinstance(line, Ref) else line
    return len(text) - len(text.lstrip())
