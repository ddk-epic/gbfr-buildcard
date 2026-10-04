# Usage: python add_summons.py <status01.prfb.yaml> <summon_list01.prfb.yaml> <summon_info01.prfb.yaml> <out.prfb.yaml>
# Copies summon_list01's first summon slot (object 90: frame, icon, names, element badge and SummonInfo) four times
# into a 2x2 grid in the orange section. Under each slot it copies summon_info01's trait row (object 20, a SkillInfo)
# and equip bonus row (object 43, a LimitBonusInfo) as children of the slot, and points the slot's SummonInfo at them,
# so that setting the summon also fills both rows. CharaInfo.Powers references the slot objects.
import sys
from prefab import Prefab
from card import PARENT, S, add_powers, card, copy_objects

p = Prefab(sys.argv[1])
slot_source = Prefab(sys.argv[2])
row_source = Prefab(sys.argv[3])

SLOT = 90  # summon_list_button01_01_btn01
KEEP = [90, 91, 92, 93, 94, 103, 104, 105, 106, 108, 109, 110, 111, 112, 113, 114]
SLOT_COMPONENTS = ["SummonInfo"]
TRAIT_ROW = 20  # list_skill_p05_01
BONUS_ROW = 43  # list_skill_p05_02
ROW_OBJECTS = 20  # objects per row subtree
ROW_W, ROW_H = 1136, 140  # base01
LINE_H = 72  # loc_list01
LINE_GAP = 6

# sharecard pixels, the orange section
SECTION_X, SECTION_Y, SECTION_W, SECTION_H = 1876.33, 1142, 987.67, 262
COLUMN_GAP = 10
SCALE = (SECTION_W - COLUMN_GAP) / 2 * S / ROW_W
SLOT_W, SLOT_H = ROW_W * SCALE / S, ROW_H * SCALE / S  # sharecard pixels
PAD_TOP = 4

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

def slot(first_id, name):
    # the KEEP objects, then both rows, with ids from first_id; returns the lines and the slot's and rows' ids
    ids = {old: first_id + i for i, old in enumerate(KEEP)}
    trait = first_id + len(KEEP)
    bonus = trait + ROW_OBJECTS
    trait_ids = {TRAIT_ROW + i: trait + i for i in range(ROW_OBJECTS)}
    bonus_ids = {BONUS_ROW + i: bonus + i for i in range(ROW_OBJECTS)}

    def edit_slot(old, block):
        if old != SLOT:
            return block
        block = keep_components(block, SLOT_COMPONENTS)
        block[1] = f"  Name: {name}"
        i = block.index("  Children:") + 1
        while block[i].startswith("  - "):
            i += 1
        block[i:i] = [f"  - {old_id}" for old_id in (TRAIT_ROW, BONUS_ROW)]  # remapped by copy_objects
        # in the order the tool writes SummonInfo's fields
        i = block.index("      Elements:")
        block[i:i] = ["      _5D33A08E:", "        ComponentName: SkillInfo", "        Index: 0",
                      f"        ObjectRefId: {TRAIT_ROW}"]
        i = block.index("      Enable: true")
        block[i:i] = ["      F58112CE:", "        ComponentName: LimitBonusInfo", "        Index: 0",
                      f"        ObjectRefId: {BONUS_ROW}"]
        return block

    lines = copy_objects(slot_source, KEEP, ids | {TRAIT_ROW: trait, BONUS_ROW: bonus}, edit_slot)
    lines += copy_objects(row_source, list(trait_ids), trait_ids)
    lines += copy_objects(row_source, list(bonus_ids), bonus_ids)
    return lines, ids[SLOT], trait, bonus

container = max(p.starts) + 1
start, end = p.range(PARENT)
last_child = max(i for i in range(start, end) if p.lines[i].startswith("  - "))
p.lines.insert(last_child + 1, f"  - {container}")
end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline

slots, new = [], []
next_id = container + 1
for i in range(4):
    lines, slot_id, trait, bonus = slot(next_id, f"bc_smn_{i}")
    slots.append((slot_id, trait, bonus))
    new += lines
    next_id = bonus + ROW_OBJECTS
new = [f"- Id: {container}", "  Name: bc_smn", "  Children:", *[f"  - {s[0]}" for s in slots], "  Active: true",
       "  Position: 0, 0, 0", "  Rotation: 0, 0, 0, 1", "  Scale: 1, 1, 1", "  Pivot: 0.5, 0.5", "  AnchorPoint: 0, 0",
       "  AnchorMin: 0.5, 0.5", "  AnchorMax: 0.5, 0.5", "  OffsetMin: -1712, -856", "  OffsetMax: 1712, 856",
       "  SizeDelta: 3424, 1712"] + new
p.lines[end:end] = new
p.reindex()

# slots at the top of the section's quarters, centred horizontally; rows centred below the frame in slot coordinates
for i, (slot_id, trait, bonus) in enumerate(slots):
    x = SECTION_X + SECTION_W * (1 + 2 * (i % 2)) / 4
    top = SECTION_Y + SECTION_H / 2 * (i // 2) + PAD_TOP
    p.set(slot_id, "Scale", (SCALE, SCALE, 1))
    p.place(slot_id, pos=card(x, top + SLOT_H / 2))
    p.place(trait, pos=(0, -(ROW_H / 2 + LINE_GAP + LINE_H / 2)))
    p.place(bonus, pos=(0, -(ROW_H / 2 + LINE_GAP + LINE_H * 3 / 2)))

add_powers(p, [s[0] for s in slots], component="")
p.save(sys.argv[4])
print(f"added ids {container}..{next_id - 1}, slots {[s[0] for s in slots]}, scale {SCALE:.4f}, "
      f"height {(SLOT_H + (LINE_GAP + 2 * LINE_H) * SCALE / S + PAD_TOP):.1f} of {SECTION_H / 2}")
