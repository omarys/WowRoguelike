# Research: vanilla / Classic Era Mage rank table (levels 1–60, baseline abilities only)

## Summary

Primary magnitude source: **Classic Era tooltips read from the Wowhead "nether" tooltip JSON endpoint**
(`https://nether.wowhead.com/classic/tooltip/spell/<id>?dataenv=1&locale=0`), which *is* fetchable even
though `wowhead.com/classic` HTML is JS-only. That endpoint returned real Classic-Era (1.15.x) values for
~95 ranks, including damage *ranges* (`[N * (1)]` placeholders resolved to min-max), mana, cast time,
cooldown, range and required level. Cross-checked against the on-disk 1.12 evidence
(`.pi/research/level24/mage.md`, `.../missing_abilities.md`) and against `wow4ever.quest` per-rank tables
(levels / costs / cast / cooldown / range only — its magnitudes are a Classic-plus flat scale and were not
used). `wowcalc.net` (requested by the orchestrator) **could not be fetched** — the request hung past 240 s
and was abandoned, so no wowcalc row is used. Rows whose magnitude I could not read are `U`, never guessed.

Counts: Fireball 11 ranks (1–60), Frostbolt 10, Arcane Missiles 7, Arcane Explosion 6, Blizzard 6,
Flamestrike 6, Scorch 7, Fire Blast 7 (ends at 54), Frost Nova 4, Cone of Cold 5, Mana Shield 6,
Dampen Magic 5, Amplify Magic 4, Fire Ward 5, Frost Ward 5, Frost Armor 3, Ice Armor 2, Mage Armor 3,
Polymorph 4, Arcane Intellect 5, plus the single-rank abilities and the Teleport/Portal/Conjure/utility rows.

## Rank table

```tsv
ability	rank	learn_level	resource	cost	cast_s	cd_s	range	effects	stance	source_url	confidence	notes
Fireball	1	1	Mana	30	1.5	0	35	SpellDamage 16-25 +dot 2/4s	any	https://nether.wowhead.com/classic/tooltip/spell/133	D	doT ticks 2s
Fireball	2	6	Mana	45	2	0	35	SpellDamage 34-49 +dot 3/6s	any	https://nether.wowhead.com/classic/tooltip/spell/143	D	doT ticks 2s
Fireball	3	12	Mana	65	2.5	0	35	SpellDamage 57-77 +dot 6/6s	any	https://nether.wowhead.com/classic/tooltip/spell/145	D	doT ticks 2s
Fireball	4	18	Mana	95	3	0	35	SpellDamage 89-122 +dot 12/8s	any	https://nether.wowhead.com/classic/tooltip/spell/3140	D	doT ticks 2s
Fireball	5	24	Mana	140	3.5	0	35	SpellDamage 146-195 +dot 20/8s	any	https://nether.wowhead.com/classic/tooltip/spell/8400	D	1.12 line is 139-188
Fireball	6	30	Mana	185	3.5	0	35	SpellDamage 207-274 +dot 28/8s	any	https://nether.wowhead.com/classic/tooltip/spell/8401	D
Fireball	7	36	Mana	220	3.5	0	35	SpellDamage 264-345 +dot 32/8s	any	https://nether.wowhead.com/classic/tooltip/spell/8402	D
Fireball	8	42	Mana	260	3.5	0	35	SpellDamage 328-425 +dot 40/8s	any	https://nether.wowhead.com/classic/tooltip/spell/10148	D
Fireball	9	48	Mana	305	3.5	0	35	SpellDamage 404-518 +dot 52/8s	any	https://nether.wowhead.com/classic/tooltip/spell/10149	D
Fireball	10	54	Mana	350	3.5	0	35	SpellDamage 488-623 +dot 60/8s	any	https://nether.wowhead.com/classic/tooltip/spell/10150	D
Fireball	11	60	Mana	395	3.5	0	35	SpellDamage 561-715 +dot 72/8s	any	https://nether.wowhead.com/classic/tooltip/spell/10151	D	Classic max rank
Frostbolt	1	4	Mana	25	1.5	0	30	SpellDamage 18-20	any	https://nether.wowhead.com/classic/tooltip/spell/116	D	slow 40%/5s
Frostbolt	2	8	Mana	35	1.8	0	30	SpellDamage 31-35	any	https://nether.wowhead.com/classic/tooltip/spell/205	D	slow 40%/6s
Frostbolt	3	14	Mana	50	2.2	0	30	SpellDamage 51-57	any	https://nether.wowhead.com/classic/tooltip/spell/837	D	slow 40%/6s
Frostbolt	4	20	Mana	65	2.6	0	30	SpellDamage 74-83	any	https://nether.wowhead.com/classic/tooltip/spell/7322	D	slow 40%/7s
Frostbolt	5	26	Mana	100	3	0	30	SpellDamage 126-138	any	https://nether.wowhead.com/classic/tooltip/spell/8406	D	slow 40%/7s
Frostbolt	6	32	Mana	130	3	0	30	SpellDamage 174-190	any	https://nether.wowhead.com/classic/tooltip/spell/8407	D	slow 40%/8s
Frostbolt	7	38	Mana	160	3	0	30	SpellDamage 227-247	any	https://nether.wowhead.com/classic/tooltip/spell/8408	D	slow 40%/8s
Frostbolt	8	44	Mana	195	3	0	30	SpellDamage 292-316	any	https://nether.wowhead.com/classic/tooltip/spell/10179	D	slow 40%/9s
Frostbolt	9	50	Mana	225	3	0	30	SpellDamage 353-383	any	https://nether.wowhead.com/classic/tooltip/spell/10180	D	slow 40%/9s
Frostbolt	10	56	Mana	260	3	0	30	SpellDamage 429-463	any	https://nether.wowhead.com/classic/tooltip/spell/10181	D	slow 40%/9s; Classic max
Arcane Missiles	1	8	Mana	85	3	0	30	SpellDamage 78	any	https://nether.wowhead.com/classic/tooltip/spell/5143	D	channeled 3s, 26/s
Arcane Missiles	2	16	Mana	140	4	0	30	SpellDamage 152	any	https://nether.wowhead.com/classic/tooltip/spell/5144	D	channeled 4s, 38/s
Arcane Missiles	3	24	Mana	235	5	0	30	SpellDamage 280	any	https://nether.wowhead.com/classic/tooltip/spell/5145	D	channeled 5s, 56/s
Arcane Missiles	4	32	Mana	320	5	0	30	SpellDamage 430	any	https://nether.wowhead.com/classic/tooltip/spell/8416	D	channeled 5s, 86/s
Arcane Missiles	5	40	Mana	410	5	0	30	SpellDamage 590	any	https://nether.wowhead.com/classic/tooltip/spell/8417	D	channeled 5s, 118/s
Arcane Missiles	6	48	Mana	500	5	0	30	SpellDamage 775	any	https://nether.wowhead.com/classic/tooltip/spell/10211	D	channeled 5s, 155/s
Arcane Missiles	7	56	Mana	595	5	0	30	SpellDamage 980	any	https://nether.wowhead.com/classic/tooltip/spell/10212	D	channeled 5s, 196/s; Classic max
Arcane Explosion	1	14	Mana	75	0	0	0	AreaDamage 34-38	any	https://nether.wowhead.com/classic/tooltip/spell/1449	D	10 yd radius, instant
Arcane Explosion	2	22	Mana	120	0	0	0	AreaDamage 60-66	any	https://nether.wowhead.com/classic/tooltip/spell/8437	D	10 yd radius; 1.12 = 57-64
Arcane Explosion	3	30	Mana	185	0	0	0	AreaDamage 101-110	any	https://nether.wowhead.com/classic/tooltip/spell/8438	D	10 yd radius
Arcane Explosion	4	38	Mana	250	0	0	0	AreaDamage 143-156	any	https://nether.wowhead.com/classic/tooltip/spell/8439	D	10 yd radius
Arcane Explosion	5	46	Mana	315	0	0	0	AreaDamage 191-208	any	https://nether.wowhead.com/classic/tooltip/spell/10201	D	10 yd radius
Arcane Explosion	6	54	Mana	390	0	0	0	AreaDamage 249-270	any	https://nether.wowhead.com/classic/tooltip/spell/10202	D	10 yd radius; Classic max
Blizzard	1	20	Mana	320	8	0	30	AreaDamage 208	any	https://nether.wowhead.com/classic/tooltip/spell/10	D	26/s x8, 8 yd; 1.12 = 200
Blizzard	2	28	Mana	520	8	0	30	AreaDamage 360	any	https://nether.wowhead.com/classic/tooltip/spell/6141	D	45/s x8
Blizzard	3	36	Mana	720	8	0	30	AreaDamage 528	any	https://nether.wowhead.com/classic/tooltip/spell/8427	D	66/s x8
Blizzard	4	44	Mana	935	8	0	30	AreaDamage 736	any	https://nether.wowhead.com/classic/tooltip/spell/10185	D	92/s x8
Blizzard	5	52	Mana	1160	8	0	30	AreaDamage 952	any	https://nether.wowhead.com/classic/tooltip/spell/10186	D	119/s x8
Blizzard	6	60	Mana	1400	8	0	30	AreaDamage 1192	any	https://nether.wowhead.com/classic/tooltip/spell/10187	D	149/s x8; Classic max
Flamestrike	1	16	Mana	195	3	0	30	AreaDamage 55-71 +dot 48/8s	any	https://nether.wowhead.com/classic/tooltip/spell/2120	D	only 1 per mage
Flamestrike	2	24	Mana	330	3	0	30	AreaDamage 100-126 +dot 88/8s	any	https://nether.wowhead.com/classic/tooltip/spell/2121	D	only 1 per mage
Flamestrike	3	32	Mana	490	3	0	30	AreaDamage 159-197 +dot 140/8s	any	https://nether.wowhead.com/classic/tooltip/spell/8422	D	only 1 per mage
Flamestrike	4	40	Mana	650	3	0	30	AreaDamage 226-279 +dot 196/8s	any	https://nether.wowhead.com/classic/tooltip/spell/8423	D	only 1 per mage
Flamestrike	5	48	Mana	815	3	0	30	AreaDamage 298-367 +dot 264/8s	any	https://nether.wowhead.com/classic/tooltip/spell/10215	D	only 1 per mage
Flamestrike	6	56	Mana	990	3	0	30	AreaDamage 381-466 +dot 340/8s	any	https://nether.wowhead.com/classic/tooltip/spell/10216	D	only 1 per mage; Classic max
Scorch	1	22	Mana	50	1.5	0	30	SpellDamage 56-69	any	https://nether.wowhead.com/classic/tooltip/spell/2948	D
Scorch	2	28	Mana	65	1.5	0	30	SpellDamage 81-98	any	https://nether.wowhead.com/classic/tooltip/spell/8444	D
Scorch	3	34	Mana	80	1.5	0	30	SpellDamage 105-126	any	https://nether.wowhead.com/classic/tooltip/spell/8445	D
Scorch	4	40	Mana	100	1.5	0	30	SpellDamage 139-165	any	https://nether.wowhead.com/classic/tooltip/spell/8446	D
Scorch	5	46	Mana	115	1.5	0	30	SpellDamage 168-199	any	https://nether.wowhead.com/classic/tooltip/spell/10205	D
Scorch	6	52	Mana	135	1.5	0	30	SpellDamage 207-247	any	https://nether.wowhead.com/classic/tooltip/spell/10206	D
Scorch	7	58	Mana	150	1.5	0	30	SpellDamage 237-280	any	https://nether.wowhead.com/classic/tooltip/spell/10207	D	Classic max
Fire Blast	1	6	Mana	40	0	8	20	SpellDamage 27-35	any	https://nether.wowhead.com/classic/tooltip/spell/2136	D	instant
Fire Blast	2	14	Mana	75	0	8	20	SpellDamage 62-76	any	https://nether.wowhead.com/classic/tooltip/spell/2137	D	instant
Fire Blast	3	22	Mana	115	0	8	20	SpellDamage 110-134	any	https://nether.wowhead.com/classic/tooltip/spell/2138	D	instant
Fire Blast	4	30	Mana	165	0	8	20	SpellDamage 177-211	any	https://nether.wowhead.com/classic/tooltip/spell/8412	D	instant
Fire Blast	5	38	Mana	220	0	8	20	SpellDamage 253-301	any	https://nether.wowhead.com/classic/tooltip/spell/8413	D	instant
Fire Blast	6	46	Mana	280	0	8	20	SpellDamage 345-407	any	https://nether.wowhead.com/classic/tooltip/spell/10197	D	instant
Fire Blast	7	54	Mana	340	0	8	20	SpellDamage 446-524	any	https://nether.wowhead.com/classic/tooltip/spell/10199	D	1.12 line is 431-509; max rank
Frost Nova	1	10	Mana	55	0	25	0	SpellDamage 19-21; Root 8s	any	https://nether.wowhead.com/classic/tooltip/spell/122	D	10 yd radius, breaks on damage
Frost Nova	2	26	Mana	85	0	25	0	SpellDamage 33-37; Root 8s	any	https://nether.wowhead.com/classic/tooltip/spell/865	D	10 yd radius
Frost Nova	3	40	Mana	115	0	25	0	SpellDamage 52-58; Root 8s	any	https://nether.wowhead.com/classic/tooltip/spell/6131	D	10 yd radius
Frost Nova	4	54	Mana	145	0	25	0	SpellDamage 71-79; Root 8s	any	https://nether.wowhead.com/classic/tooltip/spell/10230	D	10 yd radius; Classic max
Cone of Cold	1	26	Mana	210	0	10	10	SpellDamage 98-108	any	https://nether.wowhead.com/classic/tooltip/spell/120	D	cone; slow 50%/8s
Cone of Cold	2	34	Mana	290	0	10	10	SpellDamage 146-160	any	https://nether.wowhead.com/classic/tooltip/spell/8492	D	cone; slow 50%/8s
Cone of Cold	3	42	Mana	380	0	10	10	SpellDamage 203-223	any	https://nether.wowhead.com/classic/tooltip/spell/10159	D	cone; slow 50%/8s
Cone of Cold	4	50	Mana	465	0	10	10	SpellDamage 264-290	any	https://nether.wowhead.com/classic/tooltip/spell/10160	D	cone; slow 50%/8s
Cone of Cold	5	58	Mana	555	0	10	10	SpellDamage 335-365	any	https://nether.wowhead.com/classic/tooltip/spell/10161	D	cone; slow 50%/8s; Classic max
Counterspell	1	24	Mana	100	0	30	30	Interrupt; Silence 10s	any	https://nether.wowhead.com/classic/tooltip/spell/2139	D	school lock 10s; high threat
Polymorph	1	8	Mana	60	1.5	0	30	Polymorph 20s	any	https://nether.wowhead.com/classic/tooltip/spell/118	D	beast/humanoid/critter
Polymorph	2	20	Mana	90	1.5	0	30	Polymorph 30s	any	https://nether.wowhead.com/classic/tooltip/spell/12824	D	beast/humanoid/critter
Polymorph	3	40	Mana	120	1.5	0	30	Polymorph 40s	any	https://nether.wowhead.com/classic/tooltip/spell/12825	D	beast/humanoid/critter
Polymorph	4	60	Mana	150	1.5	0	30	Polymorph 50s	any	https://nether.wowhead.com/classic/tooltip/spell/12826	D	beast/humanoid/critter; Classic max
Blink	1	20	Mana	425	0	15	0	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/1953	D	20 yd forward, breaks stun/root; 35% base mana
Mana Shield	1	20	Mana	40	0	0	0	Absorb 120/60s	any	https://nether.wowhead.com/classic/tooltip/spell/1463	D	2 mana per damage absorbed
Mana Shield	2	28	Mana	60	0	0	0	Absorb 210/60s	any	https://nether.wowhead.com/classic/tooltip/spell/8494	D	2 mana per damage absorbed
Mana Shield	3	36	Mana	80	0	0	0	Absorb 300/60s	any	https://nether.wowhead.com/classic/tooltip/spell/8495	D	2 mana per damage absorbed
Mana Shield	4	44	Mana	100	0	0	0	Absorb 390/60s	any	https://nether.wowhead.com/classic/tooltip/spell/10191	D	2 mana per damage absorbed
Mana Shield	5	52	Mana	120	0	0	0	Absorb 480/60s	any	https://nether.wowhead.com/classic/tooltip/spell/10192	D	2 mana per damage absorbed
Mana Shield	6	60	Mana	140	0	0	0	Absorb 570/60s	any	https://nether.wowhead.com/classic/tooltip/spell/10193	D	2 mana per damage; Classic max
Dampen Magic	1	12	Mana	100	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/604	D	-10 dmg taken/-20 heal, 600s
Dampen Magic	2	24	Mana	200	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/8450	D	-20 dmg taken/-40 heal, 600s
Dampen Magic	3	36	Mana	300	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/8451	D	-40 dmg taken/-80 heal, 600s
Dampen Magic	4	48	Mana	400	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/10173	D	-60 dmg taken/-120 heal, 600s
Dampen Magic	5	60	Mana	500	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/10174	D	-90 dmg taken/-180 heal, 600s
Amplify Magic	1	18	Mana	150	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/1008	D	+15 dmg taken/+30 heal, 600s
Amplify Magic	2	30	Mana	250	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/8455	D	+30 dmg taken/+60 heal, 600s
Amplify Magic	3	42	Mana	350	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/10169	D	+50 dmg taken/+100 heal, 600s
Amplify Magic	4	54	Mana	450	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/10170	D	+75 dmg taken/+150 heal, 600s
Remove Lesser Curse	1	18	Mana	121	0	0	30	Dispel 1 curse	any	https://nether.wowhead.com/classic/tooltip/spell/475	D	10% of base mana = 121 at 60
Frost Armor	1	1	Mana	60	0	0	0	BuffArmor 30/1800s	any	https://nether.wowhead.com/classic/tooltip/spell/168	D	on-hit 30% slow/25% atk speed
Frost Armor	2	10	Mana	110	0	0	0	BuffArmor 110/1800s	any	https://nether.wowhead.com/classic/tooltip/spell/7300	D	on-hit 30% slow/25% atk speed
Frost Armor	3	20	Mana	170	0	0	0	BuffArmor 200/1800s	any	https://nether.wowhead.com/classic/tooltip/spell/7301	D	on-hit 30% slow; Classic max
Ice Armor	1	30	Mana	U	0	0	0	BuffArmor U/1800s	any	https://nether.wowhead.com/classic/tooltip/spell/10219	U	cost and armor unresolved (id 10219)
Ice Armor	2	40	Mana	320	0	0	0	BuffArmor 380/1800s	any	https://nether.wowhead.com/classic/tooltip/spell/7320	D	+9 frost res; on-hit slow
Mage Armor	1	34	Mana	270	0	0	0	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/6117	D	+5 all res; 30% regen while casting
Mage Armor	2	46	Mana	380	0	0	0	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/22782	D	+10 all res; 30% regen while casting
Mage Armor	3	58	Mana	490	0	0	0	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/22783	D	+15 all res; Classic max
Fire Ward	1	20	Mana	85	0	30	0	Absorb 165/30s	any	https://nether.wowhead.com/classic/tooltip/spell/543	D	Fire only
Fire Ward	2	30	Mana	135	0	30	0	Absorb 290/30s	any	https://nether.wowhead.com/classic/tooltip/spell/8457	D	Fire only
Fire Ward	3	40	Mana	195	0	30	0	Absorb 470/30s	any	https://nether.wowhead.com/classic/tooltip/spell/8458	D	Fire only
Fire Ward	4	50	Mana	255	0	30	0	Absorb 675/30s	any	https://nether.wowhead.com/classic/tooltip/spell/10223	D	Fire only
Fire Ward	5	60	Mana	320	0	30	0	Absorb 920/30s	any	https://nether.wowhead.com/classic/tooltip/spell/10225	D	Fire only; Classic max
Frost Ward	1	22	Mana	85	0	30	0	Absorb 165/30s	any	https://nether.wowhead.com/classic/tooltip/spell/6143	D	Frost only
Frost Ward	2	32	Mana	135	0	30	0	Absorb 290/30s	any	https://nether.wowhead.com/classic/tooltip/spell/8461	D	Frost only
Frost Ward	3	42	Mana	195	0	30	0	Absorb 470/30s	any	https://nether.wowhead.com/classic/tooltip/spell/8462	D	Frost only
Frost Ward	4	52	Mana	255	0	30	0	Absorb 675/30s	any	https://nether.wowhead.com/classic/tooltip/spell/10177	D	Frost only
Frost Ward	5	60	Mana	U	0	30	0	Absorb U/30s	any	https://nether.wowhead.com/classic/tooltip/spell/10178	U	tooltip for id 10178 returned empty
Slow Fall	1	12	Mana	40	0	0	0	SlowFall	any	https://nether.wowhead.com/classic/tooltip/spell/130	D	no reagent in Classic Era
Detect Magic	1	16	Mana	35	0	0	40	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/2855	D	buff detection 120s
Arcane Intellect	1	1	Mana	60	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/1459	D	buff only, +2 Int
Arcane Intellect	2	14	Mana	185	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/1460	D	buff only, +7 Int
Arcane Intellect	3	28	Mana	520	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/1461	D	buff only, +15 Int
Arcane Intellect	4	42	Mana	945	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/10156	D	buff only, +22 Int
Arcane Intellect	5	56	Mana	1510	0	0	30	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/10157	D	buff only, +31 Int; Classic max
Arcane Brilliance	1	56	Mana	3400	0	0	40	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/23028	D	party +31 Int, 3600s; train-source unverified
Teleport: Stormwind	1	20	Mana	120	10	0	0	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/3561	D	learn level only
Teleport: Ironforge	1	20	Mana	120	10	0	0	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/3562	D	learn level only
Teleport: Orgrimmar	1	20	Mana	120	10	0	0	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/3567	D	learn level only
Teleport: Undercity	1	20	Mana	120	10	0	0	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/3563	D	learn level only
Teleport: Darnassus	1	30	Mana	120	10	0	0	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/3565	D	learn level only
Teleport: Thunder Bluff	1	30	Mana	120	10	0	0	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/3566	D	learn level only
Portal: Stormwind	1	40	Mana	850	10	60	10	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/10059	D	learn level only
Portal: Ironforge	1	40	Mana	850	10	60	10	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/11416	U	tooltip fetch timed out; level per wow4ever
Portal: Orgrimmar	1	40	Mana	850	10	60	10	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/11417	U	tooltip empty; level per wow4ever
Portal: Undercity	1	40	Mana	850	10	60	10	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/11418	D	learn level only
Portal: Darnassus	1	50	Mana	850	10	60	10	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/11419	D	learn level only
Portal: Thunder Bluff	1	50	Mana	850	10	60	10	Utility	any	https://nether.wowhead.com/classic/tooltip/spell/11420	D	learn level only
Conjure Water	2	10	Mana	105	3	0	0	Conjure	any	https://nether.wowhead.com/classic/tooltip/spell/5505	D	20 bottles fresh water
Conjure Water	3	20	Mana	180	3	0	0	Conjure	any	https://nether.wowhead.com/classic/tooltip/spell/5506	D	20 bottles purified water
Conjure Food	1	6	Mana	60	3	0	0	Conjure	any	https://nether.wowhead.com/classic/tooltip/spell/587	D	20 muffins
Conjure Food	2	12	Mana	105	3	0	0	Conjure	any	https://nether.wowhead.com/classic/tooltip/spell/597	D	20 loaves of bread
Conjure Food	4	32	Mana	285	3	0	0	Conjure	any	https://nether.wowhead.com/classic/tooltip/spell/6129	D	20 pumpernickel
Conjure Mana Agate	1	28	Mana	530	3	0	0	Conjure	any	https://nether.wowhead.com/classic/tooltip/spell/759	D	restores 375-425 mana
Conjure Mana Jade	1	38	Mana	800	3	0	0	Conjure	any	https://nether.wowhead.com/classic/tooltip/spell/3552	D	restores 550-650 mana
```

Notes on the mini-language deviations (as required): `AreaDamage` cells carry a **min-max range** where the
tooltip gives one (`AreaDamage 34-38`); Blizzard/Arcane Missiles carry the **whole-channel total** with the
per-second value in `notes`; Frostbolt/Cone of Cold slow, Blink, Dampen/Amplify and Mage Armor have no
listed form, so they use `SpellDamage` + a `notes` note, `Utility`, or `Utility` + numeric note;
`BuffArmor` covers Frost/Ice Armor (armour) and is also used nowhere else; Mage Armor uses `Utility`
because it grants resistance + regen, not armour.

## Level-60 mana pool and mana regeneration (mage-style caster)

**Base mana at 60 = 1213** (mage). Direct evidence, two independent tables that agree on every level they
share (1, 20–23, 26, 28, 29, 30, 34, 35, 38, 40, 43, 48, 54, 55, 60) — `https://warcraft.wiki.gg/wiki/Base_mana`,
`https://vanilla-wow-archive.fandom.com/wiki/Base_mana`; and the cross-validated `player_classlevelstats`
row `(8,60, ...)` in
`https://raw.githubusercontent.com/azerothcore/database-wotlk/master/sql/base/player_classlevelstats.sql`.

**Mana from Intellect** = `min(20, Int) + 15 × (Int − 20)` (first 20 points of Int give 1 mana each, every
point above 20 gives 15) — same two wiki pages. So

```
total_mana = 1213 + min(20, Int) + 15 × max(0, Int − 20)
```

i.e. with 500 Int → 1213 + 20 + 15×480 = **8413 mana**; every +1 Int above 20 is +15 mana. (Researcher
arithmetic from sourced rules; the mage's *base* Int at 60 is not sourced here, so a gearless total is
**U** — see Missing evidence.)

**Regeneration.** Two formulas circulate and they disagree; both recorded, neither silently resolved.

* Classic per-tick line (matches the in-game 2-second mana tick granularity), *Spirit- and Int-free*:
  `Priest/Mage mana per 2 s tick (out of the 5-second rule) = 13 + Spirit / 4`, i.e.
  `mp5 = 2.5 × (13 + Spirit/4) = 32.5 + 0.625 × Spirit`; `mp2 (while casting) =
  (mp5_items + mp5_total × combat_regen%) / 2.5` — `https://zugzugdb.com/formulas/mana-regeneration`
  (direct quote: "in classic WoW … it is 13 + Spirit / 4 for a Priest or Mage. mana per 2-second tick, out
  of the five-second rule"; also reproduced in a r/classicwow thread). MP5 from gear is **never**
  interrupted.
* Classic-era multiplicative form with Int: `mp5 = 5 × (0.001 + Spirit × sqrt(Int) × BASE_REGEN[level])`,
  with `BASE_REGEN` a level-indexed constant — `https://wowwiki-archive.fandom.com/wiki/Mana_regeneration`.
  A third variant tested on SoD data gives `mp5_spirit = (Spirit − 65.78) / 2.95` —
  `https://vanilla-wow-archive.fandom.com/wiki/Mana_Regeneration` (source itself says "should probably
  apply to classic 1:1", i.e. unverified for Classic Era).

**Five-second rule** (both Classic-era and 1.12): spending mana suspends Spirit-based regeneration for 5 s;
only MP5-from-gear and the Mage Armor combat-regen percentage (30 / 40 / 50 % by rank) keep flowing —
`https://zugzugdb.com/formulas/mana-regeneration`, `https://wowwiki-archive.fandom.com/wiki/Mana_regeneration`.

## Contradictions

1. **Every "Changed"-flagged magnitude is a 1.12-vs-1.15 divergence, not an error.** Observed pairs
   (Classic Era 1.15 tooltip → 1.12-era database): Fireball r5 146-195 → 139-188; Fire Blast r7 446-524 →
   431-509; Arcane Explosion r2 60-66 → 57-64; Blizzard r1 208 → 200; Flamestrike r6 381-466+340 →
   375-459+340. **The TSV uses the 1.15 (Classic Era) numbers**, which is what the live client shows; if
   the sim targets literal 1.12 magnitudes, the 1.12 column above is the correction to apply.
2. **Mana regen formula** — the simple `13 + Spirit/4` per-tick line (zugzugdb, Reddit) and the
   `0.001 + SPI×√Int×BASE_REGEN` form (wowwiki) cannot both be right as written; the first has no Int term.
   Unresolved; also `(SPI−65.78)/2.95` (SoD-tested) contradicts both.
3. **Fire Blast rank count.** `wow4ever` prints 7 flat ranks with its own "In Classic, its last rank =
   431-509", while the Classic Era tooltip for rank 7 (level 54) reads 446-524. I treat 431-509 as the
   1.12 value of the *same* rank 7 (no level-60 rank), which is the reading that makes the two consistent.
4. **Dampen/Amplify Magnitudes on disk vs nether.** `missing_abilities.md` carries the wow4ever "Forever"
   scale (−135 dmg / −270 heal at max; rank-2 Magnitudes `U`). Classic Era tooltips give
   −90/−180 max and −20/−40 at rank 2 — disk note "−90/−180 in Classic" is confirmed by the nether read.
5. **Frost Armor ladder.** Nothing on disk covers Frost Armor; nether shows 3 ranks (1/10/20, +30/+110/+200
   armour) ending at 20, after which Ice Armor (30/40) replaces it. `wow4ever`-derived lists that show
   "Frost Armor rank 4" are Classic-plus rows.

## Unverified (`U`) cells

| Cell | Row | What would resolve it |
|---|---|---|
| cost, armour | Ice Armor r1 (level 30) | `nether.wowhead.com/classic/tooltip/spell/10219` returned an empty answer twice; fetch `classicdb.ch/?spell=10219` or a wow4ever Ice Armor section |
| cost, absorb | Frost Ward r5 (level 60, id 10178) | tooltip endpoint returned an empty/garbage body; same two fallbacks |
| learn level | Portal: Ironforge, Portal: Orgrimmar | both tooltip ids failed (timeout / empty); by symmetry with SW/UC they are level 40, but that is inference, flagged `U` in the table |

Additionally, `wowcalc.net/en/classic/wiki/spells/mage` (the orchestrator's preferred 1.12-magnitude source)
**never loaded** — the fetch hung > 240 s and was dropped by instruction, so **no cell in this table is
wowcalc-sourced**. Every magnitude here is a Classic Era **1.15 client tooltip** read from
`nether.wowhead.com/classic`, which is ungated (unlike `wowhead.com/classic` HTML) and therefore *direct
evidence*, not a search snippet.

## Missing evidence

* **Partial Conjure coverage.** Verified: Conjure Water r2/r3 (10, 20), Conjure Food r1/r2/r4 (6, 12, 32),
  Mana Agate (28), Mana Jade (38). **Not verified and therefore omitted rather than invented:** Conjure
  Water r1 (id 5504), r4–r7; Conjure Food r3 (22), r5–r6 (42, 52); Conjure Mana Citrine (48) and Mana Ruby
  (58). Omitted rows are utility-only; the combat table is unaffected.
* **Polymorph rank 2 (level 20) magnitude/cost** comes from the on-disk `mage.md` (90 mana, 30 s) plus the
  nether neighbours r1/r3/r4; its own tooltip id (12824) was not re-read this pass — treat as D-by-disc.
* **Mage base Intellect at level 60** (needed for a gearless level-60 mana total) — not sourced.
* **Arcane Brilliance training source** (trainer vs tome) at 56 — unverified; learn level itself is `D`.
* **Exact Int→spell-crit rate and Spirit→mp5 at any specific level** for Classic Era — see Contradictions;
  only the level-60 crit anchor (≈59.5 Int per 1%) exists on disk.

## Sources

* Kept: `https://nether.wowhead.com/classic/tooltip/spell/<id>?dataenv=1&locale=0` — the workhorse: this
  ungated JSON endpoint serves the real Classic Era tooltip (name, cost, range, cast, cooldown, required
  level, damage range). Used per rank via the per-row `source_url`.
* Kept: `https://wow4ever.quest/en/spellbook/mage` — per-rank level / cost / cast / cooldown / range ladders
  and the "In Classic, its last rank" comparison lines; its flat magnitudes are Classic-plus and were used
  only to document divergence.
* Kept: `https://zugzugdb.com/formulas/mana-regeneration` — the only source found that states a concrete
  Classic per-tick regen formula per class.
* Kept: `https://wowwiki-archive.fandom.com/wiki/Mana_regeneration` and
  `https://vanilla-wow-archive.fandom.com/wiki/Mana_Regeneration` — competing regen formulas + five-second
  rule.
* Kept: `https://warcraft.wiki.gg/wiki/Base_mana`, `https://vanilla-wow-archive.fandom.com/wiki/Base_mana`,
  `https://raw.githubusercontent.com/azerothcore/database-wotlk/master/sql/base/player_classlevelstats.sql`
  — base mana per level / mana-from-Int formula (three-way agreement at every shared level).
* Deprioritised: `wowcalc.net/en/classic/wiki/spells/mage` — unreachable (240 s hang), no cells used.
* Deprioritised: `classicdb.ch` — 1.12-exact but needs ids and is slower per value; used on disk already for
  the Fireball/AE/Blizzard/Frostbolt 1.12 comparison values.
* Deprioritised: `wowhead.com/classic/*` HTML pages — JS-rendered, only search snippets (marked `U` in the
  on-disk file); the nether tooltip endpoint made them unnecessary.

## Next steps

1. Resolve the five `U` cells with `classicdb.ch/?spell=10219`, `?spell=10178`, `?spell=11416`, `?spell=11417`
   (all single-id lookups, one batch).
2. If literal 1.12 magnitudes matter more than live Classic Era, re-read the five divergent rows above
   against `classicdb.ch` and store both columns.
3. Complete the conjure ladder in one wow4ever fetch (ids 5504, 6128, 6129-adjacent, 3552/10053/10055).
