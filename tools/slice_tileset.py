#!/usr/bin/env -S uv run --quiet
# /// script
# requires-python = ">=3.10"
# dependencies = ["pillow"]
# ///
"""Slice the generated tileset into the PNGs the Game loads at runtime.

    uv run tools/slice_tileset.py [tileset.jpg]

Writes src/WowRoguelike.Game/Assets/{tiles,sprites}/*.png. Tiles are cut from
the inside of a cell (away from the painted rims) and scaled to TILE px.
Sprites have their flat background flood-filled to transparent from the crop
edge, are trimmed, then fitted bottom-centred into a SPRITE px square.

The source boxes below are pixel coordinates in the 2400x1792 sheet: terrain
cells sit on a 300 px grid, the hero row is five uneven cells.
"""
import sys
from collections import deque
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "images/Gemini_Generated_Image_tyj66ztyj66ztyj6.jpg"
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
    sheet = Image.open(SRC).convert("RGB")
    (OUT / "tiles").mkdir(parents=True, exist_ok=True)
    (OUT / "sprites").mkdir(parents=True, exist_ok=True)
    for name, box in TILES.items():
        sheet.crop(box).resize((TILE, TILE), Image.LANCZOS).save(OUT / "tiles" / f"{name}.png")
    for name, (box, size) in SPRITES.items():
        fit(key_out(sheet.crop(box)), size).save(OUT / "sprites" / f"{name}.png")
    print(f"wrote {len(TILES)} tiles and {len(SPRITES)} sprites to {OUT.relative_to(ROOT)}")

if __name__ == "__main__":
    main()
