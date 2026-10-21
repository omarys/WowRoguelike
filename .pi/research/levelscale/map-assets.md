# Research: Can the dungeon art in `images/Maps/` become playable tile grids?

Scope: `images/Maps/` (6 dungeon folders, 29 files), `tools/extract_wc_maps.py`,
`src/WowRoguelike.Core/Maps.fs`. Read-only — no repository files were modified.

## Summary

Only one of the six dungeons (Wailing Caverns) is usable **as-is**, and only through
its five *section* images; its `WorldMap-WailingCaverns.webp` is a parchment scroll
map whose light paper background would classify as floor everywhere.
TheStockade `overview.jpg` and TheDeadmines `overhead.jpg` are the two highest-value
assets in the repo — 8000 px hand-painted **orthographic** whole-dungeon plans with
black negative space — and they are usable for **one whole-dungeon grid each**, but not
at the script's default 56-column scale (their walls are thinner than one cell).
Shadowfang Keep's 13 renders are orthographic and correctly one-room-per-file, but their
contrast is inverted (pale stone walls, dark flagstone floors), so the current
dark-walls/light-floor classifier is unreliable on them. Ragefire Chasm (345×376) and
Razorfen Kraul (650×636) are too small and label-covered to yield a room of usable size.

## How the classifier actually decides (read from source, not executed)

`tools/extract_wc_maps.py:24-31`:

```python
def classify(r, g, b):
    mx = max(r, g, b); mn = min(r, g, b)
    if mx < 40:                      # near-black -> wall
        return '#'
    if (mx - mn) < 30 and mx < 90:   # dark grey/olive -> wall
        return '#'
    return '.'                       # everything else -> floor
```

Three consequences drive every verdict below:

1. **Wall = darkness.** A wall pixel must have max channel < 40, or be a dark
   desaturated grey (max < 90 with channel spread < 30).
2. **Floor = brightness *or* colour.** Any pixel with max channel ≥ 90 is floor even if
   it is grey; any saturated pixel is floor even if it is dark. So **teal water, blue sea
   and orange lava all classify as walkable floor**.
3. **Negative space is load-bearing.** Because walls are defined by darkness, the area
   *outside* the dungeon must be black. A paper/parchment/mid-brown background turns the
   entire canvas into floor.

Resolution handling: `tools/extract_wc_maps.py:105-112` reads source dimensions with
`magick identify` and computes `h = round(ih * w / iw)`; `load()` (line 17) resizes with
`-resize f"{w}x{h}!"`. Width defaults to 56 (`sys.argv[1]`). So **aspect is free, and the
only scale knob is the column count** — source pixels per cell = `image_width / w`.

Room splitting: `components()` (line 69) + `THRESHOLD = 25` (line 13) emit **one Grid per
connected floor component of ≥ 25 cells**, cropped to its bounding box. A fully connected
dungeon therefore collapses into a single room, and labels/watermark text inside the level
silhouette become separate fake rooms.

Path note: the script currently reads `images/{name}.webp` (line 104) from a hardcoded
five-name `IMAGES` list, but the art now lives at `images/Maps/WailingCaverns/<name>.webp`.
As committed, the script's image paths do **not** resolve against the current layout.

---

## Wailing Caverns — `images/Maps/WailingCaverns/` (6 files)

| File | Dimensions | Bytes | Format | Type (observed) | Threshold verdict | Why |
|---|---|---|---|---|---|---|
| `the-screaming-gully.webp` | 1080×830 | 88,126 | WEBP | Top-down orthographic dungeon-map render | **Usable** | Black vignetted background = wall; floor warm tan/olive, teal river = floor; faint dark grid overlay too fine to survive downscale; white serif caption "THE SCREAMING GULLY" sits in the black bottom margin, not over the level |
| `dreamers-rock.webp` | 1080×848 | 78,794 | WEBP | Top-down orthographic | **Usable** | Green/tan floor on black; dark cave holes read as pillars/voids (correct); caption + reddit watermark in bottom black margin |
| `the-pit-of-fangs.webp` | 1080×1365 | 131,974 | WEBP | Top-down orthographic | **Usable** | Tall aspect → 56×71 grid; dark pit voids become pillars; teal pool at right edge becomes floor (walkable — deliberate choice); caption in margin |
| `the-winding-chasm.webp` | 1080×941 | 100,204 | WEBP | Top-down orthographic | **Usable** | Long winding corridors, floor light, walls dark; caption bottom-right in margin |
| `crag-of-the-everliving.webp` | 1080×1464 | 92,136 | WEBP | Top-down orthographic | **Usable** | Tallest aspect → 56×76; a bright-cream natural arch (light) creates a 1–2 cell fake floor above the lower chamber — the only visible artefact, absorbed by the ≥25-cell filter |
| `WorldMap-WailingCaverns.webp` | 1000×667 | 191,122 | WEBP | In-game parchment world map (scroll border, banner title) | **Not usable** | Paper background is mid-tan (max channel well over 90) → **whole canvas becomes floor**; "WAILING CAVERNS" ribbon banner is light; gold/dark scroll frame becomes wall around a floor slab. This is why it is absent from `IMAGES` in the extractor |

**Dungeon verdict: usable as-is for a multi-room grid** — the only dungeon in the repo
that is. It is exactly what the pipeline was built for. Yield per `TODO.md` ("`Maps.stitch`
joins the seven rooms"): **7 rooms from 5 sections** — most sections collapse to one
component because corridors were *removed* from the source art (rooms are detached islands
on black), which is the property that makes multi-room extraction work here.

---

## TheStockade — `images/Maps/TheStockade/` (1 file)

| File | Dimensions | Bytes | Format | Type (observed) | Threshold verdict | Why |
|---|---|---|---|---|---|---|
| `overview.jpg` | 8192×4202 | 3,791,849 | **PNG content, `.jpg` name** | Hand-painted VTT battlemap (Sanctum of Maps credit), **orthographic top-down** | **One whole-dungeon grid only, needs re-scaling** | See below |

What the image actually contains: ~30 discrete rectangular and circular cells arranged
along a lit tan/gold corridor cross, on pure black. Structural notes:

- **Geometry is ideal**: pure orthographic, no tilt, rooms are rectangles and circles,
  black negative space, an unambiguous single connected layout.
- **Contrast is partly inverted**: unlit room floors are *dark* stone (dark grey
  cobble, plausibly straddling the max-channel < 90 boundary), while the pillared
  corridors are lit warm tan (clearly floor). Walls are **pale grey stone trim** (well
  above 90 → floor). So walls may vanish and dark room interiors may become walls,
  leaving only the lit corridor cross as floor. **This is the main risk.**
- **Fake floors**: the light-grey "THE STOCKADE" title plaque (top-right) and the white
  "Sanctum of Maps / PATREON" logo (bottom-right). Both sit in the black margin and at
  56 columns are a few cells each — likely below the 25-cell threshold, but at higher
  column counts they will become fake rooms.
- **Scale is wrong for the default**: 8192 px / 56 = **146 px per cell**. The pale wall
  trim is roughly 60–100 px thick → **under one cell** → walls can disappear and rooms
  fuse into corridors. Run with `w`≈**160–200** for this file.
- Not in `IMAGES`; not a Wailing Caverns name; would need a `--dungeon` argument.

**Dungeon verdict: usable for ONE whole-dungeon grid**, at reduced column count per
source pixel (raise `w` to ~160–200) and after suppressing the title plaque/logo. Not
usable for a multi-room grid: every room opens onto the connecting corridor, so the whole
level is one connected floor component by construction.

---

## TheDeadmines — `images/Maps/TheDeadmines/` (6 files)

| File | Dimensions | Bytes | Format | Type (observed) | Threshold verdict | Why |
|---|---|---|---|---|---|---|
| `overhead.jpg` | 8000×4796 | 3,137,236 | **PNG content, `.jpg` name** | Hand-painted VTT battlemap (Sanctum of Maps), **orthographic whole dungeon** | **One whole-dungeon grid** | Cave floors are light brown/grey → floor; black surround → wall; ~8–10 chambers plus the galleon. Fails on the **blue sea** (saturated → a large fake floor region) and on the light banner + logo in the margins |
| `entrance.jpg` | 1439×1637 | 302,785 | JPEG | VTT battlemap, **orthographic, single room** | **Usable as one room, marginal** | Cave with a dark grey rock floor; the darker passages hug the wall threshold, the lit centre is clearly floor. Logo top-left → small fake floor |
| `forge.jpg` | 1925×2062 | 620,996 | JPEG | VTT battlemap, orthographic, single round room | **Usable as one room** | Mid-grey flagstone + brown plank ring; the lava centre and four lantern rings are saturated → floor; a black chain crosses the floor as a 1-cell fake wall |
| `sawmill.jpg` | 1998×1472 | 452,072 | JPEG | VTT battlemap, orthographic, single room | **Usable as one room** | Brown plank floor bridge between two rock areas, black void top-centre → wall; light logo top-right |
| `ship.jpg` | 1959×2227 | 732,193 | JPEG | VTT battlemap, orthographic, single room | **Usable as one room, water caveat** | Wooden deck (floor) surrounded by **bright blue water that classifies as floor**, so deck and sea merge into one blob; the 3–4 rock islands become the only interior walls |
| `alternate.webm` | 1080×720 | 51,030 | **WEBP, misnamed `.webm`** ("Windrosemaps" credit) | Schematic top-down dungeon plan, flat colours | **Not usable as-is** | Cleanest *geometry* in the repo (white/light cave floor, thin dark wall outlines, red marker circle, blue water) but the **background is mid-brown paper**, not black → background becomes floor and every wall dissolves. Needs the background flattened to black first |

**Dungeon verdict: twice usable.** `overhead.jpg` is a usable **one whole-dungeon grid**
(raise `w` to ~150–200; walls are 60–120 px, i.e. sub-cell at 56). The four per-room
battlemaps are usable **single-room grids** — each is already exactly one room, which is
the property the extractor wants. Note this contradicts the task brief's description of
these as "perspective screenshots": all four are orthographic top-down plans. The one
in-game-perspective-looking element is the galleon in `overhead.jpg`, drawn with slight 3D
shading but on a top-down plan.

---

## Shadowfang Keep — `images/Maps/ShadowfangKeep/` (13 files)

All thirteen are the same series: **orthographic top-down in-game renders** (square white
grid overlay, stone/wood/flagstone materials, strong baked directional light and cast
shadows), one room per file, room routing encoded in the **filename** rather than as
numbers drawn on the art. Contrast is the opposite of Wailing Caverns: **walls are the
pale stone (light), floors are dark flagstone / dark plank (dark)**.

| File | Dimensions | Bytes | Type | Threshold verdict | Why |
|---|---|---|---|---|---|
| `01 entrance_and_kennels.jpg` | 3000×2100 | 921,742 | Ortho render + grid overlay | **Not usable as-is** | Pale grey stone walls (≈200 max channel → **floor**); dark flagstone and dark wood floors straddle the wall boundary (max < 90); baked shadows across the floor become fake walls |
| `01 servants_quarters_hallway_to_5.1_stairs_to_4.1.jpg` | 3000×2100 | 928,593 | same series | **Not usable as-is** | Same inversion; not individually opened — judgement is from the series |
| `02 hallway_from_kennels_stairs_and_cells_door_leads_to_map_3_stairs.jpg` | 3000×2100 | 1,020,535 | same series | **Not usable as-is** | Series inference; note the name is long enough to break naive globbing/quoting |
| `03 Courtyard_right_door_leads_to_map_4_bottom_door_to_5.1_and_big_lever_for_gate.jpg` | 3000×2100 | 1,087,168 | Ortho render + grid overlay | **Not usable as-is** | Inspected: the light beige cobble courtyard *is* above threshold → the courtyard reads as one clean floor, which is the closest this series gets to working; the dark stone store room beside it goes to wall, and the red carpet is a saturated fake floor. Inconsistent within a single image |
| `04 diningroom_and_kitchen.jpg` | 3000×2100 | 952,329 | same series | **Not usable as-is** | Series inference |
| `06 hallway_from_5_to_7_command_center.jpg` | 1500×2100 | 471,838 | Ortho render, narrow strip | **Not usable as-is** | Inspected: dark plank floor + pale stone wall band on the bottom edge → walls become floor and floors become wall; strip aspect also means 56 columns lands ~27 px/cell on a 1-room-wide plan |
| `07 wall_below_is_01_hallway_round_thing_on_wall_is_chest.jpg` | 3000×2100 | 899,222 | same series | **Not usable as-is** | Series inference |
| `08 cellar_and_brewery_hidden_room_below_stone_upper_semi_level_entrance_in_brewery.jpg` | 3000×2100 | 983,742 | same series | **Not usable as-is** | Series inference |
| `09 lab_and_library_stairs_from_8_others_lead_to_10.jpg` | 3000×2100 | 934,150 | same series | **Not usable as-is** | Series inference |
| `10 BBEG_room_or_Arugals_lair.jpg` | 3000×2100 | 955,005 | Ortho render + grid overlay | **Not usable as-is** | Inspected: four materials in one room (dark flagstone, dark plank, cream tile, checkerboard) — cream tile → floor, flagstone → wall; the room fragments into disconnected floor patches. Large baked shadow across the middle adds fake walls |
| `4.1 stairs_from_diningroom_to_map_5.jpg` | 1800×2100 | 436,336 | same series | **Not usable as-is** | Series inference |
| `5.1 hallway.jpg` | 3000×2100 | 507,841 | same series | **Not usable as-is** | Series inference |
| `8.1 hidden_room.jpg` | 1200×1200 | 235,040 | Ortho render, square | **Not usable as-is** | Inspected and decisive: a single small room whose dark flagstone floor is surrounded by a **pale beige wall ring**. The ring classifies as floor and the floor as wall — the output would be a ring-shaped "room" around a solid core. This is the inversion at its clearest |

**Dungeon verdict: not usable as-is.** Resolution is fine (3000×2100 → 54 px/cell) and the
one-room-per-file structure is exactly right; what breaks it is **polarity plus baked
lighting**. `8.1 hidden_room.jpg` is the clean counter-example to prove it. A variant of
`classify()` with light→wall and dark→floor, plus shadow suppression, is the obvious fix;
the 5.1/8.1 room numbering is already a valid multi-room topology (13 rooms + connections
described in the filenames).

---

## Ragefire Chasm — `images/Maps/RagefireChasm/` (1 file)

| File | Dimensions | Bytes | Format | Type | Verdict | Why |
|---|---|---|---|---|---|---|
| `ragefire_chasm_map.jpg` | 345×376 | 29,539 | JPEG | Low-resolution top-down map thumbnail (blocky, JPEG-smeared) | **Not usable** | At 56 columns that is **6.2 px/cell**; corridors are ~8–14 px wide → 1–2 cells, so every room is a 1–3 cell blob. Polarity is inverted for this art too: the rock is near-black (**wall**, arguably correct) but the large interior areas are **saturated orange lava**, which classify as floor — the lava tubes become the walkable rooms. Yellow-green digits and an "E" marker are baked in and would become tiny fake floors. Needs a re-source, not a threshold tweak |

**Dungeon verdict: not usable.** Too small *and* the walkable/blocked polarity is wrong for
the level's own fiction (lava as floor).

---

## Razorfen Kraul — `images/Maps/RazorfenKraul/` (1 file)

| File | Dimensions | Bytes | Format | Type | Verdict | Why |
|---|---|---|---|---|---|---|
| `maprfk.jpg` | 650×636 | 46,949 | JPEG | In-game style **world-map render** at 3D wall shaded detail, with bright green vine/branch overlays — semi-overhead, not a flat plan | **Not usable as-is** | Three separate breaks: (1) **scale** — 650/56 = **11.6 px/cell**, corridors ~15–25 px → 1–2 cells; (2) **labels** — white/red numerals `1 2 3 4 5 6 7 8 9`, `CS` and `E` are drawn on the map, each a light blob that classifies as a fake floor patch; (3) **noise** — the light-green vine network is saturated → floor streaks, and the shaded 3D wall tops are bright enough to read as floor in places. Would need upscaling plus a de-label pass, i.e. it is a re-draw, not a conversion |

**Dungeon verdict: not usable.** Present only as a single thumbnail overview.

---

## Summary: dungeon → verdict

| Dungeon | Files | Best available asset | Verdict | What breaks it |
|---|---|---|---|---|
| **Wailing Caverns** | 6 | 5 section renders (1080 px wide) | **Usable as-is — multi-room** (7 rooms today) | Nothing; `WorldMap-*.webp` is excluded and must stay excluded |
| **TheStockade** | 1 | `overview.jpg` 8192×4202 | **Usable — one whole-dungeon grid**, `w`≈160–200 | Dark unlit room floors near the wall threshold; pale walls; sub-cell wall trim at `w`=56; title plaque/logo fake floors |
| **TheDeadmines** | 6 | `overhead.jpg` 8000×4796 **+** four ~2000 px room maps | **Usable — one whole-dungeon grid, and four single-room grids** | Blue sea → fake floor; sub-cell walls at `w`=56; logо/banner fake floors; `alternate.webm` unusable |
| **Shadowfang Keep** | 13 | any of the 3000×2100 room renders | **Not usable as-is** (recoverable with inverted polarity) | Polarity inverted — pale walls → floor, dark flagstone → wall; baked shadows → fake walls; multi-material rooms fragment |
| **Ragefire Chasm** | 1 | `ragefire_chasm_map.jpg` 345×376 | **Not usable** | 6.2 px/cell; saturated lava → walkable; baked numerals; JPEG mush |
| **Razorfen Kraul** | 1 | `maprfk.jpg` 650×636 | **Not usable as-is** | 11.6 px/cell; on-map numerals `1–9/CS/E` → fake floors; bright green vine overlay; 3D shading |
| **Blackfathom Deeps** | 0 | — | **Missing entirely** | No folder exists |
| **Scarlet Monastery** | 0 | — | **Missing entirely** | No folder exists |

### Missing assets — confirmed

- **Blackfathom Deeps: absent.** No folder under `images/Maps/`; the six folders are
  RagefireChasm, RazorfenKraul, ShadowfangKeep, TheDeadmines, TheStockade, WailingCaverns.
- **Scarlet Monastery: absent.** Same — no folder, and no single file elsewhere in the tree.
- **Present only as a single overview (no per-room art):** Ragefire Chasm (1 file),
  Razorfen Kraul (1 file), The Stockade (1 file).
- **Overview + per-room art:** TheDeadmines only (`overhead.jpg` plus `entrance/forge/sawmill/ship`).
- **Per-room art but no overview:** Shadowfang Keep (13 files, no whole-dungeon plan).
- **Both, and working:** Wailing Caverns only.

---

## Concrete requirements a replacement asset must meet to feed `tools/extract_wc_maps.py`

Derived from what the five working Wailing Caverns sections actually do, plus the
thresholds in `classify()`.

1. **Projection: pure top-down orthographic.** No isometric tilt, no perspective, no 3D
   wall shading. Every working file is a flat plan. Tilted art changes apparent wall
   thickness across the image and shears rooms.
2. **Negative space must be pure black** (max channel < 40) everywhere that is not
   walkable, with the art vignetted to black at the silhouette. This is the single most
   discriminating requirement: `WorldMap-WailingCaverns.webp` (paper) and
   `TheDeadmines/alternate.webm` (mid-brown) are otherwise perfectly clean plans and fail
   on exactly this point. Default: black canvas, or supply an alpha/blocked-space mask.
3. **Floors must be bright or coloured**: max channel ≥ 90, or channel spread ≥ 30.
   Tan/olive/green/teal all pass in the WC set. Avoid floors whose brightest channel drops
   below 90 (unlit dark flagstone) — those become walls.
4. **Walls must read darker than floors** — dark rock outline or void. Light stone walls
   (Shadowfang Keep ≈ 200 max channel) land unambiguously in "floor" and the level loses
   its walls.
5. **Decide fluid/tile polarity on purpose.** Saturated teal water (Wailing Caverns),
   blue sea (Deadmines), orange lava (Ragefire Chasm) all classify as **floor**. If a fluid
   must block movement, paint it dark; the classifier cannot distinguish it.
6. **Wall lines ≥ 1 cell thick at the chosen scale.** Required source thickness ≈
   `image_width / columns`. At the default 56 columns: ≥ 20 px on a 1080-wide image,
   ≥ 36 px on 2000 px, **≥ 146 px on the 8192 px Stockade** — which real walls are not, so
   pass a larger column count (`sys.argv[1]`, e.g. 160–200) for the big plans. Thinner
   walls vanish and rooms fuse.
7. **Minimum resolution: short side ≳ 1000 px** for a 56-column grid (the smallest working
   file is 1080×830 → 19.3 px/cell). As a rule, ≥ 15–20 source px per intended cell;
   below ~10 px/cell (Ragefire 6.2, Razorfen 11.6) rooms come out 1–3 cells wide.
8. **Label-free playable area.** Captions are acceptable only in the black margin — where
   the WC titles sit — and only if they fall under `THRESHOLD` (25 cells) after downscale.
   Anything drawn *on* the level (Razorfen's `1–9/CS/E`, Razorfen's vine overlay) becomes a
   fake room. Titles, banners and legend plaques are disqualifying: the WC `WorldMap`
   ribbon and the Stockade plaque are both light-on-dark *inside* the silhouette.
9. **No baked lighting or cast shadows.** Strong directional light (all Shadowfang Keep
   renders) reads as wall and shatters rooms into fragments; uniform ambient floor tone
   across the whole plan is required.
10. **One room per image, or rooms separated by ≥ 1 wall cell.** The extractor emits one
    Grid per connected floor component ≥ 25 cells, so a contiguous dungeon yields exactly
    one room (Pit of Fangs → 1; 5 sections → 7 rooms total). For an n-room grid, supply n
    images — which is what the Deadmines and Shadowfang Keep sets already do structurally.
11. **Aspect ratio is free** — height is derived (`h = round(ih * w / iw)`) and the resize
    is forced to that box. 1080×830 and 1080×1464 both work.
12. **Format hygiene.** Content must be a raster `magick` can decode; the committed
    `alternate.webm` is a WEBP with a video extension and both large "`*.jpg`" overviews are
    actually PNG. They decode, but per-dungeon dispatch on file extension will mis-handle
    them.
13. **Pipeline plumbing.** The extractor hardcodes a flat `images/{name}.webp` path and a
    five-name Wailing Caverns list, and writes `/tmp/Maps.fs`. Supporting a second dungeon
    needs a folder argument plus a per-dungeon name list — that is a code change, not an
    asset change.

---

## Contradictions and corrections to the brief

- **"TheDeadmines includes several perspective screenshots"** — not supported by the art.
  `entrance.jpg`, `forge.jpg`, `sawmill.jpg` and `ship.jpg` are all flat orthographic
  top-down room plans (Sanctum of Maps). Only the galleon inside `overhead.jpg` has 3D-ish
  shading, and it sits on a top-down plan.
- **"Shadowfang Keep, thirteen hand-annotated room maps"** — the annotations are in the
  *filenames* (e.g. `01 servants_quarters_hallway_to_5.1_stairs_to_4.1.jpg`,
  `03 Courtyard_right_door_leads_to_map_4...`); the images themselves carry a plain white
  grid overlay and no drawn numbers. They are in-game 3D renders, not hand-drawn maps.
- **File counts**: 29 files total (not 28), across 4 roots + 6 dungeon folders; the extra
  one is `TheDeadmines/alternate.webm`, which is not a video.
- **`images/Maps/` is largely untracked**: `git` tracks only the six
  `images/Maps/WailingCaverns/*.webp` files; the other 23 are untracked working-tree files
  (`.gitignore` only excludes a stray `Gemini_Generated_Image_*.jpg`). The art set is
  therefore not reproducible from a clone.

## Missing evidence / validation limits

- **Nothing was executed.** There is no shell in this environment, so `magick` and
  `classify()` were never run. Every threshold verdict above is
  *source-code reasoning + visual RGB estimation*, not measured output. The decisive test
  per file is running the extractor and comparing room counts/shapes.
- **9 of the 13 Shadowfang Keep files were not individually opened** (marked "series
  inference" in the table). They share one render series at 3000×2100; the four that were
  opened (`01`, `03`, `06`, `8.1`, plus `10`) are unanimous on polarity, so the inference
  is low-risk but not direct evidence.
- Exact RGB values were not sampled; statements like "pale stone ≈ 200" and "dark flagstone
  near the 90 boundary" are visual estimates (interpretation), not measurements.
- `Maps.fs` currently contains at least the four room names sighted (`the-screaming-gully`,
  `dreamers-rock`, `dreamers-rock-east`, `the-pit-of-fangs`) plus more; the "7 rooms" figure
  comes from `TODO.md`, not from a full read of the fixture.
- No asset in the repo covers Blackfathom Deeps or Scarlet Monastery; whether equivalent
  maps exist under other names elsewhere in the tree was not exhaustively searched.

## Next steps

1. Run `tools/extract_wc_maps.py` with a temporarily patched path for `TheStockade/overview.jpg`
   and `TheDeadmines/overhead.jpg` at `w=200` — that single experiment resolves the two
   biggest question marks (dark room floors at the Stockade, sea-as-floor at the Deadmines).
2. Add a `--invert` polarity switch and test it on `ShadowfangKeep/8.1 hidden_room.jpg`
   (the cleanest polarity counter-example) to see whether 13 usable rooms fall out.
3. Flatten `TheDeadmines/alternate.webm`'s brown background to black — it is the cleanest
   geometry in the repo and would likely beat `overhead.jpg` for a whole-dungeon grid.
4. Commit the art (or an LFS pointer set) so the asset base survives a clone.
