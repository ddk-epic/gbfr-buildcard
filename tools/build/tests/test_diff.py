import unittest

from diff import diff
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


if __name__ == "__main__":
    unittest.main()
