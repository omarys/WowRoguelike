# Handoff — WowRoguelike

Written 2026-09-17 at commit `d19b337`. Repo: `/home/omary/Dev/WowRoguelike`.
Working tree clean. **60/60 tests pass, solution builds at zero warnings.**

Read this file, then read the artifacts it points at. Do not re-derive the design
decisions — they are settled, recorded, and each one has a reason that is easy to
undo by accident.

---

## 1. What this is

A party-based roguelike built on World of Warcraft dungeon mechanics, in F# on
MonoGame. It exists to teach three things at once: functional programming, data
structures and algorithms, and game development. The tie-breaker when they
conflict is **FP is the spine** — the game is the artifact that forces FP to be
real. See `CONTEXT.md` for the vocabulary and `docs/adr/` for the two decisions
that constrain everything.

The target encounter is **Wailing Caverns' first fight** (Lady Anacondra in
Screaming Gully), chosen because one room contains threat, interruptible casts,
a heal that must be interrupted, CC aimed at the player, adds on a health
threshold, a shapeshift, and a randomly placed boss.

## 2. Read these first

| Artifact | Why |
|---|---|
| `CONTEXT.md` | The glossary. **The `_Avoid_` lists are binding** — code and comments must use the canonical term. Contains 20 terms. |
| `docs/adr/0001-fixed-tick-deterministic-simulation.md` | The Core never sees real time; speed control lives in the Shell. |
| `docs/adr/0002-hand-rolled-prng-integer-core.md` | splitmix64 with partitioned streams; integer-only Core arithmetic. |
| `src/WowRoguelike.Core/Content.fs` — top comment | The `VERIFY` block. **Every number in that file is classified as sourced or placeholder.** Trust the sourced ones; the placeholders are first knobs to turn. |
| Git commit message bodies | Each records the decision *and the measured outcome*. `git log` is the design log. |

## 3. Architecture, and the rule that enforces it

```
src/WowRoguelike.Core/     ← MUST NOT reference MonoGame. Compile order = dependency graph.
  Domain.fs                types + the Aura module, and ZERO logic
  Rng.fs                   splitmix64, partitioned streams
  Dungeon.fs               rooms-and-corridors generation, BFS flood fill
  Content.fs               abilities, party, encounter templates, World builders
  Heap.fs                  hand-rolled binary min-heap
  Path.fs                  A*, Dijkstra, greedy baseline, line of sight
  SimState.fs              queries, record rebuilds, ability/command resolution
  SimTick.fs               movement + the per-Tick pipeline
  Dump.fs                  World -> canonical text (the golden-replay fingerprint)
  Bench.fs                 timing harness, scripted player, diagnostics
src/WowRoguelike.Game/     MonoGame Shell: primitives + stdin console
src/WowRoguelike.Harness/  headless `sim` — references no MonoGame
tests/WowRoguelike.Tests/  xUnit + FsCheck
```

**F# is order-sensitive:** a file may only reference files above it, and within a
file a binding must be defined before use. This has already caused three build
breaks (a helper landing below its first caller). When adding something, put it
above its users, not where it reads most naturally.

`Domain.fs` holds types and no logic, so illegal dependency directions are
unexpressible. Every `Aura` case is matched in exactly one place (`module Aura`);
everything else asks it a question. Keep it that way — slice 4 adds aura kinds.

## 4. How to run things

```bash
dotnet test tests/WowRoguelike.Tests/WowRoguelike.Tests.fsproj   # 60 tests, ~12s
dotnet build WowRoguelike.slnx -c Release                        # use Release for anything timed

sim=$(pwd)/src/WowRoguelike.Harness/bin/Release/net10.0/sim.dll
dotnet $sim runs 120 4000     # outcome distribution over seeds — THE regression signal
dotnet $sim demo 1 2000       # one fight, with the combat log
dotnet $sim dungeon 1         # generate a floorplan and render it as ASCII
dotnet $sim bench             # tick cost, search cost, heap comparison
dotnet $sim diagnose          # per-bucket tick cost + search-failure cost
dotnet $sim fingerprint 1     # canonical state text

dotnet run --project src/WowRoguelike.Game    # the game; type `party`, then e.g. `3 8 kick`
```

**Gotcha:** the apphost at `src/WowRoguelike.Game/bin/.../WowRoguelike.Game` picks
up the system's .NET 8 at `/usr/lib64/dotnet` and dies. Use `dotnet run` or
`dotnet <dll>`, which use mise's .NET 10.

**Read `sim runs 120 4000` output as a fingerprint.** The known-good line at this
commit is `cleared=120 wiped=0 unresolved=0 median-ticks=1331 min=1116 max=2248`.
A refactor that changes it changed behaviour. This is how the Sim split was proven
to be a pure refactor rather than asserted to be one.

## 5. Hard-won knowledge — do not re-litigate these

These cost real time. Each is recorded in a commit body; the summary here is so
you do not repeat the investigation.

**Three of my own performance claims were falsified by measurement.**
- A\* was "50–100× too slow". Never substantiated. It was ~2.5× too slow, and now
  uses well under 1% of the tick budget.
- Replacing `Map`/`Set` in A\* with flat arrays was predicted at 10–100×. It bought
  **1.5×**. The bigger half was the heap: `ResizeArray<'p * 'v>` allocates a tuple
  per push and copies tuples on every swap, measuring **12.9× slower than the
  BCL's** at n=100000. Two parallel arrays fixed it.
- "A\* replanning frequency dominates the tick" was **false**: seed 3 ran 2.10
  searches/tick and cost 0.074 ms/tick, while seed 1 ran 1.17 and cost 0.88.
  Frequency does not correlate with cost at encounter scale.

**The one that mattered was a correctness bug, not a performance one.** A 53×
per-seed tick-cost anomaly came from mobs aiming A\* at **their target's own
tile** — which the target is standing on. The goal tile was allowed but the
corner-cutting rule blocked the diagonals around it, so once the party clustered
into a corner pocket there was no route at all; A\* expanded the entire reachable
component, `stepToward` discarded the empty path, and the identical doomed search
ran again the next tick. Fixed in `fc8a0f8` by aiming at a free tile *beside* the
target, keeping the current approach tile while valid (otherwise goal churn makes
it worse), and backing off with `StallTicks`.

**A benchmark that is not driven measures the wrong thing.** `Bench.report` steps
a world with no input; for a fight that measures the corpse pile it collapses
into, not the fight. Use `fightSplitCost`, which drives it with the scripted
player and separates the fixture's cost from `SimTick.step`'s. Sample windows are
printed alongside every number because cost is non-stationary within a fight and
varies ~50× between seeds at identical fight lengths.

## 6. Backlog — four open items

- **#20 Extract a named-field Entity builder.** `Content.hero`/`Content.mob` take
  8–9 positional args and repeat ~28 identical record fields, so a new Entity
  field touches two places and call sites are positional guesses. Its natural
  trigger — a second map — **has now arrived** with slice 3. Pure refactor;
  fingerprints prove it.
- **#22 Expose replay and make pause/step edge-triggered.** ADR-0001's whole
  justification is replay-at-any-speed, but nothing outside the tests can do it:
  applied commands are discarded and the Shell writes no log. Also, holding Space
  flips pause once per frame and holding `.` steps every frame; both need edge
  detection.
- **#24 Add the BSP generator** as the Q2c "implement twice" pair. Known
  limitation to fix while there: corridors can clip through an unrelated room and
  create an unintended connection — `sim dungeon` makes it visible.
- **#12 A\* replanning policy** — premise falsified at encounter scale. Only
  relevant to the synthetic 500-entity fixture, which nobody ships.

## 7. Next major work: slice 4, boss phases

`Phase` is in the glossary but has **no implementation**. Slice 4 is the last
piece of game work before the loop has an ending. It needs decisions before code —
put the frontier to the user rather than guessing. The open questions are:

1. **What drives a phase transition** — a health threshold, a timer, an add
   dying, or a count of something?
2. **How many phases** a Fanglord gets, and whether they are visible to the
   player.
3. **How a phase change is modelled** — the boss's ability list is rewritten, or a
   `Phase` field is switched on. Note the glossary defines Phase as *"entered by a
   transition rather than by a timer alone"*, so a pure timer is already excluded.
4. **What the new mechanics are**, and which are sourced vs invented. Anything
   invented must be labelled as such in the `VERIFY` block.
5. Whether `Effect` needs a new case for phase-specific behaviour — the DU is
   deliberately closed, so this is a real design decision.

`Lord Serpentis` is currently the Boss and reuses the Druid of the Fang kit.
`Mutanus the Devourer` is the real final boss of Wailing Caverns but his fight is
a summoning event with no sourced numbers; he is deliberately not stood in for,
and `Content.fs` says so.

## 8. Working agreements established this session

- **Ponytail mode is active** (level: full). Laziest solution that works; deletion
  over addition; no unrequested abstractions; mark deliberate simplifications with
  a `ponytail:` comment naming the ceiling.
- **Verify before claiming.** The user's environment has repeatedly shown that my
  confident numbers were wrong. Measure, then say the number. When a prediction is
  falsified, record *that it was falsified* in the commit — several commits here do.
- **Pin known gaps rather than hiding them.** When the encounter stalemated on 7 of
  120 seeds, the test asserted the stalemate and named the seeds, so movement in
  either direction stayed visible. Same for the corridor-clipping limitation.
- **Every non-trivial behaviour change needs a test that fails if it breaks.**
  FsCheck properties for invariants (no two living entities share a tile), xUnit
  for specific rules (the 10%/30% threat margins, interrupt lockout).
- **A refactor is proven, not asserted** — compare `sim runs 120 4000` before and
  after.
- WoW data claims must cite a source or be listed as a placeholder. Warcraft Wiki
  is the authority; Classic Era numbers win when sources conflict, because the
  modern Adventure Guide differs (e.g. Druid's Slumber is 15s in Classic, 6s in
  retail).

## 9. Suggested skills for the next agent

Call the **Skill** tool for these, in this order:

1. **`grilling`** — for slice 4's decisions. The five questions in §7 are a design
   frontier; work them in rounds, one round of the whole frontier at a time, with a
   recommended answer each. This is how slices 1–3 were specified.
2. **`domain-modeling`** — slice 4 will introduce Phase vocabulary, and possibly
   new terms for mechanics. Update `CONTEXT.md` inline as terms resolve, and offer
   an ADR only if the decision is hard to reverse, surprising without context, and
   the result of a real trade-off. `Phase` being driven by something other than a
   timer is a likely ADR candidate.
3. **`code-review`** — after slice 4 lands, review the diff since `d19b337` along
   both axes (Standards, Spec). The last run found a genuine P1 (a move order could
   path an entity into a wall) that all 51 tests missed; it is worth repeating. Run
   the two axes as parallel sub-agents per that skill's process.
4. **`ponytail`** — already active as a mode; read it if the session seems to have
   drifted toward over-building.

Optional, if the task turns to a specific bug or a regression:
**`diagnosing-bugs`**. If the next session is a large delegated workflow rather
than interactive work: **`pi-subagents`** — note that the last subagent run
reported `model_verification_failed` on both children (expected
`opencode-go/glm-5.3-flash:high`, observed `deepseek-v4.1-flash`), so child model
attestation is unresolved and worth checking before trusting a delegated result.

## 10. Things deliberately left alone

- **The Warrior dies in several seeds** (`hp=0/900` on seeds 1 and 98) and no wipe
  has ever been observed in 120 runs. If the tank dies most runs but the party
  never wipes, that is a balance signal, not a bug. Unmeasured.
- **`Bench.autoPilot`** is a scripted party commander inside the Core assembly —
  the nearest thing to the companion AI the spec explicitly excludes. It is
  unreachable from the Shell and used only by tests and the harness. Accepted as a
  fixture, flagged only because it is the one candidate.
- **`Form.Humanoid`** and the no-op `Shapeshift(Humanoid, _, _) -> w` branch are
  unreachable. Left as a seam until a second form exists; collapsing the DU now
  would churn `Effect`.
- **`Content.gully`** — the hand-authored 22×13 arena — is kept unchanged as the
  fixture slices 1–2 were built and tested against. Do not delete it; slice 3's
  generated Dungeon is additive.
