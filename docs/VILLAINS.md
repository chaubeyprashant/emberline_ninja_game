# Emberline — the villains

Every antagonist in the shipped game, with the numbers they actually fight with.
Transcribed from `Assets/Resources/Enemies/*.asset` (campaign stats), the duel
roster in `Assets/Scripts/Core/Session.cs` (duel stats and dialogue), the mission
table in `Assets/Scripts/Campaign/CampaignTable.cs`, and the body choices in
`docs/ENEMY_CHARACTER_SELECTION.md`.

Two stat sets exist per foe and they differ on purpose. The **campaign** numbers
are what the enemy has when it spawns in a mission. The **duel** numbers are what
it has in a one-on-one duel, tuned for a longer fight and paced by posture rather
than health.

---

## The roster at a glance

| # | Name | Title | Rank | Body | Height | Weapon | Campaign HP | Duel HP | Duel posture |
|---|---|---|---|---|---|---|---|---|---|
| 1 | **Goro** | The Toll-Captain | Mini-boss | Brute, warm | 2.25 m | Axe | 380 | 360 | 120 |
| 2 | **The Pale Shade** | What the Marsh Kept | Mini-boss | Akai, ghosted | 1.62 m | Claws | 216 | 280 | 140 |
| 3 | **Jin Kurogane** | The Storm Blade | Boss | Nightshade | 1.85 m | Sword | 340 | 340 | 160 |
| 4 | **Kagachi / Kagehira** | The Marsh Serpent | Boss | Ganfaul | 2.10 m | Sword | 420 | 480 | 190 |
| 5 | **The Convoy Captain** | Keeper of the Lantern Road | Mini-boss | Kachujin | 1.88 m | Sword | 208 | — | — |
| 6 | **The Three Blades** | Sisters of the Silent Forest | Elite | Vampire | 1.76 m | Daggers | 136 each | — | — |
| 7 | **The Drowned Guardian** | Warden of the Second Key | Mini-boss | Maw | 2.30 m | Axe | 378 | — | — |
| 8 | **The Iron Guard** | Kagehira's Shield | Mini-boss | Uriel | 2.30 m | Axe | 357 | — | — |
| 9 | **Commander Hoshu** | The Inner Gate | Mini-boss | Paladin | 2.11 m | Sword | 247 | — | — |
| — | **The Scavenger King** | What the Village Left | Mini-boss | Brute, cold | 2.08 m | Axe | 225 | — | — |

Duels 5 through 9 use the campaign definition rather than bespoke duel tuning.
The Scavenger King is a named campaign foe only; he is not on the duel roster.

---

## 1. Kagachi (Kagehira), the Marsh Serpent

**The final boss.** The warlord who burned Yorune. He and Renzo's father carried
the Black Seal together as two keepers; one kept faith with the villages, the
other with the future. He burned the village because Renzo's father refused to
hand the Seal over, and he has held Aiko for ten years asking one question. He
holds the third key and has held it for a year. He cannot open the door himself,
because the door needs a Kurogawa.

| Spec | Campaign | Duel |
|---|---|---|
| Health | 420 | 480 |
| Posture | 40 | 190, regen 10/s |
| Damage | 14 | resist 0.26 |
| Move speed | 3.9 | — |
| Reach / preferred range | 2.0 m / 3.0 m | — |
| Wind-up | 0.50 s | — |
| Armour / poise / block | 3 / 0.80 / none | — |
| Arena | Temple, night, fog | Temple, night, fog |

- **Movement style:** Spacing. He holds a band and lunges out of it, like a duelist.
- **Moveset, ten attacks** (the largest in the game): Slash ×3, Thrust, Sweep, Guard Break, Dash Strike, Retreat Slash, Parry, Poison Spit.
- **Four phase profiles**, the only enemy with all four. The fight moves through swordsman, warlord, collapsing arena, and an exhausted final duel. A phase changes how he decides, not just his health.
- **Fought at:** mission 88 (Endure, he talks instead of fighting to win), mission 95 (an ambush from the fog), mission 99 (the kill).
- **Taunt:** "Three lives, ninja. How many do you have?"
- **Philosophy:** MASTERY · EVERYTHING YOU HAVE LEARNED.
- **His reason:** "I burned Yorune for every well." With the Seal he is order; without it he is a man with an army, and armies end.
- **Death:** Renzo raises the sword and Aiko says "Don't become like them." He lowers it. Kagehira's last word is "…Weak." He dies on his own final attack.
- **Duel defeat line:** "Finish it, then. Become me." / "No. I am not you."

## 2. Jin Kurogane, the Storm Blade

**The mirror.** Kagehira's finest officer, born in Yorune, who left the village the
year before it burned to serve the warlord. He drew Kagehira the map, then tried to
stop what came, failed, and quit the army. He is the only enemy who fights with
honour, and the only one who spares Renzo, twice. He carries half a broken mask;
the other half is on his face.

| Spec | Campaign | Duel |
|---|---|---|
| Health | 340 | 340 |
| Posture | 40 | 160, regen 11/s |
| Damage | 12 | resist 0.28 |
| Move speed | 4.6 (the fastest large foe) | — |
| Reach / preferred range | 2.0 m / 2.6 m | — |
| Wind-up | 0.42 s | — |
| Armour / poise / block | none / 0.70 / 15% | — |
| Arena | Rainy battlefield | Rainy battlefield, rain |

- **Movement style:** Spacing.
- **Moveset, nine attacks:** Slash ×3, Thrust, Parry, Dash Strike ×2, Sweep, Retreat Slash. His codex line is the design brief: "He never blocks twice the same way."
- **Two phase profiles.** The final phase is all storm.
- **Fought at:** 61 (Endure, unbeatable), 63 (a duel you are meant to lose), 69 (Endure, he fights to teach), 70 (the kill).
- **Taunt:** "Attachments slow the sword. I cut mine away. Show me why you keep yours."
- **Philosophy:** TECHNIQUE · COUNTERS · ADAPTATION.
- **His function in the story:** he states the game's thesis. "If you reach Kagehira, you may become him." / "I am not him." / "Neither was he."
- **Death:** he dies with the half mask in his hand, telling Renzo that Aiko is alive in the mountain fortress. His last words are "Do not become him."

## 3. Goro, the Toll-Captain

**The first named enemy, and the one Renzo kills twice over.** A toll-captain who
made himself warlord of the valley, emptying it into prison camps and running an
execution ground. He was the young raider captain at the door of Renzo's father's
house the night Yorune burned, which the player discovers by fighting him from the
other side in a memory.

| Spec | Campaign | Duel |
|---|---|---|
| Health | 380 | 360 |
| Posture | 40 | 120, regen 10/s |
| Damage | 15 | resist 0.32 |
| Move speed | 2.6 (the slowest boss) | — |
| Reach / preferred range | 2.3 m / 2.4 m | — |
| Wind-up | 0.60 s | — |
| Armour / poise / block | 5 / 0.85 (the highest) / 20% | — |
| Arena | Fortress / castle | Burning village, night |

- **Movement style:** Direct. He walks straight in.
- **Moveset, six attacks:** Heavy Slam ×2, Spin Cleave, Sweep, Guard Break, Dash Strike. Every one is telegraphed and every one lands hard, which is his codex line exactly.
- **Two phase profiles.**
- **The tallest character in the game** at 2.25 m, bare-chested and firelit, deliberately the biggest silhouette on screen.
- **Fought at:** mission 5 (beaten, gives up that Aiko may live), 38 (Endure, he hunts Renzo and then lets him run), 40 (the kill).
- **Taunt:** "Every roof pays. Even yours, little lantern."
- **Philosophy:** POWER · PRESSURE · COMMITMENT.
- **Death:** on his own gate. "The marsh. She's under it."

## 4. The Pale Shade, What the Marsh Kept

**The one villain who does not work for anybody.** It was in the marsh before
Kagehira. It is what the marsh sends when it wants to know things, the assassins
answer to it, and it hunts Renzo through the forest for its own reasons. It is
translucent, sharing the common raider's body without ever reading as it.

| Spec | Campaign | Duel |
|---|---|---|
| Health | 216 | 280 |
| Posture | 39 | 140, regen 12/s (the fastest) |
| Damage | 19.5 | resist 0.30 |
| Move speed | 4.8 (the fastest foe in the game) | — |
| Reach / preferred range | 1.7 m / 1.9 m | — |
| Wind-up | 0.35 s | — |
| Armour / poise / block | none / 0 (staggers on any hit) / none | — |
| Arena | Graveyard | Graveyard, night, fog |

- **Movement style:** Ambush. It circles to Renzo's back before closing.
- **Moveset, six attacks:** Flurry ×3, Dash Strike, Poison Spit, Retreat Slash. Fast, weak per hit, and it never stands still.
- **Three phase profiles.** It dissolves and reforms, so the fight is about where it will be rather than where it is.
- **Its weakness is authored into the codex:** smoke takes them apart.
- **Fought at:** mission 21 (Endure, it withdraws because it was measuring him), 30 (the kill).
- **Taunt:** "…come closer…"
- **Philosophy:** SPEED · DECEPTION · POSITIONING.
- **Death:** dying, it tells him where Aiko went. "…you carry her thread… she carried yours…"

## 5. The Drowned Guardian, Warden of the Second Key

**The villain Renzo's own father set in his way.** An antlered marsh brute in hide
and bone, placed beneath the temple to keep the third key from anyone at all,
including his own children. The hardest-hitting foe in the campaign.

- **Health 378, damage 24, armour 5, poise 0.75, block 30%, posture 60.**
- Move speed 3.2, reach 2.5 m, wind-up 0.55 s. Movement style Direct.
- **Moveset, nine attacks:** Slash ×3, Spin Cleave, Heavy Slam, Thrust, Sweep, Guard Break, Dash Strike.
- It holds back when Renzo bleeds and hits harder when he does not.
- **Fought at mission 59.** Under it is not a key but his father's message.
- **Taunt:** "Your father set me here. He did not say you would come."
- **Philosophy:** ENDURANCE · REACH · REFUSAL.

## 6. The Iron Guard, Kagehira's Shield

**Kagehira's elite, and Goro's men before that.** They appear three times and they
wear Yorune steel, taken from the people on the missing list. Mechanically the
Drowned Guardian's twin, slightly cheaper, in gilded plate rather than bone.

- **Health 357, damage 24, armour 5, poise 0.75, block 30%, posture 60.**
- Same nine-attack moveset and Direct movement as the Drowned Guardian.
- **Fought at:** 74 (the wall's commander), 78 (as a unit, with a captain whose fall breaks them), 94 (Kagehira's strongest, at the summit gate).
- **Taunt:** "The warlord does not see you. I make sure of it."
- **Philosophy:** GUARD · PUNISHMENT · NO GROUND GIVEN.

## 7. Commander Hoshu, the Inner Gate

**The last door before the warlord.** Full dark plate and a great helm. He fights
with the Three Blades' discipline and Goro's strength at once, which is the design
brief for him: everything the campaign has taught, in one body.

- **Health 247, damage 22.5, armour 2, poise 0.5, block 40% (the highest), posture 60.**
- Move speed 2.9, reach 2.4 m, wind-up 0.62 s (the slowest). Movement style Spacing.
- **Moveset, nine attacks:** Slash ×4, Thrust, Parry, Sweep, Guard Break, Dash Strike.
- **Fought twice:** mission 66 as Jin's champion, where he yields rather than dies, and mission 79 at the inner gate, where he dies at the gate itself.
- **Taunt:** "He said you would reach this door. He did not say you would open it."
- **Philosophy:** COMMAND · TIMING · THE LAST DOOR.

## 8. The Convoy Captain, Keeper of the Lantern Road

**The first named enemy in the game.** A disciplined ronin who turns back for his
cargo and meets Renzo on the road. His saddlebag carries the first written mention
of the Black Seal, which is what starts the whole hunt.

- **Health 208, damage 20.7, armour 2, poise 0.5, block 40%, posture 60.**
- Mechanically Commander Hoshu's lighter twin: same nine attacks, same Spacing movement, less health and damage.
- **Fought at mission 2.**
- **Taunt:** "Everything on this road is counted. You were not."
- **Philosophy:** DISCIPLINE · FORMATION · ATTRITION.

## 9. The Three Blades, Sisters of the Silent Forest

**Three assassins who fight as one, and the first enemies sent for Renzo by name.**
Someone above Goro is paying attention. They killed one of Kagehira's own officers
for refusing to take a child north.

- **Health 136 each, damage 13.2, no armour, poise 0, posture 60.**
- Move speed 4.6, reach 1.7 m, wind-up 0.34 s (the fastest wind-up in the game). Scale 0.96, so they read smaller than everything else on screen.
- **Movement style:** Erratic, the only foe that uses it. Fast, unpredictable strafing.
- **Moveset, six attacks:** Slash ×3, Dash Strike, Retreat Slash, Quick Shot. Their codex line: they open with the dash and never trade.
- **The only Elite-rank named foe.** The other eight are mini-boss or boss.
- **Fought at mission 24.** The last of the three fights differently from her sisters.
- **Taunt:** "One for the throat. One for the heart. One to watch."
- **Philosophy:** AMBUSH · ROTATION · PATIENCE.

## 10. The Scavenger King, What the Village Left

**A villain who is not Kagehira's at all.** He made a second burned village his
own. He is proof that Yorune was not the only one, and that the searchers came
here too.

- **Health 225, damage 20.9, armour 4, poise 0.7, block 30%, posture 60.**
- Move speed 2.6, reach 2.2 m, wind-up 0.68 s (the slowest in the game). Movement style Direct.
- **Moveset, six attacks:** Heavy Slam ×2, Slash, Sweep, Guard Break, Dash Strike.
- **Fought at mission 13.** Under the elder's floor is an older map with the marsh temple marked.
- **Taunt:** "They searched too. They found me."
- Not on the duel roster.

---

## Design notes worth knowing

- **No two named foes share a body.** An early pass gave the Drowned Guardian Goro's model in green and Commander Hoshu Jin's armour in bronze; both were replaced with the Maw and the Paladin because a recoloured duplicate reads as a cheat.
- **Pairs share a moveset on purpose.** Drowned Guardian and Iron Guard are one kit at two weights; Commander Hoshu and the Convoy Captain are another. The story separates them, not the code.
- **Poise is the tell.** Goro at 0.85 shrugs off almost everything; the Pale Shade at 0 staggers to any hit. That single number is most of what makes them feel different.
- **Four foes get Endure missions**, where they cannot be beaten and the clock ends the fight instead: the Pale Shade at 21, Goro at 38, Jin at 61, 63 and 69, Kagehira at 88 and 95.
- **One inconsistency in the shipped data:** the Pale Shade's in-mission card reads "WHAT THE FOREST KEPT" while its duel card reads "WHAT THE MARSH KEPT". Both ship. Worth unifying if anyone touches that asset.
