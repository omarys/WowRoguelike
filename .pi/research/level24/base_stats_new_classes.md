# Vanilla 1.12 base stats, levels 1-24 — Paladin, Shaman, Hunter, Warlock

Pure data extraction, ready to paste into `src/WowRoguelike.Core/Content.fs`. Every number below is a
verbatim row from the primary source. Nothing is interpolated, averaged or "fixed"; wherever a second source
disagrees the primary value is kept as-is and the disagreement is listed under **Discrepancies**.

**Primary source:** `wow_world_base` v0.3.0, `extended/vanilla/base_stats.rs`
— <https://docs.rs/wow_world_base/latest/src/wow_world_base/extended/vanilla/base_stats.rs.html>
(the same file `Content.fs` already cites for the warrior / priest / rogue / mage / druid arrays).

Row semantics: `BaseStats::new(strength, agility, stamina, intellect, spirit, health, mana)`; `health` and
`mana` are the per-class base constants only, before Stamina / Intellect are added inside `derive`.
Each `const <RACE>_<CLASS>: &[BaseStats]` holds exactly 60 entries (`MAX_LEVEL = 60`), index = level - 1, so
**level 24 = index 23**. `health` / `mana` are identical for every race of a class (checked: all 4 classes
here, every level 1-24), which is the "base, not base + attribute bonus" check.

Parser sanity check: the same extraction script, run against the crate file, reproduced all 40 entries of the
already-accepted `humanWarriorBase`, `humanPriestBase` and `nightElfRogueBase` arrays in `Content.fs`
byte-for-byte, and index 23 = level 24 for those is additionally corroborated by
`.pi/research/level24/base_stats.md`.

All 14 required constants exist under the expected names — nothing is missing. Constants found per class:

- Paladin: `HUMAN_PALADIN` (L73), `DWARF_PALADIN` (L755) — 2, i.e. every vanilla paladin race.
- Shaman: `ORC_SHAMAN` (L569), `TROLL_SHAMAN` (L2367), `TAUREN_SHAMAN` (L1747) — 3, all.
- Hunter: `NIGHT_ELF_HUNTER` (L1065), `DWARF_HUNTER` (L817), `TROLL_HUNTER` (L2181), `ORC_HUNTER` (L445), `TAUREN_HUNTER` (L1685) — 5, all.
- Warlock: `GNOME_WARLOCK` (L2057), `HUMAN_WARLOCK` (L321), `ORC_WARLOCK` (L631), `UNDEAD_WARLOCK` (L1561) — 4, all.
- (The file has 40 constants in total, 10 classes.)

## Level-1 / level-24 summary (both rows quoted)

| Array | L1 tuple | L24 tuple |
|---|---|---|
| `humanPaladinBase` | `(22, 20, 22, 20, 22, 28, 59)` | `(47, 34, 46, 35, 38, 247, 525)` |
| `dwarfPaladinBase` | `(24, 16, 25, 19, 20, 28, 59)` | `(49, 30, 49, 34, 36, 247, 525)` |
| `orcShamanBase` | `(24, 17, 23, 18, 25, 27, 53)` | `(43, 28, 45, 39, 49, 234, 476)` |
| `trollShamanBase` | `(22, 22, 22, 17, 23, 27, 53)` | `(41, 33, 44, 38, 47, 234, 476)` |
| `taurenShamanBase` | `(26, 15, 23, 16, 24, 27, 53)` | `(45, 26, 45, 37, 48, 234, 476)` |
| `nightElfHunterBase` | `(17, 28, 20, 20, 21, 26, 63)` | `(28, 59, 41, 34, 36, 273, 568)` |
| `dwarfHunterBase` | `(22, 19, 24, 19, 20, 26, 63)` | `(33, 50, 45, 33, 35, 273, 568)` |
| `trollHunterBase` | `(21, 25, 22, 16, 22, 26, 63)` | `(32, 56, 43, 30, 37, 273, 568)` |
| `orcHunterBase` | `(23, 20, 23, 17, 24, 26, 63)` | `(34, 51, 44, 31, 39, 273, 568)` |
| `taurenHunterBase` | `(25, 18, 23, 15, 23, 26, 63)` | `(36, 49, 44, 29, 38, 273, 568)` |
| `gnomeWarlockBase` | `(15, 23, 20, 25, 22, 23, 59)` | `(23, 32, 33, 55, 50, 266, 467)` |
| `humanWarlockBase` | `(20, 20, 21, 22, 22, 23, 59)` | `(28, 29, 34, 49, 52, 266, 467)` |
| `orcWarlockBase` | `(23, 17, 23, 19, 25, 23, 59)` | `(31, 26, 35, 46, 53, 266, 467)` |
| `undeadWarlockBase` | `(19, 18, 22, 20, 27, 23, 59)` | `(27, 27, 35, 47, 55, 266, 467)` |

## Arrays

### Paladin

`HUMAN_PALADIN` — `base_stats.rs` L73

```fsharp
  /// Human Paladin: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private humanPaladinBase =
    [| (22, 20, 22, 20, 22, 28, 59)
       (23, 21, 23, 21, 23, 26, 64)
       (24, 21, 24, 21, 23, 34, 84)
       (25, 22, 25, 22, 24, 42, 90)
       (26, 22, 26, 22, 25, 50, 112)
       (27, 23, 27, 23, 25, 58, 120)
       (28, 23, 28, 24, 26, 66, 129)
       (29, 24, 28, 24, 26, 84, 154)
       (30, 24, 29, 25, 27, 92, 165)
       (31, 25, 30, 25, 28, 100, 192)
       (32, 25, 31, 26, 29, 108, 205)
       (33, 26, 32, 27, 29, 116, 219)
       (34, 27, 33, 27, 30, 124, 249)
       (35, 27, 34, 28, 31, 132, 265)
       (36, 28, 36, 29, 31, 131, 282)
       (38, 28, 37, 29, 32, 141, 315)
       (39, 29, 38, 30, 33, 152, 334)
       (40, 30, 39, 31, 34, 164, 354)
       (41, 30, 40, 31, 34, 177, 390)
       (42, 31, 41, 32, 35, 191, 412)
       (43, 32, 42, 33, 36, 206, 435)
       (45, 32, 43, 34, 37, 222, 459)
       (46, 33, 44, 34, 38, 239, 499)
       (47, 34, 46, 35, 38, 247, 525) |]
```

`DWARF_PALADIN` — `base_stats.rs` L755

```fsharp
  /// Dwarf Paladin: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private dwarfPaladinBase =
    [| (24, 16, 25, 19, 20, 28, 59)
       (25, 17, 26, 20, 21, 26, 64)
       (26, 17, 27, 20, 21, 34, 84)
       (27, 18, 28, 21, 22, 42, 90)
       (28, 18, 29, 21, 23, 50, 112)
       (29, 19, 30, 22, 23, 58, 120)
       (30, 19, 31, 23, 24, 66, 129)
       (31, 20, 31, 23, 24, 84, 154)
       (32, 20, 32, 24, 25, 92, 165)
       (33, 21, 33, 24, 26, 100, 192)
       (34, 21, 34, 25, 27, 108, 205)
       (35, 22, 35, 26, 27, 116, 219)
       (36, 23, 36, 26, 28, 124, 249)
       (37, 23, 37, 27, 29, 132, 265)
       (38, 24, 39, 28, 29, 131, 282)
       (40, 24, 40, 28, 30, 141, 315)
       (41, 25, 41, 29, 31, 152, 334)
       (42, 26, 42, 30, 32, 164, 354)
       (43, 26, 43, 30, 32, 177, 390)
       (44, 27, 44, 31, 33, 191, 412)
       (45, 28, 45, 32, 34, 206, 435)
       (47, 28, 46, 33, 35, 222, 459)
       (48, 29, 47, 33, 36, 239, 499)
       (49, 30, 49, 34, 36, 247, 525) |]
```

### Shaman

`ORC_SHAMAN` — `base_stats.rs` L569

```fsharp
  /// Orc Shaman: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private orcShamanBase =
    [| (24, 17, 23, 18, 25, 27, 53)
       (25, 17, 24, 19, 26, 34, 61)
       (25, 18, 25, 20, 27, 41, 68)
       (26, 18, 26, 20, 28, 48, 91)
       (27, 19, 26, 21, 29, 65, 100)
       (28, 19, 27, 22, 30, 72, 110)
       (29, 19, 28, 23, 31, 79, 121)
       (29, 20, 29, 24, 31, 86, 133)
       (30, 20, 30, 25, 32, 93, 161)
       (31, 21, 31, 25, 33, 100, 175)
       (32, 21, 32, 26, 34, 107, 190)
       (33, 22, 33, 27, 35, 114, 206)
       (33, 22, 34, 28, 36, 121, 223)
       (34, 23, 35, 29, 37, 128, 241)
       (35, 23, 36, 30, 39, 135, 260)
       (36, 24, 37, 31, 40, 142, 280)
       (37, 24, 38, 32, 41, 150, 301)
       (38, 25, 39, 33, 42, 159, 323)
       (39, 25, 40, 34, 43, 169, 346)
       (40, 26, 41, 35, 44, 180, 370)
       (41, 26, 42, 36, 45, 192, 395)
       (41, 27, 43, 37, 46, 205, 456)
       (42, 27, 44, 38, 47, 219, 448)
       (43, 28, 45, 39, 49, 234, 476) |]
```

`TROLL_SHAMAN` — `base_stats.rs` L2367

```fsharp
  /// Troll Shaman: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private trollShamanBase =
    [| (22, 22, 22, 17, 23, 27, 53)
       (23, 22, 23, 18, 24, 34, 61)
       (23, 23, 24, 19, 25, 41, 68)
       (24, 23, 25, 19, 26, 48, 91)
       (25, 24, 25, 20, 27, 65, 100)
       (26, 24, 26, 21, 28, 72, 110)
       (27, 24, 27, 22, 29, 79, 121)
       (27, 25, 28, 23, 29, 86, 133)
       (28, 25, 29, 24, 30, 93, 161)
       (29, 26, 30, 24, 31, 100, 175)
       (30, 26, 31, 25, 32, 107, 190)
       (31, 27, 32, 26, 33, 114, 206)
       (31, 27, 33, 27, 34, 121, 223)
       (32, 28, 34, 28, 35, 128, 241)
       (33, 28, 35, 29, 37, 135, 260)
       (34, 29, 36, 30, 38, 142, 280)
       (35, 29, 37, 31, 39, 150, 301)
       (36, 30, 38, 32, 40, 159, 323)
       (37, 30, 39, 33, 41, 169, 346)
       (38, 31, 40, 34, 42, 180, 370)
       (39, 31, 41, 35, 43, 192, 395)
       (39, 32, 42, 36, 44, 205, 456)
       (40, 32, 43, 37, 45, 219, 448)
       (41, 33, 44, 38, 47, 234, 476) |]
```

`TAUREN_SHAMAN` — `base_stats.rs` L1747

```fsharp
  /// Tauren Shaman: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private taurenShamanBase =
    [| (26, 15, 23, 16, 24, 27, 53)
       (27, 15, 24, 17, 25, 34, 61)
       (27, 16, 25, 18, 26, 41, 68)
       (28, 16, 26, 18, 27, 48, 91)
       (29, 17, 26, 19, 28, 65, 100)
       (30, 17, 27, 20, 29, 72, 110)
       (31, 17, 28, 21, 30, 79, 121)
       (31, 18, 29, 22, 30, 86, 133)
       (32, 18, 30, 23, 31, 93, 161)
       (33, 19, 31, 23, 32, 100, 175)
       (34, 19, 32, 24, 33, 107, 190)
       (35, 20, 33, 25, 34, 114, 206)
       (35, 20, 34, 26, 35, 121, 223)
       (36, 21, 35, 27, 36, 128, 241)
       (37, 21, 36, 28, 38, 135, 260)
       (38, 22, 37, 29, 39, 142, 280)
       (39, 22, 38, 30, 40, 150, 301)
       (40, 23, 39, 31, 41, 159, 323)
       (41, 23, 40, 32, 42, 169, 346)
       (42, 24, 41, 33, 43, 180, 370)
       (43, 24, 42, 34, 44, 192, 395)
       (43, 25, 43, 35, 45, 205, 456)
       (44, 25, 44, 36, 46, 219, 448)
       (45, 26, 45, 37, 48, 234, 476) |]
```

### Hunter

`NIGHT_ELF_HUNTER` — `base_stats.rs` L1065

```fsharp
  /// Night Elf Hunter: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private nightElfHunterBase =
    [| (17, 28, 20, 20, 21, 26, 63)
       (17, 29, 21, 21, 22, 33, 70)
       (18, 30, 22, 21, 22, 40, 91)
       (18, 32, 22, 22, 23, 57, 98)
       (19, 33, 23, 22, 23, 64, 121)
       (19, 34, 24, 23, 24, 71, 130)
       (19, 35, 25, 23, 24, 78, 155)
       (20, 36, 26, 24, 25, 85, 166)
       (20, 38, 27, 24, 26, 92, 193)
       (21, 39, 27, 25, 26, 109, 206)
       (21, 40, 28, 25, 27, 116, 235)
       (22, 42, 29, 26, 28, 123, 250)
       (22, 43, 30, 27, 28, 130, 266)
       (23, 44, 31, 27, 29, 138, 298)
       (23, 46, 32, 28, 29, 147, 316)
       (24, 47, 33, 28, 30, 157, 350)
       (24, 48, 34, 29, 31, 168, 370)
       (25, 50, 35, 30, 32, 180, 391)
       (25, 51, 36, 30, 32, 193, 428)
       (26, 53, 37, 31, 33, 207, 451)
       (26, 54, 38, 32, 34, 222, 475)
       (27, 56, 39, 32, 34, 238, 515)
       (27, 57, 40, 33, 35, 255, 541)
       (28, 59, 41, 34, 36, 273, 568) |]
```

`DWARF_HUNTER` — `base_stats.rs` L817

```fsharp
  /// Dwarf Hunter: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private dwarfHunterBase =
    [| (22, 19, 24, 19, 20, 26, 63)
       (22, 20, 25, 20, 21, 33, 70)
       (23, 21, 26, 20, 21, 40, 91)
       (23, 23, 26, 21, 22, 57, 98)
       (24, 24, 27, 21, 22, 64, 121)
       (24, 25, 28, 22, 23, 71, 130)
       (24, 26, 29, 22, 23, 78, 155)
       (25, 27, 30, 23, 24, 85, 166)
       (25, 29, 31, 23, 25, 92, 193)
       (26, 30, 31, 24, 25, 109, 206)
       (26, 31, 32, 24, 26, 116, 235)
       (27, 33, 33, 25, 27, 123, 250)
       (27, 34, 34, 26, 27, 130, 266)
       (28, 35, 35, 26, 28, 138, 298)
       (28, 37, 36, 27, 28, 147, 316)
       (29, 38, 37, 27, 29, 157, 350)
       (29, 39, 38, 28, 30, 168, 370)
       (30, 41, 39, 29, 31, 180, 391)
       (30, 42, 40, 29, 31, 193, 428)
       (31, 44, 41, 30, 32, 207, 451)
       (31, 45, 42, 31, 33, 222, 475)
       (32, 47, 43, 31, 33, 238, 515)
       (32, 48, 44, 32, 34, 255, 541)
       (33, 50, 45, 33, 35, 273, 568) |]
```

`TROLL_HUNTER` — `base_stats.rs` L2181

```fsharp
  /// Troll Hunter: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private trollHunterBase =
    [| (21, 25, 22, 16, 22, 26, 63)
       (21, 26, 23, 17, 23, 33, 70)
       (22, 27, 24, 17, 23, 40, 91)
       (22, 29, 24, 18, 24, 57, 98)
       (23, 30, 25, 18, 24, 64, 121)
       (23, 31, 26, 19, 25, 71, 130)
       (23, 32, 27, 19, 25, 78, 155)
       (24, 33, 28, 20, 26, 85, 166)
       (24, 35, 29, 20, 27, 92, 193)
       (25, 36, 29, 21, 27, 109, 206)
       (25, 37, 30, 21, 28, 116, 235)
       (26, 39, 31, 22, 29, 123, 250)
       (26, 40, 32, 23, 29, 130, 266)
       (27, 41, 33, 23, 30, 138, 298)
       (27, 43, 34, 24, 30, 147, 316)
       (28, 44, 35, 24, 31, 157, 350)
       (28, 45, 36, 25, 32, 168, 370)
       (29, 47, 37, 26, 33, 180, 391)
       (29, 48, 38, 26, 33, 193, 428)
       (30, 50, 39, 27, 34, 207, 451)
       (30, 51, 40, 28, 35, 222, 475)
       (31, 53, 41, 28, 35, 238, 515)
       (31, 54, 42, 29, 36, 255, 541)
       (32, 56, 43, 30, 37, 273, 568) |]
```

`ORC_HUNTER` — `base_stats.rs` L445

```fsharp
  /// Orc Hunter: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private orcHunterBase =
    [| (23, 20, 23, 17, 24, 26, 63)
       (23, 21, 24, 18, 25, 33, 70)
       (24, 22, 25, 18, 25, 40, 91)
       (24, 24, 25, 19, 26, 57, 98)
       (25, 25, 26, 19, 26, 64, 121)
       (25, 26, 27, 20, 27, 71, 130)
       (25, 27, 28, 20, 27, 78, 155)
       (26, 28, 29, 21, 28, 85, 166)
       (26, 30, 30, 21, 29, 92, 193)
       (27, 31, 30, 22, 29, 109, 206)
       (27, 32, 31, 22, 30, 116, 235)
       (28, 34, 32, 23, 31, 123, 250)
       (28, 35, 33, 24, 31, 130, 266)
       (29, 36, 34, 24, 32, 138, 298)
       (29, 38, 35, 25, 32, 147, 316)
       (30, 39, 36, 25, 33, 157, 350)
       (30, 40, 37, 26, 34, 168, 370)
       (31, 42, 38, 27, 35, 180, 391)
       (31, 43, 39, 27, 35, 193, 428)
       (32, 45, 40, 28, 36, 207, 451)
       (32, 46, 41, 29, 37, 222, 475)
       (33, 48, 42, 29, 37, 238, 515)
       (33, 49, 43, 30, 38, 255, 541)
       (34, 51, 44, 31, 39, 273, 568) |]
```

`TAUREN_HUNTER` — `base_stats.rs` L1685

```fsharp
  /// Tauren Hunter: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private taurenHunterBase =
    [| (25, 18, 23, 15, 23, 26, 63)
       (25, 19, 24, 16, 24, 33, 70)
       (26, 20, 25, 16, 24, 40, 91)
       (26, 22, 25, 17, 25, 57, 98)
       (27, 23, 26, 17, 25, 64, 121)
       (27, 24, 27, 18, 26, 71, 130)
       (27, 25, 28, 18, 26, 78, 155)
       (28, 26, 29, 19, 27, 85, 166)
       (28, 28, 30, 19, 28, 92, 193)
       (29, 29, 30, 20, 28, 109, 206)
       (29, 30, 31, 20, 29, 116, 235)
       (30, 32, 32, 21, 30, 123, 250)
       (30, 33, 33, 22, 30, 130, 266)
       (31, 34, 34, 22, 31, 138, 298)
       (31, 36, 35, 23, 31, 147, 316)
       (32, 37, 36, 23, 32, 157, 350)
       (32, 38, 37, 24, 33, 168, 370)
       (33, 40, 38, 25, 34, 180, 391)
       (33, 41, 39, 25, 34, 193, 428)
       (34, 43, 40, 26, 35, 207, 451)
       (34, 44, 41, 27, 36, 222, 475)
       (35, 46, 42, 27, 36, 238, 515)
       (35, 47, 43, 28, 37, 255, 541)
       (36, 49, 44, 29, 38, 273, 568) |]
```

### Warlock

`GNOME_WARLOCK` — `base_stats.rs` L2057

```fsharp
  /// Gnome Warlock: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private gnomeWarlockBase =
    [| (15, 23, 20, 25, 22, 23, 59)
       (15, 23, 21, 27, 23, 28, 98)
       (16, 24, 21, 28, 24, 43, 107)
       (16, 24, 22, 29, 25, 48, 117)
       (16, 24, 22, 30, 26, 63, 128)
       (16, 25, 23, 32, 27, 68, 140)
       (17, 25, 23, 33, 29, 83, 153)
       (17, 25, 24, 34, 30, 88, 167)
       (17, 26, 24, 35, 31, 103, 182)
       (18, 26, 25, 36, 32, 108, 198)
       (18, 27, 25, 38, 33, 123, 200)
       (18, 27, 26, 39, 34, 128, 218)
       (19, 27, 26, 40, 36, 143, 237)
       (19, 28, 27, 41, 37, 148, 257)
       (19, 28, 28, 42, 38, 153, 278)
       (20, 29, 28, 43, 39, 168, 300)
       (20, 29, 29, 45, 41, 173, 308)
       (20, 29, 29, 46, 42, 189, 332)
       (21, 30, 30, 47, 43, 196, 357)
       (21, 30, 31, 48, 45, 204, 383)
       (21, 31, 31, 50, 46, 223, 395)
       (22, 31, 32, 51, 47, 233, 423)
       (22, 32, 33, 53, 49, 244, 452)
       (23, 32, 33, 55, 50, 266, 467) |]
```

`HUMAN_WARLOCK` — `base_stats.rs` L321

```fsharp
  /// Human Warlock: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private humanWarlockBase =
    [| (20, 20, 21, 22, 22, 23, 59)
       (20, 20, 22, 23, 24, 28, 98)
       (21, 21, 22, 24, 25, 43, 107)
       (21, 21, 23, 25, 26, 48, 117)
       (21, 21, 23, 26, 27, 63, 128)
       (21, 22, 24, 27, 28, 68, 140)
       (22, 22, 24, 28, 30, 83, 153)
       (22, 22, 25, 29, 31, 88, 167)
       (22, 23, 25, 30, 32, 103, 182)
       (23, 23, 26, 31, 33, 108, 198)
       (23, 24, 26, 33, 34, 123, 200)
       (23, 24, 27, 34, 35, 128, 218)
       (24, 24, 27, 35, 37, 143, 237)
       (24, 25, 28, 36, 38, 148, 257)
       (24, 25, 29, 37, 39, 153, 278)
       (25, 26, 29, 38, 40, 168, 300)
       (25, 26, 30, 40, 43, 173, 308)
       (25, 26, 30, 41, 44, 189, 332)
       (26, 27, 31, 42, 45, 196, 357)
       (26, 27, 32, 43, 47, 204, 383)
       (26, 28, 32, 45, 48, 223, 395)
       (27, 28, 33, 46, 49, 233, 423)
       (27, 29, 34, 47, 51, 244, 452)
       (28, 29, 34, 49, 52, 266, 467) |]
```

`ORC_WARLOCK` — `base_stats.rs` L631

```fsharp
  /// Orc Warlock: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private orcWarlockBase =
    [| (23, 17, 23, 19, 25, 23, 59)
       (23, 17, 24, 20, 26, 28, 98)
       (24, 18, 24, 21, 27, 43, 107)
       (24, 18, 25, 22, 28, 48, 117)
       (24, 18, 25, 23, 29, 63, 128)
       (24, 19, 26, 24, 30, 68, 140)
       (25, 19, 26, 25, 32, 83, 153)
       (25, 19, 27, 26, 33, 88, 167)
       (25, 20, 27, 27, 34, 103, 182)
       (26, 20, 28, 28, 35, 108, 198)
       (26, 21, 28, 30, 36, 123, 200)
       (26, 21, 29, 31, 37, 128, 218)
       (27, 21, 29, 32, 39, 143, 237)
       (27, 22, 30, 33, 40, 148, 257)
       (27, 22, 31, 34, 41, 153, 278)
       (28, 23, 31, 35, 42, 168, 300)
       (28, 23, 32, 37, 44, 173, 308)
       (28, 23, 32, 38, 45, 189, 332)
       (29, 24, 33, 39, 46, 196, 357)
       (29, 24, 34, 40, 48, 204, 383)
       (29, 25, 34, 42, 49, 223, 395)
       (30, 25, 34, 43, 50, 233, 423)
       (30, 26, 35, 44, 52, 244, 452)
       (31, 26, 35, 46, 53, 266, 467) |]
```

`UNDEAD_WARLOCK` — `base_stats.rs` L1561

```fsharp
  /// Undead Warlock: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private undeadWarlockBase =
    [| (19, 18, 22, 20, 27, 23, 59)
       (19, 18, 23, 21, 28, 28, 98)
       (20, 19, 23, 22, 29, 43, 107)
       (20, 19, 24, 23, 30, 48, 117)
       (20, 19, 24, 24, 31, 63, 128)
       (20, 20, 25, 25, 32, 68, 140)
       (21, 20, 25, 26, 34, 83, 153)
       (21, 20, 26, 27, 35, 88, 167)
       (21, 21, 26, 28, 36, 103, 182)
       (22, 21, 27, 29, 37, 108, 198)
       (22, 22, 27, 31, 38, 123, 200)
       (22, 22, 28, 32, 39, 128, 218)
       (23, 22, 28, 33, 41, 143, 237)
       (23, 23, 29, 34, 42, 148, 257)
       (23, 23, 30, 35, 43, 153, 278)
       (24, 24, 30, 36, 44, 168, 300)
       (24, 24, 31, 38, 46, 173, 308)
       (24, 24, 31, 39, 47, 189, 332)
       (25, 25, 32, 40, 48, 196, 357)
       (25, 25, 33, 41, 50, 204, 383)
       (25, 26, 33, 43, 51, 223, 395)
       (26, 26, 34, 44, 52, 233, 423)
       (26, 27, 35, 45, 54, 244, 452)
       (27, 27, 35, 47, 55, 266, 467) |]
```

## Verification — second sources

Two reachable (no-login) second sources were used; the wikis were fetched through their MediaWiki
`api.php?action=parse&prop=wikitext` endpoints because the normal HTML pages are behind Cloudflare.

1. **1.12-era starting-attribute tables** — `vanilla-wow-archive.fandom.com` (wowwiki snapshot), one
   "Starting attributes" table per race page: `Human_(playable)`, `Dwarf_(playable)`,
   `Night_elf_(playable)`, `Gnome_(playable)`, `Troll_(playable)`, `Undead_(playable)`, plus `Orc_(playable)`
   and `Tauren_(playable)` (see discrepancy D5 about those two). These give the level-1
   `(str, agi, sta, int, spi)` per race/class.
2. **wowpedia `Base mana`** — <https://wowpedia.fandom.com/wiki/Base_mana>, section "Table of all values"
   (per-level base mana by class). This is the only per-level (levels 2-24) second source I could reach.

### Level-1 cross-check per class

All four classes hit the second source directly; 8 of the 9 combos covered by a self-consistent wiki table
match the crate exactly.

| Combo | Wiki L1 `(str agi sta int spi)` | Crate L1 | Result |
|---|---|---|---|
| Human Paladin | 22 / 20 / 22 / 20 / 22 | 22, 20, 22, 20, 22 | **match** |
| Dwarf Paladin | 24 / 16 / 25 / 19 / 20 | 24, 16, 25, 19, 20 | **match** |
| Troll Shaman | 22 / 22 / 22 / 17 / 23 | 22, 22, 22, 17, 23 | **match** |
| Orc Shaman | table unusable (D5) | 24, 17, 23, 18, 25 | no direct quote |
| Tauren Shaman | table unusable (D5) | 26, 15, 23, 16, 24 | no direct quote |
| Night Elf Hunter | 17 / 28 / 20 / 20 / 21 | 17, 28, 20, 20, 21 | **match** |
| Dwarf Hunter | 22 / 19 / 24 / 19 / 20 | 22, 19, 24, 19, 20 | **match** |
| Troll Hunter | 21 / 25 / 22 / 16 / 22 | 21, 25, 22, 16, 22 | **match** |
| Orc Hunter | table unusable (D5) | 23, 20, 23, 17, 24 | no direct quote |
| Tauren Hunter | table unusable (D5) | 25, 18, 23, 15, 23 | no direct quote |
| Gnome Warlock | 15 / 23 / 20 / 25 / 22 | 15, 23, 20, 25, 22 | **match** |
| Human Warlock | 20 / 20 / 21 / 22 / **23** | 20, 20, 21, 22, **22** | spirit differs (D4) |
| Orc Warlock | table unusable (D5) | 23, 17, 23, 19, 25 | no direct quote |
| Undead Warlock | 19 / 18 / 22 / 20 / 27 | 19, 18, 22, 20, 27 | **match** |

The 5 combos with "no direct quote" are still corroborated by the delta check below, which the crate
satisfies exactly.

### Uniform class deltas (DERIVED) — covers levels beyond 1 and the 5 unquoted combos

Race base attribute rows quoted from the wiki pages: Human `20/20/20/20/20`, Dwarf `22/16/23/19/19`,
Night elf `17/25/19/20/20`, Gnome `15/23/19/23/20`, Troll `21/22/21/16/21`, Undead `19/18/21/18/25`,
Orc `23/17/22/17/23`. Subtracting those from the crate's level-1 rows gives a **race-independent class
delta that is identical for every race** (the one wobble is human Spirit, which carries the +5% Spirit
racial and so rounds one point higher):

| Class | Delta `(str agi sta int spi)` | Checked on |
|---|---|---|
| Paladin | +2 / 0 / +2 / 0 / +2 (Dwarf: +1 spirit) | Human, Dwarf |
| Shaman | +1 / 0 / +1 / +1 / +2 | Troll, Orc, Tauren |
| Hunter | 0 / +3 / +1 / 0 / +1 | Night elf, Dwarf, Troll, Orc, Tauren |
| Warlock | 0 / 0 / +1 / +2 / +2 | Gnome, Human, Undead, Orc |
| (Warrior) | +3 / 0 / +2 / 0 / 0 | all 5 warrior races — human is 0/+1 spirit |
| (Rogue) | +1 / +3 / +1 / 0 / 0 | Human, Dwarf, Night elf, Gnome, Troll, Orc, Undead |
| (Mage / Priest) | 0/0/0/+3/+2 and 0/0/0/+2/+3 | Gnome, Human, Troll (mage); Human, Dwarf, Troll, Undead (priest) |

Applying those deltas reproduces every one of the 14 arrays' level-1 rows, including the 5 combos with no
direct quote, e.g. Orc base `23/17/22/17/23` + shaman delta = `24/17/23/18/25` = crate `ORC_SHAMAN`;
Tauren base `25/15/22/15/22` (DERIVED: the crate's Tauren rows minus the class delta agree on this one base
across hunter, shaman and druid) + shaman delta = `26/15/23/16/24` = crate `TAUREN_SHAMAN`.

### Base mana / base health column (levels 2-24)

`wowpedia:Base_mana` "Table of all values", compared to the crate's `mana` column (identical for all races of
a class, so the paladin comparison covers both paladin arrays):

| Class | Levels the page lists | Agree | Disagree |
|---|---|---|---|
| Paladin | 1, 2-24 | 23 | L1: crate 59, page 60 |
| Shaman | 1, 7, 10-24 | 15 | L1: crate 53, page 55; L22: crate 456, page 421 (D3) |
| Hunter | 1, 4, 8, 14 | 3 | L1: crate 63, page 65 |
| Druid (existing `nightElfDruidBase`, as a calibration of the page) | 1, 8-24 | 17 | L1: crate 17, page 50 |

The page's Mage, Priest and Warlock columns disagree with the crate almost everywhere (mage L10 crate 256 vs
page 196; warlock L1 crate 59 vs page 90), and the page itself carries an `{{ood}}` "out of date" banner, so
it is usable as corroboration **only** on the paladin/shaman/druid/hunter rows. No reachable second source
had per-level base *health* at all (the archived `Base health` page only lists patch-3.2 level-80 values), so
the `health` column has no direct second source — it is backed only by the class-only (race-independent)
consistency check and by `Content.fs`'s existing arrays, which came from the same table.

### Level-24 anchors from the task

- Human paladin L1 `(22, 20, 22, 20, 22, 28, 59)` — **matches** the crate row and the example in the task.
- Human paladin L24 `(47, 34, 46, 35, 38, 247, 525)` — **matches** the crate's index-23 row exactly
  (`HUMAN_PALADIN` L97), and the 525 mana is independently confirmed by the wowpedia paladin row for level 24.
- Human warrior L1 and L60 anchors — **do not match**; see D1 and D2.

## Discrepancies

**D1 — Human warrior level 1 anchor is wrong (not the table).**
Task anchor: `(23, 20, 23, 20, 21, 30, 0)`. Primary source and `Content.fs` `humanWarriorBase`:
`(23, 20, 22, 20, 21, 20, 0)` — stamina 22 not 23, base health 20 not 30. Two independent second sources
side with the crate: the 1.12 wiki `Attributes` article ("The warrior has 60 health, and 22 Stamina, this
grants the warrior 40 health bonus to his already existing **20 base health**") and the Human
starting-attributes table (Warrior `23/20/22/20/21`). This will be a problem if the anchor is used as a test
assertion: the assertion is wrong, the existing array is right.

**D2 — The "human warrior level 60" anchor is actually a paladin row.**
Task anchor: `(105, 65, 100, 70, 78, 1381, 0)`. The crate's `HUMAN_WARRIOR` level 60 (index 59) is
`(120, 80, 110, 30, 47, 1689, 0)`. The anchor is *exactly* the crate's `HUMAN_PALADIN` level 60 row
`(105, 65, 100, 70, 78, 1381, 1512)` with the mana column zeroed — i.e. the anchor is mislabeled, not a data
conflict. No value was adjusted.

**D3 — Shaman base mana at level 22.**
Primary source `ORC_SHAMAN`/`TROLL_SHAMAN`/`TAUREN_SHAMAN` index 21 = **456**; wowpedia's table says **421**.
The crate value also breaks the otherwise monotonic run (L21 395 → L22 **456** → L23 448 → L24 476), while
421 would fit the pattern (395 → 421 → 448 → 476). Transcribed verbatim as 456 (never invent or interpolate);
this is the one cell in the 14 arrays I would want a third opinion on before it is trusted in balance work.

**D4 — Human warlock level 1 Spirit.**
Crate 22, 1.12 wiki 23. The crate is consistent with the uniform warlock delta (`0/0/+1/+2/+2`) seen on
Gnome `15/23/19/23/20 → 15/23/20/25/22`, Undead `19/18/21/18/25 → 19/18/22/20/27` and Orc; the wiki cell is
most likely the human +5% Spirit racial applied. Crate value 22 kept.

**D5 — The Orc and Tauren race pages carry the same table (unusable for 5 combos).**
`Orc_(playable)` and `Tauren_(playable)` have byte-identical "Starting attributes" tables, and on both the
header names the Druid/Hunter/Priest/Shaman/Warrior class set with base row `23/17/22/17/23`. One is a
copy. It cannot be reconciled with the crate: its Warrior column `26/17/24/17/23` equals crate `ORC_WARRIOR`
exactly, while its Hunter column `24/20/23/17/23` differs from crate `ORC_HUNTER` `23/20/23/17/24` and its
Shaman column `23/17/23/19/25` differs from both `ORC_SHAMAN` `24/17/23/18/25` and `TAUREN_SHAMAN`
`26/15/23/16/24`. Consequently Orc Shaman, Tauren Shaman, Orc Hunter, Tauren Hunter and Orc Warlock have
**no** direct level-1 quote; they rest on the uniform-delta check, which they satisfy exactly.

**D6 — Pre-existing source quirks left untouched (not in the new arrays' scope to fix).**
- Human paladin base health dips at two levels: L1 28 → L2 26, and L14 132 → L15 131. Quoted as-is; paladin
  base health is not monotonic in the client table.
- The already-accepted `nightElfDruidBase` in `Content.fs:988` starts `(18, 25, 19, 22, 22, 33, 17)` — base
  mana 17 at level 1, where wowpedia says 50. Same class of cell-level anomaly as D3, already in the repo.
