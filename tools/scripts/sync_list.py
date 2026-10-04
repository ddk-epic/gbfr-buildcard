# Usage: python sync_list.py <prefab.prfb.yaml> <prefab.list.yaml> <out.list.yaml> [<source.list.yaml>...]
# Adds the assets the prefab references, and the source lists' entries except animations, to the prefab's list.
import re, sys

SECTIONS = ["TextureData", "AtlasData", "Materials", "Animations", "ImageData", "LanguageData"]
PER_LANGUAGE = ["TextureData", "AtlasData"]  # sections split into Common and one list per language

def prefab_assets(path):
    assets = {(s, "Common" if s in PER_LANGUAGE else None): [] for s in SECTIONS}
    def add(section, value):
        key = (section, "Common" if section in PER_LANGUAGE else None)
        if value not in assets[key]:
            assets[key].append(value)
    for line in open(path, encoding="utf-8").read().splitlines():
        m = re.match(r"\s*(?:- )?(\w+): (\S+)$", line)
        if m:
            key, value = m[1], m[2]
            if key == "TexturePath" and not value.endswith("_dummy"):
                if not value.startswith("atlas/"):
                    add("TextureData", value)
                elif value.count("/") == 1:
                    add("AtlasData", value)
            elif key == "MaterialPath":
                add("Materials", value)
            elif key == "AnimationPath":
                add("Animations", value)
            elif key == "ImageDataPath":
                add("ImageData", value)
            elif key == "LanguageData":
                add("LanguageData", value)
        m = re.match(r"\s*- (data/image/\S+)$", line)
        if m:
            add("ImageData", m[1])
    return assets

def find_list(lines, section, sub):
    # the line of the list's key and the range of its items
    key = lines.index(f"{section}:")
    if sub:
        key = next(i for i in range(key + 1, len(lines)) if re.match(rf"  {sub}:( \[\])?$", lines[i]))
    start = end = key + 1
    while end < len(lines) and re.match(r"\s*- ", lines[end]):
        end += 1
    return key, start, end

def list_assets(path):
    # every list of a list file: {(section, language or Common or None): items}
    lines = open(path, encoding="utf-8").read().splitlines()
    assets = {}
    for i, line in enumerate(lines):
        if line.rstrip(":") in SECTIONS:
            section = line.rstrip(":")
        m = re.match(r"  (\w+):", line)
        key = (section, m[1]) if m else (section, None) if line.endswith(":") and line[:-1] in SECTIONS else None
        if key:
            _, start, end = find_list(lines, *key)
            assets[key] = [re.match(r"\s*- (\S+)", l)[1] for l in lines[start:end]]
    return assets

text = open(sys.argv[2], encoding="utf-8").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
wanted = prefab_assets(sys.argv[1])
for source in sys.argv[4:]:
    for key, values in list_assets(source).items():
        if key[0] != "Animations":
            wanted[key] = wanted.get(key, []) + [v for v in values if v not in wanted.get(key, [])]
added = []
for (section, sub), values in wanted.items():
    key, start, end = find_list(lines, section, sub)
    items = [re.match(r"\s*- (\S+)", line)[1] for line in lines[start:end]]
    new = [v for v in values if v not in items]
    if not new:
        continue
    if lines[key].endswith(" []"):
        lines[key] = lines[key][:-3]
    indent = "  - " if sub else "- "
    lines[end:end] = [f"{indent}{v}" for v in new]
    added += [f"{section}{'.' + sub if sub else ''}: {v}" for v in new]
open(sys.argv[3], "w", encoding="utf-8", newline="").write(nl.join(lines))
print("\n".join(added) or "nothing to add")
