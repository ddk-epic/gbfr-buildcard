# Usage: python add_card_text.py <status01.prfb.yaml> <out.prfb.yaml>
# Adds a Text object at the top of the blue section for the mod to write to, and a ref to it at the end of
# CharaInfo.Powers, which is how the mod finds its Text component at runtime. The game writes PWR into every Powers
# text while filling the page; the mod overwrites it right after.
import sys
from prefab import Prefab

p = Prefab(sys.argv[1])
S = 3424 / 2880
ID = max(p.starts) + 1
PARENT = 426  # loc_buildcard: centred in loc_base01, same coordinates

LEFT, TOP = (1380 - 1440) * S, (720 - 16) * S
W, H = 1700, 64
INSET = 24

OBJECT = f"""- Id: {ID}
  Name: bc_text01
  Components:
  - ComponentName: Text
    Component:
      Text: build card
      FontPath: fonts/fot_skipstd_b_sdf
      MaterialPath: fonts/fot_skipstd_b_sdf/fot_skipstd_b_sdf_material
      FontSize: 48
      Color: 0.19607843, 0.37254903, 0.4901961, 1
      Margin: 0, 0, 0, 0
      IsGradient: false
      ColorMode: 3
      ColorTL: 1, 1, 1, 1
      ColorTR: 1, 1, 1, 1
      ColorBL: 1, 1, 1, 1
      ColorBR: 1, 1, 1, 1
      CharacterSpacing: 0
      LineSpacing: 0
      Alignment: 513
      Enable: true
  Active: true
  Position: 0, 0, 0
  Rotation: 0, 0, 0, 1
  Scale: 1, 1, 1
  Pivot: 0, 1
  AnchorPoint: 0, 0
  AnchorMin: 0.5, 0.5
  AnchorMax: 0.5, 0.5
  OffsetMin: 0, 0
  OffsetMax: 0, 0
  SizeDelta: {W}, {H}"""

# the object goes last, as loc_buildcard's last child, which keeps Ids depth-first
start, end = p.range(PARENT)
last_child = max(i for i in range(start, end) if p.lines[i].startswith("  - "))
p.lines.insert(last_child + 1, f"  - {ID}")
end = len(p.lines) - (p.lines[-1] == "")  # before the trailing newline
p.lines[end:end] = OBJECT.split("\n")
p.reindex()
p.place(ID, pos=(LEFT + INSET, TOP - INSET))

# CharaInfo.Powers on the root (object 0): append the ref after the list's last entry
i = p.lines.index("      Powers:", *p.range(0)) + 1
while p.lines[i].startswith("      - ") or p.lines[i].startswith("        "):
    i += 1
p.lines[i:i] = ["      - ComponentName: Text", "        Index: 0", f"        ObjectRefId: {ID}"]

p.save(sys.argv[2])
