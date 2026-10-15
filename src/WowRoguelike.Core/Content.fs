namespace WowRoguelike.Core

/// Content is data, not code (Q11a/Q28b): the *mechanics* are the Effect DU in
/// Domain.fs, and everything here is a record. Adding an ability costs a record
/// and no code.
///
/// VERIFY — every number below that is claimed as WoW data, and where it came
/// from. Anything not listed here is our own pacing choice, not a fact.
///
///   Sourced (Classic Era, Warcraft Wiki):
///     Druid's Slumber        sleep 15s, any hostile action wakes the target,
///                            only one target slept at a time  [spell 8040]
///     Healing Touch          heals a friendly target 195-243    [spells 5187, 23381]
///     Serpent Form           10s, +25 physical, cannot cast while shifted [spell 8041]
///     Threat                 aggro switches at +10% threat within melee range
///                            or +30% outside it; damage 1.0x, effective
///                            healing 0.5x, tank stances 5.0x
///     Anacondra's spawn      "always appears in the first cave of the Caverns,
///                            but spawns in one of four locations around the
///                            room"  [Warcraft Wiki, Lady Anacondra]
///     Fanglords              Lady Anacondra, Lord Cobrahn, Lord Pythas and
///                            Lord Serpentis; each uses the Druid of the Fang
///                            kit, not a separate sourced one
///
///   NOT sourced — placeholders, first knobs to turn against real data:
///     Lightning Bolt damage range (spells 9532 / 20295)
///     every cast time and cooldown in this file
///     every health pool, auto-attack range and swing timer
///     Naralex and Mutanus the Devourer: pacing-only stat blocks. Mutanus's
///     real fight is a summoning event with no sourced numbers.
///     Wrath, Moonfire, Smite, Shadow Word: Pain, Charge, Disarm, Execute and
///     Battle Shout: pacing-only numbers and effects, all placeholders.
///
///   Our own invention, deliberately: the five Party classes (Warrior, Rogue,
///   Mage, Priest, Druid), their stat spreads, and every ability in this file.
///   The stat-to-combat mapping (Stamina -> health, Intellect -> resource,
///   Spirit -> regen, Agility -> crit, Strength -> melee) is faithful in shape
///   but not in scale: it is pacing, not copied.
module Content =

  // =========================================================================
  // Grid — Screaming Gully. '#' is wall, '.' is floor.
  // =========================================================================

  let private gullyMap =
    [ "######################"
      "#....................#"
      "#....................#"
      "#....##........##....#"
      "#....##........##....#"
      "#....................#"
      "#....................#"
      "#....................#"
      "#....##........##....#"
      "#....##........##....#"
      "#....................#"
      "#....................#"
      "######################" ]

  /// Lady Anacondra "always appears in the first cave but spawns in one of four
  /// locations around the room" — north/south by east/west. This is real WoW
  /// behaviour, and it is also the seam where generation first touches the
  /// encounter.
  let anacondraSpawns =
    [ { X = 4; Y = 2 }; { X = 17; Y = 2 }; { X = 4; Y = 10 }; { X = 17; Y = 10 } ]

  // =========================================================================
  // Abilities
  // =========================================================================

  let lightningBolt =
    { Name = "Lightning Bolt"
      CastTicks = secTicks 3.0
      CooldownTicks = ticks 0
      ResourceCost = 60
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(28, 42) ] }

  let healingTouch =
    { Name = "Healing Touch"
      CastTicks = secTicks 2.5
      CooldownTicks = secTicks 10.0
      ResourceCost = 100
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(195, 243) ] }

  let serpentForm =
    { Name = "Serpent Form"
      CastTicks = ticks 0
      CooldownTicks = secTicks 15.0
      ResourceCost = 50
      Interruptible = false
      Range = 0
      TargetKind = Self
      Effects = [ Shapeshift(Serpent, secTicks 10.0, 25) ] }

  let druidsSlumber =
    { Name = "Druid's Slumber"
      CastTicks = secTicks 1.5
      CooldownTicks = secTicks 20.0
      ResourceCost = 80
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ Sleep(secTicks 15.0) ] }

  // -- Party abilities ------------------------------------------------------

  let taunt =
    { Name = "Taunt"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 50
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ Taunt ] }

  let heroicStrike =
    { Name = "Heroic Strike"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 100
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Damage(22, 30) ] }

  let lesserHeal =
    { Name = "Lesser Heal"
      CastTicks = secTicks 1.5
      CooldownTicks = ticks 0
      ResourceCost = 105
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(110, 150) ] }

  /// Priest: a strong direct heal.
  let heal =
    { Name = "Heal"
      CastTicks = secTicks 2.5
      CooldownTicks = ticks 0
      ResourceCost = 160
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(220, 300) ] }

  /// Priest: a weak heal-over-time.
  let renew =
    { Name = "Renew"
      CastTicks = ticks 0
      CooldownTicks = secTicks 5.0
      ResourceCost = 80
      Interruptible = false
      Range = 8
      TargetKind = Ally
      Effects = [ HealOverTime(secTicks 15.0, 10) ] }

  /// The Party's only interrupt, and it is melee range — so using it is a
  /// positioning decision, not a cooldown to press.
  let kick =
    { Name = "Kick"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 75
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Interrupt ] }

  let fireball =
    { Name = "Fireball"
      CastTicks = secTicks 2.5
      CooldownTicks = ticks 0
      ResourceCost = 120
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(48, 66) ] }

  let arcaneShot =
    { Name = "Arcane Shot"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 90
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ Damage(26, 34) ] }

  // -- Warrior --------------------------------------------------------------

  /// Sunder Armour: a vulnerability stack, and flat threat to hold aggro.
  let sunderArmor =
    { Name = "Sunder Armor"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 60
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Sunder(secTicks 20.0, 100); Threat(200) ] }

  let shieldBash =
    { Name = "Shield Bash"
      CastTicks = ticks 0
      CooldownTicks = secTicks 12.0
      ResourceCost = 80
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Damage(10, 16); Interrupt ] }

  // -- Rogue ----------------------------------------------------------------

  let backstab =
    { Name = "Backstab"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 70
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Damage(30, 46) ] }

  let gouge =
    { Name = "Gouge"
      CastTicks = ticks 0
      CooldownTicks = secTicks 15.0
      ResourceCost = 60
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Stun(secTicks 4.0) ] }

  let cheapShot =
    { Name = "Cheap Shot"
      CastTicks = ticks 0
      CooldownTicks = secTicks 20.0
      ResourceCost = 80
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Stun(secTicks 2.0) ] }

  // -- Mage ----------------------------------------------------------------

  let frostbolt =
    { Name = "Frostbolt"
      CastTicks = secTicks 2.5
      CooldownTicks = ticks 0
      ResourceCost = 100
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(38, 52) ] }

  let magicMissile =
    { Name = "Magic Missile"
      CastTicks = secTicks 1.5
      CooldownTicks = ticks 0
      ResourceCost = 80
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(24, 34) ] }

  let arcaneExplosion =
    { Name = "Arcane Explosion"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 120
      Interruptible = false
      Range = 0
      TargetKind = Self
      Effects = [ AreaDamage(3, 24, 36) ] }

  /// Blizzard is the same area effect with a larger radius (Q5a).
  let blizzard =
    { Name = "Blizzard"
      CastTicks = secTicks 2.0
      CooldownTicks = secTicks 15.0
      ResourceCost = 150
      Interruptible = true
      Range = 0
      TargetKind = Self
      Effects = [ AreaDamage(5, 30, 44) ] }

  let polymorph =
    { Name = "Polymorph"
      CastTicks = secTicks 1.5
      CooldownTicks = secTicks 30.0
      ResourceCost = 100
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ Polymorph(secTicks 30.0) ] }

  // -- Druid ----------------------------------------------------------------

  let rejuvenation =
    { Name = "Rejuvenation"
      CastTicks = ticks 0
      CooldownTicks = secTicks 3.0
      ResourceCost = 90
      Interruptible = false
      Range = 8
      TargetKind = Ally
      Effects = [ HealOverTime(secTicks 15.0, 14) ] }

  let regrowth =
    { Name = "Regrowth"
      CastTicks = secTicks 2.0
      CooldownTicks = ticks 0
      ResourceCost = 140
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(150, 210) ] }

  let entanglingRoots =
    { Name = "Entangling Roots"
      CastTicks = secTicks 1.5
      CooldownTicks = secTicks 20.0
      ResourceCost = 90
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ Root(secTicks 10.0) ] }

  // -- casters' damage, and the Warrior's cooldowns ---------------------------

  let wrath =
    { Name = "Wrath"
      CastTicks = secTicks 2.0
      CooldownTicks = ticks 0
      ResourceCost = 60
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(40, 55) ] }

  let moonfire =
    { Name = "Moonfire"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 80
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(30, 45); DamageOverTime(secTicks 12.0, 8) ] }

  let smite =
    { Name = "Smite"
      CastTicks = secTicks 2.0
      CooldownTicks = ticks 0
      ResourceCost = 70
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(45, 60) ] }

  let shadowWordPain =
    { Name = "Shadow Word: Pain"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 90
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ DamageOverTime(secTicks 18.0, 12) ] }

  /// Charge leaps to the target, so its range is the charge distance.
  let charge =
    { Name = "Charge"
      CastTicks = ticks 0
      CooldownTicks = secTicks 15.0
      ResourceCost = 0
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ Charge; Threat(300) ] }

  let disarm =
    { Name = "Disarm"
      CastTicks = ticks 0
      CooldownTicks = secTicks 20.0
      ResourceCost = 100
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Disarm(secTicks 8.0); Threat(200) ] }

  /// Execute is a plain heavy hit here; the real one is gated on the target's
  /// health, which is a mechanic this sim does not have yet.
  let execute =
    { Name = "Execute"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 150
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Damage(90, 120) ] }

  // =========================================================================
  // Builders
  // =========================================================================

  /// Parties generate threat at 1.0x; the Tank's stance at 5.0x.
  let private partyThreatBp = 10000
  let private tankStanceBp = 50000
  let private mobThreatBp = 10000

  /// Resource regeneration per Tick. Mobs do not regenerate at all, and that is
  /// the whole reason a fight can now end: a Druid of the Fang can afford four
  /// Healing Touches and then it is out. WoW's casters do regenerate slowly;
  /// this is the knob to turn if fights should become endurance tests.
  let private partyRegen = 2
  let private mobRegen = 0

  let hero
    (id: int)
    (name: string)
    (role: Role)
    (hp: int)
    (resource: int)
    (attack: AutoAttack)
    (threatBp: int)
    (abilities: Ability list)
    (pos: Pos)
    : Entity =
    { Id = EntityId id
      Name = name
      Faction = Party
      Role = Some role
      MaxHealth = hp
      Health = hp
      Pos = pos
      Destination = None
      MoveTicksLeft = ticks 0
      MoveTicksTotal = ticks 0
      MoveTicksPerTile = secTicks 0.4
      Goal = None
      Path = []
      StallTicks = ticks 0
      Auras = []
      Casting = None
      Cooldowns = Map.empty
      Resource = resource
      MaxResource = resource
      ResourceRegenPerTick = partyRegen
      Abilities = abilities
      AutoAttack = Some attack
      SwingTicksLeft = attack.Ticks
      Threat = Map.empty
      Target = None
      ForcedTarget = None
      ThreatMultiplierBp = threatBp
      Stats = Stats.zero
      CritChanceBp = 0
      MeleeDamageBonus = 0
      Armor = 0
      ArmorClass = Cloth
      DodgeChanceBp = 0
      BlockChanceBp = 0
      BlockValue = 0
      PushbackResistanceBp = 0
      BackstabCritBonusBp = 0
      Engaged = true
      CallForHelpBelowHealthPct = None
      HasCalledForHelp = false }

  let mob
    (id: int)
    (name: string)
    (hp: int)
    (resource: int)
    (attack: AutoAttack option)
    (abilities: Ability list)
    (callForHelpBelowPct: int option)
    (pos: Pos)
    : Entity =
    { Id = EntityId id
      Name = name
      Faction = Hostile
      Role = None
      MaxHealth = hp
      Health = hp
      Pos = pos
      Destination = None
      MoveTicksLeft = ticks 0
      MoveTicksTotal = ticks 0
      MoveTicksPerTile = secTicks 0.45
      Goal = None
      Path = []
      StallTicks = ticks 0
      Auras = []
      Casting = None
      Cooldowns = Map.empty
      Resource = resource
      MaxResource = resource
      ResourceRegenPerTick = mobRegen
      Abilities = abilities
      AutoAttack = attack
      SwingTicksLeft = (attack |> Option.map (fun a -> a.Ticks) |> Option.defaultValue (ticks 0))
      Threat = Map.empty
      Target = None
      ForcedTarget = None
      ThreatMultiplierBp = mobThreatBp
      Stats = Stats.zero
      CritChanceBp = 0
      MeleeDamageBonus = 0
      Armor = 0
      ArmorClass = Cloth
      DodgeChanceBp = 0
      BlockChanceBp = 0
      BlockValue = 0
      PushbackResistanceBp = 0
      BackstabCritBonusBp = 0
      Engaged = false
      CallForHelpBelowHealthPct = callForHelpBelowPct
      HasCalledForHelp = false }

  /// Derive combat values from the five stats (Q1a). Faithful in shape, not in
  /// scale — these are pacing placeholders.
  let private derive (armorClass: ArmorClass) (traits: Traits) (s: Stats) =
    {| MaxHealth = s.Stamina * 10
       MaxResource = s.Intellect * 5
       RegenPerTick = s.Spirit / 10
       CritChanceBp = s.Agility * 5 + traits.CritBonusBp
       DodgeChanceBp = s.Agility * 3 + traits.DodgeBonusBp
       Armor = ArmorClass.baseArmor armorClass + s.Agility / 4 + traits.ArmorBonus
       BlockChanceBp = traits.BlockChanceBp
       BlockValue = traits.BlockValue
       MeleeBonus = s.Strength / 5 |}

  /// The party-wide passives, one per class present (Q2a).
  let partyBuffs: PartyBuffKind list = [ ArcaneIntellect; DivineSpirit; MarkOfTheWild; BattleShout ]

  /// Apply the party passives to a class's base stats.
  let private buffed (stats: Stats) =
    (stats, partyBuffs)
    ||> List.fold (fun acc b ->
      match b with
      | ArcaneIntellect -> { acc with Intellect = acc.Intellect * 110 / 100 }
      | DivineSpirit -> { acc with Spirit = acc.Spirit * 110 / 100 }
      | MarkOfTheWild -> Stats.scaleBp 10500 acc
      | BattleShout -> { acc with Strength = acc.Strength * 110 / 100 })

  /// A Party member specified by the five stats rather than raw values. The
  /// passives are applied to the stats before the derived values are computed.
  let heroStats
    (id: int)
    (name: string)
    (role: Role)
    (armorClass: ArmorClass)
    (traits: Traits)
    (stats: Stats)
    (attack: AutoAttack)
    (threatBp: int)
    (abilities: Ability list)
    (pos: Pos)
    : Entity =
    let d = derive armorClass traits (buffed stats)

    { Id = EntityId id
      Name = name
      Faction = Party
      Role = Some role
      MaxHealth = d.MaxHealth
      Health = d.MaxHealth
      Pos = pos
      Destination = None
      MoveTicksLeft = ticks 0
      MoveTicksTotal = ticks 0
      MoveTicksPerTile = secTicks 0.4
      Goal = None
      Path = []
      StallTicks = ticks 0
      Auras = []
      Casting = None
      Cooldowns = Map.empty
      Resource = d.MaxResource
      MaxResource = d.MaxResource
      ResourceRegenPerTick = d.RegenPerTick
      Abilities = abilities
      AutoAttack = Some attack
      SwingTicksLeft = attack.Ticks
      Threat = Map.empty
      Target = None
      ForcedTarget = None
      ThreatMultiplierBp = threatBp
      Stats = stats
      CritChanceBp = d.CritChanceBp
      MeleeDamageBonus = d.MeleeBonus
      Armor = d.Armor
      ArmorClass = armorClass
      DodgeChanceBp = d.DodgeChanceBp
      BlockChanceBp = d.BlockChanceBp
      BlockValue = d.BlockValue
      PushbackResistanceBp = traits.PushbackResistanceBp
      BackstabCritBonusBp = traits.BackstabCritBonusBp
      Engaged = true
      CallForHelpBelowHealthPct = None
      HasCalledForHelp = false }

  // =========================================================================
  // The encounter
  // =========================================================================

  // Skill-tree traits, distinct from the party Buffs: the "assumed" per-class
  // bonuses. Warrior's threat is covered by the Tank stance, not here.
  let private warriorTraits =
    { Traits.none with
        ArmorBonus = 150
        BlockChanceBp = 800
        BlockValue = 30 }

  let private rogueTraits = { Traits.none with BackstabCritBonusBp = 6000 }
  let private casterTraits = { Traits.none with PushbackResistanceBp = 8000 }

  let private warrior =
    heroStats 1 "Warrior" Tank Plate warriorTraits { Strength = 100; Agility = 50; Stamina = 90; Intellect = 60; Spirit = 20 } { Min = 18; Max = 26; Ticks = secTicks 2.0; Range = 1 } tankStanceBp [ taunt; heroicStrike; sunderArmor; shieldBash; charge; disarm; execute ] { X = 1; Y = 5 }

  let private priest =
    heroStats 2 "Priest" Healer Cloth casterTraits { Strength = 20; Agility = 30; Stamina = 64; Intellect = 180; Spirit = 200 } { Min = 6; Max = 10; Ticks = secTicks 3.0; Range = 1 } partyThreatBp [ heal; renew; smite; shadowWordPain ] { X = 1; Y = 6 }

  let private rogue =
    heroStats 3 "Rogue" Dps Leather rogueTraits { Strength = 80; Agility = 120; Stamina = 70; Intellect = 80; Spirit = 20 } { Min = 16; Max = 24; Ticks = secTicks 1.8; Range = 1 } partyThreatBp [ backstab; kick; gouge; cheapShot ] { X = 1; Y = 7 }

  let private mage =
    // The mage's auto-attack is a wand: the same swing, at range.
    heroStats 4 "Mage" Dps Cloth casterTraits { Strength = 20; Agility = 40; Stamina = 60; Intellect = 240; Spirit = 40 } { Min = 4; Max = 8; Ticks = secTicks 3.0; Range = 8 } partyThreatBp [ fireball; frostbolt; magicMissile; arcaneExplosion; blizzard; polymorph ] { X = 2; Y = 6 }

  let private druid =
    heroStats 5 "Druid" Healer Leather casterTraits { Strength = 30; Agility = 40; Stamina = 68; Intellect = 200; Spirit = 180 } { Min = 8; Max = 12; Ticks = secTicks 3.0; Range = 1 } partyThreatBp [ rejuvenation; regrowth; entanglingRoots; wrath; moonfire ] { X = 2; Y = 7 }

  let private druidOfTheFang id pos =
    { mob id "Druid of the Fang" 480 400 (Some { Min = 12; Max = 18; Ticks = secTicks 2.0; Range = 1 }) [ lightningBolt; healingTouch; serpentForm; druidsSlumber ] None pos with
        Armor = 100 }

  /// Raptors close faster than the casting mobs.
  let private raptor id pos =
    { mob id "Raptor" 380 0 (Some { Min = 20; Max = 28; Ticks = secTicks 1.8; Range = 1 }) [] (Some 20) pos with
        MoveTicksPerTile = secTicks 0.3
        Armor = 200 }

  let private ladyAnacondra id pos =
    { mob id "Lady Anacondra" 900 600 (Some { Min = 14; Max = 20; Ticks = secTicks 2.0; Range = 1 }) [ lightningBolt; healingTouch; druidsSlumber ] None pos with
        Armor = 120 }

  /// The four Fanglords share the Druid of the Fang kit — their differences are
  /// pacing-only health and armour until sourced numbers exist.
  let private lordSerpentis id pos =
    { mob id "Lord Serpentis" 1200 600 (Some { Min = 16; Max = 24; Ticks = secTicks 2.0; Range = 1 }) [ lightningBolt; healingTouch; druidsSlumber ] None pos with
        Armor = 150 }

  let private lordCobrahn id pos =
    { mob id "Lord Cobrahn" 1100 600 (Some { Min = 15; Max = 23; Ticks = secTicks 2.0; Range = 1 }) [ lightningBolt; healingTouch; druidsSlumber ] None pos with
        Armor = 140 }

  let private lordPythas id pos =
    { mob id "Lord Pythas" 1000 600 (Some { Min = 14; Max = 22; Ticks = secTicks 2.0; Range = 1 }) [ lightningBolt; healingTouch; druidsSlumber ] None pos with
        Armor = 130 }

  /// Naralex, the sleeping dreamer at the heart of the Caverns. His nightmare
  /// fights back with the Fang kit until a Nightmare mechanic is modelled.
  let private naralex id pos =
    { mob id "Naralex" 1500 800 (Some { Min = 12; Max = 18; Ticks = secTicks 2.0; Range = 1 }) [ lightningBolt; healingTouch; druidsSlumber ] None pos with
        Armor = 100 }

  /// Mutanus the Devourer, the final boss: a heavy melee aberration with no
  /// cast. The real fight is a summoning event with no sourced numbers, so this
  /// is a pacing placeholder.
  let private mutanus id pos =
    { mob id "Mutanus the Devourer" 2200 0 (Some { Min = 24; Max = 34; Ticks = secTicks 1.8; Range = 1 }) [] None pos with
        Armor = 250 }

  /// Build the Screaming Gully encounter. The only randomness at generation is
  /// which of Anacondra's four positions she takes, drawn from the Generation
  /// stream so it cannot perturb combat rolls.
  let gully (seed: uint64) : World =
    let rng0 = Rng.streamsOf seed
    let pick, rng = Rng.drawGeneration 0 (List.length anacondraSpawns - 1) rng0
    let spawn = List.item pick anacondraSpawns

    // Her guards stand with her; the offsets land on floor for all four spawns.
    let guardA = Pos.move 1 0 spawn
    let guardB = Pos.move 2 0 spawn

    let entities =
      // The warrior leads, in front of the party and facing the Fang.
      [ { warrior with Pos = { X = 3; Y = 6 } }
        { rogue with Pos = { X = 2; Y = 6 } }
        { priest with Pos = { X = 1; Y = 6 } }
        { mage with Pos = { X = 1; Y = 5 } }
        { druid with Pos = { X = 1; Y = 7 } }
        druidOfTheFang 6 { X = 7; Y = 6 }
        raptor 7 { X = 9; Y = 3 }
        raptor 9 { X = 9; Y = 9 }
        ladyAnacondra 8 spawn
        druidOfTheFang 10 guardA
        druidOfTheFang 11 guardB ]

    { Tick = ticks 0
      Grid = Grid.ofRows gullyMap
      Entities = entities
      Pending = []
      Rng = rng
      PartyBuffs = partyBuffs
      Log = [] }

  // =========================================================================
  // Encounter templates, and a whole generated Dungeon (Q21)
  // =========================================================================

  /// The fights a generated Dungeon places. The floorplan is generated; these are
  /// authored, because the hand-tuned part of this game is the fight, not where
  /// the walls sit (Q21b).
  let private druidPull =
    { Name = "Druids of the Fang"
      Kind = Trash
      MinWidth = 8
      MinHeight = 7
      Members = [ DruidOfTheFang; DruidOfTheFang; Raptor ] }

  let private raptorNest =
    { Name = "Raptor nest"
      Kind = Trash
      MinWidth = 7
      MinHeight = 7
      Members = [ Raptor; Raptor ] }

  let private anacondraEncounter =
    { Name = "Lady Anacondra"
      Kind = MiniBoss
      MinWidth = 9
      MinHeight = 8
      Members = [ LadyAnacondra; DruidOfTheFang; DruidOfTheFang ] }

  let private serpentisEncounter =
    { Name = "Lord Serpentis"
      Kind = Boss
      MinWidth = 9
      MinHeight = 8
      Members = [ LordSerpentis; DruidOfTheFang; DruidOfTheFang ] }

  let trashTemplates = [ druidPull; raptorNest ]
  let miniBossTemplate = anacondraEncounter
  let bossTemplate = serpentisEncounter

  let templateFor (kind: EncounterKind) =
    match kind with
    | Trash -> trashTemplates
    | MiniBoss -> [ miniBossTemplate ]
    | Boss -> [ bossTemplate ]

  let private fits (room: Room) (template: EncounterTemplate) =
    Room.width room >= template.MinWidth && Room.height room >= template.MinHeight

  let private mobFor (member': EncounterMember) (id: int) (pos: Pos) : Entity =
    match member' with
    | DruidOfTheFang -> druidOfTheFang id pos
    | Raptor -> raptor id pos
    | LadyAnacondra -> ladyAnacondra id pos
    | LordCobrahn -> lordCobrahn id pos
    | LordPythas -> lordPythas id pos
    | LordSerpentis -> lordSerpentis id pos
    | Naralex -> naralex id pos
    | Mutanus -> mutanus id pos

  /// Build a Run's World: a generated floorplan with authored fights placed into
  /// it. Generation happens once, at Run creation, and draws only from the
  /// Generation stream (Q21f), so nothing generated can shift a combat roll.
  ///
  /// Fight placement is by position on the route: the Boss goes in the room
  /// farthest from the entrance by path length, the MiniBoss partway along, and
  /// Trash everywhere in between (Q21e).
  let dungeon (seed: uint64) : World =
    let layout, generation = Dungeon.generate (Rng.stream seed Rng.GenerationStream)

    let occupied = System.Collections.Generic.HashSet<Pos>()
    let mutable nextId = 6

    let takeId () =
      let id = nextId
      nextId <- nextId + 1
      id

    let freeTiles (room: Room) =
      Room.interior room
      |> Seq.filter (fun p -> not (occupied.Contains p))
      |> Seq.toList

    let place (member': EncounterMember) (pos: Pos) =
      occupied.Add pos |> ignore
      mobFor member' (takeId ()) pos

    // The Party enters at the Dungeon's entrance, on free tiles of that room.
    let entranceRoom =
      layout.Rooms |> List.find (fun r -> Room.contains r layout.Entrance)

    let partyTiles = freeTiles entranceRoom |> List.truncate 5

    let party =
      List.map2 (fun (e: Entity) p -> { e with Pos = p }) [ warrior; priest; rogue; mage; druid ] partyTiles

    for p in partyTiles do
      occupied.Add p |> ignore

    let distances = Dungeon.floodFill layout.Grid layout.Entrance

    let distanceOf (r: Room) =
      let c = Room.centre r
      distances.[c.Y * layout.Grid.Width + c.X]

    let route = layout.Rooms |> List.sortBy distanceOf
    let midIndex = max 1 (List.length route / 2)
    let mutable mobs: Entity list = []

    route
    |> List.iteri (fun i room ->
      let kind =
        if room = layout.BossRoom then Boss
        elif i = midIndex then MiniBoss
        else Trash

      let candidates = templateFor kind |> List.filter (fits room)

      // A room too small for its template gets nothing rather than a crash, and
      // the ASCII renderer shows the empty room.
      if not (List.isEmpty candidates) then
        let template =
          if kind = Trash then
            List.item (i % List.length candidates) candidates
          else
            List.head candidates

        let tiles = freeTiles room

        template.Members
        |> List.iteri (fun j member' ->
          match List.tryItem j tiles with
          | Some pos -> mobs <- mobs @ [ place member' pos ]
          | None -> ()))

    { Tick = ticks 0
      Grid = layout.Grid
      Entities = party @ mobs
      Pending = []
      Rng =
        { Generation = generation
          Combat = Rng.stream seed Rng.CombatStream }
      PartyBuffs = partyBuffs
      Log = [] }

  /// The stitched Wailing Caverns: the seven extracted rooms joined into one
  /// continuous dungeon and populated from the lore table. Placement is by the
  /// room each fight belongs to, not by template-fitting a generated floorplan.
  ///
  /// Missing bosses (Lord Cobrahn, Lord Pythas, Naralex, Mutanus) are TODO;
  /// their rooms get guards until they are modelled.
  let wailingCaverns (seed: uint64) : World =
    let grid, _, roomFloors = Maps.stitch ()
    let occupied = System.Collections.Generic.HashSet<Pos>()
    let mutable nextId = 6

    let takeId () =
      let id = nextId
      nextId <- nextId + 1
      id

    let freeTiles room =
      roomFloors.[room] |> List.filter (fun p -> not (occupied.Contains p))

    let place room (mk: int -> Pos -> Entity) =
      match freeTiles room with
      | p :: _ ->
        occupied.Add p |> ignore
        Some(mk (takeId ()) p)
      | [] -> None

    let partyTiles = freeTiles "the-screaming-gully" |> List.truncate 5

    let party =
      List.map2
        (fun (e: Entity) p -> { e with Pos = p })
        [ warrior; priest; rogue; mage; druid ]
        partyTiles

    for p in partyTiles do
      occupied.Add p |> ignore

    let mobs =
      [ // Screaming Gully — the first cave: Lady Anacondra and her guards.
        yield! place "the-screaming-gully" ladyAnacondra |> Option.toList
        yield! place "the-screaming-gully" druidOfTheFang |> Option.toList
        yield! place "the-screaming-gully" druidOfTheFang |> Option.toList
        // Winding Chasm — a connecting passage of deviate wildlife.
        yield! place "the-winding-chasm" raptor |> Option.toList
        yield! place "the-winding-chasm" druidOfTheFang |> Option.toList
        // Pit of Fangs — Lord Serpentis and his cult.
        yield! place "the-pit-of-fangs" lordSerpentis |> Option.toList
        yield! place "the-pit-of-fangs" druidOfTheFang |> Option.toList
        yield! place "the-pit-of-fangs" druidOfTheFang |> Option.toList
        // Crag of the Everliving — Lord Cobrahn and Lord Pythas, the other Fanglords.
        yield! place "crag-of-the-everliving" lordCobrahn |> Option.toList
        yield! place "crag-of-the-everliving" druidOfTheFang |> Option.toList
        yield! place "crag-of-the-everliving-south" lordPythas |> Option.toList
        yield! place "crag-of-the-everliving-south" druidOfTheFang |> Option.toList
        // Dreamer's Rock — Naralex, the dreamer, at the heart.
        yield! place "dreamers-rock" naralex |> Option.toList
        yield! place "dreamers-rock" druidOfTheFang |> Option.toList
        // The deepest chamber — Mutanus the Devourer.
        yield! place "dreamers-rock-east" mutanus |> Option.toList ]

    { Tick = ticks 0
      Grid = grid
      Entities = party @ mobs
      Pending = []
      Rng = Rng.streamsOf seed
      PartyBuffs = partyBuffs
      Log = [] }
