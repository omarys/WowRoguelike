# Vanilla attack table for a level-24 party vs level 19-20 elites

Read-only research. Every number has a URL. Where a number is interpolated or extrapolated I say so
explicitly and mark it **[inference]** — nothing level-60-only is presented as a level-24 value.

Reference frame used throughout: player level 24 → **defence skill 120, weapon skill 120**;
level-19 elite → defence/weapon skill **95**; level-20 elite → **100**.

---

## 1. Base defence and weapon skill

| Quantity | Vanilla rule | At level 24 |
|---|---|---|
| Player defence skill cap | `5 × level` (mobs too) | **120** |
| Player weapon skill cap | `5 × level` | **120** |
| Mob defence skill | `5 × mob level` | L19 elite → **95**, L20 elite → **100** |
| Mob weapon skill | `5 × mob level` | 95 / 100 |

Sources: "It is the Mob's level multiplied by 5" — [Miss (warcraft.wiki.gg)](https://warcraft.wiki.gg/wiki/Miss);
"the level cap is (your current level * 5)" — [Wowhead Classic stats guide](https://www.wowhead.com/classic/guide/classic-wow-stats-and-attributes-overview);
mob weapon skill assumed `level*5` — [Dodge (vanilla-wow-archive)](https://vanilla-wow-archive.fandom.com/wiki/Dodge).

**The level difference actually used is a skill difference, not a raw level difference**:
`Δ = targetDefence − attackerWeaponSkill`. At 24 vs a 19 elite, Δ = 95 − 120 = **−25** (25 points of player
advantage = the 5-level gap × 5); vs a 20 elite, Δ = **−20**. The formulas below take `TargetLevel*5`
directly, i.e. mob defence.

Two coefficients matter and are *not* interchangeable ([Miss](https://warcraft.wiki.gg/wiki/Miss), [Beaza via magey](https://github-wiki-see.page/m/magey/classic-warrior/wiki/Attack-table)):
- **0.1 % per skill point** when the *defender is a mob* (its defence vs your weapon skill).
- **0.04 % per skill point** when the *defender is a player* (their defence vs the attacker's weapon skill).
- Miss additionally moves **−0.02 %** per point of *attacker* skill over defender defence.

Attack-table order (one single roll, descending precedence) — verbatim from
[Attack table (vanilla-wow-archive)](https://vanilla-wow-archive.fandom.com/wiki/Attack_table):
`Miss → Dodge → Parry → Glancing Blow (players/pets vs mobs only) → Block → Critical hit → Crushing Blow
(mobs vs players, only at +4 levels) → Ordinary hit`.
Position: attacking from behind removes **parry and block** (dodge unaffected) for a player vs a mob, and
removes **parry, block and dodge** for a mob vs a player (same page). Block from behind is also removed —
[Block](https://vanilla-wow-archive.fandom.com/wiki/Block).

---

## 2. Miss chance

### Player → mob (Beaza formula, matches Blizzard's own 8 % statement)
[Miss (warcraft.wiki.gg)](https://warcraft.wiki.gg/wiki/Miss), [Beaza summary (magey)](https://github-wiki-see.page/m/magey/classic-warrior/wiki/Attack-table):

```
Δ = TargetLevel*5 − AttackerSkill
if Δ <= 10:  Miss% = 5 + Δ × 0.1
if Δ >  10:  Miss% = 6 + (Δ − 10) × 0.4
HitSuppression = Δ <= 10 ? 0 : (Δ − 10) × 0.2
Dual wield: Miss% = Miss% + 19          (flat, verified in Classic — the 80 %×+20 % form is pre-2.0)
Mob below level 10: Miss% = Miss% × (MobLevel / 10)
```

Blizzard's own numbers for the "+N level" ladder: 8 % vs +3 ([Blizzard test post](https://github.com/magey/classic-warrior/issues/5)
and [Wowhead "Not a Bug"](https://www.wowhead.com/classic/news/not-a-bug-combat-table-values-in-classic-wow-291996)).

Worked, level-24 attacker (skill 120):

| Target | Δ | Miss (1H/2H) | Miss (dual wield) |
|---|---|---|---|
| L19 elite (def 95) | −25 | **2.5 %** | **21.5 %** |
| L20 elite (def 100) | −20 | **3.0 %** | **22.0 %** |
| L24 mob | 0 | 5.0 % | 24.0 % |
| L25 / L26 | 5 / 10 | 5.5 % / 6.0 % | 24.5 % / 25.0 % |
| L27 (+3) | 15 | 8.0 % | 27.0 % |

Yellow (special) attacks: "The Base Miss Rate for Yellow Attacks is at a maximum 5 % … The Dual Wield
Penalty does not affect the miss rate for yellow attacks" ([Miss](https://warcraft.wiki.gg/wiki/Miss)).
Whether the sub-5 % reduction for *lower-level* targets applies to yellow attacks is **not documented in
any source I found** → [gap].

### Mob → player (the mirror)
[Miss](https://warcraft.wiki.gg/wiki/Miss): base 5 %, then **+0.04 %** per point of the defender's defence
over the attacker's attack rating, **−0.02 %** per point of attacker skill over defender defence.

Worked, level-20 elite (skill 100) swinging at a level-24 player (defence 120):
`5 + (120 − 100) × 0.04 =` **5.8 % miss**, and dodge / parry / block each get **+0.8 %** from the same
20-point gap (0.04 % per point). The same mob's crit chance is `base + (100 − 120) × 0.04 = base − 0.8 %`
(see §6).

---

## 3. Dodge

Formula ([Dodge, vanilla-wow-archive](https://vanilla-wow-archive.fandom.com/wiki/Dodge)):

```
Dodge% = BaseDodge(class) + AGI / (agiPer1Pct at your level) + items + talents + racial
         + (yourDefence − attackerWeaponSkill) × 0.04
```

Mob side ([Beaza via magey](https://github-wiki-see.page/m/magey/classic-warrior/wiki/Attack-table)):
`Dodge% = 5 + (TargetLevel*5 − AttackerSkill) × 0.1` — Blizzard confirms the shape: 5 % at equal level,
**+0.5 % per level above you** ([Blizzard](https://us.forums.blizzard.com/en/wow/t/bug-hit-tables/185675/12)).

Worked: mob dodging the level-24 player → L19 elite **2.5 %**, L20 elite **3.0 %**.
Player dodging a level-20 elite → base + agi term **+ 0.8 %**.

### Base dodge per class (level-60 table, "% Live as of 1.12")
[Dodge](https://vanilla-wow-archive.fandom.com/wiki/Dodge): Druid 0.9 %, Hunter 0.0 %, Mage 3.2 %,
Paladin 0.7 %, Priest 3.0 %, Rogue 0.0 %, Shaman 1.7 %, Warlock 2.0 %, Warrior 0.0 %.
These are class constants; that they hold unchanged at 24 is **[inference]** (no low-level base-dodge table
published). Night elves add +1 % ([same page](https://vanilla-wow-archive.fandom.com/wiki/Dodge)).

### Agility → 1 % dodge at level 24
It is **not linear in agility**, and the per-point value **changes with level** ("the AGI:Dodge ratio also
depends on the character level" — [Dodge](https://vanilla-wow-archive.fandom.com/wiki/Dodge)); a level-1
rogue needs 3.5 agi for 1 % crit
([Blizzard forum](https://us.forums.blizzard.com/en/wow/t/formula-for-calculating-agility-needed-per-crit/534418)),
a level-60 rogue 14.5. Sourced anchors:

- **Level 19 measured** ([XPOff 19s guide](https://xpoff.com/threads/in-depth-stat-guide-for-19s-with-scaling-formulas.93237/)):
  0.13 % dodge/agi for every class except Hunters (0.14 %) and Rogues (0.2633 %) → 7.69 / 7.14 / 3.80 agi per 1 %.
- **Level 60 table** ([Dodge](https://vanilla-wow-archive.fandom.com/wiki/Dodge)): Druid 20, Hunter 26.5,
  Mage 19.444, Paladin 19.767, Priest 20, Rogue 14.5, Shaman 19.697, Warlock 20, Warrior 20 agi per 1 %.

Linear interpolation in *level* between those two anchors **[inference — no published level-24 table exists]**:

| Class | agi per 1 % dodge @24 | % dodge per agi @24 |
|---|---|---|
| Rogue | ≈ 5.1 | ≈ 0.196 % |
| Hunter | ≈ 9.5 | ≈ 0.105 % |
| all others | ≈ 9.2 | ≈ 0.109 % |

Treat these as ±25 %. The authoritative fix, if exactness matters: the VMaNGOS world table
`player_dodge_per_agility` (class, level, rate) — schema documented at
[VMaNGOS wiki](https://vmangos.github.io/wiki/world/player_dodge_per_agility/); the *values* are not on the
wiki page, they are in the server DB dump.

---

## 4. Parry

- **Base parry is 5 %** for classes that have the passive, **0 %** otherwise. The passive is learned:
  Warrior 6, Paladin 8, Hunter 8, Rogue 12; Shaman only via the 20-point Enhancement talent "Spirit Weapons";
  Mage / Priest / Warlock / Druid have none in vanilla
  ([Parry, vanilla-wow-archive](https://vanilla-wow-archive.fandom.com/wiki/Parry)).
  At level 24 the party therefore has 5 % parry on Warrior/Paladin/Hunter/Rogue and **0 % on Shaman**
  (20 talent points unreachable), Druid, Mage, Priest, Warlock.
- Formula ([Parry](https://vanilla-wow-archive.fandom.com/wiki/Parry)):
  `% = 5 + parryRating/talents + (defenceSkill − attackerWeaponSkill) × 0.04` — i.e. **0.04 % per point of
  defence above 5 × level against players**, and **0.1 % per point against mobs** ("0.04 % against players
  and 0.1 % against mobs", Beaza via [Dodge](https://vanilla-wow-archive.fandom.com/wiki/Dodge)).
- Worked (player defending vs a level-20 elite): `5 + (120 − 100) × 0.04 =` **5.8 %**.

### Mob parry — the known non-linearity
Blizzard: "Creatures that are 3 levels above the player have a 14 % Parry chance"
([Blizzard](https://us.forums.blizzard.com/en/wow/t/bug-hit-tables/185675/12),
[Wowhead](https://www.wowhead.com/classic/news/not-a-bug-combat-table-values-in-classic-wow-291996)),
while the linear formula only gives `5 + 15×0.1 = 6.5 %`. Magey's own measurements at +0 / +1 / +2 are
4.97 % / 5.75 % / 6.55 % — i.e. ~+0.8 % per level, then a jump to 14 % at +3
([test breakdown](https://github-wiki-see.page/m/magey/classic-warrior/wiki/Attack-table)).
For **lower**-level targets the linear term is the only defensible extrapolation **[inference]**:
L19 elite **2.5 %**, L20 elite **3.0 %** — but note the +1/+2 data already run ~0.7 % above the linear
prediction, so 3.0-3.7 % for a level-20 elite is the honest range.

---

## 5. Block

- **Shield required.** Base block **5 %**; vs mobs `MIN(5 %, 5 + Δ×0.1)` (mobs can never block more than
  5 %), vs players `playerBlock + (defence − attackerSkill) × 0.04`
  ([Beaza via magey](https://github-wiki-see.page/m/magey/classic-warrior/wiki/Attack-table),
  [Block](https://vanilla-wow-archive.fandom.com/wiki/Block)).
- Worked (player defending vs L20 elite): **5.8 %**. (Mob blocking the player: 3.0 % vs a L20 elite, 2.5 %
  vs L19 — and only mobs actually carrying a shield.)
- **Block value = shield's block value + 1 per 20 Strength** (vanilla):
  [Block value (vanilla-wow-archive)](https://vanilla-wow-archive.fandom.com/wiki/Block_value) —
  "The block value is increased by one for twenty points of strength";
  [ZugZugDB](https://zugzugdb.com/formulas/block-value) — `damage blocked = shield block value + strength/20`;
  [XPOff](https://xpoff.com/threads/in-depth-stat-guide-for-19s-with-scaling-formulas.93237/) — "For every 20
  points of Strength, these classes can block one additional point".
  A block subtracts that flat amount from the hit; it is not a percentage. Rounding (floor vs round) is
  **unverified** — use `floor(str/20)` and flag it.
- **Level-24 shields with sourced numbers**:
  - **Redbeard Crest** — ilvl 24, requires 19, **547 armor, 11 block**, +6 Str, +3 Sta
    ([Grimoire](https://wowgrimoire.app/items/12997)). At six strength that is 11 + 0 = **11 block value**
    (Str/20 rounds to nothing).
  - **Kite Shield** — ilvl 27, requires 22, **491 armor, 9 block**
    ([Wowhead Classic](https://www.wowhead.com/classic/item=2446/kite-shield)).
  A level-24 warrior in BiS (~60-80 Str) would block ≈ 11 + 3..4 = **14-15** per block with Redbeard Crest.

---

## 6. Crit, and how crit and dodge share one roll

### Crit from agility
Anchors: level 19 per-point ([XPOff](https://xpoff.com/threads/in-depth-stat-guide-for-19s-with-scaling-formulas.93237/)) —
Druid/Paladin/Shaman/Warrior 0.13 %, Mage/Priest/Warlock ≈0.08 %, Hunter ≈0.07 %, Rogue 0.1316 %;
level 60 agi-per-1 %-crit ([Attributes](https://worldofwarcraft.fandom.com/et/wiki/Attributes),
[citation needed] on that page): Warrior 20, Paladin 19.55, Priest 21.94, Warlock 21.03, Mage 22.64,
Rogue 28.17 (29 is also widely cited), Shaman 31.28, Druid 32.48, Hunter 33.17.

Interpolated to 24 **[inference, ±25 %]**:

| Class | agi per 1 % crit @24 | % crit per agi @24 |
|---|---|---|
| Warrior | ≈ 9.2 | ≈ 0.109 % |
| Paladin | ≈ 9.1 | ≈ 0.109 % |
| Rogue | ≈ 10.1 | ≈ 0.099 % |
| Druid | ≈ 10.7 | ≈ 0.093 % |
| Shaman | ≈ 10.6 | ≈ 0.095 % |
| Hunter | ≈ 16.6 | ≈ 0.060 % |
| Warlock | ≈ 13.5 | ≈ 0.074 % |
| Mage | ≈ 13.7 | ≈ 0.073 % |
| Priest | ≈ 13.7 | ≈ 0.073 % |

There is no linear agility→crit formula; both per-point conversions move with level
([Blizzard forum thread](https://us.forums.blizzard.com/en/wow/t/crit-decreasing-as-level-increases/657959):
"Agility's conversion to dodge and crit both scale with level"). VMaNGOS table to use if exactness matters:
[`player_crit_per_agility`](https://vmangos.github.io/wiki/world/player_crit_per_agility/).

### Crit vs a lower-level target (this is a real bonus, not suppression)
[Beaza via magey](https://github-wiki-see.page/m/magey/classic-warrior/wiki/Attack-table):

```
BaseAttackRating = MIN(PlayerLevel*5, AttackRating)      // = 120 at level 24
Δ = BaseAttackRating − TargetDefence
if Δ <  0: crit += Δ × 0.2      (only when the target is a mob)
if Δ >= 0: crit += Δ × 0.04
vs a player: crit += (AttackRating − TargetDefence) × 0.04
```

Worked at 24: vs L19 elite Δ = +25 → **+1.0 % crit**; vs L20 elite Δ = +20 → **+0.8 % crit**; vs a L24 mob
Δ = 0 → no change; vs +3 mobs −3 %.
Glancing blows use `10 % + (TargetLevel*5 − MIN(AttackerLevel*5, AttackerSkill)) × 2 %`
([same page](https://github-wiki-see.page/m/magey/classic-warrior/wiki/Attack-table)) → at 24 vs a L19 elite
that is 10 − 50 = **negative, i.e. no glancing blows at all**; only equal-or-higher mobs glance (10 % at
equal level). Clamping at 0 is **[inference]**.

### Crit/dodge interaction
There is one roll and crit sits **below** miss/dodge/parry/glancing/block in it
([Attack table](https://vanilla-wow-archive.fandom.com/wiki/Attack_table)). Consequences to encode:
- "Miss chance and Critical chance are unmodified by Dodge … one roll determines outcome of a swing, so
  percentages are absolute" ([Dodge](https://vanilla-wow-archive.fandom.com/wiki/Dodge)). A dodged attack
  can never have been a crit; effective crit = `min(critChance, 100 − miss − dodge − parry − glancing − block)`.
- Crit suppression is *not* "crit − miss − dodge" applied to a separate roll; it is the table running out
  of room.
- Blocked hits can never crit (block precedes crit in the table).

---

## 7. Spell miss / resist for a level-24 caster

Miss by target level ([Formulas:Magical resistance, wowwiki archive](https://wowwiki-archive.fandom.com/wiki/Formulas:Magical_resistance)):

| Target | PvE | PvP |
|---|---|---|
| same level | **4 %** | 4 % |
| +1 | 5 % | 5 % |
| +2 | 6 % | 6 % |
| +3 | 17 % | 13 % |
| +4 | 28 % | 20 % |
| each further level | +11 % | +7 % |

A miss is a **full** resist — partial resists come only from resistance, not from the hit roll
([warcraft.wiki.gg Resist](https://warcraft.wiki.gg/wiki/Resist): "a miss results in a full resist (partial
resists cannot occur)"; same page confirms spell attacks use multiple rolls, melee uses one).

**Level 19-20 elite target (4-5 levels *below* the caster).** No published table covers negative level
differences; the wiki only says "If the target is significantly lower level, the spell will very rarely
fail" ([same page](https://wowwiki-archive.fandom.com/wiki/Formulas:Magical_resistance)). Vanilla has a
hard **1 % floor** on spell miss ([EU forum](https://eu.forums.blizzard.com/en/wow/t/something-is-wrong-with-the-amount-of-spell-resist/98116),
[WowX5 hit-cap guide](https://wowx5.ru/en/guides/spell-hit-cap-guide)) → use **1 % miss** for a level-24
caster vs a L19/L20 elite. That specific value is **[inference from the 1 % floor + "very rarely fail"]**,
medium confidence.

**Partial resists** require resistance on the target. Average mitigation is
`AverageResistance = (TargetResistance / (CasterLevel × 5)) × 0.75`, capped at 75 %, and
"casters below level 20 are treated as if they were level 20"
([Formulas:Magical resistance](https://wowwiki-archive.fandom.com/wiki/Formulas:Magical_resistance)).
At caster level 24 the denominator is **120**, so a level-20 elite with 24 fire resistance would average
`0.75 × 24/120 = 15 %` reduction, rolled to 0/25/50/75 %. With **zero** resistance of that school the
result is a full hit or a full resist — **no partial resists on a same-level, unresistant target**.

---

## 8. Armour damage reduction

Vanilla (1.12) form, verbatim: `Damage Reduction = Armor / (Armor + 400 + 85 × AttackerLevel)`
([magey/tbc-warrior issue #22](https://github.com/magey/tbc-warrior/issues/22), quoting Satrina's Evil
Empire guide; the same issue flags that the 400/85 constants and the "X = attacker level" placement changed
at 2.0/3.0, so this is the 1.x form).
**The attacker's level goes in the denominator** — not the target's. At level 24 the constant is
`400 + 85 × 24 = 2440`; at level 20 it is `2100`; example: 1500 armour vs a level-20 elite attacker →
`1500/3600 = 41.7 %` reduction, vs a level-24 attacker → `1500/3940 = 38.1 %`.
(Attacker levels ≥ 60 use a different form — irrelevant here, which is exactly why the level-24 numbers
above must not be borrowed from level-60 guides.)

I could not verify the repository's own implementation of this formula (no shell/grep available in this
run, and `src/WowRoguelike.Core/*.fs` was only partially read) — the shape and the denominator above are
what the code should match.

---

## Contradictions / disagreements between sources

1. **Strength → block value: /20 vs /2.** Vanilla sources say **1 block value per 20 Str**
   ([Block value, vanilla-wow-archive](https://vanilla-wow-archive.fandom.com/wiki/Block_value),
   [ZugZugDB](https://zugzugdb.com/formulas/block-value), [XPOff](https://xpoff.com/threads/in-depth-stat-guide-for-19s-with-scaling-formulas.93237/) — all agree).
   [Wowpedia Block value](https://wowpedia.fandom.com/wiki/Block_value) and Cynwise's 2008 twink guide
   ([Cynwise](https://cynwise.wordpress.com/tag/level-19-twinks/page/2/), "2 points of Strength to 1 point
   of Block Value") give **/2**, which is the TBC+ value. **Trust /20** for vanilla.
2. **Dual-wield miss penalty: `80 % × base + 20 %` vs flat `+19 %`.** Beaza's pre-2.0 text says the
   multiplic

ative form; Classic-era parsing by Bimmy (300 skill, +27 % hit, 6 000+ swings) showed a flat **+19 %**
   ([magey](https://github-wiki-see.page/m/magey/classic-warrior/wiki/Attack-table)). **Trust flat +19 %**
   for 1.12 — this is the only place I rate the Classic-era reverse-engineering above the contemporary wiki.
3. **Parry vs higher-level mobs.** Linear 0.1 %/point gives 6.5 % at +3; Blizzard and magey's data give
   **14 %** ([Blizzard](https://us.forums.blizzard.com/en/wow/t/bug-hit-tables/185675/12),
   [Wowhead](https://www.wowhead.com/classic/news/not-a-bug-combat-table-values-in-classic-wow-291996)).
   The linear form is only validated to +2. Mobs' dodge/block stay linear; parry does not.
4. **`warcraft.wiki.gg/wiki/Miss` (retail-curated) vs vanilla archive.** The retail page keeps the same
   two-branch PvE formula but adds an "attack rating over defence reduces miss by 0.02 %" line that the
   vanilla archive states only for mob-vs-player. For a PvE sim the difference is nil; for PvP it is not.
5. **Crit per agi at 60** is `[citation needed]` on the fandom Attributes table (rogue 28.17 vs 29 commonly
   cited elsewhere). It only feeds my L24 interpolation, so the ±25 % band covers it.

## Missing evidence

- **No authoritative per-level (24) agility→dodge/crit table** in any public wiki; the numbers in §3/§6 are
  interpolations between a sourced L19 measurement and a sourced L60 table. The real table is
  `player_dodge_per_agility` / `player_crit_per_agility` in the VMaNGOS DB dump (schema documented, values
  not published on the wiki).
- **Sub-5 % miss for yellow attacks vs lower-level mobs** — undocumented.
- **Mob crit rate vs a player at 24** (base mob crit %) — not researched/published for low levels; only the
  0.04 %-per-defence-point adjustment is sourced.
- **Block-value rounding** (floor/round of Str/20) — unverified.
- **Whether base dodge / base parry class constants are level-invariant below 60** — assumed, not sourced.
- **[inference] Clamping** of the glancing formula and of negative avoidances at 0 — implied by the
  game's behaviour, never stated.
- **The repo's current armour formula**: not readable in this run (no shell), so "matches vanilla" is
  unverified.

## Sources
- Kept: [Attack table (vanilla-wow-archive)](https://vanilla-wow-archive.fandom.com/wiki/Attack_table) — table order + positional rules, verbatim.
- Kept: [Miss (warcraft.wiki.gg)](https://warcraft.wiki.gg/wiki/Miss) — two-branch PvE miss formula, DW +19 %, 5 % yellow cap.
- Kept: [magey/classic-warrior Attack table wiki](https://github-wiki-see.page/m/magey/classic-warrior/wiki/Attack-table) — Beaza formulas verbatim + measured L20-L40 test data; best single source for parry/block/crit defence coefficients.
- Kept: [Blizzard hit-table post](https://us.forums.blizzard.com/en/wow/t/bug-hit-tables/185675/12) — official 5 %/+0.5 % dodge, 14 % parry at +3, 8 % miss at +3, −1 % crit/level.
- Kept: [Dodge (vanilla-wow-archive)](https://vanilla-wow-archive.fandom.com/wiki/Dodge) — dodge formula, level-60 base dodge + agi ratio table.
- Kept: [Parry (vanilla-wow-archive)](https://vanilla-wow-archive.fandom.com/wiki/Parry) — who has parry and at what level, 5 % base, 0.04 %/defence.
- Kept: [Block value (vanilla-wow-archive)](https://vanilla-wow-archive.fandom.com/wiki/Block_value) + [ZugZugDB](https://zugzugdb.com/formulas/block-value) — Str/20.
- Kept: [Formulas:Magical resistance](https://wowwiki-archive.fandom.com/wiki/Formulas:Magical_resistance) — spell miss ladder + average-resistance formula with caster level.
- Kept: [XPOff 19s stat guide](https://xpoff.com/threads/in-depth-stat-guide-for-19s-with-scaling-formulas.93237/) — the only measured low-level agi→crit/dodge per-point values found.
- Kept: [magey/tbc-warrior issue #22](https://github.com/magey/tbc-warrior/issues/22) — 1.12 armour formula verbatim with attacker level.
- Kept: [Wowhead Kite Shield](https://www.wowhead.com/classic/item=2446/kite-shield), [Grimoire Redbeard Crest](https://wowgrimoire.app/items/12997) — level-24-usable shield armour/block values.
- Deprioritized: Worldofwarcraft.fandom "Attributes" — crit-per-agi table is `[citation needed]`; used only as a second anchor.
- Deprioritized: Wowpedia Block value / Cynwise — TBC-era Str/2, contradicts vanilla.
- Deprioritized: [WowX5 hit-cap guide](https://wowx5.ru/en/guides/spell-hit-cap-guide) — SEO-ish, used only to corroborate the 1 % spell-miss floor.
- Rejected: [warcraft.wiki.gg/wiki/Resist](https://warcraft.wiki.gg/wiki/Resist) — retail framing, no vanilla miss table (but its "miss = full resist" line is useful).
- Rejected: [VMaNGOS `player_dodge_per_agility`](https://vmangos.github.io/wiki/world/player_dodge_per_agility/) as a *value* source — documents the schema only, no rows.

## Next steps
1. Pull `player_dodge_per_agility` and `player_crit_per_agility` rows for level 24 from a VMaNGOS/Turtle DB
   dump (or a `wow_world_base` stat table) and replace the §3/§6 interpolations with exact values.
2. If yellow-attack miss vs lower-level mobs matters for ability damage, measure or locate a Classic-era
   parse of special-attack miss vs a grey mob.
3. Confirm mob base crit % at level 19-20 (one Classic-era test log would settle it).
