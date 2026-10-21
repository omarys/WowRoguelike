# Classic (1.12 / Era) Priest & Druid ladders, level 13–40

Scope: ability ranks trained between level 13 and 40 (plus the last rank trained *before* 13 that you
still use), and per-band healer gear. Base stat rows per level are assumed to already exist in the sim;
this document adds **ability ranks** and **gear**.

Spell numbers are read from the 1.12 game data via Wowcalc's Classic spellbook dump (which states it
reads build 1.15.9.70003, i.e. Era values) and spot-checked against classicdb.ch (1.12 database) and
Wowhead Classic. Item numbers are from classicdb.ch (1.12), which also carries the `Requires Level`
field the task asks for.

Conventions in the tables:
- "mana" is the flat spell cost as shown on the tooltip. Spells whose cost is a percentage of base mana
  (Dispel Magic, Cure Disease, Rebirth…) are not in this document.
- Tick counts assume the vanilla 3-second tick grid: `ticks = duration / 3`.
- Nothing in the requested list has a cooldown except where a `CD` column appears; every instant spell
  is on the 1.5 s global cooldown.
- `UNVERIFIED` marks a value I could not confirm from a fetched primary source.

---

## 1. Priest — ability ranks, level 13–40

### 1a. Healing / support

| Spell | Rank | Trained at | Mana | Cast | CD | Effect (per rank) | Notes |
|---|---|---|---|---|---|---|---|
| Lesser Heal | 3 | 10 (max rank) | 75 | 2.5 s | — | 143–165 heal | **No new rank 13–40.** Ranks 1/2 are level 1/4 |
| Heal | 1 | 16 | 155 | 3.0 s | — | 307–353 heal | first "big heal" |
| Heal | 2 | 22 | 205 | 3.0 s | — | 445–507 heal | |
| Heal | 3 | 28 | 255 | 3.0 s | — | 586–662 heal | |
| Heal | 4 | 34 | 305 | 3.0 s | — | 734–826 heal | max rank before 40 |
| Renew | 2 | 14 | 65 | instant | — | 100 over 15 s (5 ticks × 20) | 15 s / 5 ticks for **all** ranks |
| Renew | 3 | 20 | 105 | instant | — | 175 over 15 s (5 × 35) | |
| Renew | 4 | 26 | 140 | instant | — | 245 over 15 s (5 × 49) | |
| Renew | 5 | 32 | 170 | instant | — | 315 over 15 s (5 × 63) | |
| Renew | 6 | 38 | 205 | instant | — | 400 over 15 s (5 × 80) | max rank before 40 |
| Power Word: Fortitude | 1 | 1 | 60 | instant | — | +3 Stamina, 30 min | usable from 13 |
| Power Word: Fortitude | 2 | 12 | 155 | instant | — | +8 Stamina, 30 min | |
| Power Word: Fortitude | 3 | 24 | 400 | instant | — | +20 Stamina, 30 min | |
| Power Word: Fortitude | 4 | 36 | 745 | instant | — | +32 Stamina, 30 min | max rank before 40 |
| Divine Spirit | 1 | 30 | 285 | instant | — | +17 Spirit, 30 min | **talent-gated** (Discipline) |
| Divine Spirit | 2 | 40 | 420 | instant | — | +23 Spirit, 30 min | |

Renewers' note: Renew tick values above are `total / 5` and match the client's per-tick buff tooltip
(e.g. Renew r2 buff = "Healing 20 damage every 3 seconds. 15 seconds remaining", verified on classicdb
`?spell=6074`).

### 1b. Damage (for sim completeness)

| Spell | Rank | Trained at | Mana | Cast | CD | Effect |
|---|---|---|---|---|---|---|
| Smite | 3 | 14 | 60 | 2.5 s | — | 58–66 Holy |
| Smite | 4 | 22 | 95 | 2.5 s | — | 97–111 Holy |
| Smite | 5 | 30 | 140 | 2.5 s | — | 158–178 Holy |
| Smite | 6 | 38 | 185 | 2.5 s | — | 222–250 Holy |
| Shadow Word: Pain | 3 | 18 | 95 | instant | — | 132 Shadow over 18 s (6 × 22) |
| Shadow Word: Pain | 4 | 26 | 155 | instant | — | 234 over 18 s (6 × 39) |
| Shadow Word: Pain | 5 | 34 | 230 | instant | — | 366 over 18 s (6 × 61) |

SW:P is 18 s / 6 ticks at every rank ≥ 3. Smite cast time is 2.5 s from rank 3 onward (ranks 1–2 are
1.5 s / 2.0 s).

---

## 2. Priest — gear by band (cloth)

Rarity of caster itemisation pre-40: there is **no +healing item in these bands** on any item I could
verify, and no spirit/int leather-equivalent for priests beyond cloth. Item bonuses below are exactly
what the 1.12 item records say. Armour values are the item's own armour, not the character's total.

| Band | Item | Slot | Req lvl | Armour | Stats | Source |
|---|---|---|---|---|---|---|
| 13–17 | [Robe of the Moccasin](https://classicdb.ch/?item=6465) | Chest | 17 | 36 | +6 Spi, +2 Sta, +2 Str | Wailing Caverns drop |
| 13–17 | [Darkweave Breeches](https://wowclassicdatabase.com/item/darkweave-breeches) | Legs | 17 | 35 | +7 Int, +6 Spi, +4 Sta | BoE world drop |
| 13–17 | [Lavishly Jeweled Ring](https://classicdb.ch/?item=1156) | Finger | 17 | — | +6 Int, +2 Agi | Deadmines (Admiral Ripsnarl) |
| 13–17 | [Staff of Westfall](https://classicdb.ch/?item=2042) | 2H staff | none listed | — | 49–74 dmg, 3.00 s (20.5 dps), +11 Spi, +5 Int | Alliance quest "The Defias Brotherhood" |
| 18–22 | [Emberstone Staff](https://wowclassicdatabase.com/item/emberstone-staff) | 2H staff | 18 | — | 47–71 dmg, 3.00 s (19.7 dps), +8 Int, +5 Spi, +5 Sta | Deadmines (Captain Greenskin) |
| 18–22 | [Spidersilk Boots](https://classicdb.ch/?item=4320) | Feet | 20 | 29 | +7 Spi, +4 Int, +4 Sta | BoE (Tailoring-crafted) |
| 18–22 | [Crescent Staff](https://classicdb.ch/?item=6505) | 2H staff | none listed | — | 47–71 dmg, 2.90 s (20.3 dps), +7 Int, +7 Spi, +7 Sta | Horde quest, Wailing Caverns |
| 23–27 | [Robes of Arugal](https://classicdb.ch/?item=6324) | Chest | 24 | 46 | +10 Spi, +9 Int, +5 Sta, +3 Agi | Shadowfang Keep (Arugal) |
| 23–27 | [Belt of Arugal](https://classicdb.ch/?item=6392) | Waist | 24 | 26 | +10 Int, +3 Spi, +2 Agi | Shadowfang Keep (Arugal) |
| 28–32 | [Electromagnetic Gigaflux Reactivator](https://classicdb.ch/?item=9492) | Head | 32 | 44 | +15 Int, +12 Spi | Gnomeregan (Mekgineer Thermaplugg) |
| 28–32 | [Mantle of Doan](https://classicdb.ch/?item=7712) | Shoulder | 33 | 38 | +8 Int, +7 Spi | Scarlet Monastery Library (Arcanist Doan) |
| 33–37 | [Illusionary Rod](https://classicdb.ch/?item=7713) | 2H staff | 34 | — | 94–142 dmg, 3.40 s (34.7 dps), +15 Int, +10 Spi, +7 Sta | SM Library (Arcanist Doan) |
| 38–40 | [Whitemane's Chapeau](https://classicdb.ch/?item=7720) | Head | 39 | 52 | +14 Spi, +14 Int, +9 Sta | SM Cathedral (Whitemane) |
| 38–40 | [Triune Amulet](https://classicdb.ch/?item=7722) | Neck | 39 | — | +7 Int, +7 Spi, +7 Sta | SM Cathedral (Whitemane) |

Best healer weapon per band (2H staff, since **no caster off-hand worth equipping exists before ~level
45** — I found no verifiable pre-40 healer off-hand, so main-hand + off-hand is strictly worse here):

| Band | Best 2H staff | Req lvl | Damage / speed | Stats |
|---|---|---|---|---|
| 13–17 | Staff of Westfall | none listed | 49–74 / 3.00 | +11 Spi, +5 Int |
| 18–22 | Emberstone Staff (A) or Crescent Staff (H) | 18 / none | 47–71 / 3.00 or 2.90 | +8 Int +5 Spi +5 Sta / +7 all three |
| 23–27 | keep Emberstone/Crescent | — | — | no better healer staff in this band |
| 28–32 | keep Emberstone/Crescent | — | — | Illusionary Rod arrives at 34 |
| 33–37 | Illusionary Rod | 34 | 94–142 / 3.40 | +15 Int, +10 Spi, +7 Sta |
| 38–40 | Illusionary Rod (still) | 34 | 94–142 / 3.40 | upgrades exist as world drops only — UNVERIFIED |

Wand note (Priest-only, matters because of Wand Specialization): the useful pre-40 wands
(Greater Magic Wand, Dwarven Flamestick, Gravestone Scepter) are outside the requested ability list and
their exact 1.12 values are **UNVERIFIED** here.

---

## 3. Druid — ability ranks, level 13–40

### 3a. Healing / support

| Spell | Rank | Trained at | Mana | Cast | CD | Effect | Notes |
|---|---|---|---|---|---|---|---|
| Healing Touch | 3 | 14 | 110 | 2.5 s | — | 204–252 heal | 1.5 s at r1, 2.0 s at r2 |
| Healing Touch | 4 | 20 | 185 | 3.0 s | — | 376–458 heal | |
| Healing Touch | 5 | 26 | 270 | 3.5 s | — | 589–711 heal | cast time settles at 3.5 s |
| Healing Touch | 6 | 32 | 335 | 3.5 s | — | 762–914 heal | |
| Healing Touch | 7 | 38 | 405 | 3.5 s | — | 958–1142 heal | max rank before 40 |
| Regrowth | 1 | 12 | 120 | 2.0 s | — | 92–106 + 98 over 21 s (7 × 14) | |
| Regrowth | 2 | 18 | 205 | 2.0 s | — | 176–200 + 175 over 21 s (7 × 25) | |
| Regrowth | 3 | 24 | 280 | 2.0 s | — | 255–289 + 259 over 21 s (7 × 37) | |
| Regrowth | 4 | 30 | 350 | 2.0 s | — | 335–377 + 343 over 21 s (7 × 49) | |
| Regrowth | 5 | 36 | 420 | 2.0 s | — | 425–477 + 427 over 21 s (7 × 61) | max rank before 40 |
| Rejuvenation | 1 | 4 | 25 | instant | — | 32 over 12 s (4 × 8) | 12 s / 4 ticks at every rank |
| Rejuvenation | 2 | 10 | 40 | instant | — | 56 over 12 s (4 × 14) | |
| Rejuvenation | 3 | 16 | 75 | instant | — | 116 over 12 s (4 × 29) | |
| Rejuvenation | 4 | 22 | 105 | instant | — | 180 over 12 s (4 × 45) | |
| Rejuvenation | 5 | 28 | 135 | instant | — | 244 over 12 s (4 × 61) | |
| Rejuvenation | 6 | 34 | 160 | instant | — | 304 over 12 s (4 × 76) | |
| Rejuvenation | 7 | 40 | 195 | instant | — | 388 over 12 s (4 × 97) | |
| Mark of the Wild | 2 | 10 | 50 | instant | — | +65 armour, +2 all attributes, 30 min | |
| Mark of the Wild | 3 | 20 | 100 | instant | — | +105 armour, +4 all attributes, 30 min | |
| Mark of the Wild | 4 | 30 | 160 | instant | — | +150 armour, +6 all attributes, **+5 resistances** | first rank with resistances |
| Mark of the Wild | 5 | 40 | 240 | instant | — | +195 armour, +8 all attributes, +10 resistances | |

### 3b. Damage / control

| Spell | Rank | Trained at | Mana | Cast | CD | Effect |
|---|---|---|---|---|---|---|
| Wrath | 3 | 14 | 55 | 2.0 s | — | 48–56 Nature |
| Wrath | 4 | 22 | 70 | 2.0 s | — | 69–79 Nature |
| Wrath | 5 | 30 | 100 | 2.0 s | — | 108–122 Nature |
| Wrath | 6 | 38 | 125 | 2.0 s | — | 148–166 Nature |
| Moonfire | 2 | 10 | 50 | instant | — | 17–21 + 32 over 12 s (4 × 8) |
| Moonfire | 3 | 16 | 75 | instant | — | 30–36 + 52 over 12 s (4 × 13) |
| Moonfire | 4 | 22 | 105 | instant | — | 46–54 + 80 over 12 s (4 × 20) |
| Moonfire | 5 | 28 | 150 | instant | — | 69–81 + 124 over 12 s (4 × 31) |
| Moonfire | 6 | 34 | 190 | instant | — | 91–107 + 164 over 12 s (4 × 41) |
| Moonfire | 7 | 40 | 235 | instant | — | 117–137 + 212 over 12 s (4 × 53) |
| Entangling Roots | 2 | 18 | 65 | 1.5 s | — | 50 Nature over 15 s (5 × 10), roots target |
| Entangling Roots | 3 | 28 | 80 | 1.5 s | — | 90 over 18 s (6 × 15), roots target |
| Entangling Roots | 4 | 38 | 95 | 1.5 s | — | 140 over 21 s (7 × 20), roots target |

**Entangling Roots is outdoors-only in 1.12** ("Only useable outdoors"). Inside a dungeon it cannot be
cast at all — for an indoor 5-man sim this ability is dead weight; treat that flag as data, not flavour.
Moonfire rank 1 has a 9 s duration; ranks 2+ are 12 s. Wrath ranks 1/2 are 1.5 s / 1.7 s cast.

---

## 4. Druid — gear by band (leather)

Reality of pre-40 Druid healer itemisation, which the sim should encode: **named leather caster gear is
almost non-existent below 40.** The only coherent leather caster line is the green *Guardian* set
(Int/Spirit, no Agility); everything else in leather in these bands is Agility/Strength (Fang set,
Nightscape, etc.). Druid healers in practice wear Guardian + cloth caster pieces + "of the Owl"
(Int/Spirit) greens. The WC **Fang** set is a trap for healers: its pieces are Agi/Str, and only the
set bonuses are caster-flavoured (4-piece: +11 healing; 5-piece: +10 Intellect).

| Band | Item | Slot | Req lvl | Armour | Stats | Source |
|---|---|---|---|---|---|---|
| 13–17 | Gloves of the Fang | Hands | 14 | 47 | +3 Agi, +2 Str (set) | Wailing Caverns; set bonuses only |
| 13–17 | Belt of the Fang | Waist | 16 | 45 | +3 Sta, +2 Agi (set) | Wailing Caverns (Lady Anacondra) |
| 13–17 | Robe of the Moccasin | Chest | 17 | 36 (cloth) | +6 Spi, +2 Sta | WC drop — cloth is fine for a healer |
| 13–17 | Staff of Westfall | 2H staff | none listed | — | 49–74 / 3.00, +11 Spi, +5 Int | Alliance quest |
| 18–22 | Armor of the Fang | Chest | 18 | 82 | +7 Spi, +2 Str (set) | Wailing Caverns (Lord Pythas) |
| 18–22 | Leggings of the Fang | Legs | 18 | 79 | +9 Agi, +5 Str, +4 Sta (set) | Wailing Caverns |
| 18–22 | Footpads of the Fang | Feet | 18 | 57 | +4 Sta, +4 Agi (set) | Wailing Caverns |
| 18–22 | Emberstone Staff / Crescent Staff | 2H staff | 18 / none | — | see Priest band table | DM / WC quest |
| 23–27 | Guardian Belt | Waist | 29 | 56 | +7 Int, +6 Spi | BoE world drop |
| 23–27 | Robes of Arugal (cloth) | Chest | 24 | 46 | +10 Spi, +9 Int, +5 Sta | Shadowfang Keep |
| 28–32 | Guardian Armor | Chest | 30 | 102 | +10 Int, +9 Spirit | BoE world drop |
| 28–32 | Guardian Belt (if not worn already) | Waist | 29 | 56 | +7 Int, +6 Spi | BoE world drop |
| 33–37 | Keep Guardian Armor/Belt | — | — | — | Guardian legs/hands/boots also exist at req 27–34 — exact stats **UNVERIFIED** | BoE world drops |
| 33–37 | Illusionary Rod | 2H staff | 34 | — | 94–142 / 3.40, +15 Int, +10 Spi, +7 Sta | SM Library |
| 38–40 | Whitemane's Chapeau (cloth) | Head | 39 | 52 | +14 Spi, +14 Int, +9 Sta | SM Cathedral |
| 38–40 | Triune Amulet | Neck | 39 | — | +7 Int, +7 Spi, +7 Sta | SM Cathedral |

Best healer weapon per band is the **same staff ladder as the Priest** (nothing on the staff upgrades is
class-restricted); the only difference is faction: Horde gets Crescent Staff at ~19, Alliance gets
Staff of Westfall off the Defias chain. No pre-40 caster off-hand verified → main-hand + off-hand is not
a real option for either class in these bands.

---

## 5. What unlocks when (healer-relevant timeline)

| Level | Priest | Druid |
|---|---|---|
| 14 | Renew r2, Smite r3, Psychic Scream r1, Cure Disease | Healing Touch r3, Wrath r3, Thorns r2, Cure Poison |
| 16 | **Heal r1** (first 3 s heal), Resurrection r2 at 22 | Rejuvenation r3, Moonfire r3, Aquatic Form |
| 18 | Shadow Word: Pain r3, Dispel Magic r1, Power Word: Shield r3 | **Regrowth r2**, Entangling Roots r2, Faerie Fire, Hibernate |
| 20 | Renew r3, Flash Heal r1, Inner Fire r2, **Shackle Undead** | Healing Touch r4, Rejuv r4 at 22, **Rebirth r1 (in-combat rez!)** |
| 24 | **PW:Fort r3 (+20 Sta)**, PW:Shield r4 | Regrowth r3, Thorns r3, Remove Curse |
| 26 | Renew r4, SW:P r4, Flash Heal r2 | Healing Touch r5, Abolish Poison |
| 30 | **Divine Spirit r1 (talent)**, Smite r5, Prayer of Healing r1, Inner Fire r3, Fade r3 | **Nature's Swiftness (talent)**, Mark of the Wild r4 (+resist), Regrowth r4, Wrath r5, Tranquility r1 |
| 32 | Renew r5, Flash Heal r3, Abolish Disease | Healing Touch r6 |
| 34 | **Heal r4 (734–826)**, Smite r6 at 38, SW:P r5 | Rejuvenation r6, Moonfire r6 |
| 36 | PW:Fort r4 (+32 Sta), PW:Shield r6, Dispel Magic r2 | Regrowth r5, Frenzied Regeneration |
| 38 | Renew r6, Flash Heal r4, Smite r6 | **Healing Touch r7 (958–1142)**, Entangling Roots r4, Starfire?no |
| 40 | **Greater Heal r1 (924–1038, 370 mana)**, Divine Spirit r2, Prayer of Healing r2, Lightwell r1, Shadowform | **Innervate (free 1-talent-point ability at 40)**, Swiftmend (talent), Rejuvenation r7, Mark of the Wild r5, Hurricane r1, Moonkin/Dire Bear |

Notes for the sim's tuning:
- **Level 40 is the single biggest healer spike for both classes**: Priest gets Greater Heal (a 3 s,
  924–1038 heal for 370 mana — more than double Heal r4's throughput per cast for only 65 more mana) and
  Druid gets Innervate + Rejuvenation r7 + Mark of the Wild r5.
- Priest's efficient single-target heal before 40 is **Heal r4** (734–826 for 305 mana ≈ 2.55 HPM);
  Renew r6 is 400 for 205 (1.95 HPM) but is instant and stacks with casting.
- Druid's efficient heal is **Healing Touch r7** (958–1142 for 405 mana ≈ 2.59 HPM), nearly identical
  efficiency to Priest Heal r4 at the same level, but on a 3.5 s cast.
- Priest HPS/HPM cross-over at 40 is the intended "Greater Heal replaces Heal" moment; below 40 there is
  no Greater Heal, no Flash Heal below 20, and no group heal below 30 (Prayer of Healing r1 at 30).
- Druid has **no group heal worth casting** until Tranquility (30) and no in-combat resurrection until
  Rebirth (20) — the latter is a genuine capability the Priest lacks until much later in a dungeon run.

---

## 6. Contradictions / disputed values

1. **Robes of Arugal required level.** A Wowhead-derived secondary answer said "requires level 20";
   classicdb.ch's 1.12 record says **Requires Level 24** (item level 29). Kept 24 (primary 1.12 data).
2. **Staff of Westfall / Crescent Staff have no `Requires Level` line** in classicdb.ch (both are quest
   rewards gated by the quest, not the item). Do not treat them as req-level-0 available gear; they are
   gated by "The Defias Brotherhood" (final chain, roughly lvl 16–20) and the Horde WC finale.
3. **Electromagnetic Gigaflux Reactivator armour/stats** agree across classicdb.ch, WowClassicDatabase
   and cavernoftime (44 armour, +15 Int, +12 Spi, req 32); the Wowhead *WotLK-Classic* variant differs
   (hit rating) — that is a later-patch item, not 1.12.
4. **Mantle of Doan** is +8 Int / +7 Spi on 1.12 (38 armour); WotLK/TBC variants show +9 Int and spell
   hit. Use the 1.12 numbers.
5. **Emberstone Staff stat totals** are stated identically (+8 Int, +5 Spi, +5 Sta, req 18) by Wowhead
   Classic, WowClassicDatabase and TBC/WotLK caveats — consistent, no conflict.

## 7. Missing evidence (explicit)

- Guardian **Leggings / Gloves / Boots / Bracers / Cloak** exact 1.12 stats: existence and req levels
  27–34 are attested, individual numbers **UNVERIFIED**. Only Guardian Armor (req 30, 102 armour, +10 Int
  +9 Spi) and Guardian Belt (req 29, 56 armour, +7 Int +6 Spi) were pulled directly.
- No verified **+healing** item below level 40 other than Embrace of the Viper's 4-piece bonus (+11).
  A Druid healer therefore has **0 +healing** in the sim until ~40 — plausible but only negatively
  verified.
- Priest **best 38–40 staff** beyond Illusionary Rod (e.g. world-drop staves such as Staff of Jordan):
  names exist, 1.12 stat lines **UNVERIFIED**, so not tabulated.
- Pre-40 **caster off-hand** (for main-hand + off-hand recommendation): none found. This is a negative
  finding, not a positive one.
- Wand values (Greater Magic Wand / Dwarven Flamestick / Gravestone Scepter): **UNVERIFIED**.
- Divine Spirit's exact Discipline talent point cost/tier in 1.12 (it unlocks at level 30): **UNVERIFIED**
  — the level-30 unlocking level itself is confirmed by the rank table.

## 8. Sources

Kept:
- [Priest Spells and Abilities — WoW Classic 1.12](https://wowcalc.net/en/classic/wiki/spells/priest) —
  complete per-rank trainer level / mana / cast / effect table for every requested Priest spell.
- [Druid Spells and Abilities — WoW Classic 1.12](https://wowcalc.net/en/classic/wiki/spells/druid) —
  same for Druid, including the Restoration tree and the outdoor-only flag on Entangling Roots.
- [classicdb.ch](https://classicdb.ch/) (1.12 database) — item `Requires Level`, armour and stat lines;
  used for Robes/Belt of Arugal, Staff of Westfall, Crescent Staff, Lavishly Jeweled Ring, Fang set,
  Whitemane's Chapeau, Illusionary Rod, Triune Amulet, Robe of the Moccasin, Spidersilk Boots, Guardian
  Armor/Belt; also spell 6074 (Renew r2) to cross-check tick structure.
- [Wowhead Classic — Lesser Heal](https://www.wowhead.com/classic/spell=2053/lesser-heal) and
  [Renew](https://www.wowhead.com/classic/spell=25315/renew) — spell-detail cross-check (cast time,
  duration, GCD).
- [WoW Classic Database](https://wowclassicdatabase.com/) — secondary confirmation of Emberstone Staff,
  Crescent Staff, Mantle of Doan, Gigaflux Reactivator, Darkweave Breeches.
- [Wowhead Classic — Mantle of Doan](https://www.wowhead.com/classic/item=7712/mantle-of-doon) /
  [Electromagnetic Gigaflux Reactivator](https://www.wowhead.com/classic/item=9492/electromagnetic-gigaflux-reactivator)
  — 1.12 stat confirmation for the SM/Gnomeregan pieces.

Rejected / deprioritised:
- Wowhead *WotLK/TBC/Retail* item variants — different stat lines, actively misleading for 1.12.
- Basin Wiki "Priest ability" table — very old beta-era numbers (43–53 Lesser Heal r1 vs 47–57 live).
- wowpedia/fandom mirrors — same data as warcraft.wiki.gg with more drift; used only for item IDs.
- Generic "leveling gear" listicles (Icy Veins / Warcraft Tavern / Reddit) — no per-item 1.12 stat
  data, and several mix TBC/Wrath itemisation.

## 9. Next steps

1. Pull Guardian legs/gloves/boots/bracers 1.12 records (classicdb item IDs ~4257–4262) to finish the
   Druid leather line — that is the single biggest gap in the gear tables.
2. Decide the sim's stance on the **outdoor-only flag** for Entangling Roots: data says it cannot be cast
   in a dungeon, so either exclude it indoors or keep it as an explicit rule.
3. If the sim models wands (Priest Wand Specialization), verify Greater Magic Wand / Dwarven Flamestick /
   Gravestone Scepter 1.12 values.
