# Katana — Dokazvo

```
Asset:                 Samurai katana low poly - Game ready
Source:                Sketchfab
URL:                   https://sketchfab.com/3d-models/samurai-katana-low-poly-game-ready-422870b712f04fcd9fb38edd467eaded
Creator:               Dokazvo (https://sketchfab.com/dokazvo)
License:               Creative Commons Attribution 4.0 (CC BY 4.0)
Commercial Use:        Yes (stated on the Sketchfab download dialog: "Commercial use is allowed")
Attribution Required:  Yes — "Samurai katana low poly - Game ready" by Dokazvo, CC BY 4.0
Downloaded Date:       2026-09-08
Unity Version:         6000.5.9f1
Render Pipeline:       Built-in. Textures are NOT used: every hand prop takes the shared
                       Mat_Prop steel surface on attach (EmberCharacterFactory.PropMaterial),
                       so only the mesh ships.
File Format:           FBX (binary 7400), 12 sub-meshes, 6,118 triangles, 3,506 vertices
```

**In project:** `Assets/Art/Weapons/Katana_Dokazvo/katana_dokazvo.fbx` (source, untouched) and
`Assets/Art/Weapons/Prefabs/katana.prefab` (the wrapper that AttachProp uses).

**Correction applied by `EmberWeaponImport.Wraps`:** authored along Z with the origin on the
blade; rotated −90° about X so the blade runs +Y, scaled 1.9/58.097 so the whole sword is
1.9 prop units (1.18 m on Renzo at his 0.62 prop scale), shifted so the grip point a third
of the way down the tsuka sits on the hand anchor. Guard at prop y=0.16, tip at 1.52,
pommel at −0.38 — within a few centimetres of the KayKit sword it replaces.

**Referenced by:** `EmberKatana.asset` (`propRight = "katana"`), Renzo's default spec.
The old `sword_1handed.fbx` is untouched and still pre-attached for any other reference.
