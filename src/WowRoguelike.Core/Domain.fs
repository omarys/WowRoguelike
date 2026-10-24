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

/// What a creature is, which decides which crowd control is legal on it. Vanilla
/// gates the control spells by type: Polymorph takes beasts, humanoids and
/// critters, Sleep takes beasts, humanoids and dragonkin. Everything else in the
/// level-24 spellbook — stuns, roots, disarm — is physical and type-blind.
[<RequireQualifiedAccess>]
type CreatureType =
  | Beast
  | Critter
  | Humanoid
  | Undead
  | Demon
  | Elemental
  | Dragonkin

module CreatureType =

  /// Polymorph (spell 118).
  let polymorphable =
    function
    | CreatureType.Beast
    | CreatureType.Humanoid
    | CreatureType.Critter -> true
    | _ -> false

  /// Sleep (spell 700), and the Druid's Slumber that shares the effect.
  let sleepable =
    function
    | CreatureType.Beast
    | CreatureType.Humanoid
    | CreatureType.Dragonkin -> true
    | _ -> false

[<Struct>]
type EntityId = EntityId of int

/// A timed state on an Entity.
type Aura =
  /// Druid's Slumber. Any hostile action wakes the target.
  | Sleeping of remaining: int<tick>
  /// Serpent Form: extra physical damage, and no casting while shifted.
  | SerpentForm of remaining: int<tick> * bonusDamage: int
  /// Cheap Shot / Kidney Shot. Cannot act, but unlike Sleep it is not broken by
  /// damage.
  | Stunned of remaining: int<tick>
  /// Entangling Roots. Cannot move, but can still act and cast.
  | Rooted of remaining: int<tick>
  /// Polymorph. Cannot act, breaks on any damage, and heals while transformed.
  | Polymorphed of remaining: int<tick>
  /// A heal-over-time. Heals `healPerTick` every Tick, and remembers which spell
  /// put it there: Renew, Rejuvenation and Regrowth are the same kind of effect
  /// and they stack, so the source is what tells them apart.
  | Rejuvenation of remaining: int<tick> * healPerTick: int * source: string
  /// Sunder Armour. Reduces the target's armour by a flat amount. Refreshed by
  /// another application rather than stacking.
  | Sundered of remaining: int<tick> * armorReduction: int
  /// A damage-over-time. Deals `damagePerTick` every Tick, unmitigated.
  | Corrupted of remaining: int<tick> * damagePerTick: int
  /// Disarm. The holder cannot swing its auto-attack while this lasts.
  | Disarmed of remaining: int<tick>
  /// Shield Block: the holder blocks far more of what swings at it.
  | Blocking of remaining: int<tick>
  /// Shield Wall: a share of every point of damage taken is removed.
  | ShieldWalled of remaining: int<tick> * reductionBp: int
  /// A Shaman totem's gift: armour, and crit and dodge in basis points. It is
  /// re-applied every Tick the carrier is within the totem's radius, so it
  /// lapses shortly after the totem dies or the member walks away.
  | TotemBoon of remaining: int<tick> * armor: int * critBp: int * dodgeBp: int

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
    | Rejuvenation(r, n, s) when r > ticks 1 -> Some(Rejuvenation(r - ticks 1, n, s))
    | Sundered(r, reduction) when r > ticks 1 -> Some(Sundered(r - ticks 1, reduction))
    | Corrupted(r, n) when r > ticks 1 -> Some(Corrupted(r - ticks 1, n))
    | Disarmed r when r > ticks 1 -> Some(Disarmed(r - ticks 1))
    | Blocking r when r > ticks 1 -> Some(Blocking(r - ticks 1))
    | ShieldWalled(r, bp) when r > ticks 1 -> Some(ShieldWalled(r - ticks 1, bp))
    | TotemBoon(r, a, c, d) when r > ticks 1 -> Some(TotemBoon(r - ticks 1, a, c, d))
    | _ -> None

  let describe =
    function
    | Sleeping r -> sprintf "sleep(%d)" r
    | SerpentForm(r, b) -> sprintf "serpent(%d,+%d)" r b
    | Stunned r -> sprintf "stun(%d)" r
    | Rooted r -> sprintf "root(%d)" r
    | Polymorphed r -> sprintf "poly(%d)" r
    | Rejuvenation(r, n, s) -> sprintf "rejuv(%d,+%d,%s)" r n s
    | Sundered(r, reduction) -> sprintf "sunder(%d,-%d)" r reduction
    | Corrupted(r, n) -> sprintf "dot(%d,%d)" r n
    | Disarmed r -> sprintf "disarm(%d)" r
    | Blocking r -> sprintf "block(%d)" r
    | ShieldWalled(r, bp) -> sprintf "wall(%d,-%d)" r bp
    | TotemBoon(r, a, c, d) -> sprintf "totem(%d,+%d,+%d,+%d)" r a c d

  /// The name a player sees, short enough to fit over a sprite.
  let label =
    function
    | Sleeping _ -> "SLEEP"
    | SerpentForm _ -> "SERPENT"
    | Stunned _ -> "STUN"
    | Rooted _ -> "ROOTED"
    | Polymorphed _ -> "POLYMORPH"
    | Rejuvenation(_, _, source) -> source
    | Sundered _ -> "SUNDER"
    | Corrupted _ -> "CORRUPTION"
    | Disarmed _ -> "DISARMED"
    | Blocking _ -> "BLOCK"
    | ShieldWalled _ -> "SHIELD WALL"
    | TotemBoon _ -> "TOTEM"

  /// True when the aura helps its holder rather than hurting it.
  let isHelpful =
    function
    | Rejuvenation _
    | Blocking _
    | ShieldWalled _
    | TotemBoon _ -> true
    | _ -> false

  /// Ticks left before it falls off.
  let remaining =
    function
    | Sleeping r
    | Stunned r
    | Rooted r
    | Polymorphed r
    | Disarmed r -> r
    | SerpentForm(r, _)
    | Rejuvenation(r, _ , _)
    | Sundered(r, _)
    | Corrupted(r, _)
    | Blocking r -> r
    | TotemBoon(r, _, _, _)
    | ShieldWalled(r, _) -> r

  /// Whole seconds left, rounded up so a live aura never reads "0s".
  let remainingSeconds (a: Aura) : int =
    int (Math.Ceiling(float (int (remaining a)) / float tickRate))

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
    | Rejuvenation(_, n, _) -> n
    | _ -> 0

  /// True on the Ticks where a periodic aura actually fires. WoW's heal- and
  /// damage-over-time effects tick once a second, and the sim runs at 20 Ticks
  /// per second, so lasting effects apply on every 20th Tick. Without this the
  /// per-second numbers would be paid twenty times over.
  let firesThisTick (a: Aura) =
    let remaining =
      match a with
      | Rejuvenation(r, _, _) -> r
      | Corrupted(r, _) -> r
      | Polymorphed r -> r
      | _ -> ticks 0

    remaining > ticks 0 && remaining % secTicks 1.0 = ticks 0

  /// A fraction of max health healed each Tick, in basis points (Polymorph).
  let maxHealthHealBpPerTick =
    function
    | Polymorphed _ -> 125
    | _ -> 0

  /// Healing still to come from heal-over-time auras, in health points. Periodic
  /// effects fire once a second, so remaining Ticks are converted to seconds
  /// before the per-second amount is multiplied in.
  let healOverTimePending (auras: Aura list) =
    auras
    |> List.sumBy (function
      | Rejuvenation(r, n, _) -> (r / secTicks 1.0) * n
      | _ -> 0)

  /// Ticks left on a heal-over-time, zero when there is none. What a healer asks
  /// before it refreshes one: re-casting into a HoT with ten seconds left is
  /// mana set on fire.
  let healOverTimeRemaining (auras: Aura list) =
    auras
    |> List.fold
      (fun acc aura ->
        match aura with
        | Rejuvenation(r, _, _) -> max acc r
        | _ -> acc)
      (ticks 0)

  /// Ticks left on the heal-over-time a named spell put up, zero when it is not
  /// running. A healer asks this per spell: a Tank wants Renew, Rejuvenation and
  /// Regrowth rolling at once, and only the source tells them apart.
  let healOverTimeRemainingFor (source: string) (auras: Aura list) =
    auras
    |> List.fold
      (fun acc aura ->
        match aura with
        | Rejuvenation(r, _, s) when s = source -> max acc r
        | _ -> acc)
      (ticks 0)

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
    | Corrupted _ -> 7
    | Disarmed _ -> 8
    | Blocking _ -> 9
    | TotemBoon _ -> 10
    | ShieldWalled _ -> 10

  /// Does casting `newer` overwrite `older`?
  let replaces (older: Aura) (newer: Aura) =
    match older, newer with
    | Rejuvenation(_, _, a), Rejuvenation(_, _, b) -> a = b
    | _ -> kind older = kind newer

  /// Prepend `a`, dropping the aura it replaces, so a re-application refreshes
  /// rather than stacks. Heal-over-times are keyed by the spell that cast them, so
  /// different spells stack while the same spell only refreshes.
  let refresh (a: Aura) (auras: Aura list) =
    a :: (auras |> List.filter (fun x -> not (replaces x a)))

  /// Flat armour reduction from Sunder Armour, summed over auras.
  let armorReduction =
    function
    | Sundered(_, reduction) -> reduction
    | _ -> 0

  /// Extra block chance from Shield Block, in basis points.
  let blockBonusBp =
    function
    | Blocking _ -> 7500
    | _ -> 0

  /// Share of incoming damage Shield Wall removes, in basis points.
  let damageReductionBp =
    function
    | ShieldWalled(_, bp) -> bp
    | _ -> 0

  /// A flat amount dealt as damage each Tick (damage-over-time).
  let flatDamagePerTick =
    function
    | Corrupted(_, n) -> n
    | _ -> 0

  /// True when this aura stops its holder swinging its auto-attack.
  let disablesAttack =
    function
    | Disarmed _ -> true
    | _ -> false

  /// Flat armour a totem's boon adds.
  let armorBonus =
    function
    | TotemBoon(_, a, _, _) -> a
    | _ -> 0

  /// Crit chance a totem's boon adds, in basis points.
  let critBonusBp =
    function
    | TotemBoon(_, _, c, _) -> c
    | _ -> 0

  /// Dodge chance a totem's boon adds, in basis points.
  let dodgeBonusBp =
    function
    | TotemBoon(_, _, _, d) -> d
    | _ -> 0

/// What a totem hands the party while it stands. The numbers are the totem's own
/// rank, so a talent that makes the totem better is a multiplier on these.
type TotemPulse =
  /// Health to every wounded party member in reach, every pulse.
  | Heals of amount: int
  /// Mana to every party member in reach, every pulse.
  | RestoresMana of amount: int
  /// A share of max mana to every party member in reach, every pulse (Mana
  /// Tide, which hands out 24% over its twelve seconds).
  | ManaFraction of maxManaBp: int
  /// Flat armour for as long as the totem stands (Stoneskin).
  | ArmorBoon of amount: int
  /// Agility for as long as the totem stands (Grace of Air). Agility is crit and
  /// dodge in this stat model and nothing else, so it arrives as basis points of
  /// each.
  | AgilityBoon of amount: int

/// A Shaman's totem: what it gives, how often, and how far. A totem is an
/// ordinary Entity — it has health, a tile and a lifetime — and this is the only
/// thing that tells the tick loop its kind.
type Totem =
  { Pulse: TotemPulse
    /// Tiles from the totem a party member still benefits. Chebyshev, like every
    /// other radius in the sim.
    Radius: int
    /// Ticks between pulses. Zero for a boon that is on for the whole lifetime.
    Period: int<tick> }

/// Something a caster calls to its side: a Hunter's pet, a Warlock's demon, a
/// Shaman's totem. It becomes a real Entity — it has health, it can be attacked,
/// and it is drawn on the grid — because a pet that cannot die and a totem that
/// cannot be killed are not the abilities they are in WoW.
///
/// The swing is raw numbers rather than an `AutoAttack` because `AutoAttack` is
/// declared later in this file; `SimCombat` builds the real one when it creates
/// the entity.
type SummonSpec =
  { Name: string
    Health: int
    Armor: int
    /// Its own swing: damage range and ticks between swings. A totem has no
    /// swing at all, which is `None`.
    Swing: (int * int * int<tick>) option
    /// Ticks it lasts before expiring on its own. None = until killed.
    Lifetime: int<tick> option
    /// Ticks to cross one tile. A totem never moves, so it is immobile.
    MoveTicksPerTile: int<tick>
    /// What it does to the party while it stands. A pet has none.
    Totem: Totem option }

/// The closed vocabulary of mechanics an Ability can have (Q28b). Adding a new
/// *mechanic* means adding a case here; adding a new *ability* means writing a
/// record in Content.fs and no code at all.
type Effect =
  /// Physical damage, mitigated by armour, dodge and block.
  | Damage of min: int * max: int
  /// A weapon strike: the actor's auto-attack roll scaled by `multiplierBp`
  /// (10000 = 100%), plus its Strength bonus and a flat `bonus`. Physical, so
  /// it runs the same dodge/block/armour pipeline a swing does.
  | WeaponDamage of multiplierBp: int * bonus: int
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
  /// Damage-over-time: deal `damagePerTick` every Tick for `duration`, unmitigated.
  | DamageOverTime of duration: int<tick> * damagePerTick: int
  /// Charge: leap to a free tile beside the target.
  | Charge
  /// Disarm: the target cannot swing its auto-attack for `duration`.
  | Disarm of duration: int<tick>
  /// Shield Block: raise the caster's block chance while it lasts.
  | Blocks of duration: int<tick>
  /// Shield Wall: cut the damage the caster takes by `reductionBp`.
  | ReducesDamage of duration: int<tick> * reductionBp: int
  /// A Rogue's combo point on the actor, kept for the next finisher.
  | ComboPoint of count: int
  /// Spend the actor's combo points as physical damage: `min`-`max` at one
  /// point, plus `perPoint` for every point after the first. Eviscerate scales
  /// exactly linearly in points (rank 4: 41-61 at one, +31 a point), which is
  /// why one flat step is enough to describe it.
  | Finisher of min: int * max: int * perPoint: int
  /// Call something to the caster's side: a Hunter's pet, a Warlock's demon, a
  /// Shaman's totem. It becomes a real Entity — it has health, it can be
  /// attacked, and it is drawn on the grid — because a pet that cannot die and
  /// a totem that cannot be killed are not the abilities they are in WoW.
  | Summon of spec: SummonSpec

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

/// How the UI files an ability away, most active first. A player looking for a
/// button looks for "the one that hurts", "the one that heals me", "the one
/// that stops the damage" — not for its position in the class kit.
[<RequireQualifiedAccess>]
[<Struct>]
type AbilityGroup =
  | Damage
  | DamageOverTime
  | Heal
  | HealOverTime
  | Defence
  | Utility

module Ability =

  /// The group an ability belongs to, read off its effects. A control ability is
  /// filed under Utility even when it also deals damage — Shield Bash lands 6
  /// damage and a 6 s lockout, and the lockout is the point of the button.
  let group (a: Ability) : AbilityGroup =
    let has test = a.Effects |> List.exists test

    if has (function
        | Interrupt -> true
        | _ -> false) then
      AbilityGroup.Utility
    elif has (function
        | Blocks _
        | ReducesDamage _ -> true
        | _ -> false) then
      AbilityGroup.Defence
    elif has (function
        | HealOverTime _ -> true
        | _ -> false) then
      AbilityGroup.HealOverTime
    elif has (function
        | Heal _ -> true
        | _ -> false) then
      AbilityGroup.Heal
    elif has (function
        | DamageOverTime _ -> true
        | _ -> false) then
      AbilityGroup.DamageOverTime
    elif has (function
        | Damage _
        | WeaponDamage _
        | SpellDamage _
        | AreaDamage _
        | Finisher _ -> true
        | _ -> false) then
      AbilityGroup.Damage
    else
      AbilityGroup.Utility

/// WoW's three resource economies. A Warrior builds rage by fighting, a Rogue
/// regenerates energy on a fixed clock, a caster spends mana — they behave
/// differently enough that one pool could no longer pretend to be all three.
type ResourceKind =
  | Mana
  | Rage
  | Energy

/// A Warrior stance or a Druid form. An ability that requires one cannot be used
/// outside it, which is what makes Overpower a Battle Stance decision rather
/// than a button.
type Stance =
  | BattleStance
  | DefensiveStance
  | BerserkerStance
  | BearForm
  | CatForm
  | TravelForm

/// One rank of an ability: the numbers a character gets when they train it.
/// Cast time, cooldown and range live here too, because they genuinely differ
/// between ranks (Healing Touch grows from 2.5 s to 3.5 s as it ranks up).
type Rank =
  { Level: int
    Cost: int
    CastTicks: int<tick>
    CooldownTicks: int<tick>
    Range: int
    Effects: Effect list }

/// The catalog form of an ability: every rank it has, oldest first, plus the
/// facts that never change between ranks. Combat only ever sees the resolved
/// `Ability` that `at` produces, so the rank ladder never reaches the tick loop.
type AbilityDef =
  { Name: string
    Resource: ResourceKind
    /// The stance or form the ability requires, if any.
    Stance: Stance option
    TargetKind: TargetKind
    Interruptible: bool
    Ranks: Rank list }

module AbilityDef =

  /// The best rank this character can train, or None while the ability is still
  /// above their level. Ranks are stored oldest-first.
  let at (level: int) (d: AbilityDef) : Ability option =
    d.Ranks
    |> List.filter (fun r -> r.Level <= level)
    |> List.tryLast
    |> Option.map (fun r ->
      { Name = d.Name
        CastTicks = r.CastTicks
        CooldownTicks = r.CooldownTicks
        ResourceCost = r.Cost
        Interruptible = d.Interruptible
        Range = r.Range
        TargetKind = d.TargetKind
        Effects = r.Effects })

  /// Every ability of a kit this character knows, in kit order.
  let kitAt (level: int) (defs: AbilityDef list) : Ability list = defs |> List.choose (at level)

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

/// What one talent tree does to one named spell. A talent that touches a single
/// spell is a line here rather than a branch in the healing pipeline: the ability
/// is built once at construction and this is folded into it there.
type SpellTalent =
  { Spell: string
    /// Percent off this spell's mana cost, in basis points.
    CostBp: int
    /// Percent added to this spell's heal, in basis points.
    HealBp: int
    /// Ticks taken off this spell's cast time.
    CastTicksOff: int<tick> }

/// The talent points a character has spent, for the handful of effects modelled.
/// A build is fixed for the length of a run — nothing here respecs — so it is a
/// constant on the hero sheet, next to the level-60 gear it was chosen with, and
/// `Talents.none` is the default that leaves a hero exactly as it was.
///
/// This is deliberately not the whole tree. A talent the vocabulary cannot
/// express — a heal-after-heal stack, a cooldown button, a proc — is named in a
/// comment beside its build and left out rather than half-modelled.
type Talents =
  { /// Percent added to every heal this character lands (Purification, Gift of
    /// Nature, Spiritual Healing), in basis points.
    HealDoneBp: int
    /// Percent off the mana cost of every healing spell (Tidal Focus), bp.
    HealCostBp: int
    /// Percent added to Intellect before the pools are derived (Ancestral
    /// Knowledge, Divine Intellect), bp.
    IntellectBp: int
    /// Percent added to mana regeneration (Reflection, Meditation), bp.
    RegenBp: int
    /// Share of a heal's mana cost handed back when that heal crits
    /// (Illumination), bp.
    ManaRefundOnCritBp: int
    /// Percent of Spirit added to every heal as flat bonus healing (Spiritual
    /// Guidance), bp.
    SpiritToHealingBp: int
    /// Percent added to what a totem gives the party (Restorative Totems), bp.
    TotemEffectBp: int
    /// The per-spell lines: cast time, cost and heal for one named spell.
    Spells: SpellTalent list }

module Talents =

  let none =
    { HealDoneBp = 0
      HealCostBp = 0
      IntellectBp = 0
      RegenBp = 0
      ManaRefundOnCritBp = 0
      SpiritToHealingBp = 0
      TotemEffectBp = 0
      Spells = [] }

  let spell (name: string) (t: Talents) = t.Spells |> List.tryFind (fun s -> s.Spell = name)

  /// Percent this character adds to one spell's heal, in basis points: the tree's
  /// own bonus plus the spell's own line.
  let healBp (name: string) (t: Talents) =
    10000
    + t.HealDoneBp
    + (spell name t |> Option.map (fun s -> s.HealBp) |> Option.defaultValue 0)

  /// Percent off one spell's mana cost, in basis points. `healing` picks up the
  /// tree-wide healing discount as well as the spell's own line.
  let costBp (name: string) (healing: bool) (t: Talents) =
    (if healing then t.HealCostBp else 0)
    + (spell name t |> Option.map (fun s -> s.CostBp) |> Option.defaultValue 0)

  /// Ticks off one spell's cast time.
  let castOff (name: string) (t: Talents) =
    spell name t |> Option.map (fun s -> s.CastTicksOff) |> Option.defaultValue (ticks 0)

type Entity =
  { Id: EntityId
    Name: string
    /// Character level. The attack table, spell hit and the armour curve are all
    /// functions of it: defence and weapon skill are five per level, and the
    /// attacker's level is the constant in the armour denominator.
    Level: int
    /// What it is. Decides which crowd control can land (see `CreatureType`).
    CreatureType: CreatureType
    /// Dual-wield: a second weapon swings with the first for half damage. Only
    /// the Rogue has one at 24; the Warrior swings a two-hander and the rest a
    /// staff or a wand.
    OffHand: bool
    Faction: Faction
    Role: Role option
    /// A scripted creature that stands inert until something wakes it — Mutanus
    /// before Naralex is disturbed. A dormant entity never picks a target, and
    /// nothing targets it: it is scenery until the script says otherwise.
    Dormant: bool
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
    /// Mana, rage or energy. Rage is built by fighting and energy regenerates on
    /// its own clock, so the tick loop has to know which economy it is feeding.
    ResourceKind: ResourceKind
    /// Rogue combo points, 0 to 5. Lost when the Rogue switches target, which is
    /// what makes a finisher a decision rather than a formality.
    ComboPoints: int
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
    /// Flat healing added to every direct heal before the tree's multiplier.
    /// Spiritual Guidance turns Spirit into exactly this.
    BonusHealing: int
    /// The trees this character has spent points in.
    Talents: Talents
    /// Set on a Shaman's totem and None on everything else, so the tick loop can
    /// find its totems without a second entity list.
    Totem: Totem option
    /// False until something engages it; the threat table starts at zero on
    /// entering combat.
    Engaged: bool
    /// Raptors call for help rather than dying quietly. A percentage of max
    /// health, not an absolute value.
    CallForHelpBelowHealthPct: int option
    HasCalledForHelp: bool
    /// The entity that called this one into being, for a summoned pet or totem.
    /// A pet dies with its owner.
    Owner: EntityId option
    /// Ticks until a summoned thing expires on its own. A totem is on a clock;
    /// a pet is not.
    ExpiresInTicks: int<tick> option }

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

/// A passive the party has because some class is present (Q2a). Each case is a
/// real Classic buff at the highest rank a level-24 caster can train.
type PartyBuffKind =
  /// Mage: Arcane Intellect rank 2, +7 Intellect.
  | ArcaneIntellect
  /// Priest: Divine Spirit rank 1. Level 30 — out of reach at 24, kept for the
  /// day the encounter roster outgrows that ceiling.
  | DivineSpirit
  /// Priest: Power Word: Fortitude rank 3, +20 Stamina.
  | PowerWordFortitude
  /// Druid: Mark of the Wild rank 3, +105 armour and +4 to all five stats.
  | MarkOfTheWild
  /// Warrior: Battle Shout rank 3, +55 melee attack power to the party.
  | BattleShout
  /// Paladin: Blessing of Might rank 3, +55 melee attack power to the party.
  | BlessingOfMight
  /// Paladin: Devotion Aura rank 3, +275 armour to the party.
  | DevotionAura

/// A quest, as the sim can actually track it: kill `Required` creatures drawn from
/// `Targets`. Quests double as the scripted encounters — the awakening of Naralex
/// is a quest that, when it completes, speaks and brings a dormant boss to life,
/// which is the only reason the dungeon has an ending rather than just corpses.
type Quest =
  { Name: string
    /// Creature names that count toward this quest.
    Targets: string list
    /// How many kills finish it.
    Required: int
    /// Creature names this quest wakes when it completes.
    Wakes: string list
    /// Lines logged when it completes, in order.
    Lines: string list
    Progress: int
    Done: bool }

module Quest =
  let create (name: string) (targets: string list) (required: int) : Quest =
    { Name = name
      Targets = targets
      Required = required
      Wakes = []
      Lines = []
      Progress = 0
      Done = false }

  /// Advance every unfinished quest that watches this kill.
  let onKill (name: string) (quests: Quest list) : Quest list =
    quests
    |> List.map (fun q ->
      if q.Done || not (List.contains name q.Targets) then
        q
      else
        let progress = min q.Required (q.Progress + 1)
        { q with Progress = progress; Done = progress >= q.Required })

  /// The quests that finished between two snapshots of the list.
  let justCompleted (before: Quest list) (after: Quest list) : Quest list =
    List.zip before after |> List.filter (fun (b, a) -> not b.Done && a.Done) |> List.map snd

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
    Log: string list
    /// The run's objectives, and with them its scripted encounters.
    Quests: Quest list }

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
