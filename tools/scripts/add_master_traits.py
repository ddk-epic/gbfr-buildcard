# Usage: python add_master_traits.py <status01.prfb.yaml> <out.prfb.yaml>
# Lays out the master traits board in the blue section and adds refs to its texts to CharaInfo.Powers.
import sys
from prefab import Prefab
from card import CENTER, INK, LEFT, RIGHT, S, Group, add_powers, append, card, image, language_setter, text

p = Prefab(sys.argv[1])
SUMMARY = 462  # bc_text01

STYLES = [  # name, colour
    ("Insight", (0.6901961, 0.42352942, 0.9411765)),
    ("Essence", (0.29411766, 0.7019608, 0.9098039)),
    ("Crux", (0.9098039, 0.37254903, 0.41568628)),
]
SLOTS = [4, 8, 8, 10]  # cells per rank
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

group = Group()

def add(method, name, comps, x, y, w, h, pivot=(0, 1)):
    # x, y: sharecard pixels relative to the section's top-left corner
    method(name, comps, SECTION_X + x, SECTION_Y + y, w, h, pivot)

add(group.text, "bc_mt_heading", text("MASTER TRAITS", 30, INK, 1, LEFT), 8, 4, 600, 40)
for s, (style, color) in enumerate(STYLES):
    x0, y0 = s * (COLUMN_W + COLUMN_GAP), HEADING_H
    add(group.image, f"bc_mt_{s}_border", image(color, 1), x0, y0, COLUMN_W, BORDER)
    top = y0 + BORDER + PAD_TOP + STYLE_HEADING_H
    rows = [(n + 1) // 2 for n in SLOTS]
    heights = [RANK_LABEL_H + r * CELL_H + (r - 1) * CELL_GAP for r in rows]
    spare = (SECTION_H - PAD_BOTTOM - top - sum(heights)) / (len(SLOTS) - 1)
    rank_tops = [top + sum(heights[:r]) + r * spare for r in range(len(SLOTS))]
    for r, rank_top in enumerate(rank_tops):
        for c in range(SLOTS[r]):
            cx = x0 + PAD_X + (c % 2) * (CELL_W + CELL_GAP)
            cy = rank_top + RANK_LABEL_H + (c // 2) * (CELL_H + CELL_GAP)
            add(group.image, f"bc_mt_{s}_{r}_{c}_fill", image(INK, 0.07), cx, cy, CELL_W, CELL_H)

    add(group.text, f"bc_mt_{s}_title", text("", 26, INK, 1, CENTER), x0, y0 + BORDER + PAD_TOP + 12, COLUMN_W, 32)
    add(group.text, f"bc_mt_{s}_style", text(style.upper(), 15, color, 1, CENTER), x0, y0 + BORDER + PAD_TOP + 12 + 32 + 8,
        COLUMN_W, 20)
    for r, rank_top in enumerate(rank_tops):
        add(group.text, f"bc_mt_{s}_{r}_label", text(f"STYLE RANK {RANKS[r]}", 17, INK, 0.8, LEFT),
            x0 + PAD_X + 2, rank_top, 300, 28)
        add(group.text, f"bc_mt_{s}_{r}_count", text("", 19, INK, 1, RIGHT), x0 + COLUMN_W - PAD_X, rank_top, 200, 28,
            (1, 1))
    for r, rank_top in enumerate(rank_tops):
        for c in range(SLOTS[r]):
            cx = x0 + PAD_X + (c % 2) * (CELL_W + CELL_GAP) + CELL_PAD
            cy = rank_top + RANK_LABEL_H + (c // 2) * (CELL_H + CELL_GAP)
            for state, alpha in (("on", 1), ("off", 0.4)):
                add(group.text, f"bc_mt_{s}_{r}_{c}_{state}",
                    text("", 19, INK, alpha, LEFT, CELL_LINE_SPACING, CELL_LINE_SPACING_ENG), cx, cy,
                    CELL_W - 2 * CELL_PAD, CELL_H)

container, _, text_ids = append(p, "bc_mtraits", group)

# bc_text01: the perk summary
p.replace(SUMMARY, "FontSize: 48", "FontSize: 22")
start, end = p.range(SUMMARY)
i = max(j for j in range(start, end) if p.lines[j] == "      Enable: true") + 1
p.lines[i:i] = language_setter()
p.reindex()
p.replace(SUMMARY, "Alignment: 513", f"Alignment: {RIGHT}")
p.place(SUMMARY, pos=card(SECTION_X + SECTION_W - 8, SECTION_Y + 10), size=(900 * S, 32 * S), pivot=(1, 1))

add_powers(p, text_ids)
p.save(sys.argv[2])
print(f"added ids {container}..{text_ids[-1]}, texts from {text_ids[0]}")
