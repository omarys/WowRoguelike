# Research: Warrior (1.12 / Classic Era) baseline rank table, learn level 1–60

Read-only research. No repo file was modified.

## Summary

Every baseline (trainer) Warrior ability a level 20–60 character can train, one row per rank (98 rows), with
learn level, rage cost, cast time, cooldown, range, effect, stance and source. Stances are separate rows.
Talent-only abilities are excluded. Three things changed the answer materially:

1. **Slam's learn level is settled: 30.** `wowcalc.net` (read from the live Classic Era client, build
   1.15.9.70003) shows Slam with only **4** ranks starting at level 30 (+32). The "rank 1 at level 20 (+16)"
   in `wow4ever.quest` is a Classic-plus addition — this resolves the contradiction recorded in
   `.pi/research/level24/warrior.md` and `missing_abilities.md` (W13) in favour of the wiki's level 30.
2. **Berserker Rage is level 32, not 30** (`wowcalc`; `wow4ever` says 30).
3. **Bloodrage is a level-10 trainer ability, not a talent** — confirmed by both `wowcalc` (Protection list,
   level 10, trainer cost 6s) and `wow4ever`.

Magnitudes are taken from `wowcalc` (true 1.12-style **ranges**, e.g. `Revenge 12 to 14`), costs/cast/CD/range
from `wow4ever`, and every divergence between the two is noted per row.

## Evidence basis

- Adopted source (magnitudes, and rank *counts*): `wowcalc.net/en/classic/wiki/spells/warrior` — per-rank
  table from the Classic Era client, with the exact tooltip per rank. This is the only source that gave
  per-rank **ranges** for Revenge and the real rank list for Slam.
- `wow4ever.quest/en/spellbook/warrior` — cost / cast / cooldown / range columns. It is a Classic-plus
  ("WoW Forever") DB; where it flags `Changed`, its magnitudes and some cooldowns are custom-server values
  (Retaliation / Recklessness / Shield Wall at 15 min instead of 30, Slam's extra level-20 rank, 8 s Taunt).
- `classicdb.ch/?spell=<id>` — used to cross-check the shout scales against **1.12** client data
  (Battle Shout 15/35/55/85/130/185/232; Demoralizing Shout -35/-70/-105/-140; Revenge R1 "12 to 15").
  It disagrees with `wowcalc` on those specific spells — see Contradictions.
- `.pi/research/level24/warrior.md` §7 and `.pi/research/level24/missing_abilities.md` W1–W13 were read first
  and reused (Taunt, Shield Block, Revenge, Demo Shout, Charge rage figures agree with `wowcalc`).
- `wowhead.com/classic` was not usable (JS-rendered) and nothing here depends on it.

## Stance gating (recorded; partly interpretation)

| Ability | Stance | Basis |
|---|---|---|
| Charge, Overpower, Retaliation, Mocking Blow | Battle | `warrior.md` §7 states Charge/Overpower; Retaliation/Mocking Blow per Classic convention (`I`) |
| Taunt, Sunder Armor, Revenge, Shield Block, Disarm, Shield Wall | Defensive | `warrior.md` §7 (Taunt, Disarm); others per convention (`I`) |
| Shield Bash | Battle or Defensive | `warrior.md` §7 (direct) |
| Execute | Battle or Berserker | `warrior.md` §7 (direct) |
| Whirlwind, Pummel, Intercept, Berserker Rage, Recklessness | Berserker | convention (`I`) |
| Heroic Strike, Rend, Hamstring, Thunder Clap, Cleave, Slam, the shouts, Bloodrage, Parry, Dual Wield | any | no stance requirement in the tooltip |

`wowcalc` prints the stance modifiers but not the per-ability stance requirement, so the `I` rows remain
interpretation; they are flagged in `confidence` and in §Unverified.

## Table

```tsv
ability	rank	learn_level	resource	cost	cast_s	cd_s	range	effects	stance	source_url	confidence	notes
Battle Stance	1	1	None	-	0	1	0	ThreatDrop -20%	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	stance row; -20% threat, no damage mod
Defensive Stance	1	10	None	-	0	1	0	Damage -10%; DamageTaken -10%; Threat +30%	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	stance row; values from wowcalc + wow4ever
Berserker Stance	1	30	None	-	0	1	0	Damage +3% crit; DamageTaken +10%; ThreatDrop -20%	Berserker	https://wowcalc.net/en/classic/wiki/spells/warrior	D	crit form not in list; -20% threat wow4ever
Parry	1	6	None	-	0	0	0	Passive	any	https://warcraft.wiki.gg/wiki/Warrior_abilities_(Classic)	I	trainer passive, single rank
Dual Wield	1	20	None	-	0	0	0	Passive	any	https://warcraft.wiki.gg/wiki/Warrior_abilities_(Classic)	I	trainer passive, offhand white-hit penalty
Charge	1	4	Rage	-	0	15	25	Energize 9; Stun 1s	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	out of combat only; range 8-25
Charge	2	26	Rage	-	0	15	25	Energize 12; Stun 1s	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	out of combat only
Charge	3	46	Rage	-	0	15	25	Energize 15; Stun 1s	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	out of combat only
Hamstring	1	8	Rage	10	0	0	5	Damage 5; MoveSlow 40%/15s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	movement-slow form not in list
Hamstring	2	32	Rage	10	0	0	5	Damage 18; MoveSlow 45%/15s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	movement-slow form not in list
Hamstring	3	54	Rage	10	0	0	5	Damage 45; MoveSlow 50%/15s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	movement-slow form not in list
Heroic Strike	1	1	Rage	15	0	0	5	Weapon 100%+11	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	next melee attack; high threat
Heroic Strike	2	8	Rage	15	0	0	5	Weapon 100%+21	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	next melee attack
Heroic Strike	3	16	Rage	15	0	0	5	Weapon 100%+32	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	next melee attack
Heroic Strike	4	24	Rage	15	0	0	5	Weapon 100%+44	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	next melee attack
Heroic Strike	5	32	Rage	15	0	0	5	Weapon 100%+58	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	next melee attack
Heroic Strike	6	40	Rage	15	0	0	5	Weapon 100%+80	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	next melee attack
Heroic Strike	7	48	Rage	15	0	0	5	Weapon 100%+111	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	next melee attack
Heroic Strike	8	56	Rage	15	0	0	5	Weapon 100%+138	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	next melee attack
Heroic Strike	9	60	Rage	15	0	0	5	Weapon 100%+157	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	next melee attack
Rend	1	4	Rage	10	0	0	5	DamageOverTime 15/9s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	bleed
Rend	2	10	Rage	10	0	0	5	DamageOverTime 28/12s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	bleed
Rend	3	20	Rage	10	0	0	5	DamageOverTime 45/15s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	bleed
Rend	4	30	Rage	10	0	0	5	DamageOverTime 66/18s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	bleed
Rend	5	40	Rage	10	0	0	5	DamageOverTime 98/21s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	bleed
Rend	6	50	Rage	10	0	0	5	DamageOverTime 126/21s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	bleed
Rend	7	60	Rage	10	0	0	5	DamageOverTime 147/21s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	bleed
Thunder Clap	1	6	Rage	20	0	6	0	Damage 10; AttackSpeedSlow 10%/10s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	4 targets; wowcalc shows 4s CD not 6s
Thunder Clap	2	18	Rage	20	0	6	0	Damage 23; AttackSpeedSlow 10%/14s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	4 targets; CD conflict 4s vs 6s
Thunder Clap	3	28	Rage	20	0	6	0	Damage 37; AttackSpeedSlow 10%/18s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	4 targets; CD conflict 4s vs 6s
Thunder Clap	4	38	Rage	20	0	6	0	Damage 55; AttackSpeedSlow 10%/22s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	4 targets; CD conflict 4s vs 6s
Thunder Clap	5	48	Rage	20	0	6	0	Damage 82; AttackSpeedSlow 10%/26s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	4 targets; CD conflict 4s vs 6s
Thunder Clap	6	58	Rage	20	0	6	0	Damage 103; AttackSpeedSlow 10%/30s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	4 targets; CD conflict 4s vs 6s
Overpower	1	12	Rage	5	0	5	5	Weapon 100%+5	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	only after a dodge; unavoidable
Overpower	2	28	Rage	5	0	5	5	Weapon 100%+15	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	only after a dodge
Overpower	3	44	Rage	5	0	5	5	Weapon 100%+25	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	only after a dodge
Overpower	4	60	Rage	5	0	5	5	Weapon 100%+35	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	only after a dodge
Cleave	1	20	Rage	20	0	0	5	Weapon 100%+5	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	next melee; target plus nearest ally
Cleave	2	30	Rage	20	0	0	5	Weapon 100%+10	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	target plus nearest ally
Cleave	3	40	Rage	20	0	0	5	Weapon 100%+18	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	target plus nearest ally
Cleave	4	50	Rage	20	0	0	5	Weapon 100%+32	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	target plus nearest ally
Cleave	5	60	Rage	20	0	0	5	Weapon 100%+50	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	target plus nearest ally
Execute	1	24	Rage	15	0	0	5	Damage 125; Damage +3/rage	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	target at 20% hp or less
Execute	2	32	Rage	15	0	0	5	Damage 200; Damage +6/rage	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	target at 20% hp or less
Execute	3	40	Rage	15	0	0	5	Damage 325; Damage +9/rage	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	target at 20% hp or less
Execute	4	48	Rage	15	0	0	5	Damage 450; Damage +12/rage	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	target at 20% hp or less
Execute	5	56	Rage	15	0	0	5	Damage 600; Damage +15/rage	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	target at 20% hp or less
Slam	1	30	Rage	15	1.5	0	5	Weapon 100%+32	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	settles conflict: no level-20 rank in Classic
Slam	2	38	Rage	15	1.5	0	5	Weapon 100%+43	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	no cooldown in Classic; wow4ever adds 18s
Slam	3	46	Rage	15	1.5	0	5	Weapon 100%+68	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	interrupts on damage
Slam	4	54	Rage	15	1.5	0	5	Weapon 100%+87	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	interrupts on damage
Mocking Blow	1	16	Rage	10	0	120	5	Damage 22; Taunt	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	moderate threat; forces target 6s
Mocking Blow	2	26	Rage	10	0	120	5	Damage 31; Taunt	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	moderate threat; forces target 6s
Mocking Blow	3	36	Rage	10	0	120	5	Damage 46; Taunt	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	moderate threat; forces target 6s
Mocking Blow	4	46	Rage	10	0	120	5	Damage 71; Taunt	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	moderate threat; forces target 6s
Mocking Blow	5	56	Rage	10	0	120	5	Damage 93; Taunt	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	moderate threat; forces target 6s
Intimidating Shout	1	22	Rage	25	0	180	10	Fear 8s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	fear form not in list; 5 flee, target cowers
Retaliation	1	20	None	-	0	1800	0	Reflect 30attacks/15s	Battle	https://wowcalc.net/en/classic/wiki/spells/warrior	D	melee counter only; wow4ever says 15min CD
Recklessness	1	50	None	-	0	1800	0	Crit U; FearImmunity 15s; DamageTaken +20%/15s	Berserker	https://wowcalc.net/en/classic/wiki/spells/warrior	D	"crits with most attacks"; wow4ever says 15min
Shield Bash	1	12	Rage	10	0	12	5	Damage 6; Interrupt; Silence 6s	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	requires shield; Battle or Defensive stance
Shield Bash	2	32	Rage	10	0	12	5	Damage 18; Interrupt; Silence 6s	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	requires shield
Shield Bash	3	52	Rage	10	0	12	5	Damage 45; Interrupt; Silence 6s	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	requires shield
Shield Block	1	16	Rage	10	0	5	0	Block +75%/5s/1attack	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	Classic 5s/1 attack; wow4ever shows 7s/2
Revenge	1	14	Rage	5	0	5	5	Damage 12-14	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	needs block, dodge or parry; classicdb says 12-15
Revenge	2	24	Rage	5	0	5	5	Damage 18-22	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	needs block, dodge or parry
Revenge	3	34	Rage	5	0	5	5	Damage 25-31	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	needs block, dodge or parry
Revenge	4	44	Rage	5	0	5	5	Damage 43-53	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	needs block, dodge or parry
Revenge	5	54	Rage	5	0	5	5	Damage 64-78	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	needs block, dodge or parry
Revenge	6	60	Rage	5	0	5	5	Damage 81-99	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	needs block, dodge or parry
Sunder Armor	1	10	Rage	15	0	0	5	ArmorDebuff -90/30s	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	stacks to 5; high threat
Sunder Armor	2	22	Rage	15	0	0	5	ArmorDebuff -180/30s	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	stacks to 5; high threat
Sunder Armor	3	34	Rage	15	0	0	5	ArmorDebuff -270/30s	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	stacks to 5; high threat
Sunder Armor	4	46	Rage	15	0	0	5	ArmorDebuff -360/30s	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	stacks to 5; high threat
Sunder Armor	5	58	Rage	15	0	0	5	ArmorDebuff -450/30s	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	stacks to 5; high threat
Taunt	1	10	None	-	0	10	5	Taunt	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	10s CD per wowcalc; wow4ever says 8s
Disarm	1	18	Rage	20	0	60	5	WeaponDisarm 10s	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	disarm form not in list
Battle Shout	1	1	Rage	10	0	0	0	ApBuff +20/120s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	20yd radius; classicdb 1.12 reads +15
Battle Shout	2	12	Rage	10	0	0	0	ApBuff +39/120s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	20yd radius; classicdb 1.12 reads +35
Battle Shout	3	22	Rage	10	0	0	0	ApBuff +59/120s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	20yd radius; classicdb 1.12 reads +55
Battle Shout	4	32	Rage	10	0	0	0	ApBuff +94/120s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	20yd radius; classicdb 1.12 reads +85
Battle Shout	5	42	Rage	10	0	0	0	ApBuff +139/120s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	20yd radius; classicdb 1.12 reads +130
Battle Shout	6	52	Rage	10	0	0	0	ApBuff +193/120s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	20yd radius; classicdb 1.12 reads +185
Battle Shout	7	60	Rage	10	0	0	0	ApBuff +232/120s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	all three sources agree at max rank
Demoralizing Shout	1	14	Rage	10	0	0	0	ApDebuff -45/30s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	10yd radius; classicdb 1.12 reads -35
Demoralizing Shout	2	24	Rage	10	0	0	0	ApDebuff -65/30s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	classicdb rank-2 value not fetched
Demoralizing Shout	3	34	Rage	10	0	0	0	ApDebuff -80/30s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	classicdb 1.12 reads -70
Demoralizing Shout	4	44	Rage	10	0	0	0	ApDebuff -115/30s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	classicdb 1.12 reads -105
Demoralizing Shout	5	54	Rage	10	0	0	0	ApDebuff -146/30s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	classicdb 1.12 reads -140; wow4ever -196
Challenging Shout	1	26	Rage	5	0	600	0	Taunt	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	10yd AoE, forces 6s; 5 rage cost confirmed
Pummel	1	38	Rage	10	0	10	5	Damage 20; Interrupt; Silence 4s	Berserker	https://wowcalc.net/en/classic/wiki/spells/warrior	D	school lockout 4s
Pummel	2	58	Rage	10	0	10	5	Damage 50; Interrupt; Silence 4s	Berserker	https://wowcalc.net/en/classic/wiki/spells/warrior	D	school lockout 4s
Whirlwind	1	36	Rage	25	0	10	0	Weapon 100%	Berserker	https://wowcalc.net/en/classic/wiki/spells/warrior	D	hits up to 4 enemies within 8 yd
Berserker Rage	1	32	None	-	0	30	0	FearImmunity 10s; Energize U	Berserker	https://wowcalc.net/en/classic/wiki/spells/warrior	D	level 32 not 30; extra rage from damage
Shield Wall	1	28	None	-	0	1800	0	DamageTaken -75%/10s	Defensive	https://wowcalc.net/en/classic/wiki/spells/warrior	D	requires shield; wow4ever says 15min, 60%
Bloodrage	1	10	Rage	-	0	60	0	Energize 10; Energize 10/10s	any	https://wowcalc.net/en/classic/wiki/spells/warrior	D	trainer ability, NOT a talent; health cost
Intercept	1	30	Rage	10	0	30	25	Damage 25; Stun 3s	Berserker	https://wowcalc.net/en/classic/wiki/spells/warrior	D	Berserker Stance, in combat
Intercept	2	42	Rage	10	0	30	25	Damage 45; Stun 3s	Berserker	https://wowcalc.net/en/classic/wiki/spells/warrior	D	Berserker Stance, in combat
Intercept	3	52	Rage	10	0	30	25	Damage 65; Stun 3s	Berserker	https://wowcalc.net/en/classic/wiki/spells/warrior	D	Berserker Stance, in combat
```

98 data rows. Every rank with `learn_level` in 1..60 that a source lists is present; no rank was invented.
Talent-only spells found on the same pages and excluded: Sweeping Strikes (30), Piercing Howl (20), Death Wish
(30), Last Stand (20), Concussion Blow (30), Shield Slam (40), Mortal Strike (40), Bloodthirst (40).

## Effects mini-language deviations

Forms not in the supplied list (each also flagged in that row's `notes`): `Passive` (Parry, Dual Wield),
`MoveSlow x%/Ns` (Hamstring), `Fear 8s` (Intimidating Shout), `FearImmunity 10s` / `FearImmunity 15s`,
`WeaponDisarm 10s`, `ApBuff +N/120s` (Battle Shout), `Damage +N/rage` (Execute), `Crit U` (Recklessness),
`Reflect 30attacks/15s` (Retaliation), `DamageTaken -10%` / `DamageTaken +10%` / `DamageTaken -75%/10s`,
`Threat +30%` / `ThreatDrop -20%`, `Energize 10/10s`, `Damage -10%` (Defensive Stance).

Two formatting choices: Revenge uses the tooltip's own range (`Damage 12-14`), and Execute's extra-rage
conversion is a second effect rather than folded into the damage figure.

## Contradictions

1. **Slam learn level — RESOLVED, in favour of level 30.** `wowcalc` (client build 1.15.9.70003) lists Slam as
   **4 ranks: 30 (+32) / 38 (+43) / 46 (+68) / 54 (+87)**, no level-20 rank, no cooldown.
   `wow4ever.quest` lists 5 ranks starting at level 20 (+16) with an 18 s cooldown — a Classic-plus
   modification. The level-30 reading matches `warcraft.wiki.gg/Warrior_abilities_(Classic)`, which is what
   `.pi/research/level24/warrior.md` and `missing_abilities.md` W13 recorded as the contradiction. The table
   now follows level 30; the old contradiction can be marked closed but `wow4ever`'s divergent data should
   stay documented because it is the source that produced the conflict.
2. **Berserker Rage level: 32 (`wowcalc`) vs 30 (`wow4ever`).** Table uses 32 — and that matches the wiki
   reading recorded on disk.
3. **Battle Shout magnitudes differ between the 1.15 client and 1.12 data.** `wowcalc` (1.15.9 client):
   20/39/59/94/139/193/232. `classicdb.ch` (1.12): 15/35/55/85/130/185/232. Both agree only at rank 7.
   Per the adopted guidance the table uses `wowcalc`; a strict-1.12 sim would want the `classicdb` column.
4. **Demoralizing Shout, same pattern.** `wowcalc`: -45/-65/-80/-115/-146. `classicdb` (1.12): -35/-70/-105/
   -140 (rank 2 not fetched). `wow4ever`: -49/-77/-98/-147/-196 for 45 s. Three different scales.
5. **Revenge rank 1 max damage:** `wowcalc` 12-14, `classicdb` "12 to 15", `wow4ever` flat 22. Ranks 2-5 exist
   only on `wowcalc` (18-22 / 25-31 / 43-53 / 64-78) and are used here as the only per-rank source.
6. **Cooldowns that `wow4ever` shortens (Classic-plus):** Retaliation 15 min vs 30 min (`wowcalc`), Shield
   Wall 15 min vs 30 min, Slam 18 s vs none, Taunt 8 s vs 10 s. Table follows `wowcalc`/`classicdb`.
7. **Thunder Clap cooldown: 6 s (`wow4ever`, and the pre-existing disk notes) vs 4 s (`wowcalc`).** Kept 6 s
   because cooldowns were assigned to `wow4ever` by the adopted split, and flagged on every row. Needs one
   more check (classicdb Thunder Clap id) before a sim relies on it.
8. **Shield Block / Shield Wall magnitudes:** `wowcalc` says 75% / 5 s / 1 attack and 75% / 10 s (matching
   `wow4ever`'s own "In Classic" lines); `wow4ever`'s custom scale says 7 s / 2 attacks and 60% / 12 s.
9. **Bloodrage classification:** not a talent — level-10 trainer spell on both `wowcalc` and `wow4ever`.

## Unverified

| Cell | What would resolve it |
|---|---|
| `Berserker Rage` `Energize U` | no tooltip number; needs the client's "extra rage from damage taken" coefficient |
| `Recklessness` `Crit U` — tooltip says "will cause critical hits with most attacks", not +100% | `classicdb` spell 1719 aura value |
| `Thunder Clap` 6 s vs 4 s cooldown | `classicdb` Thunder Clap rank-1 spell id |
| Strict-1.12 Battle Shout / Demoralizing Shout magnitudes | keep the `classicdb` column if the sim targets 1.12 rather than 1.15 |
| Stance requirement for Retaliation, Mocking Blow, Whirlwind, Pummel, Intercept, Berserker Rage, Recklessness, Revenge, Sunder Armor, Shield Block | per-spell classicdb pages print the "Requires … Stance" line; only Charge/Overpower/Shield Bash/Disarm/Execute are source-backed here |
| `Parry` (6) / `Dual Wield` (20) rows | trainer-list confirmation; neither `wowcalc` nor `wow4ever` lists passives |
| `MoveSlow`, `Fear`, `WeaponDisarm` magnitudes in a form the sim can consume | sim-side decision, not a data gap |

Not modelled: talents, custom-server spells (`Spearing Strike`, `Victory Rush`, `Tactical Mastery` — `wow4ever`
marks these `New`), weapon skills, Shoot/Throw.

## Sources

- Kept: `wowcalc.net/en/classic/wiki/spells/warrior` — per-rank table read from the Classic Era client
  (1.15.9.70003): true ranges, real rank counts (settles Slam), trainer costs, stance modifiers.
- Kept: `wow4ever.quest/en/spellbook/warrior` — cost / cast / cooldown / range for every row; its
  "In Classic, its last rank" lines were the tie-breaker for several magnitudes.
- Kept: `classicdb.ch/?spell=6673|5242|6192|11549|11550|11551|1160|11554|11555|11556|6572` — 1.12 client data
  used to expose the 1.12-vs-1.15 shout divergence.
- Kept: `warcraft.wiki.gg/wiki/Warrior_abilities_(Classic)` — learn levels, Parry/Dual Wield, and the
  Slam-at-30 reading that `wowcalc` confirmed.
- Kept/reused: `.pi/research/level24/warrior.md` §7; `.pi/research/level24/missing_abilities.md` W1–W13.
- Rejected: `wowhead.com/classic` (JS-rendered, snippet-only); `vanilla-wow-archive.fandom.com` (retail-era
  text, no rank tables); `foreverdb.net` mirrors (same DB as `wow4ever`).

## Next steps

1. One `classicdb` look at Thunder Clap's rank-1 id to settle 4 s vs 6 s.
2. Per-spell `classicdb` stance lines for the ten `I` rows in the stance table.
3. If the sim targets 1.12 rather than 1.15, re-derive Battle Shout and Demoralizing Shout from `classicdb`.
