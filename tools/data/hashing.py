# The game's string hashes.
import struct


def xxhash32_custom(text):
    # GBFRDataTools.Hashing.XXHash32Custom
    m = 0xFFFFFFFF
    p1, p2, p3, p4, p5 = 0x9E3779B1, 0x85EBCA77, 0xC2B2AE3D, 0x27D4EB2F, 0x165667B1
    rotl = lambda x, r: ((x << r) | (x >> (32 - r))) & m
    b, p, h = text.encode("ascii"), 0, 0x178A54A4
    if len(b) >= 16:
        v = [0x2557311B, 0x871FB76A, 0x0133ECF3, 0x62FC7342]
        while True:
            for k in range(4):
                v[k] = rotl((v[k] + struct.unpack_from("<I", b, p + 4 * k)[0] * p2) & m, 13) * p1 & m
            p += 16
            if len(b) - p <= 16:
                break
        h = (rotl(v[0], 1) + rotl(v[1], 7) + rotl(v[2], 12) + rotl(v[3], 18)) & m
    h = (h + len(b)) & m
    while len(b) - p >= 4:
        h = rotl((h + struct.unpack_from("<I", b, p)[0] * p3) & m, 17) * p4 & m
        p += 4
    while p < len(b):
        h = rotl((h + b[p] * p5) & m, 11) * p1 & m
        p += 1
    h ^= h >> 15
    h = h * p2 & m
    h ^= h >> 13
    h = h * p3 & m
    return h ^ (h >> 16)
