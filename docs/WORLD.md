# The region — how the open world is built (Phase 5)

Phase 5 of `docs/OPEN_WORLD_ROADMAP.md`. The valley the campaign is fought in
(`docs/ZONE.md`) is now the centre of an 800 m region, streamed around the
player at runtime, with a clock, weather, named places to find, and things to
use. Entered from **THE VALLEY** on the home screen, which replaced the ZONE
test button.

## One function, three consumers

`Core/ZoneTerrain.HeightAt` is still the only definition of the ground. The
editor bakes the inner 200 m through it (`EmberTerrain` → `Core/TerrainMesh`),
the streamer builds every outer 25 m chunk through the same `TerrainMesh`, and
every mover asks it for the floor. Nothing about the inner valley changed
inside the 78 m ring, so all hundred missions and the play bot are untouched;
the ring itself gained three passes and two river gorges, and beyond it:

| Place | Where | What the terrain does |
|---|---|---|
| The North Pass | (4, 92) | the road climbs a saddle through the ring and switchbacks to the region's edge |
| The Pilgrim Pass | (96, 58) | the east road out to the temple |
| The Toll Gap | (70, −92) | past the camp, down to the marsh |
| The Falls | (−52, 118) | the river enters 8 m higher and drops at the lip; the upper river is at −1.5 + 8 |
| The Hill Temple | (250, 60) | a 22 m rise with a flat crown, moss on the slopes |
| Kagehira's Stronghold | (240, 250) | an 18 m plateau, 45 m across |
| The Hollow (cave mouth) | (160, 200) | a 5 m sunken bowl under the ridge |
| The Drowned Marsh | (40, −290), r 120 | flat wet ground at the water line with pools below it |
| Kawai Hamlet | (−250, −150) | a flattened floor for three farms |
| The Shinobi Hollow | (−290, 210) | a clearing in the thickest wood |
| The edge | beyond 330 m | mountains rising 60 m on every side |

Six road polylines leave the valley; `ZoneTerrain.RoadLines` exposes them.
Outside the ring a road is flattened toward the low-frequency ground so it
climbs rather than trenches. The palette gained marsh, snow and moss cells;
`EmberTerrain.EnsureMaterial` rewrites the atlas on every build.

**Verify the shape** with `Emberline/Render Region Map` (`Logs/region_map.png`,
one pixel a metre, roads in cream, landmark rings in ember) and
`Emberline/Check World`, which asserts the fighting floor is unchanged, every
road is walkable, every pass is open, every landmark floor is flat and dry, the
falls drop, the marsh is wet, and the edge is a wall.

## Streaming

`Core/WorldStreamer` (on the `World` object the scene builder adds) keeps the
25 m chunks within 175 m of the player: terrain mesh, mesh collider, a water
quad where the chunk dips below the water line, scatter from a seed per chunk
(`Core/WorldScatter`: pines in noise bands and around the hideout, dead wood
and reeds in the marsh, rocks by slope), and a landmark's dressing when its
chunk arrives. Grass and reeds are a separate layer built only within 70 m. Two
chunks are built a frame, nearest first; chunks are dropped a ring beyond the
load radius so the horizon never pops in front of the player. Baked valley
chunks are skipped.

Budget: a forest chunk holds ten or so trees; at the load radius that is
roughly 600 trees, 200 rocks and 1,100 grass tufts alive, each an authored
capsule or box collider, none a mesh collider.

## Places

`Core/WorldLandmarks` is the table: id, name, blurb, kind, centre (read from
`ZoneTerrain`), discovery radius. Walking into one for the first time announces
it, and it stays discovered in the world state. The explore objective line
points at the nearest place not yet found: "SOMEWHERE UNSEEN — NE 240 m".

`Core/WorldDressing` builds each kind from the same kits as the valley: the
temple (lantern approach, two torii, a colonnaded hall with stacked roofs, a
shrine to pray at), the stronghold (a walled ring with a gate toward the road,
four towers, a yard of tents, forge, racks and loot), the hollow (a rock ring
with two leaning slabs, a fire), the drowned shrine in the marsh, the hamlet
(three houses, a well, a mill, fields, fences, a fire), the shinobi hollow
(tents, fire, targets), the falls (rock at the lip, a white face, mist), and a
watchtower and signpost at each pass. These are first passes: Phase 8 replaces
the timber with a Japanese kit, Phase 6 puts people in them.

## Using things

`Core/Interactable`: a verb, a radius, a callback. The HUD asks for the nearest
one ten times a second and the JUMP button becomes it — glyph, ember tint and a
caption (PRAY, REST) — so there is no separate interact button. `Shrine` heals,
mends every Gate and saves; `Campfire` does the same and sleeps to the next
dusk or dawn, which is how you choose night for an approach. The valley's
wayshrine and the woodcutters' fire carry them; the temple, the drowned shrine,
the hollow, the hamlet and the hideout carry their own.

## The clock and the weather

`Core/TimeOfDay`: 24 hours in 24 real minutes, running only in explore. The
scenes are authored at their theme's hour (night), so the clock modulates the
theme's own key light, ambient, fog and sky rather than replacing them: noon is
2.3× the key at a warm white, dawn and dusk warm the colour, and night is never
darker than the theme was. `Visibility.DaylightScale` goes to 0.55 at midnight.
A mission holds the clock, so nothing validated changes.

`Core/WeatherSystem`: clear, cloudy, rain, storm, fog. A random walk a step at a
time every three to six minutes with twenty-second transitions; each state is a
set of multipliers on light and fog, rain through `Atmosphere.SetRain01` (built
on demand), lightning in a storm, `Visibility.WeatherScale` (0.55 in fog), and
`NoiseSystem.Damping` (rain covers footsteps, 0.55 in a storm).

## What the world remembers

`Core/WorldState`: one versioned JSON blob under one PlayerPrefs key —
position, hour, weather, discovered places. Saved at a shrine, a fire, on
discovery, every minute, and when leaving to the menu; loaded when THE VALLEY
opens. It is the first piece of the versioned save the roadmap asks for; the
campaign's keys are untouched.

## Explore mode

`LaunchMode.Explore`: no plan, no waves, `MissionBounds.Unbounded` so the
soft arena push-back stands down (the edge mountains are the boundary), the
clock and weather running, straight into play with no briefing. Pause → LEAVE
returns to the menu and saves.

## Not in this slice

People (Phase 6), missions staged at the landmarks (Phase 7), the horse, boats,
a real cave interior, and the `World.unity` consolidation — both gameplay
scenes stream the same region, so the consolidation is a cleanup rather than a
feature.
