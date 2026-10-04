# Usage: python stack_gear.py <status01.prfb.yaml> <equip01_info01.prfb.yaml> <equip01_info_weapon01.prfb.yaml> <equip01_info02.prfb.yaml> <out.prfb.yaml>
# Replaces the weapon panel with the gear screen's Weapon section, the equip screen's trait rows and the gear screen's Sigils section, stacked in the cyan section.
import sys
from prefab import Prefab
from card import PARENT, S, card, copy_objects, keep_components

p = Prefab(sys.argv[1])
weapon = Prefab(sys.argv[2])
traits = Prefab(sys.argv[3])
sigils = Prefab(sys.argv[4])

PANEL = 1123  # bc_weapon
WEAPON_OBJECTS = 274
ROOT = 1  # root
MAX_LEVEL, EXP = 125, 155  # loc_max01, loc_exp01
SKILLS, PENDULUM = 278, 364  # loc_skill01, loc_skill02 in equip01_info_weapon01
TRAIT_OBJECTS = range(SKILLS, 421)
TRAIT_IMAGE = 14  # image_equip_00 in equip01_info_weapon01
WEAPON_IMAGE = 6  # image_equip_00 in equip01_info01
FIELDS = ["Skills", "PendulumSkillObj", "PendulumSkills", "PendulumNames"]
TITLE, GENE = 3, 5  # ttl01, loc_gene01 in equip01_info02
SIGIL_OBJECTS = [3, 4, *range(GENE, 163)]
SIGIL_ROWS = [7, 20, 33, 46, 59, 72, 85, 98, 111, 124, 137, 150]  # equip01_p01_01 to 12
OLD_ROWS = [143, 154, 165, 176, 187, 198, 209, 220, 231, 242, 253, 264]  # status01's sigil rows

# Weapon section coordinates, from its root's pivot
TOP = 314  # the title text's top
SKILLS_TOP = -305  # under the stat row
PENDULUM_GAP = 396  # loc_skill02's top below loc_skill01's
PENDULUM_H = 280  # loc_skill02's title and three rows
GAP = 10
TITLE_TEXT = 30  # the title text's top above the bar's top
GENE_GAP = 46  # loc_gene01's top below the title bar's top
GENE_H = 960

# sharecard pixels, the cyan section
SECTION_X, SECTION_Y, SECTION_W, SECTION_H = 600, 16, 756, 1388

pendulum_top = SKILLS_TOP - PENDULUM_GAP
title_top = pendulum_top - PENDULUM_H - GAP - TITLE_TEXT
gene_top = title_top - GENE_GAP
scale = SECTION_H * S / (TOP - gene_top + GENE_H)

# removes the old panel, the last objects
assert max(p.starts) == PANEL + 420 and p.children(PARENT)[-1] == PANEL
p.lines[p.starts[PANEL]:len(p.lines) - (p.lines[-1] == "")] = []
p.reindex()

ids = {old: PANEL + old for old in range(WEAPON_OBJECTS)}
trait_ids = {old: max(ids.values()) + 1 + i for i, old in enumerate(TRAIT_OBJECTS)}
trait_ids[TRAIT_IMAGE] = ids[WEAPON_IMAGE]
sigil_ids = {old: max(trait_ids.values()) + 1 + i for i, old in enumerate(SIGIL_OBJECTS)}

def field_lines(block, name):
    # a component field's lines, with their refs remapped into trait_ids
    i = block.index(f"      {name}:")
    end = i + 1
    while block[end].startswith("       ") or block[end].startswith("      - "):
        end += 1
    out = []
    for line in block[i:end]:
        if line.strip().startswith("ObjectRefId: "):
            head, ref = line.rsplit(" ", 1)
            line = f"{head} {trait_ids[int(ref)] if int(ref) != -1 else -1}"
        out.append(line)
    return out

source_info = traits.lines[slice(*traits.range(0))]
added = {name: field_lines(source_info, name) for name in FIELDS}

def edit(old, block):
    # keeps the root's WeaponInfo, names the panel, activates its root, shows the max level instead of the exp bar
    if old == 0:
        block = keep_components(block, ["WeaponInfo"])
        block[1] = "  Name: bc_weapon"
    if old == ROOT:
        block[block.index("  Active: false")] = "  Active: true"
    if old == MAX_LEVEL:
        block[block.index("  Active: false")] = "  Active: true"
    if old == EXP:
        block[block.index("  Active: true")] = "  Active: false"
    return block

new = copy_objects(weapon, list(ids), ids, edit)
# the trait rows and the Sigils section as the root's last children
i = new.index(f"- Id: {ids[ROOT]}")
i = new.index(f"  - {ids[2]}", i) + 1
new[i:i] = [f"  - {i_}" for i_ in (trait_ids[SKILLS], trait_ids[PENDULUM], sigil_ids[TITLE], sigil_ids[GENE])]
new += copy_objects(traits, list(TRAIT_OBJECTS), trait_ids)
new += copy_objects(sigils, SIGIL_OBJECTS, sigil_ids)

# WeaponInfo's trait fields, in the class's order
i = new.index("      Star:") + 4
new[i:i] = added["Skills"] + added["PendulumSkillObj"]
i = new.index("      SkillListPendulumEntry: false") + 1
new[i:i] = added["PendulumSkills"] + added["PendulumNames"]

end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline
p.lines[end:end] = new
p.reindex()

p.set(PANEL, "Scale", (scale, scale, 1))
x, y = card(SECTION_X + SECTION_W / 2, SECTION_Y)
p.place(PANEL, pos=(x, y - TOP * scale))
p.place(trait_ids[SKILLS], pos=(0, SKILLS_TOP))
p.place(trait_ids[PENDULUM], pos=(0, pendulum_top))
p.place(sigil_ids[TITLE], pos=(0, title_top))
p.place(sigil_ids[GENE], pos=(0, gene_top))

# CharaInfo.Gem on the root (object 0)
start, end = p.range(0)
i = p.lines.index("      Gem:", start, end)
for old, row in zip(OLD_ROWS, SIGIL_ROWS):
    j = p.lines.index(f"        ObjectRefId: {old}", i, end)
    p.lines[j] = f"        ObjectRefId: {sigil_ids[row]}"

p.save(sys.argv[5])
print(f"ids {PANEL}..{max(p.starts)}, scale {scale:.4f}")
