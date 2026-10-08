import unittest

import powers
from model.prefab import Prefab, Ref
from tests.test_prefab import SMALL


def add_entry(prefab, node):
    start, found = powers.entries(prefab)
    end = start + sum(len(entry) for entry in found)
    prefab.root.lines[end:end] = ["      - ComponentName: ''", "        Index: -1", Ref("        ObjectRefId: ", node)]


class Order(unittest.TestCase):
    def test_stock_first_then_tree_order(self):
        prefab = Prefab.parse(SMALL)
        add_entry(prefab, prefab.find("b"))
        add_entry(prefab, prefab.find("a"))
        powers.order(prefab, Prefab.parse(SMALL))
        self.assertEqual([entry[-1].node.name for entry in powers.entries(prefab)[1]], ["text01", "a", "b"])
        self.assertEqual(Prefab.parse(prefab.text()).root.lines[-1], "  Active: true")


if __name__ == "__main__":
    unittest.main()
