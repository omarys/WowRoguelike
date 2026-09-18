namespace WowRoguelike.Core

/// The World's queries, its rebuild helpers, and how a single ability or command
/// resolves. Everything here is a pure function of the World it is handed.
///
/// The per-Tick pipeline is in SimTick. This module is the naive baseline
/// (Q8/Q25) — records are rebuilt rather than mutated — and is the correctness
/// oracle the fast path will be measured and equivalence-tested against.
module SimState =


  /// Mobs notice the Party at this distance. Proximity aggro, as in WoW.
  let aggroRadius = 6

  /// Raptors pull their neighbours in rather than dying quietly.
  let callForHelpRadius = 10

  /// The threat margins. A unit takes aggro when it exceeds the current
  /// target's threat by 10% while within melee range of the mob, or 30% while
  /// outside it. These numbers ARE the Tank role.
  let meleeMarginBp = 1000
  let rangedMarginBp = 3000

  /// A successful interrupt locks the school out for a few seconds.
  /// ponytail: one flat lockout for every school; per-school lockouts if the
  /// distinction ever matters.
  let interruptLockout = secTicks 4.0

  // =========================================================================
  // Queries
  // =========================================================================

  let tryEntity (id: EntityId) (w: World) = w.Entities |> List.tryFind (fun e -> e.Id = id)

  let alive (e: Entity) = e.Health > 0
  let aliveParty (w: World) = w.Entities |> List.filter (fun e -> e.Faction = Party && alive e)

  let aliveHostiles (w: World) =
    w.Entities |> List.filter (fun e -> e.Faction = Hostile && alive e)

  let outcome (w: World) =
    if List.isEmpty (aliveHostiles w) then EncounterCleared
    elif List.isEmpty (aliveParty w) then PartyWiped
    else Running

  let healthPct (e: Entity) = e.Health * 100 / e.MaxHealth

  let inMelee (a: Entity) (b: Entity) = Pos.chebyshev a.Pos b.Pos <= 1

  let withinRange (range: int) (a: Entity) (b: Entity) =
    Pos.chebyshev a.Pos b.Pos <= range

  /// Distance alone is not reach: nothing at range can be hit through a wall.
  /// Melee is deliberately exempt, so something standing beside you is always
  /// hittable and a blocked caster cannot freeze next to its target.
  let canReach (range: int) (actor: Entity) (target: Entity) (w: World) =
    withinRange range actor target
    && (range <= 1 || Path.lineOfSight w.Grid actor.Pos target.Pos)

  /// A tile is taken if someone stands on it *or* is already stepping into it,
  /// otherwise two entities claim the same tile in the same tick.
  let claimedByOther (w: World) (self: EntityId) (p: Pos) =
    w.Entities
    |> List.exists (fun e ->
      alive e && e.Id <> self && (e.Pos = p || e.Destination = Some p))

  /// A free tile to stand on next to `spot`, preferring the one nearest the mover.
  ///
  /// This exists because aiming A* at a tile somebody is standing on is a goal
  /// that usually cannot be reached: the goal tile itself is allowed, but the
  /// corner rule blocks the diagonals around it, so once a Party clusters against
  /// a wall there is no route at all. A* then expands the whole reachable
  /// component before failing, and the failure repeats every tick (task 16).
  ///
  /// `Path.neighbours` applies the same corner rule, so a tile it returns is one
  /// the mover can genuinely step onto.
  let approachTile (mover: Entity) (spot: Pos) (w: World) : Pos option =
    Path.neighbours
      (fun p -> Grid.isFloor w.Grid p && not (claimedByOther w mover.Id p))
      spot
    |> List.sortBy (fun p -> Pos.chebyshev mover.Pos p)
    |> List.tryHead

  /// How long to wait before retrying a goal that produced no route.
  let pathRetryBackoff = secTicks 1.0

  // -- auras ----------------------------------------------------------------

  let isSleeping (e: Entity) =
    e.Auras |> List.exists (function Sleeping _ -> true | _ -> false)

  let isShifted (e: Entity) =
    e.Auras |> List.exists (function SerpentForm _ -> true | _ -> false)

  let serpentBonus (e: Entity) =
    e.Auras
    |> List.tryPick (function SerpentForm(_, bonus) -> Some bonus | _ -> None)
    |> Option.defaultValue 0

  /// Being shifted locks out casting, which is the whole cost of Serpent Form.
  let canCast (e: Entity) = not (isSleeping e) && not (isShifted e) && e.Casting.IsNone
  let canAct (e: Entity) = alive e && not (isSleeping e)
  let canAfford (e: Entity) (ability: Ability) = e.Resource >= ability.ResourceCost

  // -- threat ---------------------------------------------------------------

  let threatOf (id: EntityId) (m: Entity) =
    m.Threat |> Map.tryFind id |> Option.defaultValue 0

  let internal aliveMember (w: World) (id: EntityId) =
    aliveParty w |> List.tryFind (fun p -> p.Id = id)

  /// Recompute a Hostile's aggression target from its threat table. The current
  /// target is only displaced by the 10% / 30% margins; with an empty table the
  /// mob simply notices the nearest Party member.
  let resolveTarget (m: Entity) (w: World) : EntityId option =
    match m.ForcedTarget |> Option.bind (aliveMember w) with
    | Some p -> Some p.Id
    | None ->
      let tracked =
        aliveParty w
        |> List.map (fun p -> p, threatOf p.Id m)
        |> List.filter (fun (_, t) -> t > 0)

      match tracked with
      | [] ->
        aliveParty w
        |> List.sortBy (fun p -> Pos.chebyshev m.Pos p.Pos)
        |> List.tryHead
        |> Option.map (fun p -> p.Id)
      | _ ->
        let top, topThreat = tracked |> List.maxBy snd

        match m.Target |> Option.bind (aliveMember w) with
        | None -> Some top.Id
        | Some cur ->
          let curThreat = threatOf cur.Id m
          // The margin depends on whether the *challenger* is in melee range.
          let margin = if inMelee m top then meleeMarginBp else rangedMarginBp
          // Compare by cross-multiplication, so this stays integer.
          if topThreat * 10000 > curThreat * (10000 + margin) then
            Some top.Id
          else
            Some cur.Id

  let currentTarget (m: Entity) (w: World) : Entity option =
    match m.ForcedTarget |> Option.bind (aliveMember w) with
    | Some p -> Some p
    | None -> m.Target |> Option.bind (aliveMember w)

  // =========================================================================
  // Record rebuilds (the naive path)
  // =========================================================================

  let logLine (line: string) (w: World) = { w with Log = line :: w.Log }

  let mapEntity (id: EntityId) (f: Entity -> Entity) (w: World) =
    { w with
        Entities = w.Entities |> List.map (fun e -> if e.Id = id then f e else e) }

  let mapAll (f: Entity -> Entity) (w: World) = { w with Entities = w.Entities |> List.map f }

  /// Add threat to one Mob, scaled by the *generating* entity's multiplier.
  /// Only the Party generates threat; a Mob healing or hitting generates none.
  let addThreat (mobId: EntityId) (source: Entity) (amount: int) (w: World) =
    let gained = amount * source.ThreatMultiplierBp / 10000

    mapEntity
      mobId
      (fun m ->
        { m with
            Threat = m.Threat |> Map.add source.Id (threatOf source.Id m + gained)
            Engaged = true })
      w

  /// Effective healing generates half its value in threat, divided among the
  /// Mobs that observed it. Overheal is not counted.
  /// Rounding is floor, and this is the Core's only rounding rule (Q24a).
  let addHealThreat (healer: Entity) (effective: int) (w: World) =
    let observers = aliveHostiles w |> List.filter (fun m -> m.Engaged)

    match observers with
    | [] -> w
    | _ ->
      let share = effective * 5000 / 10000 / List.length observers
      observers |> List.fold (fun acc m -> addThreat m.Id healer share acc) w

  /// Any hostile action wakes a sleeper, so damage is what removes Sleeping.
  /// A corpse stops moving and stops casting, so it interpolates nowhere.
  let dealDamage (targetId: EntityId) (amount: int) (w: World) =
    mapEntity
      targetId
      (fun t ->
        let hp = max 0 (t.Health - amount)

        { t with
            Health = hp
            Auras = t.Auras |> List.filter (function Sleeping _ -> false | _ -> true)
            Casting = if hp = 0 then None else t.Casting
            Goal = if hp = 0 then None else t.Goal
            Path = if hp = 0 then [] else t.Path
            StallTicks = if hp = 0 then ticks 0 else t.StallTicks
            Destination = if hp = 0 then None else t.Destination
            MoveTicksLeft = if hp = 0 then ticks 0 else t.MoveTicksLeft
            MoveTicksTotal = if hp = 0 then ticks 0 else t.MoveTicksTotal })
      w

  let healEntity (targetId: EntityId) (amount: int) (w: World) =
    mapEntity targetId (fun t -> { t with Health = min t.MaxHealth (t.Health + amount) }) w

  // =========================================================================
  // Effects
  // =========================================================================

  let rollRange (lo: int) (hi: int) (w: World) =
    let v, rng = Rng.drawCombat lo hi w.Rng
    v, { w with Rng = rng }

  let internal nameOf (id: EntityId) (w: World) =
    tryEntity id w |> Option.map (fun e -> e.Name) |> Option.defaultValue "?"

  /// Resolve one ability's effects against one target. The resource is spent
  /// here, on resolution, so an interrupted cast costs nothing.
  let resolveAbility (actorId: EntityId) (targetId: EntityId) (ability: Ability) (w0: World) : World =
    match tryEntity actorId w0 with
    | None -> w0
    | Some actor ->
      let targetName = nameOf targetId w0

      let w0 =
        mapEntity
          actorId
          (fun e -> { e with Resource = max 0 (e.Resource - ability.ResourceCost) })
          w0

      (w0, ability.Effects)
      ||> List.fold (fun w effect ->
        match effect with
        | Damage(lo, hi) ->
          let amount, w = rollRange lo hi w
          let w = dealDamage targetId amount w
          // Only the Party builds threat on Mobs.
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w
          w |> logLine (sprintf "%s hits %s for %d" actor.Name targetName amount)

        | Heal(lo, hi) ->
          let amount, w = rollRange lo hi w

          let before =
            tryEntity targetId w |> Option.map (fun t -> t.Health) |> Option.defaultValue 0

          let w = healEntity targetId amount w

          let after =
            tryEntity targetId w |> Option.map (fun t -> t.Health) |> Option.defaultValue 0

          let effective = after - before

          let w =
            if actor.Faction = Party then addHealThreat actor effective w else w

          w |> logLine (sprintf "%s heals %s for %d" actor.Name targetName effective)

        | Interrupt ->
          match tryEntity targetId w with
          | Some t when t.Casting |> Option.exists (fun c -> c.Ability.Interruptible) ->
            let cancelled = t.Casting.Value.Ability.Name

            w
            |> mapEntity targetId (fun e ->
              { e with
                  Casting = None
                  Cooldowns = e.Cooldowns |> Map.add cancelled interruptLockout })
            |> logLine (sprintf "%s interrupts %s's %s" actor.Name t.Name cancelled)
          | Some t ->
            w |> logLine (sprintf "%s fails to interrupt %s" actor.Name t.Name)
          | None -> w

        | Sleep duration ->
          match tryEntity targetId w with
          | Some t when not (isSleeping t) ->
            w
            |> mapEntity targetId (fun e ->
              { e with
                  Auras = Sleeping duration :: e.Auras
                  Casting = None
                  Goal = None
                  Path = []
                  StallTicks = ticks 0
                  Destination = None })
            |> logLine (sprintf "%s puts %s to sleep" actor.Name t.Name)
          | _ -> w

        | Shapeshift(Serpent, duration, bonus) ->
          w
          |> mapEntity actorId (fun e ->
            { e with
                Auras =
                  SerpentForm(duration, bonus)
                  :: (e.Auras |> List.filter (function SerpentForm _ -> false | _ -> true)) })
          |> logLine (sprintf "%s shifts into serpent form" actor.Name)

        | Shapeshift(Humanoid, _, _) -> w

        | Taunt ->
          match tryEntity targetId w with
          | Some m when m.Faction = Hostile && actor.Faction = Party ->
            let highest = m.Threat |> Map.toList |> List.map snd |> List.fold max 0

            w
            |> mapEntity targetId (fun mob ->
              { mob with
                  ForcedTarget = Some actorId
                  Engaged = true
                  Threat = mob.Threat |> Map.add actorId (max highest (threatOf actorId mob)) })
            |> logLine (sprintf "%s taunts %s" actor.Name m.Name)
          | _ -> w)

  // =========================================================================
  // Commands
  // =========================================================================

  let internal allyInNeed (actor: Entity) (ability: Ability) (w: World) =
    let pool = if actor.Faction = Party then aliveParty w else aliveHostiles w

    pool
    |> List.filter (fun e -> e.Health < e.MaxHealth && canReach ability.Range actor e w)
    |> List.sortBy healthPct
    |> List.tryHead
    |> Option.map (fun e -> e.Id)

  let internal usableTarget (actor: Entity) (ability: Ability) (w: World) =
    match ability.TargetKind with
    | Self -> if ability.Effects |> List.isEmpty then None else Some actor.Id
    | Foe ->
      match currentTarget actor w with
      | Some t when canReach ability.Range actor t w -> Some t.Id
      | _ -> None
    | Ally -> allyInNeed actor ability w

  let internal startOrResolve (actor: Entity) (targetId: EntityId) (ability: Ability) (w: World) : World =
    let w =
      mapEntity
        actor.Id
        (fun e -> { e with Cooldowns = e.Cooldowns |> Map.add ability.Name ability.CooldownTicks })
        w

    if ability.CastTicks <= ticks 0 then
      resolveAbility actor.Id targetId ability w
    else
      mapEntity
        actor.Id
        (fun e ->
          { e with
              Casting =
                Some
                  { Ability = ability
                    Target = targetId
                    Remaining = ability.CastTicks } })
        w

  let applyCommand (cmd: Command) (w: World) : World =
    match cmd.Kind with
    | Stop actorId ->
      mapEntity
        actorId
        (fun e ->
          { e with
              Goal = None
              Path = []
              Destination = None
              Casting = None
              StallTicks = ticks 0 })
        w

    | MoveTo(actorId, dest) ->
      match tryEntity actorId w with
      | Some e when canAct e ->
        // You cannot stand where somebody already is. Line the mover up beside
        // the tile instead of handing A* a goal it can never reach.
        let goal =
          if
            w.Entities
            |> List.exists (fun o -> alive o && o.Id <> actorId && o.Pos = dest)
          then
            approachTile e dest w |> Option.defaultValue dest
          else
            dest

        // Re-issuing the same goal must not throw away a planned route, or a
        // caller that repeats orders every tick would replan every tick.
        mapEntity
          actorId
          (fun x ->
            if x.Goal = Some goal then
              x
            else
              { x with
                  Goal = Some goal
                  Path = []
                  StallTicks = ticks 0 })
          w
      | _ -> w

    | UseAbility(actorId, abilityName, targetId) ->
      match tryEntity actorId w with
      | None -> w
      | Some actor when not (canAct actor) -> w
      | Some actor ->
        match actor.Abilities |> List.tryFind (fun a -> a.Name = abilityName) with
        | None -> w |> logLine (sprintf "%s has no ability %s" actor.Name abilityName)
        | Some ability ->
          let onCooldown =
            actor.Cooldowns |> Map.tryFind ability.Name |> Option.defaultValue (ticks 0) > ticks 0

          if not (canAfford actor ability) then
            w |> logLine (sprintf "%s: not enough resource for %s" actor.Name ability.Name)
          elif onCooldown then
            w |> logLine (sprintf "%s: %s on cooldown" actor.Name ability.Name)
          elif ability.CastTicks > ticks 0 && not (canCast actor) then
            // Already casting is not a failure worth logging: it is the caller's
            // ordering policy, and a one-deep queue is a scheduling concern.
            if isSleeping actor then
              w |> logLine (sprintf "%s is asleep" actor.Name)
            elif isShifted actor then
              w |> logLine (sprintf "%s cannot cast while shifted" actor.Name)
            else
              w
          else
            let targetOk =
              match tryEntity targetId w with
              | None -> false
              | Some t ->
                alive t
                && (match ability.TargetKind with
                    | Self -> t.Id = actor.Id
                    | Foe -> t.Faction <> actor.Faction
                    | Ally -> t.Faction = actor.Faction)
                && (ability.TargetKind = Self || canReach ability.Range actor t w)

            if not targetOk then
              w
              |> logLine (sprintf "%s: %s invalid or out of range" actor.Name ability.Name)
            else
              startOrResolve actor targetId ability w
