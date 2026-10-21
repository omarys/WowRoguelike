# Handoff — WowRoguelike

Repo: `/home/omary/Dev/WowRoguelike`. Written at HEAD `654fc35`.
**160/160 tests pass, 4m21s** (`dotnet test tests/WowRoguelike.Tests/WowRoguelike.Tests.fsproj`).

**The working tree is not committed.** 18 tracked files are modified (+5788/−698),
`docs/HANDOFF.md` is deleted (this file replaces it), and these are untracked:
`src/WowRoguelike.Core/MapsScarlet.fs`, `tests/WowRoguelike.Tests/snapshots/`,
`tools/`, `.pi/research/**`, `images/Maps/**`, `images/Tilesets/**`,
`src/WowRoguelike.Game/Assets/{sprites,tiles}/**`. Nothing here has been reviewed as
a change set yet — `git diff` is large and is the first thing to read.

Read this file, then the artifacts it points at. Do not re-derive settled
decisions — each one has a reason that is easy to undo by accident.

---

## 1. What this is

A party-based roguelike built on World of Warcraft dungeon mechanics, in F# on
MonoGame, to teach functional programming, data structures and game development.
The tie-breaker when they conflict is **FP is the spine**. Vocabulary is in
`CONTEXT.md` (27 terms, and its `_Avoid_` lists are binding); the two decisions
that constrain everything are in `docs/adr/`.

It has grown well past the original target encounter. Today: **nine playable
classes**, **two dungeons** (Wailing Caverns, and Scarlet Monastery as four wings
entered at researched level bands), a **level knob** for the whole party, and a
**vanilla attack table** with armour that scales by attacker level. `README.md`
§Status is the accurate, user-facing summary of what works and what does not —
read it rather than trusting this file for feature state.

## 2. Read these first

| Artifact | Why |
|---|---|
| `README.md` §Status, §Quick start | What works, what is missing, and every `mise run` task. |
| `TODO.md` | The live backlog: the story track, backlog, slice 4, the roster, known limitations. |
| `CONTEXT.md` | The glossary. **The `_Avoid_` lists are binding.** |
| `docs/adr/0001-…`, `0002-…` | The Core never sees real time; splitmix64, integer-only Core arithmetic. |
| `Content.fs` — the `VERIFY` block at the top | **Every number is classified sourced or placeholder.** Trust the sourced ones. |
| `.pi/research/**` | The research the content is built from. Layout in §7. |
| Git commit bodies | Each records a decision *and its measured outcome*. `git log` is the design log — but note the last ~5.8k lines are uncommitted, so the log stops being one at `654fc35`. |

## 3. Architecture, and the rule that enforces it

```
src/WowRoguelike.Core/     ← MUST NOT reference MonoGame. Compile order = dependency graph.
  Domain.fs        981     types, the Aura module, ZERO logic
  Rng.fs            67     splitmix64, partitioned streams
  Heap.fs           78     hand-rolled binary min-heap
  Path.fs          277     A*, Dijkstra, greedy baseline, line of sight
  Dungeon.fs       296     rooms-and-corridors generation, BFS flood fill
  Maps.fs          402     Wailing Caverns rooms extracted from images/Maps
  MapsScarlet.fs   761     Scarlet Monastery's four wings, same treatment
  Content.fs      3328     abilities, rank tables, nine class kits, mobs, World builders
  SimCombat.fs     797     the attack table, armour, resources
  SimState.fs      257     queries, record rebuilds, ability/command resolution
  SimTick.fs       544     movement + the per-Tick pipeline
  PartyAi.fs       773     the party's own AI: tank, healer, dps, rogue positioning
  Bench.fs         519     timing harness, scripted player, diagnostics
  Dump.fs           73     World -> canonical text (the golden-replay fingerprint)
src/WowRoguelike.Game/     MonoGame Shell: primitives, sprites/tiles, menu, stdin console
src/WowRoguelike.Harness/  headless `sim` — references no MonoGame
tests/WowRoguelike.Tests/  xUnit + FsCheck; snapshots/ holds the bench baseline
```

**F# is order-sensitive:** a file may only reference files above it, and within a
file a binding must be defined before use. This has broken the build more than
once (a helper landing below its first caller). Put new code above its users.

`Domain.fs` holds types and no logic, so illegal dependency directions are
unexpressible. Every `Aura` case is matched in exactly one place (`module Aura`).

## 4. How to run things

`README.md` §Quick start has the full task table. The ones that matter here:

```bash
mise run build     # solution in Release; compiles the ink narrative first
mise run publish   # dist/ — the game plus Assets/ and MonoGame's native libs
mise run test      # 160 tests, ~4.5min
mise run runs      # outcome distribution over 120 seeds — THE regression signal
mise run sim -- smrun cathedral med 16000    # run a wing headless, report survivors
mise run sim -- sm graveyard min             # place a wing at a band
mise run bench     # tick cost, search cost, heap vs BCL
```

**`mise run run` plays the Release build** — the same build `test` and `bench`
measure, so what you see is what was measured. `mise run publish` is
framework-dependent: the machine needs the .NET 10 runtime, not the SDK. Add
`--self-contained -r linux-x64` to the publish task for a folder that needs
nothing at all.

**The narrative is a build product.** `src/WowRoguelike.Story/Scripts/wailing_caverns.ink.json`
is embedded in the Story assembly, and `mise run ink` produces it from the `.ink`
source (the `build` task runs it, and a pre-commit hook runs it when a `.ink` is
staged). The compiler emits a UTF-8 BOM and no final newline; the ink task strips
the BOM and adds the newline so its output is byte-identical to what is committed
— check `git diff` after compiling, it should be empty. An edited `.ink` that is
never recompiled commits a narrative the game does not play.

**Two traps, both cost real time:**

- **`dotnet build` at the repo root does not refresh the test project's copy of
  the Core DLL.** After a Core change, `dotnet test` can run against a stale
  binary and report results for code you no longer have. Build the test project
  (`dotnet build tests/WowRoguelike.Tests/WowRoguelike.Tests.fsproj`) or run the
  suite, which builds its own dependencies. This produced a whole round of
  contradictory measurements once.
- **`DOTNET_ROOT` must point at the mise-managed SDK**
  (`/home/omary/.local/share/mise/dotnet-root`), which `mise.toml` arranges.
  Unset, any invocation resolves against `/usr/lib64/dotnet` (.NET 8 only) and
  fails with "You must install or update .NET".

**`mise run runs` is a fingerprint.** The last recorded known-good line
(`cleared=120 wiped=0 unresolved=0 median-ticks=1331 min=1116 max=2248`) predates
the level-24 rebalance and the nine-class roster, so it is certainly stale.
**Re-measure it and record the new line** — until that happens the pre-push hook's
behavioural check is comparing against a fiction. Note it sweeps the hand-authored
`Content.gully` fixture (`Bench.fs:22`), not a generated dungeon.

## 5. Hooks — the only automated check

There is no CI for the code. `.pre-commit-config.yaml` is it, plus the scheduled
OpenWiki workflow (§6).

```bash
mise run hooks      # once after cloning
mise run hooks-run  # every hook against the whole tree
```

Split by cost on purpose: **pre-commit** is formatting + `dotnet build` (~6s);
**pre-push** adds `dotnet test` and `sim runs 120 4000`. Putting the suite in
pre-commit would make committing slow enough that someone reaches for
`--no-verify`, which is worse than not having the hook.

## 6. The OpenWiki workflow

`.github/workflows/openwiki-update.yml` was repaired: it now checks for an
`OPENAI_API_KEY` secret first and **skips with a warning** instead of failing, so a
red run means "OpenWiki is broken" rather than "no secret is set". Dead LangSmith
config that would have failed once a key was added was removed. To enable the
scheduled update: `gh secret set OPENAI_API_KEY`.

`openwiki/` is a generated evidence index. Per `AGENTS.md`, do not preload or
enumerate it at task start — use it just-in-time, and never hand-edit its pages.

## 7. Where the data lives

Everything numeric in `Content.fs` came from research under `.pi/research/`, and
the `VERIFY` block names which is which.

| Path | Contents |
|---|---|
| `level24/base_stats.md`, `base_stats_new_classes.md` | Per-race/class stat tables, 24 rows each (levels 1–24). |
| `level24/{warrior,rogue,mage,druid,priest,paladin,hunter,warlock,shaman}.md` | Level-24 best-in-slot gear, derived stats, ability parameters. |
| `level24/missing_abilities.md` | Ability parameters that had no home in a class file. |
| `level24/mobs.md`, `mobs_armour_and_spells.md` | Creature armour and spell data. |
| `ranks/{warrior,rogue,mage,priest,druid}.md` | Full rank tables, levels 1–60. |
| `wc/mobs.md`, `wc/quests-and-scripts.md` | The 1.12 creature rows and the quest/script chain. |
| `vanilla-attack-table.md` | Miss/dodge/parry/glance/block/crit by level delta, spell miss, armour. |
| `level60-kits-at-24.md` | Level-60 kits scaled down to 24 by pool ratio. |
| `levelscale/README.md` + `dungeon-bands.md` | Per-dungeon minimum/median/maximum bands and the per-level curves. |
| `levelscale/{warrior-rogue,mage,priest-druid}-ladder.md` | Gear ladders above 24 (28–32, 33–37, 38–40) — researched, not yet wired. |
| `levelscale/map-assets.md` | Which dungeon maps are usable as art, and the verdicts. |

Warcraft Wiki is the authority. **Classic Era numbers win when sources conflict.**

## 8. Hard-won knowledge — do not re-litigate

Each is recorded in a commit body or a code comment; this is so you do not repeat
the investigation.

**Performance claims were repeatedly falsified by measurement.** A\* was "50–100×
too slow" (actually ~2.5×, now well under 1% of the tick budget). Replacing
`Map`/`Set` with flat arrays was predicted at 10–100× and bought **1.5×**; the
bigger half was the heap — `ResizeArray<'p * 'v>` measured **12.9× slower than the
BCL's** at n=100000. "A\* replanning frequency dominates the tick" was **false**.
Measure, then say the number.

**The bug that mattered was correctness, not speed.** A 53× per-seed tick-cost
anomaly came from mobs aiming A\* at their target's own tile — the goal was
allowed but corner-cutting blocked the diagonals around it, so once the party
clustered into a pocket there was no route, A\* expanded the whole reachable
component, and the identical doomed search re-ran every tick. Fixed in `fc8a0f8`.

**A benchmark that is not driven measures the wrong thing.** `Bench.report` steps a
world with no input; for a fight it measures the corpse pile it collapses into. Use
`fightSplitCost`. Sample windows are printed with every number because cost is
non-stationary within a fight and varies ~50× between seeds at equal fight lengths.

**An unbounded flood fill ate 64 GB.** A test helper updated its `seen` set *after*
building each frontier, so a tile reachable from two neighbours was yielded twice
and the duplicates multiplied every round — exponential growth from a 12×10 grid.
The user noticed the process. Use a visited **array** (or update the set as you
yield), and never write a loop in a test whose termination is not obvious.

**Entity ids come from the roster slot, never from the class.** Members are seated
1..5 in roster order, mobs number from 6. A class builder that hard-codes its own
id collides with a mob, and every `tryEntity` on that id then finds whichever came
first — which surfaced as a *mob's* heal logged against a party member's name.

**`Entity.Stats` is pre-buff.** `SimState` applies auras and party buffs on read;
do not bake a buff into a kit's base stats or it applies twice.

**Headless runs need the AI driven by hand.** `SimTick.run`/`step` do not call
`PartyAi.decide`; the harness and scripts do (`SimTick.step (PartyAi.decide w)`).
A "headless run" that forgets this measures a party standing still.

**The Wailing Caverns ending keys on Mutanus dying, not on an item.** The four
Fanglord deaths are the only gate (they flip the Disciple's dialogue), and killing
Mutanus wakes Naralex. Three premises from the original brief were **false** and
are tagged `D` with evidence in `.pi/research/wc/quests-and-scripts.md`: the "Disc
of Naralex" does not exist, "Naralex and the Druids of the Fang" is not a quest,
and Naralex is never interactable or attackable — he is friendly, prone, and flies
off alive. Do not reintroduce a hand-off to gate the ending.

**WC-scale simulation costs ~11 ms/tick** (~130 entities). Keep dungeon-scale
assertions short; a three-seed 9000-tick test is nine minutes of suite time. The
stall regression test asserts *progress in an early window* rather than a clear,
both because a clear is not currently true and because the full run is too slow.

## 9. Open work, in priority order

1. **Wailing Caverns does not hold up as a clearable dungeon.** At the level-24
   band the party wipes on two of three seeds, killing 28–54 of 111 hostiles in
   9000 ticks. The roster is now built from the real 1.12 creature data
   (`.pi/research/wc/mobs.md`) and the quest chain is in, so the remaining work is
   balance, not content. The party stops improving at level 24 because the gear
   sets stop there — see (2), which is the real cause.
2. **Gear ladders above level 24.** Researched and waiting in
   `.pi/research/levelscale/*-ladder.md` (28–32, 33–37, 38–40). Weapon damage
   first. This is what makes the higher Scarlet Monastery bands winnable, and it is
   the single largest balance lever in the project.
3. **The class roster has no UI.** `Content.defaultRoster` is still a constant;
   nine kits exist and five get seated. `Content.partyOf`/`withRoster` are ready
   for a picker.
4. **Slice 4: boss phases.** `Phase` is in the glossary with no implementation.
   Five open design questions are listed in `TODO.md` §Slice 4 — put them to the
   user rather than guessing. `Mutanus the Devourer` is deliberately not stood in
   for: his fight is a summoning event with no sourced numbers.
5. **Mobs do not patrol.** The tank's pull-veto (`packGap`) is an approximation of
   avoiding patrol paths, not a patrol model. If patrols arrive, that heuristic
   should be revisited rather than layered on.
6. Smaller, from `TODO.md`: extract a named-field `Entity` builder (its trigger — a
   second map — has arrived); expose replay and make pause/step edge-triggered;
   add the BSP generator.

## 10. Working agreements

- **Ponytail mode is active** (level: full). Laziest solution that works; deletion
  over addition; no unrequested abstractions; mark deliberate simplifications with
  a `shortcut:` comment naming the ceiling.
- **Verify before claiming.** Confident numbers have been wrong repeatedly here.
  Measure, then state the number; when a prediction is falsified, record *that it
  was falsified*.
- **Pin known gaps rather than hiding them.** When something is a known limitation,
  assert it and name it, so movement in either direction stays visible.
- **Every non-trivial behaviour change needs a test that fails if it breaks.**
  FsCheck for invariants, xUnit for specific rules. Unit-test AI decisions against
  a small hand-drawn grid fixture (see the tank tests) rather than only through a
  full dungeon run.
- **A refactor is proven, not asserted** — compare `mise run runs` before and after.
- WoW data claims cite a source or are labelled placeholders in the `VERIFY` block.
- **Watch the test process.** The user notices when it misbehaves; a runaway loop
  is a real cost here, not a theoretical one.

## 11. Suggested skills for the next agent

Call the **Skill** tool for these, in this order:

1. **`grilling`** — for slice 4's five design questions, one round of the whole
   frontier at a time with a recommended answer each. This is how slices 1–3 were
   specified.
2. **`domain-modeling`** — slice 4 introduces `Phase` vocabulary; update
   `CONTEXT.md` inline as terms resolve, and offer an ADR only if the decision is
   hard to reverse, surprising without context, and a real trade-off.
3. **`code-review`** — before anything is committed. **~5.8k uncommitted lines**
   across 18 tracked files plus new ones have never been reviewed, and the last
   review found a genuine P1 (a move order could path an entity into a wall) that
   all 51 then-existing tests missed. Run Standards and Spec as parallel sub-agents.
4. **`ponytail`** — already active as a mode; read it if the session drifts toward
   over-building.

If the task is a hard bug or a perf regression: **`diagnosing-bugs`**. If it is a
large delegated workflow rather than interactive work: **`pi-subagents`** — and note
that child model attestation has failed here before (`model_verification_failed`,
expected `opencode-go/glm-5.3-flash:high`, observed `deepseek-v4.1-flash`), so check
before trusting a delegated result. Subagent launches are refused when the source
checkout is dirty, which it currently is.

## 12. Deliberately left alone

- **`Content.gully`** — the hand-authored 22×13 arena — is kept unchanged as the
  fixture slices 1–2 were built and tested against, and as the bench fixture. Do
  not delete it.
- **`Bench.autoPilot`** is a scripted party commander inside Core — the nearest
  thing to the companion AI the spec excludes. Unreachable from the Shell, used
  only by tests and the harness. Accepted as a fixture.
- **`Form.Humanoid`** and the no-op `Shapeshift(Humanoid, _, _) -> w` branch are
  unreachable. Left as a seam until a second form exists.
- **Paladin seals** are deferred with a `shortcut:` comment; the attack table has
  no seal hooks.
- **Rendering is rectangles plus per-dungeon tile art.** Sprites and tiles are
  keyed and loaded from `Assets/`, but there is no animation and no content
  pipeline.
- **Maps for five further dungeons** (Ragefire Chasm, Razorfen Kraul, Shadowfang
  Keep, The Deadmines, The Stockade) are extracted into `images/Maps/`, and
  Blackfathom Deeps exists only as sliced tiles/sprites in `Assets/`. Per-map
  verdicts — which are usable as art and which are not — are in
  `.pi/research/levelscale/map-assets.md`. All of this is art and research only;
  no content is built on it.
