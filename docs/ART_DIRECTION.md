# Emberline 3D — Realistic Cinematic Art Direction

Target: **"realistic stylized 3D ninja world for mobile."** Grounded materials and
cinematic light; stylised silhouettes and readable shapes. Not photoreal, not cel.

This document covers two things: the visual systems now in the engine, and the
**asset specifications for the work that cannot be done in code**.

---

## 1. The hard constraint

The in-engine pass changes *how surfaces are lit and graded*. It cannot change
what the meshes are.

Every character in the game is a **KayKit chibi model** with:

- roughly **3–3.5 heads tall** proportions (realistic is 7–7.5)
- oversized head, mitten hands, no separate hair geometry
- **one 1024² albedo atlas and nothing else** — no normal, roughness, metallic,
  AO or mask maps exist for any character

So these five items from the brief are **asset replacements, not engine work**:

| Requested | Why code cannot deliver it |
| --- | --- |
| Character proportions | Head-to-body ratio is baked into the mesh and its skeleton |
| Natural skin | Needs a skin albedo + normal + roughness; the atlas has flat colour blocks |
| Hair | There is no hair geometry — it is painted onto the head texture |
| Realistic clothing / armor | No normal or roughness maps means no weave, no leather grain, no metal wear |
| Realistic weapons | Same: single flat-colour prop meshes, no maps |

A PBR shader on a flat-colour chibi mesh produces a *well-lit chibi mesh*.
Section 4 specifies what to commission or buy.

---

## 2. What the engine now does

### Surface shader — `Emberline/Surface`
Replaces `Emberline/Toon`. Cel banding and the inverted-hull ink outline are gone.

- GGX specular + Lambert diffuse, energy-split by metallic
- **Real shadow receiving.** The toon shader cast shadows through its fallback but
  never received them, so surfaces never darkened — the single biggest reason the
  old look read as flat
- Hemispheric ambient (sky above / warm bounce below) instead of flat ambient
- Additive pass for lantern and torch practicals
- Explicit ShadowCaster pass
- **Procedural grime**: three-octave world-space noise darkening upward faces and
  crevices. This is the stand-in for the missing detail maps — it stops large flat
  albedo areas reading as plastic. It is not a substitute for real texture work

### Material system — `SurfaceKit`
Materials are requested **by physical surface type**, not configured per object:

`Skin · Cloth · Leather · Steel · DarkMetal · Wood · Stone · WetStone · Water ·
Foliage · Rope · Emissive · Ghost`

Each carries smoothness, metallic, grime strength/scale, rim and specular tint.
Changing how all leather in the game looks is a one-line edit to the table.
`SurfaceKit.Grade()` desaturates authored colours toward the cinematic palette.

### Lighting
Three-point rig replacing the single directional light:

- **Key** — soft shadows, `shadowStrength 0.72` (full-black shadows read as CG)
- **Fill** — cool, opposite side, no shadow map
- **Rim** — low warm back-light to separate silhouettes from fog

Ambient is Trilight; fog is **ExponentialSquared** (linear fog has a visible wall
that gives away draw distance). Lantern practicals stay per-pixel; road lanterns
are vertex-lit to protect the pixel-light budget.

### Post-processing — `Emberline/Grade`
One full-screen pass: filmic (ACES-approx) tonemap, saturation pull to 0.82,
contrast 1.08, cool shadow lift, warm highlight gain, soft vignette.
Highlights roll off instead of clipping, which is most of what separates
"rendered" from "photographed".

### Scalability
| | Low (30fps floor) | Medium | High |
| --- | --- | --- | --- |
| Render scale | 0.75 | 0.9 | 1.0 |
| Grade pass | **off** | on | on |
| Shadows | Hard, low res, 18m | Soft, med, 30m | Soft, high, 45m, 2 cascades |
| Pixel lights | 1 | 3 | 5 |
| Particle density | 0.5× | 1× | 1.35× |
| Skin weights | 2 bones | 4 | 4 |

---

## 3. Palette

Night exterior, low saturation. Authored colours are graded down on import to the
material system, so **do not author saturated colours** — they will be pulled.

| Role | Value |
| --- | --- |
| Stone / deck | `#20242B` damp charcoal, smoothness 0.34 |
| Timber | `#2E241B` |
| Steel | `#8C929C`, metallic 0.85, smoothness 0.62 |
| Lantern practical | `#FF8C4D` @ 2.9 intensity, 12m range |
| Key light (night) | `#8CA3DB` |
| Fog | `#0E1017`, density 0.022 |
| UI accent | keep ember, but at ≤0.6 saturation against the graded frame |

---

## 4. Asset specifications for replacement

Mobile budgets assume ~8 characters on screen at 60fps on a mid-range Android.

### 4.1 Characters (the priority)

| Spec | Value |
| --- | --- |
| Proportions | **7–7.5 heads**, adult athletic build |
| Triangles | Hero 12–18k · elite 8–12k · mook 5–8k |
| Bones | ≤ 48, single skinned mesh, ≤ 4 influences (2 on Low tier) |
| Rig | Humanoid, Mecanim-compatible — the existing `RigPose` clip mapping depends on it |
| Textures | 1024² albedo + normal + **ORM packed** (occlusion/roughness/metallic in R/G/B) |
| Format | ASTC 6×6, mips on |
| Hair | Separate low-poly card geometry or sculpted shell — not painted onto the head |
| Skin | Albedo without baked lighting; roughness 0.35–0.5; no specular map needed |
| Wear | Dirt, scuffs and edge wear **baked into albedo + roughness**, not left to the shader |

**Cast needed:** Renzo (hero), Raider, Weaver/archer, Shade (translucent), Goro
(heavy), Kagachi (boss), Jin (duelist), Axe Raider, Pike Guard, Bomber.

Sourcing that fits this budget and style: Synty *POLYGON Ninja / Samurai*
(stylised-realistic, mobile-friendly, consistent rig), Quaternius realistic
packs (CC0), or a commissioned set to the spec above. Whatever is chosen must
share **one skeleton** so the existing animation mapping keeps working.

### 4.2 Weapons

| Spec | Value |
| --- | --- |
| Triangles | 400–1,500 |
| Textures | Shared 1024² atlas across the whole weapon set — one draw call |
| Maps | Albedo + normal + ORM; edge wear and blood/soot baked in |
| Pivot | At the grip, +Z along the blade — the throw code already assumes this |

Needed: katana, twin daggers, greataxe, pike/spear, hand crossbow + bolt, kunai,
smoke bomb.

### 4.3 Environment

| Spec | Value |
| --- | --- |
| Modular kit | 2m grid roof/wall/parapet pieces so arenas stay code-generated |
| Triangles | 200–2,000 per piece |
| Textures | One 2048² trim-sheet atlas for the whole kit + normal + ORM |
| Tiling | Roof tile, plaster, timber, wet stone — 512², tileable, with normals |
| Props | Barrels, crates, lanterns, banners: shared atlas, ≤ 800 tris |

The current deck is untextured flat cubes. A **tiling roof-tile albedo + normal**
is the single highest-impact environment asset — it is the largest surface on
screen and currently the flattest.

### 4.4 VFX

Replace the Kenney cartoon sheets (`twirl`, `star`) with:
smoke/dust 512² soft-alpha sequences, spark streaks, and a subtle blood mist.
Counts stay driven by tier density; avoid additive-heavy stacking — overdraw is
the main mobile GPU cost here.

---

## 5. Recommended order

1. **Characters** — the single change that moves the look furthest, and the one
   thing the engine work cannot fake
2. **Roof-tile + stone tiling textures** — largest on-screen surface
3. **Weapon set** on a shared atlas
4. **Modular environment kit**
5. **VFX sheets**
6. **UI restyle** to match the graded frame

Until step 1 lands, the game will read as *a well-lit toy world* rather than a
realistic one — which is exactly what the current build is.
