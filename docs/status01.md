# `status01` prefab

The main page of the in-game **Character Details** panel (pause menu > character > details). Everything on that page except the background, the Q/E guide and the other pages is in this one prefab.

| | |
|---|---|
| Game path | `data/ui/layouts/pause/status/prefabs/status01.prfb` |
| Repo source | `gbfr.qol.buildcard/GBFR/data/ui/layouts/pause/status/prefabs/status01.prfb.yaml`, written by `tools/build/build.py` ([build.md](build.md)) |
| Edit log | [status01-edits.md](status01-edits.md), frozen at 5f5f456 |
| Placed by | view `pause_status01.view.viewb`, full screen, centred |
| Objects | 426 stock (Ids 0-425) + 2518 added (Ids 426-2943), see the edit log |
| Size | 20,689 lines stock |

The other pages and parts of the panel are separate prefabs: `chr_status_bg01` (white panel, blue portrait area, frame lines), `status_guide01` (Q/E arrows), `var00_chr_skill_info01` (skill details), `chr_skill_info01/02` (trait details, command list), `chr_sboard_info01` (master traits).

## File format

The YAML is GBFRDataTools' rendering of the binary prefab (`gbfr.uitools.exe b-convert`). It is one list, `Objects`, of Unity-style UI objects:

```yaml
Objects:
- Id: 17
  Name: power01
  Children:          # Ids of child objects, in draw order (later draws on top)
  - 18
  Components:        # zero or more
  - ComponentName: Image
    Component:
      Color: 1, 1, 1, 1
      ...
  Active: true       # inactive objects and their children are not drawn
  Position: 1216, 748, 0
  Rotation: 0, 0, 0, 1
  Scale: 1, 1, 1
  Pivot: 0.5, 0.5
  AnchorPoint: 1216, -194
  AnchorMin: 0, 1
  AnchorMax: 0, 1
  OffsetMin: 1124, -286
  OffsetMax: 1308, -102
  SizeDelta: 184, 184
```

- **Ids** are the object's index in the list and are in depth-first order: a parent comes before its children, and a subtree occupies a contiguous Id range. New objects go after the last object of the subtree they join. Moving an object to another parent would break the order; move it by changing its rect instead.
- **Coordinates** are 4K (3840x2160), y up. The rect fields are redundant and must stay consistent:
  - `AnchorMin`/`AnchorMax`: anchor rectangle as fractions of the parent's size (equal = a point).
  - `AnchorPoint`: the pivot's position relative to the anchor point.
  - `Position`: the pivot's position relative to the parent's centre.
  - `OffsetMin`/`OffsetMax`: the rect's corners relative to the anchors, `AnchorPoint` -/+ `Pivot` x `SizeDelta`.
  - `SizeDelta`: the size (for stretched anchors, the size minus the anchor rectangle).
  - `Scale` scales around the pivot and does not change the offsets.
- **Object references** in components are `{ComponentName, Index, ObjectRefId}`: the object Id, and which of its components (`ComponentName: ''`, `Index: -1` = the object itself).

### Where the lines go

| Lines | What |
|---|---|
| 6,541 | 426 objects x name, children and 10 rect fields |
| 5,174 | `LanguageSetter`: per-language overrides of font size and spacing, up to 128 lines per text |
| 3,032 | ~180 `Image` |
| 1,656 | 144 `Text` |
| ~4,300 | the rest: `ImageSetter`, `Animator`, icon setters, layout groups, `Shortcut`, ... |

### Components

| Component | Does |
|---|---|
| `Image` | Draws a sprite (`Sprite: {TexturePath, SpriteName}`), or a solid rect in `Color` when it has no sprite. `Type: 1` = 9-sliced. |
| `Text` | Draws text (font, size, colour, alignment); `Text` itself is empty in the file. Labels come from a `TextSetter`, values are written by the game at runtime. |
| `LanguageSetter` | Per-language text style: a style file (`LanguageData: data/language/ld_...`) and overrides (font size, spacing, ...). |
| `TextSetter` | Sets a static label by `TextID` (e.g. `TXT_PAU_LEVEL`). |
| `ImageSetter` | Picks a sprite for its target `Image` from an image set (`ImageDataPath`, e.g. `data/image/elementicon`). |
| `NumSetter` | Draws a number with digit sprites (`lv_num100/010/001`): Lvl and Master Lvl are images, not text. |
| `Mask` | Clips its children to a sprite. |
| `CanvasGroup` | Alpha for a subtree (the animations fade with it). |
| `HorizontalLayoutGroup`, `ContentSizeFitter`, `LayoutElement` | Automatic layout: these objects' rects are recomputed at runtime. |
| `Animator`, `AnimationHandle` | Open/close/loop animations (`layouts/pause/status/animations/...`). |
| `Shortcut` | A button binding (`ButtonType`). |
| `*Info` (`CharaInfo`, `WeaponInfo`, `GemInfo`, `AbilityInfo`, `UniqueSkillInfo`, `ElementInfo`, ...) | Data binding: lists the objects the game fills with one kind of data. |
| `*IconSetter` | Picks the icon sprite for a character, weapon, sigil, skill, ... |
| `ControllerStatus`, `ControllerCharaStatus02` | The page's controllers. |

## Data binding

The game fills the page through the components on the root object. **An object referenced here must not be removed**; hiding (`Active: false`), moving and scaling are safe.

`CharaInfo` (on `status01`, Id 0):

| Field | Objects |
|---|---|
| `Icons` | 4 `chr_img01`, 5 `chr_img01_mask` (portrait) |
| `Names` | 12 `name01_01`, 15 `name01_02` |
| `Elements` | 10 `loc_icon_elem01`, 83 `loc_elem01` |
| `Icon` | 81 `chr_icon01` (small portrait in Status) |
| `Level` | 21 `level01` (Lvl badge, `ItemLevel`) |
| `_D47C490A` | 46 `masterlevel02` (Master Lvl badge) |
| `Powers` | 20 `pwr_text01` (PWR) |
| `HpMaxs`, `Attacks`, `Criticals`, `Breaks` | 91 `hp_num01`, 96 `atk_num01`, 102 `crt_num01`, 108 `brk_num01` |
| `Weapon` | 114 `chr_status02_p01` (`WeaponInfo`, binds the weapon row) |
| `Gem` | 143, 154, ..., 264: the 12 sigil rows (`GemInfo` each) |
| `Ability` | 281 `btn04`, 303 `btn03`, 325 `btn01`, 347 `btn02`: the 4 skills (`AbilityInfo` each) |

`ControllerStatus` (also on Id 0) refers to the portrait (4, 5), `loc_name02` (13), `name01_02` (15) and the support skill objects (374, 376-425). `CrossPlayInfo` refers to `pltfm_icon01` (14).

## Tree

Ids, names and components; `(inactive)` marks `Active: false` in the stock file. Repeated subtrees are shown once.

```
0 status01                      Animator, ControllerStatus, CharaInfo, SoundContainer,
│                               AnimationHandle, Shortcut x8, CrossPlayInfo
└─ 1 root (inactive)            turned on by the open animation
   └─ 2 loc_base01              3424x1884 panel area, centred at y=+74
      ├─ 3 loc_chr              portrait
      │  ├─ 4 chr_img01                 Image, Dissolve, CharaIconSetter
      │  └─ 5 chr_img01_mask            Mask, CharaIconSetter
      │     └─ 6 chr_img01_col          Image
      ├─ 7 loc_name01           name badge   Image, layout
      │  ├─ 8 loc_name01_icon
      │  │  └─ 9 name01_icon (inactive)
      │  ├─ 10 loc_icon_elem01          ElementInfo
      │  │  └─ 11 icon_elem01           element icon
      │  └─ 12 name01_01                Text: character name
      ├─ 13 loc_name02 (inactive)       second name line (cross-play)
      │  ├─ 14 pltfm_icon01 (inactive)  platform icon
      │  └─ 15 name01_02                Text
      ├─ 16 loc_status01        badges
      │  ├─ 17 power01                  PWR badge   Image
      │  │  ├─ 18 pwr_icon01
      │  │  ├─ 19 pwr_text02            Text: "PWR" (TXT_PAU_STRENGTH)
      │  │  └─ 20 pwr_text01            Text: PWR value
      │  ├─ 21 level01                  Lvl badge   ItemLevel
      │  │  └─ 22 root
      │  │     └─ 23 loc_level01
      │  │        ├─ 24 loc_exp01       exp bar, "Next" value (25-31),
      │  │        │                     ticket (32-38, inactive)
      │  │        └─ 39 loc_level01
      │  │           ├─ 40 lv_text01    Text: "Lvl" (TXT_PAU_LEVEL)
      │  │           └─ 41 loc_lv_num   NumSetter: digits 42-44
      │  └─ 45 loc_ml_level01 (inactive)  Master Lvl badge, shown when unlocked
      │     ├─ 46 masterlevel02         Animator, MasterLevelSetter
      │     │  └─ 47-70                 base, effects, NumSetter digits 67-68, rank stars 70
      │     └─ 71 lv_text01             Text: "Master Lvl" (TXT_PAU_MASTER_LV)
      ├─ 72 loc_status02        right column, anchored at x=+640 in loc_base01
      │  ├─ 73 loc_chr_status01         STATUS block, 2032x256
      │  │  └─ 74 chr_status01
      │  │     └─ 75 root
      │  │        └─ 76 status_base01   Image (block background)
      │  │           ├─ 77 status_ttl_text01   Text: "Status" (TXT_PAU_CHECK_STATUS)
      │  │           └─ 78 loc_status01
      │  │              ├─ 79 loc_chr_icon01   small portrait 81, element 82-85
      │  │              ├─ 86 line01
      │  │              ├─ 87 loc_hp    icon 88, label 89, dots 90, value 91
      │  │              ├─ 92 loc_atk   icon 93, label 94, dots 95, value 96
      │  │              ├─ 97 loc_crt   icon 98, label 99, dots 100, value 101-103 (number + "%")
      │  │              └─ 104 loc_brk  icon 105, label 106, dots 107, value 108
      │  ├─ 109 loc_chr_status02        GEAR block, 2032x584
      │  │  └─ 110 chr_status02         AnimationHandle, ControllerCharaStatus02
      │  │     └─ 111 root
      │  │        └─ 112 status_base01  Image
      │  │           ├─ 113 ttl01_text01         Text: "Gear" (TXT_PAU_EQUIP)
      │  │           ├─ 114 chr_status02_p01     weapon row   WeaponInfo
      │  │           │  └─ 115 root
      │  │           │     ├─ 116 loc_equip01    icon 118, name 120, "+99" 121-124,
      │  │           │     │                     "Lvl" 126, level 127
      │  │           │     ├─ 128 loc_hp         icon 129, value 130
      │  │           │     ├─ 131 loc_atk        icon 132, value 133
      │  │           │     ├─ 134 loc_crt        icon 135, value 137-138
      │  │           │     └─ 139 loc_brk        icon 140, value 141
      │  │           └─ 142 loc_status01         sigil rows   CanvasGroup, Image
      │  │              └─ 143 chr_status02_p02_01   sigil row   GemInfo   (x12: Ids 143-274, 11 each)
      │  │                 └─ 144 root
      │  │                    ├─ 145 line01          separator (inactive on rows 11-12)
      │  │                    ├─ 146 icon01          sigil icon
      │  │                    ├─ 147 icon02 (inactive)
      │  │                    ├─ 148 loc_text01
      │  │                    │  ├─ 149 loc_text01_01   name 150, number 151 (inactive)
      │  │                    │  ├─ 152 text01_02       Text: "Lvl"
      │  │                    │  └─ 153 text01_03       Text: level value
      │  ├─ 275 loc_chr_status03        SKILLS block, 2032x352
      │  │  └─ 276 chr_status03
      │  │     └─ 277 root
      │  │        └─ 278 status_base01  Image
      │  │           ├─ 279 ttl01_text01         Text: "Skills" (TXT_PAU_ABILITY)
      │  │           └─ 280 loc_status01         2x2 grid, in order btn04, btn03, btn01, btn02
      │  │              └─ 281 chr_status03_p01_btn04   skill   AbilityInfo, DeviceObjSetter,
      │  │                 │                    KeyConfigSetter   (x4: Ids 281-368, 22 each)
      │  │                 └─ 282 root
      │  │                    └─ 283 loc_base01           bar background
      │  │                       ├─ 284 base01_set01 (inactive)   skill icon 286
      │  │                       ├─ 287 loc_guide_button          button prompts 288-295
      │  │                       ├─ 296 loc_guide_button_key (inactive)   keyboard prompt
      │  │                       ├─ 301 icon_elem01               element icon
      │  │                       └─ 302 text01                    skill name
      │  └─ 369 loc_chr_status04        SUPPORT SKILLS block, 2032x488
      │     └─ 370 chr_status04
      │        └─ 371 root
      │           └─ 372 status_base01  Image
      │              ├─ 373 ttl01_text01         Text: "Support Skills" (TXT_PAU_SKILL)
      │              ├─ 374 text_empty (inactive)
      │              └─ 375 loc_status01
      │                 └─ 376 chr_status04_p02_01   support skill   UniqueSkillInfo
      │                    │                     (x2: Ids 376-425, 25 each)
      │                    └─ 377 root
      │                       ├─ 378 loc_base00 (inactive)   text 379
      │                       └─ 380 loc_base01              name 382, description 383,
      │                                                       update icons 384-400 (inactive)
      └─ 426 loc_buildcard      added: the build card, 3424x1712
         ├─ 427 bc_mtraits      master traits
         ├─ 1753 bc_om          Over Mastery
         ├─ 1883 bc_smn         summons
         ├─ 2089 bc_weapon      weapon
         ├─ 2693 bc_skills      skills
         ├─ 2929 bc_om_ttl_text Over Mastery title
         └─ 2930 bc_frame       the ornate frame over the card
```
