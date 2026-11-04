# Research: OctoWoW Druid — the Feral (Bear + Cat) half

Source of every prose claim below: **<https://octowow.st/druid>**, fetched and read in full (68,397 bytes of HTML → 288 lines of text). Every numeric value is quoted from that page or from the server-rendered talent payload at **<https://octowow.st/talents/druid/?points=BiAYAI-FoaYRaDQIIFIB->** (135,157 bytes; **54** records of shape `{"i":slot,"index":tree,"ranks":N,"requires":…,"reqRanks":…,"reqBy":[…],"tooltip":[…]}`). No number here is filled in from memory or from vanilla 1.12.

Status: **prose complete, numbers mostly present.** The payload carries per-rank values for every talent in this build. Where the page states no magnitude it is marked **UNVERIFIED** rather than guessed.

**Tree identity is read off the payload, not assumed:** tree 0 carries Improved Wrath / Moonkin Form / Eclipse (Balance), tree 1 carries Ferocity / Feral Charge / Leader of the Pack (Feral Combat), tree 2 carries Improved Mark of the Wild / Swiftmend / Genesis / Tree of Life Form (Restoration). Record counts 20 / 18 / 16.

---

## 1. New abilities

| Ability | Cost / cooldown | Effect (as stated) |
|---|---|---|
| **Savage Bite** | level 58, "Requires: Bear or Dire Bear Form", 30 Rage, 6 sec | "Savagely bite the target, dealing 80% normal damage plus an additional 30 damage. **Generates a high amount of threat.**" One target. |
| **Barkskin (Feral)** | levels "44 / 52 / 60 (3 ranks)", "Requires: Cat, Bear or Dire Bear Form", 10 min | "Reduces Physical damage taken for the next several attacks, at the cost of reduced movement speed for the duration." **No magnitude given — UNVERIFIED.** |
| **Swift Travel Form** | level 30, "Cost: 13% of base mana" | "Usable outdoors and out of combat only." |
| **Reshift** | — | "Cancels and reapplies your current shapeshift form in one button." |
| **Efflorescence** | level 60, passive | "When another Druid overwrites your Rejuvenation or Regrowth, half of the remaining amount is delivered to that target instantly." |
| **Wrath (Rank 9)** | level 60, 210 mana | "A ninth rank, learned at level 60." |

## 2. Class mechanics

- **Items in form.** "Potions, bandages, food, and most on-use trinkets and weapons are useable in Cat, Bear and Dire Bear Form. A small number of items are individually flagged as unusable while shapeshifted."
- **Free shifting.** "Any form can be cast directly while you are already in another one: Bear to Cat, Cat to Travel, Travel to Bear. The new form replaces the old, with no need to cancel the form you are in first." This is the operator's off-tank lever at the rules level: a bear does not have to leave bear to become a cat and back.
- **Combo points.** "Changing target does not wipe your combo points. You only ever hold one set at a time: building a point on a different target starts that target from zero and discards whatever you had banked."
- **Shape cost.** "Cat, Bear and Dire Bear Form cost 35% of base mana."

## 3. Baseline spell changes (feral-relevant)

| Spell | Change (verbatim) |
|---|---|
| Faerie Fire (Feral) | "Baseline, learned at level 25 with four ranks up to level 54 and a 6 sec cooldown." (Was a Feral talent; see §4 removals.) |
| Frenzied Regeneration | "Converts each point of Rage into a percentage of your total Stamina, so it scales with your gear. Cooldown is 5 min." |
| Enrage | "The damage bonus is not consumed by swinging: it lasts the full duration. The base armor penalty sits on its own separate debuff (**-27% in Bear Form, -16% in Dire Bear Form**), so canceling the Enrage buff early stops the Rage gain but does not remove the armor loss." |
| Ferocious Bite | "Learned at level 24, with six ranks. Extra Energy is converted into a percentage damage increase." |
| Shred | "Has six ranks, and its bonus damage scales by rank." |
| Rake | "Damage scales with your Attack Power." |
| Tiger's Fury | "Also regenerates Energy over its duration, on top of its damage bonus." |
| Soothe Animal | "Rank 2 affects Beasts up to level 40, and a resisted cast causes aggro." |

**Swipe is not on this list.** It appears on the page exactly twice, both times inside a talent description (§4, Carnage and Predatory Strikes). OctoWoW states **no change to Swipe's target count, damage or threat.** See §6 for why that matters.

## 4. Talent changes per tree (only what the build takes, plus removals that matter)

Rows are not modelled anywhere below; only **rank counts, numbers and names** matter to this sim.

### Balance (tree 0) — 11 points, and only 4 of them buy anything feral

- **Nature's Grasp** (1, 1) — "While active, any time an enemy strikes the caster they have a 35% chance to become afflicted by Entangling Roots (Rank 1). Only useable outdoors. 1 charge. Lasts 45 sec." A reactive root, not a damage or mitigation talent.
- **Improved Nature's Grasp** (2, 4) — "Increases the chance for your Nature's Grasp to entangle an enemy by [15/30/45/65]%."
- **Sylvan Blessing** (3, 2) — "Gives you a [50/100]% chance after killing a target that yields experience or honor to allow your Mana to regenerate at a 100% rate while casting. Lasts 15 sec." A Druid in Bear or Cat Form is not casting and does not spend mana.
- **Natural Weapons** (6, 3) — "Increases the damage you deal with physical attacks in all forms by [3/6/10]%. Also increases chance to hit with melee attacks and spells by [1/2/3]%." **The one unambiguously feral Balance talent in the build.**
- **Omen of Clarity** (10, 1, requires Natural Weapons 3/3) — "Each of the Druid's melee attacks or offensive spell casts has a chance of causing the caster to enter a Clearcasting state. The Clearcasting state reduces the Mana, Rage or Energy cost of your next damage or healing spell or offensive ability by 100%." **Explicitly covers Rage and Energy** — the other feral Balance point.

### Feral Combat (tree 1) — 40 points

Defensive (armour, dodge, health, threat, Rage income):
- **Feral Instinct** (4, 3) — "Increases threat caused in Bear and Dire Bear Form by **[5/10/15]%** and reduces the chance enemies have to detect you while Prowling." Page: "5 Ranks → 3 Ranks."
- **Thick Hide** (6, 3) — "Increases your Armor contribution from items by **[3/6/10]%**." Page: "5 Ranks → 3 Ranks."
- **Feral Swiftness** (8, 2) — "Increases your movement speed by [15/30]% while outdoors in Cat Form and increases your chance to dodge while in Bear, Dire Bear and Cat Form by **[2/4]%**." Page: "Row 3, 2 Ranks: replaces Feline Swiftness."
- **Heart of the Wild** (21, 5) — "Increases your Intellect by [4/8/12/16/20]%. In addition, while in Bear or Dire Bear Form your **Stamina is increased by [4/8/12/16/20]%** and while in Cat Form your **Strength is increased by [4/8/12/16/20]%**."
- **Primal Fury** (11, 2, requires Sharpened Claws 3/3) — "Gives you a [50/100]% chance to gain an additional **5 Rage** anytime you get a critical strike while in Bear and Dire Bear Form…" Page: "Row 4 → Row 3."
- **Ancient Brutality** (16, 1 of 2) — "Dodging an attack while in Bear or Dire Bear Form imbues you with the spirit of the Ancients, generating **[2/4] Rage per second for 5 sec**. This effect can only occur once every 9 seconds. While in Cat Form, periodic ticks of your Bleed effects restore [3/5] Energy."
- **Carnage** (22, 1 of 2) — "Your Maul, Swipe, and Savage Bite abilities return **[5/10]% of their damage as healing to you**."
- **Berserk** (18, 1, 6 min cooldown) — "Removes all Fear effects and increases your energy regeneration rate by 100% while in Cat form, and increases your **total health by 20%** while in Bear form. After the effect ends, the health is lost. Effect lasts 20 seconds."

Offensive (damage, crit, cost):
- **Ferocity** (1, 5) — "Reduces the cost of your Maul, Swipe, Savage Bite, Claw, and Rake abilities by **[1/2/3/4/5] Rage or Energy**." Page: "Also reduces the cost of Savage Bite."
- **Feral Aggression** (2, 5) — "Increases the Attack Power reduction of your Demoralizing Roar by [8/16/24/32/40]% and the damage caused by your **Ferocious Bite by [3/6/9/12/15]%**."
- **Sharpened Claws** (10, 3) — "Increases your critical strike chance while in Bear, Dire Bear or Cat Form by **[2/4/6]%**."
- **Predatory Strikes** (13, 3) — "Increases your **melee attack power in Cat, Bear, and Dire Bear Forms by [3/6/10]%**. In addition, increases the damage caused by your Claw, Rake, **Maul, Swipe**, and Savage Bite abilities by [7/14/20]%."
- **Blood Frenzy** (14, 2) — "Increases the duration of Tiger's Fury by [6/12] sec, and causes Enrage to instantly generate [5/10] Rage. In addition, Tiger's Fury and Enrage increase your attack speed by [10/20]% for [9 sec/18 sec]." Page: "Completely rebuilt."
- **Leader of the Pack** (25, 1) — "While in Cat, Bear or Dire Bear Form, the Leader of the Pack increases ranged and melee **critical chance of all party members within 45 yards by 3%**."
- **Feral Charge** (9, 1) — "Causes you to charge an enemy, immobilizing and interrupting any spell being cast for 4 sec."
- **Brutal Impact** (5, 2) — "Increases the stun duration of your Bash and Pounce abilities by [0.5/1] sec." Neither Bash nor Pounce is in the sim's kit.

**Removals that matter:** "Feline Swiftness (replaced by Feral Swiftness), Savage Fury, and Faerie Fire (Feral), which is now baseline." Also gone from the payload: **Improved Enrage** (the page files it under Restoration removals: "Removed: Improved Rejuvenation, Improved Enrage, and Insect Swarm, which is now baseline").

## 5. What this costs in `src/WowRoguelike.Core/Content.fs`

Three buckets, cheapest first. **All 21 talents the build spends in are listed, each exactly once: 6 / 15 / 0.**

**Expressible today — data only (6).** Each needs a field or mechanism that already exists; four of the six are literally one number on the `Traits` record that `druid` already switches per spec (`Content.fs:3912-3915`).

| Talent | Concrete change |
|---|---|
| Feral Instinct 3/3 | `threatBp` for `BearDruid` (`Content.fs:3929`): `tankStanceBp` 50000 (`Content.fs:1650`) → **57500**. Lands on `Entity.ThreatMultiplierBp` (`Domain.fs:1277`, read at `SimState.fs:328`), so it multiplies *every* threat source the bear has — the only threat lever that also scales area threat. |
| Sharpened Claws 3/3 | `Traits.CritBonusBp = 600` on the Bear **and** Cat traits (`Content.fs:3914-3915`), consumed at `Content.fs:3232`. |
| Feral Swiftness 2/2 | `Traits.DodgeBonusBp = 400` (`Content.fs:3233`). The movement half is inert — the sim has no outdoor state. |
| Ferocity 5/5 | A `Spells` line with `CostBp = 3333` on "Maul" and "Claw": Maul costs 15 Rage (`Content.fs:257`), 15 × 6667 / 10000 = 10, which is the -5 the talent promises. Needs `withTalents` (`Content.fs:3461`) wired onto the Bear and Cat kits, which today bypass it (`Content.fs:3901` gives them `Talents.none`). |
| Feral Charge 1/1 | A new `AbilityDef` of `Effect.Charge` (`Domain.fs:869`) + `Interrupt` + `Root 4 sec`. Three existing effect cases. |
| Brutal Impact 2/2 | `Effect.Stun (duration + 1 s)` — but it has **no subject**: Bash and Pounce do not exist in the kit. Both are `Effect.Stun` / `Effect.Root` rows, so this is content, not vocabulary. |

**Needs a new model axis (15).**

| Talent | Missing axis |
|---|---|
| Thick Hide 3/3 | An **armour multiplier on item armour**. `Armor = d.Armor + armorBp + Agility*2 + traits.ArmorBonus` (`Content.fs:3234`) is all flat; `armorBp` comes from party buffs, not a percent. This is exactly the gap the existing `shortcut:` already names (`Content.fs:3908-3911`: "a per-spec armour multiplier is a new field, and a separate decision"). |
| Heart of the Wild 5/5 | **Per-form stat multipliers.** `IntellectBp` (`Domain.fs:1136`, folded at `Content.fs:3218`) covers the Intellect fifth. `MaxHealth = d.BaseHealth + pool s.Stamina 10` (`Content.fs:3228`) and `AttackPower = Strength*ApPerStrength + …` (`Content.fs:3237`) have no percent anywhere, so Bear Stamina +20% and Cat Strength +20% have no field. |
| Predatory Strikes 3/3 | Two: an **attack-power multiplier** (same line as above) and an **ability damage multiplier** — `SpellTalent` (`Domain.fs:1109-1118`) carries `CostBp`/`HealBp`/`CritBp`/`CastTicksOff` and **no damage field**. |
| Natural Weapons 3/3 | A **physical damage multiplier in all forms** (again no `SpellTalent` damage field) plus a **hit chance** — nothing in `Traits` or `Talents` feeds miss chance. |
| Feral Aggression 5/5 | A **damage bonus on one ability** (Ferocious Bite +15%). The Demoralizing Roar half has no subject — the ability is not in any kit. |
| Primal Fury 2/2 | A **crit-triggered resource gain**. `SimCombat.gainRage` exists but is called from damage paths only; nothing hooks a crit. |
| Ancient Brutality 1/2 | Two triggers: **on dodge → Rage over time** (needs a dodge-triggered aura; no aura is emitted by a dodge) and **on bleed tick → Energy** (no bleed aura exists — `Aura.Corrupted` is `Domain.fs:233`, unmitigated damage, and is not a bleed). |
| Carnage 1/2 | A **self-heal proportional to damage dealt**. `Effect.Heal` takes fixed min/max (`Domain.fs:810`); nothing reads back the damage just dealt. |
| Berserk 1/1 | Three: a **health multiplier buff** (`MaxHealth` is computed once at construction, `Content.fs:3228`), an **energy-regen multiplier aura**, and a **Fear removal** — there is no Fear aura to remove (`Aura` has `Stunned`/`Silenced`/`Rooted`, `Domain.fs:217-224`). |
| Leader of the Pack 1/1 | A **form-conditional party aura**. `PartyBuffKind` (`Domain.fs:1363-1378`) is a closed union applied at construction, has no crit member, and is not gated on the holder's form. |
| Blood Frenzy 2/2 | Tiger's Fury and Enrage are not in the kit; the axis it needs is an **attack-speed buff aura** — nothing modifies `AutoAttack.Ticks` after construction. |
| Omen of Clarity 1/1 | A **random proc on attack/spell**. `Empowers` + `Empower.FreeMana` (`Domain.fs:157-163`, `Domain.fs:839`) already carry the "next ability is free" half, but nothing rolls a chance on a swing. |
| Nature's Grasp 1/1 | A **reactive buff on the caster that fires when struck**. No effect in `Effect` reacts to being hit. |
| Improved Nature's Grasp 4/4 | Rides the same axis as Nature's Grasp; +65% of nothing until that exists. |
| Sylvan Blessing 2/2 | An **on-kill trigger** plus a **timed casting-regen override**. `RegenBp` (`Domain.fs:1138`) is a permanent constant; there is no kill hook. |

**Collapses or renames kit rows (0).** No talent in this build renames or collapses a sim kit row. The renames the port *does* force are at ability level, not talent level: `Maul` = `heroicStrikeRanks` and `Growl` = `tauntRanks` (`Content.fs:3946-3947`) both survive OctoWoW by name, and every feral talent that names a Cat button names one the kit already has (`Claw`, `Shred`, `Ferocious Bite`). What is *new* beside them is Swipe and Savage Bite — and the comment that justifies their absence, "Swipe is left out because the Warrior kit has no area ability to copy from: the only Cleave and Whirlwind in this file are mob abilities" (`Content.fs:3941-3943`), **is now false**: `Effect.MultiShot of maxTargets` exists (`Domain.fs:807`) and is implemented at `SimCombat.fs:406-427` — "weapon damage to the target and to up to `maxTargets - 1` other hostiles within 8 tiles of it".

## 6. The build decoded

Codec re-verified independently: 3 bits per slot, 28 slots per tree, base64, trees joined by `-`. **51 of 51.** Tree identities from the payload, as above.

```
Balance  (tree 0)  11 points   {1:1, 2:4, 3:2, 6:3, 10:1}
   1 Nature's Grasp 1/1 · 2 Improved Nature's Grasp 4/4 · 3 Sylvan Blessing 2/2
   6 Natural Weapons 3/3 · 10 Omen of Clarity 1/1
Feral    (tree 1)  40 points   {1:5, 2:5, 4:3, 5:2, 6:3, 8:2, 9:1, 10:3, 11:2, 13:3,
                                14:2, 16:1, 18:1, 21:5, 22:1, 25:1}
   1 Ferocity 5/5 · 2 Feral Aggression 5/5 · 4 Feral Instinct 3/3 · 5 Brutal Impact 2/2
   6 Thick Hide 3/3 · 8 Feral Swiftness 2/2 · 9 Feral Charge 1/1 · 10 Sharpened Claws 3/3
   11 Primal Fury 2/2 · 13 Predatory Strikes 3/3 · 14 Blood Frenzy 2/2
   16 Ancient Brutality 1/2 · 18 Berserk 1/1 · 21 Heart of the Wild 5/5
   22 Carnage 1/2 · 25 Leader of the Pack 1/1
Restoration (tree 2)  0 points   {}
```

**Every slot maps to a real talent and every full rank count matches the payload exactly** — 19 of 21 are filled to max. The two that are not are **partial spends, not mapping errors**: Ancient Brutality is 1 of 2 and Carnage is 1 of 2, which is what a 51-point budget forces (40 spent in Feral, and the last two points go where a row gate allows). Prerequisites hold: Improved Nature's Grasp needs Nature's Grasp 1/1, Omen of Clarity needs Natural Weapons 3/3, Primal Fury and Blood Frenzy need Sharpened Claws 3/3, Heart of the Wild needs Predatory Strikes 3/3.

**It genuinely serves both roles, and it is built the way a Classic feral off-tank is built.** The Feral 40 is not one-sided: defensive purchases are Feral Instinct (15% threat), Thick Hide (10% armour), Feral Swiftness (4% dodge), Heart of the Wild (20% Stamina in Bear), Primal Fury (Rage on crit), Ancient Brutality (Rage on dodge), Carnage (5% self-heal off Maul/Swipe/Savage Bite), Berserk (20% health) — 8 talents, ~20 points. Offensive purchases are Ferocity (5 off every button), Feral Aggression (15% Ferocious Bite), Sharpened Claws (6% crit), Predatory Strikes (10% AP, 20% Claw/Maul/Swipe), Blood Frenzy, Leader of the Pack, Feral Charge, Brutal Impact — 8 talents, ~20 points. **Leader of the Pack at slot 25 is the 31-point capstone and it is a party buff, not a personal one** — the build pays 40 points to hand the Warrior and the Rogues +3% crit, which is a group-support purchase no pure tank or pure dps build makes.

**What favours Cat:** Sharpened Claws, Predatory Strikes, Ferocity and Blood Frenzy all pay a Cat better than a Bear, and `Heart of the Wild`'s Cat half is +20% Strength against the Bear's +20% Stamina. **What favours Bear:** Thick Hide, Feral Instinct, Ancient Brutality's Rage-on-dodge, Berserk's health, and Carnage. The split is close to even; the build's real bias is that **it spends 7 of its 11 Balance points on talents that do nothing for either feral role** (Nature's Grasp, Improved Nature's Grasp, Sylvan Blessing — a reactive root and an on-kill mana-regen buff for a character that is neither being hit on purpose nor casting). Only Natural Weapons (3) and Omen of Clarity (1) earn their place. Those 7 are the price of reaching Natural Weapons and Omen through the row gate.

**Does it solve the operator's problem? No — and this is the load-bearing finding.** The operator's intent (verbatim): *"Druid tank and dps can share the same talent build … This will allow the Druid to act as off-tank if too many enemies are engagad and pressuring the Warrior."* The build shares fine. The off-tank part fails on one thing, and it is the same thing the sim already lacks:

> **OctoWoW gives the bear no new way to hold several enemies.** The page's *Changes to Baseline Spells* does not mention Swipe at all. There is no Swipe talent in the payload, no area-threat talent, and no change to Maul or Growl. The only two statements the page makes about Swipe are riders on other talents — Carnage, "Your Maul, **Swipe**, and Savage Bite abilities return [5/10]% of their damage as healing to you", and Predatory Strikes, "increases the damage caused by your Claw, Rake, Maul, **Swipe**, and Savage Bite abilities by [7/14/20]%" — both of which presuppose Swipe exists and neither of which widens it. The new bear button, Savage Bite, is single-target: "Savagely bite the target, dealing 80% normal damage plus an additional 30 damage. Generates a high amount of threat."

So the OctoWoW port does **not** close the sim's area-threat gap. What it does give the off-tank plan is three smaller things: **Feral Instinct's +15% threat applies to every source, so it is the one talent that scales area threat once Swipe exists**; **Carnage heals the bear 5% of Swipe's damage**, which is the survivability half of holding three mobs; and **Barkskin (Feral)** is a defensive cooldown with "Requires: Cat, Bear or Dire Bear Form" and no stated magnitude. The area ability itself is plain vanilla Swipe, which the sim does not have and which the `shortcut:` at `Content.fs:3941-3943` wrongly believes it cannot express.

**What the off-tank plan requires that the sim does not have:** (1) a multi-target melee `Ability` for the bear — expressible today as `Effect.MultiShot(3)`-shaped data, since `SimCombat.fs:406-427` already resolves "weapon damage to the target and up to `maxTargets - 1` hostiles within 8 tiles"; (2) Swipe's threat to route through `Entity.ThreatMultiplierBp` so Feral Instinct's 15% reaches it; (3) the bear's kit to be more than two buttons, which today it is not — Maul at 15 Rage against `gainRage`'s `2.5 * damage / level` (`docs/agents/domain.md:584-586`) leaves little for a third button, which is why Ferocity's -5 matters more here than its rank suggests.

## 7. Missing evidence / UNVERIFIED

- **Barkskin (Feral)'s damage reduction is never stated** — "Reduces Physical damage taken for the next several attacks" with no percentage and no charge count, and no payload record (it is level 44+, not a talent). **UNVERIFIED.**
- **Swipe's damage, target count and threat are stated nowhere in either source.** The page only names it inside two talent tooltips. Any Swipe row added to the sim must come from `.pi/research/ranks/druid.md`, not from OctoWoW. **UNVERIFIED from these two sources.**
- **Survival of the Fittest does not exist in either source** — 0 occurrences of "Survival" in the 135 KB payload and 0 on the page, despite being named in this brief. Whether OctoWoW removed it, renamed it, or folded it into Heart of the Wild is **UNVERIFIED**; it must not be ported on the strength of vanilla memory.
- **Frenzied Regeneration's conversion rate** is "a percentage of your total Stamina" with no percentage. **UNVERIFIED.**
- **The 5-points-per-row gate is not in the payload.** Records carry only explicit `requires` / `reqRanks` edges; the claim in §6 that the 7 Balance filler points are forced by row gating is inference from the slot layout, not payload evidence. **UNVERIFIED.**
- **Which of these are live on OctoWoW's current build** versus planned is not stated, as with Warrior and Priest.
- **Whether the calculator's tree is OctoWoW or vanilla-plus-partial** is inferred, not documented. Evidence for OctoWoW: Berserk, Carnage, Ancient Brutality, Open Wounds, Sylvan Blessing, Guidance of the Dream, Gale Winds and Aessina's Bloom are all in the trees and none exists in 1.12.

### Contradictions (summary page vs calculator)

1. **Survival of the Fittest is missing from both, and the page's removal list does not mention it.** The page's Feral removals are exactly "Feline Swiftness (replaced by Feral Swiftness), Savage Fury, and Faerie Fire (Feral), which is now baseline" — reading A: Survival of the Fittest is still a talent and the payload extraction missed it (it is not there: 18 records, slots 1,2,4,5,6,7,8,9,10,11,13,14,15,16,18,21,22,25); reading B: it was deleted and the page's list is incomplete. Unresolved; **do not port it**.
2. **Thick Hide's rank change and its magnitude disagree in spirit.** The page says "5 Ranks → 3 Ranks"; the payload's 3 ranks are [3/6/10]%, so the talent now caps at +10% where a 5-rank vanilla talent would have capped higher. Reading A: deliberately weakened to make room for Heart of the Wild's Stamina. Reading B: a straight rank compression at the same total. Nothing in either source settles it; the numbers above are the payload's and are what the sim should carry.
3. **Ancient Brutality and Carnage are 2-rank talents the build only half-fills**, while every other talent is maxed. Reading A: the 51-point budget ran out and 1 point was the best available in that row. Reading B: the codec's 3-bit slot values are misaligned for these two. B is refuted — the totals land on exactly 51 with all other 19 talents matching their payload maxima, so A holds.
4. **Improved Enrage is listed as a Restoration removal but is a Feral talent in vanilla.** "Removed: Improved Rejuvenation, Improved Enrage, and Insect Swarm" appears under §Restoration. Reading A: it was cut from the class entirely and filed wherever the writer was standing. Reading B: a page typo. Either way it is absent from the payload's Feral tree, so no action.

## 8. Not done

- Any change to `Content.fs`: this note is research.
- The Bear's Swipe row, and the `shortcut:` at `Content.fs:3941-3943` that it makes obsolete.
- Every **UNVERIFIED** item in §7.
- The Moonkin and Restoration halves of the Druid, which the sibling notes cover.
