# Warrior, Rogue, Priest, Druid and Mage — levelling gear bands 23–27, 28–32, 33–37, 38–40

Gear for the five classes whose ladders were older and thinner — **Warrior (tank, mail to 39 then plate at 40),
Rogue (DPS, leather), Priest (healer, cloth), Druid (healer, leather), Mage (DPS, cloth)** — in the four bands
the existing files never covered, or covered with `U` rows.

Same standard as `gear-ladder-rest.md`, `warrior-rogue-ladder.md`, `priest-druid-ladder.md` and `mage-ladder.md`:
*plausible high-achievement levelling gear, not best-in-slot*. A row is something a real player plausibly wears
at that level, obtainable by quest, dungeon drop, world-drop BoE or the auction house — not a raid item, not
battleground rank gear, not something that only drops off elites five levels above the party.

## Band floors, and what this gear has to clear

The game's band boundaries are **23, 28, 33, 38** — each table below holds items whose required level is at
least the band floor and at most the next floor − 1 (so a 38–40 table holds rl 38–40, which is the top of the
sourced data; see Missing evidence). Required level is what the game checks, so every row's `Req` is inside
its band by construction. Items that a class *keeps* from an earlier band (because nothing better exists in
band) are flagged in the band note rather than silently re-listed.

The two wings this gear must clear:

| Wing | Party level | Why it matters here |
|---|---:|---|
| Scarlet Monastery — **Library** | 36 | Arcanist Doan drops `Hypnotic Blade` (rl 34) and the Library's casters define the 33–37 caster step |
| Scarlet Monastery — **Cathedral** | 41 | `Mograine's Might` (39), `Whitemane's Chapeau` (39), `Hand of Righteousness` (39) — the 38–40 top end |

## How to read a row

| Column | Meaning |
|---|---|
| `Req` | required level from the item page |
| `Arm` | armour value from the item page, unmodified |
| `Dmg` / `Spd` | weapon damage range and swing speed in seconds; `—` on armour rows |
| `Str … Spi` | the five Vanilla stats, `0` where the item has none |
| `Where` | `BoE` = bind-on-equip (auction house / world drop table), `BoP · <mob> <chance>` = boss/elite drop, `unbound (craft/vendor)` = no bind |
| `Src` | `e<id>` = `database.emberveil.org/item/<id>`, `w<id>` = `nether.wowhead.com/classic/tooltip/item/<id>` |
| `Conf` | `V` verified from one cited page, `M` corroborated by a second, `U` inference — never ship a `U` as fact |

**Every row below is `M`, except the exceptions listed in this paragraph.** Each row was read on two hosts:
the server-rendered item page on `database.emberveil.org` and the Wowhead Classic tooltip JSON on
`nether.wowhead.com` (`www.wowhead.com` answers 403 to plain fetches; the tooltip host does not). Required
level, armour, all five stats and weapon damage/speed agree field for field, with these exceptions:

- **Weapon rows were re-read on both hosts during this pass** (11 weapons: `6909`, `6327`, `7717`, `7723`,
  `6689`, `7757`, `9408`, `19568`, `5214`, `7708`, `13064`, `5216` plus `6220`, `6691`, `7714`) and are `M`.
- **Armour rows carried in from `gear-ladder-rest.md`** reuse that file's two-host read: the Wowhead side of
  every carried id was re-read in this pass and matched; the emberveil side is the read recorded there
  (`e<id>` in its tables). Where a cell could not be re-derived it is marked, not guessed.
- **Wand minimum–maximum damage is published only by Wowhead.** emberveil publishes average damage and dps;
  on all four wands below, average × 2 equals min + max exactly (33.5→23–44, 46.5→32–61, 63.0→44–82,
  53.5→37–70), so the range is corroborated arithmetically rather than by a second published pair. Treated
  as `M` on that basis.
- **Random-suffix greens** (`Ranger Leggings`) show `<Random enchantment>` on *both* hosts and no fixed stat
  line. They are shipped for their **armour** only, with the five stat cells at the base value `0`; their
  affix varies ±1–3 per stat. Marked in the row note, not silently filled.
- **Excluded items** are named in the class notes where a class-legal or in-band item was dropped for a
  reason (reachability, PvP source, or one host lacking it).

`classicdb.ch`, `classicdb.com` and `warcraft.wiki.gg` remain unusable (did not resolve / HTTP 403) and are
not cited.

---

## 1. Warrior — mail 23–39, plate at 40

Armour class: **mail** until plate is trainable at **40** (`README.md` armour rule, same as the warrior
ladder). Leather is wearable below mail but is an armour downgrade and is not used here.
Weapon proficiency: one- and two-handed **swords, axes, maces, polearms**, daggers, fists, **shields**.

Warrior is the one class in this file that can use every weapon that beats the picks below, so the picks are
chosen on the axe/mace/sword lines rather than restricted by proficiency. Tank note: a shield (the SM shield
`Aegis of the Scarlet Commander`, rl 39) occupies the other hand and is **not modelled** by
`AutoAttack { Min; Max; Ticks; Range }` (`src/WowRecorLike.Core/Domain.fs:622` — `AutoAttack { Min; Max; Ticks; Range }`),
so every weapon row here is two-handed.

### Band 23–27 — mail

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Mail) | Frostreaver Crown | 27 | 182 | — | — | 4 | 0 | 15 | 0 | 4 | BoE | e13127/w13127 | M |
| Neck | Stygian Bone Amulet | 27 | — | — | — | 0 | 0 | 4 | 0 | 8 | BoP · Blind Hunter Rare Elite 33.3 % | e6695/w6695 | M |
| Shoulder (Mail) | Elite Shoulders | 25 | 149 | — | — | 6 | 0 | 5 | 0 | 0 | BoE | e4835/w4835 | M |
| Back | Enduring Cape | 27 | 22 | — | — | 2 | 2 | 7 | 0 | 0 | BoE | e14763/w14763 | M |
| Chest (Mail) | Avenger's Armor | 26 | 221 | — | — | 15 | 0 | 6 | 0 | 0 | BoE | e1488/w1488 | M |
| Wrist (Mail) | Pugilist Bracers | 25 | 95 | — | — | 8 | 0 | 3 | 0 | 0 | BoE | e4438/w4438 | M |
| Hands (Mail) | Algae Fists | 23 | 132 | — | — | 10 | 0 | 4 | 0 | 0 | BoP · Gelihast Elite 50.0 % | e6906/w6906 | M |
| Waist (Mail) | Slayer's Sash | 23 | 108 | — | — | 6 | 0 | 4 | 0 | 0 | BoE | e14755/w14755 | M |
| Legs (Mail) | Green Iron Leggings | 26 | 176 | — | — | 8 | 0 | 8 | 0 | 0 | BoE | e3842/w3842 | M |
| Feet (Mail) | Caverndeep Trudgers | 27 | 154 | — | — | 4 | 4 | 10 | 0 | 0 | BoE | e9510/w9510 | M |
| Finger | Thunderbrow Ring | 24 | — | — | — | 8 | 3 | 0 | 0 | 0 | BoE | e13097/w13097 | M |
| Finger | Plains Ring | 24 | — | — | — | 0 | 0 | 8 | 3 | 0 | BoE | e2039/w2039 | M |
| **Weapon** (2H sword) | **Strike of the Hydra** | 26 | — | **67–102** | **3.30** | 0 | 0 | 0 | 0 | 0 | BoP · Aku'mai Elite 33.3 % | e6909/w6909 | M |

**Weapon — Strike of the Hydra:** `AutoAttack { Min = 67; Max = 102; Ticks = secTicks 3.3; Range = 1 }` — 25.6 dps.

**Band armour total: 1239.** First band where every slot has a statted mail item, and the band where
`warrior-rogue-ladder.md` §W2 has no verified armour values at all (its row "mail from Gnomeregan / Razorfen
Downs, no specific BiS verified") — this table replaces that gap with a full 1239-armour set. Weapon: Strike
of the Hydra (2H sword, Aku'mai in Blackfathom Deeps 33 %), warrior-legal on the sword line; the reference
file's note that *Corpsemaker* (rl 29, 2H axe, 88–132 @ 3.80) is the axe alternative is **not** re-verified
here and is marked `U` — do not ship its dps.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---
### Band 28–32 — mail

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Mail) | Slayer's Skullcap | 28 | 168 | — | — | 9 | 3 | 7 | 0 | 0 | BoE | e14753/w14753 | M |
| Neck | River Pride Choker | 28 | — | — | — | 4 | 0 | 9 | 0 | 0 | BoE | e13087/w13087 | M |
| Shoulder (Mail) | Mail Combat Spaulders | 30 | 160 | — | — | 7 | 0 | 0 | 0 | 7 | BoE | e6404/w6404 | M |
| Back | Tigerstrike Mantle | 29 | 25 | — | — | 0 | 8 | 7 | 0 | 0 | BoE | e13108/w13108 | M |
| Chest (Mail) | Ironspine's Ribcage | 30 | 235 | — | — | 6 | 3 | 17 | 0 | 0 | BoP · Ironspine Rare Elite 33.3 % | e7688/w7688 | M |
| Wrist (Mail) | Chief Brigadier Bracers | 32 | 96 | — | — | 3 | 0 | 7 | 0 | 0 | BoE | e6413/w6413 | M |
| Hands (Mail) | Reticulated Bone Gauntlets | 31 | 149 | — | — | 13 | 0 | 5 | 0 | 0 | BoE | e9435/w9435 | M |
| Waist (Mail) | Scarlet Belt | 32 | 123 | — | — | 8 | 0 | 7 | 0 | 0 | BoE | e10329/w10329 | M |
| Legs (Mail) | Enduring Breeches | 32 | 192 | — | — | 8 | 8 | 6 | 0 | 0 | BoE | e14766/w14766 | M |
| Feet (Mail) | Black Ogre Kickers | 32 | 166 | — | — | 6 | 0 | 13 | 0 | 0 | BoE | e1678/w1678 | M |
| Finger | Blush Ember Ring | 32 | — | — | — | 4 | 5 | 0 | 0 | 8 | BoE | e13093/w13093 | M |
| Finger | Ring of the Underwood | 31 | — | — | — | 2 | 10 | 3 | 0 | 0 | BoE | e2951/w2951 | M |
| **Weapon** (2H mace) | **The Pacifier** | 32 | — | **104–156** | **4.00** | 18 | 0 | 7 | 0 | 0 | BoE | e6327/w6327 | M |

**Weapon — The Pacifier:** `AutoAttack { Min = 104; Max = 156; Ticks = secTicks 4.0; Range = 1 }` — 32.5 dps.

**Band armour total: 1314.** Slowest, biggest top-end weapon in the file for this band (104–156 @ 4.00):
`MeleeDamageBonus = AttackPower * Ticks / 280` rewards the slow swing, and the +18 Str feeds it. *The
Shoveler* (rl 32, 88–133 @ 3.40, same 32.5 dps) is the exact-dps alternative if the Pacifier is not on the AH.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---
### Band 33–37 — mail

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Mail) | Raging Berserker's Helm | 37 | 213 | — | — | 13 | 0 | 8 | 0 | 0 | BoP · Herod Elite 25.0 % | e7719/w7719 | M |
| Neck | Gazlowe's Charm | 36 | — | — | — | 4 | 0 | 11 | 0 | 3 | BoE | e13088/w13088 | M |
| Shoulder (Mail) | Herod's Shoulder | 37 | 196 | — | — | 6 | 0 | 15 | 0 | 0 | BoP · Herod Elite 25.0 % | e7718/w7718 | M |
| Back | Silky Spider Cape | 35 | 30 | — | — | 0 | 0 | 11 | 0 | 5 | BoP · Tuten'kash Elite | e10776/w10776 | M |
| Chest (Mail) | Scarlet Chestpiece | 34 | 250 | — | — | 8 | 0 | 19 | 0 | 0 | BoE | e10328/w10328 | M |
| Wrist (Mail) | Crushridge Bindings | 36 | 113 | — | — | 5 | 0 | 11 | 0 | 0 | BoE | e13199/w13199 | M |
| Hands (Mail) | Ravager's Handwraps | 35 | 144 | — | — | 9 | 0 | 6 | 0 | 0 | BoE | e14772/w14772 | M |
| Waist (Mail) | Heavy Notched Belt | 37 | 134 | — | — | 10 | 0 | 6 | 0 | 0 | BoE | e12257/w12257 | M |
| Legs (Mail) | Legguards of the Vault | 34 | 218 | — | — | 13 | 5 | 13 | 0 | 5 | BoE | e9396/w9396 | M |
| Feet (Mail) | Chief Brigadier Boots | 34 | 156 | — | — | 8 | 0 | 8 | 0 | 0 | BoE | e6412/w6412 | M |
| Finger | Inscribed Gold Ring | 35 | — | — | — | 2 | 0 | 8 | 0 | 0 | BoE | e5010/w5010 | M |
| Finger | Welken Ring | 35 | — | — | — | 0 | 0 | 0 | 5 | 7 | BoE | e5011/w5011 | M |
| **Weapon** (2H axe) | **Ravager** | 37 | — | **104–157** | **3.50** | 0 | 0 | 0 | 0 | 0 | BoP · Herod Elite 25.0 % | e7717/w7717 | M |

**Weapon — Ravager:** `AutoAttack { Min = 104; Max = 157; Ticks = secTicks 3.5; Range = 1 }` — 37.3 dps.

**Band armour total: 1454.** This band is the SM Armory band: `Herod's Shoulder` and `Ravager` come off the
same boss (Herod, 25 %). `warrior-rogue-ladder.md` §W2 already had Herod's Shoulder at 196 armour and
Ravager with `dmg U (87–131 claimed)` — **corrected here to 104–157 @ 3.50 (37.3 dps), read on both hosts**,
so the old `U` cell can be discarded. The proc (extra attacks) is not modelled; the sim sees only the 37.3 dps.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---
### Band 38–40 — plate at 40 (mail at 38–39)

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Plate) | Icemetal Barbute | 40 | 383 | — | — | 15 | 0 | 10 | 0 | 7 | BoP · Amnennar the Coldbringer Elite 20.0 % | e10763/w10763 | M |
| Neck | Shriveled Heart | 40 | — | — | — | 0 | 0 | 13 | 0 | 0 | BoP · Sandfury Cretin Elite | e9243/w9243 | M |
| Shoulder (Plate) | Chromite Pauldrons | 40 | 327 | — | — | 10 | 0 | 9 | 0 | 0 | BoE | e8144/w8144 | M |
| Back | Khan's Cloak | 39 | 29 | — | — | 4 | 0 | 8 | 0 | 1 | BoE | e14781/w14781 | M |
| Chest (Plate) | Carapace of Tuten'kash | 40 | 373 | — | — | 10 | 8 | 15 | 0 | 0 | BoP · Tuten'kash Elite | e10775/w10775 | M |
| Wrist (Plate) | Skullplate Bracers | 40 | 163 | — | — | 5 | 0 | 9 | 0 | 5 | BoE | e9432/w9432 | M |
| Hands (Plate) | Truesilver Gauntlets | 40 | 300 | — | — | 16 | 0 | 7 | 0 | 0 | BoE | e7938/w7938 | M |
| Waist (Plate) | Enormous Ogre Belt | 40 | 164 | — | — | 12 | 0 | 5 | 0 | 5 | BoE | e13145/w13145 | M |
| Legs (Plate) | Symbolic Legplates | 40 | 297 | — | — | 15 | 6 | 0 | 0 | 0 | BoE | e14829/w14829 | M |
| Feet (Plate) | Obsidian Greaves | 40 | 257 | — | — | 10 | 6 | 10 | 0 | 0 | BoE | e13068/w13068 | M |
| Finger | Assault Band | 39 | — | — | — | 0 | 0 | 7 | 0 | 5 | BoE | e13095/w13095 | M |
| Finger | Falcon's Hook | 39 | — | — | — | 3 | 9 | 0 | 0 | 0 | BoE | e7552/w7552 | M |
| **Weapon** (2H mace) | **Mograine's Might** | 39 | — | **87–131** | **2.80** | 0 | 0 | 17 | 0 | 16 | BoP · Scarlet Commander Mograine Elite 25.0 % | e7723/w7723 | M |

**Weapon — Mograine's Might:** `AutoAttack { Min = 87; Max = 131; Ticks = secTicks 2.8; Range = 1 }` — 38.9 dps.

**Band armour total: 2293 — at level 40 only.** Plate is trainable at 40, and every armour piece above is
rl 39–40 plate, so a warrior at 38–39 is still in mail: keep `Scarlet Chestpiece` (34, 250), `Scarlet Leggings`
(38, 233, +20 Str/+10 Sta), `Herod's Shoulder` (37, 196) and `Legguards of the Vault` (34, 218) until 40, which
is an armour total of ~1454 rather than 2293. Weapon: Mograine's Might, out of SM Cathedral — the same item
that closes the `dmg U` cell in `warrior-rogue-ladder.md` §W2. *The Jackhammer* (rl 40, 79–119 @ 2.50,
39.6 dps) is marginally ahead on dps and behind on hit size; it is **not** re-verified here.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---

## 2. Rogue — leather throughout

Armour class: **leather 1–40, no upgrade** (rogues never get mail).
Weapon proficiency: **daggers, swords, fists** (no maces, no axes, no staves, no two-handers).
Backstab requires a **dagger**, so the weapon row is a dagger in every band; a sword for a
Sinister-Strike build is named in the notes where one is sourced.

Legality note carried from the old file: `Swinetusk Shank` is a dagger and therefore a *rogue* weapon, not the
warrior's — the old §W2 row "Swinetusk Shank is rogue-only in practice" is correct and is why this class owns it.

### Band 23–27 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Leather) | Humbert's Helm | 25 | 76 | — | — | 0 | 7 | 8 | 0 | 0 | BoE | e4724/w4724 | M |
| Neck | Wolfpack Medallion | 26 | — | — | — | 0 | 3 | 5 | 0 | 0 | BoE | e5754/w5754 | M |
| Shoulder (Leather) | Mantle of Thieves | 25 | 77 | — | — | 0 | 10 | 4 | 0 | 3 | BoE | e2264/w2264 | M |
| Back | Enduring Cape | 27 | 22 | — | — | 2 | 2 | 7 | 0 | 0 | BoE | e14763/w14763 | M |
| Chest (Leather) | Emblazoned Chestpiece | 27 | 97 | — | — | 0 | 7 | 6 | 0 | 6 | BoE | e6396/w6396 | M |
| Wrist (Leather) | Barbaric Bracers | 27 | 47 | — | — | 0 | 4 | 6 | 0 | 4 | BoE | e18948/w18948 | M |
| Hands (Leather) | Emblazoned Gloves | 24 | 57 | — | — | 0 | 5 | 6 | 0 | 0 | BoE | e6397/w6397 | M |
| Waist (Leather) | Green Leather Belt | 27 | 54 | — | — | 0 | 6 | 0 | 0 | 6 | BoE | e4257/w4257 | M |
| Legs (Leather) | Petrolspill Leggings | 25 | 90 | — | — | 0 | 14 | 6 | 0 | 0 | BoE | e9509/w9509 | M |
| Feet (Leather) | Harbinger Boots | 25 | 71 | — | — | 2 | 0 | 11 | 0 | 3 | BoE | e7754/w7754 | M |
| Finger | Plains Ring | 24 | — | — | — | 0 | 0 | 8 | 3 | 0 | BoE | e2039/w2039 | M |
| Finger | Thunderbrow Ring | 24 | — | — | — | 8 | 3 | 0 | 0 | 0 | BoE | e13097/w13097 | M |
| **Weapon** (dagger) | **Meteor Shard** | 24 | — | **23–43** | **1.80** | 0 | 0 | 0 | 0 | 0 | BoP · Archmage Arugal Elite 33.3 % | e6220/w6220 | M |

**Weapon — Meteor Shard:** `AutoAttack { Min = 23; Max = 43; Ticks = secTicks 1.8; Range = 1 }` — 18.3 dps
(plus a 3 % chance-on-hit 35 Fire blast; procs are not modelled).

**Band armour total: 591.** 14 Agi on the legs and 10 on the shoulders carry the band; the rest is
Sta/Spi filler, which is what the leather pool looks like before Uldaman. Weapon: Meteor Shard, and this
settles the old file's **Meteor Shard required-level conflict (16 vs 24) in favour of 24** — both hosts say
rl 24, so `warrior-rogue-ladder.md` §R2's `U` row can be corrected. Sourced sword alternative for a
Sinister-Strike build: `Shadowfang` (rl 19, 29–55 @ 2.70, 17.8 dps) — below the dagger on dps, listed only so
the sword line is not silently missing.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---
### Band 28–32 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Leather) | Adventurer's Pith Helmet | 32 | 94 | — | — | 0 | 9 | 14 | 0 | 7 | BoE | e9420/w9420 | M |
| Neck | Kaleidoscope Chain | 30 | — | — | — | 4 | 4 | 4 | 4 | 4 | BoE | e13084/w13084 | M |
| Shoulder (Leather) | Barbaric Shoulders | 30 | 76 | — | — | 0 | 5 | 6 | 0 | 5 | BoE | e5964/w5964 | M |
| Back | Tigerstrike Mantle | 29 | 25 | — | — | 0 | 8 | 7 | 0 | 0 | BoE | e13108/w13108 | M |
| Chest (Leather) | Wolffear Harness | 31 | 113 | — | — | 0 | 17 | 6 | 0 | 3 | BoE | e13110/w13110 | M |
| Wrist (Leather) | Glyphed Bracers | 32 | 46 | — | — | 0 | 6 | 4 | 0 | 0 | BoE | e4059/w4059 | M |
| Hands (Leather) | Hawkeye's Gloves | 32 | 65 | — | — | 0 | 8 | 7 | 0 | 0 | BoE | e14594/w14594 | M |
| Waist (Leather) | Hawkeye's Cord | 31 | 58 | — | — | 4 | 6 | 6 | 0 | 0 | BoE | e14588/w14588 | M |
| Legs (Leather) | Dusky Leather Leggings | 28 | 86 | — | — | 0 | 13 | 0 | 0 | 0 | BoE | e7373/w7373 | M |
| Feet (Leather) | Swampwalker Boots | 32 | 79 | — | — | 0 | 13 | 6 | 0 | 0 | BoE | e2276/w2276 | M |
| Finger | Ring of the Underwood | 31 | — | — | — | 2 | 10 | 3 | 0 | 0 | BoE | e2951/w2951 | M |
| Finger | Ironspine's Eye | 30 | — | — | — | 4 | 9 | 0 | 0 | 0 | BoP · Ironspine Rare Elite 33.3 % | e7686/w7686 | M |
| **Weapon** (dagger) | **Swinetusk Shank** | 30 | — | **24–45** | **1.50** | 0 | 0 | 6 | 0 | 4 | BoP · Agathelos the Raging Elite 50.0 % | e6691/w6691 | M |

**Weapon — Swinetusk Shank:** `AutoAttack { Min = 24; Max = 45; Ticks = secTicks 1.5; Range = 1 }` — 23.0 dps.

**Band armour total: 642.** Fastest dagger in the warrior/rogue data (1.50 s) with +6 Sta/+4 Spi — the
classic RFK rogue pick, and the reason `warrior-rogue-ladder.md` §R2's "28–32: Swinetusk Shank, 30,
24–45 @ 1.50 (23.0 dps)" row is now sourced on both hosts rather than `M`-by-extraction.
Alternatives: `Toxic Revenger` (rl 27, 27–51 @ 1.90, 20.5 dps, Gnomeregan) and `Sliverblade` (rl 32) — the
latter was **not** re-verified this pass, so its numbers are not printed.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---
### Band 33–37 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Leather) | Nightscape Headband | 36 | 91 | — | — | 0 | 12 | 11 | 0 | 0 | BoE | e8176/w8176 | M |
| Neck | Gazlowe's Charm | 36 | — | — | — | 4 | 0 | 11 | 0 | 3 | BoE | e13088/w13088 | M |
| Shoulder (Leather) | Flintrock Shoulders | 33 | 88 | — | — | 5 | 10 | 10 | 0 | 0 | BoE | e7755/w7755 | M |
| Back | Wing of the Whelpling | 33 | 28 | — | — | 0 | 4 | 10 | 0 | 0 | BoE | e13121/w13121 | M |
| Chest (Leather) | Quillward Harness | 34 | 120 | — | — | 8 | 19 | 0 | 0 | 0 | BoE | e10583/w10583 | M |
| Wrist (Leather) | Imperial Leather Bracers | 37 | 50 | — | — | 0 | 8 | 4 | 0 | 0 | BoE | e4061/w4061 | M |
| Hands (Leather) | Swine Fists | 34 | 68 | — | — | 8 | 8 | 0 | 0 | 0 | BoP · Plaguemaw the Rotting Elite | e10760/w10760 | M |
| Waist (Leather) | Ogron's Sash | 37 | 71 | — | — | 9 | 9 | 9 | 0 | 0 | BoE | e13117/w13117 | M |
| Legs (Leather) | Hawkeye's Breeches | 33 | 93 | — | — | 0 | 14 | 3 | 0 | 0 | BoE | e14595/w14595 | M |
| Feet (Leather) | Dusky Boots | 35 | 76 | — | — | 0 | 11 | 3 | 0 | 0 | BoE | e7390/w7390 | M |
| Finger | Inscribed Gold Ring | 35 | — | — | — | 2 | 0 | 8 | 0 | 0 | BoE | e5010/w5010 | M |
| Finger | Prismstone Ring | 35 | — | — | — | 0 | 0 | 0 | 9 | 0 | BoE | e5743/w5743 | M |
| **Weapon** (dagger) | **Hypnotic Blade** | 34 | — | **26–49** | **1.40** | 0 | 8 | 0 | 3 | 0 | BoP · Arcanist Doan Elite 25.0 % | e7714/w7714 | M |

**Weapon — Hypnotic Blade:** `AutoAttack { Min = 26; Max = 49; Ticks = secTicks 1.4; Range = 1 }` — 26.8 dps.

**Band armour total: 685.** 19 Agi on the chest is the single biggest damage stat in the file for a rogue.
Weapon: Hypnotic Blade, Arcanist Doan in SM **Library** (25 %) — exactly the wing this gear is built to clear.
Its +8 Int/+3 Spi and +9 spell damage are wasted on a rogue; the dps is why it is here.
**Slot caveat:** Wowhead prints `Main Hand`, emberveil prints `One-Hand`. A main-hand dagger cannot be
off-handers' dual-wield partner, which does not affect the single-weapon `AutoAttack` model but does mean the
sim should not treat it as a second dagger.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---
### Band 38–40 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Leather) | Nightscape Headband | 36 | 91 | — | — | 0 | 12 | 11 | 0 | 0 | BoE | e8176/w8176 | M |
| Neck | Gazlowe's Charm | 36 | — | — | — | 4 | 0 | 11 | 0 | 3 | BoE | e13088/w13088 | M |
| Shoulder (Leather) | Imperial Leather Spaulders | 39 | 90 | — | — | 9 | 0 | 0 | 0 | 9 | BoE | e4737/w4737 | M |
| Back | Wing of the Whelpling | 33 | 28 | — | — | 0 | 4 | 10 | 0 | 0 | BoE | e13121/w13121 | M |
| Chest (Leather) | Quillward Harness | 34 | 120 | — | — | 8 | 19 | 0 | 0 | 0 | BoE | e10583/w10583 | M |
| Wrist (Leather) | Imperial Leather Bracers | 37 | 50 | — | — | 0 | 8 | 4 | 0 | 0 | BoE | e4061/w4061 | M |
| Hands (Leather) | Imperial Leather Gloves | 38 | 73 | — | — | 9 | 9 | 0 | 0 | 0 | BoE | e4063/w4063 | M |
| Waist (Leather) | Ogron's Sash | 37 | 71 | — | — | 9 | 9 | 9 | 0 | 0 | BoE | e13117/w13117 | M |
| Legs (Leather) | Ranger Leggings | 38 | 103 | — | — | 0 | 0 | 0 | 0 | 0 | BoE | e7478/w7478 | M |
| Feet (Leather) | Imperial Leather Boots | 38 | 81 | — | — | 5 | 11 | 0 | 0 | 0 | BoE | e6431/w6431 | M |
| Finger | Falcon's Hook | 39 | — | — | — | 3 | 9 | 0 | 0 | 0 | BoE | e7552/w7552 | M |
| Finger | Inscribed Gold Ring | 35 | — | — | — | 2 | 0 | 8 | 0 | 0 | BoE | e5010/w5010 | M |
| **Weapon** (dagger) | **Hypnotic Blade** | 34 | — | **26–49** | **1.40** | 0 | 8 | 0 | 3 | 0 | BoP · Arcanist Doan Elite 25.0 % | e7714/w7714 | M |

**Band armour total: 707** (up from 685). Six of the twelve slots are in-band; the rest are carried because
**no fixed-stat leather in rl 38–40 beats them on Agility.** The in-band leather pool at 38–40 is 52 items and
is dominated by random-suffix greens (`Nocturnal Cap` 38/95, `Wolf Rider's Leggings` 39/105) whose base page
shows armour and no stats, plus the fixed `Imperial Leather` set (`Str/Agi` and `Str/Spi` lines) — which is why
the Agi pieces stay. `Ranger Leggings` (rl 38, **103 armour**, `<Random enchantment>`) is shipped for its armour
only: 10 more armour than Hawkeye's Breeches and strictly better than nothing, but buy an *of the Monkey* roll.

**Weapon — no in-band rogue weapon is reachable.** `Sacrificial Kris` (rl 39, 25–47 @ 1.40, 25.7 dps, BoE) is
**excluded**: emberveil lists its drop table as level 43–45 rare elites (Zul'Farrak, Tanaris, the Hinterlands,
Feralas) at 0.64 % — above the SM party level, so it fails the reachability standard and is not shipped as a
row. `Sentinel's Blade` / `Scout's Blade` (rl 38) are battleground rewards and are excluded by the same rule as
the PvP rings. Hypnotic Blade (26.8 dps, rl 34) therefore remains the rogue's weapon at 40, and it out-dps's
every reachable dagger in the band.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---

## 3. Priest — cloth, healer

Armour class: **cloth only**.
Weapon: **one-hand maces, staves, daggers, wands**. Priest cannot use swords or axes.
Two weapon rows are given per band where a wand exists, because the sim's caster auto-attack can be either:
the staff is the melee swing (`Range = 1`), the wand the ranged one (`Range = 8`, following the mage ladder's
wand convention that `gear-ladder-rest.md` §4 copies).

### Band 23–27 — cloth

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Holy Shroud | 27 | 40 | — | — | 0 | 0 | 0 | 0 | 6 | BoE | e2721/w2721 | M |
| Neck | Crystal Starfire Medallion | 26 | — | — | — | 0 | 0 | 4 | 4 | 3 | BoE | e5003/w5003 | M |
| Shoulder (Cloth) | Batwing Mantle | 27 | 37 | — | — | 0 | 3 | 0 | 10 | 5 | BoP · Blind Hunter Rare Elite 33.3 % | e6697/w6697 | M |
| Back | Cloak of Rot | 26 | 22 | — | — | 0 | 0 | 0 | 7 | 0 | BoE | e4462/w4462 | M |
| Chest (Cloth) | Robes of Arugal | 24 | 46 | — | — | 0 | 3 | 5 | 9 | 10 | BoP · Archmage Arugal Elite 33.3 % | e6324/w6324 | M |
| Wrist (Cloth) | Glowing Magical Bracelets | 26 | 21 | — | — | 3 | 0 | 0 | 8 | 0 | BoE | e13106/w13106 | M |
| Hands (Cloth) | Hotshot Pilot's Gloves | 27 | 31 | — | — | 0 | 3 | 5 | 8 | 5 | BoE | e9491/w9491 | M |
| Waist (Cloth) | Belt of Arugal | 24 | 26 | — | — | 0 | 2 | 0 | 10 | 3 | BoP · Archmage Arugal Elite 33.3 % | e6392/w6392 | M |
| Legs (Cloth) | Leech Pants | 26 | 42 | — | — | 0 | 0 | 1 | 5 | 15 | BoP · Aku'mai Elite 33.3 % | e6910/w6910 | M |
| Feet (Cloth) | Acidic Walkers | 27 | 34 | — | — | 0 | 0 | 0 | 8 | 4 | BoP · Viscous Fallout Elite 33.3 % | e9454/w9454 | M |
| Finger | The Queen's Jewel | 25 | — | — | — | 0 | 0 | 1 | 2 | 8 | BoE | e13094/w13094 | M |
| Finger | Plains Ring | 24 | — | — | — | 0 | 0 | 8 | 3 | 0 | BoE | e2039/w2039 | M |
| **Weapon** (staff) | **Wind Spirit Staff** | 27 | — | **70–106** | **3.30** | 0 | 0 | 3 | 5 | 15 | BoP · Earthcaller Halmgar Rare Elite 50.0 % | e6689/w6689 | M |
| Wand (ranged) | Wand of Eventide | 27 | — | 23–44 | 1.30 | 0 | 0 | 0 | 0 | 0 | BoE | e5214/w5214 | M |

**Weapon — Wind Spirit Staff:** `AutoAttack { Min = 70; Max = 106; Ticks = secTicks 3.3; Range = 1 }` — 26.7 dps.

**Wand — Wand of Eventide:** `AutoAttack { Min = 23; Max = 44; Ticks = secTicks 1.3; Range = 8 }` — 25.8 dps.

**Band armour total: 299.** The healer set is Int/Spi-first: 15 Spirit on the legs, 15 Spirit + 5 Int on the
staff, and only two pure-Sta pieces. 299 armour is the lowest in the file — that is the cloth tax, and it is
what makes the priest the party member who cannot afford a stray Cathedral pull.

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages.

---
### Band 28–32 — cloth

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Electromagnetic Gigaflux Reactivator | 32 | 44 | — | — | 0 | 0 | 0 | 15 | 12 | BoP · Mekgineer Thermaplugg Elite 25.0 % | e9492/w9492 | M |
| Neck | Kaleidoscope Chain | 30 | — | — | — | 4 | 4 | 4 | 4 | 4 | BoE | e13084/w13084 | M |
| Shoulder (Cloth) | Nightsky Mantle | 30 | 35 | — | — | 0 | 0 | 0 | 8 | 5 | BoE | e4718/w4718 | M |
| Back | Long Silken Cloak | 32 | 24 | — | — | 0 | 0 | 0 | 5 | 6 | BoE | e4326/w4326 | M |
| Chest (Cloth) | Beguiler Robes | 29 | 50 | — | — | 0 | 0 | 7 | 12 | 8 | BoE | e7728/w7728 | M |
| Wrist (Cloth) | Nightsky Wristbands | 28 | 20 | — | — | 0 | 0 | 0 | 6 | 3 | BoE | e6407/w6407 | M |
| Hands (Cloth) | Gloves of Old | 29 | 32 | — | — | 0 | 0 | 0 | 5 | 12 | BoE | e9395/w9395 | M |
| Waist (Cloth) | Sutarn's Ring | 32 | 30 | — | — | 0 | 0 | 6 | 10 | 6 | BoE | e13105/w13105 | M |
| Legs (Cloth) | Necromancer Leggings | 30 | 45 | — | — | 0 | 0 | 12 | 11 | 0 | BoE | e2277/w2277 | M |
| Feet (Cloth) | Boots of the Enchanter | 30 | 32 | — | — | 0 | 0 | 0 | 5 | 8 | BoE | e4325/w4325 | M |
| Finger | Voodoo Band | 32 | — | — | — | 0 | 0 | 0 | 7 | 3 | BoE | e1996/w1996 | M |
| Finger | Ogremind Ring | 31 | — | — | — | 0 | 0 | 0 | 7 | 3 | BoE | e1993/w1993 | M |
| **Weapon** (staff) | **Windweaver Staff** | 32 | — | **80–121** | **3.10** | 0 | 0 | 7 | 15 | 0 | BoE | e7757/w7757 | M |
| Wand (ranged) | Necrotic Wand | 30 | — | 32–61 | 1.40 | 0 | 0 | 0 | 0 | 0 | BoP · Azshir the Sleepless Rare Elite 33.3 % | e7708/w7708 | M |

**Weapon — Windweaver Staff:** `AutoAttack { Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 1 }` — 32.4 dps.

**Wand — Necrotic Wand:** `AutoAttack { Min = 32; Max = 61; Ticks = secTicks 1.4; Range = 8 }` — 33.2 dps.

**Band armour total: 312.** +15 Int on the head and +15 Int on the staff is the biggest caster step before
Uldaman. The wand (33.2 dps) out-damages the staff (32.4 dps) in this band, so if the sim can only swing one,
the wand is the higher-dps term.

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages.

---
### Band 33–37 — cloth

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Corpseshroud | 35 | 47 | — | — | 0 | 5 | 0 | 19 | 6 | BoE | e10574/w10574 | M |
| Neck | Glowing Eye of Mordresh | 36 | — | — | — | 0 | 0 | 0 | 5 | 11 | BoP · Mordresh Fire Eye Elite 33.3 % | e10769/w10769 | M |
| Shoulder (Cloth) | Silksand Shoulder Pads | 35 | 40 | — | — | 1 | 0 | 0 | 11 | 3 | BoE | e14423/w14423 | M |
| Back | Blackforge Cape | 37 | 28 | — | — | 0 | 0 | 0 | 8 | 4 | BoE | e6424/w6424 | M |
| Chest (Cloth) | Death's Head Vestment | 35 | 58 | — | — | 0 | 0 | 12 | 8 | 13 | BoE | e10581/w10581 | M |
| Wrist (Cloth) | Mistscape Bracers | 37 | 24 | — | — | 0 | 0 | 3 | 8 | 0 | BoE | e4045/w4045 | M |
| Hands (Cloth) | Silksand Gloves | 35 | 33 | — | — | 0 | 0 | 5 | 0 | 10 | BoE | e14422/w14422 | M |
| Waist (Cloth) | Deathmage Sash | 36 | 33 | — | — | 0 | 0 | 6 | 15 | 0 | BoP · Mordresh Fire Eye Elite 33.3 % | e10771/w10771 | M |
| Legs (Cloth) | Stoneweaver Leggings | 35 | 51 | — | — | 0 | 0 | 9 | 8 | 15 | BoP · Ironaya Elite 33.3 % | e9407/w9407 | M |
| Feet (Cloth) | Thoughtcast Boots | 33 | 38 | — | — | 0 | 0 | 3 | 6 | 13 | BoE | e10578/w10578 | M |
| Finger | Welken Ring | 35 | — | — | — | 0 | 0 | 0 | 5 | 7 | BoE | e5011/w5011 | M |
| Finger | Prismstone Ring | 35 | — | — | — | 0 | 0 | 0 | 9 | 0 | BoE | e5743/w5743 | M |
| **Weapon** (staff) | **Ironshod Bludgeon** | 37 | — | **74–112** | **2.50** | 8 | 0 | 20 | 0 | 0 | BoP · Ironaya Elite 33.3 % | e9408/w9408 | M |
| Wand (ranged) | Jaina's Firestarter | 37 | — | 44–82 | 1.60 | 0 | 0 | 0 | 6 | 3 | BoE | e13064/w13064 | M |

**Weapon — Ironshod Bludgeon:** `AutoAttack { Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 1 }` — 37.2 dps.

**Wand — Jaina's Firestarter:** `AutoAttack { Min = 44; Max = 82; Ticks = secTicks 1.6; Range = 8 }` — 39.4 dps.

**Band armour total: 352.** This is the **healer's worst weapon band**: the only sourced staff at 33–37 is
Ironshod Bludgeon, whose +8 Str/+20 Sta line is a tank's, not a priest's. It is in the table because it is the
damage term and the armour; the healer-favoured alternative is to keep Windweaver Staff (rl 32, +15 Int,
32.4 dps) and accept 4.8 dps less. Jaina's Firestarter (39.4 dps, best wand in the file) beats both, so the
wand is the better single auto-attack for a casting priest. A one-hand option is `Hypnotic Blade`
(rl 34, +8 Int, +9 spell damage, SM Library) plus any off-hand — mages can use it, priests cannot (daggers are
a mage/rogue weapon; priest has maces and staves) — off-hands are not modelled either way.

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages.

---
### Band 38–40 — cloth

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Whitemane's Chapeau | 39 | 52 | — | — | 0 | 0 | 9 | 14 | 14 | BoP · High Inquisitor Whitemane Elite 33.3 % | e7720/w7720 | M |
| Neck | Necklace of Calisea | 38 | — | — | — | 0 | 0 | 8 | 7 | 7 | BoE | e1714/w1714 | M |
| Shoulder (Cloth) | Mistscape Mantle | 38 | 42 | — | — | 0 | 0 | 0 | 11 | 5 | BoE | e4734/w4734 | M |
| Back | Battle Healer's Cloak | 38 | 31 | — | — | 0 | 0 | 8 | 0 | 5 | BoP | e19528/w19528 | M |
| Chest (Cloth) | Robes of the Lich | 39 | 64 | — | — | 0 | 0 | 20 | 10 | 0 | BoP · Amnennar the Coldbringer Elite 20.0 % | e10762/w10762 | M |
| Wrist (Cloth) | Windchaser Cuffs | 38 | 25 | — | — | 0 | 2 | 0 | 9 | 0 | BoE | e14429/w14429 | M |
| Hands (Cloth) | Mistscape Gloves | 38 | 35 | — | — | 0 | 4 | 4 | 9 | 0 | BoE | e6428/w6428 | M |
| Waist (Cloth) | Windchaser Cinch | 38 | 32 | — | — | 0 | 0 | 0 | 8 | 9 | BoE | e14435/w14435 | M |
| Legs (Cloth) | Mistscape Pants | 40 | 52 | — | — | 0 | 0 | 12 | 0 | 12 | BoE | e4046/w4046 | M |
| Feet (Cloth) | Furen's Boots | 39 | 44 | — | — | 0 | 0 | 0 | 7 | 16 | BoE | e13100/w13100 | M |
| Finger | Assault Band | 39 | — | — | — | 0 | 0 | 7 | 0 | 5 | BoE | e13095/w13095 | M |
| Finger | Lorekeeper's Ring | 38 | — | — | — | 0 | 0 | 5 | 0 | 0 | BoP | e19524/w19524 | M |
| **Weapon** (staff) | **Advisor's Gnarled Staff** | 38 | — | **88–133** | **2.90** | 0 | 0 | 14 | 9 | 0 | BoP | e19568/w19568 | M |
| Wand (ranged) | Umbral Wand | 40 | — | 37–70 | 1.50 | 0 | 0 | 0 | 0 | 0 | BoE | e5216/w5216 | M |

**Weapon — Advisor's Gnarled Staff:** `AutoAttack { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 }` — 38.1 dps.

**Wand — Umbral Wand:** `AutoAttack { Min = 37; Max = 70; Ticks = secTicks 1.5; Range = 8 }` — 35.7 dps.

**Band armour total: 377.** Whitemane's Chapeau (+14 Int/+14 Spi, Cathedral 33 %) is the healer's best single
piece in the file, and it drops in the 41 wing this gear is built for. The staff also restores 6 mana per
5 s. **Wand goes backwards in this band**: Umbral Wand (35.7 dps) is *below* Jaina's Firestarter (39.4 dps,
rl 37), so a priest who owns the Firestarter should keep it — the same trap `gear-ladder-rest.md` flags for
the warlock. One-hand mace option: `Hand of Righteousness` (rl 39, 1H mace, 56–105 @ 2.70, SM Cathedral) was
`M` in `warrior-rogue-ladder.md` §W2 but was **not** re-verified this pass, so it is a note, not a row.

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages.

---

## 4. Druid — leather, healer

Armour class: **leather only** (no mail, no plate).
Weapon: **staves, one- and two-hand maces, daggers, fists**. Druid **cannot use swords or axes** — so
`Ravager` (2H axe, 37.3 dps, the shaman's 33–37 pick), `Pendulum of Doom` (2H axe) and `Strike of the Hydra`
(2H sword) are all legally out for this class. `The Pacifier` (2H mace) *is* druid-legal but carries +18 Str
and no Int, so it loses to the staff below for a healer.

### Band 23–27 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Leather) | Emblazoned Hat | 26 | 77 | — | — | 0 | 0 | 7 | 6 | 6 | BoE | e4048/w4048 | M |
| Neck | Crystal Starfire Medallion | 26 | — | — | — | 0 | 0 | 4 | 4 | 3 | BoE | e5003/w5003 | M |
| Shoulder (Leather) | Watchman Pauldrons | 27 | 80 | — | — | 3 | 0 | 11 | 0 | 4 | BoE | e7727/w7727 | M |
| Back | Cloak of Rot | 26 | 22 | — | — | 0 | 0 | 0 | 7 | 0 | BoE | e4462/w4462 | M |
| Chest (Leather) | Emblazoned Chestpiece | 27 | 97 | — | — | 0 | 7 | 6 | 0 | 6 | BoE | e6396/w6396 | M |
| Wrist (Leather) | Barbaric Bracers | 27 | 47 | — | — | 0 | 4 | 6 | 0 | 4 | BoE | e18948/w18948 | M |
| Hands (Leather) | Dokebi Gloves | 27 | 60 | — | — | 0 | 0 | 6 | 6 | 0 | BoE | e14583/w14583 | M |
| Waist (Leather) | Moss Cinch | 26 | 59 | — | — | 0 | 0 | 5 | 11 | 0 | BoP · Aku'mai Elite 33.3 % | e6911/w6911 | M |
| Legs (Leather) | Guardian Pants | 27 | 85 | — | — | 0 | 0 | 0 | 8 | 8 | BoE | e5962/w5962 | M |
| Feet (Leather) | Harbinger Boots | 25 | 71 | — | — | 2 | 0 | 11 | 0 | 3 | BoE | e7754/w7754 | M |
| Finger | Plains Ring | 24 | — | — | — | 0 | 0 | 8 | 3 | 0 | BoE | e2039/w2039 | M |
| Finger | The Queen's Jewel | 25 | — | — | — | 0 | 0 | 1 | 2 | 8 | BoE | e13094/w13094 | M |
| **Weapon** (staff) | **Wind Spirit Staff** | 27 | — | **70–106** | **3.30** | 0 | 0 | 3 | 5 | 15 | BoP · Earthcaller Halmgar Rare Elite 50.0 % | e6689/w6689 | M |

**Weapon — Wind Spirit Staff:** `AutoAttack { Min = 70; Max = 106; Ticks = secTicks 3.3; Range = 1 }` — 26.7 dps.

**Band armour total: 598.** This is the shaman's 23–27 leather set, which is already Int/Spi-first and so is
the right pool for a healer druid; armour is ~2× the priest's. Weapon: Wind Spirit Staff (+15 Spi) — druid-legal
on the staff line, and the reason a druid does not need the `The Pacifier` line that the shaman can use.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---
### Band 28–32 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Leather) | Enduring Cap | 28 | 88 | — | — | 0 | 0 | 0 | 16 | 7 | BoE | e3020/w3020 | M |
| Neck | Kaleidoscope Chain | 30 | — | — | — | 4 | 4 | 4 | 4 | 4 | BoE | e13084/w13084 | M |
| Shoulder (Leather) | Barbaric Shoulders | 30 | 76 | — | — | 0 | 5 | 6 | 0 | 5 | BoE | e5964/w5964 | M |
| Back | Glyphed Cloak | 32 | 24 | — | — | 0 | 0 | 2 | 7 | 0 | BoE | e4732/w4732 | M |
| Chest (Leather) | Spirewind Fetter | 30 | 112 | — | — | 7 | 0 | 11 | 12 | 0 | BoE | e9406/w9406 | M |
| Wrist (Leather) | Glyphed Bracers | 32 | 46 | — | — | 0 | 6 | 4 | 0 | 0 | BoE | e4059/w4059 | M |
| Hands (Leather) | Ebon Vise | 30 | 70 | — | — | 4 | 6 | 8 | 0 | 0 | BoP · Fallen Champion Rare Elite 33.3 % | e7690/w7690 | M |
| Waist (Leather) | Insignia Belt | 29 | 56 | — | — | 0 | 0 | 7 | 6 | 0 | BoE | e6409/w6409 | M |
| Legs (Leather) | Barbaric Leggings | 29 | 87 | — | — | 0 | 7 | 7 | 0 | 7 | BoE | e5963/w5963 | M |
| Feet (Leather) | Briar Tredders | 31 | 78 | — | — | 0 | 5 | 0 | 9 | 8 | BoE | e10582/w10582 | M |
| Finger | Voodoo Band | 32 | — | — | — | 0 | 0 | 0 | 7 | 3 | BoE | e1996/w1996 | M |
| Finger | Ogremind Ring | 31 | — | — | — | 0 | 0 | 0 | 7 | 3 | BoE | e1993/w1993 | M |
| **Weapon** (staff) | **Windweaver Staff** | 32 | — | **80–121** | **3.10** | 0 | 0 | 7 | 15 | 0 | BoE | e7757/w7757 | M |

**Weapon — Windweaver Staff:** `AutoAttack { Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 1 }` — 32.4 dps.

**Band armour total: 637.** The best healer stat line in the leather pool before Uldaman: 16 Int on the head,
12 Int/11 Sta on the chest, 9 Int/8 Spi on the feet. The staff's Equip is **+14 Arcane damage only** — useful
to a mage, wasted on a druid, so a druid takes this staff for the +15 Int and the 32.4 dps, nothing more.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---
### Band 33–37 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Leather) | Comfortable Leather Hat | 35 | 90 | — | — | 0 | 0 | 11 | 0 | 10 | BoE | e8174/w8174 | M |
| Neck | Gazlowe's Charm | 36 | — | — | — | 4 | 0 | 11 | 0 | 3 | BoE | e13088/w13088 | M |
| Shoulder (Leather) | Fleshhide Shoulders | 37 | 95 | — | — | 5 | 6 | 15 | 0 | 0 | BoP · Glutton Elite 50.0 % | e10774/w10774 | M |
| Back | Windchaser Cloak | 37 | 28 | — | — | 0 | 0 | 4 | 6 | 4 | BoE | e14430/w14430 | M |
| Chest (Leather) | Spirewind Fetter | 30 | 112 | — | — | 7 | 0 | 11 | 12 | 0 | BoE | e9406/w9406 | M |
| Wrist (Leather) | Warden's Wristbands | 35 | 48 | — | — | 0 | 3 | 0 | 8 | 0 | BoE | e14600/w14600 | M |
| Hands (Leather) | Warden's Gloves | 36 | 70 | — | — | 0 | 0 | 0 | 4 | 11 | BoE | e14606/w14606 | M |
| Waist (Leather) | Ogron's Sash | 37 | 71 | — | — | 9 | 9 | 9 | 0 | 0 | BoE | e13117/w13117 | M |
| Legs (Leather) | Glyphed Leggings | 35 | 96 | — | — | 0 | 0 | 11 | 10 | 0 | BoE | e4060/w4060 | M |
| Feet (Leather) | Warden's Footpads | 37 | 79 | — | — | 3 | 0 | 0 | 7 | 9 | BoE | e14599/w14599 | M |
| Finger | Prismstone Ring | 35 | — | — | — | 0 | 0 | 0 | 9 | 0 | BoE | e5743/w5743 | M |
| Finger | Welken Ring | 35 | — | — | — | 0 | 0 | 0 | 5 | 7 | BoE | e5011/w5011 | M |
| **Weapon** (staff) | **Ironshod Bludgeon** | 37 | — | **74–112** | **2.50** | 8 | 0 | 20 | 0 | 0 | BoP · Ironaya Elite 33.3 % | e9408/w9408 | M |

**Weapon — Ironshod Bludgeon:** `AutoAttack { Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 1 }` — 37.2 dps.

**Band armour total: 689.** One deliberate divergence from the shaman's 33–37 set: the shaman table lists
`Hawkeye's Tunic` (35, 110 armour, +12 Agi/+8 Sta) because a shaman's shocks scale with Agi, but a healer
druid keeps `Spirewind Fetter` (30, **112 armour**, +11 Sta/+12 Int) — more armour *and* the caster line, so the
row is carried, not replaced. Weapon: same healer's dilemma as the priest — Ironshod Bludgeon is the only
sourced 33–37 staff and its stats are a tank's. Keep `Windweaver Staff` (32, +15 Int) if the +8 Str/+20 Sta
line is worthless to the build; the dps difference is 4.8.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---
### Band 38–40 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Leather) | Imperial Leather Helm | 38 | 95 | — | — | 0 | 0 | 0 | 12 | 11 | BoE | e6433/w6433 | M |
| Neck | Necklace of Calisea | 38 | — | — | — | 0 | 0 | 8 | 7 | 7 | BoE | e1714/w1714 | M |
| Shoulder (Leather) | Imperial Leather Spaulders | 39 | 90 | — | — | 9 | 0 | 0 | 0 | 9 | BoE | e4737/w4737 | M |
| Back | Battle Healer's Cloak | 38 | 31 | — | — | 0 | 0 | 8 | 0 | 5 | BoP | e19528/w19528 | M |
| Chest (Leather) | Warden's Wraps | 39 | 120 | — | — | 0 | 12 | 12 | 0 | 0 | BoE | e14601/w14601 | M |
| Wrist (Leather) | Imperial Leather Bracers | 37 | 50 | — | — | 0 | 8 | 4 | 0 | 0 | BoE | e4061/w4061 | M |
| Hands (Leather) | Warden's Gloves | 36 | 70 | — | — | 0 | 0 | 0 | 4 | 11 | BoE | e14606/w14606 | M |
| Waist (Leather) | Ogron's Sash | 37 | 71 | — | — | 9 | 9 | 9 | 0 | 0 | BoE | e13117/w13117 | M |
| Legs (Leather) | Ranger Leggings | 38 | 103 | — | — | 0 | 0 | 0 | 0 | 0 | BoE | e7478/w7478 | M |
| Feet (Leather) | Warden's Footpads | 37 | 79 | — | — | 3 | 0 | 0 | 7 | 9 | BoE | e14599/w14599 | M |
| Finger | Prismstone Ring | 35 | — | — | — | 0 | 0 | 0 | 9 | 0 | BoE | e5743/w5743 | M |
| Finger | Welken Ring | 35 | — | — | — | 0 | 0 | 0 | 5 | 7 | BoE | e5011/w5011 | M |
| **Weapon** (staff) | **Advisor's Gnarled Staff** | 38 | — | **88–133** | **2.90** | 0 | 0 | 14 | 9 | 0 | BoP | e19568/w19568 | M |

**Weapon — Advisor's Gnarled Staff:** `AutoAttack { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 }` — 38.1 dps.

**Band armour total: 709** (up from 689). The in-band leather pool at 38–40 adds exactly three fixed-stat
pieces a healer can use — `Imperial Leather Helm` (+12 Int/+11 Spi), `Imperial Leather Spaulders` (+9 Spi) and
`Warden's Wraps` (120 armour, the highest leather chest below 41) — plus `Ranger Leggings` for armour only.
Everything else in the band is a random-suffix green (`Nocturnal Cap`, `Wolf Rider's Leggings`) or a PvP
`Defiler's`/`Highlander's` piece, both excluded. Weapon: Advisor's Gnarled Staff, druid-legal on the staff line,
+6 mana per 5 s, and the largest staff hit in the file. `Lorekeeper's Staff` (id 19572) is its Alliance-side
twin.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages.

---

## 5. Mage — cloth, DPS

Armour class: **cloth only**.
Weapon: **staves, wands, daggers, one-hand swords, off-hands**. Mage cannot use maces.
Same two-weapon layout as the priest (staff at `Range = 1`, wand at `Range = 8`), because a mage's
auto-attack in the sim can be either. The cloth rows are the same pool as the priest's — cloth is cloth — but
the picks are annotated for a **DPS** rather than a healer: Intellect and spell damage first, Spirit last.

### Band 23–27 — cloth

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Holy Shroud | 27 | 40 | — | — | 0 | 0 | 0 | 0 | 6 | BoE | e2721/w2721 | M |
| Neck | Crystal Starfire Medallion | 26 | — | — | — | 0 | 0 | 4 | 4 | 3 | BoE | e5003/w5003 | M |
| Shoulder (Cloth) | Batwing Mantle | 27 | 37 | — | — | 0 | 3 | 0 | 10 | 5 | BoP · Blind Hunter Rare Elite 33.3 % | e6697/w6697 | M |
| Back | Cloak of Rot | 26 | 22 | — | — | 0 | 0 | 0 | 7 | 0 | BoE | e4462/w4462 | M |
| Chest (Cloth) | Robes of Arugal | 24 | 46 | — | — | 0 | 3 | 5 | 9 | 10 | BoP · Archmage Arugal Elite 33.3 % | e6324/w6324 | M |
| Wrist (Cloth) | Glowing Magical Bracelets | 26 | 21 | — | — | 3 | 0 | 0 | 8 | 0 | BoE | e13106/w13106 | M |
| Hands (Cloth) | Hotshot Pilot's Gloves | 27 | 31 | — | — | 0 | 3 | 5 | 8 | 5 | BoE | e9491/w9491 | M |
| Waist (Cloth) | Belt of Arugal | 24 | 26 | — | — | 0 | 2 | 0 | 10 | 3 | BoP · Archmage Arugal Elite 33.3 % | e6392/w6392 | M |
| Legs (Cloth) | Leech Pants | 26 | 42 | — | — | 0 | 0 | 1 | 5 | 15 | BoP · Aku'mai Elite 33.3 % | e6910/w6910 | M |
| Feet (Cloth) | Acidic Walkers | 27 | 34 | — | — | 0 | 0 | 0 | 8 | 4 | BoP · Viscous Fallout Elite 33.3 % | e9454/w9454 | M |
| Finger | The Queen's Jewel | 25 | — | — | — | 0 | 0 | 1 | 2 | 8 | BoE | e13094/w13094 | M |
| Finger | Plains Ring | 24 | — | — | — | 0 | 0 | 8 | 3 | 0 | BoE | e2039/w2039 | M |
| **Weapon** (staff) | **Wind Spirit Staff** | 27 | — | **70–106** | **3.30** | 0 | 0 | 3 | 5 | 15 | BoP · Earthcaller Halmgar Rare Elite 50.0 % | e6689/w6689 | M |
| Wand (ranged) | Wand of Eventide | 27 | — | 23–44 | 1.30 | 0 | 0 | 0 | 0 | 0 | BoE | e5214/w5214 | M |

**Weapon — Wind Spirit Staff:** `AutoAttack { Min = 70; Max = 106; Ticks = secTicks 3.3; Range = 1 }` — 26.7 dps.

**Wand — Wand of Eventide:** `AutoAttack { Min = 23; Max = 44; Ticks = secTicks 1.3; Range = 8 }` — 25.8 dps.

**Band armour total: 299.** Same pool as the priest and the same caveat: below Uldaman the cloth pool has
**no spell-damage pieces at all** — every caster stat here is Int/Spi. The only spell damage in the band sits on
weapons (Hypnotic Blade's +9 is the first, at rl 34).

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages.

---
### Band 28–32 — cloth

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Electromagnetic Gigaflux Reactivator | 32 | 44 | — | — | 0 | 0 | 0 | 15 | 12 | BoP · Mekgineer Thermaplugg Elite 25.0 % | e9492/w9492 | M |
| Neck | Kaleidoscope Chain | 30 | — | — | — | 4 | 4 | 4 | 4 | 4 | BoE | e13084/w13084 | M |
| Shoulder (Cloth) | Nightsky Mantle | 30 | 35 | — | — | 0 | 0 | 0 | 8 | 5 | BoE | e4718/w4718 | M |
| Back | Long Silken Cloak | 32 | 24 | — | — | 0 | 0 | 0 | 5 | 6 | BoE | e4326/w4326 | M |
| Chest (Cloth) | Beguiler Robes | 29 | 50 | — | — | 0 | 0 | 7 | 12 | 8 | BoE | e7728/w7728 | M |
| Wrist (Cloth) | Nightsky Wristbands | 28 | 20 | — | — | 0 | 0 | 0 | 6 | 3 | BoE | e6407/w6407 | M |
| Hands (Cloth) | Gloves of Old | 29 | 32 | — | — | 0 | 0 | 0 | 5 | 12 | BoE | e9395/w9395 | M |
| Waist (Cloth) | Sutarn's Ring | 32 | 30 | — | — | 0 | 0 | 6 | 10 | 6 | BoE | e13105/w13105 | M |
| Legs (Cloth) | Necromancer Leggings | 30 | 45 | — | — | 0 | 0 | 12 | 11 | 0 | BoE | e2277/w2277 | M |
| Feet (Cloth) | Boots of the Enchanter | 30 | 32 | — | — | 0 | 0 | 0 | 5 | 8 | BoE | e4325/w4325 | M |
| Finger | Voodoo Band | 32 | — | — | — | 0 | 0 | 0 | 7 | 3 | BoE | e1996/w1996 | M |
| Finger | Ogremind Ring | 31 | — | — | — | 0 | 0 | 0 | 7 | 3 | BoE | e1993/w1993 | M |
| **Weapon** (staff) | **Windweaver Staff** | 32 | — | **80–121** | **3.10** | 0 | 0 | 7 | 15 | 0 | BoE | e7757/w7757 | M |
| Wand (ranged) | Necrotic Wand | 30 | — | 32–61 | 1.40 | 0 | 0 | 0 | 0 | 0 | BoP · Azshir the Sleepless Rare Elite 33.3 % | e7708/w7708 | M |

**Weapon — Windweaver Staff:** `AutoAttack { Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 1 }` — 32.4 dps.

**Wand — Necrotic Wand:** `AutoAttack { Min = 32; Max = 61; Ticks = secTicks 1.4; Range = 8 }` — 33.2 dps.

**Band armour total: 312.** Windweaver Staff's **+14 Arcane damage** is the mage's first real spell-damage term
and is why the mage values this staff above the priest/druid reading of the same row. Gigaflux Reactivator
(15 Int/12 Spi, Thermaplugg 25 %) is the strongest head slot in any band for a cloth DPS.

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages.

---
### Band 33–37 — cloth

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Corpseshroud | 35 | 47 | — | — | 0 | 5 | 0 | 19 | 6 | BoE | e10574/w10574 | M |
| Neck | Glowing Eye of Mordresh | 36 | — | — | — | 0 | 0 | 0 | 5 | 11 | BoP · Mordresh Fire Eye Elite 33.3 % | e10769/w10769 | M |
| Shoulder (Cloth) | Silksand Shoulder Pads | 35 | 40 | — | — | 1 | 0 | 0 | 11 | 3 | BoE | e14423/w14423 | M |
| Back | Blackforge Cape | 37 | 28 | — | — | 0 | 0 | 0 | 8 | 4 | BoE | e6424/w6424 | M |
| Chest (Cloth) | Death's Head Vestment | 35 | 58 | — | — | 0 | 0 | 12 | 8 | 13 | BoE | e10581/w10581 | M |
| Wrist (Cloth) | Mistscape Bracers | 37 | 24 | — | — | 0 | 0 | 3 | 8 | 0 | BoE | e4045/w4045 | M |
| Hands (Cloth) | Silksand Gloves | 35 | 33 | — | — | 0 | 0 | 5 | 0 | 10 | BoE | e14422/w14422 | M |
| Waist (Cloth) | Deathmage Sash | 36 | 33 | — | — | 0 | 0 | 6 | 15 | 0 | BoP · Mordresh Fire Eye Elite 33.3 % | e10771/w10771 | M |
| Legs (Cloth) | Stoneweaver Leggings | 35 | 51 | — | — | 0 | 0 | 9 | 8 | 15 | BoP · Ironaya Elite 33.3 % | e9407/w9407 | M |
| Feet (Cloth) | Thoughtcast Boots | 33 | 38 | — | — | 0 | 0 | 3 | 6 | 13 | BoE | e10578/w10578 | M |
| Finger | Welken Ring | 35 | — | — | — | 0 | 0 | 0 | 5 | 7 | BoE | e5011/w5011 | M |
| Finger | Prismstone Ring | 35 | — | — | — | 0 | 0 | 0 | 9 | 0 | BoE | e5743/w5743 | M |
| **Weapon** (staff) | **Ironshod Bludgeon** | 37 | — | **74–112** | **2.50** | 8 | 0 | 20 | 0 | 0 | BoP · Ironaya Elite 33.3 % | e9408/w9408 | M |
| Wand (ranged) | Jaina's Firestarter | 37 | — | 44–82 | 1.60 | 0 | 0 | 0 | 6 | 3 | BoE | e13064/w13064 | M |

**Weapon — Ironshod Bludgeon:** `AutoAttack { Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 1 }` — 37.2 dps.

**Wand — Jaina's Firestarter:** `AutoAttack { Min = 44; Max = 82; Ticks = secTicks 1.6; Range = 8 }` — 39.4 dps.

**Band armour total: 352.** Corpseshroud (19 Int) and Deathmage Sash (15 Int) are the mage's biggest single-stat
pieces; the staff row is again the weak slot for a caster, and the **mage-specific alternative is Hypnotic Blade**
(rl 34, **+9 spell damage**, +8 Int, SM Library, mage-legal dagger) paired with any off-hand — off-hands are not
modelled, so the staff keeps the row. Jaina's Firestarter at 39.4 dps out-damages both.

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages.

---
### Band 38–40 — cloth

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Whitemane's Chapeau | 39 | 52 | — | — | 0 | 0 | 9 | 14 | 14 | BoP · High Inquisitor Whitemane Elite 33.3 % | e7720/w7720 | M |
| Neck | Necklace of Calisea | 38 | — | — | — | 0 | 0 | 8 | 7 | 7 | BoE | e1714/w1714 | M |
| Shoulder (Cloth) | Mistscape Mantle | 38 | 42 | — | — | 0 | 0 | 0 | 11 | 5 | BoE | e4734/w4734 | M |
| Back | Battle Healer's Cloak | 38 | 31 | — | — | 0 | 0 | 8 | 0 | 5 | BoP | e19528/w19528 | M |
| Chest (Cloth) | Robes of the Lich | 39 | 64 | — | — | 0 | 0 | 20 | 10 | 0 | BoP · Amnennar the Coldbringer Elite 20.0 % | e10762/w10762 | M |
| Wrist (Cloth) | Windchaser Cuffs | 38 | 25 | — | — | 0 | 2 | 0 | 9 | 0 | BoE | e14429/w14429 | M |
| Hands (Cloth) | Mistscape Gloves | 38 | 35 | — | — | 0 | 4 | 4 | 9 | 0 | BoE | e6428/w6428 | M |
| Waist (Cloth) | Windchaser Cinch | 38 | 32 | — | — | 0 | 0 | 0 | 8 | 9 | BoE | e14435/w14435 | M |
| Legs (Cloth) | Mistscape Pants | 40 | 52 | — | — | 0 | 0 | 12 | 0 | 12 | BoE | e4046/w4046 | M |
| Feet (Cloth) | Furen's Boots | 39 | 44 | — | — | 0 | 0 | 0 | 7 | 16 | BoE | e13100/w13100 | M |
| Finger | Assault Band | 39 | — | — | — | 0 | 0 | 7 | 0 | 5 | BoE | e13095/w13095 | M |
| Finger | Lorekeeper's Ring | 38 | — | — | — | 0 | 0 | 5 | 0 | 0 | BoP | e19524/w19524 | M |
| **Weapon** (staff) | **Advisor's Gnarled Staff** | 38 | — | **88–133** | **2.90** | 0 | 0 | 14 | 9 | 0 | BoP | e19568/w19568 | M |
| Wand (ranged) | Umbral Wand | 40 | — | 37–70 | 1.50 | 0 | 0 | 0 | 0 | 0 | BoE | e5216/w5216 | M |

**Weapon — Advisor's Gnarled Staff:** `AutoAttack { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 }` — 38.1 dps.

**Wand — Umbral Wand:** `AutoAttack { Min = 37; Max = 70; Ticks = secTicks 1.5; Range = 8 }` — 35.7 dps.

**Band armour total: 377.** Robes of the Lich (+20 Sta/+10 Int) is the highest-armour cloth chest in the file
(64) and comes out of RFD, not SM; Whitemane's Chapeau is the Cathedral drop. Wand regresses (35.7 vs 39.4) —
keep Jaina's Firestarter. If the mage owns Hypnotic Blade, it is still the only spell-damage item in the file
below 41 and remains the off-hand-pairing pick.

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages.

---

## 6. The numbers the simulation consumes

### 6.1 Armour totals per class and band

These are the sums of the nine armour slots only (head, shoulder, back, chest, wrist, hands, waist, legs,
feet). Neck and finger slots contribute no armour in Vanilla and are excluded.

| Class | Armour class | 23–27 | 28–32 | 33–37 | 38–40 |
|---|---|---:|---:|---:|---:|
| Warrior | mail → plate at 40 | **1239** | **1314** | **1454** | **2293** (plate, rl 40 only; **1454** while still in mail at 38–39) |
| Rogue | leather | 591 | 642 | 685 | 707 |
| Priest | cloth | 299 | 312 | 352 | 377 |
| Druid | leather | 598 | 637 | 689 | 709 |
| Mage | cloth | 299 | 312 | 352 | 377 |

Steps of note for calibration: warrior armour almost doubles **at 40** when plate unlocks (1454 → 2293, the
single biggest discontinuity in any ladder), and the cloth total (377) is **16 % of the warrior plate total** —
a Cathedral pull that a warrior survives on armour is a cloth death. Below 40 the leather/mail gap is ~2.1×,
above it ~3.2×.

### 6.2 Weapon ladder

One row per class and band, all `M`, all re-read on both hosts. `Ticks = secTicks <speed>`;
`Range = 1` for melee and staves, `Range = 8` for wands.

| Class | Band | Weapon | Type | Req | Min | Max | Speed | dps | `AutoAttack` |
|---|---|---|---|---:|---:|---:|---:|---:|---|
| Warrior | 23–27 | Strike of the Hydra | 2H sword | 26 | 67 | 102 | 3.30 | 25.6 | `{ Min = 67; Max = 102; Ticks = secTicks 3.3; Range = 1 }` |
| Warrior | 28–32 | The Pacifier | 2H mace | 32 | 104 | 156 | 4.00 | 32.5 | `{ Min = 104; Max = 156; Ticks = secTicks 4.0; Range = 1 }` |
| Warrior | 33–37 | Ravager | 2H axe | 37 | 104 | 157 | 3.50 | 37.3 | `{ Min = 104; Max = 157; Ticks = secTicks 3.5; Range = 1 }` |
| Warrior | 38–40 | Mograine's Might | 2H mace | 39 | 87 | 131 | 2.80 | 38.9 | `{ Min = 87; Max = 131; Ticks = secTicks 2.8; Range = 1 }` |
| Rogue | 23–27 | Meteor Shard | dagger | 24 | 23 | 43 | 1.80 | 18.3 | `{ Min = 23; Max = 43; Ticks = secTicks 1.8; Range = 1 }` |
| Rogue | 28–32 | Swinetusk Shank | dagger | 30 | 24 | 45 | 1.50 | 23.0 | `{ Min = 24; Max = 45; Ticks = secTicks 1.5; Range = 1 }` |
| Rogue | 33–37 | Hypnotic Blade | dagger | 34 | 26 | 49 | 1.40 | 26.8 | `{ Min = 26; Max = 49; Ticks = secTicks 1.4; Range = 1 }` |
| Rogue | 38–40 | Hypnotic Blade (carried) | dagger | 34 | 26 | 49 | 1.40 | 26.8 | `{ Min = 26; Max = 49; Ticks = secTicks 1.4; Range = 1 }` |
| Priest | 23–27 | Wind Spirit Staff | staff | 27 | 70 | 106 | 3.30 | 26.7 | `{ Min = 70; Max = 106; Ticks = secTicks 3.3; Range = 1 }` |
| Priest | 28–32 | Windweaver Staff | staff | 32 | 80 | 121 | 3.10 | 32.4 | `{ Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 1 }` |
| Priest | 33–37 | Ironshod Bludgeon | staff | 37 | 74 | 112 | 2.50 | 37.2 | `{ Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 1 }` |
| Priest | 38–40 | Advisor's Gnarled Staff | staff | 38 | 88 | 133 | 2.90 | 38.1 | `{ Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 }` |
| Druid | 23–27 | Wind Spirit Staff | staff | 27 | 70 | 106 | 3.30 | 26.7 | `{ Min = 70; Max = 106; Ticks = secTicks 3.3; Range = 1 }` |
| Druid | 28–32 | Windweaver Staff | staff | 32 | 80 | 121 | 3.10 | 32.4 | `{ Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 1 }` |
| Druid | 33–37 | Ironshod Bludgeon | staff | 37 | 74 | 112 | 2.50 | 37.2 | `{ Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 1 }` |
| Druid | 38–40 | Advisor's Gnarled Staff | staff | 38 | 88 | 133 | 2.90 | 38.1 | `{ Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 }` |
| Mage | 23–27 | Wind Spirit Staff | staff | 27 | 70 | 106 | 3.30 | 26.7 | `{ Min = 70; Max = 106; Ticks = secTicks 3.3; Range = 1 }` |
| Mage | 28–32 | Windweaver Staff | staff | 32 | 80 | 121 | 3.10 | 32.4 | `{ Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 1 }` |
| Mage | 33–37 | Ironshod Bludgeon | staff | 37 | 74 | 112 | 2.50 | 37.2 | `{ Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 1 }` |
| Mage | 38–40 | Advisor's Gnarled Staff | staff | 38 | 88 | 133 | 2.90 | 38.1 | `{ Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 }` |

Caster wands (Priest and Mage, `Range = 8`), all `M`:

| Band | Wand | Req | Min | Max | Speed | dps | `AutoAttack` |
|---|---|---:|---:|---:|---:|---:|---|
| 23–27 | Wand of Eventide | 27 | 23 | 44 | 1.30 | 25.8 | `{ Min = 23; Max = 44; Ticks = secTicks 1.3; Range = 8 }` |
| 28–32 | Necrotic Wand | 30 | 32 | 61 | 1.40 | 33.2 | `{ Min = 32; Max = 61; Ticks = secTicks 1.4; Range = 8 }` |
| 33–37 | Jaina's Firestarter | 37 | 44 | 82 | 1.60 | 39.4 | `{ Min = 44; Max = 82; Ticks = secTicks 1.6; Range = 8 }` |
| 38–40 | Umbral Wand | 40 | 37 | 70 | 1.50 | 35.7 | `{ Min = 37; Max = 70; Ticks = secTicks 1.5; Range = 8 }` |

Two calibration observations:

1. **The melee curve is steeper than the caster curve in these bands**, but only for Warrior. Warrior weapon
   dps goes 25.6 → 38.9 (+52 %) across 23–40; Rogue goes 18.3 → 26.8 (+47 %); the casters sit flat at 26.7 →
   38.1 (+43 %) because they share one staff line. A party whose damage comes from a warrior will therefore
   *out*-scale one carried by a mage, on identical armour.
2. **The caster wand plateaus and then regresses.** Wand dps 25.8 → 33.2 → 39.4 → 35.7; the 38–40 wand is
   worse than the 33–37 one. If the sim picks the band's wand unconditionally it *loses* 3.7 dps at 40 — the
   band's wand should be `max(band wand, previous band wand)`.

## 7. Missing evidence

Each gap below came from querying the sources, not from leaving a blank.

1. **No reachable rogue weapon in 38–40.** `Sacrificial Kris` (3187, rl 39, BoE, 25–47 @ 1.40, 25.7 dps) is in
   band and obtainable in principle, but its drop table is 43–45 rare elites in Zul'Farrak, Tanaris, the
   Hinterlands and Feralas at 0.64 % — above the Cathedral party level. `Sentinel's Blade` (19549) and
   `Scout's Blade` (19545) are battleground rewards, excluded by the ladder standard. The rogue therefore
   carries `Hypnotic Blade` into 40. Searched: the full Dagger list with `24 ≤ req ≤ 40` on emberveil.
2. **No caster-statted staff in 33–37.** The only sourced 33–37 staff is `Ironshod Bludgeon` (+8 Str/+20 Sta).
   Every alternative on the staff list at rl 33–37 is either a random-suffix green or below the band's dps.
   Priests, druids and mages are told to keep `Windweaver Staff` (32) if the strength line is worthless.
3. **`Hand of Righteousness` (1H mace, rl 39) is not re-verified here.** It appears in
   `warrior-rogue-ladder.md` §W2 as `M` with 56–105 @ 2.70. It was not fetched on either host this pass, so it
   is a note for the priest's one-hand option and not a shipped row.
4. **`Corpsemaker` (2H axe, rl 29) and `The Jackhammer` (rl 40) numbers are quoted from the reference file and
   are `U`.** They are named as warrior alternatives, never as rows.
5. **Random-suffix leather 38–40 has no fixed stat line.** `Nocturnal Cap` (15156, 38, 95), `Wolf Rider's
   Leggings` (15374, 39, 105) and `Ranger Leggings` (7478, 38, 103) all show `<Random enchantment>` on both
   hosts. They are shipped (or noted) for armour only. The affix variance is ±1–3 per stat.
6. **Six of the rogue's twelve 38–40 slots and four of the druid's are carried from 33–37** because no in-band
   fixed-stat leather beats them. This is a real property of the 1.12 leather pool, not an omission: the
   38–40 leather list is 52 items, dominated by random-suffix greens and PvP `Defiler's`/`Highlander's` pieces.
7. **Procs, set bonuses and Equip effects other than raw stats are not modelled.** Ravager's bladestorm,
   Meteor Shard's and Strike of the Hydra's chance-on-hit, the staffs' spell damage and mana regeneration, and
   every suffix are represented only by the stats in the tables.
8. **Shields are not modelled.** `AutoAttack { Min; Max; Ticks; Range }` has no off-hand term, so the warrior
   tank row is a two-hander and `Aegis of the Scarlet Commander` (rl 39) is named but not tabulated.
9. **Not searched at all:** quest-reward chains that give gear (e.g. the RFD/Bonebiter chain), reputation
   rewards, profession specialisations, and world events. Any such row would be a `U` and is deliberately
   absent.
10. **Nothing above rl 40.** No mail, leather or cloth levelling item above rl 40 appeared in either source for
    these classes; bands beyond 40 need a new research pass rather than an interpolation.

## 8. Contradictions

1. **`Meteor Shard` required level: resolved to 24.** `warrior-rogue-ladder.md` §R2 reports a conflict
   ("24 (claimed) / 16 (claimed)"). Both hosts read `Requires Level 24` (item level 29). The old `U` can be
   discarded.
2. **`Hypnotic Blade` slot type: unresolved, and it matters for dual-wield.** emberveil prints `One-Hand`,
   Wowhead prints `Main Hand`. Every stat and the damage/speed agree; only the slot differs. Recorded, not
   resolved — treat it as main-hand-only (the stricter reading).
3. **`Ravager` damage: 104–157 @ 3.50.** `warrior-rogue-ladder.md` §W2 carries `dmg U (87–131 claimed)`;
   both hosts now give 104–157 @ 3.50 (37.3 dps). The old figure is wrong and is superseded here.
4. **`Swinetusk Shank` and `Hypnotic Blade` carry a "SoD Phase 2" badge on the Wowhead Classic tooltip.**
   emberveil (a 1.12 server database) lists both as normal drops with 50 % / 25 % rates from RFK and SM
   Library. The badge suggests a Season of Discovery itemisation touch; the 1.12 stats agree on both hosts, so
   the rows ship, but a server running strict 1.12 itemisation should spot-check these two.
5. **Warrior 38–40 is two different armour totals depending on level.** Plate is wearable at 40, not 39: 1454
   (mail) at 38–39, 2293 (plate) at 40. Both are stated rather than averaging.

## 9. Sources

Primary — one page per row, id in the `Src` column:

- `https://database.emberveil.org/item/<id>` — server-rendered HTML; publishes armour, damage range, speed,
  stats, binding and drop table. Example: `https://database.emberveil.org/item/7714` (Hypnotic Blade, rl 34,
  26–49 @ 1.40, +8 Int/+3 Spi, +9 spell damage, Arcanist Doan 25 %).
- `https://database.emberveil.org/compare?items=<id>,<id>,<id>,<id>` — four-item stat grid used to read a
  batch of candidates in one page (armour, required level, damage, dps, all five stats, effects). Used for
  every weapon group and for the 38–40 leather check.
- `https://database.emberveil.org/items?class=4&subclass=2&min_level=38&max_level=40` — the 38–40 leather
  census (52 items) behind the "random-suffix pool" finding.

Corroboration — one page per row:

- `https://nether.wowhead.com/classic/tooltip/item/<id>` — the host that answers plain fetches. Example:
  `https://nether.wowhead.com/classic/tooltip/item/14601` (Warden's Wraps, rl 39, 120 armour, +12 Agi/+12 Sta).
- `https://www.wowhead.com/classic/item=<id>` — cited only as the human-readable counterpart; not fetched
  (403).

Unavailable during this pass (recorded so the next researcher does not repeat it):

- `https://classicdb.ch/?item=<id>` — did not resolve.
- `https://warcraft.wiki.gg/wiki/<Item>` — HTTP 403.

Related in-repo evidence:

- `.pi/research/levelscale/README.md` — band definition, confidence marks, the level-24 `dress` problem.
- `.pi/research/levelscale/gear-ladder-rest.md` — the standard this file copies: column set, `M` discipline,
  the four classes this file does not repeat, and the per-band weapon tables this file extends to 23–40.
- `.pi/research/levelscale/warrior-rogue-ladder.md` §W2/§R2 — three cells corrected here: Ravager damage,
  Meteor Shard required level, and the 23–32 warrior mail armour gap.
- `.pi/research/levelscale/priest-druid-ladder.md`, `.pi/research/levelscale/mage-ladder.md` — the cloth and
  leather-healer standards these tables follow.
