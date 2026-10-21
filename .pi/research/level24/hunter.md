# Level 24 Hunter (Vanilla 1.12 / Classic Era) — sourced numbers

Edition: **1.12 primary** (classicdb.ch 1.12 tooltips, mangos-zero 1.12 server formulas), cross-checked against
**1.15.9.70003** (wowcalc.net built from that client build). The two editions agreed on every hunter ability
checked; the only divergence found is an attack-power offset (§8).

Tooling note: the `web_search` / `fetch_content` / `get_search_content` tools were **not** available in this
session. Pages were retrieved over plain HTTP (`curl -o <file>`, and `r.jina.ai` as a read-only proxy for
warcraft.wiki.gg, whose api.php returns Cloudflare "Blocked"). Item rows come from the `wow-classic-items` npm
dataset (Wowhead-Classic scrape, `data/json/data.json`), filtered on `requiredLevel <= 24`.

Tags: **D** = a fetched page states it · **I** = arithmetic on cited formulas · **U** = unknown/unverified.

## 1. Level-24 base stats (naked, no gear, no buffs)

Primary source: vanilla `BaseStats` arrays, order `(str, agi, sta, int, spi, base health, base mana)`, index 23 =
level 24 — `wow_world_base` `extended/vanilla/base_stats.rs`; already extracted verbatim into
`.pi/research/level24/base_stats_new_classes.md` §Arrays — **D**.

| Race | Str | Agi | Sta | Int | Spi | Base hp const | Base mana const |
|---|---|---|---|---|---|---|---|
| Night Elf `NIGHT_ELF_HUNTER` | 28 | **59** | 41 | 34 | 36 | 273 | 568 |
| Dwarf `DWARF_HUNTER` | 33 | 50 | 45 | 33 | 35 | 273 | 568 |
| Troll `TROLL_HUNTER` | 32 | 56 | 43 | 30 | 37 | 273 | 568 |
| Orc `ORC_HUNTER` | 34 | 51 | 44 | 31 | 39 | 273 | 568 |
| Tauren `TAUREN_HUNTER` | 36 | 49 | 44 | 29 | 38 | 273 | 568 |

Derived naked pools (**I**, formulas from mangos-zero `Object/StatSystem.cpp`, Sources §10):

- Health = 273 + f(Sta), `f(s) = min(s,20) + 10*(s-20)` → NE **503**, Dwarf 543, Troll 523, Orc 533, Tauren 533.
- Mana = 568 + g(Int), `g(i) = min(i,20) + 15*(i-20)` → NE **798**, Dwarf 783, Troll 738, Orc 753, Tauren 723.
- Armour naked = `2 x Agi` (no base armour in 1.12) → NE **118**, Dwarf 100, Troll 112, Orc 102, Tauren 98.

### Agility conversions for a hunter

| Conversion | Formula | Source | Conf |
|---|---|---|---|
| Ranged attack power | `(Level x 2) + (Agility x 2 - 20)` | https://warcraft.wiki.gg/wiki/Attack_power per-class table | D |
| Ranged AP (emulator variant) | `Level*2 + AGI*2 - 10` | mangos-zero `src/game/Object/StatSystem.cpp`, `UpdateAttackPowerAndDamage(ranged=true)`, `case CLASS_HUNTER` | D |
| Melee attack power | `(Level x 2) + (Str - 10) + (Agi - 10)` | warcraft.wiki.gg/wiki/Attack_power | D |
| AP → damage | `+1 DPS per 14 AP`; per-swing bonus `= AP/14 x weapon_speed` | same page, verbatim | D |
| Armour from Agility | `2 armour per Agility`; total = item armour + `2*AGI` | mangos-zero `StatSystem.cpp` `UpdateArmor()` | D |
| Melee **and** ranged crit from Agi | `AGI / classrate`, `classrate(L) = 3.5*(60-L)/59 + 53*(L-1)/59`; at L24 `classrate = 22.797` → **22.80 AGI per +1% crit** | mangos-zero `src/game/Object/PlayerStats.cpp` `GetMeleeCritFromAgility()`, `case CLASS_HUNTER: valLevel1 = 3.5f; valLevel60 = 53.0f;` — the same agility term feeds `RANGED_CRIT_PERCENTAGE` (`UpdateCritPercentage(RANGED_ATTACK)`) | D (emulator) |
| Dodge from Agi | same interpolation with `valLevel1 = 1.8f`, `valLevel60 = 26.5f` → at L24 **11.429 AGI per +1% dodge** | `GetDodgeFromAgility()`, same file | D (emulator) |
| Endpoint corroboration | "Hunters require 53 AGI [per 1% crit]" at level 60 | vanillawowarchive Agility (dev quote), quoted in `.pi/research/level24/base_stats.md` §5 | D |

Derived naked numbers (**I**): NE ranged AP **156** (wiki form; 152 in the emulator form), melee AP 115,
+2.59% crit and +5.16% dodge from Agi. The flat 5% base crit that sits under this is **not** separately verified
this run — **U**.

## 2. Best-in-slot gear usable at level 24 (requiredLevel <= 24)

**A level-24 hunter wears LEATHER only.** "Hunters and shaman began by wearing leather armor until level 40, where
they could switch to mail" — https://warcraft.wiki.gg/wiki/Mail_armor — **D**. Every mail item in this bracket
(Sparkleshell Mantle, Yorgen Bracers, Fire Hardened Coif, Turtle Scale, ...) is therefore **unusable** and excluded.

| Slot | Item | id | Req | Armour | Stats / effect | Where | Conf |
|---|---|---|---|---|---|---|---|
| Head | — no fixed-stat head with agility at req <= 24 in the dataset — | — | — | — | U (random-suffix green "of the Monkey", ilvl ~29) | — | U |
| Neck | **Scout's Medallion** (H) / Sentinel's Medallion (A) | 20442 / 20444 | 18 | 0 | **+6 Agi, +2 Sta** | WSG vendor, Honored | D |
| Shoulder | **Serpent's Shoulders** | 5404 | 18 | 68 | **+9 Agi** | BoE drop | D |
| Back | **Fenrus' Hide** | 6340 | 20 | 22 (cloth) | **+6 Agi, +5 Sta** | Fenrus the Devourer | D |
| Chest | **Bristlebark Blouse** | 14570 | 23 | 90 | **+7 Agi, +6 Sta** | BoE craft/drop | D |
| Wrist | **Bands of Serra'kis** | 6902 | 22 | 43 | **+4 Str, +6 Agi, +2 Sta** | Old Serra'kis, Blackfathom Deeps | D |
| Hands | **Naga Battle Gloves** | 888 | 22 | 61 | **+4 Str, +7 Agi, +4 Sta** | Lady Sarevess, BFD | D |
| Waist | **Deviate Scale Belt** | 6468 | 18 | 51 | **+5 Agi, +6 Sta** | BoE craft | D |
| Legs | **Leggings of the Fang** | 10410 | 18 | 79 | **+5 Str, +9 Agi, +4 Sta** | Lord Cobrahn, Wailing Caverns | D |
| Feet | **Feet of the Lynx** | 1121 | 19 | 63 | **+3 Str, +8 Agi** | BoE drop | D |
| Ring 1 | **Ring of Precision** | 1491 | 20 | 0 | **+6 Agi, +3 Spi** | zone drop | D |
| Ring 2 | **Protector's Band** (A) / Legionnaire's Band (H) | 20439 / 20429 | 18 | 0 | **+4 Str, +4 Agi, +2 Sta** | WSG vendor | D |
| Trinket | Insignia of the Alliance/Horde | — | — | — | no stats (PvP trinket) | WSG vendor | U |
| Ranged | **Naga Heartpiercer** (bow) | 3078 | 21 | — | **21-39 dmg, speed 1.8, 16.67 DPS, +5 Agi** | Lady Sarevess, BFD | D |
| Ranged alt | Hi-tech Supergun (gun) | 9487 | 24 | — | 29-55 dmg, speed 2.3, **18.26 DPS**, no stats | zone/world drop | D |
| Ammo | **Sharp Arrow** / Heavy Shot | 2515 / 2519 | 10 | — | **+3.5 DPS** (= +6.3 per shot at 1.8 speed) | vendor | D |
| Ammo alt | Crafted Heavy Shot | 8068 | 15 | — | +4.5 DPS | Engineering | D |
| Melee stat stick (2H) | none verified at req <= 24 with agility | — | — | — | U | — | U |

Other ranged weapons at req <= 24 (DPS = mid damage / speed, **I** from the dataset rows): Double-barreled
Shotgun 2098 (req 22) 27-52 @ 2.3 = 17.17 · Ranger Bow 3021 (req 20) 30-57 @ 2.7 = 16.11, +1 Agi · Long Battle
Bow 15284 (req 24) 24-46 @ 2.2 = 15.91 · Venomstrike 6469 (req 19) 26-50 @ 2.4 = 15.83. Hi-tech Supergun wins on
raw DPS; Naga Heartpiercer trades 1.6 DPS for 5 Agi (= 10 ranged AP = 0.71 DPS + 0.22% crit) — a wash in practice, **I**.

## 3. Totals at level 24

**A) Base only, naked (NE):** Str 28 / Agi 59 / Sta 41 / Int 34 / Spi 36; health 503; mana 798; armour 118;
ranged AP 156; melee AP 115.

**B) Base + the §2 set** (Naga Heartpiercer included; head and trinket empty). Gear contributes **+20 Str,
+78 Agi, +31 Sta, +3 Spi, +477 item armour**.

| Total | NE | Dwarf | Troll | Orc | Tauren |
|---|---|---|---|---|---|
| Strength | 48 | 53 | 52 | 54 | 56 |
| Agility | **137** | 128 | 134 | 129 | 127 |
| Stamina | 72 | 76 | 74 | 75 | 75 |
| Intellect | 34 | 33 | 30 | 31 | 29 |
| Spirit | 39 | 38 | 40 | 42 | 41 |
| Item armour | 477 | 477 | 477 | 477 | 477 |
| Agility armour (2/Agi) | 274 | 256 | 268 | 258 | 254 |
| **Armour** | **751** | 733 | 745 | 735 | 731 |
| **Health** | **813** | 853 | 833 | 823 | 823 |
| **Mana** | **798** | 783 | 738 | 753 | 723 |
| Ranged AP (wiki form) | **302** | 284 | 296 | 286 | 282 |
| Ranged AP (emulator form) | 312 | 294 | 306 | 296 | 292 |
| Melee AP | 213 | 209 | 214 | 211 | 211 |
| Crit from Agi (melee & ranged) | **6.01%** | 5.61% | 5.88% | 5.66% | 5.57% |
| Dodge from Agi | 11.99% | 11.20% | 11.72% | 11.29% | 11.11% |
| Physical DR vs a level-24 attacker | **23.5%** | 23.1% | 23.4% | 23.2% | 23.1% |

All rows **I**: §1 base row + §2 gear, formulas in §1. DR uses `DR = A/(A + 400 + 85*24) = A/(A + 2440)`
(`.pi/research/level24/base_stats.md` §4, from wowwiki-archive `Damage_reduction`) — **D** for the formula.

### Auto Shot output with this set (NE, Naga Heartpiercer, Sharp Arrow) — **I**

| Component | Value |
|---|---|
| Weapon DPS (21-39 @ 1.8) | 16.67 |
| Ammo DPS (Sharp Arrow) | +3.50 |
| Attack-power DPS (302 / 14) | +21.57 |
| **Auto Shot DPS pre-crit** | **41.7** |
| Per-shot damage | 66.1 - 84.1 (avg 75.1) = `21..39 + 6.3 + 38.8` |
| With 6.01% crit, before miss/dodge | ~44.2 |

Ranged miss/dodge at 24 is not modelled here — **U** (no level-24 ranged hit table obtained).

## 4. Every ability a level-24 hunter actually has

Learn levels: trainer-sourced rows from the Classic ability table (`hunter_rows.txt`, extracted from
https://warcraft.wiki.gg/wiki/Hunter_abilities_(Classic), Level/Trainer columns) — **D**; ranks, costs and
tooltips cross-read on https://wowcalc.net/en/classic/wiki/spells/hunter (1.15.9.70003) and
https://classicdb.ch/?spell=<id> (1.12) — **D**. At level 24 a hunter has **15 talent points** (levels 10-24),
so talent-gated abilities are marked as such.

| Ability | Rank at 24 | Learned | Cost | Cast | CD | Range | Effect |
|---|---|---|---|---|---|---|---|
| **Auto Shot** | — | 1 | 0 | Instant | — | 35 yd | Auto-attack loop; fires until cancelled. **35 yd** (classicdb `?spell=75`) |
| **Arcane Shot** | **3** | **20** | **50 mana** | Instant | **6 s** | 35 yd | **33 Arcane damage** (identical in 1.12 and 1.15.9) |
| **Serpent Sting** | **3** | **18** | **50 mana** | Instant | — | 35 yd | **80 Nature damage over 15 s** (5 ticks x 16, tick every 3 s — tick maths **I**) |
| **Multi-Shot** | **1** | **18** | **100 mana** | Instant | **10 s** | 35 yd | Fires at 3 targets; r1 tooltip carries **no flat bonus** (weapon damage per target) |
| **Aimed Shot** | **1** | 20 | 75 mana | **3 s cast** | 6 s | 35 yd | "+70 damage". **Talent-gated** (Marksmanship; trainer column is "—"); point cost **U** |
| **Concussive Shot** | 1 | 8 | **8% of base mana (=45)** | Instant | **12 s** | 35 yd | **No damage**; **daze, 50% slow for 4 s** |
| **Hunter's Mark** | **2** | **22** | **30 mana** | Instant | — | **100 yd** | **+45 ranged AP for all attackers** vs target, **2 min** |
| **Scorpid Sting** | 1 | 22 | 70 mana | Instant | — | 35 yd | **-20 Str and -20 Agi for 20 s** (one Sting per hunter) |
| **Wing Clip** | 1 | 12 | **40 mana** | Instant | — | **5 yd** | **5 damage + 50% slow for 10 s** |
| **Raptor Strike** | **4** | **24** | **45 mana** | Instant (next melee) | **6 s** | 5 yd | **+34 melee damage** |
| **Mongoose Bite** | 1 | 16 | 30 mana | Instant | **5 s** | 5 yd | **25 damage**, only after you dodge |
| **Aspect of the Hawk** | **2** | **18** | 35 mana | Instant | — | self | **+35 ranged AP**, until cancelled |
| **Aspect of the Monkey** | 1 | 4 | 0 | Instant | — | self | **+8% dodge**, until cancelled |
| **Aspect of the Cheetah** | 1 | 20 | **22 mana** | Instant | — | self | **+30% movement**; dazed 4 s if you take damage |
| **Track Beasts / Humanoids / Undead / Hidden** | 1 each | 1 / 10 / 18 / **24** | 0 | Instant | — | — | minimap tracking (+stealth detection on Track Hidden) |
| **Immolation Trap** | 1 | 16 | 50 mana | Instant | 15 s | placed | **105 Fire over 15 s** to the first enemy; trap lasts 1 min; **out of combat only** |
| **Freezing Trap** | 1 | 20 | 50 mana | Instant | U | placed | Freezes first enemy **up to 10 s**, breaks on damage; trap lasts 1 min; out of combat only |
| **Distracting Shot** | 2 | 20 | 30 mana | Instant | 8 s | 35 yd | pure threat |
| **Disengage** | 1 | 20 | 50 mana | Instant | 5 s | self | threat reduction, leaves combat |
| **Scare Beast** | 1 | 14 | 35 mana | **1.5 s cast** | 30 s | — | fear a beast **up to 10 s** |
| **Mend Pet** | **2** | **20** | **90 mana** | **Channeled 5 s** | — | pet | **38 health per second (190 total)** |
| **Revive Pet** | — | 10 | **100% of base mana** | 10 s cast | — | pet | pet returns with **15% health** |
| **Tame Beast** | — | 10 | **60% of base mana** | **20 s channel** | — | — | tame; armour reduced 100% while taming |
| **Beast Lore** | 1 | **24** | 0 | Instant | — | — | reads target beast's damage/health/armour/diet |
| **Eagle Eye / Eyes of the Beast** | 1 each | 14 | 12 (trainer) / 20 mana | Channeled 1 min | — | — | scouting utilities |
| **Call / Dismiss / Feed Pet, Beast Training** | — | 10 | 0 / 5 s cast / item | — | — | — | pet management |

**Not available at 24** (learn level or point gate): Rapid Fire (26), Feign Death (30), Flare (32), Wyvern Sting
(40), Viper Sting (36), Volley (40), Trueshot Aura (40), Tranquilizing Shot (?), Aspect of the Beast (30),
Aspect of the Pack (40), Aspect of the Wild (46), Frost Trap (28), Explosive Trap (34), Frost/Shadow/Nature
resistance aspects, Scatter Shot (talent, 21 points), Counterattack (30), Deterrence (talent), Bestial Wrath /
Intimidation (talent, BM). — **D** for learn levels, **D** for the "— = talent" trainer column.

## 5. Aspects and Tracking (exactly one Aspect and one Tracking active at a time)

| Active choice | At 24 | Numbers |
|---|---|---|
| Aspect of the Hawk r2 | **+35 ranged AP** (35 mana) | the default combat aspect |
| Aspect of the Monkey r1 | **+8% dodge** (free) | melee-survival / while running |
| Aspect of the Cheetah r1 | **+30% speed**, dazed 4 s on damage (22 mana) | travel only |
| Track Beasts / Humanoids / Undead / Hidden | 1 / 10 / 18 / **24** | utility, mutually exclusive |

Aspect of the Wild/Beast/Pack are all > level 24 — **D** (learn levels 30/40/46).

## 6. Rotation a level-24 hunter actually runs

Ability facts **D** (§4); the ordering, mana budget and DPS attribution are **I**.

1. **Hunter's Mark r2** (30 mana, 2 min) on the kill target — +45 ranged AP for you *and* your pet's attackers.
2. **Serpent Sting r3** on cooldown-free uptime: 50 mana → 80 Nature over 15 s. Nature damage, so it ignores armour.
3. **Multi-Shot r1** (100 mana, 10 s CD) whenever 2-3 targets are in front of you; on a single target it is
   100 mana for one weapon-damage hit — poor value unless the extra targets exist.
4. **Arcane Shot r3** (50 mana, 6 s CD, 33 Arcane) as the instant filler between Auto Shots. It is *not* worth
   spamming at this level: 50 mana per 33 damage is ~0.66 damage/mana.
5. **Auto Shot** is the backbone — 0 mana, ~41.7 DPS (§3). Every instant is squeezed between autos.
6. **Aimed Shot r1** (talent) opening shot: 75 mana, 3 s cast, +70 damage.
7. **Pet** sent in first (Growl 3 for 110 threat) so the mob is not on you; **Mend Pet r2** if it drops.
8. Survival tools: **Concussive Shot** (12 s CD, 4 s 50% slow) then **Wing Clip** (40 mana, 5 dmg, 50% slow
   10 s) to kite; **Mongoose Bite r1** only after a dodge; **Raptor Strike r4** if you must stand and melee;
   **Freezing Trap** pre-placed out of combat; **Scare Beast** vs beasts.
9. Mana reality check (**I**): a "full" 10 s of Serpent + Multi + two Arcane costs 50 + 100 + 100 = 250 mana, so
   the 798-mana pool buys ~32 s of that, then Auto Shot alone (0 mana) continues. Spirit regen is not modelled.

Single-target priority the sim should encode: `Auto Shot` always > `Serpent Sting` (refresh) > `Aimed Shot` (talent,
opener) > `Arcane Shot` while mana > ~40% > `Multi-Shot` only with >= 2 targets.

## 7. Pets at level 24

- **Tame Beast at 10**; pet max level = hunter level (24), and pets level only by fighting — **D** for the level-10
  gate and "newly-tamed pets remain at the level they are when you tame them" (Petopia Classic *Pet Differences*).
- **Training points = level x (loyalty - 1)** → a level-24 pet at loyalty 6 has **120 TP**; at loyalty 3, 48 TP —
  **D** (Petopia *How to Train Your Pet*). Max 4 active abilities per pet + unlimited passives — **D**.
- **Pet ability ranks reachable at pet level 24** (Petopia Classic *Pet Abilities*, "Minimum pet level" ladder) — **D**:

| Pet ability | Best rank at pet 24 | TP | Numbers |
|---|---|---|---|
| Growl | **3** (pet 20) | 0 | **110 threat** (+ taunt-like focus) — r1 50, r2 65, r3 110; free even at 0 TP |
| Bite | **4** (pet 24) | 10 | **31-37 damage** |
| Claw | **4** (pet 24) | 10 | **16-22 damage** |
| Charge | **3** (pet 24) | 13 | immobilise 1 s + **+180 melee AP** on the next attack (boars) |
| Furious Howl | **2** (pet 24) | 15 | **+18-22 damage** to the next physical attack of party members within 15 yd (wolves) |
| Lightning Breath | **3** (pet 24) | 10 | **36-40 Nature damage**, ranged (wind serpents) |
| Scorpid Poison | **2** (pet 24) | 15 | **15 Nature over 10 s** (scorpids) |
| Screech | **2** (pet 24) | 15 | **12-16 damage** + **-50 melee AP** to enemies in melee range (owls, bats, carrion birds) |
| Great Stamina | **4** (pet 24) | 25 | **+10 Stamina** |
| Natural Armor | **4** (pet 24) | 15 | **+240 armour** |
| Arcane/Fire/Frost/Nature/Shadow Resistance | **1** (pet 20) | 5 each | **+30 resistance** |
| Cower | **2** (pet 15) | 10 | **-55 threat** |
| Shell Shield | 1 (pet 20) | 15 | -50% damage taken, +43% attack interval (turtles) |
| Dash / Dive / Prowl / Thunderstomp | — | — | **pet level 30+ → NOT available at 24** |

- **Tameable families at 24:** Classic has 17 families (Bats, Bears, Boars, Carrion Birds, Cats, Crabs,
  Crocolisks, Gorillas, Hyenas, Owls, Raptors, Scorpids, Spiders, Tallstriders, Turtles, Wind Serpents, Wolves) —
  **D** (Petopia Classic home). Practically reachable at 24 are the families with members <= ~24 in low zones
  (cats, wolves, boars, bears, spiders, raptors, scorpids, crocolisks, tallstriders, carrion birds, bats, owls,
  crabs, hyenas, wind serpents); gorillas are mostly Stranglethorn 30+ — **I**, per-family spawn levels not verified.
- **Pet damage model (1.12 code, D):** pet melee damage = creature base damage x `DamageMultiplier` x happiness,
  where **happy x1.25 / content x1.00 / unhappy x0.75** (mangos-zero `StatSystem.cpp`,
  `if (getPetType() == HUNTER_PET ...)`). Pet AP/armour come from its own stats, not the hunter's.
- **Pet vs hunter damage share:** no numeric source found. Only qualitative anchor: a wind serpent using Lightning
  Breath "can augment your own ranged damage contribution to almost 30%" (29-twink hunter guide) — **D** as a claim,
  **U** as a measured value. Cats are claimed there to do "10% increase in all damage" — **single source, U**.
  Petopia only says families differ in DPS without publishing per-family multipliers on any page reachable here — **U**.

## 8. Mechanics the sim must model

| Ability | Kind | Duration / tick | Notes for the sim |
|---|---|---|---|
| Auto Shot | direct hit (physical) | continuous, weapon speed 1.8 s | damage = `weapon roll + ammo/shot + AP/14*speed`; 35 yd; interrupts on move |
| Arcane Shot r3 | direct hit (Arcane) | instant, 6 s CD | **33**; not reduced by armour; mana 50 |
| Serpent Sting r3 | **DoT (Nature)** | 15 s, **tick every 3 s, 5 ticks, 16/tick** | one Sting per hunter per target; refresh replaces |
| Scorpid Sting r1 | **debuff on enemy** | 20 s | -20 Str/-20 Agi; one Sting per hunter |
| Multi-Shot r1 | direct hit (physical) x3 | instant, 10 s CD | weapon damage per target, max 3; 100 mana |
| Aimed Shot r1 | direct hit (physical, cast) | 3 s cast, 6 s CD | +70; talent-gated |
| Concussive Shot r1 | **debuff (daze)** | 4 s, 50% slow, 12 s CD | **no damage**; 8% base mana |
| Wing Clip r1 | direct hit + **debuff (slow)** | 10 s, 50% | 5 damage; melee range 5 yd |
| Raptor Strike r4 | direct hit (melee, next swing) | 6 s CD | +34 |
| Mongoose Bite r1 | direct hit (melee counter) | 5 s CD, after dodge only | 25 |
| Hunter's Mark r2 | **debuff on enemy** | **120 s** | +45 ranged AP for **all** attackers, incl. pet |
| Aspect of the Hawk r2 | **self-buff** | until cancelled / replaced | +35 ranged AP; only one Aspect |
| Aspect of the Monkey r1 | self-buff | until cancelled | +8% dodge; only one Aspect |
| Aspect of the Cheetah r1 | self-buff (+risk) | until cancelled; 4 s daze on damage | +30% speed |
| Track * | self-utility | until cancelled / zone | minimap only — no combat effect |
| Immolation Trap r1 | **ground trap -> DoT (Fire)** | trap 60 s; **105 over 15 s**, tick 3 s | out of combat placement, one trap |
| Freezing Trap r1 | ground trap -> **hard CC** | freeze up to 10 s, breaks on damage; trap 60 s | out of combat placement |
| Scare Beast r1 | **CC (fear)** on beasts | up to 10 s, 1.5 s cast, 30 s CD | breaks on damage |
| Distracting Shot r2 / Disengage | threat tools | instant | no damage |
| Mend Pet r2 | **channeled heal (pet)** | 5 s channel, 38 hp/s = 190 | breaks if you move/cast |
| Revive Pet | pet resurrect | 10 s cast, 100% base mana | pet at 15% health |
| Growl 3 | **pet action (taunt/threat)** | instant, pet GCD | **+110 threat**; 4 s-ish pet taunt window |
| Bite 4 / Claw 4 | pet action, direct hit | pet attack, focus cost | 31-37 / 16-22 |
| Furious Howl 2 / Screech 2 / Charge 3 | pet action, party/pet buff + debuff | Howl: next attack only, 15 yd; Screech: -50 AP; Charge: 1 s immobilise +180 AP | wolves / owls-bats-carrion / boars |
| Lightning Breath 3 / Scorpid Poison 2 | pet action, direct (Nature) / pet DoT | LB instant 36-40; SP 15 over 10 s | wind serpents / scorpids |
| Happiness | pet state | persistent | damage x1.25 / x1.00 / x0.75 (happy/content/unhappy) |

## 9. Contradictions recorded (not resolved)

1. **Ranged attack power intercept: -20 (wiki) vs -10 (emulator).** https://warcraft.wiki.gg/wiki/Attack_power
   gives Hunter ranged AP = `(Level x 2) + (Agility x 2 - 20)`; mangos-zero `Object/StatSystem.cpp` computes
   `val2 = level*2.0f + AGI*2.0f - 10.0f`. At level 24 with 137 Agi that is **302 vs 312** ranged AP (2.5% more
   Auto Shot DPS on the emulator number). Everything else in both formulas agrees. Unresolved; pick one and stay in it.
2. **Crit-per-agility at level 24 has no direct client-table source.** The only client-data figure found is,
   via the emulator, an interpolation between the level-1 (3.5) and level-60 (53) values → 22.80 AGI per 1% at 24.
   The only published number is the level-60 "53 AGI = 1% crit". Any sim that applies 53/AGI at level 24 will
   understate a hunter's crit by ~2.3x. Recorded, not resolved.
3. **Multi-Shot r1 damage per target.** Both 1.12 (classicdb `?spell=2643`) and 1.15.9 (wowcalc) tooltips say only
   "Fires several missiles, hitting 3 targets" with no flat bonus, while community/GM descriptions claim each
   target takes weapon damage plus a per-rank bonus (r2 +40, r3 +80, ...). Both readings recorded; **U** for r1.
4. **Serpent Sting tick structure** (5 ticks of 16 at 3 s intervals) is **derived** from "80 damage over 15 sec";
   no fetched page prints the tick schedule. The 15 s duration and 80 total are **D**.
5. **Pet family damage differences.** Petopia states families differ in DPS but publishes no per-family
   multiplier on any page reachable this run; the 29-twink guide claims cats get "+10% damage" and that a
   wind-serpent pet adds "almost 30%" of a hunter's ranged damage. Single-source, **U**.
6. **Freezing Trap cooldown** could not be read from any fetched page (1.12 classicdb page shows the effect, not a
   cooldown); **U**, do not invent one.
7. **Base crit chance (5% melee / 5% ranged at every level)** is standard retail-wiki text, not a vanilla-1.12
   statement; the emulator reconciliation was not performed — **U**.

## 10. Missing evidence

- **Head slot** and **melee stat-stick** at req <= 24: no fixed-stat item with agility exists in the dataset;
  those slots are suffix greens whose concrete rolls are **omitted, not zero** in §3.
- **Trinket**: only the PvP Insignia (no stats) qualifies; AB/WSG stat trinkets need Honored rep.
- **Quiver/ammo-pouch ranged haste** was not verified for level <= 24 containers — **U**; ammo DPS alone is modelled.
- **No measured level-24 character sheet** (armour, crit, dodge, AP) from a live client; §3 is formula-derived.
- **Ranged miss/dodge/parry table at level 24** and the level-24 weapon-skill effect on a same-level mob: not found.
- **Arcane Shot / Serpent Sting scaling with ranged AP** (whether the sim should add AP-derived damage to these
  shots) is **U** — the tooltips give flat damage only, and no fetched page documents a coefficient.
- **Talent point cost / tier position** of every talent-gated ability (Aimed Shot, Deterrence, Intimidation,
  Scatter Shot, Counterattack) — only "Requires Hunter (Marksmanship/Survival/Beast Mastery)" labels were obtained.
- **Per-family pet spawn levels** used to justify "reachable at 24" are **I**, not verified per zone.

## 11. Sources

- **Kept: `wow_world_base` `extended/vanilla/base_stats.rs`** —
  https://docs.rs/wow_world_base/latest/src/wow_world_base/extended/vanilla/base_stats.rs.html — every level-24
  Str/Agi/Sta/Int/Spi/base-hp/base-mana row (already extracted in `.pi/research/level24/base_stats_new_classes.md`).
- **Kept: mangos-zero 1.12 server source** —
  https://raw.githubusercontent.com/mangoszero/server/master/src/game/Object/StatSystem.cpp (ranged/melee AP,
  armour from agility, stamina→health, intellect→mana, pet happiness multipliers) and
  https://raw.githubusercontent.com/mangoszero/server/master/src/game/Object/PlayerStats.cpp (crit and dodge per
  agility, class-rate interpolation).
- **Kept: https://warcraft.wiki.gg/wiki/Attack_power** — per-class AP formulas and the "1 DPS per 14 AP" rule
  (fetched via r.jina.ai because api.php is Cloudflare-blocked).
- **Kept: https://warcraft.wiki.gg/wiki/Mail_armor** — hunters wear leather only until level 40 (vanilla).
- **Kept: https://warcraft.wiki.gg/wiki/Hunter_abilities_(Classic)** — learn level / source per ability
  (the "Trainer" column value "—" marks talent-gated abilities); extracted table in `/tmp/h/hunter_rows.txt`.
- **Kept: https://wowcalc.net/en/classic/wiki/spells/hunter** — every rank with learn level, mana, cast time,
  cooldown (client build 1.15.9.70003).
- **Kept: classicdb.ch (1.12) spell pages** — `?spell=75` (Auto Shot, 35 yd), `3044` (Arcane Shot),
  `1978` (Serpent Sting), `2643` (Multi-Shot), `5116` (Concussive), `1130` (Hunter's Mark, 100 yd),
  `2974` (Wing Clip), `2973` (Raptor Strike), `1495` (Mongoose Bite), `13165` (Aspect of the Hawk),
  `2649` (Growl).
- **Kept: `wow-classic-items` v2.0.1 dataset** (https://github.com/Nakroma/wow-classic-items, Wowhead-Classic
  scrape) — every item row in §2 filtered to `requiredLevel <= 24`, `itemLevel <= 35`.
- **Kept: https://www.wow-petopia.com/classic/** — `abilities.php` (pet ability ranks, pet-level requirements, TP
  costs, Growl threat 50/65/110/170/240/320/415), `training.php` (TP formula = level x (loyalty-1), 4 active
  abilities), `differences.php` (pets stay at tamed level, happiness/loyalty), home page (17 families).
- **Kept: https://www.warcrafttavern.com/wow-classic/guides/29-twink-hunter/** — candidate twink item pool and the
  cat "+10% damage" / wind-serpent "almost 30% of your ranged damage" claims (recorded as single-source).
- **Rejected: wowhead.com/classic** — HTTP 403 for this client.
- **Rejected: warcraft.wiki.gg `api.php`** — returns a Cloudflare "Blocked" page; `r.jina.ai` used instead.
- **Rejected: vanilla-wow-archive / wowwiki-archive / wowpedia (fandom)** — Cloudflare CAPTCHA challenge pages.

## 12. Next steps

1. Decide the AP intercept (-20 wiki vs -10 emulator) and the crit-per-agility source before wiring the class into
   the sim; both change damage by a few percent but they must be consistent across classes.
2. Get one real level-24 hunter character sheet to replace the formula-derived pools in §3.
3. Resolve Multi-Shot r1 per-target damage and Freezing Trap's cooldown (both currently **U**).
4. Enumerate the <= 24 suffix-green pool for head/back-up slots with concrete "of the Monkey" rolls.
5. Pin down pet stat scaling (pet level → damage/AP/armour) and the hunter:pet damage split with a measured parse,
   not a twink-guide claim.
