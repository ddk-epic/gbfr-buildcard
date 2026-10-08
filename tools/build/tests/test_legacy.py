import subprocess
import unittest

import legacy
from context import REPO, TARGETS, Context
from diff import diff
from model.prefab import Prefab

STOCK_COMMIT = "662eedd"  # Add stock `status01` prefab
CARD = "root/loc_base01/loc_buildcard"
STATUS = "root/loc_base01/loc_status02/loc_chr_status01"


def git_show(rev, path):
    return subprocess.run(["git", "show", f"{rev}:{path}"], cwd=REPO, check=True, capture_output=True,
                          encoding="utf-8").stdout


class Reset(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.stock_text = git_show(STOCK_COMMIT, TARGETS["status01"])

    def stock(self):
        return Prefab.parse(self.stock_text)

    def frozen(self):
        return legacy.frozen(Context(REPO), "status01")

    def test_added_subtree_is_removed_with_its_powers(self):
        prefab = self.frozen()
        legacy.reset(prefab, self.stock(), [CARD])
        prefab.text()  # no dangling references
        report = diff(self.frozen(), prefab)
        self.assertEqual(report[0], "- root/loc_base01/loc_buildcard (2518 objects)")
        changed = [line for line in report if line.startswith("~")]
        self.assertEqual(changed, ["~ (root)", "~ root/loc_base01"])  # the Powers entries, and the child
        # CharaInfo.Weapon, Abilities and Gems back on stock's objects
        added = [line for line in report if line.startswith("    +") and "ObjectRefId" in line]
        self.assertEqual(len(added), 17, added)
        self.assertTrue(all("ObjectRefId: -> root/loc_base01/loc_status02/" in line for line in added), added)

    def test_stock_subtree_is_restored(self):
        prefab = self.frozen()
        legacy.reset(prefab, self.stock(), [STATUS])
        prefab.text()
        self.assertEqual(diff(self.stock(), prefab, STATUS), [])
        # the rest is untouched
        rest = [line for line in diff(self.frozen(), prefab) if line.startswith(("~", "-", "+"))]
        self.assertTrue(all(line.split()[1].startswith(STATUS) for line in rest), rest)

    def test_everything_ported_is_stock(self):
        # resetting every changed path leaves stock
        frozen = self.frozen()
        stock = self.stock()
        changed = [line[2:] for line in diff(stock, frozen) if line.startswith("~ ") and line != "~ (root)"]
        paths = [CARD] + [p for p in changed if not any(p.startswith(q + "/") for q in changed if q != p)]
        legacy.reset(frozen, stock, paths)
        self.assertEqual(diff(stock, frozen), [])


if __name__ == "__main__":
    unittest.main()
