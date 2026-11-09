# Research: OctoWoW Hunter — class changes

Prose source: **<https://octowow.st/hunter>**, read in full. Structural/numeric source: the talent payload
of **<https://octowow.st/talents/hunter/?points=-FoFoBDAoJQQoB-dQ>** (Next.js flight data). Nothing is
filled in from memory or from vanilla 1.12.

Status: **prose complete, numbers partial.** The page states intent ("a percentage of your ranged weapon
damage") and rarely prints the percentage. Payload per-rank values are quoted; where neither source has one
it is **UNVERIFIED**.

Tree names come from the payload's own headers: `"index":0` → **Beast Mastery**, `1` → **Marksmanship**,
`2` → **Survival**. In a record `i` is the slot in the tree (row = `i / 4`, 4 slots a row, 28 a tree);
`index` is the tree, confirmed by the tooltips' own links (Swift Aspects carries
`{"tree":0,"talent":1,"ranks":5}`).

## 1. New abilities

| Ability | Cost / cooldown / gate (as stated) | Effect (as stated) |
|---|---|---|
| **Steady Shot** | level 4, "six ranks", "5% of base mana" | "A precise shot that strikes for a percentage of your ranged weapon damage plus a flat amount." |
| **Kill Command** | "5% of base mana", "Cooldown: 8 sec" | "Command your pet to instantly attack for 80% of its attack power. Usable only after you land a critical strike on the target." (payload: slot 25, 1 rank, needs Spirit Bond 2/2, #41827) |
| **Aspect of the Wolf** | level 10, "seven ranks up to level 60" | "Increases your melee attack power. The melee counterpart to Aspect of the Hawk, and the anchor of the melee Survival build." |
| **Aspect of the Viper** | level 56 | "Regenerates a percentage of your maximum mana every few seconds." |
| **Aspect of the Snake** | level 4 | "Your melee attacks and ranged auto attacks have a chance to restore mana." |
| **Aspect of the Turtle** | level 20, "Cooldown: 3 min" | "Reduces all damage taken by 20%, but increases your time between attacks by 30% and reduces your movement speed by 30%." |
| **Viper Strike** | level 56, "Cost: 215 mana" | "A Sting that drains the target's mana over time. Shares the one-Sting-per-Hunter limit with your other Stings." |
| **Pet Command: Take Position** | — | "Command your pet to move to a targeted position and hold there." |

## 2. Changes to class mechanics

**Pet abilities, verbatim:** "Growl costs 10 Focus, Thunderstomp's cooldown is 40 sec, Shell Shield's is
60 sec, and Scorpid Poison costs 25 Focus on a 3 sec cooldown. Furious Howl costs 50 Focus, and Screech
has a 5 sec cooldown." **Stable size, verbatim:** "You can keep three pets in the stable, buying each slot
from a stablemaster."

**Focus vs mana.** The Hunter is never given an off-mana resource: new shots cost "5% of base mana", Viper
Strike flat mana. **Focus exists only on the pet** — a split the sim lacks (its hunter pays `Mana`; its
`SummonSpec` pet has no resource).

**Pets inherit the owner's gear.** "Your pet inherits your gear." Payload: Endurance Training 5 ranks,
`[6/12/18/24/30]%` of your Stamina plus `[2/4/6/8/10]%` pet health; Thick Hide 3 ranks,
`[12/24/36]%` of item armour plus `[7/14/20]%` pet armour; Spirit Bond 2 ranks, pet AP `[12/25]%` and spell
power `[7/15]%` of **ranged** attack power.

**Traps and auto-shot.** No general trap or Sting rewrite is stated — only the one-Sting limit and the
talents in §4. Auto Shot appears only as a talent subject.

## 3. Changes to baseline spells — the page's numbers, verbatim

| Spell | Change |
|---|---|
| Arcane Shot | "Deals a percentage of your ranged weapon damage on top of its Arcane damage, so it scales with your bow. It also costs roughly a third less mana at every rank (Rank 8: 190 → 125)." |
| Aimed Shot | "Mana cost cut by about a third (Rank 6: 310 → 205), but the cooldown is 26 sec. It is a cooldown, not a rotational shot: the Marksmanship tree is built around resetting it." |
| Raptor Strike | "Deals a percentage of weapon damage plus a flat bonus." |
| Mongoose Bite | "Usable on demand, deals a percentage of weapon damage, and strikes with both weapons while dual wielding." |
| Wing Clip | "Deals a percentage of weapon damage, and has a 3 sec cooldown." |
| Scorpid Sting | "Reduces the target's physical damage done and attack speed. It has a single rank." |
| Serpent Sting | "Costs about a third less mana at every rank (Rank 9: 250 → 165)." |
| Volley | "Costs about a third less mana (Rank 3: 490 → 330)." |
| Rapid Fire | "Also reduces the cast time of Aimed Shot and Steady Shot, not just your attack speed." |

No baseline change is stated for Multi-Shot, Mend Pet, Hunter's Mark, Concussive Shot, Distracting Shot,
Feign Death, traps or auto-shot. **Silent, not negative** — do not read that as "unchanged".

## 4. Talent changes per tree

Row and rank counts were checked against the payload, which agrees with the page throughout. 19 talents in
Beast Mastery, 16 in Marksmanship, 20 in Survival. Format: name (row, ranks, note).

**Beast Mastery.** Kill Command (row 7, 1, new capstone). Coordinated Assault (row 3, 1, new: after Arcane
Shot/Steady Shot/Raptor Strike the pet's next attack "also strike[s] for Physical damage equal to 20% of
your Attack Power", once per 3 sec). Swift Aspects (row 1, 5, new: 10% chance, `[3/6/9/12/15]%` attack speed
— ranged under Hawk, melee under Wolf). Improved Primal Aspects (row 2, 3, replaces Improved Aspect of the Monkey: dodge `[2/4/6]%`, and under Wolf
"you're healed for `[2/4/6]%` of your melee damage"). Scent of Blood (row 5, 3, new: `[5/10/15]%` chance,
"40% additional damage for 8 sec"). Bestial Precision (row 6, 2, new: pet hit `[4/8]%` physical, `[9/18]%`
spell, `[5/10]` weapon skill). Endurance Training / Thick Hide / Spirit Bond — re-based on the owner's gear
(§2). Bestial Wrath — "Row 7 → Row 5", "90 sec" cooldown. Intimidation — "Moved within Row 5", 3 sec stun,
+50% pet threat 8 sec. Bestial Discipline — "Row 5 → Row 4", `[2/5]` Focus a tick, `[10/20]%` pet special
cooldown. Improved Mend Pet — "+20/40% heal, plus a dispel chance". Pathfinding, Improved Eyes of the Beast
— pet speed; pet damage and Focus regen while channelling. **Removed:** Improved Aspect of the Hawk,
Improved Aspect of the Monkey, Bestial Swiftness.

**Marksmanship.** Lock and Load (row 7, 1, "new capstone, replaces Trueshot Aura": crits of Steady/Aimed/
Arcane Shot "reset Aimed Shot's cooldown and trigger Lock and Load, which cuts Aimed Shot's cast time and
makes it hit every enemy between you and the target"; payload: 100% chance, −1 sec cast, 10 sec window,
needs Experimental Ammunition 1/1). Experimental Ammunition (row 5, 1, new: 5% of Aimed Shot's damage as
Fire/Arcane/Nature plus a rider — Explosive "20% ranged weapon damage to enemies within 5 yards", Poisonous
"100% increased damage" and −240 armour 15 sec, Enchanted "100% increased damage" and +3% magic damage
taken 6 sec). Endless Quiver (row 4, 2: `[3/6]%` chance of "an additional normal ranged attack… does not
spend ammo"). Piercing Shots (row 5, 2: bleed `[15/30]%` over 8 sec, "generates no threat"; needs Mortal
Shots 5/5). Improved Stings (row 2, 5, replaces Improved Serpent + Improved Scorpid: Serpent
`[6/12/18/24/30]%`, Viper `[1/2/3/4/5]%`, Scorpid a further `[2/4/6/8/10]%`). Swiftshot (row 3, 3, new:
Arcane −`0.5/1/1.5` sec, Aimed −`2/4/6` sec). Improved Marksmanship (row 6, 2, new: Steady and Aimed
`[5/10]%`). Barrage — "Moved within Row 5", now also Volley −`1/1.5/2` sec cast. Hawk Eye — "3 Ranks → 2
Ranks". **Removed:** Trueshot Aura, Improved Hunter's Mark, Improved Arcane Shot, Improved Serpent Sting,
Improved Scorpid Sting.

**Survival.** Untamed Trapper (row 7, 1, "new capstone, replaces Wyvern Sting": Immolation/Explosive Trap
20% cheaper and scaling with melee AP, "all traps can be placed in combat"). Lacerate (row 5, 1, replaces
Counterattack: "40% of melee Attack Power" plus "a bleed for 20% of that damage over 8 sec", after a crit,
"hits 15% harder from the target's side"). Carve (row 3, 1: "up to 5 enemies in a 10 yard cone", "60% weapon
damage", "shares a cooldown with Multi-Shot"). Stinging Nettle (row 4, 2: Serpent Sting for `[20/40]%` of
its duration, "ignore resistances and immunities"). Alone Against the World (row 2, 2: `[3/6]%` all damage
with no pet). Planning Ahead (row 3, 2, replaces Clever Traps: `[13/25]%` if untriggered 5 sec). Vicious
Strikes (row 6, 2: −`[0.5/1]` sec and `[5/10]%`). Resourcefulness (row 1, 5: `[2/4/6/8/10]%` off traps and
melee) and Swift Reflexes (row 1, 2: parry and attack speed `[1/2]%`). Improved Slaying (row 1, 3, merges
Monster + Humanoid Slaying: `[1/2/3]%` damage and crit damage vs Beasts, Giants, Dragonkin, Humanoids).
Trap Mastery — "Row 4 → Row 5, 2 Ranks → 3 Ranks". Savage Strikes, Killer Instinct, Lightning Reflexes
(+`[20/40/60/80/100]%` of Agility as melee AP), Surefooted (+`[1/2/3]%` dual wielding), Entrapment and
Improved Wing Clip — "5 Ranks → 3 Ranks each". **Removed:** Wyvern Sting, Counterattack, Clever Traps,
Deflection, Monster Slaying, Humanoid Slaying.

## 5. What this costs in `src/WowRoguelike.Core/Content.fs`

`let private hunter` at `src/WowRoguelike.Core/Content.fs:4374`: a 6-row weapon ladder, `Mana`,
`partyThreatBp`, and `AbilityDef.kitAt` over `[arcaneShotRanks; serpentStingRanks; raptorStrikeRanks;
aimedShotRanks; scatterShot; multiShot; viperSting; summonPetRanks; mendPet]`. The pet is `hunterPet` at
`src/WowRoguelike.Core/Content.fs:688`, a static `SummonSpec` with `Swing = Some(38, 62, secTicks 2.0)`.

**(a) Expressible today — 5 of the build's 16 talents.** `withTalents`
(`src/WowRoguelike.Core/Content.fs:3462`) already folds per-spell `CostBp`, `CastTicksOff`, `CritBp` and
`CooldownOff` into a built kit, so these are data, not vocabulary: **Efficiency 5/5** (`Spells` lines at
`CostBp = 1000` on Arcane Shot, Serpent Sting, Aimed Shot, Multi-Shot, Viper Sting, Scatter Shot —
per-spell `CostBp` applies regardless of the `healing` flag, `src/WowRoguelike.Core/Domain.fs:1182`);
**Swiftshot 3/3** (`CooldownOff`: Arcane −1.5 s, Aimed −6 s); **Resourcefulness 5/5** (`CostBp = 1000` on
Raptor Strike; traps unmodelled); **Aimed Shot** (6 s → **26 sec** cooldown, rank stays level 20);
**Scatter Shot** (numbers only, if the sim keeps its own reading — Contradictions).

**(b) Needs a new model axis — 10 of 16.** `Talents` (`src/WowRoguelike.Core/Domain.fs:1128`) is a
**healing** record (`HealDoneBp`, `HealCostBp`, `IntellectBp`, `RegenBp`, `ManaRefundOnCritBp`, `CritArmorBp`,
`HealingWayBp`, `SpiritToHealingBp`, `TotemEffectBp`, `Spells`, `CooldownOff`) with no ranged-damage,
crit-damage or attack-speed field, and `SpellTalent` has `HealBp` but no `DamageBp`. That one fact is most
of this bucket.

- **Lethal Shots 5/5** (+5% ranged crit) — *ranged crit as its own hit class*; `CritChanceBp`
  (`src/WowRoguelike.Core/Domain.fs:1281`) is one number for melee and ranged, and no `Talents` field feeds it.
- **Mortal Shots 5/5** (+30% ranged crit damage) — *crit multiplier*; a crit is a fixed roll today.
- **Improved Marksmanship 2/2** (+10% Steady/Aimed) — *per-spell damage bonus*; `HealBp` is heal-only.
- **Ranged Weapon Specialization 5/5** (+10% ranged damage) — *ranged damage multiplier*, beside `MeleeDamageBonus`.
- **Experimental Ammunition 1/1** — *ammunition element + a queued "next cast" rider*; `Sunder` and
  `AreaDamage` exist, `Empowers` (`src/WowRoguelike.Core/Domain.fs:839`) is healer-specific.
- **Piercing Shots 2/2** — *crit-triggered effect* and *threat-free damage*; nothing fires on a crit, and
  `Threat` is additive with no zero-threat marker.
- **Lock and Load 1/1** — *crit-triggered cooldown reset* plus *a line hitting everything between caster and
  target* (`MultiShot` is 3 targets near the target, `AreaDamage` a radius; neither is a line).
- **Improved Slaying 3/3** — *per-`CreatureType` damage*; `CreatureType` exists, nothing reads it.
- **Swift Reflexes 2/2** — *parry* (absent: only dodge and block) and *attack speed* (`AutoAttack.Ticks` fixed).
- **Improved Stings 5/5** — *DoT damage %* and *drain amount %*.

**(c) Collapses or renames kit rows — 1 of 16, plus one leg.** **Improved Concussive Shot 5/5** — 5 of the
build's 41 Marksmanship points buy `[4/8/12/16/20]%` chance to stun on **Concussive Shot**, and `kitAt` has
no Concussive Shot: a row to create before the talent means anything (its on-hit chance also wants a proc
axis, so (c) with a (b) tail). **Improved Stings' Scorpid leg** — Scorpid Sting is absent from `kitAt` too;
§3's rewrite of it is a row to add, not to edit.

**The pet, explicitly. This build buys nothing for the pet: 0 of 51 points go into Beast Mastery.** Every
pet talent would land in **(b)**, because `SummonSpec` is constant data and the pet is a real `Entity` with
its own `Health`, `Armor` and `Swing` (`src/WowRoguelike.Core/Domain.fs:780`), tied to the hunter only by
`Owner` (`src/WowRoguelike.Core/Domain.fs:1319`, "A pet dies with its owner"):

- **Inheritance** — Endurance Training, Thick Hide, Spirit Bond: an inheritance link on `SummonSpec`
  (health/armour/AP as a share of the owner's), which the type cannot express at all. **This is the change
  most likely to break the model.**
- **Pet damage and crit** — Unleashed Fury, Ferocity, Frenzy, Scent of Blood, Bestial Wrath.
- **Pet control** — Kill Command, Coordinated Assault, Intimidation: a command effect that makes the *pet*
  act; `Effect` has no equivalent.
- **Pet Focus** — Bestial Discipline and §2's Growl/Thunderstomp/Scorpid Poison table: a resource on a
  summoned entity.

## 6. The build decoded

`?points=-FoFoBDAoJQQoB-dQ` → **Beast Mastery 0 / Marksmanship 41 / Survival 10 = 51 of 51.**

```
Marksmanship 41  1:5 Improved Concussive Shot  2:5 Efficiency       5:5 Improved Stings
                 6:5 Lethal Shots              9:1 Aimed Shot      11:3 Swiftshot
                14:5 Mortal Shots             16:1 Scatter Shot    17:1 Experimental Ammunition
                18:2 Piercing Shots           20:2 Improved Marksmanship
                22:5 Ranged Weapon Spec       25:1 Lock and Load
Survival     10  0:3 Improved Slaying          1:5 Resourcefulness  2:2 Swift Reflexes
```

Every payload prerequisite is satisfied: Mortal Shots `requires 6/5` (Lethal Shots 5/5), Experimental
Ammunition `requires 9/1` (Aimed Shot), Piercing Shots `requires 14/5` (Mortal Shots), Lock and Load
`requires 17/1` (Experimental Ammunition).

**It commits to Marksmanship**, and its row-7 capstone — the 31-point talent, replacing Trueshot Aura — is
**Lock and Load** (slot 25, 1 rank, #52920). With 41 points spent it also carries the row-6 damage talents
and the Aimed/Scatter/Experimental Ammunition utility line.

**Taken, among abilities the sim models:** Aimed Shot (1), Scatter Shot (1), plus talent buffs on Arcane
Shot (Swiftshot, Efficiency), Serpent Sting (Improved Stings, Efficiency), Raptor Strike (Resourcefulness),
Viper Sting (Improved Stings' drain leg, Efficiency). **Left on the table:** Multi-Shot (Barrage and Endless
Quiver untaken), Viper Sting beyond 5% drain, Mend Pet, and the pet entirely. **5 of the 41 Marksmanship
points buff an ability this `Content.fs` does not have.**

## Contradictions

1. **Aimed Shot's rank count.** Page: "Rank 6: 310 → 205". Calculator: a **1-rank** talent (slot 9) whose
   whole tooltip is "An aimed shot that increases ranged damage by 70". Two readings: trainer ranks exist
   alongside the talent rank, or the talent grants the ladder and the tooltip is r1. The **26 sec cooldown
   is page-only** — the payload carries no cooldown.
2. **Scatter Shot.** Page silent; payload: "deals 50% weapon damage and disorients the target for 4 sec. Any
   damage caused will remove the effect." The sim models a 3 sec **silence** with `Interrupt` and no damage
   (`src/WowRoguelike.Core/Content.fs:755`), by its own comment a deliberate departure. Porting faithfully
   gives a disorient, not the game's only interrupt-silence.
3. **Viper Sting vs Viper Strike.** §1 lists "**Viper Strike** … Level 56 … 215 mana"; §4's Improved Stings
   says "the mana drained by **Viper Sting**". Sim has `viperSting` at level 24, 90 mana, 15/s over 8 sec.
   Two readings: a later second sting, or a rename in one place. **Unresolved by either source.**
4. **Mana ceilings differ.** Page quotes Rank 8 Arcane Shot 190 → 125 and Rank 9 Serpent Sting 250 → 165;
   `Content.fs` ladders stop at rank 5 (105 and 115 mana). The ~33% cut matches the page's own arithmetic; it
   is **not** evidence about our rows.

## 7. Missing evidence / UNVERIFIED

- **No coefficients in the prose.** "A percentage of your ranged weapon damage" appears for Steady Shot,
  Arcane Shot, Raptor Strike, Mongoose Bite and Wing Clip — five abilities, zero percentages. **UNVERIFIED.**
- **Aimed Shot's 26 sec cooldown** and **Arcane Shot's Rank 8 cost 125** are page-only. **UNVERIFIED**
  against the calculator.
- **Experimental Ammunition** — the "active ammunition" selector, and so each rider's trigger, is never
  explained. **UNVERIFIED.**
- **Lock and Load's reset chance**: page "a chance", payload **100%** on a crit. Payload is the number, page
  the intent; the disagreement is unremarked.
- **Steady Shot's six ranks**: no per-rank values, no spell IDs.
- **Whether any of this is live** on OctoWoW's current build versus planned is not stated.
- **Whether the calculator tree is OctoWoW or vanilla-plus-partial** is inferred. Evidence for OctoWoW:
  Swiftshot, Experimental Ammunition, Endless Quiver, Piercing Shots, Improved Marksmanship, Coordinated
  Assault, Scent of Blood, Bestial Precision, Swift Aspects, Improved Slaying, Alone Against the World and
  Swift Reflexes are all present, and none exists in 1.12.
- **Sim-side gaps not chased**: whether the engine calls `Talents.costBp` for a non-healing ability, and
  whether a ranged auto-shot can take a `CritBonus` at all. **Both answered by the port, below.**
- **Not done**: the other classes.

## 8. Landed — seven of sixteen, and what the port found

Ported in `Content.fs` (`hunterTalents` + the tagged kit rows), seven of the sixteen above: Efficiency,
Resourcefulness, Swiftshot (Arcane leg only), Improved Marksmanship, Improved Stings, Ranged Weapon Spec
as `Talents.damageBp` lines, and Lethal Shots as `Traits.CritBonusBp = 500`.

Two answers to the open questions at the end of §7:

1. **`Talents.costBp` does apply to a non-healing ability** — the `CostBp` fold is not gated on the
   `healing` flag, which §5(a) already noted at `Domain.fs:1182`. Efficiency lands as written.
2. **A ranged auto-shot cannot take a talent at all.** `advanceSwings`
   (`src/WowRoguelike.Core/SimTick.fs:481`) adds `serpentBonus` and no `Talents.damageBp`, so
   `DamageDoneBp` never reaches a swing — for any class. Ranged Weapon Spec is a kit-row talent by
   force, and the Warrior's landed One-Handed Weapon Spec is inert for the same reason.

Three deviations from §5, all deliberate:

- **Swiftshot's Aimed leg (−1.5 s) is skipped.** `withTalents` floors a shortened cooldown at 1.5 s, and
  6 s − 4.5 s = 1.5 s would shorten a page-26 s row fourfold. Arcane only.
- **Improved Stings moved from (b) to (a) between the note and the port**, because the Warlock port made
  `DamageOverTime` take `Talents.damageBp` at application time. No Hunter code was needed for it.
- **Mortal Shots stays unported** and is the one talent worth an axis here: it needs the crit multiplier
  as data (`SimCombat.rollCrit` hardcodes `amount * 150 / 100`, `src/WowRoguelike.Core/SimCombat.fs:23-25`)
  plus a per-ability "benefits from bonus crit damage" flag. Improved Concussive Shot stays unported for
  the simpler reason: there is no Concussive Shot row.

Measured: `dotnet test` 240 passed / 10 skipped (two new hunter tests, plus one for the swing hook);
`comps 3 --level 60` **36 of 36 cells clean** after the hook; **99-100% Hunter mana in every cell**.
Details in `docs/agents/domain.md` §8, "Hunter: seven of sixteen".

## 9. The swing hook — landed

`Talents.damageBp` did not reach an auto-attack: `advanceSwings`
(`src/WowRoguelike.Core/SimTick.fs:481`) applied no talent multiplier, so white damage
ignored every tree. A swing has no ability row, so `SimTick.swingName = "Attack"` is the
one name it answers to, and both hands now fold `Talents.damageBp` before the attack
table (Serpent Form's bonus added after, being the form's damage).

Consequences: the Warrior's One-Handed Weapon Specialization (tagged since its port,
inert until now) applies; the Hunter tags `"Attack"` with Ranged Weapon Spec so the bow
itself takes +10%. Both over-apply the same way the note already recorded — the Warrior's
talent also raises `Shoot` and the off hand, the Hunter's also raises melee Raptor Strike
via the shared crit — and a talent that must tell a bow from a dagger still needs a name
on `AutoAttack`, which nothing has needed yet.

Red test first: `the tree's damage bonus reaches a white swing` (30 vs 33 on a 30..30
fixture, `DamageDoneBp` 0 against 1000). Bench snapshot regenerated: 23 of 30 cells move,
mean 958 → 956 ticks, no outcome flips. `comps 3 --level 60`: 36 of 36 clean.

## 10. The (b)-bucket, re-ranked on a class that actually fights

§5's ordering was written against a Hunter that **cast nothing** — the AI had filed it as
melee because Raptor Strike reaches 1 tile, so the class ran at 97-100% mana and dealt
15103 damage in the L36 probe (`docs/agents/domain.md` §8, "The lane question"). With the
lane fixed it deals **21381 at L36 and 27367 at L60**, and the ranking of what to port next
changes with it. Same fixture throughout (`library`, seed 1,
`Warrior/Priest/Mage/Rogue/Hunter`): per-ability damage and the commands the AI issued.

**The measured profile.** Crit chance is `500 + Agility×10 + Lethal Shots 500`
(`Content.fs:3296`): **27.4% at L36** (Agility 174) and **39.3% at L60** (Agility 293). The
L36 Hunter ends the wing at 1137/1695 mana — it is mana-bound for the first time. The L60
one ends at **3300/3300**: utterly idle.

| row | L36 | L60 |
|---|---|---|
| Auto Attack (swing, crits at 200%) | 7250 | 9743 |
| Arcane Shot (53 / 39 casts issued) | 5362 | 4088 |
| Multi-Shot (24 / 19) | 4438 | 7575 |
| Aimed Shot (46 / 32) | 3950 | 4244 |
| Raptor Strike (3 / 8) | 381 | 1717 |

Every hostile in every wing is **Humanoid** (`library` 44, `graveyard` 48, `armory` 50,
`cathedral` 62) — no Beasts, no Dragonkin, no Undead, and `CreatureType` has no `Giant` at
all, so a slaying talent covers one type out of the note's four.

**Mortal Shots 5/5 (+30% crit damage).** Swings crit for 200%
(`src/WowRoguelike.Core/SimCombat.fs:284`) and spells for 150%
(`SimCombat.fs:23-25`), so the two sites gain differently: a swing row goes `1+p` → `1+1.3p`
and a spell row `1+0.5p` → `1+0.8p`. At p = 0.274 that is **+6.4% on the bow and +7.2% on
the shots**, **+1462 damage, +6.8% of the Hunter's whole output**; at p = 0.393, **+8.5%
and +9.9%**, **+2396, +8.8%**. Five points, **1.4-1.8% per point** — and the axis is one
field and two multiplications, because the two multipliers are already spelled out at the
two sites (200% and 150% are Vanilla-faithful, not a bug).

**Improved Slaying 3/3 (+3% damage and crit damage vs Beasts/Giants/Dragonkin/Humanoids).**
All hostiles are Humanoid, so it is a flat **+3%** (≈641 / ≈821 damage), **1.0% per point**.
`CreatureType` is already on every entity (`Domain.fs:122`) and nothing reads it — the
cheapest axis in the bucket, and a per-type damage modifier is a shape three other classes
want (the Rogue's and Warrior's slaying talents are the same predicate).

**Piercing Shots 2/2 (a crit bleeds for 30% of the hit over 8 s).** The largest *ceiling* in
the bucket: 30% of every crit's damage, and a crit is 27-39% of hits at double damage, is
`0.6p/(1+p)` = **+12.9% at L36 and +16.9% at L60** if the bleed runs to term. It will not:
mobs die in seconds and a target swap drops it, so the honest expectation is well under
half. Two points either way. The cost is vocabulary: `ProcResult` has exactly two shapes,
`GrantResource` and `EmpowerNext`, both of which act on the **caster**, and `procOn` carries
only the caster's id (`SimCombat.fs:205`) — a proc that puts a DoT on the *target*, with a
payload derived from the triggering hit, is a third shape and a target to resolve. On top
of that, "generates no threat" needs a threat-free damage marker the engine does not have.

**Lock and Load 1/1 — worth nothing *yet*, and the reason is an (a)- bucket row that never
landed.** Its reset leg only pays when Aimed Shot is cooldown-starved, and in this kit it
is not: `aimedShotRanks` still carries `CooldownTicks = secTicks 6.0`
(`Content.fs:772`) against a 3 s cast, so the ceiling in 8064 ticks is 67 casts and the AI
issued 46; at L60 the ceiling is 46 and it issued 32. §5(a) lists "Aimed Shot 6 s → 26 s
cooldown" as expressible and it is **not in `Content.fs`** — the sim's Hunter has a far
better Aimed Shot than OctoWoW's, which is also why the build's 3 points of Swiftshot
(shaving a cooldown the sim does not have) measure as nothing. Correct the row first; a
crit-driven reset of a 6 s cooldown with a 3 s cast is worth almost nothing, and of a 26 s
cooldown is worth having. The line-AoE leg is out of reach regardless — `MultiShot` is 3
targets near the target and `AreaDamage` is a radius; neither is a line.

**The rest, and why they are last.** *Swift Reflexes 2/2* needs parry (the engine has dodge
and block) and attack speed (`AutoAttack.Ticks` is fixed data) — two axes, two points,
defensive. *Experimental Ammunition 1/1* is blocked on evidence before it is blocked on
code: §7 records that the "active ammunition" selector is never explained, and the rider's
trigger is a property of it. *Improved Concussive Shot 5/5* buys a chance to stun on a row
the kit does not have (`kitAt` holds no Concussive Shot), so five of the build's 51 points
wait on an ability plus a proc.

**Order, by measured value per axis:** Mortal Shots (5) → Improved Slaying (3) →
Piercing Shots (2) → Lock and Load (1, *after* the Aimed Shot row) → Swift Reflexes (2) →
Experimental Ammunition (1) → Improved Concussive Shot (5).

Mortal Shots is the pick: the largest measured increment in the bucket, the smallest axis,
and it is the axis every class's crit-damage talent needs. Its one honest over-application
is the same one §9's swing hook carries — the sim cannot tell a bow from a dagger, so a
`CritDamageBp` raised for ranged crits also raises Raptor Strike's, which is 381-1717
damage of 21381-27367. That is 1.8-6.3%, inside the noise of the talent itself.

## 11. Mortal Shots — landed, and the divergence it forced

`Talents.CritDamageBp` is the axis: **percentage points added to a crit's multiplier**, not
a factor (Mortal Shots 5/5 is `3000`). Both crit sites keep their own base —
`SimCombat.rollCrit` is 150% for spells, `meleeStrike` is 200% for swings — and each adds
the same points, so "+30%" is +30 wherever it lands. The Hunter's five points are
`hunterTalents.CritDamageBp = 3000`. Its over-application is the one Lethal Shots already
carries: `CritChanceBp`/`CritDamageBp` are one number per hero, so Raptor Strike's crits
get it too — 381 of 21381 damage at L36, 1717 of 27367 at L60.

**Measured** (`library`, Median, 8-seed A/B by zeroing the field at runtime, then 12 seeds
for the wipe count):

| | dmg/tick | mean damage | clears |
|---|---|---|---|
| L36 off | 2.772 | 23304 | 12/12 |
| L36 on | **3.072 (+10.8%)** | **25857 (+11.0%)** | **11/12** |
| L60 off | 4.818 | 24939 | 12/12 |
| L60 on | **5.511 (+14.4%)** | **27654 (+10.9%)** | 12/12 |

The prediction in §10 was +6.8% / +8.8% from the crit share alone; the measurement is
+11% on damage and +11-14% on throughput, because more damage also ends the wing sooner
(L60: mean 5183 → 5023 ticks).

**The one wipe, diagnosed.** On L36 seed 3 the talent flips a clear into a party wipe —
the Hunter dies first at tick 7449 and then everyone does. It is *not* the Hunter pulling
threat: at tick 7000 the eleven live mobs are on the **Priest** (5 of them), the Mage, the
Rogue and the Hunter one each, with the tank holding none of them — the pull has already
cascaded through the healer. The talent shifted the RNG stream onto a seed where the
party's margin is thin; the L36/L39 grid is wipe-prone with or without it (`comps 2
--level 36` is **34/36 clean off, 32/36 on**, and every losing cell drops one seed of two).
At L60 — the level every increment in this port has been judged at — `comps 2 --level 60`
is **36 of 36 clean**, the bench snapshot is **byte-identical** (`defaultRoster` seats no
Hunter), and `dotnet test` is **252 passed / 10 skipped**.

**The divergence, recorded rather than fixed.** §5(a) lists Aimed Shot's cooldown as
6 s → 26 s; `aimedShotRanks` still carries 6 s (`Content.fs:772`) and **stays there by
decision**, because the 26 s row makes the class's best button fire once per 26 seconds and
would require re-measuring Swiftshot and Lock and Load from scratch. Two consequences are
now recorded rather than blamed on the port: Swiftshot's 3 points shave a cooldown the sim
does not have (they measure as nothing), and Lock and Load's reset buys nothing until
something makes Aimed Shot cooldown-starved. Both are §10's rank-4 note; `Content.fs`'s
`hunterTalents` comment names the divergence where the talent is built.

## 12. Improved Slaying — landed, and the one talent that needs the target

`[1/2/3]%` damage **and** crit damage against Beasts, Giants, Dragonkin and Humanoids is
the first talent in this port whose value depends on *what is being hit*, so it could not
be a static field the way Mortal Shots' crit multiplier was. Three decisions:

- **One field, two halves.** `Talents.SlayingBp` (300 at 3/3) is both the damage share and
  the crit-damage share, using the convention §11 set: the damage share multiplies what
  lands, the crit share is points on the multiplier. A non-crit swing that deals 100 deals
  103; a critting one takes 203% of 45 rather than 200% of 45.
- **The gate is the target's `CreatureType`**, read by `SimCombat.slayingBp attacker target`.
  It pays on `Humanoid`, `Beast` and `Dragonkin`; `Giant` is not a `CreatureType` in this
  engine and no content is one, so the fourth type is unreachable rather than modelled.
- **It rides the paths that already exist**: `spellHit` (every spell, single-target and per
  body inside an area), the top of `meleeStrike` before the attack table (before armour, so
  it reads as 3% more damage rather than 3% more of what got through), the crit branch of
  `meleeStrike`, and a `DamageOverTime`'s per-tick amount baked at application.

**Where it pays and where it does not.** Every Scarlet Monastery creature is a `Humanoid`
(`ScarletMob` carries no type field, so the content builder writes `Humanoid`), which is why
the class's own fixture gets the full 3%. Wailing Caverns is mixed — the Deviate beasts are
`Beast` and the lashers and Verdan are `Elemental` — so the talent is worth less there, and
worth nothing against the elementals. That is the talent as printed, not a port failure.

**Measured.** Paired A/B over 48 seeds at library/Median L60, toggling only `SlayingBp`:
**mean damage/tick ratio 1.0289** — the analytic +3%, arrived at without any of the number
being fitted — with clear time at **0.9984** (unchanged; the wing's pace is not
Hunter-bound). The median ratio is 1.007 and only 26 of 48 seeds improve, because a 3%
change to one member moves the RNG stream and the per-seed spread is wider than the effect.
Anything that reports a talent this size from a single seed is measuring noise.

**The crit-damage half does not reach `AreaDamage`.** That path rolls one crit for the whole
area before it knows which bodies are inside it (`rollCrit` at
`src/WowRoguelike.Core/SimCombat.fs`, the `AreaDamage` case), so a per-target crit share has
nowhere to go. It is inert for the Hunter — the class has no area spell (`Multi-Shot` is
three `weaponStrike`s) — and is recorded here because the next class that takes a slaying
talent with an AoE will hit it.
