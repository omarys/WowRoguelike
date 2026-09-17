namespace WowRoguelike.Core

/// The simulation. Pure: `step` is a function of (previous World, scheduled
/// Commands) and nothing else. Every random draw comes from a World-carried
/// stream, so the same seed and command log always produce the same World.
///
/// This is the *naive* baseline implementation (Q8/Q25): records are rebuilt
/// rather than mutated. It is the correctness oracle the fast path will be
/// measured and equivalence-tested against.
module Sim =

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
  let party (w: World) = w.Entities |> List.filter (fun e -> e.Faction = Party)
  let hostiles (w: World) = w.Entities |> List.filter (fun e -> e.Faction = Hostile)
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

  let occupiedByOther (w: World) (self: EntityId) (p: Pos) =
    w.Entities |> List.exists (fun e -> alive e && e.Id <> self && e.Pos = p)

  /// A tile is taken if someone stands on it *or* is already stepping into it,
  /// otherwise two entities claim the same tile in the same tick.
  let claimedByOther (w: World) (self: EntityId) (p: Pos) =
    w.Entities
    |> List.exists (fun e ->
      alive e && e.Id <> self && (e.Pos = p || e.Destination = Some p))

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

  // -- threat ---------------------------------------------------------------

  let threatOf (id: EntityId) (m: Entity) =
    m.Threat |> Map.tryFind id |> Option.defaultValue 0

  let private aliveMember (w: World) (id: EntityId) =
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

  let private nameOf (id: EntityId) (w: World) =
    tryEntity id w |> Option.map (fun e -> e.Name) |> Option.defaultValue "?"

  /// Resolve one ability's effects against one target.
  let resolveAbility (actorId: EntityId) (targetId: EntityId) (ability: Ability) (w0: World) : World =
    match tryEntity actorId w0 with
    | None -> w0
    | Some actor ->
      let targetName = nameOf targetId w0

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

  let private allyInNeed (actor: Entity) (w: World) =
    let pool = if actor.Faction = Party then aliveParty w else aliveHostiles w

    pool
    |> List.filter (fun e -> e.Health < e.MaxHealth)
    |> List.sortBy healthPct
    |> List.tryHead
    |> Option.map (fun e -> e.Id)

  let private usableTarget (actor: Entity) (ability: Ability) (w: World) =
    match ability.TargetKind with
    | Self -> if ability.Effects |> List.isEmpty then None else Some actor.Id
    | Enemy ->
      match currentTarget actor w with
      | Some t when withinRange ability.Range actor t -> Some t.Id
      | _ -> None
    | Ally -> allyInNeed actor w

  let private startOrResolve (actor: Entity) (targetId: EntityId) (ability: Ability) (w: World) : World =
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
              Destination = None
              Casting = None })
        w

    | MoveTo(actorId, dest) ->
      match tryEntity actorId w with
      | Some e when canAct e -> mapEntity actorId (fun x -> { x with Goal = Some dest }) w
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

          if onCooldown then
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
                    | Enemy -> t.Faction <> actor.Faction
                    | Ally -> t.Faction = actor.Faction)
                && (ability.TargetKind = Self || withinRange ability.Range actor t)

            if not targetOk then
              w
              |> logLine (sprintf "%s: %s invalid or out of range" actor.Name ability.Name)
            else
              startOrResolve actor targetId ability w

  // =========================================================================
  // Per-tick phases
  // =========================================================================

  /// Auras tick down; anything at zero falls off.
  let advanceAuras (w: World) =
    mapAll
      (fun e ->
        { e with
            Auras =
              e.Auras
              |> List.choose (function
                | Sleeping r when r > ticks 1 -> Some(Sleeping(r - ticks 1))
                | SerpentForm(r, b) when r > ticks 1 -> Some(SerpentForm(r - ticks 1, b))
                | _ -> None) })
      w

  let advanceCooldowns (w: World) =
    mapAll
      (fun e ->
        { e with
            Cooldowns =
              e.Cooldowns
              |> Map.map (fun _ r -> r - ticks 1)
              |> Map.filter (fun _ r -> r > ticks 0) })
      w

  /// One tile of progress. A moving entity loses its cast — and a sleeping one
  /// neither moves nor keeps a goal.
  let advanceMovement (w: World) =
    mapAll
      (fun e ->
        if not (alive e) then
          e
        elif isSleeping e then
          { e with
              Goal = None
              Destination = None
              MoveTicksLeft = ticks 0 }
        else
          match e.Destination with
          | None -> e
          | Some dest ->
            let e = { e with Casting = None }

            if e.MoveTicksLeft > ticks 1 then
              { e with MoveTicksLeft = e.MoveTicksLeft - ticks 1 }
            else
              { e with
                  Pos = dest
                  Destination = None
                  MoveTicksLeft = ticks 0
                  MoveTicksTotal = ticks 0 })
      w

  /// Begin stepping toward a goal, one tile at a time. Greedy: no pathfinding,
  /// which is slice 2's A*.
  let stepToward (e: Entity) (dest: Pos) (w: World) : Entity =
    if e.Pos = dest then
      { e with
          Goal = None
          Destination = None }
    elif e.Destination.IsSome then
      e
    else
      let dx = sign (dest.X - e.Pos.X)
      let dy = sign (dest.Y - e.Pos.Y)

      let candidates =
        [ { X = e.Pos.X + dx; Y = e.Pos.Y + dy }
          { X = e.Pos.X + dx; Y = e.Pos.Y }
          { X = e.Pos.X; Y = e.Pos.Y + dy } ]

      match
        candidates
        |> List.tryFind (fun p -> Grid.isFloor w.Grid p && not (claimedByOther w e.Id p))
      with
      | Some next ->
        { e with
            Destination = Some next
            MoveTicksLeft = e.MoveTicksPerTile
            MoveTicksTotal = e.MoveTicksPerTile }
      | None -> e

  /// Stepped as a fold rather than a map, so two entities cannot claim the same
  /// tile in one tick.
  let advanceGoals (w: World) =
    w.Entities
    |> List.fold
      (fun (acc: World) e ->
        if not (canAct e) then
          acc
        else
          match e.Goal with
          | None -> acc
          | Some g ->
            let cur = acc.Entities |> List.find (fun x -> x.Id = e.Id)
            mapEntity e.Id (fun _ -> stepToward cur g acc) acc)
      w

  /// Casts tick down, then the ones that finished resolve against the world as
  /// it stands after every cast has advanced.
  let advanceCasts (w: World) =
    let completing =
      w.Entities
      |> List.choose (fun e ->
        match e.Casting with
        | Some c when alive e && c.Remaining <= ticks 1 -> Some(e.Id, c)
        | _ -> None)

    let w =
      mapAll
        (fun e ->
          match e.Casting with
          | Some c when c.Remaining > ticks 0 ->
            { e with Casting = Some { c with Remaining = c.Remaining - ticks 1 } }
          | _ -> e)
        w

    (w, completing)
    ||> List.fold (fun w (actorId, c) ->
      let w = mapEntity actorId (fun e -> { e with Casting = None }) w

      match tryEntity c.Target w with
      | Some t when alive t -> resolveAbility actorId c.Target c.Ability w
      | _ ->
        w
        |> logLine (sprintf "%s's %s fizzles (target gone)" (nameOf actorId w) c.Ability.Name))

  /// Everyone with a swing timer swings. Party auto-attack only engages Mobs
  /// that are already in the fight, so nobody pulls by accident.
  let advanceSwings (w: World) =
    let attackers = w.Entities |> List.filter (fun e -> canAct e && e.AutoAttack.IsSome)

    (w, attackers)
    ||> List.fold (fun w e ->
      let attack = e.AutoAttack.Value

      let target =
        if e.Faction = Party then
          aliveHostiles w
          |> List.filter (fun t -> t.Engaged && withinRange attack.Range e t)
          |> List.sortBy (fun t -> Pos.chebyshev e.Pos t.Pos)
          |> List.tryHead
        else
          currentTarget e w |> Option.filter (fun t -> withinRange attack.Range e t)

      // Extra physical damage from Serpent Form lands on every swing.
      let bonus = serpentBonus e

      if e.SwingTicksLeft > ticks 1 then
        mapEntity e.Id (fun x -> { x with SwingTicksLeft = x.SwingTicksLeft - ticks 1 }) w
      else
        let w = mapEntity e.Id (fun x -> { x with SwingTicksLeft = attack.Ticks }) w

        match target with
        | None -> w
        | Some t ->
          let amount, w = rollRange attack.Min attack.Max w
          let total = amount + bonus

          w
          |> dealDamage t.Id total
          |> fun w -> if e.Faction = Party then addThreat t.Id e total w else w
          |> logLine (sprintf "%s swings at %s for %d" e.Name t.Name total))

  /// Recompute every Hostile's aggression target, honoring the margins and
  /// clearing a Taunt whose taunter has died.
  let retarget (w: World) =
    mapAll
      (fun e ->
        if e.Faction <> Hostile || not (alive e) then
          e
        elif not e.Engaged then
          { e with
              Target = None
              ForcedTarget = None }
        else
          let forced =
            e.ForcedTarget |> Option.filter (fun id -> aliveMember w id |> Option.isSome)

          let withForced = { e with ForcedTarget = forced }
          { withForced with Target = resolveTarget withForced w })
      w

  /// Mobs decide. Ability priority is expressed by effect shape, so the caster
  /// rotation reads heal → slumber → bolt → serpent form.
  let mobDecisions (w: World) =
    let acting = w.Entities |> List.filter (fun e -> e.Faction = Hostile && canAct e)

    (w, acting)
    ||> List.fold (fun w m ->
      // Proximity aggro: a Mob notices the Party before any threat exists.
      let noticed =
        aliveParty w |> List.exists (fun p -> Pos.chebyshev m.Pos p.Pos <= aggroRadius)

      let w, m =
        if not m.Engaged && noticed then
          let w =
            w
            |> mapEntity m.Id (fun x -> { x with Engaged = true })
            |> logLine (sprintf "%s notices the party" m.Name)

          let m' = tryEntity m.Id w |> Option.defaultValue m
          // Give it a target immediately, so it is not idle for a tick.
          mapEntity m.Id (fun x -> { x with Target = resolveTarget x w }) w,
          { m' with Target = resolveTarget m' w }
        else
          w, m

      if not m.Engaged then
        w
      else
        match currentTarget m w with
        | None -> w
        | Some target ->
          let byEffect (pred: Effect -> bool) =
            m.Abilities |> List.tryFind (fun a -> List.exists pred a.Effects)

          let ready (a: Ability) =
            (m.Cooldowns |> Map.tryFind a.Name |> Option.defaultValue (ticks 0)) <= ticks 0

          let healAbility = byEffect (function Heal _ -> true | _ -> false)
          let sleepAbility = byEffect (function Sleep _ -> true | _ -> false)
          let damageAbility = byEffect (function Damage _ -> true | _ -> false)

          let shiftAbility =
            byEffect (function Shapeshift(Serpent, _, _) -> true | _ -> false)

          // Only one Party member may be slept at a time.
          let anyoneAsleep = aliveParty w |> List.exists isSleeping

          let choice =
            [ healAbility
              |> Option.filter (fun a -> ready a && canCast m)
              |> Option.bind (fun a -> usableTarget m a w |> Option.map (fun t -> a, t))

              sleepAbility
              |> Option.filter (fun a -> ready a && canCast m && not anyoneAsleep)
              |> Option.bind (fun a -> usableTarget m a w |> Option.map (fun t -> a, t))

              damageAbility
              |> Option.filter (fun a ->
                ready a && canCast m && withinRange a.Range m target)
              |> Option.map (fun a -> a, target.Id)

              // Shift only once the target has closed to melee.
              shiftAbility
              |> Option.filter (fun a -> ready a && not (isShifted m) && inMelee m target)
              |> Option.map (fun a -> a, m.Id) ]
            |> List.tryPick id

          match choice with
          | Some(ability, targetId) -> startOrResolve m targetId ability w
          | None ->
            if not (inMelee m target) then
              mapEntity m.Id (fun e -> { e with Goal = Some target.Pos }) w
            else
              w)

  /// A Mob that drops too low calls its neighbours in.
  let callForHelp (w: World) =
    let callers =
      w.Entities
      |> List.filter (fun e ->
        e.Faction = Hostile
        && alive e
        && e.Engaged
        && not e.HasCalledForHelp
        && e.CallForHelpBelowHealthPct |> Option.exists (fun p -> healthPct e <= p))

    (w, callers)
    ||> List.fold (fun w c ->
      let neighbours =
        w.Entities
        |> List.filter (fun e ->
          e.Faction = Hostile
          && alive e
          && not e.Engaged
          && Pos.chebyshev c.Pos e.Pos <= callForHelpRadius)

      let w =
        (w, neighbours)
        ||> List.fold (fun w n ->
          w
          |> mapEntity n.Id (fun e -> { e with Engaged = true })
          |> logLine (sprintf "%s calls for help, %s answers" c.Name n.Name))

      mapEntity c.Id (fun e -> { e with HasCalledForHelp = true }) w)

  // =========================================================================
  // The tick
  // =========================================================================

  /// One Tick. Phases are ordered so player intent lands before the world reacts
  /// to it, and so everything acting in a tick sees the same post-damage world.
  let step (incoming: Command list) (w0: World) : World =
    let w =
      { w0 with
          Tick = w0.Tick + ticks 1
          Pending = w0.Pending @ incoming }

    let due, later = w.Pending |> List.partition (fun c -> c.ApplyAt <= w.Tick)
    let w = { w with Pending = later }

    w
    |> fun w -> (w, due) ||> List.fold (fun w c -> applyCommand c w)
    |> advanceCooldowns
    |> advanceAuras
    |> advanceMovement
    |> advanceCasts
    |> advanceSwings
    |> callForHelp
    |> retarget
    |> mobDecisions
    |> advanceGoals

  /// Run `n` Ticks with no new input.
  let run (n: int) (w: World) : World = (w, [ 1..n ]) ||> List.fold (fun w _ -> step [] w)
