namespace WowRoguelike.Core

open System.Diagnostics

/// The measurement apparatus (Q9: no invented budget, so the benchmark harness
/// IS the discipline) plus a scripted stand-in for the player.
module Bench =

  // =========================================================================
  // A scripted player
  // =========================================================================

  let private ready (e: Entity) (abilityName: string) =
    (e.Cooldowns |> Map.tryFind abilityName |> Option.defaultValue (ticks 0)) <= ticks 0

  let private has (e: Entity) (abilityName: string) =
    e.Abilities |> List.exists (fun a -> a.Name = abilityName)

  /// Off cooldown and affordable. Affordability matters now: a caster that keeps
  /// ordering spells while dry just fills the log.
  let private canUse (e: Entity) (abilityName: string) =
    ready e abilityName
    && (e.Abilities
        |> List.tryFind (fun a -> a.Name = abilityName)
        |> Option.exists (Sim.canAfford e))

  let private within (r: int) (a: Entity) (b: Entity) = Pos.chebyshev a.Pos b.Pos <= r

  /// The same reach rule the Core uses, including its melee exemption. Without
  /// it the scripted player orders spells through walls and just fills the log.
  let private canReach (r: int) (a: Entity) (b: Entity) (w: World) =
    within r a b && (r <= 1 || Path.lineOfSight w.Grid a.Pos b.Pos)

  /// Drives the Party headlessly so benchmarks and golden replays have input.
  ///
  /// This is NOT game AI. The Party is commanded directly by the player (Q5a);
  /// this is a harness fixture that happens to live in Core so that the headless
  /// entry point can reach it without referencing MonoGame.
  let autoPilot (w: World) : Command list =
    let at = w.Tick + ticks 1
    let hostiles = Sim.aliveHostiles w
    let party = Sim.aliveParty w

    // Only members who can actually start something get orders. Re-issuing a
    // cast mid-cast is a queueing policy, not a player action.
    let actors =
      party
      |> List.filter (fun e -> e.Casting.IsNone && not (Sim.isSleeping e))

    let role r = actors |> List.tryFind (fun e -> e.Role = Some r)
    let tank = role Tank

    // Focus whatever the Party is already fighting, else the nearest Mob.
    let focus =
      match tank, hostiles with
      | _, [] -> None
      | None, _ -> List.tryHead hostiles
      | Some t, _ ->
        match hostiles |> List.filter (fun h -> h.Engaged) with
        | [] -> hostiles |> List.sortBy (fun h -> Pos.chebyshev t.Pos h.Pos) |> List.tryHead
        | engaged -> engaged |> List.sortBy (fun h -> Pos.chebyshev t.Pos h.Pos) |> List.tryHead

    let cmd (actor: Entity) (kind: CommandKind) =
      [ { ApplyAt = at
          Kind = kind } ]

    let tankCmds =
      match tank, focus with
      | Some t, Some f ->
        // Pull anything that has torn aggro off the Tank.
        let thief =
          Sim.aliveHostiles w
          |> List.tryFind (fun h -> h.Engaged && h.Target <> Some t.Id && canReach 8 t h w)

        match thief with
        | Some h when has t "Taunt" && canUse t "Taunt" -> cmd t (UseAbility(t.Id, "Taunt", h.Id))
        | _ when within 1 t f && canUse t "Heroic Strike" ->
          cmd t (UseAbility(t.Id, "Heroic Strike", f.Id))
        | _ when not (within 1 t f) -> cmd t (MoveTo(t.Id, f.Pos))
        | _ -> []
      | _ -> []

    let healerCmds =
      match role Healer with
      | None -> []
      | Some h ->
        let wounded =
          party
          |> List.filter (fun e -> Sim.healthPct e < 70)
          |> List.sortBy Sim.healthPct
          |> List.tryHead

        match wounded with
        | Some target when canReach 8 h target w && canUse h "Lesser Heal" ->
          cmd h (UseAbility(h.Id, "Lesser Heal", target.Id))
        | Some target when canReach 8 h target w -> []
        | Some target -> cmd h (MoveTo(h.Id, target.Pos))
        | None -> []

    let interruptCmds =
      match role Dps with
      | None -> []
      | Some r ->
        // The Party's only interrupt is melee range, so closing is the job.
        let caster =
          Sim.aliveHostiles w
          |> List.tryFind (fun h ->
            h.Casting |> Option.exists (fun c -> c.Ability.Interruptible))

        match caster with
        | Some c when canUse r "Kick" && within 1 r c -> cmd r (UseAbility(r.Id, "Kick", c.Id))
        | Some c when not (within 1 r c) -> cmd r (MoveTo(r.Id, c.Pos))
        | _ ->
          match focus with
          | Some f when not (within 1 r f) -> cmd r (MoveTo(r.Id, f.Pos))
          | _ -> []

    let rangedCmds =
      actors
      |> List.filter (fun e -> e.Role = Some Dps)
      |> List.collect (fun e ->
        match focus with
        | None -> []
        | Some f ->
          let shot =
            e.Abilities
            |> List.filter (fun a -> a.TargetKind = Enemy && a.Range > 1)
            |> List.tryFind (fun a -> canUse e a.Name)

          match shot with
          | Some a when canReach a.Range e f w -> cmd e (UseAbility(e.Id, a.Name, f.Id))
          | _ when not (within 8 e f) -> cmd e (MoveTo(e.Id, f.Pos))
          | _ -> [])

    tankCmds @ healerCmds @ interruptCmds @ rangedCmds

  /// Play the encounter to a conclusion, or to `maxTicks`.
  let runEncounter (seed: uint64) (maxTicks: int) : World =
    let rec loop (w: World) (remaining: int) =
      if remaining <= 0 || Sim.outcome w <> Running then
        w
      else
        loop (Sim.step (autoPilot w) w) (remaining - 1)

    loop (Content.gully seed) maxTicks

  // =========================================================================
  // Scale fixtures
  // =========================================================================

  /// A deliberately large, deliberately flat world: the Party cannot die and the
  /// Mobs are effectively immortal, so the tick loop runs at a fixed entity
  /// count for as long as you want to measure it. This is the knob that makes
  /// the optimisation work a real problem rather than a self-imposed one (Q26).
  let synthetic (hostileCount: int) (seed: uint64) : World =
    let cols = max 8 (int (ceil (sqrt (float hostileCount))))
    let width = cols + 6
    let height = cols + 6
    let tap = { Min = 1; Max = 2; Ticks = secTicks 1.0; Range = 1 }
    let forever = 100000000

    // Resource is made effectively infinite here too, so the fixture measures a
    // steady state instead of drifting as casters run dry.
    let party =
      [ Content.hero 1 "Tank" Tank forever forever tap 50000 [ Content.heroicStrike; Content.taunt ] { X = 1; Y = 1 }
        Content.hero 2 "Healer" Healer forever forever tap 10000 [ Content.lesserHeal ] { X = 1; Y = 2 }
        Content.hero 3 "Rogue" Dps forever forever tap 10000 [ Content.kick ] { X = 1; Y = 3 }
        Content.hero 4 "Mage" Dps forever forever tap 10000 [ Content.fireball ] { X = 2; Y = 1 }
        Content.hero 5 "Hunter" Dps forever forever tap 10000 [ Content.arcaneShot ] { X = 2; Y = 2 } ]

    let mobs =
      [ for i in 0 .. hostileCount - 1 ->
          let x = 3 + (i % (width - 3))
          let y = 3 + (i / (width - 3)) % (height - 3)

          let m =
            Content.mob
              (100 + i)
              "Synthetic"
              forever
              forever
              (Some tap)
              [ Content.lightningBolt; Content.healingTouch; Content.druidsSlumber; Content.serpentForm ]
              None
              { X = x; Y = y }

          { m with
              Engaged = true
              Target = Some(EntityId 1)
              Threat = Map.ofList [ EntityId 1, 1000 ] } ]

    { Tick = ticks 0
      Grid = Grid.create width height
      Entities = party @ mobs
      Pending = []
      Rng = Rng.streamsOf seed
      Log = [] }

  // =========================================================================
  // Timing
  // =========================================================================

  let timeStep (ticksToRun: int) (warmup: int) (w: World) : float =
    let mutable world = w

    for _ in 1..warmup do
      world <- Sim.step [] world

    let sw = Stopwatch.StartNew()

    for _ in 1..ticksToRun do
      world <- Sim.step [] world

    sw.Stop()
    sw.Elapsed.TotalMilliseconds / float ticksToRun

  let report (label: string) (ticksToRun: int) (w: World) : string =
    // Warm up on a quarter of the sample, so the measured loop dominates.
    let perTick = timeStep ticksToRun (max 1 (ticksToRun / 4)) w

    sprintf
      "%-28s entities=%4d  %9.4f ms/tick  %10.0f ticks/sec  (%d ticks)"
      label
      (List.length w.Entities)
      perTick
      (1000.0 / perTick)
      ticksToRun

  // =========================================================================
  // Search and heap fixtures (Q2c / Q26a)
  // =========================================================================

  /// A room with a closed cup in the middle and the goal outside it. Greedy
  /// walks into the bottom of the cup and stops; a real search walks out the top
  /// and the whole way around.
  let concaveMap =
    [ "######################"
      "#....................#"
      "#....#..........#....#"
      "#....#..........#....#"
      "#....#..........#....#"
      "#....#..........#....#"
      "#....############....#"
      "#....................#"
      "######################" ]

  let concaveStart = { X = 11; Y = 3 }
  let concaveGoal = { X = 11; Y = 7 }

  /// The same room run through all three searches, so the difference between
  /// them is a number rather than an opinion.
  let concaveReport () =
    let g = Grid.ofRows concaveMap
    let nothing (_: Pos) = false
    let a = Path.astar g nothing concaveStart concaveGoal
    let d = Path.dijkstra g nothing concaveStart concaveGoal
    let gr = Path.greedy g nothing concaveStart concaveGoal

    let reach (r: Path.Result) = if List.isEmpty r.Path then "no path" else string (List.length r.Path)

    sprintf
      "cup room: A* %s steps in %d expansions | Dijkstra %s steps in %d | greedy %s in %d"
      (reach a)
      a.Expanded
      (reach d)
      d.Expanded
      (reach gr)
      gr.Expanded

  /// A grid with walls scattered at roughly `density` percent, with the corners
  /// forced open so that a route usually exists.
  let randomMap (width: int) (height: int) (density: int) (seed: uint64) : Grid =
    let g = Grid.create width height
    let mutable rng = Rng.streamsOf seed

    for y in 1 .. height - 2 do
      for x in 1 .. width - 2 do
        let v, r = Rng.drawCombat 0 99 rng
        rng <- r

        if v < density then
          g.Walls.[y * width + x] <- true

    for p in [ { X = 1; Y = 1 }; { X = width - 2; Y = height - 2 } ] do
      g.Walls.[p.Y * width + p.X] <- false

    g

  /// Average expansions across random rooms. The A*-versus-Dijkstra ratio is the
  /// heuristic's value made visible, and the disagreement count is the
  /// optimality check: A* must never return a more expensive route.
  let compareSearches (maps: int) : string =
    let mutable astarExpand = 0
    let mutable dijkstraExpand = 0
    let mutable greedySteps = 0
    let mutable found = 0
    let mutable greedyFound = 0
    let mutable disagree = 0

    for i in 1..maps do
      let g = randomMap 24 16 22 (900UL + uint64 i)
      let start = { X = 1; Y = 1 }
      let goal = { X = 22; Y = 14 }
      let nothing (_: Pos) = false

      let a = Path.astar g nothing start goal
      let d = Path.dijkstra g nothing start goal
      let gr = Path.greedy g nothing start goal

      astarExpand <- astarExpand + a.Expanded
      dijkstraExpand <- dijkstraExpand + d.Expanded
      greedySteps <- greedySteps + gr.Expanded

      if not (List.isEmpty a.Path) then
        found <- found + 1

        if Path.pathCost start a.Path <> Path.pathCost start d.Path then
          disagree <- disagree + 1

      if not (List.isEmpty gr.Path) then
        greedyFound <- greedyFound + 1

    sprintf
      "%d random rooms: A* %d nodes (%.1f/room), Dijkstra %d (%.1f/room, %.2fx A*), greedy reached %d/%d (%d steps), A*/Dijkstra cost disagreements %d"
      maps
      astarExpand
      (float astarExpand / float maps)
      dijkstraExpand
      (float dijkstraExpand / float maps)
      (float dijkstraExpand / float (max 1 astarExpand))
      greedyFound
      found
      greedySteps
      disagree

  /// The hand-rolled heap against the BCL's, over one workload.
  let compareHeaps (count: int) : string =
    let priorities =
      Array.init count (fun i -> int ((int64 i * 2654435761L) % 1000000L))

    let sw = Stopwatch.StartNew()
    let mine = MinHeap<int, int>()

    for p in priorities do
      mine.Push(p, p)

    let popped = System.Collections.Generic.List<int>()
    let mutable go = true

    while go do
      match mine.Pop() with
      | Some(p, _) -> popped.Add p
      | None -> go <- false

    let mineMs = sw.Elapsed.TotalMilliseconds

    let ordered =
      popped |> Seq.toList = (popped |> Seq.toList |> List.sort)

    let sw2 = Stopwatch.StartNew()
    let bcl = System.Collections.Generic.PriorityQueue<int, int>()

    for p in priorities do
      bcl.Enqueue(p, p)

    while bcl.Count > 0 do
      bcl.Dequeue() |> ignore

    let bclMs = sw2.Elapsed.TotalMilliseconds

    sprintf
      "heap n=%d: hand-rolled %.2f ms (pops in order: %b), BCL PriorityQueue %.2f ms"
      count
      mineMs
      ordered
      bclMs

  // =========================================================================
  // Decomposing the tick cost (task 11)
  // =========================================================================

  /// How many entities want to move, and how many will therefore run a search
  /// this tick. `Sim.stepToward` replans exactly when a goal is set and either the
  /// stored path is empty or its next tile has been taken, so the second number
  /// is A* calls per tick.
  let searchPressure (w: World) =
    let movers =
      w.Entities |> List.filter (fun e -> Sim.alive e && e.Goal.IsSome)

    let planning =
      movers
      |> List.filter (fun e ->
        match e.Path with
        | [] -> true
        | next :: _ -> Sim.claimedByOther w e.Id next)

    List.length movers, List.length planning

  /// Cost per tick of a *driven* fight.
  ///
  /// Timing `Sim.step []` on a fight instead measures its aftermath: with no
  /// input the Priest stops healing, the Party dies, the world resolves, and every
  /// subsequent tick is nearly free. That is why the old "encounter" figures read
  /// as two orders of magnitude cheaper than the search cost they supposedly
  /// contained.
  let fightCost (seed: uint64) (ticks: int) : string =
    let mutable w = Content.gully seed
    // A short warmup only. Warming up for a quarter of the budget silently
    // measures the settled endgame, where almost nothing paths.
    let warmup = min 50 (ticks / 10)

    for _ in 1..warmup do
      w <- Sim.step (autoPilot w) w

    let from = int w.Tick
    let sw = Stopwatch.StartNew()
    let mutable live = 0

    for _ in 1..(ticks - warmup) do
      if Sim.outcome w = Running then
        w <- Sim.step (autoPilot w) w
        live <- live + 1

    sw.Stop()

    sprintf
      "driven fight: %.4f ms/tick over %d live ticks (ticks %d..%d), %A"
      (sw.Elapsed.TotalMilliseconds / float (max 1 live))
      live
      from
      (int w.Tick)
      (Sim.outcome w)

  /// The same fight, split into the scripted player's cost and `Sim.step`'s.
  ///
  /// `fightCost` times both together. If these two disagree with it, the cost is
  /// in the harness fixture rather than in the simulation, and chasing the Core
  /// would be chasing the wrong thing.
  let fightSplitCost (seed: uint64) (ticks: int) : string =
    let mutable w = Content.gully seed
    let warmup = min 50 (ticks / 10)

    for _ in 1..warmup do
      w <- Sim.step (autoPilot w) w

    let clock = Stopwatch()
    let mutable pilotMs = 0.0
    let mutable stepMs = 0.0
    let mutable live = 0

    for _ in 1..(ticks - warmup) do
      if Sim.outcome w = Running then
        clock.Restart()
        let commands = autoPilot w
        pilotMs <- pilotMs + clock.Elapsed.TotalMilliseconds
        clock.Restart()
        w <- Sim.step commands w
        stepMs <- stepMs + clock.Elapsed.TotalMilliseconds
        live <- live + 1

    let n = float (max 1 live)

    sprintf
      "seed %d over %d ticks: autopilot %.4f ms/tick, Sim.step %.4f ms/tick"
      seed
      live
      (pilotMs / n)
      (stepMs / n)

  /// The cost of a search that cannot succeed, against one that can.
  ///
  /// A Mob aims at its target's tile, which its target is standing on, so the
  /// goal is reachable only if some neighbour of it is free. When a Party clusters
  /// into a corner pocket there is no such neighbour, the search fails, and a
  /// failed search expands the whole reachable component instead of stopping
  /// early. `Sim.stepToward` then throws the empty path away and repeats the same
  /// doomed search on the very next tick.
  let searchFailureCost (runs: int) : string =
    let g = (Content.gully 1UL).Grid
    let start = { X = 18; Y = 10 }
    let goal = { X = 1; Y = 6 }

    // Seal every neighbour of the goal. (0,y) is already the map border.
    let box =
      [ { X = 1; Y = 5 }
        { X = 2; Y = 5 }
        { X = 2; Y = 6 }
        { X = 2; Y = 7 }
        { X = 1; Y = 7 } ]

    let measure (blocked: Pos -> bool) =
      Path.astar g blocked start goal |> ignore
      let sw = Stopwatch.StartNew()
      let mutable expansions = 0

      for _ in 1..runs do
        expansions <- expansions + (Path.astar g blocked start goal).Expanded

      sw.Stop()
      sw.Elapsed.TotalMilliseconds / float runs, float expansions / float runs

    let openMs, openExpansions = measure (fun _ -> false)
    let sealedMs, sealedExpansions = measure (fun p -> List.contains p box)

    sprintf
      "same grid, same endpoints: reachable goal %.4f ms / %.0f expansions | sealed goal %.4f ms / %.0f expansions (%.1fx)"
      openMs
      openExpansions
      sealedMs
      sealedExpansions
      (sealedMs / openMs)

  /// `Sim.step` cost bucketed across a fight, with A* call counts alongside so the
  /// two can be correlated rather than assumed related.
  ///
  /// Spread cost means a per-tick rule is expensive everywhere. Concentrated cost
  /// means one episode is, and the state at that tick is the thing to read.
  let stepProfile (seed: uint64) (ticks: int) (bucket: int) : string =
    let mutable w = Content.gully seed

    for _ in 1..50 do
      w <- Sim.step (autoPilot w) w

    let sb = System.Text.StringBuilder()
    let clock = Stopwatch()
    let mutable bucketMs = 0.0
    let mutable bucketReplans = 0
    let mutable bucketStart = int w.Tick
    let mutable worstMs = 0.0
    let mutable worstTick = 0
    let mutable worstWorld = w

    for _ in 1..(ticks - 50) do
      if Sim.outcome w = Running then
        let _, replans = searchPressure w
        let commands = autoPilot w
        clock.Restart()
        w <- Sim.step commands w
        let ms = clock.Elapsed.TotalMilliseconds
        bucketMs <- bucketMs + ms
        bucketReplans <- bucketReplans + replans

        if ms > worstMs then
          worstMs <- ms
          worstTick <- int w.Tick
          worstWorld <- w

        if (int w.Tick - bucketStart) >= bucket then
          sb.AppendLine(
            sprintf
              "  ticks %4d-%4d  %8.4f ms/tick  %5.2f A* calls/tick"
              bucketStart
              (int w.Tick)
              (bucketMs / float (int w.Tick - bucketStart))
              (float bucketReplans / float (int w.Tick - bucketStart))
          )
          |> ignore

          bucketMs <- 0.0
          bucketReplans <- 0
          bucketStart <- int w.Tick

    sb.AppendLine(sprintf "worst single tick: %d at %.4f ms" worstTick worstMs)
    |> ignore

    sb.Append(Dump.world worstWorld) |> ignore
    sb.ToString()

  /// Search pressure sampled every tick across a whole fight. The spread in these
  /// numbers is why per-tick cost is not one number: a long sample averages the
  /// pull phase against the settled phase, and a short one may see only one of
  /// them.
  let pressureProfile (seed: uint64) (ticks: int) : string =
    let mutable w = Content.gully seed
    let planning = ResizeArray<int>()
    let mutable started = 0

    while started < ticks && Sim.outcome w = Running do
      let _, replans = searchPressure w
      planning.Add replans
      w <- Sim.step (autoPilot w) w
      started <- started + 1

    let sorted = planning |> Seq.sort |> Seq.toArray

    let at percentile =
      if Array.isEmpty sorted then
        0
      else
        sorted.[min (Array.length sorted - 1) (percentile * Array.length sorted / 100)]

    let mean =
      if Array.isEmpty sorted then 0.0 else Array.averageBy float sorted

    sprintf
      "fight pressure, %d ticks: A* calls/tick mean %.2f, p50 %d, p95 %d, max %d"
      started
      mean
      (at 50)
      (at 95)
      (at 100)

  /// Evenly-ish spread pairs of floor tiles for a search benchmark.
  let samplePairs (g: Grid) (count: int) =
    let floor = Grid.tiles g |> Seq.filter (Grid.isFloor g) |> Seq.toArray

    [ for i in 0..count - 1 ->
        let a = floor.[i * 7 % floor.Length]
        let b = floor.[i * 13 % floor.Length]
        if a = b then a, floor.[(i + 1) % floor.Length] else a, b ]

  /// Time the search with no game around it. This is the "internals" half of the
  /// cost, and it is independent of how often the search is called.
  ///
  /// Measure it on the grid you actually care about. An open dungeon room expands
  /// far fewer nodes than a dense random map, so a figure taken from the wrong
  /// grid is off by an order of magnitude — which is exactly how an earlier
  /// version of this benchmark claimed A* cost more per tick than the whole fight
  /// measured.
  let searchCost (label: string) (grid: Grid) (pairs: (Pos * Pos) list) (runsPer: int) : string =
    let nothing (_: Pos) = false

    for (a, b) in pairs do
      Path.astar grid nothing a b |> ignore

    let sw = Stopwatch.StartNew()
    let mutable expansions = 0
    let mutable found = 0
    let mutable calls = 0

    for _ in 1..runsPer do
      for (a, b) in pairs do
        let r = Path.astar grid nothing a b
        expansions <- expansions + r.Expanded

        if not (List.isEmpty r.Path) then
          found <- found + 1

        calls <- calls + 1

    sw.Stop()

    sprintf
      "%-28s %8.4f ms/search  %7.1f expansions/search  %d calls, %d unreachable"
      label
      (sw.Elapsed.TotalMilliseconds / float calls)
      (float expansions / float calls)
      calls
      (calls - found)
