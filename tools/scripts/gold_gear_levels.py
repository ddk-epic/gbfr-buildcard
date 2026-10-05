# Usage: python gold_gear_levels.py <status01.prfb.yaml> <out.prfb.yaml>
# Colours the gear column's trait levels in the summon rows' gold.
import sys
from prefab import Prefab

p = Prefab(sys.argv[1])

WEAPON = 1705  # bc_weapon
LEVELS = ["lv01_text01", "lv01_num01", "text01_02", "text01_03"]  # trait rows, sigil rows
TOP = "1, 0.9411765, 0.88235295, 1"  # SkillInfo bonus level gradient
BOTTOM = "1, 0.6862745, 0.4509804, 1"
OUTLINE_FROM, OUTLINE_TO = "_ol06", "_ol07"

assert p.get(WEAPON, "Name") == "bc_weapon"

def descendants(id_):
    out = [id_]
    for c in p.children(id_):
        out += descendants(c)
    return out

texts = [i for i in descendants(WEAPON) if p.get(i, "Name") in LEVELS]
for i in texts:
    start, end = p.range(i)
    for j in range(start, end):
        line = p.lines[j]
        key = line.strip().split(":")[0]
        indent = line[: len(line) - len(line.lstrip())]
        if key in ("ColorTL", "ColorTR"):
            p.lines[j] = f"{indent}{key}: {TOP}"
        elif key in ("ColorBL", "ColorBR"):
            p.lines[j] = f"{indent}{key}: {BOTTOM}"
        elif key == "MaterialPath":
            p.lines[j] = line.replace(OUTLINE_FROM, OUTLINE_TO)
    # the first language container is the default outline
    first = next(j for j in range(start, end) if p.lines[j].strip() == "ContainerData:") + 1
    second = first + 1
    assert OUTLINE_FROM in p.lines[first] and OUTLINE_TO in p.lines[second]
    p.lines[first], p.lines[second] = p.lines[second], p.lines[first]

p.save(sys.argv[2])
print(f"{len(texts)} level texts: {texts}")
