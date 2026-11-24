# Shaman damage lane and interrupts — findings for pass 3

Discovered while answering the operator's questions about Earth Shock. Two separate defects
live in the same lane, and they push in opposite directions, so they must be measured together.

## 1. The interrupt assignment is party-wide consensus, and one lane drops it

`src/WowRoguelike.Core/PartyAi.fs:113-145` — `interruptAssignments (w: World)`.
Every interruptible cast worth breaking (`worthBreaking`) is assigned to **exactly one**
party member: the one carrying the cheapest kick that can land, sorted by
`((if a.Range <= 1 then 0 else 1), a.CooldownTicks)`, then by distance, then by lower id.
The comment states the intent: "each caster caught in the middle of a spell is assigned
exactly one interrupter, so two members never spend a kick on the same cast and nobody holds
back waiting for permission."

`interruptTargets (w) (m)` at `:149` returns the set of hostiles for which `m` won the job.

**It is read in only three lanes:**

- `:453` `tankOrders` (Warrior / bear)
- `:1731` `meleeOrders` (Rogue)
- `:1853` `rangedOrders` (Mage / Warlock / Hunter)

**`healerOrders` at `:588` does not read it at all.** The Shaman holds the healer seat in the
measured roster (Warrior + Priest + Mage + Warlock + Shaman), so its lane never acts on an
assignment.

### Why this is a party-wide bug, not a Shaman inconvenience

Because each cast is assigned to exactly one member, an assignment that lands on a lane that
ignores it means **nobody kicks that cast** — the other members have been told it is handled.
Earth Shock is range 5 (so it sorts behind every melee kick) with a 6 s cooldown (so it beats
Counterspell's 24 s when no melee interrupt is available). That makes it a frequent winner.

Two possible fixes, and the second is the honest one:

- (a) teach `healerOrders` (and any other lane holding an interrupt) to consume the assignment;
- (b) make `interruptAssignments` only offer the job to members whose lane will act on it.

(b) alone would silently reduce interrupt coverage. The right shape is probably (a) for every
lane that can interrupt, plus a guard so the two can never drift apart again.

## 2. Earth Shock never fires

30 seeds, `sim shmana 30`: **zero Earth Shock casts.** Not misuse — absence. It is in the
Shaman's kit (`Content.fs:5845` includes `earthShockRanks`), instant, 6 s cooldown, range 5,
effects carry `Interrupt`.

Operator ruling: **Earth Shock is to be used only as an interrupt**, never as a damage filler.

## 3. Shock cooldown is not shared — a shortcut that a fix would silently exploit

`src/WowRoguelike.Core/Content.fs:1217-1219`:

> `shortcut: shocks share one 6 s cooldown in WoW; here each has its own, so a shaman can chain Earth Shock into Flame Shock.`

Teaching the AI to press Earth Shock without modelling the shared cooldown gives the Shaman
two independent 6 s shock buttons — more interrupts than vanilla allows *and* more damage.
The two changes must land together.

## 4. Flame Shock is the least mana-efficient damage button and gets the most mana

Level 36 ranks, from `Content.fs`:

| spell | cost | damage | dmg/mana | cast |
|---|---|---|---|---|
| Lightning Bolt (r32) | 135 | 179-201 (190) | **1.41** | 3.0 s, `Interruptible = true` |
| Earth Shock (r24) | 145 | 125-133 (129) | 0.89 | instant, 6 s CD, range 5 |
| Flame Shock (r28) | 160 | 94 + 8x4 = 126 | **0.79** | instant, 6 s CD, range 5 |

Lightning Bolt is 78% more mana-efficient than Flame Shock, lands more damage per cast, and
costs 25 mana less. Yet over 30 seeds Flame Shock is the Shaman's largest damage-lane
spender: **364 casts, 58,240 mana, 15.5% of all spend**, against Lightning Bolt's 353 casts /
47,617 mana / 12.7%. Full damage lane: Purge 355 / 31,950 / 8.5%, Flame Shock 58,240,
Lightning Bolt 47,617 — **105,857 mana over 30 seeds, 3,528 a seed**, against 154,481 healing
output per seed.

Flame Shock's one genuine merit is being instant, so it belongs as movement filler. It should
not outnumber Lightning Bolt when the Shaman can stand still. Note Lightning Bolt is
`Interruptible = true`, so a caster-punishing wing punishes standing still — the two rules
interact.

## Pass 3 scope (agreed with the operator)

1. `healerOrders` consumes `interruptTargets`; Earth Shock strictly as an interrupt, ahead of
   any damage spell.
2. Fix the dropped-assignment bug generally — every lane that can interrupt must act on an
   assignment, or must not be offered one.
3. Model the shared 6 s shock cooldown so Earth Shock displaces Flame Shock.
4. Prefer Lightning Bolt when stationary; Flame Shock as movement filler only.

## Watch for

- Interrupts cut incoming damage, which helps **every** healer. This may partly answer the
  open Priest question (pass 1: hps 88.4 -> 80.1 because freed idle ticks fed its nuke).
- Items 3 and 4 both *reduce* Shaman damage while items 1 and 2 increase control, so a single
  aggregate number will hide the trade. Report dps, damage prevented, and mana separately.
- Pass 2 (Water Shield) changes the Shaman's mana budget substantially. Baseline pass 3
  against the post-pass-2 tree, not the numbers above.
