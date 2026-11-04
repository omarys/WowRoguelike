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
  whether a ranged auto-shot can take a `CritBonus` at all.
- **Not done**: seven other classes; any change to `Content.fs` — this note is research.
