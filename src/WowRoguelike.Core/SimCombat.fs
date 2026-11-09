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

  /// Roll for a critical hit against `critChanceBp`. A spell crit deals 150%,
  /// plus whatever `critDamageBp` the tree adds: Mortal Shots is 3000, so a
  /// critted Arcane Shot is 180% and not 150%. Points and not a factor, so the
  /// two sites that crit — this one at 150% for spells, `meleeStrike` at 200% for
  /// swings — can each keep their own base and still take one number.
  ///
  /// The flag is the roll, not a comparison of the amount against the roll it
  /// came from: a talent that fires on "your critical strikes" has to know, and
  /// a 150% multiplier can round to the number it started from.
  let rollCrit
    (critChanceBp: int)
    (critDamageBp: int)
    (amount: int)
    (w: World)
    : int * bool * World =
    let roll, w = rollRange 0 9999 w

    if roll < critChanceBp then
      amount * (15000 + critDamageBp) / 10000, true, w
    else
      amount, false, w

  /// Piercing Shots: a critical strike makes the thing it hit bleed for a share of
  /// the damage it took, over eight seconds. The first talent in the port that
  /// pays the *target* on a crit, which is why it is a rule here and not a `Proc`:
  /// every `ProcResult` acts on its holder (rage, a free spell, a cooldown reset),
  /// and this one needs the victim and the number the crit dealt.
  ///
  /// It is threat-free by construction rather than by a flag: the ticks are paid
  /// by `SimTick.advanceAuraDamage`, which routes through `dealDamage` and never
  /// through `addThreat` — threat is charged where a spell is cast, and this bleed
  /// is never cast.
  let piercingBleed (attacker: Entity) (targetId: EntityId) (dealt: int) (w: World) : World =
    let share = attacker.Talents.CritBleedShareBp

    if share <= 0 || dealt <= 0 then
      w
    else
      match tryEntity targetId w with
      | Some t when alive t ->
        // "Over 8 sec" is eight ticks, because a periodic aura in this sim fires
        // once a second.
        let perTick = dealt * share / 10000 / 8

        if perTick <= 0 then
          w
        else
          w
          |> mapEntity targetId (fun e ->
            { e with
                Auras = Aura.refresh (Bleeding(secTicks 8.0, perTick, Some attacker.Id)) e.Auras })
          |> logLine (sprintf "%s's critical strike leaves %s bleeding" attacker.Name t.Name)
      | _ -> w

  /// Chance (basis points), before resistance, that a damaging hit pushes a cast
  /// back. The era's answer is every hit, and the cost is time rather than the
  /// cast: 0.5 s added to what is left, at most twice per cast.
  let pushbackBaseBp = 10000

  /// What one pushback costs, and how many a single cast absorbs. Two is the
  /// era's cap, and the cap is what keeps a caster standing in a fight from being
  /// pushed into an endless cast.
  let pushbackDelayTicks = secTicks 0.5

  let pushbackMaxPerCast = 2

  /// The two leaps. They share the rule the engine enforces rather than the AI: a
  /// Warrior cannot leap to something it cannot see, so a Charge or an Intercept
  /// through a wall is refused before its cost or its cooldown is paid.
  let leapsTo (e: Effect) =
    match e with
    | Charge
    | Intercept -> true
    | _ -> false

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
  /// Improved Slaying pays against three creature types and nothing else, so it
  /// cannot be a static multiplier: it needs the target. Both halves of the
  /// talent read the same number — the damage share multiplies what lands, and
  /// the crit-damage share is points on the multiplier, the convention
  /// `Talents.CritDamageBp` already set — so one field covers "[1/2/3]% damage
  /// and crit damage".
  ///
  /// OctoWoW lists four types and the fourth is Giants; `CreatureType` has no
  /// such case and no content is one, so it is unreachable here rather than
  /// special-cased into the match.
  let slayingBp (attacker: Entity) (target: Entity) : int =
    match target.CreatureType with
    | CreatureType.Beast
    | CreatureType.Dragonkin
    | CreatureType.Humanoid -> attacker.Talents.SlayingBp
    | _ -> 0

  /// The same for a site holding only an id, which is most of them.
  let slayingAgainst (attacker: Entity) (targetId: EntityId) (w: World) : int =
    match tryEntity targetId w with
    | Some t -> slayingBp attacker t
    | None -> 0

  /// Spell damage to one target: the resist check, then the slaying share. The
  /// share multiplies here rather than at the call because this is the one place
  /// every spell — single-target and each body inside an area — passes through.
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
        amount * (10000 + slayingBp caster t) / 10000, w

  /// Hand one proc's result to its holder. Reads the entity back out of the
  /// world rather than trusting the caller's copy: a proc fired mid-swing must
  /// land on the character that is standing there now.
  let private applyProc (p: Proc) (id: EntityId) (w: World) =
    match tryEntity id w with
    | None -> w
    | Some e ->
      match p.Then with
      // Flat, not `gainRage`: that function converts damage into a level-scaled
      // trickle, and a talent that says "generates 5 rage" is five rage.
      | GrantResource n when e.ResourceKind = Rage && n > 0 && e.MaxResource > 0 ->
        w
        |> mapEntity id (fun x -> { x with Resource = min x.MaxResource (x.Resource + n) })
        |> logLine (sprintf "%s: %s generates %d rage" e.Name p.Talent n)
      | GrantResource _ -> w
      | EmpowerNext(duration, empower) ->
        w
        |> mapEntity id (fun x -> { x with Auras = Aura.refresh (Empowered(duration, empower)) x.Auras })
        |> logLine (sprintf "%s: %s procs" e.Name p.Talent)
      // `Map.remove` reads as "never cast", which is ready: the ability's own
      // cooldown entry is the only thing standing between it and its next cast.
      | ResetCooldown ability ->
        w
        |> mapEntity id (fun x -> { x with Cooldowns = Map.remove ability x.Cooldowns })
        |> logLine (sprintf "%s: %s resets %s" e.Name p.Talent ability)

  /// Fire whatever this character's build hangs off `ev`. Called where the event
  /// is already decided — a block of `meleeStrike`, a crit of `meleeStrike`, a
  /// hit of `spellDamage` — so no talent needs a branch of its own.
  let procOn (ev: ProcEvent) (id: EntityId) (w0: World) : World =
    match tryEntity id w0 with
    | None -> w0
    | Some e ->
      ((w0, e), Talents.procsOf ev e.Talents)
      ||> List.fold (fun (w, e) p ->
        let cooling =
          (e.Cooldowns |> Map.tryFind p.Talent |> Option.defaultValue (ticks 0)) > ticks 0

        if cooling then
          w, e
        else
          // A guaranteed proc draws nothing at all: the seed's stream stays
          // where a build carrying no procs left it. Zero is the roll that cannot
          // miss, since a draw of 9999 still fails a hundred-percent chance...
          // which is why the shortcut cannot simply reuse the top of the range.
          let roll, w = if p.ChanceBp >= 10000 then 0, w else rollRange 0 9999 w

          if roll >= p.ChanceBp then
            w, e
          else
            let w = applyProc p id w

            let w =
              if p.InternalCooldown > ticks 0 then
                mapEntity
                  id
                  (fun x ->
                    { x with
                        Cooldowns = x.Cooldowns |> Map.add p.Talent p.InternalCooldown })
                  w
              else
                w

            w, (tryEntity id w |> Option.defaultValue e))
      |> fst

  /// One physical hit from `attacker` on `targetId`: the attack table, then
  /// armour. A melee crit is 200% — the spell multiplier is 150% and applying it
  /// to swings too is what left physical damage short. Logs what it avoided and
  /// returns the damage actually dealt.
  let meleeStrike
    (attacker: Entity)
    (targetId: EntityId)
    (amount: int)
    (critBonusBp: int)
    (ability: string option)
    (w: World)
    : int * World =
    match tryEntity targetId w with
    | None -> 0, w
    | Some t when not (alive t) || amount <= 0 -> amount, w
    | Some t ->
      // Before the attack table, so it reads as "3% more damage" and not "3% more
      // of what got through the armour".
      let slaying = slayingBp attacker t
      let amount = amount * (10000 + slaying) / 10000
      let chances = { chanceOf attacker t with Crit = attacker.CritChanceBp + critBonusBp + levelCritBonusBp attacker t }
      let roll, w = rollRange 0 9999 w

      let mitigate raw =
        let armor =
          max 0
            (t.Armor
             + (t.Auras |> List.sumBy Aura.armorBonus)
             - (t.Auras |> List.sumBy Aura.armorReduction))

        // Inspiration / Ancestral Healing: a share on top of the armour the
        // target wears, so it is a multiplier and not a flat add. A target with
        // nothing procced multiplies by 10000.
        let armor = armor * (10000 + Aura.armorPctBp t.Auras) / 10000
        raw * (10000 - armorMitigationBp attacker.Level armor) / 10000

      match attackTable chances roll with
      | Missed -> 0, logLine (sprintf "%s misses %s" attacker.Name t.Name) w
      | Dodged -> 0, logLine (sprintf "%s dodges %s" t.Name attacker.Name) w
      | Parried -> 0, logLine (sprintf "%s parries %s" t.Name attacker.Name) w
      | Glanced -> mitigate (amount * glancingMultiplierBp attacker t / 10000), w
      | Blocked ->
        // A block is its own event: Shield Specialization pays on the block
        // whether or not the hit got through the shield.
        mitigate (max 0 (amount - t.BlockValue)), (w |> procOn OnBlock t.Id)
      | Critical ->
        // And a crit is the attacker's event, whatever it landed on. A swing's
        // base is 200% where a spell's is 150%, and the tree's crit-damage
        // talents add the same points to either — Mortal Shots always, Improved
        // Slaying only against the types it names.
        let dealt = mitigate (amount * (20000 + attacker.Talents.CritDamageBp + slaying) / 10000)

        let w = w |> procOn OnCrit attacker.Id

        // The named twin, for the talents that fire off one ability's crits
        // rather than every crit the character lands. A white swing has no name
        // and gets only the unnamed event.
        let w =
          match ability with
          | Some name -> procOn (OnCritOf name) attacker.Id w
          | None -> w

        dealt, (w |> piercingBleed attacker targetId dealt)
      | Hit -> mitigate amount, w

  /// Damage wakes a sleeper, as does any other hostile action — see `wake`. A
  /// corpse stops moving and stops casting, so it interpolates nowhere.
  /// A Warrior builds rage by fighting instead of waiting on a pool. Vanilla
  /// converts damage to rage at a rate that falls with level — `2.5 * damage /
  /// level` — which is 12 rage for a 300-damage hit at 60 and 10 for a 100-damage
  /// hit at 24, so a level-24 Warrior charging Sunder Armor and Heroic Strike
  /// (15 and 15) can afford roughly one every few seconds while he is trading.
  ///
  /// Every source that carries an actor pays: his swings, his weapon abilities,
  /// his spell damage, and damage he takes. A periodic aura tick (`dealDamage`
  /// from `SimTick`'s burn phase) does not, because the burn carries no actor —
  /// a Warrior bleeding out earns nothing for it.
  let gainRage (e: Entity) (amount: int) (w: World) =
    if e.ResourceKind <> Rage || amount <= 0 || e.MaxResource <= 0 then
      w
    else
      let gain = max 1 (amount * 5 / (2 * max 1 e.Level))

      mapEntity e.Id (fun x -> { x with Resource = min x.MaxResource (x.Resource + gain) }) w

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

    // Taking a hit is paid for in rage, at the same rate as dealing one.
    let w =
      match tryEntity targetId w with
      | Some t -> gainRage t amount w
      | None -> w

    let w =
      mapEntity
        targetId
        (fun t ->
          let hp = max 0 (t.Health - amount)

          { t with
              Health = hp
              // Being hit pulls. Until this, only proximity inside `aggroRadius`
              // (plus line of sight) could wake a mob, so every ranged opener in the
              // game — a Mage's Fireball, a Hunter's Arcane Shot, a Warrior's
              // crossbow — landed its damage on something that never reacted, and the
              // party had to walk into the room to start a fight. Damage is the oldest
              // pull in the game and it costs one flag.
              Engaged = t.Engaged || (t.Faction = Hostile && hp > 0)
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

    // Pushback: damage pushes an interruptible cast in progress back by half a
    // second instead of cancelling it, at most twice per cast, and the target's
    // resistance takes a bite out of the chance. Time is what a caster pays for
    // standing in the fight — cancelling outright made a single hit worth a whole
    // cast, which priced every cast-time talent in the game at nearly nothing.
    if amount <= 0 then
      w
    else
      match tryEntity targetId w with
      | Some t when
        alive t
        && (t.Casting
            |> Option.exists (fun c -> c.Ability.Interruptible && c.Pushes < pushbackMaxPerCast))
        ->
        let roll, w = rollRange 0 9999 w
        let chance = pushbackBaseBp * (10000 - t.PushbackResistanceBp) / 10000

        if roll < chance then
          w
          |> mapEntity targetId (fun e ->
            match e.Casting with
            | Some c ->
              { e with
                  Casting =
                    Some
                      { c with
                          Remaining = c.Remaining + pushbackDelayTicks
                          Pushes = c.Pushes + 1 } }
            | None -> e)
          |> logLine (sprintf "%s's cast is knocked back by damage" t.Name)
        else
          w
      | _ -> w


  // =========================================================================
  // Ability resolution
  // =========================================================================

  /// Effects that put health back on a friendly target: what a "next spell"
  /// cooldown can change, and what a healer's decision is about.
  let internal isHealEffect =
    function
    | Heal _
    | HealOverTime _
    | ConsumeHealOverTime -> true
    | _ -> false

  /// Resolve one ability's effects against one target. The resource is spent
  /// here, on resolution, so an interrupted cast costs nothing.
  let resolveAbility (actorId: EntityId) (targetId: EntityId) (ability: Ability) (w0: World) : World =
    match tryEntity actorId w0 with
    | None -> w0
    | Some actor ->
      let targetName = nameOf targetId w0

      // Nature's Swiftness and Inner Focus are spent by the first *heal* their
      // holder lands, and by nothing else — a Smite must not eat the Priest's
      // guaranteed crit. Inner Focus also pays the spell's cost. (The Paladin's
      // Divine Favor was the third of these and is gone: OctoWoW made it passive.)
      //
      // Free mana is the exception, and the reason it is separate: Clearcasting
      // hangs off a *damage* spell, so the spell that drinks the rebate is the
      // one that has to spend it. No kit row in the tree sets `FreeMana` today,
      // so this widens nothing that already exists — it is the road a borne
      // proc travels.
      let empowered = ability.Effects |> List.exists isHealEffect
      let held = Aura.empower actor.Auras
      // What a held buff is allowed to touch. Instant and the guaranteed crit
      // ride a heal; they ride a *damage* spell only when the buff names it, and
      // `Only` is the field that names one (Nightfall's Shadow Bolt). A buff with
      // `Only = None` therefore cannot be spent by a Smite, which is the whole
      // reason `isHealEffect` guards this gate at all.
      let scoped = Empower.appliesTo ability.Name held
      let named = held.Only |> Option.exists (fun n -> n = ability.Name)
      let waived = scoped && (empowered || named) && (held.Instant || held.FreeMana || held.SureCrit)
      let freeMana = scoped && held.FreeMana && ability.ResourceCost > 0
      let empower = if empowered && scoped then held else Empower.none
      let cost = if freeMana then 0 else ability.ResourceCost

      let w0 =
        mapEntity
          actorId
          (fun e ->
            { e with
                Resource = max 0 (e.Resource - cost)
                // The five-second rule starts when the spell is paid for, so an
                // interrupted cast — which costs nothing — leaves regeneration
                // alone. Instants count: vanilla watches the cast, not its size.
                Auras =
                  let a = Aura.refresh (RecentlyCast Content.fiveSecondRule) e.Auras
                  if waived || freeMana then Aura.spendEmpower a else a })
          w0

      // One weapon strike, so Multi-Shot's second and third targets roll exactly
      // what the first one does. `WeaponDamage` is the single-target case.
      let weaponStrike (multiplierBp: int) (bonus: int) (victim: EntityId) (w: World) =
        let rolled, w =
          match actor.AutoAttack with
          | Some a -> rollRange a.Min a.Max w
          | None -> 0, w

        let swing = rolled + actor.MeleeDamageBonus
        // The tree's share of a weapon strike, on the same principle as the heal
        // share in the heal path: one multiply, and a character with no talents
        // multiplies by 10000.
        let swing = swing * Talents.damageBp ability.Name actor.Talents / 10000
        let critBonus = if ability.Name = "Backstab" then actor.BackstabCritBonusBp else 0
        let amount, w = meleeStrike actor victim (swing * multiplierBp / 10000 + bonus) critBonus (Some ability.Name) w
        let w = dealDamage victim amount w
        // Only the Party builds threat on Mobs.
        let w = if actor.Faction = Party then addThreat victim actor amount w else w
        amount, w

      (w0, ability.Effects)
      ||> List.fold (fun w effect ->
        match effect with
        | Damage(lo, hi) ->
          let rolled, w = rollRange lo hi w
          let rolled = rolled * Talents.damageBp ability.Name actor.Talents / 10000
          let critBonus = if ability.Name = "Backstab" then actor.BackstabCritBonusBp else 0
          let amount, w = meleeStrike actor targetId rolled critBonus (Some ability.Name) w
          let w = dealDamage targetId amount w
          // Only the Party builds threat on Mobs.
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w

          if amount > 0 then
            w
            |> logLine (sprintf "%s's %s hits %s for %d" actor.Name ability.Name targetName amount)
          else
            w

        | WeaponDamage(multiplierBp, bonus) ->
          // The same roll a swing would make, scaled, plus the ability's own
          // bonus. The attack-power part of a swing is weapon damage too, so it
          // takes the multiplier: 150% of a Backstab is 150% of the whole swing,
          // not 150% of the weapon's own dice plus a flat bonus. Without this a
          // Backstab does less per point of energy than Sinister Strike.
          let amount, w = weaponStrike multiplierBp bonus targetId w

          if amount > 0 then
            w
            |> gainRage actor amount
            |> logLine (sprintf "%s's %s hits %s for %d" actor.Name ability.Name targetName amount)
          else
            w

        | MultiShot maxTargets ->
          // Weapon damage per target with no flat bonus, which is what Multi-Shot's
          // tooltip carries. The extra targets are the hostiles nearest the one
          // aimed at: the engine has no cone, and "standing next to the target" is
          // the honest stand-in for "in front of the hunter".
          let aim =
            match tryEntity targetId w with
            | Some t -> t.Pos
            | None -> actor.Pos

          let extra =
            w.Entities
            |> List.filter (fun e ->
              e.Faction <> actor.Faction
              && alive e
              && e.Id <> targetId
              && Pos.chebyshev aim e.Pos <= 8)
            |> List.sortBy (fun e -> Pos.chebyshev aim e.Pos)
            |> List.truncate (max 0 (maxTargets - 1))
            |> List.map (fun e -> e.Id)

          targetId :: extra
          |> List.fold (fun (w: World) victim ->
            let amount, w = weaponStrike 10000 0 victim w

            if amount > 0 then
              w
              |> gainRage actor amount
              |> logLine (sprintf "%s's %s hits %s for %d" actor.Name ability.Name (nameOf victim w) amount)
            else
              w) w

        | SpellDamage(lo, hi) ->
          let rolled, w = rollRange lo hi w
          let rolled = rolled * Talents.damageBp ability.Name actor.Talents / 10000
          let amount, w = spellHit ability.Name actor targetId rolled w

          let amount, crit, w =
            rollCrit
              actor.CritChanceBp
              (actor.Talents.CritDamageBp + slayingAgainst actor targetId w)
              amount
              w

          // A damage spell that landed is the event Clearcasting hangs off; a
          // resisted one is not. Rolled once per landed spell, not once per
          // ability — a wave of Arcane Missiles is five chances, as it was.
          let w = if amount > 0 then procOn OnSpellHit actor.Id w else w
          let w = dealDamage targetId amount w

          // A spell crit is a crit like any other for the named event: the ability
          // name is right here, which is why `OnCritOf` is resolved on both sides
          // of `rollCrit`.
          let w = if crit then procOn (OnCritOf ability.Name) actor.Id w else w
          let w = if crit then piercingBleed actor targetId amount w else w
          let w = gainRage actor amount w
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w
          w |> logLine (sprintf "%s's %s hits %s for %d" actor.Name ability.Name targetName amount)

        | Innervate(duration, bp) ->
          // Innervate multiplies the target's regeneration rather than handing
          // mana over, so it is worth five times as much to a healer who stops
          // casting: see `SimTick.advanceResource`.
          match tryEntity targetId w with
          | Some t when alive t ->
            w
            |> mapEntity targetId (fun e -> { e with Auras = Aura.refresh (Innervated(duration, bp)) e.Auras })
            |> logLine (sprintf "%s's %s multiplies %s's regeneration" actor.Name ability.Name t.Name)
          | _ -> w

        | Heal(lo, hi) ->
          let rolled, w = rollRange lo hi w

          // Healing Way: the stacks the target is already carrying make this
          // Healing Wave worth more, and this cast leaves the next one a stack
          // deeper. The stack lives on the *target*, not the caster, and a
          // character without the talent multiplies by 10000.
          let wayBp =
            if actor.Talents.HealingWayBp <= 0 || ability.Name <> "Healing Wave" then 10000
            else
              let stacks =
                tryEntity targetId w
                |> Option.map (fun t -> Aura.healingWayStacks t.Auras)
                |> Option.defaultValue 0

              10000 + stacks * actor.Talents.HealingWayBp

          // The tree's share of the heal lands on the rolled number, beside the
          // flat bonus healing Spiritual Guidance turns Spirit into. One roll,
          // one multiply, and a character with no talents multiplies by 10000.
          let amount =
            (rolled + actor.BonusHealing)
            * (Talents.healBp ability.Name actor.Talents * wayBp / 10000)
            / 10000

          // A heal crit is worth half again as much as the heal, which is what
          // makes a crit talent a healing talent and not only an Illumination and
          // Inspiration trigger. It is rolled for every heal, because it now moves
          // a number: the per-ability bonus is what Improved Regrowth's +50% turns
          // into a Regrowth that crits more often than anything else in the game.
          let critBp =
            actor.CritChanceBp
            + (ability.Effects
               |> List.sumBy (function
                 | CritBonus bp -> bp
                 | _ -> 0))

          let rolledCrit, w = rollRange 0 9999 w
          let crit = empower.SureCrit || rolledCrit < critBp
          let amount = if crit then amount * 3 / 2 else amount

          let before =
            tryEntity targetId w |> Option.map (fun t -> t.Health) |> Option.defaultValue 0

          let w = healEntity targetId amount w

          let after =
            tryEntity targetId w |> Option.map (fun t -> t.Health) |> Option.defaultValue 0

          let effective = after - before

          let w =
            if actor.Faction = Party then addHealThreat actor effective w else w

          let w =
            if actor.Talents.HealingWayBp <= 0 || ability.Name <> "Healing Wave" then
              w
            else
              w
              |> mapEntity targetId (fun e ->
                { e with
                    Auras =
                      Aura.refresh (HealingWay(secTicks 15.0, min 3 (Aura.healingWayStacks e.Auras + 1))) e.Auras })

          // Inspiration / Ancestral Healing and Illumination are both paid for by
          // the same crit that already enlarged the heal above, so the roll is not
          // repeated here — only what the crit pays for beyond the extra health.
          let w =
            if crit && actor.Talents.CritArmorBp > 0 then
              w
              |> mapEntity targetId (fun e ->
                { e with
                    Auras =
                      Aura.refresh (Inspired(secTicks 15.0, actor.Talents.CritArmorBp)) e.Auras })
              |> logLine (
                sprintf
                  "%s's heal crit braces %s for +%d%% armour"
                  actor.Name
                  targetName
                  (actor.Talents.CritArmorBp / 100)
              )
            else
              w

          // Illumination: a heal that crits hands its mana cost back.
          let w =
            if crit && actor.Talents.ManaRefundOnCritBp > 0 then
              let refund = cost * actor.Talents.ManaRefundOnCritBp / 10000

              w
              |> mapEntity actorId (fun e ->
                { e with Resource = min e.MaxResource (e.Resource + refund) })
              |> logLine (sprintf "%s's heal crits and returns %d mana" actor.Name refund)
            else
              w

          w |> logLine (sprintf "%s's %s heals %s for %d" actor.Name ability.Name targetName effective)

        | ConsumeHealOverTime ->
          match tryEntity targetId w with
          | Some t ->
            match Aura.swiftmend t.Auras with
            | Some(consumed, instant) ->
              // The instant heal is a fixed 12 s of Rejuvenation or 18 s of Regrowth —
              // it does not shrink as the buff runs out — and the buff is eaten whole.
              // Vanilla's Swiftmend, and the reason the AI waits for the last tick.
              let w =
                w
                |> mapEntity targetId (fun e ->
                  { e with
                      Auras =
                        e.Auras
                        |> List.filter (function
                          | Rejuvenation(_, _, s) when s = consumed -> false
                          | _ -> true) })

              let w = healEntity targetId instant w

              let effective =
                (tryEntity targetId w |> Option.map (fun e -> e.Health) |> Option.defaultValue 0)
                - t.Health

              let w =
                if actor.Faction = Party then addHealThreat actor effective w else w

              w
              |> logLine (sprintf "%s consumes the %s on %s for %d" actor.Name consumed t.Name effective)
            | None -> w |> logLine (sprintf "%s finds no heal-over-time on %s" actor.Name t.Name)
          | None -> w

        | Empowers(duration, e) ->
          w
          |> mapEntity actorId (fun a ->
            { a with Auras = Aura.refresh (Empowered(duration, e)) a.Auras })
          |> logLine (sprintf "%s readies %s" actor.Name ability.Name)

        // A per-ability crit chance was folded into the heal (or the strike) that
        // rolled it, so resolving it again would do nothing.
        | CritBonus _ -> w

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

        | Silence duration ->
          match tryEntity targetId w with
          | Some t when alive t ->
            w
            |> mapEntity targetId (fun e ->
              { e with
                  Auras =
                    Silenced duration
                    :: (e.Auras |> List.filter (fun a -> not (Aura.isSilenced a)))
                  Casting = None })
            |> logLine (sprintf "%s silences %s" actor.Name t.Name)
          | _ -> w

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

        | TakeStance form ->
          // One form at a time: the old one is dropped rather than layered. What a
          // form is worth lives in the abilities it unlocks and the casting it
          // forbids, not in a hidden multiplier here — so this case changes the
          // `Shaped` aura and nothing else. `HumanForm` drops it without putting
          // one back, because a body in humanoid shape carries no form aura.
          let auras =
            actor.Auras
            |> List.filter (fun a -> (Aura.stanceOf a).IsNone)
            |> fun rest ->
              match form with
              | HumanForm -> rest
              | _ -> Shaped form :: rest

          w
          |> mapEntity actorId (fun e -> { e with Auras = auras })
          |> logLine (sprintf "%s shifts into %s form" actor.Name (Stances.name form))

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
          let amount, _, w = rollCrit actor.CritChanceBp actor.Talents.CritDamageBp amount w

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
              w |> logLine (sprintf "%s's %s hits %s for %d" actor.Name ability.Name t.Name dealt)
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

        | Dispel(school, count) ->
          // Both directions at once: whatever on the target answers to this
          // school comes off, and the school is what makes the offensive and
          // defensive cases the same code. A purge aimed at a mob finds magic
          // buffs; a Cure Poison aimed at a member finds a poison. Nothing to
          // remove is not a wasted cast in the log — it is the common case in
          // content where the mobs never afflict anyone, and it is silent for
          // exactly that reason.
          match tryEntity targetId w with
          | Some t ->
            let matching, others =
              t.Auras |> List.partition (fun a -> Aura.dispelType a = school)

            let taken = matching |> List.truncate count

            if List.isEmpty taken then
              w
            else
              let left = others @ (matching |> List.skip taken.Length)

              w
              |> mapEntity targetId (fun e -> { e with Auras = left })
              |> logLine (
                sprintf "%s dispels %s from %s" actor.Name (String.concat ", " (taken |> List.map Aura.describe)) t.Name
              )
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
          let amount, w = meleeStrike actor targetId rolled 0 (Some ability.Name) w
          let w = dealDamage targetId amount w
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w

          w
          |> mapEntity actorId (fun e -> { e with ComboPoints = 0 })
          |> logLine (sprintf "%s's %s hits %s for %d" actor.Name ability.Name targetName amount)

        | DamageOverTime(duration, perTick) ->
          // The tree's per-spell damage share is fixed at cast time, not read on every
          // tick. By the time a `Corrupted` fires, its ability name is gone and
          // `perTick` is the only place that multiplier can survive — and a build
          // never respecs inside a run, so baking it here is the same number as
          // multiplying a second later. Improved Curse of Agony and Shadow Mastery
          // are the two that need it. The caster is kept, because a proc can hang
          // off the tick (Nightfall) and the target cannot tell you who applied it.
          // A dot crits on the tick it is applied, not on every tick, so the
          // slaying share is baked and its crit half cannot be: a `Corrupted`
          // carries a number and no attacker. The Hunter's serpent sting is the
          // only slayable dot in the build, and it does not crit.
          let perTick = perTick * Talents.damageBp ability.Name actor.Talents / 10000

          let perTick =
            match tryEntity targetId w with
            | Some t -> perTick * (10000 + slayingBp actor t) / 10000
            | None -> perTick

          match tryEntity targetId w with
          | Some t ->
            w
            |> mapEntity targetId (fun e ->
              { e with Auras = Aura.refresh (Corrupted(duration, perTick, Some actor.Id)) e.Auras })
            |> logLine (sprintf "%s afflicts %s" actor.Name t.Name)
          | None -> w

        | DrainResource(duration, perTick) ->
          // No damage and no interrupt: the sting is paid for by the target's own
          // mana, one second at a time, until it cannot afford the spell it wants.
          match tryEntity targetId w with
          | Some t ->
            w
            |> mapEntity targetId (fun e ->
              { e with Auras = Aura.refresh (Drained(duration, perTick)) e.Auras })
            |> logLine (sprintf "%s's %s drains %s" actor.Name ability.Name t.Name)
          | None -> w

        | Summon spec ->
          // A pet is a real Party entity: it fights, it can be killed, and the
          // existing Dps behaviour drives it without knowing it was summoned.
          // It needs a free tile to appear on, which is why a summon can fail —
          // the same reason a Hunter cannot call a pet into a full corridor.
          //
          // Totems are summoned the same way but obey a slot rule instead: one
          // totem per element, and a new one replaces what stood there. Mana
          // Tide over a standing Mana Spring dismisses the spring rather than
          // stacking a second water totem, so holding the tide for a hard pull
          // costs the Shaman nothing. A pet carries `Totem = None` and is never
          // displaced by this.
          let w =
            match spec.Totem with
            | None -> w
            | Some incoming ->
              let slot = TotemSlot.ofPulse incoming.Pulse

              { w with
                  Entities =
                    w.Entities
                    |> List.filter (fun t ->
                      not (
                        t.Owner = Some actorId
                        && t.Totem
                           |> Option.exists (fun standing -> TotemSlot.ofPulse standing.Pulse = slot)
                      )) }

          // A totem takes up no space, so it is dropped on the Shaman's own tile
          // and cannot fail for want of room. A pet still needs a free tile of its
          // own — that is the case the search below exists for.
          let spot =
            match spec.Totem with
            | Some _ -> Some actor.Pos
            | None ->
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
                Home = None
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

        | Resurrect(healthPct, manaPct) ->
          match tryEntity targetId w with
          | Some t when not (alive t) ->
            w
            |> mapEntity targetId (fun t ->
              { t with
                  Health = max 1 (t.MaxHealth * healthPct / 100)
                  Resource = max t.Resource (t.MaxResource * manaPct / 100) })
            |> logLine (sprintf "%s raises %s from the dead" actor.Name t.Name)
          | _ -> w

        | Charge
        | Intercept ->
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
    // Nothing in the kits picks a corpse on its own: who to bring back is a
    // decision, not a target search (see `PartyAi.decide`).
    | Corpse -> None

  /// An ability a dead member casts at its own corpse — Reincarnation, which
  /// exists precisely to be used once the member playing it has lost.
  let internal castsFromCorpse (e: Entity) (abilityName: string) =
    not (alive e)
    && (e.Abilities
        |> List.exists (fun a -> a.Name = abilityName && Ability.resurrects a))

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

    let held = Aura.empower actor.Auras

    // Instant if the kit says so, or if the caster holds an instant empower that
    // is allowed to touch this ability. Unscoped (`Only = None`) stays heal-only,
    // which is what keeps a Smite from eating a Priest's Inner Focus; a scoped one
    // names its spell, which is how Nightfall makes Shadow Bolt instant.
    let instant =
      held.Instant
      && ((ability.Effects |> List.exists isHealEffect)
          || (held.Only |> Option.exists (fun named -> named = ability.Name)))

    if ability.CastTicks <= ticks 0 || instant then
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
                    // Mind-numbing Poison: a numbed caster's spells take longer.
                    // Applied when the cast starts, not when it ends, so the bar
                    // is the length the spell will really take.
                    Remaining =
                      ability.CastTicks
                      * (10000 + (e.Auras |> List.sumBy Aura.castTimeIncreaseBp))
                      / 10000
                    Pushes = 0 } })
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
      | Some actor when not (canAct actor) && not (castsFromCorpse actor abilityName) -> w
      | Some actor ->
        match actor.Abilities |> List.tryFind (fun a -> a.Name = abilityName) with
        | None -> w |> logLine (sprintf "%s has no ability %s" actor.Name abilityName)
        | Some ability ->
          let onCooldown =
            actor.Cooldowns |> Map.tryFind ability.Name |> Option.defaultValue (ticks 0) > ticks 0

          if not (canAfford actor ability) then
            w |> logLine (sprintf "%s: not enough resource for %s" actor.Name ability.Name)
          elif
            (ability.Effects |> List.exists leapsTo)
            && not (
              match tryEntity targetId w with
              | Some t when t.Id <> actor.Id -> Path.lineOfSight w.Grid actor.Pos t.Pos
              | _ -> true
            )
          then
            // Silent for the same reason as Charge's rule below: the AI re-asks every
            // Tick and a refusal per Tick buries the log.
            w
          elif ability.Effects |> List.contains Charge && SimState.inCombat w then
            // Vanilla's own rule, and the reason Intercept exists: Charge opens a
            // fight, it does not join one. Silent, because the AI re-asks every
            // Tick and a refusal per Tick buries the log.
            w
          elif onCooldown then
            w |> logLine (sprintf "%s: %s on cooldown" actor.Name ability.Name)
          elif isSilenced actor then
            // Silence blocks instant abilities too, which `canCast` alone does not
            // (it is only consulted for a cast with a cast time). No log line: the
            // AI re-asks every Tick and would bury the log in refusals.
            w
          elif ability.Stance |> Option.exists (SimState.inStance actor >> not) then
            // The one place a required form is enforced, so an ability that names
            // one can never be cast out of it: Tree of Life forbids Healing Touch,
            // and the shift is the cost of the heal rather than a formality.
            w |> logLine (sprintf "%s is in the wrong form for %s" actor.Name ability.Name)
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
                (match ability.TargetKind with
                 | Self -> t.Id = actor.Id && (alive t || Ability.resurrects ability)
                 | Corpse -> t.Faction = actor.Faction && not (alive t)
                 | Foe -> alive t && t.Faction <> actor.Faction
                 | Ally -> alive t && t.Faction = actor.Faction)
                && (ability.TargetKind = Self || canReach ability.Range actor t w)

            if not targetOk then
              w
              |> logLine (sprintf "%s: %s invalid or out of range" actor.Name ability.Name)
            else
              startOrResolve actor targetId ability w

  // =========================================================================
  // Rogue poisons
  // =========================================================================

  /// The five poisons. A poison is *not* an `Ability`: it has no cast, no cost and
  /// no cooldown, so it never appears in a kit and the AI never "uses" it. It is
  /// on the blade, and the engine rolls for it on every auto-attack that lands.
  type Poison =
    | InstantPoison
    | DeadlyPoison
    | WoundPoison
    | CripplingPoison
    | MindNumbingPoison

  /// Instant Poison is a one-in-five proc per swing in vanilla. The four debuffing
  /// poisons ride the same rate here: four more numbers would be four more to
  /// defend, and each of those is already bounded by a stack cap or a duration.
  /// shortcut: one proc rate for all five; split it if a poison must feel rarer
  /// than the others.
  let poisonProcBp = 2000

  /// How long a landed poison lasts. Twelve seconds is vanilla's Deadly Poison, and
  /// it is long enough for a Rogue swinging every 1.9s to keep it up — which is the
  /// whole reason a stacking poison is worth applying.
  let private poisonDuration = secTicks 12.0

  let private poisonDeadlyCap = 5
  let private poisonDeadlyPerTick = 4
  let private poisonWoundCap = 5
  /// 15% of healing received removed per stack, so five stacks is -75%.
  let private poisonWoundBp = 1500
  /// A snare, not a root: vanilla Crippling is 30% and that is enough to matter
  /// over eight tiles.
  let private poisonCrippleBp = 3000
  /// Mind-numbing is 30% longer casts, which is what turns a 2.5s heal into one a
  /// kick can reach.
  let private poisonNumbBp = 3000

  /// Instant Poison's damage, scaled off the Rogue's level the way a rank would
  /// be: about 20-44 at 24, 38-80 by 60.
  let private poisonInstantLo level = 8 + level / 2
  let private poisonInstantHi level = 20 + level

  /// Who is holding a poisoned blade: the Rogue, which in this model is the only
  /// party member who runs on Energy.
  let carriesPoison (e: Entity) = e.Faction = Party && e.ResourceKind = Energy

  /// Picks both blades. The ordering is the whole policy: the situation's poison goes
  /// on the main hand, and the off hand takes the best *different* one — a Rogue has
  /// two weapons and two poison slots, and coating both blades with the same vial
  /// throws a slot away. Instant Poison is always near the top because it costs the
  /// target no debuff slot, and Deadly Poison is the fallback that finally gets to
  /// be used when the situation has nothing to say.
  ///
  /// The tail is never empty, so both slots always fill: worst case the main hand is
  /// Instant and the off hand is Deadly.
  let poisonPlan (victim: Entity) (hostiles: Entity list) =
    let isHealer (h: Entity) =
      h.Abilities
      |> List.exists (fun a ->
        a.Effects |> List.exists (function Heal _ | HealOverTime _ -> true | _ -> false))

    let healerNearby =
      hostiles |> List.exists (fun h -> h.Id <> victim.Id && Pos.chebyshev h.Pos victim.Pos <= 8 && isHealer h)

    let isCaster =
      victim.Casting.IsSome
      || victim.Abilities |> List.exists (fun a -> a.Interruptible)

    let running = victim.Health * 4 < victim.MaxHealth

    let preferred =
      [ if isHealer victim || healerNearby then WoundPoison
        if isCaster then MindNumbingPoison
        if running then CripplingPoison
        InstantPoison
        DeadlyPoison ]

    let main = List.head preferred

    let off =
      preferred |> List.filter (fun p -> p <> main) |> List.head

    main, off

  /// Stacks already standing for an aura kind. Keyed on `Aura.kind` so that no
  /// `Aura` case is matched outside the `Aura` module.
  let private stackCount (sample: Aura) (auras: Aura list) =
    auras |> List.filter (fun a -> Aura.kind a = Aura.kind sample) |> List.sumBy Aura.stacksOf

  /// Roll one blade on a landing auto-attack. `poison` is the vial that blade was
  /// coated with — `poisonPlan` decides, this only applies what it decided.
  let poisonHit (attacker: Entity) (poison: Poison) (victimId: EntityId) (w: World) =
    match tryEntity victimId w with
    | None -> w
    | Some victim ->
      let roll, w = rollRange 0 9999 w

      if roll >= poisonProcBp then
        w
      else
        match poison with
        | InstantPoison ->
          // Nature damage: unmitigated, like every spell in this model.
          let amount, w =
            rollRange (poisonInstantLo attacker.Level) (poisonInstantHi attacker.Level) w

          w
          |> dealDamage victimId amount
          |> logLine (sprintf "%s's Instant Poison stings %s for %d" attacker.Name victim.Name amount)

        | DeadlyPoison ->
          let stacks = stackCount (DeadlyPoisoned(ticks 0, 0, 0)) victim.Auras
          let stacks = min poisonDeadlyCap (stacks + 1)

          w
          |> mapEntity victimId (fun e ->
            { e with
                Auras =
                  Aura.refresh (DeadlyPoisoned(poisonDuration, poisonDeadlyPerTick, stacks)) e.Auras })
          |> logLine (sprintf "%s's Deadly Poison stacks to %d on %s" attacker.Name stacks victim.Name)

        | WoundPoison ->
          let stacks = stackCount (Wounded(ticks 0, 0, 0)) victim.Auras
          let stacks = min poisonWoundCap (stacks + 1)

          w
          |> mapEntity victimId (fun e ->
            { e with Auras = Aura.refresh (Wounded(poisonDuration, poisonWoundBp, stacks)) e.Auras })
          |> logLine (sprintf "%s's Wound Poison stacks to %d on %s" attacker.Name stacks victim.Name)

        | CripplingPoison ->
          w
          |> mapEntity victimId (fun e ->
            { e with Auras = Aura.refresh (Crippled(poisonDuration, poisonCrippleBp)) e.Auras })
          |> logLine (sprintf "%s's Crippling Poison slows %s" attacker.Name victim.Name)

        | MindNumbingPoison ->
          w
          |> mapEntity victimId (fun e ->
            { e with Auras = Aura.refresh (Numbed(poisonDuration, poisonNumbBp)) e.Auras })
          |> logLine (sprintf "%s's Mind-numbing Poison clouds %s" attacker.Name victim.Name)
