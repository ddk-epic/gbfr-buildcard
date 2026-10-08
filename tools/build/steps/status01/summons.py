# Lays out the summons section as sharecard's summon cells on a status-style panel.
from model.components import mask, rect, set_line, single
from model.prefab import Ref, copy, f
from steps.layout import PANEL_SCALE, SCREEN_CARD_RATIO, SUMMONS, card, rounded_panel, section

SLOTS = 4
COLUMN_GAP = 10  # sharecard pixels
PAD_TOP = 4  # sharecard pixels
KEEP = ["loc_text01"]  # loc_item01's children kept
ICON_W, ICON_H = 320, 440  # cmn_icsmn sprites
FADE = ("atlas/pause_pause_common", "ps_cmn_mask_list_w01")
FADE_PADDING = 243.07613 / 1152  # the fade sprite's transparent left padding, of its width
BAND_FADE = ("atlas/pause_pause_common", "ps_cmn_list02_mask")
BAND_FADE_PADDING = 390.0761 / 1112  # the band fade sprite's transparent right padding, of its width
NAME_COLOR = "0.98039216, 0.98039216, 0.9411765, 1"  # online_text01
NAME_MATERIAL, NAME_LANGUAGE = "fonts/fot_skipstd_b_sdf_ol09", "data/language/ld_skipstd_b_sdf_ol09"
STATUS_SIZE = 40  # the status panel labels' and skill names' font size
LEVEL_H = 56  # twice the level badge's height
LEVEL_GAP = 8  # the trait name to its level
LOWER_LEFT = 6  # ChildAlignment

# slot units
CELL_W, SLOT_H = 1136, 140  # base01
BAND_X, BAND_Y, BAND_W, BAND_H = 33, 17, 735, 60  # from the cell's top-left corner
NAME_X = 24  # from the band's left edge
ROW_ICON_LEFT = -478  # the row icon's left edge in its row
TRAIT_Y, BONUS_Y = 144, 230  # row centres below the cell's top
ART_W = CELL_W / 3
PORTRAIT_W = 1.2  # of the art's width

LAYOUT = ["  - ComponentName: HorizontalLayoutGroup", "    Component:", "      Padding: 0, 0, 0, 0", "      Spacing: 0",
          f"      ChildAlignment: {LOWER_LEFT}", "      ChildControlWidth: false", "      ChildControlHeight: false",
          "      ChildScaleWidth: false", "      ChildScaleHeight: false", "      ChildForceExpandWidth: false",
          "      ChildForceExpandHeight: false", "      Enable: true", "  - ComponentName: ContentSizeFitter",
          "    Component:", "      HorizontalFit: 2", "      VerticalFit: 0", "      Enable: true"]


def summon_info(trait, icon, name, bonus):
    # SummonInfo's fields in the order the tool writes them
    def ref(node):
        return Ref("        ObjectRefId: ", node)
    return ["  - ComponentName: SummonInfo", "    Component:",
            "      _5D33A08E:", "        ComponentName: SkillInfo", "        Index: 0", ref(trait),
            "      _57A2478C:", "      - ComponentName: SummonIconSetter", "        Index: 0", ref(icon),
            "      Names:", "      - ComponentName: Text", "        Index: 0", ref(name),
            "      F58112CE:", "        ComponentName: LimitBonusInfo", "        Index: 0", ref(bonus),
            "      Enable: true"]


def slot(sources, name, scale):
    # a cell: the slot's name on its band, the icon art and both rows
    source_slot, source_band, source_icon, source_trait, source_bonus = sources
    node = copy(source_slot)
    node.name = name
    item = node.child("root").child("loc_list01").child("loc_item01")
    for child in list(item.children):
        if child.name not in KEEP:
            child.remove()
    trait, bonus = node.add(copy(source_trait)), node.add(copy(source_bonus))

    band = single(source_band)
    band.name = "band"
    band.set("Active", True)
    band.set("Scale", (-1, 1, 1))
    icon = single(source_icon)
    band_fade = rect("bc_smn_band", mask(BAND_FADE))
    band_fade.add(band)
    fade = rect("bc_smn_art", mask(FADE))
    fade.add(icon)
    item.add(fade, 0)
    item.add(band_fade, 1)

    text = item.find("loc_text01/text01_01")
    start = node.lines.index("  Components:") + 1
    end = node.lines.index("  Active: true")
    node.lines[start:end] = summon_info(trait, icon, text, bonus)

    # the trait level after the trait name, its top on the row's centre
    level = trait.find("loc_skill_lv01")
    wrap = rect("bc_smn_level", LAYOUT)
    wrap.add(level)
    row_text = trait.find("loc_text")
    row_text.add(wrap)
    set_line(row_text, "Spacing", LEVEL_GAP)
    wrap.place(size=(0, LEVEL_H))

    left, top = -CELL_W / 2, 70 + PAD_TOP * SCREEN_CARD_RATIO / scale
    cell_h = 131 * SCREEN_CARD_RATIO / scale
    portrait_y = 18 * SCREEN_CARD_RATIO / scale  # below the cell's centre

    # the band fade's ramp across the band
    band_fade_w = BAND_W / (1 - BAND_FADE_PADDING)
    band_fade.place(pos=(left + BAND_X + band_fade_w / 2, top - BAND_Y - BAND_H / 2), size=(band_fade_w, BAND_H))
    band.place(pos=(BAND_W / 2 - band_fade_w / 2, 0), size=(BAND_W, BAND_H))
    set_line(text, "MaterialPath", NAME_MATERIAL)
    set_line(text, "FontSize", f(STATUS_SIZE * PANEL_SCALE / scale))
    set_line(text, "Color", NAME_COLOR)
    i = text.lines.index("      MultiData: true")
    text.lines[i:i + 1] = ["      MultiData: false", f"      LanguageData: {NAME_LANGUAGE}"]
    text.place(pos=(left + BAND_X + NAME_X, top - BAND_Y - BAND_H / 2))

    # the fade's opaque part over the last third, the icon centred on it
    fade_w = ART_W / (1 - FADE_PADDING)
    fade.place(pos=(-left - fade_w / 2, top - cell_h / 2), size=(fade_w, cell_h))
    w = ART_W * PORTRAIT_W
    icon.place(pos=(fade_w / 2 - ART_W / 2, -portrait_y), size=(w, w * ICON_H / ICON_W))

    for row, y, k in ((trait, TRAIT_Y, PANEL_SCALE / scale), (bonus, BONUS_Y, 1)):
        # the trait row at the status panel labels' size; the rows' icons at the band's left edge
        x = left + BAND_X - ROW_ICON_LEFT * k
        row.set("Scale", (k, k, 1))
        row.place(pos=(x, top - y))
        row.find("line01").set("Active", False)
        # the level's padding and spacing as in the gear trait rows
        row_level = row.find("loc_skill_lv01")
        row_level.replace("Padding: 28, 0, 34, 0", "Padding: 28, 0, 40, 4")
        row_level.find("loc_lv01").replace("Spacing: 24", "Spacing: 8")
        if row is bonus:
            row_level.place(pos=((-left - ART_W - x) / k, row_level.vec("Position")[1]))
    return node


def apply(ctx):
    summon_list = ctx.stock("summon_list01")
    summon_info01 = ctx.stock("summon_info01")
    source_slot = summon_list.find("loc_list_control01/summon_list_button01_01_btn01")
    sources = (source_slot, summon_list.find("summon_list_button01_02_btn01/place01_set01"), source_slot.find("summon_icon01/icon_base01/icon01"),
               summon_info01.find("loc_info02/list_skill_p05_01"), summon_info01.find("loc_info02/list_skill_p05_02"))

    left, top, width, height = SUMMONS
    scale = (width - COLUMN_GAP) / 2 * SCREEN_CARD_RATIO / CELL_W
    slot_h = SLOT_H * scale / SCREEN_CARD_RATIO  # sharecard pixels

    summons = section(ctx.prefab("status01").find("loc_buildcard"), "bc_smn")
    panel = summons.add(rect("bc_smn_base"))
    panel.place(pos=card(left + width / 2, top + height / 2), size=(width * SCREEN_CARD_RATIO, height * SCREEN_CARD_RATIO))
    rounded_panel(panel, "bc_smn_panel", 1)

    # slots at the top of the section's quarters
    slots = []
    for i in range(SLOTS):
        node = summons.add(slot(sources, f"bc_smn_{i}", scale))
        x = left + width * (1 + 2 * (i % 2)) / 4
        y = top + height / 2 * (i // 2) + PAD_TOP
        node.set("Scale", (scale, scale, 1))
        node.place(pos=card(x, y + slot_h / 2))
        ctx.powers(node)
        slots.append(node)
    ctx.export("SummonSlots", slots)
