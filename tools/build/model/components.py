# Builds new objects and their components as Nodes.
from model.prefab import Node, Ref, f


def rect(name, components=(), pivot=(0.5, 0.5), active=True):
    # an object with a centred zero rect
    lines = [f"  Name: {name}"]
    if components:
        lines += ["  Components:", *components]
    lines += [f"  Active: {str(active).lower()}", "  Position: 0, 0, 0", "  Rotation: 0, 0, 0, 1", "  Scale: 1, 1, 1",
              f"  Pivot: {f(pivot[0])}, {f(pivot[1])}", "  AnchorPoint: 0, 0", "  AnchorMin: 0.5, 0.5",
              "  AnchorMax: 0.5, 0.5", "  OffsetMin: 0, 0", "  OffsetMax: 0, 0", "  SizeDelta: 0, 0"]
    return Node(lines)


def single(node):
    # a detached copy of the object without its children; it may reference only itself
    if any(line.node is not node for line in node.refs()):
        raise ValueError(f"{node.path}: references another object")
    copied = Node(node.lines)
    copied.source_id = node.source_id
    copied.lines = [Ref(line.prefix, copied) if isinstance(line, Ref) else line for line in copied.lines]
    return copied


def set_line(node, key, value):
    # sets the first nested line with the key
    for i, line in enumerate(node.lines):
        if isinstance(line, str) and line.lstrip().startswith(f"{key}: "):
            node.lines[i] = f"{line[:len(line) - len(line.lstrip())]}{key}: {value}"
            return
    raise KeyError(f"{node.path}: {key}")


def mask(sprite):
    # sprite: (texture path, sprite name)
    return ["  - ComponentName: Mask", "    Component:", "      Sprite:", f"        TexturePath: {sprite[0]}",
            f"        SpriteName: {sprite[1]}", "      Offset: 0, 0", "      ChannelWeights: 0, 0, 0, 1",
            "      InvertMask: false", "      InvertOutsides: false", "      Enable: true"]


def components(node):
    # the object's component lines
    lines = node.lines
    if "  Components:" not in lines:
        return []
    start = lines.index("  Components:") + 1
    end = start
    while end < len(lines) and (isinstance(lines[end], Ref) or lines[end].startswith(("  - ", "    "))):
        end += 1
    if any(isinstance(line, Ref) for line in lines[start:end]):
        raise ValueError(f"{node.path}: components with references")
    return lines[start:end]


def image(color, alpha, sprite=None, sliced=False):
    # sprite: (texture path, sprite name)
    sprite_lines = ["      Sprite:", f"        TexturePath: {sprite[0]}", f"        SpriteName: {sprite[1]}"] if sprite else []
    return ["  - ComponentName: Image", "    Component:", f"      Color: {', '.join(f(c) for c in color)}, {f(alpha)}",
            *sprite_lines, f"      Type: {int(sliced)}", "      FillCenter: false", "      FillMethod: 0",
            "      FillOrigin: 0", "      FillAmount: 0", "      UvRect: 0, 0, 0, 0", "      RawImage: false",
            "      Clockwise: false", "      PreserveAspect: false", "      E3ED5266: 0", "      Enable: true"]
