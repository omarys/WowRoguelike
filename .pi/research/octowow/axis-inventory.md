# Axis inventory — what the ten OctoWoW notes actually ask the engine for

Built from the ten notes in `.pi/research/octowow/` (`warrior`, `rogue`, `mage`, `priest`, `shaman`,
`paladin`, `warlock`, `hunter`, `druid-feral`, `druid-moonkin`). Use it to plan the port order: the
notes sort talents by class, this file sorts the *bucket-(b)* talents by the engine mechanism they
need, so one build buys every talent on its row instead of nine class ports each re-deriving the same
gap. Nothing here is new research — every claim, every quote and every `file:line` is the note's own.
Researcher inference is marked as such.

Bucket (a) — "lands as data in an existing field" — and bucket (c) — "renames or collapses a kit row"
— are out of scope except where a note files one talent in two buckets (flagged in §6).

## 0. Entries per note

| Note | bucket-(b) entries counted | notes |
|---|---|---|
| warrior | 12 | of 18 talents the build spends in |
| rogue | 9 | of 18 |
| mage | 15 | of 19 |
| priest | 7 | of 16 |
| shaman | 7 | of 16 |
| paladin | 9 | of 17 |
| warlock | 9 | of 17 |
| hunter | 10 | of 16 — but §8 records 4 of them as since landed |
| druid-feral | 15 | of 21 |
| druid-moonkin | 14 | of 19 |

107 entries, **105 distinct talents** — `Omen of Clarity` and `Sylvan Blessing` appear in both druid
notes (same talent, two builds). *Researcher bookkeeping: the 18 axis tables below account for 67
distinct talents, the one-off list for 31, the four hunter entries already ported for 4, and three
entries the code has overtaken (Improved Moonfire, Moonfury, Feral Aggression) for 3 — 105. A handful
of talents sit on two axes and are counted once, under their dominant one.*

## 1. The axes

### A1. Trigger/proc hook — a chance event that fires an effect or an aura

| class | talent | ranks | axis (event) | concrete change named in the note |
|---|---|---|---|---|
| Warrior | Shield Specialization | 5/5 | proc: on block | "block chance is data (`Traits.BlockChanceBp`, `Domain.fs:1086`); "rage when a block occurs" needs an on-block trigger into `gainRage` (`SimCombat.fs:234-241`)" |
| Rogue | Remorseless Attacks | 2/2 | proc: on kill, consumable charge | "**Consumable "next generator" crit charge.** Nearest existing case is `Empowers of duration * empower`, which is spell-shaped (Nature's Swiftness)" |
| Mage | Arcane Concentration | 5/5 | proc: on spell hit | "**proc on spell hit** (chance + internal cooldown). The *buff* half is expressible (`Empower.FreeMana`); the trigger is not." |
| Mage | Resonance Cascade | 5/5 | proc: on spell, recursive | "**proc**, and it must recurse ("can trigger off of itself")" |
| Mage | Arcane Instability | 3/3 | proc + % base mana | "**proc** plus a **percentage-of-base-mana** cost (every `Rank.Cost` is a flat int)" |
| Mage | Temporal Convergence | 3/3 | proc: cooldown reset | "**cooldown-reset proc** (`Talents.CooldownOff` only shortens)" |
| Paladin | Blessed Strikes | 5/5 | proc: cooldown reset (+ healing power, A18) | "a cooldown-reset proc axis and a **healing-power** scaling axis (`SpiritToHealingBp` converts Spirit; nothing converts healing power)" |
| Paladin | Daybreak | 1/1 | proc: on taking damage | "a **reactive** heal: every `Effect` resolves at cast time, nothing fires on taking damage" |
| Shaman | Undertow | 2/2 | proc: on cast | "**cast-triggered proc**; `Effect` has no on-cast trigger" |
| Shaman | Tidal Surge | 2/2 | proc: on cast, refund | same axis — "**cast-triggered proc**; `Effect` has no on-cast trigger" |
| Warlock | Nightfall | 2/2 | proc: on DoT tick | "**Proc-on-tick.** The destination is nearly free — `Empowers of duration * empower: Empower` with `Instant` and `SureCrit` already models "next Shadow Bolt is instant and cannot miss" — but nothing fires it off DoT ticks." |
| Hunter | Piercing Shots | 2/2 | proc: on crit (+ threat-free) | "**crit-triggered effect** and **threat-free damage**; nothing fires on a crit, and `Threat` is additive with no zero-threat marker" |
| Hunter | Lock and Load | 1/1 | proc: on crit, cooldown reset (+ line AoE) | "**crit-triggered cooldown reset** plus *a line hitting everything between caster and target* (`MultiShot` is 3 targets near the target, `AreaDamage` a radius; neither is a line)" |
| Druid (feral) | Primal Fury | 2/2 | proc: on crit → resource | "A **crit-triggered resource gain**. `SimCombat.gainRage` exists but is called from damage paths only; nothing hooks a crit." |
| Druid (feral) | Ancient Brutality | 1/2 | proc: on dodge, on bleed tick | "Two triggers: **on dodge → Rage over time** (needs a dodge-triggered aura; no aura is emitted by a dodge) and **on bleed tick → Energy** (no bleed aura exists — `Aura.Corrupted` is `Domain.fs:233`, unmitigated damage, and is not a bleed)" |
| Druid (both) | Omen of Clarity | 1/1 | proc: on swing / on cast | feral: "A **random proc on attack/spell**. `Empowers` + `Empower.FreeMana` (`Domain.fs:157-163`, `Domain.fs:839`) already carry the "next ability is free" half, but nothing rolls a chance on a swing." · moonkin: "**Proc model.** A chance-on-cast state that zeroes the next spell's cost; `Empower.FreeMana` (`Domain.fs:161`) exists but nothing procs it." |
| Druid (both) | Sylvan Blessing | 2/2 | proc: on kill + timed regen state | feral: "An **on-kill trigger** plus a **timed casting-regen override**. `RegenBp` (`Domain.fs:1138`) is a permanent constant; there is no kill hook." · moonkin: "**On-kill proc** granting "100% mana regeneration while casting" — a state the sim does not have (`RegenBp` is a static talent number, not a timed buff)." |
| Druid (moonkin) | Owlkin Frenzy | 1/3 | proc: on taking damage | "Three at once: a **damage-taken proc**, a **pushback-resist buff**, and **percent-max-mana-per-second** regen. `Aura.Innervated` (`Domain.fs:284`) is the nearest regen aura but is keyed to Spirit regen, not to max mana." |

**What it takes:** a new subsystem. The `Effect` union (`Domain.fs:797-880`) is a closed vocabulary of
things that resolve *at cast time*; every row above needs a hook that resolves *later*: after a swing,
after a spell lands, after a tick, after a block/dodge, after a kill, after damage is taken. One case
— something shaped `Trigger of event * chanceBp * icd * effect` — would serve all sixteen, so this is
one axis with six event kinds, not six axes. Two riders ride on the same hook: the cooldown reset
(Temporal Convergence, Blessed Strikes, Lock and Load, Dark Harvest's reset-on-kill) is a write into
`Entity.Cooldowns` once the trigger exists, and the "next ability" buff half is already built
(`Empower` + `Aura.Empowered`).

### A2. Haste — a speed multiplier applied after construction

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Rogue | Blade Rush | 2/2 | attack speed (self) | "**Per-entity haste.** Swing speed lives in the shared weapon ladder (`Ticks = secTicks 1.9`)" |
| Mage | Accelerated Arcana | 1/1 | cast speed | "**haste / cast-speed multiplier**. `CastTicks` is an absolute per-rank constant; there is no haste stat anywhere" |
| Mage | Arcane Power | 1/1 | haste + mana drain + lethal threshold | "axes: haste **plus a per-second max-mana drain** plus a **lethal mana threshold**. Three new axes and a self-kill state on one talent." |
| Druid (feral) | Blood Frenzy | 2/2 | attack speed (buff) | "the axis it needs is an **attack-speed buff aura** — nothing modifies `AutoAttack.Ticks` after construction" |
| Hunter | Swift Reflexes | 2/2 | attack speed (+ parry, one-off) | "*parry* (absent: only dodge and block) and *attack speed* (`AutoAttack.Ticks` fixed)" |

**What it takes:** a new subsystem, or close to one. `CastTicks` and `AutoAttack.Ticks` are absolute
per-rank constants baked at construction, so a timed multiplier needs a recompute path in the tick
loop plus an `Aura` carrying the bp — the same shape as `Innervated` for regen, which is why this is
not just a field. Arcane Power's other two legs (a per-second max-mana drain and a lethal mana
threshold) are one-off and much dearer than the haste leg it is listed under.

### A3. Percent multiplier on a stat or pool that is currently flat

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Warrior | Toughness | 5/5 | armour %, block-value % | "percent-of-gear-armour and percent-of-block-value; `Traits.ArmorBonus` and `BlockValue` are flat ints" |
| Druid (feral) | Thick Hide | 3/3 | armour % | "An **armour multiplier on item armour**. `Armor = d.Armor + armorBp + Agility*2 + traits.ArmorBonus` (`Content.fs:3234`) is all flat; `armorBp` comes from party buffs, not a percent. This is exactly the gap the existing `shortcut:` already names (`Content.fs:3908-3911`: "a per-spec armour multiplier is a new field, and a separate decision")." |
| Druid (feral) | Heart of the Wild | 5/5 | per-form stat % | "**Per-form stat multipliers.** `IntellectBp` (`Domain.fs:1136`, folded at `Content.fs:3218`) covers the Intellect fifth. `MaxHealth = d.BaseHealth + pool s.Stamina 10` (`Content.fs:3228`) and `AttackPower = Strength*ApPerStrength + …` (`Content.fs:3237`) have no percent anywhere, so Bear Stamina +20% and Cat Strength +20% have no field." |
| Druid (feral) | Predatory Strikes | 3/3 | attack power % (+ ability damage, A13) | "an **attack-power multiplier** (same line as above) and an **ability damage multiplier** — `SpellTalent` (`Domain.fs:1109-1118`) carries `CostBp`/`HealBp`/`CritBp`/`CastTicksOff` and **no damage field**" — *the damage half has since been built; see §7* |
| Shaman | Ancestral Knowledge | 5/5 | stat-wide % | ""the total value of **all** your stats"; sim has only `IntellectBp`. Axis: **stat-wide multiplier**." |

**What it takes:** new fields on `Traits`/`Talents`. `IntellectBp` (`Domain.fs:1136`) is the template —
already a percent folded before the pools are derived — so Ancestral Knowledge is that field widened
to every stat, Thick Hide/Toughness are the same shape over `Gear` armour, and Heart of the Wild is
one percent per form-keyed pool. Cheapest axis on this list that unblocks more than one class.

### A4. Empower magnitude — what "your next spell" is allowed to change

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Paladin | Holy Judgement | 3/3 | Empower cast time | "`Empower` is `{ Instant; FreeMana; SureCrit }` with **no cast-time field**, and no Judgement-cast trigger" |
| Druid (moonkin) | Nature's Grace | 1/1 | Empower cast time | "`Empower` (`Domain.fs:157`) has `Instant`, `FreeMana`, `SureCrit` — **no cast-time reduction**. Axis: `Empower.CastTicksOff`." |
| Shaman | Ancestral Swiftness | 1/1 | Empower effectiveness | "**effectiveness modifier on an empowered cast**; `Empowers` cannot hold "damaging spells cast this way have reduced effectiveness"" |
| Mage | Arcane Rupture | 1/1 | Empower damage/cost multiplier | "**per-spell empower with damage and cost multipliers**. `Empower` (`Domain.fs:157`) has only `Instant`, `FreeMana`, `SureCrit`; "+20% Arcane Missiles damage and mana for 8 sec" needs a multiplier case." |

**What it takes:** new fields on `Empower` (`Domain.fs:157`, a three-bool struct already threaded
through `Aura.Empowered` and the cast path). The notes name the field themselves — `Empower.CastTicksOff`
— and it is the cheapest axis here by a wide margin: four talents across four classes for two int
fields and one read in the cast path.

### A5. Dispel / aura removal

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Warrior | Gag Order | 2/2 | dispel a foe's magic buff | "a dispel `Effect` plus the Shield Slam row; the Shield Bash half is fine — `Silence of duration` (`Domain.fs:823`) exists and 2/2 is 100%" |
| Warrior | Shield Slam | 1/1 | 50% dispel (+ block-value damage, one-off) | "damage scaling from block value … plus the 50% dispel" |
| Paladin | Ascendance | 1/1 | purge crowd control from self | "a **dispel of crowd control** (no effect in `Effect` removes an aura)" |
| Druid (feral) | Berserk | 1/1 | remove Fear | "a **Fear removal** — there is no Fear aura to remove" |

**What it takes:** one new `Effect` case. Both directions — strip an aura off the target, strip one off
the caster — are the same union case with a side and a kind; `Aura` already has a single owner
(`Aura.advance`), so removal is a list filter. The two Berserk/Ascendance rows add a wrinkle: there is
no `Aura.Fear`, so their subject is a state the model does not carry yet.

### A6. Hit / miss stat

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Rogue | Precision | 5/5 | hit chance | "**Hit bonus on `Entity`.** `SwingChances.Miss` exists but nothing on the hero moves it" |
| Rogue | Weapon Expertise | 2/2 | weapon skill | "**Weapon-skill axis.** Skill feeds only `levelCritBonusBp` (4 bp/point, `SimCombat.fs:115`) — +5 skill is ~20 bp crit and no glance change" |
| Warlock | Suppression | 5/5 | spell hit / resist | "**Spell hit / resist.** There is no resist or miss model anywhere in `Domain.fs` — nothing for −10% resist to reduce." |
| Druid (either) | Natural Weapons | 3/3 | melee + spell hit (+ physical %, A13) | feral: "…a **hit chance** — nothing in `Traits` or `Talents` feeds miss chance" · moonkin: "**Spell hit.** `Traits` (`Domain.fs:1086`) has `CritBonusBp` and no hit field; the attack table's spell-miss is a table, not a trait." |

**What it takes:** a new field on `Traits`/`Talents` plus a read in the attack table. `Weapon Expertise`
is the awkward one: it wants a skill value, and the notes point out skill currently feeds only
`levelCritBonusBp`, so +5 skill is not a hit bonus at all yet.

### A7. Crit-damage multiplier

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Mage | Arcane Potency | 2/2 | crit damage | "**crit-damage multiplier**. `Traits.CritBonusBp` is crit *chance*." |
| Hunter | Mortal Shots | 5/5 | crit damage (+ per-ability flag) | "*crit multiplier*; a crit is a fixed roll today" · §8: "it needs the crit multiplier as data (`SimCombat.rollCrit` hardcodes `amount * 150 / 100`, `src/WowRoguelike.Core/SimCombat.fs:23-25`) plus a per-ability "benefits from bonus crit damage" flag" |
| Druid (moonkin) | Vengeance | 5/5 | crit damage | "**Critical damage multiplier.** The crit bonus is a constant; nothing scales it." |

**What it takes:** a new field plus one unhardcoding. `rollCrit`'s `150` becomes data; the Hunter note
adds a per-ability flag because a talent that raises one spell's crit damage must not raise every
ability's.

### A8. Threat multiplier — per school, per stance

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Warrior | Defensive Tactics | 3/3 | per-stance threat | "per-stance threat: `ThreatMultiplierBp` is one scalar and no Warrior row sets `Stance` (`Stance = None` throughout)" |
| Shaman | Nature's Grace | 3/3 | per-school threat | "axis: **per-school threat multiplier**" |
| Druid (moonkin) | Subtlety | 5/5 | threat multiplier | "**Threat multiplier.** `Effect.Threat of amount` (`Domain.fs`) adds threat; nothing scales it." |

**What it takes:** an existing field that needs splitting. `Entity.ThreatMultiplierBp`
(`Domain.fs:1277`, read at `SimState.fs:328`) and `tankStanceBp = 50000` (`Content.fs:1650`) already
multiply every threat source a hero has — the missing part is keying it on school and on stance
instead of one scalar. Three classes, one field.

### A9. Crowd-control resist

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Warrior | Iron Will | 5/5 | stun/charm resist | "a control-resist roll (`Stunned` aura exists, `Domain.fs:217`; no resist, no Charm)" |
| Priest | Unbreakable Will | 5/5 | stun/fear resist | "A **crowd-control resist chance**. `Aura.Stunned`/`Silenced` exist (`Domain.fs:217`, `224`) but nothing rolls against them; `PushbackResistanceBp` is the only resist of any kind." |
| Paladin | Improved Concentration Aura | 3/3 | silence resist | "no aura system, no resistance to `Silence`" |

**What it takes:** a new field plus a roll where an aura is applied. `Traits.PushbackResistanceBp`
(`Domain.fs:1092`) is the precedent for the shape; the Paladin row additionally wants the aura itself
to exist, which is a separate gap.

### A10. Self-resource grant or refund

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Warrior | Improved Bloodrage | 2/2 | grant rage to self | "a self-resource grant `Effect` (none: `DrainResource` `Domain.fs:867` takes from a target) and a Bloodrage row" |
| Warrior | Concussion Blow | 1/1 | grant 10 rage on hit | "stun (`Domain.fs:853`), threat (`Domain.fs:860`) and unmitigated `SpellDamage` are data; "instantly generates 10 rage" needs the self-resource grant" |
| Rogue | Relentless Strikes | 1/1 | refund + stacking self buff | "**Self resource refund + stacking self damage buff.** No `Effect` returns resource to the caster; `Innervate of duration * regenBp` targets a friendly pool" |

**What it takes:** one new `Effect` case — the mirror of `DrainResource` with the actor as the
beneficiary. Relentless Strikes' second half (a 5-stack, 30 s, self damage buff) is a different gap
and is not free with it.

### A11. Target-state / target-aura modifier

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Priest | Empowered Recovery | 2/2 | target-state heal modifier (+ per-spell HoT duration) | "Two axes: a **per-spell HoT duration** (Renew's `HealOverTime(secTicks 15.0, …)` is hardcoded at `Content.fs:329`) and a **target-state healing modifier** ("to targets affected by Renew") — `healBp` is caster-side only." |
| Priest | Ascendance | 1/1 | healing-received on the target (+ dispel, + timed caster buff) | "Three at once: a **dispel of crowd control** …, a **timed caster buff** scaling cost and cast time 33%/20%, and **Apotheosis, a healing-received modifier on the healed target**. The last is the expensive one — `healBp` is read off the caster." |
| Druid (moonkin) | Balance of All Things | 3/3 | target-state conditional | "**Target-state-conditional modifiers**: mana refund if the target has Insect Swarm, +crit if the target has Moonfire. `healBp` and every `SpellTalent` field are caster-side; nothing reads the target's debuffs." |

**What it takes:** a new argument on the heal/damage resolution that reads the *target's* auras. Two
of the three also want a per-spell HoT duration, which is one field on the DoT/HoT rather than this
axis.

### A12. Spell resistance (partial resist)

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Mage | Arcane Subtlety | 2/2 | target spell resistance | "axis: **spell resistance**. `SimCombat.fs:143-164` states outright that "partial resists come only from resistance and nothing here carries resistance", so all three are inert until a resist stat exists" |
| Mage | Arcane Focus | 5/5 | spell resistance | same row's axis |
| Mage | Magic Absorption | 3/3 | spell resistance + ICD refund | "Magic Absorption additionally needs a resist-triggered mana refund with an ICD" |

**What it takes:** a new subsystem — a mitigation path the code deliberately does not have. Three
talents, one class: the worst leverage/cost ratio in the file.

### A13. Physical / white-damage multiplier with a weapon-class selector

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Rogue | Dual Wield Specialization | 5/5 | off-hand damage | "**Per-entity off-hand damage multiplier.** `OffHand = true` exists (`Content.fs:3817`) but off-hand damage is unscaled" |
| Druid (feral) | Natural Weapons | 3/3 | physical damage, all forms | "A **physical damage multiplier in all forms**" |

**What it takes:** a field that now exists, plus a selector. The Hunter port landed the swing hook
(`SimTick.fs:481` folds `Talents.damageBp` into both hands — `hunter.md` §9), so a tree-wide physical
multiplier is live; what is missing is telling a bow from a dagger or an off hand from a main hand.
*`hunter.md` §9 records the over-application this already causes: the Warrior's One-Handed
specialisation also raises `Shoot` and the off hand.*

### A14. Resource-regen multiplier aura

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Rogue | Adrenaline Rush | 1/1 | regen rate ×2 for 15 s | "**Temporary resource-regen multiplier.** Regen is one integer, `Entity.ResourceRegenPerTick` (`partyRegen = 1`, `Content.fs:1658/1727`); "100% … for 15 sec" needs an `Aura` carrying a bp multiplier the tick loop multiplies through. Nothing in `Effect` or the `Aura` union touches an *incoming tick rate*, and `Innervate` is a share of a friendly pool, not a rate" |
| Druid (feral) | Berserk | 1/1 | energy-regen ×2 (+ health buff, A15) | "an **energy-regen multiplier aura**" |

**What it takes:** a new `Aura` case plus one multiplication in the regen step. `Aura.Innervated`
(`Domain.fs:284`) is the same shape aimed at mana, so the precedent is exact.

### A15. Max-health multiplier

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Warrior | Last Stand | 1/1 | +30% max health, lost after | "a temporary max-health buff; `ReducesDamage` (`Domain.fs:880`) scales incoming damage, not the pool" |
| Druid (feral) | Berserk | 1/1 | +20% total health in Bear | "a **health multiplier buff** (`MaxHealth` is computed once at construction, `Content.fs:3228`)" |

**What it takes:** an `Aura` plus a recompute of `MaxHealth`, which is set once at construction. Both
notes say the same thing twice — the pool has no runtime.

### A16. School-keyed damage-taken modifier

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Mage | Magic Attunement | 1/1 | incoming magic damage ± | "**magic-damage-taken modifier**. Amplify/Dampen Magic are not in the kit (`Content.fs:3812`) and no `Effect` alters incoming spell damage." |
| Warlock | Shadow Vulnerability | 5/5 | shadow damage taken +20% | "**School-specific damage-taken multiplier.** `SpellDamage` is explicitly unmitigated ("mitigated by nothing (Q2a)", `Domain.fs:809`); no aura raises shadow damage taken." |

**What it takes:** a new `Aura` case carrying a school and a signed bp, read where damage lands.
`ShieldWalled`/`ReducesDamage` are the same shape without a school.

### A17. Healing-power scaling source

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Paladin | Ironclad | 2/2 | armour → healing | "no armour → healing conversion; armour is a `Gear` total with nothing downstream" |
| Paladin | Blessed Strikes | 5/5 | +% of healing power (also A1) | "…and a **healing-power** scaling axis (`SpiritToHealingBp` converts Spirit; nothing converts healing power)" |

**What it takes:** a new field or conversion. `SpiritToHealingBp` (`Domain.fs:1147`) is the template —
a percent of one stat added to every heal — so this is one more stat in the same sum.

### A18. Party crit aura

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Druid (feral) | Leader of the Pack | 1/1 | +3% party melee/ranged crit, in form | "A **form-conditional party aura**. `PartyBuffKind` (`Domain.fs:1363-1378`) is a closed union applied at construction, has no crit member, and is not gated on the holder's form." |
| Druid (moonkin) | Moonkin Form | 1/1 | +3% party spell crit | "+3% party spell crit** has no home: `PartyBuffKind` (`Domain.fs:1363`) has no crit variant and `buffed` (`Content.fs:3100`) folds buffs into `(Stats, armor, ap)` only. New axis: a party-wide spell-crit contribution threaded to `Traits.CritBonusBp`." |

**What it takes:** a new `PartyBuffKind` case and a crit contribution in `buffed`. Moonkin Form's other
two legs are data: −20% Balance mana is `SpellTalent.CostBp`, and +180% item armour the note computes
as a flat `Traits.ArmorBonus` (428 = 1.8 × 238).

## 2. Ranking by leverage

Counts are bucket-(b) talents unblocked, then distinct classes. Several talents sit on two axes and are
counted once, under their dominant one; the gate in the axis row says so.

| # | axis | talents | classes | build cost class | where it sits |
|---|---|---|---|---|---|
| 1 | A1 trigger/proc hook | 18 | 8 — warrior 1, rogue 1, mage 4, paladin 2, shaman 2, warlock 1, hunter 2, druid 5 | new subsystem | Twice the next axis and four classes wider, and it is the only axis every spec needs: proc is how the notes describe half of OctoWoW's new talents. Expensive, so it should be built first and once — nine class ports otherwise each invent their own hook. |
| 2 | A2 haste | 5 | 4 — rogue 1, mage 2, druid 1, hunter 1 | new subsystem | Cheap in fields and dear in the tick loop: `CastTicks` and `AutoAttack.Ticks` are absolute per-rank constants, so a timed multiplier needs a recompute path. One of the five is worthless without it (Arcane Power's capstone). |
| 3 | A3 percent stat multiplier | 5 | 3 — warrior 1, druid 1 class +1 build, shaman 1 | new fields on `Traits`/`Talents` | Same count as A2 for a fraction of the cost: `Talents.IntellectBp` already *is* this field, folded before the pools are derived. Pull it ahead of its rank on cost alone. |
| 4 | A4 empower magnitude | 4 | 4 — paladin 1, druid 1, shaman 1, mage 1 | new fields on `Empower` (`Domain.fs:157`) | Four classes for two int fields on a three-bool struct that is already threaded through `Aura.Empowered`. The notes name the field themselves (`Empower.CastTicksOff`); best cost-per-class in the file. |
| 5 | A5 dispel / aura removal | 4 | 3 — warrior 2, paladin 1, druid 1 | new `Effect` case | One union case, two directions, and `Aura.advance` is already the single owner of aura lifetime. Half of Warrior's Shield Slam depends on it, so it gates a (c)-bucket row too. |
| 6 | A6 hit / miss stat | 4 | 3 — rogue 2, warlock 1, druid 1 | new field + attack-table read | A trait field that nothing currently reads; the attack table already computes `SwingChances.Miss`, so this is wire-up plus a number. Weapon Expertise is the odd member — skill feeds only crit today. |
| 7 | A7 crit-damage multiplier | 3 | 3 — mage 1, hunter 1, druid 1 | new field + unhardcode `rollCrit` | Three classes and one hardcoded `150` (`SimCombat.fs:23-25`). Cheap, but the Hunter note adds a per-ability flag, which is a second field. |
| 8 | A8 threat multiplier | 3 | 3 — warrior 1, shaman 1, druid 1 | existing field, needs splitting | `Entity.ThreatMultiplierBp` and `tankStanceBp` already exist and are already read; the work is keying a scalar by school and stance. Three classes for near-zero new vocabulary. |
| 9 | A9 crowd-control resist | 3 | 3 — warrior 1, priest 1, paladin 1 | new field + roll at aura application | Same shape as `PushbackResistanceBp`, so a known pattern, but it needs a hook where an aura lands and the Paladin row wants an aura system that does not exist. |
| 10 | A10 self-resource grant | 3 | 2 — warrior 2, rogue 1 | new `Effect` case | The mirror of `DrainResource`; small, and it is the only thing standing between Warrior's bucket-(b) list and its capstone clause (Concussion Blow's 10 rage). |
| 11 | A11 target-state modifier | 3 | 2 — priest 2, druid 1 | new argument on heal/damage resolution | Every healing field is caster-side, so this is the one axis that changes `healBp`'s signature rather than adding to it. Two of the three also need a per-spell HoT duration. |
| 12 | A12 spell resistance | 3 | 1 — mage 3 | new subsystem | Highest count of any one-class axis and the code says outright that partial resists are unreachable (`SimCombat.fs:143-164`). Real work for one class's Arcane tree — the last thing to build, or scope out. |
| 13 | A13 physical multiplier, weapon-class selector | 2 | 2 — rogue 1, druid 1 | existing field + selector (half-built) | The swing hook and `Talents.damageBp` landed with the Hunter port; what is left is a name on `AutoAttack` so a bow differs from a dagger. Cheap follow-through, and it fixes the Warrior over-application the Hunter note records. |
| 14 | A14 resource-regen multiplier | 2 | 2 — rogue 1, druid 1 | new `Aura` + tick read | Exactly `Aura.Innervated` aimed at a different pool; small, but it unblocks the Rogue's 31-point talent, which their note notes is the one talent they most need. |
| 15 | A15 max-health multiplier | 2 | 2 — warrior 1, druid 1 | new `Aura` + `MaxHealth` recompute | Two classes, one pool that is computed once at construction. Small once A14's `Aura` pattern exists. |
| 16 | A16 school-keyed damage-taken | 2 | 2 — mage 1, warlock 1 | new `Aura` + damage-path read | `ReducesDamage`/`ShieldWalled` are the same shape without a school; two classes, one signed bp and a school tag. |
| 17 | A17 healing-power scaling | 2 | 1 — paladin 2 | new field | `SpiritToHealingBp` is the template and the Paladin's Holy tree is a healing tree, but it is one class for two fields. |
| 18 | A18 party crit aura | 2 | 1 — druid 2 | new `PartyBuffKind` case | Two Druid builds, one case and one branch of `buffed`. Cheap and self-contained — build it with whatever touches `PartyBuffKind` next. |

## 3. What does not group

One-off axes, one talent each (two are pairs within a class). Named so nobody re-derives them.

| class | talent | ranks | axis | concrete change named in the note |
|---|---|---|---|---|
| Warrior | Anticipation | 3/3 | Defense skill and its conversion | "a Defense-skill field and its conversion; `Traits` holds `DodgeBonusBp` / `BlockChanceBp` / `ArmorBonus` as separate flat registers" — *the note calls the conversion **UNVERIFIED** and forbids substituting a vanilla ratio* |
| Warrior | Improved Intervene | 2/2 | impairment-immunity state + a row | "Intervene is not a kit row; impairment immunity is a new state (`Root` exists, `Domain.fs:854`)" |
| Warrior | Shield Slam | 1/1 | damage scaled from block value | "damage scaling from block value: `WeaponDamage` (`Domain.fs:800-806`) scales from the weapon roll, `Damage` is flat" — the 50% dispel half is A5 |
| Warrior | Improved Shield Slam | 2/2 | a magnitude on `Blocks` | "`Blocks of duration` (`Domain.fs:878`) carries no magnitude, so "+35/70% block" cannot be expressed" — one int field |
| Rogue | Opportunity | 5/5 | positional / behind | "domain.md:1501 — "Stealth is not modelled, by design … there is no facing check"; `ambush` substitutes "a full health, untouched enemy"" |
| Rogue | Deflection | 5/5 | parry | "`SwingChances.Parry` exists (`SimCombat.fs:141`) but no `Traits`/`Entity` field raises the defender's parry — only `DodgeBonusBp` exists" — *the note contrasts this with Lightning Reflexes, which is a one-line (a) field it does not take* |
| Mage | Improved Mana Shield | 2/2 | damage-absorbing shield | "Mana Shield is not in the kit and no `Effect` absorbs damage" — `Domain.fs:1126` already names "a damage-absorbing shield" as vocabulary the model refuses |
| Mage | Arcane Power | 1/1 | per-second max-mana drain + lethal threshold | "haste **plus a per-second max-mana drain** plus a **lethal mana threshold**" (haste leg is A2) |
| Priest | Book of Prayer | 2/2 | per-caster cast memory | ""different from your previous healing spell" needs the last heal's name on the actor; `ManaRefundOnCritBp` (`Domain.fs:1141`) is the nearest precedent and it is keyed on a crit, not on history" |
| Priest | Spirit of Redemption | 1/1 | a death state that still acts + `SpiritBp` | "`SimCombat.fs:928` clears the traits at death and `Resurrect` (`Domain.fs:852`) is documented as "the only effect that may resolve against something that is already dead", so a Priest casting for ten seconds after dying inverts that invariant." |
| Priest | Reservoir of Light | 3/3 | Lightwell | "**Lightwell.** The sim has none, so the talent has no subject. Rebuilding it needs `Summon` (`Domain.fs:891`) plus a heal-triggered proc…" |
| Shaman | Spirit Link | 1/1 | damage redistribution between entities | "no `Effect` lets one actor's damage be split across others" — the Shaman's 31-pointer |
| Shaman | Improved Water Shield | 3/3 | shield charges as readable state | "**shield charges as readable state** ("for each globe still active")" |
| Paladin | Holy Shock | 1/1 | either-target (Foe or Ally) | "`TargetKind` is single-valued (`Foe`/`Ally`/`Self`/`Corpse`), so "damage to an enemy, or healing to an ally" needs an **either-target axis**, plus a proc-on-cast axis for the cooldown wipe" |
| Paladin | Improved Lay on Hands | 2/2 | raise a target's armour | "Lay on Hands is not in the kit and no `Effect` raises a target's armour (`CritArmorBp` is talent-wide, crit-triggered)" |
| Paladin | Improved Blessings | 5/5 | blessing effectiveness | "the mechanic exists but is named `TotemEffectBp` ("percent added to what a totem gives the party"); blessing effectiveness is the same shape and a different axis, so reuse-and-rename or a second field is a decision, not a constant" |
| Paladin | Blessing of Kings | 1/1 | a `PartyBuffKind` case that multiplies | "a new `PartyBuffKind` case whose branch **multiplies** all five stats by 10%; every branch of `buffed` today adds a flat number" |
| Warlock | Improved Life Tap | 2/2 | health → mana | "Already a documented hole, `Content.fs:4204-4207`: "Life Tap has no effect in the vocabulary (nothing moves health into mana) and is left out for the same reason."" |
| Warlock | Improved Drains | 2/2 | channelled drain | "Drain Soul / Drain Life / Drain Mana are not modelled; `DrainResource` moves a resource *off the target* and does not heal the caster" |
| Warlock | Siphon Life | 1/1 | lifesteal (damage on foe + heal on caster) | ""Transfers 150 Health from the target to the caster over 30 sec" is damage on the foe *plus* healing on the caster; `Effect` has no case that targets both" |
| Warlock | Malediction | 1/1 | per-source DoT identity | "The sharpest one: `Content.fs:561-563` documents the `shortcut:` — "every damage-over-time shares one `Corrupted` aura, so Corruption and Curse of Agony refresh each other instead of stacking. Split the aura by source if the warlock ever needs both on one target." Malediction is that "if"." |
| Warlock | Dark Harvest | 1/1 | channel + DoT tick rate | "**Channel** (8 s, damage split across it) **plus DoT tick-rate**. `Rank.CastTicks` is a cast, not a channel; `DamageOverTime(duration, damagePerTick)` has a fixed period, so "time between ticks reduced by 30%" has nowhere to go. Also needs a cooldown-reset-on-kill hook." |
| Warlock | Demonic Swiftness | 2/2 | a demon | ""Reduces the casting time of your Imp's Firebolt spell by 0.3/0.5 sec and reduces the cooldown of your Succubus' Lash of Pain spell by 3/6 sec." There is no Imp and no Succubus." |
| Hunter | Experimental Ammunition | 1/1 | ammunition element + queued rider | "*ammunition element + a queued "next cast" rider*; `Sunder` and `AreaDamage` exist, `Empowers` (`Domain.fs:839`) is healer-specific" |
| Hunter | Improved Slaying | 3/3 | per-`CreatureType` damage | "*per-`CreatureType` damage*; `CreatureType` exists, nothing reads it" |
| Druid (feral) | Carnage | 1/2 | self-heal proportional to damage dealt | "**A self-heal proportional to damage dealt**. `Effect.Heal` takes fixed min/max (`Domain.fs:810`); nothing reads back the damage just dealt." |
| Druid (feral) | Nature's Grasp | 1/1 | reactive buff on being struck | "**A reactive buff on the caster that fires when struck**. No effect in `Effect` reacts to being hit." |
| Druid (feral) | Improved Nature's Grasp | 4/4 | same axis, +chance | "Rides the same axis as Nature's Grasp; +65% of nothing until that exists." |
| Druid (moonkin) | Nature's Reach | 2/2 | per-spell range multiplier | "**Per-spell range multiplier.** `Ability.Range` is a literal fixed at construction (`Domain.fs` `Ability`); no `SpellTalent.RangeBp`." |
| Druid (moonkin) | Eclipse | 1/1 | two school-scoped stacking buffs | "**Two named stacking buffs with their own cooldowns and mutual exclusion**, whose magnitude depends on spell crit ("10% plus 60% of your spell critical strike chance") and which keys off **damage school** (Arcane vs Nature). No `Aura` carries a school-scoped damage bonus." |

*Researcher inference: `Nature's Reach` would fall out of `SpellTalent` almost free if anyone adds a
`RangeBp` beside the existing `CastTicksOff`, and `Improved Lay on Hands` is the same for a
target-side `ArmorBonus`; both are listed here as one-offs because no second talent in any note needs
them.*

## 4. Contradictions between the notes

1. **Whether a snare is an axis.** `mage.md` says "`Effect` has no snare, so the slow is not modelled"
   (`frostboltRanks`, `Content.fs:1209`) and buckets Frostbite 3/3 and Permafrost 3/3 under a
   *snare/Chill* axis. `warlock.md` says the opposite about the same model: "`Crippled of remaining *
   slowBp` exists (`Domain.fs:248`) and `Aura.movementSlowBp` reads it (`Domain.fs:446`), but the only
   writer is Crippling Poison at `SimCombat.fs:1308`, so "4/7/10% chance to slow 50% for 5 sec" is one
   new `Effect` case rather than a new axis." **The warlock reading is the correct one** (verified:
   `Aura.Crippled` and the movement-slow read exist) — Frostbite/Permafrost is an `Effect` case, not an
   axis, and `mage.md`'s own bucket-(b) count is one too high.
2. **Whether a damage multiplier on one ability is an axis.** `mage.md` (Arcane Rupture), `warlock.md`,
   `hunter.md` §5(b), `druid-feral.md` (Predatory Strikes, Feral Aggression) and `druid-moonkin.md`
   (Improved Moonfire, Moonfury) all rest on "`SpellTalent` (`Domain.fs:1109-1118`) carries
   `CostBp`/`HealBp`/`CritBp`/`CastTicksOff` and **no damage field**". The current code has
   `SpellTalent.DamageBp`, `Talents.DamageDoneBp`, `Talents.SpellCostBp` and the `Talents.damageBp`
   fold (`Domain.fs:1109-1125`, `Domain.fs:1218-1222`) — added by the warlock and Hunter ports.
   `hunter.md` §8 is the only note that knows: "Improved Stings moved from (b) to (a) between the note
   and the port, because the Warlock port made `DamageOverTime` take `Talents.damageBp` at application
   time." The other four notes are stale on this one field.
3. **The units of `ticks`.** `priest.md` records the correction explicitly: "`CastTicksOff =
   secTicks 0.5` on "Smite" and "Heal" — exactly -0.5 s, which is **ten ticks**, not five: the sim
   runs **twenty ticks a second** (`secTicks 1.0` = 20, `ticks 5` = 0.25 s). This note originally said
   `ticks 5`; the port caught it, and any other note that writes a duration as a bare `ticks N` is off
   by the same factor of two." `druid-moonkin.md` writes `CastTicksOff = ticks 10` (-0.5 s), which
   agrees; `mage.md` writes `{ Spell = "Frostbolt"; CastTicksOff = ticks 0.5 }` for -0.5 s, the error
   the priest note warns about. **A port reading the mage note literally would be 20× short.**
4. **Whether the `heroTraits` grant is earned.** `druid-moonkin.md` §6.4: the sim hands
   `MoonkinDruid` `healerTraits 500 7000` (`Content.fs:3920`); the 7000 is "almost exactly Guidance of
   the Dream 3/3's 70%, which this build *does* buy, so that one is fair. The **+5% crit is not in the
   build at all**" — a grant OctoWoW does not make. No note contradicts this directly, but it is a
   case where the sim currently over-grants, so any axis ranking that counts "already modelled" talents
   as satisfied should not assume the existing trait record is neutral.
5. **Two notes bucket the same talent twice within themselves.** `shaman.md` files Ancestral Swiftness
   in (b) ("effectiveness modifier on an empowered cast") *and* in (c) ("renames the `naturesSwiftness`
   row"). `hunter.md` files Improved Concussive Shot in (c) "plus a (b) tail" (a stun chance on a row
   that does not exist). `paladin.md` files Holy Shock in (b) for either-target and Blessed Strikes in
   (b) for two different axes. **Read those three as multi-axis entries**, not double-counted ones.

## 5. Where the notes' axes are already half-built

Each of these is a note naming a change that the code has already started (verified in `Domain.fs` this
session unless a note is cited as the source):

| note's axis | what already exists | who it finishes |
|---|---|---|
| damage multiplier on one ability / per-spell damage | `SpellTalent.DamageBp` (`Domain.fs:1109-1125`), `Talents.DamageDoneBp`, `Talents.damageBp` (`Domain.fs:1218-1222`), applied to periodic damage since the warlock port | mage Arcane Rupture (damage half), moonkin Improved Moonfire + Moonfury, feral Feral Aggression + Predatory Strikes (damage half), hunter Improved Marksmanship |
| damage multiplier reaching a white swing | `advanceSwings` folds `Talents.damageBp` into both hands (`SimTick.fs:481`, per `hunter.md` §8-§9) | hunter Ranged Weapon Spec **landed**; warrior One-Handed Weapon Spec became live as a side effect; leaves A13's weapon-class selector |
| "your next spell" pipe | `Empower` (`Domain.fs:157`) with `Instant`/`FreeMana`/`SureCrit`, `Aura.Empowered`, and the cast-path read | paladin Holy Judgement and moonkin Nature's Grace need one field (`Empower.CastTicksOff`), not a new mechanism; shaman Ancestral Swiftness and mage Arcane Rupture need a magnitude field |
| pushback resistance | `Traits.PushbackResistanceBp` (`Domain.fs:1092`, consumed at `SimCombat.fs:305`) exists; **no `Talents` field reaches it** | priest Holy Focus 2/2 and moonkin Guidance of the Dream 3/3 — both notes name `Talents.PushbackResistanceBp` folded where `healerTraits 500 7000` is called |
| stat percent | `Talents.IntellectBp` (`Domain.fs:1136`) already multiplies Intellect before the pools are derived | shaman Ancestral Knowledge ("all your stats"), then feral Heart of the Wild's per-form Stamina/Strength |
| threat multiplier | `Entity.ThreatMultiplierBp` (`Domain.fs:1277`, read at `SimState.fs:328`) and `tankStanceBp = 50000` (`Content.fs:1650`) | warrior Defensive Tactics, shaman Nature's Grace, moonkin Subtlety — all need the scalar split, none need a new field |
| regen multiplier | `Aura.Innervated of remaining * regenBp` (`Domain.fs:284`) multiplies whatever the holder has | rogue Adrenaline Rush and feral Berserk's energy half — a new pool, the same aura shape |
| snare | `Aura.Crippled` (`Domain.fs:248`) + `Aura.movementSlowBp`, one writer (`SimCombat.fs:1308`) | mage Frostbite and Permafrost are an `Effect` case, not an axis (see §4.1) |
| multi-target melee | `Effect.MultiShot of maxTargets` (`Domain.fs:807`), resolved at `SimCombat.fs:406-427` | the bear's Swipe — `druid-feral.md` §5 records that the `shortcut:` at `Content.fs:3941-3943` saying the vocabulary cannot express an area ability "**is now false**" |
| block magnitude | `Effect.Blocks of duration` (`Domain.fs:878`) exists with no magnitude | warrior Improved Shield Slam — one int field |
| aura lifetime | `Aura.advance` is the single match over every `Aura` case | every new `Aura` on this list: adding one breaks compilation in exactly one module |

## 6. Residual risk

- **Rank counts are the notes' own** and were not re-checked against the OctoWoW payloads. Where a
  note marks a rank UNVERIFIED (warrior Improved Intervene's `$L47%`, shaman's entirely inferred
  slot→name map, mage's 29 unrecovered records) the rank here inherits that status.
- **The code checks were targeted, not exhaustive**: `Domain.fs`'s `Effect`, `Aura`, `Empower`,
  `Traits`, `Talents`/`SpellTalent` and the `Talents.damageBp` fold were read this session; `Content.fs`
  and `SimTick.fs`/`SimCombat.fs` line references are quoted from the notes, not re-read. The hunter
  note's §8 landing report is the newest evidence in the set and is consistent with what was read.
- **Two axes were ranked on a build cost this inventory infers, not one a note states**: A2 (haste)
  and A12 (spell resistance) are called "new subsystem" because the notes describe constants and an
  absent mitigation path, but nobody has scoped either.
- **Talents on two axes** (Blessed Strikes, Berserk, Natural Weapons, Predatory Strikes, Ascendance,
  Improved Concussive Shot, Ancestral Swiftness) are counted once; a port plan that budgets by axis
  should not spend the same talent twice.
