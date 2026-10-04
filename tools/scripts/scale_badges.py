# Usage: python scale_badges.py <status01.prfb.yaml> <out.prfb.yaml>
# Scales the level, master level, PWR and name badges and moves them to their places on sharecard's card.
import sys
from prefab import Prefab
from card import S, card

p = Prefab(sys.argv[1])

LEVEL, MASTER_LEVEL, POWER, NAME = 21, 45, 17, 7  # level01, loc_ml_level01, power01, loc_name01
STATUS = 73  # loc_chr_status01

SCALE = 0.7

# sharecard pixels
LEVEL_AT = (100.5, 100)
MASTER_LEVEL_AT = (218.8, 80)
POWER_X = 80.8
COLUMN_X = 296
STATUS_TO_NAME, NAME_TO_POWER = 57, 76.5

POWER_DIGITS = (-2, 17)  # the PWR digits' centre from power01's pivot

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

status_top = 720 - (absolute(STATUS)[1] + p.vec(STATUS, "SizeDelta")[1] * p.vec(STATUS, "Scale")[1] / 2) / S
name_y = status_top - STATUS_TO_NAME
power_y = name_y - NAME_TO_POWER

for id_, at in ((LEVEL, LEVEL_AT), (MASTER_LEVEL, MASTER_LEVEL_AT)):
    scale(id_, SCALE)
    move(id_, *card(*at))

scale(POWER, SCALE)
move(POWER, *card(POWER_X - POWER_DIGITS[0] * SCALE, power_y - POWER_DIGITS[1] * SCALE))

scale(NAME, SCALE)
p.place(NAME, pivot=(0.5, 0.5))
move(NAME, *card(COLUMN_X, name_y))

p.save(sys.argv[2])
