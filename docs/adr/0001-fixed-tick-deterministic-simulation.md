# Fixed-tick deterministic simulation with Shell-owned speed control

The simulation advances in discrete **Ticks** at a fixed rate (20 per second) and never sees real
time. A tick is a function of the previous tick plus the Commands scheduled for it, so a seed and a
tick-stamped command log always produce the same World.

Speed control — including pause — lives entirely in the MonoGame **Shell**, which runs a
fixed-timestep accumulator and calls `step` a variable number of times per frame. The Core has no
notion of speed, and the command log is expressed in Ticks rather than in real time.

## Considered options

- **Variable-`dt` real-time**, feeding `GameTime` straight into the sim. The most faithful to WoW,
  but floating-point `dt` makes every bug irreproducible and every test impossible.
- **Strict turn-based initiative.** Cheap and testable, but abandons the real-time feel that is the
  whole reason the source material is worth copying.

## Consequences

Replaying a recorded session at any speed yields bit-identical results, so a fight can be
slow-motion debugged from its own replay. When the machine cannot keep up at 20Hz the simulation
falls behind in real time rather than dropping Ticks — dropping Ticks would break determinism,
slowing down does not.
