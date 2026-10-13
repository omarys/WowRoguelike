#!/usr/bin/env python3
"""Color-aware extraction of Wailing Caverns section maps -> ASCII grids.

Classifies each pixel as floor (tan/gold), wall (black/olive rock), or
green (foliage/water, non-walkable), then optionally cleans speckles/holes
and reports the connected floor components.
"""
import subprocess, sys, collections

IMAGES = [
    "the-screaming-gully",
    "dreamers-rock",
    "the-pit-of-fangs",
    "crag-of-the-everliving",
    "the-winding-chasm",
]

def load(path, w, h):
    data = subprocess.run(
        ["magick", path, "-resize", f"{w}x{h}!", "-depth", "8", "rgb:-"],
        capture_output=True, check=True).stdout
    assert len(data) == w * h * 3, (len(data), w * h * 3)
    return data

def classify(r, g, b):
    mx = max(r, g, b)
    mn = min(r, g, b)
    if mx < 40:
        return '#'          # black void
    if (mx - mn) < 30 and mx < 90:
        return '#'          # dark olive rock
    return '.'              # floor: tan, gold, green grass

def neighbors8(grid, x, y):
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx == 0 and dy == 0:
                continue
            yield x + dx, y + dy

def cleanup(grid):
    """Remove 1-pixel floor speckles and fill 1-pixel wall holes, 8-conn."""
    w, h = len(grid[0]), len(grid)
    g = [list(r) for r in grid]
    for _ in range(2):
        ng = [r[:] for r in g]
        for y in range(h):
            for x in range(w):
                floors = 0
                for nx, ny in neighbors8(g, x, y):
                    if 0 <= nx < w and 0 <= ny < h and g[ny][nx] == '.':
                        floors += 1
                if g[y][x] == '.' and floors == 0:
                    ng[y][x] = '#'
                elif g[y][x] != '.' and floors >= 7:
                    ng[y][x] = '.'
        g = ng
    return [''.join(r) for r in g]

def components(grid):
    """4-connected floor components -> list of sizes."""
    w, h = len(grid[0]), len(grid)
    seen = [[False] * w for _ in range(h)]
    sizes = []
    for sy in range(h):
        for sx in range(w):
            if grid[sy][sx] != '.' or seen[sy][sx]:
                continue
            q = [(sx, sy)]
            seen[sy][sx] = True
            n = 0
            while q:
                x, y = q.pop()
                n += 1
                for nx, ny in ((x+1, y), (x-1, y), (x, y+1), (x, y-1)):
                    if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] == '.' and not seen[ny][nx]:
                        seen[ny][nx] = True
                        q.append((nx, ny))
            sizes.append(n)
    return sizes

def keep_largest(grid):
    """Zero out every floor component except the largest (4-conn)."""
    w, h = len(grid[0]), len(grid)
    seen = [[False] * w for _ in range(h)]
    best = []
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
            if len(comp) > len(best):
                best = comp
    out = [list('#' * w) for _ in range(h)]
    for x, y in best:
        out[y][x] = '.'
    return [''.join(r) for r in out]

def main():
    mode = sys.argv[1] if len(sys.argv) > 1 else "diag"
    w = int(sys.argv[2]) if len(sys.argv) > 2 else 56
    for name in IMAGES:
        path = f"images/{name}.webp"
        info = subprocess.run(
            ["magick", "identify", "-format", "%w %h", path],
            capture_output=True, text=True).stdout.split()
        iw, ih = int(info[0]), int(info[1])
        h = round(ih * w / iw)
        data = load(path, w, h)
        rows = []
        counts = collections.Counter()
        for y in range(h):
            row = []
            for x in range(w):
                i = (y * w + x) * 3
                c = classify(data[i], data[i+1], data[i+2])
                counts[c] += 1
                row.append(c)
            rows.append(''.join(row))

        final = cleanup(rows)
        comps = sorted(components(final), reverse=True)
        if mode == 'largest':
            final = keep_largest(final)
            comps = sorted(components(final), reverse=True)
        print(f"════ {name} ({w}x{h}) floor={counts['.']} wall={counts['#']} components={comps[:8]} ════")
        for r in final:
            print(r)
        print()

    if mode == 'largest':
        # Emit an F# module with one Grid per section.
        idents = {
            "the-screaming-gully": "screamingGully",
            "dreamers-rock": "dreamersRock",
            "the-pit-of-fangs": "pitOfFangs",
            "crag-of-the-everliving": "cragOfTheEverliving",
            "the-winding-chasm": "windingChasm",
        }
        blocks = []
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
            final = keep_largest(cleanup(rows))
            ident = idents[name]
            rows = '\n        '.join(f'"{r}"' for r in final)
            blocks.append(f"  /// {name}\n  let {ident} =\n    Grid.ofRows\n      [ {rows} ]")
        out = f"""namespace WowRoguelike.Core

/// The five Wailing Caverns section maps, extracted from the section art in
/// images/ (color-aware: near-black and dark-olive are walls; tan, gold and
/// green are walkable floor) by tools/extract_wc_maps.py, then kept to the
/// largest connected floor region. These are separate areas, not one dungeon.
module Maps =

{chr(10).join(blocks)}
"""
        with open("/tmp/Maps.fs", "w") as f:
            f.write(out)
        print("wrote /tmp/Maps.fs")


main()
