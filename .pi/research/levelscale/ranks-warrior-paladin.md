# Warrior and Paladin rank ladders (levels 1–41, vanilla 1.12 / Classic Era 1.15.9)

Scope: exactly the 14 abilities the sim needs — Warrior (rage) Taunt, Heroic Strike, Sunder Armor,
Shield Bash, Shield Block, Shield Wall, Charge, Disarm, Execute; Paladin (mana) Flash of Light,
Holy Light, Judgement, Hammer of Justice, Divine Protection. Every rank whose **trained-at level**
is <= 41, oldest first. The sim picks the newest rank at or below the character's level, so the
`Level` column is the load-bearing one.

Conventions:
- `Cast`, `CD` in seconds (0 = instant, no cooldown). `Range` in **tiles, 40 yd = 8 tiles** (5 yd = 1,
  10 yd = 2); Charge's 8–25 yd band is 1.6–5; self-only is 0.
- `Conf` = `M` when the ability was read on two independent hosts (`database.emberveil.org` and
  `nether.wowhead.com/classic/tooltip/spell/<id>`), `U` when single-sourced.
- Edition: both hosts were read. Emberveil serves a 1.12 dump; Wowhead Classic serves the 1.15.9
  client. Warrior numbers are identical on both. Paladin heals differ systematically between the two
  editions (see Contradictions) and the table lists the **1.15.9 client** values, which is the edition
  `src/WowRoguelike.Core/Content.fs` already follows for Holy Light / Flash of Light
  (`Content.fs:299-320`); the 1.12 values are given per rank in Contradictions.
- Flat threat numbers (`+45`, `+118`) come from the 1.12 client's threat rows and exist on one host
  only; the "causes a high amount of threat" wording is on both. Ability level/cost/effect were
  double-sourced for every row.

## Taunt — Warrior (Rage)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 10 | 0 | 0 | 10 | 1 | forces target to attack you for 3 s; no effect if it is already attacking you; no rage cost; needs Defensive Stance | M |

Spell id: 355.

## Heroic Strike — Warrior (Rage)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 15 | 0 | 0 | 1 | WeaponDamage +11 flat, replaces next swing, extra threat (+20 flat) | M |
| 2 | 8 | 15 | 0 | 0 | 1 | WeaponDamage +21 flat, extra threat (+39 flat) | M |
| 3 | 16 | 15 | 0 | 0 | 1 | WeaponDamage +32 flat, extra threat (+59 flat) | M |
| 4 | 24 | 15 | 0 | 0 | 1 | WeaponDamage +44 flat, extra threat (+78 flat) | M |
| 5 | 32 | 15 | 0 | 0 | 1 | WeaponDamage +58 flat, extra threat (+98 flat) | M |
| 6 | 40 | 15 | 0 | 0 | 1 | WeaponDamage +80 flat, extra threat (+118 flat) | M |

Spell ids: 78, 284, 285, 1608, 11564, 11565 (rank 7 = 11566 at 48, out of range).

## Sunder Armor — Warrior (Rage)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 10 | 15 | 0 | 0 | 1 | armor -90 per stack, 5 stacks, 30 s, extra threat (+45 flat) | M |
| 2 | 22 | 15 | 0 | 0 | 1 | armor -180 per stack, 5 stacks, 30 s, extra threat (+99 flat) | M |
| 3 | 34 | 15 | 0 | 0 | 1 | armor -270 per stack, 5 stacks, 30 s, extra threat (+153 flat) | M |

Spell ids: 7386, 7405, 8380 (rank 4 = 11596 at 46, out of range).

## Shield Bash — Warrior (Rage)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 12 | 10 | 0 | 12 | 1 | direct 6, interrupt + 6 s school lockout, needs a shield | M |
| 2 | 32 | 10 | 0 | 12 | 1 | direct 18, interrupt + 6 s school lockout, needs a shield | M |

Spell ids: 72, 1671 (rank 3 = 1672 at 52, out of range). Both hosts' tooltip: "prevents any spell in
that school from being cast for 6 sec" — the lockout is **6 s**, not 8 s.

## Shield Block — Warrior (Rage)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 16 | 10 | 0 | 5 | 0 | +75% block chance for 5 s, but only 1 attack; needs a shield and Defensive Stance | M |

Spell id: 2565. Duration is **5 s**, not 10 s.

## Shield Wall — Warrior (Rage)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 28 | 0 | 0 | 1800 | 0 | -75% damage taken from melee, ranged and spells for 10 s; needs Defensive Stance | M |

Spell id: 871. Free (no rage), 30 min cooldown, one rank at every level.

## Charge — Warrior (Rage)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 4 | 0 | 0 | 15 | 1.6–5 | charge 8–25 yd, stun 1 s, +9 rage generated, no damage, out of combat only, needs Battle Stance | M |
| 2 | 26 | 0 | 0 | 15 | 1.6–5 | charge 8–25 yd, stun 1 s, +12 rage generated, no damage, out of combat only, needs Battle Stance | M |

Spell ids: 100, 6178 (rank 3 = 11578 at 46, out of range). The stun is a separate 1 s spell (7922).
Client data stores the rage gain as effect value 90/120/150 in 0.1-rage units; both hosts' tooltips say
9 / 12 rage.

## Disarm — Warrior (Rage)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 18 | 20 | 0 | 60 | 1 | disarms the target's weapon for 10 s; needs Defensive Stance | M |

Spell id: 676.

## Execute — Warrior (Rage)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 24 | 15 + all | 0 | 0 | 1 | direct 125, +3 damage per extra rage point, target at <=20% health | M |
| 2 | 32 | 15 + all | 0 | 0 | 1 | direct 200, +6 damage per extra rage point, target at <=20% health | M |
| 3 | 40 | 15 + all | 0 | 0 | 1 | direct 325, +9 damage per extra rage point, target at <=20% health | M |

Spell ids: 5308, 20658, 20660 (rank 4 = 20661 at 48, out of range). Costs 15 rage plus every remaining
point; the per-rage bonus comes from the triggered spell 26651.

## Flash of Light — Paladin (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 20 | 35 | 1.5 | 0 | 8 | heal 67-77 | M |
| 2 | 26 | 50 | 1.5 | 0 | 8 | heal 102-117 | M |
| 3 | 34 | 70 | 1.5 | 0 | 8 | heal 153-171 | M |

Spell ids: 19750, 19939, 19940 (rank 4 = 19941 at 42, out of range).

## Holy Light — Paladin (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 35 | 2.5 | 0 | 8 | heal 42-51 | M |
| 2 | 6 | 60 | 2.5 | 0 | 8 | heal 81-96 | M |
| 3 | 14 | 110 | 2.5 | 0 | 8 | heal 167-196 | M |
| 4 | 22 | 190 | 2.5 | 0 | 8 | heal 322-368 | M |
| 5 | 30 | 275 | 2.5 | 0 | 8 | heal 506-569 | M |
| 6 | 38 | 365 | 2.5 | 0 | 8 | heal 717-799 | M |

Spell ids: 635, 639, 647, 1026, 1042, 3472 (rank 7 is level 46, out of range).

## Judgement — Paladin (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 4 | 6% base | 0 | 10 | 2 | unleashes the active Seal on the target; no damage of its own (Seal of Righteousness r3 judge = 53-57 Holy) | M |

Spell id: 20271. One rank for the whole game; all damage comes from the seal's own Judgement spell
(Seal of Righteousness judges are 20187/20280/20281/20282/20283 at seal ranks 1/2/3/4/5, levels 1/10/
18/26/34). Cost is a percentage (6% of base mana), so it is not a fixed number.

## Hammer of Justice — Paladin (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 8 | 30 | 0 | 60 | 2 | stun 3 s | M |
| 2 | 24 | 50 | 0 | 60 | 2 | stun 4 s | M |
| 3 | 40 | 75 | 0 | 60 | 2 | stun 5 s | M |

Spell ids: 853, 5588, 5589 (rank 4 is level 54, out of range).

## Divine Protection — Paladin (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 6 | 15 | 0 | 300 | 0 | immune to all physical attacks and spells for 6 s, cannot attack or use physical abilities, 1 min forbearance | M |
| 2 | 18 | 35 | 0 | 300 | 0 | immune to all physical attacks and spells for 8 s, cannot attack or use physical abilities, 1 min forbearance | M |

Spell ids: 498, 5573.

## Contradictions

1. **Paladin heals: 1.12 database vs 1.15.9 client.** Emberveil (1.12 dump) gives Flash of Light
   62-72 / 96-110 / 145-163 and Holy Light 39-47 / 76-90 / 159-187 / 310-356 / 491-553 / 698-780.
   Wowhead Classic (1.15.9) gives the values in the tables above (67-77 / 102-117 / 153-171 and
   42-51 / 81-96 / 167-196 / 322-368 / 506-569 / 717-799). The gap is systematic (~2% at high ranks,
   ~8% at rank 1), so it is a Classic-Era tuning pass, not a typo; `.pi/research/level24/paladin.md`
   §7.1 records the same split against classicdb.ch/wowcalc.net. Both columns are double-sourced, so
   every heal row stays `M` — pick one edition and stay in it. Current sim code uses 1.15.9.
2. **Sunder Armor rank 1 level: 10 vs 16.** Both hosts say **10** (spell 7386, `Requires level 10`),
   and the ladder 10/22/34/46 agrees. `.pi/research/levelscale/warrior-rogue-ladder.md` Table W1
   carries a note that `warcraft.wiki.gg/wiki/Sunder_Armor` listed "Level learned 16"; wiki.gg
   answered Cloudflare 403 to every fetch this run, so that claim is neither confirmed nor refuted
   here — but two independent hosts at 10 make 10 the value to use. W1 also calls it a "class quest,
   not trainer" spell: the client's trainer list for 7386 is empty, yet it is empty for auto-learned
   rank 1s too (Heroic Strike r1, Holy Light r1), so "quest-taught" is **not** established.
3. **Taunt cost.** Both hosts show no resource cost (Wowhead's tooltip has no cost line; the client row
   has no power type). W1's "10 rage (U)" is wrong; `Content.fs:215-222` still charges 10 rage.
4. **Shield Bash lockout duration.** Both hosts: 6 s ("prevents any spell in that school from being
   cast for 6 sec"). W1 says 8 s — wrong. Spell 18498 ("Shield Bash - Silenced") is a rogue-flavoured
   placeholder row with no duration and is not the lockout aura.
5. **Shield Block duration.** 5 s, and it blocks only 1 attack (both hosts). Any 10 s figure is wrong;
   W1's 5 s is right.
6. **Disarm cost.** 20 rage (both hosts). Older secondary sources quoted 10 rage; W1's own note flags
   this, and 20 is the sourced value.
7. **Shield Wall cooldown.** 30 min (both hosts). Free, i.e. 0 rage — W1's "0" is right; one older
   source's "5 min" is wrong.
8. **Duplicate spell rows that are not player ranks.** Shield Wall 29061 (20 s, skill line null),
   Shield Block 12169 (instant, no cooldown), Taunt 29060 (1 min CD, 20 s), Taunt 26281 (level 60,
   5 s CD), Taunt 8133 / Judgement 3684 / Divine Protection 735, 3697 ("Teaches ... (Rank N)"),
   Hammer of Justice 13005 and Divine Protection 13007 (level-26/18 NPC copies with no cooldown), and
   the whole mana-costed Heroic Strike family (1605-1611, 11570-11571, 25354, 25958) are creature or
   teaching spells. Filter on a non-null skill line (`Warrior — Arms/Protection`, `Paladin — Holy`)
   to keep only player ranks.

## Gaps

- **warcraft.wiki.gg unavailable** (403 on every page and on `api.php`, Cloudflare interstitial), so
  the third cross-check the task allows was not run; the Sunder Armor "level 16" dispute therefore
  rests on the two hosts agreeing at 10.
- **No rank above 41 is tabulated** except as a comment next to each table (Heroic Strike 7, Sunder
  Armor 4, Shield Bash 3, Charge 3, Execute 4, Flash of Light 4, Holy Light 7, Hammer of Justice 4).
  Those levels come from the emberveil record alone — no Wowhead tooltip was fetched for them, and
  their numbers, ids and costs were not verified.
- **Judgement's damage is seal-dependent and was not re-verified**: the 53-57 figure is the
  Seal-of-Righteousness-rank-3 judge from `.pi/research/level24/paladin.md` §4 (1.12 numbers); the
  per-seal Judgement spells were not fetched this run, and the 1.15.9 seal-judge values are unknown.
- **Percent-based costs stay unquantified**: Judgement is 6% of base mana; no flat mana figure is
  given, because base mana depends on the character's level and Intellect.
- **Threat rows are single-host.** All `extra threat (+N)` values come from the 1.12 client dump on
  emberveil; neither Wowhead tooltip nor any other reachable page prints threat numbers.
- **Defensive-stance / shield requirements** come from Wowhead's tooltip "Requires ..." lines; the
  emberveil record does not expose them, so a wrongly required stance/shield would go unnoticed.
- **Divine Protection's 1 min forbearance** is tooltip text on both hosts; the shared cooldown with
  Divine Shield / Blessing of Protection is not modelled or verified.

## Sim alignment (source vs `src/WowRoguelike.Core/Content.fs`)

- `Content.fs:214-222` Taunt: `ResourceCost = 10` (source: 0) and `Range = 8` (source: 1 tile, 5 yd).
- `Content.fs:329-337` Judgement: `Range = 1` (source: 2 tiles, 10 yd).
- `Content.fs:301-327` Holy Light r4 and `Content.fs:313-325` Flash of Light r1 already carry the
  1.15.9 values tabulated above (the code's comments note both editions).

Those three rows are the only places where the code and the two hosts disagree; the rest of the sim's
Warrior and Paladin records match the tables here.
