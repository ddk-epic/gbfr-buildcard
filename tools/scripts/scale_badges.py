# Usage: python scale_badges.py <status01.prfb.yaml> <out.prfb.yaml>
# Scales the level, master level, PWR and name badges and moves them to their places on sharecard's card.
import sys
from prefab import Prefab
from card import S, card

p = Prefab(sys.argv[1])

LEVEL, MASTER_LEVEL, POWER, NAME = 21, 45, 17, 7  # level01, loc_ml_level01, power01, loc_name01
STATUS, SKILLS = 73, 275  # loc_chr_status01, loc_chr_status03
ELEMENT = 10  # loc_icon_elem01

SCALE = 0.7

# sharecard pixels
LEVEL_AT = (100.5, 100)
MASTER_LEVEL_AT = (218.8, 80)
POWER_X = 80.8
COLUMN_X = 296

POWER_DIGITS_X = -2  # the PWR digits' centre from power01's pivot
DIAMOND_PAD = 184 / 182  # transparent bottom row of the diamond sprite, in rect units

def absolute(id_):
    # the pivot in loc_base01 coordinates
    x, y = 0, 0
    while id_ != 2:
        px, py = p.vec(id_, "Position")[:2]
        x, y = x + px, y + py
        id_ = p.parents[id_]
    return x, y

def move(id_, x, y):
    # moves the pivot to (x, y) in loc_base01 coordinates
    ax, ay = absolute(id_)
    px, py = p.vec(id_, "Position")[:2]
    p.place(id_, pos=(px + x - ax, py + y - ay))

def scale(id_, s):
    p.set(id_, "Scale", (s, s, 1))

def half_height(id_, scale):
    return p.vec(id_, "SizeDelta")[1] * scale / 2

# name band: the status-skills gap plus the element icon's overhang above the status block; PWR diamond: the gap above the band
status_top = absolute(STATUS)[1] + half_height(STATUS, p.vec(STATUS, "Scale")[1])
status_bottom = absolute(STATUS)[1] - half_height(STATUS, p.vec(STATUS, "Scale")[1])
skills_top = absolute(SKILLS)[1] + half_height(SKILLS, p.vec(SKILLS, "Scale")[1])
gap = status_bottom - skills_top
overhang = half_height(ELEMENT, SCALE) - half_height(NAME, SCALE)
name_centre = status_top + gap + overhang + half_height(NAME, SCALE)
power_centre = name_centre + half_height(NAME, SCALE) + gap + half_height(POWER, SCALE) - DIAMOND_PAD * SCALE
name_y, power_y = 720 - name_centre / S, 720 - power_centre / S

for id_, at in ((LEVEL, LEVEL_AT), (MASTER_LEVEL, MASTER_LEVEL_AT)):
    scale(id_, SCALE)
    move(id_, *card(*at))

scale(POWER, SCALE)
move(POWER, *card(POWER_X - POWER_DIGITS_X * SCALE, power_y))

scale(NAME, SCALE)
p.place(NAME, pivot=(0.5, 0.5))
move(NAME, *card(COLUMN_X, name_y))

p.save(sys.argv[2])
