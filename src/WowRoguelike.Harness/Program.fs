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
  printfn "sim attrib [wing] [min|median|max] [maxTicks]  direct damage and healing by ability and caster"
  printfn "sim runs [n] [maxTicks]     run n seeds and summarise outcomes"
  printfn "sim autopsy [n] [wing] [--class C]  per-seed death/positioning autopsy for the healer seat in one wing"
  printfn "sim comps [n] [--level L]  party compositions through four rounds of healer/DPS swaps"
  printfn "sim activity [n] [--level L]  how a run splits into rest against fight, and casters into cast/move/idle"
  printfn "sim sheet [--level L]      per-seat pool, Spirit, tick rate and ability costs"
  printfn "sim druidspend [n]         where the Restoration Druid's mana actually goes"
  printfn "sim healers [n] [--level L]  each healer class in the same seat: throughput, mana by purpose, end-of-fight pool, activity partition"
  printfn "sim ttk [n] [--level L]      the healer damage gate tick by tick: remaining hp, party dps, seconds to kill, reserve, budget, verdict"
  printfn "sim drmana [n] [--level L]  the Restoration Druid's mana ledger: what each spell cost and bought"

  printfn
    "sim shmana [n] [--level L] [--class C]  any healer's mana ledger, plus every direct heal booked against the hole it was ordered on"

  printfn
    "sim totems [n]            the run's totem ledger: drops, pulses, hp and mana delivered, members reached"

  printfn
    "sim seat [n] [wing] [--class C] [--melee]  a fifth-seat or melee-slot swap, over many seeds"
  printfn "sim droods [n]                a Druid-only party, one per shape, over the three wings"

  printfn
    "sim interrupts [n] [--class C]  who breaks hostile casts, with which spell, and the size of the casts they broke"
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

/// `--roster A,B,C,D,E` — the party to measure with. Roles are fixed per class and
/// only the Warrior is a Tank, so a party is only realistic with one sitting in it:
/// five casters have nobody holding the pack, and their healers burn their pools on
/// a fight no real group would take. Any measurement that skips this is measuring
/// the roster, not the class.
let private rosterOf (args: string list) =
  match List.tryFindIndex ((=) "--roster") args with
  | Some i ->
    match nth args (i + 1) with
    | Some s -> Some(s.Split(',') |> Array.toList |> List.map (fun x -> x.Trim()))
    | None -> None
  | None -> None

let private intOf (args: string list) (i: int) (fallback: int) =
  match nth args i with
  | Some s ->
    match System.Int32.TryParse s with
    | true, v -> v
    | _ -> fallback
  | None -> fallback

let private flagOf (args: string list) (name: string) =
  match List.tryFindIndex ((=) name) args with
  | Some i ->
    match nth args (i + 1) with
    | Some s ->
      match System.Int32.TryParse s with
      | true, v -> Some v
      | _ -> None
    | None -> None
  | None -> None

/// A flag whose value is a word rather than a number (`--class Shaman`). Kept
/// apart from `flagOf` so a mistyped number cannot silently become a class name.
let private textOf (args: string list) (name: string) =
  match List.tryFindIndex ((=) name) args with
  | Some i -> nth args (i + 1)
  | None -> None

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

/// `sim autopsy [n] [wing] [--class C]` — per-seed death and positioning telemetry
/// for the healer seat in one wing. `comps` says which arm wipes; this says why.
/// For every seed it reports the first death (who, at what tick, against how many
/// engaged mobs, and what the healer was doing at that moment), plus the counters
/// the wipe hypotheses are argued from: how many combat ticks the healer stood
/// inside cleave radius of an engaged mob, how many of its casts took pushback or
/// never resolved, and how much damage each seat actually took (parsed from the
/// combat log). Read-only on the engine: it drives the same `Bench.autoPilot`
/// loop `comps` drives and watches the world between ticks.
let private autopsy (seedCount: int) (wingName: string) (classOverride: string option) =
  let wing = Content.wingOf wingName
  let level = Content.levelOf Content.Median wing
  let healerClass = classOverride |> Option.defaultValue "Paladin"
  let roster = [ "Warrior"; healerClass; "Mage"; "Rogue"; "Warlock" ]
  let maxTicks = if wing.Name = "graveyard" then 24000 else 32000
  let partyNames = roster

  printfn
    "autopsy %s L%d healer=%s seeds=%d roster=%s"
    wing.Name
    level
    healerClass
    seedCount
    (roster |> String.concat "/")

  // Damage each party member took, from the log's damage lines
  // ("<A>'s <B> hits <T> for <N>" / "<A> swings at <T> for <N>").
  let damageTaken (log: string list) =
    (Map.empty, log)
    ||> List.fold (fun (m: Map<string, int>) line ->
      let parts = line.Split([| " for " |], System.StringSplitOptions.None)

      if parts.Length = 2 then
        let ok, n = System.Int32.TryParse parts.[1]

        if ok && n > 0 then
          let hitIdx = parts.[0].LastIndexOf " hits "
          let swingIdx = parts.[0].LastIndexOf " swings at "

          let t =
            if hitIdx >= 0 then parts.[0].Substring(hitIdx + 6).Trim()
            elif swingIdx >= 0 then parts.[0].Substring(swingIdx + 11).Trim()
            else ""

          if partyNames |> List.contains t then
            Map.add t ((Map.tryFind t m |> Option.defaultValue 0) + n) m
          else m
        else m
      else m)

  let mutable cleared, wiped = 0, 0
  // Cross-seed totals.
  let mutable tCleave, tKnock, tStarts, tEnds, tHeals, tHealSum = 0, 0, 0, 0, 0, 0
  let mutable tCombat, tAdjacent, tDeaths, tFirstTicks, tLost = 0, 0, 0, 0, 0
  let mutable tDmgTaken = Map.empty<string, int>
  let firstBy = System.Collections.Generic.Dictionary<string, int>()

  for s in [ 1UL .. uint64 seedCount ] do
    let mutable w =
      Content.withRosterAt level roster (Content.scarletMonasteryAt wing.Name Content.Median s)

    let mutable prevCasting: (string * int * int) option = None // ability, remaining, pushes
    let mutable castStarts, castEnds, pushEvents, castLost = 0, 0, 0, 0
    let mutable combatTicks, adjacentTicks = 0, 0
    let mutable deaths = 0
    let mutable first: (int * string * int * string) option = None

    let mutable guard = 0
    let mutable prevLogLen = w.Log.Length

    while guard < maxTicks && SimState.continues w do
      let aliveBefore =
        w.Entities
        |> List.filter (fun e -> e.Faction = Party && SimState.alive e)
        |> List.map (fun e -> e.Id)
        |> Set.ofList

      w <- SimTick.step (Bench.autoPilot w) w
      guard <- guard + 1

      // Newest first: the lines this tick added sit at the head of the log.
      let added =
        let n = w.Log.Length - prevLogLen

        prevLogLen <- w.Log.Length
        if n > 0 then w.Log |> List.truncate n else []

      let healer =
        w.Entities |> List.tryFind (fun e -> e.Faction = Party && e.Role = Some Healer)

      let mobs =
        w.Entities
        |> List.filter (fun e -> e.Faction = Hostile && SimState.alive e && e.Engaged)

      if not mobs.IsEmpty then
        combatTicks <- combatTicks + 1

      // The healer's cast pressure. `Remaining` not falling means a new cast
      // began the tick the old one resolved, so back-to-back casts still count;
      // an ended cast with no resolution line that tick was lost to movement,
      // a stun or death rather than completed.
      (match healer with
       | Some h when SimState.alive h ->
           match h.Casting with
           | Some c ->
               // A pushback raises Remaining, so "not falling" alone cannot mean a
               // new cast: a raised Pushes counter is the same cast being pushed.
               let isNew, pushDelta =
                 match prevCasting with
                 | Some(_, rem, p) when c.Pushes > p -> false, c.Pushes - p
                 | Some(_, rem, _) -> int c.Remaining >= rem, 0
                 | None -> true, 0

               if isNew then castStarts <- castStarts + 1

               pushEvents <- pushEvents + pushDelta
               prevCasting <- Some(c.Ability.Name, int c.Remaining, c.Pushes)
           | None ->
               (match prevCasting with
                | Some(name, _, _) ->
                    let resolved =
                      added
                      |> List.exists (fun l ->
                        l.StartsWith(sprintf "%s's " healerClass)
                        && l.Contains(sprintf "%s heals " name))

                    if not resolved then castLost <- castLost + 1
                | None -> ())

               if prevCasting.IsSome then castEnds <- castEnds + 1

               prevCasting <- None
       | _ -> prevCasting <- None)

      if not mobs.IsEmpty then
        match healer with
        | Some h when SimState.alive h ->
            if mobs |> List.exists (fun m -> Pos.chebyshev m.Pos h.Pos <= 1) then
              adjacentTicks <- adjacentTicks + 1
        | _ -> ()

      // A death is a live-to-dead transition, not an end-state read: the tree
      // can Rebirth, so what is counted is the moment someone fell.
      for e in w.Entities do
        if e.Faction = Party && aliveBefore.Contains e.Id && not (SimState.alive e) then
          deaths <- deaths + 1

          if first.IsNone then
            let hs =
              match healer with
              | Some h when SimState.alive h ->
                  let cast =
                    h.Casting
                    |> Option.map (fun c ->
                      sprintf "%s rem=%d pushes=%d" c.Ability.Name (int c.Remaining) c.Pushes)
                    |> Option.defaultValue "idle"

                  let minD =
                    mobs
                    |> List.map (fun m -> Pos.chebyshev m.Pos h.Pos)
                    |> List.fold min System.Int32.MaxValue

                  let inCleave =
                    mobs
                    |> List.filter (fun m -> Pos.chebyshev m.Pos h.Pos <= 1)
                    |> List.length

                  sprintf "hp=%d%% cast=%s minD=%d cleaveReach=%d" (100 * h.Health / max 1 h.MaxHealth) cast minD inCleave
              | Some _ -> sprintf "DEAD hp=0 cast=none"
              | None -> "absent"

            first <- Some(int w.Tick, e.Name, mobs.Length, hs)

    let o = SimState.outcome w

    (match o with
     | EncounterCleared -> cleared <- cleared + 1
     | PartyWiped -> wiped <- wiped + 1
     | _ -> ())

    let log = w.Log |> List.rev

    let healLines =
      log
      |> List.filter (fun l -> l.StartsWith(sprintf "%s's " healerClass) && l.Contains " heals ")

    let heals = healLines.Length

    let healSum =
      healLines
      |> List.sumBy (fun l ->
        match System.Int32.TryParse(l.Substring(l.LastIndexOf " for " + 5)) with
        | true, v -> v
        | _ -> 0)

    let knock =
      log
      |> List.filter (fun l -> l.StartsWith(sprintf "%s's cast is knocked back" healerClass))
      |> List.length

    let cleaveHits =
      log
      |> List.filter (fun l ->
        l.Contains "Cleave" && l.Contains(sprintf " hits %s for " healerClass))
      |> List.length

    let dmg = damageTaken log

    tDmgTaken <-
      (tDmgTaken, dmg)
      ||> Map.fold (fun m k v -> Map.add k ((Map.tryFind k m |> Option.defaultValue 0) + v) m)

    tCleave <- tCleave + cleaveHits
    tKnock <- tKnock + knock
    tStarts <- tStarts + castStarts
    tEnds <- tEnds + castEnds
    tHeals <- tHeals + heals
    tHealSum <- tHealSum + healSum
    tCombat <- tCombat + combatTicks
    tAdjacent <- tAdjacent + adjacentTicks
    tDeaths <- tDeaths + deaths
    tLost <- tLost + castLost

    (match first with
     | Some(tick, name, _, _) ->
         tFirstTicks <- tFirstTicks + tick
         firstBy[name] <- (match firstBy.TryGetValue name with true, v -> v | _ -> 0) + 1
     | None -> ())

    let healerEnd =
      w.Entities
      |> List.tryFind (fun e -> e.Faction = Party && e.Role = Some Healer)

    let healerMana =
      healerEnd
      |> Option.map (fun h -> 100 * h.Resource / max 1 h.MaxResource)
      |> Option.defaultValue -1

    printfn
      "seed=%d %-16s ticks=%-5d deaths=%d manaEnd=%d%% heals=%d(sum=%d) starts=%d ends=%d lost=%d pushes=%d knock=%d cleaveOnH=%d adj=%d/%d"
      s
      (string o)
      guard
      deaths
      healerMana
      heals
      healSum
      castStarts
      castEnds
      castLost
      pushEvents
      knock
      cleaveHits
      adjacentTicks
      combatTicks

    (match first with
     | Some(tick, name, mobsAlive, hs) -> printfn "     first-death: %s @t=%d mobsAlive=%d healer[%s]" name tick mobsAlive hs
     | None -> printfn "     first-death: none")

  let n = max 1 seedCount

  printfn ""

  printfn
    "%s total: cleared=%d wiped=%d unresolved=%d deaths=%d"
    healerClass
    cleared
    wiped
    (seedCount - cleared - wiped)
    tDeaths

  printfn
    "  per-run avg: heals=%.0f healSum=%.0f castStarts=%.0f castEnds=%.0f lost=%.0f pushbacks=%.0f knockbacks=%.0f cleaveHitsOnHealer=%.1f adjacent=%.0f%%"
    (float tHeals / float n)
    (float tHealSum / float n)
    (float tStarts / float n)
    (float tEnds / float n)
    (float tLost / float n)
    (float tKnock / float n)
    (float tKnock / float n)
    (float tCleave / float n)
    (100.0 * float tAdjacent / float (max 1 tCombat))

  printf "  damage taken per seat (avg per run): "

  for name in partyNames do
    printf "%s=%d " name ((Map.tryFind name tDmgTaken |> Option.defaultValue 0) / n)

  printfn ""
  printf "  first deaths by seat: "

  for kv in firstBy do
    printf "%s=%d " kv.Key kv.Value

  printfn ""

  printfn
    "  adjacent = combat ticks with an engaged mob within cleave radius (chebyshev 1) of the healer"

/// Composition is the one variable the player picks and never measures. Every
/// number in Bench and `sim runs` is the default Warrior/Priest/Rogue/Mage/Druid
/// party, so none of them says whether any other five are playable.
///
/// Four rounds, each five members with a Warrior in front:
///   1. one healer plus Mage/Rogue/Warlock, once per healer, to rank the healers;
///   2. the winning healer plus each other healer, to see if a healing pair beats it;
///   3. the winning healer with the Rogue swapped for a Hunter, whose long range
///      opens the pulls the Rogue has to walk into;
///   4. the winning healer with each three of the four damage dealers behind it.
///
/// Every roster below seats five different classes. A repeated class measures that
/// class against itself, which is no comparison at all — it just weights the party
/// toward one kit — so `report` refuses a roster with a duplicate in it.
/// `--level L` seats every member at level L instead of the level the map seated
/// them at, which is how the four healers are compared at level 60: at 60 the stats
/// are the researched Gear60 sheet and the kit is the top rank of every ladder. The
/// mobs stay where the map put them, so the ladder ranks healers against each other
/// on the same content rather than measuring level-60 balance.
let private comps (seedCount: int) (level: int option) =
  let seeds = [ 1UL .. uint64 seedCount ]

  // The three built wings at their median: the target level the balance question
  // is asked at, and the level the party is seated at unless --level overrides it.
  // Cathedral has no content yet, so it is not a balance target.
  let maps =
    Content.scarletWings
    |> List.filter (fun w -> w.Name <> "cathedral")
    |> List.map (fun w ->
      let budget = if w.Name = "graveyard" then 24000 else 32000
      w.Name, (fun (s: uint64) -> Content.scarletMonasteryAt w.Name Content.Median s), w.Median, budget)

  // The scripted party, not the player: these numbers are the floor a composition
  // sets under a player, not a verdict on what a player could do with it.
  let play (w: World) maxTicks =
    let mutable x = w

    for _ in 1 .. maxTicks do
      if SimState.continues x then
        x <- SimTick.step (Bench.autoPilot x) x

    x

  let seat (roster: string list) (at: int) (w: World) =
    Content.withRosterAt (level |> Option.defaultValue at) roster w

  let measure (roster: string list) (make: uint64 -> World) (at: int) maxTicks =
    let results =
      seeds |> List.map (fun s -> play (seat roster at (make s)) maxTicks)

    let count o =
      results |> List.filter (fun w -> SimState.outcome w = o) |> List.length

    let ticks = results |> List.map (fun w -> int w.Tick) |> List.sort

    let alive =
      results |> List.map (fun w -> float (SimState.aliveParty w |> List.length)) |> List.average

    // Per seat, averaged over the seeds: share of the resource pool left when the
    // run ended, and how often that seat finished dead. A composition that stalls
    // on empty pools is a sustain problem; one that wipes with pools to spare is a
    // throughput or targeting problem. Without this the two look identical.
    let per pick cls =
      results
      |> List.map (fun w -> w.Entities |> List.filter (fun e -> e.Name = cls) |> List.sumBy pick)
      |> List.average

    let seats =
      roster
      |> List.distinct
      |> List.map (fun cls ->
        cls,
        per (fun e -> float e.Resource / float (max 1 e.MaxResource)) cls,
        per (fun e -> if SimState.alive e then 0.0 else 1.0) cls)

    count EncounterCleared, count PartyWiped, List.item (List.length seeds / 2) ticks, alive, seats

  let report (label: string) (roster: string list) =
    // A roster is a comparison, and a class cannot be compared with itself: two
    // copies of one kit just weight the party toward it and read as a finding
    // about the class. Fail here rather than publish that row.
    if List.distinct roster <> roster then
      failwithf "%s seats the same class twice: %A" label roster

    let totals =
      maps
      |> List.map (fun (mapName, make, at, maxTicks) ->
        let cleared, wiped, median, alive, seats = measure roster make at maxTicks

        printfn
          "  %-34s %-10s L%-2d cleared=%-3d wiped=%-3d unresolved=%-3d median-ticks=%-6d alive=%.1f"
          label
          mapName
          at
          cleared
          wiped
          (seedCount - cleared - wiped)
          median
          alive

        for cls, mana, dead in seats do
          printfn "       %-9s mana=%3.0f%% dead=%3.0f%%" cls (mana * 100.0) (dead * 100.0)

        cleared, median)

    List.sumBy fst totals, (totals |> List.map snd |> List.min)

  let healers = [ "Priest"; "Druid"; "Paladin"; "Shaman" ]
  let dps = [ "Mage"; "Rogue"; "Warlock" ]

  printfn
    "seeds=%d level=%s per map, maps: %s"
    seedCount
    (level |> Option.map string |> Option.defaultValue "content")
    (maps |> List.map (fun (m, _, _, _) -> m) |> String.concat ", ")

  printfn "\nround 1 - one healer, three DPS"

  let ranked =
    healers
    |> List.map (fun healer -> healer, report (sprintf "Warrior/%s/%s" healer (dps |> String.concat "/")) ("Warrior" :: healer :: dps))

  let best, _ =
    ranked
    |> List.sortByDescending (fun (_, (cleared, minMedian)) -> cleared, -minMedian)
    |> List.head

  printfn "\nround 2 - %s plus each other healer, two DPS (Mage/Warlock)" best

  for other in healers |> List.filter (fun h -> h <> best) do
    report (sprintf "Warrior/%s/%s/Mage/Warlock" best other) ([ "Warrior"; best; other ] @ [ "Mage"; "Warlock" ])
    |> ignore

  printfn "\nround 3 - Rogue swapped for a Hunter beside %s" best

  report (sprintf "Warrior/%s/Mage/Warlock/Hunter" best) ([ "Warrior"; best ] @ [ "Mage"; "Warlock"; "Hunter" ])
  |> ignore

  printfn "\nround 4 - the damage dealers, beside %s" best

  // Round 1-3 vary the healers and make one DPS swap. This varies the damage
  // dealers with the healer fixed, which is the only way to tell a weak DPS class
  // from a party that is simply short a role.
  for dpsSet in
    [ [ "Mage"; "Rogue"; "Warlock" ]
      [ "Mage"; "Hunter"; "Warlock" ]
      [ "Mage"; "Rogue"; "Hunter" ]
      [ "Hunter"; "Rogue"; "Warlock" ] ] do
    report (sprintf "Warrior/%s/%s" best (String.concat "/" dpsSet)) ([ "Warrior"; best ] @ dpsSet)
    |> ignore

  printfn "\nbest single healer: %s" best

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
      | StoryEvent.Encounter id ->
        // The story does not just mention a boss, it fights it: the id maps to a
        // real `Content` encounter, run deterministically on a fixed seed. A wipe
        // ends the story (resurrection is a known non-feature), and an id with no
        // modelled fight is said so rather than silently skipped.
        match Content.bossEncounter id 1UL with
        | Some w0 ->
          let w = Bench.runWorld w0 6000

          printfn "\n  >>> ENCOUNTER: %s <<<" id
          printfn "  %s" (Bench.summary w)

          if SimState.outcome w <> EncounterCleared then
            printfn "\n--- the party falls, and the story ends ---"
            running <- false
        | None -> printfn "\n  >>> ENCOUNTER: %s (no fight modelled) <<<\n" id
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

/// How a party spends its ticks. Two questions the balance numbers cannot answer on
/// their own: how much of a run is *waiting*, and inside a pull how much of a caster's
/// time goes on casting rather than standing still. A healer that finishes on a full
/// pool either regenerated faster than it spent or never had anything to spend it on.
///
/// States are exclusive and read *before* each step:
///   rest — out of combat, so the party is between pulls or walking to the next one
///   cast — `Casting` held, i.e. mid-cast
///   move — in combat with a remaining step or somewhere to be
///   idle — in combat, not casting, not moving. The interesting number.
///
/// Idle splits by pool: standing still because nothing needs doing is a different
/// finding from standing still because the next cast is unaffordable, and only the
/// second is a mana-economy problem. "dry" is idle below a quarter pool.
let private activity (seedCount: int) (level: int option) (rosterOverride: string list option) =
  let seeds = [ 1UL .. uint64 seedCount ]

  // Two rosters, because a party is five seats and the caster list is six classes.
  // Plain "Druid" is `TreeDruid` — the Restoration seat — while the spec names are
  // the shapes that are not healing, so only the bare name counts. Hunter is
  // deliberately absent: it auto-attacks between shots and weaves Arcane/Aimed/
  // Multishot through that, so time off the cast bar is its design, not a finding.
  let rosters =
    match rosterOverride with
    | Some r -> [ r ]
    | None ->
      [ [ "Warrior"; "Priest"; "Mage"; "Warlock"; "Shaman" ]
        [ "Paladin"; "Priest"; "Mage"; "Warlock"; "Druid" ] ]

  let isCaster (e: Entity) =
    match e.Name with
    | "Mage" | "Priest" | "Warlock" | "Shaman" | "Paladin" | "Druid" -> true
    | _ -> false

  let maps =
    Content.scarletWings
    |> List.filter (fun w -> w.Name <> "cathedral")
    |> List.map (fun w ->
      w.Name, (fun (s: uint64) -> Content.scarletMonasteryAt w.Name Content.Median s), w.Median)

  // class -> [| cast; move; idle; idle-dry; pool-sum |]
  let counts = System.Collections.Generic.Dictionary<string, int array>()

  let slot (cls: string) =
    match counts.TryGetValue cls with
    | true, a -> a
    | _ ->
      let fresh = [| 0; 0; 0; 0; 0 |]
      counts.[cls] <- fresh
      fresh

  let mutable total = 0
  let mutable resting = 0
  let mutable fighting = 0
  let mutable pulls = 0
  let mutable wasFighting = false

  let play (roster: string list) (make: uint64 -> World) (at: int) (seed: uint64) =
    let mutable w = Content.withRosterAt (level |> Option.defaultValue at) roster (make seed)
    let mutable guard = 0

    while guard < 8000 && SimState.continues w do
      let combat = SimState.inCombat w
      total <- total + 1

      if combat then
        fighting <- fighting + 1
        if not wasFighting then pulls <- pulls + 1
      else
        resting <- resting + 1

      wasFighting <- combat

      if combat then
        for e in w.Entities do
          // By class rather than by `ResourceKind`, which a Hunter's pet also
          // carries: the pet is not a caster and its idle is not a finding.
          if e.Faction = Party && SimState.alive e && isCaster e then
            let a = slot e.Name
            a.[4] <- a.[4] + e.Resource * 100 / max 1 e.MaxResource

            match e.Casting with
            | Some _ -> a.[0] <- a.[0] + 1
            | None ->
              let moving =
                int e.MoveTicksLeft > 0 || e.Destination.IsSome || not e.Path.IsEmpty

              if moving then
                a.[1] <- a.[1] + 1
              else
                a.[2] <- a.[2] + 1
                if e.Resource * 4 < e.MaxResource then a.[3] <- a.[3] + 1

      w <- SimTick.step (Bench.autoPilot w) w
      guard <- guard + 1

  for roster in rosters do
    for (_, make, at) in maps do
      for s in seeds do
        play roster make at s

  let pct (n: int) (of': int) = if of' = 0 then 0.0 else 100.0 * float n / float of'
  let secs t = float t / 20.0

  printfn "rosters %s" (rosters |> List.map (String.concat "/") |> String.concat "  |  ")
  printfn "%d seeds x %d wings each, at each wing's median level" seedCount maps.Length
  printfn ""
  printfn "run      %6d ticks (%.0fs)" total (secs total)
  printfn "  fighting %6d  %5.1f%%" fighting (pct fighting total)
  printfn "  resting  %6d  %5.1f%%" resting (pct resting total)

  let avgFight = if pulls = 0 then 0.0 else float fighting / float pulls
  let avgRest = if pulls = 0 then 0.0 else float resting / float pulls
  printfn ""
  printfn "pulls    %6d   mean pull %.0ft (%.1fs)   mean rest %.0ft (%.1fs)" pulls avgFight (secs (int avgFight)) avgRest (secs (int avgRest))
  printfn ""
  printfn "caster idle inside a pull (share of that class's living in-combat ticks)"
  printfn "%-8s %7s %7s %7s %7s   %s" "class" "cast" "move" "idle" "mean pool" "idle on a dry pool"

  for KeyValue (cls, a) in counts |> Seq.sortBy (fun kv -> kv.Key) do
    let inCombat = a.[0] + a.[1] + a.[2]
    let pool = if inCombat = 0 then 0.0 else float a.[4] / float inCombat

    printfn
      "%-8s %6.1f%% %6.1f%% %6.1f%% %6.1f%%   %5.1f%% of its idle"
      cls
      (pct a.[0] inCombat)
      (pct a.[1] inCombat)
      (pct a.[2] inCombat)
      pool
      (pct a.[3] a.[2])

/// The numbers behind an economy reading: pool, Spirit, and the per-tick rate each
/// seat actually carries, with what its abilities cost. A class that runs dry is
/// either under-funded (small pool, small rate) or over-spending (expensive kit),
/// and those two need opposite fixes — so the diagnosis needs both sides.
let private sheet (level: int option) =
  let maps =
    Content.scarletWings
    |> List.filter (fun w -> w.Name <> "cathedral")
    |> List.map (fun w ->
      w.Name, (fun (s: uint64) -> Content.scarletMonasteryAt w.Name Content.Median s), w.Median)

  let rosters =
    [ [ "Warrior"; "Priest"; "Mage"; "Warlock"; "Shaman" ]
      [ "Paladin"; "Priest"; "Mage"; "Warlock"; "Druid" ] ]

  for roster in rosters do
    for (wing, make, at) in maps do
      let w = Content.withRosterAt (level |> Option.defaultValue at) roster (make 1UL)

      printfn "%s  %s" wing (String.concat "/" roster)

      for e in w.Entities |> List.filter (fun e -> e.Faction = Party) do
        printfn
          "  %-8s L%-2d %-6A pool=%-5d spirit=%-4d int=%-4d rate/2s=%-3d fsr-share=%d%%"
          e.Name
          e.Level
          e.Role
          e.MaxResource
          e.Stats.Spirit
          e.Stats.Intellect
          e.ResourceRegenPerTick
          (e.CombatRegenBp / 100)

        if e.ResourceKind = Mana then
          for a in e.Abilities |> List.filter (fun a -> a.ResourceCost > 0) do
            printfn "      %-22s cost=%-4d cast=%.1fs range=%d" a.Name a.ResourceCost (float a.CastTicks / 20.0) a.Range

      printfn ""

/// Where the Restoration Druid's mana actually goes. The idle reading says it is
/// dry; this says which buttons emptied it. Two numbers matter and they are
/// different: what the Druid *ordered* (with its list price) and what actually
/// left the pool. A gap between them is spend that never became a cast — and a
/// class that runs dry while barely casting can only be paying in instants.
let private druidSpend (seedCount: int) (rosterOverride: string list option) =
  // A realistic party: the Warrior holds the pack, Rogue and Hunter are the melee
  // and ranged DPS, and the Druid is the only healer. Measuring the Druid beside a
  // second and third healer in a tankless party measures the roster instead.
  let roster =
    rosterOverride
    |> Option.defaultValue [ "Warrior"; "Druid"; "Rogue"; "Mage"; "Hunter" ]
  let ordered = System.Collections.Generic.Dictionary<string, int>()
  let listed = System.Collections.Generic.Dictionary<string, int>()
  let mutable drained = 0
  let mutable gained = 0
  let mutable casts = 0
  let mutable lowPct = 0
  let mutable seen = 0

  let maps =
    Content.scarletWings
    |> List.filter (fun w -> w.Name <> "cathedral")
    |> List.map (fun w -> (fun (s: uint64) -> Content.scarletMonasteryAt w.Name Content.Median s), w.Median)

  let bump (d: System.Collections.Generic.Dictionary<string, int>) k n =
    match d.TryGetValue k with
    | true, v -> d[k] <- v + n
    | _ -> d[k] <- n

  for (make, at) in maps do
    for s in [ 1UL .. uint64 seedCount ] do
      let mutable w = Content.withRosterAt at roster (make s)
      let mutable tick = 0

      while tick < 12000 && SimState.continues w do
        let cmds = Bench.autoPilot w
        let before = SimState.aliveParty w |> List.tryFind (fun e -> e.Name = "Druid")

        for c in cmds do
          match c.Kind, before with
          | UseAbility(aid, name, _), Some d when d.Id = aid ->
            bump ordered name 1

            match d.Abilities |> List.tryFind (fun a -> a.Name = name) with
            | Some a -> bump listed name a.ResourceCost
            | None -> ()
          | _ -> ()

        w <- SimTick.step cmds w

        match before, SimState.aliveParty w |> List.tryFind (fun e -> e.Name = "Druid") with
        | Some b, Some a ->
          if a.Casting.IsSome && b.Casting.IsNone then casts <- casts + 1
          if a.Resource < b.Resource then drained <- drained + (b.Resource - a.Resource)
          if a.Resource > b.Resource then gained <- gained + (a.Resource - b.Resource)
          // How often the pool is under a quarter, which is where a healer starts
          // triaging rather than healing: the difference between a class that is
          // merely short and one that is rationing.
          seen <- seen + 1
          if SimState.resourcePct a < 25 then lowPct <- lowPct + 1
        | _ -> ()

        tick <- tick + 1

  printfn "Restoration Druid, %d seeds x %d wings" seedCount maps.Length
  printfn ""
  printfn "pool drained outright   %6d mana" drained
  printfn "pool refilled by regen  %6d mana" gained
  printfn "net                     %6d mana" (gained - drained)
  printfn "ticks under 25%% pool    %6d of %d (%.1f%%)" lowPct seen (if seen = 0 then 0.0 else float lowPct * 100.0 / float seen)
  printfn "cast bars started       %6d" casts
  printfn ""
  printfn "%-24s %6s %8s" "ordered" "times" "listed spend"

  ordered
  |> Seq.sortByDescending (fun kv -> match listed.TryGetValue kv.Key with | true, v -> v | _ -> 0)
  |> Seq.iter (fun kv ->
    let spend = match listed.TryGetValue kv.Key with | true, v -> v | _ -> 0
    printfn "%-24s %6d %8d" kv.Key kv.Value spend)

/// Whether healers finish a fight with their pool intact is the question the healing
/// ladder never answers: every rung of that ladder rewards throughput, and throughput
/// is bought by spending. A class can lead it and still be the one that cannot take
/// the next pull, so this seats each healer in the *same* four-seat frame, runs the
/// same seeds, and prints throughput beside the pool it ended on.
///
/// Two readings here are derived rather than recorded, and both are stated because
/// the numbers would be misread otherwise:
///
///  * mana spent on healing against damage is derived from the pool, not from the
///    order list: each tick the healer's one `UseAbility` order is classified by
///    `Ability.group`, and whatever the pool actually fell by that tick is charged to
///    that class — `Heal`/`HealOverTime` are healing, `Damage`/`DamageOverTime` are
///    damage, `Defence`/`Utility` (totems, dispels, buffs) are a third column. Reading
///    the order list instead would double-count: a heal is charged at cast start, but
///    an order the sim rejects (a Tree of Life druid ordering Moonfire) costs nothing
///    yet is re-issued every tick, which inflates the list-price sum by two orders of
///    magnitude. The cost is that a spend smaller than that tick's regeneration shows
///    as zero rather than as a small number.
///  * healing and damage done come from `Attribution`, which reads the combat log —
///    so damage is direct hits only: a damage-over-time tick writes no line. Healing
///    includes the heal-over-time ticks, which do write a heal line apiece
///    (`SimTick.bookHotTick`), so a Druid that heals by Rejuvenation reads truthfully
///    here. Swings are booked as `Auto Attack` and included.
///
/// Averages are the mean over seeds, not the median: "does the pool survive" is a
/// question about the typical fight, and a mean over nine fights answers it without
/// hiding a single dry pull behind a rank order.
///
/// The activity columns are a *partition*, not three rates: every living in-combat
/// Tick the healer had lands in exactly one of `active`/`covered`/`unmet`/`blocked`/
/// `recov`, and the five sum to 100%. It replaces the single `idle%` this command
/// used to print, which was wrong twice over. It read `Entity.Casting`, and an
/// instant cast never sets that field — `SimCombat.startOrResolve` calls
/// `resolveAbility` outright when `CastTicks <= 0` — so Rejuvenation, every shock and
/// every cat row read as standing still; and it lumped "the party is whole" together
/// with "the party is not whole and nothing happened", which are opposite findings.
/// There is no global cooldown in this sim (nothing writes one: `Cooldowns` is keyed
/// per ability name and `startOrResolve` only ever adds the ability's own
/// `CooldownTicks`), so "spent a GCD" contributes nothing here and `active` means an
/// order was issued or a cast bar was held.
let private healers (seedCount: int) (levelOverride: int option) =
  // The four classes that fill the Healer seat, per `Content.heroOf`. Everything
  // else in the roster is held fixed, and the DPS three are the ones `comps`
  // already compares healers behind — one healer plus Mage/Rogue/Warlock.
  let classes = [ "Priest"; "Shaman"; "Paladin"; "Druid" ]

  // Level 36 is the library's median. The level is the variable that moves both
  // pool and heal size, so it is pinned rather than left to the wing's own seating.
  let wing = Content.wingOf "library"
  let level = levelOverride |> Option.defaultValue (Content.levelOf Content.Median wing)
  let seeds = [ 1UL .. uint64 seedCount ]

  printfn "Scarlet Monastery %s at level %d (library median), %d seeds" wing.Name level seedCount
  printfn "fixed seats: Warrior + Mage + Rogue + Warlock; the healer seat is the variable"
  printfn "healing includes heal-over-time ticks; damage is direct hits only; means over seeds"

  printfn ""

  printfn
    "%-8s %8s %6s %7s %6s %10s %10s %10s %7s %6s %7s %7s %7s %7s %7s %4s %4s"
    "class"
    "heal/sc"
    "hps"
    "dmg/sc"
    "dps"
    "manaHeal"
    "manaDmg"
    "manaOther"
    "endPool"
    ">=90%"
    "active"
    "covered"
    "unmet"
    "blocked"
    "recov"
    "dry"
    "low"

  for cls in classes do
    let roster = [ "Warrior"; cls; "Mage"; "Rogue"; "Warlock" ]

    let mutable healTotal = 0
    let mutable dmgTotal = 0
    let mutable manaHeal = 0
    let mutable manaDmg = 0
    let mutable manaOther = 0
    let mutable endPctSum = 0
    let mutable over90 = 0
    let mutable activeTicks = 0
    let mutable coveredTicks = 0
    let mutable unmetTicks = 0
    let mutable blockedTicks = 0
    let mutable recoveringTicks = 0
    /// In-combat Ticks the healer was *alive* for — the denominator of the
    /// partition, and the number the five buckets are asserted against below.
    let mutable liveTicks = 0
    let mutable fightTicks = 0
    /// `blocked` does not say which brake bit; the three causes need opposite fixes,
    /// so they are counted separately and reported under the table.
    let mutable blockedMana = 0
    let mutable blockedCooldown = 0
    let mutable blockedForm = 0
    let mutable dryRuns = 0
    let mutable lowRuns = 0

    for s in seeds do
      let mutable w =
        Content.withRosterAt level roster (Content.scarletMonasteryAt wing.Name Content.Median s)

      let mutable guard = 0
      let mutable minPct = 100
      let mutable everDry = false

      // The seat, not the name: one member of this party holds `Role = Healer`.
      let seat (world: World) =
        world.Entities |> List.tryFind (fun e -> e.Faction = Party && e.Role = Some Healer)

      while guard < 8000 && SimState.continues w do
        let here = seat w
        let cmds = Bench.autoPilot w

        // The one ability the healer asked for this tick, and the pool as it stood
        // before the tick. A healer takes the heal branch or the ranged branch, never
        // both, so there is at most one such order.
        let hereId = here |> Option.map (fun h -> h.Id)
        let before = match here with Some h -> h.Resource | None -> 0

        let purpose =
          match here with
          | Some h ->
            // A cast-time spell is charged when it lands, and the tick it lands on
            // carries no order, so the in-flight cast is the first source of truth
            // and the order covers instants. Either way exactly one tick sees the
            // drain, so nothing is counted twice.
            match h.Casting with
            | Some c -> Some(Ability.group c.Ability)
            | None ->
              cmds
              |> List.tryPick (fun c ->
                match c.Kind with
                | UseAbility(aid, name, _) when aid = h.Id ->
                  h.Abilities |> List.tryFind (fun a -> a.Name = name) |> Option.map Ability.group
                | _ -> None)
          | None -> None

        match here with
        | Some h ->
          // The partition, read on the state the healer decided in: `cmds` is the order
          // list `PartyAi.decide` was about to issue, and `healerTick` consults it
          // first, so a Tick spent on an order — instant or not — is `active` and can
          // never also be counted in one of the four standby buckets.
          if SimState.inCombat w then
            fightTicks <- fightTicks + 1

            if SimState.alive h then
              liveTicks <- liveTicks + 1

              match PartyAi.healerTick w h cmds with
              | PartyAi.Active -> activeTicks <- activeTicks + 1
              | PartyAi.Covered -> coveredTicks <- coveredTicks + 1
              | PartyAi.Recovering -> recoveringTicks <- recoveringTicks + 1
              | PartyAi.Unmet -> unmetTicks <- unmetTicks + 1
              | PartyAi.Blocked cause ->
                blockedTicks <- blockedTicks + 1

                match cause with
                | PartyAi.ShortOfMana -> blockedMana <- blockedMana + 1
                | PartyAi.OnCooldown -> blockedCooldown <- blockedCooldown + 1
                | PartyAi.WrongForm -> blockedForm <- blockedForm + 1
        | None -> ()

        w <- SimTick.step cmds w

        // Charge the tick's actual pool movement to the purpose of what was ordered.
        // A rejected order moves nothing, so it costs nothing here.
        match hereId, purpose with
        | Some hid, Some g ->
          match w.Entities |> List.tryFind (fun e -> e.Id = hid) with
          | Some h ->
            let drain = before - h.Resource

            if drain > 0 then
              match g with
              | AbilityGroup.Heal
              | AbilityGroup.HealOverTime -> manaHeal <- manaHeal + drain
              | AbilityGroup.Damage
              | AbilityGroup.DamageOverTime -> manaDmg <- manaDmg + drain
              | _ -> manaOther <- manaOther + drain
          | None -> ()
        | _ -> ()

        // Water marks are read after the step, so the pool is what the tick left.
        match seat w with
        | Some h ->
          let p = SimState.resourcePct h
          if p < minPct then minPct <- p
          if h.Resource <= 0 then everDry <- true
        | None -> ()

        guard <- guard + 1

      // The healer's own name is what `Attribution` keys the log lines on.
      match seat w with
      | Some h ->
        match Attribution.byCaster w |> Map.tryFind h.Name with
        | Some t ->
          healTotal <- healTotal + t.Healing
          dmgTotal <- dmgTotal + t.Damage
        | None -> ()

        let endPct = SimState.resourcePct h
        endPctSum <- endPctSum + endPct
        if endPct >= 90 then over90 <- over90 + 1
      | None -> ()

      if everDry then dryRuns <- dryRuns + 1
      if minPct < 25 then lowRuns <- lowRuns + 1

    let perSeed (n: int) = float n / float seedCount
    let share (n: int) (of': int) = if of' = 0 then 0.0 else 100.0 * float n / float of'
    let secs = float fightTicks / 20.0
    let rate (n: int) = if secs = 0.0 then 0.0 else float n / secs

    // The partition's whole point is that it has no hole in it: a Tick the classifier
    // forgot would silently resize every other bucket, which is exactly how the old
    // `idle%` stayed wrong for so long. Asserted on counts, not on shares, so rounding
    // cannot mask it.
    let partition = activeTicks + coveredTicks + unmetTicks + blockedTicks + recoveringTicks

    if partition <> liveTicks then
      failwithf
        "healer activity partition does not close for %s: %d classified of %d live in-combat ticks"
        cls
        partition
        liveTicks

    printfn
      "%-8s %8.0f %6.1f %7.0f %6.1f %10.0f %10.0f %10.0f %6.1f%% %5.0f%% %5.1f%% %5.1f%% %5.1f%% %5.1f%% %5.1f%% %4d %4d"
      cls
      (perSeed healTotal)
      (rate healTotal)
      (perSeed dmgTotal)
      (rate dmgTotal)
      (perSeed manaHeal)
      (perSeed manaDmg)
      (perSeed manaOther)
      (float endPctSum / float seedCount)
      (share over90 seedCount)
      (share activeTicks liveTicks)
      (share coveredTicks liveTicks)
      (share unmetTicks liveTicks)
      (share blockedTicks liveTicks)
      (share recoveringTicks liveTicks)
      dryRuns
      lowRuns

    // `blocked` alone does not say which brake bit, and the three causes want opposite
    // fixes — a pool problem is a budget problem, a form problem is a rotation problem.
    printfn
      "         of which blocked: mana %5.1f%%  cooldown %5.1f%%  form %5.1f%%"
      (share blockedMana liveTicks)
      (share blockedCooldown liveTicks)
      (share blockedForm liveTicks)

  printfn ""
  printfn "endPool: mean mana at end of encounter, %% of max.  >=90%%: encounters finishing at or above 90%%."
  printfn "dry: encounters the healer reached 0 mana.  low: encounters it fell under 25%% at any tick."
  printfn ""
  printfn "activity: every living in-combat Tick, in exactly one bucket, summing to 100%%."
  printfn "  active   a cast bar, an instant cast, or any order — including a walk into range."
  printfn "  covered  nothing to do: the party's projected health (now + HoTs owed) is fine."
  printfn "  unmet    a heal was needed and payable and nothing was ordered — the bad idle."
  printfn "  blocked  a heal was needed and the engine refused it (pool, cooldown, form)."
  printfn "  recov    a hole the Spirit rate closes cheaper than a cast, on a clear window."

/// One row of `drmana`'s ledger: what one ability cost the Restoration Druid, and
/// what the healing column got for it. Every field is a sum over seeds.
type private ManaRow() =
  /// Times the ability resolved, whether or not the engine charged for it.
  member val Casts = 0 with get, set
  /// Mana that left the pool on the Tick the ability resolved
  /// (`SimCombat.fs:716-720`). A spend smaller than that Tick's regeneration reads
  /// as zero here, so this is a floor on the true price; `Expected` is the formula.
  member val Paid = 0 with get, set
  /// `Ability.ResourceCost` as the kit carries it — already net of the tree's cost
  /// talents, which fold themselves in once at construction (`Content.fs:4334`).
  member val Listed = 0 with get, set
  /// The price the engine's own arithmetic predicts (`SimCombat.fs:696-714`):
  /// `ResourceCost`, less a fifth for a heal-over-time cast in Tree of Life.
  member val Expected = 0 with get, set
  /// Healing the combat log credits to this row, direct halves and ticks alike.
  member val Healed = 0 with get, set
  /// Healing the row's heals-over-time attempted, from the aura's own per-tick
  /// number: the denominator of the overheal column.
  member val HotAttempt = 0 with get, set
  member val HotLanded = 0 with get, set
  /// Ticks the spell's aura fired.
  member val HotTicks = 0 with get, set
  /// Ticks whose attempt was clipped away at the health cap.
  member val HotWaste = 0 with get, set
  member val DirectLanded = 0 with get, set
  /// Direct casts seen: the denominator of the clip count.
  member val DirectCasts = 0 with get, set
  /// Direct casts that landed whole: a cast that leaves its target below the cap
  /// was not clipped, so its `for N` *is* that cast's attempt, exactly.
  member val WholeSum = 0 with get, set
  member val WholeN = 0 with get, set
  /// Presses that landed on an aura of the same spell still ticking.
  member val Refreshes = 0 with get, set
  /// Healing those auras still had to give when they were replaced.
  member val RefreshPending = 0 with get, set
  /// The share of the re-press whose price bought time already paid for.
  member val RefreshMana = 0 with get, set
  /// Healing thrown away by a Swiftmend that ate the aura early.
  member val EatPending = 0 with get, set
  member val EatMana = 0 with get, set

/// Split a `<caster>'s <spell> heals <target> for N` line (`SimCombat.fs:1007`,
/// `SimTick.fs:138`).
let private healLine (line: string) =
  match line.IndexOf "'s " with
  | -1 -> None
  | at ->
    match line.IndexOf " heals " with
    | -1 -> None
    | mid ->
      let rest = line.Substring(mid + 7)

      match rest.LastIndexOf " for " with
      | -1 -> None
      | f ->
        match System.Int32.TryParse(rest.Substring(f + 5)) with
        | true, n ->
          Some(line.Substring(0, at), line.Substring(at + 3, mid - at - 3), rest.Substring(0, f), n)
        | _ -> None

/// Split a `<caster> consumes the <spell> on <target> for N` line
/// (`SimCombat.fs:1036`). `Attribution` does not read this shape, which is why the
/// healing column `sim healers` reports cannot see what a Swiftmend bought.
let private consumeLine (line: string) =
  match line.IndexOf " consumes the " with
  | -1 -> None
  | at ->
    let rest = line.Substring(at + 13)

    match rest.IndexOf " on " with
    | -1 -> None
    | o ->
      let tail = rest.Substring(o + 4)

      match tail.LastIndexOf " for " with
      | -1 -> None
      | f ->
        match System.Int32.TryParse(tail.Substring(f + 5)) with
        | true, n -> Some(line.Substring(0, at), rest.Substring(0, o), n)
        | _ -> None

/// `sim drmana [n] [--level L]` — the Restoration Druid's mana, booked against the
/// ability that spent it. Diagnostic only: no rule is added and no number changes.
/// The command watches the healer seat through `SimTick.step` and counts, so every
/// figure below is measured rather than recomputed.
let private drmana (seedCount: int) (levelOverride: int option) =
  // The frame `healers 9` measures the Druid in: one healer, three DPS, one tank.
  let roster = [ "Warrior"; "Druid"; "Mage"; "Rogue"; "Warlock" ]
  let wing = Content.wingOf "library"
  let level = levelOverride |> Option.defaultValue (Content.levelOf Content.Median wing)
  let seeds = [ 1UL .. uint64 seedCount ]

  // The seat is the Druid by construction, so one probe world answers the static
  // questions: who the caster is, and which of its spells are heals over time.
  let probe =
    Content.withRosterAt level roster (Content.scarletMonasteryAt wing.Name Content.Median 1UL)

  let thisDruid =
    probe.Entities |> List.find (fun e -> e.Faction = Party && e.Role = Some Healer)

  let druidName = thisDruid.Name

  let hotRefresh (a: Ability) =
    a.Effects |> List.tryPick (function HealOverTime(d, _) -> Some d | _ -> None)

  let hasDirect (a: Ability) = a.Effects |> List.exists (function Heal _ -> true | _ -> false)

  let isConsume (a: Ability) =
    a.Effects |> List.exists (function ConsumeHealOverTime -> true | _ -> false)

  let isShift (a: Ability) =
    a.Effects |> List.exists (function TakeStance _ -> true | _ -> false)

  let hotNames =
    thisDruid.Abilities
    |> List.filter (fun a -> (hotRefresh a).IsSome)
    |> List.map (fun a -> a.Name)

  let isHot (name: string) = hotNames |> List.contains name

  let rows = System.Collections.Generic.Dictionary<string, ManaRow>()

  let row (name: string) =
    match rows.TryGetValue name with
    | true, r -> r
    | _ ->
      let r = ManaRow()
      rows[name] <- r
      r

  let tally (d: System.Collections.Generic.Dictionary<string, int>) (k: string) (n: int) =
    d[k] <- (match d.TryGetValue k with
             | true, v -> v
             | _ -> 0) + n

  let mutable paidAll = 0
  let mutable gainAll = 0
  let mutable endPctSum = 0
  let mutable shiftsTree = 0
  let mutable shiftsHuman = 0
  let mutable shiftsPaid = 0
  let mutable shiftsIdle = 0

  for s in seeds do
    let mutable w =
      Content.withRosterAt level roster (Content.scarletMonasteryAt wing.Name Content.Median s)

    let mutable guard = 0
    let mutable shiftAt = -100
    let mutable shiftName = ""

    let seat (world: World) =
      world.Entities |> List.tryFind (fun e -> e.Faction = Party && e.Role = Some Healer)

    while guard < 8000 && SimState.continues w do
      let here = seat w
      let cmds = Bench.autoPilot w
      let mark = w.Log
      let before = match here with Some h -> h.Resource | None -> 0

      // A cast in flight answers only on the Tick it lands — which is the Tick the
      // pool is charged (`SimTick.fs:638`, `SimCombat.fs:716`) — and an instant
      // answers as it is ordered, in the same Tick. Either way exactly one Tick sees
      // the drain, so nothing is counted twice.
      let landing =
        match here with
        | Some h ->
          match h.Casting with
          | Some c when c.Remaining <= ticks 1 -> Some c
          | _ -> None
        | None -> None

      let resolving =
        match here with
        | None -> None
        | Some h ->
          match landing with
          | Some c -> Some(h, c.Ability, c.Target)
          | None ->
            cmds
            |> List.tryPick (fun cmd ->
              match cmd.Kind with
              | UseAbility(aid, name, tid) when aid = h.Id ->
                h.Abilities
                |> List.tryFind (fun a -> a.Name = name)
                |> Option.map (fun a -> h, a, tid)
              | _ -> None)

      let byId (id: EntityId) = w.Entities |> List.tryFind (fun e -> e.Id = id)

      match resolving with
      | Some(h, a, tid) ->
        // A shape change is worth its 60 only if the next spell needed the shape.
        // Classified against the next ability the Druid actually resolves, however
        // many Ticks away that is, so every shift is accounted for.
        if isShift a then
          // The shift a shift replaces bought a spell that was never cast: the next
          // thing the Druid resolved wanted the other shape.
          if shiftAt >= 0 then shiftsIdle <- shiftsIdle + 1
          if a.Name = "Tree of Life" then shiftsTree <- shiftsTree + 1 else shiftsHuman <- shiftsHuman + 1
          shiftAt <- guard
          shiftName <- a.Name
        elif shiftAt >= 0 then
          if a.Stance |> Option.exists (SimState.inStance h) then shiftsPaid <- shiftsPaid + 1
          else shiftsIdle <- shiftsIdle + 1
          shiftAt <- -100

        // A heal-over-time pressed onto a target already wearing one: `Aura.advance`
        // and `Aura.refresh` keep one aura per source, so the running one is dropped
        // and replaced by a full-duration copy. Nothing still owed is lost — the
        // price of the cover already bought is paid for a second time instead.
        match hotRefresh a with
        | Some d ->
          match byId tid with
          | Some t ->
            let live = Aura.healOverTimeRemainingFor a.Name t.Auras

            if live > ticks 0 then
              let r = row a.Name
              r.Refreshes <- r.Refreshes + 1
              r.RefreshPending <- r.RefreshPending + Aura.healOverTimePendingFor a.Name t.Auras
              r.RefreshMana <- r.RefreshMana + a.ResourceCost * live / d
          | None -> ()
        | None -> ()

        // Swiftmend eats the aura whole and hands over a fixed slice of it, so the
        // ticks still owed are destroyed rather than banked (`Domain.fs:865-890`) —
        // but the slice does not shrink with the aura, so the last tick is the cheap
        // one and an early press pays twice (`SimCombat.fs:1012-1030`).
        if isConsume a then
          match byId tid with
          | Some t ->
            match Aura.swiftmend t.Auras with
            | Some(source, _) ->
              match h.Abilities |> List.tryFind (fun x -> x.Name = source) with
              | Some src ->
                match hotRefresh src with
                | Some d ->
                  let live = Aura.healOverTimeRemainingFor source t.Auras
                  let r = row "Swiftmend"
                  r.EatPending <- r.EatPending + Aura.healOverTimePendingFor source t.Auras
                  r.EatMana <- r.EatMana + src.ResourceCost * live / d
                | None -> ()
              | None -> ()
            | None -> ()
          | None -> ()
      | None -> ()

      w <- SimTick.step cmds w

      let after = seat w

      let drain =
        match after with
        | Some e -> before - e.Resource
        | None -> 0

      match resolving with
      | Some(h, a, _) ->
        // A free ability moves no pool, so it is booked on its resolution instead.
        if drain > 0 || a.ResourceCost = 0 then
          let r = row a.Name
          r.Casts <- r.Casts + 1
          r.Paid <- r.Paid + max 0 drain
          r.Listed <- r.Listed + a.ResourceCost

          let cheap =
            SimState.inStance h TreeForm
            && (a.Effects |> List.exists (function HealOverTime _ -> true | _ -> false))

          r.Expected <- r.Expected + (if cheap then a.ResourceCost * 8000 / 10000 else a.ResourceCost)
      | None -> ()

      if drain > 0 then paidAll <- paidAll + drain

      match here, after with
      | Some b, Some e when e.Resource > b.Resource -> gainAll <- gainAll + (e.Resource - b.Resource)
      | _ -> ()

      // The lines this Tick wrote, oldest first: the log is a cons list and the new
      // entries share the old tail, so walking back to the mark is exact and cheap.
      let rec unwind (l: string list) (acc: string list) =
        if obj.ReferenceEquals(l, mark) then acc
        else
          match l with
          | [] -> acc
          | h :: t -> unwind t (h :: acc)

      let lines = unwind w.Log [] |> Array.ofList

      // Which line is the *direct* half of the resolved spell? An instant heals in
      // `applyCommand`, before the tick's own aura pass, so its line is the first
      // match; a cast heals in `advanceCasts`, after it, so its line is the last.
      let directAt =
        match resolving with
        | Some(_, a, tid) when hasDirect a ->
          let who = byId tid |> Option.map (fun e -> e.Name)

          let hits =
            [ for i in 0 .. lines.Length - 1 do
                match healLine lines[i] with
                | Some(caster, spell, target, _) when caster = druidName && spell = a.Name && Some target = who ->
                  yield i
                | _ -> () ]

          if hits.IsEmpty then -1 elif landing.IsSome then List.last hits else List.head hits
        | _ -> -1

      let attempt = System.Collections.Generic.Dictionary<string, int>()
      let ticked = System.Collections.Generic.Dictionary<string, int>()
      let landed = System.Collections.Generic.Dictionary<string, int>()

      lines
      |> Array.iteri (fun i line ->
        match healLine line with
        | Some(caster, spell, target, n) when caster = druidName ->
          let r = row spell
          r.Healed <- r.Healed + n

          if i = directAt then
            r.DirectLanded <- r.DirectLanded + n
            r.DirectCasts <- r.DirectCasts + 1

            // Below the cap after the Tick means nothing was clipped, so this cast's
            // `for N` is that cast's attempt and can price the clipped ones.
            match w.Entities |> List.tryFind (fun e -> e.Name = target) with
            | Some t when t.Health < t.MaxHealth ->
              r.WholeSum <- r.WholeSum + n
              r.WholeN <- r.WholeN + 1
            | _ -> ()
          elif isHot spell then
            r.HotLanded <- r.HotLanded + n
            tally landed spell n
        | Some _ -> ()
        | None ->
          match consumeLine line with
          | Some(caster, _, n) when caster = druidName ->
            let r = row "Swiftmend"
            r.Healed <- r.Healed + n
            r.DirectLanded <- r.DirectLanded + n
          | _ -> ())

      // What each heal-over-time actually tried to heal this Tick. The tick pass runs
      // after `advanceAuras`, so an aura that fired now sits on a whole second.
      for t in w.Entities do
        if t.Faction = Party && t.Health > 0 then
          let cut = 10000 - (t.Auras |> List.sumBy Aura.healingReceivedReductionBp)

          for aura in t.Auras do
            match aura with
            | Rejuvenation(_, n, source) when Aura.firesThisTick aura && isHot source ->
              tally attempt source (n * cut / 10000)
              tally ticked source 1
            | _ -> ()

      for kv in attempt do
        let got =
          match landed.TryGetValue kv.Key with
          | true, v -> v
          | _ -> 0

        let r = row kv.Key
        r.HotAttempt <- r.HotAttempt + kv.Value
        r.HotTicks <- r.HotTicks + (match ticked.TryGetValue kv.Key with
                                    | true, v -> v
                                    | _ -> 0)
        if kv.Value > got then r.HotWaste <- r.HotWaste + (kv.Value - got)

      guard <- guard + 1

    match seat w with
    | Some e -> endPctSum <- endPctSum + SimState.resourcePct e
    | None -> ()

  let paidTotal = paidAll

  // Attempted healing, and the part of it that did not land. The tick half is
  // measured; the direct half is the mean of the casts that landed whole — those,
  // and only those, show what a cast was worth before the cap took its share — so
  // it is only printed once five of them exist.
  let overheal (r: ManaRow) =
    let directAttempt = if r.WholeN >= 5 then r.WholeSum / r.WholeN * r.Casts else 0
    let attempt = r.HotAttempt + directAttempt
    let waste = r.HotWaste + max 0 (directAttempt - r.DirectLanded)
    if attempt > 0 then Some(100.0 * float waste / float attempt, waste) else None


  printfn "Restoration Druid, Scarlet Monastery %s at level %d (library median), %d seeds" wing.Name level seedCount
  printfn "fixed seats: Warrior + Mage + Rogue + Warlock; the Druid holds the healer seat"
  printfn ""

  printfn "%-18s %5s %6s %6s %6s %7s %7s %6s %6s"
    "spell" "casts" "mana" "listed" "engine" "heal" "heal/m" "over%" "clip"

  let ordered = rows.Values |> Seq.sortByDescending (fun r -> r.Paid) |> List.ofSeq

  for r in ordered do
    let name = rows |> Seq.find (fun kv -> obj.ReferenceEquals(kv.Value, r)) |> fun kv -> kv.Key

    let over =
      match overheal r with
      | Some(pct, _) -> sprintf "%5.1f%%" pct
      | None -> "   n/a"

    let ratio =
      if r.Paid > 0 then sprintf "%7.1f" (float r.Healed / float r.Paid) else "      -"

    let clip =
      if r.DirectCasts > 0 then sprintf "%2d/%d" (r.DirectCasts - r.WholeN) r.DirectCasts else "    -"

    printfn "%-18s %5d %6d %6d %6d %7d %s %s %s"
      name r.Casts r.Paid r.Listed r.Expected r.Healed ratio over clip

  printfn ""
  printfn "listed = Ability.ResourceCost as the kit carries it (cost talents already folded in, Content.fs:4334)"
  printfn "engine = SimCombat.fs:696-714: ResourceCost, less a fifth for a HoT cast in Tree of Life"
  printfn "mana is the pool movement on the resolving Tick, so it is a floor: regen inside that Tick is netted out"
  printfn "over%%  = attempted minus landed, over attempted. Heals-over-time are measured tick by"
  printfn "        tick (the aura's own number against the line the log wrote); a direct heal only"
  printfn "        shows its attempt when nothing clipped it, so a direct figure is the mean of the"
  printfn "        casts that landed whole, applied to all of them, and needs five of them to print"
  printfn "clip   = direct casts whose heal hit the health cap, out of all direct casts seen"

  printfn ""
  printfn "heals-over-time ticks: what the auras tried against what landed"
  printfn "%-18s %7s %8s %8s %7s %6s" "spell" "ticks" "attempt" "landed" "waste" "over%"

  for r in ordered do
    if r.HotTicks > 0 then
      let name = rows |> Seq.find (fun kv -> obj.ReferenceEquals(kv.Value, r)) |> fun kv -> kv.Key
      printfn "%-18s %7d %8d %8d %7d %5.1f%%"
        name
        r.HotTicks
        r.HotAttempt
        r.HotLanded
        r.HotWaste
        (100.0 * float r.HotWaste / float (max 1 r.HotAttempt))

  printfn ""
  printfn "heals-over-time re-pressed onto a live aura"
  printfn "%-18s %6s %8s %8s" "spell" "press" "pending" "mana"

  for r in ordered do
    if r.Refreshes > 0 then
      let name = rows |> Seq.find (fun kv -> obj.ReferenceEquals(kv.Value, r)) |> fun kv -> kv.Key
      printfn "%-18s %6d %8d %8d" name r.Refreshes r.RefreshPending r.RefreshMana

  printfn "pending = healing the replaced aura still had to give (Aura.refresh keeps one per source,"
  printfn "          so the ticks are not lost, the cover already paid for is bought again)"
  printfn "mana    = the share of each re-press whose price bought time already covered"

  printfn ""
  printfn "Swiftmend"

  match rows.TryGetValue "Swiftmend" with
  | true, r ->
    printfn "  presses %d, returned %d healing, %d healing still owed on the auras it ate"
      r.Casts r.Healed r.EatPending
    printfn "  mana already invested in those eaten ticks: %d (percent-damage-free: the slice is fixed," r.EatMana
    printfn "  so an early press throws the remaining ticks away and pays the full 90 for the slice)"
  | _ -> printfn "  never pressed"

  printfn ""
  printfn "form churn: %d into Tree of Life, %d back to Human Form" shiftsTree shiftsHuman
  printfn "  of the shifts, %d were followed by a spell that needed the new shape, %d were not"
    shiftsPaid shiftsIdle
  printfn "  Human Form costs 0 and Tree of Life costs 60 (Content.fs:4456-4486); leaving is free"

  printfn ""
  printfn "efficiency, healing per point of mana"

  for r in ordered |> List.filter (fun r -> r.Paid > 0) |> List.sortByDescending (fun r -> float r.Healed / float r.Paid) do
    let name = rows |> Seq.find (fun kv -> obj.ReferenceEquals(kv.Value, r)) |> fun kv -> kv.Key
    printfn "  %-18s %7.1f  (%d casts, %d mana)" name (float r.Healed / float r.Paid) r.Casts r.Paid

  printfn ""
  printfn "summary: mana spent %d, regenerated %d, potions 0 (no potion in Content)," paidTotal gainAll
  printfn "         mean end pool %.1f%% of max, healing %.0f per seed"
    (float endPctSum / float seedCount)
    (float (rows.Values |> Seq.sumBy (fun r -> r.Healed)) / float seedCount)

/// One hole a direct heal was bought to close, and what the heal actually held
/// there. A sum over seeds, and one row per band of the heal's own size.
///
/// The band is the point. `drmana` can say which spell spent the pool; it cannot
/// say whether that spell was the right size for the hole, because a healer that
/// only owns one heal never casts a small one and so has no small-heal row to be
/// compared against. `directFor` prices a heal at `casts x ResourceCost` to close
/// the deficit (`PartyAi.fs:709-724`) and the calm branch refuses any press whose
/// heal is more than twice the hole (`PartyAi.fs:1143`) — both read the deficit,
/// so the deficit is what a kit gap has to distort. A large `over%` in the small
/// bands is a heal that owns no cheaper rung; a large `over%` in the large bands
/// is a heal pressed late onto a hole that had already been closed by somebody
/// else.
type private HoleRow() =
  member val Casts = 0 with get, set
  /// Mana the pool lost on the Tick the heal resolved.
  member val Paid = 0 with get, set
  /// The deficits the presses in this band were ordered on, summed.
  member val Deficit = 0 with get, set
  /// The target's health at the order, in health points, summed with `HealthSum`'s
  /// partner below. Printed as a percentage because the question a small band asks
  /// is "how hurt was a body that got a heal this size" — the deficit alone cannot
  /// answer it, and a hole on a nearly-full body is a different defect from the same
  /// hole on a third-health one.
  member val HealthSum = 0 with get, set
  member val MaxHealthSum = 0 with get, set
  /// The heals those presses named, summed — the mean of each range, not the top,
  /// so this is what they could be expected to heal rather than what the AI priced
  /// them at. It is the denominator of this band's `over%`.
  member val Size = 0 with get, set
  member val Landed = 0 with get, set

/// One body a direct heal was spent on, and what that cost. A healer does not choose
/// the party — the roster does — but a body the party cannot keep, like a pet with a
/// fifth of a player's health, is a price the healer pays out of the same pool, and a
/// class that owns no cheap heal pays for it with its best one. That is a cost no
/// per-spell table shows, which is why it gets its own table.
type private TargetRow() =
  member val Casts = 0 with get, set
  member val Paid = 0 with get, set
  member val Landed = 0 with get, set
  member val MaxHealth = 0 with get, set

/// `sim interrupts [n] [--class C]` — who breaks hostile casts, how often, and what
/// they broke.
///
/// An interrupt is the one thing a healing ledger cannot show: a kick costs a damage
/// button and buys nothing visible in healing, so a pass that teaches a class to press
/// one looks exactly like a pass that never does. This reads the lines
/// `SimCombat.resolveAbility` already writes on a successful break
/// (`SimCombat.fs:1185`, `"%s's %s interrupts %s's %s"` — the kick, then the cast
/// it broke) and adds no rule of its own.
///
/// `prevented` is the broken spell's own nominal size — the mean of its damage and heal
/// effects, priced off the world's own copy of the row. It is **not** a health total:
/// the roll, the crit and the mitigation are all unknown at the moment of the break, and
/// for a heal it is what the cast would have closed. Read it as "the size of the button
/// that was taken away", which is the only figure an interrupted cast has.
let private interrupts (seedCount: int) (classOverride: string option) =
  let cls = classOverride |> Option.defaultValue "Shaman"
  let roster = [ "Warrior"; cls; "Mage"; "Rogue"; "Warlock" ]

  // Every wing but the cathedral, for the same reason `activity` skips it: the three are
  // the ones a level-36 party clears, and one wing is not a sample. The library alone
  // undercounts the shaman badly — its casters stand in melee contact, so a melee kick is
  // always in reach and always wins the assignment.
  let wings = Content.scarletWings |> List.filter (fun w -> w.Name <> "cathedral")

  let tally = System.Collections.Generic.Dictionary<string, int>()
  let value = System.Collections.Generic.Dictionary<string, int>()
  let rows = System.Collections.Generic.Dictionary<string, (int * int)>()
  let abilities = System.Collections.Generic.Dictionary<string, Ability>()

  // The kick half of the breakdown: per actor+spell so a class's credit can be
  // told apart from its neighbours', and rolled up per spell for the party-wide
  // share. Keyed on the log line's own words, so no second format exists to rot.
  let kicks = System.Collections.Generic.Dictionary<string, int>()
  let spells = System.Collections.Generic.Dictionary<string, int>()
  // The misuse half: a kick pressed while no cast was underway lands as plain
  // damage (`"%s's %s hits %s for %d"`, SimCombat.fs:1289) or is resisted
  // (`"%s resists %s"`, SimCombat.fs:262). Careful: this counts damage
  // *applications*, and most of them ride on successful breaks — a kick that
  // breaks a cast also logs its damage here. It is NOT the misuse detector; the
  // `kicksLate` table below is, because a press that finds no cast always logs
  // `"fails to interrupt"` regardless of damage. Misses, dodges and parries name
  // no spell (SimCombat.fs:439-441) and so cannot be attributed here.
  let kicksAsDamage = System.Collections.Generic.Dictionary<string, int>()
  // And the exact fingerprint of a late kick: an `Interrupt` application that
  // found no cast to break logs `"%s fails to interrupt %s"` (SimCombat.fs:1187)
  // even when the ability also deals damage. Counted per actor, this is the
  // number of kick presses that arrived after the cast was already gone (or
  // were aimed at nothing) — the difference between a damage-use and the
  // one-tick decision lag documented at the tank's sweep (PartyAi.fs:516-521).
  let kicksLate = System.Collections.Generic.Dictionary<string, int>()

  let bump (d: System.Collections.Generic.Dictionary<string, int>) (k: string) (n: int) =
    d[k] <- (match d.TryGetValue k with | true, was -> was + n | _ -> n)

  let nominal (a: Ability) =
    a.Effects
    |> List.sumBy (function
      | Damage(lo, hi)
      | SpellDamage(lo, hi, _)
      | Heal(lo, hi) -> (lo + hi) / 2
      | WeaponDamage(_, bonus) -> bonus
      | HealOverTime(duration, perTick) -> (duration / secTicks 1.0) * perTick
      | _ -> 0)

  for wing in wings do
    for s in [ 1UL .. uint64 seedCount ] do
      let mutable w =
        Content.withRosterAt wing.Median roster (Content.scarletMonasteryAt wing.Name Content.Median s)

      // Every row in play, before the run: a broken cast is identified by name in the log
      // and the row has to be found again to be priced.
      for e in w.Entities do
        for a in e.Abilities do
          abilities[a.Name] <- a

      let mutable guard = 0

      while guard < 8000 && SimState.continues w do
        w <- SimTick.step (Bench.autoPilot w) w
        guard <- guard + 1

      for line in w.Log do
        match line.Split([| " interrupts " |], System.StringSplitOptions.None) with
        | [| who; rest |] ->
          // The line reads "Shaman's Earth Shock interrupts Chaplain's Heal": the
          // who half carries the kick that landed, the rest half the cast it broke.
          let actor, kick =
            match who.Split([| "'s " |], System.StringSplitOptions.None) with
            | [| name; spell |] -> name, spell
            | _ -> who, "?"

          let spell =
            match rest.Split([| "'s " |], System.StringSplitOptions.None) with
            | [| _; name |] -> Some name
            | _ -> None

          let size =
            spell
            |> Option.bind (fun n ->
              match abilities.TryGetValue n with
              | true, a -> Some(nominal a)
              | _ -> None)
            |> Option.defaultValue 0

          bump tally actor 1
          bump value actor size
          bump kicks (sprintf "%s's %s" actor kick) 1
          bump spells kick 1

          match spell with
          | Some n ->
            let casts, worth =
              match rows.TryGetValue n with
              | true, was -> was
              | _ -> (0, 0)

            rows[n] <- (casts + 1, worth + size)
          | None -> ()
        | _ ->
          let asKickUse (actor: string) (spell: string) =
            match abilities.TryGetValue spell with
            | true, a when a.Effects |> List.exists (function Interrupt -> true | _ -> false) ->
              bump kicksAsDamage (sprintf "%s's %s" actor spell) 1
            | _ -> ()

          match line.Split([| " fails to interrupt " |], System.StringSplitOptions.None) with
          | [| who; _ |] -> bump kicksLate who 1
          | _ ->
            match line.Split([| " hits " |], System.StringSplitOptions.None) with
            | [| who; _ |] ->
              match who.Split([| "'s " |], System.StringSplitOptions.None) with
              | [| actor; spell |] -> asKickUse actor spell
              | _ -> ()
            | _ ->
              match line.Split([| " resists " |], System.StringSplitOptions.None) with
              | [| _; spell |] -> asKickUse "?" spell
              | _ -> ()

  let total = tally.Values |> Seq.sum
  let prevented = value.Values |> Seq.sum

  printfn
    "interrupts landed: %d over %d seeds x %d wings (%s roster: %s)"
    total
    seedCount
    (List.length wings)
    cls
    (wings |> List.map (fun w -> w.Name) |> String.concat ", ")
  printfn "prevented (nominal, the size of the buttons taken away): %d" prevented
  printfn ""
  printfn "%-12s %8s %10s" "actor" "broken" "prevented"

  for kvp in tally |> Seq.sortByDescending (fun kvp -> kvp.Value) do
    printfn "%-12s %8d %10d" kvp.Key kvp.Value (match value.TryGetValue kvp.Key with | true, v -> v | _ -> 0)

  printfn ""
  printfn "%-28s %8s %10s" "spell broken" "count" "prevented"

  for kvp in rows |> Seq.sortByDescending (fun kvp -> snd kvp.Value) do
    printfn "%-28s %8d %10d" kvp.Key (fst kvp.Value) (snd kvp.Value)

  printfn ""
  printfn "%-28s %8s" "kick used" "count"

  for kvp in kicks |> Seq.sortByDescending (fun kvp -> kvp.Value) do
    printfn "%-28s %8d" kvp.Key kvp.Value

  printfn ""
  printfn "%-28s %8s %6s" "spell kicked with" "count" "share"

  for kvp in spells |> Seq.sortByDescending (fun kvp -> kvp.Value) do
    printfn "%-28s %8d %5.1f%%" kvp.Key kvp.Value (100.0 * float kvp.Value / float total)

  printfn ""
  printfn "%-28s %8s" "kick damage applications" "count"
  printfn "(damage logged by kick presses; most ride on successful breaks)"

  if kicksAsDamage.Count = 0 then
    printfn "(none — every kick landed on a cast)"
  else
    for kvp in kicksAsDamage |> Seq.sortByDescending (fun kvp -> kvp.Value) do
      printfn "%-28s %8d" kvp.Key kvp.Value

  printfn ""
  printfn "%-12s %8s" "late kicks" "count"

  if kicksLate.Count = 0 then
    printfn "(none — no kick ever found an empty target)"
  else
    for kvp in kicksLate |> Seq.sortByDescending (fun kvp -> kvp.Value) do
      printfn "%-12s %8d" kvp.Key kvp.Value

/// `sim shmana [n] [--level L] [--class C]` — the seated healer's mana, booked
/// against the ability that spent it and, for every direct heal, against the hole
/// it was ordered on. Diagnostic only: no rule is added and no number changes.
///
/// `--class` exists so the same ledger can be run over the Priest, the Paladin and
/// the Druid: a cost is only a defect next to the class that does not pay it, and
/// the ruling this command is built to test is "the Shaman is worse per mana than
/// its peers", not "the Shaman is bad". The default is the class the question is
/// about; nothing else about the run changes with it.
let private shmana (seedCount: int) (classOverride: string option) (levelOverride: int option) =
  let cls = classOverride |> Option.defaultValue "Shaman"
  let roster = [ "Warrior"; cls; "Mage"; "Rogue"; "Warlock" ]
  let wing = Content.wingOf "library"
  let level = levelOverride |> Option.defaultValue (Content.levelOf Content.Median wing)
  let seeds = [ 1UL .. uint64 seedCount ]

  let probe =
    Content.withRosterAt level roster (Content.scarletMonasteryAt wing.Name Content.Median 1UL)

  let thisHealer =
    probe.Entities |> List.find (fun e -> e.Faction = Party && e.Role = Some Healer)

  let healerName = thisHealer.Name

  /// The size the AI itself prices a heal at (`PartyAi.fs:592-598`): the top of the
  /// range for a direct heal, the whole of the ticks for a heal-over-time. Read the
  /// same way here so a hole can be compared against what the healer thought it was
  /// buying, not against what it happened to roll.
  let healsOf (a: Ability) =
    a.Effects
    |> List.sumBy (function
      | Heal(_, mx) -> mx
      | HealOverTime(duration, perTick) -> (duration / secTicks 1.0) * perTick
      | _ -> 0)

  let hasDirect (a: Ability) = a.Effects |> List.exists (function Heal _ -> true | _ -> false)

  let hasHot (a: Ability) = a.Effects |> List.exists (function HealOverTime _ -> true | _ -> false)

  let isHeal (a: Ability) = hasDirect a || hasHot a

  /// The mean of a heal's own range — what a press is worth on average, which is
  /// the figure a band's attempted healing has to be counted in. `healsOf` above is
  /// the top of the range on purpose (the AI prices itself optimistically), so the
  /// two are deliberately different numbers.
  let meanOf (a: Ability) =
    a.Effects
    |> List.sumBy (function
      | Heal(lo, hi) -> (lo + hi) / 2
      | HealOverTime _ -> 0
      | _ -> 0)

  /// A button that hits something. `Ability.group` is the file's one answer to
  /// that question (`Domain.fs:1335-1368`), so a damage lane measured through it
  /// cannot drift from the abilities the tree actually forbids.
  let hurts (a: Ability) =
    match Ability.group a with
    | AbilityGroup.Damage
    | AbilityGroup.DamageOverTime -> true
    | _ -> false

  let rows = System.Collections.Generic.Dictionary<string, ManaRow>()

  let row (name: string) =
    match rows.TryGetValue name with
    | true, r -> r
    | _ ->
      let r = ManaRow()
      rows[name] <- r
      r

  let targets = System.Collections.Generic.Dictionary<string, TargetRow>()

  let targetRow (name: string) =
    match targets.TryGetValue name with
    | true, r -> r
    | _ ->
      let r = TargetRow()
      targets[name] <- r
      r

  // Six bands, in the order they are printed: nothing left to close, then
  // quarters of the heal, then a hole the heal does not fill. A quarter is the
  // granularity `healerOrders` already reasons at — its calm gate is "the heal is
  // no more than twice the hole", which is the 50% band exactly.
  let bandLabels =
    [| "nothing to close"; "1-25% of heal"; "25-50% of heal"; "50-75% of heal"; "75-100% of heal"; "over the heal" |]

  let bands = Array.init bandLabels.Length (fun _ -> HoleRow())

  let mutable blockedMana = 0
  let mutable blockedNearDamage = 0
  let mutable damagePaid = 0
  let mutable healerTicks = 0
  let mutable paidAll = 0
  let mutable gainAll = 0
  let mutable endPctSum = 0

  for s in seeds do
    let mutable w =
      Content.withRosterAt level roster (Content.scarletMonasteryAt wing.Name Content.Median s)

    let mutable guard = 0
    let mutable lastDamage = -100000

    let seat (world: World) =
      world.Entities |> List.tryFind (fun e -> e.Faction = Party && e.Role = Some Healer)

    // The band the heal in flight was ordered on. A cast answers three seconds
    // after it was decided, and the deficit three seconds later is not the deficit
    // the rule read — so the band is recorded at the order and carried to the Tick
    // the mana leaves. Only one slot is needed: a healer holds one cast at a time,
    // and `SimCombat` ignores a second order while it does (`SimCombat.fs:1957`),
    // so an order is only recorded when the hands are free.
    let mutable pendingBand = -1
    let mutable pendingName = ""
    let mutable pendingDeficit = 0
    let mutable pendingSize = 0
    let mutable pendingHealth = 0
    let mutable pendingMax = 0
    let mutable pendingWho = ""

    while guard < 8000 && SimState.continues w do
      let here = seat w
      let cmds = Bench.autoPilot w
      let mark = w.Log
      let before = match here with Some h -> h.Resource | None -> 0

      let free = match here with Some h -> h.Casting.IsNone | None -> false

      let landing =
        match here with
        | Some h ->
          match h.Casting with
          | Some c when c.Remaining <= ticks 1 -> Some c
          | _ -> None
        | None -> None

      let resolving =
        match here with
        | None -> None
        | Some h ->
          match landing with
          | Some c -> Some(h, c.Ability, c.Target)
          | None ->
            cmds
            |> List.tryPick (fun cmd ->
              match cmd.Kind with
              | UseAbility(aid, name, tid) when aid = h.Id ->
                h.Abilities
                |> List.tryFind (fun a -> a.Name = name)
                |> Option.map (fun a -> h, a, tid)
              | _ -> None)

      let byId (id: EntityId) = w.Entities |> List.tryFind (fun e -> e.Id = id)

      let standby =
        match here with
        | Some h -> Some(PartyAi.healerTick w h cmds)
        | None -> None

      // The hole, read on the state the order was decided in, before the Tick. Only
      // a direct heal is banded: a heal-over-time is bought against the bar it is
      // expected to outrun, not against a deficit it can never cover, and putting
      // one in this table would file every cheap HoT under "hole bigger than the
      // heal" and make the four classes incomparable.
      match resolving, free with
      | Some(h, a, tid), true when hasDirect a ->
        match byId tid with
        | Some t ->
          let deficit =
            max 0 (t.MaxHealth - (t.Health + Aura.healOverTimePending t.Auras))

          let size = max 1 (healsOf a)

          // `(deficit - 1)` so that a deficit of one point is the first band rather
          // than the second, and `min 5` so that a hole bigger than the heal stays
          // in its own band instead of spilling into the 75% one.
          let band =
            if deficit = 0 then 0 else min 5 (1 + (deficit - 1) * 4 / size)

          pendingBand <- band
          pendingName <- a.Name
          pendingDeficit <- deficit
          pendingSize <- meanOf a
          pendingHealth <- t.Health
          pendingMax <- t.MaxHealth
          pendingWho <- t.Name
        | None -> ()
      | _ -> ()

      w <- SimTick.step cmds w

      let after = seat w

      let drain =
        match after with
        | Some e -> before - e.Resource
        | None -> 0

      let mutable bookedBand = -1

      match resolving with
      | Some(h, a, _) ->
        if drain > 0 || a.ResourceCost = 0 then
          let r = row a.Name
          r.Casts <- r.Casts + 1
          r.Paid <- r.Paid + max 0 drain
          r.Listed <- r.Listed + a.ResourceCost

          let cheap =
            SimState.inStance h TreeForm
            && (a.Effects |> List.exists (function HealOverTime _ -> true | _ -> false))

          r.Expected <- r.Expected + (if cheap then a.ResourceCost * 8000 / 10000 else a.ResourceCost)

        // The press is booked against the hole it was decided on, and only once it
        // has actually been paid for — a heal that is interrupted never drains a
        // pool, so booking it at order time would credit a hole nobody closed.
        // `bookedBand` is handed to the log pass below as well, so the healing that
        // landed is filed on the same band its mana was: read the slot once, here,
        // and the two halves of the row can never disagree.
        if hasDirect a && drain > 0 && pendingName = a.Name then
          let b = bands[pendingBand]
          b.Casts <- b.Casts + 1
          b.Paid <- b.Paid + drain
          b.Deficit <- b.Deficit + pendingDeficit
          b.Size <- b.Size + pendingSize
          b.HealthSum <- b.HealthSum + pendingHealth
          b.MaxHealthSum <- b.MaxHealthSum + pendingMax
          bookedBand <- pendingBand

          // Who received it. A healer has no say over which bodies the game puts in
          // its party, but it does have to pay for them, and a body the party could
          // not save anyway is a price worth seeing on its own line.
          let who = targetRow pendingWho
          who.Casts <- who.Casts + 1
          who.Paid <- who.Paid + drain
          who.MaxHealth <- pendingMax

        if hurts a && drain > 0 then
          damagePaid <- damagePaid + drain
          lastDamage <- guard
      | None -> ()

      if drain > 0 then paidAll <- paidAll + drain

      match here, after with
      | Some b, Some e when e.Resource > b.Resource -> gainAll <- gainAll + (e.Resource - b.Resource)
      | _ -> ()

      // Blocked-on-mana ticks, and how many of them sat directly behind a cast the
      // healer made itself. The count is not a causal claim on its own; it is what
      // makes the claim checkable, because the spend that emptied the pool is in the
      // table above it.
      healerTicks <- healerTicks + 1

      match standby with
      | Some(PartyAi.Blocked PartyAi.ShortOfMana) ->
        blockedMana <- blockedMana + 1

        if lastDamage >= 0 && guard - lastDamage < int (secTicks 20.0) then
          blockedNearDamage <- blockedNearDamage + 1
      | _ -> ()

      // The lines this Tick wrote, oldest first.
      let rec unwind (l: string list) (acc: string list) =
        if obj.ReferenceEquals(l, mark) then acc
        else
          match l with
          | [] -> acc
          | h :: t -> unwind t (h :: acc)

      let lines = unwind w.Log [] |> Array.ofList

      let directAt =
        match resolving with
        | Some(_, a, tid) when hasDirect a ->
          let who = byId tid |> Option.map (fun e -> e.Name)

          let hits =
            [ for i in 0 .. lines.Length - 1 do
                match healLine lines[i] with
                | Some(caster, spell, target, _) when caster = healerName && spell = a.Name && Some target = who ->
                  yield i
                | _ -> () ]

          if hits.IsEmpty then -1 elif landing.IsSome then List.last hits else List.head hits
        | _ -> -1

      lines
      |> Array.iteri (fun i line ->
        match healLine line with
        | Some(caster, spell, target, n) when caster = healerName ->
          let r = row spell
          r.Healed <- r.Healed + n

          if i = directAt then
            r.DirectLanded <- r.DirectLanded + n
            r.DirectCasts <- r.DirectCasts + 1

            let whole =
              match w.Entities |> List.tryFind (fun e -> e.Name = target) with
              | Some t when t.Health < t.MaxHealth -> true
              | _ -> false

            if whole then
              r.WholeSum <- r.WholeSum + n
              r.WholeN <- r.WholeN + 1

            // The band's own copy of the same measurement. A cast that resolved on
            // a band boundary is filed where it was *ordered*, which is the band the
            // decision rule saw.
            if bookedBand >= 0 then
              bands[bookedBand].Landed <- bands[bookedBand].Landed + n
              (targetRow target).Landed <- (targetRow target).Landed + n
        | Some _ -> ()
        | None -> ())

      // The slot is cleared only after the log has been read, so the two halves of a
      // press are always filed on one band. Deleting it at the drain would leave the
      // healing half with nothing to book against.
      if bookedBand >= 0 then
        pendingBand <- -1
        pendingName <- ""

      guard <- guard + 1

    match seat w with
    | Some e -> endPctSum <- endPctSum + SimState.resourcePct e
    | None -> ()

  let nameOf (r: ManaRow) =
    rows |> Seq.find (fun kv -> obj.ReferenceEquals(kv.Value, r)) |> fun kv -> kv.Key

  let overheal (r: ManaRow) =
    let directAttempt = if r.WholeN >= 5 then r.WholeSum / r.WholeN * r.Casts else 0
    let attempt = r.HotAttempt + directAttempt
    let waste = r.HotWaste + max 0 (directAttempt - r.DirectLanded)
    if attempt > 0 then Some(100.0 * float waste / float attempt) else None

  printfn "%s, Scarlet Monastery %s at level %d (library median), %d seeds" thisHealer.Name wing.Name level seedCount
  printfn "fixed seats: Warrior + Mage + Rogue + Warlock; %s holds the healer seat" cls
  printfn ""

  printfn "%-24s %5s %7s %7s %6s %8s %7s %6s %7s"
    "spell" "casts" "mana" "listed" "heal/m" "heal" "over%" "clip" "share"

  let ordered = rows.Values |> Seq.sortByDescending (fun r -> r.Paid) |> List.ofSeq

  for r in ordered do
    let over =
      match overheal r with
      | Some pct -> sprintf "%5.1f%%" pct
      | None -> "   n/a"

    let ratio = if r.Paid > 0 then sprintf "%7.2f" (float r.Healed / float r.Paid) else "      -"

    let clip =
      if r.DirectCasts > 0 then sprintf "%2d/%d" (r.DirectCasts - r.WholeN) r.DirectCasts else "    -"

    let share =
      if paidAll > 0 then sprintf "%6.1f%%" (100.0 * float r.Paid / float paidAll) else "     -"

    printfn "%-24s %5d %7d %7d %s %8d %s %s %s"
      (nameOf r) r.Casts r.Paid r.Listed ratio r.Healed over clip share

  printfn ""
  printfn "mana   = the pool movement on the Tick the spell resolved, so it is a floor (regen is netted out)"
  printfn "heal/m = healing the log credits the row, over that mana"
  printfn "over%%  = attempted minus landed, over attempted; a direct heal's attempt is the mean of the"
  printfn "         casts that landed whole, applied to all of them, and needs five of those to print"
  printfn "clip   = direct casts that hit the health cap, out of all direct casts seen"

  printfn ""
  printfn "every direct heal, booked against the hole it was ordered on"
  printfn "%-18s %6s %7s %7s %6s %8s %7s"
    "hole" "casts" "mana" "deficit" "hp" "healed" "over%"

  let totalHole = bands |> Array.sumBy (fun b -> b.Casts)

  for i in 0 .. bands.Length - 1 do
    let b = bands[i]

    if b.Casts > 0 then
      let over =
        if b.Size > 0 then
          sprintf "%5.1f%%" (100.0 * float (max 0 (b.Size - b.Landed)) / float b.Size)
        else "   n/a"

      printfn "%-18s %6d %7d %7d %3d/%d %8d %s"
        (sprintf "%s (%d%%)" bandLabels[i] (100 * b.Casts / max 1 totalHole))
        b.Casts
        b.Paid
        b.Deficit
        (b.HealthSum / max 1 b.Casts)
        (b.MaxHealthSum / max 1 b.Casts)
        b.Landed
        over

  printfn ""
  printfn "hole = the deficit the press was decided on (max health less health less the ticks already owed),"
  printfn "       as a share of the heal the press named; `deficit` is that sum in health, not a share"
  printfn "hp   = the target's real health over its maximum at the order, averaged: a small hole on a"
  printfn "       nearly-full body and a small hole on a dying one are different defects"
  printfn "over%% here is the named heals' mean sizes against what they landed, so a small hole closed"
  printfn "with a big heal shows its own number instead of hiding inside the spell's average"

  printfn ""
  printfn "who the direct heals were spent on"
  printfn "%-18s %6s %7s %8s %8s %7s" "target" "casts" "mana" "max hp" "healed" "heal/m"

  for kv in targets |> Seq.sortByDescending (fun kv -> kv.Value.Paid) do
    let t = kv.Value
    let ratio = if t.Paid > 0 then sprintf "%7.2f" (float t.Landed / float t.Paid) else "      -"
    printfn "%-18s %6d %7d %8d %8d %s" kv.Key t.Casts t.Paid t.MaxHealth t.Landed ratio

  printfn "max hp is the body's own maximum, so a row whose max is a fraction of a player's is a pet;"
  printfn "heal/m on that row is what a press on it was worth, and it is the number a cheap rung would fix"

  printfn ""
  printfn "mana sold to the damage lane: %d over %d seeds (%d a seed)" damagePaid seedCount (damagePaid / max 1 seedCount)

  printfn "blocked on mana: %d ticks (%.1f%% of %d healer ticks), %d of them within 20 s of the healer's own damage cast"
    blockedMana
    (100.0 * float blockedMana / float (max 1 healerTicks))
    healerTicks
    blockedNearDamage

  printfn ""
  printfn "summary: mana spent %d, regenerated %d, mean end pool %.1f%% of max, healing %.0f per seed"
    paidAll
    gainAll
    (float endPctSum / float seedCount)
    (float (rows.Values |> Seq.sumBy (fun r -> r.Healed)) / float seedCount)

/// `sim totems [n]` — the run's totem ledger, straight off the World.
///
/// A totem pulse is silent: auras tick without a log line, and every other
/// diagnostic here reads the log, so the set's work — the health the stream
/// saved the healer, the mana the spring handed the casters, the bodies a boon
/// reached — is invisible to `attrib` and `shmana` alike. The numbers exist only
/// in the ledger `SimTick.advanceTotems` and the summon path keep (`World.
/// TotemLedger`), so this reads them and adds no rule of its own. The same run
/// `shmana` looks at — library, Median, the Shaman holding the healer seat —
/// so the totem's numbers can be read against the healer's own budget.
let private totems (seedCount: int) =
  let roster = [ "Warrior"; "Shaman"; "Mage"; "Rogue"; "Warlock" ]
  let wing = Content.wingOf "library"
  let level = Content.levelOf Content.Median wing
  let seeds = [ 1UL .. uint64 seedCount ]

  // One row per totem name, summed over the seeds. `Reached` cannot be summed
  // across runs — Entity ids are per-World — so it is kept as a mean of the
  // per-run distinct counts instead.
  let rows = System.Collections.Generic.Dictionary<string, TotemTally>()
  let reached = System.Collections.Generic.Dictionary<string, int>()
  let mutable cleared = 0

  for s in seeds do
    let mutable w =
      Content.withRosterAt level roster (Content.scarletMonasteryAt wing.Name Content.Median s)

    let mutable guard = 0

    // The wing's own clear time is nine to eleven thousand Ticks (`sim attrib`'s
    // default limit is 12000 for that reason), so an 8000 guard — `shmana`'s —
    // would read every successful run as unresolved and count its last pulls
    // in no ledger at all.
    while guard < 12000 && SimState.continues w do
      w <- SimTick.step (Bench.autoPilot w) w
      guard <- guard + 1

    if SimState.aliveHostiles w |> List.isEmpty then
      cleared <- cleared + 1

    for kv in w.TotemLedger do
      let t =
        match rows.TryGetValue kv.Key with
        | true, t -> t
        | _ -> TotemLedger.zero

      rows[kv.Key] <-
        { t with
            Drops = t.Drops + kv.Value.Drops
            Pulses = t.Pulses + kv.Value.Pulses
            HealthDelivered = t.HealthDelivered + kv.Value.HealthDelivered
            ManaDelivered = t.ManaDelivered + kv.Value.ManaDelivered }

      let r =
        match reached.TryGetValue kv.Key with
        | true, r -> r
        | _ -> 0

      reached[kv.Key] <- r + Set.count kv.Value.Reached

  printfn
    "Shaman totems, Scarlet Monastery library at level %d (library median), %d seeds"
    level
    seedCount

  printfn "fixed seats: Warrior + Mage + Rogue + Warlock; Shaman holds the healer seat"
  printfn "runs cleared: %d/%d" cleared seedCount
  printfn ""
  printfn "%-22s %6s %8s %9s %9s %10s" "totem" "drops" "pulses" "hp given" "mana" "reach/run"

  let total = ref TotemLedger.zero

  for kv in rows |> Seq.sortByDescending (fun kv -> kv.Value.Drops) do
    let t = kv.Value
    total :=
      { Drops = total.Value.Drops + t.Drops
        Pulses = total.Value.Pulses + t.Pulses
        HealthDelivered = total.Value.HealthDelivered + t.HealthDelivered
        ManaDelivered = total.Value.ManaDelivered + t.ManaDelivered
        Reached = Set.empty }

    let reach =
      match reached.TryGetValue kv.Key with
      | true, r -> r
      | _ -> 0

    printfn
      "%-22s %6d %8d %9d %9d %10.1f"
      kv.Key
      t.Drops
      t.Pulses
      t.HealthDelivered
      t.ManaDelivered
      (float reach / float seedCount)

  let t = total.Value

  printfn "%-22s %6d %8d %9d %9d" "-- total" t.Drops t.Pulses t.HealthDelivered t.ManaDelivered
  printfn ""
  printfn "drops  = the element was dropped, including over its own standing totem"
  printfn "pulses = one per party member a pulse reached; hp and mana are what actually landed"
  printfn "         (deficit-capped), so the stream's hp is healing the healer never had to cast"
  printfn "reach  = distinct party members reached, averaged per run; Grounding reads at spell"
  printfn "         resolution, not on a pulse, so its pulses are zero by design"

/// `sim seat [n] [wing] [--class C]` — the fifth seat, over many seeds.
///
/// `attrib --class C` answers one seed's by-spell split; this answers what the
/// swap costs the party. The fifth seat is the Druid's: the default roster runs
/// two healers, and a damage spec seated there leaves one. One seed showed the
/// cat seat wiping at library's tick limit; this is the multi-seed version, with
/// the bare `Druid` as the control — same seat, same seeds, tree spec.
let private seat (seedCount: int) (wingName: string) (classOverride: string option) (meleeMode: bool) =
  let wing = Content.wingOf wingName
  let level = Content.levelOf Content.Median wing
  let seatClass = classOverride |> Option.defaultValue "Druid"

  // `--melee` puts the seat in the ROGUE's slot with a Warlock behind it, so the
  // A/B is the melee body itself under identical support: cat against rogue, one
  // Priest healing both, same seeds. The default mode keeps the Druid's fifth
  // slot and its one-healer question. A spec string books under its class name
  // (a Druid (Cat) logs as Druid), and the roster in either mode holds at most
  // one member of that name.
  let roster =
    if meleeMode then [ "Warrior"; "Priest"; "Mage"; seatClass; "Warlock" ]
    else [ "Warrior"; "Priest"; "Mage"; "Rogue"; seatClass ]

  // The seat's log name: a spec suffix never reaches the combat log.
  let seatName = if seatClass.StartsWith "Druid" then "Druid" else seatClass

  let mutable cleared, wiped, limit = 0, 0, 0
  let mutable seatDied = 0
  let mutable clearTicks = 0
  let mutable seatDamage = 0
  let mutable seatHealing = 0
  let mutable priestHealing = 0

  // Whether the Rogue's escape buttons ever fire is a question the damage
  // column cannot answer: a Vanish that never happens and one that changes
  // nothing both leave it flat, and the log is the only witness.
  let mutable vanishCasts = 0
  let mutable blindCasts = 0

  for s in [ 1UL .. uint64 seedCount ] do
    let mutable w =
      Content.withRosterAt level roster (Content.scarletMonasteryAt wing.Name Content.Median s)

    // The wing's own clear time runs nine to eleven thousand ticks, so the guard
    // must not be tighter than `attrib`'s 12000 or every clear reads unresolved.
    let mutable guard = 0

    while guard < 12000 && SimState.continues w do
      w <- SimTick.step (Bench.autoPilot w) w
      guard <- guard + 1

    let o = SimState.outcome w

    // The seat is found by its roster name, unique in the roster as built. Read at
    // the end, not on the death line: the tree can Rebirth a corpse, the cat
    // cannot, and either way the number that matters is who is standing when the
    // run stops.
    let seatAlive =
      w.Entities |> List.exists (fun e -> e.Faction = Party && SimState.alive e && e.Name = seatName)

    let totals = Attribution.byCaster w

    let seatT = totals |> Map.tryFind seatName |> Option.defaultValue Attribution.empty
    let priestT = totals |> Map.tryFind "Priest" |> Option.defaultValue Attribution.empty

    (match o with
     | EncounterCleared -> cleared <- cleared + 1; clearTicks <- clearTicks + guard
     | PartyWiped -> wiped <- wiped + 1
     | Running -> limit <- limit + 1)

    if not seatAlive then seatDied <- seatDied + 1

    vanishCasts <- vanishCasts + (w.Log |> List.filter (fun l -> l = sprintf "%s vanishes" seatName) |> List.length)
    blindCasts <- blindCasts + (w.Log |> List.filter (fun l -> l.StartsWith(sprintf "%s puts " seatName)) |> List.length)

    seatDamage <- seatDamage + seatT.Damage
    seatHealing <- seatHealing + seatT.Healing
    priestHealing <- priestHealing + priestT.Healing

    printfn
      "seed %d  %-16A ticks=%-5d seat-alive=%-5b seat dmg=%d heal=%d priest heal=%d"
      s
      o
      guard
      seatAlive
      seatT.Damage
      seatT.Healing
      priestT.Healing

  printfn ""
  printfn "seat: %s in %s at level %d, %d seeds" seatClass wing.Name level seedCount
  printfn "roster: %A" roster
  printfn "cleared %d  wiped %d  unresolved %d" cleared wiped limit

  if cleared > 0 then
    printfn "mean ticks to clear: %.0f" (float clearTicks / float cleared)

  printfn
    "seat died in %d/%d; mean per seed: seat %d dmg, %d healing; priest %d healing"
    seatDied
    seedCount
    (seatDamage / max seedCount 1)
    (seatHealing / max seedCount 1)
    (priestHealing / max seedCount 1)

  printfn "escape buttons: vanish %d, blind %d casts across all seeds" vanishCasts blindCasts
  printfn "direct damage and healing only — aura ticks emit no line and are not counted"

/// `sim droods [n]` — the operator's Druid-only question: a party of five Druids,
/// one per shape (bear tank, tree healer, cat melee, moonkin ranged, and a second
/// tree so the healing matches what every clearing roster in this campaign carried).
/// Every spec books under the log name "Druid", so per-seat attribution is
/// impossible by name and only the party outcome is reported — the question is
/// viability, not who carried.
let private droods (seedCount: int) =
  let roster =
    [ "Druid (Bear)"; "Druid"; "Druid (Cat)"; "Druid (Moonkin)"; "Druid" ]

  let maps =
    Content.scarletWings
    |> List.filter (fun w -> w.Name <> "cathedral")
    |> List.map (fun w ->
      w.Name, (fun (s: uint64) -> Content.scarletMonasteryAt w.Name Content.Median s), w.Median)

  printfn "Druid-only party, %d seeds — %A" seedCount roster

  for (name, make, at) in maps do
    let mutable cleared, wiped, limit = 0, 0, 0
    let mutable clearTicks = 0
    let mutable partyDamage = 0
    let mutable partyHealing = 0

    for s in [ 1UL .. uint64 seedCount ] do
      let mutable w = Content.withRosterAt at roster (make s)
      let mutable guard = 0

      while guard < 12000 && SimState.continues w do
        w <- SimTick.step (Bench.autoPilot w) w
        guard <- guard + 1

      (match SimState.outcome w with
       | EncounterCleared -> cleared <- cleared + 1; clearTicks <- clearTicks + guard
       | PartyWiped -> wiped <- wiped + 1
       | Running -> limit <- limit + 1)

      // All five seats log as "Druid", so the single row is the whole party.
      let party =
        Attribution.byCaster w |> Map.tryFind "Druid" |> Option.defaultValue Attribution.empty

      partyDamage <- partyDamage + party.Damage
      partyHealing <- partyHealing + party.Healing

      printfn "  seed %d  %-16A ticks=%-5d" s (SimState.outcome w) guard

    printf "  %s: cleared %d  wiped %d  unresolved %d" name cleared wiped limit

    if cleared > 0 then printf "  mean clear %.0f" (float clearTicks / float cleared)

    printfn "  party %d dmg, %d healing per seed" (partyDamage / max seedCount 1) (partyHealing / max seedCount 1)

  printfn "direct damage and healing only — aura ticks emit no line and are not counted"

/// `sim ttk [n] [--level L]` — the healer's damage gate, with its inputs on show.
///
/// The aggregate tables are not evidence that a forecast is right; they are only
/// evidence that something changed. This prints, per sampled Tick, the numbers the gate
/// actually decided on — health still standing, the party's recent damage rate, the
/// seconds it implies, the healer's net pool draw, the reserve that draw implies, the
/// pool that reserve is drawn from, and the budget left over — beside the order the
/// healer asked for and the verdict. A forecast that is wrong here is wrong visibly.
let private ttk (seedCount: int) (levelOverride: int option) =
  let wing = Content.wingOf "library"
  let level = levelOverride |> Option.defaultValue (Content.levelOf Content.Median wing)

  printfn "Scarlet Monastery %s at level %d, %d seeds — healer damage gate" wing.Name level seedCount
  printfn ""

  printfn
    "hp = effective health still standing in the pull; dps = party damage, EWMA over 4 s of Ticks"

  printfn
    "ttk = hp / dps; pack = engaged-set generation (up only when a new pack joins); drain/t = healer's net pool draw per Tick (spend less regen)"

  printfn
    "reserve = drain/t x remaining Ticks x 1.25 + the heals that can still land (floor 2, scaled by the healer's slowest heal cast)"

  printfn ""

  for s in [ 1UL .. uint64 seedCount ] do
    let roster = [ "Warrior"; "Priest"; "Mage"; "Rogue"; "Warlock" ]

    let mutable w =
      Content.withRosterAt level roster (Content.scarletMonasteryAt wing.Name Content.Median s)

    // Samples are 20 s apart, plus the first Tick of the pull: the opening second is
    // exactly where a rate-based forecast is most likely to be wrong, so showing it is
    // the point of this command.
    let every = int (secTicks 20.0)
    let mutable guard = 0
    let mutable fought = 0
    let mutable rows = 0
    let mutable header = false

    while guard < 8000 && SimState.continues w do
      let here =
        w.Entities |> List.tryFind (fun e -> e.Faction = Party && e.Role = Some Healer)

      let cmds = Bench.autoPilot w

      match here with
      | Some h when SimState.inCombat w && (fought = 0 || int w.Tick % every = 0) && rows < 10 ->
        let plan = SimState.ManaBudget.plan w h

        if not header then
          printfn "seed %d" s

          printfn
            "  %6s %4s %7s %6s %6s %8s %8s %6s %8s  %-14s %s"
            "tick"
            "pack"
            "hp"
            "dps"
            "ttk"
            "drain/t"
            "reserve"
            "pool"
            "budget"
            "order"
            "gate"

          header <- true

        // The cheapest damaging spell this healer could press: the least the gate has
        // to allow before any damage can flow at all.
        let cheapest =
          h.Abilities
          |> List.filter (fun a -> Ability.isDamaging a && a.ResourceCost > 0)
          |> List.sortBy (fun a -> a.ResourceCost)
          |> List.tryHead

        // The verdict and the reason for it, read off the plan rather than re-derived:
        // `Budget` is the very number `maySpendOnDamage` compares.
        let verdict, why =
          match cheapest with
          | None -> "—", "no damaging spell in the kit"
          | Some _ when SimState.inStance h TreeForm -> "refuse", "form-locked (Tree of Life)"
          | Some a when plan.Budget >= a.ResourceCost ->
            "allow", sprintf "budget %d >= %s %d" plan.Budget a.Name a.ResourceCost
          | Some a -> "refuse", sprintf "budget %d < %s %d" plan.Budget a.Name a.ResourceCost

        // What it actually asked for. The filter only ever removes an order, never
        // invents one, so a `-` beside an `allow` means something else was wrong.
        let ordered =
          cmds
          |> List.tryPick (fun c ->
            match c.Kind with
            | UseAbility(aid, name, _) when aid = h.Id -> Some name
            | _ -> None)
          |> Option.defaultValue "-"

        printfn
          "  %6d %4d %7d %6.1f %6.0f %+8.2f %8d %6d %8d  %-14s %s (%s)"
          (int w.Tick)
          plan.EngagementEpoch
          plan.HostileHp
          plan.Dps
          plan.SecondsToKill
          plan.BurnPerTick
          plan.Reserve
          plan.Pool
          plan.Budget
          ordered
          verdict
          why

        rows <- rows + 1
      | _ -> ()

      w <- SimTick.step cmds w

      if SimState.inCombat w then
        fought <- fought + 1

      guard <- guard + 1

    if header then
      printfn "  pull ended at tick %d (%d Ticks in combat)" (int w.Tick) fought
      printfn ""

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
  | "attrib" :: rest ->
    // Which spell produced the number. A class ladder says how much a Paladin
    // healed; this is the same run split by the ability the combat log already
    // names, beside the per-caster total that ladder reports. Direct hits and
    // heals only — an aura's periodic tick emits no line, so nothing here counts
    // it and these totals are not a full throughput figure.
    let wing = rest |> List.tryItem 0 |> Option.defaultValue "graveyard"

    let difficulty =
      match rest |> List.tryItem 1 with
      | Some "min" -> Content.Minimum
      | Some "max" -> Content.Maximum
      | _ -> Content.Median

    let limit = rest |> List.tryItem 2 |> Option.map int |> Option.defaultValue 12000

    // `--class C` seats C in the fifth slot instead of the default roster, so a class's
    // damage can be split by the spelling that produced it. Without it the default
    // roster runs, which has no Shaman seat and so cannot answer that question.
    let mutable w =
      match textOf rest "--class" with
      | Some cls ->
        let roster = [ "Warrior"; "Priest"; "Mage"; "Rogue"; cls ]
        Content.withRosterAt (Content.levelOf difficulty (Content.wingOf wing)) roster (Content.scarletMonasteryAt wing difficulty 1UL)
      | None -> Content.scarletMonasteryAt wing difficulty 1UL

    let mutable tick = 0

    while tick < limit && not (SimState.aliveHostiles w |> List.isEmpty) do
      w <- SimTick.step (Bench.autoPilot w) w
      tick <- tick + 1

    let totals = Attribution.byCaster w

    let outcome =
      if SimState.aliveHostiles w |> List.isEmpty then
        "cleared"
      elif SimState.aliveParty w |> List.isEmpty then
        "wiped"
      else
        "stopped at tick limit (fight unresolved)"

    printfn
      "%s %A level=%d ticks=%d/%d outcome=%s  direct damage and healing only"
      wing
      difficulty
      (Content.levelOf difficulty (Content.wingOf wing))
      tick
      limit
      outcome

    Attribution.byAbility w
    |> Map.toList
    |> List.map (fun ((caster, ability), t) -> caster, ability, t)
    |> List.groupBy (fun (caster, _, _) -> caster)
    |> List.sortByDescending (fun (_, cells) ->
      cells |> List.sumBy (fun (_, _, t) -> t.Damage + t.Healing))
    |> List.iter (fun (caster, cells) ->
      for _, ability, t in
        cells |> List.sortByDescending (fun (_, _, t) -> t.Damage + t.Healing) do
        printfn "  %-20s %-18s dmg=%6d heal=%6d" caster ability t.Damage t.Healing

      let t = totals |> Map.tryFind caster |> Option.defaultValue Attribution.empty

      printfn "  %-20s %-18s dmg=%6d heal=%6d" caster "-- total" t.Damage t.Healing)

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
  | "comps" :: rest ->
    comps (intOf rest 0 25) (flagOf rest "--level")
    0
  | "activity" :: rest ->
    activity (intOf rest 0 3) (flagOf rest "--level") (rosterOf rest)
    0
  | "sheet" :: rest ->
    sheet (flagOf rest "--level")
    0
  | "druidspend" :: rest ->
    druidSpend (intOf rest 0 3) (rosterOf rest)
    0
  | "healers" :: rest ->
    healers (intOf rest 0 9) (flagOf rest "--level")
    0
  | "ttk" :: rest ->
    ttk (intOf rest 0 9) (flagOf rest "--level")
    0
  | "drmana" :: rest ->
    drmana (intOf rest 0 9) (flagOf rest "--level")
    0
  | "interrupts" :: rest ->
    interrupts (intOf rest 0 9) (textOf rest "--class")
    0
  | "shmana" :: rest ->
    shmana (intOf rest 0 9) (textOf rest "--class") (flagOf rest "--level")
    0
  | "totems" :: rest ->
    totems (intOf rest 0 12)
    0
  | "seat" :: rest ->
    seat
      (intOf rest 0 12)
      (rest |> List.tryItem 1 |> Option.defaultValue "library")
      (textOf rest "--class")
      (List.contains "--melee" rest)
    0
  | "droods" :: rest ->
    droods (intOf rest 0 12)
    0
  | "runs" :: rest ->
    runs (intOf rest 0 20) (intOf rest 1 2000)
    0
  | "autopsy" :: rest ->
    autopsy
      (intOf rest 0 12)
      (rest |> List.tryItem 1 |> Option.defaultValue "graveyard")
      (textOf rest "--class")
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
