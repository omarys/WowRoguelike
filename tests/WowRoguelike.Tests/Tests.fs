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
    Content.hero 1 "Tank" Tank 1000 slow 50000 [] { X = 5; Y = 5 }

  let dps =
    Content.hero 2 "Dps" Dps 1000 slow 10000 [] dpsPos

  let mob =
    Content.mob 100 "Mob" 10000 (Some slow) [] None { X = 6; Y = 5 }

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
  Assert.Equal<EntityId option>(Some(EntityId 1), Sim.resolveTarget (mobOf w) w)

[<Fact>]
let ``a challenger past the melee margin takes aggro`` () =
  // 1101 vs 1000 is over 10%, and 7,5 is adjacent to the Mob.
  let w = arena 1000 1101 { X = 7; Y = 5 }
  Assert.Equal<EntityId option>(Some(EntityId 2), Sim.resolveTarget (mobOf w) w)

[<Fact>]
let ``a challenger below the ranged margin does not take aggro`` () =
  // 1150 is over 10% but under 30%, and this DPS is far away.
  let w = arena 1000 1150 { X = 18; Y = 5 }
  Assert.Equal<EntityId option>(Some(EntityId 1), Sim.resolveTarget (mobOf w) w)

[<Fact>]
let ``a challenger past the ranged margin takes aggro`` () =
  let w = arena 1000 1301 { X = 18; Y = 5 }
  Assert.Equal<EntityId option>(Some(EntityId 2), Sim.resolveTarget (mobOf w) w)

/// The Tank stance is what makes the role work, and the margin is the reason
/// 5x threat is enough rather than merely necessary.
[<Fact>]
let ``the tank stance multiplies threat generation`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let tank = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
  let w = Sim.addThreat (EntityId 100) tank 10 w
  Assert.Equal<int>(50, Sim.threatOf (EntityId 1) (mobOf w))

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
    Content.hero 3 "Rogue" Dps 1000 slow 10000 [ Content.kick ] { X = 5; Y = 5 }

  let mob =
    Content.mob 100 "Caster" 10000 (Some slow) [ ability ] None { X = 6; Y = 5 }

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
    Sim.applyCommand
      { ApplyAt = ticks 1
        Kind = UseAbility(EntityId 3, "Kick", EntityId 100) }
      w

  let mob = mobOf w
  Assert.True(mob.Casting.IsNone)
  Assert.Equal<int<tick>>(Sim.interruptLockout, lockoutOf mob)

[<Fact>]
let ``an interrupt cannot cancel a cast that is not interruptible`` () =
  let w = casterWorld Content.serpentForm

  let w =
    Sim.applyCommand
      { ApplyAt = ticks 1
        Kind = UseAbility(EntityId 3, "Kick", EntityId 100) }
      w

  Assert.True((mobOf w).Casting.IsSome)

[<Fact>]
let ``damage wakes a sleeper`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = Sim.mapEntity (EntityId 2) (fun e -> { e with Auras = [ Sleeping(secTicks 15.0) ] }) w
  Assert.True(Sim.isSleeping (w.Entities |> List.find (fun e -> e.Id = EntityId 2)))
  let w = Sim.dealDamage (EntityId 2) 1 w
  Assert.False(Sim.isSleeping (w.Entities |> List.find (fun e -> e.Id = EntityId 2)))

/// Moves are cancelled while asleep, so sleeping a member actually removes them.
[<Fact>]
let ``a sleeping entity does not move`` () =
  let w = arena 0 0 { X = 7; Y = 5 }

  let w =
    Sim.applyCommand
      { ApplyAt = ticks 1
        Kind = MoveTo(EntityId 2, { X = 18; Y = 5 }) }
      w

  let w = Sim.mapEntity (EntityId 2) (fun e -> { e with Auras = [ Sleeping(secTicks 15.0) ] }) w
  let before = (w.Entities |> List.find (fun e -> e.Id = EntityId 2)).Pos
  let w = Sim.run 30 w
  let after = (w.Entities |> List.find (fun e -> e.Id = EntityId 2)).Pos
  Assert.Equal<Pos>(before, after)

[<Fact>]
let ``serpent form blocks casting`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = Sim.mapEntity (EntityId 1) (fun e -> { e with Auras = [ SerpentForm(secTicks 10.0, 25) ] }) w
  Assert.False(Sim.canCast (w.Entities |> List.find (fun e -> e.Id = EntityId 1)))

[<Fact>]
let ``serpent form adds physical damage to swings`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let w = Sim.mapEntity (EntityId 2) (fun e -> { e with Auras = [ SerpentForm(secTicks 10.0, 25) ] }) w
  Assert.Equal<int>(25, Sim.serpentBonus (w.Entities |> List.find (fun e -> e.Id = EntityId 2)))

// ===========================================================================
// Determinism and golden replay (ADR-0001 / Q25a)
// ===========================================================================

let private record (seed: uint64) (n: int) : Command list list * World =
  let rec loop (w: World) (remaining: int) (acc: Command list list) =
    if remaining <= 0 then
      List.rev acc, w
    else
      let cmds = Bench.autoPilot w
      loop (Sim.step cmds w) (remaining - 1) (cmds :: acc)

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
  let replayed = cmds |> List.fold (fun w c -> Sim.step c w) (Content.gully 11UL)
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
  let living = w.Entities |> List.filter Sim.alive
  let tiles = living |> List.map (fun e -> e.Pos)
  List.length tiles = (tiles |> List.distinct |> List.length)

/// This is the invariant that caught two entities stepping into one tile.
[<Property>]
let ``no two living entities ever share a tile`` (seed: int) =
  let seed = uint64 (abs seed)

  let rec loop (w: World) (n: int) =
    if n <= 0 then true
    elif not (noSharedTiles w) then false
    else loop (Sim.step (Bench.autoPilot w) w) (n - 1)

  loop (Content.gully (seed + 1UL)) 400

[<Property>]
let ``the encounter always resolves`` (seed: int) =
  let w = Bench.runEncounter (uint64 (abs seed) + 1UL) 3000
  Sim.outcome w <> Running

[<Property>]
let ``health stays within zero and maximum`` (seed: int) =
  let w = Bench.runEncounter (uint64 (abs seed) + 1UL) 3000

  w.Entities
  |> List.forall (fun e -> e.Health >= 0 && e.Health <= e.MaxHealth)

/// Every Tick is integer, and time only moves forward.
[<Property>]
let ``the tick counter advances by exactly one per step`` (n: int) =
  let n = abs n % 200
  let w = Sim.run n (Content.gully 1UL)
  w.Tick = ticks n
