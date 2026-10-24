# Healer talents: the procs and the cooldown buttons

Evidence for the four healer talent effects the simulation models outside the stat sheet: the
Healing Way stack, the armour proc Inspiration and Ancestral Healing share, Illumination's mana
refund, and the four "next spell" cooldowns (Nature's Swiftness, Divine Favor, Swiftmend, Inner
Focus). Everything here is 1.12 vanilla at level 60, where the four healer builds spend their
points; the level-24 arithmetic only appears where a cost is a share of base mana.

The stat-side talents (Purification, Spiritual Healing, Tidal Focus, Reflection, Meditation,
Ancestral Knowledge, Divine Intellect, Spiritual Guidance, Restorative Totems) were already
carried into `src/WowRoguelike.Core/Content.fs` by the talent build pass and are not restated
here.

## Procs

| Class | Talent | Rank | What it does | Stack / duration | Confidence |
|---|---|---|---|---|---|
| Shaman | Healing Way | 1/3 | Healing Wave has a 33% chance to leave a buff worth **+6%** to subsequent Healing Waves on that target | 3 stacks, 15 s | M |
| Shaman | Healing Way | 2/3 | 66% chance, **+12%** | 3 stacks, 15 s | M |
| Shaman | Healing Way | 3/3 | 100% chance, **+18%** | 3 stacks, 15 s | M |
| Priest | Inspiration | 1/3 | A heal crit leaves the target's armour **+8%** | 15 s | M |
| Priest | Inspiration | 2/3 | **+16%** | 15 s | M |
| Priest | Inspiration | 3/3 | **+25%** | 15 s | M |
| Shaman | Ancestral Healing | 1/3 | A heal crit leaves the target's armour **+8%** | 15 s | M |
| Shaman | Ancestral Healing | 2/3 | **+16%** | 15 s | M |
| Shaman | Ancestral Healing | 3/3 | **+25%** | 15 s | M |
| Paladin | Illumination | 1–5/5 | A heal crit returns **20/40/60/80/100%** of that spell's **base** mana cost | instant | M |

Read together, the two armour procs are the same talent on two classes: 25% at 3/3, fifteen
seconds, refreshing rather than stacking. The build this repo runs takes 3/3 of both, so one
number (`25%`) and one duration (`15 s`) cover both.

The level-60 builds in `Content.fs` take Healing Way 3/3 (18% per stack), Inspiration 3/3 and
Ancestral Healing 3/3 (25%) and Illumination 5/5 (100%). A hero without the talent keeps 0 and
is unaffected, which is what makes these procs invisible to the other five classes.

## Cooldown buttons

| Class | Ability | Cost | Cooldown | Effect | Confidence |
|---|---|---|---|---|---|
| Shaman | Nature's Swiftness | 0 | **3 min** | The next Nature spell with a base cast under 10 s is instant | M (shape **V** in `../level60-kits-at-24.md` §6) |
| Druid | Nature's Swiftness | 0 | 3 min | The same talent, same cooldown, on the Balance side | M |
| Paladin | Divine Favor | 4% of base mana (**21** at level 24: 525 × 0.04) | **2 min** | The next Flash of Light / Holy Light / Holy Shock has a **100%** critical effect chance | M |
| Druid | Swiftmend | 20% of base mana (**90** at level 24: 449 × 0.20) | **15 s** | Consumes a Rejuvenation or Regrowth on a friendly target and instantly heals for its remaining amount | M (shape **V** in `../level60-kits-at-24.md` §6) |
| Priest | Inner Focus | 0 | **3 min** | The next spell costs no mana and its critical effect chance is **+25%** | M (shape **V** in `../level60-kits-at-24.md` §6) |

Base mana at 24 is the per-class constant in `../level24/base_stats.md` (Priest 540, Druid 449)
and `../level24/base_stats_new_classes.md` (Paladin 525, Shaman 476). Both percentage costs are
level-invariant, which is why they express at 24 at all: the share is the same number at 24 and
at 60, only the pool it is taken from moves.

Nature's Swiftness is a **Druid** talent as well as a Shaman one. Only the Shaman's is modelled;
the Druid's Restoration build spends nothing in Balance, so the button is not in that kit.

## Deviations between this table and what the simulation does

Three places where the model and the tooltip disagree on purpose. Each is marked with a
`shortcut:` comment at the code that carries it.

- **Healing Way's duration is 15 s, and the stage brief said 20 s.** The table above is what the
  talent calculators publish; the sim uses 15 s.
- **Inner Focus is modelled as a *guaranteed* crit, not +25% crit chance.** The stage brief asked
  for the guarantee. A heal crit in this model enlarges no heal (see the `shortcut:` in the
  `Heal` branch of `src/WowRoguelike.Core/SimCombat.fs`), so the crit's only visible work is
  Inspiration's armour proc and Illumination's refund.
- **Swiftmend heals for the whole remaining Rejuvenation** rather than the tooltip's fixed 12 s
  slice of it. At 15 s of Rejuvenation the two differ by the last three seconds, and "consume
  the HoT and take what it had left" is the rule the stage brief asked for.

## Confidence and what is not sourced

`V` = read verbatim from a cited page, `M` = corroborated by a second source, `U` = inference.
Every row above is `M`: this pass read search extracts of the talent calculators listed below
rather than each page verbatim, and the local briefs carry no talent tables at all (the rank
files exclude talents by rule). The *shapes* — which talent does what, on which class — are
`V` where `../level60-kits-at-24.md` §6 already recorded them.

Not sourced anywhere on disk, and therefore not modelled:

- Ancestral Healing's Stamina component (it gives armour only in 1.12; the 5% Stamina is a
  later expansion's version, and nothing here takes it).
- Improved Reincarnation, Totemic Focus's effect on totem *duration* (only its mana discount is
  modelled), and Nature's Swiftness on the Druid side.

## Sources

- `warcrafttavern.com/wow-classic/tools/talent-calculator/{shaman,paladin,druid,priest}/` — per-rank
  talent text and cooldowns.
- `wowpedia.fandom.com/wiki/Ancestral_Healing`, `.../Illumination_(Classic)`,
  `vanilla-wow-archive.fandom.com/wiki/Shaman_talents` — the armour proc, the refund and the
  Shaman Restoration tree.
- `wowforeverarmory.com/talents/shaman/restoration-healing-way` — Healing Way's stack cap and
  duration.
- On disk: `../level60-kits-at-24.md` §6 (the three cooldown shapes and their level-invariance),
  `../level24/base_stats.md`, `../level24/base_stats_new_classes.md` (the base-mana constants the
  percentage costs are taken from).
