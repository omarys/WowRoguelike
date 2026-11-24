#!/usr/bin/env python3
"""Generate the four Scarlet Monastery wing grids in src/WowRoguelike.Core/MapsScarlet.fs.

Each wing is an architectural floorplan faithfully modeled after its reference
battlemap in images/Maps/ScarletMonastery/:
  - graveyard.jpg : Walled cemetery courtyard, central avenue & statue plaza,
                    flanking colonnades, west/east mausoleums, north main crypt.
  - library.jpg   : West entry gallery -> Entry Cloister Courtyard & southern
                    kennel -> West Cloister Gallery + cobblestone bypass loop ->
                    West Athenaeum with study alcoves -> twin pillared Main
                    Library Galleries -> East Reading Hall -> Pillared Vestibule
                    -> Arcanist Doan's Circular Sanctum.
  - armory.jpg    : South entry corridor -> Training Courtyard -> Vestibule ->
                    Armory Hall 1 (2 central pillars) -> Eastern Side-Gallery ->
                    Armory Hall 2 (2 central pillars) -> NW Guardroom & Upper
                    Hallway -> Herod's Circular Arena.
  - cathedral.jpg : South entry corridor -> Lower Courtyard around reflecting
                    pool -> twin staircases -> Upper Fountain Courtyard with
                    flanking cloisters & semicircular fountain -> Grand Basilica
                    Nave (6 pillars) -> West & East Transepts -> High Altar &
                    flanking secret chambers -> Whitemane's Circular Apse.

Every wing is verified before emission:
  - 100% single connected floor component from the south entrance
  - Zero 1-wide pinches (every doorway and corridor is at least 2 tiles wide)
  - Clean 1-tile outer wall margin around the cropped bounding box
"""
import math
import sys

DRY = "--dry" in sys.argv


class Canvas:
    def __init__(self, w: int, h: int):
        self.w = w
        self.h = h
        self.g = [["#"] * w for _ in range(h)]

    def fill(self, x0: int, y0: int, x1: int, y1: int, ch: str = "."):
        """Fill inclusive rectangle [x0..x1] x [y0..y1]."""
        for y in range(max(1, y0), min(self.h - 1, y1 + 1)):
            for x in range(max(1, x0), min(self.w - 1, x1 + 1)):
                self.g[y][x] = ch

    def wall(self, x0: int, y0: int, x1: int, y1: int):
        self.fill(x0, y0, x1, y1, "#")

    def circle(self, cx: float, cy: float, r: float, ch: str = "."):
        r2 = r * r
        for y in range(max(1, int(cy - r - 1)), min(self.h - 1, int(cy + r + 2))):
            for x in range(max(1, int(cx - r - 1)), min(self.w - 1, int(cx + r + 2))):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r2:
                    self.g[y][x] = ch

    def rows(self):
        return ["".join(r) for r in self.g]


def build_graveyard():
    """Faithful to images/Maps/ScarletMonastery/graveyard.jpg (54x96)."""
    c = Canvas(54, 96)

    # 1. Main walled cemetery interior (outer stone wall at x=0..1, 52..53, y=0..1)
    c.fill(2, 2, 51, 88)

    # 2. South entry gate & paved avenue extending below the lower colonnade
    c.wall(2, 85, 22, 94)
    c.wall(31, 85, 51, 94)
    c.fill(23, 84, 30, 94)  # Wide south entry gate (8 tiles wide)

    # 3. Lower flanking colonnades & stone cemetery divider walls (y=66..84)
    # Stone walls flanking the central avenue with wide 4-tile openings
    c.wall(21, 66, 22, 84)
    c.wall(31, 66, 32, 84)
    c.fill(21, 70, 22, 74)  # West upper opening
    c.fill(21, 79, 22, 82)  # West lower opening
    c.fill(31, 70, 32, 74)  # East upper opening
    c.fill(31, 79, 32, 82)  # East lower opening

    # Horizontal divider wall between lower colonnades and main cemetery (y=65..66)
    c.wall(2, 65, 22, 66)
    c.wall(31, 65, 51, 66)
    c.fill(10, 65, 14, 66)  # West passage north
    c.fill(39, 65, 43, 66)  # East passage north

    # Colonnade statues/pillars along the outer lower walls (2x2 crisp stone piers)
    for py in (70, 75, 80):
        c.wall(2, py, 3, py + 1)
        c.wall(50, py, 51, py + 1)

    # 4. Central Statue Plaza pedestal (matching the gargoyle statue in graveyard.jpg)
    c.wall(26, 56, 27, 57)

    # 5. West Mausoleum (against west perimeter wall, slightly further south)
    c.wall(2, 50, 12, 61)
    c.fill(4, 52, 10, 59)      # Mausoleum interior
    c.fill(11, 54, 12, 57)     # 4-wide east entrance facing cemetery

    # 6. East Mausoleum (against east perimeter wall, slightly further north)
    c.wall(41, 43, 51, 54)
    c.fill(43, 45, 49, 52)     # Mausoleum interior
    c.fill(41, 47, 42, 50)     # 4-wide west entrance facing cemetery

    # 7. Upper cemetery divider walls (y=23..24) with wide passages
    c.wall(2, 23, 18, 24)
    c.wall(35, 23, 51, 24)
    c.fill(6, 23, 9, 24)       # West gate into upper cemetery
    c.fill(44, 23, 47, 24)     # East gate into upper cemetery

    # 8. North Main Crypt / Mausoleum (centered against north wall)
    c.wall(20, 2, 33, 16)
    c.fill(22, 4, 31, 14)      # Spacious crypt chamber
    c.fill(25, 15, 28, 16)     # 4-wide south doorway facing central avenue

    return c.rows()


def build_library():
    """Faithful to images/Maps/ScarletMonastery/library.jpg (112x76)."""
    c = Canvas(112, 76)

    # 1. Far-left entry gallery running south into the Entry Courtyard
    c.fill(2, 22, 7, 50)
    c.fill(7, 44, 10, 47)      # West gate into Entry Courtyard

    # 2. Entry Cloister Courtyard (square cloister + open grassy center)
    c.fill(10, 42, 29, 59)
    # Inner cloister colonnade border with wide 4-tile openings on all 4 sides
    c.wall(14, 46, 25, 55)
    c.fill(16, 48, 23, 53)     # Walkable grassy inner courtyard
    c.fill(18, 46, 21, 47)     # North archway
    c.fill(18, 54, 21, 55)     # South archway
    c.fill(14, 49, 15, 52)     # West archway
    c.fill(24, 49, 25, 52)     # East archway

    # 3. Southern Kennel / Entry Vestibule (extends to y=74 so entrance = bottom-left)
    c.fill(18, 59, 21, 63)     # 4-wide south passage
    c.fill(14, 63, 25, 74)     # Southern kennel / entry chamber

    # 4. West Cloister Gallery (north-south hall east of courtyard)
    c.fill(29, 52, 34, 55)     # East doorway from courtyard
    c.fill(34, 28, 39, 56)     # Long north-south cloister gallery
    c.fill(26, 31, 34, 36)     # West study alcove near north end

    # Cobblestone bypass loop (southeast of West Cloister Gallery -> West Athenaeum)
    c.fill(39, 41, 54, 44)
    c.fill(51, 32, 54, 44)

    # 5. West Athenaeum (north-south red-carpet hall with two west study alcoves)
    c.fill(39, 31, 43, 34)     # Connection from West Cloister Gallery
    c.fill(42, 9, 50, 35)      # Main north-south athenaeum
    c.fill(33, 11, 42, 15)     # Upper west study alcove
    c.fill(33, 21, 42, 25)     # Lower west study alcove
    c.fill(49, 32, 52, 35)     # Connection to cobblestone bypass loop

    # 6. Twin Pillared Main Library Galleries (east-west across the north)
    c.fill(50, 12, 55, 15)     # Corridor into West Main Reading Hall
    # West Main Reading Hall
    c.fill(55, 6, 71, 21)
    c.wall(62, 10, 63, 11)     # North pillar
    c.wall(62, 16, 63, 17)     # South pillar
    # Divider wall with 4-wide central archway
    c.wall(72, 6, 73, 21)
    c.fill(72, 12, 73, 15)
    # East Main Reading Hall
    c.fill(74, 6, 90, 21)
    c.wall(81, 10, 82, 11)     # North pillar
    c.wall(81, 16, 82, 17)     # South pillar

    # 7. East Reading Hall (north-south red-carpet hall on the east)
    c.fill(90, 12, 93, 15)     # Archway into East Reading Hall
    c.fill(93, 9, 101, 36)     # Main north-south hall
    c.fill(101, 12, 106, 15)   # Small eastern alcove
    c.fill(85, 31, 93, 36)     # Southwest study chamber

    # 8. Pillared Vestibule (symmetrical chamber north of Doan's Sanctum)
    c.fill(95, 36, 98, 40)     # 4-wide south passage
    c.fill(89, 40, 104, 54)    # Vestibule chamber
    c.wall(93, 46, 94, 47)     # West pillar
    c.wall(99, 46, 100, 47)    # East pillar

    # 9. Arcanist Doan's Circular Sanctum (bottom-right rotunda, max y=72 < 74)
    c.fill(95, 54, 98, 57)     # 4-wide sanctum entrance
    c.circle(96.5, 64.0, 8.2)

    return c.rows()


def build_armory():
    """Faithful to images/Maps/ScarletMonastery/armory.jpg (62x114)."""
    c = Canvas(62, 114)

    # 1. South entry corridor running east from bottom-left
    c.fill(2, 108, 26, 111)
    c.fill(2, 108, 6, 112)     # Bottom-left entry anchor

    # 2. Training Courtyard (rectangular cloister + inner training yard)
    c.fill(21, 104, 24, 108)   # 4-wide south gate
    c.fill(18, 87, 35, 104)    # Outer courtyard cloister
    # Inner yard framing pillars/low walls with wide 4-tile openings on all 4 sides
    c.wall(22, 91, 31, 100)
    c.fill(24, 93, 29, 98)     # Walkable grassy training yard
    c.fill(25, 91, 28, 92)     # North opening (4-wide)
    c.fill(25, 99, 28, 100)    # South opening (4-wide)
    c.fill(22, 94, 23, 97)     # West opening (4-wide)
    c.fill(30, 94, 31, 97)     # East opening (4-wide)

    # 3. North Staircase & Vestibule into Armory Hall 1
    c.fill(26, 82, 29, 87)     # 4-wide stairs north
    c.fill(24, 76, 49, 82)     # L-shaped vestibule running east

    # 4. Armory Hall 1 (Lower Barracks/Armory with 2 central pillars)
    c.fill(31, 57, 49, 76)
    c.wall(39, 62, 40, 63)     # North central pillar
    c.wall(39, 70, 40, 71)     # South central pillar

    # 5. Eastern Side-Gallery & Stair Landing (bypassing the solid wall between halls)
    c.fill(49, 60, 52, 63)     # 4-wide east doorway from Armory Hall 1
    c.fill(52, 59, 58, 64)     # South stair landing
    c.fill(54, 51, 57, 59)     # 4-wide north staircase
    c.fill(52, 46, 58, 51)     # North stair landing
    c.fill(49, 47, 52, 50)     # 4-wide west doorway into Armory Hall 2

    # 6. Armory Hall 2 (Upper Armory / Weapons Hall with 2 central pillars)
    c.fill(31, 29, 49, 53)
    c.wall(39, 35, 40, 36)     # North central pillar
    c.wall(39, 44, 40, 45)     # South central pillar

    # 7. Northwest Guardroom & Long Upper Corridor running east to Herod's Arena
    c.fill(28, 31, 31, 34)     # 4-wide west doorway from Armory Hall 2 (2-tile stone wall at x=29..30)
    c.fill(23, 30, 28, 36)     # NW Guardroom
    c.fill(24, 20, 27, 30)     # 4-wide north passage
    c.fill(24, 20, 52, 23)     # Long straight eastbound hallway

    # 8. Herod's Circular Arena (top-right grand rotunda, 2-tile archway wall at y=18..19)
    c.fill(45, 16, 48, 20)     # 4-wide grand arena archway
    c.circle(46.5, 9.5, 8.2)

    return c.rows()


def build_cathedral():
    """Faithful to images/Maps/ScarletMonastery/cathedral.jpg (68x117)."""
    c = Canvas(68, 117)

    # 1. South entry corridor running west to the Lower Courtyard gate
    # 5 tiles tall (y=111..115) so the party's 5x5 roominess anchor stays in
    # the south entry vestibule instead of jumping north into the Lower Courtyard.
    c.fill(31, 111, 58, 115)

    # 2. Lower Courtyard with central Reflecting Pool
    c.fill(32, 107, 35, 111)   # 4-wide south gate
    c.fill(14, 88, 53, 107)    # Lower courtyard & flanking walkways
    c.wall(28, 88, 39, 100)    # Central reflecting pool (walk around east/west)

    # 3. Twin Staircases flanking the central waterfall canal
    c.fill(21, 83, 25, 88)     # West staircase
    c.fill(42, 83, 46, 88)     # East staircase

    # 4. Upper Fountain Courtyard with flanking cloisters & semicircular fountain
    c.fill(5, 56, 62, 83)      # Full upper courtyard + cloisters
    # Cloister colonnade piers separating covered galleries from grassy plaza
    for cy in (60, 68, 76):
        c.wall(11, cy, 12, cy + 1)
        c.wall(55, cy, 56, cy + 1)
    # Semicircular central fountain basin (impassable water landmark)
    c.wall(24, 75, 43, 83)
    c.circle(33.5, 76.0, 7.5, "#")
    # Ensure twin staircases open cleanly beside the fountain (4+ tiles wide)
    c.fill(19, 76, 23, 83)
    c.fill(44, 76, 48, 83)

    # 5. Grand Cathedral Entrance Steps into the Basilica Nave
    c.fill(29, 52, 38, 56)     # Wide 10-tile entrance steps

    # 6. Grand Basilica Nave with 2 rows of 3 square pillars (6 pillars total)
    c.fill(17, 31, 50, 52)
    for py in (34, 41, 48):
        c.wall(25, py, 26, py + 1)
        c.wall(41, py, 42, py + 1)

    # 7. Cruciform West & East Transepts (each with a central pillar)
    c.fill(2, 21, 65, 31)
    c.wall(9, 25, 10, 26)      # West transept pillar
    c.wall(57, 25, 58, 26)     # East transept pillar

    # 8. High Altar Sanctum (raised northern dais)
    c.fill(19, 14, 48, 21)
    c.wall(25, 19, 26, 20)     # West sanctuary pillar
    c.wall(41, 19, 42, 20)     # East sanctuary pillar

    # 9. Northwest & Northeast Side Chambers (including Fairbanks's Secret Chamber)
    # 2-tile masonry walls between chambers, sanctum, and transepts; 4-wide doorways.
    # NW chambers
    c.fill(2, 13, 8, 18)
    c.fill(4, 18, 7, 21)       # 4-wide doorway
    c.fill(11, 13, 16, 18)
    c.fill(12, 18, 15, 21)     # 4-wide doorway
    # NE chambers (Fairbanks's secret chamber)
    c.fill(51, 13, 56, 18)
    c.fill(52, 18, 55, 21)     # 4-wide doorway
    c.fill(59, 13, 65, 18)
    c.fill(60, 18, 63, 21)     # 4-wide doorway

    # 10. Whitemane's Circular Apse / Sanctum (top center)
    c.fill(32, 11, 35, 14)     # 4-wide passage behind high altar
    c.circle(33.5, 6.5, 5.5)

    return c.rows()


def components(grid, floor="."):
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


def is_pinch(grid, x, y):
    w, h = len(grid[0]), len(grid)
    if grid[y][x] != ".":
        return False
    fl = [
        (nx, ny)
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))
        if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] == "."
    ]
    if len(fl) != 2:
        return False
    (ax, ay), (bx, by) = fl
    return (ax == bx and ay + by == 2 * y) or (ay == by and ax + bx == 2 * x)


def pinch_count(grid):
    w, h = len(grid[0]), len(grid)
    return sum(1 for y in range(h) for x in range(w) if is_pinch(grid, x, y))


def thin_wall_count(grid):
    """Count interior 1-tile-thick walls separating floor on opposite orthogonal sides."""
    w, h = len(grid[0]), len(grid)
    count = 0
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            if grid[y][x] != "#":
                continue
            if (grid[y][x - 1] == "." and grid[y][x + 1] == ".") or (
                grid[y - 1][x] == "." and grid[y + 1][x] == "."
            ):
                count += 1
    return count


def crop(grid):
    w, h = len(grid[0]), len(grid)
    pts = [(x, y) for y in range(h) for x in range(w) if grid[y][x] == "."]
    x0, x1 = min(p[0] for p in pts), max(p[0] for p in pts)
    y0, y1 = min(p[1] for p in pts), max(p[1] for p in pts)
    inner = [grid[y][x0 : x1 + 1] for y in range(y0, y1 + 1)]
    iw = len(inner[0]) + 2
    return ["#" * iw] + ["#" + row + "#" for row in inner] + ["#" * iw]


WINGS = [
    ("graveyard", build_graveyard),
    ("library", build_library),
    ("armory", build_armory),
    ("cathedral", build_cathedral),
]


def main():
    blocks = []
    for name, builder in WINGS:
        grid = crop(builder())
        comps = components(grid)
        floor = sum(r.count(".") for r in grid)
        pinches = pinch_count(grid)
        thin_walls = thin_wall_count(grid)
        assert len(comps) == 1, f"{name}: expected 1 connected component, got {len(comps)}"
        assert pinches == 0, f"{name}: found {pinches} 1-wide pinches"
        assert thin_walls == 0, f"{name}: found {thin_walls} 1-tile-thick interior walls"
        assert floor > 500, f"{name}: expected > 500 floor tiles, got {floor}"
        print(
            f"{name}: {len(grid[0])}x{len(grid)} floor={floor} comps={len(comps)} pinch={pinches} thin={thin_walls}",
            file=sys.stderr,
        )
        body = "\n        ".join(f'"{r}"' for r in grid)
        blocks.append(f"  let {name} : Grid =\n    Grid.ofRows\n      [ {body} ]")

    header = """namespace WowRoguelike.Core

/// The four Scarlet Monastery wings, each one whole-wing architectural Grid
/// modeled after the reference battlemaps in images/Maps/ScarletMonastery/
/// by tools/extract_sm_maps.py. Every wing is a single connected walkable
/// region with rectilinear stone halls, courtyards, pillars, and circular
/// boss sanctums, and zero 1-wide pinches (every doorway is >= 2 tiles wide).
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

    out = header + "\n\n".join(blocks) + trailer
    path = "src/WowRoguelike.Core/MapsScarlet.fs"
    with open(path, "w") as f:
        f.write(out)
    print(f"wrote {path}", file=sys.stderr)


if __name__ == "__main__":
    main()
