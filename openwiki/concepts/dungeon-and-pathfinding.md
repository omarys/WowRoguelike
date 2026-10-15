---
type: Concept
title: Dungeon Generation & Grid Pathfinding
description: Procedural rooms-and-corridors layout, BFS flood fill verification, 8-way A* pathfinding without corner-cutting, and MinHeap optimization.
tags:
  - dungeon
  - pathfinding
  - astar
  - algorithms
verified:
  - by: openwiki/0.7.2
    at: 2026-10-15T19:24:01.244Z
sources:
  - id: openwiki-source-701e394d161fcf23f08ae47e
    resource: repo://docs/HANDOFF.md
  - id: openwiki-source-32bfc334e6e4abce4d2b4083
    resource: repo://src/WowRoguelike.Core/Dungeon.fs
  - id: openwiki-source-1954d9af2790c6afd1910f2c
    resource: repo://src/WowRoguelike.Core/Heap.fs
  - id: openwiki-source-1c699e36740baa441ea21570
    resource: repo://src/WowRoguelike.Core/Path.fs
generated: { by: "antigravity", at: "2026-10-15T19:24:01.244Z" }
---

# Dungeon Generation & Grid Pathfinding

Dungeon layouts and grid movement in `WowRoguelike` are implemented in [`Dungeon.fs`](../../src/WowRoguelike.Core/Dungeon.fs), [`Path.fs`](../../src/WowRoguelike.Core/Path.fs), and [`Heap.fs`](../../src/WowRoguelike.Core/Heap.fs). They operate independently of entity state, relying purely on integer tile coordinates and grid passability functions.

---

## 1. Procedural Dungeon Generation

The dungeon generator creates structured floorplans tailored for tactical party combat:

- **Rooms and Corridors**:
  - Non-overlapping rectangular rooms (sizes 6–11 tiles) are scattered across a $46 \times 26$ grid.
  - Rectangular rooms are required because boss encounters (e.g., Lady Anacondra) are authored against rectangular room spaces (such as 4-corner spawn locations).
  - Corridors are carved at a **width of 2 tiles** (`corridorWidth = 2`). A 1-tile corridor would cause a 5-member party to deadlock upon meeting or passing one another.
- **Flood Fill & Connectivity**:
  - A 4-way breadth-first search (`floodFill`) measures shortest path distances across all reachable floor tiles.
  - Used for two critical purposes:
    1. **Boss Room Selection**: Selects the room farthest from the entrance by **true path length**, rather than Euclidean straight-line distance.
    2. **Connectivity Proof**: `isConnected` verifies that every single floor tile is reachable from the entrance. Dungeons are connected by construction, and this check asserts correctness rather than using a retry loop that could mask generation bugs.

---

## 2. Grid Pathfinding: A* and Dijkstra

Pathfinding runs over an 8-way grid with strictly integer movement weights:

- **Movement Costs**:
  - Orthogonal step: $10$ (tenths of a tile).
  - Diagonal step: $14$ ($\approx 10\sqrt{2}$).
  - Admissible Octile heuristic: $10 \cdot \max(dx, dy) + 4 \cdot \min(dx, dy)$. Integer-only arithmetic avoids floating-point discrepancies across platforms (per ADR-0002).
- **Corner-Cutting Prevention**:
  - To move diagonally into an adjacent tile, **both orthogonal neighbors must be open**.
  - Entities cannot clip through diagonally adjacent walls or slip between pillar seams.
- **Target Approach & Free Adjacent Tiles**:
  - Mobs path toward a free tile adjacent to their target rather than the target's exact occupied tile.
  - Pathing to an occupied tile combined with corner-cutting blocks created pathological search failures where A* expanded entire reachable components, causing severe frame drops. Aiming for adjacent approach tiles with `StallTicks` backoff solved this issue.

---

## 3. High-Performance Binary Min-Heap

Pathfinding performance relies on [`Heap.fs`](../../src/WowRoguelike.Core/Heap.fs):

- **Parallel Arrays**:
  - The hand-rolled `MinHeap<'p, 'v>` uses two parallel `ResizeArray` instances (`priorities` and `values`) rather than an array of `('p * 'v)` tuple objects.
  - Using tuple pairs caused reference allocations on every push and tuple copying on every swap, measuring **$12.9\times$ slower** than the .NET BCL priority queue at $n=100,000$. Parallel primitive arrays eliminated allocations.
- **Flat Array Search State**:
  - In `Path.fs`, closed and open tracking replaces persistent `Map`/`Set` data structures with flat integer arrays indexed by tile position ($y \cdot \text{width} + x$), dropping per-node expansion time by $1.5\times$.

---

## 4. Line of Sight (LOS)

- Symmetric Bresenham line-of-sight checks ensure visibility between casters and targets.
- Melee combat is exempt from line-of-sight requirements to prevent stuttering around doorways and corners when entities are already adjacent.

---

## 5. Related Pages

- [Simulation Core & Deterministic State](simulation-core.md) — How entities consume paths during movement updates.
- [System Architecture](../architecture/overview.md) — The F# module hierarchy and ADR constraints.
