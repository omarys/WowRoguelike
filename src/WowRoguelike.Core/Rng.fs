namespace WowRoguelike.Core

/// splitmix64 (after Sebastiano Vigna, public domain). Hand-rolled rather than
/// System.Random because the latter's algorithm is not contractually stable
/// across .NET versions, which would quietly break replay (ADR-0002).
module Rng =

  [<Literal>]
  let private Gamma = 0x9E3779B97F4A7C15UL

  /// The mix function, also used to derive stream states from a seed.
  let private mix (z0: uint64) : uint64 =
    let mutable z = z0
    z <- (z ^^^ (z >>> 30)) * 0xBF58476D1CE4E5B9UL
    z <- (z ^^^ (z >>> 27)) * 0x94D049BB133111EBUL
    z ^^^ (z >>> 31)

  /// One step: the next 64-bit value, plus the advanced state.
  let step (s: RngState) : uint64 * RngState =
    let advanced = s.Value + Gamma
    mix advanced, { Value = advanced }

  /// Derive an independent stream by hashing (seed, index) — the canonical
  /// splitmix64 pattern of starting the generator at a derived offset.
  let stream (seed: uint64) (index: uint64) : RngState =
    { Value = mix (seed + Gamma * (index + 1UL)) }

  [<Literal>]
  let GenerationStream = 0UL

  [<Literal>]
  let CombatStream = 1UL

  /// Seed the streams from a run seed. A Generation draw cannot perturb the
  /// Combat sequence and vice versa (ADR-0002).
  let streamsOf (seed: uint64) : RngStreams =
    { Generation = stream seed GenerationStream
      Combat = stream seed CombatStream }

  /// Draw an int in [0, bound). bound <= 1 yields 0 and leaves the state alone.
  ///
  /// ponytail: modulo bias. Irrelevant for flavour rolls; switch to Lemire's
  /// multiply-shift if an exact distribution ever becomes load-bearing.
  let below (bound: int) (s: RngState) : int * RngState =
    if bound <= 1 then
      0, s
    else
      let v, s' = step s
      int (v % uint64 bound), s'

  /// Draw an int in [lo, hi] inclusive.
  let roll (lo: int) (hi: int) (s: RngState) : int * RngState =
    if hi <= lo then
      lo, s
    else
      let d, s' = below (hi - lo + 1) s
      lo + d, s'

  /// Draw a bool that is true with probability `pct` percent.
  let chance (pct: int) (s: RngState) : bool * RngState =
    let d, s' = below 100 s
    d < pct, s'

  /// Draw from one stream, leaving the other alone. The caller passes the
  /// stream selector so a new Combat draw can never shift Generation.
  let drawCombat (lo: int) (hi: int) (rng: RngStreams) : int * RngStreams =
    let v, s' = roll lo hi rng.Combat
    v, { rng with Combat = s' }

  let drawGeneration (lo: int) (hi: int) (rng: RngStreams) : int * RngStreams =
    let v, s' = roll lo hi rng.Generation
    v, { rng with Generation = s' }
