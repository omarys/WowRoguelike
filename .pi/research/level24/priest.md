# Research: Level-24 Priest Healer — real WoW Classic (vanilla 1.12 / Classic Era 1.15) numbers

Both `web_search` and `fetch_content` were available and used; no number is answered from memory. Report written to the required path.

**Concise summary**

- **Race:** Human (Classic racial *The Human Spirit* = +5% Spirit).
- **BiS (all req ≤ 24):** Green Tinted Goggles (35a, +8Sta +7Spi) · Talbar Mantle (30a, +6Int +3Sta) · Cape of the Brotherhood (21a, +6Agi +3Sta) · Tree Bark Jacket (42a, +6Sta +10Int) · Mindthrust Bracers (17a, +9Int/−5Sta) · Magefist Gloves (23a, +3Sta +5Int +4Spi) · Bright Belt (21a, +4Int +4Sta) · Darkweave Breeches (35a, +4Sta +7Int +6Spi) · Spidersilk Boots (29a, +4Sta +4Int +7Spi) · Plains Ring (+8Sta +3Int) · Seal of Wrynn (+4Sta +4Int +3Spi +3Agi +3Str). No neck/trinket verified at ≤24.
- **Gear totals:** +3 Str, +9 Agi, **+44 Sta**, **+52 Int**, **+30 Spi**, **253 armour**. Base stats at 24 and base HP/mana are **UNVERIFIED** (documented gap; labelled low-confidence interpolation only).
- **Weapon:** Staff of the Blessed Seer — 2H (so no off-hand), req 18, 47–71 dmg, 3.00 speed (19.67 dps), +2 Sta +3 Spi, **+24 healing**; total +healing = +24, +0 spell damage.
- **Spells at 24:** Lesser Heal **R3** 135–158 / 75 mana / 2.5 s · Heal **R2** 429–492 / 205 mana / 3.0 s (R3 @28 blocked) · Renew **R3** 175 over 15 s / 105 mana (R4 @26 blocked) · Smite **R4** 91–106 / 95 mana / 2.5 s (R5 @30 blocked) · SW:P **R3** 132 over 18 s / 95 mana (R4 @26 blocked).
- **Regen:** priest mp5 = 2.5 × (13 + Spirit/4); ≈90 mp5 out of the 5-second rule at the inferred Spirit, ~0 while chain-casting (no mp5 gear in build).
- **Sources:** ~45 kept source URLs (classicdb item+spell pages, warcraft.wiki.gg, vanilla-archive Spirit/Human-Classes, wow-guides.com, zugzugdb, wowhead-classic tooltips, wowclassicdatabase, Warcraft Tavern 29-twink pool), plus 6 rejected/deprioritised sources documented.

```acceptance-report
{
  "criteriaSatisfied": [
    {
      "id": "criterion-1",
      "status": "satisfied",
      "evidence": "Report written to the authoritative path with all 8 requested fields, each number carrying a source URL; unverifiable items (level-24 base attributes, base HP/mana, trinket/neck slots) explicitly marked UNVERIFIED rather than guessed. Residual risks listed."
    }
  ],
  "changedFiles": [
    "/home/omary/.pi/agent/sessions/--home-omary-Dev-WowRoguelike--/subagent-artifacts/outputs/1d2824e8-abfb-4057-9f79-fe713d2da876/.pi/research/level24/priest.md"
  ],
  "testsAddedOrUpdated": [],
  "commandsRun": [],
  "validationOutput": [
    "Gear stats cross-checked across two independent DBs where both available (classicdb 1.12 vs wowhead-classic tooltips via search results vs wowclassicdatabase): Tree Bark Jacket 42a +6Sta +10Int all three agree; Staff of the Blessed Seer req18 47-71/3.00/+2Sta+3Spi/+24 healing all three agree; Spidersilk Boots and Bright Belt conflict between classicdb (used) and an AI page summary (recorded as contradictions).",
    "Spell ranks cross-checked between classicdb 1.12 spell tooltips (primary) and warcraft.wiki.gg ability learn-levels; Smite R2 level conflict (6 vs 4) recorded. Heal/Renew/SW:P/Smite next-rank levels (28/26/26/30) confirmed above 24, so the picks at 24 are the highest usable ranks.",
    "Regen formula corroborated by two independent sources (vanilla-archive Spirit page and zugzugdb Classic formulas page): 13 + Spirit/4 per 2s tick, x2.5 for mp5."
  ],
  "residualRisks": [
    "Level-24 pre-gear base attributes (Str/Agi/Sta/Int/Spi) and class base HP/mana at 24 could not be verified: no fetchable vanilla per-level table (warcraft.wiki.gg Base attributes is modern; wowwiki-archive page is stubbed; vmangos/cmangos player_levelstats SQL paths 404; vMaNGOS wiki is schema-only; reddit blocked). Field 1 and the base half of fields 3 and 5 therefore remain open; a labelled low-confidence linear interpolation is included but must not be treated as sourced.",
    "Level 24 is not a vanilla battleground bracket, so no community BiS meta exists; the gear list is a curated build filtered from verified <=24 items (level-29 twink lists used as the candidate pool). Slot choices (esp. Spirit head vs Int head) may differ from another author's pick.",
    "Two unresolved tooltip conflicts affect the numbers if Classic Era 1.15 differs from the 1.12 DB: Spidersilk Boots extra +7 Spirit, and Azure Silk Hood possibly having +8 Intellect (would beat Green Tinted Goggles for Int stacking).",
    "Trinket slots (and neck) are empty because nothing at req <=24 could be verified; AGM/Insignia level requirements were not confirmed.",
    "Enchants, consumables and spell-power coefficients/down-ranking for the +24 healing were out of scope and are not researched."
  ],
  "noStagedFiles": true,
  "diffSummary": "Created one new research artefact (priest.md) outside the repo; no repository files touched.",
  "reviewFindings": [
    "no blockers: deliverable path and 8 requested fields satisfied; UNVERIFIED items are flagged in-file with the failed source attempts recorded"
  ],
  "manualNotes": "Method note for the parent: fetchable sources are warcraft.wiki.gg, classicdb.ch (exact 1.12 item+spell tooltips), wowclassicdatabase.com, wow-guides.com and zugzugdb.com; wowhead.com/classic pages, Warcraft Tavern tool pages, reddit and the vanilla base-stat SQL do NOT extract (details in the report's opening section). If an exact level-24 base-stat/pool figure is required, the fastest fix is the wow-guides.com classic-era gear planner (it has a level 1-60 slider reading the 1.12 dataset) or an in-game screenshot; that would close fields 1, 3 and 5."
}
```

Ponytail note: I skipped enchanting, consumables and spell-power coefficient maths (not requested) and left the neck/trinket slots empty rather than padding them with guesses — the one number to be careful with is the pre-gear base attribute block, which is explicitly UNVERIFIED and must not be shipped as sourced.
