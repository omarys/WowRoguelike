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
  let manhattan (a: Pos) (b: Pos) = abs (a.X - b.X) + abs (a.Y - b.Y)
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
  /// Druid's Slumber. Any hostile action wakes the target, so *damage* is what
  /// removes this, not merely the timer running out.
  | Sleeping of remaining: int<tick>
  /// Serpent Form: extra physical damage, and no casting while shifted.
  | SerpentForm of remaining: int<tick> * bonusDamage: int

/// The closed vocabulary of mechanics an Ability can have (Q28b). Adding a new
/// *mechanic* means adding a case here; adding a new *ability* means writing a
/// record in Content.fs and no code at all.
type Effect =
  | Damage of min: int * max: int
  | Heal of min: int * max: int
  | Interrupt
  | Sleep of duration: int<tick>
  | Shapeshift of form: Form * duration: int<tick> * bonusDamage: int
  | Taunt

type TargetKind =
  | Enemy
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

type World =
  { Tick: int<tick>
    Grid: Grid
    Entities: Entity list
    /// Issued but not yet applied, in issue order.
    Pending: Command list
    Rng: RngStreams
    /// Newest first. Excluded from fingerprints because it is derivable from
    /// the state; included in dumps because it is what a human reads.
    Log: string list }
