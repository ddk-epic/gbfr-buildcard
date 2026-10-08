import unittest

from diff import diff, revision
from model.prefab import Node, Prefab
from tests.test_prefab import SMALL


class Diff(unittest.TestCase):
    def test_same(self):
        self.assertEqual(diff(Prefab.parse(SMALL), Prefab.parse(SMALL)), [])

    def test_renumbering_is_not_a_difference(self):
        new = Prefab.parse(SMALL)
        new.root.children.reverse()
        report = diff(Prefab.parse(SMALL), new)
        # only the child order differs
        self.assertEqual(report[0], "~ (root)")
        self.assertTrue(all("child" in line for line in report[1:]))

    def test_added_removed_changed(self):
        new = Prefab.parse(SMALL)
        new.find("b").add(Node(["  Name: c", "  Active: true"])).add(Node(["  Name: d", "  Active: true"]))
        new.find("text01").set("Active", False)
        new.find("a").remove()
        new.root.lines = [line for line in new.root.lines if not hasattr(line, "node")]
        report = diff(Prefab.parse(SMALL), new)
        self.assertIn("- a (2 objects)", report)
        self.assertIn("+ b/c (2 objects)", report)
        self.assertNotIn("+ b/c/d (1 objects)", report)

    def test_tolerance(self):
        old, new = Prefab.parse(SMALL), Prefab.parse(SMALL)
        old.find("b").lines.append("  Position: 1.001, 2, 0")
        new.find("b").lines.append("  Position: 1.002, 2, 0")
        self.assertEqual(diff(old, new, tolerance=0.002), [])
        self.assertEqual(diff(old, new), ["~ b", "    -  Position: 1.001, 2, 0", "    +  Position: 1.002, 2, 0"])
        new.find("b").lines[-1] = "  Position: 1.004, 2, 0"
        self.assertEqual(diff(old, new, tolerance=0.002)[0], "~ b")

    def test_revision(self):
        # the commit that added the summon cells' panel
        self.assertEqual(revision("9804f35"), {"status01": [
            "+ root/loc_base01/loc_buildcard/bc_smn/bc_smn_base (1 objects)",
            "~ root/loc_base01/loc_buildcard/bc_smn",
            "    +  child bc_smn_base"]})


if __name__ == "__main__":
    unittest.main()
