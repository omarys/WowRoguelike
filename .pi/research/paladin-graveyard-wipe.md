# Paladin-as-healer graveyard wipe regression — autopsy report

Read-only diagnosis. No game-balance or behaviour changes implemented.
Raw data: `/tmp/pal-autopsy-{Paladin,Priest,Druid,Shaman}.log` (25-seed graveyard L33 arms, parsed
from `w.Log` by the additive `autopsy` harness verb), `/tmp/pal-smoke.log`, `/tmp/pal-wipe1.log`.

## 1. Verdict — hypothesis 3 (no panic button / burst coverage) is the mechanism

Hypotheses tested against the wiped seeds (numbers below):

| # | Hypothesis | Verdict | Deciding counts |
|---|------------|---------|-----------------|
| 3 | Burst window between casts kills the tank | **CONFIRMED (primary)** | 16/17 wipes: Flash of Light in flight at the first party death (the Warrior in 12, the Warlock Imp in 5); 5-6 mobs alive in 11/17 |
| 2 | Judgement Range=1 walks healer into cleaves | Confirmed (secondary, self-damage only) | adjacent 53% of combat ticks (Priest 11%); cleave hits on healer 12.0/run (Priest 3.3); healer self-damage 2991 avg, 8024 in seed 1 |
| 1 | Interrupts/pushbacks break the heals | REFUTED | lost casts 1.2/wipe-run; pushbacks 10.1; casts start 101 → end 101 |
| 4 | Mana starvation | REFUTED | wipe-seed healSum 12905 ≥ Priest's 11714; end pool avg 15% is a corpse-remainder, not a cause |

**Mechanism.** `healerOrders` (`src/WowRoguelike.Core/Sim/PartyAi.fs:627–1292`) reacts to HP thresholds
(urgent < 65% → fast heal). The paladin's only fast tool is Flash of Light ≈123 hp per 1.5 s + pushback
≈ **80 hps coverage, and zero between casts** — no instant, no HoT, no shield (the only healer with all
three gaps; `Content.fs:4243`). Graveyard packs are 3–6 mobs; when 5–6 are alive the tank's incoming
burst out-runs that coverage and he dies mid-cast, untouched healer at 100% hp watching. The priest at
the same moment has PW:S (instant absorb) + Renew already ticking; the druid has Rejuvenation always
rolling (437 heal-line ticks/run, only 15 direct cast-starts). Between-cast coverage, not cast failure,
is the differentiator. Per-run heal mix (derived: 96 heals, sum 12905, Flash≈123 / Holy Light≈601):
**~94 Flash of Light vs ~2 Holy Light** — the AI effectively never uses the big heal.

## 2. Death-autopsy table (graveyard L33, 25 seeds/arm, tick cap 24000)

Arm outcomes (first three match `comps` exactly):

| Healer arm | cleared | wiped | unresolved | tank dmg/run | healer dmg/run |
|---|---|---|---|---|---|
| Paladin | 6 | 17 | 2 | 11937 | 2991 |
| Priest (base) | 16 | 5 | 4 | 14725 | 841 |
| Druid | 11 | 1 | 13 | 18604 | 836 |
| Shaman | 8 | 0 | 17 | 13172 | 935 |

Wiped-seed autopsy (Paladin arm, n=17):

- First death: Warrior 12, Warlock Imp 5. No healer-first deaths at all.
- Healer state at first death: casting Flash of Light **16**, idle 1. Never Holy Light.
- Healer hp at that moment: 100% in 12 (healthy bystander).
- Mobs alive at first death: 5→8 seeds, 6→3, 4→3, 3→3 (the 4-mob pack plus a stray is the killer pull).
- First-death tick: median 3481, range 3083–7211 (~2.5–6 min in).
- Cast integrity: lost 1.2, pushbacks 10.1, knockbacks 10.5 per run.
- Positioning: healer adjacent to an engaged mob 56% of combat ticks; 14.5 cleave hits on healer per run.

Contrast arms, same metric: Priest wipes (n=5) — first deaths spread (Imp 2, Warrior 2, Mage 1), healer idle, casting Smite, or casting Lesser Heal at the death moment: the priest loses fights by over-committing to the damage lane,
a different and rarer failure. Druid wiped once (Imp first). Shaman wiped zero times (17 timeouts =
slow, not dead). "Unresolved" seeds are tick-cap timeouts, not wipes.

## 3. Ranked fix proposal (NOT implemented)

1. **AI burst policy in `healerOrders`** (PartyAi.fs:627–1292): when ≥4 mobs are engaged and the tank
   drops below ~75%, order Holy Light (or pre-cast before crossing the urgent threshold) instead of
   locking onto Flash of Light. Expected: decisive-window coverage rises from ~80 hps to ~200+ hps;
   should convert most of the 12 Warrior-first-death wipes. Pure AI change, no kit values touched.
2. **AI damage-lane hygiene**: keep the Healer-role out of melee range — Judgement (Range=1, melee nuke,
   `Content.fs:771-870`) currently walks the paladin into cleave radius (53% adjacent vs priest 19%).
   Gate melee-range nukes for the Healer role or give Judgement a caster standoff. Expected: cuts healer
   self-damage (up to 8024/seed) and ~10 pushbacks/run; secondary for wipes, free survivability, likely
   converts some of the 5 Imp-first-death cascade wipes.
3. **Kit: instant heal — OPERATOR DECISION needed on OctoWoW talents.** The vanilla L33 paladin kit has
   no panic button (documented at `Content.fs:4243`). Holy Shock exists in
   `.pi/research/octowow/paladin.md:46` (rank 1: 311–320 heal to ally, payload verified; the
   no-cooldown proc and ranks 2–4 are UNVERIFIED) but is unmodelled. If OctoWoW is in scope, this is the
   faithful fix for the gap; otherwise fixes 1–2 are the only lever.

## 4. Suite / build status

- `dotnet build src/WowRoguelike.Harness -c Release`: 0 errors, 0 warnings (`/tmp/pal-finalbuild.log`).
- `dotnet test tests/WowRoguelike.Tests -c Release`: **365/365 passed** (`/tmp/pal-finaltests.log`).
- Engine/game files (Domain.fs, Content.fs, PartyAi.fs, SimCombat.fs) untouched **by this task**; the only
  change made here is the additive `autopsy` verb in `src/WowRoguelike.Harness/Program.fs` (read-only
  measurement harness). Note: the working tree carries large *pre-existing* uncommitted changes to Core
  from earlier sessions on this branch — `git diff` vs HEAD is not a clean baseline to attribute per-session.
