import unittest

from model.prefab import Node
from steps.layout import insert

ORDER = ["a", "b", "c"]


def node(name):
    return Node([f"  Name: {name}"])


class Insert(unittest.TestCase):
    def test_any_order_gives_the_slot_order(self):
        for names in (["a", "b", "c"], ["c", "b", "a"], ["b", "c", "a"]):
            parent = node("p")
            for name in names:
                insert(parent, node(name), ORDER)
            self.assertEqual([c.name for c in parent.children], ORDER)

    def test_unslotted_child_raises(self):
        parent = node("p")
        parent.add(node("x"))
        with self.assertRaises(KeyError):
            insert(parent, node("a"), ORDER)


if __name__ == "__main__":
    unittest.main()
