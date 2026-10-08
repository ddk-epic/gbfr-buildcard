# Reads and writes the game's .msg tables.
import struct
import msgpack

def load(path):
    return msgpack.unpackb(open(path, "rb").read(), strict_map_key=False)

def pack(value):
    # msgpack with 32-bit maps and arrays
    if isinstance(value, dict):
        return b"\xdf" + struct.pack(">I", len(value)) + b"".join(pack(k) + pack(v) for k, v in value.items())
    if isinstance(value, list):
        return b"\xdd" + struct.pack(">I", len(value)) + b"".join(pack(v) for v in value)
    return msgpack.packb(value)
