# TODO — WowRoguelike

Remaining work, newest first. Numbers `#12/#20/#22/#24` are carried over from
`docs/HANDOFF.md` §7; do not reuse them for new items.

## Story (ink) — just landed, not yet wired to the sim

- [x] **Wire the rooms into encounters.** `Maps.stitch` joins the seven rooms
      into one connected grid, and `Content.wailingCaverns` places the party and
      every boss — Anacondra (first cave), Serpentis (Pit of Fangs), Cobrahn and
      Pythas (Crag), Naralex (Dreamer's Rock), Mutanus (deepest chamber).

- [ ] **Model Naralex and Mutanus properly.** They reuse the Druid of the Fang
      kit with pacing-only stat blocks. Naralex's Nightmare and Mutanus's
      summoning event need sourced numbers before real mechanics.

- [ ] **Wire story encounters to the sim.** `start_encounter(boss_id)` only logs
      the id; it does not launch the deterministic fight. Map each boss id to a
      real `Content` encounter and run it when the story reaches it.
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

## Classes — the roster

- [x] **All nine kits.** Warrior, Paladin, Rogue, Hunter, Priest, Shaman, Mage,
      Warlock, Druid — each on researched level-24 base stats and gear, each with
      the rank-appropriate abilities, all nine asserted to build by a test.

- [ ] **Give the pets and totems a body.** `Summon`/`SummonSpec` exist for a
      Hunter's pet, a Warlock's demon and a Shaman's totem, and none of the three
      has a caller yet. Each class's research file records exactly what it loses
      without them: the Hunter its Growl/Bite/Mend Pet, the Warlock its demon, the
      Shaman its totems.

- [ ] **Teach `Effect` a movement slow.** Nothing in the vocabulary snares:
      there is `Stun`, `Root`, `Sleep` and `Disarm`, but no 50 %-for-4-seconds, so
      the Hunter's Concussive Shot and the Shaman's Frost Shock and Earthbind are
      written down and not modelled.

- [ ] **Give the roster a UI.** `defaultRoster` is still a constant: nothing lets
      a player choose who goes in.

> **Entity ids come from the roster slot, never from the class.** Party members
> are seated 1..5 in roster order and mobs are numbered from 6. A class builder
> that hard-codes its own id collides with a mob, and every `tryEntity` on that id
> then finds whichever came first — which shows up as a mob's heal logged against
> a party member's name.

## Known limitations

- [ ] Loot and between-run progression.
- [ ] Resurrection — a wipe ends the run.
- [ ] Rendering is coloured rectangles; no sprites or on-screen text.
