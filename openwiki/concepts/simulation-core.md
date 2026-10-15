---
type: Concept
title: Simulation Core & Deterministic State
description: Details the 20Hz discrete tick lifecycle, state transformation pipeline, partitioned SplitMix64 PRNG, and golden-replay fingerprinting.
tags:
  - simulation
  - core
  - determinism
  - tick
verified:
  - by: openwiki/0.7.2
    at: 2026-10-15T19:24:01.244Z
sources:
  - id: openwiki-source-767b3e950fd5e6b0a1f59629
    resource: repo://docs/adr/0002-hand-rolled-prng-integer-core.md
  - id: openwiki-source-412a068b5edc86cc2688da74
    resource: repo://src/WowRoguelike.Core/Dump.fs
  - id: openwiki-source-4abbd72e07e3b5008bc71ff7
    resource: repo://src/WowRoguelike.Core/Rng.fs
  - id: openwiki-source-8313b285bc5542821256d180
    resource: repo://src/WowRoguelike.Core/SimTick.fs
generated: { by: "antigravity", at: "2026-10-15T19:24:01.244Z" }
---

# Simulation Core & Deterministic State

The simulation core in `WowRoguelike.Core` is an immutable, deterministic state machine executing at 20Hz (50ms per tick). It advances strictly through pure functions, mapping an existing `World` record and incoming player commands into an updated `World`.

---

## 1. The Per-Tick Execution Pipeline

The heartbeat of the simulation is `SimTick.step` in [`SimTick.fs`](../../src/WowRoguelike.Core/SimTick.fs#L427-L450). Every tick advances the state through a deterministic sequence of phases:

```
World(t), Commands
      │
      ▼
1. Advance Tick & Partition Due Commands (ApplyAt <= w.Tick)
      ▼
2. Apply Player Commands (Player intent always lands first)
      ▼
3. Advance Cooldowns (Decrement ability cooldown timers)
      ▼
4. Advance Resources (Accumulate class resources; mob regen is zero)
      ▼
5. Advance Auras (Age auras, purge expired effects)
      ▼
6. Advance Aura Healing (Apply HoTs and Polymorph regeneration)
      ▼
7. Advance Aura Damage (Apply DoTs unmitigated; wakes sleepers)
      ▼
8. Advance Movement (Interpolate fractional movement steps, claim tiles)
      ▼
9. Advance Casts (Tick in-progress casts, resolve completed spells)
      ▼
10. Advance Swings (Auto-attack swing timers and weapon strikes)
      ▼
11. Call For Help (Low-health mobs pull nearby idle pack members)
      ▼
12. Retarget (Re-evaluate hostile threat tables against margins)
      ▼
13. Mob Decisions (Evaluate AI decision trees and spell choices)
      ▼
14. Advance Goals (Trigger A* pathfinding toward combat targets)
      │
      ▼
World(t+1)
```

### Pipeline Guarantees
- **Player Intent First**: Player commands land at phase 2 before the world reacts, guaranteeing zero latency between scheduled intent and sim update.
- **Unified World View**: Every entity acting within a tick observes the exact same post-damage, post-movement state.
- **Resource Exhaustion**: Mob resource regeneration is deliberately set to zero (`advanceResource`), ensuring boss and elite mana pools are finite and exhaustible (e.g., a Druid of the Fang can only cast 4 Healing Touches before depleting its pool).

---

## 2. Partitioned PRNG Streams (`splitmix64`)

Per [ADR-0002](../../docs/adr/0002-hand-rolled-prng-integer-core.md), random number generation is hand-rolled in [`Rng.fs`](../../src/WowRoguelike.Core/Rng.fs) based on Sebastiano Vigna’s `splitmix64`:

- **Decoupled Streams**:
  The initial run seed derives two isolated PRNG state streams:
  1. `GenerationStream` ($0$): Controls procedural floorplan layout and room placement.
  2. `CombatStream` ($1$): Controls dice rolls, dodge/block checks, critical hits, and damage variation.
- **Desynchronization Immunity**: Adding or altering dungeon layout logic cannot perturb combat rolls during encounters.
- **Cross-Platform Stability**: Avoids `System.Random`, ensuring simulations reproduce bit-identically across all OS platforms and future .NET runtime versions.

---

## 3. State Fingerprinting & Golden Replays

In [`Dump.fs`](../../src/WowRoguelike.Core/Dump.fs), the simulation provides canonical string serialization:

- **State Canonicalization**: `Dump.world` formats every entity's coordinates, health, resource, cast state, auras, cooldowns, threat table, and goal into a single canonical multi-line string.
- **Log Exclusion**: The event log is deliberately excluded from `Dump.world` because text wording changes must not register as behavioural regressions.
- **Bit-Exact Replays**: Two `World` instances with matching dump text are identical. This canonical dump forms the basis of automated golden replay verification across Git commits.

---

## 4. Related Pages

- [System Architecture & Core Invariants](../architecture/overview.md) — Architectural boundaries and F# compilation order.
- [Combat Mechanics, Threat & Abilities](combat-and-threat.md) — Ability resolution and threat math.
- [Dungeon Generation & Grid Pathfinding](dungeon-and-pathfinding.md) — Grid mechanics and movement logic.
