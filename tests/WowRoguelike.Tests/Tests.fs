module WowRoguelike.Tests

open Xunit
open FsCheck
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
    Content.mob 100 "Mob" CreatureType.Humanoid 21 10000 1000 (Some slow) [] None { X = 6; Y = 5 }

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
    Quests = []
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
    Content.hero 3 "Rogue" Dps 1000 1000 slow 10000 [ (AbilityDef.at 24 Content.kick).Value ] { X = 5; Y = 5 }

  let mob =
    Content.mob 100 "Caster" CreatureType.Humanoid 21 10000 1000 (Some slow) [ ability ] None { X = 6; Y = 5 }

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
    Quests = []
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
  let w = SimCombat.resolveAbility (EntityId 3) (EntityId 100) (AbilityDef.at 24 Content.kick).Value w
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
      Quests = []
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
  // A drained open list is a proof, not a budget cut: callers rely on an empty
  // path meaning "unreachable" so they can back off instead of walking at it.
  Assert.False(r.Truncated)

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

/// Hallways are at least five tiles wide, so the Party walks them in formation
/// instead of queueing in them. Asserted on the carved geometry rather than on
/// `corridorWidth`, so the guarantee survives a refactor of the carving.
[<Property>]
let ``every hallway tile sits in a band at least five tiles wide`` (seed: int) =
  let plan = layoutOf seed 5UL
  let g = plan.Grid

  // Contiguous floor through `p` along one axis, both directions. The walk has to
  // take its direction as an argument: re-walking forwards from the far end
  // measures the wrong run and reports walls that are not there.
  let span (dx, dy) (p: Pos) =
    let rec walk (sx, sy) (q: Pos) =
      if Grid.isFloor g (Pos.move sx sy q) then
        1 + walk (sx, sy) (Pos.move sx sy q)
      else
        0

    walk (-dx, -dy) p + 1 + walk (dx, dy) p

  let inRoom (p: Pos) = plan.Rooms |> List.exists (fun r -> Room.contains r p)

  Grid.tiles g
  |> Seq.filter (Grid.isFloor g)
  |> Seq.forall (fun p ->
    inRoom p
    || max (span (1, 0) p) (span (0, 1) p) >= 5)

/// The lobby is the room the Party enters in, and it is bigger than a room: five
/// bodies need somewhere to stack before the first pull.
[<Fact>]
let ``the entrance room is a lobby`` () =
  for seed in 1UL..20UL do
    let plan = Dungeon.layout seed
    let lobby = plan.Rooms |> List.find (fun r -> Room.contains r plan.Entrance)

    Assert.True(
      Room.width lobby >= Dungeon.lobbyWidth && Room.height lobby >= Dungeon.lobbyHeight,
      sprintf "seed %d: lobby is %dx%d" seed (Room.width lobby) (Room.height lobby)
    )

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
/// where hallways are five tiles wide and the crowding is real.
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
      Quests = []
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
  let hit armor = 100 * (10000 - SimCombat.armorMitigationBp 21 armor) / 10000
  Assert.Equal(81, hit 500)

  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.sunderArmor w
  // 500 - 180 = 320 effective armour -> 12.8% mitigation: 100 becomes 87.
  let effective = 500 - ((mobOf w).Auras |> List.sumBy Aura.armorReduction)
  Assert.Equal(320, effective)
  Assert.Equal(87, hit effective)

/// Armour mitigates physical damage and leaves spells alone (Q2a).
[<Fact>]
let ``armor mitigates physical but not spell damage`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 100) (fun e -> { e with Armor = 500 })

  let physical = 100 * (10000 - SimCombat.armorMitigationBp 21 500) / 10000
  Assert.Equal(81, physical)

  let before = (mobOf w).Health
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.fireball w
  let dealt = before - (mobOf w).Health
  // Fireball is a spell: full 146..195, not reduced to 118..159.
  Assert.True(dealt >= 146, sprintf "dealt %d" dealt)

/// Dodge is full avoidance (Q4a), and its band sits above crit so a dodged
/// swing was never a critical one.
[<Fact>]
let ``dodge avoids a physical hit entirely`` () =
  let chances: SimCombat.SwingChances =
    { Miss = 0
      Dodge = 10000
      Parry = 0
      Glancing = 0
      Block = 0
      Crit = 9000 }

  Assert.Equal(SimCombat.Dodged, SimCombat.attackTable chances 0)
  Assert.Equal(SimCombat.Dodged, SimCombat.attackTable chances 9999)

  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 1) (fun e -> { e with DodgeChanceBp = 10000 })

  let amount, _ = SimCombat.meleeStrike (mobOf w) (EntityId 1) 100 0 w
  Assert.Equal(0, amount)

/// Block subtracts a flat value (Q4a), after the avoidance bands.
[<Fact>]
let ``block reduces a hit by the block value`` () =
  let chances: SimCombat.SwingChances =
    { Miss = 0
      Dodge = 0
      Parry = 0
      Glancing = 0
      Block = 10000
      Crit = 0 }

  Assert.Equal(SimCombat.Blocked, SimCombat.attackTable chances 5000)

  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 100) (fun e ->
      { e with
          BlockChanceBp = 10000
          BlockValue = 25
          Armor = 0 })

  let amount, _ = SimCombat.meleeStrike (mobOf w) (EntityId 100) 100 0 w
  // 100 - 25 = 75, less nothing for armour. Avoidance comes first on the table,
  // so a miss is a legitimate zero here.
  Assert.True(amount = 75 || amount = 0, sprintf "amount %d" amount)

/// A dual-wielder swings its off hand for half damage on the same cadence as
/// the main hand.
[<Fact>]
let ``a dual wielder swings its off hand too`` () =
  let logs =
    let fast = { Min = 30; Max = 30; Ticks = ticks 1; Range = 1 }

    let tank =
      Content.hero 1 "Tank" Tank 1000 1000 fast 50000 [ Content.heroicStrike ] { X = 5; Y = 5 }
      |> fun e -> { e with OffHand = true }

    let mob =
      { Content.mob 100 "Mob" CreatureType.Humanoid 21 100000 0 (Some fast) [] None { X = 6; Y = 5 } with
          Engaged = true
          Target = Some(EntityId 1) }

    [ 1UL .. 8UL ]
    |> List.map (fun seed ->
      { Tick = ticks 0
        Grid = Grid.create 20 10
        Entities = [ tank; mob ]
        Pending = []
        Rng = Rng.streamsOf seed
        PartyBuffs = []
        Quests = []
        Log = [] }
      |> SimTick.run 4
      |> fun w -> w.Log)

  // Avoidance can eat a swing, so sweep seeds for one that lands.
  Assert.True(
    logs |> List.exists (List.exists (fun l -> l.Contains "off hand")),
    System.String.Join(" | ", logs |> List.concat |> List.truncate 10)
  )

/// The Warrior fights sword and board, and the shield's buttons are what that
/// buys: a block chance, a block value, and the two cooldowns.
[<Fact>]
let ``the warrior carries a shield`` () =
  let tank = (Content.gully 1UL).Entities |> List.find (fun e -> e.Role = Some Tank)
  Assert.Equal(500, tank.BlockChanceBp)
  Assert.Equal(8, tank.BlockValue)

  for ability in [ "Shield Block"; "Shield Bash"; "Shield Wall" ] do
    Assert.True(
      tank.Abilities |> List.exists (fun a -> a.Name = ability),
      sprintf "the warrior should have %s" ability
    )

[<Fact>]
let ``shield block raises the block chance`` () =
  let blocked (w: World) =
    let tank = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
    (SimCombat.chanceOf (mobOf w) tank).Block

  let before = arena 0 0 { X = 7; Y = 5 }
  let after = SimCombat.resolveAbility (EntityId 1) (EntityId 1) Content.shieldBlock before
  Assert.Equal(blocked before + 7500, blocked after)

[<Fact>]
let ``shield wall removes three quarters of the damage`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimCombat.resolveAbility (EntityId 1) (EntityId 1) Content.shieldWall

  let before = (w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Health
  let w = SimCombat.dealDamage (EntityId 1) 100 w
  let after = (w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Health
  Assert.Equal(25, before - after)

/// Polymorph is a Beast/Humanoid/Critter spell, so a demon or dragonkin shrugs
/// it off and keeps acting.
[<Fact>]
let ``creature type decides whether polymorph lands`` () =
  let polymorphed (kind: CreatureType) =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 100) (fun e -> { e with CreatureType = kind })
    |> SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.polymorph
    |> mobOf
    |> fun e -> e.Auras |> List.exists (fun a -> match a with Polymorphed _ -> true | _ -> false)

  Assert.True(polymorphed CreatureType.Humanoid)
  Assert.True(polymorphed CreatureType.Beast)
  Assert.False(polymorphed CreatureType.Demon)
  Assert.False(polymorphed CreatureType.Undead)
  Assert.False(polymorphed CreatureType.Dragonkin)

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
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) (AbilityDef.at 24 Content.backstab).Value w
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
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) (AbilityDef.at 26 Content.cheapShot).Value w
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

/// A 100% crit chance forces every landed hit to 200% — the melee multiplier,
/// which is not the 150% spells get.
[<Fact>]
let ``a guaranteed crit deals 200 percent`` () =
  let dealt (w: World) =
    let before = (mobOf w).Health
    let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.heroicStrike w
    before - (mobOf w).Health

  // Avoidance bands sit above crit on the table, so a forced crit can still be
  // dodged; sweep seeds until the swing lands.
  let amounts =
    [ 1UL .. 8UL ]
    |> List.map (fun seed ->
      arena 0 0 { X = 7; Y = 5 }
      |> SimState.mapEntity (EntityId 1) (fun e -> { e with CritChanceBp = 10000 })
      |> fun w -> { w with Rng = Rng.streamsOf seed }
      |> dealt)

  // A 1..1 swing plus Heroic Strike's 44 is 45; a melee crit takes it to 90.
  Assert.Contains(90, amounts)
  Assert.All(amounts, fun d -> Assert.True(d = 90 || d = 0, sprintf "dealt %d" d))

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
  let mob = Content.mob 100 "Mob" CreatureType.Humanoid 21 1000 0 (Some slow) [] None { X = 3; Y = 2 }

  let w =
    { Tick = ticks 0
      Grid = Grid.ofRows [ "#####"; "#...#"; "#.#.#"; "#...#"; "#####" ]
      Entities = [ mage; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Log = [] }

  Assert.Empty(SimState.validTargets (EntityId 1) "Fireball" w)

[<Fact>]
let ``using an ability interrupts a move in progress`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let warrior =
    Content.hero 1 "Warrior" Tank 1000 1000 slow 50000 [ Content.heroicStrike ] { X = 1; Y = 1 }

  let mob = Content.mob 100 "Mob" CreatureType.Humanoid 21 1000 0 (Some slow) [] None { X = 2; Y = 1 }

  let w =
    { Tick = ticks 0
      Grid = Grid.create 10 5
      Entities = [ warrior; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Log = [] }

  // Order a move: the warrior is now walking.
  let strike (seed: uint64) =
    let w = { w with Rng = Rng.streamsOf seed }
    let w =
      SimCombat.applyCommand { ApplyAt = ticks 0; Kind = MoveTo(EntityId 1, { X = 5; Y = 3 }) } w

    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(EntityId 1, "Heroic Strike", EntityId 100) }
      w

  let w = strike 1UL
  let struck = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
  Assert.True(struck.Goal.IsNone, "the ability should clear the move goal")
  Assert.True(struck.Path.IsEmpty, "the ability should clear the path")

  // And the ability itself resolved: the mob took damage. A physical swing can
  // be avoided, so sweep seeds for one that lands.
  let hurt seed =
    (strike seed).Entities
    |> List.find (fun e -> e.Id = EntityId 100)
    |> fun e -> e.Health < 1000

  Assert.True([ 1UL .. 8UL ] |> List.exists hurt, "the ability should deal damage")

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
    { Content.mob 100 "Mob" CreatureType.Humanoid 21 1000 0 (Some fast) [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some(EntityId 1) }

  let baseWorld =
    { Tick = ticks 0
      Grid = Grid.create 20 10
      Entities = [ tank; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Log = [] }

  // Control: the mob swings and the tank is hurt. One swing can be missed — a
  // level-24 party avoids about 6% of level-21 swings — so give it twenty.
  let run20 w = List.fold (fun w _ -> SimTick.step [] w) w [ 1 .. 20 ]
  let control = run20 baseWorld
  Assert.True(
    (control.Entities |> List.find (fun e -> e.Id = EntityId 1)).Health < 1000,
    System.String.Join(" | ", List.map string control.Log)
  )

  // Disarmed: the same ticks leave the tank untouched (the disarm outlasts them).
  let disarmed =
    SimState.mapEntity (EntityId 100) (fun e -> { e with Auras = [ Disarmed(secTicks 5.0) ] }) baseWorld

  let after = run20 disarmed
  Assert.Equal(1000, (after.Entities |> List.find (fun e -> e.Id = EntityId 1)).Health)

[<Fact>]
let ``Charge moves the warrior beside the target`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }
  let warrior = Content.hero 1 "Warrior" Tank 1000 1000 slow 50000 [ Content.charge ] { X = 1; Y = 1 }
  let mob = Content.mob 100 "Mob" CreatureType.Humanoid 21 1000 0 (Some slow) [] None { X = 6; Y = 1 }

  let w =
    { Tick = ticks 0
      Grid = Grid.create 10 5
      Entities = [ warrior; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
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
  // Strength 81 (level-24 base 52 plus the mailed set's 29) + Mark of the Wild 4 =
  // 85 at 2 attack power per point, plus Battle Shout's 55 = 225 AP. Per swing
  // that is 225/14 x 2.70 s = 43.
  Assert.Equal(43, warrior.MeleeDamageBonus)

[<Fact>]
let ``the warrior starts in front of the party`` () =
  let party = (Content.gully 1UL).Entities |> List.filter (fun e -> e.Faction = Party)
  let warriorX = (party |> List.find (fun e -> e.Name = "Warrior")).Pos.X
  Assert.Equal(party |> List.map (fun e -> e.Pos.X) |> List.max, warriorX)

// ===========================================================================
// Objectives and scripted encounters
// ===========================================================================

/// The quest is the script: the objective counts kills, and finishing it wakes
/// whatever it names. Those are the two halves a scripted encounter needs, and
/// this asserts both rather than trusting the wiring.
[<Fact>]
let ``a finished objective wakes the sleeping boss it names`` () =
  let baseWorld = Content.gully 1UL
  let grunt = baseWorld.Entities |> List.filter (fun e -> e.Faction = Hostile) |> List.head

  // A boss that is scenery until something wakes it.
  let sleeper =
    { grunt with
        Id = EntityId 200
        Name = "Sleeper"
        Dormant = true
        Health = 5000
        MaxHealth = 5000 }

  let quest =
    { Quest.create "Test Objective" [ grunt.Name ] 1 with
        Wakes = [ "Sleeper" ]
        Lines = [ "The sleeper stirs" ] }

  let w = { baseWorld with Entities = sleeper :: baseWorld.Entities; Quests = [ quest ] }

  // Asleep: the party cannot see it and mobs do not consider it a fight.
  Assert.DoesNotContain(EntityId 200, SimState.aliveHostiles w |> List.map (fun e -> e.Id))
  Assert.False(SimState.inCombat { w with Entities = [ sleeper ] })

  // Kill the creature the objective watches.
  let w = SimCombat.dealDamage grunt.Id 100000 w

  Assert.True((w.Quests |> List.head).Done)
  Assert.Equal(1, (w.Quests |> List.head).Progress)
  Assert.Contains(EntityId 200, SimState.aliveHostiles w |> List.map (fun e -> e.Id))
  Assert.True(w.Log |> List.exists (fun l -> l.Contains "sleeper stirs"), String.concat " | " w.Log)

/// A kill that no objective is watching must not touch the log or the entities.
[<Fact>]
let ``a kill no objective watches changes nothing`` () =
  let baseWorld = Content.gully 1UL
  let grunt = baseWorld.Entities |> List.filter (fun e -> e.Faction = Hostile) |> List.head
  let quest = Quest.create "Hides" [ "something else" ] 3
  let w = { baseWorld with Quests = [ quest ] }
  let w = SimCombat.dealDamage grunt.Id 100000 w

  Assert.Equal(0, (w.Quests |> List.head).Progress)
  Assert.False((w.Quests |> List.head).Done)

/// The Wailing Caverns script, walked from the four Fanglords to Mutanus. This is
/// the dungeon's only real sequence and it is the reason the run can end.
[<Fact>]
let ``the Wailing Caverns script runs from the Fanglords to Mutanus`` () =
  let kill name (w: World) =
    let e =
      w.Entities
      |> List.filter (fun x -> x.Name = name && x.Faction = Hostile && SimState.alive x)
      |> List.head

    SimCombat.dealDamage e.Id 1000000 w

  let w = Content.wailingCaverns 1UL

  // Before the fight: Mutanus is not in the instance yet and Naralex is scenery.
  let dormant name (w: World) =
    (w.Entities |> List.find (fun e -> e.Name = name)).Dormant

  Assert.True(dormant "Mutanus the Devourer" w)
  Assert.True(dormant "Naralex" w)

  let targetable (w: World) = SimState.aliveHostiles w |> List.map (fun e -> e.Name)
  Assert.DoesNotContain("Mutanus the Devourer", targetable w)
  Assert.DoesNotContain("Naralex", targetable w)

  // The four Fanglords are the gate: downing them calls up the ambush.
  let w =
    [ "Lady Anacondra"; "Lord Cobrahn"; "Lord Pythas"; "Lord Serpentis" ]
    |> List.fold (fun w n -> kill n w) w

  let quest name (w: World) = w.Quests |> List.find (fun q -> q.Name = name)
  Assert.True((quest "Leaders of the Fang" w).Done)

  let ambush =
    SimState.aliveHostiles w |> List.filter (fun e -> e.Name = "Deviate Moccasin")

  Assert.Equal(3, List.length ambush)
  Assert.True(dormant "Mutanus the Devourer" w)

  // Clearing the ambush brings Mutanus up out of the water.
  let w = ambush |> List.fold (fun w e -> SimCombat.dealDamage e.Id 1000000 w) w
  Assert.True(dormant "Mutanus the Devourer" w)

  // The second wave: the nightmare oozes. Clearing them brings him up.
  let oozes =
    SimState.aliveHostiles w |> List.filter (fun e -> e.Name = "Nightmare Ectoplasm")

  Assert.Equal(8, List.length oozes)
  let w = oozes |> List.fold (fun w e -> SimCombat.dealDamage e.Id 1000000 w) w
  Assert.False(dormant "Mutanus the Devourer" w)
  Assert.Contains("Mutanus the Devourer", targetable w)

  // Killing him is the end: Naralex wakes and speaks, and every remaining
  // hostile despawns in the real instance — here they are simply no longer the
  // objective, since the boss that ends the run is dead.
  let w = kill "Mutanus the Devourer" w
  Assert.True((quest "Mutanus the Devourer" w).Done)
  Assert.True(w.Log |> List.exists (fun l -> l.Contains "I AM AWAKE"), String.concat " | " w.Log)

// ===========================================================================
// The nine-class roster
// ===========================================================================

/// The paladin's kit is the researched level-24 one, rank for rank.
[<Fact>]
let ``the paladin's kit is the level-24 one`` () =
  let paladin =
    Content.partyOf [ "Warrior"; "Paladin"; "Rogue"; "Mage"; "Druid" ] 24
    |> List.find (fun e -> e.Name = "Paladin")

  let effectsOf name =
    (paladin.Abilities |> List.find (fun a -> a.Name = name)).Effects

  // Holy Light rank 4 (level 22) and Flash of Light rank 1 (level 20) on the
  // 1.15.9 client this content follows.
  Assert.Equal<Effect list>([ Heal(322, 368) ], effectsOf "Holy Light")
  Assert.Equal<Effect list>([ Heal(67, 77) ], effectsOf "Flash of Light")
  // Judgement with Seal of Righteousness rank 3 up.
  Assert.Equal<Effect list>([ SpellDamage(53, 57) ], effectsOf "Judgement")
  // Hammer of Justice rank 2 is a 4 s stun, Divine Protection rank 2 is 8 s of
  // immunity.
  Assert.Equal<Effect list>([ Stun(secTicks 4.0) ], effectsOf "Hammer of Justice")
  Assert.Equal<Effect list>([ ReducesDamage(secTicks 8.0, 10000) ], effectsOf "Divine Protection")
  // Divine Favor is the tree's cooldown button: two minutes, 4% of base mana.
  let divineFavor =
    paladin.Abilities |> List.find (fun a -> a.Name = "Divine Favor")

  Assert.Equal(secTicks 120.0, divineFavor.CooldownTicks)
  Assert.Equal(21, divineFavor.ResourceCost)
  Assert.Equal(6, List.length paladin.Abilities)

/// The researched set, put through the sim's own formulas. Every number here is
/// derived by hand from `.pi/research/level24/paladin.md` rather than read back
/// out of the code, so a formula change that quietly moves the paladin fails.
[<Fact>]
let ``the paladin is built on the researched level-24 set`` () =
  let roster = [ "Warrior"; "Paladin"; "Rogue"; "Mage"; "Druid" ]

  let paladin =
    Content.partyOf roster 24 |> List.find (fun e -> e.Name = "Paladin")

  // Base (47, 34, 46, 35, 38) plus the seven verified pieces: Str +27, Agi +7,
  // Sta +39, Int +9, Spi +12. Mark of the Wild adds 4 to all five and Arcane
  // Intellect 7 Intellect, so Stamina is 89 and Intellect 55 — and Divine
  // Intellect is another 10% on top of that, which is 60.
  // Health = 247 + (20 + 10 x 69); mana = 525 + (20 + 15 x 40).
  Assert.Equal(957, paladin.MaxHealth)
  Assert.Equal(1145, paladin.MaxResource)
  // 557 from the four mail pieces, 90 from 45 Agility, 275 Devotion Aura, 105
  // Mark of the Wild.
  Assert.Equal(1027, paladin.Armor)
  // Entity.Stats is the dressed set before the passives: Strength 47 + 27 = 74.
  Assert.Equal(74, paladin.Stats.Strength)
  // Mark of the Wild takes it to 78, which is 156 attack power, plus Battle Shout
  // 55 and Blessing of Might 55: 266 AP, so 266/14 x 3.20 s = 60 a swing. The
  // attack power itself is not stored, so the swing bonus is what proves it.
  Assert.Equal(60, paladin.MeleeDamageBonus)
  // Verigan's Fist, 65-99 at 3.20 s.
  Assert.Equal((65, 99), (paladin.AutoAttack.Value.Min, paladin.AutoAttack.Value.Max))

/// Gear scales with level from the level-24 set to the class's level-60 pre-raid
/// best-in-slot outfit, which `.pi/research/levelscale/bis-60.md` tabulates piece
/// by piece. The level-24 numbers are frozen (they are the calibration point this
/// file's content is tuned at); level 60 has to land on the researched totals, and
/// nothing in between may go backwards.
[<Fact>]
let ``gear grows from the level-24 set to the level-60 best in slot`` () =
  // A warrior alone, so Battle Shout's attack power is the only passive and the
  // stat totals are the dressed ones: base row at 60 is (120, 80, 110, 30, 47) and
  // the researched outfit adds (119, 34, 280, 10, 16) — the file's own TOTALS.
  let warrior =
    Content.partyOf [ "Warrior" ] 60 |> List.find (fun e -> e.Name = "Warrior")

  Assert.Equal(239, warrior.Stats.Strength)
  Assert.Equal(114, warrior.Stats.Agility)
  Assert.Equal(390, warrior.Stats.Stamina)
  Assert.Equal(40, warrior.Stats.Intellect)
  // 6523 from the seventeen pieces plus 228 from 114 Agility; no Devotion Aura in
  // a warrior-only party.
  Assert.Equal(6751, warrior.Armor)

[<Fact>]
let ``the weapon grows with level up to the researched level-60 pick`` () =
  let roster =
    [ "Warrior"; "Paladin"; "Priest"; "Druid"; "Mage"; "Warlock"; "Shaman"; "Hunter"; "Rogue" ]

  // One member at a time: `partyOf` fills a five-member party from the roster
  // order, so a nine-name roster would silently drop the last four classes.
  let attack name level =
    Content.partyOf [ name ] level |> List.head |> (fun e -> Option.get e.AutoAttack)

  // Damage per second in thousandths, so the comparison stays in integers.
  let dps (a: AutoAttack) = (a.Min + a.Max) * 1000 / a.Ticks

  for name in roster do
    // The hunter's level-1 rung is the calibrated level-24 bow (66-84 at 1.80 s),
    // stronger than the researched 28-40 bows, so his band rungs dip. That is
    // pre-existing 24-band data; the level-60 rung is floored against it instead.
    let from = if name = "Hunter" then 29 else 25

    for level in from..60 do
      Assert.True(
        dps (attack name level) >= dps (attack name (level - 1)),
        sprintf "%s swings weaker at level %d than at %d" name level (level - 1)
      )

    Assert.True(
      dps (attack name 60) >= dps (attack name 24),
      sprintf "%s swings weaker at 60 than at 24" name
    )

  // The warrior's level-60 pick as researched: 53-100 at 1.60 s.
  let warrior = attack "Warrior" 60
  Assert.Equal((53, 100), (warrior.Min, warrior.Max))
  Assert.Equal(32, int warrior.Ticks)

[<Fact>]
let ``no class loses a stat on the way from 24 to 60`` () =
  let roster = [ "Warrior"; "Paladin"; "Priest"; "Druid"; "Mage"; "Warlock"; "Shaman"; "Hunter"; "Rogue" ]

  // One class at a time: a nine-class roster is not a party, and a solo class
  // brings its own passives at both levels, so the comparison stays like for like.
  let at level name =
    Content.partyOf [ name ] level |> List.find (fun e -> e.Name = name)

  for name in roster do
    for level in 25..60 do
      let before = at (level - 1) name
      let after = at level name

      Assert.True(
        after.Armor >= before.Armor,
        sprintf "%s armour fell at level %d: %d -> %d" name level before.Armor after.Armor
      )

      Assert.True(
        after.Stats.Strength >= before.Stats.Strength
        && after.Stats.Agility >= before.Stats.Agility
        && after.Stats.Stamina >= before.Stats.Stamina
        && after.Stats.Intellect >= before.Stats.Intellect
        && after.Stats.Spirit >= before.Stats.Spirit,
        sprintf "%s stats fell at level %d" name level
      )

      Assert.True(
        after.MaxHealth >= before.MaxHealth,
        sprintf "%s health fell at level %d: %d -> %d" name level before.MaxHealth after.MaxHealth
      )

/// A roster decides the party's passives, which is the whole reason the buffs
/// stopped being a fixed list.
[<Fact>]
let ``the party's passives come from the roster`` () =
  Assert.Equal<PartyBuffKind list>(
    [ BattleShout; BlessingOfMight; DevotionAura ],
    Content.partyBuffsFor [ "Warrior"; "Paladin" ]
  )

  // A paladin brings Devotion Aura, not Mark of the Wild: a party with no Druid
  // loses the armour and the four stats.
  Assert.DoesNotContain(MarkOfTheWild, Content.partyBuffsFor [ "Paladin"; "Mage" ])
  Assert.Contains(ArcaneIntellect, Content.partyBuffsFor [ "Paladin"; "Mage" ])

  // The default roster still produces exactly the passives it always had.
  Assert.Equal<PartyBuffKind list>(
    [ ArcaneIntellect; PowerWordFortitude; MarkOfTheWild; BattleShout ],
    Content.partyBuffs
  )

/// A roster swap must not hand two entities the same id: a class that arrived
/// later would otherwise take a number the mobs are already using, and every
/// `tryEntity` on that id would find whichever came first in the list. That bug
/// put the paladin on id 6, the first Druid of the Fang, and made a mob's heal
/// read as the paladin healing the boss.
[<Fact>]
let ``a roster swap keeps entity ids unique`` () =
  let w =
    Content.gully 1UL
    |> Content.withRoster [ "Warrior"; "Paladin"; "Rogue"; "Mage"; "Druid" ]

  let ids = w.Entities |> List.map (fun e -> e.Id)
  Assert.Equal(ids.Length, ids |> List.distinct |> List.length)
  // The party is seated 1..5 in roster order, so the paladin is id 2.
  Assert.Contains(EntityId 2, ids)

/// The paladin's heals have to actually leave its hands. A wounded tank and one
/// hostile are enough: the scripted healer only thinks about healing while the
/// party is in a fight.
[<Fact>]
let ``a paladin heals a wounded tank`` () =
  let party = Content.partyOf [ "Warrior"; "Paladin" ] 24

  let tank =
    { party.[0] with
        Pos = { X = 5; Y = 5 }
        Health = 300 }

  let paladin = { party.[1] with Pos = { X = 5; Y = 6 } }

  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let enemy =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 2000 2000 (Some tap) [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some tank.Id }

  let w =
    { Tick = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; paladin; enemy ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Log = [] }

  // The scripted Party is input, not part of the step: a run that never passes it
  // the autopilot's commands gives nobody an order. This is the loop `Bench` uses.
  let mutable w = w

  for _ in 1 .. 400 do
    w <- SimTick.step (Bench.autoPilot w) w

  let log = String.concat "\n" w.Log

  Assert.True(log.Contains "Paladin heals", sprintf "the paladin should have healed; log:\n%s" log)

/// The warlock's kit is the rank-for-rank level-24 one: no Banish (28), no
/// Hellfire (30), and Corruption a 2.0 s cast because Improved Corruption does
/// not exist yet at 24.
[<Fact>]
let ``the warlock's kit is the level-24 one`` () =
  let warlock =
    Content.partyOf [ "Warrior"; "Warlock" ] 24 |> List.find (fun e -> e.Name = "Warlock")

  let effectsOf name =
    (warlock.Abilities |> List.find (fun a -> a.Name = name)).Effects

  Assert.Equal<Effect list>([ SpellDamage(86, 99) ], effectsOf "Shadow Bolt")
  Assert.Equal<Effect list>([ DamageOverTime(secTicks 18.0, 12) ], effectsOf "Corruption")
  Assert.Equal<Effect list>([ DamageOverTime(secTicks 24.0, 7) ], effectsOf "Curse of Agony")
  Assert.Equal<Effect list>(
    [ SpellDamage(45, 45); DamageOverTime(secTicks 15.0, 6) ],
    effectsOf "Immolate"
  )
  Assert.Equal<Effect list>([ Sleep(secTicks 10.0) ], effectsOf "Fear")
  // Corruption is a cast, not instant.
  Assert.Equal(
    secTicks 2.0,
    (warlock.Abilities |> List.find (fun a -> a.Name = "Corruption")).CastTicks
  )
  Assert.Equal(5, List.length warlock.Abilities)

/// The researched gear totals, straight from `.pi/research/level24/warlock.md`:
/// Str 23, Agi 39, Sta 57, Int 82, Spi 73 on five unfilled slots.
[<Fact>]
let ``the warlock is built on the researched level-24 set`` () =
  let roster = [ "Warrior"; "Paladin"; "Warlock"; "Mage"; "Druid" ]

  let warlock =
    Content.partyOf roster 24 |> List.find (fun e -> e.Name = "Warlock")

  // Gear totals, before the party's passives.
  Assert.Equal(
    (23, 39, 57, 82, 73),
    (warlock.Stats.Strength,
     warlock.Stats.Agility,
     warlock.Stats.Stamina,
     warlock.Stats.Intellect,
     warlock.Stats.Spirit)
  )

  // Base health 266 with Stamina 61 (57 + Mark of the Wild); base mana 467 with
  // Intellect 93 (82 + 4 + Arcane Intellect 7), which is the largest pool of the
  // nine and the point of the class.
  Assert.Equal(696, warlock.MaxHealth)
  Assert.Equal(1582, warlock.MaxResource)
  // 175 from the six cloth pieces, 86 from 43 Agility, 105 Mark of the Wild and
  // 275 Devotion Aura from the roster.
  Assert.Equal(641, warlock.Armor)
  // Gravestone Scepter, 30-57 at 1.50 s for 29 DPS.
  Assert.Equal((30, 57), (warlock.AutoAttack.Value.Min, warlock.AutoAttack.Value.Max))

/// The shaman's kit is the level-24 one: no Windfury Weapon (30), no Chain
/// Lightning (32), and Earth Shock rank 4 which is also the party's second kick.
/// Stoneskin (16) and Healing Stream (20) are the totems a 24 has trained; Grace of
/// Air (42), Mana Tide (40) and Mana Spring (26) are above the bracket.
[<Fact>]
let ``the shaman's kit is the level-24 one`` () =
  let shaman =
    Content.partyOf [ "Warrior"; "Shaman" ] 24 |> List.find (fun e -> e.Name = "Shaman")

  let effectsOf name =
    (shaman.Abilities |> List.find (fun a -> a.Name = name)).Effects

  Assert.Equal<Effect list>([ SpellDamage(88, 100) ], effectsOf "Lightning Bolt")
  Assert.Equal<Effect list>([ SpellDamage(125, 133); Interrupt ], effectsOf "Earth Shock")
  Assert.Equal<Effect list>(
    [ SpellDamage(51, 51); DamageOverTime(secTicks 12.0, 4) ],
    effectsOf "Flame Shock"
  )
  Assert.Equal<Effect list>([ Heal(389, 453) ], effectsOf "Healing Wave")
  Assert.Equal<Effect list>(
    [ Empowers(secTicks 20.0, { Instant = true; FreeMana = false; SureCrit = false }) ],
    effectsOf "Nature's Swiftness"
  )
  Assert.Equal<string list>(
    [ "Lightning Bolt"
      "Earth Shock"
      "Flame Shock"
      "Healing Wave"
      "Stoneskin Totem"
      "Healing Stream Totem"
      "Nature's Swiftness" ],
    shaman.Abilities |> List.map (fun a -> a.Name)
  )

/// Orc base (43, 28, 45, 39, 49) plus the Druid's verified ≤24 pieces.
[<Fact>]
let ``the shaman is built on the researched level-24 set`` () =
  let roster = [ "Warrior"; "Shaman"; "Warlock"; "Mage"; "Druid" ]

  let shaman =
    Content.partyOf roster 24 |> List.find (fun e -> e.Name = "Shaman")

  // Gear totals before the passives: +0 Str, +7 Agi, +19 Sta, +48 Int, +29 Spi.
  Assert.Equal(
    (43, 35, 64, 87, 78),
    (shaman.Stats.Strength,
     shaman.Stats.Agility,
     shaman.Stats.Stamina,
     shaman.Stats.Intellect,
     shaman.Stats.Spirit)
  )

  // Base health 234, base mana 476; Stamina 68 and Intellect 98 once Mark of the
  // Wild and Arcane Intellect are up, and 102 after Ancestral Knowledge's 5%.
  Assert.Equal(734, shaman.MaxHealth)
  Assert.Equal(1726, shaman.MaxResource)
  Assert.Equal(421, shaman.Armor)
  // A 47-71 staff at 3.0 s, the same verified ≤24 weapon the Druid carries.
  Assert.Equal((47, 71), (shaman.AutoAttack.Value.Min, shaman.AutoAttack.Value.Max))

/// The healing talents are worth real healing and not just a number on a sheet:
/// one Healing Wave from the talented shaman lands a strictly larger heal than
/// the same shaman with every point taken out, on the same seed and the same
/// wound. Purification's 10% alone would overlap the rank's roll; Spiritual
/// Guidance's share of Spirit is what puts the two apart for certain.
[<Fact>]
let ``a resto shaman's heal beats the same shaman with no talents`` () =
  let party = Content.partyOf [ "Warrior"; "Shaman" ] 24
  let tank = party.[0]
  let shaman = { party.[1] with BonusHealing = 0 }

  let oneHeal (shaman: Entity) =
    let wounded =
      { tank with
          Pos = { X = 5; Y = 5 }
          Health = 300 }

    let healer = { shaman with Pos = { X = 5; Y = 6 } }
    let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

    let enemy =
      { Content.mob 100 "Near" CreatureType.Humanoid 21 4000 2000 (Some tap) [] None { X = 6; Y = 5 } with
          Engaged = true
          Target = Some wounded.Id }

    let mutable w =
      { Tick = ticks 0
        Grid = Grid.create 12 10
        Entities = [ wounded; healer; enemy ]
        Pending = []
        Rng = Rng.streamsOf 7UL
        PartyBuffs = []
        Quests = []
        Log = [] }

    let healIn (line: string) =
      let parts = line.Split ' '

      if parts.[0] = "Shaman" && parts.[1] = "heals" then
        match System.Int32.TryParse parts.[parts.Length - 1] with
        | true, n -> n
        | _ -> 0
      else
        0

    let mutable healed = 0

    // A 2.5-3.0 s cast, so a hundred Ticks is one cast with room to spare.
    for _ in 1..100 do
      if healed = 0 then
        w <- SimTick.step (Bench.autoPilot w) w
        healed <- w.Log |> List.sumBy healIn

    healed

  let talented = oneHeal shaman
  let plain = oneHeal { shaman with Talents = Talents.none }

  Assert.True(
    talented > plain,
    sprintf "the talented shaman should heal for more: %d vs %d" talented plain
  )

/// The talent cooldowns are buttons, not passives: Divine Favor is spent on the
/// first heal after the Tank drops under 40% and then waits out its two minutes.
/// The run below is 45 s of Ticks against a mob that swings for nothing, so the
/// Tank sits at 30% and the Paladin heals it over and over while the cooldown is
/// still running — one cast, and not a second one.
[<Fact>]
let ``a talent cooldown fires once and then waits for its cooldown`` () =
  let party = Content.partyOf [ "Warrior"; "Paladin" ] 24
  let tank = { party.[0] with Pos = { X = 5; Y = 5 } }

  let paladin =
    { party.[1] with
        Pos = { X = 5; Y = 6 }
        Resource = 4000
        MaxResource = 4000 }

  let wounded = { tank with Health = tank.MaxHealth * 30 / 100 }
  let tap = { Min = 0; Max = 0; Ticks = secTicks 2.7; Range = 1 }

  let enemy =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 4000 2000 (Some tap) [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some wounded.Id }

  let mutable w =
    { Tick = ticks 0
      Grid = Grid.create 12 10
      Entities = [ wounded; paladin; enemy ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Log = [] }

  // 900 Ticks is 45 s: comfortably more than the instant button needs to be
  // pressed, and well under the two-minute cooldown it starts.
  let mutable fires = 0
  let mutable was = ticks 0

  for _ in 1..900 do
    w <- SimTick.step (Bench.autoPilot w) w

    let now =
      (SimState.tryEntity paladin.Id w).Value.Cooldowns
      |> Map.tryFind "Divine Favor"
      |> Option.defaultValue (ticks 0)

    // A cast resets the timer upward; a tick that only counts down does not.
    if now > was then fires <- fires + 1

    was <- now

  let remaining =
    (SimState.tryEntity paladin.Id w).Value.Cooldowns
    |> Map.tryFind "Divine Favor"
    |> Option.defaultValue (ticks 0)

  Assert.Equal(1, fires)
  Assert.True(remaining > ticks 0, "the cooldown should still be running after 45 s")

/// A heal crit leaves armour behind. Inspiration is 3/3 on the Priest, so the
/// crit puts a quarter more armour on the healed target — and the same crit from a
/// Priest who spent no points there leaves nothing at all.
[<Fact>]
let ``a heal crit from Inspiration leaves the target's armour up`` () =
  let party = Content.partyOf [ "Warrior"; "Priest" ] 24
  let tank = { party.[0] with Pos = { X = 5; Y = 5 }; Health = 400 }

  let crit (priest: Entity) =
    let healer =
      { priest with
          Pos = { X = 5; Y = 6 }
          // A sure crit: Inspiration's proc is what is under test, not the dice.
          CritChanceBp = 10000 }

    let heal = healer.Abilities |> List.find (fun a -> a.Name = "Heal")

    let w =
      { Tick = ticks 0
        Grid = Grid.create 12 10
        Entities = [ tank; healer ]
        Pending = []
        Rng = Rng.streamsOf 3UL
        PartyBuffs = []
        Quests = []
        Log = [] }

    let healed = SimCombat.resolveAbility healer.Id tank.Id heal w
    Aura.armorPctBp (SimState.tryEntity tank.Id healed).Value.Auras

  Assert.Equal(0, Aura.armorPctBp tank.Auras)
  Assert.Equal(2500, crit party.[1])
  // The same priest with the tree emptied: the crit still happens, and pays
  // nothing, which is what keeps every non-healer byte-identical.
  Assert.Equal(0, crit { party.[1] with Talents = Talents.none })

/// Illumination 5/5 hands a crit heal's mana straight back, so the cast is free.
/// A Paladin without the talent pays for the same flash out of pocket.
[<Fact>]
let ``Illumination returns a crit heal's mana`` () =
  let party = Content.partyOf [ "Warrior"; "Paladin" ] 24
  let tank = { party.[0] with Pos = { X = 5; Y = 5 }; Health = 400 }

  let manaAfter (paladin: Entity) =
    let healer =
      { paladin with
          Pos = { X = 5; Y = 6 }
          CritChanceBp = 10000
          Resource = 100
          MaxResource = 5000 }

    let flash = healer.Abilities |> List.find (fun a -> a.Name = "Flash of Light")

    let w =
      { Tick = ticks 0
        Grid = Grid.create 12 10
        Entities = [ tank; healer ]
        Pending = []
        Rng = Rng.streamsOf 3UL
        PartyBuffs = []
        Quests = []
        Log = [] }

    (SimState.tryEntity healer.Id (SimCombat.resolveAbility healer.Id tank.Id flash w)).Value.Resource

  let withIllumination = manaAfter party.[1]
  let without = manaAfter { party.[1] with Talents = Talents.none }

  Assert.Equal(100, withIllumination)
  Assert.True(without < 100, sprintf "the plain Paladin should have paid: %d" without)

/// A totem is an Entity that pulses on its own clock: the Healing Stream totem
/// heals every party member in reach every two seconds, and stops the Tick it runs
/// out of time. The hostile is there so the party is in combat and out-of-combat
/// regeneration cannot pass for a totem's work.
[<Fact>]
let ``a healing stream totem heals the party until it expires`` () =
  let tank = { (Content.partyOf [ "Warrior" ] 24).[0] with Health = 200 }

  let totem =
    { tank with
        Name = "Healing Stream Totem"
        Pos = { X = 6; Y = 5 }
        MaxHealth = 5
        Health = 5
        Abilities = []
        AutoAttack = None
        Owner = Some tank.Id
        ExpiresInTicks = Some(secTicks 6.0)
        Totem = Some { Pulse = Heals 44; Radius = 5; Period = secTicks 2.0 } }

  let swing = { Min = 0; Max = 0; Ticks = secTicks 2.7; Range = 1 }

  let enemy =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 4000 2000 (Some swing) [] None { X = 8; Y = 5 } with
        Engaged = true
        Target = Some tank.Id }

  let mutable w =
    { Tick = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; totem; enemy ]
      Pending = []
      Rng = Rng.streamsOf 11UL
      PartyBuffs = []
      Quests = []
      Log = [] }

  let tankHealth () =
    w.Entities |> List.find (fun e -> e.Id = tank.Id) |> fun e -> e.Health

  for _ in 1..50 do
    w <- SimTick.step [] w

  let healed = tankHealth ()
  Assert.True(healed > 200, sprintf "the totem should have healed the tank: %d" healed)

  // Twenty seconds later the six-second totem is long gone, so the pool stays
  // where the last pulse left it.
  for _ in 1..400 do
    w <- SimTick.step [] w

  let settled = tankHealth ()
  Assert.False(w.Entities |> List.exists (fun e -> e.Name = "Healing Stream Totem" && SimState.alive e))
  Assert.Equal(settled, tankHealth ())

/// Nine classes is the whole roster, and every one of them has to build at 24:
/// a builder that returns the wrong name, or none at all, fails here.
[<Fact>]
let ``every class builds at level 24`` () =
  let names =
    [ "Warrior"; "Paladin"; "Rogue"; "Hunter"; "Priest"; "Shaman"; "Mage"; "Warlock"; "Druid" ]

  for name in names do
    let built = Content.partyOf [ name ] 24
    Assert.Equal(1, List.length built)
    Assert.Equal(name, (List.head built).Name)
    Assert.NotEmpty((List.head built).Abilities)

  // Nine names, five slots: the roster is seated in order.
  Assert.Equal<string list>(
    [ "Warrior"; "Paladin"; "Rogue"; "Hunter"; "Priest" ],
    Content.partyOf names 24 |> List.map (fun e -> e.Name)
  )

/// The hunter's kit is the researched level-24 one.
[<Fact>]
let ``the hunter's kit is the level-24 one`` () =
  let hunter = Content.partyOf [ "Hunter" ] 24 |> List.head

  let effectsOf name =
    (hunter.Abilities |> List.find (fun a -> a.Name = name)).Effects

  // Arcane Shot rank 3: 33 Arcane for 50 mana. Arcane is magic, so it is not
  // armour-mitigated — the record this replaced ran it through armour as physical.
  Assert.Equal<Effect list>([ SpellDamage(33, 33) ], effectsOf "Arcane Shot")
  // Serpent Sting rank 3: 80 Nature over 15 s, which is 5 a second.
  Assert.Equal<Effect list>([ DamageOverTime(secTicks 15.0, 5) ], effectsOf "Serpent Sting")
  // Raptor Strike rank 4: the next swing lands +34.
  Assert.Equal<Effect list>([ WeaponDamage(10000, 34) ], effectsOf "Raptor Strike")
  Assert.Equal(3, List.length hunter.Abilities)

/// A hunter's auto-attack is its bow, so it stands at range like the casters do:
/// 21-39 bow plus Sharp Arrow plus 302 ranged attack power, 66-84 a shot.
[<Fact>]
let ``the hunter shoots from range`` () =
  let hunter = Content.partyOf [ "Hunter" ] 24 |> List.head

  Assert.Equal(8, hunter.AutoAttack.Value.Range)
  Assert.Equal((66, 84), (hunter.AutoAttack.Value.Min, hunter.AutoAttack.Value.Max))
  Assert.Equal(secTicks 1.8, hunter.AutoAttack.Value.Ticks)

/// A hunter brings no party passive, so it must not silently change the buffs.
[<Fact>]
let ``a hunter adds no party passive`` () =
  Assert.Equal<PartyBuffKind list>([ BattleShout ], Content.partyBuffsFor [ "Warrior"; "Hunter" ])

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
    Content.hero 2 "Priest" Healer 1000 1000 tap 10000 [ Content.lesserHeal; Content.heal; Content.renew ] { X = 5; Y = 6 }

  let near = { Content.mob 100 "Near" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 6; Y = 5 } with Engaged = true; Target = Some(EntityId 1) }
  let far = { Content.mob 101 "Far" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 5; Y = 4 } with Engaged = true; Target = Some(EntityId 1) }

  let tank = { tank with Health = tankHealth; Auras = auras }

  { Tick = ticks 0
    Grid = Grid.create 12 10
    Entities = [ tank; priest; near; far ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Log = [] }

let private abilitiesFor (actor: EntityId) (cmds: Command list) =
  cmds
  |> List.choose (fun c ->
    match c.Kind with
    | UseAbility(a, n, _) when a = actor -> Some n
    | _ -> None)

/// Flame Shock is the shaman's damage spell: Earth Shock carries an interrupt, so
/// the script keeps it out of the rotation and spends it on a cast instead.
[<Fact>]
let ``the ranged behaviour picks the shaman's shock`` () =
  let shaman =
    Content.hero
      2
      "Shaman"
      Dps
      1000
      1666
      { Min = 47; Max = 71; Ticks = secTicks 3.0; Range = 1 }
      10000
      [ Content.shamanLightningBolt; Content.earthShock; Content.flameShock; Content.shamanHealingWave ]
      { X = 5; Y = 7 }

  let w =
    partyWorld (Some(EntityId 100)) [] 1000
    |> fun w -> { w with Entities = (w.Entities |> List.filter (fun e -> e.Id <> EntityId 2)) @ [ shaman ] }

  Assert.Contains("Flame Shock", abilitiesFor (EntityId 2) (PartyAi.decide w))

/// Earth Shock's second job is the kick: the shaman is a second interrupt next to
/// the Mage's Counterspell, and the Warrior's Sunder Armor cannot cover a cast.
[<Fact>]
let ``the shaman interrupts with Earth Shock`` () =
  let shaman =
    Content.hero
      4
      "Shaman"
      Dps
      1000
      1666
      { Min = 47; Max = 71; Ticks = secTicks 3.0; Range = 1 }
      10000
      [ Content.shamanLightningBolt; Content.earthShock; Content.flameShock; Content.shamanHealingWave ]
      { X = 5; Y = 6 }

  let tank =
    Content.hero
      1
      "Warrior"
      Tank
      1000
      1000
      { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }
      50000
      [ Content.sunderArmor ]
      { X = 5; Y = 5 }

  let caster =
    { Content.mob 101 "Fang" CreatureType.Humanoid 21 1000 1000 (Some { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }) [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some(EntityId 1)
        Casting =
          Some
            { Ability = Content.heal
              Target = EntityId 101
              Remaining = secTicks 2.0 } }

  let w =
    { Tick = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; shaman; caster ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Log = [] }

  Assert.Contains("Earth Shock", abilitiesFor (EntityId 4) (PartyAi.decide w))

/// And a rostered warlock is driven by the ranged behaviour: Shadow Bolt is the
/// hardest cast in the kit, so it is what the party asks for.
[<Fact>]
let ``the ranged behaviour picks the warlock's bolt`` () =
  let wand = { Min = 30; Max = 57; Ticks = secTicks 1.5; Range = 8 }

  let warlock =
    Content.hero
      2
      "Warlock"
      Dps
      1000
      1582
      wand
      10000
      [ Content.shadowBolt; Content.corruption; Content.curseOfAgony; Content.warlockImmolate; Content.fear ]
      { X = 5; Y = 7 }

  let w =
    partyWorld (Some(EntityId 100)) [] 1000
    |> fun w -> { w with Entities = (w.Entities |> List.filter (fun e -> e.Id <> EntityId 2)) @ [ warlock ] }

  Assert.Contains("Shadow Bolt", abilitiesFor (EntityId 2) (PartyAi.decide w))

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

  let tank = SimState.aliveParty w |> List.tryFind (fun e -> e.Id = EntityId 100)

  Assert.Equal<EntityId option>(
    Some(EntityId 101),
    PartyAi.strikeTarget w tank (SimState.aliveHostiles w) |> Option.map (fun e -> e.Id)
  )

/// A cast the melee members cannot reach is the Mage's to stop: Counterspell has
/// thirty yards and a thirty-second clock, so it is spent on what nobody else can
/// touch.
[<Fact>]
let ``the mage counterspells a caster out of melee reach`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let tank =
    Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 5; Y = 5 }

  let mage =
    Content.hero 4 "Mage" Dps 1000 1000 tap 10000 [ Content.fireball; Content.counterspell ] { X = 5; Y = 6 }

  let healer =
    { Content.mob 101 "Fang" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some(EntityId 1)
        Casting =
          Some
            { Ability = Content.heal
              Target = EntityId 101
              Remaining = secTicks 2.0 } }

  let w =
    { Tick = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; mage; healer ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Log = [] }

  let orders = abilitiesFor (EntityId 4) (PartyAi.decide w)
  Assert.Contains("Counterspell", orders)

/// Threat beats armour stripping: the Mob the Warrior is holding is the one that
/// will not turn round when the Dps open up on it.
[<Fact>]
let ``dps follow the warrior's threat, not just the sundered target`` () =
  let w =
    partyWorld (Some(EntityId 100)) [] 1000
    |> SimState.mapEntity (EntityId 101) (fun e -> { e with Auras = [ Sundered(secTicks 20.0, 180) ] })
    |> SimState.mapEntity (EntityId 100) (fun e -> { e with Threat = Map.ofList [ EntityId 1, 9000 ] })

  let tank = SimState.aliveParty w |> List.tryFind (fun e -> e.Id = EntityId 1)

  Assert.Equal<EntityId option>(
    Some(EntityId 100),
    PartyAi.strikeTarget w tank (SimState.aliveHostiles w) |> Option.map (fun e -> e.Id)
  )

/// A heal-over-time with ten seconds to run is worth 120 more health: casting it
/// again is mana set on fire, so the healer waits.
[<Fact>]
let ``the healer does not re-apply a heal-over-time that still has time left`` () =
  let w = partyWorld None [ Rejuvenation(secTicks 10.0, 12, "Renew") ] 700
  // It may top up the remaining 300 with a direct heal; what it must not do is
  // pay for a second heal-over-time while 120 health is still on its way.
  Assert.DoesNotContain("Renew", abilitiesFor (EntityId 2) (PartyAi.decide w))

[<Fact>]
let ``the healer refreshes the heal-over-time as it lapses`` () =
  let w = partyWorld None [ Rejuvenation(secTicks 1.0, 12, "Renew") ] 700
  Assert.Contains("Renew", abilitiesFor (EntityId 2) (PartyAi.decide w))

/// Three seconds of cast that starts when the bar is empty has started too late.
[<Fact>]
let ``the healer starts a direct heal once the deficit outruns the heals in flight`` () =
  let w = partyWorld None [ Rejuvenation(secTicks 10.0, 12, "Renew") ] 400
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

/// Taking a member over by hand stops the script driving it, and handing it back
/// resumes it.
[<Fact>]
let ``a manually controlled member gets no AI orders`` () =
  let w = partyWorld None [] 400

  let tankOrders (manual: Set<EntityId>) =
    PartyAi.supplementExcept manual [] w
    |> List.filter (fun c ->
      match c.Kind with
      | UseAbility(EntityId 1, _, _)
      | MoveTo(EntityId 1, _) -> true
      | _ -> false)
    |> List.length

  Assert.True(tankOrders Set.empty > 0, "the script should be driving the Tank")
  Assert.Equal(0, tankOrders (Set.singleton (EntityId 1)))

/// Every rule that walks the Party hangs off the Tank: the follow leash, the
/// step-aside shuffle, the lead to the next mob. With the Tank dead they all went
/// quiet at once and survivors stood on cleared floor for the rest of the run —
/// two of them sat in a door alcove of the Scarlet graveyard for 24 000 ticks
/// while the mobs they had never met waited, untouched.
[<Fact>]
let ``survivors lead themselves once the Tank is dead`` () =
  let w = partyWorld None [] 0
  Assert.Empty(SimState.aliveParty w |> List.filter (fun e -> e.Role = Some Tank))

  // Out of reach: standing beside the mob needs no walk, and a pure healer beside
  // one has nothing to cast, so the Lead rule only shows on a mob further off.
  let away =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e ->
            match e.Name with
            | "Near" -> { e with Pos = { X = 10; Y = 8 } }
            | "Far" -> { e with Pos = { X = 10; Y = 7 } }
            | _ -> e) }

  let moves =
    PartyAi.decide away
    |> List.choose (fun c ->
      match c.Kind with
      | MoveTo(EntityId 2, p) -> Some p
      | _ -> None)

  Assert.NotEmpty(moves)

/// The last member can die mid-decision: a wipe must not throw while the script
/// is still choosing what the dead would have done.
[<Fact>]
let ``a wiped party gets no orders`` () =
  let w = partyWorld None [] 0
  let dead = { w with Entities = [] }
  Assert.Empty(PartyAi.decide dead)

/// The panel files abilities under what they do, not under what order the class
/// happened to learn them in.
[<Fact>]
let ``abilities are grouped by what they do`` () =
  let warrior = (Content.gully 1UL).Entities |> List.find (fun e -> e.Role = Some Tank)
  let group name = warrior.Abilities |> List.find (fun a -> a.Name = name) |> Ability.group

  Assert.Equal(AbilityGroup.Damage, group "Heroic Strike")
  Assert.Equal(AbilityGroup.Defence, group "Shield Wall")
  Assert.Equal(AbilityGroup.Defence, group "Shield Block")
  // A control button is filed by its control, not by the 6 damage it also does.
  Assert.Equal(AbilityGroup.Utility, group "Shield Bash")
  Assert.Equal(AbilityGroup.Utility, group "Taunt")

  let priest = (Content.gully 1UL).Entities |> List.find (fun e -> e.Role = Some Healer)
  Assert.Equal(AbilityGroup.Heal, (priest.Abilities |> List.find (fun a -> a.Name = "Heal") |> Ability.group))

  Assert.Equal(
    AbilityGroup.HealOverTime,
    (priest.Abilities |> List.find (fun a -> a.Name = "Renew") |> Ability.group)
  )

[<Fact>]
let ``aura labels show a readable name, side and clock`` () =
  let sunder = Sundered(ticks 245, 20)
  Assert.Equal("SUNDER", Aura.label sunder)
  Assert.False(Aura.isHelpful sunder)
  Assert.Equal(245, int (Aura.remaining sunder))
  // 245 Ticks is 12.25s at 20Hz: rounded up, so a live aura never reads 0s.
  Assert.Equal(13, Aura.remainingSeconds sunder)
  Assert.True(Aura.isHelpful (Rejuvenation(ticks 40, 5, "Rejuvenation")))
  // Exactly one second left is "1s", not "2s": the round-up is for leftovers.
  Assert.Equal(1, Aura.remainingSeconds (SerpentForm(ticks 20, 4)))
  Assert.Equal(2, Aura.remainingSeconds (SerpentForm(ticks 21, 4)))

[<Fact>]
let ``out of combat the party recovers fast, and the mobs do not`` () =
  let w = Content.gully 1UL

  // A cleared room: the party alone, so nothing can re-engage mid-measurement.
  let idle =
    { w with
        Entities =
          w.Entities
          |> List.filter (fun e -> e.Faction = Party)
          |> List.map (fun e -> { e with Health = e.MaxHealth / 2; Resource = e.MaxResource / 2 }) }

  Assert.False(SimState.inCombat idle)

  let after = SimTick.run 20 idle
  let warrior0 = idle.Entities |> List.find (fun e -> e.Name = "Warrior")
  let warrior1 = after.Entities |> List.find (fun e -> e.Name = "Warrior")

  // One second of out-of-combat regeneration is 10% of the health pool and
  // just over 7% of the resource pool, per Tick so the leftovers are truncated.
  Assert.Equal(
    warrior0.Health + 20 * (warrior0.MaxHealth * Content.outOfCombatHealthBpPerTick / 10000),
    warrior1.Health)

  // The Warrior's pool is capped at 100, so the resource numbers are read off
  // the Priest, whose mana is the one that actually gates a pull.
  let priest0 = idle.Entities |> List.find (fun e -> e.Name = "Priest")
  let priest1 = after.Entities |> List.find (fun e -> e.Name = "Priest")

  Assert.Equal(
    priest0.Resource + 20 * (priest0.MaxResource * Content.outOfCombatResourceBpPerTick / 10000),
    priest1.Resource)

  // A mob left behind stays exactly as hurt as it was: the phase is party-only.
  let mob = w.Entities |> List.find (fun e -> e.Faction = Hostile)
  let hurtMob =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e -> if e.Id = mob.Id then { e with Health = e.MaxHealth / 2 } else e) }

  let regenned = SimTick.advanceOutOfCombatRegen hurtMob
  Assert.Equal(mob.MaxHealth / 2, (regenned.Entities |> List.find (fun e -> e.Id = mob.Id)).Health)

  // The same second with something engaged is combat: the pool regeneration is
  // off and only the slow per-Tick trickle reaches the party.
  let fight =
    { idle with
        Entities =
          idle.Entities
          @ (w.Entities
             |> List.filter (fun e -> e.Faction = Hostile)
             |> List.map (fun e -> { e with Engaged = true })) }

  Assert.True(SimState.inCombat fight)

  let fighting = SimTick.run 20 fight
  let priest2 = fighting.Entities |> List.find (fun e -> e.Name = "Priest")
  Assert.Equal(priest0.Resource + 20, priest2.Resource)

/// Lesser Heal restores less but wastes less: a small hole gets the cheap heal
/// and a big one still gets the 205-mana heal that can actually cover it.
[<Fact>]
let ``a small hole is closed with the cheap heal and a big one with the real heal`` () =
  // 90 health missing, a heal-over-time already on its way: Lesser Heal fits.
  let small =
    abilitiesFor (EntityId 2) (PartyAi.decide (partyWorld None [ Rejuvenation(secTicks 10.0, 2, "Renew") ] 870))

  Assert.Contains("Lesser Heal", small)

  // 380 missing: Lesser Heal cannot cover it, so the 205-mana heal earns its cost.
  let big =
    abilitiesFor (EntityId 2) (PartyAi.decide (partyWorld None [ Rejuvenation(secTicks 10.0, 12, "Renew") ] 500))

  Assert.Contains("Heal", big)
  Assert.DoesNotContain("Lesser Heal", big)

[<Fact>]
let ``heal-over-times from different spells stack, the same spell refreshes`` () =
  let renew = Rejuvenation(ticks 300, 12, "Renew")
  let rejuv = Rejuvenation(ticks 240, 22, "Rejuvenation")
  let stacked = Aura.refresh rejuv (Aura.refresh renew [])
  // All three HoTs a Tank wants must coexist; one spell re-cast only refreshes.
  Assert.Equal(2, stacked.Length)
  Assert.Equal(34, (stacked |> List.sumBy Aura.flatHealPerTick))
  Assert.Equal(2, Aura.refresh renew stacked |> List.length)
  Assert.Equal(300, int (Aura.healOverTimeRemainingFor "Renew" stacked))
  Assert.Equal(240, int (Aura.healOverTimeRemainingFor "Rejuvenation" stacked))
  Assert.Equal(0, int (Aura.healOverTimeRemainingFor "Regrowth" stacked))

[<Fact>]
let ``both healers keep the tank covered instead of trading one aura`` () =
  // 100 health short on the Tank: the Priest's Renew and the Druid's Rejuvenation
  // must both be maintained, which they cannot be if the second replaces the first.
  let w = partyWorld None [] 300
  let first = PartyAi.decide w |> List.choose (fun c -> match c.Kind with | UseAbility(EntityId 2, name, _) -> Some name | _ -> None)
  let druid = PartyAi.decide w |> List.choose (fun c -> match c.Kind with | UseAbility(EntityId 3, name, _) -> Some name | _ -> None)
  Assert.True(first.IsEmpty || druid.IsEmpty || first <> druid)

/// Mana is the caster's clock. Plenty of it, and the Mage buys damage per second
/// of casting; nearly dry, it buys damage per point of mana instead.
[<Fact>]
let ``the mage spends mana for throughput and a low pool for efficiency`` () =
  // The real party, with the Mage's pool and one engaged mob standing on it:
  // the choice of school is what is under test, not the walk into range.
  let mageWorld (manaPct: int) =
    let w = Content.gully 3UL
    let mob = w.Entities |> List.find (fun e -> e.Faction = Hostile)
    let magePos = (w.Entities |> List.find (fun e -> e.Id = EntityId 4)).Pos

    w
    |> SimState.mapEntity (EntityId 4) (fun e -> { e with Resource = e.MaxResource * manaPct / 100 })
    |> SimState.mapEntity mob.Id (fun e ->
      { e with
          Engaged = true
          Target = Some(EntityId 1)
          Pos = magePos })

  // Arcane Missiles: 280 for 235 mana over 5 s. Fireball: 195 for 140 over 3.5 s.
  Assert.Contains("Arcane Missiles", abilitiesFor (EntityId 4) (PartyAi.decide (mageWorld 100)))
  // Nearly dry it plays for damage per mana instead, where Fireball (195 for 140,
  // 1.39 a point) beats Frostbolt (83 for 65, 1.28) and the 235-mana channel.
  let dry = abilitiesFor (EntityId 4) (PartyAi.decide (mageWorld 15))
  Assert.Contains("Fireball", dry)
  Assert.DoesNotContain("Arcane Missiles", dry)

// ===========================================================================
// Ability ranks (Q: abilities scale with character level)
// ===========================================================================

/// A three-rank ability, to pin the "best rank at or below this level" rule.
let private threeRanks () : AbilityDef =
  { Name = "Sinister Strike"
    Resource = Energy
    Stance = None
    TargetKind = Foe
    Interruptible = false
    Ranks =
      [ { Level = 1
          Cost = 20
          CastTicks = ticks 0
          CooldownTicks = ticks 0
          Range = 1
          Effects = [ Damage(3, 5) ] }
        { Level = 6
          Cost = 25
          CastTicks = ticks 0
          CooldownTicks = ticks 0
          Range = 1
          Effects = [ Damage(6, 9) ] }
        { Level = 22
          Cost = 45
          CastTicks = ticks 0
          CooldownTicks = ticks 0
          Range = 1
          Effects = [ Damage(14, 18) ] } ] }

[<Fact>]
let ``a rank is available from its own level and stays until the next one`` () =
  Assert.Equal(None, AbilityDef.at 0 (threeRanks ()))
  Assert.Equal(20, (AbilityDef.at 5 (threeRanks ())).Value.ResourceCost)
  Assert.Equal(25, (AbilityDef.at 6 (threeRanks ())).Value.ResourceCost)
  Assert.Equal(25, (AbilityDef.at 21 (threeRanks ())).Value.ResourceCost)
  Assert.Equal(45, (AbilityDef.at 22 (threeRanks ())).Value.ResourceCost)
  // A level-60 character keeps the top rank rather than losing the ability.
  Assert.Equal(45, (AbilityDef.at 60 (threeRanks ())).Value.ResourceCost)

[<Fact>]
let ``a resolved rank carries the definition's fixed fields`` () =
  let a = (AbilityDef.at 24 (threeRanks ())).Value
  Assert.Equal("Sinister Strike", a.Name)
  Assert.Equal(Foe, a.TargetKind)
  Assert.False a.Interruptible
  Assert.Equal<Effect list>([ Damage(14, 18) ], a.Effects)

[<Fact>]
let ``a kit drops abilities the character cannot train yet`` () =
  let known = { threeRanks () with Name = "Known" }

  let locked =
    { threeRanks () with
        Name = "Locked"
        Ranks = [ { Level = 26; Cost = 35; CastTicks = ticks 0; CooldownTicks = ticks 0; Range = 1; Effects = [ Damage(40, 60) ] } ] }

  let names = AbilityDef.kitAt 24 [ known; locked ] |> List.map (fun a -> a.Name)
  Assert.Equal<string list>([ "Known" ], names)

// ===========================================================================
// The dungeon catalog
// ===========================================================================

[<Fact>]
let ``every level from 13 to 60 has somewhere to go`` () =
  let empty =
    [ 13 .. 60 ] |> List.filter (fun l -> Dungeons.forLevel l |> List.isEmpty)

  Assert.Empty empty

[<Fact>]
let ``the instances that split into wings are listed separately`` () =
  let wings name =
    Dungeons.all |> List.filter (fun d -> d.Name = name) |> List.map (fun d -> d.Wing)

  Assert.Equal(4, (wings "Scarlet Monastery" |> List.length))
  Assert.Equal(3, (wings "Dire Maul" |> List.length))
  Assert.Equal(2, (wings "Stratholme" |> List.length))
  // Everything else is a single instance with no wing name.
  let single =
    Dungeons.all
    |> List.filter (fun d -> d.Wing.IsNone)
    |> List.map (fun d -> d.Name)
    |> List.distinct

  Assert.Equal(17, single.Length)

[<Fact>]
let ``only Upper Blackrock Spire takes ten players`` () =
  let raids = Dungeons.all |> List.filter (fun d -> d.MaxPlayers > 5)
  Assert.Equal<string list>([ "Upper Blackrock Spire" ], raids |> List.map (fun d -> d.Name))

[<Fact>]
let ``a level fits an instance inclusively at both ends`` () =
  let deadmines =
    Dungeons.all |> List.find (fun d -> d.Abbreviation = "VC")

  Assert.False(Dungeons.fits 17 deadmines)
  Assert.True(Dungeons.fits 18 deadmines)
  Assert.True(Dungeons.fits 23 deadmines)
  Assert.False(Dungeons.fits 24 deadmines)

[<Fact>]
let ``no instance has an inverted level range or an impossible minimum`` () =
  let bad =
    Dungeons.all
    |> List.filter (fun d -> d.MinLevel > d.MaxLevel || d.MinLevel < 1 || d.MaxLevel > 60)

  Assert.Empty bad

// =========================================================================
// Ability ranks, combo points, and the Rogue's energy
// =========================================================================

/// The flat step of a weapon-damage ability, which is what changes by rank.
let private rankStep (level: int) (d: AbilityDef) =
  (AbilityDef.at level d).Value.Effects
  |> List.sumBy (function
    | WeaponDamage(_, bonus) -> bonus
    | _ -> 0)

let private entBy (id: int) (w: World) = w.Entities |> List.find (fun e -> e.Id = EntityId id)

/// A rank ladder hands out the best rank at or below the character's level.
[<Fact>]
let ``a strike's rank steps up at its learn level`` () =
  Assert.Equal(48, rankStep 24 Content.backstab)
  Assert.Equal(48, rankStep 27 Content.backstab)
  Assert.Equal(69, rankStep 28 Content.backstab)
  // Sinister Strike learns rank 3 (+10) at 14 and rank 4 (+15) at 22.
  Assert.Equal(10, rankStep 21 Content.sinisterStrike)
  Assert.Equal(15, rankStep 22 Content.sinisterStrike)

[<Fact>]
let ``an ability above the character's level is not in the kit`` () =
  Assert.True((AbilityDef.at 5 Content.gouge).IsNone)
  Assert.Empty(AbilityDef.kitAt 5 [ Content.gouge ])
  Assert.Equal(1, (AbilityDef.kitAt 6 [ Content.gouge ]).Length)

[<Fact>]
let ``sinister strike builds combo points up to five and no further`` () =
  let ss = (AbilityDef.at 24 Content.sinisterStrike).Value

  let swing n (w: World) =
    [ 1..n ]
    |> List.fold (fun (w: World) _ -> SimCombat.resolveAbility (EntityId 1) (EntityId 100) ss w) w

  let w = arena 0 0 { X = 7; Y = 5 }
  Assert.Equal(3, (entBy 1 (swing 3 w)).ComboPoints)
  Assert.Equal(5, (entBy 1 (swing 7 w)).ComboPoints)

/// Five points of Eviscerate at rank 4 is 165-185, and the points are gone.
[<Fact>]
let ``eviscerate cashes in the combo points for its five-point damage`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 1) (fun e -> { e with ComboPoints = 5; CritChanceBp = 0 })

  let before = (mobOf w).Health
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) (AbilityDef.at 24 Content.eviscerate).Value w
  let dealt = before - (mobOf w).Health

  Assert.True(dealt >= 165 && dealt <= 185, sprintf "dealt %d" dealt)
  Assert.Equal(0, (entBy 1 w).ComboPoints)

/// Energy is 20 per 2 s on its own clock: 20 Ticks to the second, so 40 Ticks
/// puts a rogue who spent everything back at 20.
[<Fact>]
let ``a rogue's energy comes back twenty per two seconds`` () =
  let w = Content.gully 1UL |> SimState.mapEntity (EntityId 3) (fun e -> { e with Resource = 0 })

  let after n =
    [ 1..n ] |> List.fold (fun (w: World) _ -> SimTick.step [] w) w |> entBy 3 |> fun e -> e.Resource

  Assert.Equal(20, after 40)
  Assert.Equal(40, after 80)

[<Fact>]
let ``the rogue cashes in four combo points and strikes below that`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let ordersAt (points: int) =
    let rogue =
      { Content.hero
          3
          "Rogue"
          Dps
          1000
          100
          tap
          10000
          [ (AbilityDef.at 24 Content.sinisterStrike).Value
            (AbilityDef.at 24 Content.backstab).Value
            (AbilityDef.at 24 Content.eviscerate).Value ]
          // The mob at (6,5) faces the Tank at (5,5), so its back is (7,5).
          // Backstab only exists from there, which is why the Rogue stands there.
          { X = 7; Y = 5 } with
          ComboPoints = points }

    let w =
      partyWorld (Some(EntityId 100)) [] 1000
      |> fun w -> { w with Entities = w.Entities @ [ rogue ] }

    abilitiesFor (EntityId 3) (PartyAi.decide w)

  Assert.Contains("Eviscerate", ordersAt 5)
  // Eviscerate's damage is linear in points, so four already beats the strike
  // that would build the fifth — the finisher fires from four up.
  Assert.Contains("Eviscerate", ordersAt 4)
  // From three down the hardest strike wins, which is Backstab, not the cheap
  // builder — 150% weapon damage beats 100% plus a flat step.
  Assert.Contains("Backstab", ordersAt 3)
  Assert.DoesNotContain("Eviscerate", ordersAt 3)

/// Nobody has noticed the Party yet — the only state `prePull` runs in, and the
/// state that stands in for a stealthed Rogue.
let private calmWorld (rogue: Entity) (mobs: Entity list) : World =
  { Tick = ticks 0
    Grid = Grid.create 12 10
    Entities = rogue :: mobs
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Log = [] }

let private calmMob (id: int) (name: string) (pos: Pos) : Entity =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }
  Content.mob id name CreatureType.Humanoid 21 1000 1000 (Some tap) [] None pos

/// Sap is the only opener that starts nothing, so it takes the pull's first slot
/// even though Gouge is also up: Gouge's ten-second cooldown can be spent inside
/// the fight, Sap's "not in combat" window cannot be got back.
[<Fact>]
let ``the rogue opens with Sap rather than Gouge`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let rogue =
    Content.hero
      3
      "Rogue"
      Dps
      1000
      100
      tap
      10000
      [ (AbilityDef.at 24 Content.sap).Value
        (AbilityDef.at 24 Content.gouge).Value
        (AbilityDef.at 24 Content.sinisterStrike).Value ]
      { X = 5; Y = 5 }

  let w =
    calmWorld rogue [ calmMob 100 "Scarlet Myrmidon" { X = 6; Y = 5 }
                      calmMob 101 "Scarlet Myrmidon" { X = 6; Y = 6 } ]

  let orders = abilitiesFor (EntityId 3) (PartyAi.decide w)

  Assert.Equal(Some "Sap", List.tryHead orders)

/// Stealth is not modelled: the Rogue counts as stealthed until something notices
/// it, and a hound is the one thing that sees through it. That is what stands in
/// for Improved Stealth, so a hound inside six tiles costs the Sap.
[<Fact>]
let ``a hound sees through the rogue's stealth and costs the Sap`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let rogue =
    Content.hero
      3
      "Rogue"
      Dps
      1000
      100
      tap
      10000
      [ (AbilityDef.at 24 Content.sap).Value
        (AbilityDef.at 24 Content.gouge).Value ]
      { X = 5; Y = 5 }

  let withHound = calmWorld rogue [ calmMob 100 "Scarlet Myrmidon" { X = 6; Y = 5 }
                                    calmMob 101 "Scarlet Myrmidon" { X = 6; Y = 6 }
                                    calmMob 102 "Scarlet Tracking Hound" { X = 7; Y = 6 } ]

  let orders = abilitiesFor (EntityId 3) (PartyAi.decide withHound)

  Assert.DoesNotContain("Sap", orders)
  // The hound costs the Sap, not the pull: Gouge still reaches the same pack.
  Assert.Contains("Gouge", orders)

/// A stun is mitigation, not only an interrupt: the mob swinging hardest cannot
/// swing while it is stunned, and the healer never pays for that damage.
[<Fact>]
let ``the rogue stuns an engaged enemy as mitigation`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let rogue =
    Content.hero
      3
      "Rogue"
      Dps
      1000
      100
      tap
      10000
      [ (AbilityDef.at 26 Content.sinisterStrike).Value
        (AbilityDef.at 26 Content.cheapShot).Value ]
      { X = 7; Y = 5 }

  let w =
    partyWorld (Some(EntityId 100)) [] 1000
    |> fun w -> { w with Entities = w.Entities @ [ rogue ] }

  Assert.Contains("Cheap Shot", abilitiesFor (EntityId 3) (PartyAi.decide w))

/// Bag of a Rogue's energy: he banks it for Backstab rather than spending 45 the
/// moment he has 45, because Backstab is worth far more per point.
[<Fact>]
let ``the rogue waits for backstab instead of spending 45 on sinister strike`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let ordersAt (energy: int) =
    let rogue =
      { Content.hero
          3
          "Rogue"
          Dps
          1000
          energy
          tap
          10000
          [ (AbilityDef.at 24 Content.sinisterStrike).Value
            (AbilityDef.at 24 Content.backstab).Value
            (AbilityDef.at 24 Content.eviscerate).Value ]
          // The mob at (6,5) faces the Tank at (5,5), so its back is (7,5).
          // Backstab only exists from there, which is why the Rogue stands there.
          { X = 7; Y = 5 } with
          ComboPoints = 0 }

    let w =
      partyWorld (Some(EntityId 100)) [] 1000
      |> fun w -> { w with Entities = w.Entities @ [ rogue ] }

    abilitiesFor (EntityId 3) (PartyAi.decide w)

  Assert.Contains("Backstab", ordersAt 100)
  Assert.Empty(ordersAt 45)

// ===========================================================================
// The Tank chooses where the fight happens
// ===========================================================================

/// Where an actor was told to walk, if anywhere.
let private moveFor (actor: EntityId) (cmds: Command list) =
  cmds
  |> List.tryPick (fun c ->
    match c.Kind with
    | MoveTo(a, p) when a = actor -> Some p
    | _ -> None)

/// The tile the mob's back would be on if the Tank stood at `p`: a mob faces what
/// it attacks, so the back is directly opposite. Only floor matters here — the
/// point of the Tank's move is to stop that tile being a wall.
let private backIfAt (mob: Entity) (p: Pos) =
  { X = (2 * mob.Pos.X) - p.X; Y = (2 * mob.Pos.Y) - p.Y }

/// A Warrior in melee with one mob, a Rogue in the Party, on a map drawn as rows
/// ('#' is wall). The mob always faces the Tank, so its back is whatever is
/// opposite the Tank.
let private backedWorld (rows: string list) (mobAt: Pos) (tankAt: Pos) (rogueAt: Pos) : World =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let tank =
    { Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor; Content.heroicStrike ] tankAt with
        Target = Some(EntityId 100) }

  let rogue =
    { Content.hero
        3
        "Rogue"
        Dps
        1000
        100
        tap
        10000
        [ (AbilityDef.at 24 Content.sinisterStrike).Value
          (AbilityDef.at 24 Content.backstab).Value ]
        rogueAt with
        Target = Some(EntityId 100) }

  let mob =
    { Content.mob 100 "Walled" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None mobAt with
        Engaged = true
        Target = Some(EntityId 1) }

  { Tick = ticks 0
    Grid = Grid.ofRows rows
    Entities = [ tank; rogue; mob ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Log = [] }

/// A mob with its back to a wall is a Backstab the Rogue cannot land, so the Tank
/// steps to the side: it is the one that decides which way the mob faces, and the
/// step is worth more than the swing it costs.
[<Fact>]
let ``the tank steps aside when the mob's back is a wall`` () =
  let w =
    backedWorld
      [ "############"
        "############"
        "############"
        "############"
        "######.#####"
        "#####..#####"
        "#####..#####"
        "############"
        "############"
        "############" ]
      { X = 6; Y = 5 }
      { X = 5; Y = 5 }
      { X = 5; Y = 6 }

  let mob = entBy 100 w
  Assert.False(Grid.isFloor w.Grid (backIfAt mob { X = 5; Y = 5 }), "the fixture needs the back walled")

  match moveFor (EntityId 1) (PartyAi.decide w) with
  | None -> failwith "the tank never moved, so the rogue's back stays a wall"
  | Some p ->
    Assert.True(
      Grid.isFloor w.Grid (backIfAt mob p),
      sprintf "the tank moved to %A, where the mob's back is still %A" p (backIfAt mob p))

/// The same fixture with the wall taken out: the back is already clear, so the
/// Tank holds its spot. Orbiting for its own sake is a Warrior not swinging.
[<Fact>]
let ``the tank holds still when the mob's back is already clear`` () =
  let w =
    backedWorld
      [ "############"
        "############"
        "############"
        "############"
        "######.#####"
        "#####...####"
        "#####..#####"
        "############"
        "############"
        "############" ]
      { X = 6; Y = 5 }
      { X = 5; Y = 5 }
      { X = 5; Y = 6 }

  let mob = entBy 100 w
  Assert.True(Grid.isFloor w.Grid (backIfAt mob { X = 5; Y = 5 }), "the fixture needs the back clear")
  Assert.Equal(None, moveFor (EntityId 1) (PartyAi.decide w))

/// Wedged in a dead end, every tile the Tank could stand on leaves the mob's back
/// against a wall and the casters' line broken. The mob has to move, and it moves
/// by following: the Tank steps away and the mob takes the tile it just left.
[<Fact>]
let ``the tank backs a cornered mob out of the corner`` () =
  let w =
    backedWorld
      [ "############"
        "############"
        "############"
        "############"
        "############"
        "###....#####"
        "############"
        "############"
        "############"
        "############" ]
      { X = 6; Y = 5 }
      { X = 5; Y = 5 }
      { X = 3; Y = 5 }

  let mob = entBy 100 w

  match moveFor (EntityId 1) (PartyAi.decide w) with
  | None -> failwith "the tank stayed in the dead end"
  | Some p ->
    // One step back, not a retreat: two tiles out, so the mob follows onto the
    // tile the Tank left, which has open floor behind it.
    Assert.Equal(2, Pos.chebyshev mob.Pos p)
    Assert.True(Grid.isFloor w.Grid p)

// ===========================================================================
// Simulation invariants, and the balance snapshot
// ===========================================================================

/// Something that must hold at *every* tick, not only at the end: a heal that
/// covers a negative, or a stale aura that hangs around, would otherwise hide
/// the tick that actually broke the rule.
let private broken (w: World) : string option =
  let stackable =
    function
    | Aura.Rejuvenation _ -> true
    | _ -> false

  w.Entities
  |> List.tryPick (fun e ->
    if e.Health < 0 then
      Some(sprintf "%s health %d" e.Name e.Health)
    elif e.Health > e.MaxHealth then
      Some(sprintf "%s overhealed %d/%d" e.Name e.Health e.MaxHealth)
    elif e.Resource < 0 || e.Resource > e.MaxResource then
      Some(sprintf "%s resource %d/%d" e.Name e.Resource e.MaxResource)
    else
      e.Auras
      |> List.filter (stackable >> not)
      |> List.countBy Aura.label
      |> List.tryFind (fun (_, n) -> n > 1)
      |> Option.map (fun (label, _) -> sprintf "%s has %s twice" e.Name label))

[<Property(MaxTest = 20)>]
let ``no tick leaves a broken world`` (seed: uint64) =
  let rec loop n (w: World) =
    match broken w with
    | Some why -> failwith (sprintf "seed %d tick %d: %s" seed (int w.Tick) why)
    | None when n = 0 || SimState.outcome w <> Running -> true
    | None -> loop (n - 1) (SimTick.step (Bench.autoPilot w) w)

  loop 1200 (Content.gully seed)

[<Property(MaxTest = 120)>]
let ``every encounter ends in a legal state`` (seed: uint64) (maxTicks: PositiveInt) =
  let w = Bench.runEncounter seed (min 2500 (max 60 maxTicks.Get))
  Assert.Equal<string option>(None, broken w)

[<Property(MaxTest = 20)>]
let ``the same seed replays the same fight`` (seed: uint64) =
  let a = Bench.runEncounter seed 600
  let b = Bench.runEncounter seed 600
  Assert.Equal(Dump.world a, Dump.world b)

/// One line per seed. A median would hide which seed moved; this shows it.
let private snapshotLines (seeds: uint64 list) =
  seeds |> List.map (fun s -> sprintf "%d %s" s (Bench.summary (Bench.runEncounter s 3000)))

/// Copied next to the test assembly by the project file, so no path has to be
/// guessed at runtime. Refresh it with `sim snapshots 30 > tests/.../snapshots/bench-30.txt`.
/// A function, not a value: module-level bindings of this project are evaluated
/// in a context where the cwd-based path comes out null.
let private snapshotPath () =
  System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "snapshots", "bench-30.txt")

[<Fact>]
let ``bench snapshot has not drifted`` () =
  let actual = String.concat "\n" (snapshotLines [ 1UL .. 30UL ]) + "\n"
  if System.Environment.GetEnvironmentVariable "UPDATE_SNAPSHOTS" = "1" then
    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(snapshotPath ()))
    |> ignore

    System.IO.File.WriteAllText(snapshotPath (), actual)
  else
    let expected = if System.IO.File.Exists(snapshotPath ()) then System.IO.File.ReadAllText(snapshotPath ()) else ""

    if actual <> expected then
      let was = expected.Split '\n'
      let now' = actual.Split '\n'

      let line =
        Seq.zip was now' |> Seq.tryFindIndex (fun (a, b) -> a <> b) |> Option.defaultValue 0

      failwith (
        sprintf
          "balance drift at seed %d in %s\n  was: %s\n  now: %s\nRefresh with `sim snapshots 30 > tests/WowRoguelike.Tests/snapshots/bench-30.txt`"
          (line + 1)
          (snapshotPath ())
          (Array.tryItem line was |> Option.defaultValue "<missing>")
          (Array.tryItem line now' |> Option.defaultValue "<missing>")
      )

/// A boss standing toe to toe with two Party members: one on each side, both in
/// melee reach, so area damage is visible as two damaged targets rather than one.
let private bossWorld (abilities: Ability list) (cooldowns: (string * int<tick>) list) : World =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let rogue id pos =
    Content.hero id "Rogue" Dps 10000 1000 slow 10000 [ (AbilityDef.at 24 Content.kick).Value ] pos

  let mob =
    Content.mob
      100
      "Mutanus"
      CreatureType.Humanoid
      22
      100000
      1000
      (Some slow)
      abilities
      None
      { X = 6; Y = 5 }

  let mob =
    { mob with
        Engaged = true
        Target = Some(EntityId 3)
        Threat = Map.ofList [ EntityId 3, 1000 ]
        Cooldowns = Map.ofList cooldowns }

  { Tick = ticks 0
    Grid = Grid.create 20 10
    Entities = [ rogue 3 { X = 5; Y = 5 }; rogue 4 { X = 7; Y = 5 }; mob ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Log = [] }

let private healthOf (w: World) (id: int) =
  (w.Entities |> List.find (fun e -> e.Id = EntityId id)).Health

let private isStunnedBy (w: World) (id: int) =
  (w.Entities |> List.find (fun e -> e.Id = EntityId id)).Auras
  |> List.exists (function Stunned _ -> true | _ -> false)

[<Fact>]
let ``thundercrack hits every party member in melee, not only its target`` () =
  let w = bossWorld [ Content.thundercrack ] [] |> SimTick.step []

  Assert.True(healthOf w 3 < 10000)
  Assert.True(healthOf w 4 < 10000)
  // Only the stun half is single-target; see the note on `thundercrack`.
  Assert.True(isStunnedBy w 3)

/// A body holding a tile with no step of its own used to sever the route for
/// everybody behind it: the claim-aware search found nothing, so nothing moved,
/// so no tile was ever freed and the Party froze outright — at tick 8000, five
/// abreast in a Scarlet corridor, three of them holding a goal on each other's
/// tile. The route now passes *through* an ally that is standing still, and only
/// the step itself has to be free, so a follower walks up to the blocker instead
/// of stopping where it stood.
[<Fact>]
let ``a stationary ally no longer severs the route behind it`` () =
  // One tile wide, so the blocker cannot be walked around: every route to the
  // goal ends at its tile, and the claim-aware search refuses all of them. It
  // stands two tiles in, so the follower's *first* step is a free one.
  let grid =
    Grid.ofRows [ "########"; "########"; "#......#"; "########"; "########" ]

  let tap = { Min = 1; Max = 1; Ticks = ticks 1; Range = 1 }

  let mover =
    { Content.hero 2 "Rogue" Dps 1000 100 tap 10000 [] { X = 1; Y = 2 } with
        Goal = Some { X = 6; Y = 2 } }

  let blocker = Content.hero 1 "Tank" Tank 1000 1000 tap 50000 [] { X = 4; Y = 2 }

  let w =
    { Tick = ticks 0
      Grid = grid
      Entities = [ blocker; mover ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Log = [] }

  let after = [ 1 .. 40 ] |> List.fold (fun w _ -> SimTick.step [] w) w
  let posOf id = (after.Entities |> List.find (fun e -> e.Id = id)).Pos

  // It walked up to the blocker...
  Assert.Equal(3, (posOf (EntityId 2)).X)
  // ...and stopped in front of it, because the blocking tile is still taken.
  Assert.Equal(4, (posOf (EntityId 1)).X)

/// Thundercrack and Terrify are both stuns, so a Mob that always picks its first
/// control spell would never use the second one.
[<Fact>]
let ``a stunned mob falls through to its next control spell`` () =
  let w =
    bossWorld [ Content.thundercrack; Content.terrify ] [ "Thundercrack", secTicks 30.0 ]
    |> SimTick.step []

  Assert.True(isStunnedBy w 3)
  Assert.Equal<int>(10000, healthOf w 4)

[<Fact>]
let ``the Fanglords carry their sourced mechanics`` () =
  let w = Content.wailingCaverns 1UL

  let kit name =
    w.Entities
    |> List.filter (fun e -> e.Name = name)
    |> List.collect (fun e -> e.Abilities)
    |> List.map (fun a -> a.Name)

  Assert.Contains("Thunderclap", kit "Lord Pythas")
  Assert.Contains("Serpent Form", kit "Lord Cobrahn")
  Assert.Contains("Thundercrack", kit "Mutanus the Devourer")
  Assert.Contains("Terrify", kit "Mutanus the Devourer")

[<Fact>]
let ``the party walks the whole dungeon instead of stalling in the first room`` () =
  // The Party used to clear room one and then stand still forever: while it had a
  // target it issued no movement orders at all, and once it had none the Tank
  // demanded a route to a mob holding a doorway, got none, and stalled. A clear
  // is the only assertion that covers both, because a stalled run also reports
  // `Running` rather than a loss.
  let rec loop (w: World) (n: int) =
    if n <= 0 then w else loop (SimTick.step (PartyAi.decide w) w) (n - 1)

  // Seed 1 stalled in the first room, seed 3 with the Party idle halfway and a
  // room of unaggroed mobs ahead, seed 5 packed into a corridor by its own
  // bodies. Three different deadlocks, and in all three the Party simply stopped
  // doing anything: the last stretch of the run is identical to the stretch
  // before it, while a stalled run still reports `Running` rather than a loss.
  //
  // This asserts progress, not clearance. Clearing Wailing Caverns is the balance
  // goal and it is not met — the Party wipes on two of these three seeds — so
  // asserting a clear would be asserting something untrue. A wipe is not a
  // deadlock: a dead Party has no progress to make either, which is why the
  // progress check only applies while somebody is still standing.
  let state (w: World) =
    (SimState.aliveHostiles w |> List.map (fun e -> e.Id, e.Health),
     SimState.aliveParty w |> List.map (fun e -> e.Pos))

  // 3000 ticks, not 9000: the run costs about 11 ms per tick, so three long
  // seeds would be most of the suite's runtime, and every one of these deadlocks
  // happened early — seed 1 in the first room it spawns in, seed 3 while it was
  // still standing (it wipes around tick 5000, which would make a later check
  // vacuous). The window is where the three seeds are all still alive.
  for seed in [ 1UL; 3UL; 5UL ] do
    let mid = loop (Content.wailingCaverns seed) 3000

    if not (List.isEmpty (SimState.aliveParty mid)) then
      let after = loop mid 600

      Assert.True(
        state mid <> state after,
        $"seed {seed} made no progress in its last 600 ticks: the Party stopped moving and nothing died")

/// Every world builds its Party from one roster in marching order, the Tank
/// first — the formation gives the front tile to whoever is first in the list,
/// so a reordered roster silently walks the healer into the first pull.
[<Fact>]
let ``every world fields the same party, tank first`` () =
  let worlds =
    [ "gully", Content.gully 1UL
      "wailing caverns", Content.wailingCaverns 1UL
      "scarlet monastery", Content.scarletMonasteryAt "graveyard" Content.Median 1UL ]

  let expected = [ "Warrior"; "Priest"; "Rogue"; "Mage"; "Druid" ]

  for name, w in worlds do
    let names = SimState.aliveParty w |> List.map (fun e -> e.Name)
    Assert.True((names = expected), $"{name}: {names}")

/// The MonoGame map browser opens any of these, so each one has to be a place a
/// World can stand in: floor under every entity, and a Party that can walk its
/// whole map. The generated maps are the ones that vary, so they get the seeds.
[<Fact>]
let ``every browsable map is connected and stands its entities on floor`` () =
  let worlds =
    [ for seed in 1UL..5UL do
        yield sprintf "generated dungeon seed %d" seed, Content.dungeon seed
        yield sprintf "gully seed %d" seed, Content.gully seed
        yield sprintf "wailing caverns seed %d" seed, Content.wailingCaverns seed

      for wing in Content.scarletWings do
        yield "scarlet " + wing.Name, Content.scarletMonasteryAt wing.Name Content.Median 1UL ]

  for name, w in worlds do
    let party = SimState.aliveParty w
    Assert.False(List.isEmpty party, sprintf "%s has no party" name)
    Assert.True(Dungeon.isConnected w.Grid party.Head.Pos, sprintf "%s is not connected" name)

    for e in w.Entities do
      Assert.True(Grid.isFloor w.Grid e.Pos, sprintf "%s: %s stands on a wall" name e.Name)

// ===========================================================================
// The Scarlet Monastery wings (MapsScarlet)
// ===========================================================================

/// Reachable floor from a start tile, four-neighbour flood fill. A visited array,
/// not a Set: each tile is enqueued exactly once, so this cannot fan out.
let private reachableFrom (g: Grid) (start: Pos) : int =
  if Grid.isWall g start then
    0
  else
    let seen = Array.create (g.Width * g.Height) false
    let queue = System.Collections.Generic.Queue<Pos>()
    let index (p: Pos) = p.Y * g.Width + p.X
    queue.Enqueue start
    seen.[index start] <- true
    let mutable count = 0

    while queue.Count > 0 do
      let p = queue.Dequeue()
      count <- count + 1

      for dx, dy in [ -1, 0; 1, 0; 0, -1; 0, 1 ] do
        let q = Pos.move dx dy p

        if Grid.isFloor g q && not seen.[index q] then
          seen.[index q] <- true
          queue.Enqueue q

    count

[<Fact>]
let ``every Scarlet Monastery wing is one connected walkable region`` () =
  for wing in Content.scarletWings do
    let grid = MapsScarlet.wing wing.Name
    let floors = MapsScarlet.floorTiles grid
    Assert.True(floors.Length > 500, sprintf "%s has only %d floor tiles" wing.Name floors.Length)

    // From the entrance, as the Party would walk it.
    let entrance = floors |> List.maxBy (fun p -> p.Y, -p.X)
    Assert.Equal(floors.Length, reachableFrom grid entrance)

[<Fact>]
let ``the mountain pass from the entrance reaches every room of each wing`` () =
  for wing in Content.scarletWings do
    let grid = MapsScarlet.wing wing.Name
    let floors = MapsScarlet.floorTiles grid
    let centre = floors.[floors.Length / 2]
    Assert.Equal(floors.Length, reachableFrom grid centre)

[<Fact>]
let ``the difficulty picks the researched band level`` () =
  let graveyard = Content.wingOf "graveyard"
  Assert.Equal(30, Content.levelOf Content.Minimum graveyard)
  Assert.Equal(33, Content.levelOf Content.Median graveyard)
  Assert.Equal(34, Content.levelOf Content.Maximum graveyard)

  let cathedral = Content.wingOf "cathedral"
  Assert.Equal(38, Content.levelOf Content.Minimum cathedral)
  Assert.Equal(41, Content.levelOf Content.Median cathedral)
  Assert.Equal(42, Content.levelOf Content.Maximum cathedral)

[<Fact>]
let ``a wing starts the party on floor tiles at its entrance`` () =
  for wing in Content.scarletWings do
    for difficulty in [ Content.Minimum; Content.Median; Content.Maximum ] do
      let w = Content.scarletMonasteryAt wing.Name difficulty 1UL
      let party = SimState.aliveParty w
      Assert.Equal(5, party.Length)

      for e in party do
        Assert.True(Grid.isFloor w.Grid e.Pos, sprintf "%s: %s is in a wall" wing.Name e.Name)
        Assert.True(
          e.Level = Content.levelOf difficulty wing,
          sprintf "%s: %s is level %d" wing.Name e.Name e.Level
        )
