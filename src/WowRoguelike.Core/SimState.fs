namespace WowRoguelike.Core

/// The World's queries, its rebuild helpers, and how a single ability or command
/// resolves. Everything here is a pure function of the World it is handed.
///
/// The per-Tick pipeline is in SimTick. This module is the naive baseline
/// (Q8/Q25) — records are rebuilt rather than mutated — and is the correctness
/// oracle the fast path will be measured and equivalence-tested against.
module SimState =


  /// Mobs notice the Party at this distance. Proximity aggro, as in WoW.
  ///
  /// Held at a quarter of `packSpacing` (Content.fs), because those two numbers
  /// are one decision and not two: a pull radius that reaches past the gap to the
  /// next pack turns every fight into two fights, and the map cannot put enough
  /// floor between rooms to compensate. At `packSpacing = 7` this is `7 / 4`
  /// rounded up to 2 — a mob still wakes together with the pack it is standing in
  /// (pack members are within two tiles of each other by construction), and the
  /// pack behind it stays asleep. Raising one of the pair without the other is
  /// what produced the measured graveyard problem of 14 and 17 simultaneously
  /// engaged hostiles against packs of 3 to 4.
  let aggroRadius = 2

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
  /// The heroes. A totem is faction Party so that mobs can hit it, but it is not
  /// a party member: counting one here lets a live totem answer for a dead party
  /// in `outcome`, and puts totems in the heal pool and the formation.
  let aliveParty (w: World) =
    w.Entities |> List.filter (fun e -> e.Faction = Party && alive e && e.Totem.IsNone)

  /// In combat while something hostile is engaged with the party. This is the
  /// same test the party uses to end a pull, and the one regeneration asks:
  /// a mob that has not noticed you is not a fight.
  let inCombat (w: World) =
    w.Entities |> List.exists (fun e -> e.Faction = Hostile && e.Engaged && alive e)

  let aliveHostiles (w: World) =
    w.Entities |> List.filter (fun e -> e.Faction = Hostile && alive e && not e.Dormant)

  /// A creature just died: credit it to the objectives watching for it, and if
  /// that finishes one, say so and wake whatever it unseals. This is the whole
  /// scripted-encounter mechanism — Naralex's awakening is a quest whose
  /// completion wakes Mutanus, and nothing else in the sim knows about scripts.
  let recordKill (name: string) (w: World) : World =
    let after = Quest.onKill name w.Quests
    let finished = Quest.justCompleted w.Quests after

    if List.isEmpty finished then
      { w with Quests = after }
    else
      let awake = finished |> List.collect (fun q -> q.Wakes)

      { w with
          Quests = after
          Entities =
            w.Entities
            |> List.map (fun e -> if List.contains e.Name awake then { e with Dormant = false } else e)
          Log = List.rev (finished |> List.collect (fun q -> q.Lines)) @ w.Log }

  let outcome (w: World) =
    if List.isEmpty (aliveHostiles w) then EncounterCleared
    elif List.isEmpty (aliveParty w) then PartyWiped
    else Running

  /// A corpse on the floor that can still bring itself back. Reincarnation is
  /// cast by the dead Shaman, so a party that looks wiped is a wipe that has not
  /// happened yet: the tick loop has to outlive `PartyWiped` long enough for the
  /// order to land. A pack that is already dead is the exception — `EncounterCleared`
  /// means the fight is over however many bodies are lying around.
  ///
  /// A Rebirth already in flight counts too: the Druid's 2 s cast lands whether or
  /// not the last swing of the fight killed the Druid, and resolving the run on that
  /// tick would throw away a rescue that was already paid for.
  ///
  /// The price and the target are part of the test. A corpse only raises *itself*,
  /// so a Corpse-target resurrection on a dead caster can never be cast; and a
  /// corpse does not regenerate (`SimTick.fs:126`), so a resurrection the dead
  /// caster cannot afford is one it can never afford. Counting either keeps
  /// `continues` true until the caller's tick cap — a run that cannot end,
  /// spending its remaining ticks on a party with nothing to order.
  let resurrectionPending (w: World) =
    not (List.isEmpty (aliveHostiles w))
    && w.Entities
       |> List.exists (fun e ->
         e.Faction = Party
         && ((not (alive e)
              && (e.Abilities
                  |> List.exists (fun a ->
                    Ability.resurrects a
                    && a.TargetKind = Self
                    && e.Resource >= a.ResourceCost
                    && (e.Cooldowns |> Map.tryFind a.Name |> Option.defaultValue (ticks 0)) <= ticks 0)))
             || (e.Casting |> Option.exists (fun c -> Ability.resurrects c.Ability))))

  /// Whether the run still has a Tick worth spending. `outcome` keeps its
  /// meaning — a wiped party is still `PartyWiped` — and this only says when to
  /// stop asking it, so that a resurrection gets the Tick that saves the run.
  let continues (w: World) = outcome w = Running || resurrectionPending w

  let healthPct (e: Entity) = e.Health * 100 / e.MaxHealth

  /// Mana, rage or energy left, as a percentage of the pool.
  let resourcePct (e: Entity) = e.Resource * 100 / max 1 e.MaxResource

  let inMelee (a: Entity) (b: Entity) = Pos.chebyshev a.Pos b.Pos <= 1

  let withinRange (range: int) (a: Entity) (b: Entity) =
    Pos.chebyshev a.Pos b.Pos <= range

  /// Distance alone is not reach: nothing at range can be hit through a wall.
  /// Melee is deliberately exempt, so something standing beside you is always
  /// hittable and a blocked caster cannot freeze next to its target.
  let canReach (range: int) (actor: Entity) (target: Entity) (w: World) =
    withinRange range actor target
    && (range <= 1 || Path.lineOfSight w.Grid actor.Pos target.Pos)

  /// A tile is taken if someone has settled on it. A body that is only stepping
  /// through does not claim it: two members may pass through one tile as long as
  /// neither means to stop there, and `SimTick.advanceMovement` separates them if
  /// a cross-over still lands them together. Keeping transit tiles open is what
  /// stops a two-wide corridor from refusing to carry traffic both ways.
  let claimedByOther (w: World) (self: EntityId) (p: Pos) =
    w.Entities
    |> List.exists (fun e -> alive e && e.Id <> self && e.Pos = p && e.Destination.IsNone)

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

  let isSleeping (e: Entity) = e.Auras |> List.exists Aura.isSleeping

  let isShifted (e: Entity) = e.Auras |> List.exists Aura.isSerpentForm

  let isSilenced (e: Entity) = e.Auras |> List.exists Aura.isSilenced

  let serpentBonus (e: Entity) = e.Auras |> List.sumBy Aura.bonusDamage

  /// Being shifted locks out casting, which is the whole cost of Serpent Form.
  let canCast (e: Entity) =
    not (e.Auras |> List.exists Aura.preventsCasting) && e.Casting.IsNone

  let canAct (e: Entity) =
    alive e && not (e.Auras |> List.exists Aura.preventsActing)

  /// Movement is its own gate: a Rooted entity can still act, but not step.
  let canMove (e: Entity) =
    alive e && not (e.Auras |> List.exists Aura.preventsMoving)
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

  /// The entities an ability can be cast at right now: alive, the faction its
  /// TargetKind expects, and within range and line of sight. This is the whole
  /// targeting rule — heals see every party member in reach, damaging abilities
  /// see every enemy in view.
  let validTargets (actorId: EntityId) (abilityName: string) (w: World) : Entity list =
    match tryEntity actorId w with
    | None -> []
    | Some actor ->
      match actor.Abilities |> List.tryFind (fun a -> a.Name = abilityName) with
      | None -> []
      | Some ability ->
        let factionOk (t: Entity) =
          match ability.TargetKind with
          | Self -> t.Id = actor.Id
          | Foe -> t.Faction <> actor.Faction
          | Ally -> t.Faction = actor.Faction
          | Corpse -> t.Faction = actor.Faction

        w.Entities
        |> List.filter (fun t ->
          // A resurrection's targets are exactly the bodies `alive` excludes.
          (alive t || Ability.resurrects ability)
          && factionOk t
          && (ability.TargetKind = Self || canReach ability.Range actor t w))

  // =========================================================================
  // Record rebuilds (the naive path)
  // =========================================================================

  let logLine (line: string) (w: World) = { w with Log = line :: w.Log }

  let mapEntity (id: EntityId) (f: Entity -> Entity) (w: World) =
    { w with
        Entities = w.Entities |> List.map (fun e -> if e.Id = id then f e else e) }

  let mapAll (f: Entity -> Entity) (w: World) = { w with Entities = w.Entities |> List.map f }

  /// The party is all corpses and something can still get up: the pull is lost, so
  /// everything hostile goes home, whole, and forgets. Corpses are left where they
  /// fell — they are the run's stake. The `Home` anchor is what makes the Shaman's
  /// death spot matter: a body more than `aggroRadius` from every home is invisible
  /// to the room it just lost to.
  ///
  /// shortcut: mobs teleport home instead of running; give them a walk if evade has
  /// to be visible on screen.
  let encounterReset (w: World) : World =
    if not (List.isEmpty (aliveParty w)) || not (resurrectionPending w) then
      w
    else
      mapAll
        (fun e ->
          if e.Faction <> Hostile then
            e
          else
            { e with
                Pos = defaultArg e.Home e.Pos
                Destination = None
                MoveTicksLeft = ticks 0
                MoveTicksTotal = ticks 0
                Goal = None
                Path = []
                StallTicks = ticks 0
                Health = e.MaxHealth
                Resource = e.MaxResource
                Auras = []
                Casting = None
                Cooldowns = Map.empty
                Threat = Map.empty
                Target = None
                ForcedTarget = None
                Engaged = false
                HasCalledForHelp = false })
        w

  /// Any hostile action wakes a sleeper — the sourced rule — so an interrupt or
  /// a taunt ends a sleep as surely as damage does.
  let wake (targetId: EntityId) (w: World) =
    mapEntity
      targetId
      (fun t ->
        { t with
            Auras = t.Auras |> List.filter (fun a -> not (Aura.endsOnHostileAction a)) })
      w

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

  let healEntity (targetId: EntityId) (amount: int) (w: World) =
    mapEntity targetId (fun t ->
      // Wound Poison: a poisoned target receives less than the heal that was paid
      // for. Charged here rather than at the cast, so every heal in the game — the
      // Druid's HoTs, a potion, a resurrection — meets the same cut.
      let cut = t.Auras |> List.sumBy Aura.healingReceivedReductionBp
      let amount = amount * max 0 (10000 - cut) / 10000
      { t with Health = min t.MaxHealth (t.Health + amount) }) w

  /// Name lookup for log messages.
  let internal nameOf (id: EntityId) (w: World) =
    tryEntity id w |> Option.map (fun e -> e.Name) |> Option.defaultValue "?"
