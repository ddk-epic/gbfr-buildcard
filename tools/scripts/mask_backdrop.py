# Usage: python mask_backdrop.py <chr_status_bg01.prfb.yaml> <out.prfb.yaml>
# Adds the parchment cut mask to the white panel, clipping the blue backdrop.
import sys
from prefab import Prefab

p = Prefab(sys.argv[1])

PANEL = 3  # bg01
MASK = "layouts/pause/status/noatlastextures/bc_backdrop_mask"

start, end = p.range(PANEL)
i = p.lines.index("  Active: true", start, end)
p.lines[i:i] = ["  - ComponentName: Mask", "    Component:", "      Sprite:", f"        TexturePath: {MASK}",
                f"        SpriteName: {MASK.rsplit('/', 1)[1]}", "      Offset: 0, 0", "      ChannelWeights: 0, 0, 0, 1",
                "      InvertMask: false", "      InvertOutsides: false", "      Enable: true"]

p.save(sys.argv[2])
