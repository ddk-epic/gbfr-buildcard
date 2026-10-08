# Usage: python gen_rounded.py <width> <height> <rect_w> <rect_h> <radius> <out.png>
# Draws an opaque white rect with rounded corners over the texture, for masks.
import struct, sys, zlib

W, H = int(sys.argv[1]), int(sys.argv[2])
RW, RH, R = (float(a) for a in sys.argv[3:6])
SX, SY = RW / W, RH / H  # rect units per texel
SAMPLES = 8  # per pixel side

def inside(x, y):
    cx, cy = min(max(x, R), RW - R), min(max(y, R), RH - R)
    return (x - cx) ** 2 + (y - cy) ** 2 <= R * R

def pixel(px, py):
    # pixels away from the corners are opaque
    x0, y0, x1, y1 = px * SX, py * SY, (px + 1) * SX, (py + 1) * SY
    if R <= x0 and x1 <= RW - R or R <= y0 and y1 <= RH - R:
        return b"\xff\xff\xff\xff"
    hits = sum(inside(x0 + (i + 0.5) / SAMPLES * SX, y0 + (j + 0.5) / SAMPLES * SY)
               for i in range(SAMPLES) for j in range(SAMPLES))
    return b"\xff\xff\xff" + bytes([round(255 * hits / SAMPLES ** 2)])

raw = b"".join(b"\x00" + b"".join(pixel(x, y) for x in range(W)) for y in range(H))

def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

open(sys.argv[6], "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 6, 0, 0, 0))
                              + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))
