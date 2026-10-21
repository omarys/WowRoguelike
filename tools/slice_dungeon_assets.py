#!/usr/bin/env -S uv run --quiet
# /// script
# requires-python = ">=3.10"
# dependencies = ["pillow", "numpy"]
# ///
"""Slice the generated dungeon sheets in images/ into the assets the Game loads.

    uv run tools/slice_dungeon_assets.py [slug ...]

Each sheet is a painted roster: a band of environment props on top, the nine class
character sprites with their names printed underneath, then the creatures of that
dungeon, also labelled. Assets are written per dungeon:

    Assets/tiles/<dungeon>/props/<name>.png    environment, TILE px
    Assets/sprites/<dungeon>/<name>.png        creatures, SPRITE px
    Assets/sprites/classes/<class>.png         the shared class row, SPRITE px
    Assets/tiles/<dungeon>/manifest.json       name -> source box, for re-slicing

Every sheet is laid out differently, and on several the creature art runs straight
into the printed caption with no blank row between them, so the two typed numbers
per sheet (`props_end`, `captions`) are read off the sheet itself rather than
guessed by a pixel heuristic. A group's columns still come from the window around
its printed caption, and each sprite is the tight ink box inside that window, so a
re-generated sheet only needs its band table refreshed. Creature names are typed in
below only where the roster is known; otherwise they are positional (`mob-<row>-<n>`).
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
IMAGES = ROOT / "images"
OUT = ROOT / "src/WowRoguelike.Game/Assets"

# Must match `tile` and `spriteSize` in the Game's Program.fs.
TILE = 40
SPRITE = 48

# Brightness that separates painted art from the sheet's dark, grainy paper.
INK = 96

# A crop is the bounding box of one sprite, so its two most common dark colours
# are the paper and the grid printed over it; keying near both clears the
# background to the pixel without touching the painted art.
PAPER_TOL = 16

# A frame or grid line printed over the paper is at most this wide; the background
# fill crosses anything thinner rather than treating it as a wall.
GRID_LINE = 5

# The printed captions are bright and colourless, unlike any of the painted art,
# and inside a label the word gap can be wider than the gap between two short
# neighbouring labels -- so the names are found by colour, not by brightness alone.
TEXT_MIN, TEXT_SPREAD, TEXT_GAP = 150, 26, 28

# Columns to cut a prop row into when it is one continuous painted backdrop with
# no gutter between props to find instead.
PROP_COLS = 8

CLASS_NAMES = [
    "hunter", "shaman", "paladin", "warlock", "warrior",
    "priest", "rogue", "druid", "mage",
]

# The class row is identical on every sheet, so it is cut once from this one.
CLASS_SHEET = "blackfathom-deeps"

# slug -> sheet, the last row of prop art, and the printed-caption bands top to
# bottom (class row first, then creature rows). A band is the y range of the
# printed names; the art for it is the ink between the previous band and this one.
# `labels` are the printed names of each creature band in reading order, and are
# omitted where only positional names would be honest.
SHEETS = {
    "blackfathom-deeps": dict(
        src="Gemini_Generated_Image_Blackfathom_Deeps.jpg",
        props_end=594,
        captions=[(1000, 1039), (1345, 1443), (1738, 1790)],
        labels=[
            ["nagas", "satyr", "corrupted-night-elves", "sea-giants", "hydras", "giant-crabs"],
            ["aquatics", "water-elementals", "frenzies", "gelthast", "baron-aquanis",
             "lady-sarevess", "akumai"],
        ],
    ),
    "razorfen-kraul": dict(
        src="Gemini_Generated_Image_Razorfen.jpg",
        props_end=594,
        captions=[(1000, 1039), (1361, 1440), (1709, 1791)],
    ),
    "ragefire-chasm": dict(
        src="Gemini_Generated_Image_Ragefire_Chasm.jpg",
        props_end=766,
        captions=[(1149, 1180), (1474, 1535)],
    ),
    "the-deadmines": dict(
        src="Gemini_Generated_Image_The_Deadmines.jpg",
        props_end=934,
        captions=[(1202, 1236), (1453, 1488), (1750, 1785)],
    ),
    "the-stockade": dict(
        src="Gemini_Generated_Image_The_Stockade.jpg",
        props_end=934,
        captions=[(1202, 1236), (1454, 1488), (1751, 1786)],
    ),
    "shadowfang-keep": dict(
        src="Gemini_Generated_Image_Shadowfang_Keep.jpg",
        props_end=900,
        captions=[(1202, 1236), (1454, 1488), (1751, 1785)],
    ),
    "scarlet-monastery": dict(
        src="Gemini_Generated_Image_Scarlet_Monestary.jpg",
        props_end=775,
        captions=[(1086, 1118), (1393, 1456), (1716, 1780)],
    ),
}


def mask_of(img):
    return np.asarray(img.convert("RGB"), dtype=np.int16).max(axis=2) > INK


def text_mask_of(img):
    a = np.asarray(img.convert("RGB"), dtype=np.int16)
    peak = a.max(axis=2)
    return (peak > TEXT_MIN) & (peak - a.min(axis=2) < TEXT_SPREAD)


def spans(profile, minimum):
    """Runs of truth in a 1-D profile, ignoring gaps narrower than `minimum`."""
    on = np.flatnonzero(profile > 0)
    if on.size == 0:
        return []
    out, start = [], on[0]
    for prev, cur in zip(on, on[1:]):
        if cur - prev > minimum:
            out.append((int(start), int(prev)))
            start = cur
    out.append((int(start), int(on[-1])))
    return out


def label_windows(text, block, width):
    """Column windows of the printed names in one caption band."""
    found = spans(text[block[0]:block[1] + 1].any(axis=0).astype(np.int16), 12)
    if not found:
        return []
    merged = [list(found[0])]
    for x0, x1 in found[1:]:
        if x0 - merged[-1][1] <= TEXT_GAP:
            merged[-1][1] = x1
        else:
            merged.append([x0, x1])
    edges = [0]
    for a, b in zip(merged, merged[1:]):
        edges.append((a[1] + b[0]) // 2)
    edges.append(width)
    return [(edges[i], edges[i + 1]) for i in range(len(merged))]


def tight(mask, x0, x1, y0, y1):
    part = mask[y0:y1, x0:x1]
    ys, xs = np.nonzero(part)
    if ys.size == 0:
        return None
    return (x0 + int(xs.min()), y0 + int(ys.min()), x0 + int(xs.max()) + 1, y0 + int(ys.max()) + 1)


def shifted(mask, steps, axis):
    """`mask` moved `steps` along `axis`, filling the vacated edge with False."""
    out = np.zeros_like(mask)
    if axis == 0:
        if steps > 0:
            out[steps:] = mask[:-steps]
        else:
            out[:steps] = mask[-steps:]
    else:
        if steps > 0:
            out[:, steps:] = mask[:, :-steps]
        else:
            out[:, :steps] = mask[:, -steps:]
    return out


def paper_shades(a):
    """The one or two flat colours that make up a crop's background.

    A crop holds one sprite, so most of it is paper and the grid printed over the
    paper is the next most common colour. Painted art varies too much to reach
    those top spots, and anything bright is skipped outright.
    """
    rgb = a[:, :, :3].reshape(-1, 3)
    dark = rgb.max(axis=1) < 90
    cells = rgb[dark] // 16
    bin_id = cells[:, 0] * 256 + cells[:, 1] * 16 + cells[:, 2]
    if bin_id.size == 0:
        return [np.median(rgb, axis=0)]
    counts = np.bincount(bin_id, minlength=4096)
    common = [c for c in np.argsort(counts)[::-1][:2] if counts[c] * 25 > bin_id.size]
    return [np.median(rgb[dark][bin_id == c], axis=0) for c in common]


def key_out(img, tol=PAPER_TOL):
    """Make the paper background transparent, leaving the painted art opaque.

    The background is the paper plus the faint grid printed on it -- the two most
    common dark colours of a crop. Only background the crop's edge can reach is
    cleared, so the shadowed body of a dark sprite survives even where it matches
    the paper. A line thinner than `GRID_LINE` does not count as a wall, though,
    and neither does the crop's own border: a cell's frame would otherwise fence
    off the paper it encloses and leave a black box behind the sprite.
    """
    img = img.convert("RGBA")
    a = np.asarray(img, dtype=np.int16)
    h, w = a.shape[:2]
    paper = np.zeros((h, w), bool)
    for shade in paper_shades(a):
        paper |= ((a[:, :, :3] - shade) ** 2).sum(axis=2) <= tol * tol

    pad = GRID_LINE
    padded = np.pad(paper, pad, constant_values=True)  # outside the crop is paper
    line = np.zeros_like(padded)
    for step in range(1, pad + 1):
        line |= ((shifted(padded, step, 1) & shifted(padded, -step, 1))
                 | (shifted(padded, step, 0) & shifted(padded, -step, 0)))
    free = padded | line

    ph, pw = free.shape
    inside = np.zeros_like(free)
    stack = [(x, y) for x in range(pw) for y in (0, ph - 1)]
    stack += [(x, y) for y in range(ph) for x in (0, pw - 1)]
    while stack:
        x, y = stack.pop()
        if not (0 <= x < pw and 0 <= y < ph) or inside[y, x] or not free[y, x]:
            continue
        inside[y, x] = True
        stack.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))

    a[inside[pad:pad + h, pad:pad + w]] = 0
    a[~keep_parts(a)] = 0
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def keep_parts(a):
    """Opaque pixels belonging to a real part of the sprite.

    A cell's frame and the specks of the paper are separate blobs next to the
    figure, and a frame line shifts the whole crop, so anything much smaller than
    the figure itself is dropped.
    """
    h, w = a.shape[:2]
    solid = a[:, :, 3] > 0
    keep = np.zeros_like(solid)
    parts = []
    for sy in range(h):
        for sx in range(w):
            if not solid[sy, sx] or keep[sy, sx]:
                continue
            keep[sy, sx] = True
            stack, size = [(sx, sy)], 0
            while stack:
                x, y = stack.pop()
                size += 1
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < w and 0 <= ny < h and solid[ny, nx] and not keep[ny, nx]:
                        keep[ny, nx] = True
                        stack.append((nx, ny))
            parts.append((size, (sx, sy)))
    if len(parts) > 1:
        biggest = max(size for size, _ in parts)
        for size, (x, y) in parts:
            if size * 5 < biggest:
                todo = [(x, y)]
                while todo:
                    cx, cy = todo.pop()
                    if not keep[cy, cx]:
                        continue
                    keep[cy, cx] = False
                    for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                        if 0 <= nx < w and 0 <= ny < h and solid[ny, nx]:
                            todo.append((nx, ny))
    return keep


def fit(img, size):
    img = img.crop(img.getbbox())
    scale = size / max(img.size)
    img = img.resize(
        (max(1, round(img.width * scale)), max(1, round(img.height * scale))),
        Image.NEAREST,
    )
    out = Image.new("RGBA", (size, size))
    out.paste(img, ((size - img.width) // 2, size - img.height), img)
    return out


def write_sprite(sheet, box, path, size=SPRITE):
    img = key_out(sheet.crop(box))
    # A black rule often sits between the art and its caption: drop those rows
    # so the sprite does not get a bar welded to its head.
    while img.height > 8:
        row = np.asarray(img.crop((0, 0, img.width, 1)))[0]
        if (row[:, 3] > 100).mean() > 0.7 and row[:, :3].mean() < 40:
            img = img.crop((0, 1, img.width, img.height))
        else:
            break
    bb = img.getbbox()
    if bb is None or bb[3] - bb[1] < 12:
        # A caption line caught as "art": nothing worth drawing.
        path.unlink(missing_ok=True)
        return False
    path.parent.mkdir(parents=True, exist_ok=True)
    fit(img, size).save(path)
    return True


def write_tile(sheet, box, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.crop(box).resize((TILE, TILE), Image.NEAREST).save(path)


def slice_band(sheet, mask, text, art, block, labels, out_dir, tag):
    """One sprite per caption group in a caption band, in reading order."""
    windows = label_windows(text, block, mask.shape[1])
    print(f"  {out_dir.name}: band {block} -> {len(windows)} caption group(s)")
    if not labels or len(labels) != len(windows):
        if labels:
            print(f"  ! {out_dir.name} {block}: {len(windows)} captions for "
                  f"{len(labels)} label(s) {labels} - named positionally instead")
        labels = [f"{tag}-{i + 1}" for i in range(len(windows))]
    boxes = {}
    for (x0, x1), name in zip(windows, labels):
        box = tight(mask, x0, x1, art[0], art[1] + 1)
        if box is None:
            continue
        if not write_sprite(sheet, box, out_dir / f"{name}.png"):
            print(f"  ! {out_dir.name} {block}: {name} is a caption strip, skipped")
            continue
        boxes[name] = box
    return boxes


def slice_props(sheet, mask, props_end, props, manifest):
    """The unlabelled environment art above the class row, one tile per blob."""
    for row, (y0, y1) in enumerate(spans(mask[:props_end + 1].any(axis=1).astype(np.int16), 6)):
        strip = mask[y0:y1 + 1]
        windows = spans(strip.any(axis=0).astype(np.int16), 6)
        if len(windows) < 4:
            step = mask.shape[1] / PROP_COLS
            windows = [(int(i * step), int((i + 1) * step) - 1) for i in range(PROP_COLS)]
        for col, (x0, x1) in enumerate(windows):
            box = tight(mask, x0, x1 + 1, y0, y1 + 1)
            if box is None or box[2] - box[0] < 24:
                continue
            name = f"prop-{row + 1}{col + 1:02d}"
            write_tile(sheet, box, props / f"{name}.png")
            manifest["props"][name] = box


def slice_sheet(slug, spec):
    sheet = Image.open(IMAGES / spec["src"]).convert("RGB")
    mask = mask_of(sheet)
    text = text_mask_of(sheet)
    manifest = {"sheet": spec["src"], "size": list(sheet.size), "props": {}, "creatures": {}}

    slice_props(sheet, mask, spec["props_end"], OUT / "tiles" / slug / "props", manifest)

    bands = spec["captions"]
    labels = spec.get("labels") or []
    prev_end = spec["props_end"]
    for row, band in enumerate(bands):
        art = (prev_end + 1, band[0] - 1)
        prev_end = band[1]
        if row == 0:
            # The class band: 9 printed names. Only one sheet supplies the shared row.
            found = len(label_windows(text, band, mask.shape[1]))
            if found != len(CLASS_NAMES):
                print(f"  ! {slug}: class band {band} has {found} captions, "
                      f"expected {len(CLASS_NAMES)}")
                continue
            if slug == CLASS_SHEET:
                slice_band(sheet, mask, text, art, band, CLASS_NAMES,
                           OUT / "sprites" / "classes", "class")
            continue
        manifest["creatures"].update(
            slice_band(sheet, mask, text, art, band, labels[row - 1] if row <= len(labels) else None,
                       OUT / "sprites" / slug, f"mob-{row}")
        )

    (OUT / "tiles" / slug).mkdir(parents=True, exist_ok=True)
    (OUT / "tiles" / slug / "manifest.json").write_text(json.dumps(manifest, indent=1))
    print(f"{slug}: {len(manifest['props'])} props, {len(manifest['creatures'])} creatures")


def main():
    for slug in sys.argv[1:] or list(SHEETS):
        slice_sheet(slug, SHEETS[slug])


if __name__ == "__main__":
    main()
