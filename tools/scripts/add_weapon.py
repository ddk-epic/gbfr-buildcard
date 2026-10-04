# Usage: python add_weapon.py <status01.prfb.yaml> <equip01_info_weapon01.prfb.yaml> <out.prfb.yaml>
# Copies the equip screen's weapon panel (equip01_info_weapon01: name, series, art, gauge, level, stats, weapon traits
# and the wrightstone's traits) into the cyan section, keeping only the WeaponInfo on its root, and points
# CharaInfo.Weapon at it, so that the page fill sets the whole panel. The gear block's own weapon row and the sigils
# are hidden.
import sys
from prefab import Prefab
from card import PARENT, S, card, copy_objects, keep_components

p = Prefab(sys.argv[1])
source = Prefab(sys.argv[2])

PANEL_OBJECTS = 421  # equip01_info_weapon01's objects, Ids 0 to 420
ROOT = 1  # root, inactive until the panel's in animation
PANEL_W, PANEL_H = 1200, 1716  # loc_info01
GEAR_BLOCK = 109  # loc_chr_status02: weapon row and sigils
WEAPON_ROW = 114  # chr_status02_p01, the gear block's WeaponInfo

# sharecard pixels, the cyan section
SECTION_X, SECTION_Y, SECTION_W, SECTION_H = 600, 16, 756, 1388
SCALE = SECTION_W * S / PANEL_W

first = max(p.starts) + 1
ids = {old: first + old for old in range(PANEL_OBJECTS)}

def edit(old, block):
    # keeps the root's WeaponInfo, names the panel, activates its root
    if old == 0:
        block = keep_components(block, ["WeaponInfo"])
        block[1] = "  Name: bc_weapon"
    if old == ROOT:
        block[block.index("  Active: false")] = "  Active: true"
    return block

new = copy_objects(source, list(ids), ids, edit)
start, end = p.range(PARENT)
last_child = max(i for i in range(start, end) if p.lines[i].startswith("  - "))
p.lines.insert(last_child + 1, f"  - {first}")
end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline
p.lines[end:end] = new
p.reindex()

p.set(first, "Scale", (SCALE, SCALE, 1))
p.place(first, pos=card(SECTION_X + SECTION_W / 2, SECTION_Y + PANEL_H * SCALE / S / 2))

# CharaInfo.Weapon on the root (object 0)
start, end = p.range(0)
i = p.lines.index("      Weapon:", start, end)
assert p.lines[i + 3] == f"        ObjectRefId: {WEAPON_ROW}"
p.lines[i + 3] = f"        ObjectRefId: {first}"

p.set(GEAR_BLOCK, "Active", "false")
p.save(sys.argv[3])
print(f"added ids {first}..{first + PANEL_OBJECTS - 1}, scale {SCALE:.4f}, "
      f"height {PANEL_H * SCALE / S:.1f} of {SECTION_H}")
