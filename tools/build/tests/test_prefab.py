# Usage: python -m unittest discover -s tests -t .   (from tools/build)
# Tests the prefab model's parsing, paths, tree edits and component edits.
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


TEXT = """Objects:
- Id: 0
  Name: root
  Children:
  - 1
  Components:
  - ComponentName: Info
    Component:
      Names:
      - ComponentName: Text
        Index: 0
        ObjectRefId: 1
      Level: 3
      Enable: true
  Active: true
- Id: 1
  Name: label
  Components:
  - ComponentName: Text
    Component:
      Color: 1, 1, 1, 1
      FontSize: 40
      Enable: true
  - ComponentName: LanguageSetter
    Component:
      ContainerData:
      - data/language/a
      - data/language/b
      Overwrites:
      - Language: Eng
        FontSize: 0
      - Language: Jpn
        FontSize: 36
      Enable: true
  Active: true
  SizeDelta: 0, 0
"""


class Components(unittest.TestCase):
    def setUp(self):
        self.p = Prefab.parse(TEXT)
        self.label = self.p.find("label")

    def test_names_and_lookup(self):
        self.assertEqual(self.label.components(), ["Text", "LanguageSetter"])
        self.assertEqual(self.label.component("Text").get("FontSize"), "40")
        with self.assertRaises(KeyError):
            self.label.component("Image")

    def test_set_is_scoped_to_the_component(self):
        self.label.component("LanguageSetter").set("FontSize", 20)
        self.assertEqual(self.label.component("Text").get("FontSize"), "40")
        self.assertIn("      - Language: Eng\n        FontSize: 20", self.p.text())

    def test_update_every_line(self):
        self.label.component("LanguageSetter").update("FontSize", lambda old: "30" if float(old) else old)
        self.assertIn("FontSize: 0\n      - Language: Jpn\n        FontSize: 30", self.p.text())

    def test_items(self):
        language = self.label.component("LanguageSetter")
        self.assertEqual(language.items("ContainerData"), ["data/language/a", "data/language/b"])
        language.set_items("ContainerData", ["data/language/b"])
        self.assertIn("ContainerData:\n      - data/language/b\n      Overwrites:", self.p.text())

    def test_add_drop_keep(self):
        self.label.add_component(["  - ComponentName: Mask", "    Component:", "      Enable: true"])
        self.assertEqual(self.label.components(), ["Text", "LanguageSetter", "Mask"])
        self.label.drop_component("LanguageSetter")
        self.assertEqual(self.label.components(), ["Text", "Mask"])
        self.label.keep_components("Mask")
        self.assertEqual(self.label.components(), ["Mask"])
        self.assertIn("      Enable: true\n  Active: true", self.p.text())

    def test_add_to_an_object_without_components(self):
        node = Node(["  Name: new", "  Active: true"])
        node.add_component(["  - ComponentName: Mask", "    Component:", "      Enable: true"])
        self.assertEqual(node.lines[:3], ["  Name: new", "  Components:", "  - ComponentName: Mask"])

    def test_fields(self):
        info = self.p.root.component("Info")
        self.assertEqual(len(info.field("Names")), 4)
        info.insert(["      Plus: 1"], after="Names")
        info.insert(["      Minus: 1"], before="Names")
        self.assertIn("      Minus: 1\n      Names:", self.p.text())
        self.assertIn("ObjectRefId: 1\n      Plus: 1\n      Level: 3", self.p.text())

    def test_refs(self):
        info = self.p.root.component("Info")
        other = self.p.root.add(Node(["  Name: other", "  Active: true"]))
        info.set_refs("Names", [other])
        self.assertIs(self.p.root.refs()[0].node, other)
        info.drop_ref(other)
        self.assertEqual(info.field("Names"), ["      Names:"])

    def test_component_lines_refuse_references(self):
        self.assertEqual(self.label.component_lines()[0], "  - ComponentName: Text")
        with self.assertRaises(ValueError):
            self.p.root.component_lines()


if __name__ == "__main__":
    unittest.main()
