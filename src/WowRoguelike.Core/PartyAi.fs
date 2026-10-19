namespace WowRoguelike.Core

/// The scripted party: every member the player is not driving this Tick gets its
/// order from here.
///
/// This is game AI, not a harness fixture. A Party member is a character with a
/// class, a kit and a job; when nobody is at the keyboard it plays that job —
/// Warrior holds and sunders, healers keep the group up, damage walks the Party's
/// target down in the order that kills it fastest. All five share one target
/// selection so the Party fights one thing at a time instead of five.
module PartyAi =

  let private ready (e: Entity) (abilityName: string) =
    (e.Cooldowns |> Map.tryFind abilityName |> Option.defaultValue (ticks 0)) <= ticks 0

  let private has (e: Entity) (abilityName: string) =
    e.Abilities |> List.exists (fun a -> a.Name = abilityName)

  /// Off cooldown and affordable. Affordability matters: a caster that keeps
  /// ordering spells while dry just fills the log.
  let private canUse (e: Entity) (abilityName: string) =
    ready e abilityName
    && (e.Abilities
        |> List.tryFind (fun a -> a.Name = abilityName)
        |> Option.exists (SimState.canAfford e))

  /// How far from the Tank the Party will pull. Anything further is a pull the
  /// Tank walks into alone, with the healers left behind out of range — which is
  /// how a scripted Party wipes to one boss room.
  let private leash = 8

  let private cmd (at: int<tick>) (kind: CommandKind) = [ { ApplyAt = at; Kind = kind } ]

  let private interrupts =
    fun (a: Ability) -> a.Effects |> List.exists (fun ef -> ef = Interrupt)

  /// Roughly how hard an ability hits, for ordering a kit the way a player would
  /// rather than by the order it happens to sit in the list.
  let private damageOf (a: Ability) =
    a.Effects
    |> List.sumBy (function
      | WeaponDamage(multiplierBp, bonus) -> multiplierBp / 100 + bonus
      | Damage(_, mx) -> mx
      | SpellDamage(_, mx) -> mx
      | _ -> 0)

  /// Armour already stripped off a target. Sunder Armor refreshes rather than
  /// stacking in this model, so this is zero or the flat reduction — but the
  /// selection below is written against the total, so stacking later needs no
  /// change here.
  let private sunderDepth (h: Entity) = h.Auras |> List.sumBy Aura.armorReduction

  /// What the Party should be hitting: whatever the Tank is on, unless something
  /// else carries deeper armour reduction — that is damage already paid for, and
  /// worth more than a fresh target.
  let strikeTarget (hostiles: Entity list) (tankTarget: EntityId option) =
    match hostiles with
    | [] -> None
    | _ ->
      hostiles
      |> List.sortByDescending (fun h -> sunderDepth h, (if Some h.Id = tankTarget then 1 else 0))
      |> List.tryHead

  /// What the Tank should be hitting: its own current target, else the nearest
  /// thing already on the Party, else the nearest one it can see.
  let private tankTarget (w: World) (tank: Entity option) (hostiles: Entity list) =
    match tank, hostiles with
    | _, [] -> None
    | None, _ -> hostiles |> List.sortByDescending sunderDepth |> List.tryHead
    | Some t, _ ->
      match hostiles |> List.tryFind (fun h -> Some h.Id = t.Target) with
      | Some h -> Some h
      | None ->
        hostiles
        |> List.filter (fun h -> SimState.canReach leash t h w)
        |> List.sortBy (fun h -> ((if h.Engaged then 0 else 1), Pos.chebyshev t.Pos h.Pos))
        |> List.tryHead

  /// Warrior. Taunt back anything that has left the Tank, keep Sunder Armor on the
  /// target (the debuff the Rogue's targeting reads), then spend spare rage on
  /// Heroic Strike. In that order, because armour reduction is group damage.
  let private tankOrders (w: World) (t: Entity) (target: Entity option) (at: int<tick>) =
    match target with
    | None -> []
    | Some f ->
      let sundered = f.Auras |> List.exists (fun a -> Aura.armorReduction a > 0)

      let thief =
        SimState.aliveHostiles w
        |> List.tryFind (fun h -> h.Engaged && h.Target <> Some t.Id && SimState.canReach 8 t h w)

      match thief with
      | Some h when has t "Taunt" && canUse t "Taunt" -> cmd at (UseAbility(t.Id, "Taunt", h.Id))
      | _ when not (SimState.withinRange 1 t f) -> cmd at (MoveTo(t.Id, f.Pos))
      | _ when has t "Sunder Armor" && not sundered && canUse t "Sunder Armor" ->
        cmd at (UseAbility(t.Id, "Sunder Armor", f.Id))
      | _ when canUse t "Heroic Strike" -> cmd at (UseAbility(t.Id, "Heroic Strike", f.Id))
      | _ when canUse t "Sunder Armor" -> cmd at (UseAbility(t.Id, "Sunder Armor", f.Id))
      | _ -> []

  /// Healer. A heal-over-time first: it is the cheapest health per point of mana
  /// and it keeps working while the next cast happens. It is refreshed only as it
  /// lapses, and the direct heal starts as soon as the deficit outruns what the
  /// HoTs in flight will give back — a three-second cast that begins when the bar
  /// is empty is a three-second cast that began too late.
  let private healerOrders (w: World) (h: Entity) (party: Entity list) (at: int<tick>) =
    // What this healer's heals actually do, so the choice is made on the numbers
    // and not on the order the abilities happen to sit in the kit: a Priest
    // reaching for Lesser Heal because it comes first is a dead Rogue.
    let healsOf (a: Ability) =
      a.Effects
      |> List.sumBy (function
        | Heal(_, mx) -> mx
        | HealOverTime(duration, perTick) -> (duration / secTicks 1.0) * perTick
        | _ -> 0)

    let hasHeal (a: Ability) =
      a.Effects |> List.exists (function Heal _ | HealOverTime _ -> true | _ -> false)

    let hasHot (a: Ability) =
      a.Effects |> List.exists (function HealOverTime _ -> true | _ -> false)

    // Biggest first, then the first one this healer can actually pay for.
    let bestOf (keep: Ability -> bool) =
      h.Abilities
      |> List.filter keep
      |> List.sortByDescending healsOf
      |> List.tryFind (fun a -> canUse h a.Name)

    let hot = bestOf hasHot
    let direct = bestOf (fun a -> hasHeal a && not (hasHot a))

    let projected (t: Entity) = t.Health + Aura.healOverTimePending t.Auras
    let projectedPct (t: Entity) = projected t * 100 / t.MaxHealth

    let wounded =
      party
      |> List.filter (fun e -> SimState.healthPct e < 90)
      |> List.sortBy SimState.healthPct

    // A tile this healer can actually heal from: in range, in sight, and free.
    // Walking to a tile beside the target is not enough — beside can be around a
    // corner, and then the healer stands there for the rest of the fight.
    let healingSpot (t: Entity) (a: Ability) =
      [ for dy in -a.Range .. a.Range do
          for dx in -a.Range .. a.Range do
            let p =
              { X = t.Pos.X + dx
                Y = t.Pos.Y + dy }

            if
              Grid.isFloor w.Grid p
              && Pos.chebyshev p t.Pos <= a.Range
              && not (SimState.claimedByOther w h.Id p)
              && Path.lineOfSight w.Grid p t.Pos
            then
              yield p ]
      |> List.sortBy (fun p -> Pos.chebyshev h.Pos p)
      |> List.tryHead

    // Into range, or not worth it: the reach rule is SimState's, never restated.
    let order (choice: Ability option) (t: Entity) =
      match choice with
      | Some a when SimState.canReach a.Range h t w -> cmd at (UseAbility(h.Id, a.Name, t.Id))
      | Some a ->
        cmd at (MoveTo(h.Id, healingSpot t a |> Option.defaultValue t.Pos))
      | None -> []

    // A HoT worth re-casting: none running, or one with under two seconds left.
    let lapsed (t: Entity) = Aura.healOverTimeRemaining t.Auras <= secTicks 2.0

    let hotTarget = wounded |> List.tryFind lapsed

    // Direct-heal candidates: projected health — now plus everything already
    // ticking, plus the HoT about to go up — still short of three quarters.
    let inFlight (t: Entity) =
      Aura.healOverTimePending t.Auras
      + (if Some t = hotTarget then hot |> Option.map healsOf |> Option.defaultValue 0 else 0)

    let directTarget =
      wounded
      |> List.filter (fun t -> t.Health + inFlight t < t.MaxHealth * 3 / 4)
      |> List.sortBy projectedPct
      |> List.tryHead

    match directTarget, hotTarget with
    | Some t, _ when projectedPct t < 55 -> order direct t
    | _, Some t -> order hot t
    | Some t, None -> order direct t
    | None, None -> []

  /// Melee damage. Kick a cast it can reach, then the hardest strike it can pay
  /// for on the Party's target.
  let private meleeOrders (w: World) (e: Entity) (target: Entity option) (at: int<tick>) =
    let strike =
      e.Abilities
      |> List.filter (fun a -> a.TargetKind = Foe && a.Range <= 1 && not (interrupts a))
      |> List.filter (fun a -> canUse e a.Name)
      |> List.sortByDescending damageOf
      |> List.tryHead

    let caster =
      SimState.aliveHostiles w
      |> List.tryFind (fun h -> h.Casting |> Option.exists (fun c -> c.Ability.Interruptible))

    match caster with
    | Some c when has e "Kick" && canUse e "Kick" && SimState.withinRange 1 e c ->
      cmd at (UseAbility(e.Id, "Kick", c.Id))
    | _ ->
      match target, strike with
      | Some f, Some a when SimState.withinRange 1 e f -> cmd at (UseAbility(e.Id, a.Name, f.Id))
      | Some f, _ -> cmd at (MoveTo(e.Id, f.Pos))
      | _ -> []
  /// Ranged damage. The furthest-reaching shot it can pay for, walking in only
  /// when the shot cannot reach.
  let private rangedOrders (w: World) (e: Entity) (target: Entity option) (at: int<tick>) =
    match target with
    | None -> []
    | Some f ->
      let shot =
        e.Abilities
        |> List.filter (fun a -> a.TargetKind = Foe && a.Range > 1 && not (interrupts a))
        |> List.filter (fun a -> canUse e a.Name)
        |> List.sortByDescending damageOf
        |> List.tryHead

      match shot with
      | Some a when SimState.canReach a.Range e f w -> cmd at (UseAbility(e.Id, a.Name, f.Id))
      | _ when not (SimState.withinRange 8 e f) -> cmd at (MoveTo(e.Id, f.Pos))
      | _ -> []

  /// One Tick of orders for the whole Party.
  let decide (w: World) : Command list =
    let at = w.Tick + ticks 1
    let party = SimState.aliveParty w

    // Only members who can start something get orders: re-issuing a cast mid-cast
    // is a queueing policy, not a decision.
    let actors =
      party |> List.filter (fun e -> e.Casting.IsNone && not (SimState.isSleeping e))

    let role r = actors |> List.filter (fun e -> e.Role = Some r)
    let melee (e: Entity) = e.Abilities |> List.exists (fun a -> a.TargetKind = Foe && a.Range <= 1)
    let ranged (e: Entity) = e.Abilities |> List.exists (fun a -> a.TargetKind = Foe && a.Range > 1)

    let tank = actors |> List.tryFind (fun e -> e.Role = Some Tank)

    // Only fight what the Tank can reach from where it stands. Beyond the leash
    // the Party walks to the fight together instead. Everything downstream reads
    // this list, so no member can wander off to a different mob.
    let hostiles =
      match tank with
      | Some t ->
        SimState.aliveHostiles w
        |> List.filter (fun h -> Some h.Id = t.Target || SimState.canReach leash t h w)
      | None -> SimState.aliveHostiles w

    let target = strikeTarget hostiles (tank |> Option.bind (fun t -> t.Target))
    let onTank = tankTarget w tank hostiles

    // Nothing to hit: the Tank leads and everyone else follows it. Moving the
    // Tank alone is what leaves the healers twelve tiles behind, out of range,
    // while it dies to a pull they cannot see.
    let advance =
      match tank with
      | None -> []
      | Some t ->
        let stray = role Tank @ role Healer @ role Dps

        [ match SimState.aliveHostiles w |> List.sortBy (fun h -> Pos.chebyshev t.Pos h.Pos) |> List.tryHead with
          | Some h -> yield! cmd at (MoveTo(t.Id, h.Pos))
          | None -> ()
          for e in stray do
            if e.Id <> t.Id && Pos.chebyshev e.Pos t.Pos > 3 then
              yield! cmd at (MoveTo(e.Id, SimState.approachTile e t.Pos w |> Option.defaultValue t.Pos)) ]

    if target.IsNone then advance
    else
      [ yield! role Tank |> List.collect (fun e -> tankOrders w e onTank at)
        yield! role Healer |> List.collect (fun e -> healerOrders w e party at)
        yield! role Dps |> List.filter melee |> List.collect (fun e -> meleeOrders w e target at)
        yield! role Dps |> List.filter ranged |> List.collect (fun e -> rangedOrders w e target at) ]

  let private actorOf (kind: CommandKind) =
    match kind with
    | UseAbility(actor, _, _) -> actor
    | MoveTo(actor, _) -> actor
    | Stop actor -> actor

  /// Fill in for whoever the player did not command this Tick. The player's orders
  /// always win: the script only drives the members left idle.
  let supplement (player: Command list) (w: World) : Command list =
    let driven = player |> List.map (fun c -> actorOf c.Kind) |> Set.ofList
    let scripted = decide w |> List.filter (fun c -> not (driven.Contains(actorOf c.Kind)))
    player @ scripted
