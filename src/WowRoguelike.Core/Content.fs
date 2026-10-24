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
///     Battle Shout: pacing-only effects. Their rank ladders and per-rank
///     numbers are sourced from `.pi/research/levelscale/`, but the effects
///     themselves (rage scaling, snares, fleeing) are not modelled.
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

  // Every party ability below is a rank ladder from the researched tables in
  // `.pi/research/levelscale/`, and a character resolves the newest rank at or
  // below its level (`AbilityDef.at`). The rank `rank24` resolves is the one the
  // level-24 party has always fought with, so that gate does not move; the class
  // kits ask for the same number.

  /// The rank a character of the given level trains, as the flat record a kit
  /// passes to `heroStats`.
  let private rankAt (level: int) (ability: AbilityDef) = (AbilityDef.at level ability).Value

  /// The rank a level-24 character has trained — what the class kits carry.
  let private rank24 = rankAt 24

  /// Taunt (level 10): 10 s cooldown, one rank at every level. The table has it
  /// free and at 1 tile; the 10 rage and the 8 tiles are the file's and stay,
  /// because they are what a party of this level pulls with.
  let tauntRanks: AbilityDef =
    { Name = "Taunt"
      Resource = Rage
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ { Level = 10
            Cost = 10
            CastTicks = ticks 0
            CooldownTicks = secTicks 10.0
            Range = 8
            Effects = [ Taunt ] } ] }

  let taunt = rank24 tauntRanks

  /// Heroic Strike ranks 1-6 (levels 1-40): the swing plus a flat step that grows
  /// 11 → 80. The cooldown is the weapon's own 2.70 s, because Heroic Strike
  /// replaces the next swing rather than adding one, and the table's extra threat
  /// (+20 → +118) is left to the damage alone, as it always has been.
  let heroicStrikeRanks: AbilityDef =
    { Name = "Heroic Strike"
      Resource = Rage
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, bonus in [ 1, 11; 8, 21; 16, 32; 24, 44; 32, 58; 40, 80 ] do
            { Level = level
              Cost = 15
              CastTicks = ticks 0
              CooldownTicks = secTicks 2.7
              Range = 1
              Effects = [ WeaponDamage(10000, bonus) ] } ] }

  let heroicStrike = rank24 heroicStrikeRanks

  /// Lesser Heal ranks 1-3 (levels 1, 4, 10): rank 3 is the last rank in the game.
  /// It keeps the file's 135-158 — the 1.12 pair, against the table's 1.15.9
  /// 143-165 — because it is the heal a level-24 Priest still carries.
  let lesserHealRanks: AbilityDef =
    { Name = "Lesser Heal"
      Resource = Mana
      Stance = None
      TargetKind = Ally
      Interruptible = true
      Ranks =
        [ for level, cost, cast, lo, hi in
            [ 1, 30, 1.5, 47, 58; 4, 45, 2.0, 76, 91; 10, 75, 2.5, 135, 158 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks cast
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ Heal(lo, hi) ] } ] }

  let lesserHeal = rank24 lesserHealRanks

  /// Heal ranks 1-4 (levels 16-34), 3.0 s on every rank. Rank 2 keeps the file's
  /// 453-516 — the 1.12 429-492 plus the set's +24 healing, against the table's
  /// 445-507 — because it is the rank the ladder carries at 24.
  let healRanks: AbilityDef =
    { Name = "Heal"
      Resource = Mana
      Stance = None
      TargetKind = Ally
      Interruptible = true
      Ranks =
        [ for level, cost, lo, hi in
            [ 16, 155, 307, 353; 22, 205, 453, 516; 28, 255, 586, 662; 34, 305, 734, 827 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks 3.0
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ Heal(lo, hi) ] } ] }

  let heal = rank24 healRanks

  /// Renew ranks 1-6 (levels 8-38): 15 s / 5 ticks at every rank, so the ladder is
  /// 3 → 27 a second. Rank 3 keeps the file's 12 a second — the table's 175 over
  /// 15 s rounds to 11.7, and the file carries the set's +24 healing — because it
  /// is the rank a level-24 Priest casts.
  let renewRanks: AbilityDef =
    { Name = "Renew"
      Resource = Mana
      Stance = None
      TargetKind = Ally
      Interruptible = false
      Ranks =
        [ for level, cost, perTick in
            [ 8, 30, 3; 14, 65, 7; 20, 105, 12; 26, 140, 16; 32, 170, 21; 38, 205, 27 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 5.0
              Range = 8
              Effects = [ HealOverTime(secTicks 15.0, perTick) ] } ] }

  let renew = rank24 renewRanks

  /// Fireball ranks 1-7 (levels 1-36): the cast climbs 1.5 → 3.5 s, and the burn
  /// runs 4 → 8 s behind it. Rank 5 keeps the file's 146-195 up front and 3 a
  /// second for the burn (24 in total against the table's 20); the 8 tiles are
  /// the file's for the table's 7.
  let fireballRanks: AbilityDef =
    { Name = "Fireball"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, cast, lo, hi, duration, perTick in
            [ 1, 30, 1.5, 16, 25, 4.0, 1
              6, 45, 2.0, 34, 49, 6.0, 1
              12, 65, 2.5, 57, 77, 6.0, 1
              18, 95, 3.0, 89, 122, 8.0, 2
              24, 140, 3.5, 146, 195, 8.0, 3
              30, 185, 3.5, 207, 274, 8.0, 4
              36, 220, 3.5, 264, 345, 8.0, 4 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks cast
              CooldownTicks = ticks 0
              Range = 8
              Effects =
                [ SpellDamage(lo, hi); DamageOverTime(secTicks duration, perTick) ] } ] }

  let fireball = rank24 fireballRanks

  /// Arcane Shot ranks 1-5 (levels 6-36): 33 Arcane for 50 mana at rank 3, instant
  /// on a 6 s cooldown. Magic, so it is not armour-mitigated — the old record had
  /// it as 90 mana and physical `Damage` — and there is no weapon component.
  let arcaneShotRanks: AbilityDef =
    { Name = "Arcane Shot"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, cost, damage in
            [ 6, 25, 13; 12, 35, 21; 20, 50, 33; 28, 80, 59; 36, 105, 83 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 6.0
              Range = 8
              Effects = [ SpellDamage(damage, damage) ] } ] }

  let arcaneShot = rank24 arcaneShotRanks

  // -- Paladin --------------------------------------------------------------
  // A paladin's kit, laddered like every other class's. Ranges keep the
  // codebase's 40-yd-is-8-tiles convention instead of the tables' 5-yd tiles; a
  // range does not vary by rank, so one number stands for a whole ladder.

  /// Holy Light ranks 1-6 (levels 1-38): the 1.15.9 client's heals, which is the
  /// edition this content already followed (1.12 is 39-47 … 698-780).
  let holyLightRanks: AbilityDef =
    { Name = "Holy Light"
      Resource = Mana
      Stance = None
      TargetKind = Ally
      Interruptible = true
      Ranks =
        [ for level, cost, lo, hi in
            [ 1, 35, 42, 51
              6, 60, 81, 96
              14, 110, 167, 196
              22, 190, 322, 368
              30, 275, 506, 569
              38, 365, 717, 799 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks 2.5
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ Heal(lo, hi) ] } ] }

  let holyLight = rank24 holyLightRanks

  /// Flash of Light ranks 1-3 (levels 20-34): the efficient heal — 1.9-2.2
  /// healing per mana against Holy Light's 1.6-2.0 at the same rank.
  let flashOfLightRanks: AbilityDef =
    { Name = "Flash of Light"
      Resource = Mana
      Stance = None
      TargetKind = Ally
      Interruptible = true
      Ranks =
        [ for level, cost, lo, hi in
            [ 20, 35, 67, 77; 26, 50, 102, 117; 34, 70, 153, 171 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks 1.5
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ Heal(lo, hi) ] } ] }

  let flashOfLight = rank24 flashOfLightRanks

  /// Judgement: one rank for the whole game (level 4), and no damage of its own —
  /// it unleashes the active Seal. With no seals modelled it stays the Seal of
  /// Righteousness rank-3 judge, 53-57 Holy, for 31 mana. The table puts it at
  /// 2 tiles for 6% of base mana; both are the file's approximations and stay.
  ///
  /// shortcut: the seal itself is not modelled, so Judgement is a plain Holy
  /// nuke rather than something that spends a seal. Add seals if the paladin's
  /// swing-by-swing damage ever matters.
  let judgementRanks: AbilityDef =
    { Name = "Judgement"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ { Level = 4
            Cost = 31
            CastTicks = ticks 0
            CooldownTicks = secTicks 10.0
            Range = 1
            Effects = [ SpellDamage(53, 57) ] } ] }

  let judgement = rank24 judgementRanks

  /// Hammer of Justice ranks 1-3 (levels 8, 24, 40): the stun grows 3 → 5 s on a
  /// 1 min cooldown. The paladin's interrupt and peel. Distinct from the mobs'
  /// `hammerOfJustice` below, which is the NPC spell 853 — a 3 s stun for a third
  /// of the mana. The table's 2 tiles lose to the file's 1.
  let paladinHammerOfJusticeRanks: AbilityDef =
    { Name = "Hammer of Justice"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, cost, stun in [ 8, 30, 3.0; 24, 50, 4.0; 40, 75, 5.0 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 60.0
              Range = 1
              Effects = [ Stun(secTicks stun) ] } ] }

  let paladinHammerOfJustice = rank24 paladinHammerOfJusticeRanks

  /// Divine Protection ranks 1-2 (levels 6, 18): 6 then 8 s of physical and spell
  /// immunity for 15 then 35 mana on a 5 min cooldown. The sim has no "cannot
  /// act" state, so the paladin keeps fighting through it, which makes it
  /// strictly stronger here than in WoW.
  /// shortcut: immunity without the cannot-act penalty, add the penalty if the
  /// paladin's survivability ever decides a fight on its own.
  let divineProtectionRanks: AbilityDef =
    { Name = "Divine Protection"
      Resource = Mana
      Stance = None
      TargetKind = Self
      Interruptible = false
      Ranks =
        [ for level, cost, duration in [ 6, 15, 6.0; 18, 35, 8.0 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 300.0
              Range = 0
              Effects = [ ReducesDamage(secTicks duration, 10000) ] } ] }

  let divineProtection = rank24 divineProtectionRanks

  // -- Warlock --------------------------------------------------------------
  // An Affliction warlock's kit. The ladder keeps the file's 1.12-trained-at-level
  // values at the rank a level-24 warlock carries and takes the tables' 1.15.9
  // cap values for the other ranks, which is the split the research records; the
  // per-rank mana, cast times and durations are the tables' either way.

  /// Shadow Bolt ranks 1-6 (levels 1-36). Rank 4's 86-99 is the 1.12 base the
  /// file has always carried (the table's 1.15.9 cap is 92-104), so it stays and
  /// the ranks around it keep the ladder monotone. The only spell the +30 shadow
  /// damage on the set amplifies per cast.
  let shadowBoltRanks: AbilityDef =
    { Name = "Shadow Bolt"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, cast, lo, hi in
            [ 1, 25, 1.7, 13, 17
              6, 40, 2.2, 26, 32
              12, 70, 2.8, 52, 60
              20, 110, 3.0, 86, 99
              28, 160, 3.0, 150, 170
              36, 210, 3.0, 213, 239 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks cast
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ SpellDamage(lo, hi) ] } ] }

  let shadowBolt = rank24 shadowBoltRanks

  /// Corruption ranks 1-4: 40, 90, 222 and 324 Shadow over a duration that grows
  /// 12 → 15 → 18 s. A 2.0 s cast in vanilla on every rank — Improved Corruption
  /// is a talent, and no warlock here has it. Rank 3's 37 every 3 s is 12 a
  /// second, which is the file's number (the table's 222 is 12.3 a second).
  let corruptionRanks: AbilityDef =
    { Name = "Corruption"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, duration, perTick in
            [ 4, 35, 12.0, 3; 14, 55, 15.0, 6; 24, 100, 18.0, 12; 34, 160, 18.0, 18 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks 2.0
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ DamageOverTime(secTicks duration, perTick) ] } ] }

  let corruption = rank24 corruptionRanks

  /// Curse of Agony ranks 1-4: 24 s, instant, escalating in WoW. Flat here at the
  /// per-second average: 4 / 7 / 14 / 21, where the table's totals are 84 / 180 /
  /// 324 / 504. Rank 2's 7 a second (168) is the file's number; the table's 7.5
  /// rounds to it.
  /// shortcut: every damage-over-time shares one `Corrupted` aura, so Corruption
  /// and Curse of Agony refresh each other instead of stacking. Split the aura
  /// by source if the warlock ever needs both on one target.
  let curseOfAgonyRanks: AbilityDef =
    { Name = "Curse of Agony"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, cost, perTick in
            [ 8, 25, 4; 18, 50, 7; 28, 90, 14; 38, 130, 21 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ DamageOverTime(secTicks 24.0, perTick) ] } ] }

  let curseOfAgony = rank24 curseOfAgonyRanks

  /// Immolate ranks 1-5: a Fire hit up front and 15 s of burn on every rank. Rank
  /// 3's 45 up front is the 1.12 base (the table's cap is 52) and stays; the 90
  /// over 15 s is 6 a second, which is the same on both.
  let warlockImmolateRanks: AbilityDef =
    { Name = "Immolate"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, direct, perTick in
            [ 1, 25, 10, 1
              10, 45, 23, 3
              20, 90, 45, 6
              30, 155, 100, 11
              40, 220, 147, 17 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks 2.0
              CooldownTicks = ticks 0
              Range = 8
              Effects =
                [ SpellDamage(direct, direct); DamageOverTime(secTicks 15.0, perTick) ] } ] }

  let warlockImmolate = rank24 warlockImmolateRanks

  /// Fear ranks 1-2 (levels 8 and 32): only the duration changes, 10 then 15 s.
  /// The target stops acting. The sim has no fleeing state, so this is a `Sleep`
  /// — the mob stands still and takes it, which is weaker than WoW's fear only in
  /// that it cannot be chased into another pack. The 70 mana is pacing, not data
  /// (the table costs 15 % of base mana on both ranks) and the file's 5 tiles
  /// stand for the table's 4.
  /// shortcut: fear modelled as sleep, add a flee state if mobs need to run.
  let fearRanks: AbilityDef =
    { Name = "Fear"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, duration in [ 8, 10.0; 32, 15.0 ] do
            { Level = level
              Cost = 70
              CastTicks = secTicks 1.5
              CooldownTicks = ticks 0
              Range = 5
              Effects = [ Sleep(secTicks duration) ] } ] }

  let fear = rank24 fearRanks

  // -- Hunter ---------------------------------------------------------------
  // A marksman hunter's kit, laddered to the cap. Rapid Fire (26) and Aimed Shot
  // (21 talent points, only 15 available) are still out. The shots' 8-35 yd is 7
  // tiles in the tables; the codebase's 8 stands for the whole ladder.

  /// Serpent Sting ranks 1-5 (levels 4-34): 15 s of Nature damage and nothing
  /// else — no weapon component, instant, no cooldown. Rank 3's 5 a second (75)
  /// is the file's number against the table's 80 (16 per 3 s tick).
  /// Rank 6 is trained at 42, one level past the ladder.
  let serpentStingRanks: AbilityDef =
    { Name = "Serpent Sting"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, cost, perTick in
            [ 4, 15, 1; 10, 30, 3; 18, 50, 5; 26, 80, 9; 34, 115, 14 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ DamageOverTime(secTicks 15.0, perTick) ] } ] }

  let serpentSting = rank24 serpentStingRanks

  /// Raptor Strike ranks 1-6 (levels 1-40): the next melee swing plus a flat step
  /// for 6 s of cooldown. The one ability in the hunter's kit that really is a
  /// weapon strike.
  let raptorStrikeRanks: AbilityDef =
    { Name = "Raptor Strike"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, cost, bonus in
            [ 1, 15, 5; 8, 25, 11; 16, 35, 21; 24, 45, 34; 32, 55, 50; 40, 70, 80 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 6.0
              Range = 1
              Effects = [ WeaponDamage(10000, bonus) ] } ] }

  let raptorStrike = rank24 raptorStrikeRanks

  // -- Shaman ---------------------------------------------------------------
  // An elemental shaman's kit, laddered to the cap. Windfury Weapon (30) and
  // Chain Lightning (32) are still out of it, and so are the totems. Cast times
  // climb with the ladder, so they come from the tables rank by rank; the 1.15.9
  // values are the edition this content follows.

  /// Lightning Bolt ranks 1-7 (levels 1-38): 1.5 s at rank 1 growing to 3.0 s at
  /// rank 4 and staying there.
  let shamanLightningBoltRanks: AbilityDef =
    { Name = "Lightning Bolt"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, cast, lo, hi in
            [ 1, 15, 1.5, 15, 17
              8, 30, 2.0, 28, 32
              14, 45, 2.5, 48, 56
              20, 75, 3.0, 88, 100
              26, 105, 3.0, 131, 149
              32, 135, 3.0, 179, 201
              38, 165, 3.0, 235, 263 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks cast
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ SpellDamage(lo, hi) ] } ] }

  let shamanLightningBolt = rank24 shamanLightningBoltRanks

  /// Earth Shock ranks 1-5 (levels 4-36): 125-133 at rank 4, instant, 6 s
  /// cooldown — and it is the shaman's interrupt, which is why the party gains a
  /// second kick that is not the Rogue's or the Warrior's.
  /// shortcut: shocks share one 6 s cooldown in WoW; here each has its own, so a
  /// shaman can chain Earth Shock into Flame Shock.
  let earthShockRanks: AbilityDef =
    { Name = "Earth Shock"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, cost, lo, hi in
            [ 4, 30, 19, 21
              8, 50, 35, 37
              14, 85, 65, 69
              24, 145, 125, 133
              36, 240, 235, 249 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 6.0
              Range = 5
              Effects = [ SpellDamage(lo, hi); Interrupt ] } ] }

  let earthShock = rank24 earthShockRanks

  /// Flame Shock ranks 1-4 (levels 10-40): the Fire hit scales with rank, the
  /// burn is 12 s / 4 ticks at every rank. Rank 2's 51 up front and 48 over the
  /// twelve seconds is 4 a second, which is both the file's and the table's.
  let flameShockRanks: AbilityDef =
    { Name = "Flame Shock"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, cost, direct, perTick in
            [ 10, 55, 25, 2; 18, 95, 51, 4; 28, 160, 94, 8; 40, 250, 163, 14 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 6.0
              Range = 5
              Effects =
                [ SpellDamage(direct, direct); DamageOverTime(secTicks 12.0, perTick) ] } ] }

  let flameShock = rank24 flameShockRanks

  /// Healing Wave ranks 1-9 (levels 1-56): 389-453 for 200 mana at rank 5, on a
  /// 3.0 s cast. A damage shaman will not cast it — the party's healer does the
  /// healing — but it is what a shaman has, so it is in the kit. The mana curve
  /// is monotonic 200 → 265 → 340 → 400 → 460; ranks 8 and 9 are the level 48 and
  /// 56 trains a level-60 Shaman actually stands on.
  let shamanHealingWaveRanks: AbilityDef =
    { Name = "Healing Wave"
      Resource = Mana
      Stance = None
      TargetKind = Ally
      Interruptible = true
      Ranks =
        [ for level, cost, cast, lo, hi in
            [ 1, 25, 1.5, 36, 46
              6, 45, 2.0, 69, 83
              12, 80, 2.5, 136, 162
              18, 155, 3.0, 279, 327
              24, 200, 3.0, 389, 453
              32, 265, 3.0, 552, 638
              40, 340, 3.0, 759, 873
              48, 400, 3.0, 1014, 1136
              56, 460, 3.0, 1234, 1382 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks cast
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ Heal(lo, hi) ] } ] }

  let shamanHealingWave = rank24 shamanHealingWaveRanks

  // -- Warrior --------------------------------------------------------------

  /// Sunder Armour ranks 1-3 (levels 10, 22, 34): the armour per stack grows
  /// 90 → 270 over five stacks and 30 s. Rank 2 keeps the file's flat
  /// `Threat(200)` so the Tank holds the pull the same way at every rank — the
  /// table's own flat threat is +45 / +99 / +153 — and the 6 s cooldown is the
  /// file's pacing choice.
  let sunderArmorRanks: AbilityDef =
    { Name = "Sunder Armor"
      Resource = Rage
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, armor in [ 10, 90; 22, 180; 34, 270 ] do
            { Level = level
              Cost = 15
              CastTicks = ticks 0
              CooldownTicks = secTicks 6.0
              Range = 1
              Effects = [ Sunder(secTicks 30.0, armor); Threat(200) ] } ] }

  let sunderArmor = rank24 sunderArmorRanks

  /// Shield Bash ranks 1-2 (levels 12, 32): 6 then 18 damage and a 6 s school
  /// lockout, for 10 rage on a 12 s cooldown. Needs a shield.
  let shieldBashRanks: AbilityDef =
    { Name = "Shield Bash"
      Resource = Rage
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, damage in [ 12, 6; 32, 18 ] do
            { Level = level
              Cost = 10
              CastTicks = ticks 0
              CooldownTicks = secTicks 12.0
              Range = 1
              Effects = [ Damage(damage, damage); Interrupt ] } ] }

  let shieldBash = rank24 shieldBashRanks

  /// Shield Block rank 1 (level 16): +75% block for 10 s for 10 rage, so the
  /// shield stops being a 5% coin flip and becomes what the Tank is doing with
  /// his rage. The table says 5 s and a single attack; the 10 s is the file's
  /// and stays, because it is the level-24 tank's mitigation.
  let shieldBlockRanks: AbilityDef =
    { Name = "Shield Block"
      Resource = Rage
      Stance = None
      TargetKind = Self
      Interruptible = false
      Ranks =
        [ { Level = 16
            Cost = 10
            CastTicks = ticks 0
            CooldownTicks = secTicks 5.0
            Range = 0
            Effects = [ Blocks(secTicks 10.0) ] } ] }

  let shieldBlock = rank24 shieldBlockRanks

  /// Shield Wall rank 1: 75% of all damage taken removed for 10 s, free, on the 30
  /// minute cooldown that makes it a decision rather than a rotation. One rank at
  /// every level in the table, which trains it at 28; this ladder carries it at 24
  /// because the level-24 Warrior is the party's shield tank and Shield Wall is
  /// part of that kit — moving it to 28 changes who can be hit without the button.
  let shieldWallRanks: AbilityDef =
    { Name = "Shield Wall"
      Resource = Rage
      Stance = None
      TargetKind = Self
      Interruptible = false
      Ranks =
        [ { Level = 24
            Cost = 0
            CastTicks = ticks 0
            CooldownTicks = secTicks 1800.0
            Range = 0
            Effects = [ ReducesDamage(secTicks 10.0, 7500) ] } ] }

  let shieldWall = rank24 shieldWallRanks

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

  /// Gouge: 45 energy, a 4 s incap, and it builds a combo point. The incap is a
  /// `Sleep` rather than a `Stun`: any damage breaks it, so Gouge buys a re-pull
  /// or a breather, not four free seconds of damage.
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
              Effects = [ Damage(damage, damage); Sleep(secTicks 4.0); ComboPoint 1 ] } ] }

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

  /// Sap: 65 energy, 3 tiles, and a 30 s incap that starts nothing. No cooldown,
  /// deliberately: being out of combat only *is* the limiter, so a second Sap can
  /// never happen inside a fight. `Sleep`, so the first damage breaks it.
  let sap: AbilityDef =
    { Name = "Sap"
      Resource = Energy
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level in [ 6; 22; 38; 54 ] do
            { Level = level
              Cost = 65
              CastTicks = ticks 0
              CooldownTicks = ticks 0
              Range = 3
              Effects = [ Sleep(secTicks 30.0) ] } ] }

  /// Cheap Shot: 60 energy for a 4 s stun on a target that cannot see it coming.
  /// Trained at 26, so a level-24 Rogue does not have it and the anchor is safe.
  let cheapShot: AbilityDef =
    { Name = "Cheap Shot"
      Resource = Energy
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level in [ 26; 42; 58 ] do
            { Level = level
              Cost = 60
              CastTicks = ticks 0
              CooldownTicks = ticks 0
              Range = 1
              Effects = [ Stun(secTicks 4.0) ] } ] }

  /// Kidney Shot: 25 energy, a 4 s stun, 20 s cooldown — the second link in a
  /// stun-lock, for when Cheap Shot's four seconds are not enough.
  ///
  /// shortcut: vanilla Kidney Shot scales 1-6 s with combo points and spends
  /// them; `Effect` cannot express a duration that grows with combo points, so
  /// this is a flat 4 s that spends nothing. Upgrade if the Rogue comes to depend
  /// on the lock.
  let kidneyShot: AbilityDef =
    { Name = "Kidney Shot"
      Resource = Energy
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level in [ 30; 46; 60 ] do
            { Level = level
              Cost = 25
              CastTicks = ticks 0
              CooldownTicks = secTicks 20.0
              Range = 1
              Effects = [ Stun(secTicks 4.0) ] } ] }

  // -- Mage ----------------------------------------------------------------

  /// Frostbolt ranks 1-7 (levels 4-38): the cast grows 1.5 → 3.0 s across the
  /// ladder. Every rank also slows the target 40 % for 5-8 s; `Effect` has no
  /// snare, so the slow is not modelled and only the damage is.
  let frostboltRanks: AbilityDef =
    { Name = "Frostbolt"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, cast, lo, hi in
            [ 4, 25, 1.5, 18, 20
              8, 35, 1.8, 31, 35
              14, 50, 2.2, 51, 57
              20, 65, 2.6, 74, 83
              26, 100, 3.0, 126, 138
              32, 130, 3.0, 174, 190
              38, 160, 3.0, 227, 247 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks cast
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ SpellDamage(lo, hi) ] } ] }

  let frostbolt = rank24 frostboltRanks

  /// Arcane Missiles ranks 1-5 (levels 8-40): a channel of one missile a second,
  /// so `CastTicks` is the channel length and the whole channel lands as one hit
  /// — the sim has no channel. Rank 3 keeps the file's 56 a second (280 over the
  /// five seconds), against the table's 58 a second, 290 in total.
  let arcaneMissilesRanks: AbilityDef =
    { Name = "Arcane Missiles"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, channel, total in
            [ 8, 85, 3.0, 78; 16, 140, 4.0, 152; 24, 235, 5.0, 280; 32, 320, 5.0, 430; 40, 410, 5.0, 590 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks channel
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ SpellDamage(total, total) ] } ] }

  let arcaneMissiles = rank24 arcaneMissilesRanks

  /// Arcane Explosion ranks 1-4 (levels 14-38): 57-64 Arcane at rank 2 to
  /// everything within 10 yd for 120 mana.
  let arcaneExplosionRanks: AbilityDef =
    { Name = "Arcane Explosion"
      Resource = Mana
      Stance = None
      TargetKind = Self
      Interruptible = false
      Ranks =
        [ for level, cost, lo, hi in
            [ 14, 75, 34, 38; 22, 120, 57, 64; 30, 185, 101, 110; 38, 250, 143, 156 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 10.0
              Range = 0
              Effects = [ AreaDamage(3, lo, hi) ] } ] }

  let arcaneExplosion = rank24 arcaneExplosionRanks

  /// Blizzard ranks 1-3 (levels 20-36): 200 / 360 / 528 Frost over an
  /// eight-second channel in an 8-yard radius, for 320 / 520 / 720 mana. Same
  /// channel caveat as Arcane Missiles.
  let blizzardRanks: AbilityDef =
    { Name = "Blizzard"
      Resource = Mana
      Stance = None
      TargetKind = Self
      Interruptible = true
      Ranks =
        [ for level, cost, total in [ 20, 320, 200; 28, 520, 360; 36, 720, 528 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks 8.0
              CooldownTicks = secTicks 15.0
              Range = 0
              Effects = [ AreaDamage(5, total, total) ] } ] }

  let blizzard = rank24 blizzardRanks

  /// Counterspell (level 24): instant, 100 mana, 30 s cooldown, the only stop the
  /// Mage has for a healer's cast, and the one single-rank spell in the kit. The
  /// 10 s school lock is not modelled; the interrupt is, which is the part that
  /// matters.
  let counterspellRanks: AbilityDef =
    { Name = "Counterspell"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ { Level = 24
            Cost = 100
            CastTicks = ticks 0
            CooldownTicks = secTicks 30.0
            Range = 30
            Effects = [ Interrupt ] } ] }

  let counterspell = rank24 counterspellRanks

  /// Polymorph ranks 1-3 (levels 8, 20, 40): 60 / 90 / 120 mana for 20 / 30 / 40 s
  /// of sheep on a 30 s cooldown. A level-24 Mage has rank 2, and the Scarlet
  /// casters use this same record for their own polymorph.
  let polymorphRanks: AbilityDef =
    { Name = "Polymorph"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, duration in [ 8, 60, 20.0; 20, 90, 30.0; 40, 120, 40.0 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks 1.5
              CooldownTicks = secTicks 30.0
              Range = 8
              Effects = [ Polymorph(secTicks duration) ] } ] }

  let polymorph = rank24 polymorphRanks

  // -- Druid ----------------------------------------------------------------

  /// Rejuvenation ranks 1-7 (levels 4-40): 8 → 97 every 3 s for 12 s, so the
  /// ladder is 3 → 32 a second here. The rank a level-24 Druid carries is the
  /// table's rank 5 (level 28) at the file's numbers — 22 a second, which is the
  /// table's 20 plus the staff's +24 healing — so it sits at level 24 and spans
  /// levels 24-33 rather than leaving the Druid with rank 4's heal.
  let rejuvenationRanks: AbilityDef =
    { Name = "Rejuvenation"
      Resource = Mana
      Stance = None
      TargetKind = Ally
      Interruptible = false
      Ranks =
        [ for level, cost, perTick in
            [ 4, 25, 3; 10, 40, 5; 16, 75, 10; 22, 105, 15; 24, 135, 22; 34, 160, 25; 40, 195, 32 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 3.0
              Range = 8
              Effects = [ HealOverTime(secTicks 12.0, perTick) ] } ] }

  let rejuvenation = rank24 rejuvenationRanks

  /// Regrowth ranks 1-5 (levels 12-36): a heal up front and 21 s of heal-over-time
  /// (98 / 175 / 259 / 343 / 427 total, so 4 → 20 a second). Rank 3 keeps the
  /// file's 264-299 up front — the table's 1.15.9 rank is 255-290 and the 1.12 one
  /// is 240-274, the file's numbers plus the staff's +24 healing — with the
  /// table's 12 a second for the tail.
  let regrowthRanks: AbilityDef =
    { Name = "Regrowth"
      Resource = Mana
      Stance = None
      TargetKind = Ally
      Interruptible = true
      Ranks =
        [ for level, cost, lo, hi, perTick in
            [ 12, 120, 93, 107, 4
              18, 205, 176, 201, 8
              24, 280, 264, 299, 12
              30, 350, 336, 378, 16
              36, 420, 425, 478, 20 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks 2.0
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ Heal(lo, hi); HealOverTime(secTicks 21.0, perTick) ] } ] }

  let regrowth = rank24 regrowthRanks

  /// Healing Touch ranks 1-7 (levels 1-38): the cast climbs 1.5 → 3.5 s across
  /// the ladder and settles there. Rank 4 keeps the file's 387-470 (the 1.12
  /// 363-446 plus the staff's +24 healing); the table's 1.15.9 rank 4 is
  /// 376-459. The mobs' weaker Healing Touch keeps its own record above.
  let healingTouchR4Ranks: AbilityDef =
    { Name = "Healing Touch"
      Resource = Mana
      Stance = None
      TargetKind = Ally
      Interruptible = true
      Ranks =
        [ for level, cost, cast, lo, hi in
            [ 1, 25, 1.5, 40, 55
              8, 55, 2.0, 94, 119
              14, 110, 2.5, 204, 253
              20, 185, 3.0, 387, 470
              26, 270, 3.5, 589, 712
              32, 335, 3.5, 762, 914
              38, 405, 3.5, 958, 1143 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks cast
              CooldownTicks = secTicks 10.0
              Range = 8
              Effects = [ Heal(lo, hi) ] } ] }

  let healingTouchR4 = rank24 healingTouchR4Ranks

  /// Entangling Roots ranks 1-4 (levels 8-38): the root and its Nature damage
  /// share one duration that grows 12 → 21 s, and any damage may break it.
  /// Rank 2 is 50 Nature over 15 s, which is the file's 3 a second.
  let entanglingRootsRanks: AbilityDef =
    { Name = "Entangling Roots"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, duration, perTick in
            [ 8, 50, 12.0, 1; 18, 65, 15.0, 3; 28, 80, 18.0, 4; 38, 95, 21.0, 6 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks 1.5
              CooldownTicks = secTicks 20.0
              Range = 8
              Effects = [ Root(secTicks duration); DamageOverTime(secTicks duration, perTick) ] } ] }

  let entanglingRoots = rank24 entanglingRootsRanks

  // -- casters' damage, and the Warrior's cooldowns ---------------------------

  /// Wrath ranks 1-6 (levels 1-38): 63-74 at rank 4, where the table's 1.15.9
  /// rank is 69-79 and its 1.12 one is 63-73 — the file's pair is the 1.12 value
  /// and stays, because it is what a level-24 Druid has always hit for. Rank 2's
  /// 1.7 s cast is the odd one out; the ladder settles at 2.0 s.
  let wrathRanks: AbilityDef =
    { Name = "Wrath"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, cast, lo, hi in
            [ 1, 20, 1.5, 13, 16
              6, 35, 1.7, 28, 33
              14, 55, 2.0, 48, 57
              22, 70, 2.0, 63, 74
              30, 100, 2.0, 108, 123
              38, 125, 2.0, 148, 167 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks cast
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ SpellDamage(lo, hi) ] } ] }

  let wrath = rank24 wrathRanks

  /// Moonfire ranks 1-7 (levels 4-40): an Arcane hit and an Arcane burn, both
  /// parts Arcane — not Fire, and not Nature as some tables claim. Rank 1's burn
  /// is 9 s / 3 ticks; every rank after it is 12 s / 4 ticks. Rank 4 keeps the
  /// file's 40-49 up front (the table's 1.15.9 rank is 47-55) and the table's 80
  /// over 12 s, which is 7 a second.
  let moonfireRanks: AbilityDef =
    { Name = "Moonfire"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, cost, lo, hi, duration, perTick in
            [ 4, 25, 9, 12, 9.0, 1
              10, 50, 17, 21, 12.0, 3
              16, 75, 30, 37, 12.0, 5
              22, 105, 40, 49, 12.0, 7
              28, 150, 70, 82, 12.0, 11
              34, 190, 91, 108, 12.0, 14
              40, 235, 117, 137, 12.0, 18 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 6.0
              Range = 8
              Effects =
                [ SpellDamage(lo, hi); DamageOverTime(secTicks duration, perTick) ] } ] }

  let moonfire = rank24 moonfireRanks

  /// Smite ranks 1-6 (levels 1-38): 91-106 for 95 mana at rank 4. Those two
  /// numbers are the file's, hand-entered from a 1.12 source; the table's rank 4
  /// is 97-112, about 6 % higher, and the kept row keeps the ladder monotone.
  let smiteRanks: AbilityDef =
    { Name = "Smite"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = true
      Ranks =
        [ for level, cost, cast, lo, hi in
            [ 1, 20, 1.5, 15, 20
              6, 30, 2.0, 28, 34
              14, 60, 2.5, 58, 67
              22, 95, 2.5, 91, 106
              30, 140, 2.5, 158, 178
              38, 185, 2.5, 222, 250 ] do
            { Level = level
              Cost = cost
              CastTicks = secTicks cast
              CooldownTicks = ticks 0
              Range = 8
              Effects = [ SpellDamage(lo, hi) ] } ] }

  let smite = rank24 smiteRanks

  /// Shadow Word: Pain ranks 1-5 (levels 4-34): 18 s of Shadow damage on every
  /// rank, 30 / 66 / 132 / 234 / 366 in total. Rank 3's 132 over 18 s is 7 a
  /// second, which is the file's number (the table's is 7.3).
  let shadowWordPainRanks: AbilityDef =
    { Name = "Shadow Word: Pain"
      Resource = Mana
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, cost, perTick in [ 4, 25, 2; 10, 50, 4; 18, 95, 7; 26, 155, 13; 34, 230, 20 ] do
            { Level = level
              Cost = cost
              CastTicks = ticks 0
              CooldownTicks = secTicks 6.0
              Range = 8
              Effects = [ DamageOverTime(secTicks 18.0, perTick) ] } ] }

  let shadowWordPain = rank24 shadowWordPainRanks

  /// Charge ranks 1-2: it generates rage rather than costing it, so the cost is
  /// negative — 9 at rank 1, 12 at rank 2. Both ranks stun for 1 s and then hit,
  /// and the 8 tiles are the codebase's 40-yd convention for the table's 1.6-5.
  let chargeRanks: AbilityDef =
    { Name = "Charge"
      Resource = Rage
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, rage in [ 4, -9; 26, -12 ] do
            { Level = level
              Cost = rage
              CastTicks = ticks 0
              CooldownTicks = secTicks 15.0
              Range = 8
              Effects = [ Charge; Stun(secTicks 1.0); Threat(300) ] } ] }

  let charge = rank24 chargeRanks

  /// Disarm (level 18): the target's weapon for 10 s, for 20 rage on a 20 s
  /// cooldown. One rank, and a level-24 Warrior has it.
  let disarmRanks: AbilityDef =
    { Name = "Disarm"
      Resource = Rage
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ { Level = 18
            Cost = 20
            CastTicks = ticks 0
            CooldownTicks = secTicks 20.0
            Range = 1
            Effects = [ Disarm(secTicks 10.0); Threat(200) ] } ] }

  let disarm = rank24 disarmRanks

  /// Execute ranks 1-3 (levels 24, 32, 40): 125 / 200 / 325 damage, plus 3 / 6 / 9
  /// for every point of rage spent past the 15. Free of that scaling here — the
  /// sim has no rage-to-damage conversion and no 20 %-health gate, so each rank is
  /// its flat direct hit.
  let executeRanks: AbilityDef =
    { Name = "Execute"
      Resource = Rage
      Stance = None
      TargetKind = Foe
      Interruptible = false
      Ranks =
        [ for level, damage in [ 24, 125; 32, 200; 40, 325 ] do
            { Level = level
              Cost = 15
              CastTicks = ticks 0
              CooldownTicks = secTicks 10.0
              Range = 1
              Effects = [ Damage(damage, damage) ] } ] }

  let execute = rank24 executeRanks

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
      BonusHealing = 0
      Talents = Talents.none
      Totem = None
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
      BonusHealing = 0
      Talents = Talents.none
      Totem = None
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
       (79, 54, 72, 26, 35, 649, 0)
       (80, 56, 74, 26, 36, 681, 0)
       (82, 57, 76, 26, 36, 715, 0)
       (84, 58, 77, 26, 37, 761, 0)
       (86, 59, 79, 26, 37, 799, 0)
       (88, 60, 81, 27, 38, 839, 0)
       (90, 61, 83, 27, 38, 881, 0)
       (92, 63, 84, 27, 39, 935, 0)
       (94, 64, 86, 27, 39, 981, 0)
       (96, 65, 88, 28, 40, 1029, 0)
       (98, 66, 90, 28, 40, 1079, 0)
       (100, 68, 92, 28, 42, 1131, 0)
       (102, 69, 94, 28, 42, 1185, 0)
       (104, 70, 96, 28, 43, 1241, 0)
       (106, 72, 98, 29, 44, 1299, 0)
       (109, 73, 100, 29, 44, 1359, 0)
       (111, 74, 102, 29, 45, 1421, 0)
       (113, 76, 104, 29, 45, 1485, 0)
       (115, 77, 106, 30, 46, 1551, 0)
       (118, 79, 108, 30, 46, 1619, 0)
       (120, 80, 110, 30, 47, 1689, 0) |]

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
       (29, 31, 37, 78, 85, 627, 971)
       (29, 32, 38, 80, 87, 652, 1010)
       (29, 32, 38, 82, 89, 688, 1034)
       (29, 33, 39, 84, 91, 715, 1058)
       (30, 33, 39, 86, 93, 753, 1082)
       (30, 33, 40, 88, 95, 782, 1106)
       (30, 34, 41, 90, 97, 812, 1130)
       (31, 34, 41, 92, 99, 853, 1154)
       (31, 35, 42, 94, 102, 885, 1178)
       (31, 35, 43, 96, 105, 918, 1202)
       (32, 35, 43, 98, 107, 962, 1226)
       (32, 36, 44, 100, 109, 997, 1250)
       (32, 36, 44, 102, 111, 1043, 1274)
       (33, 37, 45, 104, 113, 1080, 1298)
       (33, 37, 46, 106, 116, 1118, 1322)
       (33, 38, 46, 109, 118, 1167, 1331)
       (34, 38, 47, 111, 120, 1207, 1355)
       (34, 39, 48, 113, 123, 1248, 1379)
       (34, 39, 49, 115, 126, 1290, 1403)
       (35, 40, 49, 118, 129, 1343, 1412)
       (35, 40, 50, 120, 131, 1387, 1436) |]

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
       (52, 89, 51, 29, 37, 703, 0)
       (53, 91, 52, 29, 38, 736, 0)
       (54, 93, 53, 29, 38, 770, 0)
       (55, 95, 54, 29, 39, 805, 0)
       (56, 98, 55, 30, 39, 841, 0)
       (58, 100, 56, 30, 40, 878, 0)
       (59, 102, 57, 30, 41, 916, 0)
       (60, 104, 58, 31, 41, 955, 0)
       (61, 106, 59, 31, 42, 995, 0)
       (62, 108, 61, 31, 43, 1026, 0)
       (64, 111, 62, 32, 43, 1068, 0)
       (65, 113, 63, 32, 44, 1111, 0)
       (66, 115, 64, 32, 44, 1155, 0)
       (67, 118, 65, 33, 45, 1200, 0)
       (69, 120, 66, 33, 46, 1246, 0)
       (70, 122, 68, 33, 46, 1283, 0)
       (71, 125, 69, 34, 47, 1331, 0)
       (73, 127, 70, 34, 48, 1380, 0)
       (74, 130, 71, 34, 49, 1430, 0)
       (76, 132, 73, 35, 49, 1471, 0)
       (77, 135, 74, 35, 50, 1523, 0) |]

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
       (21, 32, 33, 88, 78, 620, 913)
       (21, 32, 34, 90, 80, 643, 934)
       (21, 32, 34, 92, 82, 677, 955)
       (21, 32, 35, 95, 84, 702, 976)
       (21, 33, 35, 97, 86, 738, 997)
       (22, 33, 36, 99, 88, 765, 1018)
       (22, 33, 36, 101, 90, 803, 1039)
       (22, 34, 37, 103, 92, 832, 1060)
       (22, 34, 37, 106, 94, 872, 1081)
       (23, 34, 38, 108, 96, 903, 1102)
       (23, 35, 38, 110, 98, 945, 1108)
       (23, 35, 39, 112, 100, 978, 1129)
       (23, 35, 39, 114, 102, 1022, 1150)
       (23, 36, 40, 117, 104, 1057, 1171)
       (24, 36, 41, 120, 106, 1093, 1177)
       (24, 36, 41, 122, 109, 1140, 1198)
       (24, 37, 42, 124, 111, 1178, 1219)
       (24, 37, 42, 127, 113, 1227, 1225)
       (25, 37, 43, 129, 115, 1267, 1246)
       (25, 38, 43, 132, 118, 1318, 1252)
       (25, 38, 44, 133, 120, 1360, 1273) |]

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
       (43, 48, 48, 67, 72, 683, 854)
       (44, 49, 49, 68, 74, 714, 869)
       (45, 49, 50, 70, 76, 746, 899)
       (46, 50, 51, 71, 77, 779, 914)
       (47, 51, 51, 73, 79, 823, 944)
       (47, 52, 52, 74, 81, 858, 959)
       (48, 53, 53, 76, 83, 894, 989)
       (49, 53, 55, 77, 84, 921, 1004)
       (50, 54, 56, 79, 86, 959, 1019)
       (51, 55, 57, 81, 88, 998, 1049)
       (52, 56, 58, 82, 90, 1038, 1064)
       (53, 57, 59, 84, 92, 1079, 1079)
       (54, 58, 60, 86, 94, 1121, 1109)
       (55, 59, 61, 87, 96, 1164, 1124)
       (56, 59, 62, 89, 98, 1208, 1139)
       (57, 60, 63, 91, 100, 1253, 1154)
       (58, 61, 64, 93, 102, 1299, 1169)
       (59, 62, 65, 94, 104, 1346, 1199)
       (60, 63, 67, 96, 106, 1384, 1214)
       (61, 64, 68, 98, 108, 1433, 1229)
       (62, 65, 69, 100, 110, 1483, 1244) |]

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
       (47, 34, 46, 35, 38, 247, 525)
       (48, 34, 47, 36, 39, 266, 552)
       (50, 35, 48, 37, 40, 286, 579)
       (51, 36, 49, 37, 42, 307, 621)
       (52, 36, 50, 38, 43, 329, 648)
       (54, 37, 52, 39, 44, 342, 675)
       (55, 38, 53, 40, 44, 366, 702)
       (56, 39, 54, 41, 45, 391, 729)
       (58, 39, 56, 42, 46, 407, 756)
       (59, 40, 57, 42, 47, 434, 798)
       (61, 41, 58, 43, 48, 462, 825)
       (62, 42, 60, 44, 49, 481, 852)
       (64, 43, 61, 45, 50, 511, 879)
       (65, 43, 62, 46, 51, 542, 906)
       (67, 44, 64, 47, 52, 564, 933)
       (68, 45, 65, 48, 53, 597, 960)
       (70, 46, 67, 49, 54, 621, 987)
       (71, 47, 68, 50, 55, 656, 1014)
       (73, 47, 70, 51, 56, 682, 1041)
       (74, 48, 71, 52, 57, 719, 1068)
       (76, 49, 73, 52, 58, 747, 1110)
       (78, 50, 74, 53, 59, 786, 1137)
       (79, 51, 76, 54, 60, 816, 1164)
       (81, 52, 77, 56, 61, 857, 1176)
       (83, 53, 79, 57, 63, 889, 1203)
       (84, 54, 81, 58, 65, 922, 1230)
       (86, 55, 82, 59, 66, 966, 1257)
       (88, 56, 84, 60, 67, 1001, 1284)
       (90, 57, 86, 61, 68, 1037, 1311)
       (92, 58, 87, 62, 69, 1084, 1338)
       (93, 59, 89, 63, 70, 1122, 1365)
       (95, 60, 91, 64, 72, 1161, 1392)
       (97, 61, 93, 65, 73, 1201, 1419)
       (99, 62, 94, 66, 74, 1252, 1446)
       (101, 63, 96, 68, 75, 1294, 1458)
       (103, 64, 98, 69, 77, 1337, 1485)
       (105, 65, 100, 70, 78, 1381, 1512) |]

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
       (49, 30, 49, 34, 36, 247, 525)
       (50, 30, 50, 35, 37, 266, 552)
       (52, 31, 51, 36, 38, 286, 579)
       (53, 32, 52, 36, 39, 307, 621)
       (54, 32, 53, 37, 40, 329, 648)
       (56, 33, 55, 38, 41, 342, 675)
       (57, 34, 56, 39, 41, 366, 702)
       (58, 35, 57, 40, 42, 391, 729)
       (60, 35, 59, 41, 43, 407, 756)
       (61, 36, 60, 41, 44, 434, 798)
       (63, 37, 61, 42, 45, 462, 825)
       (64, 38, 63, 43, 46, 481, 852)
       (66, 39, 64, 44, 47, 511, 879)
       (67, 39, 65, 45, 48, 542, 906)
       (69, 40, 67, 46, 49, 564, 933)
       (70, 41, 68, 47, 50, 597, 960)
       (72, 42, 70, 48, 51, 621, 987)
       (73, 43, 71, 49, 52, 656, 1014)
       (75, 43, 73, 50, 53, 682, 1041)
       (76, 44, 74, 51, 54, 719, 1068)
       (78, 45, 76, 51, 55, 747, 1110)
       (80, 46, 77, 52, 56, 786, 1137)
       (81, 47, 79, 53, 57, 816, 1164)
       (83, 48, 80, 55, 58, 857, 1176)
       (85, 49, 82, 56, 59, 889, 1203)
       (86, 50, 84, 57, 61, 922, 1230)
       (88, 51, 85, 58, 62, 966, 1257)
       (90, 52, 87, 59, 63, 1001, 1284)
       (92, 53, 89, 60, 64, 1037, 1311)
       (94, 54, 90, 61, 65, 1084, 1338)
       (95, 55, 92, 62, 66, 1122, 1365)
       (97, 56, 94, 63, 68, 1161, 1392)
       (99, 57, 96, 64, 69, 1201, 1419)
       (101, 58, 97, 65, 70, 1252, 1446)
       (103, 59, 99, 67, 71, 1294, 1458)
       (105, 60, 101, 68, 73, 1337, 1485)
       (107, 61, 103, 69, 74, 1381, 1512) |]

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
       (43, 28, 45, 39, 49, 234, 476)
       (44, 28, 47, 40, 50, 240, 505)
       (45, 29, 48, 41, 51, 257, 579)
       (46, 29, 49, 42, 52, 275, 566)
       (47, 30, 50, 43, 53, 294, 598)
       (48, 30, 51, 44, 55, 314, 631)
       (49, 31, 52, 45, 56, 335, 718)
       (51, 31, 54, 47, 57, 347, 699)
       (52, 32, 55, 48, 59, 370, 733)
       (53, 33, 56, 49, 60, 394, 767)
       (54, 33, 57, 50, 61, 419, 786)
       (55, 34, 59, 51, 63, 435, 820)
       (56, 35, 60, 53, 64, 462, 854)
       (57, 35, 61, 54, 65, 490, 888)
       (58, 36, 63, 55, 67, 509, 922)
       (59, 36, 64, 56, 68, 539, 941)
       (61, 37, 65, 58, 70, 570, 975)
       (62, 38, 67, 59, 71, 592, 1009)
       (63, 38, 68, 60, 73, 625, 1028)
       (64, 39, 70, 61, 74, 649, 1062)
       (66, 40, 71, 63, 76, 684, 1096)
       (67, 40, 73, 64, 77, 710, 1115)
       (68, 41, 74, 66, 79, 747, 1149)
       (69, 42, 76, 67, 80, 775, 1183)
       (71, 43, 77, 68, 82, 814, 1202)
       (72, 43, 79, 70, 84, 844, 1236)
       (73, 44, 80, 71, 85, 885, 1255)
       (75, 45, 82, 73, 87, 917, 1289)
       (76, 46, 83, 74, 89, 960, 1313)
       (78, 46, 85, 76, 90, 994, 1342)
       (79, 47, 87, 77, 92, 1029, 1376)
       (80, 48, 88, 79, 94, 1075, 1395)
       (82, 49, 90, 80, 96, 1112, 1414)
       (83, 50, 92, 82, 97, 1150, 1448)
       (85, 50, 93, 84, 99, 1199, 1467)
       (86, 51, 95, 85, 101, 1239, 1501)
       (88, 52, 97, 87, 103, 1280, 1520) |]

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
       (41, 33, 44, 38, 47, 234, 476)
       (42, 33, 46, 39, 48, 240, 505)
       (43, 34, 47, 40, 49, 257, 579)
       (44, 34, 48, 41, 50, 275, 566)
       (45, 35, 49, 42, 51, 294, 598)
       (46, 35, 50, 43, 53, 314, 631)
       (47, 36, 51, 44, 54, 335, 718)
       (49, 36, 53, 46, 55, 347, 699)
       (50, 37, 54, 47, 57, 370, 733)
       (51, 38, 55, 48, 58, 394, 767)
       (52, 38, 56, 49, 59, 419, 786)
       (53, 39, 58, 50, 61, 435, 820)
       (54, 40, 59, 52, 62, 462, 854)
       (55, 40, 60, 53, 63, 490, 888)
       (56, 41, 62, 54, 65, 509, 922)
       (57, 41, 63, 55, 66, 539, 941)
       (59, 42, 64, 57, 68, 570, 975)
       (60, 43, 66, 58, 69, 592, 1009)
       (61, 43, 67, 59, 71, 625, 1028)
       (62, 44, 69, 60, 72, 649, 1062)
       (64, 45, 70, 62, 74, 684, 1096)
       (65, 45, 72, 63, 75, 710, 1115)
       (66, 46, 73, 65, 77, 747, 1149)
       (67, 47, 75, 66, 78, 775, 1183)
       (69, 48, 76, 67, 80, 814, 1202)
       (70, 48, 78, 69, 82, 844, 1236)
       (71, 49, 79, 70, 83, 885, 1255)
       (73, 50, 81, 72, 85, 917, 1289)
       (74, 51, 82, 73, 87, 960, 1313)
       (76, 51, 84, 75, 88, 994, 1342)
       (77, 52, 86, 76, 90, 1029, 1376)
       (78, 53, 87, 78, 92, 1075, 1395)
       (80, 54, 89, 79, 94, 1112, 1414)
       (81, 55, 91, 81, 95, 1150, 1448)
       (83, 55, 92, 83, 97, 1199, 1467)
       (84, 56, 94, 84, 99, 1239, 1501)
       (86, 57, 96, 86, 101, 1280, 1520) |]

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
       (45, 26, 45, 37, 48, 234, 476)
       (46, 26, 47, 38, 49, 240, 505)
       (47, 27, 48, 39, 50, 257, 579)
       (48, 27, 49, 40, 51, 275, 566)
       (49, 28, 50, 41, 52, 294, 598)
       (50, 28, 51, 42, 54, 314, 631)
       (51, 29, 52, 43, 55, 335, 718)
       (53, 29, 54, 45, 56, 347, 699)
       (54, 30, 55, 46, 58, 370, 733)
       (55, 31, 56, 47, 59, 394, 767)
       (56, 31, 57, 48, 60, 419, 786)
       (57, 32, 59, 49, 62, 435, 820)
       (58, 33, 60, 51, 63, 462, 854)
       (59, 33, 61, 52, 64, 490, 888)
       (60, 34, 63, 53, 66, 509, 922)
       (61, 34, 64, 54, 67, 539, 941)
       (63, 35, 65, 56, 69, 570, 975)
       (64, 36, 67, 57, 70, 592, 1009)
       (65, 36, 68, 58, 72, 625, 1028)
       (66, 37, 70, 59, 73, 649, 1062)
       (68, 38, 71, 61, 75, 684, 1096)
       (69, 38, 73, 62, 76, 710, 1115)
       (70, 39, 74, 64, 78, 747, 1149)
       (71, 40, 76, 65, 79, 775, 1183)
       (73, 41, 77, 66, 81, 814, 1202)
       (74, 41, 79, 68, 83, 844, 1236)
       (75, 42, 80, 69, 84, 885, 1255)
       (77, 43, 82, 71, 86, 917, 1289)
       (78, 44, 83, 72, 88, 960, 1313)
       (80, 44, 85, 74, 89, 994, 1342)
       (81, 45, 87, 75, 91, 1029, 1376)
       (82, 46, 88, 77, 93, 1075, 1395)
       (84, 47, 90, 78, 95, 1112, 1414)
       (85, 48, 92, 80, 96, 1150, 1448)
       (87, 48, 93, 82, 98, 1199, 1467)
       (88, 49, 95, 83, 100, 1239, 1501)
       (90, 50, 97, 85, 102, 1280, 1520) |]

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
       (28, 59, 41, 34, 36, 273, 568)
       (28, 60, 42, 34, 37, 292, 611)
       (29, 62, 43, 35, 37, 312, 640)
       (29, 64, 44, 36, 38, 333, 670)
       (30, 65, 45, 36, 39, 355, 715)
       (30, 67, 46, 37, 40, 378, 745)
       (31, 69, 47, 38, 40, 402, 775)
       (31, 70, 49, 39, 41, 417, 805)
       (32, 72, 50, 39, 42, 443, 850)
       (33, 74, 51, 40, 43, 470, 880)
       (33, 75, 52, 41, 44, 498, 910)
       (34, 77, 53, 42, 45, 527, 940)
       (35, 79, 55, 43, 46, 547, 970)
       (35, 81, 56, 43, 46, 578, 1015)
       (36, 83, 57, 44, 47, 610, 1045)
       (36, 85, 58, 45, 48, 643, 1075)
       (37, 86, 60, 46, 49, 667, 1105)
       (38, 88, 61, 47, 50, 702, 1135)
       (38, 90, 62, 47, 51, 738, 1180)
       (39, 92, 63, 48, 52, 775, 1210)
       (40, 94, 65, 49, 53, 803, 1240)
       (40, 96, 66, 50, 54, 842, 1270)
       (41, 98, 68, 51, 55, 872, 1300)
       (42, 100, 69, 52, 56, 913, 1330)
       (43, 103, 70, 53, 57, 955, 1360)
       (43, 105, 72, 54, 58, 988, 1390)
       (44, 107, 73, 55, 59, 1032, 1420)
       (45, 109, 75, 56, 60, 1067, 1450)
       (46, 111, 76, 57, 61, 1113, 1480)
       (46, 113, 78, 58, 62, 1150, 1510)
       (47, 116, 79, 59, 63, 1198, 1540)
       (48, 118, 81, 60, 64, 1237, 1570)
       (49, 120, 82, 61, 65, 1287, 1600)
       (50, 123, 84, 62, 67, 1328, 1630)
       (50, 125, 86, 63, 68, 1370, 1660)
       (51, 128, 87, 64, 69, 1423, 1690)
       (52, 130, 89, 65, 70, 1467, 1720) |]

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
       (33, 50, 45, 33, 35, 273, 568)
       (33, 51, 46, 33, 36, 292, 611)
       (34, 53, 47, 34, 36, 312, 640)
       (34, 55, 48, 35, 37, 333, 670)
       (35, 56, 49, 35, 38, 355, 715)
       (35, 58, 50, 36, 39, 378, 745)
       (36, 60, 51, 37, 39, 402, 775)
       (36, 61, 53, 38, 40, 417, 805)
       (37, 63, 54, 38, 41, 443, 850)
       (38, 65, 55, 39, 42, 470, 880)
       (38, 66, 56, 40, 43, 498, 910)
       (39, 68, 57, 41, 44, 527, 940)
       (40, 70, 59, 42, 45, 547, 970)
       (40, 72, 60, 42, 45, 578, 1015)
       (41, 74, 61, 43, 46, 610, 1045)
       (41, 76, 62, 44, 47, 643, 1075)
       (42, 77, 64, 45, 48, 667, 1105)
       (43, 79, 65, 46, 49, 702, 1135)
       (43, 81, 66, 46, 50, 738, 1180)
       (44, 83, 67, 47, 51, 775, 1210)
       (45, 85, 69, 48, 52, 803, 1240)
       (45, 87, 70, 49, 53, 842, 1270)
       (46, 89, 72, 50, 54, 872, 1300)
       (47, 91, 73, 51, 55, 913, 1330)
       (48, 94, 74, 52, 56, 955, 1360)
       (48, 96, 76, 53, 57, 988, 1390)
       (49, 98, 77, 54, 58, 1032, 1420)
       (50, 100, 79, 55, 59, 1067, 1450)
       (51, 102, 80, 56, 60, 1113, 1480)
       (51, 104, 82, 57, 61, 1150, 1510)
       (52, 107, 83, 58, 62, 1198, 1540)
       (53, 109, 85, 59, 63, 1237, 1570)
       (54, 111, 86, 60, 64, 1287, 1600)
       (55, 114, 88, 61, 66, 1328, 1630)
       (55, 116, 90, 62, 67, 1370, 1660)
       (56, 119, 91, 63, 68, 1423, 1690)
       (57, 121, 93, 64, 69, 1467, 1720) |]

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
       (32, 56, 43, 30, 37, 273, 568)
       (32, 57, 44, 30, 38, 292, 611)
       (33, 59, 45, 31, 38, 312, 640)
       (33, 61, 46, 32, 39, 333, 670)
       (34, 62, 47, 32, 40, 355, 715)
       (34, 64, 48, 33, 41, 378, 745)
       (35, 66, 49, 34, 41, 402, 775)
       (35, 67, 51, 35, 42, 417, 805)
       (36, 69, 52, 35, 43, 443, 850)
       (37, 71, 53, 36, 44, 470, 880)
       (37, 72, 54, 37, 45, 498, 910)
       (38, 74, 55, 38, 46, 527, 940)
       (39, 76, 57, 39, 47, 547, 970)
       (39, 78, 58, 39, 47, 578, 1015)
       (40, 80, 59, 40, 48, 610, 1045)
       (40, 82, 60, 41, 49, 643, 1075)
       (41, 83, 62, 42, 50, 667, 1105)
       (42, 85, 63, 43, 51, 702, 1135)
       (42, 87, 64, 43, 52, 738, 1180)
       (43, 89, 65, 44, 53, 775, 1210)
       (44, 91, 67, 45, 54, 803, 1240)
       (44, 93, 68, 46, 55, 842, 1270)
       (45, 95, 70, 47, 56, 872, 1300)
       (46, 97, 71, 48, 57, 913, 1330)
       (47, 100, 72, 49, 58, 955, 1360)
       (47, 102, 74, 50, 59, 988, 1390)
       (48, 104, 75, 51, 60, 1032, 1420)
       (49, 106, 77, 52, 61, 1067, 1450)
       (50, 108, 78, 53, 62, 1113, 1480)
       (50, 110, 80, 54, 63, 1150, 1510)
       (51, 113, 81, 55, 64, 1198, 1540)
       (52, 115, 83, 56, 65, 1237, 1570)
       (53, 117, 84, 57, 66, 1287, 1600)
       (54, 120, 86, 58, 68, 1328, 1630)
       (54, 122, 88, 59, 69, 1370, 1660)
       (55, 125, 89, 60, 70, 1423, 1690)
       (56, 127, 91, 61, 71, 1467, 1720) |]

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
       (34, 51, 44, 31, 39, 273, 568)
       (34, 52, 45, 31, 40, 292, 611)
       (35, 54, 46, 32, 40, 312, 640)
       (35, 56, 47, 33, 41, 333, 670)
       (36, 57, 48, 33, 42, 355, 715)
       (36, 59, 49, 34, 43, 378, 745)
       (37, 61, 50, 35, 43, 402, 775)
       (37, 62, 52, 36, 44, 417, 805)
       (38, 64, 53, 36, 45, 443, 850)
       (39, 66, 54, 37, 46, 470, 880)
       (39, 67, 55, 38, 47, 498, 910)
       (40, 69, 56, 39, 48, 527, 940)
       (41, 71, 58, 40, 49, 547, 970)
       (41, 73, 59, 40, 49, 578, 1015)
       (42, 75, 60, 41, 50, 610, 1045)
       (42, 77, 61, 42, 51, 643, 1075)
       (43, 78, 63, 43, 52, 667, 1105)
       (44, 80, 64, 44, 53, 702, 1135)
       (44, 82, 65, 44, 54, 738, 1180)
       (45, 84, 66, 45, 55, 775, 1210)
       (46, 86, 68, 46, 56, 803, 1240)
       (46, 88, 69, 47, 57, 842, 1270)
       (47, 90, 71, 48, 58, 872, 1300)
       (48, 92, 72, 49, 59, 913, 1330)
       (49, 95, 73, 50, 60, 955, 1360)
       (49, 97, 75, 51, 61, 988, 1390)
       (50, 99, 76, 52, 62, 1032, 1420)
       (51, 101, 78, 53, 63, 1067, 1450)
       (52, 103, 79, 54, 64, 1113, 1480)
       (52, 105, 81, 55, 65, 1150, 1510)
       (53, 108, 82, 56, 66, 1198, 1540)
       (54, 110, 84, 57, 67, 1237, 1570)
       (55, 112, 85, 58, 68, 1287, 1600)
       (56, 115, 87, 59, 70, 1328, 1630)
       (56, 117, 89, 60, 71, 1370, 1660)
       (57, 120, 90, 61, 72, 1423, 1690)
       (58, 122, 92, 62, 73, 1467, 1720) |]

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
       (36, 49, 44, 29, 38, 273, 568)
       (36, 50, 45, 29, 39, 292, 611)
       (37, 52, 46, 30, 39, 312, 640)
       (37, 54, 47, 31, 40, 333, 670)
       (38, 55, 48, 31, 41, 355, 715)
       (38, 57, 49, 32, 42, 378, 745)
       (39, 59, 50, 33, 42, 402, 775)
       (39, 60, 52, 34, 43, 417, 805)
       (40, 62, 53, 34, 44, 443, 850)
       (41, 64, 54, 35, 45, 470, 880)
       (41, 65, 55, 36, 46, 498, 910)
       (42, 67, 56, 37, 47, 527, 940)
       (43, 69, 58, 38, 48, 547, 970)
       (43, 71, 59, 38, 48, 578, 1015)
       (44, 73, 60, 39, 49, 610, 1045)
       (44, 75, 61, 40, 50, 643, 1075)
       (45, 76, 63, 41, 51, 667, 1105)
       (46, 78, 64, 42, 52, 702, 1135)
       (46, 80, 65, 42, 53, 738, 1180)
       (47, 82, 66, 43, 54, 775, 1210)
       (48, 84, 68, 44, 55, 803, 1240)
       (48, 86, 69, 45, 56, 842, 1270)
       (49, 88, 71, 46, 57, 872, 1300)
       (50, 90, 72, 47, 58, 913, 1330)
       (51, 93, 73, 48, 59, 955, 1360)
       (51, 95, 75, 49, 60, 988, 1390)
       (52, 97, 76, 50, 61, 1032, 1420)
       (53, 99, 78, 51, 62, 1067, 1450)
       (54, 101, 79, 52, 63, 1113, 1480)
       (54, 103, 81, 53, 64, 1150, 1510)
       (55, 106, 82, 54, 65, 1198, 1540)
       (56, 108, 84, 55, 66, 1237, 1570)
       (57, 110, 85, 56, 67, 1287, 1600)
       (58, 113, 87, 57, 69, 1328, 1630)
       (58, 115, 89, 58, 70, 1370, 1660)
       (59, 118, 90, 59, 71, 1423, 1690)
       (60, 120, 92, 60, 72, 1467, 1720) |]

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
       (23, 32, 33, 55, 50, 266, 467)
       (23, 33, 34, 56, 52, 279, 498)
       (23, 33, 35, 57, 53, 293, 530)
       (24, 33, 35, 59, 54, 318, 548)
       (24, 34, 36, 60, 56, 334, 582)
       (25, 34, 37, 62, 57, 351, 602)
       (25, 35, 37, 63, 59, 379, 638)
       (25, 35, 37, 64, 62, 398, 674)
       (26, 36, 39, 66, 62, 418, 695)
       (26, 36, 40, 67, 64, 439, 731)
       (27, 37, 40, 69, 65, 471, 752)
       (27, 37, 41, 70, 67, 494, 788)
       (28, 38, 42, 72, 69, 518, 809)
       (28, 39, 43, 75, 69, 543, 830)
       (28, 39, 44, 76, 72, 569, 866)
       (29, 40, 44, 78, 74, 606, 887)
       (29, 40, 45, 79, 75, 634, 923)
       (30, 41, 46, 81, 77, 663, 944)
       (30, 41, 47, 83, 79, 693, 965)
       (31, 42, 48, 84, 81, 724, 1001)
       (31, 42, 49, 86, 82, 756, 1022)
       (32, 43, 49, 88, 84, 799, 1043)
       (32, 44, 50, 90, 86, 823, 1064)
       (33, 44, 51, 91, 88, 868, 1100)
       (33, 45, 52, 93, 90, 904, 1121)
       (34, 46, 53, 96, 92, 941, 1142)
       (34, 46, 54, 98, 93, 979, 1163)
       (35, 47, 55, 100, 96, 1018, 1184)
       (35, 47, 56, 102, 98, 1058, 1205)
       (36, 48, 57, 104, 100, 1089, 1226)
       (37, 49, 58, 106, 102, 1131, 1247)
       (37, 49, 58, 108, 104, 1184, 1268)
       (38, 50, 60, 110, 106, 1228, 1289)
       (38, 51, 61, 112, 108, 1273, 1310)
       (39, 52, 62, 114, 111, 1319, 1331)
       (39, 52, 63, 117, 111, 1366, 1352)
       (40, 53, 64, 119, 115, 1414, 1373) |]

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
       (28, 29, 34, 49, 52, 266, 467)
       (28, 30, 35, 50, 54, 279, 498)
       (28, 30, 36, 51, 55, 293, 530)
       (29, 30, 36, 53, 56, 318, 548)
       (29, 31, 37, 54, 58, 334, 582)
       (30, 31, 38, 56, 59, 351, 602)
       (30, 32, 38, 57, 61, 379, 638)
       (30, 32, 39, 58, 64, 398, 674)
       (31, 33, 40, 60, 65, 418, 695)
       (31, 33, 41, 61, 67, 439, 731)
       (32, 34, 41, 63, 68, 471, 752)
       (32, 34, 42, 64, 70, 494, 788)
       (33, 35, 43, 66, 72, 518, 809)
       (33, 36, 44, 68, 73, 543, 830)
       (33, 36, 45, 69, 75, 569, 866)
       (34, 37, 45, 71, 77, 606, 887)
       (34, 37, 46, 72, 78, 634, 923)
       (35, 38, 47, 74, 80, 663, 944)
       (35, 38, 48, 76, 82, 693, 965)
       (36, 39, 48, 77, 86, 724, 1001)
       (36, 39, 50, 79, 86, 756, 1022)
       (37, 40, 50, 81, 88, 799, 1043)
       (37, 41, 51, 83, 90, 823, 1064)
       (38, 41, 52, 84, 92, 868, 1100)
       (38, 42, 53, 86, 94, 904, 1121)
       (39, 43, 54, 88, 96, 941, 1142)
       (39, 43, 55, 90, 98, 979, 1163)
       (40, 44, 56, 92, 100, 1018, 1184)
       (40, 44, 57, 94, 102, 1058, 1205)
       (41, 45, 58, 96, 105, 1089, 1226)
       (42, 46, 59, 98, 107, 1131, 1247)
       (42, 46, 60, 100, 109, 1184, 1268)
       (43, 47, 61, 102, 111, 1228, 1289)
       (43, 48, 62, 104, 113, 1273, 1310)
       (44, 49, 63, 106, 116, 1319, 1331)
       (44, 49, 64, 108, 118, 1366, 1352)
       (45, 50, 65, 110, 120, 1414, 1373) |]

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
       (31, 26, 35, 46, 53, 266, 467)
       (31, 27, 36, 47, 55, 279, 498)
       (31, 27, 37, 48, 56, 293, 530)
       (32, 27, 37, 50, 57, 318, 548)
       (32, 28, 38, 51, 59, 334, 582)
       (33, 28, 39, 53, 60, 351, 602)
       (33, 29, 39, 54, 62, 379, 638)
       (33, 29, 40, 55, 64, 398, 674)
       (34, 30, 41, 57, 65, 418, 695)
       (34, 30, 41, 58, 68, 439, 731)
       (35, 31, 42, 60, 68, 471, 752)
       (35, 31, 43, 61, 70, 494, 788)
       (36, 32, 44, 63, 71, 518, 809)
       (36, 33, 45, 65, 73, 543, 830)
       (36, 33, 46, 66, 75, 569, 866)
       (37, 34, 46, 68, 77, 606, 887)
       (37, 34, 47, 69, 78, 634, 923)
       (38, 35, 47, 71, 80, 663, 944)
       (38, 35, 47, 73, 83, 693, 965)
       (39, 36, 50, 74, 84, 724, 1001)
       (39, 36, 51, 76, 85, 756, 1022)
       (40, 37, 51, 78, 87, 799, 1043)
       (40, 38, 52, 80, 89, 823, 1064)
       (41, 38, 53, 81, 91, 868, 1100)
       (41, 39, 53, 83, 93, 904, 1121)
       (42, 40, 54, 85, 96, 941, 1142)
       (42, 40, 56, 87, 97, 979, 1163)
       (43, 41, 56, 89, 99, 1018, 1184)
       (43, 41, 58, 91, 101, 1058, 1205)
       (44, 42, 59, 93, 103, 1089, 1226)
       (45, 43, 60, 95, 105, 1131, 1247)
       (45, 43, 61, 97, 106, 1184, 1268)
       (46, 44, 62, 99, 109, 1228, 1289)
       (46, 45, 62, 101, 110, 1273, 1310)
       (47, 46, 64, 103, 114, 1319, 1331)
       (47, 46, 65, 105, 116, 1366, 1352)
       (48, 47, 66, 107, 118, 1414, 1373) |]

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
       (27, 27, 35, 47, 55, 266, 467)
       (27, 28, 36, 48, 57, 279, 498)
       (27, 28, 37, 49, 58, 293, 530)
       (28, 28, 37, 51, 59, 318, 548)
       (28, 29, 38, 52, 61, 334, 582)
       (29, 29, 39, 54, 62, 351, 602)
       (29, 30, 39, 55, 64, 379, 638)
       (29, 30, 40, 56, 66, 398, 674)
       (30, 31, 41, 58, 67, 418, 695)
       (30, 31, 42, 59, 69, 439, 731)
       (31, 32, 42, 61, 70, 471, 752)
       (31, 32, 43, 62, 72, 494, 788)
       (32, 33, 43, 64, 75, 518, 809)
       (32, 34, 45, 66, 75, 543, 830)
       (32, 34, 46, 67, 77, 569, 866)
       (33, 35, 46, 69, 79, 606, 887)
       (33, 35, 47, 70, 80, 634, 923)
       (34, 36, 48, 72, 82, 663, 944)
       (34, 36, 48, 74, 85, 693, 965)
       (35, 37, 50, 75, 86, 724, 1001)
       (35, 37, 51, 77, 87, 756, 1022)
       (36, 38, 51, 79, 89, 799, 1043)
       (36, 39, 52, 81, 90, 823, 1064)
       (37, 39, 53, 82, 93, 868, 1100)
       (37, 40, 54, 84, 95, 904, 1121)
       (38, 41, 55, 86, 96, 941, 1142)
       (38, 41, 56, 88, 99, 979, 1163)
       (39, 42, 57, 90, 101, 1018, 1184)
       (39, 42, 58, 92, 103, 1058, 1205)
       (40, 43, 59, 94, 105, 1089, 1226)
       (41, 44, 60, 96, 107, 1131, 1247)
       (41, 44, 60, 98, 110, 1184, 1268)
       (42, 45, 62, 100, 111, 1228, 1289)
       (42, 46, 63, 102, 113, 1273, 1310)
       (43, 47, 63, 104, 116, 1319, 1331)
       (43, 47, 65, 106, 118, 1366, 1352)
       (44, 48, 66, 108, 120, 1414, 1373) |]

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
      FixedPool: int option
      /// The class's pre-raid best-in-slot outfit at 60 as (armour, stats) — what
      /// the level-24 pieces grow into (`.pi/research/levelscale/bis-60.md`).
      Gear60: int * (int * int * int * int * int)
      /// The build this character runs. `Talents.none` for everything that does
      /// not have one, which is every class but the four healers.
      Talents: Talents }

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
  /// The last entry whose floor is at or below `level`; the first entry when the
  /// level is below every floor. Used for the weapons, whose floors are per class.
  ///
  /// The last rung of each weapon ladder is the class's researched pre-raid
  /// best-in-slot pick at level 60 (`.pi/research/levelscale/bis-60.md`), so a
  /// level-60 party swings what the research says it wears. The Paladin's and
  /// Shaman's level-60 pick is a fast caster one-hander that would swing weaker
  /// than their level-40 weapon, so those two rungs keep its speed but carry the
  /// level-40 rung's damage per second — the same never-go-backwards rule the
  /// armour and stats follow.
  let private rungAt (bands: (int * 'a) list) (level: int) : 'a =
    match bands |> List.filter (fun (floor, _) -> floor <= level) |> List.tryLast with
    | Some(_, value) -> value
    | None -> snd (List.head bands)

  let private dress (sheet: Sheet) (level: int) : Dressed =
    let str, agi, sta, intel, spi, baseHealth, baseMana =
      sheet.Base.[max 0 (min (sheet.Base.Length - 1) (level - 1))]

    // The level-24 set, piece by piece, and no piece whose requirement is above the
    // level: that is what makes a level-18 Warrior miss the five items that need 24.
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

    let zero = { Strength = 0; Agility = 0; Stamina = 0; Intellect = 0; Spirit = 0 }
    let gear = List.fold add zero worn

    // Past 24 the outfit grows into the class's level-60 pre-raid best-in-slot set,
    // each stat along its own ratio so plate armour climbs 5x while cloth climbs 2x.
    // 36 is the distance between the two anchors, so level 60 lands on the
    // researched totals and every level between follows a straight line in the log.
    // A stat the level-24 set does not carry at all grows from zero instead.
    let t = float (max 0 (level - 24)) / 36.0
    let g60Armour, (g60Str, g60Agi, g60Sta, g60Int, g60Spi) = sheet.Gear60

    // The researched caster sets drop a little Strength and Agility for Intellect,
    // so the level-24 value is the floor: no class loses a stat on the way up.
    let grow (at24: int) (at60: int) =
      let at60 = max at24 at60
      if t = 0.0 then at24
      elif at24 <= 0 then int (System.Math.Round(float at60 * t))
      else int (System.Math.Round(float at24 * (float at60 / float at24) ** t))

    { Stats =
        { Strength = str + grow gear.Strength g60Str
          Agility = agi + grow gear.Agility g60Agi
          Stamina = sta + grow gear.Stamina g60Sta
          Intellect = intel + grow gear.Intellect g60Int
          Spirit = spi + grow gear.Spirit g60Spi }
      Armor = grow (worn |> List.sumBy (fun g -> g.Armor)) g60Armour
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
    let talents = sheet.Talents

    // Ancestral Knowledge and Divine Intellect are a share of Intellect taken
    // before the pool is derived, which is why they beat a flat bonus outright:
    // one point of Intellect is fifteen mana, not one.
    let intellect = s.Intellect * (10000 + talents.IntellectBp) / 10000
    // Spiritual Guidance adds a share of Spirit to every direct heal. It lands on
    // the entity so `resolveAbility` can add it without knowing about talents.
    let bonusHealing = s.Spirit * talents.SpiritToHealingBp / 10000
    // shortcut: combat regeneration is a flat 1 mana a Tick, so Reflection's and
    // Meditation's 15% floors away to nothing; give regen a fractional
    // accumulator when Spirit is what drives it.
    let regen = partyRegen * (10000 + talents.RegenBp) / 10000

    {| MaxHealth = d.BaseHealth + pool s.Stamina 10
       MaxResource = defaultArg sheet.FixedPool (d.BaseMana + pool intellect 15)
       RegenPerTick = regen
       BonusHealing = bonusHealing
       CritChanceBp = 500 + s.Agility * 10 + traits.CritBonusBp
       DodgeChanceBp = 500 + s.Agility * 5 + traits.DodgeBonusBp
       Armor = d.Armor + armorBp + s.Agility * 2 + traits.ArmorBonus
       BlockChanceBp = traits.BlockChanceBp
       BlockValue = traits.BlockValue
       AttackPower = s.Strength * sheet.ApPerStrength + s.Agility * sheet.ApPerAgility + attackPower |}

  /// A Party member at one level. The passives are applied to the stats before
  /// the derived values are computed.
  ///
  /// Every class trains the researched rank ladder, and `AbilityDef.at` picks the
  /// newest rank at or below the character's level.
  let heroStats
    (buffs: PartyBuffKind list)
    (level: int)
    (id: int)
    (name: string)
    (role: Role)
    (armorClass: ArmorClass)
    (traits: Traits)
    (sheet: Sheet)
    (weapons: (int * AutoAttack) list)
    (resource: ResourceKind)
    (threatBp: int)
    (abilities: Ability list)
    (pos: Pos)
    : Entity =
    let dressed = dress sheet level
    let d = derive buffs sheet dressed traits
    let attack = rungAt weapons level


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
      BonusHealing = d.BonusHealing
      Talents = sheet.Talents
      Totem = None
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

  // =========================================================================
  // The four level-60 healer builds
  // =========================================================================

  // One build per healing class, as the points a level-60 actually spends. Each
  // is written as the talents it buys in the order the tree hands them out, so a
  // reader can check it against a talent calculator; what the record cannot
  // express — a stack, a cooldown button, a proc — is named in the comment beside
  // the build and left out rather than half-modelled.

  /// The totem buttons a Shaman's tree discounts. Totemic Focus is -25% on all of
  /// them, which is a per-spell cost line and nothing else.
  let private totemDiscount =
    [ for name in
        [ "Healing Stream Totem"
          "Mana Spring Totem"
          "Stoneskin Totem"
          "Grace of Air Totem"
          "Mana Tide Totem" ] do
        { Spell = name
          CostBp = 2500
          HealBp = 0
          CastTicksOff = ticks 0 } ]

  /// Shaman 0/5/46: Purification 5/5 (+10% healing), Improved Healing Wave 5/5
  /// (-0.5 s cast), Tidal Focus 5/5 (-5% healing mana), Tidal Mastery 5/5 (+5%
  /// crit), Healing Focus 5/5 (-70% pushback), Ancestral Knowledge 5/5 (+5% Int),
  /// Restorative Totems 5/5 (+25% totem effect), Totemic Focus 5/5 (-25% totem
  /// mana) and Healing Way 3/3 (+18% a stack to the target's next Healing Wave).
  /// Ancestral Healing 3/3 comes in beside it as `CritArmorBp`.
  ///
  /// shortcut: Improved Reincarnation is modelled as the cooldown cut alone
  /// (`.pi/research/level24/shaman.md` §5) — its +20% health and mana at 2/2 is
  /// already baked into `reincarnation`'s fraction, the same way a rank's numbers
  /// are baked into its `Ability`.
  let private shamanTalents =
    { Talents.none with
        HealDoneBp = 1000
        HealCostBp = 500
        IntellectBp = 500
        TotemEffectBp = 2500
        CritArmorBp = 2500
        HealingWayBp = 1800
        CooldownOff = [ "Reincarnation", secTicks -600.0 ]
        Spells =
          { Spell = "Healing Wave"
            CostBp = 0
            HealBp = 0
            CastTicksOff = secTicks 0.5 }
          :: totemDiscount }

  /// Paladin 31/20/0, the standard PvE holy build: Divine Intellect 5/5 (+10%
  /// Int), Spiritual Focus 5/5 (-70% pushback), Healing Light 3/3 (+12% Flash of
  /// Light), Illumination 5/5 (a heal crit refunds its whole cost) and Holy Power
  /// 5/5 (+5% crit). Divine Favor is in the kit as a cooldown.
  let private paladinTalents =
    { Talents.none with
        IntellectBp = 1000
        ManaRefundOnCritBp = 10000
        Spells =
          [ { Spell = "Flash of Light"
              CostBp = 0
              HealBp = 1200
              CastTicksOff = ticks 0 } ] }

  /// Druid 5/0/46, Moonglow into Swiftmend: Gift of Nature 5/5 (+10% healing),
  /// Tranquil Spirit 5/5 (-10% Healing Touch mana), Improved Rejuvenation 3/3
  /// (+15% Rejuvenation), Nature's Focus 5/5 (-70% pushback) and Reflection 3/3
  /// (+15% mana regen while casting). Swiftmend is in the kit as a cooldown.
  ///
  /// shortcut: Nature's Swiftness is a Balance-side talent and this build spends
  /// nothing there, so the Druid's copy of the button is not in his kit.
  let private druidTalents =
    { Talents.none with
        HealDoneBp = 1000
        RegenBp = 1500
        Spells =
          [ { Spell = "Healing Touch"
              CostBp = 1000
              HealBp = 0
              CastTicksOff = ticks 0 }
            { Spell = "Rejuvenation"
              CostBp = 0
              HealBp = 1500
              CastTicksOff = ticks 0 } ] }

  /// Priest 21/30/0, deep holy: Spiritual Healing 5/5 (+10% healing), Improved
  /// Healing 3/3 (-15% Lesser/Greater Heal mana), Holy Specialization 5/5 (+5%
  /// crit), Spiritual Guidance 5/5 (+25% of Spirit as bonus healing),
  /// Meditation 3/3 (+15% mana regen while casting) and Inspiration 3/3 (+25%
  /// armour for fifteen seconds after a heal crit). Inner Focus is in the kit as a
  /// cooldown.
  ///
  /// shortcut: Power Infusion is not modelled — it is a cooldown on another
  /// character's spell damage, and nothing in the vocabulary buffs a caster.
  let private priestTalents =
    { Talents.none with
        HealDoneBp = 1000
        RegenBp = 1500
        SpiritToHealingBp = 2500
        CritArmorBp = 2500
        Spells =
          [ for name in [ "Lesser Heal"; "Heal" ] do
              { Spell = name
                CostBp = 1500
                HealBp = 0
                CastTicksOff = ticks 0 } ] }

  /// Fold a build's per-spell lines into the kit: a talent that shortens a cast
  /// or cheapens a spell changes the `Ability` record once, at construction, so
  /// the tick loop never learns that talents exist.
  let private withTalents (t: Talents) (kit: Ability list) =
    let heals (a: Ability) =
      a.Effects |> List.exists (function Heal _ | HealOverTime _ -> true | _ -> false)

    kit
    |> List.map (fun a ->
      { a with
          ResourceCost = a.ResourceCost * (10000 - Talents.costBp a.Name (heals a) t) / 10000
          CastTicks = max (ticks 0) (a.CastTicks - Talents.castOff a.Name t)
          CooldownTicks = max (ticks 0) (a.CooldownTicks + Talents.cooldownOff a.Name t) })

  /// The four healer cooldowns, as single-rank abilities. It is the talent tree
  /// that hands each one over and not a trainer, so none of them carries a level:
  /// these builds are the tree's whole shape at every level. Numbers, cooldowns
  /// and mana shares are in `.pi/research/levelscale/healer-procs.md`.
  ///
  /// shortcut: a "next spell" buff lapses after twenty seconds. In 1.12 it lasts
  /// until the next spell is cast, which is what spending it means here; the
  /// timeout only stops one sitting on a caster forever after a bad pull. The two
  /// percentage costs are also frozen at their level-24 value (4% of 525 is 21,
  /// 20% of 449 is 90), so they undercharge a level-60 healer whose pool is
  /// larger — the cooldown, not the mana, is what limits the button.
  let private nextSpell (name: string) (cooldown: int<tick>) (cost: int) (empower: Empower) =
    { Name = name
      CastTicks = ticks 0
      CooldownTicks = cooldown
      ResourceCost = cost
      Interruptible = false
      Range = 0
      TargetKind = Self
      Effects = [ Empowers(secTicks 20.0, empower) ] }

  /// Nature's Swiftness: the next healing spell is instant.
  let private naturesSwiftness =
    nextSpell "Nature's Swiftness" (secTicks 180.0) 0 { Empower.none with Instant = true }

  /// Divine Favor: the next healing spell is a guaranteed crit.
  let private divineFavor =
    nextSpell "Divine Favor" (secTicks 120.0) 21 { Empower.none with SureCrit = true }

  /// Inner Focus: the next spell is free and a guaranteed crit.
  /// shortcut: 1.12 makes it +25% critical effect chance, not a guarantee; the
  /// sure crit is what this model was asked for.
  let private innerFocus =
    nextSpell "Inner Focus" (secTicks 180.0) 0 { Empower.none with
                                                  FreeMana = true
                                                  SureCrit = true }

  /// Swiftmend: hand over what the Druid's own Rejuvenation had left and consume
  /// it. The cost is 20% of base mana, and the cooldown is what keeps it honest.
  let private swiftmend =
    { Name = "Swiftmend"
      CastTicks = ticks 0
      CooldownTicks = secTicks 15.0
      ResourceCost = 90
      Interruptible = false
      Range = 8
      TargetKind = Ally
      Effects = [ ConsumeHealOverTime ] }

  /// Reincarnation: the Shaman's own corpse gets up again, which is why it is the
  /// one `Self` ability a dead member still casts. The cooldown is the whole point
  /// of the button — once a run, at best — and it is `Reincarnation`'s 30 minutes
  /// less `shamanTalents`' Improved Reincarnation, which is data, not a branch.
  ///
  /// 20% health and mana, plus the 20% the same 2/2 talent adds on top, so the
  /// fraction is 40 because the build carries it (`.pi/research/level24/shaman.md`
  /// §5).
  ///
  /// shortcut: the Ankh reagent and the ghost walk back to the body are not
  /// modelled — the sim has no items and no death state past `Health = 0`, so the
  /// Shaman stands up where he fell. 1.12 also trained this at 30, and the kit has
  /// it at every level like the tree's other buttons.
  let reincarnation =
    { Name = "Reincarnation"
      CastTicks = ticks 0
      CooldownTicks = secTicks 1800.0
      ResourceCost = 0
      Interruptible = false
      Range = 0
      TargetKind = Self
      Effects = [ Resurrect(40, 40) ] }

  /// Rebirth: the only resurrection in the game that a *living* caster can put on
  /// someone else mid-fight, which is what `Corpse` targeting is for. Rank 1's
  /// numbers from `.pi/research/ranks/druid.md:97`: 301 mana, 2 s cast, 30 min
  /// cooldown, 30 yd, 400 health and 700 mana restored. `Resurrect` restores
  /// fractions of the target's own pool, so those flat amounts are carried as 45%
  /// and 40% — their values against the sim's level-24 druid (893 hp / 1744 mana).
  /// 8 tiles of range, like every other ranged ability here.
  ///
  /// shortcut: the out-of-combat rule is not modelled. `PartyAi` fires it while a
  /// hostile is still standing and someone is down instead, which is the case the
  /// spell exists for; the corpse's ghost walk and the reaction window are not.
  let rebirth =
    { Name = "Rebirth"
      CastTicks = ticks 20
      CooldownTicks = secTicks 1800.0
      ResourceCost = 301
      Interruptible = true
      Range = 8
      TargetKind = Corpse
      Effects = [ Resurrect(45, 40) ] }

  /// The healer's traits: the two talents that are neither a stat nor a spell
  /// line land here, where the rest of the class bonuses already live.
  let private healerTraits (attackCritBp: int) (pushbackResistanceBp: int) =
    { Traits.none with
        CritBonusBp = attackCritBp
        PushbackResistanceBp = pushbackResistanceBp }

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
        Talents = Talents.none
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

        Gear60 = (6523, (119, 34, 280, 10, 16))
        ApPerStrength = 2
        ApPerAgility = 0
        FixedPool = Some 100 }

    let shielded = (dress sheet level).Shield
    let traits = if shielded then warriorTraits else warriorTraits |> fun t -> { t with BlockChanceBp = 0; BlockValue = 0 }

    let abilities =
      AbilityDef.kitAt
        level
        ([ tauntRanks; heroicStrikeRanks; sunderArmorRanks ]
         @ (if shielded then [ shieldBashRanks; shieldBlockRanks; shieldWallRanks ] else [])
         @ [ chargeRanks; disarmRanks; executeRanks ])

    heroStats
      buffs
      level
      id
      "Warrior"
      Tank
      (if level >= 40 then Plate else Chainmail)
      traits
      sheet
      [ (1, { Min = 29; Max = 55; Ticks = secTicks 2.7; Range = 1 })
        (28, { Min = 104; Max = 156; Ticks = secTicks 4.0; Range = 1 })
        (33, { Min = 104; Max = 157; Ticks = secTicks 3.5; Range = 1 })
        (38, { Min = 104; Max = 157; Ticks = secTicks 3.5; Range = 1 })
        (40, { Min = 87; Max = 131; Ticks = secTicks 2.8; Range = 1 })
        (60, { Min = 53; Max = 100; Ticks = secTicks 1.6; Range = 1 })
        ]
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
      (healerTraits 500 7000)
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

        Gear60 = (542, (8, 11, 133, 208, 133))
        Talents = priestTalents
        ApPerStrength = 1
        ApPerAgility = 0
        FixedPool = None }
      [ (1, { Min = 47; Max = 71; Ticks = secTicks 3.0; Range = 1 })
        (28, { Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 1 })
        (33, { Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 1 })
        (38, { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 })
        (40, { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 })
        (60, { Min = 58; Max = 109; Ticks = secTicks 1.5; Range = 8 })
        ]
      Mana
      partyThreatBp
      // Lesser Heal is the downrank: smaller, and the heal a Priest actually
      // reaches for on a small hole because nothing is wasted as overheal.
      (withTalents
        priestTalents
        (AbilityDef.kitAt
          level
          [ lesserHealRanks; healRanks; renewRanks; smiteRanks; shadowWordPainRanks ]
        @ [ innerFocus ]))
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
        Talents = Talents.none
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

        Gear60 = (1272, (55, 251, 173, 8, 5))
        ApPerStrength = 1
        ApPerAgility = 1
        FixedPool = Some 100 }
      [ (1, { Min = 20; Max = 39; Ticks = secTicks 1.9; Range = 1 })
        (28, { Min = 24; Max = 45; Ticks = secTicks 1.5; Range = 1 })
        (33, { Min = 26; Max = 49; Ticks = secTicks 1.4; Range = 1 })
        (38, { Min = 26; Max = 49; Ticks = secTicks 1.4; Range = 1 })
        (40, { Min = 26; Max = 49; Ticks = secTicks 1.4; Range = 1 })
        (60, { Min = 53; Max = 100; Ticks = secTicks 1.6; Range = 1 })
        ]
      Energy
      partyThreatBp
      (AbilityDef.kitAt
        level
        [ sinisterStrike; backstab; eviscerate; gouge; kick; sap; cheapShot; kidneyShot ])
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
        Talents = Talents.none
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

        Gear60 = (787, (12, 3, 159, 237, 42))
        ApPerStrength = 0
        ApPerAgility = 0
        FixedPool = None }
      [ (1, { Min = 30; Max = 57; Ticks = secTicks 1.5; Range = 8 })
        (28, { Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 8 })
        (33, { Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 8 })
        (38, { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 8 })
        (40, { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 8 })
        (60, { Min = 58; Max = 109; Ticks = secTicks 1.5; Range = 8 })
        ]
      Mana
      partyThreatBp
      (AbilityDef.kitAt
        level
        [ fireballRanks
          frostboltRanks
          arcaneMissilesRanks
          arcaneExplosionRanks
          blizzardRanks
          polymorphRanks
          counterspellRanks ])
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
      (healerTraits 0 7000)
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

        Gear60 = (614, (8, 8, 127, 212, 161))
        Talents = druidTalents
        ApPerStrength = 1
        ApPerAgility = 0
        FixedPool = None }
      [ (1, { Min = 47; Max = 71; Ticks = secTicks 3.0; Range = 1 })
        (28, { Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 1 })
        (33, { Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 1 })
        (38, { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 })
        (40, { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 1 })
        (60, { Min = 140; Max = 210; Ticks = secTicks 3.3; Range = 1 })
        ]
      Mana
      partyThreatBp
      (withTalents
        druidTalents
        (AbilityDef.kitAt
          level
          [ healingTouchR4Ranks
            regrowthRanks
            rejuvenationRanks
            entanglingRootsRanks
            wrathRanks
            moonfireRanks ]
        @ [ swiftmend; rebirth ]))
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
      (healerTraits 500 7000)
      { Base = humanPaladinBase
        Gear =
          [ gear "Sparkleshell Mantle" (Some 24) 161 (0, 4, 10, 0, 0)
            gear "Yorgen Bracers" (Some 22) 91 (6, 3, 3, 0, 0)
            gear "Algae Fists" (Some 23) 132 (10, 0, 4, 0, 0)
            gear "Chausses of Westfall" None 173 (11, 0, 5, 0, 0)
            gear "Lorekeeper's Ring" (Some 18) 0 (0, 0, 2, 0, 0)
            gear "Plains Ring" (Some 24) 0 (0, 0, 8, 3, 0)
            gear "Verigan's Fist" None 0 (0, 0, 7, 6, 12) ]

        Gear60 = (3162, (8, 13, 137, 185, 177))
        Talents = paladinTalents
        ApPerStrength = 2
        ApPerAgility = 0
        FixedPool = None }
      [ (1, { Min = 65; Max = 99; Ticks = secTicks 3.2; Range = 1 })
        (28, { Min = 104; Max = 156; Ticks = secTicks 4.0; Range = 1 })
        (33, { Min = 95; Max = 143; Ticks = secTicks 3.2; Range = 1 })
        (38, { Min = 95; Max = 143; Ticks = secTicks 3.2; Range = 1 })
        (40, { Min = 87; Max = 131; Ticks = secTicks 2.8; Range = 1 })
        (60, { Min = 51; Max = 97; Ticks = secTicks 1.9; Range = 1 })
        ]
      Mana
      partyThreatBp
      (withTalents
        paladinTalents
        (AbilityDef.kitAt
          level
          [ flashOfLightRanks
            holyLightRanks
            judgementRanks
            paladinHammerOfJusticeRanks
            divineProtectionRanks ]
        @ [ divineFavor ]))
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
        Talents = Talents.none
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

        Gear60 = (787, (12, 3, 159, 237, 42))
        ApPerStrength = 0
        ApPerAgility = 0
        FixedPool = None }
      [ (1, { Min = 30; Max = 57; Ticks = secTicks 1.5; Range = 8 })
        (28, { Min = 80; Max = 121; Ticks = secTicks 3.1; Range = 8 })
        (33, { Min = 74; Max = 112; Ticks = secTicks 2.5; Range = 8 })
        (38, { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 8 })
        (40, { Min = 88; Max = 133; Ticks = secTicks 2.9; Range = 8 })
        (60, { Min = 58; Max = 109; Ticks = secTicks 1.5; Range = 8 })
        ]
      Mana
      partyThreatBp
      (AbilityDef.kitAt
        level
        [ shadowBoltRanks; corruptionRanks; curseOfAgonyRanks; warlockImmolateRanks; fearRanks ])
      { X = 3; Y = 6 }

  /// The Shaman's totems, at the ranks a level-60 casts. Each is a `Summon`, so it
  /// is a real Entity with a tile, a lifetime and a radius: a hostile can kill it,
  /// it never shares a tile with a body, and it expires on its own clock.
  ///
  /// Restorative Totems is the multiplier on the two that hand a number out on a
  /// pulse, and Totemic Focus is already in the kit as a per-spell cost line, so
  /// neither is a special case here.
  ///
  /// shortcut: a totem's own health and armour are flat level-60 numbers rather
  /// than a share of the Shaman's, so a geared Shaman's totems die as fast as a
  /// fresh one's; scale them off his Stamina if area damage starts eating them.
  let private totemKit (t: Talents) (level: int) : Ability list =
    let pulse (p: TotemPulse) =
      match p with
      | Heals n -> Heals(n * (10000 + t.TotemEffectBp) / 10000)
      | RestoresMana n -> RestoresMana(n * (10000 + t.TotemEffectBp) / 10000)
      | other -> other

    let totem name cost cooldown requirement lifetime radius period p =
      (requirement,
       { Name = name
         CastTicks = ticks 0
         CooldownTicks = cooldown
         ResourceCost = cost
         Interruptible = false
         Range = 0
         TargetKind = Self
         Effects =
           [ Summon
               { Name = name
                 Health = 5
                 Armor = 0
                 Swing = None
                 Lifetime = Some lifetime
                 MoveTicksPerTile = secTicks 1000.0
                 Totem = Some { Pulse = pulse p; Radius = radius; Period = period } } ] })

    // Earth, air, then water: the order the Shaman drops them in, which is the
    // priority the Party's AI reads off the kit.
    [ totem "Stoneskin Totem" 80 (ticks 0) 16 (secTicks 120.0) 20 (ticks 0) (ArmorBoon 435)
      totem "Grace of Air Totem" 120 (ticks 0) 42 (secTicks 120.0) 20 (ticks 0) (AgilityBoon 77)
      totem "Mana Tide Totem" 240 (secTicks 300.0) 40 (secTicks 12.0) 20 (secTicks 2.0) (ManaFraction 400)
      totem "Healing Stream Totem" 100 (ticks 0) 20 (secTicks 120.0) 20 (secTicks 2.0) (Heals 44)
      totem "Mana Spring Totem" 100 (ticks 0) 26 (secTicks 120.0) 20 (secTicks 2.0) (RestoresMana 12) ]
    |> List.filter (fun (requirement, _) -> level >= requirement)
    |> List.map snd

  /// Orc Shaman: the Druid's verified leather-and-cloth set, which is as far as
  /// the ≤24 pieces are sourced — shaman trains mail at 40, so nothing better is
  /// available in the bracket. Strength 43 makes him the best melee of the
  /// casters and Rockbiter rather than Windfury is what a 24 has.
  ///
  /// Counted as a `Healer`, though the kit is the elemental one: he carries
  /// Healing Wave and the Party only heals through `role Healer`
  /// (`PartyAi.decide`), so the Dps role left his heal unusable and made him
  /// untestable as a healer. He is the weakest of the four on paper — Healing
  /// Wave costs 200 for 389-453 where a Priest's heal is cheaper per point.
  ///
  /// shortcut: Lightning Shield and Rockbiter are left out — nothing in the
  /// vocabulary retaliates or imbues a weapon. The totems are in, one per element
  /// as the class plays them; `PartyAi.totemOrders` keeps them up.
  let private shaman (buffs: PartyBuffKind list) (level: int) (id: int) =
    heroStats
      buffs
      level
      id
      "Shaman"
      Healer
      Leather
      (healerTraits 500 7000)
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

        Gear60 = (3162, (0, 5, 139, 188, 169))
        Talents = shamanTalents
        ApPerStrength = 1
        ApPerAgility = 0
        FixedPool = None }
      [ (1, { Min = 47; Max = 71; Ticks = secTicks 3.0; Range = 1 })
        (28, { Min = 104; Max = 156; Ticks = secTicks 4.0; Range = 1 })
        (33, { Min = 104; Max = 157; Ticks = secTicks 3.5; Range = 1 })
        (38, { Min = 104; Max = 157; Ticks = secTicks 3.5; Range = 1 })
        (40, { Min = 124; Max = 187; Ticks = secTicks 4.0; Range = 1 })
        (60, { Min = 51; Max = 97; Ticks = secTicks 1.9; Range = 1 })
        ]
      Mana
      partyThreatBp
      (withTalents
        shamanTalents
        (AbilityDef.kitAt
          level
          [ shamanLightningBoltRanks; earthShockRanks; flameShockRanks; shamanHealingWaveRanks ]
        @ totemKit shamanTalents level
        @ [ naturesSwiftness; reincarnation ]))
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
        Talents = Talents.none
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

        Gear60 = (1604, (43, 256, 188, 25, 5))
        ApPerStrength = 0
        ApPerAgility = 0
        FixedPool = None }
      [ (1, { Min = 66; Max = 84; Ticks = secTicks 1.8; Range = 8 })
        (28, { Min = 36; Max = 68; Ticks = secTicks 2.9; Range = 8 })
        (33, { Min = 43; Max = 82; Ticks = secTicks 2.8; Range = 8 })
        (38, { Min = 43; Max = 82; Ticks = secTicks 2.8; Range = 8 })
        (40, { Min = 46; Max = 86; Ticks = secTicks 2.9; Range = 8 })
        (60, { Min = 72; Max = 137; Ticks = secTicks 2.5; Range = 8 })
        ]
      Mana
      partyThreatBp
      (AbilityDef.kitAt level [ arcaneShotRanks; serpentStingRanks; raptorStrikeRanks ])
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
  /// `withRoster` at a chosen level, so the level-60 ladder can seat every class on
  /// its Gear60 sheet instead of the level the map seated it at.
  let withRosterAt (level: int) (roster: string list) (w: World) : World =
    let existing = w.Entities |> List.filter (fun e -> e.Faction = Party)

    match existing with
    | [] -> w
    | _ ->
      let tiles = existing |> List.map (fun e -> e.Pos)
      let party = partyOf roster level |> List.truncate (List.length tiles)
      let seated = List.map2 (fun (e: Entity) p -> { e with Pos = p }) party tiles

      { w with
          Entities = seated @ (w.Entities |> List.filter (fun e -> e.Faction <> Party))
          PartyBuffs = partyBuffsFor roster }

  let withRoster (roster: string list) (w: World) : World =
    match w.Entities |> List.filter (fun e -> e.Faction = Party) with
    | [] -> w
    | first :: _ -> withRosterAt first.Level roster w

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
