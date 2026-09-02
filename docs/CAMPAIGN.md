# Emberline — the hundred-mission campaign

Generated from `Assets/Scripts/Campaign/CampaignTable.cs` by `Emberline/Write Campaign Doc`. Edit the table, not this file.

## Structure

100 missions · 10 chapters · 3 acts. Every mission carries the ten fields the design rule requires; the last of them, the **next-mission reason**, is shown on the results screen and names the mission it leads to.

### ACT I — THE RETURN
- **Chapter 1 — ASHES OF YORUNE** (1–10) · Return, mystery, first clues · *Ruins*
- **Chapter 2 — THE LANTERN NETWORK** (11–20) · Kagehira's larger operation · *Villages*
- **Chapter 3 — THE SILENT FOREST** (21–30) · Assassins, tracking and pressure · *Forest*

### ACT II — THE HUNT
- **Chapter 4 — GORO'S TERRITORY** (31–40) · War, prisoners and Goro · *Mountains*
- **Chapter 5 — INTO THE MARSH** (41–50) · Isolation and the Black Seal · *Marsh*
- **Chapter 6 — THE DROWNED TEMPLE** (51–60) · Renzo's family history · *Temples*
- **Chapter 7 — KUROGANE** (61–70) · Jin and Renzo's rivalry · *Villages*

### ACT III — THE END
- **Chapter 8 — THE IRON FORTRESS** (71–80) · The final approach · *Snow*
- **Chapter 9 — THE BLACK SEAL** (81–90) · Truth and payoff · *Stronghold*
- **Chapter 10 — THE SERPENT'S END** (91–100) · War, revenge and choice · *Seal*

## Gameplay distribution

The first type on a mission is its primary and is what the campaign validator holds against the brief's targets; the overlap column counts every mission that carries the type at all, since the brief allows categories to overlap.

| Type | Brief target | Missions carrying it | As primary |
|---|---|---|---|
| Combat | ~20 | 17 | 13 |
| Stealth | ~15 | 16 | 9 |
| Investigation | ~10 | 17 | 13 |
| Rescue | ~10 | 10 | 5 |
| Defense | ~8 | 10 | 5 |
| Escort | ~7 | 5 | 5 |
| Chase | ~7 | 9 | 4 |
| Exploration | ~8 | 19 | 8 |
| Survival | ~5 | 12 | 7 |
| Boss | ~10 | 13 | 12 |
| Sabotage | — | 6 | 5 |
| Memory | — | 6 | 2 |
| Conversation | — | 9 | 5 |
| Endure | — | 7 | 7 |

## Boss cadence

- **5 — GORO'S TOLL**: Chief
- **30 — PALE SHADE**: paleshade
- **40 — GORO'S END**: Chief
- **70 — KUROGANE**: Jin
- **99 — KAGACHI**: Kagachi

Named foes on common bodies (no new model): Convoy Captain (2), Scavenger King (13), the Three Blades (24), Pale Shade (21, 30), Drowned Guardian (59), Commander Hoshu (66, 79), Iron Guard (74, 78, 94). Jin and Kagehira appear as unbeatable foes (Endure) before their boss missions.

## The journey

Two arena geometries exist (rooftop deck, marsh). Every region is carried on them by lighting theme, weather, visibility and set dressing; the real geometry for forest, mountain, snow, temple and fortress is specified in `docs/ASSET_SPECIFICATIONS.md` and is **not** present. Mission by mission:

| Missions | Region | Arena | Theme | Weather |
|---|---|---|---|---|
| 1 | Ruins | Rooftop | BurningVillage | clear |
| 2–3 | Ruins | Rooftop | Village | night/rain |
| 4–5 | Mountains | Rooftop | Castle | clear |
| 6 | Forest | Rooftop | Village | clear |
| 7 | Forest | Rooftop | Forest | clear |
| 8 | Forest | Rooftop | Bamboo | clear |
| 9 | Marsh | Marsh | Graveyard | fog |
| 10 | Temples | Marsh | Temple | clear |
| 11–12 | Villages | Rooftop | Village | night |
| 13 | Villages | Rooftop | BurningVillage | clear |
| 14 | Villages | Rooftop | Village | clear |
| 15 | Forest | Rooftop | Forest | clear |
| 16 | Forest | Rooftop | Castle | clear |
| 17 | Forest | Rooftop | Forest | clear |
| 18 | Villages | Rooftop | Village | night |
| 19–20 | Villages | Rooftop | Castle | night |
| 21 | Forest | Rooftop | Forest | night/fog |
| 22 | Forest | Rooftop | Bamboo | clear |
| 23 | Snow | Rooftop | Mountain | snow |
| 24 | Forest | Rooftop | Forest | clear |
| 25 | Forest | Rooftop | Bamboo | night |
| 26 | Forest | Rooftop | Forest | fog |
| 27 | Forest | Rooftop | Bamboo | clear |
| 28–29 | Forest | Rooftop | Forest | clear |
| 30 | Forest | Rooftop | Graveyard | night/fog |
| 31–32 | Mountains | Rooftop | Castle | clear |
| 33–34 | Mountains | Rooftop | Fortress | clear |
| 35 | Mountains | Rooftop | Castle | clear |
| 36 | Mountains | Rooftop | Village | clear |
| 37 | Villages | Rooftop | BurningVillage | clear |
| 38 | Mountains | Rooftop | Mountain | clear |
| 39–40 | Mountains | Rooftop | Fortress | night |
| 41–45 | Marsh | Marsh | Graveyard | fog |
| 46 | Marsh | Marsh | Village | clear |
| 47 | Marsh | Marsh | Graveyard | fog |
| 48–51 | Temples | Marsh | Temple | night |
| 52 | Villages | Rooftop | VillageDawn | clear |
| 53–56 | Villages | Rooftop | BurningVillage | clear |
| 57–60 | Temples | Marsh | Temple | night |
| 61–62 | Villages | Rooftop | RainyBattlefield | night/rain |
| 63 | Villages | Rooftop | Castle | night |
| 64 | Villages | Rooftop | Village | clear |
| 65 | Villages | Rooftop | Castle | clear |
| 66–67 | Villages | Rooftop | Village | night |
| 68 | Villages | Rooftop | RainyBattlefield | rain |
| 69 | Villages | Rooftop | Castle | night |
| 70 | Villages | Rooftop | RainyBattlefield | night/rain |
| 71–73 | Snow | Rooftop | Mountain | snow/night/fog |
| 74–79 | Fortresses | Rooftop | Fortress | snow/night |
| 80–82 | Stronghold | Rooftop | Castle | night |
| 83–84 | Stronghold | Rooftop | Fortress | night |
| 85–87 | Seal | Rooftop | Temple | night |
| 88–89 | Seal | Rooftop | Fortress | night |
| 90 | Seal | Rooftop | Temple | night |
| 91 | Seal | Rooftop | BurningVillage | night |
| 92–93 | Snow | Rooftop | Mountain | snow/fog |
| 94 | Snow | Rooftop | Fortress | snow |
| 95–96 | Seal | Rooftop | Mountain | night/fog |
| 97–98 | Seal | Rooftop | Temple | night |
| 99 | Seal | Marsh | Temple | night |
| 100 | Dawn | Rooftop | VillageDawn | clear |

## Renzo, and the Seal

| Missions | Renzo | The Black Seal |
|---|---|---|
| 1–20 | Confused | Rumour |
| 21–40 | Angry | Hunted |
| 41–50 | Obsessed | Evidence |
| 51–60 | Obsessed | Protected |
| 61–70 | Consumed | Witness |
| 71–80 | Consumed | Assembled |
| 81–90 | Changed | Understood |
| 91–99 | Changed | Opened |
| 100–100 | Changed | Chosen |

## The missions

### Chapter 1 — ASHES OF YORUNE

*Return, mystery, first clues.*

#### 01 — FIRST BLOOD

- **Story purpose:** Renzo comes home to a village that no longer exists, and learns the people who burned it never left.
- **Primary objective:** Search the ruins of Yorune for any sign of who is still here.
- **Gameplay type:** Stealth + Exploration
- **Unique event:** The first enemy has not noticed you: the game teaches the silent kill by giving you one for free, then takes the option away.
- **Story discovery:** Kagehira's forces are still operating around Yorune. This was not a raid that ended.
- **Climax:** A masked assassin drops from the burned watchtower and attacks Renzo in the open.
- **Ending:** In the assassin's coat: a map with one road marked in red. LANTERN ROAD.
- **Next mission reason:** The map is the only lead there is. Renzo follows it before whoever drew it comes looking.
- *Staging:* Ruins, rooftop arena, BurningVillage · enemies: Bandit, Bandit, Assassin · bespoke plan `S01_FirstBlood`

#### 02 — THE LANTERN ROAD

- **Story purpose:** The road on the map carries an enemy convoy, and the convoy carries people.
- **Primary objective:** Shadow the convoy along the Lantern Road without being seen, and free whoever is in the wagons.
- **Gameplay type:** Rescue + Stealth + Chase
- **Unique event:** A moving patrol: the guards walk a route rather than stand a post, and the wagons stop where the lanterns are.
- **Story discovery:** The prisoners were being taken north, toward the marsh. Nobody has said why.
- **Climax:** The Convoy Captain turns back for his cargo and meets Renzo on the road.
- **Ending:** A document in the captain's saddlebag names something called the Black Seal.
- **Next mission reason:** Kagehira is spending soldiers and wagons on a seal. Renzo needs to know why, and the camp the convoy came from will.
- *Staging:* Ruins, rooftop arena, Village, night · enemies: Bandit, PikeGuard, Ranged · named foe `convoycaptain`

#### 03 — EYES IN THE DARK

- **Story purpose:** The camp the convoy came from is where the orders come from.
- **Primary objective:** Get into the enemy camp, read what they are looking for, and get out.
- **Gameplay type:** Stealth + Investigation
- **Unique event:** Vision is short and the rain covers you; archers on the roofs are the threat, not the men on the ground.
- **Story discovery:** Someone named Goro is commanding the search. Every order carries his mark.
- **Climax:** The alarm goes up. The way out is a run through a camp that is now awake.
- **Ending:** The last order on the table: Goro is closing the northern pass tonight.
- **Next mission reason:** If the pass closes, the prisoners' trail closes with it. Renzo goes north before Goro can shut the door.
- *Staging:* Ruins, rooftop arena, Village, night, rain · enemies: Assassin, Ranged, Ranged, Bandit · bespoke plan `S03_EyesInTheDark`

#### 04 — THE BROKEN GATE

- **Story purpose:** The road north runs through a checkpoint that was built to stop exactly one man.
- **Primary objective:** Reach the northern checkpoint and break through the gate.
- **Gameplay type:** Combat
- **Unique event:** The soldiers say Renzo's family name before he has said a word. They were told to expect a Kurogawa.
- **Story discovery:** Kagehira's soldiers know the name Kurogawa. They have known it for years.
- **Climax:** An elite guard holds the gate alone and does not need help.
- **Ending:** The gate falls. On the far side, a man in a toll-captain's armour is waiting and unhurried. Goro.
- **Next mission reason:** Goro has come himself. There is no road around him, only through.
- *Staging:* Mountains, rooftop arena, Castle · enemies: PikeGuard, PikeGuard, RaiderAxe, EliteWarrior

#### 05 — GORO'S TOLL

- **Story purpose:** The first named enemy, and the first proof that the search has a purpose.
- **Primary objective:** Defeat Goro.
- **Gameplay type:** Boss + Rescue
- **Unique event:** A boss who talks while he fights: every phase break, Goro says one more thing he should not.
- **Story discovery:** Goro says Renzo's sister may have survived the night Yorune burned.
- **Climax:** Goro, in a barricaded post, with nowhere for either of them to go.
- **Ending:** The prisoner ledger at the post carries a name Renzo has not heard spoken in ten years. AIKO KUROGAWA.
- **Next mission reason:** Aiko is alive, or was. The trail the prisoners took is the only way to find out which.
- *Staging:* Mountains, rooftop arena, Castle · enemies: PikeGuard, RaiderAxe, Ranged, Bandit, Bandit · boss Chief · bespoke plan `S04_GorosToll`

#### 06 — THE MISSING GIRL

- **Story purpose:** The first mission with no enemies for its opening minute: Renzo is reading the ground, not clearing it.
- **Primary objective:** Find and follow the prisoner trail north from Goro's post.
- **Gameplay type:** Investigation + Chase
- **Unique event:** Tracking: drag marks, a dropped lantern, a scrap of cloth, each one further from the road than the last.
- **Story discovery:** Aiko was transported north with a smaller group, split off from the rest.
- **Climax:** An enemy patrol finds Renzo on the trail and the hunt reverses.
- **Ending:** The trail leaves the road and enters the black pines.
- **Next mission reason:** The forest is where the trail goes. Renzo follows it in.
- *Staging:* Forest, rooftop arena, Village · enemies: Assassin, RogueNinja, Ranged · bespoke plan `S05_SerpentsTrail`

#### 07 — BLACK PINES

- **Story purpose:** The forest belongs to the rogues, and it is thick enough to hide either side.
- **Primary objective:** Cross the black pines without being caught.
- **Gameplay type:** Stealth
- **Unique event:** Dense cover cuts both ways: enemies lose you at twelve paces, and you lose them at the same.
- **Story discovery:** Enemy camps in the forest are not looking for Renzo. They are searching for the Black Seal.
- **Climax:** An ambush in the thickest of the pines, from three sides.
- **Ending:** Cut into a trunk at the forest's edge: an old symbol. Renzo knows it. It was on his father's blade.
- **Next mission reason:** A Yorune mark in a forest Yorune never reached. Renzo needs to know who cut it and when.
- *Staging:* Forest, rooftop arena, Forest · enemies: RogueNinja, RogueNinja, Ranged, Ranged, Bomber

#### 08 — FATHER'S MARK

- **Story purpose:** The first time the story turns from Kagehira to Renzo's own family.
- **Primary objective:** Follow the old marks and find out what Renzo's father was doing here.
- **Gameplay type:** Exploration + Investigation
- **Unique event:** Environmental clues only: no document tells you anything, the marks do.
- **Story discovery:** Renzo's father knew about the Black Seal, years before Yorune burned.
- **Climax:** A samurai patrol, disciplined and patient, on the one path the marks lead down.
- **Ending:** The marks end at a cairn. Under it, a route drawn by a hand Renzo recognises, pointing into the marsh.
- **Next mission reason:** His father walked into the marsh on purpose. Renzo takes the same route.
- *Staging:* Forest, rooftop arena, Bamboo · enemies: Samurai, Samurai, Ranged

#### 09 — INTO THE FOG

- **Story purpose:** The marsh is where the story stops being about soldiers.
- **Primary objective:** Cross the marsh entrance and survive what lives in it.
- **Gameplay type:** Survival
- **Unique event:** You cannot see. Standing still and listening is a mechanic, not a mood.
- **Story discovery:** Enemy soldiers in the marsh talk about an old temple, and about not going near it after dark.
- **Climax:** An attack on the enemy camp at the edge of the fog, with things that are not soldiers joining in.
- **Ending:** Through the fog, lanterns. Dozens of them. A temple.
- **Next mission reason:** The temple is what the soldiers fear and what his father's route pointed to. Renzo goes to it.
- *Staging:* Marsh, marsh arena, Graveyard, fog · enemies: Assassin, Ranged, Bomber, Shade, Shade · bespoke plan `S06_IntoTheReeds`

#### 10 — THE OLD TEMPLE

- **Story purpose:** The chapter's payoff: the Seal is not Kagehira's. It is Renzo's.
- **Primary objective:** Enter the ruined temple and find what Kagehira's men are afraid of.
- **Gameplay type:** Combat + Exploration
- **Unique event:** The temple comes down around the fight: the last stage is an escape through a collapsing hall.
- **Story discovery:** The Black Seal is bound to Renzo's family. The temple carvings show a Kurogawa holding it.
- **Climax:** Elite warriors in the temple's heart, then the ceiling.
- **Ending:** Renzo walks out of the dust with a fragment of the seal in his hand.
- **Next mission reason:** One fragment means there are others, and Kagehira is collecting. Renzo follows his supply lines to find where.
- *Staging:* Temples, marsh arena, Temple · enemies: Samurai, EliteWarrior, EliteWarrior, Shade

### Chapter 2 — THE LANTERN NETWORK

*Kagehira's larger operation.*

#### 11 — THE SUPPLY ROUTE

- **Story purpose:** The first look at the size of what Kagehira is building.
- **Primary objective:** Track the enemy supply route and burn the wagon at the end of it.
- **Gameplay type:** Sabotage + Stealth
- **Unique event:** Sabotage: the fire takes time to set, and the guards come back while it does.
- **Story discovery:** Kagehira is not hunting a seal with a raiding party. He is building an army.
- **Climax:** The supply wagon burns and its escort arrives to find it burning.
- **Ending:** Three soldiers survive and run. Renzo lets them.
- **Next mission reason:** Running men go home. Renzo follows them to wherever that is.
- *Staging:* Villages, rooftop arena, Village · enemies: Bandit, Bandit, Ranged, PikeGuard

#### 12 — SILENT CARGO

- **Story purpose:** A shipment moving at night has something to hide.
- **Primary objective:** Intercept the night shipment without waking the escort.
- **Gameplay type:** Stealth
- **Unique event:** A silent elimination challenge: every guard can be taken unseen, and every alarm costs the objective.
- **Story discovery:** The cargo is weapons, from four different provinces. Kagehira is buying from everyone.
- **Climax:** The last guard is awake, armed, and standing on the crate.
- **Ending:** A bill of lading with a supplier's name and a village Renzo has never heard of.
- **Next mission reason:** Somebody is selling Kagehira steel by the wagon. The village on the bill is the supplier.
- *Staging:* Villages, rooftop arena, Village, night · enemies: Assassin, Assassin, Bandit, Ranged

#### 13 — THE BROKEN VILLAGE

- **Story purpose:** Yorune was not the only village that burned.
- **Primary objective:** Search the destroyed settlement for what happened to it.
- **Gameplay type:** Investigation + Exploration
- **Unique event:** The village tells the story without a line of dialogue: the homes, the carts, the notices.
- **Story discovery:** This village was searching for the Seal too. Kagehira burned it for the same reason.
- **Climax:** The Scavenger King, who has made the ruin his own.
- **Ending:** Under the elder's floor: a second map, older than the first, with the marsh temple marked.
- **Next mission reason:** The elder's house held more than a map. Someone who lived there is still alive.
- *Staging:* Villages, rooftop arena, BurningVillage · enemies: Bandit, Bandit, RaiderAxe, Ranged · named foe `raiderleader`

#### 14 — THE SURVIVOR

- **Story purpose:** The first living person who remembers Aiko.
- **Primary objective:** Protect the last survivor of the village until the road is clear.
- **Gameplay type:** Escort + Defense
- **Unique event:** Waves come from every side while she walks; the fight moves with her.
- **Story discovery:** The survivor remembers Aiko. She was alive after the fire, and she was not alone.
- **Climax:** An enemy elite arrives to finish the village.
- **Ending:** Safe, the survivor tells Renzo about a road that does not appear on any map.
- **Next mission reason:** A hidden road is how the enemy moves unseen. Renzo takes it.
- *Staging:* Villages, rooftop arena, Village · enemies: Bandit, PikeGuard, Ranged, Bandit, EliteWarrior

#### 15 — HIDDEN ROAD

- **Story purpose:** The secret route is watched, which is how Renzo knows it matters.
- **Primary objective:** Follow the hidden road without being seen.
- **Gameplay type:** Stealth
- **Unique event:** Traversal under watch: the archers are above the road, not on it.
- **Story discovery:** The route ends at an enemy watchtower that sees the whole valley.
- **Climax:** Getting into the tower's shadow without a shot being fired.
- **Ending:** The tower is lit. Someone is on the top of it.
- **Next mission reason:** The tower watches every road Renzo could take. It has to come down.
- *Staging:* Forest, rooftop arena, Forest · enemies: Assassin, Assassin, Ranged, Ranged

#### 16 — WATCHFIRE

- **Story purpose:** Taking the enemy's eyes.
- **Primary objective:** Capture the watchtower and hold it.
- **Gameplay type:** Combat + Defense
- **Unique event:** Vertical assault, then a reversal: once the tower is yours, you are the one being attacked.
- **Story discovery:** The maps in the tower show three enemy territories, and a fourth marked only with a serpent.
- **Climax:** Holding the tower against its own reinforcements.
- **Ending:** The signal fire is Renzo's now. He lets it burn, so they will come to him.
- **Next mission reason:** Three territories, and the marsh route between them. A messenger will know which one matters.
- *Staging:* Forest, rooftop arena, Castle · enemies: Ranged, Ranged, Samurai, PikeGuard, Bandit

#### 17 — THE MESSENGER

- **Story purpose:** The enemy's words travel on foot.
- **Primary objective:** Intercept the messenger before he reaches the border.
- **Gameplay type:** Chase
- **Unique event:** A moving duel: the target runs, fights, runs again.
- **Story discovery:** The messenger carries orders sealed by Kagehira himself.
- **Climax:** The last stretch, where the messenger stops running and turns.
- **Ending:** The orders are in a cipher Renzo does not know. Yet.
- **Next mission reason:** A cipher needs a key, and the messenger came from a post that has one.
- *Staging:* Forest, rooftop arena, Forest · enemies: Bandit, Bandit, RogueNinja, Ranged

#### 18 — DEAD LETTER

- **Story purpose:** What Kagehira wants, in his own words.
- **Primary objective:** Get into the relay post and decode the orders.
- **Gameplay type:** Investigation + Stealth
- **Unique event:** The investigation is the mission: three pieces of the key, hidden in a post full of assassins.
- **Story discovery:** Kagehira is looking for someone called 'the daughter.'
- **Climax:** Assassins between Renzo and the last piece of the key.
- **Ending:** 'Find the daughter. She knows where he hid it.' There is only one daughter this could mean.
- **Next mission reason:** If Kagehira has been searching for Aiko, his prisoner records will say where he looked.
- *Staging:* Villages, rooftop arena, Village, night · enemies: Assassin, Assassin, Assassin, Ranged

#### 19 — THE DAUGHTER

- **Story purpose:** Confirmation.
- **Primary objective:** Infiltrate the records house and find the prisoner rolls.
- **Gameplay type:** Stealth + Investigation
- **Unique event:** The alarm is inevitable here: the records are in the one room you cannot leave quietly.
- **Story discovery:** Confirmed: Aiko Kurogawa was imprisoned, alive, six years ago.
- **Climax:** The escape after the alarm, with the roll under Renzo's coat.
- **Ending:** The roll says where she was held. It does not say where she is.
- **Next mission reason:** The enemy's communication towers pass every transfer order. The second tower has hers.
- *Staging:* Villages, rooftop arena, Castle · enemies: PikeGuard, PikeGuard, Ranged, Bandit, Assassin

#### 20 — THE SECOND LANTERN

- **Story purpose:** The enemy's second signal tower, and the first time Kagehira hears Renzo's name.
- **Primary objective:** Destroy the enemy's second communication tower.
- **Gameplay type:** Sabotage + Defense
- **Unique event:** Two towers, two routes: whichever you light first, the other is ready for you.
- **Story discovery:** The tower's log names Aiko's transfer to the silent forest.
- **Climax:** Tower destruction, then the elite squad sent to keep it standing.
- **Ending:** As the tower burns, a rider brings its last message: 'KUROGAWA IS COMING.' They know.
- **Next mission reason:** Kagehira knows Renzo's name and where he is. The forest is between them.
- *Staging:* Villages, rooftop arena, Castle, night · enemies: Ranged, Ranged, PikeGuard, Assassin, RogueNinja, EliteWarrior · bespoke plan `S08_TwinLanterns`

### Chapter 3 — THE SILENT FOREST

*Assassins, tracking and pressure.*

#### 21 — THE HUNTER

- **Story purpose:** The chapter opens with Renzo as prey.
- **Primary objective:** Survive the thing hunting you and get out of its ground.
- **Gameplay type:** Endure + Stealth
- **Unique event:** A foe you cannot beat yet: something pale in the trees that does not die when cut, and lets you live when the clock runs out.
- **Story discovery:** The hunter is not Kagehira's. It was here before him.
- **Climax:** The hunter circles closer with every minute, and the forest goes silent around it.
- **Ending:** It withdraws. It was measuring him.
- **Next mission reason:** Something in the forest is hunting Renzo for its own reasons. The only way through is to leave no trail.
- *Staging:* Forest, rooftop arena, Forest, night, fog · enemies: Assassin, RogueNinja, Shade · named foe `paleshade`

#### 22 — NO FOOTPRINTS

- **Story purpose:** Pure stealth: the mission the alarm ends.
- **Primary objective:** Infiltrate the forest camp and leave without a footprint.
- **Gameplay type:** Stealth
- **Unique event:** No combat is required at all. Every kill is optional and every one of them is loud.
- **Story discovery:** The camp holds transfer orders with Aiko's name, three weeks old.
- **Climax:** The last patrol crosses the only exit as the clock runs down.
- **Ending:** Renzo leaves the camp exactly as he found it, minus one document.
- **Next mission reason:** The orders route Aiko through a clearing to the north. Somebody died there.
- *Staging:* Forest, rooftop arena, Bamboo · enemies: Assassin, Ranged, Ranged, RogueNinja

#### 23 — BLOOD ON SNOW

- **Story purpose:** An assassination Renzo did not commit.
- **Primary objective:** Investigate the killing in the clearing.
- **Gameplay type:** Investigation
- **Unique event:** First snow: tracks stay, and so does blood. Used once, and it is the mission.
- **Story discovery:** The dead man was one of Kagehira's own officers, killed by his own side.
- **Climax:** The killers return to clean the scene and find Renzo in it.
- **Ending:** The officer's last letter: he had refused to take a child north.
- **Next mission reason:** Three assassins did this, and they are between Renzo and the road north.
- *Staging:* Snow, rooftop arena, Mountain, snow · enemies: Assassin, Assassin, RaiderAxe, PikeGuard

#### 24 — THE THREE BLADES

- **Story purpose:** Three elite assassins who fight as one.
- **Primary objective:** Defeat the Three Blades.
- **Gameplay type:** Boss + Combat
- **Unique event:** A triple duel: three assassins share one set of tactics, and the last one changes hers.
- **Story discovery:** The Blades were sent for Renzo specifically. Someone above Goro is paying attention.
- **Climax:** The last Blade, alone, faster than the other two together.
- **Ending:** A silk cord from the last Blade's wrist: the mark of the forest camp's master.
- **Next mission reason:** The camp that sent them is the camp that has the rest of the orders.
- *Staging:* Forest, rooftop arena, Forest · enemies: Assassin, Assassin, Assassin, RogueNinja · named foe `threeblades`

#### 25 — THE SILENT CAMP

- **Story purpose:** The perfect infiltration.
- **Primary objective:** Destroy the enemy camp without triggering the alarm.
- **Gameplay type:** Sabotage + Stealth
- **Unique event:** Sabotage under silence: the fire must be set in three places while the camp sleeps.
- **Story discovery:** The camp's commander was hunting the Black Seal, not Renzo. Renzo is a complication.
- **Climax:** The third fire, in the commander's own tent.
- **Ending:** The camp burns without a bell rung. Renzo watches it from the trees.
- **Next mission reason:** The commander's route led into a fog-bound forest path. Renzo takes it before dawn.
- *Staging:* Forest, rooftop arena, Bamboo, night · enemies: Assassin, RogueNinja, Ranged, Bandit, Bandit

#### 26 — THE BLIND PATH

- **Story purpose:** The forest at its worst.
- **Primary objective:** Navigate the fog-covered path to the far side of the forest.
- **Gameplay type:** Survival
- **Unique event:** Fog and listening again, deeper: the enemies here are silent until they are close.
- **Story discovery:** The forest path is marked with red thread, tied to branches at a child's height.
- **Climax:** Everything that was following in the fog arrives at once.
- **Ending:** The thread runs out at a clearing. Someone tied it here on purpose.
- **Next mission reason:** Red thread. Aiko's bracelet was red thread. Renzo follows it.
- *Staging:* Forest, rooftop arena, Forest, fog · enemies: Shade, Shade, Assassin, RogueNinja

#### 27 — THE RED THREAD

- **Story purpose:** The first thing of Aiko's that Renzo touches in ten years.
- **Primary objective:** Follow the red thread to its end.
- **Gameplay type:** Investigation
- **Unique event:** A quiet mission almost to the end: the discovery is the point, the fight is the cost.
- **Story discovery:** The thread ends at a bead from Aiko's bracelet. She was here, and she left it on purpose.
- **Climax:** The men who took her came back for the bead.
- **Ending:** Renzo ties the bead into his own wrist.
- **Next mission reason:** Aiko was leaving a trail. Where it points next is where she was taken.
- *Staging:* Forest, rooftop arena, Bamboo · enemies: Bandit, Assassin, Ranged

#### 28 — THE DECOY

- **Story purpose:** The enemy knows what Renzo is looking for now.
- **Primary objective:** Reach the reported location and rescue the girl.
- **Gameplay type:** Rescue
- **Unique event:** The rescue is a trap: the prisoner is not Aiko, and the pen is bait.
- **Story discovery:** The enemy staged Aiko's location. They are using her to catch him.
- **Climax:** The trap springs: the pen's walls were the ambush.
- **Ending:** The girl in the pen has never heard of Aiko. Renzo frees her anyway.
- **Next mission reason:** Whoever laid the trap will lay another. Renzo turns the hunt around.
- *Staging:* Forest, rooftop arena, Forest · enemies: Assassin, Assassin, Ranged, RogueNinja, PikeGuard

#### 29 — THE HUNTER'S TRAP

- **Story purpose:** Surrounded.
- **Primary objective:** Survive until there is a way out.
- **Gameplay type:** Survival
- **Unique event:** The arena closes: every wave comes from a different side and the exits shut one by one.
- **Story discovery:** The assassins answer to something they call the Pale Shade.
- **Climax:** The last exit, and the thing standing in it.
- **Ending:** The forest goes quiet. It has arrived.
- **Next mission reason:** There is no leaving the forest without going through what owns it.
- *Staging:* Forest, rooftop arena, Forest · enemies: Assassin, RogueNinja, Shade, Shade, Bomber, PikeGuard

#### 30 — PALE SHADE

- **Story purpose:** The chapter boss, and the thing that hunted Renzo in mission 21.
- **Primary objective:** Defeat the Pale Shade.
- **Gameplay type:** Boss
- **Unique event:** A boss that is not wholly there: it dissolves and reforms, and the fight is about where it will be, not where it is.
- **Story discovery:** The Pale Shade knows where Aiko went. It is what the marsh sends when it wants to know things.
- **Climax:** The Shade at its full form, in the dark.
- **Ending:** Dying, it tells him: 'She was moved. To the toll-captain's country.'
- **Next mission reason:** Aiko was transferred to Goro's territory. Renzo goes to war with Goro.
- *Staging:* Forest, rooftop arena, Graveyard, night, fog · enemies: Shade, Shade, Assassin · named foe `paleshade`

### Chapter 4 — GORO'S TERRITORY

*War, prisoners and Goro.*

#### 31 — THE FORTRESS ROAD

- **Story purpose:** Goro's country announces itself.
- **Primary objective:** Approach Goro's territory along the fortress road.
- **Gameplay type:** Exploration
- **Unique event:** Banners: every hundred paces, a serpent. The road is a statement.
- **Story discovery:** Goro has fortified the whole valley. This is not a toll post any more.
- **Climax:** The road's first garrison, which does not intend to let anyone pass.
- **Ending:** From the ridge: prisoner wagons, moving in a line toward the fortress.
- **Next mission reason:** The wagons carry people. Renzo goes down to them.
- *Staging:* Mountains, rooftop arena, Castle · enemies: PikeGuard, PikeGuard, RaiderAxe, Ranged

#### 32 — PRISONER WAGONS

- **Story purpose:** The first rescue that is about more than one person.
- **Primary objective:** Rescue the villagers from the wagons before they reach the fortress.
- **Gameplay type:** Rescue + Chase
- **Unique event:** The wagons are moving: free who you can while the escort fights and the drivers whip on.
- **Story discovery:** The prisoners are from a dozen villages. Goro has been emptying the valley.
- **Climax:** The last wagon, the escort's captain, and a road that is running out.
- **Ending:** The freed villagers speak of the camps: not prisons, pens.
- **Next mission reason:** There are camps. Renzo has the location of the nearest.
- *Staging:* Mountains, rooftop arena, Castle · enemies: Bandit, PikeGuard, Ranged, RaiderAxe, Assassin

#### 33 — BROKEN CHAINS

- **Story purpose:** Destroy the machine, not just its output.
- **Primary objective:** Free the prisoner camp and burn it.
- **Gameplay type:** Sabotage + Rescue
- **Unique event:** Two pens, two guard rotations: the second wakes when the first goes quiet.
- **Story discovery:** A camp record lists Aiko's transfer out, two months ago, to 'the execution ground.'
- **Climax:** The camp commander, an axe raider, in the burning yard.
- **Ending:** The camp is ash and the prisoners are gone into the hills.
- **Next mission reason:** 'Execution ground' is not a place Renzo can leave for tomorrow.
- *Staging:* Mountains, rooftop arena, Fortress · enemies: PikeGuard, RaiderAxe, RaiderAxe, Ranged, Bandit

#### 34 — THE EXECUTION GROUND

- **Story purpose:** The chapter's most urgent mission.
- **Primary objective:** Stop the execution.
- **Gameplay type:** Defense + Rescue
- **Unique event:** A clock the player cannot see: the executioner walks the line, and each prisoner reached is lost.
- **Story discovery:** Aiko is not among them. The ledger says she was moved again, the night before.
- **Climax:** Holding the platform against Goro's men while the last prisoners are cut loose.
- **Ending:** Everyone on the platform lives. None of them is her.
- **Next mission reason:** Goro moved her. Goro's army knows where. Renzo goes through it.
- *Staging:* Mountains, rooftop arena, Fortress · enemies: RaiderAxe, Ranged, Assassin, Samurai, EliteWarrior

#### 35 — GORO'S ARMY

- **Story purpose:** The first large, organised force.
- **Primary objective:** Break the squad Goro has sent to end this.
- **Gameplay type:** Combat
- **Unique event:** A real formation: pikes in front, archers behind, and an officer who calls the changes.
- **Story discovery:** The squad carries orders to bring Renzo in alive. Kagehira wants to talk.
- **Climax:** The officer's last stand when the formation breaks.
- **Ending:** The squad is finished. Its officer will not say who wanted Renzo alive, but the seal on the order is a serpent.
- **Next mission reason:** An army needs a smith. Renzo finds where Goro's steel is made.
- *Staging:* Mountains, rooftop arena, Castle · enemies: PikeGuard, PikeGuard, PikeGuard, Ranged, Ranged, Samurai

#### 36 — THE BLACKSMITH

- **Story purpose:** The man who made the enemy's weapons was never on their side.
- **Primary objective:** Rescue the blacksmith and get him out of Goro's reach.
- **Gameplay type:** Escort + Rescue
- **Unique event:** An escort who fights: the smith swings a hammer, and he is not fast.
- **Story discovery:** The smith forged the Three Blades' steel and marked every blade he made under duress.
- **Climax:** Goro's riders reach the road before the smith does.
- **Ending:** Safe, the smith gives Renzo the mark: every blade he made for them can be told from an honest one.
- **Next mission reason:** Goro will answer the loss of his smith by burning the village that hid him.
- *Staging:* Mountains, rooftop arena, Village · enemies: Bandit, Bandit, Ranged, RaiderAxe, Assassin

#### 37 — THE SIEGE

- **Story purpose:** The one time the player defends a place that matters.
- **Primary objective:** Defend the village from Goro's forces until they break.
- **Gameplay type:** Defense
- **Unique event:** Multiple gates: each wave picks a different one, and the village's walls fail in sequence.
- **Story discovery:** Goro's forces are under orders to burn every village between here and the marsh.
- **Climax:** The last gate, the last wave, and Goro's banner in the field beyond.
- **Ending:** The village stands. Goro's banner does not advance. It waits.
- **Next mission reason:** Goro has stopped sending men. He is coming himself.
- *Staging:* Villages, rooftop arena, BurningVillage · enemies: Bandit, Bandit, PikeGuard, Ranged, RaiderAxe, Assassin, EliteWarrior

#### 38 — THE HUNTER RETURNS

- **Story purpose:** Goro, unleashed.
- **Primary objective:** Survive Goro's hunt and reach the mountain road.
- **Gameplay type:** Endure + Chase
- **Unique event:** The boss is the pursuer: Goro cannot be beaten here, only outlasted and outrun.
- **Story discovery:** Goro is hunting Renzo against Kagehira's orders. He wants this personally.
- **Climax:** Goro at the ridge, and the drop behind Renzo.
- **Ending:** Goro lets him run. He wants the fight on his own ground.
- **Next mission reason:** Goro's ground is the mountain gate. Renzo goes to it.
- *Staging:* Mountains, rooftop arena, Mountain · enemies: RaiderAxe, PikeGuard, Ranged · named foe `goro`

#### 39 — THE MOUNTAIN GATE

- **Story purpose:** Goro's last wall.
- **Primary objective:** Break through the mountain fortress gate.
- **Gameplay type:** Combat + Sabotage
- **Unique event:** The gate has to be burned open while the wall shoots down at you.
- **Story discovery:** Inside the gatehouse: Aiko's cell, empty, with her thread on the bars.
- **Climax:** The gatehouse garrison, then the gate.
- **Ending:** The gate falls. Goro is waiting in the yard, alone, sword drawn.
- **Next mission reason:** There is nothing between Renzo and Goro now.
- *Staging:* Mountains, rooftop arena, Fortress · enemies: PikeGuard, PikeGuard, Ranged, Ranged, RaiderAxe, EliteWarrior

#### 40 — GORO'S END

- **Story purpose:** The final duel with the first enemy.
- **Primary objective:** Defeat Goro.
- **Gameplay type:** Boss
- **Unique event:** A boss the player already beat once, fighting like a man who learned from it.
- **Story discovery:** Goro took Aiko deeper into the marsh on Kagehira's orders, to a temple under the water.
- **Climax:** Goro's last phase, without his guard, without his pride.
- **Ending:** Goro dies on his own gate. 'The marsh,' he says. 'She's under it.'
- **Next mission reason:** Under the marsh. Renzo goes back into the fog.
- *Staging:* Mountains, rooftop arena, Fortress, night · enemies: PikeGuard, RaiderAxe, Ranged · boss Chief

### Chapter 5 — INTO THE MARSH

*Isolation and the Black Seal.*

#### 41 — THE DROWNED ROAD

- **Story purpose:** The ground itself is the enemy.
- **Primary objective:** Cross the flooded road before the tide closes it.
- **Gameplay type:** Survival
- **Unique event:** The water rises twice while you are standing in it.
- **Story discovery:** The carts on the road were untouched except for their lanterns. Somebody is collecting light.
- **Climax:** The last stretch, in rising water.
- **Ending:** The far bank, and voices in the fog that are not soldiers.
- **Next mission reason:** The voices are coming from the fog ahead. Renzo goes to find who is making them.
- *Staging:* Marsh, marsh arena, Graveyard · enemies: PikeGuard, PikeGuard, Ranged, Assassin, Bandit, Shade · bespoke plan `S07_DrownedRoad`

#### 42 — VOICES IN THE FOG

- **Story purpose:** The marsh speaks.
- **Primary objective:** Find the source of the voices in the fog.
- **Gameplay type:** Investigation
- **Unique event:** The voices move: each clue is where a voice was, not where it is.
- **Story discovery:** The voices are prisoners, calling from a camp the marsh has half swallowed.
- **Climax:** The shades that have been answering the voices.
- **Ending:** The camp is under the water line. So are its records.
- **Next mission reason:** Sunken records mean sunken answers. Renzo goes into the camp.
- *Staging:* Marsh, marsh arena, Graveyard, fog · enemies: Shade, Shade, Shade, Assassin

#### 43 — THE SUNKEN CAMP

- **Story purpose:** What the water kept.
- **Primary objective:** Search the submerged enemy camp.
- **Gameplay type:** Investigation + Survival
- **Unique event:** Half the arena is underwater; the search is in the shallows, and the deep is not safe.
- **Story discovery:** The camp's last commander wrote that the 'daughter' had been moved to the temple 'for the key.'
- **Climax:** The thing in the deep water that has been watching the search.
- **Ending:** A key. Aiko is not a prisoner. She is a lock.
- **Next mission reason:** The temple wants her for something. The marsh hunters between here and it want Renzo.
- *Staging:* Marsh, marsh arena, Graveyard · enemies: Shade, Shade, Bomber, Assassin

#### 44 — MARSH HUNTERS

- **Story purpose:** The assassins have followed Renzo into the fog.
- **Primary objective:** Survive the assassin ambush.
- **Gameplay type:** Survival
- **Unique event:** An ambush in fog: the enemy is closer than you can see and so are you.
- **Story discovery:** The assassins were sent by Jin Kurogane, not by Goro. A new name.
- **Climax:** The last assassins, when the fog thins and both sides can see.
- **Ending:** A Kurogane crest on the last body. Renzo has heard the name. He does not know why it stings.
- **Next mission reason:** An enemy patrol went into the fog before Renzo and did not come out. What stopped them might stop him.
- *Staging:* Marsh, marsh arena, Graveyard, fog · enemies: Assassin, Assassin, Assassin, RogueNinja, Ranged

#### 45 — THE MISSING PATROL

- **Story purpose:** Even the enemy is afraid of the marsh.
- **Primary objective:** Find the enemy patrol that disappeared.
- **Gameplay type:** Exploration + Investigation
- **Unique event:** The clues are bodies, and what killed them is still here.
- **Story discovery:** The patrol was killed by the marsh's own guardians: shades that answer to no warlord.
- **Climax:** The thing that killed the patrol, when Renzo reaches the last of them.
- **Ending:** Reed smoke on the wind. Somebody lives out here.
- **Next mission reason:** Someone survives in this marsh. They will know how to reach the temple.
- *Staging:* Marsh, marsh arena, Graveyard, fog · enemies: Shade, Shade, Shade, Shade

#### 46 — THE REED VILLAGE

- **Story purpose:** People who chose the marsh over Kagehira.
- **Primary objective:** Reach the reed village and earn its trust.
- **Gameplay type:** Defense + Exploration
- **Unique event:** The villagers are hostile at first; the defense that follows is what turns them.
- **Story discovery:** The village hides an old guide who has been to the temple and returned.
- **Climax:** The village attacked by shades, with Renzo the only blade.
- **Ending:** The guide agrees to take him. She does not agree to like it.
- **Next mission reason:** The guide knows the way. The way is not safe, and she is not fast.
- *Staging:* Marsh, marsh arena, Village · enemies: Shade, Shade, Assassin, Ranged

#### 47 — THE OLD GUIDE

- **Story purpose:** Someone else's life in Renzo's hands, on ground that wants both of them.
- **Primary objective:** Protect the guide to the temple stair.
- **Gameplay type:** Escort
- **Unique event:** An escort through the marsh: the guide stops for nothing, and the water rises behind her.
- **Story discovery:** The guide knew Renzo's father. He came this way ten years ago, with something wrapped in cloth.
- **Climax:** The temple stair, and everything in the marsh that does not want it climbed.
- **Ending:** The guide sits down on the stair and will go no further. 'Below,' she says. 'It's all below.'
- **Next mission reason:** The ruin is under the water. Renzo goes down.
- *Staging:* Marsh, marsh arena, Graveyard, fog · enemies: Shade, Shade, Bandit, Ranged, Assassin · bespoke plan `S02_LanternRoad`

#### 48 — BENEATH THE WATER

- **Story purpose:** Under the marsh.
- **Primary objective:** Enter the submerged ruin.
- **Gameplay type:** Survival + Exploration
- **Unique event:** The water is the clock: the ruin floods behind you and the way in becomes the way not-out.
- **Story discovery:** The ruin is Kurogawa work. The carvings are the same hand as the symbol in the pines.
- **Climax:** The flood reaches the chamber door as the last guardian falls.
- **Ending:** The door. Sealed. Marked with the symbol from his father's blade.
- **Next mission reason:** The chamber is the Seal's. Renzo has the fragment that opens it.
- *Staging:* Temples, marsh arena, Temple · enemies: Shade, Shade, Shade, EliteWarrior

#### 49 — THE SEAL CHAMBER

- **Story purpose:** The chapter's revelation, found rather than told.
- **Primary objective:** Discover the Black Seal chamber and what it holds.
- **Gameplay type:** Investigation + Exploration
- **Unique event:** No enemies until the chamber is read; the fight comes for what you learned.
- **Story discovery:** The Seal is not a weapon. It is a lock with three keys, and Renzo's father made the keys.
- **Climax:** The chamber's guardians wake when the first key is lifted.
- **Ending:** The first key, in Renzo's hand, and the chamber going dark.
- **Next mission reason:** One key of three. The second is wherever his father hid it, and his father's journal will say.
- *Staging:* Temples, marsh arena, Temple, night · enemies: EliteWarrior, Samurai, Shade, Bomber

#### 50 — THE FIRST KEY

- **Story purpose:** Kagehira's men arrive for what Renzo already holds.
- **Primary objective:** Escape the chamber with the first key.
- **Gameplay type:** Combat + Survival
- **Unique event:** Escape under pursuit: the chamber's own defences turn on the intruders, both sides.
- **Story discovery:** Renzo's father hid the Seal from Kagehira deliberately, at the cost of Yorune.
- **Climax:** The chamber stair, with the marsh above and Kagehira's elite below.
- **Ending:** Renzo surfaces with the key. Behind him, the temple closes.
- **Next mission reason:** His father's journal is the map to the second key. It is in the drowned temple's upper halls.
- *Staging:* Temples, marsh arena, Temple · enemies: EliteWarrior, Samurai, Assassin, Assassin, Ranged

### Chapter 6 — THE DROWNED TEMPLE

*Renzo's family history.*

#### 51 — FATHER'S JOURNAL

- **Story purpose:** The past, in his father's hand.
- **Primary objective:** Recover his father's journal from the temple's upper halls.
- **Gameplay type:** Investigation
- **Unique event:** The journal is in pieces across the halls; each page read changes what Renzo sees.
- **Story discovery:** The journal begins the year before Yorune burned. His father knew Kagehira was coming.
- **Climax:** The temple's watchers, when the last page is lifted.
- **Ending:** The last page is a date. The night Yorune burned. And a place to stand.
- **Next mission reason:** The journal asks him to remember. The memory is where the truth is.
- *Staging:* Temples, marsh arena, Temple · enemies: Shade, Shade, Samurai

#### 52 — THE LAST NIGHT

- **Story purpose:** Renzo walks through the night Yorune burned, as he remembers it.
- **Primary objective:** Play through the memory of Yorune's last evening.
- **Gameplay type:** Memory + Exploration
- **Unique event:** A memory mission: the village whole, the people alive, and the player walking through it knowing.
- **Story discovery:** The evening was ordinary. His father was not: he was packing something wrapped in cloth.
- **Climax:** The first fire, on the ridge, as the memory ends.
- **Ending:** Renzo wakes on the temple floor with the journal open to the next page.
- **Next mission reason:** The memory stopped at the fire. The next page is the fire.
- *Staging:* Villages, rooftop arena, VillageDawn · enemies: none · beat `memory_lastnight`

#### 53 — THE BURNING VILLAGE

- **Story purpose:** What actually happened.
- **Primary objective:** Fight through the burning village as it was.
- **Gameplay type:** Combat + Memory
- **Unique event:** The village burns in real time around the memory; the enemies are the ones who were there.
- **Story discovery:** The raiders were not raiding. They were searching every house for one thing.
- **Climax:** The house where his father made his stand.
- **Ending:** The memory breaks at the door. Renzo could not go in then. He cannot now.
- **Next mission reason:** His father's stand is the page he has never been able to read.
- *Staging:* Villages, rooftop arena, BurningVillage · enemies: Bandit, Bandit, RaiderAxe, Ranged · beat `memory_burning`

#### 54 — THE SWORDMASTER

- **Story purpose:** His father's final stand, as the journal tells it.
- **Primary objective:** Learn how Renzo's father fell.
- **Gameplay type:** Boss + Memory
- **Unique event:** The player fights as the memory of his father: stronger, slower, and doomed.
- **Story discovery:** His father held the door long enough for the Seal to be carried out. He did not hold it for himself.
- **Climax:** The raider captain — a young Goro — at the door.
- **Ending:** The door holds. The man behind it does not.
- **Next mission reason:** The Seal was carried out by someone. The journal says who.
- *Staging:* Villages, rooftop arena, BurningVillage · enemies: Bandit, PikeGuard, Assassin · named foe `goro`

#### 55 — MOTHER'S CHOICE

- **Story purpose:** His mother, and the people she saved.
- **Primary objective:** Play through his mother's escape with the villagers.
- **Gameplay type:** Escort + Memory + Rescue
- **Unique event:** An escort through fire, with a child at the head of the line.
- **Story discovery:** His mother carried the Seal out of Yorune. She gave it to the child to hold.
- **Climax:** The lantern line, and the raiders waiting at the end of it.
- **Ending:** The villagers reach the ridge. His mother turns back for the last child.
- **Next mission reason:** The child at the head of the line was Aiko. The memory follows her.
- *Staging:* Villages, rooftop arena, BurningVillage · enemies: Bandit, Ranged, Ranged, Bomber

#### 56 — AIKO

- **Story purpose:** Aiko's last hour of freedom.
- **Primary objective:** Play Aiko's escape until it fails.
- **Gameplay type:** Stealth + Memory
- **Unique event:** The player is Aiko: no sword, no strength, only stealth and a village she knows better than they do.
- **Story discovery:** Aiko hid the Seal before she was taken. She never told them where.
- **Climax:** The raiders find her in the shrine. She has already hidden it.
- **Ending:** The memory ends with a hand over her mouth and a thread snapping.
- **Next mission reason:** The Seal was never taken. Aiko hid it, and Kagehira has spent ten years asking her where.
- *Staging:* Villages, rooftop arena, BurningVillage · enemies: Bandit, Assassin, RogueNinja · beat `memory_aiko`

#### 57 — THE PRISONER

- **Story purpose:** Back in the present: Aiko's cell in the temple.
- **Primary objective:** Find the cell Aiko was held in.
- **Gameplay type:** Investigation
- **Unique event:** The temple's prison wing, read cell by cell; hers is the one with the thread.
- **Story discovery:** Aiko was held here for years. She scratched the second key's location into the wall in a code only a Kurogawa could read.
- **Climax:** The wardens, who have been told to let no one read the walls.
- **Ending:** Renzo reads his sister's handwriting for the first time in ten years.
- **Next mission reason:** The second key is under the temple's guardian. The wall says so.
- *Staging:* Temples, marsh arena, Temple, night · enemies: PikeGuard, PikeGuard, Ranged, Samurai

#### 58 — THE SECOND KEY

- **Story purpose:** The second of three.
- **Primary objective:** Recover the second Seal key.
- **Gameplay type:** Rescue + Combat
- **Unique event:** The key is in the flooded nave; the water is chest-deep and the enemies do not care.
- **Story discovery:** Kagehira's men found this chamber years ago and could not open it. Only a Kurogawa can.
- **Climax:** The nave's last guard, waist-deep.
- **Ending:** Two keys. The guardian below is what stands between him and the third.
- **Next mission reason:** The Drowned Guardian holds the way down. It was put there by his father.
- *Staging:* Temples, marsh arena, Temple · enemies: Shade, Shade, EliteWarrior, Samurai

#### 59 — THE DROWNED GUARDIAN

- **Story purpose:** The elite fight of the chapter, with his father's mark on it.
- **Primary objective:** Defeat the Drowned Guardian.
- **Gameplay type:** Boss
- **Unique event:** A guardian bound to the family: it holds back when Renzo bleeds, and hits harder when he does not.
- **Story discovery:** The guardian was set by his father to keep the third key from anyone — including his own children.
- **Climax:** The guardian's last phase, in the dark, in the water.
- **Ending:** It falls. Under it: not a key. A message.
- **Next mission reason:** His father left words instead of a key. The words say why.
- *Staging:* Temples, marsh arena, Temple, night · enemies: Shade, Shade · named foe `drownedguardian`

#### 60 — THE TRUTH BENEATH YORUNE

- **Story purpose:** The mid-campaign revelation.
- **Primary objective:** Read what his father left beneath Yorune.
- **Gameplay type:** Conversation + Exploration
- **Unique event:** No fight. The mission is the walk to the message and the walk back, changed.
- **Story discovery:** Kagehira did not burn Yorune for revenge or conquest. He wanted the Black Seal, and Renzo's father refused him.
- **Climax:** The message ends with a name: the man who told Kagehira where the Seal was. Kurogane.
- **Ending:** Renzo surfaces. He has stopped looking for answers. He is looking for Kurogane.
- **Next mission reason:** Jin Kurogane sold Yorune. Renzo goes to find him.
- *Staging:* Temples, marsh arena, Temple · enemies: none · beat `father_message`

### Chapter 7 — KUROGANE

*Jin and Renzo's rivalry.*

#### 61 — THE BLACK BLADE

- **Story purpose:** The first meeting with Jin.
- **Primary objective:** Survive the first encounter with Jin Kurogane.
- **Gameplay type:** Endure
- **Unique event:** Jin cannot be beaten here. He can be hurt, and he notices.
- **Story discovery:** Jin knows Renzo's name, his father's, and his sister's. He says them like a man reading a list.
- **Climax:** Jin, unhurried, taking the fight apart.
- **Ending:** Jin steps back into the rain. 'Not yet,' he says.
- **Next mission reason:** Jin walked away over the rooftops. Renzo does not let him.
- *Staging:* Villages, rooftop arena, RainyBattlefield, night, rain · enemies: Assassin, Assassin, Ranged · named foe `jin`

#### 62 — THE PURSUIT

- **Story purpose:** The rooftop chase.
- **Primary objective:** Chase Jin across the rooftops.
- **Gameplay type:** Chase
- **Unique event:** A chase against a target who fights back at every ledge and never fully runs.
- **Story discovery:** Jin is leading Renzo somewhere. The chase is his choice, not Renzo's.
- **Climax:** The last roof, the drop, and Jin waiting on the far side of it.
- **Ending:** Renzo makes the jump. Jin is already sheathing his sword.
- **Next mission reason:** Jin stopped running because he wanted to fight here. Renzo obliges him.
- *Staging:* Villages, rooftop arena, RainyBattlefield, rain · enemies: RogueNinja, RogueNinja, Assassin, Ranged

#### 63 — NO HONOR

- **Story purpose:** Renzo loses.
- **Primary objective:** Fight Jin.
- **Gameplay type:** Endure
- **Unique event:** A duel the player is meant to lose: Jin defeats Renzo, and does not kill him.
- **Story discovery:** Jin could have killed him twice. He chose not to, and he wanted Renzo to know it.
- **Climax:** Renzo on his knees, and Jin's blade at his throat, withdrawn.
- **Ending:** 'Go home, Kurogawa. There is nothing up this mountain but me.'
- **Next mission reason:** Jin let him live, and Renzo does not know why. His past will.
- *Staging:* Villages, rooftop arena, Castle, night · enemies: Samurai, Assassin · named foe `jin` · beat `jin_mercy`

#### 64 — THE FALLEN SOLDIER

- **Story purpose:** Who Jin was.
- **Primary objective:** Investigate Jin's past in the garrison town.
- **Gameplay type:** Investigation + Stealth
- **Unique event:** Investigation among people who knew Jin as a hero.
- **Story discovery:** Jin was Kagehira's finest officer and left the army the year Yorune burned.
- **Climax:** Jin's old unit, still loyal, when they realise what Renzo is asking.
- **Ending:** A portrait in the garrison hall: Jin, ten years younger, standing behind Renzo's father.
- **Next mission reason:** Jin and his father knew each other. Jin's men will know how.
- *Staging:* Villages, rooftop arena, Village · enemies: Assassin, Assassin, PikeGuard, Ranged

#### 65 — KUROGANE'S MEN

- **Story purpose:** Jin's personal unit.
- **Primary objective:** Fight Jin's personal unit.
- **Gameplay type:** Combat
- **Unique event:** Elite soldiers who fight like Jin taught them: they read heavies and they never block twice the same way.
- **Story discovery:** Jin's men were at Yorune. Every one of them.
- **Climax:** The unit's captain, who was Jin's second that night.
- **Ending:** The captain says only: 'He tried to stop it.'
- **Next mission reason:** A man who fights honestly can be asked honestly. Renzo challenges Jin to a duel with terms.
- *Staging:* Villages, rooftop arena, Castle · enemies: Samurai, Samurai, EliteWarrior, Assassin, Ranged

#### 66 — THE DUELIST

- **Story purpose:** Pure combat, on agreed terms.
- **Primary objective:** Win the duel with Jin's champion.
- **Gameplay type:** Boss
- **Unique event:** A formal duel: no allies, no interference, a ring drawn in the dust.
- **Story discovery:** Jin will not fight until Renzo has beaten his champion. He is testing something.
- **Climax:** The champion, a ronin who fights exactly like Jin.
- **Ending:** The champion yields. Jin was watching from the roofline.
- **Next mission reason:** Jin has seen enough. He sends a message: a place, a time, and no guards.
- *Staging:* Villages, rooftop arena, Village · enemies: Samurai · named foe `finalcommander`

#### 67 — THE BROKEN MASK

- **Story purpose:** Jin's connection to Yorune.
- **Primary objective:** Search Jin's old house for what he kept.
- **Gameplay type:** Exploration
- **Unique event:** An empty house that is the whole mission; every room is a clue.
- **Story discovery:** Jin's family was from Yorune. He left it the year before it burned, to serve Kagehira.
- **Climax:** The men Kagehira sent to burn the house before Renzo could read it.
- **Ending:** In the ashes of the last room: a mask, broken in half, one half missing.
- **Next mission reason:** Half a mask, and the other half is on Jin's face. He is waiting where he said.
- *Staging:* Villages, rooftop arena, Village, night · enemies: Assassin, Assassin, RogueNinja, Ranged

#### 68 — THE CONFESSION

- **Story purpose:** Jin tells the truth.
- **Primary objective:** Meet Jin and hear him out.
- **Gameplay type:** Conversation + Survival
- **Unique event:** The fight is optional and short; the conversation is the mission.
- **Story discovery:** Jin witnessed Yorune's destruction. He told Kagehira where the Seal was, and then he tried to stop what came.
- **Climax:** Kagehira's assassins interrupt the confession.
- **Ending:** 'I gave him the map. I did not give him the village. He took that himself.'
- **Next mission reason:** Jin has one more thing to say, and he will only say it with a sword in his hand.
- *Staging:* Villages, rooftop arena, RainyBattlefield, rain · enemies: Assassin, Assassin, Assassin, Ranged · beat `jin_confession`

#### 69 — LAST WARNING

- **Story purpose:** Jin's warning, and the question the last act is built on.
- **Primary objective:** Hear Jin's warning and survive what he does to prove it.
- **Gameplay type:** Endure + Conversation
- **Unique event:** Jin fights to show, not to win: every exchange is a lesson about what Renzo is becoming.
- **Story discovery:** 'If you reach Kagehira, you may become him.' Jin has watched it happen before.
- **Climax:** Jin's final demonstration: Renzo's own rage, used against him.
- **Ending:** Jin sheathes his sword. 'Tomorrow, then. Properly.'
- **Next mission reason:** Tomorrow. The duel neither of them can walk away from.
- *Staging:* Villages, rooftop arena, Castle, night · enemies: Samurai · named foe `jin` · beat `jin_warning`

#### 70 — KUROGANE

- **Story purpose:** The late-game boss, and the death that changes Renzo.
- **Primary objective:** Defeat Jin Kurogane.
- **Gameplay type:** Boss
- **Unique event:** A duel against the one enemy who fights with honour: no adds, no tricks, and a phase that is only the two of them breathing.
- **Story discovery:** Jin tells him, dying: Aiko is alive, held inside Kagehira's mountain fortress.
- **Climax:** Jin's last phase, all storm.
- **Ending:** Jin dies with the half mask in his hand. 'Do not become him.'
- **Next mission reason:** Aiko is in the mountain fortress. Renzo begins the climb.
- *Staging:* Villages, rooftop arena, RainyBattlefield, night, rain · enemies: none · boss Jin

### Chapter 8 — THE IRON FORTRESS

*The final approach.*

#### 71 — THE MOUNTAIN ROAD

- **Story purpose:** The ascent begins.
- **Primary objective:** Begin the climb to Kagehira's fortress.
- **Gameplay type:** Exploration
- **Unique event:** Snow and thin air: the road is the enemy as much as the men on it.
- **Story discovery:** The road is littered with the enemy's own dead. The mountain is killing Kagehira's men.
- **Climax:** The first garrison of the ascent, in a blizzard.
- **Ending:** Above, the glow of a camp with artillery in it.
- **Next mission reason:** The camp above holds the guns that cover the road. They have to fall.
- *Staging:* Snow, rooftop arena, Mountain, snow · enemies: PikeGuard, RaiderAxe, Ranged, Assassin

#### 72 — THE FROZEN CAMP

- **Story purpose:** Taking the guns.
- **Primary objective:** Destroy the artillery and supply camp.
- **Gameplay type:** Sabotage
- **Unique event:** Fires set in the cold burn slow, and the powder carriers know it.
- **Story discovery:** The camp's supplies were meant for a siege. Kagehira expects an army, not one man.
- **Climax:** The magazine, when the last fire reaches it.
- **Ending:** The camp goes up. The mountain shivers.
- **Next mission reason:** The explosion has loosened the slope above the road. Renzo has minutes.
- *Staging:* Snow, rooftop arena, Mountain, night, snow · enemies: Bomber, Bomber, PikeGuard, Ranged, RaiderAxe

#### 73 — THE AVALANCHE

- **Story purpose:** The mountain comes down.
- **Primary objective:** Escape the collapsing route.
- **Gameplay type:** Survival + Chase
- **Unique event:** The arena collapses behind the player: there is one direction, and the clock is the mountain.
- **Story discovery:** Kagehira's men are caught in it too. The ones who survive stop fighting each other's enemies.
- **Climax:** The last stretch, with the snow at Renzo's heels.
- **Ending:** The outer wall, out of the white.
- **Next mission reason:** The wall is the fortress. There is no more road.
- *Staging:* Snow, rooftop arena, Mountain, snow, fog · enemies: Assassin, Ranged, Shade

#### 74 — THE OUTER WALL

- **Story purpose:** Assault, defense and a boss in one mission.
- **Primary objective:** Assault the fortress wall and hold the breach.
- **Gameplay type:** Combat + Defense + Boss
- **Unique event:** Three roles in sequence: take the gate, hold it, then face the man who comes to retake it.
- **Story discovery:** The wall's commander wears Yorune steel. The smith's mark is on it.
- **Climax:** The breach held, and the commander in it.
- **Ending:** The wall is Renzo's. The inner fortress is not.
- **Next mission reason:** The inner fortress has one silent way in, and the wall's plans show it.
- *Staging:* Fortresses, rooftop arena, Fortress, snow · enemies: PikeGuard, PikeGuard, Ranged, Ranged, RaiderAxe, EliteWarrior, EliteWarrior · named foe `ironguard`

#### 75 — THE SILENT GATE

- **Story purpose:** The quiet way in.
- **Primary objective:** Infiltrate the inner fortress through the drain.
- **Gameplay type:** Stealth
- **Unique event:** The fortress asks how you want to play it, once, at the gate, and holds you to it.
- **Story discovery:** The fortress armoury is full of Yorune steel. Every blade belonged to someone on the missing list.
- **Climax:** Two elites and archer support at the inner gate.
- **Ending:** Inside. The prison tower is lit.
- **Next mission reason:** Aiko is in the tower. Renzo climbs.
- *Staging:* Fortresses, rooftop arena, Fortress, night, snow · enemies: EliteWarrior, EliteWarrior, PikeGuard, PikeGuard, Ranged · bespoke plan `S09_SerpentsGuard`

#### 76 — THE PRISON TOWER

- **Story purpose:** The rescue that has been ten years coming.
- **Primary objective:** Search the prison tower for Aiko.
- **Gameplay type:** Rescue + Stealth
- **Unique event:** Every cell freed is a voice that might be hers. None of them is.
- **Story discovery:** The prisoners say the girl with the thread was moved last night.
- **Climax:** The tower's wardens, and the alarm they are trying to reach.
- **Ending:** Her cell, at the top, empty. Warm.
- **Next mission reason:** An empty cell has a record. The record will say where.
- *Staging:* Fortresses, rooftop arena, Fortress, night · enemies: PikeGuard, PikeGuard, Assassin, Ranged

#### 77 — THE EMPTY CELL

- **Story purpose:** The near miss.
- **Primary objective:** Find where Aiko was taken.
- **Gameplay type:** Investigation
- **Unique event:** The investigation is fought: the evidence is in rooms the guards are burning.
- **Story discovery:** Aiko was moved to the inner fortress, to Kagehira himself. He has all three keys' worth of reasons.
- **Climax:** The records room, on fire, with the transfer order in it.
- **Ending:** The order is in Kagehira's own hand. She is with him.
- **Next mission reason:** Kagehira's elite guard stands between the outer fortress and the warlord's hall.
- *Staging:* Fortresses, rooftop arena, Fortress · enemies: Assassin, Assassin, PikeGuard, Ranged, RaiderAxe

#### 78 — THE IRON GUARD

- **Story purpose:** Kagehira's shield.
- **Primary objective:** Fight through Kagehira's elite guard.
- **Gameplay type:** Boss + Combat
- **Unique event:** The Iron Guard fight as a unit with a captain who calls the changes; take the captain and the unit breaks.
- **Story discovery:** The Iron Guard were Goro's men once. They know exactly who Renzo is.
- **Climax:** The captain of the guard, in the iron hall.
- **Ending:** The guard is broken. The inner gate is ahead, and the last commander.
- **Next mission reason:** One commander remains between Renzo and the hall.
- *Staging:* Fortresses, rooftop arena, Fortress · enemies: EliteWarrior, EliteWarrior, EliteWarrior, Samurai, Ranged · named foe `ironguard`

#### 79 — THE INNER GATE

- **Story purpose:** The last commander.
- **Primary objective:** Defeat the final commander.
- **Gameplay type:** Boss
- **Unique event:** The commander fights with the Three Blades' discipline and Goro's strength: everything the campaign has taught, at once.
- **Story discovery:** The commander was told Renzo would reach this gate. Kagehira has been expecting him for months.
- **Climax:** The commander's last stand at the gate itself.
- **Ending:** The gate opens. The hall beyond is lit, and empty.
- **Next mission reason:** The throne hall. Whatever is in it is what all of this was for.
- *Staging:* Fortresses, rooftop arena, Fortress · enemies: Samurai, Samurai, EliteWarrior, Ranged · named foe `finalcommander`

#### 80 — THE WARLORD'S HALL

- **Story purpose:** The end of the approach, and the wrong person on the throne.
- **Primary objective:** Reach the throne chamber.
- **Gameplay type:** Exploration
- **Unique event:** The hall is empty of soldiers and full of what Kagehira has collected: every lantern from the drowned road.
- **Story discovery:** Kagehira is gone. He left the fortress before Renzo took the wall.
- **Climax:** The hall's last defenders, and a figure behind the throne who is not one of them.
- **Ending:** Aiko. Standing. Older. Alive.
- **Next mission reason:** She is here, and Kagehira is not. Whatever he wants, he left her to tell it.
- *Staging:* Stronghold, rooftop arena, Castle, night · enemies: EliteWarrior, EliteWarrior, Assassin, Ranged

### Chapter 9 — THE BLACK SEAL

*Truth and payoff.*

#### 81 — YOU CAME

- **Story purpose:** The reunion.
- **Primary objective:** Find Aiko in the hall.
- **Gameplay type:** Conversation + Exploration
- **Unique event:** No enemies. The mission is the walk across the hall.
- **Story discovery:** Aiko has been Kagehira's prisoner for ten years and his prize for the last one. She knows what the Seal is.
- **Climax:** Aiko turns. 'You came.'
- **Ending:** Two words, and ten years.
- **Next mission reason:** Aiko has a story that will take the night to tell. The fortress is not safe to tell it in.
- *Staging:* Stronghold, rooftop arena, Castle · enemies: none · beat `you_came`

#### 82 — THE LONG NIGHT

- **Story purpose:** What happened to Aiko.
- **Primary objective:** Hold the hall while Aiko tells it.
- **Gameplay type:** Defense + Conversation
- **Unique event:** A conversation interrupted by waves: every lull is another piece of the story.
- **Story discovery:** Aiko hid the Seal the night Yorune burned. Kagehira has spent ten years asking her where, and she has never told him.
- **Climax:** The last wave, and the doors giving.
- **Ending:** 'It is under the shrine floor. He never thought to look at home.'
- **Next mission reason:** The Seal is in Yorune, and Kagehira's army stands between here and there. First, Aiko has to survive the fortress.
- *Staging:* Stronghold, rooftop arena, Castle · enemies: PikeGuard, PikeGuard, Ranged, Assassin, EliteWarrior · beat `long_night`

#### 83 — THE PRISONER

- **Story purpose:** Aiko out of the fortress.
- **Primary objective:** Escort Aiko through the fortress.
- **Gameplay type:** Escort
- **Unique event:** Aiko can run and Aiko can hide; she cannot fight, and she will not wait.
- **Story discovery:** Kagehira's soldiers hesitate at Aiko. Some of them were at Yorune.
- **Climax:** The last courtyard, and the soldiers who did not hesitate.
- **Ending:** Outside the walls. Behind them, the fortress erupts in its own fighting.
- **Next mission reason:** The fortress is fighting itself. Renzo needs to know why.
- *Staging:* Stronghold, rooftop arena, Fortress · enemies: PikeGuard, Assassin, Ranged, Bandit, RaiderAxe, EliteWarrior

#### 84 — THE BETRAYAL

- **Story purpose:** The army turns.
- **Primary objective:** Survive the fortress mutiny and find who is leading it.
- **Gameplay type:** Combat
- **Unique event:** Enemy soldiers turn against Kagehira mid-fight: the last waves are fighting each other, and then they stop.
- **Story discovery:** Half the army has turned. They followed Kagehira for conquest, not for a seal.
- **Climax:** The loyalists' last stand, and the mutineers standing down at the sight of Aiko.
- **Ending:** The mutineers open the way to the ancient chamber under the fortress.
- **Next mission reason:** The chamber under the fortress is where Kagehira kept the keys. Aiko knows the door.
- *Staging:* Stronghold, rooftop arena, Fortress, night · enemies: PikeGuard, PikeGuard, RaiderAxe, Ranged, Assassin, EliteWarrior

#### 85 — THE SEAL'S DOOR

- **Story purpose:** The door.
- **Primary objective:** Reach the ancient chamber with Aiko.
- **Gameplay type:** Exploration
- **Unique event:** Aiko reads the chamber as they walk; the clues are hers, not Renzo's.
- **Story discovery:** The chamber was built by the first Kurogawa. It was never Kagehira's to open.
- **Climax:** The chamber's guardians, awake for the first time in a century.
- **Ending:** The door, and their father's mark on it, and a message beneath the mark.
- **Next mission reason:** Their father left words at the door. They are for both of them.
- *Staging:* Seal, rooftop arena, Temple, night · enemies: Shade, Shade, EliteWarrior

#### 86 — FATHER'S FINAL MESSAGE

- **Story purpose:** His father's last word.
- **Primary objective:** Hear their father's final message.
- **Gameplay type:** Memory + Conversation
- **Unique event:** A memory neither of them has: their father, alone, the night before, speaking to children who were not there yet.
- **Story discovery:** Their father knew Kagehira would come, and hid the Seal so that no one — including his children — would open it in anger.
- **Climax:** The message ends: 'Whatever it is you are angry about when you hear this — be less.'
- **Ending:** Aiko takes Renzo's hand. He lets her.
- **Next mission reason:** The message says what the Seal is not. Aiko knows what it is.
- *Staging:* Seal, rooftop arena, Temple · enemies: none · beat `father_final`

#### 87 — THE MEANING OF THE SEAL

- **Story purpose:** What it is.
- **Primary objective:** Assemble the truth of the Seal from the chamber's carvings.
- **Gameplay type:** Investigation
- **Unique event:** The investigation is the chamber itself; the fight is Kagehira's men arriving to stop it being read.
- **Story discovery:** The Black Seal is not a weapon. It is a key: to the mountain's water, and every village below it.
- **Climax:** Kagehira's vanguard in the chamber, ordered to take Aiko alive.
- **Ending:** Kagehira has the third key. He has had it for a year. He needs the door, and the door needs a Kurogawa.
- **Next mission reason:** Kagehira is coming to the chamber himself, and he is not coming alone.
- *Staging:* Seal, rooftop arena, Temple · enemies: EliteWarrior, EliteWarrior, Assassin, Ranged, Samurai

#### 88 — KAGEHIRA'S TRUTH

- **Story purpose:** The warlord, in his own words.
- **Primary objective:** Face Kagehira and hear his reasons.
- **Gameplay type:** Endure + Conversation
- **Unique event:** Kagehira does not fight to win here. He talks, and every sentence is a cut.
- **Story discovery:** Kagehira's obsession: with the Seal, he controls every village's water. Without it, he is a bandit with an army.
- **Climax:** Kagehira's demonstration of what he can do without the Seal.
- **Ending:** He withdraws to raise his army. 'Open it, or I will burn my way to the summit and open it with your sister's hands.'
- **Next mission reason:** Kagehira's army is marching on the fortress. There is one night to prepare.
- *Staging:* Seal, rooftop arena, Fortress, night · enemies: EliteWarrior, EliteWarrior · named foe `kagachi` · beat `kagehira_truth`

#### 89 — THE FINAL MARCH

- **Story purpose:** The siege of the fortress.
- **Primary objective:** Hold the fortress against Kagehira's army.
- **Gameplay type:** Defense
- **Unique event:** The mutineers hold the walls with Renzo; the waves are the largest in the game and the last.
- **Story discovery:** Kagehira's army is breaking on the walls. He does not care. He is not with it.
- **Climax:** The final wave, and the realisation that Kagehira is already inside.
- **Ending:** The walls hold. The chamber does not.
- **Next mission reason:** Kagehira went around the army. He is at the door with the third key.
- *Staging:* Seal, rooftop arena, Fortress, night · enemies: Bandit, Bandit, PikeGuard, PikeGuard, Ranged, Ranged, RaiderAxe, Assassin, EliteWarrior, EliteWarrior

#### 90 — THE DOOR OPENS

- **Story purpose:** The chamber opens.
- **Primary objective:** Reach the chamber before Kagehira opens it.
- **Gameplay type:** Chase
- **Unique event:** A chase against a door: the clock is the ritual, and the enemies are the ones guarding it.
- **Story discovery:** Kagehira has Aiko's blood on the key. He does not need her willing.
- **Climax:** The door opens as Renzo reaches it. Kagehira takes the final key and goes up.
- **Ending:** The chamber is open. Kagehira has all three keys and a road to the summit.
- **Next mission reason:** The summit is where the Seal's lock is. Kagehira is climbing to it.
- *Staging:* Seal, rooftop arena, Temple, night · enemies: EliteWarrior, EliteWarrior, Samurai, Assassin, Ranged

### Chapter 10 — THE SERPENT'S END

*War, revenge and choice.*

#### 91 — THE BURNING FORTRESS

- **Story purpose:** The fortress falls.
- **Primary objective:** Get Aiko out of the collapsing fortress.
- **Gameplay type:** Chase + Survival
- **Unique event:** The fortress burns and falls around the escape; the exits close in the order the fire chooses.
- **Story discovery:** Kagehira set the fire himself. He is burning the bridge behind him.
- **Climax:** The last gate, and the fire reaching it first.
- **Ending:** Out. The summit road, and Kagehira's rear guard on it.
- **Next mission reason:** The road to the summit is held by the last of Kagehira's army.
- *Staging:* Seal, rooftop arena, BurningVillage, night · enemies: Assassin, Ranged, EliteWarrior, Shade

#### 92 — THE LAST ARMY

- **Story purpose:** The remaining forces.
- **Primary objective:** Fight through Kagehira's remaining army.
- **Gameplay type:** Combat
- **Unique event:** The largest sustained fight in the game, with the mutineers arriving at the end.
- **Story discovery:** The army is fighting for pay. The mutineers are fighting for Aiko. It shows.
- **Climax:** The last officer, and the mutineers breaking the line behind him.
- **Ending:** The road is open. The summit is a day's climb.
- **Next mission reason:** The summit is where Kagehira is. Renzo climbs.
- *Staging:* Snow, rooftop arena, Mountain, snow · enemies: PikeGuard, PikeGuard, PikeGuard, Ranged, Ranged, RaiderAxe, RaiderAxe, Assassin, EliteWarrior

#### 93 — THE SUMMIT ROAD

- **Story purpose:** The climb.
- **Primary objective:** Climb toward Kagehira.
- **Gameplay type:** Exploration
- **Unique event:** Thin air and a blizzard: the mission is endurance, and the enemies are the ones who could not keep up.
- **Story discovery:** Kagehira's guard died on this road. He climbed on alone.
- **Climax:** The final ascent, and the last of the guard turning back to hold it.
- **Ending:** The summit gate, and Kagehira's strongest warriors in front of it.
- **Next mission reason:** Kagehira's best are at the gate. They are the last wall.
- *Staging:* Snow, rooftop arena, Mountain, snow, fog · enemies: Assassin, Ranged, Shade, Shade

#### 94 — THE FINAL GUARD

- **Story purpose:** Kagehira's strongest.
- **Primary objective:** Defeat Kagehira's strongest warriors.
- **Gameplay type:** Boss + Combat
- **Unique event:** Every named foe's tactics, in one unit: they read, they punish, they protect, they retreat.
- **Story discovery:** The final guard were told Renzo would kill Kagehira. They were told to make sure he did not arrive whole.
- **Climax:** The last two, back to back, in the snow.
- **Ending:** The gate is open. Beyond it, the summit, and nothing on it.
- **Next mission reason:** Kagehira is on the summit. He has seen Renzo coming for an hour.
- *Staging:* Snow, rooftop arena, Fortress, snow · enemies: EliteWarrior, EliteWarrior, EliteWarrior, Samurai, Samurai, RogueNinja · named foe `ironguard`

#### 95 — THE SERPENT'S SHADOW

- **Story purpose:** Kagehira strikes first.
- **Primary objective:** Survive Kagehira's ambush.
- **Gameplay type:** Endure
- **Unique event:** The warlord as an ambusher: he attacks from the fog and vanishes, and the arena is his.
- **Story discovery:** Kagehira is faster than Renzo expected, and he does not want to kill him yet.
- **Climax:** Kagehira's blade at Renzo's back, and the cut he chooses not to make.
- **Ending:** 'Alone,' he says. 'Come alone.' Aiko is gone from Renzo's side.
- **Next mission reason:** Kagehira has Aiko. Renzo goes up alone, as he was told.
- *Staging:* Seal, rooftop arena, Mountain, night, fog · enemies: Shade, Shade, Assassin · named foe `kagachi`

#### 96 — NO WAY BACK

- **Story purpose:** Alone.
- **Primary objective:** Fight alone to the summit chamber.
- **Gameplay type:** Combat + Survival
- **Unique event:** No allies, no mutineers, no Aiko; the last of the shades and the last of the men, together.
- **Story discovery:** Kagehira has released everything the marsh made. The shades were his all along.
- **Climax:** The chamber stair, with everything he has left on it.
- **Ending:** The chamber door, and a voice inside it Renzo knows.
- **Next mission reason:** Kagehira wants to talk before the end. Renzo lets him.
- *Staging:* Seal, rooftop arena, Mountain, night · enemies: Shade, Shade, Shade, Shade, Assassin, EliteWarrior

#### 97 — FATHER AND SON

- **Story purpose:** The final truth.
- **Primary objective:** Hear what Kagehira knows about Renzo's father.
- **Gameplay type:** Conversation + Defense
- **Unique event:** The revelation, then the fight it starts.
- **Story discovery:** Renzo's father and Kagehira were brothers-in-arms. The Seal was entrusted to both. One kept faith.
- **Climax:** Kagehira's guard, when Renzo refuses the offer.
- **Ending:** 'Your father chose the villages. I chose the future. One of us was right, boy. Let us find out which.'
- **Next mission reason:** There is nothing left to say. The chamber is open and Kagehira is in it.
- *Staging:* Seal, rooftop arena, Temple, night · enemies: EliteWarrior, EliteWarrior, Samurai · beat `father_and_son`

#### 98 — THE BLACK SEAL

- **Story purpose:** The chamber, opened.
- **Primary objective:** Enter the opened Seal chamber.
- **Gameplay type:** Combat + Exploration
- **Unique event:** The chamber is a place the player has only seen carved: now it is real, and the water is rising in it.
- **Story discovery:** The Seal was never meant to be opened. It was meant to be guarded. Kagehira has opened it.
- **Climax:** The chamber's last guardians, awake, on both sides.
- **Ending:** Kagehira, at the Seal, waiting. Aiko beside him, unbound. She has not run.
- **Next mission reason:** Kagehira is standing at the Seal. This is the end of it.
- *Staging:* Seal, rooftop arena, Temple, night · enemies: Shade, Shade, EliteWarrior, EliteWarrior

#### 99 — KAGACHI

- **Story purpose:** The final boss.
- **Primary objective:** Defeat Kagehira.
- **Gameplay type:** Boss
- **Unique event:** Four phases — swordsman, warlord, collapsing arena, exhausted duel — and a choice: the execution is offered and refused.
- **Story discovery:** Kagehira staggered, on his knees. Renzo raises the sword. Aiko: 'Don't become like them.' He lowers it.
- **Climax:** Kagehira's last attack, from his knees, and Renzo's answer.
- **Ending:** Kagehira dies. Renzo's sword is lowered. He did not become him.
- **Next mission reason:** It is over. The only thing left is to leave.
- *Staging:* Seal, marsh arena, Temple, night · enemies: Shade, Shade, Shade, Shade · boss Kagachi · beat `lower_the_sword` · bespoke plan `S10_Kagachi`

#### 100 — EMBERLINE

- **Story purpose:** The emotional conclusion. Not another wave.
- **Primary objective:** Leave the fortress.
- **Gameplay type:** Conversation + Exploration
- **Unique event:** No combat at all: walking, the sunrise, and the last conversation.
- **Story discovery:** Renzo chose not to become Kagehira. Aiko sees the red thread on his wrist.
- **Climax:** Aiko: 'Where will you go?' Renzo: 'Home.' Aiko: 'There is no home.' Renzo looks toward Yorune. 'Then we'll build one.'
- **Ending:** Fade to black. END.
- **Next mission reason:** There is no next mission. There is a home to build.
- *Staging:* Dawn, rooftop arena, VillageDawn · enemies: none · beat `emberline_dawn`

## Post-campaign

Finishing mission 100 unlocks New Game+ (the existing NG+ scaling: harder enemies, altered compositions, nightmare duels), the full nine-opponent duel roster (campaign bosses and named elites on the bodies they used), and the Infinite March as the endless mode over the campaign's environments and factions.

## What this campaign does not yet have

- Environment geometry for forest, mountain, snow, temple, fortress and the stronghold. Carried by theme, weather and dressing on the two arenas; specified, not built.
- Gameplay models for adult Aiko and Jin. Cinematic beats that frame them use a clearly named `PLACEHOLDER_*_StandIn` on the primitive rig.
- A fourth Kagehira phase with a physically collapsing arena. The final fight has three mission-level phases with arena changes (water, dark) plus the refusal beat; a collapsing floor is a geometry feature.
- Voice acting. Every line is subtitled through the existing story framework's VO hook.
