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

| Enemy | Selected character | Reason | Rig | Approx. complexity | Animation compatibility | Commercial licence |
| --- | --- | --- | --- | --- | --- | --- |
| Goro (Chief) | **Brute** | Shirtless, tattooed, bald heavy — a toll-road bully. Largest silhouette in the family at 2.25 m (down from the chibi's 2.45; a realistic body at 2.45 read as a giant). Greataxe prop; the axe embedded in the model is hidden in favour of the shared prop (no slash trail — Goro's spec sets no `trail`; among the enemies only Jin, Kagachi, Samurai and the Elite Warrior's greataxe carry one). | Mixamo humanoid | see table below | Shares the 21-clip sword set through Humanoid retargeting; two-handed poses borrow Cleave/Sweep | Adobe Mixamo, royalty-free incl. games |
| Pale Shade (duel) / Shade | Akai, ghost shader | A hooded wraith: the rogue body under the translucent ghost material, pale blue, unarmed. Brady (white oni mask) was the first choice and is rejected below on cost. | — | — | same set | same |
| Jin Kurogane | **Nightshade J Friedrich** | Ornate dark armour with a horned helm and green accents; refined, clearly above a mook. Head is helmeted, so the model's build does not fight the story's pronoun. Greatsword prop, trail. | Mixamo humanoid | see below | same set | same |
| Kagehira / Kagachi | **Ganfaul M Aure** | Silver-haired warlord in spiked plate and a long coat. The one body that reads as commanding armies. Unique to him. | Mixamo humanoid | see below | same set; clones + ghost survive (one skinned mesh) | same |
| Samurai | **Kachujin G Rosales** | Red-and-white ronin with a topknot, wrapped forearms and thighs — the most authentically Japanese figure Mixamo has. Greatsword, trail. | Mixamo humanoid | see below | same set | same |
| Pike Guard | Kachujin, steel-blue tint | Same body read as a garrison soldier; spear prop (scaled sword shaft as today). | — | — | — | — |
| Elite Warrior | Kachujin, dark bronze tint, 2.1 m | Same body, heavier presence through height and the greataxe. | — | — | — | — |
| Assassin | **Akai E Espiritu** | Hooded, quiver on the back, wrapped cloth. Twin daggers as today. | Mixamo humanoid | see below | same set; dual-wield poses borrow Stab/Sweep | same |
| Archer / Weaver (Ranged) | Akai, natural | The quiver is on the model already; hand crossbow prop as today. | — | — | ranged poses use Windup/Throw/Delayed from the shared set | — |
| Rogue Ninja | Ninja, charcoal tint | Renzo's body in a colder grey so the two never read as the same man. | — | — | — | — |
| Raider (Bandit) | Akai, worn-brown tint, 1.72 m | The common low-rank warrior is the lightest body in the set (10.4k tris) — a hooded rogue in worn leather, twin daggers. Brute was the first plan and lost on cost: it is three times the triangles and ten material slots, on the enemy that appears most. | — | — | — | — |
| Axe Raider | Brute, soot tint, 1.95 m | Goro's body between raider and chief in height; greataxe. Rare enough on screen for the heavier body. | — | — | — | — |
| Powder Carrier (Bomber) | Ninja, ochre tint, 1.7 m | A ninja body carrying the smoke bomb prop; reads as a saboteur. | — | — | — | — |

The seven named campaign foes (convoy captain, three blades, drowned guardian,
iron guard, final commander, raider leader, pale shade) need no work: they
spawn on the base kind's prefab and inherit its body.

## Hierarchy the silhouettes give

```
KAGEHIRA   Ganfaul — spiked plate, long coat, unique body, 2.1 m
JIN        Nightshade — horned ornate armour, unique body, 1.85 m
PALE SHADE Akai — hooded, ghosted, pale blue, 1.62 m
GORO       Brute — 2.25 m, the tallest thing on the roof
ELITE      Kachujin at 2.1 m, dark bronze
SAMURAI    Kachujin, red, 1.92 m
ASSASSIN   Akai, violet, hooded, 1.74 m
RAIDERS    Akai, worn brown, 1.72 m
```

## Measured complexity

From `EmberMixamoProbe.Run` (`Logs/mx_all2.log`), against the §4.1 budget of
12–18k triangles for a hero and ≤ 48 bones:

| Body | Triangles (drawn) | Bones | Material slots | Used for |
| --- | --- | --- | --- | --- |
| Akai | 10,386 | 65 | 1 | Raider, Assassin, Archer, Shade |
| Kachujin | 12,610 | 75 | 2 | Samurai, Pike Guard, Elite Warrior |
| Nightshade | 12,999 | 68 | 1 | Jin |
| Ganfaul | 13,801 | 99 | 1 | Kagehira |
| Brute | ~22,800 of 31,301 (axe, earrings, lashes, moustache hidden) | 72 | 6 of 10 | Goro, Axe Raider |
| Ninja | 24,780 | 52 | 1 | Renzo, Rogue Ninja, Bomber |

Four of the six bodies sit inside the triangle budget. Every body is over the
bone budget; that is what Mixamo's auto-rig produces and it is not editable
here. The common mook body (Akai) is the lightest in the set, which is where
the count matters most.

## What was not downloaded

No animations. The 21 humanoid clips already in
`Assets/Art/Characters/Mixamo/Anims` retarget to every body above because
Unity Humanoid clips are avatar-independent. Ranged and two-handed poses
borrow from that set as noted; no bow-draw animation exists in it, which is
flagged in the report.
