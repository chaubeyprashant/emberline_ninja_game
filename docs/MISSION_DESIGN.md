# Phase 4 — Mission variety and memorable encounters

## Review of the mission plans as they stood

Twelve plans existed under `Resources/Missions`, named after *mission types*
rather than story missions: Assassination, Rescue, Infiltration, Escort, Chase,
Survival, Defense, Stealth, Duel, BossHunt, Escape, Investigation. The framework
that runs them is sound and is not being rebuilt.

The content was the problem. The story catalogue attaches a plan to each of the
ten levels, and eight of the ten attachments contradicted the level:

| Level | Plan it loaded | The plan's own name |
|---|---|---|
| 1 FIRST BLOOD | 01_Assassination | A NAME ON A LIST |
| 2 THE LANTERN ROAD | 04_Escort | THE LANTERN ROAD ✓ |
| 3 EYES IN THE DARK | 08_Stealth | EYES IN THE DARK ✓ |
| 4 GORO'S TOLL | 07_Defense | THE LAST POST |
| 5 THE SERPENT'S TRAIL | 12_Investigation | WHAT THE WATER KEPT |
| 6 INTO THE REEDS | 06_Survival | UNTIL THE BELLS |
| 7 THE DROWNED ROAD | 02_Rescue | WHAT THEY TOOK |
| 8 TWIN LANTERNS | 09_Duel | ONE BLADE, ONE ROAD |
| 9 THE SERPENT'S GUARD | 11_Escape | NOTHING BUT THE WAY OUT |
| 10 KAGACHI | 10_BossHunt | THE SERPENT'S COIL |

Consequences that a player would actually notice:

- **GORO'S TOLL contained no Goro.** It ran a generic two-stage lantern defence.
- **THE DROWNED ROAD was a rooftop rescue** with no water in it.
- **TWIN LANTERNS was a single duel** with no second objective and no split.
- **KAGACHI had one boss stage**, so the fight had no phases at the mission level.
- Briefing text, mission name and debrief all came from the plan, so the level
  select promised one mission and the briefing screen delivered another.

Two plans (03_Infiltration, 05_Chase) were never reachable at all.

Structurally the plans were also samey: six of twelve opened with the same beat
and four ran the shape the brief forbids, a walk into three fights.

## The ten missions

Each is built from the existing stage goals. New mechanics were added only where
an identity could not be expressed without them, and are marked **new**.

### 1. FIRST BLOOD — the lesson
- **Identity** the only mission where an enemy has not noticed you yet, and the
  game says so. It teaches stealth by giving you one free kill and then taking
  the option away.
- **Primary** kill the raider watching the east terrace, then survive what his
  shout brings.
- **Optional** take him silently *(SilentKill)*.
- **Composition** raiders only. One archer at the end, at distance, as a promise.
- **Environment** intact terraces, one burned home, a missing-person notice.
- **Special event** the alarm, fired by the plan after the kill: the quiet half
  of the mission ends on a scripted beat rather than on the player's mistake.
- **Climax** two raiders and an archer at the lantern line, in the open.
- **Story** the notice names a girl nobody has found. The raiders are searching,
  not looting.

### 2. THE LANTERN ROAD — the walk
- **Identity** you are not the objective. An old man carrying a flame is, and he
  keeps walking whether or not you are ready.
- **Primary** walk Yotsu to the temple gate.
- **Optional** no villager dies *(NoCivilianDeaths)* **new**.
- **Composition** patrols of raiders and pike guards, then an ambush of assassins.
- **Environment** night, lantern posts, a destroyed cart, villagers hiding in
  doorways who scatter when a fight starts.
- **Special event** an ambush behind the bearer **new**, mid-walk, plus rain.
- **Climax** the last post, fought with the bearer exposed behind you.
- **Story** the raiders cut lantern posts rather than take them. Someone wants
  the road dark.

### 3. EYES IN THE DARK — the held breath
- **Identity** the mission you can lose by being seen. Vision is short, the rain
  covers your noise, and the threat is at range.
- **Primary** clear the roofline without raising the alarm, then silence the
  spotter.
- **Optional** finish with no alarm at all *(NoAlarm)* **new**.
- **Composition** archers and a rogue ninja. Nothing that charges you.
- **Environment** night, rain, dead lanterns, the first Kagehira banner.
- **Special event** the lanterns go out halfway, cutting sight further.
- **Climax** the spotter, covered by two archers on separate roofs.
- **Story** the banner is the first thing in the game that names the enemy.

### 4. GORO'S TOLL — the wall
- **Identity** the first mission that is simply harder than you are, and the
  first named enemy. A checkpoint that takes payment in people.
- **Primary** break the toll and put Goro down.
- **Optional** free every prisoner at the post *(SaveAllPrisoners)* **new**.
- **Composition** pike guards and axe raiders, then Goro with two adds.
- **Environment** a barricade, destroyed carts, a prisoner pen, Kagehira banners.
- **Special event** Goro arrives after the guard breaks.
- **Climax** Goro in a barricaded space with no room to retreat.
- **Story** the toll ledger records people, not coin.

### 5. THE SERPENT'S TRAIL — the hunt
- **Identity** the mission with no enemies for its first minute. You are reading
  the ground, not clearing a room.
- **Primary** track the raiding party and find who they answer to.
- **Optional** do it without raising the alarm *(NoAlarm)*.
- **Composition** a fleeing scout, then the ambush he was running toward.
- **Environment** marsh, drag marks, a dropped lantern, one blood trail.
- **Special event** the scout breaks and runs when you close.
- **Climax** the ambush at the end of the trail.
- **Story** the orders in his coat name Kagehira. This is the mission that turns
  a bandit problem into a war.

### 6. INTO THE REEDS — the blind mission
- **Identity** you cannot see. Standing still and listening is a mechanic, not a
  mood.
- **Primary** survive the reeds and clear what is hunting you.
- **Optional** get out under the clock *(UnderTime)* **new**.
- **Composition** shades, almost exclusively. One bomber to punish standing still
  too long.
- **Environment** heavy fog, reeds, drowned lanterns.
- **Special event** the fog rolls in **new**; holding still pings nearby unseen
  enemies **new**.
- **Climax** the fog thins and everything that was circling you is close.
- **Story** the shades are not native to the marsh. They were made here.

### 7. THE DROWNED ROAD — the tide
- **Identity** the ground itself is the enemy. The arena changes twice while you
  are standing in it.
- **Primary** cross the causeway before the water closes it.
- **Optional** cross under the clock *(UnderTime)*.
- **Composition** ambushers who come out of the water, and a pike guard line.
- **Environment** flooded road, drowned carts, floating lanterns.
- **Special event** the water rises twice, the second time mid-fight.
- **Climax** the last stretch, fought in rising water.
- **Story** the carts were left where they stood. Nothing was stolen but light.

### 8. TWIN LANTERNS — the choice
- **Identity** the only mission with two objectives and a real order of
  operations. Whichever lantern you light first, the other is defended better.
- **Primary** light both lanterns and open the bridge.
- **Optional** light the second one without raising the alarm *(NoAlarm)*.
- **Composition** east route archers and a pike line; west route assassins and a
  rogue ninja. You always fight both, in the order you chose.
- **Environment** two towers, a bridge, Kagehira banners on both.
- **Special event** the route you leave for later wakes up **new**.
- **Climax** a samurai on the bridge once both lanterns burn.
- **Story** the lanterns are a signal. Lighting them tells someone you are coming.

### 9. THE SERPENT'S GUARD — the fortress
- **Identity** the mission that asks how you want to play it, at the gate, once,
  and then holds you to it.
- **Primary** get inside the fortress and reach the armoury.
- **Optional** never raise the alarm *(NoAlarm)*.
- **Composition** the drain route is two unaware elites; the gate route is four
  awake guards. Then elites and support either way.
- **Environment** fortress walls, banners, an armoury of abandoned weapons, a
  prisoner pen.
- **Special event** the choice of entrances **new**; reinforcements if you took
  the gate.
- **Climax** two elites with archer support at the inner gate.
- **Story** the abandoned weapons are Yorune's. This is where the missing went.

### 10. KAGACHI — the payoff
- **Identity** a boss with mission-level phases: the arena changes twice while he
  is alive, and he does not have to die for the mission to move.
- **Primary** kill Kagachi.
- **Optional** finish inside the time limit *(UnderTime)*.
- **Composition** Kagachi, with two waves of chosen between phases.
- **Environment** the coil, a hundred drowned lanterns arranged in a spiral.
- **Special event** phase breaks at three quarters and at two fifths of his
  health **new**, each with an arena change: the water rises, then the lanterns
  go out.
- **Climax** the third phase, in the dark, in the water.
- **Story** he is what the marsh was for. The lanterns were the point.

## Pacing

Every plan is authored as quiet → tension → discovery → combat → quiet →
escalation → climax → resolution. In practice that means each mission opens with
a stage that spawns nothing, keeps at least one further no-combat beat past the
midpoint, and ends on a beat that is not a fight. The mission validator enforces
the opening and the mid-mission breather so this cannot quietly rot.

## Optional objectives

Optional objectives were stages you could skip. Skippable stages are chores; the
brief asked for objectives that change how you play the mission you are already
in. They are now *conditions* evaluated across the whole mission, declared on the
plan, shown in the briefing and tracked live in the HUD.

| Challenge | Fails when |
|---|---|
| NoAlarm | the alarm is raised, once, for any reason |
| SaveAllPrisoners | any prisoner is still caged when the mission ends |
| NoCivilianDeaths | a villager dies, whoever killed them |
| SilentKill | the marked target is killed after it has noticed you |
| UnderTime | the mission clock passes the limit |

Rewards stay inside the existing shard economy: a challenge pays two or three
shards, against three to five for the mission itself. Meaningful, not mandatory.
