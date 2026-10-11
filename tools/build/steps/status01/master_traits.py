# Lays out the master traits board like the game's Master Traits menu, with a hidden captain board of 14 EX cells.
from model.components import CENTER, LEFT, RECT, RIGHT, image, mask, rect, single, text
from model.prefab import copy, f
from steps.layout import CARD_H, CARD_W, MASTER_TRAITS, OUTLINE, card, mt_clip, section

STYLES = 3
SLOTS, CAPTAIN_SLOTS = [4, 8, 8, 10], [4, 8, 8, 14]
COLUMNS = 2
RANK_TEXT_IDS = [f"TXT_PAU_SKL_BD_ST_RANK_{n}" for n in range(1, 5)]
INK = (0.19607843, 0.37254903, 0.4901961)

# the board, in card units from the section's top-left corner
HEADING_H = 61
COLUMN_GAP = 6
BORDER = 5
PAD_TOP, PAD_BOTTOM = 10, 22
STYLE_HEADING_H = 98
RANK_LABEL_H = 43
LABEL_H = 34  # a rank label's text
LABEL_W, COUNT_W = 367, 245  # a rank label's and its count's text
STYLE_TITLE_H = 39
CELL_H, CELL_GAP = 56, 5
CELL_TEXT_LEFT, CELL_TEXT_RIGHT = 12, 2
CELL_LINE_SPACING, CELL_LINE_SPACING_JPN = 4, -30

# skillboard_window01's Master Traits list
ATLAS = "atlas/pause_skillboard"
BASE = (ATLAS, "ps_sboard_list02")
TITLE_BARS = [(f"layouts/pause/skillboard/noatlastextures/{n}", n)
              for n in ("ps_sboard_ttl_base01", "ps_sboard_ttl_base02", "ps_sboard_ttl_base03", "ps_sboard_ttl_base04")]
PANEL_COLOR = (0.05490196, 0.11764706, 0.23137255), 0.5019608  # loc_base
CELL_COLOR = (0.15294118, 0.105882354, 0.20784314), 0.6  # base01
TITLE_TEXT_COLOR = (1, 1, 1), 0.69803923  # ttl_text01
ON_COLOR = (0.98039216, 0.98039216, 0.9411765), 1  # text01
OFF_COLOR = (0.6392157, 0.6784314, 0.6784314), 1  # text02
CELL_MATERIAL, CELL_LANGUAGE = "fonts/fot_skipstd_b_sdf_ds01", "data/language/ld_skipstd_b_sdf_ds01"
GAME_CELL_H, TITLE_H = 252, 74  # game units
PANEL_X, PANEL_TOP, PANEL_BOTTOM = 4.5, 6, 7  # card units, the panel around the rank's title and cells
CELLS_X = 8  # card units
BAR_INSET, BAR_H = 4, 38  # card units, the title bar's inset in the panel and its height
LABEL_LEFT, LABEL_RIGHT = 16, 12  # card units

PALETTES = ["blue", "original"]

# the picked cell style
PICKED = (107 / 255, 132 / 255, 155 / 255)  # a quarter of the way from ps_sboard_list01's colour to white
PICKED_ALPHA = 0.7
# per palette: picked colour, unpicked colour and alpha
CELL_STYLES = {
    "blue": ((0.0441, 0.2118, 0.3265), (0.0588, 0.3529, 0.5059), 0.6),  # gradient navy at 75 percent, gradient stop 2
    "original": ((0.1176, 0.1020, 0.2392), (0.1647, 0.1647, 0.3569), 0.6),  # unpicked: the stock title bars' indigo
}

# background04's scene
SCENE_W, SCENE_H = 3840, 2160
WHITE = "layouts/pause/status/noatlastextures/bc_white"
# sampled from the portrait backdrop
GRADIENT = [(0.0588, 0.2824, 0.4353), (0.0588, 0.3529, 0.5059), (0.0784, 0.4039, 0.5608),
            (0.0706, 0.5098, 0.6471), (0.0784, 0.5686, 0.6980), (0.1529, 0.6314, 0.7176)]
# per scene layer: gradient stop, brightness and alpha, None for the stock alpha
SCENE_TINTS = {
    "bg01": (0, 0.4, None), "bg02": (0, 1, None), "base01": (5, 1, None), "vignette01": (0, 1, 0.2),
    "circle01": (3, 1, None), "glow01": (5, 1, None), "glow03": (3, 1, None), "cloud01": (2, 1, None),
    "cloud02": (1, 1, None), "glow02": (4, 1, None), "ring01_01": (5, 1, None), "smoke04": (4, 1, None),
    "smoke03": (4, 1, None), "smoke02": (5, 1, None), "glow02_01": (5, 1, None), "glow02_02": (5, 1, None),
    "cloud03": (3, 1, None), "add01": (0, 1, 0.2),
}
SCENE_DESATURATE = 0.15
BAR_HIGHLIGHT = tuple(a + (b - a) * 0.5 for a, b in zip(GRADIENT[2], GRADIENT[3]))
BAR_RAMPS = [(False, 1), (False, 1), (True, 0.5), (True, 1)]  # per rank: mirrored, highlight strength
EX_DARK, EX_LIGHT = (0.19, 0.22, 0.32), (0.14, 0.375, 0.55)  # measured on screen
BAR_LEAK, BAR_ALPHA = (0.01, 0.02, 0.05), 0.8  # estimated on screen


def bar_ramp(r):
    # the silhouette's and the EX texture's colours of rank r's bar
    flip, strength = BAR_RAMPS[r]
    navy = GRADIENT[0]
    highlight = [n + (h - n) * strength for n, h in zip(navy, BAR_HIGHLIGHT)]
    tint = [0.1] + [max(0, (h - n) / (l - d)) for h, n, l, d in list(zip(highlight, navy, EX_LIGHT, EX_DARK))[1:]]
    base = [max(0, (n - t * d - k) / BAR_ALPHA) for n, t, d, k in zip(navy, tint, EX_DARK, BAR_LEAK)]
    return base, tint, flip


def scene_tint(stop, brightness):
    # the stop desaturated and scaled by brightness
    color = GRADIENT[stop]
    grey = 0.2126 * color[0] + 0.7152 * color[1] + 0.0722 * color[2]
    return [(c + (grey - c) * SCENE_DESATURATE) * brightness for c in color]

# var00_frame_header02, in header units
TITLE_TEXT_ID = "TXT_PAU_TTL_SKL_BD"
HEADER_H = 400
TITLE_Y = 128  # title_text01's centre below the header's top
HEADER_PAD = 80  # title_text01's top below the header's top
LINE_Y = 190  # line01's line below its top
LINE_CENTRE = 191  # line01's line centre below its top
TIP = 276  # ps_frame_line02's first column of plain line
LINE_FACTOR = 0.8
TITLE_SIZE, RANK_TITLE_SIZE = 96, 40  # title_text01, skillboard_window01's ttl_text01
STAR_SIZE, STAR_PAGE_SCALE, PAGE_TITLE_SIZE = 144, 0.5, 56  # a star, loc_level02's scale on the style page, its title
STYLE_TITLE_COLOR = "0.98039216, 0.98039216, 0.9411765, 1"  # info01_text01
STYLE_TITLE_GRADIENT = [("ColorTL", STYLE_TITLE_COLOR), ("ColorTR", STYLE_TITLE_COLOR),
                        ("ColorBL", "0.8627451, 0.9607843, 1, 1"), ("ColorBR", "0.8627451, 0.9607843, 1, 1")]
PERK_GAP, PERKS_GAP = 6, 24  # a name to its stars, a style's stars to the next name
MIDDLE_LEFT, MIDDLE_RIGHT = 3, 5  # ChildAlignment

LABEL_SIZE, COUNT_SIZE, CELL_SIZE, STYLE_TITLE_SIZE = 17, 19, 19, 26
BUDGET_SCALE = 0.8
BUDGET_DROP = 1  # card units
COUNT_LAYOUT = ["  - ComponentName: HorizontalLayoutGroup", "    Component:", "      Padding: 0, 0, 0, 0", "      Spacing: 0",
                f"      ChildAlignment: {MIDDLE_RIGHT}", "      ChildControlWidth: false", "      ChildControlHeight: false",
                "      ChildScaleWidth: false", "      ChildScaleHeight: false", "      ChildForceExpandWidth: false",
                "      ChildForceExpandHeight: false", "      Enable: true"]
COUNT_FIT = ["  - ComponentName: ContentSizeFitter", "    Component:", "      HorizontalFit: 2", "      VerticalFit: 0",
             "      Enable: true"]
SCALE = LABEL_SIZE / RANK_TITLE_SIZE  # the header's
PERK_NAME_SIZE = PAGE_TITLE_SIZE / STAR_PAGE_SCALE  # the style names' font size
ROW_SCALE = (STYLE_TITLE_SIZE + CELL_SIZE) / 2 / PERK_NAME_SIZE  # the style names' row

STARS = ("root/loc_base01/loc_info03/skillboard_info03/root/loc_item01/loc_info01/loc_info01_01/loc_level02/"
         "skillboard_level02/root/loc_level02")

LEFT_X, TOP_Y, WIDTH, HEIGHT = MASTER_TRAITS
COLUMN_W = (WIDTH - 2 * COLUMN_GAP) / 3
CELL_W = (COLUMN_W - 2 * (PANEL_X + CELLS_X) - CELL_GAP) / 2
CELL_TEXT_W = CELL_W - CELL_TEXT_LEFT - CELL_TEXT_RIGHT
CELL_K = CELL_H / GAME_CELL_H  # the cell images' scale


def column_x(s):
    return LEFT_X + s * (COLUMN_W + COLUMN_GAP)


def rank_tops():
    # each rank's top, in card units, the ranks spread down the column
    top = TOP_Y + HEADING_H + BORDER + PAD_TOP + STYLE_HEADING_H
    rows = [(n + 1) // COLUMNS for n in SLOTS]
    heights = [RANK_LABEL_H + r * CELL_H + (r - 1) * CELL_GAP for r in rows]
    spare = (TOP_Y + HEIGHT - PAD_BOTTOM - top - sum(heights)) / (len(SLOTS) - 1)
    return [top + sum(heights[:r]) + r * spare for r in range(len(SLOTS))]


def stock_ranks():
    # per rank: panel top and bottom, label centre, and the rows' tops and bottoms, in card units
    ranks = []
    for r, rank_top in enumerate(rank_tops()):
        rows = []
        for row in range(SLOTS[r] // COLUMNS):
            row_top = card(0, rank_top + RANK_LABEL_H + row * (CELL_H + CELL_GAP))[1]
            rows.append((row_top, row_top - CELL_H))
        ranks.append(dict(top=card(0, rank_top - PANEL_TOP)[1], bottom=rows[-1][1] - PANEL_BOTTOM,
                          label=card(0, rank_top + LABEL_H / 2)[1], rows=rows))
    return ranks


def captain_ranks(stock):
    # the stock layout with two more EX rows, everything but the rank labels shrunk by one factor
    label_h, pad_top, gap = LABEL_H, PANEL_TOP, RANK_LABEL_H - LABEL_H
    cell, row_gap, pad_bottom = CELL_H, CELL_GAP, PANEL_BOTTOM
    top, bottom = stock[0]["top"], stock[-1]["bottom"]
    fixed = len(SLOTS) * label_h
    extra = sum((CAPTAIN_SLOTS[r] - SLOTS[r]) // COLUMNS * (cell + row_gap) for r in range(len(SLOTS)))
    k = (top - bottom - fixed) / (top - bottom - fixed + extra)
    ranks, y = [], top
    for r in range(len(SLOTS)):
        panel_top = y
        y -= pad_top * k
        label = y - label_h / 2
        y -= label_h + gap * k
        rows = []
        for row in range(CAPTAIN_SLOTS[r] // COLUMNS):
            if row:
                y -= row_gap * k
            rows.append((y, y - cell * k))
            y -= cell * k
        y -= pad_bottom * k
        ranks.append(dict(top=panel_top, bottom=y, label=label, rows=rows))
        if r + 1 < len(SLOTS):
            y -= (stock[r]["bottom"] - stock[r + 1]["top"]) * k
    return ranks


def sliced_image(name, color, sprite, centre, size, scale):
    node = rect(name, image(*color, sprite, sliced=True, fill_center=True))
    node.set("Scale", (scale, scale, 1))
    return node, lambda: node.place(pos=centre, size=(size[0] / scale, size[1] / scale))


def set_text(node, color, cell=False):
    node.component("Text").set("Color", f"{', '.join(f(c) for c in color[0])}, {f(color[1])}")
    if cell:
        node.component("Text").set("MaterialPath", CELL_MATERIAL)
        node.component("LanguageSetter").set("LanguageData", CELL_LANGUAGE)


def count_row(s, r):
    # the rank's picked count and its smaller budget in a right-aligned row
    row = rect(f"bc_mt_{s}_{r}_count", COUNT_LAYOUT, (1, 1))
    for part, size, drop in (("spent", COUNT_SIZE, 0), ("budget", COUNT_SIZE * BUDGET_SCALE, BUDGET_DROP)):
        node = row.add(rect(f"bc_mt_{s}_{r}_{part}", text("", size, INK, 1, RIGHT) + COUNT_FIT, (0, 1)))
        set_text(node, TITLE_TEXT_COLOR)
        node.component("Text").set("Margin", f"0, {f(2 * drop)}, 0, 0")
        node.set("AnchorMin", "0, 1")
        node.set("AnchorMax", "0, 1")
        node.set("SizeDelta", f"0, {LABEL_H}")
    return row


def title_bar(palette, s, r, centre, size):
    # the rank's title bar layers in the palette
    scale = BAR_H / TITLE_H
    bar, at = sliced_image(f"bc_mt_{s}_{r}_ttl", ((1, 1, 1), 1), TITLE_BARS[r], centre, size, scale)
    if palette == "original":
        return [(bar, at)]
    base, tint, flip = bar_ramp(r)
    bar_image = bar.component("Image")
    bar_image.set("Color", f"{', '.join(f(c) for c in base)}, 1")
    bar_image.insert(["      MaterialPath: material/uialphaa"], after="Type")
    light, light_at = sliced_image(f"bc_mt_{s}_{r}_ttl_light", (tint, 1), TITLE_BARS[3], centre, size, scale)
    light.component("Image").insert(["      MaterialPath: material/uiadd"], after="Type")
    if flip:
        light.set("Scale", (-scale, scale, 1))
    return [(bar, at), (light, light_at)]


def grid(board, name, slots, ranks, active):
    # the rank panels, texts and each palette's title bars and cells
    container = board.add(rect(name, active=active))
    container.place(pos=(0, 0), size=(CARD_W, CARD_H))
    panels, texts, placements = [], [], []
    images = {palette: [] for palette in PALETTES}
    cells = {palette: [] for palette in PALETTES}
    for s in range(STYLES):
        x0 = column_x(s)
        left, right = card(x0 + PANEL_X, 0)[0], card(x0 + COLUMN_W - PANEL_X, 0)[0]
        for r, rank in enumerate(ranks):
            panel, at = sliced_image(f"bc_mt_{s}_{r}_base", PANEL_COLOR, BASE, ((left + right) / 2,
                                     (rank["top"] + rank["bottom"]) / 2), (right - left, rank["top"] - rank["bottom"]),
                                     CELL_K)
            panels.append(panel)
            placements.append(at)
            for palette in PALETTES:
                for bar, at in title_bar(palette, s, r, ((left + right) / 2, rank["label"]),
                                         (right - left - 2 * BAR_INSET, BAR_H)):
                    images[palette].append(bar)
                    placements.append(at)

        for r, rank in enumerate(ranks):
            label = rect(f"bc_mt_{s}_{r}_label", text("", LABEL_SIZE, INK, 0.8, LEFT), (0, 1))
            set_text(label, TITLE_TEXT_COLOR)
            label.add_component(["  - ComponentName: TextSetter", "    Component:", f"      TextID: {RANK_TEXT_IDS[r]}",
                                 "      Enable: true"])
            count = count_row(s, r)
            label_top = rank["label"] + LABEL_H / 2
            placements.append(lambda n=label, x=left + BAR_INSET + LABEL_LEFT, y=label_top:
                              n.place(pos=(x, y), size=(LABEL_W, LABEL_H)))
            placements.append(lambda n=count, x=right - BAR_INSET - LABEL_RIGHT, y=label_top:
                              n.place(pos=(x, y), size=(COUNT_W, LABEL_H)))
            texts += [label, count]

        for r, rank in enumerate(ranks):
            for c in range(slots[r]):
                cell_x = x0 + PANEL_X + CELLS_X + (c % COLUMNS) * (CELL_W + CELL_GAP)
                row_top, row_bottom = rank["rows"][c // COLUMNS]
                for palette in PALETTES:
                    base, at = sliced_image(f"bc_mt_{s}_{r}_{c}_base", CELL_COLOR, BASE,
                                            (card(cell_x + CELL_W / 2, 0)[0], (row_top + row_bottom) / 2),
                                            (CELL_W, row_top - row_bottom), CELL_K)
                    cells[palette].append(((s, r, c), base))
                    placements.append(at)
                for state, alpha, color in (("on", 1, ON_COLOR), ("off", 0.4, OFF_COLOR)):
                    node = rect(f"bc_mt_{s}_{r}_{c}_{state}",
                                text("", CELL_SIZE, INK, alpha, LEFT, CELL_LINE_SPACING, CELL_LINE_SPACING_JPN), (0, 1))
                    set_text(node, color, cell=True)
                    x = card(cell_x + CELL_TEXT_LEFT, 0)[0]
                    placements.append(lambda n=node, x=x, y=row_top, w=CELL_TEXT_W, h=row_top - row_bottom:
                                      n.place(pos=(x, y), size=(w, h)))
                    texts.append(node)

    for node in panels:
        container.add(node)
    groups = {}
    for palette in PALETTES:
        # each palette's bars and cells, only the first active
        group = container.add(rect(f"{name}_{palette}", active=palette == PALETTES[0]))
        for key in RECT:
            group.set(key, container.get(key))
        for node in images[palette] + [base for _, base in cells[palette]]:
            group.add(node)
        groups[palette] = group
    for node in texts:
        container.add(node)
    for at in placements:
        at()
    for palette in PALETTES:
        for key, base in sorted(cells[palette], key=lambda item: item[0]):
            groups[palette].add(picked_cell(groups[palette], key, base, CELL_STYLES[palette]))
    return container, groups


def picked_cell(container, key, base, style):
    # a hidden copy of the cell's base under a picked outline
    picked_color, unpicked_color, unpicked_alpha = style
    s, r, c = key
    group = rect(f"bc_mt_{s}_{r}_{c}_picked", active=False)
    for key in RECT:
        group.set(key, container.get(key))
    fill = single(base)
    fill.name = f"bc_mt_{s}_{r}_{c}_picked_base"
    fill.component("Image").set("Color", f"{', '.join(f(v) for v in picked_color)}, {f(PICKED_ALPHA)}")
    outline = single(base)
    outline.name = f"bc_mt_{s}_{r}_{c}_picked_outline"
    outline_image = outline.component("Image")
    outline_image.set("Color", f"{', '.join(f(v) for v in PICKED)}, 1")
    outline_image.set("TexturePath", OUTLINE)
    outline_image.set("SpriteName", OUTLINE.rsplit("/", 1)[1])
    outline_image.set("FillCenter", False)
    base.component("Image").set("Color", f"{', '.join(f(v) for v in unpicked_color)}, {f(unpicked_alpha)}")
    group.add(fill)
    group.add(outline)
    w, h = base.vec("SizeDelta")
    outline.set("Scale", (1, 1, 1))
    outline.place(size=(w * CELL_K, h * CELL_K))
    return group


def layout_group(name, stars, spacing, alignment, pivot):
    # an object with loc_level02's HorizontalLayoutGroup and ContentSizeFitter
    node = rect(name, stars.component_lines(), pivot)
    layout = node.component("HorizontalLayoutGroup")
    layout.set("Spacing", f(spacing / ROW_SCALE))
    layout.set("ChildAlignment", alignment)
    return node


def top_left(node):
    # anchored like a layout group's children, a star high
    node.set("AnchorMin", "0, 1")
    node.set("AnchorMax", "0, 1")
    node.set("SizeDelta", f"0, {STAR_SIZE}")
    return node


def perks(header_title, stars):
    # each style's name and stars in a row
    row = layout_group("bc_mt_perks", stars, PERKS_GAP, MIDDLE_RIGHT, (1, 0.5))
    names = []
    for s in range(STYLES):
        group = row.add(top_left(layout_group(f"bc_mt_perk_{s}", stars, PERK_GAP, MIDDLE_LEFT, (0, 0.5))))
        name = group.add(top_left(rect(f"bc_mt_perk_{s}_name", header_title.component_lines(), (0, 0.5))))
        name.drop_component("TextSetter")
        name_text = name.component("Text")
        name_text.set("Text", "''")
        name_text.set("FontSize", f(PERK_NAME_SIZE))
        name_text.set("Alignment", LEFT)
        names.append(name)
        star_row = group.add(copy(stars))
        for glow in [n for n in star_row.walk() if n.name.startswith("glow0")]:
            glow.remove()
        star_row.name = f"bc_mt_perk_{s}_stars"
        star_row.set("Scale", (1, 1, 1))
        star_row.set("AnchorMin", "0, 1")
        star_row.set("AnchorMax", "0, 1")
    return row, names


def apply(ctx):
    prefab = ctx.prefab("status01")
    header = ctx.stock("var00_frame_header02")
    header_line, header_title = header.find("line01"), header.find("title_text01")
    stars = ctx.stock("skillboard_window01").at(STARS)
    scene_source = ctx.stock("background04").find("loc_bg")

    board = section(prefab.find("loc_buildcard"), "bc_mtraits")

    # the Master Traits menu's background, without the character, covering the section and clipped to it
    clip = board.add(mt_clip("bc_mt_bg"))
    scenes = []
    for palette in PALETTES:
        scene = clip.add(copy(scene_source))
        scene.name = f"bc_mt_bg_{palette}"
        scene.set("Active", palette == PALETTES[0])
        scene.find("loc_chara01").remove()
        scenes.append(scene)
    # the blue scene's layers tinted, plain textured layers greyed
    for node in scenes[0].walk():
        if "Image" not in node.components():
            continue
        if node.name not in SCENE_TINTS:
            raise KeyError(f"{node.path}: no scene tint")
        image_component = node.component("Image")
        stop, brightness, alpha = SCENE_TINTS[node.name]
        if alpha is None:
            alpha = float(image_component.get("Color").rsplit(", ", 1)[1])
        image_component.set("Color", f"{', '.join(f(c) for c in scene_tint(stop, brightness))}, {f(alpha)}")
        if image_component.has("TexturePath") and not image_component.has("MaterialPath"):
            image_component.insert(["      MaterialPath: material/uimonochrome"], after="Type")
    k = max(WIDTH / SCENE_W, HEIGHT / SCENE_H)
    clip.set("Scale", (k, k, 1))
    clip.place(pos=card(LEFT_X + WIDTH / 2, TOP_Y + HEIGHT / 2), size=(WIDTH / k, HEIGHT / k))

    # the header's line from the section's left edge, its ornament clipped off
    left, top = card(LEFT_X, TOP_Y)
    right = card(LEFT_X + WIDTH, 0)[0]
    line_y = top - LINE_CENTRE * SCALE * LINE_FACTOR
    line_clip = board.add(rect("bc_mt_line_clip", mask((WHITE, WHITE.rsplit("/", 1)[1]))))
    clip_x = (left + right) / 2
    line_clip.place(pos=(clip_x, line_y), size=(right - left, HEADER_H * SCALE))
    line = line_clip.add(copy(header_line))
    line.name = "bc_mt_line"
    line.set("Scale", (SCALE, SCALE, 1))
    line_left = left - TIP * SCALE
    line.place(pos=(line_left - clip_x, LINE_CENTRE * SCALE), size=((right - line_left) / SCALE, HEADER_H))
    dy = line_y - (top - LINE_CENTRE * SCALE)

    stock = stock_ranks()
    panel_top = stock[0]["top"]
    old_line_y = top - LINE_Y * SCALE

    # the style names and stars padded from the right, between the title's height and the line
    perk_row, perk_names = perks(header_title, stars)
    board.add(perk_row)
    perk_row.set("Scale", (ROW_SCALE, ROW_SCALE, 1))
    bottom = top - TITLE_Y * SCALE - STAR_SIZE * ROW_SCALE / 2
    perk_row.place(pos=(right - HEADER_PAD * SCALE, (bottom + old_line_y) / 2 + dy), size=(0, STAR_SIZE), pivot=(1, 0))

    # the "Master Traits" title
    heading = board.add(rect("bc_mt_heading", header_title.component_lines(), (0, 0.5)))
    heading.component("Text").set("FontSize", f(TITLE_SIZE * SCALE))
    heading.component("TextSetter").set("TextID", TITLE_TEXT_ID)
    heading.place(pos=(left + HEADER_PAD * SCALE, top - TITLE_Y * SCALE + dy), size=(WIDTH / 2, TITLE_SIZE * SCALE))

    # the style titles in info01_text01's style, between the line and the rank panels
    titles = []
    for s in range(STYLES):
        title = board.add(rect(f"bc_mt_{s}_title", text("", STYLE_TITLE_SIZE, INK, 1, CENTER), (0, 1)))
        title_text = title.component("Text")
        title_text.set("MaterialPath", CELL_MATERIAL)
        title.component("LanguageSetter").set("LanguageData", CELL_LANGUAGE)
        title_text.set("Color", STYLE_TITLE_COLOR)
        title_text.set("IsGradient", True)
        title_text.set("ColorMode", 2)
        for key, value in STYLE_TITLE_GRADIENT:
            title_text.set(key, value)
        title.place(pos=(card(column_x(s), 0)[0], (old_line_y + panel_top) / 2 + STYLE_TITLE_H / 2), size=(COLUMN_W, STYLE_TITLE_H))
        titles.append(title)

    cells, cell_groups = grid(board, "bc_mt_cells", SLOTS, stock, True)
    captain, captain_groups = grid(board, "bc_mt_cells_captain", CAPTAIN_SLOTS, captain_ranks(stock), False)

    perk_rows = [perk_row.child(f"bc_mt_perk_{s}") for s in range(STYLES)]
    ctx.export("PerkNames", perk_names)
    ctx.export("PerkStars", [[row.find(f"base0{k + 1}/icon0{k + 1}_add") for k in range(3)] for row in perk_rows])
    ctx.export("StyleTitles", titles)
    ctx.export("Cells", cells)
    ctx.export("CaptainCells", captain)
    ctx.export("Scenes", scenes)
    ctx.export("Palettes", [[cell_groups[palette], captain_groups[palette]] for palette in PALETTES])
    boards = [(cells, SLOTS), (captain, CAPTAIN_SLOTS)]

    def cell_nodes(suffix, containers=(cells, captain)):
        return [[[[container.child(f"bc_mt_{s}_{r}_{c}_{suffix}") for c in range(slots[r])] for r in range(len(slots))]
                 for s in range(STYLES)] for container, slots in zip(containers, (SLOTS, CAPTAIN_SLOTS))]

    def palette_cell_nodes(suffix):
        return [cell_nodes(suffix, (cell_groups[palette], captain_groups[palette])) for palette in PALETTES]
    def count_nodes(part):
        return [[[container.child(f"bc_mt_{s}_{r}_count").child(f"bc_mt_{s}_{r}_{part}") for r in range(len(SLOTS))]
                 for s in range(STYLES)] for container, _ in boards]
    ctx.export("RankCounts", count_nodes("spent"))
    ctx.export("RankBudgets", count_nodes("budget"))
    ctx.export("CellPicked", palette_cell_nodes("picked"))
    ctx.export("CellBases", palette_cell_nodes("base"))
    ctx.export("CellOn", cell_nodes("on"))
    ctx.export("CellOff", cell_nodes("off"))
    ctx.export("CellTextWidth", int(CELL_TEXT_W))
