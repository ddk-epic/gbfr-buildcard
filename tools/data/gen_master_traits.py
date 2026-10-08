# Usage: python gen_master_traits.py <gbfr-extract dir> <gbfr-sharecard dir> <mod dir>
# Writes the master trait cells to <mod dir>/Data/master_traits.tsv and their button icon tags to the mod's
# text_skillboard_tag.msg.
import json, os, re, sqlite3, sys
import msg
from hashing import xxhash32_custom

EXTRACT, SHARECARD, MOD = sys.argv[1:]
STYLES = ["SB_DEF", "SB_ATK", "SB_LIMIT"]
STYLE_IDS = ["insight", "essence", "crux"]
RANKS = ["68DE92AC", "A96D9EBC", "4A5DDC7B", "3B99904D"]
RANK_IDS = ["r1", "r2", "r3", "ex"]
BUTTON_ICONS = {"LMB": 4, "RMB": 3}
TAG_FILE = "system/table/text/en/text_skillboard_tag.msg"

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

tag_file = msg.load(os.path.join(EXTRACT, TAG_FILE))
tag_file["Tag"]["tags_"] += tags
os.makedirs(os.path.join(MOD, "GBFR", "data", os.path.dirname(TAG_FILE)), exist_ok=True)
open(os.path.join(MOD, "GBFR", "data", TAG_FILE), "wb").write(msg.pack(tag_file))
print(f"{len(out)} cells, {len(tags)} with button icons, {len(missing)} without a label: {', '.join(missing)}")
