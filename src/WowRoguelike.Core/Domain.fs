namespace WowRoguelike.Core

open System

// ===========================================================================
// Time
// ===========================================================================

/// The Core's only unit of time. Every duration in the simulation is a count of
/// Ticks; nothing in the Core sees real time (ADR-0001).
[<Measure>] type tick

/// Authored durations only. Content says "1.5 seconds" and Content.fs converts
/// it to Ticks once, at the boundary (ADR-0002).
[<Measure>] type sec

[<AutoOpen>]
module Time =

  /// The sim's fixed rate. 20Hz is WoW's historical server tick, and it makes a
  /// 1.5s global cooldown exactly 30 Ticks and a 1s periodic effect 20.
  let tickRate: float<tick / sec> = 20.0<tick / sec>

  /// Lift a plain int into Ticks.
  let inline ticks (n: int) : int<tick> = n * 1<tick>

  /// Author a duration in seconds.
  let seconds (s: float) : float<sec> = s * 1.0<sec>

  /// Convert seconds to whole Ticks, rounding half away from zero. This is the
  /// single place the two units of measure meet.
  let ticksOfSeconds (s: float<sec>) : int<tick> =
    ticks (int (Math.Round(float (s * tickRate), MidpointRounding.AwayFromZero)))

  /// Author a duration in seconds and get Ticks directly.
  let secTicks (s: float) : int<tick> = ticksOfSeconds (seconds s)

// ===========================================================================
// Space
// ===========================================================================

/// A tile on the dungeon grid. Integer, so determinism is free (Q17a).
[<Struct>]
type Pos = { X: int; Y: int }

module Pos =
  let chebyshev (a: Pos) (b: Pos) = max (abs (a.X - b.X)) (abs (a.Y - b.Y))
  let move (dx: int) (dy: int) (p: Pos) = { X = p.X + dx; Y = p.Y + dy }

type Grid =
  { Width: int
    Height: int
    /// Row-major, index = Y * Width + X. A bare array because the grid is the
    /// one place Q8 sanctions mutation; the backing store is written only
    /// during construction.
    Walls: bool[] }

module Grid =

  let create width height =
    { Width = width
      Height = height
      Walls = Array.create (width * height) false }

  /// A grid that is entirely wall, for generation to carve rooms and corridors
  /// out of.
  let createSolid width height =
    let g = create width height
    Array.fill g.Walls 0 g.Walls.Length true
    g

  let inBounds (g: Grid) (p: Pos) =
    p.X >= 0 && p.Y >= 0 && p.X < g.Width && p.Y < g.Height

  let isWall (g: Grid) (p: Pos) =
    not (inBounds g p) || g.Walls.[p.Y * g.Width + p.X]

  let isFloor (g: Grid) (p: Pos) = not (isWall g p)

  let tiles (g: Grid) =
    seq {
      for y in 0 .. g.Height - 1 do
        for x in 0 .. g.Width - 1 do
          yield { X = x; Y = y }
    }

  /// Build a grid from an ASCII map: '#' is wall, anything else is floor.
  let ofRows (rows: string list) : Grid =
    let width = rows |> List.map String.length |> List.max
    let height = List.length rows
    let g = create width height
    rows
    |> List.iteri (fun y row ->
      row
      |> String.iteri (fun x c -> if c = '#' then g.Walls.[y * width + x] <- true))
    g

// ===========================================================================
// Entities
// ===========================================================================

type Faction =
  | Party
  | Hostile

/// Which of the three jobs a Party member does (Q19a). Which class fills a role
/// is content.
type Role =
  | Tank
  | Healer
  | Dps

type Form =
  | Humanoid
  | Serpent

[<Struct>]
type EntityId = EntityId of int

/// A timed state on an Entity.
type Aura =
  /// Druid's Slumber. Any hostile action wakes the target.
  | Sleeping of remaining: int<tick>
  /// Serpent Form: extra physical damage, and no casting while shifted.
  | SerpentForm of remaining: int<tick> * bonusDamage: int
  /// Gouge / Cheap Shot. Cannot act, but unlike Sleep it is not broken by damage.
  | Stunned of remaining: int<tick>
  /// Entangling Roots. Cannot move, but can still act and cast.
  | Rooted of remaining: int<tick>
  /// Polymorph. Cannot act, breaks on any damage, and heals while transformed.
  | Polymorphed of remaining: int<tick>
  /// A heal-over-time. heals `healPerTick` every Tick.
  | Rejuvenation of remaining: int<tick> * healPerTick: int
  /// Sunder Armour. Reduces the target's armour by a flat amount. Refreshed by
  /// another application rather than stacking.
  | Sundered of remaining: int<tick> * armorReduction: int

/// Every case of `Aura` is matched in exactly one place — here. Everywhere else
/// asks this module a question, so adding an aura breaks compilation in this
/// module and nowhere else.
module Aura =

  /// One Tick of ageing. None when the aura has run out.
  let advance =
    function
    | Sleeping r when r > ticks 1 -> Some(Sleeping(r - ticks 1))
    | SerpentForm(r, b) when r > ticks 1 -> Some(SerpentForm(r - ticks 1, b))
    | Stunned r when r > ticks 1 -> Some(Stunned(r - ticks 1))
    | Rooted r when r > ticks 1 -> Some(Rooted(r - ticks 1))
    | Polymorphed r when r > ticks 1 -> Some(Polymorphed(r - ticks 1))
    | Rejuvenation(r, n) when r > ticks 1 -> Some(Rejuvenation(r - ticks 1, n))
    | Sundered(r, reduction) when r > ticks 1 -> Some(Sundered(r - ticks 1, reduction))
    | _ -> None

  let describe =
    function
    | Sleeping r -> sprintf "sleep(%d)" r
    | SerpentForm(r, b) -> sprintf "serpent(%d,+%d)" r b
    | Stunned r -> sprintf "stun(%d)" r
    | Rooted r -> sprintf "root(%d)" r
    | Polymorphed r -> sprintf "poly(%d)" r
    | Rejuvenation(r, n) -> sprintf "rejuv(%d,+%d)" r n
    | Sundered(r, reduction) -> sprintf "sunder(%d,-%d)" r reduction

  let isSleeping =
    function
    | Sleeping _ -> true
    | _ -> false

  let isSerpentForm =
    function
    | SerpentForm _ -> true
    | _ -> false

  /// True when this aura stops its holder acting at all.
  let preventsActing =
    function
    | Sleeping _ | Stunned _ | Polymorphed _ -> true
    | _ -> false

  /// True when this aura stops its holder casting.
  let preventsCasting =
    function
    | Sleeping _ | SerpentForm _ | Stunned _ | Polymorphed _ -> true
    | _ -> false

  /// True when this aura stops its holder moving.
  let preventsMoving =
    function
    | Sleeping _ | Stunned _ | Polymorphed _ | Rooted _ -> true
    | _ -> false

  /// Extra physical damage added to each swing.
  let bonusDamage =
    function
    | SerpentForm(_, bonus) -> bonus
    | _ -> 0

  /// True when a hostile action against the holder ends this aura. Druid's
  /// Slumber is woken by any hostile action, not only by damage.
  let endsOnHostileAction =
    function
    | Sleeping _ | Polymorphed _ -> true
    | _ -> false

  /// A flat amount healed each Tick (heal-over-time).
  let flatHealPerTick =
    function
    | Rejuvenation(_, n) -> n
    | _ -> 0

  /// A fraction of max health healed each Tick, in basis points (Polymorph).
  let maxHealthHealBpPerTick =
    function
    | Polymorphed _ -> 125
    | _ -> 0

  /// A key grouping auras of the same kind, so re-applying refreshes instead of
  /// stacking. Exhaustive, like everything here.
  let kind =
    function
    | Sleeping _ -> 0
    | SerpentForm _ -> 1
    | Stunned _ -> 2
    | Rooted _ -> 3
    | Polymorphed _ -> 4
    | Rejuvenation _ -> 5
    | Sundered _ -> 6

  /// Prepend `a`, dropping any existing aura of the same kind, so a re-application
  /// refreshes rather than stacks.
  let refresh (a: Aura) (auras: Aura list) =
    a :: (auras |> List.filter (fun x -> kind x <> kind a))

  /// Flat armour reduction from Sunder Armour, summed over auras.
  let armorReduction =
    function
    | Sundered(_, reduction) -> reduction
    | _ -> 0

/// The closed vocabulary of mechanics an Ability can have (Q28b). Adding a new
/// *mechanic* means adding a case here; adding a new *ability* means writing a
/// record in Content.fs and no code at all.
type Effect =
  /// Physical damage, mitigated by armour, dodge and block.
  | Damage of min: int * max: int
  /// Spell damage, mitigated by nothing (Q2a).
  | SpellDamage of min: int * max: int
  | Heal of min: int * max: int
  | Interrupt
  | Sleep of duration: int<tick>
  | Shapeshift of form: Form * duration: int<tick> * bonusDamage: int
  | Taunt
  /// Spell damage to every hostile within `radius` tiles of the caster. No
  /// friendly fire, and not mitigated by armour.
  | AreaDamage of radius: int * min: int * max: int
  | Stun of duration: int<tick>
  | Root of duration: int<tick>
  | Polymorph of duration: int<tick>
  | HealOverTime of duration: int<tick> * healPerTick: int
  /// Sunder Armour: reduce the target's armour by a flat amount.
  | Sunder of duration: int<tick> * armorReduction: int
  /// Flat threat on the target, for abilities that taunt without forcing.
  | Threat of amount: int

type TargetKind =
  | Foe
  | Ally
  | Self

type Ability =
  { Name: string
    /// Zero means instant.
    CastTicks: int<tick>
    CooldownTicks: int<tick>
    ResourceCost: int
    Interruptible: bool
    /// In tiles. 1 is melee; a mob at range 1 is "within melee range" for the
    /// threat margins.
    Range: int
    TargetKind: TargetKind
    Effects: Effect list }

/// A swing on a timer, separate from the Ability list because every combatant
/// has one and none of them have to declare it.
type AutoAttack =
  { Min: int
    Max: int
    Ticks: int<tick>
    Range: int }

type Cast =
  { Ability: Ability
    Target: EntityId
    Remaining: int<tick> }

/// The five Vanilla stats. Each feeds one derived value at construction: Stamina
/// -> health, Intellect -> resource pool, Spirit -> regen, Strength -> melee
/// damage, Agility -> crit chance. Hostiles carry Stats.zero.
[<Struct>]
type Stats =
  { Strength: int
    Agility: int
    Stamina: int
    Intellect: int
    Spirit: int }

module Stats =

  let zero = { Strength = 0; Agility = 0; Stamina = 0; Intellect = 0; Spirit = 0 }

  /// Scale each stat by a basis-point multiplier, flooring. Used for party buffs.
  let scaleBp (bp: int) (s: Stats) =
    { Strength = s.Strength * bp / 10000
      Agility = s.Agility * bp / 10000
      Stamina = s.Stamina * bp / 10000
      Intellect = s.Intellect * bp / 10000
      Spirit = s.Spirit * bp / 10000 }

/// The four armour classes. Provide armour only, no stats.
type ArmorClass =
  | Plate
  | Chainmail
  | Leather
  | Cloth

module ArmorClass =

  /// Base armour per class. Agility adds a little on top, traits add the rest.
  let baseArmor =
    function
    | Plate -> 500
    | Chainmail -> 350
    | Leather -> 250
    | Cloth -> 80

/// Skill-tree bonuses that are neither stats nor party Buffs. Mostly zero; a
/// class's trait record fills in its specialisations.
type Traits =
  { CritBonusBp: int
    DodgeBonusBp: int
    ArmorBonus: int
    BlockChanceBp: int
    BlockValue: int
    PushbackResistanceBp: int
    BackstabCritBonusBp: int }

module Traits =

  let none =
    { CritBonusBp = 0
      DodgeBonusBp = 0
      ArmorBonus = 0
      BlockChanceBp = 0
      BlockValue = 0
      PushbackResistanceBp = 0
      BackstabCritBonusBp = 0 }

type Entity =
  { Id: EntityId
    Name: string
    Faction: Faction
    Role: Role option
    MaxHealth: int
    Health: int
    /// The tile being occupied, or the tile being left while stepping.
    Pos: Pos
    /// Set only while stepping between tiles; Pos is the tile being left and
    /// this is the tile being entered. The Shell interpolates between the two
    /// using MoveTicksLeft/MoveTicksTotal, which is how motion looks smooth at
    /// 20Hz without the Core knowing that rendering exists.
    Destination: Pos option
    MoveTicksLeft: int<tick>
    MoveTicksTotal: int<tick>
    /// Ticks to cross one tile. Speed is a stat, not a rendering detail.
    MoveTicksPerTile: int<tick>
    /// Where the entity has been told to go. Party only; Hostiles re-derive a
    /// goal from their target every tick.
    Goal: Pos option
    /// Remaining tiles of the current route toward Goal, nearest step first.
    /// Recomputed by A* only when it runs out or is invalidated, because
    /// re-planning every tick for every mover is the expensive way (Q26a).
    Path: Pos list
    /// Ticks to wait before attempting another search for the current Goal.
    /// Without it an unreachable goal costs a full-component search every tick,
    /// forever (see task 16).
    StallTicks: int<tick>
    Auras: Aura list
    Casting: Cast option
    Cooldowns: Map<string, int<tick>>
    /// One resource for everyone. WoW splits this into mana, rage and energy; a
    /// single pool is the lazy correct choice until the distinction itself
    /// changes gameplay. Spent when an ability resolves rather than when it
    /// starts, so an interrupted cast costs nothing.
    Resource: int
    MaxResource: int
    ResourceRegenPerTick: int
    Abilities: Ability list
    AutoAttack: AutoAttack option
    SwingTicksLeft: int<tick>
    /// Hostile entities only; Party members leave this empty.
    Threat: Map<EntityId, int>
    /// The Party member this Hostile is currently attacking. Kept between ticks
    /// because the threat margins are measured *against* it.
    Target: EntityId option
    /// A Taunt override, honoured until the taunter dies.
    ForcedTarget: EntityId option
    /// Threat generated is scaled by this, in basis points. A Tank stance is
    /// 5.0x in WoW, and that multiplier is what lets a Tank hold a pull at all.
    ThreatMultiplierBp: int
    /// The five Vanilla stats; Hostiles carry Stats.zero.
    Stats: Stats
    /// Chance to critically hit, in basis points. Derived from Agility.
    CritChanceBp: int
    /// Flat bonus to melee damage. Derived from Strength.
    MeleeDamageBonus: int
    /// Current armour, reduced by Sunder, raised by traits and agility. Mitigates
    /// physical damage only (Q2a), linear and capped at 75% (Q1a).
    Armor: int
    ArmorClass: ArmorClass
    /// Full avoidance of a physical hit, agility-derived (Q4a).
    DodgeChanceBp: int
    /// A flat amount subtracted from a blocked hit (Q4a).
    BlockChanceBp: int
    BlockValue: int
    /// Reduces the chance damage interrupts a cast in progress (Q5a).
    PushbackResistanceBp: int
    /// Extra crit chance on Backstab (Rogue trait).
    BackstabCritBonusBp: int
    /// False until something engages it; the threat table starts at zero on
    /// entering combat.
    Engaged: bool
    /// Raptors call for help rather than dying quietly. A percentage of max
    /// health, not an absolute value.
    CallForHelpBelowHealthPct: int option
    HasCalledForHelp: bool }

// ===========================================================================
// Commands
// ===========================================================================

type CommandKind =
  | UseAbility of actor: EntityId * ability: string * target: EntityId
  | MoveTo of actor: EntityId * destination: Pos
  | Stop of actor: EntityId

/// A player's intent. Commands carry the Tick they apply at (Q16a); whoever
/// issues them stamps that Tick, which is what lets a replay run at any speed
/// and still be bit-exact.
type Command = { ApplyAt: int<tick>; Kind: CommandKind }

// ===========================================================================
// Randomness
// ===========================================================================

[<Struct>]
type RngState = { Value: uint64 }

/// Independent streams split from one run seed (ADR-0002). Adding a Generation
/// draw must never shift a Combat draw.
type RngStreams =
  { Generation: RngState
    Combat: RngState }

// ===========================================================================
// World
// ===========================================================================

type Outcome =
  | Running
  | PartyWiped
  | EncounterCleared

/// A passive the party has because some class is present (Q2a).
type PartyBuffKind =
  /// Mage: +10% Intellect.
  | ArcaneIntellect
  /// Priest: +10% Spirit.
  | DivineSpirit
  /// Druid: +5% to all five stats.
  | MarkOfTheWild

type World =
  { Tick: int<tick>
    Grid: Grid
    Entities: Entity list
    /// Issued but not yet applied, in issue order.
    Pending: Command list
    Rng: RngStreams
    /// The party-wide passives, one per class present, computed once at Run
    /// creation. Held here for display and replay; applied to stats at build time.
    PartyBuffs: PartyBuffKind list
    /// Newest first. Excluded from fingerprints because it is derivable from
    /// the state; included in dumps because it is what a human reads.
    Log: string list }

// ===========================================================================
// Dungeon layout
// ===========================================================================

/// An axis-aligned rectangle of tiles, inclusive of both corners.
[<Struct>]
type Room =
  { Left: int
    Top: int
    Right: int
    Bottom: int }

module Room =

  let width (r: Room) = r.Right - r.Left + 1
  let height (r: Room) = r.Bottom - r.Top + 1
  let centre (r: Room) = { X = (r.Left + r.Right) / 2; Y = (r.Top + r.Bottom) / 2 }

  let contains (r: Room) (p: Pos) =
    p.X >= r.Left && p.X <= r.Right && p.Y >= r.Top && p.Y <= r.Bottom

  /// True when the rooms overlap, or come closer than `margin` tiles on both axes.
  let tooClose (margin: int) (a: Room) (b: Room) =
    a.Left - margin <= b.Right
    && b.Left - margin <= a.Right
    && a.Top - margin <= b.Bottom
    && b.Top - margin <= a.Bottom

  /// Tiles inset by one, so anything spawned in a room is not against its wall.
  let interior (r: Room) =
    seq {
      for y in r.Top + 1 .. r.Bottom - 1 do
        for x in r.Left + 1 .. r.Right - 1 do
          yield { X = x; Y = y }
    }

/// What a fight is, which the floorplan decides by position: Trash along the
/// route, a MiniBoss partway, the Boss at the end.
type EncounterKind =
  | Trash
  | MiniBoss
  | Boss

/// Something an EncounterTemplate can ask to have spawned.
type EncounterMember =
  | DruidOfTheFang
  | Raptor
  | LadyAnacondra
  | LordCobrahn
  | LordPythas
  | LordSerpentis
  | Naralex
  | Mutanus

/// A fight as data: what to put in a room, and how much room it needs. The
/// floorplan is generated; the fights are authored (Q21b).
type EncounterTemplate =
  { Name: string
    Kind: EncounterKind
    MinWidth: int
    MinHeight: int
    Members: EncounterMember list }

/// A generated Dungeon floorplan, plus the rooms that matter for placing fights.
type Layout =
  { Grid: Grid
    Rooms: Room list
    Entrance: Pos
    /// Farthest from the Entrance by path length, not by straight line.
    BossRoom: Room }
