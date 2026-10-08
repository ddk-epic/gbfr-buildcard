# Usage: python gen_outline.py <size> <width> <radius> <out.png>
# Draws a white square outline with rounded corners and a transparent centre, for sliced borders.
import struct, sys, zlib

N, W, R = int(sys.argv[1]), int(sys.argv[2]), float(sys.argv[3])
SAMPLES = 8  # per pixel side

def in_square(x, y, inset, radius):
    # the point lies in the square inset by inset, with corners of radius
    lo, hi = inset + radius, N - inset - radius
    cx, cy = min(max(x, lo), hi), min(max(y, lo), hi)
    if radius == 0:
        return lo <= x <= hi and lo <= y <= hi
    return (x - cx) ** 2 + (y - cy) ** 2 <= radius * radius

def inside(x, y):
    return in_square(x, y, 0, R) and not in_square(x, y, W, max(R - W, 0))

def pixel(px, py):
    hits = sum(inside(px + (i + 0.5) / SAMPLES, py + (j + 0.5) / SAMPLES) for i in range(SAMPLES) for j in range(SAMPLES))
    return b"\xff\xff\xff" + bytes([round(255 * hits / SAMPLES ** 2)])

raw = b"".join(b"\x00" + b"".join(pixel(x, y) for x in range(N)) for y in range(N))

def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

open(sys.argv[4], "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", N, N, 8, 6, 0, 0, 0))
                              + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))
