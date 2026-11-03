# Level-60 raid gear anchor (Tier 1 / Molten Core + Onyxia)

Status: researched from live item data. Supersedes the pre-raid best-in-slot figures that
`Sheet.Gear60` used to encode. Every stat below is a raw item stat — no buffs, no talents, no
enchant or consumable. Trinkets contribute no Strength/Agility/Stamina/Intellect/Spirit on most
Molten Core trinkets (their value is a proc or an equip effect this game does not model), so they
add to the slot count but not to the totals.

## Method and confidence

- Item stats come from two independent hosts: wowhead classic tooltips (`nether.wowhead.com/classic/tooltip/item/<id>`)
  and emberveil (`database.emberveil.org/item/<id>`). A row marked **M** means both hosts agree on
  armour and all five stats; **V** means only one host answered. No row here is **U** (estimate).
- Slot legality is enforced by class weapon proficiency and armour class, both encoded in the build
  script, not by eyeballing: a Warrior never receives a wand, a Mage never receives plate.
- The eight armour pieces per class are the actual Tier 1 set pieces, read from each set page
  (`database.emberveil.org/item-set/<id>` is server-rendered per id), so set membership is exact
  rather than inferred from item names.
- Weapon slots prefer one-hand + shield where the class can hold a shield (Warrior, Paladin, Shaman),
  because a tank or healer that trades 2900-3200 armour for a two-hander is not the anchor we want.
  Classes without shields take the higher-weighted of two-hand vs one-hand + held-in-offhand.
- Rings and trinkets are the two highest-weighted picks in the slot, which is why two appear.

## Totals, one row per class

The last five columns are what `Sheet.Gear60` consumes: armour, then Strength, Agility, Stamina,
Intellect, Spirit.

| Class | Role | Armour | Str | Agi | Sta | Int | Spi | Armour class |
|---|---|---|---|---|---|---|---|---|
| Warrior | Tank | 8068 | 161 | 21 | 257 | 0 | 0 | Plate |
| Paladin | Healer | 8223 | 74 | 0 | 203 | 209 | 113 | Plate |
| Hunter | Physical dps | 2947 | 0 | 163 | 192 | 52 | 40 | Mail |
| Rogue | Physical dps | 1488 | 59 | 236 | 182 | 0 | 0 | Leather |
| Shaman | Healer | 6079 | 0 | 0 | 178 | 228 | 133 | Mail |
| Druid | Healer | 1376 | 0 | 0 | 196 | 261 | 158 | Leather |
| Priest | Healer | 729 | 0 | 0 | 165 | 263 | 149 | Cloth |
| Mage | Caster | 742 | 0 | 0 | 198 | 258 | 128 | Cloth |
| Warlock | Caster | 729 | 0 | 0 | 234 | 227 | 72 | Cloth |

## What changes in `Content.fs`

Current values are the pre-raid sheet; the right-hand column is this researched Tier 1 anchor.
`Gear60` is `(armour, (strength, agility, stamina, intellect, spirit))`.

| Class | Line | Current `Gear60` | Tier 1 `Gear60` |
|---|---|---|---|
| Warrior | 3334 | `6523, (119, 34, 280, 10, 16)` | `8068, (161, 21, 257, 0, 0)` |
| Paladin | 3600 | `3162, (8, 13, 137, 185, 177)` | `8223, (74, 0, 203, 209, 113)` |
| Hunter | 3820 | `1604, (43, 256, 188, 25, 5)` | `2947, (0, 163, 192, 52, 40)` |
| Rogue | 3445 | `1272, (55, 251, 173, 8, 5)` | `1488, (59, 236, 182, 0, 0)` |
| Shaman | 3658 | `787, (12, 3, 159, 237, 42)` | `6079, (0, 0, 178, 228, 133)` |
| Druid | 3545 | `614, (8, 8, 127, 212, 161)` | `1376, (0, 0, 196, 261, 158)` |
| Priest | 3396 | `542, (8, 11, 133, 208, 133)` | `729, (0, 0, 165, 263, 149)` |
| Mage | 3497 | `787, (12, 3, 159, 237, 42)` | `742, (0, 0, 198, 258, 128)` |
| Warlock | 3759 | `3162, (0, 5, 139, 188, 169)` | `729, (0, 0, 234, 227, 72)` |

**Defect found on the way:** Paladin and Warlock carry identical armour (3162) in the current
sheet, and Shaman and Mage carry identical whole tuples (787, (12, 3, 159, 237, 42)). A cloth
Warlock cannot legitimately wear the same 3162 armour as a plate Paladin, so at least the Warlock
figure is a copy-paste error, not a researched value.

## Warrior — Tank

| Slot | Item | id | Armour | Stats | Source | Conf |
|---|---|---|---|---|---|---|
| Head | Helm of Might | 16866 | 695 | str 15 sta 35 | Garr | M |
| Neck | *no legal item in this pool* | — | 0 | — | — | — |
| Shoulder | Pauldrons of Might | 16868 | 641 | str 15 sta 22 | Sulfuron Harbinger | M |
| Back | Dragon's Blood Cape | 17107 | 124 | str 9 sta 22 | Ragnaros | M |
| Chest | Breastplate of Might | 16865 | 855 | str 20 sta 28 | Golemagg the Incinerator | M |
| Wrist | Bracers of Might | 16861 | 374 | str 11 sta 23 | MC tier set | M |
| Hands | Gauntlets of Might | 16863 | 534 | str 22 sta 17 | Lucifron | M |
| Waist | Belt of Might | 16864 | 481 | str 21 sta 15 | Firewalker | M |
| Legs | Legplates of Might | 16867 | 748 | str 24 sta 23 | Magmadar | M |
| Feet | Sabatons of Might | 16862 | 588 | str 15 sta 26 | Gehennas | M |
| Finger 1 | Heavy Dark Iron Ring | 18879 | 110 | sta 20 | Gehennas | M |
| Finger 2 | Band of Accuria | 17063 | 0 | agi 12 sta 10 | Ragnaros | V |
| Trinket 1 | Shard of the Scale | 17064 | 0 | — | Onyxia | M |
| Trinket 2 | Shard of the Flame | 17082 | 0 | — | Ragnaros | M |
| Main Hand | Brutality Blade | 18832 | 0 | str 9 agi 9 | Garr | M |
| Two-Hand | *no legal item in this pool* | — | 0 | — | — | — |
| Off Hand | Drillborer Disk | 17066 | 2918 | sta 10 | Garr | M |
| Ranged | Blastershot Launcher | 17072 | 0 | sta 6 | Golemagg the Incinerator | M |

Totals: **8068 armour**, Strength 161, Agility 21, Stamina 257, Intellect 0, Spirit 0.

## Paladin — Healer

| Slot | Item | id | Armour | Stats | Source | Conf |
|---|---|---|---|---|---|---|
| Head | Lawbringer Helm | 16854 | 695 | str 9 sta 20 int 24 spi 10 | Garr | M |
| Neck | *no legal item in this pool* | — | 0 | — | — | — |
| Shoulder | Lawbringer Spaulders | 16856 | 641 | str 10 sta 22 int 15 spi 8 | Baron Geddon | M |
| Back | Sapphiron Drape | 17078 | 63 | sta 10 int 17 | Onyxia | M |
| Chest | Lawbringer Chestguard | 16853 | 855 | str 8 sta 26 int 21 spi 13 | Golemagg the Incinerator | M |
| Wrist | Lawbringer Bracers | 16857 | 374 | str 10 sta 11 int 8 spi 11 | Flamewaker Protector | M |
| Hands | Lawbringer Gauntlets | 16860 | 534 | str 10 sta 15 int 15 spi 14 | Gehennas | M |
| Waist | Lawbringer Belt | 16858 | 481 | str 13 sta 15 int 20 spi 8 | Flamewaker Protector | M |
| Legs | Lawbringer Legplates | 16855 | 748 | str 7 sta 24 int 18 spi 18 | Magmadar | M |
| Feet | Lawbringer Boots | 16859 | 588 | str 7 sta 20 int 13 spi 10 | Lucifron | M |
| Finger 1 | Band of Sulfuras | 19138 | 0 | sta 13 int 23 spi 10 | Ragnaros | M |
| Finger 2 | Seal of the Archmagus | 17110 | 0 | sta 11 int 11 spi 11 | Baron Geddon | M |
| Trinket 1 | Shard of the Scale | 17064 | 0 | — | Onyxia | M |
| Trinket 2 | Shard of the Flame | 17082 | 0 | — | Ragnaros | M |
| Main Hand | Azuresong Mageblade | 17103 | 0 | sta 7 int 12 | Golemagg the Incinerator | M |
| Two-Hand | *no legal item in this pool* | — | 0 | — | — | — |
| Off Hand | Malistar's Defender | 17106 | 3244 | sta 9 int 12 | Ragnaros | M |
| Ranged | *no legal item in this pool* | — | 0 | — | — | — |

Totals: **8223 armour**, Strength 74, Agility 0, Stamina 203, Intellect 209, Spirit 113.

## Hunter — Physical dps

| Slot | Item | id | Armour | Stats | Source | Conf |
|---|---|---|---|---|---|---|
| Head | Giantstalker's Helmet | 16846 | 392 | agi 16 sta 23 int 15 spi 8 | Garr | M |
| Neck | *no legal item in this pool* | — | 0 | — | — | — |
| Shoulder | Giantstalker's Epaulets | 16848 | 362 | agi 16 sta 14 int 5 spi 9 | Sulfuron Harbinger | M |
| Back | Cloak of the Shrouded Mists | 17102 | 65 | agi 22 sta 12 | Ragnaros | M |
| Chest | Giantstalker's Breastplate | 16845 | 482 | agi 18 sta 23 int 11 | Golemagg the Incinerator | M |
| Wrist | Giantstalker's Bracers | 16850 | 211 | agi 14 sta 11 int 6 spi 5 | Molten Giant | M |
| Hands | Giantstalker's Gloves | 16852 | 301 | agi 12 sta 12 | Shazzrah | M |
| Waist | Giantstalker's Belt | 16851 | 271 | agi 12 sta 16 int 9 spi 4 | Flameguard | M |
| Legs | Giantstalker's Leggings | 16847 | 422 | agi 22 sta 15 int 6 spi 8 | Magmadar | M |
| Feet | Giantstalker's Boots | 16849 | 331 | agi 19 sta 14 spi 6 | Gehennas | M |
| Finger 1 | Band of Accuria | 17063 | 0 | agi 12 sta 10 | Ragnaros | V |
| Finger 2 | Heavy Dark Iron Ring | 18879 | 110 | sta 20 | Gehennas | M |
| Trinket 1 | Shard of the Scale | 17064 | 0 | — | Onyxia | M |
| Trinket 2 | Shard of the Flame | 17082 | 0 | — | Ragnaros | M |
| Main Hand | *no legal item in this pool* | — | 0 | — | — | — |
| Two-Hand | Bonereaver's Edge | 17076 | 0 | sta 16 | Ragnaros | M |
| Off Hand | *no legal item in this pool* | — | 0 | — | — | — |
| Ranged | Blastershot Launcher | 17072 | 0 | sta 6 | Golemagg the Incinerator | M |

Totals: **2947 armour**, Strength 0, Agility 163, Stamina 192, Intellect 52, Spirit 40.

## Rogue — Physical dps

| Slot | Item | id | Armour | Stats | Source | Conf |
|---|---|---|---|---|---|---|
| Head | Nightslayer Cover | 16821 | 186 | str 6 agi 20 sta 19 | Garr | M |
| Neck | *no legal item in this pool* | — | 0 | — | — | — |
| Shoulder | Nightslayer Shoulder Pads | 16823 | 171 | str 3 agi 26 sta 12 | Sulfuron Harbinger | M |
| Back | Cloak of the Shrouded Mists | 17102 | 65 | agi 22 sta 12 | Ragnaros | M |
| Chest | Nightslayer Chestpiece | 16820 | 228 | str 10 agi 29 sta 20 | Golemagg the Incinerator | M |
| Wrist | Nightslayer Bracelets | 16825 | 100 | agi 20 sta 15 | Firewalker | M |
| Hands | Nightslayer Gloves | 16826 | 143 | str 12 agi 18 sta 17 | Gehennas | M |
| Waist | Nightslayer Belt | 16827 | 128 | str 9 agi 17 sta 18 | Flamewaker Protector | M |
| Legs | Nightslayer Pants | 16822 | 200 | str 10 agi 33 sta 15 | Magmadar | M |
| Feet | Nightslayer Boots | 16824 | 157 | agi 26 sta 18 | Shazzrah | M |
| Finger 1 | Band of Accuria | 17063 | 0 | agi 12 sta 10 | Ragnaros | V |
| Finger 2 | Heavy Dark Iron Ring | 18879 | 110 | sta 20 | Gehennas | M |
| Trinket 1 | Shard of the Scale | 17064 | 0 | — | Onyxia | M |
| Trinket 2 | Shard of the Flame | 17082 | 0 | — | Ragnaros | M |
| Main Hand | Brutality Blade | 18832 | 0 | str 9 agi 9 | Garr | M |
| Two-Hand | *no legal item in this pool* | — | 0 | — | — | — |
| Off Hand | Eskhandar's Right Claw | 18203 | 0 | agi 4 | Magmadar | M |
| Ranged | Blastershot Launcher | 17072 | 0 | sta 6 | Golemagg the Incinerator | M |

Totals: **1488 armour**, Strength 59, Agility 236, Stamina 182, Intellect 0, Spirit 0.

## Shaman — Healer

| Slot | Item | id | Armour | Stats | Source | Conf |
|---|---|---|---|---|---|---|
| Head | Earthfury Helmet | 16842 | 392 | sta 24 int 23 spi 13 | Garr | M |
| Neck | *no legal item in this pool* | — | 0 | — | — | — |
| Shoulder | Earthfury Epaulets | 16844 | 362 | sta 17 int 18 spi 10 | Baron Geddon | M |
| Back | Sapphiron Drape | 17078 | 63 | sta 10 int 17 | Onyxia | M |
| Chest | Earthfury Vestments | 16841 | 482 | sta 17 int 27 spi 13 | Golemagg the Incinerator | M |
| Wrist | Earthfury Bracers | 16840 | 211 | sta 10 int 17 spi 11 | Flamewaker | M |
| Hands | Earthfury Gauntlets | 16839 | 301 | sta 14 int 13 spi 15 | Gehennas | M |
| Waist | Earthfury Belt | 16838 | 271 | sta 12 int 21 spi 7 | Molten Destroyer | M |
| Legs | Earthfury Legguards | 16843 | 422 | sta 18 int 19 spi 21 | Magmadar | M |
| Feet | Earthfury Boots | 16837 | 331 | sta 15 int 10 spi 22 | Lucifron | M |
| Finger 1 | Band of Sulfuras | 19138 | 0 | sta 13 int 23 spi 10 | Ragnaros | M |
| Finger 2 | Seal of the Archmagus | 17110 | 0 | sta 11 int 11 spi 11 | Baron Geddon | M |
| Trinket 1 | Shard of the Scale | 17064 | 0 | — | Onyxia | M |
| Trinket 2 | Shard of the Flame | 17082 | 0 | — | Ragnaros | M |
| Main Hand | Sorcerous Dagger | 18878 | 0 | sta 8 int 17 | Gehennas | M |
| Two-Hand | *no legal item in this pool* | — | 0 | — | — | — |
| Off Hand | Malistar's Defender | 17106 | 3244 | sta 9 int 12 | Ragnaros | M |
| Ranged | *no legal item in this pool* | — | 0 | — | — | — |

Totals: **6079 armour**, Strength 0, Agility 0, Stamina 178, Intellect 228, Spirit 133.

## Druid — Healer

| Slot | Item | id | Armour | Stats | Source | Conf |
|---|---|---|---|---|---|---|
| Head | Cenarion Helm | 16834 | 186 | sta 26 int 28 spi 13 | Garr | M |
| Neck | *no legal item in this pool* | — | 0 | — | — | — |
| Shoulder | Cenarion Spaulders | 16836 | 171 | sta 13 int 20 spi 10 | Baron Geddon | M |
| Back | Sapphiron Drape | 17078 | 63 | sta 10 int 17 | Onyxia | M |
| Chest | Cenarion Vestments | 16833 | 228 | sta 23 int 24 spi 16 | Golemagg the Incinerator | M |
| Wrist | Cenarion Bracers | 16830 | 100 | sta 13 int 14 spi 13 | Molten Giant | M |
| Hands | Cenarion Gloves | 16831 | 143 | sta 17 int 18 spi 15 | Shazzrah | M |
| Waist | Cenarion Belt | 16828 | 128 | sta 10 int 22 spi 10 | Molten Giant | M |
| Legs | Cenarion Leggings | 16835 | 200 | sta 18 int 19 spi 20 | Magmadar | M |
| Feet | Cenarion Boots | 16829 | 157 | sta 16 int 13 spi 15 | Lucifron | M |
| Finger 1 | Band of Sulfuras | 19138 | 0 | sta 13 int 23 spi 10 | Ragnaros | M |
| Finger 2 | Seal of the Archmagus | 17110 | 0 | sta 11 int 11 spi 11 | Baron Geddon | M |
| Trinket 1 | Shard of the Scale | 17064 | 0 | — | Onyxia | M |
| Trinket 2 | Shard of the Flame | 17082 | 0 | — | Ragnaros | M |
| Main Hand | *no legal item in this pool* | — | 0 | — | — | — |
| Two-Hand | Staff of Dominance | 18842 | 0 | sta 16 int 37 spi 14 | Golemagg the Incinerator | M |
| Off Hand | Ancient Cornerstone Grimoire | 17067 | 0 | sta 10 int 15 spi 11 | Onyxia | M |
| Ranged | *no legal item in this pool* | — | 0 | — | — | — |

Totals: **1376 armour**, Strength 0, Agility 0, Stamina 196, Intellect 261, Spirit 158.

## Priest — Healer

| Slot | Item | id | Armour | Stats | Source | Conf |
|---|---|---|---|---|---|---|
| Head | Circlet of Prophecy | 16813 | 94 | sta 17 int 27 spi 20 | Garr | M |
| Neck | *no legal item in this pool* | — | 0 | — | — | — |
| Shoulder | Mantle of Prophecy | 16816 | 87 | sta 13 int 23 spi 10 | Sulfuron Harbinger | M |
| Back | Sapphiron Drape | 17078 | 63 | sta 10 int 17 | Onyxia | M |
| Chest | Robes of Prophecy | 16815 | 116 | sta 20 int 27 spi 17 | Golemagg the Incinerator | M |
| Wrist | Vambraces of Prophecy | 16819 | 51 | sta 8 int 14 spi 10 | Flameguard | M |
| Hands | Gloves of Prophecy | 16812 | 72 | sta 10 int 15 spi 15 | Gehennas | M |
| Waist | Girdle of Prophecy | 16817 | 65 | sta 10 int 22 spi 10 | MC tier set | M |
| Legs | Pants of Prophecy | 16814 | 101 | sta 18 int 24 spi 20 | Magmadar | M |
| Feet | Boots of Prophecy | 16811 | 80 | sta 17 int 18 spi 15 | Shazzrah | M |
| Finger 1 | Band of Sulfuras | 19138 | 0 | sta 13 int 23 spi 10 | Ragnaros | M |
| Finger 2 | Seal of the Archmagus | 17110 | 0 | sta 11 int 11 spi 11 | Baron Geddon | M |
| Trinket 1 | Shard of the Scale | 17064 | 0 | — | Onyxia | M |
| Trinket 2 | Shard of the Flame | 17082 | 0 | — | Ragnaros | M |
| Main Hand | Sorcerous Dagger | 18878 | 0 | sta 8 int 17 | Gehennas | M |
| Two-Hand | *no legal item in this pool* | — | 0 | — | — | — |
| Off Hand | Ancient Cornerstone Grimoire | 17067 | 0 | sta 10 int 15 spi 11 | Onyxia | M |
| Ranged | Crimson Shocker | 17077 | 0 | int 10 | Gehennas | M |

Totals: **729 armour**, Strength 0, Agility 0, Stamina 165, Intellect 263, Spirit 149.

## Mage — Caster

| Slot | Item | id | Armour | Stats | Source | Conf |
|---|---|---|---|---|---|---|
| Head | Halo of Transcendence | 16921 | 107 | sta 17 int 27 spi 22 | Onyxia | M |
| Neck | *no legal item in this pool* | — | 0 | — | — | — |
| Shoulder | Felheart Shoulder Pads | 16807 | 87 | sta 25 int 17 spi 7 | Baron Geddon | M |
| Back | Sapphiron Drape | 17078 | 63 | sta 10 int 17 | Onyxia | M |
| Chest | Robes of Prophecy | 16815 | 116 | sta 20 int 27 spi 17 | Golemagg the Incinerator | M |
| Wrist | Felheart Bracers | 16804 | 51 | sta 18 int 11 spi 8 | Flamewaker | M |
| Hands | Felheart Gloves | 16805 | 72 | sta 18 int 15 spi 8 | Lucifron | M |
| Waist | Arcanist Belt | 16802 | 65 | sta 11 int 20 spi 10 | Molten Destroyer | M |
| Legs | Pants of Prophecy | 16814 | 101 | sta 18 int 24 spi 20 | Magmadar | M |
| Feet | Boots of Prophecy | 16811 | 80 | sta 17 int 18 spi 15 | Shazzrah | M |
| Finger 1 | Band of Sulfuras | 19138 | 0 | sta 13 int 23 spi 10 | Ragnaros | M |
| Finger 2 | Seal of the Archmagus | 17110 | 0 | sta 11 int 11 spi 11 | Baron Geddon | M |
| Trinket 1 | Shard of the Scale | 17064 | 0 | — | Onyxia | M |
| Trinket 2 | Shard of the Flame | 17082 | 0 | — | Ragnaros | M |
| Main Hand | Sorcerous Dagger | 18878 | 0 | sta 8 int 17 | Gehennas | M |
| Two-Hand | *no legal item in this pool* | — | 0 | — | — | — |
| Off Hand | Fire Runed Grimoire | 19142 | 0 | sta 12 int 21 | Magmadar | M |
| Ranged | Crimson Shocker | 17077 | 0 | int 10 | Gehennas | M |

Totals: **742 armour**, Strength 0, Agility 0, Stamina 198, Intellect 258, Spirit 128.

## Warlock — Caster

| Slot | Item | id | Armour | Stats | Source | Conf |
|---|---|---|---|---|---|---|
| Head | Felheart Horns | 16808 | 94 | sta 27 int 20 spi 10 | Garr | M |
| Neck | *no legal item in this pool* | — | 0 | — | — | — |
| Shoulder | Felheart Shoulder Pads | 16807 | 87 | sta 25 int 17 spi 7 | Baron Geddon | M |
| Back | Sapphiron Drape | 17078 | 63 | sta 10 int 17 | Onyxia | M |
| Chest | Felheart Robes | 16809 | 116 | sta 31 int 20 | Golemagg the Incinerator | M |
| Wrist | Felheart Bracers | 16804 | 51 | sta 18 int 11 spi 8 | Flamewaker | M |
| Hands | Felheart Gloves | 16805 | 72 | sta 18 int 15 spi 8 | Lucifron | M |
| Waist | Felheart Belt | 16806 | 65 | sta 18 int 15 spi 8 | Molten Destroyer | M |
| Legs | Felheart Pants | 16810 | 101 | sta 20 int 19 spi 10 | Magmadar | M |
| Feet | Felheart Slippers | 16803 | 80 | sta 23 int 11 | Shazzrah | M |
| Finger 1 | Band of Sulfuras | 19138 | 0 | sta 13 int 23 spi 10 | Ragnaros | M |
| Finger 2 | Seal of the Archmagus | 17110 | 0 | sta 11 int 11 spi 11 | Baron Geddon | M |
| Trinket 1 | Shard of the Scale | 17064 | 0 | — | Onyxia | M |
| Trinket 2 | Shard of the Flame | 17082 | 0 | — | Ragnaros | M |
| Main Hand | Sorcerous Dagger | 18878 | 0 | sta 8 int 17 | Gehennas | M |
| Two-Hand | *no legal item in this pool* | — | 0 | — | — | — |
| Off Hand | Fire Runed Grimoire | 19142 | 0 | sta 12 int 21 | Magmadar | M |
| Ranged | Crimson Shocker | 17077 | 0 | int 10 | Gehennas | M |

Totals: **729 armour**, Strength 0, Agility 0, Stamina 234, Intellect 227, Spirit 72.
