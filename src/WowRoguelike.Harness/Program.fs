module WowRoguelike.Harness.Program

open WowRoguelike.Core
open WowRoguelike.Story

let private usage () =
  printfn "sim demo [seed] [maxTicks]  play the encounter with the scripted party, show the log"
  printfn "sim fingerprint [seed]      canonical state text for golden-replay comparison"
  printfn "sim bench                   time the tick loop at several entity counts"
  printfn "sim profile                 split the tick cost into search frequency and search internals"
  printfn "sim diagnose                bucket SimTick.step cost across a fight and dump the worst tick"
  printfn "sim dungeon [seed]          generate a floorplan and render it as ASCII"
  printfn "sim maps                   render the Wailing Caverns rooms extracted from images/"
  printfn "sim wc                     render the stitched Wailing Caverns dungeon"
  printfn "sim sm [wing] [min|median|max]  render a Scarlet Monastery wing at one difficulty"
  printfn "sim runs [n] [maxTicks]     run n seeds and summarise outcomes"
  printfn "sim snapshots [n]           one stable line per seed, the snapshot test's input"
  printfn "sim story [-i]             play the Wailing Caverns story (auto-picks choices)"

let private nth (args: string list) (i: int) = args |> List.tryItem i

let private seedOf (args: string list) (fallback: uint64) =
  match nth args 0 with
  | Some s ->
    match System.UInt64.TryParse s with
    | true, v -> v
    | _ -> fallback
  | None -> fallback

let private intOf (args: string list) (i: int) (fallback: int) =
  match nth args i with
  | Some s ->
    match System.Int32.TryParse s with
    | true, v -> v
    | _ -> fallback
  | None -> fallback

let private demo (seed: uint64) (maxTicks: int) =
  let w = Bench.runEncounter seed maxTicks

  printfn "seed=%d tick=%d outcome=%A" seed w.Tick (SimState.outcome w)
  printfn ""
  printfn "--- combat log (oldest first) ---"
  // The whole log, not `Dump.log`'s tail: the harness is where a fight is read,
  // and a truncated log cannot be summed per attacker.
  printfn "%s" (w.Log |> List.rev |> String.concat "\n")
  printfn ""
  printfn "--- final state ---"
  printfn "%s" (Dump.world w)

let private runs (n: int) (maxTicks: int) =
  let results =
    [ 1UL .. uint64 n ] |> List.map (fun s -> Bench.runEncounter s maxTicks)

  let count o = results |> List.filter (fun w -> SimState.outcome w = o) |> List.length
  let cleared = count EncounterCleared
  let wiped = count PartyWiped

  let ticks = results |> List.map (fun w -> int w.Tick) |> List.sort

  printfn
    "seeds=%d cleared=%d wiped=%d unresolved=%d median-ticks=%d min=%d max=%d"
    n
    cleared
    wiped
    (n - cleared - wiped)
    (List.item (n / 2) ticks)
    (List.head ticks)
    (List.last ticks)

let private bench () =
  printfn "%-28s %s" "fixture" "result"

  // Sample sizes are explicit per fixture, because a tick's cost is both steeply
  // superlinear in entity count and non-stationary within a fight: the pull phase
  // does nearly all the pathfinding and the settled phase does almost none. A
  // window small enough to be cheap is a window that only sees one of the two,
  // so each number below is only meaningful next to its own sample size.
  let report label ticks w = printfn "%s" (Bench.report label ticks w)

  // The encounter is measured with the autopilot driving it. `Bench.report`
  // steps a world with no input, which for a fight measures the corpse pile it
  // collapses into rather than the fight.
  //
  // Several seeds, because identical-length fights differ wildly in cost: the
  // figure below is not one number, it is a distribution over seeds.
  for seed in [ 1UL; 2UL; 3UL; 4UL; 5UL ] do
    printfn "%s" (Bench.fightSplitCost seed 3000)
  report "synthetic, 100 hostiles" 300 (Bench.synthetic 100 1UL)
  report "synthetic, 250 hostiles" 60 (Bench.synthetic 250 1UL)
  report "synthetic, 500 hostiles" 20 (Bench.synthetic 500 1UL)

  printfn ""
  printfn "%s" (Bench.concaveReport ())
  printfn "%s" (Bench.compareSearches 50)

  for n in [ 10000; 100000 ] do
    printfn "%s" (Bench.compareHeaps n)

let private playStory (interactive: bool) =
  let runner = StoryRunner(StoryRunner.LoadJson "wailing_caverns.ink.json")
  let mutable running = true
  let mutable steps = 0

  while running && steps < 1000 do
    steps <- steps + 1

    for ev in runner.Continue() do
      match ev with
      | StoryEvent.Text t -> printfn "%s" t
      | StoryEvent.Encounter id -> printfn "\n  >>> ENCOUNTER: %s <<<\n" id
      | StoryEvent.QuestComplete id -> printfn "\n  >>> QUEST COMPLETE: %s <<<\n" id
      | StoryEvent.Choice opts ->
        let index, label =
          if interactive then
            printfn ""
            opts |> List.iteri (fun i (_, l) -> printfn "  [%d] %s" i l)
            printf "choose> "

            match System.Int32.TryParse(System.Console.ReadLine()) with
            | true, i when i >= 0 && i < List.length opts -> i, snd (List.item i opts)
            | _ -> 0, snd (List.head opts)
          else
            0, snd (List.head opts)

        printfn "\n  > %s\n" label
        runner.Choose index
      | StoryEvent.End ->
        printfn "\n--- the story ends ---"
        running <- false

let private renderGrid (g: Grid) =
  [ for y in 0 .. g.Height - 1 ->
      [ for x in 0 .. g.Width - 1 -> if Grid.isWall g { X = x; Y = y } then '#' else '.' ]
      |> System.String.Concat ]
  |> String.concat "\n"

let private maps () =
  Maps.rooms
  |> List.iter (fun (name, g) ->
    let entrance = Grid.tiles g |> Seq.filter (Grid.isFloor g) |> Seq.head
    printfn "%s (%dx%d) connected=%b" name g.Width g.Height (Dungeon.isConnected g entrance)
    printfn "%s\n" (renderGrid g))

let private wc () =
  let w = Content.wailingCaverns 1UL
  let party = w.Entities |> List.filter (fun e -> e.Faction = Party) |> List.length
  let hostiles = w.Entities |> List.filter (fun e -> e.Faction = Hostile) |> List.length
  printfn "stitched %dx%d  party=%d hostiles=%d" w.Grid.Width w.Grid.Height party hostiles
  printfn "%s" (renderGrid w.Grid)

[<EntryPoint>]
let main argv =
  match argv |> Array.toList with
  | [] | [ "demo" ] ->
    demo 1UL 2000
    0
  | "demo" :: rest ->
    demo (seedOf rest 1UL) (intOf rest 1 2000)
    0
  | [ "fingerprint" ] ->
    printf "%s" (Dump.fingerprint (Bench.runEncounter 1UL 2000))
    0
  | "fingerprint" :: rest ->
    printf "%s" (Dump.fingerprint (Bench.runEncounter (seedOf rest 1UL) 2000))
    0
  | [ "bench" ] ->
    bench ()
    0
  | "dungeon" :: rest ->
    let plan = Dungeon.layout (seedOf rest 1UL)
    printfn "%s" (Dungeon.describe plan)
    printfn "connected: %b" (Dungeon.isConnected plan.Grid plan.Entrance)
    printfn "%s" (Dungeon.render plan)
    0
  | [ "maps" ] ->
    maps ()
    0
  | [ "wc" ] ->
    wc ()
    0
  | "smrun" :: rest ->
    let wing = rest |> List.tryItem 0 |> Option.defaultValue "graveyard"

    let difficulty =
      match rest |> List.tryItem 1 with
      | Some "min" -> Content.Minimum
      | Some "max" -> Content.Maximum
      | _ -> Content.Median

    let limit = rest |> List.tryItem 2 |> Option.map int |> Option.defaultValue 12000
    let mutable w = Content.scarletMonasteryAt wing difficulty 1UL
    let hostiles = w.Entities |> List.filter (fun e -> e.Faction = Hostile) |> List.length
    let party = SimState.aliveParty w |> List.map (fun e -> e.Pos)
    let mutable tick = 0

    while tick < limit && not (SimState.aliveHostiles w |> List.isEmpty) do
      w <- SimTick.step (Bench.autoPilot w) w
      tick <- tick + 1



    let alive = SimState.aliveParty w |> List.map (fun e -> sprintf "%s %d/%d" e.Name e.Health e.MaxHealth)
    let now = SimState.aliveParty w |> List.map (fun e -> e.Pos)

    // How far the Tank ended from the nearest surviving mob: the difference
    // between a wipe and a Party that never found the fight.
    let nearest =
      match now, SimState.aliveHostiles w with
      | tank :: _, hostiles when not hostiles.IsEmpty ->
        hostiles |> List.map (fun e -> Pos.chebyshev e.Pos tank) |> List.min |> sprintf " nearest=%d"
      | _ -> ""

    printfn
      "%s %A level=%d ticks=%d/%d mobs=%d left=%d party=[%s]%s spawn=[%s] end=[%s]"
      wing
      difficulty
      (Content.levelOf difficulty (Content.wingOf wing))
      tick
      limit
      hostiles
      (SimState.aliveHostiles w |> List.length)
      (String.concat ", " alive)
      nearest
      (party |> List.map (fun p -> sprintf "%d,%d" p.X p.Y) |> String.concat " ")
      (now |> List.map (fun p -> sprintf "%d,%d" p.X p.Y) |> String.concat " ")

    0
  | "smtrace" :: rest ->
    // Where the ticks go: mob progress, party health and party resource every
    // `every` ticks. A wing that wipes and a wing that stalls look nothing alike
    // here, and the mob-health line says whether the Party's damage is landing.
    let wing = rest |> List.tryItem 0 |> Option.defaultValue "graveyard"

    let difficulty =
      match rest |> List.tryItem 1 with
      | Some "min" -> Content.Minimum
      | Some "max" -> Content.Maximum
      | _ -> Content.Median

    let limit = rest |> List.tryItem 2 |> Option.map int |> Option.defaultValue 12000
    let every = rest |> List.tryItem 3 |> Option.map int |> Option.defaultValue 1000
    let mutable w = Content.scarletMonasteryAt wing difficulty 1UL
    let hostiles = w.Entities |> List.filter (fun e -> e.Faction = Hostile) |> List.length
    let mobHealth = w.Entities |> List.filter (fun e -> e.Faction = Hostile) |> List.sumBy (fun e -> e.MaxHealth)
    let partyHealth = SimState.aliveParty w |> List.sumBy (fun e -> e.MaxHealth)
    let partyResource = SimState.aliveParty w |> List.sumBy (fun e -> e.MaxResource)
    let mutable tick = 0

    let detail = List.contains "v" rest

    let line () =
      let party = SimState.aliveParty w
      let mobs = SimState.aliveHostiles w

      printfn
        "%5d left=%2d mobhp=%3d%% partyhp=%3d%% res=%3d%% alive=%d"
        tick
        mobs.Length
        (if mobHealth = 0 then 0 else (mobs |> List.sumBy (fun e -> e.Health)) * 100 / mobHealth)
        (if partyHealth = 0 then 0 else (party |> List.sumBy (fun e -> e.Health)) * 100 / partyHealth)
        (if partyResource = 0 then 0 else (party |> List.sumBy (fun e -> e.Resource)) * 100 / partyResource)
        party.Length

      if detail then
        for e in party do
          printfn
            "        %-8s at %d,%d hp=%d/%d casts=%b stall=%d goal=%A path=%d"
            e.Name
            e.Pos.X
            e.Pos.Y
            e.Health
            e.MaxHealth
            e.Casting.IsSome
            (int e.StallTicks)
            e.Goal
            e.Path.Length

          match e.Goal with
          | Some g ->
            let free = Path.astar w.Grid (fun _ -> false) e.Pos g
            let taken = Path.astar w.Grid (SimState.claimedByOther w e.Id) e.Pos g

            printfn
              "        route %d,%d -> %d,%d  free=%A  blocked=%A"
              e.Pos.X
              e.Pos.Y
              g.X
              g.Y
              (free.Path.Length, free.Truncated)
              (taken.Path.Length, taken.Truncated)
          | None -> printfn "        route none"

        for h in mobs |> List.truncate 6 do
          printfn "        mob   %-22s at %d,%d hp=%d/%d target=%A" h.Name h.Pos.X h.Pos.Y h.Health h.MaxHealth h.Target

    printfn "%s %A level=%d mobs=%d" wing difficulty (Content.levelOf difficulty (Content.wingOf wing)) hostiles
    line ()

    while tick < limit && not (SimState.aliveHostiles w |> List.isEmpty) do
      w <- SimTick.step (Bench.autoPilot w) w
      tick <- tick + 1
      if tick % every = 0 then line ()

    line ()
    0
  | "sm" :: rest ->
    let wing = rest |> List.tryItem 0 |> Option.defaultValue "graveyard"

    let difficulty =
      match rest |> List.tryItem 1 with
      | Some "min" -> Content.Minimum
      | Some "max" -> Content.Maximum
      | _ -> Content.Median

    let w = Content.scarletMonasteryAt wing difficulty 1UL

    let party =
      w.Entities
      |> List.filter (fun e -> e.Faction = Party)
      |> List.map (fun e -> e.Name)

    printfn
      "%s %A  level=%d  %dx%d  party=%s"
      wing
      difficulty
      (Content.levelOf difficulty (Content.wingOf wing))
      w.Grid.Width
      w.Grid.Height
      (String.concat ", " party)

    printfn "%s" (renderGrid w.Grid)
    0
  | [ "diagnose" ] ->
    printfn "%s" (Bench.searchFailureCost 200)
    printfn ""

    for seed in [ 1UL; 2UL ] do
      printfn "=== seed %d ===" seed
      printfn "%s" (Bench.stepProfile seed 1200 100)

    0
  | [ "profile" ] ->
    let gully = (Content.gully 1UL).Grid
    let dense = Bench.randomMap 24 16 22 7000UL

    // Per-seed, because identical-length fights differ by 40x and the only thing
    // that varies between them is where Anacondra stands.
    for seed in [ 1UL; 2UL; 3UL; 4UL; 5UL ] do
      let boss =
        (Content.gully seed).Entities
        |> List.find (fun e -> e.Name = "Lady Anacondra")

      printfn
        "seed %d, anacondra at %2d,%-2d  %s"
        seed
        boss.Pos.X
        boss.Pos.Y
        (Bench.pressureProfile seed 4000)

    printfn ""
    printfn "%s" (Bench.searchCost "22x13 encounter gully" gully (Bench.samplePairs gully 200) 200)

    printfn
      "%s"
      (Bench.searchCost
        "24x16 random, 22%% walls"
        dense
        (Bench.samplePairs dense 200)
        200)

    0
  | "runs" :: rest ->
    runs (intOf rest 0 20) (intOf rest 1 2000)
    0
  | "snapshots" :: rest ->
    // The same lines the snapshot test compares, so a baseline can be produced
    // without running the test runner.
    [ 1UL .. uint64 (intOf rest 0 30) ]
    |> List.iter (fun s -> printfn "%d %s" s (Bench.summary (Bench.runEncounter s 3000)))
    0
  | "story" :: rest ->
    playStory (List.contains "-i" rest || List.contains "--interactive" rest)
    0
  | _ ->
    usage ()
    1
