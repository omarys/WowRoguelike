# Research: OctoWoW Druid — the Moonkin (Balance) half

Source of every prose claim below: **<https://octowow.st/druid>**, fetched and read in full (68,422 bytes of HTML; body text 355 lines / 9,404 chars). Every talent number is quoted from the server-rendered payload at **<https://octowow.st/talents/druid/?points=oCaYYKFYJLDAB--FAAoAC>** (135,157 bytes; **54** records of shape `{"i":slot,"index":tree,"icon":…,"ranks":N,"requires":…,"reqBy":[…],"tooltip":[…]}` — tree 0 **20**, tree 1 **18**, tree 2 **16**). No number here is filled in from memory or from vanilla 1.12.

Status: **prose complete, numbers partial.** The summary page gives almost no coefficients; where a number is absent it is marked **UNVERIFIED** rather than guessed. The payload carries per-rank values for 50 of the 54 records; four tooltips are split across lazily-referenced chunks the payload does not inline (§7).

Tooltip rendering note: the payload inlines rank values as a component, so a verbatim read shows a stray index digit glued to the bracket, e.g. `a 1[50/100]% chance`. Quotes below drop the digit and keep the value list; the raw form is recorded in §7.

---

## 1. New abilities

Nothing new is added to the Balance kit as an *ability* — the Moonkin's three damage spells are all baseline or pre-existing. What is new and caster-relevant:

| Ability | Cost / cooldown | Effect (as stated) |
|---|---|---|
| **Wrath (Rank 9)** | 210 mana, level 60 | "A ninth rank, learned at level 60." The sim's `wrathRanks` ladder stops at rank 4 for level 22 (`src/WowRoguelike.Core/Content.fs:1442`). |
| **Insect Swarm** | level 20, five ranks to 60 | "Baseline for every Druid, learned at level 20 with five ranks up to level 60." Not in the sim. |
| **Hurricane** | 10 sec cooldown | "Has a 10 sec cooldown. It does not slow the attack speed of enemies caught in it without the new Balance talent Gale Winds." Not in the sim. |
| **Owlkin Frenzy** (talent, §4) | ICD 30/25/20 sec | The Moonkin's *only* mana-return mechanic: "regenerating 1% of your maximum mana per second for 10 sec". |

The page's *New Abilities* list is otherwise feral and Restoration: Savage Bite (58, 30 Rage), Barkskin (Feral, 44/52/60), Swift Travel Form (30, 13% base mana), Reshift ("Cancels and reapplies your current shapeshift form in one button"), Efflorescence (60, passive: "When another Druid overwrites your Rejuvenation or Regrowth, half of the remaining amount is delivered to that target instantly"). None of these reaches a caster Druid.

## 2. Class mechanics

**Moonkin Form is the whole finding. Payload, verbatim (ids #24858):**

> "Transforms the Druid into Moonkin Form. While in this form the armor contribution from items is increased by 180%, the Mana cost of your Balance spells is reduced by 20%, and all party members within 30 yards have their spell critical chance increased by 3%. The Moonkin can only cast Balance spells, Innervate, and Remove Curse while shapeshifted. The act of shapeshifting frees the caster of Polymorph and Movement Impairing effects."

The page's own prose agrees and adds nothing numeric: "Reachable far earlier, and it also reduces the mana cost of your Balance spells by 20% on top of its armor and party spell-crit effects." **There is no mana regeneration in Moonkin Form** — in OctoWoW the Moonkin's regen comes from the Owlkin Frenzy proc, not the form. The sim's `form` helper (`Content.fs:3500-3505`) gives Moonkin no `Aura.preventsCasting` (that is Bear and Cat only) and no stat effect at all: `let moonkinForm = form MoonkinForm "Moonkin Form"` (`Content.fs:3521`) is a costume with a sprite.

**Forms are free to enter and free to swap.** "Any form can be cast directly while you are already in another one: Bear to Cat, Cat to Travel, Travel to Bear. The new form replaces the old, with no need to cancel the form you are in first." The page also gives per-form costs — "Cat, Bear and Dire Bear Form cost 35% of base mana. Aquatic Form and Travel Form cost 13%" — which the sim deliberately does not charge (`Content.fs:3502`: "what a form costs is the casting it forbids … charging mana for entering one would charge for it twice"). Items in forms: "Potions, bandages, food, and most on-use trinkets and weapons are useable in Cat, Bear and Dire Bear Form."

**Eclipse is the one genuinely new axis** (§4). There is no Chakra-shaped resource, no shapeshift-locked resource, and no stacking meter: the Moonkin spends mana and nothing else.

## 3. Baseline spell changes

| Spell | Change (verbatim) |
|---|---|
| Insect Swarm | "Baseline for every Druid, learned at level 20 with five ranks up to level 60." |
| Hurricane | "Has a 10 sec cooldown. It does not slow the attack speed of enemies caught in it without the new Balance talent Gale Winds." |
| Wrath | "A ninth rank, learned at level 60." Cost 210 mana. |
| Regrowth | "Costs 20% less mana at every rank. Rank 9: 880 → 704 mana." |
| Tranquility | "Heals your entire raid and ticks every second. In exchange the mana cost is doubled (925 → 1850 at Rank 4) and the cooldown is 30 min." |
| Faerie Fire (Feral) | "Baseline, learned at level 25 with four ranks up to level 54 and a 6 sec cooldown." |
| Frenzied Regeneration / Tiger's Fury / Ferocious Bite / Rake / Shred / Enrage / Soothe Animal | Feral; listed on the page, irrelevant to the Moonkin. |

**No Starfire change is documented and no Starfire record exists in the payload** — it is a plain vanilla Balance nuke, unchanged, which is why five separate talents modify it. The sim has no Starfire at all (`grep -rni starfire src/` returns nothing).

## 4. Talent changes per tree (this build only, plus removals)

### Balance (tree 0) — 16 talents, 39 points

- **Improved Wrath** (0, 5) — payload: "Reduces the cast time and global cooldown of your Wrath spell by [0.1/0.2/0.3/0.4/0.5] sec." Page: "Reduces Wrath's global cooldown as well as its cast time, so the cast-time reduction is not wasted."
- **Sylvan Blessing** (3, 2, new) — "[50/100]% chance after killing a target that yields experience or honor to allow your Mana to regenerate at a 100% rate while casting. Lasts 15 sec."
- **Guidance of the Dream** (4, 3, new) — "[23/46/70]% chance to avoid interruption caused by damage while casting your Balance spells."
- **Improved Moonfire** (5, 2) — "Increases the damage and critical strike chance of your Moonfire spell by [5/10]%." Page: "5 Ranks → 2 Ranks."
- **Natural Weapons** (6, 3) — page: "5 Ranks → 3 Ranks. Also increases your chance to hit with melee attacks and spells." Payload values for `tree 0 / talent 6` are `["3","6","10"]` and `["1","2","3"]`; the descriptive text is in a lazy chunk, so which list is damage and which is hit is **UNVERIFIED** (§7).
- **Moonfury** (8, 3) — "Increases the damage of your Starfire, Moonfire, Hurricane, Insect Swarm, and Wrath spells by [4/8/12]%." Page: "Row 6 → Row 3, 5 Ranks → 3 Ranks. Now boosts Hurricane and Insect Swarm as well as Starfire, Moonfire and Wrath."
- **Omen of Clarity** (10, 1, requires Natural Weapons) — "Each of the Druid's melee attacks or offensive spell casts has a chance of causing the caster to enter a Clearcasting state. The Clearcasting state reduces the Mana, Rage or Energy cost of your next damage or healing spell or offensive ability by 100%." Page: "Procs from your offensive spell casts as well as your melee attacks."
- **Nature's Reach** (11, 2) — "Increases the range of your Wrath, Entangling Roots, Faerie Fire, Moonfire, Starfire, Insect Swarm, Hurricane, Remove Curse, Abolish Poison, and Cure Poison spells by [10/20]%."
- **Vengeance** (13, 5) — "Increases the critical strike damage bonus of your Starfire, Moonfire, and Wrath spells by [20/40/60/80/100]%."
- **Moonglow** (14, 3) — "Reduces the Mana cost of your Moonfire, Starfire, Wrath, Hurricane, Insect Swarm, Healing Touch, Regrowth and Rejuvenation spells by [3/6/9]%."
- **Owlkin Frenzy** (16, 1 of 3, requires Moonkin Form) — "Damage taken while in Moonkin Form has a 10% chance to enrage you, granting a 30% chance to avoid interruption caused by damage while casting and regenerating 1% of your maximum mana per second for 10 sec. This effect can only trigger once every [30/25/20] seconds."
- **Moonkin Form** (17, 1) — the form itself; quoted in full in §2. Page: "Row 7 → Row 5."
- **Nature's Grace** (18, 1, requires Moonglow) — "All spell criticals grace you with a blessing of nature, reducing the casting time of your next spell by 0.5 sec."
- **Improved Starfire** (19, 3) — "Reduces the cast time of Starfire by [0.2/0.3/0.5] sec and gives it a [5/10/15]% chance to stun the target for 3 sec." Page: "Row 4 → Row 5, 5 Ranks → 3 Ranks."
- **Balance of All Things** (21, 3, new) — "Damaging a target afflicted by Insect Swarm with Wrath refunds [10/20/30]% of its mana cost. Starfire has a [3/6/9]% increased chance to critically strike against targets affected by Moonfire."
- **Eclipse** (25, 1, new capstone) — "Damage from Wrath has a 40% chance to grant Arcane Eclipse, increasing Arcane damage dealt. Damage from Starfire has a 60% chance to grant Nature Eclipse, increasing Nature damage dealt. The damage bonus is 10% plus 60% of your spell critical strike chance. Each effect lasts 15 sec and has its own 30 sec cooldown. Only one Eclipse can be active at a time."

**Not taken, worth knowing:** Nature's Grasp (1, 1) and Improved Nature's Grasp (2, 4) — the root line; **Natural Shapeshifter** (7, 3) — the mana-per-shift talent, and the build skips it entirely, which is consistent with a spec that stays in one form; **Gale Winds** (22, 2) — "Reduces the mana cost of Hurricane and gives it an attack-speed slow".

**Removed from Balance:** "Improved Entangling Roots, Improved Thorns."

### Restoration (tree 2) — 3 talents, 12 points

- **Improved Mark of the Wild** (1, 5) — "Increases the effects of your Mark of the Wild and Gift of the Wild spells by [7/14/21/28/35]%."
- **Subtlety** (6, 5) — "Reduces the threat generated by your spells by [4/8/12/16/20]%." Page: "Row 3 → Row 2. Reduces the threat generated by all your spells, not just your healing spells."
- **Reflection** (11, 2 of 3) — "Allows [5/10/15]% of your Mana regeneration to continue while casting."

**Not taken and notable:** **Nature's Swiftness** (18, 1, requires 10 points in Restoration) — the build spends 12 here and still skips it. **Omen of Clarity is not Restoration** in OctoWoW; it sits in Balance (slot 10) and *is* taken. **Swiftmend** (9, 1) is no longer a capstone — "Row 7 → Row 3. Not a capstone: it sits at Row 3 and is reachable by any Druid with a light Restoration investment" — but it is not in this build. **Removed from Restoration:** "Improved Rejuvenation, Improved Enrage, and Insect Swarm, which is now baseline."

## 5. What this costs in `src/WowRoguelike.Core/Content.fs`

Three buckets. All nineteen talents the build spends in are listed exactly once. **The Moonkin currently runs `Talents.none`** — `let talents = if spec = TreeDruid then druidTalents else Talents.none` (`Content.fs:3901`) — so this is purely additive; nothing in `druidTalents` (`Content.fs:3414`) is touched.

### (a) Expressible today — data only (5)

| Talent | Concrete change |
|---|---|
| Improved Wrath 5/5 | One `Spells` line, `Spell = "Wrath"`, `CastTicksOff = ticks 10` (-0.5 s) on `wrathRanks` (`Content.fs:1442`). |
| Moonglow 3/3 | Two `Spells` lines with `CostBp = 900`: "Wrath" and "Moonfire" (`Content.fs:1442`, `Content.fs:1470`). |
| Improved Starfire 3/3 | `CastTicksOff = ticks 10` on a **new** `starfireRanks` `AbilityDef` — expressible as data, but the rank table has no source (§7) and the spell does not exist in the sim at all. |
| Improved Mark of the Wild 5/5 | `Talents.TotemEffectBp = 3500` (`Domain.fs:1152`, "Percent added to what a totem gives the party") is the right shape but is only read on totems; applied to `MarkOfTheWild` it means scaling the `+4 stats / +105 armour` case at `Content.fs:3109`. |
| Reflection 2/3 | `RegenBp = 1000` (`Domain.fs:1138`) — the field is documented as exactly this talent ("Reflection, Meditation"). |

### (b) Needs a new model axis (14)

| Talent | Missing axis |
|---|---|
| **Moonkin Form 1/1** | Three effects, two need axes. -20% Balance mana is `SpellTalent.CostBp` (data) and +180% item armour is `Traits.ArmorBonus` (a flat sum — the caster nine carry **238** armour at 24 (`Content.fs:3993-4001`), so 1.8 × 238 = **428**, the same arithmetic as Bear's 799 = 1.8 × 444 at `Content.fs:3916`). But **+3% party spell crit** has no home: `PartyBuffKind` (`Domain.fs:1363`) has no crit variant and `buffed` (`Content.fs:3100`) folds buffs into `(Stats, armor, ap)` only. New axis: a party-wide spell-crit contribution threaded to `Traits.CritBonusBp`. |
| Improved Moonfire 2/2 | `SpellTalent` has `HealBp` and `CritBp` but **no `DamageBp`** — per-spell damage is not a field. Crit half is `CritBp = 1000`. |
| Moonfury 3/3 | Same missing `SpellTalent.DamageBp`, on five spell names, three of which the sim does not have. |
| Vengeance 5/5 | **Critical damage multiplier.** The crit bonus is a constant; nothing scales it. |
| Natural Weapons 3/3 | **Spell hit.** `Traits` (`Domain.fs:1086`) has `CritBonusBp` and no hit field; the attack table's spell-miss is a table, not a trait. |
| Guidance of the Dream 3/3 | `Traits.PushbackResistanceBp` exists (`Domain.fs:1092`) but no `Talents` field reaches it — the same gap the Priest note found for Holy Focus. Axis: `Talents.PushbackResistanceBp`, folded where `healerTraits 500 7000` is called (`Content.fs:3920`). |
| Owlkin Frenzy 1/3 | Three at once: a **damage-taken proc**, a **pushback-resist buff**, and **percent-max-mana-per-second** regen. `Aura.Innervated` (`Domain.fs:284`) is the nearest regen aura but is keyed to Spirit regen, not to max mana. |
| Sylvan Blessing 2/2 | **On-kill proc** granting "100% mana regeneration while casting" — a state the sim does not have (`RegenBp` is a static talent number, not a timed buff). |
| Omen of Clarity 1/1 | **Proc model.** A chance-on-cast state that zeroes the next spell's cost; `Empower.FreeMana` (`Domain.fs:161`) exists but nothing procs it. |
| Nature's Reach 2/2 | **Per-spell range multiplier.** `Ability.Range` is a literal fixed at construction (`Domain.fs` `Ability`); no `SpellTalent.RangeBp`. |
| Nature's Grace 1/1 | `Empower` (`Domain.fs:157`) has `Instant`, `FreeMana`, `SureCrit` — **no cast-time reduction**. Axis: `Empower.CastTicksOff`. |
| Balance of All Things 3/3 | **Target-state-conditional modifiers**: mana refund if the target has Insect Swarm, +crit if the target has Moonfire. `healBp` and every `SpellTalent` field are caster-side; nothing reads the target's debuffs. |
| Eclipse 1/1 | **Two named stacking buffs with their own cooldowns and mutual exclusion**, whose magnitude depends on spell crit ("10% plus 60% of your spell critical strike chance") and which keys off **damage school** (Arcane vs Nature). No `Aura` carries a school-scoped damage bonus. |
| Subtlety 5/5 | **Threat multiplier.** `Effect.Threat of amount` (`Domain.fs`) adds threat; nothing scales it. |

### (c) Collapses or renames kit rows (0 in the build)

None — the Moonkin's kit rows are not renamed by anything in this build. Two things outside it still need deleting or deciding:

- **Improved Entangling Roots** and **Improved Thorns** are **removed** from Balance. The sim models neither, so there is nothing to delete; a port that adds them back would be inventing.
- **Nature's Swiftness is in the Moonkin's kit and is not in this build** (`Content.fs:3977` ships `naturesSwiftness` for every Druid spec; the Tree build reaches it by definition, this Balance build skips it at 12 Restoration points). The Moonkin Form tooltip restricts the form to "Balance spells, Innervate, and Remove Curse", so either drop the row for `MoonkinDruid` or accept that it is a button the form forbids. **UNVERIFIED** whether Nature's Swiftness is castable in form on OctoWoW (it is a buff, not a heal).

**Also to check, not modelled at all:** Starfire, Insect Swarm, Hurricane, Faerie Fire and the whole Starfire-dependent engine. `AreaDamage` (`Domain.fs`) exists, so Hurricane's *instant* area hit is expressible; its ground-targeted, channelled, ticking form is not.

## 6. The build decoded

Codec re-verified independently against the payload: 3 bits per slot, base64, `-`-separated, trailing zeros trimmed. **51 of 51.** Every slot maps to a real payload record and every rank spent is ≤ the payload's `ranks` — no mismatch.

```
Balance      (tree 0)  39 points  {0:5, 3:2, 4:3, 5:2, 6:3, 8:3, 10:1, 11:2, 13:5, 14:3, 16:1, 17:1, 18:1, 19:3, 21:3, 25:1}
   0 Improved Wrath 5/5 · 3 Sylvan Blessing 2/2 · 4 Guidance of the Dream 3/3
   5 Improved Moonfire 2/2 · 6 Natural Weapons 3/3 · 8 Moonfury 3/3
  10 Omen of Clarity 1/1 · 11 Nature's Reach 2/2 · 13 Vengeance 5/5
  14 Moonglow 3/3 · 16 Owlkin Frenzy 1/3 · 17 Moonkin Form 1/1
  18 Nature's Grace 1/1 · 19 Improved Starfire 3/3 · 21 Balance of All Things 3/3
  25 Eclipse 1/1
Feral        (tree 1)   0 points  {}
Restoration  (tree 2)  12 points  {1:5, 6:5, 11:2}
   1 Improved Mark of the Wild 5/5 · 6 Subtlety 5/5 · 11 Reflection 2/3
```

Prerequisites hold throughout: Owlkin Frenzy needs Moonkin Form 1/1 (taken), Nature's Grace needs Moonglow 1/1 (taken 3/3), Omen of Clarity needs Natural Weapons 1/1 (taken 3/3), Vengeance needs Improved Moonfire 1/1 (taken 2/2). Only two talents are partial: Owlkin Frenzy 1/3 and Reflection 2/3.

**It is a pure caster build.** 39 of 51 in Balance, the capstone **Eclipse**, and exactly three Restoration talents — none of which is a heal. **The Moonkin Form that the sim treats as a costume is a 1-point talent this build buys.**

**What it implies the Moonkin should be able to do, and the sim's kit cannot:**

1. **Cast Starfire.** Five of the sixteen Balance talents modify it (Improved Starfire, Vengeance, Moonfury, Balance of All Things, Eclipse). The sim's Moonkin kit is `entanglingRootsRanks; wrathRanks; moonfireRanks` plus three buttons (`Content.fs:3976-3977`) — Wrath and Moonfire are the *only* damage spells, and half this tree has no subject without a second nuke.
2. **Cast Insect Swarm and Hurricane.** Moonfury, Moonglow and Nature's Reach all name them; Insect Swarm is baseline from level 20 and is the trigger for Balance of All Things' mana refund.
3. **Proc.** Omen of Clarity (Clearcasting), Owlkin Frenzy (mana on being hit), Sylvan Blessing (mana on kill) are three of the sixteen — the build assumes a proc model the sim does not have.
4. **Be paid for what it already gets free.** The sim hands `MoonkinDruid` `healerTraits 500 7000` (`Content.fs:3920`) — +5% crit and 70% pushback avoidance. The 7000 is almost exactly **Guidance of the Dream 3/3's 70%**, which this build *does* buy, so that one is fair. The **+5% crit is not in the build at all**: this build buys crit only per-spell (Improved Moonfire +10% on Moonfire, Balance of All Things +9% on Starfire vs Moonfire), so the flat 500 is a grant OctoWoW does not make.
5. **Pay mana it is not paying.** Moonkin Form's -20% Balance mana and Moonglow's -9% compound; the sim's Moonkin pays full Wrath (rank 4, 70 mana) and full Moonfire (rank 2, 50 mana).

## 7. Missing evidence / UNVERIFIED

- **Starfire's damage is never stated** — not on the page, not in the payload (it is not a talent). A rank table cannot be written from these two sources. **UNVERIFIED.**
- **Hurricane's and Insect Swarm's numbers are absent** — only the 10 sec cooldown and "five ranks up to level 60" are given. **UNVERIFIED.**
- **Natural Weapons' text is in a lazy chunk** the payload does not inline (`tooltip` is `[…, "$L1f", "$L20", "$L21"]`). The two value lists for `tree 0 / talent 6` are `["3","6","10"]` and `["1","2","3"]`; the page says only "increases your chance to hit with melee attacks and spells". Which list is damage and which is hit is **UNVERIFIED**.
- **Four records do not parse from the payload at all** — `tree 0 / slot 11` (Nature's Reach), `tree 1 / slot 14` (Blood Frenzy), `tree 2 / slot 15` (Tranquil Spirit), `tree 2 / slot 25` (Tree of Life Form): their JSON is split across two RSC flight chunks mid-record. Nature's Reach was recovered from the raw chunk text; the other three are not needed for this build.
- **Owlkin Frenzy's cooldown renders as `once every 901[30/25/20] seconds`** — the `901` is an unresolved component reference, not a value. The cooldown is read as a per-rank list `[30/25/20]`; the page says only "Can trigger once every 30 sec", which matches rank 1. **UNVERIFIED** whether the 30 sec is the whole of it.
- **Moonkin Form's "spell critical chance increased by 3%"** carries no radius-independent qualifier beyond "within 30 yards" and no stacking rule. Whether two Moonkins stack is not stated. **UNVERIFIED.**
- **+180% armour is "the armor contribution from items"** — whether it multiplies the caster set's 238 before or after other multipliers is not stated; 428 is this note's arithmetic, not the server's.
- **Whether the calculator's tree is OctoWoW's live build** is inferred, not documented. Evidence for OctoWoW: Eclipse, Balance of All Things, Gale Winds, Sylvan Blessing, Guidance of the Dream, Owlkin Frenzy, Natural Weapons at 3 ranks, Insect Swarm as baseline and Omen of Clarity in Balance all exist in the payload and none exists in 1.12.

### Contradictions (summary page vs payload)

1. **Moonkin Form's row.** The page says "Row 7 → Row 5"; the payload puts it at **slot 17** with `reqBy:[16]` (Owlkin Frenzy depends on it) and the capstone at slot 25 is Eclipse. Slots are not rows, so nothing is contradicted — but the page's "Row 7 → Row 5" must not be used to order anything.
2. **Nature's Swiftness.** The page files it under Restoration with "Moved within the tree to make room for the new talents above" — reading A: unchanged, still an 11-point talent. The payload puts it at **slot 18 with `requires: 10`**, i.e. reachable after 10 points spent in the tree, not 11. Reading B (payload) is the one to trust for reachability; the sim's kit already ships it as an always-available button, which is neither reading.
3. **Omen of Clarity's tree.** 1.12 has it as a Restoration talent. The payload has it in **Balance at slot 10** (`requires: 6`, Natural Weapons) and the page lists it under *Balance* with "Procs from your offensive spell casts as well as your melee attacks". The page and payload agree; a port that reads "Restoration" from memory would be wrong.
4. **Improved Moonfire's rank count.** The page says "5 Ranks → 2 Ranks" and the payload gives `ranks: 2` — agreement, recorded because a 2-rank Improved Moonfire is the sort of thing a diff gets wrong by keeping the vanilla 5.
5. **Gale Winds.** The page says Hurricane "does not slow the attack speed of enemies caught in it without the new Balance talent Gale Winds"; the payload's Gale Winds (slot 22) says "Reduces the mana cost of Hurricane and gives it an attack-speed slow". Agreement, but note the page implies the slow is *only* available via the talent — so Hurricane in the sim should have no slow, and this build does not take Gale Winds anyway.

## 8. Not done

- The **Feral half** (Bear and Cat) — 18 payload records, the page's whole *New Abilities* feral section, and Berserk/Leader of the Pack/Heart of the Wild. Out of scope here.
- The **Tree of Life / Restoration half** — the sim's `druidTalents` is a 5/0/46 Restoration build (`Content.fs:3400`) and this note does not audit it against the page's Restoration changes.
- Every **UNVERIFIED** item in §7, in particular Starfire's rank table, which is the blocker for §5(a)'s Improved Starfire.
- Any change to `Content.fs`: this note is research, and the class layer rewrite is the parent item in `docs/agents/handoff.md`.
