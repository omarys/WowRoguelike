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

  /// Roll for a critical hit against `critChanceBp`. A spell crit deals 150%.
  let rollCrit (critChanceBp: int) (amount: int) (w: World) : int * World =
    let roll, w = rollRange 0 9999 w
    if roll < critChanceBp then amount * 150 / 100, w else amount, w

  /// Base chance (basis points) that damage interrupts a cast in progress (Q5a).
  let pushbackBaseBp = 1000

  // -------------------------------------------------------------------------
  // The attack table
  // -------------------------------------------------------------------------

  /// Where a swing landed. Vanilla resolves a melee attack with ONE roll whose
  /// bands descend in this order, so a dodged attack was never a crit in the
  /// first place and a blocked hit cannot crit.
  type SwingResult =
    | Missed
    | Dodged
    | Parried
    | Glanced
    | Blocked
    | Critical
    | Hit

  /// The chance of each band, in basis points, for one attacker/target pair.
  type SwingChances =
    { Miss: int
      Dodge: int
      Parry: int
      Glancing: int
      Block: int
      Crit: int }

  /// Defence minus weapon skill. Both are five per level, so this is five times
  /// the level gap: a level-24 attacker against a level-19 mob is -25.
  let levelDelta (attacker: Entity) (target: Entity) = target.Level * 5 - attacker.Level * 5

  /// Chance (basis points) that a swing misses.
  ///
  /// The two coefficients are not interchangeable: 0.1% per skill point when the
  /// DEFENDER is a mob, 0.04% per point when the defender is a player (less
  /// 0.02% per point the attacker has over it). Vanilla floors it at 1%.
  let missChanceBp (attacker: Entity) (target: Entity) =
    let delta = levelDelta attacker target

    let bp =
      if target.Faction = Party then
        500 + max 0 delta * 4 - max 0 -delta * 2
      elif delta <= 10 then
        500 + delta * 10
      else
        600 + (delta - 10) * 40

    max 100 bp

  /// A player's dodge is its agility term plus 0.04% per point of defence over
  /// the attacker's skill; a mob's is 5% flat plus 0.1% per level it has on you.
  let dodgeChanceBp (attacker: Entity) (target: Entity) =
    let delta = levelDelta attacker target
    let boon = target.Auras |> List.sumBy Aura.dodgeBonusBp

    if target.Faction = Party then target.DodgeChanceBp + boon + max 0 delta * 4
    else max 0 (target.DodgeChanceBp + 500 + delta * 10)

  /// Mobs parry; the Party does not, so its parry is zero and needs no field.
  let parryChanceBp (attacker: Entity) (target: Entity) =
    if target.Faction = Party then 0
    else max 0 (500 + levelDelta attacker target * 10)

  /// Glancing blows are a penalty for fighting something at or above your level:
  /// the chance is 10% at equal level and falls to nothing two levels below, so
  /// a level-24 party never glances off level-19 trash.
  let glancingChanceBp (attacker: Entity) (target: Entity) =
    if target.Faction = Party then 0
    else max 0 (1000 + levelDelta attacker target * 200)

  /// Damage multiplier on a glancing blow, in basis points.
  let glancingMultiplierBp (attacker: Entity) (target: Entity) =
    let reduction = 50 * max 0 (-levelDelta attacker target)
    min 9900 (max 4000 (13000 - reduction))

  /// Hitting something below your level is a real crit bonus, not suppression:
  /// 0.04% per point of weapon skill over its defence.
  let levelCritBonusBp (attacker: Entity) (target: Entity) =
    if target.Faction = Party then 0 else max 0 (-levelDelta attacker target) * 4

  /// The table as a pure function of the roll, so the bands can be tested
  /// without a World or a seed.
  let attackTable (c: SwingChances) (roll: int) : SwingResult =
    let dodge = c.Miss + c.Dodge
    let parry = dodge + c.Parry
    let glancing = parry + c.Glancing
    let block = glancing + c.Block
    let crit = block + c.Crit

    if roll < c.Miss then Missed
    elif roll < dodge then Dodged
    elif roll < parry then Parried
    elif roll < glancing then Glanced
    elif roll < block then Blocked
    elif roll < crit then Critical
    else Hit

  /// Every band except the ability's own crit bonus, which the caller adds.
  let chanceOf (attacker: Entity) (target: Entity) : SwingChances =
    { Miss = missChanceBp attacker target
      Dodge = dodgeChanceBp attacker target
      Parry = parryChanceBp attacker target
      Glancing = glancingChanceBp attacker target
      Block = target.BlockChanceBp + (target.Auras |> List.sumBy Aura.blockBonusBp)
      Crit = attacker.CritChanceBp + (attacker.Auras |> List.sumBy Aura.critBonusBp) + levelCritBonusBp attacker target }

  /// Chance (basis points) a spell misses or is fully resisted, by level
  /// difference. A miss is a full resist — partial resists come only from
  /// resistance, which nothing here carries — and vanilla floors it at 1%.
  let spellMissChanceBp (caster: Entity) (target: Entity) =
    match target.Level - caster.Level with
    | d when d <= 0 -> 100
    | 1 -> 500
    | 2 -> 600
    | 3 -> 1700
    | 4 -> 2800
    | d -> min 9900 (2800 + (d - 4) * 1100)

  /// Physical damage reduction, WoW's real curve: A / (A + 400 + 85 * attacker
  /// level), capped at 75%. The ATTACKER's level goes in the denominator: a
  /// level-24 tank in plate (1248 armour) takes 36% less from a level-19 mob.
  let armorMitigationBp (attackerLevel: int) (armor: int) =
    if armor <= 0 then 0
    else min 7500 (armor * 10000 / (armor + 400 + 85 * attackerLevel))

  /// Roll a spell through the hit table. Vanilla resists are all-or-nothing: a
  /// miss deals zero, never a fraction, because partial resists come only from
  /// resistance and nothing here carries resistance.
  let spellHit
    (spellName: string)
    (caster: Entity)
    (targetId: EntityId)
    (amount: int)
    (w: World)
    : int * World =
    match tryEntity targetId w with
    | None -> amount, w
    | Some t ->
      let roll, w = rollRange 0 9999 w

      if roll < spellMissChanceBp caster t then
        0, logLine (sprintf "%s resists %s" t.Name spellName) w
      else
        amount, w

  /// One physical hit from `attacker` on `targetId`: the attack table, then
  /// armour. A melee crit is 200% — the spell multiplier is 150% and applying it
  /// to swings too is what left physical damage short. Logs what it avoided and
  /// returns the damage actually dealt.
  let meleeStrike
    (attacker: Entity)
    (targetId: EntityId)
    (amount: int)
    (critBonusBp: int)
    (w: World)
    : int * World =
    match tryEntity targetId w with
    | None -> 0, w
    | Some t when not (alive t) || amount <= 0 -> amount, w
    | Some t ->
      let chances = { chanceOf attacker t with Crit = attacker.CritChanceBp + critBonusBp + levelCritBonusBp attacker t }
      let roll, w = rollRange 0 9999 w

      let mitigate raw =
        let armor = max 0 (t.Armor + (t.Auras |> List.sumBy Aura.armorBonus) - (t.Auras |> List.sumBy Aura.armorReduction))
        raw * (10000 - armorMitigationBp attacker.Level armor) / 10000

      match attackTable chances roll with
      | Missed -> 0, logLine (sprintf "%s misses %s" attacker.Name t.Name) w
      | Dodged -> 0, logLine (sprintf "%s dodges %s" t.Name attacker.Name) w
      | Parried -> 0, logLine (sprintf "%s parries %s" t.Name attacker.Name) w
      | Glanced -> mitigate (amount * glancingMultiplierBp attacker t / 10000), w
      | Blocked -> mitigate (max 0 (amount - t.BlockValue)), w
      | Critical -> mitigate (amount * 200 / 100), w
      | Hit -> mitigate amount, w

  /// Damage wakes a sleeper, as does any other hostile action — see `wake`. A
  /// corpse stops moving and stops casting, so it interpolates nowhere.
  let dealDamage (targetId: EntityId) (amount: int) (w: World) =
    // Shield Wall takes its share before anything lands, so it covers every
    // source: a swing, a spell, a DoT tick.
    let amount =
      match tryEntity targetId w with
      | Some t ->
        amount * (10000 - (t.Auras |> List.sumBy Aura.damageReductionBp)) / 10000
      | None -> amount

    // What a creature was before this hit: needed to notice a death, which is
    // the one thing the quest tracker has to hear about.
    let victim =
      match tryEntity targetId w with
      | Some t when t.Faction = Hostile && alive t -> Some t.Name
      | _ -> None

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

    // A kill credits the objectives watching for it, and a quest that finishes
    // here can wake a dormant boss.
    let w =
      match victim with
      | Some name when (tryEntity targetId w |> Option.exists (fun t -> not (alive t))) ->
        SimState.recordKill name w
      | _ -> w

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
          let rolled, w = rollRange lo hi w
          let critBonus = if ability.Name = "Backstab" then actor.BackstabCritBonusBp else 0
          let amount, w = meleeStrike actor targetId rolled critBonus w
          let w = dealDamage targetId amount w
          // Only the Party builds threat on Mobs.
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w

          if amount > 0 then
            w |> logLine (sprintf "%s hits %s for %d" actor.Name targetName amount)
          else
            w

        | WeaponDamage(multiplierBp, bonus) ->
          // The same roll a swing would make, scaled, plus the ability's own
          // bonus. The attack-power part of a swing is weapon damage too, so it
          // takes the multiplier: 150% of a Backstab is 150% of the whole swing,
          // not 150% of the weapon's own dice plus a flat bonus. Without this a
          // Backstab does less per point of energy than Sinister Strike.
          let rolled, w =
            match actor.AutoAttack with
            | Some a -> rollRange a.Min a.Max w
            | None -> 0, w

          let swing = rolled + actor.MeleeDamageBonus
          let critBonus = if ability.Name = "Backstab" then actor.BackstabCritBonusBp else 0
          let amount, w = meleeStrike actor targetId (swing * multiplierBp / 10000 + bonus) critBonus w
          let w = dealDamage targetId amount w
          // Only the Party builds threat on Mobs.
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w

          if amount > 0 then
            w |> logLine (sprintf "%s's %s hits %s for %d" actor.Name ability.Name targetName amount)
          else
            w

        | SpellDamage(lo, hi) ->
          let rolled, w = rollRange lo hi w
          let amount, w = spellHit ability.Name actor targetId rolled w
          let amount, w = rollCrit actor.CritChanceBp amount w
          let w = dealDamage targetId amount w
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w
          w |> logLine (sprintf "%s hits %s for %d" actor.Name targetName amount)

        | Heal(lo, hi) ->
          let rolled, w = rollRange lo hi w

          // The tree's share of the heal lands on the rolled number, beside the
          // flat bonus healing Spiritual Guidance turns Spirit into. One roll,
          // one multiply, and a character with no talents multiplies by 10000.
          let amount =
            (rolled + actor.BonusHealing) * Talents.healBp ability.Name actor.Talents / 10000

          let before =
            tryEntity targetId w |> Option.map (fun t -> t.Health) |> Option.defaultValue 0

          let w = healEntity targetId amount w

          let after =
            tryEntity targetId w |> Option.map (fun t -> t.Health) |> Option.defaultValue 0

          let effective = after - before

          let w =
            if actor.Faction = Party then addHealThreat actor effective w else w

          // Illumination: a heal that crits hands its mana cost back. The crit
          // roll happens only when a talent pays for it, because a plain heal
          // crit changes no number this model computes.
          // shortcut: the crit pays the refund and does not also enlarge the
          // heal; double the amount when heal crits are meant to show as 1.5x.
          let w =
            if actor.Talents.ManaRefundOnCritBp <= 0 then
              w
            else
              let roll, w = rollRange 0 9999 w

              if roll < actor.CritChanceBp then
                let refund = ability.ResourceCost * actor.Talents.ManaRefundOnCritBp / 10000

                w
                |> mapEntity actorId (fun e ->
                  { e with Resource = min e.MaxResource (e.Resource + refund) })
                |> logLine (sprintf "%s's heal crits and returns %d mana" actor.Name refund)
              else
                w

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
          | Some t when not (isSleeping t) && CreatureType.sleepable t.CreatureType ->
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
          | Some t -> w |> logLine (sprintf "%s is immune to %s" t.Name ability.Name)
          | None -> w

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
          // A shout: every hostile in range is forced, not just the one clicked.
          // Distance only — a taunt carries around a corner where a bolt would not.
          if actor.Faction <> Party then
            w
          else
            w.Entities
            |> List.filter (fun m ->
              m.Faction = Hostile && alive m && withinRange ability.Range actor m)
            |> List.fold (fun w m ->
              let highest = m.Threat |> Map.toList |> List.map snd |> List.fold max 0

              w
              |> mapEntity m.Id (fun mob ->
                { mob with
                    ForcedTarget = Some actorId
                    Engaged = true
                    Threat = mob.Threat |> Map.add actorId (max highest (threatOf actorId mob)) })
              |> wake m.Id
              |> logLine (sprintf "%s taunts %s" actor.Name m.Name)) w

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
            let dealt, w = spellHit ability.Name actor t.Id amount w
            let w = dealDamage t.Id dealt w
            let w = if actor.Faction = Party then addThreat t.Id actor dealt w else w

            if dealt > 0 then
              w |> logLine (sprintf "%s blasts %s for %d" actor.Name t.Name dealt)
            else
              w) w

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
          | Some t when CreatureType.polymorphable t.CreatureType ->
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
          | Some t -> w |> logLine (sprintf "%s is immune to %s" t.Name ability.Name)
          | None -> w

        | HealOverTime(duration, perTick) ->
          // The tree's share of a heal-over-time is applied to its per-Tick
          // amount, which is what Improved Rejuvenation buys.
          let perTick = perTick * Talents.healBp ability.Name actor.Talents / 10000

          match tryEntity targetId w with
          | Some t ->
            w
            |> mapEntity targetId (fun e ->
              { e with
                  Auras = Aura.refresh (Rejuvenation(duration, perTick, ability.Name)) e.Auras })
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

          w |> logLine (sprintf "%s threatens %s for %d" actor.Name targetName amount)

        | ComboPoint count ->
          w
          |> mapEntity actorId (fun e ->
            { e with ComboPoints = min 5 (e.ComboPoints + count) })
          |> logLine (sprintf "%s builds a combo point" actor.Name)

        | Finisher(lo, hi, perPoint) ->
          // The points are read off the actor as it stood when the ability was
          // resolved, then spent: a finisher is a decision, not a formality.
          let points = actor.ComboPoints
          let extra = perPoint * max 0 (points - 1)
          let rolled, w = rollRange (lo + extra) (hi + extra) w
          let amount, w = meleeStrike actor targetId rolled 0 w
          let w = dealDamage targetId amount w
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w

          w
          |> mapEntity actorId (fun e -> { e with ComboPoints = 0 })
          |> logLine (
            sprintf "%s spends %d combo points on %s for %d" actor.Name points targetName amount
          )

        | DamageOverTime(duration, perTick) ->
          match tryEntity targetId w with
          | Some t ->
            w
            |> mapEntity targetId (fun e ->
              { e with Auras = Aura.refresh (Corrupted(duration, perTick)) e.Auras })
            |> logLine (sprintf "%s afflicts %s" actor.Name t.Name)
          | None -> w

        | Summon spec ->
          // A pet is a real Party entity: it fights, it can be killed, and the
          // existing Dps behaviour drives it without knowing it was summoned.
          // It needs a free tile to appear on, which is why a summon can fail —
          // the same reason a Hunter cannot call a pet into a full corridor.
          let spot =
            Path.neighbours
              (fun p -> Grid.isFloor w.Grid p && not (claimedByOther w actorId p))
              actor.Pos
            |> List.sortBy (fun p -> Pos.chebyshev actor.Pos p)
            |> List.tryHead

          match spot with
          | None ->
            w |> logLine (sprintf "%s has no room to summon %s" actor.Name spec.Name)
          | Some p ->
            let id =
              w.Entities
              |> List.map (fun e -> match e.Id with EntityId n -> n)
              |> List.fold max 0
              |> (+) 1
              |> EntityId

            let summon =
              { Id = id
                Name = spec.Name
                Level = actor.Level
                CreatureType = CreatureType.Beast
                OffHand = false
                Faction = Party
                Role = Some Dps
                Dormant = false
                MaxHealth = spec.Health
                Health = spec.Health
                Pos = p
                Destination = None
                MoveTicksLeft = ticks 0
                MoveTicksTotal = ticks 0
                MoveTicksPerTile = spec.MoveTicksPerTile
                Goal = None
                Path = []
                StallTicks = ticks 0
                Auras = []
                Casting = None
                Cooldowns = Map.empty
                Resource = 0
                MaxResource = 0
                ResourceRegenPerTick = 0
                ResourceKind = Mana
                ComboPoints = 0
                Abilities = []
                AutoAttack =
                  spec.Swing
                  |> Option.map (fun (lo, hi, sw) ->
                    { Min = lo; Max = hi; Ticks = sw; Range = 1 })
                SwingTicksLeft = ticks 0
                Threat = Map.empty
                Target = None
                ForcedTarget = None
                ThreatMultiplierBp = 10000
                Stats = Stats.zero
                CritChanceBp = 0
                MeleeDamageBonus = 0
                Armor = spec.Armor
                ArmorClass = Leather
                DodgeChanceBp = 0
                BlockChanceBp = 0
                BlockValue = 0
                PushbackResistanceBp = 0
                BackstabCritBonusBp = 0
                BonusHealing = 0
                Talents = Talents.none
                Totem = spec.Totem
                Engaged = true
                CallForHelpBelowHealthPct = None
                HasCalledForHelp = false
                Owner = Some actorId
                ExpiresInTicks = spec.Lifetime }

            w
            |> (fun w -> { w with Entities = summon :: w.Entities })
            |> logLine (sprintf "%s summons %s" actor.Name spec.Name)

        | Charge ->
          match tryEntity actorId w, tryEntity targetId w with
          | Some a, Some t ->
            let spot =
              Path.neighbours
                (fun p -> Grid.isFloor w.Grid p && not (claimedByOther w actorId p))
                t.Pos
              |> List.sortBy (fun p -> Pos.chebyshev a.Pos p)
              |> List.tryHead

            match spot with
            | Some p ->
              w
              |> mapEntity actorId (fun e ->
                { e with
                    Pos = p
                    Goal = None
                    Path = []
                    Destination = None
                    StallTicks = ticks 0 })
              |> logLine (sprintf "%s charges %s" a.Name t.Name)
            | None -> w
          | _ -> w

        | Disarm duration ->
          match tryEntity targetId w with
          | Some t ->
            w
            |> mapEntity targetId (fun e ->
              { e with Auras = Aura.refresh (Disarmed duration) e.Auras })
            |> logLine (sprintf "%s disarms %s" actor.Name t.Name)
          | None -> w

        | Blocks duration ->
          w
          |> mapEntity actor.Id (fun e -> { e with Auras = Aura.refresh (Blocking duration) e.Auras })
          |> logLine (sprintf "%s raises a shield" actor.Name)

        | ReducesDamage(duration, reductionBp) ->
          w
          |> mapEntity actor.Id (fun e ->
            { e with
                Auras = Aura.refresh (ShieldWalled(duration, reductionBp)) e.Auras })
          |> logLine (sprintf "%s braces behind the shield" actor.Name))


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
