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
    RestTicks = ticks 0
    Grid = Grid.create 20 10
    Entities = [ tank; dps; mob ]
    Pending = []
    Rng = Rng.streamsOf 1UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
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
              Remaining = secTicks 2.0
              Pushes = 0 } }

  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 20 10
    Entities = [ rogue; mob ]
    Pending = []
    Rng = Rng.streamsOf 1UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
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
  // Kick now carries Improved Kick's silence, and a silence does end a cast — that is
  // what a silence is for. This test is about `Interrupt`, so it hands the rogue a
  // Kick with the silence stripped and asks only the question it names.
  let interruptOnly =
    { (AbilityDef.at 24 Content.kick).Value with
        Effects = [ Interrupt ] }

  let w =
    casterWorld Content.serpentForm
    |> SimState.mapEntity (EntityId 3) (fun e -> { e with Abilities = [ interruptOnly ] })

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
// What a mob does with its turn
// ===========================================================================

// The rotation is chosen by effect shape, so an ability whose shape is not named
// there is content the sim never fires. These pin the three that were missing and
// would have caught the bug that made every Scarlet Monastery caster swing a
// mace instead of casting its bolt.

let private mobTurn (abilities: Ability list) (allies: Entity list) =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let rogue =
    Content.hero 3 "Rogue" Dps 1000 1000 slow 10000 [ (AbilityDef.at 24 Content.kick).Value ] { X = 5; Y = 5 }

  let mob =
    Content.mob 100 "Caster" CreatureType.Humanoid 21 10000 1000 (Some slow) abilities None { X = 6; Y = 5 }

  let mob =
    { mob with
        Engaged = true
        Target = Some(EntityId 3)
        Threat = Map.ofList [ EntityId 3, 1000 ] }

  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 20 10
    Entities = rogue :: mob :: allies
    Pending = []
    Rng = Rng.streamsOf 1UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }
  |> SimTick.mobDecisions

[<Fact>]
let ``a hostile caster spends its turn on its bolt`` () =
  let mob = mobOf (mobTurn [ Content.lightningBolt ] [])

  match mob.Casting with
  | Some c -> Assert.Equal("Lightning Bolt", c.Ability.Name)
  | None -> failwith "the caster swung its weapon instead of casting"

[<Fact>]
let ``a hostile caster polymorphs instead of swinging`` () =
  let mob = mobOf (mobTurn [ Content.polymorph ] [])

  match mob.Casting with
  | Some c -> Assert.Equal("Polymorph", c.Ability.Name)
  | None -> failwith "polymorph is in the kit and was never used"

[<Fact>]
let ``a hostile healer renews a wounded ally`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  // Renew (6075) as the Scarlet Chaplains carry it: instant, 12 a Tick for 15 s,
  // on an ally. Built here rather than reached for in `Content` because the
  // content's copy is private to the roster that uses it.
  let renew =
    { Content.lightningBolt with
        Name = "Renew"
        CastTicks = ticks 0
        ResourceCost = 50
        TargetKind = Ally
        Effects = [ HealOverTime(secTicks 15.0, 12) ] }

  let hurt =
    Content.mob 101 "Chaplain" CreatureType.Humanoid 39 1000 5000 (Some slow) [] None { X = 7; Y = 5 }

  let hurt =
    { hurt with
        Engaged = true
        Health = 300
        Target = Some(EntityId 3)
        Threat = Map.ofList [ EntityId 3, 1000 ] }

  let w = mobTurn [ renew ] [ hurt ]
  let healed = w.Entities |> List.find (fun e -> e.Id = EntityId 101)

  match healed.Auras |> List.tryPick (function Rejuvenation(_, perTick, source) -> Some(source, perTick) | _ -> None) with
  | Some(source, perTick) ->
    // Instant, so it is not a cast in flight: the HoT is already on the body.
    Assert.Equal("Renew", source)
    Assert.Equal(12, perTick)
  | None -> failwith "a heal-over-time was in the kit and never cast"

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
/// The five-second rule: a zeroed pool is whole again inside five seconds, which
/// is the whole of the wait between pulls. Ten seconds would make eating and
/// drinking worth modelling; five makes them pointless, which is the intent.
[<Fact>]
let ``out of combat a zeroed party is whole again in five seconds`` () =
  let w = Content.gully 1UL

  let drained =
    { w with
        Entities =
          w.Entities
          |> List.filter (fun e -> e.Faction = Party)
          |> List.map (fun e -> { e with Health = 1; Resource = 0 }) }

  Assert.False(SimState.inCombat drained)

  let fourSeconds = SimTick.run 80 drained
  let fiveSeconds = SimTick.run 100 drained

  // The restore is a step, not a slope: five seconds is the *wait*, and nothing comes
  // back during it. Half a second later the pool has snapped up at 10% a Tick.
  let snapped = SimTick.run 110 drained

  let priest (world: World) = world.Entities |> List.find (fun e -> e.Name = "Priest")

  Assert.True((priest fourSeconds).Resource < (priest fourSeconds).MaxResource, "80 ticks should not fill the pool")
  Assert.True((priest fourSeconds).Health < (priest fourSeconds).MaxHealth, "80 ticks should not heal a corpse to full")
  Assert.True((priest fiveSeconds).Health < (priest fiveSeconds).MaxHealth, "the wait alone restores nothing")

  for who in [ "Priest"; "Mage"; "Warrior" ] do
    let e = snapped.Entities |> List.find (fun x -> x.Name = who)
    Assert.Equal(e.MaxHealth, e.Health)

  Assert.Equal((priest snapped).MaxResource, (priest snapped).Resource)

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
      RestTicks = ticks 0
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
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let w =
    SimCombat.applyCommand
      { ApplyAt = ticks 1
        Kind = MoveTo(EntityId 1, Bench.concaveGoal) }
      w

  let w = SimTick.run 400 w
  Assert.Equal<Pos>(Bench.concaveGoal, (w.Entities |> List.head).Pos)

// ===========================================================================
// Movement contention (one tile, one destination)
// ===========================================================================

/// The movement ruling: a tile may be one body's destination, never two, and
/// a body in motion may be passed through. These tests pin the executor half
/// of that ruling — how a contended destination is decided, and that the
/// degenerate claim `separateStacked` can leave behind never freezes anyone.
let private walker (id: int) (name: string) (pos: Pos) : Entity =
  Content.hero
    id
    name
    Dps
    1000
    1000
    { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }
    10000
    []
    pos

let private contentionWorld (entities: Entity list) : World =
  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 20 10
    Entities = entities
    Pending = []
    Rng = Rng.streamsOf 1UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }

let private bodyAt (id: int) (w: World) = w.Entities |> List.find (fun e -> e.Id = EntityId id)

let private steppedClean (w: World) =
  let living = w.Entities |> List.filter SimState.alive
  let tiles = living |> List.map (fun e -> e.Pos) |> List.distinct
  List.length tiles = List.length living

/// `separateStacked` can push a body onto the very tile it had claimed,
/// leaving `Destination = Some own.Pos` pending forever. The step can never
/// complete — the body is "arriving" where it already stands — and while it
/// waits, `stepToward` will not re-plan for it, so the goal behind it dies
/// too. Measured on `armory` seed 1, tick 12500: the Warrior sat on its own
/// destination at (165,101) from tick 12500 to the end of the run.
[<Fact>]
let ``a claim on the body's own tile settles instead of freezing`` () =
  let tank =
    { walker 1 "Tank" { X = 10; Y = 5 } with
        Goal = Some { X = 12; Y = 5 }
        Path = [ { X = 11; Y = 5 } ]
        Destination = Some { X = 10; Y = 5 }
        MoveTicksLeft = ticks 1
        MoveTicksTotal = ticks 1 }

  let w = contentionWorld [ tank ]

  // The degenerate claim is gone the first time the executor sees it — either
  // settled outright or already re-planned into a real step.
  let w1 = SimTick.step [] w
  let t1 = bodyAt 1 w1
  Assert.True(t1.Destination <> Some t1.Pos, "a step onto the body's own tile must settle, not stay pending")

  // A standing Goal re-plans from the tile the body is on.
  let wDone = SimTick.run 40 w1
  Assert.Equal<Pos>({ X = 12; Y = 5 }, (bodyAt 1 wDone).Pos)

/// The freeze this caught, reproduced with two party members standing exactly
/// as the armory pair stood at tick 12500: one body pushed onto its own
/// destination (Warrior at (165,101), destination the same tile), and one body
/// claiming that same tile from next door with a stale route (the Imp at
/// (164,101), destination (165,101), goal (163,101)). Pre-fix, the race left
/// one of them yielding with its claim intact; the yielded body stood still on
/// the contested tile, held it against the winner, and the pair stood still
/// for the rest of the run — each waiting on the other, forever. Post-fix the
/// push-victim settles, the loser drops its claim and walks on, and neither
/// ever waits on the other.
[<Fact>]
let ``two bodies claiming one tile never wait on each other`` () =
  // Listed first, like the mid-run summon was: pre-fix, list order decided the
  // race, and this order is the one that deadlocked.
  let imp =
    { walker 2 "Imp" { X = 9; Y = 5 } with
        Goal = Some { X = 8; Y = 5 }
        Path = []
        Destination = Some { X = 10; Y = 5 }
        MoveTicksLeft = ticks 1
        MoveTicksTotal = ticks 1 }

  let war =
    { walker 1 "War" { X = 10; Y = 5 } with
        Goal = Some { X = 12; Y = 5 }
        Path = [ { X = 11; Y = 5 } ]
        Destination = Some { X = 10; Y = 5 }
        MoveTicksLeft = ticks 1
        MoveTicksTotal = ticks 1 }

  let w = contentionWorld [ imp; war ]

  // The contested tile settles to the push-victim the first tick, and the
  // other's stale claim goes back within the refusal window.
  let w1 = SimTick.step [] w
  let war1 = bodyAt 1 w1
  Assert.True(war1.Destination <> Some war1.Pos, "the push-victim's own-tile claim must settle")

  let wDone =
    (w1, [ 1..80 ])
    ||> List.fold (fun w _ ->
      let w = SimTick.step [] w
      Assert.True(steppedClean w, "contention must resolve without co-occupancy")
      w)

  // Nobody is left standing where they started: the freeze never resolved for
  // either body, so reaching the goal at all is the regression.
  Assert.Equal<Pos>({ X = 8; Y = 5 }, (bodyAt 2 wDone).Pos)
  Assert.Equal<Pos>({ X = 12; Y = 5 }, (bodyAt 1 wDone).Pos)

/// Two due steps into the same free tile: the lower EntityId lands, and the
/// loser drops its claim and re-plans in the same tick — the outcome depends
/// on nothing, not list order and not who planned first.
[<Fact>]
let ``a two-way race for one tile is decided by EntityId`` () =
  let east =
    { walker 1 "East" { X = 9; Y = 5 } with
        Goal = Some { X = 12; Y = 5 }
        Path = []
        Destination = Some { X = 10; Y = 5 }
        MoveTicksLeft = ticks 1
        MoveTicksTotal = ticks 1 }

  let west =
    { walker 2 "West" { X = 11; Y = 5 } with
        Goal = Some { X = 8; Y = 5 }
        Path = []
        Destination = Some { X = 10; Y = 5 }
        MoveTicksLeft = ticks 1
        MoveTicksTotal = ticks 1 }

  let w = contentionWorld [ west; east ]
  let w1 = SimTick.step [] w

  Assert.Equal<Pos>({ X = 10; Y = 5 }, (bodyAt 1 w1).Pos)
  Assert.Equal<Pos>({ X = 11; Y = 5 }, (bodyAt 2 w1).Pos)
  Assert.True(steppedClean w1)

  // The loser re-plans rather than waiting on the winner's tile: both reach
  // their goals, around each other.
  let wDone = SimTick.run 60 w1
  Assert.Equal<Pos>({ X = 12; Y = 5 }, (bodyAt 1 wDone).Pos)
  Assert.Equal<Pos>({ X = 8; Y = 5 }, (bodyAt 2 wDone).Pos)

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

/// Arcane Rupture is one aura carrying two numbers on purpose: "increases the damage
/// **and** mana cost of your Arcane Missiles by 20%". Splitting them would let a
/// future edit buy the damage and keep the cheap cast, which is not the trade.
[<Fact>]
let ``the rupture window pays for its extra damage`` () =
  let baseWorld = arena 0 0 { X = 7; Y = 5 }

  let ruptured =
    baseWorld
    |> SimState.mapEntity (EntityId 1) (fun e ->
      { e with Auras = Aura.Ruptured(secTicks 8.0, 2000) :: e.Auras })

  let before = (baseWorld.Entities |> List.find (fun e -> e.Id = EntityId 1)).Resource

  let plain =
    SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.arcaneMissiles baseWorld

  let up =
    SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.arcaneMissiles ruptured

  let spent (w: World) =
    before - (w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Resource

  Assert.Equal<int>(Content.arcaneMissiles.ResourceCost, spent plain)
  Assert.Equal<int>(Content.arcaneMissiles.ResourceCost * 12 / 10, spent up)

[<Fact>]
let ``the rupture window hits harder`` () =
  let mage = (Content.partyOf [ "Mage" ] 60).[0]

  let dummy =
    { Content.mob 100 "Dummy" CreatureType.Humanoid 60 400000 400000 None [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some mage.Id }

  let baseWorld =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ { mage with Pos = { X = 5; Y = 5 } }; dummy ]
      Pending = []
      Rng = Rng.streamsOf 11UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let hit (w: World) =
    let health (w: World) =
      (w.Entities |> List.find (fun e -> e.Id = EntityId 100)).Health

    let after = SimCombat.resolveAbility mage.Id (EntityId 100) Content.arcaneMissiles w
    health w - health after

  // Same seed: the roll and the jitter are identical, so the two hits differ only by
  // the window's multiplier.
  let plainHit = hit baseWorld

  let upHit =
    hit (
      baseWorld
      |> SimState.mapEntity mage.Id (fun e ->
        { e with Auras = Aura.Ruptured(secTicks 8.0, 2000) :: e.Auras })
    )

  Assert.True(plainHit > 0, sprintf "a missile should hit at all, got %d" plainHit)
  Assert.Equal<int>(plainHit * 12 / 10, upHit)

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
  // Mana regenerates on vanilla's two-second server tick, not every Tick, so fifty
  // Ticks buys one application of the rate rather than fifty. Deriving the count from
  // the cadence keeps this honest if the tick rate ever moves.
  let applications = 50 / int Content.manaRegenTicks
  Assert.Equal<int>(applications * tank.ResourceRegenPerTick, tank.Resource)

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
/// A detour that walks *away* from the goal for longer than the fast budget still
/// has to route. This is the shape that broke the graveyard: from the Party's
/// pocket every walkable step increased the heuristic, so `closest` stayed on the
/// start tile, the truncated search handed back an empty path, and every caller
/// read that as "unreachable" — a whole Party parked with `goal=(28,187) path=0`
/// while fourteen mobs sat reachable.
[<Fact>]
let ``a detour longer than the fast budget still routes`` () =
  let w, h = 700, 40
  let g = Grid.create w h
  let passage = w - 3

  // One wall across the middle with a single gap near the far edge, so reaching
  // the goal means walking almost the whole width away from it first.
  for x in 1 .. w - 2 do
    if x <> passage then g.Walls.[(h / 2) * w + x] <- true

  let start = { X = 5; Y = 2 }
  let goal = { X = 5; Y = h - 3 }
  let r = Path.astar g allowAll start goal

  Assert.False(List.isEmpty r.Path)
  Assert.False(r.Truncated)
  Assert.Equal<Pos>(goal, List.last r.Path)

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

/// One wide corridor, three bodies, one Tick. `held` is the set of tiles a body will
/// still be standing on when the Tick ends, and a body whose step is *due* is only a
/// guess at vacating: this Rogue's step lands on the Hostile parked in front of it and
/// is refused, so the Rogue stays put and its tile stays occupied. Reading the guess as
/// the answer let the Ravager behind it walk onto the Rogue — measured at tick 28 of
/// `dungeon 2` seed 1, at (58,5), and reproducible on 19 of the first 60 seeds. The
/// Hostile is the one parked in front on purpose: a parked *ally* would be traded with
/// (`swapStep`), which is a different rule and would hide this one.
let private corridorTestWorld () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 1; Range = 1 }
  let rogue = Content.hero 3 "Rogue" Dps 1000 1000 slow 10000 [] { X = 5; Y = 1 }

  let parked =
    Content.mob 6 "Druid of the Fang" CreatureType.Humanoid 24 1000 0 (Some slow) [] None { X = 6; Y = 1 }

  let ravager =
    Content.mob 8 "Deviate Ravager" CreatureType.Humanoid 20 1000 0 (Some slow) [] None { X = 4; Y = 1 }

  let stepping (e: Entity) (dest: Pos) =
    { e with
        Destination = Some dest
        MoveTicksLeft = ticks 1
        MoveTicksTotal = e.MoveTicksPerTile }

  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.ofRows [ "#########"; "#.......#"; "#########" ]
    Entities = [ stepping rogue { X = 6; Y = 1 }; parked; stepping ravager { X = 5; Y = 1 } ]
    Pending = []
    Rng = Rng.streamsOf 1UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }

let private at (w: World) (id: int) = w.Entities |> List.find (fun e -> e.Id = EntityId id)

[<Fact>]
let ``a body refused its step still holds the tile it is standing on`` () =
  let w = SimTick.advanceMovement (corridorTestWorld ())

  Assert.True(noSharedTiles w, "two living entities on one tile")

  // The Rogue's step is into the parked Fang and is refused: it keeps its tile, its
  // destination, and its one tick left.
  Assert.Equal({ X = 5; Y = 1 }, (at w 3).Pos)
  Assert.Equal(Some { X = 6; Y = 1 }, (at w 3).Destination)

  // So the Ravager waits behind it instead of standing on it — and keeps the step it
  // will take the moment the corridor clears.
  Assert.Equal({ X = 4; Y = 1 }, (at w 8).Pos)
  Assert.Equal(Some { X = 5; Y = 1 }, (at w 8).Destination)

/// The other half of the same rule, so the fix cannot be "refuse contested steps": a
/// body that really does vacate this Tick frees its tile, and the body behind it moves
/// into it on the same Tick.
[<Fact>]
let ``a body that takes its step frees the tile behind it`` () =
  let w = corridorTestWorld ()

  // The Fang in front of the Rogue steps out of the way, so the Rogue's step is legal.
  let w =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e ->
            if e.Id = EntityId 6 then
              { e with
                  Destination = Some { X = 7; Y = 1 }
                  MoveTicksLeft = ticks 1
                  MoveTicksTotal = e.MoveTicksPerTile }
            else
              e) }

  let w = SimTick.advanceMovement w

  Assert.True(noSharedTiles w, "two living entities on one tile")
  Assert.Equal({ X = 6; Y = 1 }, (at w 3).Pos)
  Assert.Equal({ X = 5; Y = 1 }, (at w 8).Pos)
  Assert.Equal({ X = 7; Y = 1 }, (at w 6).Pos)

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
      RestTicks = ticks 0
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
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
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

/// A crit has to be *visible*, not just arithmetic. Nothing else in the log marks one,
/// so a talent that raises only crit chance reads as doing nothing — the first
/// measurement of Arcane Instability's crit half counted zero crits in four hundred
/// spells for exactly that reason.
[<Fact>]
let ``a critical strike says so in the log`` () =
  let w = arena 0 0 { X = 7; Y = 5 }

  // Through `rollCrit` rather than `meleeStrike`: the attack table can answer Dodge
  // or Miss first, and this test is about the wording, not the table.
  let amount, crit, w = SimCombat.rollCrit 10000 0 100 w
  Assert.True(crit, "a 100% crit chance must set the flag")
  Assert.Equal(150, amount)

  // `logLine` conses, so the newest line is the head, not the last.
  let w = SimCombat.hitLine (mobOf w) "Swing" "Target" amount crit w
  Assert.Contains("critically hits", w.Log |> List.head)

  // And a non-crit keeps the plain wording, so the two are distinguishable.
  let w = SimCombat.hitLine (mobOf w) "Swing" "Target" amount false w
  Assert.DoesNotContain("critically", w.Log |> List.head)

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

  let amount, _, _ = SimCombat.meleeStrike (mobOf w) (EntityId 1) 100 0 None w
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

  let amount, _, _ = SimCombat.meleeStrike (mobOf w) (EntityId 100) 100 0 None w
  // 100 - 25 = 75, less nothing for armour. Avoidance comes first on the table,
  // so a miss is a legitimate zero here.
  Assert.True(amount = 75 || amount = 0, sprintf "amount %d" amount)

/// A dual-wielder swings its off hand too, and for half the damage of the main
/// hand's blow.
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
        RestTicks = ticks 0
        Grid = Grid.create 20 10
        Entities = [ tank; mob ]
        Pending = []
        Rng = Rng.streamsOf seed
        PartyBuffs = []
        Quests = []
        Forecast = Forecast.empty
        TotemLedger = TotemLedger.empty
        Log = [] }
      |> SimTick.run 4
      |> fun w -> w.Log)

  // Avoidance can eat a swing, so sweep seeds for one that lands.
  Assert.True(
    logs |> List.exists (List.exists (fun l -> l.Contains "off hand")),
    System.String.Join(" | ", logs |> List.concat |> List.truncate 10)
  )

/// The off-hand weapon is the fast one: shorter interval, less damage per blow, so
/// it has to land *more often* than the main hand across a fight. With a 19-tick
/// main hand it swings every 11 ticks, which is what this counts.
[<Fact>]
let ``the off hand lands more often than the main hand`` () =
  let swing (interval: int) =
    let weapon = { Min = 30; Max = 30; Ticks = ticks interval; Range = 1 }

    let tank =
      Content.hero 1 "Tank" Tank 1000 1000 weapon 50000 [ Content.heroicStrike ] { X = 5; Y = 5 }
      |> fun e -> { e with OffHand = true }

    let mob =
      { Content.mob 100 "Mob" CreatureType.Humanoid 21 100000 0 (Some weapon) [] None { X = 6; Y = 5 } with
          Engaged = true
          Target = Some(EntityId 1) }

    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities = [ tank; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }
    |> SimTick.run 220
    |> fun w ->
      w.Log
      |> List.filter (fun l -> l.StartsWith "Tank")
      |> List.countBy (fun l -> if l.Contains "off hand" then "off" else "main")
      |> Map.ofList

  let counts = swing 19
  let main = counts |> Map.tryFind "main" |> Option.defaultValue 0
  let off = counts |> Map.tryFind "off" |> Option.defaultValue 0
  Assert.True(main > 0, "the main hand should swing at all")
  Assert.True(off > main, sprintf "off hand %d should out-swing main hand %d" off main)

/// `Talents.damageBp` has to reach a white swing: most of a melee class's damage is
/// the auto-attack, so a tree that raises damage and misses it is a tree half applied.
/// One-Handed Weapon Specialization 5/5 is the talent that proves it.
[<Fact>]
let ``the tree's damage bonus reaches a white swing`` () =
  // A 5-tick interval is short enough to land inside the window and long enough that
  // "the first swing" is unambiguous; 30..30 means the roll contributes nothing to the
  // comparison. The timer starts full (`Content.hero` sets `SwingTicksLeft = attack.Ticks`),
  // so the window has to outlast one interval.
  let firstSwing (seed: int) (damageDoneBp: int) =
    let weapon = { Min = 30; Max = 30; Ticks = ticks 5; Range = 1 }

    let tank =
      Content.hero 1 "Tank" Tank 1000 1000 weapon 50000 [ Content.heroicStrike ] { X = 5; Y = 5 }
      |> fun e -> { e with Talents = { Talents.none with DamageDoneBp = damageDoneBp } }

    let mob =
      { Content.mob 100 "Mob" CreatureType.Humanoid 21 100000 0 (Some weapon) [] None { X = 6; Y = 5 } with
          Engaged = true
          Target = Some(EntityId 1) }

    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities = [ tank; mob ]
      Pending = []
      Rng = Rng.streamsOf (uint64 seed)
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }
    |> SimTick.run 20
    |> fun w ->
      // A dodge or a parry logs a miss, so sweep seeds for a swing that lands.
      w.Log
      |> List.tryPick (fun l ->
        if l.StartsWith "Tank swings at" then
          l.Split ' ' |> Array.last |> int |> Some
        else
          None)

  let plain = [ 1..20 ] |> List.tryPick (fun s -> firstSwing s 0)
  let talented = [ 1..20 ] |> List.tryPick (fun s -> firstSwing s 1000)
  // No attack power on the fixture, so the talent's 10% is the whole difference:
  // 30 against 33.
  Assert.Equal(Some 30, plain)
  Assert.Equal(Some 33, talented)

/// The Warrior fights sword and board, and the shield's buttons are what that
/// buys: a block chance, a block value, and the two cooldowns.
[<Fact>]
let ``the warrior carries a shield`` () =
  let tank = (Content.gully 1UL).Entities |> List.find (fun e -> e.Role = Some Tank)
  // A shield's own five percent, plus Shield Specialization 5/5's five more.
  Assert.Equal(1000, tank.BlockChanceBp)
  // 8 is the gear's own block value; 9 is that plus Toughness 5/5's +15%
  // (`BlockValueBp = 1500`, folded in `derive`).
  Assert.Equal(9, tank.BlockValue)

  // The talent's other half is a trigger, not a number: five rage on a block.
  Assert.True(
    tank.Talents.Procs |> List.exists (fun p -> p.Talent = "Shield Specialization"),
    "the warrior's Shield Specialization proc is missing from his build"
  )

  for ability in [ "Shield Block"; "Shield Bash"; "Shield Wall" ] do
    Assert.True(
      tank.Abilities |> List.exists (fun a -> a.Name = ability),
      sprintf "the warrior should have %s" ability
    )

/// Toughness 5/5 — "Increases your armor value from items by [2/4/6/8/10]% and
/// the amount of damage absorbed by your shield by [3/6/9/12/15]%"
/// (`.pi/research/octowow/warrior.md` §3): the armour half lands on the gear's
/// own share — `ItemArmor`, not the total — so Agility's two-per-point is not
/// folded, and the shield half is the block value's own line.
[<Fact>]
let ``Toughness folds the gear's own armour and the shield's block`` () =
  let warrior =
    Content.partyOf [ "Warrior" ] 60 |> List.find (fun e -> e.Name = "Warrior")

  // The Tier 1 set's 8068, +10%: 8874, which is the gear's share alone — the
  // proof the talent does not touch the 202 Agility adds on top of it.
  Assert.Equal(8874, warrior.ItemArmor)
  // The shield's 8, +15%: 9.
  Assert.Equal(9, warrior.BlockValue)

/// The Rogue's OctoWoW build, checked on the kit the party is handed rather than on
/// the record holding it, for the same reason the Warrior's test does below.
[<Fact>]
let ``the rogue's OctoWoW build lands on his kit`` () =
  let rogue =
    (Content.gully 1UL).Entities |> List.find (fun e -> e.Role = Some Dps)

  let ability name = rogue.Abilities |> List.find (fun a -> a.Name = name)

  // OctoWoW's flat energy costs, against the 45/35 the file carried.
  Assert.Equal(40, (ability "Sinister Strike").ResourceCost)
  Assert.Equal(30, (ability "Eviscerate").ResourceCost)
  // Malice 5/5 and Close Quarters Combat 2/2, both crit, and both on the hero.
  Assert.Equal(2540, rogue.CritChanceBp)
  Assert.Equal(3000, rogue.BackstabCritBonusBp)
  // Improved Eviscerate 3/3: +15% on the one-point pair and the step. The gully
  // party is level 24, so rank 4 is 41/61/31 and becomes 47/70/35 on truncation.
  // Ruthlessness 3/3 sits beside the finisher and after it, because `Finisher`
  // zeroes the pool and `ComboPoint` re-adds to it.
  Assert.Equal<Effect list>([ Finisher(47, 70, 35); ComboPoint 1 ], (ability "Eviscerate").Effects)
  // Improved Kick 2/2 adds the silence beside the school lockout.
  Assert.Contains(Silence(secTicks 2.0), (ability "Kick").Effects)

/// The Mage's OctoWoW build. Four of nineteen talents are data; the rest are in
/// `.pi/research/octowow/mage.md` §5(b) with the axis each needs.
[<Fact>]
let ``the mage's OctoWoW build lands on his kit`` () =
  let mage =
    (Content.gully 1UL).Entities |> List.find (fun e -> e.Name = "Mage")

  let ability name = mage.Abilities |> List.find (fun a -> a.Name = name)

  // Improved Frostbolt 5/5, measured against the untouched row rather than as a
  // literal: the rank a level-24 mage holds is content that can move.
  let stock = (AbilityDef.at 24 Content.frostboltRanks).Value
  Assert.Equal(stock.CastTicks - secTicks 0.5, (ability "Frostbolt").CastTicks)
  // Improved Counterspell 2/2, baked into the row because a talent cannot add an
  // `Effect`.
  Assert.Contains(Silence(secTicks 4.0), (ability "Counterspell").Effects)
  // Arcane Impact 3/3, on the two spells the talent names and no others.
  Assert.Contains(CritBonus 600, (ability "Arcane Missiles").Effects)
  Assert.Contains(CritBonus 600, (ability "Arcane Explosion").Effects)
  Assert.DoesNotContain((ability "Fireball").Effects, fun e -> e = CritBonus 600)
  // Presence of Mind, a kit row rather than a talent line.
  let pom = ability "Presence of Mind"
  Assert.Equal(secTicks 180.0, pom.CooldownTicks)
  Assert.Equal<Effect list>([ Empowers(secTicks 20.0, { Empower.none with Instant = true }) ], pom.Effects)
  // Arcane Instability 3/3 is a proc, not the flat +3%/+3% the vanilla text
  // implies (payload gaps file §5): 25% on a damaging Arcane spell, +25% to that
  // hit, 2% of *base* mana, no internal cooldown. `DamageDoneBp` stays alone
  // because a proc reaches one hit and a tree-wide field would reach the swing.
  let instability =
    mage.Talents.Procs |> List.find (fun p -> p.Talent = "Arcane Instability")

  Assert.Equal(2500, instability.ChanceBp)
  Assert.Equal(OnSpellSchoolHit Arcane, instability.Event)
  Assert.Equal(AmplifyHit(12500, 200), instability.Then)
  Assert.Equal<int<tick>>(ticks 0, instability.InternalCooldown)
  Assert.Equal(0, mage.Talents.DamageDoneBp)

/// A damaging ability carries the school it deals, because `OnSpellSchoolHit` reads
/// that field: a Fireball filed under Frost would be silent, not loud.
[<Fact>]
let ``a damaging ability says which school it deals`` () =
  let school className abilityName =
    let entity =
      Content.partyOf [ "Warrior"; className ] 24 |> List.find (fun e -> e.Name = className)

    (entity.Abilities |> List.find (fun a -> a.Name = abilityName)).Effects
    |> List.pick (function
        | SpellDamage(_, _, s) -> Some s
        | _ -> None)

  Assert.Equal(Arcane, school "Mage" "Arcane Missiles")
  Assert.Equal(Frost, school "Mage" "Frostbolt")
  Assert.Equal(Fire, school "Mage" "Fireball")
  Assert.Equal(Shadow, school "Warlock" "Shadow Bolt")
  Assert.Equal(Holy, school "Paladin" "Judgement")
  Assert.Equal(Nature, school "Shaman" "Earth Shock")
  Assert.Equal(Arcane, school "Hunter" "Arcane Shot")

/// The eruption is one hit made bigger, not a buff: the same cast with the proc at
/// zero and at a hundred lands 25% harder, and the eruption bills 2% of base mana on
/// top of the spell's own cost. The two arms share a seed, so the only difference
/// between them is the proc.
[<Fact>]
let ``an arcane eruption makes that hit a quarter bigger and bills base mana`` () =
  let mage =
    (Content.partyOf [ "Mage" ] 60) |> List.exactlyOne

  let instability =
    mage.Talents.Procs |> List.find (fun p -> p.Talent = "Arcane Instability")

  // A level-5 dummy: a level-60 mob resists a level-60 mage's spells outright, and a
  // resist is not a hit, so the proc would rightly stay silent.
  let mob =
    Content.mob 100 "Dummy" CreatureType.Humanoid 5 1000000 1000 None [] None { X = 8; Y = 5 }

  let world chance =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities =
        [ { mage with
              Talents = { mage.Talents with Procs = [ { instability with ChanceBp = chance } ] }
              Pos = { X = 7; Y = 5 } }
          mob ]
      Pending = []
      Rng = Rng.streamsOf 2UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let cast name w =
    let ability = mage.Abilities |> List.find (fun a -> a.Name = name)
    SimCombat.resolveAbility (EntityId 1) (EntityId 100) ability w

  /// The amount on the `hits` line, and proof the cast landed at all.
  let landed w =
    Assert.Contains(w.Log, fun line -> line.Contains "hits")

    w.Log
    |> List.filter (fun line -> line.Contains "hits")
    |> List.last
    |> fun line -> int (line.Split ' ' |> Array.last)

  let plain = world 0 |> cast "Arcane Missiles" |> landed
  let bigger = world 10000 |> cast "Arcane Missiles" |> landed

  Assert.Equal(plain * 125 / 100, bigger)

  // The same cast and the same seed, so the only difference in the bill is the
  // eruption's 2% of *base* mana — not of the pool, which is what makes it cheap at
  // 60 and expensive at 24.
  let manaOf w = (w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Resource
  let eruptedCast = world 10000 |> cast "Arcane Missiles"
  let quietCast = world 0 |> cast "Arcane Missiles"
  Assert.Equal(mage.BaseResource / 50, manaOf quietCast - manaOf eruptedCast)

  // The school gate: a Frost spell of the same mage does not erupt.
  let frost = world 10000 |> cast "Frostbolt"
  Assert.DoesNotContain(frost.Log, fun line -> line.Contains "erupts")

/// The Priest's OctoWoW build (Discipline 11 / Holy 40 / Shadow 0). Eight of sixteen
/// talents are data — the best hit rate of the port so far — and two of the sixteen are
/// deletions, which a port that only adds would leave live.
[<Fact>]
let ``the priest's OctoWoW build lands, and drops what the tree dropped`` () =
  let priest =
    (Content.gully 1UL).Entities |> List.find (fun e -> e.Name = "Priest")

  let ability name = priest.Abilities |> List.find (fun a -> a.Name = name)

  // Spiritual Healing 5/5, the single largest number in the port.
  Assert.Equal(3000, priest.Talents.HealDoneBp)
  // Meditation 3/3 is not in the build. This assertion is the port's other half:
  // RegenBp was 1500 and the tree spends nothing on it.
  Assert.Equal(0, priest.Talents.RegenBp)
  // Improved Renew 3/3 (+15% heal) and Mental Agility 5/5 (-10% cost) share one line,
  // because `Talents.spell` is a `List.tryFind` and a second line would be dead.
  Assert.Equal(14500, Talents.healBp "Renew" priest.Talents)
  Assert.Equal((AbilityDef.at 24 Content.renewRanks).Value.ResourceCost * 9000 / 10000, (ability "Renew").ResourceCost)
  // Mental Agility 5/5 on the instant DoT: 95 mana becomes 85.
  Assert.Equal(85, (ability "Shadow Word: Pain").ResourceCost)
  // Divine Fury 5/5, in absolutes so a failure names the number it got. Half a second
  // is ten ticks at this sim's twenty a second — Smite is a five-second cast in
  // `smiteRanks` and four seconds on the kit.
  Assert.Equal(ticks 50, (AbilityDef.at 24 Content.smiteRanks).Value.CastTicks)
  Assert.Equal(ticks 40, (ability "Smite").CastTicks)
  // ... and Heal likewise, six seconds down to five.
  Assert.Equal(ticks 60, (AbilityDef.at 24 Content.healRanks).Value.CastTicks)
  Assert.Equal(ticks 50, (ability "Heal").CastTicks)
  // ... but not Lesser Heal: Divine Fury names Heal, Greater Heal and Smite.
  Assert.Equal((AbilityDef.at 24 Content.lesserHealRanks).Value.CastTicks, (ability "Lesser Heal").CastTicks)

/// The Warrior's OctoWoW build, landed where it lands: six of the eighteen talents
/// it spends are expressible as data, and each one is checked on the kit the party
/// is actually handed rather than on the talent record, because the record is the
/// claim and the kit is the fact. The other twelve are named in
/// `.pi/research/octowow/warrior.md` §4 with the axis each one needs.
[<Fact>]
let ``the warrior's OctoWoW build lands on his kit`` () =
  let tank = (Content.gully 1UL).Entities |> List.find (fun e -> e.Role = Some Tank)

  let ability name = tank.Abilities |> List.find (fun a -> a.Name = name)

  // Improved Heroic Strike 3/3: three rage off a fifteen-rage rung. Its cooldown
  // stays the weapon's own 2.70 s, because it replaces the next swing.
  Assert.Equal(12, (ability "Heroic Strike").ResourceCost)
  Assert.Equal(secTicks 2.7, (ability "Heroic Strike").CooldownTicks)
  // Improved Taunt 2/2 and Improved Shield Wall 2/2, both cooldown cuts.
  Assert.Equal(secTicks 8.0, (ability "Taunt").CooldownTicks)
  Assert.Equal(secTicks 1200.0, (ability "Shield Wall").CooldownTicks)
  // Improved Shield Wall's other half, baked into the row's duration.
  let wall = ability "Shield Wall"
  Assert.Contains(ReducesDamage(secTicks 12.0, 7500), wall.Effects)
  // Improved Disarm 2/2: twenty seconds off Disarm's sixty, and the 1.5 s floor in
  // `withTalents` is the invariant that keeps a future talent from zeroing a row.
  Assert.Equal(secTicks 40.0, (ability "Disarm").CooldownTicks)
  // Defiance 5/5, in the place threat actually lives: the stance's 1.30 times the
  // talent's 1.20. The sheet carried a flat 6.0 here while Defiance was a placeholder
  // for "+20% over the stance", which is neither the stance's own number nor the
  // talent's.
  Assert.Equal(15600, tank.ThreatMultiplierBp)

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

/// Damage pushes a cast back instead of cancelling it: half a second of the cast
/// is lost per hit, at most twice per cast, and resistance is a chance to lose
/// nothing at all. Full resistance means the cast is untouched.
[<Fact>]
let ``damage pushes a cast back half a second`` () =
  let w = casterWorld Content.lightningBolt
  let before = (mobOf w).Casting.Value.Remaining
  let w = SimCombat.dealDamage (EntityId 100) 50 w
  Assert.Equal(before + SimCombat.pushbackDelayTicks, (mobOf w).Casting.Value.Remaining)

[<Fact>]
let ``a cast absorbs at most two pushes`` () =
  let mutable w = casterWorld Content.lightningBolt

  // The pushback roll is a coin every hit, so keep hitting: the third delay is
  // what the cap is there to refuse, not the second.
  for _ in 1 .. 30 do
    w <- SimCombat.dealDamage (EntityId 100) 1 w

  Assert.Equal(SimCombat.pushbackMaxPerCast, (mobOf w).Casting.Value.Pushes)

  let capped = (mobOf w).Casting.Value.Remaining
  let w = SimCombat.dealDamage (EntityId 100) 1 w
  Assert.Equal(capped, (mobOf w).Casting.Value.Remaining)

[<Fact>]
let ``pushback resistance stops damage delaying a cast`` () =
  let w =
    casterWorld Content.lightningBolt
    |> SimState.mapEntity (EntityId 100) (fun e -> { e with PushbackResistanceBp = 10000 })

  let before = (mobOf w).Casting.Value.Remaining
  let mutable w = w

  for _ in 1 .. 30 do
    w <- SimCombat.dealDamage (EntityId 100) 50 w

  Assert.True((mobOf w).Casting.IsSome)
  Assert.Equal(before, (mobOf w).Casting.Value.Remaining)
  Assert.Equal(0, (mobOf w).Casting.Value.Pushes)

/// Crit and dodge come from Agility: roughly 10 Agility per point of crit and 20
/// per point of dodge at level 24, on top of the 5% every character starts with.
[<Fact>]
let ``agility drives crit and dodge`` () =
  let rogue = (Content.gully 1UL).Entities |> List.find (fun e -> e.Name = "Rogue")
  // Agility 100 + Mark of the Wild 4 = 104 -> 5% + 10.4% crit, 5% + 5.2% dodge.
  // Plus 1000 bp from Malice 5/5 and Close Quarters Combat 2/2, and 3000 from
  // Improved Backstab 3/3; see `rogueTraits`.
  Assert.Equal(2540, rogue.CritChanceBp)
  Assert.Equal(1020, rogue.DodgeChanceBp)
  Assert.Equal(3000, rogue.BackstabCritBonusBp)

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

/// A crit-damage talent adds points to the multiplier, not a factor to it: the
/// Hunter's Mortal Shots 5/5 is "+30%", which is 180% on a spell (from 150%) and
/// 230% on a swing (from 200%). Both sites keep their own base, so one number
/// serves a class whose damage is half shots and half swings.
[<Fact>]
let ``a crit-damage talent widens the multiplier on both sites`` () =
  // The spell site, as a pure function: 100 damage at a guaranteed crit.
  let spell, crit, _ = SimCombat.rollCrit 10000 3000 100 (Content.dungeon 1UL)
  Assert.Equal(180, spell)
  Assert.True(crit)

  let dealt (w: World) =
    let before = (mobOf w).Health
    let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.heroicStrike w
    before - (mobOf w).Health

  let amounts =
    [ 1UL .. 8UL ]
    |> List.map (fun seed ->
      arena 0 0 { X = 7; Y = 5 }
      |> SimState.mapEntity (EntityId 1) (fun e ->
        { e with
            CritChanceBp = 10000
            Talents = { e.Talents with CritDamageBp = 3000 } })
      |> fun w -> { w with Rng = Rng.streamsOf seed }
      |> dealt)

  // 45 damage at 230% is 103, and 90 without the talent.
  Assert.Contains(103, amounts)
  Assert.All(amounts, fun d -> Assert.True(d = 103 || d = 0, sprintf "dealt %d" d))
/// Improved Slaying pays against three creature types and nothing else, so the
/// field cannot be read off the attacker alone. Both halves of the talent use the
/// one number — the damage share multiplies, the crit-damage share is points on
/// the multiplier — and `slayingBp` is the gate that decides whether either pays.
[<Fact>]
let ``slaying pays against three creature types and nothing else`` () =
  let w = arena 0 0 { X = 7; Y = 5 }
  let mob = mobOf w

  let slayer =
    { (w.Entities |> List.find (fun e -> e.Id = EntityId 1)) with
        Talents = { Talents.none with SlayingBp = 300 } }

  let against kind =
    SimCombat.slayingBp slayer { mob with CreatureType = kind }

  Assert.Equal(300, against CreatureType.Humanoid)
  Assert.Equal(300, against CreatureType.Beast)
  Assert.Equal(300, against CreatureType.Dragonkin)
  Assert.Equal(0, against CreatureType.Elemental)
  Assert.Equal(0, against CreatureType.Undead)
  Assert.Equal(0, against CreatureType.Demon)

  // The damage share, on the one damage path with no armour in it: 100 becomes
  // 103 against a type the talent names and stays 100 against one it does not.
  let share kind =
    [ 1UL .. 8UL ]
    |> List.map (fun seed ->
      arena 0 0 { X = 7; Y = 5 }
      |> SimState.mapEntity (EntityId 100) (fun e -> { e with CreatureType = kind })
      |> fun w -> { w with Rng = Rng.streamsOf seed }
      |> fun w -> fst (SimCombat.spellHit "Test" slayer (EntityId 100) 100 w))

  Assert.Contains(103, share CreatureType.Beast)
  Assert.All(share CreatureType.Humanoid, fun d -> Assert.True(d = 103 || d = 0, sprintf "dealt %d" d))
  Assert.All(share CreatureType.Elemental, fun d -> Assert.True(d = 100 || d = 0, sprintf "dealt %d" d))

  // And the port's Hunter carries it, at the rank the build buys.
  let hunter = Content.partyOf [ "Hunter" ] 36 |> List.exactlyOne
  Assert.Equal(300, hunter.Talents.SlayingBp)
  Assert.Equal(3000, hunter.Talents.CritDamageBp)

/// Piercing Shots is the first talent in the port that pays the target: a crit
/// opens a `Bleeding` aura worth a share of the damage the crit dealt, over eight
/// ticks. It has to be its own aura case, because a shared one would let the
/// Hunter's bleed refresh its own Serpent Sting away.
[<Fact>]
let ``a crit-damage talent that pays the target bleeds it and stacks with itself`` () =
  let w = arena 0 0 { X = 7; Y = 5 }

  // A 90-damage crit, 30% of it over eight ticks, is 3 a tick.
  let bled =
    w
    |> SimState.mapEntity (EntityId 1) (fun e ->
      { e with
          Talents = { Talents.none with CritBleedShareBp = 3000 } })

  let hunter = bled.Entities |> List.find (fun e -> e.Id = EntityId 1)
  let bled = bled |> SimCombat.piercingBleed hunter (EntityId 100) 90

  let bleed =
    (mobOf bled).Auras
    |> List.exactlyOne
    |> fun a -> Assert.Equal(3, Aura.flatDamagePerTick a); a

  Assert.Equal(secTicks 8.0, Aura.remaining bleed)
  Assert.Equal(Some(EntityId 1), Aura.dotCaster bleed)

  // A second crit stacks a second bleed rather than refreshing the first: the
  // ticks the older one still owes are not the newer one's to throw away. This is
  // the assertion that would have caught the bug I shipped into a 74-procs-a-fight
  // no-op — every crit was eating the previous bleed.
  let both =
    bled
    |> SimCombat.piercingBleed hunter (EntityId 100) 80
    |> SimState.mapEntity (EntityId 100) (fun e ->
      { e with
          Auras = Aura.refresh (Corrupted(secTicks 12.0, 10, Some(EntityId 1))) e.Auras })

  Assert.Equal(3, (mobOf both).Auras.Length)
  Assert.Equal(16, (mobOf both).Auras |> List.sumBy Aura.flatDamagePerTick)
  Assert.Equal(2, (mobOf both).Auras |> List.filter (function Bleeding _ -> true | _ -> false) |> List.length)

  // No talent, no bleed.
  let untouched = SimCombat.piercingBleed (mobOf w) (EntityId 100) 90 w
  Assert.Empty((mobOf untouched).Auras)

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
let ``each story boss id maps to a fight the party can clear`` () =
  for id, name in
    [ "lady_anacondra", "Lady Anacondra"
      "lord_cobrahn", "Lord Cobrahn"
      "lord_pythas", "Lord Pythas"
      "lord_serpentis", "Lord Serpentis"
      "mutanus", "Mutanus the Devourer" ] do
    let w = Content.bossEncounter id 1UL
    Assert.True(w.IsSome, sprintf "%s should have a fight" id)

    let boss =
      w.Value.Entities |> List.find (fun e -> e.Faction = Hostile)

    Assert.Equal(name, boss.Name)

    let fought = Bench.runWorld w.Value 6000
    Assert.Equal(EncounterCleared, SimState.outcome fought)
    // A dormant boss would read as Cleared at tick 0 without a fight ever happening.
    Assert.True(int fought.Tick > 0, sprintf "%s woke and fought" id)

[<Fact>]
let ``an unknown story boss id has no fight`` () =
  Assert.True(Content.bossEncounter "no_such_boss" 1UL |> Option.isNone)

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
let ``Wailing Caverns party spawns safely outside hostile aggro range`` () =
  let w = Content.wailingCaverns 1UL
  let party = SimState.aliveParty w
  let hostiles = SimState.aliveHostiles w

  Assert.Equal(5, party.Length)
  Assert.NotEmpty(hostiles)

  for p in party do
    for h in hostiles do
      let dist = Pos.chebyshev p.Pos h.Pos
      Assert.True(
        dist > SimState.aggroRadius,
        sprintf "%s at (%d,%d) spawned within aggro range (%d <= %d) of %s at (%d,%d)"
          h.Name h.Pos.X h.Pos.Y dist SimState.aggroRadius p.Name p.Pos.X p.Pos.Y)

  Assert.True(hostiles |> List.forall (fun h -> not h.Engaged), "hostiles should not start engaged")

  // Standing still at spawn for several ticks must not trigger proximity aggro.
  let afterTicks = [ 1..5 ] |> List.fold (fun acc _ -> SimTick.step [] acc) w
  let engaged = SimState.aliveHostiles afterTicks |> List.filter (fun h -> h.Engaged)
  Assert.Empty(engaged)

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
      RestTicks = ticks 0
      Grid = Grid.ofRows [ "#####"; "#...#"; "#.#.#"; "#...#"; "#####" ]
      Entities = [ mage; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
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
      RestTicks = ticks 0
      Grid = Grid.create 10 5
      Entities = [ warrior; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
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
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities = [ tank; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
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
      RestTicks = ticks 0
      Grid = Grid.create 10 5
      Entities = [ warrior; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) Content.charge w
  let moved = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
  Assert.True(Pos.chebyshev moved.Pos { X = 6; Y = 1 } <= 1, "the warrior should land beside the mob")

[<Fact>]
let ``Charge is refused in combat while Intercept is not`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }
  let warrior = Content.hero 1 "Warrior" Tank 1000 1000 slow 50000 [ Content.charge; Content.intercept ] { X = 1; Y = 1 }
  let mob = Content.mob 100 "Mob" CreatureType.Humanoid 21 1000 0 (Some slow) [] None { X = 6; Y = 1 }

  let baseWorld =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 10 5
      // Starting the Warrior with an empty bar is the point of the first half: a
      // Charge that pays nine rage cannot show it on a capped bar.
      Entities = [ { warrior with Resource = 0 }; mob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let chargeCmd = { ApplyAt = ticks 1; Kind = UseAbility(EntityId 1, "Charge", EntityId 100) }
  let interceptCmd = { ApplyAt = ticks 1; Kind = UseAbility(EntityId 1, "Intercept", EntityId 100) }

  let movedTo (w: World) =
    (w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Pos

  // Out of combat Charge works, and it pays rage rather than costing it.
  let charged = SimCombat.applyCommand chargeCmd baseWorld
  Assert.True(Pos.chebyshev (movedTo charged) { X = 6; Y = 1 } <= 1, "Charge should close the gap out of combat")
  let rageAfterCharge = (charged.Entities |> List.find (fun e -> e.Id = EntityId 1)).Resource
  Assert.True(rageAfterCharge > 0, "Charge generates rage")

  // A leap through a wall is refused. The Warrior cannot see the mob it would land
  // beside, and walling the column between the two is the whole test.
  let walled = { baseWorld with Grid = Grid.create 10 5 }

  for y in 0 .. 4 do
    walled.Grid.Walls.[y * 10 + 4] <- true

  let throughWall = SimCombat.applyCommand chargeCmd walled
  Assert.Equal(Pos.chebyshev { X = 1; Y = 1 } (movedTo throughWall), 0)

  // Once something hostile is engaged, Charge is refused and Intercept is the answer.
  // Intercept costs ten rage, so the bar is filled for that half.
  let inCombat =
    { baseWorld with
        Entities =
          baseWorld.Entities
          |> List.map (fun e ->
            if e.Faction = Hostile then { e with Engaged = true }
            elif e.Id = EntityId 1 then { e with Resource = warrior.MaxResource }
            else e) }

  let refused = SimCombat.applyCommand chargeCmd inCombat
  Assert.Equal(Pos.chebyshev { X = 1; Y = 1 } (movedTo refused), 0)

  let intercepted = SimCombat.applyCommand interceptCmd inCombat
  Assert.True(Pos.chebyshev (movedTo intercepted) { X = 6; Y = 1 } <= 1, "Intercept should close the gap in combat")

[<Fact>]
let ``anything that stuns is an interrupt too`` () =
  // The engine already cancels a cast when a Stun, a Sleep or a Polymorph lands, so
  // a Gouge or a Shield Bash is a kick written as control. Reading only the
  // `Interrupt` effect meant the party spent those buttons on the nearest body
  // while a Chaplain healed through them.
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let control name ability =
    { Content.hero 1 name Dps 1000 1000 slow 50000 (AbilityDef.kitAt 24 [ ability ]) { X = 2; Y = 1 } with
        Target = Some(EntityId 100) }

  let caster =
    { Content.mob 100 "Caster" CreatureType.Humanoid 21 2000 0 (Some slow) [] None { X = 3; Y = 1 } with
        Engaged = true
        Target = Some(EntityId 1)
        Casting =
          Some
            { Ability = Content.lightningBolt
              Target = EntityId 1
              Remaining = ticks 20
              Pushes = 0 } }

  let world =
    { Tick = ticks 100
      RestTicks = ticks 0
      Grid = Grid.create 10 5
      Entities = [ control "Rogue" Content.gouge; caster ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let onCaster =
    PartyAi.decide world
    |> List.filter (fun c ->
      match c.Kind with
      | UseAbility(_, _, target) -> target = EntityId 100
      | _ -> false)

  Assert.Single(onCaster) |> ignore

  match onCaster.Head.Kind with
  | UseAbility(_, name, _) -> Assert.Equal("Gouge", name)
  | other -> failwithf "expected a Gouge at the caster, got %A" other

// ===========================================================================
// Dispels
// ===========================================================================

let private dispelWorld (mobAuras: Aura list) =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let priest =
    { Content.hero 1 "Priest" Healer 1000 1000 slow 50000 [ Content.dispelMagic ] { X = 5; Y = 5 } with
        Target = Some(EntityId 100) }

  let mob =
    { Content.mob 100 "Chaplain" CreatureType.Humanoid 39 2000 1000 (Some slow) [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some(EntityId 1)
        Auras = mobAuras }

  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 20 10
    Entities = [ priest; mob ]
    Pending = []
    Rng = Rng.streamsOf 1UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }

[<Fact>]
let ``Dispel Magic takes a heal-over-time off the mob it is aimed at`` () =
  let w = dispelWorld [ Rejuvenation(secTicks 10.0, 12, "Renew") ]

  let after =
    SimCombat.applyCommand
      { ApplyAt = ticks 1
        Kind = UseAbility(EntityId 1, "Dispel Magic", EntityId 100) }
      w

  let mob = after.Entities |> List.find (fun e -> e.Id = EntityId 100)
  Assert.Empty(mob.Auras)

[<Fact>]
let ``a purge is only worth a global when there is something to strip`` () =
  // The common case in this content is a mob with nothing removable, and a dispel
  // spent on it is a global the party never gets back.
  let withRenew =
    PartyAi.decide (dispelWorld [ Rejuvenation(secTicks 10.0, 12, "Renew") ])
    |> List.filter (fun c ->
      match c.Kind with
      | UseAbility(_, "Dispel Magic", _) -> true
      | _ -> false)

  let bare =
    PartyAi.decide (dispelWorld [])
    |> List.filter (fun c ->
      match c.Kind with
      | UseAbility(_, "Dispel Magic", _) -> true
      | _ -> false)

  Assert.Single(withRenew) |> ignore
  Assert.Empty(bare)

[<Fact>]
let ``one caster in mid-spell gets exactly one interrupter`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let rogue =
    { Content.hero 1 "Rogue" Dps 1000 1000 slow 50000 (AbilityDef.kitAt 24 [ Content.kick ]) { X = 2; Y = 1 } with
        Target = Some(EntityId 100) }

  let mage =
    { Content.hero 2 "Mage" Dps 1000 1000 slow 50000 (AbilityDef.kitAt 24 [ Content.counterspellRanks ]) { X = 6; Y = 1 } with
        Target = Some(EntityId 100) }

  let caster =
    { Content.mob 100 "Caster" CreatureType.Humanoid 21 2000 0 (Some slow) [] None { X = 3; Y = 1 } with
        Engaged = true
        Target = Some(EntityId 1)
        Casting =
          Some
            { Ability = Content.lightningBolt
              Target = EntityId 1
              Remaining = ticks 20
              Pushes = 0 } }

  let world =
    { Tick = ticks 100
      RestTicks = ticks 0
      Grid = Grid.create 10 5
      Entities = [ rogue; mage; caster ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let kicksOnCaster =
    PartyAi.decide world
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(actor, name, target) when
        target = EntityId 100 && (name = "Kick" || name = "Counterspell")
        ->
        Some(actor, name)
      | _ -> None)

  // One cast, one kick: the Mage must not spend a twenty-four second Counterspell on
  // a spell the Rogue's ten-second Kick is already covering.
  Assert.Equal(1, kicksOnCaster.Length)
  Assert.Equal("Kick", snd kicksOnCaster.Head)

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
  // 1.15.9 client this content follows. Each carries its Holy Power 3/3 crit.
  Assert.Equal<Effect list>([ Heal(322, 368); CritBonus 600 ], effectsOf "Holy Light")
  Assert.Equal<Effect list>([ Heal(67, 77); CritBonus 600 ], effectsOf "Flash of Light")
  // Judgement with Seal of Righteousness rank 3 up, and no crit: Holy Power names
  // the two heals only.
  Assert.Equal<Effect list>([ SpellDamage(53, 57, Holy) ], effectsOf "Judgement")
  // Hammer of Justice rank 2 is a 4 s stun, Divine Protection rank 2 is 8 s of
  // immunity.
  Assert.Equal<Effect list>([ Stun(secTicks 4.0) ], effectsOf "Hammer of Justice")
  Assert.Equal<Effect list>([ ReducesDamage(secTicks 8.0, 10000) ], effectsOf "Divine Protection")
  // Purify is the era's poison cleaner, and the only thing a Paladin dispels; it is
  // here because the operator listed the class's cleaners, not because this content
  // has a poison for it to find.
  Assert.Equal<Effect list>([ Dispel(Poison, 1) ], effectsOf "Purify")
  // Divine Favor is not in the kit any more: OctoWoW's Divine Favor is a passive
  // Holy Shock crit talent, so the 1.12 cooldown row was deleted rather than tuned
  // (`.pi/research/octowow/paladin.md` §5(c)). Six abilities: the five the build
  // spends points on, plus Purify.
  Assert.DoesNotContain(paladin.Abilities, fun a -> a.Name = "Divine Favor")
  Assert.Equal(6, List.length paladin.Abilities)

/// The Paladin's OctoWoW build — Holy 40 / Protection 0 / Retribution 11, decoded in
/// `.pi/research/octowow/paladin.md` §6. Seven of its seventeen talents are data;
/// the other nine need axes the model does not have (§5(b)), and one 1.12 kit row had
/// to be deleted rather than tuned (§5(c)).
[<Fact>]
let ``the paladin's OctoWoW build lands on his kit`` () =
  // Level 24, the roster's level: at level 1 the Paladin has no Holy Light, so three
  // of the record's five spell rows would have nothing to land on.
  let paladin =
    Content.partyOf [ "Warrior"; "Paladin" ] 24
    |> List.find (fun e -> e.Name = "Paladin")

  let ability name = paladin.Abilities |> List.find (fun a -> a.Name = name)

  // Divine Intellect 5/5, and Illumination 5/5 which is a **nerf**: the sim used to
  // refund a crit heal's whole cost and OctoWoW refunds sixty per cent of it.
  Assert.Equal(1000, paladin.Talents.IntellectBp)
  Assert.Equal(6000, paladin.Talents.ManaRefundOnCritBp)
  // Healing Light 3/3 on both casts the kit carries. Holy Shock is the talent's third
  // target and there is no row for it, so it is absent by construction rather than
  // by omission.
  // `Talents.healBp` returns the multiplier including the 10000 baseline, so 11200
  // is the 12% Healing Light 3/3 on top of an unmodified heal.
  Assert.Equal(11200, Talents.healBp "Flash of Light" paladin.Talents)
  Assert.Equal(11200, Talents.healBp "Holy Light" paladin.Talents)
  // Holy Shock rides the same Healing Light 3/3 rider (octowow/paladin.md §4).
  Assert.Equal(11200, Talents.healBp "Holy Shock" paladin.Talents)
  // Holy Power 3/3 — the old comment claimed it while the record set no crit at all.
  Assert.Contains(CritBonus 600, (ability "Holy Light").Effects)
  Assert.Contains(CritBonus 600, (ability "Flash of Light").Effects)
  Assert.DoesNotContain((ability "Judgement").Effects, fun e -> e = CritBonus 600)
  // Improved Judgement 2/2: ten seconds becomes eight, and never below the 1.5 s floor.
  Assert.Equal(secTicks 8.0, (ability "Judgement").CooldownTicks)
  // Benediction 3/5: nine per cent off Judgement's mana.
  let stock = (AbilityDef.at 24 Content.judgementRanks).Value
  Assert.Equal(stock.ResourceCost * 9100 / 10000, (ability "Judgement").ResourceCost)

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
  // Blessing of Kings multiplies all five by 10% once those flats have landed,
  // which is the order the game uses: Stamina 89 -> 97, Intellect 55 -> 60 -> 66.
  // Health = 247 + (20 + 10 x 77); mana = 525 + (20 + 15 x 46).
  Assert.Equal(1037, paladin.MaxHealth)
  Assert.Equal(1235, paladin.MaxResource)
  // 557 from the four mail pieces, 90 from 45 Agility (41 before Kings), 275
  // Devotion Aura, 105 Mark of the Wild. Armour is not a stat, so Kings does not
  // multiply it — it only carries the extra 4 Agility, which is 8 armour.
  Assert.Equal(1035, paladin.Armor)
  // Entity.Stats is the dressed set before the passives: Strength 47 + 27 = 74.
  Assert.Equal(74, paladin.Stats.Strength)
  // Mark of the Wild takes it to 78, and Kings multiplies that to 85, which is 170
  // attack power, plus Battle Shout 55 and Blessing of Might 55: 280 AP, so
  // 280/14 x 3.20 s = 64 a swing. The attack power itself is not stored, so the
  // swing bonus is what proves it.
  Assert.Equal(64, paladin.MeleeDamageBonus)
  // Verigan's Fist, 65-99 at 3.20 s.
  Assert.Equal((65, 99), (paladin.AutoAttack.Value.Min, paladin.AutoAttack.Value.Max))

/// Gear scales with level from the level-24 set to the class's level-60 Tier 1
/// raid outfit, which `.pi/research/levelscale/raid-gear-60.md` tabulates piece
/// by piece. The level-24 numbers are frozen (they are the calibration point this
/// file's content is tuned at); level 60 has to land on the researched totals, and
/// nothing in between may go backwards.
[<Fact>]
let ``gear grows from the level-24 set to the level-60 best in slot`` () =
  // A warrior alone, so Battle Shout's attack power is the only passive and the
  // stat totals are the dressed ones: base row at 60 is (120, 80, 110, 30, 47) and
  // the Tier 1 outfit adds (161, 21, 257, 0, 0) — the file's own TOTALS.
  let warrior =
    Content.partyOf [ "Warrior" ] 60 |> List.find (fun e -> e.Name = "Warrior")

  Assert.Equal(281, warrior.Stats.Strength)
  Assert.Equal(101, warrior.Stats.Agility)
  Assert.Equal(367, warrior.Stats.Stamina)
  Assert.Equal(30, warrior.Stats.Intellect)
  // 8068 from the seventeen pieces, 202 from 101 Agility, and Toughness 5/5's
  // +10% on the gear's own share (8068 x 1.10 = 8874, + 202); no Devotion Aura in
  // a warrior-only party.
  Assert.Equal(9076, warrior.Armor)

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
    [ BattleShout; BlessingOfMight; DevotionAura; BlessingOfKings ],
    Content.partyBuffsFor [ "Warrior"; "Paladin" ]
  )

  // Kings is the one passive a *talent* hands out, so it arrives with the Paladin's
  // talent record rather than with the Paladin. Point that falls away and the party
  // keeps Might and Devotion Aura and loses Kings — which is exactly what the
  // talent record controls, and why a class can be seated without having spent the
  // point.
  Assert.Contains(BlessingOfKings, Content.partyBuffsFor [ "Paladin" ])
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
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; paladin; enemy ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // The scripted Party is input, not part of the step: a run that never passes it
  // the autopilot's commands gives nobody an order. This is the loop `Bench` uses.
  let mutable w = w

  for _ in 1 .. 400 do
    w <- SimTick.step (Bench.autoPilot w) w

  let log = String.concat "\n" w.Log

  Assert.True(
    w.Log |> List.exists (fun line -> line.StartsWith "Paladin's " && line.Contains " heals "),
    sprintf "the paladin should have healed; log:\n%s" log
  )

/// The balance rig splits a run's damage and healing by ability. That split is
/// only usable if it adds up to the per-entity figure a class ladder reports, so
/// the two ways `Attribution` rolls the same log up must agree — and it must have
/// read something, since an empty ability table would sum just as well.
[<Fact>]
let ``per-ability attribution adds up to the per-entity totals`` () =
  let mutable w = Content.gully 1UL

  // A whole party needs no healing — out-of-combat regeneration refills it in five
  // seconds — so a healthy run logs only mobs healing to zero and the healing half of
  // this split would have nothing to read. Half a Tank is the pressure that makes the
  // healer work, but it is not sufficient on its own: this seed was 4 until the Mage
  // gained a window row and the party started clearing the wing with the Tank still
  // whole, which logged no heal at all. Seed 1 keeps the Tank in trouble.
  w <-
    { w with
        Entities =
          w.Entities
          |> List.map (fun e ->
            if e.Faction = Faction.Party && e.Name = "Warrior" then
              { e with Health = e.MaxHealth / 2 }
            else
              e) }

  for _ in 1 .. 1200 do
    if SimState.continues w then
      w <- SimTick.step (Bench.autoPilot w) w

  let perEntity = Attribution.byCaster w

  let rolledUp =
    Attribution.byAbility w
    |> Map.toList
    |> List.fold
      (fun totals ((caster, _), t) ->
        let running = totals |> Map.tryFind caster |> Option.defaultValue Attribution.empty

        totals
        |> Map.add caster { Damage = running.Damage + t.Damage; Healing = running.Healing + t.Healing })
      Map.empty

  Assert.Equal<Map<string, Attribution.Totals>>(perEntity, rolledUp)

  Assert.True(
    (perEntity |> Map.toList |> List.sumBy (fun (_, t) -> t.Damage)) > 0,
    "the run should have damaged something"
  )

  Assert.True(
    (perEntity |> Map.toList |> List.sumBy (fun (_, t) -> t.Healing)) > 0,
    "the run should have healed something"
  )

  // Wording drift guard: if the log stopped naming the ability, every line would
  // fall back to Auto Attack and the sums above would still agree.
  Assert.True(
    Attribution.byAbility w
    |> Map.exists (fun (_, ability) _ -> ability <> "Auto Attack"),
    "the log should name abilities, not only swings"
  )

/// The warlock's kit is the rank-for-rank level-24 one: no Banish (28), no Hellfire
/// (30) — and underneath it, the operator's Affliction build, which is the part that
/// changes every number here. Cast times shrink (Bane, Improved Corruption), crit
/// arrives on the two Destruction spells (Devastation), and the range is Grim Reach's
/// 10 tiles rather than the table's 8.
[<Fact>]
let ``the warlock's kit is the level-24 one, under the affliction build`` () =
  let warlock =
    Content.partyOf [ "Warrior"; "Warlock" ] 24 |> List.find (fun e -> e.Name = "Warlock")

  let ability name = warlock.Abilities |> List.find (fun a -> a.Name = name)
  let effectsOf name = (ability name).Effects

  // Devastation 5/5 is `CritBonus 500` on the ability, because the crit roll belongs
  // to the spell that lands rather than to the caster (same shape as the healer lines).
  Assert.Equal<Effect list>([ SpellDamage(86, 99, Shadow); CritBonus 500 ], effectsOf "Shadow Bolt")
  Assert.Equal<Effect list>([ DamageOverTime(secTicks 18.0, 12) ], effectsOf "Corruption")
  Assert.Equal<Effect list>([ DamageOverTime(secTicks 24.0, 7) ], effectsOf "Curse of Agony")
  Assert.Equal<Effect list>(
    [ SpellDamage(45, 45, Fire); DamageOverTime(secTicks 15.0, 6); CritBonus 500 ],
    effectsOf "Immolate"
  )
  Assert.Equal<Effect list>([ Sleep(secTicks 10.0) ], effectsOf "Fear")

  // Bane 5/5 takes half a second off Shadow Bolt and Immolate; Improved Corruption
  // 5/5 takes a second and a half off Corruption, which is OctoWoW's 0.3 s a point
  // and leaves half a second where 1.12 would leave none.
  Assert.Equal(secTicks 2.5, (ability "Shadow Bolt").CastTicks)
  Assert.Equal(secTicks 0.5, (ability "Corruption").CastTicks)
  Assert.Equal(secTicks 1.5, (ability "Immolate").CastTicks)

  // Grim Reach 2/2 is a range, so it is baked into the Affliction ranks: 8 → 10.
  Assert.Equal(10, (ability "Corruption").Range)
  Assert.Equal(10, (ability "Curse of Agony").Range)
  Assert.Equal(8, (ability "Shadow Bolt").Range)

  // Six now, not five: `Summon Demon` sits in the kit beside the five spells. It is
  // a summon rather than a cast, but a Warlock that never calls its Imp is not a
  // Warlock — the body is a third of its damage over a long pull.
  Assert.Equal(6, List.length warlock.Abilities)
  Assert.Contains(warlock.Abilities, fun a -> a.Name = "Summon Demon")

/// Fel Concentration 2/2 is a `Traits` field, like the two healer trees' Nature's
/// Focus and Spiritual Focus: 70% off the chance a hit interrupts a cast.
[<Fact>]
let ``the warlock carries fel concentration as a trait`` () =
  let warlock =
    Content.partyOf [ "Warrior"; "Warlock" ] 24 |> List.find (fun e -> e.Name = "Warlock")

  Assert.Equal(7000, warlock.PushbackResistanceBp)

/// The three damage talents land on three different things, and the DoT one only
/// works because `SimCombat` folds the tree's share into `perTick` when the aura is
/// applied — a `Corrupted` carries no caster by the time it fires.
[<Fact>]
let ``the warlock's damage talents reach direct hits and the dot alike`` () =
  let warlock =
    Content.partyOf [ "Warrior"; "Warlock" ] 24 |> List.find (fun e -> e.Name = "Warlock")

  let mob = Content.mob 100 "Mob" CreatureType.Humanoid 24 10000 1000 None [] None { X = 4; Y = 4 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ warlock; mob ]
      Pending = []
      Rng = Rng.streamsOf 7UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // Shadow Mastery 5/5 (+10%) is on Shadow Bolt, Improved Curse of Agony 3/3 (+10%)
  // on the curse and Aftermath 3/3 (+6%) on Immolate. Corruption takes Shadow Mastery
  // only — it is a Shadow spell and Aftermath names Immolate.
  let tagOf name = Talents.damageBp name warlock.Talents
  Assert.Equal(11000, tagOf "Shadow Bolt")
  Assert.Equal(11000, tagOf "Corruption")
  Assert.Equal(11000, tagOf "Curse of Agony")
  Assert.Equal(10600, tagOf "Immolate")
  Assert.Equal(10000, tagOf "Fear")

  // Applied through `resolveAbility`, the same path a finished cast takes — Corruption is
  // a half-second cast under this build, so casting it through `ApplyAt` would only
  // start it. Rank 3 Corruption's 12 a tick becomes 13 at +10%, and Curse of Agony's 7
  // stays 7 (7 * 1.1 = 7.7 rounds down), which is why the assertion reads Corruption.
  let corruption =
    warlock.Abilities |> List.find (fun a -> a.Name = "Corruption")

  let cursed = SimCombat.resolveAbility warlock.Id mob.Id corruption w

  let perTick =
    (cursed.Entities |> List.find (fun e -> e.Id = mob.Id)).Auras
    |> List.pick (function
      | Corrupted(_, n, _) -> Some n
      | _ -> None)

  Assert.Equal(13, perTick)

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

  // Base health 266 with Stamina 61 (57 + Mark of the Wild, x1.1 for Kings = 67);
  // base mana 467 with Intellect 93 (82 + 4 + Arcane Intellect 7, x1.1 = 102), which
  // is the largest pool of the nine and the point of the class.
  Assert.Equal(756, warlock.MaxHealth)
  Assert.Equal(1717, warlock.MaxResource)
  // 175 from the six cloth pieces, 86 from 43 Agility (39 before Kings, so 90), 105
  // Mark of the Wild and 275 Devotion Aura from the roster.
  Assert.Equal(649, warlock.Armor)
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

  Assert.Equal<Effect list>([ SpellDamage(88, 100, Nature); CritBonus 500 ], effectsOf "Lightning Bolt")
  Assert.Equal<Effect list>([ SpellDamage(125, 133, Nature); Interrupt ], effectsOf "Earth Shock")
  Assert.Equal<Effect list>(
    [ SpellDamage(51, 51, Fire); DamageOverTime(secTicks 12.0, 4) ],
    effectsOf "Flame Shock"
  )
  // Tidal Mastery's +5% rides every healing and lightning spell, appended by
  // `Content.withTalents` after the row's own effects. Earth Shock and Flame Shock
  // are not lightning spells and keep their bare rows above, which is the assertion
  // that the talent landed on the right two.
  Assert.Equal<Effect list>([ Heal(389, 453); CritBonus 500 ], effectsOf "Healing Wave")
  Assert.Equal<Effect list>(
    [ Empowers(secTicks 20.0, { Instant = true; FreeMana = false; SureCrit = false; Only = None }) ],
    effectsOf "Nature's Swiftness"
  )
  Assert.Equal<string list>(
    [ "Lightning Bolt"
      "Earth Shock"
      "Flame Shock"
      "Healing Wave"
      "Stoneskin Totem"
      "Healing Stream Totem"
      "Cure Poison Totem"
      // Two rungs below the trained rank 5 (24) is rank 3 (12), the kit's top-up — the
      // same shape the Restoration Druid's kit has, and the reason `sim healers` is at
      // 2.55 healing per point instead of 2.00 at 36 (Content.fs `shamanDownrank`). It
      // sits here because the kit is `AbilityDef.kitAt` then the totems then the
      // downrank then the trained extras, which is an order the assertion pins.
      "Healing Wave (Rank 3)"
      "Nature's Swiftness"
      "Reincarnation"
      "Ancestral Spirit"
      "Purge"
      "Cure Poison"
      "Cure Disease"
      "Water Shield" ],
    shaman.Abilities |> List.map (fun a -> a.Name)
  )

/// Two rungs below the trained rank is where a top-up lives, and the Shaman needs
/// one badly: at 36 the trained rank is 265 mana for 552-638, so before this row
/// every 128-point hole on a pet was bought at 265 — 137 mana for a hole the top
/// rank cannot help filling. That is the whole of its efficiency gap (`sim healers
/// 30`: 2.00 healing per point, the Druid's 2.79, both 30 seeds).
///
/// The row only helps if `directFor` actually reaches for it, so this asserts the
/// choice rather than the row's existence — `costToClose` is what makes a downrank
/// work and a kit row alone would prove nothing. Rank 4 tops out at 339 where the
/// trained rank tops at 638: a 300 hole is one cheap cast, a 1,000 hole is two of the
/// top rank (530) against four of the cheap one (620).
///
/// The fight is built rather than pulled off `Content.dungeonAt` for two reasons. A
/// shaman in combat answers with its totems before its heals (`PartyAi.decide`
/// yields `totemOrders` first), and a real Scarlet pull ends in about three seconds
/// — long before a test can ask the same question twice — so the mob here has a
/// hundred times the health and no swing, and the tank's wound is re-cut every tick
/// instead of healing away.
[<Fact>]
let ``a shaman downranks for a small hole and takes the trained rank for a big one`` () =
  let party = Content.partyOf [ "Warrior"; "Shaman" ] 36
  let warrior = party |> List.find (fun e -> e.Name = "Warrior")
  let shaman = party |> List.find (fun e -> e.Name = "Shaman")

  let stand (e: Entity) (x: int) = { e with Pos = { X = x; Y = 9 }; Home = Some { X = x; Y = 9 } }

  let mob =
    Content.mob 100 "Grunt" CreatureType.Humanoid 36 100000 1000 None [] None { X = 13; Y = 9 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 24 20
      Entities = [ stand warrior 10; stand shaman 11; mob ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // One point on the mob is a pull: `PartyAi.decide` issues no heal out of combat,
  // and a test that never got into combat would pass an empty order list off as a
  // rank decision.
  let pulled = SimCombat.dealDamage mob.Id 1 w
  Assert.True(SimState.inCombat pulled, "the pull should put the party in combat")

  // Four ticks is the totem phase — Stoneskin, Healing Stream, Grounding — and a
  // healer answers "Stoneskin Totem" on every one of them whatever the wound is.
  // Measured from the tick after, so what the loop below sees is the heal choice.
  let settled =
    [ 1..4 ] |> List.fold (fun w _ -> SimTick.step (Bench.autoPilot w) w) pulled

  let shamanHeal (hole: int) =
    let tank (w: World) = w.Entities |> List.find (fun e -> e.Id = warrior.Id)

    let shamanIssued (w: World) =
      PartyAi.decide w
      |> List.tryPick (fun c ->
        match c.Kind with
        | UseAbility(a, n, _) when a = shaman.Id && n.StartsWith "Healing Wave" -> Some n
        | _ -> None)

    let rec play (n: int) (w: World) (log: string list) =
      if n = 0 then
        (None, log |> List.rev |> List.distinct |> String.concat ",")
      else
        // Heals move the tank back up, so the hole is re-cut every tick: the question
        // is which rank answers a hole of this size, not which rank answers whatever
        // the previous tick left behind.
        let e = tank w
        let missing = e.MaxHealth - e.Health
        let w = if missing < hole then SimCombat.dealDamage warrior.Id (hole - missing) w else w

        match shamanIssued w with
        | Some name -> (Some name, log |> List.rev |> List.distinct |> String.concat ",")
        | None ->
          play (n - 1) (SimTick.step (Bench.autoPilot w) w) (((shamanIssued w |> Option.toList) |> String.concat ",") :: log)

    play 40 settled []

  let small, smallWhy = shamanHeal 300
  let big, bigWhy = shamanHeal 1000

  Assert.True((small = Some "Healing Wave (Rank 4)"), $"a 300-point hole drew {small} ({smallWhy})")
  Assert.True((big = Some "Healing Wave"), $"a 1,000-point hole drew {big} ({bigWhy})")

/// Content's private cleave row, rebuilt shape-for-shape: the geometry is what is
/// under test, and the row itself cannot be imported through Content's private
/// Scarlet roster.
let private cleaveRow =
  { Name = "Cleave"
    CastTicks = ticks 0
    CooldownTicks = secTicks 6.0
    ResourceCost = 0
    Stance = None
    Interruptible = false
    Range = 1
    TargetKind = Foe
    Effects = [ AreaDamage(1, 88, 118) ] }

[<Fact>]
let ``enemy cleave hits only the three tiles in front of the cleaver`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }
  let hero id name pos = Content.hero id name Dps 1000 1000 slow 10000 [] pos

  let cleaver id pos aim =
    let m = Content.mob id "Cleaver" CreatureType.Humanoid 24 3018 1000 None [ cleaveRow ] None pos

    { m with Engaged = true; Target = Some(EntityId aim) }

  let bare entities =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities = entities
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let healthOf (w: World) (id: int) =
    w.Entities |> List.find (fun e -> e.Id = EntityId id) |> fun e -> e.Health

  // scratch parity: row built inside the fact
  let row =
    { Name = "Cleave"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 0
      Stance = None
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ AreaDamage(1, 88, 118) ] }

  let swing w cleaverId aimId = SimCombat.resolveAbility (EntityId cleaverId) (EntityId aimId) row w

  // Cardinal: the cleaver at (6,5) aims west at (5,5), so the cone is (5,5) and
  // the two tiles flanking it, (5,4) and (5,6). The body standing behind at
  // (7,5) is adjacent — inside the old radius-1 circle — and must take nothing.
  let w1 =
    swing
      (bare
        [ hero 1 "Aim" { X = 5; Y = 5 }
          hero 2 "Flank" { X = 5; Y = 4 }
          hero 3 "Behind" { X = 7; Y = 5 }
          cleaver 100 { X = 6; Y = 5 } 1 ])
      100
      1

  Assert.True(healthOf w1 1 < 1000, "the aimed-at body must take the cleave")
  Assert.True(healthOf w1 2 < 1000, "the flank tile is inside the front arc")
  Assert.True(healthOf w1 3 = 1000, "the tile behind the cleaver is outside the arc")

  // Diagonal: the cleaver at (12,5) aims (13,4), so the cone is the aim plus the
  // two half-steps toward it, (13,5) and (12,4). The back-diagonal body at
  // (11,6) is adjacent and safe.
  let w2 =
    swing
      (bare
        [ hero 4 "Aim" { X = 13; Y = 4 }
          hero 5 "Flank" { X = 13; Y = 5 }
          hero 6 "Behind" { X = 11; Y = 6 }
          cleaver 200 { X = 12; Y = 5 } 4 ])
      200
      4

  Assert.True(healthOf w2 4 < 1000, "a diagonal aim still takes the cleave")
  Assert.True(healthOf w2 5 < 1000, "the half-step tile is inside the diagonal arc")
  Assert.True(healthOf w2 6 = 1000, "the back-diagonal tile is outside the arc")

[<Fact>]
let ``swipe hits every adjacent enemy and nothing further`` () =
  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }
  let bear =
    Content.hero 1 "Bear" Tank 1000 1000 slow 10000 [ (AbilityDef.at 24 Content.swipeRanks).Value ] { X = 5; Y = 5 }

  let mob id pos = Content.mob id "Mob" CreatureType.Humanoid 24 100000 1000 (Some slow) [] None pos

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 24 20
      Entities = [ bear; mob 100 { X = 6; Y = 5 }; mob 101 { X = 5; Y = 4 }; mob 102 { X = 12; Y = 5 } ]
      Pending = []
      // Seed 5, not 1: at 1 the aimed mob's spell roll comes up a resist, and the
      // point of this test is the geometry, not the hit table.
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let w' = SimCombat.resolveAbility (EntityId 1) (EntityId 100) ((AbilityDef.at 24 Content.swipeRanks).Value) w
  let healthOf (id: int) = w'.Entities |> List.find (fun e -> e.Id = EntityId id) |> fun e -> e.Health

  Assert.True(healthOf 100 < 100000, "the aimed-at mob must take the swipe")
  Assert.True(healthOf 101 < 100000, "an adjacent mob the aim is not on still takes the swipe")
  // (12,5) is six tiles from the aim — inside the old radius-8-of-aim net and
  // far outside a melee cleave.
  Assert.True(healthOf 102 = 100000, "a mob that is not adjacent to the swiper takes nothing")

[<Fact>]
let ``a melee healer steps behind the mob the tank is holding`` () =
  let party = Content.partyOf [ "Warrior"; "Paladin" ] 30
  let warrior = party |> List.find (fun e -> e.Name = "Warrior")
  let paladin = party |> List.find (fun e -> e.Name = "Paladin")

  let stand (e: Entity) (x: int) = { e with Pos = { X = x; Y = 9 }; Home = Some { X = x; Y = 9 } }

  let mob =
    Content.mob 100 "Graveyard Mob" CreatureType.Humanoid 30 100000 1000 None [] None { X = 11; Y = 9 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 24 20
      Entities = [ stand warrior 10; stand paladin 14; mob ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // One point on the mob is a pull: the melee step only exists in combat.
  let pulled =
    SimState.mapEntity (EntityId 100) (fun m -> { m with Engaged = true; Target = Some warrior.Id }) w
    |> SimCombat.dealDamage (EntityId 100) 1

  let behind = { X = 12; Y = 9 }

  let stepped =
    PartyAi.decide pulled
    |> List.exists (fun c ->
      match c.Kind with
      | MoveTo(a, p) when a = paladin.Id && p = behind -> true
      | _ -> false)

  Assert.True(stepped, $"the paladin should step to the mob's back tile {behind}, not just any tile in reach")

/// The operator's Paladin heal policy (m02387), observed through `PartyAi.decide`
/// the same way the shaman downrank test observes its ranks. The tank's health is
/// pinned small so every hole below sits below the 75% line that feeds the direct
/// lane, and Holy Shock is quelled to its own cooldown so the question is strictly
/// Holy Light against Flash of Light.
///
/// The reference the threshold is measured against is the rank's HIGH bound plus
/// the flat bonus, times Healing Light 3/3 — the deterministic form of the cast
/// path's arithmetic, read from the party's own paladin so the test cannot drift
/// from a rank change.
[<Fact>]
let ``a paladin orders Holy Light by the attacker count and Flash of Light for minor holes`` () =
  let party = Content.partyOf [ "Warrior"; "Paladin" ] 30
  let warrior = party |> List.find (fun e -> e.Name = "Warrior")
  let paladin = party |> List.find (fun e -> e.Name = "Paladin")

  let stand (e: Entity) (x: int) = { e with Pos = { X = x; Y = 9 }; Home = Some { X = x; Y = 9 } }

  let holyLight = paladin.Abilities |> List.find (fun a -> a.Name = "Holy Light")
  let hi = holyLight.Effects |> List.pick (function Heal(_, hi) -> Some hi | _ -> None)
  let hlRef = (hi + paladin.BonusHealing) * Talents.healBp "Holy Light" paladin.Talents / 10000

  // `attackers n` is a pull with n mobs all on the tank: the graveyard case is
  // four, and the formula is what changes between the runs, nothing else.
  let attackers (n: int) =
    [ for i in 1 .. n ->
        Content.mob (100 + i) "Graveyard Mob" CreatureType.Humanoid 30 100000 1000 None [] None { X = 13 + i; Y = 9 } ]
    |> List.map (fun m ->
      { m with
          Engaged = true
          Target = Some warrior.Id
          Threat = Map.ofList [ warrior.Id, 1000 ] })

  let worldOf (mobs: Entity list) =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 24 20
      Entities = [ stand { warrior with MaxHealth = 700; Health = 700 } 10; stand paladin 11 ] @ mobs
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let pulled (mobs: Entity list) =
    let first = List.head mobs
    SimCombat.dealDamage first.Id 1 (worldOf mobs)

  let issued (w: World) =
    PartyAi.decide w
    |> List.tryPick (fun c ->
      match c.Kind with
      | UseAbility(a, n, _) when a = paladin.Id && (n = "Holy Light" || n = "Flash of Light") -> Some n
      | _ -> None)

  let answers (hole: int) (mobs: Entity list) =
    let quelled =
      SimState.mapEntity paladin.Id (fun e ->
        { e with Cooldowns = e.Cooldowns |> Map.add "Holy Shock" (secTicks 30.0) }) (pulled mobs)

    let rec play n w =
      if n = 0 then
        None
      else
        // The hole is re-cut every tick, so the decision answers a hole of this
        // size rather than whatever the previous cast left behind.
        let e = w.Entities |> List.find (fun x -> x.Id = warrior.Id)
        let missing = e.MaxHealth - e.Health
        let w = if missing < hole then SimCombat.dealDamage warrior.Id (hole - missing) w else w

        match issued w with
        | Some name -> Some name
        | None -> play (n - 1) (SimTick.step (Bench.autoPilot w) w)

    play 40 quelled

  let big = answers (hlRef * 85 / 100) (attackers 1)
  let minor = answers (hlRef / 3) (attackers 1)
  let grave = answers (hlRef / 3) (attackers 4)

  Assert.True((big = Some "Holy Light"), $"an 85-percent-of-Holy-Light hole with one attacker drew {big}")
  Assert.True((minor = Some "Flash of Light"), $"a minor hole with one attacker drew {minor}")
  Assert.True((grave = Some "Holy Light"), $"the same minor hole with four attackers drew {grave} - the 5-percent floor should have the big cast out")

[<Fact>]
let ``holy shock heals an ally, rides healing light, and answers only a deep hole`` () =
  let party = Content.partyOf [ "Warrior"; "Paladin" ] 30
  let warrior = party |> List.find (fun e -> e.Name = "Warrior")
  let paladin = party |> List.find (fun e -> e.Name = "Paladin")

  let stand (e: Entity) (x: int) = { e with Pos = { X = x; Y = 9 }; Home = Some { X = x; Y = 9 } }

  let mob =
    { Content.mob 100 "Graveyard Mob" CreatureType.Humanoid 30 100000 1000 None [] None { X = 13; Y = 9 } with
        Engaged = true
        Target = Some warrior.Id
        Threat = Map.ofList [ warrior.Id, 1000 ] }

  let worldOf (hole: int) =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 24 20
      Entities = [ stand { warrior with MaxHealth = 800; Health = 800 - hole } 10; stand paladin 11; mob ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let pulled (hole: int) =
    worldOf hole |> SimCombat.dealDamage (EntityId 100) 1

  let shockIssued (w: World) =
    PartyAi.decide w
    |> List.exists (fun c ->
      match c.Kind with
      | UseAbility(a, "Holy Shock", t) when a = paladin.Id && t = warrior.Id -> true
      | _ -> false)

  // The panic button is not spent on a hole the instant cannot fill: 600 is
  // past the talented heal, 100 is a Flash-sized job.
  Assert.True(shockIssued (pulled 600), "a 600-point hole should draw Holy Shock")
  Assert.False(shockIssued (pulled 100), "a 100-point hole must not spend the 30 s cooldown")

  // The engine half: the real command through the real entry, with the paladin's
  // crit chance zeroed so the healed amount is the talent rider, not a crit roll.
  let w = SimState.mapEntity paladin.Id (fun e -> { e with CritChanceBp = 0 }) (pulled 600)
  let healthOf (w: World) = w.Entities |> List.find (fun e -> e.Id = warrior.Id) |> fun e -> e.Health
  let before = healthOf w
  let paladinBefore = w.Entities |> List.find (fun e -> e.Id = paladin.Id)

  let w' =
    SimCombat.applyCommand
      { ApplyAt = ticks 1
        Kind = UseAbility(paladin.Id, "Holy Shock", warrior.Id) }
      w

  let healed = healthOf w' - before
  let bonus = paladinBefore.BonusHealing

  // The bounds are the rank's 311-320 times Healing Light 3/3 — `healBp` carries
  // the 10000 baseline, so the multiplier is 11200 — and the lower bound alone is
  // the rider's proof, because an untalented roll bottoms out at 311.
  Assert.True(healed >= (311 + bonus) * 11200 / 10000, $"Holy Shock healed {healed}, below the talented floor")
  Assert.True(healed <= (320 + bonus) * 11200 / 10000, $"Holy Shock healed {healed}, above the rank's ceiling")

  let paladinAfter = w'.Entities |> List.find (fun e -> e.Id = paladin.Id)

  Assert.True
    (paladinAfter.Resource = paladinBefore.Resource - 150,
     $"Holy Shock cost {paladinBefore.Resource - paladinAfter.Resource}, not the 150 it is priced at")

  let cooldown = paladinAfter.Cooldowns |> Map.tryFind "Holy Shock" |> Option.defaultValue (ticks 0)

  Assert.True(cooldown > ticks 0, "Holy Shock must leave its cooldown behind")

  // And the cooldown is what keeps the AI honest: the same deep hole, re-cut,
  // draws no second Holy Shock while the button is down.
  let reCut =
    let missing = 800 - healthOf w'
    if missing < 600 then SimCombat.dealDamage warrior.Id (600 - missing) w' else w'

  Assert.False(shockIssued reCut, "Holy Shock on cooldown must not be re-ordered")

/// A pet is a 420-health body, and that is the whole problem with healing one: a
/// player-sized heal cannot land on it without mostly landing as overheal. Measured
/// before the cheap rung existed, `shmana 12 --class Shaman`: the Warlock's Imp took
/// 78 presses for 19,502 mana and 8,895 healing — 0.46 a point, against the Priest's
/// 3.05 on the same body with a 63-mana rung. So the pet case is asserted on its own
/// and not left to the hole-size test above: a 130-point hole on a 420-hp body is a
/// *small* hole in absolute terms and a third of the body, and the rank that answers
/// it must be chosen for the hole, not for the body's size class.
[<Fact>]
let ``a shaman spends the cheap rung on a pet and the trained rank on a player`` () =
  let party = Content.partyOf [ "Warrior"; "Shaman" ] 36
  let warrior = party |> List.find (fun e -> e.Name = "Warrior")
  let shaman = party |> List.find (fun e -> e.Name = "Shaman")

  let stand (e: Entity) (x: int) = { e with Pos = { X = x; Y = 9 }; Home = Some { X = x; Y = 9 } }

  // The Imp's shape, not its kit: a 420-health Party body one seat away from the
  // Shaman, so the only thing that differs from the player case below is how much
  // health a heal can land on.
  let pet =
    { shaman with
        Id = EntityId 101
        Name = "Imp"
        Role = Some Dps
        MaxHealth = 420
        Health = 420 }

  let pet = stand pet 12

  let mob = Content.mob 100 "Grunt" CreatureType.Humanoid 36 100000 1000 None [] None { X = 13; Y = 9 }

  let world =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 24 20
      Entities = [ stand warrior 10; stand shaman 11; pet; mob ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let pulled = SimCombat.dealDamage mob.Id 1 world
  // Four ticks of totems, as above: a Shaman answers "Stoneskin Totem" on every one.
  let settled = [ 1..4 ] |> List.fold (fun w _ -> SimTick.step (Bench.autoPilot w) w) pulled

  let healAt (hole: int) (target: Entity) =
    let cut =
      { settled with
          Entities = settled.Entities |> List.map (fun e -> if e.Id = target.Id then { e with Health = e.MaxHealth - hole } else e) }

    PartyAi.decide cut
    |> List.tryPick (fun c ->
      match c.Kind with
      | UseAbility(a, n, t) when a = shaman.Id && t = target.Id && n.StartsWith "Healing Wave" -> Some n
      | _ -> None)

  let onPet = healAt 130 pet
  // A player-sized body with the same 130-point hole is above the need-pool's three-
  // quarters bar, so the answer is no press at all — the other half of not pointing a
  // big heal at a small wound, and the mechanism `directFor` already had.
  let onPlayer = healAt 130 warrior
  Assert.True((onPet = Some "Healing Wave (Rank 4)"), $"a 130-point hole on a 420-hp body drew {onPet}")
  Assert.True((onPlayer = None), $"a 130-point hole on a player drew {onPlayer}")

/// The standing check was written when a totem was dropped for the pull it stood
/// in: count the slot as covered, and the Shaman stops re-dropping it. But a
/// totem never expires and never moves, and one World spans the whole wing, so
/// the set that covered the last pull still "stands" wherever the last pull was
/// — and a Shaman that has walked past the spec's own radius is standing in no
/// totem's reach at all. The slot must re-read as vacant the moment the Shaman
/// leaves the drop site behind: the drop point is the Shaman's own tile, so the
/// reach is measured from the Shaman, and the set is re-dropped where the next
/// fight will actually be.
[<Fact>]
let ``a totem left behind its radius is re-dropped where the shaman stands`` () =
  let party = Content.partyOf [ "Warrior"; "Shaman" ] 36
  let warrior = party |> List.find (fun e -> e.Name = "Warrior")
  let shaman = party |> List.find (fun e -> e.Name = "Shaman")

  // The real Stoneskin spec, so the radius the test measures against is the
  // radius the wing plays with.
  let spec =
    shaman.Abilities
    |> List.find (fun a -> a.Name = "Stoneskin Totem")
    |> fun a -> a.Effects |> List.pick (function Summon s -> Some s | _ -> None)
    |> fun s -> s.Totem.Value

  let stand (e: Entity) (x: int) = { e with Pos = { X = x; Y = 9 }; Home = Some { X = x; Y = 9 } }

  // The Stoneskin the last pull stood in, left far behind the Shaman — the wing's
  // own walk between packs.
  let behind =
    { (stand warrior 40) with
        Id = EntityId 99
        Name = "Stoneskin Totem"
        Abilities = []
        AutoAttack = None
        Owner = Some shaman.Id
        Totem = Some spec }

  let world totem =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 44 20
      Entities = [ stand warrior 10; stand shaman 11; totem ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let totemsOf (w: World) =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(a, n, _) when a = shaman.Id && n.EndsWith "Totem" -> Some n
      | _ -> None)

  let far = totemsOf (world behind)

  Assert.True(
    List.contains "Stoneskin Totem" far,
    sprintf "the totem 29 tiles behind the Shaman should read as vacant: %A" far
  )

  // The same totem one tile from the Shaman: the slot reads standing, and the
  // lane moves on to the slots that are actually vacant.
  let near = totemsOf (world { behind with Pos = { X = 12; Y = 9 } })

  Assert.False(
    List.contains "Stoneskin Totem" near,
    sprintf "a totem in reach should not be re-dropped: %A" near
  )

/// The water slot is the only one with two answers, and for the whole run it
/// gave just one: kit order puts Healing Stream first, the slot takes the first
/// row, and only Mana Tide (level 40) may displace it — so Mana Spring, 15 mana
/// every two seconds to every caster, was dead code and the party drank the
/// difference. The slot is a choice, not a kit order: Healing Stream when
/// somebody is health-pressed (below the healers' own calm gate of 60 per cent),
/// Mana Spring when the party is whole and mana is what is draining. Both are
/// asked of the same world so the flip is the only thing that moves.
[<Fact>]
let ``the water slot answers pressure: mana when whole, the stream when wounded`` () =
  let party = Content.partyOf [ "Warrior"; "Mage"; "Shaman" ] 36
  let warrior = party |> List.find (fun e -> e.Name = "Warrior")
  let mage = party |> List.find (fun e -> e.Name = "Mage")
  let shaman = party |> List.find (fun e -> e.Name = "Shaman")

  let stand (e: Entity) (x: int) = { e with Pos = { X = x; Y = 9 }; Home = Some { X = x; Y = 9 } }

  // `totemOrders` answers one row a call, in kit order, and the earth row walks
  // first - so the water slot is only reached when the earth slot is already
  // standing. A Stoneskin at the Shaman's own tile pins it, and the water row is
  // the next candidate the lane sees.
  let spec =
    shaman.Abilities
    |> List.find (fun a -> a.Name = "Stoneskin Totem")
    |> fun a -> a.Effects |> List.pick (function Summon s -> Some s | _ -> None)
    |> fun s -> s.Totem.Value

  let stoneskin =
    { (stand warrior 13) with
        Id = EntityId 99
        Name = "Stoneskin Totem"
        Abilities = []
        AutoAttack = None
        Owner = Some shaman.Id
        Totem = Some spec }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 44 20
      Entities = [ stand warrior 10; stand shaman 11; stand mage 12; stoneskin ]
      Pending = []
      Rng = Rng.streamsOf 12UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let water (w: World) =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(a, n, _) when
        a = shaman.Id && (n = "Healing Stream Totem" || n = "Mana Spring Totem")
        ->
        Some n
      | _ -> None)

  // A whole party whose casters are drained: the slot answers with mana.
  let drained =
    { w with
        Entities = w.Entities |> List.map (fun e -> if e.Id = mage.Id then { e with Resource = e.MaxResource / 3 } else e) }

  Assert.Equal<string list>([ "Mana Spring Totem" ], water drained)

  // The same party, the tank at two fifths: the slot answers with the stream.
  let wounded =
    { drained with
        Entities =
          drained.Entities
          |> List.map (fun e -> if e.Id = warrior.Id then { e with Health = e.MaxHealth * 2 / 5 } else e) }

  Assert.Equal<string list>([ "Healing Stream Totem" ], water wounded)

/// Combat was the totem lane's gate, and the gate was one pull wide: the set was
/// dropped during the first ticks of a fight, then the lane went quiet until the
/// next pull was already underway. The wing is one World, so the approach to a
/// pack is exactly when the set left at the last pull wants re-dropping — not
/// the pull's own first ticks, which are for the kick and the shield. Out of
/// combat, with nothing to recover and nobody to raise, the lane answers with
/// totems; recovery keeps its priority and stays silent, and the in-combat order
/// is untouched.
[<Fact>]
let ``out of combat the totem set is re-established`` () =
  let party = Content.partyOf [ "Warrior"; "Shaman" ] 36
  let warrior = party |> List.find (fun e -> e.Name = "Warrior")
  let shaman = party |> List.find (fun e -> e.Name = "Shaman")

  let stand (e: Entity) (x: int) = { e with Pos = { X = x; Y = 9 }; Home = Some { X = x; Y = 9 } }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 24 20
      Entities = [ stand warrior 10; stand shaman 11 ]
      Pending = []
      Rng = Rng.streamsOf 7UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  Assert.False(SimState.inCombat w, "the world must start out of combat")

  let first =
    PartyAi.decide w
    |> List.tryPick (fun c ->
      match c.Kind with
      | UseAbility(a, n, _) when a = shaman.Id && n.EndsWith "Totem" -> Some n
      | _ -> None)

  // First row of the kit, so first order of the set.
  Assert.Equal(Some "Stoneskin Totem", first)

  // And the orders actually land: a few Ticks of auto-pilot put the set on the
  // floor with the party still out of combat.
  let after = [ 1..6 ] |> List.fold (fun w _ -> SimTick.step (Bench.autoPilot w) w) w

  Assert.False(SimState.inCombat after, "no pull should have happened")

  Assert.True(
    after.Entities |> List.exists (fun e -> SimState.alive e && e.Totem.IsSome),
    "the idle lane should have put a totem on the floor"
  )

/// The budget gate's floor is `ManaBudget.marginHeals` (two) times `oneHealCost`,
/// and `oneHealCost` is the *dearest* heal in the kit — deliberately, because the
/// reserve must cover the worst heal the healer would cast. That floor is also the
/// whole burst budget: the gate lets the healer spend its pool down to it, so what
/// the wall buys is exactly how many heals it can answer a burst with. The Priest's
/// wall (2 x 259) buys eight Lesser Heals at 63; the Shaman's, before the cheap rung,
/// bought exactly two at 251 — and "two" is the same number `marginHeals` asks for,
/// so any heal needed before the 20 s draw mean caught up was refused. Measured,
/// `shmana 12`: 1,867 mana-blocked ticks within 20 s of the Shaman's own damage cast,
/// against 177 for the Priest. Three presses per wall is the assertion, not a tuning
/// constant: two is the floor the gate reserves, so a wall that buys two is a wall
/// that buys nothing spare.
[<Fact>]
let ``a shaman's reserve wall covers three heals, not two`` () =
  let shaman =
    Content.partyOf [ "Warrior"; "Shaman" ] 36 |> List.find (fun e -> e.Name = "Shaman")

  let heals =
    shaman.Abilities
    |> List.filter (fun a ->
      a.Effects
      |> List.exists (function
        | Heal _ -> true
        | _ -> false))

  let dearest = heals |> List.map (fun a -> a.ResourceCost) |> List.max
  let cheapest = heals |> List.map (fun a -> a.ResourceCost) |> List.min

  Assert.True(
    cheapest * 3 <= dearest * 2,
    $"two heals at the dearest price ({dearest}) buy {(dearest * 2) / cheapest} presses at the cheapest ({cheapest})"
  )

/// Cooldowns, casts and every selector row are keyed by ability *name*, so a second
/// row also called "Healing Wave" would share the trained rank's cooldown and log
/// row — and `Talents.spell` is a `List.tryFind`, so a duplicate talent row for one
/// spell is silently dead rather than loudly wrong. Hence the rank carries its rank
/// in its name, and the row is strictly cheaper and strictly smaller than the one it
/// tops up, which is the only thing that makes it worth casting.
[<Fact>]
let ``the shaman's downrank is cheaper and smaller under a name of its own`` () =
  let shaman =
    Content.partyOf [ "Warrior"; "Shaman" ] 36 |> List.find (fun e -> e.Name = "Shaman")

  let rows = shaman.Abilities |> List.filter (fun a -> a.Name.StartsWith "Healing Wave")

  // One row per rank, and the trained one keeps its plain name.
  Assert.Equal(2, rows.Length)

  let top = rows |> List.find (fun a -> a.Name = "Healing Wave")
  let small = rows |> List.find (fun a -> a.Name <> "Healing Wave")

  let ceiling (a: Ability) =
    a.Effects
    |> List.sumBy (fun e ->
      match e with
      | Heal(_, mx) -> mx
      | _ -> 0)

  Assert.True(
    small.ResourceCost < top.ResourceCost,
    $"the top-up costs {small.ResourceCost}, the top rank {top.ResourceCost}"
  )

  Assert.True(
    ceiling small < ceiling top,
    $"the top-up heals {ceiling small}, the top rank {ceiling top}"
  )

/// Orc base (43, 28, 45, 39, 49) plus the Druid's verified ≤24 pieces.
[<Fact>]
let ``the shaman is built on the researched level-24 set`` () =
  let roster = [ "Warrior"; "Shaman"; "Warlock"; "Mage"; "Druid" ]

  let shaman =
    Content.partyOf roster 24 |> List.find (fun e -> e.Name = "Shaman")

  // Gear totals before the passives: +0 Str, +7 Agi, +22 Sta, +48 Int, +37 Spi.
  // Arctic Buckler is the +3 Sta / +8 Spi; the rest is the Druid's set.
  Assert.Equal(
    (43, 35, 67, 87, 86),
    (shaman.Stats.Strength,
     shaman.Stats.Agility,
     shaman.Stats.Stamina,
     shaman.Stats.Intellect,
     shaman.Stats.Spirit)
  )

  // Base health 234, base mana 476; Stamina 71 and Intellect 98 once Mark of the
  // Wild and Arcane Intellect are up, and 102 after Ancestral Knowledge's 5% —
  // which now raises "the total value of all your stats" (`AllStatsBp = 500`),
  // so Stamina 73 folds to 76 and the pool reads the folded number.
  Assert.Equal(794, shaman.MaxHealth)
  Assert.Equal(1726, shaman.MaxResource)
  // The shield's 642 on top of the eight leather and cloth pieces' 238, Mark of
  // the Wild's 105, and Agility 39 folded to 40 by Ancestral Knowledge's 5%.
  Assert.Equal(1065, shaman.Armor)
  // The assumed one-hander: the staff's 3.00 s rung less twelve points off each end.
  // See `Content.shaman`'s doc comment — the item is an assumption and is labelled
  // as one, because no <=24 one-hand caster weapon is sourced.
  Assert.Equal((35, 59), (shaman.AutoAttack.Value.Min, shaman.AutoAttack.Value.Max))

/// Ancestral Knowledge 5/5 — "the total value of all your stats"
/// (`.pi/research/octowow/shaman.md` §5, the costs list): every stat up 5%
/// before the pools are derived. The health pin above reads the Stamina leg;
/// here are two more — Agility's, in dodge, and Spirit's, in regeneration.
[<Fact>]
let ``Ancestral Knowledge raises every stat before the pools are derived`` () =
  let roster = [ "Warrior"; "Shaman"; "Warlock"; "Mage"; "Druid" ]

  let shaman =
    Content.partyOf roster 24 |> List.find (fun e -> e.Name = "Shaman")

  // Agility 39 buffed (35 dressed, Mark of the Wild's 4) folds to 40: the fifth
  // point of dodge per Agility, where Intellect alone would have left 39.
  Assert.Equal(700, shaman.DodgeChanceBp)
  // Spirit 90 buffed folds to 94: the client's `13 + Spirit / 4` regen is 36
  // where the unfolded 90 would pay 35.
  Assert.Equal(36, shaman.ResourceRegenPerTick)

/// The healing talents are worth real healing and not just a number on a sheet:
/// one Healing Touch from the talented Druid lands a strictly larger heal than the
/// same Druid with every point taken out, on the same seed and the same wound.
/// Gift of Nature's 10% is well clear of the rank's roll.
///
/// This test has moved twice, and both moves were forced by measurement rather
/// than convenience. It started on the Shaman, where Purification's 10% made the
/// two apart — but OctoWoW deleted Purification, so a level-24 shaman now heals
/// exactly what an untalented one heals (both 449 at the first Healing Wave). It
/// then tried the Paladin, where Healing Light only touches Flash of Light while
/// the AI casts Holy Light. The Druid is the healer whose tree carries a flat
/// bonus on every heal it has, so the property is testable there and nowhere else.
/// The wound is 50 of a full bar on purpose: healing a nearly-full tank caps both
/// numbers at "topped up" and the talent becomes invisible.
[<Fact>]
let ``a talented druid's heal beats the same druid with no talents`` () =
  let party = Content.partyOf [ "Warrior"; "Druid" ] 24
  let tank = party.[0]
  let druid = party.[1]

  let firstHeal (healer0: Entity) =
    let wounded =
      { tank with
          Pos = { X = 5; Y = 5 }
          Health = 50 }

    let healer = { healer0 with Pos = { X = 5; Y = 6 } }
    let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

    let enemy =
      { Content.mob 100 "Near" CreatureType.Humanoid 21 4000 2000 (Some tap) [] None { X = 6; Y = 5 } with
          Engaged = true
          Target = Some wounded.Id }

    let mutable w =
      { Tick = ticks 0
        RestTicks = ticks 0
        Grid = Grid.create 12 10
        Entities = [ wounded; healer; enemy ]
        Pending = []
        Rng = Rng.streamsOf 5UL
        PartyBuffs = []
        Quests = []
        Forecast = Forecast.empty
        TotemLedger = TotemLedger.empty
        Log = [] }

    let mutable amount = 0

    for _ in 1..300 do
      if amount = 0 then
        w <- SimTick.step (Bench.autoPilot w) w

        match w.Log |> List.tryFind (fun l -> l.StartsWith "Druid's " && l.Contains " heals Warrior for ") with
        | Some l ->
          match System.Int32.TryParse (l.Split ' ' |> Array.last) with
          | true, n -> amount <- n
          | _ -> ()
        | None -> ()

    amount

  let talented = firstHeal druid
  let plain = firstHeal { druid with Talents = Talents.none }

  Assert.True(talented > 0, "the talented druid should land a heal at all")

  Assert.True(
    talented > plain,
    sprintf "the talented druid should heal for more: %d vs %d" talented plain
  )

/// The damage half of the tree, held to the same promise as the healing half:
/// a talent that raises damage raises the number that lands, on a weapon strike
/// and on a nuke alike, and a build with no points multiplies by exactly 10000.
/// The same seed gives the same rolls, so the only difference between the two
/// runs is the talent.
[<Fact>]
let ``a damage talent raises a weapon strike and a nuke alike`` () =
  let party = Content.partyOf [ "Warrior"; "Rogue"; "Mage" ] 24
  let warrior = party.[0]
  let rogue = party.[1]
  let mage = party.[2]

  let dealtBy (hero: Entity) (ability: string) =
    let tank = { warrior with Pos = { X = 5; Y = 5 } }

    // Enough health that nothing dies mid-measurement. Same level as the party:
    // a level-60 dummy makes every swing miss, which measures the hit table
    // rather than the talent.
    let dummy =
      { Content.mob 100 "Dummy" CreatureType.Humanoid 24 400000 400000 None [] None { X = 6; Y = 5 } with
          Engaged = true
          Target = Some tank.Id }

    let hero = { hero with Pos = { X = 5; Y = 6 } }

    let mutable w =
      { Tick = ticks 0
        RestTicks = ticks 0
        Grid = Grid.create 12 10
        Entities = [ tank; hero; dummy ]
        Pending = []
        Rng = Rng.streamsOf 5UL
        PartyBuffs = []
        Quests = []
        Forecast = Forecast.empty
        TotemLedger = TotemLedger.empty
        Log = [] }

    let damageIn (line: string) =
      let parts = line.Split ' '

      // A crit says "critically hits", which is a different string: a helper that
      // only knows the plain one silently drops every crit, and a crit talent then
      // measures as zero.
      let hit = line.Contains(" " + ability + " hits ") || line.Contains(" " + ability + " critically hits ")

      if parts.[0] = hero.Name + "'s" && hit then
        match System.Int32.TryParse parts.[parts.Length - 1] with
        | true, n -> n
        | _ -> 0
      else
        0

    // The **first** landing hit, not a fight's total. Two whole runs diverged the
    // moment the mage gained a window row to press — different kill order, different
    // rolls — and the sums stopped being comparable at all: a 3% gap the wrong way
    // read as "the talent lowered damage". A single hit off the same seed differs
    // only by the multiplier under test.
    let mutable dealt = 0

    for _ in 1..1200 do
      if dealt = 0 then
        w <- SimTick.step (Bench.autoPilot w) w
        dealt <- w.Log |> List.sumBy damageIn

    dealt

  let talented = { Talents.none with DamageDoneBp = 1000 }

  for hero, ability in [ rogue, "Backstab"; mage, "Arcane Missiles" ] do
    // Both arms carry `Talents.none` underneath, so the only difference between them
    // is the 10% under test. The hero's own build is not neutral any more: the mage's
    // Arcane Instability is now a 25% eruption, which out-punches a flat 10% and would
    // read as "the talent lowered damage".
    let plain = dealtBy { hero with Talents = Talents.none } ability
    let boosted = dealtBy { hero with Talents = talented } ability

    Assert.True(plain > 0, sprintf "expected %s to land a %s at all, dealt %d" hero.Name ability plain)

    // Not exactly 10%: the talent scales the swing, and armour and the attack-power
    // bonus on top of it answer to their own arithmetic. Measured, a 10% Backstab
    // arrives as 270 -> 284. What this pins is that the field reaches a sword and a
    // spell alike; how much survives mitigation is the talent tests' business.
    Assert.True(
      boosted > plain,
      sprintf "a 10%% damage talent should raise %s's %s: %d -> %d" hero.Name ability plain boosted
    )

/// The Druid's two changes: the Nature's Swiftness button, which the class was
/// missing while the Shaman had it, and Improved Regrowth's crit chance, which is
/// the talent that makes Regrowth the Druid's crit heal. Both are content, so both
/// are asserted on the built hero rather than inferred from the sim.
[<Fact>]
let ``the druid carries Nature's Swiftness and a critting Regrowth`` () =
  let druid =
    Content.partyOf [ "Warrior"; "Druid" ] 33 |> List.find (fun e -> e.Name = "Druid")

  let effectsOf name =
    (druid.Abilities |> List.find (fun a -> a.Name = name)).Effects

  Assert.Contains("Nature's Swiftness", druid.Abilities |> List.map (fun a -> a.Name))
  Assert.Contains(CritBonus 5000, effectsOf "Regrowth")
  // The talent raises one spell, not the caster.
  Assert.DoesNotContain(CritBonus 5000, effectsOf "Healing Touch")
  Assert.DoesNotContain(CritBonus 5000, effectsOf "Rejuvenation")

/// A heal crit is half again the heal. It has to be, or a crit talent on a heal is
/// a placebo: Improved Regrowth's +50% would have changed nothing at all, because
/// the crit used to pay only Inspiration's armour and Illumination's refund.[<Fact>]
let ``a heal that crits is worth half again`` () =
  let party = Content.partyOf [ "Warrior"; "Priest" ] 24
  let warrior, priest = party.[0], party.[1]

  // A flat heal, so the only thing that can move the number is the crit. The
  // Warrior casts it because the Priest's talents would add bonus healing.
  let flat =
    { Name = "Test Heal"
      CastTicks = ticks 0
      CooldownTicks = ticks 0
      ResourceCost = 0
      Stance = None
      Interruptible = false
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(100, 100) ] }

  let healedWith (critBp: int) =
    let wounded =
      { priest with
          Pos = { X = 5; Y = 5 }
          Health = 300 }

    let healer =
      { warrior with
          Pos = { X = 5; Y = 6 }
          CritChanceBp = critBp }

    let w0 =
      { Tick = ticks 0
        RestTicks = ticks 0
        Grid = Grid.create 12 10
        Entities = [ wounded; healer ]
        Pending = []
        Rng = Rng.streamsOf 5UL
        PartyBuffs = []
        Quests = []
        Forecast = Forecast.empty
        TotemLedger = TotemLedger.empty
        Log = [] }

    let w = SimCombat.resolveAbility healer.Id wounded.Id flat w0
    (w.Entities |> List.find (fun e -> e.Id = wounded.Id)).Health - 300

  // 10000 basis points is certainty, so neither case depends on the seed.
  Assert.Equal(100, healedWith 0)
  Assert.Equal(150, healedWith 10000)

/// The operator's rule for Swiftmend: use it near the end of a heal-over-time, and
/// hold it back when it would only overheal. The instant is a **fixed** 12 s of
/// Rejuvenation whatever is left on the buff, so pressing at 55% on one with ten
/// seconds still to run pays the same 90 and throws those ten seconds away — mana
/// already spent, burnt. The emergency trip point buys no exemption: under Tree of
/// Life the button has no cooldown at all, so holding it costs only the heal. This
/// used to assert the opposite, which is exactly what the ledger caught.
[<Fact>]
let ``the druid holds Swiftmend until the HoT is nearly spent, even in an emergency`` () =
  // Real content: a healer only acts in a fight, so a hand-built pair with no
  // hostile in the world makes `decide` return nothing and every assertion below
  // passes on an empty list.
  let world0 =
    Content.dungeonAt 12 1UL
    |> Content.withRosterAt 12 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]
    // In a fight, because out of one the leader now waits for the party to be whole
    // (five seconds of regeneration) and issues no orders at all.
    |> fun w ->
      { w with
          Entities =
            w.Entities
            |> List.map (fun e -> if e.Faction = Hostile then { e with Engaged = true } else e) }

  let warrior = world0.Entities |> List.find (fun e -> e.Name = "Warrior")
  let druid = world0.Entities |> List.find (fun e -> e.Name = "Druid")

  // What is left on the Rejuvenation, in Ticks — zero for no buff at all.
  let world (healthPct: int) (left: int) =
    { world0 with
        Entities =
          world0.Entities
          |> List.map (fun e ->
            if e.Id = warrior.Id then
              { e with
                  Health = e.MaxHealth * healthPct / 100
                  Auras =
                    if left > 0 then
                      [ Rejuvenation(ticks left, 5, "Rejuvenation") ]
                    else
                      [] }
            elif e.Id = druid.Id then
              { e with Resource = e.MaxResource }
            else
              e) }

  let druidCasts (w: World) =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(actor, name, _) when actor = druid.Id -> Some name
      | _ -> None)

  // Twenty Ticks is the buff's last second: every Tick it had is kept and the whole
  // 60-point instant lands on a 274-point hole, so this press pays twice over.
  Assert.Contains("Swiftmend", druidCasts (world 55 20))
  // Ten seconds still to run: the press would destroy 50 points of tick whose mana
  // was already paid, so the buff is left to tick and the button held.
  Assert.DoesNotContain("Swiftmend", druidCasts (world 55 240))
  // No HoT to consume, no Swiftmend: Nature's Swiftness is the other fast heal.
  Assert.DoesNotContain("Swiftmend", druidCasts (world 55 0))
  Assert.Contains("Nature's Swiftness", druidCasts (world 55 0))

/// The Restoration layering, asserted through `decide` rather than through the
/// helpers, because the bug being guarded against was the selectors walking the kit
/// in the opposite order to what their comments claimed: `hots` was sorted by
/// *descending* heal, so `hot` and `hotFor` reached for the 396-mana Regrowth the
/// instant it lapsed, whatever the size of the hole, and 43% of the Druid's mana went
/// out through it. Layer order is Rejuvenation (160, establishing), Regrowth (396,
/// on a body already wearing Rejuvenation), then Healing Touch (the top-up).
///
/// Every heal-over-time in the sim is the one `Rejuvenation` aura case carrying the
/// spell's name as its source, so a Regrowth tick is `Rejuvenation(_, _, "Regrowth")`.
let private druidCastsAgainst
  (healthPct: int)
  (auras: Aura list)
  (cooldowns: (string * int<tick>) list)
  (stance: Stance option)
  =
  let world0 =
    Content.dungeonAt 36 1UL
    |> Content.withRosterAt 36 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]
    // In a fight, because out of one the leader waits for the party to be whole and
    // the healer issues no orders at all — which would pass every negative below.
    |> fun w ->
      { w with
          Entities =
            w.Entities
            |> List.map (fun e -> if e.Faction = Hostile then { e with Engaged = true } else e) }

  let warrior = world0.Entities |> List.find (fun e -> e.Name = "Warrior")
  let druid = world0.Entities |> List.find (fun e -> e.Name = "Druid")

  let world =
    { world0 with
        Entities =
          world0.Entities
          |> List.map (fun e ->
            if e.Id = warrior.Id then
              { e with Health = e.MaxHealth * healthPct / 100; Auras = auras }
            elif e.Id = druid.Id then
              // In Human Form, which is the shape a Druid is born in and the one
              // Healing Touch needs. Left at the roster's starting Tree Form the
              // decision spends its Tick on the shift and the heal lands next Tick,
              // which is the form churn itself rather than the layering under test.
              { e with
                  Resource = e.MaxResource
                  Cooldowns = Map.ofList cooldowns
                  // `None` leaves the form the roster made him in: the Druid is born
                  // in Tree Form here, and starting him elsewhere just makes `home`
                  // spend the Tick shifting back, which is the churn, not the layering.
                  Auras =
                    match stance with
                    | Some st -> [ Shaped st ]
                    | None -> e.Auras }
            else
              e) }

  PartyAi.decide world
  |> List.choose (fun c ->
    match c.Kind with
    | UseAbility(actor, name, _) when actor = druid.Id -> Some name
    | _ -> None)

/// Pre-emptive, and with the cheap HoT: Rejuvenation goes on the Tank before he is
/// hurt, on the strength of taking swings at all rather than on having lost health.
[<Fact>]
let ``the druid opens with Rejuvenation on an untouched tank`` () =
  let casts = druidCastsAgainst 100 [] [] None
  Assert.Contains("Rejuvenation", casts)
  // One order per Tick, so the expensive second layer is not applied in the same breath.
  Assert.DoesNotContain("Regrowth", casts)

/// Re-growth is never re-applied while its own buff is still ticking: the direct half
/// would land on health the ticking half has already promised, which is overheal at
/// 396 a press.
[<Fact>]
let ``the druid does not re-cast Regrowth while its own HoT is ticking`` () =
  let both = [ Rejuvenation(ticks 200, 5, "Rejuvenation"); Rejuvenation(ticks 200, 5, "Regrowth") ]
  Assert.DoesNotContain("Regrowth", druidCastsAgainst 80 both [] None)

/// ...and the control for it, so the rule above cannot be satisfied by a Druid that
/// simply never heals: with the buff gone, Regrowth goes straight back up. The Tank
/// sits at 70% rather than 80% so the hole can take Regrowth's 478-point up-front
/// half — `worthPressing` buys that half only when it lands whole, and at 80% it
/// would not.
[<Fact>]
let ``the druid re-casts Regrowth once its own HoT has lapsed`` () =
  let casts = druidCastsAgainst 70 [ Rejuvenation(ticks 200, 5, "Rejuvenation") ] [] None
  Assert.Contains("Regrowth", casts)

/// A heal-over-time is left alone until its last tick. `Aura.refresh` replaces rather
/// than stacks, so re-pressing a live aura buys cover the pool has already paid for:
/// at the four seconds this used to allow, a Rejuvenation with four of its twelve
/// ticks still owed was replaced 209 times a run for 9,624 mana. Three seconds is
/// what the early press looked like; a second and a half is the tail it is now held
/// for, and the pair is what makes the negative above non-vacuous.
///
/// The Tank is at 98% so Swiftmend cannot take the tail first: it sits above the
/// refresh and refuses a press that would land whole as overheal, which is the one
/// case that leaves the refresh as the order under test.
[<Fact>]
let ``the druid leaves a Rejuvenation alone until its last tick`` () =
  let threeLeft = druidCastsAgainst 98 [ Rejuvenation(secTicks 3.0, 5, "Rejuvenation") ] [] None
  Assert.DoesNotContain("Rejuvenation", threeLeft)
  let lastTick = druidCastsAgainst 98 [ Rejuvenation(secTicks 1.5, 5, "Rejuvenation") ] [] None
  Assert.Contains("Rejuvenation", lastTick)

/// Regrowth's up-front half is most of what it costs, so it is only bought when the
/// hole can absorb it. At 90% the hole is smaller than the 478-point heal and the
/// pressed half lands as overheal — which it did on 178 of 186 casts, at 420 mana
/// apiece, while Rejuvenation covered the same body for 160. At 65% the hole takes the
/// whole half, so the layering goes up as the operator asked.
[<Fact>]
let ``the druid does not press Regrowth for a hole its direct half cannot fill`` () =
  let closed = druidCastsAgainst 90 [ Rejuvenation(ticks 200, 5, "Rejuvenation") ] [] None
  Assert.DoesNotContain("Regrowth", closed)
  let open' = druidCastsAgainst 65 [ Rejuvenation(ticks 200, 5, "Rejuvenation") ] [] None
  Assert.Contains("Regrowth", open')

/// The direct heal is the top of the layering, not the bottom. With only Rejuvenation
/// up the Druid establishes the missing Regrowth instead: a Healing Touch there would
/// pay top-rank rates for health the missing HoT covers for free.
[<Fact>]
let ``the druid establishes the missing HoT before casting Healing Touch`` () =
  let casts = druidCastsAgainst 65 [ Rejuvenation(ticks 200, 5, "Rejuvenation") ] [] None
  Assert.DoesNotContain("Healing Touch", casts)
  Assert.DoesNotContain("Healing Touch (Rank 4)", casts)
  Assert.Contains("Regrowth", casts)

/// ...and with both HoTs actually ticking, the direct heal is allowed — which is the
/// shape the operator asked for. Both are nearly spent, so the Tank still has a hole:
/// a long-running HoT closes it on paper and the request would be vacuous.
[<Fact>]
let ``the druid casts Healing Touch on a target already wearing both HoTs`` () =
  let both = [ Rejuvenation(ticks 200, 5, "Rejuvenation"); Rejuvenation(ticks 200, 5, "Regrowth") ]
  // Swiftmend on cooldown: it eats a HoT for an instant heal, and this asserts the
  // layering branch rather than the emergency one.
  let cooldowns = [ "Swiftmend", secTicks 30.0; "Nature's Swiftness", secTicks 30.0 ]
  let casts = druidCastsAgainst 50 both cooldowns (Some HumanForm)
  Assert.True(casts |> List.exists (fun n -> n.StartsWith "Healing Touch"), sprintf "druid cast: %A" casts)

/// The low rank exists so a top-up does not arrive at 301 mana for 694 healing. It is
/// a named row rather than a second "Healing Touch" because cooldowns, casts and HoT
/// lookups are all keyed by name.
[<Fact>]
let ``the druid carries a downranked Healing Touch for top-ups`` () =
  let druid =
    Content.dungeonAt 36 1UL
    |> Content.withRosterAt 36 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]
    |> fun w -> w.Entities |> List.find (fun e -> e.Name = "Druid")

  let cost name =
    druid.Abilities |> List.find (fun a -> a.Name = name) |> fun a -> a.ResourceCost

  Assert.True(cost "Healing Touch (Rank 4)" < cost "Healing Touch")
  Assert.True(cost "Healing Touch (Rank 4)" > 0)

// ===========================================================================
// Tree of Life, Genesis and Preservation (OctoWoW Restoration, row 6)
// ===========================================================================

/// The 36 roster, engaged, with the Druid's auras and the Tank's health set. Every
/// Tree of Life assertion below starts here: the Druid of that roster is the healer
/// the form belongs to and carries the Restoration build as *data* on the entity, so
/// Genesis and Preservation are tested through `druid.Talents` — `druidTalents` is
/// private to `Content` and a test that reached into it would stop testing the
/// roster that ships.
let private treeParty (druidAuras: Aura list) (tankPct: int) : World =
  let w =
    Content.dungeonAt 36 1UL
    |> Content.withRosterAt 36 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]
    // In a fight: `PartyAi.decide` issues nothing out of one, so an unengaged
    // world makes every AI assertion below pass on an empty list.
    |> fun w ->
      { w with
          Entities =
            w.Entities
            |> List.map (fun e -> if e.Faction = Hostile then { e with Engaged = true } else e) }

  let druid = w.Entities |> List.find (fun e -> e.Name = "Druid")
  let tank = w.Entities |> List.find (fun e -> e.Name = "Warrior")

  { w with
      Entities =
        w.Entities
        |> List.map (fun e ->
          if e.Id = druid.Id then
            { e with Resource = e.MaxResource; Auras = druidAuras }
          elif e.Id = tank.Id then
            { e with Health = e.MaxHealth * tankPct / 100 }
          else
            e) }

let private named (name: string) (w: World) =
  w.Entities |> List.find (fun e -> e.Name = name)

/// "+180% armour contribution from items": `ItemArmor` is the gear share of `Armor`,
/// so the form adds 180% of *that share* and leaves armour from anywhere else — a
/// buff, a totem — unscaled. Asserted by handing a hand-built target the armour the
/// rule predicts and demanding the same damage, because `meleeStrike` rolls the
/// attack table before it applies armour: a bare damage number would mostly be
/// testing the roll. The biggest hit over a fixed run of seeds is the rolled damage
/// with avoidance out of the way, and it is deterministic for that seed set.
[<Fact>]
let ``Tree of Life multiplies worn armour by 180 percent and only worn armour`` () =
  let biggestHit (armor: int) (auras: Aura list) =
    let w = treeParty auras 100
    let druid = named "Druid" w
    let mob = w.Entities |> List.find (fun e -> e.Faction = Hostile)
    let w = SimState.mapEntity druid.Id (fun e -> { e with Armor = armor }) w
    let druid = named "Druid" w

    [ for seed in 1UL..40UL ->
        let dealt, _, _ = SimCombat.meleeStrike mob druid.Id 1000 0 None { w with Rng = Rng.streamsOf seed }
        dealt ]
    |> List.max

  let worn = (named "Druid" (treeParty [] 100)).ItemArmor
  Assert.True(worn > 0, "the roster Druid wears no armour, so there is nothing to multiply")

  // Armour the Druid has from somewhere other than gear. +180% is worded "from
  // items", so this share must not be multiplied through.
  let buff = 1000
  let inTree = biggestHit (worn + buff) [ Shaped TreeForm ]
  let bare = biggestHit (worn + buff) []
  // The same target with the armour the rule predicts, and no form at all: if the
  // code scaled the whole `Armor` value instead of the gear share, this would be
  // lighter than the tree and the equality below would fail.
  let byHand = biggestHit (worn + buff + worn * 180 / 100) []

  Assert.True(inTree < bare, sprintf "the tree took %d, bare took %d" inTree bare)
  Assert.Equal(byHand, inTree)

/// "Healing power of nearby party members is increased by an amount equal to 20% of
/// your Spirit": `Spirit / 5`, summed over the party's trees so a second
/// Restoration Druid is a second share, and gone the moment the form is. It is read
/// at *both* heal sites, so both are asserted — the helper alone would pass while
/// the heals ignored it.
[<Fact>]
let ``Tree of Life gives the party a fifth of the Druid's Spirit as healing`` () =
  let tree = treeParty [ Shaped TreeForm ] 100
  let druid = named "Druid" tree
  Assert.Equal(druid.Stats.Spirit / 5, SimCombat.treeOfLifeHealing tree)
  Assert.Equal(0, SimCombat.treeOfLifeHealing (treeParty [ Shaped HumanForm ] 100))

  // The HoT half. A HoT's per-Tick number is fixed when the aura is created, so the
  // share has to be there at application time and is spread across the spell's own
  // seconds — per second it would be worth `duration` times what the same bonus is
  // worth to a direct heal.
  let perTick (auras: Aura list) =
    let w = treeParty auras 100
    let d = named "Druid" w
    let t = named "Warrior" w
    let rejuv = d.Abilities |> List.find (fun a -> a.Name = "Rejuvenation")
    let w = SimCombat.resolveAbility d.Id t.Id rejuv w

    (named "Warrior" w).Auras
    |> List.pick (function
      | Rejuvenation(_, n, "Rejuvenation") -> Some n
      | _ -> None)

  Assert.True(
    perTick [ Shaped TreeForm ] > perTick [ Shaped HumanForm ],
    "the Druid's Spirit share never reached the heal-over-time"
  )

  // ...and the direct half. Same seed on both sides, so the heal roll and the crit
  // roll are identical and the only thing that can move the number is the share.
  let healed (auras: Aura list) =
    let w = treeParty auras 20
    let d = named "Druid" w
    let t = named "Warrior" w
    let ht = d.Abilities |> List.find (fun a -> a.Name = "Healing Touch")
    let w = SimCombat.resolveAbility d.Id t.Id ht w
    (named "Warrior" w).Health

  Assert.True(healed [ Shaped TreeForm ] > healed [ Shaped HumanForm ], "the Spirit share never reached the direct heal")

/// The form's half of the bargain: heal-over-time spells cost a fifth less, so the
/// 160-mana Rejuvenation is 128 in the tree. Identified by the `HealOverTime` effect
/// and not by name, so Regrowth's tail is discounted too — and Healing Touch, which
/// has no periodic half, is not. The discount is on the *list* price, so Tranquil
/// Spirit's 10% off Healing Touch is neither multiplied nor lost.
[<Fact>]
let ``Tree of Life makes heal over time spells cost a fifth less`` () =
  let costIn (stance: Stance) (name: string) =
    let w = treeParty [ Shaped stance ] 100
    let d = named "Druid" w
    let t = named "Warrior" w
    let ability = d.Abilities |> List.find (fun a -> a.Name = name)
    let w = SimCombat.resolveAbility d.Id t.Id ability w
    d.Resource - (named "Druid" w).Resource

  Assert.Equal(160, costIn HumanForm "Rejuvenation")
  Assert.Equal(128, costIn TreeForm "Rejuvenation")
  // Regrowth is one row carrying a direct heal and a tail, and the tail is enough
  // to make it a heal-over-time spell.
  Assert.Equal(costIn HumanForm "Regrowth" * 8000 / 10000, costIn TreeForm "Regrowth")
  // Healing Touch is a direct heal and keeps its price.
  Assert.Equal(costIn HumanForm "Healing Touch", costIn TreeForm "Healing Touch")

/// The form's bans, and the one exception the operator added. Healing Touch's ban is
/// not new — the row names `HumanForm`, and the form gate refuses anything that names
/// a shape the caster is not in — so the tree inherits it and the damaging-spell ban
/// is the only new refusal. On Nature's Swiftness the gate is waived, because an
/// instant heal is the one thing the form's own heal-over-times cannot do. Swiftmend
/// is the other half of the house rule: no cooldown while shaped.
[<Fact>]
let ``Tree of Life refuses Healing Touch and damaging spells unless Nature's Swiftness is up`` () =
  let cast (auras: Aura list) (name: string) =
    let w = treeParty auras 60
    let d = named "Druid" w
    let t = named "Warrior" w
    // Something for Swiftmend to eat, so that cast is a real one.
    let w =
      SimState.mapEntity
        t.Id
        (fun e -> { e with Auras = [ Rejuvenation(secTicks 12.0, 5, "Rejuvenation") ] })
        w

    SimCombat.applyCommand { ApplyAt = ticks 1; Kind = UseAbility(d.Id, name, t.Id) } w

  let refused (w: World) (why: string) =
    w.Log |> List.exists (fun line -> line.Contains why)

  Assert.True(refused (cast [ Shaped TreeForm ] "Healing Touch") "wrong form")
  Assert.True(refused (cast [ Shaped TreeForm ] "Wrath") "cannot cast Wrath in Tree of Life")
  Assert.False(refused (cast [ Shaped HumanForm ] "Healing Touch") "wrong form")

  // Nature's Swiftness: the buff is `Instant`, and the gate asks the buff rather than
  // the ability, so the same Healing Touch that was refused a line above now lands.
  let naturesSwiftness = Empowered(secTicks 10.0, { Empower.none with Instant = true })
  let swift =
    cast [ Shaped TreeForm; naturesSwiftness ] "Healing Touch"

  Assert.False(refused swift "wrong form")
  Assert.True((named "Warrior" swift).Health > (named "Warrior" (treeParty [] 60)).Health)

  let cooldownLeft (auras: Aura list) =
    (named "Druid" (cast auras "Swiftmend")).Cooldowns |> Map.containsKey "Swiftmend"

  Assert.False(cooldownLeft [ Shaped TreeForm ], "Swiftmend kept its cooldown in the tree")
  Assert.True(cooldownLeft [ Shaped HumanForm ])

/// Genesis 5/5 is `PeriodicBp` 1500 on the roster Druid, and the axis exists rather
/// than a second `HealDoneBp` line because the talent names *periodic*: the HoT tick
/// grows and Regrowth's up-front half does not. 15% of the tick, not 15% of the
/// heal-over-time as a whole — the share is applied per Tick, before it is spread.
[<Fact>]
let ``Genesis raises periodic healing and periodic damage but not direct healing`` () =
  let druid = named "Druid" (treeParty [] 100)
  Assert.Equal(11500, Talents.periodicBp druid.Talents)

  let rejuvTick talents =
    let w0 = treeParty [ Shaped HumanForm ] 100
    let d0 = named "Druid" w0
    let w = SimState.mapEntity d0.Id (fun e -> { e with Talents = talents }) w0
    let d = named "Druid" w
    let t = named "Warrior" w
    let w = SimCombat.resolveAbility d.Id t.Id (d.Abilities |> List.find (fun a -> a.Name = "Rejuvenation")) w

    (named "Warrior" w).Auras
    |> List.pick (function
      | Rejuvenation(_, n, "Rejuvenation") -> Some n
      | _ -> None)

  let bare = rejuvTick Talents.none
  let genesis = rejuvTick { Talents.none with PeriodicBp = 1500 }
  Assert.InRange(genesis, bare * 115 / 100 - 1, bare * 115 / 100 + 1)

  // The damage half of the wording: Moonfire's DoT is the only damage a Restoration
  // Druid lands, and it reads the same axis.
  let dotTick talents =
    let w0 = treeParty [ Shaped HumanForm ] 100
    let d0 = named "Druid" w0
    let w = SimState.mapEntity d0.Id (fun e -> { e with Talents = talents }) w0
    let d = named "Druid" w
    let mob = w.Entities |> List.find (fun e -> e.Faction = Hostile)
    let w = SimCombat.resolveAbility d.Id mob.Id (d.Abilities |> List.find (fun a -> a.Name = "Moonfire")) w

    (w.Entities |> List.find (fun e -> e.Id = mob.Id)).Auras
    |> List.sumBy Aura.flatDamagePerTick

  let dotBare = dotTick Talents.none
  let dotGenesis = dotTick { Talents.none with PeriodicBp = 1500 }
  Assert.True(dotBare > 0, "Moonfire left no damage-over-time to price")
  Assert.InRange(dotGenesis, dotBare * 115 / 100 - 1, dotBare * 115 / 100 + 1)

/// Preservation 3/3: Regrowth's periodic half is worth 30% more while the target is
/// already wearing Rejuvenation. It cannot be a `SpellTalent` row, which is folded
/// into the ability once at construction, because it reads the *target's* aura list
/// and no target exists then — so it is resolved at application and asserted at
/// application. The limitation is the ordering: a Rejuvenation that lands after this
/// Regrowth does not retroactively raise the tail already ticking.
[<Fact>]
let ``Preservation raises Regrowth's tick only while Rejuvenation is on the target`` () =
  let tick (auras: Aura list) =
    let w = treeParty [ Shaped HumanForm ] 100
    let d = named "Druid" w
    let t = named "Warrior" w
    let w = SimState.mapEntity t.Id (fun e -> { e with Auras = auras }) w
    let w = SimCombat.resolveAbility d.Id t.Id (d.Abilities |> List.find (fun a -> a.Name = "Regrowth")) w

    (named "Warrior" w).Auras
    |> List.pick (function
      | Rejuvenation(_, n, "Regrowth") -> Some n
      | _ -> None)

  let bare = tick []
  let preserved = tick [ Rejuvenation(secTicks 12.0, 5, "Rejuvenation") ]
  Assert.InRange(preserved, bare * 130 / 100 - 1, bare * 130 / 100 + 1)

/// Swiftmend pays a fixed twelve seconds of the buff whatever is left on it, so the
/// cheapest press is the buff closest to spent: every Tick it still had is a Tick
/// that pressing now throws away. Party order used to decide, which spent the button
/// on a body with 1.5 s of Rejuvenation left while another had 0.5 s and died with it.
///
/// The hurt body is the one with the *longer* HoT, so health-first ordering would
/// pick it and remaining-first ordering picks the other: only the ordering can decide
/// this. Both sit above 60% (the spike branch would reach for Healing Touch first) and
/// below 90% (`banked` ignores a body that is essentially whole), and both heals land
/// whole, so nothing else is in the way.
[<Fact>]
let ``the druid Swiftmends the HoT with the least left`` () =
  let w0 = treeParty [ Shaped TreeForm ] 100
  let d = named "Druid" w0
  let priest = named "Priest" w0
  let mage = named "Mage" w0

  let w =
    { w0 with
        Entities =
          w0.Entities
          |> List.map (fun e ->
            if e.Id = priest.Id then
              { e with Health = e.MaxHealth * 70 / 100; Auras = [ Rejuvenation(secTicks 1.5, 5, "Rejuvenation") ] }
            elif e.Id = mage.Id then
              { e with Health = e.MaxHealth * 80 / 100; Auras = [ Rejuvenation(secTicks 0.5, 5, "Rejuvenation") ] }
            elif e.Id = d.Id then
              // Off cooldown, so the only thing being asked is which target.
              { e with Cooldowns = Map.ofList [ "Nature's Swiftness", secTicks 30.0 ] }
            else
              e) }

  let swiftmendTarget =
    PartyAi.decide w
    |> List.pick (fun c ->
      match c.Kind with
      | UseAbility(actor, "Swiftmend", target) when actor = d.Id -> Some target
      | _ -> None)

  Assert.Equal(mage.Id, swiftmendTarget)

/// Swiftmend is spent with nobody in trouble, but only at the right moment. It pays
/// a **fixed** 12 s of Rejuvenation (or 18 s of Regrowth) whatever is left on the
/// buff, so the last tick is the whole play: every Tick the HoT already paid is kept
/// and the full instant heal lands on top. Pressing at the first tick pays the same
/// and throws the remaining Ticks away, which is why the AI waits.
[<Fact>]
let ``the druid waits for the last tick before Swiftmending`` () =
  let world0 =
    Content.dungeonAt 12 1UL
    |> Content.withRosterAt 12 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]
    // In a fight, because out of one the leader now waits for the party to be whole
    // (five seconds of regeneration) and issues no orders at all.
    |> fun w ->
      { w with
          Entities =
            w.Entities
            |> List.map (fun e -> if e.Faction = Hostile then { e with Engaged = true } else e) }

  let warrior = world0.Entities |> List.find (fun e -> e.Name = "Warrior")
  let druid = world0.Entities |> List.find (fun e -> e.Name = "Druid")

  // Second argument is the heal per second (auras fire on every twentieth Tick).
  let world (healthPct: int) (left: int) (perSecond: int) (source: string) =
    { world0 with
        Entities =
          world0.Entities
          |> List.map (fun e ->
            if e.Id = warrior.Id then
              { e with
                  Health = e.MaxHealth * healthPct / 100
                  Auras = [ Rejuvenation(ticks left, perSecond, source) ] }
            elif e.Id = druid.Id then
              { e with Resource = e.MaxResource }
            else
              e) }

  let druidCasts (w: World) =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(actor, name, _) when actor = druid.Id -> Some name
      | _ -> None)

  // The tank sits at 85% of a 609-point bar: a 91-point hole. A 5-a-second
  // Rejuvenation at its last tick is worth 60 instantly, so it lands whole.
  Assert.Contains("Swiftmend", druidCasts (world 85 20 5 "Rejuvenation"))
  // A 22-a-second Rejuvenation at the same last tick is worth 264 — that would
  // overheal a 91-point hole, so the buff is left to tick instead.
  Assert.DoesNotContain("Swiftmend", druidCasts (world 85 20 22 "Rejuvenation"))
  // Fresh, with 240 Ticks to run: no press, the HoT has not paid out yet.
  Assert.DoesNotContain("Swiftmend", druidCasts (world 85 240 5 "Rejuvenation"))
  // Regrowth is the other buff it eats: 18 s of 4 a second is 72, which fits.
  Assert.Contains("Swiftmend", druidCasts (world 85 20 4 "Regrowth"))

/// Swiftmend's healing has to reach the ledger. `Attribution` reads one shape off
/// the log — `<caster>'s <spell> heals <target> for N` — and a Swiftmend written any
/// other way is healing nothing counts: 4,718 a fight at level 36, the second
/// biggest heal in the kit, invisible to `sim healers` and `sim attrib` alike. The
/// informative line stays beside the parseable one, and must not be counted twice.
[<Fact>]
let ``Swiftmend's healing is attributed to the druid`` () =
  let w = treeParty [ Shaped TreeForm ] 85
  let d = named "Druid" w
  let t = named "Warrior" w

  let w =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e ->
            if e.Id = t.Id then
              { e with Auras = [ Rejuvenation(secTicks 1.0, 5, "Rejuvenation") ] }
            else
              e) }

  let after =
    SimCombat.applyCommand { ApplyAt = ticks 1; Kind = UseAbility(d.Id, "Swiftmend", t.Id) } w

  // The parseable line, in the shape a direct heal and a HoT tick are written in.
  Assert.Contains(after.Log, fun line -> line.Contains "Druid's Swiftmend heals Warrior for")
  // The one that says what was eaten, kept because a reader wants it and no parser
  // can use it — it names no `heals`, so `Attribution` passes over it.
  Assert.Contains(after.Log, fun line -> line.Contains "consumes the Rejuvenation on Warrior")

  // Twelve seconds of a 5-a-second Rejuvenation, landing whole on a 15% hole.
  match Attribution.byAbility after |> Map.tryFind ("Druid", "Swiftmend") with
  | Some totals -> Assert.Equal(60, totals.Healing)
  | None -> failwith "the ledger cannot see the Druid's biggest instant heal"

/// The form shift follows the cast it enables. Entering Tree of Life costs 60 and
/// the form pays it back a fifth at a time on heal-over-times, so a HoT is worth the
/// shift and a heal the tree does nothing for is not. The rule used to shift home on
/// every calm heal order — including the one standing behind a Healing Touch, which
/// the form then refuses, so the round trip was bought and the cast never happened.
[<Fact>]
let ``the druid shifts into Tree of Life only for a cast the form pays for`` () =
  let w0 = treeParty [ Shaped HumanForm ] 100
  let d = named "Druid" w0
  let tank = named "Warrior" w0

  let stage (tankPct: int) (auras: Aura list) =
    { w0 with
        Entities =
          w0.Entities
          |> List.map (fun e ->
            if e.Id = tank.Id then
              { e with
                  Health = e.MaxHealth * tankPct / 100
                  Auras = auras }
            else
              e) }

  let casts (w: World) =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(actor, name, _) when actor = d.Id -> Some name
      | _ -> None)

  // A heal-over-time about to lapse is what the tree is for: a second and a half left
  // is the last tick — `worthPressing`'s window — and still short of Swiftmend's tail,
  // so the refresh is the order and the form it wants is what gets bought. The Tank is
  // near whole so Swiftmend refuses the tail as a press that would land whole, which
  // would otherwise outrank the refresh.
  let hot = stage 98 [ Rejuvenation(secTicks 1.5, 5, "Rejuvenation") ]
  Assert.Contains("Tree of Life", casts hot)

  // The shift is one action and the cast the next, so following it through is what
  // shows the 60 bought a cast that actually happens — and that the refresh pays the
  // tree's fifth.
  let shifted =
    SimCombat.applyCommand { ApplyAt = ticks 1; Kind = UseAbility(d.Id, "Tree of Life", d.Id) } hot

  Assert.Contains("Rejuvenation", casts shifted)

  // A direct heal the tree cannot supply: Healing Touch names human form, so the form
  // has nothing to add to it. Shifting into the tree first would be 60 mana and a
  // wasted Tick for a cast the form it just bought would refuse.
  let directWanted =
    casts
      (stage
        65
        [ Rejuvenation(secTicks 5.0, 5, "Rejuvenation"); Rejuvenation(secTicks 5.0, 4, "Regrowth") ])

  Assert.Contains("Healing Touch", directWanted)
  Assert.DoesNotContain("Tree of Life", directWanted)

/// The talent cooldowns are buttons, not passives: Inner Focus is spent on the first
/// heal after the Tank drops under 60% and then waits out its three minutes. The run
/// below is 45 s of Ticks against a mob that swings for nothing, so the Tank sits at
/// 30% and the Priest heals it over and over while the cooldown is still running —
/// one cast, and not a second one.
///
/// This test used the Paladin's Divine Favor until the OctoWoW port deleted that row
/// (`paladin.md` §5(c)): a passive crit talent cannot be "spent on the first heal", so
/// the button moved to the Priest rather than being asserted back into existence.
[<Fact>]
let ``a talent cooldown fires once and then waits for its cooldown`` () =
  let party = Content.partyOf [ "Warrior"; "Priest" ] 24
  let tank = { party.[0] with Pos = { X = 5; Y = 5 } }

  let priest =
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
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ wounded; priest; enemy ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // 900 Ticks is 45 s: comfortably more than the instant button needs to be
  // pressed, and well under the three-minute cooldown it starts.
  let mutable fires = 0
  let mutable was = ticks 0

  for _ in 1..900 do
    w <- SimTick.step (Bench.autoPilot w) w

    let now =
      (SimState.tryEntity priest.Id w).Value.Cooldowns
      |> Map.tryFind "Inner Focus"
      |> Option.defaultValue (ticks 0)

    // A cast resets the timer upward; a tick that only counts down does not.
    if now > was then fires <- fires + 1

    was <- now

  let remaining =
    (SimState.tryEntity priest.Id w).Value.Cooldowns
    |> Map.tryFind "Inner Focus"
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
        RestTicks = ticks 0
        Grid = Grid.create 12 10
        Entities = [ tank; healer ]
        Pending = []
        Rng = Rng.streamsOf 5UL
        PartyBuffs = []
        Quests = []
        Forecast = Forecast.empty
        TotemLedger = TotemLedger.empty
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
        RestTicks = ticks 0
        Grid = Grid.create 12 10
        Entities = [ tank; healer ]
        Pending = []
        Rng = Rng.streamsOf 5UL
        PartyBuffs = []
        Quests = []
        Forecast = Forecast.empty
        TotemLedger = TotemLedger.empty
        Log = [] }

    (SimState.tryEntity healer.Id (SimCombat.resolveAbility healer.Id tank.Id flash w)).Value.Resource

  let withIllumination = manaAfter party.[1]
  let without = manaAfter { party.[1] with Talents = Talents.none }
  let cost = 100 - without

  // OctoWoW's Illumination 5/5 refunds sixty per cent of the cost where 1.12
  // refunded all of it, so the cast is cheaper than a plain Paladin's and still
  // not free. That is a nerf the port keeps on purpose.
  Assert.Equal(100 - cost + cost * 6000 / 10000, withIllumination)
  Assert.True(without < 100, sprintf "the plain Paladin should have paid: %d" without)

/// A totem is scenery, not a body: it claims no tile, so the party walks over it,
/// and it has no clock, so it stands where it was dropped until that element is
/// dropped again. Both were asked for after a Shaman parked a corridor with a
/// Stoneskin Totem nobody could step past.
[<Fact>]
let ``a totem claims no tile and never expires`` () =
  let shaman = (Content.partyOf [ "Shaman" ] 60).[0]

  let spec =
    shaman.Abilities
    |> List.find (fun a -> a.Name = "Stoneskin Totem")
    |> fun a -> a.Effects |> List.pick (function Summon s -> Some s | _ -> None)

  Assert.True(spec.Totem.IsSome, "the Stoneskin Totem must still be a totem")
  Assert.Equal(None, spec.Lifetime)

  let tank = { (Content.partyOf [ "Warrior" ] 60).[0] with Pos = { X = 5; Y = 5 } }

  let totem =
    { tank with
        Id = EntityId 99
        Name = "Stoneskin Totem"
        Pos = { X = 6; Y = 5 }
        Abilities = []
        AutoAttack = None
        Owner = Some tank.Id
        Totem = Some { Pulse = ArmorBoon 435; Radius = 5; Period = secTicks 2.0 } }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ { tank with Destination = Some { X = 6; Y = 5 } }; totem ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  Assert.False(SimState.claimedByOther w tank.Id totem.Pos, "the totem claimed its tile")

  let w = SimTick.step [] w
  let at id = SimState.tryEntity id w |> Option.get

  Assert.Equal({ X = 6; Y = 5 }, (at tank.Id).Pos)
  Assert.Equal({ X = 6; Y = 5 }, (at totem.Id).Pos)
  Assert.True(SimState.alive (at totem.Id), "walking onto the totem killed it")

/// Grounding Totem is the one totem whose work happens in `resolveAbility` rather
/// than on a clock: a hostile spell aimed at a party member lands on the totem
/// instead, so what a caster's bolt hits is decided at resolution.
[<Fact>]
let ``a grounding totem eats the spell aimed at the party`` () =
  let tank = { (Content.partyOf [ "Warrior" ] 24).[0] with Health = 4000 }

  let totem =
    { tank with
        Id = EntityId 300
        Name = "Grounding Totem"
        Pos = { X = 6; Y = 5 }
        MaxHealth = 40
        Health = 40
        Abilities = []
        AutoAttack = None
        Auras = []
        Owner = Some tank.Id
        Totem = Some { Pulse = Redirects; Radius = 20; Period = secTicks 120.0 } }

  let caster =
    { Content.mob 100 "Scryer" CreatureType.Humanoid 21 4000 3000 None [] None { X = 9; Y = 5 } with
        Engaged = true
        Target = Some tank.Id }

  // The bolt's shape is what the hook reads, so the mage's own Fireball stands in
  // for a Scryer's — the actor below is what makes it a hostile cast.
  let bolt =
    (Content.partyOf [ "Mage" ] 60)
    |> List.head
    |> fun m -> m.Abilities |> List.find (fun a -> a.Name = "Fireball")

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; totem; caster ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let after = SimCombat.resolveAbility caster.Id tank.Id bolt w

  let hurtTank =
    after.Entities |> List.find (fun e -> e.Id = tank.Id) |> fun e -> e.Health

  let hurtTotem =
    after.Entities |> List.find (fun e -> e.Id = totem.Id) |> fun e -> e.Health

  Assert.Equal(4000, hurtTank)
  Assert.True(hurtTotem < 40, sprintf "the totem should have taken the bolt: %d" hurtTotem)

/// A cleansing totem on its own clock, the same shape as the healing stream: the
/// pulse takes auras of one school off the party in reach, and only harmful ones —
/// a totem that stripped its own party's buffs would not be a totem.
[<Fact>]
let ``a cleansing totem takes the poison off the party`` () =
  let poisoned =
    { (Content.partyOf [ "Warrior" ] 24).[0] with
        Auras = [ DeadlyPoisoned(secTicks 20.0, 12, 1) ] }

  let totem =
    { poisoned with
        Id = EntityId 200
        Name = "Cure Poison Totem"
        Pos = { X = 6; Y = 5 }
        MaxHealth = 5
        Health = 5
        Abilities = []
        AutoAttack = None
        Owner = Some poisoned.Id
        Auras = []
        Totem = Some { Pulse = Cleanses Poison; Radius = 5; Period = secTicks 2.0 } }

  let mutable w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ poisoned; totem ]
      Pending = []
      Rng = Rng.streamsOf 7UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // One pulse is two seconds of ticks, so the loop has to cross one: a shorter run
  // asserts nothing at all, which is how this test first passed as a failure.
  for _ in 1..60 do
    w <- SimTick.step [] w

  let hurt = w.Entities |> List.find (fun e -> e.Id = poisoned.Id)
  Assert.DoesNotContain(hurt.Auras, fun a -> Aura.dispelType a = Poison)

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
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; totem; enemy ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
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

/// A totem is faction Party so that mobs can hit it, but it is not a party member:
/// with the whole party dead and a totem still ticking, the run is a wipe, not a
/// fight that is still going.
[<Fact>]
let ``a live totem does not answer for a dead party`` () =
  let dead = { (Content.partyOf [ "Warrior" ] 24).[0] with Health = 0 }

  let totem =
    { dead with
        Name = "Searing Totem"
        Health = 5
        MaxHealth = 5
        Abilities = []
        AutoAttack = None
        Owner = Some dead.Id
        ExpiresInTicks = Some(secTicks 30.0)
        Totem = Some { Pulse = Heals 44; Radius = 5; Period = secTicks 2.0 } }

  let enemy =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 4000 2000 None [] None { X = 8; Y = 5 } with
        Engaged = true }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ dead; totem; enemy ]
      Pending = []
      Rng = Rng.streamsOf 7UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  Assert.Empty(SimState.aliveParty w)
  Assert.Equal(PartyWiped, SimState.outcome w)
  Assert.False(SimState.continues w)

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
  Assert.Equal<Effect list>([ SpellDamage(33, 33, Arcane) ], effectsOf "Arcane Shot")
  // Serpent Sting rank 3: 80 Nature over 15 s, which is 5 a second.
  Assert.Equal<Effect list>([ DamageOverTime(secTicks 15.0, 5) ], effectsOf "Serpent Sting")
  // Raptor Strike rank 4: the next swing lands +34.
  Assert.Equal<Effect list>([ WeaponDamage(10000, 34) ], effectsOf "Raptor Strike")

  // Aimed Shot's researched rank 1 (level 20) is the highest this ladder has:
  // 75 mana, a 3 s cast, +70 on the weapon hit.
  let aimed = hunter.Abilities |> List.find (fun a -> a.Name = "Aimed Shot")
  Assert.Equal(secTicks 3.0, aimed.CastTicks)
  Assert.True(aimed.Interruptible, "a cast is what an interrupt has to catch")
  Assert.Equal<Effect list>([ WeaponDamage(10000, 70) ], aimed.Effects)

  // Scatter Shot is the kit's interrupt, and its silence is what makes it more
  // than one: `Interrupt` alone would let the next spell go off on the next Tick.
  Assert.Equal<Effect list>([ Interrupt; Silence(secTicks 3.0) ], effectsOf "Scatter Shot")

  match effectsOf "Summon Pet" with
  | [ Summon spec ] ->
    Assert.Equal(900, spec.Health)
    Assert.Equal(Some(38, 62, secTicks 2.0), spec.Swing)
    Assert.True(spec.Lifetime.IsNone, "a pet is for the run, not for a duration")
    Assert.True(spec.Totem.IsNone, "a pet is not a totem")
  | other -> failwithf "expected a single Summon, got %A" other

  // Mend Pet rank 2 is 38 a second for five seconds — 190 total, a fifth of the
  // 900 a level-24 pet carries.
  Assert.Equal<Effect list>([ HealOverTime(secTicks 5.0, 38) ], effectsOf "Mend Pet")

  // Multi-Shot rank 1 (level 18): three targets, weapon damage each, no flat bonus.
  Assert.Equal<Effect list>([ MultiStrike 3 ], effectsOf "Multi-Shot")

  // Viper Sting drains mana: 15 a second for 8 s. Vanilla trains it at 36, so this
  // is out of band and its amounts are this file's — see the Content comment.
  Assert.Equal<Effect list>([ DrainResource(secTicks 8.0, 15) ], effectsOf "Viper Sting")

  // Wing Clip rank 1: 5 damage and a 50 % slow for 10 s -- the runner's answer.
  Assert.Equal<Effect list>([ Damage(5, 5); Cripple(secTicks 10.0, 5000) ], effectsOf "Wing Clip")

  Assert.Equal(10, List.length hunter.Abilities)

/// The Marksmanship build underneath that kit: Efficiency on six rows, Resourcefulness
/// on the one melee row, Swiftshot on Arcane Shot *and* Aimed Shot, Improved
/// Marksmanship on Aimed Shot, Improved Stings on Serpent Sting, and Ranged Weapon
/// Spec on every damaging shot.
[<Fact>]
let ``the hunter's kit is the marksmanship build`` () =
  let hunter = Content.partyOf [ "Hunter" ] 24 |> List.head
  let ability name = hunter.Abilities |> List.find (fun a -> a.Name = name)

  // Efficiency 5/5 is 10% off the shots and stings; Resourcefulness 5/5 the same off
  // Raptor Strike, which is the melee half of a talent whose traps are unmodelled.
  Assert.Equal(45, (ability "Arcane Shot").ResourceCost)
  Assert.Equal(45, (ability "Serpent Sting").ResourceCost)
  // The page's own Aimed Shot: 75 mana at rank 1, "about a third less" per §3 of the
  // note, so 50 before Efficiency takes its 10%.
  Assert.Equal(45, (ability "Aimed Shot").ResourceCost)
  Assert.Equal(90, (ability "Multi-Shot").ResourceCost)
  Assert.Equal(81, (ability "Viper Sting").ResourceCost)
  Assert.Equal(54, (ability "Scatter Shot").ResourceCost)
  Assert.Equal(40, (ability "Raptor Strike").ResourceCost)

  // Swiftshot 3/3 takes 1.5 s off Arcane Shot's 6 s, and 6 s off Aimed Shot's 26 s.
  // That second leg is what the page's row buys: on the sim's old 6 s row the talent
  // was spending 3 points to hit the 1.5 s floor, which is nothing.
  Assert.Equal(secTicks 4.5, (ability "Arcane Shot").CooldownTicks)
  Assert.Equal(secTicks 20.0, (ability "Aimed Shot").CooldownTicks)

  // Ranged Weapon Spec 5/5 (+10%) reaches every damaging shot; Improved Marksmanship
  // 2/2 and Improved Stings 5/5 stack on their own rows on top of it.
  let tagOf name = Talents.damageBp name hunter.Talents
  Assert.Equal(11000, tagOf "Arcane Shot")
  Assert.Equal(12000, tagOf "Aimed Shot")
  Assert.Equal(14000, tagOf "Serpent Sting")
  Assert.Equal(11000, tagOf "Multi-Shot")
  Assert.Equal(10000, tagOf "Viper Sting")
  Assert.Equal(10000, tagOf "Scatter Shot")
  // Raptor Strike is a melee strike: the ranged talent must not reach it. The bow's
  // own white damage does take it, under the swing's fixed name.
  Assert.Equal(10000, tagOf "Raptor Strike")
  Assert.Equal(11000, tagOf SimTick.swingName)

/// Experimental Ammunition 1/1 — the last point of the operator's build this port
/// could not carry until the element share existed. 5% of Aimed Shot's damage lands
/// again as damage armour does not stop, every swing of it: `ElementShareBp` and not
/// `DamageBp`, because it is read off the hit that already landed and the armour
/// that hit paid is not charged twice.
///
/// Pinned as a *relationship* across a whole fight rather than as a number: a single
/// roll can resist the rider outright, and a hard-coded 5 would only re-test the
/// fixture's mob. Driven through the bench's own policy, because a cast-time shot
/// does not resolve inside the command that starts it.
[<Fact>]
let ``experimental ammunition burns a share of what aimed shot landed`` () =
  let hunter = Content.partyOf [ "Hunter" ] 36 |> List.exactlyOne
  Assert.Equal(500, Talents.elementShareBp "Aimed Shot" hunter.Talents)
  // The rider belongs to the one row the talent names.
  Assert.Equal(0, Talents.elementShareBp "Arcane Shot" hunter.Talents)
  Assert.Equal(0, Talents.elementShareBp SimTick.swingName hunter.Talents)

  let amountOf (line: string) = line.Split(" for ") |> Array.last |> int

  let mutable shares = 0

  for seed in [ 1UL .. 3UL ] do
    let roster = [ "Warrior"; "Priest"; "Mage"; "Warlock"; "Hunter" ]

    let mutable x =
      Content.scarletMonasteryAt "library" Content.Median seed |> Content.withRosterAt 36 roster

    for _ in 1 .. 9000 do
      if SimState.continues x then x <- SimTick.step (Bench.autoPilot x) x

    // The log pairs a shot with its burn, in order, so the burn must be 5% of the
    // shot immediately above it and nothing else. Both spellings: a shot that crits
    // says "critically hits" and is still the line the burn belongs to.
    let mutable landed = 0

    for line in x.Log do
      if line.Contains "Aimed Shot hits" || line.Contains "Aimed Shot critically hits" then
        landed <- amountOf line
      elif line.Contains "ammunition burns" then
        shares <- shares + 1
        Assert.Equal(landed * 500 / 10000, amountOf line)

  Assert.True(shares > 0, "the rider never landed in three library fights")

/// Lethal Shots 5/5 is +5% ranged crit, and `Traits.CritBonusBp` is the only place a
/// crit can come from — so it lands on the hero, as the rogue's Malice does.
[<Fact>]
let ``the hunter carries lethal shots as a trait`` () =
  let hunter = Content.partyOf [ "Hunter" ] 24 |> List.head
  // 5% base, 132 Agility at 10 bp a point, and Lethal Shots' 500.
  Assert.Equal(2320, hunter.CritChanceBp)

/// A hunter's auto-attack is its bow, so it stands at range like the casters do:
/// 21-39 bow plus Sharp Arrow plus 302 ranged attack power, 66-84 a shot.
[<Fact>]
let ``the hunter shoots from range`` () =
  let hunter = Content.partyOf [ "Hunter" ] 24 |> List.head

  Assert.Equal(8, hunter.AutoAttack.Value.Range)
  Assert.Equal((66, 84), (hunter.AutoAttack.Value.Min, hunter.AutoAttack.Value.Max))
  Assert.Equal(secTicks 1.8, hunter.AutoAttack.Value.Ticks)

/// Which lane a member fights in follows from what it can *damage* at range, not
/// from which rows it holds. A Hunter carries Raptor Strike at range 1 beside a full
/// Marksmanship kit, so the older test — "holds a Foe ability at range 1" — filed it
/// under melee: on the harness it swung a sword for the whole run, cast 0 of its 8
/// ranged rows, finished at 97% mana and dealt the party's lowest damage. The Rogue
/// is the control that keeps the fix honest: Sap also reaches 3 tiles, deals nothing,
/// and a Rogue still fights in the melee lane.
[<Fact>]
let ``a hunter shoots from its lane and a rogue stays in its own`` () =
  let world (roster: string list) (mobAt: int) =
    let members = Content.partyOf roster 60
    let tank = members |> List.head

    let mob =
      { Content.mob 100 "Fighter" CreatureType.Humanoid 60 1000 0 None [] None { X = mobAt; Y = 5 } with
          Engaged = true
          Target = Some tank.Id }

    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 14 12
      Entities = (members |> List.mapi (fun i e -> { e with Pos = { X = 3 + i; Y = 5 } })) @ [ mob ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let castsOf (w: World) (name: string) =
    let who = w.Entities |> List.find (fun e -> e.Name = name) |> fun e -> e.Id

    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(actor, ability, _) when actor = who -> Some ability
      | _ -> None)

  let hunterWorld = world [ "Warrior"; "Hunter" ] 9
  let hunterCasts = castsOf hunterWorld "Hunter"
  // Serpent Sting is a *pure* dot, so the dot rule presses it on the one tick the
  // target has none — that is the whole point of the rule, and it takes the opening
  // tick. It is a ranged row, so the lane split this test is about still holds; the
  // shot proves it on the tick after, once the sting is already ticking.
  Assert.Contains("Serpent Sting", hunterCasts)
  Assert.DoesNotContain("Raptor Strike", hunterCasts)

  let stungWorld =
    let w = world [ "Warrior"; "Hunter" ] 9

    { w with
        Entities =
          w.Entities
          |> List.map (fun e ->
            if e.Id = EntityId 100 then
              { e with Auras = e.Auras @ [ Corrupted(secTicks 15.0, 5, None) ] }
            else
              e) }

  // Which shot comes out follows the same rule as the shaman's: standing still, cast the
  // row with the best value per cast — that is the three-second Aimed Shot — and keep the
  // instant for the 26 s cooldown and for any tick the hunter is walking. The lane is what
  // this test is about, so the walking case below is where the instant is pinned.
  Assert.Contains("Aimed Shot", castsOf stungWorld "Hunter")
  Assert.DoesNotContain("Raptor Strike", castsOf stungWorld "Hunter")

  let walking =
    { stungWorld with
        Entities =
          stungWorld.Entities
          |> List.map (fun (e: Entity) -> if e.Name = "Hunter" then { e with Destination = Some { X = 9; Y = 5 } } else e) }

  // A move cancels a cast, so the instant is the only ranged row a walking hunter keeps.
  Assert.Contains("Arcane Shot", castsOf walking "Hunter")

  // Sap reaches 3 tiles and deals nothing; a Rogue adjacent to the mob still swings
  // rather than being filed under the ranged lane the way the Hunter was.
  let rogueWorld = world [ "Warrior"; "Rogue" ] 5
  let rogue = rogueWorld.Entities |> List.find (fun e -> e.Name = "Rogue")

  let meleeRows =
    rogue.Abilities |> List.filter (fun a -> a.TargetKind = Foe && a.Range <= 1) |> List.map (fun a -> a.Name)

  match castsOf rogueWorld "Rogue" with
  | [ name ] -> Assert.Contains(name, meleeRows)
  | other -> failwithf "the Rogue should issue exactly one melee order, got %A" other/// A hunter brings no party passive, so it must not silently change the buffs.
[<Fact>]
let ``a hunter adds no party passive`` () =
  Assert.Equal<PartyBuffKind list>([ BattleShout ], Content.partyBuffsFor [ "Warrior"; "Hunter" ])

/// Scatter Shot has to do two things an interrupt alone does not: stop the cast in
/// progress, and keep the next one from starting — including an instant, which the
/// engine's `canCast` gate does not cover because it is only consulted for a spell
/// with a cast time.
[<Fact>]
let ``scatter shot interrupts a cast and silences what comes next`` () =
  let hunter = Content.partyOf [ "Hunter" ] 24 |> List.head

  // A mob shock with no cast time, so "instant is blocked too" is a real
  // assertion and not a test of the cast-time gate.
  let shock =
    { Content.lightningBolt with
        Name = "Shock"
        CastTicks = ticks 0
        CooldownTicks = ticks 0 }

  let mob =
    Content.mob 100 "Caster" CreatureType.Humanoid 24 5000 1000 None [ Content.lightningBolt; shock ] None { X = 6; Y = 5 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ hunter; mob ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let hunterHealth (w: World) =
    (w.Entities |> List.find (fun e -> e.Id = hunter.Id)).Health

  let mobNow (w: World) = w.Entities |> List.find (fun e -> e.Id = EntityId 100)

  // The real path a command takes: `resolveAbility` applies effects and knows
  // nothing of the silence gate, so a test that called it directly would pass
  // whatever `applyCommand` did.
  let useAbility (actor: EntityId) (name: string) (target: EntityId) (w: World) =
    SimCombat.applyCommand { ApplyAt = ticks 0; Kind = UseAbility(actor, name, target) } w

  // Control: unsilenced, the mob starts the cast and the instant lands.
  let casting = useAbility (EntityId 100) "Lightning Bolt" hunter.Id w
  Assert.True((mobNow casting).Casting.IsSome, "the control cast should start")

  let shocked = useAbility (EntityId 100) "Shock" hunter.Id w
  Assert.True(hunterHealth shocked < hunter.Health, "the control instant should land")

  // Scatter Shot: the cast in progress is cancelled and the silence is on.
  let silenced = useAbility hunter.Id "Scatter Shot" (EntityId 100) casting
  Assert.True((mobNow silenced).Casting.IsNone, "the cast in progress should be interrupted")

  Assert.True(
    (mobNow silenced).Auras |> List.exists Aura.isSilenced,
    "the silence should be on the mob"
  )

  Assert.True(
    silenced.Log |> List.exists (fun line -> line.Contains "silences"),
    "the log should say what happened"
  )

  // Neither the cast nor the instant can start while it lasts.
  let recast = useAbility (EntityId 100) "Lightning Bolt" hunter.Id silenced
  Assert.True((mobNow recast).Casting.IsNone, "a silenced caster cannot start a cast")

  let reShock = useAbility (EntityId 100) "Shock" hunter.Id silenced
  Assert.Equal(hunterHealth silenced, hunterHealth reShock)

  // And it ages off, so the silence is a window and not a death sentence.
  let age (n: int) (aura: Aura) =
    [ 1..n ] |> List.fold (fun a _ -> a |> Option.bind Aura.advance) (Some aura)

  Assert.True((age 59 (Silenced(secTicks 3.0))).IsSome, "still silenced at 2.95 s")
  Assert.True((age 60 (Silenced(secTicks 3.0))).IsNone, "free again at 3 s")

/// The break line has to carry the kick's own name, not just the caster and the
/// cast it broke. The `sim interrupts` diagnostic counts per-spell kicks off that
/// line; without the spell, a Shaman's 18 breaks could be Earth Shock or anything
/// else, and nobody could tell a working kick from a lucky one.
[<Fact>]
let ``an interrupt log line names the ability that broke the cast`` () =
  let shaman = Content.partyOf [ "Shaman" ] 24 |> List.head

  let caster =
    { Content.mob 100 "Fang" CreatureType.Humanoid 24 1000 1000
        (Some { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 })
        [ Content.heal ]
        None
        { X = 6; Y = 5 } with
        Engaged = true
        Target = Some shaman.Id
        Casting =
          Some
            { Ability = Content.heal
              Target = EntityId 100
              Remaining = secTicks 2.0
              Pushes = 0 } }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ shaman; caster ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let kicked =
    SimCombat.applyCommand
      { ApplyAt = ticks 0; Kind = UseAbility(shaman.Id, "Earth Shock", EntityId 100) }
      w

  Assert.True(
    kicked.Log
    |> List.exists (fun line -> line.Contains "Shaman's Earth Shock interrupts Fang's Heal"),
    "the break line should name the spell that broke the cast"
  )

/// The Warrior's crossbow: in the kit, one flat point of damage, and — the part
/// that matters — the shot pulls. Two things had to be true for a ranged opener to
/// be a pull at all: an ability that reaches, and a mob that reacts to being hit.
/// Only proximity inside `aggroRadius` used to wake anything, so a Fireball or an
/// Arcane Shot landed on a mob that went on standing there.
[<Fact>]
let ``a warrior carries a crossbow that pulls for one point`` () =
  let warrior = Content.partyOf [ "Warrior" ] 24 |> List.head

  Assert.Contains(warrior.Abilities, fun a -> a.Name = "Shoot")

  let crossbow = warrior.Abilities |> List.find (fun a -> a.Name = "Shoot")
  Assert.True(crossbow.Range >= 6, "a pull has to reach further than a sword")

  let mob =
    Content.mob 100 "Grunt" CreatureType.Humanoid 24 5000 1000 None [] None
      { X = warrior.Pos.X + 4; Y = warrior.Pos.Y }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 24 24
      Entities = [ warrior; mob ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  Assert.False(mob.Engaged, "the mob starts unaware of the party")

  let after =
    SimCombat.applyCommand { ApplyAt = ticks 0; Kind = UseAbility(warrior.Id, "Shoot", mob.Id) } w

  let hit = after.Entities |> List.find (fun e -> e.Id = mob.Id)

  // One point: the crossbow announces the pull, it does not do the work.
  Assert.Equal(4999, hit.Health)
  Assert.True(hit.Engaged, "being hit is a pull, not a free shot")

/// The pet is a real body: the hunter calls it before the first pull, and the AI
/// mends it once it is hurt. Both orders come out of `PartyAi.decide`, because a
/// summon the AI never issues is a pet that never exists.
[<Fact>]
let ``the hunter calls a pet before the first pull and mends it`` () =
  let w =
    Content.dungeonAt 24 1UL
    |> Content.withRosterAt 24 [ "Warrior"; "Priest"; "Hunter"; "Mage"; "Rogue" ]

  let hunter = w.Entities |> List.find (fun e -> e.Name = "Hunter")

  let ordersOn (w: World) =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(a, n, t) -> Some(a, n, t)
      | _ -> None)

  // Fifty ticks is 2.5 s: enough for the summon order to be issued and resolved.
  let rec run (n: int) (w: World) =
    if n = 0 then w else run (n - 1) (SimTick.step (Bench.autoPilot w) w)

  let w = run 50 w

  let pet =
    w.Entities
    |> List.filter (fun e -> e.Owner = Some hunter.Id)
    |> List.tryHead

  Assert.True(pet.IsSome, "the hunter should have called its pet before the first pull")
  let pet = pet.Value

  // Mend Pet trains at 20, so this test runs a level-24 party: at 12 the Hunter has
  // no mend at all and the summon assertions below would be the only ones it could
  // meet. A level-24 pet is 900 health, and Endurance Training 2/5 adds its 12% at
  // summon time — the number a pet is called with is the spec's, scaled.
  Assert.Equal(1008, pet.MaxHealth)
  Assert.Equal(Party, pet.Faction)
  Assert.True(pet.AutoAttack.IsSome, "a pet with no swing is scenery")
  Assert.Equal(Some hunter.Id, pet.Owner)

  // Hurt it, and the AI should reach for Mend Pet rather than keep shooting. The
  // damage is a fraction of the pet's pool: a flat 600 would simply kill a
  // level-12 pet, and a corpse is not something Mend Pet answers.
  let hurt = SimCombat.dealDamage pet.Id (pet.MaxHealth / 2) w

  Assert.Contains(
    ordersOn hurt,
    fun (a, n, t) -> a = hunter.Id && n = "Mend Pet" && t = pet.Id
  )

  // And Mend Pet is a heal-over-time that actually lands on the pet.
  let mended = SimCombat.resolveAbility hunter.Id pet.Id (hunter.Abilities |> List.find (fun a -> a.Name = "Mend Pet")) hurt

  Assert.True(
    (mended.Entities |> List.find (fun e -> e.Id = pet.Id)).Auras
    |> List.exists (fun a -> Aura.flatHealPerTick a > 0),
    "Mend Pet should leave a heal-over-time on the pet"
  )

  // A dead pet is not replaced while its body is still on the grid.
  let dead = SimCombat.dealDamage pet.Id pet.MaxHealth hurt

  Assert.DoesNotContain(ordersOn dead, fun (_, n, _) -> n = "Summon Pet")

  Assert.Equal(
    1,
    dead.Entities |> List.filter (fun e -> e.Owner = Some hunter.Id) |> List.length
  )

/// The Warlock's demon — the second `SummonSpec` instance, and the reason the spec
/// grew a `CreatureType`. The Hunter's pet is a Beast and `CreatureType.polymorphable`
/// says so; an Imp is a Demon, so the Mage sharing its roster corner cannot sheep it.
/// Both bodies are built by the same `Summon` branch, so this pins the field that
/// tells them apart rather than the machinery underneath.
[<Fact>]
let ``the warlock calls an imp, and it is a demon rather than a beast`` () =
  let w =
    Content.dungeonAt 24 1UL
    |> Content.withRosterAt 24 [ "Warrior"; "Priest"; "Warlock"; "Mage"; "Rogue" ]

  let warlock = w.Entities |> List.find (fun e -> e.Name = "Warlock")

  let orders (w: World) =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(a, n, t) -> Some(a, n, t)
      | _ -> None)

  // The order is issued before there is a body: a demon called after the opener is
  // called too late.
  Assert.True(
    orders w |> List.exists (fun (a, n, t) -> a = warlock.Id && n = "Summon Demon" && t = warlock.Id),
    "the Warlock asks for its Imp before the pull"
  )

  let summon =
    warlock.Abilities
    |> List.find (fun a -> a.Name = "Summon Demon")
    |> fun a -> SimCombat.resolveAbility warlock.Id warlock.Id a w

  let imp = summon.Entities |> List.find (fun e -> e.Owner = Some warlock.Id)

  Assert.Equal("Imp", imp.Name)
  Assert.Equal(CreatureType.Demon, imp.CreatureType)
  Assert.False(CreatureType.polymorphable imp.CreatureType, "a Demon is not Polymorph bait")
  Assert.Equal(420, imp.MaxHealth)
  Assert.Equal(340, imp.Armor)
  // It fights. A body that stands there is not a summon.
  Assert.True(imp.AutoAttack.IsSome, "the Imp has a swing of its own")

  // And it is one body: a second order while the Imp lives is not issued.
  Assert.False(
    orders summon |> List.exists (fun (a, n, _) -> a = warlock.Id && n = "Summon Demon"),
    "one demon per run"
  )

/// The operator's second build spends 15 of its 51 points in Beast Mastery, which
/// is the tree this port used to ignore entirely. Four of the five talents scale
/// the pet the hunter already calls — Endurance Training (health, both bodies),
/// Thick Hide (armour), Unleashed Fury (damage) and Coordinated Assault (a share of
/// the hunter's attack power) — so this pins all four against one summon.
[<Fact>]
let ``the second hunter build's beast mastery scales the pet it calls`` () =
  let w =
    Content.dungeonAt 24 1UL
    |> Content.withRosterAt 24 [ "Warrior"; "Priest"; "Hunter"; "Mage"; "Rogue" ]

  let hunter = w.Entities |> List.find (fun e -> e.Name = "Hunter")

  let summon =
    hunter.Abilities
    |> List.find (fun a -> a.Name = "Summon Pet")
    |> fun a -> SimCombat.resolveAbility hunter.Id hunter.Id a w

  let pet = summon.Entities |> List.find (fun e -> e.Owner = Some hunter.Id)

  // Endurance Training 2/5: 900 -> 1008, and the hunter's own pool gains the same
  // 12% — the talent's line is "you and your pet", so both numbers move. A
  // party-buffed level-24 hunter is 1013 health, and 1134 is that pool with its
  // share added.
  Assert.Equal(1008, pet.MaxHealth)
  Assert.Equal(1134, hunter.MaxHealth)

  // Thick Hide 3/3: a level-24 pet's 700 armour becomes 952.
  Assert.Equal(952, pet.Armor)

  // Unleashed Fury 4/5: the 38-62 swing becomes 44-71.
  let swing = pet.AutoAttack.Value
  Assert.Equal((44, 71), (swing.Min, swing.Max))

  // Coordinated Assault 1/1 is a fifth of the hunter's *ranged* attack power, on an
  // extra strike no more often than every 3 s. The attack power is the era formula
  // re-derived at `Content.fs` (`level x 2 + agility x 2 - 20`), which is the same
  // 302 the researched level-24 shot range carries as its 38.8 damage — here it is
  // 292 because a buffed roster hunter stands at 132 Agility rather than 137.
  Assert.Equal(24 * 2 + hunter.Stats.Agility * 2 - 20, hunter.RangedAttackPower)

  // The extra strike has no timer of its own to live on, so it is smoothed into
  // every swing at the cadence ratio `swing / 3 s`: a fifth of 302 is 60, and a
  // 2 s pet swing carries 40/60 of it.
  Assert.Equal(
    292 * 2000 / 10000 * int swing.Ticks / int (secTicks 3.0),
    pet.MeleeDamageBonus
  )
  Assert.True(pet.MeleeDamageBonus > 0, "a hunter's own Agility is what the pet's bonus is a share of")

/// Multi-Shot's whole point is the second and third body: one aim, three rolls.[<Fact>]
let ``multi-shot hits the target and the two nearest bodies, and no more`` () =
  let hunter =
    match Content.partyOf [ "Hunter" ] 24 with
    | h :: _ -> { h with Pos = { X = 1; Y = 5 } }
    | [] -> failwith "no hunter"

  let rat (i: int) (x: int) =
    Content.mob
      (200 + i)
      "Rat"
      CreatureType.Beast
      20
      500
      0
      (Some { Min = 5; Max = 5; Ticks = secTicks 2.0; Range = 1 })
      []
      None
      { X = x; Y = 5 }

  // Two beside the target, one far enough away to be missed.
  let aimed, nearA, nearB, far = EntityId 200, EntityId 201, EntityId 203, EntityId 202

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 12
      Entities = [ hunter; rat 0 5; rat 1 5; rat 2 14; rat 3 6 ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // Marshal the layout: 202 is the far one, so put it there.
  let w =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e ->
            if e.Id = EntityId 202 then { e with Pos = { X = 14; Y = 5 } } else e) }

  let healthOf (id: EntityId) (w: World) =
    (w.Entities |> List.find (fun e -> e.Id = id)).Health

  let after =
    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(hunter.Id, "Multi-Shot", aimed) }
      w

  Assert.True(healthOf aimed after < 500, "the target should be hit")
  Assert.True(healthOf nearA after < 500, "the first body beside it should be hit")
  Assert.True(healthOf nearB after < 500, "the second body beside it should be hit")
  Assert.Equal(500, healthOf far after)

  // Four bodies is all this test has, so assert the cap directly: a kit that
  // fired at everything would have hit `far` too.
  Assert.Equal<Effect list>([ MultiStrike 3 ], hunter.Abilities |> List.find (fun a -> a.Name = "Multi-Shot") |> fun a -> a.Effects)

/// Swipe is the bear's one area row and nobody else's: the feral table's "3
/// nearby enemies" at rank 2 (level 24) for twenty Rage, on the bear's own weapon
/// roll — the damage column is the swing, so the flat step is zero.
[<Fact>]
let ``swipe is the bear's level-24 sweep and no other druid carries it`` () =
  let kitOf name = Content.partyOf [ name ] 24 |> List.head |> fun e -> e.Abilities
  let swipeOf name = kitOf name |> List.tryFind (fun a -> a.Name = "Swipe")

  match swipeOf "Druid (Bear)" with
  | Some swipe ->
    Assert.Equal<Effect list>([ MultiStrike 3 ], swipe.Effects)
    Assert.Equal(20, swipe.ResourceCost)
    // Melee: the aim is the body in front, and the room around it is the resolver's.
    Assert.Equal(1, swipe.Range)
    Assert.Equal(ticks 0, swipe.CastTicks)
  | None -> failwith "a bear carries Swipe at 24"

  for name in [ "Druid"; "Druid (Cat)"; "Druid (Moonkin)" ] do
    Assert.True((swipeOf name).IsNone, $"{name} should not carry a bear's sweep")

  // The row outlives the level it is trained at: the level-60 bear still carries
  // the only rung there is.
  Assert.True(
    (Content.partyOf [ "Druid (Bear)" ] 60 |> List.head).Abilities
    |> List.exists (fun a -> a.Name = "Swipe"),
    "the sweep is the top rung, so it is still the row at 60"
  )

/// The sweep's whole point is the second and third body, and it pays for the cast
/// out of Rage and for each body out of its health — threat included, because the
/// swing that lands the damage is the Warrior's `weaponStrike` under a feral name.
[<Fact>]
let ``a bear's swipe hits three bodies, bills twenty rage, and marks every one`` () =
  let bear =
    match Content.partyOf [ "Druid (Bear)" ] 24 with
    | [ b ] -> { b with Pos = { X = 1; Y = 5 } }
    | other -> failwithf "expected one bear, got %d" (List.length other)

  let rat (i: int) (x: int) =
    Content.mob
      (200 + i)
      "Rat"
      CreatureType.Beast
      20
      500
      0
      (Some { Min = 5; Max = 5; Ticks = secTicks 2.0; Range = 1 })
      []
      None
      { X = x; Y = 5 }

  let aimed, nearA, nearB, far = EntityId 200, EntityId 201, EntityId 203, EntityId 202

  // Under the operator's cleave rule a player sweep is every adjacent body, not
  // the nearest few to the aim, so nearA stands directly behind the bear -- the one
  // tile the old nearest-to-aim net never reached -- and the far rat at (14,5) is
  // the body the new net must not grow back toward. nearB shares the aimed tile:
  // two bodies on one tile is still one sweep.
  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 12
      Entities = [ bear; rat 0 2; rat 1 0; rat 2 14; rat 3 2 ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let healthOf (id: EntityId) (w: World) =
    (w.Entities |> List.find (fun e -> e.Id = id)).Health

  let swipeWith (rage: int) =
    let w =
      { w with
          Entities =
            w.Entities
            |> List.map (fun e -> if e.Id = bear.Id then { e with Resource = rage } else e) }

    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(bear.Id, "Swipe", aimed) }
      w

  let after = swipeWith 100

  Assert.True(healthOf aimed after < 500, "the target should be hit")
  Assert.True(healthOf nearA after < 500, "the first body beside it should be hit")
  Assert.True(healthOf nearB after < 500, "the second body beside it should be hit")
  Assert.Equal(500, healthOf far after)

  // Rage is the bill, and a bar that cannot pay it is no cast at all. Asserted as a
  // gate rather than as a subtraction: the swing *earns* rage on every body it
  // lands on, so the delta after a three-body sweep is a subtraction this test
  // would have to model rage generation to read.
  let tooPoor = swipeWith 15
  for id in [ aimed; nearA; nearB ] do
    Assert.Equal(500, healthOf id tooPoor)

  // Every body the sweep landed on is a body the bear now holds, at the stance's
  // own rate: the threat is the swing's, not a separate roll.
  for id in [ aimed; nearA; nearB ] do
    Assert.True(SimState.threatOf bear.Id (after.Entities |> List.find (fun e -> e.Id = id)) > 0, $"{id} should carry the bear's threat")

/// The bear's rotation presses Maul and never the sweep, and that is a measured
/// decision rather than an unfinished one: adding a `MultiStrike` press — on the
/// body count, and again on the body count with nothing held by the party's own
/// control — left 2 of 12 level-60 seeds uncleared, because the sweep lands on
/// the Tick the Mage's polymorph is cast and undoes it. The kit still carries the
/// row, so a scenario can press it (the test above does); the AI does not.
[<Fact>]
let ``the bear's rotation presses Maul and never the sweep`` () =
  let bear =
    match Content.partyOf [ "Druid (Bear)" ] 24 with
    | [ b ] -> b
    | other -> failwithf "expected one bear, got %d" (List.length other)

  // All three choose the bear, so no `thief` sends the tank to Growl instead.
  let rat (i: int) (x: int) =
    let mob =
      Content.mob
        (200 + i)
        "Rat"
        CreatureType.Beast
        20
        500
        0
        (Some { Min = 5; Max = 5; Ticks = secTicks 2.0; Range = 1 })
        []
        None
        { X = x; Y = 6 }

    { mob with Engaged = true; Target = Some bear.Id }

  // Rage to spare, so nothing but the policy can be the reason: a bear that could
  // not afford Maul would fall through to the weapon swing either way.
  let world (mobs: Entity list) =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 12
      Entities = { bear with Pos = { X = 1; Y = 5 }; Resource = 100; Target = Some (EntityId 200) } :: mobs
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let presses (w: World) =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(actor, name, _) when actor = bear.Id -> Some name
      | _ -> None)

  for mobs in [ [ rat 0 2 ]; [ rat 0 2; rat 1 3 ]; [ rat 0 2; rat 1 3; rat 2 2 ] ] do
    let w = world mobs
    Assert.Contains("Maul", presses w)
    Assert.DoesNotContain("Swipe", presses w)

/// Bash is the bear's kick and its stun at once, and both halves have to be on
/// the row: the `Interrupt` is what breaks the cast and the `Stun` is what a Bash
/// is, so the engine cancels the cast on either (`SimCombat.fs`). The wings run
/// level 33-39, which is rank 2 (level 32): three seconds.
[<Fact>]
let ``bash breaks the cast and stuns the caster for the rank's duration`` () =
  let bear =
    match Content.partyOf [ "Druid (Bear)" ] 36 with
    | [ b ] -> { b with Pos = { X = 1; Y = 5 }; Resource = 100 }
    | other -> failwithf "expected one bear, got %d" (List.length other)

  // Rank 2 is the wings' rung: 3 s, and the rank below is a different number.
  let bash =
    bear.Abilities
    |> List.find (fun a -> a.Name = "Bash")

  Assert.Equal<Effect list>([ Interrupt; Stun(secTicks 3.0) ], bash.Effects)
  Assert.Equal(secTicks 60.0, bash.CooldownTicks)
  Assert.Equal(10, bash.ResourceCost)

  let caster =
    { Content.mob
        100
        "Fang"
        CreatureType.Humanoid
        21
        1000
        1000
        (Some { Min = 5; Max = 5; Ticks = secTicks 2.7; Range = 1 })
        []
        None
        { X = 2; Y = 5 } with
        Engaged = true
        Target = Some bear.Id
        Casting =
          Some
            { Ability = Content.heal
              Target = EntityId 100
              Remaining = secTicks 2.0
              Pushes = 0 } }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 12
      Entities = [ bear; caster ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let after =
    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(bear.Id, "Bash", EntityId 100) }
      w

  let fang = after.Entities |> List.find (fun e -> e.Id = EntityId 100)
  Assert.True(fang.Casting.IsNone, "the cast should be broken")
  Assert.Contains(Stunned(secTicks 3.0), fang.Auras)

/// Feral Charge is the half of the talent the sim can express: an interrupt and
/// nothing else — no school lock, no immobilize, no damage. It is the kick the
/// bear can land on a caster that is not yet in its reach.
[<Fact>]
let ``feral charge interrupts a caster that is out of melee`` () =
  let bear =
    match Content.partyOf [ "Druid (Bear)" ] 36 with
    | [ b ] -> { b with Pos = { X = 1; Y = 5 }; Resource = 100 }
    | other -> failwithf "expected one bear, got %d" (List.length other)

  let charge =
    bear.Abilities
    |> List.find (fun a -> a.Name = "Feral Charge")

  Assert.Equal<Effect list>([ Interrupt ], charge.Effects)
  Assert.Equal(secTicks 15.0, charge.CooldownTicks)
  Assert.Equal(5, charge.ResourceCost)
  Assert.Equal(25, charge.Range)

  let caster =
    { Content.mob
        100
        "Fang"
        CreatureType.Humanoid
        21
        1000
        1000
        (Some { Min = 5; Max = 5; Ticks = secTicks 2.7; Range = 1 })
        []
        None
        { X = 6; Y = 5 } with
        Engaged = true
        Target = Some bear.Id
        Casting =
          Some
            { Ability = Content.heal
              Target = EntityId 100
              Remaining = secTicks 2.0
              Pushes = 0 } }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 12
      Entities = [ bear; caster ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let after =
    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(bear.Id, "Feral Charge", EntityId 100) }
      w

  let fang = after.Entities |> List.find (fun e -> e.Id = EntityId 100)
  Assert.True(fang.Casting.IsNone, "the cast should be broken")
  // The interrupt half only: no school lock or immobilize exists to land here.
  Assert.DoesNotContain(Stunned(secTicks 3.0), fang.Auras)

/// The bear answers an assigned cast with its own kit. Feral Charge is the
/// cheaper kick (5 Rage, 15 s) so it takes the cast when both are ready; Bash
/// (10 Rage, 60 s) covers the one that follows while the charge is down. And a
/// world with nothing casting never spends either — the rotation's own order
/// answers instead.
[<Fact>]
let ``the bear tank kicks an assigned cast and keeps the rotation off its interrupts`` () =
  let bear =
    match Content.partyOf [ "Druid (Bear)" ] 36 with
    | [ b ] -> b
    | other -> failwithf "expected one bear, got %d" (List.length other)

  // The tank's melee target: a rat in front of the bear, so the pull does not
  // ask for a walk before it asks for a kick.
  let rat =
    { Content.mob
        200
        "Rat"
        CreatureType.Beast
        20
        500
        0
        (Some { Min = 5; Max = 5; Ticks = secTicks 2.0; Range = 1 })
        []
        None
        { X = 2; Y = 5 } with
        Engaged = true
        Target = Some bear.Id }

  // A hostile heal mid-cast, three tiles out: worth breaking, and inside Bash's
  // reach, so the rank-2 kick can cover a cast the charge cannot.
  let caster =
    { Content.mob
        100
        "Fang"
        CreatureType.Humanoid
        21
        1000
        1000
        (Some { Min = 5; Max = 5; Ticks = secTicks 2.7; Range = 1 })
        []
        None
        { X = 4; Y = 5 } with
        Engaged = true
        Target = Some bear.Id
        Casting =
          Some
            { Ability = Content.heal
              Target = EntityId 100
              Remaining = secTicks 2.0
              Pushes = 0 } }

  let world (casterReady: bool) (casting: bool) =
    let fang =
      if casting then caster
      else { caster with Casting = None }

    let bearIn =
      { bear with
          Pos = { X = 1; Y = 5 }
          Resource = 100
          Target = Some (EntityId 200)
          Cooldowns =
            (if casterReady then
               Map.empty
             else
               Map.ofList [ "Feral Charge", secTicks 10.0 ]) }

    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 12
      Entities = [ bearIn; rat; fang ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let ordersOf (w: World) =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(actor, name, _) when actor = bear.Id -> Some name
      | _ -> None)

  // Both kicks ready: the cheaper one takes the cast.
  Assert.Contains("Feral Charge", ordersOf (world true true))
  // The charge spent: Bash covers the next cast.
  Assert.Contains("Bash", ordersOf (world false true))
  // Nothing casting: neither is a damage button. The rotation answers with
  // its own order — here the armour tag, which outranks the dump on a target
  // that has lost none — and Bash is ten Rage that order would otherwise spend.
  let idle = ordersOf (world true false)
  Assert.DoesNotContain("Bash", idle)
  Assert.DoesNotContain("Feral Charge", idle)
  Assert.Contains("Faerie Fire (Feral)", idle)

/// Viper Sting is paid out of the target's pool, once a second, and a pool that
/// cannot pay for a cast is a caster the party stops hearing from.
[<Fact>]
let ``viper sting drains mana once a second and an empty pool refuses a cast`` () =
  let hunter =
    match Content.partyOf [ "Hunter" ] 24 with
    | h :: _ -> { h with Pos = { X = 1; Y = 5 } }
    | [] -> failwith "no hunter"

  let caster =
    Content.mob
      300
      "Caster"
      CreatureType.Humanoid
      20
      1000
      400
      None
      [ Content.lightningBolt ]
      None
      { X = 6; Y = 5 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 14 10
      Entities = [ hunter; caster ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let manaOf (w: World) =
    (w.Entities |> List.find (fun e -> e.Id = EntityId 300)).Resource

  let stung =
    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(hunter.Id, "Viper Sting", EntityId 300) }
      w

  Assert.Equal(400, manaOf stung)

  Assert.True(
    (stung.Entities |> List.find (fun e -> e.Id = EntityId 300)).Auras
    |> List.exists (function Drained _ -> true | _ -> false),
    "the sting should leave a drain on the target"
  )

  // It fires on the Ticks that land on a second, so one tick of resource
  // accounting costs 15 and the next one nothing.
  let drained = SimTick.advanceResource stung
  Assert.Equal(385, manaOf drained)

  let nextTick =
    SimTick.advanceResource
      { drained with
          Entities =
            drained.Entities
            |> List.map (fun e ->
              { e with Auras = e.Auras |> List.choose Aura.advance }) }

  Assert.Equal(385, manaOf nextTick)

  // And a pool that cannot pay the bill is a cast that never starts. Lightning
  // Bolt is 60 mana, so 50 left in the pool is a caster with nothing to spend.
  let broke =
    stung
    |> fun w ->
      { w with
          Entities =
            w.Entities
            |> List.map (fun e ->
              if e.Id = EntityId 300 then { e with Resource = 50 } else e) }

  let refused =
    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(EntityId 300, "Lightning Bolt", hunter.Id) }
      broke

  Assert.True(
    refused.Log |> List.exists (fun line -> line.Contains "not enough resource"),
    "a drained caster should be refused"
  )

  Assert.True(
    (refused.Entities |> List.find (fun e -> e.Id = EntityId 300)).Casting.IsNone,
    "and the cast must not start"
  )

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
    RestTicks = ticks 0
    Grid = Grid.create 12 10
    Entities = [ tank; priest; near; far ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }

let private abilitiesFor (actor: EntityId) (cmds: Command list) =
  cmds
  |> List.choose (fun c ->
    match c.Kind with
    | UseAbility(a, n, _) when a = actor -> Some n
    | _ -> None)

/// A wiped party: the Warrior and the Shaman are both on the floor and one mob is
/// still standing. This is the state Reincarnation exists for — the run is over
/// unless the Shaman's own corpse gets a tick to cast in.
let private reincarnationWorld (cooldowns: Map<string, int<tick>>) : World =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let warrior =
    { Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 5; Y = 5 } with
        Health = 0 }

  let shaman =
    { Content.hero
        2
        "Shaman"
        Healer
        1000
        1666
        tap
        10000
        [ Content.shamanHealingWave; Content.reincarnation ]
        { X = 5; Y = 6 } with
        Health = 0
        Resource = 0
        Cooldowns = cooldowns }

  let near =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some(EntityId 1) }

  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 12 10
    Entities = [ warrior; shaman; near ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }

[<Fact>]
let ``reincarnation stands a dead shaman back up`` () =
  let w = reincarnationWorld Map.empty
  // The party reads as wiped, and the run must not be resolved until the corpse
  // that can still raise itself has had its tick.
  Assert.Equal(PartyWiped, SimState.outcome w)
  Assert.True(SimState.continues w)

  let after = SimTick.step (PartyAi.decide w) w
  let shaman = after.Entities |> List.find (fun e -> e.Name = "Shaman")

  // Rounding-sensitive: the resurrect grants a share of the pool by integer
  // division, and the same tick may or may not pay a point of regeneration.
  Assert.InRange(shaman.Health * 100 / shaman.MaxHealth, 39, 41)
  Assert.InRange(shaman.Resource * 100 / shaman.MaxResource, 39, 41)
  Assert.Equal(Running, SimState.outcome after)

/// The cooldown is the whole balance of the button: a Shaman who already used it
/// this run stays a corpse, and that run is a wipe.
[<Fact>]
let ``reincarnation on cooldown leaves the shaman dead`` () =
  let w = reincarnationWorld (Map.ofList [ "Reincarnation", secTicks 1500.0 ])
  let after = SimTick.step (PartyAi.decide w) w
  let shaman = after.Entities |> List.find (fun e -> e.Name = "Shaman")

  Assert.Equal(0, shaman.Health)
  Assert.Equal(PartyWiped, SimState.outcome after)
  Assert.False(SimState.continues after)

/// A wipe nobody can undo has to end. The Shaman's *other* resurrection is
/// Corpse-target, and a corpse cannot cast one — nor afford it, at 0 mana — so
/// counting it as pending would keep the run alive forever on nothing.
[<Fact>]
let ``a wipe with no affordable self resurrection ends the run`` () =
  let w = reincarnationWorld (Map.ofList [ "Reincarnation", secTicks 1500.0 ])

  let w =
    SimState.mapEntity
      (EntityId 2)
      (fun e -> { e with Abilities = e.Abilities @ [ Content.ancestralSpirit ] })
      w

  Assert.True(SimState.outcome w = PartyWiped, "the fixture is a wipe")
  Assert.False(SimState.resurrectionPending w)
  Assert.False(SimState.continues w)

  let after = SimTick.step (PartyAi.decide w) w
  let shaman = after.Entities |> List.find (fun e -> e.Name = "Shaman")
  Assert.Equal(0, shaman.Health)

/// A living healer, a dead ally, and a fight still in progress — the one state
/// Rebirth is for. A wipe would have no living Druid in it to cast.
let private rebirthWorld () : World =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let warrior =
    Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 5; Y = 5 }

  let druid =
    Content.hero
      2
      "Druid"
      Healer
      1000
      1744
      tap
      10000
      [ Content.healingTouchR4; Content.rebirth ]
      { X = 5; Y = 6 }

  let shaman =
    { Content.hero 3 "Shaman" Healer 1000 1666 tap 10000 [ Content.shamanHealingWave ] { X = 6; Y = 6 } with
        Health = 0 }

  let near =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some(EntityId 1) }

  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 12 10
    Entities = [ { warrior with Target = Some(EntityId 100) }; druid; shaman; near ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }

[<Fact>]
let ``rebirth raises a dead ally and the healers can target them again`` () =
  let druid, shaman = EntityId 2, EntityId 3
  let w = rebirthWorld ()

  // The target is a corpse, which no other order in the file will pick.
  let cast =
    PartyAi.decide w
    |> List.exists (fun c ->
      match c.Kind with
      | UseAbility(a, "Rebirth", t) -> a = druid && t = shaman
      | _ -> false)

  Assert.True(cast, "the living druid should cast Rebirth on the corpse")

  // Stop at the tick the revive lands on: that is the fraction the spell put
  // back, before any heal has had time to touch it.
  let mutable w = w
  let mutable raised = 0

  for _ in 1 .. 60 do
    w <- SimTick.step (PartyAi.decide w) w
    let s = w.Entities |> List.find (fun e -> e.Id = shaman)
    if raised = 0 && SimState.alive s then raised <- s.Health

  let revived = w.Entities |> List.find (fun e -> e.Id = shaman)
  Assert.True(SimState.alive revived, "the shaman should be alive again")
  Assert.True(raised * 100 / revived.MaxHealth >= 45)

  // And back in `party`, which is what the healer orders search: the healer's next
  // casts land on the member who just stood up, so the health goes up again.
  let healed =
    (w, [ 1 .. 60 ]) ||> List.fold (fun w _ -> SimTick.step (PartyAi.decide w) w)
    |> fun w -> w.Entities |> List.find (fun e -> e.Id = shaman)

  Assert.True(healed.Health > raised, "the healers should be healing the revived member")

/// A wiped party with a Shaman's Reincarnation still owed and one mob standing over
/// the bodies: the state the wipe reset and the death spot exist for. The grid is all
/// floor, so only distance decides what the room can see once it goes home.
let private wipedRoom (shamanPos: Pos) : World =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let warrior =
    { Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 4; Y = 5 } with
        Health = 0 }

  let shaman =
    { Content.hero
        2
        "Shaman"
        Healer
        1000
        1666
        tap
        10000
        [ Content.shamanHealingWave; Content.reincarnation; Content.ancestralSpirit ]
        shamanPos with
        Health = 0 }

  // Stood at (5, 8) when nothing had stirred it: `Content.mob` anchors `Home` there.
  let near =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 5; Y = 8 } with
        Engaged = true
        Target = Some(EntityId 1) }

  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 12 10
    Entities = [ warrior; shaman; near ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }

/// The pull is lost, so the room leaves: home, whole, and no longer fighting. Without
/// this the mob stands over the bodies and kills the Shaman again the moment it stands.
[<Fact>]
let ``reset sends hostiles home and clears the fight`` () =
  let w = wipedRoom { X = 5; Y = 5 }

  // Displaced, wounded and engaged: a mob that had chased the party down the room.
  let displaced =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e ->
            if e.Faction = Hostile then
              { e with Pos = { X = 1; Y = 1 }; Health = 100 }
            else
              e) }

  let after = SimTick.step [] displaced
  let mob = after.Entities |> List.find (fun e -> e.Faction = Hostile)

  Assert.Equal({ X = 5; Y = 8 }, mob.Pos)
  Assert.Equal(mob.MaxHealth, mob.Health)
  Assert.False(mob.Engaged)
  Assert.False(SimState.inCombat after)

/// Corpses stay where they fell — they are the run's stake, and the spot the Shaman
/// died in is the one the reset must not tidy up.
[<Fact>]
let ``reset leaves the corpses where they fell`` () =
  let after = SimTick.step [] (wipedRoom { X = 5; Y = 5 })
  let warrior = after.Entities |> List.find (fun e -> e.Id = EntityId 1)

  Assert.Equal(0, warrior.Health)
  Assert.Equal({ X = 4; Y = 5 }, warrior.Pos)
  Assert.Equal(PartyWiped, SimState.outcome after)

/// The margin the death spot buys: one tile past `aggroRadius`, the room that just
/// lost the pull cannot see the body standing up in it.
[<Fact>]
let ``a shaman three tiles from home is not noticed when it reincarnates`` () =
  // The wipe tick has already sent the room home; this is the tick the corpse stands up in.
  let w = SimState.encounterReset (wipedRoom { X = 5; Y = 5 })
  let after = SimTick.step (PartyAi.decide w) w
  let shaman = after.Entities |> List.find (fun e -> e.Name = "Shaman")
  let mob = after.Entities |> List.find (fun e -> e.Faction = Hostile)

  Assert.True(SimState.alive shaman, "the shaman should be back on its feet")
  Assert.False(mob.Engaged)

/// Two tiles is inside it, and this is what the Shaman dies to avoid: a body the room
/// still sees is a wipe with no way out of it.
[<Fact>]
let ``a shaman two tiles from home is pulled again`` () =
  let w = SimState.encounterReset (wipedRoom { X = 5; Y = 6 })
  let after = SimTick.step (PartyAi.decide w) w
  let mob = after.Entities |> List.find (fun e -> e.Faction = Hostile)

  Assert.True(mob.Engaged)

let private lastShamanWorld (shamanPos: Pos) : World =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let warrior =
    { Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 4; Y = 5 } with
        Health = 0 }

  let shaman =
    Content.hero
      2
      "Shaman"
      Healer
      1000
      1666
      tap
      10000
      [ Content.shamanHealingWave; Content.reincarnation; Content.ancestralSpirit ]
      shamanPos

  let near =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 5; Y = 8 } with
        Engaged = true
        Target = Some(EntityId 2) }

  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 12 10
    Entities = [ warrior; shaman; near ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }

let private shamanOrders (w: World) =
  PartyAi.decide w
  |> List.filter (fun c ->
    match c.Kind with
    | UseAbility(a, _, _) -> a = EntityId 2
    | MoveTo(a, _) -> a = EntityId 2
    | Stop a -> a = EntityId 2)

/// The behaviour the owner asked for: with the party down and Reincarnation ready, the
/// last Shaman stops fighting and walks out of the room's reach, so it dies somewhere
/// the reset can be survived from.
[<Fact>]
let ``the last living shaman walks away instead of fighting`` () =
  let orders = shamanOrders (lastShamanWorld { X = 5; Y = 7 })

  let casts =
    orders
    |> List.filter (fun c ->
      match c.Kind with
      | UseAbility _ -> true
      | _ -> false)

  Assert.True(List.isEmpty casts, "the last shaman should not be fighting")

  let steps =
    orders
    |> List.choose (fun c ->
      match c.Kind with
      | MoveTo(_, p) -> Some p
      | _ -> None)

  Assert.Equal(1, List.length steps)

  Assert.True(
    Pos.chebyshev { X = 5; Y = 8 } steps.Head >= 3,
    sprintf "walked to %A, still inside the room's reach" steps.Head
  )

/// Already out of reach: it stops where it stands and waits, which is the whole of the
/// plan once the tile is right.
[<Fact>]
let ``a shaman already out of range holds its ground`` () =
  let orders = shamanOrders (lastShamanWorld { X = 5; Y = 3 })
  Assert.True(List.isEmpty orders, sprintf "expected no orders, got %A" orders)

/// The other half of the plan: the body left behind is within Ancestral Spirit's reach,
/// so the Shaman that stood up can raise the party and start again.
[<Fact>]
let ``ancestral spirit raises the party after the reset`` () =
  let mutable w = wipedRoom { X = 5; Y = 5 }
  let mutable raised = 0

  for _ in 1 .. 200 do
    w <- SimTick.step (PartyAi.decide w) w
    let warrior = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
    if raised = 0 && SimState.alive warrior then raised <- warrior.Health

  let warrior = w.Entities |> List.find (fun e -> e.Id = EntityId 1)
  Assert.True(raised > 0, "the shaman should raise the Warrior with Ancestral Spirit")
  Assert.InRange(raised * 100 / warrior.MaxHealth, 39, 41)

[<Fact>]
let ``ancestral spirit is in the shaman kit`` () =
  let shaman = Content.partyOf [ "Shaman" ] 24 |> List.find (fun e -> e.Name = "Shaman")

  Assert.Contains("Ancestral Spirit", shaman.Abilities |> List.map (fun a -> a.Name))

/// With the Tank down, the survivors lead themselves to the nearest mob (cca018b) —
/// and the corpse the Tank left behind must not be what the walk is measured from,
/// or the Party stands on the body while the mob eats it.
[<Fact>]
let ``the walking lead is never a corpse`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  // The Tank fell at the back of the room and the healer is still at the door.
  let tank =
    { Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 10; Y = 9 } with
        Health = 0 }

  let priest =
    Content.hero 2 "Priest" Healer 1000 1000 tap 10000 [ Content.lesserHeal; Content.heal ] { X = 2; Y = 2 }

  let near =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 9; Y = 8 } with
        Engaged = true }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; priest; near ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let leads =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | MoveTo(id, p) -> Some(id, p)
      | _ -> None)

  Assert.DoesNotContain(leads, fun (id, _) -> id = EntityId 1)

  // The living healer walks toward the mob, not back to the body.
  Assert.Contains(leads, fun (id, p) -> id = EntityId 2 && Pos.chebyshev p near.Pos <= 2)

/// The Druid's cast outlives the Druid: if the last swing of the fight kills the
/// only member still standing while a Rebirth is in flight, the rescue has already
/// been paid for and the run is not over yet.
[<Fact>]
let ``a rebirth in flight keeps a wiped run alive`` () =
  let w = rebirthWorld ()

  let fallen =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e ->
            match e.Id with
            | EntityId 1 -> { e with Health = 0 }
            | EntityId 2 ->
              { e with
                  Health = 0
                  Casting =
                    Some
                      { Ability = Content.rebirth
                        Target = EntityId 3
                        Remaining = ticks 10
                        Pushes = 0 } }
            | _ -> e) }

  Assert.Equal(PartyWiped, SimState.outcome fallen)
  Assert.True(SimState.continues fallen)


/// Earth Shock carries an interrupt, so the script keeps it out of the damage rotation
/// and spends it on a cast instead. What it presses in that rotation's place depends on
/// whether the shaman can stand still: `perCast` divides by cast time, so an instant
/// scores its damage times a thousand and heads every ranking it appears in — measured,
/// Flame Shock (160 mana, 126 damage, 0.79 a point) outspent Lightning Bolt (135, 190,
/// 1.41) by 493 presses and 78 512 mana, 18.5% of all spend over 30 seeds. An instant's
/// one merit is that it can be pressed while walking, which is the second case here.
[<Fact>]
let ``the shaman casts when it can stand still and shocks when it cannot`` () =
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

  // Standing still, the three-second bolt is 78% more damage per point of mana and
  // costs 25 less, so it is the spell to cast.
  Assert.Contains("Lightning Bolt", abilitiesFor (EntityId 2) (PartyAi.decide w))

  // A move cancels a cast in progress, so only the instant keeps its damage while the
  // shaman walks — which is the whole reason Flame Shock is still in the rotation.
  let walking =
    { w with
        Entities =
          w.Entities
          |> List.map (fun (e: Entity) -> if e.Id = EntityId 2 then { e with Destination = Some { X = 8; Y = 7 } } else e) }

  Assert.Contains("Flame Shock", abilitiesFor (EntityId 2) (PartyAi.decide walking))

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
              Remaining = secTicks 2.0
              Pushes = 0 } }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; shaman; caster ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  Assert.Contains("Earth Shock", abilitiesFor (EntityId 4) (PartyAi.decide w))

/// The healer seat is a lane, not a class, and every class that holds a kick can hold it.
///
/// An assignment is a Party-wide promise that *this* member will break that cast: the
/// other members are told it is covered. So a lane that ignores its assignment does not
/// delay the kick, it **drops** it. That was the shaman: `healerOrders` never read
/// `interruptTargets`, Earth Shock's 6 s cooldown won it assignments the Mage's 30 s
/// Counterspell then never answered, and 30 seeds of `sim shmana` recorded zero Earth
/// Shock casts (`.pi/research/octowow/shaman-damage-and-interrupts.md` §1-2).
///
/// Written per class rather than per lane because the hole was a lane, and the roster has
/// nine classes for five seats: the next class seated as a healer must not need this test
/// edited. The kick also has to come before the heal — a hurt ally is standing right there
/// and a stopped cast is damage that never lands.
[<Fact>]
let ``a kick kept in the healer seat is spent on the cast the party assigned`` () =
  let kickers =
    [ "Warrior", Content.shieldBashRanks
      "Rogue", Content.kick
      "Mage", Content.counterspellRanks
      "Shaman", Content.earthShockRanks
      "Hunter", Content.scatterShot ]

  let slow = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  for name, kick in kickers do
    let healer =
      Content.hero 2 name Healer 1000 1666 slow 10000 (AbilityDef.kitAt 24 [ kick ]) { X = 5; Y = 6 }

    let hurt =
      { Content.hero 3 "Priest" Dps 2000 1000 slow 5000 [] { X = 5; Y = 5 } with
          Health = 200 }

    let caster =
      { Content.mob 100 "Caster" CreatureType.Humanoid 36 4000 1000 None [] None { X = 6; Y = 5 } with
          Engaged = true
          Target = Some(EntityId 1)
          Casting =
            Some
              { Ability = Content.heal
                Target = EntityId 100
                Remaining = secTicks 2.0
                Pushes = 0 } }

    let world =
      { Tick = ticks 0
        RestTicks = ticks 0
        Grid = Grid.create 12 10
        Entities = [ healer; hurt; caster ]
        Pending = []
        Rng = Rng.streamsOf 5UL
        PartyBuffs = []
        Quests = []
        Forecast = Forecast.empty
        TotemLedger = TotemLedger.empty
        Log = [] }

    Assert.Contains(kick.Name, abilitiesFor healer.Id (PartyAi.decide world))

/// The same contract at the party level, on the observable outcome rather than on the
/// private assignment map: a cast the roster holds a kick for goes unanswered exactly
/// never, and is kicked by two members exactly never. `Assert.Single` is the whole test —
/// zero is the shaman's dropped assignment, two would be the twenty-four-second
/// Counterspell the assignment exists to stop being spent.
[<Fact>]
let ``a cast the party holds a kick for is broken exactly once`` () =
  let roster = [ "Warrior"; "Priest"; "Mage"; "Warlock"; "Shaman" ]
  let party = Content.partyOf roster 36

  // The measured roster, and the seat that dropped the kick: the shaman heals here.
  let shaman = party |> List.find (fun e -> e.Name = "Shaman")
  Assert.True(shaman.Role = Some Healer, "the shaman holds the healer seat in the measured roster")

  let priest = party |> List.find (fun e -> e.Name = "Priest")

  // Every member's cast-breaking button, read off the kits rather than named here, so a
  // row that gains an `Interrupt` is counted without this list being edited.
  let kickNames =
    party
    |> List.collect (fun e ->
      e.Abilities
      |> List.filter (fun a -> a.Effects |> List.contains Interrupt)
      |> List.map (fun a -> a.Name))
    |> Set.ofList

  let placed (pos: Pos) (e: Entity) =
    { e with
        Pos = pos
        Destination = None
        Path = []
        Target = Some(EntityId 100) }

  // Four tiles from the shaman and eight from everyone else: out of the melee kicks'
  // reach, inside Earth Shock's five yards, and closer than Counterspell's thirty — so
  // the only thing separating the two casters is the cooldown, which is the recorded
  // mechanism (6 s beats 30 s).
  let world =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities =
        (party
         |> List.map (fun (e: Entity) ->
           let seated =
             if e.Id = shaman.Id then placed { X = 5; Y = 5 } e else placed { X = 1; Y = 1 } e

           // A hurt ally in heal range: the lane's own job is to press a heal here, and
           // the kick has to come first anyway. Behind that heal is precisely where the
           // Earth Shocks went missing.
           if e.Id = priest.Id then { seated with Health = 120 } else seated))
        @ [ { Content.mob 100 "Caster" CreatureType.Humanoid 36 4000 1000 None [] None { X = 9; Y = 5 } with
                Engaged = true
                Target = Some shaman.Id
                Casting =
                  Some
                    { Ability = Content.heal
                      Target = EntityId 100
                      Remaining = secTicks 2.0
                      Pushes = 0 } } ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let kicks =
    PartyAi.decide world
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(actor, name, target) when target = EntityId 100 && Set.contains name kickNames -> Some(actor, name)
      | _ -> None)

  let kick = Assert.Single(kicks)
  Assert.Equal(shaman.Id, fst kick)
  Assert.Equal("Earth Shock", snd kick)

/// Shocks share one 6 s cooldown in WoW, and `Content.shockCooldownGroup` is what says so.
/// Without it the interrupt fix above would have handed the shaman two 6 s shock buttons:
/// a second Earth Shock for control on a ladder that allowed one, and a free Flame Shock
/// that the pre-pass rotation would have kept pressing for damage either way.
[<Fact>]
let ``the shocks share one six second cooldown`` () =
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
      { X = 5; Y = 6 }

  let mob = Content.mob 100 "Mob" CreatureType.Humanoid 36 4000 1000 None [] None { X = 6; Y = 5 }

  let world =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ shaman; mob ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let press name (w: World) =
    SimCombat.applyCommand { ApplyAt = ticks 0; Kind = UseAbility(shaman.Id, name, mob.Id) } w

  let cooldowns (w: World) = (SimState.tryEntity shaman.Id w).Value.Cooldowns

  // Earth Shock starts both timers.
  let afterShock = press "Earth Shock" world

  Assert.True((cooldowns afterShock).ContainsKey "Flame Shock", "Earth Shock puts Flame Shock on its timer")

  Assert.True((cooldowns afterShock).ContainsKey "Earth Shock", "and its own, obviously")

  // The bolt is a different spell and keeps its own timer: the group is the shocks, not
  // every spell the shaman holds.
  Assert.False((cooldowns afterShock).ContainsKey "Lightning Bolt", "the bolt is not a shock")

  // And the other way round: Flame Shock starts the shared timer too.
  let afterFlame = press "Flame Shock" world

  Assert.True((cooldowns afterFlame).ContainsKey "Earth Shock", "Flame Shock puts Earth Shock on its timer")

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

  // Corruption is the dot `damageOf` cannot see, so it opens the pull; the bolt —
  // the hardest cast in the kit — is what the rotation asks for once the dot ticks.
  Assert.Contains("Corruption", abilitiesFor (EntityId 2) (PartyAi.decide w))

  let dotted =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e ->
            if e.Id = EntityId 100 then
              { e with Auras = e.Auras @ [ Corrupted(secTicks 18.0, 12, Some(EntityId 2)) ] }
            else
              e) }

  Assert.Contains("Shadow Bolt", abilitiesFor (EntityId 2) (PartyAi.decide dotted))

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
              Remaining = secTicks 2.0
              Pushes = 0 } }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; mage; healer ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
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

/// A pull waits for the Party. The Tank that leaves the moment the last mob dies
/// opens the next pack with the healer still down the corridor, or still empty —
/// and out-of-combat regeneration is 15% of the pool a second, so the wait costs
/// a couple of seconds and buys the whole pull.
[<Fact>]
let ``the tank waits for the party before pulling the next pack`` () =
  // One pack nobody has noticed, six tiles off, so reaching it is a walk. The
  // grid is widened only so a straggler can stand further out than the leash.
  let w =
    let basew = partyWorld None [] 1000

    { basew with
        Grid = Grid.create 20 20
        Entities =
          basew.Entities
          |> List.filter (fun e -> e.Name <> "Far")
          |> List.map (fun e ->
            if e.Name = "Near" then
              { e with
                  Pos = { X = 11; Y = 5 }
                  Engaged = false
                  Target = None }
            else
              e) }

  let tankMoves (w: World) =
    PartyAi.decide w
    |> List.exists (fun c ->
      match c.Kind with
      | MoveTo(a, _) when a = EntityId 1 -> true
      | _ -> false)

  let tankStops (w: World) =
    PartyAi.decide w
    |> List.exists (fun c ->
      match c.Kind with
      | Stop a -> a = EntityId 1
      | _ -> false)

  // Everyone up and stocked: the Tank leads.
  Assert.True(tankMoves w, "a ready Party should be led to the pack")

  // A healer on empty is not a ready Party, however close it stands.
  let dry =
    { w with
        Entities = w.Entities |> List.map (fun e -> if e.Id = EntityId 2 then { e with Resource = 0 } else e) }

  Assert.False(tankMoves dry, "the Tank should not open a pack with the healer on empty")
  Assert.True(tankStops dry, "and it should clear the route it was already walking")

  // Nor is a Party strung out beyond the leash, even on full pools.
  let straggler =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e -> if e.Id = EntityId 2 then { e with Pos = { X = 18; Y = 18 } } else e) }

  Assert.False(tankMoves straggler, "the Tank should wait for a member outside the leash")

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
let ``out of combat the party waits five seconds then snaps to full, and mobs never heal`` () =
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

  // Restoring is a step, not a slope: a second of rest changes nothing, because the
  // party has to wait five before anything comes back.
  Assert.Equal(warrior0.Health, warrior1.Health)

  let priest0 = idle.Entities |> List.find (fun e -> e.Name = "Priest")
  let priest1 = after.Entities |> List.find (fun e -> e.Name = "Priest")
  Assert.Equal(priest0.Resource, priest1.Resource)

  // Five seconds of rest plus half a second of the snap, and every pool is clean.
  let restored = SimTick.run 110 idle
  let warrior2 = restored.Entities |> List.find (fun e -> e.Name = "Warrior")
  Assert.Equal(warrior2.MaxHealth, warrior2.Health)
  let priest2 = restored.Entities |> List.find (fun e -> e.Name = "Priest")
  Assert.Equal(priest2.MaxResource, priest2.Resource)

  // Mobs are not on that clock at all: a wounded one is still wounded however long the
  // party rests beside it. Asked of the phase directly, because through `run` the
  // autopilot would be shooting it and the hit points would move for other reasons.
  let wounded =
    { w with
        RestTicks = ticks 500
        Entities =
          w.Entities
          |> List.map (fun e -> if e.Faction = Hostile then { e with Health = 1 } else e) }

  let rested = SimTick.advanceOutOfCombatRegen wounded

  for e in rested.Entities |> List.filter (fun e -> e.Faction = Hostile) do
    Assert.Equal(1, e.Health)

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
  // Twenty ticks is one second, less than a single two-second mana tick — and this
  // Priest has been healing, so the pool is going down, not up. The assertion is the
  // rule that matters: combat pays the trickle on its own clock and never the pool,
  // however long the fight runs.
  Assert.True(priest2.Resource < priest2.MaxResource, "combat should not restore the pool")

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

  // `decide` answers with one order per member, and the mage's first tick of a
  // fight spends its window rows (Arcane Power, then Arcane Rupture), so the school
  // under test is what it picks once those cooldowns are out of the way. The
  // cooldowns are set by hand rather than the world being advanced: the question
  // here is the rotation, not the opening tick.
  let schools (w: WowRoguelike.Core.World) =
    w
    |> SimState.mapEntity (EntityId 4) (fun e ->
      { e with
          Cooldowns =
            e.Cooldowns
            |> Map.add "Arcane Power" (ticks 1800)
            |> Map.add "Arcane Rupture" (ticks 1800) })
    |> PartyAi.decide
    |> abilitiesFor (EntityId 4)

  // Arcane Missiles: 280 for 235 mana over 5 s. Fireball: 195 for 140 over 3.5 s.
  Assert.Contains("Arcane Missiles", schools (mageWorld 100))
  // Nearly dry it plays for damage per mana instead, where Fireball (195 for 140,
  // 1.39 a point) beats Frostbolt (83 for 65, 1.28) and the 235-mana channel.
  let dry = schools (mageWorld 15)
  Assert.Contains("Fireball", dry)
  Assert.DoesNotContain("Arcane Missiles", dry)

/// Casting speed is a clock, and the two directions are not the same arithmetic: a
/// numbed caster takes `+bp%` longer, while a hasted one takes `10000/(10000+bp)` of
/// the time, because "30% faster" means two thirds and not 130%.
[<Fact>]
let ``haste shortens a cast and numbness lengthens it`` () =
  let mage = (Content.partyOf [ "Mage" ] 60).[0]
  let frostbolt = mage.Abilities |> List.find (fun a -> a.Name = "Frostbolt")
  // The last rank's 2.5 s, then 10000/13000 of it, then +50% of it.
  Assert.Equal(50, int (SimCombat.castClock frostbolt []))
  Assert.Equal(38, int (SimCombat.castClock frostbolt [ Casting(secTicks 20.0, 3000) ]))
  Assert.Equal(75, int (SimCombat.castClock frostbolt [ Numbed(secTicks 20.0, 5000) ]))

/// Arcane Power is a 20 s window on a 3-minute cooldown, so it is pressed once and
/// then left alone: a second press inside the window would buy nothing.
[<Fact>]
let ``Arcane Power is spent once per cooldown and then left alone`` () =
  let mutable w = Content.gully 3UL

  for _ in 1 .. 600 do
    if SimState.continues w then
      w <- SimTick.step (Bench.autoPilot w) w

  let presses = w.Log |> List.filter (fun l -> l.Contains "casting quickens") |> List.length
  Assert.True(presses >= 1, sprintf "the Mage should spend Arcane Power, log has %d presses" presses)
  Assert.True(presses <= 2, sprintf "600 ticks is under one 3-minute cooldown, got %d presses" presses)

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

/// Five points of Eviscerate at rank 4 is 165-185, improved 15% by Improved Eviscerate
/// 3/3 to 189-212, and the points are gone.
[<Fact>]
let ``eviscerate cashes in the combo points for its five-point damage`` () =
  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> SimState.mapEntity (EntityId 1) (fun e -> { e with ComboPoints = 5; CritChanceBp = 0 })

  let before = (mobOf w).Health
  let w = SimCombat.resolveAbility (EntityId 1) (EntityId 100) (AbilityDef.at 24 Content.eviscerate).Value w
  let dealt = before - (mobOf w).Health

  Assert.True(dealt >= 189 && dealt <= 212, sprintf "dealt %d" dealt)
  // Ruthlessness 3/3: the finisher zeroes the pool and then hands one point back.
  Assert.Equal(1, (entBy 1 w).ComboPoints)

/// Energy is 20 per 2 s on its own clock: 20 Ticks to the second, so 40 Ticks
/// puts a rogue who spent everything back at 20.
[<Fact>]
let ``a rogue's energy comes back twenty per two seconds`` () =
  // Energy refills on its own clock, but only during a fight — with nothing
  // engaged nothing regenerates at all — so the rate is measured with a mob
  // awake. The pull radius is two tiles and `gully`'s nearest mob stands further
  // than that, so the engagement is stated rather than waited for.
  let w =
    Content.gully 1UL
    |> fun w ->
      let mob = w.Entities |> List.find (fun e -> e.Faction = Hostile)
      SimState.mapEntity mob.Id (fun e -> { e with Engaged = true }) w
    |> SimState.mapEntity (EntityId 3) (fun e -> { e with Resource = 0 })

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

/// The newest content is seated by name and played by effect. A bear's Growl is
/// the Warrior's Taunt under a feral name, so the script may not look for
/// "Taunt" to find it: the mob that has left the tank is taunted back by
/// whatever the tank's own kit calls its taunt.
[<Fact>]
let ``a bear tank taunts with its own Growl`` () =
  let bear =
    Content.partyOf [ "Druid (Bear)" ] 24
    |> List.head
    |> fun e -> { e with Id = EntityId 3; Pos = { X = 5; Y = 4 } }

  let w =
    partyWorld (Some(EntityId 100)) [] 1000
    |> fun w -> { w with Entities = w.Entities @ [ bear ] }

  // The Near mob is on the Warrior, so it is a mob that has left the Bear.
  Assert.Contains("Growl", abilitiesFor (EntityId 3) (PartyAi.decide w))

/// Shred and Claw are the cat's own pair, 225% weapon damage against Claw's 100%,
/// Ferocious Bite its finisher and Rip its bleed finisher: the rotation finds them
/// by what they do — the harder builder is the one that needs the mob's back — so a
/// Cat fights without the script ever naming a Rogue ability.
///
/// Level 32 is Ferocious Bite's first rank, and the cat has no finisher below it;
/// the row is the ranks table's ladder, not a gap in the kit.
[<Fact>]
let ``a cat druid shreds and ferocious bites`` () =
  // Berserk is on cooldown for every case below except the one that asks about it:
  // a full bar with the cooldown ready is a Berserk tick, and the rest of these
  // cases are about the strike the cat picks once it has pressed that.
  let catAt (points: int) (resource: int) (berserkReady: bool) =
    Content.partyOf [ "Druid (Cat)" ] 32
    |> List.head
    |> fun e ->
      { e with
          Id = EntityId 3
          Pos = { X = 7; Y = 5 }
          ComboPoints = points
          Resource = resource
          Cooldowns =
            if berserkReady then
              e.Cooldowns
            else
              e.Cooldowns |> Map.add "Berserk" (ticks 100) }

  let orders (cat: Entity) (onCat: bool) (touched: bool) (bleeds: Aura list) =
    let w =
      partyWorld (Some(EntityId 100)) [] 1000
      |> fun w -> { w with Entities = w.Entities @ [ cat ] }
      |> if onCat then
           SimState.mapEntity (EntityId 100) (fun e -> { e with Target = Some(EntityId 3) })
         else
           id
      // The opener branch stands in for stealth by requiring an untouched mob: full
      // health and no aura. One point of health spent closes that window for the
      // pull, which is what every press after the first one sees.
      |> if touched then
           SimState.mapEntity (EntityId 100) (fun e -> { e with Health = e.Health - 1 })
         else
           id
      // A bleed the cat itself applied, by caster id: Rake's nine seconds and Rip's
      // twelve are one `Bleeding` case, so the caster is the only thing that tells
      // the cat's bleed from a Warlock's Corruption.
      |> if bleeds.IsEmpty then
           id
         else
           SimState.mapEntity (EntityId 100) (fun e -> { e with Auras = bleeds })

    abilitiesFor (EntityId 3) (PartyAi.decide w)

  let full = (Content.partyOf [ "Druid (Cat)" ] 32 |> List.head).MaxResource
  let bleeding = [ Bleeding(secTicks 9.0, 8, Some(EntityId 3)) ]

  let ordersAt (points: int) (onCat: bool) (touched: bool) (bleeds: Aura list) =
    orders (catAt points full false) onCat touched bleeds

  // The stealth window. An untouched mob at full health with no debuff is the enemy
  // the Cat met from behind, so the pull opens with Ravage — 350% weapon damage, the
  // feral Ambush — rather than with the Shred it leads with once the window closes.
  Assert.Contains("Ravage", ordersAt 0 false false [])

  // Past the opener a target carrying none of the cat's bleed is the one the upkeep
  // is for: Rake is forty energy for nine seconds of bleed, and it carries a combo
  // point, so it costs the bar nothing it would not have spent building anyway.
  Assert.Contains("Rake", ordersAt 0 false true [])

  // Ravage must *not* still be on offer: it is an opener, not a rotation strike.
  Assert.DoesNotContain("Ravage", ordersAt 0 false true [])

  // With the bleed up the points are what is worth building: Shred from behind,
  // because the mob is on the Warrior and its back is open.
  Assert.Contains("Shred", ordersAt 0 false true bleeding)

  // A mob that has turned on the Cat has no back to offer it, so the cheaper
  // builder is the one it strikes with. That only holds if the positional builder
  // was found by its damage rather than by the Rogue's name for it.
  Assert.Contains("Claw", ordersAt 0 true true bleeding)
  Assert.DoesNotContain("Shred", ordersAt 0 true true bleeding)

  // Five points on a bleeding target are the Bite's: a second Rip is a second aura,
  // not a refresh, so the reusable finisher is the one that pays in a hit.
  Assert.Contains("Ferocious Bite", ordersAt 5 false true bleeding)

  // Five points on a target that is *not* bleeding are Rip's, at thirty energy
  // against the Bite's thirty-five — the cheaper of the two, and the one that puts
  // the bleed back up.
  Assert.Contains("Rip", ordersAt 5 false true [])

  // Berserk wants a full bar and a mob in reach: the doubling is worth nothing if
  // there is no energy left to double and nowhere to spend it.
  Assert.Contains("Berserk", orders (catAt 5 full true) false true bleeding)

  // Ten energy buys nothing in this kit — Claw is 45, Rake 40, Rip 30 — so the cat
  // pools on the mob rather than walking out of its own auto-attack.
  Assert.Empty(orders (catAt 0 10 false) false true bleeding)

/// The cat reaches the field, stays in Cat Form and fights with its own rows. The
/// plumbing this pins is the part no unit test above covers: `withRosterAt` is the
/// only route a non-default class takes into a party, `Content.druid` builds the kit
/// per spec and births the body already shaped, and `PartyAi.decide` has to route a
/// `Shaped CatForm` Dps into `catOrders` rather than into the Rogue's lane.
[<Fact>]
let ``a seated cat druid reaches the field in Cat Form and fights`` () =
  let w0 =
    Content.dungeonAt 24 1UL
    |> Content.withRosterAt 24 [ "Warrior"; "Priest"; "Druid (Cat)"; "Mage"; "Rogue" ]
    |> fun w ->
      { w with
          Entities =
            w.Entities
            |> List.map (fun e -> if e.Faction = Hostile then { e with Engaged = true } else e) }

  // Every Druid spec carries the name "Druid" — it is one hero sheet with four kits
  // — so the cat is the member standing in Cat Form, not the last seat in the list.
  let cat =
    w0.Entities
    |> List.filter (fun e -> e.Faction = Party)
    |> List.find (fun e -> SimState.inStance e CatForm)


  // The roster name is the kit: a body seated as "Druid (Cat)" is born shaped, holds
  // Energy rather than mana, and carries the cat's rows and no heal at all — which is
  // what makes "does not heal from cat form" true by construction rather than by a
  // branch in the AI.
  Assert.Equal(Energy, cat.ResourceKind)
  // A flat hundred, the same bar the Rogue is built with: energy is the one pool
  // whose size `Content.fs` states without a level curve.
  Assert.Equal(100, cat.MaxResource)
  Assert.True(SimState.inStance cat CatForm)
  Assert.DoesNotContain(
    cat.Abilities,
    fun a ->
      Ability.group a = AbilityGroup.Heal
      || Ability.group a = AbilityGroup.HealOverTime
  )

  let mutable w = w0

  // Sampled every tick rather than only at the end: the pool is a cycle, so an
  // end-of-fight assert would pass on a bar that overflowed and came back down.
  let mutable overfilled = false

  // Twelve hundred ticks, not four hundred: at level 24 the cat's only finisher is
  // Rip, and the lane spends it when a bleed lapses with four points banked — a
  // nine-second cycle plus the builders that pay for it. Four hundred ticks is two
  // Rake windows and not one spend.
  for _ in 1 .. 1200 do
    if SimState.continues w then
      w <- SimTick.step (Bench.autoPilot w) w
      let e = w.Entities |> List.filter (fun e -> e.Id = cat.Id) |> List.exactlyOne
      if e.Resource > e.MaxResource || e.ComboPoints > 5 then overfilled <- true

  Assert.False(overfilled)

  let after = w.Entities |> List.filter (fun e -> e.Id = cat.Id) |> List.exactlyOne

  // Still a cat: nothing in the rotation shifts out of the shape it was born in, and
  // `Shaped` is what would refuse a cast, so a cat that shifted would be an idle one.
  Assert.True(SimState.inStance after CatForm)

  // Damage was dealt with the cat's own rows, and a bleed was applied — the two
  // things `catOrders` adds over the Rogue lane the body used to fall into. The
  // wording is `SimCombat`'s own: `<name>: <ability> bleeds <target>, ...`, which no
  // other route into the sim produces from an ability.
  let its = w.Log |> List.filter (fun l -> l.StartsWith "Druid")
  Assert.True(its |> List.exists (fun l -> l.Contains "Claw" || l.Contains "Shred"), String.concat " | " its)
  Assert.True(its |> List.exists (fun l -> l.Contains ": Rake bleeds" || l.Contains ": Rip bleeds"), String.concat " | " its)
  // And the points were *spent*: every finisher line carries the count it consumed,
  // which is the only thing in the log that proves Rip was not merely attempted.
  Assert.True(its |> List.exists (fun l -> l.Contains "from " && l.Contains " points"), String.concat " | " its)


/// The operator's Moonkin build (`oCaYYKFYJLDAB--FAAoAC`, Balance 39 / Restoration
/// 12) as far as the talent record reaches. This pins the arithmetic rather than the
/// intent: Improved Moonfire's 10% and Moonfury's 12% have to arrive as one 2200 row
/// because a spell gets a single row and a second one would be silently dropped, and
/// Moonglow's 9% has to sit on both damage spells. The picks the record cannot
/// honour are named in `moonkinTalents`; a count of them would go stale the moment
/// one lands, so this asserts the rows a Moonkin actually casts with.
[<Fact>]
let ``a moonkin carries its balance and restoration build`` () =
  let druid spec = Content.partyOf [ spec ] 60 |> List.head
  let moonkin = (druid "Druid (Moonkin)").Talents
  let tree = (druid "Druid").Talents

  // Improved Wrath 5/5 (half a second off the cast) and Moonglow 3/3 (9% cheaper).
  // `damageBp` is baseline-relative — 10000 is unmodified — so 12% over is 11200.
  Assert.Equal(11200, Talents.damageBp "Wrath" moonkin)
  Assert.Equal(900, Talents.costBp "Wrath" false moonkin)
  Assert.Equal(secTicks 0.5, Talents.castOff "Wrath" moonkin)

  // Improved Moonfire 2/2 (10% damage, 10% crit) folded with Moonfury 3/3 (12%
  // damage) into the one row the spell gets; Moonglow 3/3's 9% cost on top.
  Assert.Equal(12200, Talents.damageBp "Moonfire" moonkin)
  Assert.Equal(1000, Talents.critBp "Moonfire" moonkin)
  Assert.Equal(900, Talents.costBp "Moonfire" false moonkin)

  // Vengeance 5/5 (+100% crit damage) and Reflection 2/3 (10% regen while casting)
  // are class-wide fields, not per-spell rows.
  Assert.Equal(10000, moonkin.CritDamageBp)
  Assert.Equal(1000, moonkin.RegenBp)

  // A Tree must not inherit any of it: the build is the spec's, not the class's.
  Assert.Equal(10000, Talents.damageBp "Wrath" tree)
  Assert.Equal(10000, Talents.damageBp "Moonfire" tree)
  Assert.NotEqual(moonkin.CritDamageBp, tree.CritDamageBp)

/// Nobody has noticed the Party yet — the only state `prePull` runs in, and the
/// state that stands in for a stealthed Rogue.
let private calmWorld (rogue: Entity) (mobs: Entity list) : World =
  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 12 10
    Entities = rogue :: mobs
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
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

/// Sap's incap lasts forty-five seconds -- the operator's direction on the
/// duration, and what makes a pre-pull Sap worth the walk it costs.
[<Fact>]
let ``sap lasts forty-five seconds`` () =
  match (AbilityDef.at 24 Content.sap).Value.Effects with
  | [ Sleep d ] -> Assert.Equal(secTicks 45.0, d)
  | effects -> failwithf "expected one Sleep effect, got %A" effects

/// The opener sequence the operator directed: the Mage's Polymorph takes the
/// surplus mob, the Warrior's Charge takes the focus, and the sheeped mob stays
/// out of the fight -- no target, no engagement, no damage -- for as long as the
/// sheep holds.
[<Fact>]
let ``the opener sheep takes one mob and the charge takes the focus`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let tank =
    Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.charge; Content.sunderArmor ] { X = 5; Y = 5 }

  let mage =
    Content.hero
      4
      "Mage"
      Dps
      908
      2373
      tap
      10000
      [ (AbilityDef.at 24 Content.polymorphRanks).Value ]
      { X = 4; Y = 5 }

  let focusMob = Content.mob 100 "Focus" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 10; Y = 5 }

  let ccMob = Content.mob 101 "Sheepme" CreatureType.Humanoid 21 800 800 (Some tap) [] None { X = 11; Y = 5 }

  let w0 =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 16 8
      Entities = [ tank; mage; focusMob; ccMob ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let mutable w = w0

  // The opener lands first: until the cast resolves the target mob is
  // legitimately uncontrolled.
  let mutable landed = false

  for _ in 1 .. 400 do
    w <- SimTick.step (Bench.autoPilot w) w
    let sheep = w.Entities |> List.find (fun e -> e.Id = EntityId 101)

    if not landed then
      landed <- w.Log |> List.exists (fun l -> l.Contains "polymorphs Sheepme")
    else
      if sheep.Health < sheep.MaxHealth && sheep.Health > 0 then
        failwith "the sheeped mob took damage while controlled"

      // `callForHelp` flags the whole room Engaged, sleeping bodies included, so
      // the flag is not the test: a controlled mob acts on nothing and targets
      // nobody -- that is what staying out of the fight means.
      if sheep.Target.IsSome || SimState.canAct sheep then
        failwith "the sheeped mob joined the fight while controlled"

  Assert.True(
    w.Log |> List.exists (fun l -> l.Contains "polymorphs Sheepme")
    && w.Log |> List.exists (fun l -> l.Contains "charges Focus"),
    sprintf "LOGDUMP: %s" (w.Log |> String.concat " | ")
  )

/// A Tree Druid openers with Hibernate, and only onto what the vanilla spell
/// accepts: a Beast, never a Humanoid, which the engine's wider `sleepable` set
/// would otherwise allow.
[<Fact>]
let ``the tree druid hibernates a beast and not a humanoid`` () =
  let druid = Content.partyOf [ "Druid" ] 24 |> List.head
  let druid = { druid with Pos = { X = 5; Y = 5 } }

  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let withMobs (mobs: Entity list) =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 16 8
      Entities = druid :: mobs
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let humanoid = Content.mob 100 "Fang" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 6; Y = 5 }
  let beast = Content.mob 101 "Raptor" CreatureType.Beast 21 900 900 (Some tap) [] None { X = 6; Y = 6 }
  let orders = abilitiesFor druid.Id (PartyAi.decide (withMobs [ humanoid; beast ]))
  Assert.Contains("Hibernate", orders)

  // Both Humanoids: nothing Hibernate can land on, so nothing is booked.
  let humanoid2 = Content.mob 102 "Fang2" CreatureType.Humanoid 21 900 900 (Some tap) [] None { X = 6; Y = 6 }
  let none = abilitiesFor druid.Id (PartyAi.decide (withMobs [ humanoid; humanoid2 ]))
  Assert.DoesNotContain("Hibernate", none)

/// The runner's answers are in the level-24 kits, each with its sourced shape:
/// Hamstring (W9: 5 damage, 40 % for 15 s), Frost Nova (M3: an 8 s root), and
/// Frostbolt's chill riding the damage (rank 4: 40 % for 7 s).
[<Fact>]
let ``the level-24 warrior and mage carry the runner's answers`` () =
  let warrior = Content.partyOf [ "Warrior" ] 24 |> List.find (fun e -> e.Name = "Warrior")
  let hamstring = warrior.Abilities |> List.find (fun a -> a.Name = "Hamstring")

  Assert.Equal<Effect list>([ Damage(5, 5); Cripple(secTicks 15.0, 4000) ], hamstring.Effects)

  let mage = Content.partyOf [ "Mage" ] 24 |> List.find (fun e -> e.Name = "Mage")
  let nova = mage.Abilities |> List.find (fun a -> a.Name = "Frost Nova")

  Assert.Equal<Effect list>([ Root(secTicks 8.0) ], nova.Effects)

  let bolt = mage.Abilities |> List.find (fun a -> a.Name = "Frostbolt")

  Assert.True(
    bolt.Effects
    |> List.exists (function
      | Cripple(d, bp) -> d = secTicks 7.0 && bp = 4000
      | _ -> false),
    "Frostbolt rank 4 should chill 40 % for 7 s"
  )

/// The stitched caverns stand in packs: Anacondra with her guards, and no
/// aggro-radius chain spanning more than a pack plus its linked neighbour. The
/// scatter this replaced chained the whole gully -- fifteen mobs one pull.
[<Fact>]
let ``the stitched caverns stand in packs`` () =
  let w = Content.wailingCaverns 1UL
  let hostiles = w.Entities |> List.filter (fun e -> e.Faction = Hostile && not e.Dormant)
  let ana = hostiles |> List.find (fun e -> e.Name = "Lady Anacondra")

  Assert.True(
    hostiles
    |> List.exists (fun m -> m.Id <> ana.Id && Pos.chebyshev m.Pos ana.Pos <= 2),
    "Anacondra should stand with her guards"
  )

  // What one pull becomes is the biggest call-for-help *ball*: a fight starts
  // at proximity and widens by `callForHelpRadius` from every mob that gets
  // hurt -- but only around the hurt mob, so the transitive chain overstates it
  // and the ball is the honest number. Twelve is the densest room (the Crag's
  // lashers); the scatter this replaced made the first pull of the gully the
  // whole room at fifteen. The bound is the regression line: no arrangement
  // may drift back toward room-size.
  let biggestBall =
    hostiles
    |> List.map (fun m ->
      hostiles |> List.filter (fun h -> Pos.chebyshev m.Pos h.Pos <= SimState.callForHelpRadius) |> List.length)
    |> List.max

  Assert.True(biggestBall <= 12, sprintf "the biggest pull is %d, a pack and its links at most" biggestBall)

/// A hurt humanoid runs for the next group, and the party stops it: the Warrior
/// Hamstrings the runner the tick it turns. The runner never makes the ally, so
/// the pack it was fetching stays asleep.
[<Fact>]
let ``a hurt humanoid runs for help and gets hamstrung`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let tank =
    Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor; Content.hamstring ] { X = 5; Y = 5 }

  let runner =
    { Content.mob 100 "Runner" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 6; Y = 5 } with
        Health = 150
        Engaged = true
        Target = Some(EntityId 1) }

  let ally = Content.mob 101 "Ally" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 12; Y = 5 }

  let w0 =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 16 8
      Entities = [ tank; runner; ally ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let mutable w = w0

  for _ in 1 .. 10 do
    w <- SimTick.step (Bench.autoPilot w) w

  Assert.Contains(w.Log, fun (l: string) -> l.Contains "flees toward")
  Assert.Contains(w.Log, fun (l: string) -> l.Contains "cripples Runner")

  // And the run it was making never arrived: the ally is still un-engaged.
  let allyAfter = w.Entities |> List.find (fun e -> e.Id = EntityId 101)
  Assert.False(allyAfter.Engaged, "the fetched pack should still be asleep")

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
    RestTicks = ticks 0
    Grid = Grid.ofRows rows
    Entities = [ tank; rogue; mob ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
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

/// The operator's corridor case: the Rogue has no flank because the mob's back is
/// a wall, and no sidestep fixes it — the only move that opens a back is walking
/// the fight back to open floor, dragging the pack by its threat (directive
/// m02461). The Tank pulls 2-3 tiles along its approach and only onto
/// room-shaped floor, so a mid-corridor fight has no candidate at all: the pull
/// is a room-mouth tool, not a kite.
[<Fact>]
let ``the tank pulls the fight back when no flank tile exists`` () =
  let w =
    backedWorld
      [ "##############"
        "##############"
        "##############"
        "###..........#"
        "###..........#"
        "###..........#"
        "###..........#"
        "###..........#"
        "##############"
        "##############" ]
      { X = 7; Y = 3 }
      { X = 7; Y = 4 }
      { X = 6; Y = 4 }

  let mob = entBy 100 w
  Assert.False(Grid.isFloor w.Grid (backIfAt mob { X = 7; Y = 4 }), "the fixture needs the back walled")

  match moveFor (EntityId 1) (PartyAi.decide w) with
  | None -> failwith "the tank never moved, so the rogue has no flank forever"
  | Some p ->
    // The pull, not a sidestep: 2-3 tiles, on the approach line, on floor, and
    // each tile of it widens the gap to the mob.
    let step = Pos.chebyshev p { X = 7; Y = 4 }
    Assert.True(step >= 2 && step <= 3, sprintf "a %d-tile move is a sidestep, not a pull" step)
    Assert.True(Grid.isFloor w.Grid p, sprintf "the tank would pull into a wall at %A" p)
    Assert.True(
      Pos.chebyshev p mob.Pos > Pos.chebyshev { X = 7; Y = 4 } mob.Pos,
      sprintf "the tank moved to %A without widening the gap to the mob" p
    )

/// The corridor freeze. `claimedByOther` reports a mover's tile as free, because
/// teammates travel a corridor in single file through one tile — but an aside that
/// trusts it then steps onto the body, the two collide in `advanceMovement`, and the
/// pair churns in place. In graveyard this held the Tank and the Mage against each
/// other for 40000 ticks with the whole party alive and nothing engaged.
[<Fact>]
let ``no ally steps aside onto a tile a body already holds`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let tank =
    { Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 4; Y = 4 } with
        Goal = Some { X = 8; Y = 7 } }

  let mage =
    Content.hero 2 "Mage" Dps 1000 1000 tap 10000 [ Content.fireball ] { X = 4; Y = 5 }

  // Every tile around the Mage is held. One holder is mid-move, which is the case
  // `claimedByOther` calls free — the tile the aside used to pick.
  let holders =
    [ (3, 4), None
      (5, 4), None
      (3, 5), None
      (5, 5), None
      (3, 6), Some { X = 5; Y = 6 }
      (4, 6), None
      (5, 6), None ]
    |> List.mapi (fun i ((x, y), dest) ->
      { Content.hero (3 + i) "Filler" Dps 1000 1000 tap 10000 [ Content.fireball ] { X = x; Y = y } with
          Destination = dest })

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid =
        Grid.ofRows
          [ "#########"
            "#.......#"
            "#.......#"
            "#.......#"
            "#.......#"
            "#.......#"
            "#.......#"
            "#.......#"
            "#########" ]
      Entities = tank :: mage :: holders
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // The fixture is only worth anything if the Mage really has no free tile.
  let held p = w.Entities |> List.exists (fun o -> o.Pos = p && o.Id <> EntityId 2 && SimState.alive o)

  Path.neighbours (fun p -> Grid.isFloor w.Grid p && not (held p)) { X = 4; Y = 5 }
  |> List.iter (fun p -> failwithf "the fixture leaves %A free, so the mage has somewhere to go" p)

  // The aside is the only thing issuing moves here: no hostile is alive.
  let aside =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | MoveTo(a, p) -> Some(a, p)
      | _ -> None)

  Assert.NotEmpty(aside)

  for (who, p) in aside do
    Assert.False(held p, sprintf "%A stepped aside onto %A, where a body already stands" who p)

/// Two movers, one free tile, the same Tick. Neither body's tile reads as `held` —
/// both are mid-step — so nothing in the old rule stopped both landings, and the
/// generated-dungeon invariant found it in `dungeon 2` at tick 20 with a Druid of the
/// Fang and a Deviate Ravager on `{X=58;Y=5}`. The rule is the one the corridor
/// already used: the tile takes one body, the earlier in the world's order, and a
/// later claimant waits the Tick exactly as it would for a body that had stopped.
/// Deterministic on purpose — the lower EntityId is the tiebreak, so replay holds.
[<Fact>]
let ``two movers landing on one tile do not share it`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  // 4,4 -> 5,4 and 6,4 -> 5,4, both one Tick from landing.
  let first =
    { Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [] { X = 4; Y = 4 } with
        Destination = Some { X = 5; Y = 4 }
        MoveTicksLeft = ticks 1
        MoveTicksTotal = ticks 2 }

  let second =
    { Content.hero 2 "Mage" Dps 1000 1000 tap 10000 [] { X = 6; Y = 4 } with
        Destination = Some { X = 5; Y = 4 }
        MoveTicksLeft = ticks 1
        MoveTicksTotal = ticks 2 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid =
        Grid.ofRows
          [ "#########"
            "#.......#"
            "#.......#"
            "#.......#"
            "#.......#"
            "#.......#"
            "#########" ]
      Entities = [ first; second ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let landed = SimTick.advanceMovement w

  Assert.Equal<Pos>({ X = 5; Y = 4 }, (entBy 1 landed).Pos)
  Assert.Equal<Pos>({ X = 6; Y = 4 }, (entBy 2 landed).Pos)

  // The loser's claim goes back: it is not a Tick of waiting on the winner's
  // tile — that wait is what froze the armory pair — it is a re-plan next tick
  // (the movement ruling: a tile may be one body's destination, never two).
  Assert.Equal<Pos option>(None, (entBy 2 landed).Destination)
  Assert.Equal<Pos list>([], (entBy 2 landed).Path)

  let tiles =
    landed.Entities |> List.filter SimState.alive |> List.countBy (fun e -> e.Pos)

  Assert.DoesNotContain(tiles, fun (_, n) -> n > 1)

/// Temporal Convergence 3/3: "Your Arcane Missiles have a 5/10/15% chance to reset
/// the cooldown of your Arcane Rupture". The record is checked against the text it
/// was ported from, then the wiring: an Arcane Missiles hit clears the button, an
/// Arcane Explosion hit — same school, different spell — does not, which is the
/// whole difference between `OnSpellHitOf` and `OnSpellHit`.
[<Fact>]
let ``temporal convergence resets arcane rupture off arcane missiles alone`` () =
  let mage = Content.partyOf [ "Mage" ] 60 |> List.exactlyOne

  let converged =
    mage.Talents.Procs |> List.find (fun p -> p.Talent = "Temporal Convergence")

  Assert.Equal(1500, converged.ChanceBp)
  Assert.Equal<int<tick>>(secTicks 15.0, converged.InternalCooldown)
  Assert.Equal(OnSpellHitOf "Arcane Missiles", converged.Event)
  Assert.Equal(ResetCooldown "Arcane Rupture", converged.Then)

  // A hundred percent so the wiring is what is under test, not the roll.
  let held =
    { mage with
        Talents =
          { mage.Talents with
              Procs = [ { converged with ChanceBp = 10000 } ] }
        Cooldowns = Map.ofList [ "Arcane Rupture", secTicks 60.0 ] }

  // Level 5, not 60: a resist is not a hit (`amount > 0` guards the proc), and a
  // level-60 dummy resists a level-60 mage's Arcane Missiles outright.
  let mob =
    Content.mob
      100
      "Dummy"
      CreatureType.Humanoid
      5
      1000000
      1000
      None
      []
      None
      { X = 8; Y = 5 }

  let world =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities = [ { held with Pos = { X = 7; Y = 5 } }; mob ]
      Pending = []
      // Seed 2 because seed 1's first draw in this stream is below the mage's miss
      // chance and the missiles resist — and a resist is not a hit, so the proc is
      // correctly silent. The seed is part of the fixture, not noise.
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let cast name world =
    let ability = mage.Abilities |> List.find (fun a -> a.Name = name)
    SimCombat.resolveAbility (EntityId 1) (EntityId 100) ability world

  let cooldown (w: World) = (entBy 1 w).Cooldowns |> Map.tryFind "Arcane Rupture"

  let exploded = world |> cast "Arcane Explosion"

  // The cast has to have landed, or "the cooldown survived" is a tautology.
  Assert.Contains(exploded.Log, fun line -> line.Contains "hits")
  Assert.True(cooldown exploded |> Option.isSome, "Arcane Explosion cleared Arcane Rupture")

  let missed = world |> cast "Arcane Missiles"
  Assert.True(
    cooldown missed |> Option.isNone,
    sprintf "Arcane Missiles did not clear Arcane Rupture; log: %A" missed.Log)

/// A one-wide tube with the Tank at the sealed end and four allies stacked behind
/// it. Only the ally at the very back has floor to step onto, and it is five tiles
/// from the Tank — further than the aside used to look. Seed 1 graveyard level 60
/// wedged exactly like this, so the queue has to drain from the back.
[<Fact>]
let ``a tank sealed in a tube drains the queue from the back`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  // The goal is past the wall: the Tank has one, so it is not merely idle, and no
  // route to it, so every follower is holding position too.
  let tank =
    { Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 7; Y = 4 } with
        Goal = Some { X = 9; Y = 4 } }

  let allies =
    [ (6, 4); (5, 4); (4, 4); (3, 4) ]
    |> List.mapi (fun i (x, y) ->
      Content.hero (2 + i) "Mage" Dps 1000 1000 tap 10000 [ Content.fireball ] { X = x; Y = y })

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid =
        Grid.ofRows
          [ "###########"
            "###########"
            "###########"
            "###########"
            "#.......###"
            "#.#########"
            "#.#########"
            "###########" ]
      Entities = tank :: allies
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let moveFor id =
    PartyAi.decide w
    |> List.tryPick (fun c ->
      match c.Kind with
      | MoveTo(a, p) when a = EntityId id -> Some p
      | _ -> None)

  // The last ally in the queue backs into the shaft, which is the only free floor
  // within six tiles of the Tank.
  Assert.Equal(Some { X = 2; Y = 4 }, moveFor 5)

  // The three ahead of it have nowhere to go and must not thrash on the spot.
  Assert.Equal(None, moveFor 2)
  Assert.Equal(None, moveFor 3)
  Assert.Equal(None, moveFor 4)

/// A party that is out of combat does not pull while any member is past `leash` of
/// the Tank — that is the rule that keeps a pull from starting with the healer
/// twelve tiles back. But the rule reads *every* member, and in a one-wide corridor
/// a trailing Dps at distance 9 freezes a party that is otherwise in position: the
/// idle in-leash member in front of it never gets an order, so the file cannot
/// close, so the trailing member never comes inside the leash. Library L36 parked
/// exactly here for 32000 ticks — Tank 16,125, idle Shaman 23,125 (7 tiles, inside
/// the leash, so no order), Mage 24,125 and Warlock 25,125 (9 tiles) both stalled on
/// the Shaman, three mobs 3 tiles from the Tank and `lineOfSight` clear.
[<Fact>]
let ``a healer inside the leash does not hold the pull for a trailing Dps`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  // One-wide tube: mobs west, Tank, then the file east of it. The healer is 7 tiles
  // back (inside the leash of 8) and the Dps 9 (outside it).
  let tank =
    Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 4; Y = 4 }

  let healer =
    Content.hero 2 "Priest" Healer 1000 1000 tap 10000 [ Content.fireball ] { X = 11; Y = 4 }

  let lagging =
    Content.hero 3 "Mage" Dps 1000 1000 tap 10000 [ Content.fireball ] { X = 13; Y = 4 }

  let mob =
    { Content.hero 100 "Scarlet Adept" Dps 300 0 tap 40 [ Content.fireball ] { X = 1; Y = 4 } with
        Faction = Hostile
        Role = None
        Engaged = false }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid =
        Grid.ofRows
          [ "################"
            "################"
            "################"
            "################"
            "#..............#"
            "################"
            "################"
            "################" ]
      Entities = [ tank; healer; lagging; mob ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // The fixture has to have the shape the bug needs, or it proves nothing.
  Assert.True(Pos.chebyshev healer.Pos tank.Pos <= 8, "the healer is not inside the leash")
  Assert.True(Pos.chebyshev lagging.Pos tank.Pos > 8, "the Dps is not past the leash")
  Assert.False(SimState.inCombat w, "the fixture starts in combat")

  let commands = PartyAi.decide w

  let movedWest id =
    commands
    |> List.exists (fun c ->
      match c.Kind with
      | MoveTo(a, p) when a = EntityId id -> p.X < 4
      | _ -> false)

  Assert.True(movedWest 1, "the Tank stood still instead of pulling the pack 3 tiles away")

/// The other half of the same freeze: `waiting` Stops the Tank and used to issue
/// nothing at all for the members past the leash, so the wait could never end — the
/// straggler was never told to move. Nothing here is in combat and no hostile is
/// near, which is exactly the state library L36 sat in for 32000 ticks.
[<Fact>]
let ``a member past the leash walks in to the tank while the party waits`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let tank =
    Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 4; Y = 4 }

  let healer =
    Content.hero 2 "Priest" Healer 1000 1000 tap 10000 [ Content.fireball ] { X = 13; Y = 4 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid =
        Grid.ofRows
          [ "################"
            "################"
            "################"
            "################"
            "#..............#"
            "################"
            "################"
            "################" ]
      Entities = [ tank; healer ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // Past the leash, or the regroup march is not the rule under test.
  Assert.True(Pos.chebyshev healer.Pos tank.Pos > 8, "the healer is not past the leash")
  Assert.False(SimState.inCombat w, "the fixture starts in combat")

  let moves =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | MoveTo(a, p) -> Some(a, p)
      | _ -> None)

  match moves |> List.tryFind (fun (a, _) -> a = EntityId 2) with
  | None -> failwith "the straggler was given no walk, so the wait can never end"
  | Some(_, p) -> Assert.Equal({ X = 5; Y = 4 }, p)

/// A settled ally on the only exit makes the leader's claim-aware route empty, so
/// the leader holds a goal it can never reach and nothing asks the body in front to
/// move. Library L36 sat at (19,125) with a goal 20 tiles away and a settled Priest
/// on (20,125) for the whole cap. Whoever is between the Tank and its goal walks on.
[<Fact>]
let ``a settled ally on the tank's next step is sent on toward the tank's goal`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let tank =
    Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 3; Y = 4 }

  let priest =
    Content.hero 2 "Priest" Healer 1000 1000 tap 10000 [ Content.fireball ] { X = 4; Y = 4 }

  let mob =
    { Content.hero 3 "Scarlet Adept" Dps 300 0 tap 40 [ Content.fireball ] { X = 12; Y = 4 } with
        Faction = Hostile
        Role = None
        Engaged = false }

  // The Tick after the leader's own order landed: a goal, and no route to it.
  let tank = { tank with Goal = Some { X = 12; Y = 4 } }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid =
        Grid.ofRows
          [ "################"
            "################"
            "################"
            "################"
            "#..............#"
            "################"
            "################"
            "################" ]
      Entities = [ tank; priest; mob ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  Assert.False(SimState.inCombat w, "the fixture starts in combat")
  Assert.True(SimState.claimedByOther w tank.Id priest.Pos, "the Priest is not claiming the tile")

  let moves =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | MoveTo(a, p) -> Some(a, p)
      | _ -> None)

  match moves |> List.tryFind (fun (a, _) -> a = EntityId 2) with
  | None -> failwith "the body on the tank's only exit was given no order"
  // Which rule issues the walk does not matter — the follow order may get there
  // first. Clearing the exit is the invariant the corridor needs.
  | Some(_, p) -> Assert.True(p.X > 4, sprintf "the Priest stayed on the exit, heading to %A" p)

/// A Tank mid-file whose next step is held by a settled ally. `stepToward` returns
/// early whenever `Destination` is set, so this Tank never re-plans and never
/// reports a stall: it looks like it is walking — full `Path`, `Destination` set —
/// while it waits on a body that has no orders at all. Library seed 1 parked exactly
/// here (`Warrior 15,125 dest=(16,125) goal=(32,106)`, 30 steps still on file, the
/// Druid on `16,125` itself blocked by a goal-less Mage), and no aside fired,
/// because a Tank with a `Path` and a `Destination` is not "stuck" by the old test.
/// The queue has to drain from its free end.
[<Fact>]
let ``a tank held by a settled ally drains the queue from the back`` () =
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let tank =
    { Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor ] { X = 5; Y = 4 } with
        Goal = Some { X = 11; Y = 4 }
        Path = [ { X = 6; Y = 4 } ]
        Destination = Some { X = 6; Y = 4 } }

  let file =
    [ (6, 4), "Druid"; (7, 4), "Mage"; (8, 4), "Rogue"; (9, 4), "Priest" ]
    |> List.mapi (fun i ((x, y), name) ->
      let r = if name = "Priest" then Healer else Dps
      Content.hero (2 + i) name r 1000 1000 tap 10000 [ Content.fireball ] { X = x; Y = y })

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid =
        Grid.ofRows
          [ "##############"
            "##############"
            "##############"
            "##############"
            "##.........###"
            "##############"
            "##############" ]
      Entities = tank :: file
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let moveFor id =
    PartyAi.decide w
    |> List.tryPick (fun c ->
      match c.Kind with
      | MoveTo(a, p) when a = EntityId id -> Some p
      | _ -> None)

  // The Priest is the only member with floor behind it, so it backs into `10,4`
  // and the file can then shift one tile per tick.
  Assert.Equal(Some { X = 10; Y = 4 }, moveFor 5)

  // The three between it and the Tank have nowhere to go and must not thrash.
  Assert.Equal(None, moveFor 2)
  Assert.Equal(None, moveFor 3)
  Assert.Equal(None, moveFor 4)

  // The Tank takes no aside order of its own; the file moves out of its way.
  Assert.Equal(None, moveFor 1)

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

/// The canonical snapshot lives beside the project file, never beside the assembly.
/// They look interchangeable and are not: `UPDATE_SNAPSHOTS` used to write the copy in
/// `bin/`, the next build overwrote that copy from this file, and the "refreshed" baseline
/// silently reverted — so the drift test failed again with no code change at all, after
/// passing once. Walk out of `bin/` to the project directory and treat this file as the
/// one source of truth for both reading and writing.
let private snapshotPath () =
  let rec up (d: System.IO.DirectoryInfo) =
    if isNull d then
      System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "snapshots", "bench-30.txt")
    elif System.IO.Directory.GetFiles(d.FullName, "*.fsproj").Length > 0 then
      System.IO.Path.Combine(d.FullName, "snapshots", "bench-30.txt")
    else
      up d.Parent

  up (System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory()))

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
          "balance drift at seed %d in %s\n  was: %s\n  now: %s\nRefresh with UPDATE_SNAPSHOTS=1 dotnet test --filter \"bench snapshot has not drifted\""
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
    RestTicks = ticks 0
    Grid = Grid.create 20 10
    Entities = [ rogue 3 { X = 5; Y = 5 }; rogue 4 { X = 7; Y = 5 }; mob ]
    Pending = []
    Rng = Rng.streamsOf 3UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
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
      RestTicks = ticks 0
      Grid = grid
      Entities = [ blocker; mover ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let after = [ 1 .. 40 ] |> List.fold (fun w _ -> SimTick.step [] w) w
  let posOf id = (after.Entities |> List.find (fun e -> e.Id = id)).Pos

  // It walked up to the blocker...
  Assert.Equal(3, (posOf (EntityId 2)).X)
  // ...and stopped in front of it, because the blocking tile is still taken.
  Assert.Equal(4, (posOf (EntityId 1)).X)

/// The trade. A mover whose next step is a settled ally takes that ally's tile and
/// hands back its own, which is the only way a one-wide corridor lets its leader
/// past somebody with no order to move. Library L36 parked a whole wing this way:
/// the Tank held a goal 20 tiles off with a settled Priest on the only exit, the
/// claim-aware search returned no route, and `stepToward` backed off forever. Only
/// Party trades with Party, and only with somebody that is not the Tank.
[<Fact>]
let ``a settled ally trades tiles with the body that steps into it`` () =
  let grid =
    Grid.ofRows [ "########"; "########"; "#......#"; "########"; "########" ]

  let tap = { Min = 1; Max = 1; Ticks = ticks 1; Range = 1 }

  let mover =
    { Content.hero 2 "Priest" Healer 1000 1000 tap 10000 [] { X = 1; Y = 2 } with
        Goal = Some { X = 6; Y = 2 } }

  let blocker = Content.hero 1 "Mage" Dps 1000 1000 tap 50000 [] { X = 2; Y = 2 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = grid
      Entities = [ blocker; mover ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let after = [ 1 .. 40 ] |> List.fold (fun w _ -> SimTick.step [] w) w
  let posOf id = (after.Entities |> List.find (fun e -> e.Id = id)).Pos

  Assert.Equal(6, (posOf (EntityId 2)).X)
  Assert.True((posOf (EntityId 1)).X < 6, "the blocker never gave ground")

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

/// The extractor's own invariant, asserted on the shipped grid (directive
/// m02457): no floor tile narrows a wing to one tile — a pinch is a floor tile
/// whose only two orthogonal floor neighbours sit opposite each other, so the
/// map is one tile wide on that axis. Every doorway is at least two wide.
[<Fact>]
let ``no Scarlet wing pinches to a one-wide doorway`` () =
  let pinches (grid: Grid) =
    MapsScarlet.floorTiles grid
    |> List.filter (fun p ->
      let fl =
        [ -1, 0; 1, 0; 0, -1; 0, 1 ]
        |> List.map (fun (dx, dy) -> Pos.move dx dy p)
        |> List.filter (fun q -> Grid.isFloor grid q)

      fl.Length = 2
      && (let a, b = fl[0], fl[1] in
          (a.X = b.X && a.Y + b.Y = 2 * p.Y)
          || (a.Y = b.Y && a.X + b.X = 2 * p.X)))

  for wing in Content.scarletWings do
    Assert.True(
      List.isEmpty (pinches (MapsScarlet.wing wing.Name)),
      sprintf "%s still has a one-wide doorway" wing.Name
    )

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

/// Innervate multiplies regeneration rather than handing mana over, so the test is
/// that the aura lands and that one Tick of regeneration pays five times what it
/// would have — which is why the target is told to stop casting.
[<Fact>]
let ``innervate multiplies the target's regeneration`` () =
  let world =
    Content.dungeonAt 12 1UL
    |> Content.withRosterAt 12 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]

  // In combat, or out-of-combat regeneration (a share of the pool, and no
  // Innervate term) would drown the measurement.
  let engaged =
    { world with
        Entities =
          world.Entities
          |> List.map (fun e -> if e.Faction = Hostile then { e with Engaged = true } else e) }

  let druid = engaged.Entities |> List.find (fun e -> e.Name = "Druid")
  let priest = engaged.Entities |> List.find (fun e -> e.Name = "Priest")

  let spent =
    { engaged with
        Entities =
          engaged.Entities
          |> List.map (fun e -> if e.Id = priest.Id then { e with Resource = e.MaxResource / 2 } else e) }

  let after =
    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(druid.Id, "Innervate", priest.Id) }
      spent

  let priestNow = after.Entities |> List.find (fun e -> e.Id = priest.Id)

  Assert.True(
    priestNow.Auras
    |> List.exists (function
      | Innervated _ -> true
      | _ -> false),
    "Innervate should leave an aura on the target, not a lump of mana"
  )

  let ticked (w: World) =
    (SimTick.step [] w).Entities |> List.find (fun e -> e.Id = priest.Id) |> fun e -> e.Resource

  let without = ticked spent - priest.Resource / 1
  let with' = ticked after - priestNow.Resource

  Assert.True(with' > without, sprintf "innervated regen %d should beat plain %d" with' without)

/// Vanilla's five-second rule: a caster that has just spent a spell regenerates
/// the trickle, one that has stopped gets the rate. That is the mechanic the DPS
/// and Innervate rules are both built on.
[<Fact>]
let ``a caster that just cast regenerates less than one that stopped`` () =
  let world =
    Content.dungeonAt 12 1UL
    |> Content.withRosterAt 12 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]

  let engaged =
    { world with
        Entities =
          world.Entities
          |> List.map (fun e -> if e.Faction = Hostile then { e with Engaged = true } else e) }

  let mage = engaged.Entities |> List.find (fun e -> e.Name = "Mage")

  let dryWith (recent: bool) =
    { engaged with
        Entities =
          engaged.Entities
          |> List.map (fun e ->
            if e.Id <> mage.Id then e
            else
              { e with
                  // Empty on purpose: `canAfford` is false, so the Mage cannot refresh
                  // its own five-second rule and the only difference between the two
                  // halves is the aura. Measuring from half a pool would let a cheap
                  // cast re-arm the rule mid-span and blur the comparison.
                  Resource = 0
                  Auras =
                    if recent then Aura.refresh (RecentlyCast(Content.fiveSecondRule)) e.Auras
                    else e.Auras }) }

  // Two seconds, so vanilla's mana tick lands twice inside the span.
  let gain (w: World) =
    let before = w.Entities |> List.find (fun e -> e.Id = mage.Id) |> fun e -> e.Resource

    (SimTick.run 40 w).Entities
    |> List.find (fun e -> e.Id = mage.Id)
    |> fun e -> e.Resource - before

  Assert.True(gain (dryWith false) > gain (dryWith true), "casting should cost regeneration")

/// The AI gives Innervate to a healer first — the party's mana is the healer's
/// mana — and only falls back on a damage caster when no healer is dry.
[<Fact>]
let ``the druid innervates the driest healer before a drier mage`` () =
  let world =
    Content.dungeonAt 12 1UL
    |> Content.withRosterAt 12 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]

  let druid = world.Entities |> List.find (fun e -> e.Name = "Druid")
  let priest = world.Entities |> List.find (fun e -> e.Name = "Priest")
  let mage = world.Entities |> List.find (fun e -> e.Name = "Mage")

  let withMana (priestPct: int) (magePct: int) =
    { world with
        Entities =
          world.Entities
          |> List.map (fun e ->
            if e.Name = "Mage" then { e with Resource = e.MaxResource * magePct / 100; Engaged = true }
            elif e.Name = "Priest" then { e with Resource = e.MaxResource * priestPct / 100 }
            elif e.Faction = Hostile then { e with Engaged = true }
            else e) }

  let targets (w: World) =
    PartyAi.decide w
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(a, n, t) when a = druid.Id && n = "Innervate" -> Some t
      | _ -> None)

  // The Mage is emptier, but the Priest is a healer and dry: the healer wins.
  Assert.Contains(targets (withMana 20 0), fun t -> t = priest.Id)

  // With every healer comfortable the Druid still spends it on the driest bar.
  Assert.Contains(targets (withMana 100 0), fun t -> t = mage.Id)

/// A totem slot holds one totem. Mana Tide is the party's reserve and Mana Spring
/// is the standing trickle, so dropping the tide must *replace* the spring — that
/// is what lets the AI hold the tide for a hard pull without losing the trickle it
/// already paid for.
[<Fact>]
let ``mana tide replaces the mana spring in the same slot`` () =
  // Both water totems are trained above the dungeon band — Mana Spring at 26 and
  // Mana Tide at 40 — so this is a 60 Shaman in a bare world, which is the only
  // place the rule can be observed at all.
  let shaman = Content.partyOf [ "Shaman" ] 60 |> List.head

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ shaman ]
      Pending = []
      Rng = Rng.streamsOf 4UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let cast name w =
    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(shaman.Id, name, shaman.Id) }
      w

  let spring = cast "Mana Spring Totem" w

  let standing (w: World) =
    w.Entities
    |> List.filter (fun e -> e.Owner = Some shaman.Id && e.Totem.IsSome)

  Assert.Equal(1, (standing spring).Length)

  let tide = cast "Mana Tide Totem" spring
  let water = standing tide

  Assert.Equal(1, water.Length)
  Assert.Equal("Mana Tide Totem", water.Head.Name)

/// The leader's job between pulls is to let the whole party drink: the Tank holds
/// and nobody shuffles, because a shuffle beside the first pack wakes it and the
/// pull opens with the healer dry. Rage and Energy are deliberately not waited on
/// — a Warrior's bar is empty by design out of combat, and waiting on it would
/// park the party forever.
[<Fact>]
let ``the leader waits for every mana user to drink, not only the healers`` () =
  let baseWorld =
    Content.dungeonAt 12 1UL
    |> Content.withRosterAt 12 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]

  let tank = baseWorld.Entities |> List.find (fun e -> e.Name = "Warrior")
  let mage = baseWorld.Entities |> List.find (fun e -> e.Name = "Mage")

  let withMageMana (pct: int) (w: World) =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e ->
            if e.Id = mage.Id then { e with Resource = e.MaxResource * pct / 100 } else e) }

  let decisions (w: World) =
    PartyAi.decide w
    |> List.map (fun c -> c.Kind)

  let tankMoves (kinds: CommandKind list) =
    kinds
    |> List.filter (fun k ->
      match k with
      | MoveTo(e, _) | Stop e -> e = tank.Id
      | _ -> false)

  // Followers may close up on the Tank — they are gathering to drink — but no
  // order may send anybody away from it, which is what walking into the next pack
  // at a quarter bar looks like.
  let onlyTowardsTank (kinds: CommandKind list) =
    kinds
    |> List.choose (fun k ->
      match k with
      | MoveTo(_, p) -> Some p
      | _ -> None)
    |> List.forall (fun p -> Pos.chebyshev p tank.Pos <= 3)

  // Dry Mage, no healers dry: the old gate only looked at healers, so this is the
  // case that used to walk the party into the next pack.
  let dry = withMageMana 25 baseWorld
  Assert.False(SimState.inCombat dry, "the wait only applies out of combat")

  let held = decisions dry

  Assert.Contains(held, fun k -> k = Stop tank.Id)

  Assert.True(onlyTowardsTank held, "nobody should walk away from the Tank while the party drinks")

  // Topped up, the Tank is free to go: the gate is the mana, not a permanent
  // stop. A Stop can still appear for a different, transient reason -- a pull's
  // opener is booked or in flight -- so the Tank may be Stopped only when a
  // controlling cast is booked in the same Tick. A bare Stop here is a drink wait.
  let wet = withMageMana 100 baseWorld

  let bareStop =
    decisions wet
    |> List.exists (fun k ->
      match k with
      | Stop e when e = tank.Id ->
        not (
          decisions wet
          |> List.exists (fun k' ->
            match k' with
            | UseAbility(_, n, _) -> n = "Polymorph" || n = "Sap" || n = "Hibernate"
            | _ -> false)
        )
      | _ -> false)

  Assert.False(bareStop)

// ===========================================================================
// Rogue: poisons and the stealth opener
// ===========================================================================

/// The five poisons are not a rotation decision, they are a situational one: the
/// choice is re-made on every landing swing from what the target is doing rather
/// than picked once per dungeon. A healer is worth cutting, a caster worth
/// slowing, a runner worth snaring, and anything else worth plain damage.
[<Fact>]
let ``the rogue's poison matches the situation`` () =
  let world =
    Content.dungeonAt 24 1UL
    |> Content.withRosterAt 24 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]

  let hostiles = world.Entities |> List.filter (fun e -> e.Faction = Hostile)

  let healer =
    hostiles
    |> List.tryFind (fun h ->
      h.Abilities
      |> List.exists (fun a ->
        a.Effects |> List.exists (function Heal _ | HealOverTime _ -> true | _ -> false)))

  Assert.True(healer.IsSome, "the content has a healing hostile to test Wound Poison against")
  Assert.Equal(SimCombat.WoundPoison, fst (SimCombat.poisonPlan healer.Value hostiles))

  // With no other hostile in the list nothing is near a healer, so the main hand
  // defaults to plain damage: the poison that costs the target no debuff slot.
  let isHealer (h: Entity) =
    h.Abilities
    |> List.exists (fun a ->
      a.Effects |> List.exists (function Heal _ | HealOverTime _ -> true | _ -> false))

  let plain = hostiles |> List.find (fun h -> h.Casting.IsNone && not (isHealer h))
  Assert.Equal(SimCombat.InstantPoison, fst (SimCombat.poisonPlan plain []))
  Assert.Equal(SimCombat.DeadlyPoison, snd (SimCombat.poisonPlan plain []))

  let running = { plain with Health = plain.MaxHealth / 5 }
  Assert.Equal(SimCombat.CripplingPoison, fst (SimCombat.poisonPlan running []))

/// Two weapons, two poison slots. Coating both blades with the same vial throws a
/// slot away, so the plan must always name two different poisons — the situation's
/// first, and for the off hand the best one the main hand is not already using.
[<Fact>]
let ``the rogue coats both blades and never with the same poison`` () =
  let world =
    Content.dungeonAt 24 1UL
    |> Content.withRosterAt 24 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]

  let hostiles = world.Entities |> List.filter (fun e -> e.Faction = Hostile)

  for victim in hostiles do
    let main, off = SimCombat.poisonPlan victim hostiles
    Assert.NotEqual<SimCombat.Poison>(main, off)

  // The Rogue is the one carrying them, and it swings both hands.
  let rogue = world.Entities |> List.find (fun e -> e.Faction = Party && e.ResourceKind = Energy)
  Assert.True(SimCombat.carriesPoison rogue)
  Assert.True(rogue.OffHand, "the Rogue needs an off-hand for the second poison to be worth anything")

/// Wound Poison is the only thing in the model that makes healing itself weaker
/// rather than cheaper, so it has to bite in `healEntity` — every heal in the game
/// routes through there, from a Flash Heal to a potion to a resurrection.
[<Fact>]
let ``wound poison cuts the healing a target receives`` () =
  let world =
    Content.dungeonAt 24 1UL |> Content.withRosterAt 24 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]

  let victim = world.Entities |> List.find (fun e -> e.Faction = Hostile)

  // Five stacks at 15% each: a 1000 point heal lands as 250.
  let wounded =
    { victim with
        Health = 0
        MaxHealth = 10000
        Auras = [ Wounded(ticks 200, 1500, 5) ] }

  let world =
    { world with
        Entities =
          world.Entities |> List.map (fun e -> if e.Id = wounded.Id then wounded else e) }

  let healed = SimState.healEntity wounded.Id 1000 world

  Assert.Equal(
    250,
    (healed.Entities |> List.find (fun e -> e.Id = wounded.Id)).Health
  )

/// Stealth is not a state in this model: an untouched enemy is the one the Rogue
/// met from behind. Ambush is the first blow of a pull and nothing else, which is
/// why it is excluded from the strike rotation — left in, its 250% would win every
/// damage comparison and be spent as though stealth were free.
[<Fact>]
let ``the rogue opens a pull with Ambush and only then`` () =
  let world0 =
    Content.dungeonAt 24 1UL
    |> Content.withRosterAt 24 [ "Warrior"; "Priest"; "Druid"; "Mage"; "Rogue" ]

  let rogue = world0.Entities |> List.find (fun e -> e.Name = "Rogue")
  let warrior = world0.Entities |> List.find (fun e -> e.Name = "Warrior")

  Assert.Contains("Ambush", rogue.Abilities |> List.map (fun a -> a.Name))

  // The opener picks the healthiest untouched enemy in reach, so the mob to stand
  // next to is the healthiest hostile in the wing.
  let mob =
    world0.Entities
    |> List.filter (fun e -> e.Faction = Hostile)
    |> List.sortByDescending (fun e -> e.MaxHealth)
    |> List.head

  // `touched` wounds every hostile: once nothing in the wing is fresh, the window
  // for an opener is shut for the whole pull, not just for one mob.
  let world (touched: bool) =
    { world0 with
        Entities =
          world0.Entities
          |> List.map (fun e ->
            if e.Id = rogue.Id then
              { e with
                  Pos = { X = mob.Pos.X + 1; Y = mob.Pos.Y }
                  Resource = e.MaxResource }
            elif e.Id = warrior.Id then
              // The Party assists the Tank, so the Tank is what puts a mob on
              // everyone's target list.
              { e with Target = Some mob.Id }
            elif e.Faction = Hostile then
              { e with
                  Engaged = true
                  Target = Some warrior.Id
                  Health = if touched then e.MaxHealth / 2 else e.MaxHealth
                  Auras = if touched then [ Sundered(ticks 200, 3) ] else [] }
            else
              e) }

  let rogueOpens (w: World) =
    PartyAi.decide w
    |> List.exists (fun c ->
      match c.Kind with
      | UseAbility(actor, name, _) -> actor = rogue.Id && name = "Ambush"
      | _ -> false)

  Assert.True(rogueOpens (world false), "an untouched enemy is a stealth opener")
  Assert.False(rogueOpens (world true), "once the pull is under way there is no opener")

/// A Druid form is a stance, not a buff: it carries no clock and one replaces
/// the next, so the aura layer is where both facts have to hold.
[<Fact>]
let ``a Druid form has no clock and one form replaces another`` () =
  let bear = Shaped BearForm
  let cat = Shaped CatForm

  // No clock: ageing it a thousand times leaves it exactly where it was.
  let aged =
    List.fold (fun a _ -> Aura.advance a |> Option.defaultValue (Shaped MoonkinForm)) bear [ 1 .. 1000 ]
  Assert.Equal<Stance option>(Some BearForm, Aura.stanceOf aged)

  // One at a time is what `kind` is for: a new form refreshes the same slot.
  Assert.Equal(Aura.kind bear, Aura.kind cat)
  Assert.Equal(Aura.kind bear, Aura.kind (Shaped TreeForm))

  // Bear and Cat are the feral forms, and the point of them is that a body in
  // one cannot cast. Tree and Moonkin are caster forms and must not be silenced.
  Assert.True(Aura.preventsCasting (Shaped BearForm))
  Assert.True(Aura.preventsCasting (Shaped CatForm))
  Assert.False(Aura.preventsCasting (Shaped TreeForm))
  Assert.False(Aura.preventsCasting (Shaped MoonkinForm))

[<Fact>]
let ``shifting into Cat Form holds, and Bear Form locks the caster out`` () =
  let w = Content.gully 1UL
  let druid = w.Entities |> List.find (fun e -> e.Name = "Druid")

  // The forms are content but not yet in a kit: the AI policy that picks one is
  // a later step, so the test hands the Druid the buttons itself.
  let w =
    SimState.mapEntity druid.Id (fun e ->
      { e with
          Abilities =
            [ Content.catForm; Content.bearForm; Content.treeOfLife ] @ e.Abilities }) w

  let shift name =
    SimCombat.applyCommand { ApplyAt = ticks 1; Kind = UseAbility(druid.Id, name, druid.Id) } w

  let stanceAfter (world: World) =
    world.Entities |> List.find (fun e -> e.Id = druid.Id) |> SimState.stance

  // The healer Druid spawns as a tree, so the body starts in a form rather than
  // in its own shape; what is still true is that a caster form can cast.
  Assert.Equal<Stance option>(Some TreeForm, stanceAfter w)
  Assert.True((w.Entities |> List.find (fun e -> e.Id = druid.Id) |> SimState.canCast), "a tree casts")

  let w = shift "Cat Form"
  Assert.Equal<Stance option>(Some CatForm, stanceAfter w)

  // Still a cat a long time later: nothing ages a form away.
  let later = SimTick.run 400 w
  Assert.Equal<Stance option>(Some CatForm, stanceAfter later)

  let w = shift "Bear Form"
  Assert.Equal<Stance option>(Some BearForm, stanceAfter w)

  // One form, not two: the cat was dropped, not stacked under the bear.
  let bear = w.Entities |> List.find (fun e -> e.Id = druid.Id)
  Assert.Equal(1, bear.Auras |> List.filter (Aura.stanceOf >> Option.isSome) |> List.length)
  Assert.False(SimState.canCast bear, "a bear cannot cast")

  // And a caster form gives the spellbook back.
  let tree = shift "Tree of Life"
  Assert.True(SimState.canCast (tree.Entities |> List.find (fun e -> e.Id = druid.Id)))

/// The Druid is the one class that fills all three roles, and which one it fills
/// is the roster string rather than the class: `"Druid"` alone stays the healer
/// so every existing roster and ladder cell keeps its meaning. All four specs are
/// still `Name = "Druid"`, so art, logs and the tests that find the class keep
/// working.
[<Fact>]
let ``a Druid's roster string picks its spec, role and opening form`` () =
  let druid name =
    match Content.partyOf [ name ] 24 with
    | [ e ] -> e
    | other -> failwithf "expected one party member, got %d" (List.length other)

  let check name role stance =
    let e = druid name
    Assert.Equal("Druid", e.Name)
    Assert.Equal<Role option>(Some role, e.Role)
    Assert.Equal<Stance option>(Some stance, SimState.stance e)

  check "Druid" Healer TreeForm
  check "Druid (Bear)" Tank BearForm
  check "Druid (Cat)" Dps CatForm
  check "Druid (Moonkin)" Dps MoonkinForm

  // A passive is the class's, not the spec's: a spec'd Druid still brings Mark of
  // the Wild, which is the one place a roster string is read for its class alone.
  for name in [ "Druid"; "Druid (Bear)"; "Druid (Cat)"; "Druid (Moonkin)" ] do
    Assert.Contains(MarkOfTheWild, Content.partyBuffsFor [ name ])

[<Fact>]
let ``a Bear Druid cannot cast and a Tree Druid can`` () =
  let bear = Content.partyOf [ "Druid (Bear)" ] 24 |> List.head
  let tree = Content.partyOf [ "Druid" ] 24 |> List.head
  Assert.False(SimState.canCast bear, "a bear is claws and hide, not a caster")
  Assert.True(SimState.canCast tree, "the healer's form is a caster form")

[<Fact>]
let ``each Druid spec is paid for from its own resource`` () =
  let druid name = Content.partyOf [ name ] 24 |> List.head
  Assert.Equal(Rage, (druid "Druid (Bear)").ResourceKind)
  Assert.Equal(Energy, (druid "Druid (Cat)").ResourceKind)
  Assert.Equal(Mana, (druid "Druid").ResourceKind)
  Assert.Equal(Mana, (druid "Druid (Moonkin)").ResourceKind)

  // A Rage and an Energy pool are a flat 100, the Warrior's and the Rogue's own.
  Assert.Equal(100, (druid "Druid (Bear)").MaxResource)
  Assert.Equal(100, (druid "Druid (Cat)").MaxResource)

  // A tank holds threat at its form or stance rate times its threat talent, and a
  // damage spec at the party rate. The Bear and the Warrior are *not* equal: the
  // Warrior's product is Defensive Stance 1.30 x Defiance 1.20 and the Bear's is
  // the form's 1.50 x Feral Instinct 1.15. Both constants document that split in
  // `Content.fs`; before them both tanks sat on a flat 5.0 placeholder.
  let warrior = Content.partyOf [ "Warrior" ] 24 |> List.head
  Assert.Equal(10000, (druid "Druid").ThreatMultiplierBp)
  Assert.Equal(17250, (druid "Druid (Bear)").ThreatMultiplierBp)
  Assert.Equal(15600, warrior.ThreatMultiplierBp)
  // Subtlety 5/5 is the one Moonkin pick that lands on the hero rather than on
  // `Talents`: 20% of the caster's spell threat is gone, so it reads 8000 where the
  // Tree (no Subtlety) reads the party default.
  Assert.Equal(8000, (druid "Druid (Moonkin)").ThreatMultiplierBp)
  Assert.NotEqual((druid "Druid").ThreatMultiplierBp, (druid "Druid (Moonkin)").ThreatMultiplierBp)

[<Fact>]
let ``the Tree of Life is the healer's form alone`` () =
  // The operator's rule, as recorded at `formsOf` (Content.fs:5259): Tree of Life
  // belongs to the healer; a damage spec — the cat above all, whose durability a
  // Tree shift would tempt — carries neither the shift ability nor the form name.
  // Locked from both sides because the Moonkin pass will edit this file: the kit
  // gate (no `TakeStance TreeForm` effect, which is the only route a body has into
  // a stance) and the forms gate (`druidForms`, which a shifting AI reads).
  let carriesTheShift name =
    Content.partyOf [ name ] 24 |> List.head
    |> fun e -> e.Abilities |> List.exists (fun a -> a.Effects |> List.contains (TakeStance TreeForm))

  Assert.True(carriesTheShift "Druid", "the healer's kit carries the Tree of Life")

  for name in [ "Druid (Bear)"; "Druid (Cat)"; "Druid (Moonkin)" ] do
    Assert.False(
      carriesTheShift name,
      sprintf "a %s must not carry the Tree of Life shift" name
    )

    Assert.DoesNotContain(TreeForm, Content.druidForms name)


[<Fact>]
let ``Moonkin is a damage caster and carries no heal`` () =
  let names =
    Content.partyOf [ "Druid (Moonkin)" ] 24
    |> List.head
    |> fun e -> e.Abilities |> List.map (fun a -> a.Name)

  // The point of the spec: a caster dps that can heal turns every pull into a
  // stalemate, so none of the healer's spells may be in the kit.
  for heal in [ "Healing Touch"; "Regrowth"; "Rejuvenation"; "Swiftmend"; "Rebirth" ] do
    Assert.DoesNotContain(heal, names)

  for spell in [ "Wrath"; "Moonfire"; "Entangling Roots"; "Moonkin Form" ] do
    Assert.Contains(spell, names)

[<Fact>]
let ``a Cat Druid's kit is the druid's rows, not the Rogue's`` () =
  let cat = Content.partyOf [ "Druid (Cat)" ] 60 |> List.head
  let names = cat.Abilities |> List.map (fun a -> a.Name)
  Assert.Equal(0, cat.ComboPoints)

  for spell in [ "Claw"; "Shred"; "Ravage"; "Rake"; "Rip"; "Ferocious Bite"; "Cat Form" ] do
    Assert.Contains(spell, names)

  // Costs are the druid's: Claw 45, Shred 60, Ferocious Bite 35. The Rogue's
  // pair it used to borrow cost 40 and 60, and Eviscerate 35, so the first and
  // the third are where a leak would show. Shred's 60 is read after the build has
  // been folded in, and Improved Shred 2/2 takes 20% of it — 60 * 8000 / 10000.
  let ability name = cat.Abilities |> List.find (fun a -> a.Name = name)
  Assert.Equal(45, (ability "Claw").ResourceCost)
  Assert.Equal(48, (ability "Shred").ResourceCost)
  Assert.Equal(35, (ability "Ferocious Bite").ResourceCost)

  // 225% weapon damage on Shred is the ranks table's multiplier, and it is the
  // druid's number: the Rogue's Backstab row is 150%, so a copy would read 15000.
  let weaponBp (name: string) =
    (ability name).Effects
    |> List.pick (function
      | WeaponDamage(bp, _) -> Some bp
      | _ -> None)

  Assert.Equal(22500, weaponBp "Shred")
  Assert.Equal(10000, weaponBp "Claw")

/// The cat's build, read off the hero it was folded into. The tree is the operator's
/// URL decode — Feral 36, Balance 11, Restoration 4 — and the five numbers below are
/// the five picks of it that this model can express.
[<Fact>]
let ``the cat's build lands on the hero and the bear's does not move`` () =
  let cat = Content.partyOf [ "Druid (Cat)" ] 60 |> List.exactlyOne
  let bear = Content.partyOf [ "Druid (Bear)" ] 60 |> List.exactlyOne

  // Natural Weapons 3/3: +10% physical in every form.
  Assert.Equal(1000, cat.Talents.DamageDoneBp)

  let spell name =
    cat.Talents.Spells |> List.find (fun s -> s.Spell = name)

  // Predatory Strikes 3/3 (+20% Claw), Feral Aggression 5/5 (+15% Ferocious Bite),
  // Improved Shred 2/2 (+10% Shred and 20% off its 60 Energy, which is 12).
  Assert.Equal(2000, (spell "Claw").DamageBp)
  Assert.Equal(1500, (spell "Ferocious Bite").DamageBp)
  Assert.Equal(1000, (spell "Shred").DamageBp)
  Assert.Equal(2000, (spell "Shred").CostBp)
  Assert.Equal(3, cat.Talents.Spells.Length)

  // Sharpened Claws 3/3 (+6% crit) and Feral Swiftness 2/2 (+4% dodge) are traits,
  // so they show up as crit and dodge over the bear, which is the one other feral
  // sheet to compare against. The bear's stat line carries six more Agility than
  // the cat's, and at ten crit and five dodge per point that is 60 and 30 of the
  // gap: 600 - 60 = 540 and 400 - 30 = 370.
  Assert.Equal(540, cat.CritChanceBp - bear.CritChanceBp)
  Assert.Equal(370, cat.DodgeChanceBp - bear.DodgeChanceBp)

  // And the bear is untouched: Primal Fury and nothing else, no damage line, no
  // spell line. The two builds share a class and no points.
  Assert.Empty(bear.Talents.Spells)
  Assert.Equal(0, bear.Talents.DamageDoneBp)
  Assert.Equal("Primal Fury", (bear.Talents.Procs |> List.exactlyOne).Talent)

/// A Rake is a real bleed: 40 Energy, and a nine-second `Bleeding` at rank 4's
/// stated 96 total, which is 10 a tick in whole points (90 of the 96 — the six
/// points integer division costs, which the row's comment records).
[<Fact>]
let ``a Rake lands the bleed it states and lets it expire`` () =
  let cat =
    Content.partyOf [ "Druid (Cat)" ] 60
    |> List.head
    |> fun e ->
      { e with
          Id = EntityId 3
          Pos = { X = 5; Y = 4 }
          Resource = e.MaxResource }

  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> fun w -> { w with Entities = w.Entities @ [ cat ] }

  let rake =
    cat.Abilities |> List.find (fun a -> a.Name = "Rake")

  let w = SimCombat.resolveAbility (EntityId 3) (EntityId 100) rake w
  let bleed = (mobOf w).Auras |> List.exactlyOne
  Assert.Equal(10, Aura.flatDamagePerTick bleed)
  Assert.Equal(secTicks 9.0, Aura.remaining bleed)

  // One tick pays ten, straight: a bleed is unmitigated by armour, and the tick
  // path only lets Shield Wall's aura take a share.
  let before = (mobOf w).Health
  let ticked = SimTick.advanceAuraDamage w
  Assert.Equal(before - 10, (mobOf ticked).Health)

  // Nine seconds of decay and it is gone; nothing else refreshes it because no
  // party AI ran.
  let expired =
    (w, [ 1 .. int (secTicks 9.0) ])
    ||> List.fold (fun w _ -> SimTick.advanceAuras w)

  Assert.Empty((mobOf expired).Auras |> List.filter (function Bleeding _ -> true | _ -> false))

/// Rip is the finisher that pays in bleed instead of in a hit, and its total is
/// linear in the points it spends: rank 5 is 270 at one point and 168 a point, so
/// 942 over twelve seconds at five — 22 a tick against 78.
[<Fact>]
let ``a Rip's bleed scales with the combo points it spends`` () =
  let ripAt (points: int) =
    let cat =
      Content.partyOf [ "Druid (Cat)" ] 60
      |> List.head
      |> fun e ->
        { e with
            Id = EntityId 3
            Pos = { X = 5; Y = 4 }
            ComboPoints = points
            Resource = e.MaxResource }

    let w =
      arena 0 0 { X = 7; Y = 5 }
      |> fun w -> { w with Entities = w.Entities @ [ cat ] }

    let rip = cat.Abilities |> List.find (fun a -> a.Name = "Rip")
    let w = SimCombat.resolveAbility (EntityId 3) (EntityId 100) rip w
    Aura.flatDamagePerTick ((mobOf w).Auras |> List.exactlyOne), w

  let one, _ = ripAt 1
  let five, after = ripAt 5
  Assert.Equal(22, one)
  Assert.Equal(78, five)

  // The points are spent, which is what makes it a finisher rather than a second
  // builder: 78 is the fifth point's bleed and it is not still banked.
  Assert.Equal(0, (after.Entities |> List.find (fun e -> e.Id = EntityId 3)).ComboPoints)

/// Ferocious Bite is the druid's row, not the Rogue's Eviscerate under a feral
/// name. Eviscerate bakes Improved Eviscerate 3/3 into every term (`lo * 115 /
/// 100`), so a copy would read 228-297 with a step of 169 at rank 5; the ranks
/// table's druid numbers are 199-259 with a step of 147.
[<Fact>]
let ``Ferocious Bite does not pay the Rogue's Improved Eviscerate`` () =
  let cat = Content.partyOf [ "Druid (Cat)" ] 60 |> List.exactlyOne

  let bite =
    cat.Abilities |> List.find (fun a -> a.Name = "Ferocious Bite")

  Assert.Equal(1, bite.Effects.Length)

  let lo, hi, step =
    match bite.Effects with
    | [ Finisher(lo, hi, step) ] -> lo, hi, step
    | _ -> 0, 0, 0

  Assert.Equal((199, 259, 147), (lo, hi, step))

  // Five points: 199 + 147 * 4 = 787. The Rogue's baked row would read 228 + 169 *
  // 4 = 904, so the difference is the talent, and the single effect above is also
  // why Ruthlessness is not here: Eviscerate appends a combo point after its
  // finisher, and a finisher that builds a point is a different ability.
  Assert.Equal(787, lo + step * 4)

/// Berserk's cat half is Adrenaline Rush's shape: a 100% energy-regeneration
/// increase for twenty seconds, on the payload's 6 min cooldown. A drained bar
/// refills one point every two ticks, so it takes 200 ticks to reach 100 — and 100
/// ticks while the buff is up, because the regen line pays `base + base * bp /
/// 10000` and 10000 bp is exactly double.
[<Fact>]
let ``Berserk doubles the cat's energy regeneration for twenty seconds`` () =
  let catWorld (berserk: bool) =
    let cat =
      Content.partyOf [ "Druid (Cat)" ] 60
      |> List.head
      |> fun e -> { e with Id = EntityId 3; Pos = { X = 5; Y = 4 } }

    let w =
      arena 0 0 { X = 7; Y = 5 }
      |> fun w -> { w with Entities = w.Entities @ [ cat ] }

    if not berserk then
      w
    else
      let ability = cat.Abilities |> List.find (fun a -> a.Name = "Berserk")
      SimCombat.resolveAbility (EntityId 3) (EntityId 3) ability w

  let drained w =
    w |> SimState.mapEntity (EntityId 3) (fun e -> { e with Resource = 0 })

  let ticksToCap (w: World) =
    let cap =
      (w.Entities |> List.find (fun e -> e.Id = EntityId 3)).MaxResource

    let rec go (n: int) (w: World) =
      let w =
        { w with Tick = w.Tick + ticks 1 } |> SimTick.advanceResource

      if (w.Entities |> List.find (fun e -> e.Id = EntityId 3)).Resource >= cap then
        n
      else
        go (n + 1) w

    go 1 w

  Assert.Equal(200, ticksToCap (drained (catWorld false)))
  Assert.Equal(100, ticksToCap (drained (catWorld true)))

  // And the rate is back to normal when the twenty seconds are up: the same
  // drained bar takes 200 ticks again, which is the assertion that the buff is a
  // duration and not a permanent doubling.
  let lapsed =
    (catWorld true, [ 1 .. int (secTicks 20.0) ])
    ||> List.fold (fun w _ -> SimTick.advanceAuras w)

  Assert.Equal(200, ticksToCap (drained lapsed))

/// Berserk's bear half is Last Stand's shape: a fifth more maximum health for
/// twenty seconds, and the health is lost when it lapses — the clamp to the
/// shrunken maximum is the whole clause, and it is what stops the cooldown being
/// a heal.
[<Fact>]
let ``Berserk's bear half raises the maximum health and takes it back`` () =
  let bear =
    Content.partyOf [ "Druid (Bear)" ] 60
    |> List.head
    |> fun e -> { e with Id = EntityId 3; Pos = { X = 5; Y = 4 } }

  let w =
    arena 0 0 { X = 7; Y = 5 }
    |> fun w -> { w with Entities = w.Entities @ [ bear ] }

  let berserk =
    bear.Abilities |> List.find (fun a -> a.Name = "Berserk")

  // Three minutes, DERIVED rather than read: the talent payload's tooltip carries
  // no cooldown, and the two abilities the operator named as the model for each
  // half — Adrenaline Rush and Last Stand — are both three-minute cooldowns.
  Assert.Equal(secTicks 180.0, berserk.CooldownTicks)

  let before = bear.MaxHealth
  let up = SimCombat.resolveAbility (EntityId 3) (EntityId 3) berserk w

  let buffed =
    up.Entities |> List.find (fun e -> e.Id = EntityId 3)

  Assert.Equal(before + before * 2000 / 10000, buffed.MaxHealth)
  Assert.Equal(buffed.MaxHealth, buffed.Health)

  // Twenty seconds later the maximum is the old one and the body is not left
  // above it: a full-health bear reads full health again, not 120%.
  let lapse (w: World) =
    (w, [ 1 .. int (secTicks 20.0) ])
    ||> List.fold (fun w _ -> SimTick.advanceAuras w)

  let after =
    (lapse up).Entities |> List.find (fun e -> e.Id = EntityId 3)

  Assert.Equal(before, after.MaxHealth)
  Assert.Equal(before, after.Health)

  // A body wounded while buffed keeps the damage it took, and the two cases differ
  // in which number the lapse lands on. A hundred off the buffed 5259 is 5159,
  // still above the old 4383, so the clamp takes the whole 876 the buff granted
  // (Heart of the Wild's Stamina leg raised the old 3873 to 4383, and the grant
  // grew with it); a hit of 976 lands at 4283, under the old maximum, and is
  // carried across the lapse untouched. Either way the buff is not a heal that
  // banked the difference.
  let lapseHealth (amount: int) =
    (lapse (up |> SimCombat.dealDamage (EntityId 3) amount)).Entities
    |> List.find (fun e -> e.Id = EntityId 3)

  let wounded =
    (up |> SimCombat.dealDamage (EntityId 3) 100).Entities
    |> List.find (fun e -> e.Id = EntityId 3)

  Assert.Equal(buffed.MaxHealth - 100, wounded.Health)
  Assert.Equal(before, (lapseHealth 100).Health)
  Assert.Equal(before - 100, (lapseHealth 976).Health)
  Assert.Equal(before, (lapseHealth 976).MaxHealth)

/// Thick Hide 3/3 — "Increases your Armor contribution from items by [3/6/10]%"
/// (`.pi/research/octowow/druid-feral.md` §4) — folds with Bear Form's own
/// "+180% armour from items": the form is `Traits.ItemArmorBp = 18000`, the
/// talent is `Talents.ItemArmorBp = 1000`, and the two land on the gear's own
/// share together in `derive` — the flat 799 the old pin carried is gone, and
/// the fold grows with the gear instead of stopping at the level-24 set.
[<Fact>]
let ``Thick Hide folds with Bear Form on the gear's own armour`` () =
  let bear = Content.partyOf [ "Druid (Bear)" ] 60 |> List.head

  // The Tier set's 2018, x2.9 = 5852 — the gear's share alone, the number the
  // Tree of Life site would read on a druid that had one.
  Assert.Equal(5852, bear.ItemArmor)
  // Mark of the Wild's 105 and Agility 167's two-per-point (334) on top.
  Assert.Equal(6291, bear.Armor)

/// Heart of the Wild 5/5 — "+20% Intellect" for every form, "while in Bear or
/// Dire Bear Form your Stamina is increased by [..20]%", "while in Cat Form
/// your Strength is increased by [..20]%" (`.pi/research/octowow/druid-feral.md`
/// §4): one talent, three legs, and the per-form pair is the split between the
/// two feral builds' records — the bear carries the Stamina leg, the cat the
/// Strength one, and both carry the Intellect fifth.
[<Fact>]
let ``Heart of the Wild folds the bear's Stamina and the cat's Strength`` () =
  let bear = Content.partyOf [ "Druid (Bear)" ] 60 |> List.head
  let cat = Content.partyOf [ "Druid (Cat)" ] 60 |> List.head

  // Stamina 257 buffed (253 dressed, Mark of the Wild's 4) folds to 308: the
  // pool pays 510 more than the leg-less fold, and the level-60 base 1483 makes
  // it 4383.
  Assert.Equal(4383, bear.MaxHealth)
  // Strength 117 buffed folds to 140, and the cat's attack power is twice that
  // plus 161 Agility (441) with Predatory Strikes' +10% on top: 485, which the
  // 2.0 s swing carries as 485 x 40 / 280 = 69.
  Assert.Equal(69, cat.MeleeDamageBonus)

/// Predatory Strikes 3/3 — "Increases your melee attack power in Cat, Bear,
/// and Dire Bear Forms by [3/6/10]%" (`.pi/research/octowow/druid-feral.md` §4).
/// Measured on the bear, whose build carries no Strength leg: the +10% is the
/// only fold in the number.
[<Fact>]
let ``Predatory Strikes folds the bear's attack power`` () =
  let bear = Content.partyOf [ "Druid (Bear)" ] 60 |> List.head

  // Strength 137 buffed buys two a point (274), the talent's +10% makes 301, and
  // the 2.4 s swing carries 301 x 48 / 280 = 51.
  Assert.Equal(51, bear.MeleeDamageBonus)

[<Fact>]
let ``a Druid's forms are gated by its spec`` () =
  let forms name = Content.druidForms name |> List.map Stances.name |> List.sort

  // Tree of Life is the healer's, Moonkin is the caster's; Bear and Cat are open
  // to every role, and every role keeps the humanoid shape it was born in.
  Assert.Equal<string list>([ "bear"; "cat"; "humanoid"; "tree" ], forms "Druid")
  Assert.Equal<string list>([ "bear"; "cat"; "humanoid"; "moonkin" ], forms "Druid (Moonkin)")
  Assert.Equal<string list>([ "bear"; "cat"; "humanoid" ], forms "Druid (Bear)")
  Assert.Equal<string list>([ "bear"; "cat"; "humanoid" ], forms "Druid (Cat)")

  // A class that is not a Druid has no forms, and no name it does not know is
  // silently given the healer's.
  Assert.Empty(Content.druidForms "Warrior")

[<Fact>]
let ``every Druid spawns in a form its spec may take`` () =
  for name in [ "Druid"; "Druid (Bear)"; "Druid (Cat)"; "Druid (Moonkin)" ] do
    let body = Content.partyOf [ name ] 60 |> List.head
    let spawn = body.Auras |> List.choose Aura.stanceOf |> List.exactlyOne
    Assert.Contains(spawn, Content.druidForms name)

[<Fact>]
let ``shifting to humanoid leaves no form behind`` () =
  let w =
    Content.scarletMonasteryAt "graveyard" Content.Median 1UL
    |> Content.withRosterAt 24 [ "Warrior"; "Druid (Bear)"; "Mage"; "Rogue"; "Warlock" ]

  let bear = SimState.aliveParty w |> List.find (fun e -> e.Name = "Druid")
  Assert.Equal<Stance option>(Some BearForm, SimState.stance bear)

  let w = SimCombat.resolveAbility bear.Id bear.Id Content.humanForm w
  let after = SimState.aliveParty w |> List.find (fun e -> e.Id = bear.Id)
  Assert.Equal<Stance option>(None, SimState.stance after)

/// Vanilla's one form restriction this model honours: Tree of Life forbids
/// Healing Touch, so the spell is a shape decision and not a button.
[<Fact>]
let ``Healing Touch is refused in the tree and starts from the human form`` () =
  let w =
    Content.scarletMonasteryAt "graveyard" Content.Median 1UL
    |> Content.withRosterAt 24 [ "Warrior"; "Druid"; "Mage"; "Rogue"; "Warlock" ]

  let druid = SimState.aliveParty w |> List.find (fun e -> e.Name = "Druid")
  let tank = SimState.aliveParty w |> List.find (fun e -> e.Name = "Warrior")
  Assert.Equal<Stance option>(Some TreeForm, SimState.stance druid)

  let heal (w: World) =
    SimCombat.applyCommand { ApplyAt = ticks 1; Kind = UseAbility(druid.Id, "Healing Touch", tank.Id) } w

  let caster (w: World) =
    w.Entities |> List.find (fun e -> e.Id = druid.Id)

  // The tree refuses it: no cast starts, so nothing was ever on its way.
  let refused = heal w
  Assert.True((caster refused).Casting.IsNone, "a tree cannot start Healing Touch")

  // Out of the form, the same button starts the same cast.
  let w = SimCombat.resolveAbility druid.Id druid.Id Content.humanForm w
  Assert.Equal<Stance option>(None, SimState.stance (caster w))
  let started = heal w

  match (caster started).Casting with
  | Some cast -> Assert.Equal("Healing Touch", cast.Ability.Name)
  | None -> failwith "Healing Touch did not start in human form"

/// The behaviour the operator asked for: a healer that lives in the tree, leaves
/// it for the one spell the tree forbids, and gives the form back afterwards.
/// Asserted on `SimState.stance` rather than on the log, so the shape at each
/// moment is the fact being checked.
[<Fact>]
let ``the Tree Druid shifts out for Healing Touch and back into the tree`` () =
  let party = Content.partyOf [ "Warrior"; "Druid" ] 24
  let tank = { party.[0] with Pos = { X = 5; Y = 5 }; Health = party.[0].MaxHealth * 2 / 5 }
  let druid = { party.[1] with Pos = { X = 5; Y = 5 } }
  // Engaged but barely scratching: the script only heals once there is a fight
  // on, and nothing here should outrun the sequence it is being watched for.
  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let enemy =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 4000 2000 (Some tap) [] None { X = 6; Y = 5 } with
        Engaged = true
        Target = Some tank.Id }

  let w0 =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; druid; enemy ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  /// The Druid's shape, its cast, and what the script asked it to press.
  let rec play (w: World) n acc =
    if n <= 0 then
      w, List.rev acc
    else
      let cmds = Bench.autoPilot w

      let asked =
        cmds
        |> List.choose (fun c ->
          match c.Kind with
          | UseAbility(src, name, target) when src = druid.Id -> Some(name, target)
          | _ -> None)

      let w = SimTick.step cmds w
      let d = w.Entities |> List.find (fun e -> e.Id = druid.Id)
      let casting = d.Casting |> Option.map (fun c -> c.Ability.Name)
      play w (n - 1) ((SimState.stance d, casting, asked) :: acc)

  let played, seen = play w0 120 []

  let describe (ticks: (Stance option * string option * (string * EntityId) list) list) =
    ticks
    |> List.indexed
    |> List.filter (fun (_, (_, casting, asked)) -> casting.IsSome || not asked.IsEmpty)
    |> List.truncate 20
    |> sprintf "%A"

  // Out of the tree first: the form is what the heal has to be paid with.
  let shifted =
    seen
    |> List.indexed
    |> List.tryFind (fun (_, (stance, _, asked)) -> stance = None && asked |> List.exists (fun (n, _) -> n = "Human Form"))

  Assert.True(shifted.IsSome, "the Druid never shifted out of the tree: " + describe seen)

  // Healing Touch, asked for from the human shape — the tree refuses it.
  let healedFromHuman =
    seen
    |> List.indexed
    |> List.tryFind (fun (i, (stance, _, asked)) ->
      i > fst shifted.Value
      && stance = None
      && asked |> List.exists (fun (name, target) -> name = "Healing Touch" && target = tank.Id))

  Assert.True(healedFromHuman.IsSome, "Healing Touch was never cast from the human form: " + describe seen)

  // And it landed: the Tank's real health is the proof, not a log line.
  let tankAfterHeal = played.Entities |> List.find (fun e -> e.Id = tank.Id)
  Assert.True(tankAfterHeal.Health > tank.MaxHealth * 2 / 5, "the Tank was never healed")

  // Nothing left to heal: the form is given back, one action as it was taken.
  let calm = SimState.mapEntity tank.Id (fun e -> { e with Health = e.MaxHealth }) played
  let _, after = play calm 60 []

  let arc = seen @ after
  let sinceHeal = arc |> List.skip (fst healedFromHuman.Value + 1)

  Assert.True(
    sinceHeal |> List.exists (fun (_, _, asked) -> asked |> List.exists (fun (n, _) -> n = "Tree of Life")),
    "the Druid never asked to shift back into the tree: " + describe arc
  )

  Assert.True(
    sinceHeal |> List.exists (fun (stance, _, _) -> stance = Some TreeForm),
    "the Druid never got back into the tree: " + describe arc
  )

/// A form is a shape, not a buff with a clock: it lasts until it is shifted out
/// of. The Shell prints every aura an entity carries with `remainingSeconds`, and
/// `Shaped` had no case in `Aura.remaining`, so hovering a Druid in Tree of Life
/// raised a match failure and took the buff bar with it.
[<Fact>]
let ``a form aura reports a lifetime instead of throwing`` () =
  Assert.True(Aura.isForm (Shaped TreeForm))
  Assert.False(Aura.isForm (Rejuvenation(secTicks 3.0, 1, "Druid")))
  Assert.True(Aura.remainingSeconds (Shaped TreeForm) > 0)

// ===========================================================================
// Procs: a talent that fires off an event it did not choose (axis A1)
// ===========================================================================

/// Rage, or whatever else an entity's pool holds, straight out of the world.
let private poolOf (id: int) (w: World) =
  w.Entities |> List.find (fun e -> e.Id = EntityId id) |> fun e -> e.Resource

/// A proc that pays five into the holder's pool, every time the event happens
/// unless the internal cooldown says otherwise.
let private rageProc (ev: ProcEvent) (cooldown: int<tick>) =
  { Talent = "Test Proc"
    Event = ev
    ChanceBp = 10000
    InternalCooldown = cooldown
    Then = GrantResource 5 }

/// A world holding one id-2 character with `talents` and a rage pool, and one
/// id-1 character to hit it. Weapons are slow enough that nothing swings.
///
/// The `Content.heroicStrike` read is not decorative: F# stores a module's
/// `let` values in its startup class, and calling one of its functions does not
/// run that initialiser, so `Content.hero` would find `partyRegen` null. Every
/// fixture in this file reads one field for that reason; `arena` does it by
/// passing an ability.
let private procWorld (talents: Talents) : World =
  let _ = Content.heroicStrike

  let noSwing = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let fighter id pos =
    Content.mob id "Fighter" CreatureType.Humanoid 20 1000 100 (Some noSwing) [] None pos

  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 20 10
    Entities =
      [ fighter 1 { X = 5; Y = 5 }
        { fighter 2 { X = 6; Y = 5 } with
            ResourceKind = Rage
            Resource = 0
            MaxResource = 100
            Talents = talents } ]
    Pending = []
    Rng = Rng.streamsOf 7UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }

/// Lock and Load is the reset the 26 s Aimed Shot row exists for: a crit of
/// Arcane or Aimed Shot clears Aimed Shot's cooldown, and a crit of anything else
/// this character owns does not — which is why the event carries the ability's
/// name rather than firing off every crit a Hunter lands.
[<Fact>]
let ``a critical Arcane Shot resets Aimed Shot's cooldown and a critical Raptor Strike does not`` () =
  // The arena's tank tile, carrying the Hunter's own talents at 60.
  let baseWorld = arena 0 0 { X = 7; Y = 5 }

  let hunter =
    Content.partyOf [ "Hunter" ] 60
    |> List.exactlyOne
    |> fun h -> { h with Id = EntityId 1; Cooldowns = Map.ofList [ "Aimed Shot", secTicks 20.0 ] }

  let world =
    { baseWorld with
        Entities = baseWorld.Entities |> List.map (fun e -> if e.Id = hunter.Id then hunter else e) }

  let cooling (w: World) =
    (SimState.tryEntity hunter.Id w).Value.Cooldowns |> Map.containsKey "Aimed Shot"

  Assert.True(cooling world)

  // A crit of an ability the talent names clears the row.
  let reset = SimCombat.procOn (OnCritOf "Arcane Shot") hunter.Id world
  Assert.False(cooling reset)

  // A crit of one the talent does not name leaves it standing.
  let untouched = SimCombat.procOn (OnCritOf "Raptor Strike") hunter.Id world
  Assert.True(cooling untouched)

/// The engine half: an event fires the proc, it pays into the pool, and a
/// hundred-percent chance draws no random number at all — the seed's stream is
/// exactly where a build carrying no procs left it.
[<Fact>]
let ``a proc pays its resource into the holder's pool`` () =
  let w = procWorld { Talents.none with Procs = [ rageProc OnBlock (ticks 0) ] }
  let rngBefore = w.Rng
  let w = SimCombat.procOn OnBlock (EntityId 2) w

  Assert.Equal(5, poolOf 2 w)
  Assert.Equal(rngBefore, w.Rng)

/// The internal cooldown lives in `Entity.Cooldowns`, the map abilities use, so
/// `SimTick.advanceCooldowns` ages it for free. While it stands the proc is dead.
[<Fact>]
let ``a proc under its internal cooldown does not fire`` () =
  let w = procWorld { Talents.none with Procs = [ rageProc OnBlock (ticks 2) ] }
  let w = SimCombat.procOn OnBlock (EntityId 2) w
  Assert.Equal(5, poolOf 2 w)

  let again = SimCombat.procOn OnBlock (EntityId 2) w
  Assert.Equal(5, poolOf 2 again)

  // Two ticks later it is available again, with no proc-specific state at all.
  let waited = SimTick.advanceCooldowns (SimTick.advanceCooldowns again)
  let refired = SimCombat.procOn OnBlock (EntityId 2) waited
  Assert.Equal(10, poolOf 2 refired)

/// Primal Fury says "while in Bear or Dire Bear Form". A cat builds energy, and
/// energy is not rage, so the clause needs no form field: the grant finds no
/// rage to pay and does nothing.
[<Fact>]
let ``a rage grant is nothing to a pool that is not rage`` () =
  let w = procWorld { Talents.none with Procs = [ rageProc OnBlock (ticks 0) ] }

  let druid =
    { (w.Entities |> List.find (fun e -> e.Id = EntityId 2)) with
        ResourceKind = Energy
        Resource = 100 }

  let w = { w with Entities = [ for e in w.Entities -> if e.Id = EntityId 2 then druid else e ] }
  let w = SimCombat.procOn OnBlock (EntityId 2) w

  Assert.Equal(100, poolOf 2 w)

/// The buff half: a `FreeMana` empower is drunk by the damage spell that lands
/// next, not by a heal. It pays that spell's whole cost and is then spent.
[<Fact>]
let ``a free-mana empower pays for the damage spell that spends it`` () =
  let bolt =
    { Content.heroicStrike with
        Name = "Test Bolt"
        ResourceCost = 100
        CastTicks = ticks 0
        Effects = [ SpellDamage(10, 10, Arcane) ] }

  let w = procWorld Talents.none

  let mage =
    { (w.Entities |> List.find (fun e -> e.Id = EntityId 1)) with
        ResourceKind = Mana
        Resource = 500
        MaxResource = 500
        Abilities = [ bolt ] }

  let w =
    { w with
        Entities =
          [ yield mage
            yield! w.Entities |> List.filter (fun e -> e.Id <> EntityId 1) ] }

  let charged =
    SimState.mapEntity
      (EntityId 1)
      (fun e ->
        { e with
            Auras = Aura.refresh (Empowered(secTicks 15.0, { Empower.none with FreeMana = true })) e.Auras })
      w

  let after = SimCombat.resolveAbility (EntityId 1) (EntityId 2) bolt charged
  let caster = after.Entities |> List.find (fun e -> e.Id = EntityId 1)

  Assert.Equal(500, caster.Resource)
  Assert.False((Aura.empower caster.Auras).FreeMana)

/// The wiring, not the mechanism: the Warrior a roster actually seats must be the
/// one carrying Shield Specialization, and a block must be what pays him. The
/// block is forced — an attack table is a die, and a test that rolls for its
/// precondition is a test that fails on a Thursday.
[<Fact>]
let ``a rostered warrior's shield pays him rage on a block`` () =
  let warrior =
    Content.partyOf [ "Warrior"; "Priest" ] 60
    |> List.find (fun e -> e.Talents.Procs |> List.exists (fun p -> p.Talent = "Shield Specialization"))

  let mob =
    let noSwing = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

    Content.mob 900 "Brute" CreatureType.Humanoid 20 5000 0 (Some noSwing) [] None { X = 6; Y = 5 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities =
        [ { warrior with
              Pos = { X = 5; Y = 5 }
              Resource = 0
              BlockChanceBp = 10000 }
          mob ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // Big enough to survive the swing, small enough that armour does not matter.
  let (EntityId wid) = warrior.Id
  let w =
    SimCombat.meleeStrike mob warrior.Id 5 0 None w
    |> fun (amount, crit, w) -> (amount, crit, w)
    |> fun (_, _, w) -> w

  Assert.Equal(5, poolOf wid w)
  Assert.True(
    w.Log |> List.exists (fun l -> l.Contains "Shield Specialization"),
    "the block paid rage without naming the talent"
  )

/// The DoT half of a proc: the damage lands on the target and the talent belongs
/// to whoever applied it, so the tick has to be able to name its caster.
[<Fact>]
let ``a dot tick fires the applier's talent on the applier`` () =
  let nightfall =
    { Talent = "Nightfall"
      Event = OnDoTTick
      ChanceBp = 10000
      InternalCooldown = ticks 0
      Then = EmpowerNext(secTicks 10.0, { Empower.none with Instant = true; Only = Some "Shadow Bolt" }) }

  let w = procWorld { Talents.none with Procs = [ nightfall ] }

  // Fighter 1 carries a dot fighter 2 applied. `firesThisTick` pays every
  // twentieth Tick, and a six-second aura is 120 of them.
  let w =
    SimState.mapEntity (EntityId 1) (fun e ->
      { e with Auras = [ Corrupted(secTicks 6.0, 5, Some(EntityId 2)) ] })
      w

  let w = SimTick.advanceAuraDamage w

  Assert.Equal(995, (w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Health)

  let caster = w.Entities |> List.find (fun e -> e.Id = EntityId 2)
  let held = Aura.empower caster.Auras
  Assert.True(held.Instant)
  Assert.Equal(Some "Shadow Bolt", held.Only)

[<Fact>]
let ``a dot applied by nobody fires nothing at all`` () =
  let nightfall =
    { Talent = "Nightfall"
      Event = OnDoTTick
      ChanceBp = 10000
      InternalCooldown = ticks 0
      Then = EmpowerNext(secTicks 10.0, { Empower.none with Instant = true }) }

  let w = procWorld { Talents.none with Procs = [ nightfall ] }

  let w =
    SimState.mapEntity (EntityId 1) (fun e -> { e with Auras = [ Corrupted(secTicks 6.0, 5, None) ] }) w

  let w = SimTick.advanceAuraDamage w

  // The damage is still dealt — the tick is real, it just has no caster to tell.
  Assert.Equal(995, (w.Entities |> List.find (fun e -> e.Id = EntityId 1)).Health)
  let bystander = w.Entities |> List.find (fun e -> e.Id = EntityId 2)
  Assert.False((Aura.empower bystander.Auras).Instant)

/// Nightfall names Shadow Bolt, so the buff is the Warlock's next cast and not
/// whatever else is on the bar. A damage spell spending a heal-only instant is
/// the failure mode this test exists for: the while-you-wait is a Corruption that
/// would otherwise eat the proc and grant nothing.
[<Fact>]
let ``a scoped instant waits for its own spell`` () =
  let _ = Content.heroicStrike

  let shadowBolt = Content.shadowBolt
  let corruption = Content.corruption
  Assert.True(shadowBolt.CastTicks > ticks 0)
  Assert.True(corruption.CastTicks > ticks 0)

  let holding =
    { Empower.none with
        Instant = true
        Only = Some "Shadow Bolt" }

  // The caster is a real Warlock off a roster, because a caster's faction, pool
  // and kit are what the command path checks before it resolves anything — a
  // hand-built entity is refused as "invalid or out of range" and the test then
  // passes without ever casting.
  let worldWith () =
    let warlock = Content.partyOf [ "Warlock" ] 60 |> List.head
    let w = procWorld Talents.none

    w
    |> SimState.mapEntity (EntityId 2) (fun e ->
      { warlock with
          Id = e.Id
          Pos = e.Pos
          Auras = [ Empowered(secTicks 10.0, holding) ] })

  let cast ability =
    SimCombat.applyCommand
      { ApplyAt = ticks 0
        Kind = UseAbility(EntityId 2, ability, EntityId 1) }
      (worldWith ())

  let boltWorld = cast "Shadow Bolt"
  let bolt = boltWorld |> SimState.tryEntity (EntityId 2) |> Option.get
  Assert.True(bolt.Casting.IsNone, "the instance the buff names did not go off instantly")

  Assert.True(
    (boltWorld.Entities |> List.find (fun e -> e.Id = EntityId 1)).Health < 1000,
    sprintf "Shadow Bolt never landed (log %A)" boltWorld.Log
  )

  Assert.False((Aura.empower bolt.Auras).Instant, "the buff survived the spell it named")

  let dot = cast "Corruption" |> SimState.tryEntity (EntityId 2) |> Option.get
  Assert.True(dot.Casting.IsSome, "a spell the buff does not name was cast instantly")
  Assert.True((Aura.empower dot.Auras).Instant, "the wrong spell spent the buff")

/// The other half of a talent that names a spell: the rotation has to reach for it.
/// `rangedOrders` ranks a kit by direct damage per cast tick, where Immolate scores
/// 4900 and Shadow Bolt 4780, so without this rule the Warlock never casts the spell
/// Nightfall names and the proc ages out unspent (26 procs, 0 spent, measured on the
/// harness). The ranking itself is untouched — see the second assertion.
[<Fact>]
let ``the warlock reaches for the spell its buff names`` () =
  let _ = Content.heroicStrike

  let members = Content.partyOf [ "Warrior"; "Warlock" ] 60
  let warlock = members |> List.find (fun e -> e.Name = "Warlock")
  let warrior = members |> List.find (fun e -> e.Name = "Warrior")

  let world (holding: bool) =
    let mob =
      { Content.mob 100 "Fighter" CreatureType.Humanoid 20 1000 0 None [] None { X = 8; Y = 5 } with
          Engaged = true
          Target = Some warrior.Id }

    let scoped =
      Empowered(secTicks 10.0, { Empower.none with Instant = true; Only = Some "Shadow Bolt" })

    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 14 12
      Entities =
        (members
         |> List.map (fun e ->
           if e.Id = warlock.Id then
             { e with
                 Target = Some mob.Id
                 Auras = (if holding then [ scoped ] else []) }
           else
             e))
        @ [ mob ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let casts (holding: bool) =
    PartyAi.decide (world holding)
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(actor, name, _) when actor = warlock.Id -> Some name
      | _ -> None)

  Assert.Contains("Shadow Bolt", casts true)
  Assert.DoesNotContain("Shadow Bolt", casts false)

[<Fact>]
let ``the warlock's build carries Nightfall`` () =
  let _ = Content.heroicStrike

  let warlock =
    Content.partyOf [ "Warlock"; "Priest" ] 60
    |> List.find (fun e -> e.Talents.Procs |> List.exists (fun p -> p.Talent = "Nightfall"))

  let nightfall =
    warlock.Talents.Procs |> List.tryFind (fun p -> p.Talent = "Nightfall")

  Assert.True(nightfall.IsSome)
  Assert.Equal(OnDoTTick, (Option.get nightfall).Event)
  Assert.Equal(400, (Option.get nightfall).ChanceBp)

/// Swift Aspects 5/5 is 15% ranged attack speed after firing, and the port had no
/// attack-speed stat at all before it. `AutoAttack.cadence` is where that lives, so
/// the first thing to pin is that it is the *interval* that moves and that it only
/// moves for a timer with range on it.
[<Fact>]
let ``haste shortens a ranged interval and leaves a melee one alone`` () =
  let bow = { Min = 38; Max = 62; Ticks = ticks 25; Range = 8 }
  let sword = { bow with Ticks = ticks 25; Range = 1 }
  let haste = [ Hastened(secTicks 12.0, 1500) ]

  // 2.5 s cut by 15% is 2.17 s, floored to 21 ticks: time is the quantity that
  // scales, not rate, so the divisor is 10000 + bp.
  Assert.Equal(ticks 21, AutoAttack.cadence bow haste)
  // The era's version is "ranged attack speed", and this content has exactly one
  // ranged timer. A melee swing keeps its cadence even holding the buff.
  Assert.Equal(ticks 25, AutoAttack.cadence sword haste)
  Assert.Equal(ticks 25, AutoAttack.cadence bow [])

/// And it is reached through content rather than by hand: only the hunter's build
/// carries an `OnShot` proc, and firing one is what puts the aura up.
[<Fact>]
let ``Swift Aspects is the hunter's, and a bow's`` () =
  let _ = Content.heroicStrike

  let hunter =
    Content.partyOf [ "Hunter"; "Priest" ] 60
    |> List.find (fun e -> e.Name = "Hunter")

  Assert.Contains(hunter.Talents.Procs, fun p -> p.Talent = "Swift Aspects" && p.Event = OnShot)

  let warrior =
    Content.partyOf [ "Warrior"; "Priest" ] 60
    |> List.find (fun e -> e.Name = "Warrior")

  Assert.DoesNotContain(warrior.Talents.Procs, fun p -> p.Event = OnShot)

[<Fact>]
let ``a landed shot puts the hunter's haste up`` () =
  let _ = Content.heroicStrike

  let hunter =
    Content.partyOf [ "Hunter" ] 60 |> List.exactlyOne

  let sword = { Min = 1; Max = 1; Ticks = ticks 5; Range = 1 }

  let mob =
    { Content.mob 900 "Dummy" CreatureType.Humanoid 60 4000000 0 (Some sword) [] None { X = 8; Y = 5 } with
        Engaged = true
        Target = Some hunter.Id }

  let world =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities = [ { hunter with Pos = { X = 5; Y = 5 } }; mob ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  // Ten percent a shot at one shot per 5 s is roughly one proc a minute, so the
  // window is swept rather than pinned: the first seed that fires is the one whose
  // world the assertions read. Step by step, because the aura lasts 12 s and the
  // end of a long run is usually the seconds after it expired.
  let armedWorld (seed: uint64) =
    let mutable w = { world with Rng = Rng.streamsOf seed }
    let mutable armed = false

    for _ in 1 .. 600 do
      if not armed && SimState.continues w then
        w <- SimTick.step (Bench.autoPilot w) w
        armed <- w.Log |> List.exists (fun l -> l.Contains "Swift Aspects procs")

    if armed then Some w else None

  let fired =
    [ 1UL .. 12UL ]
    |> List.tryPick armedWorld
    |> Option.defaultWith (fun () -> failwith "the talent never fired over twelve seeded minutes")

  let hunter = fired.Entities |> List.find (fun e -> e.Name = "Hunter")

  Assert.Contains(hunter.Auras, function Hastened _ -> true | _ -> false)
  // The timer armed for the next shot is the hasted interval, not the 5 s the bow
  // would otherwise take: 50 ticks cut by 15% is 43.
  Assert.True(
    (int hunter.SwingTicksLeft) < 50,
    sprintf "expected the timer to be re-armed for the hasted interval, got %d" (int hunter.SwingTicksLeft)
  )

// ===========================================================================
// Healing done, the orders a form refuses, and melee-range damage
// ===========================================================================

/// The party `sim healers` measures: seat 2 is the healing seat. In a fight and
/// with the tank holding the pack's attention, because `PartyAi.decide` issues
/// nothing out of one and a pull nobody has noticed yet goes through the pre-pull
/// openers instead of the rotation under test.
let private healerSeatWorld (roster: string list) =
  let w = Content.dungeonAt 36 1UL |> Content.withRosterAt 36 roster

  let tankId = w.Entities |> List.find (fun e -> e.Name = "Warrior") |> fun t -> t.Id

  { w with
      Entities =
        w.Entities
        |> List.map (fun e ->
          if e.Faction = Hostile then
            { e with Engaged = true; Target = Some tankId }
          else
            e) }

let private healerSeat () = healerSeatWorld [ "Warrior"; "Druid"; "Mage"; "Rogue"; "Warlock" ]

/// A heal-over-time tick is healing done, and belongs to the member whose spell
/// laid it. It is booked on the log — the one event record `Attribution` reads —
/// in the direct heal's own shape, so the tick lands in `byCaster` beside the
/// casts with no second parser. `heal/sc` in `sim healers` is this number.
[<Fact>]
let ``a heal-over-time tick is booked as healing by the member who cast it`` () =
  let w = healerSeat ()
  let druid = w.Entities |> List.find (fun e -> e.Role = Some Healer)
  let tank = w.Entities |> List.find (fun e -> e.Name = "Warrior")

  let tickOn (health: int) =
    let hurt =
      { w with
          Entities =
            w.Entities
            |> List.map (fun e ->
              if e.Id = tank.Id then
                { e with
                    Health = health
                    Auras = [ Rejuvenation(secTicks 10.0, 12, "Rejuvenation") ] }
              else
                e) }

    SimTick.advanceAuraHealing hurt
    |> Attribution.byCaster
    |> Map.tryFind druid.Name
    |> Option.defaultValue Attribution.empty

  // 12 a tick on a wounded tank: all of it is healing done.
  Assert.Equal({ Attribution.empty with Healing = 12 }, tickOn (tank.MaxHealth - 500))
  // Overheal is not. Five health short of full and a tick of 12 books five, the
  // rule the direct path already follows.
  Assert.Equal({ Attribution.empty with Healing = 5 }, tickOn (tank.MaxHealth - 5))
  // Nothing wounded, nothing booked — the aura still ticks, the log stays clean.
  Assert.Equal(Attribution.empty, tickOn tank.MaxHealth)

/// A form that will not let a damaging spell out must not be asked for one. Tree
/// of Life refuses every damaging cast in `SimCombat`, so the healer's mana-to-spare
/// fallback used to issue a Moonfire per Tick that was thrown away — the Druid
/// stood there for the whole encounter. The gate is `canDamageNow`, the engine's own
/// refusals rather than a Druid special case, and the mana floor is untouched.
[<Fact>]
let ``a Tree of Life healer is not asked for damage it cannot cast`` () =
  // A run of Ticks, not one: the pull opens with a totem or a shift, so the
  // damage fallback is only asked for once those are out of the way.
  let ordersOver (tickCount: int) (roster: string list) (auras: Aura list) =
    let w = healerSeatWorld roster
    let healer = w.Entities |> List.find (fun e -> e.Role = Some Healer)

    let healer =
      { healer with
          Resource = healer.MaxResource
          Auras = auras }

    let mutable w =
      { w with
          Entities = w.Entities |> List.map (fun e -> if e.Id = healer.Id then healer else e) }

    let ordered = ResizeArray<string>()

    for _ in 1 .. tickCount do
      if SimState.continues w then
        let cmds = PartyAi.decide w
        ordered.AddRange(abilitiesFor healer.Id cmds)
        w <- SimTick.step cmds w

    healer, List.ofSeq ordered

  let tree, inTree = ordersOver 60 [ "Warrior"; "Druid"; "Mage"; "Rogue"; "Warlock" ] [ Shaped TreeForm ]

  // The verdict is the engine's: in the tree nothing in the kit may be pressed as
  // damage, so the fallback has nothing to ask for.
  Assert.False(PartyAi.canDamageNow tree, "the tree still thinks it can nuke")

  Assert.Empty(
    inTree
    |> List.filter (fun ordered -> tree.Abilities |> List.exists (fun a -> a.Name = ordered && Ability.isDamaging a))
  )

  // The same seat with a healer the form does not gag still adds damage once it
  // has nothing to heal and mana to spare, so the gate above is the form and not
  // healers having lost their fallback altogether.
  let shaman, shamanOrders =
    ordersOver 60 [ "Warrior"; "Shaman"; "Mage"; "Rogue"; "Warlock" ] []

  Assert.True(PartyAi.canDamageNow shaman, "a Shaman healer reads as unable to nuke")

  Assert.Contains(
    shamanOrders,
    fun ordered -> shaman.Abilities |> List.exists (fun a -> a.Name = ordered && Ability.isDamaging a)
  )

/// Judgment on the reach rule. A Paladin healer's only damage row is instant at
/// `Range = 1` — the file's approximation, since the research record gives a
/// cooldown and no range — and `shots` asked for `Range > 1`, so the ability was
/// unreachable and the Paladin spent nothing on damage at all. A melee-range
/// instant is a shot for a healer that is already in reach; the ability data is
/// not touched.
[<Fact>]
let ``a healer in melee reaches a melee-range instant damage row`` () =
  // The data is where it was: this test is about the filter, not the range.
  Assert.Equal(1, Content.judgement.Range)
  Assert.True(Ability.isDamaging Content.judgement)
  Assert.Equal(ticks 0, Content.judgement.CastTicks)

  let tap = { Min = 1; Max = 1; Ticks = secTicks 2.7; Range = 1 }

  let tank =
    Content.hero 1 "Warrior" Tank 1000 1000 tap 50000 [ Content.sunderArmor; Content.heroicStrike ] { X = 5; Y = 5 }

  // Adjacent to the mob, and to the tank: a healer that never has to walk in.
  let paladin =
    Content.hero
      2
      "Paladin"
      Healer
      1000
      1000
      tap
      10000
      [ Content.judgement; Content.flashOfLight ]
      { X = 6; Y = 5 }

  let near =
    { Content.mob 100 "Near" CreatureType.Humanoid 21 1000 1000 (Some tap) [] None { X = 6; Y = 6 } with
        Engaged = true
        Target = Some(EntityId 1) }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 12 10
      Entities = [ tank; paladin; near ]
      Pending = []
      Rng = Rng.streamsOf 3UL
      PartyBuffs = []
      Quests = []
      // Nothing to heal, and a pull that is nearly over: the mana budget's own
      // precondition, which used to be a flat 55% pool. A healer that has not seen the
      // fight is told to keep everything, so a test that wants a damage row has to say
      // which fight it is standing in.
      Forecast =
        { Forecast.empty with
            PartyDamage = SimState.ManaBudget.damageWindow * 100
            DamageTicks = SimState.ManaBudget.damageWindow
            HealerDrain = Map.ofList [ EntityId 2, 0 ] }
      TotemLedger = TotemLedger.empty
      Log = [] }

  // Nothing to heal and the pull nearly over, which is where the row has to appear.
  Assert.Contains(abilitiesFor (EntityId 2) (PartyAi.decide w), fun n -> n = "Judgement")

// ===========================================================================
// The healer's mana budget, which is a question about the future
// ===========================================================================

/// A World in which the healer faces a pull of `hp` effective health coming off at
/// `dps`, having been drawing `drain` mana a Tick. Both windows are written directly:
/// these tests are about the pricing, and the recorder that fills the windows has
/// `SimTick.step` and `sim ttk` to speak for it.
let private budgetWorld (hp: int) (dps: int) (drain: int) =
  let w = healerSeatWorld [ "Warrior"; "Priest"; "Mage"; "Rogue"; "Warlock" ]
  let healer = w.Entities |> List.find (fun e -> e.Role = Some Healer)

  // One engaged hostile carrying the whole of the pull's remaining health, so
  // `hostileHp` is exactly `hp` and nothing in the dungeon's own back half is counted.
  let pack =
    w.Entities
    |> List.filter (fun e -> e.Faction = Hostile && e.Engaged)
    |> List.truncate 1
    |> List.map (fun e -> { e with Health = hp; MaxHealth = max hp e.MaxHealth })

  let rest = w.Entities |> List.filter (fun e -> e.Faction <> Hostile)

  { w with
      Entities = rest @ pack
      Forecast =
        { Forecast.empty with
            // The EWMA accumulator holds `mean x damageWindow`, so a sample of the Tick
            // rate, multiplied by the window, is exactly the requested rate at rest —
            // and `DamageTicks` at the window says the warm-up is already over.
            PartyDamage = SimState.ManaBudget.damageWindow * (dps / 20)
            DamageTicks = SimState.ManaBudget.damageWindow
            EngagedHostiles = pack |> List.map (fun e -> e.Id) |> Set.ofList
            // `burnWindow`-scaled, because the window stores the mean times its length.
            HealerDrain = Map.ofList [ healer.Id, drain * SimState.ManaBudget.burnWindow ] }
      TotemLedger = TotemLedger.empty }

let private healerOf (w: World) =
  w.Entities |> List.find (fun e -> e.Role = Some Healer)

let private smiteCost (h: Entity) =
  h.Abilities |> List.find (fun a -> a.Name = "Smite") |> fun a -> a.ResourceCost

/// A pull with two hundred seconds left is a pool to ration: whatever surplus the pool
/// shows is spoken for by the healing still to come, so the gate refuses the cast even
/// with the pool full.
[<Fact>]
let ``a healer facing a long pull does not spend on damage`` () =
  let w = budgetWorld 400000 2000 2
  let priest = healerOf w
  let plan = SimState.ManaBudget.plan w priest

  Assert.True(plan.SecondsToKill > 150.0, sprintf "expected a long forecast, got %.1f s" plan.SecondsToKill)
  Assert.True(plan.Reserve > plan.Pool, sprintf "reserve %d should exceed pool %d" plan.Reserve plan.Pool)
  Assert.True(plan.Budget < smiteCost priest, sprintf "budget %d should not buy a Smite" plan.Budget)
  Assert.False(SimState.ManaBudget.maySpendOnDamage w priest (smiteCost priest))

/// The same healer, the same pool, the same draw — and a pull that is nearly over. The
/// reserve collapses toward the one-heal margin and the surplus is free.
[<Fact>]
let ``a healer facing a short pull does spend on damage`` () =
  let w = budgetWorld 600 2000 2
  let priest = healerOf w
  let plan = SimState.ManaBudget.plan w priest

  Assert.True(plan.SecondsToKill < 1.0, sprintf "expected a short forecast, got %.1f s" plan.SecondsToKill)
  Assert.True(plan.Budget >= smiteCost priest, sprintf "budget %d should buy a Smite" plan.Budget)
  Assert.True(SimState.ManaBudget.maySpendOnDamage w priest (smiteCost priest))

/// The whole point of pricing the future: the reserve falls with the horizon. Holding
/// the pool and the draw fixed and walking the pack's remaining health down, the amount
/// that must be kept back falls with it.
///
/// It now has to fall for two reasons, not one: the draw term shrinks with the horizon
/// and so does the heal floor. `Safety` is the floor on its own, so the assertions below
/// also pin the floor's shape \u2014 two heals while a long fight can still deliver them,
/// one when only one cast still lands, none when the pull dies inside a cast.
[<Fact>]
let ``the reserve shrinks as the pull nears its end`` () =
  // 2 000 hp/s of party damage and a Priest: 400 000 hp is 200 s, 4 000 hp is 2 s, and
  // 400 hp is a fifth of a second \u2014 shorter than one heal cast.
  let plans =
    [ 400000; 40000; 8000; 4000; 400 ]
    |> List.map (fun hp ->
      let w = budgetWorld hp 2000 2
      let plan = SimState.ManaBudget.plan w (healerOf w)
      plan.SecondsToKill, plan.Safety, plan.Reserve)

  for (ttk, _, reserve) in plans do
    Assert.True(reserve > 0, sprintf "reserve at ttk %.1f went to %d" ttk reserve)

  plans
  |> List.pairwise
  |> List.iter (fun ((ttkA, _, reserveA), (ttkB, _, reserveB)) ->
    Assert.True(ttkB < ttkA, sprintf "forecast should shorten: %.1f then %.1f" ttkA ttkB)
    Assert.True(reserveB <= reserveA, sprintf "reserve should shrink: %d then %d" reserveA reserveB))

  let floorAt hp =
    let w = budgetWorld hp 2000 2
    SimState.ManaBudget.plan w (healerOf w)

  let longPull = floorAt 400000
  let oneHeal = longPull.Safety / SimState.ManaBudget.marginHeals

  // Two heals while the pull is long, and exactly the same two of them, so a refined
  // floor did not quietly become a bigger one.
  Assert.Equal(SimState.ManaBudget.marginHeals * oneHeal, longPull.Safety)

  // Four seconds left is not two heals' worth of fight, and the old fixed floor is
  // exactly what the operator measured blocking the endgame at 706 against a 500 pool.
  let nearlyDone = floorAt 8000
  Assert.InRange(nearlyDone.SecondsToKill, 3.0, 5.0)
  Assert.True(nearlyDone.Safety < longPull.Safety, sprintf "floor %d did not shrink from %d" nearlyDone.Safety longPull.Safety)

  // A fifth of a second left: no cast can still land, so no heal is reserved.
  let over = floorAt 400
  Assert.True(over.SecondsToKill < 1.0, sprintf "expected under a second, got %.2f" over.SecondsToKill)
  Assert.Equal(0, over.Safety)

/// The measured bug, as a test. At four seconds left the old reserve was 706 against a
/// 500 pool \u2014 `2 x dearest heal` and nothing else, because the draw term was ~10 \u2014 so the
/// gate refused a Smite with the pull nearly over. The floor now prices the heals that
/// can still land, and one of them fits in four seconds.
[<Fact>]
let ``a nearly-finished fight frees the damage budget`` () =
  let start = budgetWorld 8000 2000 0
  let priest = healerOf start

  let w =
    { start with
        Entities =
          start.Entities
          |> List.map (fun e ->
            if e.Id = priest.Id then
              { e with Resource = 500; MaxResource = 1000 }
            else
              e) }

  let priest = healerOf w
  let plan = SimState.ManaBudget.plan w priest

  Assert.InRange(plan.SecondsToKill, 3.0, 5.0)
  Assert.Equal(500, plan.Pool)

  // The two-heal floor would still be 2x this and still refuse the cast.
  let oneHeal = plan.Safety
  Assert.True(oneHeal > 0, "a four-second pull still needs the one heal that can land")
  Assert.True(plan.Reserve <= 500, sprintf "reserve %d should fit a 500 pool" plan.Reserve)
  Assert.True(plan.Budget >= smiteCost priest, sprintf "budget %d should buy a Smite" plan.Budget)
  Assert.True(SimState.ManaBudget.maySpendOnDamage w priest (smiteCost priest))

  // The same numbers as a long pull, to show which one moved: the pool did not, the
  // horizon did. With no net draw the long pull's reserve is its floor \u2014 and that floor
  // is the pair of heals the old code charged at four seconds too.
  let longStart = budgetWorld 400000 2000 0
  let longPull = SimState.ManaBudget.plan longStart (healerOf longStart)
  Assert.Equal(SimState.ManaBudget.marginHeals * oneHeal, longPull.Safety)
  Assert.Equal(longPull.Safety, longPull.Reserve)
  Assert.True(longPull.Reserve > 500, sprintf "the pair of heals is %d, which is what refused a 500 pool" longPull.Reserve)

/// The pack is the unit of the forecast, and the rate is not: a new pack joining the
/// engaged set lengthens the horizon and leaves party dps where it was, while a hostile
/// dying is attrition and must not look like a fresh start.
[<Fact>]
let ``a new pack extends the horizon without collapsing party dps`` () =
  let start = budgetWorld 8000 2000 2
  let pack = start.Entities |> List.filter (fun e -> e.Faction = Hostile && e.Engaged)
  Assert.NotEmpty(pack)

  let joined =
    { pack.Head with
        Id = EntityId 900
        Name = "Second pack"
        Health = 8000
        MaxHealth = 8000 }

  let before = start
  let afterJoin = SimState.ManaBudget.settle before { start with Entities = start.Entities @ [ joined ] }

  // The party's output is a property of the party: an arrival cannot zero it.
  Assert.True(
    afterJoin.Forecast.PartyDamage > before.Forecast.PartyDamage * 9 / 10,
    sprintf "dps collapsed on a pull: %d then %d" before.Forecast.PartyDamage afterJoin.Forecast.PartyDamage)

  // ... and the horizon moved, because the health it divides into did.
  let hpBefore = SimState.ManaBudget.hostileHp before
  let hpAfter = SimState.ManaBudget.hostileHp afterJoin
  Assert.True(hpAfter > hpBefore, sprintf "health should grow: %d then %d" hpBefore hpAfter)
  Assert.Equal(before.Forecast.EngagementEpoch + 1, afterJoin.Forecast.EngagementEpoch)

  // Attrition, the other way: one hostile dies, the epoch does not move and the rate
  // does not reset. This is the mid-pack reset storm the epoch exists to prevent.
  let killed =
    afterJoin.Entities
    |> List.map (fun e -> if e.Id = joined.Id then { e with Health = 0 } else e)

  let afterDeath = SimState.ManaBudget.settle afterJoin { afterJoin with Entities = killed }
  Assert.Equal(afterJoin.Forecast.EngagementEpoch, afterDeath.Forecast.EngagementEpoch)

  Assert.True(
    afterDeath.Forecast.PartyDamage > before.Forecast.PartyDamage * 9 / 10,
    sprintf "dps collapsed on a corpse: %d then %d" before.Forecast.PartyDamage afterDeath.Forecast.PartyDamage)

  // The forecast reads off the surviving set, so the horizon is the short one again.
  Assert.Equal(hpBefore, SimState.ManaBudget.hostileHp afterDeath)

/// Heal-first is not part of the bargain. A wounded Tank wins over a forecast that would
/// have paid for damage, which is the one thing the budget must never be able to outbid.
[<Fact>]
let ``a wound still outranks the damage the budget would allow`` () =
  let w = budgetWorld 600 2000 2
  let priest = healerOf w
  let tank = w.Entities |> List.find (fun e -> e.Name = "Warrior")

  // The budget really would allow damage here, so the assertion below is about
  // priority and not about a refusal.
  Assert.True(SimState.ManaBudget.maySpendOnDamage w priest (smiteCost priest))

  let w =
    { w with
        Entities =
          w.Entities
          |> List.map (fun e -> if e.Id = tank.Id then { e with Health = e.MaxHealth / 3 } else e) }

  let orders = PartyAi.decide w

  let healerOrders =
    orders
    |> List.choose (fun c ->
      match c.Kind with
      | UseAbility(actor, name, _) when actor = priest.Id ->
        priest.Abilities |> List.tryFind (fun a -> a.Name = name)
      | _ -> None)

  Assert.True(not healerOrders.IsEmpty, "a wounded Tank should have been healed")
  Assert.All(healerOrders, fun a -> Assert.False(Ability.isDamaging a, sprintf "healer chose %s over the Tank" a.Name))

// ===========================================================================
// The healer activity partition (sim healers' active/covered/unmet/blocked/recov)
// ===========================================================================

/// The party `sim healers` measures, with its healer and its tank to hand. Every
/// partition test below is this world, and a bucket test is then one edit away:
/// `healerStandby` decides from the party's health, the healer's own resources and
/// the healer's own cooldowns, and from nothing else.
let private standbyParty (healer: string) =
  let w = healerSeatWorld [ "Warrior"; healer; "Mage"; "Rogue"; "Warlock" ]
  let h = w.Entities |> List.find (fun e -> e.Role = Some Healer)
  let tank = w.Entities |> List.find (fun e -> e.Name = "Warrior")
  w, h, tank

/// Put the tank at `pct` of its pool. This is the whole of "the party needs healing":
/// the AI's need test is a health percentage against its own trouble bar, not a
/// number this test may invent.
let private hurtTankTo (w: World) (tank: Entity) (pct: int) =
  { w with
      Entities =
        w.Entities
        |> List.map (fun e -> if e.Id = tank.Id then { e with Health = e.MaxHealth * pct / 100 } else e) }

/// The classifier this replaces read `Casting` and nothing else, and an instant never
/// sets it (`SimCombat.startOrResolve` resolves `CastTicks <= 0` outright), so the
/// instant classes — the two the operator is asking about — had their throughput
/// scored as standing still. Rejuvenation at zero cast ticks is that bug, and the
/// order is the fix.
[<Fact>]
let ``an instant cast counts as activity on the tick it is ordered`` () =
  let w, druid, tank = standbyParty "Druid"

  let rejuvenation = druid.Abilities |> List.find (fun a -> a.Name = "Rejuvenation")
  Assert.Equal(0, int rejuvenation.CastTicks)

  let order = { ApplyAt = ticks 0; Kind = UseAbility(druid.Id, "Rejuvenation", tank.Id) }

  Assert.Equal(PartyAi.Active, PartyAi.healerTick w druid [ order ])

  // With the order withdrawn the same Tick is a standby Tick again, so the assertion
  // above is about the instant and not about the fixture being busy.
  Assert.Equal(PartyAi.Covered, PartyAi.healerTick w druid [])

  // A cast bar is activity for the obvious reason, and `Stop` is not: it is the order
  // to stand still, and counting it would make every idle Tick look busy.
  let casting =
    { druid with
        Casting =
          Some
            { Ability = rejuvenation
              Target = tank.Id
              Remaining = ticks 10
              Pushes = 0 } }

  Assert.Equal(PartyAi.Active, PartyAi.healerTick w casting [])

  Assert.Equal(
    PartyAi.Covered,
    PartyAi.healerTick w druid [ { ApplyAt = ticks 0; Kind = Stop druid.Id } ]
  )

  // A step already under way has no fresh order on that Tick — it keeps the route it
  // was given — so movement is read from state. A healer closing on a wounded tank is
  // not idle either.
  Assert.Equal(PartyAi.Active, PartyAi.healerTick w { druid with MoveTicksLeft = ticks 3 } [])

/// The good idle: nothing hurt, so nothing owed. This is the bucket the instant and
/// heal-over-time classes should live in, and the bucket the old `idle%` could not
/// tell apart from a missed heal.
[<Fact>]
let ``a healer with nothing to do reads as covered`` () =
  let w, druid, _ = standbyParty "Druid"
  Assert.Equal(PartyAi.Covered, PartyAi.healerTick w druid [])

/// The bad idle: a heal was needed, the kit holds one that is affordable, ready and
/// legal, and nothing was ordered. It is reachable — the AI simply never leaves it
/// there, which is why the runs measure it at ~0% and not because the code cannot
/// say it.
[<Fact>]
let ``a needed, payable heal with no order reads as unmet`` () =
  let w, priest, tank = standbyParty "Priest"
  let w = hurtTankTo w tank 50
  Assert.Equal(PartyAi.Unmet, PartyAi.healerTick w priest [])

/// Blocked, three ways. The causes are kept apart because they want opposite fixes:
/// a pool problem is a budget problem, a cooldown is a rotation problem, and a form
/// problem is the healer standing in the wrong shape for the kit it wants.
[<Fact>]
let ``a heal the engine would refuse reads as blocked, by cause`` () =
  let w, priest, tank = standbyParty "Priest"
  let w = hurtTankTo w tank 50

  // No mana for any heal in the kit.
  Assert.Equal(
    PartyAi.Blocked PartyAi.ShortOfMana,
    PartyAi.healerTick w { priest with Resource = 0 } []
  )

  // The Druid's only direct heal is Healing Touch — two rows of it, `Content.fs`
  // renames the second so the two do not share one cooldown slot — 10 s of cooldown
  // (`Content.fs`'s `healingTouchR4Ranks`) and a HumanForm stance. The roster Druid
  // stands in TreeForm, so the human case has to strip the shape aura first.
  let w, druid, tank = standbyParty "Druid"
  let w = hurtTankTo w tank 50

  let healing =
    druid.Abilities
    |> List.filter (fun a -> a.Effects |> List.exists (function Heal _ -> true | _ -> false))
    |> List.map (fun a -> a.Name)

  Assert.False(healing.IsEmpty, "the fixture needs a direct heal to refuse")

  let everythingCooling =
    { druid with
        Auras = []
        Cooldowns = healing |> List.map (fun n -> n, ticks 200) |> Map.ofList }

  Assert.Equal(PartyAi.Blocked PartyAi.OnCooldown, PartyAi.healerTick w everythingCooling [])

  // Same kit, same mana, off cooldown — but a Tree Druid cannot cast it, and no held
  // empower waives the form gate.
  Assert.Equal(PartyAi.Blocked PartyAi.WrongForm, PartyAi.healerTick w druid [])

/// Recovering: a hole the Spirit rate closes cheaper than a cast, on a clear
/// five-second window. The branch is `deficit <= 2 * ResourceRegenPerTick`, and on a
/// level-36 body the trouble bar sits far above two ticks of real regeneration — so
/// this bucket measured 0.0% across all four healers. It is granted the regen it
/// needs here rather than left untested, because a bucket nobody can reach is a
/// bucket that will quietly be deleted.
[<Fact>]
let ``a hole regeneration can close reads as recovering`` () =
  let w, druid, tank = standbyParty "Druid"

  // 89% of the pool is below the trouble bar but not yet urgent, and the hole is a
  // tenth of the tank's health — so the allowance has to be at least half that.
  let w = hurtTankTo w tank 89
  let druid = { druid with ResourceRegenPerTick = tank.MaxHealth / 4 }

  Assert.True(SimState.healthPct tank >= 60, "the fixture must not be urgent, or it is not this branch")
  Assert.Equal(PartyAi.Recovering, PartyAi.healerTick w druid [])

/// The partition closes: every classified Tick lands in exactly one of the five, so
/// the shares `sim healers` prints add to 100%. Asserted on `tally` as well as in the
/// command, because a hole here resizes every other bucket silently — which is how
/// the old single `idle%` stayed wrong.
[<Fact>]
let ``the activity partition closes over all five buckets`` () =
  let w, priest, tank = standbyParty "Priest"
  let hurt = hurtTankTo w tank 50

  let ticks =
    [ PartyAi.healerTick w priest []
      PartyAi.healerTick hurt priest []
      PartyAi.healerTick hurt { priest with Resource = 0 } []
      PartyAi.healerTick hurt { priest with Cooldowns = Map.ofList [ "Heal", ticks 200 ] } []
      PartyAi.healerTick
        hurt
        { priest with
            Casting =
              Some
                { Ability = priest.Abilities |> List.find (fun a -> a.Name = "Heal")
                  Target = tank.Id
                  Remaining = ticks 10
                  Pushes = 0 } }
        [] ]

  let counts = PartyAi.tally ticks

  Assert.Equal(5, counts.Length)
  Assert.Equal(ticks.Length, Array.sum counts)
  // The cast-bar Tick is the only `active` one, and no bucket was left at zero:
  // covered, unmet and blocked all fired above.
  Assert.Equal(1, counts[0])
  Assert.True(counts[1] > 0 && counts[2] > 0 && counts[3] > 0, sprintf "a bucket went empty: %A" counts)

// ===========================================================================
// Healer AI and healer gear — one test per change.
// ===========================================================================

/// Change 1. The Imp and the Hunter's pet are both `SummonSpec` bodies built by the
/// same `Summon` branch, so "it was summoned" cannot tell them apart. The summon's
/// *kind* can: `SummonSpec.CreatureType` is copied onto the body, every hero is a
/// `Humanoid` (`Content.heroStats`), an Imp is a `Demon` (`Content.warlockDemon`) and
/// a pet is a `Beast`. The healer's need-pool keeps the Beast and drops the Demon —
/// a 420-health body cannot absorb a player-sized heal for the mana it costs, which
/// was 0.81 healing per point on the Shaman, the worst row in the ledger.
[<Fact>]
let ``a healer's need-pool drops the warlock's imp and keeps the hunter's pet`` () =
  let summonOf (owner: string) (ability: string) (roster: string list) =
    let w = Content.dungeonAt 24 1UL |> Content.withRosterAt 24 roster
    let caster = w.Entities |> List.find (fun e -> e.Name = owner)

    let after =
      caster.Abilities
      |> List.find (fun a -> a.Name = ability)
      |> fun a -> SimCombat.resolveAbility caster.Id caster.Id a w

    (after, after.Entities |> List.find (fun e -> e.Owner = Some caster.Id))

  let withDemon, imp =
    summonOf "Warlock" "Summon Demon" [ "Warrior"; "Priest"; "Warlock"; "Mage"; "Rogue" ]

  let withPet, pet =
    summonOf "Hunter" "Summon Pet" [ "Warrior"; "Priest"; "Hunter"; "Mage"; "Rogue" ]

  // The demon is still a party body: it fights, it holds threat and it can be killed.
  // Only the healer's pool drops it, so no other system learns a new exclusion.
  Assert.Contains(SimState.aliveParty withDemon, fun e -> e.Id = imp.Id)
  Assert.DoesNotContain(SimState.healableParty withDemon, fun e -> e.Id = imp.Id)

  // A pet is a party member with a Mend Pet that can hold it up, so it stays.
  Assert.Contains(SimState.aliveParty withPet, fun e -> e.Id = pet.Id)
  Assert.Contains(SimState.healableParty withPet, fun e -> e.Id = pet.Id)

  // The discriminator is the creature type — the summon's kind, not its name.
  Assert.Equal(CreatureType.Demon, imp.CreatureType)
  Assert.Equal(CreatureType.Beast, pet.CreatureType)

/// Change 2. Renew is laid down before the tank is hurt, not after: the heal-over-time
/// branch of `healerOrders` covers a target whose HoT is absent or expiring without
/// waiting for a hole, which is the same predicate that sends Rejuvenation. The mob
/// here has no swing, so the tank is at full health for the whole window and a Renew
/// order cannot be a reaction to damage.
[<Fact>]
let ``the priest renews an untouched tank rather than waiting for a wound`` () =
  let party = Content.partyOf [ "Warrior"; "Priest" ] 36
  let warrior = party |> List.find (fun e -> e.Name = "Warrior")
  let priest = party |> List.find (fun e -> e.Name = "Priest")

  let stand (e: Entity) (x: int) = { e with Pos = { X = x; Y = 9 }; Home = Some { X = x; Y = 9 } }

  let mob =
    Content.mob 100 "Grunt" CreatureType.Humanoid 36 100000 1000 None [] None { X = 15; Y = 9 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 24 20
      Entities = [ stand warrior 10; stand priest 11; mob ]
      Pending = []
      Rng = Rng.streamsOf 11UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let pulled = SimCombat.dealDamage mob.Id 1 w
  Assert.True(SimState.inCombat pulled, "the pull should put the party in combat")

  let tankIsWhole (w: World) =
    let t = w.Entities |> List.find (fun e -> e.Id = warrior.Id)
    t.Health = t.MaxHealth

  let rec play (n: int) (w: World) =
    if n = 0 then
      false
    else
      let renewed =
        PartyAi.decide w
        |> List.exists (fun c ->
          match c.Kind with
          | UseAbility(a, name, t) -> a = priest.Id && t = warrior.Id && name.StartsWith "Renew"
          | _ -> false)

      if renewed && tankIsWhole w then true else play (n - 1) (SimTick.step (Bench.autoPilot w) w)

  Assert.True(play 60 pulled, "the priest should cover an untouched tank with Renew")

/// Change 3. A healer that fights with a weapon closes to the fight when it has
/// nothing to heal, and the swing follows on `Entity.AutoAttack`'s own clock.
///
/// The Shaman is seated on a real encounter through `withRoster`, because a class
/// outside `defaultRoster` can only be measured that way — and the point of the
/// change is what it does in a real fight, not in a hand-built world. Before the
/// change the Shaman stood at its starting range and swung zero times in a whole
/// encounter; the assertion is the count, so the test fails loudly if the walk
/// regresses rather than passing on one lucky swing.
[<Fact>]
let ``a shaman healer closes to the fight between heals and lands swings`` () =
  let w =
    Content.withRoster [ "Warrior"; "Priest"; "Rogue"; "Mage"; "Shaman" ] (Content.gully 1UL)

  let shaman = w.Entities |> List.find (fun e -> e.Name = "Shaman")
  // A one-tile reach, so this is a melee rule and not a bow.
  Assert.Equal(1, shaman.AutoAttack.Value.Range)

  let played =
    [ 1..3000 ] |> List.fold (fun w _ -> SimTick.step (Bench.autoPilot w) w) w

  // Every way the attack table can answer a swing still proves the swing happened:
  // a dodge, a parry and a block all name the Shaman as the attacker.
  let swings =
    played.Log
    |> List.filter (fun l ->
      l.Contains "Shaman swings at"
      || l.Contains "dodges Shaman"
      || l.Contains "parries Shaman"
      || l.Contains "blocks Shaman"
      || l.Contains "Shaman misses")
    |> List.length

  Assert.True(swings > 0, "the shaman healer never closed to the fight and swung")

/// Change 4. The relationship the two healer sets are meant to have, pinned as the
/// relationship rather than as numbers: the Paladin's two-hander out-damages the
/// Shaman's one-hander, and the one-hander is the half that buys a shield. Both
/// level-24 sets are the researched best in slot — the Paladin's `Verigan's Fist`
/// (65-99 at 3.20 s, +6 Int +7 Sta +12 Spirit) is a caster two-hander — with one
/// exception this test now records: no <=24 one-hand caster weapon is sourced
/// (`.pi/research/level24/shaman.md:92-95`), so the Shaman's main hand is the
/// operator's authorised **assumption** rather than a citation — the staff's 3.00 s
/// rung less twelve points off each end — and the shield beside it is the one
/// verified <=24 shield, Arctic Buckler.
[<Fact>]
let ``the paladin's two-hander out-damages the shaman's one-hander, which carries a shield`` () =
  let hero (name: string) = Content.partyOf [ name ] 24 |> List.head

  let paladin = hero "Paladin"
  let shaman = hero "Shaman"

  // Damage per second in thousandths, the form the gear test uses, so the
  // comparison stays in integers. `Ticks` carries the `tick` measure, so the
  // quotient is cast back to a plain int before it is compared.
  let dps (a: AutoAttack) = int ((a.Min + a.Max) * 1000 / a.Ticks)

  let paladinDps = dps paladin.AutoAttack.Value
  let shamanDps = dps shaman.AutoAttack.Value

  // 65-99 at 3.20 s is 164 x 1000 / 64 against the one-hander's 94 x 1000 / 60:
  // about two thirds more, which is the two-hander's whole point and half of why the
  // Shaman is given a shield instead. Ticks are 20 a second.
  Assert.Equal(2562, paladinDps)
  Assert.Equal(1566, shamanDps)
  Assert.True(paladinDps > shamanDps * 13 / 10, "a two-hander should lead by roughly a third")

  // Item armour on the researched sets: four mail pieces for the Paladin (557) and
  // eight leather/cloth pieces for the Shaman (238), plus Arctic Buckler's 642 —
  // the Paladin has none because a two-hander fills both hands, which is the trade
  // this test exists to keep visible.
  Assert.Equal((65, 99), (paladin.AutoAttack.Value.Min, paladin.AutoAttack.Value.Max))
  Assert.Equal((35, 59), (shaman.AutoAttack.Value.Min, shaman.AutoAttack.Value.Max))
  Assert.Equal(0, paladin.BlockChanceBp)
  Assert.True(shaman.BlockChanceBp > 0, "the one-hander's half of the trade is the shield")

/// Change 5a. Water Shield's clock is its globes, and the globes are what pays for a
/// hit. The whole mechanism is one test because it is one mechanism: a landed hit takes
/// exactly one globe, that globe hands back exactly what the row says it holds, and the
/// last globe is the end of the shield — there is no timer, so "spent" is the only way
/// it can end and the only condition under which the Shaman puts it up again.
///
/// The pool is drained before the hits for two reasons: a full pool would clip the
/// payback at `MaxResource` and the assertion would pass on a globe that paid nothing,
/// and a partly-empty pool is what makes the nine-globe total a sum rather than a cap.
[<Fact>]
let ``water shield spends one globe a hit and is recast only once they are gone`` () =
  let party = Content.partyOf [ "Warrior"; "Shaman" ] 24
  let shaman = party |> List.find (fun e -> e.Name = "Shaman")
  let warrior = party |> List.find (fun e -> e.Name = "Warrior")

  let stand (e: Entity) (x: int) = { e with Pos = { X = x; Y = 9 }; Home = Some { X = x; Y = 9 } }

  let mob =
    Content.mob 100 "Grunt" CreatureType.Humanoid 24 100000 1000 None [] None { X = 15; Y = 9 }

  let w =
    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities = [ stand warrior 10; stand shaman 11; mob ]
      Pending = []
      Rng = Rng.streamsOf 5UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let me (w: World) = w.Entities |> List.find (fun e -> e.Id = shaman.Id)

  let spend (f: Entity -> Entity) (w: World) =
    { w with Entities = w.Entities |> List.map (fun e -> if e.Id = shaman.Id then f e else e) }

  // Read the row's own numbers rather than restating them: the test is about the
  // mechanism, so the globes and the purse come from the kit.
  let shield = shaman.Abilities |> List.find (fun a -> a.Name = "Water Shield")

  let globes, globeMana =
    shield.Effects
    |> List.pick (function RaiseShield(c, gm, _, _) -> Some(c, gm) | _ -> None)

  let charges (w: World) =
    me w
    |> fun e -> e.Auras |> List.tryPick (function WaterShield(c, _, _, _, _) -> Some c | _ -> None)

  // Stable Shields is what makes this nine; three is the bare row.
  Assert.True((globes = 9), "three base globes plus Stable Shields' six")

  let up = SimCombat.resolveAbility shaman.Id shaman.Id shield w
  Assert.Equal(Some globes, charges up)

  // A pull, so `PartyAi.decide` has a target to work from.
  let pulled = SimCombat.dealDamage mob.Id 1 up
  Assert.True(SimState.inCombat pulled, "the pull should put the party in combat")

  let ordersShield (w: World) =
    PartyAi.decide w
    |> List.exists (fun c ->
      match c.Kind with
      | UseAbility(a, name, _) -> a = shaman.Id && name = "Water Shield"
      | _ -> false)

  // Nine globes standing is a shield, and a shield is not recast.
  Assert.False(ordersShield pulled, "a shield with globes left is not recast")

  // Low enough that no payback can be clipped, high enough to afford the recast.
  let poor = spend (fun e -> { e with Resource = 200 }) pulled

  let struck = SimCombat.dealDamage shaman.Id 10 poor
  Assert.Equal(Some(globes - 1), charges struck)
  Assert.Equal(200 + globeMana, (me struck).Resource)

  // The rest of the globes, one hit each, until the last one is gone.
  let left = globes - 1

  let emptied =
    [ 1..left ] |> List.fold (fun w _ -> SimCombat.dealDamage shaman.Id 10 w) struck

  Assert.Equal(None, charges emptied)
  Assert.Equal(200 + globes * globeMana, (me emptied).Resource)

  Assert.True(
    emptied.Log |> List.exists (fun l -> l.Contains "Water Shield is spent"),
    "the last globe should be announced — there is no timer to fall off"
  )

  // A tenth hit has no globe to take: the aura is gone, not merely empty, so nothing
  // pays and nothing is silently re-armed.
  let tenth = SimCombat.dealDamage shaman.Id 10 emptied
  Assert.Equal((me emptied).Resource, (me tenth).Resource)
  Assert.Equal(None, charges tenth)

  // And now, and only now, the Shaman puts it back up.
  Assert.True(ordersShield emptied, "an empty-handed Shaman should recast Water Shield")

/// Change 5b. Improved Water Shield is a percentage of the Shaman's own regeneration,
/// read off the **live** globe count — so a talented Shaman standing behind an empty bar
/// regenerates at the rate it would have without the talent at all. That is the whole
/// content of the change and the reason the assertion below is an ordering rather than a
/// ratio: the regeneration tick truncates to whole mana, so two percent of a
/// fifteen-a-tick rate is a third of a point and the pool delta per globe is not
/// divisible. What the tick really does — the flat five-second half — is exact, so that
/// half is asserted exactly.
[<Fact>]
let ``improved water shield regeneration is paid for by the globes still standing`` () =
  let party = Content.partyOf [ "Shaman" ] 24
  let shaman = party |> List.find (fun e -> e.Name = "Shaman")

  let full, globeMana, regenBp, mp5 =
    (shaman.Abilities |> List.find (fun a -> a.Name = "Water Shield"))
      .Effects
    |> List.pick (function RaiseShield(c, gm, bp, m) -> Some(c, gm, bp, m) | _ -> None)

  // The talent has to be on the character for this test to mean anything, and it has
  // to be worth more than nothing.
  Assert.True(regenBp > 0, "the operator's build takes Improved Water Shield")
  Assert.True(mp5 > 0, "and the flat half of the same talent")

  // Ten seconds: long enough for several regeneration ticks and two five-second ones.
  let window = 200

  let poolOver (n: int) (auras: Aura list) =
    let w =
      { Tick = ticks 0
        RestTicks = ticks 0
        Grid = Grid.create 20 10
        Entities =
          [ { shaman with
                Resource = 0
                Auras = auras
                Pos = { X = 9; Y = 5 }
                Home = Some { X = 9; Y = 5 } }
            // A dormant but engaged hostile is the cheapest way to be *in combat*:
            // `inCombat` asks only for an engaged living hostile, while `aliveHostiles`
            // skips dormant bodies, so nothing targets it and nothing is cast. That
            // matters twice over — out of combat the sim restores the bar at 10% a tick
            // instead of regenerating it, and the restore clamps at the pool, which is
            // why this assertion used to read no signal at all: every arm saturated.
            { shaman with
                Id = EntityId 99
                Faction = Hostile
                Engaged = true
                Dormant = true
                Resource = 0
                Auras = [] } ]
        Pending = []
        Rng = Rng.streamsOf 5UL
        PartyBuffs = []
        Quests = []
        Forecast = Forecast.empty
        TotemLedger = TotemLedger.empty
        Log = [] }

    (List.fold (fun w _ -> SimTick.step [] w) w [ 1..n ]).Entities
    |> List.find (fun e -> e.Id = shaman.Id)
    |> fun e -> e.Resource

  let poolAfter = poolOver window

  let shield (charges: int) (bp: int) (flat: int) =
    [ Aura.WaterShield(charges, full, globeMana, bp, flat) ]

  let bare = poolAfter []
  let one = poolAfter (shield 1 regenBp mp5)
  let third = poolAfter (shield (full / 3) regenBp mp5)
  let all = poolAfter (shield full regenBp mp5)

  // The talent is not a bonus for having been taken: with no shield up the Shaman gains
  // exactly what a shieldless Shaman gains.
  Assert.Equal(bare, poolAfter [])
  Assert.True(all > bare, "a full shield should regenerate more than no shield")
  Assert.True(third >= bare, "a third of a shield should regenerate no less than none")
  Assert.True(all > third, "and a full one more than a third — the count is live, not the talent")

  // The flat half is MP5 in the era's sense and is not scaled by the globes: one globe
  // and nine pay the same nine mana a five-second window, and both pay it on every one
  // of the window's five-second marks. Five seconds is a hundred Ticks at twenty a
  // second — not five, which is what this counted before the clock was fixed.
  let windows = window / int (secTicks 5.0)

  Assert.Equal(windows * mp5, one - poolAfter (shield 1 regenBp 0))
  Assert.Equal(windows * mp5, all - poolAfter (shield full regenBp 0))

/// Change 5c. Undertow is one roll with two halves, and the inversion is the talent: a
/// Healing Wave against a **torn** shield hands a globe back, and against a **full** one
/// spends one instead. Nine is the ceiling, so the only direction a full shield can move
/// is down and the only direction a torn one can move is up — which is what makes the
/// direction of the first observed change the assertion.
[<Fact>]
let ``undertow tops a torn shield up and spends a globe of a full one`` () =
  let party = Content.partyOf [ "Shaman" ] 24
  let shaman = party |> List.find (fun e -> e.Name = "Shaman")

  let heal = shaman.Abilities |> List.find (fun a -> a.Name = "Healing Wave")

  let full, globeMana, regenBp, mp5 =
    (shaman.Abilities |> List.find (fun a -> a.Name = "Water Shield"))
      .Effects
    |> List.pick (function RaiseShield(c, gm, bp, m) -> Some(c, gm, bp, m) | _ -> None)

  Assert.True(shaman.Talents.UndertowBp > 0, "the operator's build takes Undertow")

  let at (start: int) (seed: uint64) =
    let auras =
      if start = 0 then [] else [ Aura.WaterShield(start, full, globeMana, regenBp, mp5) ]

    { Tick = ticks 0
      RestTicks = ticks 0
      Grid = Grid.create 20 10
      Entities =
        [ { shaman with
              Resource = shaman.MaxResource
              Auras = auras
              Pos = { X = 9; Y = 5 }
              Home = Some { X = 9; Y = 5 } } ]
      Pending = []
      Rng = Rng.streamsOf 1UL
      PartyBuffs = []
      Quests = []
      Forecast = Forecast.empty
      TotemLedger = TotemLedger.empty
      Log = [] }

  let charges (w: World) =
    w.Entities
    |> List.find (fun e -> e.Id = shaman.Id)
    |> fun e -> e.Auras |> List.tryPick (function WaterShield(c, _, _, _, _) -> Some c | _ -> None)

  // Forty casts at the talent's own chance: if the proc never fired the loop would
  // answer `None` and every assertion below would fail loudly rather than pass on one
  // lucky world. The pool is refilled between casts so affordability never truncates
  // the sample.
  let firstMove (start: int) (seed: uint64) =
    let refill (w: World) =
      { w with
          Entities =
            w.Entities
            |> List.map (fun e -> if e.Id = shaman.Id then { e with Resource = e.MaxResource } else e) }

    let rec loop (n: int) (w: World) =
      if n = 0 then
        None
      else
        let w = SimCombat.resolveAbility shaman.Id shaman.Id heal w |> refill

        match charges w with
        | Some c when c <> start -> Some c
        | None when start > 0 -> Some 0
        | _ -> loop (n - 1) w

    loop 40 (at start seed)

  // Torn: both halves of the shield pay out upwards, one charge at a time.
  Assert.Equal(Some(full - 3), firstMove (full - 4) 11UL)
  Assert.Equal(Some full, firstMove (full - 1) 12UL)

  // Full: the same roll spends instead. Ten is not on the table, so a restore here
  // would be a fail.
  Assert.Equal(Some(full - 1), firstMove full 13UL)

  // And a Shaman with no shield at all gets nothing: the proc acts on an aura, it does
  // not create one.
  Assert.Equal(None, firstMove 0 14UL)

// ===========================================================================
// Vanish and Blind — the Rogue's last resort
// ===========================================================================

/// The last resort's proving ground: a Tank, a Rogue, and one Mob that has
/// decided the Rogue is the problem. Threat on both members, so the Mob has a
/// table to read and a choice to make.
let private escapeWorld (rogue: Entity) : World =
  let tap = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let tank =
    Content.hero 1 "Tank" Tank 1000 1000 tap 50000 [ Content.heroicStrike ] { X = 5; Y = 5 }

  let mob =
    Content.mob 100 "Mob" CreatureType.Humanoid 21 10000 1000 (Some tap) [] None { X = 7; Y = 5 }

  let mob =
    { mob with
        Engaged = true
        Target = Some rogue.Id
        Threat = Map.ofList [ EntityId 1, 1000; rogue.Id, 500 ] }

  { Tick = ticks 0
    RestTicks = ticks 0
    Grid = Grid.create 20 10
    Entities = [ tank; rogue; mob ]
    Pending = []
    Rng = Rng.streamsOf 1UL
    PartyBuffs = []
    Quests = []
    Forecast = Forecast.empty
    TotemLedger = TotemLedger.empty
    Log = [] }

/// One press of Vanish is the whole contract: the Mob's threat table forgets the
/// Rogue, the gaze already on it stops, and the stealth rides on its own aura
/// until the Rogue's next attack ends it.
[<Fact>]
let ``vanish wipes the rogue's threat and takes it out of the mob's gaze`` () =
  let tap = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let rogue =
    Content.hero 2 "Rogue" Dps 1000 1000 tap 10000 [ (AbilityDef.at 22 Content.vanish).Value ] { X = 8; Y = 5 }

  let w = escapeWorld rogue
  let w = SimCombat.resolveAbility (EntityId 2) (EntityId 2) (AbilityDef.at 22 Content.vanish).Value w

  Assert.False(Map.containsKey (EntityId 2) (mobOf w).Threat)
  Assert.NotEqual<EntityId option>(Some(EntityId 2), (mobOf w).Target)
  Assert.True(w.Entities |> List.exists (fun e -> e.Id = EntityId 2 && SimState.isStealthed e))

/// A stealthed member is invisible to targeting: not the nearest-noticed
/// member, and not even a forced one.
[<Fact>]
let ``a stealthed member is not chosen as a target even when nearest`` () =
  // 6,5 is the Mob's own tile, nearer than the Tank at 5,5 — with no threat on
  // the table, nearest is the whole rule.
  let baseWorld = arena 0 0 { X = 6; Y = 5 }
  Assert.Equal<EntityId option>(Some(EntityId 2), SimState.resolveTarget (mobOf baseWorld) baseWorld)

  let w =
    SimState.mapEntity (EntityId 2) (fun e -> { e with Auras = [ Stealthed(secTicks 30.0) ] }) baseWorld

  Assert.Equal<EntityId option>(Some(EntityId 1), SimState.resolveTarget (mobOf w) w)

  // A taunt-forced gaze refuses the unseen body as surely as a noticed one.
  let forced = { (mobOf w) with ForcedTarget = Some(EntityId 2) }
  Assert.Equal<EntityId option>(Some(EntityId 1), SimState.resolveTarget forced w)

/// Blind buys ten seconds, and only until something lands: a `Sleep`, so the
/// first damage on the sleeper is what frees it.
[<Fact>]
let ``blind sleeps an attacker, and damage on it breaks the sleep`` () =
  let tap = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let rogue =
    Content.hero 2 "Rogue" Dps 1000 1000 tap 10000 [ (AbilityDef.at 34 Content.blind).Value ] { X = 8; Y = 5 }

  let w = escapeWorld rogue
  let w = SimCombat.resolveAbility (EntityId 2) (EntityId 100) (AbilityDef.at 34 Content.blind).Value w

  Assert.True(SimState.isSleeping (mobOf w))
  // Ten seconds, not Sap's thirty: the breather is sized for the panic that spent it.
  Assert.True(mobOf w |> fun m -> m.Auras |> List.exists (function Sleeping r -> r = secTicks 10.0 | _ -> false))

  let w = SimCombat.dealDamage (EntityId 100) 1 w
  Assert.False(SimState.isSleeping (mobOf w))

/// The lane's own judgement: a Rogue a quarter of health from the end, with a
/// Mob on it, spends Vanish before any strike — and when Vanish is down the same
/// moment answers with Blind on the hunter in reach.
[<Fact>]
let ``a hunted rogue at low health spends vanish, or blind when vanish is down`` () =
  let tap = { Min = 1; Max = 1; Ticks = ticks 100000; Range = 1 }

  let kit = AbilityDef.kitAt 40 [ Content.sinisterStrike; Content.vanish; Content.blind ]

  let rogue = { Content.hero 2 "Rogue" Dps 1000 1000 tap 10000 kit { X = 7; Y = 5 } with Health = 100 }

  Assert.Contains("Vanish", abilitiesFor (EntityId 2) (PartyAi.decide (escapeWorld rogue)))

  let cooldownSpent = { rogue with Cooldowns = Map.ofList [ "Vanish", secTicks 300.0 ] }

  let orders = abilitiesFor (EntityId 2) (PartyAi.decide (escapeWorld cooldownSpent))

  Assert.Contains("Blind", orders)
  Assert.DoesNotContain("Vanish", orders)
