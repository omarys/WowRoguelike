module WowRoguelike.Harness.Program

open WowRoguelike.Core

let private usage () =
  printfn "sim demo [seed] [maxTicks]  play the encounter with the scripted party, show the log"
  printfn "sim fingerprint [seed]      canonical state text for golden-replay comparison"
  printfn "sim bench                   time the tick loop at several entity counts"
  printfn "sim profile                 split the tick cost into search frequency and search internals"
  printfn "sim diagnose                bucket SimTick.step cost across a fight and dump the worst tick"
  printfn "sim runs [n] [maxTicks]     run n seeds and summarise outcomes"

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
  | _ ->
    usage ()
    1
