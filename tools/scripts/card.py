# Builds card objects in status01. Used by the card section scripts.
import re
from prefab import f

S = 3424 / 2880
PARENT = 426  # loc_buildcard

FONT = "fonts/fot_skipstd_b_sdf"
INK = (0.19607843, 0.37254903, 0.4901961)
LEFT, CENTER, RIGHT = 513, 514, 516

def card(x, y):
    # converts sharecard pixels on the card to loc_buildcard coordinates
    return (x - 1440) * S, (720 - y) * S

def rect(name, comps, pivot, active=True):
    return [f"  Name: {name}", "  Components:", *comps, f"  Active: {str(active).lower()}", "  Position: 0, 0, 0",
            "  Rotation: 0, 0, 0, 1", "  Scale: 1, 1, 1", f"  Pivot: {f(pivot[0])}, {f(pivot[1])}",
            "  AnchorPoint: 0, 0", "  AnchorMin: 0.5, 0.5", "  AnchorMax: 0.5, 0.5", "  OffsetMin: 0, 0",
            "  OffsetMax: 0, 0", "  SizeDelta: 0, 0"]

def image(color, alpha, sprite=None):
    # sprite: (texture path, sprite name)
    sprite_lines = ["      Sprite:", f"        TexturePath: {sprite[0]}", f"        SpriteName: {sprite[1]}"] if sprite else []
    return ["  - ComponentName: Image", "    Component:", f"      Color: {', '.join(f(c) for c in color)}, {f(alpha)}",
            *sprite_lines, "      Type: 0", "      FillCenter: false", "      FillMethod: 0", "      FillOrigin: 0", "      FillAmount: 0",
            "      UvRect: 0, 0, 0, 0", "      RawImage: false", "      Clockwise: false", "      PreserveAspect: false",
            "      E3ED5266: 0", "      Enable: true"]

def text(value, size, color, alpha, alignment, line_spacing=0, eng_line_spacing=None):
    return ["  - ComponentName: Text", "    Component:", f"      Text: '{value}'", f"      FontPath: {FONT}",
            f"      MaterialPath: {FONT}/fot_skipstd_b_sdf_material", f"      FontSize: {f(size)}",
            f"      Color: {', '.join(f(c) for c in color)}, {f(alpha)}", "      Margin: 0, 0, 0, 0",
            "      IsGradient: false", "      ColorMode: 3", "      ColorTL: 1, 1, 1, 1", "      ColorTR: 1, 1, 1, 1",
            "      ColorBL: 1, 1, 1, 1", "      ColorBR: 1, 1, 1, 1", "      CharacterSpacing: 0", f"      LineSpacing: {line_spacing}",
            f"      Alignment: {alignment}", "      Enable: true", *language_setter(eng_line_spacing)]

def language_setter(eng_line_spacing=None):
    # the font of each language, and the English line spacing if given
    enable_ls = "true" if eng_line_spacing is not None else "false"
    return ["  - ComponentName: LanguageSetter", "    Component:", "      MultiData: false",
            "      LanguageData: data/language/ld_skipstd_b_sdf_material", "      Overwrites:", "      - Language: Eng",
            "        EnableFS: false", "        FontSize: 0", "        EnableCS: false", "        CharacterSpaching: 0",
            f"        EnableLS: {enable_ls}", f"        LineSpaching: {eng_line_spacing or 0}", "        EnableMG: false",
            "        Margine: 0, 0, 0, 0", "        EnableAL: false", "        Alignment: 0", "      Enable: true"]

class Group:
    # objects in sharecard pixels on the card: (lines, x, y, w, h, pivot)
    def __init__(self):
        self.images, self.texts = [], []

    def image(self, name, comps, x, y, w, h, pivot=(0, 1), active=True):
        self.images.append((rect(name, comps, pivot, active), x, y, w, h, pivot))

    def text(self, name, comps, x, y, w, h, pivot=(0, 1)):
        self.texts.append((rect(name, comps, pivot), x, y, w, h, pivot))

def append(p, name, group):
    # Appends the group in a container under loc_buildcard. Returns the ids of the container, the images and the texts.
    container = max(p.starts) + 1
    objects = group.images + group.texts
    ids = list(range(container + 1, container + 1 + len(objects)))
    start, end = p.range(PARENT)
    last_child = max(i for i in range(start, end) if p.lines[i].startswith("  - "))
    p.lines.insert(last_child + 1, f"  - {container}")
    end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline
    new = [f"- Id: {container}", f"  Name: {name}", "  Children:", *[f"  - {i}" for i in ids], "  Active: true",
           "  Position: 0, 0, 0", "  Rotation: 0, 0, 0, 1", "  Scale: 1, 1, 1", "  Pivot: 0.5, 0.5",
           "  AnchorPoint: 0, 0", "  AnchorMin: 0.5, 0.5", "  AnchorMax: 0.5, 0.5", "  OffsetMin: -1712, -856",
           "  OffsetMax: 1712, 856", "  SizeDelta: 3424, 1712"]
    for id_, (lines, *_) in zip(ids, objects):
        new += [f"- Id: {id_}", *lines]
    p.lines[end:end] = new
    p.reindex()
    for id_, (_, x, y, w, h, pivot) in zip(ids, objects):
        p.place(id_, pos=card(x, y), size=(w * S, h * S))
    return container, ids[:len(group.images)], ids[len(group.images):]

def add_powers(p, ids, component="Text"):
    # appends refs to the objects' components, or to the objects when component is empty, to CharaInfo.Powers
    i = p.lines.index("      Powers:", *p.range(0)) + 1
    while p.lines[i].startswith("      - ") or p.lines[i].startswith("        "):
        i += 1
    name, index = (component, 0) if component else ("''", -1)
    p.lines[i:i] = [line for id_ in ids
                    for line in (f"      - ComponentName: {name}", f"        Index: {index}", f"        ObjectRefId: {id_}")]

def copy_objects(source, objects, ids, edit=None):
    # the objects' lines from another prefab, with Ids, Children and refs remapped through ids; edit(old id, lines) may
    # change an object's lines
    lines = []
    for old in objects:
        start, end = source.range(old)
        block = source.lines[start:end]
        if edit:
            block = edit(old, block)
        out = []
        in_children = False
        for line in block:
            if line == "  Children:":
                in_children = True
            elif in_children and line.startswith("  - "):
                child = int(line[4:])
                if child in ids:
                    out.append(f"  - {ids[child]}")
                continue
            else:
                in_children = False
            m = re.match(r"(\s+ObjectRefId: )(-?\d+)$", line)
            if m and int(m[2]) != -1:
                if int(m[2]) not in ids:
                    raise ValueError(f"{old} references {m[2]}, which is not copied")
                line = f"{m[1]}{ids[int(m[2])]}"
            out.append(line)
        out[0] = f"- Id: {ids[old]}"
        lines += out
    return lines

def keep_components(block, names):
    # removes the components not in names from an object's lines
    i = block.index("  Components:") + 1
    out, keep = block[:i], True
    while block[i].startswith("  - ") or block[i].startswith("    "):
        if block[i].startswith("  - ComponentName: "):
            keep = block[i][len("  - ComponentName: "):] in names
        if keep:
            out.append(block[i])
        i += 1
    return out + block[i:]
