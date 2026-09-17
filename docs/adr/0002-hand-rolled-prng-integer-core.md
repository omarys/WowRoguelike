# Hand-rolled PRNG with partitioned streams, and integer-only Core arithmetic

The Core uses a hand-written splitmix64 generator whose state lives in `World`, not
`System.Random`. `System.Random`'s algorithm is not contractually stable across .NET versions — it
changed in .NET 6 — so a replay recorded today could silently stop reproducing on a later runtime,
which would destroy the value of ADR-0001.

The run seed is split into independent per-purpose streams (generation, combat) so that adding a
generation call cannot shift every subsequent combat roll. All Core arithmetic is integer: positions
are grid tiles, durations are Ticks, and rates are in basis points with a single documented rounding
rule. No floating point appears in the simulation.

## Consequences

The Core's behaviour is reproducible across machines and across .NET versions, not merely across runs
on one machine. The cost is one rounding rule to state and test. Content authored in human units —
damage ranges, cast times in seconds — is converted to integers once, at the content boundary in
`Content.fs`.
