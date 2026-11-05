# Handoff — WowRoguelike

Repo: `/home/omary/Dev/WowRoguelike`. Written at `654fc35`, rewritten at `1fe8f95`,
re-measured after the mana model below.
**The tree is dirty: the Innervate / Mana Tide / five-second-rule / drinking-gate work
is uncommitted across ten files; 196/196 tests pass, ~30s**
(`dotnet test tests/WowRoguelike.Tests/WowRoguelike.Tests.fsproj`), Release build 0
warnings, `mise run hooks-run` clean. Everything below that predates those ten files is
committed at `1fe8f95`, each change with a body that records the decision and its
measured outcome — `git log` is the design log, and `654fc35..1fe8f95` is where the
level-scale, kit and corridor work lives.

**The `654fc35..72e15f9` change set was never reviewed.** 295 files
(+14,894/−705); ~7,000 of those added lines are F# and tests, the rest are extracted
`Assets/` PNGs and generated maps. It landed as one commit with an empty body, so
the design log has no record of it — **that review is still owed, and this file is
the brief for it.** `images/` is gitignored; `docs/HANDOFF.md` is gone, replaced by
this file, and was moved rather than rewritten.

**What has landed since:** the level-scale work (`9ab48f3` and the healer ranks and
totems above it), the **Rogue control kit at `ae920c9`** — §8's Sap / Cheap Shot /
Kidney Shot / pre-pull openers are implemented, tested and in the tree, so §8 now
reads as a description of the code rather than a specification to reimplement —
and the corridor work in §9 item 1: `8d01db5`, `978a1bc`, `d5eb335`, `5f43d19`,
`82e64f1`. The map browser (`5651086`, `779290a`) is the MonoGame shell's only
content. The `11480` / `10453` clears quoted in §9 came from the Rogue-kit work and
reproduce again; the `950 → 960 → 988` median drift does not, because the corridor
changes moved it.

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
| Git commit bodies | Each records a decision *and its measured outcome*. `git log` is the design log — the one gap is `72e15f9`, which landed a 295-file change set with an empty body, so the log stops being one between `654fc35` and the level-scale work above it. |

## 3. Architecture, and the rule that enforces it

```
src/WowRoguelike.Core/     ← MUST NOT reference MonoGame. Compile order = dependency graph.
  Domain.fs       1256     types, the Aura module, ZERO logic
  Rng.fs            67     splitmix64, partitioned streams
  Heap.fs           78     hand-rolled binary min-heap
  Path.fs          277     A*, Dijkstra, greedy baseline, line of sight
  Dungeon.fs       317     rooms-and-corridors generation, BFS flood fill
  Maps.fs          402     Wailing Caverns rooms extracted from images/Maps
  MapsScarlet.fs   761     Scarlet Monastery's four wings. **Generated** by
                           `tools/extract_sm_maps.py` from the battlemaps in
                           `images/Maps/ScarletMonastery/*.jpg` — do not hand-edit.
  Content.fs      4965     abilities, rank tables, nine class kits, mobs, World builders
  SimCombat.fs     966     the attack table, armour, resources
  SimState.fs      291     queries, record rebuilds, ability/command resolution
  SimTick.fs       644     movement + the per-Tick pipeline
  PartyAi.fs      1057     the party's own AI: tank, healer, dps, rogue positioning
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
mise run test      # 185 tests, ~1m
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
`seeds=120 cleared=120 wiped=0 unresolved=0 median-ticks=870 min=778 max=1095`
(`README.md` §How correctness is checked records 950/794/1080 from `72e15f9`; the
+38 median was the walking fix in §8 and the pre-pull crowd control in §9 item 3
both re-applying to the level-24 party, and it has since come **down** 114 ticks,
because the between-pull drinking gate stops the followers shuffling: the party
stands still while its casters drink instead of stepping aside to unseal a Tank
that is not walking. Every seed still clears. `median-ticks=1331 min=1116 max=2248`
is the older still, from before the level-24 rebalance and the nine-class roster
landed — this file predicted that shift, but no commit body records which change
produced it. Re-measure whenever behaviour
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
| `nether.wowhead.com/tooltip/spell/<id>?dataEnv=4` | Not a file — the vanilla tooltip dataset, for ranks a table stops short of (Healing Wave 8–10). It prints the value at the scaling cap, the same convention the tables use, and `hi` one higher than they record. `dataEnv=8` is **Wrath** (Level 80, "25% of base mana"); it returns different numbers for the same spell id. Validate any new id against a rank the tables already have — ranks 5 and 7 pinned this one. |
| `wc/mobs.md`, `wc/quests-and-scripts.md` | The 1.12 creature rows and the quest/script chain. |
| `vanilla-attack-table.md` | Miss/dodge/parry/glance/block/crit by level delta, spell miss, armour. |
| `level60-kits-at-24.md` | Level-60 kits scaled down to 24 by pool ratio. |
| `levelscale/README.md` + `dungeon-bands.md` | Per-dungeon minimum/median/maximum bands and the per-level curves. |
| `levelscale/raid-gear-60.md` | **The gear ceiling.** Level-60 Tier 1 / Molten Core + Onyxia outfit per class, piece by piece with item ids, boss and two-host confidence. What `Sheet.Gear60` encodes today. |
| `levelscale/bis-60.md` | Superseded as the ceiling: level-60 *pre-raid* best-in-slot per class. Kept as evidence; `Gear60` no longer reads it. |
| `level24/base_stats_25_60.md` | Vanilla `player_levelstats` rows 25–60 per class, usable as-is; matches the sim's base table exactly at 40 and 60. |
| `levelscale/{warrior-rogue,mage,priest-druid}-ladder.md` | Superseded band ladders (28–32, 33–37, 38–40). Kept as evidence; the curve does not read them. |
| `levelscale/map-assets.md` | Which dungeon maps are usable as art, and the verdicts. |
| `scripts/rank-diff.py`, `scripts/rank-verify.py` | Cross-check the rank grids in `Content.fs` against `ranks/`; `--verbose` widens to range and cast time. `rank-verify`'s `DEVIATIONS` table lists the deliberate asymmetries. |
| `scripts/band-totals.py` | Sums a band ladder from `Content.fs` so the gear tables can be checked as numbers; it is what proved the 24→60 growth was flat. |

Warcraft Wiki is the authority. **Classic Era numbers win when sources conflict.**

## 8. Hard-won knowledge — do not re-litigate

Each is recorded in a commit body or a code comment; this is so you do not repeat
the investigation.

**A totem is faction `Party` but is not a party member.** It carries
`Faction = Party` so mobs can target it; `SimState.aliveParty` filters `Totem.IsNone`.
Without that filter a live totem answers for a dead party in `outcome`, offers itself
as a heal target, and joins the formation — it read as `alive=7.6` on a five-person
party until `3c098f7`.

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

**Two healer utilities were invisible to the AI, and a name list would not fix it.**
`PartyAi.cooldownFor` (`PartyAi.fs:397-418`) is the only place a healer casts a
cooldown, and it fires on exactly two effect shapes: `Empowers` while the target is
under 60% health (`needsFastHeal`, `PartyAi.fs:395`), and `ConsumeHealOverTime` when a
Rejuvenation is worth eating. That
reaches Nature's Swiftness, Inner Focus and Swiftmend — every one of them a button
"cast on the way into a heal", as the comment there says. (Divine Favor used to be the
fourth; the OctoWoW port deleted its kit row, so the Paladin now has none.) **Innervate
and Mana Tide Totem matched neither branch, so they were never cast** — not because
the AI did not know their names, but because the matcher had no shape for a cooldown
that restores mana. The lesson is the fix that worked: a new **effect shape**, not a
new name in a list. Mana Tide needed no new shape — it was already a `Summon` — only
a **slot rule**: `Domain.totemSlot` derives each pulse's element from the data,
`SimCombat`'s `Summon` dismisses whatever stands in the same slot, and `totemOrders`
prefers the tide over the spring when **a healer is under 60%** or three hostiles are
engaged, falling back to the spring while the tide is on cooldown. Divine Protection
is still unreached: it is damage immunity, not a heal and not mana, so `cooldownFor`
has no shape for it either.

**A totem is scenery, not a body.** It claims no tile — `SimState.claimedByOther`
skips it, `SimTick.advanceMovement`'s `held` set skips it, and `separateStacked`
neither counts it nor moves it — so the party walks over it and none of the corridor
traffic rules apply to it. It is dropped on the Shaman's own tile rather than a free
neighbour, which also removes the old "has no room to summon" failure for totems
(only a pet still needs a free tile). And it has no clock: the kit passes
`Lifetime = None`, so a totem stands where it was dropped until that element is
dropped again — the slot rule is the only thing that dismisses one.

**A cooldown button is only as reachable as the branch that routes to it.** The trip
above is necessary but not sufficient: `heal` is reached only through the dispatch at
`PartyAi.fs:521-523`, and that dispatch read `projectedPct` — health plus the HoT
*about to go up*. A tank at 55% with a lapsed Rejuvenation therefore looked covered to
`directTarget`, the party "covered" him by re-casting Rejuvenation, and the fast heals
fired only below ~25%, where a whole Regrowth no longer fit the hole. The measured
signature is a cooldown that is castable, affordable, off cooldown and never fired.
The fix is a branch, not a threshold: `hurtTarget` reads **real** health
(`wounded |> List.sortBy SimState.healthPct`, `PartyAi.fs:521`) and sends a target
under 60% to `heal` regardless of projection — a spike outranks a refresh, and the
projected heal is a cast that has not started. A HoT refresh is worth 2 s of nothing
precisely when Swiftmend is worth 0 s.

The effect on the ladder is a trap for the unwary: the same 30 bench seeds all still
clear, 1% faster (896 vs 905 median ticks), with mana left rising 63% → 90% — every
healer was spending the expensive reflex (re-cast the HoT) where a direct heal was
cheaper. A balance snapshot that moves 30/30 lines is drift, not breakage; check the
`outcome=` column before re-baselining.

**Swiftmend pays a fixed 12 seconds, which is why the last tick is the play.**
Vanilla Swiftmend consumes a Rejuvenation or Regrowth and instantly heals an amount
equal to **12 s of Rejuvenation or 18 s of Regrowth** — independent of how much of the
buff is left. `Aura.swiftmend` (`Domain.fs:467-483`) is that rule: the heal comes from the
buff's *rate*, never from `healOverTimePendingFor`, so a press at the last tick pays
exactly what a press at the first tick pays. The implementation used to hand over the
pending healing instead, which made the whole mechanic a slow bleed — every second of
delay shrank the payout — and that misreading is worth recording, because it is
invisible in a damage log: both versions heal, only one rewards waiting. With the
fixed amount the right play inverts: let the HoT tick down and press at the end, which
keeps every Tick already paid *and* the full instant heal. Pressing early pays the
same and throws the remaining Ticks away.

That gives the button two routes (`PartyAi.fs:403-413` and `PartyAi.fs:528-566`). The emergency
one is unchanged — a target under 60% eats its HoT immediately, last ticks included.
The other is the last-tick route: an ally under 90% whose Rejuvenation or Regrowth has
≤ 2 s left, and where the instant heal **lands whole**
(`t.Health + instant <= t.MaxHealth`, one comparison rather than an estimate, so using
it outside an emergency can never overheal). Ordering is by lowest health, since the
payout is the same for every eligible buff. Consumption prefers Rejuvenation, as in
the game — the Druid re-casts it for less than Regrowth's up-front heal. Measured over
the 30-seed bench after the fix: 30/30 still clear, +0.3% ticks, −948 party health,
−766 mana — the HoT is no longer converted into an early payout, so it pays out over
its full duration and the mana saved is the Re-casts that conversion used to force.

**Regeneration is Spirit-shaped now, and casting is what costs it.** The model was a
flat point of mana a Tick for everybody, which made Spirit a stat that did nothing
and made `Talents.RegenBp` (Meditation, Reflection — "15% more regeneration") floor
away to zero. `Content.derive` now reads the rate off Spirit
(`max 1 (Spirit * 4 / 100)`, so a level-24 Priest sits at 2 and a raid-geared one
much higher), and `SimTick.advanceResource` implements **vanilla's five-second rule**
as the shape this sim can afford: a `RecentlyCast` aura is stamped on every ability
that is paid for (`SimCombat.resolveAbility`, so an interrupted cast — which costs
nothing — does not start it), and while it stands the rate pays **one point a Tick
less** rather than a fraction. That is the honest integer version of the rule and it
makes the class difference the rule exists for: a Spirit-heavy Priest regenerates
through a cast, a thin-Spirit Mage does not. Out of combat nothing changed — that is
still the pool-percentage drinking rate.

**Innervate is a duration, not a lump, and the target stops casting for it.**
Vanilla's "+400% regeneration for 20 s" multiplies *whatever the target has*, so it is
worth five times as much to a target that is not casting — which is the whole point
of the spell and the reason a real Druid says "stop casting". The effect is
`Innervate of duration * regenBp` on the target (`Content.innervate` passes
`40000` = five times), the multiplication happens in `advanceResource`, and
`PartyAi` suppresses `UseAbility` orders for any member carrying the aura while every
party member is above 60% health — resuming the moment somebody needs a heal. It goes
to **the driest healer first** (the party's mana is the healer's mana: a dry Mage
costs a slow pull, a dry healer costs the pull), and only falls back to a damage
caster when no healer is under 60%.

**A damage caster plays for damage a second until 15%, then for damage a point.**
`rangedOrders` already ranked by damage-per-cast and switched to damage-per-mana on a
threshold; that threshold is now the 15% the class actually plays to, and below a
twentieth of the bar the caster stops casting altogether — no spell it can afford
leaves anything for the next pack, and the only way back up is the five-second rule,
which asks for exactly that. A healer never stops. **The wand is not modelled**: there
is no item-level ranged damage in the content, so a conserving caster idles where a
real one would auto-shoot, and no rank ladder exists either — every ability has one
rank, so "cast the cheaper rank" has nothing to pick from. Both are content gaps, not
AI gaps; the free-shot branch in `rangedOrders` is already written and will fire the
day an ability with `ResourceCost = 0` exists for a caster.

**Mana Tide is untrained at every dungeon level, so the Shaman has no mana reserve
in the only fights that are measured.** The totem ladder trains Stoneskin at 16,
Healing Stream at 20, Mana Spring at 26, Mana Tide at 40 and Grace of Air at 42
(`Content.fs:3886-3890`), while the three wings sit at 33/36/39. So the slot rule
above can never fire in a graveyard, library or armory run: the Shaman's water slot
holds Mana Spring and nothing else, and the "hold the tide for a healer's dry pull"
behaviour is only observable on a level-60 party. Innervate has no level gate, so the
Druid does have its reserve in the band — which is part of why the Druid and the
Shaman are not interchangeable healers at these levels even when their numbers match.

**The leader waits for the whole party to drink, and a shuffle is not a drink.**
`PartyAi`'s between-pull gate used to look only at healers under 60% mana, so a Mage
at a quarter bar pulled anyway, and the `waiting` flag doubled as the leash flag. Now
`drinking` is its own flag — **every living mana user under 80%**, Rage and Energy
excluded on purpose (a Warrior's bar is empty by design out of combat, so waiting on
it would park the party forever, and the dead never refill) — and `waiting` is
`drinking || out of leash`. The two flags are not the same for one reason: the Tank's
`Stop` is enough to end the pull, but the *followers*' step-aside shuffle also hung
off `waiting`, so a party drinking beside the first pack would shuffle into it and
open the pull with the healer dry. The shuffle is now gated on `waiting && not
drinking`: while the party is only drinking it stands still, and members out of leash
still close up. Measured both ways in
the `` `the leader waits for every mana user to drink` `` test — a dry Mage holds the
Tank and sends nobody away from it, a topped-up one does not.

**Gear above 24 is one curve between two anchors, not a ladder of tiers.** The
researched band tables were replaced by `Sheet.Gear60`: the level-60 **Tier 1 / MC +
Onyxia** outfit per class (see `levelscale/raid-gear-60.md`), every stat grown from
its level-24 value along
`at24 * (at60/at24)^((level-24)/36)`. Thirty-six is the span between the anchors, so
60 lands exactly on the research and the log line between is smooth. **Nothing below
24 scales:** `t = float (max 0 (level - 24)) / 36.0` clamps at zero, so a level-17
party wears the level-24 kit exactly. Every built instance is at or above 24 today,
which is why this has never shown up — but a level-17 measurement would be a
level-24 measurement wearing a different label. The level-24
value is a floor per stat, because the caster outfits *drop* Strength and Agility
for Intellect. Two consequences worth keeping: the totals at 60 are what
`raid-gear-60.md` measures (a gear-only subtotal already includes the Agility armour and
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

**A route longer than the search budget read as "unreachable".** `Path.search`
expands 1200 nodes before it gives up, and the best-effort path it hands back points
at `closest` — which is still the *start* tile when the first steps legitimately walk
away from the goal (a room whose only exit is behind you). Truncation therefore
returned an **empty** path, every caller read "unreachable", and the Party parked at
`goal=(28,187) path=0 stall=7` with fourteen reachable mobs and thirteen minutes of
clock left. `search` now retries once at a budget of `width * height` when the fast
budget truncates *and* left nothing to walk; a reachable goal is then always found and
an unreachable one still exhausts its component. Measured on `graveyard max` seed 1:
943 escalations, 13 653 612 escalated nodes, ~14 478 average. That is ~38 s of CPU for
a whole dungeon run instead of a permanent stall — bounded, but it is why the matrix
is slow, and the escalation only fires after the fast search has already failed.

**Taunt is free, because rage made its price fatal.** The design table priced Taunt at
10 rage; under the old mana pool that was invisible. Under rage it is a fifth of the
Tank's whole bar per taunt, competing with Heroic Strike and Shield Block, and a Tank
that cannot afford to taunt is a wipe rather than a tuning question. Vanilla's Taunt
costs nothing for exactly that reason, so `Content.fs:234` now charges 0 (the
8-tile range stays — that is what a party of this level pulls with). Charge keeps
its negative cost, which is vanilla's rage *income*.

**Two routing faults parked the Party with half the wing alive.** Both were found by
the composition matrix below, where 26 of 72 wing-rows ended `unresolved` — a third
of all runs were not fights at all:

1. *A settled ally was treated as a wall.* `claimedByOther` exists to stop two bodies
   **stopping** on one tile; transit is legal by design and `SimTick.advanceMovement`
   separates a cross-over. The Tank's route planner blocked on it anyway, so a
   one-wide corridor sealed behind the Party: `at=(26,186) goal=(36,175) path=0
   dest=None stall=4` with 18 mobs reachable and 12 tiles of open floor ahead. The
   fallback now retries the route with the Party stepped *through* — mobs and walls
   still block.
2. *A mob in a doorway did not truncate the aim.* The "walk up to the door" fallback
   cut the route at the first **ally**, then fell through to the mob's own tile, whose
   search returns nothing: `MoveTo` repeated on an unreachable goal forever. It now
   stops at the first settled mob as well, and the guard wakes within the pull radius.

Measured on seed 1 at each wing's median level with the default roster, before and
after: library L36 stalled at the 26000-tick cap with 15 mobs left and now clears in
10332 with none; armory L39 likewise now clears in 9732.

*A third fault followed, and it was the advance itself.* The Tank aimed at the nearest
hostile **by straight-line distance, with no line of sight.** In a wing that is rooms
behind walls, that is a mob on the far side of one, and the route to it never exists.
`graveyard` seed 12 is the reproduction: five members at full health and mana, 36 mobs
alive, `engaged=0`, and `dest` alternating between two tiles for sixteen thousand ticks
while the goal sat eight tiles away behind a wall. The aim is now sight-first — what the
Tank can see is what it may pull, and only when it can see nothing at all does distance
decide. With that and the pull-radius fix below, at a 20000-tick cap: **graveyard clears
11 of 12 seeds, library 10 of 12, armory 12 of 12** — 33 of 36, against 3, 12 and 12
before. Graveyard's median clear is 11410 ticks and its two remaining wipe-side seeds
finish 4-up.

Three seeds still park, all of one shape: the leftover cluster sits in a room the party
has no sight of (`library` seeds 6 and 7, `graveyard` seed 4 — goal thirteen tiles away
through a wall, 98 steps of open floor between them, `los=false` on every remaining
mob). Ranking unseen mobs by walk distance was tried and measured *worse* (graveyard fell
to 8 of 12) because `Path.astar` returns `Truncated` best-effort prefixes whose lengths
are not distances. The residual is unresolved, and it is the next pathing task.

**The pull radius and the pack spacing are one number, not two.** `aggroRadius` was 6
(`SimState.fs`) against `packSpacing = 7` (`Content.fs`), so every pack stood inside the
notice radius of the next and a fight lasting longer than the gap dragged its neighbour
in: graveyard measured **14 and 17 simultaneously engaged hostiles** against packs of 3
to 4, while the pull itself never exceeded 3–5. `aggroRadius` is now **2**, a quarter of
the spacing — a pack still wakes as a unit, because pack members are within two tiles of
each other by construction, and the pack behind it sleeps. Chain pulling fell out of the
number rather than out of the AI. Raising either constant without the other is what
causes it; the ratio is the invariant.

**Damage is a pull.** `SimTick.fs` woke a mob only by proximity inside `aggroRadius`
with line of sight; taking a hit did not wake it. That made every ranged opener in the
game a free shot at a mob that went on standing there — a Mage's Fireball, a Hunter's
Arcane Shot, a Warrior's crossbow — and it is why the Party had to walk into a room to
start any fight. The damage path in `SimCombat.fs` now sets `Engaged` on a hostile it
hits. Clears are unchanged at 33 of 36 and every wing is faster (armory 8518 → 8310,
library 9838 → 9310, graveyard 11410 → 11096), because fights begin when they are
announced instead of when the Tank finally stands next to something.

**The Tank's crossbow (`Shoot`) is in the kit and not in the rotation, on measurement.**
Range 6, flat 1 damage, 6 s cooldown — a Warrior's thrown-weapon pull, expressed with a
crossbow because the gear pool defines no `Thrown` slot. Used as an opener it made
`graveyard` **worse**: 11 of 12 seeds cleared without it, 7 with it, and 9 with Charge
alongside. The reason is in the advance logic, not the damage: the Tank only walks when
it is out of melee, so a fight opened at six tiles is a fight the Party fights spread
out, and the wait gate then reads the spread as stragglers. A ranged pull earns its
place once the Tank *withdraws to chosen ground* after the shot with the pack following;
without that step it is just a longer walk for the mobs. The ability stays because the
class should have the tool, and the branch that used it is removed rather than disabled.

**Charge and Intercept are opposites, and are now modelled as such.** Charge has a
*negative* rage cost — 9 at rank 1, 12 at rank 2 — because it generates rage, and it is
refused outright the moment anything hostile is engaged. That gate lives in the command
path (`SimCombat.fs`, `UseAbility`), not in the AI, because the engine is what decides.
Its in-combat counterpart is **Intercept** (level 20, ten rage, eight tiles, three-second
stun, thirty-second cooldown), which shares the leap effect and costs what Charge pays.
Both are in the Warrior's kit; neither is in its rotation, on measurement: Charge as an
opener reads 9 of 12 in `graveyard` and **5 of 12** in `library` against 11 and 10
without it, and Intercept alone reads 7 of 12 in `library` against 10. The leaps are not
the problem — the geometry they create is. A Tank that arrives eight tiles ahead of its
healers is a Tank fighting alone; the straggler gate then sees a party that must be
waited for, and the pull dies of geometry rather than damage. A gap closer earns its
rage the day the party moves with the Tank, and that is a party-following change, not a
button. Requiring the whole Party within four tiles before the leap — the closest thing
this AI has to running in together — still reads 8 of 12 in `graveyard` against 11: an
eight-tile leap puts the Tank where its healers are not, and no amount of communication
closes distance. The engine also refuses a leap with no line of sight to its target now,
which is vanilla's own rule and the reason a Charge through a wall cannot happen.

**Interrupts are assigned, not raced.** A cast that has two members reaching for it is a
wasted cooldown: a Rogue's Kick is back in ten seconds and a Mage's Counterspell in
twenty-four, so double-covering a cast costs the Party the expensive one. `PartyAi`
computes one assignment per Tick — each hostile in the middle of an interruptible cast
maps to exactly one interrupter, chosen by cheapest kick that can physically land, then
nearest, then lowest id — and a member acts only on the casts it was assigned. The
assignment is derived from the `World` rather than stored, which is what makes it
*communication* rather than bookkeeping: every member reads the same state and reaches
the same conclusion on the same Tick, so nothing has to be passed and nothing drifts out
of date. This replaced a partial rule that left kicks to whoever stood closest and
silently dropped a ranged interrupt when *any* melee member was in range, whether or not
that member was assigned the cast. The unit test pins the behaviour: one caster mid-spell
with a Rogue and a Mage both able to interrupt produces exactly one `Kick` and no
`Counterspell`. The wings barely exercise it — their packs are mostly melee, which is why
the measured effect is in ticks rather than clears — but the guarantee holds for the
caster-heavy wings still to be checked. The benchmark snapshot moved when the rule
landed and its mean did not: 880 ticks over 30 seeds before, 886 after, with mean party
health 4853 → 4865. So the correct claim for this change is *cooldowns are no longer
wasted*, not *the party kills faster* — the bench proves the difference is within noise,
and quoting the one seed that improved (936 → 779 ticks) would have been cherry-picking.

**The wait gate can park a run, and this is the mechanism.** `waiting` is true when any
member is further than `leash` (8) from the Tank — and `waiting` empties `hostiles`, so
no member may act. A member stranded behind a wall with no route back therefore parks
the entire run: `PartyAi.decide` returns **zero orders** for twenty thousand ticks with
five members alive, ten thousand hostile health on the floor and fourteen mobs left
(`graveyard` seed 1, reproducible with a log grep for `orders=0`). Waiting for a pull is
worth two seconds; waiting for a member that is lost is a hang. The candidate fix — wait
only for members that are *in sight* of the Tank as well as behind — was measured only in
combination with the crossbow (graveyard 6 of 12) and needs its own run before it ships.

**The Warrior pays in rage, not mana.** `ResourceKind = Rage` with a flat 100
(`Content.fs:3479` sheet, `:3510` the `heroStats` argument). His perks were *already*
priced in rage (Taunt 10, Heroic Strike 15, Sunder Armor 15, Shield Bash 10, Shield
Block 10, Disarm 20, Execute 15) while the pool underneath them was mana that
regenerated, so the class was drawing roughly twice a Warrior's real income. Income
now comes from `SimCombat.gainRage` — `2.5 * damage / level` of damage dealt (swing,
weapon ability, spell damage) and of damage taken, which is 10 rage for a 100-damage
hit at 24 and 12 for a 300-damage hit at 60. Out of combat rage **decays** at 2/s
instead of refilling (`SimTick.advanceOutOfCombatRegen`), so he walks into a pull with
whatever the last one left him. Measured on `graveyard max` seed 1
(Warrior/Shaman/Mage/Rogue/Warlock): cleared at 15456 with a mana pool, wiped at 20680
with rage; seeds 1-4 under rage are one clear and three tick-caps. That delta is the
class working as designed rather than a bug — `gainRage`'s rate is vanilla's — so the
Warrior's costs are the balance pass's call, not something to "fix" by widening the
pool. Damage paid *into* him from a periodic aura tick carries no actor and so earns
nothing (`dealDamage` from `SimTick`'s burn phase).

**Two of the tank's buttons were dead.** Shield Block and Shield Wall are
`TargetKind = Self`, and `PartyAi`'s mitigation branch ordered them at `f.Id` — the
foe — so every order was refused at `SimCombat.fs:1055` ("Shield Block invalid or out
of range"), one log line per Tick, and the Warrior never mitigated a hit in his life.
The branch now resolves the kind (`isSelf`) and sends the ability at himself; both
fire ("braces behind the shield", "raises a shield"). Worth remembering that this
bug was invisible in the balance numbers because a refused order still consumed the
AI's turn: fixing it moved every Warrior comp's timings.

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

### Rogue: five poisons and a stealth opener — landed

Five new `Aura` cases (`Domain.fs`, kinds 18-21) carry the poisons, with readers
`Aura.poisonTick`, `Aura.healingReceivedReductionBp`, `Aura.castTimeIncreaseBp`,
`Aura.movementSlowBp`, `Aura.stacksOf`. A poison is **not an `Ability`**: no cast, no
cost, no cooldown, nothing in the kit. `SimCombat.carriesPoison` says who has one (a
party member on Energy), `SimCombat.poisonPlan` re-picks **both blades** per landing
swing, and `SimCombat.poisonHit` rolls one blade at `poisonProcBp = 2000`. Two weapons
mean two poison slots, so the plan always returns two *different* poisons: the
situation's choice on the main hand, and for the off hand the best one the main hand
is not already using. The reads are where the
mechanics already live, not new plumbing: `healEntity` and `advanceAuraHealing` cut
heals (Wound, -15%/stack to -75%), `startOrResolve` lengthens a cast already in
progress (Mind-numbing), `stepToward` raises the next tile's cost (Crippling), and
`advanceAuraDamage` adds `poisonTick` (Deadly).

The ranking is healer-in-range → Wound, caster → Mind-numbing, under 25% →
Crippling, then Instant, then Deadly; the main hand takes the head of that list and
the off hand the next distinct entry. That tail is what stops the second slot being
wasted: a healer-heavy wing gives Wound **and** Mind-numbing to the same blade pair
(measured: both fire on `Druid of the Fang`), and `Crippling Poison` reaches a
`Deviate Ravager` at low health. Deadly Poison is still the last resort — Instant
almost always outranks it — so treat its numbers as untested rather than tuned.

The off-hand keeps its **own clock**, because a fast off-hand weapon is the point of
dual-wielding: it lands more often for less damage each time. `advanceSwings` tests the
main hand on `SwingTicksLeft` and the off-hand on `w.Tick % offHandTicks`, with
`offHandTicks = attack.Ticks * 3 / 5` — a 1.5s dagger against a 2.6s main hand. The two
clocks are independent, so the off-hand swings on ticks the main hand is not. Measured
over one 1836-tick `dungeonAt 24` run: 16 main-hand swings against 29 off-hand hits.
Using the world tick rather than a new per-entity timer keeps this stateless; the Rogue
is the only dual-wielder in the content, so the global clock costs nothing. The two
poisons ride the same split — `poisonPlan` is called once per swing and `fst` rolls on
the main hand, `snd` on the off hand.

`Ambush` is new content (ranks 18-58, 250% weapon + 70…290, 60 energy) and is
**excluded from the strike rotation on purpose**: left in `mine`, its 250% would win
every `damageOf` comparison and be spent as though stealth were free. It is offered
only by the opener branch in `meleeOrders`, whose test for stealth is "an enemy still
at full health carrying no debuff" — the first blow of a pull, and nothing after.

Two things the request asked for that **were already true** and are asserted rather
than changed: `inPosition` already allows Backstab on any target that is not
targeting the Rogue, and `strike` already sorts Backstab above every other non-finisher.

### A latent movement race, found by the new opener

Adding the opener changed the Rogue's timing and broke `no two living entities share a
tile`. The cause was not the opener: `advanceMovement` built `settled` from bodies with
`Destination.IsNone`, so an arriving body could settle onto one that was **mid-step and
several ticks from leaving**. `separateStacked` cannot repair that in a one-tile
corridor — there is no free neighbour to push to. `held` now includes bodies with
`MoveTicksLeft > ticks 1`, which is exactly "will still be standing here when this tick
ends". Suite 215/215, `bench-30.txt` regenerated.

### Composition ladder after the sight-first tank fix — measured

`sim comps 3` on a pinned build (`/tmp/pin2`, Core sha `fe5101c2…`) walks four rounds of
healer/DPS swaps across the three wings, three seeds per cell, and the harness's own
verdict is **`best single healer: Paladin`**. The 36-cell table, condensed to
clears/parks/wipes per wing:

| round | roster | graveyard L33 | library L36 | armory L39 |
|---|---|---|---|---|
| 1 | Warrior/**Paladin**/Mage/Rogue/Warlock | 3 clear | 3 clear | 3 clear |
| 1 | Warrior/**Druid**/Mage/Rogue/Warlock | 2 clear, 1 park | 2 clear, 1 wipe | 3 clear |
| 1 | Warrior/**Priest**/Mage/Rogue/Warlock | 2 clear, 1 wipe | 1 clear, 2 parks | 3 clear |
| 1 | Warrior/**Shaman**/Mage/Rogue/Warlock | **0 clear, 3 parks** | 1 clear, 2 parks | 3 clear |
| 2 | Paladin/**Druid** | 3 clear | 3 clear | 3 clear |
| 2 | Paladin/**Priest** | 3 clear | 2 clear, 1 park | 3 clear |
| 2 | Paladin/**Shaman** | 2 clear, 1 park | **0 clear, 3 parks** | 3 clear |
| 3 | Paladin + Hunter (no Rogue) | **0 clear, 2 wipes** | 1 clear, 2 parks | 3 clear |
| 4 | Paladin/Mage/**Rogue**/Warlock | 3 clear | 3 clear | 3 clear |
| 4 | Paladin/Mage/**Hunter**/Warlock | 2 clear, 1 wipe | 2 clear, 1 park | 3 clear |

Read it as three facts, not one ranking:

- **The tank-LoS deadlock is gone.** No row shows the old signature — a Warrior dead with
  both healers idle at full mana. Every wipe cell has the healer at low mana (16-39% in
  round 3) or dead alongside, i.e. the party ran out of throughput, not out of a line.
  A 32-run forensics sweep (`Warrior/<healer>/Mage/Rogue/Warlock`, graveyard and
  `dungeonAt 24`, seeds 1-8, `withRosterAt` at each wing's own level) recorded **13 healer
  deaths and all 13 happened after the Warrior was already dead** — never the reverse,
  which is what the bug looked like.
- **`unresolved` is now the dominant failure, not wipes.** 12 of 36 cells park. They
  report all five alive at 24000-32000 ticks with the Warrior at 0% resource. This is the
  unreachable-mob park, unchanged and now top of the list by frequency.
- **Shaman is over-represented in the parks** — 3/3 in graveyard solo and 3/3 in library
  as a second healer, 8 of the 12 parked cells. Priest is next (3 cells). Paladin parks
  once in 12 cells, Druid once. That is a strong enough skew to suspect something
  Shaman-specific (the `leaderless` walk, a totem, or Reincarnation's bookkeeping) rather
  than map geometry alone.
- **Armory L39 clears 12 of 12 cells with every roster tried**, including the Hunter ones
  that wipe in the other two wings. Either the wing is undertuned or its geometry is too
  kind; it is not currently measuring anything.
- Rogue over Hunter is consistent: every Hunter cell has a wipe or a park, and the Rogue
  cells beside Paladin are 9/9 clean.

A second sweep seated each party at **its wing's own level** — the ladder's `comps` seats
at content level, but a probe that seats L24 inside the L33 graveyard wipes 13 of 16
cells and tells you nothing. Level-seated, `Warrior/<healer>/Mage/Rogue/Warlock`, seeds
1-8, two wings:

| wing | Priest | Druid | Paladin | Shaman |
|---|---|---|---|---|
| `dungeonAt 24` (L24) | 8/8 clear | 8/8 clear | 8/8 clear | 8/8 clear |
| graveyard (L33) | 7/8 clear, 1 wipe | 6/8 clear, 1 wipe, 1 park | **8/8 clear** | **1/8 clear, 7 parks** |

`dungeonAt 24` is clean for every healer across 32 runs — not one tank death, not one
healer death. In the graveyard, **2 healer deaths in 64 runs, both after the tank was
already dead, and zero of the `los-deadlock` class**. The fix holds.

**The park is a standoff at a room boundary, and it outranks class balance.** Graveyard
seed 1 with a Shaman, sampled every 2000 ticks from t=6000: `inCombat=false`, twelve
hostiles alive, `engaged=0`, the nearest at chebyshev **10** with `lineOfSight=false`,
`canReach 8=false`, and `Path.astar` from the Tank's tile returning **length 1** — no
route. Mob total HP is frozen at 28911 from t=8000 to 24000: nobody is fighting, nobody
is hurt, and the party stands still (`dest=-`, `stall=0`). Nobody is drinking (Shaman,
Mage and Warlock are at 100% mana; the Warrior's 0% rage and the Rogue's energy are
excluded by design) and nobody is beyond the leash (max 7). So `waiting=false`, and
`hostiles` is empty because **not one hostile passes `canReach leash`** — and `hostiles`
is what gates the pull, the pre-pull and the Tank's walk. A pack behind a wall the Tank
cannot path to therefore stops the party permanently. `leaderless` (`PartyAi.fs:1360-1390`)
is exactly the right fallback and is gated on `tank.IsNone`, so it never runs here.

Why Shaman is 7 of 8 of those cells is **not** measured — its throughput is the weakest
(one heal, `Healing Wave`, against every other healer's two or three), so it may simply
survive to reach the room more often, or reach it in a different shape. Do not conclude
"Shaman is broken" from this table alone.

Caveat that matters when quoting any of it: **3 seeds per cell**, so `dead=33%` is one
seed out of three, and a single-cell difference is not a ranking.

### Warrior: the OctoWoW build, six of eighteen landed

The operator's own build (`/talents/warrior/?points=Y--VYFoKQSopQQYB`, Arms 3 /
Protection 48) is in `warriorTalents`, part-landed: six of its eighteen talents are
expressible as data today — Improved Heroic Strike (Heroic Strike 15 → 12 rage),
Improved Taunt (10 → 8 s), Improved Disarm, Defiance (`warriorThreatBp = 60000`, a
separate constant from the Bear's `tankStanceBp`), One-Handed Weapon Specialization
(`DamageDoneBp`) and Improved Shield Wall (duration baked into the row, cooldown via
`CooldownOff`). The other twelve each need a model axis and are listed with it in
`.pi/research/octowow/warrior.md` §4.

**Two things that surfaced on the way in, both worth knowing.** First, *Disarm's base
cooldown was wrong*: the row said 20 s and both tables say 60 s
(`.pi/research/level24/warrior.md:165`, `.pi/research/ranks/warrior.md:130`). Improved
Disarm 2/2 takes twenty seconds off, and twenty off twenty is a zero-cooldown weapon
lockout on every tick — a wrong base number is invisible until a talent is applied to
it. Second, `withTalents` clamps a talent-shortened cooldown at **1.5 s** (and only a
cooldown a talent touched), so no future tree can zero a row; the clamp is not a
balance number, it is the fastest a 1.12 player can press anything.

Measured after the port: bench 30/30 still `EncounterCleared`, every seed moved by
≲10% and mostly gained health; `--level 60` 36/36 clean, the level-scaled axis still
35/36 with the same single library wipe. So the tank's six talents cost no cell —
which is what a data-only port should cost.

### Rogue: the OctoWoW build, six of eighteen, and two costs

The operator's build (`?points=aoYDI-FAroIAQoRCCAB-`, Assassination 17 / Combat 34 /
Subtlety 0) is part-landed, same shape as the Warrior's: Malice 5/5 and Close Quarters
Combat 2/2 as `CritBonusBp = 1000`, Improved Backstab 3/3 as `BackstabCritBonusBp`, and
the other four baked into the rows they edit — Improved Eviscerate (+15% on the finisher's
one-point pair and its step), Ruthlessness (`ComboPoint 1` appended **after** `Finisher`,
the only order that survives `Finisher` zeroing the pool), and Improved Kick
(`Silence 2 s` beside the lockout).

**The two flat costs are the interesting part**, because they are the OctoWoW *baseline*
change rather than a talent: Sinister Strike 45 → 40 energy and Eviscerate 35 → 30, where
the file's own comment claimed "Energy costs do not vary by rank: Sinister Strike is 45".
Five energy off a generator is not cosmetic on a 100-energy pool — it is a whole extra
Sinister Strike per fifteen seconds, which is what moved the bench.

**One test had to change shape, not numbers.** `an interrupt cannot cancel a cast that is
not interruptible` used Kick, and Kick now carries Improved Kick's silence — and a silence
*does* end a cast (`SimCombat.fs` sets `Casting = None`), because that is what a silence is.
The rule the test guards is about `Interrupt`, so it now hands the rogue a Kick with the
silence stripped and asks only its own question. That is the second time a port has exposed
a test asserting more than it claimed.

Measured: bench 30/30 clear, every seed within ~5% either way; `--level 60` 36/36; the
level-scaled axis **36/36 for the first time**, including the library cell that had wiped
2 of 2. That cell is not "fixed by the Rogue" in any proven sense — the whole party's
numbers moved, and this is a one-seed run — but there is no failing cell left to debug.

### Mage: the OctoWoW build, four of nineteen, and the two-seed lie

The operator's build (`?points=TAFOLISYZKAoB--FADD`, Arcane 40 / Fire 0 / Frost 11, every
taken talent at maximum) landed four of nineteen: Improved Frostbolt 5/5 (half a second
off a 3 s cast), Arcane Impact 3/3 (`CritBonus 600` on Arcane Missiles and Arcane
Explosion, and on nothing else — this build takes no Fire talents), Improved Counterspell
2/2 (`Silence 4 s` baked into the row, because a talent record edits numbers and this adds
an `Effect`), and Presence of Mind as a kit row built from the same `nextSpell` helper as
Nature's Swiftness: no talent record can hand over an ability the kit did not have.

The remaining fifteen are exactly the ones that would change the class: Arcane Instability
(+3% damage *and* crit to everything), Arcane Power (a casting-speed window), Arcane
Potency, the Improved Arcane Missiles line. That is not a porting oversight, it is §5(b) —
none of them has a field to land in.

**Baseline changes the note lists and this did not apply**, because the numbers are for
ranks the level-24 anchor never reaches: Fireball cheaper at rank 11 (395 → 275) and 12
(410 → 295), Arcane Intellect rank 3-5 (520 → 240, 945 → 340, 1510 → 445). A curve through
two top-rank points is a guess, and a guessed number is worse than a vanilla one that is
at least checkable.

**The measurement is the interesting part, and it is a repeat offence.** The scaled ladder
was reported green (36/36) after the Rogue port. It is not: at six seeds it is **32/36**,
four library cells each losing at least one seed, three of the four carrying a Mage. The
earlier number came from a Harness binary that `dotnet build` had declined to recompile —
the first A/B I ran to check the Mage port gave "identical output" for a reason that had
nothing to do with the Mage. The fix is `-t:Rebuild` for any A/B and a DLL hash printed
next to the numbers; and the lesson, in the same shape as the stale-DLL probe earlier in
this session, is that the benchmark's *denominator* is as easy to get wrong as its result.

Measured on a verified-current DLL: bench 30/30 clear with 18 seeds moved (all within 7%,
both directions); `--level 60` **36/36 clean at six seeds**; level-scaled **32/36 at six
seeds**, and the same single library cell fails at two seeds both before and after the
port (median 9666 either way), so the port neither caused nor fixed that one.

### Priest: eight of sixteen, and the first port where deleting was the work

Discipline 11 / Holy 40 / Shadow 0 — the operator's build — is the best hit rate of the
port so far, **eight of sixteen talents**, because a healing build spends its points on
numbers this model already has: Spiritual Healing 5/5 (`HealDoneBp` 1000 → 3000, the
largest single line in any port), Spiritual Guidance and Inspiration unchanged, Improved
Healing 3/3, Improved Renew 3/3, Mental Agility 5/5, Divine Fury 5/5.

Two things **left** the record, and this is the pattern the notes predicted: a port that
only adds leaves the old tree live. Meditation 3/3 is gone (`RegenBp` 1500 → 0), and Holy
Specialization is gone too — the old comment credited it with +5% crit that its record
never carried, and OctoWoW renames it Divinity with no numbers at all.

Two traps caught in the writing, both worth knowing before the next class:

- **`Talents.spell` is a `List.tryFind`, so a second `Spells` line for the same spell is
  silently dead.** Improved Renew and Mental Agility both land on Renew; they share one
  line (`CostBp 1000` beside `HealBp 1500`) because two lines would have looked correct and
  done nothing.
- **The sim runs twenty ticks a second, so a duration written as a bare `ticks N` is
  usually wrong by a factor of two.** Divine Fury is half a second: `secTicks 0.5` (ten
  ticks), not `ticks 5` (a quarter second). The first version of this port had it at
  `ticks 5` and the test written from the *absolute* numbers is what caught it — the
  assertion compared against the same wrong constant it would otherwise have agreed with.
  `priest.md` §5 now says so explicitly, because the next note may inherit the error.

Measured on a rebuilt Harness: bench **30/30** clear; `--level 60` **36/36 cells at six
seeds**; level-scaled **33 of 36 cells fully clean at six seeds**, the three that lose
seeds all library (`Warrior/Druid/Mage/Rogue/Warlock` 3/6, and two 5/6). Both Shaman cells
that failed before this port are now clean, so the scaled axis went from four failing cells
to three — the healer's numbers helped the rosters carrying a Shaman, which is the kind of
coupling that makes a single-class diff hard to credit.

### Paladin: seven of seventeen, one deletion, and a nerf kept on purpose

Holy 40 / Protection 0 / Retribution 11 — the operator's build — lands **seven of
seventeen talents**, and two of the seven were already in the record, so the diff is five
lines and one deletion. The Paladin is the first class where the port *lowered* a number:

- **Illumination 5/5 is `ManaRefundOnCritBp` 10000 → 6000.** The old record refunded the
  whole cost of a crit heal; payload refunds sixty per cent. The test that used to assert
  "mana back to 100" now asserts "100 − cost + 60% of cost", which is the shape of every
  talent-nerf assertion after this one.
- **Divine Favor's kit row is deleted.** The sim shipped a 2-minute cooldown that made the
  next heal a guaranteed crit; OctoWoW's Divine Favor is a passive Holy Shock crit talent
  wearing the same name. Adding the passive without deleting the cooldown would have left
  two Divine Favors live — the same trap the Priest port hit with Meditation. `SureCrit`
  is now set only by Inner Focus, and the three `Domain.fs`/`SimCombat.fs` comments that
  listed "Nature's Swiftness, Divine Favor, Inner Focus" as one shape were corrected rather
  than left to mislead.
- **Comment and record had already disagreed.** The old `paladinTalents` comment claimed
  "Holy Power 5/5 (+5% crit)" while the record set no `CritBp` at all. Before porting a
  class, diff its own comment against its own record: this one was wrong for months.

The nine unlanded talents are the trigger-shaped ones — Holy Shock's either-target cast,
Daybreak's reactive heal, Blessed Strikes' reset proc, Ironclad's armour-to-healing,
Blessing of Kings' *multiplicative* party buff (every `buffed` branch today adds a flat
number). Seals stay unmodelled, so Judgement is still a plain Holy nuke.

Two traps in the tests, both from content that is level-gated:

- **`Content.gully 1UL` gives the Paladin no Holy Light.** It is a level-22 ability, so the
  "build lands on his kit" test had nothing to land three of its five rows on and threw
  `KeyNotFoundException`. Level-24 content (`Content.partyOf [ ... ] 24`) is what that test
  needs, and any future per-class kit test should start there.
- **`Talents.healBp` returns the multiplier including the 10000 baseline.** 11200, not 1200.

Measured on a rebuilt Harness: bench **30/30** clear and the snapshot **did not move** —
which is itself informative, since the bench roster is `gully` at L24 where the Paladin's
crit heal is a rounding error. `comps 6 --level 60` is **36 of 36 cells fully clean at six
seeds**, identical to the Priest port's record, so the Illumination nerf did not cost a
single cell at the level the balance question is asked at. Do not read that as "the nerf
is free": the axis that would show it is one where a Paladin runs out of mana, and no cell
came close (the Paladin seat ends every arm above 60% mana).

## 9. Open work, in priority order
0. **The balance-and-story programme: decided with the owner, under way.** It
   governs items 3 to 7 below rather than replacing them.
   - **Status.** Done: the raid-gear anchor (twice cross-checked), the healer and damage
     ladders at all three target levels (§"Re-measured after the sight-first advance",
     §"The damage ladder"), the two invisible healer utilities, the tank-wait mana gate,
     and the interrupt assignment. Not done: **per-ability attribution** (below), the
     **three parks** that void the library cells, the one-healer question, the Druid's
     L33 triage number, and everything in the story/log bullets. The next three blocks
     are the whole of what remains for this item.
   - **The story is parametric and display-only.** Per-dungeon ink drawn from a
     shared cast and a shared quest pool, with bespoke prose per dungeon; the
     narrative never changes gameplay, so the four choice points that reconverge
     and change nothing are legitimate as they stand. It fires **between pulls
     only** — `start_encounter` / `complete_quest` at a pull boundary, never
     mid-fight — which is why the tick loop needs no suspension state.
   - **Nothing drives the ink from the game yet.** Only the harness plays it
     (`Program.fs:219`, `mise run story`); `WowRoguelike.Game.fsproj` references Core
     and nothing else, so `Encounter`/`QuestComplete` are printed and never handed to
     the sim. The renderer is the harness first, behind one shared filter helper in
     Core so the TUI can adopt it without rework.
   - **The log gets combat / dialogue / both tabs**, and dialogue appears in it (and
     as bubbles). `World.Log` is a bare `string list` (`Domain.fs:1183`) with no tick
     and no kind, so a structured event has to exist before anything can be filtered.
   - **Balance is measured per dungeon at that dungeon's own level** — the min/med/max
     anchors in item 3, not a single bracket — and the question asked of each class is
     *what sets it apart* and *what holds it back*, never "is it good".
   - **The answer is instrumented, not inferred.** A run reports wipes, clear time,
     outcome, mana left **and per-ability attribution** (casts, healing and damage by
     source, unhealable deaths). The attribution is a `SimCombat` change and it lands
     on every report test.
   - **The gear anchor is raid gear, and now verified twice.** `Sheet.Gear60` encodes
     the level-60 **Tier 1 / Molten Core** outfit per class
     (`.pi/research/levelscale/raid-gear-60.md`, nine tuples in `Content.fs`), scaled to
     each target level by the existing two-anchor curve. An independent second crawl
     of the nine class T1 sets reproduced **all nine tuples exactly** — armour and every
     stat — so the anchor is cross-checked rather than asserted by one pass. The
     pre-raid figures it replaced were thinner than the raid outfit — the
     largest gaps were armour (Paladin 3162 → 8223, Shaman 787 → 6079) — so the curve
     below 60 is now steeper than when the bands were first measured.
   - **Cells measured first: graveyard, library, armory.** Wailing Caverns and
     cathedral follow. Note §12: Deadmines and Stockades have extracted art and no
     content, so they cannot be a target until something is built on them.
   - **Every healer must be able to succeed**, each on a distinct role measured the
     same way. That requires the AI to reach the utilities it currently cannot see
     (§8, "Two healer utilities are invisible to the AI").
   - **First full matrix, measured at the target levels (`sim comps 10`, four rounds,
     36 cells).** Nobody clears reliably and the failure modes split by role, which is
     the finding to tune against:

     | healer | graveyard L33 | library L36 | armory L39 |
     |---|---|---|---|
     | Priest | 0 clear, 9 wipe, 0.5 alive | 3 clear, 6 wipe | 0 clear, 10 wipe |
     | Druid | 0 clear, 10 wipe | 0 clear, 10 wipe (stall to cap) | 2 clear, 8 wipe |
     | Paladin | 0 clear, 10 wipe | 0 clear, 5 wipe **5 unresolved** | 4 clear, 6 wipe |
     | Shaman | 0 clear, 2 wipe **8 unresolved, 4.0 alive** | **7 clear** | 2 clear, 8 wipe |

     Two healers fail by **dying** (Priest, Druid) and two by **not killing fast
     enough** (Shaman, Paladin — left alive at the tick cap with mana to spare). A
     second healer converts death into stalls: Shaman+Priest goes 4 clear / 0 wipe /
     **6 unresolved with 5.0 alive** on graveyard, and Shaman+Druid is the best cell in
     the matrix (armory 9 clear / 1 wipe, alive 4.5). So survivability is buyable and
     **damage is the wall** — the same verdict as §8's `cooldownFor` gap, from the other
     end. A healer that does no damage (Priest, Paladin) turns a winnable fight into a
     run that never closes.
   - **The Hunter is now a real DPS class, at the Rogue's expense and not in every
     room.** Rogue → Hunter beside Shaman+Mage+Warlock, 10 seeds: armory **2 → 8**
     clears, library 7 → 3, graveyard 0 → 1 clear but wipes 2 → 6 and alive 4.0 →
     1.9. The ranged sustained kit pays where the pull is open (armory) and the loss of
     Sap/Kick/Backstab control costs where the trash is dense (graveyard, library).
     Total clears across the three dungeons: Hunter 12, Rogue 9, Rogue+Hunter together
     9, Hunter+Rogue+Warlock 10 — close enough that the honest statement is *different
     rooms*, not *better class*.

   - **Re-measured after rage, free Taunt and the two aim faults (`sim comps 3`, 36
     cells).** Same split, sharper: library and armory stop stalling, graveyard keeps a
     stall of its own. Rows give `cleared/wiped/unresolved`; each row is three seeds.

     | healer (Mage/Rogue/Warlock DPS) | graveyard L33 | library L36 | armory L39 |
     |---|---|---|---|
     | Priest | 0/2/1, 1.7 alive | 2/0/1 | 1/2/0, ends 4% mana |
     | Druid | 1/2/0, 0.7 alive | 0/2/1, **wipes by tick 1764** | 2/1/0 |
     | Paladin | 1/1/1 | 0/**3**/0, 0.0 alive | 1/2/0, ends 73% mana |
     | Shaman | 1/0/2 | 2/0/1 | 1/2/0 |

     Wing totals over all 12 rosters: graveyard 11 clear / 6 wipe / **19 unresolved**,
     library 19/8/9, armory 21/15/**0**. Armory is the only wing that is a clean
     balance signal, and there it is survival: every healer ends at 4-10% mana. The
     four healers fail in four different ways — Shaman only to stalls, Priest to mana at
     armory, Druid to an early library death, Paladin to throughput (it dies with ~70%
     of its pool unspent, so its heals are too slow or too small, not too expensive).
     "Every healer can succeed" is therefore **not met**, and Paladin is the one that
     needs a number changed rather than a utility reached.

     Second healer beside Shaman still converts death into stalling rather than into
     clears on graveyard (Shaman+Priest 2 unresolved, Shaman+Paladin 3), so the earlier
     verdict stands: survivability is buyable, damage and routing are the walls. The two
     aim faults (§8) are what took library and armory to 0 unresolved; they only touch
     *which tile* the Tank aims at, so `stepToward`'s hard-first-step invariant from
     item 1 below is untouched.

   - **Re-measured after the between-pull drinking gate, Innervate and Mana Tide
     (`sim comps 3`, same 36 cells).** The gate converts stalls into fights; on its
     own the party lost them, because mana was drained going in.

     | wing | before (rage + aim faults) | drinking gate | + five-second rule, wand discipline, Innervate |
     |---|---|---|---|
     | graveyard L33 | 11 clear / 6 wipe / **19 unresolved** | 3 / 24 / 9 | **4 / 16 / 16** |
     | library L36 | 19 / 8 / 9 | 10 / 14 / 12 | **23 / 5 / 8** |
     | armory L39 | 21 / 15 / 0 | 22 / 14 / 1 | **32 / 4 / 0** |

     The middle column is the load-bearing negative result: a party that arrives with
     full mana and no restoration discipline dies where it used to park, which says the
     old 19 graveyard "unresolved" rows were not latent wins. The third column is what
     fixes it, and it does so by **spending** the mana rather than hoarding it —
     casters play for damage a second down to 15%, stop at 5% so the five-second rule
     can refill them, the leader drinks the whole party to 80% between pulls, and
     Innervate turns a Spirit-heavy Priest's bar around five times faster than it
     would fill unaided. Armory goes from one unresolved cell to zero and its clear
     rate from 58% to 89%, so the ordering of the wings is now
     **armory easiest, library middle, graveyard hardest** — which is the reverse of
     their level order and is a direct consequence of the graveyard still stalling.

     16 graveyard and 8 library seed-runs still end `unresolved` with the party alive,
     so the third stall from item 1 below is still in there and still uninvestigated.
     **Do not tune damage or healing numbers against graveyard or library until it is
     fixed**; armory is the only wing whose cells mean what they say.

   - **Re-measured after the sight-first advance, the pull-radius drop and the damage
     pull (`/tmp/ladder.fsx`, 8 seeds per cell, healer seat swapped, control = the
     default roster).** Every cell above predates a Tank that can actually advance, so
     they are history rather than a baseline. The party shape is the default five
     (`Warrior; Priest; Rogue; Mage; Druid`) with only the second seat swapped, which
     makes the Priest row a control: it reproduces the 11-of-12 baseline exactly on the
     seeds it shares.

     | healer in seat 2 | graveyard L33 | library L36 | armory L39 |
     |---|---|---|---|
     | Priest (control) | **7/8**, 11410 ticks, 3 deaths | 6/8, 9950 | **8/8**, 8596 |
     | Paladin | 6/8, 11340, 5 deaths | 4/8, 10133 | 8/8, 9539 |
     | Druid (party has **no** Priest) | 5/8, 15600, **15 deaths, 2 wipes** | 4/8, 9760 | 8/8, 9127 |
     | Shaman | 5/8, 12530, 2 deaths | **7/8** | 8/8, 10204 |

     **At armory every healer clears every seed with zero deaths.** "Every healer can
     succeed" is met on the wing with the most hostile health in it, and the only cost
     separation is pacing: Priest 8596 median ticks, Druid 9127, Paladin 9539, Shaman
     10204 — the Shaman runs 19% longer than the Priest for the same clears.

     **Graveyard separates the classes on triage speed, and the Druid is the class that
     shows it.** Swapping *only* which of the two healers sits in seat 2, with the same
     five classes present (`Warrior, Druid, Rogue, Mage, Priest` against
     `Warrior, Priest, Rogue, Mage, Druid`), moves the result from 7/8 with 3 deaths to
     5/8 with **12 deaths**. Both healers heal in both runs — `healerOrders` runs for
     every healer in the party — so the difference is what the primary seat's class brings.
     **Measured on one pinned build (`sha256 3ed95ab…`) at 8 seeds, the doc's
     7/8-with-3-deaths against 5/8-with-12-deaths pair does not reproduce, and it is not a
     transposition.** Seat 2 Priest: **5 clear / 1 wipe / 2 unresolved, 12 deaths**,
     median 15122 ticks. Seat 2 Druid: **6 / 0 / 2, 12 deaths**, median 16156. Both seats
     produce 12 deaths and the Druid seat clears one seed *more*. Two other rows drifted
     independently — the Shaman row now parks in 8 of 8 seeds (five alive, zero deaths),
     and the no-Priest Druid row went 5/8 with 15 deaths to 1/8 with 18 — while the
     Paladin row barely moved (6/8→7/8, 11340→11270 ticks). Class-unequal drift of that
     shape dates the table to before the uncommitted `PartyAi.fs` healer-order work, not
     to a transcription slip. **Do not quote this table's clear/death counts without
     re-running them.**

     Note also that the row `Warrior;Druid;Rogue;Mage;Druid` doubles a class, which
     `sim comps`/`report` refuses outright (`Program.fs:157`) and which no composition
     test should use: a doubled class loses a whole kit, so it measures the missing class
     rather than the swapped one. Swap one seat in an otherwise fixed five-class party —
     `Warrior;Priest;Rogue;Mage;Druid` against `Warrior;Druid;Rogue;Mage;Priest` — and the
     comparison is honest.
     What the narration below cannot be is a triage-speed story: `Flash Heal` is **not
     ported** (`src/WowRoguelike.Core/Content.fs:1819` defines only `lesserHeal`, 2.5 s),
     so the Priest's fastest direct heal is *slower* than Regrowth's 2.0 s, not faster.
     Attribute any gap to output and utility instead — the larger Renew at this rung, and
     Power Word: Fortitude, which the no-Priest row loses along with everything else.
     Rejuvenation is in the kit and the AI does cast it — `healerOrders`
     sorts every HoT by total healing, refreshes it on lapse, and hands a target under
     60% to the fast heal instead (Swiftmend when a Rejuvenation is up to eat, Nature's
     Swiftness otherwise) — so this is **not** a utility the AI cannot see.

     Two cautions about reading this table. The Druid row with the party at
     `Warrior, Druid, Rogue, Mage, Druid` is the harshest configuration in it — no Priest
     at all, so no Power Word: Fortitude and no Renew — and its 15 deaths are a
     *composition* effect, not a Druid heal effect. And seat position is itself a
     variable: the two rows above seat the Druid at different spawn tiles, so part of the
     death count is exposure rather than throughput. Any class ladder must therefore
     swap exactly one seat, which is what every other row in the table does.

     **Shaman's two deaths against the Priest's three say the utilities are pulling their
     weight** (Mana Tide replacing Mana Spring, Healing Stream in the fire slot, per
     §"Two healer utilities were invisible to the AI"). Its graveyard failures are parks,
     not deaths — `unresolved(3)/5` and `unresolved(14)/5`, three and fourteen mobs left
     with all five alive.

     **Library's columns are not a healer signal and should not be quoted as one.**
     Every failure there is `unresolved(15)/5` — fifteen mobs left with all five members
     alive — which is the no-sight park; Paladin and Druid both "fail" four seeds without
     a single death. The park is what to fix before library cells mean anything, and it is
     now the *only* balance blocker left: at the target levels the wall is routing, not
     healing.

     **The damage ladder, on the same method (`/tmp/dpsladder.fsx`, seat 3 swapped, tank
     and both healers held still, 8 seeds).** Seat 3 is the Rogue's seat in the default
     party, so the `Rogue` row is the control.

     | damage class (seat 3) | graveyard L33 | armory L39 |
     |---|---|---|
     | Rogue (control) | **7/8**, 11410 ticks, 3 deaths | **8/8**, 8596, 0 deaths |
     | Hunter | **8/8**, 11953, no deaths | 8/8, 9274, 0 deaths |
     | Warlock | 5/8, 11900, 6 deaths | 8/8, 8676, 0 deaths |
     | Mage (two Mages) | 6/8, 16438, 10 deaths | **8/8**, 8464, 0 deaths |
     | Warrior (two Warriors) | 3/8, 19250, 7 deaths | 8/8, 8936, 0 deaths |

     **Armory is balanced to within ten percent across all five damage classes** — every
     row clears 8/8 with zero deaths, and the median spans 8464 (Mage) to 9274 (Hunter).
     Nothing there needs a number moved.

     **Graveyard's spread is the real signal, and the Hunter now tops it.** Eight of eight
     with no deaths and no park, against the Rogue control's 7/8 and the double-Warrior
     row's 3/8: the pet is a sixth body that holds aggro and lets the tank pull, which is
     exactly what the wing rewards. The double-Warrior row quantifies what a second tank
     in a damage seat costs — three clears instead of seven, and a 19250-tick median — and
     the two-Mage row quantifies what losing the Rogue costs: 6/8, 10 deaths, 44% slower,
     because two Mages share one Polymorph and both stop moving to cast a 3.5-second
     Fireball.

     **Read the deaths column, not the clear count, for graveyard.** Most of the failed
     seeds in every row but the Hunter's are parks (18 mobs with five alive, 24 with four),
     so the number of wipes is the cleaner damage-class signal: Rogue 3, Hunter 0, Warlock
     6, Mage 10, Warrior 7. The Hunter's `5 - alive` death count is meaningless (the pet is
     a `Party` entity, so the party has six members and the arithmetic goes negative) and
     was read by hand: one seed lost the pet, none lost a member.

     **Who actually dies: the Warrior, alone, and not to aggro.** Deaths logged by
     class across 40 triage rows: Warrior **20**, Mage 11, Rogue 9, Druid 7, Priest 1. In
     `graveyard` L33 the Warrior dies at tick ~1960–2160 in **4 of 4 seeds traced**, before
     anything else on the field, and the rest of the party is at *full* health at that
     instant. The pair of probes that proves it is the mana column:

     | tick | Warrior | Priest | Druid | Warrior↔Priest | Warrior↔Druid |
     |---|---|---|---|---|---|
     | 1200 | 1778/1778 | 2833/2833 | 2524/2524 | 7 | 6 |
     | 1350 | 1629 | 2833/2833 | 2524/2524 | **10** | **9** |
     | 1650 | 570 | 2833/2833 | 2524/2524 | 10 | 9 |
     | 1950 | **40** | 2833/2833 | 2524/2524 | 10 | 9 |

     Both healers hold **full mana and full health** while the tank goes 1778 → 40, and
     they cast no heal at all from t≈1050. So this is not threat taken by a squishy (a DPS
     pulling aggro would show the Mage or Rogue dying with the Warrior healthy — the
     opposite happens), and it is not a class's heal throughput either: the mana was never
     spent. Two candidate causes, both in `PartyAi`, neither confirmed: the healers' target
     selection stops offering the tank once it stands ~10 tiles out, or the party enters a
     non-healing mode (advance/park) that never consults `healerOrders`. Read the code
     before moving a balance number — and note the consequence: `unresolved(24)` rows with
     four alive are *post-tank* parks, so tank sustain and the park bug are one bug, not two.

     **Root-caused: it is a line-of-sight deadlock, not a pull the party failed to follow.**
     The tank does not outrun anyone — `tank move 0` tiles per 10 ticks at every sample of
     the whole run, and the healers move 0 with it. At the death tick in seeds 1 and 2:

     | | Priest | Druid | tank |
     |---|---|---|---|
     | position | (9,180) | (9,179) | (7,172) |
     | distance to tank | 8 | 7 | — |
     | `Path.lineOfSight` | **false** | **false** | — |
     | `SimState.canReach 8` | **false** | **false** | — |
     | `Destination` / `StallTicks` | `None` / 20 | `None` / 20 | — |

     Both healers sit on adjacent tiles in a pocket with a wall between them and the tank.
     `canReach` is `withinRange && lineOfSight` (`SimState.fs:134`), so no heal is legal at
     any distance; `heal` therefore emits a `MoveTo` instead of a cast, `approachTile` finds
     nothing, and `StallTicks = 20` is the `pathRetryBackoff` (1 s) cycle failing over and
     over — `PartyAi.decide` is called every tick, returns fresh `MoveTo`s (verified by
     printing the command list at t=200…1200), and the healers never take a step. Mana stays
     at 2833/2833 and 2524/2524 because **no cast was ever legal**, not because none was
     wanted. Tank HP 1778 → 0 while this loop runs.

     **What the tank was actually fighting when it died: 3–4 mobs.** `targetingSomebody` = 4
     (seed 1) and 2 (seed 2) out of 37 hostiles alive, with only 3–4 within 8 tiles and 8–10
     within 10. The wing's other 33 mobs were simply elsewhere. So the tank was not
     overwhelmed by a mega-pull — a 3-mob group killed it because its two healers were
     walled off. Any difficulty number measured against this AI is measuring the deadlock.

     **[Fixed] The Tank now answers to its healers' sight lines (`PartyAi.holdSpot`).**
     `holdSpot` already chose where the Tank holds a mob, and already withheld tiles that
     blind the *damage* casters — but it asked only about `TargetKind = Foe` abilities, so
     the healers were never consulted. Two additions: `healers` (any Party member with a
     `Heal`/`HealOverTime` effect) and `seenByHealers p`, which requires
     `Path.lineOfSight` from every healer to the candidate tile. When the Tank's own tile
     fails that test it walks to the nearest tile within three that passes — the condition
     self-clears the moment the ray opens, so it cannot oscillate, and the mob follows
     because the Tank holds its threat. `noRoom` (a melee damage dealer with no free tile
     beside the mob, via `SimState.approachTile`) joins the existing `needs` flag for the
     damage side of the same question.

     Measured on `graveyard` L33, default roster, 8 seeds, before → after: **5/8 cleared
     with 12 deaths → 6/8 cleared with 0 deaths**, and the Tank no longer dies at t≈2000
     in any seed. Party HP at 24000 ticks is 5 alive in all 8 (was 4 in the park rows).
     Seeds 3 and 4 still park (`Running`, 9 hostiles, five alive) — that is the *mob*
     unreachable case, a different cause from the Tank's sight line, and it is the one that
     now remains. Suite: 212/212, `bench-30.txt` snapshot regenerated (every seed's trace
     changed, as it must when the Tank's positioning changes).

     **One healer or two? Measured, 48 cells × 3 seeds on one pinned build** (rows
     `Warrior/H/Mage/Rogue/Warlock` and `Warrior/Primary/Second/Mage/Warlock`; graveyard
     L33, library L36, armory L39; clears pooled over a primary's three partners).

     | primary | alone | with any second healer | verdict |
     |---|---|---|---|
     | Priest | 1/3 | 5/9, no wipes | **two** |
     | Druid | 0/3, three wipes | 5/9, no wipes | **two** |
     | Paladin | 2/3, one wipe | 5/9, no wipes | **two** |
     | Shaman | 1/3 | 1/9 — failures become parks | **one** |

     Armory carries no signal (all sixteen rosters 3/3) and library is park-dominated, so
     graveyard alone decides. Two rules fall out of this. A Druid alone wipes every seed at
     L33 — it is the only healer that cannot hold a party by itself, which fits the kit
     (no out-of-combat resurrection, and `Healing Touch` is never chosen). And **a Shaman
     in the second slot is the one partner that drives every primary to 0/3**: pairing a
     Shaman does not add healing, it turns a failing run into a park. Treat "Shaman needs
     help" as the AI's problem, not the pairings'.

     **`sim attrib` is the per-ability instrument** (`sim attrib [wing] [min|median|max]
     [maxTicks]`). Nine runs at 12000 ticks, level 30–40, direct damage and healing only:

     | band | Druid heal | Priest heal | Druid dmg | Priest dmg |
     |---|---|---|---|---|
     | graveyard L30 | 5711 | 2685 | 9498 | 17260 |
     | graveyard L33 | 2936 | 761 | 5512 | 7310 |
     | graveyard L34 | 2893 | 0 | 2652 | 6099 |
     | library L33 | 14378 | 3699 | 14274 | 17931 |
     | library L36 | 7329 | 4846 | 14479 | 20491 |
     | library L37 | 6520 | 2984 | 12095 | 9404 |
     | armory L37 | 9014 | 970 | 20005 | 23765 |
     | armory L39 | 8090 | 2188 | 23184 | 30121 |
     | armory L40 | 4496 | 1493 | 23162 | 28709 |

     **The HoT blind spot is a measurement trap, not a class finding.** `attrib` reads the
     combat log, and an aura's periodic tick emits no line (the harness says so in its own
     comment), so *every* log-derived healing number — this table and the ladder's — omits
     Renew, Rejuvenation, Regrowth's tail and Healing Stream. The Druid's direct share
     dominates partly because Regrowth and Swiftmend are direct; the Priest's whole
     throughput is largely invisible. Do not read the heal columns above as throughput
     until a per-entity `healingDone` accumulator exists (count it where healing is
     applied, HoT ticks included); that is the instrument this comparison actually needs.

     **Graveyard at Maximum difficulty stalls permanently — the one band no recorded
     number covers.** `sim attrib graveyard max` deals its last point of damage at tick
     ~12000 and then does **exactly zero** for the next 28000: Warrior total frozen at
     13520 from `12000/40000` through `40000/40000`. Compare graveyard Minimum, which
     clears at 12320. Since the ladder table above is all Median, this band's failure has
     never appeared in any row. It is the park bug wearing a new roster, and it is the
     strongest reason to fix parks before moving any balance number.

1. **[Fixed] A third graveyard stall.** After the two aim faults were fixed, `graveyard`
   L33 seed 1 with the default roster still parked: `at=(26,186) goal=(36,175) path=0
   dest=None stall=4`, Party at full health, all 18 surviving mobs reachable by a
   wall-only search and the nearest 12 tiles away. Root cause: the Tank aimed at the
   *shortest path* to a hostile rather than at a hostile it could **see**, so it kept
   choosing a mob behind a wall, and `SimState.canReach` faithfully reported no route —
   forever. `PartyAi` now sorts hostiles by line of sight first and path distance
   second; the wings go from 28 of 36 to **33 of 36** and the claim that follows (Charge
   leaves the party behind, §"Movement and pathing") was measured against the fixed AI.
   Three parks remain and none is root-caused: `graveyard` seed 4 (`park(18)/5` — eighteen
   mobs, five members alive), `library` seeds 6 and 7 (`park(15)/5`). They are the only
   thing left contaminating the graveyard and library balance cells.

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
   there — the earlier soft-claim attempt failed because a walker could step onto an
   ally and *stay*; this version keeps `no two living entities share a tile` by
   ending the tick with `SimTick.separateStacked`, which steps a duplicated body to a
   free neighbour. Neither entity is ever banned mid-tile and no route is lost, so a
   corridor carries traffic both ways instead of refusing it.

   **The trade comes last, and it is what finishes the corridors.** A settled ally
   used to discard the mover's route (`if taken next` → abandon the plan, set the
   stall backoff), so a leader whose *first* step was a settled ally backed off
   forever — a `Tank` at `(19,125)` with goal `(32,106)`, a settled `Priest` on the
   only exit, claim-aware route 0 steps and relaxed route 27. Now a settled Party
   body on the next step no longer discards the route, and when the step lands
   `SimTick.swapStep` trades tiles: the mover takes the ally's tile and hands back
   its own, atomically, so `separateStacked` never sees a stack. Party trades with
   Party only (a hostile's tile is a target, not a place to stand) and never with
   the Tank, which is the formation's anchor — that guard is what keeps
   `a stationary ally no longer severs the route behind it` meaning what it says.
   The ladder went from 32 of 36 cells clean to **36 of 36, 0 wipes**.

   Measured on the eight-seed freeze probe (24000 ticks): 8/8 frozen before, 0/8
   after, including the two seeds where the Tank lived and blocked the door.

   **Entering a form costs mana; leaving it costs nothing.** The operator's rule, and
   the asymmetry matters: the requirement lives on the `Ability`, not on the AI — one
   guard where abilities resolve refuses an ability whose form the actor is not in, so
   Tree of Life forbids Healing Touch as a rule rather than as a convention the AI
   happens to keep. Shifting out needs a `Human Form` ability, because "no form" is a
   stance the entity can be in and nothing in the kit could produce it. The movement
   penalty is deliberately not modelled; the shift's price is, and it is not cosmetic —
   with a free shift the healer re-entered Tree of Life as a matter of course and the
   party starved of its healing, and one graveyard cell on the scaled ladder parked
   that way until entering cost something. `shortcut: 60` is a flat level-24 figure that
   does not scale to the level-60 sheet, where Healing Touch costs 405; vanilla charges
   a share of base mana and the OctoWoW research will move these numbers anyway.

   That shift changed the benchmark at seed 9 and the change is a **divergence, not a
   speed-up**: the bench party's Druid is a caster body that never shifts, but its kit
   now carries a price, one affordability check flipped a tick earlier than before, and
   the fight resynchronised onto a different path (867 ticks instead of 909, more health
   and mana left). It was verified by A/B — setting the cost back to zero returns seed 9
   to 909 exactly — so the snapshot was refreshed knowing which line moved and why.

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

   A **second mutual-block shape**, found only at level 60 and fixed in `5f43d19`: the
   aside search asked `claimedByOther`, which reports a *moving* ally's tile as free,
   so in a cluster where every body is blocked by another body the escape tile was
   another body's. The Tank held `(10,177)` with destination `(9,178)` (the Mage)
   while the Mage's best escape was `(9,179)` (the Priest): everyone had a
   destination, nobody arrived, and the state read identically
   `inCombat=false aliveHostiles=34 engaged=0 pending=0` at tick 5000 and tick 40000.
   The aside now takes only a tile that is empty of every living body *right now*.
   Worth remembering as a rule: `alive=5.0` plus a frozen hostile count is not
   evidence the Party is fine. Seed 1 graveyard at level 60 goes on to reach
   `(26,186)` with hostiles down 34 → 22, and the effect across seeds is that
   graveyard stops being unwinnable (item 2).

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

`5f43d19` took out the mutual-block half of this (the party no longer steps onto a
body mid-move), and the shape it leaves is now measured. Seed 1 graveyard level 60
gets from `(10,177)` to `(26,186)` and then holds with `goal=(36,175)`, `path=0`,
`dest=None`, `stall=17`, the nearest hostile 12 tiles away at `(36,174)`, `freePath=100`
and `takenPath=0`. The route exists with no bodies in the way and does not exist with
claims allowed, so something is standing on the *first* step: `aim` falls through to
`Some p` (the mob's own tile), `stepToward` cannot plan, and each retry costs one
backoff with nothing moving between them. That is the same no-route-to-a-legitimate-aim
shape as the paragraph above, and the fix belongs there.

What it is worth on the ladder: at `comps 3 --level 60`, graveyard goes from no clear
in eight compositions to `Priest` and `Paladin` clearing 1 of 3 seeds each, every
other composition still unresolved at 24000 with 5 alive. Gully is untouched —
`runs 8` at the default level is 8/8, median 912. **[That ladder predates the advance
fix and the pull radius of 2 — the stalls it reports were the sight-less aim, so it
needs a fresh run before it says anything about healers.]**

**The aside reaches six tiles while the Tank is sealed.** `stuckWithGoal` (a goal,
no route, no destination) used to widen nothing: the aside looked one tile, so in a
one-wide tube — where only the ally at the *back* has floor to step onto, five tiles
away — nothing could move and the queue could never unwind. The reach is six tiles in
that state and one otherwise, so a party that is nothing but a queue drains from the
back. It changes no ladder number (`comps 3 --level 60` is still 2 cleared runs in 24,
gully still 8/8 at the default level), because the jam it fixes is not the one blocking
those runs.

**What is blocking them, measured.** Seed 1 graveyard level 60 replays into a state
that is identical at tick 16000 and tick 24000: Tank on `(25,186)` with
`goal=(36,175) path=0 dest=None`, a DPS on `(25,187)` whose own goal is the Tank's
tile, and a second DPS on `(25,188)` with `path=0` and `destination=(25,187)` — a
teammate who is standing still. It waits there forever, because `advanceMovement`
holds a mover whose destination is in `settled`
(`src/WowRoguelike.Core/SimTick.fs:220`) and `MoveTo` rewrites `Goal` and `Path` but
never `Destination` (`src/WowRoguelike.Core/SimCombat.fs:897`), so the mover is deaf to
every later order — including the aside. Clearing the destination on that branch is the
obvious fix and was tried and reverted: the Tank then shuttles between `(9,178)` and
`(11,177)` with 36 mobs alive and no kill at the cap, which is worse than standing
still. Whoever takes this next should start from those two line numbers: the
destination needs to be given up, but not on every blocked tick.

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

   **Healer and DPS composition, re-measured with `sim comps 25` after the talent,
   totem and resurrection work** (25 seeds per map, scripted party, ~28 minutes; a
   player will do better — this is the floor a composition sets). The rows this table
   carried before that work are retired: `a05ae24` gave **the four healers** their
   level-60 healing talents, so a level-24 hero is no longer the hero those rows
   measured. It did not give them to every class, and **structurally cannot**:
   `type Talents` (`Domain.fs:889-930`) models healing only — heal-done, heal-cost,
   Intellect, regen, crit mana refund, crit armour, Healing Way, Spirit-to-healing
   — so the five DPS classes are `Talents.none` by construction (`Content.fs:3321`
   Warrior, `:3433` Rogue, `:3480` Mage, `:3646` Warlock, `:3806` Hunter). A DPS
   class therefore grows from 24 to 60 by ability ranks and the gear curve alone,
   while a healer grows by those *and* a talent tree. That asymmetry is a confound
   in the level-60 comparison above, not a finding about the classes.

   | round | gully cleared | gully alive | gully median | graveyard |
   |---|---|---|---|---|
   | Warrior/Priest/Mage/Rogue/Warlock | 21/25 | 3.6 | 912 | 21 wipes, 4 unresolved, alive 0.5 |
   | Warrior/Druid/Mage/Rogue/Warlock | 24/25 | 4.5 | 870 | 21 wipes, 4 unresolved, alive 0.7 |
   | Warrior/Paladin/Mage/Rogue/Warlock | **25/25** | **5.0** | **775** | 22 wipes, 3 unresolved, alive 0.6 |
   | Warrior/Shaman/Mage/Rogue/Warlock | 24/25 | 4.5 | 1034 | **6 wipes, 19 unresolved, alive 3.6** |
   | Warrior/Paladin/Priest/Mage/Warlock | 25/25 | 5.0 | 900 | 1 wipe, 24 unresolved, alive 4.6 |
   | Warrior/Paladin/Druid/Mage/Warlock | 25/25 | 5.0 | 888 | 3 wipes, 22 unresolved, alive 4.4 |
   | Warrior/Paladin/Shaman/Mage/Warlock | 25/25 | 5.0 | 918 | 3 wipes, 22 unresolved, alive 3.7 |
   | Warrior/Paladin/Mage/Warlock/Hunter | 22/25 | 4.3 | 828 | 18 wipes, 7 unresolved, alive 1.2 |

   Paladin is still the only healer that clears gully every time with three DPS
   behind it; a second healer buys nothing on a map the party can win, and on
   graveyard it makes the party unkillable *and* unable to finish — 22-24 of 25 runs
   hit the tick cap with four to five alive, which is the alternating-route stall
   above, not a healing stalemate (no hostile is ever engaged, party HP never moves).
   Druid is the biggest mover (17/25 -> 24/25 with the talents and Rebirth), so
   "Druid is the weak healer" is retired with the rest of the old rows. The Shaman
   holds gully but is the slowest single healer (1034 against 775) and is the one
   healer whose graveyard row inverts the pattern: 6 wipes against Paladin's 22,
   because it out-heals a level-24 pack, and 19 unresolved with 3.6 alive, because a
   healer that does no damage is a pull that never closes. Swapping the Rogue for a
   Hunter costs gully survivability (5.0 -> 4.3 alive) and buys graveyard longevity
   (22 wipes -> 18, alive 0.6 -> 1.2), which is the pull distance working: fewer mobs
   wake up per pull. **Graveyard median is not a clear test for any five-member
   party** — it is a wipe-rate and stall-rate test.

   **The Hunter's rows above are stale: its kit changed after them.** The class had
   three abilities (Arcane Shot, Raptor Strike, Serpent Sting) and every one of them
   instant, which made it the thinnest kit in the game and the only DPS class with no
   cast for an interrupt to catch. It now has nine:
   - **Aimed Shot** (75 mana, 3 s cast, 6 s cooldown, +70 — the researched rank 1),
     the cast the pull protocol wants.
   - **Scatter Shot** (60 mana, 20 s cooldown, `Interrupt` plus a 3 s `Silence`), the
     game's only silence. The sim's `interrupts` predicate (`PartyAi.fs`) keys on the
     `Interrupt` effect itself, so the Hunter became a counterspeller without a new
     code path. It fires after the reclaim, never at a pulsing target: mobs that cast
     on a cooldown get silence, mobs that never cast get wand damage.
   - **Summon Pet** + **Mend Pet** (90 mana, 38 a second for 5 s), the first use of
     `SummonSpec`: the pet is a real entity with an owner, so it can die and has to be
     healed. Mend Pet fires off the pull decision table, so a pet that lost health
     keeps being mended at the next pull rather than never.
   - **Multi-Shot** (100 mana, 10 s cooldown, `MultiShot 3`), the only ability in the
     roster whose damage is per target, and the reason `resolveAbility` takes the aim
     target: it picks the aimed body plus up to two more hostiles in range and rolls
     weapon damage for each. The AI will not spend it on a single body — one aim,
     one hit is a worse Arcane Shot for double the mana — so it only fires when a
     second hostile is within 8 tiles of the target.
   - **Viper Sting** (`DrainResource(8 s, 15)` — 90 mana, 20 s cooldown), the first
     drain in the game and the first use of `Drained`: 15 mana a second for 8 s, its
     own aura, so re-stinging a stung target refreshes rather than stacks. The AI
     re-applies it only once the last sting has lapsed and only to a body that both
     has a pool and cannot afford to lose it. Against `Druid of the Fang` — 400 mana,
     no regeneration (`Content.fs:1547`) — one sting is a third of its pool, which is
     the intent: a sting that shortens a caster's fight, not one that ends it.

   Aimed Shot, Scatter Shot and Viper Sting are **out of band**: vanilla gates all
   three behind levels 36-40 or 21 talent points a level-24 hunter cannot have, so
   none is measured pre-existing content. Aimed Shot's ladder is rank 1 only because
   the research has no others (a level-60 hunter's Aimed Shot is a level-20 one until
   the ladder is extended), Multi-Shot is the researched rank 1 (level 18, in band),
   and Viper Sting's amounts are the only numbers in the hunter kit with **no
   research at all** — an estimate sized against the level-24 mobs, marked with the
   file's `shortcut:` convention. Re-run the comparison before quoting any Hunter
   DPS or clear number against the new kit; the 3806 in the table before this one and
   the 22/25 row above both measure the three-ability class.

   **The same ladder at level 60** (`sim comps 10 --level 60`, round 1) is the scale
   work's problem, not the kit's:

   | healer | gully | graveyard |
   |---|---|---|
   | Priest | 10/10, alive 5.0, median 390 | 10 unresolved at 24000, alive 5.0 |
   | Druid | 10/10, alive 5.0, median 367 | 10 unresolved at 24000, alive 5.0 |
   | Paladin | 10/10, alive 5.0, median 390 | 10 unresolved at 24000, alive 5.0 |
   | Shaman | 10/10, alive 5.0, median 480 | 10 unresolved at 24000, alive 5.0 |

   Every composition clears gully and none of them even wipes on graveyard: all ten
   seeds of all four healers sit at the 24000-tick cap with five alive and hostiles
   still standing. At level 24 graveyard is a wipe-rate test; at 60 it is a stall, so
   either mob health outgrew party damage or the pull stalls outright — that
   distinction is the first thing the level work should settle, and gully is already
   fine. The Shaman is 20-30% slower than the other three at both anchors.

   **Composition rows may not seat one class twice.** Two copies of a kit weight the
   party toward it and read as a finding about the class when the finding is about the
   roster, so `report` (`src/WowRoguelike.Harness/Program.fs:164-166`) refuses a
   duplicate with `failwithf` instead of publishing the row, and the per-seat table
   de-duplicates its labels (`Program.fs:152`). Pairings are the point: round 2 seats
   the best single healer beside each *other* healer, never beside itself.

   **Reincarnation makes a wipe survivable, and that is a mechanic, not a bug.**
   Three pieces, all new:
   - **A mob anchor.** `Entity.Home: Pos option` (`Domain.fs:1184-1190`) is set once in
     `Content.mob` (`Content.fs:1739`) and is `None` for the party, the pet and the
     Shaman-kit literal.
   - **An encounter reset.** `SimState.encounterReset` (`SimState.fs:262-297`) runs as
     the *last* phase of `SimTick.step` (`SimTick.fs:681-684`) so it sees the fight as
     the tick left it: when the party is all corpses and `resurrectionPending` is
     true, every hostile teleports to `Home`, refills, drops its auras, cooldowns,
     threat and target, and disengages. Corpses stay where they fell — they are the
     run's stake. Nothing else ever clears `Engaged`, so this is the sim's first
     evade.
   - **A death spot.** The last living member, if it is a `Healer` holding an
     off-cooldown `Self` resurrection (`PartyAi.fs:917-931`), is filtered out of
     `actors` (`PartyAi.fs:933`) — one filter that silences `healerOrders`,
     `tankOrders`, the step-aside shuffle and the `leaderless` walk, all of which
     would march it into the pack it has to die away from. It then takes only
     `fleeing` (`PartyAi.fs:1374-1420`): walk to a floor tile more than `aggroRadius`
     from every living hostile's home that is still within 8 tiles of a corpse, and
     hold there to die. After the reset the same order walks it back to the bodies.

   `Ancestral Spirit` (`Content.fs:3537-3548`, Shaman kit `Content.fs:4047`) is what
   the survivor casts: `Corpse` target, 2 s, 40% of both pools, no cooldown. Its 300
   mana is an **estimate** — `.pi/research/ranks/` has no `shaman.md` — and at 40%
   mana the Shaman has ~660, so one Reincarnation raises **two** of the four corpses.
   In vanilla the mana is the real limit on the comeback too. The tests pin the
   boundary the death spot buys: 3 tiles from home is unseen, 2 tiles is pulled again
   (`Tests.fs:3102`, `:3115`).

   The two buttons are limited by different things, and that is the point.
   `Reincarnation` itself costs **no mana** and carries a **30-minute cooldown**
   (`Content.fs:3517-3525`; Improved Reincarnation — `shamanTalents.CooldownOff`,
   `Content.fs:3330` — cuts 10 minutes off, which is data, not a branch), so it is one
   use per run and the cooldown, not the pool, is what it costs. `Ancestral Spirit`
   has no cooldown and is limited by mana. Removing Reincarnation's mana cost while
   keeping the long cooldown is deliberate: a wipe-recovery button should be gated by
   the run's duration, not by whether the Shaman happened to finish the last pull with
   a full bar.

   **A wipe nobody can undo has to end.** `resurrectionPending` (`SimState.fs:96-113`)
   is what keeps the tick loop outliving `PartyWiped`, and it counts only what the
   corpse could actually pay for: a `Self`-target resurrection (`Ancestral Spirit` is
   Corpse-target, and a corpse cannot cast one) whose cost the corpse can afford. A
   corpse does not regenerate (`SimTick.fs:126`), so an unaffordable resurrection is
   unaffordable forever, and counting it spun the rest of the tick budget ordering
   nothing — measured on graveyard seed 5, which used Reincarnation early and then
   burned 18,951 ticks after its wipe. It now resolves as `PartyWiped` at tick 5049.

   Two known trades. **`fleeing` does not ask whether the fight was winnable** — a last
   Shaman beside a single nearly-dead mob still walks away, because there is no
   wipe predictor and inventing one was not worth a heuristic; the comps rows are how
   to tell whether that costs clears. And **`Ancestral Spirit` does not enforce the
   out-of-combat rule**, exactly as `rebirth` does not.
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

   The pull also waits for the Party. While any member stands further from the Tank
   than the follow leash, or any living member is short of **either** full health or a
   full mana pool, `PartyAi.decide` empties its hostile list and clears the Tank's
   route — so no opener fires, no ranged member walks up to a focus it can see, and a
   Tank already mid-corridor stops instead of finishing the walk into the pack.
   Out-of-combat recovery is **not a trickle**. `World.RestTicks` counts the Ticks the
   party has spent continuously out of combat and is reset to zero the moment a hostile
   engages; when it passes `Content.outOfCombatRestTicks` (100 Ticks = five seconds) the
   whole party is restored at **10% of the pool per Tick**, so a zeroed bar is clean
   half a second later. Measured on `Content.gully 1UL` with every party pool drained to
   1 HP and 0 mana: nothing changes at t=100, the first 10% lands at t=101, and
   Priest 1073/1073 + 1985/1985 and Warrior 1223/1223 are reached at t=110. The
   percent-per-Tick is rounded up because ten steps of 107 out of 1073 leave three
   points short — the snap has to land exactly on the maximum or "fully restored" is a
   lie for odd pools. Rage decays instead of refilling, so a Warrior's bar is never part
   of the wait — gating on it would park the party forever; Energy likewise is excluded.
   `World.RestTicks` sits on the World rather than on each Entity because `inCombat` is
   itself party-wide: there is one fight, so there is one clock.
   `out of combat a zeroed party is whole again in five seconds` and `out of combat the
   party waits five seconds then snaps to full, and mobs never heal` cover it. `the tank waits for the party before pulling
   the next pack` and `five seconds out of combat refills health and mana to full`
   cover it.

   Consequence to keep in mind when reading a ladder: **trash that cannot out-damage
   regeneration no longer needs healing at all.** In `gully` at L24 the Warrior now
   never drops below ~92%, so a whole run logs no player heal — the only heal lines
   are mobs healing a full-health Tank for 0. Any measurement that assumed a healer
   does work in light content has to damage the Tank first.

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
   twelve kits exist — nine classes, and the Druid's four specs — and five get
   seated. `Content.partyOf`/`withRoster` are ready for a picker, and a spec'd
   entry (`"Druid (Bear)"`) can be seated through them today.
7. **Slice 4: boss phases.** `Phase` is in the glossary with no implementation.
   Five open design questions are listed in `TODO.md` §Slice 4 — put them to the
   user rather than guessing. `Mutanus the Devourer` is deliberately not stood in
   for: his fight is a summoning event with no sourced numbers.
8. **Mobs do not patrol.** The tank's pull-veto (`packGap`) is an approximation of
   avoiding patrol paths, not a patrol model. If patrols arrive, that heuristic
   should be revisited rather than layered on.
9. Smaller, from `TODO.md`: extract a named-field `Entity` builder (its trigger — a
   second map — has arrived); expose replay and make pause/step edge-triggered;
   add the BSP generator. Dead code to delete on the way past: `PartyAi.bestOf`
   (`PartyAi.fs:291`) has no caller; the Rogue's circle-back re-position branch is
   `Some a when false && …` (`PartyAi.fs:601`), so a Rogue can skip Backstab for
   want of position and never walk into it; and `VAR checkpoint` in
   `wailing_caverns.ink:31` is assigned and never read.

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
3. **`code-review`** — the `654fc35..72e15f9` set is still unreviewed (see the
   header). Run Standards and Spec as parallel sub-agents. The last review found a
   genuine P1 (a move order could path an entity into a wall) that all 51
   then-existing tests missed. Everything committed since that set has a body and a
   measurement behind it, but the gear curve and the rank tables have not been read
   by a reviewer.
4. **`diagnosing-bugs`** — for the pathing deadlock in §8/§9. It is a
   reproduce-then-fix job, and the reproduction step is already done: `smtrace <wing>
   <band> 20000 2000 v` prints route, goal, mob targets and the stall flag, and §9
   item 2 records the exact stall state at seed 1 level 60, down to the mover whose
   destination is a standing teammate's tile. The trap is that the obvious fix
   (clearing that destination) has already been tried and measured as a regression.
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
- **Art the sim cannot express yet.** `tools/slice_tileset.py <sheet>` has three
  named sheets: `wailing` (terrain, classes, Wailing Caverns creatures), `forms`
  (Druid cat/bear, the hunter pet, the four totems) and `future-forms` (Druid
  tree/moonkin). A totem is drawn by its slot rather than its name —
  `TotemSlot.ofPulse` — so `totem_earth`/`air`/`water` follow the AI's own rule
  and a renamed totem keeps its art; `totem_fire` is sliced with nothing mapping
  to it, because no fire pulse exists. The pet is `Name = "Pet"`. Of the four
  Druid forms, **Bear, Cat, Tree and Moonkin are drawn**: `TakeStance` sets a
  `Shaped` aura (`Aura.stanceOf`, `SimState.stance`) and `spriteKey` reads it.
  Travel Form is still un-drawn and has no art sliced, so a travelling body
  stays in its own shape rather than borrowing another form's.
- **The Druid is four specs, one body.** Bear (Tank), Tree (Healer), Cat (feral
  Dps) and Moonkin (balance Dps) are picked by the roster string and nothing else:
  `"Druid"` alone is the Tree healer, so every existing roster, doc and ladder
  cell keeps its meaning, and the suffix — `"Druid (Bear)"`, `"Druid (Cat)"`,
  `"Druid (Moonkin)"` — picks the other three (`heroOf`,
  `src/WowRoguelike.Core/Content.fs:4418`, reading the suffix in `druidSpecOf`,
  `src/WowRoguelike.Core/Content.fs:4408`; an unknown suffix is a `failwithf`, not
  a silent healer). One builder (`src/WowRoguelike.Core/Content.fs:3871`) shares
  the base row, the talents and the resource, and each spec carries **its own
  gear list and weapon ladder**, because a bear tanked in a healer's Intellect
  set is a bear with no armour and no attack power. The sheet has two anchors: the
  level-24 rows are that spec's own best-in-slot outfit, and `Gear60` is what the
  spec's real raid tier sums to.

  | spec | level-24 outfit | level-60 anchor |
  |---|---|---|
  | `Druid` (Tree, Healer) | caster nine + Staff of the Blessed Seer | `(1376, (0, 0, 196, 261, 158))` — +healing cloth |
  | `Druid (Cat)` | the Rogue's own sheet, Smite's Mighty Hammer | `(1102, (51, 92, 72, 0, 10))` — cat BiS |
  | `Druid (Moonkin)` | the caster nine, Emberstone Staff | `(600, (0, 0, 87, 150, 40))` — balance cloth |
  | `Druid (Bear)` | armour-and-Stamina leather, Slaghammer | `(2018, (71, 98, 184, 15, 7))` — Warden Staff |

  Cat reuses the Rogue's sheet and Moonkin the healer's: the two agree on what
  those bodies wear at 24, so the outfit that differs is the raid one. Cat and
  Bear split their weapon ladders on the same reasoning — the cat swings
  Manual Crowd Pummeler at 60, the bear Warden Staff from 43, whose 260 armour no
  cat weapon carries. Attack power follows the body rather than the class: a
  bear's Strength buys two a point and its Agility none, a cat's Agility two and
  its Strength one. Every item and total, with the host it was checked against,
  is in `.pi/research/level24/druid.md`.

  Bear armour is `Traits.ArmorBonus = 799`, which is the 180% Bear Form
  contributes at level 10 applied to the 444 the level-24 set carries, not an
  independent number. `shortcut:` it is a flat sum pinned to that set and does not
  grow with the level-60 sheet, so a level-60 bear carries 2817 where its own gear
  would carry 5650; a per-spec armour multiplier is a new field and a separate
  decision.

  Every spec keeps `Entity.Name = "Druid"`, which is what art, logs and the tests
  that find the class key off. Each kit carries its form (`bearForm`, `catForm`,
  `treeOfLife`, `moonkinForm`) and the body **spawns in it**
  (`src/WowRoguelike.Core/Content.fs:3928`), so the art is right from tick 0 and
  there is no one-tick window in which a bear could cast from caster shape.
  Bear and Cat forbid casting (`Aura.preventsCasting`), so the lockout is honest
  from the first tick. What is still *not* built is the gameplay that shifts
  mid-fight: nothing in `PartyAi` picks a form, so a Bear or Cat stays in the one
  it spawned in and the healer never shifts out.

  **Which forms a spec may enter is role-gated, and the gate is data.**
  `Content.druidForms` returns, per roster name:

  | spec | forms |
  |---|---|
  | `Druid` (Tree, Healer) | Tree of Life, Bear, Cat, Human |
  | `Druid (Moonkin)` (Caster dps) | Moonkin, Bear, Cat, Human |
  | `Druid (Cat)` (Melee dps) | Cat, Bear, Human |
  | `Druid (Bear)` (Tank) | Bear, Cat, Human |

  Tree of Life belongs to the healer and Moonkin to the caster damage spec; Bear
  and Cat are open to every role; every role may also drop back to the human
  shape it was born in. Bear and Cat are *weaker* for the two caster roles, and
  deliberately not by a penalty number: `Aura.preventsCasting` already makes a
  healer that shifted to bear a healer that cannot heal. `Stance.HumanForm`
  exists so "the shape you were born in" is a stance you can shift into rather
  than the absence of one, and `TakeStance HumanForm` drops the `Shaped` aura
  instead of adding a form (it is distinct from `Form.Humanoid`, which is what a
  creature *is*, not what it stands in). Three tests hold this: the table above,
  that each spec spawns in a form on its own list, and that shifting to human
  leaves no form behind.
- **Maps for five further dungeons** (Ragefire Chasm, Razorfen Kraul, Shadowfang
  Keep, The Deadmines, The Stockade) are extracted into `images/Maps/`, and
  Blackfathom Deeps exists only as sliced tiles/sprites in `Assets/`. Per-map
  verdicts — which are usable as art and which are not — are in
  `.pi/research/levelscale/map-assets.md`. All of this is art and research only;
  no content is built on it.
