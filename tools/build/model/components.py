# Builds new objects and their components as Nodes.
from model.prefab import Node, Ref, f


RECT = ["Position", "Rotation", "Scale", "Pivot", "AnchorPoint", "AnchorMin", "AnchorMax", "OffsetMin", "OffsetMax",
        "SizeDelta"]


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


def ref_field(name, component, target, listed=False):
    # a field referencing the target's component, as a list of one when listed
    head = "      - " if listed else "        "
    return [f"      {name}:", f"{head}ComponentName: {component}", "        Index: 0", Ref("        ObjectRefId: ", target)]


def mask(sprite):
    # sprite: (texture path, sprite name)
    return ["  - ComponentName: Mask", "    Component:", "      Sprite:", f"        TexturePath: {sprite[0]}",
            f"        SpriteName: {sprite[1]}", "      Offset: 0, 0", "      ChannelWeights: 0, 0, 0, 1",
            "      InvertMask: false", "      InvertOutsides: false", "      Enable: true"]


def image(color, alpha, sprite=None, sliced=False, fill_center=False):
    # sprite: (texture path, sprite name)
    sprite_lines = ["      Sprite:", f"        TexturePath: {sprite[0]}", f"        SpriteName: {sprite[1]}"] if sprite else []
    return ["  - ComponentName: Image", "    Component:", f"      Color: {', '.join(f(c) for c in color)}, {f(alpha)}",
            *sprite_lines, f"      Type: {int(sliced)}", f"      FillCenter: {str(fill_center).lower()}", "      FillMethod: 0",
            "      FillOrigin: 0", "      FillAmount: 0", "      UvRect: 0, 0, 0, 0", "      RawImage: false",
            "      Clockwise: false", "      PreserveAspect: false", "      E3ED5266: 0", "      Enable: true"]


FONT = "fonts/fot_skipstd_b_sdf"
LEFT, CENTER, RIGHT = 513, 514, 516  # Text.Alignment


def text(value, size, color, alpha, alignment, line_spacing=0, eng_line_spacing=None):
    # a Text in the game's body font, with its LanguageSetter
    return ["  - ComponentName: Text", "    Component:", f"      Text: '{value}'", f"      FontPath: {FONT}",
            f"      MaterialPath: {FONT}/fot_skipstd_b_sdf_material", f"      FontSize: {f(size)}",
            f"      Color: {', '.join(f(c) for c in color)}, {f(alpha)}", "      Margin: 0, 0, 0, 0",
            "      IsGradient: false", "      ColorMode: 3", "      ColorTL: 1, 1, 1, 1", "      ColorTR: 1, 1, 1, 1",
            "      ColorBL: 1, 1, 1, 1", "      ColorBR: 1, 1, 1, 1", "      CharacterSpacing: 0",
            f"      LineSpacing: {line_spacing}", f"      Alignment: {alignment}", "      Enable: true",
            *language_setter(eng_line_spacing)]


def language_setter(eng_line_spacing=None):
    # the font of each language, and the English line spacing if given
    enable_ls = "true" if eng_line_spacing is not None else "false"
    return ["  - ComponentName: LanguageSetter", "    Component:", "      MultiData: false",
            "      LanguageData: data/language/ld_skipstd_b_sdf_material", "      Overwrites:", "      - Language: Eng",
            "        EnableFS: false", "        FontSize: 0", "        EnableCS: false", "        CharacterSpaching: 0",
            f"        EnableLS: {enable_ls}", f"        LineSpaching: {eng_line_spacing or 0}", "        EnableMG: false",
            "        Margine: 0, 0, 0, 0", "        EnableAL: false", "        Alignment: 0", "      Enable: true"]
