# Prefab build

`tools/build/build.py` writes the mod's edited prefabs, `status01` and `chr_status_bg01`, their asset lists, and
`gbfr.qol.buildcard/Generated/CardIds.g.cs`, the Ids of the objects `CardWriter` sets. The outputs are committed, so
building the mod needs neither Python nor the game's files; the build steps are the source, and the outputs are never
edited by hand.

```
python tools/build/build.py --stock <dir>            # writes the outputs; <dir> holds the stock files, see below
python tools/build/build.py --stock <dir> --check    # writes nothing; exits 1, with a diff, if an output is stale
python -m unittest discover -s tests -t .    # from tools/build
```

## How it builds

1. Each prefab starts from stock (`start.py`, which also adds the card's container `loc_buildcard`), then each step
   in `build.py`'s `STEPS` changes it, in order. The last, `powers.py`, orders `CharaInfo.Powers`: stock's entries in
   stock order, then the mod's in tree order.
2. The prefabs are written with Ids in depth-first order, assigned only now: steps never see or use Ids.
3. `codegen.py` writes the exported objects' Ids to `CardIds.g.cs`, and fails if one isn't in `CharaInfo.Powers`,
   where `CardWriter` looks for it.
4. `lists.py` writes each prefab's asset list, the textures, atlases, materials, animations, image data and language
   data the game loads before building it: stock's list, then the assets the prefab names, then the stock lists a
   step merged with `ctx.merge_list`, except their animations. An asset missing from the list isn't loaded; a missing
   image data crashes the game.

### Stock files

The stock prefabs and asset lists come from `--stock`, a folder of the game's files converted to YAML, named
`<name>.prfb.yaml` and `<name>.list.yaml`:

```
gbfr.uitools.exe b-convert -i <extracted>/ui/layouts/pause/summon/prefabs/summon_list01.prfb -o <stock>/summon_list01.prfb.yaml
gbfr.uitools.exe b-convert -i <extracted>/ui/layouts/pause/status/prefabs/status01.list.listb -o <stock>/status01.list.yaml
```

The build reads `--stock` only when a step asks, and says which file is missing.

## Steps

A step is a module with `apply(ctx)`, listed in `STEPS`. `ctx` (`context.py`) has:

| | |
|---|---|
| `ctx.prefab("status01")` | the prefab being built |
| `ctx.stock(name)` | a fresh copy of a stock prefab |
| `ctx.export("SummonSlots", [node, ...])` | the nodes' Ids in `CardIds.g.cs`, as a constant or nested arrays |
| `ctx.merge_list("status01", "equip01_info02")` | merges a stock list into the prefab's asset list, for assets the game loads through code |

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

## Sections

| Prefab | Section | Step |
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
