# WowRoguelike

A party-based roguelike built on World of Warcraft dungeon mechanics, in F# on
MonoGame. Procedurally generated dungeons, five characters you command directly,
threat and interrupts, permadeath.

It is a learning project with three goals that conflict constantly: functional
programming, data structures and algorithms, and game development. The
tie-breaker is that **FP is the spine** — the game is the artifact that forces
functional programming to be real rather than decorative. When purity and frame
rate disagree, purity wins until something is measured.

```
$ mise run dungeon -- 7
46x26, 6 rooms, entrance 32,17, boss room 9,2 11x6
connected: true

##############################################
##############################################
#########BBBBBBBBBBB######.......#############
#########BBBBBBBBBBB######.......#......######
#######..BBBBBBBBBBB######.......#......######
#######..BBBBBBBBBBB######.......#......######
#######..BBBBBBBBBBB######..............######
#######..BBBBBBBBBBB######..............######
#######..#####..##########.......#......######
#######..#####...........#.......#......######
#######..#####...........####..#####..########
#####......###...........####..#####..########
#####......###...........####..#####..########
#####......###.................#####..########
#####......###........................########
#####.................................########
#####.................................########
#####......####.................@....#########
#####......####......................#########
#####......#################.........#########
#####......#################.........#########
############################.........#########
```

`B` is the boss room, `@` the entrance. The boss room is chosen as the room
farthest from the entrance **by path length**, not by straight line.

## Quick start

Requires [mise](https://mise.jdx.dev/) (it installs the right .NET and
`pre-commit`), or a .NET 10 SDK and `pre-commit` of your own.

```bash
mise install            # .NET 10 + pre-commit, per mise.toml
mise run hooks          # install the git hooks — do this once
mise run build          # solution, Release
mise run test           # 60 tests, ~12s
mise run run            # play it
```

`mise tasks ls` lists everything. The useful ones:

| Task | What it does |
|---|---|
| `mise run run` | Play the game. MonoGame window shows the arena; **you type commands on stdin** — `party` to list entities, then e.g. `3 8 kick` |
| `mise run test` | The test suite |
| `mise run runs` | Outcome distribution over 120 seeds — the behavioural regression signal |
| `mise run dungeon` | Generate a floorplan and render it as ASCII |
| `mise run sim -- <args>` | Any other harness subcommand (`demo`, `fingerprint`, `profile`) |
| `mise run bench` | Tick cost, search cost, and the hand-rolled heap against the BCL's |
| `mise run diagnose` | Per-bucket tick cost, and the cost of a search that cannot succeed |
| `mise run hooks-run` | Every hook against the whole tree |

Everything timed runs in **Release**. Debug timings here are meaningless.

## Status

Playable end to end: enter a generated dungeon, pull trash, fight a mini-boss,
kill the boss, or wipe.

**Working** — 20Hz fixed-tick deterministic simulation with pause and 0–20Hz speed
control · threat with WoW's 10%/30% margins and a 5× tank stance · interruptible
casts with a school lockout · CC, shapeshifting, adds that arrive on a health
threshold · finite resource pools · symmetric line of sight, melee exempt · A\*
pathfinding on a grid, 8-way, corner-cutting refused · rooms-and-corridors
generation, connected by construction and verified by a BFS flood fill · a
five-member party (Tank, Healer, 3 DPS) commanded directly, with no companion AI ·
a MonoGame shell drawing primitives only, with no content pipeline.

**Not there yet** — boss phases (`Phase` is in the glossary with no
implementation, and it is the next slice) · one encounter template set, WoW-derived;
no other dungeons · no loot or between-run progression · no resurrection, so a wipe
ends the run · rendering is coloured rectangles with no sprites and no text, which
is why input happens in the terminal.

## Layout

```
src/WowRoguelike.Core/      the simulation. MUST NOT reference MonoGame.
  Domain.fs                 types, and the one module that matches every Aura case
  Rng.fs                    splitmix64, split into independent streams
  Dungeon.fs                rooms-and-corridors generation, BFS flood fill
  Content.fs                abilities, the party, encounter templates, World builders
  Heap.fs                   hand-rolled binary min-heap
  Path.fs                   A*, Dijkstra, greedy baseline, line of sight
  SimState.fs               queries, rebuilds, ability and command resolution
  SimTick.fs                movement, and the per-Tick pipeline
  Dump.fs                   World -> canonical text, for golden-replay comparison
  Bench.fs                  timing harness, scripted player, diagnostics
src/WowRoguelike.Game/      MonoGame shell — window, input, drawing, interpolation
src/WowRoguelike.Harness/   headless `sim`; references no MonoGame
tests/WowRoguelike.Tests/   xUnit + FsCheck
```

F# compile order **is** the dependency graph: a file may only reference files above
it. `Domain.fs` holds types and no logic, so an illegal dependency direction cannot
be expressed.

## How correctness is checked

There is no CI. The hooks are the only automated check, so keep them working.

- **On commit** — formatting hygiene and `dotnet build` (~6s), catching compile
  errors and new warnings.
- **On push** — adds `dotnet test` and `sim runs 120 4000` (~1min).

The seed sweep is the one that matters most. The tests prove the *invariants* hold;
only the sweep proves the *game* still behaves. It currently prints:

```
seeds=120 cleared=120 wiped=0 unresolved=0 median-ticks=1331 min=1116 max=2248
```

Treat that line as a fingerprint. A refactor that moves it changed behaviour — which
is how the `Sim` → `SimState`/`SimTick` split was *proven* to be a pure refactor
rather than asserted to be one. Nothing is trusted until it is measured; several
performance intuitions in this repo's history were wrong, and the commit bodies say
which ones and by how much.

## Design constraints

Two decisions constrain everything, and both are easy to undo by accident:

- [ADR-0001](docs/adr/0001-fixed-tick-deterministic-simulation.md) — the simulation
  advances in fixed Ticks and never sees real time; speed control lives in the
  shell. This is what makes replay bit-exact.
- [ADR-0002](docs/adr/0002-hand-rolled-prng-integer-core.md) — a hand-rolled PRNG
  with partitioned streams, and no floating point in the simulation.

## Data and licensing

This is an unaffiliated fan project. **No Blizzard assets are used** — no extracted
models, textures, audio, or map data, and no game files are read. The geometry is
generated from a seed, and everything drawn is a coloured primitive.

Mechanics are modelled on Classic Era Warcraft, and every number claiming to be WoW
data is **classified as sourced or placeholder** in the `VERIFY` block at the top of
`src/WowRoguelike.Core/Content.fs`. The sourced ones — Druid's Slumber at 15s, Healing
Touch at 195–243, Serpent Form at 10s and +25, the threat margins — come from
[Warcraft Wiki](https://warcraft.wiki.gg/wiki/Wailing_Caverns). Every cast time,
cooldown and health pool is a placeholder and says so. Do not silently promote a
placeholder to a fact.

## Documentation

| | |
|---|---|
| [`CONTEXT.md`](CONTEXT.md) | The glossary. **The `_Avoid_` lists are binding** — code and comments must use the canonical term. |
| [`docs/adr/`](docs/adr/) | The two decisions above. |
| [`docs/HANDOFF.md`](docs/HANDOFF.md) | Written for a fresh agent: architecture traps, hard-won knowledge, the open backlog, and what to do next. |
