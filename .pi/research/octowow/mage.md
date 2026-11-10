# Research: OctoWoW Mage — class changes

Sources: **<https://octowow.st/mage>** (prose, read in full, 5 489 chars) and
**<https://octowow.st/talents/mage/?points=TAFoLISYZKAoB--FADD>** (server-rendered talent payload,
134 011 chars, read in 5 slices). No number below is filled in from memory or from vanilla 1.12.
Where the page gives no number the line says **UNVERIFIED**.

Status: **prose complete, numbers partial.** 55 of 84 talent records were recovered (§7), but every
talent the build actually takes is among them, so §5 and §6 rest on verified records.

## 1. New abilities

| Ability | Cost / cooldown | Effect (as stated) |
|---|---|---|
| **Arcane Rupture** | level 20, 15 sec cd, no cost stated | "Rupture the target for Arcane damage, increasing the damage (and the mana cost) of your Arcane Missiles for a duration" — talent-granted, "Requires the Arcane talent of the same name" |
| **Arcane Surge** | level 32, 85 mana, 8 sec cd | "Increases your casting speed and spell damage for a short duration" |
| **Ritual of Refreshment** | — | "creates a refreshment table … Conjured Manna Biscuits: a group version of conjured food" |
| **Polymorph: Rodent**, **Polymorph: Draenei Homunculus** | — | join "the existing sheep and pig" |
| **Teleport/Portal: Theramore, Stonard, Alah'Thalas** | — | "adding three destinations to your spellbook" |

Payload gives Arcane Rupture's numbers: "dealing 101 to 114 Arcane damage and increasing the damage
and mana cost of your Arcane Missiles spell by 20% for 8 sec" (spell **#51949**).

## 2. Class mechanics

- **Ignite threat is split.** "When several Fire Mages feed the same Ignite, the threat from the
  burn is split into equal shares between the Mages sharing it, so no single Mage carries the whole
  stack." A per-source threat rule, not a constant.
- **Arcane Missiles become interval-based.** "The missiles fire on a defined interval, which
  Accelerated Arcana then speeds up."
- **Arcane Power becomes a resource you steer.** "Increases your casting speed while draining a
  percentage of your maximum mana every second and reducing all mana gain. Fall below 10% maximum
  mana and you violently combust." Accelerated Arcana "makes casting-speed effects also speed up
  your Arcane cooldown recovery and your Arcane Missiles ticks" — casting speed is a stat here, not
  a fixed cast time.
- **Resistance matters again.** Three taken talents (Arcane Subtlety, Arcane Focus, Magic
  Absorption) are resistance modifiers; the page's Arcane section is built around them.
- No mana-pool, armour-spell or school change is stated. **Barriers:** only Ice Barrier, and only
  "also increases your Frost damage while the shield holds, on top of absorbing damage and
  preventing interrupts."

## 3. Baseline spell changes (numbers verbatim)

- **Arcane Intellect** — "Mana cost drops sharply at the higher ranks (Rank 3: 520 → 240, Rank 4:
  945 → 340, Rank 5: 1510 → 445)."
- **Arcane Brilliance** — "Available at level 42 as well as level 56, and the level 56 rank costs
  1200 mana."
- **Fireball** — "Cheaper at every rank, increasingly so at the top (Rank 11: 395 → 275, Rank 12:
  410 → 295)."
- **Arcane Missiles** — interval-based, see §2. No coefficient, cast time or damage figure is given
  for any baseline spell. **UNVERIFIED.**

## 4. Talent changes per tree

Tree names from the payload: **tree 0 = Arcane, tree 1 = Fire, tree 2 = Frost** (verified — slot 0/0
is Arcane Subtlety, tree 1 carries Improved Fireball/Impact/Ignite/Pyroblast, tree 2 carries
Improved Frostbolt/Frostbite/Ice Shards/Ice Barrier). Format: name (slot, ranks) — page text.

### Arcane
- **Arcane Power** (25, 1) — "Row 7 capstone: completely reworked". Payload: "casting speed is
  increased by 30% while draining [X]% of your maximum mana every second and reducing all mana gain
  by 50%. Falling below 10% maximum mana causes you to violently combust … killing you instantly.
  Lasts 20 sec and cannot be cancelled." (**#12042**; drain % unrecoverable — **UNVERIFIED**)
- **Arcane Instability** (16, 3) — "Row 6 → Row 5: completely reworked"; "an [16/…/25]% chance to
  uncontrollably erupt, consuming 2% of your base mana to deal 25% more damage" (#15058-60)
- **Resonance Cascade** (22, 5) — "Row 6, 5 Ranks: new"; "a [12/16/20]% chance to duplicate for 50%
  of the damage. This effect can trigger off of itself"
- **Arcane Rupture** (10, 1) — "Row 3, 1 Rank: new, replaces Arcane Resilience"
- **Temporal Convergence** (14, 3) — "Row 4, 3 Ranks: new"; requires Arcane Rupture 1
- **Accelerated Arcana** (18, 1) — "Row 5, 1 Rank: new, replaces Arcane Mind"
- **Arcane Potency** (19, 2) — "Row 5, 2 Ranks: new"; "critical strike damage bonus of your Arcane
  spells by [100?]%" (#45403, #45404)
- **Arcane Impact** (9, 3) — "Row 3, 3 Ranks: replaces Improved Arcane Explosion"; "critical strike
  chance of your Arcane spells by an additional [10]%" (#11242, #12467, #12469)
- **Magic Absorption** (1, 3) — "Row 2 → Row 1, **5 Ranks → 3 Ranks**"; "Increases all resistances
  by [10] and whenever one of your spells is partially or fully resisted you restore [mana]";
  "Internal cooldown 1 sec → 2 sec"
- **Magic Attunement** (8, 1) — "**2 Ranks → 1 Rank**"; "Amplify Magic and Dampen Magic … by 100%
  and allows them to be cast on enemies. Only affects targets level 62 or lower" (#11247)
- **Arcane Focus** (5, 5) — "Row 1 → Row 2". **Arcane Concentration** (6, 5) — "Has an 8 sec
  internal cooldown between procs" (payload: "10% chance … Clearcasting … reduces the mana cost of
  your next damage spell by 100%")
- **Arcane Subtlety** (0, 2) — not on the page; payload: "Reduces your target's resistance to all
  your spells by [10]% and reduces the threat caused by your Arcane spells by [40]%" (#37)
- Unreworked on the page: **Improved Arcane Missiles** (2, 5), **Wand Specialization** (4, 2),
  **Improved Mana Shield** (12, 2), **Improved Counterspell** (13, 2), **Arcane Meditation**
  (15, 3), **Presence of Mind** (17, 1). Page adds only that Wand Specialization "also increases
  your chance to hit with Wands, and gives wand attacks a chance to restore mana", Arcane
  Meditation's regen "is tripled while you are below a mana threshold", and Improved Counterspell's
  payload text is "100% chance to silence the target for 4 sec" (#11255, #12598)
- **Removed:** Arcane Mind, Arcane Resilience, Improved Arcane Explosion

### Fire (0 points in this build)
- **Hot Streak** (18, 2) — "Row 5, 2 Ranks: new"; "Fireball and Fire Blast crits have a chance to
  grant Hot Streak, cutting the cast time of your next Pyroblast. Stacks."
- **Fire Vulnerability** (12, 3) — "Row 4, 3 Ranks: replaces Improved Scorch"; "Fire Blast can now
  apply the Fire vulnerability debuff as well as Scorch"
- **Improved Fire Blast** (6, 3) — "Also reduces Fire Blast's global cooldown, not just its own".
  **Flame Throwing** (5, 2) — "Also increases the radius of your Blast Wave". **Blast Wave** (16, 1)
  — "Moved within Row 5". **Removed:** Improved Scorch

### Frost
- **Icicles** (18, 1) — "Row 5, 1 Rank: new"; "Freezes you in place and launches icicles at the enemy
  for heavy Frost damage over its duration. Damage taken has a high chance to shatter your icy
  prison, dealing **30% of your base health** to you"
- **Flash Freeze** (22, 2) — "Row 6, 2 Ranks: new"; "When a freeze fails because the target is
  permanently immune (every raid boss), you have a chance to gain Flash Freeze instead, which resets
  Icicles and speeds it up"
- **Ice Barrier** (25, 1) — "Also increases your Frost damage while the shield holds, on top of
  absorbing damage and preventing interrupts". **Shatter** (14, 5) — "The critical strike bonus
  against frozen targets scales with the rank you have invested in the talent". **Arctic Reach**
  (12, 2) — "Also extends the range of Icicles"
- **Piercing Ice** (4, 3), **Ice Shards** (8, 5), **Winter's Chill** (20, 5), **Improved Cone of
  Cold** (19, 3) — "Repositioned within the tree to make room for Icicles and Flash Freeze. Piercing
  Ice and Ice Shards swap rows." No removals and no rank-count change stated for Frost.

## 5. What this costs in `src/WowRoguelike.Core/Content.fs`

The Mage runs `Talents = Talents.none` and `Traits.none` (`src/WowRoguelike.Core/Content.fs:3812`),
so **nothing** in this class is modelled today. The `Talents` record
(`src/WowRoguelike.Core/Domain.fs:1128`) is healer-shaped — `HealDoneBp`, `HealCostBp`,
`ManaRefundOnCritBp`, `CritArmorBp`, `HealingWayBp` — and carries no damage-side field at all. That
is the structural fact this section is really about.

**(a) Expressible today — 4 of the build's 19.** Edits to `AbilityDef` rank ladders and
`Talents.Spells` lines (`SpellTalent { Spell; CostBp; HealBp; CritBp; CastTicksOff }`), folded in by
`withTalents` (`Content.fs:3461`).
- **Improved Frostbolt** (5/5) — `{ Spell = "Frostbolt"; CastTicksOff = ticks 0.5 }`. Payload:
  "Reduces the casting time of your Frostbolt spell by [0.2/0.3/0.4/0.5] sec."
- **Improved Counterspell** (2/2) — append `Silence(secTicks 4.0)` to `counterspellRanks`
  (`Content.fs:1299`); `Silence` is already an `Effect` case.
- **Presence of Mind** (1/1) — a new single-rank kit row built exactly like `naturesSwiftness`
  (`Content.fs:3500`): `Empowers(secTicks 20.0, { Empower.none with Instant = true })`.
- **Arcane Impact** (3/3) — `{ Spell = "Arcane Missiles"; CritBp = … }` (and Arcane Explosion), or
  `Traits.CritBonusBp` if it should cover the whole school.

**(b) Needs a new model axis — 15 of 19.** Axis named first, then the concrete change.
- **Arcane Subtlety**, **Arcane Focus**, **Magic Absorption** (3 talents) — axis: **spell
  resistance**. `SimCombat.fs:143-164` states outright that "partial resists come only from
  resistance and nothing here carries resistance", so all three are inert until a resist stat exists;
  Magic Absorption additionally needs a resist-triggered mana refund with an ICD.
- **Arcane Concentration** — axis: **proc on spell hit** (chance + internal cooldown). The *buff*
  half is expressible (`Empower.FreeMana`); the trigger is not.
- **Magic Attunement** — axis: **magic-damage-taken modifier**. Amplify/Dampen Magic are not in the
  kit (`Content.fs:3812`) and no `Effect` alters incoming spell damage.
- **Arcane Rupture** — axis: **per-spell empower with damage and cost multipliers**. `Empower`
  (`Domain.fs:157`) has only `Instant`, `FreeMana`, `SureCrit`; "+20% Arcane Missiles damage and
  mana for 8 sec" needs a multiplier case. The 101-114 hit itself is just `SpellDamage(101, 114)`.
- **Improved Mana Shield** — axis: **absorb shield**. Mana Shield is not in the kit and no `Effect`
  absorbs damage.
- **Temporal Convergence** — axis: **cooldown-reset proc** (`Talents.CooldownOff` only shortens).
- **Arcane Instability** — axis: **proc** plus a **percentage-of-base-mana** cost (every `Rank.Cost`
  is a flat int).
- **Accelerated Arcana** — axis: **haste / cast-speed multiplier**. `CastTicks` is an absolute
  per-rank constant; there is no haste stat anywhere.
- **Arcane Potency** — axis: **crit-damage multiplier**. `Traits.CritBonusBp` is crit *chance*.
- **Resonance Cascade** — axis: **proc**, and it must recurse ("can trigger off of itself").
- **Arcane Power** — axes: haste **plus a per-second max-mana drain** plus a **lethal mana
  threshold**. Three new axes and a self-kill state on one talent.
- **Frostbite**, **Permafrost** (2 talents) — axis: **snare/Chill**. `frostboltRanks`
  (`Content.fs:1209`) already says "`Effect` has no snare, so the slow is not modelled"; Frostbite
  additionally needs a freeze proc off it.

**(c) Collapses or renames kit rows — 0 of the build's 19.** No taken talent merges or renames an
existing mage kit row. The class-level candidates sit outside the build: the two new Polymorph forms
would turn the single `polymorphRanks` row (`Content.fs:1318`) into a form-keyed row, and Ritual of
Refreshment plus the three portals are new rows with no combat content. The three Arcane removals
(Arcane Mind, Arcane Resilience, Improved Arcane Explosion) delete nothing here — no such row exists.

**Pushback and interrupts.** Since the era pushback model landed (`SimCombat.fs:462`: half a second
per damaging hit, twice per cast, resisted per hit) the Mage's `Traits.none` — 0 resistance — is a
real cost rather than a formality, and it is the worst in the game by that measure. It still measures
near zero on the ladder, because nothing hits the Mage: armory L39 puts no damage at all on either
non-healer caster. The only pushback-adjacent talent in the class is **Ice Barrier** ("preventing
interrupts"), and it is not taken. Improved Counterspell is the one interrupt talent and it is a
clean `Silence` append, not a model change.

## 6. The build decoded — `TAFOLISYZKAoB--FADD`

**Arcane 40 / Fire 0 / Frost 11 = 51 of 51.** The build commits to **Arcane**; its 31-point talent is
**Arcane Power** (Arcane slot 25, 1 rank — the page's "Row 7 capstone"), gated behind **Presence of
Mind** (`requires 17 / reqRanks 1`).

`Arcane 40` — 0 Arcane Subtlety 2/2 · 1 Magic Absorption 3/3 · 5 Arcane Focus 5/5 · 6 Arcane
Concentration 5/5 · 8 Magic Attunement 1/1 · 9 Arcane Impact 3/3 · 10 Arcane Rupture 1/1 ·
12 Improved Mana Shield 2/2 · 13 Improved Counterspell 2/2 · 14 Temporal Convergence 3/3 ·
16 Arcane Instability 3/3 · 17 Presence of Mind 1/1 · 18 Accelerated Arcana 1/1 · 19 Arcane Potency
2/2 · 22 Resonance Cascade 5/5 · 25 Arcane Power 1/1.
`Frost 11` — 1 Improved Frostbolt 5/5 · 5 Frostbite 3/3 · 7 Permafrost 3/3.

Every taken talent is at maximum rank — a max-rank Arcane build, not a spread.

**Takes, against spells the sim already models:** Improved Frostbolt (Frostbolt cast time), Improved
Counterspell (Counterspell silence), Arcane Impact (Arcane Missiles/Explosion crit), Presence of Mind
(any sub-10 s cast), Accelerated Arcana, Arcane Rupture + Temporal Convergence, Arcane Potency,
Resonance Cascade and Arcane Instability (all Arcane Missiles / Arcane Explosion), Arcane Subtlety
(Arcane threat), Arcane Power (all Arcane casts). **Leaves on the table:** the whole Fire tree (0 points, so Fireball — in the kit — gets nothing);
**Improved Arcane Missiles** (slot 2, 5 ranks), **Wand Specialization** (4), **Arcane Meditation**
(15); the entire Frost capstone path (Ice Barrier 25, Icicles 18, Flash Freeze 22, Shatter 14, Ice
Shards 8, Winter's Chill 20). Frostbite and Permafrost are taken but touch nothing the sim models,
because it has no snare.

## 7. Missing evidence / UNVERIFIED

> **Filled since — see `mage-payload-gaps.md`.** The 29 gaps below were a *reading* artefact, not
> missing data: the talent panel lives in the Next.js RSC flight payload and the chunk boundaries
> fall mid-talent, so slice-by-slice reading cut nodes in half. The full document (`/talents/mage/`,
> no `points` parameter, 134 KB, no JS challenge) yields all 53 talents. Read that file instead of
> this section for anything below, including the corrected per-rank arrays.

- **29 of 84 talent records were not recovered** — the payload was read in 5 slices and several
  blocks fell between them. Unresolved slots: Arcane 3, 7, 11, 20, 21, 23, 24, 26, 27; Fire 0, 3, 7,
  14, 19, 20, 21, 23, 24, 26, 27; Frost 3, 10, 15, 16, 21, 23, 24, 26, 27. **No talent the build
  takes is among them**, but any claim about the *unused* parts of these trees is unverified.
- **Per-rank arrays are partly elided** in the payload (rendered through RSC references). Read as
  single visible values: Arcane Subtlety 10/40, Magic Absorption 10, Arcane Focus 10%, Arcane Impact
  10%, Arcane Potency 100%, Arcane Instability 16…25 and 25%. Arcane Power's per-second mana drain is
  absent entirely. **UNVERIFIED.**
- **Improved Frostbolt shows four values (0.2/0.3/0.4/0.5) for five ranks**; Frostbite shows two
  (10/15) for three. Either rank 1 is elided or the ladder changed — **UNVERIFIED** either way.
- **Arcane Surge's source is unknown.** The page lists it as a level-32 ability with cost and
  cooldown, but no Arcane talent in the payload grants it and the page never names a talent for it.
  **UNVERIFIED.**
- **No coefficients on the page for any baseline spell**, and the Fireball cost figures are for ranks
  11 and 12 — the file's `fireballRanks` (`Content.fs:338`) stops at rank 7 (level 36, 220 mana), so
  "cheaper at every rank" cannot be checked against any rank the file carries.
- **Whether the calculator is OctoWoW or vanilla-plus-partial** is inferred, not documented.
  Evidence for OctoWoW: Arcane Rupture (#51949), Temporal Convergence (#51963-65), Accelerated Arcana
  (#51981) and Resonance Cascade exist in the payload and none of them is 1.12.
- **Which changes are live on the current server build** versus planned is not stated on the page.

## Contradictions

- **Piercing Ice / Ice Shards.** The page says "Piercing Ice and Ice Shards swap rows"; the payload
  has Piercing Ice at Frost slot 4 (3 ranks) and Ice Shards at slot 8 (5 ranks) — Piercing Ice still
  ahead. Reading 1: a slot index is a flat talent id and does not order rows, so both are true.
  Reading 2: the swap is planned but not in the calculator. Unresolved.
- **Row labels vs slot indices.** The page labels Arcane Instability "Row 5" and Resonance Cascade
  "Row 6", but the payload puts Instability at slot 16 and Cascade at slot 22 — later, so they agree
  in order and disagree in absolute row. The codec's 28 slots per tree are a flat index, not a 3-wide
  row grid. Both readings recorded; do not derive a row from a slot.
- **Arcane Subtlety.** Taken 2/2 and carrying a 40% threat reduction in the payload, but the page
  never mentions it while it does document a threat change for Fire (Ignite Threat). Reading 1: it is
  unchanged from vanilla and simply not listed. Reading 2: the page's Arcane section is incomplete.
  Unresolved.

## 8. What landed: the casting-speed axis and Arcane Power

The build's 31-point capstone is the one thing in it the engine could not express, and it is now the
first attack-adjacent *stat* the model has: `Aura.Casting(remaining, speedBp)` in
`src/WowRoguelike.Core/Domain.fs`, summed by `Aura.castSpeedBp`, and `Effect.CastHaste(duration, bp)`
applied by `SimCombat`. `SimCombat.castClock` is the one place the two cast-time modifiers meet, and
they are not the same arithmetic — Mind-numbing Poison's "+50% longer" multiplies the cast, while
Arcane Power's "+30% faster" divides it (`10000/(10000+bp)`). Decided when the cast starts, so the bar
is the length the spell will really take.

`Content.mageKit` grew `arcanePower`: 20 s of 3000 bp, 3-minute cooldown, no cost, self.

**What that measured.** Eight seeds a wing, the row armed vs stripped:

| wing | clear | mage damage | presses |
|---|---|---|---|
| graveyard L24 | 5295 → 4423 (-16.5%) | 17 920 → 18 620 (+3.9%) | 15 |
| library L36 | 8357 → 8122 (-2.8%) | 154 012 → 149 998 (-2.6%) | 23 |
| armory L39 | 7762 → 7732 (-0.4%) | 190 549 → 203 151 (+6.6%) | 24 |
| cathedral L42 | 9601 → 9054 (-5.7%) | 217 415 → 208 565 (-4.1%) | 23 |

The totals disagree with themselves because the haste shifts the RNG stream: the same seed fights a
different sequence, so a smaller number can come with a faster clear. The honest measure is the one
that does not depend on alignment — Armory, eight seeds: **789 → 836 Frostbolt hits, +6.0%**, which is
the ~11% uptime of a 23% shorter cast arriving where it should. Clear time moved 0.4% in the same
direction, because a Mage is not what makes an Armory clear slow.

**AI rule.** Pressed before the rotation, not after it: a 0-cost, 0-cast self buff costs one tick and
buys twenty seconds of faster casting, and a Mage with three spells on a 2.5 s cast has something to
press on nearly every tick — so a fallback press would never fire (measured: 1 press in 8 fights).
Never re-pressed while the window is up. Two tests that assert on the mage's *school* now spend the
cooldown by hand, because the first order of a fight is the buff and not the rotation.

**Still not ported from this build:** Arcane Potency (crit damage of Arcane spells — magnitude is one
visible "100%" in the payload with the per-rank array elided, so it is unportable without a guess),
Accelerated Arcana, Arcane Rupture, Temporal Convergence, Resonance Cascade, Arcane Instability,
Arcane Subtlety (threat), Magic Absorption, Magic Attunement, Improved Mana Shield, Improved
Counterspell's silence duration, and the whole Frost 11 (Frostbite/Permafrost need a root and a snare —
the engine has both now, but nothing in the build spends them on a Mage). Arcane Power's own mana
drain — the era's price for the speed — is still UNVERIFIED and therefore absent; see §7.

## 9. Arcane Instability 3/3 — the first spell-scoped fields

"Increases your spell damage and critical strike chance by 1/2/3%." Two multipliers on the
same body, and the reason `Talents` grew `SpellDoneBp` and `SpellCritBp` rather than reusing
`DamageDoneBp`: **a caster still swings.** `DamageDoneBp` would have carried the damage half
for free and quietly handed the mage's staff 3% of its own. The fields are read at exactly
two places — `Talents.spellBp` (the `SpellDamage` branch) and the `rollCrit` call beside it —
so a white swing, an auto-attack and a weapon strike answer to `damageBp` and never see them.

Measured over 8 seeds a wing, Armed/unarmed on an otherwise identical world:

| wing | L | clear | mage damage | crit rolls |
|---|---|---|---|---|
| graveyard | 33 | 74 925 → 82 276 (+9.8%) | +3.7% | 0 |
| library | 36 | 64 977 → 64 860 (−0.2%) | +9.2% | 0 |
| armory | 39 | 61 856 → 61 177 (−1.1%) | −0.2% | 0 |
| cathedral | 42 | 72 437 → 71 750 (−0.9%) | +5.5% | 0 |

**The arithmetic is right and the per-wing number is not measurable this way.** Aggregate mage
damage went 657 604 → 686 030 (+4.3%), which is what +3% damage and +3 points of crit on a
1.5× multiplier should be worth. The clear-time column is noise: three wings inside ±1.1%
and graveyard reading +9.8%, because any change to the crit roll reorders the whole world's
random stream — the same seeds then fight a different fight. A chaotic sim can be A/B'd on
*aggregate damage*, not on individual clear times, and a single wing's delta is worth nothing.

There is no "critically" marker in the combat log, so a crit count cannot be read off it
either; verifying crit share needs the roll itself, not the log. The unit test asserts the
arithmetic (`spellBp "Frostbolt" = 10300` while `damageBp "" = 10000`) instead.
