module WowRoguelike.Tests

open Xunit
open FsCheck.Xunit
open WowRoguelike.Core
open WowRoguelike.Story

// ===========================================================================
// Fixtures
// ===========================================================================

/// A 20x10 empty arena with one Mob (id 100) at 6,5 and two Party members.
/// Swing timers are set absurdly long so nothing swings during a threat test.
let private arena (tankThreat: int) (dpsThreat: int) (dpsPos: Pos) : World =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let tank =
    Content.hero 1 "Tank" Tank 1000 1000 slow 50000 [ Content.heroicStrike ] { X = 5; Y = 5 }

  let dps =
    Content.hero 2 "Dps" Dps 1000 1000 slow 10000 [] dpsPos

  let mob =
    Content.mob 100 "Mob" 10000 1000 (Some slow) [] None { X = 6; Y = 5 }

  let mob =
    { mob with
        Engaged = true
        Target = Some(EntityId 1)
        Threat = Map.ofList [ EntityId 1, tankThreat; EntityId 2, dpsThreat ] }

  { Tick = ticks 0
    Grid = Grid.create 20 10
    Entities = [ tank; dps; mob ]
    Pending = []
    Rng = Rng.streamsOf 1UL
    PartyBuffs = []
    Log = [] }

let private mobOf (w: World) =
  w.Entities |> List.find (fun e -> e.Id = EntityId 100)

// ===========================================================================
// Randomness (ADR-0002)
// ===========================================================================

[<Fact>]
let ``the same seed yields the same combat draws`` () =
  let draws (seed: uint64) =
    let mutable rng = Rng.streamsOf seed

    [ for _ in 1..10 ->
        let v, r = Rng.drawCombat 0 1000000 rng
        rng <- r
        v ]

  Assert.Equal<int list>(draws 42UL, draws 42UL)

[<Fact>]
let ``different seeds diverge`` () =
  let first (seed: uint64) =
    let mutable rng = Rng.streamsOf seed
    let v, _ = Rng.drawCombat 0 1000000 rng
    v

  Assert.NotEqual<int>(first 1UL, first 2UL)

/// The whole point of partitioning streams: adding a generation draw must not
/// shift the combat sequence.
[<Fact>]
let ``generation draws do not perturb the combat stream`` () =
  let combatOnly =
    Rng.streamsOf 7UL |> Rng.drawCombat 0 1000000 |> snd

  let afterGeneration =
    let rng = Rng.streamsOf 7UL
    let _, rng = Rng.drawGeneration 0 999 rng
    Rng.drawCombat 0 1000000 rng |> snd

  Assert.Equal<uint64>(combatOnly.Combat.Value, afterGeneration.Combat.Value)

/// Nor the other way round.
[<Fact>]
let ``combat draws do not perturb the generation stream`` () =
  let genOnly =
    Rng.streamsOf 7UL |> Rng.drawGeneration 0 1000000 |> snd

  let afterCombat =
    let rng = Rng.streamsOf 7UL
    let _, rng = Rng.drawCombat 0 999 rng
    Rng.drawGeneration 0 1000000 rng |> snd

  Assert.Equal<uint64>(genOnly.Generation.Value, afterCombat.Generation.Value)

[<Property>]
let ``rolls stay inside their inclusive range`` (lo: int) (span: int) =
  let lo = abs lo % 1000
  let hi = lo + (abs span % 1000)
  let mutable rng = Rng.streamsOf 5UL
  let mutable ok = true

  for _ in 1..50 do
    let v, r = Rng.drawCombat lo hi rng
    rng <- r
    if v < lo || v > hi then ok <- false

  ok

// ===========================================================================
// Threat — the 10% / 30% margins
// ===========================================================================

/// Anacondra's room makes the Tank's job real, so the margins have to hold.
[<Fact>]
let ``a challenger below the melee margin does not take aggro`` () =
  // 1099 vs 1000 is under 10%.
  let w = arena 1000 1099 { X = 7; Y = 5 }
  Assert.Equal<EntityId option>(Some(EntityId 1), SimState.resolveTarget (mobOf w) w)

[<Fact>]
let ``a challenger past the melee margin takes aggro`` () =
  // 1101 vs 1000 is over 10%, and 7,5 is adjacent to the Mob.
  let w = arena 1000 1101 { X = 7; Y = 5 }
  Assert.Equal<EntityId option>(Some(EntityId 2), SimState.resolveTarget (mobOf w) w)

[<Fact>]
let ``a challenger below the ranged margin does not take aggro`` () =
  // 1150 is over 10% but under 30%, and this DPS is far away.
  let w = arena 1000 1150 { X = 18; Y = 5 }
  Assert.Equal<EntityId option>(Some(EntityId 1), SimState.resolveTarget (mobOf w) w)

[<Fact>]
let ``a challenger past the ranged margin takes aggro`` () =
  let w = arena 1000 1301 { X = 18; Y = 5 }
  Assert.Equal<EntityId option>(Some(EntityId 2), SimState.resolveTarget (mobOf w) w)

/// The Tank stance is what makes the role work, and the margin is the reason
/// 5x threat is enough rather than merely necessary.
[<Fact>]
let ``the tank stance multiplies threat generation`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let tank = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
  let w = SimState.addThreat (EntityId 100) tank 10 w
  Assert.Equal<int>(50, SimState.threatOf (EntityId 1) (mobOf w))

[<Fact>]
let ``mobs never accumulate threat against each other`` () =
  let w = Bench.runEncounter 1UL 3000

  Assert.True(
    w.Entities
    |> List.filter (fun e -> e.Faction = Party)
    |> List.forall (fun e -> Map.isEmpty e.Threat)
  )

// ===========================================================================
// Interrupts, sleep, forms
// ===========================================================================

let private casterWorld (ability: Ability) : World =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let rogue =
    Content.hero 3 "Rogue" Dps 1000 1000 slow 10000 [ Content.kick ] { X = 5; Y = 5 }

  let mob =
    Content.mob 100 "Caster" 10000 1000 (Some slow) [ ability ] None { X = 6; Y = 5 }

  let mob =
    { mob with
        Engaged = true
        Target = Some(EntityId 3)
        Threat = Map.ofList [ EntityId 3, 1000 ]
        Casting =
          Some
            { Ability = ability
              Target = EntityId 3
              Remaining = secTicks 2.0 } }

  { Tick = ticks 0
    Grid = Grid.create 20 10
    Entities = [ rogue; mob ]
    Pending = []
    Rng = Rng.streamsOf 1UL
    PartyBuffs = []
    Log = [] }

let private lockoutOf (mob: Entity) =
  mob.Cooldowns
  |> Map.tryFind "Lightning Bolt"
  |> Option.defaultValue (ticks 0)

[<Fact>]
let ``an interrupt cancels an interruptible cast and locks it out`` () =
  let w = casterWorld Content.lightningBolt

  let w =
    SimCombat.applyCommand
      { ApplyAt = ticks 1
        Kind = UseAbility(EntityId 3, "Kick", EntityId 100) }
      w

  let mob = mobOf w
  Assert.True(mob.Casting.IsNone)
  Assert.Equal<int<tick>>(SimState.interruptLockout, lockoutOf mob)

[<Fact>]
let ``an interrupt cannot cancel a cast that is not interruptible`` () =
  let w = casterWorld Content.serpentForm

  let w =
    SimCombat.applyCommand
      { ApplyAt = ticks 1
        Kind = UseAbility(EntityId 3, "Kick", EntityId 100) }
      w

  Assert.True((mobOf w).Casting.IsSome)

[<Fact>]
let ``damage wakes a sleeper`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = SimState.mapEntity (EntityId 2) (fun e -> { e with Auras = [ Sleeping(secTicks 15.0) ] }) w
  Assert.True(SimState.isSleeping (w.Entities |> List.find (fun e -> e.Id = EntityId 2)))
  let w = SimCombat.dealDamage (EntityId 2) 1 w
  Assert.False(SimState.isSleeping (w.Entities |> List.find (fun e -> e.Id = EntityId 2)))

/// Moves are cancelled while asleep, so sleeping a member actually removes them.
[<Fact>]
let ``a sleeping entity does not move`` () =
  let w = arena 0 0 { X = 7; Y = 5 }

  let w =
    SimCombat.applyCommand
      { ApplyAt = ticks 1
        Kind = MoveTo(EntityId 2, { X = 18; Y = 5 }) }
      w

  let w = SimState.mapEntity (EntityId 2) (fun e -> { e with Auras = [ Sleeping(secTicks 15.0) ] }) w
  let before = (w.Entities |> List.find (fun e -> e.Id = EntityId 2)).Pos
  let w = SimTick.run 30 w
  let after = (w.Entities |> List.find (fun e -> e.Id = EntityId 2)).Pos
  Assert.Equal<Pos>(before, after)

[<Fact>]
let ``serpent form blocks casting`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = SimState.mapEntity (EntityId 1) (fun e -> { e with Auras = [ SerpentForm(secTicks 10.0, 25) ] }) w
  Assert.False(SimState.canCast (w.Entities |> List.find (fun e -> e.Id = EntityId 1)))

[<Fact>]
let ``serpent form adds physical damage to swings`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = SimState.mapEntity (EntityId 2) (fun e -> { e with Auras = [ SerpentForm(secTicks 10.0, 25) ] }) w
  Assert.Equal<int>(25, SimState.serpentBonus (w.Entities |> List.find (fun e -> e.Id = EntityId 2)))

/// The sourced rule is *any* hostile action, not only damage. Unreachable while
/// Druid's Slumber only ever targets the Party, but it becomes reachable the
/// moment a Mob can be slept.
[<Fact>]
let ``an interrupt wakes a sleeper`` () =
  let w =
    casterWorld Content.lightningBolt
    |> SimState.mapEntity (EntityId 100) (fun e ->
      { e with Auras = [ Sleeping(secTicks 15.0) ] })

  Assert.True(SimState.isSleeping (mobOf w))
  let w = SimCombat.resolveAbility (EntityId 3) (EntityId 100) Content.kick w
  Assert.False(SimState.isSleeping (mobOf w))

[<Fact>]
let ``a taunt wakes a sleeper`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 100) (fun e ->
      { e with Auras = [ Sleeping(secTicks 15.0) ] })

  Assert.True(SimState.isSleeping (mobOf w))
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.taunt w
  Assert.False(SimState.isSleeping (mobOf w))

/// Taunt is a shout, not a poke: every hostile in the taunter's range is forced
/// onto them. The clicked target is just how the shout is aimed.
[<Fact>]
let ``a taunt forces every hostile in range`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let m = mobOf w

  // Content.taunt has Range 8 from the Tank at 5,5: 6,6 is inside, 18,9 is not.
  let second = { m with Id = EntityId 101; Name = "Second"; Pos = { X = 6; Y = 6 } }
  let far = { m with Id = EntityId 102; Name = "Far"; Pos = { X = 18; Y = 9 } }

  let w = { w with Entities = w.Entities @ [ second; far ] }
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.taunt w

  let forced (id: EntityId) =
    w.Entities |> List.find (fun e -> e.Id = id) |> _.ForcedTarget

  Assert.Equal(Some(EntityId 1), forced (EntityId 100))
  Assert.Equal(Some(EntityId 1), forced (EntityId 101))
  Assert.Equal(None, forced (EntityId 102))

// ===========================================================================
// Determinism and golden replay (ADR-0001 / Q25a)
// ===========================================================================

let private record (seed: uint64) (n: int) : Command list list * World =
  let rec loop (w: World) (remaining: int) (acc: Command list list) =
    if remaining <= 0 then
      List.rev acc, w
    else
      let cmds = Bench.autoPilot w
      loop (SimTick.step cmds w) (remaining - 1) (cmds :: acc)

  loop (Content.gully seed) n []

[<Fact>]
let ``the same seed and commands produce the same state`` () =
  let _, a = record 3UL 400
  let _, b = record 3UL 400
  Assert.Equal(Dump.fingerprint a, Dump.fingerprint b)

/// The property the whole fixed-tick design was bought for: a recorded session
/// replays to a bit-identical World.
[<Fact>]
let ``a recorded session replays exactly`` () =
  let cmds, live = record 11UL 500
  let replayed = cmds |> List.fold (fun w c -> SimTick.step c w) (Content.gully 11UL)
  Assert.Equal(Dump.fingerprint live, Dump.fingerprint replayed)

[<Fact>]
let ``different seeds produce different worlds`` () =
  let _, a = record 1UL 200
  let _, b = record 2UL 200
  Assert.NotEqual<string>(Dump.fingerprint a, Dump.fingerprint b)

/// Anacondra takes one of four positions, so the first thing generation does is
/// visible in the very first tick.
[<Fact>]
let ``generation places anacondra in one of four spots`` () =
  let spots =
    [ 1UL..40UL ]
    |> List.map (fun s ->
      (Content.gully s).Entities
      |> List.find (fun e -> e.Name = "Lady Anacondra")
      |> fun e -> e.Pos)
    |> List.distinct

  Assert.True(List.length spots > 1)
  Assert.True(spots |> List.forall (fun p -> Content.anacondraSpawns |> List.contains p))

// ===========================================================================
// Invariants
// ===========================================================================

let private noSharedTiles (w: World) =
  let living = w.Entities |> List.filter SimState.alive
  let tiles = living |> List.map (fun e -> e.Pos)
  List.length tiles = (tiles |> List.distinct |> List.length)

/// This is the invariant that caught two entities stepping into one tile.
[<Property>]
let ``no two living entities ever share a tile`` (seed: int) =
  let seed = uint64 (abs seed)

  let rec loop (w: World) (n: int) =
    if n <= 0 then true
    elif not (noSharedTiles w) then false
    else loop (SimTick.step (Bench.autoPilot w) w) (n - 1)

  loop (Content.gully (seed + 1UL)) 400

/// KNOWN ISSUE, now fixed, kept as the regression guard. Before resource pools
/// existed, 7 of 120 seeds never resolved: the Party could not out-damage three
/// Mobs alternating Healing Touch (roughly 66 HPS against 45-50 party HPS) and
/// nothing in the model ever ran out.
///
/// Mobs do not regenerate resource, so their healing is finite. These are the
/// seeds that used to run forever.
[<Fact>]
let ``resource pools end the fights that used to stalemate`` () =
  for seed in [ 26UL; 36UL; 98UL ] do
    Assert.Equal(Outcome.EncounterCleared, SimState.outcome (Bench.runEncounter seed 5000))

/// A* is wired into movement, not merely available as a library: this order is
/// unreachable by the greedy stepper the previous slice shipped.
[<Fact>]
let ``a party member ordered across the cup room walks around the wall`` () =
  let w =
    { Tick = ticks 0
      Grid = Grid.ofRows Bench.concaveMap
      Entities =
        [ Content.hero
            1
            "Solo"
            Dps
            1000
            1000
            { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }
            10000
            []
            Bench.concaveStart ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Log = [] }

  let w =
    SimCombat.applyCommand
      { ApplyAt = ticks 1
        Kind = MoveTo(EntityId 1, Bench.concaveGoal) }
      w

  let w = SimTick.run 400 w
  Assert.Equal<Pos>(Bench.concaveGoal, (w.Entities |> List.head).Pos)

[<Property>]
let ``health stays within zero and maximum`` (seed: int) =
  let w = Bench.runEncounter (uint64 (abs seed) + 1UL) 600

  w.Entities
  |> List.forall (fun e -> e.Health >= 0 && e.Health <= e.MaxHealth)

/// Every Tick is integer, and time only moves forward.
[<Property>]
let ``the tick counter advances by exactly one per step`` (n: int) =
  let n = abs n % 200
  let w = SimTick.run n (Content.gully 1UL)
  w.Tick = ticks n

// ===========================================================================
// Resource
// ===========================================================================

[<Fact>]
let ``resolving an ability spends its resource`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let before = (w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Resource
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.heroicStrike w
  let after = (w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Resource
  Assert.Equal<int>(before - Content.heroicStrike.ResourceCost, after)

/// Resource is spent on resolution, not on the attempt, so an interrupted cast
/// costs nothing.
[<Fact>]
let ``an interrupted cast spends no resource`` () =
  let w = casterWorld Content.lightningBolt
  let before = (mobOf w).Resource

  let w =
    SimCombat.applyCommand
      { ApplyAt = ticks 1
        Kind = UseAbility(EntityId 3, "Kick", EntityId 100) }
      w

  Assert.Equal<int>(before, (mobOf w).Resource)
  Assert.True((mobOf w).Casting.IsNone)

[<Fact>]
let ``an ability that cannot be afforded is refused`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 1) (fun e -> { e with Resource = 0 })

  let w =
    SimCombat.applyCommand
      { ApplyAt = ticks 1
        Kind = UseAbility(EntityId 1, "Heroic Strike", EntityId 100) }
      w

  let mob = mobOf w
  Assert.Equal<int>(10000, mob.Health)
  Assert.Equal<int>(0, SimState.threatOf (EntityId 1) mob)

/// This is the whole reason a fight can end: mob healing is finite.
[<Fact>]
let ``mobs do not regenerate resource`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 100) (fun e -> { e with Resource = 50 })

  let w = SimTick.run 200 w
  Assert.Equal<int>(50, (mobOf w).Resource)

[<Fact>]
let ``party resource regenerates`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 1) (fun e -> { e with Resource = 0 })

  let w = SimTick.run 50 w
  let tank = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
  Assert.Equal<int>(50 * tank.ResourceRegenPerTick, tank.Resource)

// ===========================================================================
// A* and the heap (Q2c / Q26a)
// ===========================================================================

let private allowAll (_: Pos) = false
let private pathStart = { X = 1; Y = 1 }

[<Fact>]
let ``the heap pops in priority order`` () =
  let h = MinHeap<int, string>()

  for p in [ 5; 1; 9; 3; 7; 1; 0; 8 ] do
    h.Push(p, string p)

  let mutable out = []
  let mutable go = true

  while go do
    match h.Pop() with
    | Some(p, _) -> out <- p :: out
    | None -> go <- false

  Assert.Equal<int list>([ 0; 1; 1; 3; 5; 7; 8; 9 ], List.rev out)

[<Fact>]
let ``the heap is empty after popping everything`` () =
  let h = MinHeap<int, int>()

  for p in [ 4; 2; 8 ] do
    h.Push(p, p)

  let mutable go = true

  while go do
    match h.Pop() with
    | Some _ -> ()
    | None -> go <- false

  Assert.True(h.IsEmpty)
  Assert.Equal<int>(0, h.Count)

/// Never more than the true octile cost, or A* is no longer optimal.
[<Fact>]
let ``the octile heuristic is admissible`` () =
  for x in 0..8 do
    for y in 0..8 do
      let h = Path.octile { X = 0; Y = 0 } { X = x; Y = y }
      let truth = 14 * min x y + 10 * (max x y - min x y)
      Assert.True(h <= truth)

[<Fact>]
let ``a star returns a path that reaches the goal`` () =
  let g = Grid.create 10 10
  let r = Path.astar g allowAll pathStart { X = 9; Y = 9 }
  Assert.False(List.isEmpty r.Path)
  Assert.Equal<Pos>({ X = 9; Y = 9 }, List.last r.Path)

/// Every step is one tile and never enters a wall.
[<Fact>]
let ``a star paths are continuous and wall-free`` () =
  let g = Grid.ofRows Bench.concaveMap
  let r = Path.astar g allowAll Bench.concaveStart Bench.concaveGoal
  Assert.False(List.isEmpty r.Path)

  (Bench.concaveStart :: r.Path)
  |> List.pairwise
  |> List.iter (fun (a, b) ->
    Assert.Equal<int>(1, Pos.chebyshev a b)
    Assert.True(Grid.isFloor g b))

/// The demonstrable way the slice-1 stepper was wrong: it walks into the cup and
/// stops, because it never looks past the next tile.
[<Fact>]
let ``greedy deadlocks in the cup room where a star does not`` () =
  let g = Grid.ofRows Bench.concaveMap
  let greedy = Path.greedy g allowAll Bench.concaveStart Bench.concaveGoal
  let searched = Path.astar g allowAll Bench.concaveStart Bench.concaveGoal
  Assert.True(List.isEmpty greedy.Path)
  Assert.False(List.isEmpty searched.Path)

[<Fact>]
let ``a star returns nothing when the goal is walled off`` () =
  let g = Grid.create 9 9

  for y in 0..8 do
    g.Walls.[y * 9 + 4] <- true

  let r = Path.astar g allowAll { X = 0; Y = 4 } { X = 8; Y = 4 }
  Assert.True(List.isEmpty r.Path)

/// A mob paths at a tile somebody is standing on, so the goal must stay
/// enterable even when the blocker says otherwise.
[<Fact>]
let ``a star treats the goal tile as enterable`` () =
  let g = Grid.create 10 10
  let goal = { X = 5; Y = 5 }
  let blocked p = p = goal
  let r = Path.astar g blocked pathStart goal
  Assert.False(List.isEmpty r.Path)
  Assert.Equal<Pos>(goal, List.last r.Path)

/// A* is optimal, so it must agree with Dijkstra on cost wherever both reach.
[<Property>]
let ``a star and dijkstra agree on cost`` (seed: int) =
  let g = Bench.randomMap 20 14 25 (uint64 (abs seed) + 1UL)
  let goal = { X = 18; Y = 12 }
  let a = Path.astar g allowAll pathStart goal
  let d = Path.dijkstra g allowAll pathStart goal

  List.isEmpty a.Path = List.isEmpty d.Path
  && (List.isEmpty a.Path
      || Path.pathCost pathStart a.Path = Path.pathCost pathStart d.Path)

// ===========================================================================
// Dungeon generation (Q21)
// ===========================================================================

let private layoutOf (seed: int) (salt: uint64) = Dungeon.layout (uint64 (abs seed) + salt)

/// The layout is connected by construction, so this is the test that says the
/// construction is right.
[<Property>]
let ``a generated dungeon is fully connected`` (seed: int) =
  let plan = layoutOf seed 1UL
  Dungeon.isConnected plan.Grid plan.Entrance

[<Property>]
let ``generated rooms are large enough for an encounter`` (seed: int) =
  let plan = layoutOf seed 2UL

  plan.Rooms
  |> List.forall (fun r ->
    Room.width r >= Dungeon.minRoomSize && Room.height r >= Dungeon.minRoomSize)

/// Rooms must not touch, or the Dungeon is one open blob and the corridors that
/// make it read as a dungeon stop existing.
[<Property>]
let ``generated rooms do not touch each other`` (seed: int) =
  let plan = layoutOf seed 3UL

  plan.Rooms
  |> List.pairwise
  |> List.forall (fun (a, b) -> not (Room.tooClose 0 a b))

/// The boss is the farthest room by path length, not by straight line. That
/// distinction is the reason this uses a flood fill rather than arithmetic.
[<Property>]
let ``the boss room is the farthest room from the entrance`` (seed: int) =
  let plan = layoutOf seed 4UL
  let distances = Dungeon.floodFill plan.Grid plan.Entrance

  let distanceOf (r: Room) =
    let c = Room.centre r
    distances.[c.Y * plan.Grid.Width + c.X]

  distanceOf plan.BossRoom = (plan.Rooms |> List.map distanceOf |> List.max)

[<Fact>]
let ``a generated dungeon deploys a party and a fight on floor tiles`` () =
  let w = Content.dungeon 1UL

  Assert.Equal(5, w.Entities |> List.filter (fun e -> e.Faction = Party) |> List.length)

  Assert.True(
    w.Entities |> List.filter (fun e -> e.Faction = Hostile) |> List.length >= 6
  )

  for e in w.Entities do
    Assert.True(
      Grid.isFloor w.Grid e.Pos,
      sprintf "%s spawned in a wall at %d,%d" e.Name e.Pos.X e.Pos.Y
    )

  let tiles = w.Entities |> List.map (fun e -> e.Pos)
  Assert.Equal(List.length tiles, tiles |> List.distinct |> List.length)

/// The one-entity-per-tile invariant has to hold in a generated Dungeon too,
/// where corridors are two tiles wide and the crowding is real.
[<Property>]
let ``no two living entities share a tile in a generated dungeon`` (seed: int) =
  let rec loop (w: World) n =
    if n <= 0 then true
    elif not (noSharedTiles w) then false
    else loop (SimTick.step (Bench.autoPilot w) w) (n - 1)

  loop (Content.dungeon (uint64 (abs seed) + 1UL)) 300

/// Acceptance: the scripted party can finish a Run it has never seen before.
[<Fact>]
let ``the scripted party can clear a generated dungeon`` () =
  let rec loop (w: World) n =
    if n <= 0 || SimState.outcome w <> Running then w
    else loop (SimTick.step (Bench.autoPilot w) w) (n - 1)

  let w = loop (Content.dungeon 1UL) 6000
  Assert.Equal<Outcome>(EncounterCleared, SimState.outcome w)

/// The heuristic's whole value, asserted rather than admired.
[<Property>]
let ``a star expands no more nodes than dijkstra`` (seed: int) =
  let g = Bench.randomMap 20 14 25 (uint64 (abs seed) + 2UL)
  let goal = { X = 18; Y = 12 }
  let a = Path.astar g allowAll pathStart goal
  let d = Path.dijkstra g allowAll pathStart goal
  a.Expanded <= d.Expanded

// ===========================================================================
// Line of sight
// ===========================================================================

[<Fact>]
let ``line of sight is clear across an open room`` () =
  let g = Grid.ofRows [ "#####"; "#...#"; "#...#"; "#####" ]
  Assert.True(Path.lineOfSight g { X = 1; Y = 1 } { X = 3; Y = 2 })

[<Fact>]
let ``a wall directly between two tiles blocks sight`` () =
  let g =
    Grid.ofRows [ "#####"; "#...#"; "#.#.#"; "#...#"; "#####" ]

  Assert.False(Path.lineOfSight g { X = 1; Y = 2 } { X = 3; Y = 2 })

[<Fact>]
let ``a wall beside the line does not block sight`` () =
  let g =
    Grid.ofRows [ "#####"; "#...#"; "#.#.#"; "#...#"; "#####" ]

  Assert.True(Path.lineOfSight g { X = 1; Y = 1 } { X = 1; Y = 3 })

/// The real encounter's pillars sit at x=5,6 and x=15,16 on rows 3,4 and 8,9.
/// This is the bug that motivated the work: Druids lightning-bolting the Party
/// through them.
[<Fact>]
let ``the encounter pillars block across but not around`` () =
  let g = (Content.gully 1UL).Grid
  Assert.False(Path.lineOfSight g { X = 4; Y = 3 } { X = 7; Y = 3 })
  Assert.True(Path.lineOfSight g { X = 4; Y = 2 } { X = 7; Y = 2 })

/// Endpoints are never consulted, so a Mob standing in a doorway with walls on
/// both sides is not blind, and nothing on a tile ever blinds itself.
[<Fact>]
let ``line of sight does not consult the endpoints`` () =
  let g = Grid.ofRows [ "#.#" ]
  Assert.True(Path.lineOfSight g { X = 0; Y = 0 } { X = 2; Y = 0 })

/// The reason sight is tested in both directions rather than one: on this map
/// the forward line slips past a corner and the reverse line does not, so a
/// single-direction test would give whoever ran second an advantage.
[<Fact>]
let ``sight does not slip diagonally past a corner`` () =
  let g =
    Grid.ofRows [ "######"; "#.#..#"; "#....#"; "#....#"; "#....#"; "######" ]

  // From 1,1 the line steps diagonally into 2,2, whose upper neighbour is a wall.
  Assert.False(Path.lineOfSight g { X = 1; Y = 1 } { X = 4; Y = 4 })
  // One row lower there is no such corner.
  Assert.True(Path.lineOfSight g { X = 1; Y = 2 } { X = 4; Y = 4 })

[<Property>]
let ``line of sight is symmetric`` (seed: int) =
  let g = Bench.randomMap 20 14 25 (uint64 (abs seed) + 3UL)
  let floor = Grid.tiles g |> Seq.filter (Grid.isFloor g) |> Seq.toList
  let sample = [ for i in 0..12 -> List.item (i * 7 % List.length floor) floor ]

  sample
  |> List.forall (fun a ->
    sample |> List.forall (fun b -> Path.lineOfSight g a b = Path.lineOfSight g b a))

/// A blocked caster next to its target must still be able to act, which is what
/// the melee exemption in `SimState.canReach` rests on.
[<Property>]
let ``adjacent tiles always see each other`` (seed: int) =
  let g = Bench.randomMap 20 14 25 (uint64 (abs seed) + 4UL)

  Grid.tiles g
  |> Seq.filter (Grid.isFloor g)
  |> Seq.forall (fun a ->
    [ -1..1 ]
    |> List.forall (fun dx ->
      [ -1..1 ]
      |> List.forall (fun dy ->
        let b = Pos.move dx dy a
        (dx = 0 && dy = 0) || not (Grid.isFloor g b) || Path.lineOfSight g a b)))

/// The goal exemption is about occupancy, not about walls. Exempting the goal from
/// the floor check as well let a `move` order path a member into a pillar, which
/// breaks the one-entity-per-tile invariant the occupancy design rests on.
[<Fact>]
let ``a star refuses a goal that is a wall`` () =
  let g =
    Grid.ofRows [ "#####"; "#...#"; "#.#.#"; "#...#"; "#####" ]

  let wall = { X = 2; Y = 2 }
  Assert.True(Grid.isWall g wall)
  Assert.True(List.isEmpty (Path.astar g allowAll { X = 1; Y = 1 } wall).Path)

[<Fact>]
let ``a move order onto a wall never leaves the entity inside it`` () =
  let g =
    Grid.ofRows [ "#####"; "#...#"; "##.##"; "#...#"; "#####" ]

  let w =
    { Tick = ticks 0
      Grid = g
      Entities =
        [ Content.hero
            1
            "Solo"
            Dps
            1000
            1000
            { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }
            10000
            []
            { X = 1; Y = 1 } ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Log = [] }

  // (1,2) is a wall.
  let w =
    SimCombat.applyCommand
      { ApplyAt = ticks 1
        Kind = MoveTo(EntityId 1, { X = 1; Y = 2 }) }
      w

  let w = SimTick.run 100 w
  let solo = w.Entities |> List.head

  Assert.True(
    Grid.isFloor w.Grid solo.Pos,
    sprintf "ended on a wall at %d,%d" solo.Pos.X solo.Pos.Y
  )

// ===========================================================================
// Classes, stats, and the new mechanics (the five-class model)
// ===========================================================================

/// Sunder Armour reduces the target's armour, which lowers its physical
/// mitigation (Q3a/Q4b).
[<Fact>]
let ``sunder armor reduces physical mitigation`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 100) (fun e -> { e with Armor = 500 })

  // 500 armour against a level-21 attacker is 18.6% mitigation: 100 becomes 81.
  let before, _ = SimCombat.mitigatePhysical (EntityId 100) 100 w
  Assert.Equal(81, before)

  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.sunderArmor w
  // 500 - 180 = 320 armour -> 12.8% mitigation: 100 becomes 87.
  let after, _ = SimCombat.mitigatePhysical (EntityId 100) 100 w
  Assert.Equal(87, after)

/// Armour mitigates physical damage and leaves spells alone (Q2a).
[<Fact>]
let ``armor mitigates physical but not spell damage`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 100) (fun e -> { e with Armor = 500 })

  let physical, _ = SimCombat.mitigatePhysical (EntityId 100) 100 w
  Assert.Equal(81, physical)

  let before = (mobOf w).Health
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.fireball w
  let dealt = before - (mobOf w).Health
  // Fireball is a spell: full 146..195, not reduced to 118..159.
  Assert.True(dealt >= 146, sprintf "dealt %d" dealt)

/// Dodge is full avoidance (Q4a).
[<Fact>]
let ``dodge avoids a physical hit entirely`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 100) (fun e -> { e with DodgeChanceBp = 10000 })

  let amount, _ = SimCombat.mitigatePhysical (EntityId 100) 100 w
  Assert.Equal(0, amount)

/// Block subtracts a flat value (Q4a).
[<Fact>]
let ``block reduces a hit by the block value`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 100) (fun e ->
      { e with
          BlockChanceBp = 10000
          BlockValue = 25 })

  let amount, _ = SimCombat.mitigatePhysical (EntityId 100) 100 w
  Assert.Equal(75, amount)

/// Casters resist being interrupted by damage (Q5a): full resistance means the
/// pushback chance is zero.
[<Fact>]
let ``pushback resistance stops damage interrupting a cast`` () =
  let w =
    casterWorld Content.lightningBolt
    |> SimState.mapEntity (EntityId 100) (fun e -> { e with PushbackResistanceBp = 10000 })

  let w = SimCombat.dealDamage (EntityId 100) 50 w
  Assert.True((mobOf w).Casting.IsSome)

/// Crit and dodge come from Agility: roughly 10 Agility per point of crit and 20
/// per point of dodge at level 24, on top of the 5% every character starts with.
[<Fact>]
let ``agility drives crit and dodge`` () =
  let rogue = (Content.gully 1UL).Entities |> List.find (fun e -> e.Name = "Rogue")
  // Agility 100 + Mark of the Wild 4 = 104 -> 5% + 10.4% crit, 5% + 5.2% dodge.
  Assert.Equal(1540, rogue.CritChanceBp)
  Assert.Equal(1020, rogue.DodgeChanceBp)
  Assert.Equal(0, rogue.BackstabCritBonusBp)

/// Backstab is 150% weapon damage plus a flat bonus, scaled by Strength the way
/// a swing is. The crit half of the trait is pinned by the test above.
[<Fact>]
let ``backstab deals 150 percent of weapon damage plus a bonus`` () =
  let swing = { Min = 16; Max = 24; Ticks = ticks 100000; Range = 1 }

  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 1) (fun e ->
      { e with
          AutoAttack = Some swing
          MeleeDamageBonus = 18
          CritChanceBp = 0 })

  let before = (mobOf w).Health
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.backstab w
  let dealt = before - (mobOf w).Health

  // 16..24 x1.5 = 24..36, + 18 Strength + the 48 rank-3 bonus = 90..102, on an
  // unarmoured Mob that cannot dodge.
  Assert.True(dealt >= 90 && dealt <= 102, sprintf "dealt %d" dealt)

/// Armour classes give armour and nothing else, as asked.
[<Fact>]
let ``armor classes only provide armor`` () =
  let w = Content.gully 1UL
  let armorOf name = (w.Entities |> List.find (fun e -> e.Name = name)).Armor

  // Warrior (plate + trait) > Rogue (leather) > Mage (cloth).
  Assert.True(armorOf "Warrior" > armorOf "Rogue")
  Assert.True(armorOf "Rogue" > armorOf "Mage")

[<Fact>]
let ``stun prevents action and is not broken by damage`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.gouge w
  Assert.False(SimState.canAct (mobOf w))
  let w = SimCombat.dealDamage (EntityId 100) 10 w
  Assert.False(SimState.canAct (mobOf w))

[<Fact>]
let ``root prevents movement but not action`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.entanglingRoots w
  let mob = mobOf w
  Assert.False(SimState.canMove mob)
  Assert.True(SimState.canAct mob)

[<Fact>]
let ``polymorph breaks on damage`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.polymorph w
  Assert.False(SimState.canAct (mobOf w))
  let w = SimCombat.dealDamage (EntityId 100) 10 w
  Assert.True(SimState.canAct (mobOf w))

/// Rejuvenation is a heal-over-time, so it heals once a second — health is paid
/// out on the Ticks a periodic aura actually fires.
[<Fact>]
let ``a heal over time heals once a second`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = SimState.mapEntity (EntityId 1) (fun e -> { e with Health = 100 }) w
  let w = SimCombat.resolveAbility (EntityId 2) (EntityId 1) Content.rejuvenation w
  let w = SimTick.run 40 w
  let tank = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
  // 22 a second for 12 s: two seconds in, 100 + 44.
  Assert.Equal(144, tank.Health)

/// Arcane Explosion hits every hostile in radius and no ally (Q5a).
[<Fact>]
let ``area damage hits hostiles but not allies`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let before = (mobOf w).Health
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.arcaneExplosion w
  Assert.True((mobOf w).Health < before)
  let dps = w.Entities |> List.find (fun e -> e.Id = EntityId 2)
  Assert.Equal(1000, dps.Health)

/// A 100% crit chance forces every hit to 150% damage.
[<Fact>]
let ``a guaranteed crit deals 150 percent`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 1) (fun e -> { e with CritChanceBp = 10000 })

  let before = (mobOf w).Health
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.heroicStrike w
  let dealt = before - (mobOf w).Health
  // A 1..1 swing plus Heroic Strike's 44 is 45; a crit takes it to 67.
  Assert.Equal(67, dealt)

/// The party passives land on the pools: Arcane Intellect rank 2 (+7 Intellect),
/// Mark of the Wild rank 3 (+4 to every stat) and Power Word: Fortitude rank 3
/// (+20 Stamina).
[<Fact>]
let ``party buffs raise the mage's resource pool`` () =
  let mage = (Content.dungeon 1UL).Entities |> List.find (fun e -> e.Name = "Mage")
  // Intellect 131 + 7 + 4 = 142 -> 20 points at 1 mana, 122 at 15 = 1850, plus
  // the base 523 -> 2373.
  Assert.Equal(2373, mage.MaxResource)
  // Stamina 56 + 4 + 20 = 80 -> 20 points at 1 health, 60 at 10 = 620, plus the
  // base 288 -> 908.
  Assert.Equal(908, mage.MaxHealth)

[<Fact>]
let ``the wailing caverns story plays through all five encounters`` () =
  let runner = StoryRunner(StoryRunner.LoadJson "wailing_caverns.ink.json")
  let lines = ResizeArray<string>()
  let encounters = ResizeArray<string>()
  let mutable ended = false
  let mutable steps = 0

  while not ended && steps < 1000 do
    steps <- steps + 1

    for ev in runner.Continue() do
      match ev with
      | StoryEvent.Text t -> lines.Add t
      | StoryEvent.Encounter id -> encounters.Add id
      | StoryEvent.Choice _ -> runner.Choose 0
      | StoryEvent.QuestComplete _ -> ()
      | StoryEvent.End -> ended <- true

  Assert.True(ended, "the story should terminate")

  Assert.Equal(
    "lady_anacondra|lord_cobrahn|lord_pythas|lord_serpentis|mutanus",
    String.concat "|" encounters)

  let all = lines |> String.concat "\n"
  Assert.Contains("Naralex", all)
  Assert.Contains("Mutanus", all)

[<Fact>]
let ``every Wailing Caverns room is connected`` () =
  Assert.Equal(7, Maps.rooms.Length)

  for name, g in Maps.rooms do
    let floor = Grid.tiles g |> Seq.filter (Grid.isFloor g) |> Seq.toList
    Assert.False(List.isEmpty floor, sprintf "%s has no walkable floor" name)
    Assert.True(Dungeon.isConnected g (List.head floor), sprintf "%s is not connected" name)

[<Fact>]
let ``the stitched Wailing Caverns is connected and every entity stands on floor`` () =
  let w = Content.wailingCaverns 1UL
  let partyPos = (w.Entities |> List.find (fun e -> e.Faction = Party)).Pos
  Assert.True(Dungeon.isConnected w.Grid partyPos, "stitched dungeon should be connected")

  for e in w.Entities do
    Assert.True(Grid.isFloor w.Grid e.Pos, sprintf "%s stands on a wall" e.Name)

  let names = w.Entities |> List.map (fun e -> e.Name)
  Assert.Contains("Lady Anacondra", names)
  Assert.Contains("Lord Serpentis", names)
  Assert.Contains("Lord Cobrahn", names)
  Assert.Contains("Lord Pythas", names)
  Assert.Contains("Naralex", names)
  Assert.Contains("Mutanus the Devourer", names)

[<Fact>]
let ``heal abilities target every party member`` () =
  let targets = SimState.validTargets (EntityId 2) "Heal" (Content.gully 1UL)
  Assert.Equal(5, targets.Length)

  for t in targets do
    Assert.Equal(Party, t.Faction)

[<Fact>]
let ``damaging abilities target enemies within view`` () =
  let targets = SimState.validTargets (EntityId 4) "Fireball" (Content.gully 1UL)
  Assert.True(targets.Length > 0, "the mage should see at least one enemy")

  for t in targets do
    Assert.Equal(Hostile, t.Faction)

[<Fact>]
let ``an enemy behind a wall is not a valid damage target`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }
  let mage = Content.hero 1 "Mage" Dps 1000 1000 slow 10000 [ Content.fireball ] { X = 1; Y = 2 }
  let mob = Content.mob 100 "Mob" 1000 0 (Some slow) [] None { X = 3; Y = 2 }

  let w =
    { Tick = ticks 0
      Grid = Grid.ofRows [ "#####"; "#...#"; "#.#.#"; "#...#"; "#####" ]
      Entities = [ mage; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Log = [] }

  Assert.Empty(SimState.validTargets (EntityId 1) "Fireball" w)

[<Fact>]
let ``using an ability interrupts a move in progress`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let warrior =
    Content.hero 1 "Warrior" Tank 1000 1000 slow 50000 [ Content.heroicStrike ] { X = 1; Y = 1 }

  let mob = Content.mob 100 "Mob" 1000 0 (Some slow) [] None { X = 2; Y = 1 }

  let w =
    { Tick = ticks 0
      Grid = Grid.create 10 5
      Entities = [ warrior; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Log = [] }

  // Order a move: the warrior is now walking.
  let w =
    SimCombat.applyCommand { ApplyAt = ticks 0; Kind = MoveTo(EntityId 1, { X = 5; Y = 3 }) } w

  Assert.True((w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Goal.IsSome, "the move should set a goal")

  // Use a melee ability on the adjacent mob: it should stop the walk.
  let w =
    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(EntityId 1, "Heroic Strike", EntityId 100) }
      w

  let struck = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
  Assert.True(struck.Goal.IsNone, "the ability should clear the move goal")
  Assert.True(struck.Path.IsEmpty, "the ability should clear the path")

  // And the ability itself resolved: the mob took damage.
  Assert.True((w.Entities |> List.find (fun e -> e.Id = EntityId 100)).Health < 1000)

[<Fact>]
let ``a damage-over-time hurts the target each tick`` () =
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.shadowWordPain (arena 0 0 { X = 7; Y = 5 })
  Assert.True((mobOf w).Auras |> List.exists (fun a -> Aura.flatDamagePerTick a <> 0))
  let before = (mobOf w).Health
  let w = SimTick.run 20 w
  Assert.True((mobOf w).Health < before, "the DoT should keep ticking")

[<Fact>]
let ``a disarmed enemy does not swing`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }
  let fast = { Min = 30; Max = 30; Ticks = ticks 1; Range = 1 }
  let tank = Content.hero 1 "Tank" Tank 1000 1000 slow 50000 [ Content.heroicStrike ] { X = 5; Y = 5 }

  let mob =
    { Content.mob 100 "Mob" 1000 0 (Some fast) [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some(EntityId 1) }

  let baseWorld =
    { Tick = ticks 0
      Grid = Grid.create 20 10
      Entities = [ tank; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Log = [] }

  // Control: the mob swings and the tank is hurt.
  let control = SimTick.step [] baseWorld
  Assert.True((control.Entities |> List.find (fun e -> e.Id = EntityId 1)).Health < 1000)

  // Disarmed: the same tick leaves the tank untouched.
  let disarmed =
    SimState.mapEntity (EntityId 100) (fun e -> { e with Auras = [ Disarmed(secTicks 5.0) ] }) baseWorld

  let after = SimTick.step [] disarmed
  Assert.Equal(1000, (after.Entities |> List.find (fun e -> e.Id = EntityId 1)).Health)

[<Fact>]
let ``Charge moves the warrior beside the target`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }
  let warrior = Content.hero 1 "Warrior" Tank 1000 1000 slow 50000 [ Content.charge ] { X = 1; Y = 1 }
  let mob = Content.mob 100 "Mob" 1000 0 (Some slow) [] None { X = 6; Y = 1 }

  let w =
    { Tick = ticks 0
      Grid = Grid.create 10 5
      Entities = [ warrior; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Log = [] }

  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.charge w
  let moved = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
  Assert.True(Pos.chebyshev moved.Pos { X = 6; Y = 1 } <= 1, "the warrior should land beside the mob")

[<Fact>]
let ``the mage's auto-attack is a ranged wand`` () =
  let mage = (Content.gully 1UL).Entities |> List.find (fun e -> e.Name = "Mage")
  Assert.True(mage.AutoAttack.Value.Range > 1, "the wand reaches beyond melee")

[<Fact>]
let ``Battle Shout buffs the party`` () =
  Assert.Contains(BattleShout, Content.partyBuffs)
  let warrior = (Content.gully 1UL).Entities |> List.find (fun e -> e.Name = "Warrior")
  // Strength 87 + Mark of the Wild 4 = 91 at 2 attack power per point, plus Battle
  // Shout's 55 = 237 AP. Per swing that is 237/14 x 2.70 s = 45.
  Assert.Equal(45, warrior.MeleeDamageBonus)

[<Fact>]
let ``the warrior starts in front of the party`` () =
  let party = (Content.gully 1UL).Entities |> List.filter (fun e -> e.Faction = Party)
  let warriorX = (party |> List.find (fun e -> e.Name = "Warrior")).Pos.X
  Assert.Equal(party |> List.map (fun e -> e.Pos.X) |> List.max, warriorX)

// ===========================================================================
// The scripted party (PartyAi)
// ===========================================================================

/// Warrior, Priest and two mobs in an open room. The Warrior is on the first
/// mob; the second is one tile further away.
let private partyWorld (tankTarget: EntityId option) (auras: Aura list) (tankHealth: int) : World =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let tank =
    { Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor; Content.heroicStrike ] { X = 5; Y = 5 } with
        Target = tankTarget }

  let priest =
    Content.hero 2 "Priest" Healer 1000 1000 tap 10000 [ Content.heal; Content.renew ] { X = 5; Y = 6 }

  let near = { Content.mob 100 "Near" 1000 1000 (Some tap) [] None { X = 6; Y = 5 } with Engaged = true; Target = Some(EntityId 1) }
  let far = { Content.mob 101 "Far" 1000 1000 (Some tap) [] None { X = 5; Y = 4 } with Engaged = true; Target = Some(EntityId 1) }

  let tank = { tank with Health = tankHealth; Auras = auras }

  { Tick = ticks 0
    Grid = Grid.create 12 10
    Entities = [ tank; priest; near; far ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Log = [] }

let private abilitiesFor (actor: EntityId) (cmds: Command list) =
  cmds
  |> List.choose (fun c ->
    match c.Kind with
    | UseAbility(a, n, _) when a = actor -> Some n
    | _ -> None)

[<Fact>]
let ``the party hits what the warrior is on`` () =
  let w = partyWorld (Some(EntityId 101)) [] 1000
  let cmds = PartyAi.decide w
  let order = cmds |> List.pick (fun c -> match c.Kind with UseAbility(EntityId 1, n, t) -> Some(n, t) | _ -> None)
  Assert.Equal<(string * EntityId)>(("Sunder Armor", EntityId 101), order)

/// Armour already stripped off a target is damage already paid for, so it
/// outranks whatever the Warrior happens to be facing.
[<Fact>]
let ``a sundered target outranks the warrior's current target`` () =
  let w =
    partyWorld (Some(EntityId 100)) [] 1000
    |> SimState.mapEntity (EntityId 101) (fun e -> { e with Auras = [ Sundered(secTicks 20.0, 180) ] })

  Assert.Equal<EntityId option>(Some(EntityId 101), PartyAi.strikeTarget (SimState.aliveHostiles w) (Some(EntityId 100)) |> Option.map (fun e -> e.Id))

/// A heal-over-time with ten seconds to run is worth 120 more health: casting it
/// again is mana set on fire, so the healer waits.
[<Fact>]
let ``the healer does not re-apply a heal-over-time that still has time left`` () =
  let w = partyWorld None [ Rejuvenation(secTicks 10.0, 12) ] 700
  Assert.Empty(abilitiesFor (EntityId 2) (PartyAi.decide w))

[<Fact>]
let ``the healer refreshes the heal-over-time as it lapses`` () =
  let w = partyWorld None [ Rejuvenation(secTicks 1.0, 12) ] 700
  Assert.Contains("Renew", abilitiesFor (EntityId 2) (PartyAi.decide w))

/// Three seconds of cast that starts when the bar is empty has started too late.
[<Fact>]
let ``the healer starts a direct heal once the deficit outruns the heals in flight`` () =
  let w = partyWorld None [ Rejuvenation(secTicks 10.0, 12) ] 400
  Assert.Contains("Heal", abilitiesFor (EntityId 2) (PartyAi.decide w))

/// The player's order is the last word: a member the player just commanded gets
/// no scripted second order in the same Tick.
[<Fact>]
let ``the script leaves a member the player already commanded alone`` () =
  let w = partyWorld None [] 400
  let mine = [ { ApplyAt = ticks 1; Kind = UseAbility(EntityId 2, "Heal", EntityId 1) } ]
  let cmds = PartyAi.supplement mine w

  let priestOrders =
    cmds
    |> List.filter (fun c ->
      match c.Kind with
      | UseAbility(EntityId 2, _, _) -> true
      | _ -> false)

  Assert.Equal<int>(1, List.length priestOrders)

[<Fact>]
let ``aura labels show a readable name, side and clock`` () =
  let sunder = Sundered(ticks 245, 20)
  Assert.Equal("SUNDER", Aura.label sunder)
  Assert.False(Aura.isHelpful sunder)
  Assert.Equal(245, int (Aura.remaining sunder))
  // 245 Ticks is 12.25s at 20Hz: rounded up, so a live aura never reads 0s.
  Assert.Equal(13, Aura.remainingSeconds sunder)
  Assert.True(Aura.isHelpful (Rejuvenation(ticks 40, 5)))
  // Exactly one second left is "1s", not "2s": the round-up is for leftovers.
  Assert.Equal(1, Aura.remainingSeconds (SerpentForm(ticks 20, 4)))
  Assert.Equal(2, Aura.remainingSeconds (SerpentForm(ticks 21, 4)))
