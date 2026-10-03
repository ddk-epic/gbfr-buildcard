# Usage: python add_master_traits.py <status01.prfb.yaml> <out.prfb.yaml>
# Lays out sharecard's master traits board in the blue section: three style columns, each with its title and four
# rank sections of cells in a two-column grid, and moves bc_text01 to the heading's right for the perk summary.
# Images come first, then the texts, referenced from CharaInfo.Powers in the order CardWriter writes them: the
# heading, then per style the title, the style name, per rank its label and count, then per rank and cell slot a
# picked and an unpicked text.
import sys
from prefab import Prefab, f

p = Prefab(sys.argv[1])
S = 3424 / 2880
PARENT = 426  # loc_buildcard: centred in loc_base01, same coordinates
SUMMARY = 462  # bc_text01

FONT = "fonts/fot_skipstd_b_sdf"
INK = (0.19607843, 0.37254903, 0.4901961)
STYLES = [  # name, colour
    ("Insight", (0.6901961, 0.42352942, 0.9411765)),
    ("Essence", (0.29411766, 0.7019608, 0.9098039)),
    ("Crux", (0.9098039, 0.37254903, 0.41568628)),
]
SLOTS = [4, 8, 8, 10]  # cells per rank, the standard grid
RANKS = ["1", "2", "3", "EX"]

# sharecard pixels, relative to the blue section's top-left corner
SECTION_X, SECTION_Y, SECTION_W, SECTION_H = 1380, 16, 1484, 1102
HEADING_H = 50
COLUMN_GAP = 5
COLUMN_W = (SECTION_W - 2 * COLUMN_GAP) / 3
BORDER = 4
PAD_X, PAD_TOP, PAD_BOTTOM = 16, 8, 18
STYLE_HEADING_H = 80
RANK_LABEL_H = 35
CELL_H, CELL_GAP = 46, 4
CELL_W = (COLUMN_W - 2 * PAD_X - CELL_GAP) / 2
CELL_PAD = 9
CELL_LINE_SPACING = -35  # Japanese font
CELL_LINE_SPACING_ENG = 8  # PFDinTextPro

def card(x, y):
    # converts sharecard pixels in the section to loc_buildcard coordinates
    return (SECTION_X + x - 1440) * S, (720 - (SECTION_Y + y)) * S

def rect(name, comps, pivot):
    return [f"  Name: {name}", "  Components:", *comps, "  Active: true", "  Position: 0, 0, 0",
            "  Rotation: 0, 0, 0, 1", "  Scale: 1, 1, 1", f"  Pivot: {f(pivot[0])}, {f(pivot[1])}",
            "  AnchorPoint: 0, 0", "  AnchorMin: 0.5, 0.5", "  AnchorMax: 0.5, 0.5", "  OffsetMin: 0, 0",
            "  OffsetMax: 0, 0", "  SizeDelta: 0, 0"]

def image(color, alpha):
    return ["  - ComponentName: Image", "    Component:", f"      Color: {', '.join(f(c) for c in color)}, {f(alpha)}",
            "      Type: 0", "      FillCenter: false", "      FillMethod: 0", "      FillOrigin: 0", "      FillAmount: 0",
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
    # sets the font of each language on its Text (PFDinTextPro for English), and the English line spacing if given
    enable_ls = "true" if eng_line_spacing is not None else "false"
    return ["  - ComponentName: LanguageSetter", "    Component:", "      MultiData: false",
            "      LanguageData: data/language/ld_skipstd_b_sdf_material", "      Overwrites:", "      - Language: Eng",
            "        EnableFS: false", "        FontSize: 0", "        EnableCS: false", "        CharacterSpaching: 0",
            f"        EnableLS: {enable_ls}", f"        LineSpaching: {eng_line_spacing or 0}", "        EnableMG: false",
            "        Margine: 0, 0, 0, 0", "        EnableAL: false", "        Alignment: 0", "      Enable: true"]

LEFT, CENTER, RIGHT = 513, 514, 516
images, texts = [], []  # (lines, x, y, w, h, pivot), in sharecard pixels

def add(group, name, comps, x, y, w, h, pivot=(0, 1)):
    group.append((rect(name, comps, pivot), x, y, w, h, pivot))

add(texts, "bc_mt_heading", text("MASTER TRAITS", 30, INK, 1, LEFT), 8, 4, 600, 40)
for s, (style, color) in enumerate(STYLES):
    x0, y0 = s * (COLUMN_W + COLUMN_GAP), HEADING_H
    add(images, f"bc_mt_{s}_border", image(color, 1), x0, y0, COLUMN_W, BORDER)
    top = y0 + BORDER + PAD_TOP + STYLE_HEADING_H
    rows = [(n + 1) // 2 for n in SLOTS]
    heights = [RANK_LABEL_H + r * CELL_H + (r - 1) * CELL_GAP for r in rows]
    spare = (SECTION_H - PAD_BOTTOM - top - sum(heights)) / (len(SLOTS) - 1)
    rank_tops = [top + sum(heights[:r]) + r * spare for r in range(len(SLOTS))]
    for r, rank_top in enumerate(rank_tops):
        for c in range(SLOTS[r]):
            cx = x0 + PAD_X + (c % 2) * (CELL_W + CELL_GAP)
            cy = rank_top + RANK_LABEL_H + (c // 2) * (CELL_H + CELL_GAP)
            add(images, f"bc_mt_{s}_{r}_{c}_fill", image(INK, 0.07), cx, cy, CELL_W, CELL_H)

    add(texts, f"bc_mt_{s}_title", text("", 26, INK, 1, CENTER), x0, y0 + BORDER + PAD_TOP + 12, COLUMN_W, 32)
    add(texts, f"bc_mt_{s}_style", text(style.upper(), 15, color, 1, CENTER), x0, y0 + BORDER + PAD_TOP + 12 + 32 + 8,
        COLUMN_W, 20)
    for r, rank_top in enumerate(rank_tops):
        add(texts, f"bc_mt_{s}_{r}_label", text(f"STYLE RANK {RANKS[r]}", 17, INK, 0.8, LEFT),
            x0 + PAD_X + 2, rank_top, 300, 28)
        add(texts, f"bc_mt_{s}_{r}_count", text("", 19, INK, 1, RIGHT), x0 + COLUMN_W - PAD_X, rank_top, 200, 28,
            (1, 1))
    for r, rank_top in enumerate(rank_tops):
        for c in range(SLOTS[r]):
            cx = x0 + PAD_X + (c % 2) * (CELL_W + CELL_GAP) + CELL_PAD
            cy = rank_top + RANK_LABEL_H + (c // 2) * (CELL_H + CELL_GAP)
            for state, alpha in (("on", 1), ("off", 0.4)):
                add(texts, f"bc_mt_{s}_{r}_{c}_{state}",
                    text("", 19, INK, alpha, LEFT, CELL_LINE_SPACING, CELL_LINE_SPACING_ENG), cx, cy,
                    CELL_W - 2 * CELL_PAD, CELL_H)

# appends the container and its objects to the file, the container as loc_buildcard's last child
container = max(p.starts) + 1
objects = images + texts
ids = list(range(container + 1, container + 1 + len(objects)))
start, end = p.range(PARENT)
last_child = max(i for i in range(start, end) if p.lines[i].startswith("  - "))
p.lines.insert(last_child + 1, f"  - {container}")
end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline
new = [f"- Id: {container}", "  Name: bc_mtraits", "  Children:", *[f"  - {i}" for i in ids], "  Active: true",
       "  Position: 0, 0, 0", "  Rotation: 0, 0, 0, 1", "  Scale: 1, 1, 1", "  Pivot: 0.5, 0.5", "  AnchorPoint: 0, 0",
       "  AnchorMin: 0.5, 0.5", "  AnchorMax: 0.5, 0.5", "  OffsetMin: -1712, -856", "  OffsetMax: 1712, 856",
       "  SizeDelta: 3424, 1712"]
for id_, (lines, *_) in zip(ids, objects):
    new += [f"- Id: {id_}", *lines]
p.lines[end:end] = new
p.reindex()
for id_, (_, x, y, w, h, pivot) in zip(ids, objects):
    p.place(id_, pos=card(x, y), size=(w * S, h * S))

# bc_text01: the perk summary, right-aligned at the heading's right
p.replace(SUMMARY, "FontSize: 48", "FontSize: 22")
start, end = p.range(SUMMARY)
i = max(j for j in range(start, end) if p.lines[j] == "      Enable: true") + 1
p.lines[i:i] = language_setter()
p.reindex()
p.replace(SUMMARY, "Alignment: 513", f"Alignment: {RIGHT}")
p.place(SUMMARY, pos=card(SECTION_W - 8, 10), size=(900 * S, 32 * S), pivot=(1, 1))

# CharaInfo.Powers on the root (object 0): append the texts' refs after the list's last entry
i = p.lines.index("      Powers:", *p.range(0)) + 1
while p.lines[i].startswith("      - ") or p.lines[i].startswith("        "):
    i += 1
first = ids[len(images)]
p.lines[i:i] = [line for id_ in ids[len(images):]
                for line in ("      - ComponentName: Text", "        Index: 0", f"        ObjectRefId: {id_}")]
p.save(sys.argv[2])
print(f"added ids {container}..{ids[-1]}, texts from {first}")
