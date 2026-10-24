# Research: Vanilla 1.12 / Classic Era Druid — complete trainable rank table (levels 1–60)

## Summary
Primary source is **wowcalc.net/en/classic/wiki/spells/druid** (per-rank table read from the live
Classic Era client, build 1.15.9.70003, 1.12 mechanics), used for level / cost / cast / cooldown /
range / magnitude. It reproduces the on-disk 1.12 evidence for every ability it overlaps on (Thorns r3
= 105 mana / 9 per hit, Faerie Fire r1 = 55 mana / −175 armour, Entangling Roots r6 = 125 mana / 270
over 27s, Wrath r4 = 70 mana, Moonfire r4 = 105 mana), so its cost columns are 1.12-consistent. A few
*direct* magnitudes run ~6% above the twinhead-1.12 tooltips in `.pi/research/level24/druid.md` (DoT/HoT
components match exactly); those rows are flagged in `notes` and in **Contradictions**. wow4ever.quest
is a **custom server** (Wrath r4 50 vs 70 mana, Regrowth r3 170 vs 280 mana) and is used only to
cross-read cost/cast/cd/range gaps.

Druids **use combo points, in Cat Form only** (max 5; Claw/Shred/Ravage/Rake/Pounce award 1; Rip and
Ferocious Bite consume them). Resources per form: caster = **Mana**; Bear/Dire Bear = **Rage**
(cap 100, gained from white damage dealt/taken, constant out-of-combat decay, pool size and decay rate
do not scale — vanilla-wow-archive/wiki/Rage); Cat Form = **Energy** (cap 100, pre-3.0.2 regen = 20
energy per 2 s tick = 10/sec, pool and rate do not scale with level —
warcraft.wiki.gg/wiki/Energy patch 3.0.2 note). All shapeshifts cost **Mana** (55% / 20% of base mana),
so they are castable only from caster form.

Prose deviations from the mini-language: `Stun Xs` is used for Bash/Pounce hard stuns (the vocabulary
only offers `Root`, which implies damage-breakable CC); `HoT x/2s` totals are written as the full
10 s channel total; `ApBuff -40/30s` is used for Demoralizing Roar's *enemy* AP debuff.

## Data (one row per rank)

```tsv
ability	rank	learn_level	resource	cost	cast_s	cd_s	range	effects	stance	source_url	confidence	notes
Healing Touch	1	1	Mana	25	1.5	0	40	Heal 40-54	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Healing Touch	2	8	Mana	55	2	0	40	Heal 94-118	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Healing Touch	3	14	Mana	110	2.5	0	40	Heal 204-252	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Healing Touch	4	20	Mana	185	3	0	40	Heal 376-458	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	twinhead 1.12: 363-446
Healing Touch	5	26	Mana	270	3.5	0	40	Heal 589-711	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Healing Touch	6	32	Mana	335	3.5	0	40	Heal 762-914	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Healing Touch	7	38	Mana	405	3.5	0	40	Heal 958-1142	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Healing Touch	8	44	Mana	495	3.5	0	40	Heal 1224-1452	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Healing Touch	9	50	Mana	600	3.5	0	40	Heal 1545-1825	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Healing Touch	10	56	Mana	720	3.5	0	40	Heal 1916-2256	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Healing Touch	11	60	Mana	800	3.5	0	40	Heal 2267-2677	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Regrowth	1	12	Mana	120	2	0	40	HoT 98/21s +direct 92-106	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Regrowth	2	18	Mana	205	2	0	40	HoT 175/21s +direct 176-200	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Regrowth	3	24	Mana	280	2	0	40	HoT 259/21s +direct 255-289	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	twinhead 1.12: direct 240-275
Regrowth	4	30	Mana	350	2	0	40	HoT 343/21s +direct 335-377	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Regrowth	5	36	Mana	420	2	0	40	HoT 427/21s +direct 425-477	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Regrowth	6	42	Mana	510	2	0	40	HoT 546/21s +direct 534-598	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Regrowth	7	48	Mana	615	2	0	40	HoT 686/21s +direct 672-750	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Regrowth	8	54	Mana	740	2	0	40	HoT 861/21s +direct 839-935	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Regrowth	9	60	Mana	880	2	0	40	HoT 1064/21s +direct 1003-1119	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Rejuvenation	1	4	Mana	25	0	0	40	HoT 32/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Rejuvenation	2	10	Mana	40	0	0	40	HoT 56/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Rejuvenation	3	16	Mana	75	0	0	40	HoT 116/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Rejuvenation	4	22	Mana	105	0	0	40	HoT 180/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	druid.md mislabels 244 rank as level 22
Rejuvenation	5	28	Mana	135	0	0	40	HoT 244/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Rejuvenation	6	34	Mana	160	0	0	40	HoT 304/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Rejuvenation	7	40	Mana	195	0	0	40	HoT 388/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Rejuvenation	8	46	Mana	235	0	0	40	HoT 488/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Rejuvenation	9	52	Mana	280	0	0	40	HoT 608/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Rejuvenation	10	58	Mana	335	0	0	40	HoT 756/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Rejuvenation	11	60	Mana	360	0	0	40	HoT 888/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Tranquility	1	30	Mana	375	10	300	0	HoT 485/10s	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	channeled party heal, 97 per 2s
Tranquility	2	40	Mana	505	10	300	0	HoT 710/10s	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	channeled party heal, 142 per 2s
Tranquility	3	50	Mana	695	10	300	0	HoT 1050/10s	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	channeled party heal, 210 per 2s
Tranquility	4	60	Mana	925	10	300	0	HoT 1470/10s	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	channeled party heal, 294 per 2s
Mark of the Wild	1	1	Mana	20	0	0	30	ArmorBuff +25/1800s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	learn level only
Mark of the Wild	2	10	Mana	50	0	0	30	ArmorBuff +65/1800s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	+2 all attributes
Mark of the Wild	3	20	Mana	100	0	0	30	ArmorBuff +105/1800s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	+4 all attributes
Mark of the Wild	4	30	Mana	160	0	0	30	ArmorBuff +150/1800s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	+6 attrs, +5 resist
Mark of the Wild	5	40	Mana	240	0	0	30	ArmorBuff +195/1800s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	+8 attrs, +10 resist
Mark of the Wild	6	50	Mana	340	0	0	30	ArmorBuff +240/1800s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	+10 attrs, +15 resist
Mark of the Wild	7	60	Mana	445	0	0	30	ArmorBuff +285/1800s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	+12 attrs, +20 resist
Gift of the Wild	1	50	Mana	900	0	0	40	ArmorBuff +240/3600s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	party-wide; +10 attrs, +15 resist
Gift of the Wild	2	60	Mana	1200	0	0	40	ArmorBuff +285/3600s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	party-wide; +12 attrs, +20 resist
Thorns	1	6	Mana	35	0	0	30	Reflect 3/600s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	10 min buff
Thorns	2	14	Mana	60	0	0	30	Reflect 6/600s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	10 min buff
Thorns	3	24	Mana	105	0	0	30	Reflect 9/600s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	10 min buff; resolves earlier "?"
Thorns	4	34	Mana	170	0	0	30	Reflect 12/600s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	10 min buff
Thorns	5	44	Mana	240	0	0	30	Reflect 15/600s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	10 min buff
Thorns	6	54	Mana	320	0	0	30	Reflect 18/600s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	10 min buff
Faerie Fire	1	18	Mana	55	0	0	30	ArmorDebuff -175/40s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	also blocks stealth; resolves earlier U
Faerie Fire	2	30	Mana	75	0	0	30	ArmorDebuff -285/40s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	also blocks stealth
Faerie Fire	3	42	Mana	95	0	0	30	ArmorDebuff -395/40s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	also blocks stealth
Faerie Fire	4	54	Mana	115	0	0	30	ArmorDebuff -505/40s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	also blocks stealth
Faerie Fire (Feral)	1	30	Mana	0	0	6	5	U	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	U	free; also Cat Form; magnitude blank at source
Entangling Roots	1	8	Mana	50	1.5	0	30	Root 12s; SpellDamage 20/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	outdoors only
Entangling Roots	2	18	Mana	65	1.5	0	30	Root 15s; SpellDamage 50/15s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	outdoors only
Entangling Roots	3	28	Mana	80	1.5	0	30	Root 18s; SpellDamage 90/18s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	outdoors only
Entangling Roots	4	38	Mana	95	1.5	0	30	Root 21s; SpellDamage 140/21s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	outdoors only
Entangling Roots	5	48	Mana	110	1.5	0	30	Root 24s; SpellDamage 200/24s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	outdoors only
Entangling Roots	6	58	Mana	125	1.5	0	30	Root 27s; SpellDamage 270/27s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	outdoors only
Hibernate	1	18	Mana	90	1.5	0	30	Sleep 20s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	beasts and dragonkin only
Hibernate	2	38	Mana	120	1.5	0	30	Sleep 30s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	beasts and dragonkin only
Hibernate	3	58	Mana	150	1.5	0	30	Sleep 40s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	beasts and dragonkin only
Remove Curse	1	24	Mana	45	0	0	30	Cure 1 curse	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	10% of base mana (449 at 24)
Abolish Poison	1	26	Mana	81	0	0	30	Cure 1 poison	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	16% base mana; 1 more every 2s for 8s
Cure Poison	1	14	Mana	35	0	0	30	Cure 1 poison	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	16% of base mana (219 at 14)
Rebirth	1	20	Mana	301	2	1800	30	Resurrect	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	85% base mana; 400 hp/700 mp
Rebirth	2	30	Mana	522	2	1800	30	Resurrect	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	85% base mana; 750 hp/1200 mp
Rebirth	3	40	Mana	726	2	1800	30	Resurrect	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	85% base mana; 1100 hp/1700 mp
Rebirth	4	50	Mana	904	2	1800	30	Resurrect	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	85% base mana; 1600 hp/2200 mp
Rebirth	5	60	Mana	1057	2	1800	30	Resurrect	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	85% base mana; 2200 hp/2800 mp
Revive	1	12	Mana	137	10	0	30	Resurrect	any	https://wow4ever.quest/en/spellbook/druid	I	75% base mana; out-of-combat rez
Revive	2	24	Mana	337	10	0	30	Resurrect	any	https://wow4ever.quest/en/spellbook/druid	I	75% base mana; out-of-combat rez
Revive	3	36	Mana	562	10	0	30	Resurrect	any	https://wow4ever.quest/en/spellbook/druid	I	75% base mana; out-of-combat rez
Revive	4	48	Mana	764	10	0	30	Resurrect	any	https://wow4ever.quest/en/spellbook/druid	I	75% base mana; out-of-combat rez
Revive	5	60	Mana	933	10	0	30	Resurrect	any	https://wow4ever.quest/en/spellbook/druid	I	75% base mana; out-of-combat rez
Nature's Grasp	1	10	Mana	50	0	60	0	Root 12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	35% chance when struck; outdoors
Nature's Grasp	2	18	Mana	65	0	60	0	Root 15s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	35% chance when struck; outdoors
Nature's Grasp	3	28	Mana	80	0	60	0	Root 18s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	35% chance when struck; outdoors
Nature's Grasp	4	38	Mana	95	0	60	0	Root 21s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	35% chance when struck; outdoors
Nature's Grasp	5	48	Mana	110	0	60	0	Root 24s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	35% chance when struck; outdoors
Nature's Grasp	6	58	Mana	125	0	60	0	Root 27s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	35% chance when struck; outdoors
Wrath	1	1	Mana	20	1.5	0	30	SpellDamage 13-15	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Wrath	2	6	Mana	35	1.7	0	30	SpellDamage 28-32	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Wrath	3	14	Mana	55	2	0	30	SpellDamage 48-56	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Wrath	4	22	Mana	70	2	0	30	SpellDamage 69-79	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	twinhead 1.12: 63-74
Wrath	5	30	Mana	100	2	0	30	SpellDamage 108-122	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Wrath	6	38	Mana	125	2	0	30	SpellDamage 148-166	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Wrath	7	46	Mana	155	2	0	30	SpellDamage 198-220	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Wrath	8	54	Mana	180	2	0	30	SpellDamage 248-276	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Starfire	1	20	Mana	95	3.5	0	30	SpellDamage 95-115	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	resolves earlier Starfire r1 U
Starfire	2	26	Mana	135	3.5	0	30	SpellDamage 146-176	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Starfire	3	34	Mana	180	3.5	0	30	SpellDamage 212-252	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Starfire	4	42	Mana	230	3.5	0	30	SpellDamage 293-347	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Starfire	5	50	Mana	275	3.5	0	30	SpellDamage 378-444	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Starfire	6	58	Mana	315	3.5	0	30	SpellDamage 451-531	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Starfire	7	60	Mana	340	3.5	0	30	SpellDamage 496-584	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Moonfire	1	4	Mana	25	0	0	30	SpellDamage 9-11; SpellDamage 12/9s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Moonfire	2	10	Mana	50	0	0	30	SpellDamage 17-21; SpellDamage 32/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Moonfire	3	16	Mana	75	0	0	30	SpellDamage 30-36; SpellDamage 52/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Moonfire	4	22	Mana	105	0	0	30	SpellDamage 46-54; SpellDamage 80/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	twinhead 1.12: 40-49 direct
Moonfire	5	28	Mana	150	0	0	30	SpellDamage 69-81; SpellDamage 124/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Moonfire	6	34	Mana	190	0	0	30	SpellDamage 91-107; SpellDamage 164/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Moonfire	7	40	Mana	235	0	0	30	SpellDamage 117-137; SpellDamage 212/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Moonfire	8	46	Mana	280	0	0	30	SpellDamage 143-167; SpellDamage 264/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Moonfire	9	52	Mana	325	0	0	30	SpellDamage 172-200; SpellDamage 320/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Moonfire	10	58	Mana	375	0	0	30	SpellDamage 195-227; SpellDamage 384/12s	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	-
Hurricane	1	40	Mana	880	10	60	30	AreaDamage 71	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	71 per sec, 10s channel
Hurricane	2	50	Mana	1180	10	60	30	AreaDamage 101	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	101 per sec, 10s channel
Hurricane	3	60	Mana	1495	10	60	30	AreaDamage 134	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	134 per sec, 10s channel
Soothe Animal	1	22	Mana	50	1.5	0	40	Threat -10	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	aggro radius -10yd, beast <=40
Soothe Animal	2	38	Mana	75	1.5	0	40	Threat -10	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	aggro radius -10yd, beast <=55
Soothe Animal	3	54	Mana	100	1.5	0	40	Threat -10	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	aggro radius -10yd, beast <=70
Barkskin	1	44	Mana	142	0	60	0	ArmorBuff +20%/15s	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	phys DR 20%, not armor; 15% base mana
Teleport: Moonglade	1	10	Mana	120	10	0	0	Teleport	any	https://wowcalc.net/en/classic/wiki/spells/druid	D	to Moonglade
Bear Form	1	10	Mana	82	0	0	0	ApBuff +30; ArmorBuff +180%	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	1.12 values; wowcalc +120AP/+560hp
Dire Bear Form	1	40	Mana	470	0	0	0	ApBuff +120; ArmorBuff +360%	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	1.12 values; wowcalc +180AP/+1240hp
Cat Form	1	20	Mana	195	0	0	0	ApBuff +40	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	1.12 values; wowcalc +120 AP; energy 100
Travel Form	1	30	Mana	123	0	0	0	Speed +40%	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	outdoors only; instant
Aquatic Form	1	16	Mana	52	0	0	0	Speed +50%	any	https://wowcalc.net/en/classic/wiki/spells/druid	I	swim speed; water breathing
Maul	1	10	Rage	15	0	0	5	Weapon 100%+18	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	on next melee attack
Maul	2	18	Rage	15	0	0	5	Weapon 100%+27	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	on next melee attack
Maul	3	26	Rage	15	0	0	5	Weapon 100%+37	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	on next melee attack
Maul	4	34	Rage	15	0	0	5	Weapon 100%+49	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	on next melee attack
Maul	5	42	Rage	15	0	0	5	Weapon 100%+71	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	on next melee attack
Maul	6	50	Rage	15	0	0	5	Weapon 100%+101	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	on next melee attack
Maul	7	58	Rage	15	0	0	5	Weapon 100%+128	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	on next melee attack
Swipe	1	16	Rage	20	0	0	5	AreaDamage 18	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	3 nearby enemies
Swipe	2	24	Rage	20	0	0	5	AreaDamage 25	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	3 nearby enemies
Swipe	3	34	Rage	20	0	0	5	AreaDamage 36	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	3 nearby enemies
Swipe	4	44	Rage	20	0	0	5	AreaDamage 60	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	3 nearby enemies
Swipe	5	54	Rage	20	0	0	5	AreaDamage 83	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	3 nearby enemies
Enrage	1	12	Rage	0	0	60	0	Energize 20	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	20 rage over 10s, -27% base armor
Growl	1	10	Rage	0	0	10	5	Taunt	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	I	wow4ever lists 8s cooldown
Challenging Roar	1	28	Rage	15	0	600	0	AreaTaunt 6s	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	forces attacks 6s; radius U
Demoralizing Roar	1	10	Rage	10	0	0	0	ApBuff -40/30s	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	enemy melee AP debuff
Demoralizing Roar	2	20	Rage	10	0	0	0	ApBuff -60/30s	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	enemy melee AP debuff
Demoralizing Roar	3	32	Rage	10	0	0	0	ApBuff -75/30s	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	enemy melee AP debuff
Demoralizing Roar	4	42	Rage	10	0	0	0	ApBuff -110/30s	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	enemy melee AP debuff
Demoralizing Roar	5	52	Rage	10	0	0	0	ApBuff -138/30s	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	wow4ever "Classic" says 130
Bash	1	14	Rage	10	0	60	5	Stun 2s	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	hard stun, not root
Bash	2	30	Rage	10	0	60	5	Stun 3s	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	hard stun, not root
Bash	3	46	Rage	10	0	60	5	Stun 4s	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	hard stun, not root
Frenzied Regeneration	1	36	Rage	0	0	180	0	Heal 10/rage	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	I	10 rage/sec up to 10s; cd U
Frenzied Regeneration	2	46	Rage	0	0	180	0	Heal 15/rage	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	I	10 rage/sec up to 10s; cd U
Frenzied Regeneration	3	56	Rage	0	0	180	0	Heal 20/rage	BearForm	https://wowcalc.net/en/classic/wiki/spells/druid	I	10 rage/sec up to 10s; cd U
Claw	1	20	Energy	45	0	0	5	Weapon 100%+27	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	awards 1 combo point
Claw	2	28	Energy	45	0	0	5	Weapon 100%+39	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	awards 1 combo point
Claw	3	38	Energy	45	0	0	5	Weapon 100%+57	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	awards 1 combo point
Claw	4	48	Energy	45	0	0	5	Weapon 100%+88	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	awards 1 combo point
Claw	5	58	Energy	45	0	0	5	Weapon 100%+115	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	awards 1 combo point
Shred	1	22	Energy	60	0	0	5	Weapon 225%+54	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	behind target; 1 combo point
Shred	2	30	Energy	60	0	0	5	Weapon 225%+72	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	behind target; 1 combo point
Shred	3	38	Energy	60	0	0	5	Weapon 225%+99	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	behind target; 1 combo point
Shred	4	46	Energy	60	0	0	5	Weapon 225%+144	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	behind target; 1 combo point
Shred	5	54	Energy	60	0	0	5	Weapon 225%+180	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	behind target; 1 combo point
Ravage	1	32	Energy	60	0	0	5	Weapon 350%+147	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	prowling, behind; 1 combo point
Ravage	2	42	Energy	60	0	0	5	Weapon 350%+217	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	prowling, behind; 1 combo point
Ravage	3	50	Energy	60	0	0	5	Weapon 350%+273	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	prowling, behind; 1 combo point
Ravage	4	58	Energy	60	0	0	5	Weapon 350%+343	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	prowling, behind; 1 combo point
Rake	1	24	Energy	40	0	0	5	Weapon 100%+19; Bleed 39/9s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	awards 1 combo point
Rake	2	34	Energy	40	0	0	5	Weapon 100%+28; Bleed 57/9s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	awards 1 combo point
Rake	3	44	Energy	40	0	0	5	Weapon 100%+43; Bleed 75/9s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	awards 1 combo point
Rake	4	54	Energy	40	0	0	5	Weapon 100%+58; Bleed 96/9s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	awards 1 combo point
Rip	1	20	Energy	30	0	0	5	BleedFinisher 42/66/90/114/138	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point, 12s
Rip	2	28	Energy	30	0	0	5	BleedFinisher 66/108/150/192/234	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point, 12s
Rip	3	36	Energy	30	0	0	5	BleedFinisher 90/144/198/252/306	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point, 12s
Rip	4	44	Energy	30	0	0	5	BleedFinisher 138/222/306/390/474	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point, 12s
Rip	5	52	Energy	30	0	0	5	BleedFinisher 192/312/432/552/672	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point, 12s
Rip	6	60	Energy	30	0	0	5	BleedFinisher 270/438/606/774/942	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point, 12s
Ferocious Bite	1	32	Energy	35	0	0	5	Finisher 50-66/86-102/122-138/158-174/194-210	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point; extra energy adds damage
Ferocious Bite	2	40	Energy	35	0	0	5	Finisher 79-103/138-162/197-221/256-280/315-339	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point; extra energy adds damage
Ferocious Bite	3	48	Energy	35	0	0	5	Finisher 122-162/214-254/306-346/398-438/490-530	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point; extra energy adds damage
Ferocious Bite	4	56	Energy	35	0	0	5	Finisher 173-223/301-351/429-479/557-607/685-735	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point; extra energy adds damage
Ferocious Bite	5	60	Energy	35	0	0	5	Finisher 199-259/346-406/493-553/640-700/787-847	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	per combo point; extra energy adds damage
Prowl	1	20	Energy	0	0	10	0	Speed -40%	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	stealth; breaks on damage
Prowl	2	40	Energy	0	0	10	0	Speed -35%	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	stealth; breaks on damage
Prowl	3	60	Energy	0	0	10	0	Speed -30%	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	stealth; breaks on damage
Pounce	1	36	Energy	50	0	0	5	Stun 2s; Bleed 90/18s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	prowling, behind; 1 combo point
Pounce	2	46	Energy	50	0	0	5	Stun 2s; Bleed 120/18s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	prowling, behind; 1 combo point
Pounce	3	56	Energy	50	0	0	5	Stun 2s; Bleed 150/18s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	prowling, behind; 1 combo point
Cower	1	28	Energy	20	0	10	5	Threat -small	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	U	magnitude not in tooltip
Cower	2	40	Energy	20	0	10	5	Threat -medium	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	U	magnitude not in tooltip
Cower	3	52	Energy	20	0	10	5	Threat -large	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	U	magnitude not in tooltip
Tiger's Fury	1	24	Energy	30	0	1	0	ApBuff +10/6s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	buffs damage done, not AP
Tiger's Fury	2	36	Energy	30	0	1	0	ApBuff +20/6s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	buffs damage done, not AP
Tiger's Fury	3	48	Energy	30	0	1	0	ApBuff +30/6s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	buffs damage done, not AP
Tiger's Fury	4	60	Energy	30	0	1	0	ApBuff +40/6s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	buffs damage done, not AP
Dash	1	26	Energy	0	0	300	0	Speed +50%/15s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	does not break prowl
Dash	2	46	Energy	0	0	300	0	Speed +60%/15s	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	does not break prowl
Track Humanoids	1	32	Energy	0	0	0	0	Passive	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	minimap tracking, no cost
Feline Grace	1	40	Energy	0	0	0	0	Passive	CatForm	https://wowcalc.net/en/classic/wiki/spells/druid	D	reduces falling damage, no cost
```

## Contradictions
1. **Direct heal / direct damage, wowcalc (1.15.9 client) vs twinhead-1.12.** wowcalc is ~6% higher on
   the *direct* component only while DoT/HoT components match exactly: Healing Touch r4 376-458 vs
   363-446 (185 mana both); Regrowth r3 255-289 vs 240-275 (+259 both); Wrath r4 69-79 vs 63-74
   (70 mana both); Moonfire r4 46-54 vs 40-49 (+80 both); Entangling Roots r2 50/15s in both.
   Unresolved; the wowcalc range is in the table and the twinhead value is in `notes`.
   Also: `druid.md`'s "Rejuvenation 244 at level 22" is a rank-label artefact — the 135-mana/244 rank
   is level **28** in wowcalc and matches twinhead's spell 2091; level 22 is 105 mana / 180 over 12s.
2. **Form bonuses.** wowcalc: Bear +120 AP / +560 health, Dire Bear +180 AP / +1240 health, Cat +120 AP.
   twinhead-1.12 (in `druid.md`) and wow4ever's "In Classic" line: Bear +30 AP / +20 health, Dire Bear
   +120 AP / +600 health, Cat +40 AP. Table reports the 1.12 values (project target) and flags wowcalc
   in `notes`.
3. **Growl cooldown**: wowcalc 10 s vs wow4ever 8 s. Table uses 10 s (`I`).
4. **Demoralizing Roar**: wowcalc 40/60/75/110/138 vs wow4ever 46/74/95/144/193 (its "In Classic" line
   says 130 for the last rank). Wowcalc reported; the other two are not 1.12-consistent elsewhere.
5. **Faerie Fire (Feral)** is labelled "Talent" by wowcalc yet lists four ranks (levels 30/30/42/54) with
   trainer costs and **no effect text**. Recorded as one rank-1 row with `U`. Do not rely on it.
6. **Revive** is absent from the wowcalc Restoration list it renders; levels/cost/cast/range come from
   wow4ever (5 ranks, 75% base mana, 10 s cast), while its "In Classic" line claims 15% health/mana
   instead of fixed amounts. Only level/cost/cast columns used.
7. wow4ever.quest is a **custom server**: Wrath r4 50 vs 70 mana, Regrowth r3 170 vs 280 mana, HT r4
   190 vs 185 mana, Maul r7 +180 vs +128, Tiger's Fury r4 +60 vs +40. Its magnitudes were not used.

## Missing evidence / Unverified
- **`U` cells in the TSV**: Faerie Fire (Feral) rank-1 armour reduction (blank at source; a 1.12 DB read
  of the feral FF spell ids resolves it); Cower threat magnitudes (tooltips only say
  small/medium/large — needs a 1.12 threat value or emulator dump); Challenging Roar radius (source says
  only "nearby enemies"); Frenzied Regeneration cooldown (wowcalc shows 3 min, no 1.12 cross-check).
- **Derived, not quoted** (`I` rows): every "% of base mana" cost is computed from the druid
  base-mana-by-level table (vanilla-wow-archive/wiki/Base_mana: lvl 10=149, 12=182, 14=219, 16=260,
  20=354, 24=449, 26=509, 30=614, 36=749, 40=854, 44=944, 48=1019, 50=1064, 60=1244). Level-1 (50) and
  level-24 (449) anchors agree with `druid.md`, but the mana numbers for Barkskin / Remove Curse /
  Cure Poison / Abolish Poison / Revive / Rebirth and all five shapeshifts are arithmetic, not a
  client read.
- Tranquility and Hurricane totals (`HoT 485/10s`, `AreaDamage 71`) are tick × duration arithmetic on
  quoted per-tick values, not printed totals.
- 1.12 rage generation is documented only as `R = 15·d/(4·c) + f·s/2 ≤ 15·d/c`
  (wowwiki-archive/wiki/Rage); the level constant `c` was not sourced. "Rage set to 10 on shifting" is
  a TBC/WotLK-era tooltip, so **initial rage on shift is U for 1.12** (powershifting + the Furor talent
  implies base shift rage is 0).
- Energy: 100 cap and 20-per-2s tick regen come from a **retail** page's patch-3.0.2 historical note;
  no 1.12-specific page was read.
- Modelling decisions, not data gaps: `Stun` used instead of `Root` for Bash/Pounce; `ApBuff -X` used for
  Demoralizing Roar (enemy AP debuff); `Passive` used for Track Humanoids / Feline Grace; **Rebirth's
  row above gives flat 400 hp / 700 mp restored, while `Resurrect` takes percentages of the target's
  pool — `Content.rebirth` therefore uses 45 % and 40 %, which are those amounts against the sim's
  level-24 druid pools (893 hp / 1744 mana).**

## Sources
- Kept: wowcalc.net/en/classic/wiki/spells/druid — live Classic Era 1.15.9 per-rank table (level, cost,
  cast, cooldown, range, magnitudes); primary for every row.
- Kept: vanilla-wow-archive/wiki/Base_mana — druid base mana by level (percent-cost arithmetic).
- Kept: vanilla-wow-archive 2007 pages (Thorns, Faerie Fire, Entangling Roots, Regrowth) — independent
  1.12 confirmation of Thorns 3/6/9/12/15/18, FF 175/285/395/505, ER durations and rank→level map.
- Kept: warcraft.wiki.gg/wiki/Energy, /Combo_point, /Rage + wowwiki-archive/wiki/Rage — energy tick rate,
  5-combo-point cap, vanilla rage formula.
- Contradiction evidence only: `.pi/research/level24/druid.md` (twinhead 1.12 tooltips).
- Deprioritised: wow4ever.quest/en/spellbook/druid — custom server; used only for Revive.
- Rejected: classicdb.ch/?spells=-11 (JS-rendered, no body text); wowhead.com/classic (JS-rendered).

## Next steps
1. Read Healing Touch 5188, Regrowth 8939, Rejuvenation 2091, Wrath 5179, Moonfire 8926 and the feral
   Faerie Fire ids on a 1.12-accurate DB to close Contradiction 1 and the one `U` magnitude.
2. If rage economy matters, source the 1.12 rage conversion constant per level and the
   initial-rage-on-shift value from an emulator dump.
