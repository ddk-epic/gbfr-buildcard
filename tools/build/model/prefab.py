# A prefab YAML as a tree of objects, with Ids assigned in depth-first order on save.
import re

REF = re.compile(r"(\s+ObjectRefId: )(-?\d+)$")


def f(v):
    v = round(v, 3)
    return str(int(v)) if v == int(v) else str(v)


class Ref:
    # an ObjectRefId line
    __slots__ = ("prefix", "node")

    def __init__(self, prefix, node):
        self.prefix, self.node = prefix, node


class Node:
    # lines: the object's lines without Id and Children; lines[0] is its Name
    def __init__(self, lines):
        self.lines = list(lines)
        self.children = []
        self.parent = None
        self.source_id = None  # the Id in the file the node was loaded from

    def __repr__(self):
        return f"<{self.path}>"

    @property
    def name(self):
        return self.lines[0][len("  Name: "):]

    @name.setter
    def name(self, value):
        self.lines[0] = f"  Name: {value}"

    # --- tree

    def add(self, child, index=None):
        if child.parent is not None:
            child.parent.children.remove(child)
        child.parent = self
        self.children.insert(len(self.children) if index is None else index, child)
        return child

    def remove(self):
        self.parent.children.remove(self)
        self.parent = None

    def walk(self):
        # depth-first, the node first
        stack = [self]
        while stack:
            node = stack.pop()
            yield node
            stack.extend(reversed(node.children))

    def child(self, name):
        found = [c for c in self.children if c.name == name]
        if len(found) != 1:
            raise KeyError(f"{self.path}: {len(found)} children named {name}")
        return found[0]

    def find(self, path):
        # each /-separated name matches exactly one descendant of the previous match
        node = self
        for name in path.split("/"):
            found = [n for n in node.walk() if n is not node and n.name == name]
            if len(found) != 1:
                raise KeyError(f"{node.path}: {len(found)} descendants named {name}")
            node = found[0]
        return node

    @property
    def segment(self):
        # the name, with #k for the k-th later sibling of the same name
        if self.parent is None:
            return self.name
        name, k = self.name, 0
        for sibling in self.parent.children:
            if sibling is self:
                break
            k += sibling.name == name
        return name if k == 0 else f"{name}#{k}"

    @property
    def path(self):
        # unique within the prefab, from the root's child down
        parts = []
        node = self
        while node.parent is not None:
            parts.append(node.segment)
            node = node.parent
        return "/".join(reversed(parts))

    # --- top-level fields

    def _line(self, key):
        prefix = f"  {key}: "
        for i, line in enumerate(self.lines):
            if isinstance(line, str) and line.startswith(prefix):
                return i
        raise KeyError(f"{self.path}.{key}")

    def get(self, key):
        return self.lines[self._line(key)].split(": ", 1)[1]

    def vec(self, key):
        return [float(v) for v in self.get(key).split(", ")]

    def set(self, key, value):
        if isinstance(value, (list, tuple)):
            value = ", ".join(f(v) for v in value)
        elif isinstance(value, bool):
            value = str(value).lower()
        self.lines[self._line(key)] = f"  {key}: {value}"

    def replace(self, old, new):
        # replaces a nested line, matched without its indentation
        for i, line in enumerate(self.lines):
            if isinstance(line, str) and line.strip() == old:
                self.lines[i] = line[: len(line) - len(line.lstrip())] + new
                return
        raise KeyError(f"{self.path}: {old}")

    # --- rects

    def size(self):
        # the rect's size, including stretched anchors
        w, h = self.vec("SizeDelta")
        amin, amax = self.vec("AnchorMin"), self.vec("AnchorMax")
        if amin != amax:
            pw, ph = self.parent.size()
            w += (amax[0] - amin[0]) * pw
            h += (amax[1] - amin[1]) * ph
        return w, h

    def place(self, pos=None, size=None, pivot=None):
        # pos: the pivot relative to the parent's pivot; recomputes AnchorPoint and the offsets
        if pivot is not None:
            self.set("Pivot", pivot)
        if size is not None:
            self.set("SizeDelta", size)
        if pos is None:
            pos = self.vec("Position")[:2]
        else:
            self.set("Position", (pos[0], pos[1], 0))
        ppiv = self.parent.vec("Pivot")
        pw, ph = self.parent.size()
        piv = self.vec("Pivot")
        amin, amax = self.vec("AnchorMin"), self.vec("AnchorMax")
        rx = amin[0] + (amax[0] - amin[0]) * piv[0] - ppiv[0]
        ry = amin[1] + (amax[1] - amin[1]) * piv[1] - ppiv[1]
        ax, ay = pos[0] - rx * pw, pos[1] - ry * ph
        w, h = self.vec("SizeDelta")
        self.set("AnchorPoint", (ax, ay))
        self.set("OffsetMin", (ax - piv[0] * w, ay - piv[1] * h))
        self.set("OffsetMax", (ax + (1 - piv[0]) * w, ay + (1 - piv[1]) * h))

    def repin(self):
        # recomputes the Positions of the node's descendants from their AnchorPoints
        pw, ph = self.size()
        ppiv = self.vec("Pivot")
        for child in self.children:
            piv = child.vec("Pivot")
            amin, amax = child.vec("AnchorMin"), child.vec("AnchorMax")
            ax, ay = child.vec("AnchorPoint")
            x = ax + (amin[0] + (amax[0] - amin[0]) * piv[0] - ppiv[0]) * pw
            y = ay + (amin[1] + (amax[1] - amin[1]) * piv[1] - ppiv[1]) * ph
            child.set("Position", (x, y, 0))
            child.repin()

    # --- references

    def refs(self):
        return [line for line in self.lines if isinstance(line, Ref)]


def copy(node, refs=None):
    # a detached deep copy of the subtree; refs maps references outside it
    originals = list(node.walk())
    copies = {}
    for old in originals:
        new = Node(old.lines)
        new.source_id = old.source_id
        copies[old] = new
    for old in originals:
        new = copies[old]
        for child in old.children:
            new.add(copies[child])
        for i, line in enumerate(new.lines):
            if isinstance(line, Ref):
                if line.node in copies:
                    target = copies[line.node]
                elif refs is not None and line.node in refs:
                    target = refs[line.node]
                else:
                    raise KeyError(f"{old.path} references {line.node.path}, outside the copied subtree")
                new.lines[i] = Ref(line.prefix, target)
    return copies[node]


def segments(node):
    # the node's children by segment
    counts, result = {}, {}
    for child in node.children:
        name = child.name
        k = counts.get(name, 0)
        counts[name] = k + 1
        result[name if k == 0 else f"{name}#{k}"] = child
    return result


class Prefab:
    def __init__(self, root):
        self.root = root

    @classmethod
    def load(cls, path):
        with open(path, encoding="utf-8", newline="") as file:
            return cls.parse(file.read())

    @classmethod
    def parse(cls, text):
        lines = text.split("\n")
        assert lines[0] == "Objects:", "not a prefab YAML"
        trailing = lines[-1] == ""
        if trailing:
            lines.pop()

        blocks = []  # (id, lines)
        for line in lines[1:]:
            if line.startswith("- Id: "):
                blocks.append((int(line[6:]), []))
            else:
                blocks[-1][1].append(line)

        nodes, children = {}, {}
        for id_, block in blocks:
            assert block[0].startswith("  Name: "), f"object {id_}: no Name first"
            kids = []
            if len(block) > 1 and block[1] == "  Children:":
                i = 2
                while i < len(block) and block[i].startswith("  - "):
                    kids.append(int(block[i][4:]))
                    i += 1
                assert kids, f"object {id_}: empty Children"
                block = block[:1] + block[i:]
            node = Node(block)
            node.source_id = id_
            nodes[id_] = node
            children[id_] = kids

        for id_, node in nodes.items():
            for child in children[id_]:
                node.add(nodes[child])
            for i, line in enumerate(node.lines):
                m = REF.match(line)
                if m and int(m[2]) >= 0:
                    node.lines[i] = Ref(m[1], nodes[int(m[2])])

        roots = [n for n in nodes.values() if n.parent is None]
        assert len(roots) == 1, f"{len(roots)} roots"
        prefab = cls(roots[0])
        prefab.trailing_newline = trailing
        return prefab

    trailing_newline = True

    def walk(self):
        return self.root.walk()

    def at(self, path):
        # the node at an exact path, as Node.path gives it
        node = self.root
        for segment in path.split("/") if path else []:
            node = segments(node).get(segment)
            if node is None:
                raise KeyError(path)
        return node

    def paths(self):
        # every node's path
        result = {self.root: ""}
        for node in self.walk():
            base = result[node] + "/" if node is not self.root else ""
            for segment, child in segments(node).items():
                result[child] = base + segment
        return result

    def find(self, path):
        return self.root.find(path)

    def ids(self):
        return {node: i for i, node in enumerate(self.walk())}

    def text(self):
        ids = self.ids()
        out = ["Objects:"]
        for node, id_ in ids.items():
            out.append(f"- Id: {id_}")
            out.append(node.lines[0])
            if node.children:
                out.append("  Children:")
                out.extend(f"  - {ids[c]}" for c in node.children)
            for line in node.lines[1:]:
                if isinstance(line, Ref):
                    if line.node not in ids:
                        raise KeyError(f"{node.path} references a node not in the prefab: {line.node.name}")
                    line = f"{line.prefix}{ids[line.node]}"
                out.append(line)
        return "\n".join(out) + ("\n" if self.trailing_newline else "")

    def save(self, path):
        text = self.text()
        with open(path, "w", encoding="utf-8", newline="") as file:
            file.write(text)
