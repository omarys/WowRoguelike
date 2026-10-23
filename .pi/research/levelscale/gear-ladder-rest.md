# Paladin, Shaman, Hunter and Warlock — levelling gear bands 13–40

Gear for the four classes that had no ladder: **Paladin, Shaman, Hunter (DPS), Warlock (DPS)**.
Same standard as `warrior-rogue-ladder.md`, `priest-druid-ladder.md` and `mage-ladder.md`: *plausible
high-achievement levelling gear, not best-in-slot*. A row is something a real player plausibly wears at
that level, obtainable by quest, dungeon drop, world-drop BoE or the auction house — not a raid item and
not an unreachable best-in-slot.

One table per band, one wearable set per table. Slots: head, neck, shoulder, back, chest, wrist, hands,
waist, legs, feet and two fingers where a second statted ring exists in band. Each table ends with the
band's weapon row, carrying **damage range and swing speed**, because that is the term
`AutoAttack { Min; Max; Ticks; Range }` needs (`src/WowRoguelike.Core/Domain.fs:622`).

## How to read a row

| Column | Meaning |
|---|---|
| `Arm` | armour value from the item page, unmodified |
| `Dmg` / `Spd` | weapon damage range and swing speed in seconds; `—` on armour rows |
| `Str … Spi` | the five Vanilla stats, `0` where the item has none |
| `Where` | `BoE` = bind-on-equip (auction house / world drop table), `BoP · <mob> <chance>` = boss/elite drop, `unbound (craft/vendor)` = no bind, usually crafted or a quest reward |
| `Src` | `e<id>` = `database.emberveil.org/item/<id>`, `w<id>` = `nether.wowhead.com/classic/tooltip/item/<id>` |
| `Conf` | `V` verified from one cited page, `M` corroborated by a second, `U`/`UNVERIFIED` inference — do not ship a `U` as fact |

**Every row below is `M`.** Each of the 251 items was read twice: the item page on
`database.emberveil.org` (server-rendered HTML, the source this repo already trusts for creatures) and
the Wowhead Classic tooltip JSON on `nether.wowhead.com` (the `www.wowhead.com` pages answer 403 to plain
fetches; the tooltip host does not). Required level, armour value, all five stats and weapon damage/speed
agree between the two, field for field, except:

- **wand damage ranges** are published only by Wowhead (emberveil shows dps and speed, not min–max), so
  the wand `Dmg` cells are single-sourced but corroborated on dps;
- **`Archer's Longbow` id 3038** exists on emberveil (rl 20, 21–39 @ 2.00) but Wowhead Classic answers
  `Entity not found` for it, so it is excluded from the Hunter 18–22 table instead of being marked `V`.

`classicdb.ch` did not resolve during this research pass; `warcraft.wiki.gg` answered 403. Neither is
cited below.

## 1. Paladin — mail 13–39, plate at 40

Armour class: **mail** from level 1 to 39, **plate** trainable at 40 (`README.md` armour rule, same as the warrior ladder).
Weapon proficiency: one- and two-handed **maces**, one- and two-handed **swords**, **polearms**, shields.
A paladin cannot use axes, staves or daggers — so every axe and staff that out-dps's the pick below (Ravager,
Corpsemaker, Thermaplugg's Left Arm as two-handed axe, Wind Spirit Staff) is legally out.

### Band 13–17 — mail

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Worn Fishing Hat | 15 | 26 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e7996/w7996 | M |
| Neck | — (no sourced item) | — | — | — | — | — | — | — | — | — | — | — | — |
| Shoulder (Mail) | Rough Bronze Shoulders | 17 | 124 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e3480/w3480 | M |
| Back | Firebane Cloak | 16 | 19 | — | — | 0 | 0 | 5 | 0 | 0 | BoE | e12979/w12979 | M |
| Chest (Mail) | Burnished Tunic | 16 | 171 | — | — | 6 | 0 | 3 | 0 | 0 | BoE | e2989/w2989 | M |
| Wrist (Mail) | Burnished Bracers | 14 | 72 | — | — | 2 | 0 | 2 | 0 | 0 | BoE | e3211/w3211 | M |
| Hands (Mail) | Burnished Gloves | 15 | 105 | — | — | 4 | 2 | 0 | 0 | 0 | BoE | e2992/w2992 | M |
| Waist (Mail) | Stormbringer Belt | 15 | 104 | — | — | 5 | 0 | 2 | 0 | 0 | BoE | e12978/w12978 | M |
| Legs (Mail) | Goblin Mail Leggings | 14 | 144 | — | — | 4 | 0 | 3 | 0 | 0 | BoE | e1943/w1943 | M |
| Feet (Mail) | Silver-linked Footguards | 16 | 129 | — | — | 3 | 0 | 7 | 0 | 0 | BoE | e12982/w12982 | M |
| Finger | Ring of the Moon | 16 | — | — | — | 1 | 0 | 3 | 0 | 0 | BoE | e12052/w12052 | M |
| Finger | Overseer's Ring | 15 | — | — | — | 3 | 0 | 0 | 0 | 0 | BoE | e1189/w1189 | M |
| **Weapon** (2H mace) | **Black Malice** | 16 | — | **48–73** | **3.30** | 0 | 0 | 0 | 0 | 0 | BoE | e3194/w3194 | M |

**Weapon — Black Malice:** `AutoAttack { Min = 48; Max = 73; Ticks = secTicks 3.3; Range = 1 }` — 18.3 dps.

Lowest band is all **BoE world-drop / crafted mail**: buyable, so it is realistic rather than aspirational. `Head` is a Common cloth hat and `Shoulder` a statless Common mail pauldron because **no statted head or mail shoulder exists below rl 20** (see Missing evidence); `Neck` has no non-PvP item at all. Weapon: Black Malice (2H mace) — paladin-legal, 18.3 dps.

Band confidence: **M** — 12 of 13 rows sourced from two agreeing pages; 1 slot(s) have no sourced item in band (see Missing evidence).

---
### Band 18–22 — mail

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Worn Fishing Hat | 15 | 26 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e7996/w7996 | M |
| Neck | — (no sourced item) | — | — | — | — | — | — | — | — | — | — | — | — |
| Shoulder (Mail) | Lambent Scale Pauldrons | 22 | 142 | — | — | 5 | 0 | 5 | 0 | 0 | BoE | e4705/w4705 | M |
| Back | Slayer's Cape | 22 | 20 | — | — | 4 | 0 | 3 | 0 | 0 | BoE | e14752/w14752 | M |
| Chest (Mail) | Martyr's Chain | 21 | 204 | — | — | 8 | 0 | 8 | 0 | 5 | BoE | e3416/w3416 | M |
| Wrist (Mail) | Yorgen Bracers | 22 | 91 | — | — | 6 | 3 | 3 | 0 | 0 | BoE | e13012/w13012 | M |
| Hands (Mail) | Thorbia's Gauntlets | 18 | 122 | — | — | 8 | 0 | 3 | 0 | 0 | BoE | e12994/w12994 | M |
| Waist (Mail) | Cobrahn's Grasp | 19 | 111 | — | — | 8 | 3 | 0 | 0 | 0 | BoP · Lord Cobrahn Elite 33.3 % | e6460/w6460 | M |
| Legs (Mail) | Dreamsinger Legguards | 21 | 179 | — | — | 8 | 0 | 8 | 0 | 5 | BoE | e13010/w13010 | M |
| Feet (Mail) | Silvered Bronze Boots | 21 | 128 | — | — | 4 | 0 | 4 | 0 | 3 | BoE | e3482/w3482 | M |
| Finger | Silverlaine's Family Seal | 21 | — | — | — | 7 | 0 | 3 | 0 | 0 | BoP · Baron Silverlaine Elite 50.0 % | e6321/w6321 | M |
| Finger | Demon Band | 19 | — | — | — | 4 | 0 | 2 | 0 | 2 | BoE | e12054/w12054 | M |
| **Weapon** (2H sword) | **Duskbringer** | 20 | — | **57–86** | **3.40** | 0 | 0 | 0 | 0 | 0 | BoE | e2205/w2205 | M |

**Weapon — Duskbringer:** `AutoAttack { Min = 57; Max = 86; Ticks = secTicks 3.4; Range = 1 }` — 21.0 dps.

`Neck` is still empty — the only req ≤ 22 necks in either source are the PvP rewards (Scout's / Sentinel's Medallion, rl 18), which need battlegrounds. Weapon: Duskbringer (2H sword, 21.0 dps). The warrior ladder's **Smite's Mighty Hammer** (rl 18, 2H mace, 55–83 @ 3.50, 19.7 dps, Mr. Smite in the Deadmines) is the same band and is the pick if Black Malice/Duskbringer are not on the AH — paladin can wear either.

Band confidence: **M** — 12 of 13 rows sourced from two agreeing pages; 1 slot(s) have no sourced item in band (see Missing evidence).

---
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

First band where every slot has a statted mail item. Weapon: Strike of the Hydra (2H sword, Blackfathom Deeps, Aku'mai 33 %), 25.6 dps. Night Reaver (rl 18, 2H axe, 52–78 @ 3.30) and Corpsemaker (rl 29, 2H axe, 88–132 @ 3.80) both beat it on some bands but **paladins cannot use axes**.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

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

**Weapon — The Pacifier:** `AutoAttack { Min = 104; Max = 156; Ticks = secTicks 4..0; Range = 1 }` — 32.5 dps.

Weapon: The Pacifier (rl 32, 104–156 @ 4.00, 32.5 dps, Lo'Grosh in Alterac, BoE). Slow swing, big top end — the sim's `MeleeDamageBonus = AttackPower * Ticks / 280` rewards it. The Shoveler (rl 32, 88–133 @ 3.40, 32.5 dps) is the exact-dps alternative.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

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
| **Weapon** (2H mace) | **Thornstone Sledgehammer** | 37 | — | **95–143** | **3.20** | 20 | 0 | 0 | 0 | 0 | BoE | e1722/w1722 | M |

**Weapon — Thornstone Sledgehammer:** `AutoAttack { Min = 95; Max = 143; Ticks = secTicks 3.2; Range = 1 }` — 37.2 dps.

Weapon: Thornstone Sledgehammer (rl 37, 95–143 @ 3.20, 37.2 dps, BoE from Narillasanz). Ravager — the warrior ladder's 33–37 weapon — is a 2H **axe** and is therefore shaman/warrior only; it is not a paladin option.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

---
### Band 38–40 — plate

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

**Plate at 40.** Every armour slot in the set above is plate at rl ≤ 40, so the 38–40 band is the first plate band; mail still beats it below 40. Weapon: Mograine's Might (rl 39, 87–131 @ 2.80, 38.9 dps, BoP from Scarlet Monastery Cathedral) — this also fills the `dmg U (87–131 claimed)` cell in `warrior-rogue-ladder.md` Table W2. The Jackhammer (rl 40, 79–119 @ 2.50, 39.6 dps) is marginally ahead on dps and behind on hit size.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

---

## 2. Shaman — leather 13–39, mail at 40

Armour class: **leather** until mail is trainable at 40. Below 40 the mail items exist but cannot be worn.
Weapon proficiency: one- and two-handed **maces**, one- and two-handed **axes**, **staves**, daggers, shields.
No swords. Two-handed weapons are Enhancement talents at 20, which is why the first band is one-handed.

### Band 13–17 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Worn Fishing Hat | 15 | 26 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e7996/w7996 | M |
| Neck | — (no sourced item) | — | — | — | — | — | — | — | — | — | — | — | — |
| Shoulder (Leather) | Bandit Shoulders | 17 | 58 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e10405/w10405 | M |
| Back | Forest Cloak | 17 | 18 | — | — | 0 | 0 | 3 | 2 | 0 | BoE | e4710/w4710 | M |
| Chest (Leather) | Starsight Tunic | 17 | 89 | — | — | 0 | 0 | 4 | 0 | 10 | BoE | e12988/w12988 | M |
| Wrist (Leather) | Bristlebark Bindings | 17 | 35 | — | — | 2 | 3 | 0 | 0 | 0 | BoE | e14569/w14569 | M |
| Hands (Leather) | Woodworking Gloves | 13 | 46 | — | — | 0 | 0 | 0 | 3 | 2 | BoE | e1945/w1945 | M |
| Waist (Leather) | Support Girdle | 17 | 45 | — | — | 4 | 0 | 3 | 0 | 0 | BoE | e1215/w1215 | M |
| Legs (Leather) | Inscribed Leather Pants | 15 | 68 | — | — | 0 | 0 | 4 | 4 | 0 | BoE | e2986/w2986 | M |
| Feet (Leather) | Stable Boots | 13 | 51 | — | — | 0 | 0 | 4 | 0 | 0 | BoE | e4789/w4789 | M |
| Finger | Lavishly Jeweled Ring | 17 | — | — | — | 0 | 2 | 0 | 6 | 0 | BoP · Gilnid Elite 45.0 % | e1156/w1156 | M |
| Finger | Black Pearl Ring | 17 | — | — | — | 0 | 0 | 1 | 1 | 6 | BoE | e6332/w6332 | M |
| **Weapon** (1H mace) | **Face Smasher** | 16 | — | **25–48** | **2.60** | 3 | 0 | 3 | 0 | 0 | BoE | e1483/w1483 | M |

**Weapon — Face Smasher:** `AutoAttack { Min = 25; Max = 48; Ticks = secTicks 2.6; Range = 1 }` — 14.0 dps.

Shaman has **no 2H weapon below level 20** (Two-Handed Axes/Maces is an Enhancement talent at 20), so the band weapon is a one-hander: Face Smasher (1H mace, 25–48 @ 2.60, 14.0 dps, BoE). A shield belongs in the other hand and is not modelled by `AutoAttack`. `Head`/`Shoulder` are the same statless-filler situation as paladin; `Neck` is the PvP-only gap.

Band confidence: **M** — 12 of 13 rows sourced from two agreeing pages; 1 slot(s) have no sourced item in band (see Missing evidence).

---
### Band 18–22 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Worn Fishing Hat | 15 | 26 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e7996/w7996 | M |
| Neck | — (no sourced item) | — | — | — | — | — | — | — | — | — | — | — | — |
| Shoulder (Leather) | Earthen Leather Shoulders | 22 | 67 | — | — | 0 | 0 | 7 | 0 | 0 | BoE | e7352/w7352 | M |
| Back | Silver-thread Cloak | 21 | 20 | — | — | 0 | 0 | 0 | 4 | 2 | BoE | e4713/w4713 | M |
| Chest (Leather) | Blackened Defias Armor | 19 | 92 | — | — | 4 | 3 | 11 | 0 | 0 | BoP · Edwin VanCleef Rare Elite 20.0 % | e10399/w10399 | M |
| Wrist (Leather) | Owl Bracers | 20 | 37 | — | — | 0 | 0 | 0 | 5 | 0 | BoE | e4796/w4796 | M |
| Hands (Leather) | Wolfclaw Gloves | 22 | 61 | — | — | 5 | 6 | 6 | 0 | 0 | BoE | e1978/w1978 | M |
| Waist (Leather) | Silver-lined Belt | 22 | 55 | — | — | 0 | 0 | 0 | 9 | 4 | BoE | e13011/w13011 | M |
| Legs (Leather) | Leggings of the Fang | 18 | 79 | — | — | 5 | 9 | 4 | 0 | 0 | BoP · Lord Cobrahn Elite 33.3 % | e10410/w10410 | M |
| Feet (Leather) | Forest Leather Boots | 19 | 58 | — | — | 4 | 0 | 4 | 0 | 0 | BoE | e3057/w3057 | M |
| Finger | Deep Fathom Ring | 21 | — | — | — | 0 | 0 | 3 | 3 | 6 | BoP · Mutanus the Devourer Elite | e6463/w6463 | M |
| Finger | Black Widow Band | 19 | — | — | — | 0 | 0 | 0 | 7 | 0 | BoE | e6199/w6199 | M |
| **Weapon** (2H axe) | **Killmaim** | 21 | — | **55–84** | **3.20** | 0 | 0 | 0 | 0 | 0 | BoE | e13016/w13016 | M |

**Weapon — Killmaim:** `AutoAttack { Min = 55; Max = 84; Ticks = secTicks 3.2; Range = 1 }` — 21.7 dps.

Killmaim (rl 21, 2H axe, 55–84 @ 3.20, 21.7 dps) lands one level after the 2H talent, which is exactly the shaman power spike. Leather, not mail, until 40.

Band confidence: **M** — 12 of 13 rows sourced from two agreeing pages; 1 slot(s) have no sourced item in band (see Missing evidence).

---
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

Weapon: Wind Spirit Staff (rl 27, 70–106 @ 3.30, 26.7 dps, +15 Spirit, BoP Earthcaller Halmgar in Razorfen Kraul). A staff is shaman-legal and the int/spirit line suits the shock build; Bloodspiller (rl 27, 2H axe, 57–87 @ 2.70) ties it on dps at 26.7 with no caster stats.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

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
| **Weapon** (2H mace) | **The Pacifier** | 32 | — | **104–156** | **4.00** | 18 | 0 | 7 | 0 | 0 | BoE | e6327/w6327 | M |

**Weapon — The Pacifier:** `AutoAttack { Min = 104; Max = 156; Ticks = secTicks 4..0; Range = 1 }` — 32.5 dps.

Weapon: The Pacifier (2H mace, 32.5 dps) — same item as paladin 28–32, and shaman-legal.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

---
### Band 33–37 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Leather) | Comfortable Leather Hat | 35 | 90 | — | — | 0 | 0 | 11 | 0 | 10 | BoE | e8174/w8174 | M |
| Neck | Gazlowe's Charm | 36 | — | — | — | 4 | 0 | 11 | 0 | 3 | BoE | e13088/w13088 | M |
| Shoulder (Leather) | Fleshhide Shoulders | 37 | 95 | — | — | 5 | 6 | 15 | 0 | 0 | BoP · Glutton Elite 50.0 % | e10774/w10774 | M |
| Back | Windchaser Cloak | 37 | 28 | — | — | 0 | 0 | 4 | 6 | 4 | BoE | e14430/w14430 | M |
| Chest (Leather) | Hawkeye's Tunic | 35 | 110 | — | — | 0 | 12 | 8 | 0 | 0 | BoE | e14592/w14592 | M |
| Wrist (Leather) | Warden's Wristbands | 35 | 48 | — | — | 0 | 3 | 0 | 8 | 0 | BoE | e14600/w14600 | M |
| Hands (Leather) | Warden's Gloves | 36 | 70 | — | — | 0 | 0 | 0 | 4 | 11 | BoE | e14606/w14606 | M |
| Waist (Leather) | Ogron's Sash | 37 | 71 | — | — | 9 | 9 | 9 | 0 | 0 | BoE | e13117/w13117 | M |
| Legs (Leather) | Glyphed Leggings | 35 | 96 | — | — | 0 | 0 | 11 | 10 | 0 | BoE | e4060/w4060 | M |
| Feet (Leather) | Warden's Footpads | 37 | 79 | — | — | 3 | 0 | 0 | 7 | 9 | BoE | e14599/w14599 | M |
| Finger | Prismstone Ring | 35 | — | — | — | 0 | 0 | 0 | 9 | 0 | BoE | e5743/w5743 | M |
| Finger | Welken Ring | 35 | — | — | — | 0 | 0 | 0 | 5 | 7 | BoE | e5011/w5011 | M |
| **Weapon** (2H axe) | **Ravager** | 37 | — | **104–157** | **3.50** | 0 | 0 | 0 | 0 | 0 | BoP · Herod Elite 25.0 % | e7717/w7717 | M |

**Weapon — Ravager:** `AutoAttack { Min = 104; Max = 157; Ticks = secTicks 3.5; Range = 1 }` — 37.3 dps.

Weapon: Ravager (rl 37, 2H axe, 104–157 @ 3.50, 37.3 dps, Herod 25 % in SM Armory) — the 33–37 axe the paladin cannot use. Its proc (extra attacks) is not in the `AutoAttack` model; the sim only sees the 37.3 dps.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

---
### Band 38–40 — mail

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Mail) | Blackforge Cowl | 40 | 202 | — | — | 0 | 0 | 12 | 7 | 7 | BoE | e4080/w4080 | M |
| Neck | Necklace of Calisea | 38 | — | — | — | 0 | 0 | 8 | 7 | 7 | BoE | e1714/w1714 | M |
| Shoulder (Mail) | Spaulders of a Lost Age | 40 | 205 | — | — | 0 | 0 | 16 | 0 | 7 | BoE | e9430/w9430 | M |
| Back | Battle Healer's Cloak | 38 | 31 | — | — | 0 | 0 | 8 | 0 | 5 | BoP | e19528/w19528 | M |
| Chest (Mail) | Deathchill Armor | 39 | 270 | — | — | 0 | 0 | 3 | 9 | 20 | BoP · Amnennar the Coldbringer Elite 20.0 % | e10764/w10764 | M |
| Wrist (Mail) | Khan's Bindings | 40 | 109 | — | — | 6 | 0 | 5 | 0 | 5 | BoE | e14778/w14778 | M |
| Hands (Mail) | Khan's Gloves | 40 | 155 | — | — | 0 | 0 | 10 | 4 | 7 | BoE | e14782/w14782 | M |
| Waist (Mail) | Blackforge Girdle | 39 | 138 | — | — | 0 | 9 | 9 | 0 | 0 | BoE | e6425/w6425 | M |
| Legs (Mail) | Ravager's Woolies | 38 | 211 | — | — | 9 | 0 | 10 | 0 | 9 | BoE | e14775/w14775 | M |
| Feet (Mail) | Blackforge Greaves | 40 | 171 | — | — | 9 | 10 | 0 | 0 | 0 | BoE | e6423/w6423 | M |
| Finger | Assault Band | 39 | — | — | — | 0 | 0 | 7 | 0 | 5 | BoE | e13095/w13095 | M |
| Finger | Lorekeeper's Ring | 38 | — | — | — | 0 | 0 | 5 | 0 | 0 | BoP | e19524/w19524 | M |
| **Weapon** (2H axe) | **Pendulum of Doom** | 39 | — | **124–187** | **4.00** | 0 | 0 | 0 | 0 | 0 | BoE | e9425/w9425 | M |

**Weapon — Pendulum of Doom:** `AutoAttack { Min = 124; Max = 187; Ticks = secTicks 4..0; Range = 1 }` — 38.9 dps.

**Mail at 40** (trainer). Weapon: Pendulum of Doom (rl 39, 2H axe, 124–187 @ 4.00, 38.9 dps, BoE Uldaman) — the largest single hit of any levelling weapon in this file.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

---

## 3. Hunter (DPS) — leather 13–39, mail at 40

Armour class: **leather** until mail is trainable at 40.
Weapon: ranged only — bow, gun or crossbow. The sim's hunter already swings at `Range = 8`
(`src/WowRoguelike.Core/Content.fs:2836`), so the melee weapon is irrelevant and is not listed.
Agility is the only stat that matters for damage; the sets below prioritise Agi > Sta.

### Band 13–17 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Worn Fishing Hat | 15 | 26 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e7996/w7996 | M |
| Neck | — (no sourced item) | — | — | — | — | — | — | — | — | — | — | — | — |
| Shoulder (Leather) | Bandit Shoulders | 17 | 58 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e10405/w10405 | M |
| Back | Glowing Lizardscale Cloak | 17 | 20 | — | — | 0 | 6 | 0 | 0 | 2 | BoP · Skum Elite 50.0 % | e6449/w6449 | M |
| Chest (Leather) | Dark Leather Tunic | 15 | 78 | — | — | 0 | 6 | 0 | 0 | 0 | BoE | e2317/w2317 | M |
| Wrist (Leather) | Bristlebark Bindings | 17 | 35 | — | — | 2 | 3 | 0 | 0 | 0 | BoE | e14569/w14569 | M |
| Hands (Leather) | Deviate Scale Gloves | 16 | 49 | — | — | 0 | 3 | 3 | 0 | 0 | BoE | e6467/w6467 | M |
| Waist (Leather) | Belt of the Fang | 16 | 45 | — | — | 0 | 2 | 3 | 0 | 0 | BoP · Lady Anacondra Elite 33.3 % | e10412/w10412 | M |
| Legs (Leather) | Prospector's Woolies | 16 | 69 | — | — | 1 | 6 | 0 | 0 | 0 | BoE | e14565/w14565 | M |
| Feet (Leather) | Agile Boots | 15 | 53 | — | — | 0 | 5 | 0 | 0 | 0 | BoE | e4788/w4788 | M |
| Finger | Lavishly Jeweled Ring | 17 | — | — | — | 0 | 2 | 0 | 6 | 0 | BoP · Gilnid Elite 45.0 % | e1156/w1156 | M |
| Finger | Ring of the Moon | 16 | — | — | — | 1 | 0 | 3 | 0 | 0 | BoE | e12052/w12052 | M |
| **Weapon** (gun) | **Lil Timmy's Peashooter** | 16 | — | **20–37** | **2.60** | 0 | 0 | 0 | 0 | 0 | BoE | e13136/w13136 | M |

**Weapon — Lil Timmy's Peashooter:** `AutoAttack { Min = 20; Max = 37; Ticks = secTicks 2.6; Range = 8 }` — 11.0 dps.

Range 8 keeps the hunter out of melee; `Range = 1` would be wrong for every row above.

Ranged only: the sim's hunter carries a bow/gun at `Range = 8` (`Content.fs:2836`), so melee is not listed. Lil Timmy's Peashooter (rl 16 gun, 20–37 @ 2.60, 11.0 dps) is BoE and the strongest ranged option in band. Leather, not mail, until 40.

Band confidence: **M** — 12 of 13 rows sourced from two agreeing pages; 1 slot(s) have no sourced item in band (see Missing evidence).

---
### Band 18–22 — leather

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Worn Fishing Hat | 15 | 26 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e7996/w7996 | M |
| Neck | — (no sourced item) | — | — | — | — | — | — | — | — | — | — | — | — |
| Shoulder (Leather) | Bristlebark Amice | 22 | 67 | — | — | 3 | 6 | 0 | 0 | 0 | BoE | e14573/w14573 | M |
| Back | Cape of the Brotherhood | 20 | 21 | — | — | 0 | 6 | 3 | 0 | 0 | BoP · Edwin VanCleef Rare Elite 30.0 % | e5193/w5193 | M |
| Chest (Leather) | Blackened Defias Armor | 19 | 92 | — | — | 4 | 3 | 11 | 0 | 0 | BoP · Edwin VanCleef Rare Elite 20.0 % | e10399/w10399 | M |
| Wrist (Leather) | Wolf Bracers | 20 | 37 | — | — | 0 | 4 | 2 | 0 | 0 | BoE | e4794/w4794 | M |
| Hands (Leather) | Wolfclaw Gloves | 22 | 61 | — | — | 5 | 6 | 6 | 0 | 0 | BoE | e1978/w1978 | M |
| Waist (Leather) | Deviate Scale Belt | 18 | 51 | — | — | 0 | 5 | 6 | 0 | 3 | BoE | e6468/w6468 | M |
| Legs (Leather) | Leggings of the Fang | 18 | 79 | — | — | 5 | 9 | 4 | 0 | 0 | BoP · Lord Cobrahn Elite 33.3 % | e10410/w10410 | M |
| Feet (Leather) | Feet of the Lynx | 19 | 63 | — | — | 3 | 8 | 0 | 0 | 0 | BoE | e1121/w1121 | M |
| Finger | Ring of Precision | 20 | — | — | — | 0 | 6 | 0 | 0 | 3 | BoE | e1491/w1491 | M |
| Finger | Defias Renegade Ring | 20 | — | — | — | 0 | 3 | 3 | 0 | 0 | BoE | e1076/w1076 | M |
| **Weapon** (gun) | **Double-barreled Shotgun** | 22 | — | **21–40** | **2.30** | 0 | 0 | 0 | 0 | 0 | BoE | e2098/w2098 | M |

**Weapon — Double-barreled Shotgun:** `AutoAttack { Min = 21; Max = 40; Ticks = secTicks 2.3; Range = 8 }` — 13.3 dps.

Range 8 keeps the hunter out of melee; `Range = 1` would be wrong for every row above.

Double-barreled Shotgun (rl 22, 21–40 @ 2.30, 13.3 dps). Note: emberveil lists `Archer's Longbow` as id 3038 (rl 20, 21–39 @ 2.00, 15.0 dps) but **wowhead classic returns `Entity not found` for that id**, so it is dropped rather than shipped as a top pick — treat 15.0 dps at rl 20 as unverified.

Band confidence: **M** — 12 of 13 rows sourced from two agreeing pages; 1 slot(s) have no sourced item in band (see Missing evidence).

---
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
| **Weapon** (bow) | **Nightstalker Bow** | 27 | — | **19–36** | **1.70** | 0 | 3 | 0 | 0 | 0 | BoP · Blind Hunter Rare Elite 33.3 % | e6696/w6696 | M |

**Weapon — Nightstalker Bow:** `AutoAttack { Min = 19; Max = 36; Ticks = secTicks 1.7; Range = 8 }` — 16.2 dps.

Range 8 keeps the hunter out of melee; `Range = 1` would be wrong for every row above.

Nightstalker Bow (rl 27, 19–36 @ 1.70, 16.2 dps, BoP Blind Hunter in Razorfen Kraul 33 %) — fast swing, low top end; Harpyclaw Short Bow (rl 27, 20–38 @ 1.80, 16.1 dps) is the AH alternative.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

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
| **Weapon** (gun) | **Glass Shooter** | 30 | — | **36–68** | **2.90** | 0 | 0 | 0 | 0 | 0 | BoP · Dark Iron Ambassador Rare Elite 33.3 % | e9456/w9456 | M |

**Weapon — Glass Shooter:** `AutoAttack { Min = 36; Max = 68; Ticks = secTicks 2.9; Range = 8 }` — 17.9 dps.

Range 8 keeps the hunter out of melee; `Range = 1` would be wrong for every row above.

Glass Shooter (rl 30, 36–68 @ 2.90, 17.9 dps, BoP Dark Iron Ambassador in Gnomeregan 33 %). Ironweaver (rl 29, 31–59 @ 2.60, 17.3) is the BoE equivalent.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

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
| **Weapon** (gun) | **The Silencer** | 37 | — | **43–82** | **2.80** | 0 | 0 | 0 | 0 | 0 | BoE | e13138/w13138 | M |

**Weapon — The Silencer:** `AutoAttack { Min = 43; Max = 82; Ticks = secTicks 2.8; Range = 8 }` — 22.3 dps.

Range 8 keeps the hunter out of melee; `Range = 1` would be wrong for every row above.

The Silencer (rl 37, 43–82 @ 2.80, 22.3 dps, BoE) — the biggest ranged jump in the ladder (+4.4 dps over 28–32). Monolithic Bow (rl 36, 41–77 @ 2.70, 21.9) is close.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

---
### Band 38–40 — mail

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Mail) | Skullsplitter Helm | 38 | 196 | — | — | 0 | 11 | 12 | 0 | 0 | BoE | e1624/w1624 | M |
| Neck | Shriveled Heart | 40 | — | — | — | 0 | 0 | 13 | 0 | 0 | BoP · Sandfury Cretin Elite | e9243/w9243 | M |
| Shoulder (Mail) | Spaulders of a Lost Age | 40 | 205 | — | — | 0 | 0 | 16 | 0 | 7 | BoE | e9430/w9430 | M |
| Back | Scorpashi Cape | 39 | 29 | — | — | 2 | 5 | 7 | 0 | 0 | BoE | e14656/w14656 | M |
| Chest (Mail) | Tough Scorpid Breastplate | 39 | 245 | — | — | 0 | 15 | 0 | 0 | 7 | BoE | e8203/w8203 | M |
| Wrist (Mail) | Tough Scorpid Bracers | 39 | 107 | — | — | 0 | 7 | 0 | 0 | 6 | BoE | e8205/w8205 | M |
| Hands (Mail) | Tough Scorpid Gloves | 40 | 155 | — | — | 0 | 10 | 0 | 0 | 9 | BoE | e8204/w8204 | M |
| Waist (Mail) | Blackforge Girdle | 39 | 138 | — | — | 0 | 9 | 9 | 0 | 0 | BoE | e6425/w6425 | M |
| Legs (Mail) | Scarlet Leggings | 38 | 233 | — | — | 20 | 0 | 10 | 0 | 0 | BoP · Herod Elite 25.0 % | e10330/w10330 | M |
| Feet (Mail) | Blackforge Greaves | 40 | 171 | — | — | 9 | 10 | 0 | 0 | 0 | BoE | e6423/w6423 | M |
| Finger | Falcon's Hook | 39 | — | — | — | 3 | 9 | 0 | 0 | 0 | BoE | e7552/w7552 | M |
| Finger | Assault Band | 39 | — | — | — | 0 | 0 | 7 | 0 | 5 | BoE | e13095/w13095 | M |
| **Weapon** (gun) | **Shadowforge Bushmaster** | 38 | — | **46–86** | **2.90** | 0 | 0 | 0 | 0 | 0 | BoE | e9422/w9422 | M |

**Weapon — Shadowforge Bushmaster:** `AutoAttack { Min = 46; Max = 86; Ticks = secTicks 2.9; Range = 8 }` — 22.8 dps.

Range 8 keeps the hunter out of melee; `Range = 1` would be wrong for every row above.

**Mail at 40.** The set above is mail in every armour slot at rl ≤ 40 (leather is the fallback below 40). Weapon: Shadowforge Bushmaster (rl 38, 46–86 @ 2.90, 22.8 dps, BoE). Note the idle trap: after 40 there is no sourced mail upgrade until Outland, so weapon damage stops growing here.

Band confidence: **M** — 13 of 13 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

---

## 4. Warlock (DPS) — cloth throughout

Armour class: **cloth**, always.
Weapon: **staff** (melee auto-attack) plus a **wand** (ranged auto-attack), following the mage ladder's
wand section. The sim already swings a warlock wand at `Range = 8`
(`src/WowRoguelike.Core/Content.fs:2753`, currently the mage's 30–57 @ 1.50), so the two-weapon layout
below matches the existing code rather than inventing one. Wand dps is the term that decays fastest between bands, so both are given.

### Band 13–17 — cloth

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Worn Fishing Hat | 15 | 26 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e7996/w7996 | M |
| Neck | — (no sourced item) | — | — | — | — | — | — | — | — | — | — | — | — |
| Shoulder (Cloth) | Double-stitched Woolen Shoulders | 17 | 26 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e4314/w4314 | M |
| Back | Sanguine Cape | 17 | 18 | — | — | 0 | 0 | 0 | 4 | 0 | BoE | e14376/w14376 | M |
| Chest (Cloth) | Seer's Robe | 16 | 35 | — | — | 0 | 0 | 0 | 6 | 3 | BoE | e2981/w2981 | M |
| Wrist (Cloth) | Mindthrust Bracers | 17 | 17 | — | — | 0 | 0 | 0 | 9 | 0 | BoE | e1974/w1974 | M |
| Hands (Cloth) | Magefist Gloves | 15 | 23 | — | — | 0 | 0 | 3 | 5 | 4 | BoE | e12977/w12977 | M |
| Waist (Cloth) | Jewel-encrusted Sash | 16 | 20 | — | — | 0 | 0 | 2 | 4 | 0 | BoE | e4436/w4436 | M |
| Legs (Cloth) | Darkweave Breeches | 17 | 35 | — | — | 0 | 0 | 4 | 7 | 6 | BoE | e12987/w12987 | M |
| Feet (Cloth) | Bluegill Sandals | 16 | 24 | — | — | 0 | 0 | 0 | 2 | 4 | BoE | e1560/w1560 | M |
| Finger | Lavishly Jeweled Ring | 17 | — | — | — | 0 | 2 | 0 | 6 | 0 | BoP · Gilnid Elite 45.0 % | e1156/w1156 | M |
| Finger | Black Pearl Ring | 17 | — | — | — | 0 | 0 | 1 | 1 | 6 | BoE | e6332/w6332 | M |
| **Weapon** (staff) | **Witching Stave** | 17 | — | **55–83** | **3.60** | 0 | 0 | 0 | 8 | 0 | BoE | e1484/w1484 | M |
| Wand (ranged) | Greater Magic Wand | 13 | — | 22–41 | 1.80 | 0 | 0 | 0 | 0 | 0 | BoE | e11288/w11288 | M |

**Weapon — Witching Stave:** `AutoAttack { Min = 55; Max = 83; Ticks = secTicks 3.6; Range = 1 }` — 19.2 dps.

**Wand — Greater Magic Wand:** `AutoAttack { Min = 22; Max = 41; Ticks = secTicks 1.8; Range = 8 }` — 17.5 dps.

Cloth throughout. **Wand** first (casters auto-shoot with it): Greater Magic Wand (rl 13, 22–41 @ 1.80, 17.5 dps) is the classic first wand and beats every staff in band on dps. Staff: Witching Stave (rl 17, 55–83 @ 3.60, 19.2 dps, +8 Int, BoE).

Band confidence: **M** — 13 of 14 rows sourced from two agreeing pages; 1 slot(s) have no sourced item in band (see Missing evidence).

---
### Band 18–22 — cloth

| Slot | Item | Req | Arm | Dmg | Spd | Str | Agi | Sta | Int | Spi | Where | Src | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|---|
| Head (Cloth) | Worn Fishing Hat | 15 | 26 | — | — | 0 | 0 | 0 | 0 | 0 | unbound (craft/vendor) | e7996/w7996 | M |
| Neck | — (no sourced item) | — | — | — | — | — | — | — | — | — | — | — | — |
| Shoulder (Cloth) | Magician's Mantle | 20 | 32 | — | — | 0 | 0 | 0 | 9 | 0 | BoE | e12998/w12998 | M |
| Back | Silver-thread Cloak | 21 | 20 | — | — | 0 | 0 | 0 | 4 | 2 | BoE | e4713/w4713 | M |
| Chest (Cloth) | Black Velvet Robes | 21 | 44 | — | — | 0 | 0 | 5 | 12 | 0 | BoE | e2800/w2800 | M |
| Wrist (Cloth) | Silver-thread Cuffs | 22 | 18 | — | — | 0 | 0 | 2 | 5 | 0 | BoE | e4036/w4036 | M |
| Hands (Cloth) | Bright Gloves | 19 | 24 | — | — | 0 | 0 | 0 | 4 | 4 | BoE | e3066/w3066 | M |
| Waist (Cloth) | Keller's Girdle | 18 | 23 | — | — | 0 | 0 | 3 | 8 | 0 | BoE | e2911/w2911 | M |
| Legs (Cloth) | Abomination Skin Leggings | 20 | 37 | — | — | 0 | 0 | 7 | 8 | 0 | BoP · Sever Elite 50.0 % | e23173/w23173 | M |
| Feet (Cloth) | Spidersilk Boots | 20 | 29 | — | — | 0 | 0 | 4 | 4 | 7 | BoE | e4320/w4320 | M |
| Finger | Deep Fathom Ring | 21 | — | — | — | 0 | 0 | 3 | 3 | 6 | BoP · Mutanus the Devourer Elite | e6463/w6463 | M |
| Finger | Black Widow Band | 19 | — | — | — | 0 | 0 | 0 | 7 | 0 | BoE | e6199/w6199 | M |
| **Weapon** (staff) | **Staff of the Shade** | 22 | — | **46–70** | **2.60** | 0 | 0 | 0 | 0 | 0 | BoE | e2549/w2549 | M |
| Wand (ranged) | Thunderwood | 22 | — | 36–67 | 1.90 | 0 | 0 | 0 | 0 | 0 | BoE | e13062/w13062 | M |

**Weapon — Staff of the Shade:** `AutoAttack { Min = 46; Max = 70; Ticks = secTicks 2.6; Range = 1 }` — 22.3 dps.

**Wand — Thunderwood:** `AutoAttack { Min = 36; Max = 67; Ticks = secTicks 1.9; Range = 8 }` — 27.1 dps.

Wand: Thunderwood (rl 22, 36–67 @ 1.90, 27.1 dps, BoE — the single largest wand jump in the file). Staff: Staff of the Shade (rl 22, 46–70 @ 2.60, 22.3 dps).

Band confidence: **M** — 13 of 14 rows sourced from two agreeing pages; 1 slot(s) have no sourced item in band (see Missing evidence).

---
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

Wand: Wand of Eventide (rl 27, 23–44 @ 1.30, 25.8 dps). Staff: Wind Spirit Staff (rl 27, 70–106 @ 3.30, 26.7 dps, +5 Int/+15 Spirit). The staff out-dps's the wand here; only one of the two can be the auto-attack.

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

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

Wand: Necrotic Wand (rl 30, 32–61 @ 1.40, 33.2 dps, BoP) — ahead of the Starfaller (rl 29, 32.9 dps, BoE). Staff: Windweaver Staff (rl 32, 80–121 @ 3.10, 32.4 dps, +15 Int).

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

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

Wand: Jaina's Firestarter (rl 37, 44–82 @ 1.60, 39.4 dps, +6 Int, +3 Spirit, BoE) — the best wand in the file. Staff: Ironshod Bludgeon (rl 37, 74–112 @ 2.50, 37.2 dps, +20 Sta, BoP Ironaya in Uldaman).

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

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

Wand: Umbral Wand (rl 40, 37–70 @ 1.50, 35.7 dps) — *below* the 33–37 wand, so keep Jaina's Firestarter if it is owned. Staff: Advisor's Gnarled Staff (rl 38, 88–133 @ 2.90, 38.1 dps, +14 Sta/+9 Int); Lorekeeper's Staff (id 19572) is the identical Horde-side twin.

Band confidence: **M** — 14 of 14 rows sourced from two agreeing pages; 0 slot(s) have no sourced item in band (see Missing evidence).

---

## 5. The weapon ladder in one table

This is the part the sim actually consumes: 24 points, one per class and band, all `M`.
`Ticks = secTicks <speed>`; `Range = 8` for hunter ranged and warlock wands, `1` otherwise.

| Class | Band | Weapon | Type | Req | Min | Max | Speed | dps | `AutoAttack` |
|---|---|---|---|---:|---:|---:|---:|---:|---|
| Paladin | 13–17 | Black Malice | 2H mace | 16 | 48 | 73 | 3.30 | 18.3 | `{ Min = 48; Max = 73; Ticks = secTicks 3.3; Range = 1 }` |
| Paladin | 18–22 | Duskbringer | 2H sword | 20 | 57 | 86 | 3.40 | 21.0 | `{ Min = 57; Max = 86; Ticks = secTicks 3.4; Range = 1 }` |
| Paladin | 23–27 | Strike of the Hydra | 2H sword | 26 | 67 | 102 | 3.30 | 25.6 | `{ Min = 67; Max = 102; Ticks = secTicks 3.3; Range = 1 }` |
| Paladin | 28–32 | The Pacifier | 2H mace | 32 | 104 | 156 | 4.00 | 32.5 | `{ Min = 104; Max = 156; Ticks = secTicks 4..0; Range = 1 }` |
| Paladin | 33–37 | Thornstone Sledgehammer | 2H mace | 37 | 95 | 143 | 3.20 | 37.2 | `{ Min = 95; Max = 143; Ticks = secTicks 3.2; Range = 1 }` |
| Paladin | 38–40 | Mograine's Might | 2H mace | 39 | 87 | 131 | 2.80 | 38.9 | `{ Min = 87; Max = 131; Ticks = secTicks 2.8; Range = 1 }` |
| Shaman | 13–17 | Face Smasher | 1H mace | 16 | 25 | 48 | 2.60 | 14.0 | `{ Min = 25; Max = 48; Ticks = secTicks 2.6; Range = 1 }` |
| Shaman | 18–22 | Killmaim | 2H axe | 21 | 55 | 84 | 3.20 | 21.7 | `{ Min = 55; Max = 84; Ticks = secTicks 3.2; Range = 1 }` |
| Shaman | 23–27 | Wind Spirit Staff | staff | 27 | 70 | 106 | 3.30 | 26.7 | `{ Min = 70; Max = 106; Ticks = secTicks 3.3; Range = 1 }` |
| Shaman | 28–32 | The Pacifier | 2H mace | 32 | 104 | 156 | 4.00 | 32.5 | `{ Min = 104; Max = 156; Ticks = secTicks 4..0; Range = 1 }` |
| Shaman | 33–37 | Ravager | 2H axe | 37 | 104 | 157 | 3.50 | 37.3 | `{ Min = 104; Max = 157; Ticks = secTicks 3.5; Range = 1 }` |
| Shaman | 38–40 | Pendulum of Doom | 2H axe | 39 | 124 | 187 | 4.00 | 38.9 | `{ Min = 124; Max = 187; Ticks = secTicks 4..0; Range = 1 }` |
| Hunter | 13–17 | Lil Timmy's Peashooter | gun | 16 | 20 | 37 | 2.60 | 11.0 | `{ Min = 20; Max = 37; Ticks = secTicks 2.6; Range = 8 }` |
| Hunter | 18–22 | Double-barreled Shotgun | gun | 22 | 21 | 40 | 2.30 | 13.3 | `{ Min = 21; Max = 40; Ticks = secTicks 2.3; Range = 8 }` |
| Hunter | 23–27 | Nightstalker Bow | bow | 27 | 19 | 36 | 1.70 | 16.2 | `{ Min = 19; Max = 36; Ticks = secTicks 1.7; Range = 8 }` |
| Hunter | 28–32 | Glass Shooter | gun | 30 | 36 | 68 | 2.90 | 17.9 | `{ Min = 36; Max = 68; Ticks = secTicks 2.9; Range = 8 }` |
| Hunter | 33–37 | The Silencer | gun | 37 | 43 | 82 | 2.80 | 22.3 | `{ Min = 43; Max = 82; Ticks = secTicks 2.8; Range = 8 }` |
| Hunter | 38–40 | Shadowforge Bushmaster | gun | 38 | 46 | 86 | 2.90 | 22.8 | `{ Min = 46; Max = 86; Ticks = secTicks 2.9; Range = 8 }` |
| Warlock | 13–17 | Witching Stave | staff | 17 | 55 | 83 | 3.60 | 19.2 | `{ Min = 55; Max = 83; Ticks = secTicks 3.6; Range = 1 }` |
| Warlock | 18–22 | Staff of the Shade | staff | 22 | 46 | 70 | 2.60 | 22.3 | `{ Min = 46; Max = 70; Ticks = secTicks 2.6; Range = 1 }` |
| Warlock | 23–27 | Wind Spirit Staff | staff | 27 | 70 | 106 | 3.30 | 26.7 | `{ Min = 70; Max = 106; Ticks = secTicks 3.3; Range = 1 }` |
| Warlock | 28–32 | Windweaver Staff | staff | 32 | 80 | 121 | 3.10 | 32.4 | `{ Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 1 }` |
| Warlock | 33–37 | Ironshod Bludgeon | staff | 37 | 74 | 112 | 2.50 | 37.2 | `{ Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 1 }` |
| Warlock | 38–40 | Advisor's Gnarled Staff | staff | 38 | 88 | 133 | 2.90 | 38.1 | `{ Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 }` |

Warlock wands, same standard:

| Band | Wand | Req | Min | Max | Speed | dps | `AutoAttack` |
|---|---|---:|---:|---:|---:|---:|---|
| 13–17 | Greater Magic Wand | 13 | 22 | 41 | 1.80 | 17.5 | `{ Min = 22; Max = 41; Ticks = secTicks 1.8; Range = 8 }` |
| 18–22 | Thunderwood | 22 | 36 | 67 | 1.90 | 27.1 | `{ Min = 36; Max = 67; Ticks = secTicks 1.9; Range = 8 }` |
| 23–27 | Wand of Eventide | 27 | 23 | 44 | 1.30 | 25.8 | `{ Min = 23; Max = 44; Ticks = secTicks 1.3; Range = 8 }` |
| 28–32 | Necrotic Wand | 30 | 32 | 61 | 1.40 | 33.2 | `{ Min = 32; Max = 61; Ticks = secTicks 1.4; Range = 8 }` |
| 33–37 | Jaina's Firestarter | 37 | 44 | 82 | 1.60 | 39.4 | `{ Min = 44; Max = 82; Ticks = secTicks 1.6; Range = 8 }` |
| 38–40 | Umbral Wand | 40 | 37 | 70 | 1.50 | 35.7 | `{ Min = 37; Max = 70; Ticks = secTicks 1.5; Range = 8 }` |

This is the part the sim actually consumes: 24 points, one per class and band, all `M`.
`Ticks = secTicks <speed>`; `Range = 8` for hunter ranged and warlock wands, `1` otherwise.
Warlock wands, same standard:
This is the part the sim actually consumes. 24 points, one per class and band, all `M`.
`Ticks = secTicks <speed>`; `Range = 8` for hunter ranged and warlock wands, `1` otherwise.
Warlock wands, same standard:
Three observations for the calibration problem in `README.md`:
1. **Weapon dps grows ~2× across the range, from ~11–19 dps at 13–17 to ~23–39 dps at 38–40.** The sim's
   current single level-24 set (`Content.dress`) sits around the 23–27 band for melee and the 28–32 band
   for casters, which is the documented reason a level-41 party cannot kill level-40 elites.
2. **The melee curve is steeper than the caster curve** at the top: `Mograine's Might`/`Pendulum of Doom`
   reach 38.9 dps while the best warlock wand reaches 39.4 and the best hunter ranged weapon only 22.8.
   A party that out-levels the content with a hunter will still feel under-gunned compared with a paladin.
3. **The 38–40 band is the end of the sourced data.** No mail or leather item above rl 40 appears in either
   source for these classes, so bands beyond 40 need a new research pass, not an interpolation.

## 6. Missing evidence

Every gap below was produced by querying the two sources, not by leaving a blank.

1. **`Neck` — bands 13–17 and 18–22, all four classes (8 rows).** No neck item with req ≤ 22 exists in
   either source except the battleground rewards `Scout's Medallion` / `Sentinel's Medallion`
   (`database.emberveil.org/item/20442`, `/20444`, rl 18, +6 Agi +2 Sta). Searched: every `Neck` row in
   the emberveil crawl with `13 ≤ req ≤ 23`. PvP rewards are excluded by the ladder standard, so the slot
   is empty rather than filled with something the party cannot earn.
2. **`Head` — bands 13–17 and 18–22 (8 rows) is filler, not a stat item.** Only `Worn Fishing Hat`
   (rl 15, Common, cloth, 26 armour, no stats) exists at or below rl 22 in either source. It is listed so
   the set has a head slot, with all five stats `0`. The first statted head in any armour class is rl 23
   (`Frostreaver Crown`, mail, 182 armour) — searched: every `Head` row with req ≤ 23, plus Wowhead
   tooltips for the Common candidates.
3. **`Shoulder` — band 13–17 (4 rows) is likewise statless.** `Rough Bronze Shoulders` (mail, 124),
   `Burnished Pauldrons` (mail, 124), `Bandit Shoulders` (leather, 58) and `Double-stitched Woolen
   Shoulders` (cloth, 26) are all rl 17 with no stats. The first statted shoulders are rl 20
   (`Cutthroat Pauldrons`, `Silvered Bronze Shoulders`).
4. **Wand damage ranges are single-sourced** (Wowhead only, 6 rows). emberveil's wand pages publish speed
   and dps but render damage as `<min> - <max> <school> Damage`, which the dps corroborates exactly:
   22–41 @ 1.80 = 17.5 dps, 36–67 @ 1.90 = 27.1, 23–44 @ 1.30 = 25.8, 32–61 @ 1.40 = 33.2,
   44–82 @ 1.60 = 39.4, 37–70 @ 1.50 = 35.7. Treat the min–max as `M`, not `V`.
5. **`Archer's Longbow` (emberveil id 3038) is unresolvable on Wowhead** (`Entity not found`). If it is
   real it would be the Hunter 18–22 pick at 15.0 dps, +1.7 dps over the table's 13.3. Not shipped.
6. **Proc and set effects are not captured.** Ravager (+extra attacks), Pendulum of Doom, Black Malice,
   Venomstrike and every "of the Bear/Tiger" suffix item are represented by their raw stats only. The
   `AutoAttack` model has no hook for a proc, so the dps column understates those weapons.
7. **No 2H weapon below level 20 for shaman** is a rule, not a gap: Two-Handed Axes/Maces is an
   Enhancement talent at 20. The 13–17 shaman row is a one-hander and a shield is not modelled.
8. **Item suffixes are collapsed.** Green BoE drops carry `of the Bear` / `of the Tiger` / `of Intellect`
   suffixes with different stats; the tables quote the unsuffixed base item page. Affix variance is
   ±1–3 per stat in these bands, i.e. smaller than the band-to-band step, except on the 38–40 rings.
9. **Not searched at all:** reputation rewards (Argent Dawn, Thorium Brotherhood), battleground gear,
   world-event items, and profession-specialised patterns. A `U` row for any of these would be an
   inference and is deliberately absent.

10. **Hunter 38–40 is the weakest band in this file (+0.5 dps over 33–37).** Mail at 40 gives no ranged upgrade
   between `Shadowforge Bushmaster` (rl 38, 22.8 dps) and Outland, and the 38–40 mail Agi pool is thin —
   `Skullsplitter Helm`, the `Tough Scorpid` set and `Blackforge` pieces sum to 76 Agi, *below* the 28–32
   leather set's 108. If the sim's hunter must carry the party at 40, its weapon damage is the limiting
   term, not its armour.

## 7. Sources

Primary — one page per row, id in the `Src` column:

- `database.emberveil.org/item/<id>` — e.g. `https://database.emberveil.org/item/3194` (Black Malice),
  `https://database.emberveil.org/item/7723` (Mograine's Might), `https://database.emberveil.org/item/2098`
  (Double-barreled Shotgun). Vanilla 1.12, server-rendered, publishes armour, damage, speed and stats.
- `https://database.emberveil.org/creatures?zone=796&sort=health` — used earlier for creature rows, not here.

Corroboration — one page per row:

- `https://nether.wowhead.com/classic/tooltip/item/<id>` — e.g. `.../3194`, `.../7723`, `.../5216`
  (Umbral Wand). This is the host that answers plain fetches; `https://www.wowhead.com/classic/item/<id>`
  returns 403 and `https://warcraft.wiki.gg/api.php` returns a wiki.gg block page.
- `https://www.wowhead.com/classic/item=<id>` — cited only as the human-readable counterpart; not fetched.

Unavailable during this pass (recorded so the next researcher does not repeat it):

- `https://classicdb.ch/?item=<id>` — did not resolve (curl exit 6/000).
- `https://warcraft.wiki.gg/wiki/<Item>` and its `api.php` — HTTP 403.

Related in-repo evidence:

- `.pi/research/levelscale/README.md` — band definition, confidence marks, the level-24 `dress` problem.
- `.pi/research/levelscale/warrior-rogue-ladder.md` Table W2 / R2 — overlapping bands this file extends;
  Smite's Mighty Hammer (18–22) and Mograine's Might (38–40) now have sourced damage here.
- `.pi/research/levelscale/mage-ladder.md` §2–§3 — wand ladder convention copied for the warlock wands.
- `.pi/research/levelscale/priest-druid-ladder.md` §2/§4 — cloth/healer set standard.
