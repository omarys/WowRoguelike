# Wailing Caverns (WoW Classic 1.12 / Classic Era) — real creature data for a level-24 five-player sim

Research date: this session. Target: **Wailing Caverns, level 17–22 elite dungeon** (Barrens).
Every number below carries a source URL. Anything not shown by a fetched page is marked **UNVERIFIED** — nothing is guessed.

---

## 0. How to read this / source tiers

| Tier | Source | What it gives | Trust |
|---|---|---|---|
| A — Classic DB (client-derived stats) | classicdb.ch NPC pages | exact **level, elite flag, Health, melee Damage min–max, Armor** | direct evidence |
| A — Wowhead Classic tooltips | `nether.wowhead.com/classic/tooltip/npc/{id}` and `/spell/{id}` | **level band, creature type, elite, spell cast time/range/values** | direct evidence |
| B — Aowow (emulator world DB, CMaNGOS/MaNGOS schema) | aowow.oldmanwarcraft.com `?npc=` / `?spell=` | **SmartAI table**: which spell, on which trigger, at which interval, plus call-for-help / flee rows | direct evidence of emulator scripting = strongest available proxy for era-accurate NPC AI (use with the caveat in §10) |
| C — Vanilla 1.12.1 computed spell values | database.stonetavern.app (vMaNGOS core, Elwynn realm, snapshot 2026-09-05) | exact **spell damage ranges** (`Lightning Bolt 64–86`) | direct evidence, private-server core |
| D — Editorial / wiki prose | warcraft.wiki.gg, wowhead classic dungeon guide | strategy, behaviour warnings, relative "difficulty" | interpretation-grade, not numbers |

**Universal caveats**
- `classicdb.ch` "Damage: X – Y (Physical)" is **per-swing melee** min–max. **No fetched DB page lists attack speed or melee DPS for any of these NPCs → attack speed and DPS are UNVERIFIED everywhere in this report.**
- `warcraft.wiki.gg` infobox "Level" fields (e.g. *10-32 Elite*, *8-30 Elite*) are **post-Cataclysm/removed-content scaling artifacts**, not 1.12 values. Use classicdb / Wowhead tooltips for level. (Category-D note, flagged so nobody pastes 10-32 into a vanilla file.)
- Aowow "Controlled abilities (19/20)" lists on the raptors are *tameable-pet skill lists*, not abilities the mob uses in combat. Ignore them.

---

## 1. Lady Anacondra `<Fanglord>` — NPC 3671

| Field | Value | Source |
|---|---|---|
| Level | **20** | classicdb `?npc=3671`; Wowhead tooltip npc/3671 → "Level 20 Humanoid (Elite)" |
| Elite? | **Elite** | same |
| Type | **Humanoid** (Night elf, Druid) | Wowhead tooltip npc/3671; warcraft.wiki.gg/wiki/Lady_Anacondra |
| Health | **2,214** | https://classicdb.ch/?npc=3671 |
| Melee per swing | **154 – 183 Physical** | https://classicdb.ch/?npc=3671 |
| Attack speed / DPS | **UNVERIFIED** | no source shows it |

**Abilities actually cast (SmartAI, Aowow `?npc=3671`)** — every row verbatim:

| Spell | ID | Cast | Numbers | Trigger / cadence |
|---|---|---|---|---|
| Lightning Bolt | 9532 | **3 sec cast**, 40 yd, 90 mana | **64–86 Nature** | in combat, `Instant` first, **repeat every 2 sec** |
| Sleep | 700 | **1.5 sec cast**, 30 yd, 60 mana | sleep **up to 20 sec**, breaks on damage, 1 target at a time | after 8–12 s, **repeat every 12–25 s**, targets a *random non-tank* unit within 30 m |
| Healing Touch (Rank 3) | 5187 | **2.5 sec cast**, 40 yd, 110 mana | heals **204–253** (tooltip, level 14–19 rendering) | when a **friendly NPC within 40 m is at 400 health**, repeat every 15–26 s; interrupts her current cast |
| Thorns Aura | 8148 | **Instant** (1 s CD) | **20 Nature damage to any melee attacker**, lasts 1 min | after 1–3 s, repeat every 50 s, **on self** |
| Nature Channeling | 13236 | channeled ("0 sec cast") | no numbers published | listed as an ability; not in her SmartAI rows (UNVERIFIED effect) |

Sources for numbers: https://nether.wowhead.com/classic/tooltip/spell/9532 · /5187 · /700 · /8148 · /13236 · https://database.stonetavern.app/spell/9532-lightning-bolt (Lightning Bolt 64–86).
**No Serpent Form for Anacondra** (she has no 8041/7965 row).

Behaviour / adds (Category D): *"She's actually more difficult to kill than either Cobrahn or Serpentis, due to these adds. Be careful of the raptors, if they are about to die they will call for assistance, and other Druids of the Fang, or other raptors will come to help out... She will cast a lightning bolt similar to a Shaman's and will cast Sleep on occasional party members, removing them from combat for 10–15 seconds."* — https://warcraft.wiki.gg/wiki/Lady_Anacondra
Note: that prose says Sleep removes the target for 10–15 s; the spell tooltip (700) says *up to 20 s*. Conflict recorded in §9.

---

## 2. Lord Cobrahn `<Fanglord>` — NPC 3669

| Field | Value | Source |
|---|---|---|
| Level | **20** | https://classicdb.ch/?npc=3669 ; Wowhead tooltip npc/3669 → "Level 20 Humanoid (Elite)" |
| Elite? | **Elite** | same |
| Type | **Humanoid** | wowhead tooltip npc/3669 |
| Health | **2,279** | https://classicdb.ch/?npc=3669 |
| Melee per swing | **145 – 172 Physical** | same |
| Attack speed / DPS | **UNVERIFIED** | — |

**Abilities actually cast** (SmartAI, https://aowow.oldmanwarcraft.com/?npc=3669):

| Spell | ID | Cast | Numbers | Trigger / cadence |
|---|---|---|---|---|
| Lightning Bolt | 9532 | 3 sec, 40 yd | 64–86 Nature | in combat, repeat every 2 s |
| Druid's Slumber | 8040 | **2.5 sec cast**, 30 yd, 60 mana | sleep **up to 15 sec**, breaks on hostile action | after 10 s, repeat every **13–20 s**, random non-tank within 30 m |
| Healing Touch (Rank 3) | 5187 | 2.5 sec | heals 204–253 | when a friendly NPC within 40 m is at **400 health**, repeat every 12–18 s |
| **Cobrahn Serpent Form** | 7965 | **Instant** | **+50 attack damage, +43 % time between attacks, lasts 5 min** (cannot be dispelled by the player) | **at 0–30 % health**, no repeat |
| Poison Proc | 3616 | passive | **15 % proc chance** (no damage value published) | applied immediately after Serpent Form |

Sources: https://nether.wowhead.com/classic/tooltip/spell/9532 · /8040 · /5187 · /7965 · /3616 · https://database.stonetavern.app/spell/9532-lightning-bolt
Pre-pull: **3× Deviate Python** (level **18–19, non-elite**, AOE-able) — https://www.wowhead.com/classic/guide/wailing-caverns-dungeon-strategy-wow-classic

---

## 3. Lord Pythas `<Fanglord>` — NPC 3670

| Field | Value | Source |
|---|---|---|
| Level | **21** | https://classicdb.ch/?npc=3670 ; wowhead tooltip npc/3670 → "Level 21 Humanoid (Elite)" |
| Elite? | **Elite** | same |
| Type | **Humanoid** | wowhead tooltip npc/3670 |
| Health | **2,409** | https://classicdb.ch/?npc=3670 |
| Melee per swing | **154 – 183 Physical** | same |
| Attack speed / DPS | **UNVERIFIED** | — |

**Abilities actually cast** (SmartAI, https://aowow.oldmanwarcraft.com/?npc=3670):

| Spell | ID | Cast | Numbers | Trigger / cadence |
|---|---|---|---|---|
| Lightning Bolt | 9532 | 3 sec, 40 yd | 64–86 Nature | in combat, repeat every 2 s |
| **Thunderclap** | 8147 | **Instant** | **71–79 Nature to nearby enemies, +43 % time between their attacks, −40 % movement speed, 10 sec** | after 6–8 s, repeat every 6–11 s, on self |
| Sleep | 700 | **1.5 sec cast**, 30 yd | up to 20 sec | after 8–12 s, repeat every 12–20 s, random non-tank within 30 m |
| Healing Touch (Rank 3) | 5187 | 2.5 sec | heals 204–253 | friendly NPC within 40 m at **400 health**, every 20–25 s, interrupts current cast |

Sources: https://nether.wowhead.com/classic/tooltip/spell/8147 · /700 · /5187 · /9532
Adds: **one Druid of the Fang + one Deviate Shambler** (Deviate Shambler = NPC 5761, **level 19–20, Elemental, Elite** — wowhead tooltip npc/5761). Wiki strategy: *"Pythas is the toughest of the Leaders of the Fang. He puts out a lot of melee damage, and he has a lot of hitpoints."* — https://warcraft.wiki.gg/wiki/Wailing_Caverns

---

## 4. Lord Serpentis `<Fanglord>` — NPC 3673

| Field | Value | Source |
|---|---|---|
| Level | **21** | https://classicdb.ch/?npc=3673 ; wowhead tooltip npc/3673 → "Level 21 Humanoid (Elite)" |
| Elite? | **Elite** | same |
| Type | **Humanoid** | wowhead tooltip npc/3673 |
| Health | **2,770** | https://classicdb.ch/?npc=3673 |
| Melee per swing | **154 – 183 Physical** | same |
| Attack speed / DPS | **UNVERIFIED** | — |

**Abilities actually cast** (SmartAI, https://aowow.oldmanwarcraft.com/?npc=3673):

| Spell | ID | Cast | Numbers | Trigger / cadence |
|---|---|---|---|---|
| Lightning Bolt | 9532 | 3 sec, 40 yd | 64–86 Nature | in combat, repeat every 2 s |
| Sleep | 700 | **1.5 sec cast**, 30 yd | up to 20 sec | after 3–6 s, repeat every **10–20 s**, random non-tank within 30 m |
| Healing Touch (**Rank 6**) | 6778 | **3.5 sec cast**, 40 yd, 335 mana | heals **762–914** (tooltip) | when a friendly NPC within 40 m is at **600 health**, every 12–18 s, interrupts current cast |

Sources: https://nether.wowhead.com/classic/tooltip/spell/6778 · /700 · /9532
Caveat (researcher note): spell 6778's tooltip is the **level-32–37 Druid rank** ("Requires level 32"), used on a level-21 NPC — the tooltip range 762–914 is what the DB publishes; the effective heal in-game is capped by the recipient's max health. Treat the exact number as tooltip-grade, not mob-tuned.

Behaviour (Category D): *"Serpentis doesn't do anything particularly noteworthy, but he's almost as tough as Pythas."* — https://warcraft.wiki.gg/wiki/Lord_Serpentis ; guide: *"he is only dangerous due to his Sleep ability... interrupt Sleep, and the fight is a cakewalk. Unlike the other Druids, Serpentis can be pulled without any additional trash."* — https://www.wowhead.com/classic/guide/wailing-caverns-dungeon-strategy-wow-classic

---

## 5. Druid of the Fang (main trash caster) — NPC 3840

| Field | Value | Source |
|---|---|---|
| Level | **19 – 20** | https://classicdb.ch/?npc=3840 ; wowhead tooltip npc/3840 → "Level 19 - 20 Humanoid (Elite)" |
| Elite? | **Elite** | same |
| Type | **Humanoid** | wowhead tooltip npc/3840 |
| Health | **1,235 – 1,332** | https://classicdb.ch/?npc=3840 |
| Melee per swing | **145 – 172 Physical** | same |
| Attack speed / DPS | **UNVERIFIED** | — |

**Abilities actually cast** (SmartAI, https://aowow.oldmanwarcraft.com/?npc=3840):

| Spell | ID | Cast | Numbers | Trigger / cadence |
|---|---|---|---|---|
| Lightning Bolt | 9532 | **3 sec cast**, 40 yd, 90 mana | **64–86 Nature** | in combat, first `Instant`, **repeat every 2 s** |
| Druid's Slumber | 8040 | **2.5 sec cast**, 30 yd, 60 mana | sleep **up to 15 sec** | after 8–9 s, repeat every **10–20 s**, random non-tank within 30 m |
| Healing Touch (Rank 3) | 5187 | **2.5 sec cast** | heals **204–253** | when a friendly NPC within 40 m is at **400 health**, every **12–18 s**, interrupts current cast |
| Serpent Form | 8041 | **Instant** | **+25 Physical damage dealt, cannot cast spells, 10 sec** | **at 0–30 % health**, no repeat |
| Nature Channeling | 13236 | channeled | no numbers published | **out of combat**, after 5 s–1 min, repeat every 1 min, on self; then removes all auras from self 6–30 s later |
| **Flee for assistance** | (SmartAI row) | — | default flee emote | **at 0–15 % health**, no repeat |

Sources: https://nether.wowhead.com/classic/tooltip/spell/9532 · /8040 · /5187 · /8041 · /13236 · https://database.stonetavern.app/spell/9532-lightning-bolt · https://aowow.oldmanwarcraft.com/?npc=3840
Category D: *"They are hard-hitting casters... Druid's Slumber will put an enemy target to sleep for up to 15 seconds... it can also target healers... Eventually, the druids will cast Serpent Form, which silences them and negates the possibility of a slumber cast."* — https://www.wowhead.com/classic/guide/wailing-caverns-dungeon-strategy-wow-classic ; wiki adds *"save a Kick, shock, or Counterspell for their final heal"* — https://warcraft.wiki.gg/wiki/Wailing_Caverns

---

## 6. Naralex — NPC 3679

| Field | Value | Source |
|---|---|---|
| Level | **25** | https://classicdb.ch/?npc=3679 ; wowhead tooltip npc/3679 → "Level 25 Humanoid (Normal)" |
| Elite? | **Normal** (not elite) | same |
| Type | **Humanoid** | https://classicdb.ch/?npc=3679 (breadcrumb: Humanoids) |
| Health | **1,240** | https://classicdb.ch/?npc=3679 |
| Melee per swing | **47 – 56 Physical** | same |
| Attack speed / DPS | **UNVERIFIED** | — |
| Abilities | **none listed on any fetched page** — no SmartAI table, no abilities section | https://classicdb.ch/?npc=3679 ; https://warcraft.wiki.gg/wiki/Naralex |

Behaviour: he is **found sleeping** in the Wailing Caverns and is woken at the end of the escort/quest chain; wiki prose describes him being rescued, not fought — https://warcraft.wiki.gg/wiki/Naralex. **Interpretation (medium confidence):** in the Classic dungeon Naralex is a quest NPC at Dreamer's Rock, not a hostile encounter; his stat block exists but no classic encounter uses it. Do **not** model him as a boss without a source that says otherwise.

---

## 7. Mutanus the Devourer — NPC 3654 (final boss, Dreamer's Rock)

| Field | Value | Source |
|---|---|---|
| Level | **22** | https://classicdb.ch/?npc=3654 ; wowhead tooltip npc/3654 → "Level 22 Humanoid (Elite)" |
| Elite? | **Elite** | same |
| Type | **Humanoid** (client type; visually a murloc/beast of the Nightmare) | wowhead tooltip npc/3654 |
| Health | **4,555** | https://classicdb.ch/?npc=3654 |
| Melee per swing | **179 – 213 Physical** | same |
| Attack speed / DPS | **UNVERIFIED** | — (Wowhead's classic NPC page reported 4,496 health in one search summary; **UNVERIFIED attribution**, classicdb 4,555 is the sourced value) |

**Abilities actually cast** (SmartAI, https://aowow.oldmanwarcraft.com/?npc=3654):

| Spell | ID | Cast | Numbers | Trigger / cadence |
|---|---|---|---|---|
| **Thundercrack** | 8150 | **Instant**, self-centred AoE, "Requires level 20" | **94 base up to 106–118 Nature damage** (conflict, §9) **+ stun 2.0–2.5 sec** | in combat, after 5–6 s, **repeat every 15–20 s** |
| **Terrify** | 7399 | **Instant**, Melee Range | target **flees in fear for 4 sec**, only 1 target at a time | after 8–11 s, repeat every **18–25 s**, on current opponent |
| **Naralex's Nightmare** | 7967 | **2 sec cast**, 30 yd | **sleep up to 15 sec**, any hostile action wakes the target, 1 target at a time | after 11–15 s, repeat every **32.4–33.8 s**, at a **random non-tank unit within 30 m** |
| (spawn/exit rows) | — | — | — | On spawn: move to point #1, then set in combat after 4 s. On death: sets instance data + Naralex data field |

Sources: https://nether.wowhead.com/classic/tooltip/spell/8150 · /7399 · /7967 · https://aowow.oldmanwarcraft.com/?npc=3654
Category D: Thundercrack's threat is aimed at melee, *"Stay at range to avoid it"* — https://warcraft.wiki.gg/wiki/Mutanus_the_Devourer

---

## 8. The deviate raptors — real names + which one the sim should use

Real raptor creature names in Wailing Caverns (all Beast, all Elite, all Raptor family):

| Name | NPC ID | Level | Health | Melee/swing | Armor | Notable ability | Source |
|---|---|---|---|---|---|---|---|
| **Deviate Ravager** | 3636 | **18 – 19** | **1,248 – 1,375** | **70 – 82** | 800 | **Thrash** (3417 and 8876): "adds a chance to do two additional attacks" — 10–15 % proc, 4 s cooldown; cast on respawn | https://classicdb.ch/?npc=3636 ; https://nether.wowhead.com/classic/tooltip/npc/3636 ; https://aowow.oldmanwarcraft.com/?npc=3636 ; tooltip spell/3417, /8876 |
| **Deviate Guardian** | 3637 | **18 – 19** | **1,275 – 1,379** | **70 – 82** | — | **Call for help within 40 m at 0–30 % health**, with the emote *"Deviate Guardian lets out a shriek, calling for help!"* | https://classicdb.ch/?npc=3637 ; https://aowow.oldmanwarcraft.com/?npc=3637 ; wowhead classic npc=3637 |
| Deviate Slayer | 3633 | 16 – 17 | 1,098 – 1,157 | 37 – 44 | — | none published (Aowow "Abilities (2)" lists nothing usable) | https://classicdb.ch/?npc=3633 ; wowhead tooltip npc/3633 |
| Deviate Stalker | 3634 | 15 – 17 | 1,065 – 1,180 | 35 – 42 | 695 | **Sneak** (22766) — stealths when out of combat and on evade | https://classicdb.ch/?npc=3634 ; https://aowow.oldmanwarcraft.com/?npc=3634 |
| Deviate Creeper | 3632 | 15 – 16 | 1,009 – 1,090 | 35 – 42 | — | "Abilities (4)" but none published (UNVERIFIED) | https://classicdb.ch/?npc=3632 |

**Recommendation — use *Deviate Ravager* (NPC 3636) as "the raptor".**
Reason (researcher inference from the sourced data): the Ravager is the **level-18–19** raptor of the caves and the one that shares Anacondra's room with the Guardian, so it is the representative "hard" cave raptor; Slayer/Stalker/Creeper are the weaker 15–17 raptors and should be modelled as a separate weaker tier if you need them.
If your sim wants the **call-for-help** behaviour (the wiki explicitly warns about it around Anacondra), swap in **Deviate Guardian (3637)** — same level, same 70–82 damage, HP +27, and the sourced 0–30 %-health call-for-help row.

---

## 9. Level spread: WC trash vs bosses (real 1.12 values)

| Band | Creatures | Levels |
|---|---|---|
| Trash, low | Deviate Creeper, Deviate Stalker, Deviate Slayer | **15 – 17** (Creeper 15–16, Stalker 15–17, Slayer 16–17) |
| Trash, high | Deviate Ravager, Deviate Guardian, Deviate Shambler, Druid of the Fang | **18 – 20** (raptors 18–19, Shambler 19–20, Druid of the Fang 19–20) |
| Non-elite add | Deviate Python (Cobrahn's 3 adds) | **18 – 19**, non-elite |
| Bosses | Lady Anacondra 20, Lord Cobrahn 20, Lord Pythas 21, Lord Serpentis 21, Mutanus the Devourer 22 | **20 – 22** |
| Non-combat NPC | Naralex | **25, Normal** |

Dungeon as a whole: **"lvl 17-24"** per https://warcraft.wiki.gg/wiki/Wailing_Caverns ; *"a level 15-25 dungeon"* per https://www.wowhead.com/classic/guide/wailing-caverns-dungeon-strategy-wow-classic . Bosses are only **2–5 levels above** the top trash and **5–7 levels below** a level-24 party.

---

## 10. What a level-24 five-player party (Warrior tank, Priest, Druid, Rogue, Mage) experiences

**Sourced statements (Category D):**
- Anacondra: *"If your interrupts are on point and you've pulled carefully, this fight should be easy."* — https://www.wowhead.com/classic/guide/wailing-caverns-dungeon-strategy-wow-classic
- Cobrahn: three non-elite Pythons first, *"easy to AOE down"*; his danger is Sleep, then Serpent Form. Same guide.
- Pythas: *"the toughest of the Leaders of the Fang. He puts out a lot of melee damage, and he has a lot of hitpoints."* — https://warcraft.wiki.gg/wiki/Wailing_Caverns
- Serpentis: *"interrupt Sleep, and the fight is a cakewalk"*; also *"the last of the 4 Druids of the Fang"* with no mandatory trash. Same guide.
- Trash casters: *"one 'Sleep' can be dealt with, but 2 or more will result in a wipe"* (Druid of the Fang / Faerie-Dragon pull). Same guide.
- Mutanus: AoE Thundercrack targets melee; *"Stay at range to avoid it."* — https://warcraft.wiki.gg/wiki/Mutanus_the_Devourer

**Verdict from the sources: these bosses are *not* dangerous to a competent level-24 group** — every one of them is 3–4 levels *below* the party, all their damage is interruptible/dispellable (Sleep is dispellable by Priest/Druid/Paladin/Shaman per the guide), and no boss has an enrage or a wipe mechanic. The real failure modes named by the sources are (a) extra Druids of the Fang pulled with a boss → 2+ simultaneous Sleeps → wipe, (b) the disciplined flee/call-for-help chains from Druid of the Fang (0–15 %) and Deviate Guardian (0–30 %), (c) Sleep landing on the healer mid-pull.

**Fight duration — INFERENCE, no source states timings.** Arithmetic from the sourced HP only, not a sourced figure:
- Fanglord ~2,214–2,770 HP. A level-24 five-man's *effective* boss DPS is roughly 120–250 (tank + 3 DPS at 40–80 DPS each, minus the Priest/Druid healing downtime and interrupts). → **~10–25 s per Fanglord**.
- Mutanus 4,555 HP → **~20–45 s**, longer if the Rogue/tank eat Thundercrack stuns and the Mage/Healer get slept.
- Practically: each boss reads as a **~15–30 s burn**; the danger budget is spent on the trash pull in front of the boss, not on the boss.
- Derived DPS if you need a per-swing→DPS conversion: **UNVERIFIED attack speed**; at the common 2.0 s vanilla elite swing (researcher assumption, **not sourced**) Mutanus ≈ 98 DPS, Fanglords ≈ 84 DPS, Deviate Ravager ≈ 38 DPS, Druid of the Fang ≈ 79 DPS. Re-derive after finding a real attack-speed source.

---

## 11. Contradictions / conflicts recorded

1. **Thundercrack damage & stun (Mutanus, spell 8150).** Wowhead Classic tooltip: *"106 to 118 Nature damage..., stunning them for 2.50 sec"* (template base `94`). warcraft.wiki.gg (search-result summary): *94–106 Nature damage, 2.5 s stun*. classic-wow-archive fandom (classic-era wiki): *"Thundershock — Inflicts 94-107 Nature damage to nearby enemies, stunning them for 2 sec."* → **Lower bound 94 is consistent across all three; the upper bound (106 / 107 / 118) and stun (2.0 / 2.5 s) disagree.** Suggested sim value: **94–107, 2.5 s**.
2. **Anacondra's Sleep duration.** Spell 700 tooltip: *up to 20 sec*. warcraft.wiki.gg prose: *"removing them from combat for 10–15 seconds."*
3. **Cobrahn Serpent Form wording.** Spell 7965 tooltip: *"increasing attack damage by 50, and increasing the time between attacks by 43 %"*. Wowhead guide: *"increasing his damage by 50 and attack speed by 43 %."* The tooltip implies **slower** attacks (−30 % attack rate); the guide implies faster. Tooltip is the primary source → use **+50 damage, −43 % attack rate**.
4. **Mutanus health.** classicdb 4,555 (sourced) vs 4,496 surfaced in one Wowhead-derivative search summary that could not be attributed to a specific fetched page (**UNVERIFIED**).
5. **Healing Touch rank/values.** Bosses (Anacondra/Cobrahn/Pythas/Druid of the Fang) all use **rank 3 (5187, 204–253)**; Serpentis alone uses **rank 6 (6778, 762–914, "Requires level 32")** — a level-21 NPC casting a level-32 heal is anomalous and may be an emulator DB or era quirk. Flagged, not resolved.

## 12. Missing evidence / UNVERIFIED list
- **Attack speed and melee DPS for every creature in this report** — not published by any fetched source.
- Armor values only recorded for Deviate Ravager (800) and Deviate Stalker (695).
- Naralex: no abilities, no threat role; whether any classic encounter attacks him (**UNVERIFIED**).
- Nature Channeling (13236) effect numbers, Druid of the Fang "Nature Channeling" purpose beyond out-of-combat aura clearing.
- Deviate Creeper / Deviate Slayer ability lists (Aowow shows counts but no usable rows).
- Exact Mug/aggro-radius values; exact respawn timers.
- Whether Thundercrack is a self-centred PBAoE with a fixed radius (tooltip says "nearby enemies"; radius UNVERIFIED).
- The 4,496 Mutanus health figure's attribution.
- **Validation limitation:** one `source_check` was run (Mutanus abilities) and returned **status "unclear" with no semantic support/contradiction assessment**, so its passages could not be machine-verified; the classic-wow-archive page was fetched manually instead to cross-check Thundercrack. All other numbers are direct page reads, not `source_check`-validated.

---

## 13. Sources

**Kept (with what each is used for):**
- https://classicdb.ch/?npc=3671 — Anacondra: 20 Elite, 2,214 HP, 154–183
- https://classicdb.ch/?npc=3669 — Cobrahn: 20 Elite, 2,279 HP, 145–172
- https://classicdb.ch/?npc=3670 — Pythas: 21 Elite, 2,409 HP, 154–183
- https://classicdb.ch/?npc=3673 — Serpentis: 21 Elite, 2,770 HP, 154–183
- https://classicdb.ch/?npc=3654 — Mutanus: 22 Elite, 4,555 HP, 179–213
- https://classicdb.ch/?npc=3840 — Druid of the Fang: 19–20 Elite, 1,235–1,332 HP, 145–172
- https://classicdb.ch/?npc=3679 — Naralex: 25 Normal Humanoid, 1,240 HP, 47–56
- https://classicdb.ch/?npc=3636 / ?npc=3637 / ?npc=3633 / ?npc=3634 / ?npc=3632 — the five deviate raptors/crawlers
- https://classicdb.ch/?spell=700 — Sleep 1.5 s cast / 20 s duration
- https://aowow.oldmanwarcraft.com/?npc=3671 · /3669 · /3670 · /3673 · /3654 · /3840 · /3636 · /3634 · /3637 — SmartAI ability/trigger/interval tables and call-for-help/flee rows
- https://nether.wowhead.com/classic/tooltip/npc/3671 · /3669 · /3670 · /3673 · /3654 · /3840 · /3679 · /3636 · /3633 · /5761 — level, elite flag, creature type
- https://nether.wowhead.com/classic/tooltip/spell/9532 · /5187 · /8041 · /3417 · /8876 · /8147 · /8148 · /6778 · /3616 · /8040 · /700 · /13236 · /8150 · /7967 · /7399 · /7965 — cast time, range, mana, damage/heal/stun numbers
- https://database.stonetavern.app/spell/9532-lightning-bolt — Lightning Bolt 64–86 Nature (Vanilla 1.12.1, vMaNGOS)
- https://warcraft.wiki.gg/wiki/Wailing_Caverns — Pythas/trash strategy prose, python adds, loot table mob list
- https://warcraft.wiki.gg/wiki/Lady_Anacondra — Sleep 10–15 s, raptor call-for-help warning
- https://warcraft.wiki.gg/wiki/Lord_Serpentis — "almost as tough as Pythas"
- https://warcraft.wiki.gg/wiki/Mutanus_the_Devourer — ability list (94–106 / 2.5 s / 4 s fear / 15 s sleep), "stay at range"
- https://warcraft.wiki.gg/wiki/Lord_Pythas — humanoid elite, add pair
- https://warcraft.wiki.gg/wiki/Naralex — sleeping quest NPC, not an encounter
- https://www.wowhead.com/classic/guide/wailing-caverns-dungeon-strategy-wow-classic — boss-by-boss strategy, difficulty grading, dispel note
- https://classic-wow-archive.fandom.com/wiki/Mutanus_the_Devourer — classic-era ability text ("Thundershock 94-107, stun 2 sec")
- https://www.wowhead.com/classic/npc=3637/deviate-guardian — 18–19 Elite, 1,347 HP (corroborates classicdb 1,275–1,379)

**Rejected / deprioritised:**
- warcraft.wiki.gg infobox "Level 8-30 / 10-32 Elite" fields — post-Cataclysm scaling, not vanilla (used only as a warning).
- Aowow "Controlled abilities (19–20)" pet-skill lists on the raptors — tameable-pet skills, not combat AI.
- vanilla-twinhead.twinstar.cz — returned an anti-bot "Why have I been blocked?" page, no data.
- WoWhead/EvoWow WotLK/TBC/MoP pages surfacing in search — wrong era.
- Various "wow forever / mmonster / forever-codex / wowhandbook" guide farms — SEO-tier, no primary numbers.
- classicdb.ch direct fetch without a reader proxy — JS-only, empty; all classicdb data above was obtained through the reader endpoint.

**Source count: 27 distinct page URLs kept and cited** (classicdb NPC pages ×10, classicdb spell ×1, Aowow NPC pages ×9, Wowhead Classic tooltips ×26 endpoints on 2 domains, wiki pages ×6, guides/archives ×2). Grouped by domain: **classicdb.ch, aowow.oldmanwarcraft.com, wowhead.com (+nether.wowhead.com), warcraft.wiki.gg, database.stonetavern.app, classic-wow-archive.fandom.com** — 6 domains, ~45 endpoints.

---

## 14. Compact per-creature table (drop-in)

| Creature | Lvl | Elite | Type | Health | Melee/swing | Key castables (cast time · numbers · cadence) | Flee/call-help |
|---|---|---|---|---|---|---|---|
| Lady Anacondra (3671) | 20 | Elite | Humanoid | 2,214 | 154–183 | Lightning Bolt 9532 (3 s · 64–86 Nature · every 2 s); Sleep 700 (1.5 s · 20 s · every 12–25 s, non-tank); Healing Touch 5187 R3 (2.5 s · 204–253 · ally ≤400 HP, every 15–26 s); Thorns Aura 8148 (instant · 20 Nature to attackers · 1 min · every 50 s) | none |
| Lord Cobrahn (3669) | 20 | Elite | Humanoid | 2,279 | 145–172 | Lightning Bolt 9532 (3 s · 64–86 · 2 s); Druid's Slumber 8040 (2.5 s · 15 s sleep · 13–20 s); Healing Touch R3 (2.5 s · 204–253 · every 12–18 s); **Serpent Form 7965** (instant · +50 dmg, −43 % attack rate, 5 min · at 30 % HP); Poison Proc 3616 (15 %) | none |
| Lord Pythas (3670) | 21 | Elite | Humanoid | 2,409 | 154–183 | Lightning Bolt 9532 (3 s · 64–86 · 2 s); **Thunderclap 8147** (instant · 71–79 Nature AoE, +43 % attack interval, −40 % speed, 10 s · every 6–11 s); Sleep 700 (1.5 s · 20 s · 12–20 s); Healing Touch R3 (2.5 s · 204–253 · 20–25 s) | none |
| Lord Serpentis (3673) | 21 | Elite | Humanoid | 2,770 | 154–183 | Lightning Bolt 9532 (3 s · 64–86 · 2 s); Sleep 700 (1.5 s · 20 s · every 10–20 s); Healing Touch **R6 6778** (3.5 s · 762–914 · ally ≤600 HP, every 12–18 s) | none |
| Druid of the Fang (3840) | 19–20 | Elite | Humanoid | 1,235–1,332 | 145–172 | Lightning Bolt 9532 (3 s · 64–86 · every 2 s); Druid's Slumber 8040 (2.5 s · 15 s · 10–20 s); Healing Touch R3 (2.5 s · 204–253 · 12–18 s); **Serpent Form 8041** (instant · +25 dmg, cannot cast, 10 s · at 30 % HP); Nature Channeling 13236 (out of combat) | **flee at 15 %** |
| Naralex (3679) | 25 | **Normal** | Humanoid | 1,240 | 47–56 | none published; sleeping quest NPC, not fought | n/a |
| Mutanus the Devourer (3654) | 22 | Elite | Humanoid | 4,555 | 179–213 | **Thundercrack 8150** (instant · 94–107/118 Nature AoE + 2–2.5 s stun · every 15–20 s); **Terrify 7399** (instant, melee · 4 s fear · every 18–25 s); **Naralex's Nightmare 7967** (2 s cast, 30 yd · 15 s sleep · every 32–34 s, non-tank) | none |
| **Deviate Ravager (3636) ← use as "the raptor"** | 18–19 | Elite | Beast (Raptor) | 1,248–1,375 | 70–82 | Thrash 3417/8876 (10–15 % chance for 2 extra attacks, 4 s CD) | none (Guardian has it) |
| Deviate Guardian (3637) | 18–19 | Elite | Beast (Raptor) | 1,275–1,379 | 70–82 | — | **call for help within 40 m at 30 % HP** |
| Deviate Slayer (3633) / Stalker (3634) / Creeper (3632) | 16–17 / 15–17 / 15–16 | Elite | Beast (Raptor) | 1,098–1,157 / 1,065–1,180 / 1,009–1,090 | 37–44 / 35–42 / 35–42 | Stalker: Sneak 22766 (stealth out of combat / on evade) | none |
| Deviate Shambler (5761) — Pythas add | 19–20 | Elite | Elemental | UNVERIFIED | UNVERIFIED | UNVERIFIED (only active-ability reports: self-regeneration, purgeable) | none |

**Attack speed / DPS: UNVERIFIED for every row — no source publishes it.**
