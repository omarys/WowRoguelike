#!/usr/bin/env python3
"""Extract every Wailing Caverns section map room into a separate Grid fixture.

Each significant connected floor component becomes its own cropped grid (bounding
box + one-cell wall margin), named <section> for the largest room and
<section>-<direction> for the rest. Emits Maps.fs.
"""
import subprocess, sys

IMAGES = [
    "the-screaming-gully",
    "dreamers-rock",
    "the-pit-of-fangs",
    "crag-of-the-everliving",
    "the-winding-chasm",
]
THRESHOLD = 25          # drop components smaller than this (art noise)

def load(path, w, h):
    data = subprocess.run(
        ["magick", path, "-resize", f"{w}x{h}!", "-depth", "8", "rgb:-"],
        capture_output=True, check=True).stdout
    assert len(data) == w * h * 3
    return data

def classify(r, g, b):
    mx = max(r, g, b)
    mn = min(r, g, b)
    if mx < 40:
        return '#'
    if (mx - mn) < 30 and mx < 90:
        return '#'
    return '.'

def cleanup(grid):
    w, h = len(grid[0]), len(grid)
    g = [list(r) for r in grid]
    for _ in range(2):
        ng = [r[:] for r in g]
        for y in range(h):
            for x in range(w):
                floors = 0
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        if dx == 0 and dy == 0:
                            continue
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < w and 0 <= ny < h and g[ny][nx] == '.':
                            floors += 1
                if g[y][x] == '.' and floors == 0:
                    ng[y][x] = '#'
                elif g[y][x] != '.' and floors >= 7:
                    ng[y][x] = '.'
        g = ng
    return [''.join(r) for r in g]

def components(grid):
    w, h = len(grid[0]), len(grid)
    seen = [[False] * w for _ in range(h)]
    out = []
    for sy in range(h):
        for sx in range(w):
            if grid[sy][sx] != '.' or seen[sy][sx]:
                continue
            q = [(sx, sy)]
            seen[sy][sx] = True
            comp = []
            while q:
                x, y = q.pop()
                comp.append((x, y))
                for nx, ny in ((x+1, y), (x-1, y), (x, y+1), (x, y-1)):
                    if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] == '.' and not seen[ny][nx]:
                        seen[ny][nx] = True
                        q.append((nx, ny))
            out.append(comp)
    return out

def crop(grid, comp):
    w, h = len(grid[0]), len(grid)
    xs = [x for x, _ in comp]
    ys = [y for _, y in comp]
    x0, x1 = max(0, min(xs) - 1), min(w - 1, max(xs) + 1)
    y0, y1 = max(0, min(ys) - 1), min(h - 1, max(ys) + 1)
    rows = []
    for y in range(y0, y1 + 1):
        rows.append(''.join('.' if (x, y) in comp else '#' for x in range(x0, x1 + 1)))
    return rows

def direction(a, b):
    ax = sum(x for x, _ in a) / len(a)
    ay = sum(y for _, y in a) / len(a)
    bx = sum(x for x, _ in b) / len(b)
    by = sum(y for _, y in b) / len(b)
    dx, dy = bx - ax, by - ay
    if abs(dx) > abs(dy):
        return 'east' if dx > 0 else 'west'
    return 'south' if dy > 0 else 'north'

def main():
    w = int(sys.argv[1]) if len(sys.argv) > 1 else 56
    entries = []
    for name in IMAGES:
        path = f"images/{name}.webp"
        info = subprocess.run(
            ["magick", "identify", "-format", "%w %h", path],
            capture_output=True, text=True).stdout.split()
        iw, ih = int(info[0]), int(info[1])
        h = round(ih * w / iw)
        data = load(path, w, h)
        rows = []
        for y in range(h):
            row = []
            for x in range(w):
                i = (y * w + x) * 3
                row.append(classify(data[i], data[i+1], data[i+2]))
            rows.append(''.join(row))

        grid = cleanup(rows)
        comps = [c for c in components(grid) if len(c) >= THRESHOLD]
        comps.sort(key=len, reverse=True)
        for i, comp in enumerate(comps):
            room = crop(grid, comp)
            rname = name if i == 0 else f"{name}-{direction(comps[0], comp)}"
            entries.append((rname, room, len(comp)))
            print(f"{rname}: {len(comp)} cells, {len(room[0])}x{len(room)}")

    # Emit F#.
    blocks = []
    for rname, room, _ in entries:
        rows = '\n            '.join(f'"{r}"' for r in room)
        blocks.append(f'      "{rname}",\n        Grid.ofRows\n          [ {rows} ]')
    body = '\n'.join(blocks)
    out = f"""namespace WowRoguelike.Core

/// Every Wailing Caverns room, extracted from the section art in images/ by
/// tools/extract_wc_maps.py: near-black and dark-olive are walls; tan, gold and
/// green are walkable floor. Each connected floor region of at least 25 cells
/// becomes its own room, cropped to its bounding box with a one-cell wall
/// margin. Within a section the largest room keeps the section name and the
/// rest take a compass suffix.
module Maps =

  /// All extracted rooms, largest-per-section first, each a connected Grid.
  let rooms : (string * Grid) list =
    [
{body}
    ]
"""
    with open("/tmp/Maps.fs", "w") as f:
        f.write(out)
    print("wrote /tmp/Maps.fs")

main()
