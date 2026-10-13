// =============================================================================
// Wailing Caverns — "The Fang of the Nightmare"
//
// Story-and-encounter beats for the party roguelike, authored in ink so the
// narrative is data, not code: the sim fights the encounters, and this script
// narrates around them. The host drives it through two external functions:
//
//   start_encounter(boss_id)   the party has reached a boss; hand off to the sim
//   complete_quest(quest_id)   a quest objective has been satisfied
//
// Canon (verbatim names, sourced): the four Fang leaders are Lord Cobrahn,
// Lord Pythas, Lord Serpentis and Lady Anacondra; the final aberration is
// Mutanus the Devourer; Naralex is the night-elf druid sleeping at the heart
// of the caverns. The premise — Naralex led his followers into a shared slumber
// to dream the arid Barrens green, and the Nightmare poisoned that dream — is
// the dungeon's backstory.
//
// Placeholder: every line of dialogue here is authored in the spirit of the
// source, not a verbatim transcription. Quest and boss names are canon; the
// exact words are original. The sim currently models only a subset of these
// bosses (Lady Anacondra and Lord Serpentis); the others are story beats the
// roster will grow into.
//
// Recompile (inklecate v1.2.1+):
//   inklecate -o Scripts/wailing_caverns.ink.json Scripts/wailing_caverns.ink
// =============================================================================

EXTERNAL start_encounter(boss_id)
EXTERNAL complete_quest(quest_id)

VAR checkpoint = "entry"

-> entry

=== entry ===
The Lushwater Oasis steams beneath the Barrens sun — a wet green sore in a land of baked red stone. At its centre, the water does not ripple. It only sinks.
A rent in the rock drinks the oasis down into darkness, and from that darkness rises a sound like a thousand serpents sighing at once.
+ [Descend into the caverns]
    -> caverns
+ [Study the water first]
    The surface of the pool is wrong. Reflections of trees bend where no wind blows, and beneath your own face something long and pale uncoils and is gone.
    -> caverns

=== caverns ===
The heat of the Barrens dies behind you. The walls sweat, then glisten, then breathe.
Ferns the colour of old bruises grow from every seam, and coiled in the roots of each is a shape that might once have been a snake, or a druid, or both.
The sighing has become a voice — many voices, murmuring the same word: the Fang.
-> quest_hub

=== quest_hub ===
Where three tunnels meet, a tauren crouches beside a guttering lantern, keeping his back to the wall. He watches the darkness the way a man watches a rising flood.
"You have come at a poor hour," he says. "The Fang is waking."
* ["What is the Fang?"]
    "Druids of the Fang," he says, and the name sits heavy. "Our own. They came with Naralex to heal the Barrens — to dream the green back into this land. Now they do not dream. They serve the serpent."
    -> quest_hub
* ["Who is Naralex?"]
    "A night elf, and a dreamer greater than any of us. He sleeps below, at the very root of the caves, holding a whole green world in his mind. But the dream has turned. What he dreams now, the caverns obey."
    -> quest_hub
+ ["I am here to put a stop to this."]
    The tauren nods once, and relief and fear cross his face in equal measure.
    "Then hear me. There are two things that need doing here."
    -> quests
* [Leave him and press on]
    "Wait —" he calls after you, but the tunnels have already swallowed the sound.
    -> fang_hall

=== quests ===
"There are hides to be gathered from the warped creatures — deviate hides — proof of the corruption, and a measure of how far it has spread. Bring me what you find.
And more than that: the Fang has four leaders. Lord Cobrahn. Lord Pythas. Lord Serpentis. Lady Anacondra. As long as one of them dreams, Naralex cannot wake."
+ [Accept both tasks]
    "The caverns will try to turn you," he warns. "They whisper. Do not answer them."
    -> fang_hall
+ [Decline]
    "Then the Fang will dream on," he says, "and the Barrens will not be saved by you."
    -> fang_hall

=== fang_hall ===
The tunnel opens into a hall of hanging moss and shallow, silvered water. Pillars of stone twist upward like the ribs of some vast buried thing.
In the water, something moves with a woman's shape, and does not move like a woman at all.
Lady Anacondra rises, and her eyes are a serpent's, golden and unblinking.
"Little dreamers," she hisses. "Come. The Fang will teach you to shed your skin."
~ start_encounter("lady_anacondra")
-> post_anacondra

=== post_anacondra ===
Lady Anacondra falls, and for a heartbeat the water stills.
The silvered surface clears, and in it you see the hall as it once was — a grove, whole and green, before the dream curdled it.
Then the vision is gone.
-> cobrahn

=== cobrahn ===
The next chamber stinks of dry scale and old venom. Coiled across a dais of fused bone, Lord Cobrahn watches your approach with the patience of something that has already decided how it will digest you.
"You walk upright and call it strength," he breathes. "The Fang crawls, and knows better."
~ start_encounter("lord_cobrahn")
-> post_cobrahn

=== post_cobrahn ===
Lord Cobrahn's coils slacken at last. The dais cracks beneath his weight, and cold water wells up through the stone.
-> pythas

=== pythas ===
A hiss like sand through a skull fills the third chamber. Lord Pythas hangs from the ceiling, his long body woven through the dark, his voice sliding from every direction at once.
"The dreamer below made us," he says. "He dreamed a world without endings. We are what happens to a thing that will not end."
~ start_encounter("lord_pythas")
-> post_pythas

=== post_pythas ===
Lord Pythas falls from the dark and does not rise. The ceiling is suddenly empty, and the silence is worse than the hissing.
-> serpentis

=== serpentis ===
The last of the Fang leaders does not wait to be found. Lord Serpentis blocks the tunnel ahead, his form vast and pale, his eyes reflecting your torch like two lamps at the bottom of a well.
"You have killed the dream's children," he says, almost gently. "Now you will meet its teeth."
~ start_encounter("lord_serpentis")
-> post_serpentis

=== post_serpentis ===
With Lord Serpentis still, the caverns go quiet — a held breath, waiting.
The Fang's four leaders are dead. Somewhere above, the tauren's lantern gutters, and the Barrens waits.
~ complete_quest("leaders_of_the_fang")
-> devourer

=== devourer ===
The water is deeper here, black and still, and the air tastes of salt and things long drowned.
From the depths rises Mutanus the Devourer — not a druid, not a serpent, but a nightmare's answer to both: a pale, bloated maw that has forgotten what it once was.
It does not speak. It only hungers.
~ start_encounter("mutanus")
-> post_mutanus

=== post_mutanus ===
Mutanus the Devourer collapses back into the black water, and this time the surface closes over it and holds.
The dream's teeth are gone. Only the dreamer remains.
~ complete_quest("deviate_hides")
-> naralex

=== naralex ===
At the heart of the caverns lies Naralex, the dreamer.
He sleeps on a bed of living moss beneath a shaft of light that should not reach this deep, and the green of the Barrens-that-could-be ripples across his brow. Around him, the nightmare coils like smoke, and through him it pours out into the stone.
His lips move, but the words are not his own.
+ [Wake him]
    You reach through the smoke and take the dreamer by the shoulder.
    "Naralex," you say. "The Fang is broken. The dream can end."
    The nightmare writhes. The caverns shriek. And for the first time in years, Naralex's eyes begin to open.
    -> ending
+ [Leave him to dream]
    The nightmare will keep him, and the Barrens will keep dying. Some part of you already knows you will come back, and finish this.
    -> ending

=== ending ===
Light spills down through the shaft, and the green that was trapped inside the dreamer's mind washes out over the caverns, soft as rain.
The ferns straighten. The water clears. The sighing stops.
The Barrens will not bloom overnight — but somewhere above, in the Lushwater Oasis, a seed has cracked open.
And the Wailing Caverns have, at last, gone quiet.
-> END
