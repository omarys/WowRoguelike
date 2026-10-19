# Level 24 Mage (Classic Era 1.15 / vanilla 1.12 data) — sourced numbers for a game-balance file

Research date: session run of this task. Every number below carries a source URL. Values I could not verify against a fetchable source are explicitly marked **UNVERIFIED**. Where the two classic data lineages disagree (1.12-era databases vs Classic Era 1.15 tooltips), both values are given.

**Tooling note (read this first):** `web_search` and `fetch_content` were available and used. `classicdb.ch`, `warcraft.wiki.gg` (including `?action=raw` wikitext) and the `vanilla-wow-archive.fandom.com` / `classic-wow-archive.fandom.com` wikis are fetchable. **Wowhead Classic pages (items, spells, guides, gear-sets), `wowclassicdb.com`, `vanillawowdb.com`, `warcraftdb.com`, the Warcraft Tavern BiS-twink tool and `wowclassicbis.com` are NOT extractable** (JS-rendered / "content appears incomplete"). Wowhead values quoted below therefore come from search-result snippets of those pages, and are labelled as such. `source_check` ran for the Fireball Rank 5 claim and returned "unclear" (it retrieved the wowhead-classic page but its automated support assessment did not produce a verdict); the passages it stored were inspected manually and are quoted below.

---

## 1. Race used, and base class+race stats at level 24 BEFORE gear

**Race used: Gnome (Alliance).** Rationale source: Warcraft Tavern's level-29 twink mage guide, which states plainly that *"there is a clear winner for Mages when it comes to race and that is the Gnome… Too many benefits here to ignore"* and highlights **Expansive Mind** as *"might be the best racial trait to be had for a Mage"* — https://www.warcrafttavern.com/wow-classic/guides/29-twink-mage/

**Gnome racial, Classic version:** *Expansive Mind — Gnome racial, Passive, "Intelligence increased by 5%."* — https://warcraft.wiki.gg/wiki/Expansive_Mind (direct evidence, Classic tab of the page)

### Base class stats at level 24 (no gear, no racial offset)

| Stat | Value |
|---|---|
| Strength | 31 |
| Agility | 43 |
| Stamina | 43 |
| Intellect | 50 |
| Spirit | 56 |

Source: https://warcraft.wiki.gg/wiki/Base_attributes (raw wikitext fetched: mage column, row `24`: Agility `43`, Intellect `50`, Strength `31`, Stamina `43`, Spirit `56`; neighbours for sanity: row 23 = 40/47/29/41/52, row 25 = 45/52/32/45/58). **Support: direct evidence for the table values; source-interpretation caveat:** that page is the *current-era* wiki table (it contains Death Knights, Monks and rows up to level 100 and is flagged "outdated"). I could **not** find a Classic-Era-specific base-attribute table, so **whether rows 1–60 of that table are byte-identical to 1.15 values is UNVERIFIED.** (Its level-1 mage base of 8/10/11/12/13 is, however, consistent with the level-1 race-class values cross-checked in §1.2 below, which are pure vanilla sources.)

Other sourced anchors from the same table: level 23 mage base Spirit 52, level 25 Spirit 58; **base crit is 5% for all classes/races/levels** ("Players have 5% base crit for all classes, races, and levels." — same page, *Crit* section).

### 1.2 Race-adjusted base stats at level 24 — **UNVERIFIED**

Level-1 race-class starting values (Armor / Health / Mana included), direct evidence:
Gnome mage **Str 15, Agi 23, Sta 19, Int 27, Spi 22 — Armor 51, Health 51, Mana 225**; Human mage 20/20/20/23/23 — 45/52/165; Troll mage 21/22/21/19/23 — 49/62/119; Undead mage 19/18/21/21/27 — 41/62/135.
Sources: https://classic-wow-archive.fandom.com/wiki/Mage and (slightly different Gnome armour/agility variant) https://vanilla-wow-archive.fandom.com/wiki/Mage

Cross-check that validates those numbers (researcher derivation): with the wiki's mana rule "Mana from Intellect = min(20,Int) + 15×(Int−min(20,Int))" and level-1 mage base mana **100**, Gnome 100 + (20 + 15×7) = **225** ✔, Human 100 + (20+15×3) = **165** ✔, Troll 100 + 19 = **119** ✔, Undead 100 + 35 = **135** ✔. Same check on health with "1 health per stamina for the first 20 stamina, 10 per point thereafter" and base health **32** at level 1: Gnome 32+19 = 51 ✔, Human 32+20 = 52 ✔, Troll/Undead 32+30 = 62 ✔. (Rules: https://vanilla-wow-archive.fandom.com/wiki/Base_mana , https://worldofwarcraft.fandom.com/et/wiki/Attributes ; base health 32 comes from the class-level table in §5.)

**I did not find any source giving a per-race-per-level (or race-offset) stat table for Classic.** Therefore the gnome-adjusted level-24 base block (e.g. class base + the level-1 gnome offsets +7 Str/+13 Agi/+8 Sta/+15 Int/+9 Spi) is **UNVERIFIED** and is *not* used in the totals below. **Researcher inference (labelled):** because the +5% Intellect racial is an *aura* separate from the character's Int stat, the level-1 race-class differences are almost certainly a fixed race-class offset rather than the aura; but I have no source resolving how that offset behaves at level 24.

---

## 2. Best-in-slot gear list at level 24

**Important structural finding:** **no published level-24 mage BiS guide exists for Classic Era.** Searches returned only bracket guides for 19 and 29 (Wowhead's "Classic Level 19 Twink Mage BiS Guide" and Warcraft Tavern's level-29 twink guide — https://www.wowhead.com/classic/guide/mage-classic-level-19-twink , https://www.warcrafttavern.com/wow-classic/guides/29-twink-mage/), plus a JS-only community gear set "Best Gear for 24 Mage atm" (https://www.wowhead.com/classic/gear-set/best-gear-for-24-mage-atm-2188 — page not extractable). **The level-24 list below is therefore researcher-constructed** from item-database records, using only items whose required level is ≤ 24 (or which have no required level) — it is a *verified-availability* list, not a community-endorsed BiS ranking. Marked accordingly per row.

| Slot | Item | Req. level | Acquisition | Armor | Stats | Support/notes |
|---|---|---|---|---|---|---|
| Head | **Spellpower Goggles Xtreme** | none listed (requires Engineering 215) | Crafted, BoE (schematic 10605) | 46 | **+21 to damage/healing with magical spells** | https://classicdb.ch/?item=10502 — direct evidence. Usability at 24 rests on the profession tier gate below. |
| Neck | **Sentinel's Medallion** (ilvl-14 version) | Requires Level 9 | PvP reward, Silverwing Sentinels (1 Mark of Honor, Illiyana Moonblaze, Ashenvale) | 0 | +2 Agility, +2 Stamina | https://warcraft.wiki.gg/wiki/Sentinel%27s_Medallion — direct evidence. Weakest slot; see §8/§Missing evidence. |
| Shoulder | **Berylline Pads** | none listed (quest: Alliance `[27D]` "The Crone of the Kraul", Horde `[34D]` "A Vengeful Fate") | Quest (Razorfen Kraul) | 39 | +6 Spirit, +5 Stamina, +10 Intellect | https://classicdb.ch/?item=4197 (stats, no "Requires Level" line), https://warcraft.wiki.gg/wiki/Berylline_Pads (sources) |
| Back | **Engineer's Cloak** | none listed (quest `[27]` "Gerenzo Wrenchwhistle") | Quest reward, BoP | 20 | +5 Stamina, +2 Intellect | https://classicdb.ch/?item=6667 ; https://warcraft.wiki.gg/wiki/Engineer%27s_Cloak |
| Chest | **Robes of Arugal** | **24** | Shadowfang Keep, BoP | 46 | +10 Spirit, +9 Intellect, +5 Stamina, +3 Agility | https://classicdb.ch/?item=6324 (classicdb search page for "Robes of Arugal") |
| Chest (alt, BoE/AH) | **Tree Bark Jacket** | **19** | World drop, BoE | 42 | +6 Stamina, +10 Intellect | https://classicdb.ch/?item=1486 (via `?search=Tree+Bark+Jacket`) |
| Waist | **Belt of Arugal** | **24** | Shadowfang Keep, BoP | 26 | +10 Intellect, +3 Spirit, +2 Agility | https://classicdb.ch/?item=6392 |
| Legs | **Azure Silk Pants** | **23** | Crafted Tailoring 140 / AH, BoE | 36 | +6 Intellect; Equip: **+10 damage done by Frost spells** | https://classicdb.ch/?item=7046 (classicdb labels it the 1.4/Vanilla version) |
| Legs (alt) | **Sacred Burial Trousers** | none listed | Quest, BoP | 39 | +3 Strength, +5 Intellect, +9 Spirit | https://classicdb.ch/?item=6282 |
| Feet | **Spidersilk Boots** | **20** | Crafted Tailoring 125 / AH, BoE | 29 | +4 Stamina, +4 Intellect, +7 Spirit | https://classicdb.ch/?item=4320 |
| Wrist | **Mindthrust Bracers** | **17** | World drop, BoE | 17 | +9 Intellect, **−5 Stamina** | https://classicdb.ch/?item=1974 |
| Hands | **Magefist Gloves** | **15** | BoE / AH | 23 | +3 Stamina, +4 Spirit, +5 Intellect | https://classicdb.ch/?item=12977 |
| Ring | **Plains Ring** | **24** | BoE, Unique | 0 | +8 Stamina, +3 Intellect | https://classicdb.ch/?item=2039 |
| Ring | **Black Pearl Ring** | **17** | BoE, Unique (ilvl 24) | 0 | +2 Stamina, +2 Intellect, +6 Spirit | https://classicdb.ch/?item=6332 (also https://theforeverera.com/en/items/6332/ which states its "Classic values from Classic Era") |
| Two-hand | **Rod of the Sleepwalker** | **24** | Blackfathom Deeps (Twilight Lord Kelris), BoP | 0 | +11 Intellect, +10 Spirit; 53–80 damage, speed 2.80, 23.8 DPS | https://classicdb.ch/?item=1155 |
| Ranged (wand) | **Gravestone Scepter** | none listed (quest chain requires level 18) | Quest chain ending in `[27D]` "Blackfathom Villainy" (BFD) | 0 | +1 Spirit, +5 Shadow Resistance; see §6 | https://classicdb.ch/?item=7001 ; https://warcraft.wiki.gg/wiki/Blackfathom_Villainy_(Classic) (quest level 27, "(Requires 18)") |
| Trinket | **none verified usable by a mage at ≤24** | — | — | — | — | `Insignia of the Alliance` item 18854 on classicdb is "Classes: Warrior" only (https://classicdb.ch/?item=18854); the mage-usable PvP trinket ID was not verified. |

### Items checked and **excluded** because their required level exceeds 24 (all direct evidence from classicdb.ch)
| Item | Req. level | URL |
|---|---|---|
| Archer's Cloak | Requires Level 29 | https://classicdb.ch/?item=9860 |
| Beguiler Robes | Requires Level 29 | https://classicdb.ch/?item=7728 |
| Hotshot Pilot's Gloves | Requires Level 27 | https://classicdb.ch/?item=9491 |
| Basalt Necklace ("of the Eagle/Intellect") | Requires Level 27 | https://classicdb.ch/?item=12028 |
| Starfaller (wand, 32–60 arcane @1.40 = 32.9 DPS) | Requires Level 29 | https://classicdb.ch/?item=13063 |
| Lorekeeper's Ring (+4 Sta, +7 spell damage, 2 mana/5s) | Requires Level 28 | https://classicdb.ch/?item=19525 |
| Rose Colored Goggles | requires Engineering 230 → Artisan tier = character level 35 | https://classicdb.ch/?item=10503 |

**Profession gate that makes the head slot legal at 24** (needed for the Spellpower Goggles Xtreme claim): Apprentice = level 5 (skill 1–75), Journeyman = level 10 (50–150), **Expert = level 20 (125–225)**, Gnome/Goblin Engineering = level 30 (200–375), Artisan = level 35 (200–300), Master = level 50 (275–375). — https://warcraft.wiki.gg/wiki/Engineering_proficiencies (direct evidence). Engineering 215 therefore only needs character level 20, and 215 ≤ 225, so the goggles are obtainable at level 24. (Corroborating general rule: https://warcraft.wiki.gg/wiki/Classic_proficiency)

---

## 3. TOTALS at level 24 with that gear (base vs gear split)

| Stat | Class base (lvl 24) | From gear | **TOTAL** |
|---|---|---|---|
| Strength | 31 | +0 (alt legs give +3) | **31** |
| Agility | 43 | +7 (Robes of Arugal +3, Belt of Arugal +2, Sentinel's Medallion +2) | **50** |
| Stamina | 43 | +29 (Berylline +5, Robes +5, Spidersilk +4, Mindthrust −5, Magefist +3, Engineer's Cloak +5, Medallion +2, Plains Ring +8, Black Pearl Ring +2) | **72** |
| Intellect | 50 | +71 (Berylline +10, Robes +9, Belt +10, Azure Silk Pants +6, Spidersilk +4, Mindthrust +9, Magefist +5, Cloak +2, Plains Ring +3, Black Pearl Ring +2, Rod of the Sleepwalker +11) | **121** |
| Spirit | 56 | +47 (Berylline +6, Robes +10, Belt +3, Spidersilk +7, Magefist +4, Black Pearl Ring +6, Rod of the Sleepwalker +10, Gravestone Scepter +1) | **103** |

Arithmetic: gear sums are additions of the item values in §2 (direct evidence per item); the base row is the §1 table. **The split is only valid if the caveat in §1 holds** (class base table is Classic-identical — UNVERIFIED) **and** the gnome race offset is ignored (see §1.2, UNVERIFIED). With the gnome **Expansive Mind** aura (+5% Intellect: https://warcraft.wiki.gg/wiki/Expansive_Mind) applied to the gear-inclusive Intellect: floor(121 × 1.05) = **127 Intellect** (researcher computation from the sourced 5% and the §3 total).

---

## 4. Total ARMOUR from that gear

**282 armour**, summed from the item armour values: head 46 + shoulders 39 + chest 46 + waist 26 + legs 36 + feet 29 + wrist 17 + hands 23 + back 20 = 282. (Neck, both rings, staff, wand and trinket contribute 0 base armour; rings/weapons amulets/trinkets "have no base armor" — https://worldofwarcraft.fandom.com/et/wiki/Attributes.) Per-item sources are in §2.

Excluded: **armour contributed by Agility is UNVERIFIED.** Pre-Cataclysm Agility increased armour (the Cataclysm patch notes on the same Attributes page record "Agility: … **no longer increases armor**", i.e. it did before), but I found **no source stating the vanilla coefficient** at level 24, so no number is claimed here. **UNVERIFIED.**

---

## 5. Health pool, mana pool, base regen behaviour at level 24

Sourced building blocks:
* 1 Stamina = 10 health, with the **first 20 Stamina giving 1 health each**; 1 Intellect = 15 mana, with the **first 20 Intellect giving 1 mana each** — https://worldofwarcraft.fandom.com/et/wiki/Attributes and the base-mana formula on https://warcraft.wiki.gg/wiki/Base_mana
* **Base mana (mage) by level:** level 1 = 100, 20 = 371, 21 = 385, 22 = 415, 23 = 431, 26 = 515, 28 = 556, 30 = 613, 34 = 712, 35 = 733, 38 = 811, 40 = 853, 43 = 916, 48 = 1021, 54 = 1117, 55 = 1138, 60 = 1213 — https://warcraft.wiki.gg/wiki/Base_mana **and** https://vanilla-wow-archive.fandom.com/wiki/Base_mana. **Level 24 is a documented gap (`-`) in both tables.**
* **Gap filled by cross-validated class-level table:** `player_classlevelstats` (class 8 = Mage) row `(8,24, 298, 463)` → **base health 298, base mana 463** at level 24 — https://raw.githubusercontent.com/azerothcore/database-wotlk/master/sql/base/player_classlevelstats.sql (direct evidence from the file). **Validation (researcher):** every overlap with the two wiki tables above matches exactly — level 1 (32 HP / 100 mana), 20 (371), 21 (385), 22 (415), 23 (431), 26 (515), 28 (556), 29 (592), 30 (613), 34 (712), 35 (733), 38 (811), 40 (853), 43 (916), 48 (1021), 54 (1117), 55 (1138), 60 (1213), plus level-1 base HP 32 which is independently required by the four vanilla level-1 race/health data points in §1.2. **Caveat: this is a WotLK-server base table, not an official Classic dump** — treat 298/463 as high-confidence inference from a cross-validated table, not as a Classic tooltip.

Computed (researcher arithmetic on the sourced rules and values):

| Quantity | Value |
|---|---|
| Health from Stamina | 20 + 10×(72−20) = **540** |
| **Total health** | 298 + 540 = **838** |
| Mana from Intellect | 20 + 15×(121−20) = **1535** |
| **Total mana** | 463 + 1535 = **1998** |
| Mana with gnome Expansive Mind (Int 127) | 463 + 20 + 15×107 = **2088** |

**Base mana-regen behaviour:** mana regeneration has two parts — out of combat you regenerate the full amount (Spirit regen + combat regen); in combat, normally only the combat-regen component (5% of base mana per 5 s) — https://warcraft.wiki.gg/wiki/Mana_regeneration (direct evidence; page flags itself outdated and describes the post-4.0.1 state, but it also records that the **five-second rule** applied before 4.0.1, i.e. in Classic). **Exact Classic Spirit→mana-per-5s formula at level 24: UNVERIFIED** (no source located); note also that for mages Spirit is the *only* regen stat in Classic (there is no in-combat mana regen for mages without talents/MP5 gear — the 19/29-bracket sources treat Spirit as the regen stat only).

---

## 6. Wand (the mage's auto-attack), and main-hand/off-hand

**BiS wand at level 24: Gravestone Scepter.**
* 30–57 Shadow damage, speed **1.50**, **29.0 damage per second**, +1 Spirit, +5 Shadow Resistance — https://classicdb.ch/?item=7001 and https://octowow.st/db/?item=7001 (both direct evidence, identical values). Quest reward from the Blackfathom Deeps chain "Blackfathom Villainy" (`[27D]`, requires level 18) — https://warcraft.wiki.gg/wiki/Blackfathom_Villainy_(Classic). No "Requires Level" line appears on the item in classicdb/octowow/tbcdb records, and the quest only requires level 18, so it is usable at 24.
* **Contradiction (recorded, not resolved):** https://warcraft.wiki.gg/wiki/Gravestone_Scepter shows the same item as **"6 – 12 Shadow Damage (6 damage per second) … +1 Spirit, +6 Shadow Resistance"** while listing Item Level 29 and speed 1.50. The 30–57 / 29.0 DPS figure matches the item's reputation ("Its DPS was very high for the level it could be obtained at… useful to level 40+", same wiki page) and matches three independent databases; the wiki's 6–12 figure is almost certainly a level-scaled template artifact. **Use 30–57 @1.50 (29.0 DPS).**

Other wands checked: **Starfaller** — 32–60 Arcane @1.40 = **32.9 DPS**, but **Requires Level 29 → NOT available at 24** (https://classicdb.ch/?item=13063). "Cookie's Stirring Rod" (22.3 DPS, level 17, Deadmines) was only seen in a search summary of https://wowclassicforever.info/classes/mage/weapons/ — **UNVERIFIED**, and in any case lower DPS than Gravestone Scepter.

**Main-hand/off-hand:** the mage **does not need one** with this list — the BiS weapon found is the two-hander **Rod of the Sleepwalker** (req 24, +11 Intellect/+10 Spirit), which occupies both hand slots, and the wand is the separate ranged slot. Mages can use staves, wands, daggers and swords (with training) — https://classic-wow-archive.fandom.com/wiki/Mage. **A one-hand + off-hand combination that beats the staff at ≤24 is UNVERIFIED** (the level-29 list's pair — Zealot Blade main hand, Witch's Finger / Durable Rod off hand: https://www.warcrafttavern.com/wow-classic/guides/29-twink-mage/ — had no verified ≤24 required levels; see Missing evidence).

---

## 7. Real level-24 spell tooltip values

Highest rank usable at 24 for each spell. All figures below are direct evidence from https://classicdb.ch/?spell=<id> (classicdb prints the spell's learn level as "Level: N"); where Classic Era 1.15 tooltips differ, the wowhead-classic value is given too.

| Spell | Highest rank at 24 | Learn level | Mana | Cast | Effect | Rank NOT available at 24 |
|---|---|---|---|---|---|---|
| **Fireball** | Rank 5 (id 8400) | 24 | 140 | 3.5 s | **wowhead classic / Classic Era: 146–195 Fire + 20 over 8 s**; classicdb (1.12-era): 139–188 Fire + 20 over 8 s | Rank 6 at level 30 |
| **Frostbolt** | Rank 4 (id 7322) | 20 | 65 | 2.6 s | 74–83 Frost, movement slowed 40% for 7 s | **Rank 5 (id 8406) = level 26 → NOT available** |
| **Arcane Missiles** | Rank 3 (id 5145) | 24 | 235 | Channeled 5 s | 56 Arcane damage each second (5 ticks, via child spell 7270) | Rank 4 (id 8416) = level 32 → NOT available |
| **Arcane Explosion** | Rank 2 (id 8437) | 22 | 120 | Instant | 57–64 Arcane to all targets within 10 yd | Rank 3 (id 8438) = level 30 → NOT available |
| **Blizzard** | Rank 1 (id 10) | 20 | 320 | Channeled 8 s | 200 Frost over 8 s (25 per 1 s tick, 8 yd radius) | Rank 2 (id 6141) = level 28 → NOT available |
| **Polymorph** | Rank 2 (id 12824) | 20 | 90 | 1.5 s | Up to 30 s sheep, 30 yd, Beasts/Humanoids/Critters, one target | Rank 3 = level 40 → NOT available |

Sources: https://classicdb.ch/?spell=8400 , https://classicdb.ch/?spell=7322 , https://classicdb.ch/?spell=5145 , https://classicdb.ch/?spell=8437 , https://classicdb.ch/?spell=10 , https://classicdb.ch/?spell=12824 (all direct evidence). Fireball Rank 5 Classic Era value 146–195 + 20/8 s: https://www.wowhead.com/classic/spell=8400/fireball (search-result snippet of the live tooltip: "140 Mana | 35 yd range | 3.5 sec cast | Requires Mage | Requires level 24 | Hurls a fiery ball that causes 146 to 195 Fire damage and an additional 20 Fire damage over 8 sec."), corroborated by https://www.stillsixty.com/spell/8400 ("Classic Era … 146 to 195 … 140 Mana … Requires Level 24"). The 1.12-era 139–188 value: https://classicdb.ch/?spell=8400 and https://turtledb.cthuly.com/site/spell/8400.html , https://turtledb.cthuly.com/site/spell/8400.html (also differs on mana: 120). Rank order/train levels for Fireball also confirmed by Wowhead's fire leveling guide search snippet ("Fireball: … train this at the following levels: 1, 6, 12, 18, 24, 30, …" — https://www.wowhead.com/classic/guide/classes/mage/fire/leveling-tips ). Frostbolt cast-time/rank detail cross-check: https://www.wowhead.com/classic/spell=7322/frostbolt snippet ("65 Mana, 30 yd, 2.6 sec cast, Requires level 20") matches classicdb exactly.

Adjacent values sometimes quoted (useful for the balance file): Frostbolt Rank 1 = level 4, 25 mana, 1.5 s, 18–20; Rank 3 = level 14, 50 mana, 2.2 s, 51–57, slow 40%/6 s — https://www.wowhead.com/classic/spell=116/frostbolt , https://www.wowhead.com/classic/spell=837/frostbolt (search snippets). Fireball Rank 1 (Classic, wiki tooltip) = 30 mana, 1.5 s, 16 to 25 + 2 over 4 s — https://warcraft.wiki.gg/wiki/Fireball (Classic tab). **Rank *availability* is the decision-relevant part and is verified; the rank-1 numbers are secondary.**

Spell *access* list for a level-24 mage (utility, from the wiki): Arcane Intellect 1, Conjure Water 4, Conjure Food 6, Arcane Missiles 8, Polymorph 8, Frostbolt 4, Fireball 1/6/12/18/24 … — https://warcraft.wiki.gg/wiki/Mage_abilities_(Classic) and https://warcraft.wiki.gg/wiki/Mage_abilities_(Classic) . The 19-twink source notes the AoE-leveling floor: "You need to be at least level 22 before the real AoE leveling will be possible" — https://www.zockify.com/classic/mage/leveling/ (search snippet).

---

## 8. Total +spell damage from that gear, and crit chance

**+Spell damage:** **+21 to all spell damage and healing** (Spellpower Goggles Xtreme — https://classicdb.ch/?item=10502 ), plus **+10 Frost-only** (Azure Silk Pants — https://classicdb.ch/?item=7046 ). Total: **+21 general, +31 for Frost spells**; no other item in the list carries spell damage. Note the level-29-bracket caster pieces that *do* carry spell damage (e.g. Lorekeeper's Ring +7, req 28) are **not** usable at 24 (§2).

**Crit chance:**
* **Base 5%** for all classes/races/levels — https://warcraft.wiki.gg/wiki/Base_attributes (*Crit* section).
* **Intellect contribution at level 24: UNVERIFIED.** The only authoritative statement found is the Blizzard clarification quoted at https://www.lurkerlounge.com/forums/thread-4286.html : *"At level 60, a Mage's expected amount of INT is 286 points. That expected amount yields a 5% crit chance. This works out to 59.5 points of INT for each Crit% point. At levels before 60, the numbers are different…"* The companion table gives Mage Expected@60 = 286 and "L.60 1% Crit = 59.5" (https://worldofwarcraft.fandom.com/et/wiki/Attributes), and the game's client data is the `GtChanceToSpellCrit` DBC indexed by class and level 1–100 (https://trinitycore.info/files/DBC/335/gtchancetospellcrit — schema only, values not published there).
* With the §3 total of **121 Intellect**: applying the level-60 rate ⇒ **+2.03% spell crit**; applying a level-proportional estimate (59.5 × 24/60 = 23.8 Int per 1%) ⇒ **+5.08%**. Total spell crit is therefore **≈7.0%** (level-60 rate) to **≈10.1%** (level-proportional estimate), **both UNVERIFIED**. **Researcher inference (labelled):** the linear-with-level reading is the reading implied by the quoted statement ("the numbers are different, but keep the basic ratio of expected amount of INT" per level); it is *not* confirmed by a Classic source.

---

## Contradictions

1. **Fireball Rank 5 damage.** Wonders/tooltips disagree by data lineage: **146–195 + 20 over 8 s** (Wowhead Classic tooltip; corroborated by stillsixty's "Classic Era" column) vs **139–188 + 20 over 8 s** (classicdb.ch, turtledb). Mana also differs in one source (turtledb: 120 vs 140). Same cast time (3.5 s) and same learn level (24) everywhere. Cause is almost certainly 1.12-era vs Classic Era (1.15) data. **Recommendation: use 146–195 for a 1.15 target, 139–188 for a 1.12 target.**
2. **Gravestone Scepter damage.** warcraft.wiki.gg shows 6–12 (6 DPS) while classicdb.ch, octowow and tbcdb show 30–57 (29.0 DPS) at speed 1.50. The wiki figure looks like a template scaling artifact; the 30–57 value is corroborated by three databases and by the wiki's own prose about its high DPS. Shadow resistance is +5 (three DBs) vs +6 (wiki).
3. **Level-1 gnome mage armour/agility.** classic-wow-archive lists Gnome mage Agility 23 / Armor 51; vanilla-wow-archive lists Agility 22 / Armor 44 (both with Int 27, mana 225). Trivial for §3 but recorded.
4. **Warcraft Tavern's level-29 BiS head (Spellpower Goggles Xtreme, Engineering 215) vs its own bracket.** The profession table (Expert = level 20) makes this legal at 29 — so no contradiction with the guide, but note that the same guide's *neck* (Basalt Necklace, req 27) and other picks would be illegal at 24, which is why a 29 list cannot be reused for a 24 build.

**None found** for: spell learn levels and mana/cast-time values (classicdb vs wowhead classic agree on every spell checked except Fireball's damage), the required-level values in §2 exclusions, and the profession tier table.

---

## Missing evidence

* **Per-race level-24 base stats for Classic** (the actual number on a gnome mage's character sheet before gear). No per-race-per-level table or calculator with a citable value could be extracted; the class-only table is used instead and its Classic-identity is unproven. **UNVERIFIED.**
* **Base health and base mana at level 24 as official Classic values.** The wiki tables have a hole exactly at mage level 24; the 298/463 pair comes from a cross-validated WotLK-emulator base table. **High-confidence inference, not a Classic source.**
* **Armour from Agility** (coefficient) at level 24. **UNVERIFIED.**
* **Intellect→spell crit conversion at level 24.** Only the level-60 rate (59.5) and the "scales with level" statement exist. **UNVERIFIED.**
* **Spirit→mana-per-5s regen formula** for an out-of-combat level-24 mage (and the classic five-second-rule implementation details). **UNVERIFIED.**
* **A mage-usable PvP trinket at ≤24** (`Insignia of the Alliance` item 18854 is Warrior-only on classicdb; the class-agnostic version's ID/required level was not verified). **UNVERIFIED.**
* **A caster neck at ≤24 worth the slot** — the only verified neck was the Agility/Stamina WSG medallion. Random-suffix greens ("of the Owl/Intellect") of the appropriate item level are the practical filler, but I found no fetchable table giving the exact suffix values at item level ≤ 24, so no number is quoted. **UNVERIFIED.**
* **Level-24 one-hand + off-hand pair** (and required levels for Zealot Blade / Witch's Finger / Durable Rod / Defiler's & Highlander's cloth belt+boots / Abomination Skin Leggings). **UNVERIFIED.**
* **Whether any published level-24 mage BiS list exists** beyond the unfetchable Wowhead gear set 2188 — I found none in text form.

---

## Sources

**Kept**
* https://classicdb.ch/?item=<id> and https://classicdb.ch/?spell=<id> — the workhorse: item stats **with explicit "Requires Level N"** lines, spell learn levels ("Level: N"), mana, cast times. (Used for 10502, 10503, 4197, 9860, 7728, 1974, 9491, 12028, 1155, 13063, 1486, 6324, 6392, 10657, 12977, 7046, 6282, 4320, 6667, 18854, 19525, 2039, 6332, 7001; spells 8400, 7322, 5145, 8437, 10, 12824.)
* https://warcraft.wiki.gg/wiki/Base_attributes (raw wikitext) — level-by-level base stats per class including mage level 24, base crit 5%.
* https://warcraft.wiki.gg/wiki/Base_mana — base-mana table (documents the level-24 gap) and the mana-from-Intellect formula.
* https://vanilla-wow-archive.fandom.com/wiki/Base_mana — independent copy of the same base-mana table (also shows the gap) + a second formula statement.
* https://warcraft.wiki.gg/wiki/Engineering_proficiencies — the level gates (Expert = 20) that decide whether the BiS head is legal at 24.
* https://warcraft.wiki.gg/wiki/Expansive_Mind — Gnome Classic racial: +5% Intellect.
* https://warcraft.wiki.gg/wiki/Blackfathom_Villainy_(Classic) — Gravestone Scepter's source quest level/requirement (27 / requires 18).
* https://warcraft.wiki.gg/wiki/Gravestone_Scepter , https://octowow.st/db/?item=7001 , https://tbcdb.com/?item=7001 — wand values (and the recorded contradiction).
* https://raw.githubusercontent.com/azerothcore/database-wotlk/master/sql/base/player_classlevelstats.sql — class/level base health + base mana rows (mage level 24 = 298/463), cross-validated against two wiki tables.
* https://warcraft.wiki.gg/wiki/Mana_regeneration — regen model + five-second-rule history.
* https://worldofwarcraft.fandom.com/et/wiki/Attributes — stamina/intellect conversion wording ("first 20 points"), Int→crit expectation table (ignore its post-Cata sections).
* https://www.lurkerlounge.com/forums/thread-4286.html — the Blizzard clarification on 59.5 Int per 1% mage spell crit at 60 and its level dependence.
* https://www.warcrafttavern.com/wow-classic/guides/29-twink-mage/ — race ranking (Gnome), and the level-29 bracket BiS list used as an item *candidate pool* (each candidate then checked for ≤24 legality on classicdb).
* https://www.wowhead.com/classic/spell=8400/fireball , https://www.stillsixty.com/spell/8400 — Classic Era Fireball Rank 5 tooltip (146–195). (Also used by the `source_check` run for that claim.)
* https://classic-wow-archive.fandom.com/wiki/Mage , https://vanilla-wow-archive.fandom.com/wiki/Mage — level-1 race-class stat/health/mana table, weapon proficiencies, rune of race choices.

**Rejected / deprioritized**
* Wowhead Classic item/guide/gear-set pages (19-twink guide, "Best Gear for 24 Mage atm", fire leveling guide, `classic-wow-stats-and-attributes-overview`) — not extractable; used only via search snippets where they uniquely corroborate (Fireball R5 tooltip, train-level list).
* https://www.warcrafttavern.com/wow-classic/tools/bis-twink/mage/29/ and https://www.wowclassicbis.com/twink/mage/29 — JavaScript-only, no extractable content.
* https://vanilla-wow-archive.fandom.com/wiki/Fireball — its "Table" is a late-expansion (percent-of-base-mana, level-through-10) artifact, not vanilla rank data.
* https://vanilla-wow-archive.fandom.com/wiki/Base_health — only lists level-80 base health; no classic by-level data.
* https://www.wowhead.com/classic/gear-set/29-twink-frost-mage-20372 and reddit/forum threads (r/classicwow, xpoff.com "24 Twink Gear Guides", ownedcore 29 guide) — community chatter, no verifiable per-number data (the xpoff "24 twink" threads are retail-era, not Classic Era).
* https://wowclassicforever.info/classes/mage/weapons/ — a private-server-flavoured weapons planner; its Cookie's Stirring Rod line is unverified.
* https://www.wowdb.com/items/7001 — modern retail tooltip for the same item ID (shows +16 Int/+4 Sta, ilvl 18): wrong era entirely, excluded.
* Google Sheets / calculators (sixtyupgrades, warcrafttavern base-stats tool, cal67.calculator.city) — JS or inaccessible, no citable values.

---

## Next steps (highest value only)

1. Get a **Classic Era client dump of `player_levelstats`** (1.15 DBC) to fix the two biggest gaps at once: per-race base stats at 24 and the gnome offset (the `GtChanceToSpellCrit` DBC from the same dump would also fix Int→crit).
2. Pull **Wowhead Classic item pages through a browser-capable fetcher** to (a) confirm required levels for the Ring/Neck/legs alternatives and the Defiler's/Highlander's cloth pieces, and (b) read the "Best Gear for 24 Mage atm" gear set (id 2188) as a community cross-check on §2.
3. Verify a **caster neck and trinket at ≤24** (random-suffix "of Intellect/Owl" greens at item level ≤24) — currently the two softest slots in the list.
