module WowRoguelike.Harness.Program

open WowRoguelike.Core

let private usage () =
  printfn "sim demo [seed] [maxTicks]  play the encounter with the scripted party, show the log"
  printfn "sim fingerprint [seed]      canonical state text for golden-replay comparison"
  printfn "sim bench                   time the tick loop at several entity counts"
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

  printfn "seed=%d tick=%d outcome=%A" seed w.Tick (Sim.outcome w)
  printfn ""
  printfn "--- combat log (oldest first) ---"
  printfn "%s" (Dump.log w)
  printfn ""
  printfn "--- final state ---"
  printfn "%s" (Dump.world w)

let private runs (n: int) (maxTicks: int) =
  let results =
    [ 1UL .. uint64 n ] |> List.map (fun s -> Bench.runEncounter s maxTicks)

  let count o = results |> List.filter (fun w -> Sim.outcome w = o) |> List.length
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
  printfn "%s" (Bench.report "encounter, scripted party" 2000 (Bench.runEncounter 1UL 200))
  printfn "%s" (Bench.report "encounter, idle" 2000 (Content.gully 1UL))

  for n in [ 100; 250; 500 ] do
    printfn "%s" (Bench.report (sprintf "synthetic, %d hostiles" n) 2000 (Bench.synthetic n 1UL))

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
  | "runs" :: rest ->
    runs (intOf rest 0 20) (intOf rest 1 2000)
    0
  | _ ->
    usage ()
    1
