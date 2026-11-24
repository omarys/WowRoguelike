#!/usr/bin/env python3
"""Extract Wailing Caverns section rooms AND the full stitched world map from
images/Maps/WailingCaverns/ and emit src/WowRoguelike.Core/Maps.fs.

Section rooms (Maps.rooms):
  Extracted from the 5 section battlemaps (the-screaming-gully, dreamers-rock,
  the-pit-of-fangs, crag-of-the-everliving, the-winding-chasm) into 7 connected
  Grid fixtures.

Stitched dungeon (Maps.stitch):
  Extracted directly from WorldMap-WailingCaverns.webp so the full dungeon
  preserves Wailing Caverns' authentic branching/looping cavern topology:
    - Entrance tunnel (south-center) -> The Screaming Gully (central hub)
    - North from Gully -> Dreamer's Rock + Dreamer's Rock East (pool cove)
    - West from Gully -> The Pit of Fangs (western C-shaped loop)
    - Southeast from Gully -> The Winding Chasm -> Crag of the Everliving
      (sweeping eastern outer ring + inner chamber) + Crag of the Everliving South
"""
import os
from collections import deque
from pathlib import Path
from PIL import Image

IMG_DIR = Path(os.environ.get("WC_IMG_DIR", "images/Maps/WailingCaverns"))
OUT_FS = Path("src/WowRoguelike.Core/Maps.fs")

SECTION_IMAGES = [
    "the-screaming-gully",
    "dreamers-rock",
    "the-pit-of-fangs",
    "crag-of-the-everliving",
    "the-winding-chasm",
]
THRESHOLD = 25  # drop tiny art-noise components


def classify_section_pixel(r: int, g: int, b: int) -> str:
    mx = max(r, g, b)
    mn = min(r, g, b)
    if mx < 40:
        return "#"
    if (mx - mn) < 30 and mx < 90:
        return "#"
    return "."


def cleanup(grid: list[str], passes: int = 2) -> list[str]:
    w, h = len(grid[0]), len(grid)
    g = [list(r) for r in grid]
    for _ in range(passes):
        ng = [r[:] for r in g]
        for y in range(h):
            for x in range(w):
                floors = 0
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        if dx == 0 and dy == 0:
                            continue
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < w and 0 <= ny < h and g[ny][nx] == ".":
                            floors += 1
                if g[y][x] == "." and floors <= 1:
                    ng[y][x] = "#"
                elif g[y][x] != "." and floors >= 6:
                    ng[y][x] = "."
        g = ng
    return ["".join(r) for r in g]


def components(grid: list[str]) -> list[list[tuple[int, int]]]:
    w, h = len(grid[0]), len(grid)
    seen = [[False] * w for _ in range(h)]
    out = []
    for sy in range(h):
        for sx in range(w):
            if grid[sy][sx] != "." or seen[sy][sx]:
                continue
            q = [(sx, sy)]
            seen[sy][sx] = True
            comp = []
            while q:
                x, y = q.pop()
                comp.append((x, y))
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < w and 0 <= ny < h and grid[ny][nx] == "." and not seen[ny][nx]:
                        seen[ny][nx] = True
                        q.append((nx, ny))
            out.append(comp)
    return out


def crop_component(grid: list[str], comp: list[tuple[int, int]]) -> list[str]:
    w, h = len(grid[0]), len(grid)
    comp_set = set(comp)
    xs = [x for x, _ in comp]
    ys = [y for _, y in comp]
    x0, x1 = max(0, min(xs) - 1), min(w - 1, max(xs) + 1)
    y0, y1 = max(0, min(ys) - 1), min(h - 1, max(ys) + 1)
    rows = []
    for y in range(y0, y1 + 1):
        rows.append("".join("." if (x, y) in comp_set else "#" for x in range(x0, x1 + 1)))
    return rows


def compass_direction(a: list[tuple[int, int]], b: list[tuple[int, int]]) -> str:
    ax = sum(x for x, _ in a) / len(a)
    ay = sum(y for _, y in a) / len(a)
    bx = sum(x for x, _ in b) / len(b)
    by = sum(y for _, y in b) / len(b)
    dx, dy = bx - ax, by - ay
    if abs(dx) > abs(dy):
        return "east" if dx > 0 else "west"
    return "south" if dy > 0 else "north"


def widen_pinches(grid: list[list[str]]) -> None:
    """Widen any 1-tile horizontal or vertical corridor pinch so a 5-hero party
    never deadlocks on a single-tile bottleneck."""
    h, w = len(grid), len(grid[0])
    for y in range(2, h - 2):
        for x in range(2, w - 2):
            if grid[y][x] == ".":
                if grid[y - 1][x] == "#" and grid[y + 1][x] == "#" and grid[y][x - 1] == "." and grid[y][x + 1] == ".":
                    grid[y - 1][x] = "."
                if grid[y][x - 1] == "#" and grid[y][x + 1] == "#" and grid[y - 1][x] == "." and grid[y + 1][x] == ".":
                    grid[y][x - 1] = "."


def extract_section_rooms(target_w: int = 56) -> list[tuple[str, list[str], int]]:
    entries = []
    for name in SECTION_IMAGES:
        path = IMG_DIR / f"{name}.webp"
        im = Image.open(path).convert("RGB")
        iw, ih = im.size
        h = round(ih * target_w / iw)
        im = im.resize((target_w, h), Image.Resampling.BILINEAR)
        px = im.load()
        rows = []
        for y in range(h):
            rows.append("".join(classify_section_pixel(*px[x, y]) for x in range(target_w)))
        grid = cleanup(rows, passes=2)
        comps = [c for c in components(grid) if len(c) >= THRESHOLD]
        comps.sort(key=len, reverse=True)
        for i, comp in enumerate(comps):
            room = crop_component(grid, comp)
            rg = [list(r) for r in room]
            widen_pinches(rg)
            room = ["".join(r) for r in rg]
            cnt = sum(r.count(".") for r in room)
            rname = name if i == 0 else f"{name}-{compass_direction(comps[0], comp)}"
            entries.append((rname, room, cnt))
    return entries


# ---------------------------------------------------------------------------
# WorldMap-WailingCaverns.webp extraction for the full stitched dungeon
# ---------------------------------------------------------------------------

def _lum(rgb: tuple[int, int, int]) -> float:
    r, g, b = rgb
    return (0.299 * r + 0.587 * g + 0.114 * b) / 255.0


def _flood_region(px, w: int, h: int, seeds: list[tuple[int, int]], thresh: float,
                  bounds: tuple[int, int, int, int]) -> set[tuple[int, int]]:
    x_min, y_min, x_max, y_max = bounds
    seen: set[tuple[int, int]] = set()
    q: deque[tuple[int, int]] = deque()
    for sx, sy in seeds:
        if _lum(px[sx, sy]) >= thresh and (sx, sy) not in seen:
            seen.add((sx, sy))
            q.append((sx, sy))
    while q:
        x, y = q.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if x_min <= nx <= x_max and y_min <= ny <= y_max and (nx, ny) not in seen:
                if _lum(px[nx, ny]) >= thresh:
                    seen.add((nx, ny))
                    q.append((nx, ny))
    return seen


def _carve_brush(grid: list[list[str]], x: int, y: int, ch: str, radius: int = 1):
    h, w = len(grid), len(grid[0])
    for dy in range(-radius, radius + 1):
        for dx in range(-radius, radius + 1):
            nx, ny = x + dx, y + dy
            if 1 <= nx < w - 1 and 1 <= ny < h - 1:
                grid[ny][nx] = ch


def _carve_path(grid: list[list[str]], p0: tuple[int, int], p1: tuple[int, int], ch: str, radius: int = 1):
    x0, y0 = p0
    x1, y1 = p1
    steps = max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(steps + 1):
        x = round(x0 + (x1 - x0) * i / steps)
        y = round(y0 + (y1 - y0) * i / steps)
        _carve_brush(grid, x, y, ch, radius)


def extract_world_map(gw: int = 100, gh: int = 80) -> tuple[list[str], tuple[int, int]]:
    """Extract the full branching/looping Wailing Caverns layout from
    WorldMap-WailingCaverns.webp and tag every floor cell with its region:
      G = the-screaming-gully
      D = dreamers-rock
      M = dreamers-rock-east
      P = the-pit-of-fangs
      C = the-winding-chasm
      E = crag-of-the-everliving
      S = crag-of-the-everliving-south
    """
    im = Image.open(IMG_DIR / "WorldMap-WailingCaverns.webp").convert("RGB")
    w, h = im.size
    px = im.load()

    # Flood-fill each cavern interior component inside the dark ink wall outline.
    # Each flood-fill is tightly bounded to prevent any parchment leakage.
    interior: set[tuple[int, int]] = set()
    # 1. Dreamer's Rock + Screaming Gully main cavern & waterways
    interior |= _flood_region(
        px, w, h,
        [(350, 110), (295, 115), (415, 95), (420, 235), (252, 308), (510, 292)],
        thresh=0.45, bounds=(200, 30, 535, 340),
    )
    # 2. Lady Anacondra's pink southern ledge in Screaming Gully
    interior |= _flood_region(
        px, w, h,
        [(309, 285)],
        thresh=0.43, bounds=(265, 255, 355, 325),
    )
    # 3. Pit of Fangs (western wing: upper curled gold chamber, far-west loop, south lobe)
    interior |= _flood_region(
        px, w, h,
        [(175, 255), (70, 240), (155, 375)],
        thresh=0.45, bounds=(20, 135, 230, 415),
    )
    # 4. Winding Chasm + Crag of the Everliving (central-east & southeast figure-8)
    interior |= _flood_region(
        px, w, h,
        [(550, 275), (565, 325), (610, 360), (540, 450), (590, 495), (724, 442)],
        thresh=0.44, bounds=(500, 265, 765, 585),
    )
    # 4b. Crag of the Everliving inner pink chamber (Verdan the Everliving's chamber)
    interior |= _flood_region(
        px, w, h,
        [(619, 495), (615, 455), (625, 485)],
        thresh=0.40, bounds=(575, 415, 655, 535),
    )
    # 5. Crag of the Everliving South (southernmost loop — Skum's lair)
    interior |= _flood_region(
        px, w, h,
        [(560, 595)],
        thresh=0.45, bounds=(510, 530, 615, 630),
    )

    xs = [x for x, _ in interior]
    ys = [y for _, y in interior]
    x0, x1 = min(xs) - 10, max(xs) + 10
    y0, y1 = min(ys) - 10, max(ys) + 10

    def to_grid(ix: int, iy: int) -> tuple[int, int]:
        gx = round((ix - x0) * (gw - 2) / (x1 - x0)) + 1
        gy = round((iy - y0) * (gh - 2) / (y1 - y0)) + 1
        return max(1, min(gw - 2, gx)), max(1, min(gh - 2, gy))

    # Downsample to (gw, gh) with 1-cell solid wall border
    raw = [["#"] * gw for _ in range(gh)]
    for gy in range(1, gh - 1):
        for gx in range(1, gw - 1):
            cnt = 0
            for sy in range(4):
                for sx in range(4):
                    ix = int(x0 + (gx - 1 + (sx + 0.5) / 4) * (x1 - x0) / (gw - 2))
                    iy = int(y0 + (gy - 1 + (sy + 0.5) / 4) * (y1 - y0) / (gh - 2))
                    if (ix, iy) in interior:
                        cnt += 1
            if cnt >= 5:
                raw[gy][gx] = "."

    # Clean up tiny speckles
    cleaned = cleanup(["".join(r) for r in raw], passes=2)
    g = [list(r) for r in cleaned]

    # Ensure authentic connections from WorldMap-WailingCaverns.webp are >= 3 tiles wide
    # so the 5-hero party never stalls at a 1-tile bridge or ink divider:
    # 1. Entrance tunnel from the spiral portal (461, 382) winding NW into Screaming Gully
    ent_pts = [
        to_grid(461, 382),
        to_grid(452, 345),
        to_grid(420, 325),
        to_grid(380, 315),
        to_grid(365, 290),
        to_grid(365, 255),
    ]
    for a, b in zip(ent_pts, ent_pts[1:]):
        _carve_path(g, a, b, ".", radius=1)
    # Slight entrance vestibule at the spiral portal so the 5-hero party spawns cleanly
    entrance = ent_pts[0]
    _carve_brush(g, entrance[0], entrance[1], ".", radius=2)

    # 2. Screaming Gully -> Anacondra's pink southern ledge
    _carve_path(g, to_grid(340, 265), to_grid(309, 285), ".", radius=1)
    _carve_path(g, to_grid(309, 285), to_grid(275, 260), ".", radius=1)

    # 3. Screaming Gully -> Dreamer's Rock (northern neck)
    _carve_path(g, to_grid(372, 185), to_grid(365, 140), ".", radius=1)

    # 4. Screaming Gully -> Pit of Fangs (western water bridge & Pit internal loop)
    _carve_path(g, to_grid(275, 265), to_grid(250, 305), ".", radius=1)
    _carve_path(g, to_grid(250, 305), to_grid(200, 295), ".", radius=1)
    _carve_path(g, to_grid(200, 295), to_grid(170, 265), ".", radius=1)
    _carve_path(g, to_grid(200, 295), to_grid(145, 330), ".", radius=1)
    _carve_path(g, to_grid(145, 330), to_grid(155, 365), ".", radius=1)
    _carve_path(g, to_grid(115, 320), to_grid(90, 295), ".", radius=1)

    # 5. Screaming Gully -> Winding Chasm (eastern diagonal river crossing + north cove)
    _carve_path(g, to_grid(455, 250), to_grid(510, 290), ".", radius=1)
    _carve_path(g, to_grid(510, 290), to_grid(555, 320), ".", radius=1)
    _carve_path(g, to_grid(550, 275), to_grid(565, 325), ".", radius=1)

    # 6. Winding Chasm -> Crag of the Everliving (inner pink chamber, western path, outer east ring)
    _carve_path(g, to_grid(575, 385), to_grid(545, 435), ".", radius=1)
    _carve_path(g, to_grid(610, 380), to_grid(615, 445), ".", radius=1)
    _carve_path(g, to_grid(615, 445), to_grid(619, 495), ".", radius=1)
    _carve_path(g, to_grid(619, 495), to_grid(605, 520), ".", radius=1)
    _carve_path(g, to_grid(635, 390), to_grid(695, 415), ".", radius=1)
    _carve_path(g, to_grid(695, 415), to_grid(730, 465), ".", radius=1)
    _carve_path(g, to_grid(730, 465), to_grid(705, 535), ".", radius=1)
    _carve_path(g, to_grid(705, 535), to_grid(635, 555), ".", radius=1)
    _carve_path(g, to_grid(635, 555), to_grid(605, 520), ".", radius=1)
    _carve_path(g, to_grid(545, 460), to_grid(585, 515), ".", radius=1)

    # 7. Crag of the Everliving -> Crag of the Everliving South (southern loop)
    _carve_path(g, to_grid(585, 520), to_grid(565, 565), ".", radius=1)
    _carve_path(g, to_grid(565, 565), to_grid(560, 595), ".", radius=1)
    _carve_path(g, to_grid(635, 555), to_grid(585, 580), ".", radius=1)

    # Eliminate any 1-tile corridor pinches before taking the main component
    widen_pinches(g)

    # Keep only the single main connected component (and preserve 1-cell border)
    for x in range(gw):
        g[0][x] = "#"
        g[gh - 1][x] = "#"
    for y in range(gh):
        g[y][0] = "#"
        g[y][gw - 1] = "#"

    comps = components(["".join(r) for r in g])
    comps.sort(key=len, reverse=True)
    main_comp = set(comps[0])

    # Assign every floor tile to its exact Wailing Caverns region based on WorldMap coordinates.
    def region_of(gx: int, gy: int) -> str:
        # Map grid coords back to original WorldMap-WailingCaverns.webp pixel coords
        ix = x0 + (gx - 1) * (x1 - x0) / (gw - 2)
        iy = y0 + (gy - 1) * (y1 - y0) / (gh - 2)

        # Pit of Fangs (western wing, x < 235) — check before iy < 162 so its
        # northern curve is never misclassified as Dreamer's Rock.
        if ix < 235:
            return "P"      # the-pit-of-fangs

        # Dreamer's Rock (north chamber above y=162)
        if iy < 162:
            # Eastern pool cove where Mutanus emerges from the water
            if ix >= 382:
                return "M"  # dreamers-rock-east
            return "D"      # dreamers-rock

        # Screaming Gully (central hub + entrance tunnel, x < 495)
        if ix < 495:
            return "G"      # the-screaming-gully

        # Winding Chasm (diagonal river + green/purple cavern, y < 405)
        if iy < 405:
            return "C"      # the-winding-chasm

        # Crag of the Everliving South (southern loop + lower approach, y >= 542 and x < 665)
        if iy >= 542 and ix < 665:
            return "S"      # crag-of-the-everliving-south

        # Crag of the Everliving (inner chamber + sweeping eastern outer ring)
        return "E"          # crag-of-the-everliving

    tagged_rows = []
    for y in range(gh):
        row = []
        for x in range(gw):
            if (x, y) in main_comp:
                row.append(region_of(x, y))
            else:
                row.append("#")
        tagged_rows.append("".join(row))

    return tagged_rows, entrance


def emit_maps_fs(
    section_entries: list[tuple[str, list[str], int]],
    world_rows: list[str],
    entrance: tuple[int, int],
) -> str:
    blocks = []
    for rname, room, _ in section_entries:
        rows = "\n            ".join(f'"{r}"' for r in room)
        blocks.append(f'      "{rname}",\n        Grid.ofRows\n          [ {rows} ]')
    rooms_body = "\n".join(blocks)

    world_body = "\n      ".join(f'"{r}"' for r in world_rows)
    ex, ey = entrance

    return f"""namespace WowRoguelike.Core

/// Wailing Caverns rooms and stitched world layout, extracted from the sample
/// maps in images/Maps/WailingCaverns/ by tools/extract_wc_maps.py.
///
/// - `rooms`: the 7 individual section rooms extracted from the 5 section maps.
/// - `stitch`: the full branching/looping Wailing Caverns dungeon extracted
///   directly from `WorldMap-WailingCaverns.webp`, with every floor cell tagged
///   by its cavern region (`G` = the-screaming-gully, `D` = dreamers-rock,
///   `M` = dreamers-rock-east, `P` = the-pit-of-fangs, `C` = the-winding-chasm,
///   `E` = crag-of-the-everliving, `S` = crag-of-the-everliving-south).
module Maps =

  /// All 7 extracted section rooms, largest-per-section first, each a connected Grid.
  let rooms : (string * Grid) list =
    [
{rooms_body}
    ]

  // =========================================================================
  // Full Wailing Caverns layout — extracted from WorldMap-WailingCaverns.webp
  // =========================================================================

  let private worldLayout : string list =
    [ {world_body} ]

  let private worldEntrance : Pos = {{ X = {ex}; Y = {ey} }}

  let private regionKey =
    function
    | 'G' -> Some "the-screaming-gully"
    | 'D' -> Some "dreamers-rock"
    | 'M' -> Some "dreamers-rock-east"
    | 'P' -> Some "the-pit-of-fangs"
    | 'C' -> Some "the-winding-chasm"
    | 'E' -> Some "crag-of-the-everliving"
    | 'S' -> Some "crag-of-the-everliving-south"
    | _ -> None

  /// Build the full Wailing Caverns dungeon from `worldLayout`. Deterministic.
  /// Returns the connected Grid, the entrance tile at the southern spiral portal,
  /// and each of the 7 room regions' floor tiles in canvas coordinates.
  /// `the-screaming-gully` tiles are ordered closest-to-entrance first so the
  /// party spawns cleanly at the entrance vestibule.
  let stitch () : Grid * Pos * Map<string, Pos list> =
    let height = List.length worldLayout
    let width = (List.head worldLayout).Length
    let canvas = Grid.createSolid width height

    let buckets =
      [ "the-screaming-gully"
        "the-winding-chasm"
        "the-pit-of-fangs"
        "crag-of-the-everliving"
        "crag-of-the-everliving-south"
        "dreamers-rock"
        "dreamers-rock-east" ]
      |> List.map (fun k -> k, ResizeArray<Pos>())
      |> Map.ofList

    worldLayout
    |> List.iteri (fun y row ->
      for x in 0 .. width - 1 do
        match regionKey row.[x] with
        | Some name ->
          canvas.Walls.[y * width + x] <- false
          buckets.[name].Add {{ X = x; Y = y }}
        | None -> ())

    let dist2 (a: Pos) (b: Pos) =
      let dx = a.X - b.X
      let dy = a.Y - b.Y
      dx * dx + dy * dy

    let prioritizeLandmarks (landmarks: Pos list) (tiles: Pos list) =
      let chosen = ResizeArray<Pos>()
      let used = System.Collections.Generic.HashSet<Pos>()

      for lm in landmarks do
        let best =
          tiles
          |> List.filter (fun p -> not (used.Contains p))
          |> List.sortBy (fun p -> dist2 p lm, p.Y, p.X)
          |> List.tryHead

        match best with
        | Some p ->
          used.Add p |> ignore
          chosen.Add p
        | None -> ()

      (Seq.toList chosen) @ (tiles |> List.filter (fun p -> not (used.Contains p)))

    let roomFloors =
      buckets
      |> Map.map (fun name tiles ->
        let list = Seq.toList tiles
        match name with
        | "the-screaming-gully" ->
          list |> List.sortBy (fun p -> dist2 p worldEntrance, p.Y, p.X)
        | "the-pit-of-fangs" ->
          // Place Cobrahn (upper gold curl), Pythas (south green alcove), Kresh (river loop)
          prioritizeLandmarks [ {{ X = 24; Y = 32 }}; {{ X = 19; Y = 47 }}; {{ X = 10; Y = 22 }} ] list
        | "crag-of-the-everliving" ->
          // Place Serpentis (sweeping outer east ridge) and Verdan (inner pink chamber)
          prioritizeLandmarks [ {{ X = 94; Y = 57 }}; {{ X = 81; Y = 62 }} ] list
        | "dreamers-rock-east" ->
          // Center Mutanus in the eastern pool cove
          prioritizeLandmarks [ {{ X = 53; Y = 9 }} ] list
        | _ -> list)

    canvas, worldEntrance, roomFloors
"""


def main():
    section_entries = extract_section_rooms(56)
    print("=== Section Rooms ===")
    for rname, room, cnt in section_entries:
        print(f"  {rname:30s}: {cnt:4d} floor cells ({len(room[0])}x{len(room)})")

    world_rows, entrance = extract_world_map(100, 80)
    print(f"\n=== Stitched World Map ({len(world_rows[0])}x{len(world_rows)}, entrance={entrance}) ===")
    counts: dict[str, int] = {}
    for r in world_rows:
        for ch in r:
            if ch != "#":
                counts[ch] = counts.get(ch, 0) + 1
    labels = {
        "G": "the-screaming-gully",
        "D": "dreamers-rock",
        "M": "dreamers-rock-east",
        "P": "the-pit-of-fangs",
        "C": "the-winding-chasm",
        "E": "crag-of-the-everliving",
        "S": "crag-of-the-everliving-south",
    }
    for k, name in labels.items():
        print(f"  {k} ({name:28s}): {counts.get(k, 0):4d} floor cells")
    print(f"  TOTAL floor cells: {sum(counts.values())}\n")
    for r in world_rows:
        print(r)

    fs_code = emit_maps_fs(section_entries, world_rows, entrance)
    OUT_FS.write_text(fs_code, encoding="utf-8")
    print(f"\nWrote {OUT_FS} ({len(fs_code)} bytes)")


if __name__ == "__main__":
    main()
