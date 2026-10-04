# Usage: python sync_list.py <prefab.prfb.yaml> <prefab.list.yaml> <out.list.yaml>
# Adds the assets a prefab references and its list (the assets the game loads with the prefab) lacks: textures and
# atlases (TexturePath), materials (MaterialPath), animations (AnimationPath), image data (ImageDataPath and
# ImageDataPaths) and language data (LanguageData). Placeholder textures (*_dummy) and per-language atlases
# (atlas/<language>/...) are left out, as in the game's lists. New entries go at the end of their section.
import re, sys

SECTIONS = ["TextureData", "AtlasData", "Materials", "Animations", "ImageData", "LanguageData"]

def prefab_assets(path):
    assets = {s: [] for s in SECTIONS}
    def add(section, value):
        if value not in assets[section]:
            assets[section].append(value)
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

def section_items(lines, section):
    # the range of the section's list (its Common list for TextureData and AtlasData) and its items
    start = lines.index(f"{section}:") + 1
    if section in ("TextureData", "AtlasData"):
        start = lines.index("  Common:", start) + 1
        if lines[start - 1] == "  Common: []":
            raise ValueError(f"{section}.Common is empty")
    end = start
    while end < len(lines) and re.match(r"\s*- ", lines[end]):
        end += 1
    return start, end, [re.match(r"\s*- (\S+)", line)[1] for line in lines[start:end]]

text = open(sys.argv[2], encoding="utf-8").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
added = []
for section, values in prefab_assets(sys.argv[1]).items():
    start, end, items = section_items(lines, section)
    indent = re.match(r"(\s*- )", lines[start])[1] if end > start else "- "
    new = [v for v in values if v not in items]
    lines[end:end] = [f"{indent}{v}" for v in new]
    added += [f"{section}: {v}" for v in new]
open(sys.argv[3], "w", encoding="utf-8", newline="").write(nl.join(lines))
print("\n".join(added) or "nothing to add")
