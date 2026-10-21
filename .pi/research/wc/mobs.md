# Wailing Caverns — complete creature roster (vanilla 1.12 / Classic Era)

Scope: the **pre-Cataclysm instance**, zone id **718**, Classic Era = the 1.12 creature rows. Everything below is
what the instance spawns from the entrance cave to Mutanus' water at Dreamer's Rock.

Evidence tags: **D** = a fetched page states it · **I** = derived by arithmetic on a cited value/formula ·
**U** = unknown / unverified. An estimate is always tagged **I** and says "estimate" — never a sourced number.

## 0. Sources

| # | Source | What it gives | Tier |
|---|---|---|---|
| S1 | `https://database.emberveil.org/creature/<npc>` | 1.12 rows: level, **health, armour, per-swing damage, attack speed, attack power, DPS**, CC/school immunities, ability cadence + trigger, spawn count, movement, drop % | primary, D |
| S2 | `https://classicdb.ch/?npc=<npc>` | 1.12 reference DB: health, damage, armour (independent second opinion) | D |
| S3 | `https://nether.wowhead.com/classic/tooltip/npc/<npc>` | level band, elite flag, creature type/race, zone id | D |
| S4 | `https://warcraft.wiki.gg/wiki/<page>` | the six subzone names, the per-subzone denizens table, strategy prose | D |
| S5 | `https://www.wowhead.com/classic/guide/wailing-caverns-dungeon-strategy-wow-classic` | pull composition, boss order, pack notes | D |
| S6 | `https://warcraft.wiki.gg/index.php?title=<page>&action=raw` | raw wikitext (used to prove a page does / does not exist) | D |

S1 is the project's existing 1.12 creature source (`src/WowRoguelike.Core/Content.fs`, Scarlet Monastery block) and is
the **only** source that publishes **attack speed** — which is why it is primary here. Where S1 and S2 disagree, see §9.

## 1. Sub-areas — the six real map sections

From `warcraft.wiki.gg/wiki/Template:Wailing_Caverns_Subzones` (S4). There is no "Greymane's" anything.

| # | Subzone | Role in the route | Boss(es) placed here (S4 denizens table) |
|---|---|---|---|
| 1 | **Screaming Gully** | first big cavern after the entrance | **Lady Anacondra** |
| 2 | **Winding Chasm** | the long connecting passage | (no boss) |
| 3 | **Cavern of Mists** | side chamber off the chasm | (no boss) |
| 4 | **Pit of Fangs** | the snake pit + its river | **Lord Cobrahn**, **Lord Pythas**; rare **Kresh** patrols the river |
| 5 | **Crag of the Everliving** | the high crag | **Lord Serpentis**, **Verdan the Everliving**, optional **Skum**, rare **Deviate Faerie Dragon** |
| 6 | **Dreamer's Rock** | the far end, Naralex's island | **Mutanus the Devourer** (spawned by the Naralex event) |

The non-instanced cave in front of the portal is **Northern Barrens** (zone 17), not zone 718 — its mobs are in §7.

## 2. Full creature roster — instance (zone 718)

All rows S1. "lvl-A / lvl-B" = the per-level rows the page publishes (min level / max level); health/damage/armour are
**per level**, and a single value means the page publishes one level only. Speed = seconds between auto-attacks (D).
Every one of these has **aggro range 20 yd** and **calls for help 5 yd** (D, S1) unless noted.

### 2a. Beasts — raptors (family: Raptor)

| Creature | NPC | lvl | HP | melee | spd | armour | DPS | Ability (cadence · trigger) | spawns |
|---|---|---|---|---|---|---|---|---|---|
| Deviate Ravager | 3636 | 18–19 E | 1,251 / 1,347 | 45–59 / 47–63 | 2.00 | 791 / 826 | 25.9 / 27.5 | **Thrash** (3391) 15–20 s, on self | 16 |
| Deviate Guardian | 3637 | 18–19 E | 1,251 / 1,347 | 45–59 / 47–63 | 2.00 | 791 / 826 | 25.9 / 27.5 | — (no ability row; S4 says it calls for help) | 15 |
| Deviate Creeper | 3632 | 15–16 E | — / 1,068 | — / 40–53 | 2.00 | 685 / 721 | 21.9 / 23.1 | **Infected Wound** (3427) 6–10 s; **Sneak** on spawn | 0 in 718 (12 in Barrens) |
| Deviate Slayer | 3633 | 16–17 E | — / 1,158 | — / 43–56 | 2.00 | 721 / 756 | 23.1 / 24.8 | **Fatal Bite** (7938) at ≤40 % HP | 0 in 718 (14 in Barrens) |
| Deviate Stalker | 3634 | 15–17 E | — / 1,158 | — / 43–56 | 2.00 | 685 / 756 | 21.9 / 24.8 | **Faded** (6408) permanent stealth aura | 0 in 718 (4 in Barrens) |

`E` = Elite. Creeper/Slayer/Stalker are **not in the instance** — S3 puts them in zone 17 (Barrens) and S1 counts
their spawns in "The Barrens". They live in the cave complex in front of the portal.

### 2b. Beasts — snakes, crocolisk, wind serpents, turtle

| Creature | NPC | lvl | HP | melee | spd | armour | DPS | Ability (cadence · trigger) | spawns |
|---|---|---|---|---|---|---|---|---|---|
| Deviate Adder | 5048 | 18–19 E | 1,251 / 1,347 | 45–59 / 47–63 | 2.00 | 791 / 826 | 25.9 / 27.5 | **Poison Proc** (3616) passive, 15 % on melee | 9 |
| Deviate Viper | 5755 | 19–20 E | 1,123 / 1,210 | 47–63 / 50–66 | 2.00 | 826 / 861 | 27.5 / 29.1 | **Localized Toxin** (7947) 10–16 s, random enemy | 8 |
| Deviate Moccasin | 5762 | 20–21 E | 1,452 / 1,563 | 53–63 / 55–66 | 2.00 | 819 / 854 | 29.1 / 30.2 | — (none published) | U |
| Deviate Python | 8886 | 18–19 **Normal** | — / 180 | 26–35 / 28–37 | 2.00 | 791 / 826 | 15.3 / 16.2 | — (none published) | 12 |
| Deviate Crocolisk | 5053 | 18–19 **Normal** | 417 / 449 | 26–35 / 28–37 | 2.00 | 791 / 826 | 15.3 / 16.2 | **Tendon Rip** (3604) at ≤5 yd, −25 % speed 8 s, 20 s CD | 14 |
| Deviate Dreadfang | 5056 | 20–21 E | 1,452 / 1,563 | 50–66 / 52–69 | 2.00 | 861 / 897 | 29.1 / 30.2 | **Terrify** (7399) at ≤5 yd; +125 % run speed | 11 |
| Deviate Venomwing | 5756 | 20–21 E | 1,452 / 1,563 | 50–66 / 52–69 | 2.00 | 861 / 897 | 29.1 / 30.2 | **Toxic Spit** (7951) 3–5 s, ranged; 50 % also in melee | 12 |
| Kresh (rare, patrols) | 3653 | 20 E | 1,452 | 50–66 | 2.00 | **4,149** | 29.1 | none cast; aggro range 20 yd, **patrols a route** | 1 |

Kresh's armour is real and enormous: S1 says 4,149 and S2 independently says 4,140 — both agree, so it is **D**, not a
typo. It is ~5× every other level-20 creature and makes him the one trash-tier mob a level-24 party cannot burst.

### 2c. Elementals and oozes

| Creature | NPC | lvl | HP | melee | spd | armour | DPS | Ability (cadence · trigger) | spawns |
|---|---|---|---|---|---|---|---|---|---|
| Deviate Shambler | 5761 | 19–20 E | 1,212 / 1,299 | 44–58 / 47–62 | 2.00 | 684 / 712 | 25.6 / 27.1 | **Wild Regeneration** (7948) at ≤50 % HP: 3 s cast, 65 mana, heals 38–62 every 2 s for 20 s, Magic (dispellable) | 24 |
| Evolving Ectoplasm | 3640 | 17–18 E | 1,158 / 1,251 | 43–56 / 45–59 | 2.00 | 756 / 791 | 24.8 / 25.9 | — (none published) | 13 |
| Nightmare Ectoplasm | 5763 | 20–21 **Normal** | 494 / 531 | 29–39 / 31–41 | 2.00 | 819 / 854 | 17.1 / 17.8 | — ; **CC-immune: rooted** | U (spawned by the event) |
| Verdan the Everliving | 5775 | 21 E | 4,168 | **247–328** | **3.50** | 854 | 82.1 | **Grasping Vines** (8142) while target ≤8 yd | 1 |
| Deviate Lasher | 5055 | 19–20 **Normal** | 135 | 31–41 / 32–43 | 2.00 | 786 / 819 | 17.8 / 18.8 | **Wide Slash** (7342) while target ≤8 yd | 52 |

`Deviate Lasher` and `Deviate Crocolisk`/`Python` are **Normal**, not Elite — they die to one AoE. Verdan's
type on S1 is **Elemental**, so he cannot be polymorphed, sapped or hibernated. Verdan's CC immunity list (S1):
**fleeing, rooted, stunned, horrified** — i.e. no fear, no root, no stun.

### 2d. Humanoids

| Creature | NPC | lvl | HP | mana | melee | spd | armour | DPS | Abilities (see §3) | spawns |
|---|---|---|---|---|---|---|---|---|---|---|
| Druid of the Fang | 3840 | 19–20 E | 1,212 / 1,299 | 912 | 44–58 / 47–62 | 2.00 | 684 / 712 | 25.6 / 27.1 | Lightning Bolt, Druid's Slumber, Healing Touch, Serpent Form, Nature Channeling | **70** |
| Lady Anacondra | 3671 | 20 E | 2,165 | 490 | 68–91 | 2.00 | 712 | 39.8 | Lightning Bolt, Sleep, Healing Touch, Thorns Aura, Nature Channeling | 4 spawn points, 1 active |
| Lord Cobrahn | 3669 | 20 E | 2,165 | 490 | 94–125 | 2.00 | 712 | 54.9 | Lightning Bolt, Druid's Slumber, Healing Touch, Cobrahn Serpent Form, Poison | 1 |
| Lord Pythas | 3670 | 21 E | 2,320 | 510 | 81–107 | 2.00 | 741 | 47.0 | Lightning Bolt, Sleep, Healing Touch, Thunderclap | 1 |
| Lord Serpentis | 3673 | 21 E | 2,784 | 1,530 | 81–107 | 2.00 | 741 | 47.0 | Lightning Bolt, Sleep, Healing Touch (rank 6) | 1 |
| Skum | 3674 | 21 E | 3,908 | — | 61–81 | 2.00 | 854 | 35.5 | **Chained Bolt** (6254) while target ≤30 yd | 1 |
| Mutanus the Devourer | 3654 | 22 E | 4,496 | 400 | 74–98 | 2.00 | 887 | 42.8 | Naralex's Nightmare, Terrify, Thundercrack | 1 |
| Disciple of Naralex | 3678 | 20 E, **friendly** | 1,299 | 980 | 27–36 | 2.00 | 712 | 15.9 | none listed; escort NPC, immune to players, kills lock the run | 1 |
| Naralex | 3679 | 25, no rank, **friendly** | 713 | — | 32–43 | 2.00 | 1,037 | 18.7 | none; **asleep**, immune to players, never a kill target | 1 |

All four Fanglords + Skum + the Faerie Dragon + Verdan share CC immunity **fleeing, horrified** (S1); the Disciple is
immune to **charmed, fleeing, asleep**. Mutanus is immune to **horrified** and to the **Shadow** spell school.

### 2e. Critters and script-spawned creatures

| Creature | NPC | lvl | HP | melee | spd | armour | Notes | spawns |
|---|---|---|---|---|---|---|---|---|
| Biletoad | 3835 | 1 Critter | 8 | 1–1 | 8.00 | 20 | **Biletoad Infection** (10251) on death (75 %): sacrifices itself, −2 Stamina to everything within 5 yd for 3 min | 11 |
| Serpentbloom Snake | 3680 | 1 Beast | 8 | 9–12 | 8.00 | 20 | summoned by the **Serpentbloom Trap** object (spell 6195) when a Serpentbloom node is looted; no XP | via objects |

## 3. Every ability, with numbers

All S1 (`database.emberveil.org/spell/<id>`). "CD" = the ability's own cooldown; "cadence" = the interval S1 records
on the NPC that casts it.

| Spell (id) | Cast | Cost | Range | Duration | Numbers | Notes |
|---|---|---|---|---|---|---|
| Lightning Bolt (9532) | 3.0 s | 90 mana | 40 yd | — | **64–86 Nature** | the shared WC caster nuke; `Cannot be used while shapeshifted` |
| Druid's Slumber (8040) | 2.5 s | 60 mana | 30 yd | 15 s | sleep; **Magic** | cancelled by damage; one target at a time |
| Sleep (700) | 1.5 s | 60 mana | 30 yd | 20 s | sleep; **Magic** | cancelled by damage; one target at a time |
| Healing Touch R3 (5187) | 2.5 s | 110 mana | 40 yd | — | **195–243** heal (+1.9/level) | rank 3 of 11, requires level 14 |
| Healing Touch R4 (5188) | 3.0 s | 185 mana | 40 yd | — | **363–445** heal | the rank S1 records on **Lord Cobrahn** |
| Healing Touch R6 (6778) | 3.5 s | 335 mana | 40 yd | — | **742–894** heal | requires level 32, cast by the level-21 Serpentis — anomalous, see §9 |
| Thorns Aura (8148) | Instant | — | 30 yd | 60 s, 1 s CD | **20 Nature** to any melee attacker | area aura, party-wide |
| Thunderclap (8147) | Instant | — | 10 yd | 10 s | **71–79 Nature** + time-between-attacks **+43 %** + move speed **−40 %** | Magic |
| Cobrahn Serpent Form (7965) | Instant | — | self | 5 min | **+50 damage dealt**, **time between attacks +43 %** | triggered at **≤30 % health**, no repeat |
| Serpent Form (8041) | Instant | — | self | 10 s | **+25 Physical**, **cannot cast spells** | triggered at **≤30 % health**; the silence is the point |
| Poison (17330) | Instant | — | 30 yd | 20 s | **20 Nature every 2 s** (200 total) | Poison, 2 s cadence |
| Poison Proc (3616) | passive | — | self | — | **15 %** chance on melee → triggers 744 | |
| Naralex's Nightmare (7967) | 2.0 s | — | 30 yd | 15 s | sleep; **Magic** | one target at a time; also applies Charm 100 + invisibility detection |
| Terrify (7399) | Instant | — | 5 yd | 4 s | **fear**; Magic | one target at a time |
| Thundercrack (8150) | Instant | — | 10 yd dmg / 20 yd stun | 2.5 s | **94–106 Nature** + **2.5 s stun** | Magic; Mutanus' only AoE |
| Grasping Vines (8142) | 1.0 s | — | 10 yd | 10 s | **150–250 Physical** + knockdown + **root 10 s** | Magic dispel type, mechanic "rooted" |
| Wild Regeneration (7948) | 3.0 s | 65 mana | self | 20 s | **38–62 heal every 2 s** | Magic — dispellable |
| Chained Bolt (6254) | 1.8 s | — | 30 yd | — | **28–32 Nature**, **3 targets** | Skum's only ability |
| Tendon Rip (3604) | next melee | — | 5 yd | 8 s | **−25 % move speed** | 20 s cooldown |
| Wide Slash (7342) | next melee | — | 10 yd cone | — | weapon damage **+5** | cone |
| Toxic Spit (7951) | 2.5 s | — | 30 yd | — | **38–62 Nature** | Poison dispel type |
| Localized Toxin (7947) | Instant | — | 5 yd | 60 s | **1 Nature every 5 s** (+0.3/level) | Poison |
| Infected Wound (3427) | next melee | — | melee | 5 min | **+3 Physical damage taken** (+0.25/level) | Disease |
| Fatal Bite (7938) | next melee | — | melee | — | **drain 50 HP**, heals caster up to 100 | requires target **<20 % health** |
| Thrash (3391) | Instant | — | self | — | **+2 extra attacks** | 15–20 s cadence |
| Faded (6408) | Instant | — | self | until cancelled | stealth, **−30 % speed** | Deviate Stalker |
| Sneak (22766) | Instant | — | self | until cancelled | stealth, **−50 % speed** | out of combat only |
| Clone (7952) | 2.5 s | — | self | — | summons 1 **Cloned Ectoplasm** | 6–17 s cadence |
| Biletoad Infection (10251) | Instant | — | 5 yd | 3 min | **−2 Stamina**, 5 yd radius, **instakills the caster** | on death, 75 % |
| Nature Channeling (13236) | channelled | — | self | — | no numbers published (**U**) | out of combat, every 2 s; cancels on any action |
| Lightning Breath (25008) | Instant | 50 Focus | 20 yd | — | **21–23 Nature** | pet-training skill, not used by the mobs in combat |
| Serpentbloom Snake (6195) | Instant | — | object | — | summons 1 Serpentbloom Snake | cast by the Serpentbloom Trap |

## 4. Bosses in the order a group meets them

Order from S5 (the wowhead walkthrough) with the S4 subzone each stands in. Optional fights are marked.

### 4.1 Kresh — rare, optional, Pit of Fangs river
| Field | Value | Tag |
|---|---|---|
| Level / rank | 20 Elite, Beast (Turtle) | D (S1, S3) |
| Health | 1,452 | D (S1) |
| Melee | 50–66, 2.00 s, 29.1 DPS | D (S1) |
| Armour | **4,149** (S1) / **4,140** (S2) | D — two sources agree |
| Movement | **Patrols a route** along the Pit of Fangs river | D (S1) |
| Abilities | **none cast in combat** (only tameable-pet skills Bite R3 / Shell Shield R1 at 10 % each) | D (S1) |
| Drops that matter | Deviate Hide 80 %, Worn Turtle Shell Shield 50 %, **Kresh's Back** 50 %, Thick Scaly Tail 30 % | D (S1) |
| Quest use | none | D |

### 4.2 Lady Anacondra `<Fanglord>` — Screaming Gully (first boss of the route)
| Field | Value | Tag |
|---|---|---|
| Level / rank | 20 Elite, Humanoid | D (S1, S2, S3) |
| Health | 2,165 (S1) / 2,214 (S2) | D — see §9 |
| Mana | 490 | D (S1) |
| Melee | 68–91, 2.00 s, 39.8 DPS (S1) / 154–183 (S2) | D — see §9 |
| Armour | 712 (S1 **and** S2 agree) | D |
| **Lightning Bolt** | 3 s cast, 64–86 Nature, every 2 s | D (S1) |
| **Sleep** | 1.5 s cast, up to 20 s, Magic, every 12–25 s, random non-tank, one target | D (S1) |
| **Healing Touch** | 2.5 s cast, 195–243, fires on the most-injured ally within 40 yd, every 15–26 s | D (S1) |
| **Thorns Aura** | 20 Nature to melee attackers, 60 s, refreshed every 50 s | D (S1) |
| **Nature Channeling** | out of combat, every 2 s, no published effect | D (S1) / U (numbers) |
| Adds | she shares the room with Deviate Ravager/Guardian and Druid of the Fang; **raptors call for help when nearly dead** (S4 prose) | D |
| CC immunity | fleeing, horrified | D (S1) |
| **Not found** | no Serpent Form, no enrage, no summon, no pet | D (S1 has no such row) |

### 4.3 Lord Cobrahn `<Fanglord>` — Pit of Fangs
| Field | Value | Tag |
|---|---|---|
| Level / rank | 20 Elite, Humanoid | D (S1–S3) |
| Health | 2,165 (S1) / 2,279 (S2) | D — see §9 |
| Mana | 490 | D (S1) |
| Melee | 94–125, 2.00 s, 54.9 DPS (S1) / 145–172 (S2) | D — see §9 |
| Armour | 712 | D |
| **Lightning Bolt** | 3 s, 64–86, every 2 s; stops at ≤30 % health | D (S1) |
| **Druid's Slumber** | 2.5 s, 15 s sleep, Magic, every 13–20 s, random non-tank | D (S1) |
| **Healing Touch** | S1 records **rank 4 (5188): 363–445**, 3 s cast. (S2 lists 5187 R3 195–243 for him) | D — see §9 |
| **Cobrahn Serpent Form** | Instant at **≤30 % health**: **+50 damage dealt**, **time between attacks +43 %** (slower, not faster), 5 min, not dispellable by the player | D (S1) |
| **Poison** | 20 Nature every 2 s for 20 s, applied from 30 % health, 2 s cadence | D (S1) |
| Adds | **3× Deviate Python** (level 18–19, **non-elite**, 180 HP, AoE fodder) sit with him | D (S5) |
| **Not found** | **Lightning Shield does not exist on Cobrahn** — no such row in S1 or S2; his shield-like effect is nothing of the sort. No enrage. | D (absence in S1/S2) |

### 4.4 Deviate Faerie Dragon — rare, optional, Crag of the Everliving
| Field | Value | Tag |
|---|---|---|
| Level / rank | 20 **Rare Elite**, Dragonkin | D (S1, S3) |
| Health | 1,452 | D (S1) |
| Melee | 50–66, 2.00 s, 29.1 DPS | D (S1) |
| Armour | 819 | D (S1) |
| Abilities | **none** | D (S1) |
| Movement | stands still | D (S1) |
| Spawns | 1 (may simply not be up) | D (S1) |
| Pull | S5: "a **4-mob pull, all elites**" — the dragon plus **2 Druids of the Fang** plus one more elite. CC one Druid, burn the other. | D (S5) |
| Drops that matter | (rare-quality caster/leather pieces; no quest use) | D (S5) |

### 4.5 Lord Pythas `<Fanglord>` — Pit of Fangs
| Field | Value | Tag |
|---|---|---|
| Level / rank | 21 Elite, Humanoid | D (S1–S3) |
| Health | 2,320 (S1) / 2,409 (S2) | D — see §9 |
| Mana | 510 | D (S1) |
| Melee | 81–107, 2.00 s, 47.0 DPS (S1) / 154–183 (S2) | D — see §9 |
| Armour | 741 (S1) / 712 (S2) | D — see §9 |
| **Lightning Bolt** | 3 s, 64–86, every 2 s | D (S1) |
| **Thunderclap** | Instant, **71–79 Nature** AoE 10 yd, **+43 % time between attacks**, **−40 % move speed**, 10 s, every 6–11 s | D (S1) |
| **Sleep** | 1.5 s, up to 20 s, every 12–20 s, random non-tank | D (S1) |
| **Healing Touch** | 2.5 s, 195–243, most-injured ally, every 20–25 s | D (S1) |
| Adds | **1× Druid of the Fang + 1× Deviate Shambler** (S1/S4). S4 strategy explicitly suggests **polymorphing the druid**. | D |
| **Not found** | **no Voidwalker, no summon, no warlock pet of any kind.** Pythas is a druid, not a warlock — S1 has no summon row and S2 lists only 9532/8147/700/5187. The "pet Dreadcoil" claim in a secondary summary is **not** in S1 or S2 (**U**, treat as false). | D (absence in S1/S2) |
| CC immunity | fleeing, horrified | D (S1) |

### 4.6 Skum — optional boss, Crag of the Everliving
| Field | Value | Tag |
|---|---|---|
| Level / rank | 21 Elite, Beast | D (S1, S3) |
| Health | **3,908** | D (S1) |
| Melee | 61–81, 2.00 s, 35.5 DPS | D (S1) |
| Armour | 854 | D (S1) |
| **Chained Bolt** | 1.8 s cast, 30 yd, **28–32 Nature to up to 3 targets** — S5: spread the melee out | D (S1, S5) |
| Immunities | **Spell school: Nature**; CC: fleeing, horrified | D (S1) |
| Guards | S4: guarded by **two Druids of the Fang** plus nearby trash | D |
| Quest use | none; optional but his loot is worth it (S5) | D |

### 4.7 Lord Serpentis `<Fanglord>` — Crag of the Everliving (last of the four)
| Field | Value | Tag |
|---|---|---|
| Level / rank | 21 Elite, Humanoid | D (S1–S3) |
| Health | 2,784 (S1) / 2,770 (S2) | D — see §9 |
| Mana | **1,530** (3× the other Fanglords) | D (S1) |
| Melee | 81–107, 2.00 s, 47.0 DPS (S1) / 154–183 (S2) | D — see §9 |
| Armour | 741 (S1) — S2 publishes no armour row for him | D (S1) |
| **Lightning Bolt** | 3 s, 64–86, every 2 s | D (S1) |
| **Sleep** | 1.5 s, up to 20 s, every 10–20 s, random non-tank | D (S1) |
| **Healing Touch rank 6 (6778)** | 3.5 s cast, **742–894**, fires on the most-injured ally within 40 yd, every 120 s | D (S1) |
| Adds | **none** — S5: "Serpentis can be pulled without any additional trash, making this encounter easier than the rest" | D (S5) |
| **Not found** | **no "Chains of Nizraeil", no totem, no add behaviour, no script.** S1's ability list for him is exactly three spells; S4's Abilities section is empty. Any such mechanic is a fabrication. | D (absence in S1/S4) |
| CC immunity | fleeing, horrified | D (S1) |

### 4.8 Verdan the Everliving — optional, Crag of the Everliving
| Field | Value | Tag |
|---|---|---|
| Level / rank | 21 Elite, **Elemental** (bog beast) | D (S1, S3) |
| Health | **4,168** (S1) | D |
| Melee | **247–328**, **3.50 s**, 82.1 DPS | D (S1) |
| Armour | 854 | D (S1) |
| **Grasping Vines** | 1 s cast, 10 yd radius: **150–250 Physical**, **knocks the target down**, **roots for 10 s**; fires whenever his target is within 8 yd | D (S1) |
| Immunities | CC: **fleeing, rooted, stunned, horrified** — cannot be feared, rooted or stunned | D (S1) |
| Movement | stands still, 67 % walk speed | D (S1) |
| Adds / script | **none** — no waves, no dialogue, no trigger | D (S1, S4) |
| Optional? | Yes for the four Fanglord kills, but S5 says he "should always be killed to **unlock the waterfall shortcut behind him**" — so killing him is a **navigation** shortcut, not a clear requirement | D (S5) |
| Drops | Living Root 33 %, Seedcloud Buckler 33 %, Sporid Cape 33 % | D (S1) |
| **Not found** | no knockback beyond the Grasping Vines knockdown; no enrage; no second ability | D |

### 4.9 The Naralex event → Mutanus the Devourer — Dreamer's Rock (final boss)
Script steps are in `.pi/research/wc/quests-and-scripts.md` §C1; the creature numbers:

| Field | Value | Tag |
|---|---|---|
| Level / rank | 22 Elite, Humanoid (a murloc) | D (S1–S3) |
| Health | **4,496** (S1) / 4,555 (S2) | D — see §9 |
| Mana | 400 | D (S1) |
| Melee | 74–98, 2.00 s, 42.8 DPS (S1) / 179–213 (S2) | D — see §9 |
| Armour | **887** (S1) — S2 publishes no armour row | D (S1) |
| Immunities | **Spell school: Shadow** (so no shadow magic lands on him); CC: **horrified** | D (S1) |
| **Naralex's Nightmare (7967)** | 2 s cast, 30 yd, **sleep up to 15 s**, Magic, one target, fires on **a random enemy other than his main target** every **18–25 s** | D (S1) |
| **Terrify (7399)** | Instant, 5 yd, **fear 4 s**, Magic, one target, on a random non-tank every **12–18 s** | D (S1) |
| **Thundercrack (8150)** | Instant, **94–106 Nature** to everything within 10 yd, **2.5 s stun** within 20 yd, Magic, every **10–12 s** | D (S1) |
| Adds | none of his own — the escort waves (3× Deviate Moccasin, then many Nightmare Ectoplasm) come first | D (S1 / scripts.md) |
| **Not found** | **no "consume", no swallow, no knockback.** S1's ability list is exactly those three spells; the wiki's ability section lists the same three. | D (absence in S1/S4) |
| Drops | **Glowing Shard 100 %**, Deviate Hide 80 %, Slime-encrusted Pads 33 %, Mutant Scale Breastplate 33 %, Deep Fathom Ring 33 % | D (S1) |

## 5. Optional / rare summary

| Name | Where | Level | Health | Why you'd kill it |
|---|---|---|---|---|
| **Kresh** | Pit of Fangs river (patrol) | 20 Elite | 1,452 | Kresh's Back (50 %), Deviate Hide (80 %) |
| **Skum** | Crag of the Everliving pen, 2 Druid of the Fang guards | 21 Elite | 3,908 | loot only, no quest |
| **Deviate Faerie Dragon** | Crag of the Everliving, on the way to Serpentis | 20 Rare Elite | 1,452 | rare loot; may not be up |
| **Verdan the Everliving** | Crag of the Everliving, own ledge | 21 Elite | 4,168 | unlocks the waterfall shortcut |
| Kresh/Skum/Verdan/Faerie Dragon are the objective of **no quest** (`.pi/research/wc/quests-and-scripts.md` §G4). | | | | |

## 6. Room-by-room spawn plan

Counts tagged **D** are the spawn-row counts S1 publishes for zone 718. The **split between rooms is an
estimate (I)** — S1 gives the total per creature, not the room, and the wiki denizens table (S4) gives the *membership*
of each room but no counts. Use the totals as hard, the per-room numbers as a defensible starting point.

### 6.1 Screaming Gully (first cavern)
| Mob | Count | Tag |
|---|---|---|
| **Lady Anacondra** | 1 (she has 4 spawn points, 1 active) | D |
| Deviate Ravager | ~4 | I |
| Deviate Guardian | ~4 | I |
| Druid of the Fang | ~4 | I |
| Evolving Ectoplasm | ~2 | I |
Membership per S4: Anacondra, Deviate Guardian, Deviate Ravager, Druid of the Fang, Evolving Ectoplasm.

### 6.2 Winding Chasm (connecting passage) + Cavern of Mists (side chamber)
| Mob | Count | Tag |
|---|---|---|
| Deviate Lasher | ~12 (of 52) | I |
| Deviate Shambler | ~6 (of 24) | I |
| Druid of the Fang | ~10 (of 70) | I |
| Deviate Crocolisk | ~4 (of 14) | I |
| Deviate Dreadfang | ~3 (of 11) | I |
| Biletoad (critter) | ~5 (of 11) | I |
S4 lists no boss for either subzone.

### 6.3 Pit of Fangs (the snake pit + its river)
| Mob | Count | Tag |
|---|---|---|
| **Lord Cobrahn** | 1 | D |
| **Lord Pythas** | 1 | D |
| **3× Deviate Python** (Cobrahn's pre-pull) | 3 | D (S5) |
| Deviate Adder | ~5 (of 9) | I |
| Deviate Viper | ~5 (of 8) | I |
| Deviate Python | ~8 (of 12) | I |
| Deviate Crocolisk | ~6 (of 14) | I |
| Kresh (patrol) | 1 | D |
| Druid of the Fang | ~10 | I |
S4 denizens: river = Kresh, Deviate Crocolisk, Druid of the Fang; pit = Cobrahn, Pythas, Deviate Adder, Deviate Python, Deviate Viper.

### 6.4 Crag of the Everliving
| Mob | Count | Tag |
|---|---|---|
| **Lord Serpentis** | 1 | D |
| **Verdan the Everliving** | 1 | D |
| **Skum** + 2 Druid of the Fang guards | 1 + 2 | D |
| **Deviate Faerie Dragon** (rare) | 1 | D |
| Deviate Lasher | ~20 (of 52) | I |
| Deviate Shambler | ~10 (of 24) | I |
| Deviate Venomwing | ~6 (of 12) | I |
| Deviate Dreadfang | ~4 (of 11) | I |
| Druid of the Fang | ~18 (of 70) | I |
S4 denizens: Skum, Serpentis, Verdan, Deviate Faerie Dragon (rare); Dreadfang, Lasher, Shambler, Venomwing, Druid of the Fang.

### 6.5 Dreamer's Rock (the far end)
| Mob | Count | Tag |
|---|---|---|
| **Disciple of Naralex** (escort NPC) | 1 | D |
| **Naralex** (asleep, friendly) | 1 | D |
| **Mutanus the Devourer** (spawned by the event) | 1 | D |
| Escort wave 1: **3× Deviate Moccasin** (level 20 elite) | 3 | D (scripts.md §C1 step 9) |
| Ritual wave 2: **many Nightmare Ectoplasm** (level 20, non-elite) | 8–12, exact count **U** | I (count), D (that it is uncounted) |
| Evolving Ectoplasm | ~6 (of 13) | I |
S4 denizens: Mutanus; Deviate Moccasin, Evolving Ectoplasm, Nightmare Ectoplasm; Naralex (neutral).

### 6.6 Totals sanity check
S1 spawn rows in zone 718: Druid of the Fang 70 · Deviate Lasher 52 · Deviate Shambler 24 · Deviate Ravager 16 ·
Deviate Guardian 15 · Deviate Crocolisk 14 · Evolving Ectoplasm 13 · Deviate Venomwing 12 · Deviate Python 12 ·
Deviate Dreadfang 11 · Biletoad 11 · Deviate Adder 9 · Deviate Viper 8 · 1 each of Kresh, Skum, Verdan, Cobrahn,
Pythas, Serpentis, Disciple, Naralex, Faerie Dragon, Mutanus. That is ~276 hostile spawn rows + 11 critters.
**Caveat (I):** the 70 / 52 / 24 figures look high for a level-17–22 dungeon; if your sim wipes the party early,
scale the three big trash totals down rather than the small ones, which match the guide's pack sizes.

## 7. The entrance cave (Northern Barrens, zone 17) — NOT the instance

S1/S3 put these outside the portal. They matter only if the sim starts at the cave mouth.

| Creature | NPC | lvl | Type | Health | melee | armour | Notes |
|---|---|---|---|---|---|---|---|
| Deviate Creeper | 3632 | 15–16 E | Beast (Raptor) | 1,068 | 40–53 | 685–721 | Infected Wound, Sneak; 12 spawns |
| Deviate Slayer | 3633 | 16–17 E | Beast (Raptor) | 1,158 | 43–56 | 721–756 | Fatal Bite at ≤40 % HP; 14 spawns |
| Deviate Stalker | 3634 | 15–17 E | Beast (Raptor) | 1,158 | 43–56 | 685–756 | Faded (permanent stealth); 4 spawns |
| Deviate Lurker | 3641 | 16–17 E | Beast (threshadon) | 1,158 | 43–56 | 721–756 | 8 spawns |
| Devouring Ectoplasm | 3638 | 16–17 E | Elemental (ooze) | 1,158 | 43–56 | 721–756 | **Clone** (7952); 10 spawns; drops Wailing Essence |
| Cloned Ectoplasm | 5780 | 16–17 E | Elemental (ooze) | 1,158 | 40–53 | 721–756 | summoned by the above |
| Mad Magglish | (goblin) | — | Humanoid | U | U | U | stealthed, 3 fixed spawn points, drops 99-Year-Old Port (scripts.md §C3) |
| Boahn, Trigore the Lasher | — | — | — | U | U | U | rares in the caves, not the instance (scripts.md §C3) |

## 8. Creatures that are **not** in Wailing Caverns (do not spawn them)

Several names in the request exist in the game but in a **different** dungeon, or not at all. Evidence is the raw
wikitext (S6) plus S3's zone field.

| Name | Verdict | Evidence |
|---|---|---|
| **Murk Slitherer** (5224) | **Temple of Atal'Hakkar**, Swamp of Sorrows, level 44–46. Not WC. | S6 `Murk_Slitherer` |
| **Murk Spitter** (5225) | **Temple of Atal'Hakkar**. Not WC. | S6 `Murk_Spitter` |
| **Murk Worm** (5226) | **Temple of Atal'Hakkar**, level 47–48 elite rock borer. Not WC. | S6 `Murk_Worm` |
| **Murk Jugin** | **Does not exist** — `action=raw` returns HTTP 404. | S6 `Murk_Jugin` |
| **Saturated Ooze** (5228) | **Temple of Atal'Hakkar**, level 47–48 elite. Not WC. | S6 `Saturated_Ooze` |
| **Fungal Ooze** (5235) | **Temple of Atal'Hakkar**, level 45–46 elite. Not WC. | S6 `Fungal_Ooze` |
| **Devouring Ectoplasm** (3638) | real, but **outside the portal** in Northern Barrens (§7), not in the instance | S1/S3 |
| **Cloned Ectoplasm** (5780) | real, summoned by Devouring Ectoplasm outside the portal | S6 `Cloned_Ectoplasm` |
| **Deviate Lurker** (3641) | real, **outside the instanced portion** (threshadon, Barrens) | S6 `Deviate_Lurker` |
| **Deviate Horror** (3642) | **not in the world on this patch** — S1 marks it "Nothing spawns or summons it" | S1 |
| **Deviate Nightmare** (5914) | **not in the world on this patch**; also flagged **Friendly**, grants no XP | S1 |
| **Naralex's Nightmare** as a *creature* | it is a **spell** (7967), not a mob | S1 |
| "Chains of Nizraeil" (Serpentis) | **does not exist** — no ability row on any source | S1/S4 |
| "Lightning Shield" (Cobrahn) | **does not exist** on him | S1/S2 |
| "Voidwalker" / summon (Pythas) | **does not exist** | S1/S2 |

## 9. Where the sources disagree (and what this file picked)

| # | Fact | S1 (emberveil 1.12) | S2 (classicdb 1.12) | Picked | Why |
|---|---|---|---|---|---|
| 1 | Anacondra melee | 68–91 | 154–183 | **S1** | S1 is per-level and pairs the damage with the armour (712) and speed (2.00) it also publishes; S2 gives one number for the whole level range and no speed. |
| 2 | Cobrahn melee | 94–125 | 145–172 | **S1** | same; and S1's ordering (Cobrahn > Anacondra) is the one the Fanglord health totals support. |
| 3 | Pythas / Serpentis melee | 81–107 | 154–183 | **S1** | same. |
| 4 | Mutanus melee | 74–98 | 179–213 | **S1** | same. |
| 5 | Anacondra health | 2,165 | 2,214 | **S1** | both 1.12; S1's number is the one its own per-level row publishes. Difference is 2 %. |
| 6 | Cobrahn health | 2,165 | 2,279 | **S1** | as above. |
| 7 | Pythas health | 2,320 | 2,409 | **S1** | as above. |
| 8 | Serpentis health | 2,784 | 2,770 | **S1** | as above. |
| 9 | Mutanus health | 4,496 | 4,555 | **S1** | as above. |
| 10 | Pythas armour | 741 | 712 | **S1** | S1 keys armour to the same (level, class) row as the damage it publishes. |
| 11 | Cobrahn's heal | **R4 (5188) 363–445** | R3 (5187) 195–243 | **S1** | S1's own ability row names 5188; S2's NPC page lists 5187. |
| 12 | Serpentis' heal | R6 (6778) **742–894**, 3.5 s, **120 s** cadence | R6 (6778), no cadence | **S1** | S1 adds the cadence; the rank agrees. Note the anomaly: a level-21 mob casting a level-32 heal. |
| 13 | Thundercrack damage | **94–106** | (not published) | **S1** | S1's number is the spell row and matches the wiki's lower bound 94. Older wiki text says 94–107, one guide says 106–118; 94–106 is the primary spell row. |
| 14 | Mutanus' Shadow immunity | **yes**, spell school Shadow | (not published) | **S1** | S4's prose says "immune to Shadow spells" too. |
| 15 | Attack speed | **published for every creature** (2.00 s for nearly all, 3.50 s for Verdan, 8.00 s for critters) | never published | **S1** | this is the reason S1 is the primary source. |
| 16 | Kresh armour | 4,149 | 4,140 | **both — agree** | the two 1.12 DBs agree within 0.2 %, so it is real, not a typo. |
| 17 | Level fields on warcraft.wiki.gg infoboxes ("8-30 Elite") | 20 / 21 / 22 | 20 / 21 / 22 | **S1/S2** | the wiki infobox levels are post-7.3.5 retail scaling and are wrong for Classic. |

## 10. What could not be sourced (**U**) — state these as unknown, not as numbers

| # | Unknown | Notes |
|---|---|---|
| 1 | Exact per-room spawn **counts** | S1 gives the zone total per creature and S4 gives room membership; the split in §6 is an estimate (I). |
| 2 | Deviate Moccasin and Nightmare Ectoplasm spawn rows | S1 publishes no "Found in" row for either; both are event-spawned. |
| 3 | The exact number of Nightmare Ectoplasm in ritual wave 2 | S4 says "many"; no count anywhere. |
| 4 | Nature Channeling (13236) effect | the spell row has a Dummy aura and no numbers. |
| 5 | Whether the Disciple of Naralex can be killed in Classic Era | retail hotfixed it in 2013; Classic Era behaviour unverified (scripts.md §C1 step 15). |
| 6 | Mad Magglish / Boahn / Trigore stat rows | outside the instance; not fetched. |
| 7 | Respawn timers in a live instance | S1 gives 2 h for normal spawns and 10 h for bosses, but "in a dungeon, a reset instance has it back at once". |
| 8 | Deviate Creeper/Slayer/Stalker ability damage | only the spell rows (Infected Wound +3 damage taken, Fatal Bite drain 50) are known, not the observed hits. |

## 11. Spawn list for the sim — entrance to final boss, in order

```
ENTRANCE (instance portal, zone 718)
  → Screaming Gully    : Lady Anacondra (20E) + Deviate Ravager/Guardian + Druid of the Fang + Evolving Ectoplasm
  → Winding Chasm      : Deviate Lasher/Shambler/Crocolisk/Dreadfang + Druid of the Fang + Biletoad
  → Cavern of Mists    : side chamber, same trash pool
  → Pit of Fangs river : Kresh (20E, patrolling, 4149 armour)
  → Pit of Fangs       : Lord Cobrahn (20E) + 3x Deviate Python
                         Deviate Adder / Deviate Viper / Deviate Python / Deviate Crocolisk
  → Crag of the Everliving : Lord Pythas (21E) + 1 Druid of the Fang + 1 Deviate Shambler
                         Skum (21E, 3908 HP, Chained Bolt) + 2 Druid of the Fang
                         Deviate Faerie Dragon (20 rare elite, 4-mob pull)
                         Lord Serpentis (21E) + NO adds
                         Verdan the Everliving (21E, 4168 HP, 247-328 @ 3.5 s, Grasping Vines)
  → Dreamer's Rock     : Disciple of Naralex (friendly escort)
                         wave 1: 3x Deviate Moccasin (20E)
                         wave 2: many Nightmare Ectoplasm (20 normal)
                         MUTANUS THE DEVOURER (22E, 4496 HP, Thundercrack/Terrify/Naralex's Nightmare)
                         → Naralex wakes; every remaining mob despawns
```
