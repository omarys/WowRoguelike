# Level bands and per-class ladders

Evidence for scaling the dungeons by character level. Six source reports sit beside this
file; this page is the index and the summary.

| File | Covers |
|---|---|
| `dungeon-bands.md` | enter/trash/boss levels, derived min/median/max, boss and room counts, citations |
| `warrior-rogue-ladder.md` | Warrior and Rogue ability ranks by level 13–40, gear by level band |
| `priest-druid-ladder.md` | Priest and Druid ability ranks by level 13–40, gear by level band |
| `mage-ladder.md` | Mage ability ranks by level 13–40, wands, gear by level band |
| `map-assets.md` | whether each image in `images/Maps/` can become a tile grid, and what is missing |
| `healer-procs.md` | Healing Way, Inspiration/Ancestral Healing, Illumination, and the four "next spell" cooldowns |

Confidence is marked in every source table: `V` verified from a cited page, `M` corroborated
second source, `U`/`UNVERIFIED` inference — do not ship a `U` as fact.

## Dungeon bands

`min` = entrance-trash floor. `max` = highest boss. `median` = highest boss − 1 (a group one
level under the final boss has its full toolkit and out-levels the last pull); where the
researcher's rule and a documented recommendation disagreed by more than ~2 levels, the
disagreement is stated in the source report.

| Dungeon | Enter | Trash | Bosses | Documented | **min** | **median** | **max** |
|---|---|---|---|---|---|---|---|
| Ragefire Chasm | 10 | 13–15 | 16 ×4 | 13–18 | 13 | 15 | 16 |
| The Deadmines | 10 | 16–20 | 19–21 | 18–23 | 16 | 20 | 21 |
| Wailing Caverns | 10 | 17–21 | 20–22 | 15–25 | 17 | 21 | 22 |
| Shadowfang Keep | 14 | 18–25 | 20–26 | 22–30 | 18 | 25 | 26 |
| The Stockade | 15 | 23–26 | 24–29 | 22–30 | 23 | 28 | 29 |
| Blackfathom Deeps | 15 | 22–27 | 25–28 | 24–32 | 22 | 27 | 28 |
| Razorfen Kraul | 25 | 24–32 | 28–33 | 30–40 | 24 | 32 | 33 |
| SM Graveyard | 21 | 30–33 | 32–34 | ~29–34 | 30 | 33 | 34 |
| SM Library | 21 | 33–36 | 34–37 | ~31–35 | 33 | 36 | 37 |
| SM Armory | 21 | 37–38 | 40 | ~36–38 | 37 | 39 | 40 |
| SM Cathedral | 21 | 38–40 | 40–42 | ~37–40 | 38 | 41 | 42 |

Note the ordering trap: Shadowfang Keep has a lower entrance band than Blackfathom Deeps and
The Stockade, and Scarlet Monastery's four wings are four separate bands, not one.

## What "median" means in play

The only band measured inside the simulation so far is Wailing Caverns, run at every level on
five seeds with `Content.wailingCavernsAt`:

| Level | Clears | End state |
|---|---|---|
| 14 | 0/5 | wiped around 195 s |
| 16 | 0/5 | wiped around 200 s |
| 18 | 0/5 | wiped 170–450 s in |
| 20 | 5/5 | 81–94% health, 2–5 alive |
| 22 | 5/5 | 75–91% health, 3–5 alive |
| 24 | 5/5 | 97–100% health, 5 alive |

Against the table above the dungeon reads min 17 / median 21 / max 22, and a five-player
party needed 20 to clear it reliably — the last two levels under the median are wipes. Treat
that offset as the calibration between the researched band and what this simulation's party
can actually do: the sim needs roughly `median − 1`, not `min`.

## Class data

Each ladder gives, for levels 13–40: which rank of each simulated ability is live at that
level, and what that rank costs and does; then gear by band (13–17, 18–22, 23–27, 28–32,
33–37, 38–40) with item level requirements and stats. The abilities covered are exactly the
ones `src/WowRoguelike.Core/Content.fs` models, so a band can be filled in without a second
research pass.

Two caveats that affect how much of it can be used as-is:

- **Per-rank numbers are less solid than rank→level mappings.** Wowhead spell pages are
  JS-rendered and refused plain fetches during this research; the working sources were
  `wowhead.com/classic/spell=<id>` raw HTML, `warcraft.wiki.gg`, `wowcalc.net` and
  `classicdb.ch` via search extracts. Ranks are `V`; several per-rank values are `M` or `U`.
- **Gear bands are "plausible high-achievement levelling gear", not best-in-slot.** Several
  armour rows have a verified required level but unverified armour value or stats.

## Maps

Only **Wailing Caverns** is usable as-is, and only through its five section images (the
`WorldMap` file is a parchment scroll whose paper reads as floor).

| Dungeon | Verdict |
|---|---|
| Wailing Caverns | usable now — five room grids already extracted into `Maps.fs` |
| The Stockade | `overview.jpg` (8192×4202) is a hand-painted orthographic plan with black negative space — good for **one whole-dungeon grid**, but not at the extractor's default 56-column scale (walls are thinner than one cell) |
| The Deadmines | `overhead.jpg` (8000×4796) same story; the four perspective screenshots are unusable |
| Shadowfang Keep | 13 orthographic one-room renders, correctly split per room, but **contrast inverted** (pale stone walls, dark flagstone floors) so the current dark-wall classifier misreads it; either invert the classifier or re-source |
| Ragefire Chasm | unusable — 345×376, label-covered, too small for a room grid at any scale |
| Razorfen Kraul | unusable — 650×636 overview with labels |
| Blackfathom Deeps | **missing entirely** |
| Scarlet Monastery | **missing entirely** |

The single most useful next asset is a label-free top-down floorplan per room, dark walls on
light floor, at least ~1000 px on the short side — that is what the five Wailing Caverns
sections happen to be.

## Scarlet Monastery: what was built from this data

`src/WowRoguelike.Core/MapsScarlet.fs` holds four grid fixtures thresholded from
`images/Maps/ScarletMonastery/`, each sealed to a single connected region.
`Content.scarletMonasteryAt <wing> <difficulty> <seed>` enters a wing at one of the
three bands above.

Creature rows come from database.emberveil.org, a vanilla 1.12 database that serves
server-rendered HTML (`/creature/<id>`, and `/creatures?zone=796&sort=health` for
the health column, which the creature page omits). Wowhead Classic does not publish
creature armour, so armour stays the fitted 40 a level used across this file.

### The open calibration problem

The party stops improving at level 24: `Content.dress` wears the level-24 best-in-set
pieces and filters them by required level, so a level-41 party still swings a
level-24 weapon against level-40 elites. Measured at the median band, that is:

| wing | median level | hostiles killed | outcome |
|---|---|---|---|
| graveyard | 33 | 40 of 43 | party alive, full health |
| library | 36 | 12 of 39 | party alive, healer at 75% |
| armory | 39 | 17-33 of 45 | wipe |
| cathedral | 41 | 1 of 57 | wipe |

Graveyard is already in band; the higher wings are not, and the missing gear is the
largest single term. Weapon damage per band is the one to fix first.
