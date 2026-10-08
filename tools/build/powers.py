# Last build step: orders CharaInfo's Powers list, stock's entries in stock order, then the mod's in tree order.
from model.prefab import Ref

START = "      Powers:"


def entries(prefab):
    # the list's first line index, and its entries as line lists
    lines = prefab.root.lines
    start = lines.index(START) + 1
    found = []
    for line in lines[start:]:
        if isinstance(line, str) and line.startswith("      - "):
            found.append([line])
        elif isinstance(line, Ref) or line.startswith("        "):
            found[-1].append(line)
        else:
            break
    for entry in found:
        if not isinstance(entry[-1], Ref):
            raise ValueError(f"Powers entry without an object: {entry[0].strip()}")
    return start, found


def nodes(prefab):
    # the nodes CharaInfo.Powers references
    return {entry[-1].node for entry in entries(prefab)[1]}


def add(prefab, node, component=""):
    # appends a reference to the node's component, or to the node when component is empty
    start, found = entries(prefab)
    end = start + sum(len(entry) for entry in found)
    name, index = (component, 0) if component else ("''", -1)
    prefab.root.lines[end:end] = [f"      - ComponentName: {name}", f"        Index: {index}",
                                  Ref("        ObjectRefId: ", node)]


def order(prefab, stock):
    start, found = entries(prefab)
    stock_paths = [entry[-1].node.path for entry in entries(stock)[1]]
    ids = prefab.ids()

    def key(entry):
        node = entry[-1].node
        if node.path in stock_paths:
            return 0, stock_paths.index(node.path)
        return 1, ids[node]

    lines = prefab.root.lines
    end = start + sum(len(entry) for entry in found)
    lines[start:end] = [line for entry in sorted(found, key=key) for line in entry]


def apply(ctx):
    order(ctx.prefab("status01"), ctx.stock("status01"))
