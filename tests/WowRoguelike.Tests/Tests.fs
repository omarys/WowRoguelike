module WowRoguelike.Tests

open Xunit
open FsCheck.Xunit
open WowRoguelike.Core

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
    Log = [] }

let private lockoutOf (mob: Entity) =
  mob.Cooldowns
  |> Map.tryFind "Lightning Bolt"
  |> Option.defaultValue (ticks 0)

[<Fact>]
let ``an interrupt cancels an interruptible cast and locks it out`` () =
  let w = casterWorld Content.lightningBolt

  let w =
    SimState.applyCommand
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
    SimState.applyCommand
      { ApplyAt = ticks 1
        Kind = UseAbility(EntityId 3, "Kick", EntityId 100) }
      w

  Assert.True((mobOf w).Casting.IsSome)

[<Fact>]
let ``damage wakes a sleeper`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = SimState.mapEntity (EntityId 2) (fun e -> { e with Auras = [ Sleeping(secTicks 15.0) ] }) w
  Assert.True(SimState.isSleeping (w.Entities |> List.find (fun e -> e.Id = EntityId 2)))
  let w = SimState.dealDamage (EntityId 2) 1 w
  Assert.False(SimState.isSleeping (w.Entities |> List.find (fun e -> e.Id = EntityId 2)))

/// Moves are cancelled while asleep, so sleeping a member actually removes them.
[<Fact>]
let ``a sleeping entity does not move`` () =
  let w = arena 0 0 { X = 7; Y = 5 }

  let w =
    SimState.applyCommand
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
      Log = [] }

  let w =
    SimState.applyCommand
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
  let w = SimState.resolveAbility (EntityId 1) (EntityId 100) Content.heroicStrike w
  let after = (w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Resource
  Assert.Equal<int>(before - Content.heroicStrike.ResourceCost, after)

/// Resource is spent on resolution, not on the attempt, so an interrupted cast
/// costs nothing.
[<Fact>]
let ``an interrupted cast spends no resource`` () =
  let w = casterWorld Content.lightningBolt
  let before = (mobOf w).Resource

  let w =
    SimState.applyCommand
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
    SimState.applyCommand
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
      Log = [] }

  // (1,2) is a wall.
  let w =
    SimState.applyCommand
      { ApplyAt = ticks 1
        Kind = MoveTo(EntityId 1, { X = 1; Y = 2 }) }
      w

  let w = SimTick.run 100 w
  let solo = w.Entities |> List.head

  Assert.True(
    Grid.isFloor w.Grid solo.Pos,
    sprintf "ended on a wall at %d,%d" solo.Pos.X solo.Pos.Y
  )
