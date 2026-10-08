import unittest

import powers
from model.prefab import Prefab
from tests.test_prefab import SMALL


class Order(unittest.TestCase):
    def test_stock_first_then_tree_order(self):
        prefab = Prefab.parse(SMALL)
        powers.add(prefab, prefab.find("b"))
        powers.add(prefab, prefab.find("a"), "Text")
        powers.order(prefab, Prefab.parse(SMALL))
        self.assertEqual([entry[-1].node.name for entry in powers.entries(prefab)[1]], ["text01", "a", "b"])
        self.assertEqual(Prefab.parse(prefab.text()).root.lines[-1], "  Active: true")


if __name__ == "__main__":
    unittest.main()
