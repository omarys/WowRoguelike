# Research: OctoWoW Warlock — class changes

Two sources, both read in full: **prose** — <https://octowow.st/warlock>, quoted throughout §1–§4; and
**numbers** — <https://octowow.st/talents/warlock/?points=FoASaASABBFAB--FAVYAo>, the server-rendered
RSC payload (131,001 chars, recovered in 5 slices), whose talent records
(`{"i":…,"ranks":…,"requires":…,"reqRanks":…,"reqBy":[…],"tooltip":[…]}`) were parsed out. Per-rank
values below are copied from its `"values":[…]` arrays. Status: **prose complete, build numbers
complete**; the page prints almost no coefficient, so anything not in a `values` array is
**UNVERIFIED**.

Tree names and maxima from the calculator's own headers: **Affliction 44**, **Demonology 48**,
**Destruction 51**. Grid is 4 per row × 7 rows = 28 slots per tree — the codec's 28 — confirmed
independently by the page's own row moves (§4).

---

## 1. New abilities

| Ability | Cost / cooldown (as stated) | Effect (as stated) |
|---|---|---|
| **Dark Harvest** | "Learned at Level: 40, three ranks at 40 / 50 / 60", "Cooldown: 30 sec" | "Reap the target's life for heavy Shadow damage over its channel. While channeling, the time between ticks of your Affliction spells on that target is reduced. If the target dies during the channel, the cooldown resets. The Affliction capstone talent." |
| **Mana Funnel** | "Learned at Level: 40, second rank at 50" | "Channel mana to your demon each second: the mana counterpart to Health Funnel." |
| **Demon Gate** | "Learned at Level: 60", "Cost: 80% of base mana", "Cooldown: 30 min" | "Opens a gate into the Twisting Nether that summons a Felguard under your command for 3 min. Once control is lost the Felguard attacks you until one of you dies." |

Dark Harvest's rank-1 tooltip carries the only hard number: "dealing 704 Shadow damage over 8 sec",
and "the time between periodic ticks of your Affliction spells on the target is reduced by 30%".

## 2. Class mechanics

**The section is one bullet.** "Changes to Class Mechanics" holds exactly one ★ entry — **Soul
Bags**: "A soul bag now gives every Soul Shard, Infernal Stone and Demonic Figurine you spend a
chance not to be consumed. The chance scales with the bag: 5% for a Small Soul Pouch, 10% for a Box
of Souls, 15% for a Soul Pouch, 20% for a Felcloth Bag and 25% for a Core Felcloth Bag." Everything
else that reads as a mechanic change is filed under Talents instead:

- **Soul shards** — only the bag refund. No change to how shards are earned or spent is stated.
- **Curses** — no §2 entry. Malediction makes "Curse of Agony … able to be active alongside your
  other curses, except Curse of Doom"; Improved Curse of Weakness now "improves the attack speed
  reduction specifically".
- **Life Tap** — no §2 entry. Only "Increases the amount of Mana awarded by your Life Tap spell by
  10/20%."
- **Demons** — no §2 entry at all. Every demon change is a talent or a new ability: Soul Link
  reworked, Power Overwhelming, Soul Entrapment, Nether Studies, Improved Stones, Soul Funnel, Demonic
  Aegis, Sinister Pursuit, Unholy Power, Demonic Sacrifice, Master Demonologist, Fel Domination, Mana
  Funnel, Demon Gate. **The largest block of the class sits entirely outside the sim.**

## 3. Baseline spell changes (verbatim numbers)

| Spell | Change |
|---|---|
| **Soul Fire** | "Learned at level 38, with four ranks. Mana cost 335 → 260 and cooldown 60 sec → 30 sec, and it now benefits significantly from spell damage." |
| **Hellfire** | "Mana cost 645 → 160." |
| **Inferno** | "Cooldown 60 min → 10 min, cost 100% → 80% of base mana." |
| **Ritual of Doom** | "Cooldown 60 min → 30 min, cost 100% → 80% of base mana, it needs fewer participants, and the Doomguard arrives under your command rather than having to be enslaved instantly or turning on the group. It also no longer sacrifices a random participant." |
| **Conflagrate** | "Consumes 3 sec of your Immolate rather than the whole thing, so using it no longer wipes your damage over time effect." |
| **Searing Pain** | "No longer causes a high amount of threat, which makes it usable outside of tanking situations." |
| **Siphon Life** | "Transfers health over the full duration rather than in fixed per-tick amounts." |

**None of these seven is in the sim's warlock kit.** The kit is Shadow Bolt, Corruption, Curse of
Agony, Immolate, Fear — `AbilityDef.kitAt` at `src/WowRoguelike.Core/Content.fs:4243-4245`.

## 4. Talent changes per tree

Row / rank counts in **bold** are the calculator's; the rest quotes the page.

**Affliction.** **Dark Harvest** (Row 7, **1**) is the new capstone and "replaces Dark Pact".
**Malediction** (Row 5, **1**) lets "Curse of Agony … be active alongside your other curses, except
Curse of Doom", and applying Recklessness / Shadows / Elements "also applies your highest rank of
Curse of Agony". **Soul Siphon** (Row 4, **3**) raises Drain Soul / Dark Harvest / Death Coil damage
"for each Affliction effect you have on the target, up to four". **Rapid Deterioration** (Row 5,
**2**) adds that "casting speed effects also speed up your damage over time ticks" — payload
prerequisite `requires 17, reqRanks 1` (Siphon Life). **Improved Drains** (Row 2, **2**) merges
Improved Drain Soul + Improved Drain Life + Improved Drain Mana. **Resilient Shadows** (Row 2, **3**)
"Reduces the chance your Affliction spells are dispelled". **Fel Concentration** goes **5 → 2 ranks**
and "Covers all your Affliction spells rather than only the three channeled Drains". **Nightfall**
(Row 3, **2**) "Also procs from Dark Harvest and your Drain spells, not just Corruption and Drain
Life". **Improved Curse of Weakness** **3 → 2 ranks**. **Curse of Exhaustion** Row 5 → **Row 3**, and
Improved Curse of Exhaustion **4 → 2 ranks**. **Removed:** Dark Pact, Amplify Curse, Improved Drain
Soul, Improved Drain Life, Improved Drain Mana. Unlisted, presumed unchanged: Suppression (**1**,5),
Improved Corruption (**2**,5), Improved Life Tap (**6**,2), Improved Curse of Agony (**8**,3), Grim
Reach (**12**,2), Siphon Life (**17**,1), Shadow Mastery (**21**,5).

**Demonology — the pet tree, entirely out of the sim's reach.** One ★ bullet, "Your demon scales
with your gear", covers four talents: "Fel Stamina adds a share of your Stamina and reduces your
demon's chance to be critically struck. Fel Intellect (5 Ranks → 3) adds a share of your Intellect
and lets its mana regenerate while casting. Demonic Precision (Row 5, 3 Ranks: new) passes on your
hit and crit. Unleashed Potential (Row 6, 3 Ranks: new) lets your demon benefit from your Fire and
Shadow spell power." **Soul Link** (Row 7 capstone) is reworked — any demon, and Greater/Enslaved
demons "can no longer break free while it is active". **Power Overwhelming** (Row 5, **1**) "Channels
energy into your demon, breaking all crowd control on it and increasing its damage, at the cost of
damaging it over the duration". **Soul Entrapment** (Row 1, **3**) "Increases all your damage while
you have no demon under your control, which makes going petless a real option". **Nether Studies**
(Row 4, **3**) replaces Improved Imp + Voidwalker + Succubus — "One talent improving Seduction,
Torment, Blood Pact and Tainted Blood together". **Improved Stones** (Row 4, **2**) replaces Improved
Healthstone + Firestone + Spellstone and "Covers Felstone, Wrathstone, Voidstone and Spellstone, plus
Firestone's crit bonus". **Soul Funnel** (Row 2, **2**) replaces Improved Health Funnel, "scaling off
your demon's missing health or mana". **Demonic Aegis** (Row 2, **3**) "Increases the effectiveness of
your Demon Armor". **Sinister Pursuit** (Row 1, **2**) "Increases your demons' movement speed".
**Master Summoner** (Row 4, **2**) "Also reduces the cooldown of Inferno, Ritual of Doom and Demon
Gate". **Unholy Power** moved within Row 4, **5 → 3 ranks**, and hits "all your demons rather than
three named ones". **Demonic Sacrifice** Row 5 → **Row 3**; its Imp effect is now all spell damage
"rather than Fire damage only", and the buff "is canceled by enslaving a demon as well as summoning
one". **Master Demonologist** moved within Row 6; its Imp effect "reduces your spell costs instead of
your threat". **Fel Domination** (Row 3, **1**) "Applies to any Common demon summon rather than a
fixed list of four". **Removed:** Improved Imp, Improved Voidwalker, Improved Succubus, Improved
Healthstone, Improved Firestone, Improved Spellstone, Improved Health Funnel, Improved Enslave Demon.

**Destruction.** **Shadow Vulnerability** (Row 1, **5**) replaces Improved Shadow Bolt: "Shadow Bolt
and Drain Soul have a chance to increase Shadow damage taken by the target, with a higher chance on
critical hits, rather than only ever proccing from Shadow Bolt crits". **Improved Soul Fire** (Row 5,
**2**) is new — "Soul Fire has a chance to refund a Soul Shard and increase your Fire damage".
**Demonic Swiftness** (Row 2, **2**) merges Improved Firebolt + Improved Lash of Pain. **Aftermath**
goes **5 → 3 ranks** and "Also increases Immolate's periodic damage, and its proc slows the target by
50% rather than dazing it". **Intensity** Row 4 → **Row 3** and "Covers all your Fire spells rather
than only Rain of Fire, Hellfire and Soul Fire". **Pyroclasm** Row 5 → **Row 4**; "Conflagrate can
now trigger the stun as well". **Improved Immolate** (Row 5, **5**) hits "Immolate's total damage
rather than only its initial hit". **Bane** (Row 2, **5**) "Also reduces the cast time of Searing
Pain". **Destructive Reach** (Row 4, **2**) "Also increases the radius of Hellfire". **Shadowburn**
(Row 3, **1**) moved within Row 3. **Removed:** Improved Shadow Bolt, Improved Firebolt, Improved
Lash of Pain.

## 5. What this costs in `src/WowRoguelike.Core/Content.fs`

The builder is `let private warlock` at `Content.fs:4208` and it passes `Talents.none`
(`Content.fs:4218`). `type Talents` (`Domain.fs:1128`) is a **healer** record — `HealDoneBp`,
`HealCostBp`, `IntellectBp`, `RegenBp`, `CritArmorBp`, `HealingWayBp`, `SpiritToHealingBp`,
`TotemEffectBp`, `Spells: SpellTalent list`, `CooldownOff` — and `type SpellTalent`
(`Domain.fs:1109`) is `{ Spell; CostBp; HealBp; CritBp; CastTicksOff }`, with **no damage field**. So
no warlock talent is "just a talent entry"; each is a rank-table edit or a new field/case.

**(a) Expressible today — 8 of the 17 talents in the build.** **Improved Corruption** (5/5, −0.3…1.5 s
Corruption cast) and **Bane** (5/5, −0.5 s Shadow Bolt and Immolate) are cast-time edits:
`SpellTalent.CastTicksOff` already exists, or bake them into `corruptionRanks` (`Content.fs:539`) and
`shadowBoltRanks` / `warlockImmolateRanks` (`Content.fs:512`, `Content.fs:585`) — 3.0 → 2.5 s and
2.0 → 1.5 s. **Devastation** (5/5, +5% Destruction crit) is the one talent needing no new field at
all: `SpellTalent.CritBp`. **Fel Concentration** (2/2, 35/70% chance not to lose casting time) is
exactly `Traits.PushbackResistanceBp` (`Domain.fs:1092`), already set per hero — `healerTraits` at
`Content.fs:3637` is the precedent; the warlock passes `Traits.none`. **Grim Reach** (2/2, +20%
Affliction range) is `Rank.Range`, a plain int: 8 → 10 tiles. **Improved Curse of Agony** (3/3, +10%
CoA damage) and **Shadow Mastery** (5/5, +10% Shadow) bake into `curseOfAgonyRanks`
(`Content.fs:564`) perTick 21 → 23 and across `shadowBoltRanks` / `corruptionRanks`. **Aftermath**
(3/3, +6% Immolate periodic) bakes into `warlockImmolateRanks`; its slow half is (b), below.

Caveat on the whole bucket: baking a talent into a rank table changes **every** warlock, not just
this build. The file already does this for the 1.15.9 cap values, so it is in keeping, but it loses
the talent's identity, and `Talents` needs a `DamageBp` field before the difference is expressible as
a talent.

**(b) Needs a new model axis — 9 of the 17, axis named.**

| Talent (spent) | Axis it needs |
|---|---|
| **Suppression** (5/5) | **Spell hit / resist.** There is no resist or miss model anywhere in `Domain.fs` — nothing for −10% resist to reduce. |
| **Improved Life Tap** (2/2) | **Health → mana conversion.** Already a documented hole, `Content.fs:4204-4207`: "Life Tap has no effect in the vocabulary (nothing moves health into mana) and is left out for the same reason." Life Tap is not a kit row. |
| **Improved Drains** (2/2) | **Channelled drain.** Drain Soul / Drain Life / Drain Mana are not modelled; `DrainResource` moves a resource *off the target* and does not heal the caster. |
| **Nightfall** (2/2) | **Proc-on-tick.** The destination is nearly free — `Empowers of duration * empower: Empower` with `Instant` and `SureCrit` already models "next Shadow Bolt is instant and cannot miss" — but nothing fires it off DoT ticks. |
| **Siphon Life** (1/1) | **Lifesteal.** "Transfers 150 Health from the target to the caster over 30 sec" is damage on the foe *plus* healing on the caster; `Effect` has no case that targets both. |
| **Malediction** (1/1) | **Per-source DoT identity.** The sharpest one: `Content.fs:561-563` documents the `shortcut:` — "every damage-over-time shares one `Corrupted` aura, so Corruption and Curse of Agony refresh each other instead of stacking. Split the aura by source if the warlock ever needs both on one target." Malediction is that "if". |
| **Dark Harvest** (1/1) | **Channel** (8 s, damage split across it) **plus DoT tick-rate**. `Rank.CastTicks` is a cast, not a channel; `DamageOverTime(duration, damagePerTick)` has a fixed period, so "time between ticks reduced by 30%" has nowhere to go. Also needs a cooldown-reset-on-kill hook. |
| **Shadow Vulnerability** (5/5) | **School-specific damage-taken multiplier.** `SpellDamage` is explicitly unmitigated ("mitigated by nothing (Q2a)", `Domain.fs:809`); no aura raises shadow damage taken. |
| **Demonic Swiftness** (2/2) | **Demon.** "Reduces the casting time of your Imp's Firebolt spell by 0.3/0.5 sec and reduces the cooldown of your Succubus' Lash of Pain spell by 3/6 sec." There is no Imp and no Succubus. **Flagged: the build takes this talent.** |

Aftermath's slow half also lands here in spirit: `Crippled of remaining * slowBp` exists
(`Domain.fs:248`) and `Aura.movementSlowBp` reads it (`Domain.fs:446`), but the only writer is
Crippling Poison at `SimCombat.fs:1308`, so "4/7/10% chance to slow 50% for 5 sec" is one new
`Effect` case rather than a new axis.

**(c) Collapses or renames kit rows — 0 of the 17.** Nothing in this build renames or deletes a row
of the sim's warlock kit. The merges the page describes — Improved Drains (three Drains), Demonic
Swiftness (Firebolt + Lash of Pain), Nether Studies, Improved Stones, Soul Funnel — all merge talents
that were never kit rows, and Dark Harvest "replaces Dark Pact", which was never a kit row either.
**The warlock is the opposite of the Warrior case: the cost is not row surgery, it is that 9 of 17
talents need a new axis and `Talents` has no record shape for a DPS class to hang any of them on.**

## 6. The build decoded — `FoASaASABBFAB--FAVYAo`

```
Affliction   31 points  {1:5, 2:5, 6:2, 7:2, 8:3, 9:2, 12:2, 13:2,
                         17:1, 19:1, 21:5, 25:1}       = 31 / 44
Demonology    0 points  {}                             =  0 / 48
Destruction  20 points  {1:5, 4:2, 5:5, 6:3, 10:5}     = 20 / 51
= 51 of 51
```

Affliction: Suppression 5/5 (2/4/6/8/10 %), Improved Corruption 5/5 (0.3…1.5 sec), Improved Life Tap
2/2 (10/20 %), Improved Drains 2/2 (5/10 %), Improved Curse of Agony 3/3 (3/6/10 %), Fel
Concentration 2/2 (35/70 %), Grim Reach 2/2 (10/20 %), Nightfall 2/2 (2/4 %), Siphon Life 1/1 (150
health over 30 sec), Malediction 1/1, Shadow Mastery 5/5 (2/4/6/8/10 %), **Dark Harvest 1/1** (704
Shadow over 8 sec; −30% Affliction tick interval). Destruction: Shadow Vulnerability 5/5 (2/4/6/8/10 %
chance → +20% shadow taken for 10 sec), Demonic Swiftness 2/2 (0.3/0.5 sec Firebolt; 3/6 sec Lash of
Pain), Bane 5/5 (0.1…0.5 sec; Soul Fire 0.4…2 sec), Aftermath 3/3 (2/4/6 % periodic; 4/7/10 % slow
50% / 5 sec), Devastation 5/5 (1/2/3/4/5 % crit).

**The 31-point talent is Dark Harvest** — Affliction row 7, 1 rank, gated behind `requires 21,
reqRanks 5` (Shadow Mastery 5/5), which the build has.

**Taken against what the sim has.** Eleven touch a modelled ability (Improved Corruption, Improved
Curse of Agony, Grim Reach, Fel Concentration, Nightfall, Shadow Mastery, Dark Harvest, Shadow
Vulnerability, Bane, Aftermath, Devastation). Four touch nothing the sim has at all — Improved Life
Tap, Improved Drains, Siphon Life, Demonic Swiftness — and Malediction is half-moot, its stacking half
needing a second curse the kit does not carry.

**Left on the table.** All of Demonology (0/48 — Soul Link, Demonic Sacrifice, Master Demonologist,
Unholy Power, the whole pet package). Affliction: Resilient Shadows, Improved Curse of Weakness,
Curse of Exhaustion, Improved Curse of Exhaustion, Soul Siphon, Rapid Deterioration. Destruction:
Cataclysm, Intensity, Shadowburn, Pyroclasm, Destructive Reach, Improved Searing Pain, Improved Soul
Fire, Improved Immolate, Ruin, Emberstorm, Conflagrate.

**Does the build need a demon the sim does not have? Yes — one talent does.** Demonic Swiftness at
2/2 is in the build and does nothing but shorten the Imp's Firebolt cast and the Succubus' Lash of
Pain cooldown. The build is otherwise pet-free by design (Soul Entrapment, the petless talent, is not
taken either). Note `Domain.fs:772` and `Domain.fs:888` name "a Warlock's demon" as an intended user
of `Summon`, while the builder's comment (`Content.fs:4204-4207`) says the Imp and Voidwalker are
deliberately still out — not in conflict, but only the second is currently true.

## 7. Missing evidence / UNVERIFIED

- **Almost no coefficients on the summary page.** "heavy Shadow damage", "a percentage of", "benefits
  significantly" — the only hard number in the prose is Conflagrate's "3 sec". Every number here came
  from the calculator's `values` arrays. What the payload also omits is **UNVERIFIED**: Dark Harvest's
  per-rank damage and cooldown beyond rank 1, and the Felguard's stats.
- **Soul Shard economy is not described.** §2 gives the bag refund and nothing else; whether shards
  are still consumed by summons or by Soul Fire is not stated. **UNVERIFIED.**
- **Improved Curse of Agony's tooltip is blank** in the payload where every other talent has a
  description paragraph, and the prose never mentions the talent. The only evidence is the `values`
  array 3/6/10, so "increases the damage done by your Curse of Agony" is inferred from the name.
  **UNVERIFIED.**
- **The page lists changes only.** Talents absent from it — Suppression, Improved Corruption,
  Improved Life Tap, Improved Curse of Agony, Grim Reach, Siphon Life, Shadow Mastery, Cataclysm,
  Devastation, Improved Searing Pain, Ruin, Emberstorm — are presumed unchanged from whatever baseline
  the calculator uses. Which baseline that is (OctoWoW live, or vanilla-plus-partial) is **inferred,
  not documented**; evidence for OctoWoW is that Dark Harvest, Soul Siphon, Rapid Deterioration,
  Resilient Shadows, Malediction and Improved Drains all exist in the tree and none exists in 1.12.
  **Whether they are live on OctoWoW's current build** versus planned is not stated anywhere.
- **Fel Domination** (Demonology slot 9, 1 rank) appears as a bare RSC tooltip rather than a
  `{"i":…}` record, so Demonology's rank sum reads 47 against a stated maximum of 48. It does not
  affect the build (0 points there).

## Contradictions between the page and the calculator

1. **Siphon Life is filed under "Changes to Baseline Spells" on the page but is an Affliction talent
   in the calculator** — slot 17, row 5, 1 rank, and the prerequisite for Rapid Deterioration
   (`reqBy: [16]`). "Transfers health over the full duration rather than in fixed per-tick amounts"
   describes a talent's effect, not a baseline spell's. Reading the calculator: a talent. Reading the
   page: a spell change.
2. **Aftermath's row.** The page records only "5 Ranks → 3 Ranks" and no row move, but the calculator
   places it at Destruction slot 6 — **row 2**, where vanilla has it in row 1. Every other row move
   the page states (Intensity 4→3, Pyroclasm 5→4, Curse of Exhaustion 5→3, Demonic Sacrifice 5→3,
   Soul Link to row 7, Shadowburn within row 3) matches the payload exactly, so this is a page
   omission rather than a parse error.
3. **"Your demon scales with your gear" is one ★ bullet** covering four talents (Fel Stamina 5, Fel
   Intellect 3, Demonic Precision 3, Unleashed Potential 3) with no per-talent line, and it never
   states Fel Stamina's rank count at all (the payload shows 5). The calculator is the only place that
   split is visible — the page alone cannot build the tree.

## Not done

Eight other classes; and no change to `Content.fs` — this note is research, and the class layer
rewrite is the parent item in `docs/agents/handoff.md`.
