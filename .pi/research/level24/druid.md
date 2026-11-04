# Research: Level 24 Druid Healer — real WoW Classic (1.12 / Era) numbers

Every number below carries a URL. Anything I could not verify against a fetched source is marked
**UNVERIFIED** — nothing is filled in from memory.

Tooling note: `web_search` + `fetch_content` were available and used. Primary numeric sources used:
`vanilla-twinhead.twinstar.cz` (1.12 spell/item DB, exact 1.12 tooltips), `classicdb.ch` (1.12 item DB),
`vanilla-wow-archive.fandom.com` (2005-era wiki pages: base mana table, attribute rules),
`classic-wow-archive.fandom.com` (2007-era wiki: rank→level tables, starting stats),
`warcraft.wiki.gg` (modern wiki, used only where it still carries Classic tooltips).

---

## 1. Race and base class+race stats at level 24 (BEFORE gear)

**Race used: Night Elf** (Alliance-only druid race, and the only NE option; Tauren is the Horde
alternative). Source: https://classic-wow-archive.fandom.com/wiki/Druid

**Level-24 base attributes: UNVERIFIED.** I could not find any fetched primary source giving a
per-level attribute table for 1.12/1.15 (the modern `warcraft.wiki.gg/wiki/Base_attributes` tables are
retail values and do **not** match vanilla; the 2005-era wiki table is a truncated stub that stops at
level 11). Do **not** put invented numbers in the balance file for this field.

What *is* verified, and is what you actually need to compute level-24 values:

| Fact | Value | Source |
|---|---|---|
| NE druid level-1 attributes (wiki A) | Str 17 / Agi 24 / Sta 20 / Int 22 / Spi 22; Armor 48, Health 54, Mana 100 | https://vanilla-wow-archive.fandom.com/wiki/Druid |
| NE druid level-1 attributes (wiki B) | Str 18 / Agi 25 / Sta 19 / Int 22 / Spi 22; Armor 55, Health 53, Mana 100 | https://classic-wow-archive.fandom.com/wiki/Druid |
| Druid **base mana at level 24** (class-only, race-independent) | **449** | https://vanilla-wow-archive.fandom.com/wiki/Base_mana |
| Mana from Intellect | `min(20,Int) + 15*(Int - min(20,Int))` → at Int ≥ 20: `20 + 15*(Int-20)` | https://vanilla-wow-archive.fandom.com/wiki/Base_mana |
| Health from Stamina | 10 HP per point; the first 20 points give 1 HP each | https://vanilla-wow-archive.fandom.com/wiki/Attributes |
| Armor from Agility | 2 armor per Agility (all classes) | https://vanilla-wow-archive.fandom.com/wiki/Attributes |

Sanity check of the two level-1 rows against the mana formula (direct evidence):
`449` is level 24; at level 1 base mana is `50`, and `50 + 20 + 15*(22-20) = 100` = the level-1 mana
shown on both wiki pages. The formula and the table agree.

**Contradiction recorded:** the two archived wikis disagree on NE druid level-1 Sta/Agi/Str and on
Health/Armor (17/24/20 vs 18/25/19; 48/54 vs 55/53). Both agree on Int 22, Spi 22, Mana 100.
The 2007 archive (wiki B) is the later 1.12-era snapshot; I would use wiki B for level-1 anchors.

**To close this field properly:** a client/emulator dump of `player_levelstats` (race 4, class 11,
level 24) or an in-game `/dump UnitStat("player",1..5)` on a naked level-24 NE druid. I could not reach
a fetchable copy of that table.

---

## 2. Best-in-slot gear at level 24

There is **no authoritative level-24 BiS list** for Classic druids — searches turned up only level-19
and level-29 twink guides (e.g. https://www.warcrafttavern.com/wow-classic/guides/29-twink-druid),
so the set below is **researcher-constructed** (interpretation), with every item's tooltip verified
individually. Slots I could not verify a caster item for are listed as gaps rather than filled.

| Slot | Item | Req lvl | Armor | Stats | Source type |
|---|---|---|---|---|---|
| Head | Azure Silk Hood | 24 | 33 | *(no stats in 1.12)* | BoE crafted/AH — https://classicdb.ch/?item=7048 |
| Shoulder | Silver-thread Amice | 24 | 32 | +7 Int, +2 Sta | BoE AH — https://classicdb.ch/?item=6395 |
| Back | Watcher's Cape | 19 | 19 | *(random suffix only; suffix values UNVERIFIED)* | BoE AH — https://classicdb.ch/?item=14179 |
| Chest | Robes of Arugal | 24 | 46 | +3 Agi, +9 Int, +5 Sta, +10 Spi | Shadowfang Keep, Archmage Arugal (BoP) — https://wowclassicdatabase.com/item/robes-of-arugal |
| Wrist | Silver-thread Cuffs | 22 | 18 | +5 Int, +2 Sta | BoE AH — https://classicdb.ch/?item=4036 |
| Hands | **UNVERIFIED / gap** | — | — | — | no verified caster glove ≤ 24 found |
| Waist | Belt of Arugal | 24 | 26 | +2 Agi, +10 Int, +3 Spi | Shadowfang Keep (BoP) — https://wowclassicdatabase.com/item/belt-of-arugal |
| Legs | Darkweave Breeches | 17 | 35 | +7 Int, +6 Spi, +4 Sta | BoE AH — https://classicdb.ch/?item=12987 |
| Feet | Spidersilk Boots | 20 | 29 | +4 Int, +4 Sta, +7 Spi | BoE crafted/AH — https://classicdb.ch/?item=4320 |
| Finger | Lavishly Jeweled Ring | 17 | — | +2 Agi, +6 Int (Unique) | BoP (drop/quest) — https://classicdb.ch/?item=1156 |
| Neck | **UNVERIFIED / gap** | — | — | — | — |
| Main hand | Staff of the Blessed Seer (2H) | 18 | — | +2 Sta, +3 Spi, Equip: +24 healing | BoE world drop — https://classicdb.ch/?item=2271 |

Evidence-quality note: the two Arugal tooltips come from `wowclassicdatabase.com` search/page text
(their pages are JS-rendered and would not extract cleanly), the rest were fetched directly from
`classicdb.ch` tooltips.

---

## 3. TOTALS at level 24 with that gear

Because base attributes at 24 are UNVERIFIED, I give the **gear half exactly** and the **base half as
formulas**, not invented numbers.

**Gear-only totals (direct evidence, summed from §2):**

| Stat | Gear total | Breakdown |
|---|---|---|
| Strength | **0** | — |
| Agility | **7** | 3 (Robes) + 2 (Belt) + 2 (Ring) |
| Stamina | **19** | 2+5+2+4+4+2 |
| Intellect | **48** | 7+9+5+10+7+4+6 |
| Spirit | **29** | 10+3+6+7+3 |

**Totals at 24:** `total = base(24) + gear`, i.e. `Str = base`, `Agi = base+7`, `Sta = base+19`,
`Int = base+48`, `Spi = base+29`.

Derived, formula-based consequences of the gear (arithmetic from §1, interpretation):
- **+720 mana** from the +48 Intellect (`15 × 48`, valid for Int > 20).
- **+190 health** from the +19 Stamina (`10 × 19`, valid above 20 Sta).
- **+14 armor** from the +7 Agility (`2 × 7`).

---

## 4. Armour from the gear, and form bonuses

- **Armour from items: 238** = 33 (hood) + 32 (amice) + 19 (cape) + 46 (robes) + 18 (cuffs) + 26 (belt)
  + 35 (legs) + 29 (boots). (Direct sum of §2 tooltips.)
- Plus `2 × Agility` from the Agility stat (rule: https://vanilla-wow-archive.fandom.com/wiki/Attributes).
- Cloak is the one item with a random suffix, so 238 is the floor for the listed set.

**Bear Form (level 10, the form usable at 24)** — 1.12 tooltip, direct evidence
(https://vanilla-twinhead.twinstar.cz/?spell=5487):
`Shapeshift into a bear, increasing melee attack power by 30, armor contribution from items by 180%,
and health by 20.` (55% of base mana, instant.)
→ Bear armour from items = `238 × 2.8 = 666.4` ≈ **666** armour from the items above (researcher
arithmetic from the verified tooltip); **+30 attack power**, **+20 health**.

**Cat Form (level 20)** — 1.12 tooltip, direct evidence
(https://vanilla-twinhead.twinstar.cz/?spell=768): `increasing melee attack power by 40 plus Agility` →
**no armour bonus in Cat Form**.

Caveat (interpretation): the Bear tooltip says "armor contribution from items", so whether
agility-derived armour is also multiplied by 2.8 is not stated by the source; treat 666 as item-armour
only and add agility armour unmultiplied if you need a single number.

---

## 5. Health, mana pool, and mana regen per 5s

**Mana pool:** `449 (druid base mana at 24) + 20 + 15*(Int_total − 20)` — direct formula + table value:
https://vanilla-wow-archive.fandom.com/wiki/Base_mana. With the §2 gear that is
`449 + 20 + 15*(Int_layer24 + 48 − 20)`; because `Int_layer24` is UNVERIFIED the absolute pool is
UNVERIFIED. The gear's contribution alone is +720 mana.
(Cross-check that the formula is vanilla-correct: level 1 → 50 base + 50 from Int 22 = 100 mana, which
matches both archived Druid pages.)

**Health pool:** `base_health(24) + min(20,Sta) + 10*(Sta−20)` (rules from
https://vanilla-wow-archive.fandom.com/wiki/Attributes). `base_health(24)` is **UNVERIFIED** — the
archived page only publishes level-80 values. Gear contributes +190 health.

**Mana regen per 5 s from Spirit at level 24: UNVERIFIED for 1.12/1.15.**
- The only complete formula I could source is the post-2.4 uniform one:
  `5 × (0.001 + Spirit × sqrt(Int) × BASE_REGEN[level])` with `BASE_REGEN[24] = 0.010990`
  (https://warcraft.wiki.gg/wiki/Mana_regeneration).
- That page and the wiki Spirit page both state this uniform system arrived **in Patch 2.4**, i.e. it is
  not the 1.12 formula (https://wowwiki-archive.fandom.com/wiki/Spirit). Using it for a 1.12/1.15
  balance file would be wrong, so I am not reporting an mp5 figure as fact.
- Also verified (context you can trust): spirit regen is interrupted for 5 s after any mana spend
  (five-second rule), and MP5 items keep ticking regardless —
  https://wowwiki-archive.fandom.com/wiki/Spirit

---

## 6. Weapon

**BiS main-hand at 24: Staff of the Blessed Seer** (two-hand staff, so **no off-hand**) — fetched 1.12 tooltip:
https://classicdb.ch/?item=2271

| Field | Value |
|---|---|
| Requires level | **18** |
| Damage | **47 – 71** |
| Speed | **3.00** |
| DPS | **19.7** (as printed) |
| Stats | +3 Spirit, +2 Stamina |
| Equip | Increases healing done by spells and effects by up to **24** |
| Bind | Binds when equipped (world drop, AH-able) |

It is the only level-≤24 weapon I verified that carries a flat +healing affix (`+24`), and druids are
weapon-stat carriers, not weapon-DPS users in caster form — hence BiS-on-healing here (interpretation).
Druid weapon proficiencies (staves, 1H/2H maces, daggers, fist): https://classic-wow-archive.fandom.com/wiki/Druid

---

## 7. Real level-24 tooltip values

All from `vanilla-twinhead.twinstar.cz` (1.12 spell DB). Rank→level availability cross-checked against
https://classic-wow-archive.fandom.com/wiki/Druid and https://wow4ever.quest/en/spellbook/druid.

| Spell | Highest rank at 24 | Mana | Cast | Effect (1.12 tooltip) | Source |
|---|---|---|---|---|---|
| Healing Touch | **Rank 4** (learned 20; rank 5 at 26) | 185 | 3.0 s | Heals 363–446 | https://vanilla-twinhead.twinstar.cz/?spell=5188 |
| Rejuvenation | **rank at lvl 22** (next at 28) | 135 | Instant | Heals 244 over 12 s (61 per 3 s tick) | https://vanilla-twinhead.twinstar.cz/?spell=2091 |
| Regrowth | **Rank 3** (learned **24**; rank 4 at 30) | 280 | 2.0 s | Heals 240–275 + 259 over 21 s (37 per 3 s) | https://vanilla-twinhead.twinstar.cz/?spell=8939 |
| Entangling Roots | **Rank 2** (learned 18; rank 3 at 28) | 65 | 1.5 s | Roots 15 s, 50 Nature damage over 15 s (10 per 3 s), outdoor only | https://vanilla-twinhead.twinstar.cz/?spell=1062 |
| Wrath | **Rank 4** (learned 22; rank 5 at 30) | 70 | 2.0 s | 63–74 Nature damage, 30 yd | https://vanilla-twinhead.twinstar.cz/?spell=5179 |
| Moonfire | **Rank 4** (learned 22; rank 5 at 28) | 105 | Instant | 40–49 Arcane + 80 Arcane over 12 s (20 per 3 s), 30 yd | https://vanilla-twinhead.twinstar.cz/?spell=8926 |

Ranks explicitly **NOT** available at 24: Healing Touch 5 (26), Rejuvenation next rank (28), Regrowth 4
(30), Entangling Roots 3 (28), Wrath 5 (30), Moonfire 5 (28).
Also not available: Remove Curse (24 — actually learned *at* 24, available), Abolish Poison (26),
Innervate (40), Nature's Swiftness (talents, 31 points), Starfire rank 1 (20, available but off-role),
Travel Form (30). Rank-level table: https://classic-wow-archive.fandom.com/wiki/Druid

Rank-label caveat: twinhead numbers ranks with the level-1 rank counted, so its "Rank 5" Rejuvenation is
the 5th rank overall and the one learned at 22; the 2007-era wiki lists Rejuvenation ranks starting at
level 4 (10 ranks). Same spell, different counting — the **required level** is the reliable key.

---

## 8. Total +healing / +spell damage from BiS gear at 24

- **+24 healing** (Staff of the Blessed Seer, the only such affix in the verified set —
  https://classicdb.ch/?item=2271).
- **+0 spell damage** on any verified item in §2.
- Caveat: the cloak slot takes a random suffix; an "of Healing"/"of the Owl" suffix would add a small
  amount, and its exact value at the 19–24 item-level band is **UNVERIFIED**.

---

## Contradictions

1. **NE druid level-1 stats**: 17/24/20/22/22 with 48 armor & 54 health
   (https://vanilla-wow-archive.fandom.com/wiki/Druid) vs 18/25/19/22/22 with 55 armor & 53 health
   (https://classic-wow-archive.fandom.com/wiki/Druid). Same for Int 22 / Spi 22 / Mana 100. Not resolved;
   prefer the later (1.12-era) archive.
2. **Regrowth rank numbering**: twinhead calls the level-24 rank "Rank 3", while some listings start
   Regrowth at level 1 (spell 8936). Required level is the usable key; the tooltip numbers come from the
   level-24 spell entry either way.

## Missing evidence

- Level-24 **base attributes** (Str/Agi/Sta/Int/Spi) before gear — no fetchable 1.12/1.15 table found.
- Level-24 **base health** (only level-80 values published by the archive I reached).
- Vanilla (pre-2.4) **Spirit→mp5 formula**, so no trustworthy mp5 number.
- Verified **hand** and **neck** items ≤ level 24; verified suffix values for the "of the Owl/Healing"
  green items that would fill those slots.
- Bear Form's multiplier on **agility-derived** armour (tooltip says "from items" only).

## Sources

- Kept: `vanilla-twinhead.twinstar.cz` spell pages (5188, 2091, 8939, 1062, 5179, 8926, 5487, 768) —
  exact 1.12 spell tooltips incl. required level; the only source with verbatim 1.12 numbers.
- Kept: `classicdb.ch` item pages (2271, 6395, 4036, 7048, 12987, 4320, 14179, 1156) — 1.12 item
  tooltips, server-rendered, fetchable.
- Kept: https://vanilla-wow-archive.fandom.com/wiki/Base_mana — level-by-level druid base mana (449 @ 24)
  and the Int→mana formula; cross-checks against level-1 mana.
- Kept: https://vanilla-wow-archive.fandom.com/wiki/Attributes — Sta→HP (first 20 points), Agi→2 armor.
- Kept: https://classic-wow-archive.fandom.com/wiki/Druid — 1.12-ish starting stats and full
  rank→level tables for all six spells.
- Kept: https://wowclassicdatabase.com/item/robes-of-arugal and `.../belt-of-arugal` — the two SFK drops.
- Kept as limited evidence: https://warcraft.wiki.gg/wiki/Mana_regeneration — post-2.4 regen formula +
  `BASE_REGEN[24]`; kept only to show why it must *not* be used for 1.12.
- Rejected: https://warcraft.wiki.gg/wiki/Base_attributes — retail per-level tables that contradict
  vanilla starting stats.
- Rejected: AI-generated stat tables returned by search (e.g. "level 24 = 37/48/40/41/41",
  "= 29/25/29/26/29", "= 27/25/26/27/26") — mutually inconsistent, no primary source, not used.
- Rejected: azerothcore `player_class_stats` — WotLK-era data, wrong expansion.
- Deprioritized: wowhead.com classic pages — JS-rendered, tooltips only partially extractable; snippets
  only used to *discover* items, then verified on classicdb.

## Next steps

1. Get the real level-24 base attributes + base health from a 1.12 `player_levelstats` dump or an in-game
   naked-stat screenshot; that single gap unblocks fields 1, 3, 5 completely.
2. Get the vanilla Spirit→mp5 coefficient (pre-2.4, class-specific) from a 2006-era theorycraft source;
   without it field 5's regen number cannot be sourced.
3. Fill hand/neck slots, or accept "of the Owl" greens and source their suffix values at item level 24–29.

---

## 9. Balance (moonkin) damage at 24

Added when the Druid gained four specs (`b9a2878`). No authoritative
level-24 Balance BiS list exists for Classic Era either — searches surface only level-19
and level-29 twink guides — so this is "best verified", not BiS.

**Finding: at 24 the caster pool is the same pool the healer set already uses.** Cloth
Int/Spirit is what exists at this level, so the armour and the nine non-weapon rows are
unchanged from §2, and the only real decision is the weapon. The totals below are the
§2 rows with a different weapon in slot 10.

| # | Slot | Item | Req lvl | Armour | Sta | Int | Spi | Bind / source |
|---|---|---|---|---|---|---|---|---|
| 1 | head | Azure Silk Hood | 24 | 33 | 0 | 0 | 0 | BoE crafted/AH — https://classicdb.ch/?item=7048 |
| 2 | shoulder | Silver-thread Amice | 24 | 32 | +2 | +7 | 0 | BoE AH — https://classicdb.ch/?item=6395 |
| 3 | back | Watcher's Cape | 19 | 19 | 0 | 0 | 0 | BoE AH (suffix unverified) — https://classicdb.ch/?item=14179 |
| 4 | chest | Robes of Arugal | 24 | 46 | +5 | +9 | +10 | BoP, SFK — Arugal — https://wowclassicdatabase.com/item/robes-of-arugal |
| 5 | wrist | Silver-thread Cuffs | 22 | 18 | +2 | +5 | 0 | BoE AH — https://classicdb.ch/?item=4036 |
| 6 | waist | Belt of Arugal | 24 | 26 | 0 | +10 | +3 | BoP, SFK — https://wowclassicdatabase.com/item/belt-of-arugal |
| 7 | legs | Darkweave Breeches | 17 | 35 | +4 | +7 | +6 | BoE AH — https://classicdb.ch/?item=12987 |
| 8 | feet | Spidersilk Boots | 20 | 29 | +4 | +4 | +7 | BoE crafted/AH — https://classicdb.ch/?item=4320 |
| 9 | ring | Lavishly Jeweled Ring | 17 | 0 | 0 | +6 | 0 | BoP quest/drop — https://classicdb.ch/?item=1156 |
| 10 | weapon | **Emberstone Staff** (2H) | 18 | 0 | +5 | +8 | +5 | BoP, Deadmines — Captain Greenskin — https://classicdb.ch/?item=5201 |

**Totals: armour 238, Str 0, Agi +7, Sta +22, Int +56, Spi +31.**

Modelled consequence of the +56 Int over the healer's +48: the sim derives 15 mana per
Intellect (`Content.fs:3226`), so 840 mana against the healer's 720. Intellect is the
damage stat at this level because it is the mana that casts Wrath and Moonfire; the 1.12
tooltips in §7 confirm damage comes from spell ranks, not from a gear affix.

### Weapon: staff vs dagger + held-in-offhand

Druids use daggers, staves, 1H/2H maces and held-in-off-hand items, but not shields
(https://classic-wow-archive.fandom.com/wiki/Druid), so the pair is legal. Full-set totals
(armour always 238, Agi always 7):

| Option | Req lvl | Sta | Int | Spi | Source |
|---|---|---|---|---|---|
| **Emberstone Staff** (2H) | 18 | 22 | **56** | 31 | https://classicdb.ch/?item=5201 |
| Crescent Staff (2H) | 10 (quest) | **24** | 55 | **33** | https://classicdb.ch/?item=6505 |
| Staff of Westfall (2H) | 14 | 17 | 53 | **37** | https://classicdb.ch/?item=2042 |
| Staff of the Friar (2H) | 19 | 21 | 51 | 37 | https://classicdb.ch/?item=3415 |
| Blackfang + Witch's Finger | 20 / 21 | 21 | 55 | 28 | https://classicdb.ch/?item=2236 , https://classicdb.ch/?item=16887 |
| Blackfang + Orb of Mistmantle | 20 / 23 | 17 | 48 | 32 | https://classicdb.ch/?item=13031 |
| Blackfang + Tear of Grief | 20 / 11 | 17 | 55 | 25 | https://classicdb.ch/?item=5611 |

**Chosen: Emberstone Staff.** No offhand pair can match it, because there is no verified
caster dagger at this level — the best is Blackfang at +2 Spirit — so the dagger path spends
two slots to reach 55 Int / 28 Spi where one staff reaches 56 / 31.

Crescent Staff is three stat points better and is the honest ceiling, but it is a **Horde**
quest reward and this druid is a Night Elf, so it is not available. Recorded rather than
used. Staff of the Blessed Seer is *not* carried into this set: its value is its +24 healing,
which the sim cannot hold (see §8), so for Balance it is the worst weapon on the list.

### Contradictions

1. **Orb of Mistmantle** — 1.12 tooltip is +4 Spirit only (classicdb); wowhead Classic lists
   +6 Int/+4 Spi. Modern values are not 1.12. The 1.12 line is used.
2. **Staff of Westfall required level** — classicdb prints none; Wowhead/JudgeHype say 14.
   Both are ≤ 24, so immaterial.
3. **Witch's Finger Intellect** — 1.12 is +7; a custom-server client lists 8. +7 is used.
4. **Crescent Staff / Tear of Grief level gates** — classicdb prints no Requires Level; the
   gates are the quests (req 10 and req 11).
5. **"of the Eagle" / "of the Owl" greens** — no verified suffix magnitude at item level
   24–29, so a random-suffix item cannot be shown to beat a fixed-stat set.

### Missing evidence

- **Any dagger ≤ 24 with Intellect on the 1.12 tooltip.** None found. This is what decides
  the weapon comparison above.
- **Suffix values** at item level 24–29 — the same gap §2 records.
- **A head item with stats at 24** — Azure Silk Hood has none in 1.12; no alternative verified.
- **Any +spell damage cloth or leather ≤ 24** — the only such affix found is Orb of
  Mistmantle's +6 *Shadow* damage, useless to Balance, so nothing is carried for an affix the
  sim cannot hold anyway.

### Sources

1.12 tooltips (classicdb.ch items 7048, 6395, 14179, 4036, 12987, 4320, 1156, 5201, 6505,
2042, 3415, 2236, 16887, 13031, 5611; quest 914), plus wowclassicdatabase.com for
robes-of-arugal, belt-of-arugal and emberstone-staff drop details, plus
vanilla-wow-archive.fandom.com for 15 mana/Int and 10 HP/Sta.

---

## 10. Feral (cat) and Bear tank at 24

Same caveat as §9: no authoritative level-24 BiS list exists, so these are "best verified".
Each number was read off a fetched 1.12 page; nothing is from memory.

### Strength and Agility conversion (this is what the stats are worth)

Source-code-derived formulas (https://vanilla-wow-archive.fandom.com/wiki/Formulas:Attack_Power):

```
FORM_CAT:  AP = Level x (mLevelMult + 2.0) + STR x 2.0 + AGI - 20.0
FORM_BEAR: AP = Level x (mLevelMult + 3.0) + STR x 2.0       - 20.0
NORMAL:    AP =                            STR x 2.0         - 20.0
```

Confirmed by tooltips: Cat Form "increasing melee attack power by 40 plus Agility"
(https://classicdb.ch/?spell=768); Bear Form "attack power by 30, armor contribution from
items by 180%, and health by 20" (https://classicdb.ch/?spell=5487).

- **Cat optimises Strength** — twice the AP of Agility — with Agility second (armour at 2 a
  point, plus crit and dodge whose 1.12 coefficients could not be verified).
- **Bear optimises Stamina and item armour.** Agility gives a bear no AP at all, so it buys
  only armour.
- The sim's Druid sheet had `ApPerStrength = 1, ApPerAgility = 0` for the one healer spec,
  which halves Strength and ignores Agility entirely. Four specs need four conversions:
  feral (2, 1), bear (2, 0), and (2, 0) for the two casters, whose caster gear carries no
  Strength either way.

### Feral (cat) damage at 24

| Slot | Item | Req | Armour | Str/Agi/Sta/Int/Spi | Bind & source | Notes |
|---|---|---|---|---|---|---|
| Head | Azure Silk Hood (cloth) | 24 | 33 | 0/0/0/0/0 | BoE crafted/AH — https://classicdb.ch/?item=7048 | no leather head at or below 24 exists (§Missing); pure armour |
| Shoulder | Hillman's Shoulders | 21 | 65 | 0/0/5/0/4 | BoE crafted (LW) — https://classicdb.ch/?item=4251 | highest-armour leather shoulder verified |
| Back | Sentry Cloak | 19 | 21 | 0/4/5/0/0 | BoE world drop — https://classicdb.ch/?item=2059 | cloth back slot |
| Chest | Tunic of Westfall | none on tooltip (ilvl 24) | 92 | 0/11/5/0/0 | BoP quest — https://classicdb.ch/?item=2041 | tooltip carries no Requires Level, so `None` in `gear` |
| Wrist | Forest Leather Bracers | 19 | 37 | 0/5/0/0/0 | BoE world drop — https://classicdb.ch/?item=3202 | |
| Waist | Dark Leather Belt | 20 | 48 | 0/4/4/0/0 | BoE crafted (LW) — https://classicdb.ch/?item=4249 | |
| Legs | Leggings of the Fang | 18 | 79 | 5/9/4/0/0 | BoP, Lord Cobrahn, Wailing Caverns — https://classicdb.ch/?item=10410 | best item in the set |
| Feet | Footpads of the Fang | 18 | 57 | 0/4/4/0/0 | BoP, Lord Serpentis, Wailing Caverns — https://classicdb.ch/?item=10411 | |
| Ring | Thunderbrow Ring | 24 | — | 8/3/0/0/0 | BoE, Unique — https://classicdb.ch/?item=13097 | 19 AP in cat form |
| Weapon | **Smite's Mighty Hammer** (2H mace) | 18 | 0 | 11/4/0/0/0 | BoP, Mr. Smite, Deadmines — https://classicdb.ch/?item=7230 | 55-83 dmg, 3.50 s, 19.7 dps |
| **Totals** | | | **432** | **24 / 44 / 27 / 0 / 4** | | |

**Cat AP from gear: 24x2 + 44 = +92** before Cat Form's flat +40 and the level term.
Agility also adds 88 armour at 2 a point.

Weapon alternatives: **Twisted Chanter's Staff** (55-84, 3.40 s, 20.4 dps, +10 Int +6 Sta,
BoE, req 19, https://classicdb.ch/?item=890) is 0.7 dps and one top-end higher but about
26 AP lower, so the hammer wins without a verified cat damage normalisation.

### Bear tank at 24

| Slot | Item | Req | Armour | Str/Agi/Sta/Int/Spi | Bind & source | Notes |
|---|---|---|---|---|---|---|
| Head | Green Tinted Goggles (cloth) | none (ilvl 30) | 35 | 0/0/8/0/7 | BoE, Engineering recipe — https://classicdb.ch/?item=4385 | only sub-24 head with Stamina; BoE, so bought rather than made, which is why the profession is not modelled |
| Shoulder | Hillman's Shoulders | 21 | 65 | 0/0/5/0/4 | BoE crafted — https://classicdb.ch/?item=4251 | |
| Back | Sentry Cloak | 19 | 21 | 0/4/5/0/0 | BoE — https://classicdb.ch/?item=2059 | |
| Chest | Blackened Defias Armor | 19 | 92 | 4/3/11/0/0 | BoP, Deadmines — https://classicdb.ch/?item=10399 | the Defias 2pc "+10 Armor" is a set bonus and is not modelled |
| Wrist | Glowing Leather Bracers | 23 | 40 | 0/2/5/0/0 | BoE — https://classicdb.ch/?item=2017 | |
| Waist | Dark Leather Belt | 20 | 48 | 0/4/4/0/0 | BoE crafted — https://classicdb.ch/?item=4249 | |
| Legs | Leggings of the Fang | 18 | 79 | 5/9/4/0/0 | BoP, Wailing Caverns — https://classicdb.ch/?item=10410 | no Stamina-heavy leather legs verified; highest armour wins |
| Feet | Footpads of the Fang | 18 | 57 | 0/4/4/0/0 | BoP, Wailing Caverns — https://classicdb.ch/?item=10411 | |
| Ring | Plains Ring | 24 | — | 0/0/8/3/0 | BoE, Unique — https://classicdb.ch/?item=2039 | 8 Sta is the largest verified; the Int is dead weight |
| Weapon | **Twisted Chanter's Staff** (2H) | 19 | 0 | 0/0/6/10/0 | BoE — https://classicdb.ch/?item=890 | 55-84 dmg, 3.40 s, 20.4 dps |
| **Totals** | | | **437** | **9 / 26 / 60 / 13 / 7** | | |

**Bear Form consequences:** item armour x 2.8 (the 180% contribution) = **1223** from 437, and
60 Stamina is about +600 health. Gear AP is only 9x2 = +18, and Agility adds none in Bear
Form, which is why a bear does not pursue it. The sim's flat `Traits.ArmorBonus = 428` was
238 x 1.8 for the healer's set; this set implies **437 x 1.8 = 786**.

### Contradictions

1. Hillman's Shoulders Spirit: +4 (classicdb, wowhead classic spell page) vs +3 (jigma.eu,
   labelled beta data). +4 used.
2. Druid Strength-to-AP: the 1.12 source-code formula gives Str x 2, while
   https://wowpedia.fandom.com/wiki/Attributes_(pre-6.0) says "Druids gain 1 melee attack
   power per point of strength". Both fetched; the 1.12 formula is used.
3. Cowl of the Fang (+4 Agi/+5 Sta/+4 Int/+3 Spi, req 22, leather head) appears on
   Turtle/OctoWow databases as item 81006 and is absent from classicdb's 1.12 list. Treated
   as custom-server data and excluded. If it were real it would replace both head slots.

### Missing evidence

- Level-24 **base attributes** remain unverified (the gap §1 and §Contradictions already
  record); every total above is gear-only.
- **No leather head at or below 24** — the lowest verified leather helm is Hardened Leather
  Helm at req 30 (https://classicdb.ch/?item=8747), so the head slot falls back to cloth.
- Agility-to-crit and Agility-to-dodge coefficients at 24: no verified 1.12 value, so Agility
  is scored as armour plus AP only in both sets.
- Random-suffix greens ("of the Monkey/Tiger/Bear") almost certainly beat some rows, but
  their suffix magnitude at item level 24-29 is unverified (same gap §2 records), so they
  are excluded rather than guessed.
- The `mLevelMult` constant in both form formulas is unverified.
- **Professions and set bonuses cannot be expressed** in `gear`: the Defias 2pc armor and 5pc
  attack power are worth about +23 armour together and are not counted.
- Bear Form's multiplier applies to the "armor contribution from items" only, so 1223 is
  item armour and excludes the Agility term.

### Sources

classicdb.ch items 7048, 4251, 2059, 2041, 3202, 4249, 10410, 10411, 13097, 7230, 890, 10399,
2017, 2039, 4385 and spell pages 5487, 768; plus
https://vanilla-wow-archive.fandom.com/wiki/Formulas:Attack_Power for both form formulas.
Rejected: Turtle/OctoWow item 81006, jigma.eu's Spirit value, and wowpedia's pre-6.0
Str-to-AP line (cited only as a contradiction).
