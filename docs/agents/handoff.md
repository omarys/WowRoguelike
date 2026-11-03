# Handoff — WowRoguelike

Written at commit `aba0722`, with this session's work on top as one WIP commit (see
below).
Repo: `/home/omary/Dev/WowRoguelike` (F#, .NET 10, MonoGame shell).

**218/218 tests green**, Release build clean — re-verified on this tree with
`dotnet test tests/WowRoguelike.Tests -c Release`.

Everything settled — the brief, the architecture rules, the run table, and every
decision with its reason — is in `docs/agents/domain.md`. Read that first, then §9
(open work). Do not re-derive what it records; the reasoning in there is easy to
undo by accident. This file only covers where the work stands and what is next.

## The session's work is one unsplit commit

14 modified files and three new ones, green but committed as a checkpoint rather
than as reviewable history:

```
M  docs/agents/domain.md            M  src/WowRoguelike.Core/Path.fs
M  mise.toml                        M  src/WowRoguelike.Core/SimCombat.fs
M  src/WowRoguelike.Core/Bench.fs   M  src/WowRoguelike.Core/SimState.fs
M  src/WowRoguelike.Core/Content.fs M  src/WowRoguelike.Core/SimTick.fs
M  src/WowRoguelike.Core/Domain.fs  M  src/WowRoguelike.Core/WowRoguelike.Core.fsproj
M  src/WowRoguelike.Core/PartyAi.fs M  src/WowRoguelike.Harness/Program.fs
M  tests/WowRoguelike.Tests/Tests.fs
M  tests/WowRoguelike.Tests/snapshots/bench-30.txt
A  src/WowRoguelike.Core/Attribution.fs
A  docs/agents/handoff.md
A  .pi/research/levelscale/raid-gear-60.md
```

`docs/agents/handoff.md` (this file) replaces the old
`wowroguelike-handoff-graveyard-stall.md`. `.unipi/` is tooling noise, ignore it.
Splitting the commit into reviewable pieces is a real piece of owed work — the diff
spans movement, party AI, content, harness and tests, and nothing has been
reviewed by anyone but the agent that wrote it.

## What this session changed

Details and measured outcomes are in `docs/agents/domain.md`; these are pointers
only.

| area | state |
|---|---|
| Rogue kit — 5 poisons in 2 slots, situation policy, Backstab on any target not targeting the Rogue, Ambush/Cheap Shot openers, off-hand swing cadence | implemented, tested |
| Tank line-of-sight discipline — pull-back rule so the party is never healed through a wall | implemented, tested, `domain.md:923-990` |
| Out-of-combat recovery — 5 s wait, then 100% back at 10%/tick, `World.RestTicks` | implemented, tested, `domain.md:1436-1466` |
| Reincarnation — no mana cost, 30 min cooldown less Improved Reincarnation, 40% pools | verified already correct, needed no change |
| Per-ability attribution — `sim attrib`, nine-band table | shipped, `Attribution.fs` (new) |
| Composition ladder — 36 cells, one healer per class | measured, `domain.md` §9 item 0 |
| Corridor parks — a Tank whose next step is held by a settled ally now counts as stuck | fixed, 5 of 6 park seeds clear; one shuffle left, below |

## Open bug 1 — the parks (5 of 6 cleared, one shuffle left)

A "park" is a wing run that ends `Running` at the tick cap with the party alive
and hostiles alive: `unresolved` in the ladder table.

**Re-diagnosed and re-measured on the current tree.** The previous diagnosis here —
"truncated routes are trusted, that is the livelock, fix it in two lines in
`stepToward`" — described code that no longer exists, and its example seed no
longer parks. Do not implement it. Everything below was measured during the
validation pass, against a pinned binary (rebuild `--no-incremental`, md5
unchanged before and after).

### The parks are real

- library seeds 1, 2, 4 and 5 at `Maximum`, 40000-tick cap: `Running`, 15 hostiles
  alive, parked 33 330–34 292 ticks.
- graveyard seeds 5 and 8 at `Maximum`, same cap: `Running`, 12 and 9 hostiles.
- `sim comps 2 --level 60` (8m23s): graveyard `unresolved=1` of 2 seeds for **both**
  rosters; library `unresolved=1` for Warrior/Priest/Hunter/Rogue/Warlock.
- Parked bodies have been Priest, Druid, Rogue and Warrior across runs. It is not a
  healer-rank problem.

### The old example is dead

graveyard seed 1 at a 24000-tick cap now **clears**: Median `EncounterCleared`
t=11410, Maximum `EncounterCleared` t=10888, 5 alive, 0 hostiles. The
`Running` / 2 alive / 27 hostiles / "Shaman truncated at 1200" table that used to
sit here came from that seed and is obsolete.

### Cause 2 (truncation) is already fixed

`Path.search` runs the 1200-node quick search and retries at **full grid budget**
when that search truncated *and* came back empty (added by this session's diff; not
present at `aba0722`). An empty route from `Path.astar` therefore now means "no
route", not "the budget ran out".

Measured on the park that survives (library seed 1, t=40000, `Priest 19,125` with
`goal 14,125`, `path=0`) — the claim-aware search is honest, not truncated:

| probe | result |
|---|---|
| claim-aware (`SimState.claimedByOther`) | `len=0 expanded=8465 truncated=false` — the escalated retry ran and exhausted the component |
| stationary allies passable (the relaxation `stepToward` already has) | `len=5 expanded=5 truncated=false` |
| no claims at all | `len=5 expanded=5` |
| who is on the goal tile | nobody |

`expanded=8465` is the proof the retry fired; no partial route is being adopted
here. And `Truncated` is read **nowhere** outside `Path.fs` except two diagnostic
prints (`Program.fs:456-457`) and two assertions (`Tests.fs:627,653`). "Treat
`Truncated` as failure" moves no park — measured, not reasoned.

### What is actually wrong: a blocking chain whose only escape is an idle body

One tick of the park, the party packed into a single lane at y=125 (library seed 1,
`Maximum`, t=40000, 15 hostiles alive):

```
Warrior 15,125  dest 16,125  goal 32,106  path=30  -> wants the Druid's tile
Druid   16,125               goal 17,125  path=0   -> wants the Mage's tile
Mage    17,125               Goal = None          <- idle, claims 17,125
Rogue   18,125               Goal = None          <- idle, claims 18,125
Priest  19,125               goal 14,125  path=0   -> wants the Rogue's tile
```

Every member reports `moving=true`; nothing is casting or resting. The Priest's
relaxed route is 5 steps and its first step is the Rogue's tile; the Druid's is one
step into the Mage's. Both blockers hold **no `Goal`**, and `advanceGoals` only
visits entities that have one, so nothing ever asks them to move. `PartyAi`'s
step-aside fires only for allies within `reach` of the **Tank** (1 tile, 6 when the
Tank has a goal and is stuck — `PartyAi.fs:1302`), and this Tank is walking east
with `path=30`, so it never trips. A body that stands still claims its tile forever
(`SimState.claimedByOther` = alive **and** `Destination.IsNone`), the mover re-arms
`pathRetryBackoff` (`secTicks 1.0`, 20 ticks, `SimState.fs:165`), and the standoff
is stable. That is the livelock: the party walls itself in behind two members with
nothing to do.

### The gate: `stuckWithGoal` was too narrow

The aside shuffle was reachable all along — `movement`, which holds it, is defined
outside `match target with` and its comment says so ("Walking orders apply in and
out of a fight", `PartyAi.fs:1152`). What was wrong is when it looks: the aside
only considers allies within `reach` of the Tank, and `reach` is 1 unless the Tank
counts as stuck (`stuckWithGoal`, `PartyAi.fs:1302`). `stuckWithGoal` was
`t.Goal.IsSome && t.Path.IsEmpty && t.Destination.IsNone`, so the Tank above — full
`Path`, `Destination` set — read as *walking* and got `reach = 1`. The only ally
within one tile was the Druid, and the Druid has no free neighbour. Nothing else
was ever asked to move.

### The fix, and what it actually fixed

A `Destination` whose tile is held by a settled body now counts as stuck
(`heldStep`, `PartyAi.fs`), so `reach` becomes 6 and the queue is asked to drain
from its free end. Covered by `a tank held by a settled ally drains the queue from
the back` in `tests/WowRoguelike.Tests/Tests.fs` — red at `Some (4,4)` (the leash,
re-aiming the Priest back through the file) before, green at `Some (10,4)` after.
219/219 tests green.

Re-measured with the fix in the binary, six seeds at `Maximum`, 40000-tick cap:

| wing | seed | before | after |
|---|---|---|---|
| library | 1 | 40000, 15 mobs | **clears t=10100** |
| library | 2 | 40000, 15 mobs | **clears t=11176** |
| library | 4 | 40000, 15 mobs | **clears t=10156** |
| library | 5 | 40000, 15 mobs | **clears t=10718** |
| graveyard | 8 | 40000, 9 mobs | **clears t=11690** |
| graveyard | 5 | 40000, 12 mobs | still 40000, 12 mobs — different bug, below |

### The stale-DLL trap that nearly buried this

`src/WowRoguelike.Harness/bin/Release/net10.0/WowRoguelike.Core.dll` is only
refreshed by a build that includes the Harness. `dotnet test` writes
`tests/WowRoguelike.Tests/bin/.../WowRoguelike.Core.dll` instead. So an `fsi` probe
that `#r`s the Harness copy silently measures whatever was last built *there* — the
first reading of this fix said "still parked, no change" from a pre-fix DLL. Check
before believing a probe: the two copies must have the same sha256, and it must be
the one you just built.

### What is left: graveyard seed 5 is a shuffle, not a freeze

Every member is moving (`stall=0`, no held `Destination`s), and `PartyAi.decide`
issues 1 command. The party sits in a 2×3 pocket around `(37,174)` and drifts about
one tile per ten ticks without closing on anything; the nearest mob is 9–12 tiles
away and none of the 12 is targeting the party. Goals point at each other's tiles —
`Priest`'s goal is the Warrior's own tile, and the Warrior and Mage share `(36,175)`
— so the file orbits itself instead of translating.

That is a different failure from the one just fixed (there, nothing moved; here,
everything moves and nothing progresses), and it needs its own red test before it
is touched: sample the occupied-square set over ten ticks and assert it changes
*and* the party closes distance.

Ruled out for this one: "an idle body yields". The blocker's claim-aware search on
the frozen shape returned `len=0 expanded=8465`, i.e. no floor route avoiding the
file exists at all — a body with no free neighbour cannot sidestep. It would only
help in rooms with width.

Note the trap: `Path.astar`'s predicate argument is `blocked` (true = impassable),
not `passable`, while `Path.neighbours` takes `passable`. A probe with it inverted
reports every mob on the map as unreachable — that mistake was made twice here.

## Open bug 2 — the level-60 graveyard stall (older, still open)

Separate from the parks, and still unexplained: graveyard at L60 runs to the
24000-tick cap with `alive=5.0`, no kill, hostile count frozen. Previously
attributed to `advanceMovement` holding a mover whose destination is a settled
body's tile, with `MoveTo` rewriting `Goal`/`Path` but never `Destination`
(`SimCombat.fs:1058`; `SimTick.stepToward` at `SimTick.fs:304`). Clearing `Destination` on that branch was tried and
**reverted** — it unfreezes the pair and then the Tank shuttles between `(9,178)`
and `(11,177)` with 36 mobs alive. Do not redo it. The best-standing idea was to
give the destination up only after it has been blocked for *N* ticks, with *N* from
a replay probe rather than a guess.

Still live, not a stale note: `sim comps 2 --level 60` on the current tree reports
graveyard `unresolved=1` of 2 seeds for both rosters (`median-ticks=24000`,
`alive=6.0`), and library `unresolved=1` for one of them. The blocking chain in
Open bug 1 is the better candidate for the same signature; check whether the L60
stall is that livelock before working on it — and note that clearing `Destination`
was already tried and reverted.

## How to measure without lying to yourself

- **Pin the binary.** Several runs this session were wasted on a DLL rebuilt
  mid-sweep. Build once, copy the DLL aside, record its sha256, and run every
  comparison arm against that copy. A rebuild with `--no-incremental` that leaves
  the md5 unchanged is how you prove the probe you ran was the current source.
- **To see what a parked mover sees**, load
  `src/WowRoguelike.Harness/bin/Release/net10.0/WowRoguelike.Core.dll` from a
  `dotnet fsi` script, step `SimTick.step (Bench.autoPilot w) w` to the cap, then
  call `Path.astar w.Grid (SimState.claimedByOther w e.Id) e.Pos goal` and print
  `Expanded`/`Truncated`. `Expanded > 1200` proves the escalated retry ran, which
  is what separates "no route" from "budget".
- `dotnet run --project src/WowRoguelike.Harness -c Release -- comps <seeds>` is
  ~10 minutes for 36 cells, and `--no-build` is not optional in a scripted sweep.
- Tick budgets come from the harness: **graveyard 24000, library and armory
  32000**, loop gated on `SimState.continues`.
- `sim snapshots` regenerates `tests/WowRoguelike.Tests/snapshots/bench-30.txt`;
  any behaviour change moves it, and the diff is the cheapest review of the blast
  radius.
- Since the 5-second recovery change, **light content no longer needs a healer** —
  in `gully` L24 the Warrior never drops below ~92%, so a healthy run logs no
  player heal at all. Any fixture that assumes "a real run heals something" has to
  damage the Tank first.

## Working agreements this project keeps re-learning

- **Red first.** Verify the test fails on unmodified code. One fixture this session
  "passed" on old code because the candidates were mis-seated.
- **A frozen `alive=5.0` with an unchanging hostile count is a stall**, not
  difficulty. Run the same seed at a much larger cap before concluding anything.
- **Measure the ladder before claiming a fix.** The parks A/B showed a change that
  looked structural moved no number at all.
- **One writer per worktree.** Parallel agents were fine for scoped stages, but
  child model attestation has failed in this workspace before — treat a delegate's
  summary as evidence, then check the diff.
- **Revert temporary probes**; `git status` should show only intended work when you
  stop.

## Suggested skills

Call the **Skill** tool for these, in this order:

1. **`diagnosing-bugs`** — the parks are diagnosed but not fixed; the skill's
   reproduce-then-fix loop is how the last three movement bugs were closed.
2. **`ponytail`** — already active as a mode. The temptation is a general movement
   scheduler; the bug wants one branch and a `Truncated` check.
3. **`tdd`** — the fix needs a red test first, and a `comps` sweep is ~10 minutes
   per arm, so drive it with a unit test rather than replays.
4. **`code-review`** — for the uncommitted diff above, and for the older
   `654fc35..72e15f9` set that is still owed. Standards and Spec as parallel
   sub-agents.
5. **`pi-subagents`** — only if the next step is a large delegated sweep. Keep one
   writer per worktree.
