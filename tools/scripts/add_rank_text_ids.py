# Usage: python add_rank_text_ids.py <status01.prfb.yaml> <out.prfb.yaml>
# Gives the master traits style rank labels the game's text ids.
import re, sys
from prefab import Prefab

p = Prefab(sys.argv[1])

TEXT_IDS = [f"TXT_PAU_SKL_BD_ST_RANK_{n}" for n in range(1, 5)]

labels = [(i, int(m[1])) for i in p.starts if (m := re.fullmatch(r"bc_mt_\d+_(\d+)_label", p.get(i, "Name")))]
for i, rank in sorted(labels, reverse=True):
    start, end = p.range(i)
    at = next(k for k in range(start, end) if p.lines[k].startswith("  Active: "))
    p.lines[at:at] = ["  - ComponentName: TextSetter", "    Component:", f"      TextID: {TEXT_IDS[rank]}",
                      "      Enable: true"]
p.reindex()

# removes the labels' Text refs from CharaInfo.Powers
start, end = p.range(0)
powers = next(k for k in range(start, end) if p.lines[k].strip() == "Powers:")
ids = {i for i, _ in labels}
k = powers + 1
while k < end and p.lines[k].startswith("      - "):
    if p.lines[k].strip() == "- ComponentName: Text" and int(p.lines[k + 2].split(": ")[1]) in ids:
        del p.lines[k:k + 3]
        end -= 3
    else:
        k += 3
p.reindex()

p.save(sys.argv[2])
print(f"{len(labels)} labels, {sum(line.strip() == f'ObjectRefId: {i}' for i in ids for line in p.lines)} refs left")
