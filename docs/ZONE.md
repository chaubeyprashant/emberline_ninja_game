# The open mission zone

The first real place in Emberline. Built by `Emberline/Build Zone`
(`EmberZone.Build`), which regenerates `Assets/Scenes/Zone.unity` from code like
everything else in the project. Reachable in-game from the **ZONE** button on the
home screen.

## What replaced what

| Before | After |
|---|---|
| `Deck`: one 130 x 130 m cube, flat, hard rectangular edge | `EmberTerrain`: a chunked flat-shaded height mesh, 200 m across |
| `Parapet` x4: cube walls at +/-64.6 m with colliders — the cage | Nothing. A mountain ring rises from 78 m out to 30 m tall |
| Chimneys, huts, carts, reeds as `CreatePrimitive` cubes | 229 imported models across four CC0 kits |
| KayKit **dungeon interior** props scattered outdoors | Forest, village, camp and shrine props chosen for outdoors |
| One flat arena re-lit 11 ways | One valley with distinct places in it |

The cage is gone because the geometry that made it is gone, not because a
movement clamp was loosened. `MissionBounds` was never the cause: it was already
a soft elliptical push-back and it is reused here at 76 m, just inside the
mountains, so it only ever engages where the player is already walking into rock.

## Layout

North is +Z. The valley floor is roughly 156 m across.

```
                        MOUNTAIN RING  (78 -> 100 m, up to 30 m tall)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        ~                    NORTH FOREST                      ~
        ~        torii gate  ->  road from the north           ~
   R    ~                                                      ~
   I    ~   ruin        .        ROAD                          ~
   V    ~                 \                                    ~
   E    ~   shrine +        \                                  ~
   R    ~   second torii      VILLAGE  (well, forge, market,   ~
        ~                      mills, farm plots, lanterns)    ~
   bridge~                        \                            ~
        ~                          \                           ~
        ~        SOUTH FOREST       \      woodcutters         ~
        ~                            \                         ~
        ~                             ENEMY CAMP  (plateau,    ~
        ~                             palisade, towers, pen)   ~
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
```

Landmarks are constants on `EmberTerrain` (`VillageCentre`, `CampCentre`,
`RoadPts`, `RiverCentreX`) so the terrain, the scatter masks and the prop
placement all read the same numbers.

## Approaches to the camp

The camp sits on a plateau lifted 9.5 m, ringed by a palisade that is
deliberately incomplete. Five ways in, and the geometry is what creates them:

| Approach | What makes it possible |
|---|---|
| **Front** | The road climbs to a gated section of wall at bearing 311 degrees |
| **Flank** | The palisade is simply absent between bearings 95 and 200 |
| **Forest** | The south forest band runs to within a short dash of the wall |
| **Rear** | The prisoner pen and the supply dump are at the back, away from the gate |
| **High ground** | The ridge north-east of the plateau overlooks the whole camp |

The towers stand where they can see the road. The supply dump is piled at the
back, so sabotage and a frontal assault are not the same walk.

## Rendering and budget

| | |
|---|---|
| Triangles in scene | ~289,000 total, chunked and frustum-culled |
| Terrain | 13,000 tris across 64 chunks, one material |
| Materials, whole zone | 21 — 17 flat-colour nature + 3 kit atlases + terrain |
| Textures | Three 512 repainted atlases + one 64px terrain palette |
| Colliders | Authored only: capsules for trunks, boxes for structures, none for grass |

Trees, rocks and structures cast shadows; grass, flowers and mushrooms do not.
Terrain receives shadows but does not cast them.

## Style: how the assets were made to match

The kits do not ship in Emberline's palette and were not used as they arrived.

- **Kenney Nature Kit** carries no texture at all — its models reference named
  flat-colour materials. Those names are mapped to Emberline colours in
  `EmberKenneyAssets.Repaint`, so `grass` 0.17,0.85,0.72 becomes 0.185,0.270,0.205
  and the whole forest resolves to shared materials in the game's own palette.
- **The textured kits** have their `colormap.png` genuinely repainted on import:
  pulled 70% toward luminance, highlight-compressed, then cooled. Hues in the
  amber band keep more chroma, because lantern orange is the one saturated colour
  the art direction allows. Originals are kept unmodified alongside.
- Everything renders through `Emberline/Surface`, the same shader as the cast,
  under the same trilight ambient and three-point rig.

## The East-Asian architecture gap

No CC0, low-poly, modular, directly-downloadable Japanese architecture pack
exists. Kenney, KayKit, Quaternius, OpenGameArt and Poly Pizza were all searched.

So Emberline builds its own. `EmberZoneDressing.Torii` and `.Shrine` assemble the
two road gates and the wayshrine from Fantasy Town timber: two uprights, a
lintel, a ridge beam, and a raised platform under a high roof with lanterns
either side. This keeps the entire zone on one set of materials and gives the
game its own silhouette rather than a borrowed one.

## Missions are fought here

`BuildScene` no longer calls `BuildArena`. Both gameplay scenes build this
valley, so Rooftop and Marsh are the same place under different light and
weather rather than two identical decks. All 100 campaign missions run in it.

Moving them onto real ground meant removing one assumption that ran through the
whole game: **the floor was a plane at y = 0.**

| What assumed a flat deck | What it does now |
|---|---|
| `EnemyBrain.Launched` meant `y > 0.02` | measured against the ground beneath the enemy |
| a juggled body landed at `y = 0` | lands on the surface under it |
| walking never touched `y` at all | `StickToGround` on the path every update already ends on |
| villagers/prisoners/bearers walked a plane | a `GroundHug` component, added once in `CivilianRig` |
| falling through reset the player to `y = 0.5` | recovers onto the floor, plus half a metre |
| the out-of-bounds net clamped `y` to `0.5` | clamps to the ground there |
| enemy spawns came out of `MissionBounds` at `y = 0` | snapped to the surface |

`Core.ZoneTerrain` holds the height field in runtime code and `EmberTerrain`
delegates to it, so the mesh and the AI cannot disagree. `Core.Ground` is the one
place that answers "how high is the floor", and it returns 0 when no valley is
loaded — which is why the endless Road North corridor and the opening are
untouched.

### The arena inside the world

A 60 m play radius cost nothing on an empty deck. In the valley it reached deep
into the treeline, so a fight became a scramble between trunks. The play area is
**34 m** now, the treeline starts at 40 m instead of 26 m, and nothing with a
collider scatters inside 36 m.

The village had to open up too. Every mission centres on the origin — the player
spawns there and objectives and clues appear around it — so the origin has to be
a fighting floor. The houses ring a **20 m green** (`PlazaRadius`), which is a
better village and a far better place to fight.

### Verified

The playthrough bot completes missions 1 to 6 in the valley, optional objectives
earned, including the Goro boss fight, the prisoner rescue and the chase.

## Known gaps

- Only missions 1 to 6 have been bot-verified in the valley. A full sweep of all
  100 has not been run.
- Every mission is still sited at the origin. The camp, the river and the forest
  are places you can walk to, but no mission is yet *staged* at one of them.
- The **ZONE** button on the home screen must be removed before the next store
  upload.
