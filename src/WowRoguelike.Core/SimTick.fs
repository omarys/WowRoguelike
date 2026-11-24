namespace WowRoguelike.Core

open WowRoguelike.Core.SimState
open WowRoguelike.Core.SimCombat

/// The per-Tick pipeline, and movement. Phase order is the contract: player
/// intent lands before the world reacts to it, and everything acting in a Tick
/// sees the same post-damage world.
module SimTick =

  open System.Collections.Generic

  /// Thread-local generation-stamped flat tile occupancy table, turning
  /// `claimedByOther` from an O(N) entity-list scan into an O(1) array lookup.
  type private TileClaims() =
    let mutable tileStamp = Array.zeroCreate<int> 0
    let mutable tileOwner = Array.zeroCreate<int> 0
    let mutable stamp = 0
    let mutable width = 0
    let mutable height = 0

    [<System.ThreadStatic; DefaultValue>]
    static val mutable private instance: TileClaims

    static member Get() =
      let s = TileClaims.instance

      if obj.ReferenceEquals(s, null) then
        let fresh = TileClaims()
        TileClaims.instance <- fresh
        fresh
      else
        s

    member _.Begin(g: Grid) =
      let size = g.Width * g.Height
      width <- g.Width
      height <- g.Height

      if tileStamp.Length < size then
        let cap = max size (max 256 (tileStamp.Length * 2))
        tileStamp <- Array.zeroCreate<int> cap
        tileOwner <- Array.zeroCreate<int> cap
        stamp <- 1
      elif stamp = System.Int32.MaxValue then
        System.Array.Clear(tileStamp, 0, tileStamp.Length)
        stamp <- 1
      else
        stamp <- stamp + 1

    member _.PopulateClaimed(w: World) =
      let g = w.Grid
      let size = g.Width * g.Height
      width <- g.Width
      height <- g.Height

      if tileStamp.Length < size then
        let cap = max size (max 256 (tileStamp.Length * 2))
        tileStamp <- Array.zeroCreate<int> cap
        tileOwner <- Array.zeroCreate<int> cap
        stamp <- 1
      elif stamp = System.Int32.MaxValue then
        System.Array.Clear(tileStamp, 0, tileStamp.Length)
        stamp <- 1
      else
        stamp <- stamp + 1

      let curStamp = stamp
      let wWidth = width
      let wHeight = height
      let ts = tileStamp
      let to' = tileOwner

      for e in w.Entities do
        if e.Health > 0 && e.Totem.IsNone && e.Destination.IsNone then
          let px = e.Pos.X
          let py = e.Pos.Y

          if px >= 0 && py >= 0 && px < wWidth && py < wHeight then
            let idx = py * wWidth + px
            let (EntityId idInt) = e.Id

            if ts.[idx] <> curStamp then
              ts.[idx] <- curStamp
              to'.[idx] <- idInt
            elif to'.[idx] <> idInt then
              to'.[idx] <- System.Int32.MinValue

    member _.IsClaimedByOtherIdx(selfIdInt: int, idx: int) =
      tileStamp.[idx] = stamp && tileOwner.[idx] <> selfIdInt

    member _.IsClaimedByOtherPos(selfIdInt: int, p: Pos) =
      p.X >= 0
      && p.Y >= 0
      && p.X < width
      && p.Y < height
      && (let idx = p.Y * width + p.X in tileStamp.[idx] = stamp && tileOwner.[idx] <> selfIdInt)

    /// Remove `selfIdInt` from its tile when it begins moving (Destination becomes Some).
    /// Returns false if multiple entities shared the tile and a full repopulate is needed.
    member _.Release(selfIdInt: int, p: Pos) =
      if p.X >= 0 && p.Y >= 0 && p.X < width && p.Y < height then
        let idx = p.Y * width + p.X

        if tileStamp.[idx] = stamp then
          if tileOwner.[idx] = selfIdInt then
            tileStamp.[idx] <- 0
            true
          else
            false
        else
          true
      else
        true

    /// Fast O(N) check whether any two alive non-totem entities share a tile.
    member this.HasAnyStacked(w: World) =
      this.Begin(w.Grid)
      let curStamp = stamp
      let wWidth = width
      let wHeight = height
      let ts = tileStamp
      let mutable stacked = false
      let mutable cur = w.Entities

      while not stacked && not (List.isEmpty cur) do
        let e = cur.Head
        cur <- cur.Tail

        if e.Health > 0 && e.Totem.IsNone then
          let px = e.Pos.X
          let py = e.Pos.Y

          if px >= 0 && py >= 0 && px < wWidth && py < wHeight then
            let idx = py * wWidth + px

            if ts.[idx] = curStamp then
              stacked <- true
            else
              ts.[idx] <- curStamp

      stacked


  // =========================================================================
  // Per-tick phases
  // =========================================================================

  /// Auras age, and anything that runs out falls off. The ageing rule for each
  /// kind lives in `Aura.advance`, which is the one place that matches every case.
  let advanceAuras (w: World) =
    if not (w.Entities |> List.exists (fun e -> not e.Auras.IsEmpty)) then
      w
    else
      mapAll
        (fun e ->
          if e.Auras.IsEmpty then
            e
          else
            let auras = e.Auras |> List.choose Aura.advance

            let lapsed =
              e.Auras
              |> List.tryPick (function
                | Invigorated(_, _, granted) -> Some granted
                | _ -> None)

            match lapsed with
            | Some granted ->
              let holds =
                auras
                |> List.exists (function
                  | Invigorated _ -> true
                  | _ -> false)

              if not holds then
                let maximum = e.MaxHealth - granted
                { e with Auras = auras; MaxHealth = maximum; Health = min e.Health maximum }
              else
                { e with Auras = auras }
            | None -> { e with Auras = auras })
        w

  /// Summoned things live on their owner's clock: a totem runs out of time, and a
  /// pet dies when the Hunter that called it does. Neither can be modelled as an
  /// aura, because both are entities that hostiles can attack.
  let advanceSummons (w: World) =
    if not (w.Entities |> List.exists (fun e -> e.Owner.IsSome || e.ExpiresInTicks.IsSome)) then
      w
    else
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
    if not (w.Entities |> List.exists (fun e -> alive e && e.Totem.IsSome)) then
      w
    else
      let totems = w.Entities |> List.filter (fun e -> alive e && e.Totem.IsSome)
      let party = aliveParty w

      let apply (w: World) (t: Entity) (e: Entity) =
        // A pulse is silent in the log — aura ticks write no line — so the run's
        // ledger is accumulated right here, where the pulse lands. Delivered is
        // capped at the member's own deficit: overheal and overfill are not
        // delivery, and the question the ledger answers is what the totem saved
        // the healer in spells and the casters in drinking. `e` is the member as
        // the Tick began, which is exact for a pulse: one totem per slot, so no
        // other totem has moved the member's bars since the snapshot was taken.
        let book h m (w: World) =
          { w with TotemLedger = TotemLedger.pulse t.Name e.Id (max 0 h) (max 0 m) w.TotemLedger }

        match t.Totem.Value.Pulse with
        | Heals amount when amount > 0 ->
          book (min (e.MaxHealth - e.Health) amount) 0
            (mapEntity e.Id (fun m -> { m with Health = min m.MaxHealth (m.Health + amount) }) w)
        | RestoresMana amount ->
          book 0 (min (e.MaxResource - e.Resource) amount)
            (mapEntity e.Id (fun m -> { m with Resource = min m.MaxResource (m.Resource + amount) }) w)
        | ManaFraction bp ->
          let amount = e.MaxResource * bp / 10000
          book 0 (min (e.MaxResource - e.Resource) amount)
            (mapEntity e.Id (fun m -> { m with Resource = min m.MaxResource (m.Resource + amount) }) w)
        | ArmorBoon armor ->
          book 0 0
            (mapEntity e.Id (fun m -> { m with Auras = Aura.refresh (TotemBoon(ticks 2, armor, 0, 0)) m.Auras }) w)
        | AgilityBoon agility ->
          // Agility is ten crit and five dodge per point, the rates `derive` turns a
          // hero's own Agility into.
          let boon = TotemBoon(ticks 2, 0, agility * 10, agility * 5)
          book 0 0 (mapEntity e.Id (fun m -> { m with Auras = Aura.refresh boon m.Auras }) w)
        | Cleanses school ->
          book 0 0
            (mapEntity
              e.Id
              (fun m ->
                { m with
                    Auras =
                      m.Auras
                      |> List.filter (fun a -> Aura.isHelpful a || Aura.dispelType a <> school) })
              w)
        | Heals _ -> w
        | Redirects -> w (* read at spell resolution, not on a clock *)

      (w, totems)
      ||> List.fold (fun w t ->
        let spec = t.Totem.Value

        // A totem with no period is a boon that is simply on; the fold must reach
        // it every Tick or the aura never lands at all. A totem with a period
        // pulses only on its own beat.
        if spec.Period > ticks 0 && w.Tick % spec.Period <> ticks 0 then
          w
        else
          party
          |> List.filter (fun e -> Pos.chebyshev t.Pos e.Pos <= spec.Radius)
          |> List.fold (fun w e -> apply w t e) w)

  /// The party members that could have laid a tick of `spell`.
  ///
  /// A heal-over-time carries the *spell* that made it and not the caster: two
  /// Druid heals are one kind of aura and the source is what tells them apart
  /// (`Aura.Rejuvenation`), so unlike a dot — which carries an `EntityId` because
  /// Nightfall and Ancient Brutality read their own ticks — a tick has to be
  /// traced back by the kit. One Restoration Druid, which is every party this
  /// repo measures, resolves exactly. Two of them and the aura genuinely cannot
  /// say which one it was.
  let private hotCasters (w: World) (spell: string) =
    w.Entities
    |> List.filter (fun e ->
      e.Faction = Party
      && (e.Abilities |> List.exists (fun a -> a.Name = spell)))

  /// The log lines one heal-over-time tick is worth, one per caster it belongs to:
  /// the direct heal's line character for character (`<caster>'s <spell> heals
  /// <target> for N`), because the log is the only event record `Attribution` reads
  /// and a tick booked any other way is invisible to `sim attrib` and `sim healers`
  /// alike. Two casters of the class split the tick evenly rather than each being
  /// credited the whole of it — the total stays honest, which is what the healing
  /// column is asking for.
  let private hotLines (w: World) (target: Entity) (spell: string) (amount: int) =
    let line (caster: Entity) (share: int) =
      sprintf "%s's %s heals %s for %d" caster.Name spell target.Name share

    match hotCasters w spell with
    | [] -> []
    | [ caster ] -> [ line caster amount ]
    | casters ->
      let n = List.length casters

      casters
      |> List.mapi (fun i c ->
        let share = if i = n - 1 then amount - (amount / n) * (n - 1) else amount / n
        if share > 0 then Some(line c share) else None)
      |> List.choose id

  /// One Tick's worth of heal-over-time written down as healing done: split across
  /// the auras that fired, by the per-second heal each one carries, and cut to the
  /// health that actually moved — overheal is not healing done, which is the rule
  /// the direct path already enforces with its `effective`.
  ///
  /// Written per firing aura, not per Tick: a tick is once a second, so this is a
  /// handful of lines a second beside the swing and hit lines, not the flood a
  /// refusal-per-Tick would be. Polymorph's regen rides in the same total and is
  /// nobody's spell, so the heal-over-times take only their share of what landed.
  let private bookHotTick (w: World) (target: Entity) (firing: Aura list) (heal: int) (effective: int) =
    let ticking =
      firing
      |> List.choose (fun a ->
        match a with
        | Rejuvenation(_, perTick, spell) when perTick > 0 -> Some(spell, perTick)
        | _ -> None)

    let flat = ticking |> List.sumBy snd
    let landing = if heal > 0 then effective * flat / heal else 0
    let mutable left = landing

    (w, List.indexed ticking)
    ||> List.fold (fun w (i, (spell, perTick)) ->
      let last = i = List.length ticking - 1
      let share = if last then left else min left (landing * perTick / flat)
      left <- left - share

      if share <= 0 then
        w
      else
        (w, hotLines w target spell share) ||> List.fold (fun w l -> logLine l w))

  /// Heal-over-time and Polymorph's regen tick down once a second.
  let advanceAuraHealing (w: World) =
    (w, w.Entities)
    ||> List.fold (fun w e ->
      if not (alive e) || e.Auras.IsEmpty then
        w
      else
        let mutable anyFiring = false
        let mutable flat = 0
        let mutable pctBp = 0

        for a in e.Auras do
          if Aura.firesThisTick a then
            anyFiring <- true
            flat <- flat + Aura.flatHealPerTick a
            pctBp <- pctBp + Aura.maxHealthHealBpPerTick a

        if not anyFiring then
          w
        else
          let heal = flat + (e.MaxHealth * pctBp / 10000)

          if heal <= 0 then
            w
          else
            let cut = e.Auras |> List.sumBy Aura.healingReceivedReductionBp
            let heal = heal * max 0 (10000 - cut) / 10000

            if heal <= 0 then
              w
            else
              let firing = e.Auras |> List.filter Aura.firesThisTick
              let health = min e.MaxHealth (e.Health + heal)
              let w = mapEntity e.Id (fun x -> { x with Health = health }) w
              bookHotTick w e firing heal (health - e.Health))

  /// Damage-over-time ticks down once a second, applied unmitigated. It routes
  /// through dealDamage so the target wakes and, if it dies, drops its orders.
  let advanceAuraDamage (w: World) =
    let mutable burning: (Entity * int) list = []

    for e in w.Entities do
      if alive e && not e.Auras.IsEmpty then
        let mutable dmg = 0

        for a in e.Auras do
          if Aura.firesThisTick a then
            dmg <- dmg + Aura.flatDamagePerTick a + Aura.poisonTick a

        if dmg > 0 then
          burning <- (e, dmg) :: burning

    if burning.IsEmpty then
      w
    else
      let burning = List.rev burning
      let w = (w, burning) ||> List.fold (fun w (e, dmg) -> dealDamage e.Id dmg w)

      (w, burning)
      ||> List.fold (fun w (e, _) ->
        (w, e.Auras |> List.choose (fun a -> if Aura.firesThisTick a then Aura.dotCaster a else None))
        ||> List.fold (fun w caster -> SimCombat.procOn OnDoTTick caster w))

  let advanceCooldowns (w: World) =
    if not (w.Entities |> List.exists (fun e -> not e.Cooldowns.IsEmpty)) then
      w
    else
      mapAll
        (fun e ->
          if e.Cooldowns.IsEmpty then
            e
          else
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
    let manaTick = w.Tick % Content.manaRegenTicks = ticks 0
    let mp5Tick = w.Tick % secTicks 5.0 = ticks 0

    mapAll
      (fun e ->
        if not (alive e) then
          e
        else
          let hasAuras = not e.Auras.IsEmpty

          let gain =
            match e.ResourceKind with
            | Rage -> 0
            | Energy ->
              if not energyTick then
                0
              else
                let boon =
                  if not hasAuras then
                    0
                  else
                    e.Auras
                    |> List.tryPick (function
                      | Innervated(_, bp) -> Some bp
                      | _ -> None)
                    |> Option.defaultValue 0

                1 + 1 * boon / 10000
            | Mana ->
              if not manaTick && not mp5Tick then
                0
              elif not hasAuras then
                if manaTick then e.ResourceRegenPerTick else 0
              else
                let mutable boon = 0
                let mutable fresh = false
                let mutable globes = 0
                let mutable shieldMp5 = 0

                for a in e.Auras do
                  match a with
                  | Innervated(_, bp) when boon = 0 -> boon <- bp
                  | RecentlyCast _ -> fresh <- true
                  | WaterShield(charges, _, _, bp, mp5) when globes = 0 && shieldMp5 = 0 ->
                    globes <- charges * bp
                    shieldMp5 <- mp5
                  | _ -> ()

                let rate =
                  if not manaTick then 0
                  elif fresh then e.ResourceRegenPerTick * e.CombatRegenBp / 10000
                  else e.ResourceRegenPerTick

                let steady = if mp5Tick then shieldMp5 else 0
                rate * (10000 + boon + globes) / 10000 + steady

          let drain =
            if not hasAuras then
              0
            else
              let mutable d = 0

              for a in e.Auras do
                if Aura.firesThisTick a then
                  d <- d + Aura.flatDrainPerTick a

              d

          if gain = 0 && drain = 0 then
            e
          else
            let nextRes = max 0 (min e.MaxResource (e.Resource + gain) - drain)
            if nextRes = e.Resource then e else { e with Resource = nextRes })
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
  let private swapStep (justLanded: Set<EntityId>) (w: World) =
    // `moverFrom` is the tile the mover is standing on, so a body coming the other
    // way is a mutual crossing: both end where they meant to go. A body that landed
    // in this same pass is still in motion, not settled, and is not a trade partner —
    // that is what keeps the loser of a same-tile race (`yields`, above) waiting
    // behind the winner instead of trading places with it.
    // The Tank used to be excluded from trading outright, and the cost of that
    // exclusion was a permanent deadlock: measured, seed 1 of `wailingCaverns` after
    // the last kill in the first room — a Rogue's next step was the parked Tank's
    // tile, the Tank had no order left to give, and nobody could step, so the Party
    // stood still for the 3000 ticks the walk test allows. A trade is mutual, so a
    // Tank that trades ends one tile sideways, still beside the body it let past.
    let settledPartyAt (moverFrom: Pos) (p: Pos) =
      w.Entities
      |> List.tryFind (fun o ->
        alive o
        && o.Faction = Party
        && o.Totem.IsNone
        && not (Set.contains o.Id justLanded)
        && o.Pos = p
        && (o.Destination.IsNone || o.Destination = Some moverFrom))

    let trades =
      w.Entities
      |> List.choose (fun e ->
        match e.Destination with
        | Some d when alive e && e.Faction = Party && e.MoveTicksLeft <= ticks 1 ->
          match settledPartyAt e.Pos d with
          | Some b when b.Id <> e.Id -> Some(e.Id, e.Pos, b.Id, d)
          | _ -> None
        | _ -> None)

    (w, trades)
    ||> List.fold (fun acc (mover, from, blocker, toward) ->
      // Both squares must be empty of everyone but the pair. Two movers queued behind
      // the same settled body pick the same tile to trade for, and applying both put
      // them on it together — the second trade's `toward` is the first mover's new
      // `Pos`. A trade refused here is one body waiting the Tick, which is what
      // `held` already does to every other contested step, so nothing is lost: the
      // losing mover keeps its destination and lands next Tick if the tile clears.
      let thirdParty =
        acc.Entities
        |> List.exists (fun o ->
          o.Id <> mover
          && o.Id <> blocker
          && alive o
          && o.Totem.IsNone
          && (o.Pos = toward || o.Pos = from))

      if thirdParty then
        acc
      else
        acc
        |> mapEntity mover (fun m ->
          { m with
              Pos = toward
              Destination = None
              MoveTicksLeft = ticks 0
              MoveTicksTotal = ticks 0
              StallTicks = ticks 0 })
        |> mapEntity blocker (fun b -> { b with Pos = from }))

  let private separateStacked (w: World) =
    if not ((TileClaims.Get()).HasAnyStacked(w)) then
      w
    else
      let taken = HashSet<Pos>()
      let mutable acc = []

      for e in w.Entities do
        if not (alive e) || e.Totem.IsSome then
          acc <- e :: acc
        else
          let spot =
            if taken.Contains e.Pos then
              Path.neighbours (fun p -> Grid.isFloor w.Grid p && not (taken.Contains p)) e.Pos
              |> List.sortBy (fun p -> p.X, p.Y)
              |> List.tryHead
              |> Option.defaultValue e.Pos
            else
              e.Pos

          taken.Add spot |> ignore
          acc <- (if spot = e.Pos then e else { e with Pos = spot }) :: acc

      { w with Entities = List.rev acc }

  let private settleArrived (w: World) =
    if not (w.Entities |> List.exists (fun e -> alive e && e.Totem.IsNone && e.Destination = Some e.Pos)) then
      w
    else
      mapAll
        (fun e ->
          if
            alive e
            && e.Totem.IsNone
            && e.Destination = Some e.Pos
          then
            { e with
                Destination = None
                MoveTicksLeft = ticks 0
                MoveTicksTotal = ticks 0
                StallTicks = ticks 0 }
          else
            e)
        w

  let advanceMovement (w: World) =
    let mutable anyMoving = false
    let mutable anyIncapacitated = false

    for e in w.Entities do
      if alive e then
        if e.Destination.IsSome then
          anyMoving <- true
        elif not e.Auras.IsEmpty && not (canMove e) && (e.Auras |> List.exists Aura.preventsActing) then
          if e.Goal.IsSome || not e.Path.IsEmpty || e.StallTicks <> ticks 0 || e.MoveTicksLeft <> ticks 0 then
            anyIncapacitated <- true

    if not anyMoving && not anyIncapacitated then
      separateStacked w
    else
      let stepRefusalLimit = ticks 3
      let w = settleArrived w

      let held = HashSet<Pos>()
      let dueMovers = ResizeArray<Entity>()

      for e in w.Entities do
        if alive e && e.Totem.IsNone then
          if e.Destination.IsNone || e.MoveTicksLeft > ticks 1 then
            held.Add e.Pos |> ignore
          else
            dueMovers.Add e

      let firstToLand = Dictionary<Pos, EntityId>()
      let yields = HashSet<EntityId>()
      let landedNow = HashSet<EntityId>()

      let mutable changed = true

      while changed do
        firstToLand.Clear()
        yields.Clear()
        landedNow.Clear()

        for i = 0 to dueMovers.Count - 1 do
          let e = dueMovers.[i]

          if canMove e then
            let d = e.Destination.Value

            if not (held.Contains d) then
              match firstToLand.TryGetValue d with
              | true, prevId ->
                if e.Id < prevId then
                  yields.Add prevId |> ignore
                  firstToLand.[d] <- e.Id
                else
                  yields.Add e.Id |> ignore
              | false, _ -> firstToLand.[d] <- e.Id

        for kv in firstToLand do
          landedNow.Add kv.Value |> ignore

        let beforeCount = held.Count

        for i = 0 to dueMovers.Count - 1 do
          let e = dueMovers.[i]

          if not (landedNow.Contains e.Id) then
            held.Add e.Pos |> ignore

        changed <- held.Count <> beforeCount

      let landedSet =
        if landedNow.Count = 0 then Set.empty else Set.ofSeq landedNow

      mapAll
        (fun e ->
          if not (alive e) then
            e
          elif not (canMove e) then
            if e.Auras |> List.exists Aura.preventsActing then
              { e with
                  Goal = None
                  Path = []
                  StallTicks = ticks 0
                  Destination = None
                  MoveTicksLeft = ticks 0 }
            else
              e
          else
            match e.Destination with
            | None -> e
            | Some dest ->
              let e = if e.Casting.IsSome then { e with Casting = None } else e

              if e.MoveTicksLeft > ticks 1 then
                { e with MoveTicksLeft = e.MoveTicksLeft - ticks 1 }
              elif yields.Contains e.Id then
                { e with
                    Destination = None
                    Path = []
                    MoveTicksLeft = ticks 0
                    MoveTicksTotal = ticks 0 }
              elif held.Contains dest then
                if e.StallTicks >= stepRefusalLimit then
                  { e with
                      Destination = None
                      Path = []
                      MoveTicksLeft = ticks 0
                      MoveTicksTotal = ticks 0 }
                else
                  { e with StallTicks = e.StallTicks + ticks 1 }
              else
                { e with
                    Pos = dest
                    Destination = None
                    MoveTicksLeft = ticks 0
                    MoveTicksTotal = ticks 0
                    StallTicks = ticks 0 })
        w
      |> separateStacked
      |> swapStep landedSet


  /// Begin stepping toward a goal, using a pre-populated O(1) tile claims table.
  let private stepTowardWithClaims (claims: TileClaims) (e: Entity) (dest: Pos) (w: World) : Entity =
    if e.Pos = dest then
      { e with
          Goal = None
          Destination = None
          Path = []
          StallTicks = ticks 0 }
    elif e.Destination.IsSome then
      e
    elif e.StallTicks > ticks 0 then
      { e with StallTicks = e.StallTicks - ticks 1 }
    else
      let (EntityId selfIdInt) = e.Id
      let takenPos p = claims.IsClaimedByOtherPos(selfIdInt, p)

      let planned =
        match e.Path with
        | next :: _ when takenPos next -> []
        | kept -> kept

      let route =
        let found =
          if List.isEmpty planned then
            (Path.astarByIndex w.Grid (fun idx -> claims.IsClaimedByOtherIdx(selfIdInt, idx)) e.Pos dest).Path
          else
            planned

        // Relaxed fallback: stationary tiles are passable. Since `taken` only
        // includes `Destination.IsNone` (stationary) entities, `taken && not stationary`
        // is identically false.
        if List.isEmpty found then
          (Path.astarByIndex w.Grid (fun _ -> false) e.Pos dest).Path
        else
          found

      match route with
      | [] ->
        { e with
            Path = []
            StallTicks = pathRetryBackoff }
      | next :: rest ->
        let nextTaken = takenPos next

        let aSettledAlly =
          nextTaken
          && (w.Entities
              |> List.exists (fun o ->
                alive o
                && o.Id <> e.Id
                && o.Faction = Party
                && o.Totem.IsNone
                && o.Role <> Some Tank
                && o.Pos = next
                && o.Destination.IsNone))

        if nextTaken && not aSettledAlly then
          { e with
              Path = []
              StallTicks = pathRetryBackoff }
        else
          let slow =
            if e.Auras.IsEmpty then 0 else e.Auras |> List.sumBy Aura.movementSlowBp

          let step = e.MoveTicksPerTile * (10000 + slow) / 10000

          { e with
              Path = rest
              Destination = Some next
              MoveTicksLeft = step
              MoveTicksTotal = step
              StallTicks = ticks 0 }

  /// Begin stepping toward a goal. A* plans the route once and the entity walks
  /// it, re-planning only when the route runs out or its next step is taken.
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
      { e with StallTicks = e.StallTicks - ticks 1 }
    else
      let claims = TileClaims.Get()
      claims.PopulateClaimed(w)
      stepTowardWithClaims claims e dest w

  /// Stepped as a fold rather than a map, so two entities cannot claim the same
  /// tile in one tick.
  let advanceGoals (w: World) =
    if not (w.Entities |> List.exists (fun e -> e.Goal.IsSome && canMove e)) then
      w
    else
      let claims = TileClaims.Get()
      claims.PopulateClaimed(w)

      let mutable acc = w

      for e in w.Entities do
        if canMove e then
          match e.Goal with
          | None -> ()
          | Some g ->
            let updated = stepTowardWithClaims claims e g acc

            if not (obj.ReferenceEquals(updated, e)) then
              if e.Destination.IsNone && updated.Destination.IsSome then
                let (EntityId idInt) = e.Id

                if not (claims.Release(idInt, e.Pos)) then
                  acc <- mapEntity e.Id (fun _ -> updated) acc
                  claims.PopulateClaimed(acc)
                else
                  acc <- mapEntity e.Id (fun _ -> updated) acc
              else
                acc <- mapEntity e.Id (fun _ -> updated) acc

      acc

  /// Casts tick down, then the ones that finished resolve against the world as
  /// it stands after every cast has advanced.
  let advanceCasts (w: World) =
    if not (w.Entities |> List.exists (fun e -> e.Casting.IsSome)) then
      w
    else
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
    let readyAttackers = ResizeArray<Entity * bool * bool>()
    let mutable anyNonReady = false

    for e in w.Entities do
      if canAct e && e.AutoAttack.IsSome && (e.Auras.IsEmpty || not (e.Auras |> List.exists Aura.disablesAttack)) then
        let attack = e.AutoAttack.Value
        let offHandTicks = max (ticks 2) (attack.Ticks * 3 / 5)
        let mainReady = e.SwingTicksLeft <= ticks 1
        let offReady = e.OffHand && w.Tick % offHandTicks = ticks 0

        if mainReady || offReady then
          readyAttackers.Add(e, mainReady, offReady)
        else
          anyNonReady <- true

    // Batch-decrement all non-ready attackers in one single pass instead of N mapEntity rebuilds.
    let w =
      if not anyNonReady then
        w
      else
        mapAll
          (fun e ->
            if
              canAct e
              && e.AutoAttack.IsSome
              && (e.Auras.IsEmpty || not (e.Auras |> List.exists Aura.disablesAttack))
            then
              let attack = e.AutoAttack.Value
              let offHandTicks = max (ticks 2) (attack.Ticks * 3 / 5)
              let mainReady = e.SwingTicksLeft <= ticks 1
              let offReady = e.OffHand && w.Tick % offHandTicks = ticks 0

              if not mainReady && not offReady then
                { e with SwingTicksLeft = e.SwingTicksLeft - ticks 1 }
              else
                e
            else
              e)
          w

    let mutable acc = w

    for i = 0 to readyAttackers.Count - 1 do
      let e, mainReady, offReady = readyAttackers.[i]
      let attack = e.AutoAttack.Value

      let target =
        if e.Faction = Party then
          let mutable bestDist = System.Int32.MaxValue
          let mutable bestTarget: Entity option = None

          for t in acc.Entities do
            if t.Faction = Hostile && t.Health > 0 && not t.Dormant && t.Engaged && canReach attack.Range e t acc then
              let d = Pos.chebyshev e.Pos t.Pos
              if d < bestDist then
                bestDist <- d
                bestTarget <- Some t

          bestTarget
        else
          currentTarget e acc |> Option.filter (fun t -> canReach attack.Range e t acc)

      let bonus = if e.Auras.IsEmpty then 0 else serpentBonus e

      let w =
        if mainReady then
          mapEntity e.Id (fun x -> { x with SwingTicksLeft = AutoAttack.cadence attack e.Auras }) acc
        else
          mapEntity e.Id (fun x -> { x with SwingTicksLeft = x.SwingTicksLeft - ticks 1 }) acc

      acc <-
        match target with
        | None -> w
        | Some t ->
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
              let total =
                (rolled + e.MeleeDamageBonus) * Talents.damageBp swingName e.Talents / 10000 + bonus

              let total, crit, w = SimCombat.meleeStrike e t.Id total 0 None w

              w
              |> dealDamage t.Id total
              |> SimCombat.gainRage e total
              |> fun w -> if e.Faction = Party then addThreat t.Id e total w else w
              |> fun w ->
                if total > 0 then
                  logLine
                    (if crit then
                       sprintf "%s critically swings at %s for %d" e.Name t.Name total
                     else
                       sprintf "%s swings at %s for %d" e.Name t.Name total)
                    w
                else
                  w
              |> fun w ->
                if total > 0 && SimCombat.carriesPoison e then
                  SimCombat.poisonHit e mainPoison t.Id w
                else
                  w
              |> fun w ->
                if total > 0 && attack.Range > 1 then
                  SimCombat.procOn OnShot e.Id w
                else
                  w

          if not offReady then
            w
          else
            let rolled, w = rollRange attack.Min attack.Max w
            let offRoll =
              (rolled + e.MeleeDamageBonus) * Talents.damageBp swingName e.Talents / 10000 + bonus

            let offHand, offCrit, w = SimCombat.meleeStrike e t.Id (offRoll / 2) 0 None w

            w
            |> dealDamage t.Id offHand
            |> SimCombat.gainRage e offHand
            |> fun w -> if e.Faction = Party then addThreat t.Id e offHand w else w
            |> fun w ->
              if offHand > 0 then
                logLine
                  (if offCrit then
                     sprintf "%s's off hand critically hits %s for %d" e.Name t.Name offHand
                   else
                     sprintf "%s's off hand hits %s for %d" e.Name t.Name offHand)
                  w
              else
                w
            |> fun w ->
              if offHand > 0 && SimCombat.carriesPoison e then
                SimCombat.poisonHit e offPoison t.Id w
              else
                w

    acc

  /// Recompute every Hostile's aggression target, honoring the margins and
  /// clearing a Taunt whose taunter has died.
  let retarget (w: World) =
    let party = aliveParty w
    let seen = party |> List.filter (fun p -> not (isStealthed p))

    let isAlivePartyId (id: EntityId) =
      let rec loop (xs: Entity list) =
        match xs with
        | [] -> false
        | p :: rest -> if p.Id = id then true else loop rest

      loop party

    mapAll
      (fun e ->
        if e.Faction <> Hostile || not (alive e) || e.Dormant then
          e
        elif not e.Engaged then
          if e.Target.IsNone && e.ForcedTarget.IsNone then
            e
          else
            { e with
                Target = None
                ForcedTarget = None }
        else
          let forced =
            match e.ForcedTarget with
            | Some id when isAlivePartyId id -> e.ForcedTarget
            | _ -> None

          let withForced =
            if forced = e.ForcedTarget then e else { e with ForcedTarget = forced }

          let nextTarget = resolveTargetWithSeen withForced seen

          if nextTarget = withForced.Target then
            withForced
          else
            { withForced with Target = nextTarget })
      w

  /// Mobs decide. Ability priority is expressed by effect shape, so the caster
  /// rotation reads heal → renew → slumber → bolt → serpent form.
  let mobDecisions (w: World) =
    let claims = TileClaims.Get()
    claims.PopulateClaimed(w)
    let mutable claimsDirty = false
    let mutable party = aliveParty w
    let mutable partyDirty = false
    let mutable acc = w

    let inline ensureParty () =
      if partyDirty then
        party <- aliveParty acc
        partyDirty <- false

      party

    let inline ensureClaims () =
      if claimsDirty then
        claims.PopulateClaimed(acc)
        claimsDirty <- false

      claims

    for m0 in w.Entities do
      if m0.Faction = Hostile && not m0.Dormant && canAct m0 then
        let pList = ensureParty ()

        let m =
          if not m0.Engaged then
            let noticed =
              pList
              |> List.exists (fun p ->
                Pos.chebyshev m0.Pos p.Pos <= aggroRadius && Path.lineOfSight acc.Grid m0.Pos p.Pos)

            if noticed then
              let w1 =
                acc
                |> mapEntity m0.Id (fun x -> { x with Engaged = true })
                |> logLine (sprintf "%s notices the party" m0.Name)

              let m' = tryEntity m0.Id w1 |> Option.defaultValue { m0 with Engaged = true }
              let targetId = resolveTarget m' w1
              acc <- mapEntity m0.Id (fun x -> { x with Target = targetId }) w1
              { m' with Target = targetId }
            else
              m0
          else
            m0

        if m.Engaged then
          let targetOpt =
            let findInParty (id: EntityId) =
              let rec loop (xs: Entity list) =
                match xs with
                | [] -> None
                | p :: rest -> if p.Id = id then Some p else loop rest

              loop pList

            match m.ForcedTarget |> Option.bind findInParty with
            | Some p -> Some p
            | None -> m.Target |> Option.bind findInParty

          match targetOpt with
          | None -> ()
          | Some target ->
            let ready (a: Ability) =
              (if m.Cooldowns.IsEmpty then
                 true
               else
                 (m.Cooldowns |> Map.tryFind a.Name |> Option.defaultValue (ticks 0)) <= ticks 0)
              && canAfford m a

            let byEffect (pred: Effect -> bool) =
              let mutable firstMatch: Ability option = None
              let mutable readyMatch: Ability option = None
              let mutable cur = m.Abilities

              while readyMatch.IsNone && not (List.isEmpty cur) do
                let a = cur.Head
                cur <- cur.Tail

                if List.exists pred a.Effects then
                  if firstMatch.IsNone then
                    firstMatch <- Some a

                  if ready a then
                    readyMatch <- Some a

              match readyMatch with
              | Some _ -> readyMatch
              | None -> firstMatch

            let choice =
              if m.Abilities.IsEmpty then
                None
              else
                let mCanCast = canCast m

                let mutable picked: (Ability * EntityId) option = None

                if mCanCast then
                  match byEffect (function Heal _ -> true | _ -> false) with
                  | Some a when ready a ->
                    picked <- usableTarget m a acc |> Option.map (fun t -> a, t)
                  | _ -> ()

                  if picked.IsNone then
                    match byEffect (function HealOverTime _ -> true | _ -> false) with
                    | Some a when ready a ->
                      picked <- usableTarget m a acc |> Option.map (fun t -> a, t)
                    | _ -> ()

                  if picked.IsNone then
                    match byEffect (function AreaDamage _ -> true | _ -> false) with
                    | Some a when ready a && canReach a.Range m target acc ->
                      picked <- Some(a, target.Id)
                    | _ -> ()

                  if picked.IsNone then
                    match byEffect (function Stun _ -> true | _ -> false) with
                    | Some a when ready a && canReach a.Range m target acc ->
                      picked <- Some(a, target.Id)
                    | _ -> ()

                  if picked.IsNone then
                    match byEffect (function Sleep _ -> true | _ -> false) with
                    | Some a when ready a && not (pList |> List.exists isSleeping) ->
                      picked <- usableTarget m a acc |> Option.map (fun t -> a, t)
                    | _ -> ()

                  if picked.IsNone then
                    match byEffect (function Polymorph _ -> true | _ -> false) with
                    | Some a when
                      ready a
                      && canReach a.Range m target acc
                      && not (pList |> List.exists (fun p -> p.Auras |> List.exists (function Polymorphed _ -> true | _ -> false)))
                      ->
                      picked <- Some(a, target.Id)
                    | _ -> ()

                  if picked.IsNone then
                    match byEffect (function SpellDamage _ -> true | _ -> false) with
                    | Some a when ready a && canReach a.Range m target acc ->
                      picked <- Some(a, target.Id)
                    | _ -> ()

                  if picked.IsNone then
                    match byEffect (function Damage _ -> true | _ -> false) with
                    | Some a when ready a && canReach a.Range m target acc ->
                      picked <- Some(a, target.Id)
                    | _ -> ()

                if picked.IsNone then
                  match byEffect (function Shapeshift(Serpent, _, _) -> true | _ -> false) with
                  | Some a when ready a && not (isShifted m) && inMelee m target ->
                    picked <- Some(a, m.Id)
                  | _ -> ()

                picked

            match choice with
            | Some(ability, targetId) ->
              acc <-
                startOrResolve m targetId ability acc
                |> mapEntity m.Id (fun e ->
                  if e.Goal.IsNone && e.Path.IsEmpty && e.StallTicks = ticks 0 then
                    e
                  else
                    { e with
                        Goal = None
                        Path = []
                        StallTicks = ticks 0 })

              partyDirty <- true
              claimsDirty <- true
            | None ->
              let (EntityId selfIdInt) = m.Id
              let c = ensureClaims ()

              let fleeTo =
                if
                  m.CreatureType = CreatureType.Humanoid
                  && SimState.healthPct m <= 20
                  && SimState.canAct m
                then
                  let mutable arrived = false
                  let mutable bestDist = System.Int32.MaxValue
                  let mutable bestAlly: Entity option = None

                  for e in acc.Entities do
                    if
                      e.Faction = Hostile
                      && e.Health > 0
                      && not e.Dormant
                      && not e.Engaged
                      && e.Id <> m.Id
                    then
                      let d = Pos.chebyshev m.Pos e.Pos

                      if d <= 2 then
                        arrived <- true
                      elif d > 3 && d < bestDist then
                        bestDist <- d
                        bestAlly <- Some e

                  if arrived then None else bestAlly
                else
                  None

              match fleeTo with
              | Some ally ->
                match SimState.approachTileWith m.Pos ally.Pos acc.Grid (fun _ _ idx -> c.IsClaimedByOtherIdx(selfIdInt, idx)) with
                | Some spot when spot <> m.Pos ->
                  acc <-
                    acc
                    |> mapEntity m.Id (fun e ->
                      if e.Goal = Some spot then
                        e
                      else
                        { e with
                            Goal = Some spot
                            Path = []
                            StallTicks = ticks 0 })
                    |> logLine (sprintf "%s flees toward %s" m.Name ally.Name)
                | _ ->
                  if m.Goal.IsSome || not m.Path.IsEmpty || m.StallTicks <> ticks 0 then
                    acc <-
                      mapEntity
                        m.Id
                        (fun e ->
                          { e with
                              Goal = None
                              Path = []
                              StallTicks = ticks 0 })
                        acc
              | None ->
                if inMelee m target then
                  if m.Goal.IsSome || not m.Path.IsEmpty || m.StallTicks <> ticks 0 then
                    acc <-
                      mapEntity
                        m.Id
                        (fun e ->
                          { e with
                              Goal = None
                              Path = []
                              StallTicks = ticks 0 })
                        acc
                else
                  let keep =
                    match m.Goal with
                    | Some g when
                      Pos.chebyshev g target.Pos <= 1
                      && Grid.isFloor acc.Grid g
                      && not (c.IsClaimedByOtherPos(selfIdInt, g))
                      ->
                      Some g
                    | _ -> None

                  let spotOpt =
                    match keep with
                    | Some _ -> keep
                    | None ->
                      SimState.approachTileWith
                        m.Pos
                        target.Pos
                        acc.Grid
                        (fun _ _ idx -> c.IsClaimedByOtherIdx(selfIdInt, idx))

                  match spotOpt with
                  | Some spot ->
                    if m.Goal <> Some spot then
                      acc <-
                        mapEntity
                          m.Id
                          (fun e ->
                            { e with
                                Goal = Some spot
                                Path = []
                                StallTicks = ticks 0 })
                          acc
                  | None ->
                    if m.Goal.IsSome || not m.Path.IsEmpty || m.StallTicks <> ticks 0 then
                      acc <-
                        mapEntity
                          m.Id
                          (fun e ->
                            { e with
                                Goal = None
                                Path = []
                                StallTicks = ticks 0 })
                          acc

    acc

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

    if callers.IsEmpty then
      w
    else
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
    let nextTick = w0.Tick + ticks 1

    let w, due =
      if w0.Pending.IsEmpty && incoming.IsEmpty then
        { w0 with Tick = nextTick }, []
      else
        let pending = if w0.Pending.IsEmpty then incoming elif incoming.IsEmpty then w0.Pending else w0.Pending @ incoming
        let due, later = pending |> List.partition (fun c -> c.ApplyAt <= nextTick)
        { w0 with Tick = nextTick; Pending = later }, due

    let w =
      if due.IsEmpty then w else (w, due) ||> List.fold (fun w c -> applyCommand c w)

    w
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
    |> SimState.ManaBudget.settle w0
    |> SimState.encounterReset

  /// Run `n` Ticks with no new input.
  let run (n: int) (w: World) : World = (w, [ 1..n ]) ||> List.fold (fun w _ -> step [] w)
