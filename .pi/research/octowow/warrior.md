# Research: OctoWoW Warrior — class changes

Source of every sentence below: **<https://octowow.st/warrior>**, fetched and read in full. The
numeric cross-checks come from **<https://octowow.st/talents/warrior>**'s server-rendered talent
payload (see §6). No number here is filled in from memory or from vanilla 1.12.

Status: **prose complete for Warrior, numbers incomplete.** The summary page states intent
("gains a fifth rank", "Deals a percentage of weapon damage") and almost never prints a coefficient.
Coefficients must be taken from the talent payload or the spell pages, and are marked **UNVERIFIED**
where absent.

---

## 1. New abilities

| Ability | Cost / cooldown | Effect (as stated) |
|---|---|---|
| **Intervene** (level 60) | 10 rage, 30 sec | Run at high speed to a party member, intercepting the next melee or ranged attack against them |
| **Die by the Sword** (passive) | — | Increases parry chance while under Retaliation |
| **Master Strike** (Arms talent, Row 3) | — | Weapon damage plus a **main-hand-weapon-conditional** rider: Mace disorients until damaged, Sword disarms, Axe immobilises, Polearm "its own distinct effect" |

## 2. Class mechanics

**Rage generation is rewritten.** Rage per swing = damage dealt **plus a share based on weapon
speed**, explicitly tuned so armour mitigation costs less rage than a damage-only formula would. The
page states weapon speed is **rage-neutral**: equal-DPS slow and fast weapons generate the same rage.
This is a change to the generation *formula*, not to a constant.

## 3. Baseline spell changes

| Spell | Change |
|---|---|
| Slam | Percentage of weapon damage, gains a fifth rank, **usable while moving** |
| Bloodthirst | Damage + **35% of attack power**, and increases movement speed |
| Mortal Strike | Percentage of weapon damage (scales with the weapon) |
| Whirlwind, Cleave | Both scale from melee damage |
| Shield Slam | Damage modified by **attack power as well as shield block value** |
| Enrage | Damage bonus lasts its full duration |
| Thunder Clap | Usable in **Defensive Stance as well as Battle Stance** |

## 4. Talent changes

### Arms
- **Master of Arms** (Row 5, 5 ranks) — replaces Axe/Mace/Sword/Polearm Specialization. One talent
  adapting to the equipped weapon: axes give crit, maces ignore armour, swords give a chance at an
  extra attack, "and so on".
- **Master Strike** (Row 3, 1 rank) — new, replaces Anger Management.
- **Boundless Anger** (Row 5, 3 ranks) — new; raises maximum Rage above 100.
- **Improved Disciplines** (Row 5, 3 ranks) — new; reduces Retaliation/Recklessness/Shield Wall cooldowns.
- **Precision Cut** (Row 6, 3 ranks) — replaces Improved Hamstring; more Execute damage per extra rage.
- **Improved Slam** — moved Fury → Arms Row 6, **5 ranks → 2 ranks**; cuts Slam's GCD as well as cast time.
- **Sweeping Strikes** — applies to abilities as well as main-hand melee.
- **Improved Thunder Clap** — also increases damage, not only reduces rage cost.
- **Two-Handed Weapon Specialization** — **5 → 3 ranks**, also raises skill with all two-handers.
- **Improved Rend** — **3 → 2 ranks**.
- **Removed:** Axe/Mace/Sword/Polearm Specialization, Anger Management, Improved Hamstring.

### Fury
- **Blood Drinker** (Row 6, 3 ranks) — new; while under Enrage/Death Wish/Recklessness, attacks restore a share of max health.
- **Improved Shouts** (Row 3, 5 ranks) — merges Improved Battle Shout + Improved Demoralising Shout.
- **Battlefield Mobility** (Row 4, 2 ranks) — replaces Improved Intercept; one talent for Intercept/Intervene/Charge cooldowns.
- **Ravager** (Row 5, 3 ranks) — replaces Improved Cleave; Whirlwind cooldown and Cleave rage cost.
- **Improved Pummel** (Row 4, 2 ranks) — new; Pummel can slow, and spell-school lockouts last longer.
- **Dual Wield Specialization** — **Row 4 → Row 2**, also raises off-hand hit chance.
- **Unbridled Wrath** — extra-rage chance doubled with a two-hander.
- **Improved Berserker Rage** — also a chance to break movement-impairing effects.
- **Booming Voice** — increases the **area** of all shouts.
- **Flurry** — number of hasted swings scales with the talent.
- **Removed:** Improved Battle Shout, Improved Demoralising Shout, Improved Cleave, Improved Intercept.

### Protection
- **Defensive Tactics** (Row 6, 3 ranks) — new; while wearing a shield, keep part of Defensive Stance's threat bonus in other stances.
- **Gag Order** (Row 4, 2 ranks) — replaces Improved Sunder Armor; Shield Slam dispel chance, Shield Bash silence chance.
- **Improved Shield Slam** (Row 5, 2 ranks) — replaces Improved Shield Bash; −0.75 sec cooldown, extra block chance on the next attack.
- **Reprisal** (Row 5, 2 ranks) — new; more Revenge damage, chance to refund its rage.
- **Improved Intervene** (Row 3, 2 ranks) — replaces Improved Shield Block; chance of immunity to movement impairment after Intervene.
- **Shield Slam** — **Row 7 → Row 5**; explicitly no longer a capstone gate.
- **Shield Specialization** — blocking always generates rage.
- **Toughness** — also increases shield-absorbed damage.
- **Improved Revenge** — moved within Row 3, also reduces Revenge cooldown.
- **Improved Disarm** — **3 → 2 ranks**, reduces Disarm's cooldown.
- **Anticipation** — **5 → 3 ranks**.
- **Concussion Blow** — 20 sec cooldown, **no rage cost**, 3 sec stun.
- **Removed:** Improved Shield Block, Improved Sunder Armor, Improved Shield Bash.

## 5. What this costs in `src/WowRoguelike.Core/Content.fs`

Three buckets, cheapest first.

**Expressible today** — coefficient and rank-table changes only. Slam's fifth rank, Bloodthirst
"damage + 35% AP", Mortal Strike/Whirlwind/Cleave scaling from melee damage, Shield Slam taking AP on
top of block value, Concussion Blow's cost and cooldown, and every rank-count change (Two-Handed
5→3, Anticipation 5→3, Improved Slam 5→2, Improved Rend 3→2, Improved Disarm 3→2). These are edits to
`AbilityDef` rank ladders and coefficients; the model already carries them.

**Needs a new axis in the model, not a new entry.** **Master Strike** and **Master of Arms** are
both "behave differently depending on the equipped main-hand weapon". `Effect` is a closed union with
no weapon dispatch, and `AbilityDef` has one definition shared by every wielder of a rank band. So
this is either a per-entity substitution at cast time or a weapon-keyed variant table — a design
decision before it is an implementation. Four abilities in this class alone depend on it (Master
Strike's four riders, Master of Arms' four bonuses).

**Collapses kit rows.** Master of Arms merges four specialisations into one talent; Improved Shouts
merges two. Our kits are hand-written per rank, so a merge deletes entries rather than editing them —
cheap to do, but it changes what a rank band *is*, and a diff that adds Master of Arms without
removing the four specialisations would leave both live.

**Also to check, not yet modelled at all:** max Rage above 100 (Boundless Anger), shout radius
(Booming Voice), and Defensive Tactics' "keep part of the threat bonus in another stance" — that last
one interacts with the `Threat` model directly.

## 6. The build string codec (verified)

`?points=` is **3 bits per talent, 28 talents per tree, three `-`-separated base64 chunks**, each
padded to 14 chars with `A`; the packer trims trailing `A`s (and uses a trailing `=` form for the
cosmetic/screenshot path). Both directions live in the site bundle
`/talents/_next/static/chunks/app/%5Blocale%5D/%5Bclass%5D/page-ce9cdb06bf9c66c9.js` (`_H` unpack,
`LD` pack).

The operator's build `Y--VYFoKQSopQQYB` decodes to:

```
Arms        3 points  {0: 3}
Fury        0 points  {}
Protection 48 points  {0:2, 1:5, 2:3, 5:5, 6:5, 8:1, 9:2, 10:2,
                       12:2, 13:2, 14:5, 16:5, 17:1, 18:2, 20:2, 22:3, 25:1}
= 51 of 51
```

Index→name mapping verified against the site's own payload for four talents: Toughness (tree 2,
index 6, 5 ranks) = 5, Shield Slam (17, 1) = 1, Gag Order (12, 2) = 2, Improved Shield Slam (18, 2) = 2.

Two records taken verbatim from the payload, showing what a full parse yields:

- tree 0, index 0 — `ability_rogue_ambush`, ranks 3 — **Improved Heroic Strike**: "Reduc…"
- tree 0, index 10 — `ability_backstab`, ranks 3, `requires: 2`, `reqRanks: 2` — **Deep Wounds**:
  "Your critical strikes cause the opponent to bleed, dealing [20/40/60]% of your…" (IDs #12290, #12963)

The payload also carries `reqBy` (dependants) and a `spellIds` array per talent, so prerequisites and
Blizzard spell IDs are available without scraping tooltips by hand.

## 7. Missing evidence

- **No coefficients on the summary page.** "A percentage of weapon damage" without the percentage is
  not implementable; every such value must come from the payload or a spell page. **UNVERIFIED.**
- **Master Strike's Polearm rider** is described only as "its own distinct effect". **UNVERIFIED.**
- **Rage formula is described qualitatively.** "Damage dealt plus a share based on weapon speed" needs
  the actual share before it can be coded; a weapon-speed-neutral formula is testable, which is the
  good news — see §5.
- **Which of these are live on OctoWoW's current build** versus planned is not stated on the page.
- **Whether the calculator's tree is OctoWoW or vanilla-plus-partial** is inferred, not documented.
  Evidence for OctoWoW: `master_strike_1`, `ability_warrior_improveddisciplines` and
  `ability_warrior_decisivestrike` all appear in the Arms tree and none of them exists in 1.12. The
  calculator also shows an Arms talent at Row 6 with icon `ability_warrior_strengthofarms` and 3 ranks
  whose identity was **not** established — do not assume it is Precision Cut without checking.

## 8. Not done

- Eight other classes.
- Coefficients for anything in §7.
- Any change to `Content.fs`: this note is research, and the class layer rewrite is the parent item in
  `docs/agents/handoff.md`.
