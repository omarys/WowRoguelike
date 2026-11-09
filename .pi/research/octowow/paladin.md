# Research: OctoWoW Paladin — class changes

Prose source: **<https://octowow.st/paladin>**, fetched in full. Numbers, rank counts, prerequisites and
tree/slot names come from the server-rendered payload of
**<https://octowow.st/talents/paladin/?points=AoaAYQdQpAYoB--FYQAAI>** (`fetch_content` mode `raw`):
per-talent `i`, `ranks`, `requires`/`reqRanks`/`reqBy`, per-rank `values`, spell IDs. Quoted prose is
the summary page; `payload:` marks a value that exists only in the calculator. Nothing is filled in from
vanilla 1.12 memory.

Status: **prose complete, coefficients absent.** Full ladders exist for Crusader Strike and per-rank
values for five talents; no percentage exists anywhere the page says "a percentage of weapon damage".

---

## 1. New abilities

| Ability | Cost / cooldown (verbatim) | Effect |
|---|---|---|
| **Holy Strike** (lvl 4) | "eight ranks up to level 60, Cooldown: 6 sec"; "5 mana at Rank 1, 90 at Rank 8" | "Strike the target for a percentage of weapon damage as Holy damage, restoring health and mana to you and nearby allies" — percentage, heal, mana return and radius all **UNVERIFIED** |
| **Crusader Strike** (lvl 10) | ranks at "10 / 22 / 34 / 46 / 58"; mana 20/40/70/100/120 | damage 30/46/70/106/160; "+5/9/14/21/29 bonus Holy per stack", "stacking up to 5 times for 30 sec" |
| **Hand of Reckoning** (lvl 10) | "Cooldown: 10 sec" | "Taunts the target to attack you. Has no effect if the target is already attacking you." No cost, range or threat figure: **UNVERIFIED** |
| **Bulwark of the Righteous** (Protection capstone) | 1-rank talent, "three ranks of the ability" | payload: "dealing 274 to 301 Holy damage and reducing the damage you take by 30% for 12 sec"; ranks 2–3 **UNVERIFIED** |

## 2. Class mechanics

**Judgement is refreshed by swings, not recast.** "Your auto-attack swings refresh your own Judgement
debuff on the target: any swing that lands for damage resets its timer … Crusader Strike and Holy Strike
go further and refresh every Paladin's Judgement on that target, not only your own." A change to who
owns the debuff and to what refreshes it; nothing on the page touches its damage.

**Seals, blessings, auras.** "Players may only have one Blessing on them per Paladin at any one time"
(payload, on Blessing of Sanctuary and Blessing of Kings); the page works around the rule in naming only:
"Hand of Freedom and Hand of Protection: Named as Hands rather than Blessings, so they do not clash with
the one-Blessing-per-target rule in players' heads." Auras move between trees rather than changing (§4).
"Seal of Wisdom has five ranks"; Judgement stays the seal-unleash and gains no independent scaling.

**Procs.** Three, all chance-on-event: "Redoubt: Procs from any successful melee or ranged attack
against you"; "Reckoning: Procs after you block"; Holy Shock's "chance to have no cooldown for its next
cast". Payload curves: Redoubt 2/4/6/8/10%, Reckoning 10/20/30/40/50%; Holy Shock's chance **UNVERIFIED**.

## 3. Baseline spell changes

| Spell | Change (verbatim) |
|---|---|
| Consecration | "Baseline from level 20. Rank 5 costs 205 mana, and its damage wanes as the effect runs rather than ticking evenly." No damage figures. |
| Holy Shock | "Four ranks"; payload rank 1: "97 to 104 Holy damage to an enemy, or 311 to 320 healing to an ally. Casting Holy Shock has a chance to have no cooldown for its next cast." Ranks 2–4 **UNVERIFIED** (#20473 is the only spell ID). |
| Flash of Light | "gains a seventh rank at level 60" (the sim's ladder stops at rank 3, level 34) |
| Seal of Wisdom | "has five ranks" |
| Blessing of Light | "Also increases the effect of Holy Shock on the target, not just Holy Light and Flash of Light." |
| Exorcism | "Generates reduced threat." Amount **UNVERIFIED**. |
| Hand of Freedom, Hand of Protection | Renamed from Blessings (§2) |
| Blessing of Sanctuary | "Reduces damage taken from all sources." Payload narrows it: "by up to 10 for 10 min", plus 14 Holy to the attacker on a block. See Contradictions. |

## 4. Talent changes

Slot numbering is the payload's: 28 slots per tree, **slot = row×4 + column**; the page's "Row N" is the
1-indexed row, so slot 25 is Row 7. Tree order is Holy, Protection, Retribution (labels sit at the same
document positions as the three grids).

**Holy.** New: **Daybreak** (Row 7, slot 25, 1 rank, `requires` Holy Shock 1) — "Critically healing an
ally applies Daybreak for 30 sec. When the ally takes damage, Daybreak is consumed to heal them for 248
to 289"; **Blessed Strikes** (Row 6, slot 22, 5 ranks, `requires` Holy Shock 1) — "Crusader Strike has a
20/40/60/80/100% chance to reset the cooldown of your Holy Shock … Holy Strike['s] healing … is
increased by 20/40/60/80/100% and benefits from an additional 5/10/15/20/25% of your healing power";
**Ironclad** (Row 4, slot 14, 2 ranks) — "up to 1/2% of your Armor from items"; **Holy Judgement** (Row
2, slot 4, 3 ranks) — "reduces the casting time of your next Holy Light by 0.3/0.6/1 sec".
Moved: **Holy Shock** "Row 7 → Row 5" (slot 17), "Not the capstone, so a Holy Paladin reaches it far
sooner"; **Sanctity Aura** (slot 9) and **Improved Concentration Aura** (slot 12), "Both moved into
Holy". Reworked: **Divine Favor** "1 Rank → 5 Ranks … A passive that improves your chance to critically
strike with Holy Shock" (payload 10/20/30/40/50%, slot 16, `requires` Holy Shock 1). Rank cuts: **Holy
Power** "5 Ranks → 3 Ranks" (payload 2/4/6% crit on Holy Light and Flash); **Spiritual Focus** "5 Ranks
→ 2 Ranks" (payload 35/70% — the 70% endpoint survives at two ranks). Numbers the page withholds:
**Illumination** "Always returns a percentage of the spell's base cost after a critical heal" — payload
**12/24/36/48/60%**; **Healing Light** "Also increases Holy Shock's healing" — payload 4/8/12% on Holy
Light, Flash of Light and Holy Shock. **Removed:** "Lasting Judgement, Improved Blessing of Wisdom, and
Consecration, which is now baseline"; none is in the payload's Holy tree. Untouched: Divine Strength,
Divine Intellect, Improved Seal of Righteousness, Unyielding Faith, Improved Lay on Hands.

**Protection.** New: **Bulwark of the Righteous** (Row 7, slot 25, `requires` Holy Shield 1), "an active
mitigation button for Protection"; **Righteous Defense** (Row 5, slot 16, 3 ranks, `requires` Improved
Righteous Fury 3) — "While Righteous Fury is active, your damage taken is reduced by 3/6/10%";
**Improved Hand of Reckoning** (Row 4, slot 13, 2 ranks) — "+4/8%" to hit.
Replacement: **Righteous Strikes** (Row 6, slot 22, 5 ranks) "replaces One-Handed Weapon
Specialization" — Holy Strike threat +5/10/15/20/25% and damage +5/10/15/20/25%, and Crusader Strike
grants Zealous Defense, "reducing the damage of the next blocked attack by 6/12/18/24/30%".
Procs widened: **Redoubt** fires on any melee **or ranged** attack against you, not only crits;
**Reckoning** "Procs after you block". Moved: **Holy Shield** "Row 7 → Row 5" (slot 17), "Its damage
generates 50% additional threat" (payload: 45% block for 10 sec, 35 Holy on block, 4 charges);
**Blessing of Sanctuary** "Row 5 → Row 3" (slot 9). Widened: **Shield Specialization** also has "a
chance to restore 2% of maximum mana when you block, no more than once every 5 sec" (payload
33/66/100%); **Precision** "Increases your chance to hit with spells as well as melee attacks".
Rank cut: **Anticipation** "5 Ranks → 3 Ranks" (payload 7/14/20 Defense).
**Removed:** "One-Handed Weapon Specialization. Blessing of Kings moved to Retribution" (Kings is still
one rank, now Retribution slot 10).

**Retribution.** New: **Vengeful Strikes** (Row 6, slot 21, 5 ranks) — "Crusader Strike deals an
additional 2/4/6/8/10% damage and Zeal increases your attack and casting speed by an additional 2% per
stack. Holy Strike infuses you with Holy Might, increasing your Strength by 4/8/12/16/20% for 20 sec";
**Improved Blessings** (Row 1, slot 1, 5 ranks) "replaces Improved Blessing of Might … Covers Blessing
of Wisdom as well as Blessing of Might, in one talent" (payload 4/8/12/16/20%).
Moved: **Vengeance** "Row 6 → Row 5" (slot 17, `requires` Conviction 5), with a threat rider the page
omits — "decreases threat you generate by 2/4/6/8/10% … does not apply … under Righteous Fury";
**Vindication** "Row 3 → Row 4" (slot 14), "Reduces the target's damage dealt" (payload 3/6/9%);
**Two-Handed Weapon Specialization** "Row 5 → Row 4" (slot 12), also "+1/2/3 weapon skill"; **Seal of
Command** "Row 3 → Row 5" (slot 18) — payload "70% of melee damage", judge 68–72 (137–145 if stunned)
"+15% of your attack power"; **Blessing of Kings** "Moved out of Protection into Retribution Row 3"
(slot 10); **Repentance** and **Eye for an Eye / Improved Retribution Aura** "Repositioned within the
tree" (slots 25, 16, 8; Repentance's rider is "80 Holy damage each time they perform a melee attack for
20 sec" against immune targets). **Removed:** "Improved Blessing of Might". Sanctity Aura leaves for Holy.

## 5. What this costs in `src/WowRoguelike.Core/Content.fs`

Three buckets, counted over the 17 talents this build takes (§6): **8 expressible today, 8 needing a new
axis, 1 collapsing a kit row.** Kings moved from (b) to (a) after being priced and built — see the end of
this section.

**(a) Expressible today — coefficient, rank, cooldown, cost edits. 7.** Divine Intellect 5/5 →
`IntellectBp = 1000`, already there. Spiritual Focus 2/2 → `healerTraits 500 7000` already carries the
70% pushback endpoint, and since the era pushback model landed (`SimCombat.fs:462`) that endpoint is
70% of the halves of a second that a hit costs a cast, not 70% of a chance to keep the cast at all. Healing Light 3/3 → `HealBp = 1200` already on Flash of Light; extend that line
to Holy Light and Holy Shock. Illumination 5/5 → `ManaRefundOnCritBp` **10000 → 6000** — a nerf to live
behaviour, since payload 5/5 refunds 60%, not the whole cost. Holy Power 3/3 → `SpellTalent { Spell =
"Holy Light"; CritBp = 600 }` plus the Flash of Light twin; the sim's comment already claims "Holy Power
5/5 (+5% crit)" while `paladinTalents` sets no `CritBp` at all, so comment and record disagree before this
port starts. Benediction 3/5 → `SpellTalent { Spell = "Judgement"; CostBp = 900 }`. Improved Judgement
2/2 → `CooldownOff = [ "Judgement", secTicks -2.0 ]`, Judgement 10 s → 8 s.

**(b) Needs a new model axis. 8.** Holy Judgement 3/3 — `Empower` is `{ Instant; FreeMana; SureCrit }`
with **no cast-time field**, and no Judgement-cast trigger. Improved Lay on Hands 2/2 — Lay on Hands is
not in the kit and no `Effect` raises a target's armour (`CritArmorBp` is talent-wide, crit-triggered).
Improved Concentration Aura 3/3 — no aura system, no resistance to `Silence`. Ironclad 2/2 — no armour →
healing conversion; armour is a `Gear` total with nothing downstream. Holy Shock 1/1 — `TargetKind` is
single-valued (`Foe`/`Ally`/`Self`/`Corpse`), so "damage to an enemy, or healing to an ally" needs an
**either-target axis**, plus a proc-on-cast axis for the cooldown wipe. Blessed Strikes 5/5 — a
cooldown-reset proc axis and a **healing-power** scaling axis (`SpiritToHealingBp` converts Spirit;
nothing converts healing power). Daybreak 1/1 — a **reactive** heal: every `Effect` resolves at cast
time, nothing fires on taking damage. Improved Blessings 5/5 — the mechanic exists but is named
`TotemEffectBp` ("percent added to what a totem gives the party"); blessing effectiveness is the same
shape and a different axis, so reuse-and-rename or a second field is a decision, not a constant.
Blessing of Kings 1/1 — **landed.** A `PartyBuffKind` case whose branch multiplies all five stats by
10%; `buffed` now folds the flat branches first and applies one percentage to the total, which is the
order the game uses, and `Talents.PartyBuffs` is how a *talent* hands the party a passive. Armour and
attack power are not stats, so Kings does not multiply them. Priced first on a twelve-seed A/B at 60:
**−5.4% clear time (library), −2.5% (armory)**, 11/12 and 9/12 seeds faster, all seeds clearing in both
arms, party end-state unchanged.

Out of the build but in the same file: Holy Strike's "percentage of weapon damage as Holy" needs a damage
school on `WeaponDamage` (it runs the physical armour pipeline today), and Crusader Strike's stacking
"+Holy damage taken" has no analogue — `Sunder` is the only debuff touching a mitigation number.

**(c) Collapses or renames a kit row. 1.** **Divine Favor 5/5.** The sim ships a `divineFavor` cooldown
row ("the next healing spell is a guaranteed crit", 2 min, 21 mana). OctoWoW's Divine Favor is a passive
5-rank Holy Shock crit talent gated behind Holy Shock: same name, different thing, so the kit row must be
**deleted**, not tuned, and its replacement is a bucket-(a) `SpellTalent` line. A diff that adds the crit
line without removing the cooldown leaves both live.

## 6. The build decoded

`AoaAYQdQpAYoB--FYQAAI` = **40 / 0 / 11 = 51 of 51**. Every slot binds to a payload talent whose rank
count matches the ranks spent, so the mapping is checkable rather than assumed:

```
Holy (tree 0), 40 points — slot:ranks
  2:5 Divine Intellect        4:3 Holy Judgement         5:2 Spiritual Focus
  8:3 Healing Light          10:2 Improved Lay on Hands 12:3 Improved Concentration Aura
 13:5 Illumination           14:2 Ironclad              16:5 Divine Favor
 17:1 Holy Shock             20:3 Holy Power            22:5 Blessed Strikes
 25:1 Daybreak
Protection (tree 1): none
Retribution (tree 2), 11 points
  1:5 Improved Blessings      2:3 Benediction            4:2 Improved Judgement
 10:1 Blessing of Kings
```

**A healer build, not a tank or retribution build.** 40 of 51 points are in Holy, it commits to Holy, and
its 31-point talent is **Daybreak** (slot 25, Row 7, `requires` Holy Shock) — crit a heal, then absorb
one hit for 248–289. The 11 Retribution points buy Kings as their only 1-point talent and stop at the
10-point row gate; none of them is a damage talent, and Seal of Command (slot 18, needs 20 points) is
left on the table.

Against what the sim already models: it **takes** Divine Intellect, Spiritual Focus, Healing Light and
Illumination — the four the `paladinTalents` comment names — and **leaves** Divine Strength, Improved
Seal of Righteousness, Sanctity Aura, Unyielding Faith and all four Protection capstones. It does take
Holy Power, which the sim's comment claims but its record does not implement. Being 0 in Protection,
four bucket-(b) tank axes (Bulwark, Righteous Strikes, Righteous Defense, Shield Specialization) are out
of scope for *this* build even though they are the class's biggest changes.

## Contradictions

- **Blessing of Sanctuary.** Summary: "Reduces damage taken from all sources" — no magnitude, and
  `ReducesDamage` in this model takes a percentage. Payload: "reducing damage taken from all sources by
  **up to 10** for 10 min", a flat per-hit number. Both readings are on the same server; the flat one is
  what the calculator carries.
- **Holy Shock ranks.** Summary: "Four ranks". Payload: one tooltip, one spell ID (#20473), rank-1 values
  only. A reader following the summary expects four ladders and the calculator shows one.
- **Illumination.** Summary: "Always returns a percentage of the spell's base cost", no number; payload
  12–60%. Recorded, not reconciled: a 60% ceiling is far short of classic's refund and the page does not
  say whether that is intended.
- **Eye for an Eye / Improved Retribution Aura.** Summary: "Repositioned within the tree." Payload: slots
  16 and 8. The summary gives no origin positions, so "repositioned" cannot be checked.

## 7. Missing evidence / UNVERIFIED

- **No coefficients anywhere.** Holy Strike's percentage, Consecration's per-tick damage, Exorcism's
  threat multiplier, Hand of Reckoning's cost and range: all **UNVERIFIED**. Nothing implementable comes
  out of the prose alone.
- **Holy Strike's heal and mana return** — neither amount nor radius stated; the class's signature
  ability is unmodellable from this page.
- **Holy Shock ranks 2–4**, **Consecration ranks other than 5**, **Bulwark's other two ranks** — absent.
- **Which changes are live** on OctoWoW's current build versus planned is nowhere stated.
- **Whether the calculator's tree is OctoWoW or vanilla-plus-partial** is inferred, not documented.
  Evidence for OctoWoW: Daybreak, Blessed Strikes, Ironclad, Holy Judgement, Vengeful Strikes and Bulwark
  all carry spell IDs in the 513xx range and none exists in 1.12.
- The **trigger/state machinery** these new talents need (reactive heals, cooldown resets, empowered
  next-cast) has no representation in `Domain.fs`; what a triggered heal should cost in ticks is a design
  question this research cannot answer.
- The payload's `requires` for Divine Favor points at Holy Shock in the **same** row. The site permits it;
  whether the client does is **UNVERIFIED**.

## Not done

- Eight other classes.
- Coefficients for anything in §7.
- Any change to `Content.fs`: this note is research, and the class layer rewrite is the parent item in
  `docs/agents/handoff.md`.
