import unittest

import legacy
from context import REPO, Context
from diff import diff

CARD = "root/loc_base01/loc_buildcard"
STATUS = "root/loc_base01/loc_status02/loc_chr_status01"


class Reset(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.ctx = Context(REPO)

    def stock(self):
        return self.ctx.stock("status01")

    def frozen(self):
        return legacy.frozen("status01")

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
