import unittest

from steps.layout import FRAME, HEADING_MARGIN, INSET, OVER_MASTERY, PAD, SKILLS, TITLE_BAR_H, TITLE_SCALE
from steps.panel import HEADING_H, Heading, content_box, heading_y

HEADING = Heading(None, "", HEADING_MARGIN)


class SectionHeading(unittest.TestCase):
    def test_section_headings_bar_top_is_the_frame_gap_below_the_panel_top(self):
        bar_h = TITLE_BAR_H * TITLE_SCALE
        for h in (SKILLS[3], OVER_MASTERY[3]):
            self.assertAlmostEqual(heading_y(h, HEADING), h / 2 - (INSET - FRAME) - bar_h)

    def test_titled_box_starts_under_the_heading(self):
        for _, _, w, h in (SKILLS, OVER_MASTERY):
            box = content_box(w, h, HEADING)
            self.assertAlmostEqual(box.top, heading_y(h, HEADING) - HEADING_H / 2)
            self.assertEqual((box.left, box.right, box.bottom), (-w / 2 + PAD, w / 2 - PAD, -h / 2 + PAD))


if __name__ == "__main__":
    unittest.main()
