# Level 24 Holy Paladin (Vanilla 1.12 / Classic Era) — sourced numbers

Web tools ran (web_search + fetch_content). No `source_check` passage validation was available this run (see §8),
so every number is tagged: **D** = a fetched page states it · **I** = researcher arithmetic/inference from cited
formulas · **U** = unknown. Where a 1.12 database and the 1.15.9 client disagree, **both numbers are listed and
flagged** (§7) — that happens for every heal and for Seal of Command's Judgement.
Race assumed: **Human** (paladins are Alliance-only); Dwarf given as alternative in §1.

## 1. Level-24 base stats (naked, no buffs)

| Stat | Human 24 | Dwarf 24 |
|---|---|---|
| Strength | **47** | 49 |
| Agility | **34** | 30 |
| Stamina | **46** | 49 |
| Intellect | **35** | 34 |
| Spirit | **38** | 36 |
| Base health (pre-Stamina) | **247** | 247 |
| Base mana (pre-Intellect) | **525** | 525 |

Vanilla `BaseStats` table (`HUMAN_PALADIN`/`DWARF_PALADIN`, entry 24; level 1 = `(22,20,22,20,22,28,59)`, level-60
human = `(105,65,100,70,78,1381,1512)`), order `new(str, agi, sta, int, spi, hp, mana)` —
https://docs.rs/wow_world_base/latest/src/wow_world_base/extended/vanilla/base_stats.rs.html — **D**. The hp/mana
columns are pre-Stamina/pre-Intellect baselines (level-1 mana 59 + 20 Int ≈ 79, matching the level-1 paladin pool) — **I**.
Derived pools (**I**): **Health = 247 + f(Sta)**, 10 hp per point but "the first 20 points give 1 health each"
(https://wow-guides.com/knowledge/attributes) → f(46) = 20 + 260 = **280 → 527 health naked**.
**Mana = 525 + f(Int)**, `ManaFromInt = min(20,Int) + 15×(Int−min(20,Int))`
(https://wowpedia.fandom.com/wiki/Base_mana) → f(35) = 20 + 225 = **245 → 770 mana naked**. Both are arithmetic,
not a measured character sheet.

## 2. Best-in-slot gear usable at level 24 (level req ≤ 24)

At 24 a paladin wears **mail** (plate is trained at level 40 — "Plate Mail | 40",
https://warcraft.wiki.gg/wiki/Paladin_abilities_(Classic), **D**). Nearly every caster-stat piece in this bracket is
a **random-suffix green** ("of the Eagle" Int/Sta, "of the Owl" Int/Spi, "of Intellect") whose concrete rolls I could
**not** verify for one single item, so those slots are marked U rather than guessed.

| Slot | Item | Req | Armor | Stats / effect | Source | Conf |
|---|---|---|---|---|---|---|
| Head | — none verified ≤24 — | — | — | U (suffix green, ilvl ~29–31) | — | U |
| Neck | — none verified — (River Pride Choker is req **28**) | — | — | U | — | D |
| Shoulder | **Sparkleshell Mantle** (mail) | **24** | **161** | **+10 Sta, +4 Agi** | BoE → AH | D |
| Back | — none verified — | — | — | U (suffix green / Engineer's Cloak ilvl 27) | — | U |
| Chest | **Greenweave Robe** (cloth) usable but statless before roll | **22** | 41 | random suffix, values U | BoE → AH | D |
| Wrist | **Yorgen Bracers** (mail) | **22** | **91** | **+6 Str, +3 Agi, +3 Sta** | BoE world drop | D |
| Hands | **Algae Fists** (mail) | **23** | **132** | **+10 Str, +4 Sta** | Gelihast, Blackfathom Deeps | D |
| Waist | — none verified — | — | — | U | — | U |
| Legs | **Chausses of Westfall** (mail) | no req line (ilvl 24) | **173** | **+5 Sta, +11 Str** | quest *The Defias Brotherhood* | D |
| Feet | — none verified — | — | — | U | — | U |
| Ring 1 | **Lorekeeper's Ring** | **18** | — | **+2 Sta; +5 damage & healing; 2 mana/5 s** | WSG vendor (Illiyana Moonblaze) | D |
| Ring 2 | **Plains Ring** | **24** | — | **+8 Sta, +3 Int** | Rotting Agam'ar, Razorfen Kraul | D |
| Trinket | Insignia of the Alliance | U | — | U | WSG vendor | U |
| Weapon (2H) | **Verigan's Fist** (2H mace, paladin only) | no req line (quest req 20, quest lvl 22) | — | **65–99 dmg, speed 3.20, 25.6 DPS, +6 Int, +7 Sta, +12 Spirit** | quest *The Test of Righteousness* | D |
| Shield (1H setup) | **Arctic Buckler** | no req line (quest lvl 27) | **642** (see §7) | **13 block, +3 Sta, +8 Spirit, +5 Frost res** | quest *Blackfathom Villainy* (BFD) | D (armour disputed) |
| Main hand (1H setup) | — none verified with caster stats ≤24 — | — | — | U | suffix "of the Owl" mace/sword | U |

Fetched item pages: https://classicdb.ch/?item=13131 (161 armor, +10 Sta +4 Agi, req 24) · `?item=13012` (91 armor,
+6 Str/+3 Agi/+3 Sta, req 22) · `?item=6906` (132 armor, +10 Str/+4 Sta, req 23) · `?item=6087` (173 armor, +5 Sta
+11 Str, **no Requires Level line**, ilvl 24) · `?item=20431` (+2 Sta, "+5 damage and healing", 2 mp5, req 18) ·
`?item=2039` (+8 Sta +3 Int, req 24) · `?item=13087` (req **28** → excluded) · `?item=6953` (Verigan's Fist) ·
`?item=7002` (Arctic Buckler) · `?item=14133` (Greenweave Robe, req 19–22) — all **D**.
Verigan's Fist cross-check https://warcraft.wiki.gg/wiki/Verigan%27s_Fist (quest reward, quest lvl 22) — **D**.
**Unusable at 24** (verified req levels): Frostreaver Crown **27** (`?item=13127`), Avenger's Armor **26**
(`?item=1488`), Dreamslayer **28** (`?item=7752`), Turtle Scale pieces **36+**
(https://warcraft.wiki.gg/wiki/Turtle_Scale_collection) — **D**.

## 3. Totals at level 24

**A) Base only (human):** Str 47 / Agi 34 / Sta 46 / Int 35 / Spi 38; health 527; mana 770.

**B) Base + verified set of §2** (shoulders, wrist, hands, legs, both rings, Verigan's Fist):

| Total | Value |
|---|---|
| Strength | **74** (47 + 6 + 10 + 11) |
| Agility | **41** (34 + 4 + 3) |
| Stamina | **85** (46 + 10 + 3 + 4 + 5 + 2 + 8 + 7) |
| Intellect | **44** (35 + 3 + 6) |
| Spirit | **50** (38 + 12) |
| Item armour | **557** (161 + 91 + 132 + 173) |
| Agility armour (2 per Agi) | **82** |
| Armour without aura | **639** |
| Armour with Devotion Aura r3 (+275) | **914** |
| Spell damage/healing from gear | **+5** (Lorekeeper's Ring only) |
| Health | **917** = 247 + (20 + 10×65) — **I** |
| Mana | **905** = 525 + (20 + 15×24) — **I** |

Arithmetic is exact; the set is **five slots short** (head, back, waist, feet, chest roll), so true 24 BiS is higher
on armour/Stamina/Int — **U**. No other +healing/+spell power item at req ≤ 24 was found (**U**, §8).

## 4. Abilities a level-24 Holy paladin actually has

Learn levels gated by https://warcraft.wiki.gg/wiki/Paladin_abilities_(Classic) (Level/Source columns) — **D**;
ranks/costs/effects from https://wowcalc.net/en/classic/wiki/spells/paladin (built from client build
1.15.9.70003) — **D**; tooltip wording + range spot-checked on classicdb.ch 1.12 pages — **D**.

| Ability | Rank at 24 | Learned | Mana | Cast | CD | Range | Effect |
|---|---|---|---|---|---|---|---|
| **Holy Light** | **4** | **22** | **190** | **2.5 s** | — | **40 yd** | **310–357** (1.12) / **322–368** (1.15.9) |
| **Flash of Light** | **1** | **20** | **35** | **1.5 s** | — | **40 yd** | **62–73** (1.12) / **67–77** (1.15.9) |
| **Lay on Hands** | **1** | **10** | **0** (drains all mana) | instant | **1 hr** | **40 yd** | heals = paladin's **max health**; **no** mana restore |
| Redemption | 2 | **24** | 80% base mana | 10 s | — | — | res 150 hp / 260 mana, out of combat |
| **Purify** | 1 | **8** | **8% base mana** | instant | — | **30 yd** | removes 1 disease + 1 poison |
| **Cleanse** | **NOT AVAILABLE** | **42** | — | — | — | — | disease+poison+magic |
| **Judgement** | 1 | **4** | **6% base mana** (≈31 — **I**) | instant | **10 s** | **10 yd** | unleashes the active Seal |
| **Seal of Righteousness** | **3** | **18** | **60** | instant | — | self | Holy dmg per swing scaled by weapon speed; Judgement **53–57** |
| Seal of the Crusader | 3 | **22** | 65 | instant | — | self | +107 AP, 40% faster attacks, less per hit; Judgement +50 Holy taken |
| Seal of Justice | 1 | **22** | 13% base mana | instant | — | self | chance to stun 2 s; Judgement stops fleeing |
| **Seal of Command** | 1 — **TALENT (Ret, 10 pts)** | **20** | 65 | instant | — | self | 70% of weapon dmg as Holy on proc; Judgement **46.5–55.5** (1.12) / **68.5** (1.15.9); 93–102 / 137–145 if stunned |
| **Seal of Light** | **NOT AVAILABLE** | **30** | — | — | — | — | self-heal on hit |
| **Seal of Wisdom** | **NOT AVAILABLE** | **38** | — | — | — | — | mana back on hit |
| **Consecration** | 1 — **TALENT (Holy, 11 pts)** | **20** | **135** | instant | **8 s** | self, **8 yd radius** | **64 Holy over 8 s** (8 per 1 s tick) |
| Exorcism | 1 | **20** | 85 | instant | 15 s | U | **90–102** Holy, undead/demon only |
| Turn Undead | 1 | **24** | 35 | 1.5 s | 30 s | U | undead flees up to **10 s**, breaks on damage |
| **Hammer of Justice** | **2** | **24** | **50** | instant | **1 min** | **10 yd** | **stun 4 s** |
| **Divine Protection** | **2** | **18** | **35** | instant | **5 min** | self | **8 s** phys+spell immunity; **cannot act**; 1 min forbearance |
| Blessing of Protection | 2 | **24** | 7% base mana | instant | 5 min | U | 8 s physical immunity on ally |
| Blessing of Freedom | 1 | 18 | 10% base mana | instant | 20 s | U | 10 s movement-impair immunity |
| Righteous Fury | 1 | 16 | 30% base mana | instant | — | self | +60% Holy threat, 30 min |
| **Blessing of Might** | **3** | **22** | **45** | instant | — | **30 yd** | **+55 melee AP**, 5 min |
| **Blessing of Wisdom** | **2** | **24** | **45** | instant | — | 30 yd (U for this rank) | **15 mana per 5 s**, 5 min |
| Blessing of Kings | **TALENT (Protection)** | 20 | 8% base mana | instant | — | U | +10% all stats, 5 min |
| **Divine Shield** | **NOT AVAILABLE** | **34** | — | — | — | — | 10 s full immunity |
| **Holy Shock** | **NOT AVAILABLE — TALENT, 40** | **40** | — | — | — | — | 204–220 dmg/heal at r1 |
| **Sanctity Aura** | **NOT AVAILABLE — TALENT, 30** | 30 | — | — | — | — | +10% Holy damage |
| Blessing of Salvation / Sanctuary / Divine Favor / Hammer of Wrath / Blessing of Light | **NOT AVAILABLE** | 26 / 30 / 30 / 44 / 40 | — | — | — | — | — |

Ability sources: Holy Light r4 "190 mana / 40 yd / 2.5 sec cast / Heals … 310 to 357", Level 22 —
https://classicdb.ch/?spell=1026 · Flash of Light r1 "35 mana / 40 yd / 1.5 sec / Heals … 62 to 73", Level 20 —
https://classicdb.ch/?spell=19750 · Lay on Hands r1 "40 yd / heals … equal to maximum health, drains all remaining
mana", Level 10 — https://classicdb.ch/?spell=633 · Judgement "6% of base mana / 10 yd / 10 sec cooldown", Level 4 —
https://classicdb.ch/?spell=20271 · Hammer of Justice r2 "50 mana / 10 yd / Stuns … 4 sec", Level 24 —
https://classicdb.ch/?spell=5588 · Consecration r1 "135 mana / 64 Holy damage over 8 sec", Level 20, self, radius
8 yd, 8 dmg per 1 s tick — https://classicdb.ch/?spell=26573 · Seal of Command r1 "65 mana … 70% of normal weapon
damage … Judgement 46.5 to 55.5, 93 to 102 if stunned", Level 20 — https://classicdb.ch/?spell=20375 · Blessing of
Might r3 "45 mana / 30 yd / +55 melee attack power / Level 22" — https://classicdb.ch/?spell=19835 · Purify
"8% of base mana / 30 yd", Level 8 — https://classicdb.ch/?spell=1152 — all **D**.
Seal of Command talent cost: "a talent in the Retribution tree that required 10 talent points invested" —
https://wowpedia.fandom.com/wiki/Seal_of_Command — **D** (wiki). Consecration talent cost "11 pts in the holy tree" —
https://www.reddit.com/r/classicwow/comments/d5hahp/confused_about_consecration_for_paladins/ — **D**, forum-level,
medium confidence; it is a **trainer** spell only in TBC+ (BC table on the wiki page lists it as Trainer) — **D**.
Talents at 24: **15 points** available (from level 10) — **I**; reachable = Consecration (11 in Holy), Blessing of
Kings, Seal of Command (10 in Ret); unreachable = Sanctity Aura (30), Divine Favor (30), Sanctuary (30), Holy Shock (40).
**Key consequence for the sim: at 24 there is no mana-return Seal and no Cleanse** — mana sustain is Spirit regen,
Blessing of Wisdom (15/5 s) and potions only — **I** (follows directly from the 30/38/42 gates).

## 5. Auras (only ONE active; 30-yd party aura, no mana cost, instant)

| Aura | Rank at 24 | Learned | Numeric effect |
|---|---|---|---|
| **Devotion Aura** | **3** | **20** | **+275 armour** to party in 30 yd (rank table 55 / 160 / **275** / 390 at 30) |
| **Retribution Aura** | **1** | **16** | **5 Holy damage** to any creature striking a party member (r2 = 8 at 26) |
| **Concentration Aura** | 1 | **22** | **35% chance to ignore spell interruption** when damaged |
| **Sanctity Aura** | **NOT AVAILABLE — Ret talent, 30** | 30 | +10% Holy damage done by party in 30 yd |

Devotion rank table — https://wowcalc.net/en/classic/wiki/spells/paladin — **D**; classicdb confirms L30 rank 4
"Gives 390 additional armor … within 30 yards" (effect: Mod Resistance 390, radius 30 yd) —
https://classicdb.ch/?spell=10291 — **D**. Retribution r1 = 5 and Concentration = 35% — wowcalc — **D**. Sanctity
Aura = Tier 5 Retribution talent, level 30, "+10% Holy damage … within 30 yards" —
https://warcraft.wiki.gg/wiki/Sanctity_Aura — **D**. So the real level-24 decision is **armour (Devotion)** vs
**5 damage per incoming hit (Retribution)** vs **35% pushback immunity (Concentration)** — **I**.

## 6. Rotation a level-24 Holy paladin actually runs

Ability facts **D** (§4); the ordering/efficiency is **I**.
1. **Seal of Righteousness r3** (60 mana) kept up; **Seal of Command r1** (65 mana) only if Ret-talented.
2. **Judgement** on cooldown when mana allows (6% base mana, 10 s CD, 10 yd): 53–57 Holy via SoR, 46.5–55.5 via SoC.
3. **Flash of Light r1** (35 mana, 1.5 s, 62–73 → 1.77–2.09 healing/mana) is the efficient heal; **Holy Light r4**
   (190 mana, 2.5 s, 310–357 → 1.63–1.88 healing/mana) only for throughput.
4. **Hammer of Justice r2** (50 mana, 60 s CD, 4 s stun, 10 yd) = the interrupt/peel.
5. **Divine Protection r2** (35 mana, 5 min CD, 8 s) to survive — you cannot act while it is up.
6. **Blessing of Might r3** (+55 AP) or **Blessing of Wisdom r2** (15 mana/5 s); one Blessing per paladin per target.
7. **Aura:** Devotion melee default, Concentration while hard-casting under pressure, Retribution when taking constant hits.
8. Ret-talented only: **Consecration r1** (135 mana, 8 s CD, 64 damage over 8 s) — poor value at this level.
9. Emergencies: **Lay on Hands r1** (free, 1 hr CD), **Blessing of Protection r2**, **Blessing of Freedom**, **Purify**.

## 7. Contradictions / disputes (recorded, not resolved)

1. **1.12 database vs 1.15.9 client disagree on every paladin heal and on Seal of Command's Judgement.**
   Holy Light r4 **310–357** (classicdb, https://classicdb.ch/?spell=1026) vs **322–368** (wowcalc). Flash of Light r1
   **62–73** (classicdb) vs **67–77** (wowcalc). Seal of Command r1 Judgement **46.5–55.5 / 93–102 stunned**
   (classicdb) vs **68.5 / 137–145** (wowcalc). The pattern is systematic, so it looks like a Classic-Era tuning pass
   rather than a typo — **I** (cause unverified). Pick one edition and stay in it.
2. **Arctic Buckler armour: 642 / 13 block** (classicdb `?item=7002`, wowclassicdatabase) vs **87 armour** (+3 Sta,
   +7 Spirit, +4 Frost Res) on https://warcraft.wiki.gg/wiki/Arctic_Buckler. A 555-armour swing on a level-24
   character — the largest single uncertainty in §3. Unresolved.
3. **Level-60 rating tables do not transfer** to 24, and warcraft.wiki.gg/wiki/Base_attributes is the **retail**
   table (DK/Monk columns) — rejected. No level-24 paladin crit / spell-crit conversion source found (U).
4. **Sanctity Aura**: wowcalc "Level 30" vs wiki "Tier 5 Retribution talent" — same practical outcome (unavailable
   at 24), exact point cost unverified.
5. **Consecration's 11-point cost** rests on a Reddit thread + a Turtle-WoW forum post, not a database page.

## 8. Missing evidence

- **Head, back, waist, feet, chest roll, and a 1H caster weapon**: no ≤24 item with fixed caster stats verified.
  Those slots are suffix greens; I verified no concrete roll, so their contribution to §3 is **omitted, not zero**.
- **Health/mana at 24 are formula-derived**; no fetched source publishes a level-24 paladin's actual pools. If the
  low-level Stamina quirk is mis-stated, health changes; mana likewise depends on the wowpedia Intellect formula.
- **Ranges marked U**: Exorcism, Turn Undead, Blessing of Wisdom r2, Blessing of Protection, Blessing of Freedom.
- **Only +5 damage/healing found** at req ≤ 24 (Lorekeeper's Ring). The ≤24 +healing pool was not exhaustively
  searched: wowhead list pages are JS-only and wowclassicdatabase pages render client-side, so no working
  "filter by required level + stats" database was reachable in this run.
- **No `source_check` passage validation** was possible this run for Holy Light/Flash of Light base heals, Arctic
  Buckler armour, or Consecration's talent cost; those rest on fetched classicdb/wowcalc/wiki pages only. This is a
  validation limitation, not a confirmed error.
- **No measured** level-24 armour→damage-reduction, spell crit, dodge/parry values; a simulator needs those
  transition points and they are absent here.

## 9. Sources

- Kept: **https://wowcalc.net/en/classic/wiki/spells/paladin** — every paladin rank with learn level, mana, cast
  time, cooldown and tooltip, read from client 1.15.9.70003; the only complete per-rank table found.
- Kept: **classicdb.ch** (1.12): spells `1026`, `19750`, `633`, `20271`, `5588`, `26573`, `20375`, `19835`, `1152`,
  `10291`; items `13131`, `13012`, `6906`, `6087`, `20431`, `2039`, `6953`, `7002`, `14133`, `13127`, `1488`, `7752`,
  `13087` — ranges, Req Level, and the 1.12 tooltip values that disagree with 1.15.9.
- Kept: **https://warcraft.wiki.gg/wiki/Paladin_abilities_(Classic)** — authoritative learn-level/source table
  (Default/Trainer/Quest/Talent) per expansion; used to gate every ability at 24.
- Kept: **https://docs.rs/wow_world_base/latest/src/wow_world_base/extended/vanilla/base_stats.rs.html** — only
  per-level vanilla base-stat table found.
- Kept: **https://wowpedia.fandom.com/wiki/Base_mana** — Intellect→mana formula used in §1/§3.
- Kept: **https://wow-guides.com/knowledge/attributes** — Stamina→health rate + the first-20-points quirk.
- Kept: **https://www.warcrafttavern.com/wow-classic/guides/29-twink-paladin/** — vetted candidate item pool whose
  req levels I then checked one by one (most are unusable at 24).
- Kept: **https://warcraft.wiki.gg/wiki/Verigan%27s_Fist** · **/wiki/Arctic_Buckler** ·
  **/wiki/Turtle_Scale_collection** · **/wiki/Sanctity_Aura** · **https://wowpedia.fandom.com/wiki/Seal_of_Command**.
- Rejected: **wowhead classic item-list pages** (`/classic/items/armor/...`) — JS-only, no extractable stats.
- Rejected: **wowclassicdatabase.com** item pages — client-rendered; only the item-level title is readable.
- Rejected: **warcraft.wiki.gg/wiki/Base_attributes** — retail table, not vanilla.

## 10. Next steps

1. Choose the edition (1.12 DB vs 1.15.9 client) and re-derive every heal from one source; the ~4% Holy Light /
   Flash of Light gap is systematic.
2. Resolve Arctic Buckler armour (642 vs 87) — biggest lever in §3.
3. Enumerate the ≤24 suffix-green pool for head/back/waist/feet/chest from a database that filters by required level
   and stats, and record concrete rolls.
4. Get one real level-24 paladin sheet (health, mana, armour, crit) to replace the formula-derived pools.
