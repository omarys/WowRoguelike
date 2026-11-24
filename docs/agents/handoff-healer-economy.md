# Handoff — healer economy, Tree of Life, and the fight-duration budget

Successor to `docs/agents/handoff.md`, which is still the reference for everything that
landed before this session. Read **this** file for the remaining work; read
`docs/agents/domain.md` for architecture (it is 200 KB and authoritative — do not
re-derive what it already states).

- Repo: `/home/omary/Dev/wowroguelike` (F#, .NET 10)
- Branch `main`, HEAD `65dcd40`, **all of this session's work is uncommitted**
- **360 passed, 0 failed** (`dotnet test tests/WowRoguelike.Tests -c Release`), Release
  build clean with 0 warnings; all 30 benchmark seeds `EncounterCleared`
- `mise trust` once per clone, or the `dotnet` shim falls back to SDK 8 and the pre-commit
  hook dies on `WowRoguelike.slnx` (`domain.md` §4)

The working tree also carries modifications this session did **not** make and did not
review: `openspec/**` (specs, config, rag-manifest, plus four untracked spec dirs),
`src/WowRoguelike.Core/Attribution.fs`, `src/WowRoguelike.Core/Bench.fs`. Establish what
they are before committing anything, and do not assume they are part of this work.

## What landed

### 1. Restoration Druid mana economy

The Druid was the least efficient healer (1.83 healing per mana, worst of four). Two
independent sinks: Rejuvenation re-pressed on targets that already carried it (209 wasted
presses), and Regrowth used as a substitute for Healing Touch while starting from a
smaller pool. Both were one error — *buying cover for time already paid for* — so they
were fixed with a single shared predicate rather than two patches.

Result: **1.83 → 2.79 healing per mana** (second best, behind the Paladin at 3.14),
`manaHeal` 14 027 → 11 897, Regrowth overheal 53.3% → 10.8%, Regrowth HoT waste
46.9% → 19.4%, end-of-fight mana 35.6% → 68.9%.

The five-second-rule work from the same stretch is in `SimCombat.fs` (regen forfeit on
cast, with `costToClose` and the hold branch) and is covered by tests in
`tests/WowRoguelike.Tests/Tests.fs`.

### 2. Tree of Life package

`Content.fs:4435` was `form TreeForm "Tree of Life" 60` — legitimately a stance and
nothing else. Now:

- +180% **item** armour while shaped (`armour` is a plain int downstream, so the coded
  `+1800`-style value used elsewhere would have produced 19× instead of 2.8× — verified
  by measurement, 360 → 1008 alone and 360 → 1944 stacked)
- party-wide healing power +20% of the Druid's Spirit, applied at resolution for direct
  heals and at application for HoTs
- HoT mana cost −20% in form
- no damaging spells and no Healing Touch, except Healing Touch under an active Nature's
  Swiftness; **Swiftmend loses its cooldown entirely** while in ToL (the OctoWoW house
  rule)

`druidTalents` (`Content.fs:4210`) also gained **Genesis** (+15% periodic healing and
damage) and **Preservation** (+30% Regrowth periodic while the target carries
Rejuvenation). Both were missing from the data entirely. Note `Talents.spell` is a
`List.tryFind`, so a second row for one spell is silently dead — one row per spell.

The healer now holds ToL as its home stance and shifts out only for a hole the form cannot
cover. A comfortable ToL Druid idles ~57% of a fight with ~70% mana and **cannot spend it**,
because the form forbids damage. That is the delivered design, not a defect, and the
operator has not authorised changing it.

### 2b. Shaman mana efficiency, and the idle metric rebuilt

The Shaman was the worst healer on every axis (1.63 healing per mana). Three independent
causes, none of them the obvious one:

- **It healed a 420-hp Imp with player-sized heals** — 19 502 mana over 12 seeds for
  8 895 healing, because its kit held one direct heal and so `costToClose` had a
  one-element choice. A downrank rung (`Content.fs:5770`, wired `:5850`) took that body
  from 0.46 to 0.81 healing per point. It stays the worst row by construction: a 420-hp
  body caps at 420 health, and rank 4 is the bottom of the ladder a level-36 Shaman holds.
- **No cheap rung at all**, so 1–25% holes drew a full-rank heal at 84.6% overheal. That
  band no longer appears in the ledger; the trained rank's overheal fell 27.9% → 5.3%.
  Adding rank 3 was measured and **rejected**: better per point, but 5 of 30 seeds ran dry.
- **Self-inflicted starvation**, and the mechanism is the interesting part: `ManaBudget`'s
  floor is `marginHeals` (2) × `oneHealCost` (`SimState.fs:568`, the *dearest* heal), and
  that floor is also the entire burst budget, because the gate spends the pool down to it.
  So the floor buys two *presses* regardless of class — eight 63-mana Lesser Heals for the
  Priest, but only two 251-mana presses for the Shaman, so every third heal of a burst was
  refused. The rung gives the wall 3.4 presses. Blocked ticks within 20 s of the Shaman's
  own damage cast 1 867 → 502, dry seeds 2 → 0.

30 seeds: hps 49.1 → **55.8**, healing per mana 1.63 → **2.13**, mana-blocked 9.2% → 5.0%,
end pool 65.4%, no dry seeds. The other three are byte-identical (Priest 88.4 / Paladin
74.5 / Druid 90.2 hps).

**This traded Shaman damage for Shaman healing: 28.0 → 21.9 dps.** That is honesty rather
than a nerf — it was previously casting shocks it could not afford and then holding heals
it could not pay for — but if its damage is wanted back, the lever is pool or regen, not a
looser gate. `marginHeals` and `drainMargin` were deliberately **not** retuned: they are
shared with the Paladin, and moving them moves classes that were required to stay fixed.

The in-combat idle metric was also rebuilt, because the old one read only `Casting` and so
scored every instant cast as idle (`SimCombat.fs:1827` resolves instants without ever setting
the bar). **There is no GCD in this sim** — that is the main reason the metric was unfair.
Ticks now partition into `active` / `covered` / `unmet` / `blocked` / `recovering`, summing to
100% with that closure asserted in code (`PartyAi.fs:1327-1355`, harness loop
`Program.fs:750`). The partition also caught a real classifier defect: `healerStandby`
tested cooldown before form, making `WrongForm` unreachable (`PartyAi.fs:1268`).

The operator's read on the Druid is **confirmed and must not be re-opened as a defect**:
of its 51.8% idle, 42.3 points are `covered` and 0.1 `unmet`. Its instants and its
Regrowth/Rejuvenation coverage are doing the work. The Priest is the *least* idle class
(11.4%), which is structural rather than a bias — a pure hard-cast healer never gets the
option to stand still. Instants and HoTs buy permission to be idle; that permission sits
with the Druid.

### 2c. Shaman pass 3 — Earth Shock as an interrupt, shared shocks, and the spell choice

Four defects, all operator-identified. Evidence in
`.pi/research/octowow/shaman-damage-and-interrupts.md`; the operator's rule is that
**Earth Shock is an interrupt and nothing else**.

**1. Earth Shock was never pressed.** Zero casts in 30 seeds before this pass, although it
is in the kit, instant, 6 s, range 5. Two independent causes: the damage rows and the
kick rows were ranked as one list (so a 0.42-point shock loses to any bolt), and the kick
sat behind a lane that never ran it. It is now its own row, and it is pressed **first**,
before any damage button, whenever the shaman holds an interrupt assignment.

**2. A dropped assignment was a party-wide bug, not a Shaman bug.**
`PartyAi.interruptAssignments` (`:113-152`) gives each interruptible cast to **exactly one**
member — the cheapest kick that can land — and tells the others it is handled. Three lanes
acted on their assignments (`tankOrders` `:453`, `meleeOrders` `:1731`, `rangedOrders`
`:1853`); `healerOrders` `:588` never read them. The Shaman holds the healer seat, so every
assignment it won was dropped by the whole party. The fix is a contract, not a branch:
`assignedKick` is the single place that answers an assignment, and every lane that can
interrupt calls it — the melee and ranged lanes first, the healer lane after its own heal
so a kick still outranks it. The tank lane keeps its own copy because a tank's shield slam
is its threat button. The layers are disjoint (melee / ranged+healer ranges do not overlap),
so the fix cannot double-kick. Two tests pin it: one asserts a healer-lane kick is emitted
with a hurt ally standing in heal range (where it previously vanished), and one asserts the
structural invariant directly — *every* target `interruptAssignments` hands out is answered
by some lane that can interrupt, swept over targets that reach the healer lane as well as
the two melee lanes.

**3. Shocks shared one 6 s cooldown in WoW; here they had one each.** `Content.fs:~1217`
carried the shortcut comment. Teaching the AI to press Earth Shock without fixing this
would have given the Shaman two 6 s shock buttons — more interrupts than vanilla allows
*and* more damage. The cooldown is now keyed on the shock family, the comment is replaced
with the reason, and a test asserts a shock is refused inside 6 s of another shock.

**4. Flame Shock was the largest damage-lane spender because instants headed every
ranking.** `perCast` divides by `CastTicks`, so an instant scores its damage times a
thousand and beats any cast. A stationary caster now ranks only the rows it can actually
cast to completion and keeps instants for the ticks it is walking (a move cancels a cast).
Measured on one seed, library median, with only that line differing:

```
                 total   Lightning Bolt   Flame Shock   auto
instant-first    9 291        3 712         1 739      3 840
cast-first      18 178       11 246           329      6 603
```

Flame Shock −81%, Lightning Bolt +203%. The rule is data-driven (`CastTicks`), not
name-driven, so it also moved the Hunter: standing still it now casts Aimed Shot instead of
Arcane Shot, and keeps Arcane Shot for the 26 s cooldown and for ticks it is walking. That
test was updated to pin both halves; it is the only other class the rule touches.

**Measured, before → after.** `sim shmana 30`, library at level 36, 30 seeds:

| | before | after |
|---|---|---|
| mana spent | 423 303 | **385 661** |
| regenerated | 397 907 | 372 616 |
| mean end pool | 69.5% | **84.2%** |
| healing per seed | 15 340 | **15 980** |
| mana sold to the damage lane (per seed) | 146 012 (4 867) | **98 657 (3 288)** |
| healer ticks blocked on mana | 2.5% | 2.0% |
| Flame Shock | 493 casts / 78 512 | **72 / 11 520** |
| Lightning Bolt | 503 / 67 500 | **650 / 87 137** |
| Earth Shock | 0 | 1 / 240 |
| Ancestral Spirit | 44 / 12 158 | 20 / 5 381 |

Healing rose 4% on 9% less mana and a 32% smaller damage lane, and the end pool gained 15
points. Damage in the lane moved the other way — nominal damage off the cast counts (126 a
Flame Shock, 190 a Lightning Bolt, ignoring crit and mitigation) is 157 688 → 132 622, i.e.
**−16% damage for −32% mana**, 1.08 → 1.34 damage per point.

**Interrupts, `sim interrupts 12` over graveyard + library + armory** (new diagnostic, reads
the interrupt lines `SimCombat.fs:1185` writes — the format it originally read,
`"%s interrupts %s's %s"`, now also names the kick: `"%s's %s interrupts %s's %s"`, a
later observability fix):

| | before | after |
|---|---|---|
| casts broken, whole party | 554 | **569** |
| nominal value of those casts | 124 491 | **129 915** |
| by the Shaman | 6 (952) | **18 (3 382)** |
| by Warrior / Rogue / Mage | 312 / 234 / 2 | 314 / 234 / 3 |

Every point of the party's gain is the recovered assignment: the Shaman took 12 more kicks
and nobody else lost one. It is only 2% of the total because the assignment rule hands a
cast to the *cheapest* kick that can land, and the Warrior and Rogue are in melee contact
with a 5-yard, 100-mana shock losing that contest by design. If the operator wants Earth
Shock to take more of them, the lever is the assignment rule at `PartyAi.fs:113`, not the
class.

**Which spell did the kicking — measured after the log line gained the kick's name.**
Pass 3's "18 Shaman breaks" was actor-only credit: no tool could say those breaks were
Earth Shock rather than anything else. The break line at `SimCombat.fs:1185` now reads
`"%s's %s interrupts %s's %s"`, `sim interrupts 12` prints a per-spell breakdown, and
the answer is that **all 18 of the Shaman's breaks are Earth Shock** — the pass-3 credit
is real kicks, not noise:

| kick | breaks | share of the 569 |
|---|---|---|
| Warrior's Shield Bash | 314 | 55.2% |
| Rogue's Kick | 234 | 41.1% |
| Shaman's Earth Shock | 18 | 3.2% |
| Mage's Counterspell | 3 | 0.5% |

96 of every 100 breaks are the two melee classes — the assignment rule working as designed.
Gouge, Hammer of Justice and Scatter Shot do not appear because the default roster is
Warrior / Shaman / Mage / Rogue / Warlock: no seat holds them.

Pass 3 also **did not touch** Water Shield, Improved Water Shield, Undertow or the gear
sheet, and did not retune `marginHeals` / `drainMargin`.

**Activity, `sim activity 30`, 30 seeds × 3 wings** (like-for-like, both sides at 30):

| | before | after |
|---|---|---|
| Shaman cast / move / idle | 63.1% / 17.4% / 19.5% | **71.7% / 15.9% / 12.4%** |
| Shaman mean pool | 60.2% | **69.1%** |
| Shaman idle on a dry pool | 42.7% of its idle | **29.5%** |
| run fighting / resting | 83.4% / 16.6% | 82.9% / 17.1% |
| mean pull / mean rest | 944t / 188t | 938t / 194t |

The cast share rises because the lane now presses a three-second cast instead of an instant;
that is not a cost, it is the same fight with a longer button. Every other class is within a
point of where it was (Druid 11.7→11.7 cast, Paladin 31.0→31.0, Priest 50.0→49.9, Warlock
47.0→46.2, Mage 67.4→67.5) — the drift is the RNG timeline, and it confirms the change is
isolated to the Shaman.

### 2d. Shaman totems — the party engine the healer never turned on

The operator asked whether the Shaman makes good use of its totems. It did not — four AI
defects and one engine bug, all fixed and measured (`sim totems 12`, library median, level
36, fixed seats with the Shaman healing):

1. **Distance-blind standing check** (`PartyAi.fs`) — a totem of a slot counted as "standing"
   while alive, wherever it stood. Totems never expire and never move, and one World spans
   the wing, so a party 20+ tiles past the drop site had lost the whole set while every slot
   still read covered. A slot now stands only within its radius of the Shaman.
2. **Mana Spring was dead code at 36** — kit order puts Healing Stream first in the water
   slot, totems never expire, and only Mana Tide (level 40) could displace it. The party's
   casters received zero totem mana for entire runs. The water slot now answers pressure:
   Mana Spring when the party is whole, Healing Stream when it is wounded; the toughPull
   Mana Tide logic is untouched for 40+.
3. **No out-of-combat maintenance** — totems were ordered only in combat (`betweenPulls`,
   `PartyAi.fs:2891`), so a fresh out-of-range pull spent its opening ticks rebuilding the
   set mid-fight. Out of combat the lane now re-establishes it while approaching the next
   pack, behind recovery and raise orders.
4. **Restorative Totems discount omitted three totems** (`Content.fs:3929-3948`) — Cure
   Poison, Cleansing and Grounding paid full price; all eight summon rows now carry the
   `CostBp` 2500 line.
5. **Boon totems with no period were placebos** — `advanceTotems`'s old guard
   `spec.Period <= ticks 0 || w.Tick % spec.Period <> ticks 0` skipped Period-0 rows on
every
   tick, so **Stoneskin and Grace of Air never applied their aura at all**, anywhere in the
   sim, while standing. The instrumentation's first run is what exposed it: Stoneskin
   booked 0 pulses over 12 seeds while "standing". Fixed at `SimTick.fs:121-126`.

Measurement (`sim totems 12`, before → after): run clearance **0/12 → 8/12**; drops 162 →
343 (re-drops after moving on); Healing Stream 129,199 → 97,811 HP given (the stream now
   shares the slot instead of owning it); Mana Spring 0 → 66,948 mana given (5,579/seed to
   the casters, deficit-capped); Stoneskin 0 → 685,231 aura pulses. The healer's own budget
   (`sim shmana 12`): end pool 82.2% → 93.4%, healing per seed 15,244 → 19,371 — it spends
   more because the party now survives to need it, and still finishes richer. `sim attrib
   library` is byte-identical before/after, so non-Shaman classes are untouched. The swing
   is the whole lane working together (live armor aura + spring mana + re-drops), not any
   one fix — the attribution of the 8 cleared runs to individual defects was not measured.

Caveat: the survey's quoted magnitudes were wrong — the kit rows are `RestoresMana 12` and
`Heals 44` (`Content.fs:5809-5810`), not the 15/2s and 55/2s the first survey derived.
Delivery is also deficit-capped, so per-pulse figures understate reach at full bars.
`TotemLedger` lives in `Domain.fs:2213` (the delegate report said SimState.fs — wrong file,
real code). Ledger numbers reproduced independently to the digit: 343 drops, 97,811 HP,
66,948 mana, 8/12 cleared.

### 3. Fight-duration mana budget

Replaces the flat "cast damage above 55% mana" threshold. `module ManaBudget`,
`src/WowRoguelike.Core/SimState.fs:384`.

- **party dps is an EWMA that never resets** (`:540-556`), time constant 80 ticks / 4 s
  (`:407`). Derived, not chosen: Healing Touch is 3.5 s and the DPS bolts 3.0 s, so a 3 s
  mean truncates a 3.5 s cast to zero for its whole duration — that was the oscillation.
  The old reset lived in `if not (inCombat w)`; only a TPK clears it now.
- **healer draw is a 400-tick / 20 s EWMA** (`:422`) and is **net**
  (`:494`: `(pool before − pool after) − damage spend`). Gross spend double-counted
  regeneration and reserved ten pools for a one-pool fight.
- **engagement epoch** (`:540-548`) tracks the engaged hostile set, incrementing only when
  the new set is not a subset of the old, so a corpse does not reset-storm mid-pack.
- **reserve** = `draw × min(ttk, 600) × 1.25 + safety`, where `safety = healsLeft ×
  oneHealCost` and `healsLeft = clamp(⌊ticksLeft / healDeliveryTicks⌋, 0, 2)`
  (`:595-616`, `:698-703`). The floor used to be a flat two heals, which demanded 706 mana
  with four seconds left in the fight and refused damage with the pull nearly over.
- gate `Budget = pool − reserve` against the cheapest damaging spell (`:680`, `:690`),
  consulted at `PartyAi.fs:2315` **before** `rangedOrders` — that function answers with a
  `MoveTo`, so filtering its answer still let a broke healer walk at the mob.

**30-seed same-tree A/B** (not cross-tree — see the noise note below):

| class | hps | dps | end mana |
|---|---|---|---|
| Priest | 88.1 → 88.4 | 21.0 → 23.0 | 57.2% → 49.2% |
| Shaman | 49.6 → 49.1 | 23.6 → **28.0** | 76.5% → 63.9% |
| Paladin | 75.3 → 74.5 | 9.8 → **12.2** | 48.3% → 41.9% |
| Druid | 90.3 → 90.2 | 8.4 → 8.7 | 76.5% → 78.2% |

**Read the gate as an affordability test, not a duration oracle.** The EWMA fixed the
rate, not the scalar: HP runs 0 → 20 000 at a pack boundary, so ttk still spans p10 5 s to
p90 198 s inside one seed. Degenerate readings fell (infinite/600-capped 2.2% → 0.4%, gate
flips per seed 33.7 → 20.3). If more precision is wanted, gate on
`draw × one-cast horizon` instead of raw ttk — that is the measurement's own
recommendation.

### 4. Cat Form unparked

Almost everything already existed: kit, spec, 100 Energy at 10/s (doubled under
Innervated), combo points, `"Druid (Cat)"` → `CatDruid` (`Content.fs:6017`), `formsOf`
giving `[CatForm; BearForm; HumanForm]`, role `Dps`, born in CatForm. Only the AI lane was
missing: `catOrders` (`PartyAi.fs:1251-1345`) and dispatch (`:2437-2445`).

Rotation: Berserk when the bar is within 5 Energy of full (a bar 5 short should not defer a
cooldown whose value is the regen it shelters), Rip at ≥4 CP on an unbled target, Rake when
the bleed falls off, Ferocious Bite on a bleeding target, Shred from behind / Claw from the
front, otherwise pool. Bleeds are tracked by caster id, not ability name. It works in-form
because every cat row is instant (`CastTicks = 0`) and `Aura.preventsCasting`
(`Domain.fs:773`) only refuses `> 0` casts; the cat kit contains no heal, so "no healing
from cat form" is true by construction.

The Feral build is decoded in `.pi/research/octowow/druid-feral.md` §10.1 (calculator build
`BSAaAI-AoYDQaDSQIFQB-Ag`, Feral 36 / Balance 11 / Restoration 4, slot by slot with
prerequisites) and mapped to fields in §10.2 against `Content.fs:5067 catTalents`. Ten
talents are encoded.

**Measured** — `sim attrib <wing> median 12000 --class "Druid (Cat)"` seats the cat in the
fifth slot (bare `Druid` maps to `TreeDruid` in `heroOf`, `Content.fs:6220`, so the spec
suffix is the whole difference). One seed per wing, direct damage only (no aura ticks;
Rake's bleed direct hits are counted):

| wing | cat dmg | party share | rank | tree Druid in same seat | ticks |
|---|---|---|---|---|---|
| armory | 25 068 | 17.6% | 4th of 5 | 5 658 (3.9%) | 7 938 (ended) |
| graveyard | 18 604 | 17.4% | 4th of 5 | 1 804 (2.4%) | 9 247 (ended) |
| library | 10 603 | 19.7% | 4th of 5 | 2 918 (2.3%) | **12 000 (wiped)** |

Rake > auto-attack > Ravage/Shred/Ferocious Bite is the damage order; the Druid heals
nothing from cat form (0 across all three wings, as constructed). dps at 20 ticks/s:
63.1 (armory), 40.2 (graveyard), unquotable for library (run unfinished). `attrib` now
prints `outcome=` (`cleared` / `wiped` / tick-limit) — added for this check — and the
verdict is: **armory and graveyard cleared, library wiped at the limit**. Same seed, same
wing, the tree-Druid seat cleared at 10 530 ticks, so the cat seat's one-healer party is
the difference. Single seed — the multi-seed version follows.

**Multi-seed durability** — `sim seat 12 <wing> [--class C]` (new instrument; the bare
`Druid` control runs the same seeds in the same fifth seat, so the columns below are a
clean A/B; direct damage and healing only, so the cat's bleed ticks are not counted):

| wing | seat | cleared | wiped | unresolved | mean clear ticks | seat died | seat dmg/seed | priest heal/seed |
|---|---|---|---|---|---|---|---|---|
| armory | cat | 12/12 | 0 | 0 | 8 196 | 0/12 | 24 617 | 28 735 |
| armory | tree | 12/12 | 0 | 0 | 9 215 | 0/12 | 4 389 | 13 068 |
| graveyard | cat | 7/12 | 4 | 1 | 9 693 | 5/12 | 19 088 | 22 992 |
| graveyard | tree | 11/12 | 0 | 1 | 10 360 | 0/12 | 3 772 | 9 299 |
| library | cat | 4/12 | 8 | 0 | 9 708 | 8/12 | 15 194 | 25 620 |
| library | tree | 8/12 | 0 | 4 | 10 284 | 0/12 | 2 226 | 13 149 |

Three readings. **Fights faster**: every wing the cat clears, it clears 6–11% faster than
the tree control. **Wipes fewer: no** — the tree never wipes (0 in 36 control seeds) and
never dies; the cat seat turns graveyard from 0 wipes to 4 and library from 0 to 8. **The
cat itself is the casualty**: it died in 5/12 graveyard and 8/12 library runs, and the
death and the wipe travel together — a melee body with no heal (the kit has none, and the
operator's ruling keeps Tree out of it) whose only healer is a Priest busy with the tank.
The single-seed table above was rosy: even graveyard, which cleared at one seed, wipes a
third of the time. Note the library tree control leaves 4 runs unresolved — the two-healer
party sometimes stalls where the cat party either clears fast or dies.

**Melee-slot A/B — the cat against the Rogue** (`--melee`: the seat takes the
ROGUE's slot, a Warlock sits fifth, so both arms share Warrior + Priest + Mage +
Warlock and differ only in the melee body; same seeds, one healer):

| wing (level) | seat | cleared | wiped | unresolved | mean clear ticks | seat died | seat dmg/seed | priest heal/seed |
|---|---|---|---|---|---|---|---|---|
| armory (39) | Rogue | 11/12 | 1 | 0 | 7 815 | 1/12 | 28 305 | 24 030 |
| armory (39) | Cat | 11/12 | 1 | 0 | 8 306 | 1/12 | 24 932 | 24 889 |
| graveyard (33) | Rogue | 8/12 | 1 | 3 | 9 270 | 1/12 | 20 293 | 16 046 |
| graveyard (33) | Cat | 4/12 | 3 | 5 | 10 044 | 4/12 | 16 398 | 19 839 |
| library (36) | Rogue | 5/12 | 4 | 3 | 9 131 | 4/12 | 19 604 | 24 165 |
| library (36) | Cat | 6/12 | 3 | 3 | 9 324 | 3/12 | 16 851 | 25 193 |

The Rogue holds the slot. Armory is a tie on outcomes with the Rogue ahead on
speed (6%) and damage (12%). Graveyard is decisive: the cat dies 4 times to the
Rogue's 1, wipes 3 to 1, clears 4 to 8 — and draws 24% more Priest healing
(19.8k vs 16.0k), so it is both the weaker and the softer body. Library is the
cat's best wing and it still only edges outcomes (6/3 against 5/4) while clearing
6% slower. Caveat on the damage column: direct damage only, so the cat's Rake
and Rip bleed ticks are not counted — the true damage gap is smaller than shown,
but not small enough to flip any wing. The cat's case as a melee slot would need
what the fifth-seat trial also pointed at: it must stop needing the Priest.

### 4b. Moonkin measured — and the Druid-only party answered

The Moonkin needed no code at all: kit, talents (the operator's `oCaYYKFYJLDAB--FAAoAC`),
form, Dps role and the `"Druid (Moonkin)"` mapping (`Content.fs:6251`) were already landed —
`rangedOrders` serves it through the same mana-budget gate as the Mage and Warlock.
`sim seat 12 <wing> --class "Druid (Moonkin)"` against the same-seat controls:

| wing | moonkin (12 seeds) | cat | tree control |
|---|---|---|---|
| armory | 11/1/0 clear/wipe/unresolved, 1 death, 28 083 dmg, priest 25 861, 8 252 ticks | 12/0/0, 0, 24 617, priest 28 735, 8 196 | 12/0/0, 0, 4 389, priest 13 068, 9 215 |
| graveyard | 8/2/2, 2 deaths, 15 253, priest 18 758, 9 940 ticks | 7/4/1, 5, 19 088, priest 22 992, 9 693 | 11/0/1, 0, 3 772, priest 9 299, 10 360 |
| library | **8/0/4, 0 deaths, 19 381, priest 24 344, 9 379 ticks** | 4/8/0, 8, 15 194, priest 25 620, 9 708 | 8/0/4, 0, 2 226, priest 13 149, 10 284 |

**The Moonkin is the best fifth seat measured.** At library it exactly matches the tree
control's outcomes (8/0/4, zero deaths) while carrying 8.7× its damage; against the cat it
is strictly better there (0 deaths to 8, no wipes) and comparable at armory and graveyard.
It clears 4–10% faster than the tree everywhere. And its numbers are honest direct damage
— the cat's exclude bleed ticks, so the true gap is wider than shown. Moonfire barely fires
(~800/seed): the lane prefers Wrath on mana efficiency, which is the right call for a body
with no movement pressure. The Moonkin is not a gimmick; the cat is the weaker seat.

**The Druid-only party is the gimmick.** `sim droods [n]` (new instrument) runs
`["Druid (Bear)"; "Druid"; "Druid (Cat)"; "Druid (Moonkin)"; "Druid"]` — bear tank, tree
healer, cat melee, moonkin ranged, second tree — over the three wings, 12 seeds each:

| wing | cleared | wiped | unresolved | party dmg/seed | party heal/seed |
|---|---|---|---|---|---|
| graveyard | 0 | 2 | 10 | 66 562 | 34 562 |
| library | 0 | 6 | 6 | 66 181 | 42 237 |
| armory | 0 | 4 | 8 | 104 796 | 43 365 |

**Still 0 for 36 — but the failure changed mechanism.** The operator ruled the kit
itself incomplete: vanilla bears carry **Bash** and **Feral Charge**, and the sim's bear
had neither. Both are now landed — `bashRanks` (`Content.fs:433`, L10/32/44, 2/3/4 s,
`Interrupt`+`Stun`, 10 rage, 60 s) and `feralCharge` (`:456`, L20, 5 rage, 15 s, range 25,
`Interrupt` only — trained with a `shortcut:` comment because `bearTalents` grants no
abilities). The tank's caster branch is generalized from Shield-Bash-by-name to
`assignedKick` (`PartyAi.fs:173`), the cheapest ready interrupt on an assigned cast; the
generalization had to exclude `Charge`/`Intercept` by effect (`:183`) after the bench
drifted (seed 1, 771→808 ticks) by promoting the Warrior's Intercept to fallback kick.

And the measurement is clean: a graveyard seed shows **18 landed kicks, 0 failed, and not
one hostile heal in the entire log** — the bear breaks every Mana Burn, Fireball and
Flame Spike it is assigned. The Druid-only party's residual killer is **pack damage on a
leather tank**: a Scarlet Sentry cleaving the bear for 107 ×2 while five hostiles grind
it down. Not un-broken casts — tank survivability and throughput.

**Operator ruling (post-measurement): the Druid-only party is not a balance target.**
It stays a gimmick measurement — interesting, closed, not something to fix. The
feature-tier open question above (armor/leather-vs-plate modeling for the bear) is
descoped with it. Of the Druid specs, **only the Tree/healer may appear in a standard
roster rotation**; the cat and moonkin fifth-seat comparisons above are measurements,
not balance targets either.

### 4c. Rogue escapes — Vanish and Blind, and what they did to the melee A/B

The melee A/B gave the Rogue the slot but also showed its bill: 1/12 deaths at armory and
graveyard, 4/12 at library, every death travelling with a wipe. The kit's answer in the
source game is two buttons the sim did not have, so both were added and taught, and the
A/B was re-run to see if the deaths move.

**Vanish** (`Content.fs`, beside Sap ~:1549): instant, Cost 0 (vanilla spends a reagent,
not Energy — the sim models no reagents), 5-min cooldown, Level 22. It adds a stealth
mechanic the codebase had none of: a new `Stealthed` state beside `Sleeping`/`Stunned`
(`Domain.fs:258`, decayed at `:486-491`), threat entries wiped from every hostile's
`Threat` map on cast, `SimState.resolveTarget` (`SimState.fs:247-280`) unable to pick a
stealthed member, broken by the Rogue's own next damage. One deviation is commented in
place: world-level `SimState.inCombat` (`SimState.fs:83`) is any-hostile-engaged, so an
individual cannot unilaterally "leave combat" — threat-wipe plus untargetable-while-stealthed
is the sim's equivalent. **Blind**: `Sleep(secTicks 10.0)` (the Sap effect, breaks on damage),
instant, Cost 0, 3-min cooldown, Level 34, range 3 — deliberately *not* tagged `Interrupt`, so
the interrupt-assignment system (`PartyAi.fs:113-145`) cannot draft it as a kick, and the
level gate means the level-33 graveyard wing does without it, as in vanilla training.

**AI** (`PartyAi.fs` `meleeOrders` ~:1731, Rogue's lane only — the cat's lane is untouched):
below 25% hp *and* actually targeted, prefer Vanish, else Blind the attacker; while stealthed
the lane retreats instead of attacking, and both abilities are excluded from the rotation
pickers (`~:231/240`, `~:1838/1842`) and the pre-pull opener list (`~:1918`) so they are never
burned as filler. Five tests: vanish drops targeting and threat, stealth target-skip, blind
sleeps an in-combat attacker and breaks on damage, the low-hp behaviour test, and the kit
contract.

**Re-measured** (`sim seat 12 <wing> --melee`, same seeds, escape counters added to the
instrument — `escape buttons: vanish N, blind M casts across all seeds`, counted from the
seat's own log lines):

| wing | armory (39) | graveyard (33) | library (36) |
|---|---|---|---|
| outcomes (clear/wipe/unresolved) | 11/1/0 — unchanged | 8/1/3 — unchanged | 5/4/3 — unchanged |
| mean clear ticks | 7 815 → 7 986 | 9 270 → 9 258 | 9 131 → 9 227 |
| seat dmg/seed | 28 305 → 28 011 | 20 293 → 20 166 | 19 604 → 19 977 |
| priest heal/seed | 24 030 → 23 954 | 16 046 → 15 997 | 24 165 → 24 312 |
| vanish casts / 12 seeds | 1 | 1 | 4 |

The honest verdict: **the buttons work and barely matter**. Vanish fired once at armory,
once at graveyard, four times at library — exactly the wings where the Rogue was dying —
and the outcomes did not move. The single armory death and the four library deaths happened
anyway; the escapes bought at most a retargeting pause, and with one healer and threat
flowing to the Warrior, the party could not be saved by the Rogue stepping out of it. Damage,
speed and healing all sit inside seed noise (±2%). The conclusion for the cat comparison is
unchanged — the Rogue holds the slot — and the conclusion for the deaths is the one the
fifth-seat table already carried: one healer is the binding constraint, not the melee body.

One measurement caveat: `blind` is counted from the same log line as Sap (`"%s puts %s to
sleep"`), so the blind column conflates the two sleeps; with Sap pre-pull only, a nonzero
in-run count is Blind, but the two cannot be told apart numerically as the log stands.

### 5. Two latent bugs found on the way

- **Pathing invariant.** `no two living entities share a tile in a generated dungeon`
  failed. Root cause `SimTick.fs:438-445`: the `held` set equated *asking to step* with
  *vacating*, so a body whose step was refused, out-raced, or `canMove = false` still stood
  on its tile while absent from `held`, and the next mover walked onto it. It fires on
  **19 of the first 60 dungeon seeds on the untouched baseline**, all at `(58,5)` tick 28 —
  latent, not caused by any recent change. Fixed with a monotone fixpoint `settleHeld`
  (`SimTick.fs:480-538`), a trade guard so a swap cannot land on a third body
  (`SimTick.fs:402`), and strict `occupiedByOther` (`SimState.fs:155`) for the two places
  that write position outright — Charge/Intercept and summon placement
  (`SimCombat.fs:1557`, `:1696`).
- **The drift test was giving false greens.** `snapshotPath ()`
  (`tests/WowRoguelike.Tests/Tests.fs:7510`) resolved from the working directory — which is
  `bin/Release/net10.0/` — so `UPDATE_SNAPSHOTS=1` wrote the build artifact and the next
  `dotnet test` copied the stale repo file back over it. It now walks out of `bin/` to the
  project directory and uses the repo file as the single source of truth. Two earlier
  "refreshed, green" claims in this session were false because of it; the baseline now
  carries the genuine post-budget value (seed 1 `ticks=771`).

## Harness diagnostics added

`sim sheet [--level L]` · `sim activity [n] [--level L]` · `sim healers [n]` ·
`sim ttk [n]` · `sim comps` · `sim snapshots` · `druidspend [n]` · `drmana [n]` ·
`sim interrupts [n] [--class C]` · `sim totems [n]` ·
`sim seat [n] [wing] [--class C] [--melee]` — the fifth seat over many seeds
(outcomes, clear speed, seat deaths, seat damage against the bare-`Druid` control
in the same seat), or with `--melee` the fourth slot against the Rogue control
with a Warlock behind it ·
`sim droods [n]` — a Druid-only party (bear/tree/cat/moonkin/tree), outcomes and
party totals per wing; all five seats log as "Druid", so no per-seat split is
possible by name ·
`sim attrib [wing] [diff] [maxTicks] --class C` (now with `outcome=` — cleared / wiped /
tick-limit, which `attrib` alone never used to tell you). Wired
in `src/WowRoguelike.Harness/Program.fs`; `sim ttk` prints the forecast inputs and the
gate's verdict per sampled tick and is the tool to reach for before touching budget
constants, and `sim interrupts` is the only way to see control: a kick costs a damage
button and buys nothing visible in a healing ledger, so without it a pass that teaches a
class to interrupt looks exactly like one that does not. It reports the broken cast's own
nominal size, not a health total — the roll, the crit and the mitigation are all unknown at
the moment of the break. `sim totems` reads the `TotemLedger` (`Domain.fs:2213`) that
`advanceTotems` books every pulse into — drops, pulses, deficit-capped HP and mana
delivered, and reach per run; it is the only instrument that can see totem work, because
totem pulses are silent in the combat log.

`attrib --class C` seats C in the fifth slot, which is how a class's damage gets split by
the spelling that produced it. Without the flag the default roster runs and it has no
Shaman seat, so the by-spell question was previously unanswerable. It is one seed: use it
for shares, and `shmana` / `activity` for rates. **The class string takes spec suffixes**:
`--class "Druid (Cat)"` seats the cat (bare `Druid` maps to `TreeDruid` in `heroOf`,
`Content.fs:6220`) — that is how the §4 cat table was taken.

`sim comps` runs fixed rosters only, so it **cannot** seat a cat or any spec variant —
attrib with a `--class` spec string is the instrument for that. `comps` is also **slow**:
25 seeds × 3 maps × 4 swap rounds did not finish in 40 minutes. Budget an hour or pass a
smaller seed count and say so.

## Traps

- **9 seeds is inside the noise.** One 94-mana Wrath moved a Druid's hps by 3.45%. Use 30.
- **Every seed re-rolls when decisions change.** Bench tick-count drift across a decision
  change includes a different RNG timeline, not just the mechanical effect. Do not claim a
  fight got faster without a fixed-seed A/B.
- **Compare within one tree.** Mid-campaign numbers in this session do not reproduce across
  tree states; the worker that redid the A/B inside a single tree produced the only table
  worth trusting (reproduced above).
- **Never refresh the baseline over a failing behaviour test.** The snapshot test is
  expected to drift; a red invariant test is not.
- FsCheck property tests are randomised — run an invariant several times, not once.
- **`dotnet build` can report 0 errors on a changed file without compiling it** (seen
  2026-11-18: root build said "0 errors" while the Tests DLL sat 38 minutes stale; the
  new test was neither compiled nor run). After touching a test file, rebuild with
  `--no-incremental` or confirm the run's `Total:` count moved — a green that did not
  change the count is a false green.
- The 5-second rule and the 2 s server tick need no second implementation: the net-drain
  identity cancels spend out.

## Remaining work, ordered

1. **Get four figures from the operator** (blocking, they exist nowhere in the code or the
   calculator's tooltips — they are tree prose): Omen of Clarity proc chance; Furor Energy
   on shapeshift; Open Wounds' Rip and Claw percentages; Tiger's Fury's Energy-over-time
   (its row is absent entirely).
2. **Six Feral talents are blocked on model axes, not numbers** — building code paths, not
   transcription, and worthless to ask the operator for values first: Leader of the Pack
   (`PartyBuffKind` has no crit entry), Heart of the Wild (`Talents.StrengthBp` does not
   exist), Ancient Brutality (no bleed-tick → resource hook), Primal Fury
   (`ProcResult.GrantResource` pays Rage only), Carnage/Open Wounds (nothing reads the
   target's auras for these), Furor (nothing pays on `TakeStance`).
3. **The DoT refresh rule** — carried from the previous handoff, untouched.
4. **The Shaman's residual Imp cost** — 0.81 healing per point, the worst row left. It needs
   a heal cheaper than rank 4, which the 1–60 ladder does not have; this is a data question
   for the operator, not a code one.
5. **Headline cat-DPS number — taken; both durability trials also taken.** The
   instruments: `sim attrib <wing> median 12000 --class "Druid (Cat)"` (one seed,
   §4 table 1), `sim seat 12 <wing> [--class C]` (the fifth seat, §4 table 2), and
   `sim seat 12 <wing> --melee [--class C]` (the melee slot, §4 table 3 — cat vs
   Rogue with support held identical). Verdicts: the cat clears 6–11% faster
   wherever it survives but dies unsupported (0 wipes and 0 deaths for the tree
   control anywhere; the cat seat wipes 8/12 library and 4/12 graveyard); against
   the Rogue in the same slot the cat loses the slot outright — decisive at
   graveyard, tied-at-best elsewhere, always slower, always softer (it draws
   24% more Priest healing there). What would make the cat a real seat — a
   self-heal lever the kit permits, or cutting its healer draw — was not
   investigated; the measurement closes here.
6. Optional: if Shaman damage is wanted back, the lever is still pool or regen — but pass 3
   showed the cheap win was the *spell choice*, not the budget: the lane shed a third of its
   mana for a sixth of its damage because it was pressing a 0.79-point instant over a
   1.41-point cast it could stand still for. Do not loosen `marginHeals` / `drainMargin` —
   they are shared across classes.
7. **Purge is now 8.4% of the Shaman's spend** (360 casts, 32 382 mana at library, up from
   6.9%) because the damage lane no longer absorbs the income. Whether an offensive dispel
   is worth a twelfth of the pool was never asked and is not answered here.
8. **The interrupt assignment rule** gives each cast to the cheapest kick that can land, so
   a range-5, 100-mana shock loses to a melee kick on every pack a Warrior or Rogue is
   touching. That is why the Shaman still takes only 3% of breaks. If Earth Shock should
   take more, change `PartyAi.fs:113`, and re-run `sim interrupts 12`.
9. **The kick's *identity* is now observable; its *cost* still is not.** The break line at
   `SimCombat.fs:1185` carries the interrupting ability (`"%s's %s interrupts %s's %s"`),
   `sim interrupts` counts who broke what with which spell, and the measurement is in — the
   Shaman's 18 breaks over 12 seeds are all Earth Shock. What remains open is the ledger
   half: `sim shmana` prices the *damage lane*, and pass 3 deliberately moved Earth Shock out
   of it into its own row, so the kick's 100 mana appears in no per-spell cost table. If
   that figure is wanted, the lever is a kick row in the damage ledger.
10. **A comment cites a test that does not exist.** `PartyAi.fs:162` names
    `every class that holds a kick acts on the interrupt it is assigned`; the actual test is
    `a cast the party holds a kick for is broken exactly once` (`Tests.fs:6075`). The contract
    is enforced; the citation is wrong and will rot. Fix the comment, not the test.
11. **Vanish/Blind follow-ups, cheap and optional**: (a) the blind cast count is conflated
    with Sap — the log line `"%s puts %s to sleep"` is shared (`SimCombat.fs` sleep path);
    split it (`"%s blinds %s"`) if the count ever matters; (b) the escape triggers fired
    1/1/4 times over 12 seeds each — if the deaths are ever worth chasing, the lever is the
    threshold (25% hp) or what the stealthed Rogue does with its 30 s, not more buttons.

Do not re-open the Druid's idle share. It is `covered`, not wasted — 42.3 of its 51.8 idle
points are the party being safe while HoTs tick, with `unmet` at 0.1. The operator ruled
this expected and the metric now proves them right.

## Verification

```
dotnet build src/WowRoguelike.Core -c Release
dotnet test tests/WowRoguelike.Tests -c Release            # expect 360 passed, 0 failed
UPDATE_SNAPSHOTS=1 dotnet test tests/WowRoguelike.Tests -c Release \
  --filter "bench snapshot has not drifted"                 # only when drift is intended
dotnet run --project src/WowRoguelike.Harness -c Release -- sim healers 30
dotnet run --project src/WowRoguelike.Harness -c Release -- sim ttk 9
```

## Suggested skills

Call the Skill tool for these before starting:

- **`openlore-orient`** — run it before touching `SimCombat.fs`, `PartyAi.fs` or
  `SimState.fs`. These are large files with dense cross-references and the call graph
  matters more than reading order.
- **`tdd`** — every mechanic in this repo is landed with a behaviour test, and the
  existing tests are the only reliable specification of intent. Write the failing test
  first for any new talent axis.
- **`diagnosing-bugs`** — for the remaining Feral axes and anything in the budget/forecast.
  This session's two real wins came from reproducing an invariant violation before
  explaining it, not from reasoning about the code.
- **`code-review`** — before committing any of the above; the tree is 5.8 k lines dirty and
  carries unrelated pre-existing changes.
- **`writing-for-agents`** — only if `docs/agents/handoff.md` or `domain.md` are being
  rewritten rather than extended.
