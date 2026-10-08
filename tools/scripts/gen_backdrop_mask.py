# Usage: python gen_backdrop_mask.py <width> <height> <out.png>
# Draws sharecard's parchment cut as the alpha of a mask over the white panel.
import math, struct, sys, zlib

S = 3424 / 2880  # card scale

W, H = int(sys.argv[1]), int(sys.argv[2])

# bg01, 4K units
PANEL_W, PANEL_H = 3472, 1763
CARD_LEFT, CARD_TOP = 24, 24

# sharecard's ParchmentBackdrop, art pixels
ART_W, ART_H = 1392, 1813
EDGE_TOP_X, EDGE_BOT_X = 1390, 622
SPIKE_TOP_Y, SPIKE_APEX_OFF, SPIKE_INNER_OFF, SPIKE_OUTER_OFF = 453, 8, 26, 78
ANGLE, PIVOT_Y, MASK_W = 14, 2900, 1106
CARD_H = 1440

SUBROWS = 4

tan = math.tan(math.radians(ANGLE))
top_x = EDGE_TOP_X + (EDGE_BOT_X - EDGE_TOP_X) * PIVOT_Y / ART_H + tan * PIVOT_Y
bot_x = top_x - tan * ART_H
apex = (top_x + (bot_x - top_x) * SPIKE_TOP_Y / ART_H + SPIKE_APEX_OFF, SPIKE_TOP_Y)
inner, outer = bot_x + SPIKE_INNER_OFF, bot_x + SPIKE_OUTER_OFF

def card_px(x, y):
    return x * MASK_W / ART_W, y * CARD_H / ART_H

top_x, _ = card_px(top_x, 0)
bot_x, _ = card_px(bot_x, 0)
apex = card_px(*apex)
inner, _ = card_px(inner, 0)
outer, _ = card_px(outer, 0)

def lerp_x(x0, y0, x1, y1, y):
    return x0 + (x1 - x0) * (y - y0) / (y1 - y0)

def spans(y):
    # spans inside the cut at card row y
    if not 0 <= y <= CARD_H:
        return []
    result = [(0, lerp_x(top_x, 0, bot_x, CARD_H, y))]
    if y >= apex[1]:
        result.append((lerp_x(*apex, inner, CARD_H, y), lerp_x(*apex, outer, CARD_H, y)))
    return result

def to_u(x):
    # card pixels to texels
    return (x * S + CARD_LEFT) * W / PANEL_W

coverage = [[0.0] * W for _ in range(H)]
for v in range(H):
    row = coverage[v]
    for sub in range(SUBROWS):
        y = ((v + (sub + 0.5) / SUBROWS) * PANEL_H / H - CARD_TOP) / S
        for a, b in spans(y):
            a, b = max(0.0, to_u(a)), min(float(W), to_u(b))
            for u in range(int(a), min(W, math.ceil(b))):
                row[u] += (min(b, u + 1) - max(a, u)) / SUBROWS

raw = bytearray()
for row in coverage:
    raw.append(0)
    for c in row:
        raw += bytes((255, 255, 255, round(min(c, 1.0) * 255)))

def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 6, 0, 0, 0)) \
      + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b"")
open(sys.argv[3], "wb").write(png)
