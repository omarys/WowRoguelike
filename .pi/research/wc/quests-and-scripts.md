# Wailing Caverns — quests and scripted encounters (vanilla 1.12 / Classic Era)

Scope: the pre-Cataclysm instance (Classic Era uses 1.12 data; everything below was removed in 4.0.3a).
Evidence tags: **D** = a fetched page states it · **I** = derived by me · **U** = unverified.

Primary sources: `https://warcraft.wiki.gg/wiki/Wailing_Caverns`, `.../Muyoh`, `.../Naralex`, `.../Mutanus_the_Devourer`,
`.../Verdan_the_Everliving`, `.../Kresh`, `.../Deviate_Faerie_Dragon`, plus per-quest pages mirrored at
`https://classicdb.ch/?quest=<id>` (faction "Side:" field) and `https://warcraft.wiki.gg/index.php?title=X&action=raw`.
Quest IDs below are the Classic Era IDs (same on wowhead.com/classic/quest=<id>).

---

## Part A — Quests

### A0. Chain prelude, picked up outside the instance (Horde only)

| Quest | id | Lvl / min | Giver (location) | Objective | Turn in | Reward | Tag |
|---|---|---|---|---|---|---|---|
| Altered Beings | 877 | 13 / 9 | Tonga Runetotem, Crossroads | Bring 8 Altered Snapjaw shells | Tonga Runetotem | XP | U |
| Hamuul Runetotem | 1489 | 16 / 10 | Tonga Runetotem, Crossroads | "Speak with Hamuul Runetotem" | Archdruid Hamuul Runetotem, Elder Rise, Thunder Bluff | 855 XP, +25 Thunder Bluff | D |
| Nara Wildmane | 1490 | 16 / 12 | Archdruid Hamuul Runetotem, Elder Rise | "Speak with Nara Wildmane" | Nara Wildmane, Elder Rise | 120 XP, +10 Thunder Bluff | D |
| Raptor Horns | 865 | 18 / 13 | Mebok Mizzyrix, Ratchet | 5 Intact Raptor Horn from Sunscale Scytheclaws | Mebok Mizzyrix | 5× Raptor Punch, Barkeeper's Cloak | D |

These four only unlock the real dungeon quests (`Leaders of the Fang`, `Smart Drinks`). A sim that starts at the
instance portal can treat them as pre-completed. Sources: `.../Hamuul_Runetotem_(quest)`,
`.../Nara_Wildmane_(quest)`, `.../Raptor_Horns`, `classicdb.ch/?quest=1489|1490|865`.

### A1. Quests that send you INTO the dungeon (turn in outside or at the portal)

| Quest | id | Lvl / min | Faction | Giver (location) | Exact objective | Turn in | Notable reward | Tag |
|---|---|---|---|---|---|---|---|---|
| Leaders of the Fang | 914 | 22 / 10 | **Horde only** | Nara Wildmane, Elder Rise, Thunder Bluff | Bring the **4 dream gems**: Gem of Cobrahn, Gem of Anacondra, Gem of Pythas, Gem of Serpentis — i.e. kill all four Fanglords and loot each | Nara Wildmane | choose **Crescent Staff** or **Wingblade**; 2,600 XP | D |
| Serpentbloom | 962 | 18 / 14 | **Horde only** | Apothecary Zamah, Pools of Vision, Thunder Bluff | Collect **10 Serpentbloom** (herb nodes inside the instance; looting one spawns a Serpentbloom Snake) | Apothecary Zamah | **Apothecary Gloves**, 20s, +250 Undercity | D |
| Deviate Hides | 1486 | 17 / 13 | Both (neutral) | **Nalpak**, right-eye-socket cave *above* the portal | Collect **20 Deviate Hides** (drops from deviate beasts; skinning the corpse yields extra) | Nalpak | choose **Slick Deviate Leggings** or **Deviate Hide Pack** (10-slot); 18s, 1,600 XP | D |
| Deviate Eradication | 1487 | 21 / 15 | Both (neutral) | **Ebru**, right-eye-socket cave above the portal | Kill **7 Deviate Ravagers + 7 Deviate Vipers + 7 Deviate Shamblers + 7 Deviate Dreadfangs** | Ebru | choose **Pattern: Deviate Scale Belt**, **Sizzle Stick**, **Dagmire Gauntlets**; 25s, 2,500 XP | D |
| Trouble at the Docks | 959 | 18 / 14 | Both | Crane Operator Bigglefuzz, Ratchet | Retrieve **99-Year-Old Port** from **Mad Magglish** (stealthed goblin in the caves *before* the portal, 3 possible spawn points) | Bigglefuzz | 10s, 1,350 XP, +250 Ratchet | D |
| Smart Drinks | 1491 | 18 / 13 | Both | Mebok Mizzyrix, Ratchet | Bring **6 Wailing Essence** (100% drop from Evolving / Devouring / Nightmare Ectoplasm) | Mebok Mizzyrix | 10s, 1,350 XP, +500 Ratchet (requires Raptor Horns) | D |

Nalpak and Ebru are **not** inside the instance: they stand in the cave in the skull's right eye socket, reached by
climbing the mountain behind the portal and dropping onto the ledge. A sim that starts at the portal must grant
their quests for free or model the ledge walk. Sources: `.../Deviate_Hides_(Classic)`, `.../Deviate_Eradication`,
`.../Trouble_at_the_Docks`, `.../Smart_Drinks`, `.../Serpentbloom_(quest)`, `.../Leaders_of_the_Fang`;
faction per `classicdb.ch/?quest=914|962|1486|1487|959|1491` ("Side:").

### A2. Quests started by a dungeon drop

| Quest | id | Lvl / min | Faction | Start item | Giver | Objective | Turn in | Reward | Tag |
|---|---|---|---|---|---|---|---|---|---|
| The Glowing Shard | 6981 | 26 / 15 | Both | **Glowing Shard** (item 10441, BOP, unique, ilvl 15) drops from **Mutanus the Devourer** | the item itself | "Travel to Ratchet… deliver the shard as you are directed" → talk to **Sputtervalve** (Ratchet, no `?` marker, gossip topic) → **Falla Sagewind** | Falla Sagewind, camp on the mountain above the instance | 2,650 XP only | D |
| In Nightmares (Horde) | 3369 | 25 / 15 | **Horde only** | — | Falla Sagewind | Deliver the **Nightmare Shard** to **Arch Druid Hamuul Runetotem**, Elder Rise, Thunder Bluff | Hamuul Runetotem | choose **Talbar Mantle** or **Quagmire Galoshes**; 2,000 XP, +250 Thunder Bluff | D |
| In Nightmares (Alliance) | 3370 | 25 / 15 | **Alliance only** | — | Falla Sagewind | Deliver the **Nightmare Shard** to **Mathrengyl Bearwalker**, Cenarion Enclave, Darnassus | Mathrengyl Bearwalker | choose **Talbar Mantle** or **Quagmire Galoshes** | D |

Both `In Nightmares` versions share quest name and start; the fork is by faction (`other-faction` on each page).
The Glowing Shard is the only item that leaves the instance and the only proof of a kill, so it is the natural
"loot the last boss" step for a sim. Sources: `.../The_Glowing_Shard`, `.../Glowing_Shard`,
`.../In_Nightmares_(Horde)`, `.../In_Nightmares_(Alliance)`.

### A3. Names that are NOT real — do not model these

| Claimed quest | Verdict | Evidence |
|---|---|---|
| "Naralex and the Druids of the Fang" | **Does not exist** in vanilla/Classic. `wiki/Naralex_and_the_Druids_of_the_Fang` is a redlink (0-byte `action=raw`), and `intitle:Naralex` returns only Naralex, Muyoh, Disciples of Naralex, Disciples of Naralex (quest, retail), Champion: Naralex. The vanilla "Naralex + Fang" content is carried by `Nara Wildmane` → `Leaders of the Fang` (Horde) and by Muyoh's dialogue (both factions). | D |
| "Disc of Naralex" | **Does not exist** — no such item, quest, or page. Phrase search for `"Disc of Naralex"` returns 0 hits on warcraft.wiki.gg; `intitle:Disc` (86 hits) lists no Naralex disc. Nearest real items are **Scepter of Naralex** (156997) and **Cudgel of Naralex** (156998), both rewards of *The Nightmare Scar* added in 7.3.5 — retail only. | D |
| "What's Haunting Witch Hill?" | **Real quest, wrong dungeon.** id 27188, Dustwallow Marsh, giver Mordant Grimsby at Swamplight Manor, chain `The Witch's Bane` → `Cleansing Witch Hill`; added in patch 2.3.0. Nothing to do with Wailing Caverns. | D |

Sources: `https://warcraft.wiki.gg/wiki/What%27s_Haunting_Witch_Hill%3F`, `.../Scepter_of_Naralex`,
`.../Cudgel_of_Naralex`, `https://warcraft.wiki.gg/api.php?action=query&list=search&srsearch=%22Disc+of+Naralex%22`.

---

## Part B — Chain and gating

```
HORDE chain (into the instance)
  Altered Beings (Tonga Runetotem)
    └─ Hamuul Runetotem (1489)
        └─ Nara Wildmane (1490)
            └─ Leaders of the Fang (914)   ← requires all 4 Fanglords dead + 4 gems looted
BOTH chain (out of the instance)
  Glowing Shard  ← 100% quest drop from Mutanus
    └─ The Glowing Shard (6981, Sputtervalve → Falla Sagewind)
        ├─ In Nightmares (Horde)     3369  → Hamuul Runetotem
        └─ In Nightmares (Alliance)  3370  → Mathrengyl Bearwalker
BOTH, independent (one-hop, no prerequisites)
  Deviate Hides (1486) · Deviate Eradication (1487) · Trouble at the Docks (959)
  Raptor Horns (865) ──► Smart Drinks (1491)
HORDE, independent
  Serpentbloom (962)
```
Tag: D for every link (each page's `Progression`/`previous`/`next` field); the diagram is I.

**The Disc of Naralex does not exist (see A3).** There is no quest that grants it and no item that performs the
awakening. In vanilla/Classic the awakening is performed *by an NPC* — the tauren druid **Disciple of Naralex**
(named `Muyoh` only from patch 4.0.3a onward) — and it costs the player nothing but the four Fanglord kills.
Tags: D (Muyoh page: "Only after his corrupted servants, the Fanglords, have been slain can I perform the
awakening ritual"; rename note in `==Patch changes==`).

**What actually gates the ending**

| Gate | Rule | Tag |
|---|---|---|
| G1 | All four Fanglords dead: Lady Anacondra (Screaming Gully), Lord Cobrahn & Lord Pythas (Pit of Fangs), Lord Serpentis (Crag of the Everliving). Order is free. | D |
| G2 | Only then does the Disciple of Naralex offer the "ready to defend me" branch; before that his dialogue is the "Naralex sleeps again / only after the Fanglords are slain" text. | D |
| G3 | The `Leaders of the Fang` gems are **quest items** — they drop only for players on quest 914, and only from the matching Fanglord. Killing a Fanglord without the quest gives no gem, so Horde must pick the quest up *before* the kills. | I |
| G4 | Nothing else gates the ending: Kresh, Skum, Verdan the Everliving and the Deviate Faerie Dragon are all optional and are the objective of no quest. | D |

---

## Part C — Scripted encounters

### C1. The Naralex awakening — the instance's one real script

Terminology for the sim: `DISCIPLE` = the tauren druid NPC at the instance entrance (Classic name
`Disciple of Naralex`, npc 3678; renamed Muyoh in 4.0.3a). `NARALEX` = the sleeping night elf on the island in
Dreamer's Rock at the far end of the instance (npc 3679, level 25, 713 hp, reaction friendly to both factions —
`{{Aggro|1|1}}`). Tags: D for names/levels/reaction, I for the npc-id mapping of the Classic Era name.

**Implementable step list**

| # | Trigger | Actor | Action / state change | Dialogue or spawn | Tag |
|---|---|---|---|---|---|
| 1 | Player enters instance, first NPC met | DISCIPLE | Gossip available immediately, *not* gated on quests. Casts **Mark of the Wild rank 2** on the player (armor +65, all attributes +2, 30 min). | "Naralex sleeps again! … Only after his corrupted servants, the Fanglords, have been slain can I perform the awakening ritual." | D |
| 2 | All 4 Fanglords dead (G1) | DISCIPLE | Gossip branch flips to "ready to defend me". No item required, no turn-in. | "The Fanglords are dead! All hope is not lost! … When you are ready to defend me, say the word, and I shall lead the way to Naralex's chamber." | D |
| 3 | Player accepts that branch | DISCIPLE | Escort mode on: DISCIPLE is now a **friendly, killable, escort NPC** that paths from the entrance toward Dreamer's Rock. He self-heals with **Druid's Potion** (298–402) and can **Sleep** a player for 30 s. | "I must make the necessary preparations… You must protect me!" | D |
| 4 | Scripted spawn point **A** ("the first turn" of his path) | spawner | Spawn **2 raptors** (deviate raptors, e.g. Deviate Ravager/Guardian) on the party. This is a fixed trigger, not a proximity pull. | — | D (wave count) / I (exact raptor type) |
| 5 | Scripted spawn point **B** (the **circle of torches**, a room with a ring of braziers) | DISCIPLE | Begins a **30-second cast, Serpentine Cleansing** ("Banishes the spirits of the slain Fanglords"). During the cast, waves of **Deviate Vipers** spawn and must be killed. Cast completion is the flag `caverns_purified`. | "Within this circle of fire I must cast the spell to banish the spirits of the slain Fanglords." / "Deal with this Deviate Viper! I need to prepare to awake Naralex!" / on complete: "The caverns have been purified. To Naralex's chamber we go!" | D |
| 6 | `caverns_purified` | DISCIPLE | Resumes pathing to Dreamer's Rock. | "Beyond this corridor, Naralex lies in fitful sleep. Let us go awaken him before it is too late." | D |
| 7 | DISCIPLE reaches NARALEX on the Dreamer's Rock island | DISCIPLE | Begins the **awakening ritual** — a long, interruptible-looking but uninterruptible channel. NARALEX stays prone on the island; he is **not** attackable, never gains a hostile flag, and is **never** a kill target. No player interaction is ever performed on NARALEX. | "Protect me brave souls as I delve into the Emerald Dream to rescue Naralex…" / emote: "Disciple of Naralex begins to preform the awakening ritual on Naralex." | D |
| 8 | Ritual tick 1 | NARALEX (emote only) | Flavour. | "Naralex tosses fitfully in troubled sleep." | D |
| 9 | Ritual tick 2 → **wave 1** | spawner | Spawn **3× Deviate Moccasin** (level 20 elite). | "Attacked! Help get this Deviate Moccasin off of me!" | D |
| 10 | wave 1 cleared → ritual tick 3 | NARALEX (emote only) | Flavour. | "Naralex writhes in agony. The Disciple seems to be breaking through." | D |
| 11 | Ritual tick 4 → **wave 2** | spawner | Spawn **many Nightmare Ectoplasm** (level 20 non-elite oozes). | "Deal with this Nightmare Ectoplasm! I need to prepare to awake Naralex!" | D |
| 12 | wave 2 cleared → **wave 3 telegraph** | NARALEX (emote only) | Before Mutanus appears. | "Naralex dreams up a horrendous vision. Something stirs beneath the murky waters." | D |
| 13 | Telegraph fires (short delay, ~"Mutanus comes up rather quickly" — no full eat/drink window) | spawner | Spawn **Mutanus the Devourer** out of the water of Dreamer's Rock: level 22 elite murloc, boss. Abilities: **Naralex's Nightmare** (sleep 15 s, Magic, interruptable), **Terrify** (fear 4 s, Magic, 1 target), **Thundercrack** (94–106 Nature + 2.5 s stun, point-blank AoE). **Immune to Shadow** magic. | — | D |
| 14 | Mutanus dies | NARALEX | **End of the dungeon.** NARALEX stands, yells, and departs — he transforms into an owl and flies away; pre-Cataclysm trivia notes the Disciple departed *untransformed* ("hovercow"). All remaining mobs and Druids of the Fang in the instance despawn. Mutanus' corpse yields the **Glowing Shard** for anyone eligible. | yell: "I AM AWAKE, AT LAST!" then "Ah, to be pulled from the dreaded nightmare! I thank you, my loyal Disciple, along with your brave companions." / "We must go and gather with the other Disciples. There is much work to be done before I can make another attempt to restore the Barrens. Farewell, brave souls!" | D |
| 15 | Failure mode | DISCIPLE | If DISCIPLE dies the run cannot be completed (retail hotfix 2013-05-29: "Fixed an issue where Muyoh could be killed, preventing players from completing the dungeon"). Whether Classic Era re-enables his death is **U**; model him as essential-but-killable with the run soft-locked. | — | D (hotfix note) / U (Classic Era behaviour) |

**Answers to the explicit questions**

| Question | Answer | Tag |
|---|---|---|
| Is Naralex attackable before the event? | No. Reaction is `{{Aggro|1|1}}` (friendly to both factions) at every listed location; a friendly NPC cannot be attacked, and he has no loot table and no abilities. | D (reaction) / I (non-attackable) |
| Does Naralex die? | No, at any point, and not after the event either — he flies off alive. He has no death state in the encounter. | D |
| When does Naralex become "interactable"? | Never as an object of player interaction. The interactable is the **Disciple**; Naralex is a purely scripted prop whose state changes are driven by the Disciple's cast and by Mutanus' death. | I |
| What does the Disc do? | There is no Disc (see A3/B). The Disciple's 30 s **Serpentine Cleansing** cast at the torch circle is the only "rite" the party must defend. | D |
| What spawns as a result, and where? | **Mutanus the Devourer**, from the water of **Dreamer's Rock**, immediately after the third ritual wave. | D |
| Does the party need to kill the Fanglords first? | Yes — G1/G2. Not a per-player quest check; it is the instance boss-death state behind the Disciple's gossip. | D |

Sources: `.../Muyoh` (dialogue block, "Escort tactics", abilities, hotfix), `.../Naralex` (`==Quotes==`, Npclocations),
`.../Mutanus_the_Devourer` (abilities, Shadow immunity, "last boss"), `.../Wailing_Caverns` (walkthrough, subzone table).

### C2. Verdan the Everliving — optional, hostile, off the critical path

| Fact | Value | Tag |
|---|---|---|
| Where | The **Crag of the Everliving**, the same final room as Lord Serpentis; he stands apart on his own ledge/island | D |
| How reached | Same path as Serpentis — you do not need to enter his sub-area; walkthrough advice is literally "don't get near and aggro Verdan" while killing Serpentis | D |
| Trigger | None. He is a normal hostile elite (`{{Aggro|-1|-1}}`) with **no scripted event**, no waves, no dialogue. | D |
| Optional? | **Yes.** "Killing Verdan is optional, he is not the object of any quest." Nothing in G1–G4 references him, and skipping him does not block the Naralex event. | D |
| Threat | The hardest fight in the instance: large HP pool, 200–300 melee per hit, **Grasping Vines** (Physical PBAoE, knockdown + 10 s immobilise). | D |
| Loot | Living Root / Seedcloud Buckler / Sporid Cape — no quest use. | D |
| Type for CC | Elemental (bog beast) — cannot be polymorphed or sapped | D (creature type) / I (CC consequence) |

`https://warcraft.wiki.gg/wiki/Verdan_the_Everliving`

### C3. Everything else with script-like behaviour

| Thing | Behaviour to model | Required for clear? | Tag |
|---|---|---|---|
| **Kresh** (npc 3653) | Rare-spawn-*like* named turtle, **neutral** (`{{Aggro|0|0}}`) — never initiates combat, wanders the river of **Pit of Fangs** near the start. Single ability **Crushing Bite** (armor −10% for 20 s). Tameable by hunters. | No | D |
| **Deviate Faerie Dragon** (npc 5912) | **Rare elite**, estimated 5–10% spawn chance, in the **Crag of the Everliving** on the way to Lord Serpentis. Dragonkin, hostile, no abilities listed, no script. May simply not exist in a given run. | No | D |
| **Skum** | Named thunder lizard in a pen in the Crag of the Everliving, guarded by two Druids of the Fang plus nearby trash. Optional boss, no quest, no script. Killable out of order. | No | D |
| **Mad Magglish** | Stealthed goblin in the **non-instanced caves before the portal** (3 fixed spawn points). Uses **Sneak** (stealth, −50% speed) and **Smoke Bomb** (3 s stun); he is invisible until you get close, then attacks. Drops **99-Year-Old Port** for quest 959. | No (out-of-instance) | D |
| **Druid of the Fang** (trash) | Not scripted, but has a two-phase loop worth scripting: opens with **Lightning Bolt**, shapeshifts to a cobra for melee, morphs back to **heal itself** when badly hurt; can **Sleep** a party member. | No | D |
| **Deviate Shambler** | Casts a self-**regeneration**; purge/dispel counters. | No | D |
| **Deviate raptors** (Ravager/Guardian/Slayer/Stalker) | **Call for help when nearly dead** — pulls neighbours. Only "scripted" behaviour on ordinary trash. | No | D |
| **Serpentbloom nodes** | Herb objects inside the instance; looting one **spawns a Serpentbloom Snake**. 10 needed for quest 962. Trackable by herbalists. | No | D |
| **Ectoplasms** | Evolving Ectoplasm clones/splits (Cloned Ectoplasm); Devouring/Evolving/Nightmare Ectoplasm drop **Wailing Essence** at 100% for quest 1491. | No | D |
| **Doors / gates / kill-to-open** | **None.** The instance has no locked door, no lever, no "kill X to open the way". The only hard gate is the four Fanglords → Disciple gossip (G1/G2). Navigation gates are jumps/drops (e.g. "jump off the cliff" to return from the Crag). | — | D |
| **Boahn / Trigore the Lasher** | Rare mobs in the **non-instanced** entrance caves, not in the instance proper. | No | D |

Sources: `.../Kresh`, `.../Deviate_Faerie_Dragon`, `.../Skum`, `.../Mad_Magglish`, `.../Serpentbloom`,
`.../Wailing_Caverns` (denizen table + walkthrough).

### C4. Clear condition

| Question | Answer | Tag |
|---|---|---|
| What ends the dungeon? | **Mutanus the Devourer dies.** That single event fires Naralex's awakening, the despawn of every remaining hostile in the instance, and the departure of Naralex and his disciple. | D |
| Is that reachable without the quests? | Yes. The kill chain is G1 (four Fanglords) → G2 (Disciple gossip) → C1 steps 3–13. No quest is a prerequisite for the ending; quests only add objectives and loot. | D (G1/G2 dialogue gating) / I (no quest prerequisite) |
| What should a sim award at the end? | The **Glowing Shard** from Mutanus (starts quest 6981, which forks to `In Nightmares` by faction), plus the four **dream gems** for Horde players holding quest 914, plus **20 Deviate Hides** / the four 7-kill counters / **10 Serpentbloom** / **6 Wailing Essence** / **99-Year-Old Port** for whoever holds those quests. | I |
| Does the group have to leave? | No requirement; the instance stays open. Retail's post-4.0.3a "Naralex flies off" animation is the visual end-state. | D |
| Optional content that can be skipped entirely | Kresh, Skum, Verdan the Everliving, Deviate Faerie Dragon, Mad Magglish (outside), the non-instanced entrance rares. | D |

`https://warcraft.wiki.gg/wiki/Wailing_Caverns` (walkthrough: "If you manage to defeat this boss, Naralex awakens,
all the monsters and Druids of the Fang in the Wailing Caverns disappear"), `.../Muyoh`.

---

## Ambiguities a sim author must decide

| # | Ambiguity | Evidence state |
|---|---|---|
| 1 | Exact escort route and how long the Disciple walks: the wiki gives the two scripted spawn points ("the first turn", "the circle of torches") but not coordinates, and does not say whether the corridor between the torch circle and Dreamer's Rock is scripted or just trash. | D for the two triggers, U for geometry |
| 2 | Exact raptor species spawned at escort trigger A (wiki says only "2 raptors"). | U |
| 3 | Exact count of Nightmare Ectoplasm in ritual wave 2 (wiki: "many … non-elite slimes"). | D that it is many/uncounted |
| 4 | Whether Classic Era still lets the Disciple be killed (retail hotfixed this in 2013). | U |
| 5 | Whether Naralex transforms into an owl *and* the Disciple does too, or only Naralex (walkthrough says both transform; the trivia note says the Disciple floated away untransformed). Cosmetic only. | conflicting D |
