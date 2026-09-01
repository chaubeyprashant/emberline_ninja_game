# Emberline — Asset Sourcing Plan

How to acquire the art that replaces the current chibi placeholder set.

Companion to [ASSET_SPECIFICATIONS.md](ASSET_SPECIFICATIONS.md), which defines
*what* to build. This document covers *where to get it, in what order, and what
to check before paying*.

> **Prices and licence terms are deliberately absent.** Marketplace pricing and
> licensing change, and inventing figures would make this document worse than
> useful. Every entry below marked **[VERIFY]** requires checking at the time of
> purchase.

---

## 1. Visual target

**Serious cinematic ninja action.** Adult realistic human proportions.
**Stylised realism, not photorealism** — believable materials and proportions,
but readable silhouettes and controlled detail rather than scanned pores.

- Dark, restrained palette. Albedo mostly 0.10–0.45; one accent per character.
- Physically based materials throughout — natural cloth, leather and metal.
- Cinematic lighting: one shadow-casting key, shadowless fill and rim.

**Not:** chibi proportions, toy-like geometry, cel shading, cartoon outlines,
exaggerated anime proportions, saturated primary colours.

### The rule that governs every decision here

**Consistency beats maximum realism.** A coherent world reads better than a
half-upgraded one. A realistic Renzo standing among cartoon barrels looks
*worse* than today, because the mismatch reads as broken rather than stylish.

Never buy realistic characters without a matching plan for the environment they
stand in. If budget only covers one, do the environment materials first —
they are cheaper, they are most of the screen, and they do not clash with
anything.

---

## 2. Character acquisition strategy

### Option A — Character creator system

Reallusion **Character Creator 4** or equivalent.

**Advantages:** one consistent human base for the whole roster; proportions
match by construction; Humanoid rig out of the box; custom clothing; the
pipeline is reusable for every future character.

**Disadvantages:** real learning curve; licensing needs checking for commercial
game use **[VERIFY]**; clothing and weapons are still additional work; exported
meshes usually need decimating to mobile budgets.

**Best when:** you intend to build the full roster yourself and want the cast to
look related. For a solo project this is usually the strongest option, because
consistency is the hardest thing to buy piecemeal.

### Option B — Ready-made marketplace assets

Unity Asset Store, ArtStation Marketplace, Sketchfab, other legitimate
marketplaces.

**Advantages:** fastest; you see the result before committing.

**Disadvantages:** you are limited to what exists. Realistic adult ninja and
samurai characters at mobile budgets are not a well-served niche — most
"ninja" packs are stylised, and most realistic ones are PC-budget. Characters
bought from different vendors rarely look like the same game.

**Non-negotiable requirements:** Humanoid rig · adult proportions · PBR
materials · FBX source · replaceable materials · mobile-reasonable triangle
count · animation compatibility · **commercial game licence [VERIFY]**.

### Option C — Commission

ArtStation, Upwork, or a known character artist.

**Advantages:** exactly the spec, exactly consistent, you own it.

**Disadvantages:** highest cost and longest lead time.

**Rule: commission the full roster only after one commissioned character has
passed the Unity pipeline end to end.** One character through integration
surfaces every problem — rig naming, scale, socket placement, material slots —
before it is multiplied by thirteen.

---

## 3. Character purchase checklist

Check every item before paying. **Never purchase based on screenshots alone** —
marketplace renders are made in offline renderers at PC budgets and tell you
nothing about rig, scale or triangle count.

**Licensing**
- [ ] Commercial game licence, explicitly permitting use in a shipped product **[VERIFY]**
- [ ] Redistribution terms allow compiled-in use in an APK **[VERIFY]**
- [ ] No per-title or revenue-share obligation you cannot meet **[VERIFY]**

**Source files**
- [ ] FBX provided (not only .blend, .max, or a Unity package of prefabs)
- [ ] Textures supplied as separate image files, not only embedded

**Rig**
- [ ] Unity **Humanoid** compatible
- [ ] Bone count within budget
- [ ] Hand bones suitable as weapon sockets
- [ ] No baked weapon fused to the hand mesh

**Transform**
- [ ] Correct scale (metres; ~1.8 m adult)
- [ ] Correct orientation (Y-up, facing +Z)
- [ ] Correct pivot (origin at the floor between the feet)

**Geometry and materials**
- [ ] Triangle count within budget
- [ ] Material slot count within budget
- [ ] PBR maps present — albedo, **normal**, and roughness/metallic (ideally ORM-packed)
- [ ] Texture resolution within budget, or downscalable without falling apart
- [ ] Distinct skin, cloth and leather materials rather than one flat atlas
- [ ] Hair is geometry or alpha-clip cards, not an unsupported hair system

**Integration**
- [ ] Weapon compatible with the socket approach
- [ ] Animation compatible (retargetable, or clips included)
- [ ] LODs included, or the mesh is clean enough to auto-generate them

---

## 4. Renzo — the first character

The player character, on screen constantly, and the first thing to replace.

**Character.** Adult male ninja, 27.

**Body.** Athletic, realistic proportions, medium build. Natural shoulder
width. Normal head-to-body ratio (**7.5 heads**). No exaggerated musculature —
he is trained, not a bodybuilder.

**Clothing.** Dark layered cloth. Wrapped forearms. Leather straps — chest rig,
belt, thigh. Light armour elements only: forearm guards, buckles; no plate.
Cloth that moves. Visible wear and dirt at knees, elbows, hem and boots.

**Weapon.** Katana, realistic proportions, socketed to the right hand.

**Signature detail.** **Aiko's red thread bracelet on the left wrist.** Thin,
slightly frayed. Story-critical and the single saturated colour on the model —
it must be visible in an over-the-shoulder shot.

**Rig.** Unity Humanoid.

**Performance target (mobile):**

| | |
|---|---|
| Triangles LOD0 | 22,000 (hard cap 24,000) |
| Bones | ≤ 52 |
| Material slots | 2 — body opaque, hair/cloth-fringe alpha-clip |
| Textures | 1024² albedo + 1024² normal + 1024² ORM |
| LODs | 3 (100% / 50% / 25%), cull ~40 m |

These are mid-range Android targets, not PC specs. A 100k-triangle character
from a marketplace is not a bargain — it is a retopology job.

---

## 5. Enemy acquisition

Thirteen enemies. The design requirement is that **each is identifiable by
silhouette alone at 20 m, in fog, in one frame.**

**They do not all need unique base meshes.** Clothing, armour and weapon
variants over a shared base create sufficiently different silhouettes and are
far cheaper. Suggested families:

- **Base A — light human:** Raider, Assassin, Archer, Powder Carrier, Rogue Ninja
- **Base B — armoured human:** Pike Guard, Axe Raider, Samurai, Elite Warrior
- **Unique meshes:** Goro (oversized), Jin, Kagehira, Shade

| # | Enemy | Body | Clothing / armour | Weapon | Silhouette read | Colour / material | Gameplay identity |
|---|---|---|---|---|---|---|---|
| 1 | **Raider** | Average | Rough mismatched soldier clothing, cloth head wrap | Worn straight sword | Baseline human, nothing exaggerated | Dull browns, rust; worn cloth and iron | The measuring stick — everything else reads against him |
| 2 | **Assassin** | Lean, tall | Light close-fitting, no armour, face covered | Twin short blades, reverse grip | **Narrow**, low stance | Charcoal, muted; matte cloth | Fastest thing on the field; folds if caught |
| 3 | **Pike Guard** | Solid | Defensive armour, chest plate, shoulder guards | Long polearm held across the body | **Wide horizontal** — the polearm *is* the silhouette | Iron grey, leather straps | Owns the space you want to stand in |
| 4 | **Archer** | Average | Layered cloth, light leather bracer | Longbow, back quiver | **Asymmetric** — bow arm out, quiver above shoulder | Earth tones, waxed cloth | Punishing at range, harmless in melee |
| 5 | **Axe Raider** | Heavy | Heavy body protection, fur/leather mantle | Large single-bit axe | **Top-heavy**, wide shoulders | Dark leather, fur, heavy iron | Armoured; shrugs off chip damage |
| 6 | **Samurai** | Solid | Distinct lamellar armour, sode shoulder plates, kabuto-like helm | Katana | **Boxy, formal** — the most structured shape | Lacquered dark red/black, silk cord | Blocks, then punishes greed |
| 7 | **Rogue Ninja** | Lean | Dark stealth equipment, hood up, wrapped limbs | Short blade | Reads as **the player's mirror** | Colder, cleaner than Renzo; blue-black | Your own kit used against you |
| 8 | **Elite Warrior** | Athletic heavy | High-quality layered plate over mail, no rust | Decorated blade | **Full and heavy** but agile | Polished steel, deep colour, gold accent | Full moveset, no single answer |
| 9 | **Shade** | Indistinct | Ragged, incomplete, translucent | Claws | **Blurred edges** — the only unclear silhouette | Desaturated teal, translucent | Marsh-born; smoke tears it apart |
| 10 | **Powder Carrier** | Average | Raider base plus bulky satchel and bandolier | Thrown bombs | Raider **plus a visible load** on the back | Browns with powder-stained canvas | Kill first or fight on burning ground |
| 11 | **Goro** | Very large (1.25× height) | Minimal armour over bulk, scarred | Oversized axe | **Largest mass in the game** | Scarred skin, crude iron | Telegraphs everything and lands anyway |
| 12 | **Jin** | Lean, precise | Sparse clothing, no armour | One perfect blade | **The calmest silhouette** — stillness reads as skill | Near-monochrome, one cold accent | Answered by reading him, not trading |
| 13 | **Kagehira** | Tall, imposing | Long war-cloak that moves, elevated shoulder line, mask | Ornamented blade | **Visually dominant** — largest presence, most vertical | Black, deep red, one metal accent | The warlord; the only ornamented character |

---

## 6. Animation strategy

**Use one shared Humanoid animation library and retarget it across the whole
roster.** This is the single largest saving available, and it is why
[ASSET_SPECIFICATIONS.md](ASSET_SPECIFICATIONS.md) requires Humanoid rigs.

> **Note:** the current placeholder models import as **Generic**, not Humanoid.
> Moving to Humanoid is part of the replacement, not a separate task.

### Sources to evaluate

| Source | Notes |
|---|---|
| **Mixamo** (Adobe) | Large humanoid library covering most base categories; exports FBX that retargets to Unity Humanoid. Confirm current licence terms for commercial game use **[VERIFY]** |
| **Unity Asset Store** animation packs | Often sold as Humanoid-ready sword/combat sets; check retargeting quality and licence **[VERIFY]** |
| **Marketplace packs** (ArtStation etc.) | Variable; verify rig compatibility before purchase **[VERIFY]** |
| **Custom animation** | Reserve for what defines a character |

### Shared vs custom

**Shareable across the roster via Humanoid retargeting** — buy or download once:

Idle · Walk · Run · Sprint · Jump · Fall · Land · Dodge · Guard · Hit reaction ·
Stagger · Knockdown · Get up · Death · Weapon draw · Weapon sheath

**Should be custom, or at minimum per-weapon-class** — these carry the feel of
the combat and generic clips will not sell them:

- **Light attack 1/2/3** — must chain and alternate sides convincingly
- **Heavy attack** — and especially its wind-up
- **Parry** — a tight, readable window
- **Guard break**
- **Execution** — per weapon class

**Per-boss custom:** Goro, Jin and Kagehira each need signature attacks. A boss
using stock animations reads as a reskinned mook.

**The one clip worth paying most for is the heavy attack wind-up.** The whole
combat model depends on the player reading a telegraph at 20 m. Everything else
can be adequate; that one cannot.

### What the engine currently drives

The game addresses **12 poses** (`RigPose`): Idle, Run, Strike1, Strike2,
Strike3, Cleave, Windup, Hurt, Dash, Dead, Spawn, Taunt. The categories above
are a superset — the extras (walk, sprint, jump, guard, parry, knockdown, get
up, draw, sheath) are for planned expansion and map onto existing states as the
animation set grows. Buy the superset; wire what the game asks for.

---

## 7. Environment sourcing

**The environment is as important as the characters** and is most of the frame.
Ten missions currently reuse two arenas, which is the main reason locations do
not read as distinct.

Acquire as **modular kits** — wall, floor, roof, pillar, stair, door — plus a
prop set per location, rather than pre-built scenes. The arena builder
assembles scenes in code; it needs pieces, not finished levels.

| Location | Architecture | Props | Vegetation / terrain |
|---|---|---|---|
| **Village** | Wooden houses, roofs, fences | Lanterns, carts, barrels, signs, crates | Roadside grass, dirt roads |
| **Bamboo Forest** | — | Branches, fallen bamboo, shrine rope | Bamboo, rocks, dirt, leaf litter, fog cards |
| **Fortress** | Stone walls, gates, guard towers, courtyards | Torches, barricades, banners | Gravel, scorched earth |
| **Marsh** | Broken structures, boardwalks, stilt huts | Fish racks, rope, wrecked carts | Mud, standing water, dead trees, reeds, rocks |
| **Mountain Temple** | Temple architecture, stone stairs, shrines | Lanterns, bells, offering stones | Snow, conifers, rock |
| **Burning Village** | Destroyed buildings, collapsed frames | Burnt wood, debris, fire props | Ash ground, embers, smoke |
| **Graveyard** | Broken structures, low walls | Tombstones, grave markers | Dead trees, fog, wet ground |

Budgets per location: modular pieces **300–1,500 tris**, hero props to 3,000,
one 2048² atlas per kit plus a shared detail/normal set. A location may reuse at
most ~30% of another's props before the two stop reading as different places.

---

## 8. Free material sources

Materials are the cheapest realism available and carry no clash risk.

| Source | Content | Licence |
|---|---|---|
| **Poly Haven** | PBR materials, HDRIs, models | Published as CC0 — **[VERIFY]** per asset at download |
| **ambientCG** | PBR material library | Published as CC0 — **[VERIFY]** per asset at download |

Use primarily for: **stone · wood · dirt · mud · metal · cloth · ground · wet
surfaces** — which covers the eight material types the surface shader is built
around.

**Licence discipline:**
- Record the licence for every asset in the purchase record (§13), including
  free ones. "It was free" is not a licence.
- CC0 means no attribution required, but **verify the specific asset** — a
  library being generally CC0 does not guarantee every item is.
- **Never copy assets from a website without checking the licence.** Images
  found via search engines, artwork on portfolio sites and game rips are not
  usable regardless of how well they fit.

The engine already imports at ASTC 6×6 with per-class size caps
(`ConfigureTextureBudgets`), so downloaded 4K materials are downscaled
automatically — but downloading 4K when 1K is the target wastes repository
space. Fetch at the target resolution.

---

## 9. Asset priority

| Priority | Item | Why |
|---|---|---|
| **P0** | 1. Renzo | On screen constantly; proves the pipeline |
| | 2. Main enemies (Raider, Assassin, Pike Guard, Axe Raider) | Most-seen opponents; establish silhouette language |
| | 3. Primary environment materials | Cheapest realism per unit of effort; most of the frame |
| | 4. Main weapons (katana, enemy sword/axe/spear/bow) | Always in frame beside the character |
| | 5. Mission-critical architecture | Makes locations distinguishable |
| **P1** | 6. Bosses (Goro, Jin, Kagehira) | High impact but seen briefly |
| | 7. Secondary enemies (Archer, Samurai, Rogue Ninja, Elite, Shade, Powder Carrier) | |
| | 8. Props | |
| | 9. Vegetation | |
| | 10. Environmental VFX | |
| **P2** | 11. Cosmetic details (dyes, blade finishes) | |
| | 12. Minor props | |
| | 13. Background decoration | |

---

## 10. Do not buy yet

Until **Renzo + one enemy + one environment** have passed integration, do not
purchase:

- **Thirteen characters simultaneously.** If the pipeline needs a change, you
  pay for it thirteen times.
- **Huge environment packs.** Most of a large pack goes unused, and large packs
  are the most common source of mismatched art styles.
- **Facial animation systems.** There is no face rig, the camera never holds on
  a face, and story beats are staged in silhouette by design. This is money
  spent on something the game cannot show.
- **High-end cinematic assets.** PC-budget assets are a retopology job, not a
  shortcut.
- **Expensive VFX packs.** The VFX systems already exist and are tier-scaled;
  they need art direction, not more particles.
- **Large animation libraries.** Buy a small set, prove retargeting on the real
  rig, then expand.

---

## 11. First proof of concept

The first purchase is a **test**, not a milestone. Buy the minimum that proves
the pipeline:

> **Renzo + one enemy + one weapon + one environment kit**

Suggested enemy: **Pike Guard** — its silhouette differs most from the player,
so a silhouette failure is obvious immediately.

The proof of concept must demonstrate all of:

- [ ] Character appears correctly (scale, orientation, pivot)
- [ ] Humanoid animation retargets and plays
- [ ] Combat works — attacks connect, damage applies
- [ ] Hit detection works (distance/angle based; should be unaffected)
- [ ] Weapon socket works — weapon sits in the hand, correct orientation
- [ ] Materials look correct under the game's lighting, not a marketplace render
- [ ] Shadows cast and receive correctly
- [ ] LODs switch without visible popping
- [ ] Android performance acceptable with 8 enemies present
- [ ] **The character does not look visually disconnected from the environment**

That last one is the real test and the easiest to fail. Judge it on device, in
the game's own lighting, not in the editor viewport.

**Only after all ten pass should the remaining roster be purchased.**

---

## 12. Unity validation

A validator exists: **`Emberline → Validate Character`** with an FBX selected in
the Project window, or `EmberAssetValidator.ValidatePath(path, class)` from a
script. It reports every failure at once rather than stopping at the first.

### Coverage — verified against the implementation

| Field | Status |
|---|---|
| Triangles vs class budget | ✅ Covered |
| Bone count | ✅ Covered |
| Material slots | ✅ Covered |
| Rig type (Humanoid) | ✅ Covered |
| Sockets (both hands) | ✅ Covered |
| Orientation (Y-up, taller than wide) | ✅ Covered |
| Pivot (origin at the feet) | ✅ Covered |
| Clips | ⚠️ **Counts only** — see gaps |
| Texture sizes vs class budget | ✅ Covered |

### Known gaps — tooling tasks, not features that exist

1. **Clip names are not verified.** The validator checks only that the FBX
   carries at least 12 clips, not that the 12 required poses are present. The
   placeholder passes this check with 76 clips, none of which are named for the
   pose table. *Task: validate against the declared pose→clip mapping.*
2. **No LOD check.** The spec requires three LOD levels; the validator does not
   look for a `LODGroup` or LOD meshes. *Task: add.*
3. **No PBR map check.** It does not verify that normal and ORM/roughness maps
   exist — only that textures are not oversized. A model with albedo only would
   pass. *Task: add.*
4. **Character class is manual.** The menu item assumes `Mook` budgets; a player
   or boss asset must be validated from script to get the right budget.
   *Task: infer from filename or prompt.*

Licence, commercial-use rights and vendor terms **cannot be validated
automatically** and must be checked by hand against §3 and recorded in §13.

---

## 13. Purchase record

Copy this block per asset — including free ones. This is what prevents
licensing problems and duplicate purchases.

```
Asset:
Vendor:
URL:
Price:
License:
Platform:
Commercial use:            (yes / no / conditions)
FBX:                       (yes / no)
Rig:                       (Humanoid / Generic / none)
Triangles:
Materials:
Textures:                  (resolution + which maps)
Animations:                (included / retargeted / none)
LOD:                       (included / generated / none)
Dependencies:              (shaders, packages, render pipeline)
Validator result:          (paste the Emberline → Validate Character output)
Integration status:        (not started / in progress / integrated)
Approved / Rejected:
Notes:
```

Keep completed records in `docs/purchases/`.

---

## 14. Final recommendation

Acquisition order:

1. **Renzo**
2. **One enemy** (Pike Guard suggested)
3. **Katana**
4. **One environment kit**
5. **Environment materials** (free CC0 sources — can run in parallel from the start)
6. **Shared animation library**
7. **Remaining enemy roster**
8. **Bosses**
9. **Mission-specific environments**
10. **Props and VFX**

### The key rule

> **Prove one complete character and environment pipeline before spending
> heavily.**

Everything in this document exists to protect that rule. The pipeline is
already built and the validator already runs — what has not been proven is that
a *real purchased asset* survives it. Until one has, every additional purchase
is a bet on an untested assumption.
