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

    if target.Faction = Party then
      let boon = if target.Auras.IsEmpty then 0 else target.Auras |> List.sumBy Aura.dodgeBonusBp
      target.DodgeChanceBp + boon + max 0 delta * 4
    else
      max 0 (target.DodgeChanceBp + 500 + delta * 10)

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
    let blockBonus = if target.Auras.IsEmpty then 0 else target.Auras |> List.sumBy Aura.blockBonusBp
    let critBonus = if attacker.Auras.IsEmpty then 0 else attacker.Auras |> List.sumBy Aura.critBonusBp

    { Miss = missChanceBp attacker target
      Dodge = dodgeChanceBp attacker target
      Parry = parryChanceBp attacker target
      Glancing = glancingChanceBp attacker target
      Block = target.BlockChanceBp + blockBonus
      Crit = attacker.CritChanceBp + critBonus + levelCritBonusBp attacker target }

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

  /// Hand one proc's result to its holder, and report what it did to the *hit* that
  /// fired it: `10000` for every result that leaves the hit alone, the multiplier for
  /// the one that does not. Reads the entity back out of the world rather than
  /// trusting the caller's copy: a proc fired mid-swing must land on the character
  /// that is standing there now.
  let private applyProc (p: Proc) (id: EntityId) (w: World) : int * World =
    match tryEntity id w with
    | None -> 10000, w
    | Some e ->
      let unchanged w = 10000, w

      match p.Then with
      // Flat, not `gainRage`: that function converts damage into a level-scaled
      // trickle, and a talent that says "generates 5 rage" is five rage.
      | GrantResource n when e.ResourceKind = Rage && n > 0 && e.MaxResource > 0 ->
        unchanged (
          w
          |> mapEntity id (fun x -> { x with Resource = min x.MaxResource (x.Resource + n) })
          |> logLine (sprintf "%s: %s generates %d rage" e.Name p.Talent n)
        )
      | GrantResource _ -> unchanged w
      | EmpowerNext(duration, empower) ->
        unchanged (
          w
          |> mapEntity id (fun x ->
            { x with Auras = Aura.refresh (Empowered(duration, empower)) x.Auras })
          |> logLine (sprintf "%s: %s procs" e.Name p.Talent)
        )
      // The payload is a buff the holder carries, so it lands through the same
      // `Aura.refresh` every castable buff uses: a second proc refreshes the first
      // rather than stacking a second copy. A buff that moves the swing interval
      // also re-arms the timer it landed on — measured, the shot that procced it
      // has already re-set that timer from the pre-proc auras, so without this the
      // first swing after a Swift Aspects proc is the un-hasted one and the talent
      // loses a fifth of its value.
      | Gain aura ->
        unchanged (
          w
          |> mapEntity id (fun x ->
            let auras = Aura.refresh aura x.Auras

            let armed =
              match x.AutoAttack with
              | Some atk -> min x.SwingTicksLeft (AutoAttack.cadence atk auras)
              | None -> x.SwingTicksLeft

            { x with Auras = auras; SwingTicksLeft = armed })
          |> logLine (sprintf "%s: %s procs" e.Name p.Talent)
        )
      // `Map.remove` reads as "never cast", which is ready: the ability's own
      // cooldown entry is the only thing standing between it and its next cast.
      | ResetCooldown ability ->
        unchanged (
          w
          |> mapEntity id (fun x -> { x with Cooldowns = Map.remove ability x.Cooldowns })
          |> logLine (sprintf "%s: %s resets %s" e.Name p.Talent ability)
        )
      // The hit itself, and the only result that is not "unchanged". The mana comes
      // off `BaseResource` because that is the pool the payload prices ("2% of your
      // base mana"), and it is spent rather than gated on: an eruption is not a
      // cast, so a mage on an empty bar still erupts and simply pays what is left.
      | AmplifyHit(damageBp, manaBp) ->
        let paid = min e.Resource (e.BaseResource * manaBp / 10000)

        damageBp,
        (w
         |> mapEntity id (fun x -> { x with Resource = x.Resource - paid })
         |> logLine (
           sprintf
             "%s: %s erupts, +%d%% damage for %d mana"
             e.Name
             p.Talent
             (damageBp - 10000)
             paid
         ))

  /// Fire whatever this character's build hangs off `ev`, and hand back both the
  /// world and what the hit that fired it *gained*: a damage multiplier, `10000` when
  /// no proc fired or whatever fired left the hit alone. Almost every proc is that
  /// second case, which is why `procOn` below throws the multiplier away — but a proc
  /// that makes the current hit bigger (Arcane Instability) cannot ride the world
  /// alone, because `dealDamage` has already been told the number by the time a
  /// `World` comes back.
  let procDamage (ev: ProcEvent) (id: EntityId) (w0: World) : int * World =
    match tryEntity id w0 with
    | None -> 10000, w0
    | Some e ->
      (((10000, w0), e), Talents.procsOf ev e.Talents)
      ||> List.fold (fun ((gain, w), e) p ->
        let cooling =
          (e.Cooldowns |> Map.tryFind p.Talent |> Option.defaultValue (ticks 0)) > ticks 0

        if cooling then
          (gain, w), e
        else
          // A guaranteed proc draws nothing at all: the seed's stream stays
          // where a build carrying no procs left it. Zero is the roll that cannot
          // miss, since a draw of 9999 still fails a hundred-percent chance...
          // which is why the shortcut cannot simply reuse the top of the range.
          let roll, w = if p.ChanceBp >= 10000 then 0, w else rollRange 0 9999 w

          if roll >= p.ChanceBp then
            (gain, w), e
          else
            let procBp, w = applyProc p id w

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

            // Two eruptions on one hit multiply rather than the last one winning:
            // each proc is an independent "this hit is 25% bigger".
            (gain * procBp / 10000, w), (tryEntity id w |> Option.defaultValue e))
      |> fst

  /// The same call for the sites that only care that the procs happened. Called
  /// where the event is already decided — a block of `meleeStrike`, a crit of
  /// `meleeStrike`, a hit of `spellHit` — so no talent needs a branch of its own.
  let procOn (ev: ProcEvent) (id: EntityId) (w: World) : World = procDamage ev id w |> snd

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
    : int * bool * World =
    match tryEntity targetId w with
    | None -> 0, false, w
    | Some t when not (alive t) || amount <= 0 -> amount, false, w
    | Some t ->
      // Before the attack table, so it reads as "3% more damage" and not "3% more
      // of what got through the armour".
      let slaying = slayingBp attacker t
      let amount = amount * (10000 + slaying) / 10000
      let chances = { chanceOf attacker t with Crit = attacker.CritChanceBp + critBonusBp + levelCritBonusBp attacker t }
      let roll, w = rollRange 0 9999 w

      let mitigate raw =
        // Tree of Life multiplies the armour the target *wears*, not the armour it
        // has: `ItemArmor` is the gear share of `Armor` (see the field's note), so
        // +180% is applied to that share alone and a buff such as Inspiration is
        // neither multiplied nor multiplied through. The two shares are equal for
        // every entity that exists today, so in the common case this reads exactly as
        // "+180% armour" while staying right if a buff ever separates them.
        let armor =
          if t.Auras.IsEmpty then
            max 0 t.Armor
          else
            let treeArmor = if SimState.inStance t TreeForm then t.ItemArmor * 180 / 100 else 0

            let baseArmor =
              max 0
                (t.Armor
                 + treeArmor
                 + (t.Auras |> List.sumBy Aura.armorBonus)
                 - (t.Auras |> List.sumBy Aura.armorReduction))

            // Inspiration / Ancestral Healing: a share on top of the armour the
            // target wears, so it is a multiplier and not a flat add. A target with
            // nothing procced multiplies by 10000.
            let pctBp = Aura.armorPctBp t.Auras
            if pctBp = 0 then baseArmor else baseArmor * (10000 + pctBp) / 10000

        raw * (10000 - armorMitigationBp attacker.Level armor) / 10000

      match attackTable chances roll with
      | Missed -> 0, false, logLine (sprintf "%s misses %s" attacker.Name t.Name) w
      | Dodged -> 0, false, logLine (sprintf "%s dodges %s" t.Name attacker.Name) w
      | Parried -> 0, false, logLine (sprintf "%s parries %s" t.Name attacker.Name) w
      | Glanced -> mitigate (amount * glancingMultiplierBp attacker t / 10000), false, w
      | Blocked ->
        // A block is its own event: Shield Specialization pays on the block
        // whether or not the hit got through the shield.
        mitigate (max 0 (amount - t.BlockValue)), false, (w |> procOn OnBlock t.Id)
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

        dealt, true, (w |> piercingBleed attacker targetId dealt)
      | Hit -> mitigate amount, false, w

  /// A hit's log line, with the crit said out loud. Nothing else in the log marks one,
  /// and a talent that raises only crit chance looks like it does nothing without
  /// this: the first measurement of Arcane Instability's crit half read zero crits in
  /// four hundred spells for exactly that reason.
  let hitLine (actor: Entity) (ability: string) (target: string) (amount: int) (crit: bool) (w: World) =
    logLine
      (if crit then
         sprintf "%s's %s critically hits %s for %d" actor.Name ability target amount
       else
         sprintf "%s's %s hits %s for %d" actor.Name ability target amount)
      w

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

  /// The two casts Undertow watches. Matched on the base name so the cheaper
  /// "Healing Wave (Rank n)" rung `Content.shamanDownrank` builds is covered too:
  /// that row is renamed *after* its talents are folded in, and the talent is about
  /// the spell rather than about the rank.
  let private undertowCast (name: string) =
    name.StartsWith("Healing Wave") || name.StartsWith("Lesser Healing Wave")

  let dealDamage (targetId: EntityId) (amount: int) (w: World) =
    match tryEntity targetId w with
    | None -> w
    | Some t ->
      // Shield Wall takes its share before anything lands, so it covers every
      // source: a swing, a spell, a DoT tick.
      let amount =
        if t.Auras.IsEmpty then
          amount
        else
          let red = t.Auras |> List.sumBy Aura.damageReductionBp
          if red = 0 then amount else amount * (10000 - red) / 10000

      // What a creature was before this hit: needed to notice a death, which is
      // the one thing the quest tracker has to hear about. The health it had left is
      // also the only honest measure of the hit.
      let victim, effective =
        if t.Faction = Hostile && alive t then Some t.Name, min amount t.Health else None, 0

      let w = if effective > 0 then ManaBudget.recordDamage effective w else w

      // Taking a hit is paid for in rage, at the same rate as dealing one.
      let rageGain =
        if t.ResourceKind = Rage && amount > 0 && t.MaxResource > 0 then
          max 1 (amount * 5 / (2 * max 1 t.Level))
        else
          0

      let hp = max 0 (t.Health - amount)
      let died = victim.IsSome && hp = 0

      // Only allocate a filtered aura list when a waking aura is actually present.
      let aurasAfterWake =
        if t.Auras.IsEmpty || not (t.Auras |> List.exists Aura.endsOnHostileAction) then
          t.Auras
        else
          t.Auras |> List.filter (fun a -> not (Aura.endsOnHostileAction a))

      // Water Shield: the globe that paid for this hit, and the mana it was holding.
      let resourceAfterGlobe, aurasAfterGlobe, shieldSpent =
        let res0 =
          if rageGain > 0 then min t.MaxResource (t.Resource + rageGain) else t.Resource

        if amount > 0 && hp > 0 && not aurasAfterWake.IsEmpty then
          match Aura.waterShield aurasAfterWake with
          | Some(WaterShield(charges, _, globeMana, _, _)) when charges > 0 ->
            let nextAuras =
              aurasAfterWake
              |> List.choose (function
                | WaterShield(c, mx, gm, bp, mp) when c > 0 ->
                  if c > 1 then Some(WaterShield(c - 1, mx, gm, bp, mp)) else None
                | a -> Some a)

            min t.MaxResource (res0 + globeMana), nextAuras, (charges = 1)
          | _ -> res0, aurasAfterWake, false
        else
          res0, aurasAfterWake, false

      // Pushback: damage pushes an interruptible cast in progress back by half a
      // second instead of cancelling it, at most twice per cast.
      let castingAfter, pushed, w =
        if hp = 0 then
          None, false, w
        elif amount > 0 then
          match t.Casting with
          | Some c when c.Ability.Interruptible && c.Pushes < pushbackMaxPerCast ->
            let roll, w = rollRange 0 9999 w
            let chance = pushbackBaseBp * (10000 - t.PushbackResistanceBp) / 10000

            if roll < chance then
              Some
                { c with
                    Remaining = c.Remaining + pushbackDelayTicks
                    Pushes = c.Pushes + 1 },
              true,
              w
            else
              t.Casting, false, w
          | _ -> t.Casting, false, w
        else
          t.Casting, false, w

      let w =
        mapEntity
          targetId
          (fun _ ->
            { t with
                Health = hp
                Resource = resourceAfterGlobe
                // Being hit pulls.
                Engaged = t.Engaged || (t.Faction = Hostile && hp > 0)
                Auras = aurasAfterGlobe
                Casting = castingAfter
                Goal = if hp = 0 then None else t.Goal
                Path = if hp = 0 then [] else t.Path
                StallTicks = if hp = 0 then ticks 0 else t.StallTicks
                Destination = if hp = 0 then None else t.Destination
                MoveTicksLeft = if hp = 0 then ticks 0 else t.MoveTicksLeft
                MoveTicksTotal = if hp = 0 then ticks 0 else t.MoveTicksTotal })
          w

      let w =
        match victim with
        | Some name when died -> SimState.recordKill name w
        | _ -> w

      let w =
        if shieldSpent then logLine (sprintf "%s's Water Shield is spent" t.Name) w else w

      if pushed then logLine (sprintf "%s's cast is knocked back by damage" t.Name) w else w


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

  /// Grounding Totem: a harmful spell aimed at a party member lands on the
  /// nearest redirecting totem in reach of that member instead, and the totem is
  /// what has to survive it. Read here, at the one place an ability resolves,
  /// rather than as an aura — the totem is the target for the length of one
  /// spell, which is not a state anything else in the engine needs to see.
  ///
  /// Only magic is redirected: `SpellDamage` and `Dot` are the two shapes a
  /// hostile spell arrives in, and a Warrior's axe is neither.
  let redirectTarget (actor: Entity) (targetId: EntityId) (ability: Ability) (w: World) : EntityId =
    let magical =
      ability.Effects
      |> List.exists (function
        | SpellDamage _
        | DamageOverTime _ -> true
        | _ -> false)

    if not magical || actor.Faction = Party then
      targetId
    else
      match tryEntity targetId w with
      | Some target when target.Faction = Party ->
        let mutable bestId = targetId
        let mutable bestDist = System.Int32.MaxValue

        for e in w.Entities do
          if alive e && e.Faction = Party then
            match e.Totem with
            | Some t when t.Pulse = Redirects ->
              let d = Pos.chebyshev e.Pos target.Pos
              if d <= t.Radius && d < bestDist then
                bestDist <- d
                bestId <- e.Id
            | _ -> ()

        bestId
      | _ -> targetId

  /// Tree of Life's aura of life: while a party Druid holds the form, every party
  /// member — the Druid included — heals as though it had `Spirit / 5` more bonus
  /// healing, which is the form's "20% of your spirit" at the rate the rest of
  /// this file already spends Spirit at.
  let treeOfLifeHealing (w: World) =
    let mutable total = 0

    for e in w.Entities do
      if e.Faction = Party && alive e && SimState.inStance e TreeForm then
        total <- total + e.Stats.Spirit / 5

    total

  /// Resolve one ability's effects against one target. The resource is spent
  /// here, on resolution, so an interrupted cast costs nothing.
  let resolveAbility (actorId: EntityId) (targetId0: EntityId) (ability: Ability) (w0: World) : World =
    match tryEntity actorId w0 with
    | None -> w0
    | Some actor ->
      let w0, targetId = w0, redirectTarget actor targetId0 ability w0
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
      // Arcane Rupture raises the cost as well as the hit ("increasing the damage and
      // mana cost of your Arcane Missiles"), so it is read here rather than at the
      // damage site alone: a free-ish buff that also cost more was the whole trade.
      let ruptureBp =
        if ability.Name = "Arcane Missiles" then Aura.ruptureBp actor.Auras else 0

      let listCost =
        if ruptureBp <= 0 then ability.ResourceCost
        else ability.ResourceCost * (10000 + ruptureBp) / 10000

      let cost =
        // Tree of Life's half of its bargain: the form takes the direct heals away
        // and cheapens the heal-over-time ones by a fifth, so a Druid in the tree
        // pays 128 for the Rejuvenation it pays 160 for out of it. Identified by
        // the `HealOverTime` effect rather than by name, so Rejuvenation, Regrowth
        // and any HoT added later are all covered by one rule.
        if freeMana then
          0
        elif
          SimState.inStance actor TreeForm
          && (ability.Effects |> List.exists (function HealOverTime _ -> true | _ -> false))
        then
          listCost * 8000 / 10000
        else
          listCost

      // What the healer actually paid, and what it owes the fight. Charged here, at
      // the one place a spell is paid for, so the reserve sees the same number the pool
      // does. Damage goes in its own bucket because it is the one kind of spend the
      // reserve must not charge for: it is what the gate is allowed to buy with the
      // surplus, so charging it would raise the very bar that licensed it.
      let paid = min cost actor.Resource

      let w0 =
        (mapEntity
          actorId
          (fun e ->
            { e with
                Resource = min e.MaxResource (max 0 (e.Resource - cost))
                // The five-second rule starts when the spell is paid for, so an
                // interrupted cast — which costs nothing — leaves regeneration
                // alone. Instants count: vanilla watches the cast, not its size.
                Auras =
                  let a = Aura.refresh (RecentlyCast Content.fiveSecondRule) e.Auras
                  if waived || freeMana then Aura.spendEmpower a else a })
          w0)
        |> fun w ->
          if actor.Role = Some Healer then
            if Ability.isDamaging ability then
              ManaBudget.recordDamageSpend actorId paid w
            else
              ManaBudget.recordSpend actorId paid w
          else
            w

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
        let amount, crit, w = meleeStrike actor victim (swing * multiplierBp / 10000 + bonus) critBonus (Some ability.Name) w
        let w = dealDamage victim amount w

        // Experimental Ammunition: a share of what just landed lands again as
        // damage no armour stops. Read off the *landed* amount, so a crit shares its
        // crit, and through `spellHit` rather than `meleeStrike`, so the armour the
        // physical half just paid is not charged a second time — the whole
        // difference between this field and a `DamageBp` multiplier. The shot still
        // reports its own damage: the burn is a second line and a second threat tick,
        // exactly as the era's combat log splits them.
        let w =
          let shareBp = Talents.elementShareBp ability.Name actor.Talents

          if shareBp <= 0 || amount <= 0 then
            w
          else
            let share, w = spellHit "Experimental Ammunition" actor victim (amount * shareBp / 10000) w
            let w = dealDamage victim share w

            let w =
              if share > 0 then
                w |> logLine (sprintf "%s's ammunition burns %s for %d" actor.Name (nameOf victim w) share)
              else
                w

            // Only the Party builds threat on Mobs.
            if actor.Faction = Party then addThreat victim actor share w else w

        // Only the Party builds threat on Mobs.
        let w = if actor.Faction = Party then addThreat victim actor amount w else w
        amount, crit, w

      // Vanish's stealth ends the moment its holder lands an attack of its own:
      // attacking out of stealth is the vanilla opener model. One drop before the
      // fold covers every damage effect below, whatever shape it takes.
      let w0 =
        if actor.Faction = Party && Ability.isDamaging ability then
          SimState.breakStealth actorId w0
        else
          w0

      (w0, ability.Effects)
      ||> List.fold (fun w effect ->
        match effect with
        | Damage(lo, hi) ->
          let rolled, w = rollRange lo hi w
          let rolled = rolled * Talents.damageBp ability.Name actor.Talents / 10000
          let critBonus = if ability.Name = "Backstab" then actor.BackstabCritBonusBp else 0
          let amount, crit, w = meleeStrike actor targetId rolled critBonus (Some ability.Name) w
          let w = dealDamage targetId amount w
          // Only the Party builds threat on Mobs.
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w

          if amount > 0 then hitLine actor ability.Name targetName amount crit w else w

        | WeaponDamage(multiplierBp, bonus) ->
          // The same roll a swing would make, scaled, plus the ability's own
          // bonus. The attack-power part of a swing is weapon damage too, so it
          // takes the multiplier: 150% of a Backstab is 150% of the whole swing,
          // not 150% of the weapon's own dice plus a flat bonus. Without this a
          // Backstab does less per point of energy than Sinister Strike.
          let amount, crit, w = weaponStrike multiplierBp bonus targetId w

          if amount > 0 then
            hitLine actor ability.Name targetName amount crit w |> gainRage actor amount
          else
            w

        | MultiStrike maxTargets ->
          // Weapon damage per target with no flat bonus, which is what both rows
          // carry: Multi-Shot's tooltip in the Hunter's table and Swipe's in the
          // feral's, where the damage column is the weapon's own roll and the
          // "three nearby enemies" is the whole of the shape. The extra targets
          // are the hostiles nearest the one aimed at: the engine has no cone, and
          // "standing next to the target" is the honest stand-in for "in front"
          // — of the hunter or of the bear.
          let aim =
            match tryEntity targetId w with
            | Some t -> t.Pos
            | None -> actor.Pos

          let extra =
            // Player cleave (the operator's ruling, m02387): a cleave is every
            // hostile adjacent to the swinging body, not the nearest three to
            // the aim, and never a cone. Name-gated on the party side, so
            // Multi-Shot keeps its three-target cap and its radius. The Warrior
            // has no Cleave row in this content — Swipe is the whole of the
            // rule today.
            if actor.Faction = Party && ability.Name = "Swipe" then
              w.Entities
              |> List.filter (fun e ->
                e.Faction <> actor.Faction
                && alive e
                && e.Id <> targetId
                && Pos.chebyshev actor.Pos e.Pos <= 1)
              |> List.map (fun e -> e.Id)
            else
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
            let amount, crit, w = weaponStrike 10000 0 victim w

            if amount > 0 then
              w
              |> hitLine actor ability.Name (nameOf victim w) amount crit
              |> gainRage actor amount
            else
              w) w

        | SpellDamage(lo, hi, school) ->
          let rolled, w = rollRange lo hi w
          // `damageBp`: one spell's own line plus the tree's tree-wide damage. It
          // used to be a spell-only variant of this, so that Arcane Instability's
          // +3% spell damage stayed off a staff swing; that talent is a proc now and
          // the variant went with it.
          let rolled = rolled * Talents.damageBp ability.Name actor.Talents / 10000

          // Arcane Rupture's share, on the same spell only: the aura names one spell
          // and the talent that grants it grants the ability, not the number.
          let rolled =
            if ability.Name = "Arcane Missiles" then
              rolled * (10000 + Aura.ruptureBp actor.Auras) / 10000
            else
              rolled
          let amount, w = spellHit ability.Name actor targetId rolled w

          let amount, crit, w =
            rollCrit
              (actor.CritChanceBp + Talents.critBp ability.Name actor.Talents)
              (actor.Talents.CritDamageBp + slayingAgainst actor targetId w)
              amount
              w

          // Arcane Instability, and the reason this branch asks for a multiplier
          // back: a proc that makes *this hit* bigger has to be resolved between the
          // hit and the damage, because `dealDamage` takes the number. The event is
          // the school, not the spell — the payload prices the talent on "damaging
          // Arcane spells", so a mage's Frostbolt must not erupt.
          let amplifyBp, w =
            if amount > 0 then
              procDamage (OnSpellSchoolHit school) actor.Id w
            else
              10000, w

          let amount = amount * amplifyBp / 10000

          // A damage spell that landed is the event Clearcasting hangs off; a
          // resisted one is not. Rolled once per landed spell, not once per
          // ability — a wave of Arcane Missiles is five chances, as it was.
          let w = if amount > 0 then procOn OnSpellHit actor.Id w else w

          // The named half of the same event: `OnSpellHitOf` is what a talent that
          // resets one specific ability hangs off (Temporal Convergence on Arcane
          // Missiles), resolved here while the ability name is in hand. A build with
          // no named procs draws nothing, so the stream stays put.
          let w =
            if amount > 0 then procOn (OnSpellHitOf ability.Name) actor.Id w else w

          let w = dealDamage targetId amount w

          // A spell crit is a crit like any other for the named event: the ability
          // name is right here, which is why `OnCritOf` is resolved on both sides
          // of `rollCrit`.
          let w = if crit then procOn (OnCritOf ability.Name) actor.Id w else w
          let w = if crit then piercingBleed actor targetId amount w else w
          let w = gainRage actor amount w
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w
          w |> hitLine actor ability.Name targetName amount crit

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
          // `treeOfLifeHealing` is added to the flat bonus and not to the
          // multiplier: it *is* bonus healing, which is what `BonusHealing` is, so
          // it is the same shape as Spiritual Guidance and lands before the tree's
          // percent exactly as that does.
          let amount =
            (rolled + actor.BonusHealing + treeOfLifeHealing w)
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

          // Undertow: a Healing Wave or Lesser Healing Wave has a chance to hand a
          // spent globe back, and at a full shield to spend one instead. Both halves
          // are one roll, because they are one talent and the inversion *is* the
          // talent: a Shaman sitting on nine globes tops nothing up, so the proc
          // costs it a charge and the cycle keeps turning. The globe taken at full is
          // spent without paying back its mana — that is what makes the inversion a
          // tax rather than a second helping.
          let w =
            if actor.Talents.UndertowBp <= 0 || not (undertowCast ability.Name) then
              w
            else
              let roll, w = rollRange 0 9999 w

              if roll >= actor.Talents.UndertowBp then
                w
              else
                match tryEntity actorId w |> Option.bind (fun a -> Aura.waterShield a.Auras) with
                | Some(WaterShield(charges, maxCharges, _, _, _)) ->
                  let next = if charges >= maxCharges then charges - 1 else charges + 1

                  w
                  |> mapEntity actorId (fun e ->
                    { e with
                        Auras =
                          e.Auras
                          |> List.choose (function
                            | WaterShield(_, mx, gm, bp, mp) ->
                              if next > 0 then Some(WaterShield(next, mx, gm, bp, mp)) else None
                            | a -> Some a) })
                | _ -> w

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

              // Two lines, because the log is two things at once. The first is the only
              // shape `Attribution` reads — `<caster>'s <spell> heals <target> for N`,
              // the same shape a direct heal and a heal-over-time tick are written in
              // — and a Swiftmend written any other way is healing the ledger cannot
              // see: the biggest heal in the kit, invisible to `sim healers` and to
              // `sim attrib`. The second says what was eaten, which is the part a
              // reader of the log wants and no parser can use.
              w
              |> logLine (sprintf "%s consumes the %s on %s for %d" actor.Name consumed t.Name effective)
              |> logLine (sprintf "%s's %s heals %s for %d" actor.Name ability.Name t.Name effective)
            | None -> w |> logLine (sprintf "%s finds no heal-over-time on %s" actor.Name t.Name)
          | None -> w

        | Empowers(duration, e) ->
          w
          |> mapEntity actorId (fun a ->
            { a with Auras = Aura.refresh (Empowered(duration, e)) a.Auras })
          |> logLine (sprintf "%s readies %s" actor.Name ability.Name)

        // Arcane Power: a casting-speed window on the caster itself. Same shape as
        // `Empowers` and deliberately a different aura — a *timed* speed stat rather
        // than a single spell's waiver, and the attack-speed `Hastened` is not it.
        | CastHaste(duration, speedBp) ->
          w
          |> mapEntity actorId (fun a ->
            { a with Auras = Aura.refresh (Casting(duration, speedBp)) a.Auras })
          |> logLine (sprintf "%s's casting quickens" actor.Name)

        // Arcane Rupture: eight seconds in which the caster's own Arcane Missiles hit
        // harder and cost more. One aura, because the payload moves both together.
        | Rupture(duration, damageBp) ->
          w
          |> mapEntity actorId (fun a ->
            { a with Auras = Aura.refresh (Ruptured(duration, damageBp)) a.Auras })
          |> logLine (sprintf "%s ruptures the weave" actor.Name)

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
            |> logLine (sprintf "%s's %s interrupts %s's %s" actor.Name ability.Name t.Name cancelled)
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

        | Vanish duration ->
          // The caster's threat leaves every Hostile's table, and a gaze already
          // on it stops here — `resolveTarget` re-picks from the members it can
          // still see, and the stealth keeps the vanished body out of that pool
          // until its own next attack ends it.
          let w =
            w.Entities
            |> List.filter (fun m -> m.Faction = Hostile)
            |> List.fold (fun acc m ->
              mapEntity
                m.Id
                (fun x ->
                  { x with
                      Threat = x.Threat |> Map.remove actorId
                      Target = if x.Target = Some actorId then None else x.Target })
                acc)
              w

          w
          |> mapEntity
               actorId
               (fun e ->
                 { e with
                     Auras =
                       Stealthed duration
                       :: (e.Auras |> List.filter (fun a -> not (Aura.isStealthed a))) })
          |> logLine (
            if ability.Name = "Stealth" then
              sprintf "%s enters stealth" actor.Name
            else
              sprintf "%s vanishes" actor.Name
          )

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
              m.Faction = Hostile && alive m && canReach ability.Range actor m w)
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

          // Enemy cleave is a 90° cone, not a circle: the three adjacent tiles
          // in the arc the cleaver faces — its target's tile and the two
          // flanking it. Facing is direction-to-target because the grid keeps no
          // facing field, and the 8-connected grid makes "the three front tiles"
          // the target tile plus both half-steps toward it (for a diagonal, the
          // two orthogonal half-steps). This is the whole fix for the graveyard
          // wipe: the melee-stepping healer standing behind the tank's mob was
          // eating every cleave aimed at the tank, ~12 a run. One name test —
          // `cleave` in Content is a single row every cleaving mob shares (Sentry,
          // Torturer, Gallant, Monk, Soldier, Fallen Champion, Loksey,
          // Beastmaster, Guardsman, Myrmidon, Herod, Centurion) — and the
          // faction gate keeps any future player cleave a full circle, because
          // the operator's rule is omnidirectional for players (Whirlwind too:
          // a spin has no front).
          let frontArc =
            if actor.Faction = Hostile && ability.Name = "Cleave" then
              match tryEntity targetId w with
              | Some t ->
                  let dx, dy = sign (t.Pos.X - actor.Pos.X), sign (t.Pos.Y - actor.Pos.Y)
                  let flanks =
                    if dx = 0 then [ -1, dy; 1, dy ]
                    elif dy = 0 then [ dx, -1; dx, 1 ]
                    else [ dx, 0; 0, dy ]

                  Some((dx, dy) :: flanks |> List.map (fun (fx, fy) -> Pos.move fx fy actor.Pos))
              | None -> None // nothing aimed at: no facing, and radius-1 keeps the old circle
            else
              None

          w.Entities
          |> List.filter (fun e ->
            e.Faction <> actor.Faction
            && alive e
            && Pos.chebyshev actor.Pos e.Pos <= radius
            && Path.lineOfSight w.Grid actor.Pos e.Pos
            && (frontArc
                |> Option.map (fun tiles -> List.contains e.Pos tiles)
                |> Option.defaultValue true))
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

        | Cripple(duration, slowBp) ->
          match tryEntity targetId w with
          | Some t when alive t ->
            w
            |> mapEntity targetId (fun e -> { e with Auras = Aura.refresh (Crippled(duration, slowBp)) e.Auras })
            |> logLine (sprintf "%s cripples %s" actor.Name t.Name)
          | _ -> w

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
          // Four multipliers stack on a heal-over-time, and every one of them is
          // read at this single moment, because a `Rejuvenation` aura carries a
          // number and not a recipe (`Rejuvenation(remaining, healPerTick, source)`).
          //
          //  - `healBp`: the tree's own bonus and this spell's line — Improved
          //    Rejuvenation is the one that buys it.
          //  - `periodicBp`: Genesis, which is periodic-only, so it lands here and
          //    on the DoT site and on no direct heal.
          //  - Preservation: Regrowth's tail, and only while the target is
          //    *already* wearing Rejuvenation. That condition is why this branch
          //    resolves its target before it prices the tick, and it is why the
          //    field cannot live on Regrowth's `SpellTalent` row — that row is
          //    folded into the ability once, at construction, when no target
          //    exists yet.
          //  - `treeOfLifeHealing`: the party's `Spirit / 5`, and the only additive
          //    one. Spread across the HoT's own seconds rather than added per
          //    second: per second it would be worth `duration` times what the same
          //    bonus is worth to a direct heal, and Rejuvenation would out-heal
          //    Healing Touch on a bonus meant to be a fifth of a Druid's Spirit.
          let target = tryEntity targetId w

          let preservationBp =
            match target with
            | Some t when
              ability.Name = "Regrowth"
              && actor.Talents.PreservationBp > 0
              && Aura.healOverTimeRemainingFor "Rejuvenation" t.Auras > ticks 0
              ->
              10000 + actor.Talents.PreservationBp
            | _ -> 10000

          let perTick = perTick * Talents.healBp ability.Name actor.Talents / 10000
          let perTick = perTick * Talents.periodicBp actor.Talents / 10000
          let perTick = perTick * preservationBp / 10000

          let perTick =
            perTick + treeOfLifeHealing w / max 1 (duration / secTicks 1.0)

          match target with
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

        | Energize amount ->
          // Enrage's row is literally `Energize 20` (`.pi/research/ranks/druid.md`),
          // and the Rage arrives at once: this model has no rage-over-time aura to
          // spread it over the table's ten seconds. Capped like every other gain, and
          // it lands on the *actor*, not the target, so an ability with a foe target
          // would still pay the caster.
          let w =
            mapEntity
              actorId
              (fun e ->
                if e.ResourceKind = Rage && e.MaxResource > 0 then
                  { e with Resource = min e.MaxResource (e.Resource + amount) }
                else
                  e)
              w

          w |> logLine (sprintf "%s: %s grants %d rage" actor.Name ability.Name amount)

        | RaiseShield(charges, globeMana, regenPerGlobeBp, mp5) ->
          // The globes go up full, and there is nothing to schedule afterwards: no
          // cooldown, no duration, no refresh. The shield comes off when the last
          // charge is spent (`spendWaterShieldGlobe`) and the AI puts it back the
          // next time it is short — `PartyAi.shieldOrders`. `refresh` keeps a stale
          // half-spent aura from sitting beside the fresh one.
          w
          |> mapEntity
               actorId
               (fun e ->
                 { e with
                     Auras =
                       Aura.refresh (WaterShield(charges, charges, globeMana, regenPerGlobeBp, mp5)) e.Auras })
          |> logLine (sprintf "%s's %s raises %d globes" actor.Name ability.Name charges)

        | Lacerate(duration, perTick, maxStacks) ->
          // The stack count is read off the target's auras, so the fifth press is
          // worth five times the first and the sixth only refreshes it. `Aura.replaces`
          // makes a new Lacerate replace the old one, so `refresh` cannot leave two
          // half-stacked bleeds behind.
          let standing =
            (tryEntity targetId w |> Option.map (fun t -> Aura.lacerateStacks t.Auras) |> Option.defaultValue 0)

          let stacks = min maxStacks (standing + 1)

          let w =
            w
            |> mapEntity targetId (fun e ->
              { e with Auras = Aura.refresh (Lacerated(duration, perTick, stacks)) e.Auras })
            |> logLine (sprintf "%s: %s lacerates %s, %d" actor.Name ability.Name targetName stacks)

          w

        | Bleed(duration, perTick) ->
          // A bleed is `Bleeding`, the aura Piercing Shots already uses, so the
          // ticks are paid by `SimTick.advanceAuraDamage` through `dealDamage` and
          // never through `addThreat`: threat is charged where a spell is cast. A
          // re-application stacks rather than refreshes, because `Aura.replaces`
          // keeps every `Bleeding` distinct for Piercing Shots' sake — a cat that
          // presses Rake twice holds two bleeds, which is the price of sharing the
          // aura with a talent that has to stack.
          w
          |> mapEntity targetId (fun e ->
            { e with
                Auras = Aura.refresh (Bleeding(duration, perTick, Some actor.Id)) e.Auras })
          |> logLine (sprintf "%s: %s bleeds %s, %d a tick" actor.Name ability.Name targetName perTick)

        | BleedFinisher(duration, total, perPoint) ->
          // The combo points are read off the actor as it stood when the ability
          // was resolved, then spent — a finisher is a decision. The per-tick share
          // is the table's total for that point count divided by the duration, in
          // whole points, so a 12-second Rip at one point (42) ticks 3 and at five
          // (138) ticks 11.
          let points = actor.ComboPoints
          let total = total + perPoint * max 0 (points - 1)
          let ticksLeft = int duration / int (secTicks 1.0)
          let perTick = total / max 1 ticksLeft

          let w =
            w
            |> mapEntity targetId (fun e ->
              { e with
                  Auras = Aura.refresh (Bleeding(duration, perTick, Some actor.Id)) e.Auras })
            |> mapEntity actorId (fun e -> { e with ComboPoints = 0 })

          w
          |> logLine (
            sprintf "%s: %s bleeds %s, %d over %d ticks from %d points" actor.Name ability.Name targetName total ticksLeft points
          )

        | Fortified(duration, healthBp) ->
          // Last Stand's shape: the maximum rises and the health with it, so the
          // buff is worth a bar rather than a heal — and `SimTick.advanceAuras`
          // takes the same points back off the maximum when the aura lapses.
          let w =
            w
            |> mapEntity actorId (fun e ->
              // The grant is computed off the *unbuffed* maximum: a second Berserk
              // while the first still stands would otherwise raise a bar that is
              // already raised and hand back only the last grant when it lapses.
              // Unreachable in play — twenty seconds of aura against a three-minute
              // cooldown — but the aura is refreshed rather than stacked, so the
              // arithmetic has to be the one that survives a refresh.
              let held =
                e.Auras
                |> List.tryPick (function
                  | Invigorated(_, _, n) -> Some n
                  | _ -> None)
                |> Option.defaultValue 0

              let granted = (e.MaxHealth - held) * healthBp / 10000

              { e with
                  MaxHealth = e.MaxHealth - held + granted
                  Health = e.Health - held + granted
                  Auras = Aura.refresh (Invigorated(duration, healthBp, granted)) e.Auras })

          w |> logLine (sprintf "%s: %s raises %s's maximum health" actor.Name ability.Name actor.Name)

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
          // A tree that names the finisher raises it, exactly as it raises a
          // weapon strike: Ferocious Bite's Feral Aggression is 15% on this row and
          // nothing else, and the Rogue's Eviscerate reads 10000 because its own
          // build has no such line.
          let rolled = rolled * Talents.damageBp ability.Name actor.Talents / 10000
          let amount, crit, w = meleeStrike actor targetId rolled 0 (Some ability.Name) w
          let w = dealDamage targetId amount w
          let w = if actor.Faction = Party then addThreat targetId actor amount w else w

          w
          |> mapEntity actorId (fun e -> { e with ComboPoints = 0 })
          |> hitLine actor ability.Name targetName amount crit

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
          // Genesis: periodic damage is the other half of the talent's wording, and
          // a Restoration Druid's Moonfire and Entangling Roots are the only damage
          // it lands. Applied beside the tree's damage share rather than folded into
          // it, so a build can have one and not the other.
          let perTick = perTick * Talents.periodicBp actor.Talents / 10000

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
                (fun p -> Grid.isFloor w.Grid p && not (occupiedByOther w actorId p))
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

            // Endurance Training, Thick Hide, Unleashed Fury and Coordinated
            // Assault all scale the same body, so they are read here and nowhere
            // else: a pet has no abilities to hang a `Spells` row on, which is why
            // these four are fields rather than spell talents. A totem is not a
            // pet — it has no talents behind it — so only a summon with
            // `Totem = None` reads them.
            let pet = spec.Totem.IsNone
            let t = actor.Talents
            let scaleBp (bp: int) (v: int) = if pet then v * (10000 + bp) / 10000 else v
            let petHealth = scaleBp t.HealthBp spec.Health
            let petArmor = scaleBp t.PetArmorBp spec.Armor
            // Coordinated Assault is `20% of your attack power` on an extra strike,
            // at most once every 3 s. The pet has no ability list to hang a timer
            // on, so the extra strike is smoothed into its every swing at the
            // cadence ratio `swing / 3 s` — the same shape the DoT and proc models
            // use. It reads ranged attack power, not `MeleeDamageBonus`: a hunter's
            // attack power is his Agility, and his melee bonus is zero because he
            // never swings at range 1.
            let petBonus =
              if not pet then
                0
              else
                let swingTicks =
                  spec.Swing
                  |> Option.map (fun (_, _, sw) -> int sw)
                  |> Option.defaultValue (int (secTicks 2.0))

                actor.RangedAttackPower
                * t.PetApBp
                / 10000
                * swingTicks
                / int (secTicks 3.0)

            let summon =
              { Id = id
                Name = spec.Name
                Level = actor.Level
                CreatureType = spec.CreatureType
                OffHand = false
                Faction = Party
                Role = Some Dps
                Dormant = false
                MaxHealth = petHealth
                Health = petHealth
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
                // A pet holds no mana and no talent prices a share of it.
                BaseResource = 0
                ResourceRegenPerTick = 0
                CombatRegenBp = 0
                ResourceKind = Mana
                ComboPoints = 0
                Abilities = []
                AutoAttack =
                  spec.Swing
                  |> Option.map (fun (lo, hi, sw) ->
                    { Min = scaleBp t.PetDamageBp lo
                      Max = scaleBp t.PetDamageBp hi
                      Ticks = sw
                      Range = 1 })
                SwingTicksLeft = ticks 0
                Threat = Map.empty
                Target = None
                ForcedTarget = None
                ThreatMultiplierBp = 10000
                Stats = Stats.zero
                CritChanceBp = 0
                MeleeDamageBonus = petBonus
                RangedAttackPower = 0
                Armor = petArmor
                // A pet has no gear list, so its whole body armour is its item
                // armour; nothing multiplies it but Thick Hide, above.
                ItemArmor = petArmor
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
            |> (fun w ->
              // A totem drop is the one thing about a summon the ledger wants: its
              // pulses are silent, so this count — not the log the drop also
              // writes — is the record of the set the run kept standing.
              let w =
                if spec.Totem.IsSome then
                  { w with TotemLedger = TotemLedger.drop spec.Name w.TotemLedger }
                else
                  w

              { w with Entities = summon :: w.Entities })
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
            // `occupiedByOther`, not `claimedByOther`: the Charge writes `Pos`
            // outright, so a tile somebody is stepping through is not free ground.
            let spot =
              Path.neighbours
                (fun p -> Grid.isFloor w.Grid p && not (occupiedByOther w actorId p))
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
    let isParty = actor.Faction = Party
    let mutable bestId: EntityId option = None
    let mutable bestPct = System.Int32.MaxValue

    for e in w.Entities do
      let inPool =
        if isParty then e.Faction = Party && alive e && e.Totem.IsNone
        else e.Faction = Hostile && alive e && not e.Dormant

      if inPool && e.Health < e.MaxHealth && canReach ability.Range actor e w then
        let pct = healthPct e
        if pct < bestPct then
          bestPct <- pct
          bestId <- Some e.Id

    bestId

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

  /// How long one cast of `ability` takes for a caster holding `auras`. Two stats
  /// meet here and they are not the same arithmetic: a numbed caster takes `+bp%`
  /// longer, while a hasted one takes `10000/(10000+bp)` of the time, because
  /// "30% faster" means two thirds and not 130%. Composed as time: a 3 s Frostbolt
  /// numbed by 50% is 4.5 s, and the same one hasted by 30% of *that* is 3.46 s.
  let castClock (ability: Ability) (auras: Aura list) : int<tick> =
    let numbed =
      ability.CastTicks
      * (10000 + (auras |> List.sumBy Aura.castTimeIncreaseBp))
      / 10000

    let speedBp = auras |> List.sumBy Aura.castSpeedBp

    if speedBp > 0 then
      numbed * 10000 / (10000 + speedBp)
    else
      numbed

  let internal startOrResolve (actor: Entity) (targetId: EntityId) (ability: Ability) (w: World) : World =
    // Using an ability interrupts a move in progress: the character stops to
    // strike or cast rather than walking on through the order.
    let w =
      mapEntity
        actor.Id
        (fun e ->
          { e with
              // Tree of Life's house rule: Swiftmend has no cooldown in the form, so
              // the emergency button is available as often as the Druid can afford
              // it. Identified by the `ConsumeHealOverTime` effect rather than by
              // name, so this reads as "the spell that eats a heal-over-time is never
              // on cooldown in the tree". The cooldown exists to stop the Druid from
              // spending every heal-over-time it lays down; in the tree those are
              // already a fifth cheaper, and the form has given up its direct heals.
              // A cooldown is not always one spell's own: the shocks share one 6 s
              // timer in WoW (`Content.shockCooldownGroup`), so casting Earth Shock
              // has to put Flame Shock on cooldown too. Writing every name in the
              // group keeps `Entity.Cooldowns` a plain name → ready-tick map, which
              // is what `PartyAi.ready` and the affordability refusal already read;
              // the alternative — a group field on `Ability` — would be a second
              // place for "is this ready" to be answered, and it would have to be
              // threaded through every construction site in `Content`.
              Cooldowns =
                if
                  SimState.inStance actor TreeForm
                  && (ability.Effects |> List.exists (function ConsumeHealOverTime -> true | _ -> false))
                then
                  e.Cooldowns
                else
                  let shares =
                    if List.contains ability.Name Content.shockCooldownGroup then
                      Content.shockCooldownGroup
                    else
                      [ ability.Name ]

                  (e.Cooldowns, shares)
                  ||> List.fold (fun cds name -> cds |> Map.add name ability.CooldownTicks)
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
                    // Mind-numbing Poison lengthens a cast and Arcane Power
                    // shortens it; `castClock` is the one place those two meet and
                    // it runs when the cast starts, so the bar a viewer sees is the
                    // length the spell will really take.
                    Remaining = castClock ability e.Auras
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
          elif ability.Name = "Stealth" && SimState.inCombat w then
            // Out-of-combat stealth only; Vanish is the in-combat equivalent.
            w
          elif onCooldown then
            w |> logLine (sprintf "%s: %s on cooldown" actor.Name ability.Name)
          elif isSilenced actor then
            // Silence blocks instant abilities too, which `canCast` alone does not
            // (it is only consulted for a cast with a cast time). No log line: the
            // AI re-asks every Tick and would bury the log in refusals.
            w
          elif SimState.inStance actor TreeForm && Ability.isDamaging ability then
            // Tree of Life: "you cannot cast damaging spells". The operator's house
            // rule lets Healing Touch through on Nature's Swiftness and says nothing
            // about the damage half, so the damage ban stays absolute. Read off
            // `Ability.isDamaging` rather than a name list, so a spell added to the
            // Druid's kit is covered without a second edit. Logged rather than silent
            // because a healer's AI never asks for these: a line here is a real
            // mistake, not per-Tick noise.
            w |> logLine (sprintf "%s cannot cast %s in Tree of Life" actor.Name ability.Name)
          elif
            ability.Stance |> Option.exists (SimState.inStance actor >> not)
            // The one place a required form is enforced, so an ability that names
            // one can never be cast out of it: Tree of Life forbids Healing Touch,
            // and the shift is the cost of the heal rather than a formality.
            //
            // The operator's house rule is the exception: on Nature's Swiftness a
            // Healing Touch is castable from the tree, because an instant heal is the
            // one thing the form's own heal-over-times cannot do and it is worth the
            // shift back. Gated on the buff being `Instant` rather than on the
            // ability's name, so the rule reads as "the buff that skips the cast pays
            // for the shift" and would cover any instant heal the class gains later.
            && not (
              (Aura.empower actor.Auras).Instant
              && Empower.appliesTo ability.Name (Aura.empower actor.Auras)
            )
          then
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
