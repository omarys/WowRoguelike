# Mage payload gaps — recovered server-rendered talent text (octowow.st)

Fills the §7 gap in `.pi/research/octowow/mage.md` ("29 of 84 talent records not recovered").
**Nothing in this file is UNRECOVERED.** All entries below are quoted verbatim from the live
server-rendered payload and are complete for the whole Mage talent panel (53 talents: 19 Arcane,
18 Fire, 16 Frost).

## Fetch method (why the earlier read failed)

The `?points=TAFOLISYZKAoB--FADD` URL is behind the BlazingFast JS challenge (curl returns an
`8849`-byte "Just a moment please…" page). The **unchallenged** variant
`https://octowow.st/talents/mage/` (no `points` parameter) returns the full **134 011-byte** SSR
document, and the talent data is not in the HTML text at all — it is in the Next.js RSC flight
payload, as `self.__next_f.push([1,"…"])` inline-script chunks whose **chunk boundaries fall
mid-talent-node** (e.g. `Accelerated Arcana`'s entry id `52` is split: the name is at the end of one
push chunk and `pl-1 text-lg\",\"children\":[\"#\",52]` starts the next). That is the mechanical
cause of the 29 missing records: slice-by-slice reading cut nodes in half. Reading the whole
document and walking the flight payload recovers everything.

Shape of each node (unescaped from the push strings):

```
["$","$L1c","<slot>",{"i":<i>,"index":<tree>,"icon":"…","ranks":<n>,
  "requires":<talentId|"$undefined">,"reqRanks":<n|"$undefined">,"reqBy":[],
  "tooltip":[
    ["$","h4",null,{"className":"tw-color","children":"<Name>"}],
    ["$","p",null,{"className":"dev-hidden:inline hidden pl-1 text-lg","children":["#",<entryId>]}],
    ["$","$L1d",null,{"tree":0,"talent":<i>,"ranks":<n>}],
    ["$","p",null,{"className":"whitespace-pre-wrap","children":[<text with $L1e value nodes>]}],
    ["$","p",null,{"className":"text-blue-gray …","children":["IDs: ","#…, #…"]}]
  ]}
```

`slot` = the `$L1c` id = the slot number used in the notes. `index` 0/1/2 = Arcane/Fire/Frost.
Value nodes are `["$","$L1e","1",{"tree":0,"talent":<i>,"values":["a","b","c"]}]` — one array per
`{n}` placeholder, and **the array length always equals the rank count** for every talent below.

---

## 1. Temporal Convergence — slot 14, 3 ranks (top priority, previously no effect text)

- **Verbatim:** "Your Arcane Missiles have a chance of [5/10/15]% to reset the cooldown of Arcane
  Rupture and cause it to return its base mana cost on its next cast.  This effect can occur once
  every 15 sec."
  (Two spaces before "This effect" are in the payload.)
- **Per-rank values:** one array, 3 values for 3 ranks — `["5","10","15"]` → 5% / 10% / 15%.
- **Spell IDs:** #51963, #51964, #51965. **Talent entry id:** #48. **Icon:** `spell_nature_stormreach`.
- **Requires:** `"requires":10,"reqRanks":1` — talent id 10 = **Arcane Rupture**, 1 rank. Confirms
  the notes' "requires Arcane Rupture 1".
- **Note:** confirms §4's "Row 4, 3 Ranks: new; requires Arcane Rupture 1" and adds the entire
  missing effect. Not a passive stat: it is a **cooldown-reset + mana-refund proc on Arcane
  Missiles** with a **15 sec internal cooldown**.

## 2. Arcane Potency — slot 19, 2 ranks (number was uncertain as "[100?]%")

- **Verbatim:** "Increases the critical strike damage bonus of your Arcane spells by [50/100]%."
- **Per-rank values:** one array, 2 values for 2 ranks — `["50","100"]` → 50% / 100%.
- **Spell IDs:** #45403, #45404. **Talent entry id:** #53. **Icon:** `inv_enchant_essencemagiclarge`.
- **Note:** **corrected** — the "100?" was rank 2; the ladder is 50% then 100%. It multiplies the
  *crit damage bonus*, not total crit damage.

## 3. Resonance Cascade — slot 22, 5 ranks (was an elided 3-value "[12/16/20]")

- **Verbatim:** "Your successful damaging Arcane spells have a [4/8/12/16/20]% chance to duplicate
  for 50% of the damage.  This effect can trigger off of itself and duplicated Arcane Missiles are
  channeled in tandem with the original cast."
- **Per-rank values:** one array, **5 values for 5 ranks** — `["4","8","12","16","20"]`.
- **Spell IDs:** #51262, #51987, #51988, #51989, #51990 (**five** IDs, one per rank).
  **Talent entry id:** #54. **Icon:** `spell_mage_focusingcrystal`.
- **Note:** **corrected** — the ladder is 4/8/12/16/20, not starting at 12. The notes' "12/16/20"
  were ranks 3–5. The notes' next sentence is confirmed verbatim, plus a new clause: duplicated
  Arcane Missiles are **channeled in tandem** with the original cast.

## 4. Accelerated Arcana — slot 18, 1 rank (previously no number)

- **Verbatim:** "Increases the casting speed of your Arcane spells by 5%.  In addition, casting
  speed increase effects increase the cooldown recovery speed of your Arcane spells and the tick
  speed of your Arcane Missiles, reducing their duration."
- **Per-rank values:** **none** — the 5% is a hardcoded literal in the text, not a `$L1e` value
  node. There is no per-rank array (1 rank).
- **Spell ID:** #51981. **Talent entry id:** #52. **Icon:** `spell_arcane_portaldarnassus`.
- **Note:** **supplies the missing number** — 5% casting speed, flat. Confirms the notes' mechanic
  reading (haste also compresses Arcane cooldown recovery and Arcane Missiles tick interval).

## 5. Arcane Instability — slot 16, 3 ranks (settles the "rework vs flat +spell damage/crit" split)

- **Verbatim:** "Your successful damaging Arcane spells have an [8/16/25]% chance to uncontrollably
  erupt, consuming 2% of your base mana to deal 25% more damage."
- **Per-rank values:** one array, 3 values for 3 ranks — `["8","16","25"]` → 8% / 16% / 25%.
- **Spell IDs:** #15058, #15059, #15060. **Talent entry id:** #50. **Icon:** `spell_shadow_teleport`.
- **Note:** **confirms** the page's rework and **fills the elided middle rank** (`[16/…/25]` → 8/16/25).
  The payload text contains **no** "+spell damage" and **no** "+crit chance" wording at all: the
  effect is purely a **proc** (damaging Arcane spell → 8/16/25% chance to erupt → costs 2% of *base*
  mana → deals +25% more damage). The repo's landed "flat +spell damage and crit" reading is **not**
  supported by the payload.

## 6. Resistance talents — full per-rank arrays

### Arcane Subtlety — slot 0, 2 ranks
- **Verbatim:** "Reduces your target's resistance to all your spells by [5/10] and reduces the
  threat caused by your Arcane spells by [20/40]%."
- **Per-rank values:** **two** arrays, both 2 values for 2 ranks — `["5","10"]` and `["20","40"]`.
  Placeholder `{0}` = flat resistance reduction (**no percent sign in the text**), `{1}` = threat
  reduction in %.
- **Spell IDs:** #11210, #12592. **Talent entry id:** #37. **Icon:** `spell_holy_dispelmagic`.
- **Note:** **partially corrected** — the notes' "[10]%" wrongly gave the rank-2 resistance value as
  a percentage; it is 5 then 10 **flat resistance points**, and the threat ladder is 20/40%.

### Magic Absorption — slot 1, 3 ranks
- **Verbatim:** "Increases all resistances by [4/7/10] and whenever one of your spells is partially
  or fully resisted you restore [1/2/3]% of your maximum mana.  This effect can occur once every
  2 sec."
- **Per-rank values:** **two** arrays, both 3 values for 3 ranks — `["4","7","10"]` and `["1","2","3"]`.
- **Spell IDs:** #29441, #29444, #29445. **Talent entry id:** #38. **Icon:** `spell_nature_astralrecalgroup`.
- **Note:** **corrected** — the notes' "[10] resistance" was the rank-3 value only; ladder is
  4/7/10. The mana refund is **[1/2/3]% of maximum mana**, which the notes left as "[mana]".
  The 2 sec ICD confirms the notes' "1 sec → 2 sec".

### Arcane Focus — slot 5, 5 ranks
- **Verbatim:** "Reduces the chance that the opponent can resist your Arcane spells by [2/4/6/8/10]%."
- **Per-rank values:** one array, **5 values for 5 ranks** — `["2","4","6","8","10"]`.
- **Spell IDs:** #11222, #12839, #12840, #12841, #12842 (**five**, one per rank).
  **Talent entry id:** #41. **Icon:** `spell_holy_devotion`.
- **Note:** confirms the % reading and supplies the full ladder (flat resist-chance reduction, no
  resistance stat involved).

---

## Bonus recoveries (unasked, but they settle other open lines in `mage.md`)

- **Arcane Power** (25, 1), #12042, entry #42: the file's explicit **UNVERIFIED** mana drain resolves to
  **1%**. Verbatim: "When activated, your casting speed is increased by 30% while draining 1% of your
  maximum mana every second and reducing all mana gain by 50%. Falling below 10% maximum mana causes
  you to violently combust from uncontrollable power, killing you instantly. Lasts 20 sec and cannot
  be cancelled."
- **Arcane Impact** (9, 3), #11242, #12467, #12469: values `["2","4","6"]` → max **+6%** crit chance,
  not the notes' "[10]%". Verbatim: "Increases the critical strike chance of your Arcane spells by an
  additional [2/4/6]%."
- **Improved Mana Shield** (12, 2), #11252, #12605: **payload gap on the server's side** — the text
  reads "Decreases the mana lost per point of damage taken when Mana Shield is active by %." with an
  **empty values array**. There is no number in the payload to recover; recording it as
  **UNRECOVERED (server-side)** is the honest result.
- **Arcane Concentration** (6, 5): `["2","4","6","8","10"]`. Verbatim: "Gives you a [2/4/6/8/10]%
  chance of entering a Clearcasting state after any damage spell hits a target.  The Clearcasting
  state reduces the mana cost of your next damage spell by 100%.  This effect can occur once every
  8 sec." (IDs line is absent from the payload for this talent.)
- Other verbatim Arcane texts recovered in the same pass, if useful: Arcane Subtlety is slot 0 and
  Improved Arcane Missiles (2, 5) is `["20","40","60","80","100"]`; Wand Specialization (4, 2) is
  `["13","25"]` damage + `["5","10"]` hit + "Successful Wand attacks have a chance to restore 90
  mana."; Arcane Meditation (15, 3) is `["7","14","20"]` tripled below 35% mana; Improved
  Counterspell (13, 2) is `["50","100"]`; Arcane Rupture (10, 1) is "Rupture your target with the
  force of concentrated arcane energy, dealing 101 to 114 Arcane damage and increasing the damage
  and mana cost of your Arcane Missiles spell by 20% for 8 sec."

## Correction summary vs the notes' current reading

| Talent | Slot | Notes said | Payload says |
|---|---|---|---|
| Temporal Convergence | 14 | no effect text | 5/10/15% chance on Arcane Missiles to reset Arcane Rupture CD + refund base mana, 15 sec ICD |
| Arcane Potency | 19 | "[100?]%" | 50/100% crit-damage-bonus |
| Resonance Cascade | 22 | "[12/16/20]%" for 5 ranks | 4/8/12/16/20% |
| Accelerated Arcana | 18 | no number | 5% Arcane cast speed, flat |
| Arcane Instability | 16 | "[16/…/25]%" | 8/16/25% proc, 2% base mana, +25% damage |
| Arcane Subtlety | 0 | "[10]%" resist, "[40]%" threat | 5/10 **flat** resist, 20/40% threat |
| Magic Absorption | 1 | "[10]" resist, "[mana]" | 4/7/10 resist, 1/2/3% max mana, 2 sec ICD |
| Arcane Focus | 5 | percentages unknown | 2/4/6/8/10% |
| Arcane Power (bonus) | 25 | drain % UNVERIFIED | 1% per second |
| Improved Mana Shield (bonus) | 12 | — | UNRECOVERED: server text has an empty value slot |

## What remains unverified after this pass

- Improved Mana Shield's percentage — genuinely absent from the payload (empty values array), not a
  fetch failure.
- Tooltip *rendering* order/CSS is reported from the flight payload, not from a headless-browser DOM
  render; the strings and numbers themselves are the server's.
- The `?points=…` variant was **not** read (challenge-protected). The `points` parameter only
  pre-allocates ranks; it does not change talent text, so this does not affect any number above, but
  rank-by-rank "spent" display values were never observed.
