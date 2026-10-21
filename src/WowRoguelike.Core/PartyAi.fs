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
      | WeaponDamage(multiplierBp, bonus) -> 30 * multiplierBp / 10000 + bonus
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
  let strikeTarget (w: World) (tank: Entity option) (hostiles: Entity list) =
    match hostiles with
    | [] -> None
    | _ ->
      // The Mob the Tank holds the most threat on is the one that will not turn
      // around: Dps damage there is free, damage anywhere else drags a Mob off the
      // Tank and onto a healer. Sunder depth breaks the tie, because armour
      // reduction compounds everyone's damage.
      hostiles
      |> List.sortByDescending (fun h ->
        (match tank with
         | Some t -> SimState.threatOf t.Id h
         | None -> 0),
        sunderDepth h,
        (if tank |> Option.exists (fun t -> Some h.Id = t.Target) then 1 else 0))
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

  /// The eight tiles around a spot, in a fixed order so a choice never depends on
  /// how a list happened to be sorted.
  let private ring =
    [ -1, -1; 0, -1; 1, -1; -1, 0; 1, 0; -1, 1; 0, 1; 1, 1 ]

  /// Mobs face whatever they are attacking and the sim keeps no facing field, so
  /// "behind a mob" is the tile on the far side of it from `from` — the tile a
  /// Rogue has to stand on to land a Backstab.
  let private behind (m: Entity) (from: Pos) =
    Pos.move (sign (m.Pos.X - from.X)) (sign (m.Pos.Y - from.Y)) m.Pos

  /// The tile behind a mob: opposite whatever it is facing, which is whatever it
  /// is attacking. None when it has no target and so faces nothing in particular.
  let private behindOf (w: World) (f: Entity) =
    f.Target
    |> Option.bind (fun id -> w.Entities |> List.tryFind (fun x -> x.Id = id))
    |> Option.map (fun x -> behind f x.Pos)

  /// Where a Rogue can stand behind a mob: the back tile, when it is floor and
  /// nobody else already holds it. None means there is nowhere to go — the mob's
  /// back is against a wall.
  let private behindSpot (w: World) (e: Entity) (f: Entity) =
    match behindOf w f with
    | Some b when b <> e.Pos && Grid.isFloor w.Grid b && not (SimState.claimedByOther w e.Id b) ->
      Some b
    | _ -> None

  /// Where the Tank should hold a mob it is already in melee with.
  ///
  /// The Tank is what chooses where a fight happens, so three problems land on it:
  ///  - the mob's back is against a wall, so the Rogue has nothing to Backstab;
  ///  - the mob is wedged where the casters have no line to it;
  ///  - another pack is close enough to be pulled in with it.
  /// The Tank orbits the mob to fix the first two and steps back to fix the third.
  ///
  /// It moves only for those reasons, never to shuffle a tile or two: the sort is
  /// coarse (a wall behind the mob, the pack distance in three-tile bands, then
  /// open floor) and the two coarse keys are compared on their own, so a Tank that
  /// already holds a good spot stays on it instead of orbiting forever.
  let private holdSpot (w: World) (m: Entity) (tank: Entity) (party: Entity list) =
    let backstabber =
      party |> List.exists (fun e -> e.Abilities |> List.exists (fun a -> a.Name = "Backstab"))

    // A caster behind a corner cannot help, and standing closer does not fix it:
    // the mob has to be pulled out of wherever it is hiding.
    let blind =
      party
      |> List.filter (fun e ->
        e.Abilities |> List.exists (fun a -> a.TargetKind = Foe && a.Range > 1))
      |> List.exists (fun e -> not (Path.lineOfSight w.Grid e.Pos m.Pos))

    let packGap (p: Pos) =
      w.Entities
      |> List.filter (fun e ->
        e.Faction = Hostile && e.Id <> m.Id && SimState.alive e && not e.Dormant)
      |> List.map (fun e -> Pos.chebyshev p e.Pos)
      |> List.sort
      |> List.tryHead
      |> Option.defaultValue 99

    let score (p: Pos) =
      let backBlocked = if Grid.isFloor w.Grid (behind m p) then 0 else 1

      let room =
        ring
        |> List.filter (fun (dx, dy) -> Grid.isFloor w.Grid (Pos.move dx dy p))
        |> List.length

      // Open floor is worth having (it is what gives the casters their line), but
      // it only breaks ties: the one thing that justifies walking is opening the
      // mob's back.
      (if backstabber then backBlocked else 0),
      -room,
      Pos.chebyshev tank.Pos p,
      p.Y,
      p.X

    let free (p: Pos) =
      Grid.isFloor w.Grid p && not (SimState.claimedByOther w tank.Id p)

    let candidates =
      ring |> List.map (fun (dx, dy) -> Pos.move dx dy m.Pos) |> List.filter free

    // The step that makes the mob move: it is out of melee from there, so it
    // follows, and it ends up on the tile the Tank just left — with its back to
    // the tile it came from, which is open floor. One step, not a retreat.
    let pullBack =
      let p = Pos.move (sign (tank.Pos.X - m.Pos.X)) (sign (tank.Pos.Y - m.Pos.Y)) tank.Pos
      if p <> tank.Pos && free p then Some p else None

    match candidates with
    | [] -> pullBack
    | cs ->
      let best = cs |> List.sortBy score |> List.head
      let coarse (p: Pos) =
        let a, _, _, _, _ = score p
        a

      // The hold has a second job when somebody needs the mob's back: stand where
      // it is open. A mob faces what it attacks, so the tile opposite the Tank is
      // the Rogue's, and a back against a wall is a Backstab that cannot happen.
      // The old hold score counts "out of melee" against a tile, which is exactly
      // the step that opens the back — so the tile that leaves the back walled is
      // scored out when somebody needs it and nobody else can fix it.
      let needs = backstabber || blind

      let backWalled (p: Pos) =
        needs && not (Grid.isFloor w.Grid (behind m p))

      let rank (p: Pos) = coarse p + (if backWalled p then 10_000 else 0)

      // Every free tile around it has a wall behind it, so the mob is wedged in a
      // corner or a dead end and there is no orbiting to be done. Only then is
      // making it move the answer — and only when somebody actually needs the
      // back (a Rogue) or the line (a caster), because retreating otherwise is
      // just kiting the Party into the next pack.
      let wedged =
        needs && List.forall (fun p -> not (Grid.isFloor w.Grid (behind m p))) cs

      if wedged then
        pullBack
      elif
        rank best < rank tank.Pos
        && packGap best >= packGap tank.Pos
        && Pos.chebyshev tank.Pos best <= 1
      then
        // Never orbit toward another pack: the walk that opens a back is not worth
        // a second group arriving, which is what a Tank walking sideways into an
        // un-pulled room buys. And never more than a sidestep either — the Tank's
        // own rotation is worth more than a long walk to a better tile.
        Some best
      else
        None

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

      // A Tank's job is the damage it stops, not the damage it deals. Disarm a
      // swing that would land on it, Shield Bash a caster before its spell
      // finishes, and only then start reducing armour.
      let pressured = SimState.healthPct t < 80 || (SimState.aliveHostiles w |> List.filter (fun h -> h.Engaged) |> List.length) > 1
      let caster =
        SimState.aliveHostiles w
        |> List.tryFind (fun h ->
          SimState.canReach 1 t h w
          && h.Casting |> Option.exists (fun c -> c.Ability.Interruptible))
      let mitigation =
        [ if pressured && has t "Disarm" && canUse t "Disarm" then yield "Disarm"
          // The shield's buttons. Rage does not yet come from damage dealt or
          // taken (see the Warrior pass), so a pool of 100 has to last the whole
          // fight: Shield Block and Shield Wall are held for a Tank that is
          // actually losing health, not spent as a rotation.
          if SimState.healthPct t < 60 && has t "Shield Block" && canUse t "Shield Block" then
            yield "Shield Block"
          if SimState.healthPct t < 30 && has t "Shield Wall" && canUse t "Shield Wall" then
            yield "Shield Wall" ]

      match thief with
      | Some h when has t "Taunt" && canUse t "Taunt" -> cmd at (UseAbility(t.Id, "Taunt", h.Id))
      | _ when not (SimState.withinRange 1 t f) -> cmd at (MoveTo(t.Id, f.Pos))
      | _ when caster.IsSome && has t "Shield Bash" && canUse t "Shield Bash" ->
        cmd at (UseAbility(t.Id, "Shield Bash", caster.Value.Id))
      | _ when not mitigation.IsEmpty -> cmd at (UseAbility(t.Id, mitigation.Head, f.Id))
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

    // Every heal-over-time this healer brings, not just the biggest: Renew,
    // Rejuvenation and Regrowth stack, and a Tank wearing all three is a Tank the
    // direct heals only have to top up.
    let hots =
      h.Abilities
      |> List.filter (fun a -> hasHot a && canUse h a.Name)
      |> List.sortByDescending healsOf

    let hot = List.tryHead hots

    /// The cheapest heal that still covers the hole. Dropping the biggest heal
    /// on a small hole is mana burnt as overheal, which is exactly why a real
    /// Priest downranks: Lesser Heal restores less and goes further.
    let directFor (deficit: int) =
      let affordable =
        h.Abilities
        |> List.filter (fun a -> hasHeal a && not (hasHot a) && canUse h a.Name)
        |> List.sortByDescending healsOf

      affordable
      |> List.filter (fun a -> healsOf a >= deficit)
      |> List.tryLast
      |> Option.orElse (List.tryHead affordable)

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

    // A HoT worth re-casting: none of that spell running, or one with under four
    // seconds left — early enough that the next one is up before this one lapses,
    // which is the difference between "on him most of the time" and a 43% uptime.
    let lapsedFor (t: Entity) (a: Ability) =
      Aura.healOverTimeRemainingFor a.Name t.Auras <= secTicks 4.0

    // The Tank first: he is the one taking swings, so his coverage is the group's.
    // The Tank first, then whoever is actually hurt: pre-emptively renewing an
    // untouched Dps only burns mana it will need for the Tank.
    let hotTarget =
      let byPriority =
        party
        |> List.filter (fun e -> e.Role = Some Tank || SimState.healthPct e < 90)
        |> List.sortBy (fun e ->
          (if e.Role = Some Tank then 0 else 1),
          SimState.healthPct e)

      hots
      |> List.tryPick (fun a ->
        byPriority
        |> List.tryFind (fun t -> lapsedFor t a)
        |> Option.map (fun t -> t, a))
      |> Option.map fst

    /// The spell to refresh `t` with: the biggest one it is missing or losing.
    let hotFor (t: Entity) = hots |> List.tryFind (lapsedFor t)

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
    | Some t, _ when projectedPct t < 65 -> order (directFor (t.MaxHealth - projected t)) t
    | _, Some t -> order (hotFor t) t
    | Some t, None -> order (directFor (t.MaxHealth - projected t)) t
    | None, None ->
      // Nobody is in trouble and nothing needs refreshing, so close the small
      // holes with the cheap heal instead of leaving them for the next burst —
      // but never pay a heal that is mostly overheal.
      match wounded |> List.tryHead with
      | Some t when projectedPct t < 90 ->
        let deficit = t.MaxHealth - projected t

        match directFor deficit with
        | Some a when healsOf a / 2 <= deficit -> order (Some a) t
        | _ -> []
      | _ -> []

  /// Expected damage of a finisher at the points the Rogue is holding. Eviscerate
  /// is linear in points, so the midpoint of its one-point pair plus the step per
  /// extra point is its average hit.
  let private finisherOf (e: Entity) (a: Ability) =
    a.Effects
    |> List.sumBy (function
      | Finisher(lo, hi, per) -> (lo + hi) / 2 + per * max 0 (e.ComboPoints - 1)
      | _ -> 0)

  let private isFinisher (a: Ability) =
    a.Effects |> List.exists (function Finisher _ -> true | _ -> false)

  /// Melee damage. Kick a cast it can reach, cash in the combo points, then the
  /// hardest strike it can pay for on the Party's target. Eviscerate's damage is
  /// linear in points, so four points already out-earns the strike that would
  /// build a fifth: it fires from four up, never below.
  let private meleeOrders (w: World) (e: Entity) (target: Entity option) (at: int<tick>) =
    // Backstab is a positional strike: it exists from behind the mob and nowhere
    // else, and a mob faces what it is attacking. Making that tile exist is the
    // Tank's job (`holdSpot`); not wasting 60 energy discovering it does not is
    // the Rogue's, so from the front he falls back to Sinister Strike.
    let inPosition (f: Entity) =
      ignore (f, behindOf w f)
      true

    let mine =
      e.Abilities
      |> List.filter (fun a ->
        a.TargetKind = Foe
        && a.Range <= 1
        && not (interrupts a)
        && (a.Name <> "Backstab"
            || (match target with
                | Some f -> inPosition f
                | None -> true)))

    let finisher =
      mine
      |> List.filter isFinisher
      |> List.filter (fun a -> canUse e a.Name && SimState.canAfford e a && e.ComboPoints >= 4)
      |> List.sortByDescending (finisherOf e)
      |> List.tryHead

    // Pool the resource for the strike worth casting instead of emptying the bar
    // the moment it clears the cheap one's cost: a Rogue who spends 45 as soon as
    // he has 45 will never reach the 60 his Backstab costs, and Backstab is worth
    // far more per point than Sinister Strike is. Below that he auto-attacks.
    // Rank on readiness alone, not on readiness-plus-affordability: `canUse` folds
    // the resource check in, and ranking through it would quietly drop Backstab
    // the moment it became unaffordable and hand the turn to the cheap strike.
    let strike =
      mine
      |> List.filter (fun a -> not (isFinisher a) && ready e a.Name)
      |> List.sortByDescending damageOf
      |> List.tryHead
      |> Option.filter (SimState.canAfford e)

    // Kicking a spell matters more than the target the Party is on: a healer
    // left to finish its cast undoes more damage than the Rogue adds. Reachable
    // casters come first, and Gouge — a four-second stun — is the backup when
    // Kick is down and the caster is a few tiles away.
    let caster =
      SimState.aliveHostiles w
      |> List.filter (fun h -> h.Casting |> Option.exists (fun c -> c.Ability.Interruptible))
      |> List.sortBy (fun h -> ((if SimState.canReach 1 e h w then 0 else 1), Pos.chebyshev e.Pos h.Pos))
      |> List.tryHead

    match caster with
    | Some c when has e "Kick" && canUse e "Kick" && SimState.withinRange 1 e c ->
      cmd at (UseAbility(e.Id, "Kick", c.Id))
    | Some c when has e "Gouge" && canUse e "Gouge" && SimState.withinRange 1 e c ->
      cmd at (UseAbility(e.Id, "Gouge", c.Id))
    | Some c when has e "Kick" && canUse e "Kick" && SimState.canReach 4 e c w ->
      cmd at (MoveTo(e.Id, SimState.approachTile e c.Pos w |> Option.defaultValue c.Pos))
    | _ ->
      match target with
      | None -> []
      | Some f ->
        // A Rogue already in melee but facing the wrong way walks round the mob
        // rather than striking from the front — but only while it has nothing to
        // spend. Backstab is worth 150% weapon damage for 60 energy against
        // Sinister Strike's 100% for 45, which is a 14% edge per point, not a
        // doubling: a Rogue who walks instead of striking loses more than the
        // position buys. Walking on the ticks it is pooling energy costs nothing.
        let backstab = e.Abilities |> List.tryFind (fun a -> a.Name = "Backstab")

        let circle =
          match backstab with
          | Some a when false &&
            SimState.withinRange 1 e f
            && not (inPosition f)
            && not (SimState.canAfford e a)
            ->
            behindSpot w e f
          | _ -> None

        match circle with
        | Some p -> cmd at (MoveTo(e.Id, p))
        | None ->
          match finisher |> Option.orElse strike with
          | Some a when SimState.withinRange 1 e f -> cmd at (UseAbility(e.Id, a.Name, f.Id))
          | _ ->
            // A Rogue with a Backstab goes round the back rather than into the
            // Tank's spot: the Tank is already holding the mob from the front.
            let stand =
              match behindSpot w e f with
              | Some p -> p
              | None -> f.Pos

            cmd at (MoveTo(e.Id, stand))
  /// Ranged damage. Mana is the caster's clock: while it has plenty it plays for
  /// damage per second of casting, and once it is low it plays for damage per
  /// point of mana instead. A free shot (a wand) is the fallback rather than the
  /// winner — ranked naively, "infinite efficiency" would mean it never casts.
  let private rangedOrders (w: World) (e: Entity) (target: Entity option) (at: int<tick>) =
    match target with
    | None -> []
    | Some f ->
      let perMana (a: Ability) = damageOf a * 1000 / max 1 a.ResourceCost
      let perCast (a: Ability) = damageOf a * 1000 / max 1 (int a.CastTicks)

      // A spell that lands is damage no heal answers in time. Counterspell it —
      // whatever it is casting, wherever it is standing — and only then dps.
      let counterspell =
        e.Abilities
        |> List.tryFind (fun a -> a.Effects |> List.contains Interrupt && a.Range > 1)

      // Leave the casts a melee member can kick to it: one interrupt per cast,
      // spent by whoever is standing closest.
      let kickable =
        SimState.aliveParty w
        |> List.filter (fun p ->
          p.Abilities
          |> List.exists (fun a -> a.Effects |> List.contains Interrupt && a.Range <= 1))

      let casting =
        SimState.aliveHostiles w
        |> List.filter (fun h -> h.Casting |> Option.exists (fun c -> c.Ability.Interruptible))
        |> List.filter (fun h -> not (kickable |> List.exists (fun p -> SimState.canReach 1 p h w)))
        |> List.filter (fun h -> counterspell |> Option.exists (fun a -> SimState.canReach a.Range e h w))
        |> List.sortBy (fun h -> Pos.chebyshev e.Pos h.Pos)
        |> List.tryHead

      let shots =
        e.Abilities
        |> List.filter (fun a -> a.TargetKind = Foe && a.Range > 1 && not (interrupts a))
        |> List.filter (fun a -> canUse e a.Name)

      let paid = shots |> List.filter (fun a -> a.ResourceCost > 0)
      let free = shots |> List.filter (fun a -> a.ResourceCost = 0)
      let lowMana = e.MaxResource > 0 && e.Resource * 5 < e.MaxResource * 2

      let shot =
        (if lowMana then paid |> List.sortByDescending perMana else paid |> List.sortByDescending perCast)
        |> List.tryHead
        |> Option.orElse (free |> List.sortByDescending damageOf |> List.tryHead)

      match counterspell, casting with
      | Some a, Some c when canUse e a.Name -> cmd at (UseAbility(e.Id, a.Name, c.Id))
      | _ ->
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

    let target = strikeTarget w tank hostiles
    let onTank = tankTarget w tank hostiles

    // Walking orders apply in and out of a fight. Out of one the Tank leads
    // toward the nearest hostile; in one it closes the last few tiles to the mob
    // it is actually fighting, because a Tank that stops three steps short is a
    // Tank whose Party fights at range and loses. Followers hold the leash
    // behind it either way: moving the Tank alone is what leaves the healers
    // twelve tiles back, out of range, while it dies to a pull they cannot see.
    let movement =
      match tank with
      | None -> []
      | Some t ->
        let spot =
          match onTank with
          | Some f -> Some f.Pos
          | None ->
            SimState.aliveHostiles w
            |> List.sortBy (fun h -> Pos.chebyshev t.Pos h.Pos)
            |> List.tryHead
            |> Option.map (fun h -> h.Pos)

        // Set when the Tank wants to close distance this tick. It may still fail to
        // get a move — the corridor can be full of its own Party — and that is
        // exactly when the Party has to stand aside.
        let mutable wantsToMove = false

        // In melee the Tank is not walking to the mob, it is choosing where to
        // hold it: the fight belongs on open floor, with the mob's back clear for
        // the Rogue and no other pack inside reach of the pull.
        let reposition =
          match onTank with
          | Some f when Pos.chebyshev t.Pos f.Pos <= 1 -> holdSpot w f t party
          | _ -> None

        [ match reposition with
          | Some p when p <> t.Pos ->
            wantsToMove <- true
            yield! cmd at (MoveTo(t.Id, p))
          | _ ->
          match spot with
          | Some p when Pos.chebyshev t.Pos p > 1 ->
            wantsToMove <- true
            // Where to walk to, in falling order of ambition: a free tile beside
            // the mob, the mob's own tile (A* exempts its goal, so a body standing
            // on it is not a wall), and finally the last tile of the corridor
            // before whoever is holding the door. That last case is the one that
            // matters: a mob parked in a doorway seals its room completely, and a
            // Tank that demands a route all the way in never leaves the room it
            // cleared. Walking up to the door is enough — the guard aggros from
            // six tiles and the fight that opens the room happens there.
            let taken n = SimState.claimedByOther w t.Id n
            let route to_ = (Path.astar w.Grid taken t.Pos to_).Path

            // Only plan when there is no plan. Three searches a tick, forever, is
            // the expensive way to walk a corridor — `stepToward` already keeps
            // the route and replans only when it runs out, and a failed search
            // expands the whole map, so retrying during the backoff turned a
            // stall into 30 s of CPU per room.
            //
            // `StallTicks > 0` used to be part of this condition and made the
            // backoff permanent: the countdown only ticks inside `stepToward`,
            // which only runs for an entity that was issued a move, so a stalled
            // mover stopped being issued moves and stalled forever.
            let aim =
              if t.Destination.IsSome || not t.Path.IsEmpty then
                None
              else
                match SimState.approachTile t p w with
                | Some a when not (List.isEmpty (route a)) -> Some a
                | _ ->
                  // Nobody alive in the way: walk to the last tile of the corridor
                  // before whoever is holding the door. The search run is over
                  // open floor, so it cannot fail the way a blocked one does.
                  let free = (Path.astar w.Grid (fun _ -> false) t.Pos p).Path

                  match free |> List.tryFindIndex taken with
                  | Some i when i > 0 -> Some free.[i - 1]
                  | _ -> Some p

            match aim with
            // `> 1` alone was a deadlock: with an ally one tile away the aim
            // truncates to a waypoint beside the Tank, the guard refuses the
            // order, and nobody in the corridor ever moves. Not being beside the
            // Mob is what asks for a move; the aim tile only has to differ.
            | Some a when a <> t.Pos && Pos.chebyshev t.Pos p > 1 -> yield! cmd at (MoveTo(t.Id, a))
            | _ -> ()
          | _ -> ()
          // The Party is its own traffic jam. A room two tiles wide has room for
          // one route, and the ally who happens to be standing on it has no
          // reason to move — so the Tank stalls on one Step the whole dungeon.
          // While the Tank is walking, anyone pressed against it steps aside.
          if t.Goal.IsSome || wantsToMove then
            for e in role Healer @ role Dps do
              if e.Id <> t.Id && Pos.chebyshev e.Pos t.Pos <= 1 then
                // Distance alone ties: two tiles either side of the Tank are both
                // one away, and an unstable sort then shuttles the ally between
                // them every tick while the Tank stays blocked. A fixed tie-break
                // makes the ally keep walking in one direction until it is clear.
                let aside =
                  Path.neighbours
                    (fun p -> Grid.isFloor w.Grid p && not (SimState.claimedByOther w e.Id p))
                    e.Pos
                  |> List.sortByDescending (fun p -> Pos.chebyshev p t.Pos, p.X, p.Y)
                  |> List.tryHead

                match aside with
                | Some p -> yield! cmd at (MoveTo(e.Id, p))
                | None -> ()
          // An ally that steps aside walks straight back if the leash is still
          // pulling it in, and the Tank's way out closes again — a two-tick
          // shimmy that never resolves. While the Tank holds a goal it cannot
          // route to, holding position beats following: the corridor empties, the
          // Tank finds its route, and the leash resumes on the next tick.
          let holding =
            (t.Goal.IsSome && t.Path.IsEmpty && t.Destination.IsNone)
            || (wantsToMove && t.Goal.IsNone)

          if not holding then
            for e in role Tank @ role Healer @ role Dps do
              if e.Id <> t.Id && Pos.chebyshev e.Pos t.Pos > 3 then
                // Re-aim only when the route on file no longer ends beside the Tank.
                // A follower re-issued every tick pays a whole-map search for every
                // tile the Tank walks, which is 100 ms a tick while the Party is
                // strung out down a corridor.
                let reaim =
                  match e.Goal with
                  | Some g -> Pos.chebyshev g t.Pos > 1
                  | None -> true

                if reaim then
                  yield! cmd at (MoveTo(e.Id, SimState.approachTile e t.Pos w |> Option.defaultValue t.Pos)) ]

    movement
    @ (if target.IsNone then
         []
       else
         [ yield! role Tank |> List.collect (fun e -> tankOrders w e onTank at)
           yield! role Healer
                  |> List.collect (fun e ->
                    match healerOrders w e party at with
                    // Nothing to heal: a healer with mana to spare adds damage
                    // rather than standing idle, but keeps a reserve for the heal
                    // that has to land next.
                    | [] when SimState.resourcePct e >= 55 -> rangedOrders w e target at
                    | orders -> orders)
           yield! role Dps |> List.filter melee |> List.collect (fun e -> meleeOrders w e target at)
           yield! role Dps |> List.filter ranged |> List.collect (fun e -> rangedOrders w e target at) ])

  let private actorOf (kind: CommandKind) =
    match kind with
    | UseAbility(actor, _, _) -> actor
    | MoveTo(actor, _) -> actor
    | Stop actor -> actor

  /// As `supplement`, for a player holding a member by hand: the listed members
  /// get no scripted order at all, so they do only what was clicked for them.
  let supplementExcept (manual: Set<EntityId>) (player: Command list) (w: World) : Command list =
    let driven = player |> List.map (fun c -> actorOf c.Kind) |> Set.ofList

    let scripted =
      decide w
      |> List.filter (fun c ->
        let actor = actorOf c.Kind
        not (driven.Contains actor) && not (manual.Contains actor))

    player @ scripted

  /// Fill in for whoever the player did not command this Tick. The player's orders
  /// always win: the script only drives the members left idle.
  let supplement (player: Command list) (w: World) : Command list =
    supplementExcept Set.empty player w
