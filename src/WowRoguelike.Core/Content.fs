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

  /// Cobrahn's Serpent Form is spell 7965, the Fanglord's version: +50 a swing
  /// for five minutes, and no spells at all while it lasts. Long enough that it
  /// is the rest of his fight.
  let serpentFormCobrahn =
    { Name = "Serpent Form"
      CastTicks = ticks 0
      CooldownTicks = secTicks 30.0
      ResourceCost = 0
      Interruptible = false
      Range = 0
      TargetKind = Self
      Effects = [ Shapeshift(Serpent, secTicks 300.0, 50) ] }

  /// Thunderclap (spell 8147), Lord Pythas: instant Nature damage to everything
  /// within a couple of tiles of him. The 43% slower attacks and the snare are
  /// not modelled; the damage is the part that kills.
  let thunderclap =
    { Name = "Thunderclap"
      CastTicks = ticks 0
      CooldownTicks = secTicks 8.0
      ResourceCost = 0
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ AreaDamage(2, 71, 79) ] }

  /// Thundercrack (spell 8150), Mutanus: 94-106 Nature to everything in melee
  /// and a stun. The one boss mechanic a melee-heavy party has to respect.
  /// shortcut: the stun lands on the primary target only, stun the whole area if
  /// the difference ever shows through the fight.
  let thundercrack =
    { Name = "Thundercrack"
      CastTicks = ticks 0
      CooldownTicks = secTicks 18.0
      ResourceCost = 0
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ AreaDamage(2, 94, 106); Stun(secTicks 2.5) ] }

  /// Terrify (spell 7399). Vanilla makes the target flee for 4 s; the sim has no
  /// fleeing, so fear is a stun that logs under its own name.
  /// shortcut: fear == stun, add a fleeing state if running away ever matters.
  let terrify =
    { Name = "Terrify"
      CastTicks = ticks 0
      CooldownTicks = secTicks 20.0
      ResourceCost = 0
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Stun(secTicks 4.0) ] }

  /// Naralex's Nightmare (spell 7967): the two-second slumber Mutanus opens with,
  /// aimed past the Tank at whoever is holding the fight together.
  let naralexsNightmare =
    { Name = "Naralex's Nightmare"
      CastTicks = secTicks 2.0
      CooldownTicks = secTicks 30.0
      ResourceCost = 0
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ Sleep(secTicks 15.0) ] }

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
      // Arcane Shot rank 3 (level 20): 33 Arcane for 50 mana. Magic, so it is not
      // armour-mitigated — the old record had it as 90 mana and physical `Damage`.
      ResourceCost = 50
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(33, 33) ] }

  // -- Paladin --------------------------------------------------------------
  // A level-24 paladin's kit. Like the Priest, Mage, Druid and Warrior these
  // are single-rank records carrying the rank a level-24 paladin has trained;
  // only the Rogue's abilities downrank with level. Ranges follow the codebase's
  // 40-yd-is-8-tiles convention rather than WoW's yards.

  /// Holy Light rank 4 (level 22): 310-357 on 1.12, 322-368 on the 1.15.9 client
  /// this content otherwise follows. 190 mana, 2.5 s cast, 40 yd.
  let holyLight =
    { Name = "Holy Light"
      CastTicks = secTicks 2.5
      CooldownTicks = ticks 0
      ResourceCost = 190
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(322, 368) ] }

  /// Flash of Light rank 1 (level 20): 62-73 on 1.12, 67-77 on 1.15.9. The
  /// efficient heal — 1.77-2.09 healing per mana against Holy Light's 1.63-1.88.
  let flashOfLight =
    { Name = "Flash of Light"
      CastTicks = secTicks 1.5
      CooldownTicks = ticks 0
      ResourceCost = 35
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(67, 77) ] }

  /// Judgement rank 1 (level 4) with Seal of Righteousness rank 3 up: 53-57 Holy
  /// on a 10 s cooldown for 6% of base mana. Holy damage, so unmitigated.
  ///
  /// shortcut: the seal itself is not modelled, so Judgement is a plain Holy
  /// nuke rather than something that spends a seal. Add seals if the paladin's
  /// swing-by-swing damage ever matters.
  let judgement =
    { Name = "Judgement"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 31
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ SpellDamage(53, 57) ] }

  /// Hammer of Justice rank 2 (level 24): a 4 s stun on a 1 min cooldown. The
  /// paladin's interrupt and peel. Distinct from the mobs' `hammerOfJustice`
  /// below, which is the NPC spell 853 — a 3 s stun for a third of the mana.
  let paladinHammerOfJustice =
    { Name = "Hammer of Justice"
      CastTicks = ticks 0
      CooldownTicks = secTicks 60.0
      ResourceCost = 50
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ Stun(secTicks 4.0) ] }

  /// Divine Protection rank 2 (level 18): 8 s of physical and spell immunity for
  /// 35 mana on a 5 min cooldown. The sim has no "cannot act" state, so the
  /// paladin keeps fighting through it, which makes it strictly stronger here
  /// than in WoW.
  /// shortcut: immunity without the cannot-act penalty, add the penalty if the
  /// paladin's survivability ever decides a fight on its own.
  let divineProtection =
    { Name = "Divine Protection"
      CastTicks = ticks 0
      CooldownTicks = secTicks 300.0
      ResourceCost = 35
      Interruptible = false
      Range = 0
      TargetKind = Self
      Effects = [ ReducesDamage(secTicks 8.0, 10000) ] }

  // -- Warlock --------------------------------------------------------------
  // A level-24 Affliction warlock's kit. Values are 1.12 (classicdb), the same
  // lineage as the rest of the party, not the ~7 % hotter wowcalc tooltips.

  /// Shadow Bolt rank 4 (level 20): 86-99 Shadow, 110 mana, 3.0 s cast, 30 yd.
  /// The only spell the +30 shadow damage on the set amplifies per cast.
  let shadowBolt =
    { Name = "Shadow Bolt"
      CastTicks = secTicks 3.0
      CooldownTicks = ticks 0
      ResourceCost = 110
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(86, 99) ] }

  /// Corruption rank 3 (level 24): 222 Shadow over 18 s, 100 mana. A 2.0 s cast
  /// in vanilla — Improved Corruption is what makes it instant, and a level-24
  /// warlock cannot afford 5/5 in it. Ticks land 37 every 3 s, so 12 a second.
  let corruption =
    { Name = "Corruption"
      CastTicks = secTicks 2.0
      CooldownTicks = ticks 0
      ResourceCost = 100
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ DamageOverTime(secTicks 18.0, 12) ] }

  /// Curse of Agony rank 2 (level 18): 180 Shadow over 24 s, instant, 50 mana.
  /// Escalating in WoW; flat here, at the 7.5-a-second average.
  /// shortcut: every damage-over-time shares one `Corrupted` aura, so Corruption
  /// and Curse of Agony refresh each other instead of stacking. Split the aura
  /// by source if the warlock ever needs both on one target.
  let curseOfAgony =
    { Name = "Curse of Agony"
      CastTicks = ticks 0
      CooldownTicks = ticks 0
      ResourceCost = 50
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ DamageOverTime(secTicks 24.0, 7) ] }

  /// Immolate rank 3 (level 20): 45 Fire up front and 90 more over 15 s, 90 mana,
  /// 2.0 s cast. Fire, so the shadow damage on the gear does nothing for it.
  let warlockImmolate =
    { Name = "Immolate"
      CastTicks = secTicks 2.0
      CooldownTicks = ticks 0
      ResourceCost = 90
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(45, 45); DamageOverTime(secTicks 15.0, 6) ] }

  /// Fear rank 1 (level 8): 1.5 s cast, 15 % of base mana, 20 yd. The target
  /// stops acting for up to 10 s. The sim has no fleeing state, so this is a
  /// `Sleep` — the mob stands still and takes it, which is weaker than WoW's
  /// fear only in that it cannot be chased into another pack.
  /// shortcut: fear modelled as sleep, add a flee state if mobs need to run.
  let fear =
    { Name = "Fear"
      CastTicks = secTicks 1.5
      CooldownTicks = ticks 0
      ResourceCost = 70
      Interruptible = true
      Range = 5
      TargetKind = Foe
      Effects = [ Sleep(secTicks 10.0) ] }

  // -- Hunter ---------------------------------------------------------------
  // A level-24 marksman hunter. Ranks are what a 24 has trained; Rapid Fire (26)
  // and Aimed Shot (21 talent points, only 15 available) are out. Values from the
  // 1.12/1.15.9 rank tables in `.pi/research/level24/hunter.md`.

  /// Serpent Sting rank 3 (level 18): 80 Nature over 15 s, 100 mana, instant.
  let serpentSting =
    { Name = "Serpent Sting"
      CastTicks = ticks 0
      CooldownTicks = ticks 0
      ResourceCost = 50
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ DamageOverTime(secTicks 15.0, 5) ] }

  /// Raptor Strike rank 4 (level 24): the next melee swing lands +34 damage for 45
  /// mana on a 6 s cooldown.
  let raptorStrike =
    { Name = "Raptor Strike"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 45
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ WeaponDamage(10000, 34) ] }

  // -- Shaman ---------------------------------------------------------------
  // A level-24 elemental shaman's kit. Ranks are the highest a level-24 shaman has
  // trained: Windfury Weapon is level 30 and Chain Lightning 32, so neither is
  // here. Values from the 1.15.9 client rank tables, recorded in
  // `.pi/research/level24/shaman.md`.

  /// Lightning Bolt rank 4 (level 20): 88-100 Nature, 75 mana, 3.0 s, 30 yd.
  let shamanLightningBolt =
    { Name = "Lightning Bolt"
      CastTicks = secTicks 3.0
      CooldownTicks = ticks 0
      ResourceCost = 75
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(88, 100) ] }

  /// Earth Shock rank 4 (level 24): 125-133 Nature, instant, 145 mana, 6 s
  /// cooldown, 20 yd — and it is the shaman's interrupt, which is why the
  /// party gains a second kick that is not the Rogue's or the Warrior's.
  /// shortcut: shocks share one 6 s cooldown in WoW; here each has its own, so a
  /// shaman can chain Earth Shock into Flame Shock.
  let earthShock =
    { Name = "Earth Shock"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 145
      Interruptible = false
      Range = 5
      TargetKind = Foe
      Effects = [ SpellDamage(125, 133); Interrupt ] }

  /// Flame Shock rank 2 (level 18): 51 Fire up front and 48 more over 12 s, 95
  /// mana, instant, 6 s cooldown, 20 yd.
  let flameShock =
    { Name = "Flame Shock"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 95
      Interruptible = false
      Range = 5
      TargetKind = Foe
      Effects = [ SpellDamage(51, 51); DamageOverTime(secTicks 12.0, 4) ] }

  /// Healing Wave rank 5 (level 24): 389-453 for 200 mana on a 3.0 s cast. A
  /// damage shaman will not cast it — the party's healer does the healing — but
  /// it is what a shaman has, so it is in the kit.
  let shamanHealingWave =
    { Name = "Healing Wave"
      CastTicks = secTicks 3.0
      CooldownTicks = ticks 0
      ResourceCost = 200
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(389, 453) ] }

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

  /// Shield Block: +75% block for 10 s for 10 rage, so the shield stops being a
  /// 5% coin flip and becomes what the Tank is doing with his rage.
  let shieldBlock =
    { Name = "Shield Block"
      CastTicks = ticks 0
      CooldownTicks = secTicks 5.0
      ResourceCost = 10
      Interruptible = false
      Range = 0
      TargetKind = Self
      Effects = [ Blocks(secTicks 10.0) ] }

  /// Shield Wall: 75% of all damage taken removed for 10 s, free, on the 30
  /// minute cooldown that makes it a decision rather than a rotation.
  let shieldWall =
    { Name = "Shield Wall"
      CastTicks = ticks 0
      CooldownTicks = secTicks 1800.0
      ResourceCost = 0
      Interruptible = false
      Range = 0
      TargetKind = Self
      Effects = [ ReducesDamage(secTicks 10.0, 7500) ] }

  // -- Rogue ----------------------------------------------------------------
  // Every rank from the vanilla 1.12 tables in .pi/research/ranks/rogue.md
  // (per-rank rows read off the Classic Era client, magnitudes cross-checked
  // against classicdb spell ids). Energy costs do not vary by rank: Sinister
  // Strike is 45 and Backstab 60 from level 1 to 60.

  /// Sinister Strike: 100% weapon damage plus a flat step that grows by rank.
  let sinisterStrike: AbilityDef =
    { Name = "Sinister Strike"
      Resource = Energy
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, bonus in
            [ 1, 3; 6, 6; 14, 10; 22, 15; 30, 22; 38, 33; 46, 52; 54, 68 ] do
            { Level = level
              Cost = 45
              CastTicks = ticks 0
              CooldownTicks = ticks 0
              Range = 1
              Effects = [ WeaponDamage(10000, bonus); ComboPoint 1 ] } ] }

  /// Backstab: 150% weapon damage plus a step that nearly doubles by the top
  /// rank. No cooldown — the 60 energy is the whole limiter, which at 10 energy
  /// a second means one Backstab every six seconds. Rank 9 exists only on the
  /// 1.15.9 client, so this table stops at rank 8.
  let backstab: AbilityDef =
    { Name = "Backstab"
      Resource = Energy
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, bonus in
            [ 4, 15; 12, 30; 20, 48; 28, 69; 36, 90; 44, 135; 52, 165; 60, 210 ] do
            { Level = level
              Cost = 60
              CastTicks = ticks 0
              CooldownTicks = ticks 0
              Range = 1
              Effects = [ WeaponDamage(15000, bonus); ComboPoint 1 ] } ] }

  /// Eviscerate: the finisher. Damage is exactly linear in combo points (rank 4:
  /// 41-61 at one point, +31 a point to 165-185 at five), so a rank only needs
  /// its one-point pair and its step.
  let eviscerate: AbilityDef =
    { Name = "Eviscerate"
      Resource = Energy
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, lo, hi, step in
            [ 1, 6, 10, 5
              8, 14, 22, 8
              16, 25, 39, 14
              24, 41, 61, 31
              32, 60, 90, 45
              40, 99, 143, 77
              48, 144, 212, 110
              56, 199, 295, 151 ] do
            { Level = level
              Cost = 35
              CastTicks = ticks 0
              CooldownTicks = ticks 0
              Range = 1
              Effects = [ Finisher(lo, hi, step) ] } ] }

  /// Gouge: 45 energy, a 4 s incap, and it builds a combo point.
  let gouge: AbilityDef =
    { Name = "Gouge"
      Resource = Energy
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, damage in [ 6, 10; 18, 20; 32, 32; 46, 55; 60, 75 ] do
            { Level = level
              Cost = 45
              CastTicks = ticks 0
              CooldownTicks = secTicks 10.0
              Range = 1
              Effects = [ Damage(damage, damage); Stun(secTicks 4.0); ComboPoint 1 ] } ] }

  /// Kick: 25 energy, 10 s cooldown, five-second school lockout.
  let kick: AbilityDef =
    { Name = "Kick"
      Resource = Energy
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, damage in [ 12, 15; 26, 30; 42, 45; 58, 80 ] do
            { Level = level
              Cost = 25
              CastTicks = ticks 0
              CooldownTicks = secTicks 10.0
              Range = 1
              Effects = [ Damage(damage, damage); Interrupt ] } ] }

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

  /// Counterspell: learnable at 24, instant, 100 mana, 30 s cooldown, 30 yd —
  /// the only stop the Mage has for a healer's cast. The 10 s school lock is not
  /// modelled; the interrupt is, which is the part that matters.
  let counterspell =
    { Name = "Counterspell"
      CastTicks = ticks 0
      CooldownTicks = secTicks 30.0
      ResourceCost = 100
      Interruptible = false
      Range = 30
      TargetKind = Foe
      Effects = [ Interrupt ] }

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

  /// Out-of-combat regeneration, in basis points of the pool per Tick: 50 is
  /// 0.5% a Tick, which at 20Hz is 10% a second — a party is whole again in the
  /// ten seconds it takes to walk to the next room. Only the party regenerates;
  /// a mob that heals up behind a corner would make a cleared room unwinnable.
  let outOfCombatHealthBpPerTick = 50
  let outOfCombatResourceBpPerTick = 75

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
      Dormant = false
      Name = name
      Level = 24
      CreatureType = CreatureType.Humanoid
      OffHand = false
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
      ResourceKind = Mana
      ComboPoints = 0
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
      HasCalledForHelp = false
      Owner = None
      ExpiresInTicks = None }

  let mob
    (id: int)
    (name: string)
    (kind: CreatureType)
    (level: int)
    (hp: int)
    (resource: int)
    (attack: AutoAttack option)
    (abilities: Ability list)
    (callForHelpBelowPct: int option)
    (pos: Pos)
    : Entity =
    { Id = EntityId id
      Dormant = false
      Name = name
      Level = level
      CreatureType = kind
      OffHand = false
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
      ResourceKind = Mana
      ComboPoints = 0
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
      HasCalledForHelp = false
      Owner = None
      ExpiresInTicks = None }

  /// One wearable piece: the level it needs and what it gives while worn. No item
  /// in any set grants attack power or crit, so there are no fields for them.
  type GearPiece =
    { Name: string
      /// None when the item is gated by a quest or a profession rather than a
      /// character level — 13 pieces across the five sets. Those are worn at
      /// every level, which makes a low-level party slightly stronger than truth;
      /// see .pi/research/levelscale/gear_req_levels.md.
      ReqLevel: int option
      Armor: int
      /// The off-hand the Warrior's shield abilities need to be legal.
      Shield: bool
      Stats: Stats }

  let private gear (name: string) (req: int option) (armor: int) (str, agi, sta, intel, spi) =
    { Name = name
      ReqLevel = req
      Armor = armor
      Shield = false
      Stats =
        { Strength = str
          Agility = agi
          Stamina = sta
          Intellect = intel
          Spirit = spi } }

  let private shieldPiece (name: string) (req: int option) (armor: int) (str, agi, sta, intel, spi) =
    { gear name req armor (str, agi, sta, intel, spi) with Shield = true }

  /// One level of a race+class base row as
  /// (strength, agility, stamina, intellect, spirit, class base health, class base
  /// mana), level 1 at index 0. Verbatim from `wow_world_base`'s vanilla
  /// `base_stats.rs`, whose arrays are one row per level with index = level - 1;
  /// the level-24 entries here are the ones `.pi/research/level24/base_stats.md`
  /// documents. `health` and `mana` are the class constants, before Stamina and
  /// Intellect are added — `derive` does that.
  let private humanWarriorBase =
    [| (23, 20, 22, 20, 21, 20, 0)
       (24, 21, 23, 20, 21, 29, 0)
       (25, 21, 24, 20, 22, 38, 0)
       (26, 22, 25, 20, 22, 47, 0)
       (28, 23, 26, 20, 22, 56, 0)
       (29, 24, 27, 21, 22, 65, 0)
       (30, 24, 28, 21, 23, 74, 0)
       (31, 25, 29, 21, 23, 83, 0)
       (32, 26, 30, 21, 23, 92, 0)
       (33, 26, 31, 21, 24, 101, 0)
       (35, 27, 33, 21, 24, 100, 0)
       (36, 28, 34, 21, 24, 109, 0)
       (37, 29, 35, 21, 25, 118, 0)
       (39, 30, 36, 22, 25, 128, 0)
       (40, 30, 37, 22, 25, 139, 0)
       (41, 31, 38, 22, 26, 151, 0)
       (42, 32, 40, 22, 26, 154, 0)
       (44, 33, 41, 22, 26, 168, 0)
       (44, 33, 41, 22, 26, 168, 0)
       (47, 35, 43, 22, 27, 199, 0)
       (48, 35, 45, 23, 27, 206, 0)
       (49, 36, 46, 23, 28, 224, 0)
       (51, 37, 47, 23, 28, 243, 0)
       (52, 38, 49, 23, 29, 253, 0)
       (54, 39, 50, 23, 29, 274, 0)
       (55, 40, 51, 23, 29, 296, 0)
       (57, 41, 53, 23, 30, 309, 0)
       (58, 42, 54, 24, 30, 333, 0)
       (60, 43, 56, 24, 31, 348, 0)
       (62, 44, 57, 24, 31, 374, 0)
       (63, 45, 58, 24, 31, 401, 0)
       (65, 46, 60, 24, 32, 419, 0)
       (66, 47, 61, 24, 32, 448, 0)
       (68, 48, 63, 25, 33, 468, 0)
       (70, 49, 64, 25, 33, 499, 0)
       (72, 50, 66, 25, 34, 521, 0)
       (73, 51, 68, 25, 34, 545, 0)
       (75, 52, 69, 25, 34, 581, 0)
       (77, 53, 71, 26, 35, 609, 0)
       (79, 54, 72, 26, 35, 649, 0) |]

  let private humanPriestBase =
    [| (20, 20, 20, 22, 24, 31, 110)
       (20, 20, 20, 23, 25, 37, 165)
       (20, 20, 21, 24, 26, 42, 189)
       (21, 21, 21, 25, 28, 57, 200)
       (21, 21, 21, 27, 29, 72, 197)
       (21, 21, 22, 28, 30, 77, 210)
       (21, 21, 22, 29, 31, 92, 224)
       (21, 22, 22, 30, 32, 107, 239)
       (21, 22, 23, 31, 34, 112, 255)
       (22, 22, 23, 33, 35, 127, 272)
       (22, 22, 24, 34, 36, 132, 275)
       (22, 23, 24, 35, 38, 147, 294)
       (22, 23, 24, 36, 39, 162, 314)
       (22, 23, 25, 38, 40, 167, 320)
       (23, 23, 25, 39, 43, 182, 342)
       (23, 24, 26, 40, 44, 187, 365)
       (23, 24, 26, 42, 45, 202, 389)
       (23, 24, 26, 43, 47, 217, 399)
       (23, 25, 27, 44, 48, 222, 425)
       (24, 25, 27, 46, 50, 237, 437)
       (24, 25, 28, 47, 51, 242, 465)
       (24, 25, 28, 49, 53, 258, 494)
       (24, 26, 29, 50, 54, 265, 509)
       (25, 26, 29, 52, 56, 283, 540)
       (25, 26, 30, 53, 57, 292, 557)
       (25, 27, 30, 55, 59, 312, 590)
       (25, 27, 30, 56, 61, 333, 609)
       (25, 27, 31, 58, 63, 345, 644)
       (26, 28, 31, 59, 65, 368, 665)
       (26, 28, 32, 61, 67, 382, 687)
       (26, 28, 32, 63, 68, 407, 725)
       (26, 29, 33, 64, 70, 423, 749)
       (27, 29, 33, 66, 72, 450, 788)
       (27, 29, 34, 68, 73, 468, 812)
       (27, 30, 34, 69, 75, 497, 836)
       (28, 30, 35, 71, 77, 517, 860)
       (28, 30, 36, 73, 79, 538, 899)
       (28, 31, 36, 75, 81, 570, 923)
       (28, 31, 37, 76, 84, 593, 947)
       (29, 31, 37, 78, 85, 627, 971) |]

  let private nightElfRogueBase =
    [| (18, 28, 20, 20, 20, 25, 0)
       (19, 29, 21, 20, 20, 32, 0)
       (19, 30, 21, 20, 21, 49, 0)
       (20, 32, 22, 21, 21, 56, 0)
       (21, 33, 23, 21, 21, 63, 0)
       (21, 34, 23, 21, 22, 80, 0)
       (22, 36, 24, 21, 22, 87, 0)
       (23, 37, 24, 21, 22, 104, 0)
       (24, 38, 25, 21, 23, 111, 0)
       (24, 40, 26, 22, 23, 118, 0)
       (25, 41, 27, 22, 24, 125, 0)
       (26, 42, 27, 22, 24, 142, 0)
       (27, 44, 28, 22, 24, 149, 0)
       (27, 45, 29, 22, 25, 156, 0)
       (28, 47, 29, 23, 25, 173, 0)
       (29, 48, 30, 23, 26, 181, 0)
       (30, 49, 31, 23, 26, 190, 0)
       (31, 51, 32, 23, 26, 200, 0)
       (32, 53, 32, 23, 27, 221, 0)
       (32, 54, 33, 24, 27, 233, 0)
       (33, 56, 34, 24, 28, 246, 0)
       (34, 57, 35, 24, 28, 260, 0)
       (35, 59, 36, 24, 29, 275, 0)
       (36, 60, 36, 25, 29, 301, 0)
       (37, 62, 37, 25, 30, 318, 0)
       (38, 64, 38, 25, 30, 336, 0)
       (39, 65, 39, 25, 30, 355, 0)
       (40, 67, 40, 25, 31, 375, 0)
       (40, 69, 41, 26, 31, 396, 0)
       (41, 71, 41, 26, 32, 428, 0)
       (42, 72, 42, 26, 32, 451, 0)
       (43, 74, 43, 26, 33, 475, 0)
       (44, 76, 44, 27, 33, 500, 0)
       (45, 78, 45, 27, 34, 526, 0)
       (46, 80, 46, 27, 34, 553, 0)
       (48, 82, 47, 28, 35, 581, 0)
       (49, 83, 48, 28, 36, 610, 0)
       (50, 85, 49, 28, 36, 640, 0)
       (51, 87, 50, 28, 37, 671, 0)
       (52, 89, 51, 29, 37, 703, 0) |]

  let private gnomeMageBase =
    [| (15, 23, 19, 26, 22, 31, 100)
       (15, 23, 19, 28, 23, 37, 170)
       (15, 23, 20, 29, 24, 42, 181)
       (15, 24, 20, 32, 25, 57, 178)
       (15, 24, 20, 33, 27, 72, 191)
       (16, 24, 20, 34, 28, 87, 205)
       (16, 24, 21, 35, 29, 92, 220)
       (16, 24, 21, 36, 30, 107, 221)
       (16, 24, 21, 38, 31, 122, 238)
       (16, 25, 22, 39, 33, 127, 256)
       (16, 25, 22, 40, 34, 142, 275)
       (16, 25, 22, 42, 35, 157, 280)
       (16, 25, 23, 43, 36, 162, 301)
       (17, 25, 23, 44, 38, 177, 323)
       (17, 26, 23, 46, 39, 192, 331)
       (17, 26, 24, 47, 40, 197, 355)
       (17, 26, 24, 48, 42, 212, 365)
       (17, 26, 24, 50, 43, 227, 391)
       (17, 26, 25, 51, 44, 232, 403)
       (17, 27, 25, 54, 46, 247, 431)
       (18, 27, 25, 55, 47, 262, 445)
       (18, 27, 26, 57, 49, 267, 475)
       (18, 27, 26, 58, 50, 282, 491)
       (18, 28, 27, 60, 52, 288, 523)
       (18, 28, 27, 61, 53, 305, 541)
       (18, 28, 27, 63, 55, 323, 575)
       (18, 28, 28, 65, 56, 332, 595)
       (19, 28, 28, 66, 58, 352, 616)
       (19, 29, 29, 68, 59, 363, 652)
       (19, 29, 29, 70, 61, 385, 673)
       (19, 29, 29, 71, 63, 408, 694)
       (19, 29, 30, 74, 64, 422, 730)
       (19, 30, 30, 76, 66, 447, 751)
       (20, 30, 31, 77, 68, 463, 772)
       (20, 30, 31, 79, 69, 490, 793)
       (20, 31, 32, 81, 71, 508, 814)
       (20, 31, 32, 83, 73, 537, 850)
       (20, 31, 32, 85, 75, 567, 871)
       (21, 31, 33, 87, 76, 588, 892)
       (21, 32, 33, 88, 78, 620, 913) |]

  let private nightElfDruidBase =
    [| (18, 25, 19, 22, 22, 33, 17)
       (19, 25, 20, 23, 23, 41, 57)
       (19, 26, 20, 24, 24, 58, 65)
       (20, 26, 21, 25, 25, 65, 74)
       (20, 27, 21, 26, 26, 82, 84)
       (21, 27, 22, 27, 27, 89, 95)
       (21, 28, 23, 28, 28, 96, 107)
       (22, 28, 23, 28, 29, 113, 120)
       (22, 29, 24, 29, 30, 120, 134)
       (23, 29, 24, 30, 31, 137, 149)
       (23, 30, 25, 31, 33, 144, 165)
       (24, 30, 26, 32, 34, 151, 182)
       (24, 31, 26, 33, 35, 168, 200)
       (25, 31, 27, 34, 36, 175, 219)
       (26, 32, 28, 36, 37, 182, 239)
       (26, 32, 28, 37, 38, 199, 260)
       (27, 33, 29, 38, 40, 206, 282)
       (27, 34, 30, 39, 41, 214, 305)
       (28, 34, 30, 40, 42, 233, 329)
       (29, 35, 31, 41, 43, 243, 354)
       (29, 35, 32, 42, 45, 254, 380)
       (30, 36, 33, 43, 46, 266, 392)
       (31, 37, 33, 44, 47, 289, 420)
       (31, 37, 34, 46, 49, 303, 449)
       (32, 38, 35, 47, 50, 318, 479)
       (33, 38, 36, 48, 51, 334, 509)
       (33, 39, 36, 49, 53, 361, 524)
       (34, 40, 37, 50, 54, 379, 554)
       (35, 40, 38, 52, 56, 398, 584)
       (35, 41, 39, 53, 57, 418, 614)
       (36, 42, 40, 54, 58, 439, 629)
       (37, 42, 41, 56, 60, 461, 659)
       (38, 43, 41, 57, 61, 494, 689)
       (38, 44, 42, 58, 63, 518, 704)
       (39, 44, 43, 60, 64, 543, 734)
       (40, 45, 44, 61, 66, 569, 749)
       (41, 46, 45, 62, 68, 596, 779)
       (42, 46, 46, 64, 69, 624, 809)
       (42, 47, 47, 65, 71, 653, 824)
       (43, 48, 48, 67, 72, 683, 854) |]

  // -- The other four classes, for the nine-class roster ----------------------
  // Same shape and the same source as the five above (the client's own
  // PlayerBaseStats table). Extracted from
  // .pi/research/level24/base_stats_new_classes.md, which cross-checked every
  // level-24 row against an independent reference.

  /// Human Paladin: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private humanPaladinBase =
    [| (22, 20, 22, 20, 22, 28, 59)
       (23, 21, 23, 21, 23, 26, 64)
       (24, 21, 24, 21, 23, 34, 84)
       (25, 22, 25, 22, 24, 42, 90)
       (26, 22, 26, 22, 25, 50, 112)
       (27, 23, 27, 23, 25, 58, 120)
       (28, 23, 28, 24, 26, 66, 129)
       (29, 24, 28, 24, 26, 84, 154)
       (30, 24, 29, 25, 27, 92, 165)
       (31, 25, 30, 25, 28, 100, 192)
       (32, 25, 31, 26, 29, 108, 205)
       (33, 26, 32, 27, 29, 116, 219)
       (34, 27, 33, 27, 30, 124, 249)
       (35, 27, 34, 28, 31, 132, 265)
       (36, 28, 36, 29, 31, 131, 282)
       (38, 28, 37, 29, 32, 141, 315)
       (39, 29, 38, 30, 33, 152, 334)
       (40, 30, 39, 31, 34, 164, 354)
       (41, 30, 40, 31, 34, 177, 390)
       (42, 31, 41, 32, 35, 191, 412)
       (43, 32, 42, 33, 36, 206, 435)
       (45, 32, 43, 34, 37, 222, 459)
       (46, 33, 44, 34, 38, 239, 499)
       (47, 34, 46, 35, 38, 247, 525) |]

  /// Dwarf Paladin: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private dwarfPaladinBase =
    [| (24, 16, 25, 19, 20, 28, 59)
       (25, 17, 26, 20, 21, 26, 64)
       (26, 17, 27, 20, 21, 34, 84)
       (27, 18, 28, 21, 22, 42, 90)
       (28, 18, 29, 21, 23, 50, 112)
       (29, 19, 30, 22, 23, 58, 120)
       (30, 19, 31, 23, 24, 66, 129)
       (31, 20, 31, 23, 24, 84, 154)
       (32, 20, 32, 24, 25, 92, 165)
       (33, 21, 33, 24, 26, 100, 192)
       (34, 21, 34, 25, 27, 108, 205)
       (35, 22, 35, 26, 27, 116, 219)
       (36, 23, 36, 26, 28, 124, 249)
       (37, 23, 37, 27, 29, 132, 265)
       (38, 24, 39, 28, 29, 131, 282)
       (40, 24, 40, 28, 30, 141, 315)
       (41, 25, 41, 29, 31, 152, 334)
       (42, 26, 42, 30, 32, 164, 354)
       (43, 26, 43, 30, 32, 177, 390)
       (44, 27, 44, 31, 33, 191, 412)
       (45, 28, 45, 32, 34, 206, 435)
       (47, 28, 46, 33, 35, 222, 459)
       (48, 29, 47, 33, 36, 239, 499)
       (49, 30, 49, 34, 36, 247, 525) |]

  /// Orc Shaman: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private orcShamanBase =
    [| (24, 17, 23, 18, 25, 27, 53)
       (25, 17, 24, 19, 26, 34, 61)
       (25, 18, 25, 20, 27, 41, 68)
       (26, 18, 26, 20, 28, 48, 91)
       (27, 19, 26, 21, 29, 65, 100)
       (28, 19, 27, 22, 30, 72, 110)
       (29, 19, 28, 23, 31, 79, 121)
       (29, 20, 29, 24, 31, 86, 133)
       (30, 20, 30, 25, 32, 93, 161)
       (31, 21, 31, 25, 33, 100, 175)
       (32, 21, 32, 26, 34, 107, 190)
       (33, 22, 33, 27, 35, 114, 206)
       (33, 22, 34, 28, 36, 121, 223)
       (34, 23, 35, 29, 37, 128, 241)
       (35, 23, 36, 30, 39, 135, 260)
       (36, 24, 37, 31, 40, 142, 280)
       (37, 24, 38, 32, 41, 150, 301)
       (38, 25, 39, 33, 42, 159, 323)
       (39, 25, 40, 34, 43, 169, 346)
       (40, 26, 41, 35, 44, 180, 370)
       (41, 26, 42, 36, 45, 192, 395)
       (41, 27, 43, 37, 46, 205, 456)
       (42, 27, 44, 38, 47, 219, 448)
       (43, 28, 45, 39, 49, 234, 476) |]

  /// Troll Shaman: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private trollShamanBase =
    [| (22, 22, 22, 17, 23, 27, 53)
       (23, 22, 23, 18, 24, 34, 61)
       (23, 23, 24, 19, 25, 41, 68)
       (24, 23, 25, 19, 26, 48, 91)
       (25, 24, 25, 20, 27, 65, 100)
       (26, 24, 26, 21, 28, 72, 110)
       (27, 24, 27, 22, 29, 79, 121)
       (27, 25, 28, 23, 29, 86, 133)
       (28, 25, 29, 24, 30, 93, 161)
       (29, 26, 30, 24, 31, 100, 175)
       (30, 26, 31, 25, 32, 107, 190)
       (31, 27, 32, 26, 33, 114, 206)
       (31, 27, 33, 27, 34, 121, 223)
       (32, 28, 34, 28, 35, 128, 241)
       (33, 28, 35, 29, 37, 135, 260)
       (34, 29, 36, 30, 38, 142, 280)
       (35, 29, 37, 31, 39, 150, 301)
       (36, 30, 38, 32, 40, 159, 323)
       (37, 30, 39, 33, 41, 169, 346)
       (38, 31, 40, 34, 42, 180, 370)
       (39, 31, 41, 35, 43, 192, 395)
       (39, 32, 42, 36, 44, 205, 456)
       (40, 32, 43, 37, 45, 219, 448)
       (41, 33, 44, 38, 47, 234, 476) |]

  /// Tauren Shaman: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private taurenShamanBase =
    [| (26, 15, 23, 16, 24, 27, 53)
       (27, 15, 24, 17, 25, 34, 61)
       (27, 16, 25, 18, 26, 41, 68)
       (28, 16, 26, 18, 27, 48, 91)
       (29, 17, 26, 19, 28, 65, 100)
       (30, 17, 27, 20, 29, 72, 110)
       (31, 17, 28, 21, 30, 79, 121)
       (31, 18, 29, 22, 30, 86, 133)
       (32, 18, 30, 23, 31, 93, 161)
       (33, 19, 31, 23, 32, 100, 175)
       (34, 19, 32, 24, 33, 107, 190)
       (35, 20, 33, 25, 34, 114, 206)
       (35, 20, 34, 26, 35, 121, 223)
       (36, 21, 35, 27, 36, 128, 241)
       (37, 21, 36, 28, 38, 135, 260)
       (38, 22, 37, 29, 39, 142, 280)
       (39, 22, 38, 30, 40, 150, 301)
       (40, 23, 39, 31, 41, 159, 323)
       (41, 23, 40, 32, 42, 169, 346)
       (42, 24, 41, 33, 43, 180, 370)
       (43, 24, 42, 34, 44, 192, 395)
       (43, 25, 43, 35, 45, 205, 456)
       (44, 25, 44, 36, 46, 219, 448)
       (45, 26, 45, 37, 48, 234, 476) |]

  /// Night Elf Hunter: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private nightElfHunterBase =
    [| (17, 28, 20, 20, 21, 26, 63)
       (17, 29, 21, 21, 22, 33, 70)
       (18, 30, 22, 21, 22, 40, 91)
       (18, 32, 22, 22, 23, 57, 98)
       (19, 33, 23, 22, 23, 64, 121)
       (19, 34, 24, 23, 24, 71, 130)
       (19, 35, 25, 23, 24, 78, 155)
       (20, 36, 26, 24, 25, 85, 166)
       (20, 38, 27, 24, 26, 92, 193)
       (21, 39, 27, 25, 26, 109, 206)
       (21, 40, 28, 25, 27, 116, 235)
       (22, 42, 29, 26, 28, 123, 250)
       (22, 43, 30, 27, 28, 130, 266)
       (23, 44, 31, 27, 29, 138, 298)
       (23, 46, 32, 28, 29, 147, 316)
       (24, 47, 33, 28, 30, 157, 350)
       (24, 48, 34, 29, 31, 168, 370)
       (25, 50, 35, 30, 32, 180, 391)
       (25, 51, 36, 30, 32, 193, 428)
       (26, 53, 37, 31, 33, 207, 451)
       (26, 54, 38, 32, 34, 222, 475)
       (27, 56, 39, 32, 34, 238, 515)
       (27, 57, 40, 33, 35, 255, 541)
       (28, 59, 41, 34, 36, 273, 568) |]

  /// Dwarf Hunter: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private dwarfHunterBase =
    [| (22, 19, 24, 19, 20, 26, 63)
       (22, 20, 25, 20, 21, 33, 70)
       (23, 21, 26, 20, 21, 40, 91)
       (23, 23, 26, 21, 22, 57, 98)
       (24, 24, 27, 21, 22, 64, 121)
       (24, 25, 28, 22, 23, 71, 130)
       (24, 26, 29, 22, 23, 78, 155)
       (25, 27, 30, 23, 24, 85, 166)
       (25, 29, 31, 23, 25, 92, 193)
       (26, 30, 31, 24, 25, 109, 206)
       (26, 31, 32, 24, 26, 116, 235)
       (27, 33, 33, 25, 27, 123, 250)
       (27, 34, 34, 26, 27, 130, 266)
       (28, 35, 35, 26, 28, 138, 298)
       (28, 37, 36, 27, 28, 147, 316)
       (29, 38, 37, 27, 29, 157, 350)
       (29, 39, 38, 28, 30, 168, 370)
       (30, 41, 39, 29, 31, 180, 391)
       (30, 42, 40, 29, 31, 193, 428)
       (31, 44, 41, 30, 32, 207, 451)
       (31, 45, 42, 31, 33, 222, 475)
       (32, 47, 43, 31, 33, 238, 515)
       (32, 48, 44, 32, 34, 255, 541)
       (33, 50, 45, 33, 35, 273, 568) |]

  /// Troll Hunter: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private trollHunterBase =
    [| (21, 25, 22, 16, 22, 26, 63)
       (21, 26, 23, 17, 23, 33, 70)
       (22, 27, 24, 17, 23, 40, 91)
       (22, 29, 24, 18, 24, 57, 98)
       (23, 30, 25, 18, 24, 64, 121)
       (23, 31, 26, 19, 25, 71, 130)
       (23, 32, 27, 19, 25, 78, 155)
       (24, 33, 28, 20, 26, 85, 166)
       (24, 35, 29, 20, 27, 92, 193)
       (25, 36, 29, 21, 27, 109, 206)
       (25, 37, 30, 21, 28, 116, 235)
       (26, 39, 31, 22, 29, 123, 250)
       (26, 40, 32, 23, 29, 130, 266)
       (27, 41, 33, 23, 30, 138, 298)
       (27, 43, 34, 24, 30, 147, 316)
       (28, 44, 35, 24, 31, 157, 350)
       (28, 45, 36, 25, 32, 168, 370)
       (29, 47, 37, 26, 33, 180, 391)
       (29, 48, 38, 26, 33, 193, 428)
       (30, 50, 39, 27, 34, 207, 451)
       (30, 51, 40, 28, 35, 222, 475)
       (31, 53, 41, 28, 35, 238, 515)
       (31, 54, 42, 29, 36, 255, 541)
       (32, 56, 43, 30, 37, 273, 568) |]

  /// Orc Hunter: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private orcHunterBase =
    [| (23, 20, 23, 17, 24, 26, 63)
       (23, 21, 24, 18, 25, 33, 70)
       (24, 22, 25, 18, 25, 40, 91)
       (24, 24, 25, 19, 26, 57, 98)
       (25, 25, 26, 19, 26, 64, 121)
       (25, 26, 27, 20, 27, 71, 130)
       (25, 27, 28, 20, 27, 78, 155)
       (26, 28, 29, 21, 28, 85, 166)
       (26, 30, 30, 21, 29, 92, 193)
       (27, 31, 30, 22, 29, 109, 206)
       (27, 32, 31, 22, 30, 116, 235)
       (28, 34, 32, 23, 31, 123, 250)
       (28, 35, 33, 24, 31, 130, 266)
       (29, 36, 34, 24, 32, 138, 298)
       (29, 38, 35, 25, 32, 147, 316)
       (30, 39, 36, 25, 33, 157, 350)
       (30, 40, 37, 26, 34, 168, 370)
       (31, 42, 38, 27, 35, 180, 391)
       (31, 43, 39, 27, 35, 193, 428)
       (32, 45, 40, 28, 36, 207, 451)
       (32, 46, 41, 29, 37, 222, 475)
       (33, 48, 42, 29, 37, 238, 515)
       (33, 49, 43, 30, 38, 255, 541)
       (34, 51, 44, 31, 39, 273, 568) |]

  /// Tauren Hunter: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private taurenHunterBase =
    [| (25, 18, 23, 15, 23, 26, 63)
       (25, 19, 24, 16, 24, 33, 70)
       (26, 20, 25, 16, 24, 40, 91)
       (26, 22, 25, 17, 25, 57, 98)
       (27, 23, 26, 17, 25, 64, 121)
       (27, 24, 27, 18, 26, 71, 130)
       (27, 25, 28, 18, 26, 78, 155)
       (28, 26, 29, 19, 27, 85, 166)
       (28, 28, 30, 19, 28, 92, 193)
       (29, 29, 30, 20, 28, 109, 206)
       (29, 30, 31, 20, 29, 116, 235)
       (30, 32, 32, 21, 30, 123, 250)
       (30, 33, 33, 22, 30, 130, 266)
       (31, 34, 34, 22, 31, 138, 298)
       (31, 36, 35, 23, 31, 147, 316)
       (32, 37, 36, 23, 32, 157, 350)
       (32, 38, 37, 24, 33, 168, 370)
       (33, 40, 38, 25, 34, 180, 391)
       (33, 41, 39, 25, 34, 193, 428)
       (34, 43, 40, 26, 35, 207, 451)
       (34, 44, 41, 27, 36, 222, 475)
       (35, 46, 42, 27, 36, 238, 515)
       (35, 47, 43, 28, 37, 255, 541)
       (36, 49, 44, 29, 38, 273, 568) |]

  /// Gnome Warlock: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private gnomeWarlockBase =
    [| (15, 23, 20, 25, 22, 23, 59)
       (15, 23, 21, 27, 23, 28, 98)
       (16, 24, 21, 28, 24, 43, 107)
       (16, 24, 22, 29, 25, 48, 117)
       (16, 24, 22, 30, 26, 63, 128)
       (16, 25, 23, 32, 27, 68, 140)
       (17, 25, 23, 33, 29, 83, 153)
       (17, 25, 24, 34, 30, 88, 167)
       (17, 26, 24, 35, 31, 103, 182)
       (18, 26, 25, 36, 32, 108, 198)
       (18, 27, 25, 38, 33, 123, 200)
       (18, 27, 26, 39, 34, 128, 218)
       (19, 27, 26, 40, 36, 143, 237)
       (19, 28, 27, 41, 37, 148, 257)
       (19, 28, 28, 42, 38, 153, 278)
       (20, 29, 28, 43, 39, 168, 300)
       (20, 29, 29, 45, 41, 173, 308)
       (20, 29, 29, 46, 42, 189, 332)
       (21, 30, 30, 47, 43, 196, 357)
       (21, 30, 31, 48, 45, 204, 383)
       (21, 31, 31, 50, 46, 223, 395)
       (22, 31, 32, 51, 47, 233, 423)
       (22, 32, 33, 53, 49, 244, 452)
       (23, 32, 33, 55, 50, 266, 467) |]

  /// Human Warlock: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private humanWarlockBase =
    [| (20, 20, 21, 22, 22, 23, 59)
       (20, 20, 22, 23, 24, 28, 98)
       (21, 21, 22, 24, 25, 43, 107)
       (21, 21, 23, 25, 26, 48, 117)
       (21, 21, 23, 26, 27, 63, 128)
       (21, 22, 24, 27, 28, 68, 140)
       (22, 22, 24, 28, 30, 83, 153)
       (22, 22, 25, 29, 31, 88, 167)
       (22, 23, 25, 30, 32, 103, 182)
       (23, 23, 26, 31, 33, 108, 198)
       (23, 24, 26, 33, 34, 123, 200)
       (23, 24, 27, 34, 35, 128, 218)
       (24, 24, 27, 35, 37, 143, 237)
       (24, 25, 28, 36, 38, 148, 257)
       (24, 25, 29, 37, 39, 153, 278)
       (25, 26, 29, 38, 40, 168, 300)
       (25, 26, 30, 40, 43, 173, 308)
       (25, 26, 30, 41, 44, 189, 332)
       (26, 27, 31, 42, 45, 196, 357)
       (26, 27, 32, 43, 47, 204, 383)
       (26, 28, 32, 45, 48, 223, 395)
       (27, 28, 33, 46, 49, 233, 423)
       (27, 29, 34, 47, 51, 244, 452)
       (28, 29, 34, 49, 52, 266, 467) |]

  /// Orc Warlock: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private orcWarlockBase =
    [| (23, 17, 23, 19, 25, 23, 59)
       (23, 17, 24, 20, 26, 28, 98)
       (24, 18, 24, 21, 27, 43, 107)
       (24, 18, 25, 22, 28, 48, 117)
       (24, 18, 25, 23, 29, 63, 128)
       (24, 19, 26, 24, 30, 68, 140)
       (25, 19, 26, 25, 32, 83, 153)
       (25, 19, 27, 26, 33, 88, 167)
       (25, 20, 27, 27, 34, 103, 182)
       (26, 20, 28, 28, 35, 108, 198)
       (26, 21, 28, 30, 36, 123, 200)
       (26, 21, 29, 31, 37, 128, 218)
       (27, 21, 29, 32, 39, 143, 237)
       (27, 22, 30, 33, 40, 148, 257)
       (27, 22, 31, 34, 41, 153, 278)
       (28, 23, 31, 35, 42, 168, 300)
       (28, 23, 32, 37, 44, 173, 308)
       (28, 23, 32, 38, 45, 189, 332)
       (29, 24, 33, 39, 46, 196, 357)
       (29, 24, 34, 40, 48, 204, 383)
       (29, 25, 34, 42, 49, 223, 395)
       (30, 25, 34, 43, 50, 233, 423)
       (30, 26, 35, 44, 52, 244, 452)
       (31, 26, 35, 46, 53, 266, 467) |]

  /// Undead Warlock: the 7-tuple is (str, agi, sta, int, spi, base health, base mana).
  let private undeadWarlockBase =
    [| (19, 18, 22, 20, 27, 23, 59)
       (19, 18, 23, 21, 28, 28, 98)
       (20, 19, 23, 22, 29, 43, 107)
       (20, 19, 24, 23, 30, 48, 117)
       (20, 19, 24, 24, 31, 63, 128)
       (20, 20, 25, 25, 32, 68, 140)
       (21, 20, 25, 26, 34, 83, 153)
       (21, 20, 26, 27, 35, 88, 167)
       (21, 21, 26, 28, 36, 103, 182)
       (22, 21, 27, 29, 37, 108, 198)
       (22, 22, 27, 31, 38, 123, 200)
       (22, 22, 28, 32, 39, 128, 218)
       (23, 22, 28, 33, 41, 143, 237)
       (23, 23, 29, 34, 42, 148, 257)
       (23, 23, 30, 35, 43, 153, 278)
       (24, 24, 30, 36, 44, 168, 300)
       (24, 24, 31, 38, 46, 173, 308)
       (24, 24, 31, 39, 47, 189, 332)
       (25, 25, 32, 40, 48, 196, 357)
       (25, 25, 33, 41, 50, 204, 383)
       (25, 26, 33, 43, 51, 223, 395)
       (26, 26, 34, 44, 52, 233, 423)
       (26, 27, 35, 45, 54, 244, 452)
       (27, 27, 35, 47, 55, 266, 467) |]

  /// The sourced sheet behind one party member, independent of level: the base
  /// row per level, the best-in-slot set, and the class's own attack-power
  /// conversion. At level 24 this reproduces the level-24 numbers exactly; below
  /// it, the party wears only the pieces it can wear and stands in its own base
  /// row, which is what makes the same dungeon harder at a lower level.
  type Sheet =
    { Base: (int * int * int * int * int * int * int) array
      Gear: GearPiece list
      ApPerStrength: int
      ApPerAgility: int
      /// Rage and energy are fixed pools; mana is computed from Intellect.
      FixedPool: int option }

  /// The five classes a run starts with, in marching order. A roster is names
  /// rather than a type because it is what the class-select screen stores and
  /// what a saved run replays.
  let defaultRoster = [ "Warrior"; "Priest"; "Rogue"; "Mage"; "Druid" ]

  /// The party-wide passives, one per class present (Q2a), each at the highest
  /// rank a level-24 caster can train. Computed from the roster, so a party
  /// without a Warrior gets no Battle Shout.
  let partyBuffsFor (roster: string list) : PartyBuffKind list =
    [ if List.contains "Mage" roster then ArcaneIntellect
      if List.contains "Priest" roster then PowerWordFortitude
      if List.contains "Druid" roster then MarkOfTheWild
      if List.contains "Warrior" roster then BattleShout
      if List.contains "Paladin" roster then BlessingOfMight
      if List.contains "Paladin" roster then DevotionAura ]

  /// The default party's passives.
  let partyBuffs: PartyBuffKind list = partyBuffsFor defaultRoster

  /// Apply the party passives. Armour and attack power come back separately:
  /// they are not one of the five stats, but they are just as real.
  let private buffed (buffs: PartyBuffKind list) (stats: Stats) =
    buffs
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
        | BattleShout -> acc, armor, ap + 55
        | BlessingOfMight -> acc, armor, ap + 55
        | DevotionAura -> acc, armor + 275, ap)
      (stats, 0, 0)

  /// Health or mana from an attribute: the first 20 points are worth 1 each and
  /// every point after that is worth `perPoint` (10 health, 15 mana).
  let private pool (n: int) (perPoint: int) = min 20 n + (max 0 (n - 20)) * perPoint

  /// A party member's footing at one level: the stats its base row and whatever
  /// of the set it can wear add up to, the item armour, and whether any of it is
  /// a shield — which is what the shield abilities depend on.
  type Dressed =
    { Stats: Stats
      Armor: int
      BaseHealth: int
      BaseMana: int
      Shield: bool }

  /// What a party member is actually standing in at one level: its race+class base
  /// row for that level, plus every piece of the set it is high enough to wear.
  /// This is the whole point of the level knob — a level-18 Warrior has the
  /// level-18 row and none of the five pieces that need 24, Tower Shield first.
  let private dress (sheet: Sheet) (level: int) : Dressed =
    let str, agi, sta, intel, spi, baseHealth, baseMana =
      sheet.Base.[max 0 (min (sheet.Base.Length - 1) (level - 1))]

    let worn =
      sheet.Gear
      |> List.filter (fun g ->
        match g.ReqLevel with
        | Some req -> req <= level
        | None -> true)

    let add (acc: Stats) (g: GearPiece) =
      { Strength = acc.Strength + g.Stats.Strength
        Agility = acc.Agility + g.Stats.Agility
        Stamina = acc.Stamina + g.Stats.Stamina
        Intellect = acc.Intellect + g.Stats.Intellect
        Spirit = acc.Spirit + g.Stats.Spirit }

    { Stats =
        List.fold add { Strength = str; Agility = agi; Stamina = sta; Intellect = intel; Spirit = spi } worn
      Armor = worn |> List.sumBy (fun g -> g.Armor)
      BaseHealth = baseHealth
      BaseMana = baseMana
      Shield = worn |> List.exists (fun g -> g.Shield) }

  /// Derive combat values from a dressed character and its traits with Classic's
  /// real formulas: health and mana from Stamina and Intellect, 2 armour per
  /// Agility on top of item armour, and crit and dodge per Agility. The
  /// agility-to-crit rate is the one interpolated number here (~10 Agility per 1%
  /// at level 24); it is level-dependent in Classic and is not yet.
  let private derive (buffs: PartyBuffKind list) (sheet: Sheet) (d: Dressed) (traits: Traits) =
    let s, armorBp, attackPower = buffed buffs d.Stats

    {| MaxHealth = d.BaseHealth + pool s.Stamina 10
       MaxResource = defaultArg sheet.FixedPool (d.BaseMana + pool s.Intellect 15)
       RegenPerTick = partyRegen
       CritChanceBp = 500 + s.Agility * 10 + traits.CritBonusBp
       DodgeChanceBp = 500 + s.Agility * 5 + traits.DodgeBonusBp
       Armor = d.Armor + armorBp + s.Agility * 2 + traits.ArmorBonus
       BlockChanceBp = traits.BlockChanceBp
       BlockValue = traits.BlockValue
       AttackPower = s.Strength * sheet.ApPerStrength + s.Agility * sheet.ApPerAgility + attackPower |}

  /// A Party member at one level. The passives are applied to the stats before
  /// the derived values are computed.
  ///
  /// shortcut: only the Rogue trains a real rank ladder (below 24 he uses the
  /// rank he could train); the other four keep their level-24 ability ranks at
  /// every level, so a low-level caster hits harder than the real game allows.
  /// Upgrading means giving every ability a `Rank` ladder in this file.
  let heroStats
    (buffs: PartyBuffKind list)
    (level: int)
    (id: int)
    (name: string)
    (role: Role)
    (armorClass: ArmorClass)
    (traits: Traits)
    (sheet: Sheet)
    (attack: AutoAttack)
    (resource: ResourceKind)
    (threatBp: int)
    (abilities: Ability list)
    (pos: Pos)
    : Entity =
    let dressed = dress sheet level
    let d = derive buffs sheet dressed traits

    // Attack power adds damage per swing at AP/14 per second, scaled by the
    // weapon's own speed: 14 * 20 Ticks in the denominator.
    let meleeBonus = d.AttackPower * int attack.Ticks / (14 * 20)

    { Id = EntityId id
      Dormant = false
      Name = name
      Level = level
      CreatureType = CreatureType.Humanoid
      OffHand = false
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
      ResourceKind = resource
      ComboPoints = 0
      Abilities = abilities
      AutoAttack = Some attack
      SwingTicksLeft = attack.Ticks
      Threat = Map.empty
      Target = None
      ForcedTarget = None
      ThreatMultiplierBp = threatBp
      Stats = dressed.Stats
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
      HasCalledForHelp = false
      Owner = None
      ExpiresInTicks = None }

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

  /// Human Warrior: the mailed best-in-slot set (765 armour) plus the Tower
  /// Shield's 483, Shadowfang 29-55 at 2.70 s. Rage is a flat 100. Below level 24
  /// five pieces vanish with the shield, and Shield Bash, Shield Block and Shield
  /// Wall go with it.
  ///
  /// Mail, not plate: a Warrior does not train plate until 40.
  let private warrior (buffs: PartyBuffKind list) (level: int) (id: int) =
    let sheet =
      { Base = humanWarriorBase
        Gear =
          [ gear "Sparkleshell Mantle" (Some 24) 161 (0, 4, 10, 0, 0)
            gear "Sentry Cloak" (Some 19) 21 (0, 4, 5, 0, 0)
            gear "Blackened Defias Armor" (Some 19) 92 (4, 3, 11, 0, 0)
            gear "Beetle Clasps" None 83 (0, 2, 5, 0, 0)
            gear "Slayer's Gloves" (Some 24) 122 (0, 0, 6, 0, 5)
            gear "Chausses of Westfall" None 173 (11, 0, 5, 0, 0)
            gear "Slayer's Slippers" (Some 24) 134 (6, 0, 0, 0, 4)
            gear "Thunderbrow Ring" (Some 24) 0 (8, 3, 0, 0, 0)
            gear "Shadowfang" (Some 19) 0 (0, 0, 0, 0, 0)
            shieldPiece "Tower Shield" (Some 24) 483 (0, 0, 0, 0, 0) ]
        ApPerStrength = 2
        ApPerAgility = 0
        FixedPool = Some 100 }

    let shielded = (dress sheet level).Shield
    let traits = if shielded then warriorTraits else warriorTraits |> fun t -> { t with BlockChanceBp = 0; BlockValue = 0 }

    let abilities =
      [ taunt; heroicStrike; sunderArmor ]
      @ (if shielded then [ shieldBash; shieldBlock; shieldWall ] else [])
      @ [ charge; disarm; execute ]

    heroStats
      buffs
      level
      id
      "Warrior"
      Tank
      (if level >= 40 then Plate else Chainmail)
      traits
      sheet
      { Min = 29; Max = 55; Ticks = secTicks 2.7; Range = 1 }
      Mana
      tankStanceBp
      abilities
      { X = 1; Y = 5 }

  /// Human Priest: 253 armour from the set, Staff of the Blessed Seer 47-71 at
  /// 3.00 s with +24 healing, which is folded into the heals above.
  let private priest (buffs: PartyBuffKind list) (level: int) (id: int) =
    heroStats
      buffs
      level
      id
      "Priest"
      Healer
      Cloth
      Traits.none
      { Base = humanPriestBase
        Gear =
          [ gear "Green Tinted Goggles" None 35 (0, 0, 8, 0, 7)
            gear "Talbar Mantle" None 30 (0, 0, 3, 6, 0)
            gear "Cape of the Brotherhood" None 21 (0, 6, 3, 0, 0)
            gear "Tree Bark Jacket" (Some 19) 42 (0, 0, 6, 10, 0)
            gear "Mindthrust Bracers" (Some 17) 17 (0, 0, -5, 9, 0)
            gear "Magefist Gloves" (Some 15) 23 (0, 0, 3, 5, 4)
            gear "Bright Belt" None 21 (0, 0, 4, 4, 0)
            gear "Darkweave Breeches" (Some 17) 35 (0, 0, 4, 7, 6)
            gear "Spidersilk Boots" (Some 20) 29 (0, 0, 4, 4, 7)
            gear "Plains Ring" (Some 24) 0 (0, 0, 8, 3, 0)
            gear "Seal of Wrynn" None 0 (3, 3, 4, 4, 3)
            gear "Staff of the Blessed Seer" (Some 18) 0 (0, 0, 2, 0, 3) ]
        ApPerStrength = 1
        ApPerAgility = 0
        FixedPool = None }
      { Min = 47; Max = 71; Ticks = secTicks 3.0; Range = 1 }
      Mana
      partyThreatBp
      // Lesser Heal is the downrank: smaller, and the heal a Priest actually
      // reaches for on a small hole because nothing is wasted as overheal.
      [ lesserHeal; heal; renew; smite; shadowWordPain ]
      { X = 1; Y = 6 }

  /// Night Elf Rogue: 221 armour from the set plus 200 from Agility, Assassin's
  /// Blade 20-39 at 1.90 s, 100 energy. The one class whose ability ranks are
  /// levelled, so his damage falls off properly below 24.
  let private rogue (buffs: PartyBuffKind list) (level: int) (id: int) =
    heroStats
      buffs
      level
      id
      "Rogue"
      Dps
      Leather
      Traits.none
      { Base = nightElfRogueBase
        Gear =
          [ gear "Sentinel's Medallion" (Some 18) 0 (0, 6, 2, 0, 0)
            gear "Dark Leather Shoulders" (Some 23) 68 (0, 7, 0, 0, 0)
            gear "Sentry Cloak" (Some 19) 21 (0, 4, 5, 0, 0)
            gear "Tunic of Westfall" None 92 (0, 11, 5, 0, 0)
            gear "Scaled Leather Bracers" (Some 24) 40 (0, 0, 0, 0, 0)
            gear "Wrangler's Belt" (Some 19) 0 (0, 0, 0, 0, 0)
            gear "Protector's Band" (Some 18) 0 (4, 4, 2, 0, 0)
            gear "Assassin's Blade" (Some 19) 0 (3, 4, 0, 0, 0)
            gear "Assassin's Blade (off hand)" (Some 19) 0 (3, 4, 0, 0, 0) ]
        ApPerStrength = 1
        ApPerAgility = 1
        FixedPool = Some 100 }
      { Min = 20; Max = 39; Ticks = secTicks 1.9; Range = 1 }
      Energy
      partyThreatBp
      (AbilityDef.kitAt level [ sinisterStrike; backstab; eviscerate; gouge; kick ])
      { X = 1; Y = 7 }
    |> fun r ->
      // A second Assassin's Blade in the off hand: dual wield is learned at 10.
      { r with OffHand = true }

  /// Gnome Mage: 282 armour from the set, and a Gravestone Scepter wand (30-57 at
  /// 1.50 s) as the auto-attack, which is why the Mage has no attack-power
  /// contribution. Four pieces need 24, so a level-20 Mage loses Robes and Belt
  /// of Arugal, Plains Ring and the Rod of the Sleepwalker.
  let private mage (buffs: PartyBuffKind list) (level: int) (id: int) =
    heroStats
      buffs
      level
      id
      "Mage"
      Dps
      Cloth
      Traits.none
      { Base = gnomeMageBase
        Gear =
          [ gear "Spellpower Goggles Xtreme" None 46 (0, 0, 0, 0, 0)
            gear "Sentinel's Medallion" (Some 9) 0 (0, 2, 2, 0, 0)
            gear "Berylline Pads" None 39 (0, 0, 5, 10, 6)
            gear "Engineer's Cloak" None 20 (0, 0, 5, 2, 0)
            gear "Robes of Arugal" (Some 24) 46 (0, 3, 5, 9, 10)
            gear "Mindthrust Bracers" (Some 17) 17 (0, 0, -5, 9, 0)
            gear "Magefist Gloves" (Some 15) 23 (0, 0, 3, 5, 4)
            gear "Belt of Arugal" (Some 24) 26 (0, 2, 0, 10, 3)
            gear "Azure Silk Pants" (Some 23) 36 (0, 0, 0, 6, 0)
            gear "Spidersilk Boots" (Some 20) 29 (0, 0, 4, 4, 7)
            gear "Plains Ring" (Some 24) 0 (0, 0, 8, 3, 0)
            gear "Black Pearl Ring" (Some 17) 0 (0, 0, 2, 2, 6)
            gear "Rod of the Sleepwalker" (Some 24) 0 (0, 0, 0, 11, 10)
            gear "Gravestone Scepter" None 0 (0, 0, 0, 0, 1) ]
        ApPerStrength = 0
        ApPerAgility = 0
        FixedPool = None }
      { Min = 30; Max = 57; Ticks = secTicks 1.5; Range = 8 }
      Mana
      partyThreatBp
      [ fireball; frostbolt; arcaneMissiles; arcaneExplosion; blizzard; polymorph; counterspell ]
      { X = 2; Y = 6 }

  /// Night Elf Druid: 238 armour from the set, the same staff as the Priest. Four
  /// pieces need 24.
  let private druid (buffs: PartyBuffKind list) (level: int) (id: int) =
    heroStats
      buffs
      level
      id
      "Druid"
      Healer
      Leather
      Traits.none
      { Base = nightElfDruidBase
        Gear =
          [ gear "Azure Silk Hood" (Some 24) 33 (0, 0, 0, 0, 0)
            gear "Silver-thread Amice" (Some 24) 32 (0, 0, 2, 7, 0)
            gear "Watcher's Cape" (Some 19) 19 (0, 0, 0, 0, 0)
            gear "Robes of Arugal" (Some 24) 46 (0, 3, 5, 9, 10)
            gear "Silver-thread Cuffs" (Some 22) 18 (0, 0, 2, 5, 0)
            gear "Belt of Arugal" (Some 24) 26 (0, 2, 0, 10, 3)
            gear "Darkweave Breeches" (Some 17) 35 (0, 0, 4, 7, 6)
            gear "Spidersilk Boots" (Some 20) 29 (0, 0, 4, 4, 7)
            gear "Lavishly Jeweled Ring" (Some 17) 0 (0, 2, 0, 6, 0)
            gear "Staff of the Blessed Seer" (Some 18) 0 (0, 0, 2, 0, 3) ]
        ApPerStrength = 1
        ApPerAgility = 0
        FixedPool = None }
      { Min = 47; Max = 71; Ticks = secTicks 3.0; Range = 1 }
      Mana
      partyThreatBp
      [ healingTouchR4; regrowth; rejuvenation; entanglingRoots; wrath; moonfire ]
      { X = 2; Y = 7 }

  /// Human Paladin: mail at 24 — plate is trained at 40 — with Verigan's Fist
  /// 65-99 at 3.20 s. Five of the researched BiS slots have no verifiable item at
  /// level 24 (head, back, waist, feet, chest), so his armour is the lowest of
  /// the nine by a wide margin: 639 with the aura up, against the Warrior's 748
  /// without one. Strength is worth 2 attack power a point, so the two Strength
  /// pieces and the mace carry his damage.
  ///
  /// He is a Healer here because that is the role he fills in a party that
  /// already has a Warrior; the roster can put him anywhere.
  let private paladin (buffs: PartyBuffKind list) (level: int) (id: int) =
    heroStats
      buffs
      level
      id
      "Paladin"
      Healer
      Chainmail
      Traits.none
      { Base = humanPaladinBase
        Gear =
          [ gear "Sparkleshell Mantle" (Some 24) 161 (0, 4, 10, 0, 0)
            gear "Yorgen Bracers" (Some 22) 91 (6, 3, 3, 0, 0)
            gear "Algae Fists" (Some 23) 132 (10, 0, 4, 0, 0)
            gear "Chausses of Westfall" None 173 (11, 0, 5, 0, 0)
            gear "Lorekeeper's Ring" (Some 18) 0 (0, 0, 2, 0, 0)
            gear "Plains Ring" (Some 24) 0 (0, 0, 8, 3, 0)
            gear "Verigan's Fist" None 0 (0, 0, 7, 6, 12) ]
        ApPerStrength = 2
        ApPerAgility = 0
        FixedPool = None }
      { Min = 65; Max = 99; Ticks = secTicks 3.2; Range = 1 }
      Mana
      partyThreatBp
      [ flashOfLight; holyLight; judgement; paladinHammerOfJustice; divineProtection ]
      { X = 1; Y = 8 }

  /// Gnome Warlock: cloth, the same washed-out set as the Mage — five of the
  /// researched slots have no verifiable item at 24 — with the Gravestone
  /// Scepter's 29 DPS wand in hand. Stamina 57 and Intellect 82 before the
  /// party's passives, which is the lowest armour of the nine and the largest
  /// mana pool.
  ///
  /// shortcut: no demon. `Summon` and `SummonSpec` exist and the Imp/Voidwalker
  /// are level-24 summons, but the Summon path has no caller yet, so adding a
  /// pet here would be its first test. Life Tap has no effect in the vocabulary
  /// (nothing moves health into mana) and is left out for the same reason.
  let private warlock (buffs: PartyBuffKind list) (level: int) (id: int) =
    heroStats
      buffs
      level
      id
      "Warlock"
      Dps
      Cloth
      Traits.none
      { Base = gnomeWarlockBase
        Gear =
          [ gear "Green Tinted Goggles" None 35 (0, 0, 8, 0, 7)
            gear "Talbar Mantle" None 30 (0, 0, 3, 6, 0)
            gear "Sentry Cloak" (Some 19) 21 (0, 4, 5, 0, 0)
            gear "Robes of Arugal" (Some 24) 46 (0, 3, 5, 9, 10)
            gear "Mindthrust Bracers" (Some 17) 17 (0, 0, -5, 9, 0)
            gear "Hands of Darkness" (Some 24) 26 (0, 0, 0, 0, 5)
            gear "Plains Ring" (Some 24) 0 (0, 0, 8, 3, 0)
            gear "Staff of the Shade" (Some 22) 0 (0, 0, 0, 0, 0)
            gear "Gravestone Scepter" None 0 (0, 0, 0, 0, 1) ]
        ApPerStrength = 0
        ApPerAgility = 0
        FixedPool = None }
      { Min = 30; Max = 57; Ticks = secTicks 1.5; Range = 8 }
      Mana
      partyThreatBp
      [ shadowBolt; corruption; curseOfAgony; warlockImmolate; fear ]
      { X = 3; Y = 6 }

  /// Orc Shaman: the Druid's verified leather-and-cloth set, which is as far as
  /// the ≤24 pieces are sourced — shaman trains mail at 40, so nothing better is
  /// available in the bracket. Strength 43 makes him the best melee of the
  /// casters and Rockbiter rather than Windfury is what a 24 has.
  ///
  /// shortcut: no totems. `Summon`/`SummonSpec` have no caller in the sim yet, and
  /// a totem would be that path's first test. Lightning Shield and Rockbiter are
  /// also left out: nothing in the vocabulary retaliates or imbues a weapon.
  let private shaman (buffs: PartyBuffKind list) (level: int) (id: int) =
    heroStats
      buffs
      level
      id
      "Shaman"
      Dps
      Leather
      Traits.none
      { Base = orcShamanBase
        Gear =
          [ gear "Azure Silk Hood" (Some 24) 33 (0, 0, 0, 0, 0)
            gear "Silver-thread Amice" (Some 24) 32 (0, 0, 2, 7, 0)
            gear "Watcher's Cape" (Some 19) 19 (0, 0, 0, 0, 0)
            gear "Robes of Arugal" (Some 24) 46 (0, 3, 5, 9, 10)
            gear "Silver-thread Cuffs" (Some 22) 18 (0, 0, 2, 5, 0)
            gear "Belt of Arugal" (Some 24) 26 (0, 2, 0, 10, 3)
            gear "Darkweave Breeches" (Some 17) 35 (0, 0, 4, 7, 6)
            gear "Spidersilk Boots" (Some 20) 29 (0, 0, 4, 4, 7)
            gear "Lavishly Jeweled Ring" (Some 17) 0 (0, 2, 0, 6, 0)
            gear "Staff of the Blessed Seer" (Some 18) 0 (0, 0, 2, 0, 3) ]
        ApPerStrength = 1
        ApPerAgility = 0
        FixedPool = None }
      { Min = 47; Max = 71; Ticks = secTicks 3.0; Range = 1 }
      Mana
      partyThreatBp
      [ shamanLightningBolt; earthShock; flameShock; shamanHealingWave ]
      { X = 3; Y = 7 }

  /// Night Elf Hunter: level-24 leather only — hunters too train mail at 40 — on
  /// the researched agility loadout. Agility 137 is the highest in the roster, so
  /// this is the class whose auto-attack carries the party's ranged damage.
  ///
  /// The damage range is the researched per-shot total (21-39 bow, +6.3 Sharp
  /// Arrow, +38.8 from 302 ranged attack power) rather than a bare weapon, because
  /// vanilla's ranged AP formula is `level x 2 + agility x 2 - 20` and the level
  /// term and the offset cannot be expressed through `ApPerAgility`.
  /// shortcut: no pet. `Summon`/`SummonSpec` still have no caller, and a hunter
  /// without its pet loses Growl, Bite and Mend Pet, so the kit is the shots
  /// alone. Aspects, Hunter's Mark, Multi-Shot, Concussive Shot and Wing Clip are
  /// also out: nothing in the vocabulary carries a ranged-attack-power buff, a
  /// multi-target shot, or a movement slow — `Effect` has stun, root, sleep and
  /// disarm, but no snare of any strength.
  let private hunter (buffs: PartyBuffKind list) (level: int) (id: int) =
    heroStats
      buffs
      level
      id
      "Hunter"
      Dps
      Leather
      Traits.none
      { Base = nightElfHunterBase
        Gear =
          [ gear "Scout's Medallion" (Some 18) 0 (0, 6, 2, 0, 0)
            gear "Serpent's Shoulders" (Some 18) 68 (0, 9, 0, 0, 0)
            gear "Fenrus' Hide" (Some 20) 22 (0, 6, 5, 0, 0)
            gear "Bristlebark Blouse" (Some 23) 90 (0, 7, 6, 0, 0)
            gear "Bands of Serra'kis" (Some 22) 43 (4, 6, 2, 0, 0)
            gear "Naga Battle Gloves" (Some 22) 61 (4, 7, 4, 0, 0)
            gear "Deviate Scale Belt" (Some 18) 51 (0, 5, 6, 0, 0)
            gear "Leggings of the Fang" (Some 18) 79 (5, 9, 4, 0, 0)
            gear "Feet of the Lynx" (Some 19) 63 (3, 8, 0, 0, 0)
            gear "Ring of Precision" (Some 20) 0 (0, 6, 0, 0, 3)
            gear "Protector's Band" (Some 18) 0 (4, 4, 2, 0, 0) ]
        ApPerStrength = 0
        ApPerAgility = 0
        FixedPool = None }
      { Min = 66; Max = 84; Ticks = secTicks 1.8; Range = 8 }
      Mana
      partyThreatBp
      [ arcaneShot; serpentSting; raptorStrike ]
      { X = 3; Y = 8 }

  /// Every class the sim can build, by the name a roster entry uses. None means
  /// that class has no kit yet, which is why a roster is filtered rather than
  /// trusted.
  let private heroOf (buffs: PartyBuffKind list) (level: int) (id: int) (className: string) : Entity option =
    match className with
    | "Warrior" -> Some(warrior buffs level id)
    | "Priest" -> Some(priest buffs level id)
    | "Rogue" -> Some(rogue buffs level id)
    | "Mage" -> Some(mage buffs level id)
    | "Druid" -> Some(druid buffs level id)
    | "Paladin" -> Some(paladin buffs level id)
    | "Warlock" -> Some(warlock buffs level id)
    | "Shaman" -> Some(shaman buffs level id)
    | "Hunter" -> Some(hunter buffs level id)
    | _ -> None

  /// A party by class name, in marching order: the Tank first, then the healer
  /// and the damage. Every world builds its Party from this one function, so a
  /// change of roster is one edit rather than four, and the Tank always takes the
  /// front tile the formation gives it. The passives are the roster's, so the
  /// roster has to be decided before any member is built.
  ///
  /// Ids come from the slot, not the class: a class that arrived later would
  /// otherwise need a number the mobs are already using, and every `tryEntity` on
  /// that id would find whichever came first in the list. A party is five seats —
  /// the roster has nine classes so any five can be chosen, and the rest of the
  /// roster is not seated.
  let partyOf (roster: string list) (level: int) : Entity list =
    let buffs = partyBuffsFor roster
    roster |> List.truncate 5 |> List.indexed |> List.choose (fun (slot, className) -> heroOf buffs level (slot + 1) className)

  /// Replace a world's party with a different roster, on the tiles the existing
  /// party already holds. A class outside `defaultRoster` can only be measured
  /// through this, because a dungeon builder always seats the default five. The
  /// party is rebuilt rather than renamed, so the new roster's passives are
  /// applied to the members that get them.
  let withRoster (roster: string list) (w: World) : World =
    let existing = w.Entities |> List.filter (fun e -> e.Faction = Party)

    match existing with
    | [] -> w
    | first :: _ ->
      let tiles = existing |> List.map (fun e -> e.Pos)
      let party = partyOf roster first.Level |> List.truncate (List.length tiles)
      let seated = List.map2 (fun (e: Entity) p -> { e with Pos = p }) party tiles

      { w with
          Entities = seated @ (w.Entities |> List.filter (fun e -> e.Faction <> Party))
          PartyBuffs = partyBuffsFor roster }

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

  /// The published 1.12 melee rows are the client database's base damage, and at
  /// face value a level-19 elite hits a level-24 tank for about thirty after
  /// armour, which turns the dungeon into a walkthrough. The boss rows in this
  /// file were already built at roughly 2.2x the published figure (Anacondra
  /// 2.1x, Mutanus 2.3x), so every creature added after them uses that factor.
  /// Health and armour are taken verbatim.
  let private wcMeleeScale = 22

  /// One Wailing Caverns creature from its researched row:
  /// `wc id name kind level hp (min, max) swing armour abilities callHelp pos`.
  /// Melee is scaled by `wcMeleeScale` in basis points.
  let private wc
    (id: int)
    (name: string)
    (kind: CreatureType)
    (level: int)
    (hp: int)
    (dmgMin: int, dmgMax: int)
    (swing: float)
    (armour: int)
    (abilities: Ability list)
    (callForHelpBelowPct: int option)
    (pos: Pos)
    : Entity =
    { mob
        id
        name
        kind
        level
        hp
        0
        (Some
          { Min = dmgMin * wcMeleeScale / 10
            Max = dmgMax * wcMeleeScale / 10
            Ticks = secTicks swing
            Range = 1 })
        abilities
        callForHelpBelowPct
        pos with
        Armor = armour }

  /// Druid of the Fang, the trash caster: 1280 health (1235-1332), 145-172 a
  /// swing, 780 armour. Serpent Form at 30% health replaces its casts with a
  /// harder melee — which the sim models as a bounded shapeshift.
  let private druidOfTheFang id pos =
    { mob id
        "Druid of the Fang"
        CreatureType.Humanoid
        20 // NPC 3840, level 19-20
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
        CreatureType.Beast
        19 // NPC 3636, level 18-19
        1310
        0
        (Some { Min = 70; Max = 82; Ticks = secTicks 2.0; Range = 1 })
        []
        (Some 20)
        pos with
        MoveTicksPerTile = secTicks 0.3
        Armor = 800 }

  /// Deviate Moccasin (5762), the awakening ritual's first wave: three of them,
  /// level 20-21 elite.
  let private deviateMoccasin id pos =
    wc id "Deviate Moccasin" CreatureType.Beast 21 1563 (55, 66) 2.0 854 [] (Some 20) pos

  /// Deviate Guardian (3637): the same raptor as the Ravager without Thrash.
  let private deviateGuardian id pos =
    wc id "Deviate Guardian" CreatureType.Beast 19 1347 (47, 63) 2.0 826 [] (Some 20) pos

  /// Deviate Adder (5048): a passive poison proc on melee, which the effect
  /// vocabulary has no case for.
  let private deviateAdder id pos =
    wc id "Deviate Adder" CreatureType.Beast 19 1347 (47, 63) 2.0 826 [] (Some 20) pos

  /// Deviate Viper (5755) and its Localized Toxin, likewise unmodelled.
  let private deviateViper id pos =
    wc id "Deviate Viper" CreatureType.Beast 20 1210 (50, 66) 2.0 861 [] (Some 20) pos

  /// Deviate Python (8886): normal, not elite — 180 health. Cobrahn keeps three.
  let private deviatePython id pos =
    wc id "Deviate Python" CreatureType.Beast 19 180 (28, 37) 2.0 826 [] (Some 20) pos

  /// Deviate Crocolisk (5053): normal, 449 health. Tendon Rip is a snare.
  let private deviateCrocolisk id pos =
    wc id "Deviate Crocolisk" CreatureType.Beast 19 449 (28, 37) 2.0 826 [] (Some 20) pos

  /// Deviate Dreadfang (5056): elite wind serpent with Terrify.
  let private deviateDreadfang id pos =
    wc id "Deviate Dreadfang" CreatureType.Beast 21 1563 (52, 69) 2.0 897 [] (Some 20) pos

  /// Deviate Venomwing (5756): elite wind serpent, ranged Toxic Spit.
  let private deviateVenomwing id pos =
    wc id "Deviate Venomwing" CreatureType.Beast 21 1563 (52, 69) 2.0 897 [] (Some 20) pos

  /// Deviate Lasher (5055): normal, 135 health — the dungeon's filler trash, and
  /// the only thing in here that dies to a single Arcane Explosion.
  let private deviateLasher id pos =
    wc id "Deviate Lasher" CreatureType.Elemental 20 135 (32, 43) 2.0 819 [] (Some 20) pos

  /// Deviate Shambler (5761): casts Wild Regeneration under half health.
  let private deviateShambler id pos =
    wc id "Deviate Shambler" CreatureType.Elemental 20 1299 (47, 62) 2.0 712 [] (Some 20) pos

  /// Evolving Ectoplasm (3640): the ooze that splits. The clone is not modelled,
  /// so this is the ooze itself and nothing more.
  let private evolvingEctoplasm id pos =
    wc id "Evolving Ectoplasm" CreatureType.Undead 18 1251 (45, 59) 2.0 791 [] (Some 20) pos

  /// Nightmare Ectoplasm (5763): normal, 531 health, and rooted against CC. The
  /// second wave of the awakening ritual.
  let private nightmareEctoplasm id pos =
    wc id "Nightmare Ectoplasm" CreatureType.Undead 21 531 (31, 41) 2.0 854 [] (Some 20) pos

  /// Kresh (3653), the river rare: armour 4,149, which is five times every other
  /// level-20 creature and the one trash pull a level-24 party cannot burst down.
  let private kresh id pos =
    wc id "Kresh" CreatureType.Beast 20 1452 (50, 66) 2.0 4149 [] (Some 20) pos

  /// Skum (3674), the optional crag boss: Chained Bolt is unmodelled.
  let private skum id pos =
    wc id "Skum" CreatureType.Beast 21 3908 (61, 81) 2.0 854 [] (Some 20) pos

  /// Verdan the Everliving (5775): 4,168 health, a 3.5 s swing for 247-328, and
  /// Grasping Vines — a knockdown plus a ten-second immobilise the vocabulary has
  /// no case for. Elemental, so he cannot be polymorphed.
  let private verdan id pos =
    wc id "Verdan the Everliving" CreatureType.Elemental 21 4168 (247, 328) 3.5 854 [] (Some 20) pos

  /// Lady Anacondra, level 20 elite: 2214 health, 154-183 a swing. She also
  /// carries a Thorns aura in Classic; a reflect-damage effect is not modelled.
  let private ladyAnacondra id pos =
    { mob id
        "Lady Anacondra"
        CreatureType.Humanoid
        20
        2214
        600
        (Some { Min = 154; Max = 183; Ticks = secTicks 2.0; Range = 1 })
        [ lightningBolt; healingTouch; fangSleep ]
        None
        pos with
        Armor = 712 }

  /// The four Fanglords share the Druid of the Fang kit — their differences are
  /// health, swing and (for Serpentis) a rank-6 heal. Cobrahn adds his Serpent
  /// Form, Pythas his Thunderclap.
  let private lordSerpentis id pos =
    { mob id
        "Lord Serpentis"
        CreatureType.Humanoid
        21
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
        CreatureType.Humanoid
        20
        2279
        600
        (Some { Min = 145; Max = 172; Ticks = secTicks 2.0; Range = 1 })
        [ lightningBolt; healingTouch; serpentFormCobrahn ]
        None
        pos with
        Armor = 712 }

  let private lordPythas id pos =
    { mob id
        "Lord Pythas"
        CreatureType.Humanoid
        21
        2409
        600
        (Some { Min = 154; Max = 183; Ticks = secTicks 2.0; Range = 1 })
        [ thunderclap; lightningBolt; healingTouch; fangSleep ]
        None
        pos with
        Armor = 712 }

  /// Naralex, level 25 and not an elite: 1240 health, 47-56 a swing. He is not a
  /// fight and never becomes one: he sleeps through the ritual on Dreamer's Rock
  /// and leaves alive when Mutanus dies. He is Dormant from the first tick and
  /// nothing ever wakes him, so he is scenery that renders and cannot be
  /// targeted — the dungeon's ending is Mutanus, not him.
  let private naralex id pos =
    { mob id
        "Naralex"
        CreatureType.Humanoid
        25
        1240
        800
        (Some { Min = 47; Max = 56; Ticks = secTicks 2.0; Range = 1 })
        [ lightningBolt; healingTouch; druidsSlumber ]
        None
        pos with
        Armor = 960
        Dormant = true }

  /// Mutanus the Devourer, level 22 elite: 4555 health, 179-213 a swing. No
  /// weapon kit at all — everything he does is a spell: Thundercrack (94-107
  /// area Nature plus a 2.5 s stun), Terrify (a 4 s fear) and Naralex's Nightmare
  /// (a 15 s sleep on one of us). He is also the one creature here with no
  /// published armour (fitted, 40/level).
  ///
  /// He starts Dormant: in the real instance he is not in the dungeon at all
  /// until the awakening ritual reaches its third wave, and the quest chain is
  /// what stands in for that.
  let private mutanus id pos =
    { mob id
        "Mutanus the Devourer"
        CreatureType.Humanoid
        22
        4555
        0
        (Some { Min = 179; Max = 213; Ticks = secTicks 2.0; Range = 1 })
        [ thundercrack; terrify; naralexsNightmare ]
        None
        pos with
        Armor = 880
        Dormant = true }

  /// Build the Screaming Gully encounter. The only randomness at generation is
  /// which of Anacondra's four positions she takes, drawn from the Generation
  /// stream so it cannot perturb combat rolls.
  let gullyAt (level: int) (seed: uint64) : World =
    let rng0 = Rng.streamsOf seed
    let pick, rng = Rng.drawGeneration 0 (List.length anacondraSpawns - 1) rng0
    let spawn = List.item pick anacondraSpawns

    // Her guards stand with her; the offsets land on floor for all four spawns.
    let guardA = Pos.move 1 0 spawn
    let guardB = Pos.move 2 0 spawn

    let entities =
      // The warrior leads, in front of the party and facing the Fang.
      let spots =
        [ { X = 3; Y = 6 }; { X = 1; Y = 6 }; { X = 2; Y = 6 }; { X = 1; Y = 5 }; { X = 1; Y = 7 } ]

      let creatures =
        [ druidOfTheFang 6 { X = 7; Y = 6 }
          raptor 7 { X = 9; Y = 3 }
          raptor 9 { X = 9; Y = 9 }
          ladyAnacondra 8 spawn
          druidOfTheFang 10 guardA
          druidOfTheFang 11 guardB ]

      List.map2 (fun (e: Entity) p -> { e with Pos = p }) (partyOf defaultRoster level) spots @ creatures

    { Tick = ticks 0
      Grid = Grid.ofRows gullyMap
      Entities = entities
      Pending = []
      Rng = rng
      PartyBuffs = partyBuffs
      Quests = []
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
  let dungeonAt (level: int) (seed: uint64) : World =
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
      List.map2 (fun (e: Entity) p -> { e with Pos = p }) (partyOf defaultRoster level) partyTiles

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
      Quests = []
      Log = [] }

  /// Wailing Caverns' objectives, and the script that stands in for the
  /// Disciple's awakening ritual. The four Fanglords are the gate: in the real
  /// instance their deaths flip the Disciple's dialogue, which is a boss-death
  /// check and not a quest, so the quest here does the same job. Finishing it
  /// calls up the ambush at Dreamer's Rock; clearing the ambush brings Mutanus up
  /// out of the water; killing him is the end of the dungeon and wakes Naralex.
  let private wailingCavernsQuests =
    let objective name targets required wakes lines =
      { Quest.create name targets required with
          Wakes = wakes
          Lines = lines }

    [ objective
        "Leaders of the Fang"
        [ "Lady Anacondra"; "Lord Cobrahn"; "Lord Pythas"; "Lord Serpentis" ]
        4
        [ "Deviate Moccasin" ]
        [ "Disciple of Naralex: The Fanglords are dead! All hope is not lost."
          "Disciple of Naralex: When you are ready to defend me, say the word, and I shall lead the way to Naralex's chamber."
          "Disciple of Naralex: Within this circle of fire I must cast the spell to banish the spirits of the slain Fanglords." ]
      objective
        "Serpentine Cleansing"
        [ "Deviate Moccasin" ]
        3
        [ "Nightmare Ectoplasm" ]
        [ "Disciple of Naralex: The caverns have been purified. To Naralex's chamber we go!"
          "Disciple of Naralex: Protect me brave souls as I delve into the Emerald Dream to rescue Naralex..."
          "Naralex dreams up a horrendous vision. Something stirs beneath the murky waters." ]
      objective
        "The Awakening"
        [ "Nightmare Ectoplasm" ]
        8
        [ "Mutanus the Devourer" ]
        [ "Disciple of Naralex: Deal with this Nightmare Ectoplasm! I need to prepare to awake Naralex!" ]
      objective
        "Mutanus the Devourer"
        [ "Mutanus the Devourer" ]
        1
        []
        [ "Naralex: I AM AWAKE, AT LAST!"
          "Naralex: Ah, to be pulled from the dreaded nightmare! I thank you, my loyal Disciple, along with your brave companions."
          "Naralex: We must go and gather with the other Disciples. There is much work to be done before I can make another attempt to restore the Barrens. Farewell, brave souls!" ] ]

  /// The stitched Wailing Caverns: the seven extracted rooms joined into one
  /// continuous dungeon and populated from the lore table. Placement is by the
  /// room each fight belongs to, not by template-fitting a generated floorplan.
  let wailingCavernsAt (level: int) (seed: uint64) : World =
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
        (partyOf defaultRoster level)
        partyTiles

    for p in partyTiles do
      occupied.Add p |> ignore

    // The population follows `.pi/research/wc/mobs.md` §6: the boss placement is
    // the sourced part, the per-room trash counts are the defensible estimate
    // (the wiki publishes per-creature totals for the zone but not per room).
    let mobs =
      [ // Screaming Gully — Anacondra's cavern, the first big room.
        yield! place "the-screaming-gully" ladyAnacondra |> Option.toList
        for _ in 1..4 do
          yield! place "the-screaming-gully" raptor |> Option.toList
        for _ in 1..4 do
          yield! place "the-screaming-gully" deviateGuardian |> Option.toList
        for _ in 1..4 do
          yield! place "the-screaming-gully" druidOfTheFang |> Option.toList
        for _ in 1..2 do
          yield! place "the-screaming-gully" evolvingEctoplasm |> Option.toList
        // Winding Chasm — the long connecting passage, and the side chamber.
        for _ in 1..12 do
          yield! place "the-winding-chasm" deviateLasher |> Option.toList
        for _ in 1..6 do
          yield! place "the-winding-chasm" deviateShambler |> Option.toList
        for _ in 1..8 do
          yield! place "the-winding-chasm" druidOfTheFang |> Option.toList
        for _ in 1..4 do
          yield! place "the-winding-chasm" deviateCrocolisk |> Option.toList
        for _ in 1..3 do
          yield! place "the-winding-chasm" deviateDreadfang |> Option.toList
        // Pit of Fangs — the snake pit and its river: Cobrahn, Pythas, and Kresh.
        yield! place "the-pit-of-fangs" lordCobrahn |> Option.toList
        yield! place "the-pit-of-fangs" lordPythas |> Option.toList
        yield! place "the-pit-of-fangs" kresh |> Option.toList
        for _ in 1..8 do
          yield! place "the-pit-of-fangs" deviatePython |> Option.toList
        for _ in 1..5 do
          yield! place "the-pit-of-fangs" deviateAdder |> Option.toList
        for _ in 1..5 do
          yield! place "the-pit-of-fangs" deviateViper |> Option.toList
        for _ in 1..6 do
          yield! place "the-pit-of-fangs" deviateCrocolisk |> Option.toList
        for _ in 1..10 do
          yield! place "the-pit-of-fangs" druidOfTheFang |> Option.toList
        // Crag of the Everliving — Serpentis, Verdan, Skum and the most trash.
        yield! place "crag-of-the-everliving" lordSerpentis |> Option.toList
        yield! place "crag-of-the-everliving" verdan |> Option.toList
        yield! place "crag-of-the-everliving-south" skum |> Option.toList
        for _ in 1..20 do
          yield! place "crag-of-the-everliving" deviateLasher |> Option.toList
        for _ in 1..10 do
          yield! place "crag-of-the-everliving" deviateShambler |> Option.toList
        for _ in 1..6 do
          yield! place "crag-of-the-everliving" deviateVenomwing |> Option.toList
        for _ in 1..4 do
          yield! place "crag-of-the-everliving-south" deviateDreadfang |> Option.toList
        for _ in 1..12 do
          yield! place "crag-of-the-everliving-south" druidOfTheFang |> Option.toList
        // Dreamer's Rock — Naralex asleep, and the two waves the ritual calls up:
        // both stand here Dormant until their quest wakes them.
        yield! place "dreamers-rock" naralex |> Option.toList
        for _ in 1..3 do
          yield! place "dreamers-rock" (fun id pos -> { deviateMoccasin id pos with Dormant = true }) |> Option.toList
        for _ in 1..8 do
          yield! place "dreamers-rock" (fun id pos -> { nightmareEctoplasm id pos with Dormant = true }) |> Option.toList
        for _ in 1..6 do
          yield! place "dreamers-rock" evolvingEctoplasm |> Option.toList
        // Mutanus comes up out of the water on the last wave.
        yield! place "dreamers-rock-east" mutanus |> Option.toList ]

    { Tick = ticks 0
      Grid = grid
      Entities = party @ mobs
      Pending = []
      Rng = Rng.streamsOf seed
      PartyBuffs = partyBuffs
      Quests = wailingCavernsQuests
      Log = [] }

  // -- Scarlet Monastery ----------------------------------------------------
  //
  // Stats are the 1.12 creature rows from database.emberveil.org (level, health,
  // melee range, swing speed); Wowhead Classic does not publish creature armour,
  // so armour stays the fitted 40 a level used elsewhere in this file. Ability
  // numbers are the spell tooltips, and where a rank could not be pinned down the
  // figure is the class rank nearest the mob's level, said so in the comment.

  /// Cleave (845): weapon damage plus 5 to the target and its nearest ally.
  let private cleave =
    { Name = "Cleave"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 0
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ AreaDamage(1, 88, 118) ] }

  /// Herod's Whirlwind (15589): one weapon swing to everything in melee. The raw
  /// spell value is unreadable, so it is his own swing — 267-307 at 2.5 s.
  let private whirlwind =
    { Name = "Whirlwind"
      CastTicks = ticks 0
      CooldownTicks = secTicks 10.0
      ResourceCost = 0
      Interruptible = false
      Range = 1
      TargetKind = Foe
      Effects = [ AreaDamage(1, 267, 307) ] }

  /// Flame Spike (8814): 93-107 Fire, then 50 every 3 s for 9 s.
  let private flameSpike =
    { Name = "Flame Spike"
      CastTicks = secTicks 3.0
      CooldownTicks = secTicks 9.0
      ResourceCost = 120
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(93, 107); DamageOverTime(secTicks 9.0, 50) ] }

  /// Immolate (11665): 148 up front, then 51 every 3 s for 15 s.
  let private immolate =
    { Name = "Immolate"
      CastTicks = secTicks 2.0
      CooldownTicks = secTicks 12.0
      ResourceCost = 220
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(148, 148); DamageOverTime(secTicks 15.0, 51) ] }

  /// Hammer of Justice (853): instant, 3 s stun, one minute cooldown.
  let private hammerOfJustice =
    { Name = "Hammer of Justice"
      CastTicks = ticks 0
      CooldownTicks = secTicks 60.0
      ResourceCost = 30
      Interruptible = false
      Range = 3
      TargetKind = Foe
      Effects = [ Stun(secTicks 3.0) ] }

  /// A Scarlet caster's Fireball (9053 lineage): 3 s, 64 Fire at the level-20
  /// rank, scaled here to the level-37 rank the Wizards and Evokers cast.
  let private scarletFireball =
    { Name = "Fireball"
      CastTicks = secTicks 3.0
      CooldownTicks = secTicks 5.0
      ResourceCost = 140
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(139, 188) ] }

  /// Frostbolt, the level-37 rank: the snare is not modelled, only the damage.
  let private scarletFrostbolt =
    { Name = "Frostbolt"
      CastTicks = secTicks 3.0
      CooldownTicks = secTicks 5.0
      ResourceCost = 105
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(120, 155) ] }

  /// A Scarlet priest's Smite at the level-36-40 rank.
  let private scarletSmite =
    { Name = "Smite"
      CastTicks = secTicks 2.5
      CooldownTicks = secTicks 4.0
      ResourceCost = 95
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(130, 160) ] }

  /// Heal (6064): 3 s cast, 734-827. The Chaplains and Abbots cast a lower rank,
  /// which is the same shape for less.
  let private scarletHeal =
    { Name = "Heal"
      CastTicks = secTicks 3.0
      CooldownTicks = secTicks 8.0
      ResourceCost = 305
      Interruptible = true
      Range = 8
      TargetKind = Ally
      Effects = [ Heal(734, 827) ] }

  /// Renew (6075): instant heal over time, 12 a Tick for 15 s.
  let private scarletRenew =
    { Name = "Renew"
      CastTicks = ticks 0
      CooldownTicks = secTicks 8.0
      ResourceCost = 105
      Interruptible = false
      Range = 8
      TargetKind = Ally
      Effects = [ HealOverTime(secTicks 15.0, 12) ] }

  /// Mana Burn (8129): 191-204 mana drained, 0.5 damage a point. The drain is not
  /// modelled, only the damage it deals.
  let private manaBurn =
    { Name = "Mana Burn"
      CastTicks = secTicks 3.0
      CooldownTicks = secTicks 8.0
      ResourceCost = 95
      Interruptible = true
      Range = 8
      TargetKind = Foe
      Effects = [ SpellDamage(95, 102) ] }

  /// A Scarlet mob before it is given an id and a place to stand.
  type private ScarletMob =
    { Key: string
      Name: string
      Level: int
      Health: int
      Damage: int * int
      Swing: float
      /// Mana. A mob does not regenerate, so this is what stops a Scarlet healer:
      /// without a pool it heals for free forever and the fight cannot end.
      Mana: int
      Abilities: Ability list }

  /// One roster row: `sm key name level health (min, max) swing abilities`.
  let private sm key name level health damage swing abilities : ScarletMob =
    { Key = key
      Name = name
      Level = level
      Health = health
      Damage = damage
      Swing = swing
      Mana = 0
      Abilities = abilities }

  /// A caster: 40 mana a level, enough for four or five heals at these levels.
  let private smC key name level health damage swing abilities : ScarletMob =
    { sm key name level health damage swing abilities with Mana = 40 * level }

  /// Every creature of the four wings, one row each, straight from the 1.12
  /// database. The wing lists below say where each one is met.
  let private scarletRoster : ScarletMob list =
    [ // Graveyard
      sm "trainee" "Scarlet Trainee" 30 2175 (31, 41) 2.00 []
      smC "scryer" "Scarlet Scryer" 31 2550 (41, 54) 2.00 [ manaBurn; scarletFireball ]
      sm "sentry" "Scarlet Sentry" 31 3018 (74, 98) 2.00 [ cleave ]
      sm "torturer" "Scarlet Torturer" 31 3018 (87, 115) 2.00 [ cleave ]
      smC "spirit" "Unfettered Spirit" 32 1057 (43, 57) 2.00 [ scarletFrostbolt ]
      smC "phantasm" "Haunting Phantasm" 33 3330 (78, 104) 2.00 [ scarletSmite ]
      sm "vishas" "Interrogator Vishas" 32 5328 (90, 104) 2.00 []
      smC "thalnos" "Bloodmage Thalnos" 34 7736 (75, 99) 2.00 [ flameSpike; immolate ]
      // Library
      smC "adept" "Scarlet Adept" 34 2901 (44, 58) 2.00 [ scarletSmite; scarletRenew ]
      sm "gallant" "Scarlet Gallant" 34 2901 (75, 99) 2.00 [ cleave ]
      sm "hound" "Scarlet Tracking Hound" 34 3489 (60, 80) 1.50 []
      sm "monk" "Scarlet Monk" 36 3831 (108, 143) 1.80 [ cleave ]
      sm "soldier" "Scarlet Soldier" 36 3831 (85, 112) 2.00 [ cleave ]
      smC "azshir" "Azshir the Sleepless" 33 2784 (73, 96) 2.00 [ scarletFrostbolt ]
      sm "champion-fallen" "Fallen Champion" 33 3330 (78, 104) 2.00 [ cleave ]
      sm "ironspine" "Ironspine" 33 3330 (83, 99) 2.00 []
      sm "loksey" "Houndmaster Loksey" 34 7736 (114, 152) 2.00 [ cleave ]
      smC "doan" "Arcanist Doan" 37 8730 (69, 79) 2.00 [ scarletFireball; arcaneExplosion; polymorph ]
      // Armory
      sm "beastmaster" "Scarlet Beastmaster" 35 3660 (83, 110) 2.00 [ cleave ]
      smC "diviner" "Scarlet Diviner" 35 3027 (45, 60) 2.00 [ scarletFireball ]
      smC "chaplain" "Scarlet Chaplain" 36 3150 (79, 104) 2.00 [ scarletSmite; scarletHeal; scarletRenew ]
      smC "conjuror" "Scarlet Conjuror" 36 2805 (36, 49) 2.00 [ scarletFireball ]
      sm "guardsman" "Scarlet Guardsman" 37 4008 (193, 255) 2.80 [ cleave ]
      sm "protector" "Scarlet Protector" 37 3279 (81, 108) 2.00 []
      smC "evoker" "Scarlet Evoker" 37 2910 (37, 51) 2.00 [ scarletFireball ]
      sm "defender" "Scarlet Defender" 38 4185 (90, 119) 2.00 []
      smC "sorcerer" "Scarlet Sorcerer" 38 3012 (38, 51) 2.00 [ scarletFrostbolt; polymorph ]
      sm "myrmidon" "Scarlet Myrmidon" 38 4185 (90, 119) 1.70 [ cleave ]
      sm "herod" "Herod" 40 12192 (267, 307) 2.50 [ whirlwind; cleave ]
      // Cathedral
      smC "wizard" "Scarlet Wizard" 39 3640 (75, 100) 2.00 [ scarletFireball; polymorph ]
      sm "centurion" "Scarlet Centurion" 39 4377 (149, 198) 2.00 [ cleave ]
      sm "champion" "Scarlet Champion" 40 3678 (104, 138) 2.00 []
      smC "abbot" "Scarlet Abbot" 40 3678 (104, 138) 2.00 [ scarletSmite; scarletHeal ]
      smC "fairbanks" "High Inquisitor Fairbanks" 40 6130 (151, 200) 2.00 [ scarletSmite; scarletHeal ]
      sm "mograine" "Scarlet Commander Mograine" 42 5945 (181, 204) 2.20 [ hammerOfJustice ]
      smC "whitemane" "High Inquisitor Whitemane" 42 5284 (138, 152) 2.00 [ scarletSmite; scarletHeal; scarletRenew ] ]

  let private scarletMobOf (key: string) : ScarletMob =
    scarletRoster
    |> List.tryFind (fun m -> m.Key = key)
    |> Option.defaultWith (fun () -> failwithf "no such Scarlet mob: %s" key)

  /// One pull: `pack [ "sentry", 2; "trainee", 1 ]`.
  let private pack (members: (string * int) list) : string list =
    [ for key, count in members do
        yield! List.replicate count key ]

  /// The pull order of each wing, entrance first, boss last. Counts are the
  /// researched pull density of that wing: Graveyard 12-16 pulls, Library 12-18,
  /// Armory 15-20, Cathedral 18-25 plus its gauntlet.
  let private scarletPullPlan (wing: string) : string list list =
    match wing with
    | "graveyard" ->
      [ pack [ "trainee", 3 ]
        pack [ "trainee", 2; "sentry", 1 ]
        pack [ "scryer", 2; "trainee", 1 ]
        pack [ "sentry", 2; "scryer", 1 ]
        pack [ "torturer", 1; "trainee", 2 ]
        pack [ "torturer", 2; "scryer", 1 ]
        pack [ "spirit", 2; "phantasm", 1 ]
        pack [ "phantasm", 2; "torturer", 1 ]
        pack [ "sentry", 2; "torturer", 1 ]
        pack [ "scryer", 2; "spirit", 1 ]
        pack [ "phantasm", 2; "sentry", 1 ]
        pack [ "vishas", 1; "torturer", 2 ]
        pack [ "phantasm", 2; "spirit", 2 ]
        pack [ "thalnos", 1; "scryer", 2 ] ]
    | "library" ->
      [ pack [ "gallant", 2; "adept", 1 ]
        pack [ "soldier", 2; "monk", 1 ]
        pack [ "adept", 2; "gallant", 1 ]
        pack [ "monk", 2; "soldier", 1 ]
        pack [ "hound", 2; "soldier", 1 ]
        pack [ "gallant", 2; "adept", 1 ]
        pack [ "monk", 2; "hound", 1 ]
        pack [ "soldier", 2; "adept", 1 ]
        pack [ "azshir", 1; "monk", 2 ]
        pack [ "champion-fallen", 1; "ironspine", 1; "soldier", 1 ]
        pack [ "loksey", 1; "hound", 2 ]
        pack [ "monk", 2; "gallant", 1 ]
        pack [ "doan", 1; "adept", 2 ] ]
    | "armory" ->
      [ pack [ "gallant", 2; "adept", 1 ]
        pack [ "defender", 2; "chaplain", 1 ]
        pack [ "beastmaster", 1; "hound", 2 ]
        pack [ "diviner", 2; "gallant", 1 ]
        pack [ "myrmidon", 2; "chaplain", 1 ]
        pack [ "guardsman", 1; "protector", 2 ]
        pack [ "evoker", 2; "defender", 1 ]
        pack [ "conjuror", 2; "myrmidon", 1 ]
        pack [ "sorcerer", 2; "protector", 1 ]
        pack [ "guardsman", 1; "defender", 2 ]
        pack [ "diviner", 1; "evoker", 1; "chaplain", 1 ]
        pack [ "myrmidon", 2; "sorcerer", 1 ]
        pack [ "protector", 2; "conjuror", 1 ]
        pack [ "guardsman", 2; "chaplain", 1 ]
        pack [ "herod", 1; "myrmidon", 2 ] ]
    | _ ->
      [ pack [ "champion", 2; "abbot", 1 ]
        pack [ "wizard", 2; "defender", 1 ]
        pack [ "centurion", 1; "sorcerer", 2 ]
        pack [ "abbot", 2; "champion", 1 ]
        pack [ "wizard", 2; "protector", 1 ]
        pack [ "centurion", 2; "abbot", 1 ]
        pack [ "sorcerer", 2; "champion", 1 ]
        pack [ "wizard", 1; "centurion", 2 ]
        pack [ "abbot", 2; "wizard", 1 ]
        pack [ "sorcerer", 2; "protector", 2 ]
        pack [ "champion", 2; "centurion", 1 ]
        pack [ "wizard", 2; "abbot", 1 ]
        pack [ "centurion", 2; "sorcerer", 1 ]
        pack [ "champion", 2; "abbot", 2 ]
        pack [ "centurion", 2; "wizard", 1 ]
        pack [ "champion", 2; "sorcerer", 1 ]
        pack [ "fairbanks", 1; "abbot", 2 ]
        pack [ "mograine", 1; "whitemane", 1; "champion", 2 ] ]

  /// Lay a wing's pull plan out from the entrance to the deepest corner. Every
  /// battlemap puts the door on the bottom edge, so clear order is walk order: the
  /// i-th pull sits at the walk position proportional to its place in the plan,
  /// and each member takes the nearest free tile to that centre.
  /// How far from the door the Party's own room extends, and how far apart two
  /// pulls have to be so that clearing one does not chain into the next.
  let private partyRoom = 8
  let private packSpacing = 7

  let private scarletPopulate
    (grid: Grid)
    (plan: string list list)
    (occupied: Set<Pos>)
    (anchor: Pos)
    : Entity list =
    let floors = MapsScarlet.floorTiles grid
    let entrance = floors |> List.maxBy (fun p -> p.Y, -p.X)

    let walk =
      floors
      |> List.sortBy (fun p -> Pos.chebyshev p entrance, p.Y, p.X)
      |> Array.ofList

    let free = System.Collections.Generic.HashSet<Pos>(occupied)
    let counters = System.Collections.Generic.Dictionary<string, int>()
    let mutable nextId = 6

    let takeId () =
      let id = nextId
      nextId <- nextId + 1
      id

    /// "Scarlet Sentry", then "Scarlet Sentry 2" for the next one.
    let nameOf (template: ScarletMob) =
      let seen =
        match counters.TryGetValue template.Key with
        | true, n -> n
        | _ -> 0

      counters.[template.Key] <- seen + 1

      if seen = 0 then template.Name else sprintf "%s %d" template.Name (seen + 1)

    let pulls = List.length plan
    let mobs = ResizeArray<Entity>()

    // Space the pulls by actual distance, not by their place in the walk order.
    // Walking order zigzags (it is a chebyshev sort), so evenly spaced indices put
    // two packs in one room — which is how a wing becomes a single 12-mob fight.
    let centres =
      let chosen = ResizeArray<Pos>()

      for p in walk do
        if
          chosen.Count < pulls
          && Pos.chebyshev p anchor > partyRoom
          && chosen |> Seq.forall (fun c -> Pos.chebyshev c p >= packSpacing)
        then
          chosen.Add p

      // A wing too cramped for that spacing still needs its pulls: fall back to
      // evenly spaced positions along the walk.
      if chosen.Count < pulls then
        [ for i in 0 .. pulls - 1 ->
            walk.[min (walk.Length - 1) ((i + 1) * walk.Length / (pulls + 1))]
            |> fun p -> if Pos.chebyshev p anchor <= partyRoom then entrance else p ]
      else
        List.ofSeq chosen

    plan
    |> List.iteri (fun i keys ->
      let centre = centres.[min i (centres.Length - 1)]

      let nearest =
        walk |> Array.sortBy (fun p -> Pos.chebyshev p centre, p.Y, p.X)

      for key in keys do
        match nearest |> Array.tryFind (fun p -> not (free.Contains p)) with
        | Some pos ->
          free.Add pos |> ignore
          let template = scarletMobOf key

          mobs.Add(
            mob
              (takeId ())
              (nameOf template)
              CreatureType.Humanoid
              template.Level
              template.Health
              template.Mana
              (Some
                { Min = fst template.Damage
                  Max = snd template.Damage
                  Ticks = secTicks template.Swing
                  Range = 1 })
              template.Abilities
              None
              pos
          )
        | None -> ())

    List.ofSeq mobs

  /// Which of a wing's three researched levels the Party enters at. The band is
  /// the difficulty ladder: minimum is the level the dungeon opens at, maximum
  /// is the level of its highest boss.
  type Difficulty =
    | Minimum
    | Median
    | Maximum

  /// One Scarlet Monastery wing: its extracted grid and the band researched in
  /// .pi/research/levelscale/dungeon-bands.md.
  type Wing =
    { Name: string
      Minimum: int
      Median: int
      Maximum: int }

  let scarletWings : Wing list =
    [ { Name = "graveyard"; Minimum = 30; Median = 33; Maximum = 34 }
      { Name = "library"; Minimum = 33; Median = 36; Maximum = 37 }
      { Name = "armory"; Minimum = 37; Median = 39; Maximum = 40 }
      { Name = "cathedral"; Minimum = 38; Median = 41; Maximum = 42 } ]

  let wingOf (name: string) : Wing =
    scarletWings
    |> List.tryFind (fun w -> w.Name = name)
    |> Option.defaultWith (fun () -> failwithf "no such Scarlet Monastery wing: %s" name)

  let levelOf (difficulty: Difficulty) (wing: Wing) : int =
    match difficulty with
    | Minimum -> wing.Minimum
    | Median -> wing.Median
    | Maximum -> wing.Maximum

  /// A wing with the Party standing at its entrance. All four battlemaps put the
  /// entrance on the bottom edge, so the door is the lowest floor tile and the
  /// Party fills the five tiles nearest it. The mobs land on top of this once
  /// each wing's sourced roster is in; the grid is walkable now, which is what
  /// the extraction needed checking against.
  let scarletMonasteryAt (wingName: string) (difficulty: Difficulty) (seed: uint64) : World =
    let wing = wingOf wingName
    let level = levelOf difficulty wing
    let grid = MapsScarlet.wing wing.Name
    let floors = MapsScarlet.floorTiles grid
    let entrance = floors |> List.maxBy (fun p -> p.Y, -p.X)

    // Enter in the roomiest tile near the door rather than in the doorway: five
    // bodies in a one-tile corridor cannot pass each other, and a Tank boxed in by
    // its own healers never leaves the room.
    let roominess (p: Pos) =
      [ for dy in -2 .. 2 do
          for dx in -2 .. 2 do
            { X = p.X + dx; Y = p.Y + dy } ]
      |> List.filter (Grid.isFloor grid)
      |> List.length

    let anchor =
      floors
      |> List.filter (fun p -> Pos.chebyshev p entrance <= 10)
      |> List.sortBy (fun p -> -(roominess p), Pos.chebyshev p entrance)
      |> List.head

    let mobs =
      scarletPopulate grid (scarletPullPlan wing.Name) (Set.singleton anchor) anchor
      |> List.map (fun e -> { e with Armor = 40 * e.Level })

    let partyTiles =
      // Marching order, not a cluster: the Tank stands at the head of the route
      // toward the first pack and the others file in behind it. Five bodies bunched
      // in a two-tile doorway box the Tank in with its own healers, and the whole
      // wing is then lost to geometry rather than to difficulty.
      let pack = mobs |> List.minBy (fun m -> Pos.chebyshev m.Pos anchor)
      let route = (Path.astar grid (fun _ -> false) anchor pack.Pos).Path |> List.truncate 5

      if route.Length = 5 then
        List.rev route
      else
        floors
        |> List.sortBy (fun p -> Pos.chebyshev p anchor, p.Y, p.X)
        |> List.truncate 5

    let party =
      List.map2
        (fun (e: Entity) p -> { e with Pos = p })
        (partyOf defaultRoster level)
        partyTiles

    { Tick = ticks 0
      Grid = grid
      Entities = party @ mobs
      Pending = []
      Rng = Rng.streamsOf seed
      PartyBuffs = partyBuffs
      Quests = []
      Log = [] }

  /// The three worlds at the reference level of 24. Their `*At` forms take the
  /// Party's level, which is the whole difficulty knob: the same dungeon, the same
  /// mobs, a Party wearing only the gear it can reach.
  let gully (seed: uint64) : World = gullyAt 24 seed
  let dungeon (seed: uint64) : World = dungeonAt 24 seed
  let wailingCaverns (seed: uint64) : World = wailingCavernsAt 24 seed
