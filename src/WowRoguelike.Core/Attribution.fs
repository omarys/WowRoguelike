namespace WowRoguelike.Core

/// Per-ability attribution, read off the combat log.
///
/// The log is the only event record the sim keeps, so the ability a line names is
/// the whole attribution: `<caster>'s <ability> hits <target> for N` for a direct
/// hit and `. heals .` for a heal, whether it landed as a cast or as a tick of a
/// heal-over-time (`SimTick.bookHotTick` writes the tick in exactly this shape).
/// What the log cannot say is left out rather than guessed at — damage-over-time
/// ticks emit no line at all, and a swing names no ability, so it is booked as
/// `Auto Attack`. Every total here is therefore direct damage and healing
/// including the heal-over-time ticks, which is what `sim healers` reports.
module Attribution =

  open System

  /// What one line says happened.
  type Effect =
    | Damage of amount: int
    | Healing of amount: int

  /// One attributable line: who, which ability, how much.
  type Entry =
    { Caster: string
      Ability: string
      Effect: Effect }

  /// A running total, so one caster's abilities can be added up into the
  /// per-entity figure a class ladder reports.
  type Totals = { Damage: int; Healing: int }

  let empty: Totals = { Damage = 0; Healing = 0 }

  /// The amount the line ends with, beside the text in front of `marker`, which
  /// is what names the kind of line it is.
  let private beforeMarker (marker: string) (line: string) =
    match line.IndexOf(marker, System.StringComparison.Ordinal) with
    | -1 -> None
    | at ->
      let lastSpace = line.LastIndexOf ' '

      match System.Int32.TryParse(line.AsSpan(lastSpace + 1)) with
      | true, amount -> Some(line.Substring(0, at), amount)
      | _ -> None

  /// `<caster>'s <ability>` split at the possessive. Neither a caster nor an
  /// ability name contains one, so the last occurrence is the separator.
  let private casterAndAbility (prefix: string) =
    match prefix.LastIndexOf("'s ", System.StringComparison.Ordinal) with
    | -1 -> None
    | at -> Some(prefix.Substring(0, at), prefix.Substring(at + 3))

  let private entryOf (line: string) : Entry option =
    match beforeMarker " swings at " line with
    | Some(caster, amount) -> Some { Caster = caster; Ability = "Auto Attack"; Effect = Damage amount }
    | None ->
      match beforeMarker " hits " line with
      | Some(prefix, n) ->
        casterAndAbility prefix
        |> Option.map (fun (caster, ability) ->
          { Caster = caster
            Ability = ability
            Effect = Damage n })
      | None ->
        match beforeMarker " heals " line with
        | Some(prefix, n) ->
          casterAndAbility prefix
          |> Option.map (fun (caster, ability) ->
            { Caster = caster
              Ability = ability
              Effect = Healing n })
        | None -> None

  let private add (e: Entry) (t: Totals) =
    match e.Effect with
    | Damage n -> { t with Damage = t.Damage + n }
    | Healing n -> { t with Healing = t.Healing + n }

  /// Every attributable line, oldest first — `World.Log` is newest first.
  let entries (w: World) : Entry list = w.Log |> List.rev |> List.choose entryOf

  /// Damage and healing per caster and ability: the table the balance rig prints.
  let byAbility (w: World) : Map<string * string, Totals> =
    (Map.empty, w.Log)
    ||> List.fold (fun byCasterAbility line ->
      match entryOf line with
      | None -> byCasterAbility
      | Some e ->
        let key = e.Caster, e.Ability
        let running = byCasterAbility |> Map.tryFind key |> Option.defaultValue empty
        byCasterAbility |> Map.add key (add e running))

  /// The same rolled up to the caster alone: the per-entity totals `byAbility`
  /// has to add up to.
  let byCaster (w: World) : Map<string, Totals> =
    (Map.empty, w.Log)
    ||> List.fold (fun totals line ->
      match entryOf line with
      | None -> totals
      | Some e ->
        let running = totals |> Map.tryFind e.Caster |> Option.defaultValue empty
        totals |> Map.add e.Caster (add e running))
