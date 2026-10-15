namespace WowRoguelike.Core

open WowRoguelike.Core.SimState

/// Dice rolls, the physical mitigation pipeline, ability resolution, and the
/// command handlers that drive them. Every function here is a pure transform
/// of the World (or World * World) drawing from the Combat RNG stream.
///
/// World reads and rebuilds go through SimState, the layer below; the per-Tick
/// pipeline above is SimTick.
module SimCombat =


  // =========================================================================
  // Damage maths
  // =========================================================================

  let rollRange (lo: int) (hi: int) (w: World) =
    let v, rng = Rng.drawCombat lo hi w.Rng
    v, { w with Rng = rng }

  /// Roll for a critical hit against `critChanceBp`. A crit deals 150% damage.
  let rollCrit (critChanceBp: int) (amount: int) (w: World) : int * World =
    let roll, w = rollRange 0 9999 w
    if roll < critChanceBp then amount * 150 / 100, w else amount, w

  /// Base chance (basis points) that damage interrupts a cast in progress (Q5a).
  let pushbackBaseBp = 1000

  /// Armour: each point is 10 basis points of physical reduction, capped at 75%
  /// (Q1a).
  let private armorMitigationBp (armor: int) = min 7500 (armor * 10)

  /// Dodge (full avoid), block (flat value), then armour (linear, capped). This
  /// is the physical-only pipeline (Q2a/Q4a); spells skip it entirely.
  let mitigatePhysical (targetId: EntityId) (amount: int) (w: World) : int * World =
    match tryEntity targetId w with
    | None -> amount, w
    | Some t ->
      if not (alive t) || amount <= 0 then
        amount, w
      else
        let dodgeRoll, w = rollRange 0 9999 w

        if dodgeRoll < t.DodgeChanceBp then
          (0, w |> logLine (sprintf "%s dodges" t.Name))
        else
          let blockRoll, w = rollRange 0 9999 w
          let blocked = if blockRoll < t.BlockChanceBp then t.BlockValue else 0
          let effectiveArmor = max 0 (t.Armor - (t.Auras |> List.sumBy Aura.armorReduction))
          let afterBlock = max 0 (amount - blocked)
          afterBlock * (10000 - armorMitigationBp effectiveArmor) / 10000, w

  /// Damage wakes a sleeper, as does any other hostile action — see `wake`. A
  /// corpse stops moving and stops casting, so it interpolates nowhere.
  let dealDamage (targetId: EntityId) (amount: int) (w: World) =
    let w =
      mapEntity
        targetId
        (fun t ->
          let hp = max 0 (t.Health - amount)

          { t with
              Health = hp
              Auras = t.Auras |> List.filter (fun a -> not (Aura.endsOnHostileAction a))
              Casting = if hp = 0 then None else t.Casting
              Goal = if hp = 0 then None else t.Goal
              Path = if hp = 0 then [] else t.Path
              StallTicks = if hp = 0 then ticks 0 else t.StallTicks
              Destination = if hp = 0 then None else t.Destination
              MoveTicksLeft = if hp = 0 then ticks 0 else t.MoveTicksLeft
              MoveTicksTotal = if hp = 0 then ticks 0 else t.MoveTicksTotal })
        w

    // Pushback: damage can interrupt an interruptible cast in progress, reduced
    // by the target's resistance (Q5a).
    if amount <= 0 then
      w
    else
      match tryEntity targetId w with
      | Some t when alive t && (t.Casting |> Option.exists (fun c -> c.Ability.Interruptible)) ->
        let roll, w = rollRange 0 9999 w
        let chance = pushbackBaseBp * (10000 - t.PushbackResistanceBp) / 10000

        if roll < chance then
          w
          |> mapEntity targetId (fun e -> { e with Casting = None })
          |> logLine (sprintf "%s's cast is interrupted by damage" t.Name)
        else
          w
      | _ -> w


  // =========================================================================
  // Ability resolution
  // =========================================================================

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
          let critBonus = if ability.Name = "Backstab" then actor.BackstabCritBonusBp else 0
          let amount, w = rollCrit (actor.CritChanceBp + critBonus) amount w
          let amount, w = mitigatePhysical targetId amount w
          let w = dealDamage targetId amount w
          // Only the Party builds threat on Mobs.
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w
          w |> logLine (sprintf "%s hits %s for %d" actor.Name targetName amount)

        | SpellDamage(lo, hi) ->
          let amount, w = rollRange lo hi w
          let amount, w = rollCrit actor.CritChanceBp amount w
          let w = dealDamage targetId amount w
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
            |> wake targetId
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
                  Auras =
                    Sleeping duration
                    :: (e.Auras |> List.filter (fun a -> not (Aura.isSleeping a)))
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
                  :: (e.Auras |> List.filter (fun a -> not (Aura.isSerpentForm a))) })
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
            |> wake targetId
            |> logLine (sprintf "%s taunts %s" actor.Name m.Name)
          | _ -> w

        | AreaDamage(radius, lo, hi) ->
          // One roll and one crit for the whole area, applied to every hostile
          // within `radius` of the actor. No friendly fire, and not mitigated by
          // armour (spell).
          let amount, w = rollRange lo hi w
          let amount, w = rollCrit actor.CritChanceBp amount w

          w.Entities
          |> List.filter (fun e ->
            e.Faction <> actor.Faction
            && alive e
            && Pos.chebyshev actor.Pos e.Pos <= radius)
          |> List.fold (fun w t ->
            let w = dealDamage t.Id amount w
            let w = if actor.Faction = Party then addThreat t.Id actor amount w else w
            w |> logLine (sprintf "%s blasts %s for %d" actor.Name t.Name amount)) w

        | Stun duration ->
          match tryEntity targetId w with
          | Some t ->
            w
            |> mapEntity targetId (fun e ->
              { e with
                  Auras = Aura.refresh (Stunned duration) e.Auras
                  Casting = None
                  Goal = None
                  Path = []
                  StallTicks = ticks 0
                  Destination = None })
            |> logLine (sprintf "%s stuns %s" actor.Name t.Name)
          | None -> w

        | Root duration ->
          match tryEntity targetId w with
          | Some t ->
            w
            |> mapEntity targetId (fun e ->
              { e with
                  Auras = Aura.refresh (Rooted duration) e.Auras
                  Goal = None
                  Path = []
                  Destination = None })
            |> logLine (sprintf "%s roots %s" actor.Name t.Name)
          | None -> w

        | Polymorph duration ->
          match tryEntity targetId w with
          | Some t ->
            w
            |> mapEntity targetId (fun e ->
              { e with
                  Auras = Aura.refresh (Polymorphed duration) e.Auras
                  Casting = None
                  Goal = None
                  Path = []
                  StallTicks = ticks 0
                  Destination = None })
            |> logLine (sprintf "%s polymorphs %s" actor.Name t.Name)
          | None -> w

        | HealOverTime(duration, perTick) ->
          match tryEntity targetId w with
          | Some t ->
            w
            |> mapEntity targetId (fun e ->
              { e with
                  Auras = Aura.refresh (Rejuvenation(duration, perTick)) e.Auras })
            |> logLine (sprintf "%s casts a heal-over-time on %s" actor.Name t.Name)
          | None -> w

        | Sunder(duration, reduction) ->
          match tryEntity targetId w with
          | Some t ->
            w
            |> mapEntity targetId (fun e ->
              { e with Auras = Aura.refresh (Sundered(duration, reduction)) e.Auras })
            |> logLine (sprintf "%s sunders %s" actor.Name t.Name)
          | None -> w

        | Threat amount ->
          let w =
            if actor.Faction = Party then addThreat targetId actor amount w else w

          w |> logLine (sprintf "%s threatens %s for %d" actor.Name targetName amount))


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
    // Using an ability interrupts a move in progress: the character stops to
    // strike or cast rather than walking on through the order.
    let w =
      mapEntity
        actor.Id
        (fun e ->
          { e with
              Cooldowns = e.Cooldowns |> Map.add ability.Name ability.CooldownTicks
              Goal = None
              Path = []
              Destination = None
              StallTicks = ticks 0 })
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
