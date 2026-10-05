# Usage: python add_weapon_type.py <status01.prfb.yaml> <equip01_info_weapon01.prfb.yaml> <out.prfb.yaml>
# Replaces the line under the weapon name with the equip screen's series line and its ornaments.
import sys
from prefab import Prefab, renumber
from card import copy_objects

p = Prefab(sys.argv[1])
source = Prefab(sys.argv[2])

WEAPON = 1705  # bc_weapon
NAME_LINE = 1814  # line02
TYPE, ORNAMENTS = 123, [124, 125]  # type_text01, main_image01_01/02
CELL_FONT = 19  # bc_mt_0_0_0_off
TYPE_FONT = 36
TYPE_GAP = 37  # the name's original centre to the series line's centre
NAME_RISE = 8

assert p.get(WEAPON, "Name") == "bc_weapon" and p.get(NAME_LINE, "Name") == "line02"
assert source.get(TYPE, "Name") == "type_text01"

blocks = {i: p.lines[slice(*p.range(i))] for i in p.starts}
children = {i: p.children(i) for i in p.starts}
name = children[NAME_LINE][0]
first = max(p.starts) + 1
ids = {old: first + n for n, old in enumerate([TYPE, *ORNAMENTS])}
copied = copy_objects(source, ids, ids)
for old, new in ids.items():
    start = copied.index(f"- Id: {new}")
    end = next((i for i in range(start + 1, len(copied)) if copied[i].startswith("- Id: ")), len(copied))
    blocks[new] = copied[start:end]
children[ids[TYPE]] = [ids[o] for o in ORNAMENTS]
children.update({ids[o]: [] for o in ORNAMENTS})
children[NAME_LINE] = children[NAME_LINE] + [ids[TYPE]]

# hides the line, keeps the name on it
block = blocks[NAME_LINE]
i = block.index("  - ComponentName: Image")
block[block.index("      Enable: true", i)] = "      Enable: false"

# WeaponInfo.TypeText
block = blocks[WEAPON]
i = block.index("      WeaponImageObj:")
block[i:i] = ["      TypeText:", "        ComponentName: Text", "        Index: 0", f"        ObjectRefId: {ids[TYPE]}"]

new = renumber(p, blocks, children)
type_text = new[ids[TYPE]]
name_y = p.vec(new[name], "Position")[1]
scale = round(CELL_FONT / (TYPE_FONT * p.vec(WEAPON, "Scale")[1]), 3)
p.set(type_text, "Scale", (scale, scale, 1))
p.place(type_text, pos=(0, name_y - TYPE_GAP))
p.place(new[name], pos=(p.vec(new[name], "Position")[0], name_y + NAME_RISE))

p.save(sys.argv[3])
print(f"type text {type_text} at scale {scale}, ornaments {[new[ids[o]] for o in ORNAMENTS]}, line {new[NAME_LINE]}")
