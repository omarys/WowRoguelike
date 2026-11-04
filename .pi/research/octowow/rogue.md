# Research: OctoWoW Rogue — class changes
Source of every sentence below: **<https://octowow.st/rogue>**, fetched and read in full. Numeric
cross-checks come from **<https://octowow.st/talents/rogue/?points=aoYDI-FAroIAQoRCCAB->**'s
server-rendered talent payload (see §6). No number here is filled in from memory or from vanilla 1.12.
Status: **prose complete for Rogue, coefficients largely absent.** The summary page states intent
("a percentage of thrown weapon damage", "a percentage of melee damage") and almost never prints the
percentage. Per-rank numbers exist only in the payload and are marked **UNVERIFIED** against spell pages.
---
## 1. New abilities
| Ability | Cost / cooldown | Effect (as stated) |
|---|---|---|
| **Blade Flurry** (level 30, baseline) | 25 Energy, cooldown 4 sec | "Your attacks strike an additional nearby enemy. While active, your damage and Energy regeneration are reduced. Lasts until canceled." Now "a toggle you turn on for packs and off for single targets" |
| **Ghostly Strike** (baseline) | 40 Energy, 20 sec | "A strike dealing a percentage of melee damage that also increases your dodge chance, awarding a combo point" |
| **Flourish** | 20 Energy | "Finishing move that increases your chance to parry by an additional 20%, lasting longer per combo point": 1 pt 3 sec, 2 pts 4 sec, 3 pts 5 sec, 4 pts 6 sec, 5 pts 7 sec |
| **Deadly Throw** | — | "Hurl your thrown weapon at the target for a percentage of thrown weapon damage, interrupting spellcasting. Gains range and an off-hand poison application from the Throwing Weapon Specialization talent" |
| **Agitating Poison** | 30 min coat | "Each strike has a 20% chance to inflict Nature damage and cause additional threat: a threat poison" |
| **Shiv** | — | "An instant off-hand weapon attack that automatically applies the poison on your off-hand weapon, rather than waiting for a proc" |
| **Quickfooted** (passive) | — | "Your movement speed is increased, stacking with other movement speed effects" |
## 2. Class mechanics
**Combo points survive a target change.** "Changing target does not wipe your combo points. They stay
banked on the target you built them on, so you can swap away and come back to spend them. You only ever
hold one set at a time: building a point on a different target starts that target from zero and discards
whatever you had banked. Druids get the same change."
**Energy costs drop** (see §3) — a change to the constants, not the pool or the tick rate.
**Poisons gain a threat vial** (Agitating Poison) and two talents that change *application* rather than
damage: Efficient Poisons ("Reduces the chance your poisons will be resisted by [4/8/12]% and gives your
poison applications a [15/30/45]% chance to not consume a charge") and Improved Poisons ("Increases the
chance to apply poisons to your target by [3/6/10]%"). Charges are a new resource the page introduces.
**Stealth is still a pre-fight state, but now has after-effects**: Blackjack — "you have a [50/100]%
chance to return to stealth after using your Sap ability"; Camouflage — "reduces the chance enemies detect
you while stealthed"; Cloaked in Shadows — "Vanishing cloaks you and nearby party members in a shield
absorbing a share of your maximum health". Ghostly Strike became baseline, so it is no longer a talent.
## 3. Changes to baseline spells
| Spell | Change (verbatim) |
|---|---|
| Energy costs | "Sinister Strike costs 40 Energy at every rank, Eviscerate 30, and Rupture and Slice and Dice 20." |
| Backstab, Ambush, Ghostly Strike | "Scale from your melee damage." |
| Sprint and Evasion | "Both come off cooldown in 4 min at every rank." |
| Blade Flurry | Baseline (was a Combat talent); stat box gives "Cost: 25 Energy  Cooldown: 4 sec" |
No percentage is printed for Backstab, Ambush, Ghostly Strike or Deadly Throw. **UNVERIFIED.**
## 4. Talent changes
Ranks/rows below are read from the payload, not the page; where the page also gives them the two agree.
### Assassination (18 talents)
- **Noxious Assault** (Row 7, 1 rank) — new capstone: "Strikes with both weapons … and applies the poisons
  from both weapons. Awards combo points."
- **Envenom** (Row 5, 1) — new finisher: poison effectiveness and application chance, "lasting 12 to 28 sec
  depending on combo points spent".
- **Taste for Blood** (Row 4, 2) — new: "Extends Rupture's duration, and each Rupture increases your melee
  damage per combo point for its full duration".
- **Efficient Poisons** (Row 5, 3) — new. **Throwing Weapon Specialization** (Row 3, 2) — new.
- **Improved Blade Tactics** (Row 2, 3) — new, "replaces Improved Slice and Dice".
- **Vigor** — "Row 7 → Row 6, 1 Rank → 2 Ranks. No longer a capstone." Payload ladder is **[5/10] max Energy**
  plus a poison-application Energy grant; the page names only the Energy grant.
- **Lethality** — "Applies to all your combo point generating abilities." (5 ranks, unchanged)
- **Relentless Strikes** — "As well as restoring Energy, it increases the damage of your finishing moves for
  a duration, stacking." Payload: "20% chance per combo point to restore 20 energy and increase the damage of
  your finishing moves by 5% for 30 sec. Stacks up to 5 times."
- **Remorseless Attacks** — "The crit bonus after a killing blow applies to your next combo point generator".
- **Cold Blood** — moved within Row 5; "Also empowers Noxious Assault" (payload adds Evisc/SS/Backstab/Ambush).
- **Vile Poisons and Improved Poisons** — "5 Ranks → 3 Ranks each." Payload confirms 3/3.
- **Removed:** Improved Slice and Dice, Improved Kidney Shot. Improved Expose Armor moved to Subtlety.
### Combat (18 talents)
- "The four weapon specializations are gone — Dagger, Mace, Sword and Fist Weapon Specialization are
  replaced by" **Close Quarters Combat** (Row 5, 2 — "crit with Maces, Daggers and Fist Weapons", payload
  [2/5]%), **Hack and Slash** (Row 5, 2 — "extra attack with Axes and Swords", [2/5]%) and **Concussive
  Blows** (Row 4, **5** ranks — "chance to stun with a mace", [1/2/3/4/6]%).
- **Surprise Attack** (Row 5, 1) — new, "takes Blade Flurry's slot": "deals 25% of your Attack Power … Only
  useable after the target dodges and cannot be blocked, dodged or parried".
- **Blade Rush** (Row 6, 2) — new: "Increases your melee attack speed by [2/5]% and reduces the time between
  your Energy regeneration ticks by an amount equal to your Agility."
- **Weapon Expertise** — "Row 6 → Row 5. Covers Axe, Dagger, Fist, Mace and Sword." Payload [3/5] skill.
- **Improved Backstab** — "Moved within Row 2" (payload Row 2, 3): "+30% crit … and a [15/30/45]% chance to
  award an additional combo point."
- **Aggression** — "Also increases the damage of Riposte and Surprise Attack." **Opportunity** and
  **Setup** — "Both moved out of Subtlety and into Combat"; payload puts Opportunity at Row 1, not Row 3.
- **Removed:** Improved Sinister Strike, Endurance, and Blade Flurry (now baseline).
- Untouched and unmentioned by the page: Lightning Reflexes (Row 1, 5, +5% dodge), Riposte, Improved Sprint,
  Improved Kick, Dual Wield Specialization, Precision, Deflection, Adrenaline Rush (Row 7 capstone, 1 rank).
### Subtlety (19 talents)
- **Mark for Death** (Row 7, 1) — new capstone, "replaces Premeditation": party AP +30% and "magical damage
  by up to 18% of your melee attack power".
- **Tricks of the Trade** (Row 6, **5**) — "replaces Deadliness": party crit chance from openers and finishers.
- **Shadow of Death** (Row 5, 1) — new finisher: a sigil storing damage, cap "based on your attack power".
- **Honor Among Thieves** (Row 6, 2) — party crits grant payload **Energy [2/5]**.
- **Smoke Bomb** (Row 3, 1), **Cloaked in Shadows** (Row 4, 2), **Improved Ghostly Strike** (Row 3, 3),
  **Blinding Haze** (Row 4, 3), **Bloody Mess** (Row 5, 2) — new.
- **Blackjack** (Row 4, 2) — "replaces Improved Sap". **Serrated Blades** — "Row 4 → Row 2". **Improved
  Ambush** — "Row 3 → Row 2". **Camouflage** — "Row 2 → Row 1". **Hemorrhage** — "Row 5 → Row 3".
- **Removed:** Premeditation, Deadliness, Master of Deception, Sleight of Hand, Improved Sap, Heightened
  Senses, and Ghostly Strike (now baseline). Improved Expose Armor arrives from Assassination (Row 1, 2).
## 5. What this costs in `src/WowRoguelike.Core/Content.fs`
`let private rogue` is Content.fs:3765. Gear is inline (no shared `rogueDress`/`rogueWeapons`):
Assassin's Blade main hand **and** "(off hand)", `OffHand = true`, `FixedPool = Some 100`,
`Gear60 = (1488, (59, 236, 182, 0, 0))`. Weapon ladder `[ 1,20-39,1.9s; 28,24-45,1.5s; 33,26-49,1.4s;
38,26-49,1.4s; 40,26-49,1.4s; 60,53-100,1.6s ]`, all `Range = 1`, `Energy`, `partyThreatBp`.
`AbilityDef.kitAt level [ sinisterStrike; backstab; ambush; eviscerate; gouge; kick; sap; cheapShot;
kidneyShot ]` (Content.fs:3809). Six battle abilities, two openers, one finisher.
**Combo points are modelled.** `Effect` has `ComboPoint of count: int` (Domain.fs:882) and
`Finisher of min * max * perPoint` (Domain.fs:887); `Entity.ComboPoints` (Domain.fs:1264) is capped at 5
(SimCombat.fs:793) and zeroed by the finisher (SimCombat.fs:807); `PartyAi.fs:775` spends at `>= 4` points.
What is *not* modelled is a **duration that grows with combo points** — `kidneyShot` carries a recorded
`shortcut:` saying exactly that. So: combo points yes, combo-point-scaled *durations* no.
**(a) Expressible today — 6 of the build's 18.**
| Talent | Concrete change |
|---|---|
| Improved Eviscerate 3/3 | `+15%` on `eviscerate`'s `Finisher(lo, hi, step)` triplet |
| Malice 5/5 | `Traits.CritBonusBp = 500`, which already feeds `CritChanceBp` (Content.fs:3232) |
| Ruthlessness 3/3 | append `ComboPoint 1` **after** `Finisher` in `eviscerate.Effects` — both cases exist; order is load-bearing (807 zeroes, then 793 re-adds) |
| Improved Backstab (crit half) | `Traits.BackstabCritBonusBp = 3000`; `SimCombat.fs:367` already special-cases `ability.Name = "Backstab"` |
| Improved Kick 2/2 | append `Silence(secTicks 2.0)` to `kick.Effects` — `Silence` is already a case |
| Close Quarters Combat 2/2 | `+5%` crit, folded into `CritChanceBp`. Faithful only because the kit always wields a dagger |
**(b) Needs a new model axis — 9 of 18.** The axis is named in each row.
| Talent | Axis it needs |
|---|---|
| **Adrenaline Rush 1/1** (the 31-pointer) | **Temporary resource-regen multiplier.** Regen is one integer, `Entity.ResourceRegenPerTick` (`partyRegen = 1`, Content.fs:1658/1727); "100% … for 15 sec" needs an `Aura` carrying a bp multiplier the tick loop multiplies through. Nothing in `Effect` or the `Aura` union touches an *incoming tick rate*, and `Innervate` is a share of a friendly pool, not a rate |
| Relentless Strikes 1/1 | **Self resource refund + stacking self damage buff.** No `Effect` returns resource to the caster; `Innervate of duration * regenBp` targets a friendly pool |
| Remorseless Attacks 2/2 | **Consumable "next generator" crit charge.** Nearest existing case is `Empowers of duration * empower`, which is spell-shaped (Nature's Swiftness) |
| Opportunity 5/5 | **Positional/behind axis.** domain.md:1501 — "Stealth is not modelled, by design … there is no facing check"; `ambush` substitutes "a full health, untouched enemy" |
| Deflection 5/5 | **Self parry bonus.** `SwingChances.Parry` exists (SimCombat.fs:141) but no `Traits`/`Entity` field raises the defender's parry — only `DodgeBonusBp` exists |
| Dual Wield Specialization 5/5 | **Per-entity off-hand damage multiplier.** `OffHand = true` exists (Content.fs:3817) but off-hand damage is unscaled |
| Blade Rush 2/2 | **Per-entity haste.** Swing speed lives in the shared weapon ladder (`Ticks = secTicks 1.9`) |
| Precision 5/5 | **Hit bonus on `Entity`.** `SwingChances.Miss` exists but nothing on the hero moves it |
| Weapon Expertise 2/2 | **Weapon-skill axis.** Skill feeds only `levelCritBonusBp` (4 bp/point, SimCombat.fs:115) — +5 skill is ~20 bp crit and no glance change |
**(c) Collapses or renames kit rows — 3 of 18.**
| Talent | What it does to a row |
|---|---|
| **Improved Blade Tactics** 3/3 | It *renames* Improved Slice and Dice, but neither `Slice and Dice` nor `Flourish` exists in Content.fs. The row it edits is absent: adding it means adding two abilities first |
| **Riposte** 1/1 | A new kit row ("A strike that becomes active after parrying … 150% weapon damage and disarms the target for 6 sec") *and* a parry-event trigger — it lands here because it is a new row, but it cannot fire without the (b) parry axis |
| **Surprise Attack** 1/1 | A new kit row gated on the target dodging. The attack table already produces `Dodged` (SimCombat.fs:127) and nothing consumes it, so this is a new row plus a reactive trigger |
Note the asymmetry the build walks into: it *takes* Deflection (parry, no field) and *skips* Lightning
Reflexes (dodge, `Traits.DodgeBonusBp` already wired at Content.fs:3233). Cheapest first change: Lightning
Reflexes is a one-line field; Deflection is new plumbing.
## 6. The build decoded
`?points=aoYDI-FAroIAQoRCCAB-`, 3 bits/talent, 28 slots/tree, three base64 chunks, **51/51 spent**:
```
Assassination  17 points  {0:3, 1:2, 2:5, 4:3, 7:3, 8:1}
Combat         34 points  {1:5, 4:5, 5:3, 6:5, 8:1, 12:2, 14:5,
                           16:2, 17:1, 19:2, 21:2, 25:1}
Subtlety        0 points  {}
```
Slot→name verified against the payload for all 18:
`{0:3}` Improved Eviscerate 3/3 · `{1:2}` Remorseless Attacks 2/2 · `{2:5}` Malice 5/5 · `{4:3}`
Ruthlessness 3/3 · `{7:3}` Improved Blade Tactics 3/3 · `{8:1}` Relentless Strikes 1/1 —
`{1:5}` Opportunity 5/5 · `{4:5}` Deflection 5/5 · `{5:3}` Improved Backstab 3/3 · `{6:5}` Precision 5/5 ·
`{8:1}` Riposte 1/1 · `{12:2}` Improved Kick 2/2 · `{14:5}` Dual Wield Specialization 5/5 · `{16:2}` Close
Quarters Combat 2/2 · `{17:1}` Surprise Attack 1/1 · `{19:2}` Weapon Expertise 2/2 · `{21:2}` Blade Rush 2/2 ·
`{25:1}` Adrenaline Rush 1/1.
**The build commits to Combat** (34 of 51), takes **Adrenaline Rush** as its 31-point talent — "Increases
your Energy regeneration rate by 100% for 15 sec" — and leaves both other capstones unbought: Noxious
Assault (Assassination Row 7) and Mark for Death (Subtlety Row 7). Subtlety is empty, so Blackjack, Shadow of
Death, Tricks of the Trade, Cloaked in Shadows and Hemorrhage are all off the table. Also unbought in
Assassination: Envenom, Cold Blood, Seal Fate, Taste for Blood, Vigor, Efficient Poisons, Vile Poisons,
Improved Poisons, Lethality, Throwing Weapon Specialization. Unbought in Combat: Concussive Blows, Hack and
Slash, Aggression, Lightning Reflexes, Improved Sprint, Setup. Of the 18 it takes, **6 are expressible today
and the 31-pointer is not one of them.**
## 7. Contradictions
- **Noxious Assault's damage term.** Page: "Strikes with both weapons for damage plus 30% of your attack
  power". Payload: "Strikes with both weapons, dealing **30 damage** plus 30% of your Attack Power". Two
  readings: weapon-damage-based strike vs a flat 30 + AP. Unresolved.
- **Blade Flurry's "Cooldown: 4 sec"** against "a toggle you turn on for packs and off for single targets" /
  "Lasts until canceled". Either a 4 s duration or a mislabelled stat box; the page never says which.
- **Page vs this sim (not page vs page).** Page: "Sinister Strike costs 40 Energy at every rank, Eviscerate
  30". `sinisterStrike` (Content.fs:1028) is `Cost = 45` and `eviscerate` (Content.fs:1103) is `Cost = 35`,
  with a comment claiming "Energy costs do not vary by rank: Sinister Strike is 45 and Backstab 60". The
  OctoWoW values are 40 and 30; Backstab 60 is not mentioned by the page either way.
## 8. Missing evidence / UNVERIFIED
- **Every percentage on the page's ability descriptions** — Ghostly Strike, Deadly Throw, Throwing Weapon
  Specialization, Mark for Death's 18%, Shadow of Death's cap. Only talent ladders carry numbers. **UNVERIFIED.**
- **Poison charges.** Efficient Poisons' "chance to not consume a charge" presupposes a charge counter the
  page never defines (charges per coat? per application?). **UNVERIFIED.**
- **Rupture and Slice and Dice are priced** ("Rupture and Slice and Dice 20") but neither ability exists in
  Content.fs and the page never states their damage or duration. **UNVERIFIED.**
- **Two unlisted removals.** Initiative and Improved Feint are in vanilla's Subtlety tree, are absent from
  the calculator's tree, and are *not* named in the page's removal list. Treated as removed; not confirmed.
- **Whether the page reflects the live build or a plan** is nowhere stated.
- **Whether OctoWoW's tree or vanilla-plus-partial** — evidence for OctoWoW: `#52511` Surprise Attack,
  `#52507/#52508` Blade Rush, `#52518-52520` Efficient Poisons, `#52512/#52514` Honor Among Thieves and
  `#52714` Noxious Assault, none of which exist in 1.12. Assignment of the three trees to
  Assassination/Combat/Subtlety is inferred from the capstones and the page's row numbers, which agree.
## 9. Not done
- Eight other classes.
- Coefficients for anything in §8.
- Any change to `Content.fs`: this note is research, and the class layer rewrite is the parent item in
  `docs/agents/handoff.md`.
