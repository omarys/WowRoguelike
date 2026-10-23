# Shaman, Hunter and Warlock rank ladders (levels 1–41, vanilla 1.12 / Classic Era 1.15.9)

Scope: exactly the 12 abilities the sim needs — Shaman (mana) Lightning Bolt, Earth Shock, Flame
Shock, Healing Wave; Hunter (mana) Arcane Shot, Serpent Sting, Raptor Strike; Warlock (mana) Shadow
Bolt, Corruption, Curse of Agony, Immolate, Fear. Every rank whose **trained-at level** is <= 41,
oldest first. The sim picks the newest rank at or below the character's level, so the `Level` column
is the load-bearing one.

Conventions:
- `Cast`, `CD` in seconds (0 = instant / no cooldown). `Range` in **tiles, 40 yd = 8 tiles** (5 yd = 1,
  10 yd = 2, 20 yd = 4, 30 yd = 6). Melee is 1. Shocks are 20 yd = 4. Hunter shots are 8–35 yd = 7.
- `Cost` is mana. Fear is the one exception: it costs **15 % of base mana on every rank** and no host
  prints a flat figure, so its cell reads `15%`.
- `Conf` = `M` when the ability was read on two independent hosts (`database.emberveil.org` and
  `nether.wowhead.com/classic/tooltip/spell/<id>`, with `wowcalc.net`'s 1.15.9 client tables as the
  tie-breaker), `U` when only one host prints the row. See Contradictions for the two rows where the
  hosts read differently.
- **Edition.** The tables list the **1.15.9 Classic Era** values, which is the edition
  `src/WowRoguelike.Core/Content.fs` already follows for the shaman records (Lightning Bolt 88-100,
  Earth Shock 125-133, Healing Wave 389-453). Emberveil serves the 1.12 dump, and on these spells its
  lower numbers are **not** an edition difference: emberveil prints the value at the *trained-at
  level* and names the cap ("Scales to level 25"), while the tooltip hosts print the value at the
  *scaling cap*. Full per-rank 1.12 bases are in Contradictions.

## Lightning Bolt — Shaman (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 15 | 1.5 | 0 | 6 | direct 15-17 Nature | M |
| 2 | 8 | 30 | 2.0 | 0 | 6 | direct 28-32 Nature | M |
| 3 | 14 | 45 | 2.5 | 0 | 6 | direct 48-56 Nature | M |
| 4 | 20 | 75 | 3.0 | 0 | 6 | direct 88-100 Nature | M |
| 5 | 26 | 105 | 3.0 | 0 | 6 | direct 131-149 Nature | M |
| 6 | 32 | 135 | 3.0 | 0 | 6 | direct 179-201 Nature | M |
| 7 | 38 | 165 | 3.0 | 0 | 6 | direct 235-263 Nature | M |

Spell ids: 403, 529, 548, 915, 943, 6041, 10391 (rank 8 = 10392 at 44, rank 9 = 15207 at 50,
rank 10 = 15208 at 56, all out of range). Range 30 yd on both hosts. Cast time climbs 1.5 → 3.0 s
across the ladder; only rank 1 is faster than the GCD.

## Earth Shock — Shaman (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 4 | 30 | 0 | 6 | 4 | direct 19-21 Nature, interrupt + 2 s school lockout, high threat | M |
| 2 | 8 | 50 | 0 | 6 | 4 | direct 35-37 Nature, interrupt + 2 s school lockout, high threat | M |
| 3 | 14 | 85 | 0 | 6 | 4 | direct 65-69 Nature, interrupt + 2 s school lockout, high threat | M |
| 4 | 24 | 145 | 0 | 6 | 4 | direct 125-133 Nature, interrupt + 2 s school lockout, high threat | M |
| 5 | 36 | 240 | 0 | 6 | 4 | direct 235-249 Nature, interrupt + 2 s school lockout, high threat | M |

Spell ids: 8042, 8044, 8045, 8046, 10412 (rank 6 = 10413 at 48, rank 7 = 10414 at 60). Range 20 yd.
The lockout is **2 s** on every rank (both hosts). Rank 4 is the level-24 anchor: 125-133 for 145
mana — exactly what `Content.earthShock` carries.

## Flame Shock — Shaman (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 10 | 55 | 0 | 6 | 4 | direct 25 Fire + DoT 28 Fire over 12 s (4 ticks of 7) | M |
| 2 | 18 | 95 | 0 | 6 | 4 | direct 51 Fire + DoT 48 Fire over 12 s (4 ticks of 12) | M |
| 3 | 28 | 160 | 0 | 6 | 4 | direct 94 Fire + DoT 96 Fire over 12 s (4 ticks of 24) | M |
| 4 | 40 | 250 | 0 | 6 | 4 | direct 163 Fire + DoT 168 Fire over 12 s (4 ticks of 42) | M |

Spell ids: 8050, 8052, 8053, 10447 (rank 5 = 10448 at 52, rank 6 = 29228 at 60). Range 20 yd.
The DoT is 4 ticks at a 3 s interval and never changes duration; only the direct hit scales with
rank. Both hosts agree on the DoT for every rank.

## Healing Wave — Shaman (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 25 | 1.5 | 0 | 8 | heal 36-46 | M |
| 2 | 6 | 45 | 2.0 | 0 | 8 | heal 69-83 | M |
| 3 | 12 | 80 | 2.5 | 0 | 8 | heal 136-162 | M |
| 4 | 18 | 155 | 3.0 | 0 | 8 | heal 279-327 | M |
| 5 | 24 | 200 | 3.0 | 0 | 8 | heal 389-453 | M |
| 6 | 32 | 265 | 3.0 | 0 | 8 | heal 552-638 | M |
| 7 | 40 | 340 | 3.0 | 0 | 8 | heal 759-873 | M |

Spell ids: 331, 332, 547, 913, 939, 959, 8005 (rank 8 = 10395 at 48, rank 9 = 10396 at 56,
rank 10 = 25357 at 60). Range 40 yd on both hosts — the longest range in this file.

## Arcane Shot — Hunter (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 6 | 25 | 0 | 6 | 7 | direct 13 Arcane (no weapon damage) | M |
| 2 | 12 | 35 | 0 | 6 | 7 | direct 21 Arcane (no weapon damage) | M |
| 3 | 20 | 50 | 0 | 6 | 7 | direct 33 Arcane (no weapon damage) | M |
| 4 | 28 | 80 | 0 | 6 | 7 | direct 59 Arcane (no weapon damage) | M |
| 5 | 36 | 105 | 0 | 6 | 7 | direct 83 Arcane (no weapon damage) | M |

Spell ids: 3044, 14281, 14282, 14283, 14284 (rank 6 = 14285 at 44, rank 7 = 14286 at 52,
rank 8 = 14287 at 60). Range 8–35 yd (7 tiles; there is a dead zone inside 8 yd). Every rank is
**instant**, on the 1.5 s GCD, with a 6 s cooldown — it is *not* on the Auto Shot swing timer
(Wowhead's flag list for 3044 and every other rank: `Do Not Reset Combat Timers`). It requires a
ranged weapon but adds **no weapon damage**: the only effect row is `School Damage(Arcane)`, so the
sim's `SpellDamage(33, 33)` shape is right and a `WeaponDamage` component would be wrong.

## Serpent Sting — Hunter (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 4 | 15 | 0 | 0 | 7 | DoT 20 Nature over 15 s (5 ticks of 4) | M |
| 2 | 10 | 30 | 0 | 0 | 7 | DoT 40 Nature over 15 s (5 ticks of 8) | M |
| 3 | 18 | 50 | 0 | 0 | 7 | DoT 80 Nature over 15 s (5 ticks of 16) | M |
| 4 | 26 | 80 | 0 | 0 | 7 | DoT 140 Nature over 15 s (5 ticks of 28) | M |
| 5 | 34 | 115 | 0 | 0 | 7 | DoT 210 Nature over 15 s (5 ticks of 42) | M |

Spell ids: 1978, 13549, 13550, 13551, 13552 (rank 6 = 13553 at **42** — one level past the sim's cap,
rank 7 = 13554 at 50, rank 8 = 13555 at 58, rank 9 = 25295 at 60). Range 8–35 yd (7 tiles).
Every rank is **instant**, no cooldown, 1.5 s GCD, and again `Do Not Reset Combat Timers` — it does
not consume or delay an Auto Shot. **No weapon damage**: spell 1978's only effect is
`Apply Aura: Periodic Damage`, 5 ticks at a 3 s interval over a fixed 15 s. `Content.serpentSting`
uses `DamageOverTime(secTicks 15.0, 5)`, i.e. 75 total, which is 5 short of rank 3's 80.

## Raptor Strike — Hunter (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 15 | 0 | 6 | 1 | WeaponDamage (100% weapon) + 5 flat, next melee swing | M |
| 2 | 8 | 25 | 0 | 6 | 1 | WeaponDamage (100% weapon) + 11 flat, next melee swing | M |
| 3 | 16 | 35 | 0 | 6 | 1 | WeaponDamage (100% weapon) + 21 flat, next melee swing | M |
| 4 | 24 | 45 | 0 | 6 | 1 | WeaponDamage (100% weapon) + 34 flat, next melee swing | M |
| 5 | 32 | 55 | 0 | 6 | 1 | WeaponDamage (100% weapon) + 50 flat, next melee swing | M |
| 6 | 40 | 70 | 0 | 6 | 1 | WeaponDamage (100% weapon) + 80 flat, next melee swing | M |

Spell ids: 2973, 14260, 14261, 14262, 14263, 14264 (rank 7 = 14265 at 48, rank 8 = 14266 at 56).
Range 5 yd = 1 tile, `GCD 0`. Every rank is instant but **on the next melee swing** (client flag
`On next swing (players)`), and the only effect is `Deal Weapon Damage` with the flat bonus above —
this is the one ability in this file that really is `WeaponDamage`-shaped.
`Content.raptorStrike`'s `WeaponDamage(10000, 34)` matches rank 4 exactly.

## Shadow Bolt — Warlock (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 25 | 1.7 | 0 | 6 | direct 13-17 Shadow | M |
| 2 | 6 | 40 | 2.2 | 0 | 6 | direct 26-32 Shadow | M |
| 3 | 12 | 70 | 2.8 | 0 | 6 | direct 52-60 Shadow | M |
| 4 | 20 | 110 | 3.0 | 0 | 6 | direct 92-104 Shadow | M |
| 5 | 28 | 160 | 3.0 | 0 | 6 | direct 150-170 Shadow | M |
| 6 | 36 | 210 | 3.0 | 0 | 6 | direct 213-239 Shadow | M |

Spell ids: 686, 695, 705, 1088, 1106, 7641 (rank 7 = 11659 at 44, rank 8 = 11660 at 52,
rank 9 = 11661 at 60, rank 10 = 25307 at 60). Range 30 yd. Cast time 1.7 / 2.2 / 2.8 / 3.0 s — the
odd non-integer 1.7 s at rank 1 is on both hosts.

## Corruption — Warlock (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 4 | 35 | 2.0 | 0 | 6 | DoT 40 Shadow over 12 s (4 ticks of 10) | M |
| 2 | 14 | 55 | 2.0 | 0 | 6 | DoT 90 Shadow over 15 s (5 ticks of 18) | M |
| 3 | 24 | 100 | 2.0 | 0 | 6 | DoT 222 Shadow over 18 s (6 ticks of 37) | M |
| 4 | 34 | 160 | 2.0 | 0 | 6 | DoT 324 Shadow over 18 s (6 ticks of 54) | M |

Spell ids: 172, 6222, 6223, 7648 (rank 5 = 11671 at 44, rank 6 = 11672 at 54, rank 7 = 27216 at 60).
Range 30 yd. The duration grows 12 → 15 → 18 s, and it stays 18 s from rank 3 on. Note that
Corruption is a **2.0 s cast** here on both hosts: Improved Corruption (instant) is a talent, and a
level-24 warlock cannot reach 5/5 in it.

## Curse of Agony — Warlock (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 8 | 25 | 0 | 0 | 6 | DoT 84 Shadow over 24 s (12 ticks, escalating) | M |
| 2 | 18 | 50 | 0 | 0 | 6 | DoT 180 Shadow over 24 s (12 ticks, escalating) | M |
| 3 | 28 | 90 | 0 | 0 | 6 | DoT 324 Shadow over 24 s (12 ticks, escalating) | M |
| 4 | 38 | 130 | 0 | 0 | 6 | DoT 504 Shadow over 24 s (12 ticks, escalating) | M |

Spell ids: 980, 1014, 6217, 11711 (rank 5 = 11712 at 48, rank 6 = 11713 at 58). Range 30 yd,
instant, no cooldown. The damage is deliberately back-loaded: 12 ticks at a 2 s interval whose
per-tick value rises through the 24 s, so the tooltip total is not the tick average × 12 for any
partial window. The sim flattens it; the flat equivalent at this ladder is 84/24 = 3.5, 180/24 = 7.5,
324/24 = 13.5 and 504/24 = 21 a second.

## Immolate — Warlock (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 1 | 25 | 2.0 | 0 | 6 | direct 10 Fire + DoT 20 Fire over 15 s (5 ticks of 4) | M |
| 2 | 10 | 45 | 2.0 | 0 | 6 | direct 23 Fire + DoT 40 Fire over 15 s (5 ticks of 8) | M |
| 3 | 20 | 90 | 2.0 | 0 | 6 | direct 52 Fire + DoT 90 Fire over 15 s (5 ticks of 18) | M |
| 4 | 30 | 155 | 2.0 | 0 | 6 | direct 100 Fire + DoT 165 Fire over 15 s (5 ticks of 33) | M |
| 5 | 40 | 220 | 2.0 | 0 | 6 | direct 147 Fire + DoT 255 Fire over 15 s (5 ticks of 51) | M |

Spell ids: 348, 707, 1094, 2941, 11665 (rank 6 = 11667 at 50, rank 7 = 11668 at 60, rank 8 = 25309 at
60). Range 30 yd, 2.0 s cast, no cooldown, 15 s DoT duration on every rank. This is the ability where
the two hosts read a different direct hit by 1 (see Contradictions).

## Fear — Warlock (Mana)
| Rank | Level | Cost | Cast | CD | Range | Effects | Conf |
|---|---|---:|---:|---:|---:|---|---|
| 1 | 8 | 15% | 1.5 | 0 | 4 | fear 10 s (breaks on damage, one target at a time) | M |
| 2 | 32 | 15% | 1.5 | 0 | 4 | fear 15 s (breaks on damage, one target at a time) | M |

Spell ids: 5782, 6213 (rank 3 = 6215 at 56). Range 20 yd, 1.5 s cast, no cooldown. Only the duration
changes with rank; cost, cast, range and the "damage may interrupt / only 1 target" rules are
identical on both ranks and both hosts. A level-41 warlock still has rank 2 — rank 3 is at 56.

## Contradictions

1. **Immolate's direct hit differs by 1 between the two 1.15 hosts.** Wowhead's tooltip prints
   11 / 24 / 53 / 101 / 148 for ranks 1-5; wowcalc's 1.15.9.70003 client table prints
   10 / 23 / 52 / 100 / 147, which is what the tables above carry. Emberveil's 1.12 base for the same
   ranks is 8 / 19 / 45 / 90 / 134 with "Scales to level" caps of 5 / 15 / 25 / 35 / 45 — i.e. a
   trained-at-level value that lands on the wowcalc figure at the cap. All three hosts agree on the
   20 / 40 / 90 / 165 / 255 DoT. Both hosts were read for every rank, so the rows stay `M`; take
   10/23/52/100/147 as the value and do not mix in the Wowhead +1.
2. **A handful of upper bounds print +1 on Wowhead.** Lightning Bolt 2/3/6/7 read 28-33, 48-57,
   179-202, 235-264; Earth Shock 1/2 read 19-22, 35-38 and rank 4 reads **126-134**; Healing Wave
   rank 1 reads 36-47; Shadow Bolt 1/6 read 13-18, 213-240. Wowcalc's client tables give the values
   in the tables above (28-32, 48-56, 179-201, 235-263, 19-21, 35-37, **125-133**, 36-46, 13-17,
   213-239). The Earth Shock rank-4 case is the one that matters: `.pi/research/level24/shaman.md`
   and `Content.earthShock` both use **125-133**, so 125-133 is the consistent choice.
3. **Emberveil is not a lower edition — it is a lower point on the same curve.** Every one of these
   spells carries per-level scaling on the 1.12 dump (Lightning Bolt rank 4 "Scales to level 25" from
   a base of 83-95, Shadow Bolt rank 4 base 86-98 "Scales to level 25", Immolate rank 3 base 45,
   Earth Shock rank 4 base 119-127), and the tooltip hosts print the value at that cap. So the
   ~5-7 % gap the previous reports read as a "Classic-Era tuning pass" is, for these twelve abilities,
   level scaling: 83-95 → 88-100 is +1 a level for five levels, not a rebalance. The practical
   consequence is that a freshly-trained rank sits **below** the printed tooltip value until the
   character reaches the scaling cap (rank 4 Shadow Bolt at level 20 hits for 86-98, not 92-104).
   This file prints the cap values, because that is what the character has for all but the first few
   levels of a rank's life, and because `Content.fs`'s shaman records already use them.
4. **The repo's warlock numbers are trained-at-level base values, not cap values.**
   `.pi/research/level24/warlock.md` §6 and `Content.shadowBolt` carry 86-99 for Shadow Bolt rank 4
   and `Content.warlockImmolate` carries 45 + 90 for Immolate rank 3, against 92-104 and 52 + 90 here.
   Those are the rank-4 and rank-3 bases (emberveil: 86-98, 45), one point off the printed tooltip on
   the top bound. Whatever the ladder is wired to, the shaman and warlock records must end up in the
   same convention or the warlock reads ~6 % weaker than the shaman at the same level. Current split:
   shaman = cap (1.15.9), warlock = base (1.12).
5. **The brief's weapon-damage instruction does not hold for Arcane Shot or Serpent Sting.**
   Wowhead's spell-detail effect list for 3044 (Arcane Shot rank 1) is `School Damage(Arcane) Value:
   13` and nothing else; for 1978 (Serpent Sting rank 1) it is `Apply Aura: Periodic Damage Value: 4
   every 3 seconds` and nothing else. Neither carries `Deal Weapon Damage`. Only Raptor Strike (2973)
   does — `Deal Weapon Damage Value: 5`, flagged `On next swing (players)`. Giving Arcane Shot a
   weapon-damage component would overstate a hunter's burst by a whole Auto Shot per cast.
6. **Hunter ranged range is 8–35 yd, not 41.** Both hosts give Arcane Shot and Serpent Sting
   `8 - 35 yd range` (the 8 yd minimum is the dead zone), so 35 yd = 7 tiles and 41 yd never appears
   in this file. The only 41-yd figure in hunter tooltips is Multi-Shot's, which is not in scope.
7. **Rank-level collisions above the cap, for completeness (not tabulated).** Serpent Sting rank 6 is
   at level **42** — the nearest miss in the file — and Shadow Bolt ranks 9/10, Immolate ranks 7/8 and
   Corruption rank 7 each have two ranks sharing a level (both at 60). No ability here has a duplicate
   trained-at level inside 1–41.

## Gaps

- **Nothing in the 1–41 window is unsourced.** Every rank in every table was read on both
  `database.emberveil.org/spell/<id>` and `nether.wowhead.com/classic/tooltip/spell/<id>`, and every
  one is inside wowcalc's per-class 1.15.9 rank table as a third read. There are no `U` rows.
- **warcraft.wiki.gg was unavailable** (Cloudflare 403 on every page, including `api.php`; the
  `r.jina.ai` proxy returns the retail article, which has no Classic rank table). Its intended job —
  arbitrating "level learned" disputes — was therefore not needed, because no dispute appeared: all
  three hosts agree on every trained-at level in this file.
- **Ranks above 41 were not verified beyond their level.** The "out of range" ids and levels in each
  table's spell-id line come from the emberveil rank chains; their costs, damage and cast times were
  not fetched (except the few that the wowcalc tables print in full).
- **Flat mana for Fear is unknown by design.** 15 % of base mana is tooltip text on both hosts; base
  mana depends on level and Intellect, so no numeric cost is stated. `Content.fear` charges a flat 70,
  which is a pacing choice, not data.
- **Curse of Agony's escalation curve is not tabulated.** Both hosts give only the 24 s total. The
  per-tick ramp (which ticks carry how much) would need the aura rows, which neither reachable page
  exposes; the sim's flat average is the only defensible model from these sources.
- **Spell-power coefficients were not re-verified.** Wowhead's effect rows print them per rank
  (e.g. Arcane Shot rank 1 SP mod 0.204), but they are not tabulated here and should not be trusted
  from this file.
- **No threat figures for the shocks.** "Causes a high amount of threat" is tooltip text on both
  hosts; the numeric multiplier is not printed anywhere reachable.

## Sim alignment (source vs `src/WowRoguelike.Core/Content.fs`)

| Record | Line | Carries | This ladder says |
|---|---|---|---|
| `arcaneShot` | `src/WowRoguelike.Core/Content.fs:281` | rank 3, 33 Arcane, 50 mana, 6 s CD | matches rank 3 exactly; `Range = 8` should be **7** |
| `serpentSting` | `src/WowRoguelike.Core/Content.fs:445` | `DamageOverTime(15.0, 5)` = 75 | rank 3 is **80** (16 a tick); `Range = 8` should be **7** |
| `raptorStrike` | `src/WowRoguelike.Core/Content.fs:457` | `WeaponDamage(10000, 34)`, 45 mana, 6 s CD | matches rank 4 exactly, `Range = 1` correct |
| `shadowBolt` | `src/WowRoguelike.Core/Content.fs:374` | 86-99 | rank 4 cap value is **92-104** (base 86-98); `Range = 8` should be **6** |
| `corruption` | `src/WowRoguelike.Core/Content.fs:387` | `DamageOverTime(18.0, 12)` = 216 | rank 3 is **222** (37 a tick); `Range = 8` should be **6** |
| `curseOfAgony` | `src/WowRoguelike.Core/Content.fs:402` | `DamageOverTime(24.0, 7)` = 168 | rank 2 is **180** (7.5 a second); `Range = 8` should be **6** |
| `warlockImmolate` | `src/WowRoguelike.Core/Content.fs:414` | 45 + `DamageOverTime(15.0, 6)` = 90 | rank 3 cap value is **52** + 90 (base 45); `Range = 8` should be **6** |
| `fear` | `src/WowRoguelike.Core/Content.fs:429` | `Sleep(10.0)`, rank 1 | matches rank 1; `Range = 5` should be **4** |
| `shamanLightningBolt` | `src/WowRoguelike.Core/Content.fs:474` | 88-100 | matches rank 4; `Range = 8` should be **6** |
| `earthShock` | `src/WowRoguelike.Core/Content.fs:489` | 125-133 + `Interrupt` | matches rank 4; `Range = 5` should be **4** |
| `flameShock` | `src/WowRoguelike.Core/Content.fs:501` | 51 + `DamageOverTime(12.0, 4)` = 48 | matches rank 2; `Range = 5` should be **4** |
| `shamanHealingWave` | `src/WowRoguelike.Core/Content.fs:514` | `Heal(389, 453)` | matches rank 5; `Range = 8` correct (40 yd) |

The `Range` column is the one systematic mismatch. The comment at
`src/WowRoguelike.Core/Content.fs:297` claims the "40-yd-is-8-tiles convention", but the records are
actually yards ÷ 4 clamped at 8 (20 yd → 5, 30 yd → 8, 35 yd → 8). Under the brief's 5-yds-a-tile
rule every ranged record in the file renumbers (20 → 4, 30 → 6, 35 → 7) and only the 40 yd heals and
the melee records stay put. Either renumber all twelve records at once or restate the convention —
do not mix the two.

## Sources

- `https://database.emberveil.org/spell/<id>` — per-rank `Rank N of M`, `Requires Level N`, mana,
  cast, cooldown, range, damage, and the `Scales to level N` cap that explains the 1.12 bases. Rank
  chains read off this host's rank links gave every id in this file.
- `https://database.emberveil.org/spells?name=<Ability>` — the only working search (`?search=` is
  ignored by the server; the filter fields are `name` and `learnable_at`). Used to find the rank-1 id
  of each ability.
- `https://nether.wowhead.com/classic/tooltip/spell/<id>` — second host; JSON tooltip with
  `Requires level N`, cost, range, cast, cooldown and damage.
- `https://wowcalc.net/en/classic/wiki/spells/<class>` — 1.15.9.70003 client rank tables (per-rank
  Level / Cost / Cast / Effect), tie-breaker and third read.
- `https://www.wowhead.com/classic/spell=<id>` via the `https://r.jina.ai/` read-only proxy — spell
  detail pages, used only for the effect lists and flags (weapon damage, GCD, `On next swing`,
  `Do Not Reset Combat Timers`). Plain fetches of `www.wowhead.com` return 403; the proxy works.
- `src/WowRoguelike.Core/Content.fs` — current records, for the alignment table.
- `.pi/research/level24/shaman.md`, `hunter.md`, `warlock.md` — the level-24 anchor rows; all anchor
  values reproduce here except the two warlock rows in Contradictions §4.
