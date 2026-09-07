# Emberline — campaign architecture

The redesign brief asked for one thing: stop Renzo receiving an objective and
running to the objective marker. This document is the thinking the brief demanded
happen before the hundred missions were touched — what the shipped campaign
actually does, what is missing, the systems the new campaign needs, the people it
needs them for, what each of those costs to build, and the shape that came out.

The hundred missions themselves are in `CAMPAIGN.md`, which is generated from
`CampaignTable.cs` (the story) and `CampaignDesignTable.cs` (the game). This file
is written by hand and is the argument behind both.

Part 1 is the audit. Parts 2 to 4 are the design. Part 5 is the build cost and
says plainly which pieces are not built. Part 6 is what actually shipped in this
change, with the numbers a reviewer should check.

---

## PART 1 — WHAT THE SHIPPED CAMPAIGN ACTUALLY IS

Not what it intends. What a player does with their hands, mission by mission,
read out of `CampaignTable.cs` and the hundred generated plan assets.

### Fifty-nine of the hundred missions are one of two missions

Sorting every mission by what its stated objective actually asks the player to do:

| Shape | Count | What the player does |
|---|---|---|
| Clear the arena | 38 | walk in, kill what spawns, walk out |
| Walk and read | 21 | walk a route, touch N glowing clues, fight once |
| Everything else | 41 | the campaign's actual ideas |

The 38 break down as 19 pure kill-or-break missions, 4 "destroy the object" with
a timer, 6 "hold the point against waves", and 9 "survive what is in here". The 21
are the investigation formula: the clue is a floating cube on a jittered ring with
no authored content, and finding it is walking within a metre and a half of it.

The generated stage text gives the game away. Across roughly 598 authored stages,
the objective line **"READ THE GROUND" appears eighteen times**, "READ THE FIELD"
thirteen, "THE FIRST OF THEM" thirteen, "PRESS ON" thirteen, "CLEAR" thirteen,
"GONE" thirteen. A player who reaches mission 40 has read the same six sentences
about seventy times.

And `Campaign.cs` hardcodes `objective = MissionObjective.Clear` on all one
hundred levels regardless of what the mission says it is. Whatever the table
claims a mission is about, the catalogue tells the game it is a clear.

### The campaign has one named ally, and she is not there

Searching the mission table, every story beat asset and the whole of the runtime
cast roster: the only named ally in Emberline is **Aiko**, and she is absent as a
present character for **79 of 100 missions**. Everybody else with a name is an
enemy — Goro, the Pale Shade, the Three Blades, the Convoy Captain, the Scavenger
King, the Drowned Guardian, the Iron Guard, Commander Hoshu, Jin, Kagehira.

The people who could have been allies are all introduced and then dropped:

- the survivor of mission 14, who remembers Aiko alive after the fire
- the blacksmith of mission 36, who marked every blade he was forced to make
- the old guide of mission 47, who knew Renzo's father
- the girl freed from the pen at mission 28
- the mutineers of 84, 89 and 92, who fight beside Renzo three times and are
  never named, described or spoken to

Every one of them is a single-mission object. Mechanically they are the same
object: one generic runtime class called the lantern bearer, with no name, no
dialogue and no variation, and whose own class comment notes that enemies never
path to it. There is no friendly combatant anywhere in the project and no concept
of a side.

### Villagers are cargo

Villagers appear in seven missions. In five of them they are freight: prisoners
in wagons, bodies in a pen, a line on an execution platform. They are alive,
present and not being carried in exactly two missions of a hundred — the siege at
37 and the reed village at 46. The class comment states the design directly:
villagers do not fight and cannot help. Enemies cannot even kill them; the only
way a villager dies is the player's own swing.

The freed villagers of missions 32 and 33 "go into the hills" and are never
mentioned again in the remaining sixty-seven missions.

### No villain's defeat changes anything

Ten villains, twenty-two appearances. Every single defeat resolves into one of
three things: a document, a dying line, or an opened gate — each pointing at the
next mission. Goro's valley is not stated to be freed when Goro dies. The forest
does not become safe when the Three Blades fall. The marsh does not open when the
Pale Shade dies. Jin's territory does not change hands. The only defeat with a
consequence wider than the following mission is the last one.

The Iron Guard is used three times, at 74, 78 and 94, as the same generic wall.

### The engine can already do more than the campaign asks of it

This is the good news, and it changes what the redesign costs.

- **A working two-route branch system.** A stage can hold two entrances with
  separate rosters; the director marks both, remembers which the player took, and
  every later stage in the plan spawns its B roster instead of its A roster
  accordingly. There is even an event that turns the road not taken back on
  behind you. It is used by **two missions out of a hundred**.
- **A pre-mission choice widget on the briefing screen**, built for duel terms:
  it cycles an option, rebuilds the screen, and changes both the fight and its
  reward. That is an approach selector wearing a different label.
- **A persisted general-purpose flag store** that nothing in the shipped game
  writes to. Village trust, preparation and camp intel all fit in it for free.
- **A cinematic system** that plays an authored beat anywhere inside a mission,
  suppresses gameplay while it runs, and degrades gracefully when the asset is
  missing.
- **`Eliminate` is never used once.** Neither is a marked assassination target;
  the "assassinate" and "duel" goals fall through to the same code path as "kill
  everything in the arena", and four differently named goals are one behaviour.
- **`Chase` has no fleeing target.** The runner is an ordinary enemy that charges
  the player, and the mission fails on a timer.
- **`Defend` has no point to defend.** It is a duplicate of survive-for-N-seconds
  with an orange marker next to it.

### The seven things that are missing, named plainly

1. **Anyone to talk to.** Six of the nine conversation missions are conversations
   with an enemy or a dead man.
2. **Any reason to be somewhere before the fight starts.** There is no
   reconnaissance, and no mission's difficulty depends on anything done earlier.
3. **Any choice at the door.** Two missions in a hundred offer an entrance.
4. **Any persistence.** No per-mission, per-region or per-faction state of any
   kind survives a mission ending. Nothing remembers the player.
5. **Any place.** There are regions and arena themes; there are no camps, no
   villages and no home that the player returns to and finds changed.
6. **Any reason to prepare.** No supply, no resource, no upgrade path that runs
   through the world rather than through a shop menu.
7. **Any consequence.** See above.

### What is genuinely good and must survive

- **Every mission ends on the reason the next one begins.** This is the campaign's
  best property and the redesign keeps it untouched.
- **The Endure missions**, where a foe cannot be beaten and the clock ends the
  fight, are a real idea, well used four times over.
- **The memory chapter**, 52 to 56, where the player walks Yorune's last evening,
  fights as his father, then plays Aiko with no sword. Nothing needs changing.
- **The optional objectives are conditions on the whole mission**, not skippable
  chores. That decision was already right.
- **The pacing validator** already forbids a mission from opening on a fight,
  ending on a fight, running more than two fights back to back, or sharing its
  shape or its roster with the mission before it. The redesign tightens it; it
  does not replace it.


---

## PART 2 — THE COMPANIONS

Six people. Three fight, three do not. Every one of them is a promotion of somebody
the shipped campaign already puts on screen and then forgets, which is the point:
the game already introduces a survivor who remembers Aiko, a blacksmith who
marked every blade, and an old guide who knew Renzo's father, and then never uses
any of them again.

Renzo is alone for missions 1 to 11. That is deliberate and it is the baseline
the rest of the campaign is measured against. He is alone again from 95 to 99,
and by then the word means something else entirely. At 100 all six are there,
and which of them is standing is the sum of what the player prepared for.

---

### SUZU — the Scout
**Joins mission 12. Leaves mission 77. Returns mission 89.**

Nineteen, a toll-road runner's daughter from a valley Goro emptied. She has been
stealing from the same convoys Renzo is shadowing, for a year, badly.

- **Motivation** her brother Kanta is a name on a prisoner roll. Everything she
  does is a way of reading more rolls.
- **Specialty** patrol routes, second entrances, alarms. With Suzu, a camp's
  watch rotation is visible; without her it must be learned by watching.
- **Strength** she is the only reason a STEALTH approach exists before mission 40.
- **Weakness** she runs. When a fight turns, she leaves — and the mission does
  not fail, which is worse, because it means the player watches her go.
- **With Renzo** she thinks he is proof that revenge works. Early this is charming.
  By mission 60 it is the most frightening thing anyone says to him.
- **With villagers** she knows everyone's cousin. Trust rises faster in any
  village she walks into.
- **Personal missions** *What He Was Carrying* (17), *What the Rolls Say* (19),
  *Kanta* (76). She also walks the long way round at 45, which is where she
  finds the reed village before anybody else does.
- **Story load** she is why mission 76 lands. Renzo opens every cell in the prison
  tower looking for Aiko and none of them is Aiko — but one of them is Suzu's
  brother. She gets what he came for. What Renzo does with his face in that moment
  is the cleanest read on his transformation in the game, and the player controls
  none of it.
- **Line, early** "You're going to get her back. People do."
- **Line, late** "You used to ask me what I saw. Now you tell me what I saw."

---

### FUMI — the Informant
**Joins mission 14. Never fights. Present to the end.**

The survivor of mission 14, given a name and a job. She was a clerk in a records
house; she survived the fire because she was in the cellar copying a ledger.

- **Motivation** she wrote down where people were sent. The lists Kagehira's
  officers use to find people are, in part, in her handwriting.
- **Specialty** she reads the enemy's paper. Every document Renzo brings back is
  worth more with Fumi than without: a captured order becomes a watch rotation, a
  ledger becomes a camp card, a cipher becomes a name.
- **Strength** with Fumi, one intact document replaces an entire recon mission.
  She is the systemic answer to "I do not want to scout tonight."
- **Weakness** she will not destroy records, ever, including the ones killing
  people. This costs the player something real at mission 87.
- **With Renzo** she remembers Aiko as a child and he cannot ask her about it
  without his voice changing. She notices.
- **Personal missions** *The Cellar* (19), *Her Own Handwriting* (64). At 87 she
  refuses to burn the records, which is preparation rather than a personal
  mission and costs the player something real either way.
- **Line** "I am not on your side. I am on the side of knowing. It has been on
  your side so far."

---

### TSURU — the Archer
**Joins mission 26. Present to mission 93.**

A conscript archer of Kagehira's who walked off a wall two years ago and has been
living in the pines since. He is the one who found the body of the officer killed
for refusing to take a child north, and he buried him.

- **Motivation** he wants the war to end in any direction at all. He does not
  care who wins. This is not cowardice and the game should never frame it as such.
- **Specialty** height. Given a roof, a ridge or a tower, Tsuru covers an
  approach — and covering fire is what makes an AMBUSH approach survivable.
- **Strength** he is the only companion who improves a fight you are losing.
- **Weakness** arrows are finite and the game counts them. A village at trust 2
  supplies him; a village left to burn does not.
- **With Renzo** he is the only one who has stood where the enemy stands. He is
  the small, survivable version of Jin, met forty missions early, so that when Jin
  says "I gave him the map" the player already knows what that costs a person.
- **Personal missions** *The Last Arrow* (35), *Names on the Shafts* (92). At 74
  he takes the wall he deserted, and the game does not let anyone enjoy it.
- **Line** "I shot at people like you for two years. I was not good at it. That
  is the only reason we are talking."

---

### DAIGO — the Warrior
**Joins mission 33. His ending is decided by the player at mission 89.**

Taken out of Goro's pens with a smith's collar still on him. A wrestler before
the war, enormous, slow, and — this is the whole character — a man who once ran
from a burning house with somebody still inside it.

- **Motivation** the debt. He states it once, at mission 43, and never again.
- **Specialty** he holds. A gate, a bridge, a door, a line of villagers. He
  cannot chase and should never be asked to.
- **Strength** ALLIED approaches only work because someone can hold a gate.
- **Weakness** he cannot be quiet. No mission with Daigo has a STEALTH approach,
  and the game should say so out loud on the briefing screen.
- **With Renzo** they barely talk. Daigo is the one who physically stops him at
  mission 68 and takes a punch for it.
- **Personal missions** *The Coward's Debt* (43), *His Own Mark* (78, shared with
  Toku). *What He Owes* is 89, and it is not a personal mission because it is
  not his to decide.
- **Story load** at mission 89 Daigo holds the fortress gate through the largest
  waves in the game. **Whether he lives is decided by preparation, not by script**
  — the barricades, the arms, the militia and the mutineers are all flags the
  player either set or did not set across the preceding fifteen missions. If he
  dies, mission 100 is a different scene, permanently. This is the one death in
  the campaign, it is earned, and it is the player's.
- **Line** "I will be at the gate. That is all I am for. Don't waste it."

---

### TOKU — the Blacksmith
**Joins mission 36. Never leaves the forge.**

The smith of mission 36, given his name and the consequence he already had: he
marked every blade he was forced to make, and those blades are on the Iron Guard
at mission 78 and on the wall commander at mission 74.

- **Motivation** to unmake his own work. He is the reason Yorune steel keeps
  turning up on the enemy — he armed them, under a hammer held over his family.
- **Specialty** weapons. Steel brought back from a raid becomes an upgrade; the
  duel with Jin is meaningfully different depending on whether the player fed the
  forge for the twenty missions before it.
- **Strength** the only source of weapon tiers that is not a shop.
- **Weakness** he needs materials, and materials come from villages and camps —
  the loop the brief asks for, closed.
- **Personal missions** *His Own Mark* (78), *The Last Honest Blade* (94). He
  joins at 36 and then goes nowhere for fifty-eight missions, which is why the
  one time he climbs — as far as the last guard post, carrying a blade, and no
  further — is worth a mission of its own.
- **Line** "Every one of them has my mark. I put it there so that one day somebody
  would come and ask me why. You took eleven years."

---

### NIRE — the Healer
**Joins mission 46. Refuses the mountain at mission 71.**

The old guide of mission 47, moved earlier and given a trade. A marsh herbalist,
seventy-odd, who knew Renzo's father when he came through with something wrapped
in cloth.

- **Motivation** she does not think anyone should be going anywhere. She comes
  because the alternative is Renzo going alone.
- **Specialty** medicine, and the marsh. She is the only route through water.
- **Strength** supplies that persist between missions; one recovery per mission.
- **Weakness** she is slow, and she will not enter a fight. A mission with Nire
  is a mission with a person in it who can die.
- **With Renzo** she knew his father, so she is the only character with standing
  to say the sentence the game is built around: *you are becoming the other one.*
  She says it at mission 69, before Jin does, and Renzo does not answer her.
- **Personal missions** *What the Water Keeps* (47), *She Will Not Climb* (71).
  At 60 she is in the room when Renzo stops looking for answers, and she is the
  one who notices first.
- **Line** "I knew two men who were sure. One burned a village and one let it
  burn. Be a third thing."

---

### How the six change across the campaign

The companions are the instrument that measures Renzo. Their read on him moves in
four steps, and the same three lines are re-recorded at each step:

| Missions | What they say about him |
|---|---|
| 12–40 | "Renzo is careful." |
| 41–60 | "Renzo does not stop." |
| 61–80 | "Renzo is becoming reckless. Say it to him." |
| 81–99 | "He is becoming like Kagehira." — and one of them says it to his face |

At mission 68 they refuse an assault outright. The mission does not start. The
player has to either wait until morning — a real, playable, different mission —
or go alone, which is also playable, and worse, and remembered.

---

## PART 3 — THE NINE SYSTEMS THE REDESIGN NEEDS

Each system is stated as: what it is, what the player feels, what data it needs,
what runtime work it costs, and what it would cost to cut it.

**Read this section as design, not as a release note.** Systems 1 to 5 are
authored, validated and visible in the game's plans today. Systems 6 to 9 are
authored as campaign data and are not yet wired to anything the player can feel.
Part 5 says exactly which is which and what each remaining piece costs.

### S1 — The approach (the planning phase)

A major mission is not one plan. It is one **objective** with two to five
**approaches**, and the player picks one on the briefing screen before the
mission loads.

    STEALTH      go in unseen, alarms disabled, few enemies, no margin for error
    ASSAULT      the front door, the most enemies, the most room to fight
    AMBUSH       do not enter: break the road, split the patrol, fight on your ground
    SABOTAGE     burn the supplies, empty the camp, take the objective from a shell
    ALLIED       bring companions and militia; multiple directions at once

Not every approach is available on every mission, and an approach past the first
is **unlocked by reconnaissance, information or preparation** — not by a menu.
The first entry is the default and is always available having done nothing at
all, and on all but three of the missions that offer a choice it is the front
door: a player who prepares nothing can walk in and have the hardest version of
the fight. This is the rule the brief asks for. Preparation is never mandatory.
It is rewarding.

The three exceptions are missions 11, 12 and 20, which are a supply train, a
night shipment and a signal tower. There is no front door on a moving wagon, and
the campaign should not invent one to satisfy a rule.

Data: `MissionApproach[] approaches` on the campaign mission; each approach names
the plan variant it loads, the flag that unlocks it, and one line of companion
dialogue that proposes it.

Runtime: the director already runs a plan asset chosen by name, and `ReachAny`
already proves the engine can hold two routes open in one mission. An approach is
a *plan variant*: `C034_Stealth`, `C034_Assault`. The generator emits them; the
briefing screen picks one. This is the single largest runtime item and the one
worth building first, because every other system feeds it.

### S2 — Reconnaissance

Before a camp, Renzo can watch it. A recon mission is a real mission with a real
fail state (being seen), whose reward is *information*, spent on approaches.

Recon marks are things, not icons: the alarm bell, the commander's tent, the
prisoner pen, the supply store, the archers' roofline, the patrol that walks the
wrong way at the wrong time, the drain nobody watches. Each one found is one
entry on the camp's card, and the card persists.

The player is never shown the whole camp. Finding six of nine marks is a good
night's work, and the three you missed are the three that surprise you.

Data: `ReconMark[]` per camp; `campId` on missions that read from the same camp.
Runtime: `Investigate` already finds N clues; recon is Investigate whose results
persist past the mission end. Cheap.

### S3 — Preparation, and its consequences

A preparation mission is optional, short, and changes the mission it feeds:

    burn the supply train        →  eight fewer defenders in the assault
    free the pen first           →  freed prisoners fight, badly, on your side
    recruit the hunters          →  two allied archers on the wall
    repair the barricades        →  the village line holds one wave longer
    get the steel to Toku        →  one weapon tier before the duel
    scout the ridge              →  the commander is marked from the first second
    learn the watch rotation     →  a four-minute window with no patrol

The consequence is applied by the *next* mission reading a flag, so a player who
skipped everything fights the authored hard version and a player who did all six
fights a camp that is already half beaten. Both are shipped, playable missions.

Data: `string[] setsFlags` and `string[] readsFlags` on the mission.
Runtime: PlayerPrefs, the same store `StoryMemory` and `Feats` already use.

### S4 — Camps as sandboxes

A camp is a persistent place with a name, a garrison of twenty-four to forty, and
four to seven things in it that the player can act on independently: the
commander, the alarm bell, the prisoner pen, the supply store, the armoury, the
blacksmith, a patrol route, a locked area, a way in nobody watches. A camp
appears in three to seven missions across its chapter — watched, raided,
sabotaged, taken, then lived in. The full list of what is in each one is in
`CAMPAIGN.md`, because the camps are data, not prose.

    THE TOLL POST       ch1   Goro's checkpoint, and the first place you scout
    THE BROKEN BANNER   ch2   bandits in an abandoned village
    THE SILENT CAMP     ch3   the forest camp the Three Blades answer to
    THE PENS            ch4   Goro's prison camp, the largest in Act II
    THE SUNKEN CAMP     ch5   what the marsh took back
    THE GARRISON        ch7   Jin's town, which is not hostile at first
    THE FROZEN CAMP     ch8   artillery above the mountain road
    THE IRON FORTRESS   ch8   the wall, the drain, the tower, the inner gate
    THE SUMMIT ROAD     ch10  the last one, and the only one you take alone

That is nine, not eight, because the Silent Camp earned its name during the
pass: three missions in chapter 3 were already circling the same place without
saying so.

### S5 — Companions

Six, and no more. Two may be taken into a mission. Each is a *verb*, not a
health bar: what they change is how the mission can be approached.

Companions are not a squad that follows you. They appear where their reason to
be there is. They refuse missions they disagree with, and after mission 61 they
start disagreeing more.

### S6 — Villages and trust

Five villages, each with a trust level of 0 to 3, raised by doing work for them
and lowered by leaving them to burn. Trust buys supply, not stats:

    YORUNE             ch1    nothing, until mission 100, and that is the point
    ASHFALL            ch2    hunters, and the arrows Tsuru counts
    KIBA               ch4    steel, Toku's forge, and militia at full trust
    THE REED VILLAGE   ch5    medicine, marsh routes, and Nire
    THE GARRISON TOWN  ch7    information, and the only place in the game
                              where nobody is hunting anybody

At trust 3 a village will send militia to an ALLIED approach. Militia are not
soldiers: they die, and the mission remembers how many.

### S7 — Consequence: the world after a villain

Each of the ten villains owns a piece of the world before their boss fight, and
losing it changes the map. This is stated as world-state, not as a debrief line.

### S8 — Walking away

Three encounters are deliberately beyond the player on first contact and are
**not** gated by a locked door: the player may walk in, and should walk out. Each
is a mission that already exists, attempted before its preparation is done, and
each is revisited later with the ally, the blade or the collapsed slope that
makes it possible. They are named in Part 4 under the "not yet" rule.

### S9 — The road between missions

Travel beats with no combat and no objective marker: a fire, a meal, a repair, a
conversation, a body on the road, a rumour. Five of them in the shipped
redesign — missions 27, 60, 68, 81 and 100 — each carrying one piece of character
work that no cutscene delivers, and each placed at a turn in the story rather
than at a convenient gap in the pacing.

Mission 27 is the one to point at. It is the red thread, a wood, and nobody else
in it. The game never says there is nothing here; the player finds that out by
being ready for a fight that does not come, which is a thing the shipped
campaign could not do once in a hundred missions.

---

## PART 4 — THE RULES THE NEW HUNDRED IS WRITTEN TO

The brief's fourteen questions were asked of every one of the hundred. Most
missions failed on the same four: the player has no choice, nothing was prepared,
nobody is there, and nothing changes. Those four are what the design layer fixes,
mission by mission, without touching a story spine that was already good. One
mission — 27 — was rewritten outright, because it was a quiet discovery with a
fight bolted onto the end and the campaign badly needed one place with no fight
in it. These are the rules that came out of asking, stated so the next person can
hold the line.

### The shape rule

A major mission is not a level. It is a **sequence of missions around one place**,
and the place is the unit of design, not the mission:

    DISCOVERY   somebody tells you a thing exists, or you see smoke
    RECON       you go and look, and you are not allowed to be seen
    PREPARE     optional work that changes the assault: supplies, allies, steel
    PLAN        you pick the approach at the briefing, from what you learned
    EXECUTE     the mission itself, in the shape you chose
    CONSEQUENCE the world is different, and the difference is visible
    LEAD        the next thing to want

The camps of chapters 1, 2, 4 and 8 get the full seven beats. The rest get four
or five of them. The shape is a spine, not a template, and a chapter that runs it
twice in ten missions is worse than one that runs it once.

### The rhythm rule

No more than two combat-primary missions in a row, campaign-wide — the existing
validator already forbids three sharing a primary type, and the redesign tightens
it to a real alternation:

    ACTION → QUIET → EXPLORATION → TENSION → PREPARATION → ACTION → EMOTION → REWARD

Every chapter of ten contains at least: one recon, one preparation, one downtime
beat with no enemies at all, one mission with a real approach choice, one
consequence beat where the world visibly changed, and one companion scene.

### The marker rule

An objective marker is the last resort, not the first. In order of preference:
a person tells you; a document says; smoke, a flag, a footprint or a body shows;
a landmark implies; the map has a circle on it; and only then a marker. The three
tutorial missions and every accessibility setting keep markers on. This is the one
rule that must ship with an options toggle, because it is the one that makes the
game unplayable for somebody if it is wrong.

None of this is built. The director spawns a marker for every reach stage today
and the redesign did not change that. It is on the list in Part 5 and it is the
one item on that list that must not ship without its accessibility setting.

### The "not yet" rule

Three encounters are open, reachable, and beyond the player when first met. None
is behind a locked door and none scales. The player walks in, sees what it is,
and leaves; and forty missions later comes back with the thing that changed.

    M13  the Broken Banner at full strength, walked into before its supply line
         is burned at M11. Every approach past the front door is shut, and the
         front door is a garrison of thirty-one.
    M29  the pen at the Silent Camp, which closes around Renzo rather than
         opening. Suzu opens it at M58, and the game does not remark on it.
    M74  the outer wall with the frozen camp's guns still on it, attempted
         before M72 brings the slope down. It is a wall, and it is above you.

### The preparation rule

Preparation is never mandatory and always visible. Before every major assault the
briefing shows what the player did and did not do, in plain language, and the
mission is genuinely different in both directions. A player who prepares nothing
gets the authored hard version, which is a complete, fair, shippable mission — not
a punishment.

### The consequence rule

A villain's defeat is a change to the world, stated as world-state and visible on
the next mission that touches it, not a line on a results screen.

| Villain | Beaten at | What changes |
|---|---|---|
| Convoy Captain | 2 | the Lantern Road carries merchants again; the first supply runs open |
| Goro | 40 | the pens empty, the valley repopulates, Kiba can be rebuilt |
| Scavenger King | 13 | Ashfall's stores come back; crafting materials appear |
| Three Blades | 24 | assassination ambushes stop appearing on travel beats |
| Pale Shade | 30 | the marsh becomes crossable at night, which is what Act II needs |
| Drowned Guardian | 59 | the temple stops being hostile; the marsh route becomes permanent |
| Commander Hoshu | 79 | the inner fortress loses its discipline; patrols wander |
| Iron Guard | 78 | Kagehira's steel line breaks; Toku can unmake his own work |
| Jin Kurogane | 70 | the garrison town stands down; its people will talk |
| Kagehira | 99 | the system collapses; the water is nobody's |

### The anticipation rule

Every mission carries the sentence the player should be thinking when it ends,
written in the first person and stored on the mission itself. The validator
requires one on all hundred and forbids two in a row from being the same, which
is a weaker rule than it sounds: it is easy to satisfy and immediately obvious
when a mission cannot be given one at all. That was the useful part. Four
missions were reworked because writing their want honestly produced "I want the
next mission to start", and one of them became mission 27.

The thirteen wants the brief names — that weapon, that technique, that place,
that person, revenge, that villain, the truth, better gear, that camp again, my
village, what Jin is hiding, reach Aiko, confront Kagehira — are the categories
those hundred sentences fall into, and every one of the thirteen is used.

Ninety-nine of the hundred sentences are distinct. The one repeat is deliberate:
missions 69 and 99 both end on *I want to not become him*, thirty missions apart,
once when Jin says it to Renzo and once when Renzo is standing over Kagehira with
the sword up. If a later pass finds that repeat and removes it, the campaign has
lost its thesis.

### The rule that survives from the shipped game

Every mission still ends on the reason the next one begins. That was already the
best thing about the campaign and the redesign does not touch it.

---

## PART 5 — WHAT THIS COSTS TO BUILD

The audit of the shipped runtime is the good news of this document. Four of the
brief's demands are nearly free, because the engine already has the machinery and
the campaign never switched it on.

### Already built and unused

**The branch system.** `MissionStage` carries `point`/`pointB` and `spawn`/`spawnB`.
The director marks both routes, remembers which one the player took, and — this is
the part that matters — **every later stage in the plan spawns its B roster instead
of its A roster if the player took route B**. `StageEvent.RouteWakes` turns the
road not taken back on behind you. It works today. It is used by exactly two of a
hundred missions. Switching it on across the campaign converts "walk to the marker"
into "choose an entrance and live with it" without one line of new runtime code.

**A pre-mission choice widget on the briefing screen.** The duel screen already
cycles a modifier button that rebuilds the briefing and changes the fight's terms
and its reward. That is an approach selector with a different label on it.

**A persisted, generic, unused flag store.** `StoryFlags.SetFlag(id)` / `Flag(id)`
writes `sf_<id>` to PlayerPrefs and nothing in the shipped game calls it. Village
trust, preparation flags and camp intel all live here at zero storage cost.

**A general cinematic system** that plays an authored beat anywhere inside a
mission, suppresses gameplay while it runs, degrades gracefully when the asset is
missing, and already builds coloured stand-in rigs for named characters on demand.
Companion dialogue mid-mission is an authoring job, not an engineering one.

**An NPC that walks, talks, takes damage and can die**, with a health bar already
wired into the HUD and into the feat system. A companion who is present, speaks,
is protected and can be lost is this class plus a name.

### Not built, and honestly expensive

**A companion who fights.** There is no friendly combatant anywhere in the project
and no faction concept at all; the enemy brain's entire target model is the player,
and the attack-token pool and squad coordinator are both built around exactly one
target. This is the single expensive item in the whole redesign and it should be
scheduled deliberately rather than assumed.

### The order to build it in

| # | Item | Cost | State | Unlocks |
|---|------|------|-------|---------|
| 1 | Campaign data model: approaches, companions, flags, camps, villages | — | **done** | everything downstream |
| 2 | Generator emits `ReachAny` splits on every mission with two approaches | small | **done** | in-mission approach choice, on 43 plans |
| 3 | Camp marks authored as optional in-mission objectives | small | **done** | 19 findable objectives the marker never sends you to |
| 4 | Stage text drawn from each mission's own prose instead of six constants | small | **done** | the plans stop repeating themselves |
| 5 | Recon results persist past the mission as a camp card | small | not built | reconnaissance that accumulates |
| 6 | Preparation flags written to `sf_*` and read to thin a later roster | small | not built | "preparation must matter", mechanically |
| 7 | Briefing approach selector, copied from the duel-terms widget | medium | not built | the planning phase proper |
| 8 | Companion as a present, speaking, losable NPC (escort-class, not a fighter) | medium | not built | five of six companions, Daigo's gate |
| 9 | Village trust: a counter per village, spent on supply and militia | medium | not built | the help-village-village-helps-you loop |
| 10 | Markers demoted behind people, documents and landmarks, with an accessibility toggle | medium | not built | the marker rule, which must not ship without the toggle |
| 11 | A friendly combat brain | large | not built | Tsuru's covering fire, allied assaults, militia |

Items 1 to 4 shipped with this change and are what makes the redesigned hundred
play differently from the shipped hundred today. Items 5 and 6 are the two that
turn the campaign's seventy-nine remembered facts from authored intent into
mechanical effect, and they are small; they are the next thing to build. Item 7
is what turns an in-mission branch into the planning phase the brief describes.

Item 11 is the only one that needs a new system rather than a switch being
thrown, and the campaign is written so that it degrades honestly without it:
every allied approach has a version where the allies open a door, draw a patrol
and hold a line off screen, which the existing mutiny, reinforcement and
route-wakes events can already express.

Until items 5 and 6 land, the flag graph is authored, validated and readable in
`CAMPAIGN.md`, and it is checked for causality on every build — but no mission is
yet mechanically easier because of one. That is the honest state and it should
not be described any other way in a store listing or a patch note.

---

## PART 6 — THE SHAPE OF THE NEW HUNDRED

The mission-by-mission detail is in `CAMPAIGN.md`, generated from the story table and the design table together. This is the shape that comes out, and the row a reviewer should disbelieve first is the last one: the engine's two-route branch was always there.

| | Shipped | Redesigned |
|---|---|---|
| Missions offering a choice at the door | 2 | 44 |
| Missions with somebody other than Renzo in them | 5, all unnamed | 67, all named |
| Named allies | 1, absent for 79 missions | 7 |
| Reconnaissance missions | 0 | 5 |
| Optional preparation whose result is read later | 0 | 4 |
| Missions with no combat in them | 4, three of which are cutscenes | 5 |
| Facts the world remembers between missions | 0 | 79 |
| Missions with a stated world consequence | 1 | 56 |
| Camps that are places across several missions | 0 | 9 |
| Villages that remember what you did | 0 | 5 |
| Generated plans using the engine's two-route branch | 2 | 43 |
| Optional objectives inside a camp, findable but not required | 0 | 19 |

Stage-goal totals across the hundred plans move accordingly: `ReachAny` from 2 to 43, `FreePrisoners` from 5 to 12, `Investigate` from 126 to 143. Eighty-three fight stages now field a different roster depending on which entrance the player took, which is the mechanical definition of the choice mattering.

### Chapter by chapter

| Ch | Name | What the ten missions are | Choices | With companions |
|---|---|---|---|---|
| 1 | ASHES OF YORUNE | Discovery ×3 · Story ×3 · Assault ×2 · Recon · Consequence | 2 | 0 |
| 2 | THE LANTERN NETWORK | Story ×3 · Assault ×2 · Personal ×2 · Prepare · Recon · Consequence | 9 | 9 |
| 3 | THE SILENT FOREST | Story ×4 · Assault ×2 · Recon · Discovery · Downtime · Consequence | 4 | 8 |
| 4 | GORO'S TERRITORY | Assault ×3 · Defend ×2 · Story ×2 · Recon · Personal · Consequence | 7 | 10 |
| 5 | INTO THE MARSH | Story ×3 · Personal ×2 · Recon · Prepare · Defend · Discovery · Consequence | 5 | 7 |
| 6 | THE DROWNED TEMPLE | Memory ×5 · Discovery ×2 · Assault · Story · Downtime | 2 | 5 |
| 7 | KUROGANE | Story ×4 · Discovery ×2 · Personal · Assault · Downtime · Consequence | 2 | 7 |
| 8 | THE IRON FORTRESS | Personal ×3 · Story ×2 · Assault ×2 · Prepare · Discovery · Consequence | 6 | 9 |
| 9 | THE BLACK SEAL | Story ×3 · Defend ×2 · Downtime · Assault · Memory · Prepare · Consequence | 5 | 7 |
| 10 | THE SERPENT'S END | Story ×5 · Personal ×2 · Discovery · Consequence · Downtime | 2 | 5 |

Two shapes in that table are deliberate and should not be "fixed" by a later
pass. Chapter 1 has no companions in it at all: Renzo is alone for eleven
missions so that the arrival of a scout at 12 is an event rather than a menu
entry. Chapter 10 sheds them one mission at a time until missions 95 to 99 have
nobody in them at all, so that the word *alone* costs what eighty missions of
company made it worth — and then puts all six on mission 100, where the only
question left is which of them is standing.

### The three validators

`Emberline/Rebuild And Check Campaign` authors the hundred plans and then runs
all three in one batch session:

- **Check Campaign** — the story rules. Ten fields on every mission, chapter and
  act structure, the boss cadence, the gameplay-type distribution against the
  brief's targets, Renzo's state and the Seal's stage by band, region ordering.
- **Check Mission Design** — the pacing rules, on the generated plans. No mission
  opens on a fight or ends on one, no more than two fights run back to back
  outside a late boss, every mission has a breather past its midpoint, and no two
  consecutive missions share a shape or a roster.
- **Check Campaign Design** — the redesign's own rules, new here. The flag graph
  is causal and nothing is read before it can be set. An Assault without a choice
  at the door is a failure. A companion cannot appear before they join, and Daigo
  cannot be on a stealth approach because Daigo cannot be quiet. Every chapter
  lets the player look before acting, changes the world at least once, and has
  somebody in it other than Renzo. Every companion gets two personal missions.
  Every camp's mission list matches its assignments and carries at least four
  things to find. Every village is returned to. No five missions in a row pass
  without a choice, a companion or a turn. And each of the ten villains changes
  something when they fall.
