# Research: verified rank ladders, Priest / Druid / Mage, levels 1–41

Scope: **only** the 18 abilities named in the brief. Every rank whose *trained-at* level is ≤ 41
(the sim's maximum), oldest rank first, so a sim that picks "newest rank ≤ character level" has a
complete ladder with no holes. This file supersedes the rank tables in `mage-ladder.md` and
`priest-druid-ladder.md` (both started at level 12–14 and omitted ranks 1–2 of most abilities);
the gear/wand sections of those files are untouched and still valid.

## How each row was produced

Two independent hosts were fetched for **every** rank, and both were read for level, cost, cast
time, range and effect magnitude:

1. `https://database.emberveil.org/spell/<id>` — 1.12 database, per-rank page with
   `Requires Level`, mana, cast, range and the tooltip text plus a rank list (`Rank N` links, which
   is how each rank's spell id was discovered).
2. `https://nether.wowhead.com/classic/tooltip/spell/<id>` — Classic Era (1.15.x) tooltip JSON.

`Conf`:

- `M` — the rank was read on **both** hosts. Levels, mana cost, cast time and range agree on all
  90 rows in this file; the two hosts disagree only on *magnitudes* (1.12 vs 1.15 rescale), which
  are recorded per ability in `## Contradictions`.
- `U` — single-sourced. No row needed this: **all 90 rows are `M`.**

Values used in the `Effects` column are the **Wowhead Classic (1.15.x)** magnitudes, because that is
the client this repo's `Content.fs` otherwise follows (see the Holy Light comment there: "310-357 on
1.12, 322-368 on the 1.15.9 client this content otherwise follows"). The 1.12 (Emberveil) magnitudes
are in `## Contradictions` for every ability where the two differ.

Conventions:

- `Cast` = cast time in seconds; `0` means instant (still on the 1.5 s global cooldown).
- `CD` = cooldown in seconds; `0` for everything except Counterspell.
- `Range` = **tiles**, 40 yd = 8 tiles (5 yd = 1, 10 yd = 2, 20 yd = 4, 30 yd = 6, 35 yd = 7).
  Melee is 1. Self-centred AoE is `0` with the radius in `Effects`.
- `Level` = trained-at level = the tooltip's "Requires level" (identical on both hosts for every
  rank here; no rank is trainer-gated by anything else, none is a talent or a book drop).
- `Effects`: `direct A-B School` = one hit; `DoT T School over D s (N ticks)` = **total** T;
  `HoA t every 3 s for D s (T total)` = heal over time; `channel x/s for D s` = per-second damage.
  Tick count = duration / tick interval (2 s for Fireball's burn, 3 s for everything else).

---

## Lesser Heal — Priest (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 30 | 1.5 | 0 | 8 | heal 47-58 | M |
| 2 | 4 | 45 | 2.0 | 0 | 8 | heal 76-91 | M |
| 3 | 10 | 75 | 2.5 | 0 | 8 | heal 143-165 | M |

Rank 3 is the last rank in the game (ids 2050 / 2052 / 2053).

## Heal — Priest (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 16 | 155 | 3.0 | 0 | 8 | heal 307-353 | M |
| 2 | 22 | 205 | 3.0 | 0 | 8 | heal 445-507 | M |
| 3 | 28 | 255 | 3.0 | 0 | 8 | heal 586-662 | M |
| 4 | 34 | 305 | 3.0 | 0 | 8 | heal 734-827 | M |

4 ranks total (2054 / 2055 / 6063 / 6064); the ladder ends at 34.

## Renew — Priest (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 8 | 30 | 0 | 0 | 8 | HoA 9 every 3 s for 15 s (45 total) | M |
| 2 | 14 | 65 | 0 | 0 | 8 | HoA 20 every 3 s for 15 s (100 total) | M |
| 3 | 20 | 105 | 0 | 0 | 8 | HoA 35 every 3 s for 15 s (175 total) | M |
| 4 | 26 | 140 | 0 | 0 | 8 | HoA 49 every 3 s for 15 s (245 total) | M |
| 5 | 32 | 170 | 0 | 0 | 8 | HoA 63 every 3 s for 15 s (315 total) | M |
| 6 | 38 | 205 | 0 | 0 | 8 | HoA 80 every 3 s for 15 s (400 total) | M |

15 s / 5 ticks at every rank. Ranks above the cap: r7 @ 44, r8 @ 50, r9 @ 56, r10 @ 60 (25315).

## Smite — Priest (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 20 | 1.5 | 0 | 6 | direct 15-20 Holy | M |
| 2 | 6 | 30 | 2.0 | 0 | 6 | direct 28-34 Holy | M |
| 3 | 14 | 60 | 2.5 | 0 | 6 | direct 58-67 Holy | M |
| 4 | 22 | 95 | 2.5 | 0 | 6 | direct 97-112 Holy | M |
| 5 | 30 | 140 | 2.5 | 0 | 6 | direct 158-178 Holy | M |
| 6 | 38 | 185 | 2.5 | 0 | 6 | direct 222-250 Holy | M |

Cast time grows 1.5 → 2.0 → 2.5 s and stays at 2.5 s from rank 3 on. Above the cap: r7 @ 46, r8 @ 54.

## Shadow Word: Pain — Priest (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 4 | 25 | 0 | 0 | 6 | DoT 30 Shadow over 18 s (6 ticks) | M |
| 2 | 10 | 50 | 0 | 0 | 6 | DoT 66 Shadow over 18 s (6 ticks) | M |
| 3 | 18 | 95 | 0 | 0 | 6 | DoT 132 Shadow over 18 s (6 ticks) | M |
| 4 | 26 | 155 | 0 | 0 | 6 | DoT 234 Shadow over 18 s (6 ticks) | M |
| 5 | 34 | 230 | 0 | 0 | 6 | DoT 366 Shadow over 18 s (6 ticks) | M |

18 s / 6 ticks (5 / 11 / 22 / 39 / 61 per tick) at every rank. Above the cap: r6 @ 42, r7 @ 50, r8 @ 58.

## Healing Touch — Druid (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 25 | 1.5 | 0 | 8 | heal 40-55 | M |
| 2 | 8 | 55 | 2.0 | 0 | 8 | heal 94-119 | M |
| 3 | 14 | 110 | 2.5 | 0 | 8 | heal 204-253 | M |
| 4 | 20 | 185 | 3.0 | 0 | 8 | heal 376-459 | M |
| 5 | 26 | 270 | 3.5 | 0 | 8 | heal 589-712 | M |
| 6 | 32 | 335 | 3.5 | 0 | 8 | heal 762-914 | M |
| 7 | 38 | 405 | 3.5 | 0 | 8 | heal 958-1143 | M |

Cast time climbs 1.5 → 3.5 s over ranks 1–5 and settles at 3.5 s. Above the cap: r8 @ 44,
r9 @ 50, r10 @ 56, r11 @ 60 (25297).

## Regrowth — Druid (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 12 | 120 | 2.0 | 0 | 8 | heal 93-107, HoA 14 every 3 s for 21 s | M |
| 2 | 18 | 205 | 2.0 | 0 | 8 | heal 176-201, HoA 25 every 3 s for 21 s | M |
| 3 | 24 | 280 | 2.0 | 0 | 8 | heal 255-290, HoA 37 every 3 s for 21 s | M |
| 4 | 30 | 350 | 2.0 | 0 | 8 | heal 336-378, HoA 49 every 3 s for 21 s | M |
| 5 | 36 | 420 | 2.0 | 0 | 8 | heal 425-478, HoA 61 every 3 s for 21 s | M |

Rank 1 is trained at 12, so a druid below 12 has only Healing Touch. HoT is 21 s / 7 ticks at every
rank (98 / 175 / 259 / 343 / 427 total). Above the cap: r6 @ 42, r7 @ 48, r8 @ 54, r9 @ 60.

## Rejuvenation — Druid (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 4 | 25 | 0 | 0 | 8 | HoA 8 every 3 s for 12 s (32 total) | M |
| 2 | 10 | 40 | 0 | 0 | 8 | HoA 14 every 3 s for 12 s (56 total) | M |
| 3 | 16 | 75 | 0 | 0 | 8 | HoA 29 every 3 s for 12 s (116 total) | M |
| 4 | 22 | 105 | 0 | 0 | 8 | HoA 45 every 3 s for 12 s (180 total) | M |
| 5 | 28 | 135 | 0 | 0 | 8 | HoA 61 every 3 s for 12 s (244 total) | M |
| 6 | 34 | 160 | 0 | 0 | 8 | HoA 76 every 3 s for 12 s (304 total) | M |
| 7 | 40 | 195 | 0 | 0 | 8 | HoA 97 every 3 s for 12 s (388 total) | M |

12 s / 4 ticks at every rank — not the 15 s of Renew. Above the cap: r8 @ 46, r9 @ 52, r10 @ 58,
r11 @ 60 (25299).

## Entangling Roots — Druid (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 8 | 50 | 1.5 | 0 | 6 | root 12 s, DoT 20 Nature over 12 s (4 ticks) | M |
| 2 | 18 | 65 | 1.5 | 0 | 6 | root 15 s, DoT 50 Nature over 15 s (5 ticks) | M |
| 3 | 28 | 80 | 1.5 | 0 | 6 | root 18 s, DoT 90 Nature over 18 s (6 ticks) | M |
| 4 | 38 | 95 | 1.5 | 0 | 6 | root 21 s, DoT 140 Nature over 21 s (7 ticks) | M |

Root duration and DoT duration are the same number at every rank (12 / 15 / 18 / 21 s); any damage
may break the root. **"Only useable outdoors"** is on both hosts' text for every rank — inside a
dungeon this spell cannot be cast at all. Above the cap: r5 @ 48, r6 @ 58.

## Wrath — Druid (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 20 | 1.5 | 0 | 6 | direct 13-16 Nature | M |
| 2 | 6 | 35 | 1.7 | 0 | 6 | direct 28-33 Nature | M |
| 3 | 14 | 55 | 2.0 | 0 | 6 | direct 48-57 Nature | M |
| 4 | 22 | 70 | 2.0 | 0 | 6 | direct 69-79 Nature | M |
| 5 | 30 | 100 | 2.0 | 0 | 6 | direct 108-123 Nature | M |
| 6 | 38 | 125 | 2.0 | 0 | 6 | direct 148-167 Nature | M |

Rank 2's 1.7 s cast is the odd one out; 2.0 s from rank 3 on. Above the cap: r7 @ 46, r8 @ 54.

## Moonfire — Druid (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 4 | 25 | 0 | 0 | 6 | direct 9-12 Arcane, DoT 12 Arcane over 9 s (3 ticks) | M |
| 2 | 10 | 50 | 0 | 0 | 6 | direct 17-21 Arcane, DoT 32 Arcane over 12 s (4 ticks) | M |
| 3 | 16 | 75 | 0 | 0 | 6 | direct 30-37 Arcane, DoT 52 Arcane over 12 s (4 ticks) | M |
| 4 | 22 | 105 | 0 | 0 | 6 | direct 47-55 Arcane, DoT 80 Arcane over 12 s (4 ticks) | M |
| 5 | 28 | 150 | 0 | 0 | 6 | direct 70-82 Arcane, DoT 124 Arcane over 12 s (4 ticks) | M |
| 6 | 34 | 190 | 0 | 0 | 6 | direct 91-108 Arcane, DoT 164 Arcane over 12 s (4 ticks) | M |
| 7 | 40 | 235 | 0 | 0 | 6 | direct 117-137 Arcane, DoT 212 Arcane over 12 s (4 ticks) | M |

Rank 1's burn is **9 s / 3 ticks**; ranks 2+ are 12 s / 4 ticks. Both parts are Arcane (not Fire, and
not Nature as some tables claim). Above the cap: r8 @ 46, r9 @ 52, r10 @ 58.

## Fireball — Mage (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 30 | 1.5 | 0 | 7 | direct 16-25 Fire, DoT 2 Fire over 4 s (2 ticks) | M |
| 2 | 6 | 45 | 2.0 | 0 | 7 | direct 34-49 Fire, DoT 3 Fire over 6 s (3 ticks) | M |
| 3 | 12 | 65 | 2.5 | 0 | 7 | direct 57-77 Fire, DoT 6 Fire over 6 s (3 ticks) | M |
| 4 | 18 | 95 | 3.0 | 0 | 7 | direct 89-122 Fire, DoT 12 Fire over 8 s (4 ticks) | M |
| 5 | 24 | 140 | 3.5 | 0 | 7 | direct 146-195 Fire, DoT 20 Fire over 8 s (4 ticks) | M |
| 6 | 30 | 185 | 3.5 | 0 | 7 | direct 207-274 Fire, DoT 28 Fire over 8 s (4 ticks) | M |
| 7 | 36 | 220 | 3.5 | 0 | 7 | direct 264-345 Fire, DoT 32 Fire over 8 s (4 ticks) | M |

35 yd = 7 tiles (the only 35 yd range in this file). Rank 5 is the first at the full 3.5 s cast.
Above the cap: r8 @ 42, r9 @ 48, r10 @ 54, r11 @ 60, and a second level-60 rank r12 (25306).

## Frostbolt — Mage (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 4 | 25 | 1.5 | 0 | 6 | direct 18-20 Frost, snare 40% 5 s | M |
| 2 | 8 | 35 | 1.8 | 0 | 6 | direct 31-35 Frost, snare 40% 6 s | M |
| 3 | 14 | 50 | 2.2 | 0 | 6 | direct 51-57 Frost, snare 40% 6 s | M |
| 4 | 20 | 65 | 2.6 | 0 | 6 | direct 74-82 Frost, snare 40% 7 s | M |
| 5 | 26 | 100 | 3.0 | 0 | 6 | direct 126-138 Frost, snare 40% 7 s | M |
| 6 | 32 | 130 | 3.0 | 0 | 6 | direct 174-190 Frost, snare 40% 8 s | M |
| 7 | 38 | 160 | 3.0 | 0 | 6 | direct 227-247 Frost, snare 40% 8 s | M |

Snare is a 40 % movement debuff, no periodic damage. Cast time 1.5 → 3.0 s over ranks 1–5.
Above the cap: r8 @ 44, r9 @ 50, r10 @ 56, r11 @ 60 (25304).

## Arcane Missiles — Mage (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 8 | 85 | 3.0 | 0 | 6 | channel 26/s for 3 s (78 total) | M |
| 2 | 16 | 140 | 4.0 | 0 | 6 | channel 38/s for 4 s (152 total) | M |
| 3 | 24 | 235 | 5.0 | 0 | 6 | channel 58/s for 5 s (290 total) | M |
| 4 | 32 | 320 | 5.0 | 0 | 6 | channel 86/s for 5 s (430 total) | M |
| 5 | 40 | 410 | 5.0 | 0 | 6 | channel 118/s for 5 s (590 total) | M |

`Cast` is the **channel length** — one missile per second, first missile lands on cast start; any
interrupt or pushback truncates the remaining missiles. Above the cap: r6 @ 48, r7 @ 56, r8 @ 56
(25345, a second level-56 rank).

## Arcane Explosion — Mage (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 14 | 75 | 0 | 0 | 0 | direct 34-38 Arcane, 10 yd (2 tiles) around caster | M |
| 2 | 22 | 120 | 0 | 0 | 0 | direct 60-66 Arcane, 10 yd (2 tiles) around caster | M |
| 3 | 30 | 185 | 0 | 0 | 0 | direct 101-110 Arcane, 10 yd (2 tiles) around caster | M |
| 4 | 38 | 250 | 0 | 0 | 0 | direct 143-156 Arcane, 10 yd (2 tiles) around caster | M |

Self-centred AoE, so `Range` 0 and the 2-tile radius lives in `Effects`; hits every enemy in the
radius for the same damage. Above the cap: r5 @ 46, r6 @ 54.

## Blizzard — Mage (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 20 | 320 | 8.0 | 0 | 6 | channel 26/s for 8 s (208 total), 8 yd (2 tiles) radius | M |
| 2 | 28 | 520 | 8.0 | 0 | 6 | channel 45/s for 8 s (360 total), 8 yd (2 tiles) radius | M |
| 3 | 36 | 720 | 8.0 | 0 | 6 | channel 66/s for 8 s (528 total), 8 yd (2 tiles) radius | M |

Both hosts print **30 yd** (6 tiles) for the target point; the damage lands in an 8 yd (2 tile)
radius around it, 8 ticks at 1 s. Above the cap: r4 @ 44, r5 @ 52, r6 @ 60.

## Polymorph — Mage (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 8 | 60 | 1.5 | 0 | 6 | polymorph 20 s | M |
| 2 | 20 | 90 | 1.5 | 0 | 6 | polymorph 30 s | M |
| 3 | 40 | 120 | 1.5 | 0 | 6 | polymorph 40 s | M |

No damage; target wanders, cannot act, regenerates fast, breaks on any damage. Beasts, humanoids and
critters only; one polymorphed target per mage. Above the cap: r4 @ 60 (12826).

## Counterspell — Mage (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 24 | 100 | 0 | 30 | 6 | interrupt + lockout 10 s | M |

The only single-rank ability in this file (id 2139) and the only ability with a real cooldown.
Both hosts print **10 s** of school lockout, not the 6 s in the brief's example line. High threat.

---

## Contradictions

**1.12 (Emberveil) vs 1.15.x (Wowhead Classic) magnitudes.** Levels, mana, cast time and range are
identical on both hosts for all 90 rows; only effect numbers move. The pattern is a flat rescale of
direct damage and heals (+3…+10 %) with **DoT/HoT totals untouched**. The table below lists both
ladders in rank order so a row can be re-checked without re-fetching.

| Ability | Emberveil 1.12 (ranks ≤41) | Wowhead 1.15.x (used in tables) |
|---|---|---|
| Lesser Heal | 46-56 / 71-85 / 135-157 | 47-58 / 76-91 / 143-165 |
| Heal | 295-341 / 429-491 / 566-642 / 712-804 | 307-353 / 445-507 / 586-662 / 734-827 |
| Renew | 45 / 100 / 175 / 245 / 315 / 400 | identical (all ranks) |
| Smite | 13-17 / 25-31 / 54-62 / 91-105 / 150-170 / 212-240 | 15-20 / 28-34 / 58-67 / 97-112 / 158-178 / 222-250 |
| Shadow Word: Pain | 30 / 66 / 132 / 234 / 366 | identical (all ranks) |
| Healing Touch | 37-51 / 88-112 / 195-243 / 363-445 / 572-694 / 742-894 / 936-1120 | 40-55 / 94-119 / 204-253 / 376-459 / 589-712 / 762-914 / 958-1143 |
| Regrowth (direct) | 84-98 / 164-188 / 240-274 / 318-360 / 405-457 | 93-107 / 176-201 / 255-290 / 336-378 / 425-478 |
| Regrowth (HoT) | 98 / 175 / 259 / 343 / 427 over 21 s | identical |
| Rejuvenation | 32 / 56 / 116 / 180 / 244 / 304 / 388 over 12 s | identical (all ranks) |
| Entangling Roots | 20 / 50 / 90 / 140 Nature | identical (all ranks) |
| Wrath | 12-14 / 25-29 / 44-52 / 63-73 / 101-115 / 139-157 | 13-16 / 28-33 / 48-57 / 69-79 / 108-123 / 148-167 |
| Moonfire (direct) | 7-9 / 13-17 / 25-31 / 40-48 / 61-73 / 81-97 / 105-125 | 9-12 / 17-21 / 30-37 / 47-55 / 70-82 / 91-108 / 117-137 |
| Moonfire (DoT) | 12 (9 s) / 32 / 52 / 80 / 124 / 164 / 212 | identical |
| Fireball (direct) | 14-22 / 31-45 / 53-73 / 84-116 / 139-187 / 199-265 / 255-335 | 16-25 / 34-49 / 57-77 / 89-122 / 146-195 / 207-274 / 264-345 |
| Fireball (DoT) | 2 (4 s) / 3 / 6 / 12 / 20 / 28 / 32 | identical |
| Frostbolt | 18-20 / 31-35 / 51-57 / 74-82 / 126-138 / 174-190 / 227-247 | identical (all ranks) |
| Arcane Missiles | 24/s / 36/s / 56/s / 83/s / 115/s | 26/s / 38/s / 58/s / 86/s / 118/s |
| Arcane Explosion | 32-36 / 57-63 / 97-105 / 139-151 | 34-38 / 60-66 / 101-110 / 143-156 |
| Blizzard (total over 8 s) | 200 (25/s) / 352 (44/s) / 520 (65/s) | 208 (26/s) / 360 (45/s) / 528 (66/s) |
| Polymorph | 20 / 30 / 40 s | identical |
| Counterspell | interrupt, 10 s lockout, 30 s CD | identical |

**`mage-ladder.md` is wrong on Frostbolt ranks 3–7.** It prints 54-60 / 78-86 / 132-144 / 180-196 /
235-255 (wowcalc.net); both Emberveil and Wowhead Classic independently print 51-57 / 74-82 / 126-138 /
174-190 / 227-247 — about 5 % lower. The table above uses the two-host value. Its Fireball, Arcane
Explosion and Arcane Missiles columns agree with Wowhead Classic to the rounding, so only Frostbolt
needs replacing.

**Off-by-one/rounding in `priest-druid-ladder.md`.** Heal r4 is 734-**827** (not 826), Healing Touch
r4 376-**459** (not 458), r5 589-**712** (not 711), r7 958-**1143** (not 1142); those are Wowhead's
exclusive upper bounds, Emberveil's ladder is lower again. Also that file's Regrowth r1 direct value
(92-106) is on yet another scale: both hosts now say 93-107 (1.15) / 84-98 (1.12).

**Blizzard range: 30 yd, not 36 yd.** Both hosts print 30 yd (6 tiles). Some third-party tables
carry 36 yd for Blizzard; the `Range` column above (6) is the verified value.

**Counterspell lockout: 10 s, not 6 s.** The brief's example row says `interrupt + lockout 6 s`; both
hosts say "preventing any spell from that school of magic from being cast for 10 sec".

**No level disputes.** Every trained-at level in this file agreed between the two hosts, so
`warcraft.wiki.gg` was not needed to break a tie; levels that the older ladder docs gave (Renew 14,
Heal 16, Smite 14, HT 14, Regrowth 12, Entangling Roots 18, Fireball 12, Frostbolt 14, …) all match.

## Gaps

- **No unsourced rank.** All 90 ranks whose trained-at level is ≤ 41 were read on both hosts; there
  is no `U` row and no `U` cell in this file.
- **Out-of-scope ranks excluded by the level ≤ 41 rule** (listed so a later reader knows the ladder
  continues and does not mistake these for missing data): Fireball 42/48/54/60/60; Frostbolt
  44/50/56/60; Arcane Missiles 48/56/56; Arcane Explosion 46/54; Blizzard 44/52/60; Polymorph 60;
  Renew 44/50/56/60; Smite 46/54; Shadow Word: Pain 42/50/58; Healing Touch 44/50/56/60; Regrowth
  42/48/54/60; Rejuvenation 46/52/58/60; Entangling Roots 48/58; Wrath 46/54; Moonfire 46/52/58;
  Lesser Heal, Heal and Counterspell have no further ranks at all.
- **Not verified, and deliberately not in any table:** spell-power coefficients, threat values,
  resistance/partial-resist behaviour, and downranking penalties. The tables are rank data only.
- **Rank-list tails are odd on both hosts and were not resolved** because they are all above the cap:
  Emberveil shows two level-60 Fireball ranks (10151 and 25306), two level-56 Arcane Missiles ranks
  (10212 and 25345), two level-56 Healing Touch ranks (9889 and 25297) and a level-60 Frostbolt
  (25304); which of each pair is trainer-taught and which is a book/quest reward is unsourced.
- **Regrowth rank 1 trained-at level** is the one value worth a second opinion if a future dispute
  appears: both hosts say 12, and no rank-1-Regrowth-below-12 exists, so a level-8–11 druid has no
  Regrowth at all in the sim.
