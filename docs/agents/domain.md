# Handoff — WowRoguelike

Repo: `/home/omary/Dev/WowRoguelike`. Written at `654fc35`; the reviewed change set
was committed at `72e15f9` and merged as HEAD `1e49951`, and the level-scale work
described below is staged on top of it, uncommitted. **169/169 tests pass, ~1m20s**
(`dotnet test tests/WowRoguelike.Tests/WowRoguelike.Tests.fsproj`), Release build 0
warnings.
**The change set is committed but was never reviewed.** `git diff 654fc35..72e15f9`
is 295 files (+14,894/−705); ~7,000 of those added lines are F# and tests, the rest
are extracted `Assets/` PNGs and generated maps. It landed as one commit with an
empty body, so the design log has no record of it — the review is still owed, and
this file is the brief for it. `images/` is now gitignored; `docs/HANDOFF.md` is
gone, replaced by this file, and was moved rather than rewritten.

**A second change set is staged and uncommitted on top of that** (HEAD is still the
`1e49951` merge): the level-scale work — ability ranks for all nine classes, the
level-24→60 gear curve, weapon rungs up to 60, the `smtrace`
harness command, two research checking scripts, eight research files, and the doc
updates. **163/163 tests pass, ~1m40s**; `mise run runs` reads `seeds=120
cleared=120 wiped=0 unresolved=0 median-ticks=950 min=794 max=1080`, which is this
file's baseline exactly. `mise run hooks-run` is clean. It is committed on top of `1e49951`, so citing a
hash here would go stale on the next amend; the amend to `72e15f9`'s empty commit
message is still owed.

**The Rogue control kit and the party-lead walk described in §8 are not in the
worktree.** They were written and then discarded before ever being staged:
reverting a delegate's edits with `git checkout -- .` also throws away every
unstaged change, and that work was unstaged. §8's Sap / Cheap Shot / Kidney Shot /
hound / stun-as-mitigation paragraphs are therefore a specification to reimplement,
not a description of the tree — the mechanics, the numbers and the test names are
recorded there. The `11480` / `10453` clears quoted in §9 came from that work and
will not reproduce until it is redone, and the `950 → 960 → 988` median drift no
longer exists.

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
  MapsScarlet.fs   761     Scarlet Monastery's four wings. **Generated** by
                           `tools/extract_sm_maps.py` from the battlemaps in
                           `images/Maps/ScarletMonastery/*.jpg` — do not hand-edit.
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
mise run test      # 169 tests, ~1m20s
mise run runs      # outcome distribution over 120 seeds — THE regression signal
mise run sim -- smrun cathedral med 16000    # run a wing headless, report survivors
mise run sim -- sm graveyard min             # place a wing at a band
mise run sim -- smtrace cathedral med 20000 2000 [v]  # progress samples: mob hp, party hp, resource; v adds routes and mob targets
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

**`mise run runs` is a fingerprint.** Currently:
`seeds=120 cleared=120 wiped=0 unresolved=0 median-ticks=988 min=794 max=1140`
(`README.md` §How correctness is checked records 950/794/1080 from `72e15f9`; the
+38 median is the walking fix in §8 and the pre-pull crowd control in §9 item 3 both
re-applying to the level-24 party). It moved
from the older `median-ticks=1331 min=1116 max=2248` when the level-24 rebalance and
the nine-class roster landed — this file predicted that shift, but no commit body
records which change produced it. Re-measure whenever behaviour
changes. Note the sweep drives the hand-authored `Content.gully` fixture
(`Bench.fs:22`), not a generated dungeon, and not a Scarlet wing.

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
| `ranks/ranks-{paladin,shaman,hunter,warlock}.md` | Rank tables for the four classes the earlier pass missed, plus armour and `VERIFY` lists. |
| `wc/mobs.md`, `wc/quests-and-scripts.md` | The 1.12 creature rows and the quest/script chain. |
| `vanilla-attack-table.md` | Miss/dodge/parry/glance/block/crit by level delta, spell miss, armour. |
| `level60-kits-at-24.md` | Level-60 kits scaled down to 24 by pool ratio. |
| `levelscale/README.md` + `dungeon-bands.md` | Per-dungeon minimum/median/maximum bands and the per-level curves. |
| `levelscale/bis-60.md` | **The gear ceiling.** Level-60 pre-raid best-in-slot per class, piece by piece, with gear-only subtotals. What `Sheet.Gear60` encodes. |
| `level24/base_stats_25_60.md` | Vanilla `player_levelstats` rows 25–60 per class, usable as-is; matches the sim's base table exactly at 40 and 60. |
| `levelscale/{warrior-rogue,mage,priest-druid}-ladder.md` | Superseded band ladders (28–32, 33–37, 38–40). Kept as evidence; the curve does not read them. |
| `levelscale/map-assets.md` | Which dungeon maps are usable as art, and the verdicts. |
| `scripts/rank-diff.py`, `scripts/rank-verify.py` | Cross-check the rank grids in `Content.fs` against `ranks/`; `--verbose` widens to range and cast time. `rank-verify`'s `DEVIATIONS` table lists the deliberate asymmetries. |
| `scripts/band-totals.py` | Sums a band ladder from `Content.fs` so the gear tables can be checked as numbers; it is what proved the 24→60 growth was flat. |

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

**Gear above 24 is one curve between two anchors, not a ladder of tiers.** The
researched band tables were replaced by `Sheet.Gear60`: the level-60 pre-raid
best-in-slot outfit per class, every stat grown from its level-24 value along
`at24 * (at60/at24)^((level-24)/36)`. Thirty-six is the span between the anchors, so
60 lands exactly on the research and the log line between is smooth. The level-24
value is a floor per stat, because the caster outfits *drop* Strength and Agility
for Intellect. Two consequences worth keeping: the totals at 60 are what
`bis-60.md` measures (a gear-only subtotal already includes the Agility armour and
any set bonus), so do not add the set's Agility to armour a second time; and the
Paladin's and Shaman's researched level-60 pick is a fast caster one-hander that
would swing weaker than their level-40 weapon, so those two rungs carry the 40 rung's
DPS at the researched speed.

**`Content.partyOf` fills a five-member party in roster order.** A nine-name roster
silently drops Warlock, Shaman, Hunter and Rogue — which is how a level-scaling test
came to check five classes while naming nine. Ask for one class at a time
(`partyOf [ name ] level`) when the test is about a class.

**`HealOverTime` is per second** (`Domain.fs:329`), not per tick and not per
three-second tick. Rejuvenation's lower ranks were transcribed as per-tick and ran at
2/4/7/11 instead of 3/5/10/15 until `scripts/rank-verify.py` resolved the column.
Compare against the research with the scripts, not by eye — the same script found
Fireball r3's cast time, Molten Blast r1's cost, Execute's whole ladder and the
druid/shaman rank counts.

**The hunter's researched weapon ladder is not monotonic.** His level-1 rung is the
calibrated level-24 bow (66–84 @ 1.80 s, 41.7 DPS) and the researched 28–40 bows are
17.9–22.8 DPS, so he loses 57% of his weapon DPS at level 28. That is pre-existing
24-band data, left alone; the level-60 rung is floored against the 24 rung instead
and the weapon-growth test starts at 29.

**Walking was made unstuck in two places, and one stall remains.** `SimState.stepToward`
passed `claimedByOther` into A\*'s cost function, so every tile another entity held
read as a wall; when that left no route at all the mover backed off, retried and
failed forever. Two changes landed:

- `stepToward` now plans against claims first (unchanged behaviour) and may ignore
  them **only when the mover has no free neighbour at all** (`sealedIn`). Measured:
  letting it ignore claims whenever the claim-aware search came back empty had the
  level-30 party dead by tick 6000 with 34 mobs left — it walks straight through a
  pack and pulls half the wing.
- `PartyAi`'s walking block keyed off `tank`, so losing the Tank withdrew every move
  order *and* the stand-aside rule; four survivors stood sealed around a member
  whose first step was blocked, byte-identical from tick 15000 to 20000. Movement
  now falls back to `lead = tank |> Option.orElse (actors |> List.tryHead)`; the
  tactical reads still use `tank`. Two regression tests cover it (`a party with no
  tank still gets walking orders`, and the approach ring test was dropped with the
  `approachTile` widening).

**The remaining stall is `approachTile` returning `None`.** When the whole ring
around a target is claimed, `approachTile` gives up and the caller falls back to the
target's own tile — a goal A\* can never reach, since the target stands on it. Widening
it to a second ring *was* implemented and *was* reverted: with it, `the scripted
party can clear a generated dungeon` wiped (`Expected: EncounterCleared / Actual:
PartyWiped`). Measured in `smtrace graveyard max`, the world is now frozen at 2
survivors and 20 mobs from tick 12500 on. **That is the next pathing task and it needs
a different shape than a wider ring** — a re-target or a per-entity "unreachable"
memory, not more ring.

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

1. **Mutual Party blocking: fixed** (`8d01db5`). Five abreast in a corridor
   claimed every tile of it at once; because none of them stepped, none ever freed a
   tile, and the claim-aware A* failed for all of them while the claim-free one
   succeeded. The diagnosis above was right and its proposed shape was the one that
   worked: `stepToward` now re-plans with a **stationary** ally's tile passable when
   the claim-aware search comes back empty, and still requires the first step to
   clear the hard claim, so a relaxed route can never put two bodies on one tile. It
   buys only the free prefix of a route, which is enough — one body moves, its tile
   frees, the queue drains. `a stationary ally no longer severs the route behind it`
   is red on `9ab48f3` and green after.

   `claimedByOther` was then relaxed to allow **transit**, at the request of the
   owner: a tile is claimed only by a body that has settled on it (`Destination =
   None`), so two members may pass through one tile as long as neither means to stop
   there. The earlier soft-claim attempt failed because a walker could step onto an
   ally and *stay*; this version keeps `no two living entities share a tile` by
   ending the tick with `SimTick.separateStacked`, which steps a duplicated body to a
   free neighbour. Neither entity is ever banned mid-tile and no route is lost, so a
   corridor carries traffic both ways instead of refusing it.

   Measured on the eight-seed freeze probe (24000 ticks): 8/8 frozen before, 0/8
   after, including the two seeds where the Tank lived and blocked the door.

   Measured at 12000 ticks, seed 1, frozen → moving: graveyard min froze at tick 8000
   with 27 mobs left and 5/5 alive at full health, and now reaches 37 left with 1
   alive; graveyard max 26 → 30 left; library max 16 left with 4 alive at 74% → 15
   left with all five at 100%.

   **The freeze had been masking fights the Party was losing.** An unfrozen Party
   pulls the packs a frozen one never reached, so on `graveyard min` it now grinds
   down to a single survivor where it used to stand at 100% health. Do not read the
   old 100%-health columns as the Party doing well: a stalemate is not a win. What is
   left of this bug's family is the `approachTile` stall in §8 — `library max` now
   walks until tick 8000 and then holds at 15 mobs left, 5 alive at 100% and mob hp
   frozen at 42%.

2. **The `approachTile` stall — the worst bug in the tree.** See §8. A widening was
tried and reverted because it broke a clear. The next shape should be a no-route
re-target: when `stepToward` cannot plan at all, clear the goal so `PartyAi` picks a
different hostile, and stop `approachTile`'s `None` from becoming "walk to the tile
the target is standing on". Check `mise run runs` and the generated-dungeon test
immediately; both moved under the last attempt.

A second shape of the same family, found while measuring compositions: the party
is at 100% health, no hostile is engaged (`target=-` everywhere) and nothing
bleeds for 20000 ticks, because the leading body **oscillates between two
adjacent tiles**. `Warrior/Druid/Mage/Rogue/Warlock` on `graveyard` median seed 20
shuffles `10,177` <-> `9,178` from tick 2000 to 24000 with `goal=18,179` and a
route of length 19-20 that is replanned every tick and reverses each time. A
no-route re-target will not catch it - there *is* a route, it just alternates - so
this one needs the mover to keep its current route instead of replanning when
replanning returns a first step that undoes the last one. Reproduced identically
on `591e9bf` and `a164db3`, so it is not the pass-through change.

3. **Scarlet Monastery wings need a full matrix before any tuning.** One seed per
   cell, 16000-tick budget, via `smtrace <wing> <min|med|max> 16000 16000`:

   | wing | levels (min/med/max) | minimum | median | maximum |
   |---|---|---|---|---|
   | graveyard | 30/33/34 | 35 left, 1 alive at 20% | 26 left, 2 alive at 38% | 26 left, 4 alive at 74% |
   | library | 33/36/37 | wipe (36 left) | wipe (27 left) | 15 left, 5 alive at 100%, stalled from tick 8000 |
   | armory | 37/39/40 | wipe at 6 left (mob hp 14%) | wipe at 6 left | **clear at tick 9918**, 4 alive |
   | cathedral | 38/41/42 | wipe (38 left) | wipe (25 left) | wipe (27 left) |

   One seed (1) per cell, 16000 ticks, `smtrace <wing> <min|med|max> 16000 16000`.
   Read it knowing that **difficulty sets the Party's level as well as the wing's**,
   and the Party gains more at the max anchor than the mobs do: the max column is
   the *surviving* one everywhere except cathedral. Armory clears at max and wipes at
   min; library stands at max and wipes at min. So the lever per cell is the level
   anchor, not trash density, and "make it harder" is currently backwards.

   Nothing here is a win rate — one seed per cell cannot say one. Wipes at 6-left are
   the closest thing to a finish line the Party has. `library max` contradicts an
   earlier note in this section: it does **not** clear at 11480 on this build (with
   the stun at the 45-energy gate it wipes at 26 left), so treat that number as
   retired.
4. **The pull protocol is specified by the user and only its first line exists.**
   The intended shape (user's words, condensed): *pre-combat CC that does not start
   combat (Rogue's Sap) first; then a ranged member opens on the priority enemy from
   distance — wand, bow or spell — and the group runs to a point that breaks line of
   sight; as the group runs in, more CC (Polymorph, Entangling Roots, Charm) minimises
   active enemies; once far enough from other packs the Warrior charges the priority
   target and holds the non-CC'd mobs with Taunt while the group burns the priority
   target from a safe distance. With a ranger, the ranger opens with a ranged attack
   plus a freezing trap, then as above.*

   In the engine already: `Path.lineOfSight` (spell range and mob aggro both use it),
   `Polymorph` in the Mage kit, `EntanglingRoots` in the Druid kit, `Gouge` (melee
   4s stun) for the Rogue, plus `Taunt`, `Charge`, `Stun` and `Sleep`;
   `SimTick.fs:353` makes aggro proximity + line of sight. **Not in the engine: Sap,
   any freezing trap, an out-of-combat-only cast rule, a pull point, or a
   priority-target ranking** (the party focuses by armour depth today).

   Shipped this session: `PartyAi.prePull` — while no hostile holds a target, the
   first able caster takes one enemy out of the fight, choosing by *preference*:
   **Sap first**, then Polymorph / Entangling Roots / Sleep, then Gouge last (they
   all reach the same target; Sap is the only one that starts nothing and costs a
   minute of cooldown, so it must not be spent where a short cooldown would do).
   `the rogue opens with Sap rather than Gouge` and `the party opens an unengaged
   pull with crowd control` cover it.

   Rogue kit as of this pass: **Sap** (3 tiles, 65 energy, 30 s incapacitate,
   **no cooldown** — being out of combat only *is* the limiter, so a second Sap can
   never happen inside a fight), **Gouge** re-modelled as a
   **`Sleep` incapacitate that any damage breaks** rather than a `Stun`, **Cheap
   Shot** (level 26, 4 s stun, 60 energy) and **Kidney Shot** (level 30, 4 s stun,
   20 s cooldown) so the Rogue can stun-lock a caster. Kidney Shot's vanilla
   combo-point scaling is a recorded `shortcut:` — `Effect` cannot express a
   duration that grows with combo points. Both land at 26/30, so the level-24
   anchor is untouched (and the `runs` sweep uses the default party, which has no
   Rogue, which is why the fingerprint stayed at `median=988`).

   **Stealth is not modelled, by design.** The Rogue counts as stealthed for the
   whole pre-pull window, and only a hound sees through it — Scarlet's `Scarlet
   Tracking Hound` (and `Houndmaster Loksey`, matched by name) within 6 tiles
   cancels Sap (`a hound sees through the rogue's stealth and costs the Sap`).
   That stands in for the Improved Stealth talent and is why there is no facing
   check and no Distract.

   **Stuns are mitigation, not only interrupts.** `Cheap Shot` / `Kidney Shot` go on
   the healthiest engaged enemy that can still act (`the rogue stuns an engaged enemy
   as mitigation`), gated at the **top of the energy bar** rather than on the
   recovered code's 45. "Stun and still strike" cannot both hold on a 100 point pool
   when Cheap Shot costs 60: the honest gate needs 105 energy and never fires at all,
   which is why the first cut of this gated on 45 and left the Rogue with nothing to
   swing. Taking the stun where regen would otherwise cap out is the version that
   measured best. A/B at 16000 ticks, seed 1, 45-gate → top-of-bar gate: **library max
   wipes at 26 left → 15 left with five alive at 100%**, **armory max wipes at 9 left
   → clears at tick 9918**, graveyard max 31 → 26 left (4 alive both), graveyard min
   25 left with 2 alive → 35 left with 1. Two wipes became a clear and a standstill.
   The graveyard-min row is what it costs.

   Measured: a ranged opener was tried
   *without* the tank pickup and made `library max` wipe — the casters pull while
   the Warrior is still walking; **do not add the ranged pull before the pull point
   and the pickup order exist.** The user's "wait for cooldowns and regenerate
   between pulls" tactic is likewise not implemented: the party has no notion of
   pausing between packs, so every pull starts on whatever cooldowns happen to be
   up. That is the next real piece of the protocol.

   **An opener must never be picked as an attack.** The ranged path's `shots` list
   originally took every `Foe` ability with `Range > 1`, so the Rogue fired Sap as
   a zero-damage "shot" the moment it was learned. `shots` now requires
   `damageOf a > 0`; add any future opener with that in mind.

5. **Wailing Caverns at level 24 does not clear** (wipes on two of three seeds,
   killing 28–54 of 111 hostiles in 9000 ticks). Its roster and quest chain are real;
   the gap is balance. Unchanged by the level-scale work — level 24 is the anchor.
6. **The class roster has no UI.** `Content.defaultRoster` is still a constant;
   nine kits exist and five get seated. `Content.partyOf`/`withRoster` are ready
   for a picker.
7. **Slice 4: boss phases.** `Phase` is in the glossary with no implementation.
   Five open design questions are listed in `TODO.md` §Slice 4 — put them to the
   user rather than guessing. `Mutanus the Devourer` is deliberately not stood in
   for: his fight is a summoning event with no sourced numbers.
8. **Mobs do not patrol.** The tank's pull-veto (`packGap`) is an approximation of
   avoiding patrol paths, not a patrol model. If patrols arrive, that heuristic
   should be revisited rather than layered on.
9. Smaller, from `TODO.md`: extract a named-field `Entity` builder (its trigger — a
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
3. **`code-review`** — before anything is committed. **~7k uncommitted lines**
   across 30+ tracked files have never been reviewed, and the last review found a
   genuine P1 (a move order could path an entity into a wall) that all 51
   then-existing tests missed. Run Standards and Spec as parallel sub-agents.
   The level-scale half of the change set has been checked by two research scripts
   and 169 tests; the gear curve and the rank tables have not been read by a
   reviewer.
4. **`diagnosing-bugs`** — for the pathing deadlock in §8/§9. It is a
   reproduce-then-fix job: `smtrace <wing> <band> 20000 2000 v` already prints the
   route, the goal, the mob targets and the stall flag, so the reproduction exists
   before any code changes.
5. **`ponytail`** — already active as a mode; read it if the session drifts toward
   over-building. Note the harness grew a `smtrace` command and two Python scripts
   this session; resist adding more tooling where an existing command would answer
   the question.

If the task is a hard bug or a perf regression: **`diagnosing-bugs`**. If it is a
large delegated workflow rather than interactive work: **`pi-subagents`** — and note
that child model attestation has failed here before (`model_verification_failed`,
expected `opencode-go/glm-5.3-flash:high`, observed `deepseek-v4.1-flash`), so check
before trusting a delegated result. Subagent launches are refused when the source
checkout is dirty, which it currently is. A worker given a 30-minute limit died at
the limit here after writing its file — check the activity log (`~/.pi/agent/
delegate-runs/<runId>/activity.jsonl`) and verify the file on disk (a clean
`dotnet build`) instead of re-dispatching it blind.

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
