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

  /// A hurt mob shouts, and what answers is what can hear it. Ten tiles was half
  /// a stitched room -- the radius that made the first pull of the Wailing
  /// Caverns the whole floor at once. Four tiles is the 20 yd of this file's
  /// 40-yd-8-tile convention: it reaches the pack (which is what "pull their
  /// neighbours in" means; the placement puts a pack's bodies within two tiles of
  /// its anchor) and not the pack across the gap.
  let callForHelpRadius = 4

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

  let tryEntity (id: EntityId) (w: World) =
    let rec loop (xs: Entity list) =
      match xs with
      | [] -> None
      | e :: rest -> if e.Id = id then Some e else loop rest

    loop w.Entities

  let alive (e: Entity) = e.Health > 0

  let internal hasAliveParty (w: World) =
    let rec loop (xs: Entity list) =
      match xs with
      | [] -> false
      | e :: rest ->
        if e.Faction = Party && e.Health > 0 && e.Totem.IsNone then true else loop rest

    loop w.Entities

  let internal hasAliveHostile (w: World) =
    let rec loop (xs: Entity list) =
      match xs with
      | [] -> false
      | e :: rest ->
        if e.Faction = Hostile && e.Health > 0 && not e.Dormant then true else loop rest

    loop w.Entities

  /// The heroes. A totem is faction Party so that mobs can hit it, but it is not
  /// a party member: counting one here lets a live totem answer for a dead party
  /// in `outcome`, and puts totems in the heal pool and the formation.
  let aliveParty (w: World) =
    w.Entities |> List.filter (fun e -> e.Faction = Party && alive e && e.Totem.IsNone)

  /// A Warlock's summoned demon. Read off the summon's *kind* rather than its name:
  /// `SummonSpec.CreatureType` is copied onto the summoned entity when it is built
  /// (`SimCombat`, the `Summon` branch), every hero is `CreatureType.Humanoid`
  /// (`Content.heroStats`), and the only `Demon` spec in the roster is the Imp
  /// (`Content.warlockDemon`). So a Party-faction body with this type is a demon
  /// that somebody called, and nothing else can answer to the test.
  ///
  /// The Hunter's pet is the other summon and it is a `Beast`, which is the whole
  /// distinction: a pet is a party member with abilities behind it and a Mend Pet
  /// that can hold it up, while a demon is a damage cooldown its owner loses for
  /// free. That is why one stays in the heal pool and this one does not.
  let isSummonedDemon (e: Entity) = e.Faction = Party && e.CreatureType = CreatureType.Demon

  /// The bodies a healer may spend mana on. `aliveParty` minus summoned demons.
  ///
  /// A healer's decision procedure prices every press against the hole it closes,
  /// and a demon is a hole that cannot be closed: the Imp is a 420-health body at
  /// level 24, so a full-rank heal lands almost entirely as overheal and a downrank
  /// still clips. Measured on the Shaman it was 19 502 mana over 12 seeds for
  /// 8 895 healing, 0.81 per point — the worst row in the ledger by construction,
  /// because a 420-health pool caps a heal at 420 however cheaply the rank is bought.
  /// Excluding the body is the only fix that does not require a heal cheaper than the
  /// bottom of the level 1-60 ladder, which does not exist.
  ///
  /// Kept beside `aliveParty` rather than inline at the call sites so the healer's
  /// decision and the metric that scores it cannot disagree about who was in the pool.
  let healableParty (w: World) = aliveParty w |> List.filter (isSummonedDemon >> not)

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
    if not (hasAliveHostile w) then EncounterCleared
    elif not (hasAliveParty w) then PartyWiped
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
    hasAliveHostile w
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
  /// stops a two-wide corridor from refusing to carry traffic both ways. A totem
  /// claims nothing at all: it is scenery the party may stand on.
  let claimedByOther (w: World) (self: EntityId) (p: Pos) =
    let rec loop (xs: Entity list) =
      match xs with
      | [] -> false
      | e :: rest ->
        if e.Health > 0 && e.Id <> self && e.Totem.IsNone && e.Pos = p && e.Destination.IsNone then
          true
        else
          loop rest

    loop w.Entities

  /// The strict version: a tile is occupied if any living body is standing on it,
  /// whatever that body is doing. `claimedByOther` is a planning hint and lets a
  /// mover keep its transit tile, which is right for choosing where to walk and
  /// wrong for a write that moves a body without going through
  /// `SimTick.advanceMovement` — a Charge or a summon placed on a mid-step body's
  /// tile is two bodies on one square, and nothing downstream undoes it.
  let occupiedByOther (w: World) (self: EntityId) (p: Pos) =
    let rec loop (xs: Entity list) =
      match xs with
      | [] -> false
      | e :: rest ->
        if e.Health > 0 && e.Id <> self && e.Totem.IsNone && e.Pos = p then
          true
        else
          loop rest

    loop w.Entities

  /// Zero-allocation helper for `approachTile` that preserves exact `Path.neighbours`
  /// row-major tie-breaking order.
  let internal approachTileWith (moverPos: Pos) (spot: Pos) (g: Grid) (isBlocked: int -> int -> int -> bool) : Pos option =
    let width = g.Width
    let height = g.Height
    let walls = g.Walls
    let sx = spot.X
    let sy = spot.Y

    let inline passable x y =
      if x >= 0 && y >= 0 && x < width && y < height then
        let idx = y * width + x
        not walls.[idx] && not (isBlocked x y idx)
      else
        false

    let okN = passable sx (sy - 1)
    let okS = passable sx (sy + 1)
    let okW = passable (sx - 1) sy
    let okE = passable (sx + 1) sy

    let mutable bestDist = System.Int32.MaxValue
    let mutable bestX = 0
    let mutable bestY = 0

    let inline consider x y =
      let d = max (abs (moverPos.X - x)) (abs (moverPos.Y - y))
      if d < bestDist then
        bestDist <- d
        bestX <- x
        bestY <- y

    // Exact row-major (-1..1, -1..1) order matching Path.neighbours + stable List.sortBy.
    if okW && okN && passable (sx - 1) (sy - 1) then consider (sx - 1) (sy - 1)
    if okN then consider sx (sy - 1)
    if okE && okN && passable (sx + 1) (sy - 1) then consider (sx + 1) (sy - 1)
    if okW then consider (sx - 1) sy
    if okE then consider (sx + 1) sy
    if okW && okS && passable (sx - 1) (sy + 1) then consider (sx - 1) (sy + 1)
    if okS then consider sx (sy + 1)
    if okE && okS && passable (sx + 1) (sy + 1) then consider (sx + 1) (sy + 1)

    if bestDist = System.Int32.MaxValue then
      None
    else
      Some { X = bestX; Y = bestY }

  /// A free tile to stand on next to `spot`, preferring the one nearest the mover.
  let approachTile (mover: Entity) (spot: Pos) (w: World) : Pos option =
    approachTileWith mover.Pos spot w.Grid (fun x y _ -> claimedByOther w mover.Id { X = x; Y = y })

  /// How long to wait before retrying a goal that produced no route.
  let pathRetryBackoff = secTicks 1.0

  // -- auras ----------------------------------------------------------------

  let isSleeping (e: Entity) = e.Auras |> List.exists Aura.isSleeping
  let isStealthed (e: Entity) = e.Auras |> List.exists Aura.isStealthed

  let isShifted (e: Entity) = e.Auras |> List.exists Aura.isSerpentForm

  /// The form a body is in: Bear, Cat, Travel, Tree or Moonkin. None is a body
  /// in its own shape, which is why this is an option rather than a default.
  let stance (e: Entity) = e.Auras |> List.tryPick Aura.stanceOf

  /// Standing in `s`. A body in its own shape is human, and carries no form aura
  /// to match, so `None` is the human case rather than "no answer".
  let inStance (e: Entity) (s: Stance) =
    match stance e with
    | None -> s = HumanForm
    | Some f -> f = s

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
    let rec loop (xs: Entity list) =
      match xs with
      | [] -> None
      | e :: rest ->
        if e.Id = id then
          if e.Faction = Party && e.Health > 0 && e.Totem.IsNone then Some e else None
        else
          loop rest

    loop w.Entities

  let internal resolveTargetWithSeen (m: Entity) (seen: Entity list) : EntityId option =
    match seen with
    | [] -> None
    | _ ->
      let mutable forcedFound = false
      let mutable forcedId = EntityId 0
      let mutable hasTop = false
      let mutable topEntity = Unchecked.defaultof<Entity>
      let mutable topThreat = 0
      let mutable curFound = false
      let mutable curEntity = Unchecked.defaultof<Entity>
      let mutable nearestDist = System.Int32.MaxValue
      let mutable nearestId = EntityId 0

      let hasForced = m.ForcedTarget.IsSome
      let forcedTargetVal = if hasForced then m.ForcedTarget.Value else EntityId 0
      let hasCur = m.Target.IsSome
      let curTargetVal = if hasCur then m.Target.Value else EntityId 0
      let hasThreat = not m.Threat.IsEmpty

      let rec scan (xs: Entity list) =
        match xs with
        | [] -> ()
        | p :: rest ->
          if hasForced && p.Id = forcedTargetVal then
            forcedFound <- true
            forcedId <- p.Id
          else
            if hasCur && p.Id = curTargetVal then
              curFound <- true
              curEntity <- p

            if hasThreat then
              let t = threatOf p.Id m
              if t > 0 && (not hasTop || t > topThreat) then
                hasTop <- true
                topEntity <- p
                topThreat <- t

            if not hasTop then
              let d = Pos.chebyshev m.Pos p.Pos
              if d < nearestDist then
                nearestDist <- d
                nearestId <- p.Id

            scan rest

      scan seen

      if forcedFound then
        Some forcedId
      elif not hasTop then
        if nearestDist = System.Int32.MaxValue then None else Some nearestId
      elif not curFound then
        Some topEntity.Id
      else
        let curThreat = threatOf curEntity.Id m
        // The margin depends on whether the *challenger* is in melee range.
        let margin = if inMelee m topEntity then meleeMarginBp else rangedMarginBp
        // Compare by cross-multiplication, so this stays integer.
        if topThreat * 10000 > curThreat * (10000 + margin) then
          Some topEntity.Id
        else
          Some curEntity.Id

  /// Recompute a Hostile's aggression target from its threat table. The current
  /// target is only displaced by the 10% / 30% margins; with an empty table the
  /// mob simply notices the nearest Party member.
  ///
  /// A stealthed member is invisible here — threat table, nearest-noticed
  /// fallback and even `ForcedTarget` all refuse it — which is the whole of what
  /// Vanish buys.
  let resolveTarget (m: Entity) (w: World) : EntityId option =
    let seen =
      aliveParty w |> List.filter (fun p -> not (isStealthed p))

    resolveTargetWithSeen m seen

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
    let mutable cur = w.Entities
    let mutable idx = 0
    let mutable found = false
    let mutable target = Unchecked.defaultof<Entity>
    let mutable tail = []

    while not found && not (List.isEmpty cur) do
      let head = cur.Head
      let next = cur.Tail

      if head.Id = id then
        found <- true
        target <- head
        tail <- next
      else
        idx <- idx + 1
        cur <- next

    if not found then
      w
    else
      let updated = f target

      if obj.ReferenceEquals(updated, target) then
        w
      elif idx = 0 then
        { w with Entities = updated :: tail }
      else
        let prefix = Array.zeroCreate<Entity> idx
        let mutable p = w.Entities

        for i = 0 to idx - 1 do
          prefix.[i] <- p.Head
          p <- p.Tail

        let mutable acc = updated :: tail

        for i = idx - 1 downto 0 do
          acc <- prefix.[i] :: acc

        { w with Entities = acc }

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
    if hasAliveParty w || not (resurrectionPending w) then
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

  /// Vanish's stealth ends when its holder lands an attack of its own
  /// (`SimCombat`, the damage sites). Taking damage does not end it.
  let breakStealth (id: EntityId) (w: World) =
    mapEntity id (fun e -> { e with Auras = e.Auras |> List.filter (fun a -> not (Aura.isStealthed a)) }) w

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

/// How long the fight has left, and how much mana that says a healer must keep.
///
/// The rule being implemented: a healer that finishes a pull at 90% mana had more
/// to give, but how much more depends on how much longer it has to keep healing.
/// A long pull is a pool to ration; the last heal-clock is free. So the budget is
/// not a threshold on the pool — it is the pool minus the reserve that the fight's
/// remaining length implies. A flat "cast above X% mana" cannot express that,
/// because it answers a question about the present with no reference to the future:
/// it spends while the pull is long and stops when the pull is nearly over, which is
/// exactly backwards.
///
/// Cost per Tick is a handful of floats and two O(1) mean updates, and `sim healers`
/// runs 30 seeds \u00d7 ~800 Ticks \u00d7 5 bodies inside the test suite, so nothing here is
/// allowed to allocate a graph.
  module ManaBudget =

    /// Ticks of history the party's damage mean represents: four seconds.
    ///
    /// The party's own clocks set the floor. Its melee swings land on a 2.0\u20133.1 s clock
    /// per body and its casts run 2.5\u20133.5 s, so a mean shorter than four seconds is at
    /// the mercy of one landed or dodged swing: at three seconds a single body's hit is
    /// a fifth of the history, which is the jitter the old trailing sum showed as a
    /// 4 s \u2194 29 s forecast. Four seconds carries two full swing clocks, so every melee
    /// body lands twice inside the mean, and it spans a complete cast so a caster's
    /// contribution enters as a rate. It lags a real change in output by at most one
    /// cast, and the measured `sim ttk 9` trace at 60 and 80 Ticks differed by under 1%
    /// at the sampled Ticks, so the longer of the two is the better-smoothed choice.
    let damageWindow = 80

    /// Ticks a mean needs before it is allowed to answer: one second.
    ///
    /// Below this the estimate is a prior built from a handful of Ticks, which is
    /// exactly the early-fight noise the mean exists to remove.
    let minSamples = 20

    /// The margin on the projected burn, in basis points: 25% on top.
    ///
    /// A heal that lands late is a death and one that lands early is only mana. The
    /// forecast is a rate times a horizon and can be wrong in either factor, so the
    /// rate carries the margin; the heal floor that used to be flat is now the scaled
    /// `safety` in `plan`.
    let safetyBp = 12500

    /// Ticks of history the healer's draw is averaged over: twenty seconds.
    ///
    /// Deliberately not the party's three-second window. Damage lands continuously, so a
    /// short window reads a rate; healing arrives in bursts with nothing between them, so
    /// a short window mostly reads zero. Measured, `sim ttk 3` on seed 1: the healer's
    /// draw per Tick at 20 s intervals came out 1.05, 3.33, 3.33, 4.38, 2.55, 0.00, 6.87,
    /// 1.05, 0.00, 0.00 \u2014 the zeros are the seconds between the pulls and the 6.87 is
    /// one burst, and multiplied by a 50-second horizon either one is a reserve off by an
    /// order of magnitude. Twenty seconds is several heal cycles, which is the timescale
    /// the healing *requirement* actually has.
    ///
    /// Stored as an exponentially weighted mean scaled by this window, updated in O(1).
    let burnWindow = 400

    let empty = Forecast.empty

    /// Effective damage against a hostile. Overkill is not damage: the forecast
    /// divides the health still standing by the rate it is coming off, so counting a
    /// 200-point hit on a 5 hp corpse as 200 would claim the fight is forty times
    /// shorter than it is, and shortening the forecast is the one direction that makes
    /// the healers spend.
    let recordDamage (amount: int) (w: World) =
      if amount <= 0 then
        w
      else
        let f = w.Forecast

        { w with
            Forecast = { f with DamageThisTick = f.DamageThisTick + amount } }

    /// Mana a healer paid for something that is not damage.
    let recordSpend (healer: EntityId) (amount: int) (w: World) =
      if amount <= 0 then
        w
      else
        let f = w.Forecast
        let paid = f.BurnThisTick |> Map.tryFind healer |> Option.defaultValue 0

        { w with
            Forecast = { f with BurnThisTick = f.BurnThisTick |> Map.add healer (paid + amount) } }

    /// Mana a healer paid for damage. Kept apart from `recordSpend` because `settle`
    /// subtracts it back out of the pool drift it measures, and because it is the one
    /// kind of spend the reserve must never charge for: if the mana the gate paid out
    /// were charged to the reserve that licensed it, every payout would raise the bar
    /// and the gate would strangle itself.
    let recordDamageSpend (healer: EntityId) (amount: int) (w: World) =
      if amount <= 0 then
        w
      else
        let f = w.Forecast
        let paid = f.HealDamageThisTick |> Map.tryFind healer |> Option.defaultValue 0

        { w with
            Forecast =
              { f with HealDamageThisTick = f.HealDamageThisTick |> Map.add healer (paid + amount) } }

    /// Close one Tick: move the accumulators into the means and clear them. Called
    /// once at the end of `SimTick.step`, which hands over the World as the Tick found
    /// it, because a Tick is the sample and a hit is not.
    ///
    /// The damage mean is an EWMA in O(1): `acc - acc / window + sample` would hold
    /// `mean * window` at rest, but that subtraction truncates to zero for small
    /// accumulators and the mean would never forget, so the decay multiplies first
    /// (`acc * (window - 1) / window`) and reaches zero when nothing is hitting. That
    /// decay replaces the old reset between pulls: a pack dying is attrition and must
    /// not move the rate, a new pack joining only lengthens the health the same rate
    /// divides into, and a rest fades the rate on its own instead of pretending the
    /// party's damage fell to zero. A wipe is the one event that ends the run of pulls,
    /// so that is the one event that clears the Forecast.
    ///
    /// The healer's sample is its **net drain**, not its gross spend: what the pool
    /// actually lost to everything except damage. That is the quantity the reserve
    /// needs, because mana regenerated during the fight pays part of the fight. Charging
    /// the reserve for gross spend double-counts the regeneration and reserves about ten
    /// pools for a one-pool fight, which is how the first cut of this policy ended up
    /// refusing every damaging cast \u2014 measured, `healers 9`: the Priest's damage spend
    /// fell 7%, its healing need rose 15% because the kills got slower, and it finished
    /// the pull on *less* mana than before.
    ///
    /// `drain = (pool before - pool after) - damage spend`. The gross spend cancels out
    /// of that identity, so no regeneration model is needed and the five-second rule,
    /// the two-second server tick and the pool clipping at full are all accounted for by
    /// measurement rather than by a second implementation of them.
    let settle (before: World) (w: World) =
      if not (hasAliveParty w) then
        { w with Forecast = Forecast.empty }
      else
        let f = w.Forecast
        let mutable drain = f.HealerDrain
        let mutable engagedCount = 0
        let mutable allInPrev = true
        let prevEngaged = f.EngagedHostiles
        let hasHealDmg = not f.HealDamageThisTick.IsEmpty

        for e in w.Entities do
          if e.Faction = Party && e.Role = Some Healer then
            let d =
              match tryEntity e.Id before with
              | Some b ->
                let damage =
                  if hasHealDmg then
                    f.HealDamageThisTick |> Map.tryFind e.Id |> Option.defaultValue 0
                  else
                    0

                b.Resource - e.Resource - damage
              | None -> 0

            let past = drain |> Map.tryFind e.Id |> Option.defaultValue 0
            let next = past - past / burnWindow + d
            drain <- Map.add e.Id next drain
          elif e.Faction = Hostile && e.Health > 0 && e.Engaged then
            engagedCount <- engagedCount + 1
            if allInPrev && not (Set.contains e.Id prevEngaged) then
              allInPrev <- false

        let damage =
          (f.PartyDamage * (damageWindow - 1)) / damageWindow + f.DamageThisTick

        // Fast path: if the engaged set is identical to last tick's, reuse it
        // without allocating a new Set (saves O(N log N) per tick at scale).
        let engaged, grew =
          if allInPrev && engagedCount = prevEngaged.Count then
            prevEngaged, false
          else
            let mutable s = Set.empty
            for e in w.Entities do
              if e.Faction = Hostile && e.Health > 0 && e.Engaged then
                s <- Set.add e.Id s
            s, not allInPrev

        { w with
            Forecast =
              { PartyDamage = damage
                DamageTicks = min damageWindow (f.DamageTicks + 1)
                EngagedHostiles = engaged
                EngagementEpoch = (if grew then f.EngagementEpoch + 1 else f.EngagementEpoch)
                HealerDrain = drain
                DamageThisTick = 0
                BurnThisTick = Map.empty
                HealDamageThisTick = Map.empty } }

    /// The hostiles the party is actually fighting. The engaged test is the one that
    /// matters \u2014 `aliveHostiles` includes the room's sleepers, and a pull is one pack,
    /// not a floor.
    let private engagedHostiles (w: World) =
      w.Entities |> List.filter (fun e -> e.Faction = Hostile && alive e && e.Engaged)

    /// Damage still to do: every hostile the party is actually fighting, at the health
    /// it has left.
    let hostileHp (w: World) = engagedHostiles w |> List.sumBy (fun e -> e.Health)

    /// The healer's own dearest heal, which is the margin's floor: whatever the
    /// forecast gets wrong about the rate or the horizon, a healer holding this much can
    /// always pay for one more real heal. It is also the entire reserve for a healer with
    /// no history yet \u2014 the first second of a pull has no rate, and opening it with a
    /// nuke because nothing has been observed is the one wrong answer.
    let private oneHealCost (h: Entity) =
      h.Abilities
      |> List.filter (fun a ->
        a.Effects
        |> List.exists (function
          | Heal _
          | HealOverTime _ -> true
          | _ -> false))
      |> List.map (fun a -> a.ResourceCost)
      |> function
        | [] -> 0
        | costs -> List.max costs

    /// The clock a heal's mana must respect: the slowest heal the healer carries.
    ///
    /// A heal that finishes casting after the pull dies was never needed, so the last
    /// slot in the reserve is worth one delivery time, not a whole heal. The slowest
    /// cast is the one a late reserve would be too late for: the Priest's Heal is 3.0 s,
    /// the Shaman's Healing Wave 3.0 s, the Paladin's Holy Light 2.5 s and the Druid's
    /// Healing Touch 3.5 s. Instants are still spaced by the 1.5 s global cooldown the
    /// sim charges, so a kit that is all Renew still has a non-zero delivery clock.
    let private healDeliveryTicks (h: Entity) =
      let slowest =
        h.Abilities
        |> List.filter (fun a ->
          a.Effects
          |> List.exists (function
            | Heal _
            | HealOverTime _ -> true
            | _ -> false))
        |> List.map (fun a -> a.CastTicks)
        |> function
          | [] -> ticks 0
          | casts -> List.max casts

      max (secTicks 1.5) slowest

    /// Heals the margin caps at: two.
    ///
    /// Two is the floor for a long fight, not a fixed charge. One is not enough there,
    /// and the measurement is why: the reserve is the only thing between a healer's last
    /// damage cast and its last heal, and at 30 seeds with a one-heal margin the Priest
    /// finished 4 pulls out of 30 on literally no mana and its `hps` fell 2.8% \u2014 over
    /// the 2% the operator allows \u2014 because the healing it could not pay for was the
    /// healing the pull needed at the moment it ended. But a fixed two-heal floor also
    /// cannot shrink, and that condemned the endgame: at `ttk 4` it reserved 706 against
    /// a pool of 500 while the draw term was ~10, so the gate refused damage with four
    /// seconds left. `plan` therefore keeps `marginHeals` only while the horizon can
    /// still deliver that many heals and drops the count as it falls.
    let marginHeals = 2

    /// Everything the gate decides on, in one record, so `sim ttk` prints the numbers
    /// the gate used rather than a second derivation of them that can drift from it.
    type DamagePlan =
      { /// Effective hit points still standing in the pull.
        HostileHp: int
        /// Party damage per Tick from the EWMA, and the same per second.
        DamagePerTick: float
        Dps: float
        /// Seconds the pull has left at the mean's own rate. `infinity` when there is
        /// no rate to divide by, which the gate reads as "conserve".
        SecondsToKill: float
        /// Net mana the healer has been drawing from its own pool per Tick over the
        /// window: gross non-damage spend less regeneration. Negative means it was
        /// topping the pool up.
        BurnPerTick: float
        /// The margin: the heals that can still land before the pull dies, at the
        /// healer's own dearest heal price. Shrinks with the horizon.
        Safety: int
        /// Mana the healer must still be able to spend: net drain \u00d7 horizon + margin.
        Reserve: int
        Pool: int
        /// What is left over for damage. Negative means nothing is.
        Budget: int
        /// The pull's generation: increments when a new pack joins the engaged set.
        /// Zero before the first engage; a shrink or a corpse does not move it.
        EngagementEpoch: int
        /// Ticks the forecast puts between here and the end of the pull.
        TicksLeft: float
        /// Ticks of party damage behind the forecast. Below `minSamples` every number
        /// above is a prior, not a measurement.
        Samples: int }

    let plan (w: World) (h: Entity) : DamagePlan =
      let f = w.Forecast
      let samples = f.DamageTicks
      let perSecond = float (secTicks 1.0)

      // The accumulator holds `mean * damageWindow`, so dividing by the window is the
      // mean. There is no per-sample division to normalise: the EWMA already carries
      // its own history weight, and a fresh accumulator understates the rate (which
      // reads as a longer fight, the conservative direction) until the warm-up ends.
      let damagePerTick = float f.PartyDamage / float damageWindow

      let dps = damagePerTick * perSecond
      let hp = hostileHp w
      let trusted = samples >= minSamples && damagePerTick > 0.0

      let secondsToKill =
        if hp <= 0 then
          // Nothing is engaged: the pull is over and nothing is owed.
          0.0
        elif trusted then
          float hp / dps
        else
          infinity

      // Ten minutes caps the arithmetic on a pull the party cannot finish. Anything
      // longer than that is "conserve forever" either way, and an uncapped float would
      // overflow the int below.
      let ticksLeft = min (600.0 * perSecond) (secondsToKill * perSecond)

      let burnPerTick =
        f.HealerDrain
        |> Map.tryFind h.Id
        |> Option.map (fun acc -> float acc / float burnWindow)
        |> Option.defaultValue 0.0

      // The margin is the heals that can still land before the pull dies, not a fixed
      // pair. A heal takes `healDeliveryTicks` to arrive, so a horizon shorter than one
      // cast has room for no heal at all and a horizon of two casts has room for one.
      // This is the endgame fix: `2 x dearest heal` cannot shrink, which is what left a
      // 706 reserve against a 500 pool at `ttk 4` while the draw term was ~10, and
      // refused a Smite with four seconds left. `marginHeals` still caps a long pull.
      let healsLeft =
        if ticksLeft <= 0.0 then
          0
        else
          min marginHeals (int (ticksLeft / float (healDeliveryTicks h)))

      let safety = healsLeft * oneHealCost h

      // A healer whose pool is drifting *up* owes the fight nothing, and a negative
      // reserve would be a licence to spend mana it does not have. The signed value is
      // kept in `BurnPerTick` because "topping up" is a different measurement from
      // "breaking even" and `sim ttk` should say which one it saw.
      let drawn = max 0.0 burnPerTick

      let reserve =
        // `trusted` and not the finiteness of `ticksLeft`: the ten-minute cap below is
        // applied to an infinite `secondsToKill` too, so testing the capped value never
        // fires. Measured, `sim ttk 2` before this: a Tick whose window held 19 samples
        // and no measurable rate printed `ttk Infinity` beside `reserve 259`, i.e. the
        // one case that must conserve was handing out the whole pool.
        if not trusted then
          // No rate: hold the whole pool. A healer that has never been observed
          // spending has no idea what the fight will cost it, and the cheap error is to
          // heal from full rather than to nuke from full.
          h.MaxResource
        else
          int (drawn * ticksLeft * float safetyBp / 10000.0) + safety

      { HostileHp = hp
        DamagePerTick = damagePerTick
        Dps = dps
        SecondsToKill = secondsToKill
        BurnPerTick = burnPerTick
        Safety = safety
        Reserve = reserve
        Pool = h.Resource
        Budget = h.Resource - reserve
        EngagementEpoch = f.EngagementEpoch
        TicksLeft = ticksLeft
        Samples = samples }

    /// The cheapest damaging spell the healer could press, which is the least the gate
    /// has to allow before any damage can flow at all.
    let cheapestDamage (h: Entity) =
      h.Abilities
      |> List.filter (fun a -> Ability.isDamaging a && a.ResourceCost > 0)
      |> List.sortBy (fun a -> a.ResourceCost)
      |> List.tryHead

    /// The gate: a damaging spell may be paid for only out of what the reserve does not
    /// claim. Long pull \u2192 the reserve exceeds the pool \u2192 no damage at all. Short pull
    /// \u2192 the reserve shrinks to the heals that can still land \u2192 almost anything.
    let maySpendOnDamage (w: World) (h: Entity) (cost: int) = (plan w h).Budget >= cost

    /// Whether this healer may spend on damage at all, asked once for the whole
    /// fallback rather than per spell.
    ///
    /// The fallback asks `rangedOrders` for the next thing to press, and `rangedOrders`
    /// answers with a `MoveTo` when it is out of reach \u2014 so filtering its answer after
    /// the fact still lets a refused healer walk towards the mob it may not hit. The
    /// operator's rule is that a healer does not walk in for damage it cannot cast, so
    /// the test goes before the call and a broke healer stands still.
    let mayDamage (w: World) (h: Entity) =
      match cheapestDamage h with
      | Some a -> maySpendOnDamage w h a.ResourceCost
      | None -> false
