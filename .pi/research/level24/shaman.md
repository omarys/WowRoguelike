# Level 24 Shaman (Vanilla 1.12 / Classic Era 1.15.9) — sourced numbers

Every rank below is the **highest a level-24 shaman has trained**. Ranks above 24 are
listed only to prove they are out of reach.

## 1. Base stats at level 24, before gear

`base_stats.rs` (`BaseStats::new(str, agi, sta, int, spi, hp, mana)`), entry index 23.
Copied verbatim from `.pi/research/level24/base_stats_new_classes.md`, which quotes the
level-1 and level-24 rows of all three shaman arrays.

| | Orc | Troll | Tauren |
|---|---|---|---|
| Str | 43 | 41 | 45 |
| Agi | 28 | 33 | 26 |
| Sta | 45 | 44 | 45 |
| Int | 39 | 38 | 37 |
| Spi | 49 | 47 | 48 |
| Base health | 234 | 234 | 234 |
| Base mana | 476 | 476 | 476 |

The sim builds the **Orc** (`orcShamanBase`), the same way it builds a Gnome warlock and
a Human paladin.

## 2. Abilities and ranks at 24

Source for every row: the rank tables at
https://wowcalc.net/en/classic/wiki/spells/shaman (read from the 1.15.9 client,
build 1.15.9.70003). Cross-checked against classicdb.ch where a spell page was fetched
(Earth Shock 125-133 at rank 4, Healing Wave 389-453 at rank 5).

### Elemental

| Ability | Rank | Level | Mana | Cast | CD | Range | Effect |
|---|---|---|---|---|---|---|---|
| Lightning Bolt | 4 | 20 | 75 | 3.0 s | — | 30 yd | 88-100 Nature |
| Earth Shock | 4 | 24 | 145 | instant | 6 s | 20 yd | 125-133 Nature, **interrupts + 2 s school lockout**, high threat |
| Flame Shock | 2 | 18 | 95 | instant | 6 s | 20 yd | 51 Fire + 48 Fire over 12 s |
| Frost Shock | 1 | 20 | 115 | instant | 6 s | 20 yd | 95-101 Frost, -50 % speed for 8 s |
| Searing Totem | 2 | 20 | 45 | instant | — | 20 yd | totem, 35 s, 13-17 Fire per attack |
| Fire Nova Totem | 2 | 22 | 170 | instant | 15 s | 10 yd | 110-124 Fire, delayed 4 s |
| Stoneclaw Totem | 2 | 18 | 30 | instant | 30 s | 8 yd | 276 health, 15 s, taunts |
| Earthbind Totem | 1 | 6 | 6 % base | instant | 15 s | 10 yd | slow, 45 s |
| Purge | 1 | 12 | 10 % base | instant | — | 30 yd | strips 1 magic effect |

### Enhancement

| Ability | Rank | Level | Mana | Effect |
|---|---|---|---|---|
| Rockbiter Weapon | 4 | 24 | 75 | **+193 melee attack power**, 5 min |
| Lightning Shield | 3 | 24 | 125 | 3 charges, **51 Nature** each, 10 min |
| Stoneskin Totem | 3 | 24 | 90 | party takes **-11 melee damage**, 20 yd, 2 min |
| Strength of Earth Totem | 2 | 24 | 65 | party **+20 Strength**, 20 yd, 2 min |
| Frostbrand Weapon | 1 | 20 | 60 | 47 Frost on a proc |
| Ghost Wolf | 1 | 20 | 100 | +40 % speed, outdoor only |

### Restoration

| Ability | Rank | Level | Mana | Cast | Effect |
|---|---|---|---|---|---|
| Healing Wave | 5 | 24 | 200 | 3.0 s | heals **389-453** |
| Lesser Healing Wave | 1 | 20 | 105 | 1.5 s | heals **170-194** |
| Healing Stream Totem | 1 | 20 | 40 | instant | 6 health every 2 s, 20 yd, 1 min |
| Tremor Totem | 1 | 18 | 60 | instant | clears fear/charm/sleep, 30 yd |
| Cure Poison / Cure Disease | 1 | 16 / 22 | 9 % base | instant | one effect |

### Not reachable at 24 (verified next-rank level)

Windfury Weapon **30**, Mana Spring Totem 26, Chain Lightning 32, Windfury Totem 32,
Magma Totem 26, Grace of Air Totem 42, Chain Heal 40, Bloodlust/Heroism 60,
Grounding Totem 30, Nature's Swiftness (talent) 30, Elemental Mastery (talent) 40,
Stormstrike (talent) 40.

**Windfury is the one that matters**: the vanilla shaman identity at 60, and a level-24
shaman does not have it. Rockbiter rank 4's +193 attack power is the 24 substitute.

## 3. Gear at 24 — what is verified

No separately sourced level-24 shaman set was produced for this file: the fetch that was
meant to produce it ran without web tools. The sim therefore dresses the shaman in the
**same ≤24 pieces already verified for the Druid** (`.pi/research/level24/druid.md`), all
of which carry a level requirement of 24 or less:

Azure Silk Hood (24), Silver-thread Amice (24), Watcher's Cape (19), Robes of Arugal (24),
Silver-thread Cuffs (22), Belt of Arugal (24), Darkweave Breeches (17), Spidersilk Boots
(20), Lavishly Jeweled Ring (17), Staff of the Blessed Seer (18).

That is 238 item armour and **Str +0, Agi +5, Sta +17, Int +48, Spi +29** on top of the
Orc base. Five slots stay unfilled, exactly as they do for the other casters, so the set
is a floor and not a true BiS.

- **Not verified:** whether a level-24 shaman can do better than this with mail or a
  one-hander and shield. Shaman trains mail at 40, so the leather/cloth restriction is
  correct for the bracket, but no ≤24 mail-and-shield shaman list was sourced.
- The staff is kept over a one-hander because it is the verified ≤24 caster weapon the
  other classes already use; a shield would raise armour and is not modelled.

## 4. What the sim models, and what it does not

Modelled: Lightning Bolt, Earth Shock (with its interrupt), Flame Shock (direct + DoT),
Healing Wave, Reincarnation (§5), and the rank-4 base-stat line.

**Not modelled, with a `shortcut:` comment in `Content.fs`:**

- **Totems.** `Summon`/`SummonSpec` exist but have no caller in the sim yet. A totem
  would be the first thing to exercise that path, which is a job for its own change.
- **Lightning Shield** (retaliation charges), **Rockbiter** (+193 AP is a weapon imbue,
  and nothing in the vocabulary imbues), **Stoneskin/Strength of Earth** (party-aura
  totems, same missing Summon path).
- **Shock cooldowns are shared in WoW.** The sim gives each ability its own cooldown, so
  a shaman can chain Earth Shock and Flame Shock; in the game one locks out the other for
  6 s. `shortcut:` recorded on the abilities.
- **Reincarnation's Ankh reagent and the ghost walk back to the corpse.** The
  resurrection itself is modelled (§5); what 1.12 wraps around it is not.

## 5. Reincarnation and Improved Reincarnation

Every number the sim's resurrection uses. `Content.reincarnation` is the ability and
`shamanTalents.CooldownOff` is the talent's cooldown cut.

| Source | Cooldown | Restores | Cost / reagent | Confidence |
|---|---|---|---|---|
| Reincarnation, Classic Era (build 1.15.9) | 30 min | 20% health, 20% mana | none, 1 Ankh, self only, instant | `V` |
| Reincarnation, 1.12 (patch 1.1.1) | 60 min | 20% health, 20% mana | none, 1 Ankh, self only, instant | `V` |
| Improved Reincarnation 1/2 | −10 min | +10% health and mana | Restoration tier 2 | `V` |
| Improved Reincarnation 2/2 | −20 min | +20% health and mana | Restoration tier 2 | `V` |
| Learned at | level 30 in 1.12 (level 8 in Classic Era) | — | — | `V` |

**Contradiction, and the value the sim uses.** The two cooldown rows cannot both hold:
30 min less rank 2's 20 min is 10 min, while 1.12's 60 min less the same 20 min is 40.
This project models 1.12 mechanics with Classic Era numbers, so the sim takes
**30 min base, less 10 min at 2/2 = 20 min** — chosen rather than derived; the 1.12 base
of 60 min would make the button unusable more than once a run either way. The restored
fraction has no such conflict: 20% + 20% at 2/2 = **40% health and mana**, and the sim's
shaman build carries the talent at 2/2, so `Content.reincarnation` restores 40% of both.

Not modelled (`shortcut:` on the ability): the Ankh reagent, the ghost-walk to the
corpse, and the level-30 trainer gate — the sim's kit is the level-60 tree on a level-24
body, exactly as Nature's Swiftness and Swiftmend are.

## 6. Sources

1. https://wowcalc.net/en/classic/wiki/spells/shaman — every rank table above.
2. https://classicdb.ch/?spell=8042 (Earth Shock rank 4) — 125-133.
3. https://www.wowhead.com/classic/spell=939/healing-wave — Healing Wave rank 5, 389-453.
4. `.pi/research/level24/base_stats_new_classes.md` — the three shaman base arrays.
5. `.pi/research/level24/druid.md` — the verified ≤24 gear pieces reused in §3.
6. https://warcraft.wiki.gg/wiki/Reincarnation and https://www.wowdb.com/spells/20608-reincarnation
   — cooldown and the 20% health/mana tooltip (Classic Era, 1.15.9).
7. https://warcraft.wiki.gg/wiki/Improved_Reincarnation — −10/−20 min and +10/+20% health
   and mana at 1/2 and 2/2.
8. https://warcraft.wiki.gg/wiki/Patch_1.1.1 — Reincarnation "once every hour" at launch.
