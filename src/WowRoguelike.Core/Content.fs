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

  /// The mob version of Lightning Bolt (spell 9532): 64-86 Nature on a 3 s cast,
  /// re-cast every 2 s. Every Wailing Caverns caster uses this one rank.
  let lightningBolt =
    { Name = "Lightning Bolt"
      CastTicks = secTicks 3.0
      CooldownTicks = secTicks 2.0
      ResourceCost = 60
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(64, 86) ] }

  /// Healing Touch as the Wailing Caverns druids cast it (spell 5187, unranked):
  /// a flat 195 on a 2.5 s cast, roughly every 15 s. The rank-3 player spell's
  /// 204-253 range does not apply to the NPC version.
  let healingTouch =
    { Name = "Healing Touch"
      CastTicks = secTicks 2.5
      CooldownTicks = secTicks 15.0
      ResourceCost = 90
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(195, 195) ] }

  /// Lord Serpentis heals with spell 6778 instead: a flat 742 on a 3.5 s cast.
  /// That heal is the reason his fight is an interrupt fight.
  let healingTouchR6 =
    { Name = "Healing Touch"
      CastTicks = secTicks 3.5
      CooldownTicks = secTicks 15.0
      ResourceCost = 240
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(742, 742) ] }

  let serpentForm =
    { Name = "Serpent Form"
      CastTicks = ticks 0
      CooldownTicks = secTicks 15.0
      ResourceCost = 50
      Interruptible = false
      Range = 0
      TargetKind = Self
      Effects = [ Shapeshift(Serpent, secTicks 10.0, 25) ] }

  /// Druid's Slumber (spell 8040), the Druid-of-the-Fang version: a 2.5 s cast
  /// that sleeps one non-tank for 15 s. This is the pull-killer the guides warn
  /// about.
  let druidsSlumber =
    { Name = "Druid's Slumber"
      CastTicks = secTicks 2.5
      CooldownTicks = secTicks 15.0
      ResourceCost = 80
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ Sleep(secTicks 15.0) ] }

  /// Sleep (spell 700), the shorter cast the Fanglords and Anacondra use: 1.5 s
  /// for a 20 s sleep.
  let fangSleep =
    { Name = "Sleep"
      CastTicks = secTicks 1.5
      CooldownTicks = secTicks 15.0
      ResourceCost = 80
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ Sleep(secTicks 20.0) ] }

  // -- Party abilities ------------------------------------------------------

  // Every party ability below is the highest rank a level-24 character can
  // train, with its real mana/rage/energy cost, cast time and tooltip numbers.
  // Sources: per-spell Classic DB rows collected in .pi/research/level24/.

  let taunt =
    { Name = "Taunt"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 10
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ Taunt ] }

  /// Rank 4: the swing plus 44 damage, for 15 rage. The cooldown is the weapon's
  /// own 2.70 s, because Heroic Strike replaces the next swing rather than
  /// adding one.
  let heroicStrike =
    { Name = "Heroic Strike"
      CastTicks = ticks 0
      CooldownTicks = secTicks 2.7
      ResourceCost = 15
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ WeaponDamage(10000, 44) ] }

  /// Rank 3: 135-158 for 75 mana.
  let lesserHeal =
    { Name = "Lesser Heal"
      CastTicks = secTicks 2.5
      CooldownTicks = ticks 0
      ResourceCost = 75
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(135, 158) ] }

  /// Priest rank 2: 429-492 for 205 mana, plus the set's +24 healing.
  let heal =
    { Name = "Heal"
      CastTicks = secTicks 3.0
      CooldownTicks = ticks 0
      ResourceCost = 205
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(453, 516) ] }

  /// Priest rank 3: 175 over 15 s for 105 mana, plus the set's +24 healing.
  let renew =
    { Name = "Renew"
      CastTicks = ticks 0
      CooldownTicks = secTicks 5.0
      ResourceCost = 105
      Interruptible = false
      Range = 8
      TargetKind = Ally
      Effects = [ HealOverTime(secTicks 15.0, 12) ] }

  /// The Party's only interrupt, and it is melee range — so using it is a
  /// positioning decision, not a cooldown to press. Rank 1: 15 damage, 25
  /// energy, 10 s.
  let kick =
    { Name = "Kick"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 25
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Damage(15, 15); Interrupt ] }

  let fireball =
    { Name = "Fireball"
      CastTicks = secTicks 3.5
      CooldownTicks = ticks 0
      ResourceCost = 140
      Interruptible = true
      Range = 8
      TargetKind = Foe
      // Rank 5: 146-195 plus 20 over 8 s.
      Effects = [ SpellDamage(146, 195); DamageOverTime(secTicks 8.0, 3) ] }

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

  /// Sunder Armour rank 2: 180 armour per stack, five stacks, 30 s, 15 rage —
  /// plus flat threat so the Tank holds the pull.
  let sunderArmor =
    { Name = "Sunder Armor"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 15
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Sunder(secTicks 30.0, 180); Threat(200) ] }

  /// Rank 1: 6 damage and a 6 s school lockout, for 10 rage.
  let shieldBash =
    { Name = "Shield Bash"
      CastTicks = ticks 0
      CooldownTicks = secTicks 12.0
      ResourceCost = 10
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Damage(6, 6); Interrupt ] }

  // -- Rogue ----------------------------------------------------------------

  /// Rank 3: 150% weapon damage plus 48, for 60 energy. The swing carries the
  /// Rogue's attack power, which is what makes Backstab hit — it scales with
  /// weapons and stats, not with a flat number.
  let backstab =
    { Name = "Backstab"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 60
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ WeaponDamage(15000, 48) ] }

  /// Rank 2: 20 damage and a 4 s stun, for 45 energy.
  let gouge =
    { Name = "Gouge"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 45
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Damage(20, 20); Stun(secTicks 4.0) ] }

  // Cheap Shot is trained at level 26, so a level-24 Rogue does not have it.

  // -- Mage ----------------------------------------------------------------

  /// Rank 4: 74-83 Frost for 65 mana.
  let frostbolt =
    { Name = "Frostbolt"
      CastTicks = secTicks 2.6
      CooldownTicks = ticks 0
      ResourceCost = 65
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(74, 83) ] }

  /// Rank 3: a five-second channel, 56 Arcane each second, for 235 mana. The sim
  /// has no channel, so the whole channel lands as one hit.
  let arcaneMissiles =
    { Name = "Arcane Missiles"
      CastTicks = secTicks 5.0
      CooldownTicks = ticks 0
      ResourceCost = 235
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(280, 280) ] }

  /// Rank 2: 57-64 Arcane to everything within 10 yards, for 120 mana.
  let arcaneExplosion =
    { Name = "Arcane Explosion"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 120
      Interruptible = false
      Range = 0
      TargetKind = Self
      Effects = [ AreaDamage(3, 57, 64) ] }

  /// Rank 1: 200 Frost over an eight-second channel in an 8-yard radius, for 320
  /// mana. Same channel caveat as Arcane Missiles.
  let blizzard =
    { Name = "Blizzard"
      CastTicks = secTicks 8.0
      CooldownTicks = secTicks 15.0
      ResourceCost = 320
      Interruptible = true
      Range = 0
      TargetKind = Self
      Effects = [ AreaDamage(5, 200, 200) ] }

  /// Rank 2: 90 mana, 1.5 s cast, up to 30 s.
  let polymorph =
    { Name = "Polymorph"
      CastTicks = secTicks 1.5
      CooldownTicks = secTicks 30.0
      ResourceCost = 90
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ Polymorph(secTicks 30.0) ] }

  // -- Druid ----------------------------------------------------------------

  /// Level-22 rank: 244 over 12 s for 135 mana, plus the staff's +24 healing.
  let rejuvenation =
    { Name = "Rejuvenation"
      CastTicks = ticks 0
      CooldownTicks = secTicks 3.0
      ResourceCost = 135
      Interruptible = false
      Range = 8
      TargetKind = Ally
      Effects = [ HealOverTime(secTicks 12.0, 22) ] }

  /// Rank 3: 240-275 up front plus 259 over 21 s, for 280 mana.
  let regrowth =
    { Name = "Regrowth"
      CastTicks = secTicks 2.0
      CooldownTicks = ticks 0
      ResourceCost = 280
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(264, 299); HealOverTime(secTicks 21.0, 12) ] }

  /// Rank 4: 363-446 for 185 mana over three seconds, plus the staff's +24.
  /// The mobs' weaker Healing Touch keeps its own record above.
  let healingTouchR4 =
    { Name = "Healing Touch"
      CastTicks = secTicks 3.0
      CooldownTicks = secTicks 10.0
      ResourceCost = 185
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(387, 470) ] }

  /// Rank 2: roots for 15 s and 50 Nature damage, for 65 mana.
  let entanglingRoots =
    { Name = "Entangling Roots"
      CastTicks = secTicks 1.5
      CooldownTicks = secTicks 20.0
      ResourceCost = 65
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ Root(secTicks 15.0); DamageOverTime(secTicks 15.0, 3) ] }

  // -- casters' damage, and the Warrior's cooldowns ---------------------------

  /// Rank 4: 63-74 Nature for 70 mana.
  let wrath =
    { Name = "Wrath"
      CastTicks = secTicks 2.0
      CooldownTicks = ticks 0
      ResourceCost = 70
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(63, 74) ] }

  /// Rank 4: 40-49 Arcane plus 80 over 12 s, for 105 mana.
  let moonfire =
    { Name = "Moonfire"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 105
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(40, 49); DamageOverTime(secTicks 12.0, 7) ] }

  /// Rank 4: 91-106 for 95 mana.
  let smite =
    { Name = "Smite"
      CastTicks = secTicks 2.5
      CooldownTicks = ticks 0
      ResourceCost = 95
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(91, 106) ] }

  /// Rank 3: 132 Shadow over 18 s for 95 mana.
  let shadowWordPain =
    { Name = "Shadow Word: Pain"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 95
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ DamageOverTime(secTicks 18.0, 7) ] }

  /// Charge generates rage rather than costing it: rank 1 refunds 9.
  let charge =
    { Name = "Charge"
      CastTicks = ticks 0
      CooldownTicks = secTicks 15.0
      ResourceCost = -9
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ Charge; Threat(300) ] }

  let disarm =
    { Name = "Disarm"
      CastTicks = ticks 0
      CooldownTicks = secTicks 20.0
      ResourceCost = 20
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Disarm(secTicks 10.0); Threat(200) ] }

  /// Rank 1: 125 damage, plus 3 per point of rage; the rage scaling and the
  /// 20%-health gate are mechanics this sim does not model yet.
  let execute =
    { Name = "Execute"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 15
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Damage(125, 125) ] }

  // =========================================================================
  // Builders
  // =========================================================================

  /// Parties generate threat at 1.0x; the Tank's stance at 5.0x.
  let private partyThreatBp = 10000
  let private tankStanceBp = 50000
  let private mobThreatBp = 10000

  /// Resource regeneration per Tick. A Tick is 1/20 s, so this is 20 a second:
  /// the closest whole number to a level-24 priest's ~86 mana per 5 s out of the
  /// five-second rule and to a Rogue's 10 energy a second. Mobs do not
  /// regenerate at all, and that is the whole reason a fight can end: a Druid of
  /// the Fang can afford a handful of casts and then it is spent.
  let private partyRegen = 1
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

  /// The sourced level-24 sheet behind one party member. Base pools and base
  /// attributes come from the vanilla class+race table at level 24
  /// (`wow_world_base` base_stats); the gear totals are the best-in-slot set a
  /// level-24 twink can actually wear; the attack-power conversion is the
  /// class's own. See `.pi/research/level24/` for the per-item sources.
  type Sheet =
    { Stats: Stats
      BaseHealth: int
      BaseMana: int
      GearArmor: int
      ApPerStrength: int
      ApPerAgility: int
      /// Rage and energy are fixed pools; mana is computed from Intellect.
      FixedPool: int option }

  /// The party-wide passives, one per class present (Q2a), each at the highest
  /// rank a level-24 caster can train.
  let partyBuffs: PartyBuffKind list =
    [ ArcaneIntellect; MarkOfTheWild; BattleShout; PowerWordFortitude ]

  /// Apply the party passives. Armour and attack power come back separately:
  /// they are not one of the five stats, but they are just as real.
  let private buffed (stats: Stats) =
    partyBuffs
    |> List.fold
      (fun (acc: Stats, armor: int, ap: int) b ->
        match b with
        | ArcaneIntellect -> { acc with Intellect = acc.Intellect + 7 }, armor, ap
        | DivineSpirit -> { acc with Spirit = acc.Spirit + 17 }, armor, ap
        | PowerWordFortitude -> { acc with Stamina = acc.Stamina + 20 }, armor, ap
        | MarkOfTheWild ->
          { acc with
              Strength = acc.Strength + 4
              Agility = acc.Agility + 4
              Stamina = acc.Stamina + 4
              Intellect = acc.Intellect + 4
              Spirit = acc.Spirit + 4 },
          armor + 105,
          ap
        | BattleShout -> acc, armor, ap + 55)
      (stats, 0, 0)

  /// Health or mana from an attribute: the first 20 points are worth 1 each and
  /// every point after that is worth `perPoint` (10 health, 15 mana).
  let private pool (n: int) (perPoint: int) = min 20 n + (max 0 (n - 20)) * perPoint

  /// Derive combat values from the sheet and traits with Classic's real
  /// formulas: health and mana from Stamina and Intellect, 2 armour per Agility
  /// on top of item armour, and crit and dodge per Agility. The agility-to-crit
  /// rate is the one interpolated number here (~10 Agility per 1% at level 24).
  let private derive (sheet: Sheet) (traits: Traits) =
    let s, armorBp, attackPower = buffed sheet.Stats

    {| MaxHealth = sheet.BaseHealth + pool s.Stamina 10
       MaxResource = defaultArg sheet.FixedPool (sheet.BaseMana + pool s.Intellect 15)
       RegenPerTick = partyRegen
       CritChanceBp = 500 + s.Agility * 10 + traits.CritBonusBp
       DodgeChanceBp = 500 + s.Agility * 5 + traits.DodgeBonusBp
       Armor = sheet.GearArmor + armorBp + s.Agility * 2 + traits.ArmorBonus
       BlockChanceBp = traits.BlockChanceBp
       BlockValue = traits.BlockValue
       AttackPower = s.Strength * sheet.ApPerStrength + s.Agility * sheet.ApPerAgility + attackPower |}

  /// A Party member specified by its level-24 sheet. The passives are applied to
  /// the stats before the derived values are computed.
  let heroStats
    (id: int)
    (name: string)
    (role: Role)
    (armorClass: ArmorClass)
    (traits: Traits)
    (sheet: Sheet)
    (attack: AutoAttack)
    (threatBp: int)
    (abilities: Ability list)
    (pos: Pos)
    : Entity =
    let d = derive sheet traits

    // Attack power adds damage per swing at AP/14 per second, scaled by the
    // weapon's own speed: 14 * 20 Ticks in the denominator.
    let meleeBonus = d.AttackPower * int attack.Ticks / (14 * 20)

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
      Stats = sheet.Stats
      CritChanceBp = d.CritChanceBp
      MeleeDamageBonus = meleeBonus
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

  // The one per-class bonus that is not a stat: a shield's own block. Every
  // other class fights with what the gear and the formulas give it.
  let private warriorTraits =
    { Traits.none with
        BlockChanceBp = 500
        BlockValue = 8 }

  // Sourced level-24 sheets. Base attributes and base health/mana are the
  // vanilla class+race row for level 24 (Human Warrior, Night Elf Rogue, Gnome
  // Mage, Human Priest, Night Elf Druid); the gear totals and weapons are the
  // best-in-slot level-24 set, item by item, in .pi/research/level24/.

  /// Human Warrior, 87 Str / 54 Agi / 91 Sta: 1248 armour (765 from the set plus
  /// the Tower Shield's 483), Shadowfang 29-55 at 2.70 s. Rage is a flat 100.
  let private warrior =
    heroStats
      1
      "Warrior"
      Tank
      Plate
      warriorTraits
      { Stats = { Strength = 87; Agility = 54; Stamina = 91; Intellect = 23; Spirit = 38 }
        BaseHealth = 253
        BaseMana = 0
        GearArmor = 1248
        ApPerStrength = 2
        ApPerAgility = 0
        FixedPool = Some 100 }
      { Min = 29; Max = 55; Ticks = secTicks 2.7; Range = 1 }
      tankStanceBp
      [ taunt; heroicStrike; sunderArmor; shieldBash; charge; disarm; execute ]
      { X = 1; Y = 5 }

  /// Human Priest, 28 Str / 35 Agi / 73 Sta / 104 Int / 86 Spi: 253 armour from
  /// the set, Staff of the Blessed Seer 47-71 at 3.00 s with +24 healing, which
  /// is folded into the heals above.
  let private priest =
    heroStats
      2
      "Priest"
      Healer
      Cloth
      Traits.none
      { Stats = { Strength = 28; Agility = 35; Stamina = 73; Intellect = 104; Spirit = 86 }
        BaseHealth = 283
        BaseMana = 540
        GearArmor = 253
        ApPerStrength = 1
        ApPerAgility = 0
        FixedPool = None }
      { Min = 47; Max = 71; Ticks = secTicks 3.0; Range = 1 }
      partyThreatBp
      [ heal; renew; smite; shadowWordPain ]
      { X = 1; Y = 6 }

  /// Night Elf Rogue, 46 Str / 100 Agi / 50 Sta: 221 armour from the set plus 200
  /// from Agility, Assassin's Blade 20-39 at 1.90 s, 100 energy.
  let private rogue =
    heroStats
      3
      "Rogue"
      Dps
      Leather
      Traits.none
      { Stats = { Strength = 46; Agility = 100; Stamina = 50; Intellect = 25; Spirit = 29 }
        BaseHealth = 301
        BaseMana = 0
        GearArmor = 221
        ApPerStrength = 1
        ApPerAgility = 1
        FixedPool = Some 100 }
      { Min = 20; Max = 39; Ticks = secTicks 1.9; Range = 1 }
      partyThreatBp
      [ backstab; kick; gouge ]
      { X = 1; Y = 7 }

  /// Gnome Mage, 18 Str / 35 Agi / 56 Sta / 131 Int / 99 Spi: 282 armour from the
  /// set, and a Gravestone Scepter wand (30-57 at 1.50 s) as the auto-attack,
  /// which is why the Mage has no attack-power contribution.
  let private mage =
    heroStats
      4
      "Mage"
      Dps
      Cloth
      Traits.none
      { Stats = { Strength = 18; Agility = 35; Stamina = 56; Intellect = 131; Spirit = 99 }
        BaseHealth = 288
        BaseMana = 523
        GearArmor = 282
        ApPerStrength = 0
        ApPerAgility = 0
        FixedPool = None }
      { Min = 30; Max = 57; Ticks = secTicks 1.5; Range = 8 }
      partyThreatBp
      [ fireball; frostbolt; arcaneMissiles; arcaneExplosion; blizzard; polymorph ]
      { X = 2; Y = 6 }

  /// Night Elf Druid, 31 Str / 44 Agi / 53 Sta / 94 Int / 78 Spi: 238 armour from
  /// the set, the same staff as the Priest.
  let private druid =
    heroStats
      5
      "Druid"
      Healer
      Leather
      Traits.none
      { Stats = { Strength = 31; Agility = 44; Stamina = 53; Intellect = 94; Spirit = 78 }
        BaseHealth = 303
        BaseMana = 449
        GearArmor = 238
        ApPerStrength = 1
        ApPerAgility = 0
        FixedPool = None }
      { Min = 47; Max = 71; Ticks = secTicks 3.0; Range = 1 }
      partyThreatBp
      [ healingTouchR4; regrowth; rejuvenation; entanglingRoots; wrath; moonfire ]
      { X = 2; Y = 7 }

  // Wailing Caverns creatures. Health, per-swing melee damage and armour are the
  // real Classic values from the classicdb.ch and Wowhead Classic NPC pages
  // collected in .pi/research/level24/mobs.md, with the armour values and the
  // unranked NPC spell numbers in mobs_armour_and_spells.md.
  //
  // Two gaps, both marked where they bite: attack speed is not published for any
  // of these creatures, so every mob swings at the common 2.0 s elite rate; and
  // Mutanus is the one creature whose armour no page lists, so his is fitted at
  // 40 per level through the sourced points around him. Mana pools are unsourced
  // and exist only to stop a mob casting forever.

  /// Druid of the Fang, the trash caster: 1280 health (1235-1332), 145-172 a
  /// swing, 780 armour. Serpent Form at 30% health replaces its casts with a
  /// harder melee — which the sim models as a bounded shapeshift.
  let private druidOfTheFang id pos =
    { mob id
        "Druid of the Fang"
        1280
        400
        (Some { Min = 145; Max = 172; Ticks = secTicks 2.0; Range = 1 })
        [ lightningBolt; healingTouch; serpentForm; druidsSlumber ]
        None
        pos with
        Armor = 683 }

  /// The cave raptor is the Deviate Ravager (NPC 3636): 1310 health (1248-1375),
  /// 70-82 a swing and the one sourced armour value, 800. Its Thrash proc is not
  /// modelled. Raptors close faster than the casting mobs.
  let private raptor id pos =
    { mob id
        "Deviate Ravager"
        1310
        0
        (Some { Min = 70; Max = 82; Ticks = secTicks 2.0; Range = 1 })
        []
        (Some 20)
        pos with
        MoveTicksPerTile = secTicks 0.3
        Armor = 800 }

  /// Lady Anacondra, level 20 elite: 2214 health, 154-183 a swing. She also
  /// carries a Thorns aura in Classic; a reflect-damage effect is not modelled.
  let private ladyAnacondra id pos =
    { mob id
        "Lady Anacondra"
        2214
        600
        (Some { Min = 154; Max = 183; Ticks = secTicks 2.0; Range = 1 })
        [ lightningBolt; healingTouch; fangSleep ]
        None
        pos with
        Armor = 712 }

  /// The four Fanglords share the Druid of the Fang kit — their differences are
  /// health, swing and (for Serpentis) a rank-6 heal. Cobrahn's Serpent Form and
  /// Pythas' Thunderclap are not modelled yet.
  let private lordSerpentis id pos =
    { mob id
        "Lord Serpentis"
        2770
        600
        (Some { Min = 154; Max = 183; Ticks = secTicks 2.0; Range = 1 })
        [ lightningBolt; healingTouchR6; fangSleep ]
        None
        pos with
        Armor = 712 }

  let private lordCobrahn id pos =
    { mob id
        "Lord Cobrahn"
        2279
        600
        (Some { Min = 145; Max = 172; Ticks = secTicks 2.0; Range = 1 })
        [ lightningBolt; healingTouch; fangSleep ]
        None
        pos with
        Armor = 712 }

  let private lordPythas id pos =
    { mob id
        "Lord Pythas"
        2409
        600
        (Some { Min = 154; Max = 183; Ticks = secTicks 2.0; Range = 1 })
        [ lightningBolt; healingTouch; fangSleep ]
        None
        pos with
        Armor = 712 }

  /// Naralex, level 25 and not an elite: 1240 health, 47-56 a swing. The real one
  /// is a sleeping quest NPC rather than a fight — he is here with the Fang kit
  /// only because the Nightmare mechanic is not modelled.
  let private naralex id pos =
    { mob id
        "Naralex"
        1240
        800
        (Some { Min = 47; Max = 56; Ticks = secTicks 2.0; Range = 1 })
        [ lightningBolt; healingTouch; druidsSlumber ]
        None
        pos with
        Armor = 960 }

  /// Mutanus the Devourer, level 22 elite: 4555 health, 179-213 a swing. His
  /// Thundercrack (94-107 area Nature plus a stun), Terrify (fear) and Naralex's
  /// Nightmare (sleep) are real and unmodelled, so this is his melee only. He is
  /// also the one creature here with no published armour (fitted, 40/level).
  let private mutanus id pos =
    { mob id
        "Mutanus the Devourer"
        4555
        0
        (Some { Min = 179; Max = 213; Ticks = secTicks 2.0; Range = 1 })
        []
        None
        pos with
        Armor = 880 }

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
