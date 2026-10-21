# Research: vanilla Priest (1.12 / Classic Era) rank table, levels 1-60

Read-only research. No repo file modified.

## Summary
137 ranks across 30 baseline Priest abilities a level 1-60 character can train (talents excluded).
Primary source is **wowcalc.net/en/classic/wiki/spells/priest**, a per-rank table read from the live
Classic Era client (build 1.15.9.70003, 1.12 mechanics), which resolves the Flash Heal rank 1 U
(202-246 heal, 125 mana). It disagrees with `classicdb.ch` (1.12 dump, the source used by
`.pi/research/level24/`) by roughly +6% on heal/damage magnitudes - both sides are listed below, and
every affected level-20-24 row is flagged in the notes column.

## Findings
1. **Flash Heal r1 resolved.** 20 / 125 mana / 1.5 s / 40 yd / heals 202-246 (wowcalc 1.15.9). classicdb
   spell 2061 prints 193-238 for the same rank, the same +5..6% offset seen everywhere else. **D** (wowcalc)
2. **Systematic magnitude offset classicdb vs live Classic client.** Mind Blast r1 39-44 (classicdb) vs
   42-46 (wowcalc/wowhead classic); r3 112-121 vs 117-125; Lesser Heal r3 135-158 vs 143-165; Heal r2
   429-492 vs 445-507; PW:S r4 234 vs 244. The on-disk `.pi/research/level24/` numbers are on the
   classicdb scale. **D** for each pair; researcher inference: two data sets, not two patches.
3. **Cooldowns differ by source.** Live client: PW:S 4 s, Fade 30 s, Psychic Scream 30 s, Mind Blast 8 s.
   1.12 classicdb pages print no cooldown for all four. The table uses the live-client values (matching the
   existing audit in `.pi/research/level24/missing_abilities.md`, which already had 4 s / 30 s / 8 s). **D**
4. **Renew r10 (60) and Holy Fire r8 (60) exist** on the live client, plus PoH r5 (60, 1070 mana, no trainer
   cost listed) - classicdb's 1.12 dump stops at Renew r9 / Holy Fire r7 / PoH r4. **D**
5. **Feedback is a Discipline talent in 1.12**, which is why wowcalc omits it from the baseline spellbook;
   its 5 ranks are listed anyway (flagged) because the task asked for it. Holy Nova, Divine Spirit, Inner
   Focus, Power Infusion, Lightwell, Mind Flay, Silence, Vampiric Embrace, Shadowform confirmed `Talent` on
   the same page and are excluded. **D**
6. **% of base mana costs converted** with priest base mana per level from `wow_world_base`
   (`extended/vanilla/base_stats.rs`, HUMAN_PRIEST array): L10 272, L14 320, L18 399, L22 494, L32 749,
   L34 812, L36 860, L46 1130, L58 1403. Resurrection 75 %, Dispel Magic 18 %, Cure/Abolish Disease 15 %. **I**
   (percentage is direct evidence, the numeric is my conversion).
7. **Prayer of Healing / Prayer of Fortitude / Prayer of Spirit / Prayer of Shadow Protection / Shadow
   Protection / Abolish Disease / Levitate / Mind Vision were missing from the task list** and are baseline
   (trainer-taught), so they are included. **D**

## Contradictions
- Flash Heal r1 magnitude: 202-246 (wowcalc, live 1.15.9) vs 193-238 (classicdb 1.12 dump) vs 174-214
  (zugzugdb, which labels it "Same in Classic Era" - rejected as inconsistent with its own r2).
- PW:S r4 absorb: 244 (wowcalc) vs 234 (classicdb) vs 226 (on-disk audit).
- Lesser Heal r3 / Heal r2: on-disk `.pi/research/level24/priest.md` uses 135-158 / 429-492 (classicdb);
  live client is 143-165 / 445-507.
- Fade / Psychic Scream / Mind Blast cooldown: 30 s / 30 s / 8 s on the live client, none in the 1.12 dump.

## Missing evidence
- Fade threat magnitudes (-55 ... -820) come only from classicdb 1.12; wowcalc does not print them. If the
  magnitudes were rescaled like the heals, these are ~6 % low.
- Desperate Prayer (racial Human/Dwarf) and Fear Ward (Dwarf) are not on the wowcalc class page; no rows.
- Prayer of Healing r5 (60) has no trainer cost - source of the spell (book/quest) unverified.

## Sources
- Kept: wowcalc.net/en/classic/wiki/spells/priest - per-rank table from the live Classic Era client; all
  magnitudes, levels, costs, cast times, cooldowns, ranges.
- Kept: classicdb.ch/?spell=<id> - 1.12 dump; used for Fade threat values and as the 1.12 cross-check.
- Kept: wow_world_base `extended/vanilla/base_stats.rs` (docs.rs) - priest base mana per level for % costs.
- Kept on disk: `.pi/research/level24/missing_abilities.md`, `.pi/research/level24/base_stats.md`.
- Rejected: zugzugdb.com - Flash Heal r1 174-214 contradicts its own r2/r3 ladder.
- Rejected: wowhead.com/classic - JS-rendered; snippets only (used to confirm Mind Blast 8 s CD and
  Psychic Scream 30 s CD).
- Excluded by rule: Holy Nova (talent), Divine Spirit, Lightwell, Power Infusion, Inner Focus.

## Table
See `priest_ranks.tsv` payload below (one fenced TSV block, 13 columns).
