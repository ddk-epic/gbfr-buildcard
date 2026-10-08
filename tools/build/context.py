# The prefabs, stock prefabs and exports a build step works with.
import os
import subprocess

import powers
from model.prefab import Prefab

REPO = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "../.."))
PREFABS = "gbfr.qol.buildcard/GBFR/data/ui/layouts/pause/status/prefabs"

# the prefabs the build writes, by name
TARGETS = {
    "status01": f"{PREFABS}/status01.prfb.yaml",
    "chr_status_bg01": f"{PREFABS}/chr_status_bg01.prfb.yaml",
}

def git_show(rev, path):
    # the file at a revision, or None where it does not exist
    result = subprocess.run(["git", "show", f"{rev}:{path}"], cwd=REPO, capture_output=True, encoding="utf-8")
    return result.stdout if result.returncode == 0 else None


class Context:
    def __init__(self, repo, stock=None):
        self.repo = repo
        self.stock_dir = stock
        self.prefabs = {}  # name: the Prefab being built
        self.exports = {}  # key: node, or nested lists of nodes
        self.lists = {}  # name: the asset list's text
        self.merged = {}  # name: the stock lists merged into its asset list
        self._stock = {}

    def prefab(self, name):
        return self.prefabs[name]

    def stock(self, name):
        # a fresh copy of the game's prefab
        if name not in self._stock:
            self._stock[name] = self.stock_file(f"{name}.prfb")
        return Prefab.parse(self._stock[name])

    def stock_list(self, name):
        # the game's asset list of the prefab
        return self.stock_file(f"{name}.list")

    def stock_file(self, name):
        # <stock dir>/<name>.yaml
        if self.stock_dir is None:
            raise SystemExit(f"{name}: this build needs stock files, pass --stock <dir>")
        path = os.path.join(self.stock_dir, f"{name}.yaml")
        if not os.path.exists(path):
            raise SystemExit(f"{path} not found: convert the game's {name}b with gbfr.uitools b-convert")
        with open(path, encoding="utf-8") as file:
            return file.read()

    def merge_list(self, name, source):
        # merges the source prefab's asset list, except animations, into the prefab's
        self.merged.setdefault(name, []).append(source)

    def powers(self, node, component=""):
        # adds the node to status01's CharaInfo.Powers
        powers.add(self.prefab("status01"), node, component)

    def export(self, key, node):
        if key in self.exports:
            raise KeyError(f"{key} exported twice")
        self.exports[key] = node
