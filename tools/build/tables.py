# Adds the card's Save Card row to the game's footer guide table, ui/table/guide_button.msg.
import os
import struct
import sys

import msgpack

sys.path.append(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "data"))
from hashing import xxhash32_custom  # noqa: E402

OUTPUT = "gbfr.qol.buildcard/GBFR/data/ui/table/guide_button.msg"

LABEL = "SaveCard"
TEXT_ID = "TXT_PAU_PHT_SHOOT"
BUTTON = ("L3", 13)  # the guide table's button: name, index; 1 on the keyboard in menus


def pack(value):
    # msgpack with map32 and array32 throughout, as the game's tables are written
    if isinstance(value, dict):
        return b"\xdf" + struct.pack(">I", len(value)) + b"".join(pack(k) + pack(v) for k, v in value.items())
    if isinstance(value, list):
        return b"\xdd" + struct.pack(">I", len(value)) + b"".join(pack(v) for v in value)
    return msgpack.packb(value, use_bin_type=True)


def row():
    return {"Element": {
        "label_": LABEL,
        "button_": {"str_": BUTTON[0], "index_": BUTTON[1]},
        "button2_": {"str_": "None", "index_": 0},
        "isAnd_": False,
        "textID_": TEXT_ID,
        "colorFlag_": True,
        "color_": {"str_": "Guide", "index_": 0},
        "config_": {"str_": "---", "index_": 0},
    }}


def apply(ctx):
    table = msgpack.unpackb(ctx.stock_bytes("guide_button.msg"), raw=False)
    table["GuideList"]["datas_"].append(row())
    ctx.binaries["guide_button"] = (OUTPUT, pack(table))
    ctx.export("SaveCardLabel", xxhash32_custom(LABEL))
    # the Shortcut enum counts from one below the guide table's
    ctx.export("SaveCardButton", BUTTON[1] - 1)
