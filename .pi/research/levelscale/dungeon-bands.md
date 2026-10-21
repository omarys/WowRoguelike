# Classic 5-Man Dungeon Level Bands

Scope: vanilla 1.12 / Classic Era 1.15 five-player dungeons — Ragefire Chasm, The Deadmines, Wailing Caverns, Shadowfang Keep, The Stockade, Blackfathom Deeps, Razorfen Kraul, and the four Scarlet Monastery wings.

**Primary numeric source for creature and boss levels:** Wowhead Classic Era zone pages (`wowhead.com/classic/zone=<id>`), which embed the live 1.15.8 creature table (`minlevel`/`maxlevel`/`classification` per NPC). Cross-checked against the Wowhead NPC tooltip endpoint for a sample of bosses. Wowhead's live "Classic" data environment is **1.15.8**, not 1.12; see the 1.12 note at the end.

**Derivation rules used (stated once, applied uniformly):**
- **enter level** = the instance-portal level gate (the character level the game blocks below), taken from documented tables — *not* a recommended level.
- **entrance trash** = level range of the lowest-tier instance mobs you meet in the first one or two pulls (named mobs given in each section, so you can check me).
- **deepest trash** = level range of the highest-tier non-boss mobs near the final encounter.
- **derived MIN** = entrance-trash floor.
- **derived MAX** = highest boss level in the dungeon.
- **derived MEDIAN** = highest boss level − 1 (rule: a group one level under the final boss has the full toolkit and out-levels the last pull). Cross-checked against the documented recommendation; where the two disagree by more than ~2 levels I say so in the section.
- **rooms** = rough count of distinct combat spaces (my own count off the in-game maps; **no source states a room count** — treat as an estimate for pacing only).

---

## Summary table

| Dungeon | Enter lvl | Entrance trash | Deepest trash | Boss levels | Documented recommendation | Derived MIN | Median | MAX | Bosses | Rooms (est.) |
|---|---|---|---|---|---|---|---|---|---|---|
| Ragefire Chasm | 10 | 13–15 | 13–15 | 16, 16, 16, 16 | 13–18 (Wowhead) / 15–21 (wiki infobox) | 13 | 15 | 16 | 4 | ~5 |
| The Deadmines | 10 | 16–18 | 19–20 | 19, 20, 20, 20, 20, 20, 20, 21 (+rare 19) | 18–23 | 16 | 20 | 21 | 8 (+1 rare) | ~8 |
| Wailing Caverns | 10 | 17–19 | 20–21 | 20, 20, 20, 21, 21, 21, 21, 22 (+rare 20) | 15–25 | 17 | 21 | 22 | 8 (+1 rare) | ~8 |
| Shadowfang Keep | 14 | 18–21 | 23–25 | 20, 22, 24, 24, 24, 25, 25, 26 (+rare 25) | 22–30 | 18 | 25 | 26 | 8 (+1 rare) | ~10 |
| The Stockade | 15 | 23–24 | 25–26 | 24, 26, 27, 28, 29 (+rare 26) | 22–30 | 23 | 28 | 29 | 5 (+1 rare) | ~7 |
| Blackfathom Deeps | 15 | 22–25 | 26–27 | 25, 25, 26, 26, 27, 28 (+28 quest, +26 quest) | 24–32 | 22 | 27 | 28 | 6 (+2 quest) | ~7 |
| Razorfen Kraul | 25 | 24–26 | 30–32 | 28, 30, 30, 31, 32, 32, 33, 33 (+rare 32) | 30–40 (Wowhead) / 29–38 (wiki) | 24 | 32 | 33 | 7 (+1 rare) | ~9 |
| SM — Graveyard | 21 | 30–31 | 32–33 | 32, 34 (+3 rares at 33) | 26–45 whole monastery; wings ~29–34 | 30 | 33 | 34 | 2 (+3 rare) | ~3 |
| SM — Library | 21 | 33–34 | 35–36 | 34, 37 | as above; ~31–35 | 33 | 36 | 37 | 2 | ~5 |
| SM — Armory | 21 | 37–38 | 37–38 | 40 | as above; ~36–38 | 37 | 39 | 40 | 1 | ~3 |
| SM — Cathedral | 21 | 38–39 | 39–40 | 40, 42, 42 | as above; ~37–40 | 38 | 41 | 42 | 3 (one optional) | ~4 |

---

## Ragefire Chasm (Orgrimmar, zone 2437)

**Bosses (1.15.8, all Elite):** Oggleflint 16 (tag "Ragefire Chieftain"), Jergosh the Invoker 16, Bazzalan 16, Taragaman the Hungerer 16. Four bosses, all the same level — RFC is the one vanilla dungeon where boss level carries no information.
Source: https://www.wowhead.com/classic/zone=2437/ragefire-chasm

**Trash:** Ragefire Trogg 13–15, Ragefire Shaman 13–15, Earthborer 13–14, Molten Elemental 13–15, Searing Blade Cultist –15, Searing Blade Enforcer 13–15, Searing Blade Warlock 13–15. Entrance and deepest trash are the same band (13–15); the instance is one level of cave, not a ramp.

**Enter level:** 10 — documented directly in Wowhead's Classic dungeon guide ("Quick Facts — Minimum Level: 10; Advised Level: 13–18"), and matching warcraft.wiki.gg's Classic infobox ("Min lvl 10").
Sources: https://www.wowhead.com/classic/guide/classic-dungeons-overview · https://warcraft.wiki.gg/wiki/Ragefire_Chasm_(Classic)

**Documented recommendation:** Wowhead guide: **13–18**. warcraft.wiki.gg Classic infobox: **Level 15–21**. These disagree — see Contradictions.

**Derivation:** MIN 13 (Ragefire Trogg floor) · MAX 16 (any boss) · MEDIAN 15 (16 − 1).
Reality check: pocket history in Wowhead comments and guides puts comfortable clears at 13–15 with a healer, which brackets 15 fine.

**Boss count 4 · rooms ~5** (entry ramp, trogg tunnel, elemental/worm room, Searing Blade chamber, Taragaman's lava chamber).

---

## The Deadmines (Westfall, zone 1581)

**Bosses (1.15.8):** Rhahk'Zor 19, Sneed's Shredder 20, Sneed 20, Gilnid 20, Mr. Smite 20, Captain Greenskin 20, Cookie 20, Edwin VanCleef 21 (flagged Rare Elite in the 1.15.8 table, `classification 2`). Rare: Miner Johnson 19.
Source: https://www.wowhead.com/classic/zone=1581/the-deadmines

**Trash (levels):** Defias Watchman 16–17 · Defias Evoker 17–18 · Defias Overseer 17–18 · Goblin Woodcarver 18–19 · Goblin Engineer 18–19 · Goblin Craftsman 18–19 · Defias Squallshaper 19–20 · Defias Blackguard 19–20 · Defias Pirate 19–20 · Remote-Controlled Golem 18 · Defias Companion 14–15 (non-elite escort).
Entrance trash = **16–18** (Watchman/Evoker/Overseer in the entry tunnel). Deepest trash = **19–20** (Squallshaper/Blackguard/Pirate on the deck and below).

**Enter level:** 10. Reported identically by two independent secondary tables (PC Gamer's Classic dungeon table and Warcraft Tavern's list). Wowhead's live guide field for DM was not readable at the time of research — see Missing evidence.
Sources: https://www.pcgamer.com/wow-classic-dungeon-instance-levels/ · https://www.warcrafttavern.com/wow-classic/guides/dungeons-instances/

**Documented recommendation:** **18–23** (Wowhead Classic dungeons guide; PC Gamer prints the same range).
Source: https://www.wowhead.com/classic/guide/classic-dungeons-overview

**Derivation:** MIN 16 · MAX 21 (VanCleef) · MEDIAN 20 (21 − 1).
Note: MIN 16 is the *trash* floor, not a playable entry band — the portal gate is 10, and a level-10 group cannot kill 16–17 mobs. For design purposes use median 20 and treat 16 as "the wall".

**Boss count 8 (+1 rare) · rooms ~8** (entry tunnel, forge courtyard, foundry, mast room, goblin foundry, lower deck, upper deck, VanCleef's cabin).

---

## Wailing Caverns (The Barrens, zone 718)

**Bosses (1.15.8):** Kresh 20, Lord Cobrahn 20, Lady Anacondra 20, Lord Pythas 21, Skum 21, Lord Serpentis 21, Verdan the Everliving 21, Mutanus the Devourer 22. Rare: Deviate Faerie Dragon 20.
Source: https://www.wowhead.com/classic/zone=718/wailing-caverns

**Trash (levels):** Evolving Ectoplasm 17–18 · Deviate Adder 18–19 · Deviate Crocolisk 18–19 · Deviate Python 18–19 · Deviate Viper 19–20 · Deviate Lasher 19–20 · Druid of the Fang 19–20 · Deviate Dreadfang 20–21 · Deviate Moccasin 20–21 · Nightmare Ectoplasm 20–21.
Entrance = **17–19**; deepest (Mutanus ritual area / upper caves) = **20–21**.

**Enter level:** 10 (PC Gamer + Warcraft Tavern tables agree).
Sources: https://www.pcgamer.com/wow-classic-dungeon-instance-levels/ · https://www.warcrafttavern.com/wow-classic/guides/dungeons-instances/

**Documented recommendation:** **15–25** (Wowhead Classic guide list). Note the wiki's RFC-style infobox value for WC could not be fetched (HTTP 502 on two attempts).
Source: https://www.wowhead.com/classic/guide/classic-dungeons-overview

**Derivation:** MIN 17 · MAX 22 (Mutanus) · MEDIAN 21 (22 − 1). The documented range (15–25) brackets this; the low end 15 is optimistic — a 15 group struggles with 18–19 elites.

**Boss count 8 (+1 rare) · rooms ~8** (entry chamber, four Fanglord lairs, Dreaming chamber, Verdan's alcove, Mutanus' ritual chamber; plus the two side chambers off the entrance).

---

## Shadowfang Keep (Silverpine Forest, zone 209)

**Bosses (1.15.8):** Rethilgore 20, Razorclaw the Butcher 22, Baron Silverlaine 24, Commander Springvale 24, Odo the Blindwatcher 24, Fenrus the Devourer 25, Wolf Master Nandos 25, Archmage Arugal 26. Rare: Deathsworn Captain 25 (rare elite).
Source: https://www.wowhead.com/classic/zone=209/shadowfang-keep

**Trash (levels):** Shadowfang Whitescalp –18 · Shadowfang Moonwalker 19–20 · Shadowfang Wolfguard 20–21 · Shadowfang Darksoul 20–21 · Haunted Servitor 20–21 · Deathstalker 21 (non-elite) · Wailing Guardsman 21–22 · Shadowfang Glutton 21–22 · Vile Bat 22–23 · Blood Seeker 23–24 · Tormented Officer 23–24 · Shadowfang Ragetooth 23–24 · Lupine Horror 24–25 · Bleak Worg / Slavering Worg 18–25 (wide range — they scale with the floor they're on).
Entrance = **18–21** (courtyard worgen and ghosts); deepest = **23–25** (Ragetooth/Lupine Horror on the ramparts and in Arugal's tower).

**Enter level:** 14 (PC Gamer). Warcraft Tavern's alternative table prints 10 for SFK — conflict recorded.
Sources: https://www.pcgamer.com/wow-classic-dungeon-instance-levels/ · https://www.warcrafttavern.com/wow-classic/guides/dungeons-instances/

**Documented recommendation:** **22–30** (Wowhead Classic guide; PC Gamer identical).
Source: https://www.wowhead.com/classic/guide/classic-dungeons-overview

**Derivation:** MIN 18 · MAX 26 (Arugal) · MEDIAN 25 (26 − 1). Divergence note: recommended range opens at 22, six levels above Arugal-minus-one's neighbourhood — i.e. Blizzard-style advice is "come at 22 and go through to 30", my median is "the level at which Arugal dies".

**Boss count 8 (+1 rare) · rooms ~10** (courtyard, stables, dining hall, kitchen, cellar, library, tower base, ramparts, Odo's room, Arugal's chamber).

---

## The Stockade (Stormwind, zone 717)

**Bosses (1.15.8):** Targorr the Dread 24, Dextren Ward 26, Kam Deepfury 27, Hamhock 28, Bazil Thredd 29. Rare: Bruegal Ironknuckle 26 (rare elite). *Caveat: Targorr/Dextren/Kam levels come from an earlier pass of this research and were not re-confirmed in the final 1.15.8 fetch (the stored page slice cut off before their entries); Hamhock 28, Bazil Thredd 29 and Bruegal 26 are directly confirmed.*
Source: https://www.wowhead.com/classic/zone=717/the-stockade

**Trash (levels, confirmed):** Defias Prisoner 23–24 · Defias Captive 23–24 · Defias Inmate 24–25 · Defias Convict 24–25 · Defias Insurgent 25–26.
Entrance = **23–24**; deepest = **25–26**. The Stockade is the flattest level curve of the classic dungeons: 4 levels from door to final boss.

**Enter level:** 15 (PC Gamer, Warcraft Tavern and pvpbank all agree).
Source: https://www.pcgamer.com/wow-classic-dungeon-instance-levels/

**Documented recommendation:** **22–30** (Wowhead Classic guide).
Source: https://www.wowhead.com/classic/guide/classic-dungeons-overview

**Derivation:** MIN 23 · MAX 29 (Thredd) · MEDIAN 28 (29 − 1).

**Boss count 5 (+1 rare) · rooms ~7** (entry hall, three cell-block wings, courtyard, Thredd's hall; the instance is a ring, so rooms are traversed twice).

---

## Blackfathom Deeps (Ashenvale, zone 719)

**Bosses (1.15.8):** Ghamoo-ra 25, Lady Sarevess 25, Gelihast 26, Old Serra'kis 26, Twilight Lord Kelris 27, Aku'mai 28. Quest/summon encounters inside the same zone: Lorgus Jett 26, Baron Aquanis 28.
Source: https://www.wowhead.com/classic/zone=719/blackfathom-deeps
(Note: the 1.15.8 table also contains duplicate SoD-era/scaled entries with `minlevel: 9999` for Ghamoo-ra, Aku'mai, Kelris, Serra'kis and Aquanis — ignore those; the level-bearing duplicates are the ones quoted.)

**Trash (levels):** Aqua Guardian 22–25 (non-elite) · Aku'mai Fisher –23 · Deep Pool Threshfin 24–26 · Blackfathom Murloc 25 · Murkshallow Softshell 25 · Phantasmal Crustacean 25–26 · Aku'mai Servant 26 · Fathom Elemental 26 · Lorgus Jett 26 · Corrosive Droplet 26 · Blackfathom Elite 26 · Aku'mai Snapjaw 26–27 · Phantasmal Snapjaw 26–27 · Blackfathom Tide Priestess –26.
Entrance = **22–25** (strand-crab and murloc pools); deepest = **26–27** (Kelris' temple and Aku'mai's cavern).

**Enter level:** conflicting — **15** (PC Gamer, pvpbank) vs **10** (warcraft.wiki.gg Classic infobox "Min lvl"). Recorded as unresolved; 15 is the safer design value.
Sources: https://www.pcgamer.com/wow-classic-dungeon-instance-levels/ · https://warcraft.wiki.gg/wiki/Blackfathom_Deeps_(Classic)

**Documented recommendation:** **24–32** (Wowhead Classic guide; warcraft.wiki.gg Classic infobox also says 24–32). PC Gamer prints 20–30.
Source: https://www.wowhead.com/classic/guide/classic-dungeons-overview

**Derivation:** MIN 22 · MAX 28 (Aku'mai) · MEDIAN 27 (28 − 1).

**Boss count 6 (+2 quest) · rooms ~7** (entry pool, turtle pool, murloc cave, Twilight altar, sunken temple, Aku'mai's cavern, plus the Domina/Serra'kis side pool).

---

## Razorfen Kraul (The Barrens, zone 491)

**Bosses (1.15.8):** Roogug 28, Aggem Thorncurse 30, Death Speaker Jargba 30, Ward Guardian 31, Overlord Ramtusk 32, Blind Hunter 32 (rare elite), Agathelos the Raging 33, Charlga Razorflank 33. Rare: Earthcaller Halmgar 32 (rare elite).
Source: https://www.wowhead.com/classic/zone=491/razorfen-kraul

**Trash (levels):** Razorfen Warrior 24–25 · Razorfen Quilguard 25–26 · Razorfen Warden 25–26 · Razorfen Totemic 29 · Razorfen Spearhide 29–30 (rare elite) · Quilguard Champion 30–31 · Greater Kraul Bat 32 · Death's Head Ward Keeper 15 (non-elite totem keeper) · Boar Spirit 16–20 (summoned).
Entrance = **24–26** (Warrior/Quilguard/Warden in the thorn maze); deepest = **30–32** (Quilguard Champion, Ward Guardian, Greater Kraul Bat).

**Enter level:** 25 (PC Gamer). Warcraft Tavern's alternate table printed 30 for RFK — conflict noted.
Source: https://www.pcgamer.com/wow-classic-dungeon-instance-levels/

**Documented recommendation:** **30–40** (Wowhead Classic guide) vs **29–38** (warcraft.wiki.gg Classic infobox, "WoW" row; the "BC" row says 24–29). Also note the Wowhead RFK page's own meta text says "an easy dungeon for level 25–35 players" (user comment, not editorial).
Source: https://www.wowhead.com/classic/guide/classic-dungeons-overview

**Derivation:** MIN 24 · MAX 33 (Charlga) · MEDIAN 32 (33 − 1).

**Boss count 7 (+1 rare) · rooms ~9** (entrance, thorn maze branches, boar pens, Roogug's side cavern, the spiral temple, Ward Guardian's platform, Agathelos' grave, the bridges, Charlga's platform).

---

## Scarlet Monastery (Tirisfal Glades, zone 796 — four wings)

All four wings share zone id 796 in the database; wing assignment below is by boss and trash family, which is unambiguous.

**Bosses (1.15.8):**
- **Graveyard:** Interrogator Vishas 32, Bloodmage Thalnos 34. Rares: Fallen Champion 33, Ironspine 33, Azshir the Sleepless 33.
- **Library:** Houndmaster Loksey 34, Arcanist Doan 37.
- **Armory:** Herod 40.
- **Cathedral:** High Inquisitor Fairbanks 40 (ghost, optional), Scarlet Commander Mograine 42, High Inquisitor Whitemane 42.
Source: https://www.wowhead.com/classic/zone=796/scarlet-monastery

**Trash (levels):** Scarlet Scryer 30–31 · Scarlet Torturer 30–31 · Unfettered Spirit 31–(33) · Scarlet Sentry 30–31 · Anguished Dead 31–33 · Haunting Phantasm 32–33 · Scarlet Adept 33–34 · Scarlet Tracking Hound 33–34 · Scarlet Beastmaster 34–35 · Scarlet Diviner 34–35 · Scarlet Chaplain 34–36 · Scarlet Conjuror 35–36 · Scarlet Soldier 35–36 · Scarlet Monk 35–36 · Fire Elemental 35–36 · Scarlet Guardsman 36–37 · Scarlet Evoker 36–37 · Scarlet Protector 36–37 · Scarlet Sorcerer 37–38 · Scarlet Myrmidon 37–38 · Scarlet Defender 37–38 · Scarlet Wizard 38–39 · Scarlet Centurion 38–39 · Scarlet Champion 39–40 · Scarlet Abbot 39–40.
Per wing: **Graveyard** entrance 30–31 → deepest 32–33 · **Library** 33–34 → 35–36 · **Armory** 37–38 (flat) · **Cathedral** 38–39 → 39–40.

**Enter level:** 21 for the whole monastery (PC Gamer). All wings share the same door and the same gate, even though the wings span 30–42 in content — a design point worth noting: the gate is per-building, not per-wing.
Source: https://www.pcgamer.com/wow-classic-dungeon-instance-levels/

**Documented recommendation:** **26–45** for Scarlet Monastery as a whole (Wowhead Classic guide; PC Gamer identical). Per-wing figures only exist in third-party guides: Icy Veins lists Graveyard 29–34, Library 31–35, Armory 36–38, Cathedral 37–40 — that is a guide's recommendation, not a Blizzard in-game figure.
Sources: https://www.wowhead.com/classic/guide/classic-dungeons-overview · https://www.icy-veins.com/wow-classic/scarlet-monastery-dungeon-guides-hub

**Derivation per wing:** MIN = entrance-trash floor, MAX = highest boss, MEDIAN = MAX − 1.
Graveyard MIN 30 · MEDIAN 33 · MAX 34. Library MIN 33 · MEDIAN 36 · MAX 37. Armory MIN 37 · MEDIAN 39 · MAX 40. Cathedral MIN 38 · MEDIAN 41 · MAX 42.
Threshold note: the monastery-wide 26–45 recommendation is the only Blizzard-side number; the four wing bands above are derived, and they match Icy Veins' per-wing advice within ~1–2 levels, which is a useful independent check.

**Boss count:** Graveyard 2 (+3 rares) · Library 2 · Armory 1 · Cathedral 3 (Fairbanks optional) = 8 unique bosses across the building.
**Rooms (est.):** Graveyard ~3 · Library ~5 (Loksey's hall, the library floor, Doan's chamber) · Armory ~3 (hall, training yard, Herod's arena) · Cathedral ~4 (nave, chapel, Mograine/Whitemane room).

---

## Contradictions (recorded, not resolved)

1. **Ragefire Chasm recommended range:** 13–18 (Wowhead guide, PC Gamer) vs **15–21** (warcraft.wiki.gg Classic infobox "Level"). The wiki figure looks like the modern/post-Cataclysm scaling band rather than the Classic-era one; I lean on 13–18, but flag it.
2. **Wailing Caverns recommended range:** 15–25 (Wowhead) vs 17–24 (Wowhead zone meta text) — two figures from the same site, different pages.
3. **The Deadmines recommended range:** 18–23 (Wowhead, PC Gamer) vs 15–22 (Warcraft Tavern "Dungeons & Instances" table).
4. **Blackfathom Deeps enter level:** 10 (wiki infobox "Min lvl") vs 15 (PC Gamer, pvpbank). Unresolved.
5. **Blackfathom Deeps recommendation:** 24–32 (Wowhead, wiki) vs 22–28 (Warcraft Tavern) vs 20–30 (PC Gamer).
6. **Ragefire Chasm enter level:** 10 (Wowhead guide, wiki) vs 8 (Warcraft Tavern table, and a Wowhead comment from 2009).
7. **Shadowfang Keep enter level:** 14 (PC Gamer) vs 10 (Warcraft Tavern).
8. **Razorfen Kraul recommendation:** 30–40 (Wowhead) vs 29–38 (wiki infobox) vs 24–29 (wiki's BC-era row).

## Missing evidence / UNVERIFIED

- **1.12 vs 1.15 divergence per creature.** Wowhead's Classic environment is 1.15.8 only. classicdb.ch (the 1.12 mirror) is dead — the site renders entirely client-side and returned ~169 visible characters on every fetch attempt. No 1.12 creature-level dump was obtained. What would settle it: an archived 1.12 `creature_template` dump (e.g. a CMaNGOS/VMaNGOS `creature_template` SQL of the 1.12 schema), queried for `minlevel/maxlevel` per boss entry.
- **Instance-portal gate values** are secondary-sourced (PC Gamer / Warcraft Tavern / wiki infobox). The authoritative source is DBC data (`MapDifficulty`/`LFGDungeons` required-level columns) or in-game testing; neither was accessible. The wiki "Min lvl" infobox field is the closest thing to game data and it disagrees with the press table for BFD and RFC.
- **The Stockade** Targorr the Dread (24), Dextren Ward (26), Kam Deepfury (27) — from an earlier pass, not re-confirmed in the final fetch. What would settle it: fetching `wowhead.com/classic/npc=1696` (Targorr), `=1663` (Dextren Ward), `=1666` (Kam Deepfury) tooltips.
- **Room counts** are my own count from dungeon maps; no source publishes one. Treat ±1–2.
- **Exact boss list completeness:** dungeons with conditional/rare bosses (Miner Johnson, Deathsworn Captain, Deviate Faerie Dragon, Earthcaller Halmgar, the three Graveyard rares, Fairbanks, Baron Aquanis, Lorgus Jett) are listed separately because they are not always present in a run.

## Sources

Kept:
- Wowhead Classic Era zone pages (2437, 1581, 718, 209, 717, 719, 491, 796) — the 1.15.8 creature table with per-NPC `minlevel`/`maxlevel`/`classification`; the boss and trash numbers above.
- Wowhead "Classic Dungeons Overview" guide — `Minimum Level` and `Advised Level` quick facts; the only wowhead-side documented recommendation, and the direct source of RFC's enter level 10 / advised 13–18.
- warcraft.wiki.gg Classic pages (Ragefire Chasm, Blackfathom Deeps, Razorfen Kraul) — infobox `Level` and `Min lvl` fields; used as a second documented source and as the source of most contradictions.
- PC Gamer "WoW Classic dungeons by level" — a complete min/recommended table used for enter levels where no primary source exists.
- Warcraft Tavern dungeon lists — used only to surface conflicts (its two pages disagree with each other).
- Icy Veins Scarlet Monastery hub — per-wing practical levels, used as a sanity check on my derived wing bands.

Rejected / deprioritized:
- classicdb.ch — dead (client-side only), could not retrieve 1.12 data.
- Wowhead zone pages in `readable` mode — content is JS-rendered, extraction returns "incomplete content".
- Wowhead comments — anecdotal, used only for flavour about difficulty, never for numbers.
- zockify.com, pvpbank.com — SEO-shaped dungeon tables; cited nowhere above except as conflict surface.

## Next steps

1. Pull the three Stockade boss tooltips (`wowhead.com/classic/npc=1696`, `=1663`, `=1666`) to close the one boss-level gap.
2. Resolve the enter-level gates from DBC data (a 1.12/1.15 `MapDifficulty` + `LFGDungeons` dump) rather than press tables — this is the single largest uncertainty for design use.
3. If 1.12 fidelity matters, get a 1.12 `creature_template` dump and diff boss `minlevel/maxlevel` against the 1.15.8 values used here.
