# Twin Daggers — cs3dviz

```
Asset:                 Dagger with a wooden handle
Source:                Sketchfab
URL:                   https://sketchfab.com/3d-models/dagger-with-a-wooden-handle-8e74e69fda494c5e8d4ae75afa1e7df7
Creator:               cs3dviz (https://sketchfab.com/cs3dviz)
License:               Creative Commons Attribution 4.0 (CC BY 4.0)
Commercial Use:        Yes (Sketchfab download dialog: "Commercial use is allowed")
Attribution Required:  Yes — "Dagger with a wooden handle" by cs3dviz, CC BY 4.0
Downloaded Date:       2026-09-08
Unity Version:         6000.5.9f1
Render Pipeline:       Built-in. Textures not used; shared Mat_Prop steel on attach.
File Format:           FBX (binary 7400), 2,063 triangles, 1,100 vertices
```

**In project:** `Assets/Art/Weapons/Dagger_cs3dviz/dagger_cs3dviz.fbx` and
`Assets/Art/Weapons/Prefabs/twindagger.prefab`.

**Correction (`EmberWeaponImport.Wraps`, fraction mode):** the source is 7 mm tall, blade down, origin
at the guard — too small for hand-typed points, so the builder measures it and sizes it to 0.75 prop
units (0.47 m on Renzo), grip at 14% and guard at 29% of the length from the pommel. Tip at prop y=0.65.

**Referenced by:** `TwinDaggers.asset` (`propRight` and `propLeft = "twindagger"`), one in each hand.
The KayKit `dagger` is unchanged; other characters still reference it.
