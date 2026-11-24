# TODO — WowRoguelike

Remaining work, newest first. Numbers `#12/#20/#22/#24` are carried over from the
old handoff; do not reuse them for new items.

## Story (ink) — just landed, not yet wired to the sim

- [x] **Wire the rooms into encounters.** `Maps.stitch` joins the seven rooms
      into one connected grid, and `Content.wailingCaverns` places the party and
      every boss — Anacondra (first cave), Serpentis (Pit of Fangs), Cobrahn and
      Pythas (Crag), Naralex (Dreamer's Rock), Mutanus (deepest chamber).

- [ ] **Model Naralex and Mutanus properly.** They reuse the Druid of the Fang
      kit with pacing-only stat blocks. Naralex's Nightmare and Mutanus's
      summoning event need sourced numbers before real mechanics.

- [x] **Wire story encounters to the sim.** `start_encounter(boss_id)` no
      longer just logs: `Content.bossEncounter` maps each ink id to a real
      encounter (the party against that boss on the gully map; Mutanus arrives
      awake because the knot itself is the awakening), `sim story` runs each
      fight deterministically on seed 1 and prints `Bench.summary`, and a wipe
      ends the story run.
- [ ] **Play the story in the Game shell.** Only the headless harness renders it;
      the MonoGame window has no dialogue or choice UI.
- [ ] **Source-check the ink dialogue.** Boss/quest names and the Naralex/Nightmare
      premise are canon; the lines are authored paraphrase. Either mark them
      sourced-vs-placeholder per the `VERIFY` discipline or leave a note that they
      stay as flavour text.
- [ ] **Exercise `sim story -i`.** The interactive choice path is untested; only
      auto-play is covered.

## Combat AI — what the visual runs showed

- [x] **Ensure crowd control is actually used.** Landed: the pre-pull opener is
      now a *sequence* (operator direction) — the Rogue Saps when geometry
      allows, the Mage Polymorphs the surplus target (a caster first), a Tree
      Druid Hibernates a Beast/Dragonkin, and a caster out of range walks to the
      Tank's side while the pull is held (`PartyAi` `prePull` plus the
      `holdForCc` gates on the tank's walk and the damage lanes). Sap is 45 s
      (operator direction). Measured on the stitched dungeon: the original run
      booked zero controls and first-died at ticks 2125/1680/5280/1840 over
      seeds 1-4; the opener now books 1-3 controls per run and first-deaths move
      to 2600/4480/6280/7203. Tests: the sheeped mob stays out of the fight, the
      tree druid's Hibernate type gate, Sap's 45 s.
- [x] **Warrior Charge as the pull opener, gated on single-pack aggro.** Landed:
      once the openers land, the tank Charges the nearest *uncontrolled* focus
      (`strikeTarget`'s tie-break puts the sheep at the head of the pull, and
      charging a sleeper is a refused cast). The "multiple groups" gate is the
      landing spot: anything outside the focus's own cluster (3 tiles) must be
      beyond `aggroRadius + 1` of it, with sight. Charges land in the dungeon
      runs; the gully pays ~100 ticks a clear for a full-health outcome
      (snapshot refreshed).
- [ ] **Hunter: draw the group onto a Freezing Trap.** The directed pull for a
      party with a Hunter: the hunter opens at range, the pack runs onto a frozen
      trap, Mage Polymorphs a caster, then Charge. **Traps are not in the
      vocabulary** (`Content.fs` says so at the totem discount comment) — this
      needs an `Effect` kind plus tick resolution for a placed trap, new engine
      machinery rather than a content row.
- [ ] **Warlock: Succubus Charm in the opener.** Needs a demon body first —
      `Summon`/`SummonSpec` exist but no Warlock pet has a caller yet (see
      "Give the pets and totems a body" below); Charm is then a controlling
      ability on the Succubus's kit.
- [ ] **Rogue's Sap as a true stealth walk.** The engine's proximity aggro does
      not know about stealth, so a rogue walking to Sap range (3 tiles) of a
      dense pack is noticed by neighbours and the window dies before the Sap.
      Today the Rogue Saps when the geometry already allows it. A stealthed walk
      needs movement the hostile `noticed` check can exempt — engine work, booked
      here rather than hacked into `prePull`.

## Packs, fleeing, and the runner's answers — landed this session

- [x] **Pack placement.** The stitched caverns were a room-quota scatter: the
      first pull of `visual:wc` was the whole gully, because a 10-tile
      `callForHelpRadius` (half a room) cascaded everything it reached. Now each
      room's sourced totals stand in knots of ~4 around anchors ≥ 7 tiles apart
      (Anacondra with two guards, bosses alone), overflow goes to the
      farthest tile rather than dropping the quota, and the radius is 4 — the
      20 yd of the file's yard convention. Measured: the first pull is 7; the
      biggest simultaneous pull in the dungeon is 12 (the Crag); **seeds 1 and 3
      now clear the whole stitched dungeon** — the runs that wiped inside 3 000
      ticks in every earlier version. A graded fallback (packs at 3 tiles) was
      tried and reverted: knots that close fight as one 12-mob pull, while far
      sparse lines fight as rolling local segments.
- [x] **The warrior drags the fight.** Already built (`holdSpot`'s corridor
      pull with the `packGap` guard refuses a landing that moves toward the
      next pack); what made it real is the pack gaps it now works against.
- [x] **Mobs flee, and the party stops them.** A humanoid at a fifth of its
      health runs for the nearest un-engaged group (`SimTick` `mobDecisions`,
      "X flees toward Y"); the AI answers a runner with the first available
      stopper, ranked: Blind > Frost Nova > Gouge > Entangling Roots >
      Hamstring > Wing Clip > Frostbolt (`PartyAi` `antiFlee`). Hamstring is
      the workhorse in measurement (112 cripples on a cleared seed 1 run).
- [ ] **Warlock: Curse of Exhaustion in the runner's answers.** Not in the
      warlock research file and likely out of reach at level 24 — it needs
      sourcing before a row exists, so it is booked here rather than invented.
      shortcut: Frost Nova is modelled single-target (its area form needs an
      AoE control effect first); a rank 2+ Frost Nova has no source either.

## Backlog — carried over from the old handoff

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
      new and sourced vs invented. Grill the user (see `docs/agents/domain.md` §11).

## Classes — the roster

- [x] **All nine kits.** Warrior, Paladin, Rogue, Hunter, Priest, Shaman, Mage,
      Warlock, Druid — each on researched level-24 base stats and gear, each with
      the rank-appropriate abilities, all nine asserted to build by a test.

- [ ] **Give the pets and totems a body.** `Summon`/`SummonSpec` exist for a
      Hunter's pet, a Warlock's demon and a Shaman's totem, and none of the three
      has a caller yet. Each class's research file records exactly what it loses
      without them: the Hunter its Growl/Bite/Mend Pet, the Warlock its demon, the
      Shaman its totems.

- [x] **Teach `Effect` a movement slow.** Landed as `Effect.Cripple` (applies
      `Aura.Crippled`, which `advanceMovement` already priced): Hamstring
      (W9-sourced, Warrior kit), Wing Clip (hunter.md-sourced, Hunter kit) and
      Frostbolt's chill (r3 6 s / r4 7 s direct evidence) all carry it, and the
      AI presses the first available stopper on a fleeing mob — see "The runner's
      answers" below. Still written down and not modelled: the Hunter's Concussive
      Shot and the Shaman's Frost Shock and Earthbind.

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
