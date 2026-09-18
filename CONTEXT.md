# WowRoguelike

A party-based run game set in dungeons shaped like World of Warcraft's: small groups of mobs, threat, interruptible casts, avoidable ground effects, and bosses with phases. Built in F# on MonoGame as a vehicle for learning functional programming, data structures and algorithms, and engine craft.

## Language

### Run structure

**Run**:
One attempt at a Dungeon, from entry to completion or party death. Nothing carries over between Runs except what the player learned.
_Avoid_: game, session, attempt

**Roguelike**:
The run structure of this game — procedurally varied Dungeons, permadeath, no carried-over power. It does *not* refer to turn-based play; combat here is real-time.
_Avoid_: roguelite, traditional roguelike

**Dungeon**:
The authored environment a Run takes place in, consisting of rooms connected by corridors, with a boss at the end.
_Avoid_: map, level, instance

### Dungeon vocabulary

**Mob**:
A hostile creature in a Dungeon that is not a boss.
_Avoid_: enemy, monster, NPC, creep

**Trash**:
Mobs in the aggregate, as the thing between you and the boss.
_Avoid_: minions

**Pull**:
The set of Mobs engaged as one unit, and the act of engaging them.
_Avoid_: encounter, pack, group

**Add**:
A Mob that joins a Pull after it has started, rather than one engaged at its start.
_Avoid_: reinforcement, spawn, extra

**Boss**:
A Mob that ends a Dungeon, with multiple Phases and mechanics that cannot be ignored.
_Avoid_: elite, final boss

**Phase**:
A distinct segment of a Boss fight with its own mechanic set, entered by a transition rather than by a timer alone.
_Avoid_: stage, mode

### Simulation

**Tick**:
One fixed advance of the simulation. The only unit of time the simulation knows about; everything else — cooldowns, DoTs, casts — is counted in Ticks.
_Avoid_: frame, update, turn, step

**Party**:
The player's characters. Every member is commanded directly by the player; there is no companion AI.
_Avoid_: group, team, squad, roster

**Command**:
A player's intent, directed at one Party member, valid at a given Tick. Commands are the only way the player affects the simulation.
_Avoid_: input, action, intent, order

**Core**:
The simulation. Pure, deterministic, and unaware that MonoGame exists.
_Avoid_: model, logic, engine, backend

**Shell**:
The MonoGame host around the Core: window, input, drawing, interpolation. Owns no game rules.
_Avoid_: client, view, frontend, renderer

### Combat

**Cast**:
An ability that resolves after a fixed number of Ticks rather than instantly, and can be reacted to during that window.
_Avoid_: channel, windup, delay

**Interruptible**:
A Cast that an interrupt ability can stop, cancelling it before it resolves.
_Avoid_: cancellable, breakable

**Resource**:
The pool an ability is paid for from when it resolves. Running out is what ends a fight; a caster with an empty pool can only melee.
_Avoid_: mana, energy, rage, power
