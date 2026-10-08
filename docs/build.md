# Prefab build

`tools/build/build.py` writes the mod's edited prefabs, `status01` and `chr_status_bg01`, and
`gbfr.qol.buildcard/Generated/CardIds.g.cs`, the Ids of the objects `CardWriter` sets. The outputs are committed, so
building the mod needs neither Python nor the game's files; the build steps are the source, and the outputs are never
edited by hand.

```
python tools/build/build.py --stock <dir>            # writes the outputs; <dir> holds the stock prefabs, see below
python tools/build/build.py --stock <dir> --check    # writes nothing; exits 1, with a diff, if an output is stale
python -m unittest discover -s tests -t .    # from tools/build
```

## How it builds

1. Each prefab starts from its frozen copy (`legacy.py`, see [Porting](#porting)), then each step in `build.py`'s
   `STEPS` changes it, in order. The last, `powers.py`, orders `CharaInfo.Powers`: stock's entries in stock order,
   then the mod's in tree order.
2. The prefabs are written with Ids in depth-first order, assigned only now: steps never see or use Ids.
3. `codegen.py` writes the exported objects' Ids to `CardIds.g.cs`, and fails if one isn't in `CharaInfo.Powers`,
   where `CardWriter` looks for it.

### Stock prefabs

Stock `status01` is read from commit 662eedd, which added it. The other stock prefabs come from `--stock`, a folder of
the game's prefabs converted to YAML, one file per prefab, named `<name>.prfb.yaml`:

```
gbfr.uitools.exe b-convert -i <extracted>/ui/layouts/pause/status/prefabs/summon_list01.prfb -o <stock>/summon_list01.prfb.yaml
```

Legacy loads `status01` and `chr_status_bg01` to reset ported paths, and steps load other prefabs (`summon_list01`,
`equip01_info01`, ...) to copy objects from. The build reads `--stock` only when a step asks, and says
which file is missing.

## Steps

A step is a module with `apply(ctx)`, listed in `STEPS`. `ctx` (`context.py`) has:

| | |
|---|---|
| `ctx.prefab("status01")` | the prefab being built |
| `ctx.stock(name)` | a fresh copy of a stock prefab |
| `ctx.export("SummonSlots", [node, ...])` | the nodes' Ids in `CardIds.g.cs`, as a constant or nested arrays |

Objects are `Node`s (`model/prefab.py`):

| | |
|---|---|
| `node.find("bc_mt_cells_captain/bc_mt_0_0_label")` | each name must match exactly one descendant, at any depth, or it raises: names repeat (766 in `status01`), so scope the search by an ancestor |
| `node.child(name)`, `node.children`, `node.parent` | |
| `node.add(child, index)`, `node.remove()`, `copy(node, refs)` | references are nodes, so adding, removing and moving objects needs no renumbering; `copy` remaps references inside the copied subtree, `refs` maps any outside it |
| `node.get(key)`, `node.vec(key)`, `node.set(key, value)`, `node.replace(old, new)` | top-level fields, and any nested line |
| `node.place(pos, size, pivot)`, `node.repin()`, `node.size()` | rect math, keeping the redundant rect fields consistent (see [status01.md](status01.md#file-format)) |
| `node.path`, `prefab.at(path)`, `prefab.paths()` | the unique path from the root's names, as `diff.py` reports them |

A step reads shared measurements (section rects in sharecard pixels, title scales) from constants, not from what an
earlier step left in the tree, so steps depend on each other only through the objects they build.

## Porting

The old one-off scripts in `tools/scripts/` changed the committed YAML in place and referred to objects by the Ids of
the moment. They are ported one section at a time:

| Prefab | Section | Status |
|---|---|---|
| `status01` | frame | `steps/status01/frame.py` |
| `status01` | Over Mastery | `steps/status01/over_mastery.py` |
| `status01` | summons | `steps/status01/summons.py` |
| `status01` | skills | `steps/status01/skills.py` |
| `status01` | status | `steps/status01/status.py` |
| `status01` | portrait | `steps/status01/portrait.py` |
| `status01` | gear | `steps/status01/gear.py` |
| `status01` | master traits | `steps/status01/master_traits.py` |
| `chr_status_bg01` | panel, backdrop | `steps/chr_status_bg01/backdrop.py` |

Until a section is ported, `legacy.py` carries it: each prefab starts as it was at the commit in `FROZEN`, and the
paths in `PORTED` are reset to stock (a subtree stock has) or removed (one the mod added). References into a reset
subtree follow to the object of the same path; one without takes stock's value of the same field and list index
(`CharaInfo.Weapon`, `Abilities`, `Gems`); a list entry stock doesn't have is dropped (the mod's `Powers` entries).

To port a section:

1. Write `tools/build/steps/<prefab>/<section>.py`, from the old scripts the edit logs name for it, finding objects
   by name and exporting what `CardWriter` sets. Add it to `STEPS`.
2. Add the section's paths to `PORTED`: its subtree under `loc_buildcard`, and the stock objects it changes
   (`python tools/build/diff.py <stock>/status01.prfb.yaml <committed status01>` lists all 70).
3. `python tools/build/build.py --stock <dir> --check`: a port that keeps the section as it was shows no differences.
   Then build, delete the old scripts it replaces, and commit with the check's output.

When `PORTED` holds everything, `legacy.py` and `tools/scripts/` go.

The edit logs ([status01-edits.md](status01-edits.md), [chr_status_bg01-edits.md](chr_status_bg01-edits.md)) end at
the frozen commit; the steps and their history replace them.
