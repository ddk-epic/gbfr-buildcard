# Usage: python add_master_traits_bg.py <status01.prfb.yaml> <background04.prfb.yaml> <out.prfb.yaml>
# Puts the Master Traits menu's background (background04's loc_bg, without the character) behind the master traits
# board, covering the blue section and clipped to it. Renumbers the Ids after it.
import sys
from prefab import Prefab, renumber
from card import S, card, copy_objects, rect

p = Prefab(sys.argv[1])
src = Prefab(sys.argv[2])

BOARD = 463  # bc_mtraits
SCENE = 2  # loc_bg
CHARA = 26  # loc_chara01
SCENE_W, SCENE_H = 3840, 2160
MASK = "layouts/pause/status/noatlastextures/bc_white"

# sharecard pixels, the blue section
LEFT, TOP, WIDTH, HEIGHT = 1380, 16, 1484, 1102

def subtree(prefab, id_, skip):
    out = [id_]
    for c in prefab.children(id_):
        if c != skip:
            out += subtree(prefab, c, skip)
    return out

def drop_chara(old, block):
    return [l for l in block if l != f"  - {CHARA}"]

clip = max(p.starts) + 1
objects = subtree(src, SCENE, CHARA)
ids = {old: clip + 1 + i for i, old in enumerate(objects)}
scene_lines = copy_objects(src, objects, ids, drop_chara)

blocks = {i: p.lines[slice(*p.range(i))] for i in p.starts}
children = {i: p.children(i) for i in p.starts}
mask = ["  - ComponentName: Mask", "    Component:", "      Sprite:", f"        TexturePath: {MASK}",
        f"        SpriteName: {MASK.rsplit('/', 1)[1]}", "      Offset: 0, 0", "      ChannelWeights: 0, 0, 0, 1",
        "      InvertMask: false", "      InvertOutsides: false", "      Enable: true"]
block = rect("bc_mt_bg", mask, (0.5, 0.5))
blocks[clip] = [f"- Id: {clip}", block[0], "  Children:", *block[1:]]
children[clip] = [ids[SCENE]]
starts = [i for i, l in enumerate(scene_lines) if l.startswith("- Id: ")] + [len(scene_lines)]
for a, b in zip(starts, starts[1:]):
    block = scene_lines[a:b]
    id_ = int(block[0][6:])
    blocks[id_] = block
    children[id_] = []
    if "  Children:" in block:
        i = block.index("  Children:") + 1
        while i < len(block) and block[i].startswith("  - "):
            children[id_].append(int(block[i][4:]))
            i += 1
children[BOARD] = [clip] + children[BOARD]

new = renumber(p, blocks, children)
clip, scene = new[clip], new[ids[SCENE]]

# the scene covers the section, centred on it; the clip is the section
k = max(WIDTH * S / SCENE_W, HEIGHT * S / SCENE_H)
p.set(clip, "Scale", (k, k, 1))
p.place(clip, pos=card(LEFT + WIDTH / 2, TOP + HEIGHT / 2), size=(WIDTH * S / k, HEIGHT * S / k))

p.save(sys.argv[3])
print(f"ids {clip}..{scene + len(objects) - 1}, scale {k:.4f}, shift after it {len(objects) + 1}")
