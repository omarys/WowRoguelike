---
type: Concept
title: Combat Mechanics, Threat & Abilities
description: Rules governing physical mitigation, 10%/30% threat margins, tank stances, spell pushback, interrupt lockouts, and content verification.
tags:
  - combat
  - threat
  - mitigation
  - abilities
verified:
  - by: openwiki/0.7.2
    at: 2026-10-15T19:24:01.244Z
sources:
  - id: openwiki-source-2e17704c2855416358a8aa67
    resource: repo://src/WowRoguelike.Core/Content.fs
  - id: openwiki-source-c728107969eaa220e0103eb9
    resource: repo://src/WowRoguelike.Core/SimCombat.fs
  - id: openwiki-source-847677da125a61b02dfa5ccc
    resource: repo://src/WowRoguelike.Core/SimState.fs
generated: { by: "antigravity", at: "2026-10-15T19:24:01.244Z" }
---

# Combat Mechanics, Threat & Abilities

Combat resolution in `WowRoguelike` adapts Classic World of Warcraft mechanics into a 20Hz deterministic simulation. Game rules operate as pure transforms in [`SimCombat.fs`](../../src/WowRoguelike.Core/SimCombat.fs) and [`SimState.fs`](../../src/WowRoguelike.Core/SimState.fs), while abilities and encounter configurations are defined as data records in [`Content.fs`](../../src/WowRoguelike.Core/Content.fs).

---

## 1. Threat & Aggro Tables

Hostile mobs evaluate target priority through threat tables populated exclusively by the Party:

- **Aggro Margins**: A challenger pulls aggro from the current target only when exceeding the current target's threat by a margin based on the challenger's distance to the mob:
  - **Melee range**: Challenger must exceed current target's threat by **10%** (`meleeMarginBp = 1000`).
  - **Ranged**: Challenger must exceed current target's threat by **30%** (`rangedMarginBp = 3000`).
  - Integer evaluation: `topThreat * 10000 > curThreat * (10000 + margin)`.
- **Threat Multipliers**:
  - Baseline party attacks generate threat at $1.0\times$ (`10000` basis points).
  - Tank stance (Warrior) generates threat at **$5.0\times$** (`50000` basis points), ensuring the tank holds aggro despite lower raw DPS.
  - Effective healing generates half its value in threat ($0.5\times$), split evenly among all engaged hostile observers in the encounter.
- **Forced Targets & Taunt**: Abilities like Taunt assign a temporary forced target or set the challenger's threat to the maximum on the table.

---

## 2. Damage & Mitigation Pipeline

Combat damage distinguishes strictly between physical strikes and spells:

### Physical Mitigation Pipeline
Physical hits pass through a three-stage mitigation pipeline:
1. **Dodge**: Checked against `DodgeChanceBp`. A successful roll results in full avoidance (0 damage).
2. **Block**: Checked against `BlockChanceBp`. A successful roll subtracts a flat `BlockValue` from incoming damage: `afterBlock = max 0 (amount - blocked)`.
3. **Armor Mitigation**: Linear reduction where each point of effective armor provides 10 basis points (0.1%) of physical reduction, capped at 75% (`min 7500 (armor * 10)`). Effective armor accounts for debuffs like Sunder Armor.

### Spell Mitigation
Spells and magical effects bypass Dodge, Block, and Armor completely, dealing raw damage directly to the health pool unless absorbed or resisted.

---

## 3. Spellcasting, Pushback & Interrupt Lockout

- **Cast Windows**: Casts progress across discrete Ticks. If an ability is tagged `Interruptible`, it can be disrupted before resolving.
- **Resource Timing**: Resources (mana/energy/rage) are deducted upon **resolution**, not initiation. An interrupted cast incurs zero resource cost.
- **Pushback**: Taking damage while casting an interruptible ability triggers a pushback check. The base interruption chance is 10% (`pushbackBaseBp = 1000`), scaled down by the caster's resistance:
  $$\text{chance} = \text{pushbackBaseBp} \times \frac{10000 - \text{PushbackResistanceBp}}{10000}$$
- **Interrupt Lockout**: Direct interrupt abilities (e.g., Kick) immediately abort an in-progress cast and impose a school lockout of 4.0 seconds (`interruptLockout = secTicks 4.0`, or 80 ticks at 20Hz).

---

## 4. Content Boundaries: Sourced vs. Placeholder

The top of [`Content.fs`](../../src/WowRoguelike.Core/Content.fs#L7-L38) maintains a strict `VERIFY` block distinguishing sourced WoW data from placeholders:

| Classification | Attributes & Spells | Authority / Note |
|---|---|---|
| **Sourced** | Druid's Slumber (15s sleep, wakes on damage) | Classic Era Warcraft Wiki (Spell 8040) |
| **Sourced** | Healing Touch (195–243 healing) | Spells 5187, 23381 |
| **Sourced** | Serpent Form (10s duration, +25 physical) | Spell 8041 |
| **Sourced** | Threat Margins (10% melee / 30% ranged, 5.0x tank stance) | Classic Era threat mechanics |
| **Sourced** | Lady Anacondra 4-corner spawn points & Fanglord kit | Wailing Caverns dungeon data |
| **Placeholder** | Cast times, cooldowns, auto-attack swing timers | Pacing adjustments; first tuning knobs |
| **Placeholder** | Mob health pools, Lightning Bolt damage ranges | Pacing adjustments |
| **Invented** | 5 Party classes, base stat distributions, party buffs | Custom archetype design for roguelike loop |

---

## 5. Related Pages

- [Simulation Core & Deterministic State](simulation-core.md) — The 20Hz tick execution and state transitions.
- [Dungeon Generation & Grid Pathfinding](dungeon-and-pathfinding.md) — Mob movement, positioning, and line of sight.
- [System Architecture](../architecture/overview.md) — Project boundaries and ADR invariants.
