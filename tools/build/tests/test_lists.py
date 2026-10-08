import os
import unittest

import lists
from context import PREFABS, REPO
from model.prefab import Prefab

SMALL = """Objects:
- Id: 0
  Name: root
  Components:
  - ComponentName: Image
    Component:
      Sprite:
        TexturePath: layouts/pause/status/noatlastextures/bc_white
        SpriteName: bc_white
      Enable: true
  - ComponentName: ImageSetter
    Component:
      ImageDataPath: data/image/elementicon
      Enable: true
  Active: true
"""

STOCK = """TextureData:
  Common:
  - layouts/pause/status/noatlastextures/bc_white
  Eng: []
Materials: []
Animations: []
ImageData: []
"""


class Lists(unittest.TestCase):
    def test_committed_lists_round_trip(self):
        for name in ("status01", "chr_status_bg01"):
            with open(os.path.join(REPO, PREFABS, f"{name}.list.yaml"), encoding="utf-8") as file:
                text = file.read()
            self.assertEqual(lists.render(lists.parse(text)), text)

    def test_build_appends_new_assets_and_source_entries_except_animations(self):
        source = "Materials:\n- material/uidissolve\nAnimations:\n- layouts/x/animations/a\nImageData: []\n"
        built = lists.parse(lists.build(STOCK, Prefab.parse(SMALL), [source]))
        self.assertEqual(built["TextureData", "Common"], ["layouts/pause/status/noatlastextures/bc_white"])
        self.assertEqual(built["ImageData", None], ["data/image/elementicon"])
        self.assertEqual(built["Materials", None], ["material/uidissolve"])
        self.assertEqual(built["Animations", None], [])


if __name__ == "__main__":
    unittest.main()
