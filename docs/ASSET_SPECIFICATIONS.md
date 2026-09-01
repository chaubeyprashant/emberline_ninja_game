# Emberline — Realistic Asset Specifications

Commissioning document for the character, weapon and environment art that
replaces the current placeholder set. Written against the engine as built, so
anything delivered to this spec drops in without gameplay changes.

**Status: no replacement asset exists yet.** Everything shipping today is a
recoloured KayKit chibi model. The visual identity is *not* realistic and will
not become realistic until these assets are produced. This document exists so
that work can be commissioned precisely.

Target hardware: Samsung Galaxy A33 5G class (Mali-G68 MP4, 6 GB). Unity
6000.5.9f1, **Built-in Render Pipeline**, IL2CPP, ARM64.

---

## 0. The integration contract

Read this before modelling anything. It is what makes a model droppable.

**What the game does NOT care about.** Combat, AI, health, hitboxes, weapons,
mission logic and camera are all independent of the mesh:

- There are **no hit colliders on characters**. `CombatController.CollectTargets`
  iterates a registry and filters by distance and angle from the root
  transform. Damage is geometry-independent. A new mesh cannot break hit
  detection.
- Height is **auto-normalised**: the importer measures the skinned bounds and
  scales the model to the spec's height. Model units and scene scale do not
  need to match.
- Animation is **indirected**: the game asks for a `RigPose`, and a per-model
  table maps that pose to a clip name in the FBX. Clip names are yours to
  choose; you declare the mapping.

**What the model MUST provide:**

| Requirement | Why |
|---|---|
| A humanoid skeleton, Unity **Humanoid** rig | Shared retargeting across the roster |
| Bone named `hand.r` (or `handslot.r`) | Right-hand weapon socket |
| Bone named `hand.l` (or `handslot.l`) | Left-hand socket (dual wield, bombs) |
| Forward = **+Z**, up = **+Y**, origin at **floor between the feet** | Facing, spawning, ground clamp |
| All visuals under a **single root child** | Swap boundary — see §9 |
| The 12 clips in §3 | The pose set the game drives |

Deliver as **FBX 2020**, metres, Y-up, with textures as separate PNGs (not
embedded).

---

## 1. Player — Renzo Kurogawa

The hero model. Seen closest and most often; gets the largest budget.

**Identity.** Adult male, 27. Athletic, realistic proportions — **7.5 heads
tall**. Serious, composed expression. He is not a brawler and not a
superhero: lean, functional, carrying visible mileage.

**Silhouette.** Read him at 20 m from the back three-quarter view the gameplay
camera uses. Distinctive: hood down by default, high collar, wrapped forearms,
sword at the left hip, a subtle asymmetry from the shoulder strap.

**Costume.**
- Dark layered ninja clothing — charcoal, ink-blue, near-black. Value range
  0.10–0.28 albedo. Never pure black (it kills the rim light).
- Cloth wraps at forearms and shins, frayed at the ends.
- Leather straps: chest rig, belt, thigh strap. Worn edges, not new.
- Small metal protection only — forearm guards and a few buckles. No plate.
- **Aiko's red bracelet on the left wrist.** Thin red cord, slightly frayed.
  This is a story-critical prop and must be visible in an over-the-shoulder
  shot. It is the one saturated colour on the model.
- Dirt and wear concentrated at knees, elbows, hem and boots. Road grime, not
  battle damage.

**Explicitly not:** oversized head, chibi proportions, spiky anime hair, cape,
glowing anything, exposed midriff, bright colours beyond the bracelet.

| | |
|---|---|
| Triangles LOD0 | **22,000** (hard cap 24,000) |
| Bones | ≤ **52**, humanoid; no finger bones past the first joint |
| Material slots | **2** — body (opaque), hair/cloth fringe (alpha clip) |
| Textures | 1× **1024²** albedo, 1× 1024² normal, 1× 1024² ORM |
| Blendshapes | None — see §3 note on faces |

---

## 2. Enemies — 10 specifications

Every one must be identifiable **by silhouette alone**, at 20 m, in fog, in
one frame. That is the design requirement; the costume detail serves it.

Shared rules: humanoid rig per §0, same texture set structure, values in the
0.12–0.45 albedo band, one accent colour each for instant read.

| # | Enemy | Silhouette read | Costume | Tris | Textures |
|---|---|---|---|---|---|
| 1 | **Raider** | Baseline human, nothing exaggerated | Rough soldier clothing, mismatched layers, worn straight sword, cloth head wrap | 9,000 | 1024 set |
| 2 | **Assassin** | **Narrow**, tallest-thin, low stance | Light close-fitting clothing, no armour, twin short blades held reverse-grip, face covered | 9,000 | 1024 set |
| 3 | **Pike Guard** | **Wide horizontal** — the polearm is the silhouette | Defensive armour: chest plate, shoulder guards, long polearm held across the body | 11,000 | 1024 set |
| 4 | **Archer** | **Asymmetric** — bow arm out, quiver above shoulder | Layered cloth, light leather bracer, longbow, back quiver | 9,500 | 1024 set |
| 5 | **Axe Raider** | **Top-heavy**, wide shoulders | Heavy body protection, fur/leather mantle, large single-bit axe | 11,000 | 1024 set |
| 6 | **Samurai** | **Boxy, formal** — the most structured shape | Distinct lamellar armour, sode shoulder plates, kabuto-like helm, katana | 12,000 | 1024 set |
| 7 | **Rogue Ninja** | Reads as **the player's mirror** | Dark stealth equipment, hood up, wrapped limbs, short blade — deliberately close to Renzo but colder and cleaner | 10,000 | 1024 set |
| 8 | **Elite Warrior** | **Full and heavy** but agile | High-quality armour, layered plate over mail, decorated blade, no rust | 13,000 | 1024 set |
| 9 | **Goro** (mini-boss) | **Largest mass** in the game — 1.25× human height, huge | Large intimidating warrior, scarred, minimal armour over bulk, oversized weapon | 16,000 | 2048 set |
| 10 | **Jin** (boss) | **Lean expert duelist** — the calmest silhouette | Sparse, precise clothing; no armour; one perfect blade; reads as skill, not power | 15,000 | 2048 set |

**Kagachi / Kagehira** (final boss, warlord) — separate and above the ten:
visually dominant. Long coat or war-cloak that moves, elevated shoulder line,
mask or partial face covering, the only character permitted an ornamented
weapon and a second accent colour. **18,000 tris, 2048 set.**

Two further enemies exist in code and reuse the above where art is not
commissioned: **Shade** (marsh spirit — may stay stylised/translucent by
design) and **Powder Carrier** (bomber — Raider base with satchel).

---

## 3. Required animation list

The game drives exactly **12 poses** (`RigPose`). Every character must supply
a clip for each; the FBX clip names are yours, declared in the model's spec
table.

| Pose | Purpose | Length | Loop | Notes |
|---|---|---|---|---|
| `Idle` | Standing, weapon ready | 2–4 s | Yes | Breathing; must read as alert, not relaxed |
| `Run` | Locomotion | 0.6–0.9 s | Yes | Blended by speed; no root motion |
| `Strike1` | Light attack 1 | 0.35 s | No | Impact at ~40% |
| `Strike2` | Light attack 2 | 0.35 s | No | Opposite side to Strike1 |
| `Strike3` | Light attack 3 (finisher) | 0.5 s | No | Heavier; this one launches |
| `Cleave` | Heavy attack swing | 0.6 s | No | Follows `Windup` |
| `Windup` | Heavy attack telegraph | 0.4 s | No | **Must read at 20 m** — the fight depends on it |
| `Hurt` | Flinch | 0.3 s | No | Additive-safe upper body |
| `Dash` | Dodge / lunge | 0.3 s | No | Also used for enemy dash attacks |
| `Dead` | Death | 1.2 s | No | Settles to floor; no ragdoll in engine |
| `Spawn` | Entrance | 1.0 s | No | Enemies only; player uses `Idle` |
| `Taunt` | Boss intro / alert | 1.5 s | No | Used by the boss intro camera |

**Root motion is disabled.** All movement is code-driven; animations must not
translate the root.

**No facial animation.** There is no face rig, no blendshapes, and the camera
never holds on a face — story beats are staged in silhouette and
over-the-shoulder deliberately. Do not budget for facial work.

---

## 4. Weapon assets

Weapons are separate FBX props socketed to `hand.r` / `hand.l`. They are not
skinned.

| Weapon | Description | Tris | Texture |
|---|---|---|---|
| Ember Katana | Realistic katana — the hero prop, seen most | 1,800 | 512 set |
| Storm Tanto | Short blade, plain fittings | 900 | 512 set |
| Marsh Hook | Hooked blade, rope wrap, marsh-stained | 1,400 | 512 set |
| Twin Daggers | Pair, matched | 700 each | shared 512 |
| Smoke Bomb | Small ceramic sphere, fuse | 400 | 256 set |
| Hand Crossbow | Compact, wood and iron | 1,600 | 512 set |
| Enemy sword / axe / spear / bow | One each, worn | 900–1,500 | shared 512 atlas |

Rules: origin at the **grip**, blade along **+Z**, no baked hand. Realistic
proportions — a katana is 100–105 cm blade, not a slab. Metal must be
believable steel: roughness 0.25–0.4, not mirror.

---

## 5. Environment asset list

Requirement: **the player recognises the location without reading the mission
name.** Today there are two arena scenes reused for ten story missions, which
is the single largest reason the game reads as generic.

Ten distinct locations, one per story mission:

| # | Mission | Location identity | Key architecture / props | Terrain | Weather / light |
|---|---|---|---|---|---|
| 1 | First Blood | Village rooftops at dusk | Tiled roofs, lantern lines, drying frames | Timber decking | Dusk, warm lanterns |
| 2 | The Lantern Road | Mountain road, lantern posts | Stone shrine markers, carts, rope bridge | Packed earth, gravel | Night, mist |
| 3 | Eyes in the Dark | Walled compound interior | Screens, courtyards, storehouses | Flagstone | Dark, few lights (stealth) |
| 4 | Goro's Toll | Bridge / toll gate | Heavy timber gate, barricades, braziers | Wet stone | Night, firelight |
| 5 | The Serpent's Trail | Bamboo forest | Dense bamboo, narrow paths, shrine rope | Leaf litter | Overcast, wind |
| 6 | Into the Reeds | Marsh edge | Boardwalks, stilt huts, fish racks | Shallow water, mud | Fog, cold light |
| 7 | The Drowned Road | Flooded causeway | Half-sunk structures, drowned carts | Standing water | Rain, heavy fog |
| 8 | Twin Lanterns | Temple grounds | Temple hall, stone steps, two great lanterns | Stone, moss | Night, still |
| 9 | The Serpent's Guard | Fortress approach | Walls, gates, siege damage | Rubble, scorched earth | Ash, fire glow |
| 10 | Kagachi | Fortress interior / keep | Great hall, banners, throne | Polished stone | Dark, dramatic key |

**Per-location kit:** 12–20 unique props, 1 modular architecture set (wall,
floor, roof, pillar, stair, door), 2–4 vegetation types where applicable.

**Budgets:** modular pieces **300–1,500 tris** each; hero props up to 3,000;
one **2048 atlas** per location kit plus a shared 1024 detail/normal set.
Vegetation on a separate alpha-clip atlas.

**Reuse rule:** a location may reuse at most **30%** of another location's
props. Beyond that the two stop reading as different places.

---

## 6. Texture budgets

| Asset class | Albedo | Normal | ORM | Format |
|---|---|---|---|---|
| Player | 1024² | 1024² | 1024² | ASTC 6×6 |
| Enemy (mook) | 1024² | 1024² | 1024² | ASTC 6×6 |
| Boss | 2048² | 2048² | 2048² | ASTC 6×6 |
| Weapon | 512² | 512² | 512² | ASTC 6×6 |
| Environment kit | 2048² atlas | 2048² | 2048² | ASTC 6×6 |
| VFX | 256² | — | — | ASTC 6×6 |

**ORM packing:** R = ambient occlusion, G = roughness, B = metallic. One
texture, not three. The engine's `Emberline/Surface` shader consumes this.

Total texture memory target: **≤ 180 MB** resident at any time. The importer
already enforces ASTC 6×6 and per-class max sizes — see
`ConfigureTextureBudgets` in the bootstrap.

---

## 7. Polygon budgets

| | Tris |
|---|---|
| Player | 22,000 |
| Enemy mook | 9,000–13,000 |
| Mini-boss / boss | 15,000–18,000 |
| Weapon | 400–1,800 |
| Environment prop | 300–3,000 |
| **Scene total, worst case** | **≤ 200,000 visible** |

Worst case is 8 enemies plus the player plus environment. At 13,000 per enemy
that is 104,000 for characters alone, which is why the mook cap matters more
than the hero cap.

---

## 8. LOD strategy

Three levels per character, generated not hand-authored unless the silhouette
breaks:

| LOD | Screen height | Tris | Notes |
|---|---|---|---|
| LOD0 | > 40% | 100% | Hero framing, executions, boss intro |
| LOD1 | 15–40% | 50% | Normal combat range |
| LOD2 | 5–15% | 25% | Background, crowd |
| Cull | < 5% | — | ~40 m |

Environment: LOD0/LOD1 only for props over 1,000 tris; modular architecture
pieces get no LOD (they are already cheap) but **must be static-batched** —
the bootstrap already calls `StaticBatchingUtility.Combine` per arena segment.

Also required: **GPU instancing** enabled on all environment materials,
**occlusion culling** baked per scene, and **baked lighting** for static
geometry with at most **one realtime shadow-casting directional light** (the
key). Fills and rims stay realtime but shadowless — this is already how
`BuildLighting` is written; keep it.

---

## 9. Integration plan

Ordered so each step is independently verifiable and nothing is blocked on
the whole set arriving.

**Step 1 — Visual isolation (engine work, no art needed).**
Introduce an explicit `VisualRoot` under each character holding every renderer,
the Animator and all sockets. Gameplay components stay on the parent. This
makes "swap the model" a single-subtree operation. *Implemented in this phase.*

**Step 2 — Material pipeline (engine work, no art needed).**
The builder currently forces **one shared material with a single palette
texture onto every renderer**, which a PBR character with albedo/normal/ORM
cannot survive. Add a material mode so a model can keep its own authored
materials. *Implemented in this phase.*

**Step 3 — Data-driven sockets (engine work, no art needed).**
Socket bone names become data instead of a hardcoded `handslot` substring
search, so a new rig declares its own names. *Implemented in this phase.*

**Step 4 — Validator (engine work, no art needed).**
An editor check that takes a candidate FBX and reports pass/fail against this
document: rig type, bone count, socket presence, triangle count, material
count, texture sizes, and the 12 required clips. *Implemented in this phase.*

**Step 5 — Player first.** Commission and integrate Renzo alone. He is on
screen constantly; validating the pipeline on one model surfaces every problem
before it is multiplied by thirteen.

**Step 6 — Enemies in silhouette order.** Pike Guard, Assassin, Axe Raider
first — the three whose silhouettes differ most from each other and from the
Raider. If those three read correctly at distance, the approach is sound.

**Step 7 — Bosses.** Goro, Jin, Kagehira.

**Step 8 — Environments, one mission at a time**, starting with missions 1 and
6 (the two existing arenas) so the new kits replace something rather than
adding a scene.

**Acceptance for every asset:** passes the validator, renders correctly in
`EmberSnapshot`/`EmberStorySnapshot`, and holds frame rate on device with
8 enemies present.
