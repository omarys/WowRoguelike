#!/usr/bin/env -S uv run --quiet
# /// script
# requires-python = ">=3.10"
# dependencies = ["pillow"]
# ///
"""Slice the generated tilesets into the PNGs the Game loads at runtime.

    uv run tools/slice_tileset.py [sheet|tileset.jpg]

Writes src/WowRoguelike.Game/Assets/{tiles,sprites}/*.png. Tiles are cut from
the inside of a cell (away from the painted rims) and scaled to TILE px.
Sprites have their flat background flood-filled to transparent from the crop
edge, are trimmed, then fitted bottom-centred into a SPRITE px square.

Sheets are named: `wailing` (the default, terrain + classes + Wailing Caverns
creatures), `forms` (Druid cat/bear, hunter pet, the four totems) and
`future-forms` (Druid tree/moonkin). A path as the first argument keeps the old
behaviour: slice it with the `wailing` boxes.

The source boxes below are pixel coordinates in a 2400x1792 sheet.
"""
import sys
from collections import deque
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
TILESETS = ROOT / "images/Tilesets"
OUT = ROOT / "src/WowRoguelike.Game/Assets"

# Must match `tile` and `spriteSize` in the Game's Program.fs.
TILE = 40
SPRITE = 48

# name -> (left, top, right, bottom), cut square from inside the cell.
TILES = {
    "floor0": (650, 50, 850, 250),     # mossy dirt
    "floor1": (610, 320, 790, 500),    # mossy dirt, second variant
    "wall": (1240, 60, 1440, 260),     # rock with roots
    "panel": (50, 50, 250, 250),       # dark cobbles, UI panel backing
}

HERO_Y = (910, 1188)
MOB1_Y = (1210, 1486)
MOB2_Y = (1510, 1784)

def cell(col, ys):
    return (col * 300 + 12, ys[0], col * 300 + 288, ys[1])

# name -> (box, size)
SPRITES = {
    "warrior": ((12, HERO_Y[0], 440, HERO_Y[1]), SPRITE),
    "priest": ((460, HERO_Y[0], 888, HERO_Y[1]), SPRITE),
    "rogue": ((912, HERO_Y[0], 1488, HERO_Y[1]), SPRITE),
    "druid": ((1512, HERO_Y[0], 1938, HERO_Y[1]), SPRITE),
    "mage": ((1962, HERO_Y[0], 2388, HERO_Y[1]), SPRITE),
    "raptor0": (cell(0, MOB1_Y), SPRITE),
    "raptor1": (cell(0, MOB2_Y), SPRITE),
    "anacondra": (cell(2, MOB1_Y), SPRITE),
    "fang_druid": (cell(3, MOB1_Y), SPRITE),
    "serpent": (cell(6, MOB1_Y), SPRITE),
    "cobra_lord": (cell(7, MOB1_Y), SPRITE),
    "pythas": (cell(2, MOB2_Y), SPRITE),
    "serpentis": (cell(3, MOB2_Y), SPRITE),
    "mutanus": (cell(7, MOB2_Y), SPRITE),
    "deviate_turtle": (cell(4, MOB1_Y), SPRITE),
    # Floor decorations.
    "mushroom_green": ((1212, 312, 1350, 452), 24),
    "mushroom_blue": ((1350, 312, 1488, 452), 24),
    "mushroom_teal": ((1212, 452, 1350, 590), 22),
    "fern": ((1212, 612, 1488, 888), 34),
}

# The extended sheet re-draws cat/bear and adds the pet and the four totems. A
# totem is drawn by its slot, not its name, so `fire` is here ahead of the first
# fire totem in the content: there is no fire pulse yet, so nothing maps to it.
# Boxes are the content bbox of each painted cell: sprite plus its stone plinth,
# stopping above the caption. The captions are the reason these are not the whole
# 300 px cell -- a caption left inside the crop survives the flood fill as an
# island and `fit` then shrinks the sprite to make room for it.
FORMS = {
    "cat_form": ((63, 239, 415, 512), SPRITE),
    "bear_form": ((444, 159, 813, 516), SPRITE),
    "pet": ((912, 249, 1188, 500), SPRITE),
    "totem_fire": ((1815, 124, 2065, 378), SPRITE),
    "totem_earth": ((2090, 142, 2340, 378), SPRITE),
    "totem_water": ((1815, 440, 2066, 693), SPRITE),
    "totem_air": ((2090, 450, 2340, 676), SPRITE),
}

# For use in the future: the healer and caster forms sit between cat and bear and
# the totems on their own sheet. Nothing in the sim has a form state yet.
FUTURE_FORMS = {
    "tree_form": ((855, 102, 1184, 510), SPRITE),
    "moonkin_form": ((1215, 117, 1544, 510), SPRITE),
}

# name -> (sheet, tiles, sprites)
SHEETS = {
    "wailing": (TILESETS / "Gemini_Generated_Image_tyj66ztyj66ztyj6.jpg", TILES, SPRITES),
    "forms": (TILESETS / "Gemini_Generated_Image_Extended_Druid_Shaman_Hunter.jpg", {}, FORMS),
    "future-forms": (TILESETS / "Gemini_Generated_Image_Druid_Tree_and_Moonkin.jpg", {}, FUTURE_FORMS),
}

def key_out(img, tol=30):
    """Flood-fill the background from every edge pixel, making it transparent."""
    img = img.convert("RGBA")
    w, h = img.size
    px = img.load()
    edge = [px[x, y] for x in range(w) for y in (0, h - 1)] + [px[x, y] for y in range(h) for x in (0, w - 1)]
    bg = tuple(sorted(c[i] for c in edge)[len(edge) // 2] for i in range(3))
    near = lambda c: sum((c[i] - bg[i]) ** 2 for i in range(3)) <= tol * tol
    seen = bytearray(w * h)
    q = deque((x, y) for x in range(w) for y in (0, h - 1))
    q.extend((x, y) for y in range(h) for x in (0, w - 1))
    while q:
        x, y = q.popleft()
        if not (0 <= x < w and 0 <= y < h) or seen[y * w + x]:
            continue
        seen[y * w + x] = 1
        if near(px[x, y]):
            px[x, y] = (0, 0, 0, 0)
            q.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
    return img

def fit(img, size):
    img = img.crop(img.getbbox())
    scale = size / max(img.size)
    img = img.resize((max(1, round(img.width * scale)), max(1, round(img.height * scale))), Image.LANCZOS)
    out = Image.new("RGBA", (size, size))
    out.paste(img, ((size - img.width) // 2, size - img.height), img)
    return out

def main():
    arg = sys.argv[1] if len(sys.argv) > 1 else "wailing"
    if arg in SHEETS:
        src, tiles, sprites = SHEETS[arg]
    else:  # a path: the original single-sheet behaviour.
        src, tiles, sprites = Path(arg), TILES, SPRITES

    sheet = Image.open(src).convert("RGB")
    (OUT / "tiles").mkdir(parents=True, exist_ok=True)
    (OUT / "sprites").mkdir(parents=True, exist_ok=True)
    for name, box in tiles.items():
        sheet.crop(box).resize((TILE, TILE), Image.LANCZOS).save(OUT / "tiles" / f"{name}.png")
    for name, (box, size) in sprites.items():
        keyed = key_out(sheet.crop(box))
        if keyed.getbbox() is None:
            raise SystemExit(f"{name}: box {box} is empty -- caption inside it, or the sheet moved")
        fit(keyed, size).save(OUT / "sprites" / f"{name}.png")
    print(f"{arg}: wrote {len(tiles)} tiles and {len(sprites)} sprites to {OUT.relative_to(ROOT)}")

if __name__ == "__main__":
    main()
