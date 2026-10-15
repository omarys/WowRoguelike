---
type: Architecture
title: System Architecture & Core Invariants
description: Architectural design, Core vs Shell boundaries, F# dependency ordering, and foundational ADR invariants in WowRoguelike.
tags:
  - architecture
  - simulation
  - fsharp
  - invariants
verified:
  - by: openwiki/0.7.2
    at: 2026-10-15T19:24:01.244Z
sources:
  - id: openwiki-source-8658b205553501450f042c01
    resource: repo://docs/adr/0001-fixed-tick-deterministic-simulation.md
  - id: openwiki-source-767b3e950fd5e6b0a1f59629
    resource: repo://docs/adr/0002-hand-rolled-prng-integer-core.md
  - id: openwiki-source-701e394d161fcf23f08ae47e
    resource: repo://docs/HANDOFF.md
  - id: openwiki-source-23775c3de52f3ab95a13cb8b
    resource: repo://README.md
  - id: openwiki-source-4abbd72e07e3b5008bc71ff7
    resource: repo://src/WowRoguelike.Core/Rng.fs
  - id: openwiki-source-5964c51368e1b01ef6851400
    resource: repo://src/WowRoguelike.Core/WowRoguelike.Core.fsproj
generated: { by: "antigravity", at: "2026-10-15T19:24:01.244Z" }
---

# System Architecture & Core Invariants

`WowRoguelike` is a party-based tactical roguelike built on World of Warcraft dungeon mechanics in F# and MonoGame. The project follows a strict architectural boundary where **functional programming is the spine**: purity, determinism, and testability govern the simulation core, while platform concerns remain strictly isolated in the shell.

---

## 1. System Decomposition

The repository is structured into distinct projects with unidirectional dependencies:

| Project | Role | Dependencies & Invariants |
|---|---|---|
| [`src/WowRoguelike.Core/`](../../src/WowRoguelike.Core/) | Pure deterministic simulation. | **MUST NOT reference MonoGame.** Integer-only arithmetic, no floating-point math, no real-time clock references. |
| [`src/WowRoguelike.Game/`](../../src/WowRoguelike.Game/) | MonoGame Shell. | References `Core`. Handles window creation, input parsing (keyboard & stdin), frame interpolation, primitive drawing, and variable timestep accumulation. Owns zero game rules. |
| [`src/WowRoguelike.Harness/`](../../src/WowRoguelike.Harness/) | Headless execution CLI (`sim`). | References `Core` directly with no MonoGame dependencies. Runs automated 120-seed regression sweeps, benchmark profiling, and ASCII dungeon generation. |
| [`src/WowRoguelike.Story/`](../../src/WowRoguelike.Story/) | Narrative engine runner. | Ink story integration and runner for Wailing Caverns narrative sections. |
| [`tests/WowRoguelike.Tests/`](../../tests/WowRoguelike.Tests/) | Verification suite. | xUnit unit tests and FsCheck property-based tests verifying deterministic invariants and game rules. |

---

## 2. F# Compilation Order as Dependency Graph

In F#, compilation order strictly defines the dependency graph: a source file may only reference symbols declared in files positioned above it in the project file. The compile order in [`WowRoguelike.Core.fsproj`](../../src/WowRoguelike.Core/WowRoguelike.Core.fsproj#L15-L28) is intentional and enforces architectural layering:

```
Domain.fs        -> Pure types (Entity, Stats, World, Aura DU, Command) with ZERO logic.
Rng.fs           -> Hand-rolled splitmix64 PRNG with partitioned streams.
Dungeon.fs       -> Rooms-and-corridors generation and BFS flood-fill connectivity.
Maps.fs          -> Canonical Wailing Caverns room grids.
Content.fs       -> Authoring party templates, mob kits, and World builders.
Heap.fs          -> Hand-rolled binary min-heap with parallel arrays.
Path.fs          -> A*, Dijkstra, line of sight, and corner-cutting avoidance.
SimState.fs      -> Immutable World queries and record rebuild helpers.
SimCombat.fs     -> Mitigation (Armor, Block, Dodge), threat, interrupts, and command resolution.
SimTick.fs       -> Movement pipeline, Tick advance, and event dispatch.
Dump.fs          -> World to canonical text serialization for bit-exact replay diffs.
Bench.fs         -> Scripted autopilot player, timing harness, and diagnostic profiler.
```

Because [`Domain.fs`](../../src/WowRoguelike.Core/Domain.fs) contains types and no game logic, circular or illegal dependency directions are impossible to express at compile time.

---

## 3. Foundational Invariants & ADRs

Two architectural decisions constrain all code in the simulation core:

### ADR-0001: Fixed-Tick Deterministic Simulation
Per [ADR-0001](../../docs/adr/0001-fixed-tick-deterministic-simulation.md):
- The simulation advances in discrete **Ticks** at a fixed rate of 20Hz (20 ticks per second) and never sees real time.
- State progression is a pure function: $\text{World}_{t+1} = \text{step}(\text{World}_t, \text{Commands}_t)$.
- Speed control, pause, and single-step debugging live entirely in the **Shell**, which uses an accumulator to invoke `SimTick.step` zero, one, or multiple times per display frame.
- If the hardware slows down, the simulation lags behind real time rather than skipping ticks, preserving absolute determinism and bit-identical replays.

### ADR-0002: Partitioned PRNG Streams & Integer-Only Arithmetic
Per [ADR-0002](../../docs/adr/0002-hand-rolled-prng-integer-core.md):
- The Core uses a hand-rolled `splitmix64` PRNG embedded inside `World`, avoiding `System.Random` whose implementation is unstable across .NET versions.
- PRNG seeds are split into independent, partitioned streams (e.g., generation vs combat) so that additional dungeon generation rolls cannot displace subsequent combat dice.
- All Core arithmetic is strictly integer: positions are grid coordinates, durations are integer Ticks, and percentages/rates are expressed in basis points (or scaled integers) with explicit rounding rules. Floating-point numbers are prohibited in `Core`.

---

## 4. Cross-System Navigation

- [Quickstart & Task Routing](../quickstart.md) — Workflow commands and development setup.
- [Simulation Core & Deterministic State](../concepts/simulation-core.md) — Deep dive into `SimTick`, `SimState`, and state updates.
- [Combat Mechanics, Threat & Abilities](../concepts/combat-and-threat.md) — Threat margins, interrupt lockout, and combat resolution rules.
