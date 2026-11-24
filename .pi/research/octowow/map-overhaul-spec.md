# Map overhaul spec — operator directives m02410

Verbatim: "Please perform a full review of maps using assets in the images/ folder and increase map size significantly. Even a doorway should be two tiles wide, at a minimum, and the tank should pull engaged enemies back if party members are unable to safely flank. … note that the Scarlet Monestary map I saw during the `mise run visual-sm-test` looked like Scarlet Monestary enemies in a Wailing Caverns map. Wailing Caverns takes place in a cave, and Scarlet Monestary takes place in a large stone church. Please see images/Tilesets/Gemini_Generated_Image_Scarlet_Monastery.jpg for tileset example and images/Maps/ScarletMonastery for map examples."

**This supersedes the earlier ruling** recorded in docs/agents/handoff.md § "A3 is built" ("the operator ruled the authentic map stands"). The new ruling: maps grow significantly, doorways ≥ 2 tiles minimum, SM renders as a stone church.

## Verified root causes (do not re-diagnose)

1. **Cave look**: `tools/slice_tileset.py` slices sheets `wailing` / `forms` / `future-forms` only — no Scarlet sheet. The Game draws every map with global `tiles/wall` (rock with roots) + `tiles/floor0/1` (mossy dirt) — `src/WowRoguelike.Game/Program.fs:1370-1382`. SM wings therefore wear Wailing Caverns terrain. A per-entry art folder already exists in `browseSources` (SM entries carry `"sprites/scarlet-monastery"`, Program.fs:986-992) — the tile theme must follow the same per-entry pattern.
2. **Small maps**: `tools/extract_sm_maps.py` extracts each wing at long side 200 (`WINGS` table) with wall modes graveyard=stone, others=dark. Its connectivity repair carves **one-wide** L corridors between floor islands — a guaranteed 1-tile doorway source. Current grids are ~96–191 cols.
3. **Corridor jams** (handoff, A3 section): graveyard 1-7/12 cleared with runs alive at the 24,000-tick limit — diagnosed as a PartyAi target-acquisition problem in one-wide corridors, not geometry. Wider maps should relieve it; must be measured, not assumed.

## Work items

### 1 — Extractor: size + doorway minimum
- Bump `WINGS` long side 200 → **400** (double linear, ~4× area; a large stone church). Watch run cost — the extractor's own `--dry` mode measures tight-share first.
- Make the carved L-corridor repair **two tiles wide**.
- Add a doorway pass: every transition between a room and a corridor (any 1-tile-wide floor pinch, i.e. a floor tile whose orthogonal floor neighbours number ≤ 2 and sit opposite each other) gets dilated to ≥ 2 wide. The invariant to enforce and test: **no floor tile anywhere in any wing is a 1-wide pinch**. If the reference image genuinely shows a 1-wide door, widen it anyway — the operator's minimum is explicit.
- Regenerate `src/WowRoguelike.Core/MapsScarlet.fs` with the tool (never hand-edit grids). Keep stone/dark wall modes as-is.
- MapsScarlet.fs must stay a single-grid-per-wing (no `rawWing`/`widen` remnants — they were reverted).

### 2 — Scarlet church art
- **No vision is available anywhere in this stack** (verified: the session model and every delegate role report `[Current model does not support images]`; the oracle probe `del_mwnwy3ct_drem` honestly returned VISION-UNAVAILABLE). Do not wait for a visual description; do not fabricate one. Derive the boxes programmatically:
  - All generated sheets share a 2400x1792 canvas with ~300px cells cut inside painted rims (see `tools/slice_tileset.py`: `cell(col, ys) = col*300+12 … col*300+288`; wailing boxes: `floor0` = col 2 row 0, `floor1` = col 2 row 1, `wall` = col 4 row 0). **Working hypothesis: the Scarlet sheet uses the same cell layout template, different theme** — so `scarlet-wall` ≈ col 4 row 0, `scarlet-floor0/1` ≈ col 2 rows 0/1. VERIFY, don't assume: detect the actual cell grid (projection profile / rim detection) and classify candidate cells by pixel statistics — the same technique `extract_sm_maps.py` already uses for its stone/dark wall modes. A wall cell must read as wall-like (high edge density, mortar lines, cool gray); a floor cell as uniform pale stone. If the candidates fail the sanity check, scan every cell and pick the best wall/floor matches by those statistics; record the chosen boxes in a comment.
  - Add a `scarlet` sheet to `tools/slice_tileset.py` emitting `tiles/scarlet-wall` (+ wall variant if a second wall-like cell exists), `tiles/scarlet-floor0/1`, and — only if the statistics show a clearly distinct ornate cell — `tiles/scarlet-floor-ornate`. TILE=40, cut inside the cell away from rims, per the script's conventions. Mark with a `shortcut:` comment: boxes chosen by pixel statistics, no eyes verified them.
- Game: per-entry tile theme. `browseSources`/playable entries already carry an art-folder string — extend the draw path (`Program.fs:1370-1382`) so an entry with a scarlet theme draws `tiles/scarlet-wall` / `tiles/scarlet-floor0/1` (falling back to the global tiles when absent, which is the existing convention at :332). SM wings get the theme; Wailing Caverns entries keep cave art.
- Decorations slot (`Program.fs:1382`): leave for the SM wings unless the sheet statistics show an obviously object-like cell (small bright object on flat background — the sprite-cut path in the script already handles flood-fill trim). Do not generate new art.

### 3 — Tank pull-back AI
Operator verbatim: "the tank should pull engaged enemies back if party members are unable to safely flank."
- Composes with the just-landed melee-behind logic (paladin package, worker `del_mwnw1agg_f9ba`): when a party melee is engaged and the behind-tile predicate finds no safe flank tile (the corridor fallback case), the tank's move order changes to a retreat: step back 2-3 tiles along the approach direction (or to the nearest open floor away from the pack) while holding threat; mobs re-path to it; melee re-path behind once the arc opens.
- Gate against oscillation: commit to the pull for a fixed tick window and re-evaluate only after it, not every tick.
- Keep it in `PartyAi.fs` tank orders (the same lane as `meleeMark`/`home`/`shapeFor`, PartyAi.fs:668-681 precedent). Widen maps should make this rare; it exists for the pinches the dilation can't fix (corridor combat mid-transit).

### 4 — Tests
- Core suite: (a) every wing passes the no-1-wide-pinch invariant; (b) tank pull-back: in a constructed 1-wide corridor engagement with a flanking melee, the tank's order becomes a retreat and a behind-tile opens for the melee after the pull; (c) existing grid tests (reachability, room extraction) stay green — regenerate whatever map-derived snapshot the suite owns via its own update path.
- Game is not under the test suite; verify by running `dotnet build src/WowRoguelike.Game -c Release` and a short `--sm-tour` run to confirm the tour still cycles all four wings end to end with the larger maps (bigger grids mean longer wings — check the tour's advance-on-run-over pacing still moves; adjust the hold only if a wing stalls).
- **OPERATOR HANDS OFF VISUALS (m02450 + m02453)**: do not block on a visual sign-off. When the build lands, make sure the existing `mise run visual-sm-test` (mise.toml:101, runs `--sm-tour`) exercises the new scarlet tile theme on every wing, fix anything that breaks the tour, and simply tell the operator it is ready — they run it manually and judge the art themselves.

### 4b — Visual tour tasks, standardized naming (operator requests m02457 + m02461)
- **Standardize the two mise tasks as `visual:sm` and `visual:wc`** (mise task names allow the colon form; delete/rename the old `visual-sm-test` so only the new names remain, and keep the description strings saying which wings/entries each tours).
- `visual:sm` = the current `visual-sm-test` behavior (mise.toml:101, runs `--sm-tour`).
- `visual:wc` = new `--wc-tour` flag on the Game. Generalize the existing tour machinery (`tourWings` filters menu entries by label prefix `"Scarlet Monastery — "`, Program.fs:965-970) — the WC variant filters the banded `Wailing Caverns` entries (Program.fs:266, `Content.wailingCavernsAt` bands 17/21/22) and reuses the same auto-advance/hold code path. Purpose: the operator eyeballs that the cave art stays cave art after the theme split lands (SM=stone, WC=cave is exactly the regression this tour catches).
- Update any docs/handoff references to the old `visual-sm-test` name to the new `visual:sm` (grep for it).

### 5 — Measurement
- After green: `comps` all wings × both rosters, ≥12 seeds (25 if time), built harness binary, no leading "sim" token. Acceptance: no wing regresses its cleared rate vs the b68 clean baseline (graveyard 1-7/12, library 7-11/12, armory 10-12/12 — the widened maps should improve or hold these), graveyard's alive-at-limit share drops, run wall time reported.
- Any command >3 min prints a heartbeat every 60 s (5-min silent watchdog kills the run). Long runs via `setsid nohup … > /tmp/map-fix-*.log 2>&1 &`.
- No commit. Report to /tmp with file:line, tables, and any shortcut comments verbatim.

## Sequencing (hard)
Start only after worker `del_mwnw1agg_f9ba` (paladin package) has finished and its work is verified — one writer per repo, and item 3 composes with that worker's behind-tile predicate.
