#!/usr/bin/env python3
"""Extract each Scarlet Monastery wing map into one Grid per wing.

A wing map is one building, so unlike extract_wc_maps.py this keeps a single
grid rather than splitting it into rooms. Two things need care:

- Wall polarity. The three schematic maps are dark-on-light like the Wailing
  Caverns sections. The graveyard is a rendered battlemap whose walls are pale
  stone, so there light desaturated stone is the wall and grass/path is floor.
- Connectivity. Narrow corridors on the armory read as wall, leaving the four
  halls as separate floor islands. Components of at least 8 cells are kept and
  then joined to the largest with a carved L-shaped corridor, so every room is
  reachable. That is a repair of the classifier, not of the map.

Emits src/WowRoguelike.Core/MapsScarlet.fs.
"""
import subprocess, sys

# argv[1] overrides the long side and argv[2] == "--dry" measures without
# writing. The battlemaps are D&D reference art with real hallways, so the scale
# decides how wide a hallway lands: too small and a drawn hall collapses to a
# one-tile corridor.
SCALE = int(sys.argv[1]) if len(sys.argv) > 1 and sys.argv[1].isdigit() else None
DRY = "--dry" in sys.argv

# The long side is in tiles. At 100 a drawn hallway collapses to a single tile,
# which is what boxed the Party in; the battlemaps are D&D reference art with
# real hallways, so 200 lands them two to three tiles wide.
WINGS = [
    # name, image, long side, wall mode
    ("graveyard", "images/Maps/ScarletMonastery/graveyard.jpg", 200, "stone"),
    ("library", "images/Maps/ScarletMonastery/library.jpg", 200, "dark"),
    ("armory", "images/Maps/ScarletMonastery/armory.jpg", 200, "dark"),
    ("cathedral", "images/Maps/ScarletMonastery/cathedral.jpg", 200, "dark"),
]

MIN_COMPONENT = 8


def tight_share(grid):
    """Share of floor tiles with two or fewer floor neighbours: a proxy for how
    much of the map is a one-tile-wide corridor."""
    w, h = len(grid[0]), len(grid)
    tight = total = 0
    for y in range(h):
        for x in range(w):
            if grid[y][x] != '.':
                continue
            total += 1
            n = sum(1 for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))
                    if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] == '.')
            if n <= 2:
                tight += 1
    return tight / total if total else 0.0


def load(path, w, h):
    data = subprocess.run(
        ["magick", path, "-resize", f"{w}x{h}!", "-depth", "8", "rgb:-"],
        capture_output=True, check=True).stdout
    assert len(data) == w * h * 3, (len(data), w, h)
    return data


def classify(r, g, b, mode):
    mx, mn = max(r, g, b), min(r, g, b)
    if mode == "dark":
        if mx < 40:
            return '#'
        if (mx - mn) < 30 and mx < 90:
            return '#'
        return '.'
    if mx < 45:                       # the black page background
        return '#'
    if mx > 165 and (mx - mn) < 42:   # pale desaturated stone
        return '#'
    return '.'


def cleanup(grid):
    """Same two-pass pass as extract_wc_maps.py: fill specks, plug pinholes."""
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


def components(grid, floor='.'):
    w, h = len(grid[0]), len(grid)
    seen = [[False] * w for _ in range(h)]
    out = []
    for sy in range(h):
        for sx in range(w):
            if grid[sy][sx] != floor or seen[sy][sx]:
                continue
            q, comp = [(sx, sy)], []
            seen[sy][sx] = True
            while q:
                x, y = q.pop()
                comp.append((x, y))
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] == floor and not seen[ny][nx]:
                        seen[ny][nx] = True
                        q.append((nx, ny))
            out.append(comp)
    return sorted(out, key=len, reverse=True)


def thin_walls(grid, min_cells=4):
    """Wall specks below `min_cells` are props — gravestones, pillars, shadows —
    not structure, so they become floor. Without this the rendered graveyard is
    speckled with unmappable noise."""
    g = [list(r) for r in grid]
    for comp in components(grid, '#'):
        if len(comp) < min_cells:
            for x, y in comp:
                g[y][x] = '.'
    return [''.join(r) for r in g]


def bridge(grid, comps):
    """Carve an L corridor from each island to the mass already joined."""
    g = [list(r) for r in grid]
    joined = {p for c in comps[:1] if len(c) >= MIN_COMPONENT for p in c}
    for comp in comps[1:]:
        if len(comp) < MIN_COMPONENT or not joined:
            continue
        _, p, q = min(((abs(p[0] - q[0]) + abs(p[1] - q[1]), p, q)
                       for p in comp for q in joined), key=lambda t: t[0])
        (px, py), (qx, qy) = p, q
        for x in range(min(px, qx), max(px, qx) + 1):
            g[py][x] = '.'
        for y in range(min(py, qy), max(py, qy) + 1):
            g[y][qx] = '.'
        joined |= {p for p in comp}
    return [''.join(r) for r in g]


def seal(grid):
    """Any floor island the bridge could not reach becomes wall. What the Party
    can see is then exactly what it can walk to — an unreachable pocket is a
    target the tank can path to and never touch."""
    comps = components(grid)
    g = [list(r) for r in grid]
    for comp in comps[1:]:
        for x, y in comp:
            g[y][x] = '#'
    return [''.join(r) for r in g]


def crop(grid):
    w, h = len(grid[0]), len(grid)
    pts = [(x, y) for y in range(h) for x in range(w) if grid[y][x] == '.']
    x0, x1 = min(p[0] for p in pts), max(p[0] for p in pts)
    y0, y1 = min(p[1] for p in pts), max(p[1] for p in pts)
    return [grid[y][max(0, x0 - 1):x1 + 2] for y in range(max(0, y0 - 1), min(h, y1 + 2))]


def main():
    blocks = []
    for name, path, long_side, mode in WINGS:
        long_side = SCALE or long_side
        info = subprocess.run(["magick", "identify", "-format", "%w %h", path],
                              capture_output=True, text=True).stdout.split()
        iw, ih = int(info[0]), int(info[1])
        if iw >= ih:
            w, h = long_side, max(1, round(ih * long_side / iw))
        else:
            h, w = long_side, max(1, round(iw * long_side / ih))
        data = load(path, w, h)
        rows = [''.join(classify(data[(y * w + x) * 3], data[(y * w + x) * 3 + 1],
                                 data[(y * w + x) * 3 + 2], mode)
                        for x in range(w))
                for y in range(h)]
        # Specks, islands and gaps all scale with the map, so their thresholds
        # must too: a room is the same fraction of the map at any resolution.
        speck = max(4, round(4 * (long_side / 100) ** 2))
        island = max(MIN_COMPONENT, round(MIN_COMPONENT * (long_side / 100) ** 2))
        cleaned = thin_walls(cleanup(rows), speck)
        grid = seal(bridge(cleaned, components(cleaned)))
        reach = len(components(grid)[0]) if components(grid) else 0
        grid = crop(grid)
        islands = len([c for c in components(grid) if len(c) >= island])
        floor = sum(r.count('.') for r in grid)
        print(f"{name}: {len(grid[0])}x{len(grid)} floor={floor} rooms={islands} "
              f"tight={tight_share(grid):.0%} walkable={reach}", file=sys.stderr)
        body = '\n        '.join(f'"{r}"' for r in grid)
        blocks.append(f'  let {name} : Grid =\n    Grid.ofRows\n      [ {body} ]')

    header = """namespace WowRoguelike.Core

/// The four Scarlet Monastery wings, each one whole-wing Grid extracted from the
/// battlemap in images/Maps/ScarletMonastery/ by tools/extract_sm_maps.py.
/// Schematics are dark-walled; the graveyard is a rendered map with pale stone
/// walls, so it classifies by stone instead of darkness. Islands left by narrow
/// corridors are joined with a carved L corridor so every room is reachable.
module MapsScarlet =

"""
    trailer = """
  let wings : (string * Grid) list =
    [ "graveyard", graveyard
      "library", library
      "armory", armory
      "cathedral", cathedral ]

  let wing (name: string) : Grid =
    wings
    |> List.tryFind (fun (n, _) -> n = name)
    |> Option.map snd
    |> Option.defaultWith (fun () -> failwithf "no such wing: %s" name)

  /// Every walkable tile, top row first — the order the entrance is found in.
  let floorTiles (g: Grid) : Pos list =
    [ for y in 0 .. g.Height - 1 do
        for x in 0 .. g.Width - 1 do
          if Grid.isFloor g { X = x; Y = y } then { X = x; Y = y } ]
"""
    if DRY:
        return

    out = header + "\n".join(blocks) + trailer
    path = "src/WowRoguelike.Core/MapsScarlet.fs"
    with open(path, "w") as f:
        f.write(out)
    print(f"wrote {path}", file=sys.stderr)


main()
