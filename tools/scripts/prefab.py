# Reads and edits a prefab YAML's objects and rects in place. Used by the layout scripts.

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
