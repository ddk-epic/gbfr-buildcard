# Usage: python gen_portrait_mask.py <ps_cmn_mask_chara02.dds> <width> <ease start> <curve start> <curve end> <shoulder> <floor> <angle> <pivot> <out.png>
# Draws the portrait's mask in sharecard pixels from its opaque right edge: an ease to the shoulder, an S-curve to the
# floor and a tail to 0, leaning by the angle about the pivot row, over the rows of the decoded ps_cmn_mask_chara02.
import math, struct, sys, zlib

src = open(sys.argv[1], "rb").read()
H, W = struct.unpack_from("<II", src, 12)
OFF = 128 + (20 if src[84:88] == b"DX10" else 0)
WIDTH, EASE, START, END, SHOULDER, FLOOR, ANGLE, PIVOT = map(float, sys.argv[2:10])
TAN = math.tan(math.radians(ANGLE))
CARD_H = 1440
PAD_ROWS = 17.076  # ps_cmn_mask_chara02's transparent rows
EASE_SLOPE = -2 * (1 - SHOULDER) / (START - EASE)  # the ease's slope at its end
TAIL_SLOPE = -2 * FLOOR / (WIDTH - END)  # the tail's slope at its start


def fade(p):
    # alpha at p pixels from the opaque edge
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


raw = bytearray()
for y in range(H):
    raw.append(0)
    v = src[OFF + (y * W + W - 1) * 4 + 3]
    # the fade's lean at the row's card y
    shift = (PIVOT - (H - y - 0.5) / (H - PAD_ROWS) * CARD_H) * TAN
    for x in range(W):
        a = fade((W - x - 0.5) / W * WIDTH - shift)
        raw += bytes((255, 255, 255, round(min(max(a, 0), 1) * v)))


def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

open(sys.argv[10], "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 6, 0, 0, 0))
                              + chunk(b"IDAT", zlib.compress(bytes(raw), 9)) + chunk(b"IEND", b""))
