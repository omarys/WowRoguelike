# TODO — WowRoguelike

Remaining work, newest first. Numbers `#12/#20/#22/#24` are carried over from
`docs/HANDOFF.md` §7; do not reuse them for new items.

## Story (ink) — just landed, not yet wired to the sim

- [ ] **Wire the five section maps into encounters.** `Maps.fs` holds the five
      extracted Grids (`screamingGully`, `dreamersRock`, `pitOfFangs`,
      `cragOfTheEverliving`, `windingChasm`), but no World builder consumes them
      yet — they are fixtures, not fights.

- [ ] **Wire story encounters to the sim.** `start_encounter(boss_id)` only logs
      the id; it does not launch the deterministic fight. Each boss id must map to
      a real `Content` encounter. Three bosses have no mob yet — `lord_cobrahn`,
      `lord_pythas`, `mutanus` — while `lady_anacondra` and `lord_serpentis` exist
      in `Content.fs`. Add the missing mobs (Mutanus is the real final boss, a
      summoning event with no sourced numbers).
- [ ] **Play the story in the Game shell.** Only the headless harness renders it;
      the MonoGame window has no dialogue or choice UI.
- [ ] **Source-check the ink dialogue.** Boss/quest names and the Naralex/Nightmare
      premise are canon; the lines are authored paraphrase. Either mark them
      sourced-vs-placeholder per the `VERIFY` discipline or leave a note that they
      stay as flavour text.
- [ ] **Exercise `sim story -i`.** The interactive choice path is untested; only
      auto-play is covered.

## Backlog — carried over from HANDOFF §7

- [ ] **#20 Extract a named-field Entity builder.** `Content.hero`/`Content.mob`
      take 8–9 positional args and repeat ~28 identical record fields. Pure
      refactor; proven by the `sim runs` fingerprint.
- [ ] **#22 Expose replay + edge-triggered pause/step.** ADR-0001's whole
      justification is replay-at-any-speed, but nothing outside the tests can do
      it; applied commands are discarded and the Shell writes no log. Holding Space
      or `.` should not repeat every frame.
- [ ] **#24 Add the BSP generator** (the Q2c "implement twice" pair), and fix
      corridors clipping through unrelated rooms while there.
- [ ] **#12 A* replanning policy** — premise falsified at encounter scale; only
      matters for the synthetic 500-entity fixture. Low priority.

## Slice 4 — boss phases

- [ ] **Implement `Phase`.** In the glossary with no implementation; the next
      piece before the loop has an ending. Needs decisions before code — what
      drives a transition, how many phases, how it is modelled, which mechanics are
      new and sourced vs invented. Grill the user (see HANDOFF §8).

## Known limitations

- [ ] Loot and between-run progression.
- [ ] Resurrection — a wipe ends the run.
- [ ] Rendering is coloured rectangles; no sprites or on-screen text.
