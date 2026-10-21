# Classic Era (1.12 / 1.15) — Warrior & Rogue ladders, levels 13–40

Built for the dungeon-party simulation. Base stat rows already exist in the repo, so this file covers **ability ranks** (which rank is live at each level, and what it does) and **gear bands** (what a levelling character could plausibly be wearing).

**Confidence key used in every table**
- `V` = verified: a real tooltip / spell page was read (URL cited in the row or in Sources).
- `M` = medium: corroborated by a secondary DB or a consistent secondary table, numbers plausible but not read off a primary tooltip.
- `U` = **UNVERIFIED** — my inference from vanilla memory; do not ship as fact.

**Method / its limits (read this before trusting numbers)**
- Wowhead Classic spell pages, `classicdb.ch`, `classicdb.com` and `wowhead.com/classic/item=…` are **JavaScript-rendered or 403/404 to plain fetch** as of this run. Only `wowhead.com/classic/spell=<id>` (raw HTML), `warcraft.wiki.gg`, `wowclassicdatabase.com` (sometimes), `foreverdb.net`, and search-provider page extracts returned readable data.
- Consequently: **rank→level mappings are solid**; **per-rank numbers are mixed**; several armour pieces have a verified required level but unverified armour/stats. Everything doubtful is marked.
- `1.12` (vanilla) and `1.15` (Classic Era) share these numbers; no Era-specific scaling differences were found for the abilities below.

---

# WARRIOR

## Table W1 — Ability ranks by level (13–40, with sub-13 context)

| Ability | Rank | Level trained | Cost | Cast | Cooldown | Effect | Conf |
|---|---|---:|---|---|---|---|---|
| Heroic Strike | R1 (context) | 1 | 15 rage | next melee | none (replaces swing) | +11 damage, extra threat | V |
| Heroic Strike | R2 (context) | 8 | 15 rage | next melee | none | +21 damage | V |
| Heroic Strike | **R3** | **16** | 15 rage | next melee | none | **+32 damage**, extra threat | V |
| Heroic Strike | **R4** | **24** | 15 rage | next melee | none | **+44 damage** | V |
| Heroic Strike | **R5** | **32** | 15 rage | next melee | none | **+58 damage** | V |
| Heroic Strike | **R6** | **40** | 15 rage | next melee | none | **+80 damage** | V |
| Sunder Armor | R1 (context) | 10 (class quest, not trainer) | 15 rage | instant | none | −90 armour per stack, 5 stacks, 30 s, high threat | V |
| Sunder Armor | **R2** | **22** | 15 rage | instant | none | **−180 armour/stack** (5 stacks, 30 s) | V |
| Sunder Armor | **R3** | **34** | 15 rage | instant | none | **−270 armour/stack** | V |
| Charge | R1 (context) | 4 | 0 | instant | 15 s (U) | 8–25 yd, 1 s stun, **+9 rage generated**, no damage | M |
| Charge | **R2** | **26** | 0 | instant | 15 s (U) | **+12 rage generated**, 1 s stun | M |
| Shield Bash | **R1** | **12** | 10 rage | instant | 12 s | 6 damage + interrupt, 8 s school lockout, requires shield | V |
| Shield Bash | **R2** | **32** | 10 rage | instant | 12 s | 18 damage + interrupt, 8 s lockout | M |
| Shield Block | **R1** | **16** | 10 rage | instant | 5 s | +75 % block chance for 5 s, requires shield | M |
| Disarm | **R1** | **18** | 20 rage | instant | 1 min | disarms target's weapon for 10 s | M |
| Execute | **R1** | **24** | 15 rage + all remaining | instant | none | 125 base + 3 per extra rage | M |
| Execute | **R2** | **32** | 15 rage + all remaining | instant | none | 200 base + 6 per extra rage | M |
| Execute | **R3** | **40** | 15 rage + all remaining | instant | none | 325 base + 9 per extra rage | M |
| Shield Wall | **R1** | **28** | 0 | instant | 30 min | −75 % damage taken for 10 s, requires shield | M |
| Taunt | **R1** | **10** (context) | 10 rage (U) | instant | 10 s | taunts: sets threat to the highest threat target | M |

**Conflicts recorded, not resolved:**
- Sunder Armor R1 level: the rank table says **10** (class quest), but `warcraft.wiki.gg/wiki/Sunder_Armor` shows "Level learned 16" for one Classic instance. Use 10 (matches the widely quoted ladder 10/22/34/46/58).
- Taunt rage cost: secondary sources say "no cost", others "10 rage". Marked U; the simulation already uses 10 rage.
- Shield Wall cooldown: 30 min (vanilla's long defensive cooldown). One secondary source implied 5 min — rejected.
- Charge damage: Classic Charge deals **no damage**; it only stuns and generates rage. Some private-server data adds damage — ignore.
- Disarm cost 20 rage / 1 min CD is medium confidence; older sources say 10 rage.

### What changes at which level — Warrior

| Level | New |
|---|---|
| 13 | nothing new (previous: Shield Bash R1 @12) |
| **16** | **Heroic Strike R3 (+32)**; **Shield Block** |
| **18** | **Disarm** |
| **22** | **Sunder Armor R2 (−180)** |
| **24** | **Heroic Strike R4 (+44)**; **Execute R1 (125 +3/rage)** |
| **26** | **Charge R2 (+12 rage)** |
| **28** | **Shield Wall** (−75 % / 10 s / 30 min) |
| **32** | **Heroic Strike R5 (+58)**; **Execute R2 (200 +6/rage)**; **Shield Bash R2 (18)** |
| **34** | **Sunder Armor R3 (−270)** |
| **40** | **Heroic Strike R6 (+80)**; **Execute R3 (325 +9/rage)**; **plate armour trainable** (mail until then) |

Biggest sim-relevant steps: 16 (HS +32, Shield Block), 24 (HS +44 and Execute appear in the same level), 32 (HS +58, Execute +6/rage, Shield Bash 18), 34 (sunder −270), 40 (HS +80, Execute +9/rage, plate).

---

## Table W2 — Gear bands (Warrior, mail 13–39 → plate at 40)

Armour class progression: **mail from level 1 to 39; plate becomes wearable at 40** (trainer). Leather is wearable below mail but is a downgrade in armour.

| Band | Item | Slot | Req lvl | Armour | Stats | Source | Conf |
|---|---|---|---:|---:|---|---|---|
| 13–17 | Rockslicer | 2H axe | 16 | – | 36–55 dmg, speed 3.30 (13.79 dps), +7 Str | Deadmines, Rhahk'Zor (~4 %) | V |
| 13–17 | Goblin Mail Leggings | Legs (mail) | 14 | 144 | +4 Str, +3 Sta | Deadmines trash | M |
| 13–17 | Dagmire Gauntlets | Hands (mail) | 18 | U | U | Wailing Caverns quest | U |
| 13–17 | green "of the Bear" mail chest/shoulder | — | ~13–17 | U | +Sta/+Str suffix | world drops | U |
| 18–22 | Smite's Mighty Hammer | 2H mace | 18 | – | 55–83 dmg, speed 3.50 (19.7 dps), +11 Str, +4 Agi | Deadmines, Mr. Smite | V |
| 18–22 | Cruel Barb | 1H sword | 19 | – | 30–57 dmg, speed 2.80 (15.54 dps), +12 Attack Power | Deadmines, Edwin VanCleef (~16 %) | V |
| 18–22 | Mutant Scale Breastplate | Chest (mail) | 23 | 211 | +6 Str, +6 Sta | Wailing Caverns, Mutanus | M |
| 18–22 | Taskmaster Axe | 1H/2H axe | U (~20s) | – | U | Deadmines | U |
| 23–27 | Mutant Scale Breastplate (see above) | Chest (mail) | 23 | 211 | +6 Str, +6 Sta | WC | M |
| 23–27 | green mail "of the Bear" / "of Strength" shoulders+belt | — | 23–27 | U | U | world drops | U |
| 23–27 | Bonebiter | 2H axe | U (likely 38–40) | – | U | Razorfen Downs quest chain | U |
| 28–32 | mail from Gnomeregan / Razorfen Downs (no specific BiS verified) | — | 28–32 | U | U | – | U |
| 28–32 | Swinetusk Shank is rogue-only in practice (dagger) — warrior keeps the 2H | — | — | – | – | – | U |
| 33–37 | Herod's Shoulder | Shoulder (mail) | 37 | 196 | +6 Str, +15 Sta | SM Armory, Herod (~34 %) | V |
| 33–37 | Ravager | 2H axe | 37 | – | dmg U (87–131 claimed), proc: 3 extra attacks | SM Armory, Herod | M |
| 38–40 | Scarlet Leggings | Legs (mail) | 38 | 233 | +20 Str, +10 Sta | SM (Scarlet mobs) | V |
| 38–40 | Mograine's Might | 2H mace | 39 | – | dmg U | SM Cathedral | M |
| 38–40 | Hand of Righteousness | 1H mace | 39 | – | 56–105 dmg, speed 2.70 (claimed) | SM Cathedral | M |
| 38–40 | **plate**: any plate quest/dungeon blue at 40 | all | 40 | higher | — | trainers unlock plate | V (rule) |

Notes for the sim: the only verified 13–40 weapon **damage/speed** pairs are Rockslicer (3.30/36–55), Smite's Mighty Hammer (3.50/55–83), Cruel Barb (2.80/30–57) and Hand of Righteousness (2.70/56–105, M). Weapon-damage-based abilities (Heroic Strike, Execute is flat, Charge none) scale off these, so use those four as the weapon ladder if only four data points are needed: 16 → 18 → 19 (1H) → 37 → 39.

---

# ROGUE

## Table R1 — Ability ranks by level (13–40, with sub-13 context)

| Ability | Rank | Level trained | Cost | Cast | Cooldown | Effect | Conf |
|---|---|---:|---|---|---|---|---|
| Sinister Strike | R1 (context) | 1 | 40 energy | instant | none | weapon damage + 3 | M |
| Sinister Strike | R2 (context) | 6 | 40 energy | instant | none | weapon damage + 6 | M |
| Sinister Strike | **R3** | **14** | 40 energy | instant | none | **weapon damage + 10** | V (level), M (value) |
| Sinister Strike | **R4** | **22** | 40 energy | instant | none | **weapon damage + 15** | M |
| Sinister Strike | **R5** | **30** | 40 energy | instant | none | **weapon damage + 22** | M |
| Sinister Strike | **R6** | **38** | 40 energy | instant | none | **weapon damage + 33** | M |
| Backstab | R1 (context) | 4 | 60 energy | instant | none | 150 % weapon dmg + 15, requires dagger, behind target | M |
| Backstab | R2 (context) | 12 | 60 energy | instant | none | 150 % weapon dmg + 30 | M |
| Backstab | **R3** | **20** | 60 energy | instant | none | **150 % weapon dmg + 48** | M |
| Backstab | **R4** | **28** | 60 energy | instant | none | **150 % weapon dmg + 69** | M |
| Backstab | **R5** | **36** | 60 energy | instant | none | **150 % weapon dmg + 90** | M |
| Eviscerate | R1/R2 (context) | 1 / 8 | 35 energy | instant | none | finisher, 1–5 CP, flat damage | V (level) |
| Eviscerate | **R3** | **16** | 35 energy | instant | none | 1 CP 25–39 … 5 CP 101–115 | M |
| Eviscerate | **R4** | **24** | 35 energy | instant | none | 1 CP 41–61 … 5 CP 165–185 | M |
| Eviscerate | **R5** | **32** | 35 energy | instant | none | 1 CP 60–90 … 5 CP 240–270 | M |
| Eviscerate | **R6** | **40** | 35 energy | instant | none | 1 CP 99–143 … 5 CP 407–451 | M |
| Gouge | R1 (context) | 6 | 45 energy | instant | none | 10 damage, blinds 4 s (breaks on damage) | M |
| Gouge | **R2** | **18** | 45 energy | instant | none | **20 damage**, 4 s blind | M |
| Gouge | **R3** | **32** | 45 energy | instant | none | **32 damage**, 4 s blind | M |
| Kick | **R1** | **12** | 25 energy | instant | 10 s | 15 damage + interrupt | M |
| Kick | **R2** | **26** | 25 energy | instant | 10 s | 30 damage + interrupt | M |

Full Eviscerate per-CP table (tooltip base, before Attack Power; from the warcraftdb spell page):

| Rank (level) | 1 CP | 2 CP | 3 CP | 4 CP | 5 CP |
|---|---|---|---|---|---|
| R1 (1) | 6–10 | 11–15 | 16–20 | 21–25 | 26–30 |
| R2 (8) | 14–22 | 25–33 | 36–44 | 47–55 | 58–66 |
| R3 (16) | 25–39 | 44–58 | 63–77 | 82–96 | 101–115 |
| R4 (24) | 41–61 | 72–92 | 103–123 | 134–154 | 165–185 |
| R5 (32) | 60–90 | 105–135 | 150–180 | 195–225 | 240–270 |
| R6 (40) | 99–143 | 176–220 | 253–297 | 330–374 | 407–451 |

**Conflicts recorded:** Backstab "150 % weapon damage" is constant across ranks per the vanilla wiki quote; only the flat bonus grows. Some private-server DBs list a rising % (165/180/…) — rejected, not Classic. Backstab R6/R7 bonuses (44/52) could not be verified and are out of the 13–40 required range anyway.

### What changes at which level — Rogue

| Level | New |
|---|---|
| **14** | **Sinister Strike R3 (+10)** |
| **16** | **Eviscerate R3** |
| **18** | **Gouge R2 (20 dmg)** |
| **20** | **Backstab R3 (+48)** |
| **22** | **Sinister Strike R4 (+15)** |
| **24** | **Eviscerate R4** |
| **26** | **Kick R2 (30 dmg)** |
| **28** | **Backstab R4 (+69)** |
| **30** | **Sinister Strike R5 (+22)** |
| **32** | **Eviscerate R5**; **Gouge R3 (32 dmg)** |
| **36** | **Backstab R5 (+90)** |
| **38** | **Sinister Strike R6 (+33)** |
| **40** | **Eviscerate R6** |

Rhythm worth encoding: from 14 to 40 the rogue gains a new rank every 2–6 levels; **both** generators (SS/Backstab) and the finisher step at 20–24, 28–32 and 36–38, so damage is roughly stepped rather than smooth.

---

## Table R2 — Gear bands (Rogue, leather throughout)

Armour class progression: **leather 1–40, no upgrade** (rogues never get mail). Prefer Agility/Stamina "of the Monkey"/"of the Tiger" suffixes on greens.

| Band | Item | Slot | Req lvl | Armour | Stats | Source | Conf |
|---|---|---|---:|---:|---|---|---|
| 13–17 | Gloves of the Fang | Hands (leather) | 14 | 47 | +2 Str, +3 Agi | Wailing Caverns, Druid of the Fang (~2.4 %) | V |
| 13–17 | Slick Deviate Leggings | Legs (leather) | ~14–16 | U | Agi/Sta | WC quest "Deviate Hides" | U |
| 13–17 | Tail Spike | Dagger | ~16 | – | dmg/speed U | Wailing Caverns | U |
| 18–22 | Cruel Barb | 1H sword | 19 | – | 30–57 dmg, 2.80 (15.54 dps), +12 AP | Deadmines, VanCleef | V |
| 18–22 | Assassin's Blade | Dagger | 19 | – | 20–39 dmg, 1.90 (15.53 dps), +3 Str, +4 Agi | SFK zone drop, bind-on-equip | V |
| 18–22 | Serpent's Shoulders | Shoulder (leather) | 18 | 59 | no stats (white quality) | WC, Lady Anacondra (~43 %) | V |
| 18–22 | Leggings of the Fang | Legs (leather) | 18 | 79 | +5 Str, +9 Agi, +4 Sta | WC | M |
| 18–22 | Armor of the Fang | Chest (leather) | 18 | 82 | +2 Str, +7 Spirit | WC | M |
| 23–27 | Shadowfang | 1H sword | 19 | – | 29–55 dmg, 2.70 (17.78 dps) | SFK, rare | M |
| 23–27 | Meteor Shard | Dagger | 24 (claimed) / 16 (claimed) — **conflict** | – | 17–33 dmg, 1.80 | SFK | U |
| 23–27 | green leather "of the Monkey" chest/legs | — | 23–27 | U | Agi/Sta | world drops | U |
| 28–32 | Toxic Revenger | Dagger | 27 | – | 27–51 dmg, 1.90 (20.53 dps) | Gnomeregan | M |
| 28–32 | Swinetusk Shank | Dagger | 30 | – | 24–45 dmg, 1.50 (23.0 dps) | Razorfen Kraul | M |
| 28–32 | Ebon Vise | Hands (leather) | 30 | 70 | +4 Str, +6 Agi, +8 Sta | SM Graveyard mobs | M |
| 33–37 | Revelosh's Spaulders | Shoulder (leather) | 36 | 84 | random suffix (aim "of the Monkey": +Agi/+Sta) | Uldaman, Revelosh | M |
| 33–37 | Fleshhide Shoulders | Shoulder (leather) | 37 | 95 | +5 Str, +6 Agi, +15 Sta | Razorfen Downs, Glutton | M |
| 33–37 | Arachnid Gloves | Hands (leather) | 37 | 79 | +10 Nature Resist + suffix | RFD, Tuten'kash | M |
| 38–40 | RFD/SM/Uldaman leather with Agi suffix (no single verified BiS) | — | 38–40 | U | U | — | U |

Notes for the sim: rogue weapons that are verified as *damage/speed* pairs are Cruel Barb (2.80), Assassin's Blade (1.90), Shadowfang (2.70), Toxic Revenger (1.90), Swinetusk Shank (1.50). Backstab requires a **dagger**, so the sim should force Assassin's Blade → Toxic Revenger/Swinetusk Shank as the main-hand dagger line while Cruel Barb/Shadowfang stay sword options for Sinister Strike.

---

## Cross-cutting gaps (be explicit in any downstream use)

1. **No verified per-item armour values** for most Warrior mail in bands 13–17, 23–27, 28–32, and for most Rogue leather in 13–17 and 38–40. Armour values quoted are only for the rows marked V/M.
2. **Item ids / required levels for WC quest rewards** (Dagmire Gauntlets, Slick Deviate Leggings, Tail Spike) were not machine-verifiable this run — item pages are JS-only.
3. **Taunt rage cost**, **Disarm cost/CD**, **Shield Wall cooldown (30 vs 5 min)**, **Charge cooldown**, and **Meteor Shard required level (16 vs 24)** remain contested across secondary sources. All four are marked, not silently picked.
4. Execute/Backstab/Sinister Strike per-rank numbers came from secondary DB extracts (foreverdb, vanilla-wiki mirror, warcraftdb) rather than wowhead tooltips; treat as medium.

## Sources

Kept
- https://www.wowhead.com/classic/spell=78/heroic-strike — Heroic Strike R1 tooltip (15 rage, next melee, +11), rank count 9.
- https://www.wowhead.com/classic/spell=7386/sunder-armor — Sunder Armor Classic tooltip: 15 rage, instant, −90, 30 s, 5 stacks.
- https://warcraft.wiki.gg/wiki/Heroic_Strike — Classic block: 15 rage / next melee / +11, "9 ranks".
- https://warcraft.wiki.gg/wiki/Sunder_Armor — Classic block ("5 ranks", −90 per application); also the source of the level-16 conflict note.
- https://foreverdb.net/spell/1608 — Heroic Strike rank/bonus table (R4 = +44 at 24).
- https://foreverdb.net/spell/7386 — Sunder Armor ranks 10/22/34/46/58 with 90/180/270/360/450.
- https://foreverdb.net/spell/1758 — Sinister Strike R3 at level 14 and the +3/+6/+10/+15/+22/+33/+52/+68 ladder.
- https://www.wowhead.com/classic/spell=72/shield-bash — Shield Bash ranks at 12/32/52.
- https://warcraft.wiki.gg/wiki/Warrior_abilities_(Classic) — Taunt 10, Shield Block 16, Disarm 18, Shield Wall 28, Charge 4/26/46, Execute 24/32/40.
- https://warcraft.wiki.gg/wiki/Rogue_abilities_(Classic) — Backstab 4/12/20/28/36/44/52/60, Gouge 6/18/32/46/60, Kick 12/26/42/58, Eviscerate ladder.
- https://classic.warcraftdb.com/data/spell/2098 — Eviscerate per-rank per-combo-point table; Kick R1 15 dmg / R2 30 dmg at 25 energy (spell 1766).
- https://www.wowclassicdatabase.com/item/cruel-barb — Cruel Barb: ilvl 24, req 19, 30–57 @2.80, +12 AP, VanCleef 15.7 %.
- https://www.wowhead.com/classic/item=5191/cruel-barb — same tooltip, independent corroboration.
- https://www.wowclassicdatabase.com/item/rockslicer + https://www.wowhead.com/classic/item=872/rockslicer — Rockslicer: req 16, 36–55 @3.30, +7 Str, Rhahk'Zor.
- https://www.wowclassicdatabase.com/item/assassins-blade — Assassin's Blade: ilvl 24, req 19, 20–39 @1.90, +3 Str/+4 Agi, BoE.
- https://www.wowclassicdatabase.com/item/herods-shoulder — Herod's Shoulder: req 37, mail shoulder, 196 armour, +6 Str/+15 Sta.
- https://www.wowclassicdatabase.com/item/scarlet-leggings — Scarlet Leggings: req 38, mail legs, 233 armour, +20 Str/+10 Sta.
- https://www.wowclassicdatabase.com/item/serpents-shoulders + https://www.wowhead.com/classic/item=5404/serpents-shoulders — Serpent's Shoulders: req 18, 59 armour, no stats.
- https://www.wowclassicdatabase.com/item/gloves-of-the-fang — Gloves of the Fang: req 14, 47 armour, +2 Str/+3 Agi.
- https://www.wowhead.com/classic/spell=2565/shield-block, /classic/spell=871/shield-wall, /classic/spell=676/disarm, /classic/spell=355/taunt — cost/cooldown/duration for the defensive kit (M confidence, page text quoted by a search extract).
- https://wowforevertalents.com/abilities/warrior/ — Shield Bash damage per rank (6/18/45).

Rejected / deprioritized
- `classicdb.ch` and `classicdb.com` — JS-rendered / HTTP 403 to every plain fetch attempt this run, despite being the requested 1.12 source.
- `wowhead.com/classic/item=…` and `/tooltip/item/<id>` — 404/JS-only; only `/spell=<id>` raw HTML is fetchable.
- `wowhead.com` comment pages surfaced by search — player chatter (e.g. "Meteor Shard at 24"), useful only as hints.
- Icy Veins Classic warrior/rogue levelling guides — no per-band gear tables; skipped.
- openai-provider "answers" for mid-level BiS lists — internally contradictory (gave two different stat blocks for Gloves of the Fang, one citing a /forever/ URL); used only where a real tooltip corroborated.
