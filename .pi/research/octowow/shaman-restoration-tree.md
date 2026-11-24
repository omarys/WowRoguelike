# OctoWoW Shaman — Restoration tree, as given by the operator

Source: the project operator, reading https://octowow.st/talents/shaman/ directly
(build `points=-F-FoDoaLFYQKFAB`). This supersedes the talent *names* recovered by
points-string decoding in the earlier research pass, which misread two rows: it reported
`Totemic Mastery` and `Spirit Link`, neither of which is in the tree, and missed
`Stable Shields` entirely. Effects below are verbatim intent, with notes on what the sim
can and cannot represent.

**There is no `Purification` anywhere in the Shaman tree.** `src/WowRoguelike.Core/Content.fs:3931`
describes the sim's Shaman as running "Purification 5/5" — that row is stale and wearing a
name that does not exist on this server.

## The tree

| Talent | Effect | Sim representation |
|---|---|---|
| Improved Healing Wave | −0.75 s cast on Healing Wave | trivial: cast time is already in ticks, 0.75 s = 15 ticks |
| Tidal Focus | healing spells −5% mana, totems −25% | trivial: `CostBp` |
| Ancestral Healing | target +25% armour for 15 s after a healing crit | needs a crit→aura hook on the target |
| Tidal Mastery | +5% crit to healing **and lightning** spells | trivial: `CritBp` (note: covers lightning too) |
| Healing Way | Healing Wave/Lesser Healing Wave 100%, Chain Heal 33%, to raise the next HW/Chain Heal on that target by 6% for 15 s; stacks 3× | needs a **per-target stacking** aura; only `Lacerate` has `maxStacks` today and it targets enemies |
| Healing Focus | 70% chance to avoid interruption while casting heals | needs pushback-on-damage; may not be modelled |
| Nature's Grace | −15% threat from Nature spells | needs threat modelling for spells |
| Restorative Totems | Mana Spring cost −50%, Healing Stream effect +25% | totems exist in the kit; nothing reads "improves totem by X" |
| Improved Water Shield | while Water Shield is active: +9 mana per 5 s, and **+2% total mana regen per active globe** | needs live-charge-count-dependent regen |
| Tidal Surge | after healing a target affected by Healing Way, 30% chance to regain mana = 15% of the spell's base cost | needs a mana-return proc keyed on a target aura |
| Ancestral Swiftness | next Nature spell under 10 s cast becomes instant; damaging spells cast this way do 25% less | needs a one-shot "next spell" state |
| Undertow | Healing Wave/Lesser Healing Wave 50% chance to restore a Water Shield charge; **at max charges it consumes one instead** | needs charge *restoration*, and the max-charge inversion |
| Improved Chain Heal | −1 s cast on Chain Heal | trivial |
| Stable Shields | +6 charges to shield spells | trivial once charges exist — but see below, it is the whole balance of Water Shield |

## Water Shield — confirmed mechanics

- **Cost**: similar to Lightning Shield (reuse that row's cost; Lightning Shield's cost is
  already in the sim).
- **Duration**: until its charges are spent — *not* a timed buff. This removes buff upkeep
  from the AI entirely; it recasts only when the globes are gone.
- **Cooldown**: none.
- **Globes**: 3 base, **+6 from Stable Shields** (so 9 if that talent is taken). Operators'
  earlier figure of 83 mana per globe is the unverified archive number from
  `shaman.md:27`.
- **Consumed**: one globe per charge-consuming event. With Undertow, Healing Wave and
  Lesser Healing Wave have a 50% chance to *refund* a globe, and at full charges the
  effect instead consumes one — so a Shaman sitting on a full shield self-taxes on purpose.

### Why this matters more than it looks

At 9 globes × 83 mana, one cast is 747 mana back over its life, and Undertow turns every
second Healing Wave into a charge refund. That is a mana engine, not a defensive cooldown.
It will move the Shaman's healing-per-mana figure far more than the AI fixes did, so it
must be measured against a fixed baseline and the other three classes must stay
byte-identical.

## Order of work

The trivial rows are pure data (`Content.fs` `shamanTalents`): Improved Healing Wave,
Tidal Focus, Tidal Mastery, Improved Chain Heal, plus the Water Shield package. They can
land without touching the engine.

The concept-heavy rows each need a new model primitive and should be greenlit
individually: Ancestral Healing, Healing Way, Healing Focus, Nature's Grace, Restorative
Totems, Tidal Surge, Ancestral Swiftness, Undertow's max-charge inversion.
