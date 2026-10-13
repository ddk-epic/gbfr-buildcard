# Usage: python -m unittest discover -s tests -t .   (from tools/build)
# Tests CardLayout.g.cs generation: sections, records, hashes and the prefab hash.
import unittest

import codegen
from codegen import Hash, Record
from model.prefab import Prefab

SMALL = """Objects:
- Id: 0
  Name: status01
  Children:
  - 1
  Active: true
- Id: 1
  Name: a
  Active: true
"""


class NamedNode:
    def __init__(self, name):
        self.name = name


class CodegenTest(unittest.TestCase):
    def setUp(self):
        self.prefab = Prefab.parse(SMALL)
        self.a = self.prefab.find("a")

    def test_prefab_hash_matches_the_runtime(self):
        # UiTreeTests.PrefabHashMatchesTheBuild asserts the same value
        self.assertEqual(codegen.prefab_hash({NamedNode("status01"): 0, NamedNode("a"): 1}), 0xBB11DC59)

    def test_prefab_hash_unquotes_names(self):
        quoted = codegen.prefab_hash({NamedNode("'loc_ring '"): 0, NamedNode("'it''s'"): 1})
        self.assertEqual(quoted, codegen.prefab_hash({NamedNode("loc_ring "): 0, NamedNode("it's"): 1}))

    def test_name_hash_unquotes_the_name(self):
        self.assertEqual(codegen.name_hash(NamedNode("'status01'")), codegen.name_hash(NamedNode("status01")))

    def test_constants_take_the_smallest_fitting_type(self):
        text = codegen.generate(self.prefab, {"Big": 0x80000000, "On": True})
        self.assertIn("    public const uint Big = 2147483648;", text)
        self.assertIn("    public const bool On = true;", text)

    def test_sections_become_nested_classes(self):
        text = codegen.generate(self.prefab, {"Card.Width": 10, "Root": self.a, "Card.Name": Hash(0x887AE0B0)})
        self.assertIn("    public const short Root = 1;", text)
        self.assertIn("    public static class Card\n    {\n", text)
        self.assertIn("        public const uint Name = 0x887AE0B0;", text)
        self.assertIn("        public const int Width = 10;", text)
        self.assertIn("        public const int ObjectCount = 2;", text)

    def test_records_become_record_structs(self):
        cell = Record("Board.Cell", On=self.a, Picked=[self.a, self.a], Scale=0.5)
        text = codegen.generate(self.prefab, {"Board.Cells": [cell, cell]})
        self.assertIn("public readonly record struct Cell(short On, short[] Picked, float Scale);", text)
        self.assertIn("public static readonly Cell[] Cells = [new(1, [1, 1], 0.5f), new(1, [1, 1], 0.5f)];", text)

    def test_records_of_one_type_must_match(self):
        with self.assertRaises(TypeError):
            codegen.generate(self.prefab, {"Cells": [Record("Cell", On=self.a), Record("Cell", On=1)]})

    def test_objects_must_be_in_status01(self):
        other = Prefab.parse(SMALL).find("a")
        with self.assertRaises(KeyError):
            codegen.generate(self.prefab, {"Other": other})


if __name__ == "__main__":
    unittest.main()
