# Usage: python -m unittest discover -s tests -t .   (from tools/build)
# Tests the prefab model's parsing, paths and tree edits.
import os
import unittest

from model.prefab import Node, Prefab, Ref, copy

PREFABS = os.path.join(os.path.dirname(__file__), "../../../gbfr.qol.buildcard/GBFR/data/ui/layouts/pause/status/prefabs")

SMALL = """Objects:
- Id: 0
  Name: root
  Children:
  - 1
  - 3
  Components:
  - ComponentName: CharaInfo
    Component:
      Powers:
      - ComponentName: Text
        Index: 0
        ObjectRefId: 2
  Active: true
- Id: 1
  Name: a
  Children:
  - 2
  Active: true
- Id: 2
  Name: text01
  Active: true
- Id: 3
  Name: b
  Active: true
"""


class RoundTrip(unittest.TestCase):
    def test_repo_prefabs(self):
        for name in ["status01.prfb.yaml", "chr_status_bg01.prfb.yaml"]:
            with self.subTest(name):
                with open(os.path.join(PREFABS, name), encoding="utf-8", newline="") as file:
                    text = file.read()
                prefab = Prefab.parse(text)
                self.assertEqual(prefab.text(), text)
                self.assertTrue(all(n.source_id == i for n, i in prefab.ids().items()))

    def test_paths_unique(self):
        prefab = Prefab.load(os.path.join(PREFABS, "status01.prfb.yaml"))
        paths = prefab.paths()
        self.assertEqual(len(set(paths.values())), len(paths))
        for node, path in paths.items():
            self.assertIs(prefab.at(path), node)
        node = prefab.find("bc_mt_cells_captain/bc_mt_0_0_label")
        self.assertEqual(node.path, paths[node])


class Tree(unittest.TestCase):
    def setUp(self):
        self.p = Prefab.parse(SMALL)

    def test_insert_renumbers_refs(self):
        a = self.p.find("a")
        a.add(Node(["  Name: new", "  Active: true"]), index=0)
        text = self.p.text()
        self.assertIn("- Id: 2\n  Name: new", text)
        self.assertIn("ObjectRefId: 3", text)  # text01 moved from 2 to 3

    def test_find_requires_unique(self):
        self.p.find("b").add(Node(["  Name: text01", "  Active: true"]))
        with self.assertRaises(KeyError):
            self.p.find("text01")
        self.assertEqual(self.p.find("a/text01").path, "a/text01")

    def test_removed_target_is_an_error(self):
        self.p.find("text01").remove()
        with self.assertRaises(KeyError):
            self.p.text()

    def test_copy_remaps_inner_refs(self):
        a = self.p.find("a")
        a.lines.append(Ref("  ObjectRefId: ", a.children[0]))
        dup = copy(a)
        dup.name = "a2"
        self.p.root.add(dup)
        self.assertIs(dup.refs()[0].node, dup.children[0])
        self.assertIs(a.refs()[0].node, a.children[0])
        self.assertIn("ObjectRefId: 5", self.p.text())  # a2's ref, at a2/text01

    def test_copy_outside_ref(self):
        p = Prefab.parse(SMALL)
        b = p.find("b")
        b.lines.append(Ref("  ObjectRefId: ", p.find("text01")))
        with self.assertRaises(KeyError):
            copy(b)
        replacement = Node(["  Name: other"])
        self.assertIs(copy(b, refs={p.find("text01"): replacement}).refs()[0].node, replacement)


if __name__ == "__main__":
    unittest.main()
