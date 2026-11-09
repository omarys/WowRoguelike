# Research: OctoWoW Priest — class changes

Source of every prose claim below: **<https://octowow.st/priest>**, fetched and read in full. Every numeric value is quoted from that page or from the server-rendered talent payload at **<https://octowow.st/talents/priest/?points=AoFAB-aoFAAYToRYAoB->** (130,254 bytes of HTML; 52 records of shape `{"i":slot,"index":tree,...}`, 18 Discipline / 17 Holy / 17 Shadow). No number here is filled in from memory or from vanilla 1.12.

Status: **prose complete, numbers partial.** The summary page prints almost no coefficients ("Causes instant Shadow damage", "Heals a percentage of the damage taken"); where a number is absent it is marked **UNVERIFIED** rather than guessed. The payload does carry per-rank values for 40 of the 52 records, quoted verbatim below.

---

## 1. New abilities

| Ability | Cost / cooldown | Effect (as stated) |
|---|---|---|
| **Improved Shadowform** (passive) | — | "Reduces the mana cost of your Shadow spells by 15% while in Shadowform." Confirmed gone from the tree — no longer a talent point. |
| **Pain Spike** | level 60, 15 sec | "Causes instant Shadow damage, but the damage dealt begins healing itself back over 5 sec after it lands. Burst that has to be followed up." |
| **Shadow Mend** | — | "Heals a friendly target, but damages you for 50% of the amount healed. Generates reduced threat. A Shadow Priest's only direct heal." |

**New racials** — "Most are the familiar Classic racials, but a few are new to OctoWoW: Dwarves learn Avatar, Night Elves gain Searing Shot, and the Tauren Priest arrives with Spiritwalk and Light of An'she." Avatar: "Turns a friendly target to stone, increasing their armor and all resistances for a short time." Searing Shot: "Fires a searing arrow for weapon damage plus additional Fire damage." Spiritwalk: "Enter the spirit realm, becoming invisible for the duration." Light of An'she: "Blasts the target with light, reducing their chance to hit and burning them for Holy damage over time." Fear Ward is de-raced: "(Fear Ward, once a Dwarf-only prayer, is now trained by Priests of every race, so it no longer appears here.)"

## 2. Class mechanics

**Shadowform stops being a Holy lock-out, for one spell.** "Holy Nova can be cast while you are in Shadowform. It is not free: casting it in Shadowform also deals Holy damage to you." The payload states the rule from the spell side: Holy Nova is "the one Holy spell Shadowform does not lock out, at the cost of damaging you."

There is **no Chakra.** The words chakra, Atonement and Penance do not appear on the page; the only form-shaped mechanic is Shadowform's lock-out and the only resource-shaped mechanic is nothing — the Priest spends mana and nothing else. Any "chakra-style shape" in a port would be invented, not researched.

## 3. Baseline spell changes

| Spell | Change (verbatim) |
|---|---|
| Divine Spirit | "Baseline for every Priest from level 30. It is also far cheaper (Rank 3: 785 → 340, Rank 4: 970 → 445) and gains a fifth rank at level 60." |
| Lightwell | "Baseline from level 30, and rebuilt so that nobody has to click it. Whenever you heal a friendly target with Lesser Heal, Flash Heal, Heal or Greater Heal, the Lightwell heals another injured ally nearby. Cooldown 10 min → 5 min." |
| Smite | "Costs 40 mana and gains a ninth rank." |
| Holy Nova | "Costs 180 mana and can be cast in Shadowform: it is the one Holy spell Shadowform does not lock out, at the cost of damaging you." |
| Prayer of Healing | "Heals your target and their party, so you can heal a different group in the raid." |
| Blessed Recovery | "Heals a percentage of the damage taken after a melee or ranged critical hit." (Now baseline rather than a talent.) |

Payload numbers: Holy Nova — "27 to 31 Holy damage to all enemy targets within 10 yards and healing all party members within 10 yards for 54 to 62", ids #15237. Blessed Recovery (still a Holy talent in the payload, 3 ranks) — "heal [8/16/25]% of the damage taken over 6 sec".

## 4. Talent changes

Rows are not modelled anywhere below; only **rank counts, numbers and names** matter to this sim.

### Discipline (tree 0)
- **Chastise** (slot 25, 1 rank, requires Enlighten 1/1) — "Row 7, 1 Rank: new capstone, replaces Power Infusion". Payload: "causing 139 to 160 Holy damage … disoriented for up to 2 sec … attack and casting speed is increased by 13% for 8 sec, increased to 12 sec if the damage results in a critical strike."
- **Enlighten** (17, 1) — "Row 5, 1 Rank: new". Payload: "10% chance with helpful spells, or 15% chance with harmful spells, to burn them for 4% of their total health as Holy damage and increase both your and their spell damage and healing by 10% for 8 sec … If cast on yourself … 15%."
- **Resurgent Shield** (18, 1, requires Imp. PW:S 3/3) — "Row 5, 1 Rank: new, takes Divine Spirit's slot". Payload: "25% of its base mana cost is refunded and increases damage and healing done by Holy spells and effects by up to 10% of the shield's total absorption amount for 8 sec."
- **Purifying Flames** (13, 2) — "Row 4, 2 Ranks: new". Payload: Holy Fire "has a [50/100]% chance to increase your Holy damage by 12% for 10 sec."
- **Blessed Concentration** (6, 2) — "Row 2, 2 Ranks: replaces Martyrdom".
- **Piercing Light** (1, 3) — "Row 1, 3 Ranks: new"; "-[2/4/6]%" resist.
- **Searing Light** — "Moved from Holy to Discipline Row 4, **2 Ranks → 3 Ranks**: reworked". Payload: crits have "[33/66/100]% chance to cause your next Smite spell to be instant cast and cost 60% less mana. Lasts 10 sec."
- **Mental Strength** — "**5 Ranks → 3 Ranks**: reworked"; "+[3/6/10]% Intellect and [1/2/3]% casting speed".
- **Mental Agility** — "Row 4 → Row 1"; "Also reduces the cost of your offensive Holy and Discipline spells, not just instants." 5 ranks, "[2/4/6/8/10]%".
- **Improved Inner Fire** — "Row 4 → Row 3, **3 Ranks → 2 Ranks**"; "+[15/30]%".
- **Wand Specialization** — "**5 Ranks → 2 Ranks**"; "Covers Bows as well as Wands, adds hit chance, and gives those attacks a chance to restore mana." Payload adds "+[13/25]% damage and [5/10]% hit … restore 60 mana".
- **Force of Will** (5 ranks) — "Also increases the amount absorbed by your Power Word: Shield" ("[4/8/12/16/20]%" absorb on top of 1-5% offensive crit).
- **Removed:** "Power Infusion, Martyrdom, and Divine Spirit, which is now baseline. Improved Mana Burn moved to Shadow."
- Unchanged and still at vanilla rank counts — evidence the tree is vanilla-plus-patch, not a rebuild: Inner Focus 1, Silent Resolve 5, Improved PW:Fortitude 2, Improved PW:Shield 3, Meditation 3, Unbreakable Will 5.

### Holy (tree 1)
- **Ascendance** (25, 1, requires Spirit of Redemption 1/1) — "Row 7, 1 Rank: new capstone, replaces Lightwell". Payload: "purges all forms of crowd control … Healing spells cost 33% less mana and their cast time is reduced by 20%. Allies healed gain Apotheosis, increasing healing received by 15% for 10 sec. Lasts 30 sec."
- **Reservoir of Light** (18, 3) — "Row 5, 3 Ranks: new"; Lightwell's Splendor of Light "+[33/66/100]%".
- **Book of Prayer** (16, 2) — "Row 5, 2 Ranks: replaces Improved Prayer of Healing"; "regain [15/30]% of its mana cost if it is different from your previous healing spell".
- **Empowered Recovery** (12, 2, requires Improved Renew 3/3) — "Row 4, 2 Ranks: new"; "duration of Renew by 3 sec and … healing done by your healing spells by [3/6]% to targets affected by Renew".
- **Divinity** (2, 5) — "Row 1, 5 Ranks: replaces Holy Specialization"; crit "+[1/2/3/4/5]%".
- **Holy Focus** (1, 2) — "Row 1, 2 Ranks: replaces Healing Focus"; "[35/70]% chance to avoid interruption caused by damage while casting **any Holy spell**", not just heals.
- **Improved Healing** (13, 3) — "Also reduces the mana cost of Prayer of Healing"; "-[5/10/15]%".
- **Holy Reach** — "Row 4 → Row 2"; "Also extends the range of Chastise"; "+[10/20]%".
- **Spirit of Redemption** — "Also increases your total Spirit while alive, so it works before you die as well as after"; payload: "+10% Spirit … becomes the Spirit of Redemption for 10 sec … can cast healing spells at no cost".
- **Spell Warding** — "**5 Ranks → 3 Ranks**, moved within Row 2"; "-[3/6/10]%".
- **Removed:** "Holy Specialization, Healing Focus, Improved Prayer of Healing, and Lightwell, which is now baseline".

### Shadow (tree 2)
- **Vampiric Touch** (18, 2, requires Vampiric Embrace 1/1) — "Row 5, 2 Ranks: replaces Improved Vampiric Embrace"; "+[2/5]% healed … all party members to also gain mana equal to [1/2]% of any Shadow spell damage you deal".
- **Shadow Weaving** (15, 5) — "Reworked. Your Shadow spells grant you a stacking Shadow Weaving buff, and your spells then apply a vulnerability scaled by how many stacks you hold." Payload: "[20/40/60/80/100]% chance … 3% per stack … Stacks up to 5 times."
- **Spirit Tap** (1, 5) — "Also procs from a Mind Blast critical strike, not only from killing blows." Payload: "[20/40/60/80/100]% chance to gain a 100% bonus to your Spirit after killing a target … or critically striking a target with Mind Blast."
- **Improved Fade** — "Row 4 → Row 3"; "Also reduces the threat returned to you when Fade ends"; "-[3/6] sec cooldown, -[25/50]% threat returned".
- **Improved Mana Burn** (13, 2) — "Moved out of Discipline into Shadow."
- **Shadow Reach** — "**3 Ranks → 2 Ranks**"; "+[10/20]%".
- "Improved Mind Blast moves up to Row 1, Blackout down to Row 2."
- Present but unmentioned by the page: Improved Psychic Scream (8, 2), Mind Flay (10, 1, "75 Shadow damage over 3 sec and slowing their movement speed by 50%"), Silence (16, 1, requires Imp. Psychic Scream 2/2), Shadowform (25, 1, "+15% Shadow damage, -15% Physical damage taken").

## 5. What this costs in `src/WowRoguelike.Core/Content.fs`

Three buckets, cheapest first. All sixteen talents the build spends in are listed.

**Expressible today — data only (8).** Each is a field that already exists on `Talents` (`src/WowRoguelike.Core/Domain.fs:1128`) or `SpellTalent` (`Domain.fs:1109`), folded at construction by `withTalents` (`Content.fs:3463`) or at heal time by `Talents.healBp` (`Domain.fs:1179`).

| Talent | Concrete change |
|---|---|
| Spiritual Healing 5/5 | `HealDoneBp = 1000` → **`3000`** (`Content.fs:3446`). Largest one-line change in the port. |
| Spiritual Guidance 5/5 | `SpiritToHealingBp = 2500` — already correct (`Content.fs:3448`). |
| Inspiration 3/3 | `CritArmorBp = 2500` — already correct (`Content.fs:3449`). |
| Improved Healing 3/3 | `CostBp = 1500` on "Lesser Heal"/"Heal" — already correct (`Content.fs:3451`). OctoWoW adds Prayer of Healing to the list; that spell is not in the kit. |
| Inner Focus 1/1 | Already shipped as the `innerFocus` ability (`Content.fs:3527`). |
| Mental Agility 5/5 | Three new `Spells` lines with `CostBp = 1000`: "Renew", "Shadow Word: Pain", "Smite". Slot moves to Row 1 — no cost, rows are not modelled. |
| Improved Renew 3/3 | One new `Spells` line, `HealBp = 1500` on "Renew"; `healBp` already adds a spell line onto `HealDoneBp`. |
| Divine Fury 5/5 | `CastTicksOff = secTicks 0.5` on "Smite" and "Heal" (`smiteRanks` `Content.fs`, `healRanks` `Content.fs`) — exactly -0.5 s, which is **ten ticks**, not five: the sim runs **twenty ticks a second** (`secTicks 1.0` = 20, `ticks 5` = 0.25 s). This note originally said `ticks 5`; the port caught it, and any other note that writes a duration as a bare `ticks N` is off by the same factor of two. |

**Needs a new model axis (7).**

| Talent | Missing axis |
|---|---|
| Unbreakable Will 5/5 | A **crowd-control resist chance**. `Aura.Stunned`/`Silenced` exist (`Domain.fs:217`, `224`) but nothing rolls against them; `PushbackResistanceBp` is the only resist of any kind. |
| Holy Focus 2/2 | `PushbackResistanceBp` **does** exist — on `Traits` (`Domain.fs:1092`, consumed at `SimCombat.fs:462`) — but no `Talents` field reaches it. Since the era pushback model landed the field is worth something real: every damaging hit on a cast costs half a second unless the resistance roll says otherwise, and a cast absorbs two. Axis: `Talents.PushbackResistanceBp` folded where `healerTraits 500 7000` is called (`Content.fs:3722`). The Priest's 7000 already comes from that helper, so the row is a plumbing job, not a model one — but it only pays where the Priest is hit mid-cast, which is graveyard and not armory. |
| Empowered Recovery 2/2 | Two axes: a **per-spell HoT duration** (Renew's `HealOverTime(secTicks 15.0, …)` is hardcoded at `Content.fs:329`) and a **target-state healing modifier** ("to targets affected by Renew") — `healBp` is caster-side only. |
| Book of Prayer 2/2 | **Per-caster cast memory.** "different from your previous healing spell" needs the last heal's name on the actor; `ManaRefundOnCritBp` (`Domain.fs:1141`) is the nearest precedent and it is keyed on a crit, not on history. |
| Spirit of Redemption 1/1 | **A death state that still acts**, plus a tree-wide Spirit percent (there is `IntellectBp`, no `SpiritBp`). `SimCombat.fs:928` clears the traits at death and `Resurrect` (`Domain.fs:852`) is documented as "the only effect that may resolve against something that is already dead", so a Priest casting for ten seconds after dying inverts that invariant. |
| Reservoir of Light 3/3 | **Lightwell.** The sim has none, so the talent has no subject. Rebuilding it needs `Summon` (`Domain.fs:891`) plus a heal-triggered proc on Lesser Heal/Flash Heal/Heal/Greater Heal. |
| Ascendance 1/1 | Three at once: a **dispel of crowd control** (no effect in `Effect` removes an aura), a **timed caster buff** scaling cost and cast time 33%/20%, and **Apotheosis, a healing-received modifier on the healed target**. The last is the expensive one — `healBp` is read off the caster. |

**Collapses or renames kit rows (1 + the class-wide renames).** **Divinity** is the one build talent: it renames Holy Specialization, adds no numbers, and exposes that the `priestTalents` comment (`Content.fs:3435`) claims "Holy Specialization 5/5 (+5% crit)" for a bonus the record never carried — there is no tree-wide crit field and `Spells[].CritBp` is per spell. Outside the build, the same bucket takes three more: **Holy Focus** replaces Healing Focus, **Book of Prayer** replaces Improved Prayer of Healing, **Ascendance** replaces Lightwell — and the doc comment's `shortcut: Power Infusion is not modelled` (`Content.fs:3441`) is **obsolete**, because OctoWoW deletes Power Infusion. A diff that adds the new names without deleting the old ones leaves both live.

**Also to check, not modelled at all:** Power Word: Shield and therefore the whole Discipline shield line (Improved PW:S, Resurgent Shield, Force of Will's absorb rider) — `Domain.fs:1126` already names "a damage-absorbing shield, a spell-school lockout" as the two things the vocabulary deliberately cannot express. Prayer of Healing, Flash Heal, Greater Heal, Holy Nova, Mind Flay and Vampiric Embrace are names the sim does not have. `PartyBuffKind.DivineSpirit` exists but is annotated "out of reach at 24" (`Domain.fs:1366`); OctoWoW makes it baseline at 30, bringing it into reach the moment the encounter roster passes that ceiling.

## 6. The build decoded

Codec re-verified independently against the payload: 3 bits per slot, base64, `-`-separated, trailing zeros trimmed (`AoFAB` = 10 slots, `aoFAAYToRYAoB` = 26; the priest trees stop at slot 25). **51 of 51.**

```
Discipline (tree 0)  11 points   {2:5, 5:5, 9:1}
   2 Mental Agility 5/5 · 5 Unbreakable Will 5/5 · 9 Inner Focus 1/1
Holy       (tree 1)  40 points   {0:3, 1:2, 2:5, 5:5, 10:3, 12:2, 13:3, 14:5, 16:2, 17:1, 18:3, 22:5, 25:1}
   0 Improved Renew 3/3 · 1 Holy Focus 2/2 · 2 Divinity 5/5 · 5 Divine Fury 5/5
   10 Inspiration 3/3 · 12 Empowered Recovery 2/2 · 13 Improved Healing 3/3
   14 Spiritual Guidance 5/5 · 16 Book of Prayer 2/2 · 17 Spirit of Redemption 1/1
   18 Reservoir of Light 3/3 · 22 Spiritual Healing 5/5 · 25 Ascendance 1/1
Shadow     (tree 2)   0 points   {}
```

Prerequisites hold: Empowered Recovery needs Improved Renew 3/3 (taken), Ascendance needs Spirit of Redemption 1/1 (taken).

**It commits to Holy** — 40 of 51 points, and every point in that tree is a healing or mana talent. **Its 31-point talent is Ascendance** (slot 25, requires 30 spent in Holy; the build has 39 there before spending it).

**It is unambiguously a healing build.** Its only offensive purchases are Mental Agility's rider on offensive Holy spells and Divinity's crit; there is no Shadowform, no Mind Blast, no Vampiric Embrace and no point in tree 2 at all.

**Already modelled and taken:** Spiritual Healing, Spiritual Guidance, Inspiration, Improved Healing, Inner Focus — five of the eight cheap ones need no new code, only numbers.

**Already modelled and NOT taken — the port's real content:** the current `priestTalents` (`Content.fs:3444`) ships **Meditation 3/3 (`RegenBp = 1500`)** and **Holy Specialization 5/5**, and this build takes neither. Porting it drops `RegenBp` from 1500 to 0 and renames Holy Specialization to Divinity.

**Left on the table:** the entire Shadow tree; **Chastise** (the Discipline capstone — unreachable with 11 points); the whole Power Word: Shield line (Improved PW:S, Resurgent Shield, Force of Will); Holy Nova; Holy Reach; Spell Warding; Blessed Recovery; Piercing Light; Silent Resolve; Mental Strength; Enlighten; Searing Light; Purifying Flames; Wand Specialization; Blessed Concentration; Improved PW:Fortitude; Improved Inner Fire.

## 7. Missing evidence

- **Pain Spike's damage is never stated** — "Causes instant Shadow damage" with no range, and no payload record (it is level 60, not a talent). **UNVERIFIED.**
- **Shadow Mend's heal is never stated** — only "damages you for 50% of the amount healed". **UNVERIFIED.**
- **Improved Shadowform's rank/stack count** is not in the payload (it is not a talent any more), so "-15%" is taken as the whole of it. If it stacks, the page does not say. **UNVERIFIED.**
- **Lightwell's Splendor of Light magnitude** is missing: Reservoir of Light says "+[33/66/100]% effectiveness" of a heal the page never sizes. **UNVERIFIED.**
- **Improved Mana Burn's tooltip in the payload is a rendering placeholder** — the literal text `$L610$L611` — so its two ranks have no numbers at all. **UNVERIFIED.**
- **Improved Psychic Scream's tooltip is empty** in the payload (2 ranks, no values). **UNVERIFIED.**
- **Ascendance's Apotheosis** is "+15% for 10 sec" with no stack count and no statement of whether it refreshes. **UNVERIFIED.**
- **Which of these are live on OctoWoW's current build** versus planned is not stated, as with Warrior.
- **Whether the calculator's tree is OctoWoW or vanilla-plus-partial** is inferred, not documented. Evidence for OctoWoW: Chastise, Enlighten, Resurgent Shield, Piercing Light, Book of Prayer, Reservoir of Light, Ascendance and Vampiric Touch are all in the trees and none exists in 1.12.

### Contradictions (summary page vs calculator)

1. **Holy Nova is a talent, not a baseline spell.** The page files it under *Changes to Baseline Spells* ("Costs 180 mana and can be cast in Shadowform") — reading A: every Priest has it. The calculator has it as **tree 1, slot 11, 1 rank, no prerequisite**, ids #15237 — reading B: it costs a point. Vanilla Holy Nova was also a 1-rank Holy talent, so B is the conservative reading and A may be describing the effect rather than the acquisition.
2. **Smite's cost.** The page says "Costs 40 mana"; the sim's ladder stops at rank 6 for 185 mana (`Content.fs:1508`) and no rank in either source costs 40. The page never says **which rank** it means — reading A: a re-cost of one rank; reading B: rank 1 moving from 20 to 40. Neither is checkable from the two sources. Marked **UNVERIFIED**, deliberately not ported.
3. **Divinity's position.** The page calls it "Row 1, 5 Ranks"; the calculator's `reqBy` graph puts Improved Renew at slot 0 and Empowered Recovery's prerequisite on it, with Divinity at slot 2 and no dependants. Slots are not rows and nothing contradicts, but the "Row 1" label cannot be checked against the payload and must not be used to order anything.
4. **Unbreakable Will and Improved Psychic Scream** appear in the calculator's trees and are never mentioned in the page's talent lists. Reading A: unchanged vanilla talents, so no action. Reading B: missing prose. Recorded as an omission, not resolved.

## 8. Not done

- Eight other classes.
- Every **UNVERIFIED** item in §7.
- Any change to `Content.fs`: this note is research, and the class layer rewrite is the parent item in `docs/agents/handoff.md`.
