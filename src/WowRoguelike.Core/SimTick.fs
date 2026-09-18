namespace WowRoguelike.Core

open WowRoguelike.Core.SimState

/// The per-Tick pipeline, and movement. Phase order is the contract: player
/// intent lands before the world reacts to it, and everything acting in a Tick
/// sees the same post-damage world.
module SimTick =


  // =========================================================================
  // Per-tick phases
  // =========================================================================

  /// Auras age, and anything that runs out falls off. The ageing rule for each
  /// kind lives in `Aura.advance`, which is the one place that matches every case.
  let advanceAuras (w: World) =
    mapAll (fun e -> { e with Auras = e.Auras |> List.choose Aura.advance }) w

  let advanceCooldowns (w: World) =
    mapAll
      (fun e ->
        { e with
            Cooldowns =
              e.Cooldowns
              |> Map.map (fun _ r -> r - ticks 1)
              |> Map.filter (fun _ r -> r > ticks 0) })
      w

  /// Resource accumulates. Mob regeneration is zero, and that is the entire
  /// reason a fight can now end: a Druid of the Fang can afford four Healing
  /// Touches and then it is spent.
  let advanceResource (w: World) =
    mapAll
      (fun e ->
        if not (alive e) then
          e
        else
          { e with
              Resource = min e.MaxResource (e.Resource + e.ResourceRegenPerTick) })
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
              Path = []
              StallTicks = ticks 0
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

  /// Begin stepping toward a goal. A* plans the route once and the entity walks
  /// it, re-planning only when the route runs out or its next step is taken.
  /// Re-planning for every mover on every tick is the expensive way to do this.
  let stepToward (e: Entity) (dest: Pos) (w: World) : Entity =
    if e.Pos = dest then
      { e with
          Goal = None
          Destination = None
          Path = []
          StallTicks = ticks 0 }
    elif e.Destination.IsSome then
      e
    elif e.StallTicks > ticks 0 then
      // A previous attempt at this goal found no route. Waiting is what makes a
      // doomed search cost one search per backoff instead of one per tick.
      { e with StallTicks = e.StallTicks - ticks 1 }
    else
      // Walls are A*'s business, not the blocker's.
      let taken p = claimedByOther w e.Id p

      // A route whose next step is now occupied is stale: drop it and replan.
      let planned =
        match e.Path with
        | next :: _ when taken next -> []
        | kept -> kept

      let route =
        if List.isEmpty planned then
          (Path.astar w.Grid taken e.Pos dest).Path
        else
          planned

      match route with
      | [] ->
        { e with
            Path = []
            StallTicks = pathRetryBackoff }
      | next :: rest ->
        if taken next then
          { e with
              Path = []
              StallTicks = pathRetryBackoff }
        else
          { e with
              Path = rest
              Destination = Some next
              MoveTicksLeft = e.MoveTicksPerTile
              MoveTicksTotal = e.MoveTicksPerTile
              StallTicks = ticks 0 }

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
          |> List.filter (fun t -> t.Engaged && canReach attack.Range e t w)
          |> List.sortBy (fun t -> Pos.chebyshev e.Pos t.Pos)
          |> List.tryHead
        else
          currentTarget e w |> Option.filter (fun t -> canReach attack.Range e t w)

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
            && canAfford m a

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
                ready a && canCast m && canReach a.Range m target w)
              |> Option.map (fun a -> a, target.Id)

              // Shift only once the target has closed to melee.
              shiftAbility
              |> Option.filter (fun a -> ready a && not (isShifted m) && inMelee m target)
              |> Option.map (fun a -> a, m.Id) ]
            |> List.tryPick id

          match choice with
          | Some(ability, targetId) ->
            // Committed to an action: stand still and stop planning.
            startOrResolve m targetId ability w
            |> mapEntity m.Id (fun e ->
              { e with
                  Goal = None
                  Path = []
                  StallTicks = ticks 0 })
          | None ->
            if inMelee m target then
              // Already in reach; no reason to move.
              mapEntity
                m.Id
                (fun e ->
                  { e with
                      Goal = None
                      Path = []
                      StallTicks = ticks 0 })
                w
            else
              // Close on a tile beside the target, never on the target's own
              // tile, which it is standing on and which is therefore usually
              // unreachable. Keep the tile it is already heading for while that
              // stays valid, so a target shifting by one tile does not throw away
              // a planned route and replan from scratch.
              let keep =
                match m.Goal with
                | Some g when
                  Pos.chebyshev g target.Pos <= 1
                  && Grid.isFloor w.Grid g
                  && not (claimedByOther w m.Id g)
                  ->
                  Some g
                | _ -> None

              match keep |> Option.orElse (approachTile m target.Pos w) with
              | Some spot ->
                mapEntity
                  m.Id
                  (fun e ->
                    if e.Goal = Some spot then
                      e
                    else
                      { e with
                          Goal = Some spot
                          Path = []
                          StallTicks = ticks 0 })
                  w
              | None ->
                // Nowhere to stand beside it; hold rather than pay for a search
                // that cannot succeed.
                mapEntity
                  m.Id
                  (fun e ->
                    { e with
                        Goal = None
                        Path = []
                        StallTicks = ticks 0 })
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
    |> advanceResource
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
