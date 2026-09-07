# Emberline environment audit

Written before any new asset was downloaded, to establish what the world is
made of today and what "matches the characters" actually means in numbers.

## 1. What is trapping Renzo

The cage is **real geometry, not a movement clamp**. `EmberlineBootstrap.BuildArena`
builds it:

| Object | Primitive | Size | Position |
|---|---|---|---|
| `Deck` | Cube | 130 x 0.5 x 130 m | y = -0.25 |
| `Parapet` x4 | Cube | 130 x 0.8 x 0.6 m | +/-64.6 m on X and Z |

The four parapets form a literal rectangular box 129 m across with colliders on
every side. The floor is one flat cube with a hard rectangular edge, which is the
straight horizon line visible in play.

`MissionBounds` is **not** the cause and does not need replacing. It is already a
60 m ellipse with three zones: free movement inside 85% of the radius, a quadratic
soft push-back to the edge, and a safety teleport only past 1.35x radius. It is
soft, round, and configurable per mission. It sits well inside the parapets, so
the player usually feels the soft push before touching the box.

Two separate fixes are therefore needed, and only the first is urgent:
1. Delete the parapet ring and the flat deck cube, and replace them with terrain.
2. Give missions a real per-mission radius. Every generated mission currently
   serialises `missionRadius: 0`, which `MissionBounds` documents as "use the 60 m
   default", so all 100 missions share one circle size.

## 2. What the world is made of

Everything in the playable space is `GameObject.CreatePrimitive`:

- Cubes: deck, parapets, roof ridges, chimneys, huts, cart beds, lantern posts and bulbs, reeds
- Cylinders: water pools, cart wheels
- Spheres: marsh wisps

There is **no terrain, no vegetation, no building, and no Japanese architecture**
anywhere in the project.

The only imported environment art is one pack: **KayKit Dungeon Remastered**,
584 KB, at `Assets/Art/Environments/Dungeon/`, 14 props wired into
`Resources/Props/Dressing`:

    rubble_large, rubble_half, crates_stacked, box_large, box_small,
    barrel_large, barrel_small, keg, chest, table_small,
    banner_red, banner_thin_red, column, torch_lit

These are **dungeon interior** props. They are what the brief calls "strange
objects unrelated to the game": red heraldic banners hanging in mid-air over an
outdoor rooftop, treasure chests and beer kegs on a roof ridge, a stone column in
a marsh. The props themselves are fine work and match the characters; they are
simply the wrong set for outdoors.

## 3. Only two scenes exist

`ShippedScenes` is `Opening`, `Rooftop`, `Marsh`. All 100 campaign missions run in
**one of two arenas**, re-lit through `EnvThemeId` (11 themes: Village, Forest,
Bamboo, Mountain, Temple, Castle, Fortress, Graveyard, BurningVillage,
RainyBattlefield, VillageDawn) and re-dressed by `MissionDressing`.

So a "Forest" mission and a "Castle" mission are the same flat 130 m deck with
different fog colour and different dungeon props scattered on it. That is the
root of "it looks like a test scene rather than a real game world" - because
structurally it is one test scene wearing 11 colour filters.

## 4. The style the environment has to match

Anything imported has to survive next to the existing cast, which is KayKit:
chunky low-poly, flat-ish shading, one hand-painted texture atlas per pack, no
normal maps.

Rendering, from `Assets/Shaders/EmberSurface.shader` and `BuildLighting`:

| Property | Value |
|---|---|
| Pipeline | Built-in RP, Android, Unity 6000.5.9f1 |
| Main shader | `Emberline/Surface` - mobile-lean GGX + Lambert, real shadow receive |
| Extras | rim light, procedural triplanar "wear" grime, no per-asset detail maps |
| Ambient | Trilight: cool sky, mid equator, warm ground bounce |
| Fog | Exponential-squared, per-theme colour and density |
| Sky | `Skybox/Procedural`, one tinted material per theme, sun size 0 |
| Lights | Three-point rig: casting key, cool opposite fill, low back-light |
| Skinning | GPU |

Palette is dark and desaturated: blue-grey stone around 0.15-0.24, warm lantern
orange as the only saturated accent.

**Selection rule that follows from this**: imported assets must be low-poly with a
single shared texture atlas, so one material covers a whole pack and the shared
`Emberline/Surface` material keeps draw calls flat. Photoreal PBR scan assets
(most of Poly Haven's model library) are rejected on style, not quality - they
would make the characters look like cartoons pasted into a photograph. Poly Haven
remains useful for skybox HDRIs only.

## 5. Consequences for the build

- One atlas per pack, one material per pack, GPU instancing on repeated nature props.
- Colliders must be authored, not `Mesh Collider` on every tree - a forest of
  convex mesh colliders is the fastest way to lose the frame budget on an A33.
- LODs are not shipped by the CC0 low-poly packs; density control and draw
  distance do that job instead.
