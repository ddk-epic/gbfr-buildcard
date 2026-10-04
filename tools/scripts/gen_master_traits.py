# Usage: python gen_master_traits.py <gbfr-extract dir> <gbfr-sharecard dir> <mod dir>
# Writes the master trait cells to <mod dir>/Data/master_traits.tsv and their button icon tags to the mod's
# text_skillboard_tag.msg.
import json, os, re, sqlite3, struct, sys
import msgpack

EXTRACT, SHARECARD, MOD = sys.argv[1:]
STYLES = ["SB_DEF", "SB_ATK", "SB_LIMIT"]
STYLE_IDS = ["insight", "essence", "crux"]
RANKS = ["68DE92AC", "A96D9EBC", "4A5DDC7B", "3B99904D"]
RANK_IDS = ["r1", "r2", "r3", "ex"]
BUTTON_ICONS = {"LMB": 4, "RMB": 3}
TAG_FILE = "system/table/text/en/text_skillboard_tag.msg"

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

def pack(value):
    # msgpack with 32-bit maps and arrays
    if isinstance(value, dict):
        return b"\xdf" + struct.pack(">I", len(value)) + b"".join(pack(k) + pack(v) for k, v in value.items())
    if isinstance(value, list):
        return b"\xdd" + struct.pack(">I", len(value)) + b"".join(pack(v) for v in value)
    return msgpack.packb(value)

def element(**fields):
    return {"Element": fields}

data = os.path.join(SHARECARD, "src", "data")
load = lambda *path: json.load(open(os.path.join(data, *path), encoding="utf-8"))
shared = load("master-trait-shared.json")
characters = {c["charId"]: c["id"] for c in load("characters.json")}

db = sqlite3.connect(os.path.join(EXTRACT, "tables.sqlite"))
rows = db.execute("select SkillboardEffectOrUiId, SkillboardCategoryId, SkillboardGroupId, CharacterId, Unk25, Unk30 "
                  "from skillboard_layout order by CharacterId, Unk30").fetchall()

out, tags, missing = [], [], []
positions = {}
for key, category, group, chara, unk25, order in rows:
    if category not in STYLES or chara not in characters:
        continue
    style, rank = STYLES.index(category), RANKS.index(group)
    traits = load("characters", f"{characters[chara]}.json")["masterTraits"]
    cells = traits["cells"] if traits.get("grid") == "captain" else {**shared, **traits["cells"]}
    if unk25 == 100:  # perk
        label = traits["titles"][STYLE_IDS[style]] if rank == 0 else ""
        out.append((key, style, rank, 0, label, ""))
        continue
    position = positions[chara, style, rank] = positions.get((chara, style, rank), 0) + 1
    cell = cells.get(f"{STYLE_IDS[style]}.{RANK_IDS[rank]}.{position}")
    if cell is None:
        missing.append(f"{chara} {STYLE_IDS[style]}.{RANK_IDS[rank]}.{position}")
        continue

    label, icons = cell["label"], []
    while m := re.search(r"\[(LMB|RMB)\]", label):
        icons.append((BUTTON_ICONS[m[1]], m.start() + 3 * len(icons)))
        label = label[:m.start()] + "<d>" + label[m.end():]
    text_hash = ""
    if icons:
        text_id = f"TXT_BC_MT_{key}"
        text_hash = f"{xxhash32_custom(text_id):08X}"
        tags.append(element(id_=text_id, subid_="",
                            icons_=[element(icon_=icon, start_=at, end_=at) for icon, at in icons],
                            dynamics_=[element(dtag_=6, index_=-1, start_=at, end_=at) for _, at in icons]))
    out.append((key, style, rank, position, label, text_hash))

with open(os.path.join(MOD, "Data", "master_traits.tsv"), "w", encoding="utf-8", newline="\n") as f:
    for row in out:
        f.write("\t".join(str(v) for v in row) + "\n")

tag_file = msgpack.unpackb(open(os.path.join(EXTRACT, TAG_FILE), "rb").read(), strict_map_key=False)
tag_file["Tag"]["tags_"] += tags
os.makedirs(os.path.join(MOD, "GBFR", "data", os.path.dirname(TAG_FILE)), exist_ok=True)
open(os.path.join(MOD, "GBFR", "data", TAG_FILE), "wb").write(pack(tag_file))
print(f"{len(out)} cells, {len(tags)} with button icons, {len(missing)} without a label: {', '.join(missing)}")
