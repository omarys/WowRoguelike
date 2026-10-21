# Missing level-24 class abilities — five specs (Priest / Rogue / Mage / Warrior / Druid)

Research date: this session. Read-only research; no repo file was modified.

**What this file is.** A candidate list of abilities that a real vanilla 1.12 / Classic Era 1.15 character
*could* have at level 24 and that the sim does **not** implement, per the diff baseline given in the task
(Warrior: Taunt, Heroic Strike, Sunder Armor, Shield Bash, Charge, Disarm, Execute + Battle Shout buff;
Rogue: Backstab, Kick, Gouge; Mage: Fireball, Frostbolt, Arcane Missiles, Arcane Explosion, Blizzard,
Polymorph + Arcane Intellect; Priest: Lesser Heal, Heal, Renew, Smite, SW:Pain + PW:Fortitude; Druid:
Healing Touch, Regrowth, Rejuvenation, Entangling Roots, Wrath, Moonfire + Mark of the Wild).
Buffs are not reported as gaps (the sim models them as a fixed party list).

**Rule applied to every row:** learn level must be **≤ 24**. Anything learned at 25+ is listed only in the
"out of reach" table for that class, so the exclusion is auditable.

**Evidence labelling.** `D` = direct evidence (a fetched rank table / spell page states the level and/or the
number). `I` = interpretation (researcher reading of a source, e.g. picking "highest rank ≤ 24" out of a rank
list, or converting a "% of base mana" cost). `U` = unverified — the number could not be pinned to a source
in this pass; it is marked in-line instead of being invented.

**Main numeric source (and its limits).** The per-class rank tables at `wow4ever.quest/en/spellbook/<class>`
(reachable; the sibling mirrors `wow4ever.com` / `foreverdb.net` / `theforeverera.com` were not fetchable in
this session). Those tables print, per rank: level, cost, cast time, cooldown, range, and the tooltip line,
plus an "In Classic, its last rank …" comparison line. **Caveat that matters for numbers:** the site is a
Classic-plus database, so rows are flagged `Changed` where its own values differ from Classic. For nearly
all rows here the *levels and costs* match the vanilla anchors already on disk; where the *magnitude* line
diverges from Classic (it does for Demoralizing Shout, Thunder Clap, Shield Block, Shield Wall, Bloodrage,
Battle Shout), the divergence is recorded and the affected number is marked `U` rather than silently
imported. Where a per-rank magnitude is not printed at all (Thorns, Faerie Fire, Mind Blast, Flash Heal,
Mana Shield, Mana Burn, Inner Fire rank 2), that too is marked `U`.

Anchors reused without re-verification (per the task): `.pi/research/level24/warrior.md` (§7 ability table +
learn-level list), `mage.md` (§7 spell table), `rogue.md` (§7 ability tooltips, classicdb-sourced),
`druid.md` (§7 tooltip table, twinhead 1.12-sourced), `base_stats.md`.

---

## 1. Healing Priest (Holy/Disc-flavoured healer)

| # | Ability (highest rank ≤24) | Learned | Cost | Cast / CD / Range | Tooltip numbers for that rank | Verdict |
|---|---|---|---|---|---|---|
| P1 | **Power Word: Shield, rank 4** | **24** (D) | **175 mana** (D) | Instant / **4 s CD** / 40 yd (D) | "Draws on the soul of the party member to shield them, **absorbing 226 damage**. Lasts 30 sec. While the shield holds, **spellcasting will not be interrupted by damage**. Once shielded, the target cannot be shielded again for **15 sec**." (D — wow4ever per-spell page; rank 1 = 44, r2 = 86, r3 = 154, r4 = 226, matching vanilla's 44/86/154/226 progression) | **(a) essential** |
| P2 | **Fade, rank 2** | **20** (D) | **75 mana** (D) | Instant / **30 s CD** / self (D) | "Fade out, discouraging enemies from attacking you for **10 sec**." Magnitude not in tooltip; Warcraft Tavern's Classic threat table lists rank 1 as **−55 threat** (I/U — rank 2 value not verified) | **(a) essential** (healer aggro dump) |
| P3 | **Flash Heal, rank 1** | **20** (D) | `U` | **1.5 s cast** / — / 40 yd (D) | Fast heal; **rank-1 heal amount and mana cost not captured in this pass** (`U`). Known from the same table: rank 7 heals 825 (Forever) / Classic max rank 812–958 (D) | **(a) essential** (tank spike heal) |
| P4 | **Dispel Magic, rank 1** | **18** (D) | **18% of base mana** (D) | Instant / — / 30 yd (D) | "Dispels magic on the target, removing **1 harmful spell** from a friend or **1 beneficial spell** from an enemy." Rank 2 at 36 (out) | **(a) essential** for 5-man (elite magic debuffs) |
| P5 | **Inner Fire, rank 2** | **20** (D) | **65 mana** (D) | Instant / — / self (D) | Armor buff, 10 min or 20 charges; **rank-2 armor value not printed** (`U`; the max rank line reads "increasing armor by 1395") | **(b) useful** (priest survivability) |
| P6 | **Mind Blast, rank 3** | **22** (D) | **110 mana** (D) | **1.5 s cast / 8 s CD** / 30 yd (D) | Shadow nuke, **rank-3 damage not captured** (`U`). Contradiction recorded below (90 vs 110 mana) | **(c) marginal** — threat-heavy nuke on a healer |
| P7 | **Holy Fire, rank 2** | **24** (D) | **95 mana** (D) | **3.5 s cast** / — / 30 yd (D) | Damage + DoT; one secondary source gives **56–70 + 25 over 10 s** (U, single source, not the rank table) | **(c) marginal** |
| P8 | **Shackle Undead, rank 1** | **20** (D) | **90 mana** (D) | **1.5 s cast** / — / 30 yd (D) | Undead-only CC ("unable to move, attack or cast", breaks on damage, one target); rank 1 duration **20 s** on the rank table, Classic max rank 50 s (D/I) | **(b) useful** (undead pulls) |
| P9 | **Psychic Scream, rank 1** | **14** (D) | **100 mana** (D) | Instant / **30 s CD** / 8 yd (D) | "causing **2 enemies** within 8 yards to **flee for 8 sec**" (D) | **(b) useful** as an emergency disengage; risky in 5-mans |
| P10 | **Resurrection, rank 2** | **22** (D) | **75% of base mana** (D) | **10 s cast** / — / 30 yd (D) | Out-of-combat res (cannot be cast in combat) (D) | **(b) useful** if the sim models deaths |
| P11 | **Mana Burn, rank 1** | **24** (D) | **95 mana** (D) | **3 s cast** / — / 30 yd (D) | Burns enemy mana, damage = mana burned; magnitude per rank `U` | **(c) marginal** at 24 (PvP tool) |
| P12 | **Cure Disease, rank 1** | **14** (D) | **15% of base mana** (D) | Instant / — / 30 yd (D) | Removes 1 disease (D) | **(c) situational** |
| P13 | **Desperate Prayer, rank 1** | **10** (D) | none (D) | Instant / **10 min CD** / self (D) | Instant self-heal; magnitude `U`. Racial (Human/Dwarf priest) — availability depends on race choice | **(c) marginal** unless the sim models priest self-saves |

**Out of reach at 24 (verified levels):** Flash Heal rank 2 (26), Mind Blast rank 4 (28), Psychic Scream
rank 2 (28), Inner Fire rank 3 (30), Resurrection rank 3 (34), Shackle Undead rank 2 (40), Feedback rank 2+,
Divine Spirit (30), Prayer of Healing (`U`), Power Word: Shield rank 5 (30). **Feedback rank 1 is listed at
level 20** by the spellbook but it is a Discipline *talent* in 1.12, so it is talent-gated rather than
trainable — treated as out of scope (`U` on whether the sim should model talents at all).

## 2. Combat Dagger Rogue

| # | Ability (highest rank ≤24) | Learned | Cost | Cast / CD / Range | Tooltip numbers for that rank | Verdict |
|---|---|---|---|---|---|---|
| R1 | **Sinister Strike, rank 4** | **22** (D, classicdb spell 1759) | **45 energy** (D) | Instant / — / 5 yd | "An instant strike that causes **15 damage in addition to your normal weapon damage**. Awards 1 combo point." (D) | **(a) essential** — the only reliable combo-point builder for a dagger build |
| R2 | **Eviscerate, rank 4** | **24** (D, classicdb spell 6762) | **35 energy** (D) | Instant / — / 5 yd | **1 cp 41–61 · 2 cp 72–92 · 3 cp 103–123 · 4 cp 134–154 · 5 cp 165–185** (D) | **(a) essential** — the finisher |
| R3 | **Slice and Dice, rank 1** | **10** (D) | **25 energy** (D) | Instant / — / self | "Finishing move that increases melee attack speed by **20%**"; duration 9 s (1 cp) → **21 s** (5 cp) (D) | **(b) useful** — sustained elite DPS; competes with Eviscerate for combo points |
| R4 | **Evasion, rank 1** | **8** (D) | none (D) | Instant / **5 min CD** / self (D) | "The rogue's **dodge chance will increase by 50%** for **15 sec**." (D) | **(b) useful** — off-tanking adds / surviving cleaves |
| R5 | **Sap, rank 1** | **10** (D) | **65 energy** (D) | Instant / — / 5 yd (D) | "Incapacitates the target for up to **25 sec**. Only works on **Humanoids** not in combat… Does not break Stealth." (D) | **(b) useful** if the sim models pulls |
| R6 | **Expose Armor, rank 1** | **14** (D) | **25 energy** (D) | Instant / — / 5 yd (D) | Finisher reducing armor per combo point; rank-1 table row reads "1 point: **340 armor**" (D for the number, `U` whether that is the rank-1 or max-rank scale — max rank listed as 1700) | **(c) marginal here** — does not stack with the warrior's Sunder Armor in vanilla (I) |
| R7 | **Garrote, rank 2** | **22** (D) | **50 energy** (D) | Instant / — / 5 yd (D) | "causing **204 damage over 18 sec**, increased by your Attack Power. Must be stealthed and behind the target. Awards 1 combo point." (D) | **(b) useful** — best opener available at 24 for a fight longer than a few seconds |
| R8 | **Ambush, rank 1** | **18** (D) | **60 energy** (D) | Instant / — / 5 yd (D) | "causing **250% weapon damage plus 290**… Requires a dagger in the main hand." (D) | **(b) useful** — dagger opener, front-loads damage |
| R9 | **Rupture, rank 1** | **20** (D) | **25 energy** (D) | Instant / — / 5 yd (D) | Bleed finisher scaled per combo point; rank-1 per-point values `U` (not printed in the captured slice) | **(c) marginal** — bleeds are weak on short elite fights |
| R10 | **Feint, rank 1** | **16** (D) | **20 energy** (D) | Instant / **10 s CD** / 5 yd (D) | "lowering your threat by a **small** amount" — magnitude intentionally vague in the tooltip (`U`) | **(c) marginal** — rogues rarely over-threat a Prot warrior |
| R11 | **Vanish, rank 1** | **22** (D) | none (D) | Instant / **5 min CD** / self (D) | "vanish from sight… for 10 sec. Also breaks movement impairing effects" (D) | **(c) marginal** in a fight-only sim; **(b)** if wipe recovery matters |
| R12 | **Sprint, rank 1** | **10** (D) | none (D) | Instant / **5 min CD** / self (D) | "**+50%** movement speed for **15 sec**. Does not break stealth." (D) | **(c) marginal** |
| R13 | **Distract, rank 1** | **22** (D) | **30 energy** (D) | Instant / **30 s CD** / 30 yd (D) | Pulls nearby enemies' attention for 10 s; does not break stealth (D) | **(c) marginal** |
| R14 | **Pick Lock** | `U` (16 per trainer lists; not verified this pass) | none | — / — / 5 yd | Utility only (D for existence) | **(c) out of scope** for combat |

**Out of reach at 24 (verified):** Cheap Shot (**26** — no stun opener at 24; rogue.md),
Kick rank 2 (26), Eviscerate rank 5 (32), Gouge rank 3 (32), Backstab rank 4 (28), Kidney Shot (30),
Blade Flurry (30, talent), Cold Blood (talent), Adrenaline Rush (talent), Vanish rank 2 (42).

## 3. Arcane Mage

| # | Ability (highest rank ≤24) | Learned | Cost | Cast / CD / Range | Tooltip numbers for that rank | Verdict |
|---|---|---|---|---|---|---|
| M1 | **Counterspell** | **24** (D) | **100 mana** (D) | **Instant / 30 s CD** / 30 yd (D) | "Counters the enemy's spellcast, preventing any spell from that school of magic from being cast for **10 sec**. Generates a **high amount of threat**." (D) | **(a) essential** — the group's only ranged interrupt that also locks the school for 10 s |
| M2 | **Blink** | **20** (D) | **35% of base mana** (D) | Instant / **15 s CD** / self (D) | "Teleports the caster **20 yards** forward… Also frees the caster from **Stuns and Immobilizes**." (D) | **(b) useful** — escape, reposition, breaks roots |
| M3 | **Frost Nova, rank 1** | **10** (D) | **55 mana** (D) | Instant / **25 s CD** / 10 yd (D) | Roots targets in place for **8 sec** (breaks on damage) (D) | **(b) useful** — add control / kiting, cheap |
| M4 | **Mana Shield, rank 1** | **20** (D) | **40 mana** (D) | Instant / — / self (D) | Absorbs damage using mana (**2 mana per point of damage** — reported for rank 3 by a Wowhead snippet, `U` for rank 1's absorb amount) | **(c) marginal** — mana is the mage's scarcest resource at 1998 total |
| M5 | **Conjure Water, rank 3** | **20** (D) | **180 mana** (D) | **3 s cast** / — / self (D) | Conjures drinkable water (stack size `U`); rank 4 at 30 | **(b) useful** out of combat only — a sim that ignores downtime gets nothing from it |
| M6 | **Flamestrike, rank 2** | **24** (D) | **330 mana** (D) | **3.0 s cast** / — / 30 yd (D) | rank 2: "burning all enemies within the area for **109 Fire damage** and an additional **84 Fire damage over 8 sec**" (D, Forever scale; Classic max rank reads 375–459 + 340 — the mid-rank Classic value is `U`) | **(c) marginal** — 330 mana for ~190 total damage is the worst efficiency on this list |
| M7 | **Dampen Magic, rank 2** | **24** (D) | **200 mana** (D) | Instant / — / 30 yd (D) | Reduces spell damage taken and healing received on the target for 10 min; rank-2 magnitudes `U` (max rank line: −135 damage / −270 healing on the Forever scale, −90/−180 in Classic) | **(b) useful** vs caster elites; expensive |
| M8 | **Remove Curse, rank 1** | **18** (D) | `U` | Instant / — / 30 yd (D) | Removes 1 curse from a friendly target (D) | **(b) useful** situationally |
| M9 | **Fire Ward, rank 1** | **20** (D) | **85 mana** (D) | Instant / **30 s CD** / self (D) | Absorbs Fire damage (amount `U`; disappears when the ward is consumed) | **(c) marginal** (self-only) |
| M10 | **Slow Fall** | **12** (D) | **40 mana** (D) | Instant / — / — (D) | Slows falling speed 30 s (D) | **(c) out of scope** |

**Out of reach at 24 (verified):** Ice Armor (30), Ice Barrier (40, talent), Amplify Magic (30 per a wiki
citation — `U`), Teleport: Stormwind/Ironforge/etc. (20, city utility), Portal (40), Pyroblast (Fire talent),
Arcane Power (talent), Fireball rank 6 (30), Frostbolt rank 5 (26), Arcane Missiles rank 4 (32),
Arcane Explosion rank 3 (30), Blizzard rank 2 (28), Polymorph rank 3 (40).
**Detect Magic:** no entry found in the mage rank table consulted — existence/level `U` (do not add to the
sim without confirming).

## 4. Protection Warrior (sword-and-board)

| # | Ability (highest rank ≤24) | Learned | Cost | Cast / CD / Range | Tooltip numbers for that rank | Verdict |
|---|---|---|---|---|---|---|
| W1 | **Revenge, rank 2** | **24** (D) | **5 rage** (D) | Instant / **5 s CD** / 5 yd (D) | "Instantly counterattack an enemy for **34 damage** and a **high amount of threat**. Revenge must follow a **block, dodge or parry**." (D) | **(a) essential** — cheapest threat-per-rage in the kit, and the reason Shield Block matters |
| W2 | **Shield Block** | **16** (D) | **10 rage** (D) | Instant / **5 s CD** / self (D) | "Increases chance to **block by 75% for 5 sec**, but will only block **1 attack**." (Classic line, D; the Forever variant reads 7 s / 2 attacks) | **(a) essential** — active mitigation *and* it enables Revenge |
| W3 | **Demoralizing Shout, rank 2** | **24** (D) | **10 rage** (D) | Instant / — / 10 yd (D) | Reduces melee AP of all enemies within 10 yd; rank-2 magnitude `U` (Forever rank 2 = −77 AP / 45 s; Classic max rank = −140 AP / 30 s — the two scales disagree, so do not import either mid-rank value blind) | **(a) essential** — cuts incoming tank damage on every mob |
| W4 | **Mocking Blow, rank 1** | **16** (D) | **10 rage** (D) | Instant / **2 min CD** / 5 yd (D) | "A mocking attack that causes **22 damage**, a moderate amount of threat and **forces the target to focus attacks on you for 6 sec**." (D) | **(b) useful** — second taunt when Taunt is on cooldown/immune |
| W5 | **Bloodrage** | **10** (D) | health, not rage (D) | Instant / **1 min CD** / self (D) | "Generates **10 rage** at the cost of health, and then generates an additional **10 rage over 10 sec**." (D) | **(b) useful** — rage on pull without a Charge window |
| W6 | **Cleave, rank 1** | **20** (D) | **20 rage** (D) | Instant / — / 5 yd (D) | "weapon damage **plus 5** to the target and a second nearby enemy" (D) | **(b) useful** on 2-target pulls; bad threat-per-rage single target |
| W7 | **Thunder Clap, rank 2** | **18** (D) | **20 rage** (D) | Instant / **6 s CD** / 4 targets (D) | "increasing the time between their attacks by **20%** for **14 sec** and doing **23 damage**" (Forever scale; the Classic line states **10%** at max rank — mid-rank Classic % `U`) | **(b) useful** — AoE attack-speed debuff cuts group damage taken |
| W8 | **Rend, rank 3** | **20** (D) | **10 rage** (D) | Instant / — / 5 yd (D) | "bleed for **45 damage over 15 sec**" (D) | **(c) marginal** for a tank (no threat rider, breaks no CC but does break Sap/Gouge/Poly-adjacent plans) |
| W9 | **Hamstring, rank 1** | **8** (D) | **10 rage** (D) | Instant / — / 5 yd (D) | "causing **5 damage** and slowing the enemy's movement by **40%** for 15 sec" (D) | **(c) marginal** in a static sim; **(b)** if mobs flee |
| W10 | **Overpower, rank 1** | **12** (D) | **5 rage** (D) | Instant / **5 s CD** / 5 yd (D) | "causing weapon damage **plus 5**… Only useable after the target **dodges**. Cannot be blocked, dodged or parried." (D) | **(c) marginal** — needs Battle Stance (loses Defensive Stance threat/damage reduction) |
| W11 | **Intimidating Shout** | **22** (D) | **25 rage** (D) | Instant / **3 min CD** / 10 yd (D) | "causing the targeted enemy to **cower in fear**; up to **5 enemies** within 8 yds will **flee in fear** … **8 sec**" (D) | **(c) risky** — AoE fear in a 5-man pulls extra packs |
| W12 | **Retaliation** | **20** (D) | none (D) | Instant / **15 min CD** / self (D) | "counterattack any enemy that strikes you in melee for **15 sec**… maximum of **30 attacks**" (D) | **(c) marginal** — 15 min CD |
| W13 | **Slam, rank 1** | **20** per the rank table, **30** per `warrior.md`'s wiki-derived list — **contested** | **15 rage** (D) | **1.5 s cast** (D) | "causing weapon damage **plus 16**" (D) | **(c) marginal** for Prot even if legal: it is a channelled 1.5 s cast that replaces auto-attacks |

**Out of reach at 24 (verified, both sources agree):** Shield Wall (**28**), Challenging Shout (26),
Berserker Rage (30), Berserker Stance (30), Intercept (30), Pummel (**38**), Whirlwind (36),
Shield Slam (**40**, talent), Concussion Blow (talent), Last Stand (talent), Recklessness (50).

## 5. Restoration / Balance Druid

| # | Ability (highest rank ≤24) | Learned | Cost | Cast / CD / Range | Tooltip numbers for that rank | Verdict |
|---|---|---|---|---|---|---|
| D1 | **Faerie Fire, rank 1** | **18** (D) | **55 mana** (D) | Instant / — / 30 yd (D) | Decreases target armor (rank-1 magnitude `U`; max rank = 505 armor, and it prevents stealth/invisibility) | **(a) essential** — free-ish armor debuff that raises the whole group's physical DPS |
| D2 | **Thorns, rank 3** | **24** (D) | **105 mana** (D) | Instant / — / 30 yd (D) | Reflects Nature damage to attackers for 10 min; **rank-3 damage value not printed on the source** (`U`) | **(a) essential** for a tank-healer hybrid packing AoE threat |
| D3 | **Hibernate, rank 1** | **18** (D) | **90 mana** (D) | **1.5 s cast** / — / 30 yd (D) | Sleeps Beasts/Dragonkin for up to **20 sec**, breaks on damage, one target (D) | **(b) useful** CC |
| D4 | **Remove Curse** | **24** (D) | **10% of base mana** (D) | Instant / — / 30 yd (D) | "Dispels **1 Curse** (Classic line) / 1 Curse or Bane from a friendly target" (D) | **(b) useful**; exactly at the level cap |
| D5 | **Rebirth, rank 1** | **20** (D) | **85% of base mana** (D) | **2 s cast / 30 min CD** / 30 yd (D) | **In-combat** resurrection (unique to druids); restored-hp/mana per rank `U` | **(b) useful** if the sim models deaths mid-fight |
| D6 | **Revive, rank 2** | **24** (D) | **75% of base mana** (D) | **10 s cast** / — / 30 yd (D) | Out-of-combat res (D) | **(c) marginal** (out of combat only) |
| D7 | **Cure Poison** | **14** (D) | **16% of base mana** (D) | Instant / — / 30 yd (D) | Cures 1 poison (D) | **(c) situational** |
| D8 | **Starfire, rank 1** | **20** (D) | **95 mana** (D) | **3.5 s cast** / — / 30 yd (D) | Arcane nuke; rank-1 damage `U` (not in the captured slice) | **(c) marginal** — mana-inefficient at 24 |
| D9 | **Bear Form** | **10** (D) | **55% of base mana** (D) | Instant / — / self (D) | "+30 attack power, armor contribution from items **+180%**, health **+180**" (D) | **(c) out of role** for a healer/caster, but it is the emergency-tank button |
| D10 | **Maul, rank 2** | **18** (D) | **15 rage** (D) | Instant / — / 5 yd (D) | "Increases the druid's next attack by ≈31" — rank-2 value `U` (table shows 37/49/71/101/128 for ranks 3–7) | **(c) out of role** |
| D11 | **Swipe, rank 2** | **24** (D) | **20 rage** (D) | Instant / — / 5 yd (D) | "Swipe **3 nearby enemies**, inflicting **25 damage**" (D) | **(c) out of role** (bear AoE threat) |
| D12 | **Enrage** | **12** (D) | none (D) | Instant / **1 min CD** / self (D) | "Instantly generates **10 Rage** and another **20 Rage over 10 sec**, but reduces base armor by **27%** in Bear Form" (D) | **(c) out of role** |
| D13 | **Cat Form / Claw / Shred (rank 1, lvl 20)** | 20 (D) | 55% base mana / 45 / 60 energy (D) | melee (D) | Cat: +12 AP + Agility; Claw 110% + 27; Shred 155% + 22 behind (D) | **(c) out of role** — a Resto/Balance build has no feral damage talents |

**Out of reach at 24 (verified):** Abolish Poison (**26**), Travel Form (**30**), Tranquility (30 per
secondary sources — `U`), Barkskin (**44**), Innervate (**40**, talent), Nature's Swiftness / Nature's Grasp
(talents), Pounce (36), Dash (talent), Moonkin Form (40), Hurricane (**40**), Insect Swarm (talent, 20),
Starfire rank 2 (26).

---

## Consolidated priority order (all five specs, highest gameplay impact first)

Ranking logic: does the ability (1) keep the tank alive, (2) improve healer mana efficiency, (3) fix threat
(build or dump), (4) interrupt, (5) raise 5-man damage on elites. Abilities that are only situational,
out-of-role, or gated behind a contested level sit lower.

| # | Ability | Class | Lvl | Why (≤12 words) |
|---|---|---|---|---|
| 1 | Power Word: Shield r4 | Priest | 24 | 226-absorb on tank, blocks spell pushback |
| 2 | Revenge r2 | Warrior | 24 | 5-rage high-threat strike; core prot rotation |
| 3 | Fade r2 | Priest | 20 | Healer threat dump prevents wipes |
| 4 | Flash Heal r1 | Priest | 20 | 1.5 s tank-save heal, cheap on mana |
| 5 | Sinister Strike r4 | Rogue | 22 | Combo-point engine for dagger DPS |
| 6 | Eviscerate r4 | Rogue | 24 | 165–185 finisher at 5 CP |
| 7 | Shield Block | Warrior | 16 | Active mitigation that also enables Revenge |
| 8 | Counterspell | Mage | 24 | Interrupt plus 10 s school lockout |
| 9 | Dispel Magic r1 | Priest | 18 | Removes elite magic debuffs off the party |
| 10 | Demoralizing Shout r2 | Warrior | 24 | AP debuff lowers incoming tank damage |
| 11 | Slice and Dice r1 | Rogue | 10 | +20% attack speed, sustained elite damage |
| 12 | Faerie Fire r1 | Druid | 18 | Armor debuff raises whole group's DPS |
| 13 | Thorns r3 | Druid | 24 | Reflect damage while the tank is hit |
| 14 | Frost Nova r1 | Mage | 10 | Cheap root for add control |
| 15 | Blink | Mage | 20 | Escape, repositions, breaks roots |
| 16 | Inner Fire r2 | Priest | 20 | Armor buff keeps the healer alive |
| 17 | Garrote r2 | Rogue | 22 | Best sustained-damage opener available, 204/18 s |
| 18 | Ambush r1 | Rogue | 18 | Dagger opener, 250% + 290 front-loaded |
| 19 | Mocking Blow r1 | Warrior | 16 | Backup taunt on 2 min cooldown |
| 20 | Bloodrage | Warrior | 10 | Instant pull rage without a charge |
| 21 | Evasion r1 | Rogue | 8 | +50% dodge for off-tanking or cleaves |
| 22 | Dampen Magic r2 | Mage | 24 | Cuts spell damage on tank, costly mana |
| 23 | Hibernate r1 | Druid | 18 | 20 s single-target CC vs beasts |
| 24 | Mind Blast r3 | Priest | 22 | Burst damage, but heavy healer threat |
| 25 | Shackle Undead r1 | Priest | 20 | Undead-only CC for specific pulls |
| 26 | Sap r1 | Rogue | 10 | Pre-pull humanoid removal, 25 s |
| 27 | Thunder Clap r2 | Warrior | 18 | AoE attack-speed slow cuts group damage |
| 28 | Psychic Scream r1 | Priest | 14 | Emergency disengage; risks extra packs |
| 29 | Cleave r1 | Warrior | 20 | Two-target rage dump, weak single-target threat |
| 30 | Remove Curse (Druid) | Druid | 24 | Curse removal, exactly at cap |
| 31 | Rebirth r1 | Druid | 20 | Only in-combat battle-res in the game |
| 32 | Remove Curse (Mage) | Mage | 18 | Situational curse dispel |
| 33 | Resurrection r2 / Revive r2 | Priest / Druid | 22 / 24 | Recovery if deaths are modelled |
| 34 | Conjure Water r3 | Mage | 20 | Out-of-combat regen only |
| 35 | Feint r1 | Rogue | 16 | Threat drop, rarely needed behind a tank |
| 36 | Holy Fire r2 | Priest | 24 | Damage-over-time, bad mana per damage |
| 37 | Mana Shield r1 | Mage | 20 | Spends scarce mana to absorb damage |
| 38 | Intimidating Shout | Warrior | 22 | AoE fear in a 5-man pulls extra mobs |
| 39 | Hamstring r1 | Warrior | 8 | Snare, only matters if mobs flee |
| 40 | Overpower r1 | Warrior | 12 | Dodge punish, but costs the defensive stance |
| 41 | Expose Armor r1 | Rogue | 14 | Armor debuff already covered by Sunder Armor |
| 42 | Flamestrike r2 | Mage | 24 | 330 mana for ~190 damage, worst efficiency |
| 43 | Rupture r1 | Rogue | 20 | Bleed finisher, weak on short fights |
| 44 | Starfire r1 | Druid | 20 | Slow, mana-hungry nuke for a healer build |
| 45 | Mana Burn r1 | Priest | 24 | PvP tool, no PvE value at 24 |
| 46 | Cure Poison / Cure Disease | Druid / Priest | 14 | Situational dispels |
| 47 | Vanish / Sprint / Distract / Pick Lock | Rogue | 22 / 10 / 22 / — | Travel and utility, no combat value |
| 48 | Bear Form / Maul / Swipe / Enrage / Cat Form | Druid | 10 / 18 / 24 / 12 / 20 | Out of role for a Resto/Balance build |
| 49 | Slow Fall / Fire Ward / Retaliation / Desperate Prayer | Mage / Warrior / Priest | 12 / 20 / 20 / 10 | Very narrow or very long cooldowns |
| 50 | Slam r1 | Warrior | 20 or 30 (contested) | Cast-time filler, poor for a shield tank |

Rows 1–13 are the ones that change sim outcomes; 14–27 are noticeable improvements; 28–50 are polish.

## Contradictions recorded (not resolved)

1. **Slam's learn level.** `wow4ever.quest` rank table: **rank 1 at level 20** (15 rage, 1.5 s cast, "+16",
   and an 18 s cooldown that is a Classic-plus change) — `warrior.md` §7, citing
   `warcraft.wiki.gg/wiki/Warrior_abilities_(Classic)`, lists Slam as **level 30 → not available at 24**.
   The level is decision-relevant (availability) but not gameplay-relevant (Slam is poor for Prot), so it is
   ranked last either way. No third source settled it in this pass.
2. **Mind Blast rank 3 mana.** `wow4ever` rank table: **110 mana**; a Wowhead-snippet answer in this session
   said **90 mana** and gave the level as 22. Level agrees (22), mana conflicts.
3. **Demoralizing Shout scale.** Forever rank 5 = −196 AP/45 s, its "In Classic" line = −140 AP/30 s.
   Since the max ranks disagree, the rank-2 Classic value cannot be derived from either → `U`.
4. **Thunder Clap slow.** Forever tooltips: "+20% time between attacks" up to 30 s; the "In Classic" line
   states **10%** at max rank. Rank-2 Classic value `U`.
5. **Shield Block duration/targets.** Forever: 7 s, 2 attacks. Classic line: **5 s, 1 attack**. This file
   uses the Classic line.
6. **Bloodrage cost field.** `wow4ever` prints "20% of base Power" as the cost while the tooltip says it
   generates rage "at the cost of health" — the cost field is a bad data mapping; there is no rage cost.
7. **Shield Slam / Revenge wording.** Forever and Classic tooltips also disagree on the *basis* of damage
   (flat vs. 81–99 range); recorded because the same data set supplied the numbers above.
8. **Power Word: Shield last rank.** Forever 928 absorb vs Classic 942; ranks 1–4 (44/86/154/226) agree, so
   the level-24 value used here (226) is safe.
9. **Amplify Magic level** — asserted as 30 by a wiki-citing search answer, `U`; not added to any table.

## Missing evidence

- **Priest:** Flash Heal rank 1 mana + heal amount; Mind Blast rank 3 damage; Holy Fire rank 2 exact numbers;
  Inner Fire rank 2 armor; Holy Fire / Feedback talent-vs-trainable status in 1.12; Desperate Prayer magnitude
  and its race restriction in Classic Era.
- **Rogue:** Rupture rank 1 per-point damage; Feint rank 1 threat magnitude (tooltip is deliberately vague);
  Expose Armor rank-1 vs max-rank armor table; Pick Lock learn level.
- **Mage:** Mana Shield rank 1 absorb; Dampen Magic rank 2 percentages; Remove Curse cost (printed as `%` on
  the spell page, not captured); Conjure Water stack size; **whether Detect Magic exists as a trainable arcane
  spell in 1.12** (absent from the rank table consulted).
- **Warrior:** Demoralizing Shout rank 2, Thunder Clap rank 2, and Shield Wall/Shield Block *Classic* mid-rank
  magnitudes (see contradictions 3–5); Slam learn level (contradiction 1).
- **Druid:** Thorns rank 3 damage per hit (the rank table prints "?" for every rank); Faerie Fire rank 1 armor
  reduction; Maul rank 2 bonus; Starfire rank 1 damage; Rebirth rank 1 restored hp/mana; Tranquility learn
  level.
- **Cross-cutting:** all "forever-scale vs Classic-scale" mid-rank magnitudes. The `wow4ever` tables are the
  only per-rank source that was fetchable end-to-end this session; Classicdb (`classicdb.ch/?spell=<id>`) is
  1.12-exact and is what `rogue.md`/`druid.md` used, but it needs a spell **id** per ability and those ids
  were only on disk for the already-anchored spells.
- **Validation limitation:** `source_check` was not used for these claims (no registered provider available
  in this subagent's tool set that returned passages), and `wowhead.com/classic` spell pages are not
  extractable (documented in `mage.md`). Numbers therefore rest on fetched rank tables plus search-answer
  snippets, which is why every uncertain figure is marked `U` rather than filled in.

## Sources

- **Kept — `wow4ever.quest/en/spellbook/{priest,mage,warrior,druid,rogue}`** (fetched in full; per-rank
  level / cost / cast / cooldown / range / tooltip tables + "In Classic" comparison lines). Primary numeric
  source for every table above. Corpus example pages: `/en/spellbook/priest-power-word-shield`,
  `/en/spellbook/warrior-revenge`, `/en/spellbook/mage-counterspell`, `/en/spellbook/druid-thorns`.
- **Kept — `.pi/research/level24/warrior.md`** (on disk): level-24 warrior ability table with ranks, rage
  costs, cooldowns, Learn-levels for the whole warrior kit, and the "not available at 24" list.
- **Kept — `.pi/research/level24/rogue.md`** (on disk): classicdb-sourced rank data for Backstab, Sinister
  Strike, Eviscerate, Kick, Gouge, Cheap Shot; used verbatim for R1/R2.
- **Kept — `.pi/research/level24/mage.md`** (on disk): level-24 mage spell table (ranks, cost, cast, values).
- **Kept — `.pi/research/level24/druid.md`** (on disk): twinhead-1.12 tooltip table + availability notes
  (Remove Curse at 24; Abolish Poison 26; Travel Form 30; Innervate 40; Starfire 20).
- **Kept — `classicdb.ch/?spell=<id>`** (via `rogue.md`): 1.12-exact ranks and costs; the check that
  independently confirms Sinister Strike r4 @22 / 45 energy and Eviscerate r4 @24 / 35 energy.
- **Kept — `warcraft.wiki.gg/wiki/Warrior_abilities_(Classic)`** (via `warrior.md`): learn levels and the
  plate-at-40 / stance gates.
- **Kept (secondary) — `foreverdb.net/spell/586` (Fade r1 −55 threat via Warcraft Tavern threat table),
  `warcrafttavern.com/wow-classic/guides/threat-guide-reference-table/`** — only threat magnitudes found.
- **Rejected — `wowhead.com/classic/spell=<id>`**: not extractable (JS-rendered); used only through
  search-result snippets, and every such number is marked `U` in-line.
- **Rejected — `wow4ever.com` / `www.wow4ever.com`**: fetch aborted repeatedly this session (the working
  host is `wow4ever.quest`).
- **Rejected — `warcraft.wiki.gg` per-ability `?action=raw` pages**: 404 for the `(Classic)`-suffixed titles
  and the plain-title wikitext returned no rank tables to the extractor.
- **Deprioritized — 21/31-point talent abilities** (Shield Slam, Last Stand, Cold Blood, Blade Flurry,
  Adrenaline Rush, Innervate, Nature's Swiftness, Ice Barrier, Pyroblast): out of reach at level 24 by talent
  points as well as level.

## Next steps

1. Fill the `U` cells from `classicdb.ch/?spell=<id>` one ability at a time (ids can be harvested from the
   now-fetched `wow4ever` pages, which link a slug per spell) — that alone would make every row `D`.
2. Decide two policy questions for the sim: does it model **stances** (Battle vs Defensive) and **talents**?
   Overpower and Slam only matter in Battle Stance; Shield Slam/Pyroblast/Innervate only matter as talents.
3. If the sim models pulls and CC, raise Sap / Shackle Undead / Hibernate / Frost Nova; if it models only
   sustained single-target fights, drop them below row 40.
