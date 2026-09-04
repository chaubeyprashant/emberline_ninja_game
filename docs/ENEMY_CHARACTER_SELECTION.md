# Enemy character selection

Part 2 of the enemy visual overhaul. The audit is
`docs/ENEMY_CHARACTER_ASSET_AUDIT.md`; the closing report is
`docs/ENEMY_CHARACTER_OVERHAUL_REPORT.md`.

## The reference: Renzo

The player is Mixamo's **Ninja** (`Ch24_nonPBR`), imported Humanoid, rendered
through `Emberline/Surface` with its own 1024² albedo, normalised to 1.8 m,
sword on a declared grip anchor. Every enemy below was judged against that
body in Mixamo's large viewer or a zoomed thumbnail, not against its name.

## How the library was searched

Mixamo's free character library holds 108 characters. Searching the brief's
terms — ninja, samurai, warrior, masked, ronin, armored, dark, elite — returns
almost nothing by name (the query "ninja" yields three results, one of them a
ninja), so every one of the 108 was reviewed by thumbnail and the plausible
ones in the viewer. Rejections that mattered, because the thumbnails lied:

| Character | Why it is out |
| --- | --- |
| Heraklios By A. Dizon | Reads as an armoured warrior at thumbnail size; in the viewer it is a gas-masked dieselpunk soldier with ammo pouches and a breathing tube |
| Vanguard By T. Choonyung | Sci-fi power armour |
| Paladin J Nordstrom (both) | European plate knight with a great helm |
| Castle Guard 01 / 02 | European chainmail guards |
| Peasant Man / Peasant Girl | Bavarian, not Japanese |
| Prisoner B Styperek, Romero, Yaku J Ignite, Mremireh O Desbiens | Zombies |
| Warrok W Kurniawan, Maw J Laygo, Mutant, Goblin, Demon, Pumpkinhulk | Monsters; the brief wants a human warrior family |
| The Boss, Big Vegas | Cartoon proportions |
| **Brady** | The oni mask was the best Shade read in the library, but the body alone is 52,972 triangles (59,194 with hair and beard) — three and a half times the hero budget, on an enemy that appears in numbers. Downloaded, measured, removed. |
| Exo Gray / Exo Red, Swat, Alien Soldier, Crypto | Modern or sci-fi |
| Everything in street clothes (Remy, Jolleen, Kate, Megan, Leonard, Joe …) | Modern |

That leaves six bodies that belong in the same world as the Ninja, all
adult-proportioned, all realistic PBR, all on the shared Mixamo skeleton.

## Selection

Per the brief's §6, a small number of bases with variants beats thirteen
unrelated models. Variants are material variants: one body, one texture, a
different `tint` on the game shader, a different weapon, a different height.

| Enemy | Body | Why it reads as itself | Tint (multiplies albedo) | Height |
| --- | --- | --- | --- | --- |
| Renzo (player) | **Ninja** | The hero silhouette: hooded, masked, navy | none | 1.80 |
| Raider (Bandit) | **Akai** | The commonest mook gets the cheapest body (10.4k): hooded rogue, twin daggers | warm tanned leather 1.05/0.72/0.45 | 1.72 |
| Weaver / Archer | **Erika Archer** | Dark layered leather with a quiver on her back — reads "ranged" before she shoots | none | 1.68 |
| Assassin | **Vampire A Lusth** | A red hood over pale wrapped grey — the one splash of red among the mooks | none | 1.76 |
| Rogue Ninja | Ninja, pale cold | Renzo's own body, deliberately: the mirror. Lifted and cooled so the two never read as one man | moonlit steel 0.70/0.88/1.25 | 1.78 |
| Powder Carrier (Bomber) | **Pirate** | A cloaked, hooded saboteur. The atlas is mostly white, so the tint takes it down to night slate | night slate 0.40/0.38/0.48 | 1.70 |
| Shade / Pale Shade | Akai, ghosted | Translucent, so it shares the Raider's body without ever reading as it | pale blue + 0.55 alpha | 1.62 |
| Pike Guard | **Kachujin**, cold | Garrison steel-blue against the Samurai's red, and a spear where the Samurai has a greatsword | cold steel 0.40/0.66/1.30 | 1.95 |
| Samurai | Kachujin, as authored | The red-and-white ronin, topknot and wrapped limbs | none | 1.88 |
| Elite Warrior | **Uriel A Plotexia** | Ornate gilded plate — status you can read at a glance, above every other non-boss | none | 2.05 |
| Axe Raider | **Brute**, cold | Goro's body 0.27 m shorter and sooted cold, so the pair are two different men | soot 0.42/0.45/0.58 | 1.98 |
| Goro (Chief) | Brute, warm | The biggest thing on the roof, bare-chested and firelit | firelit 1.20/0.82/0.68 | 2.25 |
| Jin Kurogane | **Nightshade J Friedrich** | Horned ornate armour, unique to him | none | 1.85 |
| Kagehira / Kagachi | **Ganfaul M Aure** | Spiked plate and a long coat — the one body that commands armies | none | 2.10 |

Nine bodies carry fourteen roles. The rule is that **no two enemies which can
share a wave share a body**; where a body is reused it is only across a pair
separated by something stronger than colour:

| Body | Shared by | What separates them |
| --- | --- | --- |
| Ninja | Renzo / Rogue Ninja | A deliberate mirror — the story's point. Navy vs pale cold |
| Akai | Raider / Shade | The Shade is translucent |
| Kachujin | Samurai / Pike Guard | Greatsword vs spear, red vs steel-blue |
| Brute | Goro / Axe Raider | 2.25 m warm and bare-chested vs 1.98 m cold and sooted |

Tints multiply the albedo in `Emberline/Surface`, so a value above 1 brightens
rather than washes. The first pass used 0.5–0.8 greys, which only darkened
already-dark textures and left the cast looking alike — that is the defect
this table replaces.

### The named foes needed their own bodies after all

The seven named campaign foes are `CopySerialized` clones of a base kind's def,
so they inherited its body — and `EnemyDef.modelSpec` existed for exactly this
but nothing read it. `GameManager.PrefabFor(kind, defId)` is now the single
place that decides which body a spawn uses, and the bootstrap builds an
`Assets/Prefabs/Named_<id>.prefab` for any named foe that declares a visual.

### A recolour is not a unique character

The first attempt at the named foes gave the Drowned Guardian **Goro's body in
green** and Commander Hoshu **Jin's armour in bronze**. That is the same defect
as the grey tints, one level up: a repainted model still reads as the same
character. Three more bodies were downloaded so that **every one of the nine
duel opponents is a different model**, not a different colour:

| Rung | Opponent | Body | Was |
| --- | --- | --- | --- |
| 01 | Goro | Brute | — |
| 02 | The Pale Shade | Akai, ghosted | — |
| 03 | Jin Kurogane | Nightshade | — |
| 04 | Kagachi | Ganfaul | — |
| 05 | The Convoy Captain | Kachujin | — |
| 06 | The Three Blades | Vampire | shared the Assassin's body |
| 07 | The Drowned Guardian | **Maw** — antlered marsh brute in hide and bone | Goro recoloured green |
| 08 | The Iron Guard | Uriel | — |
| 09 | Commander Hoshu | **Paladin** — full dark plate and a great helm | Jin recoloured bronze |

The Assassin moved to **Arissa** (hooded cutthroat in a long coat) to free the
Vampire for the Three Blades, so the mook and the named foe differ too.

Three bodies were downloaded for this and one was rejected: **Morak** is 47,768
triangles from a 114 MB source — nearly three times the hero budget — so the
Assassin took Arissa (8,970) instead.

## Hierarchy the silhouettes give

```
KAGEHIRA    Ganfaul — spiked plate and long coat, 2.10 m
JIN         Nightshade — horned ornate armour, 1.85 m
GORO        Brute — 2.25 m, bare-chested, the tallest thing on the roof
ELITE       Uriel — gilded plate, 2.05 m
PALE SHADE  Akai — hooded and translucent, 1.62 m
SAMURAI     Kachujin, red, 1.88 m       PIKE GUARD  Kachujin, steel-blue, spear, 1.95 m
AXE RAIDER  Brute, cold, 1.98 m         ASSASSIN    Vampire, red hood, 1.76 m
ARCHER      Erika, quiver, 1.68 m       BOMBER      Pirate, cloaked, 1.70 m
RAIDER      Akai, warm brown, 1.72 m    ROGUE NINJA Ninja, pale cold, 1.78 m
```

## Measured complexity

From `EmberMixamoProbe.Run` (`Logs/mx_all2.log`), against the §4.1 budget of
12–18k triangles for a hero and ≤ 48 bones:

| Body | Triangles (drawn) | Renderers | Bones | Used for |
| --- | --- | --- | --- | --- |
| Arissa | 8,970 | 4 | 73 | Assassin |
| Akai | 10,386 | 1 | 65 | Raider, Shade |
| Uriel | 11,026 | 22 | 72 | Elite Warrior, Iron Guard |
| Kachujin | 12,610 | 1 | 75 | Samurai, Pike Guard, Convoy Captain |
| Nightshade | 12,999 | 1 | 68 | Jin |
| Pirate | 13,115 | 1 | 76 | Bomber, Scavenger King |
| Ganfaul | 13,801 | 1 | 99 | Kagehira |
| Maw | 13,910 | 1 | 64 | Drowned Guardian |
| Paladin | 14,660 | 2 | 67 | Commander Hoshu |
| Vampire | 15,022 | 1 | 99 | Three Blades |
| Erika | 20,526 | 4 | 67 | Archer |
| Brute | ~22,800 of 31,301 | 5 of 9 | 72 | Goro, Axe Raider |
| Ninja | 24,780 | 1 | 52 | Renzo, Rogue Ninja |

Rejected on cost: **Morak** 47,768 tris (114 MB source), **Brady** 52,972.

Nine of the thirteen bodies sit inside the triangle budget, and the commonest
mook body (Akai) is near the lightest, which is where the count matters most.
Every body is over the bone budget; that is what Mixamo's auto-rig produces and
it is not editable here.

**Uriel is the one to watch.** Its triangle count is modest but it arrives as
22 separate skinned meshes, so it costs 22 draw calls rather than one. That is
why it went to the Elite Warrior — tough, and never numerous — instead of a
wave mook. If Elites ever spawn in threes, this is the first thing to revisit.

## What was not downloaded

No animations. The 21 humanoid clips already in
`Assets/Art/Characters/Mixamo/Anims` retarget to every body above because
Unity Humanoid clips are avatar-independent. Ranged and two-handed poses
borrow from that set as noted; no bow-draw animation exists in it, which is
flagged in the report.
