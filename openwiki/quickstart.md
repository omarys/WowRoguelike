---
type: Guide
title: Quickstart & Task Routing
description: Entry point and task-routing map for WowRoguelike developers and agents, covering environment setup, core commands, and topic navigation.
tags:
  - quickstart
  - guide
  - tasks
  - workflow
verified:
  - by: openwiki/0.7.2
    at: 2026-10-15T19:24:01.244Z
sources:
  - id: openwiki-source-39c3295efc089133e87a9c80
    resource: repo://CONTEXT.md
  - id: openwiki-source-72b5d686f860ea86c8592080
    resource: repo://mise.toml
  - id: openwiki-source-23775c3de52f3ab95a13cb8b
    resource: repo://README.md
generated: { by: "antigravity", at: "2026-10-15T19:24:01.244Z" }
---

# Quickstart & Task Routing

Welcome to `WowRoguelike`, a party-based tactical roguelike built on World of Warcraft dungeon mechanics in F# and MonoGame. This guide routes developers and agents to common developer workflows and system documentation.

---

## 1. Quick Environment Setup

The repository uses [mise](https://mise.jdx.dev/) to manage dev dependencies (.NET 10 SDK and `pre-commit`).

```bash
mise install            # Install .NET 10 SDK and pre-commit per mise.toml
mise run hooks          # Install pre-commit and pre-push Git hooks (run once)
mise run build          # Build the solution in Release configuration
mise run test           # Run unit and property test suites (xUnit + FsCheck)
mise run run            # Launch the game (MonoGame window; commands via stdin)
```

> [!IMPORTANT]
> Always verify that your environment resolves .NET 10. `mise.toml` pins `DOTNET_MULTILEVEL_LOOKUP=0` so invocations do not accidentally resolve system-wide .NET 8 installations.

---

## 2. Common Tasks & Commands

Key mise commands declared in [`mise.toml`](../mise.toml):

| Command | Action | Primary Output / Goal |
|---|---|---|
| `mise run build` | Compiles the solution in **Release** | Confirms compilation with zero warnings |
| `mise run test` | Runs the test suite | 75+ xUnit & FsCheck invariant assertions |
| `mise run runs` | Runs `sim runs 120 4000` | The 120-seed behavioral regression sweep |
| `mise run run` | Runs `src/WowRoguelike.Game` | Playable arena; enter stdin commands (e.g. `party`, `3 8 kick`) |
| `mise run dungeon` | Procedural dungeon generation | ASCII map printout (e.g. `mise run dungeon -- 7`) |
| `mise run story` | Wailing Caverns story runner | Ink narrative runner (pass `-i` for interactive) |
| `mise run bench` | Performance harness | Tick timing, search costs, and MinHeap benchmarks |
| `mise run diagnose` | Profiling diagnostics | Per-bucket tick breakdown and search-failure costs |
| `mise run hooks-run` | Runs all hooks locally | Validates formatting, compile, and tests against tree |

---

## 3. Documentation & Topic Routing

Navigate directly to deep system documentation based on your task:

```
openwiki/
├── quickstart.md                                (This page: routing & setup)
├── architecture/
│   └── overview.md                              (System boundaries, F# compile order, ADR-0001, ADR-0002)
├── concepts/
│   ├── simulation-core.md                       (20Hz tick pipeline, World state, SplitMix64 PRNG)
│   ├── combat-and-threat.md                     (Threat margins, mitigation, interrupts, Content.fs verify block)
│   └── dungeon-and-pathfinding.md               (Rooms-and-corridors, BFS connectivity, A* & MinHeap)
└── workflows/
    └── verification-and-benchmarking.md         (120-seed sweep fingerprint, Git hooks, Bench harness)
```

### Routing by Task Type

- **Working on Simulation, State, or PRNG?**
  Read [Simulation Core & Deterministic State](concepts/simulation-core.md) and [System Architecture](architecture/overview.md).
  - Understand the 20Hz tick advance order in `SimTick.step`.
  - Remember integer-only arithmetic and SplitMix64 stream partitioning.

- **Modifying Combat, Threat, Abilities, or Classes?**
  Read [Combat Mechanics, Threat & Abilities](concepts/combat-and-threat.md).
  - Note the 10% (melee) and 30% (ranged) threat margins, and 5.0x tank stance multiplier.
  - Review the `VERIFY` block in `Content.fs` before altering spell numbers.

- **Working on Maps, Generation, or Movement?**
  Read [Dungeon Generation & Grid Pathfinding](concepts/dungeon-and-pathfinding.md).
  - Note 2-tile wide corridors to avoid party deadlocks.
  - Understand corner-cutting restrictions in 8-way A*.

- **Running Benchmarks or Verifying Changes?**
  Read [Verification, Benchmarking & Harness Workflows](workflows/verification-and-benchmarking.md).
  - Always run `mise run runs` to verify that the 120-seed behavioral fingerprint has not drifted unintentionally.

---

## 4. Ubiquitous Vocabulary

Per [`CONTEXT.md`](../CONTEXT.md), avoid non-canonical terminology in code and documentation:

- Use **Run** (_avoid: game, session, attempt_).
- Use **Dungeon** (_avoid: map, level, instance_).
- Use **Mob** (_avoid: enemy, monster, NPC, creep_).
- Use **Pull** (_avoid: pack, group_).
- Use **Tick** (_avoid: frame, update, turn, step_).
- Use **Party** (_avoid: group, team, squad, roster_).
- Use **Command** (_avoid: input, action, intent, order_).
