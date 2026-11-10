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

  /// Roughly how hard an ability hits, for ordering a kit the way a player would
  /// rather than by the order it happens to sit in the list.
  let private damageOf (a: Ability) =
    a.Effects
    |> List.sumBy (function
      | WeaponDamage(multiplierBp, bonus) -> 30 * multiplierBp / 10000 + bonus
      | MultiShot _ -> 30
      | Damage(_, mx) -> mx
      | SpellDamage(_, mx, _) -> mx
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
  /// stun out of the pair. Ambush is out by name because nothing in its effects
  /// says "stealth": it is a stealth opener, and offering it here would spend it
  /// as though stealth were free.
  let private builders (e: Entity) =
    e.Abilities
    |> List.filter (fun a ->
      a.CooldownTicks = ticks 0
      && a.TargetKind = Foe
      && a.Name <> "Ambush"
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
      let myInterrupts = interruptTargets w t

      let caster =
        SimState.aliveHostiles w
        |> List.filter (fun h -> Set.contains h.Id myInterrupts)
        |> List.tryFind (fun h ->
          SimState.canReach 1 t h w
          && h.Casting |> Option.exists (fun c -> c.Ability.Interruptible))
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

      let dump =
        abilityWhere t (function WeaponDamage _ -> true | _ -> false)
        |> Option.filter (fun a -> a.ResourceCost > 0)

      match thief, taunt with
      | Some h, Some a when canUse t a.Name -> cmd at (UseAbility(t.Id, a.Name, h.Id))
      // Intercept is in the kit and stays out of the rotation: measured, it reads 7
      // of 12 in `library` against 10 without it. The leaps themselves are not the
      // problem — the geometry they create is. A Tank that arrives eight tiles ahead
      // of its healers is a Tank fighting alone, and the straggler gate then sees a
      // party that has to be waited for and parks the pull. The gap closer earns its
      // rage the day the party moves with the Tank; until then walking is faster.
      | _ when not (SimState.withinRange 1 t f) -> cmd at (MoveTo(t.Id, f.Pos))
      | _ when caster.IsSome && has t "Shield Bash" && canUse t "Shield Bash" ->
        cmd at (UseAbility(t.Id, "Shield Bash", caster.Value.Id))
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
      | _ when has t "Sunder Armor" && not sundered && canUse t "Sunder Armor" ->
        cmd at (UseAbility(t.Id, "Sunder Armor", f.Id))
      | _ when dump.IsSome && canUse t dump.Value.Name ->
        cmd at (UseAbility(t.Id, dump.Value.Name, f.Id))
      | _ when canUse t "Sunder Armor" -> cmd at (UseAbility(t.Id, "Sunder Armor", f.Id))
      | _ -> []

  /// Healer. A heal-over-time first: it is the cheapest health per point of mana
  /// and it keeps working while the next cast happens. It is refreshed only as it
  /// lapses, and the direct heal starts as soon as the deficit outruns what the
  /// HoTs in flight will give back — a three-second cast that begins when the bar
  /// is empty is a three-second cast that began too late. The talent cooldowns go
  /// in front of that direct heal below 60%, which is one cast-time of health above
  /// death rather than a panic threshold.
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
      let inForm =
        affordable |> List.filter (fun a -> a.Stance |> Option.forall (SimState.inStance h))

      let best (l: Ability list) =
        l |> List.filter covers |> List.tryLast |> Option.orElse (List.tryHead l)

      match inForm |> List.filter covers with
      | _ :: _ -> best inForm
      | [] -> best affordable

    let projected (t: Entity) = t.Health + Aura.healOverTimePending t.Auras
    let projectedPct (t: Entity) = projected t * 100 / t.MaxHealth

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
    let needsFastHeal (t: Entity) = SimState.healthPct t < 60

    let cooldownFor (t: Entity) =
      let fast = needsFastHeal t

      h.Abilities
      |> List.tryFind (fun a ->
        canUse h a.Name
        && (a.TargetKind = Self || SimState.canReach a.Range h t w)
        && (a.Effects
            |> List.exists (function
              | Empowers _ -> fast
              // Swiftmend is worth it when there is a heal-over-time worth eating,
              // Rejuvenation or Regrowth. In an emergency the last Ticks on it are
              // given up for the instant heal; outside one, `banked` waits for them.
              | ConsumeHealOverTime ->
                fast
                && (Aura.swiftmend t.Auras |> Option.map snd |> Option.defaultValue 0) > 0
              | _ -> false)))

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

    /// The shift back to the shape the class lives in, when it is in the wrong one.
    let home =
      homeForm h
      |> Option.bind (fun s -> if SimState.inStance h s then None else shiftTo h s)
      |> Option.map (fun f -> cmd at (UseAbility(h.Id, f.Name, h.Id)))

    /// The shift that makes a cast legal, when the spell names a shape. One action
    /// per Tick like any other order: a Tree Druid that wants Healing Touch shifts
    /// out first, casts next Tick, and is given the form back after.
    let shapeFor (a: Ability) =
      match a.Stance with
      | Some s when not (SimState.inStance h s) ->
        shiftTo h s |> Option.map (fun f -> cmd at (UseAbility(h.Id, f.Name, h.Id)))
      | _ -> None

    // Into range, or not worth it: the reach rule is SimState's, never restated.
    let order (choice: Ability option) (t: Entity) =
      match choice with
      | Some a when SimState.canReach a.Range h t w ->
        (match shapeFor a with
         | Some shift -> shift
         // Nothing here makes staying human cost anything, but the form is the point
         // of the spec: once the hole is not a hole, the tree is where the healer
         // belongs. A calm target can spare the Tick; a dying one cannot.
         | None when not (needsFastHeal t) ->
           (match home with
            | Some back -> back
            | None -> cmd at (UseAbility(h.Id, a.Name, t.Id)))
         | None -> cmd at (UseAbility(h.Id, a.Name, t.Id)))
      | Some a -> cmd at (MoveTo(h.Id, healingSpot t a |> Option.defaultValue t.Pos))
      | None -> []

    /// Into a direct heal: the cooldown first when it is up and the target is in
    /// trouble, the biggest affordable heal otherwise.
    let heal (t: Entity) =
      match cooldownFor t with
      // They are self-buffs, so the caster is the target of the button itself.
      | Some a -> cmd at (UseAbility(h.Id, a.Name, (if a.TargetKind = Self then h.Id else t.Id)))
      | None -> order (directFor (t.MaxHealth - projected t)) t
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
    // outside an emergency can never overheal. Identified by effect shape, not by
    // name, so another HoT-eating button would be picked up too.
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
        |> List.sortBy SimState.healthPct
        |> List.tryPick (fun t ->
          match Aura.swiftmend t.Auras with
          | Some(source, instant) when
            instant > 0
            && t.Health + instant <= t.MaxHealth
            && Aura.healOverTimeRemainingFor source t.Auras <= secTicks 2.0
            ->
            Some(UseAbility(h.Id, a.Name, t.Id))
          | _ -> None))

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
    | Some t, _ when projectedPct t < 65 -> heal t
    | _, Some t -> order (hotFor t) t
    | Some t, None -> heal t
    | None, None ->
      // Nobody is in trouble and nothing needs refreshing, so close the small
      // holes with the cheap heal instead of leaving them for the next burst —
      // but never pay a heal that is mostly overheal.
      match wounded |> List.tryHead with
      | Some t when projectedPct t < 90 ->
        let deficit = t.MaxHealth - projected t

        match directFor deficit with
        | Some a when healsOf a / 2 <= deficit -> order (Some a) t
        | _ -> Option.defaultValue [] home
      | _ -> Option.defaultValue [] home

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
  let private totemOrders (w: World) (e: Entity) (party: Entity list) (at: int<tick>) =
    let standing =
      w.Entities
      |> List.filter (fun t -> t.Owner = Some e.Id && SimState.alive t)
      |> List.choose (fun t -> t.Totem |> Option.map (fun x -> TotemSlot.ofPulse x.Pulse))
      |> Set.ofList

    let toughPull =
      (SimState.aliveHostiles w
       |> List.filter (fun h -> h.Target.IsSome)
       |> List.length) >= 3
      // A healer's bar is the party's reserve, so the tide is held for the pull
      // where the healer itself is running dry — not merely for one where
      // somebody is. That is the Shaman answering a call, not topping up.
      || (party
          |> List.filter (fun p -> p.Role = Some Healer && p.ResourceKind = Mana)
          |> List.exists (fun p -> SimState.resourcePct p < 60))

    // A tide is allowed to displace a spring; nothing else may displace a
    // neighbour, because two totems of one element is not a thing the game has.
    let displacesSpring (a: Ability) =
      a.Name = "Mana Tide Totem"
      && w.Entities
         |> List.exists (fun t ->
           t.Owner = Some e.Id && t.Name = "Mana Spring Totem" && SimState.alive t)

    e.Abilities
    |> List.tryPick (fun a ->
      let slot =
        a.Effects
        |> List.tryPick (function
          | Summon s -> s.Totem |> Option.map (fun x -> TotemSlot.ofPulse x.Pulse)
          | _ -> None)

      let summons = a.Effects |> List.exists (function Summon s -> s.Totem.IsSome | _ -> false)

      if
        summons
        && slot.IsSome
        && (not (standing.Contains slot.Value) || displacesSpring a)
        && (a.Name <> "Mana Tide Totem" || toughPull)
        && (a.Name <> "Mana Spring Totem" || not toughPull)
        && canUse e a.Name
      then
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
        // Ambush is a stealth opener, not a rotation strike: left in this list it
        // would win every `damageOf` comparison and be spent as though stealth
        // were free. It is offered only by the opener branch below.
        && a.Name <> "Ambush")

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

    let openWith =
      [ "Ambush"; "Cheap Shot" ]
      |> List.filter (fun n -> has e n && canUse e n)
      |> List.tryHead

    let surplus = e.Resource >= e.MaxResource - 5

    let stun =
      [ "Cheap Shot"; "Kidney Shot" ]
      |> List.filter (fun n -> has e n && canUse e n && surplus)
      |> List.tryHead

    match caster with
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

      let isMultiShot (a: Ability) =
        a.Effects |> List.exists (function MultiShot _ -> true | _ -> false)

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

      let shots =
        e.Abilities
        |> List.filter (fun a ->
          a.TargetKind = Foe
          && a.Range > 1
          && not (interrupts a)
          && not (isWindow a)
          && damageOf a > 0
          && (crowded || not (isMultiShot a)))
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

      let shot =
        (if lowMana then paid |> List.sortByDescending perMana else paid |> List.sortByDescending perCast)
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
      | _ ->
        match shot with
        | Some a when SimState.canReach a.Range e f w -> cmd at (UseAbility(e.Id, a.Name, f.Id))
        | _ when not (SimState.withinRange 8 e f) -> cmd at (MoveTo(e.Id, f.Pos))
        | _ -> []

  /// One Tick of orders for the whole Party.
  /// The hunter's pet. `Summon` builds a real Party entity owned by the hunter, so
  /// it can be killed and it has to be healed; nothing else in the roster brings a
  /// body that outlives its owner's swing. Both orders sit outside the `target`
  /// gate on purpose — a pet called after the opener is called too late, and a pet
  /// mended only while a hostile is up is a pet that dies between packs.
  ///
  /// One pet per run: re-summoning a dead pet would leave its corpse on the floor
  /// with a second pet beside it, and nothing in the engine removes an entity.
  let private petOrders (w: World) (party: Entity list) (at: int<tick>) =
    party
    |> List.collect (fun h ->
      if not (has h "Summon Pet") then
        []
      else
        match party |> List.filter (fun m -> m.Owner = Some h.Id) |> List.tryHead with
        | None ->
          if w.Entities |> List.exists (fun e -> e.Owner = Some h.Id) then
            // The pet died. No second body while its corpse is still on the grid.
            []
          elif canUse h "Summon Pet" then
            cmd at (UseAbility(h.Id, "Summon Pet", h.Id))
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
    let prePull =
      let engaged = SimState.aliveHostiles w |> List.exists (fun h -> h.Target.IsSome)

      let controlling =
        fun (a: Ability) ->
          a.Effects
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
        let surplus =
          SimState.aliveHostiles w
          |> List.filter (fun h -> h.Id <> focus.Id && h.Casting.IsNone && SimState.canAct h)
          |> List.sortByDescending (fun h -> h.MaxHealth)

        // One caster thins the pull. Two on the same target wastes both, so the
        // opener below skips whoever cast it.
        let cc =
          match surplus with
          | [] -> None
          | h :: _ ->
            actors
            |> List.tryPick (fun e ->
              e.Abilities
              |> List.filter controlling
              |> List.filter (fun a -> canUse e a.Name && reaches e h a && unnoticed e a)
              |> List.sortBy preference
              |> List.tryHead
              |> Option.map (fun a -> { ApplyAt = at; Kind = UseAbility(e.Id, a.Name, h.Id) }))

        // The Tank's half of the opener stays out of the rotation on measurement.
        // Charge reads 8 of 12 in `graveyard` against 11 without it, and requiring
        // the party within four tiles first does not rescue it: an eight-tile leap
        // puts the Tank where its healers are not, and the fight that follows is the
        // Tank alone against a pack. Communication does not fix distance. The ability
        // is in the kit, the engine enforces its out-of-combat rule and its wall rule,
        // and the branch lands when the party can be told to run in *ahead* of it.
        let chargeIn: Command list = []

        [ yield! Option.toList cc
          yield! chargeIn ]
      | _ -> []

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
            // The nearest mob is not the next pull: a wing is rooms behind walls,
            // and the mob three tiles past one is nearer than the mob across the
            // floor you are standing on. Aiming at the nearest-by-distance goal
            // parked the Party against a wall for the rest of the run — `graveyard`
            // seed 12 ended with five healthy members, 36 mobs alive, and the Tank
            // alternating its first step between two tiles for 16 000 ticks, its
            // goal 8 tiles away and behind a wall with no sight of it. What it can
            // see is what it may pull; only when it can see nothing at all does the
            // nearest dead body anywhere become the goal, which is the explore case.
            (if waiting then [] else SimState.aliveHostiles w)
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
          if waiting then yield! cmd at (Stop t.Id)

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

    petOrders w party at
    @ prePull
    @ movement
    @ regroup
    @ fleeing
    @ leaderless
    @ resurrections
    @ (if target.IsNone then
         []
       else
         [ yield! role Tank |> List.collect (fun e -> tankOrders w e onTank at)
           yield! role Healer
                  |> List.collect (fun e ->
                    // Totems first: they are instant, they last two minutes, and a
                    // Shaman with no earth totem down is a Tank taking the full
                    // spread of every swing.
                    let totems =
                      if SimState.inCombat w then totemOrders w e party at else []

                    if not totems.IsEmpty then
                      totems
                    else
                      match healerOrders w e party at with
                      // Nothing to heal: a healer with mana to spare adds damage
                      // rather than standing idle, but keeps a reserve for the heal
                      // that has to land next.
                      | [] when SimState.resourcePct e >= 55 -> rangedOrders w e target at
                      | orders -> orders)
           yield! role Dps |> List.filter melee |> List.collect (fun e -> meleeOrders w e target at)
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
