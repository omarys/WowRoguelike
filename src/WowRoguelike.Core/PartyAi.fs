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

  /// Whether this body can actually land a damaging ability in the shape it is
  /// standing in. Both halves are the engine's own refusals, not the AI's opinion:
  /// `SimCombat` refuses a damaging cast outright in Tree of Life ("you cannot cast
  /// damaging spells") and refuses a spell whose required form the caster is not in.
  /// Hence a predicate: a kit full of nukes says nothing about what its holder may
  /// press this Tick. Read by the healer's damage fallback below and by the `shots`
  /// filter, so "can I hit with this" has one answer everywhere.
  let canLandDamage (e: Entity) (a: Ability) =
    Ability.isDamaging a
    && not (SimState.inStance e TreeForm)
    && (a.Stance |> Option.forall (SimState.inStance e))

  /// Whether a controlling ability can land at all, caster and target both — the
  /// engine's own refusals, mirrored so an opener is never spent on a cast that
  /// cannot land: a damaging spell is refused in Tree of Life and a spell whose
  /// Stance the caster is not in never resolves, while Polymorph and Sleep each
  /// refuse whole creature types (`polymorphable` / `sleepable`), which is why a
  /// cast at Verdan the Everliving reads as "is immune".
  let canControl (e: Entity) (a: Ability) (h: Entity) =
    (a.Stance |> Option.forall (SimState.inStance e))
    && (not (Ability.isDamaging a) || not (SimState.inStance e TreeForm))
    && a.Effects
       |> List.exists (function
         | Polymorph _ -> CreatureType.polymorphable h.CreatureType
         // Hibernate's `Sleep` is a Beast/Dragonkin spell in the source and the
         // engine's `sleepable` set is wider (Humanoid too), so the narrower
         // vanilla rule is applied here at booking time.
         | Sleep _ ->
             (if a.Name = "Hibernate" then
                h.CreatureType = CreatureType.Beast
                || h.CreatureType = CreatureType.Dragonkin
              else
                CreatureType.sleepable h.CreatureType)
         | Root _ -> true
         | Cripple _ -> true
         | _ -> false)

  /// Whether anything in the kit can be pressed as damage right now. A Tree of Life
  /// Druid answers no — the form is where its healing lives and the engine will not
  /// let it nuke — so a healer with mana to spare has nothing to spend it on rather
  /// than an order per Tick that is thrown away.
  let canDamageNow (e: Entity) = e.Abilities |> List.exists (canLandDamage e)

  /// How far from the Tank the Party will pull. Anything further is a pull the
  /// Tank walks into alone, with the healers left behind out of range — which is
  /// how a scripted Party wipes to one boss room.
  let private leash = 8

  /// The ability that puts the holder in `s`, if the kit carries one. A shift is
  /// one action of the same kind as any other, so this is all a shape change is.
  let private shiftTo (e: Entity) (s: Stance) =
    e.Abilities
    |> List.tryFind (fun a ->
      a.Effects
      |> List.exists (function
        | TakeStance f -> f = s
        | _ -> false))

  /// The form a body goes back to when it has no reason to be in another: the
  /// first form the kit offers, because a kit lists its own shape before the
  /// shift back out of it (`Content`'s Druid kits end `[ treeOfLife; humanForm ]`).
  let private homeForm (e: Entity) =
    e.Abilities
    |> List.tryPick (fun a ->
      a.Effects
      |> List.tryPick (function
        | TakeStance f -> Some f
        | _ -> None))

  let private cmd (at: int<tick>) (kind: CommandKind) = [ { ApplyAt = at; Kind = kind } ]

  /// Anything that leaves the caster unable to finish its spell is an interrupt.
  /// The engine already cancels the cast when a `Stun`, a `Sleep` or a `Polymorph`
  /// lands (`SimCombat.fs`, the three branches that set `Casting = None`), so a
  /// Hammer of Justice or a Gouge is a kick that happens to be written as control.
  /// Reading only `Interrupt` here meant the party's control buttons were spent on
  /// the nearest body while a Chaplain healed through them.
  let private breaksCast =
    function
    | Interrupt
    | Stun _
    | Sleep _
    | Polymorph _ -> true
    | _ -> false

  let private interrupts =
    fun (a: Ability) -> a.Effects |> List.exists breaksCast

  /// A kick is a damage button spent on nothing else, so it pays only when the
  /// cast it stops is worth more than the damage given up. Measured on the ladder:
  /// breaking *every* interruptible cast cost the library wing 5-8% of its clear
  /// time, because a Gouge (no damage) or a Shield Bash replacing a Sinister Strike
  /// or a Shield Slam to stop a bolt a 3050-health Warrior heals back is a loss.
  /// So the bar is a quarter of the victim's health, a cast long enough that the
  /// hit is big by construction, or a heal — the thing a player actually kicks.
  let private worthBreaking (w: World) (c: Cast) =
    let heals =
      c.Ability.Effects
      |> List.exists (function
        | Heal _
        | HealOverTime _ -> true
        | _ -> false)

    let lethal =
      match w.Entities |> List.tryFind (fun e -> e.Id = c.Target) with
      | Some victim ->
        c.Ability.Effects
        |> List.exists (function
          | SpellDamage(damage, _, _) -> damage * 4 >= victim.MaxHealth
          | _ -> false)
      | None -> false

    heals || lethal || c.Ability.CastTicks >= secTicks 3.0

  /// Interrupts are scarce and shared, and the Party is assumed to be talking: each
  /// caster caught in the middle of a spell is assigned exactly one interrupter, so
  /// two members never spend a kick on the same cast and nobody holds back waiting for
  /// permission. The assignment is computed from the World rather than passed as a
  /// message — every member reads the same World and so reaches the same answer, which
  /// makes it a consensus that cannot drift out of date.
  ///
  /// The cheapest kick that can actually land takes the job: a melee interrupt is back
  /// in ten seconds where a Counterspell is back in twenty-four, so a sword already in
  /// range keeps the long cooldown in reserve. Ties go to the closer member and then to
  /// the lower id, so the same cast gets the same answer on the next Tick.
  let private interruptAssignments (w: World) =
    SimState.aliveHostiles w
    |> List.filter (fun h ->
      h.Casting
      |> Option.exists (fun c -> c.Ability.Interruptible && worthBreaking w c))
    |> List.choose (fun h ->
      SimState.aliveParty w
      |> List.choose (fun m ->
        m.Abilities
        |> List.filter interrupts
        |> List.filter (fun a -> canUse m a.Name && SimState.canReach a.Range m h w)
        |> List.sortBy (fun a -> ((if a.Range <= 1 then 0 else 1), a.CooldownTicks))
        |> List.tryHead
        |> Option.map (fun a -> ((if a.Range <= 1 then 0 else 1), Pos.chebyshev m.Pos h.Pos, m.Id)))
      |> List.sort
      |> List.tryHead
      |> Option.map (fun (_, _, id) -> h.Id, id))
    |> Map.ofList

  /// The hostile ids this member is the agreed interrupter for. Evaluated once per
  /// call, because the assignment walks every pair: asking it inside a `List.filter`
  /// predicate recomputes the whole thing once per mob.
  let private interruptTargets (w: World) (m: Entity) : Set<EntityId> =
    interruptAssignments w
    |> Map.toList
    |> List.choose (fun (hostile, interrupter) -> if interrupter = m.Id then Some hostile else None)
    |> Set.ofList

  /// What this member owes the Party: the kick it should spend and the caster it is
  /// the assigned interrupter for, if any.
  ///
  /// This exists because an assignment that no lane reads is not a delay, it is a
  /// **dropped** interrupt. `interruptAssignments` gives each cast to exactly one
  /// member, so a lane that ignores its assignment does not hand the job back to the
  /// Warrior — the others were told it was covered. That is what happened to the
  /// Shaman: the healer seat never read `interruptTargets`, and Earth Shock's 6 s
  /// cooldown beat the Mage's 24 s Counterspell in the assignment order, so the
  /// shaman won casts it then never kicked (`sim shmana 30`: zero Earth Shock casts,
  /// `.pi/research/octowow/shaman-damage-and-interrupts.md` §1-2). The same hole
  /// swallowed a Paladin's Hammer of Justice, which `breaksCast` reads as a kick too.
  ///
  /// Every lane that can interrupt answers with this function, and
  /// `a cast the party holds a kick for is broken exactly once` asserts the
  /// contract over the whole roster — the class that lands next cannot silently
  /// reintroduce the hole, because the test is written against `decide`, not against
  /// a lane.
  let private assignedKick (w: World) (m: Entity) : (Ability * EntityId) option =
    let mine = interruptTargets w m

    SimState.aliveHostiles w
    |> List.filter (fun h -> h.Casting |> Option.exists (fun c -> c.Ability.Interruptible))
    |> List.filter (fun h -> Set.contains h.Id mine)
    |> List.sortBy (fun h -> Pos.chebyshev m.Pos h.Pos)
    |> List.tryPick (fun h ->
      m.Abilities
      |> List.filter interrupts
      // A leap is not a kick. `Charge` and `Intercept` carry a `Stun`, so they read
      // as interrupts and would win assignments a real kick should take — but both
      // move the kicker to the target, and a Tank that arrives ahead of its healers
      // is the geometry the Intercept note in `tankOrders` measures against
      // (7 of 12 against 10 without). Feral Charge is modeled without its leap, so
      // it stays a kick; the Warrior's Shield Bash was and remains its only one.
      |> List.filter (fun a ->
        a.Effects
        |> List.exists (function Charge | Intercept -> true | _ -> false)
        |> not)
      |> List.filter (fun a -> canUse m a.Name && SimState.canReach a.Range m h w)
      |> List.sortBy (fun a -> ((if a.Range <= 1 then 0 else 1), a.CooldownTicks))
      |> List.tryHead
      |> Option.map (fun a -> a, h.Id))

  /// Roughly how hard an ability hits, for ordering a kit the way a player would
  /// rather than by the order it happens to sit in the list. A sweep is scored per
  /// body, not per cast: every ranking that asks "how hard" here is asking about
  /// the target in front of the actor, and a three-target row scored at its best
  /// case would out-bid a real strike every time. What a sweep actually catches is
  /// `tankOrders`'s question, and it is asked there.
  let private damageOf (a: Ability) =
    a.Effects
    |> List.sumBy (function
      | WeaponDamage(multiplierBp, bonus) -> 30 * multiplierBp / 10000 + bonus
      | MultiStrike _ -> 30
      | Damage(_, mx) -> mx
      | SpellDamage(_, mx, _) -> mx
      | _ -> 0)

  /// What one application of a damage-over-time is worth over its whole duration.
  /// `DamageOverTime` carries a per-second figure (`Content.fs`: "Rank 3's 37 every
  /// 3 s is 12 a second"), so the duration in seconds is the multiplier — the same
  /// arithmetic `HealOverTime` gets at the hot path below.
  ///
  /// It is a *total*, not a rate, and that is the point: a dot is pressed once and
  /// paid for once, so what a second application buys is the whole remaining
  /// duration. `damageOf` cannot say any of this — it reads direct hits only, which
  /// is why Corruption scores 0 and the `damageOf > 0` filter drops it from `shots`
  /// before ranking ever sees it.
  let private dotOf (a: Ability) =
    a.Effects
    |> List.sumBy (function
      | DamageOverTime(duration, perTick) -> (duration / secTicks 1.0) * perTick
      | _ -> 0)

  /// The first ability in a kit whose effects satisfy `test`. Rules ask what an
  /// ability *does*, not what it is called: a bear's Growl is the Warrior's Taunt
  /// under a feral name and its Maul is Heroic Strike, and the effect is the only
  /// thing that says so.
  let private abilityWhere (e: Entity) (test: Effect -> bool) =
    e.Abilities |> List.tryFind (fun a -> a.Effects |> List.exists test)

  /// A kit's combo-point builders, hardest first. There are two in a kit and the
  /// content carries no positional flag, so the harder one is read as the strike
  /// that needs the mob's back: Backstab is 150% weapon damage against Sinister
  /// Strike's 100%, and a Cat's Shred against its Claw is the same pair renamed.
  /// No cooldown, because energy is the whole limiter — which is what leaves the
  /// stun out of the pair. Ambush and Ravage are out by name because nothing in
  /// their effects says "stealth": they are stealth openers, and offering them here
  /// would spend them as though stealth were free — and worse, Ravage's 350% would
  /// become the `positional` strike and demote Shred to a fallback.
  let private builders (e: Entity) =
    e.Abilities
    |> List.filter (fun a ->
      a.CooldownTicks = ticks 0
      && a.TargetKind = Foe
      && a.Name <> "Ambush"
      && a.Name <> "Ravage"
      && a.Effects |> List.exists (function ComboPoint _ -> true | _ -> false))
    |> List.sortByDescending damageOf

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
    // Whoever brings a builder that wants the mob's back, which is a fact about the
    // kit's numbers rather than its names (`builders`).
    let backstabber = party |> List.exists (fun e -> not (List.isEmpty (builders e)))

    // A caster behind a corner cannot help, and standing closer does not fix it:
    // the mob has to be pulled out of wherever it is hiding.
    let blind =
      party
      |> List.filter (fun e ->
        e.Abilities |> List.exists (fun a -> a.TargetKind = Foe && a.Range > 1))
      |> List.exists (fun e -> not (Path.lineOfSight w.Grid e.Pos m.Pos))

    // `blind` above asks the *damage* casters whether they can see the mob. It says
    // nothing about the healers, and `SimState.canReach` is within-range *and*
    // line-of-sight — so a Tank its healers cannot see takes no healing at all, at
    // any distance. That is how a party watches its Tank die with both healers at
    // full mana: the Tank held a mob in a room corner, both healers stood in the
    // doorway behind the door jamb, and every heal was refused for want of a ray.
    let healers =
      party
      |> List.filter (fun e ->
        e.Id <> tank.Id
        && e.Abilities
           |> List.exists (fun a ->
             a.Effects |> List.exists (function Heal _ | HealOverTime _ -> true | _ -> false)))

    let seenByHealers (p: Pos) = healers |> List.forall (fun e -> Path.lineOfSight w.Grid e.Pos p)

    // The other half of the same question: a damage dealer with no free tile beside
    // the mob cannot help whatever the Tank does about the casters' line. Folded into
    // `needs` rather than handled separately, because the answer is the same one —
    // make the mob move.
    let meleeDps =
      party
      |> List.filter (fun e ->
        e.Id <> tank.Id
        && e.Abilities |> List.exists (fun a -> a.TargetKind = Foe && a.Range <= 1))

    let noRoom = meleeDps |> List.exists (fun e -> (SimState.approachTile e m.Pos w).IsNone)

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

    // A Tank with no healer sight line is out of the fight whether or not it is
    // holding the mob: walk to the nearest tile its healers *can* see. The condition
    // clears itself the moment the line opens, so it cannot oscillate, and the mob
    // follows because the Tank is what holds its threat. Three tiles, not thirty:
    // the job is to step out of the corner, not to kite the pack.
    let rescue =
      if List.isEmpty healers || seenByHealers tank.Pos then
        None
      else
        [ for dx in -3 .. 3 do
            for dy in -3 .. 3 do
              yield Pos.move dx dy tank.Pos ]
        |> List.filter (fun p -> p <> tank.Pos && free p && seenByHealers p)
        |> List.sortBy (fun p -> Pos.chebyshev p m.Pos, Pos.chebyshev p tank.Pos, p.Y, p.X)
        |> List.tryHead

    match rescue with
    | Some p -> Some p
    | None ->

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
      let needs = backstabber || blind || noRoom

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

      // The Tank pull-back (the operator's corridor case, composed with the
      // flank logic): a builder committed to this fight has no safe flank tile —
      // the mob's back is against a wall or a tile somebody already holds — so
      // no orbit opens it and the one-step `pullBack` only moves the problem.
      // The Tank walks the fight back two or three tiles along its approach,
      // to the nearest open floor away from the mob, dragging the pack by its
      // threat: the mob re-faces the Tank as it follows and its back opens onto
      // the room mouth, which is where the flank finally exists.
      //
      // Two bounds keep it honest. `openRoom` >= 5 means the pull only ever
      // lands the fight on room-shaped floor — a mid-corridor Tank finds no
      // candidate at all, so this never kites the pack down a corridor — and
      // `packGap` never drags it toward the next un-pulled group. The
      // anti-oscillation half is the engine's own commitment: a `MoveTo` sets
      // `Goal`, the walk runs off `Path` without a fresh order, and the
      // candidate is a pure function of the world, so re-deciding on a later
      // tick either re-issues the same goal or the flank has opened and
      // `stuckFlanker` has cleared — there is no tile to ping-pong between.
      let stuckFlanker =
        meleeDps
        |> List.exists (fun e ->
          not (List.isEmpty (builders e))
          && Pos.chebyshev e.Pos m.Pos <= 2
          && (match behindOf w m with
              | Some b -> b <> e.Pos && (behindSpot w e m).IsNone
              | None -> false))

      let openRoom (p: Pos) =
        ring |> List.filter (fun (dx, dy) -> Grid.isFloor w.Grid (Pos.move dx dy p)) |> List.length

      let pullBack3 =
        let dx = sign (tank.Pos.X - m.Pos.X)
        let dy = sign (tank.Pos.Y - m.Pos.Y)
        let along k = Pos.move (dx * k) (dy * k) tank.Pos
        [ 2; 3 ]
        |> List.tryFind (fun k ->
          let goal = along k
          goal <> tank.Pos
          && List.forall (fun k' -> Grid.isFloor w.Grid (along k')) [ 1 .. k ]
          && free goal
          && openRoom goal >= 5)
        |> Option.map along

      // A `stuckFlanker` that cannot find a pull falls through to the orbit
      // below instead of dead-ending: in a corridor the pull has no candidate
      // (every landing tile is wall or packed), but a one-tile sidestep still
      // turns the mob enough to open its back — the same answer the orbit
      // branch already gives, and the one the corridor fixture needs.
      let pulledBack =
        if wedged then
          match pullBack3 with
          | Some p when packGap p >= packGap tank.Pos -> Some p
          | _ -> pullBack
        elif stuckFlanker then
          match pullBack3 with
          | Some p when packGap p >= packGap tank.Pos -> Some p
          | _ -> None
        else
          None

      match pulledBack with
      | Some p -> Some p
      | None ->
        if
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
      // swing that would land on it, kick a caster before its spell finishes —
      // with whatever the kit's cheapest ready interrupt is, not a named button,
      // so the bear's Bash and Feral Charge ride the same branch the Warrior's
      // Shield Bash did — and only then start reducing armour.
      let pressured = SimState.healthPct t < 80 || (SimState.aliveHostiles w |> List.filter (fun h -> h.Engaged) |> List.length) > 1
      let kick = assignedKick w t
      let mitigation =
        [ if pressured && has t "Disarm" && canUse t "Disarm" then yield "Disarm"
          // The shield's buttons. Rage is earned from damage dealt and taken now,
          // but a Warrior cannot afford all of it at once, so Shield Block and
          // Shield Wall stay held for a Tank that is actually losing health rather
          // than spent as a rotation.
          if SimState.healthPct t < 60 && has t "Shield Block" && canUse t "Shield Block" then
            yield "Shield Block"
          if SimState.healthPct t < 30 && has t "Shield Wall" && canUse t "Shield Wall" then
            yield "Shield Wall" ]

      // The tank's two had-to-be-named buttons, read off the effects instead: a
      // bear's Growl is a `Taunt` and its Maul is a paid `WeaponDamage` strike,
      // exactly as the Warrior's are, and the rank tables are the same ones.
      let taunt = abilityWhere t (function Taunt -> true | _ -> false)

      // A sweep is in the bear's kit and out of its rotation: measured, not
      // assumed. Two conditions were tried on level 60 over twelve seeds, and both
      // left 2 of 12 runs uncleared (seeds 4 and 6, stopped at the tick cap with
      // the pack reset to full):
      //
      //  - press on the body count alone (`MultiStrike`'s three, all engaged); and
      //  - press only when no body in reach is held by the party's own control
      //    (`Aura.endsOnHostileAction`, so nothing sweeps into a polymorph).
      //
      // The second is the interesting failure, and the log of seed 4 names it: on
      // one Tick the Mage polymorphs a Ravager and the bear's sweep lands on it,
      // because both orders are decided from the same pre-Tick world — one Tick of
      // lag is enough to undo the party's own control. A Tank that breaks its
      // party's sheep is worse than a Tank that does not sweep, so the button waits
      // for the day the decision has that lookahead.
      let dump =
        abilityWhere t (function WeaponDamage _ -> true | _ -> false)
        |> Option.filter (fun a -> a.ResourceCost > 0)

      // The operator's tank rotation, read off **effects** rather than names, so the
      // same five orders serve the Warrior and the bear (`.pi/research/octowow/
      // druid-feral.md`, §9): a Rage opener is an `Energize`, an armour debuff is a
      // `Sunder`, a threat strike is a `WeaponDamage` that also states a `Threat`,
      // and the weave is a `Lacerate`. Reading names instead would mean a second
      // copy of this function the day the second tank arrives.
      let affordable pred =
        abilityWhere t pred |> Option.filter (fun a -> canUse t a.Name)

      let rageOpener = affordable (function Energize _ -> true | _ -> false)
      let armorDebuff = affordable (function Sunder _ -> true | _ -> false)

      let threatStrike =
        t.Abilities
        |> List.tryFind (fun a ->
          canUse t a.Name
          && a.Effects |> List.exists (function Threat _ -> true | _ -> false)
          && a.Effects |> List.exists (function WeaponDamage _ -> true | _ -> false))

      let weave = affordable (function Lacerate _ -> true | _ -> false)

      // "Pop Enrage immediately on pulls to cover the initial low-rage window":
      // the threshold is what the button grants, so it is pressed when it would
      // refill an empty bar rather than at 90 Rage.
      let lowRage = t.Resource < 20

      let standingLacerates =
        match target with
        | Some body -> Aura.lacerateStacks body.Auras
        | None -> 0

      match thief, taunt with
      | Some h, Some a when canUse t a.Name -> cmd at (UseAbility(t.Id, a.Name, h.Id))
      // Intercept is in the kit and stays out of the rotation: measured, it reads 7
      // of 12 in `library` against 10 without it. The leaps themselves are not the
      // problem — the geometry they create is. A Tank that arrives eight tiles ahead
      // of its healers is a Tank fighting alone, and the straggler gate then sees a
      // party that has to be waited for and parks the pull. The gap closer earns its
      // rage the day the party moves with the Tank; until then walking is faster.
      | _ when not (SimState.withinRange 1 t f) -> cmd at (MoveTo(t.Id, f.Pos))
      | _ when kick.IsSome ->
        let ability, hostile = kick.Value
        cmd at (UseAbility(t.Id, ability.Name, hostile))
      | _ when not mitigation.IsEmpty ->
        let name = mitigation.Head

        // Shield Block and Shield Wall are `Self` abilities, and ordering one at
        // the foe is refused every Tick: "Shield Block invalid or out of range",
        // which is what the tank's mitigation had been doing — two dead buttons,
        // a full log of refusals, and a Warrior who died with Shield Wall unused.
        // `mitigation` holds names, so the kind is looked up here.
        let isSelf =
          t.Abilities |> List.exists (fun a -> a.Name = name && a.TargetKind = Self)

        cmd at (UseAbility(t.Id, name, (if isSelf then t.Id else f.Id)))
      // The pull's first free Rage, then the armour tag, then the threat strike on
      // cooldown — which is the operator's single-target order and the AoE one at
      // once, since a pull is one body until the pack arrives.
      | _ when rageOpener.IsSome && lowRage ->
        cmd at (UseAbility(t.Id, rageOpener.Value.Name, t.Id))
      | _ when armorDebuff.IsSome && not sundered ->
        cmd at (UseAbility(t.Id, armorDebuff.Value.Name, f.Id))
      | _ when threatStrike.IsSome ->
        cmd at (UseAbility(t.Id, threatStrike.Value.Name, f.Id))
      | _ when weave.IsSome && standingLacerates < 5 ->
        cmd at (UseAbility(t.Id, weave.Value.Name, f.Id))
      // Sunder Armor is a Warrior's only armour button and the bear's Faerie Fire is
      // too, so a Tank that already applied it has nothing left but the dump. The
      // armour debuff is re-applied above only when the target has lost it.
      | _ when has t "Sunder Armor" && canUse t "Sunder Armor" ->
        cmd at (UseAbility(t.Id, "Sunder Armor", f.Id))
      | _ when dump.IsSome && canUse t dump.Value.Name ->
        cmd at (UseAbility(t.Id, dump.Value.Name, f.Id))
      | _ -> []

  /// The two health bars the healer's decision procedure is built on, named once so
  /// that anything *measuring* a healer cannot drift from the thing it measures.
  /// `healerTroublePct` is the projected bar below which the calm branches stop
  /// believing the party is fine; `healerFastPct` is the trip point for the instant
  /// heals and the talent cooldowns, one cast-time of health above death rather than
  /// a panic threshold; `healerUrgentPct` is the projected bar at which a spike
  /// outranks a refresh.
  let healerTroublePct = 90
  let healerFastPct = 60
  let healerUrgentPct = 65

  /// Healer. A heal-over-time first: it is the cheapest health per point of mana
  /// and it keeps working while the next cast happens. It is refreshed only as it
  /// lapses, and the direct heal starts as soon as the deficit outruns what the
  /// HoTs in flight will give back — a three-second cast that begins when the bar
  /// is empty is a three-second cast that began too late. The talent cooldowns go
  /// in front of that direct heal below 60%, which is one cast-time of health above
  /// death rather than a panic threshold.
  let private healerOrders (w: World) (h: Entity) (party: Entity list) (at: int<tick>) =
    // The need-pool, not the roster: everything below — who is wounded, who is
    // pre-emptively covered, who a direct heal may land on — is asked of this list,
    // and a demon is not a body a healer's mana can do anything useful for
    // (`SimState.healableParty`). Filtered once here rather than at each of the six
    // places that walk the pool, so no branch can quietly reintroduce it.
    let party = party |> List.filter (SimState.isSummonedDemon >> not)
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

    /// A HoT that also heals up front — Regrowth, and nothing else in the kits.
    /// It is the one HoT that must never be re-applied while its own buff is still
    /// running: the direct half would land on health the ticking half has already
    /// promised, which is overheal bought at 396 a press.
    let isTwoPart (a: Ability) =
      hasHot a && a.Effects |> List.exists (function Heal _ -> true | _ -> false)

    /// The direct half of a two-part HoT, in health — Regrowth's up-front heal and
    /// nothing else. Zero for a pure HoT, which is what lets the press gate below
    /// read as a shape test rather than a Druid test.
    let upFrontOf (a: Ability) =
      a.Effects |> List.sumBy (function Heal(_, mx) -> mx | _ -> 0)

    // Biggest first, then the first one this healer can actually pay for.
    let bestOf (keep: Ability -> bool) =
      h.Abilities
      |> List.filter keep
      |> List.sortByDescending healsOf
      |> List.tryFind (fun a -> canUse h a.Name)

    // Every heal-over-time this healer brings, not just the biggest: Renew,
    // Rejuvenation and Regrowth stack, and a Tank wearing all three is a Tank the
    // direct heals only have to top up.
    //
    // Cheapest first, and that is the order the layering happens in. Rejuvenation is
    // 160 and goes up pre-emptively the moment the Tank is taking swings; Regrowth is
    // 396 and only goes on a body already wearing Rejuvenation. Sorted the other way
    // — biggest first, which is what this was — both `hot` and `hotFor` reach for
    // Regrowth the instant it lapses, whatever the size of the hole, and 43% of the
    // Druid's whole mana budget went out through the expensive one.
    let hots =
      h.Abilities
      |> List.filter (fun a -> hasHot a && canUse h a.Name)
      |> List.sortBy healsOf

    let hot = List.tryHead hots

    /// Vanilla's five-second rule starts when a spell is *paid for*, so a cast does
    /// not cost its mana alone: it also suspends the Spirit regeneration for the next
    /// five seconds. Only the `CombatRegenBp` trickle survives — 15% for a Druid with
    /// no Meditation — which on this one is 46 a two-second tick dropping to 6. So one
    /// cast out of a *clear* window forfeits about ninety-seven mana on top of the
    /// mana it costs: a third of a top-rank Healing Touch, spent on nothing.
    ///
    /// A healer already inside the window pays none of it — the window is running
    /// whatever it does next — and that asymmetry is the whole of downranking. A healer
    /// that is continuously busy should cast the cheapest thing that closes the hole,
    /// because its next window is already lost. A healer whose hands have been still
    /// should not nibble at sixty points of damage: it should either land one heal
    /// large enough to be worth breaking the window, or keep still and let the Spirit
    /// rate refill the pool.
    let fsrActive =
      h.Auras |> List.exists (function RecentlyCast _ -> true | _ -> false)

    let fsrForfeit =
      if fsrActive then
        0
      else
        let full = float h.ResourceRegenPerTick
        let trickle = full * float h.CombatRegenBp / 10000.0
        int ((full - trickle) * float Content.fiveSecondRule / float Content.manaRegenTicks)

    /// Whether an instant empower lets `a` be cast from a form it names and the
    /// healer is not in. The engine's form gate reads exactly this — `Instant` and
    /// `Empower.appliesTo` — so the AI asks the same question in the same terms
    /// rather than keeping its own list of which spells a buff opens.
    let formWaived (a: Ability) =
      let held = Aura.empower h.Auras
      held.Instant && Empower.appliesTo a.Name held

    /// How much of the pool this heal actually asks for. Dropping the biggest heal on
    /// a small hole is mana burnt as overheal, which is exactly why a real Priest
    /// downranks: Lesser Heal restores less and goes further — and it is the same
    /// arithmetic that says a *bigger* rank wins the moment the cheap one would have to
    /// be cast twice, because the first cast has already opened the window and the
    /// second owes its mana outright. Charging every cast the hole needs, rather than
    /// only the first, is what stops "keep casting the cheap rank" from looking free.
    let directFor (deficit: int) =
      let byHeal (l: Ability list) = l |> List.sortByDescending healsOf

      let covers (a: Ability) = healsOf a >= deficit

      let affordable =
        h.Abilities
        |> List.filter (fun a -> hasHeal a && not (hasHot a) && canUse h a.Name)
        |> byHeal

      // The shape the healer is already in comes first: Tree of Life is where the
      // resto spec lives and it is the shape that forbids Healing Touch, so the
      // shift out is spent only when the form's own heals cannot fill the hole.
      // Picking on size alone would leave the Druid parked in human form for the
      // whole fight and the form the spec is named for would never be used.
      //
      // `formWaived` is the engine's own exception read back: an instant empower
      // waives the form requirement, so on Nature's Swiftness a Healing Touch counts
      // as in-form from inside the tree. Without it the operator's house rule would
      // be unreachable from the AI — Nature's Swiftness would be pressed and no heal
      // would follow, because the one spell it exists to waive had been filtered out
      // a Tick earlier, and the tree would be left for a heal that was already legal.
      let inForm =
        affordable
        |> List.filter (fun a ->
          (a.Stance |> Option.forall (SimState.inStance h)) || formWaived a)

      let costToClose (a: Ability) =
        let heal = max 1 (healsOf a)
        let casts = (deficit + heal - 1) / heal
        (casts * a.ResourceCost) + fsrForfeit, casts

      // Cheapest hole closed, then fewest casts, then the smaller heal. The two
      // tie-breaks matter: at equal cost one big cast beats two small ones because it
      // hands the Tick back to the healer, and at equal everything else there is no
      // reason to bank heal that lands as overheal.
      let best (l: Ability list) =
        l
        |> List.sortBy (fun a ->
          let cost, casts = costToClose a
          cost, casts, healsOf a)
        |> List.tryHead

      match inForm |> List.filter covers with
      | _ :: _ -> best inForm
      | [] -> best affordable

    let projected (t: Entity) = t.Health + Aura.healOverTimePending t.Auras
    let projectedPct (t: Entity) = projected t * 100 / t.MaxHealth

    /// The operator's Paladin heal policy (m02387), one formula per target and
    /// no tank special-case: Holy Light when the deficit is a large share of what
    /// Holy Light would restore — 80% for a body one enemy is hitting, a quarter
    /// less per extra enemy engaged on it, 5% at the floor — Flash of Light for
    /// everybody else. The "save Flash for minor damage" half falls out of the
    /// arithmetic: the mage nobody is hitting needs an 80%-of-Holy-Light hole
    /// before the 2.5-second cast is worth it, while the tank in the graveyard's
    /// four-mob pull qualifies at the 5% floor almost immediately — which is the
    /// pull the policy exists for (`.pi/research/paladin-graveyard-wipe.md`: 16
    /// of 17 first deaths had a ~123-point Flash in flight; ~94 Flash to ~2 Holy
    /// Light a run).
    ///
    /// The threshold is measured against the deterministic reference the cast
    /// path would produce — SimCombat's heal arithmetic with the roll pinned to the
    /// rank's HIGH bound: the flat bonus healing folded in, then the talent
    /// multiplier (Healing Light 3/3, HealBp 1200). No Tree of Life term and no
    /// Healing Way term: both are other classes' multipliers, and no party this
    /// content fields puts one beside a Paladin.
    let paladinDirect (t: Entity) (deficit: int) =
      let named (n: string) = h.Abilities |> List.tryFind (fun a -> a.Name = n)

      match named "Holy Light", named "Flash of Light" with
      | Some hl, Some fol ->
        // "Actively engaged": an enemy swinging at this target, which in this
        // model is a mob that is Engaged and has the target as its Target.
        let n =
          SimState.aliveHostiles w
          |> List.filter (fun m -> m.Engaged && m.Target = Some t.Id)
          |> List.length
          |> max 1

        let shareBp = max (8000 - 2500 * (n - 1)) 500
        let hlRef = (healsOf hl + h.BonusHealing) * Talents.healBp hl.Name h.Talents / 10000

        let pick = if deficit * 10000 >= shareBp * hlRef then hl else fol

        // The policy names the spell; affordability still decides. A Holy Light
        // the pool cannot pay falls through to the shared cost-to-close pick,
        // which finds the cheaper heal on its own terms.
        if canUse h pick.Name then Some pick else directFor deficit
      | _ -> directFor deficit

    /// The instant heal on a cooldown — Holy Shock, and nothing else in any kit:
    /// every other cooldown button is an empower or an aura-eater, not a heal
    /// (`cooldownFor` owns those). Pressed only when the deficit will absorb the
    /// whole talented heal, so a 30-second cooldown is never spent on overheal —
    /// and the burst window gets an instant rather than the 1.5-second Flash it
    /// used to die waiting on.
    let panicFor (t: Entity) =
      let deficit = t.MaxHealth - projected t

      h.Abilities
      |> List.tryFind (fun a ->
        canUse h a.Name
        && a.CastTicks <= ticks 0
        && a.CooldownTicks > ticks 0
        // A direct heal, not a HoT: the button must land whole, this Tick. Renew
        // and Rejuvenation are instant and cooled too, but their healing is ten
        // seconds out — the hot lane (`hotFor`) owns those, and an emergency
        // answered with a HoT is the wipe the burst window exists to prevent.
        && a.Effects |> List.exists (function Heal _ -> true | _ -> false)
        && not (hasHot a)
        && healsOf a > 0
        && SimState.canReach a.Range h t w
        && deficit * 10000
          >= (healsOf a + h.BonusHealing) * Talents.healBp a.Name h.Talents)

    // The cooldown buttons — Nature's Swiftness, Swiftmend, Inner Focus. None of
    // them is a heal, so the selection above cannot see them: they are cast on the
    // way *into* a heal, and only when the hole is big enough to be worth a two- or
    // three-minute cooldown. A healer who has none of them (every class but these
    // three) finds nothing here and is unaffected.
    //
    // The Paladin used to be the fourth, through Divine Favor. OctoWoW turns Divine
    // Favor into a passive Holy Shock talent, so the kit row is gone and the class
    // has no button to press — its absence here is the port, not an oversight.
    //
    // The trip point is 60% and not 40%, because the whole point of an instant is
    // the cast time it skips: a Druid's Regrowth takes two seconds, so a button
    // pressed at 40% is a heal that lands in the low twenties, if at all. One
    // cast-time of health above death is what a fast heal is worth — and the
    // measurement that set it is the Druid taking twelve deaths beside a Priest
    // taking three (§"Balance status"), all of them spikes answered a heal late.
    let needsFastHeal (t: Entity) = SimState.healthPct t < healerFastPct

    /// The play the operator asked for, in one place because both callers must agree
    /// on it: the heal-over-time is left to tick down and only eaten at its end.
    ///
    /// `Aura.swiftmend` pays a *fixed* 12 s of Rejuvenation or 18 s of Regrowth
    /// whatever is left on the buff, and `Aura.refresh` replaces rather than stacks,
    /// so a press on a fresh aura destroys the pending Ticks whose mana was already
    /// paid. The press costs the same on the first Tick as on the last; the last one
    /// keeps every Tick the aura had. `secTicks 2.0` is "the last tick", the same
    /// two seconds `refreshWindow` now treats as the edge of a refresh.
    ///
    /// The pure-overheal refusal is part of the predicate rather than the caller's
    /// job: an instant that would land whole is the only one worth buying, and a
    /// press that would overheal is *held* — the aura keeps healing the gap it was
    /// already going to fill and the button is kept. Returns what the aura has left,
    /// so a caller can order candidates least-remaining-first.
    let tailToBurn (t: Entity) =
      match Aura.swiftmend t.Auras with
      | Some(source, instant) when
        instant > 0
        && t.Health + instant <= t.MaxHealth
        && Aura.healOverTimeRemainingFor source t.Auras <= secTicks 2.0
        ->
        Some(Aura.healOverTimeRemainingFor source t.Auras)
      | _ -> None

    let cooldownFor (t: Entity) =
      let fast = needsFastHeal t

      h.Abilities
      |> List.tryFind (fun a ->
        canUse h a.Name
        && (a.TargetKind = Self || SimState.canReach a.Range h t w)
        && (a.Effects
            |> List.exists (function
              | Empowers _ -> fast
              // Swiftmend is worth it only on the buff's tail — `tailToBurn` is the
              // whole rule, and an emergency is not a licence to eat a fresh aura.
              // Pressing at 55% on a Rejuvenation with ten seconds left pays the same
              // 90 and throws those ten seconds away, which is the mana the ledger
              // said bought nothing. Under Tree of Life the button has no cooldown at
              // all (`SimCombat`, the cooldown exemption), so holding it here costs
              // only the instant heal — never a cooldown — and what it must not do is
              // spend mana already spent.
              | ConsumeHealOverTime -> fast && (tailToBurn t).IsSome
              | _ -> false)))

    let wounded =
      party
      |> List.filter (fun e -> SimState.healthPct e < healerTroublePct)
      |> List.sortBy SimState.healthPct

    // A tile this healer can actually heal from: in range, in sight, and free.
    // Walking to a tile beside the target is not enough — beside can be around a
    // corner, and then the healer stands there for the rest of the fight.
    /// The nearest floor tile from which `t` is within `reach` tiles: the shared
    /// placement rule behind both a heal's casting spot and a swing's standing spot.
    /// Written once because the two shapes are the same question — "where do I stand
    /// to reach that body?" — differing only in the number, and the house pattern for
    /// two classes that need the same predicate is one predicate (`worthPressing`).
    let spotInReach (t: Entity) (reach: int) =
      [ for dy in -reach .. reach do
          for dx in -reach .. reach do
            let p =
              { X = t.Pos.X + dx
                Y = t.Pos.Y + dy }

            if
              Grid.isFloor w.Grid p
              && Pos.chebyshev p t.Pos <= reach
              && not (SimState.claimedByOther w h.Id p)
              && Path.lineOfSight w.Grid p t.Pos
            then
              yield p ]
      |> List.sortBy (fun p -> Pos.chebyshev h.Pos p)
      |> List.tryHead

    let healingSpot (t: Entity) (a: Ability) = spotInReach t a.Range

    /// The body the party is actually fighting — as close as this world gets to a
    /// "tank's target".
    ///
    /// There is no party-level target concept to read: `SimTick.advanceSwings` gives
    /// every armed body its *own* swing target by scanning `aliveHostiles` for the
    /// nearest `Engaged` body in reach, and nothing records "what the tank is on" as
    /// shared state. So it is reconstructed here: the Tank's own `Target` first, since
    /// that is the party's statement of intent (set by the pull, then kept honest by
    /// `currentTarget`'s threat recompute), and the swing resolver's own
    /// nearest-engaged rule as the fallback. The fallback is deliberately the same rule
    /// `advanceSwings` uses, so the tile a healer walks to is the body its swing will
    /// actually land on instead of one it would have to be re-told about.
    ///
    /// The approximation is real and worth naming: a tank whose threat target has just
    /// flipped can point this one Tick stale, and a tank that named nothing leaves the
    /// healer walking at the nearest engaged body rather than the tank's. Both are the
    /// correct direction to be wrong in — the swing then resolves against whatever
    /// `advanceSwings` picks, which is that same nearest-engaged body.
    let meleeMark =
      let asFoe (id: EntityId) =
        w.Entities
        |> List.tryFind (fun e -> e.Id = id && e.Faction = Hostile && SimState.alive e)

      party
      |> List.tryFind (fun p -> p.Role = Some Tank)
      |> Option.bind (fun tk -> tk.Target)
      |> Option.bind asFoe
      |> Option.orElse (
        SimState.aliveHostiles w
        |> List.filter (fun t -> t.Engaged)
        |> List.sortBy (fun t -> Pos.chebyshev h.Pos t.Pos, t.Id)
        |> List.tryHead
      )

    /// One step into swing reach of `meleeMark`, for a healer that fights with a weapon.
    ///
    /// Scoped by shape first, then by class. A healer with a sitting form to return to
    /// (`homeForm`) is the Druid, and the Druid's melee comes from the shape it already
    /// fights in — moving its caster form would work against the shapeshift logic and
    /// against `home`. What is left is tested against `isMeleeHealer`.
    ///
    /// No new timer: `Entity.AutoAttack` already carries the reach and `SimTick`
    /// `advanceSwings` already runs its `SwingTicksLeft` clock for every armed body,
    /// so getting in reach is the whole job — the swing follows on its own cadence. No
    /// order at all when already in reach: a standing swing writes no command
    /// (`healerActed`'s "a swing is not a decision"), and a redundant `MoveTo` would
    /// both churn the path and count idle Ticks as busy.
    ///
    /// This can never cost a heal. It is reached only from `idle`, which is the tail of
    /// the chain — the heal, the refresh, the banked HoT and the dispel all sit above
    /// it — so a Tick that has anything to cast never gets here to walk.
    /// The healers that fight with a weapon between heals.
    ///
    /// Named classes, not a structural test, because the structural tests available
    /// here all give the wrong set: every healer carries a one-tile melee weapon
    /// (`AutoAttack.Range` is 1 for all four), `Role` is `Healer` for all four, and
    /// `Entity` carries no armour class — `ItemArmor` is a number, not a proficiency,
    /// and the Druid's leather would slip through a threshold that the Priest's cloth
    /// should fail. `homeForm` already removes the Druid (it has a shape to return
    /// to); this list is what is left.
    ///
    /// The Priest was in the first cut of this rule and was taken back out on the
    /// measurement: at 30 seeds it gave up 4.7 hps (88.4 -> 83.7) for 2.9 dps, because
    /// a cloth healer standing in a mob's face takes the damage it then has to spend
    /// its own mana undoing. The trade is acceptable for a mail/leather healer whose
    /// weapon is worth something and is not worth it for the Priest. Keep the list
    /// short: a class joins it when a measurement says the swing pays for itself.
    let isMeleeHealer (e: Entity) = e.Name = "Shaman" || e.Name = "Paladin"

    let meleeStep =
      h.AutoAttack
      |> Option.filter (fun a -> a.Range <= 1)
      |> Option.filter (fun _ -> homeForm h = None && isMeleeHealer h && SimState.inCombat w)
      |> Option.bind (fun a ->
        meleeMark
        |> Option.filter (fun t -> not (SimState.canReach a.Range h t w))
        |> Option.bind (fun t ->
          // Behind the mark first — a melee-stepping healer is exactly the body
          // the enemy cleave cone is aimed past, and the back tile is the one
          // tile the 90° front arc cannot reach (`SimCombat`, the Cleave gate).
          // Any tile in reach is the fallback, not the preference: a corridor
          // fight leaves the mob's back against a wall, and a healer that
          // refuses to stand anywhere is a healer that never swings at all.
          match behindSpot w h t with
          | Some b -> Some b
          | None -> spotInReach t a.Range))
      |> Option.map (fun p -> cmd at (MoveTo(h.Id, p)))

    /// What a healer with nothing to do this Tick does: close to the fight if it fights
    /// with a weapon and is not there yet, otherwise return to its sitting shape. One
    /// definition because the chain below falls back to it in four places, and the four
    /// must not drift apart.
    /// The shift back to the shape the class lives in, when it is in the wrong one.
    /// Ungated: this is the *sitting* form, what a body with nothing to do returns to.
    let home =
      homeForm h
      |> Option.bind (fun s -> if SimState.inStance h s then None else shiftTo h s)
      |> Option.map (fun f -> cmd at (UseAbility(h.Id, f.Name, h.Id)))

    /// What a healer with nothing to do this Tick does: close to the fight if it fights
    /// with a weapon and is not there yet, otherwise return to its sitting shape. One
    /// definition because the chain below falls back to it in four places, and the four
    /// must not drift apart. Declared after `home` because it reads it.
    let idle = meleeStep |> Option.orElse home

    /// Whether the cast about to be made is one the home form pays for.
    ///
    /// Tree of Life buys a fifth off every heal-over-time's price (`SimCombat`, the
    /// cost site) and adds its Spirit to both heal sites, and it is where the spec
    /// lives, so a heal-over-time is worth the 60 to be back in it. A heal the form
    /// does nothing for is not: entering for one is 60 mana that buys nothing, which
    /// is what the churn came to — 54.7 shifts an encounter and 2.9 of them followed
    /// by a spell the new shape actually served. Identified by effect shape, not by
    /// name, so Rejuvenation, Regrowth and any heal-over-time added later are one
    /// rule.
    let wantsHome (a: Ability) =
      a.Effects
      |> List.exists (function
        | HealOverTime _ -> true
        | ConsumeHealOverTime -> true
        | _ -> false)

    /// The shift home, but only when the cast standing behind it wants the form.
    let homeFor (a: Ability) = if wantsHome a then home else None

    /// The shift that makes a cast legal, when the spell names a shape. One action
    /// per Tick like any other order: a Tree Druid that wants Healing Touch shifts
    /// out first, casts next Tick, and is given the form back after.
    ///
    /// A waived ability asks for no shift: the empower already made it legal, so
    /// shifting would spend the Tick the instant heal exists to save.
    let shapeFor (a: Ability) =
      match a.Stance with
      | Some s when not (SimState.inStance h s) ->
        if formWaived a then
          None
        else
          shiftTo h s |> Option.map (fun f -> cmd at (UseAbility(h.Id, f.Name, h.Id)))
      | _ -> None

    // Into range, or not worth it: the reach rule is SimState's, never restated.
    let order (choice: Ability option) (t: Entity) =
      match choice with
      | Some a when SimState.canReach a.Range h t w ->
        (match shapeFor a with
         | Some shift -> shift
         // The shift follows the cast it enables, in both directions. Out of the tree
         // only for a spell that names a shape (`shapeFor`, which is Healing Touch and
         // nothing else in this kit), and back into it only for a cast the form pays
         // for — `homeFor`. The old rule shifted home on every calm heal order, which
         // is 60 mana per order to sit in a form the next Healing Touch would leave
         // again: the round trip was paid for and the cast that justified it usually
         // never happened. A calm target can spare the Tick, which is what the form is
         // for; it cannot spare 60 mana for nothing.
         | None when not (needsFastHeal t) ->
           (match homeFor a with
            | Some back -> back
            | None -> cmd at (UseAbility(h.Id, a.Name, t.Id)))
         | None -> cmd at (UseAbility(h.Id, a.Name, t.Id)))
      | Some a -> cmd at (MoveTo(h.Id, healingSpot t a |> Option.defaultValue t.Pos))
      | None -> []

    /// Into a direct heal: the instant on cooldown first (the Paladin's panic
    /// button), then the cooldown empowers, then the size-based pick — the
    /// Paladin's `paladinDirect`, the shared cost-to-close pick otherwise.
    let heal (t: Entity) =
      match panicFor t |> Option.orElse (cooldownFor t) with
      // They are self-buffs, so the caster is the target of the button itself.
      | Some a -> cmd at (UseAbility(h.Id, a.Name, (if a.TargetKind = Self then h.Id else t.Id)))
      | None -> order (paladinDirect t (t.MaxHealth - projected t)) t
    // A HoT press must buy something the target does not already have, and there are
    // exactly two ways to buy it twice.
    //
    // The time: a live aura still has ticks on it that were already paid for, and
    // `Aura.refresh` replaces rather than stacks, so re-pressing throws them away and
    // pays full price to buy the same cover again. How much tail is worth replacing
    // depends on what the kit does with a tail. A kit that only rolls its HoTs can
    // afford the generous four seconds — early enough that the next one is up before
    // this one lapses. A kit that *banks* one — a consume button such as Swiftmend
    // turns the ticks left on an aura into an instant heal — must wait for the last
    // tick, because an early press destroys the bank `tailToBurn` is holding for as
    // well as the ticks. At four seconds that was a Rejuvenation replaced with a
    // third of its duration (four of twelve ticks) still owed: 209 presses and 9,624
    // mana a run on time already covered.
    //
    // The heal: a two-part HoT lands a direct half as well, and that half is most of
    // what it costs. Pressed on a body whose hole the ticking HoTs already cover it
    // lands as overheal — 178 of 186 Regrowth halves were clipped — when the cheaper
    // pure HoT would have covered the same body for a third of the mana. So the
    // direct half is only bought when the hole can actually absorb it.
    let banksTail =
      h.Abilities
      |> List.exists (fun a ->
        a.Effects |> List.exists (function
          | ConsumeHealOverTime -> true
          | _ -> false))

    /// Two seconds is the last tick — the same edge `tailToBurn` treats as the end of
    /// a buff — for a kit that banks its tails; four for a kit that only rolls them.
    let refreshWindow = if banksTail then secTicks 2.0 else secTicks 4.0

    let worthPressing (t: Entity) (a: Ability) =
      let remaining = Aura.healOverTimeRemainingFor a.Name t.Auras

      let inTime =
        if isTwoPart a then remaining <= secTicks 0.0 else remaining <= refreshWindow

      // Measured on current health, not on `projected`: the direct half lands after
      // the cast, and charging the pending ticks against it too would demand a hole
      // the size of the whole two-part heal before the HoT is allowed up at all —
      // which on this kit is a Druid that stops layering and wipes on the seeds that
      // needed the cover.
      let holeCanTakeUpFront =
        let upFront = upFrontOf a
        upFront = 0 || t.MaxHealth - t.Health >= upFront

      inTime && holeCanTakeUpFront

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
        |> List.tryFind (fun t -> worthPressing t a)
        |> Option.map (fun t -> t, a))
      |> Option.map fst

    /// The spell to refresh `t` with: the biggest one it is missing or losing.
    let hotFor (t: Entity) = hots |> List.tryFind (worthPressing t)

    // Direct-heal candidates: projected health — now plus everything already
    // ticking, plus the HoT about to go up — still short of three quarters.
    let inFlight (t: Entity) =
      Aura.healOverTimePending t.Auras
      + (if Some t = hotTarget then hot |> Option.map healsOf |> Option.defaultValue 0 else 0)
    /// Whether every HoT this healer can currently pay for is already ticking on
    /// the target. The direct heal is the top of the layering, not the bottom: it
    /// is spent on a body already wearing the HoTs, never in place of one. A healer
    /// with no HoTs in its kit has nothing to establish and passes straight through,
    /// which is why this is a shape test and not a Druid test.
    let fullyCovered (t: Entity) =
      hots |> List.forall (fun a -> Aura.healOverTimeRemainingFor a.Name t.Auras > secTicks 0.0)

    let directTarget =
      wounded
      |> List.filter (fun t -> t.Health + inFlight t < t.MaxHealth * 3 / 4)
      |> List.filter fullyCovered
      |> List.sortBy projectedPct
      |> List.tryHead

    /// Innervate is mana given away, so it goes to the healer with the lowest bar —
    /// the Priest or the Paladin in practice, and the Druid himself when he is the
    /// only healer or the driest one. Healers first because the party's mana is the
    /// healer's mana: a dry Mage costs a slow pull, a dry healer costs the pull. It
    /// is taken mid-fight only, and never in front of a heal somebody needs.
    let innervate =
      if not (SimState.inCombat w) || not (canUse h "Innervate") then
        None
      else
        let dry (m: Entity) = SimState.alive m && m.ResourceKind = Mana && SimState.resourcePct m < 60
        let healers = party |> List.filter (fun m -> m.Role = Some Healer && dry m)
        let others = party |> List.filter (fun m -> m.Role <> Some Healer && dry m)

        (if not healers.IsEmpty then healers else others)
        |> List.sortBy SimState.resourcePct
        |> List.tryHead
        |> Option.map (fun t -> cmd at (UseAbility(h.Id, "Innervate", t.Id)))

    // Free and instant, so it is taken before any heal — but never in front of a
    // heal that somebody needs this Tick, and never with nobody dry.
    let urgent = directTarget |> Option.exists (fun t -> projectedPct t < 65)

    match innervate with
    | Some gift when not urgent -> gift
    | _ ->

    // A spike outranks a refresh. `projected` counts the HoT that is *about* to go
    // up, so a tank at 55% with a lapsed Rejuvenation looks covered to
    // `directTarget` and the party settles for re-casting Rejuvenation — two
    // seconds of nothing, which is precisely the wait Swiftmend and Nature's
    // Swiftness exist to skip. Real health decides this branch, not projection.
    let hurtTarget = wounded |> List.sortBy SimState.healthPct |> List.tryHead

    match hurtTarget with
    | Some t when needsFastHeal t -> heal t
    | _ ->

    // The Rejuvenation, or the Regrowth, at its last tick. Swiftmend pays a fixed
    // 12 s of Rejuvenation or 18 s of Regrowth whatever is left on the buff, so the
    // play is the opposite of banking the tail: let the HoT tick down and press at
    // the end, which keeps every Tick it already paid *and* the whole instant heal.
    // Pressing early pays the same and throws the remaining Ticks away. Only when
    // the heal lands whole — `t.Health + instant <= t.MaxHealth` — so that using it
    // outside an emergency can never overheal. A candidate that would land as
    // overheal is not a candidate at all: the button is *held*, not spent, and the
    // HoT keeps ticking into the gap it was already going to heal. Identified by
    // effect shape, not by name, so another HoT-eating button is picked up too.
    //
    // Candidates are ordered by time remaining, not by who is hurt most. The instant
    // is the same size whatever is left on the buff, so the cheapest press is the
    // buff closest to expiring — every Tick it still has is a Tick that pressing now
    // would throw away. Taking the first match in party order, as this did, spent
    // the button on a HoT with 1.9 s left while another had 0.2 s and died with it;
    // under Tree of Life, where the button has no cooldown, that is a wasted press
    // every Tick it is up.
    let banked =
      h.Abilities
      |> List.tryFind (fun a ->
        canUse h a.Name
        && (a.Effects
            |> List.exists (function
              | ConsumeHealOverTime -> true
              | _ -> false)))
      |> Option.bind (fun a ->
        party
        |> List.filter (fun t ->
          SimState.alive t
          && SimState.healthPct t < 90
          && (a.TargetKind = Self || SimState.canReach a.Range h t w))
        |> List.choose (fun t ->
          // The tail rule is `tailToBurn`'s, shared with the emergency path so the two
          // cannot drift: near expiry, and never a press that would land as overheal.
          tailToBurn t |> Option.map (fun remaining -> t, remaining))
        // Time remaining first, health as the tie-break: two buffs about to expire
        // in the same Tick are ordered by who has less to lose.
        |> List.sortBy (fun (t, remaining) -> remaining, SimState.healthPct t)
        |> List.tryHead
        |> Option.map (fun (t, _) -> UseAbility(h.Id, a.Name, t.Id)))

    match banked with
    | Some cast -> cmd at cast
    | None ->

    // A cleaner is worth a global only when the body carries something it can take
    // off: `Dispel` on a target with no matching aura is a wasted button, and in
    // this content that is the common case. A friendly cleaner looks for a harmful
    // aura of its school on a member; an offensive one (Purge, Dispel Magic) looks
    // for a helpfully granted aura of that school on the mob already being fought —
    // which here means the Scarlet Chaplain's Renew and almost nothing else.
    // Instant, so it never holds a heal back; it sits below the heal because a heal
    // saves the body this Tick and a dispel does not.
    let dispelSchoolOf (a: Ability) =
      a.Effects
      |> List.tryPick (function
        | Dispel(school, _) -> Some school
        | _ -> None)

    let foeTarget =
      h.Target
      |> Option.bind (fun id ->
        w.Entities
        |> List.tryFind (fun e -> e.Id = id && e.Faction = Hostile && SimState.alive e))
    let cleanse =
      h.Abilities
      |> List.filter (fun a -> canUse h a.Name && (dispelSchoolOf a).IsSome)
      |> List.tryPick (fun a ->
        let school = (dispelSchoolOf a).Value

        let carrying (wantHelpful: bool) (e: Entity) =
          e.Auras
          |> List.exists (fun x -> Aura.dispelType x = school && Aura.isHelpful x = wantHelpful)

        match a.TargetKind with
        | Foe ->
          // A healer's own target is whoever it is healing, so an offensive purge
          // picks its own: the nearest hostile with something up that this school
          // can take off. `foeTarget` is the fallback for a caster whose target is
          // already the mob it is fighting.
          let candidates =
            SimState.aliveHostiles w
            |> List.filter (fun f -> SimState.canReach a.Range h f w && carrying true f)
            |> List.sortBy (fun f -> Pos.chebyshev h.Pos f.Pos, f.Id)
            |> List.tryHead

          (match candidates with
           | Some f -> Some f
           | None -> foeTarget |> Option.filter (fun f -> SimState.canReach a.Range h f w && carrying true f))
          |> Option.map (fun f -> UseAbility(h.Id, a.Name, f.Id))
        | _ ->
          party
          |> List.filter (fun t -> SimState.alive t && SimState.canReach a.Range h t w && carrying false t)
          |> List.sortBy SimState.healthPct
          |> List.tryHead
          |> Option.map (fun t -> UseAbility(h.Id, a.Name, t.Id)))
      |> Option.map (cmd at)

    match cleanse with
    | Some c -> c
    | None ->

    match directTarget, hotTarget with
    | Some t, _ when projectedPct t < healerUrgentPct -> heal t
    | _, Some t -> order (hotFor t) t
    | Some t, None -> heal t
    | None, None ->
      // Nobody is in trouble and nothing needs refreshing, so close the small
      // holes with the cheap heal instead of leaving them for the next burst —
      // but never pay a heal that is mostly overheal.
      match wounded |> List.tryHead with
      | Some t when projectedPct t < healerTroublePct ->
        let deficit = t.MaxHealth - projected t

        // A healer with its hands still is regenerating at the full Spirit rate, and
        // a hole smaller than that rate's own tick is the rate's job, not a spell's.
        // Closing it with a cast would forfeit the whole five-second window to buy a
        // few dozen points — the trade that empties a Druid over a long fight, one
        // cheap rank at a time. Nobody is under three quarters here, so the wait is
        // safe: this is the branch for a party that is not in trouble.
        if not fsrActive && deficit <= 2 * h.ResourceRegenPerTick then
          Option.defaultValue [] idle
        elif
          match paladinDirect t deficit with
          | Some a -> healsOf a / 2 <= deficit
          | _ -> false
        then
          match paladinDirect t deficit with
          | Some a -> order (Some a) t
          | _ -> Option.defaultValue [] idle
        else
          Option.defaultValue [] idle
      | _ -> Option.defaultValue [] idle

  /// The engine's own ways of refusing a heal, in the order `SimCombat`'s
  /// `UseAbility` arm checks them. Not every refusal can produce a blocked Tick:
  /// "no target" and "out of range" both leave the healer with an *order* — the
  /// party is the target pool, so an empty pool means nothing needs healing rather
  /// than a refusal, and `healerOrders`'s `order` answers an unreachable target with
  /// a `MoveTo` instead of standing still (`PartyAi.fs`, the `order` helper). A
  /// wrong form is answered the same way, by `shapeFor`'s shift. What is left is the
  /// two refusals the AI cannot route around: a pool too short for any heal, and a
  /// heal that is off cooldown's opposite. Both are real blockages; both are kept as
  /// separate cases because they need opposite fixes.
  type HealBlock =
    /// `SimState.canAfford` refuses every heal the kit holds.
    | ShortOfMana
    /// Every heal the kit could pay for is still on its cooldown.
    | OnCooldown
    /// Every affordable, ready heal names a form the healer is not standing in, and
    /// no held empower waives it (`formWaived`'s test, read back).
    | WrongForm

  /// Why a healer stood still. Decided only from the healer's *own* predicates, so a
  /// measurement can report a Tick as idle without inventing a second definition of
  /// "needs healing" that the AI would not recognise. Read it only on a Tick where
  /// the healer issued no order at all: every branch below explains a decline, and a
  /// Tick the healer acted on is not in any of them.
  ///
  /// The split matters because standing still is not one thing. A healer whose party
  /// is fine is doing its job; a healer whose heal is coming out of its own Spirit
  /// regeneration is doing its job faster than a cast would; a healer with an empty
  /// pool is stopped by the engine; and a healer that could have healed and did not is
  /// the only one of the four that is a finding. The single `idle%` this replaced
  /// lumped all four together and, worse, put an *instant* cast in there as well — the
  /// classifier read `Casting`, and an instant never sets it.
  type HealerStandby =
    /// Nothing needs doing: every party member's projected health — now plus the
    /// ticks already owed by its heals-over-time — is above the AI's own trouble
    /// bar. This is the bucket the heal-over-time classes live in, and it is not a
    /// defect: the ticks are the throughput.
    | StandbyCovered
    /// A hole exists but regeneration is the cheaper closer: the five-second window
    /// is clear and the hole is no bigger than two ticks of the Spirit rate. The AI
    /// deliberately declines to break the window for it (`healerOrders`, the
    /// `not fsrActive && deficit <= 2 * ResourceRegenPerTick` branch), which is mana
    /// coming back rather than a heal being missed.
    | StandbyRecovering
    /// A heal was wanted and the engine would have refused it.
    | StandbyBlocked of HealBlock
    /// A heal was wanted, the kit holds one that is affordable, ready and legal, and
    /// nothing was ordered. This is the only bucket that is a cost.
    | StandbyUnmet

  let healerStandby (w: World) (h: Entity) : HealerStandby =
    // The healer's own need-pool, so the classifier scores the pool the AI decided
    // on and not a wider one it never consulted (`SimState.healableParty`).
    let party = SimState.healableParty w

    let healsOf (a: Ability) =
      a.Effects
      |> List.sumBy (function
        | Heal(_, mx) -> mx
        | HealOverTime(duration, perTick) -> (duration / secTicks 1.0) * perTick
        | _ -> 0)

    let hasHot (a: Ability) =
      a.Effects |> List.exists (function HealOverTime _ -> true | _ -> false)

    let isHeal (a: Ability) =
      a.Effects |> List.exists (function Heal _ | HealOverTime _ -> true | _ -> false)

    let projected (t: Entity) = t.Health + Aura.healOverTimePending t.Auras
    let projectedPct (t: Entity) = projected t * 100 / t.MaxHealth

    // The same three predicates `healerOrders` builds its chain from: who is hurt and
    // in what order, whether the window is already broken, and which heals the
    // engine's form gate would let through.
    let wounded =
      party
      |> List.filter (fun e -> SimState.healthPct e < healerTroublePct)
      |> List.sortBy SimState.healthPct

    let fsrActive = h.Auras |> List.exists (function RecentlyCast _ -> true | _ -> false)

    let formWaived (a: Ability) =
      let held = Aura.empower h.Auras
      held.Instant && Empower.appliesTo a.Name held

    // `directFor`'s kit filter: a heal, not a heal-over-time. A HoT refresh that was
    // declined is not a missed heal — the ticking half it would replace is still
    // running, and `worthPressing` is what decides that, not this function.
    let direct = h.Abilities |> List.filter (fun a -> isHeal a && not (hasHot a))

    match wounded with
    | [] -> StandbyCovered
    | t :: _ ->
      // Most hurt first, which is the order `wounded` hands the AI: the head is the
      // body `hurtTarget` and the calm branch both look at.
      let deficit = t.MaxHealth - projected t
      let urgent = SimState.healthPct t < healerFastPct
      let needed = urgent || projectedPct t < healerTroublePct

      if not needed then
        // Wounded by real health but whole once the HoTs already in flight land: the
        // party's health is acceptable by the AI's own measure, and the cover is
        // exactly the heals-over-time the operator is describing.
        StandbyCovered
      elif not urgent && not fsrActive && deficit <= 2 * h.ResourceRegenPerTick then
        StandbyRecovering
      else
        // The engine's gates, in `SimCombat`'s order: form, then cooldown, then pool.
        // `canUse` folds cooldown and pool together, so they are separated here rather
        // than re-tested through it.
        let legal =
          direct
          |> List.filter (fun a -> (a.Stance |> Option.forall (SimState.inStance h)) || formWaived a)

        let offCooldown (a: Ability) =
          (h.Cooldowns |> Map.tryFind a.Name |> Option.defaultValue (ticks 0)) <= ticks 0

        let ready =
          legal
          |> List.filter offCooldown
          |> List.filter (SimState.canAfford h)

        if not direct.IsEmpty && ready.IsEmpty then
          // The engine's gates in `SimCombat`'s order: form, then cooldown, then pool.
          // `legal` is empty only when every direct heal names a form the healer is not
          // in, and that has to be tested *before* the cooldown filter: a form-refused
          // kit has no off-cooldown member either, so testing cooldown first would
          // report every form problem as a cooldown one and leave `WrongForm` dead.
          if legal.IsEmpty then
            StandbyBlocked WrongForm
          elif legal |> List.filter offCooldown |> List.isEmpty then
            StandbyBlocked OnCooldown
          else
            StandbyBlocked ShortOfMana
        elif not urgent && (ready |> List.map healsOf |> List.max) / 2 > deficit then
          // The AI's own overheal refusal (`healsOf a / 2 <= deficit`, the cheap-heal
          // branch): the best heal it could buy would have to waste more than half
          // itself, so the hole is left alone on purpose. Reading that as `unmet`
          // would blame the healer for a party that is, by its own arithmetic, fine.
          StandbyCovered
        else
          StandbyUnmet

  /// One living in-combat Tick of a healer, in exactly one bucket. The harness counts
  /// these and asserts the five shares sum to 100%, so an edit that adds a sixth exit
  /// to a healer's decision — or forgets one — fails a build rather than quietly
  /// resizing someone's `idle%`.
  ///
  /// `Active` is decided first and exclusively: a Tick the healer ordered something on
  /// is not standing still, whatever the party looked like, so the four standby
  /// buckets can only ever describe a Tick with no order on it.
  type HealerTick =
    | Active
    | Covered
    | Recovering
    | Unmet
    | Blocked of HealBlock

  /// Whether this Tick was spent doing anything at all.
  ///
  /// Read from three sources, because no one of them covers the class space:
  ///
  ///  - `Casting` catches a hard cast in progress, which is the only thing the old
  ///    `idle%` looked at.
  ///  - The orders catch a decision made this Tick, and crucially an *instant* cast:
  ///    `SimCombat.startOrResolve` resolves an ability with `CastTicks <= 0` outright
  ///    and never sets `Casting`, so Rejuvenation, every shock and every cat row would
  ///    otherwise read as the healer standing still. That bias falls hardest on the
  ///    classes that live on instants, which is why the metric was wrong for them.
  ///  - The movement state catches a step already under way: a body half-way through a
  ///    walk has no fresh order on that Tick — it keeps the route it was given — and a
  ///    healer closing on a wounded tank is not idle either.
  ///
  /// `Stop` is deliberately not activity: it is the order to stand still, and counting
  /// it would make every idle Tick look busy. Neither is the auto attack, which writes
  /// no command at all: a swing is not a decision.
  let healerActed (h: Entity) (cmds: Command list) : bool =
    let moving = int h.MoveTicksLeft > 0 || h.Destination.IsSome || not h.Path.IsEmpty

    let ordered =
      cmds
      |> List.exists (fun c ->
        match c.Kind with
        | UseAbility(actor, _, _)
        | MoveTo(actor, _) -> actor = h.Id
        | Stop _ -> false)

    h.Casting.IsSome || moving || ordered

  let healerTick (w: World) (h: Entity) (cmds: Command list) : HealerTick =
    if healerActed h cmds then
      Active
    else
      match healerStandby w h with
      | StandbyCovered -> Covered
      | StandbyRecovering -> Recovering
      | StandbyUnmet -> Unmet
      | StandbyBlocked cause -> Blocked cause

  /// Fold a run of classified Ticks into the five counts `sim healers` prints, in
  /// report order: active, covered, unmet, blocked, recovering. The `match` is the
  /// point — it is exhaustive over `HealerTick`, so a sixth bucket cannot be added
  /// without landing in this table, and a caller that sums the five against the
  /// number of Ticks it folded in is asserting the partition closes.
  let tally (ticks: HealerTick list) : int array =
    let counts = Array.zeroCreate 5

    for t in ticks do
      let bucket =
        match t with
        | Active -> 0
        | Covered -> 1
        | Unmet -> 2
        | Blocked _ -> 3
        | Recovering -> 4

      counts[bucket] <- counts[bucket] + 1

    counts

  /// A Shaman keeps one totem of each element up, the way the class plays them:
  /// earth, air, then water, each dropped at his feet to stand for two minutes.
  /// They are instant casts, so one a Tick is enough to have all three down — and
  /// three orders for one actor in one Tick is exactly what the sim forbids.
  ///
  /// The water slot has a choice in it and the choice is the point: Mana Spring is
  /// the standing trickle, Mana Tide is the five-minute reserve, and the tide is
  /// held for a pull that might outlast the party's mana — three or more hostiles
  /// engaged, or a caster under 40%. When it lands it replaces the spring, which
  /// one-totem-per-slot makes free (`SimCombat`'s summon dismisses the occupant).
  /// Water Shield is a self-buff with no clock: it lasts until its globes are spent,
  /// so its whole upkeep is putting it back when it is gone. That is why this is one
  /// "is it up" test rather than a priority row — there is no refresh to schedule and
  /// nothing to re-press while it stands.
  ///
  /// It sits ahead of the heals because Improved Water Shield's regeneration is
  /// proportional to the globes still standing: every Tick without the shield is
  /// regeneration already lost, and a Shaman that heals first and shields second
  /// never finds the global. Ahead of the totems for the same reason — the shield is
  /// free of cast time and of any duration, so there is no ordering where it loses.
  ///
  /// The row is found in the kit rather than asked for by name, so nothing here knows
  /// a Shaman from anything else: a class whose kit carries no such effect gets no
  /// order, which is why this branch costs the Druid, the Priest and the Paladin
  /// exactly one `List.tryFind` over seven-ish rows a Tick.
  let private shieldOrders (e: Entity) (at: int<tick>) =
    let carries = e.Auras |> List.exists (function WaterShield _ -> true | _ -> false)

    if carries then
      []
    else
      e.Abilities
      |> List.tryFind (fun a ->
        a.TargetKind = Self
        && (a.Effects |> List.exists (function RaiseShield _ -> true | _ -> false))
        && canUse e a.Name)
      |> Option.map (fun a -> cmd at (UseAbility(e.Id, a.Name, e.Id)))
      |> Option.defaultValue []

  /// One Shaman, one totem per element, dropped at his feet — and one order per
  /// Tick, because they are all instant casts and three orders for one actor in
  /// one Tick is exactly what the sim forbids. The kit's own order is the drop
  /// order: earth, air, then water.
  ///
  /// A slot is standing only where the party can still reach it. The drop point
  /// is the Shaman's own tile (`SimCombat` puts the summon at the caster's feet),
  /// a totem never moves and never expires, and one World spans the wing — so a
  /// totem alive at the last pull's ground is *not* standing here. The reach test
  /// is the spec's own radius, measured from the Shaman: the set is always at
  /// his feet, the party walks with him, and when he is outside the radius the
  /// slot is vacant and wants re-dropping wherever he now stands.
  ///
  /// The water slot is the one real choice in the set. Health pressure answers
  /// first — health is what the healer spends mana to replace, so while anybody
  /// is wounded the stream is mana the healer does not have to spend, and 55
  /// health a pulse to every body in reach beats 15 mana a pulse to every
  /// caster. Without pressure the spring is the standing pick: mana is the run's
  /// binding constraint, and a pull that opens with the trickle already running
  /// is a pull the healer opens richer. The switch only runs towards pressure
  /// inside a fight (a stream that keeps swapping for the spring as the healer
  /// tops the tank up would oscillate with every swing); out of combat rest
  /// heals the party faster than any totem and pays for the drop out of free
  /// regeneration, so the spring replaces a stream that was left standing and
  /// the set arrives at the next pull shaped for it.
  let private totemOrders (w: World) (e: Entity) (party: Entity list) (at: int<tick>) =
    // (slot, name) of every standing totem the party can still reach.
    let standing =
      w.Entities
      |> List.filter (fun t ->
        t.Owner = Some e.Id
        && SimState.alive t
        && t.Totem |> Option.forall (fun x -> Pos.chebyshev e.Pos t.Pos <= x.Radius))
      |> List.choose (fun t ->
        t.Totem |> Option.map (fun x -> TotemSlot.ofPulse x.Pulse, t.Name))

    let covered (slot: int) = standing |> List.exists (fun (s, _) -> s = slot)

    let standingWater = standing |> List.tryFind (fun (s, _) -> s = 2) (* water *)

    // The tide is the five-minute reserve, held for a pull that might outlast
    // the party's mana: three or more hostiles with a target, or a healer
    // running dry. In combat only — this lane also runs between pulls now, and
    // a tide dropped while resting spends its cooldown where regeneration is
    // free anyway.
    let toughPull =
      SimState.inCombat w
      && ((SimState.aliveHostiles w
           |> List.filter (fun h -> h.Target.IsSome)
           |> List.length) >= 3
          // A healer's bar is the party's reserve, so the tide is held for the pull
          // where the healer itself is running dry — not merely for one where
          // somebody is. That is the Shaman answering a call, not topping up.
          || (party
              |> List.filter (fun p -> p.Role = Some Healer && p.ResourceKind = Mana)
              |> List.exists (fun p -> SimState.resourcePct p < 60)))

    // Anybody in the party health-pressured: below 60% is where the healers'
    // own calm gate starts reaching, so the stream arrives with the same urgency
    // the heals would.
    let pressed =
      party
      |> List.exists (fun p -> SimState.alive p && SimState.healthPct p < 60)

    let waterWanted =
      if pressed then "Healing Stream Totem" else "Mana Spring Totem"

    e.Abilities
    |> List.tryPick (fun a ->
      let slot =
        a.Effects
        |> List.tryPick (function
          | Summon s -> s.Totem |> Option.map (fun x -> TotemSlot.ofPulse x.Pulse)
          | _ -> None)

      let summons = a.Effects |> List.exists (function Summon s -> s.Totem.IsSome | _ -> false)

      if not (summons && slot.IsSome && canUse e a.Name) then
        None
      else
        let s = slot.Value

        // The tide keeps its own gate, and is the one drop allowed to land on a
        // live totem: over a standing spring, because the spring it would feed
        // is replaced by the pool refilled outright — over nothing else, because
        // a tide that displaced a stream would take healing away to add mana.
        if a.Name = "Mana Tide Totem" then
          if
            toughPull
            && (not (covered s) || standingWater = Some(2, "Mana Spring Totem"))
          then
            Some(UseAbility(e.Id, a.Name, e.Id))
          else
            None
        elif s <> 2 (* water *) then
          // The boon slots have no choice in them: standing where it reaches is
          // the whole of the job, and vacant is the only time to drop.
          if covered s then None else Some(UseAbility(e.Id, a.Name, e.Id))
        else
          // The water slot. Only the wanted totem is a candidate, so the two
          // cleaners in the slot never win it by walking the kit order — no
          // Scarlet mob poisons or infects, and the kit's own comment says so.
          let wantIt = a.Name = waterWanted

          // The tide holds the slot on a tough pull: the spring it would feed is
          // about to be replaced by the totem that refills the pool outright.
          let heldForTide = a.Name = "Mana Spring Totem" && toughPull

          let displace =
            match standingWater with
            | None -> true
            | Some(_, name) when name = a.Name -> false (* already standing here *)
            | Some(_, "Mana Spring Totem") ->
              // Pressure has arrived: the stream displaces the spring, because
              // the wounds are what the mana was being saved for.
              a.Name = "Healing Stream Totem"
            | Some(_, _) ->
              // A stream stands. In a fight it keeps the slot — swapping it for
              // the spring as the healer tops the party up would oscillate with
              // every swing the tank takes. Out of combat rest heals faster than
              // any totem can, so the spring displaces it to be standing for the
              // next pull, and rest pays for the drop.
              a.Name = "Mana Spring Totem" && not (SimState.inCombat w)

          if wantIt && not heldForTide && displace then
            Some(UseAbility(e.Id, a.Name, e.Id))
          else
            None)
    |> Option.map (cmd at)
    |> Option.defaultValue []

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

  /// The feral Druid's damage, and the only lane in this file that spends combo
  /// points on a bleed. A cat arrives here as an ordinary `Dps` member in Cat Form,
  /// so it is dispatched from its own branch rather than through `meleeOrders`:
  /// three of that function's rules read this kit as empty. Rip is a
  /// `BleedFinisher` and `isFinisher` knows only `Finisher`, so a cat would pay
  /// thirty energy for a one-point bleed and never see the five it could have
  /// spent. Rake's worth is its bleed while `damageOf` reads direct hits only, so
  /// the upkeep loses every ranking to Claw's identical 100% weapon damage. And
  /// Berserk is a `Self` cooldown that no `TargetKind = Foe` filter ever sees.
  /// Teaching `meleeOrders` those three things would put a form branch in front of
  /// every Rogue swing to buy a Rogue nothing.
  ///
  /// The rotation is the kit's own arithmetic rather than a priority list invented
  /// here:
  /// - **Berserk at a full bar.** `Aura.Innervated` doubles `advanceResource`'s
  ///   energy for twenty seconds, and energy parked at the cap is regeneration
  ///   already thrown away — so the cooldown is worth pressing when there is
  ///   nothing left to fill, which is the `surplus` test the Rogue's stun uses.
  /// - **Rip while the target is not already bleeding.** `BleedFinisher` is
  ///   `total + step * (points - 1)`, and a point costs a Claw's 45 energy while
  ///   rank 6 Rip's own step is 168 damage — so the five-point cast is the cheap
  ///   one. Four is the floor for the reason `meleeOrders` gives for Eviscerate.
  /// - **Rake while there is no bleed and Rip is not ready to spend.** Forty energy
  ///   buys nine seconds of bleed: it is the bridge that keeps the target bleeding
  ///   while the points build, which is the one thing a cat does that a Rogue does
  ///   not.
  /// - **Ferocious Bite for the points** once something is bleeding. A second Rip
  ///   on a bleeding target is not a refresh — a finisher has no handle on the aura
  ///   it applied — it is a second aura, so the reusable spend is the Bite.
  /// - **Shred from behind, Claw from the front, and pool below both:** the same
  ///   positional rule `meleeOrders` applies to Backstab, through `builders` for
  ///   the same reason (225% weapon damage against 100%).
  ///
  /// What it deliberately does not do is heal. The cat kit holds no heal at all —
  /// those rows live on the `TreeDruid` kit — and `Aura.preventsCasting` would
  /// refuse a cast from this shape anyway; every cat row is instant, which is the
  /// property that lets a form-locked body act.
  let private catOrders (w: World) (e: Entity) (target: Entity option) (at: int<tick>) =
    // The cat's own bleed, by caster id: Rake's nine seconds and Rip's twelve are
    // one `Bleeding` case under two names, so "is my bleed up" cannot be asked by
    // name — and the id is what stops a Warlock's Corruption from answering yes.
    let bleeding (f: Entity) =
      f.Auras
      |> List.exists (function
        | Bleeding(_, _, Some caster) -> caster = e.Id
        | _ -> false)

    // Ravage is the stealth opener `meleeOrders` already offers a cat, repeated
    // here so that moving the lane costs the cat nothing: an enemy still at full
    // health and carrying no debuff is one it met from behind.
    let opener =
      SimState.aliveHostiles w
      |> List.filter (fun h -> h.Health = h.MaxHealth && h.Auras.IsEmpty && SimState.withinRange 1 e h)
      |> List.sortByDescending (fun h -> h.MaxHealth)
      |> List.tryHead

    // A full bar is the only bar Berserk is worth: below it the doubling would be
    // spent catching up rather than on the twenty seconds it lasts.
    let berserk = has e "Berserk" && canUse e "Berserk" && e.Resource >= e.MaxResource - 5

    // A strike that needs `points` combo points before it is worth spending them.
    let strike (n: string) (points: int) =
      e.Abilities
      |> List.tryFind (fun a -> a.Name = n)
      |> Option.filter (fun a -> ready e a.Name && SimState.canAfford e a && e.ComboPoints >= points)

    // Which of the kit's two builders needs the back: `builders` puts the harder
    // one first, and for this kit that is Shred at 225% weapon damage.
    let positional = builders e |> List.tryHead |> Option.map (fun a -> a.Name)

    let inPosition (f: Entity) = not (f.Target |> Option.exists (fun id -> id = e.Id))

    match target with
    | None -> []
    | Some f ->
      let bleedUp = bleeding f
      // Rip and Rake both want a target that is not bleeding; the difference is
      // that Rip spends points and Rake spends energy. Rip first is why the points
      // are built at all: five points on a bleedless target buy both the spend and
      // the bleed back, where Rake would buy only the bleed.
      //
      // Bite ranks behind both because a Rip beside a live bleed is a second aura
      // rather than a refresh, so the finisher that pays in a hit is the one to
      // press once the upkeep is already running. It exists from level 32 only
      // (`ferociousBiteRanks`), which is why a level-24 cat's spend is Rip or
      // nothing.
      let rip = if bleedUp then None else strike "Rip" 4
      let rake = if bleedUp then None else strike "Rake" 0
      let bite = strike "Ferocious Bite" 4

      let builder =
        builders e
        |> List.filter (fun a ->
          ready e a.Name
          && SimState.canAfford e a
          && (Some a.Name <> positional || inPosition f))
        |> List.tryHead

      let openWith = if has e "Ravage" && canUse e "Ravage" then Some "Ravage" else None

      // In reach means one tile: every cat row is a melee row, Rip and Rake
      // included, so there is no second range to ask about.
      let spend (a: Ability) = cmd at (UseAbility(e.Id, a.Name, f.Id))

      match openWith, opener with
      | Some n, Some h -> cmd at (UseAbility(e.Id, n, h.Id))
      // Berserk is the one order that is not a strike at the target, and it goes
      // first because it costs no energy and its twenty seconds start now.
      | _ when berserk && SimState.withinRange 1 e f -> cmd at (UseAbility(e.Id, "Berserk", e.Id))
      | _ when SimState.withinRange 1 e f ->
        match rip |> Option.orElse rake |> Option.orElse bite |> Option.orElse builder with
        | Some a -> spend a
        // Nothing affordable: stay on the mob and let the bar fill. Moving here
        // would walk the cat out of its own auto-attack for no gain.
        | None -> []
      | _ ->
        match behindSpot w e f with
        | Some p -> cmd at (MoveTo(e.Id, p))
        | None -> cmd at (MoveTo(e.Id, f.Pos))

  /// Melee damage. Kick a cast it can reach, cash in the combo points, then the
  /// hardest strike it can pay for on the Party's target. Eviscerate's damage is
  /// linear in points, so four points already out-earns the strike that would
  /// build a fifth: it fires from four up, never below.
  let private meleeOrders (w: World) (e: Entity) (target: Entity option) (at: int<tick>) =
    // The harder builder is a positional strike: it exists from behind the mob and
    // nowhere else, and a mob faces what it is attacking. `holdSpot` is what makes
    // the back exist for a mob the Tank holds. A mob that has *turned on the Rogue*
    // has no back to offer him — every tile he could step to is in front of it — so
    // from the front he falls back to the cheap builder rather than burning the
    // energy on a strike the mob is facing. Any other target qualifies, whichever
    // seat it is hitting.
    let inPosition (f: Entity) = not (f.Target |> Option.exists (fun id -> id = e.Id))

    // Which of the kit's two builders needs the back: `builders` puts the harder
    // one first.
    let positional = builders e |> List.tryHead |> Option.map (fun a -> a.Name)

    let mine =
      e.Abilities
      |> List.filter (fun a ->
        a.TargetKind = Foe
        && a.Range <= 1
        && not (interrupts a)
        && (Some a.Name <> positional
            || (match target with
                | Some f -> inPosition f
                | None -> true))
        // Ambush and Ravage are stealth openers, not rotation strikes: left in
        // this list they would win every `damageOf` comparison and be spent as
        // though stealth were free. They are offered only by the opener branch
        // below.
        && a.Name <> "Ambush"
        && a.Name <> "Ravage")

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
      // The positional builder first, and by rule rather than by arithmetic: it is
      // worth 150% weapon damage against the other builder's 100%, so `damageOf`
      // happens to put it on top today, and the sort says so on purpose in case a
      // rank is retuned.
      |> List.sortByDescending (fun a -> (if Some a.Name = positional then 1 else 0), damageOf a)
      |> List.tryHead
      |> Option.filter (SimState.canAfford e)

    // Kicking a spell matters more than the target the Party is on: a healer
    // left to finish its cast undoes more damage than the Rogue adds. Reachable
    // casters come first, and Gouge — a four-second stun — is the backup when
    // Kick is down and the caster is a few tiles away.
    let myInterrupts = interruptTargets w e

    let caster =
      SimState.aliveHostiles w
      |> List.filter (fun h -> h.Casting |> Option.exists (fun c -> c.Ability.Interruptible))
      |> List.filter (fun h -> Set.contains h.Id myInterrupts)
      |> List.sortBy (fun h -> ((if SimState.canReach 1 e h w then 0 else 1), Pos.chebyshev e.Pos h.Pos))
      |> List.tryHead

    // A stun is mitigation too, not only an interrupt: the mob hitting the Party
    // hardest cannot swing while it is stunned, and that is damage the healer never
    // pays for. Cheap Shot and Kidney Shot go on the healthiest engaged enemy that
    // is still able to act — but only when the Rogue can pay for a strike
    // afterwards, because a Rogue that spends every point of energy on control adds
    // no damage and the fight just gets longer.
    let uncontrolled =
      SimState.aliveHostiles w
      |> List.filter (fun h -> h.Target.IsSome && SimState.canAct h && SimState.withinRange 1 e h)
      |> List.sortByDescending (fun h -> h.MaxHealth)
      |> List.tryHead

    // shortcut: `Cheap Shot` costs 60 of a 100 point pool, so "stun and still
    // strike" cannot both hold — the honest gate needs 105 energy and never
    // fires once. Measured, that removes the stun entirely and `library max`
    // wipes at 4000 with 27 mobs left; the stun taken at the top of the bar,
    // where regen would otherwise cap out, grinds the same fight to 11 with the
    // Party intact at full health. Re-tune if the pool or the cost moves.
    // The opener. Stealth is not a state in this model; this stands in for it. An
    // enemy still at full health and carrying no debuff is one the Party has not
    // touched, so it is the enemy the Rogue met from behind — the first blow of a
    // pull may be a stealth opener, Ambush for the damage or Cheap Shot for the
    // stun when Ambush is not trained. As soon as anything lands the health is no
    // longer full and the window is closed for that pull.
    let opener =
      SimState.aliveHostiles w
      |> List.filter (fun h ->
        h.Health = h.MaxHealth && h.Auras.IsEmpty && SimState.withinRange 1 e h)
      |> List.sortByDescending (fun h -> h.MaxHealth)
      |> List.tryHead

    // Ravage first: it is the cat's own opener and no Rogue carries it, while
    // Ambush is the Rogue's and no cat carries it — so the order costs neither
    // class anything and reads as "the hardest opener this kit has".
    let openWith =
      [ "Ravage"; "Ambush"; "Cheap Shot" ]
      |> List.filter (fun n -> has e n && canUse e n)
      |> List.tryHead

    let surplus = e.Resource >= e.MaxResource - 5

    let stun =
      [ "Cheap Shot"; "Kidney Shot" ]
      |> List.filter (fun n -> has e n && canUse e n && surplus)
      |> List.tryHead

    // The last resort. A Rogue a quarter of health from the end spends its
    // escape buttons before any rotation strike — Vanish to drop every mob's
    // gaze at once, else Blind for the nearest hunter still in reach. Either
    // one is this Tick's whole action; displacing one strike is the price, and
    // at this moment the strike is worth less than the Rogue.
    let hunted =
      SimState.aliveHostiles w
      |> List.filter (fun h -> h.Engaged && h.Target = Some e.Id)
      |> List.sortBy (fun h -> Pos.chebyshev e.Pos h.Pos)

    let lastResort =
      if hunted.IsEmpty || e.Health * 4 >= e.MaxHealth then
        None
      elif canUse e "Vanish" then
        Some(cmd at (UseAbility(e.Id, "Vanish", e.Id)))
      else
        hunted
        |> List.tryFind (fun h -> canUse e "Blind" && SimState.withinRange 3 e h)
        |> Option.map (fun h -> cmd at (UseAbility(e.Id, "Blind", h.Id)))

    match caster with
    // Vanish bought seconds, not damage: while the stealth runs this lane places
    // nothing, because the first strike would be what ends it. The rotation
    // resumes on its own once the aura has run out or the fight is over.
    | _ when SimState.isStealthed e -> []
    | _ when lastResort.IsSome -> lastResort.Value
    | Some c when has e "Kick" && canUse e "Kick" && SimState.withinRange 1 e c ->
      cmd at (UseAbility(e.Id, "Kick", c.Id))
    | Some c when has e "Gouge" && canUse e "Gouge" && SimState.withinRange 1 e c ->
      cmd at (UseAbility(e.Id, "Gouge", c.Id))
    | Some c when has e "Kick" && canUse e "Kick" && SimState.canReach 4 e c w ->
      cmd at (MoveTo(e.Id, SimState.approachTile e c.Pos w |> Option.defaultValue c.Pos))
    | _ when openWith.IsSome && opener.IsSome ->
      cmd at (UseAbility(e.Id, openWith.Value, opener.Value.Id))
    | _ when stun.IsSome && uncontrolled.IsSome ->
      cmd at (UseAbility(e.Id, stun.Value, uncontrolled.Value.Id))
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
        let walkRound = builders e |> List.tryHead

        let circle =
          match walkRound with
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
            // A Rogue with a positional strike goes round the back rather than into the
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

      // One interrupt per cast, and the assignee is whoever the Party agreed on
      // (`interruptAssignments`): a melee kick outranks a Counterspell, so the long
      // cooldown is spent only when no sword can reach.
      let myInterrupts = interruptTargets w e

      let casting =
        SimState.aliveHostiles w
        |> List.filter (fun h -> h.Casting |> Option.exists (fun c -> c.Ability.Interruptible))
        |> List.filter (fun h -> Set.contains h.Id myInterrupts)
        |> List.filter (fun h -> counterspell |> Option.exists (fun a -> SimState.canReach a.Range e h w))
        |> List.sortBy (fun h -> Pos.chebyshev e.Pos h.Pos)
        |> List.tryHead

      // A shot deals damage. Sap, Polymorph and the Rogue's stuns reach further
      // than a sword, but firing one as an attack spends an opener on nothing —
      // `prePull` is the only place an opener belongs.
      // Multi-Shot pays per target, so on one body it is a worse Arcane Shot for
      // twice the mana. Fire it only when a second hostile is near enough to be
      // caught by it.
      let crowded =
        SimState.aliveHostiles w
        |> List.filter (fun h -> h.Faction <> e.Faction && Pos.chebyshev f.Pos h.Pos <= 8)
        |> List.length
        >= 2

      /// A row that sweeps: the shot the Hunter holds is the only one this
      /// rotation presses, so the name stays the shot's even though the shape is
      /// the engine's and the bear carries one too (`Effect.MultiStrike`).
      let isSweep (a: Ability) =
        a.Effects |> List.exists (function MultiStrike _ -> true | _ -> false)

      // A row that exists to open a window is not a damage spell, whatever hit it
      // carries. `perCast` divides by `max 1 castTicks`, so a zero-cast ability's
      // score is its damage times a thousand and it heads every ranking it appears
      // in — measured, the Mage led with Arcane Rupture at L36 and never channelled
      // the missile the window was for, and at L42 the AI stop-start was the row
      // itself. The window is pressed by its own rule below; the rotation keeps the
      // spells it buffs.
      let isWindow (a: Ability) =
        a.Effects
        |> List.exists (function
          | Rupture _
          | CastHaste _ -> true
          | _ -> false)

      /// A melee-range instant damage row, when the actor can already reach with it
      /// and holds the healer seat.
      ///
      /// `Range > 1` below is how "a shot is fired from where you stand" was said,
      /// and it keeps the Rank-1 swings a melee rotation owns out of a caster's
      /// rotation. Judgement is the row the rule gets wrong: a Paladin's only damage
      /// button is instant at `Range = 1` (`Content.fs:796`, the file's standing
      /// approximation — the research record in `.pi/research/octowow/paladin.md`
      /// gives a cooldown for it and no range), so a Paladin healer had nothing to
      /// press and spent the fight's spare mana on nothing at all. Reach is the
      /// gate and never a reason to walk in, the line the Hunter's fallback draws
      /// for Raptor Strike.
      ///
      /// The healer role is part of the test on purpose: Raptor Strike is the same
      /// shape, and admitting it here would put an instant swing at the head of a
      /// ranged ranking that already reaches it through `meleeOrders`.
      let meleeShot (a: Ability) =
        e.Role = Some Healer
        && a.CastTicks <= ticks 0
        && SimState.canReach a.Range e f w

      let shots =
        e.Abilities
        |> List.filter (fun a ->
          a.TargetKind = Foe
          && (a.Range > 1 || meleeShot a)
          && not (interrupts a)
          && not (isWindow a)
          && damageOf a > 0
          && (crowded || not (isSweep a)))
        |> List.filter (fun a -> canUse e a.Name)

      // Mana is what a caster mob spends to be dangerous, so draining it is a
      // damage cut that no heal answers. Re-applied only once the last sting has
      // lapsed: the aura refreshes rather than stacks, so a second sting on a
      // stung target buys nothing.
      let sting =
        e.Abilities
        |> List.filter (fun a ->
          a.TargetKind = Foe
          && a.Effects |> List.exists (function DrainResource _ -> true | _ -> false)
          && canUse e a.Name)
        |> List.tryFind (fun a -> SimState.canReach a.Range e f w)

      // Damage that keeps landing while the caster does something else, maintained
      // the way the sting is: pressed while the target carries no dot, and not
      // re-pressed while one ticks, because the aura refreshes rather than stacks.
      // This is the rule that makes an Affliction Warlock an Affliction Warlock —
      // without it Corruption and Curse of Agony are never cast at all, since
      // `shots` requires `damageOf > 0` and a dot's direct hit is nothing.
      //
      // `Aura.Corrupted` is deliberately shared by every dot (`Content.fs`: the two
      // Warlock rows refresh each other), so "the target already has a dot" is one
      // aura test and the hardest dot is the one worth applying.
      let dot =
        e.Abilities
        |> List.filter (fun a ->
          a.TargetKind = Foe
          // Only the dots the ranking is blind to. `damageOf` reads a direct hit and
          // nothing else, so a spell that carries both — Fireball's burn, Immolate's
          // opening hit — is already ranked on that hit and must not be re-pressed
          // here; doing so preempted Arcane Missiles with Fireball on every pull.
          // What this rule exists for is the *pure* dot: `damageOf` 0, `dotOf` not.
          && damageOf a = 0
          && dotOf a > 0
          && canUse e a.Name)
        |> List.filter (fun a -> SimState.canReach a.Range e f w)
        |> List.sortByDescending dotOf
        |> List.tryHead

      let worthDotting =
        f.Health > 0
        && not (f.Auras |> List.exists (function Corrupted _ -> true | _ -> false))
        && dot.IsSome

      let worthStinging =
        f.MaxResource > 0
        && f.Resource > 0
        && not (f.Auras |> List.exists (function Drained _ -> true | _ -> false))
        && sting.IsSome

      let paid = shots |> List.filter (fun a -> a.ResourceCost > 0)
      let free = shots |> List.filter (fun a -> a.ResourceCost = 0)
      // 15% is where a real caster stops playing for damage a second and starts
      // playing for damage a point of mana: above it, cast the best spell; below
      // it, cast the cheapest that still lands, and let the five-second rule do
      // the refilling.
      let lowMana = e.MaxResource > 0 && e.Resource * 100 < e.MaxResource * 15

      /// Whether the caster can stand still for a cast this Tick. A body carrying a
      /// destination or a path is walking, and a move cancels a cast in progress, so
      /// an instant is the only damage a walking caster keeps.
      let stationary = e.Destination.IsNone && e.Path.IsEmpty

      /// The shots worth ranking by cast time. `perCast` divides by `CastTicks`, so
      /// **any** instant scores its damage times a thousand and heads every ranking
      /// it appears in — measured, that made Flame Shock (160 mana, 126 damage, 0.79
      /// a point) outrank Lightning Bolt (135, 190, 1.41) on every Tick the Shaman
      /// could have stood still: 493 presses and 78 512 mana, 18.5% of all spend, on
      /// the worse spell (`sim shmana 30`, `.pi/research/octowow/
      /// shaman-damage-and-interrupts.md` §4). An instant's merit is that it can be
      /// pressed while walking, which is exactly the case below where it still wins.
      /// Read off `CastTicks`, not names, so a second shaman row or a Hunter's Aimed
      /// Shot gets the same rule.
      let standing = paid |> List.filter (fun a -> a.CastTicks > ticks 0)

      let ranked =
        if lowMana then
          List.sortByDescending perMana
        else
          List.sortByDescending perCast

      let shot =
        (if stationary && not standing.IsEmpty then standing else paid)
        |> ranked
        |> List.tryHead
        |> Option.orElse (free |> List.sortByDescending damageOf |> List.tryHead)

      // The spell the rotation casts when mana is not the binding constraint — the
      // one an "Arcane Missiles" window is actually worth pressing for. `shot` is
      // the wrong gate: below 15% mana it flips to a per-mana ordering, and on those
      // ticks pressing the window is precisely what stops the channel it is for.
      // Measured with `shot` as the gate: 87 presses a fight at library and not one
      // missile to ride them.
      let channels =
        paid
        |> List.sortByDescending perCast
        |> List.tryHead
        |> Option.exists (fun b -> b.Name = "Arcane Missiles")

      // A buff that names its spell is a reason to cast that spell. Nightfall
      // makes the next Shadow Bolt instant and free of cast time, and the ranking
      // above never picks Shadow Bolt at all: `damageOf` counts only an ability's
      // direct hit, so Immolate's 147 over a 30-tick cast scores 4900 per cast
      // tick against Shadow Bolt's 4780 and wins every time. A Warlock therefore
      // casts Immolate and nothing else, and the proc would expire unused on a
      // spell the rotation never reaches. Only a scoped buff gets here (`Only` is
      // `Some` for Nightfall and nothing else), so no rotation that exists today
      // moves.
      let scopedShot =
        let held = Aura.empower e.Auras

        if held.Instant then
          held.Only
          |> Option.bind (fun named ->
            e.Abilities |> List.tryFind (fun a -> a.Name = named && canUse e a.Name))
        else
          None

      // Presence of Mind already rides `scopedShot`'s lane above when a spell is
      // named; Arcane Power names none, so it needs its own rule. Pressed before the
      // rotation, not after it: it costs one tick and buys twenty seconds of a 23%
      // shorter cast, so waiting for an idle tick means never spending it at all (a
      // Mage with three spells on a 3 s cast has something to press almost every
      // tick). Never re-pressed while the window is up — 20 s of a 3-minute cooldown
      // is 11% uptime, so a second press inside it buys nothing.
      let quicken =
        e.Abilities
        |> List.filter (fun a ->
          a.Effects |> List.exists (function CastHaste _ -> true | _ -> false))
        |> List.tryFind (fun a ->
          canUse e a.Name
          && e.Casting.IsNone
          && not (e.Auras |> List.exists (function Casting _ -> true | _ -> false)))

      // Arcane Rupture: eight seconds of harder — and dearer — Arcane Missiles on a 15
      // second cooldown, so it is up for roughly half of a fight and the aura is the
      // only thing worth checking. Pressed before the rotation for the same reason as
      // Arcane Power: the buff has to be standing when the channel starts.
      let rupture =
        e.Abilities
        |> List.filter (fun a ->
          a.Effects |> List.exists (function Rupture _ -> true | _ -> false))
        |> List.tryFind (fun a ->
          canUse e a.Name
          && e.Casting.IsNone
          && SimState.canReach a.Range e f w
          && not (e.Auras |> List.exists (function Ruptured _ -> true | _ -> false))
          // Only when the rotation is picking the spell this row rides. The kit
          // carries Arcane Missiles at every level, but the ranking picks Fireball
          // while Fireball is the better spell per cast — at L36 it does — and a
          // press there buys nothing but 150 mana and a tick.
          && channels)

      match counterspell, casting with
      | Some a, Some c when canUse e a.Name -> cmd at (UseAbility(e.Id, a.Name, c.Id))
      | _ when quicken.IsSome -> cmd at (UseAbility(e.Id, quicken.Value.Name, e.Id))
      | _ when rupture.IsSome -> cmd at (UseAbility(e.Id, rupture.Value.Name, f.Id))
      | _ when worthStinging -> cmd at (UseAbility(e.Id, sting.Value.Name, f.Id))
      | _ when scopedShot |> Option.exists (fun a -> SimState.canReach a.Range e f w) ->
        cmd at (UseAbility(e.Id, scopedShot.Value.Name, f.Id))
      | _ when worthDotting -> cmd at (UseAbility(e.Id, dot.Value.Name, f.Id))
      | _ ->
        match shot with
        | Some a when SimState.canReach a.Range e f w -> cmd at (UseAbility(e.Id, a.Name, f.Id))
        | _ when not (SimState.withinRange 8 e f) -> cmd at (MoveTo(e.Id, f.Pos))
        | _ -> []

  /// One Tick of orders for the whole Party.
  /// The hunter's pet and the warlock's demon. `Summon` builds a real Party entity
  /// owned by its caller, so it can be killed and it has to be healed; nothing else
  /// in the roster brings a body that outlives its owner's swing. Both orders sit
  /// outside the `target` gate on purpose — a pet called after the opener is called
  /// too late, and a pet mended only while a hostile is up is a pet that dies between
  /// packs.
  ///
  /// One body per run. Re-summoning would leave the corpse on the floor with a live
  /// body beside it, and nothing in the engine removes an entity, so the `None` arm
  /// below checks the *corpse* rather than the living party. That costs the Warlock
  /// more than the Hunter: a demon is a 10-second recast in the era, and here a dead
  /// Imp stays dead for the rest of the run. Fixing it means removing dead summons
  /// from `w.Entities`, which is a change to the engine rather than to this order set.
  let private petOrders (w: World) (party: Entity list) (at: int<tick>) =
    party
    |> List.collect (fun h ->
      // Which row this hero calls its body with. The Hunter and the Warlock differ in
      // exactly two ways: the row name, and that only the Hunter has a mending spell.
      let call =
        match h.Abilities |> List.map (fun a -> a.Name) with
        | rows when List.contains "Summon Pet" rows -> Some "Summon Pet"
        | rows when List.contains "Summon Demon" rows -> Some "Summon Demon"
        | _ -> None

      match call with
      | None -> []
      | Some row ->
        match party |> List.filter (fun m -> m.Owner = Some h.Id) |> List.tryHead with
        | None ->
          if w.Entities |> List.exists (fun e -> e.Owner = Some h.Id) then
            // The body died. No second one while its corpse is still on the grid.
            []
          elif canUse h row then
            cmd at (UseAbility(h.Id, row, h.Id))
          else
            []
        | Some p when has h "Mend Pet" && p.Health * 10 < p.MaxHealth * 7 && canUse h "Mend Pet" ->
          cmd at (UseAbility(h.Id, "Mend Pet", p.Id))
        | Some _ -> [])

  let decide (w: World) : Command list =
    let at = w.Tick + ticks 1
    let party = SimState.aliveParty w

    // The last Shaman standing, with Reincarnation ready, is not a fighter any more:
    // it is a corpse that has not happened yet. Every order set below assumes a
    // party that means to win the pull, and two of them — the step-aside shuffle and
    // the `leaderless` walk to the nearest hostile — would march it into the pack it
    // has to die away from. One filter silences all of them; `fleeing` below is the
    // only order it takes.
    let dying =
      match party with
      | [ last ] when last.Role = Some Healer ->
        last.Abilities
        |> List.filter (fun a -> a.TargetKind = Self && Ability.resurrects a && canUse last a.Name)
        |> function
          | [] -> None
          | _ -> Some last
      | _ -> None

    // Only members who can start something get orders: re-issuing a cast mid-cast
    // is a queueing policy, not a decision. A totem is not a member — it has no
    // abilities and must never be told to walk, so it takes no orders at all.
    let actors =
      party
      |> List.filter (fun e -> e.Casting.IsNone && not (SimState.isSleeping e) && e.Totem.IsNone)
      |> List.filter (fun e -> not (dying |> Option.exists (fun d -> d.Id = e.Id)))
    let role r = actors |> List.filter (fun e -> e.Role = Some r)
    // What a member *is* follows from where it can hurt something, not from which
    // abilities it happens to hold. A Hunter carries Raptor Strike beside a full
    // Marksmanship kit, and the old test — "holds a Foe ability at range 1" —
    // claimed it for the melee lane, so every fight it swung a sword and never
    // fired a shot: 0 of its 8 ranged rows were ever cast, 97% mana at the end of a
    // run and the lowest damage in the party. Damage is the qualifier, not reach
    // alone, because a Rogue's Sap also reaches 3 tiles and a Rogue is still melee;
    // Sap deals nothing and an Arcane Shot deals damage, and `damageOf` is the
    // function that already says which is which.
    let ranged (e: Entity) =
      e.Abilities
      |> List.exists (fun a -> a.TargetKind = Foe && a.Range > 1 && damageOf a > 0 && not (interrupts a))

    // Exclusive, not overlapping: issuing both order sets puts two commands for one
    // actor in the same Tick. A ranged member keeps its melee row as a fallback and
    // gets it only from the empty case below, which is one command either way.
    let melee (e: Entity) =
      not (ranged e) && e.Abilities |> List.exists (fun a -> a.TargetKind = Foe && a.Range <= 1)

    let tank = actors |> List.tryFind (fun e -> e.Role = Some Tank)

    // A pull waits for the Party. A Tank that walks off the moment the last Mob
    // dies opens the next pack with the healer still down the corridor, or still
    // empty — and out-of-combat regeneration is 20% of the pool a second, so a
    // zeroed pool is whole again in five seconds and the wait costs one short stop
    // before every pull. Waiting empties
    // `hostiles`, which is what stops every pull at once: no opener from
    // `prePull`, no ranged member walking up to a focus it can already see, and
    // no advance. The Tank's route is cleared as well, because a Tank already
    // walking keeps stepping toward a goal its AI no longer renews.
    /// The leader waits for the whole party to be *whole*, not merely to be able to
    /// cast: health as well as mana, because five seconds of regeneration refills
    /// both and a pull begun at 60% health is a pull the healer pays for. Rage and
    /// Energy are excluded on purpose — a Warrior's bar is empty by design out of
    /// combat, so waiting on it would park the party forever — and so are the dead,
    /// who never refill.
    let toppedUp =
      party
      |> List.filter (fun m -> SimState.alive m)
      |> List.exists (fun m ->
        m.Health < m.MaxHealth
        || (m.ResourceKind = Mana && m.MaxResource > 0 && m.Resource < m.MaxResource))

    let drinking = toppedUp

    // The wait exists so a pull does not start with the healer twelve tiles back
    // and out of triage range, so only the healers gate it. Reading every member
    // froze the Party outright: in a one-wide corridor a Dps 9 tiles back (one past
    // `leash`) held `waiting` forever while the member in front of it stood idle at
    // 7 tiles with no order at all, so the file could never close and nothing could
    // ever aggro. Library L36 parked that way for 32000 ticks, three mobs three
    // tiles away with line of sight clear.
    let waiting =
      not (SimState.inCombat w)
      && (drinking
          || match tank with
             | None -> false
             | Some t ->
               party
               |> List.exists (fun m -> m.Role = Some Healer && Pos.chebyshev m.Pos t.Pos > leash))

    // Only fight what the Tank can reach from where it stands. Beyond the leash
    // the Party walks to the fight together instead. Everything downstream reads
    // this list, so no member can wander off to a different mob.
    let hostiles =
      if waiting then
        []
      else
        match tank with
        | Some t ->
          SimState.aliveHostiles w
          |> List.filter (fun h -> Some h.Id = t.Target || SimState.canReach leash t h w)
        | None -> SimState.aliveHostiles w

    let target = strikeTarget w tank hostiles
    let onTank = tankTarget w tank hostiles

    // Pre-pull. While nothing has noticed the Party yet, thin the pack and open
    // from range instead of walking the whole room awake: a Polymorph, Roots or a
    // Gouge takes one enemy out of the fight before it starts, and one ranged
    // member opens on the Mob the Party means to kill first. A Scarlet wing holds
    // 43 hostiles, and fighting them in fives is what wipes the Party.
    let prePull, holdForCc =
      // `Engaged` beside the target: a pack that has noticed the party — proximity
      // aggro sets the flag even before its first target resolves — is not a pack
      // an opener can still land on.
      let engaged =
        SimState.aliveHostiles w
        |> List.exists (fun h -> h.Target.IsSome || h.Engaged)

      let controlling =
        fun (a: Ability) ->
          a.Name <> "Blind"
          && a.Effects
             |> List.exists (function
               | Polymorph _ | Root _ | Sleep _ -> true
               | _ -> false)

      let reaches (e: Entity) (h: Entity) (a: Ability) =
        Pos.chebyshev e.Pos h.Pos <= a.Range && Path.lineOfSight w.Grid e.Pos h.Pos

      // Stealth is not modelled. The Rogue counts as stealthed for the whole
      // pre-pull window — no hostile has noticed the party, which is the only state
      // `prePull` runs in — and only hounds see through it, which stands in for the
      // Improved Stealth talent. That is why Sap needs no facing check and why
      // Distract is not implemented.
      //
      // shortcut: stealth is "not in combat, and no hound within 6 tiles"; add a
      // real stealth state if facing or Distract ever matters.
      let unnoticed (e: Entity) (a: Ability) =
        a.Name <> "Sap"
        || not (
          SimState.aliveHostiles w
          |> List.exists (fun h -> h.Name.Contains "Hound" && Pos.chebyshev h.Pos e.Pos <= 6)
        )

      // Sap earns the pull's first slot even when a Polymorph is in reach: it is the
      // only opener that starts nothing, and its minute-long cooldown means it must
      // not be spent on a pack a shorter cooldown already covers.
      let preference (a: Ability) =
        match a.Name with
        | "Sap" -> 0
        | "Polymorph" | "Entangling Roots" | "Hibernate" | "Sleep" -> 1
        | _ -> 2

      match target with
      | Some focus when not engaged ->
        // The dangerous ones first: hardest hitters carry the most health here, and
        // `canAct` keeps a body already slept out of the pool — without it the same
        // mob heads the list every tick and the opener is spent on a sleeper.
        // Scoped to `hostiles`, the pull the leash already called for: the whole
        // dungeon's biggest body is three rooms away and not this pull's to control.
        let surplus =
          hostiles
          |> List.filter (fun h -> h.Id <> focus.Id && h.Casting.IsNone && SimState.canAct h)
          |> List.sortByDescending (fun h -> h.MaxHealth)

        // The opener is a sequence, not one cast (operator direction): the Rogue
        // Saps first, the Mage Polymorphs a caster when the pack has one, a Druid
        // Hibernates what its creature type allows. One control per actor and one
        // target each — two on the same mob waste one — and the candidates scan in
        // danger order, not just the first: the biggest body in a pack is often the
        // one control cannot land on (an Elemental Lasher), and insisting on it
        // spends the opener on an "is immune" line instead of the Raptor beside it.
        let isCaster (h: Entity) = h.Abilities |> List.exists (fun a -> a.CastTicks > ticks 0)

        // An actor openers only what its control reaches from the Tank's side of
        // the door — walking past a stopped Tank into the pack does not converge
        // (measured: the caster oscillates at the corridor mouth), but walking up
        // to the Tank does. So every chosen target is in its ability's reach *from
        // the Tank's position*, with line of sight from there, and the out-of-range
        // actor's order is a walk to the Tank. Arrival is ±1 tile: a plan that lands
        // one short ends the hold and the Tank walks in as before — the old
        // no-opener pull, not a stall. Without a Tank there is no door to reach from,
        // so a leaderless party openers only what it can already reach.
        let doorReached (t: Entity) (a: Ability) (h: Entity) =
          a.Range >= Pos.chebyshev t.Pos h.Pos && Path.lineOfSight w.Grid t.Pos h.Pos

        let ranked =
          actors
          |> List.collect (fun e ->
            e.Abilities
            |> List.filter controlling
            |> List.filter (fun a -> canUse e a.Name && unnoticed e a)
            |> List.map (fun a -> e, a))
          |> List.sortBy (fun (_, a) -> preference a)

        let rec pick taken acc =
          function
          | [] -> List.rev acc
          | (e, a) :: rest ->
            if acc |> List.exists (fun (e', _, _) -> e'.Id = e.Id) then
              pick taken acc rest
            else
              let targets =
                surplus
                |> List.filter (fun h ->
                  not (List.contains h.Id taken)
                  && canControl e a h
                  && (match tank with
                      | Some t -> doorReached t a h
                      | None -> reaches e h a))
                |> (if a.Name = "Polymorph" then
                      List.sortByDescending (fun h -> (isCaster h, h.MaxHealth))
                    else
                      List.sortByDescending (fun h -> h.MaxHealth))

              match targets with
              | h :: _ -> pick (h.Id :: taken) ((e, a, h) :: acc) rest
              | [] -> pick taken acc rest

        let chosen = pick [] [] ranked

        // In range the cast flies this Tick, with a Stop first to drop a stale
        // walking goal. Out of range the walk is to the Tank — the door the plan
        // was measured from — and the pull is held until the opener has landed.
        // The hold ends by itself: travel ends beside the Tank, in range and the
        // cast flies, or one short and the Tank walks in as before. Without a Tank
        // every chosen opener is already in range, so there is nothing to hold.
        let cc, walking, hold =
          chosen
          |> List.fold
            (fun (cc, walking, hold) (e, a, h) ->
              if reaches e h a then
                ( cc
                  @ [ { ApplyAt = at; Kind = Stop e.Id }
                      { ApplyAt = at; Kind = UseAbility(e.Id, a.Name, h.Id) } ],
                  walking,
                  hold )
              else
                match tank with
                | Some t when Pos.chebyshev e.Pos t.Pos <= 1 ->
                    // Beside the Tank and still out of reach: the door's vantage
                    // cannot deliver this plan (measured: a corner wall between the
                    // mage's approach tile and the target re-books forever and holds
                    // the pull closed). Give the plan up -- no walk, no hold, the
                    // Tank walks in the old way.
                    (cc, walking, hold)
                | Some t ->
                    // The walk is to the Tank's side, the door the plan was
                    // measured from. A route that just dropped stale does not hold
                    // the pull: the Tank creeps a tile whenever any walker stalls,
                    // so a walk that can never converge lets the window close the
                    // old way rather than parking the party at the door forever.
                    let holds = e.StallTicks <= ticks 0
                    (cc, walking @ [ { ApplyAt = at; Kind = MoveTo(e.Id, t.Pos) } ], hold || holds)
                | None -> (cc, walking, hold))
            ([], [], false)

        // The rest of the hold is the cast in flight, so the pack opens on the
        // sheep rather than on it. No other branch orders a controlling ability, so
        // a casting one here is by construction the pre-pull opener.
        let openerFlying =
          party
          |> List.exists (fun e -> e.Casting |> Option.exists (fun c -> controlling c.Ability))

        // With the opener landed the Tank closes the gap the way the kit is built
        // to (operator direction): a free leap that stuns the focus, pays rage and
        // puts threat on it, instead of eight walked ticks at the head of the
        // party. The old measurement that kept this branch empty charged into a
        // pack with no control on it; the opener is what makes the landing safe.
        // The gate on "multiple groups" is the landing spot: any hostile the
        // leap's arrival would notice (within the aggro radius, in sight) that the
        // leash had not already called for the pull means a second group would
        // come with it, and the Tank walks in the old way instead.
        let chargeIn: Command list =
          let controlled h =
            h.Auras
            |> List.exists (function
              | Sleeping _ | Polymorphed _ | Rooted _ -> true
              | _ -> false)

          let openersLanded = hostiles |> List.exists controlled

          // The charge goes to the nearest hostile the openers did *not* take:
          // `strikeTarget`'s pre-fight ties put the just-sheeped mob at the head of
          // the pull, and charging a sleeper is a refused cast that also breaks the
          // sheep. The landing is beside that foe, so the only hostiles it can
          // notice are the foe's own cluster (three tiles around it) -- the same
          // group a walk-in would engage. A second *group* inside that radius is
          // what the gate refuses: anything outside the cluster must be beyond the
          // aggro radius of the landing, with sight of it.
          let foe =
            match tank with
            | Some t ->
              hostiles
              |> List.filter (fun h -> not (controlled h))
              |> List.sortBy (fun h -> Pos.chebyshev t.Pos h.Pos)
              |> List.tryHead
            | None -> None

          let cluster h = Option.forall (fun f -> Pos.chebyshev h.Pos f.Pos <= 3) foe

          let landingSafe =
            match foe with
            | Some f ->
              SimState.aliveHostiles w
              |> List.filter (fun h -> h.Id <> f.Id && not (cluster h))
              |> List.forall (fun h ->
                Pos.chebyshev h.Pos f.Pos > SimState.aggroRadius + 1
                || not (Path.lineOfSight w.Grid h.Pos f.Pos))
            | None -> false

          match tank, foe with
          | Some t, Some f when openersLanded && landingSafe && not hold && not openerFlying ->
            t.Abilities
            |> List.tryFind (fun a -> a.Name = "Charge")
            |> Option.filter (fun a -> canUse t a.Name && reaches t f a)
            |> Option.map (fun a ->
              [ { ApplyAt = at; Kind = Stop t.Id }
                { ApplyAt = at; Kind = UseAbility(t.Id, a.Name, f.Id) } ])
            |> Option.defaultValue []
          | _ -> []

        [ yield! cc
          yield! walking
          yield! chargeIn ],
        // `cc` too: the booking Tick is before the cast exists, and the damage
        // lane opening on that same Tick is what the opener was booked to precede.
        not (List.isEmpty cc)
        || hold
        || openerFlying
        || not (List.isEmpty chargeIn)
      | _ -> [], false

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
        // Gated as a whole, not only its explore arm: `onTank`'s `Some f -> Some
        // f.Pos` bypassed the wait/hold below and kept the Tank walking into the
        // pack it was being held for.
        let spot =
          if waiting || holdForCc then
            None
          else
            match onTank with
            | Some f -> Some f.Pos
            | None ->
            // The nearest mob is not the next pull: a wing is rooms behind walls,
            // and the mob three tiles past one is nearer than the mob across the
            // floor you are standing on. Aiming at the nearest-by-distance goal
            // parked the Party against a wall for the rest of the run — `graveyard`
            // seed 12 ended with five healthy members, 36 mobs alive, and the Tank
            // alternating its first step between two tiles for 16 000 ticks, its
            // goal 8 tiles away and behind a wall with no sight of it. What it can
            // see is what it may pull; only when it can see nothing at all does the
            // nearest dead body anywhere become the goal, which is the explore case.
            SimState.aliveHostiles w
            |> List.sortBy (fun h ->
              (if Path.lineOfSight w.Grid t.Pos h.Pos then 0 else 1), Pos.chebyshev t.Pos h.Pos)
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

        [ // Holding for the Party clears the route rather than merely declining to
          // renew it: a Tank mid-corridor keeps stepping toward its old goal, and
          // walks into the pack it was meant to wait for.
          if waiting || holdForCc then yield! cmd at (Stop t.Id)

          match reposition with
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

            // A settled ally is not a wall. `claimedByOther` exists to stop two
            // bodies *stopping* on one tile, and the transit case is already legal
            // by design — `SimTick.advanceMovement` separates a cross-over. Blocking
            // a route on it is what seals a Tank behind its own Party in a one-wide
            // corridor, which at `graveyard` median L33 parked the whole Party at
            // `at=(26,186) goal=(36,175) path=0 dest=None stall=4` with eighteen mobs
            // reachable and twelve tiles of open floor in front of it. Mobs still
            // block: walking onto one is not a road.
            let takenByHostile n =
              w.Entities
              |> List.exists (fun e ->
                SimState.alive e && e.Id <> t.Id && e.Pos = n && e.Destination.IsNone && e.Faction = Hostile)

            let routeThrough to_ = (Path.astar w.Grid takenByHostile t.Pos to_).Path

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
                  // Second try: the same approach tile with the Party stepped
                  // through instead of around.
                  let through =
                    Path.neighbours
                      (fun n -> Grid.isFloor w.Grid n && not (takenByHostile n))
                      p
                    |> List.sortBy (fun n -> Pos.chebyshev t.Pos n)
                    |> List.tryFind (fun n -> not (List.isEmpty (routeThrough n)))

                  match through with
                  | Some a -> Some a
                  | None ->
                    // Nobody alive in the way: walk to the last tile of the corridor
                    // before whoever is holding the door. The search run is over
                    // open floor, so it cannot fail the way a blocked one does. A
                    // settled *mob* holds the door as surely as an ally does — the
                    // guard standing in a one-wide gap seals its room — and the
                    // truncation used to stop only at allies, so the aim fell through
                    // to the mob's own tile, the search returned nothing, and the
                    // Tank repeated `MoveTo` on an unreachable goal for the rest of
                    // the run. Walking up to the door is enough: the guard aggros
                    // from six tiles and the fight that opens the room happens there.
                    //
                    // Investigated, not fixed: the `i > 0` guard here leaves
                    // `| _ -> Some p` to aim at the mob's own tile when the *first*
                    // step of the free route is already blocked (`free.[0]`, since
                    // `Path.walkBack` excludes the start tile), which is the absorbing
                    // `MoveTo`-on-an-unreachable-goal loop above. Measured: changing it
                    // to stand instead (`| Some 0 -> None`) left all eleven park cells
                    // byte-identical, so this branch is not what parks them and the
                    // guard stays as it is. See §9 item 1 for the surviving signature.
                    let free = (Path.astar w.Grid (fun _ -> false) t.Pos p).Path

                    match free |> List.tryFindIndex (fun n -> taken n || takenByHostile n) with
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
          // A Tank sealed behind its own Party cannot drain the corridor one ally
          // at a time: in a one-wide tube only the ally at the *back* has floor to
          // step onto, and it is nowhere near the Tank. Seed 1 graveyard level 60
          // ended up exactly there — `goal=(36,175) path=0 dest=None stall=17`, the
          // route existing over free floor and not over claimed floor, with three
          // allies stacked behind the Tank. Nothing was engaged, so the whole queue
          // may step away from it and the tube empties from the back.
          // A Tank whose next step is a settled body is stuck just as surely as one
          // with no route at all. `stepToward` returns early while `Destination` is
          // set — it never re-plans, so it never reports a stall — and a Tank
          // waiting on a body that has no orders can wait forever. Library seed 1
          // parked exactly like that: `dest=(16,125)` onto a Druid that was itself
          // blocked by a goal-less Mage, `path` 30 steps long, so this test called it
          // walking and no aside ever fired. Count a held `Destination` as stuck.
          let heldStep =
            match t.Destination with
            | Some d ->
              w.Entities
              |> List.exists (fun o ->
                o.Id <> t.Id && SimState.alive o && o.Pos = d && o.Destination.IsNone)
            | None -> false

          let stuckWithGoal =
            t.Goal.IsSome && (heldStep || (t.Path.IsEmpty && t.Destination.IsNone))

          let reach = if stuckWithGoal then 6 else 1

          // Stepping aside is for unsealing a Tank that is walking. While the
          // Party is only drinking it is moving *nobody*: a shuffle beside the
          // first pack wakes it, and the pull starts with the healer at a quarter
          // bar — the exact thing the wait exists to prevent. The leash case is
          // not that: members out of range still have to close up.
          if t.Goal.IsSome || wantsToMove || (waiting && not drinking) then
            for e in role Healer @ role Dps do
              if e.Id <> t.Id && Pos.chebyshev e.Pos t.Pos <= reach then
                // Distance alone ties: two tiles either side of the Tank are both
                // one away, and an unstable sort then shuttles the ally between
                // them every tick while the Tank stays blocked. A fixed tie-break
                // makes the ally keep walking in one direction until it is clear.
                //
                // Stepping aside needs a tile that is empty *now*, not one whose
                // occupant is already moving: `claimedByOther` lets a mover keep
                // its tile free so the party can trail in single file, and a
                // cluster where every mover is blocked by another mover then
                // picks the same occupied tiles over and over. That froze the
                // Tank against the Mage in graveyard for 40000 ticks with the
                // whole party alive and nothing engaged (`probe` on seed 1).
                let occupied p =
                  w.Entities |> List.exists (fun o -> o.Id <> e.Id && SimState.alive o && o.Pos = p)

                let aside =
                  Path.neighbours (fun p -> Grid.isFloor w.Grid p && not (occupied p)) e.Pos
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
            stuckWithGoal || (wantsToMove && t.Goal.IsNone)

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

    // Every recovery rule below hangs off the Tank: it owns the follow leash, the
    // step-aside shuffle and the walk to the next mob. With the Tank dead all of
    // them go quiet at once, so survivors stand on a cleared floor for the rest of
    // the run — the mobs they have not met never notice them, `target` stays None,
    // and no member is issued anything. Two survivors sat in a door alcove for
    // 24 000 ticks that way. Whoever is left leads itself to the nearest hostile.
    let leaderless =
      if tank.IsSome || party.IsEmpty then
        []
      else
        match
          SimState.aliveHostiles w
          |> List.sortBy (fun h -> party |> List.map (fun e -> Pos.chebyshev e.Pos h.Pos) |> List.min)
        with
        | [] -> []
        | nearest :: _ ->
          actors
          |> List.collect (fun e ->
            // Only someone with no plan at all: re-issuing a walk every tick pays
            // a whole-map search per member, and a member already walking keeps
            // its route until it runs out.
            if not e.Destination.IsNone || not e.Path.IsEmpty then
              []
            else
              match SimState.approachTile e nearest.Pos w with
              | Some p when p <> e.Pos -> cmd at (MoveTo(e.Id, p))
              | _ -> [])

    // Waiting for a straggler used to be an order to stand still. `waiting` Stops the
    // Tank and issues nothing else, so a member that fell past the leash was never
    // given a walk and could never come inside it — the wait had no exit. Library L36
    // and graveyard L33 parked there for the whole tick cap: five healthy members,
    // nobody drinking, the healer 11 tiles back and the Tank idle. So the wait is a
    // regroup march: whoever is past the leash walks in to the Tank.
    //
    // This cannot collide with the step-aside shuffle: that reaches 6 tiles at most
    // and this only fires past the leash of 8, so no actor gets two walks in a tick.
    let regroup =
      match tank with
      | Some t when waiting && not drinking ->
        party
        |> List.collect (fun m ->
          if
            m.Id = t.Id
            || Pos.chebyshev m.Pos t.Pos <= leash
            || m.Destination.IsSome
            || not m.Path.IsEmpty
          then
            // Only someone with no plan at all: a whole-map search every tick is the
            // expensive way to take three steps, and a walker keeps its route until
            // it runs out.
            []
          else
            let taken p =
              w.Entities |> List.exists (fun o -> o.Id <> m.Id && SimState.alive o && o.Pos = p)

            Path.neighbours (fun p -> Grid.isFloor w.Grid p && not (taken p)) t.Pos
            |> List.sortBy (fun p -> Pos.chebyshev p m.Pos, p.X, p.Y)
            |> List.tryHead
            |> Option.map (fun p -> cmd at (MoveTo(m.Id, p)))
            |> Option.defaultValue [])
      | _ -> []

    // The floor. A resurrection cannot be decided in `healerOrders`: that only ever
    // sees the living, and Reincarnation is cast *by* the Shaman's own corpse. So
    // it is decided here, where the dead are still part of the world, and it is
    // decided only while a hostile is left — a corpse is not worth standing up to
    // an empty room, and a wipe is not a moment to spend a thirty-minute cooldown.
    let dead =
      w.Entities
      |> List.filter (fun e ->
        e.Faction = Party
        && not (SimState.alive e)
        // A summon is not a fallen member. A dead totem is `Faction = Party` too, so
        // without this the Shaman picks it ahead of any real corpse — totems die at
        // x=22 and the wounded stand to their right — and casts Ancestral Spirit at
        // a five-health stub that can never come back. The cast cancels its walk, so
        // the healer stayed where it was, `waiting` never cleared, the Tank never
        // pulled, and library L36 parked for the whole tick cap that way.
        && e.Totem.IsNone)

    let resurrections =
      if SimState.aliveHostiles w |> List.isEmpty then
        []
      else
        // Self-raise first: the corpse that can get itself up needs no one's help.
        let selfRaise =
          dead
          |> List.collect (fun e ->
            e.Abilities
            |> List.filter (fun a -> a.TargetKind = Self && Ability.resurrects a && canUse e a.Name)
            |> List.truncate 1
            |> List.collect (fun a -> cmd at (UseAbility(e.Id, a.Name, e.Id))))

        // Rebirth is the other half: a healer above the floor raising one of the
        // dead. Only the living cast it, so it is decided off `actors`, and only
        // once the self-raise has not already answered the wipe.
        let raiseOne =
          if not selfRaise.IsEmpty then
            []
          else
            actors
            |> List.collect (fun e ->
              match e.Abilities |> List.tryFind (fun a -> a.TargetKind = Corpse && canUse e a.Name) with
              | None -> []
              | Some a ->
                match
                  dead
                  |> List.filter (fun d -> Pos.chebyshev e.Pos d.Pos <= a.Range)
                  |> List.sortBy (fun d -> d.Pos.X, d.Pos.Y)
                  |> List.tryHead
                with
                | None -> []
                | Some d -> cmd at (UseAbility(e.Id, a.Name, d.Id)))

        selfRaise @ raiseOne

    // Innervate multiplies regeneration, so a caster that keeps casting multiplies
    // almost nothing. An innervated member therefore stops casting while the party
    // is out of trouble — the Druid bought that silence when it spent the spell —
    // and resumes the moment anybody is hurt enough to need it.
    let resting =
      let safe =
        party
        |> List.filter SimState.alive
        |> List.forall (fun m -> m.Health * 100 >= m.MaxHealth * 60)

      if not safe then
        Set.empty
      else
        party
        |> List.filter (fun m ->
          m.Auras
          |> List.exists (function
            | Innervated _ -> true
            | _ -> false))
        |> List.map (fun m -> m.Id)
        |> Set.ofList

    // Below a twentieth of the bar a damage caster stops entirely: no spell it can
    // afford leaves it anything for the next pack, and the only way back up is the
    // five-second rule, which asks for exactly that. A healer never stops.
    let drained (m: Entity) =
      m.Role <> Some Healer && m.MaxResource > 0 && m.Resource * 100 < m.MaxResource * 5

    // Where to die. Reincarnation raises the Shaman where it fell, so the tile it
    // falls on is the whole plan: when the party wipes the pull is lost, the room goes
    // home and forgets, and a body more than `aggroRadius` from every one of those
    // homes is invisible to it — which is what lets the Shaman stand up, raise the
    // party and walk back in. Standing at distance is not enough on its own: the
    // corpse also has to be within Ancestral Spirit's reach of somebody it means to
    // raise, or the safe tile is a dead end.
    let fleeing =
      match dying with
      | None -> []
      | Some shaman ->
        // Only plan when there is no plan, for the reason `leaderless` does it: a
        // whole-map search every tick is the expensive way to take three steps, and
        // a mover already walking keeps its route until it runs out.
        if shaman.Goal.IsSome || not shaman.Path.IsEmpty || shaman.Destination.IsSome then
          []
        else
          let homes =
            SimState.aliveHostiles w |> List.map (fun h -> defaultArg h.Home h.Pos)

          let corpses =
            w.Entities |> List.filter (fun e -> e.Faction = Party && not (SimState.alive e) && e.Totem.IsNone)

          // Ancestral Spirit reaches 8 tiles (`Content.ancestralSpirit.Range`).
          let raiseRange = 8

          let safe (p: Pos) =
            homes |> List.forall (fun h -> Pos.chebyshev h p > SimState.aggroRadius)

          let raisesSomebody (p: Pos) =
            corpses |> List.exists (fun c -> Pos.chebyshev c.Pos p <= raiseRange)

          let route =
            Path.astar w.Grid (SimState.claimedByOther w shaman.Id) shaman.Pos

          Grid.tiles w.Grid
          |> Seq.filter (Grid.isFloor w.Grid)
          |> Seq.filter (fun p -> safe p && raisesSomebody p)
          |> Seq.sortBy (fun p -> (Pos.chebyshev shaman.Pos p, p.X, p.Y))
          |> Seq.truncate 20
          |> Seq.tryPick (fun p ->
            if p = shaman.Pos || not (route p).Path.IsEmpty then Some p else None)
          |> Option.filter (fun p -> p <> shaman.Pos)
          // Already standing somewhere safe: hold, and wait to be killed.
          |> Option.map (fun p -> cmd at (MoveTo(shaman.Id, p)))
          |> Option.defaultValue []

    // Out of combat, with no target and nothing to recover, the lane would
    // otherwise idle — and that is exactly the moment the totem set wants. The
    // wing is one World: the set that covered the last pull is standing
    // wherever the last pull was, and the approach to the next is when the
    // slots re-read vacant (the Shaman has walked out of their radius) and the
    // set is re-dropped where the next fight will actually be. Recovery keeps
    // its priority — a flight, a resurrection or a target still in view means
    // this stays silent — and the in-combat order is untouched: kick, shield,
    // totems, heals.
    let betweenPulls =
      if
        SimState.inCombat w
        || target.IsSome
        || not (List.isEmpty fleeing)
        || not (List.isEmpty resurrections)
      then
        []
      else
        party |> List.collect (fun e -> totemOrders w e party at)

    // The runner's answer (operator direction): a fleeing mob is the pull
    // widening -- every tile it crosses fetches the pack it reaches -- so the
    // first available stopper lands on it, ranked by how hard it holds. Blind
    // and Frost Nova stop it outright, Gouge and Entangling Roots hold it in
    // place, Hamstring, Wing Clip and Frostbolt's chill slow it, which is
    // enough for a party a tile behind. Ordered after the lanes, so the same
    // actor's damage order cannot overwrite it.
    let antiFlee =
      let rank =
        function
        | "Blind" -> 0
        | "Frost Nova" -> 1
        | "Gouge" -> 2
        | "Entangling Roots" -> 3
        | "Hamstring" -> 4
        | "Wing Clip" -> 5
        | "Frostbolt" -> 6
        | _ -> 99

      let reachesTo (e: Entity) (h: Entity) (a: Ability) =
        Pos.chebyshev e.Pos h.Pos <= a.Range && Path.lineOfSight w.Grid e.Pos h.Pos

      // A runner is a low-health engaged mob whose goal points away from every
      // party member -- a chasing mob's goal is a tile beside its target, which
      // is never farther from that target than the mob itself.
      let isRunner (h: Entity) =
        h.Engaged
        && SimState.canAct h
        && SimState.healthPct h <= 25
        && h.Goal
           |> Option.exists (fun g ->
                party |> List.forall (fun p -> Pos.chebyshev p.Pos g > Pos.chebyshev p.Pos h.Pos))

      let runner =
        SimState.aliveHostiles w
        |> List.filter isRunner
        |> List.sortBy (fun h ->
          party
          |> List.map (fun p -> Pos.chebyshev p.Pos h.Pos)
          |> List.fold min System.Int32.MaxValue)
        |> List.tryHead

      match runner with
      | None -> []
      | Some r ->
        actors
        |> List.collect (fun e ->
          e.Abilities
          |> List.filter (fun a -> rank a.Name < 99)
          |> List.filter (fun a -> canUse e a.Name && reachesTo e r a && canControl e a r)
          |> List.map (fun a -> (rank a.Name, e, a)))
        |> List.sortBy (fun (rk, _, _) -> rk)
        |> List.tryHead
        |> Option.map (fun (_, e, a) -> [ { ApplyAt = at; Kind = UseAbility(e.Id, a.Name, r.Id) } ])
        |> Option.defaultValue []

    petOrders w party at
    @ prePull
    @ movement
    @ regroup
    @ fleeing
    @ leaderless
    @ resurrections
    @ betweenPulls
    // While a pull's openers are still pending -- a controller walking into
    // position, a cast in flight, a charge about to land -- nobody attacks:
    // the damage lane's ranged opener would engage the pack before the sheep
    // or the Sap could land (measured: Arcane Rupture woke the pack on the
    // same Tick the Polymorph was booked). The openers ARE the pull's start;
    // the shot that used to open it follows them.
    @ (if target.IsNone || holdForCc then
         []
       else
         [ yield! role Tank |> List.collect (fun e -> tankOrders w e onTank at)
           yield! role Healer
                  |> List.collect (fun e ->
                    // The kick comes before everything else this lane can do, and
                    // before the damage fallback at the bottom of it. An assignment
                    // is the Party's consensus that *this* member is the one who
                    // will stop that cast (`assignedKick` above), so deferring it
                    // behind a heal, a totem or the mana gate is not a delay — the
                    // cast finishes and nobody else was ever asked to press. The
                    // damage gate in particular must not refuse it: a kick is
                    // damage *prevented*, and `worthBreaking` has already decided
                    // the cast is worth more than the spell given up.
                    match assignedKick w e with
                    | Some(kick, victim) -> cmd at (UseAbility(e.Id, kick.Name, victim))
                    | None ->
                      // Totems first: they are instant, they last two minutes, and a
                      // Shaman with no earth totem down is a Tank taking the full
                      // spread of every swing. Combat is no longer the gate: the
                      // approach to a pack is exactly when a set left behind wants
                      // re-dropping, and the pull's first Ticks are the wrong place
                      // to spend the global on it.
                      let totems = totemOrders w e party at

                      // The shield is the one order that is not a heal, and it is the
                      // cheapest: instant, no cooldown, no duration.
                      let upkeep = shieldOrders e at

                      if not upkeep.IsEmpty then
                        upkeep
                      elif not totems.IsEmpty then
                        totems
                      else
                        match healerOrders w e party at with
                        // Nothing to heal. The heal-first priority above is untouched —
                        // any order it produced has already won, and always will.
                        //
                        // What is left is whether this healer can afford damage, and the
                        // answer is not a threshold on the pool. A flat "cast above 55%"
                        // was what stood here, and it answers a question about the
                        // present with no reference to the future: it spends while the
                        // pull is long and keeps spending as the pull ends, which is
                        // exactly backwards. `ManaBudget` prices the future instead.
                        //
                        // The window says how fast the party has been taking health off
                        // the pack, and `remaining hostile HP / that rate` says how many
                        // seconds are left. The healer's own observed net pool draw times
                        // those seconds, plus 25% and one heal, is what it must still be
                        // able to pay. A long pull reserves more than the whole pool and
                        // nothing is cast; the last few seconds reserve almost nothing and
                        // almost anything is.
                        //
                        // The question is asked before `rangedOrders` rather than after it,
                        // because that function answers with a `MoveTo` when the target is
                        // out of reach: a healer that may not cast also may not walk in for
                        // the cast it was refused.
                        //
                        // The other gate is the form: `SimCombat` refuses a damaging cast
                        // in a form that forbids it, and a Tree of Life Druid used to spend
                        // whole encounters ordering the Moonfire that was thrown away every
                        // Tick. A form-locked healer is not a damage dealer and this policy
                        // does not make it one — so in practice the gate acts on the Priest
                        // and the Shaman, and a Paladin's Judgement is melee-range by design
                        // and is never walked to.
                        | [] when canDamageNow e && SimState.ManaBudget.mayDamage w e ->
                          rangedOrders w e target at
                        | orders -> orders)
           // Cat Form is the one melee shape with its own ordering: a feral Druid is
           // a `Dps` member whose kit is written against bleeds and combo points
           // rather than against Backstab's position, so it takes `catOrders` and
           // the melee lane below skips the body this line has just served.
           yield!
             role Dps
             |> List.filter (fun e -> SimState.inStance e CatForm && melee e)
             |> List.collect (fun e ->
               // The cat's kit carries no kick today, and the assignment is still
               // answered here rather than left to the form: a feral body that wins
               // a cast is a cast nobody else was asked to break, and a row added to
               // the cat kit later must not need this lane edited. Same contract as
               // the healer lane above — `assignedKick` or bust.
               match assignedKick w e with
               | Some(kick, victim) -> cmd at (UseAbility(e.Id, kick.Name, victim))
               | None -> catOrders w e target at)
           yield!
             role Dps
             |> List.filter (fun e -> melee e && not (SimState.inStance e CatForm))
             |> List.collect (fun e -> meleeOrders w e target at)
           yield! role Dps
                  |> List.filter ranged
                  |> List.filter (fun e -> not (drained e) && not (resting.Contains e.Id))
                  |> List.collect (fun e ->
                    match rangedOrders w e target at with
                    // Shots on cooldown and nothing to fire: a member with a mob on
                    // top of it still has a sword arm, and Raptor Strike at range 1
                    // is worth more than the idle tick. Only the ability is taken,
                    // never the walk — `meleeOrders` closes to melee when nothing
                    // connects, which is right for a Rogue and would make a Hunter
                    // charge in on every cooldown gap and shoot its way back out.
                    | [] ->
                      meleeOrders w e target at
                      |> List.filter (fun c ->
                        match c.Kind with
                        | UseAbility _ -> true
                        | _ -> false)
                    | orders -> orders) ]
       @ antiFlee
         |> List.filter (fun c ->
           match c.Kind with
           | UseAbility(source, _, _) -> not (resting.Contains source)
           | _ -> true))

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
