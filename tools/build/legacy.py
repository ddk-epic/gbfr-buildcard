# First build step: starts each prefab from its frozen copy and resets the ported paths to stock.
from context import TARGETS, git_show
from model.prefab import Prefab, Ref, copy

# the commit the frozen prefabs are read from
FROZEN = "5f5f45671a159215941f4632267213071ae516c9"

# per prefab, the paths reset to stock, or removed where stock has none
PORTED = {
    "status01": [
        "root/loc_base01/loc_buildcard/bc_frame",
        "root/loc_base01/loc_buildcard/bc_om",
        "root/loc_base01/loc_buildcard/bc_om_ttl_text",
        "root/loc_base01/loc_buildcard/bc_smn",
        "root/loc_base01/loc_buildcard/bc_skills",
        "root/loc_base01/loc_status02/loc_chr_status03",
        "root/loc_base01/loc_status02/loc_chr_status04",
        "root/loc_base01/loc_status02/loc_chr_status01",
        "root/loc_base01/loc_name01",
        "root/loc_base01/loc_status01/power01",
        "root/loc_base01/loc_status01/level01",
        "root/loc_base01/loc_status01/loc_ml_level01",
        "root/loc_base01/loc_chr",
        "root/loc_base01/loc_buildcard/bc_weapon",
        "root/loc_base01/loc_status02/loc_chr_status02",
        "root/loc_base01/loc_buildcard/bc_mtraits",
    ],
    "chr_status_bg01": [
        "root/loc_bg/bg01",
    ],
}


def frozen(name):
    return Prefab.parse(git_show(FROZEN, TARGETS[name]))


def apply(ctx):
    for name in TARGETS:
        prefab = frozen(name)
        if PORTED[name]:
            reset(prefab, ctx.stock(name), PORTED[name])
        ctx.prefabs[name] = prefab


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
