# Bow — twistedc3

```
Asset:                 Low Poly Bow + Arrow
Source:                Sketchfab
URL:                   https://sketchfab.com/3d-models/low-poly-bow-arrow-afbf2e77489a4cfc9fc6454d1d698c25
Creator:               twistedc3 (https://sketchfab.com/twistedc3)
License:               Creative Commons Attribution 4.0 (CC BY 4.0)
Commercial Use:        Yes (Sketchfab download dialog: "Commercial use is allowed")
Attribution Required:  Yes — "Low Poly Bow + Arrow" by twistedc3, CC BY 4.0
Downloaded Date:       2026-09-08
Unity Version:         6000.5.9f1
Render Pipeline:       Built-in. Textures not used; shared Mat_Prop steel on attach. (The author
                       notes an unresolved shading bug in their upload; irrelevant here, since the
                       source material is discarded.)
File Format:           FBX (binary 7400), 406 triangles, 246 vertices — bow plus three loose arrows
```

**In project:** `Assets/Art/Weapons/Bow_twistedc3/bow_twistedc3.fbx` and `Assets/Art/Weapons/Prefabs/yumi.prefab`.

**Correction (`EmberWeaponImport.Wraps`):** stave vertical along Y, 5.1 tall, belly at x=−2.28 at
mid-height; the three arrows lie on the +X side and are disabled by the wrapper's new
`hideChildren` rule ("arrow"). Gripped on the belly at mid-height, sized 2.7/5.096 so the bow spans
1.67 m on Renzo. A true yumi is 2.2 m and would leave the frame.

**Referenced by:** `Yumi.asset` — a new WeaponDef (`id = "yumi"`, unlocks at mission 16), the first
weapon carried in the **left** hand with nothing in the right. Fan-shot cleave of three, and the throw
button looses an arrow (the existing Bolt projectile stands in until an arrow model is made).
