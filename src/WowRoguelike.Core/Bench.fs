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

  let private within (r: int) (a: Entity) (b: Entity) = Pos.chebyshev a.Pos b.Pos <= r

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
          |> List.tryFind (fun h -> h.Engaged && h.Target <> Some t.Id && within 8 t h)

        match thief with
        | Some h when has t "Taunt" && ready t "Taunt" -> cmd t (UseAbility(t.Id, "Taunt", h.Id))
        | _ when within 1 t f && has t "Heroic Strike" && ready t "Heroic Strike" ->
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
        | Some target when within 8 h target -> cmd h (UseAbility(h.Id, "Lesser Heal", target.Id))
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
        | Some c when has r "Kick" && ready r "Kick" && within 1 r c ->
          cmd r (UseAbility(r.Id, "Kick", c.Id))
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
            |> List.tryFind (fun a -> ready e a.Name)

          match shot with
          | Some a when within a.Range e f -> cmd e (UseAbility(e.Id, a.Name, f.Id))
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

    let party =
      [ Content.hero 1 "Tank" Tank forever tap 50000 [ Content.heroicStrike; Content.taunt ] { X = 1; Y = 1 }
        Content.hero 2 "Healer" Healer forever tap 10000 [ Content.lesserHeal ] { X = 1; Y = 2 }
        Content.hero 3 "Rogue" Dps forever tap 10000 [ Content.kick ] { X = 1; Y = 3 }
        Content.hero 4 "Mage" Dps forever tap 10000 [ Content.fireball ] { X = 2; Y = 1 }
        Content.hero 5 "Hunter" Dps forever tap 10000 [ Content.arcaneShot ] { X = 2; Y = 2 } ]

    let mobs =
      [ for i in 0 .. hostileCount - 1 ->
          let x = 3 + (i % (width - 3))
          let y = 3 + (i / (width - 3)) % (height - 3)

          let m =
            Content.mob
              (100 + i)
              "Synthetic"
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
    let perTick = timeStep ticksToRun 50 w

    sprintf
      "%-28s entities=%4d  %9.4f ms/tick  %10.0f ticks/sec"
      label
      (List.length w.Entities)
      perTick
      (1000.0 / perTick)

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
