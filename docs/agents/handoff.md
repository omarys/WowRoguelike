# Handoff — WowRoguelike

Written at commit `aba0722`, with that session's work on top as one WIP commit
`40cd7f4` and a second WIP commit `16a12b8` (the park fix — see Open bug 1).
Repo: `/home/omary/Dev/WowRoguelike` (F#, .NET 10, MonoGame shell).

**222/222 tests green**, Release build clean — re-verified on this tree with
`dotnet test tests/WowRoguelike.Tests -c Release`.

Everything settled — the brief, the architecture rules, the run table, and every
decision with its reason — is in `docs/agents/domain.md`. Read that first, then §9
(open work). Do not re-derive what it records; the reasoning in there is easy to
undo by accident. This file only covers where the work stands and what is next.

## The session's work is two unsplit commits

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
| Composition ladder — 36 cells, one healer per class | re-measured after the totem fix: 32 cells clean, 4 cells with one seed parked, 0 wipes |
| Corridor parks — a Tank whose next step is held by a settled ally now counts as stuck | fixed: 5 of 6 park seeds clear; graveyard 5 shuffles instead, below |

## Totems are scenery (changed after the above)

Asked for directly, and it simplified the sim rather than adding to it: a totem now
claims no tile (`SimState.claimedByOther`, `SimTick.advanceMovement`'s `held`,
`separateStacked`), is dropped on the Shaman's own tile instead of a free neighbour
(so a totem can no longer fail for want of room — only a pet still searches), and
carries `Lifetime = None` so it stands where it was dropped until that element is
dropped again. The slot rule in `SimCombat`'s `Summon` is now the only thing that
dismisses one. Measured after the change: **32 of 36 cells clean, 4 parked seeds,
0 wipes** — the same ladder result as before it, so this bought correctness and a
simpler corner rule, not clearances. 223/223 tests green, `bench-30` unchanged.

## CLOSED — both open bugs below were one dead totem

The two bugs written up under this heading were re-diagnosed by measurement on
this tree. They were not movement bugs. Both are closed by one three-line change.

Measured with `dotnet run --project src/WowRoguelike.Harness -c Release --no-build
-- comps 2 --level 60`, 36 cells:

| | before | after |
|---|---|---|
| cells fully clean | 29 | 32 |
| cells with a parked seed | 7 | 4 |
| parked seeds | 10 | 4 |
| wipes | 0 | 0 |

**Root cause.** `PartyAi.decide` built its resurrection candidate list as
"`Faction = Party` and not alive". A Shaman's totem is `Faction = Party` too — a
`Summon` with `Totem = Some …`, five health, immobile — so the moment a Stoneskin
or Grace of Air totem died, that totem was the first corpse by X order and the
Shaman cast **Ancestral Spirit at it, every tick, forever**, because a dead totem
can never come back. Casting clears `Destination` in `SimTick.advanceMovement`, so
each cast threw away the walk that had just been planned, and the healer never
moved.

That one loop produced both symptoms:

- **Bug 2 (the 3-tile freeze).** The healer stuck in place was past `leash` of the
  Tank, so `waiting` stayed true, so `hostiles` was emptied and the Tank was given
  `Stop` and no goal, so nothing ever aggroed and `inCombat` stayed false. Library
  L36 sat like that for the full 32000-tick cap. The frozen cell's own numbers say
  it: the Shaman at 23,125 with `Destination = Some (22,125)`, `MoveTicksLeft`
  8 → 7 → re-issued → 8, and a dead Stoneskin Totem standing on (22,125). The
  sim was issuing `UseAbility (EntityId 2, "Ancestral Spirit", EntityId 48)` every
  tick, and 48 was that totem.
- **Bug 1 (graveyard seed 5).** Same loop with no totem in reach of a pull: the
  party orbited in the 2×3 pocket while the healer re-cast, which is what the
  mutual-goal-overlap read of the log looked like from outside. It now clears at
  tick 11830.

**The fix** (`src/WowRoguelike.Core/PartyAi.fs`): `&& e.Totem.IsNone` on both
places that scan party corpses — the resurrection list and the `fleeing` plan's
`corpses`, which decided which safe tile raises somebody. A summon is not a fallen
member. No new state, no new pass, no change to movement.

**Two smaller holes closed on the way there**, both in `PartyAi.decide`, both now
carrying a unit test:

- `waiting` gated on *every* member being inside `leash`. A Dps nine tiles back in
a one-wide corridor held the party still, and the members in front of it had no
order at all, so the file could never close. Only the healers gate it now — the
wait exists so a pull does not start with the healer out of triage range.
- `waiting` used to Stop the Tank and issue *nothing* to the stragglers, which
means the wait had no exit even in principle. It is a regroup march now: anyone
past the leash with no plan walks in to the Tank. This is what surfaced the totem
bug — the walk was issued and thrown away every tick.

Regression cover: `a healer inside the leash does not hold the pull for a trailing
Dps` and `a member past the leash walks in to the tank while the party waits` in
`tests/WowRoguelike.Tests/Tests.fs`, plus the whole 221-test suite, the
`bench-30` snapshot unchanged, and the 36-cell ladder at 0 wipes.

**What is left (4 seeds, 4 cells).** Every remaining cell clears its other seeds,
so these are one-off seeds, not whole cells: graveyard L33 with the Druid, Paladin
and Hunter rosters, and library L36 with Priest+Mage+Warlock+Hunter.

They are **not** the freeze. The party is alive, the Tank holds a goal and mobs are
left 11–20 tiles away: it walks and does not close.

**One of the four is understood, and the obvious fix for it is wrong.** Library L36
Priest+Mage+Warlock+Hunter **seed 2** still has 15 mobs at tick 60000, double the
harness cap. The Tank sits at `(19,125)` with `goal = Some (32,106)`, `path = 0`,
`stall = 10`, `Destination = None`; a settled **Priest** stands on `(20,125)`, the
only exit, and both side tiles are walls. `Path.astar` with `claimedByOther`
returns **0 steps** (the claim-free search returns 27), so `stepToward` has no route
to keep and the stall backoff repeats forever. Note the asymmetry: the Tank's
*combat* branch already routes with `routeThrough` (hostile claims only) for
exactly this reason, but the out-of-combat leader walk does not.

The first attempt at it — order whoever stands between the Tank and its goal to
walk on toward that goal — **fixed this seed (clears at tick 7680) and cost the
ladder more than it bought**: 32 clean cells fell to 29 and parked seeds rose from
4 to 8, with graveyard L33 losing cells it had just won. It was reverted, and the
revert restores 32 / 4 / 0 wipes exactly. The lesson is the one this file keeps
re-teaching: the out-of-combat walk and the formation orders pull in opposite
directions, so a corridor fix belongs in the movement layer, not as another
`PartyAi` order. `SimTick.advanceMovement` already permits two bodies *passing*
through one tile — what it will not do is let a body *settle* on one that is going
nowhere. Letting a leader step onto a stationary ally's tile (and hold there until
it frees) is the smaller, local change, and it is the next thing to try. The three
graveyard seeds have not been re-run at a longer cap yet.

## Bug 1 as it stood — the parks (CLOSED above: one dead totem)

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

## Bug 2 as it stood — cells that stall 3 tiles from a pack (CLOSED above)

The ladder was re-run on the fixed tree (`sim comps 2 --level 60`, 7m52s): **36
cells, 29 clean, 0 wipes, 7 cells with 1–2 of their 2 seeds parked at the cap** —
10 parked seeds in total, all of them in graveyard L33 or library L36, never armory
L39:

| roster | wing | level | unresolved | median-ticks |
|---|---|---|---|---|
| Warrior/Shaman/Mage/Rogue/Warlock | graveyard | 33 | 2 | 24000 |
| Warrior/Shaman/Mage/Rogue/Warlock | library | 36 | 2 | 32000 |
| Warrior/Priest/Shaman/Mage/Warlock | graveyard | 33 | 2 | 24000 |
| Warrior/Paladin/Mage/Rogue/Warlock | graveyard | 33 | 1 | 24000 |
| Warrior/Priest/Shaman/Mage/Warlock | library | 36 | 1 | 32000 |
| Warrior/Priest/Mage/Warlock/Hunter | library | 36 | 1 | 32000 |
| Warrior/Priest/Mage/Rogue/Hunter | graveyard | 33 | 1 | 24000 |

All seven are five-member rosters, and the four with no second healer are the worst.
Every parked seed has `alive=5.0` or `6.0`, so nothing is dying — the fight simply
never starts.

### What the parked cell actually looks like

Reproduced outside the harness with `Content.withRosterAt 60 roster` on
`Content.scarletMonasteryAt "library" Content.Median 1UL`, then stepped to the cap:

- **The pack is 3 tiles away, on open floor, in the Tank's row.** Row `y=125`,
  `x=8..22` reads `...MMM..PP.....` — mobs at 11–13, party at 16–17.
- `Path.lineOfSight w.Grid tank.Pos mob.Pos` is **true** for all three nearest mobs.
- Every one of the 18 mobs is reachable: a flood from the Tank's tile (via
  `Path.neighbours` over `Grid.isFloor`) covers **8508** tiles and contains them all.
- And yet `PartyAi.decide` leaves the Tank with `Goal=None`, while every mob has
  `Target=None`. Nobody on either side is engaged.

So this is not geometry, not pathing, not reachability, and not the corridor parks
(in those, the Tank held a goal). It is **target acquisition**: the party does not
pick a pack it is standing next to, and the pack does not notice the party.

### It predates the park fix

Built `40cd7f4` in a worktree and ran the same cell against that DLL: `tick=32000`,
`mobs=18`, Tank at `(16,125)`, `goal=None` — identical to the fixed tree. The same
cell parks at the map's own level too (`withRosterAt 36`: 32000 ticks, 15 mobs,
`goal=None`), so it is not specific to the level-60 seating either.

That also means the ladder's older `unresolved=6` and this run's 10 parked seeds
are not comparable — different trees and no per-seed list on the old run. Quote
this table, not that number.

### Confirmed cause: `waiting` freezes the party, and nothing can un-freeze it

The filter is not the problem; the list is empty before it is consulted. `waiting`
(`PartyAi.fs:1044-1050`):

```fsharp
let waiting =
  not (SimState.inCombat w)
  && (drinking
      || match tank with
         | None -> false
         | Some t -> party |> List.exists (fun m -> Pos.chebyshev m.Pos t.Pos > leash))

let hostiles = if waiting then [] else ...
```

`leash` is 8 (`PartyAi.fs:30`). On the parked library cell at the cap, out of combat:

| member | pos | chebyshev to Tank | goal | stall |
|---|---|---|---|---|
| Warrior (Tank) | 16,125 | 0 | none | 0 |
| Rogue | 17,125 | 1 | (18,125) | 0 |
| Shaman | 23,125 | 7 | **none** | 0 |
| Mage | 24,125 | 8 | (18,125) | **9** |
| Warlock | 25,125 | **9** | (18,125) | **9** |

One member is one tile past the leash, so `waiting` is true, `hostiles` is empty,
the Tank takes no goal, nobody engages — and therefore `inCombat` stays false and
the wait never ends. The corridor is one tile wide (no floor at `y=124`/`y=126`),
so the queue cannot go round: `Path.astar` with bodies claimed returns `route=0,
expanded=1` for the Mage (boxed by the Shaman at 23, the Warlock at 25 and walls)
and `route=0, expanded=8453` for the Warlock. The Shaman at 23 is **idle** — 7
tiles from the Tank is inside the leash, so it is never given a follow order — and
the two behind it can never pass it.

So: within-leash members stand wherever they are, the file behind them jams on a
one-tile corridor, the last member stays over the leash, and `waiting` holds the
whole party out of combat forever. This also explains why the park fix does not
touch these cells: the Tank held no goal and only the *followers* were stalled.

### Two candidate fixes, both needing the ladder

1. **While `waiting`, give every member the follow tile, not just those beyond the
   leash.** The idle Shaman is the blocker and it has a free tile west, so the queue
   drains forward one step per tick until the last member is inside the leash and
   `waiting` clears. Risk: out-of-combat churn — everyone converging on one tile
   every time the party is out of combat can oscillate, and the formation tile is
   shared, so members may tail-chase.
2. **Bound the wait.** After *N* ticks out of combat with no member closing, drop
   `waiting` and fight with the party split. Risk: this is exactly the pull the
   leash was added for — it would trade a stall for wipes, so it needs the ladder,
   not an argument.

The red test to write first: a one-tile-wide corridor, Tank and two members behind
it, one idle member *inside* the leash between them, mobs three tiles ahead of the
Tank. Assert the party reaches the mobs. On the current tree that test hangs at the
cap, which is the bug stated in the smallest shape that has it.

Older note, still worth keeping: a graveyard L60 run to the 24000-tick cap with the
hostile count frozen was previously attributed to `advanceMovement` holding a mover
whose destination is a settled body's tile, `MoveTo` rewriting `Goal`/`Path` but
never `Destination` (`SimCombat.fs:1058`; `SimTick.stepToward` at `SimTick.fs:304`).
Clearing `Destination` on that branch was tried and **reverted** — it unfreezes the
pair and then the Tank shuttles between `(9,178)` and `(11,177)` with 36 mobs alive.
Do not redo it. The `Destination`-as-stuck predicate in Open bug 1 covers the same
shape without that regression.

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
