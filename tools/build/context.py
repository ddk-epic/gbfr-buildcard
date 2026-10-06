# The prefabs, stock prefabs and exports a build step works with.
import os

from model.prefab import Prefab

PREFABS = "gbfr.qol.buildcard/GBFR/data/ui/layouts/pause/status/prefabs"

# the prefabs the build writes, by name
TARGETS = {
    "status01": f"{PREFABS}/status01.prfb.yaml",
    "chr_status_bg01": f"{PREFABS}/chr_status_bg01.prfb.yaml",
}


class Context:
    def __init__(self, repo, stock=None):
        self.repo = repo
        self.stock_dir = stock
        self.prefabs = {}  # name: the Prefab being built
        self.exports = {}  # key: node, or nested lists of nodes
        self._stock = {}

    def prefab(self, name):
        return self.prefabs[name]

    def stock(self, name):
        # a fresh copy of the game's prefab, from <stock dir>/<name>.prfb.yaml
        if self.stock_dir is None:
            raise SystemExit(f"{name}: this build needs stock prefabs, pass --stock <dir>")
        if name not in self._stock:
            path = os.path.join(self.stock_dir, f"{name}.prfb.yaml")
            if not os.path.exists(path):
                raise SystemExit(f"{path} not found: convert the game's {name}.prfb with gbfr.uitools b-convert")
            with open(path, encoding="utf-8", newline="") as file:
                self._stock[name] = file.read()
        return Prefab.parse(self._stock[name])

    source = stock  # another prefab to copy objects from

    def export(self, key, node):
        if key in self.exports:
            raise KeyError(f"{key} exported twice")
        self.exports[key] = node
