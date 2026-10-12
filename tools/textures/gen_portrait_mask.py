# Usage: python gen_portrait_mask.py <ps_cmn_mask_chara02.dds> <width> <ease start> <curve start> <curve end> <shoulder> <floor> <angle> <pivot> [<radius>] <out.png>
# Draws the portrait's mask from its opaque right edge over the rows of the decoded ps_cmn_mask_chara02.
import math, struct, sys, zlib

src = open(sys.argv[1], "rb").read()
H, W = struct.unpack_from("<II", src, 12)
OFF = 128 + (20 if src[84:88] == b"DX10" else 0)
WIDTH, EASE, START, END, SHOULDER, FLOOR, ANGLE, PIVOT = map(float, sys.argv[2:10])
RADIUS = float(sys.argv[10]) if len(sys.argv) > 11 else None
TAN = math.tan(math.radians(ANGLE))
CARD_H = 1764  # card units
PAD_ROWS = 17.076  # ps_cmn_mask_chara02's transparent rows
EASE_SLOPE = -2 * (1 - SHOULDER) / (START - EASE)  # the ease's slope at its end
TAIL_SLOPE = -2 * FLOOR / (WIDTH - END)  # the tail's slope at its start
if RADIUS is not None:
    # the circles' center
    CX = EASE - RADIUS * math.cos(math.radians(ANGLE))
    CY = PIVOT - RADIUS * math.sin(math.radians(ANGLE))


def fade(p):
    # alpha at p card units from the opaque edge
    if p <= EASE:
        return 1
    if p < START:
        t = (p - EASE) / (START - EASE)
        return 1 - (1 - SHOULDER) * t * t
    if p < END:
        # cubic Hermite from the shoulder to the floor
        d = END - START
        t = (p - START) / d
        return ((2 * t ** 3 - 3 * t ** 2 + 1) * SHOULDER + (t ** 3 - 2 * t ** 2 + t) * d * EASE_SLOPE
                + (3 * t ** 2 - 2 * t ** 3) * FLOOR + (t ** 3 - t ** 2) * d * TAIL_SLOPE)
    return FLOOR * (1 - min((p - END) / (WIDTH - END), 1)) ** 2


def dist(x, y):
    # the fade's distance at card point (x, y)
    if RADIUS is None:
        return x - (PIVOT - y) * TAN
    return EASE + math.hypot(x - CX, y - CY) - RADIUS


raw = bytearray()
for y in range(H):
    raw.append(0)
    v = src[OFF + (y * W + W - 1) * 4 + 3]
    card_y = (H - y - 0.5) / (H - PAD_ROWS) * CARD_H
    for x in range(W):
        a = fade(dist((W - x - 0.5) / W * WIDTH, card_y))
        raw += bytes((255, 255, 255, round(min(max(a, 0), 1) * v)))


def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

open(sys.argv[-1], "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 6, 0, 0, 0))
                               + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b""))
