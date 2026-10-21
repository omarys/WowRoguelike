# Research: level-60 ability rosters expressed at level 24 (BiS-24 gear, level-24 mobs)

Read-only research pass. All numbers taken from files on disk unless a URL is given.
Nothing was changed in `src/`, `spec/` or any data file.

**Citation convention.** Every number below cites the file on disk and the *row key* inside that file's
TSV table (for example `ranks/warrior.md` §Table, row `Whirlwind 1`). Line numbers are given as an
approximate band (`L~55–190`) because the read tool returned unnumbered slices; the row key is the
authoritative locator and is greppable verbatim. Web values carry a URL.

## 1. The scaling rule, and the one place it collapses

Agreed rule: a spell that has a rank at or below 24 uses that rank. A spell whose **first** rank is above
24 keeps its damage-per-mana (heal-per-mana) efficiency, with cost *and* output scaled by

```
s = (level-24 resource pool) / (level-60 resource pool)
```

so the cost is the same fraction of the local pool that the first rank is of a 60 pool. Because cost and
output scale by the same factor, efficiency is invariant by construction.

**Resource pools.** `base mana` per level from `warcraft.wiki.gg/wiki/Base_mana` agrees exactly with the
druid figures already on disk (`ranks/druid.md`, §Missing evidence: "lvl 10=149 … 24=449 … 60=1244"),
which is the cross-check that makes that table usable:

`total_mana = base_mana + min(20, Int) + 15 × max(0, Int − 20)` (`ranks/mage.md` §Level-60 mana pool).

| class | level-24 pool | source | level-60 pool | source | s |
|---|---|---|---|---|---|
| Warrior | 100 rage (cap) | `ranks/warrior.md` §Table, row `Rage`… rage cap 100 is level-invariant — cap not on disk, **see note A** | 100 rage | same | **1.00** |
| Rogue | 100 energy | `ranks/rogue.md` §Table, row `Energy 1` (`Max 100; Regen 20/2s`) | 100 energy | same row | **1.00** |
| Mage | 1998 mana | `level24/mage.md` §5 (base 463 + Int 121) | 8413 mana | `ranks/mage.md` §Level-60 mana pool (base 1213, Int 500) | **0.2375** |
| Priest | 1760 mana (derived: base 480 + Int 104) | base mana 480 @24 — `warcraft.wiki.gg/wiki/Base_mana`; gear +52 Int — `level24/priest.md` summary | ≈7030 mana (**estimate**: base ≈1300, Int 400) | base 60 **UNVERIFIED**; Int 400 is an **assumption** | **0.250** |
| Druid | ≈1630 mana (derived: base 449 + Int ≈96 est.) | base 449 — `ranks/druid.md` §Missing evidence | ≈6960 mana (**estimate**: base 1244, Int 400) | `ranks/druid.md` (base); Int 400 assumed | **0.234** |

**Note A — the rule is trivial for Warrior and Rogue.** Rage and energy are caps of 100 at level 24 and at
60; only *generation rates* differ with level, not the pool. So `s = 1` and every first-rank rage/energy
cost and magnitude is already the correct at-24 value. Every warrior and rogue row below is therefore
"first rank, unchanged" — this is the single largest simplification in the whole exercise and it is a
direct consequence of the agreed rule, not a fudge.

**Percentage-of-base-mana costs need no scaling at all** — they are already level-relative. This applies to
Druid Abolish Poison (16 %), Barkskin (15 %), Rebirth (85 %), Revive (75 %), and Priest Abolish Disease
(15 %). Arithmetic check on the druid rows: `0.16 × 509 (level-26 base) = 81` matches the 81 mana printed
for Abolish Poison r1 in `ranks/druid.md` §Table row `Abolish Poison 1`, and `0.15 × 449 = 67` is the
correct at-24 figure for Barkskin. So the on-disk absolute numbers are level-26/level-44 snapshots of a
percentage, and the at-24 value is the same percentage of 449.

**Sensitivity of s (casters).** Both pools are dominated by the Int term, so `s ≈ (Int24−20)/(Int60−20)`.
If level-60 BiS Int is 300 instead of 400 the priest factor rises to ≈0.32; at 500 it falls to ≈0.21. The
mage factor is the only one with *both* endpoints sourced, so it is the calibration point: a two-thirds
drop in pool size. Confidence: mage high, priest/druid medium-low (see §6).

## 2. Warrior — s = 1.00 (rage pool identical)

Available at 24: Charge r1 (4, 15 s, 8–25 yd), Hamstring r1 (8, 10 rage), Heroic Strike r4 (24, 15 rage,
100 %+44), Rend r3 (20, 10 rage, 45/15 s), Thunder Clap r2 (18, 20 rage, 23 dmg), Overpower r1 (12,
5 rage, 100 %+5), Cleave r1 (20, 20 rage), Execute r1 (24, 15 rage, 125 +3/rage), Mocking Blow r1 (16,
10 rage, 120 s), Intimidating Shout r1 (22, 25 rage, 180 s), Retaliation (20), Shield Bash r1 (12,
10 rage), Shield Block r1 (16, 10 rage), Revenge r2 (24, 5 rage, 18–22), Sunder Armor r2 (22, 15 rage),
Taunt (10), Disarm (18, 20 rage), Battle Shout r3 (22), Demoralizing Shout r2 (24), Bloodrage (10),
Dual Wield (20), Defensive Stance (10). Source: `ranks/warrior.md` §Table, rows `Heroic Strike 4`,
`Execute 1`, `Revenge 2`, `Sunder Armor 2`, `Demoralizing Shout 2`, `Charge 1`, … (L~55–190).

| ability | first rank lvl | rank ≤24? | at-24 numbers | scaling arithmetic |
|---|---|---|---|---|
| Slam | 30 | **no** | *no at-24 rank* — first rank 15 rage, 1.5 s, 100 %+32 | s=1 → 15 rage / +32 unchanged. Slam's learn level is settled at **30** (`ranks/warrior.md` §Contradictions 1) |
| Whirlwind | 36 | **no** | *no at-24 rank* — 25 rage, 10 s CD, up to 4 targets in 8 yd, 100 % weapon | s=1 → 25 rage unchanged. 25/100 = 25 % of pool at 24 and at 60 |
| Intercept | 30 | **no** | 10 rage, 30 s CD, 25 yd, 25 dmg + 3 s stun | s=1 → unchanged |
| Pummel | 38 | **no** | 10 rage, 10 s CD, 20 dmg, 4 s school lockout | s=1 → unchanged |
| Challenging Shout | 26 | **no** | 5 rage, 600 s CD, 10 yd AoE taunt 6 s | s=1 → unchanged |
| Shield Wall | 28 | **no** | 0 rage, 1800 s CD, −75 % damage taken 10 s, needs shield | s=1 → unchanged |
| Berserker Rage | 32 | **no** | 0 rage, 30 s CD, fear immunity 10 s | s=1 → unchanged |
| Berserker Stance | 30 | **no** | 0 rage, +3 % crit, +10 % damage taken, −20 % threat | s=1 → unchanged |
| Recklessness | 50 | **no** | 0 rage, 1800 s CD, 15 s | s=1 → unchanged, but 15 s of "most attacks crit" is a 60-tier payoff at 24 — flag (§4.1) |

## 3. Rogue — s = 1.00 (energy pool identical)

Available at 24: Sinister Strike r4 (22), Backstab r3 (20), Eviscerate r4 (24, 35 energy), Slice and Dice
r1 (10, 25), Expose Armor r1 (14, 25), Garrote r2 (22, 50), Ambush r1 (18, 60), Rupture r1 (20, 25),
Sap r1 (10, 65), Kick r1 (12, 25), Gouge r2 (18, 45), Feint r1 (16, 20), Evasion (8), Sprint r1 (10),
Vanish (22), Distract (22), Stealth r2 (20), Pick Lock (16), Pick Pocket (4), Detect Traps (24).
Source: `ranks/rogue.md` §Table, rows `Energy 1`, `Eviscerate 4`, `Ambush 1`, `Kick 1`, … (L~42–160).

| ability | first rank lvl | rank ≤24? | at-24 numbers | scaling arithmetic |
|---|---|---|---|---|
| Cheap Shot | 26 | **no** | 60 energy, stealth, 4 s stun, 2 combo points, no damage | s=1 → 60 energy. 60/100 = 60 % of pool at both levels |
| Kidney Shot | 30 | **no** | 25 energy, 20 s CD, stun 1/2/3/4/5 s by combo point | s=1 → unchanged (stun duration is not a resource magnitude) |
| Blind | 34 | **no** | 30 energy, 10 yd, 10 s disorient, reagent Blinding Powder, CD **U** | s=1 → 30 energy; the missing CD is a disk gap (`ranks/rogue.md` §Table row `Blind 1`, confidence `I`) |
| Disarm Trap | 30 | **no** | 0 energy, 2 s cast, stealth-only utility | s=1 → unchanged |
| Blade Flurry | talent, no ranks | **no** | — | see §4.2 |

## 4. Mage — s = 0.2375 (1998 → 8413)

Available at 24: Fireball r≤5 and Frostbolt r≤4 (**numbers in `level24/mage.md`, not re-read this pass —
UNVERIFIED here; rank availability is what matters and both have a ≤24 rank**), Arcane Missiles r3 (24,
235 mana/5 s/280), Arcane Explosion r2 (22, 120/60–66), Blizzard r1 (20, 320/208 over 8 s), Flamestrike r2
(24, 330), Scorch r1 (22, 50/56–69), Fire Blast r3 (22, 115/110–134), Frost Nova r1 (10, 55), Polymorph
r2 (20), Blink (20, 425), Mana Shield r1 (20, 40/absorb 120), Dampen Magic r2 (24), Amplify Magic r1 (18),
Remove Lesser Curse (18), Frost Armor r3 (20), Fire Ward r1 (20), Frost Ward r1 (22), Slow Fall (12),
Detect Magic (16), Arcane Intellect r2 (14), Conjure Water r3 (20), Conjure Food r2 (12). Source:
`ranks/mage.md` §Table (L~45–200).

| ability | first rank lvl | rank ≤24? | at-24 numbers | first-rank numbers → scaled to the 24 budget |
|---|---|---|---|---|
| Cone of Cold | 26 | **no** | *no at-24 rank* | r1: 210 mana, 98–108 dmg, instant, 10 s CD, 10 yd, −50 % slow 8 s. **`210 × 0.2375 = 50 mana`; `98–108 × 0.2375 = 23–26 dmg`** (same 10 s CD / 10 yd / slow) |
| Mage Armor | 34 | **no** | *no at-24 rank* | r1: 270 mana, +5 all res, 30 % regen while casting, 1800 s. **`270 × 0.2375 = 64 mana`**; the +5 res / 30 % regen are 60-tier magnitudes and do not scale (§5.1) |
| Teleport: Darnassus / Thunder Bluff | 30 | **no** | *no at-24 rank* | 120 mana, 10 s cast. **`120 × 0.2375 = 29 mana`** |
| Portal: any | 40 (Darnassus/TB 50) | **no** | *no at-24 rank* | 850 mana, 10 s cast, 60 s CD, 10 yd. **`850 × 0.2375 = 202 mana`** |
| Conjure Mana Jade | 38 | **no** | *no at-24 rank* | 800 mana, 3 s, makes a gem restoring 550–650. **`800 × 0.2375 = 190 mana`**; the gem restore must also scale or it is a free mana engine: **`550–650 × 0.2375 = 131–154`** (§5.2) |
| Arcane Brilliance | 56 | **no** | *no at-24 rank* | 3400 mana, party +31 Int 3600 s, 40 yd. **`3400 × 0.2375 = 808 mana`** = 40 % of the 1998 pool — arithmetically correct, practically awful at 24 (§5.1) |
| Ice Armor | 30 | **no** | *no at-24 rank* | cost and armour **U** on disk (`ranks/mage.md` §Table row `Ice Armor 1`, confidence `U`; tooltip id 10219 returned empty twice). Scaled value therefore **UNVERIFIED** |
| Fireball / Frostbolt / everything else | ≤24 | **yes** | use the at-24 rank as listed above | no scaling needed |

## 5. Priest — s = 0.250 (1760 → ≈7030)

Available at 24: Lesser Heal r3 (135–158 / 75 mana / 2.5 s), Heal r2 (429–492 / 205 / 3.0 s), Renew r3
(175 over 15 s / 105), Smite r4 (91–106 / 95 / 2.5 s), Shadow Word: Pain r3 (132 over 18 s / 95), Power
Word: Shield r≤4, Flash Heal r1 (20), Fade (20), Psychic Scream (14), Dispel Magic (18), Resurrection r1
(10), Inner Fire (12), Holy Fire r1 (20), Shackle Undead r1 (20), Mind Control, Mind Vision (22), Cure
Disease (14), Mana Burn n/a. Source: `level24/priest.md` summary bullet "Spells at 24"; rank ladders in
`ranks/priest.md` (its 13-column TSV payload was **not locatable on disk** — see §7).

| ability | first rank lvl | rank ≤24? | at-24 numbers | first-rank numbers → scaled to the 24 budget |
|---|---|---|---|---|
| Prayer of Healing | 30 | **no** | *no at-24 rank* | r1 (id 596): 410 mana, 3 s cast, 30 yd, heals **party** 312–333. **`410 × 0.25 = 103 mana`; `312–333 × 0.25 = 78–83 per party member`**. Full-party (5) throughput 390–415 for 103 mana is healthy; the per-target heal is below Lesser Heal r3 (135–158) — flag §5.3 |
| Shadow Protection | 30 | **no** | *no at-24 rank* | r1 (id 976): 250 mana, instant, 30 yd, +30 shadow res 10 min. **`250 × 0.25 = 63 mana`**; the +30 res does not scale and is a 60-tier magnitude (§5.1) |
| Abolish Disease | 32 | **no** | *no at-24 rank* | id 552: **15 % of base mana** → 72 mana at 24. **No scaling needed** — percentage costs are level-relative |
| Levitate | 34 | **no** | *no at-24 rank* | id 1706: 100 mana, reagent Light Feather, 2 min. **`100 × 0.25 = 25 mana`** |
| Prayer of Fortitude | 48 | **no** | *no at-24 rank* | r1 (id 21562): 2600 mana, instant, 40 yd, party +43 Sta 1 h, reagent Holy Candle. **`2600 × 0.25 = 650 mana`** = 37 % of pool; the +43 Sta is a 60-tier magnitude (§5.1) |
| Divine Spirit, Holy Nova, Inner Focus, Power Infusion, Lightwell, Desperate Prayer, Fear Ward | talent / racial | **no** | — | see §6 |

## 6. Druid — s = 0.234 for mana (base 449 → 1244 + Int), s = 1.00 for rage/energy

Available at 24: Wrath r4 (22), Starfire r1 (20), Moonfire r4 (22), Rejuvenation r4 (22), Healing Touch
r≤4, Regrowth r3 (24), Thorns r3 (24), Faerie Fire r1 (18), Entangling Roots r2 (18), Hibernate r1 (18),
Remove Curse (24, 45 mana = 10 % base), Cure Poison (14), Rebirth r1 (20, 301 = 85 % of level-20 base),
Revive r2 (24), Nature's Grasp r2 (18), Soothe Animal r1 (22), Bear Form (10), Aquatic Form (16), Cat Form
(20), Maul r2 (18), Swipe r2 (24), Enrage (12), Growl (10), Demoralizing Roar r2 (20), Bash r1 (14),
Claw r1 (20), Shred r1 (22), Rip r1 (20), Rake r1 (24), Prowl r1 (20), Tiger's Fury r1 (24), Teleport:
Moonglade (10). Source: `ranks/druid.md` §Table (L~40–190).

| ability | first rank lvl | rank ≤24? | at-24 numbers | first-rank numbers → scaled to the 24 budget |
|---|---|---|---|---|
| Abolish Poison | 26 | **no** | *no at-24 rank* | 16 % of base mana → **`0.16 × 449 = 72 mana`**. No scaling (percentage cost). Disk prints 81 because it is 16 % of the level-26 base (509) |
| Barkskin | 44 | **no** | *no at-24 rank* | 15 % of base mana → **`0.15 × 449 = 67 mana`**, 60 s CD, 20 % physical DR 15 s. No scaling |
| Tranquility | 30 | **no** | *no at-24 rank* | r1: 375 mana, 10 s channel, 300 s CD, party HoT 485 over 10 s. **`375 × 0.234 = 88 mana`; `485 × 0.234 = 113 party heal over 10 s`**. Solo this is 1.3 heal/mana — only sane with 3+ party members (§5.4) |
| Travel Form | 30 | **no** | *no at-24 rank* | 123 mana, +40 % outdoor speed. **`123 × 0.234 = 29 mana`** |
| Dire Bear Form | 40 | **no** | *no at-24 rank* | 470 mana, +120 AP / +360 % armour (1.12 values). **`470 × 0.234 = 110 mana`**; form multipliers do not scale |
| Hurricane | 40 | **no** | *no at-24 rank* | r1: 880 mana, 10 s channel, 60 s CD, 71 dmg/s per target. **`880 × 0.234 = 206 mana`; `71 × 0.234 = 17 dmg/s` → 170 per target over the channel**. Per-second damage drops **below the 20-damage floor flagged in the task** — see §5.5 |
| Gift of the Wild | 50 | **no** | *no at-24 rank* | r1: 900 mana, party +240 armour / +10 attrs / +15 res, 3600 s, 40 yd. **`900 × 0.234 = 211 mana`**; the buff magnitudes are 60-tier (§5.1) |
| Challenging Roar | 28 | **no** | *no at-24 rank* | 15 **rage**, 600 s CD, AoE taunt 6 s | s=1 (rage pool identical) → **15 rage, unchanged** |
| Faerie Fire (Feral) | 30 | **no** | *no at-24 rank* | 0 mana (free), 6 s CD, magnitude **U** on disk | s=1 → free; magnitude stays UNVERIFIED |
| Pounce | 36 | **no** | *no at-24 rank* | 50 **energy**, prowling+behind, 2 s stun, 90 bleed/18 s, 1 cp | s=1 → unchanged |
| Ravage | 32 | **no** | *no at-24 rank* | 60 **energy**, 350 %+147, prowling+behind | s=1 → unchanged |
| Ferocious Bite | 32 | **no** | *no at-24 rank* | 35 **energy**, 50–66 … 194–210 by combo point | s=1 → unchanged |
| Cower | 28 | **no** | *no at-24 rank* | 20 **energy**, 10 s CD, threat −small | s=1 → unchanged; magnitude **U** |
| Dash / Track Humanoids / Feline Grace | 26 / 32 / 40 | **no** | *no at-24 rank* | 0 energy | s=1 → unchanged |
| Everything else (Wrath, Moonfire, Starfire, Healing Touch, Regrowth, Rejuvenation, Thorns, FF, roots, forms, Claw, Shred, Rake, Rip, Maul, Swipe, Bash, Growl, Demo Roar, Enrage, Prowl, Tiger's Fury, Nature's Grasp, Soothe Animal, Rebirth, Revive, Cure Poison, Remove Curse) | ≤24 | **yes** | use the at-24 rank as listed above | no scaling |

**Hunter, Warlock, Shaman, Paladin.** No local brief exists: `.pi/research/ranks/` and
`.pi/research/level24/` contain only warrior, mage, druid, rogue, priest (+ `base_stats.md`,
`missing_abilities.md`). No table is produced for those four classes; building one would require a fresh
per-rank research pass, not arithmetic on existing data.

## 5x. Where the rule produces something absurd (stated, not silently corrected)

**5.1 Unscalable magnitudes — buff and debuff numbers are 60-tier and the rule says nothing about them.**
Scaling only cost + damage/heal leaves every *magnitude* untouched, which at level 24 is enormous:
Priest Prayer of Fortitude **+43 Sta ≈ +430 HP** (roughly doubles a level-24 pool) for 650 mana;
Shadow Protection **+30 shadow res**; Mage Mage Armor **+5 all res / 30 % regen while casting**;
Druid Gift of the Wild **+240 armour / +10 attributes / +15 res** to the whole party for 211 mana;
Mage Arcane Brilliance **+31 Int** (≈ +465 mana to each recipient) for 808 mana.
*Proposed correction (inference, not evidence):* scale the magnitude by the same s and round down —
PoF → **+10 Sta**, Shadow Protection → **+7 shadow res**, Gift of the Wild → **+56 armour / +2 attrs**,
Arcane Brilliance → **+7 Int**. That keeps the ability's cost-to-value ratio at both tiers.

**5.2 Conjure Mana Jade is a net-loss item if both sides scale, and a free-mana exploit if only the cost
scales.** Cost 190 mana for a gem restoring 131–154 is a loss; cost 190 for 550–650 is a 3× profit per
conjure. Recommend: keep the cost scaled (190) and the restore scaled (131–154), i.e. the gem is
marginal at 24 — which mirrors its marginal role at 60 — rather than inventing a 24-only gem.

**5.3 Prayer of Healing's per-target heal (78–83) is below Lesser Heal r3 (135–158).** This is not an
error: PoH is a 5-target spell, so at 3+ targets it beats single-target healing on throughput. Flag for
the balance pass only if the sim AI evaluates heals per-target instead of per-cast.

**5.4 Tranquility (113 party heal over a 10 s channel for 88 mana) and Hurricane (17 dmg/s/target for
206 mana)** are both multi-target-only spells after scaling. Solo they are strictly worse than
Rejuvenation r4 (180 over 12 s for 105) and Wrath r4 (69–79 for 70). Acceptable if the sim models
multi-target value; broken-looking otherwise.

**5.5 Hurricane's 17 dmg/s is the one case that trips the "damage under 20" absurdity test** named in the
task. Options: (a) accept 17/s and rely on target count; (b) renormalise Hurricane to a *total* damage
budget instead of a per-second one (170 per target total is above the floor). Recommendation: (b) for the
sim's damage model, since the spell's cost is charged once for the whole channel.

**5.6 Warrior Recklessness / Shield Wall / Retaliation (0 rage, 30 min CD, 15 s of a 60-tier payoff)**
cannot be budgeted by a resource-fraction rule at all — they cost nothing. Their balance lever is the
cooldown, and a 30-minute cooldown in a level-24 dungeon run is effectively once-per-run. Not absurd, but
outside the rule's reach.

## 6. Talent-only abilities — no rank to scale, need invented first-rank equivalents

All are proposal-level design (labelled **inference**). Amounts for the unnamed ones are **UNVERIFIED**
on disk; the *shape* of each proposal is the recommendation.

| class | ability | why the rule cannot express it | proposed first-rank equivalent at 24 |
|---|---|---|---|
| Warrior | Mortal Strike | 31-pt Arms talent, no trainer rank; would otherwise arrive at 40 | 30 rage, 6 s CD, 100 % weapon + 65, **−50 % healing taken 10 s**. Damage chassis = Heroic Strike r4 (100 %+44) bumped to the first-rank 60 ratio, cost left at 30 rage (30 % of pool at both tiers) |
| Warrior | Bloodthirst | 31-pt Fury, no rank | 30 rage, 8 s CD, 100 % weapon damage, heal = 0.3 × (damage dealt) capped at 5 % max HP. Cost is pool-invariant so only the damage/heal chassis needs a 24-tier value |
| Warrior | Shield Slam | 31-pt Prot, **r1 at level 40** | 20 rage, 6 s CD, damage = **shield block value + 45**, dispels 1 magic effect. Rage pool is 100 at both tiers, so the cost is 20 rage unchanged; only the block-value term needs a level-24 shield from the warrior brief |
| Warrior | Sweeping Strikes, Piercing Howl, Death Wish, Last Stand, Concussion Blow | talents, no ranks | copy the nearest at-24 class analogue: Sweeping Strikes → 2 extra targets for 10 s on a 30 s CD; Piercing Howl → Hamstring-equivalent AoE slow (40 %/15 s); Death Wish → +20 % damage, −20 % armour, 30 s of a 3 min CD; Last Stand → +30 % max HP 20 s on a 10 min CD; Concussion Blow → 15 rage, 45 s CD, 5 s stun, no damage |
| Rogue | Blade Flurry | 31-pt Combat, no ranks | **25 energy, 2 min CD, +20 % attack speed 15 s, +1 extra target melee.** Energy is 100 at both tiers so the cost needs no scaling; the 20 % haste is a 60-tier magnitude and could be cut to +8 % under §5.1's rule |
| Rogue | Adrenaline Rush, Cold Blood, Preparation, Vigor, Hemorrhage, Ghostly Strike, Riposte | talents (Hemorrhage/Ghostly Strike/Riposte *do* have ranks; the rest do not) | Adrenaline Rush → 0 energy, 5 min CD, +100 % energy regen 15 s; Cold Blood → 0 energy, 3 min CD, next attack crits; Preparation → 0 energy, 10 min CD, resets Sprint/Evasion/Vanish/Blind/Cold Blood; Vigor → +10 max energy (100 → 110, a 10 % pool change, acceptable); Hemorrhage r0 → 35 energy, 100 %+3 damage, +7 bleed damage taken 15 s (cost from the rank-1 60 value, 35 energy, unchanged) |
| Mage | Cold Snap | 21-pt Frost, no cost, no damage, no ranks | **nothing to scale.** Keep as-is: 0 mana, 10 min CD, finishes Frost cooldowns. A 0-cost 0-damage ability is level-invariant; only *which* cooldowns exist at 24 changes (Blink, Frost Nova, Cone of Cold if granted) |
| Mage | Ice Barrier | 31-pt Frost talent with ranks; r1 at 30, absorb/cost **U** on disk | 0.2375 × r1 (which needs sourcing first). Provisionally: **~44 mana, absorbs ~180, 30 s** — i.e. ≈ 10 % of the level-24 HP+mana pool, matching the r1 share of the 60 pool. Marked UNVERIFIED until the r1 tooltip is read |
| Mage | Icy Veins, Arcane Power, Presence of Mind, Combustion, Pyroblast, Blast Wave | talents; Pyroblast/Blast Wave have ranks, the rest do not | Icy Veins → 0 mana, 3 min CD, −20 % cast time 20 s; Arcane Power → 0 mana, 3 min CD, +30 % damage / +30 % mana cost 15 s; Presence of Mind → 0 mana, 3 min CD, next cast instant; Combustion → 0 mana, 5 min CD, +10 % crit per fire hit stacking 3; Pyroblast r0 → 125 mana after scaling, 6 s cast, 5 s DoT; Blast Wave r0 → 55 mana after scaling, 25 dmg + 30 % slow |
| Priest | Divine Spirit, Inner Focus, Power Infusion, Lightwell, Desperate Prayer, Fear Ward | talents / racials, no trainer ranks | see next column — the two that matter at 24 are the *reactive* ones: **Inner Focus → 0 mana, 3 min CD, next spell free +25 % crit (level-invariant, grant as-is)**; **Fear Ward (racial) → 0 mana, 30 s CD, one fear immunity, level-invariant, grant as-is**. Divine Spirit → +10 Spirit party (scaled from the 60-tier value); Desperate Prayer → instant self-heal of ~35 % max HP, 10 min CD |
| Druid | Nature's Swiftness | 21-pt Balance/Restoration talent, no ranks, no cost | **nothing to scale.** Keep: 0 mana, 3 min CD, next Nature spell instant. Add the level-24 caveat that its payoff spells are Healing Touch r≤4 and Wrath r4 |
| Druid | Swiftmend | 31-pt Restoration, no ranks, **cost is a % of base mana** | Percentage cost → level-invariant, so it expresses directly at 24: **~17 % of base mana (≈76 at 24)**, 15 s CD, consumes a Regen/Rejuv to heal for its remaining amount. The only invented part is the *heal size*, which inherits the at-24 Rejuvenation r4 (180) if that is what it consumes |
| Druid | Innervate, Feral Charge, Nature's Reach/Moonglow-style stat talents | talents | Innervate → 0 mana, 6 min CD, +400 % mana regen 20 s (level-invariant, grant as-is); Feral Charge → 5 rage, 15 s CD, charge + 4 s immobilise (rage pool identical → 5 rage unchanged); stat talents need no representation (they modify the character sheet, not the ability list) |

## 7. Missing evidence

* `ranks/priest.md` says "See `priest_ranks.tsv` payload below" but **that TSV is not present** under
  `.pi/research/ranks/` or `.pi/research/level24/` (two ENOENT probes). Every priest at-24 magnitude in
  this report therefore comes from `level24/priest.md`'s summary bullet, not from the rank ladder. The
  ladder itself should be re-exported.
* Priest base mana at level 60 is blank in `warcraft.wiki.gg/wiki/Base_mana`'s vanilla table (priest
  column has gaps at 25, 29, 30, 35, 39, 41–59, 60). The ≈1300 used here is an **inference** from the
  priest↔mage gap at level 40 (911 vs 853) applied to the mage's 1213. This is the single weakest number
  in §1 and it propagates to every priest scaled cost (±30 %).
* Level-60 BiS Int for priest/druid (assumed 400) is not on disk anywhere.
* Level-24 BiS Int for druid (≈96 assumed) is not on disk; `level24/druid.md` §1 explicitly says the
  level-24 base attributes are UNVERIFIED and must not be invented.
* Disk conflict on priest base mana at 24: `ranks/base_stats.md` says **540**, the wiki table says **480**
  and is corroborated on druid (449 ✓). Report used 480; mage used 463 (azerothcore) rather than the
  interpolated 459 from the wiki table — a 1 % difference.
* Mage Fireball / Frostbolt at-24 magnitudes were not re-read this pass (absent from the slice of
  `ranks/mage.md` read); they are available at 24 so they need no scaling, but their cells are UNVERIFIED.
* All talent-ability magnitudes in §6 (Ice Barrier, Pyroblast r1, Blast Wave r1, Hemorrhage r1, Divine
  Spirit, Swiftmend's % cost, Innervate) are **UNVERIFIED** — the local briefs contain no talent tables
  (each rank file explicitly excludes talents: `ranks/warrior.md` §Table lists Mortal Strike, Bloodthirst,
  Shield Slam etc. as "excluded").
* Rage cap 100 is asserted as level-invariant; the rank files document the formula
  (`R = 15·d/(4·c) + f·s/2`) but not the cap. Low risk, but not directly sourced.

## 8. Sources

Kept (disk): `.pi/research/ranks/warrior.md`, `rogue.md`, `mage.md`, `druid.md`, `priest.md` (ranks and
levels), `.pi/research/level24/priest.md` and `level24/mage.md` (at-24 pools and the +52 Int / 1998-mana
figures), `.pi/research/level24/druid.md` §1 (the UNVERIFIED base-attribute warning).

Kept (web, only for the values disk did not carry):
`warcraft.wiki.gg/wiki/Base_mana` (base mana per level; corroborates druid 449/1244);
`nether.wowhead.com/classic/tooltip/spell/596, /21562, /976, /552, /1706` (Prayer of Healing r1, Prayer of
Fortitude r1, Shadow Protection r1, Abolish Disease, Levitate — read as raw tooltip JSON).

Rejected/deprioritised: `wow4ever.quest` (custom server, flagged as Classic-plus in every rank file);
`wowhead.com/classic` HTML (JS-rendered); zugzugdb Flash Heal (contradicts its own ladder).

## Next steps

1. Re-export `priest_ranks.tsv` and read the real priest ladder; then re-derive the priest s with base
   mana 60 from a second source.
2. Source one talent-ability table (levels, costs, magnitudes) so §6's proposals stop being inventions —
   the six named abilities (Cold Snap, Nature's Swiftness, Swiftmend, Blade Flurry, Ice Barrier, Shield
   Slam) are the minimum.
3. Decide §5.1 explicitly: either magnitudes scale with s, or level-60 buffs ship at 24 at full strength.
   Every caster's value at 24 hinges on that single choice more than on the cost arithmetic.
