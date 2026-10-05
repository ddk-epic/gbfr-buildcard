# Reads and edits a prefab YAML's objects and rects in place. Used by the layout scripts.
import re

def f(v):
    v = round(v, 3)
    return str(int(v)) if v == int(v) else str(v)

class Prefab:
    def __init__(self, path):
        text = open(path, encoding="utf-8").read()
        self.nl = "\r\n" if "\r\n" in text else "\n"
        self.lines = text.split(self.nl)
        self.reindex()

    def reindex(self):
        # call after adding or removing lines
        self.starts = {}
        self.parents = {}
        for i, line in enumerate(self.lines):
            if line.startswith("- Id: "):
                self.starts[int(line[6:])] = i
        for id_ in self.starts:
            for child in self.children(id_):
                self.parents[child] = id_

    def save(self, path):
        open(path, "w", encoding="utf-8", newline="").write(self.nl.join(self.lines))

    def range(self, id_):
        start = self.starts[id_]
        end = start + 1
        while end < len(self.lines) and not self.lines[end].startswith("- Id: "):
            end += 1
        return start, end

    def children(self, id_):
        start, end = self.range(id_)
        if "  Children:" not in self.lines[start:end]:
            return []
        i = self.lines.index("  Children:", start, end) + 1
        result = []
        while self.lines[i].startswith("  - "):
            result.append(int(self.lines[i][4:]))
            i += 1
        return result

    def _line(self, id_, key):
        start, end = self.range(id_)
        for i in range(start, end):
            if self.lines[i].startswith(f"  {key}: "):
                return i
        raise KeyError(f"{id_}.{key}")

    def get(self, id_, key):
        return self.lines[self._line(id_, key)].split(": ", 1)[1]

    def vec(self, id_, key):
        return [float(v) for v in self.get(id_, key).split(", ")]

    def set(self, id_, key, value):
        if isinstance(value, (list, tuple)):
            value = ", ".join(f(v) for v in value)
        self.lines[self._line(id_, key)] = f"  {key}: {value}"

    def replace(self, id_, old, new):
        # replaces a nested line, matched without its indentation
        start, end = self.range(id_)
        for i in range(start, end):
            if self.lines[i].strip() == old:
                self.lines[i] = self.lines[i][: len(self.lines[i]) - len(self.lines[i].lstrip())] + new
                return
        raise KeyError(f"{id_}: {old}")

    def size(self, id_):
        # the rect's actual size: stretched anchors add the parent's size
        w, h = self.vec(id_, "SizeDelta")
        amin, amax = self.vec(id_, "AnchorMin"), self.vec(id_, "AnchorMax")
        if amin != amax:
            pw, ph = self.size(self.parents[id_])
            w += (amax[0] - amin[0]) * pw
            h += (amax[1] - amin[1]) * ph
        return w, h

    def place(self, id_, pos=None, size=None, pivot=None):
        # pos: the pivot relative to the parent's pivot; recomputes AnchorPoint and the offsets
        if pivot is not None:
            self.set(id_, "Pivot", pivot)
        if size is not None:
            self.set(id_, "SizeDelta", size)
        if pos is None:
            pos = self.vec(id_, "Position")[:2]
        else:
            self.set(id_, "Position", (pos[0], pos[1], 0))
        parent = self.parents[id_]
        ppiv = self.vec(parent, "Pivot")
        pw, ph = self.size(parent)
        piv = self.vec(id_, "Pivot")
        amin, amax = self.vec(id_, "AnchorMin"), self.vec(id_, "AnchorMax")
        rx = amin[0] + (amax[0] - amin[0]) * piv[0] - ppiv[0]
        ry = amin[1] + (amax[1] - amin[1]) * piv[1] - ppiv[1]
        ax, ay = pos[0] - rx * pw, pos[1] - ry * ph
        w, h = self.vec(id_, "SizeDelta")
        self.set(id_, "AnchorPoint", (ax, ay))
        self.set(id_, "OffsetMin", (ax - piv[0] * w, ay - piv[1] * h))
        self.set(id_, "OffsetMax", (ax + (1 - piv[0]) * w, ay + (1 - piv[1]) * h))

    def repin(self, id_):
        # recomputes the Positions of the object's descendants from their AnchorPoints
        for child in self.children(id_):
            pw, ph = self.size(id_)
            ppiv = self.vec(id_, "Pivot")
            piv = self.vec(child, "Pivot")
            amin, amax = self.vec(child, "AnchorMin"), self.vec(child, "AnchorMax")
            ax, ay = self.vec(child, "AnchorPoint")
            x = ax + (amin[0] + (amax[0] - amin[0]) * piv[0] - ppiv[0]) * pw
            y = ay + (amin[1] + (amax[1] - amin[1]) * piv[1] - ppiv[1]) * ph
            self.set(child, "Position", (x, y, 0))
            self.repin(child)


def renumber(p, blocks, children):
    # rewrites the prefab from blocks (Id -> lines) and children (Id -> child Ids) with Ids in depth-first order;
    # returns the old -> new Id map
    order = []
    stack = [0]
    while stack:
        i = stack.pop()
        order.append(i)
        stack.extend(reversed(children[i]))
    assert sorted(order) == sorted(blocks)
    ids = {old: new for new, old in enumerate(order)}
    lines = []
    for old in order:
        lines.append(f"- Id: {ids[old]}")
        in_children = False
        for line in blocks[old][1:]:
            if line == "  Children:":
                lines.append(line)
                lines.extend(f"  - {ids[c]}" for c in children[old])
                in_children = True
                continue
            if in_children and line.startswith("  - "):
                continue
            in_children = False
            m = re.match(r"(\s+ObjectRefId: )(-?\d+)$", line)
            if m and int(m[2]) != -1:
                line = f"{m[1]}{ids[int(m[2])]}"
            lines.append(line)
    start = p.starts[0]
    end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline
    p.lines[start:end] = lines
    p.reindex()
    return ids
