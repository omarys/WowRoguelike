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
///     Lord Serpentis         one of the four Fanglords; his kit here is the
///                            Druid of the Fang kit, not a separate sourced one
///
///   NOT sourced — placeholders, first knobs to turn against real data:
///     Lightning Bolt damage range (spells 9532 / 20295)
///     every cast time and cooldown in this file
///     every health pool, auto-attack range and swing timer
///
///   Our own invention, deliberately: the Party. Five heroes, their classes,
///   health and damage are chosen for encounter pacing, not copied.
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
      Effects = [ Damage(28, 42) ] }

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
      Effects = [ Damage(48, 66) ] }

  let arcaneShot =
    { Name = "Arcane Shot"
      CastTicks = ticks 0
      CooldownTicks = secTicks 6.0
      ResourceCost = 90
      Interruptible = false
      Range = 8
      TargetKind = Foe
      Effects = [ Damage(26, 34) ] }

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
      Engaged = false
      CallForHelpBelowHealthPct = callForHelpBelowPct
      HasCalledForHelp = false }

  // =========================================================================
  // The encounter
  // =========================================================================

  let private warrior =
    hero 1 "Warrior" Tank 900 300 { Min = 18; Max = 26; Ticks = secTicks 2.0; Range = 1 } tankStanceBp [ taunt; heroicStrike ] { X = 1; Y = 5 }

  let private priest =
    hero 2 "Priest" Healer 640 900 { Min = 6; Max = 10; Ticks = secTicks 3.0; Range = 1 } partyThreatBp [ lesserHeal ] { X = 1; Y = 6 }

  let private rogue =
    hero 3 "Rogue" Dps 700 400 { Min = 16; Max = 24; Ticks = secTicks 1.8; Range = 1 } partyThreatBp [ kick ] { X = 1; Y = 7 }

  let private mage =
    hero 4 "Mage" Dps 600 1200 { Min = 4; Max = 8; Ticks = secTicks 3.0; Range = 1 } partyThreatBp [ fireball ] { X = 2; Y = 6 }

  let private hunter =
    hero 5 "Hunter" Dps 680 900 { Min = 14; Max = 20; Ticks = secTicks 2.2; Range = 8 } partyThreatBp [ arcaneShot ] { X = 2; Y = 7 }

  let private druidOfTheFang id pos =
    mob id "Druid of the Fang" 480 400 (Some { Min = 12; Max = 18; Ticks = secTicks 2.0; Range = 1 }) [ lightningBolt; healingTouch; serpentForm; druidsSlumber ] None pos

  /// Raptors close faster than the casting mobs.
  let private raptor id pos =
    { mob id "Raptor" 380 0 (Some { Min = 20; Max = 28; Ticks = secTicks 1.8; Range = 1 }) [] (Some 20) pos with
        MoveTicksPerTile = secTicks 0.3 }

  let private ladyAnacondra id pos =
    mob id "Lady Anacondra" 900 600 (Some { Min = 14; Max = 20; Ticks = secTicks 2.0; Range = 1 }) [ lightningBolt; healingTouch; druidsSlumber ] None pos

  /// Lord Serpentis, last of the four Fanglords, reusing the Druid of the Fang
  /// kit rather than inventing one. Mutanus the Devourer is the real final boss of
  /// Wailing Caverns, but his fight is a summoning event with no sourced numbers,
  /// so he is deliberately not stood in for here.
  let private lordSerpentis id pos =
    mob id "Lord Serpentis" 1200 600 (Some { Min = 16; Max = 24; Ticks = secTicks 2.0; Range = 1 }) [ lightningBolt; healingTouch; druidsSlumber ] None pos

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
      [ warrior; priest; rogue; mage; hunter
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
    | LordSerpentis -> lordSerpentis id pos

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
      List.map2 (fun (e: Entity) p -> { e with Pos = p }) [ warrior; priest; rogue; mage; hunter ] partyTiles

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
      Log = [] }
