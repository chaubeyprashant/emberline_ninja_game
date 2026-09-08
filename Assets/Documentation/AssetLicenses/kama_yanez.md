# Marsh Hook — Yanez Designs kama

```
Asset:                 Kama
Source:                Sketchfab
URL:                   https://sketchfab.com/3d-models/kama-4f96317aecdf421fbe3018a8dbc8f305
Creator:               Yanez Designs (https://sketchfab.com/Yanez-Designs)
License:               Creative Commons Attribution 4.0 (CC BY 4.0)
Commercial Use:        Yes (Sketchfab download dialog: "Commercial use is allowed")
Attribution Required:  Yes — "Kama" by Yanez Designs, CC BY 4.0
Downloaded Date:       2026-09-08
Unity Version:         6000.5.9f1
Render Pipeline:       Built-in. Textures not used; shared Mat_Prop steel on attach.
File Format:           FBX (binary 7400, nested inside source/Kama.zip), 2,140 triangles, 1,400 vertices
```

**In project:** `Assets/Art/Weapons/Kama_Yanez/kama_yanez.fbx` and `Assets/Art/Weapons/Prefabs/kama.prefab`.

**Why a kama:** the Marsh Hook's whole identity is hooking and dragging enemies in. A kama — the
Japanese sickle that doubled as a weapon — says that in silhouette, and it belongs in this world
where a fantasy grapnel would not. The Kusarigama alternative was rejected because its chain would
hang rigid in the fist.

**Correction (`EmberWeaponImport.Wraps`):** butt cap at the origin, handle along +Z to 7.27, blade
out in +X at the far end — the KayKit axe's head-sideways layout. Rotated −90° about X, held 30% up
the handle, sized 1.35/7.275 so it spans 0.84 m on Renzo. Guard at prop y=0.76, tip at 0.80.

**Referenced by:** `MarshHook.asset` (`propRight = "kama"`). The KayKit `axe_2handed` is unchanged and
still pre-attached.
