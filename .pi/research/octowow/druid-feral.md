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
- **Berserk** (18, 1; the tooltip states **no cooldown** — see §10.4) — "Removes all Fear effects and increases your energy regeneration rate by 100% while in Cat form, and increases your **total health by 20%** while in Bear form. After the effect ends, the health is lost. Effect lasts 20 seconds."

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
| Ferocity 5/5 | A `Spells` line with `CostBp = 3333` on "Maul" and "Claw": Maul costs 15 Rage (`Content.fs:257`), 15 × 6667 / 10000 = 10, which is the -5 the talent promises. It needs `withTalents` (`Content.fs:4263`) on the kit that carries it, and the cat's kit is now folded there (`Content.fs:5133-5137`) — but **the pick is not bought**, so no row uses this line. The Bear kit is still a bare `AbilityDef.kitAt`. |
| Feral Charge 1/1 | A new `AbilityDef` of `Effect.Charge` (`Domain.fs:869`) + `Interrupt` + `Root 4 sec`. Three existing effect cases. |
| Brutal Impact 2/2 | `Effect.Stun (duration + 1 s)` — but it has **no subject**: Bash and Pounce do not exist in the kit. Both are `Effect.Stun` / `Effect.Root` rows, so this is content, not vocabulary. |

**Needs a new model axis (15), and four of these rows have since landed for the cat build
(§10) — the table is kept as the bear build's planning record, not as a live gap list.**

| Talent | Missing axis |
|---|---|
| Thick Hide 3/3 | An **armour multiplier on item armour**. `Armor = d.Armor + armorBp + Agility*2 + traits.ArmorBonus` (`Content.fs:3234`) is all flat; `armorBp` comes from party buffs, not a percent. This is exactly the gap the existing `shortcut:` already names (`Content.fs:3908-3911`: "a per-spec armour multiplier is a new field, and a separate decision"). |
| Heart of the Wild 5/5 | **Per-form stat multipliers.** `IntellectBp` (`Domain.fs:1136`, folded at `Content.fs:3218`) covers the Intellect fifth. `MaxHealth = d.BaseHealth + pool s.Stamina 10` (`Content.fs:3228`) and `AttackPower = Strength*ApPerStrength + …` (`Content.fs:3237`) have no percent anywhere, so Bear Stamina +20% and Cat Strength +20% have no field. (Berserk's `Aura.Invigorated` now moves `MaxHealth` at runtime, but it is a temporary grant carried in the aura, not a Stamina multiplier.) |
| Predatory Strikes 3/3 | Two, and **the damage half has since landed** (§10): `SpellTalent.DamageBp` exists and the cat's Claw line reads `2000` (`Content.fs:5001`). What is left is an **attack-power multiplier** — same line as above. |
| Natural Weapons 3/3 | The damage half **has since landed** (§10): `Talents.DamageDoneBp = 1000` (`Content.fs:4990`). What is left is the **hit chance** — nothing in `Traits` or `Talents` feeds miss chance. |
| Feral Aggression 5/5 | **Landed for the cat** (§10): `SpellTalent{Spell="Ferocious Bite"; DamageBp=1500}` (`Content.fs:5007`). The Demoralizing Roar half still has no subject — the ability is not in any kit. |
| Primal Fury 2/2 | A **crit-triggered resource gain**. `SimCombat.gainRage` exists but is called from damage paths only; nothing hooks a crit. |
| Ancient Brutality 1/2 | Two triggers: **on dodge → Rage over time** (needs a dodge-triggered aura; no aura is emitted by a dodge) and **on bleed tick → Energy**. The bleed is on the target — `Aura.Bleeding` (`Domain.fs:293`) predates this commit, and `Effect.Bleed`/`Effect.BleedFinisher` (`Domain.fs:1249`, `:1258`) are the routes that now write it — but no bleed tick pays a resource: `ProcResult.GrantResource` pays Rage only. |
| Carnage 1/2 | A **self-heal proportional to damage dealt**. `Effect.Heal` takes fixed min/max (`Domain.fs:810`); nothing reads back the damage just dealt. |
| Berserk 1/1 | **Both halves landed** (§10.4): `berserkCat` is `Aura.Innervated` at +100% for 20 s (`Content.fs:559`) and `berserkBear` is `Aura.Invigorated` at +20% maximum health for 20 s (`Content.fs:576`), on one 3-minute cooldown. What is not modelled is the **Fear removal** — there is no Fear aura to remove (`Aura` has `Stunned`/`Silenced`/`Rooted`, `Domain.fs:217-224`). |
| Leader of the Pack 1/1 | A **form-conditional party aura**. `PartyBuffKind` (`Domain.fs:1363-1378`) is a closed union applied at construction, has no crit member, and is not gated on the holder's form. |
| Blood Frenzy 2/2 | Tiger's Fury and Enrage are not in the kit; the axis it needs is an **attack-speed buff aura** — nothing modifies `AutoAttack.Ticks` after construction. |
| Omen of Clarity 1/1 | A **random proc on attack/spell**. `Empowers` + `Empower.FreeMana` (`Domain.fs:157-163`, `Domain.fs:839`) already carry the "next ability is free" half, but nothing rolls a chance on a swing. |
| Nature's Grasp 1/1 | A **reactive buff on the caster that fires when struck**. No effect in `Effect` reacts to being hit. |
| Improved Nature's Grasp 4/4 | Rides the same axis as Nature's Grasp; +65% of nothing until that exists. |
| Sylvan Blessing 2/2 | An **on-kill trigger** plus a **timed casting-regen override**. `RegenBp` (`Domain.fs:1138`) is a permanent constant; there is no kill hook. |

**Collapses or renames kit rows (0).** No talent in this build renames or collapses a sim kit row. The renames the port *does* force are at ability level, not talent level: `Maul` = `heroicStrikeRanks` and `Growl` = `tauntRanks` (`Content.fs:3946-3947`) both survive OctoWoW by name, and every feral talent that names a Cat button names one the kit already has (`Claw`, `Shred`, `Ferocious Bite`). What is *new* beside them is Swipe and Savage Bite — and both of the objections recorded against them have fallen: the vocabulary objection ("Swipe is left out because the Warrior kit has no area ability to copy from: the only Cleave and Whirlwind in this file are mob abilities", `Content.fs:3941-3943`) is **false** — the shape exists as `Effect.MultiStrike of maxTargets` (`Domain.fs:1104`), resolved at `SimCombat.fs:756` through `meleeStrike` (`SimCombat.fs:397`) — and **Swipe itself is now landed** at rank 2 for level 24 and above: three bodies within 8 tiles of the target, twenty Rage, weapon roll per body, threat through the same `Entity.ThreatMultiplierBp` a swing uses.

**What is landed is the row, not the rotation.** The bear's AI presses Maul and never the sweep, and that is measured rather than pending: pressing Swipe on the body count alone left **2 of 12 level-60 seeds uncleared** (seeds 4 and 6, stopped at the 6000-tick cap with the pack alive and reset to full). The log of seed 4 names the mechanism — on one Tick the Mage polymorphs a Ravine Ravager and the bear's sweep lands on it, because both orders are decided from the same pre-Tick world, and `Aura.endsOnHostileAction` means the sweep wakes the sheep. A second rule (`no body in reach is held`) did not help, because the race is intra-Tick rather than in the visible state. With the press removed, the row's presence is inert: over the same twelve seeds the summaries are **byte-identical to the run with Swipe stripped**, tick for tick. It earns its press the day a Tank's decision can see the cast the party is about to make.

## 6. The build decoded (the bear build — the cat's is §10)

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

- Any change to `Content.fs` **from this note**: it is research. The cat build of §10 is
the exception — it landed as `ed70d53` and was written from this file's §1-§4.
- The Bear's Swipe row landed in `7a12e52`. What is still open is the AI that would
press it (§5, and `docs/agents/domain.md` §8).
- Every **UNVERIFIED** item in §7.
- The Moonkin and Restoration halves of the Druid, which the sibling notes cover.
- **The cat's rotation, and any measurement of it.** §10 is a kit and a talent record:
nothing picks between Claw and Shred, nothing decides when to spend combo points on Rip
or Ferocious Bite, and `PartyAi` has no cat branch at all. See `docs/agents/handoff.md`.

## 9. Threat, as the operator states it

The payload page this note is built from does not print a threat section. The
operator supplied the following on 2026-11-10 as the build's own numbers, and
`Content.fs` now cites this section for every one of them. They are recorded
here rather than in a tooltip the repo can fetch, so a later reader knows
exactly which magnitudes are sourced and which are the operator's.

- **Bear Form and Dire Bear Form threat bonuses are raised to 50%.** OctoWoW's
  balance pass, per the operator. The 1.12 ranks table carries no threat column
  for either form (`.pi/research/ranks/druid.md:146-147` lists `ApBuff` and
  `ArmorBuff` only), so this is the payload's number and not the era's.
- **Druid tanks can pull with Faerie Fire while in Dire Bear Form.** This is
  what makes the feral row's 5 yard range a pull tool rather than a melee
  button, and it is why `Content.fs` gives the feral Faerie Fire a 6 sec
  cooldown and no cost.
- **Demoralizing Roar generates threat on every enemy it hits**, and that is
  what makes it the pack-opening button rather than only an attack-power debuff.
- **The core threat abilities**, in the operator's words: Savage Bite (Primal
  Bite) on cooldown, Demoralizing Roar for the pack opening, Faerie Fire on the
  pull and refreshed, Swipe as the AoE builder, Maul only in surplus Rage, and
  Enrage popped on the pull to cover the low-Rage window.

**What this section does and does not license.** It licenses the two form
multipliers and the *roles* of six buttons. It does not supply a damage, a
duration or a per-application threat for any of them: those still come from
`.pi/research/ranks/druid.md` where it has a row, and stay marked as derived
where it does not. Feral Instinct's +15% is separate and cited at
`Content.fs` from §4 of this note (`.pi/research/octowow/druid-feral.md:59`).

## 10. The cat build — the feral damage build, and the kit that stopped being the Rogue's

`ed70d53`. The cat spec had been the Rogue's kit under feral names — Claw was
`sinisterStrikeRanks`, Shred `backstabRanks`, Ferocious Bite `eviscerateRanks` — under
`Talents.none` and `Traits.none`. `eviscerateRanks` bakes a Rogue's Improved Eviscerate
`×1.15` into every term, which this build never buys, so the copy was wrong by a talent
as well as by a name. The operator's build replaces the kit, the talents and the traits.

**The build:** <https://octowow.st/talents/druid/?points=BSAaAI-AoYDQaDSQIFQB-Ag> —
**Feral 36 / Balance 11 / Restoration 4 = 51 points.** The operator's first link was a
near-identical string, `…DSQAFQB-Ao`, and it is **superseded** by this one. The two
differ in exactly two slots and nowhere else: **Feral slot 18 (Berserk) 0 → 1** and
**Restoration slot 2 (Furor) 5 → 4**. Both strings total 51; the second buys the
capstone and pays for it out of Furor.

### 10.1 The decode

Codec: base64 alphabet, **two talent slots per character, three bits each, high slot
first**, one `-`-separated segment per tree in **Balance / Feral / Restoration** order.
It is validated the only way that counts — run against the bear build in §6 it
reproduces that decode slot for slot and totals its 51 points:

```
Balance   (tree 0)  11 points  {1:1, 2:2, 3:2, 6:3, 7:2, 10:1}
   1 Nature's Grasp 1/1 · 2 Improved Nature's Grasp 2/2 · 3 Sylvan Blessing 2/2
   6 Natural Weapons 3/3 · 7 Natural Shapeshifter 2/2 · 10 Omen of Clarity 1/1
Feral     (tree 1)  36 points  {2:5, 4:3, 7:3, 8:2, 10:3, 11:2, 13:3, 14:2, 15:2,
                                16:2, 18:1, 21:5, 22:2, 25:1}
   2 Feral Aggression 5/5 · 4 Feral Instinct 3/3 · 7 Open Wounds 3/3
   8 Feral Swiftness 2/2 · 10 Sharpened Claws 3/3 · 11 Primal Fury 2/2
   13 Predatory Strikes 3/3 · 14 Blood Frenzy 2/2 · 15 Improved Shred 2/2
   16 Ancient Brutality 2/2 · 18 Berserk 1/1 · 21 Heart of the Wild 5/5
   22 Carnage 2/2 · 25 Leader of the Pack 1/1
Restoration (tree 2)  4 points  {2:4}
   2 Furor 4/5
```

Fifteen of the Feral slots carry the bear decode's own names. The two that do not are
**7** and **15** — the only two Feral records the bear build never takes — and their
rank counts settle which is which: 3 ranks is **Open Wounds 3/3**, 2 is **Improved
Shred 2/2**, the two names the code's `catTalents` doc block uses.

Prerequisites hold against the payload's `requires`/`reqRanks` edges: Omen of Clarity
needs Natural Weapons 3/3, Primal Fury and Blood Frenzy need Sharpened Claws 3/3,
Heart of the Wild needs Predatory Strikes 3/3.

**The one partial spend is Furor at 4/5, and it is what the Berserk point cost.** The
bear build spends no Restoration at all, so the cat build's Restoration 4 is entirely
Furor, bought down from 5 to fund slot 18.

### 10.2 What landed, talent by talent

| Pick | Model | Where |
|---|---|---|
| Natural Weapons 3/3 | `Talents.DamageDoneBp = 1000` — "[3/6/10]% damage with physical attacks in all forms". `Talents.damageBp` reaches the weapon strike, the finisher and the white swing | `Content.fs:4990` |
| Improved Shred 2/2 | `SpellTalent{Spell="Shred"; DamageBp=1000; CostBp=2000}` — **the `CostBp` is DERIVED**: 20% of the row's 60 Energy is 12, and `2000` bp of 60 is 12 | `Content.fs:4992-4996` |
| Predatory Strikes 3/3 | Claw `DamageBp = 2000` — the payload's "[7/14/20]% damage caused by Claw, Rake, Maul, Swipe" at 3/3. **Rake is named by the same clause and gets no line**: most of Rake's damage is its bleed, and a percentage that has to reach the aura as well as the strike is a change to the bleed path, not a `Spells` line | `Content.fs:4998-5002` |
| Feral Aggression 5/5 | Ferocious Bite `DamageBp = 1500` — "[3/6/9/12/15]% damage to Ferocious Bite" at 5/5 | `Content.fs:5004-5008` |
| Sharpened Claws 3/3 | `Traits.CritBonusBp = 600` — "[2/4/6]% critical strike chance" | `Content.fs:5065` |
| Feral Swiftness 2/2 | `Traits.DodgeBonusBp = 400` — the dodge half only; the movement half is inert, nothing reads a speed bonus | `Content.fs:5065` |
| Berserk 1/1 | two abilities, not one line — §10.4 | `Content.fs:559`, `:576` |

`catTalents` is selected at `Content.fs:5046` and the two traits at `Content.fs:5065`.
**The kit is folded through `withTalents`** (`Content.fs:4263`), which the cat's kit
never was: before this commit a `Spells` line on a cat was inert, because the ability
the tick loop reads was built from the bare row. The fold happens once, at construction
(`Content.fs:5133-5137`), so Improved Shred's twelve Energy and the three damage lines
are already inside the `Ability`.

### 10.3 The kit

All five rows are the druid's own, out of `.pi/research/ranks/druid.md`, not renamed
Rogue rows:

| Row | Cost | Shape |
|---|---|---|
| Claw (`Content.fs:432`) | 45 Energy | 100% weapon plus a flat step per rank, one combo point. The builder, and the reason a cat spends its bar in 45s where a Rogue spends the same bar in 40s |
| Shred (`Content.fs:451`) | 60 Energy | 225% weapon plus a step, one combo point |
| Rake (`Content.fs:476`) | 40 Energy | 100% weapon plus a step, one combo point, and a nine-second bleed: totals 39/57/75/96 over four ranks, paid **4/6/8/10 a tick** |
| Rip (`Content.fs:499`) | 30 Energy | a finisher bleed linear in the points it spends — rank 6 (level 60) is **270 + 168 a point** over twelve seconds, paid 22 a tick at one point and 78 at five. The points are spent |
| Ferocious Bite (`Content.fs:525`) | 35 Energy | the finisher — rank 5 (level 60) is **199-259 with a step of 147** |

**Two derived things in that table, both marked in the rows themselves.**

Rake's bleed is a **DERIVED** per-tick figure: the ranks table states a *total* over
nine seconds and `Aura.Bleeding` (`Domain.fs:293`) pays a whole number of points a
tick, so the per-tick number is that total divided by nine — which pays **36, 54, 72 and
90** of the stated 39, 57, 75 and 96. What is lost is integer division's, and a
fractional accumulator would be a new field on `Aura` for three damage at rank 1.

Ferocious Bite is **deliberately not derived from `eviscerate`**, and that is the one
place the old copy was actively wrong: `eviscerateRanks` bakes Improved Eviscerate 3/3
into every term (`lo * 115 / 100`), so a copy would read 228-297 with a step of 169 at
rank 5 where the druid's table says 199-259 and 147. Feral Aggression's +15% is a
`Spells` line on the hero instead, so a reader can see where it came from. Eviscerate
also appends Ruthlessness' combo point after its finisher; Ferocious Bite does not,
which is why it carries one `Finisher` effect and no `ComboPoint`.

### 10.4 Berserk — one talent, two buttons

The payload's tooltip for Berserk (IDs #51415/#51416; talent record `i=18`, tree index
1), verbatim:

> Removes all Fear effects and increases your energy regeneration rate by 100% while in
> Cat form, and increases your total health by 20% while in Bear form. After the effect
> ends, the health is lost. Effect lasts 20 seconds.

The operator's own framing, and the one the implementation follows: **the cat half is a
Rogue's Adrenaline Rush, the bear half is a Warrior's Last Stand.** The two halves share
nothing but a name and a cooldown, so they are **two abilities** — `berserkCat`
(`Content.fs:559`) and `berserkBear` (`Content.fs:576`), one on each kit — rather than
one row with a form branch inside the effect resolver.

**The cat half** rides `Aura.Innervated`, the aura Innervate already uses, because both
are a statement about how fast a bar refills. `SimTick.advanceResource`
(`SimTick.fs:175-199`) pays Energy as `1 + 1 * boon / 10000` and **not** as
`base * (10000 + bp) / 10000`, and that is why the mechanism only works at exactly
100%: Energy is a one-point clock, a floored 1.5× of one point rounds back to one, and
a smaller bonus would round away entirely. `+100%` lands on a whole second point — two
Energy a window instead of one, which halves the time a drained bar takes to refill
(200 ticks to 100). The mana clock already honoured this aura; the energy clock did not.

**The bear half** needed `Aura.Invigorated` (`Domain.fs:359`), **the model's first
health-boosting aura**, and it is a field on the body rather than a derived function for
one reason: `MaxHealth` is a stored field, not a function of the holder's auras, so a
percentage cannot be recomputed on read. `Effect.Fortified` (`Domain.fs:1266`, resolved
at `SimCombat.fs:1311`) raises the maximum by `healthBp` once, when the ability
resolves, and **carries the absolute `granted` in the aura** because the undo has to
know the exact number to take away rather than recompute a percentage of a maximum that
already includes it. `SimTick.advanceAuras` (`SimTick.fs:19-47`) is the one place every
aura's lifetime ends, so it is the one place that can subtract: when the `Invigorated`
aura is gone it sets `MaxHealth = MaxHealth - granted` and `Health = min Health
MaxHealth`. **That clamp is the payload's "after the effect ends, the health is lost",
and it is what stops the cooldown being a heal** — a bear at full health reads full
health again afterwards, not 120%, and a bear wounded while buffed keeps exactly the
damage it took.

**The cooldown is 3 minutes, DERIVED.** The talent's tooltip states none. Adrenaline
Rush and Last Stand — the two abilities the operator named as the model for each half —
are both three-minute cooldowns, and that is where the number comes from rather than
from a table nobody has checked.

**Not modelled:** the Fear removal, because there is no `Aura.Fear` for the party to
shed — nothing in this model can be feared.

### 10.5 Not modelled, pick by pick

Each is named with its reason in the `catTalents` doc block (`Content.fs:4918-4986`);
the table is the same list put against the tree, so a reader can see the mechanism
rather than the prose:

| Pick | Why it has no line |
|---|---|
| Leader of the Pack 1/1 | a party-wide +3% crit aura, and `PartyBuffKind` has no crit entry |
| Heart of the Wild 5/5 | +20% Strength in cat form; `Talents` has `IntellectBp` and `HealthBp` and no `StrengthBp` |
| Furor 4/5 | a shift into cat grants Energy, and nothing pays on a shift — `TakeStance` sets an aura and the resource clock is never consulted about it |
| Open Wounds 3/3 | the payload's rewrite gives Rip more damage and Claw +30% per bleed on the target; nothing in the damage path reads the target's aura list, so "per active bleed" has nothing to count |
| Carnage 2/2 | the payload's rewrite makes Ferocious Bite refresh Rake and Rip; a finisher has no handle on an aura another ability applied, and the refresh would be a second lookup by name in the aura list |
| Blood Frenzy 2/2 | Tiger's Fury and Enrage are in no kit, and nothing modifies `AutoAttack.Ticks` after construction — `Aura.Hastened`'s cadence only ever touches a timer with `Range > 1` |
| Ancient Brutality 2/2 | no bleed tick pays a resource, and the other half needs a dodge-triggered aura |
| Feral Instinct 3/3 | bear threat; the cat has no threat row and `feralInstinctBp` is the bear's |
| Omen of Clarity 1/1 | a clearcasting proc, and there is no free-cast aura for Energy — `Empower.FreeMana` waives a mana cost |
| The Balance filler (1, 2, 3, 7) | a reactive root, an on-kill mana regen, a shapeshift cost cut: none has a subject. They are the price of reaching Natural Weapons through the row gate |
| Ferocity 5/5 | **not bought** — its five off Claw and Rake is what the cat spends on Improved Shred and Open Wounds instead |
| Primal Fury 2/2 | five Energy on a crit, and `ProcResult.GrantResource` pays Rage only. The bear keeps it |

### 10.6 One caution the code embodies

The cat and the bear share `nightElfDruidBase` (`Content.fs:2579`) and the same
derivation, but **not** the same outfit: at 60 the bear's set carries **98 Agility**
against the cat's **92** (`Content.fs:5209`, `:5211`). Crit and dodge are
`500 + Agility * 10` and `500 + Agility * 5` (`Content.fs:3751-3752`), so six Agility is
60 crit bp and 30 dodge bp — which is why the cat's advantage over the bear reads
**540 / 370** and not the full **600 / 400** the two traits declare. The test asserts the
difference (`the cat's build lands on the hero and the bear's does not move`), so the
smaller number is the intended one and not a rounding slip.

### 10.7 What this does not decide

**The rotation.** Combo points are built by Claw and Shred and spent by Rip and
Ferocious Bite, and nothing chooses between them: `PartyAi` has no cat branch, so the
AI presses whatever its generic melee orders rank. **Nothing here is measured either** —
`Content.defaultRoster` (`Content.fs:3554`) seats no cat, so no bench row and no tick
measurement sees this build at all, and the ladder figures for `Druid (Cat)` in
`docs/agents/handoff.md` were taken with the Rogue's kit. §10 is a kit and a talent
record, not a build that has been shown to work.
