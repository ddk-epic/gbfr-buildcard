# Usage: python patch_texts.py <gbfr-extract dir> <mod dir>
# Writes the mod's text_ui.msg with status01 copies of other prefabs' text variants.
import os, sys
import msg

EXTRACT, MOD = sys.argv[1:]
TEXT_FILE = "system/table/text/en/text_ui.msg"
PREFAB = "status01"
# text id: the prefab whose variant status01 takes
VARIANTS = {"TXT_PAU_ITEM_WEAPON": "equip01_info01"}

table = msg.load(os.path.join(EXTRACT, TEXT_FILE))
rows = table["rows_"]
for text_id, source in VARIANTS.items():
    assert not any(r["column_"]["id_hash_"] == text_id and r["column_"]["subid_hash_"] == PREFAB for r in rows)
    i = next(i for i, r in enumerate(rows)
             if r["column_"]["id_hash_"] == text_id and r["column_"]["subid_hash_"] == source)
    rows.insert(i + 1, {"column_": {**rows[i]["column_"], "subid_hash_": PREFAB}})
    print(f"{text_id} for {PREFAB}: {rows[i]['column_']['text_']}")

os.makedirs(os.path.join(MOD, "GBFR", "data", os.path.dirname(TEXT_FILE)), exist_ok=True)
open(os.path.join(MOD, "GBFR", "data", TEXT_FILE), "wb").write(msg.pack(table))
