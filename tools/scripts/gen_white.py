# Usage: python gen_white.py <size> <out.png>
# Draws an opaque white square, for rectangular masks.
import struct, sys, zlib

N = int(sys.argv[1])
raw = (b"\x00" + b"\xff" * 4 * N) * N

def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

open(sys.argv[2], "wb").write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", N, N, 8, 6, 0, 0, 0))
                              + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))
