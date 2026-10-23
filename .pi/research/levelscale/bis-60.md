# Pre-raid best-in-slot at level 60

The top anchor of the gear-scaling curve. `Content.dress` bottoms out at the level-24 best-in-slot set
(`src/WowRoguelike.Core/Content.fs`); this file is the level-60 set the other end of that curve needs,
one per class, with the totals written the same way `dress` computes them so the two ends are
subtractable without interpretation.

**"Pre-raid"** = obtainable at 60 without entering Molten Core, Onyxia's Lair, Blackwing Lair,
Zul'Gurub, Ahn'Qiraj or Naxxramas. Dungeon drops (including Upper Blackrock Spire, Stratholme and
Scholomance), quest rewards, crafted gear, world drops and PvP rewards up to rank 10 are all in scope;
rank 11+ (Marshal's / Warlord's) is not.

**How a total is formed.** `Content.dress` starts from the class+race base attribute row for the level
and adds the worn items. So does every `TOTALS` line here: `armour` is item armour only (no base term,
no shield block value), and `str`/`agi`/`sta`/`int`/`spi` are the level-60 base row **plus** the items.
That is exactly how the level-24 anchors were built — `Content.dress` at 24 for Human Warrior gives
81/54/91/23/38, which is base row `(52,38,49,23,29)` plus the set's `29/16/42/0/9`. Comparing this
file's totals against those anchors is therefore apples to apples; the gear-only subtotal is quoted in
each class note for anyone who wants the items alone.

The level-60 base rows used (index 59 of the arrays in `Content.fs`):

| Class | Race the sim uses | Str | Agi | Sta | Int | Spi |
|---|---|---:|---:|---:|---:|---:|
| Warrior | Human | 120 | 80 | 110 | 30 | 47 |
| Paladin | Human | 105 | 65 | 100 | 70 | 78 |
| Priest | Human | 35 | 40 | 50 | 120 | 131 |
| Druid | Night Elf | 62 | 65 | 69 | 100 | 110 |
| Mage | Gnome | 25 | 38 | 44 | 133 | 120 |
| Warlock | Gnome | 40 | 53 | 64 | 119 | 115 |
| Shaman | Orc | 88 | 52 | 97 | 87 | 103 |
| Hunter | Night Elf | 52 | 130 | 89 | 65 | 70 |
| Rogue | Night Elf | 77 | 135 | 74 | 35 | 50 |

**Confidence** follows this directory's convention (`README.md`): `M` = the number was read on two
independent hosts and they agree, `V` = one host only (or two hosts that disagree on a cell, in which
case the first-listed host's value is tabulated and the disagreement is recorded under "Cell
disagreements"), `U` = estimated. Every row here is `M` or `V`; nothing is `U`, but see "What is weak"
before treating the file as final — `M` means "two hosts printed the same digits", not "I confirmed the
item is obtainable".

**Method.** The candidate pool is a crawl of `database.emberveil.org` (a vanilla 1.12 database that
serves server-rendered HTML) covering 4407 items: every armour slot for all four armour types plus every
weapon class, `ilvl` up to 66 for drops and up to 71 for PvP vendor gear. Each candidate was filtered for
raid zone, raid-adjacent drop (Nexus Crystal, Brilliant Shard, "not in the world"), rank 11+ PvP naming,
and armour type legality, then scored with the weights the sim's own formulas imply:
`sta*3 + str + armour*0.05` for the protection tank, `int*2 + spi*1.5 + sta` for healers,
`int*2 + sta*1.5 + spi*0.3` for the casters, `agi*2 + sta*1.5 + str*0.3` for the agility classes and
`(min+max)/2/speed` for weapons. Slots were filled independently (no set-bonus chasing), then each
chosen item's stats were re-read from a second host
(`nether.wowhead.com/classic/tooltip/item/<id>`) and diffed against the first.

## Warrior — protection tank, pre-raid BiS at 60
| Slot | Item | Armour | Str | Agi | Sta | Int | Spi | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Head | Lieutenant Commander's Lamellar Headguard | 534 | 9 | 8 | 31 | 0 | 0 | V |
| Shoulder | Highlander's Plate Spaulders | 553 | 18 | 17 | 20 | 0 | 0 | M |
| Chest | Darksoul Breastplate | 736 | 0 | 0 | 32 | 0 | 0 | M |
| Wrist | Sergeant Major's Plate Wristguards | 287 | 7 | 0 | 17 | 0 | 0 | M |
| Hands | Knight-Lieutenant's Plate Gauntlets | 429 | 17 | 0 | 17 | 0 | 0 | M |
| Waist | Knight-Captain's Lamellar Cinch | 353 | 9 | 0 | 15 | 10 | 9 | M |
| Legs | Outrider's Plate Legguards | 646 | 28 | 0 | 27 | 0 | 0 | M |
| Feet | Knight-Lieutenant's Plate Greaves | 472 | 10 | 9 | 23 | 0 | 0 | M |
| Neck | Master Sergeant's Insignia | 0 | 0 | 0 | 17 | 0 | 7 | M |
| Back | Redoubt Cloak | 45 | 0 | 0 | 15 | 0 | 0 | M |
| Ring 1 | Signet Ring of the Bronze Dragonflight | 0 | 11 | 0 | 19 | 0 | 0 | M |
| Ring 2 | Signet Ring of the Bronze Dragonflight | 0 | 10 | 0 | 18 | 0 | 0 | M |
| Trinket 1 | Rune of Perfection | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Trinket 2 | Rune of Duty | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Main hand | Black Amnesty | 0 | 0 | 0 | 0 | 0 | 0 | M |
| Off-hand | The Immovable Object | 2468 | 0 | 0 | 15 | 0 | 0 | M |
| Ranged | — | 0 | 0 | 0 | 0 | 0 | 0 | M |

TOTALS Warrior armour=6523 str=239 agi=114 sta=390 int=40 spi=63 weapon=53-100 speed=1.60 ranged=none conf=V

Gear-only subtotal, i.e. what `Content.dress` would add on top of the level-60 base row (120 Str / 80 Agi / 110 Sta / 30 Int / 47 Spi): armour 6523, 119/34/280/10/16. Ignored on nine of the seventeen pieces: +1% hit (Darksoul Breastplate), +1% crit (Outrider's Plate Legguards), +7 defence (Redoubt Cloak), +5 defence on each Signet Ring of the Bronze Dragonflight, +27 shield block value (The Immovable Object — not added to armour either), Hamstring costs 3 less rage (gauntlets), 6 hp/5 s (Rune of Duty), −20 spell resistance to your targets (Rune of Perfection), a threat-drop proc (Black Amnesty). None of that is Strength, Stamina or armour, so the tabulated total understates a real tank by roughly a hit, a crit and 17 defence.

## Paladin — holy healer, pre-raid BiS at 60
| Slot | Item | Armour | Str | Agi | Sta | Int | Spi | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Head | Feathermoon Headdress | 143 | 0 | 0 | 13 | 20 | 21 | M |
| Shoulder | Devout Mantle | 64 | 0 | 0 | 4 | 21 | 9 | M |
| Chest | Dreamwalker Armor | 365 | 0 | 0 | 17 | 20 | 20 | M |
| Wrist | Devout Bracers | 35 | 0 | 0 | 7 | 10 | 10 | M |
| Hands | Mooncloth Gloves | 55 | 0 | 0 | 9 | 16 | 15 | M |
| Waist | Detention Strap | 205 | 0 | 0 | 10 | 17 | 11 | M |
| Legs | Magister's Leggings | 76 | 0 | 0 | 12 | 20 | 21 | M |
| Feet | Mooncloth Boots | 55 | 0 | 0 | 11 | 14 | 13 | M |
| Neck | Amulet of the Redeemed | 0 | 0 | 0 | 9 | 9 | 8 | M |
| Back | Sergeant Major's Cape | 43 | 8 | 8 | 8 | 8 | 8 | M |
| Ring 1 | Band of the Hierophant | 0 | 0 | 0 | 7 | 10 | 11 | M |
| Ring 2 | Royal Seal of Alexis | 0 | 0 | 0 | 10 | 10 | 8 | M |
| Trinket 1 | Rune of Perfection | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Trinket 2 | Rune of Duty | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Main hand | Changuk Smasher | 0 | 0 | 0 | 6 | 6 | 5 | M |
| Off-hand | Garrett Family Crest | 2121 | 0 | 5 | 0 | 4 | 17 | M |
| Ranged | — | 0 | 0 | 0 | 0 | 0 | 0 | M |

TOTALS Paladin armour=3162 str=113 agi=78 sta=237 int=255 spi=255 weapon=44-83 speed=1.90 ranged=none conf=M

Gear-only subtotal: armour 3162, 8/13/137/185/177. Ignored: +15 healing (Amulet of the Redeemed), 6 hp/5 s, −20 spell resistance, and the shield's block value. Garrett Family Crest is 2121 of the 3162 armour — the set is cloth-and-leather everywhere except the off-hand.

## Priest — healer, pre-raid BiS at 60
| Slot | Item | Armour | Str | Agi | Sta | Int | Spi | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Head | Devout Crown | 71 | 0 | 0 | 13 | 24 | 15 | M |
| Shoulder | Devout Mantle | 64 | 0 | 0 | 4 | 21 | 9 | M |
| Chest | Devout Robe | 89 | 0 | 0 | 13 | 24 | 15 | M |
| Wrist | Devout Bracers | 35 | 0 | 0 | 7 | 10 | 10 | M |
| Hands | Mooncloth Gloves | 55 | 0 | 0 | 9 | 16 | 15 | M |
| Waist | Belt of the Archmage | 54 | 0 | 0 | 10 | 25 | 0 | M |
| Legs | Magister's Leggings | 76 | 0 | 0 | 12 | 20 | 21 | M |
| Feet | Mooncloth Boots | 55 | 0 | 0 | 11 | 14 | 13 | M |
| Neck | Amulet of the Redeemed | 0 | 0 | 0 | 9 | 9 | 8 | M |
| Back | Sergeant Major's Cape | 43 | 8 | 8 | 8 | 8 | 8 | M |
| Ring 1 | Band of the Hierophant | 0 | 0 | 0 | 7 | 10 | 11 | M |
| Ring 2 | Royal Seal of Alexis | 0 | 0 | 0 | 10 | 10 | 8 | M |
| Trinket 1 | Rune of Perfection | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Trinket 2 | Rune of Duty | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Main hand | Wand of Eternal Light | 0 | 0 | 0 | 0 | 6 | 0 | V |
| Off-hand | Highborne Star | 0 | 0 | 3 | 6 | 11 | 0 | M |
| Ranged | — | 0 | 0 | 0 | 0 | 0 | 0 | M |

TOTALS Priest armour=542 str=43 agi=51 sta=183 int=328 spi=264 weapon=58-109 speed=1.50 ranged=none conf=V

Gear-only subtotal: armour 542, 8/11/133/208/133. Ignored: 1% spell crit (Belt of the Archmage), +15 healing (Amulet of the Redeemed), 2 mp5 (Wand of Eternal Light), 6 hp/5 s, −20 spell resistance. The main hand is a wand by design — this sim's casters fight with a wand in the main hand and a held item in the off-hand.

## Druid — healer, pre-raid BiS at 60
| Slot | Item | Armour | Str | Agi | Sta | Int | Spi | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Head | Feathermoon Headdress | 143 | 0 | 0 | 13 | 20 | 21 | M |
| Shoulder | Devout Mantle | 64 | 0 | 0 | 4 | 21 | 9 | M |
| Chest | Devout Robe | 89 | 0 | 0 | 13 | 24 | 15 | M |
| Wrist | Devout Bracers | 35 | 0 | 0 | 7 | 10 | 10 | M |
| Hands | Mooncloth Gloves | 55 | 0 | 0 | 9 | 16 | 15 | M |
| Waist | Belt of the Archmage | 54 | 0 | 0 | 10 | 25 | 0 | M |
| Legs | Magister's Leggings | 76 | 0 | 0 | 12 | 20 | 21 | M |
| Feet | Mooncloth Boots | 55 | 0 | 0 | 11 | 14 | 13 | M |
| Neck | Amulet of the Redeemed | 0 | 0 | 0 | 9 | 9 | 8 | M |
| Back | Sergeant Major's Cape | 43 | 8 | 8 | 8 | 8 | 8 | M |
| Ring 1 | Band of the Hierophant | 0 | 0 | 0 | 7 | 10 | 11 | M |
| Ring 2 | Royal Seal of Alexis | 0 | 0 | 0 | 10 | 10 | 8 | M |
| Trinket 1 | Rune of Perfection | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Trinket 2 | Rune of Duty | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Main hand | Staff of Hale Magefire | 0 | 0 | 0 | 0 | 25 | 22 | M |
| Off-hand | — | 0 | 0 | 0 | 0 | 0 | 0 | M |
| Ranged | — | 0 | 0 | 0 | 0 | 0 | 0 | M |

TOTALS Druid armour=614 str=70 agi=73 sta=196 int=312 spi=271 weapon=140-210 speed=3.30 ranged=none conf=M

Gear-only subtotal: armour 614, 8/8/127/212/161. Ignored: 1% spell crit (Belt of the Archmage), +15 healing, 6 hp/5 s, −20 spell resistance. The off-hand row is deliberately empty: the main hand is a two-handed staff, so a held item there would be an illegal outfit. The best legal one-hand-plus-held pair (Changuk Smasher + Arcane Star) scores 61 against the staff's 83 on Int/Spirit/Stamina, so the staff wins on the stats this sim reads.

## Mage — DPS, pre-raid BiS at 60
| Slot | Item | Armour | Str | Agi | Sta | Int | Spi | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Head | Magister's Crown | 71 | 0 | 0 | 11 | 30 | 5 | M |
| Shoulder | Highlander's Epaulets | 185 | 0 | 0 | 18 | 17 | 0 | M |
| Chest | Magister's Robes | 89 | 0 | 0 | 9 | 31 | 8 | M |
| Wrist | Magiskull Cuffs | 38 | 0 | 0 | 7 | 17 | 0 | M |
| Hands | Mooncloth Gloves | 55 | 0 | 0 | 9 | 16 | 15 | M |
| Waist | Belt of the Archmage | 54 | 0 | 0 | 10 | 25 | 0 | M |
| Legs | Outrider's Silk Leggings | 188 | 0 | 0 | 23 | 19 | 10 | M |
| Feet | Knight-Lieutenant's Satin Walkers | 64 | 0 | 0 | 17 | 15 | 0 | M |
| Neck | Horizon Choker | 0 | 0 | 0 | 4 | 14 | 4 | M |
| Back | Shroud of Arcane Mastery | 43 | 0 | 0 | 10 | 11 | 0 | M |
| Ring 1 | Ring of Infinite Wisdom | 0 | 0 | 0 | 10 | 13 | 0 | M |
| Ring 2 | Ring of Eternal Justice | 0 | 12 | 0 | 11 | 12 | 0 | M |
| Trinket 1 | Rune of Perfection | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Trinket 2 | Rune of Duty | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Main hand | Wand of Eternal Light | 0 | 0 | 0 | 0 | 6 | 0 | V |
| Off-hand | Highborne Star | 0 | 0 | 3 | 6 | 11 | 0 | M |
| Ranged | — | 0 | 0 | 0 | 0 | 0 | 0 | M |

TOTALS Mage armour=787 str=37 agi=41 sta=203 int=370 spi=162 weapon=58-109 speed=1.50 ranged=none conf=V

Gear-only subtotal: armour 787, 12/3/159/237/42. Ignored, and this is the biggest ignored budget in the file: spell damage on the shoulders, the legs, the boots (+14) and Ring of Eternal Justice — roughly 60-70 spell damage across four pieces — plus 1% spell crit, 1% spell hit, 5 mp5 on Ring of Infinite Wisdom, 2 mp5 on the wand, 6 hp/5 s and −20 spell resistance. Two armour rows are suspect: Highlander's Epaulets (185) and Outrider's Silk Leggings (188) are cloth pieces carrying mail-sized armour values on emberveil. If both are host errors, the honest armour total is nearer 560.

## Warlock — DPS, pre-raid BiS at 60
| Slot | Item | Armour | Str | Agi | Sta | Int | Spi | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Head | Magister's Crown | 71 | 0 | 0 | 11 | 30 | 5 | M |
| Shoulder | Highlander's Epaulets | 185 | 0 | 0 | 18 | 17 | 0 | M |
| Chest | Magister's Robes | 89 | 0 | 0 | 9 | 31 | 8 | M |
| Wrist | Magiskull Cuffs | 38 | 0 | 0 | 7 | 17 | 0 | M |
| Hands | Mooncloth Gloves | 55 | 0 | 0 | 9 | 16 | 15 | M |
| Waist | Belt of the Archmage | 54 | 0 | 0 | 10 | 25 | 0 | M |
| Legs | Outrider's Silk Leggings | 188 | 0 | 0 | 23 | 19 | 10 | M |
| Feet | Knight-Lieutenant's Satin Walkers | 64 | 0 | 0 | 17 | 15 | 0 | M |
| Neck | Horizon Choker | 0 | 0 | 0 | 4 | 14 | 4 | M |
| Back | Shroud of Arcane Mastery | 43 | 0 | 0 | 10 | 11 | 0 | M |
| Ring 1 | Ring of Infinite Wisdom | 0 | 0 | 0 | 10 | 13 | 0 | M |
| Ring 2 | Ring of Eternal Justice | 0 | 12 | 0 | 11 | 12 | 0 | M |
| Trinket 1 | Rune of Perfection | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Trinket 2 | Rune of Duty | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Main hand | Wand of Eternal Light | 0 | 0 | 0 | 0 | 6 | 0 | V |
| Off-hand | Highborne Star | 0 | 0 | 3 | 6 | 11 | 0 | M |
| Ranged | — | 0 | 0 | 0 | 0 | 0 | 0 | M |

TOTALS Warlock armour=787 str=52 agi=56 sta=223 int=356 spi=157 weapon=58-109 speed=1.50 ranged=none conf=V

Same set as the Mage (both are cloth casters on the same scoring weights), so the same ignored budget: ~60-70 spell damage, 1% spell crit, 1% spell hit, 5+2 mp5, 6 hp/5 s, −20 spell resistance. Gear-only subtotal: armour 787, 12/3/159/237/42, with the same caveat on the two suspect cloth armour values (nearer 560 if Highlander's Epaulets and Outrider's Silk Leggings are mis-stated).

## Shaman — healer, pre-raid BiS at 60
| Slot | Item | Armour | Str | Agi | Sta | Int | Spi | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Head | Feathermoon Headdress | 143 | 0 | 0 | 13 | 20 | 21 | M |
| Shoulder | Devout Mantle | 64 | 0 | 0 | 4 | 21 | 9 | M |
| Chest | Dreamwalker Armor | 365 | 0 | 0 | 17 | 20 | 20 | M |
| Wrist | Devout Bracers | 35 | 0 | 0 | 7 | 10 | 10 | M |
| Hands | Mooncloth Gloves | 55 | 0 | 0 | 9 | 16 | 15 | M |
| Waist | Detention Strap | 205 | 0 | 0 | 10 | 17 | 11 | M |
| Legs | Magister's Leggings | 76 | 0 | 0 | 12 | 20 | 21 | M |
| Feet | Mooncloth Boots | 55 | 0 | 0 | 11 | 14 | 13 | M |
| Neck | Amulet of the Redeemed | 0 | 0 | 0 | 9 | 9 | 8 | M |
| Back | Shroud of Arcane Mastery | 43 | 0 | 0 | 10 | 11 | 0 | M |
| Ring 1 | Band of the Hierophant | 0 | 0 | 0 | 7 | 10 | 11 | M |
| Ring 2 | Royal Seal of Alexis | 0 | 0 | 0 | 10 | 10 | 8 | M |
| Trinket 1 | Rune of Perfection | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Trinket 2 | Rune of Duty | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Main hand | Changuk Smasher | 0 | 0 | 0 | 6 | 6 | 5 | M |
| Off-hand | Garrett Family Crest | 2121 | 0 | 5 | 0 | 4 | 17 | M |
| Ranged | — | 0 | 0 | 0 | 0 | 0 | 0 | M |

TOTALS Shaman armour=3162 str=88 agi=57 sta=236 int=275 spi=272 weapon=44-83 speed=1.90 ranged=none conf=M

Gear-only subtotal: armour 3162, 0/5/139/188/169. Ignored: 1% spell hit (Shroud of Arcane Mastery), +15 healing (Amulet of the Redeemed), 6 hp/5 s, −20 spell resistance. The 2121-armour shield is two-thirds of the armour total; without it a mail shaman's body is 1041.

## Hunter — DPS, pre-raid BiS at 60
| Slot | Item | Armour | Str | Agi | Sta | Int | Spi | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Head | Shadowcraft Cap | 141 | 13 | 20 | 18 | 0 | 5 | M |
| Shoulder | Highlander's Chain Pauldrons | 312 | 0 | 20 | 18 | 17 | 0 | M |
| Chest | Primal Batskin Jerkin | 181 | 0 | 32 | 6 | 0 | 0 | M |
| Wrist | Forest Stalker's Bracers | 86 | 11 | 19 | 8 | 0 | 0 | M |
| Hands | Knight-Lieutenant's Chain Vices | 242 | 0 | 18 | 16 | 0 | 0 | M |
| Waist | Knight-Captain's Leather Belt | 95 | 9 | 15 | 14 | 0 | 0 | M |
| Legs | Outrider's Leather Pants | 233 | 0 | 28 | 27 | 0 | 0 | M |
| Feet | Knight-Lieutenant's Chain Greaves | 266 | 0 | 20 | 19 | 0 | 0 | M |
| Neck | Amulet of the Darkmoon | 0 | 10 | 19 | 10 | 0 | 0 | M |
| Back | Shifting Cloak | 48 | 0 | 17 | 8 | 0 | 0 | M |
| Ring 1 | Signet of the Unseen Path | 0 | 0 | 19 | 11 | 8 | 0 | M |
| Ring 2 | Signet Ring of the Bronze Dragonflight | 0 | 0 | 19 | 9 | 0 | 0 | M |
| Trinket 1 | Rune of Perfection | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Trinket 2 | Rune of Duty | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Main hand | Black Amnesty | 0 | 0 | 0 | 0 | 0 | 0 | M |
| Off-hand | Scarlet Kris | 0 | 0 | 10 | 10 | 0 | 0 | M |
| Ranged | Core Marksman Rifle | 0 | 0 | 0 | 0 | 0 | 0 | M |

TOTALS Hunter armour=1604 str=95 agi=386 sta=277 int=90 spi=75 weapon=53-100 speed=1.60 ranged=64-120/2.50 conf=M

Gear-only subtotal: armour 1604, 43/256/188/25/5. Ignored: +22 ranged attack power (Core Marksman Rifle), 1% hit (Primal Batskin Jerkin and Signet Ring of the Bronze Dragonflight), 1% crit (Outrider's Leather Pants), 1% dodge (Shifting Cloak), Multi-Shot +4% (Knight-Lieutenant's Chain Vices), 6 hp/5 s, −20 spell resistance. `weapon` is the melee main hand; the bow slot is the separate `ranged` field.

## Rogue — DPS, pre-raid BiS at 60
| Slot | Item | Armour | Str | Agi | Sta | Int | Spi | Conf |
|---|---|---:|---:|---:|---:|---:|---:|---|
| Head | Shadowcraft Cap | 141 | 13 | 20 | 18 | 0 | 5 | M |
| Shoulder | Highlander's Leather Shoulders | 258 | 0 | 18 | 17 | 0 | 0 | M |
| Chest | Primal Batskin Jerkin | 181 | 0 | 32 | 6 | 0 | 0 | M |
| Wrist | Forest Stalker's Bracers | 86 | 11 | 19 | 8 | 0 | 0 | M |
| Hands | Knight-Lieutenant's Dragonhide Gloves | 110 | 12 | 12 | 12 | 0 | 0 | M |
| Waist | Knight-Captain's Leather Belt | 95 | 9 | 15 | 14 | 0 | 0 | M |
| Legs | Outrider's Leather Pants | 233 | 0 | 28 | 27 | 0 | 0 | M |
| Feet | Mongoose Boots | 120 | 0 | 23 | 9 | 0 | 0 | M |
| Neck | Amulet of the Darkmoon | 0 | 10 | 19 | 10 | 0 | 0 | M |
| Back | Shifting Cloak | 48 | 0 | 17 | 8 | 0 | 0 | M |
| Ring 1 | Signet of the Unseen Path | 0 | 0 | 19 | 11 | 8 | 0 | M |
| Ring 2 | Signet Ring of the Bronze Dragonflight | 0 | 0 | 19 | 9 | 0 | 0 | M |
| Trinket 1 | Rune of Perfection | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Trinket 2 | Rune of Duty | 0 | 0 | 0 | 7 | 0 | 0 | M |
| Main hand | Black Amnesty | 0 | 0 | 0 | 0 | 0 | 0 | M |
| Off-hand | Scarlet Kris | 0 | 0 | 10 | 10 | 0 | 0 | M |
| Ranged | — | 0 | 0 | 0 | 0 | 0 | 0 | M |

TOTALS Rogue armour=1272 str=132 agi=386 sta=247 int=43 spi=55 weapon=53-100 speed=1.60 ranged=none conf=M

Gear-only subtotal: armour 1272, 55/251/173/8/5. Ignored: +30 attack power (Highlander's Leather Shoulders), 1% hit (Primal Batskin Jerkin and Signet Ring of the Bronze Dragonflight), 1% crit (Outrider's Leather Pants), 1% dodge (Shifting Cloak), stealth detection (gloves), 6 hp/5 s, −20 spell resistance. Two one-handers, no shield.


## Cross-check against the level-24 anchor

Every class's 60 total beats its 24 total in every stat, so no row here contradicts `Content.dress`
arithmetic. Ratios are in the next section.

| Class | Armour 24 → 60 | Str 24 → 60 | Agi 24 → 60 | Sta 24 → 60 | Int 24 → 60 | Spi 24 → 60 |
|---|---:|---:|---:|---:|---:|---:|
| Warrior | 1269 → 6523 | 81 → 239 | 54 → 114 | 91 → 390 | 23 → 40 | 38 → 63 |
| Paladin | 557 → 3162 | 74 → 113 | 41 → 78 | 85 → 237 | 44 → 255 | 50 → 255 |
| Priest | 253 → 542 | 28 → 43 | 35 → 51 | 73 → 183 | 104 → 328 | 86 → 264 |
| Druid | 238 → 614 | 31 → 70 | 44 → 73 | 53 → 196 | 94 → 312 | 78 → 271 |
| Mage | 282 → 787 | 18 → 37 | 35 → 41 | 56 → 203 | 131 → 370 | 99 → 162 |
| Warlock | 175 → 787 | 23 → 52 | 39 → 56 | 57 → 223 | 82 → 356 | 73 → 157 |
| Shaman | 238 → 3162 | 43 → 88 | 35 → 57 | 64 → 236 | 87 → 275 | 78 → 272 |
| Hunter | 477 → 1604 | 48 → 95 | 132 → 386 | 72 → 277 | 34 → 90 | 39 → 75 |
| Rogue | 221 → 1272 | 46 → 132 | 100 → 386 | 50 → 247 | 25 → 43 | 29 → 55 |

Two things to know when reading that table. Armour at 24 has no base term and neither does armour at 60,
but three of the 24 values here are the ones `Content.dress` actually sums (Priest 253, Mage 282,
Warlock 175) while `.pi/research/levelscale/gear-ladder-rest.md` quotes 236/265/158 for the same three
classes — those three omitted Mindthrust Bracers (17 armour, −5 Sta, +9 Int). The stat totals agree with
the anchor numbers given in the brief either way; only the armour column moves. And the armour ratios are
not comparable across classes: a cloth class at 60 gains ~2.1-2.8x while the three shield classes gain
5.1-13.3x, because a level-60 shield is 2100-2500 armour on its own and a level-24 shield is 483.

## What is weak

1. **Per-item stats are the weak spot, as in every other file in this directory.** Every row was diffed
   against a second host; four cells disagreed and are listed under "Cell disagreements". The rest agree,
   which is what `M` claims and nothing more.
2. **Obtainability was filtered automatically, not read per item.** Raid zones, raid-adjacent drops and
   rank 11+ PvP names were excluded by rule, but a handful of items carry 21xxx/22xxx ids from the
   Ahn'Qiraj and Scourge-invasion patches (Ring of Eternal Justice, Ring of Infinite Wisdom, Amulet of
   the Redeemed, Shroud of Arcane Mastery, Wand of Eternal Light). They are quest/vendor rewards, not
   raid drops, so they pass the rule; on a server pinned to an earlier patch they would not exist, and the
   ring/neck slots would fall back to ilvl 60-63 dungeon loot at roughly −10 Int each.
3. **Two cloth armour values are almost certainly host errors** (Highlander's Epaulets 185,
   Outrider's Silk Leggings 188 — cloth pieces with mail-sized armour). They are tabulated as read and
   flagged in the Mage/Warlock notes rather than corrected; the effect is +223 armour on those two
   classes and nothing else.
4. **The `ilvl <= 66` cut for drops** drops a few ilvl 67-68 world-drop epics. They are world drops, not
   dungeon BiS, so the omission should not move a total by more than a point or two.
5. **Set bonuses are ignored.** Six of the nine sets mix dungeon-set-1 pieces (Devout, Magister's,
   Shadowcraft, Dreamwalker) that have 2/4/6/8-piece bonuses the sim does not model. Those bonuses are
   mostly spell damage, healing or resistances, so they would land in the ignored budget, not in the
   five stats.
6. **Trinkets are the same two items for every class** (Rune of Perfection, Rune of Duty), because no
   pre-raid trinket carries a modelled stat worth more than the +7 Stamina each of those two gives. A
   real player would use a Darkmoon Faire or Argent Dawn trinket and get spell damage or crit, which
   this sim does not read.

## Sources

Primary host, one page per row (`id` in the tables above is not printed, but every item name resolves):

- `https://database.emberveil.org/item/<id>` — server-rendered 1.12 HTML: armour, damage range, speed,
  the five stats, binding, drop table. Example: `https://database.emberveil.org/item/13000` (Staff of
  Hale Magefire, ilvl 62, req 57, 140-210 @ 3.30, +25 Int/+22 Spi).
- `https://database.emberveil.org/items?class=<n>&min_level=<n>&max_level=<n>&page=<n>` — the candidate
  census behind the scoring pass (armour classes 1-4, weapons class 2).

Corroboration host, one page per row:

- `https://nether.wowhead.com/classic/tooltip/item/<id>` — JSON tooltip, the only Wowhead endpoint that
  answers plain fetches. Example: `https://nether.wowhead.com/classic/tooltip/item/13000`.

Unavailable during this pass (recorded so the next researcher does not repeat it):

- `https://www.wowhead.com/classic/item=<id>` — 403 to plain fetches; tooltip endpoint above is the
  workaround.
- `https://classicdb.ch/?item=<id>` — did not resolve.
- `https://warcraft.wiki.gg/wiki/<Item>` — HTTP 403.

### Cell disagreements

The second host's tooltip was parsed and diffed against the first host's row. Four cells disagree; the
first host's value is what the tables tabulate, and each row is `V` for that reason:

| Class | Item | Cell | emberveil (tabulated) | wowhead tooltip |
|---|---|---|---:|---:|
| Warrior | Lieutenant Commander's Lamellar Headguard (16434) | Int | 0 | 6 |
| Priest, Mage, Warlock | Wand of Eternal Light (22254) | damage | 58-109 | no damage line in the tooltip |

A wand with no damage line is a parse gap on the tooltip host, not a real 0-0 weapon: the wand's
58-109 @ 1.50 is what `weapon` reports for the three caster classes.

Related in-repo evidence:

- `.pi/research/levelscale/README.md` — confidence letters, band definitions, the level-24 `dress`
  problem this file is the other end of.
- `.pi/research/levelscale/gear-ladder-tiers.md` §9 — the source-list format copied here, and the
  23-40 band tables whose 40 rung is the last researched rung below this one.
- `src/WowRoguelike.Core/Content.fs` — the level-24 sets, the class+race base rows, `dress`, and the
  `gearRungs = [1; 28; 33; 38; 40]` ladder this file would extend.

## Ratios

`value60 / value24`, armour first then each stat, using the totals above and the level-24 anchors in
`Content.fs`. Armour 24 values are `Content.dress` sums (see the note under the cross-check table for the
three that differ from `gear-ladder-rest.md`).

| Class | Armour | Str | Agi | Sta | Int | Spi |
|---|---:|---:|---:|---:|---:|---:|
| Warrior | 5.14 | 2.95 | 2.11 | 4.29 | 1.74 | 1.66 |
| Paladin | 5.68 | 1.53 | 1.90 | 2.79 | 5.80 | 5.10 |
| Priest | 2.14 | 1.54 | 1.46 | 2.51 | 3.15 | 3.07 |
| Druid | 2.58 | 2.26 | 1.66 | 3.70 | 3.32 | 3.47 |
| Mage | 2.79 | 2.06 | 1.17 | 3.62 | 2.82 | 1.64 |
| Warlock | 4.50 | 2.26 | 1.44 | 3.91 | 4.34 | 2.15 |
| Shaman | 13.29 | 2.05 | 1.63 | 3.69 | 3.16 | 3.49 |
| Hunter | 3.36 | 1.98 | 2.92 | 3.85 | 2.65 | 1.92 |
| Rogue | 5.76 | 2.87 | 3.86 | 4.94 | 1.72 | 1.90 |

Read the armour column with care before fitting a curve through it: the three shield classes jump by
5.1-13.3x while cloth jumps 2.1-4.5x, so a single armour-per-level line cannot fit both. The stat
columns are better behaved — Sta lands in 2.5-4.9x for every class, Int 1.7-5.8x, Agi 1.2-3.9x — but
Agi for the Mage (1.17) and Int for the Warrior (1.74) are near-flat because those classes' 60 sets were
armed for a different stat and simply inherit the base row's growth there.
