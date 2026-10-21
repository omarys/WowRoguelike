# Research: Mage damage ladder, levels 13–40 (vanilla 1.12 / Classic Era)

Scope note: base attribute rows already exist in the repo, so they are not re-derived here.
All spell tables come from one primary tabulation of the Classic client (wowcalc.net, build
1.15.9.70003 — "Classic 1.12 / 1.15.9" page) and were spot-checked against classicdb.ch (1.12
data) for rank levels. Item rows come from classicdb.ch item pages.

**Data-version caveat (read first).** Wowcalc's tooltips are the 1.15.x Classic Era values. Where
I cross-checked against classicdb 1.12 the low ranks differ by a few points (Fireball R1 14–23 in
1.12 vs 16–24 in 1.15; Frostbolt R1 18–21 vs 20–22; Arcane Explosion R1 32–37 vs 34–38; Arcane
Missiles R1 24/s vs 25/s). Ranks 3+ were not cross-checked against 1.12, so treat them as
single-source, medium confidence, not UNVERIFIED.

---

## 1. Ability ranks, level 13–40

Cooldowns: every damage spell below has **0 s cooldown** (global cooldown 1.5 s applies to the
instant ones). Only Counterspell has a real cooldown (30 s). "-" = not applicable.
Damage is the printed tooltip range at 0 spell power; "total" adds the DoT/tick component.

### Fireball (train levels 12, 18, 24, 30, 36 in this window; R8 is level 42)

| Rank | Lvl | Mana | Cast | Direct dmg | DoT | DoT ticks | Total/cast | dmg/mana | dmg/s cast |
|---|---|---|---|---|---|---|---|---|---|
| 3 | 12 | 65 | 2.5 s | 57–77 | 6 over 6 s | 3 × 2 | 73 | 1.12 | 29.2 |
| 4 | 18 | 95 | 3.0 s | 89–121 | 12 over 8 s | 4 × 3 | 117 | 1.23 | 39.0 |
| 5 | 24 | 140 | 3.5 s | 146–194 | 20 over 8 s | 4 × 5 | 190 | 1.36 | 54.3 |
| 6 | 30 | 185 | 3.5 s | 207–273 | 28 over 8 s | 4 × 7 | 268 | 1.45 | 76.6 |
| 7 | 36 | 220 | 3.5 s | 264–344 | 32 over 8 s | 4 × 8 | 336 | 1.53 | 96.0 |

Total/cast, dmg/mana and dmg/s are computed here from the printed ranges (researcher calculation,
not source text). DoT is Fire damage, 2 s tick interval, does not stack from the same caster.

### Frostbolt (train levels 14, 20, 26, 32, 38)

| Rank | Lvl | Mana | Cast | Damage | Snare | Total/cast | dmg/mana | dmg/s cast |
|---|---|---|---|---|---|---|---|---|
| 3 | 14 | 50 | 2.2 s | 54–60 | 40 % / 6 s | 57 | 1.14 | 25.9 |
| 4 | 20 | 65 | 2.6 s | 78–86 | 40 % / 7 s | 82 | 1.26 | 31.5 |
| 5 | 26 | 100 | 3.0 s | 132–144 | 40 % / 7 s | 138 | 1.38 | 46.0 |
| 6 | 32 | 130 | 3.0 s | 180–196 | 40 % / 8 s | 188 | 1.45 | 62.7 |
| 7 | 38 | 160 | 3.0 s | 235–255 | 40 % / 8 s | 245 | 1.53 | 81.7 |

Snare is a movement-speed debuff (chilled), no periodic damage, so no tick count.

### Arcane Missiles (train levels 16, 24, 32, 40)

| Rank | Lvl | Mana | Channel | Per-second | Ticks | Total | dmg/mana | dmg/s |
|---|---|---|---|---|---|---|---|---|
| 2 | 16 | 140 | 4 s | 37 | 4 | 148 | 1.06 | 37.0 |
| 3 | 24 | 235 | 5 s | 58 | 5 | 290 | 1.23 | 58.0 |
| 4 | 32 | 320 | 5 s | 85 | 5 | 425 | 1.33 | 85.0 |
| 5 | 40 | 410 | 5 s | 117 | 5 | 585 | 1.43 | 117.0 |

Channel, not a cast: interruption/pushback truncates the remaining missiles. One missile per
second; the first missile lands immediately on cast start.

### Arcane Explosion (train levels 14, 22, 30, 38)

| Rank | Lvl | Mana | Cast | Damage (10 yd AoE, per target) | dmg/mana per target |
|---|---|---|---|---|---|
| 1 | 14 | 75 | instant | 34–38 | 0.48 |
| 2 | 22 | 120 | instant | 60–66 | 0.53 |
| 3 | 30 | 185 | instant | 101–109 | 0.57 |
| 4 | 38 | 250 | instant | 143–155 | 0.60 |

### Blizzard (train levels 20, 28, 36)

| Rank | Lvl | Mana | Channel | Total (8 s, per target) | Per second | dmg/mana |
|---|---|---|---|---|---|---|
| 1 | 20 | 320 | 8 s | 200 | 25 | 0.63 |
| 2 | 28 | 520 | 8 s | 360 | 45 | 0.69 |
| 3 | 36 | 720 | 8 s | 528 | 66 | 0.73 |

Tick structure: 1 s tick interval, 8 ticks per full channel (tick count = duration/tick interval;
the printed text gives only the 8 s total, per-tick split is researcher inference).

### Polymorph (train levels 8, 20, 40)

| Rank | Lvl | Mana | Cast | Duration |
|---|---|---|---|---|
| 1 | 8 | 60 | 1.5 s | 20 s |
| 2 | 20 | 90 | 1.5 s | 30 s |
| 3 | 40 | 120 | 1.5 s | 40 s |

No damage. Single target, Beast/Humanoid/Critter only, breaks on any damage, one target at a time.

### Counterspell (single rank, level 24)

| Rank | Lvl | Mana | Cast | Cooldown | Effect |
|---|---|---|---|---|---|
| 1 | 24 | 100 | instant | 30 s | interrupts; locks that spell school for 10 s; high threat |

---

## 2. Wands (the auto-attack between casts)

| Wand | Req lvl | Damage | School | Speed | DPS | Source / note |
|---|---|---|---|---|---|---|
| Greater Magic Wand | 13 | 22–41 | Arcane | 1.80 | 17.5 | crafted (enchanting); item 11288 |
| Smoldering Wand | 15 | 15–28 | Fire | 1.60 | 13.4 | drop; worse than Greater Magic Wand |
| Cookie's Stirring Rod | 17 | 20–38 | Arcane | 1.30 | 22.3 | BoP, Deadmines (Cookie); 50/50 durability |
| Spellcrafter Wand | 26 | 24–45 | Arcane | 1.70 | 20.3 | **downgrade in DPS vs Cookie's** — skip it |
| Gravestone Scepter | 26 (listed "Level 29") | 30–57 | Shadow | 1.50 | 29.0 | +1 Spirit, +5 Shadow Res; level label ambiguous |
| Moonbeam Wand | 30 | 30–57 | Nature | 1.80 | 24.2 | — |
| Excavation Rod | 30 | 32–60 | Fire | 1.90 | 24.2 | — |
| Consecrated Wand | 30 | 20–38 | Nature | 1.20 | 24.2 | lowest per-hit, fastest |
| Captain Rackmore's Tiller | 36 | 31–58 | Frost | 1.50 | 29.7 | — |
| Flash Wand | 37 | 27–52 | Nature | 1.30 | 30.4 | — |
| Kodo Brander | 38 | 41–77 | Arcane | 1.90 | 31.1 | highest DPS in window |

Bands actually used in practice: **13–16** Greater Magic Wand (17.5 dps) → **17–25** Cookie's
Stirring Rod (22.3 dps) → **26–29** Gravestone Scepter (29.0 dps; UNVERIFIED exact req level, the
page prints "Level 29" under Quick Facts rather than "Requires Level") → **30–35** any of the
24.2 dps trio → **36–37** Rackmore's Tiller → **38–40** Kodo Brander.
Note the counter-intuitive fact: wand DPS at 17 (22.3) already exceeds every Fireball/Frostbolt
cast DPS until level ~22–24, and stays above Arcane Explosion per-cast damage the whole way.

---

## 3. Gear bands (cloth, dungeon drops + quest rewards; no 60 twinks)

Armour values are classicdb "Armor". Where a slot is missing I could not verify a level-appropriate
item and it is marked UNVERIFIED rather than filled with a guess.

| Band | Item | Slot | Req lvl | Armour | Bonuses |
|---|---|---|---|---|---|
| 13–17 | Robe of the Moccasin | chest | 17 | 36 | +2 Str, +2 Sta, +6 Spi |
| 13–17 | Gold-flecked Gloves | hands | 17 | 22 | +4 Str, +3 Int |
| 13–17 | Glowing Lizardscale Cloak | back | 17 | 20 | +6 Agi, +2 Spi |
| 13–17 | legs / waist / feet | — | — | — | UNVERIFIED |
| 13–17 | **weapon:** Greater Magic Wand (13) / Smoldering Wand (15); staff at ≤17 UNVERIFIED (Emberstone Staff is 18) | | | | |
| 18–22 | Emberstone Staff | 2H staff | 18 | — | +8 Int, +5 Spi, +5 Sta |
| 18–22 | Cape of the Brotherhood | back | 20 | 21 | +6 Agi, +3 Sta |
| 18–22 | Robe of the Moccasin (carried over) | chest | 17 | 36 | +2 Str, +2 Sta, +6 Spi |
| 18–22 | legs / hands / feet | — | — | — | UNVERIFIED |
| 18–22 | **weapon:** Cookie's Stirring Rod (17, 22.3 dps) | | | | |
| 23–27 | Robes of Arugal | chest | 24 | 46 | +9 Int, +10 Spi, +5 Sta, +3 Agi |
| 23–27 | Belt of Arugal | waist | 24 | 26 | +10 Int, +3 Spi, +2 Agi |
| 23–27 | Rod of the Sleepwalker | 2H staff | 24 | — | +11 Int, +10 Spi |
| 23–27 | Wind Spirit Staff | 2H staff | 27 | — | +5 Int, +15 Spi, +3 Sta |
| 23–27 | hands / legs / feet / head | — | — | — | UNVERIFIED |
| 23–27 | **weapon:** Rod of the Sleepwalker → Wind Spirit Staff; wand Spellcrafter Wand (26) is a DPS downgrade, keep Cookie's | | | | |
| 28–32 | Nightsky Robe | chest | 32 | 49 | +8 Sta, +8 Int, +8 Spi |
| 28–32 | Nightsky Boots | feet | 29 | 32 | +4 Int, +8 Sta |
| 28–32 | Necromancer Leggings | legs | 30 | 45 | +11 Int, +12 Sta, +10 Shadow spell dmg |
| 28–32 | head / hands / waist | — | — | — | UNVERIFIED |
| 28–32 | **weapon:** Rod of the Sleepwalker / Wind Spirit Staff; wand Moonbeam Wand or Excavation Rod (30) | | | | |
| 33–37 | Robe of Doan | chest | 33 | 50 | +4 Sta, +13 Spi |
| 33–37 | Mantle of Doan | shoulders | 33 | 38 | +8 Int, +7 Spi |
| 33–37 | Robe of the Magi | chest | 35 | 58 | +6 Int, +5 Spi, **+22 spell dmg** |
| 33–37 | Illusionary Rod | 2H staff | 34 | — | +15 Int, +10 Spi, +7 Sta |
| 33–37 | head / hands / legs / feet | — | — | — | UNVERIFIED |
| 33–37 | **weapon:** Illusionary Rod; wand Captain Rackmore's Tiller (36) → Flash Wand (37) | | | | |
| 38–40 | Whitemane's Chapeau | head | 39 | 52 | +14 Int, +14 Spi, +9 Sta |
| 38–40 | Triune Amulet | neck | 39 | — | +7 Int, +7 Spi, +7 Sta |
| 38–40 | Robe of the Magi | chest | 35 | 58 | +6 Int, +5 Spi, +22 spell dmg |
| 38–40 | Necromancer Leggings | legs | 30 | 45 | +11 Int, +12 Sta, +10 Shadow dmg |
| 38–40 | hands / waist / feet | — | — | — | UNVERIFIED |
| 38–40 | **weapon:** Illusionary Rod; wand Kodo Brander (38, 31.1 dps) | | | | |

Practical reading of the gear table for a simulator: the only *spell-damage* item in the whole
13–40 window that I could verify is **Robe of the Magi (+22)** at 35. Everything else is Int/Spirit,
which in 1.12 buys mana pool and regen (Int ≈ 15 mana per point pre-talents) and crit, not per-cast
damage. So a per-level damage model can be driven almost entirely by rank + wand + Int-derived mana.

---

## 4. Where damage per mana / per cast actually jumps

Rank-ups that change the model (everything else is a flat ~8–10 % step):

1. **Level 20 — the first real package.** Frostbolt R4 (78–86, 2.6 s) plus Blizzard R1 and
   Polymorph R2 in the same level. Frostbolt's dmg/mana goes 1.14 → 1.26 and its cast DPS 26 → 31.5.
2. **Level 24 — Fireball R5 and Arcane Missiles R3.** Fireball R5 is the *first* rank at the full
   3.5 s cast (R3 2.5 s, R4 3.0 s), i.e. the first rank with the full spell-power coefficient, and
   per-cast damage jumps 117 → 190 (+62 %). Arcane Missiles R3 switches to the 5-missile channel
   (290 total, 1.23 dmg/mana). Counterspell also unlocks here (30 s, 10 s school lock).
   Gear jumps the same level (Robes/Belt of Arugal, Rod of the Sleepwalker).
3. **Level 30 — the biggest single-rank damage jump in the window.** Fireball R6 adds +78 per cast
   (+41 %) and 1.53 dmg/mana is only reached again at 36. Arcane Explosion R3 (101–109) triples
   its AoE-per-cast over R1. Wand tier also steps to 24.2 dps at 30.
4. **Level 32 — Frostbolt R6 + Arcane Missiles R4 + Nightsky Robe** land together; Frostbolt
   reaches 62.7 cast DPS and 1.45 dmg/mana, Arcane Missiles 85 DPS.
5. **Level 36–38 — the top of the window.** Fireball R7 at 36 (96 cast DPS, 1.53 dmg/mana);
   Frostbolt R7 and Arcane Explosion R4 at 38 (81.7 DPS, 1.53 dmg/mana, and instant 149-damage AoE).
   Wand also steps twice (29.7 → 30.4 dps).
6. **Level 40 — Arcane Missiles R5** hits 117 DPS per channel (the highest single-target DPS
   number available in this level range) and Polymorph R3 gives 40 s of control.

Structural observations worth encoding:
- **Frostbolt ≥ Fireball on damage per mana at every tier** (1.14/1.12, 1.26/1.23, 1.38/1.36,
  1.45/1.45, 1.53/1.53) but Fireball wins on damage per cast at 24+ and adds a DoT that keeps
  ticking while the mage moves. Equal at 32–36, so the choice is AoE/DoT vs snare, not efficiency.
- **Arcane Explosion is never mana-efficient** (0.48–0.60 dmg/mana per target); it pays only with
  ≥3 targets stacked, where its instant cast also beats every cast-time spell's DPS.
- **Blizzard is worse than Arcane Explosion per mana at every level** (0.63 vs 0.48 at 20 is the
  only point where Blizzard wins; from 30 on AE > Blizzard per target) — Blizzard is a
  slow/area-control tool at these levels, not a damage tool.
- **The wand is a real DPS contributor to ~level 22** and a filler afterwards (17–22 DPS vs 39–96
  spell DPS). Model it as a between-casts filler with 1.3–1.9 s swings, not as a rotation.

---

## Contradictions

- **1.12 vs Classic Era low-rank damage.** classicdb (1.12): Fireball R1 14–23 + 2 over 4 s;
  Frostbolt R1 18–21; Arcane Explosion R1 32–37; Arcane Missiles R1 24/s. Wowcalc (1.15.9):
  16–24, 20–22, 34–38, 25/s. Same ranks, same levels, different numbers — Classic Era's client
  tuning, not a data error. Both sets recorded above; the tables use 1.15 values.
- **Fireball R1 top end.** Three tabulations give 14–22, 14–23 (classicdb) and 16–24/16–25
  (wowhead 1.15, wowcalc). Unresolved; irrelevant to 13–40 gameplay but flagged.
- **Gravestone Scepter required level**: classicdb prints "Level 29" in Quick Facts while every
  other weapon page prints "Requires Level N". Its actual requirement is probably 26. UNVERIFIED.

## Missing evidence

- Per-rank values for ranks 3+ were not cross-checked against a second independent 1.12 source;
  only the rank levels were confirmed twice (wowcalc rank table + classicdb).
- Spell-power coefficients per rank (the reason R5 at 24 matters) are well known in the community
  (~0.7935 for 3.5 s Fireball/Frostbolt) but I did **not** verify them from a primary source here
  → treat coefficient claims as researcher knowledge, not evidence.
- Blizzard per-tick split (25/45/66 per second) is inferred from the 8 s total, not printed.
- Gear: several slots per band (legs for 18–22, hands/feet for 23–27, head/hands for 28–32,
  head/hands/legs/feet for 33–37) are UNVERIFIED — classicdb search pages for those names render
  client-side and repeatedly failed to extract. The 13–40 gear table is a minimum viable set, not
  an exhaustive in-band BiS list.
- Dungeon/quest provenance of most items (Deadmines, Shadowfang Keep, Wailing Caverns, Uldaman,
  Scarlet Monastery) is stated from item knowledge, not from the fetched pages; classicdb's
  "source" field did not extract for these items.

## Sources

- Kept: Mage spells and abilities — WoW Classic 1.12 / 1.15.9, full rank tables
  (https://wowcalc.net/en/classic/wiki/spells/mage) — the single source for every mana cost, cast
  time and damage value in section 1; explicitly documents client build 1.15.9.70003.
- Kept: classicdb.ch spell pages (https://classicdb.ch/?spell=133, ?spell=116, ?spell=5143,
  ?spell=1449) — 1.12 cross-check of rank 1 values and of the DoT/snare structure.
- Kept: classicdb.ch item pages (individual URLs listed per row above, e.g.
  https://classicdb.ch/?search=Robes+of+Arugal, ?search=Whitemane%27s+Chapeau,
  ?search=Illusionary+Rod, ?search=Robe+of+the+Magi, ?search=Necromancer+Leggings,
  ?search=Emberstone+Staff, ?search=Belt+of+Arugal, ?search=Cookie%27s+Stirring+Rod,
  ?search=Kodo+Brander, ?item=11288) — armour/stats/required level.
- Kept: Wowhead Classic spell pages (https://www.wowhead.com/classic/spell=133/fireball,
  ?spell=116/frostbolt) — effect-level detail (40 % snare, 4 s DoT duration, GCD), used only to
  confirm structure, since Wowhead renders only one rank per URL.
- Rejected: guildorder.com spellbook and various "forever/1.15.9 private server" mirrors — rank
  tables matched wowcalc but several are per-server reimplementations; redundant once wowcalc was
  fetched.
- Rejected: warcraftdb.com list pages (wand/cloth) — client-side rendered, no data extractable.

## Next steps

- Pull a second 1.12 tabulation for ranks 3+ (e.g. a 1.12 DBC dump or wowclassicdb) to settle the
  Classic-Era vs 1.12 delta on every rank, not just rank 1.
- Fill the gear gaps by ID-addressable item lookups (classicdb `?item=<id>` works reliably where
  `?search=` fails) for the UNVERIFIED slots.
