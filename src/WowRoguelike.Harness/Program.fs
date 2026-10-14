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
  printfn "sim runs [n] [maxTicks]     run n seeds and summarise outcomes"
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
  printfn "%s" (Dump.log w)
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
  | "story" :: rest ->
    playStory (List.contains "-i" rest || List.contains "--interactive" rest)
    0
  | _ ->
    usage ()
    1
