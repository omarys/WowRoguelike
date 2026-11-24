# Axis A3 — the percent stat multiplier (5 talents, 3 classes)

Repo: /home/omary/Dev/wowroguelike (F#; three projects, build `dotnet build src/WowRoguelike.Core -c Release`,
tests `dotnet test tests/WowRoguelike.Tests -c Release`, both from the repo root — do NOT run from `bin/`).
Suite is green at 360/360 on HEAD; keep it green and add tests for every fold you add.

## What this axis is

`.pi/research/octowow/axis-inventory.md` §A3 ranks this the next axis by multiplicity.
Five talents need "a percent multiplier on a stat or pool that is currently flat":

| talent | ranks | what the note asks for |
|---|---|---|
| Warrior **Toughness** | 5/5 | percent-of-gear-armour and percent-of-block-value |
| Druid feral **Thick Hide** | 3/3 | armour multiplier on item armour |
| Druid feral **Heart of the Wild** | 5/5 | per-form stat % (Bear Stamina +20%, Cat Strength +20%…) |
| Druid feral **Predatory Strikes** | 3/3 | attack power % (its ability-damage half is already built — see note §7) |
| Shaman **Ancestral Knowledge** | 5/5 | stat-wide multiplier ("all your stats") |

The exact rank values and any riders are the notes' own words — read the relevant sections of
`.pi/research/octowow/warrior.md`, `druid-feral.md`, and `shaman.md` before writing code. The notes are
the source of truth; this file only routes you. If a note and this spec disagree, the note wins.

## Anchors (verified today; line numbers drift, grep before editing)

- The template fold: `Content.fs:3852` —
  `let intellect = s.Intellect * (10000 + talents.IntellectBp) / 10000` — and
  `Talents.IntellectBp` is declared at `Domain.fs:1721`, defaulted at `:1827`. This is the shape
  every new percent field copies: bp on `Talents`/`Traits`, folded *before* the pools are derived.
- The existing shortcut you will resolve: `Content.fs:4057` — "Ancestral Knowledge's 'all your
  stats' is still narrowed to `IntellectBp`" — with `IntellectBp = 500` at `:4069`. Find the
  `shortcut:` comment near it and replace it; widening this one field to a stat-wide fold is part
  of the job, not a nice-to-have.
- The second shortcut: the axis-inventory quotes an old `Content.fs:3908-3911` comment —
  "a per-spec armour multiplier is a new field, and a separate decision" — near the armour
  construction. The armour line has moved (grep `Armor = ` in Content.fs; it folds gear armour,
  party-buff `armorBp`, `Agility*2` and `traits.ArmorBonus`, all flat). Thick Hide and Toughness
  land here.
- Pools with no percent anywhere: `MaxHealth` and `AttackPower` construction lines in Content.fs
  (grep `MaxHealth = ` and `AttackPower = `; the feral per-form numbers are what Heart of the
  Wild and Predatory Strikes multiply).
- Talent lists are per spec: `Content.fs:5369` maps specs to talent sets (`TreeDruid -> druidTalents`;
  find the feral druid's own set the same way, and `warriorTalents` / `shamanTalents`).
- Row shapes to copy: `SpellTalent` rows look like
  `{ Spell = ...; CostBp; HealBp; DamageBp; CritBp; ElementShareBp; CastTicksOff }`
  (Content.fs:4340-4348 area); `Traits` records like
  `let private warlockTraits = { Traits.none with PushbackResistanceBp = 7000 }`.

## Rules

1. **F# integer bp arithmetic everywhere** — the codebase folds as `base * (10000 + bp) / 10000`.
   Match it exactly; no floats.
2. **Fold before the pools are derived** — the same point `IntellectBp` folds at. A percent field
   that folds after `MaxHealth` is computed does nothing.
3. Keep the per-form gates for the Druid rows: Heart of the Wild's Bear-Stamina leg must only
   apply in Bear form, the Cat-Strength leg only in Cat form — that gating is the whole point of
   the talent. If per-form stat folding requires plumbing the current form into the fold site,
   do the plumbing honestly rather than picking one form.
4. Every talent row you add needs a comment naming the note and section it came from — the
   file's convention (see the Nightfall comment at Content.fs:4351 for the style).
5. Tests: one per talent, in the existing style (grep Tests.fs for a recent talent test —
   e.g. the Blessing of Kings or Nature's Grace tests — and copy its shape). Each test asserts
   the arithmetic: e.g. Toughness at rank N raises the hero's `Armor` by exactly N% of the gear
   component; Heart of the Wild changes the Bear's `MaxHealth` but not the Cat's; Ancestral
   Knowledge raises every stat. Keep them deterministic — build a hero directly through the
   Content constructors the existing tests use.
6. Do not touch `PartyAi`, `SimCombat` or the Game project. This axis is construction-time only.
   If you find yourself editing the tick loop, you have drifted — stop and re-read this spec.
7. If a note's value contradicts the sim's existing rank ladder for a spell (level-learned ranks),
   the sim wins — these talents are the only new data.

## Report back

- Build + suite result (exact counts).
- Per talent: the note section you implemented, the field you added, and the fold site.
- Any note text you chose NOT to implement, with the reason (e.g. a rider that belongs to a
  different axis) — skipped riders are fine, silent ones are not.
- The two `shortcut:` comments you resolved (quote their old text).

## Verification contract

A follow-up read will check your claims against the tree: the five talents present in Content.fs,
the two shortcut comments gone, the fold sites consistent with `IntellectBp`'s shape, and the suite
green. Report exact file:line for each change so the check is mechanical.
