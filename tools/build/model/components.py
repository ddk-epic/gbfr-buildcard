# Builds new objects and their components as Nodes.
from model.prefab import Node, f


def rect(name, components=(), pivot=(0.5, 0.5), active=True):
    # an object with a centred zero rect
    lines = [f"  Name: {name}"]
    if components:
        lines += ["  Components:", *components]
    lines += [f"  Active: {str(active).lower()}", "  Position: 0, 0, 0", "  Rotation: 0, 0, 0, 1", "  Scale: 1, 1, 1",
              f"  Pivot: {f(pivot[0])}, {f(pivot[1])}", "  AnchorPoint: 0, 0", "  AnchorMin: 0.5, 0.5",
              "  AnchorMax: 0.5, 0.5", "  OffsetMin: 0, 0", "  OffsetMax: 0, 0", "  SizeDelta: 0, 0"]
    return Node(lines)


def image(color, alpha, sprite=None, sliced=False):
    # sprite: (texture path, sprite name)
    sprite_lines = ["      Sprite:", f"        TexturePath: {sprite[0]}", f"        SpriteName: {sprite[1]}"] if sprite else []
    return ["  - ComponentName: Image", "    Component:", f"      Color: {', '.join(f(c) for c in color)}, {f(alpha)}",
            *sprite_lines, f"      Type: {int(sliced)}", "      FillCenter: false", "      FillMethod: 0",
            "      FillOrigin: 0", "      FillAmount: 0", "      UvRect: 0, 0, 0, 0", "      RawImage: false",
            "      Clockwise: false", "      PreserveAspect: false", "      E3ED5266: 0", "      Enable: true"]
