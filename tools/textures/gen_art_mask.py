# Usage: python gen_art_mask.py <width> <height> <mask width> <mask height> <left> <right> <top> <bottom> <out.png>
# Draws an opaque mask fading to 0 over each side's width, in the mask's units.
import struct, sys, zlib

W, H = map(int, sys.argv[1:3])
MASK_W, MASK_H, LEFT, RIGHT, TOP, BOTTOM = map(float, sys.argv[3:9])


def ramp(p, w):
    # smoothstep alpha at a distance from an edge
    t = min(p / w, 1) if w > 0 else 1
    return t * t * (3 - 2 * t)


cols = [ramp(u, LEFT) * ramp(MASK_W - u, RIGHT) for u in ((x + 0.5) / W * MASK_W for x in range(W))]
raw = bytearray()
for y in range(H):
    v = (y + 0.5) / H * MASK_H
    row = ramp(v, TOP) * ramp(MASK_H - v, BOTTOM)
    raw.append(0)
    for c in cols:
        raw += bytes((255, 255, 255, round(c * row * 255)))


def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

open(sys.argv[9], "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 6, 0, 0, 0))
                              + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b""))
