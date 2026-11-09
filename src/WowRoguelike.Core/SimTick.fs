namespace WowRoguelike.Core

open WowRoguelike.Core.SimState
open WowRoguelike.Core.SimCombat

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

  /// Summoned things live on their owner's clock: a totem runs out of time, and a
  /// pet dies when the Hunter that called it does. Neither can be modelled as an
  /// aura, because both are entities that hostiles can attack.
  let advanceSummons (w: World) =
    mapAll
      (fun e ->
        match e.Owner, e.ExpiresInTicks with
        | None, _ -> e
        | Some owner, _ when not (tryEntity owner w |> Option.exists alive) ->
          { e with Health = 0 }
        | _, Some left when left <= ticks 1 -> { e with Health = 0 }
        | _, Some left -> { e with ExpiresInTicks = Some(left - ticks 1) }
        | _, None -> e)
      w

  /// A totem works on the party from the tile it stands on: the two that hand out
  /// a number do it on their own clock, and the two that give a boon keep an aura
  /// on everyone in reach. An aura is re-applied every Tick, so it lapses shortly
  /// after the totem dies or the member walks out of range — which is why a totem
  /// needs no cleanup pass of its own.
  let advanceTotems (w: World) =
    let totems = w.Entities |> List.filter (fun e -> alive e && e.Totem.IsSome)

    if List.isEmpty totems then
      w
    else
      let party = aliveParty w

      let apply (w: World) (t: Entity) (e: Entity) =
        match t.Totem.Value.Pulse with
        | Heals amount when amount > 0 ->
          mapEntity e.Id (fun m -> { m with Health = min m.MaxHealth (m.Health + amount) }) w
        | RestoresMana amount ->
          mapEntity e.Id (fun m -> { m with Resource = min m.MaxResource (m.Resource + amount) }) w
        | ManaFraction bp ->
          let amount = e.MaxResource * bp / 10000
          mapEntity e.Id (fun m -> { m with Resource = min m.MaxResource (m.Resource + amount) }) w
        | ArmorBoon armor ->
          mapEntity e.Id (fun m -> { m with Auras = Aura.refresh (TotemBoon(ticks 2, armor, 0, 0)) m.Auras }) w
        | AgilityBoon agility ->
          // Agility is ten crit and five dodge per point, the rates `derive` turns a
          // hero's own Agility into.
          let boon = TotemBoon(ticks 2, 0, agility * 10, agility * 5)
          mapEntity e.Id (fun m -> { m with Auras = Aura.refresh boon m.Auras }) w
        | Cleanses school ->
          mapEntity
            e.Id
            (fun m ->
              { m with
                  Auras =
                    m.Auras
                    |> List.filter (fun a -> Aura.isHelpful a || Aura.dispelType a <> school) })
            w
        | Heals _ -> w
        | Redirects -> w (* read at spell resolution, not on a clock *)

      (w, totems)
      ||> List.fold (fun w t ->
        let spec = t.Totem.Value

        if spec.Period <= ticks 0 || w.Tick % spec.Period <> ticks 0 then
          w
        else
          party
          |> List.filter (fun e -> Pos.chebyshev t.Pos e.Pos <= spec.Radius)
          |> List.fold (fun w e -> apply w t e) w)

  /// Heal-over-time and Polymorph's regen tick down once a second.
  let advanceAuraHealing (w: World) =
    mapAll
      (fun e ->
        if not (alive e) then
          e
        else
          let firing = e.Auras |> List.filter Aura.firesThisTick
          let flat = firing |> List.sumBy Aura.flatHealPerTick
          let pctBp = firing |> List.sumBy Aura.maxHealthHealBpPerTick
          let heal = flat + (e.MaxHealth * pctBp / 10000)
          // Wound Poison cuts heals over time as well as casts; they are paid for
          // by the same healer, so they meet the same reduction.
          let cut = e.Auras |> List.sumBy Aura.healingReceivedReductionBp
          let heal = heal * max 0 (10000 - cut) / 10000

          if heal <= 0 then
            e
          else
            { e with Health = min e.MaxHealth (e.Health + heal) })
      w

  /// Damage-over-time ticks down once a second, applied unmitigated. It routes
  /// through dealDamage so the target wakes and, if it dies, drops its orders.
  ///
  /// The tick then offers the *applier* its own event, once per aura that fired:
  /// Nightfall rolls per tick of a Corruption, not per burning target per second,
  /// and it rolls on the caster because the damage lands on the target. A dot
  /// applied by nobody (`caster = None`, which is every mob's) offers nothing.
  let advanceAuraDamage (w: World) =
    let ticking (e: Entity) = e.Auras |> List.filter Aura.firesThisTick

    let perTick (e: Entity) =
      ticking e |> List.sumBy (fun a -> Aura.flatDamagePerTick a + Aura.poisonTick a)

    let burning = w.Entities |> List.filter (fun e -> alive e && perTick e > 0)

    let w = (w, burning) ||> List.fold (fun w e -> dealDamage e.Id (perTick e) w)

    // After the damage, so a tick that kills still ticks: the last tick of a
    // Corruption is a tick. `burning` is the pre-damage list on purpose.
    (w, burning)
    ||> List.fold (fun w e ->
      (w, ticking e |> List.choose Aura.dotCaster)
      ||> List.fold (fun w caster -> SimCombat.procOn OnDoTTick caster w))

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
  /// Touches and then it is spent. A Rogue's energy does not work that way: it
  /// comes back 20 per 2 s on its own clock, which at 20 Ticks to the second is
  /// one point every other Tick, whatever his Spirit or the content says.
  let advanceResource (w: World) =
    let energyTick = w.Tick % ticks 2 = ticks 0

    mapAll
      (fun e ->
        if not (alive e) then
          e
        else
          let gain =
            match e.ResourceKind with
            | Energy -> if energyTick then 1 else 0
            | Mana ->
              // The rate comes from Spirit (see `Content.derive`). Inside the five
              // seconds after a cast it pays a Tick less rather than a fraction,
              // because the rate is a whole number of points — so a priest's Spirit
              // still carries regeneration through a cast and a mage's thin Spirit
              // does not, which is the class difference the rule exists to make.
              let fresh =
                e.Auras
                |> List.exists (function
                  | RecentlyCast _ -> true
                  | _ -> false)

              let rate = if fresh then max 0 (e.ResourceRegenPerTick - 1) else e.ResourceRegenPerTick

              // Innervate multiplies what is actually there, so a target that has
              // stopped casting gets the full five times and a target that keeps
              // casting multiplies the trickle.
              let boon =
                e.Auras
                |> List.tryPick (function
                  | Innervated(_, bp) -> Some bp
                  | _ -> None)
                |> Option.defaultValue 0

              rate * (10000 + boon) / 10000
            | Rage -> 0

          // Viper Sting is paid out of the pool, once a second like every other
          // periodic aura. Regen is added first so the sting cuts what the target
          // actually has, not what it would have had.
          let drain =
            e.Auras |> List.filter Aura.firesThisTick |> List.sumBy Aura.flatDrainPerTick

          { e with Resource = max 0 (min e.MaxResource (e.Resource + gain) - drain) })
      w

  /// Out of combat the party rests. This is not a trickle: it is **five seconds of
  /// waiting and then the whole pool back**, which is why nobody carries food and why
  /// the Tank's between-pull stop is the only cost. The restore itself is stepped at
  /// 10% of the pool a Tick so the health bar visibly snaps up rather than blinking,
  /// which costs half a second and is why `RestTicks` keeps running past the mark.
  ///
  /// Rage is the exception, and it is what makes the class the class: it decays
  /// (vanilla's 2 a second) instead of refilling, so a Warrior walks into the
  /// next pull with whatever the last one left him rather than a full 100.
  let advanceOutOfCombatRegen (w: World) =
    let rageDecay = if w.Tick % ticks 10 = ticks 0 then 1 else 0
    let rested = w.RestTicks >= Content.outOfCombatRestTicks

    mapAll
      (fun e ->
        if e.Faction <> Party || not (alive e) then
          e
        else
          let health =
            if rested then
              min e.MaxHealth (e.Health + Content.outOfCombatHealthPerTick e.MaxHealth)
            else
              e.Health

          let resource =
            match e.ResourceKind with
            | Rage -> max 0 (e.Resource - rageDecay)
            | _ when rested ->
              min e.MaxResource (e.Resource + Content.outOfCombatResourcePerTick e.MaxResource)
            | _ -> e.Resource

          { e with Health = health; Resource = resource })
      { w with RestTicks = w.RestTicks + ticks 1 }

  /// Regeneration is the one phase that asks whose fight it is. Combat resets the
  /// rest clock; everything out of combat counts down to the restore.
  let advanceRegen (w: World) =
    if inCombat w then
      advanceResource { w with RestTicks = ticks 0 }
    else
      advanceOutOfCombatRegen w

  /// One tile of progress. A moving entity loses its cast — and a sleeping one
  /// neither moves nor keeps a goal.
  /// Passing through a tile is allowed; ending a Tick on one is not, because two
  /// bodies drawn on the same tile stay stacked for the rest of the run and both
  /// stop being able to path past anything. Whoever shares with an earlier entry in
  /// the list steps to the nearest free neighbour. Only a cross-over leaves a
  /// neighbour unavailable, so this moves nobody on an ordinary Tick.
  /// A mover whose step lands on a settled ally's tile trades places with it. In a
  /// one-wide corridor nobody else can yield, so without the trade the leader holds a
  /// goal it can never reach: the body in front has no order to move, so the tile is
  /// never freed and the route is dropped as stale every backoff. Only Party trades
  /// with Party — a hostile's tile is a target, not a place to stand — and the trade
  /// is atomic, so `separateStacked` never sees two bodies on one tile.
  let private swapStep (w: World) =
    let settledPartyAt (p: Pos) =
      w.Entities
      |> List.tryFind (fun o ->
        alive o
        && o.Faction = Party
        && o.Totem.IsNone
        && o.Role <> Some Tank
        && o.Pos = p
        && o.Destination.IsNone)

    let trades =
      w.Entities
      |> List.choose (fun e ->
        match e.Destination with
        | Some d when alive e && e.Faction = Party && e.MoveTicksLeft <= ticks 1 ->
          match settledPartyAt d with
          | Some b when b.Id <> e.Id -> Some(e.Id, e.Pos, b.Id, d)
          | _ -> None
        | _ -> None)

    (w, trades)
    ||> List.fold (fun acc (mover, from, blocker, toward) ->
      acc
      |> mapEntity mover (fun m ->
        { m with
            Pos = toward
            Destination = None
            MoveTicksLeft = ticks 0
            MoveTicksTotal = ticks 0 })
      |> mapEntity blocker (fun b -> { b with Pos = from }))

  let private separateStacked (w: World) =
    let _, entities =
      (([], []), w.Entities)
      ||> List.fold (fun (taken, acc) e ->
        if not (alive e) || e.Totem.IsSome then
          // A totem takes no space and never gives ground: leave it where it was
          // summoned and let bodies share its tile.
          taken, e :: acc
        else
          let spot =
            if List.contains e.Pos taken then
              Path.neighbours (fun p -> Grid.isFloor w.Grid p && not (List.contains p taken)) e.Pos
              |> List.sortBy (fun p -> p.X, p.Y)
              |> List.tryHead
              |> Option.defaultValue e.Pos
            else
              e.Pos

          spot :: taken, { e with Pos = spot } :: acc)

    { w with Entities = List.rev entities }

  let advanceMovement (w: World) =
    // A body that is going nowhere holds its tile; a body that is itself stepping
    // through does not. Two members may share a tile in passing, which is what lets
    // a corridor carry traffic in both directions, but nobody settles onto someone
    // who has stopped — that one keeps its route and waits the tick.
    // `held` is every tile a body will still be standing on when this tick ends:
    // a body with no destination, or one whose step does not complete now
    // (`MoveTicksLeft > 1` is exactly the case that only decrements). A body whose
    // step lands this tick is vacating, so its tile can be taken. A totem is not a
    // body: it holds nothing and nobody has to walk around it.
    //
    // It used to be "any body with no destination", which let a mover settle onto
    // a body that was mid-step and still three ticks from leaving. `separateStacked`
    // cannot always undo that: in a one-tile corridor there is no free neighbour to
    // push to, so the two stayed on the same square.
    let held =
      w.Entities
      |> List.filter (fun e ->
        alive e
        && e.Totem.IsNone
        && (e.Destination.IsNone || e.MoveTicksLeft > ticks 1))
      |> List.map (fun e -> e.Pos)
      |> Set.ofList

    mapAll
      (fun e ->
        if not (alive e) then
          e
        elif not (canMove e) then
          if e.Auras |> List.exists Aura.preventsActing then
            // Incapacitated: drop everything, it will not resume.
            { e with
                Goal = None
                Path = []
                StallTicks = ticks 0
                Destination = None
                MoveTicksLeft = ticks 0 }
          else
            // Rooted: hold position and resume when the root ends.
            e
        else
          match e.Destination with
          | None -> e
          | Some dest ->
            let e = { e with Casting = None }

            if e.MoveTicksLeft > ticks 1 then
              { e with MoveTicksLeft = e.MoveTicksLeft - ticks 1 }
            elif Set.contains dest held then
              e
            else
              { e with
                  Pos = dest
                  Destination = None
                  MoveTicksLeft = ticks 0
                  MoveTicksTotal = ticks 0 })
      w
    |> separateStacked
    |> swapStep


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
        let found =
          if List.isEmpty planned then
            (Path.astar w.Grid taken e.Pos dest).Path
          else
            planned

        // Five abreast claim every tile of a corridor at once, so the claim-aware
        // route fails while the claim-free one succeeds — and because none of them
        // steps, none ever frees a tile. Retrying with a *stationary* ally's tile
        // passable hands the mover the free prefix of a route, which is enough:
        // one body moves, its tile frees, and the queue drains. The step below
        // still has to clear `taken`, so the relaxed search can never put two
        // bodies on one tile. A body already stepping into a tile still reserves
        // it — that claim is about to become real.
        let stationary p =
          w.Entities
          |> List.exists (fun o -> alive o && o.Id <> e.Id && o.Pos = p && o.Destination.IsNone)

        if List.isEmpty found then
          (Path.astar w.Grid (fun p -> taken p && not (stationary p)) e.Pos dest).Path
        else
          found

      match route with
      | [] ->
        { e with
            Path = []
            StallTicks = pathRetryBackoff }
      | next :: rest ->
        // A settled Party body on the next step is not a reason to throw the route
        // away: it will trade tiles with the mover when the step lands (`swapStep`
        // in `advanceMovement`), which is the only way a one-wide corridor lets its
        // leader past an ally that has no order to move. Anything else — a hostile,
        // or a body mid-step — still means the route is stale.
        let aSettledAlly =
          w.Entities
          |> List.exists (fun o ->
            alive o
            && o.Id <> e.Id
            && o.Faction = Party
            && o.Totem.IsNone
            && o.Role <> Some Tank
            && o.Pos = next
            && o.Destination.IsNone)

        if taken next && not aSettledAlly then
          { e with
              Path = []
              StallTicks = pathRetryBackoff }
        else
          // Crippling Poison raises the cost of the next tile. Seeded here because
          // this is the one place the per-tile cost is read; an existing step in
          // progress keeps the length it started with.
          let slow = e.Auras |> List.sumBy Aura.movementSlowBp
          let step = e.MoveTicksPerTile * (10000 + slow) / 10000

          { e with
              Path = rest
              Destination = Some next
              MoveTicksLeft = step
              MoveTicksTotal = step
              StallTicks = ticks 0 }

  /// Stepped as a fold rather than a map, so two entities cannot claim the same
  /// tile in one tick.
  let advanceGoals (w: World) =
    w.Entities
    |> List.fold
      (fun (acc: World) e ->
        if not (canMove e) then
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
      | Some t when alive t || Ability.resurrects c.Ability -> resolveAbility actorId c.Target c.Ability w
      | _ ->
        w
        |> logLine (sprintf "%s's %s fizzles (target gone)" (nameOf actorId w) c.Ability.Name))

  /// The name a white swing answers to in `Talents.damageBp`. A swing has no
  /// ability row to key off, so it is one fixed name for every class: a tree that
  /// raises auto-attack damage either sets `DamageDoneBp` (One-Handed Weapon
  /// Specialization) or tags `Spells` with this name. It is also why a talent that
  /// must tell a bow from a dagger needs a name on `AutoAttack`, which does not
  /// exist yet — no landed talent cares.
  let swingName = "Attack"

  /// Everyone with a swing timer swings. Party auto-attack only engages Mobs
  /// that are already in the fight, so nobody pulls by accident.
  let advanceSwings (w: World) =
    let attackers =
      w.Entities
      |> List.filter (fun e ->
        canAct e && e.AutoAttack.IsSome && not (e.Auras |> List.exists Aura.disablesAttack))

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

      // The off-hand weapon is the fast one — vanilla's 1.5s dagger against a 2.6s
      // main hand — so it lands more often for half the damage each time. 3/5 of the
      // main interval is that ratio, and the clock is the world tick rather than a
      // per-entity timer so nothing new has to be carried in the entity: the rate is
      // what matters, and the Rogue is the only dual-wielder in the content.
      let offHandTicks = max (ticks 2) (attack.Ticks * 3 / 5)

      // Each hand keeps its own clock. The main hand is the entity's swing timer;
      // the off-hand's is the world tick modulo its own (faster) interval, so it
      // lands independently of the main hand instead of riding along with it.
      let mainReady = e.SwingTicksLeft <= ticks 1
      let offReady = e.OffHand && w.Tick % offHandTicks = ticks 0

      if not mainReady && not offReady then
        mapEntity e.Id (fun x -> { x with SwingTicksLeft = x.SwingTicksLeft - ticks 1 }) w
      else
        let w =
          if mainReady then
            // The interval is re-read on every reset, so a haste buff that lands
            // mid-swing shortens the *next* swing rather than retroactively
            // speeding up the one in flight — the timer already spent is spent.
            mapEntity e.Id (fun x -> { x with SwingTicksLeft = AutoAttack.cadence attack e.Auras }) w
          else
            mapEntity e.Id (fun x -> { x with SwingTicksLeft = x.SwingTicksLeft - ticks 1 }) w

        match target with
        | None -> w
        | Some t ->
          // Two blades, two poisons: the main hand carries the situation's choice
          // and the off hand the best different one. Decided once per swing so both
          // hands meet the same situation.
          let mainPoison, offPoison =
            if SimCombat.carriesPoison e then
              SimCombat.poisonPlan t (aliveHostiles w)
            else
              InstantPoison, InstantPoison

          let w =
            if not mainReady then
              w
            else
              let rolled, w = rollRange attack.Min attack.Max w
              // The tree's share of a white swing, on the same principle as the
              // weapon-strike path in `SimCombat.resolveAbility`: one multiply, and a
              // character with no talents multiplies by 10000. Serpent Form's bonus is
              // added after it, being the form's damage rather than the weapon's.
              let total =
                (rolled + e.MeleeDamageBonus) * Talents.damageBp swingName e.Talents / 10000 + bonus

              // The auto-attack runs the same attack table every physical special does,
              // so a mob dodges both identically.
              let total, w = SimCombat.meleeStrike e t.Id total 0 None w

              w
              |> dealDamage t.Id total
              |> SimCombat.gainRage e total
              |> fun w -> if e.Faction = Party then addThreat t.Id e total w else w
              |> fun w ->
                if total > 0 then
                  logLine (sprintf "%s swings at %s for %d" e.Name t.Name total) w
                else
                  w
              |> fun w ->
                if total > 0 && SimCombat.carriesPoison e then
                  SimCombat.poisonHit e mainPoison t.Id w
                else
                  w
              // "After firing": only a timer with range on it. Rolled once per
              // landed shot, so a miss or a dodge is not a chance to proc.
              |> fun w ->
                if total > 0 && attack.Range > 1 then
                  SimCombat.procOn OnShot e.Id w
                else
                  w

          // Half the damage, but it gets there more often: the off-hand swings on
          // its own faster cadence rather than riding the main hand. shortcut: it
          // rolls the main-hand table, so it misses 5% rather than vanilla's 24% —
          // worth about 6% of a Rogue's damage; give `meleeStrike` a miss-penalty
          // parameter if that matters.
          if not offReady then
            w
          else
            let rolled, w = rollRange attack.Min attack.Max w
            // The off hand takes the same tree's share: a talent that raises white
            // damage raises both hands.
            let offRoll =
              (rolled + e.MeleeDamageBonus) * Talents.damageBp swingName e.Talents / 10000 + bonus

            let offHand, w = SimCombat.meleeStrike e t.Id (offRoll / 2) 0 None w

            w
            |> dealDamage t.Id offHand
            |> SimCombat.gainRage e offHand
            |> fun w -> if e.Faction = Party then addThreat t.Id e offHand w else w
            |> fun w ->
              if offHand > 0 then
                logLine (sprintf "%s's off hand hits %s for %d" e.Name t.Name offHand) w
              else
                w
            |> fun w ->
              if offHand > 0 && SimCombat.carriesPoison e then
                SimCombat.poisonHit e offPoison t.Id w
              else
                w)

  /// Recompute every Hostile's aggression target, honoring the margins and
  /// clearing a Taunt whose taunter has died.
  let retarget (w: World) =
    mapAll
      (fun e ->
        if e.Faction <> Hostile || not (alive e) || e.Dormant then
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
  /// rotation reads heal → renew → slumber → bolt → serpent form.
  ///
  /// Every shape a hostile kit actually ships has to be named here or the ability
  /// is content the sim never fires: until `SpellDamage`, `HealOverTime` and
  /// `Polymorph` were added, a Fireball, a mob's Renew and Arcanist Doan's
  /// Polymorph were all data with no reader, and the only damage a caster mob ever
  /// dealt was its auto-attack.
  let mobDecisions (w: World) =
    let acting = w.Entities |> List.filter (fun e -> e.Faction = Hostile && not e.Dormant && canAct e)

    (w, acting)
    ||> List.fold (fun w m ->
      // Proximity aggro: a Mob notices the Party before any threat exists — and
      // only what it can actually see, so a pack in the next room stays asleep
      // until the Party walks in (Q: aggro through walls is not a pull mechanic).
      let noticed =
        aliveParty w
        |> List.exists (fun p ->
          Pos.chebyshev m.Pos p.Pos <= aggroRadius && Path.lineOfSight w.Grid m.Pos p.Pos)

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
          let ready (a: Ability) =
            (m.Cooldowns |> Map.tryFind a.Name |> Option.defaultValue (ticks 0)) <= ticks 0
            && canAfford m a

          // Prefer a ready one, so two control spells on the same Mob take turns
          // instead of the first match starving the second.
          let byEffect (pred: Effect -> bool) =
            let matching =
              m.Abilities |> List.filter (fun a -> List.exists pred a.Effects)

            matching |> List.tryFind ready |> Option.orElse (List.tryHead matching)

          let healAbility = byEffect (function Heal _ -> true | _ -> false)
          let sleepAbility = byEffect (function Sleep _ -> true | _ -> false)
          let damageAbility = byEffect (function Damage _ -> true | _ -> false)

          /// A hostile caster's bolt: Fireball, Frostbolt, Smite, Flame Spike,
          /// Immolate. Separate from `Damage` because a spell is not mitigated by
          /// armour and does not roll against dodge, and because a mob that has
          /// both should spend its cast rather than its swing.
          let spellAbility =
            byEffect (function SpellDamage _ -> true | _ -> false)

          /// Renew: a heal-over-time on a friendly mob. The AI has no way to
          /// value a HoT over a heal, so this only fires when the direct heal is
          /// on cooldown or the mob has none — which is exactly when a Chaplain
          /// or an Adept would use it.
          let renewAbility =
            byEffect (function HealOverTime _ -> true | _ -> false)

          /// Polymorph. A control spell like Sleep, and it shares Sleep's
          /// one-at-a-time rule: two polymorphs on one pack is a pack that never
          /// fights back at all.
          let polymorphAbility =
            byEffect (function Polymorph _ -> true | _ -> false)

          let shiftAbility =
            byEffect (function Shapeshift(Serpent, _, _) -> true | _ -> false)

          let areaAbility =
            byEffect (function AreaDamage _ -> true | _ -> false)

          let stunAbility =
            byEffect (function Stun _ -> true | _ -> false)

          // Only one Party member may be slept at a time.
          let anyoneAsleep = aliveParty w |> List.exists isSleeping

          let anyonePolymorphed =
            aliveParty w |> List.exists (fun p -> p.Auras |> List.exists (function Polymorphed _ -> true | _ -> false))

          let choice =
            [ healAbility
              |> Option.filter (fun a -> ready a && canCast m)
              |> Option.bind (fun a -> usableTarget m a w |> Option.map (fun t -> a, t))

              // A HoT goes on an ally that is already hurt, after the direct heal
              // has had its chance: a mob that heals and renews on the same tick
              // should heal, because the heal saves the body this tick.
              renewAbility
              |> Option.filter (fun a -> ready a && canCast m)
              |> Option.bind (fun a -> usableTarget m a w |> Option.map (fun t -> a, t))

              // Area damage and stuns are aimed at the current target and only
              // when it is in reach, so a clap is never cast at nothing.
              areaAbility
              |> Option.filter (fun a -> ready a && canCast m && canReach a.Range m target w)
              |> Option.map (fun a -> a, target.Id)

              stunAbility
              |> Option.filter (fun a -> ready a && canCast m && canReach a.Range m target w)
              |> Option.map (fun a -> a, target.Id)

              sleepAbility
              |> Option.filter (fun a -> ready a && canCast m && not anyoneAsleep)
              |> Option.bind (fun a -> usableTarget m a w |> Option.map (fun t -> a, t))

              polymorphAbility
              |> Option.filter (fun a ->
                ready a && canCast m && not anyonePolymorphed && canReach a.Range m target w)
              |> Option.map (fun a -> a, target.Id)

              // The bolt before the swing: a caster mob's damage is its spell.
              spellAbility
              |> Option.filter (fun a -> ready a && canCast m && canReach a.Range m target w)
              |> Option.map (fun a -> a, target.Id)

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
    |> advanceRegen
    |> advanceSummons
    |> advanceTotems
    |> advanceAuras
    |> advanceAuraHealing
    |> advanceAuraDamage
    |> advanceMovement
    |> advanceCasts
    |> advanceSwings
    |> callForHelp
    |> retarget
    |> mobDecisions
    |> advanceGoals
    // Last, so it sees the fight as the tick left it: the wipe that ended the pull
    // has to have happened before the room can be told to forget it.
    |> SimState.encounterReset

  /// Run `n` Ticks with no new input.
  let run (n: int) (w: World) : World = (w, [ 1..n ]) ||> List.fold (fun w _ -> step [] w)
