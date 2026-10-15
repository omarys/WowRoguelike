---
type: Workflow
title: Verification, Benchmarking & Harness Workflows
description: Verification strategy, 120-seed behavioural sweep fingerprint, pre-commit/pre-push hooks, and profiling harnesses.
tags:
  - testing
  - benchmarking
  - verification
  - mise
  - hooks
verified:
  - by: openwiki/0.7.2
    at: 2026-10-15T19:24:01.244Z
sources:
  - id: openwiki-source-4d1645cb6317345817452838
    resource: repo://.pre-commit-config.yaml
  - id: openwiki-source-701e394d161fcf23f08ae47e
    resource: repo://docs/HANDOFF.md
  - id: openwiki-source-72b5d686f860ea86c8592080
    resource: repo://mise.toml
  - id: openwiki-source-23775c3de52f3ab95a13cb8b
    resource: repo://README.md
  - id: openwiki-source-f71b153754be1241ff1b9faa
    resource: repo://src/WowRoguelike.Core/Bench.fs
generated: { by: "antigravity", at: "2026-10-15T19:24:01.244Z" }
---

# Verification, Benchmarking & Harness Workflows

In `WowRoguelike`, verification operates without a remote CI server; instead, local Git hooks, automated seed sweeps, and benchmark profiling serve as the definitive verification net.

---

## 1. The 120-Seed Behavioral Sweep Fingerprint

Unit tests prove that isolated invariants hold (e.g., threat math, no two living entities sharing a tile), but only the headless simulation sweep proves that **game behavior remains intact**.

The primary behavioral regression signal is driven by `WowRoguelike.Harness`:

```bash
mise run runs
# or directly:
dotnet run --project src/WowRoguelike.Harness -c Release -- runs 120 4000
```

This runs 120 distinct PRNG seeds up to 4,000 ticks each using `Bench.autoPilot` (a headless scripted party commander). The known-good output is treated as a behavioral fingerprint:

```
seeds=120 cleared=120 wiped=0 unresolved=0 median-ticks=1331 min=1116 max=2248
```

A refactor or optimization that alters these values indicates an unintended gameplay or pathfinding behavior change. For example, the separation of `Sim` into `SimState` and `SimTick` was proven to be a pure refactor by verifying an identical seed sweep before and after.

---

## 2. Git Hooks Pipeline (`.pre-commit-config.yaml`)

Hooks are structured in two deliberate tiers to keep developer feedback fast while preventing regressions from reaching remote branches:

| Hook Stage | Tasks | Target & Runtime |
|---|---|---|
| **Pre-Commit** | Formatting, trailing whitespace, large file guards ($\le 512\text{KB}$), XML verification, and `dotnet build` in Release mode. | Catches compile breaks and compiler warnings in $\sim 6\text{s}$. Fast enough that `--no-verify` is never needed. |
| **Pre-Push** | `dotnet test` (xUnit + FsCheck) and `sim runs 120 4000` (120-seed sweep). | Thorough behavioral check taking $\sim 1\text{m}$. Guarantees no broken state reaches remote repositories. |

### Installing & Executing Hooks
```bash
mise run hooks       # Installs both pre-commit and pre-push hooks (run once)
mise run hooks-run   # Executes all hooks against the entire codebase
```

---

## 3. Headless Harness Commands & Benchmarking

All benchmarking tasks must be executed under the **Release** configuration. Debug timings are discarded as meaningless noise.

Key tasks available via [`mise.toml`](../../mise.toml):

```bash
mise run build       # Build entire solution in Release configuration
mise run test        # Run xUnit and FsCheck test suites
mise run runs        # Execute the 120-seed behavioral regression sweep
mise run dungeon     # Generate a procedural floorplan and display as ASCII (e.g. mise run dungeon -- 7)
mise run bench       # Benchmark tick duration, A* search cost, and MinHeap vs BCL PriorityQueue
mise run diagnose    # Profile per-bucket tick cost and unreachable search failure costs
mise run story       # Run the ink narrative engine for Wailing Caverns (-i for interactive)
```

---

## 4. Benchmarking Methodology (`Bench.fs`)

- **Scripted Driver**: Benchmarks use `Bench.fightSplitCost` to drive the simulation with `Bench.autoPilot`. Undriven benchmarks merely measure the cost of an idle world collapsing into a corpse pile, which produces invalid performance data.
- **Critical Path Measurements**:
  - `MinHeap` vs BCL `PriorityQueue`: Asserts that parallel arrays eliminate reference allocation overhead on push/pop.
  - `fightSplitCost`: Separates the test harness harness/fixture overhead from pure `SimTick.step` execution cost.

---

## 5. Related Pages

- [System Architecture & Core Invariants](../architecture/overview.md) — Architectural rules and project boundaries.
- [Simulation Core & Deterministic State](../concepts/simulation-core.md) — The 20Hz tick loop that benchmarks exercise.
- [Quickstart & Task Routing](../quickstart.md) — Quick command reference.
