# Usage: python gen_masteries.py <gbfr-extract dir> <mod dir>
# Writes the mastery nodes to <mod dir>/Data/masteries.tsv.
import os, re, sqlite3, sys
from hashing import xxhash32_custom

EXTRACT, MOD = sys.argv[1:]
EXTENSION_FROM = 300  # DiffSeparatorMaybe of the nodes past 100%
TRANSCENDED = 7  # ReqWepTranscensionLevel of the bought transcendence rows
LADDER = 8  # bits per limit_bonus entry
OFFENSE, OFFENSE_EXTENSION, DEFENSE, DEFENSE_EXTENSION, COLLECTION, TRANSCENDENCE = range(6)

db = sqlite3.connect(os.path.join(EXTRACT, "tables.sqlite"))
rows = []
for table, base, extension in (("ap_tree_atk", OFFENSE, OFFENSE_EXTENSION), ("ap_tree_def", DEFENSE, DEFENSE_EXTENSION)):
    rows += [(chara, key, index, extension if diff >= EXTENSION_FROM else base) for chara, key, index, diff in
             db.execute(f"select CharaId, LimitBonusId, LimitBonusParamIndex, DiffSeparatorMaybe from {table}")]
rows += [(chara, key, index, COLLECTION) for chara, key, index in
         db.execute("select CharaId, LimitBonusId, LimitBonusParamIndex from ap_tree_wep")]
rows += [(chara, key, index, TRANSCENDENCE) for chara, key, index in
         db.execute("select CharaId, LimitBonusId, LimitBonusParamIndex from ap_tree_rebuild "
                    "where ReqWepTranscensionLevel = ?", (TRANSCENDED,))]


def key_hash(key):
    # the LimitBonusId as a hash; some ids are names
    return key if re.fullmatch("[0-9A-F]{8}", key) else f"{xxhash32_custom(key):08X}"


# chara and limit_bonus key -> each ladder bit's section, - for an unused bit
ladders = {}
for chara, key, index, section in rows:
    ladder = ladders.setdefault((chara, key_hash(key)), ["-"] * LADDER)
    if ladder[index] != "-":
        raise ValueError(f"{chara} {key} bit {index} in two sections")
    ladder[index] = str(section)

with open(os.path.join(MOD, "Data", "masteries.tsv"), "w", encoding="utf-8", newline="\n") as f:
    for (chara, key), ladder in sorted(ladders.items()):
        f.write(f"{xxhash32_custom(chara):08X}\t{key}\t{''.join(ladder)}\n")
print(f"{len(rows)} nodes in {len(ladders)} entries of {len({c for c, _ in ladders})} characters")
