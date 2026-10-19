# Wailing Caverns mobs — stats for a level-24 player ceiling

Two data generations are mixed below and are **not** identical:
`classicdb.ch` = 1.12 / vanilla reference data (has armour + damage + full health ranges);
`wowhead.com/classic` = live Classic Era 1.15.8 (has health, usually no armour/damage row).
Every number carries its source URL.

## 1. Druid of the Fang (trash caster, NPC 3840)
| Field | Value | Source |
|---|---|---|
| Level | 19–20 (elite) | classicdb.ch/?npc=3840 ; wowhead.com/classic/npc=3840 |
| Health | 1,235–1,332 (1.15 single value: 1,299) | classicdb.ch/?npc=3840 ; wowhead.com/classic/npc=3840 |
| Armour | 683 | classicdb.ch/?npc=3840 |
| Melee | 145–172 physical | classicdb.ch/?npc=3840 |
| Attack speed | UNVERIFIED (see §9) | — |
Spells: Lightning Bolt (9532), Healing Touch (5187), Druid's Slumber (8040), Serpent Form (8041) — spell list per classicdb.ch/?npc=3840.

## 2. Lady Anacondra (boss, NPC 3671)
| Field | Value | Source |
|---|---|---|
| Level | 20 (elite, mana 490) | classicdb.ch/?npc=3671 ; wowhead.com/classic/npc=3671 |
| Health | 2,214 (1.12) / 2,165 (1.15) | classicdb.ch/?npc=3671 ; wowhead.com/classic/npc=3671 |
| Armour | 712 | classicdb.ch/?npc=3671 |
| Melee | 154–183 physical | classicdb.ch/?npc=3671 |
| Attack speed | UNVERIFIED | — |
Spells: Thorns Aura (8148), Lightning Bolt (9532), Sleep (700), Healing Touch (5187) — classicdb.ch/?npc=3671.

## 3. Druid of the Fang — Serpent Form
Serpent form is **an aura on the same NPC entry**, not a separate creature: health, armour and level are unchanged; only melee output and the ability to cast change.
| Spell | Cast | Duration | Effects (as printed) | Source |
|---|---|---|---|---|
| Serpent Form (8041) | Instant | 10 s | Change Model [Serpent Form]; Apply Aura: Silence; Mod Damage Done (Physical) **+25** | wowhead.com/classic/spell=8041 |
- Interpretation (mine, not the page's): +25 flat physical per swing with spellcasting silenced for the duration. The 10 s duration contradicts wiki-comment claims that druids "do not appear to turn back" (classicdb.ch/?npc=3840 comments) — treat duration as data, behaviour as anecdote.

## 4. Deviate raptors / lashers
| Mob | Level | Health | Armour | Melee | Source |
|---|---|---|---|---|---|
| Deviate Ravager (3636) | 18–19 elite | 1,248–1,375 | 800 | 70–82 | classicdb.ch/?npc=3636 |
| Deviate Stalker (3634) | 15–17 elite | 1,065–1,180 | 695 | 35–42 | classicdb.ch/?npc=3634 |
| Deviate Lasher (5055) | 19–20, **no elite flag** | 145 | UNVERIFIED | UNVERIFIED | wowhead.com/classic/npc=5055 |
- Deviate Stalker is a stealthing mob (wowhead listing, npc=3634).
- Lasher caveat: 145 health with no elite classification on the 1.15 page is far below every other WC entry; its 1.12 page did not load (classicdb.ch/?npc=5055 returned an incomplete page). Do not use it as standard trash health.
- Older Beast Lore comment on the Ravager page gives level 18 / 44–60 damage / 791 armour / 1,251 health (classicdb.ch/?npc=3636 comments) — disagrees with the infobox damage; see Contradictions.

## 5. The three Fanglords
| Mob | Level | Health | Armour | Melee | Source |
|---|---|---|---|---|---|
| Lord Cobrahn (3669) | 20 elite | 2,279 | 712 | 145–172 | classicdb.ch/?npc=3669 |
| Lord Pythas (3670) | 21 elite | 2,409 | 712 | 154–183 | classicdb.ch/?npc=3670 |
| Lord Serpentis (3673) | 21 elite | 2,770 | UNVERIFIED (no armour row on page) | 154–183 | classicdb.ch/?npc=3673 |
Spell lists: Cobrahn = Lightning Bolt 9532, Poison 17330, **Cobrahn Serpent Form 7965**, Druid's Slumber 8040. Pythas = 9532, Thunderclap 8147, Sleep 700, Healing Touch 5187. Serpentis = 9532, Sleep 700, Healing Touch **6778** (all per classicdb.ch/?npc=3669, 3670, 3673).
Cobrahn's serpent form (7965): Instant, 5 min, Change Model [Cobrahn Snake Form] + Mod Damage Done (Physical) +50 + Mod Melee Attack Speed −43% (wowhead.com/classic/spell=7965). Wiki says his "DPS will increase slightly" (warcraft.wiki.gg/wiki/Lord_Cobrahn).

## 6. Mutanus the Devourer (3654)
| Field | Value | Source |
|---|---|---|
| Level | 22 (elite) | classicdb.ch/?npc=3654 ; wowhead.com/classic/npc=3654 |
| Health | 4,555 (1.12) / 4,496 (1.15) | classicdb.ch/?npc=3654 ; wowhead.com/classic/npc=3654 |
| Armour | UNVERIFIED (absent from both pages) | — |
| Melee | 179–213 physical | classicdb.ch/?npc=3654 |
| Attack speed | UNVERIFIED | — |
Spells: Naralex's Nightmare (7967), Terrify (7399), Thundercrack (8150) — classicdb.ch/?npc=3654.

## 7. Spell data (all unranked NPC abilities — `rank_enus` is empty, e.g. wowhead.com/classic/spell=9532)
| Spell (id) | Cast | CD | Duration | Numbers | Source |
|---|---|---|---|---|---|
| Lightning Bolt (9532) | 3.0 s | none | — | School Damage (Nature) **Value: 64**; 90 mana; 40 yd; flags: "Cannot be used while shapeshifted", "Spell damage depends on caster level" | wowhead.com/classic/spell=9532 |
| Healing Touch (5187) | 2.5 s | none | — | Heal **195** (SP mod 0.554) | wowhead.com/classic/spell=5187 |
| Healing Touch (6778, Serpentis) | 3.5 s | none | — | Heal **742** | wowhead.com/classic/spell=6778 |
| Druid's Slumber (8040) | 2.5 s | none | 15 s | Apply Aura: Stun | wowhead.com/classic/spell=8040 |
| Sleep (700) | 1.5 s | none | 20 s | Apply Aura: Stun | wowhead.com/classic/spell=700 |
| Thunderclap (8147) | Instant | none | 10 s | Nature 71 @10 yd; −40% run speed (snared); −43% melee attack speed | wowhead.com/classic/spell=8147 |
| Thundercrack (8150) | Instant | none | 2.5 s | Nature 94 @20 yd + Stun | wowhead.com/classic/spell=8150 |
| Terrify (7399) | Instant | none | 4 s | Apply Aura: Fear | wowhead.com/classic/spell=7399 |
| Naralex's Nightmare (7967) | 2 s | none | 15 s | Stun + Charm 100 + Invisibility Detection | wowhead.com/classic/spell=7967 |
| Poison (17330) | Instant | none | 20 s | "20 Nature damage every 2 sec for 20 sec", 30 yd | nether.wowhead.com/classic/tooltip/spell/17330 |
| Thorns Aura (8148) | Instant | 1 s | 60 s | Damage shield **20**, radius 30 yd | wowhead.com/classic/spell=8148 |
- Anacondra's Sleep is described in prose as removing a member for "10–15 seconds" (warcraft.wiki.gg/wiki/Lady_Anacondra) vs the spell row's 20 s duration (spell=700). Contradiction recorded, not resolved.
- Lightning Bolt at Value 64 is the *spell effect base*; the "depends on caster level" flag means the level-19–21 caster damage is not the same as 64 in practice. Treat 64 as a floor/anchor, not the observed hit.

## 8. General rules — armour
- **No single "armour by mob level" table exists.** In the Classic server schema armour is `round(BaseArmor(level, unit_class) × ArmorMultiplier)`; the base comes from `creature_template_classlevelstats` keyed by **(level, creature class)**, and the multiplier lives on `creature_template` (https://tangled.org/carl.tngl.sh/OMaNGOS/blob/main/reports/parity/entities__Creature_stats.md — F3 cites `Creature.cpp:1373-1374, 1413-1424, 1493`; field names per https://www.azerothcore.org/wiki/creature_template).
- The numeric Classic base table itself is **UNVERIFIED here** — I could not retrieve a rendered copy of `creature_template_classlevelstats` for levels 15–22 (`cmangos/classic-db` ships it only as large SQL dumps). What the observed data shows instead, from classicdb.ch/?npc=3634/3636/3669/3670/3671/3840:
  - humanoid, lvl 19–20 → **683**; lvl 20 → **712**; lvl 20–21 → **712**
  - beast, lvl 15–17 → **695**; lvl 18–19 → **800**
  So armour is *not* monotonic in level alone; it varies by creature class, exactly as the `(level, class)` key implies.
- Mitigation of mob armour when *you* attack it: `%Reduction = Armor / (85 × attacker_level + Armor + 400)`, capped 75% (https://warcraft.wiki.gg/wiki/Damage_reduction). Researcher inference at attacker level 24 (denominator 2440): 683 → 21.9%, 695 → 22.2%, 712 → 22.6%, 800 → 24.7%.

## 9. General rules — melee damage and speed
- **Documented formula, not guesswork:** `min = (BaseDamage + BaseMeleeAP/14) × attackTime × DamageMultiplier`, `max = (1.5×BaseDamage + BaseMeleeAP/14) × attackTime × DamageMultiplier`, with a fallback to the template's stored damage when no class row exists (https://tangled.org/carl.tngl.sh/OMaNGOS/blob/main/reports/parity/entities__Creature_stats.md, cited to `Creature.cpp:1377-1402`; identical shape quoted in https://talk.trinitycore.org/t/creature-damage/29398). Practical consequence: **same-level mobs can hit differently** because `DamageMultiplier` is per template.
- Therefore per-mob min/max must be read off the creature row (the classicdb.ch pages above do carry it) — do not extrapolate from level.
- **Attack speed: no per-mob value found for any of these creatures.** It is the `attackTime` term above; the engine-side default is 2000 ms (`BaseAttackTime`, https://www.azerothcore.org/wiki/creature_template). Treat 2.0 s as an unverified default for these mobs, not a sourced number.

## Contradictions (left unresolved)
1. Health, 1.12 vs 1.15: Anacondra 2,214 (classicdb.ch/?npc=3671) vs 2,165 (wowhead.com/classic/npc=3671); Mutanus 4,555 vs 4,496 (classicdb.ch/?npc=3654 ; wowhead.com/classic/npc=3654); Druid of the Fang 1,235–1,332 vs 1,299 (classicdb.ch/?npc=3840 ; wowhead.com/classic/npc=3840). Consistent small downward re-tune in Classic Era.
2. Ravager damage: 70–82 (classicdb.ch/?npc=3636 infobox) vs 44–60 (same page's 2006 Beast Lore comment). The comment is player-captured and older; infobox wins, but neither is authoritative.
3. Sleep duration: 20 s (wowhead.com/classic/spell=700) vs "10–15 seconds" prose (warcraft.wiki.gg/wiki/Lady_Anacondra).
4. Level: warcraft.wiki.gg boss pages show "8–30 Elite" scaling levels (warcraft.wiki.gg/wiki/Lord_Cobrahn, /Lord_Serpentis, /Lady_Anacondra) — that is post-7.3.5 retail scaling and is **wrong for Classic**; use classicdb/wowhead single levels.
5. Serpent Form duration 10 s (spell=8041) vs player reports that druids stay transformed (classicdb.ch/?npc=3840 comments).

## Confidence
- **Well sourced (two sources or a primary spell row):** all levels; Anacondra/Pythas/Serpentis/Cobrahn/Mutanus/Ravager/Stalker health and melee ranges (classicdb 1.12 infoboxes); boss health cross-checked on wowhead 1.15; every spell cast time, cooldown, duration and effect value (wowhead spell "Spell Details"); mitigation formula (wiki.gg); armour and damage *mechanism* (CMaNGOS-derived parity report).
- **Interpolated / inferred by me, not stated in a source:** the level-24 mitigation percentages in §8; the reading of Serpent Form as +25 flat physical per swing; the "2.0 s default" attack speed.
- **UNVERIFIED:** attack speed for every mob; Mutanus armour; Serpentis armour; Deviate Lasher health/armour/melee (only a 145-health wowhead row, no 1.12 page); the numeric `creature_template_classlevelstat` base-armour table for levels 15–22; actual in-game Lightning Bolt damage (only effect base 64 + a level-scaling flag). To close these I would need: a `cmangos/classic-db` `creature_template` / `creature_template_classlevelstats` dump (gives armour, damage, attack time per entry at once), or client DBC `CreatureClassStats`.
- Validation limitation: `source_check` on the damage/armour formula returned status "unclear" (no automated entailment available), so the formula rests on my own read of the parity report, not on a machine-checked passage.
