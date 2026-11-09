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

/// What a "your next spell" talent changes about the spell it lands on. The
/// 1.12 cooldowns of that shape — Nature's Swiftness, Divine Favor, Inner Focus —
/// differ only in which of these three things they set, so they are one buff with
/// three fields rather than three buffs. Divine Favor's kit row is gone since the
/// OctoWoW port made it a passive (`.pi/research/octowow/paladin.md` §5(c)), but
/// the shape stays: `SureCrit` is what Inner Focus sets.
[<Struct>]
type Empower =
  { /// Cast it instantly whatever its cast time says.
    Instant: bool
    /// Charge no mana for it.
    FreeMana: bool
    /// It always crits.
    SureCrit: bool
    /// The one ability this waives for. `None` rides whatever spell comes next,
    /// which is what the healer cooldowns (Nature's Swiftness, Inner Focus) are;
    /// `Some "Shadow Bolt"` is Nightfall, whose buff names a spell and must not
    /// be spent on Corruption. `Empower.appliesTo` is the one test for it.
    Only: string option }

module Empower =

  let none =
    { Instant = false
      FreeMana = false
      SureCrit = false
      Only = None }

  /// Whether this empower is allowed to touch `ability`. Unscoped buffs ride
  /// whatever comes next; a scoped one waits for its spell, and a caster that
  /// reached for something else keeps holding it.
  let appliesTo (ability: string) (e: Empower) =
    match e.Only with
    | None -> true
    | Some named -> named = ability

/// What a build can fire off an event it did not choose: a block it made, a
/// critical it landed, a damage spell that hit. These are the events the combat
/// path already resolves and can name — the swing hook landed with the Hunter
/// port, so a crit or a block is a case here rather than a new pipeline.
///
/// Adding an event is a case here and one `SimCombat.procOn` call at the place
/// that event is decided. Nothing else in the engine has to know it exists.
[<Struct>]
type ProcEvent =
  /// A physical hit of this character's landed critically.
  | OnCrit
  /// A critical strike landed by one *named* ability. The crit sites know the
  /// name — a `WeaponDamage` row hands its own to `meleeStrike`, a damage spell
  /// is resolved under it — which is what a talent like Lock and Load needs: it
  /// fires off three shots and not off the same character's Raptor Strike.
  | OnCritOf of ability: string
  /// This character blocked a physical hit aimed at it.
  | OnBlock
  /// A damage spell of this character's landed. A resisted spell is not a hit.
  | OnSpellHit
  /// One tick of a damage-over-time this character applied. Fired on the
  /// *caster*, from the tick, which is why `Corrupted` records who applied it:
  /// the damage lands on the target and the talent belongs to the applier.
  | OnDoTTick

/// What one proc hands over when it fires. Three shapes, because the talents
/// that need more than these three are talents the vocabulary cannot express yet
/// and are named in a comment beside their build instead.
type ProcResult =
  /// Resource in the character's own pool (rage on a block, rage on a crit).
  /// It does nothing to a pool that is not rage, which is what makes Primal
  /// Fury's "while in Bear or Dire Bear Form" clause the resource kind rather
  /// than a field: a cat builds energy, and energy is not rage.
  | GrantResource of amount: int
  /// The "next spell" buff a caster is holding — Clearcasting's free mana.
  | EmpowerNext of duration: int<tick> * empower: Empower
  /// One named ability's cooldown goes to zero. A reset is not a reduction:
  /// `CooldownOff` is the build-time fold that shortens a row, this is the state
  /// the tick map holds, and it is cleared because the ability has just been
  /// used rather than because a tree was spent.
  | ResetCooldown of ability: string

/// One talent's trigger: the chance, how soon it may fire again, and what it
/// hands over. `Talent` is the name it is logged and cooled down under — the
/// internal cooldown lives in `Entity.Cooldowns`, the same tick map abilities
/// use, so an ICD needs no state of its own.
type Proc =
  { Talent: string
    Event: ProcEvent
    /// Percent chance in basis points: Arcane Concentration 5/5 is 1000. A
    /// chance of 10000 fires without drawing a random number at all, so a
    /// deterministic talent leaves the seed's stream where it found it.
    ChanceBp: int
    /// Ticks before it may fire again; `ticks 0` fires whenever the event does.
    InternalCooldown: int<tick>
    Then: ProcResult }

/// A Warrior stance or a Druid form. An ability that requires one cannot be used
/// outside it, which is what makes Overpower a Battle Stance decision rather
/// than a button. Declared above `Aura` because an aura carries one.
///
/// `HumanForm` is the shape a Druid is born in: it is a form you can shift
/// *into* from any other, which is why it is a case here rather than the absence
/// of one. (Distinct from `Form.Humanoid`, which is what a mob *is*.)
type Stance =
  | BattleStance
  | DefensiveStance
  | BerserkerStance
  | HumanForm
  | BearForm
  | CatForm
  | TravelForm
  | TreeForm
  | MoonkinForm

/// The name a form is drawn and labelled with.
module Stances =
  let name =
    function
    | BattleStance -> "battle"
    | DefensiveStance -> "defensive"
    | BerserkerStance -> "berserker"
    | HumanForm -> "humanoid"
    | BearForm -> "bear"
    | CatForm -> "cat"
    | TravelForm -> "travel"
    | TreeForm -> "tree"
    | MoonkinForm -> "moonkin"

/// A timed state on an Entity.
type Aura =
  /// Druid's Slumber. Any hostile action wakes the target.
  | Sleeping of remaining: int<tick>
  /// Serpent Form: extra physical damage, and no casting while shifted.
  | SerpentForm of remaining: int<tick> * bonusDamage: int
  /// A Druid form or a Warrior stance. Unlike every other aura this one has no
  /// clock: it lasts until another form replaces it, because a form is a stance
  /// you hold rather than a buff you refresh. Bear and Cat also stop the holder
  /// casting (see `preventsCasting`), which is what makes shifting a decision.
  | Shaped of form: Stance
  /// Cheap Shot / Kidney Shot. Cannot act, but unlike Sleep it is not broken by
  /// damage.
  | Stunned of remaining: int<tick>
  /// Entangling Roots. Cannot move, but can still act and cast.
  | Rooted of remaining: int<tick>
  /// Polymorph. Cannot act, breaks on any damage, and heals while transformed.
  | Polymorphed of remaining: int<tick>
  /// Scatter Shot. No casting at all while it lasts, instant abilities included,
  /// so a silenced caster cannot simply pick a cheaper spell and carry on.
  | Silenced of remaining: int<tick>
  /// A heal-over-time. Heals `healPerTick` every Tick, and remembers which spell
  /// put it there: Renew, Rejuvenation and Regrowth are the same kind of effect
  /// and they stack, so the source is what tells them apart.
  | Rejuvenation of remaining: int<tick> * healPerTick: int * source: string
  /// Sunder Armour. Reduces the target's armour by a flat amount. Refreshed by
  /// another application rather than stacking.
  | Sundered of remaining: int<tick> * armorReduction: int
  /// A damage-over-time. Deals `damagePerTick` every Tick, unmitigated. It
  /// remembers who applied it, because a talent can hang off its own ticks:
  /// Nightfall watches the Warlock's Corruption and Ancient Brutality watches a
  /// Druid's bleed, and neither can be read off the target — the tick lands on
  /// the wrong entity, at a moment the caster may already be dead.
  | Corrupted of remaining: int<tick> * damagePerTick: int * caster: EntityId option
  /// The bleed a critical strike opens (Piercing Shots). Its own case rather than
  /// another `Corrupted` because `replaces` compares kinds, so sharing one would
  /// make a Hunter's bleed eat its own Serpent Sting — a sting and a bleed are two
  /// debuffs on the target in the game, and they have to be two auras here.
  | Bleeding of remaining: int<tick> * damagePerTick: int * caster: EntityId option
  /// Deadly Poison: a damage-over-time that stacks instead of refreshing, because
  /// the fifth dose is worth five times the first. The duration is shared; the
  /// stacks are what the Tick multiplies by.
  | DeadlyPoisoned of remaining: int<tick> * damagePerTick: int * stacks: int
  /// Wound Poison: every heal the holder receives is cut by `reductionBp` per
  /// stack, up to five. It stacks for the same reason Deadly Poison does — the
  /// Rogue's job against a healer is to keep landing it — and it is the only
  /// thing in the model that makes healing itself weaker rather than cheaper.
  | Wounded of remaining: int<tick> * reductionBp: int * stacks: int
  /// Mind-numbing Poison: the holder's casts take longer. The one poison that
  /// changes what a caster can do rather than how fast it dies.
  | Numbed of remaining: int<tick> * increaseBp: int
  /// Crippling Poison: the holder crosses a tile slower, which is the whole of
  /// what a snare is in a game where movement is a per-tile cost.
  | Crippled of remaining: int<tick> * slowBp: int
  /// Viper Sting. Drains `perTick` of the holder's resource every Tick.
  | Drained of remaining: int<tick> * perTick: int
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
  /// Healing Way: one buff carrying a stack count, so that a second Healing Wave
  /// into the same target is worth more than the first and the third is where it
  /// stops. It is one aura rather than three because the talent has one stack
  /// cap and one expiry, and a new application refreshes instead of adding.
  | HealingWay of remaining: int<tick> * stacks: int
  /// Inspiration / Ancestral Healing: the armour a heal crit left on the healed
  /// target, in basis points. Same case for both talents — the number in it is
  /// whichever tree's rank applies.
  | Inspired of remaining: int<tick> * armorBp: int
  /// Nature's Swiftness or Inner Focus sitting on the caster, waiting to be spent
  /// on the next spell. It falls off when it is used and when it times out.
  | Empowered of remaining: int<tick> * empower: Empower
  /// Vanilla's five-second rule: time since the last spell left this character's
  /// hands. While it stands, regeneration is the trickle rather than the rate —
  /// which is why a caster that stops casting gets its mana back, and why an
  /// innervated healer is told to stop.
  | RecentlyCast of remaining: int<tick>
  /// Innervate, held for its duration rather than paid in one lump: the holder's
  /// regeneration is multiplied by `regenBp` (40000 = five times) while it
  /// stands. Multiplying *whatever the holder has* is the point of the shape —
  /// a target that keeps casting multiplies the five-second trickle and gets
  /// nearly nothing, which is what vanilla's +400% does to a casting priest.
  | Innervated of remaining: int<tick> * regenBp: int

/// Every case of `Aura` is matched in exactly one place — here. Everywhere else
/// asks this module a question, so adding an aura breaks compilation in this
/// module and nowhere else.
module Aura =

  /// One Tick of ageing. None when the aura has run out.
  let advance =
    function
    | Sleeping r when r > ticks 1 -> Some(Sleeping(r - ticks 1))
    | SerpentForm(r, b) when r > ticks 1 -> Some(SerpentForm(r - ticks 1, b))
    | Shaped _ as s -> Some s
    | Stunned r when r > ticks 1 -> Some(Stunned(r - ticks 1))
    | Rooted r when r > ticks 1 -> Some(Rooted(r - ticks 1))
    | Polymorphed r when r > ticks 1 -> Some(Polymorphed(r - ticks 1))
    | Silenced r when r > ticks 1 -> Some(Silenced(r - ticks 1))
    | Rejuvenation(r, n, s) when r > ticks 1 -> Some(Rejuvenation(r - ticks 1, n, s))
    | Sundered(r, reduction) when r > ticks 1 -> Some(Sundered(r - ticks 1, reduction))
    | Corrupted(r, n, c) when r > ticks 1 -> Some(Corrupted(r - ticks 1, n, c))
    | Bleeding(r, n, c) when r > ticks 1 -> Some(Bleeding(r - ticks 1, n, c))
    | DeadlyPoisoned(r, n, s) when r > ticks 1 -> Some(DeadlyPoisoned(r - ticks 1, n, s))
    | Wounded(r, bp, s) when r > ticks 1 -> Some(Wounded(r - ticks 1, bp, s))
    | Numbed(r, bp) when r > ticks 1 -> Some(Numbed(r - ticks 1, bp))
    | Crippled(r, bp) when r > ticks 1 -> Some(Crippled(r - ticks 1, bp))
    | Drained(r, n) when r > ticks 1 -> Some(Drained(r - ticks 1, n))
    | Disarmed r when r > ticks 1 -> Some(Disarmed(r - ticks 1))
    | Blocking r when r > ticks 1 -> Some(Blocking(r - ticks 1))
    | ShieldWalled(r, bp) when r > ticks 1 -> Some(ShieldWalled(r - ticks 1, bp))
    | TotemBoon(r, a, c, d) when r > ticks 1 -> Some(TotemBoon(r - ticks 1, a, c, d))
    | HealingWay(r, n) when r > ticks 1 -> Some(HealingWay(r - ticks 1, n))
    | Inspired(r, bp) when r > ticks 1 -> Some(Inspired(r - ticks 1, bp))
    | Empowered(r, e) when r > ticks 1 -> Some(Empowered(r - ticks 1, e))
    | RecentlyCast r when r > ticks 1 -> Some(RecentlyCast(r - ticks 1))
    | Innervated(r, bp) when r > ticks 1 -> Some(Innervated(r - ticks 1, bp))
    | _ -> None

  /// The name a form is drawn and labelled with. Declared before `describe`
  /// because F# binds in file order and both of them want it.

  let describe =
    function
    | Sleeping r -> sprintf "sleep(%d)" r
    | SerpentForm(r, b) -> sprintf "serpent(%d,+%d)" r b
    | Shaped f -> sprintf "shaped(%s)" (Stances.name f)
    | Stunned r -> sprintf "stun(%d)" r
    | Rooted r -> sprintf "root(%d)" r
    | Polymorphed r -> sprintf "poly(%d)" r
    | Silenced r -> sprintf "silence(%d)" r
    | Drained(r, n) -> sprintf "drained(%d,%d)" r n
    | Rejuvenation(r, n, s) -> sprintf "rejuv(%d,+%d,%s)" r n s
    | Sundered(r, reduction) -> sprintf "sunder(%d,-%d)" r reduction
    | Corrupted(r, n, _) -> sprintf "dot(%d,%d)" r n
    | Bleeding(r, n, _) -> sprintf "bleed(%d,%d)" r n
    | DeadlyPoisoned(r, n, s) -> sprintf "deadly(%d,%d,x%d)" r n s
    | Wounded(r, bp, s) -> sprintf "wound(%d,-%d,x%d)" r bp s
    | Numbed(r, bp) -> sprintf "numb(%d,+%d)" r bp
    | Crippled(r, bp) -> sprintf "cripple(%d,-%d)" r bp
    | Disarmed r -> sprintf "disarm(%d)" r
    | Blocking r -> sprintf "block(%d)" r
    | ShieldWalled(r, bp) -> sprintf "wall(%d,-%d)" r bp
    | TotemBoon(r, a, c, d) -> sprintf "totem(%d,+%d,+%d,+%d)" r a c d
    | HealingWay(r, n) -> sprintf "healingway(%d,x%d)" r n
    | Inspired(r, bp) -> sprintf "inspired(%d,+%d)" r bp
    | Empowered(r, e) ->
      sprintf
        "empowered(%d,%s%s%s%s)"
        r
        (if e.Instant then "i" else "")
        (if e.FreeMana then "f" else "")
        (if e.SureCrit then "c" else "")
        (match e.Only with
         | Some named -> ">" + named
         | None -> "")
    | RecentlyCast r -> sprintf "recent(%d)" r
    | Innervated(r, bp) -> sprintf "innervated(%d,x%d)" r bp

  /// The name a player sees, short enough to fit over a sprite.
  let label =
    function
    | Sleeping _ -> "SLEEP"
    | SerpentForm _ -> "SERPENT"
    | Shaped f -> (Stances.name f).ToUpperInvariant()
    | Stunned _ -> "STUN"
    | Rooted _ -> "ROOTED"
    | Polymorphed _ -> "POLYMORPH"
    | Silenced _ -> "SILENCED"
    | Drained _ -> "DRAINED"
    | Rejuvenation(_, _, source) -> source
    | Sundered _ -> "SUNDER"
    | Corrupted _ -> "CORRUPTION"
    | Bleeding _ -> "BLEED"
    | DeadlyPoisoned _ -> "DEADLY POISON"
    | Wounded _ -> "WOUND POISON"
    | Numbed _ -> "NUMBED"
    | Crippled _ -> "CRIPPLED"
    | Disarmed _ -> "DISARMED"
    | Blocking _ -> "BLOCK"
    | ShieldWalled _ -> "SHIELD WALL"
    | TotemBoon _ -> "TOTEM"
    | HealingWay _ -> "HEALING WAY"
    | Inspired _ -> "INSPIRED"
    | Empowered _ -> "EMPOWERED"
    | RecentlyCast _ -> "CASTING"
    | Innervated _ -> "INNERVATE"

  /// True when the aura helps its holder rather than hurting it.
  let isHelpful =
    function
    | Rejuvenation _
    | Blocking _
    | ShieldWalled _
    | TotemBoon _
    | HealingWay _
    | Inspired _
    | Empowered _ -> true
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
    | Silenced r
    | Sundered(r, _)
    | Corrupted(r, _, _)
    | Bleeding(r, _, _)
    | Drained(r, _)
    | Blocking r -> r
    | TotemBoon(r, _, _, _)
    | ShieldWalled(r, _)
    | HealingWay(r, _)
    | Inspired(r, _)
    | Empowered(r, _) -> r
    | RecentlyCast r
    | Innervated(r, _)
    | DeadlyPoisoned(r, _, _)
    | Wounded(r, _, _)
    | Numbed(r, _)
    | Crippled(r, _) -> r
    // A form has no clock: it lasts until it is shifted out of. Asking a `function`
    // that predates the form auras for its lifetime used to raise a match failure,
    // and the buff bar asks about every aura an entity carries, so a Druid in Tree
    // of Life took the display down with it.
    | Shaped _ -> ticks System.Int32.MaxValue

  /// A form is the shape a body is standing in rather than a buff with a clock.
  let isForm =
    function
    | Shaped _ -> true
    | _ -> false

  /// Whole seconds left, rounded up so a live aura never reads "0s".
  let remainingSeconds (a: Aura) : int =
    int (Math.Ceiling(float (int (remaining a)) / float tickRate))

  // --- Poison readers ------------------------------------------------------
  // Rogue poisons are the one aura family the engine reads for their *magnitude*
  // rather than as a flag, so each gets a reader here and the call sites sum
  // over the holder's auras.

  /// Deadly Poison's damage for one Tick, already multiplied by its stacks.
  let poisonTick =
    function
    | DeadlyPoisoned(_, perTick, stacks) -> perTick * stacks
    | _ -> 0

  /// Wound Poison's cut to healing received, summed over its stacks. The
  /// five-stack cap is enforced where the aura is applied, not here.
  let healingReceivedReductionBp =
    function
    | Wounded(_, bp, stacks) -> bp * stacks
    | _ -> 0

  /// Mind-numbing: how much longer the holder's casts take, in basis points.
  let castTimeIncreaseBp =
    function
    | Numbed(_, bp) -> bp
    | _ -> 0

  /// Crippling: how much slower the holder crosses a tile, in basis points.
  let movementSlowBp =
    function
    | Crippled(_, bp) -> bp
    | _ -> 0

  /// Stack count of a stacking aura; 1 for one that does not stack.
  let stacksOf =
    function
    | DeadlyPoisoned(_, _, n) -> n
    | Wounded(_, _, n) -> n
    | HealingWay(_, n) -> n
    | _ -> 1

  let isSleeping =
    function
    | Sleeping _ -> true
    | _ -> false

  let isSerpentForm =
    function
    | SerpentForm _ -> true
    | _ -> false

  /// The form this aura puts its holder in, if any. Forms replace each other, so
  /// this is the whole of the current-form query — there is never a list of them.
  let stanceOf =
    function
    | Shaped f -> Some f
    | _ -> None

  let isSilenced =
    function
    | Silenced _ -> true
    | _ -> false

  /// True when this aura stops its holder acting at all.
  let preventsActing =
    function
    | Sleeping _ | Stunned _ | Polymorphed _ -> true
    | _ -> false

  /// True when this aura stops its holder casting.
  let preventsCasting =
    function
    | Sleeping _ | SerpentForm _ | Stunned _ | Polymorphed _ | Silenced _ -> true
    | Shaped(BearForm | CatForm) -> true
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
      | Corrupted(r, _, _) -> r
      | Bleeding(r, _, _) -> r
      | DeadlyPoisoned(r, _, _) -> r
      | Drained(r, _) -> r
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

  /// Healing still to come from the heal-over-time a named spell put up.
  let healOverTimePendingFor (source: string) (auras: Aura list) =
    auras
    |> List.sumBy (function
      | Rejuvenation(r, n, s) when s = source -> (r / secTicks 1.0) * n
      | _ -> 0)

  /// What Swiftmend hands over at once, and which buff it eats: 12 seconds of the
  /// target's Rejuvenation, or 18 seconds of its Regrowth — vanilla's numbers, and
  /// deliberately **not** the healing still pending. That difference is the whole
  /// play: a Swiftmend pressed at the last tick pays the same as one pressed at the
  /// first, so the HoT should be left to tick down and only banked at its end. The
  /// Rejuvenation is eaten first, as in the game, because the Druid re-casts it
  /// cheaper than Regrowth's up-front heal.
  let swiftmend (auras: Aura list) =
    let secondsOn source =
      auras
      |> List.fold
        (fun acc aura ->
          match aura with
          | Rejuvenation(_, n, s) when s = source -> max acc n
          | _ -> acc)
        0

    match secondsOn "Rejuvenation", secondsOn "Regrowth" with
    | 0, 0 -> None
    | rejuv, _ when rejuv > 0 -> Some("Rejuvenation", rejuv * (secTicks 12.0 / secTicks 1.0))
    | _, regrowth -> Some("Regrowth", regrowth * (secTicks 18.0 / secTicks 1.0))

  /// A key grouping auras of the same kind, so re-applying refreshes instead of
  /// stacking. Exhaustive, like everything here.
  let kind =
    function
    | Sleeping _ -> 0
    | SerpentForm _ -> 1
    | Shaped _ -> 22
    | Stunned _ -> 2
    | Rooted _ -> 3
    | Polymorphed _ -> 4
    | Rejuvenation _ -> 5
    | Sundered _ -> 6
    | Corrupted _ -> 7
    | Bleeding _ -> 23
    | Disarmed _ -> 8
    | Blocking _ -> 9
    | TotemBoon _ -> 10
    | ShieldWalled _ -> 10
    | HealingWay _ -> 11
    | Inspired _ -> 12
    | Empowered _ -> 13
    | Silenced _ -> 14
    | Drained _ -> 15
    | RecentlyCast _ -> 16
    | Innervated _ -> 17
    | DeadlyPoisoned _ -> 18
    | Wounded _ -> 19
    | Numbed _ -> 20
    | Crippled _ -> 21

  /// Does casting `newer` overwrite `older`?
  let replaces (older: Aura) (newer: Aura) =
    match older, newer with
    | Rejuvenation(_, _, a), Rejuvenation(_, _, b) -> a = b
    // Piercing Shots stacks, and has to: a crit's bleed is its own eight seconds,
    // and a Hunter that crits again two seconds later would otherwise swallow the
    // six ticks the first one still owes. Measured before this line existed, the
    // talent was worth nothing at all — 74 procs a fight and not one of them
    // finishing, because every crit ate the previous bleed.
    | Bleeding _, Bleeding _ -> false
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

  /// Who applied this damage-over-time, when the aura knows who did. Only a
  /// talent on the applier reads it, so nothing else in the engine asks.
  let dotCaster =
    function
    | Corrupted(_, _, Some caster) -> Some caster
    | Bleeding(_, _, Some caster) -> Some caster
    | _ -> None

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
    | Corrupted(_, n, _) -> n
    | Bleeding(_, n, _) -> n
    | _ -> 0

  /// Resource drained per firing Tick, summed over auras. Viper Sting is the only
  /// one, so this is zero or one number.
  let flatDrainPerTick =
    function
    | Drained(_, n) -> n
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

  /// Healing Way stacks on a target, zero when the buff is not running.
  let healingWayStacks (auras: Aura list) =
    auras
    |> List.fold
      (fun acc aura ->
        match aura with
        | HealingWay(_, n) -> n
        | _ -> acc)
      0

  /// Armour a heal crit's proc adds, in basis points. Zero when nothing procced.
  let armorPctBp (auras: Aura list) =
    auras
    |> List.sumBy (function
      | Inspired(_, bp) -> bp
      | _ -> 0)

  /// The "next spell" buff a caster is holding, `Empower.none` when there is none.
  let empower (auras: Aura list) =
    auras
    |> List.tryPick (function
      | Empowered(_, e) -> Some e
      | _ -> None)
    |> Option.defaultValue Empower.none

  /// Drop the "next spell" buff: the spell it was meant for has been cast.
  let spendEmpower (auras: Aura list) =
    auras
    |> List.filter (function
      | Empowered _ -> false
      | _ -> true)

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

/// Which totem slot a pulse occupies. A Shaman drops one totem per slot and a
/// new one overrides what stood there — Mana Tide replaces Mana Spring rather
/// than standing beside it — so the slot belongs to the pulse, not to a name list
/// the AI keeps in parallel with its own data.
module TotemSlot =
  let ofPulse (p: TotemPulse) =
    match p with
    | ArmorBoon _ -> 0 (* earth *)
    | AgilityBoon _ -> 1 (* air *)
    | Heals _
    | RestoresMana _
    | ManaFraction _ -> 2 (* water *)

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
  /// Multi-Shot: weapon damage to the target and to up to `maxTargets - 1` other
  /// hostiles within 8 tiles of it. Vanilla's r1 fires at 3 targets for weapon
  /// damage each, no flat bonus (`.pi/research/level24/hunter.md`, Multi-Shot row).
  | MultiShot of maxTargets: int
  /// Spell damage, mitigated by nothing (Q2a).
  | SpellDamage of min: int * max: int
  | Heal of min: int * max: int
  /// Mana handed to a friendly target as a share of \emph{its own} pool (basis
  /// points, 10000 = the whole bar). Innervate: vanilla models it as +400%
  /// regeneration for twenty seconds, which is about a fifth of a Priest's pool,
  /// and this grants that fifth once. A pool that refills over a duration and one
  /// that refills at once end a fight the same way, and only one of them needs a
  /// per-Tick aura.
  /// Innervate: the target's mana regeneration is multiplied for a duration, and
  /// it is multiplied *as it stands* — a caster who keeps casting multiplies the
  /// five-second trickle and gains almost nothing, which is why the AI tells an
  /// innervated caster to stop.
  | Innervate of duration: int<tick> * regenBp: int
  | Interrupt
  /// Scatter Shot's daze. The target cannot start any cast — instant abilities
  /// included — for `duration`. Unlike Interrupt it does not care what the target
  /// was already doing, so it also stops a caster that had not started yet.
  | Silence of duration: int<tick>
  | Sleep of duration: int<tick>
  | Shapeshift of form: Form * duration: int<tick> * bonusDamage: int
  /// Enter a form and stay in it: no clock, no stack, and it replaces whatever
  /// form the caster was in. The state lives as a `Shaped` aura so that one
  /// place (`Aura.advance`) still owns every aura's lifetime.
  | TakeStance of stance: Stance
  | Taunt
  /// Spell damage to every hostile within `radius` tiles of the caster. No
  /// friendly fire, and not mitigated by armour.
  | AreaDamage of radius: int * min: int * max: int
  /// A buff on the caster that changes the next spell it lands: Nature's
  /// Swiftness, Inner Focus (`.pi/research/levelscale/healer-procs.md`).
  | Empowers of duration: int<tick> * empower: Empower
  /// This ability's own crit chance, above the caster's (Improved Regrowth's +50%
  /// on Regrowth). A crit heal is worth half again as much, so this is a healing
  /// talent; it sits on the ability because the tree raises one spell, not the
  /// caster.
  | CritBonus of bp: int
  /// Swiftmend: heal for what the target's heal-over-time had left, and consume
  /// it. A heal, so it takes the healer's healing bonus like any other.
  | ConsumeHealOverTime
  /// Reincarnation and Rebirth: stand a body back up at a fraction of its pools.
  /// Not a heal — the target is a corpse, so it takes neither the healer's bonus
  /// nor the healing rules, and it is the only effect that may resolve against
  /// something that is already dead.
  | Resurrect of healthPct: int * manaPct: int
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
  /// Viper Sting: drain `amountPerTick` of the target's resource every Tick for
  /// `duration`. A caster with an empty pool cannot pay for a cast, so on a
  /// mana-using mob this is a throughput cut, not a damage ability — which is
  /// what a hunter without a kick needs against a caster it cannot reach.
  | DrainResource of duration: int<tick> * amountPerTick: int
  /// Charge: leap to a free tile beside the target.
  | Charge
  /// Intercept: the same leap, for a Warrior already in the fight. Separate from
  /// `Charge` because the two are opposites — Charge only works out of combat and
  /// *grants* rage, Intercept works in combat and *costs* it — and the engine has
  /// to enforce the first rule, not just the AI.
  | Intercept
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
  /// A dead party member: Rebirth's target, and nothing else in any kit. The
  /// living are not valid for it, exactly as the dead are not valid for `Ally`.
  | Corpse

type Ability =
  { Name: string
    /// Zero means instant.
    CastTicks: int<tick>
    CooldownTicks: int<tick>
    ResourceCost: int
    /// The stance or form the ability requires, if any. Carried down from the
    /// `AbilityDef` because combat only ever sees the resolved `Ability`.
    Stance: Stance option
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
        | Heal _
        | ConsumeHealOverTime -> true
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

  /// Whether an ability brings a body back from the dead. That is the one thing
  /// that resolves against a corpse, and the one thing a dead member still casts.
  let resurrects (a: Ability) =
    a.Effects
    |> List.exists (function
      | Resurrect _ -> true
      | _ -> false)

/// WoW's three resource economies. A Warrior builds rage by fighting, a Rogue
/// regenerates energy on a fixed clock, a caster spends mana — they behave
/// differently enough that one pool could no longer pretend to be all three.
type ResourceKind =
  | Mana
  | Rage
  | Energy

/// A Warrior stance or a Druid form. Moved above `Aura`, which carries one.
///
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
        Stance = d.Stance
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
    /// Percent added to this spell's damage, in basis points (Fire Power on
    /// Fireball). The damage half of `HealBp`, and the reason a build can put
    /// more into one nuke than another.
    DamageBp: int
    /// Percent added to this spell's crit chance, in basis points.
    CritBp: int
    /// Ticks taken off this spell's cast time.
    CastTicksOff: int<tick> }

/// A passive the party has because some class is present (Q2a). Each case is a
/// real Classic buff at the highest rank a level-24 caster can train.
///
/// Declared up here rather than beside `World`, which is where a party-wide thing
/// would sit, because `Talents` names one: a talent can hand the party a passive
/// (Blessing of Kings), and `Talents` is defined above `World`.
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
  /// Paladin: Blessing of Kings 1/1, +10% to all five of the party's stats. The
  /// only *multiplicative* passive here, and the only one a talent gates: every
  /// other branch of `buffed` adds a flat number, which is why the buff layer
  /// had to grow a percentage column to hold this one.
  | BlessingOfKings

/// The talent points a character has spent, for the handful of effects modelled.
/// A build is fixed for the length of a run — nothing here respecs — so it is a
/// constant on the hero sheet, next to the level-60 gear it was chosen with, and
/// `Talents.none` is the default that leaves a hero exactly as it was.
///
/// This is deliberately not the whole tree. A talent the vocabulary cannot
/// express — a damage-absorbing shield, a spell-school lockout — is named in a
/// comment beside its build and left out rather than half-modelled.
type Talents =
  { /// Percent added to every heal this character lands (Purification, Gift of
    /// Nature, Spiritual Healing), in basis points.
    HealDoneBp: int
    /// Percent added to every point of damage this character deals, melee or
    /// spell (Arcane Instability, Piercing Howl's opposite, Force of Will). The
    /// damage half of `HealDoneBp`: a talent that raises damage by ten percent
    /// is the same shape as one that raises healing.
    DamageDoneBp: int
    /// Percent off the mana cost of every healing spell (Tidal Focus), bp.
    HealCostBp: int
    /// Percent off the mana cost of every damaging spell (Frost Channeling,
    /// improved Fireball's cheaper sibling), bp.
    SpellCostBp: int
    /// Percentage points added to a critical strike's multiplier, bp. Mortal
    /// Shots' "+30% critical strike damage" is 3000, which turns a spell crit
    /// from 150% into 180% and a swing crit from 200% into 230%. Points and not a
    /// factor, because "+30%" means the same thirty points whichever site it
    /// lands on — which is why each site keeps its own base multiplier rather
    /// than this field replacing it.
    CritDamageBp: int
    /// Percent of a critical strike's damage the victim then bleeds back over
    /// eight seconds (Piercing Shots, 30% at 2/2), bp. The only talent here that
    /// pays the *target* instead of the holder, which is why it is not a `Proc`:
    /// a proc's payload acts on its owner by construction.
    CritBleedShareBp: int
    /// Percent added to damage dealt, and to a crit's multiplier, against Beasts,
    /// Dragonkin and Humanoids (Improved Slaying, 3% at 3/3), bp. Unlike
    /// `CritDamageBp` this one cannot be read off the attacker alone — the
    /// target's `CreatureType` decides whether it pays at all — so every site
    /// that spends it also has to be holding the target.
    SlayingBp: int
    /// Percent added to Intellect before the pools are derived (Ancestral
    /// Knowledge, Divine Intellect), bp.
    IntellectBp: int
    /// Percent added to mana regeneration (Reflection, Meditation), bp.
    RegenBp: int
    /// Percent of a heal's mana cost handed back when that heal crits
    /// (Illumination), bp.
    ManaRefundOnCritBp: int
    /// Percent a heal crit adds to the healed target's armour, for fifteen
    /// seconds (Inspiration and Ancestral Healing, both 25% at 3/3), bp.
    CritArmorBp: int
    /// Percent one Healing Way stack adds to the next Healing Wave the target
    /// receives (18% at 3/3), bp. Stacks three times.
    HealingWayBp: int
    /// Percent of Spirit added to every heal as flat bonus healing (Spiritual
    /// Guidance), bp.
    SpiritToHealingBp: int
    /// Percent added to what a totem gives the party (Restorative Totems), bp.
    TotemEffectBp: int
    /// The per-spell lines: cast time, cost and heal for one named spell.
    Spells: SpellTalent list
    /// Ticks taken off one named cooldown, and the spells it applies to
    /// (Improved Reincarnation: -10 min at 2/2). A list rather than a field per
    /// spell, because a talent that changes a cooldown is still just data.
    CooldownOff: (string * int<tick>) list
    /// The triggers this build hangs off the combat path: rage when it blocks,
    /// a free spell when its next hit lands. A list because a build carries
    /// several and their order is the order they fire in.
    Procs: Proc list
    /// Party-wide passives this build hands out rather than receives (Blessing of
    /// Kings). Unlike every other field here the effect lands on *other* members,
    /// so it is read when the party's passives are assembled and never by the
    /// holder's own combat path.
    PartyBuffs: PartyBuffKind list }

module Talents =

  let none =
    { HealDoneBp = 0
      DamageDoneBp = 0
      HealCostBp = 0
      SpellCostBp = 0
      CritDamageBp = 0
      CritBleedShareBp = 0
      SlayingBp = 0
      IntellectBp = 0
      RegenBp = 0
      ManaRefundOnCritBp = 0
      CritArmorBp = 0
      HealingWayBp = 0
      SpiritToHealingBp = 0
      TotemEffectBp = 0
      Spells = []
      CooldownOff = []
      Procs = []
      PartyBuffs = [] }

  let spell (name: string) (t: Talents) = t.Spells |> List.tryFind (fun s -> s.Spell = name)

  /// The procs this build hangs off one event, in the order it lists them.
  let procsOf (ev: ProcEvent) (t: Talents) = t.Procs |> List.filter (fun p -> p.Event = ev)

  /// Percent this character adds to one spell's heal, in basis points: the tree's
  /// own bonus plus the spell's own line.
  let healBp (name: string) (t: Talents) =
    10000
    + t.HealDoneBp
    + (spell name t |> Option.map (fun s -> s.HealBp) |> Option.defaultValue 0)

  /// Percent this character adds to one spell's damage, in basis points: the
  /// tree's own bonus plus the spell's own line. The damage twin of `healBp`.
  ///
  /// Covered: direct hits, weapon strikes and Multi-Shot, and — since the warlock
  /// port — periodic damage, which `SimCombat` scales at application time because a
  /// `Corrupted` aura carries no source by the time it ticks. What is still not
  /// covered is *per-source* identity: every damage-over-time shares one aura, so a
  /// talent that has to tell Corruption's burn from Curse of Agony's (Malediction)
  /// needs the aura split first, not a multiplier.
  let damageBp (name: string) (t: Talents) =
    10000
    + t.DamageDoneBp
    + (spell name t |> Option.map (fun s -> s.DamageBp) |> Option.defaultValue 0)

  /// Percent off one spell's mana cost, in basis points. `healing` picks up the
  /// tree-wide healing discount as well as the spell's own line, and everything
  /// else picks up the spell-damage discount.
  let costBp (name: string) (healing: bool) (t: Talents) =
    (if healing then t.HealCostBp else t.SpellCostBp)
    + (spell name t |> Option.map (fun s -> s.CostBp) |> Option.defaultValue 0)

  /// Ticks off one spell's cast time.
  let castOff (name: string) (t: Talents) =
    spell name t |> Option.map (fun s -> s.CastTicksOff) |> Option.defaultValue (ticks 0)

  /// Percent one spell's crit chance is raised by, in basis points (Improved
  /// Regrowth's half again on Regrowth). On the spell, not the caster.
  let critBp (name: string) (t: Talents) =
    spell name t |> Option.map (fun s -> s.CritBp) |> Option.defaultValue 0

  /// Ticks off one spell's cooldown.
  let cooldownOff (name: string) (t: Talents) =
    t.CooldownOff
    |> List.tryFind (fun (spell, _) -> spell = name)
    |> Option.map snd
    |> Option.defaultValue (ticks 0)

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
    /// Where this Hostile stands when nothing has stirred it, and where a lost
    /// pull puts it back. A body's distance from every mob's home is what decides
    /// whether the room it just lost can see it again, so this is the anchor the
    /// Shaman's death spot is measured against. `None` on Party members: a hero
    /// has no spawn point to be sent back to.
    Home: Pos option
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
    /// Ticks the party has spent continuously out of combat, reset the moment a
    /// hostile engages. Out-of-combat regeneration is not a trickle: after five
    /// seconds of rest the whole party is restored at once (see
    /// `SimTick.advanceOutOfCombatRegen`). Lives on the World rather than on each
    /// Entity because `SimState.inCombat` is itself a party-wide question — there
    /// is one fight, not one per member, so there is one clock.
    RestTicks: int<tick>
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
