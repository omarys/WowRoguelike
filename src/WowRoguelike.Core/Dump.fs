namespace WowRoguelike.Core

/// Text views of the World. `world` is the canonical, diffable form used as the
/// golden-replay fingerprint (Q25a); it deliberately excludes the Log, because
/// the Log is derivable from state and a wording change should not read as a
/// behaviour change.
module Dump =

  open System.Text

  let auraText =
    function
    | Sleeping r -> sprintf "sleep(%d)" r
    | SerpentForm(r, b) -> sprintf "serpent(%d,+%d)" r b

  let private pairs (m: Map<'k, 'v>) (fmt: 'k -> 'v -> string) =
    m |> Map.toList |> List.sortBy fst |> List.map (fun (k, v) -> fmt k v) |> String.concat ","

  let private entityIdText (EntityId i) = string i

  let entity (e: Entity) =
    let (EntityId id) = e.Id

    let point =
      function
      | Some p -> sprintf "%d,%d" p.X p.Y
      | None -> "-"

    let cast =
      e.Casting
      |> Option.map (fun c -> sprintf "%s@%d->%s" c.Ability.Name c.Remaining (entityIdText c.Target))
      |> Option.defaultValue "-"

    let optionId = e.Target |> Option.map entityIdText |> Option.defaultValue "-"
    let forcedId = e.ForcedTarget |> Option.map entityIdText |> Option.defaultValue "-"

    sprintf
      "%d %-18s %-7s %2d,%-2d hp=%d/%d dest=%s move=%d/%d goal=%s path=%d cast=%s auras=[%s] cd=[%s] threat=[%s] target=%s forced=%s engaged=%b called=%b"
      id
      e.Name
      (if e.Faction = Party then "party" else "hostile")
      e.Pos.X
      e.Pos.Y
      e.Health
      e.MaxHealth
      (point e.Destination)
      e.MoveTicksLeft
      e.MoveTicksTotal
      (point e.Goal)
      (List.length e.Path)
      cast
      (e.Auras |> List.map auraText |> String.concat ",")
      (pairs e.Cooldowns (fun k v -> sprintf "%s:%d" k v))
      (pairs e.Threat (fun k v -> sprintf "%s:%d" (entityIdText k) v))
      optionId
      forcedId
      e.Engaged
      e.HasCalledForHelp

  /// Canonical state. Two Worlds with the same text are the same World.
  let world (w: World) =
    let sb = StringBuilder()
    sb.AppendLine(sprintf "tick=%d outcome=%s" w.Tick (string (Sim.outcome w))) |> ignore
    sb.AppendLine(sprintf "rng gen=%d combat=%d" w.Rng.Generation.Value w.Rng.Combat.Value)
    |> ignore

    for e in w.Entities do
      sb.AppendLine(entity e) |> ignore

    sb.ToString()

  /// The permanent regression net: state text compared against a recorded run.
  let fingerprint (w: World) = world w

  /// Newest lines first, as stored.
  let log (w: World) = w.Log |> List.truncate 40 |> List.rev |> String.concat "\n"
