# Handoff — WowRoguelike

Written at commit `aba0722`, with that session's work on top as one WIP commit
`40cd7f4` and a second WIP commit `16a12b8` (the park fix — see Open bug 1).
Repo: `/home/omary/Dev/WowRoguelike` (F#, .NET 10, MonoGame shell).

**251 passed and 10 skipped, 0 failed** (`dotnet test tests/WowRoguelike.Tests -c
Release`, 260 total), Release build clean — re-verified 2026-11-05 on the tree at
`42975a1` (Hunter port `67d7c1c`, swing hook `42975a1`) and again on the proc axis
(§"The proc axis is built", below) and on Nightfall (§"Nightfall is built", below). The
10 skips are the operator's parked Bear/Cat/Moonkin specs.

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

## The Hunter's lane (a class that cast nothing, under seven landed talents)

The Hunter port's own measurement said "seven of sixteen landed, 100% mana in every
cell". The mana figure was the bug, not a finding. `PartyAi.decide` filed a Dps member
as `melee` when its kit held a `Foe` ability at range 1, and the Hunter holds Raptor
Strike — so it never saw `rangedOrders` and never fired a shot. On the harness at
library L36 seed 1 it dealt **15103 damage, 6228 of it Raptor Strike**, and Arcane Shot,
Aimed Shot, Multi-Shot, Serpent Sting and Viper Sting each logged **zero casts** for the
whole run. Eight of nine rows were dead.

The lane now keys on damage, not reach: `ranged` = a `Foe` row past range 1 with
`damageOf > 0` that is not an interrupt. The Rogue is the control that keeps it honest
(Sap reaches 3, deals nothing, stays melee). A ranged member still gets its adjacent
melee row as a **fallback ability** when every shot is on cooldown or unaffordable, but
never the `MoveTo(behindSpot …)` that `meleeOrders` ends its chain with — otherwise the
Hunter charges in on every gap and shoots its way back out.

**After: 15103 → 21381 direct damage on the same seed (27367 at L60), and minimum mana
97% → 0.1% at L36.** At L60 it still ends above 90% on a 3300-point Gear60 pool, so
"not resource-limited" is a level-60 claim only. `comps 2 --level 60` is **36 of 36 cells
clean**. The bench snapshot is **byte-identical to HEAD** — `defaultRoster` seats no
Hunter, so the bench cannot see this class (the same rule as the Warlock); do not read a
quiet bench as a quiet change. New test:
`a hunter shoots from its lane and a rogue stays in its own` (it asserts the Rogue keeps
its melee row, so a future lane change cannot quietly take Sap away).

The lesson, recorded in `docs/agents/domain.md`: **a talent port is only as live as the
lane its class fights in.** Nightfall and this are the same failure shape — mechanic
correct, AI never reached it. When a class's next talent is "more damage on a shot",
probe first that the shot is being fired.

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

## The proc axis is built (the first item the axis inventory ranked)

`.pi/research/octowow/axis-inventory.md` ranked the trigger/proc hook first: eighteen
talents across eight classes need an event the character did not choose — a block, a
crit, a hit. It is now a subsystem rather than a per-port invention. `Domain.fs` carries
`ProcEvent` (`OnCrit`, `OnBlock`, `OnSpellHit`), `ProcResult` (`GrantResource`,
`EmpowerNext`), `Proc = { Talent; Event; ChanceBp; InternalCooldown; Then }` and
`Talents.Procs`; `SimCombat.procOn` is called from the three places those events are
already decided. Three talents landed as proof, one per event and covering both result
kinds: **Warrior Shield Specialization 5/5** (block bp 500 → 1000 and 5 flat rage on a
block), **Druid bear Primal Fury 2/2** (5 rage on a crit — the "in Bear Form" clause is
the rage-only guard, so it needs no form field), **Mage Arcane Concentration 5/5** (10%
on a landed damage spell, 8 s ICD, fifteen seconds of `Empower.FreeMana`).

Read `domain.md` §"The proc axis" before touching it: the four decisions (flat not
`gainRage`, rage-only, ICD in `Entity.Cooldowns`, a 100% chance draws no random number)
are each one edit away from being silently undone. The one existing behaviour it moved is
the "next spell" gate in `resolveAbility`, which now reads `FreeMana` for any spell while
instant and guaranteed-crit stay heal-only — no kit row sets `FreeMana`, so nothing that
already existed changed.

Measured on a pinned binary: `comps 2 --level 60` is **36 of 36 cells clean, 0 wipes**
(the same answer as before, which is the only result a change that can only add rage and
free mana is allowed to give — the Warrior's block chance did move, so it was worth
checking). `bench-30` moved on all 30 seeds and was refreshed deliberately: the Mage's
proc draws a die where nothing drew one before, so every climb re-rolls (ticks −60..+34,
HP −239..+319, resource −627..+247, all 30 still cleared). The caveat to carry: the Mage
ends every cell at **97-99% mana**, so Clearcasting's rebate is invisible to every mana
floor — third class in a row where a level-60 caster's resource is a floor, not a
constraint.

And the part worth knowing before the next session plans anything: **the rest of A1 is
blocked by content, not by the vocabulary.** `domain.md` §"What is left of A1" has the
table — no Bloodrage in the Warrior kit, no Crusader Strike or Holy Shock in the Paladin's,
no source giving the Shaman's two numbers, and Moonkin parked. A8 (haste) and A9 (percent
multipliers) were ranked second and third by the inventory and **no landed kit has a
talent that would use either** — there is no Flurry, no Aspect of the Hawk, no Blade Rush
anywhere in `Content.fs`. That is the real state: the engine has more axes than the
content has talents, so the next move is a build, not a subsystem.

## Nightfall is built (A1's DoT axis, and the AI bug it exposed)

A1's fourth talent, and the first one a *tick* hangs off: `ProcEvent.OnDoTTick`, with
`Aura.Corrupted` carrying `caster: EntityId option` so the tick knows whose build to
consult, and `Empower.Only` scoping an empower to one named ability. Warlock Nightfall
2/2 (`ChanceBp = 400`) grants `Empower.Instant` restricted to Shadow Bolt for ten seconds.
Read `domain.md` §"The tick that belongs to the applier" before touching either half: the
`caster` is `option` on purpose (a casterless dot still ticks, it just fires nothing), and
the scoping gate in `resolveAbility` is the one line that keeps Inner Focus heal-only while
letting a named damage spell through.

**The finding worth carrying: the rotation could not reach the spell the talent names.**
A `dotnet fsi` probe against the built DLL counted 26 procs across six level-60 runs and
**every buff expired unspent** — because `PartyAi.rangedOrders` ranks spells by
`damageOf * 1000 / castTicks` and `damageOf` ignores a dot, so Immolate (4900) beats Shadow
Bolt (4780) by 2.5% and a `sim` Warlock casts Immolate 115 times and Shadow Bolt **never**.
Corruption and Curse of Agony score 0 and are never cast at all. That is pre-existing and
class-wide, not Nightfall's doing. The fix landed as one AI rule — a held `Empower` naming
a spell is a reason to cast that spell — which cannot touch any rotation that exists today
because no other buff is scoped. After it: 26 procs → 26 Shadow Bolt hits, all while
holding, mean 25 ticks held against 200 before. ``the warlock reaches for the spell its
buff names`` pins it, and pins the control too: with no buff held the Warlock still casts
Immolate, because the ranking was not re-tuned.

Measured on the pinned Core DLL
`d5e8db4873a3590d571dac80c2dee3040b521a9d88d41c2133519e006c05daa1`: `comps 2 --level 60`
**36 of 36 cells clean, 0 wipes, 0 parks** — the allowed answer for a change that can only
add an instant. The blast radius is visible in the same table and is the reason to trust
it: the three `Warrior/Priest/Mage/Rogue/Hunter` cells (no Warlock) came back **byte
identical**, and all thirty Warlock cells moved on ticks alone — mostly faster (graveyard
Priest lock 6255 → 5984, Druid lock 7072 → 6528), every one still 2 of 2 cleared.
`bench-30` did **not** move and should not: the bench roster is
Warrior/Priest/Rogue/Mage/Druid, no Warlock, so the bench cannot fire Nightfall. The caveat
is the usual one: the Warlock ends every cell at 97-100% mana, so an instant he casts
instead of casting is tempo, and this axis resolves it only through ticks.

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

**Nothing is left on the ladder: 36 of 36 cells clean, 0 wipes.** The four parked
seeds were one movement bug, and the fix is the one this file pointed at. `stepToward`
found a relaxed route when the claim-aware one was empty (a settled ally's tile
passable), and then **threw that route away** — `if taken next` discarded the whole
plan and set the stall backoff, so a leader whose very next step was a settled ally
backed off forever. The Tile Library's Tank at `(19,125)` was exactly that: goal
`(32,106)`, claim-aware route 0 steps, relaxed route 27, `path = 0`, `stall = 10`,
a settled Priest on the only exit and walls either side.

Two changes in `SimTick.fs`, both in the movement layer:

- a settled ally on the next step no longer discards the route — the mover keeps
  walking at it, and anything else on that tile (a hostile, a body mid-step) still
  means the route is stale;
- `swapStep` trades tiles when the step lands: the mover takes the ally's tile and
  hands back its own, atomically, so `separateStacked` never sees a stack. Party
  trades with Party only, and never with the Tank — the Tank is the formation's
  anchor, which is also what keeps `a stationary ally no longer severs the route
  behind it` honest.

Ordering the blocker forward instead had cost 3 cells; this costs none: the ladder
went 32 clean / 4 parked to **36 clean / 0 parked, 0 wipes**, and library L36 seed 2
now clears at tick 7121. `bench-30` was refreshed deliberately (seed 3: 1133 → 1139
ticks, party hp 4958 → 4771, same `EncounterCleared`, mobs untouched), because traffic
through a corridor is not the same route once bodies trade places.

**Re-measured, because one lucky run is not a result.** The ladder was run twice more
and both came back identical: 36/36 cells clean, 0 parked, 0 wipes. And the seeds
that used to park were re-run at a **60000-tick** cap, three times their harness
budget, precisely because "clears at 24000" cannot tell a fix from a seed that is
merely slow: Druid 6337/7232, Paladin 6592/8451, Priest+Mage+Rogue+Hunter
6450/6775, Priest+Mage+Warlock+Hunter 7200/8183, Priest+Mage+Hunter+Warlock
8475/7616, Shaman control 6956/6560 — all `EncounterCleared`, 5–6 alive, under a
third of the budget. Nothing is hiding at the cap.

`SummonSpec.Lifetime` was left in place on purpose. Nothing sets it to `Some` any
more (totems and the pet both pass `None`), so it looks like dead config — but it is
the field the "a totem has no clock" test asserts on, and deleting it would leave
that requirement pinned by nothing at all.

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

- **The scaled axis is knife-edge per seed, so quote the seed count.** At
  `comps 6` with no `--level` (map median: graveyard 33, library 36, armory 39) the
  tree after the swing hook is **31 of 36 cells clean** — five cells each losing
  exactly one seed, to a `PartyWiped` and never to a park. The same five cells at
  **12 seeds each wipe exactly once** (library seeds 6 and 5, graveyard seed 6), so the
  rate is ~1 seed in 12 and stable, not a class statement: one node in 12 wipes,
  reproducibly, on that roster. Before the swing hook the same 5 cells × 6 seeds were
  all `EncounterCleared` and `comps 6` was 35 of 36 clean (the one dirty cell the known
  park). **A change that cannot weaken the party (+10% white damage) moved this axis by
  four cells**, which is the proof that a one-seed flip between commits is RNG-path
  sensitivity, not a regression: quote the scaled axis with its seed count, and read a
  delta as a class finding only when a cell loses more than one seed.
- **The wipes are throughput death spirals, not resource or park failures.** Library
  seed 6, `Warrior/Priest/Mage/Hunter/Warlock`: the party is at full health with the
  healer at 3085/3085 mana until t=5600, then 6-8 mobs engage and stay engaged for the
  rest of the run; the Warrior and the Hunter die at t≈6900-7200 under a 1027-1127
  `Heal` cadence, and the Priest still ends at ~2500/3085. No cell at these medians is
  healer-mana-limited (`comps 6` ends every roster's healer above 90% mana), so no
  healer tuning can move a wipe — the failing seed simply pulls a sustained pack that
  the composition's single-target throughput cannot chew through.
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

## OctoWoW port — where it stands, and the parked Druid specs

**The operator's direction (m01837, paraphrase with ref):** *do not include Bear, Cat or
Moonkin in any tests for now; the focus is migrating the existing classes to the OctoWoW
rules.* So the ten tests that seat those three specs are `Skip`ped with that reason in
`tests/WowRoguelike.Tests/Tests.fs` — deleting the `Skip` argument re-enables each one.
The specs stay in `Content.fs` (the four-spec work is landed and green); they are simply
not measured. No ladder or `comps` cell seats them either, so nothing else needs gating.

| class | built | landed | notes |
|---|---|---|---|
| Warrior | Prot 3/0/48 | **6 of 18** | all six data-only; 12 need axes |
| Rogue | 17/34/0 | **6 of 18** | six data-only, plus two flat energy costs from the baseline changes |
| Mage | Arcane 40/Frost 11 | **4 of 19** | four data-only, incl. Presence of Mind as a kit row; 15 need axes |
| Priest | Disc 11/Holy 40 | **8 of 16** | best hit rate of the port; two talents had to be *deleted* |
| Shaman | 0/5/46 | 1 | `DamageDoneBp`-shaped talent only |
| Paladin | 40/0/11 | **7 of 17** | five lines and one deletion; Illumination is a **nerf** (10000 → 6000) and Divine Favor's cooldown row is gone; `comps 6 --level 60` 36/36 clean, bench 30/30 with the snapshot unmoved |
| Warlock | 31/0/20 | **8 of 17** | needed a model change: DoT ticks now take `Talents.damageBp` at aura-application time; `comps 6 --level 60` 36/36 clean, but the warlock ends every arm at 99-100% mana |
| Hunter | 0/41/10 | **7 of 16** | first class to get cheaper from an earlier port: Improved Stings rides the Warlock's DoT-at-application change with no new code; Lethal Shots had to become `Traits.CritBonusBp` (hero-wide crit). **The class then turned out to have never fired a shot** — the AI's lane test filed it as melee because of Raptor Strike. Fixed (`ranged` = a damaging `Foe` row past range 1), 15103 → 21381 damage at L36, `comps 2 --level 60` 36/36 clean, bench byte-identical (no Hunter in `defaultRoster`). The seven talents now sit on a class that uses them; re-rank the Hunter's (b)-bucket after that. |
| Druid | — | 0 | notes written (feral + moonkin), three specs `Skip`ped by the operator's direction |

The Warrior port is the template: each (a)-bucket talent lands as a field, each (b)-bucket
talent is left out and named with the axis it needs. Two findings from it are recorded in
`docs/agents/domain.md` §8 — Disarm's cooldown was wrong by a factor of three, and
`withTalents` now floors a talent-shortened cooldown at 1.5 s so no tree can zero a row.

The (b)-bucket is where the *next* class gets cheaper: eight axes recur across the nine
notes — self-resource grants, dispels, chain heals, stacking self-buffs, damage-taken
debuffs, temporary health, per-stance threat, and `Blocks` with a magnitude. Each one
built unlocks talents in three or four classes at once, which is why the port order below
is by axis rather than by class. Nine ports in, one axis has been built after the fact
(the Warlock's DoT-at-application change) and it immediately paid for a Hunter talent
with no code at all — the cheapest evidence yet that building an axis beats porting a
class.

**The swing hook landed** (`SimTick.advanceSwings`, `SimTick.swingName`). A white swing
has no ability row, so it answers to one fixed name and `Talents.damageBp` now folds into
both hands. That repairs the Warrior's One-Handed Weapon Specialization, which had been
tagged since its port and doing nothing — most of that tree's damage is white — and it
lets the Hunter's Ranged Weapon Specialization reach the bow. **A talent that has to tell
a bow from a dagger still cannot**: that needs a name on `AutoAttack`, and no landed
talent cares yet. The bench snapshot moved by 23 of 30 cells for this (mean 958 → 956
ticks, no outcome flips), which is the smallest honest evidence that the swing is now
where the tree lives, and `comps 3 --level 60` afterwards is **36 of 36 cells clean**.
On the scaled axis (map median levels) it is **31 of 36 cells clean at 6 seeds**, five
cells each losing one seed to a throughput wipe — see the instrument bullets above: the
same five cells wipe on exactly one seed in twelve, and a +10% white-damage buff cannot
make a party weaker, so those flips are the axis's resolution, not a regression. Nothing
in the level-60 ladder, the bench or the tests moves.

**Wave A is now one class from done: only the Druid is left**, and the operator has
parked Bear, Cat and Moonkin, so it is the feral/moonkin specs that have to be measured
before any Druid talents can be. Two axes recur there — per-form stat swaps (already
modelled) and `ThreatMultiplierBp` (already a field) — so the Druid is likely a data port
overlaid on a role question rather than a vocabulary one.

## TODO — research the OctoWoW class changes, all nine classes

**Requested by the operator, 2026-11-04 (m01257).** The operator wants this game's
class layer to mirror **OctoWoW**, not vanilla 1.12, and is willing to generate a
per-class build (talents + gear) to accompany each write-up.

Sources, one page per class — the operator's list repeats `rogue`, so it is nine
pages and not ten:

- <https://octowow.st/druid>
- <https://octowow.st/hunter>
- <https://octowow.st/mage>
- <https://octowow.st/paladin>
- <https://octowow.st/priest>
- <https://octowow.st/rogue>
- <https://octowow.st/shaman>
- <https://octowow.st/warlock>
- <https://octowow.st/warrior>

### Builds received

The operator's builds, decoded with the verified codec (3 bits per talent, 28 slots
per tree, three `-`-separated base64 chunks). These are arithmetic, not research:
every one lands on exactly 51 of 51, and the tree *names* follow the page order for
that class — the note for each class confirms the names against the payload:

| class (role the sim runs) | `?points=` | split |
|---|---|---|
| Warrior (tank) | `Y--VYFoKQSopQQYB` | Arms 3 / Fury 0 / Protection 48 |
| Paladin (healer) | `AoaAYQdQpAYoB--FYQAAI` | Holy 40 / Protection 0 / Retribution 11 |
| Priest (healer) | `AoFAB-aoFAAYToRYAoB-` | Discipline 11 / Holy 40 / Shadow 0 |
| Rogue (melee dps) | `aoYDI-FAroIAQoRCCAB-` | Assassination 17 / Combat 34 / Subtlety 0 |
| Hunter (dps + pet) | `-FoFoBDAoJQQoB-dQ` | Beast Mastery 0 / Marksmanship 41 / Survival 10 |
| Mage (caster dps) | `TAFoLISYZKAoB--FADD` | Arcane 40 / Fire 0 / Frost 11 |
| Warlock (caster dps) | `FoASaASABBFAB--FAVYAo` | Affliction 31 / Demonology 0 / Destruction 20 |
| Shaman (healer + totems) | `-F-FoDoaLFYQKFAB` | Elemental 0 / Enhancement 5 / Restoration 46 |
| Druid (caster dps) | `oCaYYKFYJLDAB--FAAoAC` | Balance 39 / Feral 0 / Restoration 12 |
| Druid (tank **and** melee dps, one build) | `BiAYAI-FoaYRaDQIIFIB-` | Balance 11 / Feral 40 / Restoration 0 |

Notes the builds imply, before any research is read:

- **Nothing is spread thin.** Every build goes 39–48 points deep in one tree. The sim
  currently folds talents in as flat traits, so a port that only reads the deep tree
  gets most of the value; a port that ignores the 10–12 point dips gets less.
- **The Warrior build is a full tank** (Protection 48, with only 3 points in Arms),
  which matches the ladder's Warrior tank — and the sim currently spends no points at
  all.
- **The Druid has two builds, not four.** Balance 39 for the caster, and Feral 40 for
  both the tank and the melee dps. The operator's stated intent:

  > "Druid tank and dps can share the same talent build … This will allow the Druid
  > to act as off-tank if too many enemies are engagad and pressuring the Warrior."

  That is a **role-switch requirement**, and it is the one thing the current bear kit
  cannot do: it holds Maul + Growl and has no area ability (a `shortcut:` in
  `Content.fs` says Swipe was left out because the Warrior kit had nothing to copy).
  An off-tank that cannot pick up several enemies cannot relieve a pressured Warrior,
  so the feral note is asked explicitly whether OctoWoW changes Swipe or threat.

### What the ten notes found

Every note lives in `.pi/research/octowow/`: `warrior`, `paladin`, `priest`, `rogue`,
`hunter`, `mage`, `warlock`, `shaman`, `druid-feral`, `druid-moonkin`. Counts are over
the talents each build actually takes, not over the tree:

| class (role the sim runs) | expressible today | needs a new axis | collapses a kit row |
|---|---|---|---|
| Shaman (healer + totems) | 7 of 16 | 7 | 2 |
| Priest (healer) | 8 of 16 | 7 | 1 (+3 named outside the build) |
| Rogue (melee dps) | 6 of 18 | 9 | 3 |
| Paladin (healer) | 7 of 17 | 9 | 1 |
| Warlock (caster dps) | 8 of 17 | 9 | 0 |
| Hunter (dps + pet) | 5 of 16 | 10 | 1 |
| Druid (caster dps) | 5 of 19 | 14 | 0 |
| Druid (tank + melee dps) | 6 of 21 | 15 | 0 |
| Mage (caster dps) | 4 of 19 | 15 | 0 |
| Warrior (tank) | 6 of 18 | 12 | 0 — but half its (b) bucket needs new `Effect` cases, not new fields |

**62 of 177 talent spends are data edits; 107 need a model change.** That ratio is the
headline: this is not a content port, it is a vocabulary change with a content port
attached.

### The four blockers that decide the order

1. **There is no resistance model at all.** `Domain.fs` carries only
   `PushbackResistanceBp`, which is spell pushback and not spell-school resistance.
   This single gap blocks a large share of Mage's 15: several talents differ only in
   which school they penetrate.
2. **`Talents` (`Domain.fs:1128`) speaks only healing.** Every field is
   `HealDoneBp`, `HealCostBp`, `CritArmorBp`, `SpiritToHealingBp`…; a talent that raises
   *damage* by 10% has no field to write to. This blocks Warlock, Mage, Hunter and Rogue
   alike, so it is one change that unblocks four classes — the highest-leverage edit in
   the whole port.
3. **A melee area ability already exists, and a comment says it does not.**
   `Content.fs:3941-3943` justifies leaving Swipe out of the bear kit: "the only Cleave and
   Whirlwind in this file are mob abilities". That is now false — `Effect.MultiShot of
   maxTargets` (`Domain.fs:807`) is implemented at `SimCombat.fs:406-427` and resolves
   "weapon damage to the target and up to `maxTargets - 1` other hostiles within 8 tiles".
   **This is the off-tank the operator asked for**, and it is data, not a new axis.
4. **There is no proc or stacking axis.** Eclipse (the Moonkin capstone), Omen of
   Clarity, Shadow Weaving and several Hunter talents all assume one. Nothing today
   models a state that accumulates and is consumed.

### The off-tank question, answered

`druid-feral.md` §6 puts it in three parts, and the third is the interesting one:

1. The bear needs a multi-target melee button — expressible **today** as `MultiShot`-shaped
   data, per blocker 3 above.
2. Swipe's threat must route through `Entity.ThreatMultiplierBp` so Feral Instinct's +15%
   reaches it, rather than arriving as a fixed number.
3. The bear has two buttons and little Rage to spend on a third: `gainRage` gives
   `2.5 * damage / level` (`docs/agents/domain.md:584-586`), which is why Ferocity's −5
   Rage matters far more here than its single rank suggests. **An off-tank that cannot
   afford its own area ability is the failure mode to watch.**

### Two premises of this session's briefs that turned out wrong

- I told the Rogue agent that combo points were not modelled. They are, and well:
  `Effect.ComboPoint` (`Domain.fs:882`), accumulation capped at 5 (`SimCombat.fs:793`),
  finisher scaling (`PartyAi.fs:733`), reset on spend (`SimCombat.fs:807`), and the AI
  gating finishers on `ComboPoints >= 4` (`PartyAi.fs:775`). The note corrected me; the
  ordering is load-bearing (a `ComboPoint` appended before `Finisher` is zeroed by it).
- I briefed the Warlock note as "no demons, so pet talents are blocked". The build takes
  almost no pet talents, so that blocker barely matters — the class is blocked by
  blocker 2 instead.

### Obsolete rationale the notes turned up

Each is a comment or `shortcut:` in `Content.fs` that OctoWoW makes untrue, and each will
mislead the next reader if left alone:

- `Content.fs:3441` — "shortcut: Power Infusion is not modelled". OctoWoW **deletes**
  Power Infusion, so the shortcut describes a hole that no longer needs filling.
- `Content.fs:3435` — `priestTalents` claims "Holy Specialization 5/5 (+5% crit)" for a
  bonus the record never carried. §4 of `priest.md` shows Holy Specialization is replaced
  by Divinity anyway.
- `Content.fs:3941-3943` — the Swipe rationale, disproved above by `Effect.MultiShot`.

The project already has the right convention for the rest: `Domain.fs:1126` — *"A talent
the vocabulary cannot express … is named in a comment beside its build and left out rather
than half-modelled."* Follow it. Do not build a general talent engine.

### Suggested order of work

1. **`Talents` gains a damage half** (blocker 2). One record change; unblocks four classes.
2. **Shaman, then Priest.** Cheapest per talent, and both already have their mechanics
   (totems, heals). Shaman's totem talents write to `TotemBoon`-adjacent data that exists.
3. **Druid feral as the off-tank** (blocker 3) — `MultiShot`-shaped Swipe, threat via
   `ThreatMultiplierBp`, then measure whether the Rage economy can pay for it. This is the
   operator's stated goal and it needs the least new vocabulary of any role change.
4. **Rogue, Paladin, Warlock, Hunter.** Rogue's premise surprise (combo points already
   modelled) makes it cheaper than its 9-axis count suggests.
5. **Resistance model** (blocker 1), then **Mage**: last, because it is the most blocked
   class in the set and the least rewarding to do first.
6. **Proc/stacking axis** (blocker 4) only when Eclipse or Omen of Clarity becomes worth
   modelling on its own — not speculatively.

What the research has to answer, per class, before any code moves:

1. **Which abilities change, and into what** — new ranks, renamed or merged
   abilities, changed coefficients, costs and cooldowns. The sim's kits are
   `AbilityDef.kitAt` ladders in `src/WowRoguelike.Core/Content.fs`, so a change is
   a rank band, not a list entry.
2. **What the resource model becomes.** Rage, mana, energy and combo points have
   assumptions baked into `SimCombat.fs`; a class change that alters generation or
   decay is a core change, not a content one.
3. **What it does to the two gear anchors.** Every spec carries a level-24 outfit
   and a level-60 `Gear60` sum. If OctoWoW moves a class from Intellect to
   Stamina, or adds a feral bench, both anchors move.
4. **What breaks the shapes the sim already encodes** — forms (the Druid's four),
   pets (`Name = "Pet"`), totems (`TotemSlot.ofPulse`), shields, stances. Those
   have art and AI wired to them; a class change that adds a tenth shape is a
   sprite-pipeline task too.
5. **Whether the numbers are lining up with anything** — OctoWoW is a private-server
   project, so establish first whether its baseline is 1.12 or a later client, and
   whether tooltips are published as text. A balance layer fetched from nowhere
   verifiable is worse than vanilla numbers, because it cannot be checked.

Deliverable shape: one note per class under `.pi/research/octowow/<class>.md`, in
the form `.pi/research/level24/druid.md` already uses — itemised, every number
attributed to the host it came from, an explicit **Contradictions** section, and a
**Missing evidence** section. Then, not before, a diff plan against `Content.fs`.

Sequencing note: the operator's per-class build is the useful second half of each
note — it becomes that class's `Gear60` anchor and the talent path the sim
implicitly assumes. Do not encode a build the operator has not actually chosen.

## TODO — three Druid specs have gear but no ladder coverage

Measured while wiring per-spec gear (commit range ending at the per-spec gear
commit): seating `Druid (Bear)`, `Druid (Cat)` and `Druid (Moonkin)` into the
ladder party at level 60 gives

| map | Bear | Cat | Moonkin | `Druid` (Tree) |
|---|---|---|---|---|
| graveyard | **3/3 unresolved at the 24000 cap** | 3 clear, 7907 | 3 clear, 7840 | 3 clear, 6763 |
| library | 3 clear, 5865 | 3 clear, 6110 | 3 clear, 6146 | 3 clear, 6296 |
| armory | 3 clear, 5609 | 3 clear, 5844 | 3 clear, 6097 | 3 clear, 6025 |

So the bear's graveyard cell is open. The gear totals themselves were checked
against two hosts and summed by hand, which is why this is recorded as a
*behaviour* gap and not a data one: an outfit can be right and the body still never
finishes. Note the phase-one shortcut in the same breath — the bear's kit is Maul
plus Growl (Heroic Strike and Taunt under feral names), so it has no area damage
and no second cooldown, and graveyard is the map with the most walking. Whether
that is the cause is **unmeasured**: the 60000-tick probe that would separate a
stall from slow killing timed out rather than answering.

Two reasons not to chase it yet: no `comps` cell seats these three specs, so
there is no ladder to A/B against; and the OctoWoW research above rewrites the
same kits. Fixing a kit that is about to be replaced is the definition of doing
the work twice.

## TODO — the scaled ladder's one-seed cells: diagnosed, policy question open

Found while re-measuring after the first Shaman change, and **pre-existing**: the same two
cells fail on `HEAD` with the Shaman edit stashed, so they are not a Shaman regression.

The ladder has two axes and only one of them has been watched. `comps N` with no `--level`
seats each map at its own level (graveyard 33, library 36, armory 39); `comps N --level 60`
seats everything on the level-60 sheets, which is the axis every "36/36 clean" claim in
this document was measured on. At `--level 60`: 36 of 36 clean, including with the Shaman
change below. **Level-60 axis: 36/36 cells clean at six seeds.** Level-scaled axis:
**33 of 36 cells fully clean at six seeds**, three cells losing seeds and all three in
library.

> **Correction, and the reason it is here.** This file claimed "36/36 on the scaled axis,
the first time it has ever been fully green" after the Rogue port. That was measured with a
Harness binary `dotnet build` had declined to recompile. With a forced rebuild the scaled
axis is 32/36 at six seeds — see `docs/agents/domain.md` §8 (Mage). Any A/B here needs
`-t:Rebuild` and a printed DLL hash.

Seeds matter more than the ladder does. At **two** seeds the scaled axis reads 35/36; at
**six** it reads 32/36, and the difference is not one bad cell appearing, it is four
library cells each losing one or two seeds out of six. The single cell that fails at two
seeds — `Warrior/Priest/Mage/Hunter/Warlock library`, median 9666 — fails identically
before and after the Mage port, so it is not a Mage regression either. **A two-seed green
run means "no cell failed twice", not "no cell fails".**

The failing cells at six seeds, as of the Mage port:

| roster | seeds cleared | wiped | median ticks |
|---|---|---|---|
| Warrior/Priest/Mage/Rogue/Warlock | 5 of 6 | 1 | 8650 |
| Warrior/Druid/Mage/Rogue/Warlock | 3 of 6 | 3 | 8843 |
| Warrior/Shaman/Mage/Hunter/Warlock | 5 of 6 | 1 | 9308 |
| Warrior/Shaman/Hunter/Rogue/Warlock | 5 of 6 | 0 (1 unresolved) | 9408 |

Three of the four carry a Mage, which is where the port is suspected but not proven: the
Mage's twelve unported talents are the damage ones, so the class is currently a caster
that casts faster and crits a little harder on two spells and nothing else. Library at
level 36 is the tightest cell in the ladder and was already the map that failed before any
talent landed.

**After the Priest port the same axis reads 33 of 36 fully clean**, and the set changed
again: `Warrior/Druid/Mage/Rogue/Warlock` still loses 3 of 6, the two Shaman cells are now
clean, and the two 5-of-6 cells both carry a Priest. So a healer's numbers moved cells
whose roster contains no healer change at all — worth remembering before attributing any
single cell to a single class.

**Resolved after the Hunter port and the swing hook (2026-11-05).** Forced rebuild, DLL
md5 `42d25151a89ac0d2c7fc3e686f4e177d`, `comps 6` with no `--level`: **31 of 36 cells
fully clean**, every failure a `PartyWiped` and every one exactly one seed of six.

| roster | map | seeds cleared | median ticks |
|---|---|---|---|
| Warrior/Druid/Mage/Rogue/Warlock | armory | 5 of 6 | 7836 |
| Warrior/Paladin/Mage/Rogue/Warlock | armory | 5 of 6 | 8186 |
| Warrior/Priest/Mage/Hunter/Warlock | graveyard | 5 of 6 | 10468 |
| Warrior/Priest/Mage/Hunter/Warlock | library | 5 of 6 | 9031 |
| Warrior/Priest/Mage/Rogue/Hunter | library | 5 of 6 | 9217 |

The old worst cell — `Warrior/Druid/Mage/Rogue/Warlock library`, 3 of 6 — is now clean,
and the losing cells have spread to armory and graveyard. Three probes settle what that
means:

- **The failure is a property of the seed, not a rate.** Re-run at 12 seeds, the three
  sampled cells wipe exactly once each (library seeds 6 and 5, graveyard seed 6): 1 in 12,
  the same seed that failed at six. A one-seed cell is not "17% broken", it is one node.
- **The swing hook moved this axis without being able to weaken the party.** The
  pre-hook binary (`67d7c1c`) clears all five cells at seeds 1-6 and reads **35 of 36**;
  the hook is +10% white Warrior damage, so the four-cell delta is RNG-path divergence —
  different kill order, different path, a different seed's pull arrives at a different
  time. This is the clearest evidence yet that the scaled axis is measuring paths.
- **The wipes are throughput, not park and not mana.** Traced library seed 6
  (`Warrior/Priest/Mage/Hunter/Warlock`): full health with the Priest at 3085/3085 mana
  until t=5600, then 6-8 mobs engage and never disengage; Warrior and Hunter die at
  t≈6900-7200 under a 1027-1127 `Heal` cadence, and the Priest finishes at ~2500/3085.
  `comps 6` ends every roster's healer above 90% mana, so no healer tuning can move a wipe.

So this TODO is **not** a class bug and there is no code fix queued for it: the open
question is a policy one — either a level-median party is allowed to lose a sustained
6-8 mob pack on one map seed in twelve, or the pull is too large and the fix belongs in
content density or the party's target selection, not in the ladder. Until that is decided,
quote the scaled axis with its seed count and treat any one-seed flip between commits as
path noise.

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
