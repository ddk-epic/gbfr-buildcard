# Last build step: each prefab's asset list, stock's entries, then the assets the prefab references, then the merged lists.
import re

# per-language sections, split into Common and one list per language
PER_LANGUAGE = ["TextureData", "AtlasData"]
ITEM = re.compile(r"\s*- (\S+)$")


def parse(text):
    # {(section, language or None): items}, in file order
    lists, section = {}, None
    for line in text.splitlines():
        if not line.startswith((" ", "-")):
            section = line.split(":")[0]
            if section not in PER_LANGUAGE:
                lists[section, None] = []
        elif line.startswith("  ") and not line.startswith("  - "):
            lists[section, line.strip().split(":")[0]] = []
        else:
            lists[next(reversed(lists))].append(ITEM.match(line)[1])
    return lists


def render(lists):
    lines, section = [], None
    for (name, language), items in lists.items():
        if language is not None:
            if name != section:
                lines.append(f"{name}:")
            section = name
            lines += [f"  {language}:" + (" []" if not items else ""), *(f"  - {item}" for item in items)]
        else:
            lines += [f"{name}:" + (" []" if not items else ""), *(f"- {item}" for item in items)]
    return "\n".join(lines) + "\n"


def referenced(prefab):
    # the assets the prefab's lines name, by list key
    found = []
    for node in prefab.walk():
        for line in node.lines:
            if not isinstance(line, str):
                continue
            m = re.match(r"\s*(?:- )?(\w+): (\S+)$", line)
            key, value = (m[1], m[2]) if m else (None, None)
            if key == "TexturePath" and not value.endswith("_dummy"):
                if not value.startswith("atlas/"):
                    found.append((("TextureData", "Common"), value))
                elif value.count("/") == 1:
                    found.append((("AtlasData", "Common"), value))
            elif key == "MaterialPath":
                found.append((("Materials", None), value))
            elif key == "AnimationPath":
                found.append((("Animations", None), value))
            elif key == "ImageDataPath":
                found.append((("ImageData", None), value))
            elif key == "LanguageData":
                found.append((("LanguageData", None), value))
            m = re.match(r"\s*- (data/image/\S+)$", line)
            if m:
                found.append((("ImageData", None), m[1]))
    return found


def build(stock, prefab, sources):
    # stock's list with the prefab's assets and the source lists' entries except animations appended
    lists = parse(stock)
    extra = referenced(prefab) + [(key, item) for source in sources for key, items in parse(source).items()
                                  if key[0] != "Animations" for item in items]
    for key, item in extra:
        if item not in lists[key]:
            lists[key].append(item)
    return render(lists)


def apply(ctx):
    for name in ctx.prefabs:
        ctx.lists[name] = build(ctx.stock_list(name), ctx.prefab(name),
                                [ctx.stock_list(source) for source in ctx.merged.get(name, [])])
