# A prefab YAML as a tree of objects, with Ids assigned in depth-first order on save.
import re

REF = re.compile(r"(\s+ObjectRefId: )(-?\d+)$")


def f(v):
    v = round(v, 3)
    return str(int(v)) if v == int(v) else str(v)


def value(v):
    # a field value as the YAML writes it
    if isinstance(v, (list, tuple)):
        return ", ".join(f(x) for x in v)
    if isinstance(v, bool):
        return str(v).lower()
    return str(v)


HEADER = "  - ComponentName: "


def is_top(line):
    # an object-level key, as Name or Active
    return isinstance(line, str) and line.startswith("  ") and line[2] not in " -"


def is_field(line):
    # a component-level key
    return isinstance(line, str) and line.startswith("      ") and line[6] not in " -"


def key_of(line):
    # the key of a line at any depth, or None
    if not isinstance(line, str):
        return None
    stripped = line.lstrip()
    return stripped.split(": ", 1)[0].rstrip(":") if ":" in stripped and not stripped.startswith("- ") else None


class Ref:
    # an ObjectRefId line
    __slots__ = ("prefix", "node")

    def __init__(self, prefix, node):
        self.prefix, self.node = prefix, node


class Component:
    def __init__(self, node, name):
        self.node, self.name = node, name

    def _span(self):
        # its fields' lines, after the ComponentName and Component lines
        _, start, end = next(span for span in self.node._spans() if span[0] == self.name)
        return start + 2, end

    def _keyed(self, key):
        start, end = self._span()
        found = [i for i in range(start, end) if key_of(self.node.lines[i]) == key]
        if not found:
            raise KeyError(f"{self.node.path}.{self.name}: {key}")
        return found

    def get(self, key):
        # the first line with the key, at any depth
        return self.node.lines[self._keyed(key)[0]].split(": ", 1)[1]

    def set(self, key, v):
        self.update(key, lambda _: value(v), first=True)

    def update(self, key, fn, first=False):
        # every line with the key, or the first, set to fn of its value
        for i in self._keyed(key)[:1 if first else None]:
            line = self.node.lines[i]
            self.node.lines[i] = f"{line[:len(line) - len(line.lstrip())]}{key}: {fn(line.split(': ', 1)[1])}"

    def _field(self, name):
        # the field's line and the end of its lines
        start, end = self._span()
        lines = self.node.lines
        i = next((i for i in range(start, end) if is_field(lines[i]) and key_of(lines[i]) == name), None)
        if i is None:
            raise KeyError(f"{self.node.path}.{self.name}: {name}")
        j = i + 1
        while j < end and not is_field(lines[j]):
            j += 1
        return i, j

    def field(self, name):
        i, j = self._field(name)
        return self.node.lines[i:j]

    def insert(self, lines, before=None, after=None):
        # fields, before or after the named field
        i = self._field(before)[0] if before else self._field(after)[1]
        self.node.lines[i:i] = lines

    def items(self, name):
        return [line[len("      - "):] for line in self.field(name)[1:]]

    def set_items(self, name, items):
        i, j = self._field(name)
        self.node.lines[i + 1:j] = [f"      - {item}" for item in items]

    def set_refs(self, name, targets):
        # points the field's references at the targets, in order
        i, j = self._field(name)
        refs = [k for k in range(i, j) if isinstance(self.node.lines[k], Ref)]
        if len(refs) < len(targets):
            raise KeyError(f"{self.node.path}.{self.name}.{name}: {len(refs)} references for {len(targets)} targets")
        for k, target in zip(refs, targets):
            self.node.lines[k] = Ref(self.node.lines[k].prefix, target)

    def drop_ref(self, target):
        # removes the list entry referencing the target
        start, end = self._span()
        k = next((k for k in range(start, end) if isinstance(self.node.lines[k], Ref) and self.node.lines[k].node is target),
                 None)
        if k is None:
            raise KeyError(f"{self.node.path}.{self.name}: no reference to {target.path}")
        del self.node.lines[k - 2:k + 1]


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

    def set(self, key, v):
        self.lines[self._line(key)] = f"  {key}: {value(v)}"

    # --- components

    def _block(self):
        # the Components block's line range, empty after Name when there is none
        if "  Components:" not in self.lines:
            return 1, 1
        start = self.lines.index("  Components:") + 1
        end = start
        while end < len(self.lines) and not is_top(self.lines[end]):
            end += 1
        return start, end

    def _spans(self):
        # (name, start, end) of each component
        start, end = self._block()
        heads = [i for i in range(start, end) if isinstance(self.lines[i], str) and self.lines[i].startswith(HEADER)]
        return [(self.lines[i][len(HEADER):], i, j) for i, j in zip(heads, heads[1:] + [end])]

    def components(self):
        return [name for name, _, _ in self._spans()]

    def component(self, name):
        found = self.components().count(name)
        if found != 1:
            raise KeyError(f"{self.path}: {found} {name} components")
        return Component(self, name)

    def component_lines(self):
        # every component's lines; raises on components with references
        start, end = self._block()
        lines = self.lines[start:end]
        if any(isinstance(line, Ref) for line in lines):
            raise ValueError(f"{self.path}: components with references")
        return lines

    def set_components(self, lines):
        start, end = self._block()
        if "  Components:" not in self.lines:
            if lines:
                self.lines[1:1] = ["  Components:", *lines]
        else:
            self.lines[start:end] = lines

    def add_component(self, lines):
        start, end = self._block()
        self.set_components(self.lines[start:end] + list(lines))

    def drop_component(self, name):
        self.component(name)
        _, start, end = next(span for span in self._spans() if span[0] == name)
        del self.lines[start:end]

    def keep_components(self, *names):
        self.set_components([line for name, start, end in self._spans() if name in names
                             for line in self.lines[start:end]])

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
